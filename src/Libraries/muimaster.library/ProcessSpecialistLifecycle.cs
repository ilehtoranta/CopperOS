/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Class-owned teardown for the MG09 Process.mui / Slave.mui specialist family.
// Both classes are Semaphore.mui subclasses layered over a real headless
// object. Their OM_DISPOSE-equivalent must release the class-owned state (the
// copied Process Name block and the per-object sidecar) and, for a still-running
// Process, kill the launched scheduler task so it is never orphaned, before
// handing the object to the frozen object core. Caller/app-owned references
// (SourceClass/SourceObject/Application/Class/Object) are never freed, and a
// repeated disposal finds no sidecar and is a safe no-op.
public static class MuiProcessSpecialistLifecycle
{
	public static bool Dispose<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiServicePlatform =>
		MuiProcessSpecialistCore.Dispose(ref platform, state, obj);
}
