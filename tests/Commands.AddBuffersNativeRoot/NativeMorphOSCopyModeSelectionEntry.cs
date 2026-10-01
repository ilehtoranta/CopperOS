using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private control-block root for Copy mode-selection static proof.</summary>
public static class NativeMorphOSCopyModeSelectionEntry
{
    [M68kEntryPoint]
    public static int Main(int bytes, CONST_STRPTR control)
    {
        if (bytes != 12 || control.IsNull) return DOS.RETURN_FAIL;
        var p = control.Address;
        var valid = NativeMorphOSCopyModeSelection.TrySelect(APTR.ReadUInt32(p, 0),
            unchecked((int)APTR.ReadUInt32(p, 4)), out var mode, out var flags);
        APTR.WriteUInt32(control, 0, valid ? 1u : 0u);
        APTR.WriteUInt32(control, 4, unchecked((uint)mode));
        APTR.WriteUInt32(control, 8, flags);
        return valid ? DOS.RETURN_OK : DOS.RETURN_FAIL;
    }
}
