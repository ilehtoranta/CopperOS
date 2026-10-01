using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private resident native entry for the Workbench 3.1 Mount profile.</summary>
public static class NativeWorkbench31MountEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(36))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (argumentLength < 0 ||
            (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);

        var result = workbench.IsNotNull
            ? NativeWorkbench31MountCommand.RunWorkbench(workbench,
                out var ioError)
            : NativeWorkbench31MountCommand.Run(out ioError);
        return NativeCommandStartup.Finish(result, ioError, workbench);
    }
}
