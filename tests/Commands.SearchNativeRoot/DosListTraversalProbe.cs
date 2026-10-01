using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.SearchNativeRoot;

/// <summary>Private reachability root for DOS list read-lock traversal.</summary>
public static class DosListTraversalProbe
{
    // Control LONGs: kind, DOS base, count, acquired, IoErr, completion marker.
    public const int ControlBytes = 24;

    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        if (argumentLength != ControlBytes || argumentText.IsNull)
            return DOS.RETURN_FAIL;
        var control = argumentText.Address;
        DOS.DOSLibraryBase = APTR.FromPointer(APTR.ReadUInt32(control, 4));
        var result = NativeDosListTraversal.TryCount(
            (DosListLockFlags)APTR.ReadUInt32(control, 0), out var count,
            out var acquired, out var ioError);
        APTR.WriteUInt32(control, 8, count);
        APTR.WriteUInt32(control, 12, acquired ? 1u : 0u);
        APTR.WriteUInt32(control, 16, unchecked((uint)ioError));
        APTR.WriteUInt32(control, 20, 0x444C5450); // DLTP.
        return result;
    }
}
