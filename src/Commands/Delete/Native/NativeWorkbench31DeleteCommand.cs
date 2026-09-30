using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Delete profile. The classic four-slot boundary is kept
/// separate from MorphOS while the matcher, protection and deletion worker is
/// shared. FOLLOWLINKS remains MorphOS-only until the classic binary proves it.
/// </summary>
public static class NativeWorkbench31DeleteCommand
{
    public const string Template = NativeMorphOSDeleteCommand.WorkbenchTemplate;
    public const uint ResultCount = NativeMorphOSDeleteCommand.WorkbenchResultCount;

    public static int Run() => NativeMorphOSDeleteCommand.RunWorkbench31();
}
