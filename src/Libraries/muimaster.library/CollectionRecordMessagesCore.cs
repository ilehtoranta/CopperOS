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

// Named packet adapters keep the MorphOS List record payloads as semantic
// structs. Only this memory layer translates their fixed guest boundaries.
internal static class MuiCollectionRecordMessageMemoryCodec
{
	private static bool TryGetPacketSize(MuiCollectionRecordPacketKind packet,
		out uint size)
	{
		switch (packet)
		{
			case MuiCollectionRecordPacketKind.EntryPool:
				size = MuiCollectionEntryPoolMessage.Size;
				return true;
			case MuiCollectionRecordPacketKind.Display:
				size = MuiCollectionDisplayMessage.Size;
				return true;
			case MuiCollectionRecordPacketKind.Compare:
				size = MuiCollectionCompareMessage.Size;
				return true;
			case MuiCollectionRecordPacketKind.TestPos:
				size = MuiCollectionTestPosMessage.Size;
				return true;
		}
		size = 0;
		return false;
	}

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiCollectionRecordPacketKind packet,
		MuiCollectionRecordField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint fieldIndex;
		switch (packet)
		{
			case MuiCollectionRecordPacketKind.EntryPool:
				fieldIndex = field switch
				{
					MuiCollectionRecordField.MethodId => 0,
					MuiCollectionRecordField.Entry => 1,
					MuiCollectionRecordField.Pool => 2,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionRecordPacketKind.Display:
				fieldIndex = field switch
				{
					MuiCollectionRecordField.MethodId => 0,
					MuiCollectionRecordField.Entry => 1,
					MuiCollectionRecordField.Array => 2,
					MuiCollectionRecordField.Row => 3,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionRecordPacketKind.Compare:
				fieldIndex = field switch
				{
					MuiCollectionRecordField.MethodId => 0,
					MuiCollectionRecordField.Entry1 => 1,
					MuiCollectionRecordField.Entry2 => 2,
					MuiCollectionRecordField.Column => 3,
					_ => uint.MaxValue,
				};
				break;
			case MuiCollectionRecordPacketKind.TestPos:
				fieldIndex = field switch
				{
					MuiCollectionRecordField.MethodId => 0,
					MuiCollectionRecordField.X => 1,
					MuiCollectionRecordField.Y => 2,
					MuiCollectionRecordField.Result => 3,
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
		return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiCollectionMethodMessage.FieldSize, out address);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor.Message, cursor.Packet,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionRecordPacketKind packet,
		MuiCollectionRecordField field, out APTR address)
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
		APTR message, MuiCollectionRecordPacketKind packet,
		MuiCollectionRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (packet == MuiCollectionRecordPacketKind.EntryPool)
		{
			if (!MuiCollectionRecordStructPacketCodec.TryReadEntryPool(ref platform,
				message, out var entryPool)) return false;
			if (field == MuiCollectionRecordField.MethodId)
				value = entryPool.MethodId;
			else if (field == MuiCollectionRecordField.Entry)
				value = entryPool.Entry;
			else if (field == MuiCollectionRecordField.Pool)
				value = entryPool.Pool;
			else return false;
			return true;
		}
		if (packet == MuiCollectionRecordPacketKind.Display)
		{
			if (!MuiCollectionRecordStructPacketCodec.TryReadDisplay(ref platform,
				message, out var display)) return false;
			if (field == MuiCollectionRecordField.MethodId)
				value = display.MethodId;
			else if (field == MuiCollectionRecordField.Entry)
				value = display.Entry;
			else if (field == MuiCollectionRecordField.Array)
				value = display.Array;
			else if (field == MuiCollectionRecordField.Row)
				value = display.Row;
			else return false;
			return true;
		}
		if (packet == MuiCollectionRecordPacketKind.Compare)
		{
			if (!MuiCollectionRecordStructPacketCodec.TryReadCompare(ref platform,
				message, out var compare)) return false;
			if (field == MuiCollectionRecordField.MethodId)
				value = compare.MethodId;
			else if (field == MuiCollectionRecordField.Entry1)
				value = compare.Entry1;
			else if (field == MuiCollectionRecordField.Entry2)
				value = compare.Entry2;
			else if (field == MuiCollectionRecordField.Column)
				value = compare.Column;
			else return false;
			return true;
		}
		if (packet == MuiCollectionRecordPacketKind.TestPos)
		{
			if (!MuiCollectionRecordStructPacketCodec.TryReadTestPos(ref platform,
				message, out var testPos)) return false;
			if (field == MuiCollectionRecordField.MethodId)
				value = testPos.MethodId;
			else if (field == MuiCollectionRecordField.X)
				value = testPos.X;
			else if (field == MuiCollectionRecordField.Y)
				value = testPos.Y;
			else if (field == MuiCollectionRecordField.Result)
				value = testPos.Result;
			else return false;
			return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionRecordPacketKind packet,
		MuiCollectionRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (packet == MuiCollectionRecordPacketKind.EntryPool)
		{
			if (!MuiCollectionRecordStructPacketCodec.TryReadEntryPool(ref platform,
				message, out var entryPool)) return false;
			if (field == MuiCollectionRecordField.MethodId)
				entryPool.MethodId = value;
			else if (field == MuiCollectionRecordField.Entry)
				entryPool.Entry = value;
			else if (field == MuiCollectionRecordField.Pool)
				entryPool.Pool = value;
			else return false;
			return MuiCollectionRecordStructPacketCodec.TryWriteEntryPool(ref platform,
				message, entryPool);
		}
		if (packet == MuiCollectionRecordPacketKind.Display)
		{
			if (!MuiCollectionRecordStructPacketCodec.TryReadDisplay(ref platform,
				message, out var display)) return false;
			if (field == MuiCollectionRecordField.MethodId)
				display.MethodId = value;
			else if (field == MuiCollectionRecordField.Entry)
				display.Entry = value;
			else if (field == MuiCollectionRecordField.Array)
				display.Array = value;
			else if (field == MuiCollectionRecordField.Row)
				display.Row = value;
			else return false;
			return MuiCollectionRecordStructPacketCodec.TryWriteDisplay(ref platform,
				message, display);
		}
		if (packet == MuiCollectionRecordPacketKind.Compare)
		{
			if (!MuiCollectionRecordStructPacketCodec.TryReadCompare(ref platform,
				message, out var compare)) return false;
			if (field == MuiCollectionRecordField.MethodId)
				compare.MethodId = value;
			else if (field == MuiCollectionRecordField.Entry1)
				compare.Entry1 = value;
			else if (field == MuiCollectionRecordField.Entry2)
				compare.Entry2 = value;
			else if (field == MuiCollectionRecordField.Column)
				compare.Column = value;
			else return false;
			return MuiCollectionRecordStructPacketCodec.TryWriteCompare(ref platform,
				message, compare);
		}
		if (packet == MuiCollectionRecordPacketKind.TestPos)
		{
			if (!MuiCollectionRecordStructPacketCodec.TryReadTestPos(ref platform,
				message, out var testPos)) return false;
			if (field == MuiCollectionRecordField.MethodId)
				testPos.MethodId = value;
			else if (field == MuiCollectionRecordField.X)
				testPos.X = value;
			else if (field == MuiCollectionRecordField.Y)
				testPos.Y = value;
			else if (field == MuiCollectionRecordField.Result)
				testPos.Result = value;
			else return false;
			return MuiCollectionRecordStructPacketCodec.TryWriteTestPos(ref platform,
				message, testPos);
		}
		return false;
	}
}

// Compatibility wrapper for existing dispatcher and host callers. New code
// should use the packet-named memory adapter above.
internal static class MuiCollectionRecordFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiCollectionRecordFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionRecordMessageMemoryCodec.TryGetAddress(ref platform,
			cursor, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionRecordPacketKind packet,
		MuiCollectionRecordField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionRecordMessageMemoryCodec.TryReadUInt32(ref platform,
			message, packet, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionRecordPacketKind packet,
		MuiCollectionRecordField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiCollectionRecordMessageMemoryCodec.TryWriteUInt32(ref platform,
			message, packet, field, value);
}

// Live List record packets use declaration-order named structs. Raw numeric
// positions remain confined to the compatibility adapter above.
internal static class MuiCollectionRecordStructPacketCodec
{
	private static bool TryCreate<TPlatform>(ref TPlatform platform,
		APTR message, uint size, out MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, message, size,
			out cursor);

	internal static bool TryReadEntryPool<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionEntryPoolMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionEntryPoolMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Pool) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteEntryPool<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionEntryPoolMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionEntryPoolMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Entry) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Pool) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadDisplay<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionDisplayMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionDisplayMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Array) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Row) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteDisplay<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionDisplayMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionDisplayMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Entry) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Array) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Row) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadCompare<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionCompareMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionCompareMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry1) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Entry2) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Column) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteCompare<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionCompareMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionCompareMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Entry1) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Entry2) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Column) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadTestPos<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return TryCreate(ref platform, message, MuiCollectionTestPosMessage.Size,
			out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.X) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Y) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out packet.Result) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryWriteTestPos<TPlatform>(ref TPlatform platform,
		APTR message, MuiCollectionTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory =>
		TryCreate(ref platform, message, MuiCollectionTestPosMessage.Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.X) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Y) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			packet.Result) && MuiGuestStructCursor.IsComplete(cursor);
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
		return (method == Construct || method == Destruct) &&
			MuiCollectionRecordStructPacketCodec.TryReadEntryPool(ref platform,
				message, out packet) && packet.MethodId == method;
	}

	internal static bool WriteEntryPool<TPlatform>(ref TPlatform platform,
		APTR message, uint method, uint entry, uint pool)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (method != Construct && method != Destruct) return false;
		var packet = default(MuiCollectionEntryPoolMessage);
		packet.MethodId = method;
		packet.Entry = entry;
		packet.Pool = pool;
		return MuiCollectionRecordStructPacketCodec.TryWriteEntryPool(ref platform,
			message, packet);
	}

	internal static bool TryReadDisplay<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionDisplayMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionRecordStructPacketCodec.TryReadDisplay(ref platform,
			message, out packet) && packet.MethodId == Display;
	}

	internal static bool WriteDisplay<TPlatform>(ref TPlatform platform,
		APTR message, uint entry, uint array, uint row)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionDisplayMessage);
		packet.MethodId = Display;
		packet.Entry = entry;
		packet.Array = array;
		packet.Row = row;
		return MuiCollectionRecordStructPacketCodec.TryWriteDisplay(ref platform,
			message, packet);
	}

	internal static bool TryReadCompare<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionCompareMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionRecordStructPacketCodec.TryReadCompare(ref platform,
			message, out packet) && packet.MethodId == Compare;
	}

	internal static bool WriteCompare<TPlatform>(ref TPlatform platform,
		APTR message, uint entry1, uint entry2, uint column)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionCompareMessage);
		packet.MethodId = Compare;
		packet.Entry1 = entry1;
		packet.Entry2 = entry2;
		packet.Column = column;
		return MuiCollectionRecordStructPacketCodec.TryWriteCompare(ref platform,
			message, packet);
	}

	internal static bool TryReadTestPos<TPlatform>(ref TPlatform platform,
		APTR message, out MuiCollectionTestPosMessage packet)
		where TPlatform : struct, IMuiGuestMemory
	{
		packet = default;
		return MuiCollectionRecordStructPacketCodec.TryReadTestPos(ref platform,
			message, out packet) && packet.MethodId == TestPos;
	}

	internal static bool WriteTestPos<TPlatform>(ref TPlatform platform,
		APTR message, uint x, uint y, uint result)
		where TPlatform : struct, IMuiGuestMemory
	{
		var packet = default(MuiCollectionTestPosMessage);
		packet.MethodId = TestPos;
		packet.X = x;
		packet.Y = y;
		packet.Result = result;
		return MuiCollectionRecordStructPacketCodec.TryWriteTestPos(ref platform,
			message, packet);
	}
}
