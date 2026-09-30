using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Dir profile wrapper. The inspected classic binary exposes
/// the same six-slot grammar candidate; the bounded public-DOS worker remains
/// shared until original guest behavior separates the profiles.
/// </summary>
public static class NativeWorkbench31DirCommand
{
    public const string Template =
        "DIR,OPT/K,ALL/S,DIRS/S,FILES/S,INTER/S";
    public const uint ResultCount = 6;

    public static int Run(out int ioError) =>
        NativeMorphOSDirCommand.Run(false, Template, ResultCount, out ioError);
}
