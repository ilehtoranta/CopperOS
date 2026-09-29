using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Resident entry for the classic AddDataTypes profile.</summary>
public static class NativeWorkbench31AddDataTypesEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0,
                workbench);

        if (argumentLength < 0 ||
            (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);

        var result = workbench.IsNotNull
            ? NativeWorkbench31AddDataTypesCommand.RunWorkbenchStartup(
                workbench, out var ioError)
            : NativeWorkbench31AddDataTypesCommand.Run(out ioError);
        return NativeCommandStartup.Finish(result, ioError, workbench);
    }
}
