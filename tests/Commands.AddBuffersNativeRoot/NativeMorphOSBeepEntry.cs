using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private resident native entry for the MorphOS Beep command.</summary>
public static class NativeMorphOSBeepEntry
{
    [M68kEntryPoint]
    public static int Main()
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        var result = NativeMorphOSBeepCommand.Run(out _);
        return NativeCommandStartup.FinishWithoutDos(result, workbench);
    }
}
