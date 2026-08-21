/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Native-safe public MUI_DisposeObject boundaries. The ordinary path owns the
// headless object record; the class-service path additionally releases the
// guest-resident class lease held by an external-aware object factory.
public static class MuiObjectDisposalServiceCore
{
	public static bool DisposeObject<TPlatform>(ref TPlatform platform,
		APTR headlessState, APTR obj)
		where TPlatform : struct, IMuiServicePlatform =>
		MuiProcessSpecialistCore.Valid(ref platform, headlessState, obj)
			? MuiProcessSpecialistLifecycle.Dispose(ref platform, headlessState, obj)
			: MuiMenuSpecialistCore.Valid(ref platform, headlessState, obj)
				? MuiMenuSpecialistLifecycle.Dispose(ref platform, headlessState, obj)
			: MuiMiscSpecialistCore.ValidObject(ref platform, headlessState, obj)
				? MuiMiscSpecialistLifecycle.Dispose(ref platform, headlessState, obj)
			: MuiHeadlessObjectCore.DisposeObject(ref platform, headlessState, obj);

	// Public-vector form with both resident state blocks. Objects created by
	// the external-aware factory are routed through the lease-aware path;
	// ordinary registry objects use the direct headless lifecycle.
	public static bool DisposeObject<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR headlessState, APTR obj)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (obj.IsNull) return false;
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			headlessState, obj);
		var classPointer = MuiHeadlessObjectCore.ClassPointer(ref platform,
			classRecord);
		if (classPointer.IsNotNull &&
			MuiClassServiceCore.ObjectLeaseCount(ref platform, serviceState,
				classPointer) != 0)
			return DisposeObjectWithClassService(ref platform, serviceState,
				headlessState, obj);
		return DisposeObject(ref platform, headlessState, obj);
	}

	public static bool DisposeObjectWithClassService<TPlatform>(
		ref TPlatform platform, APTR serviceState, APTR headlessState, APTR obj)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (obj.IsNull) return false;
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			headlessState, obj);
		var classPointer = MuiHeadlessObjectCore.ClassPointer(ref platform,
			classRecord);
		if (classPointer.IsNull ||
			MuiClassServiceCore.ObjectLeaseCount(ref platform, serviceState,
				classPointer) == 0) return false;
		var disposed = MuiProcessSpecialistCore.Valid(ref platform, headlessState,
			obj)
			? MuiProcessSpecialistLifecycle.Dispose(ref platform, headlessState, obj)
			: MuiMenuSpecialistCore.Valid(ref platform, headlessState, obj)
				? MuiMenuSpecialistLifecycle.Dispose(ref platform, headlessState, obj)
			: MuiMiscSpecialistCore.ValidObject(ref platform, headlessState, obj)
				? MuiMiscSpecialistLifecycle.Dispose(ref platform, headlessState, obj)
			: MuiHeadlessObjectCore.DisposeObject(ref platform, headlessState, obj);
		if (!disposed) return false;
		return MuiClassServiceCore.ReleaseObjectLease(ref platform, serviceState,
			classPointer);
	}
}
