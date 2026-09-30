/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_AppMessage is a transient pointer to Exec's fixed AppMessage record.
// The record remains caller-owned guest memory; this named shape and its codec
// are the only place that crosses the packed Workbench ABI.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAppMessageNodeState
{
	internal const uint Size = 20;
	internal const uint FieldSize32 = 4;
	internal const uint FieldSize16 = 2;
	internal const uint FieldSize8 = 1;
	internal const uint SuccessorOffset = 0;
	internal const uint PredecessorOffset = 4;
	internal const uint TypeOffset = 8;
	internal const uint PriorityOffset = 9;
	internal const uint NameOffset = 10;
	internal const uint ReplyPortOffset = 14;
	internal const uint LengthOffset = 18;

	internal APTR Successor;
	internal APTR Predecessor;
	internal byte Type;
	internal sbyte Priority;
	internal APTR Name;
	internal APTR ReplyPort;
	internal ushort Length;
}

internal enum MuiAppMessageNodeField : byte
{
	Successor,
	Predecessor,
	Type,
	Priority,
	Name,
	ReplyPort,
	Length,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAppMessageNodeFieldCursor
{
	internal APTR Record;
	internal MuiAppMessageNodeField Field;
}

internal static class MuiAppMessageNodeFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAppMessageNodeFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageNodeMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address, out _);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageNodeMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageNodeMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageNodeMemoryCodec.TryReadUInt16(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageNodeMemoryCodec.TryWriteUInt16(ref platform,
			record, field, value);
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageNodeMemoryCodec.TryReadUInt8(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageNodeMemoryCodec.TryWriteUInt8(ref platform,
			record, field, value);
	}
}

