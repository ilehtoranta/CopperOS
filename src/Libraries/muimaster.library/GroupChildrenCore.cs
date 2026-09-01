/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupForwardState
{
	public const uint Magic = 0x47465744; // "GFWD"
	public const uint Size = 16;
	public const uint FieldSize = 4;
	public const uint CookieOffset = 0;
	public const uint ForwardOffset = 4;
	public const uint ForwardDepthOffset = 8;
	public const uint ForwardCountOffset = 12;
	public uint Cookie;
	public uint Forward;
	public uint ForwardDepth;
	public uint ForwardCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupForwardTraversalState
{
	public const uint Size = 12;
	public uint Remaining;
	public uint Visited;
	public uint Exhausted;
}

internal static class MuiGroupForwardStateValidation
{
	internal static bool IsValidRecord(MuiGroupForwardState value) =>
		value.Cookie == MuiGroupForwardState.Magic && value.Forward <= 1 &&
		value.ForwardDepth <= 1;

	internal static bool IsValidState(MuiGroupForwardState value) =>
		IsValidRecord(value);
}

internal static class MuiGroupForwardTraversalCore
{
	internal static bool TryVisit(ref MuiGroupForwardTraversalState state)
	{
		if (state.Remaining == 0)
		{
			state.Exhausted = 1;
			return false;
		}
		state.Remaining--;
		state.Visited = state.Visited == uint.MaxValue
			? uint.MaxValue : state.Visited + 1;
		return true;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupChildListState
{
	public const uint Magic = 0x47434C53; // "GCLS"
	public const uint Size = 32;
	public const uint FieldSize = 4;
	public const uint CookieOffset = 0;
	public const uint GroupOffset = 4;
	public const uint ListOffset = 8;
	public const uint EntriesOffset = 12;
	public const uint CountOffset = 16;
	public const uint CapacityOffset = 20;
	public const uint MutationOffset = 24;
	public const uint GenerationOffset = 28;
	public uint Cookie;
	public APTR Group;
	public APTR List;
	public APTR Entries;
	public uint Count;
	public uint Capacity;
	public uint Mutation;
	public uint Generation;
}

internal static class MuiGroupChildListStateValidation
{
	internal static bool IsValidRecord(MuiGroupChildListState value) =>
		value.Cookie == MuiGroupChildListState.Magic && value.Group.IsNotNull &&
		value.List.IsNotNull && value.Count <= value.Capacity &&
		value.Count <= MuiHeadlessLayout.MaximumTraversal &&
		value.Capacity <= MuiHeadlessLayout.MaximumTraversal &&
		(value.Count == 0 || value.Entries.IsNotNull) &&
		value.Count <= uint.MaxValue / MuiGroupChildListEntry.Size;

	internal static bool IsValidState<TPlatform>(ref TPlatform platform,
		MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!IsValidRecord(value) || !platform.IsMapped(value.List,
			Amiga.List.Size)) return false;
		if (value.Count == 0) return true;
		return platform.IsMapped(value.Entries,
			value.Count * MuiGroupChildListEntry.Size);
	}
}

internal static class MuiGroupForwardStateCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupForwardState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupForwardState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Cookie) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Forward) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ForwardDepth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ForwardCount) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupForwardState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupForwardState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Cookie) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Forward) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ForwardDepth) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ForwardCount)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupForwardState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return !address.IsNull &&
			MuiGroupForwardStateValidation.IsValidRecord(value) &&
			WriteRecord(ref platform, address, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupForwardState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return TryReadRecord(ref platform, address, out value) &&
			MuiGroupForwardStateValidation.IsValidState(value);
	}
}

