using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.NativeRoot;

/// <summary>
/// Private native closure root for the MorphOS-profile Eval body. It is not a
/// shipping file, a native execution fixture, or evidence of command parity.
/// </summary>
public static class NativeEvalEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(36))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, workbench);

        var result = NativeEvalCommand.Run(NativeEvalProfile.MorphOS320,
            out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
