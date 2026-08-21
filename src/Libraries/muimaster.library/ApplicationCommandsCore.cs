/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MorphOS exposes MUIA_Application_Commands as a caller-owned, NULL-terminated
// array of these fixed-width records.  The command table remains in guest
// memory; this codec is the only place that crosses its packed ABI boundary.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationCommandRecord
{
	internal const uint Size = 36;
	internal APTR Name;
	internal APTR Template;
	internal int Parameters;
	internal APTR Hook;
	internal int Reserved0;
	internal int Reserved1;
	internal int Reserved2;
	internal int Reserved3;
	internal int Reserved4;
}

internal enum MuiApplicationCommandField : byte
{
	Name,
	Template,
	Parameters,
	Hook,
	Reserved0,
	Reserved1,
	Reserved2,
	Reserved3,
	Reserved4,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationCommandFieldCursor
{
	internal APTR Record;
	internal MuiApplicationCommandField Field;
}

internal static class MuiApplicationCommandFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationCommandField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationCommandField.Name:
				offset = 0;
				break;
			case MuiApplicationCommandField.Template:
				offset = 4;
				break;
			case MuiApplicationCommandField.Parameters:
				offset = 8;
				break;
			case MuiApplicationCommandField.Hook:
				offset = 12;
				break;
			case MuiApplicationCommandField.Reserved0:
				offset = 16;
				break;
			case MuiApplicationCommandField.Reserved1:
				offset = 20;
				break;
			case MuiApplicationCommandField.Reserved2:
				offset = 24;
				break;
			case MuiApplicationCommandField.Reserved3:
				offset = 28;
				break;
			case MuiApplicationCommandField.Reserved4:
				offset = 32;
				break;
			default:
				offset = 0;
				return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationCommandFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationCommandFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationCommandFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationCommandRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationCommandRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationCommandRecord.Size)) return false;
		if (!MuiApplicationCommandFieldCursorCodec.TryRead(ref platform, address,
			MuiApplicationCommandField.Name, out var rawName) ||
			!MuiApplicationCommandFieldCursorCodec.TryRead(ref platform, address,
				MuiApplicationCommandField.Template, out var rawTemplate) ||
			!MuiApplicationCommandFieldCursorCodec.TryRead(ref platform, address,
				MuiApplicationCommandField.Hook, out var rawHook)) return false;
		value.Name = APTR.FromPointer(rawName);
		value.Template = APTR.FromPointer(rawTemplate);
		value.Hook = APTR.FromPointer(rawHook);
		if (!MuiApplicationCommandFieldCursorCodec.TryRead(ref platform, address,
			MuiApplicationCommandField.Parameters, out var rawParameters) ||
			!MuiApplicationCommandFieldCursorCodec.TryRead(ref platform, address,
				MuiApplicationCommandField.Reserved0, out var rawReserved0) ||
			!MuiApplicationCommandFieldCursorCodec.TryRead(ref platform, address,
				MuiApplicationCommandField.Reserved1, out var rawReserved1) ||
			!MuiApplicationCommandFieldCursorCodec.TryRead(ref platform, address,
				MuiApplicationCommandField.Reserved2, out var rawReserved2) ||
			!MuiApplicationCommandFieldCursorCodec.TryRead(ref platform, address,
				MuiApplicationCommandField.Reserved3, out var rawReserved3) ||
			!MuiApplicationCommandFieldCursorCodec.TryRead(ref platform, address,
				MuiApplicationCommandField.Reserved4, out var rawReserved4)) return false;
		value.Parameters = unchecked((int)rawParameters);
		value.Reserved0 = unchecked((int)rawReserved0);
		value.Reserved1 = unchecked((int)rawReserved1);
		value.Reserved2 = unchecked((int)rawReserved2);
		value.Reserved3 = unchecked((int)rawReserved3);
		value.Reserved4 = unchecked((int)rawReserved4);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationCommandRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationCommandRecord.Size)) return false;
		return MuiApplicationCommandFieldCursorCodec.TryWrite(ref platform, address,
			MuiApplicationCommandField.Name, value.Name.Raw) &&
			MuiApplicationCommandFieldCursorCodec.TryWrite(ref platform, address,
				MuiApplicationCommandField.Template, value.Template.Raw) &&
			MuiApplicationCommandFieldCursorCodec.TryWrite(ref platform, address,
				MuiApplicationCommandField.Parameters,
				unchecked((uint)value.Parameters)) &&
			MuiApplicationCommandFieldCursorCodec.TryWrite(ref platform, address,
				MuiApplicationCommandField.Hook, value.Hook.Raw) &&
			MuiApplicationCommandFieldCursorCodec.TryWrite(ref platform, address,
				MuiApplicationCommandField.Reserved0,
				unchecked((uint)value.Reserved0)) &&
			MuiApplicationCommandFieldCursorCodec.TryWrite(ref platform, address,
				MuiApplicationCommandField.Reserved1,
				unchecked((uint)value.Reserved1)) &&
			MuiApplicationCommandFieldCursorCodec.TryWrite(ref platform, address,
				MuiApplicationCommandField.Reserved2,
				unchecked((uint)value.Reserved2)) &&
			MuiApplicationCommandFieldCursorCodec.TryWrite(ref platform, address,
				MuiApplicationCommandField.Reserved3,
				unchecked((uint)value.Reserved3)) &&
			MuiApplicationCommandFieldCursorCodec.TryWrite(ref platform, address,
				MuiApplicationCommandField.Reserved4,
				unchecked((uint)value.Reserved4));
	}
}

