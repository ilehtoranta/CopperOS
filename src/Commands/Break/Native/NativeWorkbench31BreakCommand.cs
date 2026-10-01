using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 Break body.  The classic profile has a mandatory
/// numeric CLI process and no MorphOS public-port or PID extensions.  DOS
/// owns the ReadArgs result storage for the duration of target resolution;
/// the Exec target lookup and Signal are protected by Forbid/Permit.
/// </summary>
public static class NativeWorkbench31BreakCommand
{
    public const string Template = "PROCESS/A/N,ALL/S,C/S,D/S,E/S,F/S";
    public const uint ResultCount = 6;

    private const uint CtrlC = 1u << 12;
    private const uint CtrlD = 1u << 13;
    private const uint CtrlE = 1u << 14;
    private const uint CtrlF = 1u << 15;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            // Workbench 3.1's command prints the raw ReadArgs fault, without
            // a command-name prefix, and returns RETURN_FAIL (20).
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_OK;
        var error = 0;
        do
        {
            if (!arguments.TryGetResult(0, out var processSlot) ||
                !arguments.TryGetResult(1, out var all) ||
                !arguments.TryGetResult(2, out var c) ||
                !arguments.TryGetResult(3, out var d) ||
                !arguments.TryGetResult(4, out var e) ||
                !arguments.TryGetResult(5, out var f) ||
                processSlot == 0)
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            var processNumber = APTR.ReadUInt32(
                APTR.FromPointer(processSlot), 0);
            APTR target;
            Exec.Forbid();
            target = DOS.FindCliProc(processNumber);
            if (target.IsNotNull)
            {
                var mask = all != 0 ? CtrlC | CtrlD | CtrlE | CtrlF :
                    (c != 0 ? CtrlC : 0) |
                    (d != 0 ? CtrlD : 0) |
                    (e != 0 ? CtrlE : 0) |
                    (f != 0 ? CtrlF : 0);
                if (mask == 0)
                    mask = CtrlC;
                Exec.Signal(target, mask);
                Exec.Permit();
            }
            else
            {
                Exec.Permit();
                DOS.VFPrintf(DOS.Output(),
                    "Process %ld does not exist\n",
                    APTR.FromPointer(processSlot));
                error = -1;
            }
        }
        while (false);

        if (error != 0 && error != -1)
        {
            ioError = error;
            DOS.PrintFault((DOS.Error)ioError, "Break");
            result = DOS.RETURN_FAIL;
        }
        else
        {
            result = error == 0 ? DOS.RETURN_OK : DOS.RETURN_FAIL;
            ioError = 0;
            DOS.SetIoErr(DOS.Error.None);
        }

        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }
}
