using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private control-block root for Copy's bounded TestDest stage.</summary>
public static class NativeMorphOSCopyDestinationEntry
{
    [M68kEntryPoint]
    public static int Main(int controlBytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, workbench);
        if (controlBytes != 24 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, APTR.Null);
        var address = control.Address;
        var result = NativeMorphOSCopyDestination.Prepare(
            APTR.FromPointer(APTR.ReadUInt32(address, 0)),
            APTR.ReadUInt32(address, 4) != 0,
            APTR.ReadUInt32(address, 8) != 0,
            APTR.ReadUInt32(address, 12) != 0, out var ioError);
        APTR.WriteUInt32(control, 16, unchecked((uint)result));
        APTR.WriteUInt32(control, 20, unchecked((uint)ioError));
        return NativeCommandStartup.Finish(result == NativeMorphOSCopyDestination.Error
            ? DOS.RETURN_FAIL : DOS.RETURN_OK, ioError, APTR.Null);
    }
}
