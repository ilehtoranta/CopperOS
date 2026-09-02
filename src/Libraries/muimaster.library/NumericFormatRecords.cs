/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the object-owned Numeric.mui format string.
public struct MuiNumericFormatState
{
	public APTR Format;
}

// Guest-resident numeric format state.  The format is copied into
// NumericFormatKey before publication, so the caller's source pointer is not
// retained by a numeric-family object.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNumericFormatStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint FormatOffset = 4;
	internal const uint Cookie = 0x4D4E4654u; // 'MNFT'

	internal uint Magic;
	internal APTR Format;
}

internal static class MuiNumericFormatStateAdmission
{
	internal const int MaximumLength = 256;

	internal static bool Validate(MuiNumericFormatStateRecord value) =>
		value.Magic == MuiNumericFormatStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(value) || obj.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull)
			return false;
		return value.Format.IsNull || CStringCodec.TryReadLength(ref platform,
			value.Format, MaximumLength, out _);
	}
}

internal enum MuiNumericFormatStateField : byte
{
	Magic,
	Format,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNumericFormatStateFieldCursor
{
	internal APTR Record;
	internal MuiNumericFormatStateField Field;
}

internal static class MuiNumericFormatStateFieldCursorCodec
{
	private static bool TryResolve(MuiNumericFormatStateField field,
		out uint offset)
	{
		if (field == MuiNumericFormatStateField.Magic)
			offset = MuiNumericFormatStateRecord.MagicOffset;
		else if (field == MuiNumericFormatStateField.Format)
			offset = MuiNumericFormatStateRecord.FormatOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNumericFormatStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiNumericFormatStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, MuiNumericFormatStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericFormatStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiNumericFormatStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericFormatStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiNumericFormatStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. The named format record remains the
// semantic state; this bounded adapter is the only fixed-layout translation.
internal static class MuiNumericFormatStateRecordMemoryCodec
{
	private static bool TryResolve(MuiNumericFormatStateField field,
		out uint offset)
	{
		if (field == MuiNumericFormatStateField.Magic)
			offset = MuiNumericFormatStateRecord.MagicOffset;
		else if (field == MuiNumericFormatStateField.Format)
			offset = MuiNumericFormatStateRecord.FormatOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericFormatStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryResolve(field, out var offset) &&
			TryGetAddress(ref platform, record, offset, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericFormatStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiNumericFormatStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiNumericFormatStateField.Magic)
			value = state.Magic;
		else if (field == MuiNumericFormatStateField.Format)
			value = state.Format.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericFormatStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiNumericFormatStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiNumericFormatStateField.Magic)
			state.Magic = value;
		else if (field == MuiNumericFormatStateField.Format)
			state.Format = APTR.FromPointer(value);
		else return false;
		return MuiNumericFormatStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiNumericFormatStateRecord.Size -
			MuiNumericFormatStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiNumericFormatStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiNumericFormatStateRecord.FieldSize);
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

internal static class MuiNumericFormatStateRecordCodec
{
	// Production record access is sequential and struct-shaped. The legacy
	// field-address adapters above remain available for malformed-state
	// diagnostics, but they are not part of this structural path.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiNumericFormatStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var format) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Magic = magic;
		value.Format = APTR.FromPointer(format);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiNumericFormatStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiNumericFormatStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Format.Raw)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNumericFormatStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiNumericFormatStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
