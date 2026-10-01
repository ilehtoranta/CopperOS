/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The six Listtree presentation attributes that are not part of the mutation
// policy have one canonical guest-resident record.  Format retains its
// caller-owned pointer; Title is the MorphOS boolean compatibility projection
// despite the historical string-typed declaration.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreePresentationStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x4C545650u; // 'LTVP'
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint EmptyNodesOffset = 4;
	internal const uint FormatOffset = 8;
	internal const uint MultiSelectOffset = 12;
	internal const uint NListOffset = 16;
	internal const uint TitleOffset = 20;
	internal const uint TreeColumnOffset = 24;

	internal uint Magic;
	internal uint EmptyNodes;
	internal APTR Format;
	internal uint MultiSelect;
	internal uint NList;
	internal uint Title;
	internal uint TreeColumn;
}

internal enum MuiListtreePresentationField : byte
{
	Magic,
	EmptyNodes,
	Format,
	MultiSelect,
	NList,
	Title,
	TreeColumn,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListtreePresentationFieldCursor
{
	internal APTR Record;
	internal MuiListtreePresentationField Field;
}

internal static class MuiListtreePresentationMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiListtreePresentationField field,
		out uint index)
	{
		index = field switch
		{
			MuiListtreePresentationField.Magic => 0,
			MuiListtreePresentationField.EmptyNodes => 1,
			MuiListtreePresentationField.Format => 2,
			MuiListtreePresentationField.MultiSelect => 3,
			MuiListtreePresentationField.NList => 4,
			MuiListtreePresentationField.Title => 5,
			MuiListtreePresentationField.TreeColumn => 6,
			_ => uint.MaxValue,
		};
		return index != uint.MaxValue;
	}

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiListtreePresentationField field,
		out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = 0;
		if (!TryResolveFieldIndex(field, out var index)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiListtreePresentationStateRecord.FieldSize, out var candidate))
				return false;
			if (current == index)
			{
				address = candidate;
				size = MuiListtreePresentationStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryGetAddress(ref platform, record, field, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, out APTR address,
		out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiListtreePresentationStateRecord.Size, out var cursor)) return false;
		return TryTakeField(ref platform, ref cursor, field, out address, out size);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiListtreePresentationStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiListtreePresentationField.Magic)
			value = state.Magic;
		else if (field == MuiListtreePresentationField.EmptyNodes)
			value = state.EmptyNodes;
		else if (field == MuiListtreePresentationField.Format)
			value = state.Format.Raw;
		else if (field == MuiListtreePresentationField.MultiSelect)
			value = state.MultiSelect;
		else if (field == MuiListtreePresentationField.NList)
			value = state.NList;
		else if (field == MuiListtreePresentationField.Title)
			value = state.Title;
		else if (field == MuiListtreePresentationField.TreeColumn)
			value = state.TreeColumn;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListtreePresentationStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiListtreePresentationField.Magic)
			state.Magic = value;
		else if (field == MuiListtreePresentationField.EmptyNodes)
			state.EmptyNodes = value;
		else if (field == MuiListtreePresentationField.Format)
			state.Format = APTR.FromPointer(value);
		else if (field == MuiListtreePresentationField.MultiSelect)
			state.MultiSelect = value;
		else if (field == MuiListtreePresentationField.NList)
			state.NList = value;
		else if (field == MuiListtreePresentationField.Title)
			state.Title = value;
		else if (field == MuiListtreePresentationField.TreeColumn)
			state.TreeColumn = value;
		else return false;
		return MuiListtreePresentationStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Listtree presentation cursor. Live serialization uses the direct adapter.
internal static class MuiListtreePresentationFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListtreePresentationFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreePresentationMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListtreePresentationFieldCursor cursor, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreePresentationMemoryCodec.TryGetAddress(ref platform, cursor.Record,
			cursor.Field, out address, out size);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreePresentationMemoryCodec.TryReadUInt32(ref platform, record, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListtreePresentationMemoryCodec.TryWriteUInt32(ref platform, record, field,
			value);
}

internal static class MuiListtreePresentationStateRecordCodec
{
	// Declaration-order Listtree presentation record: scalar policy fields,
	// Format APTR, and the title/tree-column projections. The named cursor is
	// the production ABI boundary; the field adapter remains diagnostic.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListtreePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListtreePresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.EmptyNodes) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var format) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MultiSelect) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.NList) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var title) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.TreeColumn)) return false;
		value.Format = APTR.FromPointer(format);
		value.Title = title;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiListtreePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListtreePresentationStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.EmptyNodes) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Format.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MultiSelect) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.NList) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Title) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.TreeColumn) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListtreePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListtreePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadStructural(ref platform, address, out value) ||
			value.Magic != MuiListtreePresentationStateRecord.Cookie) return false;
		value.Title = value.Title == 0 ? 0u : 1u;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListtreePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull ||
			!platform.IsMapped(address, MuiListtreePresentationStateRecord.Size) ||
			value.Magic != MuiListtreePresentationStateRecord.Cookie) return false;
		return WriteRecord(ref platform, address, value);
	}
}
