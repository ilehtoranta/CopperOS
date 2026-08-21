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
	internal const uint Cookie = 0x41535054u; // 'ASPT'

	internal uint Magic;
	internal uint Number;
	internal APTR Panel;
	internal uint Requests;
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
	private static bool TryResolve(MuiApplicationSettingsPanelStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationSettingsPanelStateField.Magic:
			case MuiApplicationSettingsPanelStateField.Number:
			case MuiApplicationSettingsPanelStateField.Panel:
			case MuiApplicationSettingsPanelStateField.Requests:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationSettingsPanelStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationSettingsPanelStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPanelStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationSettingsPanelStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationSettingsPanelStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationSettingsPanelStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationSettingsPanelStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSettingsPanelStateRecord.Size) ||
			!MuiApplicationSettingsPanelStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSettingsPanelStateField.Magic, out var magic) ||
			magic != MuiApplicationSettingsPanelStateRecord.Cookie ||
			!MuiApplicationSettingsPanelStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSettingsPanelStateField.Number, out value.Number) ||
			!MuiApplicationSettingsPanelStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSettingsPanelStateField.Panel, out var panel) ||
			!MuiApplicationSettingsPanelStateFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationSettingsPanelStateField.Requests,
				out value.Requests)) return false;
		value.Magic = magic;
		value.Panel = APTR.FromPointer(panel);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationSettingsPanelStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationSettingsPanelStateRecord.Size) || value.Magic !=
			MuiApplicationSettingsPanelStateRecord.Cookie) return false;
		return MuiApplicationSettingsPanelStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSettingsPanelStateField.Magic, value.Magic) &&
			MuiApplicationSettingsPanelStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSettingsPanelStateField.Number, value.Number) &&
			MuiApplicationSettingsPanelStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSettingsPanelStateField.Panel, value.Panel.Raw) &&
			MuiApplicationSettingsPanelStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationSettingsPanelStateField.Requests, value.Requests);
	}
}
