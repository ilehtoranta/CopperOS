using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private receipt root for Copy's joined OpenDestDir branch.</summary>
public static class NativeMorphOSCopyOpenDestinationEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 32 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR, (int)DOS.Error.LineTooLong, workbench);
        var address = control.Address;
        var result = NativeMorphOSCopyOpenDestination.Open(
            APTR.FromPointer(APTR.ReadUInt32(address, 0)),
            APTR.FromPointer(APTR.ReadUInt32(address, 4)), APTR.ReadUInt32(address, 8),
            APTR.ReadUInt32(address, 12) != 0, APTR.ReadUInt32(address, 16) != 0,
            out var noFileSystem, out var ioError);
        APTR.WriteUInt32(control, 20, result.Raw);
        APTR.WriteUInt32(control, 24, noFileSystem ? 1u : 0);
        APTR.WriteUInt32(control, 28, unchecked((uint)ioError));
        if (!result.IsNull) DOS.UnLock(result);
        return NativeCommandStartup.Finish(result.IsNull ? DOS.RETURN_FAIL : DOS.RETURN_OK,
            ioError, APTR.Null);
    }
}