internal static class MuiGroupChildListStateCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupChildListState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Cookie) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var group) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var list) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var entries) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Count) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Capacity) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Mutation) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Group = APTR.FromPointer(group);
		value.List = APTR.FromPointer(list);
		value.Entries = APTR.FromPointer(entries);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupChildListState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Cookie) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Group.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.List.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Entries.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Count) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Capacity) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Mutation) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Generation)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return !address.IsNull &&
			MuiGroupChildListStateValidation.IsValidState(ref platform, value) &&
			WriteRecord(ref platform, address, value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadRecord(ref platform, address, out value)) return false;
		return MuiGroupChildListStateValidation.IsValidState(ref platform,
			value);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiGroupForwardRecordInput
{
	public uint Forward;
	public uint ForwardDepth;
	public uint ForwardCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiGroupChildListStateInput
{
	public APTR Group;
	public APTR List;
	public APTR Entries;
	public uint Count;
	public uint Capacity;
	public uint Mutation;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupChildListEntry
{
	public const uint Size = 16;
	public const uint FieldSize = 4;
	public const uint NextOffset = 0;
	public const uint PreviousOffset = 4;
	public const uint ObjectOffset = 8;
	public const uint ReservedOffset = 12;
	public const uint ProjectionMagic = 0x47454E54; // "GENT"
	public APTR Next;
	public APTR Previous;
	public APTR Object;
	public APTR Reserved;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupChildListEntryCursor
{
	internal const uint EntrySize = MuiGroupChildListEntry.Size;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiGroupChildListEntryVectorCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiGroupChildListEntryCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupChildListEntryVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);

	// Complete projection entries cross the vector boundary as named records;
	// slot-address arithmetic remains private to the bounded memory adapter.
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiGroupChildListEntryCursor cursor, out MuiGroupChildListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		return MuiGroupChildListEntryCodec.TryRead(ref platform, address,
			out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiGroupChildListEntryCursor cursor, MuiGroupChildListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		return MuiGroupChildListEntryCodec.Write(ref platform, address, value);
	}
}

// Struct-first guest-memory adapter for the caller-owned Group child-list
// projection vector. Index arithmetic stays at this ABI boundary; consumers
// receive only complete named 16-byte entries.
internal static class MuiGroupChildListEntryVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >
			(uint.MaxValue - vector.Raw) / MuiGroupChildListEntry.Size)
			return false;
		var offset = index * MuiGroupChildListEntry.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiGroupChildListEntry.Size);
	}
}

internal static class MuiGroupChildListEntryCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupChildListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupChildListEntry.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var previous) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var @object) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var reserved) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Next = APTR.FromPointer(next);
		value.Previous = APTR.FromPointer(previous);
		value.Object = APTR.FromPointer(@object);
		value.Reserved = APTR.FromPointer(reserved);
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupChildListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupChildListEntry.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Next.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Previous.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Object.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupChildListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryReadRecord(ref platform, address, out value)) return false;
		return value.Reserved.Raw == MuiGroupChildListEntry.ProjectionMagic;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupChildListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return !address.IsNull && WriteRecord(ref platform, address, value);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupExecListRecord
{
	internal const uint Size = Amiga.List.Size;
	internal const uint LongFieldSize = 4;
	internal const uint ByteFieldSize = 1;
	internal const uint HeadOffset = 0;
	internal const uint TailOffset = 4;
	internal const uint TailPredOffset = 8;
	internal const uint TypeOffset = 12;
	internal const uint PaddingOffset = 13;
	internal APTR Head;
	internal APTR Tail;
	internal APTR TailPred;
	internal NodeType Type;
	internal byte Padding;
}

internal enum MuiGroupRecordKind : byte
{
	Forward,
	ChildList,
	ChildEntry,
	ExecList,
}

