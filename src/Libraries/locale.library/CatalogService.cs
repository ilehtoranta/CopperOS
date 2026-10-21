/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.Locale;

/// <summary>Catalog selection and cache. Caller holds the library semaphore.</summary>
public static class CatalogService
{
    public const uint TagBase = 0x80090000;
    public static uint Open<T>(ref T p, uint locale, uint name, uint tags, ref uint head) where T : struct, ILocalePlatform
    {
        uint descriptor = 0, at = tags;
        while (at != 0)
        {
            uint tag = p.Read32(at), data = p.Read32(at + 4); at += 8;
            if (tag == 0) break;
            if (tag == 2) { at = data; continue; }
            if (tag == 3) { at += data * 8; continue; }
            if (tag == TagBase + 6) descriptor = data;
        }
        for (uint attempt = 0; attempt < 2; attempt++)
        {
            uint root = OpenTranslation(ref p, locale, name, tags, ref head);
            if (root == 0 && p.LastError() == 103 && attempt == 0) { Flush(ref p, ref head); continue; }
            if (descriptor == 0) return root;
            uint wrapper = CatalogCore.BuiltIn(ref p, descriptor, root);
            if (wrapper == 0) Close(ref p, root);
            else { p.Error(0); p.Write32(wrapper + CatalogCore.Next, head); head = wrapper; return wrapper; }
            if (p.LastError() != 103 || attempt != 0) return 0;
            Flush(ref p, ref head);
        }
        return 0;
    }
    private static uint OpenTranslation<T>(ref T p, uint locale, uint name, uint tags, ref uint head) where T : struct, ILocalePlatform
    {
        // OC_BuiltInCodeSet (+2) does not request a translation encoding.
        // OC_CodeSet (+5) alone selects it, independent of tag order.
        uint builtIn = p.Literal(LocaleText.English), forced = 0, version = 0, codeSet = 0;
        bool convert = true;
        while (tags != 0)
        {
            uint tag = p.Read32(tags), data = p.Read32(tags + 4); tags += 8;
            if (tag == 0) break;
            if (tag == 1) continue;
            if (tag == 2) { tags = data; continue; }
            if (tag == 3) { tags += data * 8; continue; }
            if (tag == TagBase + 1) builtIn = data;
            else if (tag == TagBase + 3) version = data & 65535;
            else if (tag == TagBase + 4) forced = data;
            else if (tag == TagBase + 5) codeSet = data;
            else if (tag == TagBase + 7) convert = data != 0;
        }
        p.Error(0);
        if (name == 0) { p.Error(205); return 0; }
        uint size = LocaleMemory.Length(ref p, name) + 128;
        uint path = p.Allocate(size);
        if (path == 0) { p.Error(103); return 0; }
        // Explicit paths are useful to loaders; ordinary names, including
        // sys/c.catalog, are searched below in their language directories.
        bool explicitPath = false;
        for (uint at = name; p.Read8(at) != 0; at++) if (p.Read8(at) == 58) explicitPath = true;
        if (explicitPath)
        {
            uint direct = TryFile(ref p, name, 0, version, codeSet, convert, ref head);
            p.Free(path); return direct;
        }
        for (int preference = forced != 0 ? -1 : 0; preference < 10; preference++)
        {
            uint language = preference < 0 ? forced : p.Read32(locale + 8 + (uint)preference * 4);
            if (language == 0) continue;
            if (builtIn != 0 && LocaleMemory.Equal(ref p, builtIn, language)) { p.Free(path); p.Error(0); return 0; }
            // Language names from public locale records may be caller-owned.
            // Allocate to their actual length instead of imposing a path cap.
            uint needed = LocaleMemory.Length(ref p, language) + LocaleMemory.Length(ref p, name) + 32;
            if (needed > size) { p.Free(path); size = needed; path = p.Allocate(size); if (path == 0) { p.Error(103); return 0; } }
            uint result = 0, firstLocation = 0;
            for (uint attempt = 0; attempt < 4; attempt++)
            {
                if (attempt == 2) firstLocation = p.CatalogLocations() == 2 ? 1u : 0;
                uint location = (attempt & 1) ^ firstLocation;
                LocaleCore.Path(ref p, path, size, location == 0 ? p.Literal(LocaleText.ProgramCatalogs) : p.Literal(LocaleText.LocaleCatalogs), language, p.Literal(LocaleText.Slash));
                uint append = path + LocaleMemory.Length(ref p, path);
                LocaleMemory.Copy(ref p, name, append, LocaleMemory.Length(ref p, name) + 1);
                result = TryFile(ref p, path, language, version, codeSet, convert, ref head, attempt >= 2);
                if (result != 0) break;
                if (p.LastError() == 103) { p.Free(path); return 0; }
            }
            if (result != 0) { p.Free(path); p.Error(0); return result; }
        }
        p.Free(path); return 0;
    }
    private static uint TryFile<T>(ref T p, uint path, uint language, uint version, uint codeSet, bool convert, ref uint head, bool load = true) where T : struct, ILocalePlatform
    {
        for (uint c = head; c != 0; c = p.Read32(c + CatalogCore.Next))
        {
            if (!LocaleMemory.Equal(ref p, p.Read32(c + CatalogCore.Name), path) ||
                (version != 0 && p.Read16(c + 24) != version) ||
                p.Read32(c + CatalogCore.BuiltIns) != (convert ? 1u : 0) || (convert && p.Read32(c + 20) != codeSet)) continue;
            p.Write32(c + CatalogCore.References, p.Read32(c + CatalogCore.References) + 1); return c;
        }
        if (!load) return 0;
        uint file = p.LoadFile(path, out uint bytes);
        if (file == 0) return 0;
        uint catalog = CatalogCore.Read(ref p, file, bytes, path, language, version, codeSet, convert);
        if (catalog == 0) { p.Free(file); return 0; }
        p.Write32(catalog + CatalogCore.Next, head); head = catalog; return catalog;
    }
    public static void Close<T>(ref T p, uint catalog) where T : struct, ILocalePlatform
    {
        if (catalog == 0) return;
        p.Write32(catalog + CatalogCore.References, p.Read32(catalog + CatalogCore.References) - 1);
    }
    public static void Flush<T>(ref T p, ref uint head) where T : struct, ILocalePlatform
    {
        uint c = head, previous = 0;
        while (c != 0)
        {
            uint next = p.Read32(c + CatalogCore.Next);
            if (p.Read32(c + CatalogCore.References) == 0)
            { if (previous == 0) head = next; else p.Write32(previous + CatalogCore.Next, next); CatalogCore.Release(ref p, c); }
            else previous = c;
            c = next;
        }
    }
}
