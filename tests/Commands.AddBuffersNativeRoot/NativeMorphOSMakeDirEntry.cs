using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>
/// Private resident entry for the source-informed MorphOS MakeDir candidate.
/// The packed 50.4 command and full guest parity remain separate gates.
/// </summary>
public static class NativeMorphOSMakeDirEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL,
                (int)DOS.Error.InvalidResidentLibrary, workbench);
        if (workbench.IsNotNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, workbench);
        if (argumentLength < 0 ||
            (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, APTR.Null);

        var result = NativeMorphOSMakeDirCommand.Run(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
