/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Owning Application command-table pointer.  The fixed command entries and
// their strings remain caller-owned guest memory; this record publishes only
// the validated table capability without a managed mirror.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationCommandsStateRecord
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x41434D53u; // 'ACMS'

	internal uint Magic;
	internal APTR Table;
}

internal enum MuiApplicationCommandsStateField : byte
{
	Magic,
	Table,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationCommandsStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationCommandsStateField Field;
}

internal static class MuiApplicationCommandsStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationCommandsStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationCommandsStateField.Magic:
			case MuiApplicationCommandsStateField.Table:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationCommandsStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationCommandsStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationCommandsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationCommandsStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationCommandsStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull ||
			!MuiApplicationCommandsStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationCommandsStateField.Magic, out var magic) ||
			magic != MuiApplicationCommandsStateRecord.Cookie ||
			!MuiApplicationCommandsStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationCommandsStateField.Table, out var table))
			return false;
		value.Magic = magic;
		value.Table = APTR.FromPointer(table);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || value.Magic !=
			MuiApplicationCommandsStateRecord.Cookie) return false;
		return MuiApplicationCommandsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationCommandsStateField.Magic,
			value.Magic) &&
			MuiApplicationCommandsStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationCommandsStateField.Table,
			value.Table.Raw);
	}
}
