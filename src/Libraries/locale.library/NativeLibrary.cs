/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Locale;

public static partial class LocaleNativeLibrary
{
    public const uint PositiveBytes = 324, NegativeBytes = 228;
    public const ushort Version = 53, Revision = 9;
    internal const uint DosBase = 38, DefaultLocale = 42, Catalogs = 46, LiveLocales = 50, Segment = 54, Gate = 58;
    internal const uint Busy = 104, MemoryInterrupt = 108, MemoryRegistered = 130;
    internal const uint DefaultHistory = 132;
    internal const uint UtilityBase = 136, SystemBase = 140, DosCatalog = 144, OriginalFunctions = 148, PatchThunks = 180;
    public static uint LibraryImageEntry() => uint.MaxValue;
    private static NativeLocalePlatform Bind(uint library)
    { return new NativeLocalePlatform { DosBase = APTR.ReadUInt32(APTR.FromPointer(library + DosBase), 0) }; }
    private static uint Locale(ref NativeLocalePlatform p, uint library, uint locale) => locale == 0 ? p.Read32(library + DefaultLocale) : locale;

    [M68kExport("copperos.locale.Init")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Init([M68kRegister(M68kRegister.D0)] uint library,
        [M68kRegister(M68kRegister.A0)] uint segment, [M68kRegister(M68kRegister.A6)] uint exec)
    {
        var p = default(NativeLocalePlatform);
        uint dos = Exec.OpenLibraryRaw(CString.FromLiteral("dos.library"), 37).Raw;
        if (dos == 0) { Exec.FreeMem(APTR.FromPointer(library - NegativeBytes), PositiveBytes + NegativeBytes); return 0; }
        p.Write32(library + DosBase, dos); p.DosBase = dos;
        uint l = LocaleCore.Default(ref p);
        if (l == 0) { Exec.CloseLibrary(APTR.FromPointer(dos)); Exec.FreeMem(APTR.FromPointer(library - NegativeBytes), PositiveBytes + NegativeBytes); return 0; }
        p.Write32(library + Segment, segment); p.Write32(library + DefaultLocale, l);
        p.Write8(library + 8, 9); p.Write32(library + 10, CString.ToUInt32(CString.FromLiteral("locale.library")));
        p.Write8(library + 14, 6); p.Write16(library + 20, Version); p.Write16(library + 22, Revision);
        // Keep the header text conventional while making Version FILE work.
        p.Write32(library + 24, CString.ToUInt32(CString.FromLiteral("$VER: locale.library 53.9 (CopperOS)")) + 6);
        Exec.InitSemaphore(APTR.FromPointer(library + Gate));
        uint configured = LocaleCore.Open(ref p, CString.ToUInt32(CString.FromLiteral("ENV:sys/locale.prefs")), l);
        if (configured != 0) { p.Write32(library + DefaultLocale, configured); LocaleCore.Close(ref p, l); }
        uint utility = Exec.OpenLibraryRaw(CString.FromLiteral("utility.library"), 37).Raw;
        if (utility == 0)
        {
            LocaleCore.Close(ref p, p.Read32(library + DefaultLocale)); Exec.CloseLibrary(APTR.FromPointer(dos));
            Exec.FreeMem(APTR.FromPointer(library - NegativeBytes), PositiveBytes + NegativeBytes); return 0;
        }
        p.Write32(library + UtilityBase, utility); p.Write32(library + SystemBase, exec);
        UpdateDosCatalog(ref p, library, p.Read32(library + DefaultLocale));
        InstallPatches(ref p, library);
        if (p.Read16(exec + 20) >= 39)
        {
            p.Write8(library + MemoryInterrupt + 8, 2);
            p.Write32(library + MemoryInterrupt + 10, p.Read32(library + 10));
            p.Write32(library + MemoryInterrupt + 14, library);
            p.Write32(library + MemoryInterrupt + 18, APTR.ExportAddress("copperos.locale.MemoryHandler").Raw);
            Exec.AddMemHandler(APTR.FromPointer(library + MemoryInterrupt)); p.Write16(library + MemoryRegistered, 1);
        }
        p.Error(0);
        return library;
    }
    [M68kExport("copperos.locale.Open")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Open([M68kRegister(M68kRegister.A6)] uint library)
    {
        var p = default(NativeLocalePlatform);
        Exec.Forbid();
        // Exec checks lib_Version before dispatching this vector. Like V40,
        // the vector itself accepts every D0 value (including OldOpenLibrary).
        p.Write16(library + 32, (ushort)(p.Read16(library + 32) + 1));
        p.Write8(library + 14, (byte)(p.Read8(library + 14) & ~8));
        Exec.Permit(); return library;
    }
    [M68kExport("copperos.locale.Close")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Close([M68kRegister(M68kRegister.A6)] uint library)
    {
        var p = default(NativeLocalePlatform); Exec.Forbid();
        ushort count = (ushort)(p.Read16(library + 32) - 1); p.Write16(library + 32, count);
        bool expunge = count == 0;
        Exec.Permit(); return expunge ? Expunge(library) : 0;
    }
    [M68kExport("copperos.locale.Expunge")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Expunge([M68kRegister(M68kRegister.A6)] uint library)
    {
        var p = Bind(library); Exec.Forbid();
        // Like the original, the system patches pin the library for the life
        // of the system, including when another patch has chained our code.
        if (p.Read32(library + 34) != 0)
        {
            p.Write8(library + 14, (byte)(p.Read8(library + 14) & ~8));
            if (p.Read32(library + Busy) == 0 && Exec.AttemptSemaphore(APTR.FromPointer(library + Gate)) != 0)
            {
                uint closed = p.Read32(library + Catalogs); CatalogService.Flush(ref p, ref closed); p.Write32(library + Catalogs, closed);
                Exec.ReleaseSemaphore(APTR.FromPointer(library + Gate));
            }
            Exec.Permit(); return 0;
        }
        uint cached = p.Read32(library + Catalogs); CatalogService.Flush(ref p, ref cached); p.Write32(library + Catalogs, cached);
        bool used = p.Read16(library + 32) != 0 || p.Read32(library + LiveLocales) != 0;
        for (uint c = p.Read32(library + Catalogs); c != 0; c = p.Read32(c + CatalogCore.Next))
            if (p.Read32(c + CatalogCore.References) != 0) used = true;
        if (used) { p.Write8(library + 14, (byte)(p.Read8(library + 14) | 8)); Exec.Permit(); return 0; }
        if (p.Read16(library + MemoryRegistered) != 0) Exec.RemMemHandler(APTR.FromPointer(library + MemoryInterrupt));
        Exec.Remove(APTR.FromPointer(library)); Exec.Permit();
        uint head = p.Read32(library + Catalogs); CatalogService.Flush(ref p, ref head);
        LocaleCore.Close(ref p, p.Read32(library + DefaultLocale));
        for (uint node = p.Read32(library + DefaultHistory); node != 0;)
        { uint next = p.Read32(node); LocaleCore.Close(ref p, p.Read32(node + 4)); p.Free(node); node = next; }
        uint segment = p.Read32(library + Segment), dos = p.Read32(library + DosBase);
        Exec.CloseLibrary(APTR.FromPointer(dos));
        Exec.FreeMem(APTR.FromPointer(library - NegativeBytes), PositiveBytes + NegativeBytes); return segment;
    }
    [M68kExport("copperos.locale.Reserved")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Reserved() => 0;

    [M68kExport("copperos.locale.GetCatalogStr")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint GetCatalogStr([M68kRegister(M68kRegister.A0)] uint catalog,
        [M68kRegister(M68kRegister.D0)] uint id, [M68kRegister(M68kRegister.A1)] uint fallback)
    { var p = default(NativeLocalePlatform); return CatalogCore.GetString(ref p, catalog, id, fallback); }

    [M68kExport("copperos.locale.OpenCatalogA")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint OpenCatalogA([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale,
        [M68kRegister(M68kRegister.A1)] uint name, [M68kRegister(M68kRegister.A2)] uint tags)
    {
        var p = Bind(library); Exec.ObtainSemaphore(APTR.FromPointer(library + Gate));
        p.Write32(library + Busy, 1);
        uint head = p.Read32(library + Catalogs);
        uint result = CatalogService.Open(ref p, Locale(ref p, library, locale), name, tags, ref head);
        p.Write32(library + Catalogs, head); p.Write32(library + Busy, 0);
        Exec.ReleaseSemaphore(APTR.FromPointer(library + Gate)); return result;
    }
    [M68kExport("copperos.locale.CloseCatalog")]
    public static void CloseCatalog([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint catalog)
    {
        var p = default(NativeLocalePlatform); Exec.ObtainSemaphore(APTR.FromPointer(library + Gate));
        CatalogService.Close(ref p, catalog);
        Exec.ReleaseSemaphore(APTR.FromPointer(library + Gate));
    }
    [M68kExport("copperos.locale.OpenLocale")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint OpenLocale([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint name)
    {
        var p = Bind(library); Exec.ObtainSemaphore(APTR.FromPointer(library + Gate));
        p.Write32(library + Busy, 1);
        uint result = LocaleCore.Open(ref p, name, p.Read32(library + DefaultLocale));
        if (result == 0 && p.LastError() == 103)
        {
            uint head = p.Read32(library + Catalogs); CatalogService.Flush(ref p, ref head); p.Write32(library + Catalogs, head);
            result = LocaleCore.Open(ref p, name, p.Read32(library + DefaultLocale));
        }
        if (result != 0) p.Write32(library + LiveLocales, p.Read32(library + LiveLocales) + 1);
        p.Write32(library + Busy, 0); Exec.ReleaseSemaphore(APTR.FromPointer(library + Gate)); return result;
    }
    [M68kExport("copperos.locale.CloseLocale")]
    public static void CloseLocale([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale)
    {
        if (locale == 0) return;
        var p = Bind(library); Exec.ObtainSemaphore(APTR.FromPointer(library + Gate));
        LocaleCore.Close(ref p, locale); p.Write32(library + LiveLocales, p.Read32(library + LiveLocales) - 1);
        Exec.ReleaseSemaphore(APTR.FromPointer(library + Gate));
    }
    [M68kExport("copperos.locale.LocalePrefsUpdate")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint LocalePrefsUpdate([M68kRegister(M68kRegister.A6)] uint library, [M68kRegister(M68kRegister.A0)] uint locale,
        [M68kRegister(M68kRegister.A1)] uint workspace)
    {
        var p = Bind(library); Exec.ObtainSemaphore(APTR.FromPointer(library + Gate));
        uint old = p.Read32(library + DefaultLocale);
        if (locale != 0)
        {
            // Retired defaults stay alive for lock-free NULL-locale calls and
            // returned language strings. Each history node owns one reference.
            uint node = p.Allocate(8);
            if (node == 0) { Exec.ReleaseSemaphore(APTR.FromPointer(library + Gate)); p.Error(103); return 0; }
            p.Write32(node, p.Read32(library + DefaultHistory)); p.Write32(node + 4, old); p.Write32(library + DefaultHistory, node);
            p.Write32(locale + LocaleLayout.References, p.Read32(locale + LocaleLayout.References) + 1);
            p.Write32(old + LocaleLayout.References, p.Read32(old + LocaleLayout.References) + 1);
            p.Write32(library + DefaultLocale, locale); p.Write32(library + LiveLocales, p.Read32(library + LiveLocales) + 1);
            UpdateDosCatalog(ref p, library, locale);
            UpdateLanguageEnvironment(ref p, library, locale, workspace);
        }
        Exec.ReleaseSemaphore(APTR.FromPointer(library + Gate)); return old;
    }
    [M68kExport("copperos.locale.MemoryHandler")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint MemoryHandler([M68kRegister(M68kRegister.A1)] uint library)
    {
        var p = default(NativeLocalePlatform);
        if (p.Read32(library + Busy) != 0 || Exec.AttemptSemaphore(APTR.FromPointer(library + Gate)) == 0) return 0;
        uint head = p.Read32(library + Catalogs); bool freed = false;
        for (uint c = head; c != 0; c = p.Read32(c + CatalogCore.Next)) if (p.Read32(c + CatalogCore.References) == 0) freed = true;
        CatalogService.Flush(ref p, ref head); p.Write32(library + Catalogs, head);
        Exec.ReleaseSemaphore(APTR.FromPointer(library + Gate)); return freed ? 1u : 0;
    }
}
