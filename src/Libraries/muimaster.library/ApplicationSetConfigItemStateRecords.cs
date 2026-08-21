/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIM_Application_SetConfigItem private state.  Data is an opaque caller
// pointer: the record retains its APTR value but never dereferences or copies
// the preferences payload.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSetConfigItemStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x41534349u; // 'ASCI'

	internal uint Magic;
	internal uint Item;
	internal APTR Data;
	internal uint Requests;
}

internal enum MuiApplicationSetConfigItemStateField : byte
{
	Magic,
	Item,
	Data,
	Requests,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSetConfigItemStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationSetConfigItemStateField Field;
}

internal static class MuiApplicationSetConfigItemStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationSetConfigItemStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationSetConfigItemStateField.Magic:
			case MuiApplicationSetConfigItemStateField.Item:
			case MuiApplicationSetConfigItemStateField.Data:
			case MuiApplicationSetConfigItemStateField.Requests:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSetConfigItemStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationSetConfigItemStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSetConfigItemStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationSetConfigItemStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSetConfigItemStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationSetConfigItemStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationSetConfigItemStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSetConfigItemStateRecord.Size) ||
			!MuiApplicationSetConfigItemStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSetConfigItemStateField.Magic, out var magic) ||
			magic != MuiApplicationSetConfigItemStateRecord.Cookie ||
			!MuiApplicationSetConfigItemStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSetConfigItemStateField.Item, out value.Item) ||
			!MuiApplicationSetConfigItemStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSetConfigItemStateField.Data, out var data) ||
			!MuiApplicationSetConfigItemStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSetConfigItemStateField.Requests,
				out value.Requests)) return false;
		value.Magic = magic;
		value.Data = APTR.FromPointer(data);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSetConfigItemStateRecord.Size) || value.Magic !=
			MuiApplicationSetConfigItemStateRecord.Cookie) return false;
		return MuiApplicationSetConfigItemStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSetConfigItemStateField.Magic, value.Magic) &&
			MuiApplicationSetConfigItemStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationSetConfigItemStateField.Item, value.Item) &&
			MuiApplicationSetConfigItemStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationSetConfigItemStateField.Data, value.Data.Raw) &&
			MuiApplicationSetConfigItemStateFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationSetConfigItemStateField.Requests, value.Requests);
	}
}
