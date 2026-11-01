/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

// A call-local guest-memory transaction. The native generic platform itself
// must remain a four-byte carrier; this state is never embedded in that value.
internal static class MuiNativeBuiltinMinMax
{
    internal const uint PassSize = 20;
    private const int Head = 0, Current = 4, Active = 8, Reliable = 12, Depth = 16;
    private const uint NodeSize = 24;
    private const int Next = 0, Object = 4, Generation = 8, Bounds = 12;

    internal static bool Begin<T>(ref T p, APTR pass, APTR state, APTR root)
        where T : struct, IMuiLayoutPlatform
    {
        if (!p.IsMapped(pass, PassSize)) return false;
        End(ref p, pass);
        p.WriteUInt32(pass, Active, 1); p.WriteUInt32(pass, Reliable, 1);
        if (MeasureTree(ref p, pass, state, root, 0)) return true;
        End(ref p, pass);
        return false;
    }

    internal static void Invalidate<T>(ref T p, APTR pass) where T : struct, IMuiHeadlessPlatform
    {
        if (pass.IsNotNull && p.IsMapped(pass, PassSize) && p.ReadUInt32(pass, Active) != 0)
            p.WriteUInt32(pass, Reliable, 0);
    }

    internal static void End<T>(ref T p, APTR pass) where T : struct, IMuiHeadlessPlatform
    {
        if (pass.IsNull || !p.IsMapped(pass, PassSize)) return;
        var node = APTR.FromPointer(p.ReadUInt32(pass, Head));
        p.Clear(pass, PassSize);
        for (uint count = 0; node.IsNotNull && count < MuiHeadlessLayout.MaximumTraversal; count++)
        {
            var next = APTR.FromPointer(p.ReadUInt32(node, Next));
            p.Free(node, NodeSize); node = next;
        }
    }

    internal static bool TryRead<T>(ref T p, APTR pass, APTR state, APTR obj,
        out MuiMinMaxValues values) where T : struct, IMuiLayoutPlatform
    {
        values = default;
        if (pass.IsNull || !p.IsMapped(pass, PassSize) || p.ReadUInt32(pass, Active) == 0 ||
            p.ReadUInt32(pass, Current) == obj.Raw) return false;
        if (p.ReadUInt32(pass, Reliable) != 0)
        {
            var node = APTR.FromPointer(p.ReadUInt32(pass, Head));
            for (uint count = 0; node.IsNotNull && count < MuiHeadlessLayout.MaximumTraversal; count++)
            {
                if (p.ReadUInt32(node, Object) == obj.Raw)
                {
                    var record = MuiHeadlessObjectCore.FindObject(ref p, state, obj);
                    return record.IsNotNull && MuiHeadlessObjectCodec.TryRead(ref p, record, out var live) &&
                        live.Generation == p.ReadUInt32(node, Generation) &&
                        MuiMinMaxRecordCodec.TryReadRecord(ref p, APTR.FromPointer(node.Raw + Bounds), out values);
                }
                node = APTR.FromPointer(p.ReadUInt32(node, Next));
            }
        }
        // Callbacks invalidate the snapshot. Fall back to bounded live
        // composite measurement instead of returning stale parent bounds.
        var collection = MuiListCore.Classify(ref p, state, obj);
        var control = MuiCommonControlCore.Classify(ref p, state, obj);
        if (collection != MuiCollectionClass.List && collection != MuiCollectionClass.Listview &&
            (MuiCommonControlCore.IsAreaControlClass(control) || !MuiGroupChangeCore.IsGroupObject(ref p, state, obj))) return false;
        var depth = p.ReadUInt32(pass, Depth);
        if (depth >= 64) return true;
        var previous = p.ReadUInt32(pass, Current);
        p.WriteUInt32(pass, Current, obj.Raw); p.WriteUInt32(pass, Depth, depth + 1);
        var success = Calculate(ref p, state, obj, out values);
        p.WriteUInt32(pass, Depth, depth); p.WriteUInt32(pass, Current, previous);
        if (!success) values = default;
        return true;
    }

    private static bool MeasureTree<T>(ref T p, APTR pass, APTR state, APTR obj,
        uint depth) where T : struct, IMuiLayoutPlatform
    {
        if (depth >= 64) return false;
        var complete = false;
        for (var index = 0; index < 65535; index++)
        {
            var child = MuiFamilyCore.GetChild(ref p, state, obj, index, APTR.Null);
            if (child.IsNull) { complete = true; break; }
            if (!MeasureTree(ref p, pass, state, child, depth + 1)) return false;
        }
        if (!complete) return false;
        var previous = p.ReadUInt32(pass, Current);
        var previousDepth = p.ReadUInt32(pass, Depth);
        p.WriteUInt32(pass, Current, obj.Raw); p.WriteUInt32(pass, Depth, previousDepth + 1);
        var success = Calculate(ref p, state, obj, out var values);
        p.WriteUInt32(pass, Depth, previousDepth); p.WriteUInt32(pass, Current, previous);
        if (!success) return false;
        if (p.ReadUInt32(pass, Reliable) == 0) return true;
        var record = MuiHeadlessObjectCore.FindObject(ref p, state, obj);
        if (record.IsNull || !MuiHeadlessObjectCodec.TryRead(ref p, record, out var live)) return false;
        var node = p.Allocate(NodeSize, MuiHeadlessLayout.AllocationFlags);
        if (node.IsNull) return false;
        p.WriteUInt32(node, Next, p.ReadUInt32(pass, Head)); p.WriteUInt32(node, Object, obj.Raw);
        p.WriteUInt32(node, Generation, live.Generation);
        if (!MuiMinMaxRecordCodec.WriteRecord(ref p, APTR.FromPointer(node.Raw + Bounds), values))
        { p.Free(node, NodeSize); return false; }
        p.WriteUInt32(pass, Head, node.Raw);
        return true;
    }

    private static bool Calculate<T>(ref T p, APTR state, APTR obj, out MuiMinMaxValues values)
        where T : struct, IMuiLayoutPlatform
    {
        values = default;
        if (MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, MuiCommonControlCore.ShowMe, out var shown) && shown == 0) return true;
        var collection = MuiListCore.Classify(ref p, state, obj);
        if (collection == MuiCollectionClass.List || collection == MuiCollectionClass.Listview)
        {
            var storage = p.Allocate(MuiMinMaxValues.Size, MuiHeadlessLayout.AllocationFlags);
            if (storage.IsNull) return false;
            var measured = collection == MuiCollectionClass.List ?
                MuiListCore.AskMinMax(ref p, state, obj, storage) : MuiListviewCore.AskMinMax(ref p, state, obj, storage);
            var read = measured && MuiMinMaxRecordCodec.TryReadRecord(ref p, storage, out values);
            p.Free(storage, MuiMinMaxValues.Size);
            return read;
        }
        if (!MuiCommonControlCore.IsAreaControlClass(MuiCommonControlCore.Classify(ref p, state, obj)) &&
            MuiGroupChangeCore.IsGroupObject(ref p, state, obj))
            values = MuiGroupLayoutCore.ComputeMinMax(ref p, state, obj);
        else values = MuiAreaLayoutCore.ComputeMinMax(ref p, state, obj);
        return true;
    }
}
