using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Entries;

/// <summary>
/// <c>C:Eval</c> with the Workbench 3.1 profile. Same startup sequence as the
/// <c>NativeWorkbench31EvalEntry</c> closure root in tests/Commands.NativeRoot
/// (qualified by tools/Commands/qualify_eval_wb31_native_entry.ps1). For the
/// MorphOS 3.20 superset, call <c>NativeEvalCommand.Run(NativeEvalProfile.MorphOS320, ...)</c>.
/// </summary>
public static class EvalEntry
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

        var result = NativeEvalCommand.RunWorkbench31(out var ioError);
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
