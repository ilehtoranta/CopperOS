using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private receipt root for Copy's APF_DIDDIR transition.</summary>
public static class NativeMorphOSCopyDirectoryExitEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 44 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);
        var address = control.Address;
        var depth = unchecked((int)APTR.ReadUInt32(address, 8));
        var current = new BPTR(APTR.ReadUInt32(address, 16));
        var commandFlags = 1u;
        var pathSize = 99;
        NativeMorphOSCopyDirectoryExit.ProcessMatched(
            APTR.FromPointer(APTR.ReadUInt32(address, 0)),
            APTR.FromPointer(APTR.ReadUInt32(address, 0) + (uint)DosLayout.AnchorPath.Info),
            unchecked((int)APTR.ReadUInt32(address, 4)), ref depth,
            new BPTR(APTR.ReadUInt32(address, 12)), ref current,
            ref commandFlags, ref pathSize, NativeMorphOSCopyMetadata.Protection,
            out var dispatch, out var parentFailed);
        APTR.WriteUInt32(control, 20, current.Raw);
        APTR.WriteUInt32(control, 24, unchecked((uint)depth));
        APTR.WriteUInt32(control, 28, dispatch ? 1u : 0);
        APTR.WriteUInt32(control, 32, parentFailed ? 1u : 0);
        APTR.WriteUInt32(control, 36, commandFlags);
        APTR.WriteUInt32(control, 40, unchecked((uint)pathSize));
        return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, APTR.Null);
    }
}
