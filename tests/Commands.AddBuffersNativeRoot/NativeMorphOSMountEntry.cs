using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private resident native entry for the explicit-source Mount path.</summary>
public static class NativeMorphOSMountEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (argumentLength < 0 ||
            (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);

        var result = workbench.IsNotNull
            ? NativeMorphOSMountCommand.RunWorkbenchProfile(
                NativeMorphOSMountCommand.MountTemplate,
                NativeMorphOSMountCommand.MountResultCount, false,
                workbench, out var ioError)
            : NativeMorphOSMountCommand.Run(out ioError);
        return NativeCommandStartup.Finish(result, ioError, workbench);
    }
}
