/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the live Text.mui contents pointer.
public struct MuiTextContentsState
{
	public APTR Contents;
}

// Guest-resident Text.mui contents state.  The pointer is caller-owned when
// MUIA_Text_Copy is FALSE and points at object-owned TextCopyKey storage when
// CopyContents has materialised a copy.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextContentsStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D54434Eu; // 'MTCN'

	internal uint Magic;
	internal APTR Contents;
}

internal enum MuiTextContentsStateField : byte
{
	Magic,
	Contents,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextContentsStateFieldCursor
{
	internal APTR Record;
	internal MuiTextContentsStateField Field;
}

internal static class MuiTextContentsStateFieldCursorCodec
{
	private static bool TryResolve(MuiTextContentsStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiTextContentsStateField.Magic => 0,
			MuiTextContentsStateField.Contents => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextContentsStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiTextContentsStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextContentsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextContentsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextContentsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextContentsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiTextContentsStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextContentsStateRecord.Size) ||
			!MuiTextContentsStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiTextContentsStateField.Magic, out var magic) ||
			magic != MuiTextContentsStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiTextContentsStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextContentsStateField.Contents, out var contents))
			return false;
		value.Contents = APTR.FromPointer(contents);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextContentsStateRecord.Size) || value.Magic !=
			MuiTextContentsStateRecord.Cookie) return false;
		return MuiTextContentsStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextContentsStateField.Magic, value.Magic) &&
			MuiTextContentsStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiTextContentsStateField.Contents, value.Contents.Raw);
	}
}
