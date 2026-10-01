using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Type profile. The classic five-slot syntax is kept separate
/// from MorphOS NOLINE while sharing the public-DOS text and HEX worker.
/// </summary>
public static class NativeWorkbench31TypeCommand
{
    public const string Template = NativeMorphOSTypeCommand.WorkbenchTemplate;
    public const uint ResultCount = NativeMorphOSTypeCommand.WorkbenchResultCount;

    public static int Run(out int ioError) =>
        NativeMorphOSTypeCommand.RunWorkbench31(out ioError);
}
