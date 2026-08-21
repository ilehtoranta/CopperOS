/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Native-safe public MUI_NewObjectA boundaries. Class resolution is
// case-sensitive and uses guest-resident registry and class-service state.
public static class MuiObjectFactoryServiceCore
{
	public const uint MaximumClassNameLength = 255;

	public static APTR NewObjectA<TPlatform>(ref TPlatform platform, APTR state,
		APTR className, APTR tags) where TPlatform : struct, IMuiServicePlatform
	{
		if (!ReadyClassName(ref platform, className) ||
			!MuiAslTagListCore.Validate(ref platform, tags)) return APTR.Null;
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref platform,
			state, className);
		if (classRecord.IsNull) return APTR.Null;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			classRecord, tags);
		if (obj.IsNull || AdoptSpecialist(ref platform, state, obj))
			return obj;
		DisposeCreatedObject(ref platform, state, obj);
		return APTR.Null;
	}

	public static APTR NewObjectAWithClassService<TPlatform>(ref TPlatform platform,
		APTR serviceState, APTR headlessState, APTR className, APTR tags)
		where TPlatform : struct, IMuiServicePlatform
	{
		if (!ReadyClassName(ref platform, className) ||
			!MuiAslTagListCore.Validate(ref platform, tags)) return APTR.Null;
		var classPointer = MuiClassServiceCore.GetClass(ref platform,
			serviceState, className);
		if (classPointer.IsNull) return APTR.Null;
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref platform,
			headlessState, className);
		if (classRecord.IsNull)
		{
			MuiClassServiceCore.FreeClass(ref platform, serviceState,
				classPointer);
			return APTR.Null;
		}
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform,
			headlessState, classRecord, tags);
		if (obj.IsNull)
		{
			MuiClassServiceCore.FreeClass(ref platform, serviceState,
				classPointer);
			return APTR.Null;
		}
		if (!AdoptSpecialist(ref platform, headlessState, obj))
		{
			DisposeCreatedObject(ref platform, headlessState, obj);
			MuiClassServiceCore.FreeClass(ref platform, serviceState,
				classPointer);
			return APTR.Null;
		}
		if (MuiClassServiceCore.TrackObjectLease(ref platform, serviceState,
			classPointer)) return obj;
		DisposeCreatedObject(ref platform, headlessState, obj);
		MuiClassServiceCore.FreeClass(ref platform, serviceState, classPointer);
		return APTR.Null;
	}

	// Compatibility forwarding surface for callers that historically paired the
	// factory with its lease-aware disposal helper. The implementation lives in
	// the disposal service so dispatcher-only closures do not pull the factory
	// construction graph into their native image.
	public static bool DisposeObjectWithClassService<TPlatform>(
		ref TPlatform platform, APTR serviceState, APTR headlessState, APTR obj)
		where TPlatform : struct, IMuiServicePlatform =>
		MuiObjectDisposalServiceCore.DisposeObjectWithClassService(ref platform,
			serviceState, headlessState, obj);

	// Direct object factories resolve specialist classes by their official class
	// id and attach guest-resident state after OM_NEW/tag application. A failed
	// sidecar allocation rolls the object back without exposing a partially
	// initialized instance. All other classes remain on the frozen headless path.
	private static bool AdoptSpecialist<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform
	{
		// The public factory is also the construction boundary for the frozen
		// common-control families.  OM_NEW/tag application has already happened
		// in CreateObjectA; run the class-aware normalization now so Numeric,
		// String, Image, Prop, Gauge, and the other controls receive their
		// bounded defaults and failure-atomic owned payloads before any additive
		// specialist sidecar is attached.  Unknown/custom classes remain a no-op.
		var classRecord = MuiHeadlessObjectCore.ObjectClassRecord(ref platform,
			state, obj);
		if (classRecord.IsNull || !MuiCommonControlCore.Construct(ref platform,
			state, classRecord, obj)) return false;

		var processClass = MuiProcessSpecialistCore.ClassifyObject(ref platform,
			state, obj);
		if (processClass != MuiProcessSpecialistClass.None)
			return MuiProcessSpecialistCore.AttachByObject(ref platform, state, obj)
				.IsNotNull;
		var cls = MuiMenuSpecialistCore.ClassifyObject(ref platform, state, obj);
		if (cls != MuiMenuSpecialistClass.None)
			return MuiMenuSpecialistCore.AttachByObject(ref platform, state, obj)
				.IsNotNull;
		var miscClass = MuiMiscSpecialistCore.ClassifyObject(ref platform, state,
			obj);
		return miscClass == MuiMiscSpecialistClass.None ||
			MuiMiscSpecialistCore.AttachByObject(ref platform, state, obj).IsNotNull;
	}

	private static bool DisposeCreatedObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiServicePlatform =>
		MuiProcessSpecialistCore.Valid(ref platform, state, obj)
			? MuiProcessSpecialistLifecycle.Dispose(ref platform, state, obj)
			: MuiMenuSpecialistCore.Valid(ref platform, state, obj)
				? MuiMenuSpecialistLifecycle.Dispose(ref platform, state, obj)
			: MuiMiscSpecialistCore.ValidObject(ref platform, state, obj)
				? MuiMiscSpecialistLifecycle.Dispose(ref platform, state, obj)
			: MuiHeadlessObjectCore.DisposeObject(ref platform, state, obj);

	private static bool ReadyClassName<TPlatform>(ref TPlatform platform,
		APTR className) where TPlatform : struct, IMuiServicePlatform
	{
		if (className.IsNull) return false;
		uint length;
		return CStringCodec.TryReadLength(ref platform, className,
			MaximumClassNameLength + 1, out length) && length != 0;
	}
}
