/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Central codec for the fixed MorphOS 3.20 List edit packet family. The List
// state machine consumes named records; only this adapter owns guest-memory
// offsets, signed row/column conversion, and method validation.
internal enum MuiCollectionEditPacketKind : byte
{
	CreateEditObject,
	Edit,
	EditDone,
	EndEdit,
}

internal enum MuiCollectionEditField : byte
{
	MethodId,
	Row,
	Column,
	Entry,
	EditObject,
	Mode,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionEditFieldCursor
{
	internal APTR Message;
	internal MuiCollectionEditPacketKind Packet;
	internal MuiCollectionEditField Field;
}

// Named packet adapters keep List edit payloads as semantic structs. Only
// this memory layer translates their fixed guest boundaries and signed words.
internal static class MuiCollectionEditMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiCollectionEditPacketKind packet,
		out uint size)
	{
		switch (packet)
		{
			case MuiCollectionEditPacketKind.CreateEditObject:
				size = MuiCollectionCreateEditObjectMessage.Size;
				return true;
			case MuiCollectionEditPacketKind.Edit:
				size = MuiCollectionEditMessage.Size;
				return true;
			case MuiCollectionEditPacketKind.EditDone:
				size = MuiCollectionEditDoneMessage.Size;
				return true;
			case MuiCollectionEditPacketKind.EndEdit:
				size = MuiCollectionEndEditMessage.Size;
				return true;
		}
		size = 0;
		return false;
	}

	private static bool TryResolve(MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, out uint offset)
	{
		switch (packet)
		{
			case MuiCollectionEditPacketKind.CreateEditObject:
				if (field == MuiCollectionEditField.MethodId) { offset = MuiCollectionCreateEditObjectMessage.MethodIdOffset; return true; }
				if (field == MuiCollectionEditField.Row) { offset = MuiCollectionCreateEditObjectMessage.RowOffset; return true; }
				if (field == MuiCollectionEditField.Column) { offset = MuiCollectionCreateEditObjectMessage.ColumnOffset; return true; }
				if (field == MuiCollectionEditField.Entry) { offset = MuiCollectionCreateEditObjectMessage.EntryOffset; return true; }
				break;
			case MuiCollectionEditPacketKind.Edit:
				if (field == MuiCollectionEditField.MethodId) { offset = MuiCollectionEditMessage.MethodIdOffset; return true; }
				if (field == MuiCollectionEditField.Row) { offset = MuiCollectionEditMessage.RowOffset; return true; }
				if (field == MuiCollectionEditField.Column) { offset = MuiCollectionEditMessage.ColumnOffset; return true; }
				break;
			case MuiCollectionEditPacketKind.EditDone:
				if (field == MuiCollectionEditField.MethodId) { offset = MuiCollectionEditDoneMessage.MethodIdOffset; return true; }
				if (field == MuiCollectionEditField.Row) { offset = MuiCollectionEditDoneMessage.RowOffset; return true; }
				if (field == MuiCollectionEditField.Column) { offset = MuiCollectionEditDoneMessage.ColumnOffset; return true; }
				if (field == MuiCollectionEditField.Entry) { offset = MuiCollectionEditDoneMessage.EntryOffset; return true; }
				if (field == MuiCollectionEditField.EditObject) { offset = MuiCollectionEditDoneMessage.EditObjectOffset; return true; }
				break;
			case MuiCollectionEditPacketKind.EndEdit:
				if (field == MuiCollectionEditField.MethodId) { offset = MuiCollectionEndEditMessage.MethodIdOffset; return true; }
				if (field == MuiCollectionEditField.Mode) { offset = MuiCollectionEndEditMessage.ModeOffset; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionEditFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor.Message, cursor.Packet,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(packet, field, out var offset) ||
			!TryGetPacketSize(packet, out var packetSize) ||
			message.IsNull || message.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(message, packetSize))
			return false;
		address = APTR.FromPointer(message.Raw + offset);
		return platform.IsMapped(address, MuiCollectionMethodMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, message, packet, field,
			out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper for existing dispatcher and host callers. New code
// should use the packet-named memory adapter above.
internal static class MuiCollectionEditFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionEditFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionEditMessageMemoryCodec.TryGetAddress(ref platform,
			cursor, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionEditMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
}

// Live List edit packets use declaration-order named structs. Signed row and
// column LONGs are transported as raw ULONG bit patterns so every 68k value is
// preserved without an exception or managed conversion path.
internal static class MuiCollectionEditStructPacketCodec
{
	private static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR message, uint size, out MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, message, size,
			out cursor);

	internal static bool TryReadCreateEditObject<TPlatform>(
		ref TPlatform platform, APTR message,
		out MuiCollectionCreateEditObjectMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryCreate(ref platform, message,
			MuiCollectionCreateEditObjectMessage.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawRow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawColumn) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		packet.Row = unchecked((int)rawRow);
		packet.Column = unchecked((int)rawColumn);
		return true;
	}

	internal static bool TryWriteCreateEditObject<TPlatform>(
		ref TPlatform platform, APTR message,
		MuiCollectionCreateEditObjectMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message,
			MuiCollectionCreateEditObjectMessage.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)packet.Row)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)packet.Column)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Entry) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadEdit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryCreate(ref platform, message, MuiCollectionEditMessage.Size,
			out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawRow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawColumn) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		packet.Row = unchecked((int)rawRow);
		packet.Column = unchecked((int)rawColumn);
		return true;
	}

	internal static bool TryWriteEdit<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionEditMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)packet.Row)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)packet.Column)) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadEditDone<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEditDoneMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!TryCreate(ref platform, message, MuiCollectionEditDoneMessage.Size,
			out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawRow) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawColumn) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.EditObject) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		packet.Row = unchecked((int)rawRow);
		packet.Column = unchecked((int)rawColumn);
		return true;
	}

	internal static bool TryWriteEditDone<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditDoneMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionEditDoneMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)packet.Row)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)packet.Column)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Entry) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.EditObject) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadEndEdit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEndEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionEndEditMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Mode) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteEndEdit<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEndEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionEndEditMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Mode) && MuiGuestStructCursor.IsComplete(cursor);
}

