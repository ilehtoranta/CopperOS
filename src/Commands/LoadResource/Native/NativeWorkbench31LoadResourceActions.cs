using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Resource actions from Workbench 3.1 LoadResource HUNK2 0x0694-0x091c.
/// The caller serializes the opened-resource registry, retains the DOS V39,
/// Utility V39, Graphics V39 and optional Locale V38 leases, and owns the
/// installed one-shot LoadSeg cache. The result is the source's DOS error,
/// with zero meaning that processing can continue to the next match. The
/// messageCatalogState argument is the caller-owned four-byte state slot,
/// not a catalog handle.
/// </summary>
public static class NativeWorkbench31LoadResourceActions
{
    private struct ThreeLongs
    {
#pragma warning disable CS0649 // Written using explicit native byte offsets.
        public uint A, B, C;
#pragma warning restore CS0649

        public static APTR AddressOf(ref ThreeLongs value) =>
            throw new System.NotSupportedException(
                "ThreeLongs.AddressOf is lowered by CopperSharp.");
    }

    public static int Load(APTR registry, CString name, bool keepLocked,
        APTR messageCatalogState)
    {
        if (NativeWorkbench31LoadResourceRegistry.Find(registry, name).IsNotNull)
        {
            NativeWorkbench31LoadResourceMessages.PrintName(messageCatalogState,
                0xc358, name);
            return 0;
        }

        var segment = DOS.LoadSeg(name).GetValueOrDefault();
        if (segment.IsNull)
        {
            var error = unchecked((int)DOS.IoErr());
            if (error == 121 || error == 205 || error == 212)
                return LoadCatalog(registry, name, keepLocked, messageCatalogState,
                    error);
            NativeWorkbench31LoadResourceMessages.PrintName(messageCatalogState,
                0xc359, name);
            return error;
        }

        if (!NativeWorkbench31LoadResourceLoadSeg.TryCache(name, segment))
        {
            DOS.UnLoadSeg(segment);
            return 103;
        }

        var kind = Classify(segment);
        if (kind == 1)
        {
            var result = LoadLibrary(registry, name, keepLocked, messageCatalogState);
            NativeWorkbench31LoadResourceLoadSeg.TryRemove(segment);
            return result;
        }
        if (kind == 2)
        {
            // A device is left in the one-shot SegList cache, even without
            // LOCK. The source neither opens a device nor adds a type-1 record.
            return 0;
        }
        if (kind == 3)
        {
            var result = LoadFont(registry, name, keepLocked, messageCatalogState);
            NativeWorkbench31LoadResourceLoadSeg.TryRemove(segment);
            return result;
        }

        NativeWorkbench31LoadResourceLoadSeg.TryRemove(segment);
        NativeWorkbench31LoadResourceMessages.PrintName(messageCatalogState,
            0xc35b, name);
        return 212;
    }

    private static int LoadLibrary(APTR registry, CString name,
        bool keepLocked, APTR messageCatalogState)
    {
        var library = Exec.OpenLibraryRaw(name, 0);
        if (library.IsNotNull)
        {
            if (!keepLocked ||
                !NativeWorkbench31LoadResourceRegistry.TryAdd(registry, name,
                    0, library))
                Exec.CloseLibrary(library);
            return 0;
        }

        NativeWorkbench31LoadResourceMessages.PrintName(messageCatalogState,
            0xc359, name);
        // Unlike the ordinary LoadSeg error path, the source samples IoErr
        // after its diagnostic. Do not move this capture before printing.
        return unchecked((int)DOS.IoErr());
    }

    private static int LoadCatalog(APTR registry, CString name,
        bool keepLocked, APTR messageCatalogState, int loadError)
    {
        if (Locale.LocaleLibraryBase.IsNull)
            return loadError;

        var storage = default(ThreeLongs);
        var tags = ThreeLongs.AddressOf(ref storage);
        APTR.WriteUInt32(tags, 0, 0x80090001); // OC_BuiltInLanguage.
        APTR.WriteUInt32(tags, 4, 0);
        APTR.WriteUInt32(tags, 8, 0); // TAG_DONE.
        var catalog = APTR.FromPointer(Locale.OpenCatalogA(0, name, tags.Raw));
        if (catalog.IsNotNull)
        {
            if (!keepLocked ||
                !NativeWorkbench31LoadResourceRegistry.TryAdd(registry, name,
                    3, catalog))
                Locale.CloseCatalog(catalog.Raw);
            return 0;
        }

        NativeWorkbench31LoadResourceMessages.PrintName(messageCatalogState,
            0xc35b, name);
        return unchecked((int)DOS.IoErr());
    }

