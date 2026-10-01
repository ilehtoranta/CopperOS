using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Entries;

/// <summary>
/// <c>C:Beep</c> (MorphOS body). Startup sequence copied verbatim from the
/// qualified <c>NativeMorphOSBeepEntry</c> in tests/Commands.AddBuffersNativeRoot/.
/// Keep the two in step.
/// </summary>
public static class BeepEntry
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
