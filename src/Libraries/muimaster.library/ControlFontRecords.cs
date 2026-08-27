/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the optional common-control TextFont pointer.
public struct MuiControlFontState
{
	public bool Present;
	public APTR Font;
}

// Guest-resident shared Font state. Presence is kept separate so a missing
// Font attribute remains distinguishable from a present NULL pointer.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiControlFontStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D43464Eu; // 'MCFN'

	internal uint Magic;
	internal uint Present;
	internal APTR Font;
}

internal static class MuiControlFontStateAdmission
{
	internal static bool Validate(MuiControlFontStateRecord value) =>
		value.Magic == MuiControlFontStateRecord.Cookie && value.Present <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiControlFontStateField : byte
{
	Magic,
	Present,
	Font,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiControlFontStateFieldCursor
{
	internal APTR Record;
	internal MuiControlFontStateField Field;
}

internal static class MuiControlFontStateFieldCursorCodec
{
	private static bool TryResolve(MuiControlFontStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiControlFontStateField.Magic => 0,
			MuiControlFontStateField.Present => 4,
			MuiControlFontStateField.Font => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiControlFontStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiControlFontStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiControlFontStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiControlFontStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Font consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiControlFontStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiControlFontStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiControlFontStateRecord.Size)) return false;
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

internal static class MuiControlFontStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiControlFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiControlFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.Present) ||
			!MuiControlFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 8, out var font)) return false;
		value.Font = APTR.FromPointer(font);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiControlFontStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiControlFontStateAdmission.Validate(value)) return false;
		return MuiControlFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiControlFontStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			4, value.Present) &&
			MuiControlFontStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			8, value.Font.Raw);
	}
}
