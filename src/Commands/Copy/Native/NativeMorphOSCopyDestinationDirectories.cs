using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded MorphOS Copy <c>OpenDestDir</c> directory-creation path. The caller
/// supplies writable, null-terminated destination storage and retains the
/// returned final shared lock.
/// </summary>
public static class NativeMorphOSCopyDestinationDirectories
{
    public const int Error = -1;

    /// <summary>
    /// Walks slash-delimited prefixes, retaining existing directories and
    /// creating absent or replaced prefixes. Each temporary created-directory
    /// lock is released before the next prefix; the final shared lock belongs
    /// to the caller. This deliberately excludes non-filesystem fallback,
    /// verbose diagnostics, and command-result selection.
    /// </summary>
    public static BPTR Open(APTR mutableName, bool forceOverwrite, out int ioError)
    {
        ioError = 0;
        if (mutableName.IsNull)
        {
            ioError = (int)DOS.Error.BadTemplate;
            DOS.SetIoErr((DOS.Error)ioError);
            return BPTR.Null;
        }

        var index = 0;
        while (true)
        {
            var character = APTR.ReadUInt8(mutableName, index);
            if (character == 0 || character == (byte)'/')
            {
                APTR.WriteUInt8(mutableName, index, 0);
                var outcome = NativeMorphOSCopyDestination.Prepare(mutableName,
                    true, false, forceOverwrite, out ioError);
                if (outcome == NativeMorphOSCopyDestination.None ||
                    outcome == NativeMorphOSCopyDestination.Deleted)
                {
                    var created = DOS.CreateDirRaw(CString.FromPointer(mutableName.Raw));
                    if (!created.IsNull)
                        DOS.UnLock(created);
                    else
                    {
                        ioError = (int)DOS.IoErr();
                        APTR.WriteUInt8(mutableName, index, character);
                        return BPTR.Null;
                    }
                }
                else if (outcome != NativeMorphOSCopyDestination.Directory)
                {
                    APTR.WriteUInt8(mutableName, index, character);
                    return BPTR.Null;
                }
                APTR.WriteUInt8(mutableName, index, character);
                if (character == 0)
                    break;
            }
            index++;
        }

        var destination = DOS.LockRaw(CString.FromPointer(mutableName.Raw),
            DOS.LockMode.Shared);
        if (destination.IsNull)
            ioError = (int)DOS.IoErr();
        return destination;
    }
}
