using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private receipt root for Copy's IsMatchPattern helper.</summary>
public static class NativeMorphOSCopyPatternClassifierEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 12 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);
        var address = control.Address;
        var result = NativeMorphOSCopyPatternClassifier.IsMatchPattern(
            CString.FromPointer(APTR.ReadUInt32(address, 0)),
            APTR.FromPointer(APTR.ReadUInt32(address, 4)));
        APTR.WriteUInt32(control, 8, unchecked((uint)result));
        return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, APTR.Null);
    }
}
