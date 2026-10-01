using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private receipt root for Copy's directory-entry transition.</summary>
public static class NativeMorphOSCopyDirectoryEntryEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 20 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);
        var address = control.Address;
        NativeMorphOSCopyDirectoryEntry.Process(
            APTR.FromPointer(APTR.ReadUInt32(address, 0)),
            APTR.ReadUInt32(address, 4) != 0, APTR.ReadUInt32(address, 8) != 0,
            APTR.ReadUInt32(address, 12) != 0,
            out var dispatch, out var deep);
        APTR.WriteUInt32(control, 16, (dispatch ? 1u : 0) | (deep ? 2u : 0));
        return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, APTR.Null);
    }
}
