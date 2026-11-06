using Amiga;
using CopperStart.Dos;

namespace CopperOS.Shell.Dos;

/// <summary>Provides a DOS-owned image for a child when its caller is not permanent.</summary>
internal static class PublicShellImageLease
{
    // The package gate branches over this fixed header. Its digest covers the
    // complete pre-packaging HUNK, including relocation records.
    private const uint HeaderBytes = 44;
    private const uint HeaderBranch = 0x6000002a;
    private const uint HeaderMagic = 0x43534846;
    private const uint HeaderVersion = 1;

    /// <summary>
    /// Returns a null image for a permanent Shell, or a matching LoadSeg image
    /// whose ownership can be transferred to CreateNewProc. The installed disk
    /// file is checked against the running package before it is executed.
    /// </summary>
    public static bool Prepare(ref CopperSharpNativeDosPlatform memory, APTR dos,
        APTR task, APTR entry, out BPTR image)
    {
        image = BPTR.Null;
        if (PublicShellBackgroundCore.IsPermanentShell(ref memory, dos, entry)) return true;
        var running = RunningImage(ref memory, task, entry);
        if (!HasHeader(ref memory, running))
        {
            PublicShellFileServices.SetError(dos, DOS.Error.InvalidResidentLibrary);
            return false;
        }
        var loaded = NativeForegroundDosVectors.LoadSeg(dos, Text("L:Shell-Seg"));
        if (loaded.IsNull) return false;
        if (!SameHeader(ref memory, running, loaded))
        {
            NativeForegroundDosVectors.UnLoadSeg(dos, loaded);
            PublicShellFileServices.SetError(dos, DOS.Error.InvalidResidentLibrary);
            return false;
        }
        image = loaded;
        return true;
    }

    /// <summary>Reclaims an image when CreateNewProc did not take ownership.</summary>
    public static void Abort(APTR dos, BPTR image)
    {
        if (image.IsNull) return;
        var error = PublicShellFileServices.GetError(dos);
        NativeForegroundDosVectors.UnLoadSeg(dos, image);
        PublicShellFileServices.SetError(dos, (DOS.Error)error);
    }

    private static BPTR RunningImage(ref CopperSharpNativeDosPlatform memory,
        APTR task, APTR entry)
    {
        if (!DosProcessCodec.IsMapped(ref memory, task) || entry.IsNull) return BPTR.Null;
        var array = DosProcessCodec.ReadSegmentList(ref memory, task).Address;
        if (array.IsNull || !memory.IsMapped(array, 20)) return BPTR.Null;
        // Shells place their segment in slot 4; a directly started Shell may
        // still have it in slot 3. Both are documented Process segment slots.
        for (var offset = 16; offset >= 12; offset -= 4)
        {
            var first = BPTR.FromRaw(memory.ReadUInt32(array, offset));
            var segment = first;
            while (segment.IsNotNull)
            {
                var address = segment.Address;
                if (address.Raw < 4 || address.Raw > uint.MaxValue - 4 ||
                    !memory.IsMapped(APTR.FromPointer(address.Raw - 4), 8)) break;
                var allocation = address.Raw - 4;
                var bytes = memory.ReadUInt32(APTR.FromPointer(allocation));
                if (bytes < 8 || allocation > uint.MaxValue - bytes) break;
                if (entry.Raw >= address.Raw + 4 && entry.Raw < allocation + bytes) return first;
                segment = BPTR.FromRaw(memory.ReadUInt32(address));
            }
        }
        return BPTR.Null;
    }

    private static bool HasHeader(ref CopperSharpNativeDosPlatform memory, BPTR image)
    {
        var segment = image.Address;
        if (segment.IsNull || segment.Raw < 4 || segment.Raw > uint.MaxValue - HeaderBytes - 4 ||
            !memory.IsMapped(APTR.FromPointer(segment.Raw - 4), HeaderBytes + 8)) return false;
        var bytes = memory.ReadUInt32(APTR.FromPointer(segment.Raw - 4));
        if (bytes < HeaderBytes + 8) return false;
        var code = APTR.FromPointer(segment.Raw + 4);
        return memory.ReadUInt32(code, 0) == HeaderBranch &&
            memory.ReadUInt32(code, 4) == HeaderMagic &&
            memory.ReadUInt32(code, 8) == HeaderVersion;
    }

    private static bool SameHeader(ref CopperSharpNativeDosPlatform memory, BPTR first, BPTR second)
    {
        if (!HasHeader(ref memory, first) || !HasHeader(ref memory, second)) return false;
        var a = APTR.FromPointer(first.Address.Raw + 4);
        var b = APTR.FromPointer(second.Address.Raw + 4);
        for (var offset = 12; offset < (int)HeaderBytes; offset += 4)
            if (memory.ReadUInt32(a, offset) != memory.ReadUInt32(b, offset)) return false;
        return true;
    }

    private static APTR Text(CString value) => APTR.FromPointer(CString.ToUInt32(value));
}
