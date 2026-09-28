using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded MorphOS ChangeTaskPri body.  It uses DOS ReadArgs and the public
/// Exec task vectors, retaining all parser and target state per invocation.
/// MorphOS task-PID lookup is used only when the running Exec advertises the
/// public V50.45 extension. Missing-process diagnostics use the current
/// process error stream when configured, matching the MorphOS source.
/// </summary>
public static class NativeMorphOSChangeTaskPriCommand
{
    public const string Template = "PRI=PRIORITY/A/N,PROCESS/K/N";
    public const uint ResultCount = 2;
    public const string ExtendedHelp =
        "ChangeTaskPri : Change the priority of a CLI task\n" +
        "\tPRI=PRIORITY/A/N  New priority of task\n" +
        "\tPROCESS/K        Optional process number of change\n";

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeMorphOSProcessControlArguments.TryRead(Template, ResultCount,
                ExtendedHelp, out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "ChangeTaskPri");
            return arguments.ReturnLevel;
        }

        var error = 0;
        var result = DOS.RETURN_OK;
        do
        {
            if (!arguments.TryGetResult(0, out var prioritySlot) ||
                !arguments.TryGetResult(1, out var processSlot) ||
                prioritySlot == 0)
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            var priority = unchecked((int)APTR.ReadUInt32(
                APTR.FromPointer(prioritySlot), 0));
            var processNumber = processSlot == 0 ? 0u :
                APTR.ReadUInt32(APTR.FromPointer(processSlot), 0);
            APTR target;
            Exec.Forbid();
            if (processSlot != 0)
            {
                target = DOS.FindCliProc(processNumber);
                if (target.IsNull && SupportsFindTaskByPid())
                    target = FindTaskByPid(processNumber);
            }
            else
            {
                target = Exec.FindTask(CString.FromPointer(0));
            }

            if (target.IsNull)
            {
                Exec.Permit();
                var stream = DOS.Output();
                var currentTask = Exec.FindTask(CString.FromPointer(0));
                if (currentTask.IsNotNull)
                {
                    var currentError = BPTR.FromRaw(APTR.ReadUInt32(
                        currentTask, DosLayout.Process.CurrentError));
                    if (currentError.IsNotNull) stream = currentError;
                }
                DOS.VFPrintf(stream,
                    "ChangeTaskPri: Process %ld does not exist.\n",
                    APTR.FromPointer(processSlot));
                DOS.SetIoErr(DOS.Error.None);
                error = -1;
            }
            else if (priority < -128 || priority > 127)
            {
                error = (int)DOS.Error.ObjectTooLarge;
                Exec.Permit();
            }
            else
            {
                Exec.SetTaskPri(target, unchecked((sbyte)priority));
                Exec.Permit();
            }
        }
        while (false);

        if (error != 0 && error != -1)
        {
            ioError = error;
            DOS.PrintFault((DOS.Error)ioError, "ChangeTaskPri");
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
