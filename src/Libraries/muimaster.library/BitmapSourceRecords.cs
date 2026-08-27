/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of a Bitmap.mui BitMap or Bodychunk.mui Body pointer.
// The source remains caller-owned; setup may derive a separate remapped buffer.
public struct MuiBitmapSourceState
{
	public APTR Source;
}

// Guest-resident source state shared by the two bitmap-family source attrs.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapSourceStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D42534Fu; // 'MBSO'

	internal uint Magic;
	internal APTR Source;
}

internal enum MuiBitmapSourceStateField : byte
{
	Magic,
	Source,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapSourceStateFieldCursor
{
	internal APTR Record;
	internal MuiBitmapSourceStateField Field;
}

internal static class MuiBitmapSourceStateFieldCursorCodec
{
	private static bool TryResolve(MuiBitmapSourceStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiBitmapSourceStateField.Magic => 0,
			MuiBitmapSourceStateField.Source => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapSourceStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiBitmapSourceStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapSourceStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiBitmapSourceStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapSourceStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBitmapSourceStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Bitmap source consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiBitmapSourceStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiBitmapSourceStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiBitmapSourceStateRecord.Size)) return false;
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

internal static class MuiBitmapSourceStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiBitmapSourceStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) ||
			!MuiBitmapSourceStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out var source)) return false;
		value.Source = APTR.FromPointer(source);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiBitmapSourceStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiBitmapSourceStateAdmission.Validate(ref platform, value)) return false;
		return MuiBitmapSourceStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiBitmapSourceStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 4, value.Source.Raw);
	}
}

internal static class MuiBitmapSourceStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiBitmapSourceStateRecord.Cookie &&
		(value.Source.IsNull || platform.IsMapped(value.Source, 4));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}
