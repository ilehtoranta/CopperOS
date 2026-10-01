using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Entries;

/// <summary>
/// <c>C:Format</c> (MorphOS body). Startup sequence copied verbatim from the
/// qualified <c>NativeMorphOSFormatEntry</c> in tests/Commands.AddBuffersNativeRoot/.
/// Keep the two in step.
/// </summary>
public static class FormatEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, workbench);
        if (argumentLength < 0 ||
            (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, APTR.Null);

        var result = NativeMorphOSFormatCommand.Run(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