// Fixed Exec message-node fields are translated through this named adapter.
// Mixed byte, word, and long widths stay local to the bounded MorphOS guest
// record boundary; message consumers use the semantic node struct.
internal static class MuiAppMessageNodeMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiAppMessageNodeField field,
		out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		switch (field)
		{
			case MuiAppMessageNodeField.Successor:
				fieldSize = MuiAppMessageNodeState.FieldSize32;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					fieldSize, out address);
			case MuiAppMessageNodeField.Predecessor:
				fieldSize = MuiAppMessageNodeState.FieldSize32;
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiAppMessageNodeState.FieldSize32, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					fieldSize, out address);
			case MuiAppMessageNodeField.Type:
				fieldSize = MuiAppMessageNodeState.FieldSize8;
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiAppMessageNodeState.FieldSize32, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize32, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					fieldSize, out address);
			case MuiAppMessageNodeField.Priority:
				fieldSize = MuiAppMessageNodeState.FieldSize8;
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiAppMessageNodeState.FieldSize32, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize32, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize8, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					fieldSize, out address);
			case MuiAppMessageNodeField.Name:
				fieldSize = MuiAppMessageNodeState.FieldSize32;
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiAppMessageNodeState.FieldSize32, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize32, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize8, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize8, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					fieldSize, out address);
			case MuiAppMessageNodeField.ReplyPort:
				fieldSize = MuiAppMessageNodeState.FieldSize32;
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiAppMessageNodeState.FieldSize32, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize32, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize8, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize8, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize32, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					fieldSize, out address);
			case MuiAppMessageNodeField.Length:
				fieldSize = MuiAppMessageNodeState.FieldSize16;
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiAppMessageNodeState.FieldSize32, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize32, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize8, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize8, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize32, out _) ||
					!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageNodeState.FieldSize32, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					fieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiAppMessageNodeState.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address,
				out fieldSize)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAppMessageNodeCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAppMessageNodeField.Successor) value = state.Successor.Raw;
		else if (field == MuiAppMessageNodeField.Predecessor) value = state.Predecessor.Raw;
		else if (field == MuiAppMessageNodeField.Name) value = state.Name.Raw;
		else if (field == MuiAppMessageNodeField.ReplyPort) value = state.ReplyPort.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAppMessageNodeCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAppMessageNodeField.Successor) state.Successor =
			APTR.FromPointer(value);
		else if (field == MuiAppMessageNodeField.Predecessor) state.Predecessor =
			APTR.FromPointer(value);
		else if (field == MuiAppMessageNodeField.Name) state.Name =
			APTR.FromPointer(value);
		else if (field == MuiAppMessageNodeField.ReplyPort) state.ReplyPort =
			APTR.FromPointer(value);
		else return false;
		return MuiAppMessageNodeCodec.WriteStructural(ref platform, record, state);
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAppMessageNodeCodec.TryReadStructural(ref platform, record,
			out var state) || field != MuiAppMessageNodeField.Length) return false;
		value = state.Length;
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAppMessageNodeCodec.TryReadStructural(ref platform, record,
			out var state) || field != MuiAppMessageNodeField.Length) return false;
		state.Length = value;
		return MuiAppMessageNodeCodec.WriteStructural(ref platform, record, state);
	}

	internal static bool TryReadUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAppMessageNodeCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAppMessageNodeField.Type) value = state.Type;
		else if (field == MuiAppMessageNodeField.Priority) value =
			unchecked((byte)state.Priority);
		else return false;
		return true;
	}

	internal static bool TryWriteUInt8<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageNodeField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAppMessageNodeCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAppMessageNodeField.Type) state.Type = value;
		else if (field == MuiAppMessageNodeField.Priority) state.Priority =
			unchecked((sbyte)value);
		else return false;
		return MuiAppMessageNodeCodec.WriteStructural(ref platform, record, state);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAppMessageRecord
{
	internal const uint Size = 86;
	internal const uint FieldSize32 = 4;
	internal const uint FieldSize16 = 2;
	internal const uint TypeOffset = 20;
	internal const uint UserDataOffset = 22;
	internal const uint IdOffset = 26;
	internal const uint NumberOfArgumentsOffset = 30;
	internal const uint ArgumentListOffset = 34;
	internal const uint VersionOffset = 38;
	internal const uint ClassOffset = 40;
	internal const uint MouseXOffset = 42;
	internal const uint MouseYOffset = 44;
	internal const uint SecondsOffset = 46;
	internal const uint MicrosOffset = 50;
	internal const uint Reserved0Offset = 54;
	internal const uint Reserved1Offset = 58;
	internal const uint Reserved2Offset = 62;
	internal const uint Reserved3Offset = 66;
	internal const uint Reserved4Offset = 70;
	internal const uint Reserved5Offset = 74;
	internal const uint Reserved6Offset = 78;
	internal const uint Reserved7Offset = 82;

	internal MuiAppMessageNodeState Message;
	internal ushort Type;
	internal uint UserData;
	internal uint Id;
	internal int NumberOfArguments;
	internal APTR ArgumentList;
	internal ushort Version;
	internal ushort Class;
	internal short MouseX;
	internal short MouseY;
	internal uint Seconds;
	internal uint Micros;
	internal uint Reserved0;
	internal uint Reserved1;
	internal uint Reserved2;
	internal uint Reserved3;
	internal uint Reserved4;
	internal uint Reserved5;
	internal uint Reserved6;
	internal uint Reserved7;
}

internal enum MuiAppMessageField : byte
{
	Type,
	UserData,
	Id,
	NumberOfArguments,
	ArgumentList,
	Version,
	Class,
	MouseX,
	MouseY,
	Seconds,
	Micros,
	Reserved0,
	Reserved1,
	Reserved2,
	Reserved3,
	Reserved4,
	Reserved5,
	Reserved6,
	Reserved7,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAppMessageFieldCursor
{
	internal APTR Record;
	internal MuiAppMessageField Field;
}

internal static class MuiAppMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAppMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address, out _);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageRecordMemoryCodec.TryReadUInt16(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageRecordMemoryCodec.TryWriteUInt16(ref platform,
			record, field, value);
	}
}

