/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// The root MUI object occupies the drawable client rectangle of its Intuition
// window. Keep that rectangle as named geometry instead of deriving it from
// public-object or Window byte offsets.
internal struct MuiNativeWindowContentRectangle
{
	internal int Left;
	internal int Top;
	internal int Width;
	internal int Height;
}

internal static class MuiNativeWindowContentLayoutCore
{
	internal static bool TryCreate(Window window,
		out MuiNativeWindowContentRectangle rectangle)
	{
		rectangle = default;
		var left = (int)window.BorderLeft;
		var top = (int)window.BorderTop;
		var right = (int)window.BorderRight;
		var bottom = (int)window.BorderBottom;
		var width = (int)window.Width;
		var height = (int)window.Height;
		if (left < 0 || top < 0 || right < 0 || bottom < 0 ||
			width <= left + right || height <= top + bottom) return false;

		rectangle.Left = left;
		rectangle.Top = top;
		rectangle.Width = width - left - right;
		rectangle.Height = height - top - bottom;
		return true;
	}

	internal static bool TryRelayoutRootAfterResize(
		ref MuiNativeClassPlatform platform, APTR publicObjects,
		APTR ownerRoot, APTR windowObject, APTR windowSidecar)
	{
		var memory = default(MuiNativeClassMemory);
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, windowObject, out var windowBinding) ||
			windowBinding.Sidecar != windowSidecar ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, windowSidecar,
				out var windowState) || windowState.Object != windowObject ||
			windowState.NativeWindow.IsNull || !memory.IsMapped(
				windowState.NativeWindow, Window.Size) ||
			!MuiNativeApplicationSleep.TryReadAttribute(ref memory,
				windowSidecar, MuiWindowPublicCore.Open, out var isOpen,
				out var hasOpen) || !hasOpen || isOpen == 0)
			return false;

		if (!MuiNativeApplicationSleep.TryReadAttribute(ref memory,
			windowSidecar, MuiWindowPublicCore.RootObject, out var rootRaw,
			out var hasRoot)) return false;
		if (!hasRoot || rootRaw == 0) return true;

		var rootObject = APTR.FromPointer(rootRaw);
		if (!MuiNativePublicObjectCore.TryFindLive(ref platform, publicObjects,
			ownerRoot, rootObject, out _)) return false;
		var nativeWindow = IntuitionScreenWindowGuestCodec.ReadWindow(ref memory,
			windowState.NativeWindow);
		if (!TryCreate(nativeWindow, out var rectangle) ||
			!MuiMasterPrivateRootCodec.TryRead(ref memory, ownerRoot,
				out var privateRoot) || privateRoot.LoaderState == 0)
			return false;
		var ownerAddress = APTR.FromPointer(privateRoot.LoaderState);
		if (!MuiNativeClassOwnerCodec.TryRead(ref memory, ownerAddress,
			out var owner) || owner.LibraryBase.IsNull) return false;

		return MuiNativeLayoutServiceCore.Layout(ref platform, publicObjects,
			ownerRoot, owner.LibraryBase, rootObject, rectangle.Left,
			rectangle.Top, rectangle.Width, rectangle.Height, 0);
	}
}
