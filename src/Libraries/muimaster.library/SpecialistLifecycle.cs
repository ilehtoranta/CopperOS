/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Class-owned teardown for the MG09 pen/color specialist family. The frozen
// generic object closure (MuiHeadlessObjectCore) is deliberately not involved:
// the specialists carry their own copied blocks and pen acquisitions, so their
// OM_DISPOSE-equivalent lives here. Dispose first balances the Setup pen
// lifecycle (releasing exactly once through MuiDrawingServiceCore, never
// bypassing its tracking), then frees every class-owned copied block and
// invalidates the instance so a repeated disposal is a safe no-op. Reference
// objects and caller-owned Palette entry/name arrays are never freed.
public static class MuiColorSpecialistLifecycle
{
	public static bool Dispose<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out _)) return false;

		// Balance the pen lifecycle. Cleanup releases the held pen exactly once
		// and also frees Colorfield's transient pen spec; a never-set-up (or
		// already-cleaned) instance releases nothing.
		MuiColorSpecialistCore.Cleanup(ref platform, instance);

		// Free the persistent class-owned copied blocks. Colorfield's spec is
		// already gone (transient, freed by Cleanup); Pendisplay's persistent
		// spec and every RGB copy are released here. Reference/Entries/Names are
		// caller-owned and left untouched.
		if (!MuiColorSpecialistStateCodec.TryRead(ref platform, instance,
			out var state)) return false;
		var spec = state.SpecBlock;
		if (spec.IsNotNull)
			MuiColorSpecialistCore.MuiColorFree(ref platform, spec,
				MuiColorSpecialistLayout.SpecSize);
		var rgb = state.RgbBlock;
		if (rgb.IsNotNull)
			MuiColorSpecialistCore.MuiColorFree(ref platform, rgb,
				MuiColorSpecialistLayout.RgbSize);

		// Invalidate: a repeated disposal must find nothing to release or free.
		platform.Clear(instance, MuiColorSpecialistState.Size);
		return true;
	}
}
