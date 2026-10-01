using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Exe2ArcNativeRoot;

/// <summary>
/// Source-linked resident entry for the MorphOS Exe2Arc owner. The input is
/// parsed by the command through DOS ReadArgs; this entry only owns the common
/// Workbench message, DOS base, and final teardown boundary.
/// </summary>
public static class Exe2ArcEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull)
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (argumentLength < 0 ||
            (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, APTR.Null);

        var dosBase = DOS.DOSLibraryBase;
        var result = NativeMorphOSExe2ArcCommand.Run(dosBase, out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
