using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 DiskChange syntax candidate. The classic image exposes the
/// DEVICE/A boundary; its DOS 36 startup is separate from the MorphOS body.
/// </summary>
public static class NativeWorkbench31DiskChangeCommand
{
    public const string Template = NativeMorphOSDiskChangeCommand.Template;
    public const uint ResultCount = NativeMorphOSDiskChangeCommand.ResultCount;

    public static int Run(out int ioError) =>
        NativeMorphOSDiskChangeCommand.Run(out ioError);
}
