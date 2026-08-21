/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Application menu event state. MenuAction is the selected item UserData;
// MenuHelp is the getter-only help UserData published by menu transport.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMenuStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x414D5354u; // 'AMST'

	internal uint Magic;
	internal uint MenuAction;
	internal uint MenuHelp;
}

internal enum MuiApplicationMenuStateField : byte
{
	Magic,
	MenuAction,
	MenuHelp,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationMenuStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationMenuStateField Field;
}

internal static class MuiApplicationMenuStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationMenuStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationMenuStateField.Magic:
			case MuiApplicationMenuStateField.MenuAction:
			case MuiApplicationMenuStateField.MenuHelp:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationMenuStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationMenuStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMenuStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationMenuStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationMenuStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationMenuStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationMenuStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationMenuStateRecord.Size) ||
			!MuiApplicationMenuStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationMenuStateField.Magic, out var magic) ||
			magic != MuiApplicationMenuStateRecord.Cookie ||
			!MuiApplicationMenuStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationMenuStateField.MenuAction,
				out value.MenuAction) ||
			!MuiApplicationMenuStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationMenuStateField.MenuHelp,
				out value.MenuHelp)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationMenuStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationMenuStateRecord.Size) || value.Magic !=
			MuiApplicationMenuStateRecord.Cookie) return false;
		return MuiApplicationMenuStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationMenuStateField.Magic,
			value.Magic) &&
			MuiApplicationMenuStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationMenuStateField.MenuAction,
			value.MenuAction) &&
			MuiApplicationMenuStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationMenuStateField.MenuHelp,
			value.MenuHelp);
	}
}
