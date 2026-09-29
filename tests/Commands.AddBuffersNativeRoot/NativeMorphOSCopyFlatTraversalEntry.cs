using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private receipt root for Copy's flat PatCopy matcher lane.</summary>
public static class NativeMorphOSCopyFlatTraversalEntry
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
        var matcherResult = NativeMorphOSCopyFlatTraversal.Capture(
            CString.FromPointer(APTR.ReadUInt32(address, 0)),
            APTR.FromPointer(APTR.ReadUInt32(address, 4)),
            APTR.FromPointer(APTR.ReadUInt32(address, 8)),
            APTR.ReadUInt32(address, 12), out var delivered);
        APTR.WriteUInt32(control, 16, unchecked((uint)matcherResult));
        APTR.WriteUInt32(control, 20, delivered);
        return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, APTR.Null);
    }
}
