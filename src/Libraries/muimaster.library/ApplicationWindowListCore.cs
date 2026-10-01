/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The public MUIA_Application_WindowList value is an Exec List pointer. The
// list is a read-only guest projection over the application's owned Window
// children; application and child links remain in the existing typed Family
// records. The projection is rebuilt after a guest topology mutation. ABI
// field offsets are confined to the small codecs below; list logic uses the
// named state and entry structs.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowListState
{
	internal const uint Magic = 0x41574C53; // "AWLS"
	internal const uint Size = 32;
	internal const uint CookieOffset = 0;
	internal const uint ApplicationOffset = 4;
	internal const uint ListOffset = 8;
	internal const uint EntriesOffset = 12;
	internal const uint CountOffset = 16;
	internal const uint CapacityOffset = 20;
	internal const uint MutationOffset = 24;
	internal const uint GenerationOffset = 28;
	internal const uint FieldSize = 4;
	internal uint Cookie;
	internal APTR Application;
	internal APTR List;
	internal APTR Entries;
	internal uint Count;
	internal uint Capacity;
	internal uint Mutation;
	internal uint Generation;
}

internal enum MuiApplicationWindowListStateField : byte
{
	Cookie,
	Application,
	List,
	Entries,
	Count,
	Capacity,
	Mutation,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowListStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationWindowListStateField Field;
}

internal static class MuiApplicationWindowListStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowListStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationWindowListStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationWindowListStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationWindowListStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Struct-first guest-memory adapter for the fixed application window-list
// state record. The bounded cursor walks the complete packed struct before
// selecting a field; offset constants remain ABI documentation only.
internal static class MuiApplicationWindowListStateRecordMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiApplicationWindowListStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiApplicationWindowListStateField.Cookie:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out address);
			case MuiApplicationWindowListStateField.Application:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out address);
			case MuiApplicationWindowListStateField.List:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out address);
			case MuiApplicationWindowListStateField.Entries:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out address);
			case MuiApplicationWindowListStateField.Count:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out address);
			case MuiApplicationWindowListStateField.Capacity:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out address);
			case MuiApplicationWindowListStateField.Mutation:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out address);
			case MuiApplicationWindowListStateField.Generation:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListState.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListState.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiApplicationWindowListState.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationWindowListStateCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		switch (field)
		{
			case MuiApplicationWindowListStateField.Cookie:
				value = state.Cookie; return true;
			case MuiApplicationWindowListStateField.Application:
				value = state.Application.Raw; return true;
			case MuiApplicationWindowListStateField.List:
				value = state.List.Raw; return true;
			case MuiApplicationWindowListStateField.Entries:
				value = state.Entries.Raw; return true;
			case MuiApplicationWindowListStateField.Count:
				value = state.Count; return true;
			case MuiApplicationWindowListStateField.Capacity:
				value = state.Capacity; return true;
			case MuiApplicationWindowListStateField.Mutation:
				value = state.Mutation; return true;
			case MuiApplicationWindowListStateField.Generation:
				value = state.Generation; return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationWindowListStateCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		switch (field)
		{
			case MuiApplicationWindowListStateField.Cookie:
				state.Cookie = value; break;
			case MuiApplicationWindowListStateField.Application:
				state.Application = APTR.FromPointer(value); break;
			case MuiApplicationWindowListStateField.List:
				state.List = APTR.FromPointer(value); break;
			case MuiApplicationWindowListStateField.Entries:
				state.Entries = APTR.FromPointer(value); break;
			case MuiApplicationWindowListStateField.Count:
				state.Count = value; break;
			case MuiApplicationWindowListStateField.Capacity:
				state.Capacity = value; break;
			case MuiApplicationWindowListStateField.Mutation:
				state.Mutation = value; break;
			case MuiApplicationWindowListStateField.Generation:
				state.Generation = value; break;
			default:
				return false;
		}
		return MuiApplicationWindowListStateCodec.WriteStructural(ref platform,
			record, state);
	}
}

internal static class MuiApplicationWindowListStateCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationWindowListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.Capacity < value.Count ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiApplicationWindowListState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				MuiApplicationWindowListState.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Application.Raw) ||
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

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationWindowListState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationWindowListState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var cookie) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var application) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var list) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var entries) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var count) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var capacity) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var mutation) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var generation) ||
			!MuiGuestStructCursor.IsComplete(cursor) ||
			cookie != MuiApplicationWindowListState.Magic ||
			capacity < count) return false;
		value.Cookie = cookie;
		value.Application = APTR.FromPointer(application);
		value.List = APTR.FromPointer(list);
		value.Entries = APTR.FromPointer(entries);
		value.Count = count;
		value.Capacity = capacity;
		value.Mutation = mutation;
		value.Generation = generation;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationWindowListState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationWindowListState value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowListEntry
{
	internal const uint Size = 16;
	internal const uint ProjectionMagic = 0x4157454E; // "AWEN"
	internal const uint NextOffset = 0;
	internal const uint PreviousOffset = 4;
	internal const uint ObjectOffset = 8;
	internal const uint ReservedOffset = 12;
	internal const uint FieldSize = 4;
	internal APTR Next;
	internal APTR Previous;
	internal APTR Object;
	internal APTR Reserved;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowListEntryCursor
{
	internal const uint EntrySize = MuiApplicationWindowListEntry.Size;
	internal const uint MaximumEntries = MuiHeadlessLayout.MaximumTraversal;
	internal APTR Base;
	internal uint Index;
}

internal enum MuiApplicationWindowListEntryField : byte
{
	Next,
	Previous,
	Object,
	Reserved,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationWindowListEntryFieldCursor
{
	internal APTR Record;
	internal MuiApplicationWindowListEntryField Field;
}

internal static class MuiApplicationWindowListEntryFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowListEntryFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationWindowListEntryRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListEntryField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationWindowListEntryRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListEntryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationWindowListEntryRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Struct-first guest-memory adapter for one Exec-style window-list entry.
// Pointer members stay APTR in the semantic record; this adapter validates
// the complete fixed entry before exposing a named member address.
internal static class MuiApplicationWindowListEntryRecordMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiApplicationWindowListEntryField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiApplicationWindowListEntryField.Next:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListEntry.FieldSize, out address);
			case MuiApplicationWindowListEntryField.Previous:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListEntry.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListEntry.FieldSize, out address);
			case MuiApplicationWindowListEntryField.Object:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListEntry.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListEntry.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListEntry.FieldSize, out address);
			case MuiApplicationWindowListEntryField.Reserved:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListEntry.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListEntry.FieldSize, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiApplicationWindowListEntry.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiApplicationWindowListEntry.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListEntryField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiApplicationWindowListEntry.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListEntryField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationWindowListEntryCodec.TryReadStructural(ref platform,
			record, out var entry)) return false;
		switch (field)
		{
			case MuiApplicationWindowListEntryField.Next:
				value = entry.Next.Raw; return true;
			case MuiApplicationWindowListEntryField.Previous:
				value = entry.Previous.Raw; return true;
			case MuiApplicationWindowListEntryField.Object:
				value = entry.Object.Raw; return true;
			case MuiApplicationWindowListEntryField.Reserved:
				value = entry.Reserved.Raw; return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationWindowListEntryField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationWindowListEntryCodec.TryReadStructural(ref platform,
			record, out var entry)) return false;
		switch (field)
		{
			case MuiApplicationWindowListEntryField.Next:
				entry.Next = APTR.FromPointer(value); break;
			case MuiApplicationWindowListEntryField.Previous:
				entry.Previous = APTR.FromPointer(value); break;
			case MuiApplicationWindowListEntryField.Object:
				entry.Object = APTR.FromPointer(value); break;
			case MuiApplicationWindowListEntryField.Reserved:
				entry.Reserved = APTR.FromPointer(value); break;
			default:
				return false;
		}
		return MuiApplicationWindowListEntryCodec.WriteStructural(ref platform,
			record, entry);
	}
}

internal static class MuiApplicationWindowListEntryVectorCodec
{
	internal static bool TryAdvance(ref MuiApplicationWindowListEntryCursor cursor,
		uint items)
	{
		if (items == 0 || cursor.Index > uint.MaxValue - items)
			return false;
		var next = cursor.Index + items;
		if (next > MuiApplicationWindowListEntryCursor.MaximumEntries)
			return false;
		cursor.Index = next;
		return true;
	}

	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationWindowListEntryVectorMemoryCodec.TryGetEntry(ref platform,
			vector, index, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out MuiApplicationWindowListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryGetEntry(ref platform, vector, index, out var address))
			return false;
		return MuiApplicationWindowListEntryCodec.TryRead(ref platform, address,
			out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, MuiApplicationWindowListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetEntry(ref platform, vector, index, out var address))
			return false;
		return MuiApplicationWindowListEntryCodec.Write(ref platform, address,
			value);
	}

	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowListEntryCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiApplicationWindowListEntryVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowListEntryCursor cursor,
		out MuiApplicationWindowListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		return MuiApplicationWindowListEntryCodec.TryRead(ref platform, address,
			out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiApplicationWindowListEntryCursor cursor,
		MuiApplicationWindowListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		return MuiApplicationWindowListEntryCodec.Write(ref platform, address,
			value);
	}
}

