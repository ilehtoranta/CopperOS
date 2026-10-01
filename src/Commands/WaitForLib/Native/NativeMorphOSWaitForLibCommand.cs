using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 WaitForLib body. The command polls Exec's public library list
/// without opening or loading the target library, preserving the meaning of
/// waiting for a library to appear and keeping each parser lease invocation
/// owned.
/// </summary>
public static class NativeMorphOSWaitForLibCommand
{
    public const string Template = "LIBNAME/A,I=INTERVAL/K/N,L=LOOP/K/N";
    public const uint ResultCount = 3;

    private const uint TicksPerSecond = 50;
    private const uint CtrlC = 1u << 12;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "WaitForLib");
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_FAIL;
        var error = 0;
        do
        {
            if (!arguments.TryGetResult(0, out var name) || name == 0 ||
                !arguments.TryGetResult(1, out var intervalSlot) ||
                !arguments.TryGetResult(2, out var loopSlot))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            var interval = intervalSlot == 0 ? 1 : ReadSigned(intervalSlot);
            var loops = loopSlot == 0 ? 11 : ReadSigned(loopSlot);
            if (interval < 0 || loops < 0)
            {
                error = (int)DOS.Error.BadNumber;
                break;
            }

            var remaining = loops;
            var libraryList = LibraryList();
            for (;;)
            {
                var found = Exec.FindName(libraryList,
                    CString.FromPointer(name)).IsNotNull;
                if (found)
                {
                    result = DOS.RETURN_OK;
                    break;
                }

                if ((Exec.SetSignal(0u, 0u) & CtrlC) != 0)
                {
                    error = (int)DOS.Error.Break;
                    result = DOS.RETURN_WARN;
                    break;
                }

                if (remaining != 0)
                {
                    remaining--;
                    if (remaining == 0)
                        break;
                }

                if (!MultiplyTicks((uint)interval, out var ticks))
                {
                    error = (int)DOS.Error.ObjectTooLarge;
                    break;
                }
                DOS.Delay(unchecked((int)ticks));
            }
        }
        while (false);

        arguments.Release();
        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static APTR LibraryList()
    {
        var execBase = APTR.FromPointer(
            APTR.ReadUInt32(APTR.FromPointer(4), 0));
        return APTR.FromPointer(execBase.Raw +
            (uint)ExecLayout.ExecBase.LibraryList);
    }

    private static int ReadSigned(uint slot) =>
        unchecked((int)APTR.ReadUInt32(APTR.FromPointer(slot), 0));

    private static bool MultiplyTicks(uint seconds, out uint ticks)
    {
        ticks = 0;
        if (seconds > uint.MaxValue / TicksPerSecond)
            return false;
        ticks = seconds * TicksPerSecond;
        return true;
    }
}
