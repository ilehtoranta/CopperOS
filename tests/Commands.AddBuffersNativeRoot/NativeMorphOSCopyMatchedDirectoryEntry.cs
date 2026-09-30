using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

public static class NativeMorphOSCopyMatchedDirectoryEntry
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
        var flags = 1u;
        NativeMorphOSCopyDirectoryEntry.ProcessMatched(
            APTR.FromPointer(address.Raw + 160), false, true,
            APTR.ReadUInt32(address, 8) != 0,
            APTR.FromPointer(address.Raw + 16), ref flags,
            out var dispatch, out var deep);
        APTR.WriteUInt32(address, 12, deep ? 1u : 0);
        APTR.WriteUInt32(address, 24, dispatch ? 1u : 0);
        return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, APTR.Null);
    }
}
