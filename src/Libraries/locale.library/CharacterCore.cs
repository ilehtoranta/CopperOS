/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.Locale;

/// <summary>Built-in ISO-8859-1 English; installed language drivers override individual operations.</summary>
public static class CharacterCore
{
    public const uint Alpha = 1, Control = 2, Digit = 4, Graph = 8, LowerCase = 16,
        Printable = 32, Punctuation = 64, Space = 128, UpperCase = 256, Hex = 512;
    // V40 English deliberately maps the multiplication/division pair too.
    public static uint Lower(uint c) => (c >= 65 && c <= 90) || (c >= 192 && c <= 222) ? c + 32 : c;
    public static uint Upper(uint c) => (c >= 97 && c <= 122) || (c >= 224 && c <= 254) ? c - 32 : c;
    public static uint Classify(uint c)
    {
        if (c > 255) return 0;
        uint f = 0;
        if (c < 32 || (c >= 127 && c < 160)) f |= Control;
        if (c == 32 || c == 160 || (c >= 9 && c <= 13)) f |= Space;
        if ((c >= 32 && c < 127) || c >= 160 || (c >= 9 && c <= 13)) f |= Printable;
        if ((c >= 33 && c < 127) || c >= 161) f |= Graph;
        if (c >= 48 && c <= 57) f |= Digit | Hex;
        if ((c >= 65 && c <= 90) || (c >= 192 && c <= 223)) f |= Alpha | UpperCase;
        if ((c >= 97 && c <= 122) || (c >= 223 && c <= 255 && c != 247)) f |= Alpha | LowerCase;
        uint lower = Lower(c);
        if (lower >= 97 && lower <= 102) f |= Hex;
        if ((f & Graph) != 0 && (f & (Alpha | Digit)) == 0) f |= Punctuation;
        return f;
    }
    public static uint Primary(uint c)
    {
        c = Lower(c);
        if (c == 223) return 116; // between S and T, not an expansion to "ss"
        if (c >= 224 && c <= 230) c = 97;
        else if (c == 231) c = 99;
        else if (c >= 232 && c <= 235) c = 101;
        else if (c >= 236 && c <= 239) c = 105;
        else if (c == 241) c = 110;
        else if ((c >= 242 && c <= 246) || c == 248) c = 111;
        else if (c >= 249 && c <= 252) c = 117;
        else if (c == 253 || c == 255) c = 121;
        return c >= 116 ? c + 1 : c;
    }
    public static uint Operation<T>(ref T p, uint locale, uint function, uint value) where T : struct, ILocalePlatform
    {
        uint driver = LocaleMemory.U32(ref p, locale + LocaleLayout.Driver);
        if (driver != 0 && (LocaleMemory.U32(ref p, locale + LocaleLayout.Mask) & (1u << (int)function)) != 0)
            return p.Language(driver, function, value, 0, 0, 0);
        if (function == 0) return Lower(value);
        if (function == 1) return Upper(value);
        uint mask = function switch { 4 => Alpha | Digit, 5 => Alpha, 6 => Control, 7 => Digit, 8 => Graph,
            9 => LowerCase, 10 => Printable, 11 => Punctuation, 12 => Space, 13 => UpperCase, 14 => Hex, _ => 0 };
        return Classify(value) & mask;
    }
    public static int Compare<T>(ref T p, uint locale, uint a, uint b, int length, uint type) where T : struct, ILocalePlatform
    {
        uint driver = LocaleMemory.U32(ref p, locale + LocaleLayout.Driver);
        if (driver != 0 && (LocaleMemory.U32(ref p, locale + LocaleLayout.Mask) & (1u << 16)) != 0)
            return (int)p.Language(driver, 16, (uint)length, type, a, b);
        if (type > 2) return 0;
        // Secondary weights are compared only after ALL primary weights agree.
        int secondary = 0;
        while (length != 0)
        {
            uint x = a == 0 ? 0u : p.Read8(a++), y = b == 0 ? 0u : p.Read8(b++);
            uint lx = Upper(x), ly = Upper(y);
            uint px = type == 0 ? lx : Primary(x), py = type == 0 ? ly : Primary(y);
            if (px != py) return (int)px - (int)py;
            if (type == 2 && secondary == 0) secondary = (int)x - (int)y;
            if (x == 0 || y == 0) break;
            if (length > 0) length--;
        }
        return secondary;
    }
    public static uint Convert<T>(ref T p, uint locale, uint text, uint buffer, uint size, uint type) where T : struct, ILocalePlatform
    {
        uint driver = LocaleMemory.U32(ref p, locale + LocaleLayout.Driver);
        if (driver != 0 && (LocaleMemory.U32(ref p, locale + LocaleLayout.Mask) & (1u << 15)) != 0)
            return p.Language(driver, 15, size, type, text, buffer);
        if (size == 0 || type > 2) return 0;
        uint n = LocaleMemory.Length(ref p, text), written = 0;
        // A separator byte smaller than every non-NUL primary key makes these
        // keys match the two-pass comparison (including prefix strings).
        for (uint i = 0; i < n && written < size - 1; i++)
        {
            uint value = type == 0 ? Upper(p.Read8(text + i)) : Primary(p.Read8(text + i));
            if (type == 2)
            {
                // Nibble weights reserve 1 for the primary/secondary separator,
                // including when the input contains control bytes such as 1.
                p.Write8(buffer + written++, (byte)(2 + (value >> 4)));
                if (written < size - 1) p.Write8(buffer + written++, (byte)(2 + (value & 15)));
            }
            else p.Write8(buffer + written++, (byte)value);
        }
        if (type == 2 && written < size - 1)
        {
            p.Write8(buffer + written++, 1);
            for (uint i = 0; i < n && written < size - 1; i++)
            {
                uint value = p.Read8(text + i);
                p.Write8(buffer + written++, (byte)(2 + (value >> 4)));
                if (written < size - 1) p.Write8(buffer + written++, (byte)(2 + (value & 15)));
            }
        }
        p.Write8(buffer + written, 0); return written;
    }
}
