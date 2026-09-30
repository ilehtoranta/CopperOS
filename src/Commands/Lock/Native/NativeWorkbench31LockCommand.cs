using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Lock syntax candidate. The observed classic template matches
/// the MorphOS public-DOS boundary; startup and qualification remain a
/// separate DOS 36 profile until original guest behavior is captured.
/// </summary>
public static class NativeWorkbench31LockCommand
{
    public const string Template = NativeMorphOSLockCommand.Template;
    public const uint ResultCount = NativeMorphOSLockCommand.ResultCount;

    public static int Run(out int ioError) =>
        NativeMorphOSLockCommand.Run(out ioError);
}
