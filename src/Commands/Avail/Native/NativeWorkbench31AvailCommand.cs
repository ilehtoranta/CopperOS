using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 Avail body.  The original v40 command reports the
/// classic Exec memory classes through AvailMem; parser, startup and teardown
/// remain owned by the command entry.
/// </summary>
public static class NativeWorkbench31AvailCommand
{
    private const uint FormatBytes = 20;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead("CHIP/S,FAST/S,TOTAL/S,FLUSH/S", 4,
                out var arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        var format = APTR.Null;
        var result = DOS.RETURN_OK;
        do
        {
            if (!arguments.TryGetResult(0, out var chip) ||
                !arguments.TryGetResult(1, out var fast) ||
                !arguments.TryGetResult(2, out var total) ||
                !arguments.TryGetResult(3, out var flush))
            {
                ioError = (int)DOS.Error.BadTemplate;
                result = DOS.RETURN_FAIL;
                break;
            }

            // FLUSH is intentionally fail-closed until the versioned command
            // contract identifies the exact expunge ordering.  Returning a
            // false success would make the bounded body unsafe to promote.
            if (flush != 0)
            {
                ioError = (int)DOS.Error.NotImplemented;
                result = DOS.RETURN_FAIL;
                break;
            }

            if (chip != 0 || fast != 0 || total != 0)
            {
                var flags = chip != 0 ? Exec.MemoryFlags.Chip :
                    fast != 0 ? Exec.MemoryFlags.Fast : Exec.MemoryFlags.Any;
                var available = Exec.AvailMem(flags);
                var bytes = APTR.Null;
                bytes = Exec.AllocMem(4, Exec.MemoryFlags.Public |
                    Exec.MemoryFlags.Clear);
                if (bytes.IsNull)
                {
                    ioError = (int)DOS.Error.NoFreeStore;
                    result = DOS.RETURN_FAIL;
                    break;
                }
                APTR.WriteUInt32(bytes, 0, available);
                DOS.VPrintf("%ld\n", bytes);
                Exec.FreeMem(bytes, 4);
                break;
            }

            format = Exec.AllocMem(FormatBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (format.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }

            DOS.PutStr("Type Available In-Use Maximum Largest\n");
            if (!WriteRow(format, "chip", Exec.MemoryFlags.Chip) ||
                !WriteRow(format, "fast", Exec.MemoryFlags.Fast) ||
                !WriteRow(format, "total", Exec.MemoryFlags.Any))
            {
                ioError = (int)DOS.IoErr();
                result = DOS.RETURN_FAIL;
            }
        }
        while (false);

        if (format.IsNotNull) Exec.FreeMem(format, FormatBytes);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool WriteRow(APTR format, CString type,
        Exec.MemoryFlags flags)
    {
        var available = Exec.AvailMem(flags);
        var maximum = Exec.AvailMem(flags | Exec.MemoryFlags.Total);
        var largest = Exec.AvailMem(flags | Exec.MemoryFlags.Largest);
        APTR.WriteUInt32(format, 0, CString.ToUInt32(type));
        APTR.WriteUInt32(format, 4, available);
        APTR.WriteUInt32(format, 8, unchecked(maximum - available));
        APTR.WriteUInt32(format, 12, maximum);
        APTR.WriteUInt32(format, 16, largest);
        return DOS.VPrintf("%s %ld %ld %ld %ld\n", format) >= 0;
    }
}
