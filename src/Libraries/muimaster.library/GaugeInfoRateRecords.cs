/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the MorphOS Gauge.InfoRate LONG.  MorphOS leaves
// the meaning of this V4 attribute undocumented; retaining its signed LONG
// representation is nevertheless important for ABI-compatible Get/Set.
public struct MuiGaugeInfoRateState
{
	public int InfoRate;
}

// The state is kept separately from MuiGaugeState so the existing gauge
// progress record remains ABI-stable.  The complete record is guest-resident
// and is exchanged only through the bounded codec below.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGaugeInfoRateStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint Cookie = 0x4D474952u; // 'MGIR'

	internal uint Magic;
	internal int InfoRate;
}

internal enum MuiGaugeInfoRateStateField : byte
{
	Magic,
	InfoRate,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGaugeInfoRateStateFieldCursor
{
	internal APTR Record;
	internal MuiGaugeInfoRateStateField Field;
}

internal static class MuiGaugeInfoRateStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGaugeInfoRateStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGaugeInfoRateStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGaugeInfoRateStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoRateStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGaugeInfoRateStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoRateStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGaugeInfoRateStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

// The bounded cursor walks the complete packed record before selecting a field.
// All Gauge.InfoRate consumers operate on the named record or semantic value.
internal static class MuiGaugeInfoRateStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiGaugeInfoRateStateField field,
		out uint index)
	{
		if (field == MuiGaugeInfoRateStateField.Magic)
			index = 0;
		else if (field == MuiGaugeInfoRateStateField.InfoRate)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoRateStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGaugeInfoRateStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGaugeInfoRateStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiGaugeInfoRateStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiGaugeInfoRateStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiGaugeInfoRateStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoRateStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGaugeInfoRateStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGaugeInfoRateStateField.Magic)
			value = state.Magic;
		else if (field == MuiGaugeInfoRateStateField.InfoRate)
			value = unchecked((uint)state.InfoRate);
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoRateStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGaugeInfoRateStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGaugeInfoRateStateField.Magic)
			state.Magic = value;
		else if (field == MuiGaugeInfoRateStateField.InfoRate)
			state.InfoRate = unchecked((int)value);
		else return false;
		return MuiGaugeInfoRateStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiGaugeInfoRateStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGaugeInfoRateStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGaugeInfoRateStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var infoRate)) return false;
		value.InfoRate = unchecked((int)infoRate);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGaugeInfoRateStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGaugeInfoRateStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.InfoRate)) &&
			MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGaugeInfoRateStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGaugeInfoRateStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiGaugeInfoRateStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGaugeInfoRateStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGaugeInfoRateStateAdmission.Validate(value) &&
		WriteRecord(ref platform, address, value);

}

internal static class MuiGaugeInfoRateStateAdmission
{
	internal static bool Validate(MuiGaugeInfoRateStateRecord value) =>
		value.Magic == MuiGaugeInfoRateStateRecord.Cookie;

	internal static bool Validate(MuiGaugeInfoRateState value) => true;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiGaugeInfoRateStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
