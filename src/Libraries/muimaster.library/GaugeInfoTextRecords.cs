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
	private static bool TryResolve(MuiGaugeInfoTextStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiGaugeInfoTextStateField.Magic => 0,
			MuiGaugeInfoTextStateField.InfoText => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGaugeInfoTextStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiGaugeInfoTextStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoTextStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiGaugeInfoTextStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeInfoTextStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGaugeInfoTextStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Gauge consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiGaugeInfoTextStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiGaugeInfoTextStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiGaugeInfoTextStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
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
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGaugeInfoTextStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiGaugeInfoTextStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var infoText)) return false;
		value.InfoText = APTR.FromPointer(infoText);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiGaugeInfoTextStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGaugeInfoTextStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGaugeInfoTextStateAdmission.Validate(value)) return false;
		return MuiGaugeInfoTextStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, 0, value.Magic) &&
			MuiGaugeInfoTextStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.InfoText.Raw);
	}
}
