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

internal static class MuiGaugeInfoTextStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiGaugeInfoTextStateRecord.Size) ||
			!MuiGaugeInfoTextStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiGaugeInfoTextStateField.Magic, out var magic) ||
			magic != MuiGaugeInfoTextStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiGaugeInfoTextStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiGaugeInfoTextStateField.InfoText, out var infoText))
			return false;
		value.InfoText = APTR.FromPointer(infoText);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGaugeInfoTextStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiGaugeInfoTextStateRecord.Size) || value.Magic !=
			MuiGaugeInfoTextStateRecord.Cookie) return false;
		return MuiGaugeInfoTextStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiGaugeInfoTextStateField.Magic, value.Magic) &&
			MuiGaugeInfoTextStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiGaugeInfoTextStateField.InfoText, value.InfoText.Raw);
	}
}