// Fixed AppMessage fields are translated through this named adapter. The
// record contains the Exec node prefix followed by mixed-width message data;
// only this bounded guest-memory layer knows those MorphOS slots.
internal static class MuiAppMessageRecordMemoryCodec
{
	private static bool TryTakeNodePrefix<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize32, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize32, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize8, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize8, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize32, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize32, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize16, out _);

	private static bool TryTakeVersionPrefix<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		TryTakeNodePrefix(ref platform, ref cursor) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize16, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize32, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize32, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize32, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize32, out _);

	private static bool TryTakeMouseXPrefix<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		TryTakeVersionPrefix(ref platform, ref cursor) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize16, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize16, out _);

	private static bool TryTakeSecondsPrefix<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		TryTakeMouseXPrefix(ref platform, ref cursor) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize16, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize16, out _);

	private static bool TryTakeReservedPrefix<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor)
		where TPlatform : struct, IMuiGuestMemory =>
		TryTakeSecondsPrefix(ref platform, ref cursor) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize32, out _) &&
		MuiGuestStructCursor.TryTake(ref platform, ref cursor,
			MuiAppMessageNodeState.FieldSize32, out _);

	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiAppMessageField field,
		out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		switch (field)
		{
			case MuiAppMessageField.Type:
				fieldSize = MuiAppMessageRecord.FieldSize16;
				return TryTakeNodePrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.UserData:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeNodePrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageRecord.FieldSize16, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Id:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeNodePrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageRecord.FieldSize16, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.NumberOfArguments:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeNodePrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageRecord.FieldSize16, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.ArgumentList:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeNodePrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						MuiAppMessageRecord.FieldSize16, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Version:
				fieldSize = MuiAppMessageRecord.FieldSize16;
				return TryTakeVersionPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Class:
				fieldSize = MuiAppMessageRecord.FieldSize16;
				return TryTakeVersionPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.MouseX:
				fieldSize = MuiAppMessageRecord.FieldSize16;
				return TryTakeMouseXPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.MouseY:
				fieldSize = MuiAppMessageRecord.FieldSize16;
				return TryTakeMouseXPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Seconds:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeSecondsPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Micros:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeSecondsPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Reserved0:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeReservedPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Reserved1:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeReservedPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Reserved2:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeReservedPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Reserved3:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeReservedPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Reserved4:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeReservedPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Reserved5:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeReservedPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Reserved6:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeReservedPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			case MuiAppMessageField.Reserved7:
				fieldSize = MuiAppMessageRecord.FieldSize32;
				return TryTakeReservedPrefix(ref platform, ref cursor) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out _) &&
					MuiGuestStructCursor.TryTake(ref platform, ref cursor,
						fieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, out APTR address,
		out uint fieldSize) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiAppMessageRecord.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address,
				out fieldSize)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAppMessageRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAppMessageField.UserData) value = state.UserData;
		else if (field == MuiAppMessageField.Id) value = state.Id;
		else if (field == MuiAppMessageField.NumberOfArguments) value =
			unchecked((uint)state.NumberOfArguments);
		else if (field == MuiAppMessageField.ArgumentList) value = state.ArgumentList.Raw;
		else if (field == MuiAppMessageField.Seconds) value = state.Seconds;
		else if (field == MuiAppMessageField.Micros) value = state.Micros;
		else if (field == MuiAppMessageField.Reserved0) value = state.Reserved0;
		else if (field == MuiAppMessageField.Reserved1) value = state.Reserved1;
		else if (field == MuiAppMessageField.Reserved2) value = state.Reserved2;
		else if (field == MuiAppMessageField.Reserved3) value = state.Reserved3;
		else if (field == MuiAppMessageField.Reserved4) value = state.Reserved4;
		else if (field == MuiAppMessageField.Reserved5) value = state.Reserved5;
		else if (field == MuiAppMessageField.Reserved6) value = state.Reserved6;
		else if (field == MuiAppMessageField.Reserved7) value = state.Reserved7;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAppMessageRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAppMessageField.UserData) state.UserData = value;
		else if (field == MuiAppMessageField.Id) state.Id = value;
		else if (field == MuiAppMessageField.NumberOfArguments) state.NumberOfArguments =
			unchecked((int)value);
		else if (field == MuiAppMessageField.ArgumentList) state.ArgumentList =
			APTR.FromPointer(value);
		else if (field == MuiAppMessageField.Seconds) state.Seconds = value;
		else if (field == MuiAppMessageField.Micros) state.Micros = value;
		else if (field == MuiAppMessageField.Reserved0) state.Reserved0 = value;
		else if (field == MuiAppMessageField.Reserved1) state.Reserved1 = value;
		else if (field == MuiAppMessageField.Reserved2) state.Reserved2 = value;
		else if (field == MuiAppMessageField.Reserved3) state.Reserved3 = value;
		else if (field == MuiAppMessageField.Reserved4) state.Reserved4 = value;
		else if (field == MuiAppMessageField.Reserved5) state.Reserved5 = value;
		else if (field == MuiAppMessageField.Reserved6) state.Reserved6 = value;
		else if (field == MuiAppMessageField.Reserved7) state.Reserved7 = value;
		else return false;
		return MuiAppMessageRecordCodec.WriteStructural(ref platform, record, state);
	}

	internal static bool TryReadUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, out ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAppMessageRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAppMessageField.Type) value = state.Type;
		else if (field == MuiAppMessageField.Version) value = state.Version;
		else if (field == MuiAppMessageField.Class) value = state.Class;
		else if (field == MuiAppMessageField.MouseX) value = unchecked((ushort)state.MouseX);
		else if (field == MuiAppMessageField.MouseY) value = unchecked((ushort)state.MouseY);
		else return false;
		return true;
	}

	internal static bool TryWriteUInt16<TPlatform>(ref TPlatform platform,
		APTR record, MuiAppMessageField field, ushort value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAppMessageRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiAppMessageField.Type) state.Type = value;
		else if (field == MuiAppMessageField.Version) state.Version = value;
		else if (field == MuiAppMessageField.Class) state.Class = value;
		else if (field == MuiAppMessageField.MouseX) state.MouseX = unchecked((short)value);
		else if (field == MuiAppMessageField.MouseY) state.MouseY = unchecked((short)value);
		else return false;
		return MuiAppMessageRecordCodec.WriteStructural(ref platform, record, state);
	}
}

