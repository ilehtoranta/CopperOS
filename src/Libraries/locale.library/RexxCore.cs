/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.Locale;

/// <summary>MorphOS -30 ARexx register interface, using classic RexxMsg layout.</summary>
public static class RexxCore
{
    private static uint Argument<T>(ref T p, uint message, uint index) where T : struct, ILocalePlatform => p.Read32(message + 40 + index * 4);
    private static bool Integer<T>(ref T p, uint text, out uint value) where T : struct, ILocalePlatform
    {
        value = 0; if (text == 0) return false;
        bool negative = p.Read8(text) == 45;
        if (negative || p.Read8(text) == 43) text++;
        uint count = 0;
        while (p.Read8(text) >= 48 && p.Read8(text) <= 57)
        {
            uint digit = p.Read8(text++) - 48u;
            if (value > (uint.MaxValue - digit) / 10) return false;
            value = value * 10 + digit; count++;
        }
        if (negative) value = 0u - value;
        return count != 0 && p.Read8(text) == 0;
    }
    private static uint Decimal<T>(ref T p, uint buffer, uint number, bool signed) where T : struct, ILocalePlatform
    {
        uint at = buffer;
        if (signed && (int)number < 0) { p.Write8(at++, 45); number = 0u - number; }
        uint power = 1;
        while (power <= number / 10) power *= 10;
        do { p.Write8(at++, (byte)(48 + number / power)); number %= power; power /= 10; } while (power != 0);
        p.Write8(at, 0); return buffer;
    }
    public static uint Evaluate<T>(ref T p, uint locale, uint message, ref uint catalogs, out uint result, out uint temporary) where T : struct, ILocalePlatform
    {
        result = temporary = 0;
        if (message == 0 || (p.Read32(message + 28) & 0xff000000) != 0x02000000 || Argument(ref p, message, 0) == 0) return 10;
        uint command = uint.MaxValue;
        for (uint i = 0; i < 18; i++) if (LocaleMemory.Equal(ref p, Argument(ref p, message, 0), p.Literal(1000 + i))) { command = i; break; }
        if (command == uint.MaxValue) return 1;
        uint expected = command == 0 || command == 2 || command == 6 ? 3u : 1u;
        if ((p.Read32(message + 28) & 15) != expected) return 17;
        for (uint i = 1; i <= expected; i++) if (Argument(ref p, message, i) == 0) return 18;
        uint a = Argument(ref p, message, 1), b = expected >= 2 ? Argument(ref p, message, 2) : 0,
            c = expected >= 3 ? Argument(ref p, message, 3) : 0;
        temporary = p.Allocate(64);
        if (temporary == 0) return 3;
        result = temporary + 32;
        if (command == 0)
        {
            if (!Integer(ref p, c, out uint version)) return 18;
            p.Write32(temporary, CatalogService.TagBase + 1); p.Write32(temporary + 4, b);
            p.Write32(temporary + 8, CatalogService.TagBase + 3); p.Write32(temporary + 12, version);
            uint catalog = CatalogService.Open(ref p, locale, a, temporary, ref catalogs);
            Decimal(ref p, result, catalog, false);
        }
        else if (command == 1)
        {
            if (!Integer(ref p, a, out uint catalog)) return 18;
            CatalogService.Close(ref p, catalog); p.Write8(result, 48);
        }
        else if (command == 2)
        {
            if (!Integer(ref p, a, out uint catalog) || !Integer(ref p, b, out uint id)) return 18;
            result = CatalogCore.GetString(ref p, catalog, id, c);
        }
        else if (command == 3)
        {
            if (!Integer(ref p, a, out uint id)) return 18;
            result = LocaleCore.String(ref p, locale, id);
        }
        else if (command == 4 || command == 5) p.Write8(result, (byte)CharacterCore.Operation(ref p, locale, command - 4, p.Read8(a)));
        else if (command == 6)
        {
            if (!Integer(ref p, c, out uint type)) return 18;
            Decimal(ref p, result, (uint)CharacterCore.Compare(ref p, locale, a, b, -1, type), true);
        }
        else p.Write8(result, CharacterCore.Operation(ref p, locale, command - 3, p.Read8(a)) != 0 ? (byte)49 : (byte)48);
        return 0;
    }
}
