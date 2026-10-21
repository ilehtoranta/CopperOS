/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.Locale;

public struct LocaleFormatSpec
{
    public uint Position, Width, Limit, Bytes, Type;
    public bool Left, Zero, HasLimit;
}

public static class FormatCore
{
    private static uint Decimal<T>(ref T p, ref uint at) where T : struct, ILocalePlatform
    { uint n = 0; while (p.Read8(at) >= 48 && p.Read8(at) <= 57) n = n * 10 + p.Read8(at++) - 48u; return n; }
    private static LocaleFormatSpec Spec<T>(ref T p, ref uint at, ref uint ordinal) where T : struct, ILocalePlatform
    {
        var s = default(LocaleFormatSpec); s.Bytes = 2;
        uint start = at, position = Decimal(ref p, ref at);
        if (p.Read8(at) == 36) { at++; s.Position = position; } else at = start;
        if (p.Read8(at) == 45) { s.Left = true; at++; }
        s.Zero = p.Read8(at) == 48; s.Width = Decimal(ref p, ref at);
        if (p.Read8(at) == 46) { s.HasLimit = true; at++; s.Limit = Decimal(ref p, ref at); }
        if (p.Read8(at) == 108) { at++; s.Bytes = 4; if (p.Read8(at) == 108) { at++; s.Bytes = 8; } }
        else if (p.Read8(at) == 76) { at++; s.Bytes = 8; }
        s.Type = p.Read8(at); if (s.Type != 0) at++;
        if (s.Type == 115 || s.Type == 98) s.Bytes = 4;
        if (s.Type != 100 && s.Type != 68 && s.Type != 117 && s.Type != 85 && s.Type != 120 && s.Type != 88 && s.Type != 115 && s.Type != 98 && s.Type != 99) s.Bytes = 0;
        if (s.Bytes != 0) { ordinal++; if (s.Position == 0) s.Position = ordinal; }
        return s;
    }
    public static uint Format<T>(ref T p, uint l, uint format, uint stream, uint hook, bool rawCompatibility = false) where T : struct, ILocalePlatform
    {
        if (format == 0) return stream;
        uint at = format, ordinal = 0, maximum = 0; bool positional = false, wideGrouped = false;
        // First pass detects positional arguments; the ordinary RawDoFmt path
        // needs no workspace allocation at all.
        while (p.Read8(at) != 0)
        {
            if (p.Read8(at++) != 37) continue;
            uint before = ordinal; var s = Spec(ref p, ref at, ref ordinal);
            if (s.Bytes == 4 && (s.Type == 68 || s.Type == 85)) wideGrouped = true;
            if (s.Bytes != 0) { if (s.Position != before + 1) positional = true; if (s.Position > maximum) maximum = s.Position; }
        }
        uint table = 0;
        if (positional)
        {
            if (maximum >= 0x1ffffffe) { p.Error(103); return stream; }
            table = p.Allocate((maximum + 1) << 3);
            if (table == 0) { p.Error(103); return stream; }
            at = format; ordinal = 0;
            while (p.Read8(at) != 0)
            {
                if (p.Read8(at++) != 37) continue;
                var s = Spec(ref p, ref at, ref ordinal);
                if (s.Bytes != 0) p.Write32(table + (s.Position << 3), s.Bytes);
            }
            uint offset = 0;
            for (uint i = 1; i <= maximum; i++) { p.Write32(table + (i << 3) + 4, offset); offset += p.Read32(table + (i << 3)); }
        }
        at = format; ordinal = 0; uint next = stream, end = stream;
        while (p.Read8(at) != 0)
        {
            uint c = p.Read8(at++);
            if (c != 37) { p.Hook(hook, l, c); continue; }
            var s = Spec(ref p, ref at, ref ordinal);
            if (s.Bytes == 0) { if (s.Type != 0) p.Hook(hook, l, s.Type); continue; }
            uint arg = positional ? stream + p.Read32(table + (s.Position << 3) + 4) : next;
            next = arg + s.Bytes; if (next > end) end = next;
            uint high = 0, low;
            if (s.Bytes == 8) { high = p.Read32(arg); low = p.Read32(arg + 4); }
            else low = s.Bytes == 4 ? p.Read32(arg) : p.Read16(arg);
            if (s.Type == 115 || s.Type == 98 || s.Type == 99)
            {
                uint text = low, length;
                if (s.Type == 99) length = 1;
                else if (s.Type == 98) { text <<= 2; length = text == 0 ? 0u : p.Read8(text++); }
                else length = LocaleMemory.Length(ref p, text);
                if (s.HasLimit && length > s.Limit) length = s.Limit;
                if (!s.Left) Pad(ref p, l, hook, s.Width, length, s.Zero ? 48u : 32u);
                if (s.Type == 99) p.Hook(hook, l, low & 255);
                else for (uint i = 0; i < length; i++) p.Hook(hook, l, p.Read8(text + i));
                if (s.Left) Pad(ref p, l, hook, s.Width, length, 32);
                continue;
            }
            bool negative = false;
            if (s.Type == 100 || s.Type == 68)
            {
                if (s.Bytes == 2 && (low & 0x8000) != 0) { negative = true; low = (uint)(-(short)low); }
                else if (s.Bytes == 4 && (low & 0x80000000) != 0) { negative = true; low = 0u - low; }
                else if (s.Bytes == 8 && (high & 0x80000000) != 0) { negative = true; low = 0u - low; high = ~high + (low == 0 ? 1u : 0); }
            }
            uint radix = s.Type == 120 || s.Type == 88 ? 16u : 10u;
            uint h = high, v = low, digits = 0;
            do { Divide(ref h, ref v, radix); digits++; } while (h != 0 || v != 0);
            uint separator = 0, grouping = 0, groups = 0;
            if (s.Type == 68 || s.Type == 85)
            {
                separator = p.Read32(l + LocaleLayout.GroupSeparator); grouping = p.Read32(l + LocaleLayout.Grouping);
                for (uint i = 1; i < digits; i++) if (Boundary(ref p, grouping, i)) groups++;
            }
            uint lengthNumber = digits + groups * LocaleMemory.Length(ref p, separator) + (negative ? 1u : 0);
            if (!s.Left && !s.Zero) Pad(ref p, l, hook, s.Width, lengthNumber, 32);
            if (negative) p.Hook(hook, l, 45);
            if (!s.Left && s.Zero) Pad(ref p, l, hook, s.Width, lengthNumber, 48);
            Number(ref p, l, hook, high, low, radix, s.Type == 120, separator, grouping, 0);
            if (s.Left) Pad(ref p, l, hook, s.Width, lengthNumber, 32);
        }
        p.Free(table); p.Hook(hook, l, 0);
        // V40's system RawDoFmt patch returns stream+2 whenever a long D/U
        // conversion occurs, even with further arguments. Keep that observed
        // register result in the compatibility adapter only.
        return rawCompatibility && wideGrouped ? stream + 2 : end;
    }
    private static void Pad<T>(ref T p, uint l, uint hook, uint width, uint length, uint c) where T : struct, ILocalePlatform
    { while (width > length) { p.Hook(hook, l, c); width--; } }
    // Divide a 64-bit integer by a small radix using four 16-bit limbs. This
    // deliberately avoids compiler/runtime 64-bit division on a 68000.
    private static uint Divide(ref uint high, ref uint low, uint radix)
    {
        uint a = high >> 16, rem = a % radix, qh = (a / radix) << 16;
        a = (rem << 16) | (high & 65535); rem = a % radix; qh |= a / radix;
        a = (rem << 16) | (low >> 16); rem = a % radix; uint ql = (a / radix) << 16;
        a = (rem << 16) | (low & 65535); rem = a % radix; ql |= a / radix;
        high = qh; low = ql; return rem;
    }
    private static bool Boundary<T>(ref T p, uint grouping, uint remaining) where T : struct, ILocalePlatform
    {
        if (grouping == 0) return false;
        uint at = grouping, group = p.Read8(at++);
        while (group != 0 && group != 127 && group != 255)
        {
            if (remaining == group) return true;
            if (remaining < group) return false;
            remaining -= group;
            uint next = p.Read8(at); if (next != 0) { group = next; at++; }
        }
        return false;
    }
    private static void Number<T>(ref T p, uint l, uint hook, uint high, uint low, uint radix, bool upper, uint separator, uint grouping, uint right) where T : struct, ILocalePlatform
    {
        uint digit = Divide(ref high, ref low, radix);
        if (high != 0 || low != 0) Number(ref p, l, hook, high, low, radix, upper, separator, grouping, right + 1);
        p.Hook(hook, l, digit < 10 ? 48 + digit : (upper ? 65u : 97u) + digit - 10);
        if (right != 0 && separator != 0 && Boundary(ref p, grouping, right)) DateCore.Text(ref p, l, hook, separator);
    }
}
