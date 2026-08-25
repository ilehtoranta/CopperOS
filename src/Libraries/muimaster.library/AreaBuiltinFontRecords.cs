/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_BuiltinFont is an unsigned ABI value whose documented selectors are
// negative signed constants (MUIV_BuiltinFont_*). Keep the raw ULONG bit
// pattern so the guest can distinguish an explicit Inherit selector from an
// absent tag without relying on a private object offset.
public struct MuiAreaBuiltinFontState
{
	public uint Selector;
	public uint Present;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaBuiltinFontStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4D424652u; // 'MBFR'

	internal uint Magic;
	internal uint Selector;
	internal uint Present;
	internal uint Generation;
}

internal enum MuiAreaBuiltinFontStateField : byte
{
	Magic,
	Selector,
	Present,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaBuiltinFontStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaBuiltinFontStateField Field;
}

internal static class MuiAreaBuiltinFontStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaBuiltinFontStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaBuiltinFontStateField.Magic => 0,
			MuiAreaBuiltinFontStateField.Selector => 4,
			MuiAreaBuiltinFontStateField.Present => 8,
			MuiAreaBuiltinFontStateField.Generation => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaBuiltinFontStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiAreaBuiltinFontStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaBuiltinFontStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaBuiltinFontStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaBuiltinFontStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaBuiltinFontStateRecord.Size) ||
			!MuiAreaBuiltinFontStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaBuiltinFontStateField.Magic, out var magic) ||
			magic != MuiAreaBuiltinFontStateRecord.Cookie ||
			!MuiAreaBuiltinFontStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaBuiltinFontStateField.Selector, out value.Selector) ||
			!MuiAreaBuiltinFontStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaBuiltinFontStateField.Present, out value.Present) ||
			!MuiAreaBuiltinFontStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaBuiltinFontStateField.Generation,
				out value.Generation)) return false;
		value.Magic = magic;
		return value.Present <= 1;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaBuiltinFontStateRecord.Size) || value.Magic !=
			MuiAreaBuiltinFontStateRecord.Cookie || value.Present > 1) return false;
		return MuiAreaBuiltinFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaBuiltinFontStateField.Magic, value.Magic) &&
			MuiAreaBuiltinFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaBuiltinFontStateField.Selector, value.Selector) &&
			MuiAreaBuiltinFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaBuiltinFontStateField.Present, value.Present) &&
			MuiAreaBuiltinFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaBuiltinFontStateField.Generation,
			value.Generation);
	}
}
