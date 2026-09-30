using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Destination portion of MorphOS Copy's TestDest policy. It owns only its
/// transient target lock and FileInfoBlock; later stages own protection-force,
/// directory creation, path construction, and user-facing diagnostics.
/// </summary>
public static class NativeMorphOSCopyDestination
{
    public const int Error = -1, None = 0, Deleted = 1, Directory = 2,
        CantDelete = -2;

    /// <summary>
    /// Classifies or removes an existing destination. `directoryTarget` keeps
    /// an existing directory, while ordinary targets delete an existing file
    /// or empty directory unless DONTOVERWRITE is set. FORCEOVERWRITE clears
    /// protection before the delete attempt, matching MorphOS KillFile; the
    /// SetProtection result does not suppress that attempt.
    /// </summary>
    public static int Prepare(APTR name, bool directoryTarget, bool dontOverwrite,
        bool forceOverwrite, out int ioError)
        => Prepare(name, directoryTarget, dontOverwrite, forceOverwrite,
            false, APTR.Null, out ioError);

    /// <summary>Extended examination uses sixteen caller-owned tag bytes.</summary>
    public static int Prepare(APTR name, bool directoryTarget, bool dontOverwrite,
        bool forceOverwrite, bool extendedExamine, APTR examineTags, out int ioError)
    {
        ioError = 0;
        if (name.IsNull)
        {
            ioError = (int)DOS.Error.BadTemplate;
            DOS.SetIoErr((DOS.Error)ioError);
            return Error;
        }

        var lock_ = DOS.LockRaw(CString.FromPointer(name.Raw), DOS.LockMode.Shared);
        if (lock_.IsNull)
            return None;

        var fib = DOS.AllocDosObject((uint)DosObjectType.FileInfoBlock, APTR.Null);
        var result = Error;
        if (fib.IsNull)
        {
            DOS.UnLock(lock_);
            ioError = (int)DOS.IoErr();
            return Error;
        }

        APTR.WriteUInt8(fib, FileInfoBlock.ActualExtensionFlagsOffset, 0);
        int examined;
        if (extendedExamine)
        {
            APTR.WriteUInt32(examineTags, 0, 0x80000e11);
            APTR.WriteUInt32(examineTags, 4, 1);
            APTR.WriteUInt32(examineTags, 8, 0);
            APTR.WriteUInt32(examineTags, 12, 0);
            examined = DOS.Examine64(lock_, fib, examineTags);
        }
        else
        {
            examined = DOS.Examine(lock_, fib);
            APTR.WriteUInt32(fib, FileInfoBlock.Size64Offset, 0);
            APTR.WriteUInt32(fib, FileInfoBlock.Size64Offset + 4,
                APTR.ReadUInt32(fib, FileInfoBlock.SizeOffset));
            APTR.WriteUInt32(fib, FileInfoBlock.NumBlocks64Offset, 0);
            APTR.WriteUInt32(fib, FileInfoBlock.NumBlocks64Offset + 4,
                APTR.ReadUInt32(fib, 128)); // fib_NumBlocks
        }
        if (examined != 0)
        {
            var isDirectory = FileInfoBlock.GetDirEntryType(fib.Raw) > 0;
            DOS.UnLock(lock_);
            lock_ = BPTR.Null;
            if (directoryTarget && isDirectory)
                result = Directory;
            else if (dontOverwrite)
                result = CantDelete;
            else
            {
                var target = CString.FromPointer(name.Raw);
                if (forceOverwrite) DOS.SetProtection(target, 0);
                if (DOS.DeleteFile(target) != 0) result = Deleted;
            }
        }
        // TestDest retains the FIB until after classification/deletion, and
        // does not restore an earlier IoErr over cleanup results.
        DOS.FreeDosObject((uint)DosObjectType.FileInfoBlock, fib);
        if (lock_.IsNotNull) DOS.UnLock(lock_);
        if (result == CantDelete)
        {
            DOS.SetIoErr(DOS.Error.ObjectExists);
        }
        if (result < 0) ioError = (int)DOS.IoErr();
        return result;
    }
}
