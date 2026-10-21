/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.Locale;

public static class LocaleCore
{
    public static uint Default<T>(ref T p) where T : struct, ILocalePlatform
    {
        uint l = p.Allocate(LocaleLayout.Size + 64);
        if (l == 0) { p.Error(103); return 0; }
        p.Write32(l + LocaleLayout.References, 1);
        p.Write32(l, p.Literal(LocaleText.DefaultCountry)); p.Write32(l + 4, p.Literal(LocaleText.EnglishDriver));
        p.Write32(l + 8, p.Literal(LocaleText.English)); p.Write32(l + 56, 0x55534100); p.Write32(l + 60, 1); p.Write8(l + 68, 1);
        p.Write32(l + 72, p.Literal(LocaleText.DefaultDateTime)); p.Write32(l + 76, p.Literal(LocaleText.DefaultDate));
        p.Write32(l + 80, p.Literal(LocaleText.DefaultTime)); p.Write32(l + 84, p.Literal(LocaleText.DefaultShortDateTime));
        p.Write32(l + 88, p.Literal(LocaleText.DefaultShortDate)); p.Write32(l + 92, p.Literal(LocaleText.DefaultShortTime));
        p.Write32(l + 96, p.Literal(LocaleText.DecimalPoint)); p.Write32(l + 100, p.Literal(LocaleText.GroupSeparator)); p.Write32(l + 104, p.Literal(LocaleText.DecimalPoint));
        p.Write32(l + 108, p.Literal(LocaleText.GroupingThree)); p.Write32(l + 112, p.Literal(LocaleText.GroupingThree));
        p.Write32(l + 116, p.Literal(LocaleText.DecimalPoint)); p.Write32(l + 120, p.Literal(LocaleText.GroupSeparator)); p.Write32(l + 124, p.Literal(LocaleText.GroupSeparator));
        p.Write32(l + 128, p.Literal(LocaleText.GroupingThree)); p.Write32(l + 132, p.Literal(LocaleText.GroupingThree));
        p.Write8(l + 136, 2); p.Write8(l + 137, 2);
        p.Write32(l + 140, p.Literal(LocaleText.Dollar)); p.Write32(l + 144, p.Literal(LocaleText.Cent)); p.Write32(l + 148, p.Literal(LocaleText.Usd));
        p.Write32(l + 152, p.Literal(LocaleText.Empty)); p.Write8(l + 157, 1);
        p.Write32(l + 160, p.Literal(LocaleText.Minus)); p.Write8(l + 165, 1);
        return l;
    }
    public static uint Open<T>(ref T p, uint name, uint defaultLocale) where T : struct, ILocalePlatform
    {
        if (name == 0)
        { p.Write32(defaultLocale + LocaleLayout.References, p.Read32(defaultLocale + LocaleLayout.References) + 1); return defaultLocale; }
        uint file = p.LoadFile(name, out uint bytes);
        if (file == 0) return 0;
        if (!LocaleMemory.Form(ref p, file, bytes, 0x50524546, out uint end)) { p.Free(file); p.Error(212); return 0; }
        uint prefs = 0, country = 0, cursor = file + 12;
        while (cursor < end)
        {
            if (!LocaleMemory.Chunk(ref p, cursor, end, out uint id, out uint data, out uint size, out cursor)) { p.Free(file); p.Error(212); return 0; }
            if (id == 0x4c434c45) { if (size < 860) { p.Free(file); p.Error(212); return 0; } prefs = data; }
            if (id == 0x43545259) { if (size < 504) { p.Free(file); p.Error(212); return 0; } country = data; }
        }
        if (prefs == 0 || !LocaleMemory.Terminated(ref p, prefs + 16, 32)) { p.Free(file); p.Error(212); return 0; }
        for (uint i = 0; i < 10; i++)
            if (!LocaleMemory.Terminated(ref p, prefs + 48 + i * 30, 30)) { p.Free(file); p.Error(212); return 0; }
        if (country == 0) country = prefs + 356;
        if (!ValidateCountry(ref p, country)) { p.Free(file); p.Error(212); return 0; }
        uint l = Default(ref p);
        if (l == 0) { p.Free(file); return 0; }
        p.Write32(l + LocaleLayout.File, file);
        p.Write32(l, prefs + 16); p.Write32(l + 64, p.Read32(prefs + 348));
        // Classic reserved flags remain zero; do not leak AROS-only bits.
        for (uint i = 0; i < 10; i++) p.Write32(l + 8 + i * 4, p.Read8(prefs + 48 + i * 30) == 0 ? 0 : prefs + 48 + i * 30);
        Country(ref p, l, country);
        // The first available preferred .language wins. Missing methods fall
        // back individually to English according to the driver's mask.
        for (uint i = 0; i < 10; i++)
        {
            uint language = p.Read32(l + 8 + i * 4);
            if (language == 0) continue;
            if (LocaleMemory.Equal(ref p, language, p.Literal(LocaleText.English))) break;
            uint path = l + LocaleLayout.Size;
            if (!Path(ref p, path, 64, p.Literal(LocaleText.LanguageDirectory), language, p.Literal(LocaleText.LanguageSuffix))) continue;
            uint driver = p.OpenLanguage(path);
            if (driver == 0) continue;
            p.Write32(l + LocaleLayout.Driver, driver); p.Write32(l + LocaleLayout.Mask, p.Language(driver, uint.MaxValue, 0, 0, 0, 0));
            // Keep only the leaf language name in the public structure.
            Path(ref p, path, 64, language, p.Literal(LocaleText.LanguageSuffix), 0);
            p.Write32(l + 4, path); break;
        }
        return l;
    }
    public static void Close<T>(ref T p, uint locale) where T : struct, ILocalePlatform
    {
        if (locale == 0) return;
        uint refs = p.Read32(locale + LocaleLayout.References) - 1;
        p.Write32(locale + LocaleLayout.References, refs);
        if (refs != 0) return;
        uint driver = p.Read32(locale + LocaleLayout.Driver);
        if (driver != 0) p.CloseLanguage(driver);
        CatalogService.Close(ref p, p.Read32(locale + LocaleLayout.SystemCatalog));
        p.Free(p.Read32(locale + LocaleLayout.File)); p.Free(locale);
    }
    private static bool ValidateCountry<T>(ref T p, uint c) where T : struct, ILocalePlatform
    {
        if (!LocaleMemory.Terminated(ref p, c + 25, 80) || !LocaleMemory.Terminated(ref p, c + 105, 40) ||
            !LocaleMemory.Terminated(ref p, c + 145, 40) || !LocaleMemory.Terminated(ref p, c + 185, 80) ||
            !LocaleMemory.Terminated(ref p, c + 265, 40) || !LocaleMemory.Terminated(ref p, c + 305, 40)) return false;
        for (uint i = 0; i < 10; i++) if (!LocaleMemory.Terminated(ref p, c + 345 + i * 10, 10)) return false;
        for (uint i = 0; i < 4; i++) if (!LocaleMemory.Terminated(ref p, c + 447 + i * 10, 10)) return false;
        return LocaleMemory.Terminated(ref p, c + 490, 10);
    }
    private static void Country<T>(ref T p, uint l, uint c) where T : struct, ILocalePlatform
    {
        // CountryPrefs is a packed disk structure: ULONGs in its byte fields
        // may be odd aligned, so only byte reads are used for its longwords.
        p.Write32(l + 56, DiskLong(ref p, c + 16)); p.Write32(l + 60, DiskLong(ref p, c + 20));
        p.Write8(l + 68, p.Read8(c + 24)); p.Write8(l + 69, p.Read8(c + 503));
        p.Write32(l + 72, c + 25); p.Write32(l + 76, c + 105); p.Write32(l + 80, c + 145);
        p.Write32(l + 84, c + 185); p.Write32(l + 88, c + 265); p.Write32(l + 92, c + 305);
        for (uint i = 0; i < 10; i++) p.Write32(l + 96 + i * 4, c + 345 + i * 10);
        p.Write8(l + 136, p.Read8(c + 445)); p.Write8(l + 137, p.Read8(c + 446));
        p.Write32(l + 140, c + 447); p.Write32(l + 144, c + 457); p.Write32(l + 148, c + 467);
        p.Write32(l + 152, c + 477); p.Write32(l + 160, c + 490);
        for (uint i = 0; i < 3; i++) { p.Write8(l + 156 + i, p.Read8(c + 487 + i)); p.Write8(l + 164 + i, p.Read8(c + 500 + i)); }
    }
    private static uint DiskLong<T>(ref T p, uint a) where T : struct, ILocalePlatform =>
        ((uint)p.Read8(a) << 24) | ((uint)p.Read8(a + 1) << 16) | ((uint)p.Read8(a + 2) << 8) | p.Read8(a + 3);
    public static bool Path<T>(ref T p, uint buffer, uint capacity, uint a, uint b, uint c) where T : struct, ILocalePlatform
    {
        uint n = 0;
        for (uint part = 0; part < 3; part++)
        {
            uint s = part == 0 ? a : part == 1 ? b : c;
            if (s == 0) continue;
            while (p.Read8(s) != 0) { if (n >= capacity - 1) return false; p.Write8(buffer + n++, p.Read8(s++)); }
        }
        p.Write8(buffer + n, 0); return true;
    }
    public static uint String<T>(ref T p, uint locale, uint id) where T : struct, ILocalePlatform
    {
        uint driver = p.Read32(locale + LocaleLayout.Driver);
        if (driver != 0 && (p.Read32(locale + LocaleLayout.Mask) & 8) != 0) return p.Language(driver, 3, id, 0, 0, 0);
        return id switch
        {
            1 => p.Literal(LocaleText.Sunday), 2 => p.Literal(LocaleText.Monday), 3 => p.Literal(LocaleText.Tuesday), 4 => p.Literal(LocaleText.Wednesday),
            5 => p.Literal(LocaleText.Thursday), 6 => p.Literal(LocaleText.Friday), 7 => p.Literal(LocaleText.Saturday),
            8 => p.Literal(LocaleText.Sun), 9 => p.Literal(LocaleText.Mon), 10 => p.Literal(LocaleText.Tue), 11 => p.Literal(LocaleText.Wed), 12 => p.Literal(LocaleText.Thu), 13 => p.Literal(LocaleText.Fri), 14 => p.Literal(LocaleText.Sat),
            15 => p.Literal(LocaleText.January), 16 => p.Literal(LocaleText.February), 17 => p.Literal(LocaleText.March), 18 => p.Literal(LocaleText.April),
            19 => p.Literal(LocaleText.May), 20 => p.Literal(LocaleText.June), 21 => p.Literal(LocaleText.July), 22 => p.Literal(LocaleText.August),
            23 => p.Literal(LocaleText.September), 24 => p.Literal(LocaleText.October), 25 => p.Literal(LocaleText.November), 26 => p.Literal(LocaleText.December),
            27 => p.Literal(LocaleText.Jan), 28 => p.Literal(LocaleText.Feb), 29 => p.Literal(LocaleText.Mar), 30 => p.Literal(LocaleText.Apr),
            31 => p.Literal(LocaleText.May), 32 => p.Literal(LocaleText.Jun), 33 => p.Literal(LocaleText.Jul), 34 => p.Literal(LocaleText.Aug),
            35 => p.Literal(LocaleText.Sep), 36 => p.Literal(LocaleText.Oct), 37 => p.Literal(LocaleText.Nov), 38 => p.Literal(LocaleText.Dec),
            39 => p.Literal(LocaleText.Yes), 40 => p.Literal(LocaleText.No), 41 => p.Literal(LocaleText.Am), 42 => p.Literal(LocaleText.Pm),
            43 => p.Literal(LocaleText.Minus), 44 => p.Literal(LocaleText.Minus), 45 => p.Literal(LocaleText.Quote), 46 => p.Literal(LocaleText.Quote),
            47 => p.Literal(LocaleText.Yesterday), 48 => p.Literal(LocaleText.Today), 49 => p.Literal(LocaleText.Tomorrow), 50 => p.Literal(LocaleText.Future),
            51 => p.Literal(LocaleText.EnglishDisplay), _ => 0,
        };
    }
}
