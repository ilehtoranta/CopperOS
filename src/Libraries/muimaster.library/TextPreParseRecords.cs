/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Public semantic view of the object-owned Text.mui PreParse string.
public struct MuiTextPreParseState
{
	public APTR PreParse;
}

// Guest-resident Text.mui PreParse state.  The pointer is always copied into
// TextPreParseKey before publication, so callers may release their source.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextPreParseStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D545050u; // 'MTPP'

	internal uint Magic;
	internal APTR PreParse;
}

internal enum MuiTextPreParseStateField : byte
{
	Magic,
	PreParse,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextPreParseStateFieldCursor
{
	internal APTR Record;
	internal MuiTextPreParseStateField Field;
}

internal static class MuiTextPreParseStateFieldCursorCodec
{
	private static bool TryResolve(MuiTextPreParseStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiTextPreParseStateField.Magic => 0,
			MuiTextPreParseStateField.PreParse => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextPreParseStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
			cursor.Record, MuiTextPreParseStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPreParseStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextPreParseStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextPreParseStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextPreParseStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiTextPreParseStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextPreParseStateRecord.Size) ||
			!MuiTextPreParseStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiTextPreParseStateField.Magic, out var magic) ||
			magic != MuiTextPreParseStateRecord.Cookie) return false;
		value.Magic = magic;
		if (!MuiTextPreParseStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextPreParseStateField.PreParse, out var preParse))
			return false;
		value.PreParse = APTR.FromPointer(preParse);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextPreParseStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextPreParseStateRecord.Size) || value.Magic !=
			MuiTextPreParseStateRecord.Cookie) return false;
		return MuiTextPreParseStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextPreParseStateField.Magic, value.Magic) &&
			MuiTextPreParseStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiTextPreParseStateField.PreParse, value.PreParse.Raw);
	}
}
