using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Source-observed MorphOS Relabel command body.</summary>
public static class NativeMorphOSRelabelCommand
{
    private const uint ListFlags = (uint)(DosListLockFlags.Read | DosListLockFlags.Devices | DosListLockFlags.Volumes);
    private const uint FindFlags = (uint)(DosListLockFlags.Devices | DosListLockFlags.Volumes);

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead("DRIVE/A,NAME/A", 2, out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }
        var scratch = APTR.Null;
        var scratchBytes = 0u;
        var result = DOS.RETURN_FAIL;
        do
        {
            if (!arguments.TryGetResult(0, out var drive) || drive == 0 || !arguments.TryGetResult(1, out var name) || name == 0)
            { ioError = (int)DOS.Error.BadTemplate; break; }
            if (ContainsColon(APTR.FromPointer(name))) { DOS.PutStr("':' not legal character in volume name\n"); break; }
            var driveLength = Length(APTR.FromPointer(drive));
            if (driveLength == 0 || APTR.ReadUInt8(APTR.FromPointer(drive), unchecked((int)(driveLength - 1))) != (byte)':')
            { DOS.PutStr("Invalid device or volume name\n"); break; }
            scratchBytes = driveLength;
            scratch = Exec.AllocMem(scratchBytes, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (scratch.IsNull) { ioError = (int)DOS.Error.NoFreeStore; break; }
            for (var index = 0u; index + 1 < driveLength; index++) APTR.WriteUInt8(scratch, unchecked((int)index), APTR.ReadUInt8(APTR.FromPointer(drive), unchecked((int)index)));
            var list = DOS.LockDosList(ListFlags);
            var entry = list.IsNull ? APTR.Null : DOS.FindDosEntry(list, CString.FromPointer(scratch.Raw), FindFlags);
            if (list.IsNotNull) DOS.UnLockDosList(ListFlags);
            if (entry.IsNull) { DOS.PutStr("Invalid device or volume name\n"); break; }
            if (DOS.Relabel(CString.FromPointer(drive), CString.FromPointer(name)) == 0)
            { ioError = (int)DOS.IoErr(); DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0)); break; }
            result = DOS.RETURN_OK;
        }
        while (false);
        if (scratch.IsNotNull) Exec.FreeMem(scratch, scratchBytes);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static uint Length(APTR value)
    {
        var length = 0u;
        while (APTR.ReadUInt8(value, unchecked((int)length)) != 0) length++;
        return length;
    }
    private static bool ContainsColon(APTR value)
    {
        for (var index = 0u;; index++) { var current = APTR.ReadUInt8(value, unchecked((int)index)); if (current == 0) return false; if (current == (byte)':') return true; }
    }
}
