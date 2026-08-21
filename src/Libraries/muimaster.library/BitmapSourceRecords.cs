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

internal static class MuiBitmapSourceStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiBitmapSourceStateRecord.Size) ||
			!MuiBitmapSourceStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiBitmapSourceStateField.Magic, out var magic) ||
			magic != MuiBitmapSourceStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiBitmapSourceStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBitmapSourceStateField.Source, out var source)) return false;
		value.Source = APTR.FromPointer(source);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapSourceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiBitmapSourceStateRecord.Size) || value.Magic !=
			MuiBitmapSourceStateRecord.Cookie) return false;
		return MuiBitmapSourceStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapSourceStateField.Magic, value.Magic) &&
			MuiBitmapSourceStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapSourceStateField.Source, value.Source.Raw);
	}
}
