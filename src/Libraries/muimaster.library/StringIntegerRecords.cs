/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of MUIA_String_Integer.  The guest attribute remains a
// ULONG ABI value, while consumers can use the signed value without a managed
// numeric object or a private String offset.
public struct MuiStringIntegerState
{
	public int Value;
}

// Guest-resident signed String.mui integer state.  Contents remain the source
// text; this record is the canonical parsed value exposed by Integer Get.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringIntegerStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D53494Eu; // 'MSIN'

	internal uint Magic;
	internal int Value;
}

internal enum MuiStringIntegerStateField : byte
{
	Magic,
	Value,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringIntegerStateFieldCursor
{
	internal APTR Record;
	internal MuiStringIntegerStateField Field;
}

internal static class MuiStringIntegerStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringIntegerStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiStringIntegerStateField.Magic => 0,
			MuiStringIntegerStateField.Value => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringIntegerStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiStringIntegerStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringIntegerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringIntegerStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringIntegerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringIntegerStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringIntegerStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringIntegerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringIntegerStateRecord.Size) ||
			!MuiStringIntegerStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringIntegerStateField.Magic, out var magic) ||
			magic != MuiStringIntegerStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiStringIntegerStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringIntegerStateField.Value, out var raw)) return false;
		value.Value = unchecked((int)raw);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringIntegerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringIntegerStateRecord.Size) || value.Magic !=
			MuiStringIntegerStateRecord.Cookie) return false;
		return MuiStringIntegerStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringIntegerStateField.Magic, value.Magic) &&
			MuiStringIntegerStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringIntegerStateField.Value,
				unchecked((uint)value.Value));
	}
}
