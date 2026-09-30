using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>
/// Private resident probe for the source-derived PRODUCT parser used by
/// MorphOS BindDrivers. Provider results are empty so the supplied fixture can
/// inspect every requested manufacturer/product pair without loading drivers.
/// </summary>
public static class NativeMorphOSBindDriversProductParserEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var expansion = Exec.OpenLibraryRaw(Expansion.Name, 37);
        if (expansion.IsNull) return DOS.RETURN_FAIL;
        Expansion.ExpansionLibraryBase = expansion;

        NativeMorphOSBindDriversCommand.GetConfigDev(
            APTR.FromPointer(CString.ToUInt32(
                "prefix=514/2|  +33/-4tail|invalid")));
        NativeMorphOSBindDriversCommand.GetConfigDev(
            APTR.FromPointer(CString.ToUInt32("-12x/+7suffix")));
        NativeMorphOSBindDriversCommand.GetConfigDev(
            APTR.FromPointer(CString.ToUInt32("123")));
        NativeMorphOSBindDriversCommand.GetConfigDev(
            APTR.FromPointer(CString.ToUInt32("prefix=11/22|33/44")));
        NativeMorphOSBindDriversCommand.GetConfigDev(
            APTR.FromPointer(CString.ToUInt32("")));

        Exec.CloseLibrary(expansion);
        Expansion.ExpansionLibraryBase = APTR.Null;
        return DOS.RETURN_OK;
    }
}
