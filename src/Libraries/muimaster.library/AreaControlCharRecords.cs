/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_ControlChar is a character-valued Area policy. Keep the normalized
// low-byte value in a named record; Text.mui's separate MUIA_Text_ControlChar
// state remains class-specific and is not silently aliased to this field.
public struct MuiAreaControlCharStateInput
{
	public uint Character;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaControlCharStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x41434348u; // 'ACCH'

	internal uint Magic;
	internal uint Character;
	internal uint Generation;
}

internal enum MuiAreaControlCharStateField : byte
{
	Magic,
	Character,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaControlCharStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaControlCharStateField Field;
}

internal static class MuiAreaControlCharStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaControlCharStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiAreaControlCharStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaControlCharStateField.Character:
				offset = 4;
				return true;
			case MuiAreaControlCharStateField.Generation:
				offset = 8;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaControlCharStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaControlCharStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaControlCharStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaControlCharStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaControlCharStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaControlCharStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaControlCharStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaControlCharStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaControlCharStateRecord.Size) ||
			!MuiAreaControlCharStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaControlCharStateField.Magic, out var magic) ||
			magic != MuiAreaControlCharStateRecord.Cookie ||
			!MuiAreaControlCharStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaControlCharStateField.Character, out var character) ||
			!MuiAreaControlCharStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaControlCharStateField.Generation,
				out var generation)) return false;
		value.Magic = magic;
		value.Character = character & 0xFFu;
		value.Generation = generation;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaControlCharStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaControlCharStateRecord.Size) || value.Magic !=
			MuiAreaControlCharStateRecord.Cookie) return false;
		return MuiAreaControlCharStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaControlCharStateField.Magic, value.Magic) &&
			MuiAreaControlCharStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaControlCharStateField.Character,
			value.Character & 0xFFu) &&
			MuiAreaControlCharStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaControlCharStateField.Generation, value.Generation);
	}
}
