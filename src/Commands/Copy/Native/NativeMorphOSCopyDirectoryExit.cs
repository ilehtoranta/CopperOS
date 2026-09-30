using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Bounded APF_DIDDIR exit transition from MorphOS Copy PatCopy.</summary>
public static class NativeMorphOSCopyDirectoryExit
{
    /// <summary>
    /// Completes the directory-exit branch after the matcher FIB has been
    /// snapshotted. Metadata flags use NativeMorphOSCopyMetadata's mapping.
    /// </summary>
    public static bool ProcessMatched(APTR anchor, APTR sourceFib, int mode,
        ref int depth, BPTR destination, ref BPTR currentDestination,
        ref uint commandFlags, ref int destinationPathSize, uint metadataFlags,
        out bool dispatchWork, out bool parentFailed)
    {
        var hadDestination = currentDestination.IsNotNull;
        var exited = Process(anchor, mode, ref depth, destination,
            ref currentDestination, out dispatchWork, out parentFailed);
        if (!exited) return false;
        commandFlags |= 1u << 23; // COPYFLAG_ENTERSECOND
        if (!hadDestination) return true;
        destinationPathSize = 0;
        if (parentFailed) return true;
        if (mode == NativeMorphOSCopyModeSelection.Copy ||
            mode == NativeMorphOSCopyModeSelection.Move)
        {
            var previous = DOS.CurrentDirRaw(currentDestination);
            NativeMorphOSCopyMetadata.Apply(APTR.FromPointer(anchor.Raw +
                (uint)DosLayout.AnchorPath.Info + FileInfoBlock.FileNameOffset),
                sourceFib, metadataFlags);
            DOS.CurrentDirRaw(previous);
        }
        return true;
    }

    /// <summary>
    /// Handles one already-reported directory exit. It clears APF_DIDDIR,
    /// decrements Copy depth, schedules the source's DELETE/MOVE deferred work,
    /// and changes CurDest to its parent. Only a transient CurDest is unlocked;
    /// the destination owner is retained. Returns whether APF_DIDDIR was set.
    /// </summary>
    public static bool Process(APTR anchor, int mode, ref int depth,
        BPTR destination, ref BPTR currentDestination, out bool dispatchWork,
        out bool parentFailed)
    {
        dispatchWork = false;
        parentFailed = false;
        var flags = APTR.ReadUInt8(anchor, DosLayout.AnchorPath.Flags);
        if ((flags & (byte)AnchorPathFlags.DidDirectory) == 0) return false;

        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
            (byte)(flags & ~(byte)AnchorPathFlags.DidDirectory));
        depth = (depth - 1) & 255; // CopyData.Deep is UBYTE in the original.
        dispatchWork = mode == NativeMorphOSCopyModeSelection.Delete ||
            mode == NativeMorphOSCopyModeSelection.Move;

        var previous = currentDestination;
        if (previous.IsNull) return true;
        currentDestination = DOS.ParentDirRaw(previous);
        if (previous.Raw != destination.Raw)
            DOS.UnLock(previous);
        parentFailed = currentDestination.IsNull;
        return true;
    }
}
