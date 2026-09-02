/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// OpenConfigWindow request state. Flags retain the MorphOS ULONG payload while
// ClassId remains a caller-owned validated guest C-string pointer.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationConfigWindowStateRecord
{
	internal const uint Size = 20;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint FlagsOffset = 4;
	internal const uint ClassIdOffset = 8;
	internal const uint RequestsOffset = 12;
	internal const uint ReservedOffset = 16;
	internal const uint Cookie = 0x41435754u; // 'ACWT'

	internal uint Magic;
	internal uint Flags;
	internal APTR ClassId;
	internal uint Requests;
	internal uint Reserved;
}

// OpenConfigWindow retains raw MorphOS flags and a caller-owned class-id
// string. The admission layer validates only the guest pointer contract and
// live Application owner; flags and request counts remain full ULONGs.
internal static class MuiApplicationConfigWindowStateAdmission
{
	private const uint MaximumStringLength = 65536;

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationConfigWindowStateRecord.Cookie &&
		(value.ClassId.IsNull || CStringCodec.TryReadLength(ref platform,
			value.ClassId, MaximumStringLength, out _));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application,
		MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;
}

internal enum MuiApplicationConfigWindowStateField : byte
{
	Magic,
	Flags,
	ClassId,
	Requests,
	Reserved,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationConfigWindowStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationConfigWindowStateField Field;
}

internal static class MuiApplicationConfigWindowStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationConfigWindowStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationConfigWindowStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationConfigWindowStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationConfigWindowStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationConfigWindowStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationConfigWindowStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed application config-window state is read and written as a named value.
// Keep packed guest positions in this bounded ABI adapter; production
// consumers do not select numeric slots directly.
internal static class MuiApplicationConfigWindowStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationConfigWindowStateField field,
		out uint offset)
	{
		if (field == MuiApplicationConfigWindowStateField.Magic)
			offset = MuiApplicationConfigWindowStateRecord.MagicOffset;
		else if (field == MuiApplicationConfigWindowStateField.Flags)
			offset = MuiApplicationConfigWindowStateRecord.FlagsOffset;
		else if (field == MuiApplicationConfigWindowStateField.ClassId)
			offset = MuiApplicationConfigWindowStateRecord.ClassIdOffset;
		else if (field == MuiApplicationConfigWindowStateField.Requests)
			offset = MuiApplicationConfigWindowStateRecord.RequestsOffset;
		else if (field == MuiApplicationConfigWindowStateField.Reserved)
			offset = MuiApplicationConfigWindowStateRecord.ReservedOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationConfigWindowStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record,
			MuiApplicationConfigWindowStateRecord.Size) &&
			platform.IsMapped(address,
				MuiApplicationConfigWindowStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationConfigWindowStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationConfigWindowStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationConfigWindowStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationConfigWindowStateField.Flags)
			value = state.Flags;
		else if (field == MuiApplicationConfigWindowStateField.ClassId)
			value = state.ClassId.Raw;
		else if (field == MuiApplicationConfigWindowStateField.Requests)
			value = state.Requests;
		else if (field == MuiApplicationConfigWindowStateField.Reserved)
			value = state.Reserved;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationConfigWindowStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationConfigWindowStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationConfigWindowStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationConfigWindowStateField.Flags)
			state.Flags = value;
		else if (field == MuiApplicationConfigWindowStateField.ClassId)
			state.ClassId = APTR.FromPointer(value);
		else if (field == MuiApplicationConfigWindowStateField.Requests)
			state.Requests = value;
		else if (field == MuiApplicationConfigWindowStateField.Reserved)
			state.Reserved = value;
		else return false;
		return MuiApplicationConfigWindowStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationConfigWindowStateRecordCodec
{
	// OpenConfigWindow state is a fixed five-ULONG record. Exchange the
	// declaration-ordered fields as one named struct; class-id pointer validity
	// remains the responsibility of the admission layer below.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationConfigWindowStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var classId) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Requests) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Reserved)) return false;
		value.ClassId = APTR.FromPointer(classId);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationConfigWindowStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ClassId.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Requests) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Reserved) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationConfigWindowStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationConfigWindowStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationConfigWindowStateRecord.Size) ||
			!MuiApplicationConfigWindowStateAdmission.Validate(ref platform, value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
