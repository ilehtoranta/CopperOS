/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Save/Load settings state. Operation is 1 for Save and 0 for Load. Name is
// the caller-owned C-string selector, including MorphOS's Null and -1 ENV /
// ENVARC sentinels. The counters use saturating ULONG semantics.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsPersistenceStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x41505354u; // 'APST'

	internal uint Magic;
	internal uint Operation;
	internal APTR Name;
	internal uint Requests;
	internal uint Saves;
	internal uint Loads;
}

internal enum MuiApplicationSettingsPersistenceStateField : byte
{
	Magic,
	Operation,
	Name,
	Requests,
	Saves,
	Loads,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsPersistenceStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationSettingsPersistenceStateField Field;
}

internal static class MuiApplicationSettingsPersistenceStateFieldCursorCodec
{
	private static bool TryResolve(
		MuiApplicationSettingsPersistenceStateField field, out uint offset)
	{
		switch (field)
		{
			case MuiApplicationSettingsPersistenceStateField.Magic:
			case MuiApplicationSettingsPersistenceStateField.Operation:
			case MuiApplicationSettingsPersistenceStateField.Name:
			case MuiApplicationSettingsPersistenceStateField.Requests:
			case MuiApplicationSettingsPersistenceStateField.Saves:
			case MuiApplicationSettingsPersistenceStateField.Loads:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSettingsPersistenceStateFieldCursor cursor,
		out APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationSettingsPersistenceStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPersistenceStateField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationSettingsPersistenceStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPersistenceStateField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationSettingsPersistenceStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationSettingsPersistenceStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSettingsPersistenceStateRecord.Size) ||
			!MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSettingsPersistenceStateField.Magic, out var magic) ||
			magic != MuiApplicationSettingsPersistenceStateRecord.Cookie ||
			!MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSettingsPersistenceStateField.Operation,
				out value.Operation) ||
			!MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSettingsPersistenceStateField.Name, out var name) ||
			!MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSettingsPersistenceStateField.Requests,
				out value.Requests) ||
			!MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSettingsPersistenceStateField.Saves,
				out value.Saves) ||
			!MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSettingsPersistenceStateField.Loads,
				out value.Loads)) return false;
		value.Magic = magic;
		value.Name = APTR.FromPointer(name);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSettingsPersistenceStateRecord.Size) || value.Magic !=
			MuiApplicationSettingsPersistenceStateRecord.Cookie) return false;
		return MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSettingsPersistenceStateField.Magic, value.Magic) &&
			MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSettingsPersistenceStateField.Operation,
			value.Operation) &&
			MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSettingsPersistenceStateField.Name, value.Name.Raw) &&
			MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSettingsPersistenceStateField.Requests,
			value.Requests) &&
			MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSettingsPersistenceStateField.Saves, value.Saves) &&
			MuiApplicationSettingsPersistenceStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSettingsPersistenceStateField.Loads, value.Loads);
	}
}
