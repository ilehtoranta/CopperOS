/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the optional common-control TextFont pointer.
public struct MuiControlFontState
{
	public bool Present;
	public APTR Font;
}

// Guest-resident shared Font state. Presence is kept separate so a missing
// Font attribute remains distinguishable from a present NULL pointer.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiControlFontStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D43464Eu; // 'MCFN'

	internal uint Magic;
	internal uint Present;
	internal APTR Font;
}

internal enum MuiControlFontStateField : byte
{
	Magic,
	Present,
	Font,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiControlFontStateFieldCursor
{
	internal APTR Record;
	internal MuiControlFontStateField Field;
}

internal static class MuiControlFontStateFieldCursorCodec
{
	private static bool TryResolve(MuiControlFontStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiControlFontStateField.Magic => 0,
			MuiControlFontStateField.Present => 4,
			MuiControlFontStateField.Font => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiControlFontStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiControlFontStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiControlFontStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiControlFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiControlFontStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiControlFontStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiControlFontStateRecord.Size) ||
			!MuiControlFontStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiControlFontStateField.Magic, out var magic) ||
			magic != MuiControlFontStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiControlFontStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiControlFontStateField.Present, out value.Present) ||
			!MuiControlFontStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiControlFontStateField.Font, out var font) ||
			value.Present > 1) return false;
		value.Font = APTR.FromPointer(font);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiControlFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiControlFontStateRecord.Size) || value.Magic !=
			MuiControlFontStateRecord.Cookie || value.Present > 1) return false;
		return MuiControlFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiControlFontStateField.Magic, value.Magic) &&
			MuiControlFontStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiControlFontStateField.Present, value.Present) &&
			MuiControlFontStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiControlFontStateField.Font, value.Font.Raw);
	}
}
