/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

// Window-relative coordinates, stable headless object generation, and ordinary
// Family ownership are sufficient for a native button capture. No host input
// state or private BOOPSI layout is involved.
internal static class MuiBuiltinPointerCore
{
    private const uint Capture = 0x7FF10081;
    private const uint CaptureGeneration = 0x7FF10082;
    private const uint CaptureSelected = 0x7FF10083;
    internal static bool Handle<T>(ref T p, APTR state, APTR window, int x, int y, bool down,
        out APTR changed, out bool notify) where T : struct, IMuiLayoutPlatform
    {
        changed = APTR.Null; notify = false;
        if (down)
        {
            MuiHeadlessObjectCore.GetRawAttribute(ref p, state, window, Capture, out var existing);
            if (existing != 0) return true; // Ignore duplicate downs until this capture ends.
            MuiHeadlessObjectCore.GetRawAttribute(ref p, state, window, MuiWindowPublicCore.RootObject, out var root);
            var target = Hit(ref p, state, APTR.FromPointer(root), x, y, 0);
            if (target.IsNull) return false;
            if (MuiCommonControlCore.Classify(ref p, state, target) == MuiControlClass.String)
            {
                if (!Set(ref p, state, window, MuiWindowPublicCore.ActiveObject, target.Raw)) return false;
                changed = target;
                return true;
            }
            var record = MuiHeadlessObjectCore.FindObject(ref p, state, target);
            if (!MuiHeadlessObjectCodec.TryRead(ref p, record, out var value)) return false;
            MuiHeadlessObjectCore.GetAttribute(ref p, state, target, MuiCommonControlCore.Selected, out var selected);
            if (!Set(ref p, state, window, CaptureGeneration, value.Generation) ||
                !Set(ref p, state, window, CaptureSelected, selected) ||
                !Set(ref p, state, window, Capture, target.Raw)) return false;
            MuiHeadlessObjectCore.GetAttribute(ref p, state, target, MuiCommonControlCore.InputMode, out var mode);
            if (mode == 3) return true; // Toggle commits only on an accepted release.
            MuiCommonControlCore.SetGadgetState(ref p, state, target, 1, 1);
            changed = target; notify = true;
            return true;
        }
        return Release(ref p, state, window, x, y, false, out changed, out notify);
    }
    internal static bool Cancel<T>(ref T p, APTR state, APTR window, out APTR changed)
        where T : struct, IMuiLayoutPlatform => Release(ref p, state, window, 0, 0, true, out changed, out _);
    private static bool Release<T>(ref T p, APTR state, APTR window, int x, int y, bool cancel,
        out APTR changed, out bool notify) where T : struct, IMuiLayoutPlatform
    {
        changed = APTR.Null; notify = false;
        MuiHeadlessObjectCore.GetRawAttribute(ref p, state, window, Capture, out var captured);
        if (captured == 0) return false;
        MuiHeadlessObjectCore.GetRawAttribute(ref p, state, window, CaptureGeneration, out var generation);
        MuiHeadlessObjectCore.GetRawAttribute(ref p, state, window, CaptureSelected, out var selected);
        if (!Set(ref p, state, window, Capture, 0)) return false; // Clear before any callback.
        var target = APTR.FromPointer(captured);
        var record = MuiHeadlessObjectCore.FindObject(ref p, state, target);
        if (record.IsNull || !MuiHeadlessObjectCodec.TryRead(ref p, record, out var value) ||
            value.Generation != generation) return true;
        if (MuiApplicationWindowCore.FindContainingWindow(ref p, state, target) != window)
        {
            MuiCommonControlCore.SetGadgetState(ref p, state, target, selected, 0, false);
            changed = target;
            return true;
        }
        MuiHeadlessObjectCore.GetRawAttribute(ref p, state, window, MuiWindowPublicCore.RootObject, out var root);
        var accepted = !cancel && Hit(ref p, state, APTR.FromPointer(root), x, y, 0) == target;
        MuiHeadlessObjectCore.GetAttribute(ref p, state, target, MuiCommonControlCore.InputMode, out var mode);
        if (mode == 3)
        {
            if (!accepted) return true;
            MuiCommonControlCore.SetGadgetState(ref p, state, target, selected == 0 ? 1u : 0u, 0);
        }
        else MuiCommonControlCore.SetGadgetState(ref p, state, target, selected, 0, accepted);
        changed = target; notify = accepted;
        return true;
    }
    private static APTR Hit<T>(ref T p, APTR state, APTR obj, int x, int y, uint depth)
        where T : struct, IMuiLayoutPlatform
    {
        if (obj.IsNull || depth >= 64 || !MuiAreaLayoutCore.TryReadGeometryState(ref p, state, obj, out var rect) ||
            rect.Width <= 0 || rect.Height <= 0 || x < rect.Left || y < rect.Top ||
            x - rect.Left >= rect.Width || y - rect.Top >= rect.Height) return APTR.Null;
        if (MuiHeadlessObjectCore.GetAttribute(ref p, state, obj, MuiCommonControlCore.Disabled, out var disabled) && disabled != 0)
            return APTR.Null;
        if (MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, MuiCommonControlCore.ShowMe, out var shown) && shown == 0)
            return APTR.Null;
        var hit = APTR.Null;
        for (var i = 0; i < 65535; i++)
        {
            var child = MuiFamilyCore.GetChild(ref p, state, obj, i, APTR.Null);
            if (child.IsNull) break;
            var candidate = Hit(ref p, state, child, x, y, depth + 1);
            if (candidate.IsNotNull) hit = candidate;
        }
        if (hit.IsNotNull) return hit;
        var cls = MuiCommonControlCore.Classify(ref p, state, obj);
        if (cls == MuiControlClass.String) return obj;
        if (cls != MuiControlClass.Text && cls != MuiControlClass.Image) return APTR.Null;
        MuiHeadlessObjectCore.GetAttribute(ref p, state, obj, MuiCommonControlCore.InputMode, out var mode);
        return mode >= 1 && mode <= 3 ? obj : APTR.Null;
    }
    private static bool Set<T>(ref T p, APTR state, APTR obj, uint attribute, uint value)
        where T : struct, IMuiLayoutPlatform => MuiHeadlessObjectCore.SetAttribute(ref p, state, obj, attribute, value, false);
}
