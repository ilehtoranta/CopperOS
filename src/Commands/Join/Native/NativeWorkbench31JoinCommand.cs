using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Join profile. The observed classic syntax matches the
/// MorphOS ReadArgs boundary, so the public-DOS body is shared while startup
/// and profile admission remain separate.
/// </summary>
public static class NativeWorkbench31JoinCommand
{
    public const string Template = NativeMorphOSJoinCommand.Template;
    public const uint ResultCount = NativeMorphOSJoinCommand.ResultCount;

    public static int Run() => NativeMorphOSJoinCommand.Run();
}
