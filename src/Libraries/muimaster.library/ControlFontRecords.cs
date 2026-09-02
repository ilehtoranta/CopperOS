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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint PresentOffset = 4;
	internal const uint FontOffset = 8;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiControlFontStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiControlFontStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		return MuiControlFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiControlFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Font consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiControlFontStateRecordMemoryCodec
{
	private static bool TryResolve(MuiControlFontStateField field,
		out uint offset)
	{
		if (field == MuiControlFontStateField.Magic)
			offset = MuiControlFontStateRecord.MagicOffset;
		else if (field == MuiControlFontStateField.Present)
			offset = MuiControlFontStateRecord.PresentOffset;
		else if (field == MuiControlFontStateField.Font)
			offset = MuiControlFontStateRecord.FontOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiControlFontStateRecord.Size) &&
			platform.IsMapped(address, MuiControlFontStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiControlFontStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiControlFontStateField.Magic)
			value = state.Magic;
		else if (field == MuiControlFontStateField.Present)
			value = state.Present;
		else if (field == MuiControlFontStateField.Font)
			value = state.Font.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiControlFontStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiControlFontStateField.Magic)
			state.Magic = value;
		else if (field == MuiControlFontStateField.Present)
			state.Present = value;
		else if (field == MuiControlFontStateField.Font)
			state.Font = APTR.FromPointer(value);
		else return false;
		return MuiControlFontStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	// Legacy raw-offset adapter retained for bounded diagnostics and older
	// callers. Typed field access above is the preferred API.
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiControlFontStateRecord.Size -
			MuiControlFontStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiControlFontStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiControlFontStateRecord.FieldSize);
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
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiControlFontStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Present) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var font)) return false;
		value.Font = APTR.FromPointer(font);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiControlFontStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Present) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Font.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiControlFontStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiControlFontStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
