/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.Locale;

/// <summary>Native boundary. All addresses refer to big-endian guest memory.</summary>
public interface ILocalePlatform
{
    byte Read8(uint address);
    ushort Read16(uint address);
    uint Read32(uint address);
    void Write8(uint address, byte value);
    void Write16(uint address, ushort value);
    void Write32(uint address, uint value);
    uint Allocate(uint bytes); // zero-filled; zero on failure
    void Free(uint address);
    uint Literal(uint text);
    uint LoadFile(uint name, out uint bytes); // owned allocation; zero on failure
    uint CatalogLocations(); // mounted PROGDIR (bit 0) and LOCALE (bit 1), without requesters
    void Error(int error);
    int LastError();
    uint OpenLanguage(uint name);
    void CloseLanguage(uint language);
    uint Language(uint language, uint function, uint d0, uint d1, uint a1, uint a2);
    uint Hook(uint hook, uint locale, uint character);
}

/// <summary>Public ABI and private record offsets, kept at the serialization boundary.</summary>
public static class LocaleLayout
{
    public const uint PublicSize = 168, Size = 192;
    internal const uint SystemCatalog = 188;
    public const uint References = 168, Driver = 172, Mask = 176, File = 180, Next = 184;
    public const uint Name = 0, LanguageName = 4, Preferred = 8, Flags = 48, CodeSet = 52;
    public const uint Country = 56, Telephone = 60, GmtOffset = 64, Measuring = 68, Calendar = 69;
    public const uint DateTime = 72, Date = 76, Time = 80, ShortDateTime = 84, ShortDate = 88, ShortTime = 92;
    public const uint Decimal = 96, GroupSeparator = 100, FractionSeparator = 104, Grouping = 108, FractionGrouping = 112;
    public const uint MonDecimal = 116, MonGroupSeparator = 120, MonFractionSeparator = 124, MonGrouping = 128, MonFractionGrouping = 132;
    public const uint MonDigits = 136, MonIntDigits = 137, Currency = 140, SmallCurrency = 144, IntCurrency = 148;
    public const uint PositiveSign = 152, NegativeSign = 160;
}

public static class LocaleMemory
{
    public static ushort U16<T>(ref T p, uint a) where T : struct, ILocalePlatform =>
        p.Read16(a);
    public static uint U32<T>(ref T p, uint a) where T : struct, ILocalePlatform =>
        p.Read32(a);
    public static void W16<T>(ref T p, uint a, ushort v) where T : struct, ILocalePlatform
    { p.Write16(a, v); }
    public static void W32<T>(ref T p, uint a, uint v) where T : struct, ILocalePlatform
    { p.Write32(a, v); }
    public static uint Length<T>(ref T p, uint a) where T : struct, ILocalePlatform
    { uint n = 0; if (a != 0) while (p.Read8(a + n) != 0) n++; return n; }
    public static void Copy<T>(ref T p, uint source, uint destination, uint count) where T : struct, ILocalePlatform
    { for (uint i = 0; i < count; i++) p.Write8(destination + i, p.Read8(source + i)); }
    public static bool Equal<T>(ref T p, uint a, uint b) where T : struct, ILocalePlatform
    {
        if (a == 0 || b == 0) return a == b;
        for (;;) { uint x = p.Read8(a++), y = p.Read8(b++); if (CharacterCore.Lower(x) != CharacterCore.Lower(y)) return false; if (x == 0) return true; }
    }
    // IFF chunk lengths are untrusted disk data, unlike caller-owned API structures.
    public static bool Form<T>(ref T p, uint file, uint bytes, uint type, out uint end) where T : struct, ILocalePlatform
    {
        end = 0;
        if (bytes < 12 || file > uint.MaxValue - bytes || U32(ref p, file) != 0x464f524d || U32(ref p, file + 8) != type) return false;
        uint size = U32(ref p, file + 4);
        if (size < 4 || size > bytes - 8) return false;
        end = file + 8 + size; return true;
    }
    public static bool Chunk<T>(ref T p, uint cursor, uint end, out uint id, out uint data, out uint size, out uint next) where T : struct, ILocalePlatform
    {
        id = data = size = next = 0;
        if (cursor > end || end - cursor < 8) return false;
        id = U32(ref p, cursor); size = U32(ref p, cursor + 4); data = cursor + 8;
        if (size > end - data || (size == end - data && (size & 1) != 0)) return false;
        next = data + size + (size & 1); return true;
    }
    public static bool Terminated<T>(ref T p, uint a, uint length) where T : struct, ILocalePlatform
    { for (uint i = 0; i < length; i++) if (p.Read8(a + i) == 0) return true; return false; }
}
