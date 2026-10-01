using Amiga;
using CopperOS.Commands.Native;
using CopperSharp.Compiler;

namespace CopperOS.Commands.AddBuffersNativeRoot;

/// <summary>Private resident native entry for the MorphOS Info command.</summary>
public static class NativeMorphOSInfoEntry
{
    [M68kEntryPoint]
    public static int Main(int argumentLength, CONST_STRPTR argumentText)
    {
        var workbench = NativeCommandStartup.ReceiveWorkbenchMessage();
        if (!NativeCommandStartup.OpenDos(37))
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, workbench);
        if (workbench.IsNotNull)
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, workbench);
        if (argumentLength < 0 ||
            (argumentLength != 0 && argumentText.IsNull))
            return NativeCommandStartup.Finish(DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, APTR.Null);

        // MorphOS Info opens these providers before entering doInfo(). The
        // locale library is optional in the source, and OpenLocale failure
        // does not suppress the command; CloseLocale is still paired whenever
        // locale.library itself was acquired.
        var utility = Exec.OpenLibraryRaw(Utility.Name, 37);
        if (utility.IsNull)
            return NativeCommandStartup.Finish(DOS.RETURN_FAIL, 0, APTR.Null);
        Utility.UtilityLibraryBase = utility;

        var localeLibrary = Exec.OpenLibraryRaw(Locale.Name, 38);
        uint locale = 0;
        if (localeLibrary.IsNotNull)
        {
            Locale.LocaleLibraryBase = localeLibrary;
            locale = Locale.OpenLocale(CString.FromPointer(0));
        }

        var result = NativeMorphOSInfoCommand.Run(locale, out var ioError);
        if (result != DOS.RETURN_OK)
            DOS.PrintFault(DOS.IoErr(), CString.FromPointer(0));
        if (localeLibrary.IsNotNull)
        {
            Locale.CloseLocale(locale);
            Exec.CloseLibrary(localeLibrary);
        }
        Exec.CloseLibrary(utility);
        Utility.UtilityLibraryBase = APTR.Null;
        return NativeCommandStartup.Finish(result, ioError, APTR.Null);
    }
}