public static class MuiApplicationCommandsCore
{
	public const uint Commands = 0x80428648;
	public const uint MagicTemplate = 0xFFFFFFFF;

	// Commands are projected from the named caller-owned command-table state.
	// Keep the generic OM_GET admission predicate beside that typed projection.
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == Commands;

	// The public table has no count field, so validation is bounded by the same
	// guest traversal ceiling used by every other MUI list/vector walk.
	private const uint MaximumStringLength = 65536;
	private const uint CommandsStateKey = 0x7F0A001Bu;

	internal static bool TryGetApplicationCommandsState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			CommandsStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, application,
			CommandsStateKey) != unchecked((int)
			MuiApplicationCommandsStateRecord.Size)) return false;
		return MuiApplicationCommandsStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool PublishApplicationCommandsState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			CommandsStateKey);
		if (TryGetApplicationCommandsState(ref platform, state, application,
			out value))
		{
			FillApplicationCommandsState(ref platform, state, application,
				ref value);
			return MuiApplicationCommandsStateRecordCodec.Write(ref platform,
				block, value);
		}

		value = default;
		value.Magic = MuiApplicationCommandsStateRecord.Cookie;
		FillApplicationCommandsState(ref platform, state, application, ref value);
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiApplicationCommandsStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiApplicationCommandsStateRecord.Size);
		var written = MuiApplicationCommandsStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state,
			application, CommandsStateKey, scratch,
			unchecked((int)MuiApplicationCommandsStateRecord.Size));
		platform.Clear(scratch, MuiApplicationCommandsStateRecord.Size);
		platform.Free(scratch, MuiApplicationCommandsStateRecord.Size);
		return added;
	}

	private static void FillApplicationCommandsState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		ref MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, application,
			Commands, out var table)) table = 0;
		value.Table = APTR.FromPointer(table);
	}

	internal static bool TryGet<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		handled = attribute == Commands;
		if (!handled) return false;
		if (!PublishApplicationCommandsState(ref platform, state, obj,
			out var record)) return false;
		value = record.Table.Raw;
		return true;
	}

	internal static bool TrySet<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify, out bool handled)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		handled = attribute == Commands;
		if (!handled) return false;
		return SetValidated(ref platform, state, record, value, notify);
	}

	public static bool SetApplicationCommandsValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR application, uint value)
		where TPlatform : struct, IMuiApplicationPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state,
			application);
		return record.IsNotNull && SetValidated(ref platform, state, record, value,
			false);
	}

	internal static bool TryValidate<TPlatform>(ref TPlatform platform,
		APTR table) where TPlatform : struct, IMuiGuestMemory
	{
		if (table.IsNull) return true;
		var cursor = default(MuiApplicationCommandTableCursor);
		cursor.Base = table;
		while (cursor.Index < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiApplicationCommandTableCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			if (!MuiApplicationCommandRecordCodec.TryRead(ref platform, address,
				out var command)) return false;
			// A NULL name terminates the caller-owned command table.  The
			// remaining fields are intentionally not interpreted at this boundary.
			if (command.Name.IsNull) return true;
			if (!CStringCodec.TryReadLength(ref platform, command.Name,
				MaximumStringLength, out _)) return false;
			if (command.Template.IsNotNull &&
				command.Template.Raw != MagicTemplate &&
				!CStringCodec.TryReadLength(ref platform, command.Template,
					MaximumStringLength, out _)) return false;
			cursor.Index++;
		}
		return false;
	}

	private static bool SetValidated<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiApplicationCommandTableCodec.TryValidate(ref platform,
			APTR.FromPointer(value))) return false;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue) || objectValue.Boopsi.IsNull) return false;
		var owner = objectValue.Boopsi;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, record,
			Commands, out var previous)) previous = 0;
		if (!MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state,
			record, Commands, value, notify)) return false;
		if (PublishApplicationCommandsState(ref platform, state, owner,
			out _)) return true;
		MuiHeadlessObjectCore.SetRecordAttributeRaw(ref platform, state, record,
			Commands, previous, false);
		PublishApplicationCommandsState(ref platform, state, owner, out _);
		return false;
	}
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationCommandTableCursor
{
	internal const uint EntrySize = MuiApplicationCommandRecord.Size;
	internal APTR Base;
	internal uint Index;
}

internal static class MuiApplicationCommandTableCodec
{
	internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
		MuiApplicationCommandTableCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (cursor.Base.IsNull || cursor.Index >
			(uint.MaxValue - cursor.Base.Raw) /
			MuiApplicationCommandTableCursor.EntrySize) return false;
		var offset = cursor.Index *
			MuiApplicationCommandTableCursor.EntrySize;
		if (cursor.Base.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Base.Raw + offset);
		return platform.IsMapped(address,
			MuiApplicationCommandTableCursor.EntrySize);
	}

	internal static bool TryValidate<TPlatform>(ref TPlatform platform, APTR table)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiApplicationCommandsCore.TryValidate(ref platform, table);
}
