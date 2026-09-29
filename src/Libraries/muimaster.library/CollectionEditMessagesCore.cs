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

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint fieldIndex;
		switch (packet)
		{
			case MuiCollectionEditPacketKind.CreateEditObject:
				fieldIndex = field switch
				{
					MuiCollectionEditField.MethodId => 0,
					MuiCollectionEditField.Row => 1,
					MuiCollectionEditField.Column => 2,
					MuiCollectionEditField.Entry => 3,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionEditPacketKind.Edit:
				fieldIndex = field switch
				{
					MuiCollectionEditField.MethodId => 0,
					MuiCollectionEditField.Row => 1,
					MuiCollectionEditField.Column => 2,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionEditPacketKind.EditDone:
				fieldIndex = field switch
				{
					MuiCollectionEditField.MethodId => 0,
					MuiCollectionEditField.Row => 1,
					MuiCollectionEditField.Column => 2,
					MuiCollectionEditField.Entry => 3,
					MuiCollectionEditField.EditObject => 4,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionEditPacketKind.EndEdit:
				fieldIndex = field switch
				{
					MuiCollectionEditField.MethodId => 0,
					MuiCollectionEditField.Mode => 1,
					_ => uint.MaxValue,
				};
				break;
			default:
				return false;
		}
		if (fieldIndex == uint.MaxValue) return false;
		if (fieldIndex > 0 && !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, MuiCollectionMethodMessage.FieldSize, out _)) return false;
		if (fieldIndex > 1 && !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, MuiCollectionMethodMessage.FieldSize, out _)) return false;
		if (fieldIndex > 2 && !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, MuiCollectionMethodMessage.FieldSize, out _)) return false;
		if (fieldIndex > 3 && !MuiGuestStructCursor.TryTake(ref platform,
			ref cursor, MuiCollectionMethodMessage.FieldSize, out _)) return false;
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiCollectionMethodMessage.FieldSize, out address);
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
		if (!TryGetPacketSize(packet, out var packetSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, message, packetSize,
				out var guestCursor) ||
			!TryTakeField(ref platform, ref guestCursor, packet, field,
				out address)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (packet == MuiCollectionEditPacketKind.CreateEditObject)
		{
			if (!MuiCollectionEditStructPacketCodec.TryReadCreateEditObject(
				ref platform, message, out var create)) return false;
			if (field == MuiCollectionEditField.MethodId)
				value = create.MethodId;
			else if (field == MuiCollectionEditField.Row)
				value = unchecked((uint)create.Row);
			else if (field == MuiCollectionEditField.Column)
				value = unchecked((uint)create.Column);
			else if (field == MuiCollectionEditField.Entry)
				value = create.Entry;
			else return false;
			return true;
		}
		if (packet == MuiCollectionEditPacketKind.Edit)
		{
			if (!MuiCollectionEditStructPacketCodec.TryReadEdit(ref platform,
				message, out var edit)) return false;
			if (field == MuiCollectionEditField.MethodId)
				value = edit.MethodId;
			else if (field == MuiCollectionEditField.Row)
				value = unchecked((uint)edit.Row);
			else if (field == MuiCollectionEditField.Column)
				value = unchecked((uint)edit.Column);
			else return false;
			return true;
		}
		if (packet == MuiCollectionEditPacketKind.EditDone)
		{
			if (!MuiCollectionEditStructPacketCodec.TryReadEditDone(ref platform,
				message, out var done)) return false;
			if (field == MuiCollectionEditField.MethodId)
				value = done.MethodId;
			else if (field == MuiCollectionEditField.Row)
				value = unchecked((uint)done.Row);
			else if (field == MuiCollectionEditField.Column)
				value = unchecked((uint)done.Column);
			else if (field == MuiCollectionEditField.Entry)
				value = done.Entry;
			else if (field == MuiCollectionEditField.EditObject)
				value = done.EditObject;
			else return false;
			return true;
		}
		if (packet == MuiCollectionEditPacketKind.EndEdit)
		{
			if (!MuiCollectionEditStructPacketCodec.TryReadEndEdit(ref platform,
				message, out var endEdit)) return false;
			if (field == MuiCollectionEditField.MethodId)
				value = endEdit.MethodId;
			else if (field == MuiCollectionEditField.Mode)
				value = endEdit.Mode;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet == MuiCollectionEditPacketKind.CreateEditObject)
		{
			if (!MuiCollectionEditStructPacketCodec.TryReadCreateEditObject(
				ref platform, message, out var create)) return false;
			if (field == MuiCollectionEditField.MethodId)
				create.MethodId = value;
			else if (field == MuiCollectionEditField.Row)
				create.Row = unchecked((int)value);
			else if (field == MuiCollectionEditField.Column)
				create.Column = unchecked((int)value);
			else if (field == MuiCollectionEditField.Entry)
				create.Entry = value;
			else return false;
			return MuiCollectionEditStructPacketCodec.TryWriteCreateEditObject(
				ref platform, message, create);
		}
		if (packet == MuiCollectionEditPacketKind.Edit)
		{
			if (!MuiCollectionEditStructPacketCodec.TryReadEdit(ref platform,
				message, out var edit)) return false;
			if (field == MuiCollectionEditField.MethodId)
				edit.MethodId = value;
			else if (field == MuiCollectionEditField.Row)
				edit.Row = unchecked((int)value);
			else if (field == MuiCollectionEditField.Column)
				edit.Column = unchecked((int)value);
			else return false;
			return MuiCollectionEditStructPacketCodec.TryWriteEdit(ref platform,
				message, edit);
		}
		if (packet == MuiCollectionEditPacketKind.EditDone)
		{
			if (!MuiCollectionEditStructPacketCodec.TryReadEditDone(ref platform,
				message, out var done)) return false;
			if (field == MuiCollectionEditField.MethodId)
				done.MethodId = value;
			else if (field == MuiCollectionEditField.Row)
				done.Row = unchecked((int)value);
			else if (field == MuiCollectionEditField.Column)
				done.Column = unchecked((int)value);
			else if (field == MuiCollectionEditField.Entry)
				done.Entry = value;
			else if (field == MuiCollectionEditField.EditObject)
				done.EditObject = value;
			else return false;
			return MuiCollectionEditStructPacketCodec.TryWriteEditDone(ref platform,
				message, done);
		}
		if (packet == MuiCollectionEditPacketKind.EndEdit)
		{
			if (!MuiCollectionEditStructPacketCodec.TryReadEndEdit(ref platform,
				message, out var endEdit)) return false;
			if (field == MuiCollectionEditField.MethodId)
				endEdit.MethodId = value;
			else if (field == MuiCollectionEditField.Mode)
				endEdit.Mode = value;
			else return false;
			return MuiCollectionEditStructPacketCodec.TryWriteEndEdit(ref platform,
				message, endEdit);
		}
		return false;
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
		packet = default;
		if (!MuiCollectionEditStructPacketCodec.TryReadEditDone(ref platform,
			message, out var structural) || structural.MethodId != EditDone)
			return false;
		packet = structural;
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
		return MuiCollectionEditStructPacketCodec.TryWriteEditDone(ref platform,
			message, structural);
	}

	internal static bool TryReadEndEdit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEndEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (!MuiCollectionEditStructPacketCodec.TryReadEndEdit(ref platform,
			message, out var structural) || structural.MethodId != EndEdit)
			return false;
		packet = structural;
		return true;
	}

	internal static bool WriteEndEdit<TPlatform>(ref TPlatform platform,
		APTR message, uint mode)
		where TPlatform : struct, IMuiGuestMemory
	{
		var structural = default(MuiCollectionEndEditMessage);
		structural.MethodId = EndEdit;
		structural.Mode = mode;
		return MuiCollectionEditStructPacketCodec.TryWriteEndEdit(ref platform,
			message, structural);
	}
}
