/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiConstructionChildSlots
{
	internal APTR First;
	internal APTR Second;
	internal APTR Third;
}

internal static class MuiConstructionChildSlotsCore
{
	// Variable-length constructors can reserve one owning slot before each
	// child. Earlier slots remain parent-owned if a later reservation fails.
	internal static bool ReserveNext<TPlatform>(ref TPlatform platform, APTR owner,
		out APTR slot) where TPlatform : struct, IMuiHeadlessPlatform
	{
		slot = APTR.Null;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner, out var parent) ||
			parent.Boopsi.IsNull ||
			(parent.Flags & (MuiHeadlessObjectCore.ObjectDisposing |
				MuiHeadlessObjectCore.ObjectProviderBusy |
				MuiHeadlessObjectCore.ObjectConstructionBinding)) != 0 ||
			parent.ChildrenHead.IsNull != parent.ChildrenTail.IsNull ||
			!MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, owner,
				MuiHeadlessObjectField.Flags, out var flags)) return false;
		platform.WriteUInt32(flags, 0, parent.Flags | MuiHeadlessObjectCore.ObjectConstructionBinding);
		var node = MuiHeadlessMemory.Allocate(ref platform, MuiHeadlessChildRecord.Size);
		var append = APTR.Null;
		var ready = node.IsNotNull &&
			WriteSlot(ref platform, node, owner, parent.ChildrenTail, APTR.Null);
		if (ready && parent.ChildrenTail.IsNotNull)
			ready = MuiHeadlessChildCodec.TryRead(ref platform, parent.ChildrenTail, out var last) &&
				last.Next.IsNull && last.Owner.Raw == owner.Raw &&
				MuiHeadlessChildMemoryCodec.TryGetAddress(ref platform, parent.ChildrenTail,
					MuiHeadlessChildField.Next, out append);
		else if (ready)
			ready = MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, owner,
				MuiHeadlessObjectField.ChildrenHead, out append);
		if (!ready || !MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, owner,
			MuiHeadlessObjectField.ChildrenTail, out var tail))
		{
			FreeSlot(ref platform, node);
			platform.WriteUInt32(flags, 0, platform.ReadUInt32(flags, 0) &
				~MuiHeadlessObjectCore.ObjectConstructionBinding);
			return false;
		}
		platform.WriteUInt32(append, 0, node.Raw);
		platform.WriteUInt32(tail, 0, node.Raw);
		platform.WriteUInt32(flags, 0, platform.ReadUInt32(flags, 0) &
			~MuiHeadlessObjectCore.ObjectConstructionBinding);
		slot = node;
		return true;
	}

	// Once registration succeeds the slot owns even a partially initialized
	// record. A null result must be cleaned through the owner, not a lost local.
	internal static APTR CreateOwned<TPlatform>(ref TPlatform platform, APTR state,
		APTR owner, APTR slot, APTR classRecord)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, owner, out var parent) ||
			parent.Boopsi.IsNull ||
			(parent.Flags & (MuiHeadlessObjectCore.ObjectDisposing |
				MuiHeadlessObjectCore.ObjectProviderBusy |
				MuiHeadlessObjectCore.ObjectConstructionBinding)) != 0) return APTR.Null;
		var current = parent.ChildrenHead;
		uint visited = 0;
		while (current.IsNotNull && current.Raw != slot.Raw)
		{
			if (visited++ >= MuiHeadlessLayout.MaximumTraversal ||
				!MuiHeadlessChildCodec.TryRead(ref platform, current, out var next)) return APTR.Null;
			current = next.Next;
		}
		if (current.IsNull ||
			!MuiHeadlessChildCodec.TryRead(ref platform, slot, out var link) ||
			link.Owner.Raw != owner.Raw || link.Object.IsNotNull ||
			!MuiHeadlessChildMemoryCodec.TryGetAddress(ref platform, slot,
				MuiHeadlessChildField.Object, out var objectField) ||
			!MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, owner,
				MuiHeadlessObjectField.Flags, out var ownerFlags)) return APTR.Null;
		platform.WriteUInt32(ownerFlags, 0, parent.Flags | MuiHeadlessObjectCore.ObjectConstructionBinding);
		var child = MuiHeadlessObjectCore.CreateObjectInReservedSlot(ref platform, state,
			classRecord, APTR.Null, objectField);
		platform.WriteUInt32(ownerFlags, 0, platform.ReadUInt32(ownerFlags, 0) &
			~MuiHeadlessObjectCore.ObjectConstructionBinding);
		return child;
	}

	// False leaves disposal authority with the caller. This must not be used
	// by a constructor that drops its raw child handle on binding failure.
	internal static bool Bind<TPlatform>(ref TPlatform platform, APTR state,
		APTR owner, APTR slot, APTR child)
		where TPlatform : struct, IMuiHeadlessPlatform
		=> BindCore(ref platform, state, owner, slot, child, false, out _);

	// On transferred=true the slot owns cleanup even when retain returns false.
	internal static bool BindCreated<TPlatform>(ref TPlatform platform, APTR state,
		APTR owner, APTR slot, APTR child, out bool transferred)
		where TPlatform : struct, IMuiHeadlessPlatform
		=> BindCore(ref platform, state, owner, slot, child, true, out transferred);

	private static bool BindCore<TPlatform>(ref TPlatform platform, APTR state,
		APTR owner, APTR slot, APTR child, bool transfer, out bool transferred)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		transferred = false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, child);
		var cleanupOwner = APTR.Null;
		if (transfer) cleanupOwner = owner;
		if (record.IsNull || record.Raw == owner.Raw ||
			!MuiFamilyCore.AdmitsChildDisposal(ref platform, state, record,
				cleanupOwner) ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, owner, out var parent) ||
			(parent.Flags & (MuiHeadlessObjectCore.ObjectDisposing |
				MuiHeadlessObjectCore.ObjectProviderBusy |
				MuiHeadlessObjectCore.ObjectConstructionBinding)) != 0 ||
			!MuiHeadlessObjectCodec.TryRead(ref platform, record, out var value) ||
			value.Parent.IsNotNull ||
			(value.Flags & (MuiHeadlessObjectCore.ObjectDisposing |
				MuiHeadlessObjectCore.ObjectProviderBusy |
				MuiHeadlessObjectCore.ObjectConstructionBinding)) != 0) return false;
		var cursor = parent.ChildrenHead;
		uint visited = 0;
		var found = false;
		while (cursor.IsNotNull)
		{
			if (visited++ >= MuiHeadlessLayout.MaximumTraversal ||
				!MuiHeadlessChildCodec.TryRead(ref platform, cursor, out var next)) return false;
			if (cursor.Raw == slot.Raw) found = true;
			else if (next.Object.Raw == record.Raw) return false;
			cursor = next.Next;
		}
		if (!found ||
			!MuiHeadlessChildCodec.TryRead(ref platform, slot, out var link) ||
			link.Owner.Raw != owner.Raw ||
			(link.Object.IsNotNull && (!transfer || link.Object.Raw != record.Raw)) ||
			!MuiHeadlessChildMemoryCodec.TryGetAddress(ref platform, slot,
				MuiHeadlessChildField.Object, out var objectField) ||
			!MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, record,
				MuiHeadlessObjectField.Parent, out var parentField)) return false;
		if (transfer)
		{
			if (!MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, owner,
				MuiHeadlessObjectField.Flags, out var ownerFlags) ||
				!MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, record,
					MuiHeadlessObjectField.Flags, out var childFlags)) return false;
			platform.WriteUInt32(ownerFlags, 0, parent.Flags | MuiHeadlessObjectCore.ObjectConstructionBinding);
			platform.WriteUInt32(childFlags, 0, value.Flags | MuiHeadlessObjectCore.ObjectConstructionBinding);
			platform.WriteUInt32(objectField, 0, record.Raw);
			platform.WriteUInt32(parentField, 0, owner.Raw);
			transferred = true;
			var retained = platform.RetainObject(child);
			// Clear only our guard; preserve a provider's independent busy state.
			platform.WriteUInt32(childFlags, 0, platform.ReadUInt32(childFlags, 0) &
				~MuiHeadlessObjectCore.ObjectConstructionBinding);
			platform.WriteUInt32(ownerFlags, 0, platform.ReadUInt32(ownerFlags, 0) &
				~MuiHeadlessObjectCore.ObjectConstructionBinding);
			MuiHeadlessMemory.Mutated(ref platform, state);
			return retained;
		}
		if (!platform.RetainObject(child)) return false;
		// Requires the platform's retain operation to preserve admitted storage.
		platform.WriteUInt32(objectField, 0, record.Raw);
		platform.WriteUInt32(parentField, 0, owner.Raw);
		transferred = true;
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	// Reserve the complete owning topology before creating component objects.
	// Only an empty owner is accepted. Empty slots are cleanup-safe tombstones;
	// the constructor must separately distinguish incomplete from complete state.
	internal static bool Reserve<TPlatform>(ref TPlatform platform, APTR owner,
		out MuiConstructionChildSlots slots)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		slots = default;
		if (!IsEmptyOwner(ref platform, owner) ||
			!MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, owner,
				MuiHeadlessObjectField.Flags, out var ownerFlags)) return false;
		var flags = platform.ReadUInt32(ownerFlags, 0);
		if ((flags & MuiHeadlessObjectCore.ObjectConstructionBinding) != 0) return false;
		// Reservation calls the allocator before any owning links are visible.
		// Pin the owner against supported disposal/topology entry points first.
		platform.WriteUInt32(ownerFlags, 0, flags | MuiHeadlessObjectCore.ObjectConstructionBinding);
		var reserved = default(MuiConstructionChildSlots);
		reserved.First = MuiHeadlessMemory.Allocate(ref platform, MuiHeadlessChildRecord.Size);
		if (reserved.First.IsNotNull)
			reserved.Second = MuiHeadlessMemory.Allocate(ref platform, MuiHeadlessChildRecord.Size);
		if (reserved.Second.IsNotNull)
			reserved.Third = MuiHeadlessMemory.Allocate(ref platform, MuiHeadlessChildRecord.Size);
		if (reserved.Third.IsNull ||
			!WriteSlot(ref platform, reserved.First, owner, APTR.Null, reserved.Second) ||
			!WriteSlot(ref platform, reserved.Second, owner, reserved.First, reserved.Third) ||
			!WriteSlot(ref platform, reserved.Third, owner, reserved.Second, APTR.Null) ||
			!IsEmptyOwner(ref platform, owner) ||
			!MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, owner,
				MuiHeadlessObjectField.ChildrenHead, out var head) ||
			!MuiHeadlessObjectMemoryCodec.TryGetAddress(ref platform, owner,
				MuiHeadlessObjectField.ChildrenTail, out var tail))
		{
			FreeSlot(ref platform, reserved.First);
			FreeSlot(ref platform, reserved.Second);
			FreeSlot(ref platform, reserved.Third);
			platform.WriteUInt32(ownerFlags, 0, platform.ReadUInt32(ownerFlags, 0) &
				~MuiHeadlessObjectCore.ObjectConstructionBinding);
			return false;
		}
		// No provider callbacks or fallible admissions between these publications.
		platform.WriteUInt32(head, 0, reserved.First.Raw);
		platform.WriteUInt32(tail, 0, reserved.Third.Raw);
		platform.WriteUInt32(ownerFlags, 0, platform.ReadUInt32(ownerFlags, 0) &
			~MuiHeadlessObjectCore.ObjectConstructionBinding);
		slots = reserved;
		return true;
	}

	private static bool IsEmptyOwner<TPlatform>(ref TPlatform platform, APTR owner)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCodec.TryRead(ref platform, owner, out var value) &&
		value.Boopsi.IsNotNull &&
		value.ChildrenHead.IsNull && value.ChildrenTail.IsNull &&
		(value.Flags & (MuiHeadlessObjectCore.ObjectDisposing |
			MuiHeadlessObjectCore.ObjectProviderBusy)) == 0;

	private static bool WriteSlot<TPlatform>(ref TPlatform platform, APTR slot,
		APTR owner, APTR previous, APTR next)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var value = default(MuiHeadlessChildRecord);
		value.Owner = owner;
		value.Previous = previous;
		value.Next = next;
		return MuiHeadlessChildCodec.Write(ref platform, slot, value);
	}

	private static void FreeSlot<TPlatform>(ref TPlatform platform, APTR slot)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (slot.IsNotNull) platform.Free(slot, MuiHeadlessChildRecord.Size);
	}
}
