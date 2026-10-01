using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded MorphOS Break command body.  It keeps the ReadArgs lease and all
/// target resolution state invocation-local and uses the public Exec/DOS
/// process and port vectors.  MorphOS task-PID lookup is used only when the
/// running Exec advertises the public V50.45 extension.
/// </summary>
public static class NativeMorphOSBreakCommand
{
    public const string Template = "PROCESS/N,PORT,ALL/S,C/S,D/S,E/S,F/S";
    public const uint ResultCount = 7;
    public const string ExtendedHelp =
        "Break : Set the attention flags of a DOS task\n" +
        "\tPROCESS/N  Process to set flags for\n" +
        "\tPORT       Portname for the Process to set flags for\n" +
        "\tALL/S      Set ALL attention flags\n" +
        "\tC/S        Set the CTRL-C flag\n" +
        "\tD/S        Set the CTRL-D flag\n" +
        "\tE/S        Set the CTRL-E flag\n" +
        "\tF/S        Set the CTRL-F flag\n";

    private const uint CtrlC = 1u << 12;
    private const uint CtrlD = 1u << 13;
    private const uint CtrlE = 1u << 14;
    private const uint CtrlF = 1u << 15;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeMorphOSProcessControlArguments.TryRead(Template, ResultCount,
                ExtendedHelp, out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "Break");
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_OK;
        var error = 0;
        var errStream = DOS.Output();
        do
        {
            if (!arguments.TryGetResult(0, out var processSlot) ||
                !arguments.TryGetResult(1, out var port) ||
                !arguments.TryGetResult(2, out var all) ||
                !arguments.TryGetResult(3, out var c) ||
                !arguments.TryGetResult(4, out var d) ||
                !arguments.TryGetResult(5, out var e) ||
                !arguments.TryGetResult(6, out var f))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            var processNumber = processSlot == 0 ? 0u :
                APTR.ReadUInt32(APTR.FromPointer(processSlot), 0);
            APTR target = APTR.Null;
            Exec.Forbid();
            if (processSlot != 0 && processNumber != 0)
            {
                target = DOS.FindCliProc(processNumber);
                if (target.IsNull && SupportsFindTaskByPid())
                    target = FindTaskByPid(processNumber);
            }
            else if (port != 0)
            {
                var messagePort = Exec.FindPort(CString.FromPointer(port));
                if (messagePort.IsNotNull)
                    target = APTR.FromPointer(APTR.ReadUInt32(messagePort,
                        ExecLayout.MsgPort.SignalTask));
            }

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
                if (processSlot != 0)
                    DOS.VFPrintf(errStream,
                        "Break: Process %ld does not exist.\n",
                        APTR.FromPointer(processSlot));
                else if (port != 0)
                    DOS.VFPrintf(errStream,
                        "Break: Port \"%s\" does not exist.\n",
                        APTR.FromPointer(port));
                else
                    DOS.FPuts(errStream,
                        "Break: Either PROCESS or PORT is required.\n");
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
            // The source uses -1 as an internal "target missing" marker,
            // then deliberately clears IoErr while still returning FAIL.
            ioError = 0;
            DOS.SetIoErr(DOS.Error.None);
        }

        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool SupportsFindTaskByPid()
    {
        var execBase = APTR.FromPointer(APTR.ReadUInt32(
            APTR.FromPointer(4), 0));
        var version = APTR.ReadUInt16(execBase, ExecLayout.Library.Version);
        var revision = APTR.ReadUInt16(execBase, ExecLayout.Library.Revision);
        return version > 50 || version == 50 && revision >= 45;
    }

    private static APTR FindTaskByPid(uint processId)
    {
        var execBase = APTR.FromPointer(APTR.ReadUInt32(
            APTR.FromPointer(4), 0));
        var function = APTR.FromPointer(APTR.ReadUInt32(execBase,
            ExecLvo.FindTaskByPID));
        return function.IsNull ? APTR.Null : Exec.FindTaskByPIDIndirect(
            function, execBase, processId);
    }
}
