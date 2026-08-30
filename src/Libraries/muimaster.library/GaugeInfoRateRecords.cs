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
	internal const uint MagicOffset = 0;
	internal const uint InfoRateOffset = 4;
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
		MuiGaugeInfoRateStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

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

// The only layer that translates the fixed guest record into addresses.  All
// Gauge.InfoRate consumers operate on the named record or semantic value.
internal static class MuiGaugeInfoRateStateRecordMemoryCodec
{
	private static bool TryResolve(MuiGaugeInfoRateStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiGaugeInfoRateStateField.Magic:
				offset = MuiGaugeInfoRateStateRecord.MagicOffset;
				return true;
			case MuiGaugeInfoRateStateField.InfoRate:
				offset = MuiGaugeInfoRateStateRecord.InfoRateOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoRateStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiGaugeInfoRateStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiGaugeInfoRateStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoRateStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoRateStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
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
