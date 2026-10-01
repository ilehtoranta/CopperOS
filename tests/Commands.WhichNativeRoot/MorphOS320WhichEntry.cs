using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.WhichNativeRoot;

/// <summary>
/// Private native entry for the bounded MorphOS Which profile. It keeps the
/// MorphOS alias slots distinct while sharing the public DOS lookup owner.
/// </summary>
public static class MorphOS320WhichEntry
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

        var result = Workbench31WhichCommand.RunMorphOS(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
