using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Resident entry for the AddDataTypes shared-list transaction.</summary>
public static class NativeMorphOSAddDataTypesListEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0,
                workbench);

        if (argumentLength < 0 ||
            (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);

        if (workbench.IsNotNull)
        {
            var result = NativeMorphOSAddDataTypesCommand
                .RunWorkbenchStartup(workbench, out var startupError);
            return NativeCommandStartup.Finish(result, startupError,
                workbench);
        }

        var resultLevel = NativeMorphOSAddDataTypesCommand.Run(
            out var ioError);
        return NativeCommandStartup.Finish(resultLevel, ioError, workbench);
    }
}
