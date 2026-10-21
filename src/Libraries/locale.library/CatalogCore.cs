/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.Locale;

/// <summary>IFF CTLG reader and immutable adaptive string index.</summary>
public static class CatalogCore
{
    // SDK Catalog prefix is 28 bytes. Index storage never changes after publication.
    public const uint Size = 72, File = 28, Index = 32, Count = 36, Minimum = 40,
        Dense = 44, References = 48, Next = 52, BuiltIns = 56, Name = 60, Converted = 64, Root = 68;

    /// <summary>O(1) dense lookup; O(log n) sparse lookup. No allocation, division, locks or string scanning.</summary>
    public static uint GetString<T>(ref T p, uint catalog, uint id, uint fallback) where T : struct, ILocalePlatform
    {
        if (catalog == 0) return fallback;
        uint index = p.Read32(catalog + Index), count = p.Read32(catalog + Count), result = 0;
        if (p.Read32(catalog + Dense) != 0)
        {
            uint slot = id - p.Read32(catalog + Minimum);
            if (slot < count) result = p.Read32(index + (slot << 2));
        }
        else
        {
            uint low = 0, high = count;
            while (low < high)
            {
                uint mid = low + ((high - low) >> 1), entry = index + (mid << 3), key = p.Read32(entry);
                if (id < key) high = mid;
                else if (id > key) low = mid + 1;
                else { result = p.Read32(entry + 4); break; }
            }
        }
        if (result != 0) return result;
        uint root = p.Read32(catalog + Root);
        // A per-open built-in wrapper gives translations priority, with the
        // same immutable index and no change to other users of the cache.
        if (root != 0) return GetString(ref p, root, id, fallback);
        return fallback;
    }

