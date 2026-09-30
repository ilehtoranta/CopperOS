using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Search candidate using its captured eight-slot DOS template
/// and the shared public-DOS traversal body. Exact Workbench behavior remains
/// open pending reference execution.
/// </summary>
public static class NativeWorkbench31SearchCommand
{
    public const string Template =
        "FROM/M,SEARCH/A,ALL/S,NONUM/S,QUIET/S,QUICK/S,FILE/S,PATTERN/S";
    public const uint ResultCount = 8;

    public static int Run(out int ioError) =>
        NativeMorphOSSearchCommand.Run(false, out ioError);
}
