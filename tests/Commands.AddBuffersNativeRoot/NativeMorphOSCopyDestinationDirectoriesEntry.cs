using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private receipt root for Copy's bounded OpenDestDir path.</summary>
public static class NativeMorphOSCopyDestinationDirectoriesEntry
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
        if (controlBytes != 20 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, APTR.Null);
        var address = control.Address;
        var destination = NativeMorphOSCopyDestinationDirectories.Open(
            APTR.FromPointer(APTR.ReadUInt32(address, 0)),
            APTR.ReadUInt32(address, 4) != 0, out var ioError);
        APTR.WriteUInt32(control, 8, destination.Raw);
        APTR.WriteUInt32(control, 12, unchecked((uint)ioError));
        if (!destination.IsNull)
            DOS.UnLock(destination);
        APTR.WriteUInt32(control, 16, APTR.ReadUInt8(APTR.FromPointer(
            APTR.ReadUInt32(address, 0)), 0));
        return NativeCommandStartup.Finish(destination.IsNull ? DOS.RETURN_FAIL :
            DOS.RETURN_OK, ioError, APTR.Null);
    }
}