internal static class MuiAppMessageNodeCodec
{
	internal static bool TryReadFields<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, out MuiAppMessageNodeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
			out var rawSuccessor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawPredecessor) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var rawType) ||
			!MuiGuestStructCursor.TryReadUInt8(ref platform, ref cursor,
				out var rawPriority) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawName) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawReplyPort) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var rawLength)) return false;
		value.Successor = APTR.FromPointer(rawSuccessor);
		value.Predecessor = APTR.FromPointer(rawPredecessor);
		value.Type = rawType;
		value.Priority = unchecked((sbyte)rawPriority);
		value.Name = APTR.FromPointer(rawName);
		value.ReplyPort = APTR.FromPointer(rawReplyPort);
		value.Length = rawLength;
		return true;
	}

	internal static bool TryWriteFields<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiAppMessageNodeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Successor.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Predecessor.Raw) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				value.Type) &&
			MuiGuestStructCursor.TryWriteUInt8(ref platform, ref cursor,
				unchecked((byte)value.Priority)) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Name.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ReplyPort.Raw) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				value.Length);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAppMessageNodeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAppMessageNodeState.Size, out var cursor) ||
			!TryReadFields(ref platform, ref cursor, out value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAppMessageNodeState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAppMessageNodeState.Size, out var cursor) ||
			!TryWriteFields(ref platform, ref cursor, value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAppMessageNodeState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAppMessageNodeState value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWorkbenchArgumentRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint LockOffset = 0;
	internal const uint NameOffset = 4;

	internal BPTR Lock;
	internal STRPTR Name;
}

