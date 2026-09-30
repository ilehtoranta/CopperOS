/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS keeps MUIA_Application_Window creation-time-only. RequestA still
// needs a temporary window to participate in the owning application's native
// NewInput pump, so this private relationship is deliberately separate from
// the public initializer tag. Parent identity is kept in the named public
// object bindings consumed by the native event and signal walkers.
internal static class MuiNativeRequesterApplicationWindowCore
{
	internal static bool CanAttach<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR window)
		where TMemory : struct, IMuiGuestMemory
	{
		if (application.IsNull || window.IsNull || application == window ||
			!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, application, out var applicationBinding) ||
			!MuiNativeApplicationSleep.TryIsApplicationClass(ref memory,
				applicationBinding.Class, out var isApplication) || !isApplication ||
			!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, window, out var windowBinding) ||
			!MuiNativeApplicationSleep.TryIsWindowClass(ref memory,
				windowBinding.Class, out var isWindow) || !isWindow ||
			windowBinding.Parent.IsNotNull ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, windowBinding.Sidecar,
				out var windowSidecar)) return false;
		return windowSidecar.Parent.IsNull &&
			(windowSidecar.Flags &
				MuiNativeMuiObjectRecord.ObjectParentKindMask) == 0;
	}

	internal static bool CanDetach<TMemory>(ref TMemory memory,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR window)
		where TMemory : struct, IMuiGuestMemory
	{
		if (application.IsNull || window.IsNull || application == window ||
			!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, application, out var applicationBinding) ||
			!MuiNativeApplicationSleep.TryIsApplicationClass(ref memory,
				applicationBinding.Class, out var isApplication) || !isApplication ||
			!MuiNativePublicObjectCore.TryFindLive(ref memory, publicObjects,
				ownerRoot, window, out var windowBinding) ||
			!MuiNativeApplicationSleep.TryIsWindowClass(ref memory,
				windowBinding.Class, out var isWindow) || !isWindow ||
			windowBinding.Parent != application ||
			!MuiNativeMuiObjectCodec.TryRead(ref memory, windowBinding.Sidecar,
				out var windowSidecar) || windowSidecar.Parent != application ||
			(windowSidecar.Flags &
				MuiNativeMuiObjectRecord.ObjectParentKindMask) != 0 ||
			!MuiNativeObjectStateCore.TryGetAttribute(ref memory,
				windowBinding.Sidecar, MuiWindowPublicCore.Open,
				out var isOpen)) return false;
		return isOpen == 0;
	}

	internal static bool Attach(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR window)
	{
		var memory = default(MuiNativeClassMemory);
		var admitted = CanAttach(ref memory, publicObjects, ownerRoot,
			application, window);
		Exec.Forbid();
		var attached = admitted && CanAttach(ref memory, publicObjects,
			ownerRoot, application, window) &&
			MuiNativePublicObjectCore.SetParent(ref platform, ownerRoot,
				publicObjects, application, window, 0);
		Exec.Permit();
		return attached;
	}

	internal static bool Detach(ref MuiNativeClassPlatform platform,
		APTR publicObjects, APTR ownerRoot, APTR application, APTR window)
	{
		var memory = default(MuiNativeClassMemory);
		var admitted = CanDetach(ref memory, publicObjects, ownerRoot,
			application, window);
		Exec.Forbid();
		var detached = admitted && CanDetach(ref memory, publicObjects,
			ownerRoot, application, window) &&
			MuiNativePublicObjectCore.ClearParent(ref platform, ownerRoot,
				publicObjects, application, window);
		Exec.Permit();
		return detached;
	}
}
