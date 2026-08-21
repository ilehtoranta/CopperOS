/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Renderer-produced MUIA_Text_Shortened status.  The public attribute remains
// a guest ULONG, while the live value is kept in a named record so drawing and
// getters share one state seam without a private Text offset or managed flag.
public struct MuiTextShortenedState
{
	public uint Shortened;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextShortenedStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D545853u; // 'MTXS'

	internal uint Magic;
	internal uint Shortened;
}

internal enum MuiTextShortenedStateField : byte
{
	Magic,
	Shortened,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextShortenedStateFieldCursor
{
	internal APTR Record;
	internal MuiTextShortenedStateField Field;
}

internal static class MuiTextShortenedStateFieldCursorCodec
{
	private static bool TryResolve(MuiTextShortenedStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiTextShortenedStateField.Magic => 0,
			MuiTextShortenedStateField.Shortened => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextShortenedStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiTextShortenedStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextShortenedStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextShortenedStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextShortenedStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextShortenedStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiTextShortenedStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextShortenedStateRecord.Size) ||
			!MuiTextShortenedStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiTextShortenedStateField.Magic, out var magic) ||
			magic != MuiTextShortenedStateRecord.Cookie)
			return false;
		value.Magic = magic;
		return MuiTextShortenedStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextShortenedStateField.Shortened, out value.Shortened);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextShortenedStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextShortenedStateRecord.Size) || value.Magic !=
			MuiTextShortenedStateRecord.Cookie) return false;
		return MuiTextShortenedStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextShortenedStateField.Magic, value.Magic) &&
			MuiTextShortenedStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiTextShortenedStateField.Shortened, value.Shortened);
	}
}
