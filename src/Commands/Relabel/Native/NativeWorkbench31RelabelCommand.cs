using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench Relabel 37.2 public-vector behavior. All writable storage is
/// invocation-owned; startup and DOS library ownership belong to the caller.
/// </summary>
public static class NativeWorkbench31RelabelCommand
{
    public const string Template = "DRIVE/A,NAME/A";
    public const uint ResultCount = 2;

    private const uint FindFlags = (uint)(DosListLockFlags.Devices | DosListLockFlags.Volumes | DosListLockFlags.Assigns);
    private const uint LockFlags = FindFlags | (uint)DosListLockFlags.Read;

    public static int Run(out int ioError)
    {
        ioError = 0;
        var slots = Exec.AllocMem(8, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (slots.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            DOS.PrintFault(DOS.Error.NoFreeStore, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_FAIL;
        var parsed = DOS.ReadArgs(Template, slots, APTR.Null);
        if (parsed.IsNull)
            DOS.PrintFault(DOS.IoErr(), CString.FromPointer(0));
        else
        {
            var drive = APTR.FromPointer(APTR.ReadUInt32(slots, 0));
            var name = APTR.FromPointer(APTR.ReadUInt32(slots, 4));
            var colon = false;
            for (var index = 0; APTR.ReadUInt8(name, index) != 0; index++)
                if (APTR.ReadUInt8(name, index) == (byte)':') colon = true;
            if (colon)
                DOS.PutStr("':' not legal character in volume name\n");
            else
            {
                var length = 0;
                while (APTR.ReadUInt8(drive, length) != 0) length++;
                // The classic command removes the last byte, even when it is
                // not a colon. Use our own buffer rather than modifying the
                // parser's storage or reproducing its empty-string underwrite.
                var lookupLength = length == 0 ? 0 : length - 1;
                var bytes = unchecked((uint)(lookupLength + 2));
                var scratch = Exec.AllocMem(bytes, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
                if (scratch.IsNull)
                {
                    DOS.SetIoErr(DOS.Error.NoFreeStore);
                    DOS.PrintFault(DOS.Error.NoFreeStore, CString.FromPointer(0));
                }
                else
                {
                    for (var index = 0; index < lookupLength; index++)
                        APTR.WriteUInt8(scratch, index, APTR.ReadUInt8(drive, index));
                    var list = DOS.LockDosList(LockFlags);
                    var entry = DOS.FindDosEntry(list, CString.FromPointer(scratch.Raw), FindFlags);
                    DOS.UnLockDosList(LockFlags);
                    if (entry.IsNull)
                        DOS.PutStr("Invalid device or volume name\n");
                    else
                    {
                        APTR.WriteUInt8(scratch, lookupLength, (byte)':');
                        if (DOS.Relabel(CString.FromPointer(scratch.Raw), CString.FromPointer(name.Raw)) == 0)
                            DOS.PrintFault(DOS.IoErr(), CString.FromPointer(0));
                        else result = DOS.RETURN_OK;
                    }
                    // Protect DOS's ambient error from the extra Exec cleanup.
                    var savedError = DOS.IoErr();
                    Exec.FreeMem(scratch, bytes);
                    DOS.SetIoErr(savedError);
                }
            }
            DOS.FreeArgs(parsed);
        }
        // Original FreeArgs is allowed to change the final process error.
        ioError = (int)DOS.IoErr();
        Exec.FreeMem(slots, 8);
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }
}
