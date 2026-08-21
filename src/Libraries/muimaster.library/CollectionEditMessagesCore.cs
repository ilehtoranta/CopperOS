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

internal static class MuiCollectionEditFieldCursorCodec
{
	private static bool TryResolve(MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, out uint offset)
	{
		switch (packet)
		{
			case MuiCollectionEditPacketKind.CreateEditObject:
				if (field == MuiCollectionEditField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionEditField.Row) { offset = 4; return true; }
				if (field == MuiCollectionEditField.Column) { offset = 8; return true; }
				if (field == MuiCollectionEditField.Entry) { offset = 12; return true; }
				break;
			case MuiCollectionEditPacketKind.Edit:
				if (field == MuiCollectionEditField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionEditField.Row) { offset = 4; return true; }
				if (field == MuiCollectionEditField.Column) { offset = 8; return true; }
				break;
			case MuiCollectionEditPacketKind.EditDone:
				if (field == MuiCollectionEditField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionEditField.Row) { offset = 4; return true; }
				if (field == MuiCollectionEditField.Column) { offset = 8; return true; }
				if (field == MuiCollectionEditField.Entry) { offset = 12; return true; }
				if (field == MuiCollectionEditField.EditObject) { offset = 16; return true; }
				break;
			case MuiCollectionEditPacketKind.EndEdit:
				if (field == MuiCollectionEditField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionEditField.Mode) { offset = 4; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionEditFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Packet, cursor.Field, out var offset) ||
			cursor.Message.IsNull || cursor.Message.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(cursor.Message.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiCollectionEditFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEditPacketKind packet,
		MuiCollectionEditField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiCollectionEditFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
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
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionCreateEditObjectMessage.Size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != CreateEditObject) return false;
		packet.MethodId = header.MethodId;
		if (!MuiCollectionEditFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionEditPacketKind.CreateEditObject,
			MuiCollectionEditField.Row, out var rawRow) ||
			!MuiCollectionEditFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCollectionEditPacketKind.CreateEditObject,
				MuiCollectionEditField.Column, out var rawColumn) ||
			!MuiCollectionEditFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCollectionEditPacketKind.CreateEditObject,
				MuiCollectionEditField.Entry, out packet.Entry)) return false;
		packet.Row = unchecked((int)rawRow);
		packet.Column = unchecked((int)rawColumn);
		return true;
	}

	internal static bool WriteCreateEditObject<TPlatform>(
		ref TPlatform platform, APTR message, int row, int column, uint entry)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionCreateEditObjectMessage.Size)) return false;
		return MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionEditPacketKind.CreateEditObject,
			MuiCollectionEditField.MethodId, CreateEditObject) &&
			MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.CreateEditObject,
				MuiCollectionEditField.Row, unchecked((uint)row)) &&
			MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.CreateEditObject,
				MuiCollectionEditField.Column, unchecked((uint)column)) &&
			MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.CreateEditObject,
				MuiCollectionEditField.Entry, entry);
	}

	internal static bool TryReadEdit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEditMessage.Size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != Edit)
			return false;
		packet.MethodId = header.MethodId;
		if (!MuiCollectionEditFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionEditPacketKind.Edit,
			MuiCollectionEditField.Row, out var rawRow) ||
			!MuiCollectionEditFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCollectionEditPacketKind.Edit,
				MuiCollectionEditField.Column, out var rawColumn)) return false;
		packet.Row = unchecked((int)rawRow);
		packet.Column = unchecked((int)rawColumn);
		return true;
	}

	internal static bool WriteEdit<TPlatform>(ref TPlatform platform,
		APTR message, int row, int column)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEditMessage.Size)) return false;
		return MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionEditPacketKind.Edit,
			MuiCollectionEditField.MethodId, Edit) &&
			MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.Edit, MuiCollectionEditField.Row,
				unchecked((uint)row)) &&
			MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.Edit, MuiCollectionEditField.Column,
				unchecked((uint)column));
	}

	internal static bool TryReadEditDone<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEditDoneMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEditDoneMessage.Size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != EditDone) return false;
		packet.MethodId = header.MethodId;
		if (!MuiCollectionEditFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionEditPacketKind.EditDone,
			MuiCollectionEditField.Row, out var rawRow) ||
			!MuiCollectionEditFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone,
				MuiCollectionEditField.Column, out var rawColumn) ||
			!MuiCollectionEditFieldCursorCodec.TryReadUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone,
				MuiCollectionEditField.Entry, out packet.Entry) ||
			!MuiCollectionEditFieldCursorCodec.TryReadUInt32(ref platform, message,
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
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEditDoneMessage.Size)) return false;
		return MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionEditPacketKind.EditDone,
			MuiCollectionEditField.MethodId, EditDone) &&
			MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone, MuiCollectionEditField.Row,
				unchecked((uint)row)) &&
			MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone, MuiCollectionEditField.Column,
				unchecked((uint)column)) &&
			MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone, MuiCollectionEditField.Entry,
				entry) &&
			MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EditDone,
				MuiCollectionEditField.EditObject, editObject);
	}

	internal static bool TryReadEndEdit<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEndEditMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEndEditMessage.Size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != EndEdit) return false;
		packet.MethodId = header.MethodId;
		return MuiCollectionEditFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionEditPacketKind.EndEdit,
			MuiCollectionEditField.Mode, out packet.Mode);
	}

	internal static bool WriteEndEdit<TPlatform>(ref TPlatform platform,
		APTR message, uint mode)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEndEditMessage.Size)) return false;
		return MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionEditPacketKind.EndEdit,
			MuiCollectionEditField.MethodId, EndEdit) &&
			MuiCollectionEditFieldCursorCodec.TryWriteUInt32(ref platform, message,
				MuiCollectionEditPacketKind.EndEdit, MuiCollectionEditField.Mode,
				mode);
	}
}
