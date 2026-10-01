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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint PreParseOffset = 4;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextPreParseStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextPreParseStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextPreParseStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

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
	private static bool TryResolveFieldIndex(MuiTextPreParseStateField field,
		out uint index)
	{
		if (field == MuiTextPreParseStateField.Magic)
			index = 0;
		else if (field == MuiTextPreParseStateField.PreParse)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPreParseStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextPreParseStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextPreParseStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiTextPreParseStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiTextPreParseStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiTextPreParseStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPreParseStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiTextPreParseStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiTextPreParseStateField.Magic)
			value = state.Magic;
		else if (field == MuiTextPreParseStateField.PreParse)
			value = state.PreParse.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPreParseStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiTextPreParseStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiTextPreParseStateField.Magic)
			state.Magic = value;
		else if (field == MuiTextPreParseStateField.PreParse)
			state.PreParse = APTR.FromPointer(value);
		else return false;
		return MuiTextPreParseStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiTextPreParseStateRecord.Size -
			MuiTextPreParseStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiTextPreParseStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiTextPreParseStateRecord.FieldSize);
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
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextPreParseStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var preParse)) return false;
		value.Magic = magic;
		value.PreParse = APTR.FromPointer(preParse);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiTextPreParseStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextPreParseStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextPreParseStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PreParse.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}
