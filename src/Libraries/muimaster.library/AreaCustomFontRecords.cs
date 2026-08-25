/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_CustomFont is a caller-owned MUI font-specification string. Keep the
// pointer and presence separate so an explicit NULL value remains distinct
// from an omitted attribute, without retaining a managed string.
public struct MuiAreaCustomFontStateInput
{
	public APTR Spec;
	public uint Present;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCustomFontStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4143464Eu; // 'ACFN'

	internal uint Magic;
	internal APTR Spec;
	internal uint Present;
	internal uint Generation;
}

internal enum MuiAreaCustomFontStateField : byte
{
	Magic,
	Spec,
	Present,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaCustomFontStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaCustomFontStateField Field;
}

internal static class MuiAreaCustomFontStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaCustomFontStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaCustomFontStateField.Magic => 0,
			MuiAreaCustomFontStateField.Spec => 4,
			MuiAreaCustomFontStateField.Present => 8,
			MuiAreaCustomFontStateField.Generation => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaCustomFontStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiAreaCustomFontStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaCustomFontStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaCustomFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaCustomFontStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaCustomFontStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaCustomFontStateRecord.Size) ||
			!MuiAreaCustomFontStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Magic, out var magic) ||
			magic != MuiAreaCustomFontStateRecord.Cookie ||
			!MuiAreaCustomFontStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Spec, out var spec) ||
			!MuiAreaCustomFontStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Present, out value.Present) ||
			!MuiAreaCustomFontStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Generation,
				out value.Generation) || value.Present > 1) return false;
		value.Magic = magic;
		value.Spec = APTR.FromPointer(spec);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaCustomFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaCustomFontStateRecord.Size) || value.Magic !=
			MuiAreaCustomFontStateRecord.Cookie || value.Present > 1) return false;
		return MuiAreaCustomFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaCustomFontStateField.Magic, value.Magic) &&
			MuiAreaCustomFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Spec, value.Spec.Raw) &&
			MuiAreaCustomFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Present, value.Present) &&
			MuiAreaCustomFontStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaCustomFontStateField.Generation,
				value.Generation);
	}
}
