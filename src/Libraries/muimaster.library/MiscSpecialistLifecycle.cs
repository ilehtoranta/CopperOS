/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Class-owned teardown for the final MG09 misc specialist family. The frozen
// generic object closure (MuiHeadlessObjectCore) is deliberately not involved:
// these classes carry their own copied strings, bounded record blocks, adopted
// Filepanel row children and ASL/hook scratch, so their OM_DISPOSE-equivalent
// lives here. Dispose recursively releases everything the class owns and then
// invalidates the instance so a repeated disposal is a safe no-op. Caller-owned
// references (Aboutmui's Application, the Panel window, Mccprefs gadget/label
// references, the FilterFunc hook and any caller ASL tag list) are never freed.
public static class MuiMiscSpecialistLifecycle
{
	public static bool Dispose<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiMiscSpecialistCore.Valid(ref platform, instance)) return false;

		// Recursive, ownership-correct release of children/strings/records.
		MuiMiscSpecialistCore.DisposeOwned(ref platform, instance);

		// Invalidate: a repeated disposal must find nothing to release or free.
		platform.Clear(instance, MuiMiscSpecialistLayout.InstanceSize);
		return true;
	}

	// Factory-created Misc objects keep their specialist state in a guest
	// sidecar, just like Process/Menu. Dispose the class-owned state first,
	// detach and free the sidecar, then let the frozen object core release the
	// headless record and its children.
	public static bool Dispose<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		var instance = MuiMiscSpecialistCore.ObjectInstance(ref platform, state,
			obj);
		if (instance.IsNull) return false;
		if (!Dispose(ref platform, instance)) return false;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			MuiMiscSpecialistLayout.SidecarAttribute, 0, false);
		platform.Clear(instance, MuiMiscSpecialistLayout.InstanceSize);
		platform.Free(instance, MuiMiscSpecialistLayout.InstanceSize);
		return MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);
	}
}
