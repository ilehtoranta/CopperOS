using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 LoadResource's catalog ids, English defaults and DOS formatter.
/// The caller owns a cleared four-byte catalog-state slot and the optional
/// Locale library lease. Begin/End preserve the selected source's lazy-open
/// and non-cleared-pointer behavior, including nested listing closes. Native
/// call-order qualification does not prove the validity of a guest catalog
/// pointer after CloseCatalog or the effects of repeated closes.
/// </summary>
public static class NativeWorkbench31LoadResourceMessages
{
    public const int StateBytes = 4;

    private struct NameArgument
    {
        public uint Name;

        public static APTR AddressOf(ref NameArgument value) =>
            throw new System.NotSupportedException(
                "NameArgument.AddressOf is lowered by CopperSharp.");
    }

    public static void Begin(APTR catalogState)
    {
        if (APTR.ReadUInt32(catalogState, 0) != 0 || Locale.LocaleLibraryBase.IsNull)
            return;
        APTR.WriteUInt32(catalogState, 0,
            Locale.OpenCatalogA(0, CString.FromLiteral("sys/c.catalog"), 0));
    }

    public static void End(APTR catalogState)
    {
        var catalog = APTR.ReadUInt32(catalogState, 0);
        if (catalog != 0)
            Locale.CloseCatalog(catalog);
        // HUNK2 0x0262-0x0276 leaves DATA+0x027c unchanged after CloseCatalog.
        // Do not clear it or synthesize a balanced-reference policy here.
    }

    public static CString Get(APTR catalogState, uint id)
    {
        var fallback = Default(id);
        // The original calls GetCatalogStr even when OpenCatalogA returned
        // null, provided locale.library itself is available.
        return Locale.LocaleLibraryBase.IsNull ? fallback : CString.FromPointer(
            Locale.GetCatalogStr(APTR.ReadUInt32(catalogState, 0), unchecked((int)id), fallback));
    }

    public static void Print(APTR catalogState, uint id, APTR arguments)
    {
        Begin(catalogState);
        DOS.VPrintf(Get(catalogState, id), arguments);
        End(catalogState);
    }

    public static void PrintName(APTR catalogState, uint id, CString name)
    {
        var argument = new NameArgument { Name = CString.ToUInt32(name) };
        Print(catalogState, id, NameArgument.AddressOf(ref argument));
    }

    private static CString Default(uint id)
    {
        if (id == 0xc350) return CString.FromLiteral("Library");
        if (id == 0xc351) return CString.FromLiteral("Device");
        if (id == 0xc352) return CString.FromLiteral("Font");
        if (id == 0xc353) return CString.FromLiteral("Catalog");
        if (id == 0xc354) return CString.FromLiteral("TYPE");
        if (id == 0xc355) return CString.FromLiteral("%-9s%s\n");
        if (id == 0xc356) return CString.FromLiteral("NAME\n");
        if (id == 0xc357) return CString.FromLiteral("No resources currently locked\n");
        if (id == 0xc358) return CString.FromLiteral("'%s' is already a locked resource\n");
        if (id == 0xc359) return CString.FromLiteral("Error while loading '%s' - ");
        if (id == 0xc35a) return CString.FromLiteral("Requires diskfont.library V37 - ");
        if (id == 0xc35b) return CString.FromLiteral("'%s' couldn't be loaded as a resource - ");
        if (id == 0xc35c) return CString.FromLiteral("'%s' is not a locked resource\n");
        if (id == 0xc35d) return CString.FromLiteral("'%s' - ");
        // Id zero is the empty entry. Callers use only the source's known ids.
        return CString.FromLiteral("");
    }
}
