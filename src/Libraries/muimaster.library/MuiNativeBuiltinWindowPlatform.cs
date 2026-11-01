/* SPDX-License-Identifier: MIT */
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.MuiMaster;

internal partial struct MuiNativeBuiltinPlatform
{
    private uint WindowAttribute(APTR obj, uint attribute)
    {
        MuiHeadlessObjectCore.GetRawAttribute(ref this, Registry, obj, attribute, out var value);
        return value;
    }
    public APTR OpenBuiltinWindow(APTR obj)
    {
        var native = Native;
        if (native.IntuitionBase.IsNull) return APTR.Null;
        var description = Allocate(NewWindow.Size, MuiHeadlessLayout.AllocationFlags);
        if (description.IsNull) return APTR.Null;
        var width = unchecked((int)WindowAttribute(obj, MuiWindowPublicCore.Width));
        var height = unchecked((int)WindowAttribute(obj, MuiWindowPublicCore.Height));
        var screen = APTR.FromPointer(WindowAttribute(obj, MuiWindowPublicCore.Screen));
        var spec = new NewWindow
        {
            LeftEdge = 12, TopEdge = 16,
            Width = (short)(width > 0 && width <= 640 ? width : 320),
            Height = (short)(height > 0 && height <= 256 ? height : 180),
            DetailPen = 0, BlockPen = 1,
            Flags = WindowFlags.CloseGadget | WindowFlags.DragBar | WindowFlags.DepthGadget |
                WindowFlags.Activate | WindowFlags.SimpleRefresh,
            IDCMPFlags = IDCMPFlags.CloseWindow | IDCMPFlags.RefreshWindow |
                IDCMPFlags.MouseButtons | IDCMPFlags.RawKey,
            Title = STRPTR.FromPointer(WindowAttribute(obj, MuiWindowPublicCore.Title)),
            Screen = screen, Type = screen.IsNull ? ScreenType.Workbench : ScreenType.Custom,
            MinWidth = 80, MinHeight = 40, MaxWidth = 640, MaxHeight = 256,
        };
        IntuitionScreenWindowGuestCodec.WriteNewWindow(ref this, description, spec);
        var window = MuiNativeIntuitionCalls.OpenWindow(native.IntuitionBase, description);
        Free(description, NewWindow.Size);
        return window;
    }
    public void CloseBuiltinWindow(APTR window)
    {
        var native = Native;
        if (window.IsNotNull && native.IntuitionBase.IsNotNull)
            MuiNativeIntuitionCalls.CloseWindow(native.IntuitionBase, window);
    }
    internal bool FitBuiltinWindow(APTR obj, APTR nativeWindow, APTR root)
    {
        var widthRequest = unchecked((int)WindowAttribute(obj, MuiWindowPublicCore.Width));
        var heightRequest = unchecked((int)WindowAttribute(obj, MuiWindowPublicCore.Height));
        if (widthRequest > 0 && heightRequest > 0) return true;
        var window = IntuitionScreenWindowGuestCodec.ReadWindow(ref this, nativeWindow);
        if (window.Screen.IsNull) return false;
        if (!MuiNativeScreenStructCodec.TryRead(ref this, window.Screen, out var screen)) return false;
        var preferred = MuiAreaLayoutCore.ComputeMinMax(ref this, Registry, root);
        var width = MuiNativeBuiltinWindowSizing.PreferredExtent(widthRequest, window.Width,
            preferred.MinWidth, preferred.DefWidth, window.BorderLeft + window.BorderRight,
            screen.Width - window.LeftEdge);
        var height = MuiNativeBuiltinWindowSizing.PreferredExtent(heightRequest, window.Height,
            preferred.MinHeight, preferred.DefHeight, window.BorderTop + window.BorderBottom,
            screen.Height - window.TopEdge);
        if (width <= 0 || height <= 0) return false;
        if (width == window.Width && height == window.Height) return true;
        var native = Native;
        var graphics = GraphicsBase;
        if (native.IntuitionBase.IsNull || graphics.IsNull) return false;
        MuiNativeBuiltinWindowCalls.SizeWindow(native.IntuitionBase, nativeWindow,
            width - window.Width, height - window.Height);
        // SizeWindow is asynchronous. Do not lay out or paint against the
        // requested box before Intuition has actually committed it. No Layer
        // is held here, and the small read scope never spans WaitTOF.
        // https://wiki.amigaos.net/wiki/Window_Structures_and_Functions
        for (var attempt = 0; attempt < 50; attempt++)
        {
            Exec.Forbid();
            var actual = IntuitionScreenWindowGuestCodec.ReadWindow(ref this, nativeWindow);
            var completed = actual.Width == width && actual.Height == height;
            Exec.Permit();
            if (completed) return true;
            MuiNativeBuiltinGraphicsCalls.WaitTOF(graphics);
        }
        return false;
    }
    public bool PublishBuiltinWindow(APTR obj, APTR window)
    {
        var native = Native;
        if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref this, Context, out var objects)) return false;
        if (window.IsNull)
        {
            MuiNativeBuiltinInput.CancelWindow(ref this, obj);
            var root = WindowAttribute(obj, MuiWindowPublicCore.RootObject);
            if (root != 0 && !MuiNativeBuiltinPresentation.CleanupTree(ref this, Registry, APTR.FromPointer(root), 0)) return false;
            return MuiNativePublicObjectCore.SetNativeWindow(ref native, objects, native.OwnerRoot, obj, APTR.Null);
        }
        if (!MuiNativePublicObjectCore.SetNativeWindow(ref native, objects, native.OwnerRoot, obj, window)) return false;
        if (MuiNativeBuiltinPresentation.Present(ref this, obj, window)) return true;
        var failedRoot = WindowAttribute(obj, MuiWindowPublicCore.RootObject);
        if (failedRoot != 0) MuiNativeBuiltinPresentation.CleanupTree(ref this, Registry, APTR.FromPointer(failedRoot), 0);
        MuiNativePublicObjectCore.SetNativeWindow(ref native, objects, native.OwnerRoot, obj, APTR.Null);
        return false;
    }
    internal void ReleaseBuiltinWindow(APTR obj)
    {
        var native = Native;
        if (!MuiNativeClassOwnerCodec.TryGetPublicObjectStateAddress(ref this, Context, out var objects) ||
            !MuiNativePublicObjectCore.TryFindBinding(ref native, objects, native.OwnerRoot, obj, out var binding) ||
            !MuiNativeMuiObjectCodec.TryRead(ref this, binding.Sidecar, out var sidecar) ||
            sidecar.NativeWindow.IsNull) return;
        var window = sidecar.NativeWindow;
        // Clear before the provider call so recursive disposal cannot close twice.
        sidecar.NativeWindow = APTR.Null;
        if (MuiNativeMuiObjectCodec.Write(ref this, binding.Sidecar, sidecar)) CloseBuiltinWindow(window);
    }
}

[AmigaLibrary("intuition.library", AmigaLibraryBasePolicy.CallerProvided)]
internal static class MuiNativeBuiltinWindowCalls
{
    [AmigaLvo(IntuitionLvo.SizeWindow)]
    internal static extern void SizeWindow([M68kRegister(M68kRegister.A6)] APTR library,
        [M68kRegister(M68kRegister.A0)] APTR window,
        [M68kRegister(M68kRegister.D0)] int deltaWidth, [M68kRegister(M68kRegister.D1)] int deltaHeight);
}
