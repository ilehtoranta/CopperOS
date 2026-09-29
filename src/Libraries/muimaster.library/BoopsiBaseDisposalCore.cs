/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// The base MUI destructor stage, entered once after derived-class cleanup.
// This is not installed in every class dispatcher: doing so would retire the
// same sidecar at multiple levels of a superclass chain. The native gateway
// must retain its service/class scope until this call and DoSuperMethod return.
// It must also own an immutable method packet for that duration (normally an
// SDK BOOPSI.Message on the native stack), not a pointer into retiring object
// storage. This private stage does not pin arbitrary caller memory.
internal static class MuiBoopsiBaseDisposalCore
{
	// Completion is separate from the BOOPSI result (a successful destructor may
	// return zero). On false, no superclass destructor has been called, and the
	// existing object/child/font cleanup records retain retry authority. A public
	// void DisposeObject wrapper must not reinterpret false as successful release.
	internal static bool TryDispose<T>(ref T platform, APTR state,
		APTR dispatchClassRecord, APTR obj, APTR message, out uint result)
		where T : struct, IMuiHeadlessPlatform
	{
		result = 0;
		if (message.IsNull || (message.Raw & 1u) != 0 ||
			!MuiBoopsiMethodMessageCodec.TryReadMethodId(ref platform, message,
				out var method) || method != BOOPSI.OM_DISPOSE ||
			!MuiHeadlessObjectCore.TryFindObject(ref platform, state, obj, out var record) ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, record, out var owner) ||
			!MuiBoopsiRegisteredClassCore.TryResolveDispatchClass(ref platform, state, owner.Class,
				dispatchClassRecord, out var dispatchClass)) return false;
		if (!MuiHeadlessObjectCore.DisposeObjectState(ref platform, state, obj, false))
			return false;
		// Cleanup deliberately did not invoke the most-derived DisposeObject.
		// Use the dispatching class, not the most-derived owning class, and do not
		// touch sidecar/class storage after the superclass callback can release it.
		result = platform.DoSuperMethod(dispatchClass.Boopsi, obj, message);
		return true;
	}

}

// Shared registered-hierarchy admission for the base constructor and destructor.
internal static class MuiBoopsiRegisteredClassCore
{
	internal static bool TryResolveDispatchClass<T>(ref T platform, APTR state,
		APTR owningClass, APTR dispatchRecord, out MuiHeadlessClassRecord dispatchClass)
		where T : struct, IMuiGuestMemory
	{
		dispatchClass = default;
		if (owningClass.IsNull || dispatchRecord.IsNull ||
			!MuiHeadlessStateCodec.TryRead(ref platform, state, out var registry)) return false;
		// Count the registered classes once. That count bounds both superclass
		// traversal and lookup, so cycles cannot repeatedly revisit a small registry.
		var cursor = registry.Classes;
		uint count = 0;
		while (cursor.IsNotNull && count < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, cursor, out var value)) return false;
			count++;
			cursor = value.Next;
		}
		if (cursor.IsNotNull || count == 0 ||
			!TryFindClass(ref platform, registry.Classes, count, dispatchRecord,
				APTR.Null, out dispatchClass) || dispatchClass.Boopsi.IsNull ||
			!TryFindClass(ref platform, registry.Classes, count, owningClass,
				APTR.Null, out var current)) return false;
		for (uint depth = 0; depth < count; depth++)
		{
			if (current.Boopsi == dispatchClass.Boopsi) return true;
			if (current.Super.IsNull || !TryFindClass(ref platform, registry.Classes,
				count, APTR.Null, current.Super, out current)) return false;
		}
		return false;
	}

	private static bool TryFindClass<T>(ref T platform, APTR head, uint count,
		APTR record, APTR boopsi, out MuiHeadlessClassRecord value)
		where T : struct, IMuiGuestMemory
	{
		value = default;
		var cursor = head;
		for (uint visited = 0; cursor.IsNotNull && visited < count; visited++)
		{
			if (!MuiHeadlessClassCodec.TryRead(ref platform, cursor, out value)) return false;
			if (record.IsNotNull ? cursor == record : value.Boopsi == boopsi) return true;
			cursor = value.Next;
		}
		return false;
	}
}