internal enum MuiGroupRecordField : byte
{
	Cookie,
	Forward,
	ForwardDepth,
	ForwardCount,
	Group,
	List,
	Entries,
	Count,
	Capacity,
	Mutation,
	Generation,
	Next,
	Previous,
	Object,
	Reserved,
	Head,
	Tail,
	TailPred,
	Type,
	Padding,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupRecordFieldCursor
{
	internal APTR Address;
	internal MuiGroupRecordKind Record;
	internal MuiGroupRecordField Field;
}

internal static class MuiGroupRecordMemoryCodec
{
	private static bool TryResolve(MuiGroupRecordKind record,
		MuiGroupRecordField field, out uint offset, out uint size,
		out uint fieldSize)
	{
		offset = 0;
		size = 0;
		fieldSize = 0;
		if (record == MuiGroupRecordKind.Forward)
		{
			size = MuiGroupForwardState.Size;
			fieldSize = MuiGroupForwardState.FieldSize;
			if (field == MuiGroupRecordField.Cookie)
				offset = MuiGroupForwardState.CookieOffset;
			else if (field == MuiGroupRecordField.Forward)
				offset = MuiGroupForwardState.ForwardOffset;
			else if (field == MuiGroupRecordField.ForwardDepth)
				offset = MuiGroupForwardState.ForwardDepthOffset;
			else if (field == MuiGroupRecordField.ForwardCount)
				offset = MuiGroupForwardState.ForwardCountOffset;
			else return false;
			return true;
		}
		if (record == MuiGroupRecordKind.ChildList)
		{
			size = MuiGroupChildListState.Size;
			fieldSize = MuiGroupChildListState.FieldSize;
			if (field == MuiGroupRecordField.Cookie)
				offset = MuiGroupChildListState.CookieOffset;
			else if (field == MuiGroupRecordField.Group)
				offset = MuiGroupChildListState.GroupOffset;
			else if (field == MuiGroupRecordField.List)
				offset = MuiGroupChildListState.ListOffset;
			else if (field == MuiGroupRecordField.Entries)
				offset = MuiGroupChildListState.EntriesOffset;
			else if (field == MuiGroupRecordField.Count)
				offset = MuiGroupChildListState.CountOffset;
			else if (field == MuiGroupRecordField.Capacity)
				offset = MuiGroupChildListState.CapacityOffset;
			else if (field == MuiGroupRecordField.Mutation)
				offset = MuiGroupChildListState.MutationOffset;
			else if (field == MuiGroupRecordField.Generation)
				offset = MuiGroupChildListState.GenerationOffset;
			else return false;
			return true;
		}
		if (record == MuiGroupRecordKind.ChildEntry)
		{
			size = MuiGroupChildListEntry.Size;
			fieldSize = MuiGroupChildListEntry.FieldSize;
			if (field == MuiGroupRecordField.Next)
				offset = MuiGroupChildListEntry.NextOffset;
			else if (field == MuiGroupRecordField.Previous)
				offset = MuiGroupChildListEntry.PreviousOffset;
			else if (field == MuiGroupRecordField.Object)
				offset = MuiGroupChildListEntry.ObjectOffset;
			else if (field == MuiGroupRecordField.Reserved)
				offset = MuiGroupChildListEntry.ReservedOffset;
			else return false;
			return true;
		}
		if (record == MuiGroupRecordKind.ExecList)
		{
			size = MuiGroupExecListRecord.Size;
			if (field == MuiGroupRecordField.Head)
			{
				offset = MuiGroupExecListRecord.HeadOffset;
				fieldSize = MuiGroupExecListRecord.LongFieldSize;
			}
			else if (field == MuiGroupRecordField.Tail)
			{
				offset = MuiGroupExecListRecord.TailOffset;
				fieldSize = MuiGroupExecListRecord.LongFieldSize;
			}
			else if (field == MuiGroupRecordField.TailPred)
			{
				offset = MuiGroupExecListRecord.TailPredOffset;
				fieldSize = MuiGroupExecListRecord.LongFieldSize;
			}
			else if (field == MuiGroupRecordField.Type)
			{
				offset = MuiGroupExecListRecord.TypeOffset;
				fieldSize = MuiGroupExecListRecord.ByteFieldSize;
			}
			else if (field == MuiGroupRecordField.Padding)
			{
				offset = MuiGroupExecListRecord.PaddingOffset;
				fieldSize = MuiGroupExecListRecord.ByteFieldSize;
			}
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGroupRecordKind recordKind,
		MuiGroupRecordField field, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(recordKind, field, out var offset, out var size,
			out fieldSize) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress, out var fieldSize) || fieldSize != 4) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress, out var fieldSize) || fieldSize != 4) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		out byte value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress, out var fieldSize) || fieldSize != 1) return false;
		value = platform.ReadUInt8(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		byte value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress, out var fieldSize) || fieldSize != 1) return false;
		platform.WriteUInt8(fieldAddress, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// group-record cursor. Live Group child-state/list codecs use the direct
// named-record adapter above.
internal static class MuiGroupRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGroupRecordFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupRecordMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Record, cursor.Field, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupRecordMemoryCodec.TryReadUInt32(ref platform, address, record,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupRecordMemoryCodec.TryWriteUInt32(ref platform, address, record,
			field, value);

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		out byte value) where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupRecordMemoryCodec.TryReadUInt8(ref platform, address, record,
			field, out value);

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupRecordKind record, MuiGroupRecordField field,
		byte value) where TPlatform : struct, IMuiGuestMemory =>
		MuiGroupRecordMemoryCodec.TryWriteUInt8(ref platform, address, record,
			field, value);
}

internal static class MuiGroupExecListCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGroupExecListRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupExecListRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var head) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var tail) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var tailPred) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var type) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out value.Padding) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Head = APTR.FromPointer(head);
		value.Tail = APTR.FromPointer(tail);
		value.TailPred = APTR.FromPointer(tailPred);
		value.Type = (NodeType)type;
		return true;
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGroupExecListRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGroupExecListRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Head.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Tail.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TailPred.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				(byte)value.Type) ||
			!MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Padding)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGroupExecListRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadRecord(ref platform, address, out value)) return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGroupExecListRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return !address.IsNull && WriteRecord(ref platform, address, value);
	}
}