    private static int LoadFont(APTR registry, CString name,
        bool keepLocked, APTR messageCatalogState)
    {
        var original = APTR.FromPointer(CString.ToUInt32(name));
        var length = 0;
        while (APTR.ReadUInt8(original, length) != 0)
            length++;
        var split = length;
        while (split > 0 && APTR.ReadUInt8(original, split) != (byte)'/' &&
            APTR.ReadUInt8(original, split) != (byte)':')
            split--;

        var fontName = Exec.AllocVec(unchecked((uint)(length + 5)), 0);
        if (fontName.IsNull)
            return 103;

        var index = 0;
        while (index <= length)
        {
            APTR.WriteUInt8(fontName, index, APTR.ReadUInt8(original, index));
            index++;
        }
        // The source replaces the final separator (or index zero) with .font.
        APTR.WriteUInt8(fontName, split, (byte)'.');
        APTR.WriteUInt8(fontName, split + 1, (byte)'f');
        APTR.WriteUInt8(fontName, split + 2, (byte)'o');
        APTR.WriteUInt8(fontName, split + 3, (byte)'n');
        APTR.WriteUInt8(fontName, split + 4, (byte)'t');
        APTR.WriteUInt8(fontName, split + 5, 0);

        var result = 0;
        var diskfont = Exec.OpenLibraryRaw(CString.FromLiteral("diskfont.library"), 37);
        if (diskfont.IsNotNull)
        {
            var storage = default(ThreeLongs);
            var scratch = ThreeLongs.AddressOf(ref storage);
            // LONG parsed size at +0; packed TextAttr at +4, with name +0,
            // YSize +4, style +6 and flags +7. Explicit stores avoid CLR packing.
            // The original ignores StrToLong's return and leaves the size
            // uninitialized on failure. Our cleared scratch makes that
            // otherwise undefined malformed-suffix case deterministic.
            DOS.StrToLong(CString.FromPointer(DOS.FilePart(name).Raw), scratch);
            APTR.WriteUInt32(scratch, 4, fontName.Raw);
            APTR.WriteUInt16(scratch, 8,
                unchecked((ushort)APTR.ReadUInt32(scratch, 0)));
            APTR.WriteUInt8(scratch, 10, 0);
            APTR.WriteUInt8(scratch, 11, 2); // FPF_DISKFONT.

            var previousDiskfontBase = Diskfont.DiskfontLibraryBase;
            Diskfont.DiskfontLibraryBase = diskfont;
            var font = APTR.FromPointer(Diskfont.OpenDiskFont(scratch.Raw + 4));
            Diskfont.DiskfontLibraryBase = previousDiskfontBase;
            if (font.IsNotNull)
            {
                if (!keepLocked ||
                    !NativeWorkbench31LoadResourceRegistry.TryAdd(registry, name,
                        2, font))
                    Graphics.CloseFont(font.Raw);
            }
            else
            {
                NativeWorkbench31LoadResourceMessages.PrintName(messageCatalogState,
                    0xc359, CString.FromPointer(fontName.Raw));
                result = unchecked((int)DOS.IoErr());
            }
            Exec.CloseLibrary(diskfont);
        }
        else
        {
            NativeWorkbench31LoadResourceMessages.PrintName(messageCatalogState,
                0xc35a, CString.FromLiteral(""));
            result = unchecked((int)DOS.IoErr());
        }

        Exec.FreeVec(fontName);
        return result;
    }

    private static int Classify(BPTR segment)
    {
        var resident = FindResident(segment);
        if (resident.IsNotNull)
        {
            var type = APTR.ReadUInt8(resident, 12);
            if (type == 9) return 1; // NT_LIBRARY.
            if (type == 3) return 2; // NT_DEVICE.
            return 0;
        }

        var fontHeader = APTR.FromPointer(segment.Address.Raw + 8);
        if (APTR.ReadUInt8(fontHeader, 8) != 12) // NT_FONT.
            return 0;
        APTR.WriteUInt16(fontHeader, 14, 0x0f80); // DFH_ID.
        return 3;
    }

    private static APTR FindResident(BPTR firstSegment)
    {
        var nextSegment = firstSegment;
        while (nextSegment.IsNotNull)
        {
            var candidate = nextSegment.Address;
            nextSegment = BPTR.FromRaw(APTR.ReadUInt32(candidate, 0));
            var remaining = APTR.ReadUInt32(candidate, -4) >> 1;
            while (remaining != 0)
            {
                if (APTR.ReadUInt16(candidate, 0) == 0x4afc)
                {
                    if (APTR.ReadUInt32(candidate, 2) == candidate.Raw)
                        return candidate;
                    // Source 0x0358 rewinds its postincrement before the
                    // self-pointer check and does not advance on mismatch.
                    // A false matchword exhausts this segment's counter.
                }
                else
                    candidate = APTR.FromPointer(candidate.Raw + 2);
                remaining--;
            }
        }
        return APTR.Null;
    }
}
