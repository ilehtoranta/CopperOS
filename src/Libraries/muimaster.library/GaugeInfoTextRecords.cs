/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the object-owned Gauge.mui InfoText format.
public struct MuiGaugeInfoTextState
{
	public APTR InfoText;
}

// Guest-resident Gauge InfoText state.  The bounded copy is retained in the
// object's GaugeInfoTextKey Dataspace entry.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGaugeInfoTextStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint InfoTextOffset = 4;
	internal const uint Cookie = 0x4D474954u; // 'MGIT'

	internal uint Magic;
	internal APTR InfoText;
}

internal static class MuiGaugeInfoTextStateAdmission
{
	internal const int MaximumLength = 256;

	internal static bool Validate(MuiGaugeInfoTextStateRecord value) =>
		value.Magic == MuiGaugeInfoTextStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.InfoText.IsNull || CStringCodec.TryReadLength(ref platform,
			value.InfoText, MaximumLength, out _);
	}
}

internal enum MuiGaugeInfoTextStateField : byte
{
	Magic,
	InfoText,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGaugeInfoTextStateFieldCursor
{
	internal APTR Record;
	internal MuiGaugeInfoTextStateField Field;
}

internal static class MuiGaugeInfoTextStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGaugeInfoTextStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGaugeInfoTextStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoTextStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGaugeInfoTextStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoTextStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGaugeInfoTextStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

// Struct-first guest-memory adapter. Gauge consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiGaugeInfoTextStateRecordMemoryCodec
{
	private static bool TryResolve(MuiGaugeInfoTextStateField field,
		out uint offset)
	{
		if (field == MuiGaugeInfoTextStateField.Magic)
			offset = MuiGaugeInfoTextStateRecord.MagicOffset;
		else if (field == MuiGaugeInfoTextStateField.InfoText)
			offset = MuiGaugeInfoTextStateRecord.InfoTextOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoTextStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiGaugeInfoTextStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiGaugeInfoTextStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoTextStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGaugeInfoTextStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGaugeInfoTextStateField.Magic)
			value = state.Magic;
		else if (field == MuiGaugeInfoTextStateField.InfoText)
			value = state.InfoText.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoTextStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGaugeInfoTextStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiGaugeInfoTextStateField.Magic)
			state.Magic = value;
		else if (field == MuiGaugeInfoTextStateField.InfoText)
			state.InfoText = APTR.FromPointer(value);
		else return false;
		return MuiGaugeInfoTextStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	// Legacy raw-offset adapter retained for bounded diagnostics and older
	// callers. Typed field access above is the preferred API.
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiGaugeInfoTextStateRecord.Size -
			MuiGaugeInfoTextStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiGaugeInfoTextStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiGaugeInfoTextStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiGaugeInfoTextStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGaugeInfoTextStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var infoText)) return false;
		value.InfoText = APTR.FromPointer(infoText);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGaugeInfoTextStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.InfoText.Raw) &&
			MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiGaugeInfoTextStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGaugeInfoTextStateAdmission.Validate(value) &&
		WriteRecord(ref platform, address, value);
}
