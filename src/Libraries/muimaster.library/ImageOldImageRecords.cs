/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the caller-owned graphics.library Image pointer.
public struct MuiImageOldImageState
{
	public APTR Image;
}

// Guest-resident Image.mui OldImage state. The pointer remains caller-owned;
// this record only gives the object a named, validated state boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageOldImageStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D494F49u; // 'MIOI'

	internal uint Magic;
	internal APTR Image;
}

internal enum MuiImageOldImageStateField : byte
{
	Magic,
	Image,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageOldImageStateFieldCursor
{
	internal APTR Record;
	internal MuiImageOldImageStateField Field;
}

internal static class MuiImageOldImageStateFieldCursorCodec
{
	private static bool TryResolve(MuiImageOldImageStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiImageOldImageStateField.Magic => 0,
			MuiImageOldImageStateField.Image => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageOldImageStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiImageOldImageStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageOldImageStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageOldImageStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageOldImageStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageOldImageStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiImageOldImageStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageOldImageStateRecord.Size) ||
			!MuiImageOldImageStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiImageOldImageStateField.Magic, out var magic) ||
			magic != MuiImageOldImageStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiImageOldImageStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiImageOldImageStateField.Image, out var image)) return false;
		value.Image = APTR.FromPointer(image);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageOldImageStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageOldImageStateRecord.Size) || value.Magic !=
			MuiImageOldImageStateRecord.Cookie) return false;
		return MuiImageOldImageStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiImageOldImageStateField.Magic, value.Magic) &&
			MuiImageOldImageStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiImageOldImageStateField.Image, value.Image.Raw);
	}
}
