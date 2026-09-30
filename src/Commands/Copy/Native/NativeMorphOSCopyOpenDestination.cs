using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Joins MorphOS Copy's filesystem and non-filesystem OpenDestDir branches.</summary>
public static class NativeMorphOSCopyOpenDestination
{
    /// <summary>
    /// Full stateful OpenDestDir routing. Caller supplies writable terminated
    /// names that fit the command's 2048-byte destination workspace.
    /// Unlike the older bounded helper, flags change before the stream lock,
    /// including when that final lock fails.
    /// </summary>
    public static BPTR Open(APTR name, ref NativeMorphOSCopyTraversalState state)
    {
        if ((state.Mode == NativeMorphOSCopyModeSelection.Copy ||
             state.Mode == NativeMorphOSCopyModeSelection.Move) &&
            !NativeMorphOSCopyTraversal.IsFileSystem(name))
        {
            state.Flags |= 1u << 25;
            var offset = 0;
            byte value;
            do
            {
                value = APTR.ReadUInt8(name, offset);
                APTR.WriteUInt8(state.DestinationName, offset, value);
                offset++;
            } while (value != 0);
            return DOS.LockRaw(CString.FromPointer(state.DestinationName.Raw + (uint)offset - 1),
                DOS.LockMode.Shared);
        }
        return NativeMorphOSCopyMakeDirectory.OpenDirectory(name, ref state);
    }

    /// <summary>
    /// Final directory-target source loop after destination classification and
    /// pattern preparation. Does not reinterpret a single-file destination.
    /// The caller must establish original primary/secondary admission first.
    /// </summary>
    public static void RunDirectorySources(ref NativeMorphOSCopyOptions options,
        APTR classifierAnchor, ref NativeMorphOSCopyTraversalState state)
    {
        if (state.Result != 0 || state.SecondaryResult != 0 ||
            (state.Flags & (1u << 21)) != 0) return;
        state.Destination = Open(options.Target, ref state);
        if (state.Destination.IsNull) return;
        var limit = (state.Flags & 1024) != 0 ? DOS.RETURN_OK : DOS.RETURN_WARN;
        var offset = 0;
        uint source;
        while (state.Result <= limit &&
            (source = APTR.ReadUInt32(options.Sources, offset)) != 0 &&
            !NativeCommandIo.IsCtrlCPending())
        {
            NativeMorphOSCopyTraversal.Run(APTR.FromPointer(source), classifierAnchor, ref state);
            offset += 4;
        }
        DOS.UnLock(state.Destination);
        state.Destination = BPTR.Null;
        state.CurrentDestination = BPTR.Null;
    }
    /// <summary>
    /// Opens a Copy/MOVE destination directory. The non-filesystem branch has
    /// precedence only when its device test succeeds; an ordinary filesystem
    /// name follows the slash-prefix creation path. The caller retains the
    /// returned lock and owns both writable name buffers.
    /// </summary>
    public static BPTR Open(APTR mutableName, APTR destinationName,
        uint destinationBytes, bool copyOrMove, bool forceOverwrite,
        out bool destinationNoFileSystem, out int ioError)
    {
        destinationNoFileSystem = false;
        var nonFileSystem = NativeMorphOSCopyNonFileSystemDestination.TryOpen(
            mutableName, destinationName, destinationBytes, copyOrMove,
            out var selectedNonFileSystem, out ioError);
        if (selectedNonFileSystem)
        {
            destinationNoFileSystem = true;
            return nonFileSystem;
        }
        if (ioError != 0)
            return BPTR.Null;
        return NativeMorphOSCopyDestinationDirectories.Open(mutableName,
            forceOverwrite, out ioError);
    }
}