// Struct-first guest-memory adapter for the caller-owned WindowList entry
// vector. Index arithmetic is confined here; consumers receive only complete
// named 16-byte list-entry records.
internal static class MuiApplicationWindowListEntryVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >= MuiApplicationWindowListEntryCursor.MaximumEntries || index >
			(uint.MaxValue - vector.Raw) / MuiApplicationWindowListEntry.Size)
			return false;
		var offset = index * MuiApplicationWindowListEntry.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address,
			MuiApplicationWindowListEntry.Size);
	}
}

internal static class MuiApplicationWindowListEntryCodec
{
	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationWindowListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationWindowListEntry.Size, out var cursor) ||
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
		out MuiApplicationWindowListEntry value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationWindowListEntry.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var next) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var previous) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var obj) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var reserved) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Next = APTR.FromPointer(next);
		value.Previous = APTR.FromPointer(previous);
		value.Object = APTR.FromPointer(obj);
		value.Reserved = APTR.FromPointer(reserved);
		return value.Reserved.Raw == MuiApplicationWindowListEntry.ProjectionMagic;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiApplicationWindowListEntry value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationWindowListEntry value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);
}

public static class MuiApplicationWindowListCore
{
	public const uint WindowList = 0x80429ABE;
	private const uint WindowOwner = 0x7FFE0010;
	private const uint StateAttribute = 0x7FFE0045;

