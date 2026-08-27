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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ItemOffset = 4;
	internal const uint DataOffset = 8;
	internal const uint RequestsOffset = 12;
	internal const uint Cookie = 0x41534349u; // 'ASCI'

	internal uint Magic;
	internal uint Item;
	internal APTR Data;
	internal uint Requests;
}

// SetConfigItem deliberately does not interpret the preferences payload. A
// NULL payload is valid; a non-NULL payload must still identify one mapped
// guest byte before the caller-owned APTR is retained. The record also needs a
// live Application owner before it is published or consumed.
internal static class MuiApplicationSetConfigItemStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationSetConfigItemStateRecord.Cookie &&
		(value.Data.IsNull || platform.IsMapped(value.Data, 1));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application,
		MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(ref platform, value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSetConfigItemStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSetConfigItemStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSetConfigItemStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSetConfigItemStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSetConfigItemStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSetConfigItemStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed SetConfigItem state is read and written as a named value. Keep packed
// guest positions in this bounded ABI adapter; production consumers do not
// select numeric slots directly.
internal static class MuiApplicationSetConfigItemStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationSetConfigItemStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationSetConfigItemStateField.Magic:
				offset = MuiApplicationSetConfigItemStateRecord.MagicOffset;
				return true;
			case MuiApplicationSetConfigItemStateField.Item:
				offset = MuiApplicationSetConfigItemStateRecord.ItemOffset;
				return true;
			case MuiApplicationSetConfigItemStateField.Data:
				offset = MuiApplicationSetConfigItemStateRecord.DataOffset;
				return true;
			case MuiApplicationSetConfigItemStateField.Requests:
				offset = MuiApplicationSetConfigItemStateRecord.RequestsOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSetConfigItemStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record,
			MuiApplicationSetConfigItemStateRecord.Size) &&
			platform.IsMapped(address,
				MuiApplicationSetConfigItemStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSetConfigItemStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSetConfigItemStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationSetConfigItemStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationSetConfigItemStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationSetConfigItemStateField.Magic,
			out var magic) ||
			!MuiApplicationSetConfigItemStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSetConfigItemStateField.Item,
				out value.Item) ||
			!MuiApplicationSetConfigItemStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSetConfigItemStateField.Data,
				out var data) ||
			!MuiApplicationSetConfigItemStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSetConfigItemStateField.Requests, out value.Requests))
			return false;
		value.Magic = magic;
		value.Data = APTR.FromPointer(data);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationSetConfigItemStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSetConfigItemStateRecord.Size) ||
			!MuiApplicationSetConfigItemStateAdmission.Validate(ref platform, value))
			return false;
		return MuiApplicationSetConfigItemStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSetConfigItemStateField.Magic,
			value.Magic) &&
			MuiApplicationSetConfigItemStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationSetConfigItemStateField.Item,
				value.Item) &&
			MuiApplicationSetConfigItemStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationSetConfigItemStateField.Data,
				value.Data.Raw) &&
			MuiApplicationSetConfigItemStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationSetConfigItemStateField.Requests, value.Requests);
	}
}
