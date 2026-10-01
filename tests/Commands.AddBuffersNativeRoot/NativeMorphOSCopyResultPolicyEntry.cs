using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private receipt root for Copy's final result policy.</summary>
public static class NativeMorphOSCopyResultPolicyEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 16 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR, (int)DOS.Error.LineTooLong, workbench);
        var address = control.Address;
        var result = NativeMorphOSCopyResultPolicy.Finalize((int)APTR.ReadUInt32(address, 0),
            (int)APTR.ReadUInt32(address, 4), APTR.ReadUInt32(address, 8));
        APTR.WriteUInt32(control, 12, unchecked((uint)result));
        return NativeCommandStartup.Finish(result, (int)DOS.IoErr(), APTR.Null);
    }
}