// MorphOS Group child ownership, child-count publication, and SetAttrs
// forwarding. The object topology remains in the existing Family records;
// this boundary only adds the Group-specific ABI semantics around that typed
// topology. Forwarding is deliberately bounded by the same traversal ceiling
// as Family, Notify, and layout walks.
public static class MuiGroupChildrenCore
{
	public const uint Child = 0x804226E6;
	public const uint FamilyChild = 0x8042C696;
	public const uint ChildCount = 0x80420322;
	public const uint ChildList = 0x80424748;
	public const uint FamilyChildCount = 0x8042B25A;
	public const uint FamilyList = 0x80424B9E;
	public const uint Forward = 0x80421422;
	public const uint ForwardDepth = 0x80428488;

	private const uint StateAttribute = 0x7FFE0042;
	private const uint ChildListStateAttribute = 0x7FFE0043;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == ChildCount || attribute == ChildList;

	internal static bool IsFamilyPublicGetterAttribute(uint attribute) =>
		attribute == FamilyChildCount || attribute == FamilyList;

	internal static bool TryGetFamily<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = false;
		if (!IsFamilyPublicGetterAttribute(attribute) ||
			!MuiFamilyCore.IsFamilyObject(ref platform, state, obj)) return false;
		handled = true;
		if (attribute == FamilyChildCount)
		{
			value = CountChildren(ref platform, state, obj);
			return true;
		}
		var list = EnsureChildList(ref platform, state, obj);
		value = list.Raw;
		return list.IsNotNull;
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var obj = objectValue.Boopsi;
		if (obj.IsNull) return false;

