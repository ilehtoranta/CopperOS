/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Both allocations are detached and caller-owned. No allocation, loader or
// dispatcher calls occur during publication. The caller serializes mutations
// and keeps admitted storage stable; failed preparation publishes neither list.
internal static class MuiExternalClassPublication
{
	internal static bool TryPublish<T>(ref T memory, APTR service, APTR registry,
		APTR classRecord, APTR leaseRecord, APTR name, APTR boopsi, APTR library)
		where T : struct, IMuiGuestMemory
	{
		if (!MuiClassServiceStateCodec.TryRead(ref memory, service, out var state) ||
			state.Magic != MuiClassServiceLayout.Magic || state.Generation == 0 ||
			state.Headless != registry ||
			!MuiHeadlessStateCodec.TryRead(ref memory, registry, out var headless) ||
			headless.Magic != MuiHeadlessLayout.Magic || headless.Version != MuiHeadlessLayout.Version ||
			name.IsNull || boopsi.IsNull || library.IsNull) return false;
		var cls = default(MuiHeadlessClassRecord);
		cls.Next = headless.Classes;
		cls.Name = name;
		cls.Boopsi = boopsi;
		cls.Flags = MuiHeadlessObjectCore.ClassExternal;
		var lease = default(MuiClassServiceLeaseRecord);
		lease.Next = state.Head;
		lease.Flags = MuiClassServiceLayout.FlagExternal | MuiClassServiceLayout.FlagOwnsClassId;
		lease.ClassId = name;
		lease.Boopsi = boopsi;
		lease.LibraryBase = library;
		lease.RefCount = 1;
		lease.HeadlessClass = classRecord;
		if (!MuiHeadlessClassCodec.Write(ref memory, classRecord, cls) ||
			!MuiClassServiceLeaseCodec.Write(ref memory, leaseRecord, lease)) return false;
		return TryCommitLinks(ref memory, service, registry, classRecord, leaseRecord,
			unchecked(headless.Mutation + 1));
	}

	// Resolve every named destination before the first visible write. A late
	// mapping refusal cannot leave a registry without its owning loader lease.
	private static bool TryCommitLinks<T>(ref T memory, APTR service, APTR registry,
		APTR classRecord, APTR leaseRecord, uint mutation) where T : struct, IMuiGuestMemory
	{
		if (!MuiClassRecordMemoryCodec.TryGetAddress(ref memory, service,
			MuiClassRecordKind.State, MuiClassRecordField.Head, out var serviceHead) ||
			!MuiHeadlessStateMemoryCodec.TryGetAddress(ref memory, registry,
				MuiHeadlessStateField.Classes, out var classes) ||
			!MuiHeadlessStateMemoryCodec.TryGetAddress(ref memory, registry,
				MuiHeadlessStateField.Mutation, out var generation)) return false;
		memory.WriteUInt32(classes, 0, classRecord.Raw);
		memory.WriteUInt32(serviceHead, 0, leaseRecord.Raw);
		memory.WriteUInt32(generation, 0, mutation);
		return true;
	}
}
