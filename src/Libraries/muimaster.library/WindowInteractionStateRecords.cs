/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Window interaction state shared by Snapshot and cycle-chain/active-object
// methods. Public attributes remain projections; the remembered position
// request and copied cycle chain are kept in one named guest record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowInteractionStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x57495354u; // 'WIST'

	internal uint Magic;
	internal uint SnapshotFlags;
	internal uint SnapshotRequests;
	internal APTR CycleChainHead;
	internal uint CycleChainCount;
	internal uint CycleChainRequests;
}

// Interaction state carries one canonical Snapshot BOOL and a guest-resident
// cycle-chain capability. The structural predicate checks the record's own
// scalar/pointer shape; the live predicate additionally proves the bounded
// chain topology and object ownership before Window consumers traverse it.
internal static class MuiWindowInteractionStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiWindowInteractionStateRecord.Cookie &&
		value.SnapshotFlags <= 1 &&
		value.CycleChainCount <= MuiHeadlessLayout.MaximumTraversal &&
		(value.CycleChainCount == 0
			? value.CycleChainHead.IsNull
			: IsMapped(ref platform, value.CycleChainHead,
				MuiApplicationWindowNodeRecord.Size));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(ref platform, value) || owner.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, owner).IsNull)
			return false;
		if (value.CycleChainCount == 0) return value.CycleChainHead.IsNull;

		var node = value.CycleChainHead;
		for (var index = 0u; index < value.CycleChainCount; index++)
		{
			if (node.IsNull || !MuiApplicationWindowNodeCodec.TryRead(ref platform,
				node, out var record) || record.Sequence != index + 1 ||
				record.Auxiliary != 0 || record.Packet != 0 || record.Value.IsNull ||
				MuiHeadlessObjectCore.FindObject(ref platform, state,
					record.Value).IsNull) return false;
			if (index + 1 == value.CycleChainCount)
				return record.Next.IsNull;
			node = record.Next;
		}
		return false;
	}

	private static bool IsMapped<TPlatform>(ref TPlatform platform, APTR value,
		uint size) where TPlatform : struct, IMuiGuestMemory =>
		!value.IsNull && platform.IsMapped(value, size);
}

internal enum MuiWindowInteractionStateField : byte
{
	Magic,
	SnapshotFlags,
	SnapshotRequests,
	CycleChainHead,
	CycleChainCount,
	CycleChainRequests,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWindowInteractionStateFieldCursor
{
	internal APTR Record;
	internal MuiWindowInteractionStateField Field;
}

internal static class MuiWindowInteractionStateFieldCursorCodec
{
	private static bool TryResolve(MuiWindowInteractionStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiWindowInteractionStateField.Magic:
			case MuiWindowInteractionStateField.SnapshotFlags:
			case MuiWindowInteractionStateField.SnapshotRequests:
			case MuiWindowInteractionStateField.CycleChainHead:
			case MuiWindowInteractionStateField.CycleChainCount:
			case MuiWindowInteractionStateField.CycleChainRequests:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowInteractionStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiWindowInteractionStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInteractionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiWindowInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInteractionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiWindowInteractionStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Snapshot flags, request counters, and
// cycle-chain topology remain named semantic fields; this bounded adapter is
// the sole fixed-layout translation for the guest record.
internal static class MuiWindowInteractionStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowInteractionStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowInteractionStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiWindowInteractionStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiWindowInteractionStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, 0, out var magic) ||
			!MuiWindowInteractionStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, 4, out value.SnapshotFlags) ||
			!MuiWindowInteractionStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, 8, out value.SnapshotRequests) ||
			!MuiWindowInteractionStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, 12, out var cycleHead) ||
			!MuiWindowInteractionStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, 16, out value.CycleChainCount) ||
			!MuiWindowInteractionStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, 20, out value.CycleChainRequests)) return false;
		value.Magic = magic;
		value.CycleChainHead = APTR.FromPointer(cycleHead);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiWindowInteractionStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowInteractionStateAdmission.Validate(ref platform, value))
			return false;
		return MuiWindowInteractionStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiWindowInteractionStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.SnapshotFlags) &&
			MuiWindowInteractionStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 8, value.SnapshotRequests) &&
			MuiWindowInteractionStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 12, value.CycleChainHead.Raw) &&
			MuiWindowInteractionStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 16, value.CycleChainCount) &&
			MuiWindowInteractionStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 20, value.CycleChainRequests);
	}
}
