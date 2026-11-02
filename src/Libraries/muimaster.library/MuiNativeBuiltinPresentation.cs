/* SPDX-License-Identifier: MIT */
using Amiga;

namespace CopperOS.MuiMaster;

internal static class MuiNativeBuiltinPresentation
{
    private const uint RenderInfoKey = 0x7FF10080;
    internal static bool Present(ref MuiNativeBuiltinPlatform p, APTR obj, APTR nativeWindow)
    {
        MuiHeadlessObjectCore.GetRawAttribute(ref p, p.Registry, obj, MuiWindowPublicCore.RootObject, out var root);
        if (root == 0) return true;
        var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref p, nativeWindow);
        if (!MuiNativeWindowContentLayoutCore.TryCreate(window, out var rectangle)) return false;
        var scratch = p.Allocate(MuiDrawingRenderInfoRecord.Size, MuiHeadlessLayout.AllocationFlags);
        if (scratch.IsNull) return false;
        var render = new MuiDrawingRenderInfoRecord
        {
            WindowObject = obj, Window = nativeWindow, Screen = window.Screen, RastPort = window.RastPort,
        };
        var stored = MuiDrawingRenderInfoCodec.Write(ref p, scratch, render) &&
            MuiStoreCore.DataspaceAdd(ref p, p.Registry, obj, RenderInfoKey, scratch, (int)MuiDrawingRenderInfoRecord.Size);
        p.Free(scratch, MuiDrawingRenderInfoRecord.Size);
        if (!stored) return false;
        var info = MuiStoreCore.DataspaceFind(ref p, p.Registry, obj, RenderInfoKey);
        if (!PrepareTree(ref p, p.Registry, APTR.FromPointer(root), info, 0) ||
            !p.BeginMinMax(APTR.FromPointer(root))) return false;
        if (!p.FitBuiltinWindow(obj, nativeWindow, APTR.FromPointer(root)))
        { p.EndMinMax(); return false; }
        window = IntuitionScreenWindowGuestCodec.ReadWindow(ref p, nativeWindow);
        if (!MuiNativeWindowContentLayoutCore.TryCreate(window, out rectangle))
        { p.EndMinMax(); return false; }
        render.Screen = window.Screen; render.RastPort = window.RastPort;
        if (!MuiDrawingRenderInfoCodec.Write(ref p, info, render))
        { p.EndMinMax(); return false; }
        var laidOut = LayoutTree(ref p, p.Registry, APTR.FromPointer(root), rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height, 0);
        p.EndMinMax();
        return laidOut && DrawTree(ref p, p.Registry, APTR.FromPointer(root), 0);
    }
    internal static bool PrepareTree<T>(ref T p, APTR state, APTR obj, APTR info, uint depth)
        where T : struct, IMuiLayoutPlatform
    {
        if (depth >= 64 || !MuiAreaLayoutCore.Setup(ref p, state, obj, info) ||
            !MuiAreaLayoutCore.Show(ref p, state, obj)) return false;
        for (var i = 0; i < 65535; i++)
        {
            var child = MuiFamilyCore.GetChild(ref p, state, obj, i, APTR.Null);
            if (child.IsNull) return true;
            if (!PrepareTree(ref p, state, child, info, depth + 1)) return false;
        }
        return false;
    }
    internal static bool LayoutTree<T>(ref T p, APTR state, APTR obj,
        int left, int top, int width, int height, uint depth) where T : struct, IMuiLayoutPlatform
    {
        if (depth >= 64) return false;
        var collection = MuiListCore.Classify(ref p, state, obj);
        var laidOut = collection == MuiCollectionClass.List ? MuiListCore.Layout(ref p, state, obj, left, top, width, height) :
            collection == MuiCollectionClass.Listview ? MuiListviewCore.Layout(ref p, state, obj, left, top, width, height) :
            MuiGroupChangeCore.IsGroupObject(ref p, state, obj) ? MuiGroupLayoutCore.Layout(ref p, state, obj, left, top, width, height) :
            MuiAreaLayoutCore.Layout(ref p, state, obj, left, top, width, height);
        if (!laidOut) return false;
        for (var i = 0; i < 65535; i++)
        {
            var child = MuiFamilyCore.GetChild(ref p, state, obj, i, APTR.Null);
            if (child.IsNull) return true;
            if (!MuiAreaLayoutCore.TryReadGeometryState(ref p, state, child, out var geometry) ||
                !LayoutTree(ref p, state, child, geometry.Left, geometry.Top, geometry.Width, geometry.Height, depth + 1)) return false;
        }
        return false;
    }
    internal static bool DrawTree<T>(ref T p, APTR state, APTR obj, uint depth) where T : struct, IMuiLayoutPlatform
    {
        if (depth >= 64) return false;
        if (MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, MuiCommonControlCore.ShowMe, out var shown) && shown == 0) return true;
        if (MuiListCore.Classify(ref p, state, obj) == MuiCollectionClass.List &&
            MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, 0x8042D8C7, out var quiet) && quiet != 0) return true;
        // Page layout gives inactive children an empty box. Their descendants
        // may retain minimum-sized geometry, but must not paint over the page.
        if (!MuiAreaLayoutCore.TryReadGeometryState(ref p, state, obj, out var geometry) ||
            geometry.Width <= 0 || geometry.Height <= 0) return true;
        if (!DrawOne(ref p, state, obj, 0)) return false;
        for (var i = 0; i < 65535; i++)
        {
            var child = MuiFamilyCore.GetChild(ref p, state, obj, i, APTR.Null);
            if (child.IsNull) return true;
            if (!DrawTree(ref p, state, child, depth + 1)) return false;
        }
        return false;
    }
    internal static bool CleanupTree<T>(ref T p, APTR state, APTR obj, uint depth) where T : struct, IMuiLayoutPlatform
    {
        if (depth >= 64) return false;
        for (var i = 0; i < 65535; i++)
        {
            var child = MuiFamilyCore.GetChild(ref p, state, obj, i, APTR.Null);
            if (child.IsNull)
            {
                // A failed parent setup leaves later children unprepared.
                // Do not create font/color/resize stores while closing them.
                MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, 0x7FFF0002, out var setup);
                return setup == 0 || (MuiAreaLayoutCore.Hide(ref p, state, obj) && MuiAreaLayoutCore.Cleanup(ref p, state, obj));
            }
            if (!CleanupTree(ref p, state, child, depth + 1)) return false;
        }
        return false;
    }
    private static bool DrawOne<T>(ref T p, APTR state, APTR obj, uint flags) where T : struct, IMuiLayoutPlatform
    {
        var collection = MuiListCore.Classify(ref p, state, obj);
        if (collection == MuiCollectionClass.List) return MuiListCore.Draw(ref p, state, obj, flags);
        // The registered native Listview owns its ordinary List child. Paint
        // the frame here; the tree walk paints that child exactly once.
        if (collection == MuiCollectionClass.Listview) return MuiAreaLayoutCore.Draw(ref p, state, obj, flags);
        return MuiCommonControlCore.IsAreaControlClass(MuiCommonControlCore.Classify(ref p, state, obj)) ?
            MuiCommonControlCore.DrawControl(ref p, state, obj, flags) : MuiAreaLayoutCore.Draw(ref p, state, obj, flags);
    }
    internal static bool DrawObject(ref MuiNativeBuiltinPlatform p, APTR obj, uint flags)
    {
        if (!IsVisibleInTree(ref p, p.Registry, obj)) return true;
        if (MuiApplicationMessageCore.IsWindowObject(ref p, p.Registry, obj))
        {
            MuiHeadlessObjectCore.GetRawAttribute(ref p, p.Registry, obj, MuiWindowPublicCore.RootObject, out var root);
            return root == 0 || DrawTree(ref p, p.Registry, APTR.FromPointer(root), 0);
        }
        return DrawTree(ref p, p.Registry, obj, 0);
    }
    internal static bool IsVisibleInTree<T>(ref T p, APTR state, APTR obj) where T : struct, IMuiHeadlessPlatform
    {
        var current = obj;
        for (uint depth = 0; current.IsNotNull && depth < 64; depth++)
        {
            if (MuiHeadlessObjectCore.GetRawAttribute(ref p, state, current, MuiCommonControlCore.ShowMe, out var shown) && shown == 0) return false;
            // Window owns the Area tree, but has no Area layout rectangle.
            // Its default zero projection must not hide all of its content.
            if (MuiApplicationMessageCore.IsWindowObject(ref p, state, current)) return true;
            if (MuiAreaLayoutCore.TryReadGeometryState(ref p, state, current, out var geometry) &&
                (geometry.Width <= 0 || geometry.Height <= 0)) return false;
            current = MuiHeadlessObjectCore.ParentObject(ref p, state, current);
        }
        return current.IsNull;
    }
    internal static bool RedrawAfterSet<T>(ref T p, APTR state, APTR obj) where T : struct, IMuiLayoutPlatform
    {
        // Constructor and cleanup setters have no drawable surface. A callback
        // may also have disposed the target before the public setter returns.
        if (!MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, 0x7FFF0002, out var setup) || setup == 0 ||
            !MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, 0x7FFF0003, out var shown) || shown == 0 ||
            !IsVisibleInTree(ref p, state, obj)) return true;
        if (MuiListCore.Classify(ref p, state, obj) == MuiCollectionClass.List &&
            MuiHeadlessObjectCore.GetRawAttribute(ref p, state, obj, 0x8042D8C7, out var quiet) && quiet != 0) return true;
        return DrawTree(ref p, state, obj, 0);
    }
    internal static bool RedrawChangedList<T>(ref T p, APTR state, APTR obj, uint previousRequests)
        where T : struct, IMuiLayoutPlatform =>
        MuiListCore.RedrawRequests(ref p, state, obj) == previousRequests ||
        RedrawAfterSet(ref p, state, obj);
    internal static void PublishStringAcknowledge(ref MuiNativeBuiltinPlatform p, APTR obj)
    {
        var native = p.Native;
        if (MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref p, p.Context, out var objects) &&
            MuiHeadlessObjectCore.GetAttribute(ref p, p.Registry, obj, MuiCommonControlCore.StringAcknowledge, out var value))
            MuiNativePublicObjectCore.SetAttribute(ref native, objects, native.OwnerRoot,
                obj, MuiCommonControlCore.StringAcknowledge, value, true);
    }
    internal static void PublishInputChanges(ref MuiNativeBuiltinPlatform p, APTR obj,
        bool notify = true, bool stringContentsChanged = false)
    {
        var native = p.Native;
        if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref p, p.Context, out var objects)) return;
        // Control input lives in the typed headless records. Public Notify
        // registrations live in the native sidecar used by Application input.
        // Publish changed public interaction values, never internal store keys.
        for (var i = 0; i < 6; i++)
        {
            var attribute = i == 0 ? MuiCommonControlCore.Pressed :
                i == 1 ? MuiCommonControlCore.Selected :
                i == 2 ? MuiCommonControlCore.CycleActive :
                i == 3 ? MuiCommonControlCore.StringContents :
                i == 4 ? MuiCommonControlCore.NumericValue : MuiCommonControlCore.ImageState;
            if (!MuiHeadlessObjectCore.GetAttribute(ref p, p.Registry, obj, attribute, out var value)) continue;
            var hadPrevious = MuiNativePublicObjectCore.GetAttribute(ref native, objects, native.OwnerRoot, obj, attribute, out var previous);
            if (!(attribute == MuiCommonControlCore.StringContents && stringContentsChanged) &&
                ((!hadPrevious && value == 0) || (hadPrevious && previous == value))) continue;
            if (!MuiNativePublicObjectCore.SetAttribute(ref native, objects, native.OwnerRoot, obj, attribute, value, notify)) return;
        }
    }
}
