using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.MorphOSSetDateNativeRoot;

/// <summary>
/// Private native root for the bounded MorphOS 3.20 SetDate qualification.
/// The source profile's stack records are represented by invocation-owned
/// public storage; original guest and package gates remain separate.
/// </summary>
public static class MorphOSSetDateEntry
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
        var result = NativeMorphOSSetDateCommand.Run(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
