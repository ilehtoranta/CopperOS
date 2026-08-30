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
	private static bool TryResolve(MuiSpecialistHookMessageField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiSpecialistHookMessageField.MethodId:
				offset = MuiSpecialistHookMessage.MethodIdOffset;
				return true;
			case MuiSpecialistHookMessageField.Param1:
				offset = MuiSpecialistHookMessage.Param1Offset;
				return true;
			case MuiSpecialistHookMessageField.Param2:
				offset = MuiSpecialistHookMessage.Param2Offset;
				return true;
			case MuiSpecialistHookMessageField.Reserved:
				offset = MuiSpecialistHookMessage.ReservedOffset;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiSpecialistHookMessageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, MuiSpecialistHookMessage.Size))
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiSpecialistHookMessage.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSpecialistHookMessageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSpecialistHookMessageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiSpecialistHookMessageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSpecialistHookMessageFieldCursor cursor, out APTR address)
	where TPlatform : struct, IMuiGuestMemory =>
		MuiSpecialistHookMessageRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

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

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSpecialistHookMessage value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSpecialistHookMessage value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
