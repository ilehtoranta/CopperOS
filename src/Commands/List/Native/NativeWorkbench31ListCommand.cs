using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 List profile using the shared public-DOS matcher path. The
/// shorter template's ALL slot is mapped separately from the MorphOS template;
/// exact guest parity, sorting, owners and LFORMAT remain open.
/// </summary>
public static class NativeWorkbench31ListCommand
{
    public const string Template =
        "DIR/M,P=PAT/K,KEYS/S,DATES/S,NODATES/S,TO/K,SUB/K,SINCE/K,UPTO/K,QUICK/S,BLOCK/S,NOHEAD/S,FILES/S,DIRS/S,LFORMAT/K,ALL/S";
    public const uint ResultCount = 16;

    public static int Run(out int ioError) =>
        NativeMorphOSListCommand.Run(Template, ResultCount, out ioError);
}