internal enum MuiWorkbenchArgumentField : byte
{
	Lock,
	Name,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWorkbenchArgumentFieldCursor
{
	internal APTR Record;
	internal MuiWorkbenchArgumentField Field;
}

internal static class MuiWorkbenchArgumentFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiWorkbenchArgumentFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiWorkbenchArgumentRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiWorkbenchArgumentField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiWorkbenchArgumentRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiWorkbenchArgumentField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiWorkbenchArgumentRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Workbench argument records are exposed as named BPTR/STRPTR fields.
// The bounded cursor walks the complete two-slot struct; vector and message
// consumers use the semantic record rather than selecting numeric offsets.
internal static class MuiWorkbenchArgumentRecordMemoryCodec
{
	private static bool TryTakeField<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiWorkbenchArgumentField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		switch (field)
		{
			case MuiWorkbenchArgumentField.Lock:
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiWorkbenchArgumentRecord.FieldSize, out address);
			case MuiWorkbenchArgumentField.Name:
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiWorkbenchArgumentRecord.FieldSize, out _)) return false;
				return MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiWorkbenchArgumentRecord.FieldSize, out address);
			default:
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiWorkbenchArgumentField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!MuiGuestStructCursor.TryCreate(ref platform, record,
			MuiWorkbenchArgumentRecord.Size, out var cursor) ||
			!TryTakeField(ref platform, ref cursor, field, out address)) return false;
		return true;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWorkbenchArgumentField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiWorkbenchArgumentRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWorkbenchArgumentField.Lock) value = state.Lock.Raw;
		else if (field == MuiWorkbenchArgumentField.Name) value = state.Name.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiWorkbenchArgumentField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWorkbenchArgumentRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiWorkbenchArgumentField.Lock) state.Lock = BPTR.FromRaw(value);
		else if (field == MuiWorkbenchArgumentField.Name) state.Name =
			STRPTR.FromPointer(value);
		else return false;
		return MuiWorkbenchArgumentRecordCodec.WriteStructural(ref platform,
			record, state);
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiWorkbenchArgumentVectorCursor
{
	internal const uint EntrySize = MuiWorkbenchArgumentRecord.Size;
	internal const uint MaximumEntries = 65535;
	internal APTR Base;
	internal uint Index;
}

// Struct-first guest-memory adapter for the caller-owned AppMessage argument
// vector. Each named argument record is exposed only within the MorphOS
// argument-count bound and after complete record-range validation.
internal static class MuiWorkbenchArgumentVectorMemoryCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (vector.IsNull || index >=
			MuiWorkbenchArgumentVectorCursor.MaximumEntries || index >
			(uint.MaxValue - vector.Raw) /
			MuiWorkbenchArgumentRecord.Size) return false;
		var offset = index * MuiWorkbenchArgumentRecord.Size;
		if (vector.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(vector.Raw + offset);
		return platform.IsMapped(address, MuiWorkbenchArgumentRecord.Size);
	}
}

internal static class MuiWorkbenchArgumentVectorCodec
{
	internal static bool TryAdvance(ref MuiWorkbenchArgumentVectorCursor cursor,
		uint items)
	{
		if (items == 0 || cursor.Index > uint.MaxValue - items)
			return false;
		var next = cursor.Index + items;
		if (next > MuiWorkbenchArgumentVectorCursor.MaximumEntries) return false;
		cursor.Index = next;
		return true;
	}

	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiWorkbenchArgumentVectorCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiWorkbenchArgumentVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		MuiWorkbenchArgumentVectorCursor cursor,
		out MuiWorkbenchArgumentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!TryGetEntry(ref platform, cursor, out var address) ||
			!MuiWorkbenchArgumentRecordCodec.TryRead(ref platform, address,
				out value))
		{
			value = default;
			return false;
		}
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		MuiWorkbenchArgumentVectorCursor cursor,
		MuiWorkbenchArgumentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetEntry(ref platform, cursor, out var address)) return false;
		return MuiWorkbenchArgumentRecordCodec.Write(ref platform, address, value);
	}

	// Complete named-record bridge for indexed consumers. The memory adapter
	// remains the sole owner of slot arithmetic and range validation; callers
	// receive the typed Workbench argument record rather than a guest address.
	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, out MuiWorkbenchArgumentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiWorkbenchArgumentVectorMemoryCodec.TryGetEntry(ref platform,
			vector, index, out var address) ||
			!MuiWorkbenchArgumentRecordCodec.TryRead(ref platform, address,
				out value))
		{
			value = default;
			return false;
		}
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR vector, uint index, MuiWorkbenchArgumentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiWorkbenchArgumentVectorMemoryCodec.TryGetEntry(ref platform,
			vector, index, out var address)) return false;
		return MuiWorkbenchArgumentRecordCodec.Write(ref platform, address, value);
	}
}

internal static class MuiWorkbenchArgumentRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiWorkbenchArgumentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWorkbenchArgumentRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawLock) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawName) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;
		value.Lock = BPTR.FromRaw(rawLock);
		value.Name = STRPTR.FromPointer(rawName);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiWorkbenchArgumentRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiWorkbenchArgumentRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Lock.Raw) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Name.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiWorkbenchArgumentRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiWorkbenchArgumentRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);
}

internal static class MuiAppMessageRecordCodec
{
	internal static bool TryReadFields<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, out MuiAppMessageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiAppMessageNodeCodec.TryReadFields(ref platform, ref cursor,
			out var messageValue) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var rawType) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawUserData) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawArguments) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawArgumentList) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var rawVersion) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var rawClass) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var rawMouseX) ||
			!MuiGuestStructCursor.TryReadUInt16(ref platform, ref cursor,
				out var rawMouseY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawSeconds) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawMicros) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawReserved0) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawReserved1) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawReserved2) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawReserved3) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawReserved4) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawReserved5) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawReserved6) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var rawReserved7)) return false;
		value.Message = messageValue;
		value.Type = rawType;
		value.UserData = rawUserData;
		value.Id = rawId;
		value.NumberOfArguments = unchecked((int)rawArguments);
		value.ArgumentList = APTR.FromPointer(rawArgumentList);
		value.Version = rawVersion;
		value.Class = rawClass;
		value.MouseX = unchecked((short)rawMouseX);
		value.MouseY = unchecked((short)rawMouseY);
		value.Seconds = rawSeconds;
		value.Micros = rawMicros;
		value.Reserved0 = rawReserved0;
		value.Reserved1 = rawReserved1;
		value.Reserved2 = rawReserved2;
		value.Reserved3 = rawReserved3;
		value.Reserved4 = rawReserved4;
		value.Reserved5 = rawReserved5;
		value.Reserved6 = rawReserved6;
		value.Reserved7 = rawReserved7;
		return true;
	}

	internal static bool TryWriteFields<TPlatform>(ref TPlatform platform,
		ref MuiGuestStructCursor cursor, MuiAppMessageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAppMessageNodeCodec.TryWriteFields(ref platform, ref cursor,
			value.Message) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				value.Type) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.UserData) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Id) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.NumberOfArguments)) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ArgumentList.Raw) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				value.Version) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				value.Class) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.MouseX)) &&
			MuiGuestStructCursor.TryWriteUInt16(ref platform, ref cursor,
				unchecked((ushort)value.MouseY)) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Seconds) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Micros) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved0) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved1) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved2) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved3) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved4) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved5) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved6) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved7);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAppMessageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAppMessageRecord.Size, out var cursor) ||
			!TryReadFields(ref platform, ref cursor, out value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAppMessageRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAppMessageRecord.Size, out var cursor) ||
			!TryWriteFields(ref platform, ref cursor, value)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAppMessageRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryRead(ref platform, address, out value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiAppMessageRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		Write(ref platform, address, value);

}

public static class MuiApplicationMessageCore
{
	public const uint ApplicationObject = 0x8042D3EE;
	public const uint AppMessage = 0x80421955;
	public const uint WindowAppWindow = 0x804280CF;

	// These attributes are owned by the application/message routing record even
	// when the object is a custom or otherwise unknown MUI class. Keep the
	// admission predicate next to the typed getter so direct Get and generic
	// OM_GET cannot drift apart.
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == ApplicationObject || attribute == AppMessage ||
		attribute == WindowAppWindow;

	private const uint ApplicationInitialized = 0x7FFE0044;
	private const uint WindowOpen = 0x80428AA0;
	internal const uint RoutingStateKey = 0x7F0A001Au;
	private const int MaximumArguments = 65535;
	private const uint MaximumStringLength = 65536;

