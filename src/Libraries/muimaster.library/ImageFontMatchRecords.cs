/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the optional Image.mui font-match string pointer.
public struct MuiImageFontMatchStringState
{
	public bool Present;
	public APTR MatchString;
}

// Guest-resident FontMatchString state. The pointer remains caller-owned; the
// record provides a named presence and lifetime boundary for the attribute.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageFontMatchStringStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x4D49464Du; // 'MIFM'

	internal uint Magic;
	internal uint Present;
	internal APTR MatchString;
}

internal enum MuiImageFontMatchStringStateField : byte
{
	Magic,
	Present,
	MatchString,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiImageFontMatchStringStateFieldCursor
{
	internal APTR Record;
	internal MuiImageFontMatchStringStateField Field;
}

internal static class MuiImageFontMatchStringStateFieldCursorCodec
{
	private static bool TryResolve(MuiImageFontMatchStringStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiImageFontMatchStringStateField.Magic => 0,
			MuiImageFontMatchStringStateField.Present => 4,
			MuiImageFontMatchStringStateField.MatchString => 8,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiImageFontMatchStringStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiImageFontMatchStringStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStringStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiImageFontMatchStringStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiImageFontMatchStringStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiImageFontMatchStringStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiImageFontMatchStringStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiImageFontMatchStringStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageFontMatchStringStateRecord.Size) ||
			!MuiImageFontMatchStringStateFieldCursorCodec.TryReadUInt32(
				ref platform, address, MuiImageFontMatchStringStateField.Magic,
				out var magic) || magic != MuiImageFontMatchStringStateRecord.Cookie)
			return false;
		value.Magic = magic;
		if (!MuiImageFontMatchStringStateFieldCursorCodec.TryReadUInt32(
			ref platform, address, MuiImageFontMatchStringStateField.Present,
			out value.Present) || !MuiImageFontMatchStringStateFieldCursorCodec
			.TryReadUInt32(ref platform, address,
				MuiImageFontMatchStringStateField.MatchString, out var matchString) ||
			value.Present > 1) return false;
		value.MatchString = APTR.FromPointer(matchString);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiImageFontMatchStringStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiImageFontMatchStringStateRecord.Size) || value.Magic !=
			MuiImageFontMatchStringStateRecord.Cookie || value.Present > 1)
			return false;
		return MuiImageFontMatchStringStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiImageFontMatchStringStateField.Magic,
			value.Magic) && MuiImageFontMatchStringStateFieldCursorCodec
			.TryWriteUInt32(ref platform, address,
				MuiImageFontMatchStringStateField.Present, value.Present) &&
			MuiImageFontMatchStringStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiImageFontMatchStringStateField.MatchString,
				value.MatchString.Raw);
	}
}
