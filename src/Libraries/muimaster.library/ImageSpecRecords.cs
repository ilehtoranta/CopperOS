/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the Image.mui tagged Image_Spec value and its
// ImageBuiltinSpec fallback. Raw is either a builtin image number or a guest
// STRPTR to a "kind:value" specification; Builtin is retained separately so
// absent Image_Spec does not become builtin image zero.
public struct MuiImageSpecState
{
	public bool Present;
	public uint Raw;
	public bool BuiltinPresent;
	public uint Builtin;
}

// Guest-resident Image spec union. Presence is kept separately for both
// attributes so an absent Image_Spec does not become builtin image zero during
// rendering and a supplied builtin value zero remains representable.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageSpecStateRecord
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x4D495350u; // 'MISP'

	internal uint Magic;
	internal uint Present;
	internal uint Raw;
	internal uint BuiltinPresent;
	internal uint Builtin;
}

internal enum MuiImageSpecStateField : byte
{
	Magic,
	Present,
	Raw,
	BuiltinPresent,
	Builtin,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageSpecStateFieldCursor
{
	internal APTR Record;
	internal MuiImageSpecStateField Field;
}

internal static class MuiImageSpecStateFieldCursorCodec
{
	private static bool TryResolve(MuiImageSpecStateField field,
		out uint offset)
		{
			offset = field switch
		{
			MuiImageSpecStateField.Magic => 0,
			MuiImageSpecStateField.Present => 4,
			MuiImageSpecStateField.Raw => 8,
			MuiImageSpecStateField.BuiltinPresent => 12,
			MuiImageSpecStateField.Builtin => 16,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageSpecStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiImageSpecStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageSpecStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageSpecStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageSpecStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageSpecStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiImageSpecStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageSpecStateRecord.Size) ||
			!MuiImageSpecStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiImageSpecStateField.Magic, out var magic) ||
			magic != MuiImageSpecStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiImageSpecStateFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiImageSpecStateField.Present, out value.Present) ||
			!MuiImageSpecStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiImageSpecStateField.Raw, out value.Raw) ||
			!MuiImageSpecStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiImageSpecStateField.BuiltinPresent, out value.BuiltinPresent) ||
			!MuiImageSpecStateFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiImageSpecStateField.Builtin, out value.Builtin)) return false;
		return value.Present <= 1 && value.BuiltinPresent <= 1;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageSpecStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageSpecStateRecord.Size) || value.Magic !=
			MuiImageSpecStateRecord.Cookie || value.Present > 1 ||
			value.BuiltinPresent > 1) return false;
		return MuiImageSpecStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiImageSpecStateField.Magic, value.Magic) &&
			MuiImageSpecStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiImageSpecStateField.Present, value.Present) &&
			MuiImageSpecStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiImageSpecStateField.Raw, value.Raw) &&
			MuiImageSpecStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiImageSpecStateField.BuiltinPresent, value.BuiltinPresent) &&
			MuiImageSpecStateFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiImageSpecStateField.Builtin, value.Builtin);
	}
}
