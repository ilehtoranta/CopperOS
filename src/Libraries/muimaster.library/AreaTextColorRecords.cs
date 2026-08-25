/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public Area projection of MorphOS MUIA_TextColor. Color is a packed
// 00RRGGBB value; Active is non-zero only during MUIM_Setup..MUIM_Cleanup.
public struct MuiAreaTextColorState
{
	public uint Color;
	public uint Active;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTextColorStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4D544352u; // 'MTCR'

	internal uint Magic;
	internal uint Color;
	internal uint Active;
	internal uint Generation;
}

internal enum MuiAreaTextColorStateField : byte
{
	Magic,
	Color,
	Active,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaTextColorStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaTextColorStateField Field;
}

internal static class MuiAreaTextColorStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaTextColorStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaTextColorStateField.Magic => 0,
			MuiAreaTextColorStateField.Color => 4,
			MuiAreaTextColorStateField.Active => 8,
			MuiAreaTextColorStateField.Generation => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaTextColorStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaTextColorStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaTextColorStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaTextColorStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaTextColorStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaTextColorStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaTextColorStateRecord.Size) ||
			!MuiAreaTextColorStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaTextColorStateField.Magic, out var magic) ||
			magic != MuiAreaTextColorStateRecord.Cookie ||
			!MuiAreaTextColorStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaTextColorStateField.Color, out value.Color) ||
			!MuiAreaTextColorStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaTextColorStateField.Active, out value.Active) ||
			!MuiAreaTextColorStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaTextColorStateField.Generation,
				out value.Generation)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaTextColorStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaTextColorStateRecord.Size) || value.Magic !=
			MuiAreaTextColorStateRecord.Cookie) return false;
		return MuiAreaTextColorStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaTextColorStateField.Magic, value.Magic) &&
			MuiAreaTextColorStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaTextColorStateField.Color, value.Color) &&
			MuiAreaTextColorStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaTextColorStateField.Active, value.Active) &&
			MuiAreaTextColorStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaTextColorStateField.Generation, value.Generation);
	}
}
