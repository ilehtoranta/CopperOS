using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Reboot syntax candidate. The classic image is an empty
/// template command; startup is a separate DOS 36 profile.
/// </summary>
public static class NativeWorkbench31RebootCommand
{
    public const string Template = NativeMorphOSRebootCommand.Template;

    public static int Run(out int ioError) =>
        NativeMorphOSRebootCommand.Run(out ioError);
}
