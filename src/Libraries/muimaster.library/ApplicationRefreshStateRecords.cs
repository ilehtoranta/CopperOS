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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ChecksOffset = 4;
	internal const uint RefreshedWindowsOffset = 8;
	internal const uint Cookie = 0x41524654u; // 'ARFT'

	internal uint Magic;
	internal uint Checks;
	internal uint RefreshedWindows;
}

// CheckRefresh telemetry is meaningful only for a live Application. The two
// counters retain their complete MorphOS ULONG range; only the record cookie
// and owner capability are admitted here.
internal static class MuiApplicationRefreshStateAdmission
{
	internal static bool Validate(MuiApplicationRefreshStateRecord value) =>
		value.Magic == MuiApplicationRefreshStateRecord.Cookie;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR application, MuiApplicationRefreshStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, application).IsNull;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiApplicationRefreshStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationRefreshStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationRefreshStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationRefreshStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationRefreshStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiApplicationRefreshStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Fixed CheckRefresh telemetry is read and written as a named value. Keep the
// packed guest positions in this ABI adapter; production consumers do not
// select fields through the compatibility cursor.
internal static class MuiApplicationRefreshStateRecordMemoryCodec
{
	private static bool TryResolve(MuiApplicationRefreshStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiApplicationRefreshStateField.Magic:
				offset = MuiApplicationRefreshStateRecord.MagicOffset;
				return true;
			case MuiApplicationRefreshStateField.Checks:
				offset = MuiApplicationRefreshStateRecord.ChecksOffset;
				return true;
			case MuiApplicationRefreshStateField.RefreshedWindows:
				offset = MuiApplicationRefreshStateRecord.RefreshedWindowsOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationRefreshStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiApplicationRefreshStateRecord.Size) &&
			platform.IsMapped(address, MuiApplicationRefreshStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationRefreshStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiApplicationRefreshStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiApplicationRefreshStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiApplicationRefreshStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiApplicationRefreshStateRecordMemoryCodec.TryReadUInt32(
			ref platform, address, MuiApplicationRefreshStateField.Magic,
			out var magic) ||
			!MuiApplicationRefreshStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address, MuiApplicationRefreshStateField.Checks,
				out value.Checks) ||
			!MuiApplicationRefreshStateRecordMemoryCodec.TryReadUInt32(
				ref platform, address,
				MuiApplicationRefreshStateField.RefreshedWindows,
				out value.RefreshedWindows)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiApplicationRefreshStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiApplicationRefreshStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiApplicationRefreshStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiApplicationRefreshStateRecord.Size) ||
			!MuiApplicationRefreshStateAdmission.Validate(value)) return false;
		return MuiApplicationRefreshStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiApplicationRefreshStateField.Magic,
			value.Magic) &&
			MuiApplicationRefreshStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address, MuiApplicationRefreshStateField.Checks,
				value.Checks) &&
			MuiApplicationRefreshStateRecordMemoryCodec.TryWriteUInt32(
				ref platform, address,
				MuiApplicationRefreshStateField.RefreshedWindows,
				value.RefreshedWindows);
	}
}
