using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private resident native entry for the MorphOS Beep command.</summary>
public static class NativeMorphOSBeepEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        _ = argumentLength;
        _ = argumentText;
        var result = NativeMorphOSBeepCommand.Run(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, workbench);
    }
}
