/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the object-owned String.mui contents pointer.
public struct MuiStringContentsState
{
	public APTR Contents;
}

// Guest-resident String contents state.  The pointer is backed by the
// object-owned StringCopyKey Dataspace entry after CopyContents completes.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringContentsStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D53434Eu; // 'MSCN'

	internal uint Magic;
	internal APTR Contents;
}

internal enum MuiStringContentsStateField : byte
{
	Magic,
	Contents,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringContentsStateFieldCursor
{
	internal APTR Record;
	internal MuiStringContentsStateField Field;
}

internal static class MuiStringContentsStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringContentsStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringContentsStateField.Magic => 0,
			MuiStringContentsStateField.Contents => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringContentsStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringContentsStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringContentsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringContentsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringContentsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringContentsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringContentsStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringContentsStateRecord.Size) ||
			!MuiStringContentsStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringContentsStateField.Magic, out var magic) ||
			magic != MuiStringContentsStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiStringContentsStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringContentsStateField.Contents, out var contents))
			return false;
		value.Contents = APTR.FromPointer(contents);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringContentsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringContentsStateRecord.Size) || value.Magic !=
			MuiStringContentsStateRecord.Cookie) return false;
		return MuiStringContentsStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringContentsStateField.Magic, value.Magic) &&
			MuiStringContentsStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringContentsStateField.Contents, value.Contents.Raw);
	}
}
