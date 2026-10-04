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
    public static int Main()
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        var result = NativeMorphOSBeepCommand.Run(out _);
        return NativeCommandStartup.FinishWithoutDos(result, workbench);
    }
}
