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
		offset = field switch
		{
			MuiNumericFormatStateField.Magic => 0,
			MuiNumericFormatStateField.Format => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
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
		return platform.IsMapped(address, 4);
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiNumericFormatStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiNumericFormatStateRecord.Size)) return false;
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

internal static class MuiNumericFormatStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiNumericFormatStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiNumericFormatStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var format))
			return false;
		value.Format = APTR.FromPointer(format);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiNumericFormatStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNumericFormatStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiNumericFormatStateAdmission.Validate(value)) return false;
		return MuiNumericFormatStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiNumericFormatStateRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, 4, value.Format.Raw);
	}
}
