/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared semantic geometry for Bitmap.mui and Bodychunk.mui.  Width and
// Height remain MorphOS ULONG-compatible while layout and decoding consume
// one named value rather than separate anonymous attribute reads.
public struct MuiBitmapGeometryState
{
	public uint Width;
	public uint Height;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapGeometryStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D424759u; // 'MBGY'

	internal uint Magic;
	internal uint Width;
	internal uint Height;
}

internal enum MuiBitmapGeometryStateField : byte
{
	Magic,
	Width,
	Height,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBitmapGeometryStateFieldCursor
{
	internal APTR Record;
	internal MuiBitmapGeometryStateField Field;
}

internal static class MuiBitmapGeometryStateFieldCursorCodec
{
	private static bool TryResolve(MuiBitmapGeometryStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiBitmapGeometryStateField.Magic => 0,
			MuiBitmapGeometryStateField.Width => 4,
			MuiBitmapGeometryStateField.Height => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBitmapGeometryStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiBitmapGeometryStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapGeometryStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiBitmapGeometryStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBitmapGeometryStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBitmapGeometryStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiBitmapGeometryStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiBitmapGeometryStateRecord.Size) ||
			!MuiBitmapGeometryStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiBitmapGeometryStateField.Magic, out var magic) ||
			magic != MuiBitmapGeometryStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiBitmapGeometryStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBitmapGeometryStateField.Width, out value.Width) ||
			!MuiBitmapGeometryStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiBitmapGeometryStateField.Height, out value.Height))
			return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBitmapGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiBitmapGeometryStateRecord.Size) || value.Magic !=
			MuiBitmapGeometryStateRecord.Cookie) return false;
		return MuiBitmapGeometryStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapGeometryStateField.Magic, value.Magic) &&
			MuiBitmapGeometryStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapGeometryStateField.Width, value.Width) &&
			MuiBitmapGeometryStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiBitmapGeometryStateField.Height, value.Height);
	}
}
