using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 ChangeTaskPri body.  It keeps the classic optional
/// CLI process selection and signed priority range while leaving the
/// MorphOS-only FindTaskByPID extension out of this profile.
/// </summary>
public static class NativeWorkbench31ChangeTaskPriCommand
{
    public const string Template = "PRI=PRIORITY/A/N,PROCESS/K/N";
    public const uint ResultCount = 2;

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

        var error = 0;
        var rangeError = false;
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
            if (priority < -128 || priority > 127)
            {
                // Workbench checks the signed range before resolving PROCESS.
                // Preserve that precedence and avoid task-list protection or
                // lookup for an invalid priority.
                rangeError = true;
            }
            else
            {
                APTR target;
                Exec.Forbid();
                target = processSlot != 0
                    ? DOS.FindCliProc(processNumber)
                    : Exec.FindTask(CString.FromPointer(0));

                if (target.IsNull)
                {
                    Exec.Permit();
                    DOS.VFPrintf(DOS.Output(),
                        "Process %ld does not exist\n",
                        APTR.FromPointer(processSlot));
                    DOS.SetIoErr(DOS.Error.None);
                    error = -1;
                }
                else
                {
                    Exec.SetTaskPri(target, unchecked((sbyte)priority));
                    Exec.Permit();
                }
            }
        }
        while (false);

        if (rangeError)
        {
            DOS.PutStr("Priority out of range (-128 to +127)\n");
            ioError = 0;
            result = DOS.RETURN_FAIL;
            DOS.SetIoErr(DOS.Error.None);
        }
        else if (error != 0 && error != -1)
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
}
