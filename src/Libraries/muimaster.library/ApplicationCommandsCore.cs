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
	internal const uint FieldSize = 4;
	internal const uint NameOffset = 0;
	internal const uint TemplateOffset = 4;
	internal const uint ParametersOffset = 8;
	internal const uint HookOffset = 12;
	internal const uint Reserved0Offset = 16;
	internal const uint Reserved1Offset = 20;
	internal const uint Reserved2Offset = 24;
	internal const uint Reserved3Offset = 28;
	internal const uint Reserved4Offset = 32;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationCommandFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationCommandRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed MUI_Command records are read and written as named value types. Keep
// the packed guest positions in this bounded ABI adapter; command-table
// consumers never choose a numeric slot through the compatibility surface.
internal static class MuiApplicationCommandRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationCommandField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationCommandField.Name:
				offset = MuiApplicationCommandRecord.NameOffset; return true;
			case MuiApplicationCommandField.Template:
				offset = MuiApplicationCommandRecord.TemplateOffset; return true;
			case MuiApplicationCommandField.Parameters:
				offset = MuiApplicationCommandRecord.ParametersOffset; return true;
			case MuiApplicationCommandField.Hook:
				offset = MuiApplicationCommandRecord.HookOffset; return true;
			case MuiApplicationCommandField.Reserved0:
				offset = MuiApplicationCommandRecord.Reserved0Offset; return true;
			case MuiApplicationCommandField.Reserved1:
				offset = MuiApplicationCommandRecord.Reserved1Offset; return true;
			case MuiApplicationCommandField.Reserved2:
				offset = MuiApplicationCommandRecord.Reserved2Offset; return true;
			case MuiApplicationCommandField.Reserved3:
				offset = MuiApplicationCommandRecord.Reserved3Offset; return true;
			case MuiApplicationCommandField.Reserved4:
				offset = MuiApplicationCommandRecord.Reserved4Offset; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiApplicationCommandRecord.Size) &&
			platform.IsMapped(address, MuiApplicationCommandRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
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
		if (!MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiApplicationCommandField.Name, out var rawName) ||
			!MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiApplicationCommandField.Template, out var rawTemplate) ||
			!MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiApplicationCommandField.Hook, out var rawHook)) return false;
		value.Name = APTR.FromPointer(rawName);
		value.Template = APTR.FromPointer(rawTemplate);
		value.Hook = APTR.FromPointer(rawHook);
		if (!MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiApplicationCommandField.Parameters, out var rawParameters) ||
			!MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiApplicationCommandField.Reserved0, out var rawReserved0) ||
			!MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiApplicationCommandField.Reserved1, out var rawReserved1) ||
			!MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiApplicationCommandField.Reserved2, out var rawReserved2) ||
			!MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiApplicationCommandField.Reserved3, out var rawReserved3) ||
			!MuiApplicationCommandRecordMemoryCodec.TryReadUInt32(ref platform,
				address, MuiApplicationCommandField.Reserved4, out var rawReserved4)) return false;
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
		return MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiApplicationCommandField.Name, value.Name.Raw) &&
			MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationCommandField.Template, value.Template.Raw) &&
			MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationCommandField.Parameters,
				unchecked((uint)value.Parameters)) &&
			MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationCommandField.Hook, value.Hook.Raw) &&
			MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationCommandField.Reserved0,
				unchecked((uint)value.Reserved0)) &&
			MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationCommandField.Reserved1,
				unchecked((uint)value.Reserved1)) &&
			MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationCommandField.Reserved2,
				unchecked((uint)value.Reserved2)) &&
			MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationCommandField.Reserved3,
				unchecked((uint)value.Reserved3)) &&
			MuiApplicationCommandRecordMemoryCodec.TryWriteUInt32(ref platform,
				address, MuiApplicationCommandField.Reserved4,
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
	internal const uint CommandsStateKey = 0x7F0A001Bu;

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
		return MuiApplicationCommandsStateRecordCodec.TryReadStructural(ref platform,
			block, out value) && MuiApplicationCommandsStateAdmission.ValidateLive(
			ref platform, state, application, value);
	}

	private static bool HasApplicationCommandsStateStorage<TPlatform>(
		ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiStoreCore.DataspaceLength(ref platform, state, application,
			CommandsStateKey) != 0;

	internal static bool ApplicationCommandsStateAvailable<TPlatform>(
		ref TPlatform platform, APTR state, APTR application)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		!HasApplicationCommandsStateStorage(ref platform, state, application) ||
		TryGetApplicationCommandsState(ref platform, state, application, out _);

	private static bool PublishApplicationCommandsState<TPlatform>(
		ref TPlatform platform, APTR state, APTR application,
		out MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, application,
			CommandsStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, application,
			CommandsStateKey);
		if (length != 0 && length != unchecked((int)
			MuiApplicationCommandsStateRecord.Size)) return false;
		if (length != 0)
		{
			if (!MuiApplicationCommandsStateRecordCodec.TryReadStructural(ref platform,
				block, out value) ||
				!MuiApplicationCommandsStateAdmission.ValidateLive(ref platform, state,
					application, value)) return false;
			FillApplicationCommandsState(ref platform, state, application,
				ref value);
			if (!MuiApplicationCommandsStateAdmission.ValidateLive(ref platform, state,
				application, value)) return false;
			return MuiApplicationCommandsStateRecordCodec.Write(ref platform,
				block, value);
		}

		value = default;
		value.Magic = MuiApplicationCommandsStateRecord.Cookie;
		FillApplicationCommandsState(ref platform, state, application, ref value);
		if (!MuiApplicationCommandsStateAdmission.ValidateLive(ref platform, state,
			application, value)) return false;
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
		if (!ApplicationCommandsStateAvailable(ref platform, state, owner))
			return false;
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
