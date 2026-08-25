/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Runtime result owned by an Area after OpenCustomFont/Setup.  Font is an
// opaque provider handle; Spec is the caller-owned guest string from which it
// was opened.  Keeping both as APTR values makes the lifetime explicit without
// introducing a managed font object.
public struct MuiAreaCustomFontHandleState
{
	public APTR Font;
	public APTR Spec;
	public uint Generation;
	public uint Active;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiAreaCustomFontRuntimeRecord
{
	public const uint Size = 20;
	public const uint Cookie = 0x41434652u; // 'ACFR'

	public uint Magic;
	public APTR Font;
	public APTR Spec;
	public uint Generation;
	public uint Active;
}

public enum MuiAreaCustomFontRuntimeField : byte
{
	Magic,
	Font,
	Spec,
	Generation,
	Active,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MuiAreaCustomFontRuntimeFieldCursor
{
	public APTR Record;
	public MuiAreaCustomFontRuntimeField Field;
}

public static class MuiAreaCustomFontRuntimeFieldCursorCodec
{
	private static bool TryResolve(MuiAreaCustomFontRuntimeField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaCustomFontRuntimeField.Magic => 0,
			MuiAreaCustomFontRuntimeField.Font => 4,
			MuiAreaCustomFontRuntimeField.Spec => 8,
			MuiAreaCustomFontRuntimeField.Generation => 12,
			MuiAreaCustomFontRuntimeField.Active => 16,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	public static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontRuntimeFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiAreaCustomFontRuntimeRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	public static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontRuntimeField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaCustomFontRuntimeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	public static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontRuntimeField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaCustomFontRuntimeFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

public static class MuiAreaCustomFontRuntimeRecordCodec
{
	public static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCustomFontRuntimeRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaCustomFontRuntimeRecord.Size) ||
			!MuiAreaCustomFontRuntimeFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontRuntimeField.Magic, out value.Magic) ||
			value.Magic != MuiAreaCustomFontRuntimeRecord.Cookie ||
			!MuiAreaCustomFontRuntimeFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontRuntimeField.Font, out var font) ||
			!MuiAreaCustomFontRuntimeFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontRuntimeField.Spec, out var spec) ||
			!MuiAreaCustomFontRuntimeFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontRuntimeField.Generation,
				out value.Generation) ||
			!MuiAreaCustomFontRuntimeFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontRuntimeField.Active, out value.Active) ||
			value.Active > 1) return false;
		value.Font = APTR.FromPointer(font);
		value.Spec = APTR.FromPointer(spec);
		return true;
	}

	public static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCustomFontRuntimeRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaCustomFontRuntimeRecord.Size) || value.Magic !=
			MuiAreaCustomFontRuntimeRecord.Cookie || value.Active > 1) return false;
		return MuiAreaCustomFontRuntimeFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaCustomFontRuntimeField.Magic, value.Magic) &&
			MuiAreaCustomFontRuntimeFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCustomFontRuntimeField.Font, value.Font.Raw) &&
			MuiAreaCustomFontRuntimeFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCustomFontRuntimeField.Spec, value.Spec.Raw) &&
			MuiAreaCustomFontRuntimeFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCustomFontRuntimeField.Generation,
				value.Generation) &&
			MuiAreaCustomFontRuntimeFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCustomFontRuntimeField.Active, value.Active);
	}
}
