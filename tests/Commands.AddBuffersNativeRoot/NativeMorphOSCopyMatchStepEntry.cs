using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private native root for the combined PatCopy match body.</summary>
public static class NativeMorphOSCopyMatchStepEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull || bytes != 80 || control.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, workbench);
        var c = control.Address;
        var first = APTR.ReadUInt32(c, 40) != 0;
        var depth = unchecked((int)APTR.ReadUInt32(c, 44));
        var current = BPTR.FromRaw(APTR.ReadUInt32(c, 48));
        var flags = APTR.ReadUInt32(c, 52);
        var pathSize = unchecked((int)APTR.ReadUInt32(c, 56));
        var deep = APTR.ReadUInt32(c, 60) != 0;
        var failed = NativeMorphOSCopyMatchStep.Process(
            APTR.FromPointer(APTR.ReadUInt32(c, 0)),
            APTR.FromPointer(APTR.ReadUInt32(c, 4)),
            APTR.FromPointer(APTR.ReadUInt32(c, 8)),
            APTR.FromPointer(APTR.ReadUInt32(c, 12)),
            APTR.FromPointer(c.Raw + 72),
            unchecked((int)APTR.ReadUInt32(c, 16)),
            APTR.ReadUInt32(c, 20) != 0, APTR.ReadUInt32(c, 24) != 0,
            BPTR.FromRaw(APTR.ReadUInt32(c, 28)), APTR.ReadUInt32(c, 32),
            ref first, ref depth, ref current, ref flags, ref pathSize,
            ref deep, out var work);
        APTR.WriteUInt32(c, 40, first ? 1u : 0);
        APTR.WriteUInt32(c, 44, unchecked((uint)depth));
        APTR.WriteUInt32(c, 48, current.Raw);
        APTR.WriteUInt32(c, 52, flags);
        APTR.WriteUInt32(c, 56, unchecked((uint)pathSize));
        APTR.WriteUInt32(c, 60, deep ? 1u : 0);
        APTR.WriteUInt32(c, 64, work ? 1u : 0);
        APTR.WriteUInt32(c, 68, failed ? 1u : 0);
        return NativeCommandStartup.Finish(DOS.RETURN_OK, 0, APTR.Null);
    }
}
