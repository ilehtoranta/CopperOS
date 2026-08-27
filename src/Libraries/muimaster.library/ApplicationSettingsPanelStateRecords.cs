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

// Fixed BuildSettingsPanel result state is read and written as a named value.
// Keep packed guest positions in this ABI adapter; production consumers do not
// select fields through the compatibility cursor.
internal static class MuiApplicationSettingsPanelStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationSettingsPanelStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationSettingsPanelStateField.Magic:
				offset = MuiApplicationSettingsPanelStateRecord.MagicOffset;
				return true;
			case MuiApplicationSettingsPanelStateField.Number:
				offset = MuiApplicationSettingsPanelStateRecord.NumberOffset;
				return true;
			case MuiApplicationSettingsPanelStateField.Panel:
				offset = MuiApplicationSettingsPanelStateRecord.PanelOffset;
				return true;
			case MuiApplicationSettingsPanelStateField.Requests:
				offset = MuiApplicationSettingsPanelStateRecord.RequestsOffset;
				return true;
		}
		offset = 0;
		return false;
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
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPanelStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationSettingsPanelStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationSettingsPanelStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationSettingsPanelStateField.Magic,
			out var magic) ||
			!MuiApplicationSettingsPanelStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSettingsPanelStateField.Number,
				out value.Number) ||
			!MuiApplicationSettingsPanelStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSettingsPanelStateField.Panel,
				out var panel) ||
			!MuiApplicationSettingsPanelStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationSettingsPanelStateField.Requests,
				out value.Requests)) return false;
		value.Magic = magic;
		value.Panel = APTR.FromPointer(panel);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationSettingsPanelStateAdmission.Validate(ref platform, value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSettingsPanelStateRecord.Size) ||
			!MuiApplicationSettingsPanelStateAdmission.Validate(ref platform, value))
			return false;
		return MuiApplicationSettingsPanelStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationSettingsPanelStateField.Magic,
			value.Magic) &&
			MuiApplicationSettingsPanelStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationSettingsPanelStateField.Number,
				value.Number) &&
			MuiApplicationSettingsPanelStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationSettingsPanelStateField.Panel,
				value.Panel.Raw) &&
			MuiApplicationSettingsPanelStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationSettingsPanelStateField.Requests,
				value.Requests);
	}
}
