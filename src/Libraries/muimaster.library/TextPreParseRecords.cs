/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the object-owned Text.mui PreParse string.
public struct MuiTextPreParseState
{
	public APTR PreParse;
}

// Guest-resident Text.mui PreParse state.  The pointer is always copied into
// TextPreParseKey before publication, so callers may release their source.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextPreParseStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D545050u; // 'MTPP'

	internal uint Magic;
	internal APTR PreParse;
}

internal static class MuiTextPreParseStateAdmission
{
	internal const int MaximumLength = 65536;

	internal static bool Validate(MuiTextPreParseStateRecord value) =>
		value.Magic == MuiTextPreParseStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.PreParse.IsNull || CStringCodec.TryReadLength(ref platform,
			value.PreParse, MaximumLength, out _);
	}
}

internal enum MuiTextPreParseStateField : byte
{
	Magic,
	PreParse,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextPreParseStateFieldCursor
{
	internal APTR Record;
	internal MuiTextPreParseStateField Field;
}

internal static class MuiTextPreParseStateFieldCursorCodec
{
	private static bool TryResolve(MuiTextPreParseStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiTextPreParseStateField.Magic => 0,
			MuiTextPreParseStateField.PreParse => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextPreParseStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiTextPreParseStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPreParseStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextPreParseStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPreParseStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextPreParseStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. The named PreParse pointer remains the
// semantic record; this bounded adapter owns the fixed guest translation.
// The cursor codec remains available for compatibility and malformed-state
// diagnostics.
internal static class MuiTextPreParseStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiTextPreParseStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiTextPreParseStateRecord.Size)) return false;
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

internal static class MuiTextPreParseStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiTextPreParseStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiTextPreParseStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var preParse)) return false;
		value.PreParse = APTR.FromPointer(preParse);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiTextPreParseStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiTextPreParseStateAdmission.Validate(value)) return false;
		return MuiTextPreParseStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiTextPreParseStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.PreParse.Raw);
	}
}
