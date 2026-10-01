using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.LoadResourceNativeRoot;

/// <summary>Qualification-only driver with supplied library leases and worker storage.</summary>
public static class NativeWorkbench31LoadResourceRegistryProbe
{
    [M68kEntryPoint]
    public static int Main(int scenario, CONST_STRPTR context)
    {
        Utility.UtilityLibraryBase = APTR.FromPointer(0x8000);
        Graphics.GraphicsLibraryBase = APTR.FromPointer(0x9000);
        Locale.LocaleLibraryBase = APTR.FromPointer(0xa000);
        var list = context.Address;
        NativeWorkbench31LoadResourceRegistry.Initialize(list);
        if (!NativeWorkbench31LoadResourceRegistry.IsEmpty(list) ||
            NativeWorkbench31LoadResourceRegistry.Find(list, CString.FromLiteral("absent")).IsNotNull)
            return 101;
        if (scenario == 0)
            return 0;

        var firstName = CString.FromPointer(list.Raw + 32);
        if (scenario == 2)
        {
            if (NativeWorkbench31LoadResourceRegistry.TryAdd(list, firstName, 0,
                APTR.FromPointer(0x1234)) || !NativeWorkbench31LoadResourceRegistry.IsEmpty(list))
                return 102;
        }
        var type = scenario == 4 ? (byte)2 : scenario == 5 ? (byte)3 :
            scenario == 6 ? (byte)1 : scenario == 7 ? (byte)255 : (byte)0;
        if (!NativeWorkbench31LoadResourceRegistry.TryAdd(list, firstName, type,
            APTR.FromPointer(0x1234)))
            return 103;
        var first = APTR.FromPointer(APTR.ReadUInt32(list, 0));
        if (NativeWorkbench31LoadResourceRegistry.IsEmpty(list))
            return 104;
        APTR.WriteUInt8(APTR.FromPointer(CString.ToUInt32(firstName)), 0, (byte)'X');
        if (NativeWorkbench31LoadResourceRegistry.Find(list,
            CString.FromLiteral("ALPHA.LIBRARY")).Raw != first.Raw)
            return 105;
        if (NativeWorkbench31LoadResourceRegistry.Find(list,
            CString.FromLiteral("absent")).IsNotNull)
            return 106;

        if (scenario == 1 || scenario == 9)
        {
            var secondName = scenario == 9 ? CString.FromLiteral("alpha.library") :
                CString.FromLiteral("Beta.font");
            if (!NativeWorkbench31LoadResourceRegistry.TryAdd(list, secondName, 2,
                APTR.FromPointer(0x2345)))
                return 107;
            var second = APTR.FromPointer(APTR.ReadUInt32(first, 0));
            if (APTR.ReadUInt32(list, 0) != first.Raw ||
                APTR.ReadUInt32(list, 8) != second.Raw ||
                NativeWorkbench31LoadResourceRegistry.Find(list, secondName).Raw !=
                    (scenario == 9 ? first.Raw : second.Raw))
                return 108;
            NativeWorkbench31LoadResourceRegistry.Remove(first);
            if (NativeWorkbench31LoadResourceRegistry.Find(list, secondName).Raw != second.Raw)
                return 109;
            NativeWorkbench31LoadResourceRegistry.CloseAndRemove(second);
            if (!NativeWorkbench31LoadResourceRegistry.IsEmpty(list))
                return 110;
            if (!NativeWorkbench31LoadResourceRegistry.TryAdd(list,
                CString.FromLiteral("Reuse.catalog"), 3, APTR.FromPointer(0x3456)))
                return 111;
            NativeWorkbench31LoadResourceRegistry.CloseAndRemove(
                APTR.FromPointer(APTR.ReadUInt32(list, 0)));
        }
        else
            NativeWorkbench31LoadResourceRegistry.CloseAndRemove(first);

        return NativeWorkbench31LoadResourceRegistry.IsEmpty(list) &&
            APTR.ReadUInt32(list, 8) == list.Raw ? 0 : 112;
    }
}
