using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.NativeRoot;

/// <summary>
/// Non-shipping command for qualifying actual image entry, DOS vectors,
/// allocation/error ownership, ReadArgs, and shared-SegList execution.
/// Its deliberate result is the VALUE argument, or the entry argument length.
/// This is a test protocol, not an Amiga/MorphOS compatibility command.
/// </summary>
public static class NativeCommandProbe
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(36))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);

        if (workbench.IsNotNull)
            return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, workbench);

        if (argumentLength < 0 || (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);

        if (!NativeCommandArguments.TryRead("VALUE/N,FAIL/S", 2,
            out var arguments))
            return NativeCommandStartup.Finish(arguments.ReturnLevel,
                arguments.IoError, workbench);

        if (!arguments.TryGetResult(0, out var numberPointer) ||
            !arguments.TryGetResult(1, out var failSwitch) ||
            arguments.TryGetResult(2, out _) ||
            arguments.TryGetResult(uint.MaxValue, out _))
        {
            arguments.Release();
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,
                (int)DOS.Error.BadTemplate, workbench);
        }

        var number = APTR.FromPointer(numberPointer);
        var result = number.IsNull ? argumentLength : (int)APTR.ReadUInt32(number, 0);
        var ioError = 0;
        if (failSwitch != 0)
        {
            result = DOS.RETURN_ERROR;
            ioError = (int)DOS.Error.ObjectNotFound;
        }
        else if (argumentLength != 0 &&
            DOS.Write(DOS.Output(), argumentText.Address, argumentLength) != argumentLength)
        {
            result = DOS.RETURN_ERROR;
            ioError = (int)DOS.IoErr();
        }

        arguments.Release();
        arguments.Release(); // A second release must not free anything again.
        if (arguments.IsSuccess || arguments.ResultCount != 0 ||
            arguments.TryGetResult(0, out _))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,
                (int)DOS.Error.BadTemplate, workbench);
        return NativeCommandStartup.Finish(result, ioError, workbench);
    }
}
