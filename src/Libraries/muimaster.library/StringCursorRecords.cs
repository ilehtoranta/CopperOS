/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident editor cursor state for String.mui.  The public attributes
// remain synchronized for MorphOS callers, while editing and drawing consume
// one validated record instead of treating two unrelated scalar slots as a
// private widget layout.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringCursorStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D534352u; // 'MSCR'

	internal uint Magic;
	internal int BufferPos;
	internal int DisplayPos;
}

internal enum MuiStringCursorStateField : byte
{
	Magic,
	BufferPos,
	DisplayPos,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringCursorStateFieldCursor
{
	internal APTR Record;
	internal MuiStringCursorStateField Field;
}

internal static class MuiStringCursorStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringCursorStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringCursorStateField.Magic => 0,
			MuiStringCursorStateField.BufferPos => 4,
			MuiStringCursorStateField.DisplayPos => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringCursorStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiStringCursorStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringCursorStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringCursorStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, field, out var raw))
			return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringCursorStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, field, unchecked((uint)value));
}

internal static class MuiStringCursorStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringCursorStateRecord.Size) ||
			!MuiStringCursorStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringCursorStateField.Magic, out var magic) ||
			magic != MuiStringCursorStateRecord.Cookie)
			return false;
		value.Magic = magic;
		return MuiStringCursorStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiStringCursorStateField.BufferPos, out value.BufferPos) &&
			MuiStringCursorStateFieldCursorCodec.TryReadInt32(ref platform,
				address, MuiStringCursorStateField.DisplayPos,
				out value.DisplayPos);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringCursorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringCursorStateRecord.Size) || value.Magic !=
			MuiStringCursorStateRecord.Cookie) return false;
		return MuiStringCursorStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringCursorStateField.Magic, value.Magic) &&
			MuiStringCursorStateFieldCursorCodec.TryWriteInt32(ref platform,
				address, MuiStringCursorStateField.BufferPos, value.BufferPos) &&
			MuiStringCursorStateFieldCursorCodec.TryWriteInt32(ref platform,
				address, MuiStringCursorStateField.DisplayPos,
				value.DisplayPos);
	}
}
