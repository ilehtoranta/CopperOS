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

internal static class MuiListtreePresentationFieldCursorCodec
{
	private static bool TryResolve(MuiListtreePresentationField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiListtreePresentationField.Magic: offset = 0; return true;
			case MuiListtreePresentationField.EmptyNodes: offset = 4; return true;
			case MuiListtreePresentationField.Format: offset = 8; return true;
			case MuiListtreePresentationField.MultiSelect: offset = 12; return true;
			case MuiListtreePresentationField.NList: offset = 16; return true;
			case MuiListtreePresentationField.Title: offset = 20; return true;
			case MuiListtreePresentationField.TreeColumn: offset = 24; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListtreePresentationFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiListtreePresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiListtreePresentationFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListtreePresentationField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListtreePresentationFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiListtreePresentationStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListtreePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull ||
			!platform.IsMapped(address, MuiListtreePresentationStateRecord.Size) ||
			!MuiListtreePresentationFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListtreePresentationField.Magic, out var magic) ||
			magic != MuiListtreePresentationStateRecord.Cookie ||
			!MuiListtreePresentationFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListtreePresentationField.EmptyNodes,
				out value.EmptyNodes) ||
			!MuiListtreePresentationFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListtreePresentationField.Format, out var format) ||
			!MuiListtreePresentationFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListtreePresentationField.MultiSelect,
				out value.MultiSelect) ||
			!MuiListtreePresentationFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListtreePresentationField.NList, out value.NList) ||
			!MuiListtreePresentationFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListtreePresentationField.Title, out var title) ||
			!MuiListtreePresentationFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListtreePresentationField.TreeColumn,
				out value.TreeColumn)) return false;
		value.Magic = magic;
		value.Format = APTR.FromPointer(format);
		value.Title = title == 0 ? 0u : 1u;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListtreePresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull ||
			!platform.IsMapped(address, MuiListtreePresentationStateRecord.Size) ||
			value.Magic != MuiListtreePresentationStateRecord.Cookie) return false;
		return MuiListtreePresentationFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiListtreePresentationField.Magic, value.Magic) &&
			MuiListtreePresentationFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreePresentationField.EmptyNodes, value.EmptyNodes) &&
			MuiListtreePresentationFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreePresentationField.Format, value.Format.Raw) &&
			MuiListtreePresentationFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreePresentationField.MultiSelect,
				value.MultiSelect) &&
			MuiListtreePresentationFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreePresentationField.NList, value.NList) &&
			MuiListtreePresentationFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreePresentationField.Title, value.Title) &&
			MuiListtreePresentationFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListtreePresentationField.TreeColumn,
				value.TreeColumn);
	}
}
