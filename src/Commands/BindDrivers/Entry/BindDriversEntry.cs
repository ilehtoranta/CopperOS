using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Entries;

/// <summary>
/// <c>C:BindDrivers</c> (Workbench 3.1 body). Startup sequence copied verbatim from the
/// qualified <c>NativeWorkbench31BindDriversEntry</c> in tests/Commands.AddBuffersNativeRoot/.
/// Keep the two in step.
/// </summary>
public static class BindDriversEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        var result = NativeWorkbench31BindDriversCommand.Run(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, workbench);
    }
}
