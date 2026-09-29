using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private receipt root for Copy's non-filesystem destination fallback.</summary>
public static class NativeMorphOSCopyNonFileSystemDestinationEntry
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
        if (controlBytes != 28 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, APTR.Null);
        var address = control.Address;
        var result = NativeMorphOSCopyNonFileSystemDestination.TryOpen(
            APTR.FromPointer(APTR.ReadUInt32(address, 0)),
            APTR.FromPointer(APTR.ReadUInt32(address, 4)), APTR.ReadUInt32(address, 8),
            APTR.ReadUInt32(address, 12) != 0, out var noFileSystem, out var ioError);
        APTR.WriteUInt32(control, 16, result.Raw);
        APTR.WriteUInt32(control, 20, noFileSystem ? 1u : 0);
        APTR.WriteUInt32(control, 24, unchecked((uint)ioError));
        if (!result.IsNull)
            DOS.UnLock(result);
        return NativeCommandStartup.Finish(result.IsNull ? DOS.RETURN_FAIL :
            DOS.RETURN_OK, ioError, APTR.Null);
    }
}
