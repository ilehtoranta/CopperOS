/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the optional Rectangle.mui bar title pointer.
public struct MuiRectangleBarTitleState
{
	public bool Present;
	public APTR Title;
}

// Guest-resident title state. Presence remains separate from the pointer so
// an absent title is not confused with a present NULL value.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRectangleBarTitleStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D524254u; // 'MRBT'

	internal uint Magic;
	internal uint Present;
	internal APTR Title;
}

internal enum MuiRectangleBarTitleStateField : byte
{
	Magic,
	Present,
	Title,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiRectangleBarTitleStateFieldCursor
{
	internal APTR Record;
	internal MuiRectangleBarTitleStateField Field;
}

internal static class MuiRectangleBarTitleStateFieldCursorCodec
{
	private static bool TryResolve(MuiRectangleBarTitleStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiRectangleBarTitleStateField.Magic => 0,
			MuiRectangleBarTitleStateField.Present => 4,
			MuiRectangleBarTitleStateField.Title => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiRectangleBarTitleStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiRectangleBarTitleStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectangleBarTitleStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiRectangleBarTitleStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiRectangleBarTitleStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiRectangleBarTitleStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiRectangleBarTitleStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiRectangleBarTitleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiRectangleBarTitleStateRecord.Size) ||
			!MuiRectangleBarTitleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiRectangleBarTitleStateField.Magic, out var magic) ||
			magic != MuiRectangleBarTitleStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiRectangleBarTitleStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiRectangleBarTitleStateField.Present, out value.Present) ||
			!MuiRectangleBarTitleStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiRectangleBarTitleStateField.Title, out var title) ||
			value.Present > 1) return false;
		value.Title = APTR.FromPointer(title);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiRectangleBarTitleStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiRectangleBarTitleStateRecord.Size) || value.Magic !=
			MuiRectangleBarTitleStateRecord.Cookie || value.Present > 1) return false;
		return MuiRectangleBarTitleStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiRectangleBarTitleStateField.Magic,
			value.Magic) && MuiRectangleBarTitleStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiRectangleBarTitleStateField.Present,
			value.Present) && MuiRectangleBarTitleStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiRectangleBarTitleStateField.Title,
			value.Title.Raw);
	}
}
