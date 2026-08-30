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
	private static bool TryResolve(MuiListtreePresentationField field,
		out uint offset, out uint recordSize)
	{
		recordSize = MuiListtreePresentationStateRecord.Size;
		if (field == MuiListtreePresentationField.Magic)
		{
			offset = MuiListtreePresentationStateRecord.MagicOffset;
			return true;
		}
		if (field == MuiListtreePresentationField.EmptyNodes)
		{
			offset = MuiListtreePresentationStateRecord.EmptyNodesOffset;
			return true;
		}
		if (field == MuiListtreePresentationField.Format)
		{
			offset = MuiListtreePresentationStateRecord.FormatOffset;
			return true;
		}
		if (field == MuiListtreePresentationField.MultiSelect)
		{
			offset = MuiListtreePresentationStateRecord.MultiSelectOffset;
			return true;
		}
		if (field == MuiListtreePresentationField.NList)
		{
			offset = MuiListtreePresentationStateRecord.NListOffset;
			return true;
		}
		if (field == MuiListtreePresentationField.Title)
		{
			offset = MuiListtreePresentationStateRecord.TitleOffset;
			return true;
		}
		if (field == MuiListtreePresentationField.TreeColumn)
		{
			offset = MuiListtreePresentationStateRecord.TreeColumnOffset;
			return true;
		}
		offset = 0;
		recordSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var recordSize) ||
			record.IsNull || record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, recordSize)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address,
			MuiListtreePresentationStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
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
