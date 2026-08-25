/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Text.mui's initializer-only Unicode policy.  The public attribute remains
// a BOOL-shaped ULONG, while the implementation keeps its normalized value in
// a named guest record so metrics and drawing share one semantic source.
public struct MuiTextUnicodeState
{
	public uint Unicode;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextUnicodeStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4D54554Eu; // 'MTUN'

	internal uint Magic;
	internal uint Unicode;
}

internal enum MuiTextUnicodeStateField : byte
{
	Magic,
	Unicode,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiTextUnicodeStateFieldCursor
{
	internal APTR Record;
	internal MuiTextUnicodeStateField Field;
}

internal static class MuiTextUnicodeStateFieldCursorCodec
{
	private static bool TryResolve(MuiTextUnicodeStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiTextUnicodeStateField.Magic => 0,
			MuiTextUnicodeStateField.Unicode => 4,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiTextUnicodeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiTextUnicodeStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextUnicodeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiTextUnicodeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiTextUnicodeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiTextUnicodeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiTextUnicodeStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiTextUnicodeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextUnicodeStateRecord.Size) ||
			!MuiTextUnicodeStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiTextUnicodeStateField.Magic, out var magic) ||
			magic != MuiTextUnicodeStateRecord.Cookie) return false;
		value.Magic = magic;
		return MuiTextUnicodeStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiTextUnicodeStateField.Unicode, out value.Unicode);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiTextUnicodeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiTextUnicodeStateRecord.Size) || value.Magic !=
			MuiTextUnicodeStateRecord.Cookie) return false;
		return MuiTextUnicodeStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextUnicodeStateField.Magic, value.Magic) &&
			MuiTextUnicodeStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiTextUnicodeStateField.Unicode, value.Unicode);
	}
}

// Keep the wire value lossless for malformed-state diagnostics. Text Unicode
// mode is a MorphOS BOOL and must be canonical before metrics or rendering
// consumers use the named policy state.
internal static class MuiTextUnicodeStateValidation
{
	internal static bool IsValidRecord(MuiTextUnicodeStateRecord value) =>
		value.Unicode <= 1;

	internal static bool IsValidState(MuiTextUnicodeState value) =>
		value.Unicode <= 1;
}
