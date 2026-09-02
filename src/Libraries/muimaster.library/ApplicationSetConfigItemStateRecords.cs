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

// Fixed SetConfigItem state is transferred as a named record. Numeric guest
// positions are confined to the bounded ABI adapter; production consumers
// exchange the declaration-order struct through the sequential cursor below.
internal static class MuiApplicationSetConfigItemStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationSetConfigItemStateField field,
		out uint offset)
	{
		if (field == MuiApplicationSetConfigItemStateField.Magic)
			offset = MuiApplicationSetConfigItemStateRecord.MagicOffset;
		else if (field == MuiApplicationSetConfigItemStateField.Item)
			offset = MuiApplicationSetConfigItemStateRecord.ItemOffset;
		else if (field == MuiApplicationSetConfigItemStateField.Data)
			offset = MuiApplicationSetConfigItemStateRecord.DataOffset;
		else if (field == MuiApplicationSetConfigItemStateField.Requests)
			offset = MuiApplicationSetConfigItemStateRecord.RequestsOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
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
		if (!MuiApplicationSetConfigItemStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiApplicationSetConfigItemStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationSetConfigItemStateField.Item)
			value = state.Item;
		else if (field == MuiApplicationSetConfigItemStateField.Data)
			value = state.Data.Raw;
		else if (field == MuiApplicationSetConfigItemStateField.Requests)
			value = state.Requests;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSetConfigItemStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationSetConfigItemStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiApplicationSetConfigItemStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationSetConfigItemStateField.Item)
			state.Item = value;
		else if (field == MuiApplicationSetConfigItemStateField.Data)
			state.Data = APTR.FromPointer(value);
		else if (field == MuiApplicationSetConfigItemStateField.Requests)
			state.Requests = value;
		else return false;
		return MuiApplicationSetConfigItemStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationSetConfigItemStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationSetConfigItemStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Item) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var data) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Requests)) return false;
		value.Data = APTR.FromPointer(data);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationSetConfigItemStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Item) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Data.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Requests) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationSetConfigItemStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSetConfigItemStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationSetConfigItemStateAdmission.Validate(
			ref platform, value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
