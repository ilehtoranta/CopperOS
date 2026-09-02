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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint TableOffset = 4;
	internal const uint Cookie = 0x41434D53u; // 'ACMS'

	internal uint Magic;
	internal APTR Table;
}

// The command table is caller-owned guest memory. A NULL table means no
// commands; otherwise the existing bounded named command-table codec validates
// every entry and its caller-owned strings. The sidecar also requires a live
// Application before command state can be published or consumed.
internal static class MuiApplicationCommandsStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationCommandsStateRecord.Cookie &&
		MuiApplicationCommandsCore.TryValidate(ref platform, value.Table);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationCommandsStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationCommandsStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationCommandsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationCommandsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed application command-state records are read and written as named
// values. Keep the packed guest positions in this bounded ABI adapter;
// production state consumers do not select numeric slots directly.
internal static class MuiApplicationCommandsStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationCommandsStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationCommandsStateField.Magic:
				offset = MuiApplicationCommandsStateRecord.MagicOffset;
				return true;
			case MuiApplicationCommandsStateField.Table:
				offset = MuiApplicationCommandsStateRecord.TableOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandsStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiApplicationCommandsStateRecord.Size) &&
			platform.IsMapped(address, MuiApplicationCommandsStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandsStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationCommandsStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationCommandsStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationCommandsStateField.Table)
			value = state.Table.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationCommandsStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiApplicationCommandsStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationCommandsStateField.Table)
			state.Table = APTR.FromPointer(value);
		else return false;
		return MuiApplicationCommandsStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationCommandsStateRecordCodec
{
	// Application command state is a fixed two-ULONG capability record. Keep
	// production exchange in declaration order so the command table pointer is
	// carried with its cookie as one named struct; the field adapter remains a
	// bounded compatibility surface for targeted corruption tests.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationCommandsStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var table)) return false;
		value.Table = APTR.FromPointer(table);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationCommandsStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Table.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationCommandsStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationCommandsStateAdmission.Validate(
			ref platform, value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
