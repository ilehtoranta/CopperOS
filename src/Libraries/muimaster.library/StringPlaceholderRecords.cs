/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

public struct MuiStringPlaceholderState
{
	public APTR Contents;
}

// Guest-resident String.mui placeholder state.  The pointer identifies the
// object-owned bounded C string produced by CopyContents; callers never need a
// private widget offset or a managed text copy to render it.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringPlaceholderStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D535048u; // 'MSPH'

	internal uint Magic;
	internal APTR Contents;
}

internal enum MuiStringPlaceholderStateField : byte
{
	Magic,
	Contents,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringPlaceholderStateFieldCursor
{
	internal APTR Record;
	internal MuiStringPlaceholderStateField Field;
}

internal static class MuiStringPlaceholderStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringPlaceholderStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringPlaceholderStateField.Magic => 0,
			MuiStringPlaceholderStateField.Contents => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringPlaceholderStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringPlaceholderStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPlaceholderStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringPlaceholderStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringPlaceholderStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringPlaceholderStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringPlaceholderStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringPlaceholderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringPlaceholderStateRecord.Size) ||
			!MuiStringPlaceholderStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiStringPlaceholderStateField.Magic, out var magic) ||
			magic != MuiStringPlaceholderStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiStringPlaceholderStateFieldCursorCodec.TryReadUInt32(
			ref platform, address,
			MuiStringPlaceholderStateField.Contents, out var contents))
			return false;
		value.Contents = APTR.FromPointer(contents);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringPlaceholderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringPlaceholderStateRecord.Size) || value.Magic !=
			MuiStringPlaceholderStateRecord.Cookie) return false;
		return MuiStringPlaceholderStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringPlaceholderStateField.Magic, value.Magic) &&
			MuiStringPlaceholderStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiStringPlaceholderStateField.Contents, value.Contents.Raw);
	}
}
