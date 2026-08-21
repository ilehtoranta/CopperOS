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

internal static class MuiNumericFormatStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiNumericFormatStateRecord.Size) ||
			!MuiNumericFormatStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiNumericFormatStateField.Magic, out var magic) ||
			magic != MuiNumericFormatStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiNumericFormatStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiNumericFormatStateField.Format, out var format))
			return false;
		value.Format = APTR.FromPointer(format);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNumericFormatStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiNumericFormatStateRecord.Size) || value.Magic !=
			MuiNumericFormatStateRecord.Cookie) return false;
		return MuiNumericFormatStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiNumericFormatStateField.Magic, value.Magic) &&
			MuiNumericFormatStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiNumericFormatStateField.Format, value.Format.Raw);
	}
}
