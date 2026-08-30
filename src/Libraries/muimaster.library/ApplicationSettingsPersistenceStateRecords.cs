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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint OperationOffset = 4;
	internal const uint NameOffset = 8;
	internal const uint RequestsOffset = 12;
	internal const uint SavesOffset = 16;
	internal const uint LoadsOffset = 20;
	internal const uint Cookie = 0x41505354u; // 'APST'

	internal uint Magic;
	internal uint Operation;
	internal APTR Name;
	internal uint Requests;
	internal uint Saves;
	internal uint Loads;
}

// Save/Load retains MorphOS's two sentinel selectors: Null selects the
// default environment and ULONG(-1) selects ENVARC. Any other selector remains
// a bounded caller-owned C string. Operation is the explicit Save/Load BOOL.
internal static class MuiApplicationSettingsPersistenceStateAdmission
{
	private const uint MaximumStringLength = 65536;

	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationSettingsPersistenceStateRecord.Cookie &&
		value.Operation <= 1 &&
		(value.Name.IsNull || value.Name.Raw == uint.MaxValue ||
			CStringCodec.TryReadLength(ref platform, value.Name,
				MaximumStringLength, out _));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application,
		MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSettingsPersistenceStateFieldCursor cursor,
		out APTR address) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSettingsPersistenceStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPersistenceStateField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSettingsPersistenceStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPersistenceStateField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSettingsPersistenceStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed application settings persistence state is transferred as a named
// record. Numeric guest positions are confined to this ABI adapter; the
// compatibility cursor above remains available only to legacy callers and
// malformed-state diagnostics.
internal static class MuiApplicationSettingsPersistenceStateRecordMemoryCodec
{
	private static bool TryResolve(
		MuiApplicationSettingsPersistenceStateField field, out uint offset)
	{
		switch (field)
		{
			case MuiApplicationSettingsPersistenceStateField.Magic:
				offset = MuiApplicationSettingsPersistenceStateRecord.MagicOffset;
				return true;
			case MuiApplicationSettingsPersistenceStateField.Operation:
				offset = MuiApplicationSettingsPersistenceStateRecord.OperationOffset;
				return true;
			case MuiApplicationSettingsPersistenceStateField.Name:
				offset = MuiApplicationSettingsPersistenceStateRecord.NameOffset;
				return true;
			case MuiApplicationSettingsPersistenceStateField.Requests:
				offset = MuiApplicationSettingsPersistenceStateRecord.RequestsOffset;
				return true;
			case MuiApplicationSettingsPersistenceStateField.Saves:
				offset = MuiApplicationSettingsPersistenceStateRecord.SavesOffset;
				return true;
			case MuiApplicationSettingsPersistenceStateField.Loads:
				offset = MuiApplicationSettingsPersistenceStateRecord.LoadsOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPersistenceStateField field,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record,
			MuiApplicationSettingsPersistenceStateRecord.Size) &&
			platform.IsMapped(address,
				MuiApplicationSettingsPersistenceStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPersistenceStateField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPersistenceStateField field,
		uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationSettingsPersistenceStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationSettingsPersistenceStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Operation) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var name) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Requests) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Saves) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Loads)) return false;
		value.Name = APTR.FromPointer(name);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationSettingsPersistenceStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Operation) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Name.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Requests) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Saves) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Loads) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationSettingsPersistenceStateAdmission.Validate(ref platform,
			value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSettingsPersistenceStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationSettingsPersistenceStateAdmission
			.Validate(ref platform, value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