internal static class MuiCollectionEditMessageCodec
{
	internal const uint CreateEditObject = 0x804219AEu;
	internal const uint Edit = 0x8042843Du;
	internal const uint EditDone = 0x80423AB3u;
	internal const uint EndEdit = 0x804203EEu;

	internal static bool TryReadCreateEditObject<TPlatform>(
		ref TPlatform platform, APTR message,
		out MuiCollectionCreateEditObjectMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionEditStructPacketCodec.TryReadCreateEditObject(
			ref platform, message, out packet) && packet.MethodId == CreateEditObject;
	}

	internal static bool WriteCreateEditObject<TPlatform>(
		ref TPlatform platform, APTR message, int row, int column, uint entry)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionCreateEditObjectMessage);
		packet.MethodId = CreateEditObject;
		packet.Row = row;
		packet.Column = column;
		packet.Entry = entry;
		return MuiCollectionEditStructPacketCodec.TryWriteCreateEditObject(
			ref platform, message, packet);
	}

	internal static bool TryReadEdit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionEditStructPacketCodec.TryReadEdit(ref platform,
			message, out packet) && packet.MethodId == Edit;
	}

	internal static bool WriteEdit<TPlatform>(ref TPlatform platform,
		APTR message, int row, int column)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionEditMessage);
		packet.MethodId = Edit;
		packet.Row = row;
		packet.Column = column;
		return MuiCollectionEditStructPacketCodec.TryWriteEdit(ref platform,
			message, packet);
	}

	internal static bool TryReadEditDone<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEditDoneMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (MuiCollectionEditStructPacketCodec.TryReadEditDone(ref platform,
			message, out var structural) && structural.MethodId == EditDone)
		{
			packet = structural;
			return true;
		}
		packet = default;
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEditDoneMessage.Size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodIdValue(ref platform, message,
				out methodId) || methodId != EditDone) return false;
		packet.MethodId = methodId;
		if (!MuiCollectionEditMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiCollectionEditPacketKind.EditDone,
			MuiCollectionEditField.Row, out var rawRow) ||
			!MuiCollectionEditMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone,
				MuiCollectionEditField.Column, out var rawColumn) ||
			!MuiCollectionEditMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone,
				MuiCollectionEditField.Entry, out packet.Entry) ||
			!MuiCollectionEditMessageMemoryCodec.TryReadUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone,
				MuiCollectionEditField.EditObject, out packet.EditObject)) return false;
		packet.Row = unchecked((int)rawRow);
		packet.Column = unchecked((int)rawColumn);
		return true;
	}

	internal static bool WriteEditDone<TPlatform>(ref TPlatform platform,
		APTR message, int row, int column, uint entry, uint editObject)
		where TPlatform : struct, IMuiGuestMemory
	{
		var structural = default(MuiCollectionEditDoneMessage);
		structural.MethodId = EditDone;
		structural.Row = row;
		structural.Column = column;
		structural.Entry = entry;
		structural.EditObject = editObject;
		if (MuiCollectionEditStructPacketCodec.TryWriteEditDone(ref platform,
			message, structural)) return true;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEditDoneMessage.Size)) return false;
		return MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionEditPacketKind.EditDone,
			MuiCollectionEditField.MethodId, EditDone) &&
			MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone, MuiCollectionEditField.Row,
				unchecked((uint)row)) &&
			MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone, MuiCollectionEditField.Column,
				unchecked((uint)column)) &&
			MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone, MuiCollectionEditField.Entry,
				entry) &&
			MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone,
				MuiCollectionEditField.EditObject, editObject);
	}

	internal static bool TryReadEndEdit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEndEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (MuiCollectionEditStructPacketCodec.TryReadEndEdit(ref platform,
			message, out var structural) && structural.MethodId == EndEdit)
		{
			packet = structural;
			return true;
		}
		packet = default;
		uint methodId;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEndEditMessage.Size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodIdValue(ref platform, message,
				out methodId) || methodId != EndEdit) return false;
		packet.MethodId = methodId;
		return MuiCollectionEditMessageMemoryCodec.TryReadUInt32(ref platform,
			message, MuiCollectionEditPacketKind.EndEdit,
			MuiCollectionEditField.Mode, out packet.Mode);
	}

	internal static bool WriteEndEdit<TPlatform>(ref TPlatform platform,
		APTR message, uint mode)
		where TPlatform : struct, IMuiGuestMemory
	{
		var structural = default(MuiCollectionEndEditMessage);
		structural.MethodId = EndEdit;
		structural.Mode = mode;
		if (MuiCollectionEditStructPacketCodec.TryWriteEndEdit(ref platform,
			message, structural)) return true;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEndEditMessage.Size)) return false;
		return MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionEditPacketKind.EndEdit,
			MuiCollectionEditField.MethodId, EndEdit) &&
			MuiCollectionEditMessageMemoryCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EndEdit, MuiCollectionEditField.Mode,
				mode);
	}
}
