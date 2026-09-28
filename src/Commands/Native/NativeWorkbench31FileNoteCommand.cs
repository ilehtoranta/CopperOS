using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Filenote profile entry. The bounded public-DOS body is shared
/// with MorphOS; the startup wrapper and qualification suite remain separate
/// so DOS version and Workbench message ownership are explicit.
/// </summary>
public static class NativeWorkbench31FileNoteCommand
{
    public const string Template = NativeMorphOSFileNoteCommand.Template;
    public const uint ResultCount = NativeMorphOSFileNoteCommand.ResultCount;

    public static int Run(out int ioError) =>
        NativeMorphOSFileNoteCommand.Run(out ioError);
}
