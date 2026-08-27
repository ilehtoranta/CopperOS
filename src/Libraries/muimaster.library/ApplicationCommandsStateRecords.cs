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
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationCommandsStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationCommandsStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationCommandsStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationCommandsStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationCommandsStateField.Magic,
			out var magic) ||
			!MuiApplicationCommandsStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationCommandsStateField.Table,
				out var table))
			return false;
		value.Magic = magic;
		value.Table = APTR.FromPointer(table);
		return true;
	}

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
		return MuiApplicationCommandsStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationCommandsStateField.Magic,
			value.Magic) &&
			MuiApplicationCommandsStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationCommandsStateField.Table,
				value.Table.Raw);
	}
}
