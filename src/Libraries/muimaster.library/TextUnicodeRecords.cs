/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Text.mui's initializer-only Unicode policy.  The public attribute remains
// a BOOL-shaped ULONG, while the implementation keeps its normalized value in
// a named guest record so metrics and drawing share one semantic source.
public struct MuiTextUnicodeState
{
	public uint Unicode;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextUnicodeStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint UnicodeOffset = 4;
	internal const uint Cookie = 0x4D54554Eu; // 'MTUN'

	internal uint Magic;
	internal uint Unicode;
}

internal enum MuiTextUnicodeStateField : byte
{
	Magic,
	Unicode,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextUnicodeStateFieldCursor
{
	internal APTR Record;
	internal MuiTextUnicodeStateField Field;
}

internal static class MuiTextUnicodeStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextUnicodeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextUnicodeStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextUnicodeStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextUnicodeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextUnicodeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextUnicodeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextUnicodeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Text consumers use the named Unicode
// policy record; this bounded adapter is the only layer that translates its
// fixed guest layout into addresses. The cursor codec remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiTextUnicodeStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiTextUnicodeStateField field,
		out uint index)
	{
		if (field == MuiTextUnicodeStateField.Magic)
			index = 0;
		else if (field == MuiTextUnicodeStateField.Unicode)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextUnicodeStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextUnicodeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextUnicodeStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiTextUnicodeStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiTextUnicodeStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiTextUnicodeStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextUnicodeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiTextUnicodeStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiTextUnicodeStateField.Magic)
			value = state.Magic;
		else if (field == MuiTextUnicodeStateField.Unicode)
			value = state.Unicode;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextUnicodeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiTextUnicodeStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiTextUnicodeStateField.Magic)
			state.Magic = value;
		else if (field == MuiTextUnicodeStateField.Unicode)
			state.Unicode = value;
		else return false;
		return MuiTextUnicodeStateRecordCodec.WriteStructural(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiTextUnicodeStateRecord.Size -
			MuiTextUnicodeStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiTextUnicodeStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiTextUnicodeStateRecord.FieldSize);
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

internal static class MuiTextUnicodeStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextUnicodeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextUnicodeStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Unicode) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiTextUnicodeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextUnicodeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiTextUnicodeStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextUnicodeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiTextUnicodeStateAdmission.Validate(value) &&
		WriteStructural(ref platform, address, value);

	internal static bool WriteStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		MuiTextUnicodeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiTextUnicodeStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Unicode) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextUnicodeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);
}

// Keep the wire value lossless for malformed-state diagnostics. Text Unicode
// mode is a MorphOS BOOL and must be canonical before metrics or rendering
// consumers use the named policy state.
internal static class MuiTextUnicodeStateValidation
{
	internal static bool IsValidRecord(MuiTextUnicodeStateRecord value) =>
		value.Unicode <= 1;

	internal static bool IsValidState(MuiTextUnicodeState value) =>
		value.Unicode <= 1;
}

internal static class MuiTextUnicodeStateAdmission
{
	internal static bool Validate(MuiTextUnicodeStateRecord value) =>
		value.Magic == MuiTextUnicodeStateRecord.Cookie &&
		MuiTextUnicodeStateValidation.IsValidRecord(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiTextUnicodeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