	internal static bool TryGetApplicationMessageRoutingState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RoutingStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			RoutingStateKey) != unchecked((int)
			MuiApplicationMessageRoutingStateRecord.Size)) return false;
		return MuiApplicationMessageRoutingStateRecordCodec.TryReadStructural(
			ref platform, block, out value) &&
			MuiApplicationMessageRoutingStateAdmission.ValidateLive(ref platform,
				state, obj, value);
	}

	private static MuiApplicationMessageRoutingStateRecord ReadRoutingState<
		TPlatform>(ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PublishRoutingState(ref platform, state, obj, out var value))
			return value;
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			RoutingStateKey) != 0) return default;
		value = default;
		value.Magic = MuiApplicationMessageRoutingStateRecord.Cookie;
		FillRoutingState(ref platform, state, obj, ref value);
		return value;
	}

	private static bool PublishRoutingState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj,
		out MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RoutingStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			RoutingStateKey);
		if (length != 0 && length != unchecked((int)
			MuiApplicationMessageRoutingStateRecord.Size)) return false;
		if (length != 0)
		{
			if (!MuiApplicationMessageRoutingStateRecordCodec.TryReadStructural(
				ref platform, block, out value) ||
				!MuiApplicationMessageRoutingStateAdmission.ValidateLive(ref platform,
					state, obj, value)) return false;
			FillRoutingState(ref platform, state, obj, ref value);
			if (!MuiApplicationMessageRoutingStateAdmission.ValidateLive(ref platform,
				state, obj, value)) return false;
			return MuiApplicationMessageRoutingStateRecordCodec.Write(ref platform,
				block, value);
		}

		value = default;
		value.Magic = MuiApplicationMessageRoutingStateRecord.Cookie;
		FillRoutingState(ref platform, state, obj, ref value);
		if (!MuiApplicationMessageRoutingStateAdmission.ValidateLive(ref platform,
			state, obj, value)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationMessageRoutingStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationMessageRoutingStateRecord.Size);
		var written = MuiApplicationMessageRoutingStateRecordCodec.Write(
			ref platform, scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			RoutingStateKey, scratch,
			unchecked((int)MuiApplicationMessageRoutingStateRecord.Size));
		platform.Clear(scratch, MuiApplicationMessageRoutingStateRecord.Size);
		platform.Free(scratch, MuiApplicationMessageRoutingStateRecord.Size);
		return added;
	}

	private static void FillRoutingState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj,
		ref MuiApplicationMessageRoutingStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			AppMessage, out var appMessage)) appMessage = 0;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			WindowAppWindow, out var appWindow)) appWindow = 0;
		value.AppMessage = APTR.FromPointer(appMessage);
		value.WindowAppWindow = appWindow == 0 ? 0u : 1u;
	}

	private static bool SetRoutingAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var obj = objectValue.Boopsi;
		if (obj.IsNull) return false;
		if (!PublishRoutingState(ref platform, state, obj, out _)) return false;
		var previous = 0u;
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			attribute, out previous);
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, attribute, value, notify)) return false;
		if (PublishRoutingState(ref platform, state, obj, out _)) return true;
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			attribute, previous, notify);
		return false;
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = IsPublicGetterAttribute(attribute);
		if (!handled) return false;
		if (attribute == WindowAppWindow)
		{
			if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
				out var objectValue)) return false;
			return SetWindowAppWindowValue(ref platform, state, objectValue.Boopsi,
				value);
		}
		// ApplicationObject and AppMessage are getter-only. AppMessage is
		// changed only by PublishAppMessage while a notification is executing.
		return false;
	}

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = IsPublicGetterAttribute(attribute);
		if (!handled) return false;
		if (attribute == ApplicationObject)
		{
			value = FindApplication(ref platform, state, obj).Raw;
			return true;
		}
		if (attribute == AppMessage || attribute == WindowAppWindow)
		{
			var routing = ReadRoutingState(ref platform, state, obj);
			value = attribute == AppMessage ? routing.AppMessage.Raw :
				routing.WindowAppWindow;
			return true;
		}
		return true;
	}

	public static bool SetWindowAppWindowValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR window, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsWindowObject(ref platform, state, window)) return false;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, window,
			WindowOpen, out var open) && open != 0) return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, window);
		return SetRoutingAttribute(ref platform, state, record, WindowAppWindow,
			value == 0 ? 0u : 1u, false);
	}

	// Publish an AppMessage to a target object in an AppWindow subtree. The
	// pointer is valid only during synchronous notification dispatch; the
	// previous transient value is restored afterwards without a managed copy.
	public static bool PublishAppMessage<TPlatform>(ref TPlatform platform,
		APTR state, APTR target, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsAppWindowTarget(ref platform, state, target) ||
			!ValidateMessage(ref platform, message)) return false;
		return PublishToTarget(ref platform, state, target, message);
	}

	// App icon drops use the application's caller-owned DropObject target even
	// though that object is not itself a Window.mui instance.
	public static bool PublishApplicationDropMessage<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			application, 0x8042A07F, out var iconified) || iconified == 0 ||
			!MuiHeadlessObjectCore.GetAttribute(ref platform, state, application,
				0x80421266, out var target) || target == 0 ||
			!ValidateMessage(ref platform, message)) return false;
		return PublishToTarget(ref platform, state, APTR.FromPointer(target),
			message);
	}

	internal static bool ValidateMessage<TPlatform>(ref TPlatform platform,
		APTR message) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAppMessageRecordCodec.TryRead(ref platform, message,
			out var value) || value.NumberOfArguments < 0 ||
			value.NumberOfArguments > MaximumArguments) return false;
		if (value.NumberOfArguments == 0) return true;
		if (value.ArgumentList.IsNull ||
			(uint)value.NumberOfArguments > uint.MaxValue /
			MuiWorkbenchArgumentRecord.Size) return false;
		var bytes = (uint)value.NumberOfArguments *
			MuiWorkbenchArgumentRecord.Size;
		if (!platform.IsMapped(value.ArgumentList, bytes)) return false;
		var argumentCursor = default(MuiWorkbenchArgumentVectorCursor);
		argumentCursor.Base = value.ArgumentList;
		argumentCursor.Index = 0;
		for (var index = 0u; index < (uint)value.NumberOfArguments; index++)
		{
			if (!MuiWorkbenchArgumentVectorCodec.TryRead(ref platform,
				argumentCursor, out var argument)) return false;
			var name = APTR.FromPointer(argument.Name.Raw);
			if (name.IsNotNull && !CStringCodec.TryReadLength(ref platform, name,
				MaximumStringLength, out _)) return false;
			if (index + 1 < (uint)value.NumberOfArguments &&
				!MuiWorkbenchArgumentVectorCodec.TryAdvance(ref argumentCursor, 1))
				return false;
		}
		return true;
	}

	private static bool PublishToTarget<TPlatform>(ref TPlatform platform,
		APTR state, APTR target, APTR message)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, target);
		if (record.IsNull) return false;
		// Admit the existing transient sidecar before touching the caller-visible
		// AppMessage attribute or firing its notification chain.
		if (!PublishRoutingState(ref platform, state, target, out _)) return false;
		var hadPrevious = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, target, AppMessage, out var previous);
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, AppMessage, message.Raw, true)) return false;
		if (!PublishRoutingState(ref platform, state, target, out _))
		{
			MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
				AppMessage, hadPrevious ? previous : 0, false);
			PublishRoutingState(ref platform, state, target, out _);
			return false;
		}
		var restored = MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform,
			state, record, AppMessage, hadPrevious ? previous : 0, false);
		return restored && PublishRoutingState(ref platform, state, target, out _);
	}

	private static bool IsAppWindowTarget<TPlatform>(ref TPlatform platform,
		APTR state, APTR target) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = target;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, current,
				WindowAppWindow, out var enabled) && enabled != 0) return true;
			current = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
		}
		return false;
	}

	private static APTR FindApplication<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = obj;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, current,
				ApplicationInitialized, out var initialized) && initialized != 0)
				return current;
			current = MuiHeadlessObjectCore.ParentObject(ref platform, state,
				current);
		}
		return APTR.Null;
	}

	internal static bool IsWindowObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue) || !MuiHeadlessClassCodec.TryRead(ref platform,
			objectValue.Class, out var classValue)) return false;
		var name = classValue.Name;
		if (!MuiWindowClassNameRecordCodec.TryReadRecord(ref platform, name,
			out var nameValue)) return false;
		return nameValue.Word0 == 0x57696E64 && // Wind
			nameValue.Word1 == 0x6F772E6D && // ow.m
			nameValue.Character0 == (byte)'u' &&
			nameValue.Character1 == (byte)'i' &&
			nameValue.Terminator == 0;
	}
}