	// The returned Exec List is a named, read-only projection. Generic OM_GET
	// uses this predicate to admit the getter even though Application.mui is not
	// a common-control class.
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == WindowList;

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = IsPublicGetterAttribute(attribute);
		// The returned Exec List is strictly read-only. Family mutations are the
		// only supported way to change application windows.
		return !handled;
	}

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR application, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = IsPublicGetterAttribute(attribute);
		if (!handled) return false;
		var list = Ensure(ref platform, state, application);
		value = list.Raw;
		// A live application with no windows still has a valid, empty list
		// projection.  Keep the attribute handled even when its value is Null.
		return true;
	}

	// Read-only NextObject-compatible traversal of the public list projection.
	// The caller supplies the current guest node, normally initialized from
	// Amiga.List.Head; no managed enumerator or object graph is created.
	public static APTR NextObject<TPlatform>(ref TPlatform platform, APTR list,
		ref uint cursorRaw) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = APTR.FromPointer(cursorRaw);
		if (list.IsNull || !platform.IsMapped(list, Amiga.List.Size) ||
			cursor.IsNull || !platform.IsMapped(cursor,
			MuiApplicationWindowListEntry.Size)) return APTR.Null;
		if (!MuiApplicationWindowListEntryCodec.TryRead(ref platform, cursor,
			out var entry)) return APTR.Null;
		cursorRaw = entry.Next.Raw;
		return entry.Object;
	}

	internal static void Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state,
			application);
		if (record.IsNull || !MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, application, StateAttribute, out var raw)) return;
		FreeStateBlock(ref platform, APTR.FromPointer(raw));
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			StateAttribute, 0, false);
	}

	private static APTR Ensure<TPlatform>(ref TPlatform platform, APTR state,
		APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state,
			application);
		if (record.IsNull || !MuiHeadlessStateCodec.TryRead(ref platform, state,
			out var stateValue)) return APTR.Null;
		var mutation = stateValue.Mutation;
		var oldBlock = APTR.Null;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			application, StateAttribute, out var oldRaw)) oldBlock = APTR.FromPointer(oldRaw);
		if (MuiApplicationWindowListStateCodec.TryRead(ref platform, oldBlock,
			out var current) && current.Application.Raw == application.Raw &&
			current.Mutation == mutation && current.List.IsNotNull &&
			platform.IsMapped(current.List, Amiga.List.Size)) return current.List;

		var count = CountWindows(ref platform, state, application);
		if (count > uint.MaxValue / MuiApplicationWindowListEntry.Size)
			return APTR.Null;
		var list = MuiHeadlessMemory.Allocate(ref platform, Amiga.List.Size);
		if (list.IsNull) return APTR.Null;
		var entriesSize = count * MuiApplicationWindowListEntry.Size;
		var entries = entriesSize == 0 ? APTR.Null :
			MuiHeadlessMemory.Allocate(ref platform, entriesSize);
		if (entriesSize != 0 && entries.IsNull)
		{
			FreeProjection(ref platform, list, APTR.Null, 0);
			return APTR.Null;
		}

		var childIndex = 0u;
		var selected = 0u;
		var previous = APTR.Null;
		while (childIndex < MuiHeadlessLayout.MaximumTraversal &&
			selected < count)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, application,
				unchecked((int)childIndex++), APTR.Null);
			if (child.IsNull) break;
			if (!IsOwnedWindow(ref platform, state, application, child)) continue;
			var cursor = default(MuiApplicationWindowListEntryCursor);
			cursor.Base = entries;
			cursor.Index = selected;
			if (!MuiApplicationWindowListEntryVectorCodec.TryGetEntry(
				ref platform, cursor, out var entry))
			{
				FreeProjection(ref platform, list, entries, entriesSize);
				return APTR.Null;
			}
			var next = APTR.Null;
			if (selected + 1 < count)
			{
				var nextCursor = cursor;
				if (!MuiApplicationWindowListEntryVectorCodec.TryAdvance(
					ref nextCursor, 1) ||
					!MuiApplicationWindowListEntryVectorCodec.TryGetEntry(
						ref platform, nextCursor, out next))
				{
					FreeProjection(ref platform, list, entries, entriesSize);
					return APTR.Null;
				}
			}
			var entryValue = default(MuiApplicationWindowListEntry);
			entryValue.Next = next;
			entryValue.Previous = previous;
			entryValue.Object = child;
			entryValue.Reserved = APTR.FromPointer(
				MuiApplicationWindowListEntry.ProjectionMagic);
			if (!MuiApplicationWindowListEntryVectorCodec.TryWrite(ref platform,
				cursor, entryValue))
			{
				FreeProjection(ref platform, list, entries, entriesSize);
				return APTR.Null;
			}
			previous = entry;
			selected++;
		}
		if (selected != count)
		{
			FreeProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		var listValue = default(Amiga.List);
		listValue.Head = count == 0 ? APTR.Null : entries;
		listValue.Tail = APTR.Null;
		listValue.TailPred = count == 0 ? APTR.Null : previous;
		listValue.Type = NodeType.Unknown;
		var listRecord = default(MuiGroupExecListRecord);
		listRecord.Head = listValue.Head;
		listRecord.Tail = listValue.Tail;
		listRecord.TailPred = listValue.TailPred;
		listRecord.Type = listValue.Type;
		listRecord.Padding = 0;
		if (!MuiGroupExecListCodec.Write(ref platform, list, listRecord))
		{
			FreeProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}

		var replacement = default(MuiApplicationWindowListState);
		replacement.Cookie = MuiApplicationWindowListState.Magic;
		replacement.Application = application;
		replacement.List = list;
		replacement.Entries = entries;
		replacement.Count = count;
		replacement.Capacity = count;
		replacement.Mutation = mutation;
		replacement.Generation = MuiHeadlessMemory.NextSequence(ref platform,
			state);
		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationWindowListState.Size);
		if (block.IsNull || !MuiHeadlessObjectCore.SetRecordAttributeRaw(
			ref platform, state, record, StateAttribute, block.Raw, false))
		{
			if (block.IsNotNull) platform.Free(block,
				MuiApplicationWindowListState.Size);
			FreeProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		// Allocation and the private attribute write do not mutate Family
		// topology, so the captured mutation remains the valid cache key.
		if (!MuiApplicationWindowListStateCodec.Write(ref platform, block,
			replacement))
		{
			MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
				StateAttribute, 0, false);
			platform.Free(block, MuiApplicationWindowListState.Size);
			FreeProjection(ref platform, list, entries, entriesSize);
			return APTR.Null;
		}
		FreeStateBlock(ref platform, oldBlock);
		return list;
	}

	private static uint CountWindows<TPlatform>(ref TPlatform platform,
		APTR state, APTR application) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = 0u;
		for (var index = 0u; index < MuiHeadlessLayout.MaximumTraversal; index++)
		{
			var child = MuiFamilyCore.GetChild(ref platform, state, application,
				unchecked((int)index), APTR.Null);
			if (child.IsNull) break;
			if (IsOwnedWindow(ref platform, state, application, child)) count++;
		}
		return count;
	}

	private static bool IsOwnedWindow<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, APTR child)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, child,
			WindowOwner, out var owner) && owner == application.Raw;

	private static void FreeStateBlock<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiApplicationWindowListStateCodec.TryRead(ref platform, block,
			out var value)) return;
		var entriesSize = value.Count > uint.MaxValue /
			MuiApplicationWindowListEntry.Size ? 0u : value.Count *
			MuiApplicationWindowListEntry.Size;
		FreeProjection(ref platform, value.List, value.Entries, entriesSize);
		platform.Clear(block, MuiApplicationWindowListState.Size);
		platform.Free(block, MuiApplicationWindowListState.Size);
	}

	private static void FreeProjection<TPlatform>(ref TPlatform platform,
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
}
