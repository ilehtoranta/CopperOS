using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Protect profile. The observed classic template matches the
/// MorphOS boundary, so the public-DOS body is shared while startup remains a
/// separate DOS 36 profile.
/// </summary>
public static class NativeWorkbench31ProtectCommand
{
    public const string Template = NativeMorphOSProtectCommand.Template;
    public const uint ResultCount = NativeMorphOSProtectCommand.ResultCount;

    public static int Run(out int ioError) =>
        NativeMorphOSProtectCommand.Run(out ioError);
}
