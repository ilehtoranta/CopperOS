/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

internal interface IMuiClassicMenuPlatform : IMuiAllocationPlatform
{
    bool IsMenustripObject(APTR value);
    APTR CreateClassicMenustrip(APTR newMenu);
    void DisposeClassicMenustrip(APTR strip);
}

// Classic Application_Menu accepts a GadTools NewMenu vector. Translate only
// at the classic native service boundary; the MorphOS object setter remains strict.
internal static class MuiClassicApplicationMenu
{
    internal const uint Menu = 0x80420E1F;
    internal const uint Menustrip = 0x804252D9;
    internal static bool Prepare<T>(ref T p, APTR tags, out APTR prepared,
        out uint bytes, out APTR generated) where T : struct, IMuiClassicMenuPlatform
    {
        prepared = tags; bytes = 0; generated = APTR.Null;
        var cursor = new MuiAslTagItemCursor { Base = tags };
        uint steps = 0, count = 0, menus = 0;
        while (true)
        {
            if (!Next(ref p, ref cursor, ref steps, out var item)) return false;
            if (item.Tag == 0) break;
            count++;
            if (item.Tag == Menu) menus++;
        }
        if (menus == 0) return true;
        // An initialize-only menu relationship has a single cleanup owner.
        if (menus != 1) return false;
        bytes = (count + 1) * MuiAslTagItemRecord.Size;
        prepared = p.Allocate(bytes, MuiHeadlessLayout.AllocationFlags);
        if (prepared.IsNull) { bytes = 0; return false; }
        cursor = new MuiAslTagItemCursor { Base = tags }; steps = 0; menus = 0;
        for (uint i = 0; i <= count; i++)
        {
            if (!Next(ref p, ref cursor, ref steps, out var item) ||
                (item.Tag == 0) != (i == count) ||
                !MuiAslTagItemVectorCodec.TryWrite(ref p,
                    new MuiAslTagItemCursor { Base = prepared, Index = i }, item))
            { Finish(ref p, prepared, bytes, generated, false); bytes = 0; prepared = APTR.Null; return false; }
            if (item.Tag == Menu) menus++;
        }
        if (menus != 1)
        { Finish(ref p, prepared, bytes, generated, false); bytes = 0; prepared = APTR.Null; return false; }
        // Finish copying before a factory callback can reenter caller memory.
        for (uint i = 0; i < count; i++)
        {
            var at = new MuiAslTagItemCursor { Base = prepared, Index = i };
            if (!MuiAslTagItemVectorCodec.TryRead(ref p, at, out var item))
            { Finish(ref p, prepared, bytes, generated, false); bytes = 0; prepared = APTR.Null; return false; }
            if (item.Tag != Menu) continue;
            if (item.Data == 0) item.Tag = MuiAslTagListCore.TagIgnore;
            else if (!p.IsMenustripObject(APTR.FromPointer(item.Data)))
            {
                generated = p.CreateClassicMenustrip(APTR.FromPointer(item.Data));
                if (generated.IsNull)
                { Finish(ref p, prepared, bytes, generated, false); bytes = 0; prepared = APTR.Null; return false; }
                item.Data = generated.Raw;
                item.Tag = Menustrip;
            }
            if (!MuiAslTagItemVectorCodec.TryWrite(ref p, at, item))
            { Finish(ref p, prepared, bytes, generated, false); generated = APTR.Null; bytes = 0; prepared = APTR.Null; return false; }
        }
        return true;
    }
    internal static void Finish<T>(ref T p, APTR prepared, uint bytes,
        APTR generated, bool success) where T : struct, IMuiClassicMenuPlatform
    {
        if (!success && generated.IsNotNull) p.DisposeClassicMenustrip(generated);
        if (bytes != 0) p.Free(prepared, bytes);
    }
    internal static bool Next<T>(ref T p, ref MuiAslTagItemCursor cursor,
        ref uint steps, out MuiAslTagItemRecord item) where T : struct, IMuiGuestMemory
    {
        item = default;
        while (cursor.Base.IsNotNull && steps++ < MuiAslTagListCore.MaximumSteps)
        {
            if (!MuiAslTagItemVectorCodec.TryRead(ref p, cursor, out item)) return false;
            if (item.Tag == 0) return true;
            if (item.Tag == MuiAslTagListCore.TagMore)
            { cursor = new MuiAslTagItemCursor { Base = APTR.FromPointer(item.Data) }; continue; }
            var advance = item.Tag == MuiAslTagListCore.TagSkip ? item.Data + 1u : 1u;
            if (advance == 0 || !MuiAslTagItemVectorCodec.TryAdvance(ref cursor, advance)) return false;
            if (item.Tag != MuiAslTagListCore.TagIgnore && item.Tag != MuiAslTagListCore.TagSkip) return true;
        }
        item = default;
        return cursor.Base.IsNull;
    }
}
