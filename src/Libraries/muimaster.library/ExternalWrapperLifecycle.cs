/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Class-owned teardown for the MG09 external-resource wrapper family
// (Boopsi.mui / Dtpic.mui). The frozen generic object closure
// (MuiHeadlessObjectCore) is deliberately not involved: these classes own an
// external resource whose exactly-once release lives here. Dispose recursively
// releases the wrapped boopsi object and the class library the wrapper opened
// (closed exactly once), or the datatypes picture (released exactly once),
// frees the owned name copy, remember buffer and message scratch, then
// invalidates the instance so a repeated disposal is a safe no-op. Caller-owned
// references (a private class, the creation tag list, the caller name buffer)
// are never freed.
public static class MuiExternalWrapperLifecycle
{
	public static bool Dispose<TPlatform>(ref TPlatform platform, APTR instance)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!MuiExternalWrapperCore.Valid(ref platform, instance)) return false;

		// Recursive, ownership-correct release of the external resource and every
		// owned block.
		MuiExternalWrapperCore.DisposeOwned(ref platform, instance);

		// Invalidate: a repeated disposal must find nothing to release or free.
		platform.Clear(instance, MuiExternalWrapperLayout.InstanceSize);
		return true;
	}
}