    public static uint Read<T>(ref T p, uint file, uint bytes, uint name, uint language, uint version, uint codeSet, bool convert = true) where T : struct, ILocalePlatform
    {
        if (!LocaleMemory.Form(ref p, file, bytes, 0x43544c47, out uint end)) { p.Error(212); return 0; }
        uint cursor = file + 12, strings = 0, stringBytes = 0, lang = 0, actualCodeSet = 0;
        ushort actualVersion = 0, revision = 0;
        while (cursor < end)
        {
            if (!LocaleMemory.Chunk(ref p, cursor, end, out uint id, out uint data, out uint size, out cursor)) { p.Error(212); return 0; }
            if (id == 0x53545253) { if (strings != 0) { p.Error(212); return 0; } strings = data; stringBytes = size; }
            else if (id == 0x4c414e47) { if (!LocaleMemory.Terminated(ref p, data, size)) { p.Error(212); return 0; } lang = data; }
            else if (id == 0x43534554) { if (size < 4) { p.Error(212); return 0; } actualCodeSet = p.Read32(data); }
            else if (id == 0x46564552)
            {
                if (!LocaleMemory.Terminated(ref p, data, size)) { p.Error(212); return 0; }
                uint at = data;
                // $VER: name version.revision; use the first numeric token after the name.
                while (p.Read8(at) != 0 && p.Read8(at) != 32) at++;
                while (p.Read8(at) == 32) at++;
                while (p.Read8(at) != 0 && p.Read8(at) != 32) at++;
                while (p.Read8(at) == 32) at++;
                uint v = Number(ref p, ref at); actualVersion = (ushort)v;
                if (p.Read8(at) == 46) { at++; revision = (ushort)Number(ref p, ref at); }
            }
        }
        if ((version != 0 && version != actualVersion) || codeSet > 2 || actualCodeSet > 2) { p.Error(212); return 0; }
        if (lang != 0 && language != 0 && !LocaleMemory.Equal(ref p, lang, language)) { p.Error(212); return 0; }
        uint count = 0, atString = strings, stop = strings + stringBytes, convertedBytes = 0;
        uint outputCodeSet = convert ? codeSet : actualCodeSet;
        bool materialize = convert && codeSet != actualCodeSet;
        while (atString < stop)
        {
            if (stop - atString < 8) { p.Error(212); return 0; }
            uint n = p.Read32(atString + 4);
            if (n > stop - atString - 8) { p.Error(212); return 0; }
            uint padded = (n + 3) & ~3u;
            if (padded < n || padded > stop - atString - 8) { p.Error(212); return 0; }
            uint pos = atString + 8, limit = pos + n, code; bool terminated = false;
            while (pos < limit)
            {
                if (!CharsetCore.Read(ref p, ref pos, limit, actualCodeSet, out code)) { p.Error(212); return 0; }
                uint width = CharsetCore.Width(code, outputCodeSet);
                if (convertedBytes > uint.MaxValue - width) { p.Error(103); return 0; }
                convertedBytes += width;
                if (code == 0) { terminated = true; break; }
            }
            // Classic CatComp lengths may exclude the NUL. Padding is not
            // part of the string, and a multiple-of-four length has no spare
            // byte. Materialize a terminated pool once when opening it.
            if (!terminated)
            {
                uint width = CharsetCore.Width(0, outputCodeSet);
                if (convertedBytes > uint.MaxValue - width) { p.Error(103); return 0; }
                convertedBytes += width; materialize = true;
            }
            atString += 8 + padded; count++;
        }
        uint nameLength = LocaleMemory.Length(ref p, name) + 1, languageLength = LocaleMemory.Length(ref p, language) + 1;
        uint catalog = p.Allocate(Size + nameLength + languageLength);
        if (catalog == 0) { p.Error(103); return 0; }
        p.Write32(catalog + File, file); p.Write32(catalog + Name, catalog + Size);
        LocaleMemory.Copy(ref p, name, catalog + Size, LocaleMemory.Length(ref p, name) + 1);
        p.Write32(catalog + 10, catalog + Size); // Node.ln_Name
        if (language != 0) LocaleMemory.Copy(ref p, language, catalog + Size + nameLength, languageLength);
        p.Write32(catalog + 16, lang != 0 ? lang : language != 0 ? catalog + Size + nameLength : 0);
        p.Write32(catalog + 20, convert ? codeSet : actualCodeSet); p.Write16(catalog + 24, actualVersion); p.Write16(catalog + 26, revision);
        p.Write32(catalog + References, 1);
        p.Write32(catalog + BuiltIns, convert ? 1u : 0);
        if (count != 0)
        {
            uint index = p.Allocate(count << 3);
            if (index == 0) { p.Free(catalog); p.Error(103); return 0; }
            uint converted = 0;
            if (materialize)
            {
                converted = p.Allocate(convertedBytes);
                if (converted == 0) { p.Free(index); p.Free(catalog); p.Error(103); return 0; }
                p.Write32(catalog + Converted, converted);
            }
            atString = strings;
            for (uint i = 0; i < count; i++)
            {
                p.Write32(index + (i << 3), p.Read32(atString));
                uint text = atString + 8;
                if (converted != 0)
                {
                    uint pos = text, limit = text + p.Read32(atString + 4), code = 0;
                    text = converted;
                    while (pos < limit)
                    { CharsetCore.Read(ref p, ref pos, limit, actualCodeSet, out code); if (code == 0) break; CharsetCore.Write(ref p, ref converted, code, outputCodeSet); }
                    CharsetCore.Write(ref p, ref converted, 0, outputCodeSet);
                }
                p.Write32(index + (i << 3) + 4, text);
                atString += 8 + ((p.Read32(atString + 4) + 3) & ~3u);
            }
            Sort(ref p, index, count);
            // Duplicate IDs retain the first occurrence in the file.
            uint unique = 0;
            for (uint i = 0; i < count; i++)
            {
                uint entry = index + (i << 3);
                if (unique != 0 && p.Read32(index + ((unique - 1) << 3)) == p.Read32(entry)) continue;
                p.Write32(index + (unique << 3), p.Read32(entry));
                p.Write32(index + (unique << 3) + 4, p.Read32(entry + 4)); unique++;
            }
            count = unique;
            uint minimum = p.Read32(index), maximum = p.Read32(index + ((count - 1) << 3)), span = maximum - minimum;
            if (span < count * 2 && span < 0x3ffffffe)
            {
                uint dense = p.Allocate((span + 1) << 2);
                if (dense != 0)
                {
                    for (uint i = 0; i < count; i++)
                        p.Write32(dense + ((p.Read32(index + (i << 3)) - minimum) << 2), p.Read32(index + (i << 3) + 4));
                    p.Free(index); index = dense; count = span + 1;
                    p.Write32(catalog + Dense, 1); p.Write32(catalog + Minimum, minimum);
                }
                // Memory pressure keeps the already-valid sparse index.
            }
            p.Write32(catalog + Index, index); p.Write32(catalog + Count, count);
        }
        return catalog;
    }
    private static uint Number<T>(ref T p, ref uint at) where T : struct, ILocalePlatform
    { uint n = 0; while (p.Read8(at) >= 48 && p.Read8(at) <= 57) n = n * 10 + p.Read8(at++) - 48u; return n; }
    private static bool Less<T>(ref T p, uint a, uint b) where T : struct, ILocalePlatform
    { uint x = p.Read32(a), y = p.Read32(b); return x < y || (x == y && p.Read32(a + 4) < p.Read32(b + 4)); }
    private static void Swap<T>(ref T p, uint a, uint b) where T : struct, ILocalePlatform
    { uint id = p.Read32(a), text = p.Read32(a + 4); p.Write32(a, p.Read32(b)); p.Write32(a + 4, p.Read32(b + 4)); p.Write32(b, id); p.Write32(b + 4, text); }
    private static void Sift<T>(ref T p, uint index, uint root, uint count) where T : struct, ILocalePlatform
    {
        while (root < count / 2)
        {
            uint child = root * 2 + 1;
            if (child + 1 < count && Less(ref p, index + (child << 3), index + ((child + 1) << 3))) child++;
            if (!Less(ref p, index + (root << 3), index + (child << 3))) break;
            Swap(ref p, index + (root << 3), index + (child << 3)); root = child;
        }
    }
    private static void Sort<T>(ref T p, uint index, uint count) where T : struct, ILocalePlatform
    {
        for (uint i = count / 2; i != 0;) Sift(ref p, index, --i, count);
        for (uint i = count; i > 1;) { --i; Swap(ref p, index, index + (i << 3)); Sift(ref p, index, 0, i); }
    }
    public static void Release<T>(ref T p, uint catalog) where T : struct, ILocalePlatform
    {
        uint root = p.Read32(catalog + Root);
        if (root != 0) CatalogService.Close(ref p, root);
        p.Free(p.Read32(catalog + Index)); p.Free(p.Read32(catalog + Converted)); p.Free(p.Read32(catalog + File)); p.Free(catalog);
    }
    public static uint BuiltIn<T>(ref T p, uint descriptor, uint root) where T : struct, ILocalePlatform
    {
        uint type = p.Read32(descriptor), bytes = p.Read32(descriptor + 4), data = p.Read32(descriptor + 8), count = 0;
        if (type > 1 || bytes > int.MaxValue || data > uint.MaxValue - bytes) { p.Error(212); return 0; }
        uint at = data, end = data + bytes;
        while (at < end)
        {
            if (type == 0) { if (end - at < 8) { p.Error(212); return 0; } at += 8; }
            else { if (end - at < 6) { p.Error(212); return 0; } uint n = p.Read16(at + 4); if (n == 0 || n > end - at - 6 || !LocaleMemory.Terminated(ref p, at + 6, n)) { p.Error(212); return 0; } at += 6 + n; }
            count++;
        }
        uint c = p.Allocate(Size);
        if (c == 0) { p.Error(103); return 0; }
        uint index = count == 0 ? 0 : p.Allocate(count << 3);
        if (count != 0 && index == 0) { p.Free(c); p.Error(103); return 0; }
        at = data;
        for (uint i = 0; i < count; i++)
        {
            uint id = p.Read32(at), text = type == 0 ? p.Read32(at + 4) : at + 6;
            p.Write32(index + (i << 3), id); p.Write32(index + (i << 3) + 4, GetString(ref p, root, id, text));
            at += type == 0 ? 8u : 6u + p.Read16(at + 4);
        }
        Sort(ref p, index, count); p.Write32(c + Index, index); p.Write32(c + Count, count);
        p.Write32(c + Root, root); p.Write32(c + BuiltIns, 2); p.Write32(c + References, 1);
        if (root != 0) LocaleMemory.Copy(ref p, root + 16, c + 16, 12);
        return c;
    }
}
