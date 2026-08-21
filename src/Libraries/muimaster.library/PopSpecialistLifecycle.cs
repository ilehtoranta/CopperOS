/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Class-owned teardown for the MG09 Pop* specialist family. The frozen generic
// object closure (MuiHeadlessObjectCore) is deliberately not involved: the Pop
// classes adopt their String/Button children and retain their popup Object, so
// their OM_DISPOSE-equivalent lives here. Dispose first cancels any live popup
// (releasing an active ASL requester and freeing a volatile window), then
// recursively disposes the adopted children and retained popup object and frees
// every class-owned copied block (materialized array, ASL service state, hook
// scratch). Finally the instance is invalidated so a repeated disposal is a
// safe no-op. Caller-owned references (the source array, ASL tag list) are
// never freed.
public static class MuiPopSpecialistLifecycle
{
	public static bool Dispose<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiPopSpecialistCore.Valid(ref platform, instance)) return false;

		// Recursive, ownership-correct release of children/popup/copied state.
		MuiPopSpecialistCore.DisposeOwned(ref platform, instance);

		// Invalidate: a repeated disposal must find nothing to release or free.
		platform.Clear(instance, MuiPopSpecialistLayout.InstanceSize);
		return true;
	}
}
