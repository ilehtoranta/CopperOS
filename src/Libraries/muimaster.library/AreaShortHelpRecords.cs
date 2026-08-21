/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_ShortHelp is an opaque OBString pointer. Keep the public seam as a
// value type so a future bubble service can consume the guest object without
// exposing a managed string or a private Area offset.
public struct MuiAreaShortHelpStateInput
{
	public APTR Text;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaShortHelpStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x41534850u; // 'ASHP'

	internal uint Magic;
	internal APTR Text;
	internal uint Generation;
}

internal enum MuiAreaShortHelpStateField : byte
{
	Magic,
	Text,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaShortHelpStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaShortHelpStateField Field;
}

internal static class MuiAreaShortHelpStateFieldCursorCodec
{
	private static bool TryResolve(MuiAreaShortHelpStateField field,
		out uint offset)

	{
		switch (field)
		{
			case MuiAreaShortHelpStateField.Magic:
				offset = 0;
				return true;
			case MuiAreaShortHelpStateField.Text:
				offset = 4;
				return true;
			case MuiAreaShortHelpStateField.Generation:
				offset = 8;
				return true;
			default:
				offset = 0;
				return false;
		}
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaShortHelpStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiAreaShortHelpStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaShortHelpStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaShortHelpStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaShortHelpStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiAreaShortHelpStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaShortHelpStateRecord.Size) ||
			!MuiAreaShortHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaShortHelpStateField.Magic, out var magic) ||
			magic != MuiAreaShortHelpStateRecord.Cookie ||
			!MuiAreaShortHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaShortHelpStateField.Text, out var text) ||
			!MuiAreaShortHelpStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiAreaShortHelpStateField.Generation,
				out value.Generation)) return false;
		value.Magic = magic;
		value.Text = APTR.FromPointer(text);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaShortHelpStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiAreaShortHelpStateRecord.Size) || value.Magic !=
			MuiAreaShortHelpStateRecord.Cookie) return false;
		return MuiAreaShortHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiAreaShortHelpStateField.Magic, value.Magic) &&
			MuiAreaShortHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaShortHelpStateField.Text, value.Text.Raw) &&
			MuiAreaShortHelpStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiAreaShortHelpStateField.Generation,
				value.Generation);
	}
}
