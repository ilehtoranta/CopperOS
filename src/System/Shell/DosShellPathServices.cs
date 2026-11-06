using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>DOS services used by the shared Shell path transaction.</summary>
internal static class DosShellPathServices
{
    public static bool Enter<TPlatform>(ref TPlatform dos, uint quiet,
        out APTR process, out APTR previous) where TPlatform : struct, IDosPlatform
    {
        process = APTR.Null; previous = APTR.Null;
        if (!DosRequesterPolicy.TryEnter(ref dos, quiet, out var scope)) return false;
        process = scope.Process; previous = scope.PreviousWindowPointer;
        return true;
    }

    public static void Exit<TPlatform>(ref TPlatform dos, APTR process, APTR previous)
        where TPlatform : struct, IDosPlatform
    {
        var scope = new DosRequesterScope
        {
            Process = process, PreviousWindowPointer = previous,
            Active = process.IsNull ? 0u : 1u,
        };
        DosRequesterPolicy.Exit(ref dos, ref scope);
    }

    public static BPTR LockDirectory<TPlatform>(ref TPlatform dos, APTR state, APTR name)
        where TPlatform : struct, IDosPlatform
    {
        var info = dos.AllocateGuest(FileInfoBlock.SizeInBytes);
        if (info.IsNull || !DosFileInfoBlockCodec.IsMapped(ref dos, info))
        {
            if (info.IsNotNull) dos.FreeGuest(info, FileInfoBlock.SizeInBytes);
            DosCore.SetIoErr(ref dos, state, DOS.Error.NoFreeStore);
            return BPTR.Null;
        }
        dos.Clear(info, FileInfoBlock.SizeInBytes);
        var directory = DosCore.Lock(ref dos, state, name, DOS.LockMode.Shared);
        var valid = directory.IsNotNull && DosCore.Examine(ref dos, state, directory, info) != 0;
        var error = DosCore.IoErr(ref dos, state);
        if (valid && DosFileInfoBlockCodec.ReadDirEntryType(ref dos, info) <= 0)
        {
            valid = false;
            error = DOS.Error.ObjectWrongType;
        }
        dos.FreeGuest(info, FileInfoBlock.SizeInBytes);
        if (!valid && directory.IsNotNull)
        {
            DosCore.UnLock(ref dos, state, directory);
            directory = BPTR.Null;
        }
        DosCore.SetIoErr(ref dos, state, valid ? DOS.Error.None : error);
        return directory;
    }

    public static bool SameLock<TPlatform>(ref TPlatform dos, APTR state,
        BPTR first, BPTR second, out bool same) where TPlatform : struct, IDosPlatform
    {
        var comparison = DosCore.SameLock(ref dos, state, first, second);
        var error = DosCore.IoErr(ref dos, state);
        same = comparison == 0;
        return error == DOS.Error.None && comparison is >= -1 and <= 1;
    }

    public static void ReleaseLock<TPlatform>(ref TPlatform dos, APTR state, BPTR directory)
        where TPlatform : struct, IDosPlatform
    {
        var error = DosCore.IoErr(ref dos, state);
        DosCore.UnLock(ref dos, state, directory);
        DosCore.SetIoErr(ref dos, state, error);
    }
}
