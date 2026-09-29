/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The Pop* and Filepanel specialists share a class-owned CallHookPkt scratch
// message. Keep the complete wire record named so callers can update the
// semantic fields without treating the message as an anonymous offset tuple.
// The final ULONG is reserved for forward-compatible MorphOS hook data and is
// preserved by read/modify/write callers.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSpecialistHookMessage
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MethodIdOffset = 0;
	internal const uint Param1Offset = 4;
	internal const uint Param2Offset = 8;
	internal const uint ReservedOffset = 12;

	internal uint MethodId;
	internal uint Param1;
	internal uint Param2;
	internal uint Reserved;
}

internal enum MuiSpecialistHookMessageField : byte
{
	MethodId,
	Param1,
	Param2,
	Reserved,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSpecialistHookMessageFieldCursor
{
	internal APTR Record;
	internal MuiSpecialistHookMessageField Field;
}

// Struct-first guest-memory adapter for the shared specialist hook packet.
// All guest wire positions are confined to this resolver; consumers operate
// on MuiSpecialistHookMessage fields and never issue packet-relative writes.
internal static class MuiSpecialistHookMessageRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiSpecialistHookMessageField field,
		out uint index)
	{
		if (field == MuiSpecialistHookMessageField.MethodId)
			index = 0;
		else if (field == MuiSpecialistHookMessageField.Param1)
			index = 1;
		else if (field == MuiSpecialistHookMessageField.Param2)
			index = 2;
		else if (field == MuiSpecialistHookMessageField.Reserved)
			index = 3;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiSpecialistHookMessageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiSpecialistHookMessageFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSpecialistHookMessageFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiSpecialistHookMessage.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiSpecialistHookMessage.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiSpecialistHookMessage.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSpecialistHookMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiSpecialistHookMessageCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiSpecialistHookMessageField.MethodId)
			value = state.MethodId;
		else if (field == MuiSpecialistHookMessageField.Param1)
			value = state.Param1;
		else if (field == MuiSpecialistHookMessageField.Param2)
			value = state.Param2;
		else if (field == MuiSpecialistHookMessageField.Reserved)
			value = state.Reserved;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSpecialistHookMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiSpecialistHookMessageCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiSpecialistHookMessageField.MethodId)
			state.MethodId = value;
		else if (field == MuiSpecialistHookMessageField.Param1)
			state.Param1 = value;
		else if (field == MuiSpecialistHookMessageField.Param2)
			state.Param2 = value;
		else if (field == MuiSpecialistHookMessageField.Reserved)
			state.Reserved = value;
		else return false;
		return MuiSpecialistHookMessageCodec.WriteStructural(ref platform, record,
			state);
	}
}

internal static class MuiSpecialistHookMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSpecialistHookMessageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSpecialistHookMessageFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiSpecialistHookMessageRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSpecialistHookMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiSpecialistHookMessageRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSpecialistHookMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiSpecialistHookMessageRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

internal static class MuiSpecialistHookMessageCodec
{
	// Declaration-order hook packet: MethodId, Param1, Param2, Reserved.
	// Keep this small fixed message on the named cursor so specialist callers
	// never depend on packet-relative offsets.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiSpecialistHookMessage value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSpecialistHookMessage.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MethodId) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Param1) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Param2) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Reserved) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiSpecialistHookMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address, MuiSpecialistHookMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSpecialistHookMessage.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MethodId) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Param1) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Param2) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Reserved) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiSpecialistHookMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSpecialistHookMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSpecialistHookMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
