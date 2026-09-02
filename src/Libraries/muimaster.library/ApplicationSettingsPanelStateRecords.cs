/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// BuildSettingsPanel request state. Number retains the MorphOS ULONG request,
// Panel is the returned guest object pointer (or Null), and Requests counts
// accepted calls with saturating ULONG semantics.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsPanelStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint NumberOffset = 4;
	internal const uint PanelOffset = 8;
	internal const uint RequestsOffset = 12;
	internal const uint Cookie = 0x41535054u; // 'ASPT'

	internal uint Magic;
	internal uint Number;
	internal APTR Panel;
	internal uint Requests;
}

// BuildSettingsPanel retains the requested number and the platform's returned
// MUI object capability. A null panel is valid; a non-null panel must be a
// mapped guest pointer and, for live admission, a live MUI object.
internal static class MuiApplicationSettingsPanelStateAdmission
{
	internal static bool Validate<TPlatform>(ref TPlatform platform,
		MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Magic == MuiApplicationSettingsPanelStateRecord.Cookie &&
		(value.Panel.IsNull || platform.IsMapped(value.Panel, 1));

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application,
		MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!Validate(ref platform, value) ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull)
			return false;
		return value.Panel.IsNull ||
			!MuiHeadlessObjectCore.FindObject(ref platform, state,
				value.Panel).IsNull;
	}
}

internal enum MuiApplicationSettingsPanelStateField : byte
{
	Magic,
	Number,
	Panel,
	Requests,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationSettingsPanelStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationSettingsPanelStateField Field;
}

internal static class MuiApplicationSettingsPanelStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSettingsPanelStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSettingsPanelStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPanelStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSettingsPanelStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPanelStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationSettingsPanelStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed BuildSettingsPanel result state is transferred as a named record.
// Numeric guest positions are confined to the bounded ABI adapter; production
// consumers exchange the declaration-order struct through the sequential
// cursor below.
internal static class MuiApplicationSettingsPanelStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationSettingsPanelStateField field,
		out uint offset)
	{
		if (field == MuiApplicationSettingsPanelStateField.Magic)
			offset = MuiApplicationSettingsPanelStateRecord.MagicOffset;
		else if (field == MuiApplicationSettingsPanelStateField.Number)
			offset = MuiApplicationSettingsPanelStateRecord.NumberOffset;
		else if (field == MuiApplicationSettingsPanelStateField.Panel)
			offset = MuiApplicationSettingsPanelStateRecord.PanelOffset;
		else if (field == MuiApplicationSettingsPanelStateField.Requests)
			offset = MuiApplicationSettingsPanelStateRecord.RequestsOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPanelStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record,
			MuiApplicationSettingsPanelStateRecord.Size) &&
			platform.IsMapped(address,
				MuiApplicationSettingsPanelStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPanelStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiApplicationSettingsPanelStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiApplicationSettingsPanelStateField.Magic)
			value = state.Magic;
		else if (field == MuiApplicationSettingsPanelStateField.Number)
			value = state.Number;
		else if (field == MuiApplicationSettingsPanelStateField.Panel)
			value = state.Panel.Raw;
		else if (field == MuiApplicationSettingsPanelStateField.Requests)
			value = state.Requests;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPanelStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiApplicationSettingsPanelStateRecordCodec.TryReadStructural(
			ref platform, record, out var state)) return false;
		if (field == MuiApplicationSettingsPanelStateField.Magic)
			state.Magic = value;
		else if (field == MuiApplicationSettingsPanelStateField.Number)
			state.Number = value;
		else if (field == MuiApplicationSettingsPanelStateField.Panel)
			state.Panel = APTR.FromPointer(value);
		else if (field == MuiApplicationSettingsPanelStateField.Requests)
			state.Requests = value;
		else return false;
		return MuiApplicationSettingsPanelStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiApplicationSettingsPanelStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationSettingsPanelStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Number) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var panel) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Requests)) return false;
		value.Panel = APTR.FromPointer(panel);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiApplicationSettingsPanelStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Number) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Panel.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Requests) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationSettingsPanelStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiApplicationSettingsPanelStateAdmission.Validate(
			ref platform, value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
