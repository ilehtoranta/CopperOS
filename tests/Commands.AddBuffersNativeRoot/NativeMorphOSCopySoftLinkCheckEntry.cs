using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

public static class NativeMorphOSCopySoftLinkCheckEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 24 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);
        var address = control.Address;
        var enter = NativeMorphOSCopySoftLinkCheck.CanEnter(
            BPTR.FromRaw(APTR.ReadUInt32(address, 0)),
            APTR.FromPointer(APTR.ReadUInt32(address, 4)),
            APTR.ReadUInt32(address, 8) != 0,
            APTR.FromPointer(address.Raw + 16));
        APTR.WriteUInt32(address, 12, enter ? 1u : 0);
        return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, APTR.Null);
    }
}
