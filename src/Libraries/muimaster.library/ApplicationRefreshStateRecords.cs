/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// CheckRefresh telemetry. Checks is the saturating number of accepted checks;
// RefreshedWindows is the number of live native windows refreshed by the last
// accepted call.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationRefreshStateRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x41524654u; // 'ARFT'

	internal uint Magic;
	internal uint Checks;
	internal uint RefreshedWindows;
}

internal enum MuiApplicationRefreshStateField : byte
{
	Magic,
	Checks,
	RefreshedWindows,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiApplicationRefreshStateFieldCursor
{
	internal APTR Record;
	internal MuiApplicationRefreshStateField Field;
}

internal static class MuiApplicationRefreshStateFieldCursorCodec
{
	private static bool TryResolve(MuiApplicationRefreshStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationRefreshStateField.Magic:
			case MuiApplicationRefreshStateField.Checks:
			case MuiApplicationRefreshStateField.RefreshedWindows:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationRefreshStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiApplicationRefreshStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationRefreshStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiApplicationRefreshStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationRefreshStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiApplicationRefreshStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationRefreshStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationRefreshStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationRefreshStateRecord.Size) ||
			!MuiApplicationRefreshStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationRefreshStateField.Magic, out var magic) ||
			magic != MuiApplicationRefreshStateRecord.Cookie ||
			!MuiApplicationRefreshStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationRefreshStateField.Checks, out value.Checks) ||
			!MuiApplicationRefreshStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiApplicationRefreshStateField.RefreshedWindows,
				out value.RefreshedWindows)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationRefreshStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationRefreshStateRecord.Size) || value.Magic !=
			MuiApplicationRefreshStateRecord.Cookie) return false;
		return MuiApplicationRefreshStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationRefreshStateField.Magic,
			value.Magic) &&
			MuiApplicationRefreshStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationRefreshStateField.Checks,
			value.Checks) &&
			MuiApplicationRefreshStateFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiApplicationRefreshStateField.RefreshedWindows,
			value.RefreshedWindows);
	}
}
