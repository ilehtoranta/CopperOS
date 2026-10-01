/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Serialized under the native service/class scope. Root and linked receipt
// allocations stay alive across callbacks. Memory admission itself is not a
// scheduler or arbitrary guest callback. Allocate supplies fresh disjoint storage
// and native Free cannot fail.
internal static class MuiConstructionReceiptCore
{
	internal static bool TryConstruct<T>(ref T platform, APTR root, APTR state,
		APTR dispatchClass, APTR owningClass, APTR operand, APTR message,
		out APTR receipt, out MuiBoopsiConstructionResult result)
		where T : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (!TryBegin(ref platform, root, state, dispatchClass, owningClass, out receipt)) return false;
		if (!MuiConstructionReceiptCodec.TryRead(ref platform, receipt,
			out var running)) return false;
		running.Phase = MuiConstructionReceiptRecord.Running;
		if (!MuiConstructionReceiptCodec.Write(ref platform, receipt, running)) return false;
		var complete = MuiBoopsiBaseConstructionCore.TryConstructRetained(ref platform, state,
			dispatchClass, owningClass, operand, message, receipt, out result);
		// NativeObject was already recorded immediately after superclass return,
		// before attachment's first fallible allocation. Running blocks retirement.
		if (!MuiConstructionReceiptCodec.TryRead(ref platform, receipt,
			out var returned)) return false;
		returned.InitializedObject = result.InitializedObject;
		returned.Ownership = (uint)result.Ownership;
		returned.Phase = MuiConstructionReceiptRecord.Returned;
		if (!MuiConstructionReceiptCodec.Write(ref platform, receipt, returned)) return false;
		return complete;
	}

	internal static bool TryBegin<T>(ref T platform, APTR root, APTR state,
		APTR dispatchClass, APTR owningClass, out APTR receipt)
		where T : struct, IMuiAllocationPlatform
	{
		receipt = APTR.Null;
		if (state.IsNull || !MuiMasterPrivateRootCodec.TryRead(ref platform, root, out var before) ||
			before.ClassRegistry != state.Raw || (before.Flags & MuiMasterLifecycleCore.RootDisposing) != 0 ||
			!TryFind(ref platform, root, APTR.Null, out _, out _)) return false;
		var address = platform.Allocate(MuiConstructionReceiptRecord.Size, MuiHeadlessLayout.AllocationFlags);
		if (address.IsNull) return false;
		// Allocation may reenter. Link against fresh root state, never the old head.
		if (!MuiMasterPrivateRootCodec.TryRead(ref platform, root, out var fresh) ||
			fresh.ClassRegistry != state.Raw || fresh.RegistryGeneration != before.RegistryGeneration ||
			(fresh.Flags & MuiMasterLifecycleCore.RootDisposing) != 0 ||
			!TryFind(ref platform, root, APTR.Null, out _, out _))
		{
			platform.Free(address, MuiConstructionReceiptRecord.Size);
			return false;
		}
		MuiConstructionReceiptRecord value = default;
		value.Magic = MuiConstructionReceiptRecord.Cookie;
		value.Root = root;
		value.State = state;
		value.Next = fresh.PendingConstructionHead;
		value.DispatchClass = dispatchClass;
		value.OwningClass = owningClass;
		value.Phase = MuiConstructionReceiptRecord.Reserved;
		value.RootGeneration = fresh.RegistryGeneration;
		value.DisposeMethod = BOOPSI.OM_DISPOSE;
		if (!MuiConstructionReceiptCodec.Write(ref platform, address, value))
		{
			platform.Free(address, MuiConstructionReceiptRecord.Size);
			return false;
		}
		fresh.PendingConstructionHead = address;
		if (!MuiMasterPrivateRootCodec.Write(ref platform, root, fresh))
		{
			platform.Free(address, MuiConstructionReceiptRecord.Size);
			return false;
		}
		receipt = address;
		return true;
	}

	// Called only after derived-class construction succeeds. Removing this
	// receipt transfers the result to the existing registered object lifetime.
	internal static bool TryCommit<T>(ref T platform, APTR root, APTR receipt)
		where T : struct, IMuiHeadlessPlatform
	{
		if (!TryFind(ref platform, root, receipt, out var value, out _) ||
			value.Phase != MuiConstructionReceiptRecord.Returned ||
			value.Ownership != (uint)MuiObjectAttachmentOwnership.Transferred ||
			value.NativeObject.IsNull || value.InitializedObject != value.NativeObject ||
			!MuiHeadlessObjectCore.TryFindObject(ref platform, value.State, value.NativeObject, out var sidecar) ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, sidecar, out var obj) || obj.Class != value.OwningClass ||
			(obj.Flags & MuiHeadlessObjectCore.ObjectInitialized) == 0 ||
			(obj.Flags & (MuiHeadlessObjectCore.ObjectDisposing | MuiHeadlessObjectCore.ObjectProviderBusy |
				MuiHeadlessObjectCore.ObjectConstructionBinding | MuiHeadlessObjectCore.ObjectControlConstructionActive |
				MuiHeadlessObjectCore.ObjectRadioConstructionPending | MuiHeadlessObjectCore.ObjectScrollbarConstructionPending)) != 0)
			return false;
		return TryRemove(ref platform, root, receipt);
	}

	internal static bool TryDiscard<T>(ref T platform, APTR root, APTR receipt)
		where T : struct, IMuiAllocationPlatform
	{
		if (!TryFind(ref platform, root, receipt, out var value, out _) ||
			(value.Phase != MuiConstructionReceiptRecord.Reserved && value.Phase != MuiConstructionReceiptRecord.Returned) ||
			(value.NativeObject.IsNotNull && value.Ownership != (uint)MuiObjectAttachmentOwnership.AlreadyRegistered))
			return false;
		return TryRemove(ref platform, root, receipt);
	}

	// Constructor-failure recovery at the base stage. Derived resources must be
	// cleaned first. Unresolved aliases are retained, never guessed to be owned.
	internal static bool TryCleanup<T>(ref T platform, APTR root, APTR receipt)
		where T : struct, IMuiHeadlessPlatform
	{
		if (!TryFind(ref platform, root, receipt, out var value, out _)) return false;
		if (value.Phase == MuiConstructionReceiptRecord.Done) return TryRemove(ref platform, root, receipt);
		if (value.Phase != MuiConstructionReceiptRecord.Returned) return false;
		if (value.NativeObject.IsNull || value.Ownership == (uint)MuiObjectAttachmentOwnership.AlreadyRegistered)
			return TryDiscard(ref platform, root, receipt);
		if (value.Ownership != (uint)MuiObjectAttachmentOwnership.CallerOwned &&
			value.Ownership != (uint)MuiObjectAttachmentOwnership.Transferred) return false;
		if (!MuiBoopsiRegisteredClassCore.TryResolveDispatchClass(ref platform, value.State,
			value.OwningClass, value.DispatchClass, out var dispatch) ||
			!MuiHeadlessObjectCore.TryFindObject(ref platform, value.State, value.NativeObject, out var sidecar)) return false;
		var registered = value.Ownership == (uint)MuiObjectAttachmentOwnership.Transferred;
		if (registered)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, sidecar, out var obj) || obj.Class != value.OwningClass) return false;
		}
		else if (sidecar.IsNotNull) return false;
		if (!MuiConstructionReceiptCodec.TryGetLocations(ref platform, receipt,
			out var fields)) return false;
		value.Phase = MuiConstructionReceiptRecord.Cleaning;
		if (!MuiConstructionReceiptCodec.Write(ref platform, receipt, value)) return false;
		if (registered)
		{
			if (!MuiBoopsiBaseDisposalCore.TryDispose(ref platform, value.State, value.DispatchClass,
				value.NativeObject, fields.DisposeMethod, out _))
			{
				if (MuiConstructionReceiptCodec.TryRead(ref platform, receipt,
					out var refused))
				{
					refused.Phase = MuiConstructionReceiptRecord.Returned;
					MuiConstructionReceiptCodec.Write(ref platform, receipt, refused);
				}
				return false;
			}
		}
		else platform.DoSuperMethod(dispatch.Boopsi, value.NativeObject, fields.DisposeMethod);
		// Record successful native cleanup before fallible unlink admission. A
		// later retry of Done removes only the receipt and never calls super twice.
		if (!MuiConstructionReceiptCodec.TryRead(ref platform, receipt,
			out var completed)) return false;
		completed.NativeObject = APTR.Null;
		completed.InitializedObject = APTR.Null;
		completed.Ownership = (uint)MuiObjectAttachmentOwnership.Unresolved;
		completed.Phase = MuiConstructionReceiptRecord.Done;
		if (!MuiConstructionReceiptCodec.Write(ref platform, receipt, completed)) return false;
		return TryRemove(ref platform, root, receipt);
	}

	internal static bool TryRead<T>(ref T memory, APTR root, APTR receipt,
		out MuiConstructionReceiptRecord value) where T : struct, IMuiGuestMemory
	{
		value = default;
		return receipt.IsNotNull && TryFind(ref memory, root, receipt, out value, out _);
	}

	private static bool TryFind<T>(ref T memory, APTR root, APTR target,
		out MuiConstructionReceiptRecord selected, out APTR previous)
		where T : struct, IMuiGuestMemory
	{
		selected = default;
		previous = APTR.Null;
		if (!MuiMasterPrivateRootCodec.TryRead(ref memory, root, out var owner) || owner.ClassRegistry == 0) return false;
		var cursor = owner.PendingConstructionHead;
		var predecessor = APTR.Null;
		var found = false;
		uint visited = 0;
		while (cursor.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiConstructionReceiptCodec.TryRead(ref memory, cursor, out var item) ||
				item.Root != root || item.State.Raw != owner.ClassRegistry || item.RootGeneration != owner.RegistryGeneration ||
				item.Next == cursor) return false;
			if (cursor == target)
			{
				if (found) return false;
				found = true;
				selected = item;
				previous = predecessor;
			}
			predecessor = cursor;
			cursor = item.Next;
		}
		return cursor.IsNull && (target.IsNull || found);
	}

	private static bool TryRemove<T>(ref T platform, APTR root, APTR receipt)
		where T : struct, IMuiAllocationPlatform
	{
		if (receipt.IsNull || !TryFind(ref platform, root, receipt, out var value, out var previous)) return false;
		if (previous.IsNull)
		{
			if (!MuiMasterPrivateRootCodec.TryRead(ref platform, root, out var owner) ||
				owner.PendingConstructionHead != receipt) return false;
			owner.PendingConstructionHead = value.Next;
			if (!MuiMasterPrivateRootCodec.Write(ref platform, root, owner)) return false;
		}
		else
		{
			if (!MuiConstructionReceiptCodec.TryRead(ref platform, previous,
				out var previousValue) || previousValue.Next != receipt) return false;
			previousValue.Next = value.Next;
			if (!MuiConstructionReceiptCodec.Write(ref platform, previous,
				previousValue)) return false;
		}
		platform.Clear(receipt, MuiConstructionReceiptRecord.Size);
		platform.Free(receipt, MuiConstructionReceiptRecord.Size);
		return true;
	}
}
