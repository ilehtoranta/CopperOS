using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private resident entry for the Workbench 3.1 BindDrivers profile.</summary>
public static class NativeWorkbench31BindDriversEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        var result = NativeWorkbench31BindDriversCommand.Run(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, workbench);
    }
}
