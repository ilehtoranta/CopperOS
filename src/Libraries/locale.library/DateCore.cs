/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.Locale;

public struct LocaleDate
{
    public uint Year, Month, Day, Weekday, YearDay, Hour, Minute, Second;
}

public static class DateCore
{
    public static bool Leap(uint year) => year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
    public static uint MonthDays(uint year, uint month) => month == 2 ? (Leap(year) ? 29u : 28u) :
        month == 4 || month == 6 || month == 9 || month == 11 ? 30u : 31u;
    public static LocaleDate Decode(uint days, uint minutes, uint ticks)
    {
        var d = default(LocaleDate);
        d.Weekday = days % 7; // 1978-01-01 was Sunday
        // Move to 2000-01-01 before splitting Gregorian 400-year cycles.
        uint n = days + 722450; // days since 0000-01-01 (proleptic Gregorian)
        d.Year = (n / 146097) * 400; n %= 146097;
        while (n >= (Leap(d.Year) ? 366u : 365u)) { n -= Leap(d.Year) ? 366u : 365u; d.Year++; }
        d.YearDay = n + 1; d.Month = 1;
        while (n >= MonthDays(d.Year, d.Month)) { n -= MonthDays(d.Year, d.Month); d.Month++; }
        d.Day = n + 1; d.Hour = minutes / 60; d.Minute = minutes % 60; d.Second = ticks / 50;
        return d;
    }
    public static bool Encode(LocaleDate d, out uint days)
    {
        days = 0;
        if (d.Year < 1978 || d.Year > 9999 || d.Month < 1 || d.Month > 12 || d.Day < 1 || d.Day > MonthDays(d.Year, d.Month) ||
            d.Hour > 23 || d.Minute > 59 || d.Second > 59) return false;
        uint y = d.Year - 1;
        uint total = 365 * d.Year + y / 4 - y / 100 + y / 400 + 1;
        for (uint m = 1; m < d.Month; m++) total += MonthDays(d.Year, m);
        days = total + d.Day - 1 - 722450; return true;
    }
    public static void Format<T>(ref T p, uint locale, uint format, uint stamp, uint hook) where T : struct, ILocalePlatform
    {
        if (format == 0) return;
        var d = Decode(p.Read32(stamp), p.Read32(stamp + 4), p.Read32(stamp + 8));
        FormatTemplate(ref p, locale, format, d, hook);
        p.Hook(hook, locale, 0);
    }
    public static void Text<T>(ref T p, uint locale, uint hook, uint text) where T : struct, ILocalePlatform
    { if (text != 0) while (p.Read8(text) != 0) p.Hook(hook, locale, p.Read8(text++)); }
    public static void Number<T>(ref T p, uint locale, uint hook, uint n, uint width, uint pad) where T : struct, ILocalePlatform
    {
        uint power = 1, digits = 1;
        while (power <= n / 10) { power *= 10; digits++; }
        while (width > digits) { p.Hook(hook, locale, pad); width--; }
        do { p.Hook(hook, locale, 48 + n / power); n %= power; power /= 10; } while (power != 0);
    }
    private static void FormatTemplate<T>(ref T p, uint l, uint format, LocaleDate d, uint hook) where T : struct, ILocalePlatform
    {
        while (p.Read8(format) != 0)
        {
            uint c = p.Read8(format++);
            if (c != 37) { p.Hook(hook, l, c); continue; }
            c = p.Read8(format++); if (c == 0) break;
            uint nested = 0, text = 0, n = 0, width = 0, pad = 48; bool numeric = true;
            switch (c)
            {
                case 97: text = LocaleCore.String(ref p, l, 8 + d.Weekday); numeric = false; break;
                case 65: text = LocaleCore.String(ref p, l, 1 + d.Weekday); numeric = false; break;
                case 98: case 104: text = LocaleCore.String(ref p, l, 26 + d.Month); numeric = false; break;
                case 66: text = LocaleCore.String(ref p, l, 14 + d.Month); numeric = false; break;
                case 99: nested = p.Literal(LocaleText.DateTimeTemplate); break;
                case 67: nested = p.Literal(LocaleText.LongDateTimeTemplate); break;
                case 68: case 120: nested = p.Literal(LocaleText.DefaultShortDate); break;
                case 70: nested = p.Literal(LocaleText.IsoDateTemplate); break; // MorphOS format extension, same register ABI
                case 100: n = d.Day; width = 2; break;
                case 101: n = d.Day; width = 2; pad = 32; break;
                case 72: n = d.Hour; width = 2; break;
                case 113: n = d.Hour; break;
                case 73: case 81: n = d.Hour % 12; if (n == 0) n = 12; width = c == 73 ? 2u : 0; break;
                case 106: n = d.YearDay; width = 3; break;
                case 109: n = d.Month; width = 2; break;
                case 77: n = d.Minute; width = 2; break;
                case 110: p.Hook(hook, l, 10); numeric = false; break;
                case 112: text = LocaleCore.String(ref p, l, d.Hour < 12 ? 41u : 42u); numeric = false; break;
                case 114: nested = p.Literal(LocaleText.TwelveHourTemplate); break;
                case 82: nested = p.Literal(LocaleText.HoursMinutesTemplate); break;
                case 83: n = d.Second; width = 2; break;
                case 116: p.Hook(hook, l, 9); numeric = false; break;
                case 84: case 88: nested = p.Literal(LocaleText.HoursMinutesSecondsTemplate); break;
                case 85: n = (d.YearDay + 6 - d.Weekday) / 7; width = 2; break;
                case 119: n = d.Weekday; break;
                case 87: n = (d.YearDay + 6 - ((d.Weekday + 6) % 7)) / 7; width = 2; break;
                case 121: n = d.Year % 100; width = 2; break;
                case 89: n = d.Year; width = 4; break;
                case 90: numeric = false; break;
                default: p.Hook(hook, l, c); numeric = false; break;
            }
            if (nested != 0) FormatTemplate(ref p, l, nested, d, hook);
            else if (text != 0) Text(ref p, l, hook, text);
            else if (numeric) Number(ref p, l, hook, n, width, pad);
        }
    }
    private struct Input { public uint Current; public bool Loaded; }
    private static uint Peek<T>(ref T p, uint l, uint hook, ref Input input) where T : struct, ILocalePlatform
    { if (!input.Loaded) { input.Current = p.Hook(hook, l, 0); input.Loaded = true; } return input.Current; }
    private static void Consume(ref Input input) { input.Loaded = false; }
    private static bool Digits<T>(ref T p, uint l, uint hook, ref Input input, uint max, out uint value) where T : struct, ILocalePlatform
    {
        value = 0; uint n = 0;
        while (n < max) { uint c = Peek(ref p, l, hook, ref input); if (c < 48 || c > 57) break; value = value * 10 + c - 48; Consume(ref input); n++; }
        return n != 0;
    }
    // Match a localized word without buffering the input or allocating. The
    // candidates for each field have disjoint prefixes once fully consumed.
    private static bool Word<T>(ref T p, uint l, uint hook, ref Input input, uint first, uint count, out uint match) where T : struct, ILocalePlatform
    {
        uint candidates = (1u << (int)count) - 1, position = 0;
        match = 0;
        while (candidates != 0)
        {
            uint c = Peek(ref p, l, hook, ref input), next = 0;
            for (uint i = 0; i < count; i++)
            {
                if ((candidates & (1u << (int)i)) == 0) continue;
                uint text = LocaleCore.String(ref p, l, first + i), at = p.Read8(text + position);
                if (at == 0) { match = i; return true; }
                if (CharacterCore.Operation(ref p, l, 0, at) == CharacterCore.Operation(ref p, l, 0, c)) next |= 1u << (int)i;
            }
            if (next == 0 || c == 0) return false;
            candidates = next; Consume(ref input); position++;
        }
        return false;
    }
    // Legacy DOS options: trailing input (1), optional time suffix (2),
    // ignore meridiem (4), and numeric/abbreviated month alternatives (8).
    // Public ParseDate uses the strict default.
    public static bool Parse<T>(ref T p, uint l, uint stamp, uint format, uint hook, uint options = 0) where T : struct, ILocalePlatform
    {
        if (format == 0) return false;
        var input = default(Input); var d = default(LocaleDate);
        d.Year = 1978; d.Month = 1; d.Day = 1;
        bool twelve = false; uint ampm = 0; bool hasAmPm = false;
        while (p.Read8(format) != 0)
        {
            uint c = p.Read8(format++);
            if (c != 37)
            {
                if (Peek(ref p, l, hook, ref input) != c)
                { if ((options & 2) != 0 && (c == 58 || c == 32)) break; return false; }
                Consume(ref input); continue;
            }
            c = p.Read8(format++); uint n;
            if (c == 97 || c == 65 || c == 98 || c == 66 || c == 104 || c == 112)
            {
                if ((options & 8) != 0 && (c == 98 || c == 66 || c == 104) && Peek(ref p, l, hook, ref input) >= 48 && Peek(ref p, l, hook, ref input) <= 57)
                { if (!Digits(ref p, l, hook, ref input, 2, out n)) return false; d.Month = n; continue; }
                uint first = c == 97 ? 8u : c == 65 ? 1u : c == 66 ? 15u : c == 112 ? 41u : 27u;
                if (!Word(ref p, l, hook, ref input, first, (c == 97 || c == 65) ? 7u : c == 112 ? 2u : 12u, out n)) return false;
                if (c == 98 || c == 66 || c == 104) d.Month = n + 1;
                if (c == 112) { ampm = n; hasAmPm = true; }
                continue;
            }
            if (c == 37) { if (Peek(ref p, l, hook, ref input) != 37) return false; Consume(ref input); continue; }
            if (c == 101) while (Peek(ref p, l, hook, ref input) == 32) Consume(ref input);
            if ((options & 8) != 0 && c == 109 && Peek(ref p, l, hook, ref input) > 57)
            { if (!Word(ref p, l, hook, ref input, 27, 12, out n)) return false; d.Month = n + 1; continue; }
            if (c != 100 && c != 101 && c != 72 && c != 73 && c != 81 && c != 109 && c != 77 && c != 83 && c != 121 && c != 89) return false;
            if (!Digits(ref p, l, hook, ref input, c == 89 ? 4u : 2u, out n)) return false;
            if (c == 100 || c == 101) d.Day = n;
            else if (c == 72) d.Hour = n;
            else if (c == 73 || c == 81) { if ((options & 4) != 0) d.Hour = n; else { if (n == 0 || n > 12) return false; d.Hour = n % 12; twelve = true; } }
            else if (c == 109) d.Month = n;
            else if (c == 77) d.Minute = n;
            else if (c == 83) d.Second = n >= 60 && (options & 2) != 0 ? 0 : n;
            else if (c == 121) d.Year = n < 78 ? 2000 + n : 1900 + n;
            else d.Year = n;
        }
        if ((options & 1) == 0 && Peek(ref p, l, hook, ref input) != 0) return false;
        if (twelve && hasAmPm && (options & 4) == 0) d.Hour += ampm * 12;
        if (!Encode(d, out uint days)) return false;
        if (stamp != 0) { p.Write32(stamp, days); p.Write32(stamp + 4, d.Hour * 60 + d.Minute); p.Write32(stamp + 8, d.Second * 50); }
        return true;
    }
}
