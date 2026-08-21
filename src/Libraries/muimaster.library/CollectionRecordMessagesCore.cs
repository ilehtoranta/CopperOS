/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Central codec for the fixed MorphOS 3.20 List record packet family. The
// List core consumes named entry/pool, display, compare, and hit-test fields;
// only this adapter owns their packed guest-memory boundaries.
internal enum MuiCollectionRecordPacketKind : byte
{
	EntryPool,
	Display,
	Compare,
	TestPos,
}

internal enum MuiCollectionRecordField : byte
{
	MethodId,
	Entry,
	Pool,
	Array,
	Row,
	Entry1,
	Entry2,
	Column,
	X,
	Y,
	Result,
}

[System.Runtime.InteropServices.StructLayout(
	System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
internal struct MuiCollectionRecordFieldCursor
{
	internal APTR Message;
	internal MuiCollectionRecordPacketKind Packet;
	internal MuiCollectionRecordField Field;
}

internal static class MuiCollectionRecordFieldCursorCodec
{
	private static bool TryResolve(MuiCollectionRecordPacketKind packet,
		MuiCollectionRecordField field, out uint offset)
	{
		switch (packet)
		{
			case MuiCollectionRecordPacketKind.EntryPool:
				if (field == MuiCollectionRecordField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionRecordField.Entry) { offset = 4; return true; }
				if (field == MuiCollectionRecordField.Pool) { offset = 8; return true; }
				break;
			case MuiCollectionRecordPacketKind.Display:
				if (field == MuiCollectionRecordField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionRecordField.Entry) { offset = 4; return true; }
				if (field == MuiCollectionRecordField.Array) { offset = 8; return true; }
				if (field == MuiCollectionRecordField.Row) { offset = 12; return true; }
				break;
			case MuiCollectionRecordPacketKind.Compare:
				if (field == MuiCollectionRecordField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionRecordField.Entry1) { offset = 4; return true; }
				if (field == MuiCollectionRecordField.Entry2) { offset = 8; return true; }
				if (field == MuiCollectionRecordField.Column) { offset = 12; return true; }
				break;
			case MuiCollectionRecordPacketKind.TestPos:
				if (field == MuiCollectionRecordField.MethodId) { offset = 0; return true; }
				if (field == MuiCollectionRecordField.X) { offset = 4; return true; }
				if (field == MuiCollectionRecordField.Y) { offset = 8; return true; }
				if (field == MuiCollectionRecordField.Result) { offset = 12; return true; }
				break;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionRecordFieldCursor cursor, out APTR address)
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
		APTR message, MuiCollectionRecordPacketKind packet,
		MuiCollectionRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiCollectionRecordFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionRecordPacketKind packet,
		MuiCollectionRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiCollectionRecordFieldCursor);
		cursor.Message = message;
		cursor.Packet = packet;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiCollectionRecordMessageCodec
{
	internal const uint Compare = 0x80421B68u;
	internal const uint Construct = 0x8042D662u;
	internal const uint Destruct = 0x80427D51u;
	internal const uint Display = 0x80425377u;
	internal const uint TestPos = 0x80425F48u;

	internal static bool TryReadEntryPool<TPlatform>(ref TPlatform platform,
		APTR message, uint method, out MuiCollectionEntryPoolMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEntryPoolMessage.Size) ||
			(method != Construct && method != Destruct) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != method) return false;
		packet.MethodId = header.MethodId;
		return MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionRecordPacketKind.EntryPool,
			MuiCollectionRecordField.Entry, out packet.Entry) &&
			MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.EntryPool,
				MuiCollectionRecordField.Pool, out packet.Pool);
	}

	internal static bool WriteEntryPool<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint entry, uint pool)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionEntryPoolMessage.Size) ||
			(method != Construct && method != Destruct)) return false;
		return MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionRecordPacketKind.EntryPool,
			MuiCollectionRecordField.MethodId, method) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.EntryPool,
				MuiCollectionRecordField.Entry, entry) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.EntryPool,
				MuiCollectionRecordField.Pool, pool);
	}

	internal static bool TryReadDisplay<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionDisplayMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionDisplayMessage.Size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != Display) return false;
		packet.MethodId = header.MethodId;
		return MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionRecordPacketKind.Display,
			MuiCollectionRecordField.Entry, out packet.Entry) &&
			MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.Display,
				MuiCollectionRecordField.Array, out packet.Array) &&
			MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.Display,
				MuiCollectionRecordField.Row, out packet.Row);
	}

	internal static bool WriteDisplay<TPlatform>(ref TPlatform platform,
		APTR message, uint entry, uint array, uint row)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionDisplayMessage.Size)) return false;
		return MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionRecordPacketKind.Display,
			MuiCollectionRecordField.MethodId, Display) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.Display,
				MuiCollectionRecordField.Entry, entry) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.Display,
				MuiCollectionRecordField.Array, array) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.Display,
				MuiCollectionRecordField.Row, row);
	}

	internal static bool TryReadCompare<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionCompareMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionCompareMessage.Size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != Compare) return false;
		packet.MethodId = header.MethodId;
		return MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionRecordPacketKind.Compare,
			MuiCollectionRecordField.Entry1, out packet.Entry1) &&
			MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.Compare,
				MuiCollectionRecordField.Entry2, out packet.Entry2) &&
			MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.Compare,
				MuiCollectionRecordField.Column, out packet.Column);
	}

	internal static bool WriteCompare<TPlatform>(ref TPlatform platform,
		APTR message, uint entry1, uint entry2, uint column)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionCompareMessage.Size)) return false;
		return MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionRecordPacketKind.Compare,
			MuiCollectionRecordField.MethodId, Compare) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.Compare,
				MuiCollectionRecordField.Entry1, entry1) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.Compare,
				MuiCollectionRecordField.Entry2, entry2) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.Compare,
				MuiCollectionRecordField.Column, column);
	}

	internal static bool TryReadTestPos<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionTestPosMessage.Size) ||
			!MuiCollectionBasicMessageCodec.TryReadMethodId(ref platform, message,
				out var header) || header.MethodId != TestPos) return false;
		packet.MethodId = header.MethodId;
		return MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
			message, MuiCollectionRecordPacketKind.TestPos,
			MuiCollectionRecordField.X, out packet.X) &&
			MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.TestPos,
				MuiCollectionRecordField.Y, out packet.Y) &&
			MuiCollectionRecordFieldCursorCodec.TryReadUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.TestPos,
				MuiCollectionRecordField.Result, out packet.Result);
	}

	internal static bool WriteTestPos<TPlatform>(ref TPlatform platform,
		APTR message, uint x, uint y, uint result)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (message.IsNull || !platform.IsMapped(message,
			MuiCollectionTestPosMessage.Size)) return false;
		return MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
			message, MuiCollectionRecordPacketKind.TestPos,
			MuiCollectionRecordField.MethodId, TestPos) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.TestPos,
				MuiCollectionRecordField.X, x) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.TestPos,
				MuiCollectionRecordField.Y, y) &&
			MuiCollectionRecordFieldCursorCodec.TryWriteUInt32(ref platform,
				message, MuiCollectionRecordPacketKind.TestPos,
				MuiCollectionRecordField.Result, result);
	}
}
