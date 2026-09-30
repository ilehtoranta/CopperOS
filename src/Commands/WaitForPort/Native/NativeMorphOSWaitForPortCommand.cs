using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 WaitForPort body.  The command checks a named Exec message
/// port at the requested interval and returns OK when the requested presence
/// state is observed.  All parser and timing state is invocation owned.
/// </summary>
public static class NativeMorphOSWaitForPortCommand
{
    public const string Template =
        "PORTNAME/A,I=INTERVAL/K/N,L=LOOP/K/N,D=DISAPPEAR/S";
    public const uint ResultCount = 4;

    private const uint TicksPerSecond = 50;
    private const uint CtrlC = 1u << 12;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "WaitForPort");
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_FAIL;
        var error = 0;
        do
        {
            if (!arguments.TryGetResult(0, out var name) || name == 0 ||
                !arguments.TryGetResult(1, out var intervalSlot) ||
                !arguments.TryGetResult(2, out var loopSlot) ||
                !arguments.TryGetResult(3, out var disappear))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            var interval = intervalSlot == 0 ? 1 : ReadSigned(intervalSlot);
            var loops = loopSlot == 0 ? 10 : ReadSigned(loopSlot);
            if (interval < 0 || loops < 0)
            {
                error = (int)DOS.Error.BadNumber;
                break;
            }

            // LOOP=0 is the source-documented unbounded form.  It is only
            // terminated by finding the requested state or Ctrl-C.
            var remaining = loops;
            for (;;)
            {
                var present = Exec.FindPort(CString.FromPointer(name)).IsNotNull;
                if ((disappear == 0 && present) ||
                    (disappear != 0 && !present))
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