		if (attribute == FamilyChild)
		{
			handled = true;
			if (MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record) ||
				!MuiFamilyCore.IsFamilyObject(ref platform, state, obj)) return false;
			var child = APTR.FromPointer(value);
			return child.IsNotNull && MuiFamilyCore.AddTail(ref platform, state,
				obj, child);
		}
		if (attribute == Child && MuiFamilyCore.IsFamilyObject(ref platform,
			state, obj) && !MuiGroupChangeCore.IsGroupObject(ref platform, state,
			obj))
		{
			// MorphOS documents MUIA_Group_Child as a Family_Child alias. Keep
			// the alias initialize-only for non-Group Family classes; the existing
			// Group path below retains its established behavior.
			handled = true;
			if (MuiHeadlessObjectCore.IsObjectInitialized(ref platform, record))
				return false;
			var child = APTR.FromPointer(value);
			return child.IsNotNull && MuiFamilyCore.AddTail(ref platform, state,
				obj, child);
		}
		if (!MuiGroupChangeCore.IsGroupObject(ref platform, state, obj)) return false;

		if (attribute == Child)
		{
			handled = true;
			var child = APTR.FromPointer(value);
			if (child.IsNull) return false;
			return MuiFamilyCore.AddTail(ref platform, state, obj, child);
		}

		// MUIA_Group_ChildList is a read-only [..G] projection.  A caller must
		// use OM_ADDMEMBER/OM_REMMEMBER (the Family seam), never replace the
		// returned list pointer through SetAttrs.
		if (attribute == ChildList)
		{
			handled = true;
			return false;
		}

		if (attribute == Forward || attribute == ForwardDepth)
		{
			handled = true;
			return SetForwardState(ref platform, state, record, attribute,
				value, notify);
		}

		if (!TryReadForwardState(ref platform, record, out var forward))
		{
			if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				StateAttribute, out var stateBlock) && stateBlock != 0)
			{
				handled = true;
			}
			return false;
		}
		if (forward.Forward == 0) return false;
		handled = true;
		var traversal = default(MuiGroupForwardTraversalState);
		if (forward.ForwardDepth != 0)
			traversal.Remaining = MuiHeadlessLayout.MaximumTraversal;
		return ForwardAttribute(ref platform, state, obj, attribute, value,
			notify, forward.ForwardDepth != 0, ref traversal);
	}

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = false;
		if (!MuiGroupChangeCore.IsGroupObject(ref platform, state, obj))
			return false;
		if (attribute == ChildCount)
		{
			handled = true;
			value = CountChildren(ref platform, state, obj);
			return true;
		}
		if (attribute == ChildList)
		{
			handled = true;
			var list = EnsureChildList(ref platform, state, obj);
			value = list.Raw;
			return list.IsNotNull;
		}
		if (attribute != Forward && attribute != ForwardDepth) return false;
		handled = true;
		if (TryReadForwardState(ref platform,
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj),
			out var forward))
		{
			value = attribute == Forward ? forward.Forward : forward.ForwardDepth;
			return true;
		}
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			StateAttribute, out var stateBlock) && stateBlock != 0) return false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out var raw)) return false;
		// Compatibility writes can predate the typed forwarding record. Keep the
		// public BOOL projection canonical even in that raw-only state.
		value = raw == 0 ? 0u : 1u;
		return true;
	}

	// Struct-first native qualification seam for the forward state record.
	public static bool WriteForwardRecord<TPlatform>(ref TPlatform platform,
		APTR storage, uint forward, uint depth, uint requests)
		where TPlatform : struct, IMuiGuestMemory
	{
		var input = default(MuiGroupForwardRecordInput);
		input.Forward = forward;
		input.ForwardDepth = depth;
		input.ForwardCount = requests;
		return WriteForwardRecord(ref platform, storage, input);
	}

	public static bool WriteForwardRecord<TPlatform>(ref TPlatform platform,
		APTR storage, MuiGroupForwardRecordInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiGroupForwardState);
		value.Cookie = MuiGroupForwardState.Magic;
		value.Forward = input.Forward;
		value.ForwardDepth = input.ForwardDepth;
		value.ForwardCount = input.ForwardCount;
		return MuiGroupForwardStateCodec.Write(ref platform, storage, value);
	}

	public static uint DispatchForwardRecord<TPlatform>(ref TPlatform platform,
		APTR storage) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGroupForwardStateCodec.TryRead(ref platform, storage,
			out var value)) return 0;
		return value.Forward ^ value.ForwardDepth ^ value.ForwardCount;
	}

	public static bool WriteChildListStateRecord<TPlatform>(
		ref TPlatform platform, APTR storage, MuiGroupChildListStateInput input)
		where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiGroupChildListState);
		value.Cookie = MuiGroupChildListState.Magic;
		value.Group = input.Group;
		value.List = input.List;
		value.Entries = input.Entries;
		value.Count = input.Count;
		value.Capacity = input.Capacity;
		value.Mutation = input.Mutation;
		value.Generation = input.Generation;
		return MuiGroupChildListStateCodec.Write(ref platform, storage, value);
	}

	public static uint DispatchChildListStateRecord<TPlatform>(
		ref TPlatform platform, APTR storage) where TPlatform : struct,
		IMuiGuestMemory
	{
		if (!MuiGroupChildListStateCodec.TryRead(ref platform, storage,
			out var value)) return 0;
		return value.Group.Raw ^ value.List.Raw ^ value.Entries.Raw ^
			value.Count ^ value.Capacity ^ value.Mutation ^ value.Generation;
	}

	// Struct-first native qualification seam for the read-only List header and
	// its two-entry projection. Production callers obtain this view from
	// MUIA_Group_ChildList; the fixed storage form keeps the freestanding ABI
	// test independent of the headless object's broader dispatcher closure.
	public static bool WriteChildListRecord<TPlatform>(ref TPlatform platform,
		APTR listStorage, APTR entriesStorage, APTR first, APTR second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (listStorage.IsNull || entriesStorage.IsNull || !platform.IsMapped(
			listStorage, Amiga.List.Size) || !platform.IsMapped(entriesStorage,
			MuiGroupChildListEntry.Size * 2)) return false;
		var cursor = default(MuiGroupChildListEntryCursor);
		cursor.Base = entriesStorage;
		if (!MuiGroupChildListEntryVectorCodec.TryGetEntry(ref platform, cursor,
			out var firstEntry)) return false;
		cursor.Index = 1;
		if (!MuiGroupChildListEntryVectorCodec.TryGetEntry(ref platform, cursor,
			out var secondEntry)) return false;
		WriteEntry(ref platform, firstEntry, secondEntry, APTR.Null, first);
		WriteEntry(ref platform, secondEntry, APTR.Null, firstEntry, second);
		var list = default(Amiga.List);
		list.Head = firstEntry;
		list.Tail = APTR.Null;
		list.TailPred = secondEntry;
		list.Type = NodeType.Unknown;
		return WriteList(ref platform, listStorage, list);
	}

	internal static void Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		CleanupChildList(ref platform, state, obj);
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !TryReadForwardState(ref platform, record,
			out _)) return;
		var block = APTR.FromPointer(ReadPrivateAttribute(ref platform, record));
		if (block.IsNull || !platform.IsMapped(block,
			MuiGroupForwardState.Size)) return;
		platform.Clear(block, MuiGroupForwardState.Size);
		platform.Free(block, MuiGroupForwardState.Size);
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			StateAttribute, 0, false);
	}

	// Read-only NextObject-compatible traversal for the typed projection.  The
	// public MorphOS contract still routes through intuition.library/NextObject;
	// this helper is the local ABI seam used until CopperStart's intuition
	// vector is wired to understand the same guest object representation. The
	// cursor is initialized from Exec List.Head and is advanced to the next
	// projected entry, with no managed enumerator or object allocation.
	public static APTR NextObject<TPlatform>(ref TPlatform platform, APTR list,
		ref uint cursorRaw) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = APTR.FromPointer(cursorRaw);
		if (list.IsNull || !platform.IsMapped(list, Amiga.List.Size) ||
			cursor.IsNull || !platform.IsMapped(cursor,
			MuiGroupChildListEntry.Size)) return APTR.Null;
		if (!MuiGroupChildListEntryCodec.TryRead(ref platform, cursor,
			out var entry)) return APTR.Null;
		var obj = entry.Object;
		cursorRaw = entry.Next.Raw;
		return obj;
	}

	private static bool SetForwardState<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = EnsureForwardState(ref platform, state, record);
		if (block.IsNull || !TryReadState(ref platform, block, out var current))
			return false;
		var previous = current;
		if (attribute == Forward) current.Forward = value == 0 ? 0u : 1u;
		else current.ForwardDepth = value == 0 ? 0u : 1u;
		current.ForwardCount = current.ForwardCount == uint.MaxValue
			? uint.MaxValue : current.ForwardCount + 1;
		if (!WriteState(ref platform, block, current)) return false;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, attribute, value == 0 ? 0u : 1u, false))
		{
			WriteState(ref platform, block, previous);
			return false;
		}
		MuiHeadlessMemory.Mutated(ref platform, state);
		if (notify) MuiNotifyCore.DispatchAttributeChange(ref platform, state,
			record, attribute, value == 0 ? 0u : 1u);
		return true;
	}

	private static bool ForwardAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR group, uint attribute, uint value, bool notify,
		bool recursive, ref MuiGroupForwardTraversalState traversal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = CountChildren(ref platform, state, group);
		var success = true;
		for (var index = 0u; index < count; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, group,
				unchecked((int)index), APTR.Null);
			if (recursive && !MuiGroupForwardTraversalCore.TryVisit(
				ref traversal)) break;
			if (child.IsNull || !MuiHeadlessObjectCore.SetAttribute(ref platform,
				state, child, attribute, value, notify)) success = false;
			if (recursive && child.IsNotNull &&
				MuiGroupChangeCore.IsGroupObject(ref platform, state, child) &&
				!ForwardAttribute(ref platform, state, child, attribute, value,
					notify, true, ref traversal)) success = false;
		}
		if (TryReadForwardState(ref platform,
			MuiHeadlessObjectCore.FindObject(ref platform, state, group),
			out var current))
		{
			current.ForwardCount = current.ForwardCount == uint.MaxValue
				? uint.MaxValue : current.ForwardCount + 1;
			var record = MuiHeadlessObjectCore.FindObject(ref platform, state,
				group);
			var block = APTR.FromPointer(ReadPrivateAttribute(ref platform,
				record));
			if (TryReadState(ref platform, block, out _) &&
				!WriteState(ref platform, block, current)) success = false;
		}
		return success;
	}

	private static uint CountChildren<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = 0u;
		while (count < MuiHeadlessLayout.MaximumTraversal &&
			MuiFamilyCore.GetChild(ref platform, state, group,
				unchecked((int)count), APTR.Null).IsNotNull) count++;
		return count;
	}

	private static APTR EnsureChildList<TPlatform>(ref TPlatform platform,
		APTR state, APTR group) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, group);
		if (record.IsNull) return APTR.Null;
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var mutation = stateValue.Mutation;
		var hasExisting = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, group, ChildListStateAttribute, out var existing) &&
			existing != 0;
		var block = APTR.FromPointer(existing);
		if (hasExisting)
		{
			if (!TryReadChildListState(ref platform, block, out var current))
				return APTR.Null;
			if (current.Group.Raw == group.Raw && current.Mutation == mutation &&
				current.List.IsNotNull && platform.IsMapped(current.List,
					Amiga.List.Size)) return current.List;
		}

		var count = CountChildren(ref platform, state, group);
		if (count > uint.MaxValue / MuiGroupChildListEntry.Size) return APTR.Null;
		var list = MuiHeadlessMemory.Allocate(ref platform, Amiga.List.Size);
		if (list.IsNull) return APTR.Null;
		var entriesSize = count * MuiGroupChildListEntry.Size;
		var entries = entriesSize == 0 ? APTR.Null :
			MuiHeadlessMemory.Allocate(ref platform, entriesSize);
		if (entriesSize != 0 && entries.IsNull)
		{
			platform.Clear(list, Amiga.List.Size);
			platform.Free(list, Amiga.List.Size);
			return APTR.Null;
		}

		var previous = APTR.Null;
		for (var index = 0u; index < count; index++)
		{
			var cursor = default(MuiGroupChildListEntryCursor);
			cursor.Base = entries;
			cursor.Index = index;
			if (!MuiGroupChildListEntryVectorCodec.TryGetEntry(ref platform, cursor,
				out var entry))
			{
				FreeChildListProjection(ref platform, list, entries, entriesSize);
				return APTR.Null;
			}
			var child = MuiFamilyCore.GetChild(ref platform, state, group,
				unchecked((int)index), APTR.Null);
			var next = APTR.Null;
			if (index + 1 < count)
			{
				cursor.Index++;
				if (!MuiGroupChildListEntryVectorCodec.TryGetEntry(ref platform,
					cursor, out next))
				{
					FreeChildListProjection(ref platform, list, entries,
						entriesSize);
					return APTR.Null;
				}
			}
			WriteEntry(ref platform, entry, next, previous, child);
			previous = entry;
		}
		var listValue = default(Amiga.List);
		listValue.Head = count == 0 ? APTR.Null : entries;
		listValue.Tail = APTR.Null;
		listValue.TailPred = count == 0 ? APTR.Null : previous;
		listValue.Type = NodeType.Unknown;
		listValue.Padding = 0;
		if (!WriteList(ref platform, list, listValue))
		{
			FreeChildListProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}

		var replacement = default(MuiGroupChildListState);
		replacement.Cookie = MuiGroupChildListState.Magic;
		replacement.Group = group;
		replacement.List = list;
		replacement.Entries = entries;
		replacement.Count = count;
		replacement.Capacity = count;
		replacement.Mutation = mutation;
		replacement.Generation = MuiHeadlessMemory.NextSequence(ref platform,
			state);
		var oldBlock = block;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiGroupChildListState.Size);
		if (block.IsNull)
		{
			if (block.IsNotNull)
			{
				platform.Clear(block, MuiGroupChildListState.Size);
				platform.Free(block, MuiGroupChildListState.Size);
			}
			FreeChildListProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, ChildListStateAttribute, block.Raw, false))
		{
			platform.Clear(block, MuiGroupChildListState.Size);
			platform.Free(block, MuiGroupChildListState.Size);
			FreeChildListProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		if (!MuiHeadlessStateCodec.TryRead(ref platform, state,
			out stateValue))
		{
			MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
				ChildListStateAttribute, oldBlock.Raw, false);
			platform.Clear(block, MuiGroupChildListState.Size);
			platform.Free(block, MuiGroupChildListState.Size);
			FreeChildListProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		replacement.Mutation = stateValue.Mutation;
		if (!WriteChildListState(ref platform, block, replacement))
		{
			MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
				ChildListStateAttribute, oldBlock.Raw, false);
			platform.Clear(block, MuiGroupChildListState.Size);
			platform.Free(block, MuiGroupChildListState.Size);
			FreeChildListProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		FreeChildListStateBlock(ref platform, oldBlock);
		return list;
	}

	private static void CleanupChildList<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return;
		var block = APTR.FromPointer(ReadPrivateAttribute(ref platform, record,
			ChildListStateAttribute));
		FreeChildListStateBlock(ref platform, block);
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			ChildListStateAttribute, 0, false);
	}

	private static void FreeChildListStateBlock<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadChildListState(ref platform, block, out var value)) return;
		var entriesSize = value.Count > uint.MaxValue /
			MuiGroupChildListEntry.Size ? 0u : value.Count *
			MuiGroupChildListEntry.Size;
		FreeChildListProjection(ref platform, value.List, value.Entries,
			entriesSize);
		platform.Clear(block, MuiGroupChildListState.Size);
		platform.Free(block, MuiGroupChildListState.Size);
	}

	private static void FreeChildListProjection<TPlatform>(ref TPlatform platform,
		APTR list, APTR entries, uint entriesSize)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (entries.IsNotNull && entriesSize != 0 && platform.IsMapped(entries,
			entriesSize))
		{
			platform.Clear(entries, entriesSize);
			platform.Free(entries, entriesSize);
		}
		if (list.IsNotNull && platform.IsMapped(list, Amiga.List.Size))
		{
			platform.Clear(list, Amiga.List.Size);
			platform.Free(list, Amiga.List.Size);
		}
	}

	private static void WriteEntry<TPlatform>(ref TPlatform platform, APTR entry,
		APTR next, APTR previous, APTR obj) where TPlatform : struct, IMuiGuestMemory
	{
		var value = default(MuiGroupChildListEntry);
		value.Next = next;
		value.Previous = previous;
		value.Object = obj;
		value.Reserved = APTR.FromPointer(MuiGroupChildListEntry.ProjectionMagic);
		MuiGroupChildListEntryCodec.Write(ref platform, entry, value);
	}

	private static bool WriteList<TPlatform>(ref TPlatform platform, APTR address,
		Amiga.List value) where TPlatform : struct, IMuiGuestMemory
	{
		var record = default(MuiGroupExecListRecord);
		record.Head = value.Head;
		record.Tail = value.Tail;
		record.TailPred = value.TailPred;
		record.Type = value.Type;
		record.Padding = value.Padding;
		return MuiGroupExecListCodec.Write(ref platform, address, record);
	}

	private static bool TryReadChildListState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupChildListStateCodec.TryRead(ref platform, block, out value);

	private static bool WriteChildListState<TPlatform>(ref TPlatform platform,
		APTR block, MuiGroupChildListState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupChildListStateCodec.Write(ref platform, block, value);

	private static APTR EnsureForwardState<TPlatform>(ref TPlatform platform,
		APTR state, APTR record) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return APTR.Null;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			objectValue.Boopsi, StateAttribute, out var existing) && existing != 0)
		{
			var existingBlock = APTR.FromPointer(existing);
			if (TryReadState(ref platform, existingBlock, out _))
				return existingBlock;
			return APTR.Null;
		}
		var block = APTR.Null;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiGroupForwardState.Size);
		if (block.IsNull) return APTR.Null;
		var value = default(MuiGroupForwardState);
		value.Cookie = MuiGroupForwardState.Magic;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			objectValue.Boopsi, Forward, out var forward))
			value.Forward = forward == 0 ? 0u : 1u;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			objectValue.Boopsi, ForwardDepth, out var forwardDepth))
			value.ForwardDepth = forwardDepth == 0 ? 0u : 1u;
		if (!WriteState(ref platform, block, value))
		{
			platform.Clear(block, MuiGroupForwardState.Size);
			platform.Free(block, MuiGroupForwardState.Size);
			return APTR.Null;
		}
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, StateAttribute, block.Raw, false))
		{
			platform.Clear(block, MuiGroupForwardState.Size);
			platform.Free(block, MuiGroupForwardState.Size);
			return APTR.Null;
		}
		WriteState(ref platform, block, value);
		return block;
	}

	private static uint ReadPrivateAttribute<TPlatform>(ref TPlatform platform,
		APTR record, uint attribute = StateAttribute)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return 0;
		var item = objectValue.Attributes;
		var visited = 0u;
		while (item.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessAttributeCodec.TryRead(ref platform, item,
				out var attributeValue)) return 0;
			if (attributeValue.Id == attribute) return attributeValue.Value;
			item = attributeValue.Next;
		}
		return 0;
	}

	private static bool TryReadForwardState<TPlatform>(ref TPlatform platform,
		APTR record, out MuiGroupForwardState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		var block = APTR.FromPointer(ReadPrivateAttribute(ref platform, record));
		return TryReadState(ref platform, block, out value);
	}

	private static bool WriteState<TPlatform>(ref TPlatform platform, APTR block,
		MuiGroupForwardState value) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupForwardStateCodec.Write(ref platform, block, value);

	private static bool TryReadState<TPlatform>(ref TPlatform platform, APTR block,
		out MuiGroupForwardState value) where TPlatform : struct, IMuiGuestMemory
		=> MuiGroupForwardStateCodec.TryRead(ref platform, block, out value);
}
