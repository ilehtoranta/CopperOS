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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint SnapshotFlagsOffset = 4;
	internal const uint SnapshotRequestsOffset = 8;
	internal const uint CycleChainHeadOffset = 12;
	internal const uint CycleChainCountOffset = 16;
	internal const uint CycleChainRequestsOffset = 20;
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
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiWindowInteractionStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		var skips = field switch
		{
			MuiWindowInteractionStateField.Magic => 0u,
			MuiWindowInteractionStateField.SnapshotFlags => 1u,
			MuiWindowInteractionStateField.SnapshotRequests => 2u,
			MuiWindowInteractionStateField.CycleChainHead => 3u,
			MuiWindowInteractionStateField.CycleChainCount => 4u,
			MuiWindowInteractionStateField.CycleChainRequests => 5u,
			_ => uint.MaxValue,
		};
		if (skips == uint.MaxValue) return false;
		for (var i = 0u; i < skips; i++)
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiWindowInteractionStateRecord.FieldSize, out _)) return false;
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiWindowInteractionStateRecord.FieldSize, out address);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWindowInteractionStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
			MuiWindowInteractionStateRecord.Size, out var structCursor) ||
			!TryTakeField(ref platform, ref structCursor, cursor.Field, out address))
			return false;
		return true;
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
// cycle-chain topology remain named semantic fields; typed selection walks the
// packed struct. The numeric overload remains a compatibility bridge for older
// callers that carry an ABI offset.
internal static class MuiWindowInteractionStateRecordMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiWindowInteractionStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		var skips = field switch
		{
			MuiWindowInteractionStateField.Magic => 0u,
			MuiWindowInteractionStateField.SnapshotFlags => 1u,
			MuiWindowInteractionStateField.SnapshotRequests => 2u,
			MuiWindowInteractionStateField.CycleChainHead => 3u,
			MuiWindowInteractionStateField.CycleChainCount => 4u,
			MuiWindowInteractionStateField.CycleChainRequests => 5u,
			_ => uint.MaxValue,
		};
		if (skips == uint.MaxValue) return false;
		for (var i = 0u; i < skips; i++)
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiWindowInteractionStateRecord.FieldSize, out _)) return false;
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiWindowInteractionStateRecord.FieldSize, out address);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInteractionStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiWindowInteractionStateRecord.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInteractionStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWindowInteractionStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowInteractionStateField.Magic)
			value = state.Magic;
		else if (field == MuiWindowInteractionStateField.SnapshotFlags)
			value = state.SnapshotFlags;
		else if (field == MuiWindowInteractionStateField.SnapshotRequests)
			value = state.SnapshotRequests;
		else if (field == MuiWindowInteractionStateField.CycleChainHead)
			value = state.CycleChainHead.Raw;
		else if (field == MuiWindowInteractionStateField.CycleChainCount)
			value = state.CycleChainCount;
		else if (field == MuiWindowInteractionStateField.CycleChainRequests)
			value = state.CycleChainRequests;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWindowInteractionStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWindowInteractionStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWindowInteractionStateField.Magic)
			state.Magic = value;
		else if (field == MuiWindowInteractionStateField.SnapshotFlags)
			state.SnapshotFlags = value;
		else if (field == MuiWindowInteractionStateField.SnapshotRequests)
			state.SnapshotRequests = value;
		else if (field == MuiWindowInteractionStateField.CycleChainHead)
			state.CycleChainHead = APTR.FromPointer(value);
		else if (field == MuiWindowInteractionStateField.CycleChainCount)
			state.CycleChainCount = value;
		else if (field == MuiWindowInteractionStateField.CycleChainRequests)
			state.CycleChainRequests = value;
		else return false;
		return MuiWindowInteractionStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiWindowInteractionStateRecord.Size -
			MuiWindowInteractionStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiWindowInteractionStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiWindowInteractionStateRecord.FieldSize);
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
	// Sequential named-struct path used by Window snapshot/cycle-chain code.
	// Cookie, counters, and the cycle-chain pointer are exchanged in
	// declaration order; numeric positions remain confined to the compatibility
	// adapter.
	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowInteractionStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SnapshotFlags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SnapshotRequests) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.CycleChainHead.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.CycleChainCount) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.CycleChainRequests) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWindowInteractionStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var snapshotFlags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var snapshotRequests) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var cycleChainHead) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var cycleChainCount) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var cycleChainRequests) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Magic = magic;
		value.SnapshotFlags = snapshotFlags;
		value.SnapshotRequests = snapshotRequests;
		value.CycleChainHead = APTR.FromPointer(cycleChainHead);
		value.CycleChainCount = cycleChainCount;
		value.CycleChainRequests = cycleChainRequests;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiWindowInteractionStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

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
		return WriteRecord(ref platform, address, value);
	}
}
