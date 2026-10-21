/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;
namespace CopperOS.Locale;

public static partial class LocaleNativeLibrary
{
    private static int PatchOffset(uint i) => i == 0 ? -522 : i == 1 ? -168 : i == 2 ? -162 : i == 3 ? -180 : i == 4 ? -174 : i == 5 ? -744 : i == 6 ? -750 : -978;
    private static uint PatchBase(ref NativeLocalePlatform p, uint library, uint i) => p.Read32(library + (i == 0 ? SystemBase : i < 5 ? UtilityBase : DosBase));
    private static void InstallPatches(ref NativeLocalePlatform p, uint library)
    {
        // Per-instance code binds the locale base. The on-disk image remains
        // read-only, and the private vectors accept the foreign library's A6.
        for (uint i = 0; i < 8; i++)
        {
            uint code = library + PatchThunks + i * 18;
            p.Write16(code, 0x2f0e); // move.l a6,-(sp)
            p.Write16(code + 2, 0x2c7c); p.Write32(code + 4, library);
            p.Write16(code + 8, 0x4eb9); p.Write32(code + 10, p.Read32(library - 186 - i * 6 + 2));
            p.Write16(code + 14, 0x2c5f); p.Write16(code + 16, 0x4e75);
        }
        Exec.CacheClearU();
        Exec.Forbid();
        for (uint i = 0; i < 8; i++)
        {
            uint thunk = library + PatchThunks + i * 18;
            Exec.SetFunction(APTR.FromPointer(library), -186 - (int)i * 6, APTR.FromPointer(thunk));
            uint old = Exec.SetFunction(APTR.FromPointer(PatchBase(ref p, library, i)), PatchOffset(i), APTR.FromPointer(thunk)).Raw;
            p.Write32(library + OriginalFunctions + i * 4, old);
        }
        p.Write32(library + 34, 1); Exec.Permit();
    }
    private static void UpdateDosCatalog(ref NativeLocalePlatform p, uint library, uint locale)
    {
        int error = p.LastError(); uint head = p.Read32(library + Catalogs);
        uint catalog = p.Read32(locale + LocaleLayout.SystemCatalog);
        if (catalog == 0)
        {
            p.Write32(library + Busy, 1);
            catalog = CatalogService.Open(ref p, locale, CString.ToUInt32(CString.FromLiteral("sys/dos.catalog")), 0, ref head);
            p.Write32(locale + LocaleLayout.SystemCatalog, catalog); p.Write32(library + Busy, 0);
        }
        // Retain the previous system catalog: DOS returns borrowed strings
        // which may still be used after a preferences change.
        p.Write32(library + Catalogs, head); p.Write32(library + DosCatalog, catalog); p.Error(error);
    }
    private static void UpdateLanguageEnvironment(ref NativeLocalePlatform p, uint library, uint locale, uint workspace)
    {
        uint source = p.Read32(locale + LocaleLayout.LanguageName), length = 0;
        // OpenLocale's own language names are at most 30 characters plus the
        // suffix. Copy the basename without modifying the public driver name.
        while (p.Read8(source + length) != 0 && p.Read8(source + length) != 46)
        { p.Write8(workspace + length, p.Read8(source + length)); length++; }
        p.Write8(workspace + length, 0);
        int error = p.LastError();
        LocaleDos.SetVar(p.DosBase, CString.ToUInt32(CString.FromLiteral("Language")), workspace, (int)length, 0x100);
        p.Error(error);
        uint code = 0;
        if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("english")))) code = p.Read8(locale + LocaleLayout.Measuring) == 1 ? 1u : 2u;
        else if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("deutsch")))) code = 3;
        else if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("français")))) code = 4;
        else if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("español")))) code = 5;
        else if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("italiano")))) code = 6;
        else if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("português")))) code = 7;
        else if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("dansk")))) code = 8;
        else if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("nederlands")))) code = 9;
        else if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("norsk")))) code = 10;
        else if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("suomi")))) code = 11;
        else if (LocaleMemory.Equal(ref p, workspace, CString.ToUInt32(CString.FromLiteral("svenska")))) code = 12;
        if (code != 0) p.Write8(p.Read32(library + UtilityBase) + 34, (byte)code);
    }
    [M68kExport("copperos.locale.LocStrnicmp")]
    [return: M68kRegister(M68kRegister.D0)]
    public static int LocStrnicmp([M68kRegister(M68kRegister.A6)] uint library,
        [M68kRegister(M68kRegister.A0)] uint a, [M68kRegister(M68kRegister.A1)] uint b, [M68kRegister(M68kRegister.D0)] int length)
    { var p = default(NativeLocalePlatform); return CharacterCore.Compare(ref p, Locale(ref p, library, 0), a, b, length, 0); }
    [M68kExport("copperos.locale.LocStricmp")]
    [return: M68kRegister(M68kRegister.D0)]
    public static int LocStricmp([M68kRegister(M68kRegister.A6)] uint library,
        [M68kRegister(M68kRegister.A0)] uint a, [M68kRegister(M68kRegister.A1)] uint b) => LocStrnicmp(library, a, b, -1);
    [M68kExport("copperos.locale.LocToLower")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint LocToLower([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D0)] uint c)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, 0), 0, c); }
    [M68kExport("copperos.locale.LocToUpper")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint LocToUpper([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D0)] uint c)
    { var p = default(NativeLocalePlatform); return CharacterCore.Operation(ref p, Locale(ref p, library, 0), 1, c); }
    [M68kExport("copperos.locale.LocDosGetLocalizedString")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint LocDosGetLocalizedString([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.D1)] int id)
    {
        var p = Bind(library); uint key = (uint)id;
        uint text = CatalogCore.GetString(ref p, p.Read32(library + DosCatalog), key, 0);
        if (text != 0) return text;
        uint original = p.Read32(library + OriginalFunctions + 28);
        // Both the classic private getter and the locale replacement take
        // the error ID in D1. Fault leaves unrelated scratch data in D0.
        return original == 0 ? 0 : NativeLocaleCallbacks.DosString(APTR.FromPointer(original), p.DosBase, id);
    }
    // The packager supplies stack workspace and the RawDoFmt callback bridge.
    [M68kExport("copperos.locale.LocRawDoFmt")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint LocRawDoFmt([M68kRegister(M68kRegister.A6)] uint library,
        [M68kRegister(M68kRegister.A0)] uint format, [M68kRegister(M68kRegister.A1)] uint stream, [M68kRegister(M68kRegister.A2)] uint hook)
    { var p = Bind(library); return FormatCore.Format(ref p, Locale(ref p, library, 0), format, stream, hook, true); }
}
