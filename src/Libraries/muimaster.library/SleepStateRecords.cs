/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared fixed-width sleep state. Window owners use SavedDisabled to restore
// their prior disabled value; application owners leave that field zero.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSleepStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint DepthOffset = 4;
	internal const uint SavedDisabledOffset = 8;
	internal const uint RequestOffset = 12;
	internal const uint Cookie = 0x534C5053u; // 'SLPS'

	internal uint Magic;
	internal uint Depth;
	internal uint SavedDisabled;
	internal uint Request;
}

// Sleep depth and the public request are one nesting counter in the MorphOS
// projection. SavedDisabled is a canonical ULONG BOOL; the structural codec
// remains available for inspecting malformed guest storage without allowing
// it into sleep-sensitive consumers.
internal static class MuiSleepStateAdmission
{
	internal static bool Validate(MuiSleepStateRecord value) =>
		value.Magic == MuiSleepStateRecord.Cookie &&
		value.SavedDisabled <= 1 && value.Request == value.Depth;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiSleepStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !owner.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, owner).IsNull;
}

internal enum MuiSleepStateField : byte
{
	Magic,
	Depth,
	SavedDisabled,
	Request,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSleepStateFieldCursor
{
	internal APTR Record;
	internal MuiSleepStateField Field;
}

internal static class MuiSleepStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSleepStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSleepStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiSleepStateRecordMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform, record,
			field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform, record,
			field, value);
	}
}

// Fixed sleep state is transferred as a named record. Numeric guest positions
// are confined to this ABI adapter; the compatibility cursor above remains
// available only to legacy callers and malformed-state diagnostics.
internal static class MuiSleepStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiSleepStateField field,
		out uint index)
	{
		if (field == MuiSleepStateField.Magic)
			index = 0;
		else if (field == MuiSleepStateField.Depth)
			index = 1;
		else if (field == MuiSleepStateField.SavedDisabled)
			index = 2;
		else if (field == MuiSleepStateField.Request)
			index = 3;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, out APTR address)
	where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiSleepStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSleepStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiSleepStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiSleepStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiSleepStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiSleepStateRecordCodec.TryReadStructural(ref platform, record,
			out var state))
			return false;
		if (field == MuiSleepStateField.Magic)
			value = state.Magic;
		else if (field == MuiSleepStateField.Depth)
			value = state.Depth;
		else if (field == MuiSleepStateField.SavedDisabled)
			value = state.SavedDisabled;
		else if (field == MuiSleepStateField.Request)
			value = state.Request;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiSleepStateRecordCodec.TryReadStructural(ref platform, record,
			out var state))
			return false;
		if (field == MuiSleepStateField.Magic)
			state.Magic = value;
		else if (field == MuiSleepStateField.Depth)
			state.Depth = value;
		else if (field == MuiSleepStateField.SavedDisabled)
			state.SavedDisabled = value;
		else if (field == MuiSleepStateField.Request)
			state.Request = value;
		else return false;
		return MuiSleepStateRecordCodec.WriteRecord(ref platform, record, state);
	}
}

internal static class MuiSleepStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiSleepStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSleepStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Depth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.SavedDisabled) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Request)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiSleepStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSleepStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Depth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.SavedDisabled) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Request) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiSleepStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSleepStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiSleepStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSleepStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiSleepStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
