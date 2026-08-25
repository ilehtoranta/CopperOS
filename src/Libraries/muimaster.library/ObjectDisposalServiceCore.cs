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
		DisposeKnownObject(ref platform, headlessState, obj);

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
		var disposed = DisposeKnownObject(ref platform, headlessState, obj);
		if (!disposed) return false;
		return MuiClassServiceCore.ReleaseObjectLease(ref platform, serviceState,
			classPointer);
	}

	// The public disposal vectors also receive the standalone MG09 wrapper
	// instances. They are not entries in the generic headless object list, so a
	// generic fallback would report success=false and strand the wrapper's
	// external class/picture and owned guest blocks. Keep the routing typed and
	// centralized so both ordinary and class-service forms have identical
	// exactly-once teardown semantics.
	private static bool DisposeKnownObject<TPlatform>(ref TPlatform platform,
		APTR headlessState, APTR obj)
		where TPlatform : struct, IMuiServicePlatform =>
		MuiProcessSpecialistCore.Valid(ref platform, headlessState, obj)
			? MuiProcessSpecialistLifecycle.Dispose(ref platform, headlessState, obj)
			: MuiMenuSpecialistCore.Valid(ref platform, headlessState, obj)
				? MuiMenuSpecialistLifecycle.Dispose(ref platform, headlessState, obj)
			: MuiMiscSpecialistCore.ValidObject(ref platform, headlessState, obj)
				? MuiMiscSpecialistLifecycle.Dispose(ref platform, headlessState, obj)
			: MuiExternalWrapperCore.Valid(ref platform, obj)
				? MuiExternalWrapperLifecycle.Dispose(ref platform, obj)
			: MuiHeadlessObjectCore.DisposeObject(ref platform, headlessState, obj);
}
