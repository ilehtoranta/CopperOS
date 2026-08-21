/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the renderer-produced remapped Bitmap/Bodychunk
// pointer. The pointer remains guest-owned and may be null when no decoded or
// remapped source is available.
public struct MuiBitmapRemappedState
{
	public APTR Remapped;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapRemappedStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D425253u; // 'MBRS'

	internal uint Magic;
	internal APTR Remapped;
}

internal enum MuiBitmapRemappedStateField : byte
{
	Magic,
	Remapped,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapRemappedStateFieldCursor
{
	internal APTR Record;
	internal MuiBitmapRemappedStateField Field;
}

internal static class MuiBitmapRemappedStateFieldCursorCodec
{
	private static bool TryResolve(MuiBitmapRemappedStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiBitmapRemappedStateField.Magic => 0,
			MuiBitmapRemappedStateField.Remapped => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapRemappedStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiBitmapRemappedStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapRemappedStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiBitmapRemappedStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapRemappedStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBitmapRemappedStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiBitmapRemappedStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiBitmapRemappedStateRecord.Size) ||
			!MuiBitmapRemappedStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiBitmapRemappedStateField.Magic, out var magic) ||
			magic != MuiBitmapRemappedStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiBitmapRemappedStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBitmapRemappedStateField.Remapped, out var remapped))
			return false;
		value.Remapped = APTR.FromPointer(remapped);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapRemappedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiBitmapRemappedStateRecord.Size) || value.Magic !=
			MuiBitmapRemappedStateRecord.Cookie) return false;
		return MuiBitmapRemappedStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapRemappedStateField.Magic, value.Magic) &&
			MuiBitmapRemappedStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapRemappedStateField.Remapped,
			value.Remapped.Raw);
	}
}
