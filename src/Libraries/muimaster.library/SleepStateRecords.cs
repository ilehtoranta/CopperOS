/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared fixed-width sleep state. Window owners use SavedDisabled to restore
// their prior disabled value; application owners leave that field zero.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSleepStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint DepthOffset = 4;
	internal const uint SavedDisabledOffset = 8;
	internal const uint RequestOffset = 12;
	internal const uint Cookie = 0x534C5053u; // 'SLPS'

	internal uint Magic;
	internal uint Depth;
	internal uint SavedDisabled;
	internal uint Request;
}

// Sleep depth and the public request are one nesting counter in the MorphOS
// projection. SavedDisabled is a canonical ULONG BOOL; the structural codec
// remains available for inspecting malformed guest storage without allowing
// it into sleep-sensitive consumers.
internal static class MuiSleepStateAdmission
{
	internal static bool Validate(MuiSleepStateRecord value) =>
		value.Magic == MuiSleepStateRecord.Cookie &&
		value.SavedDisabled <= 1 && value.Request == value.Depth;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, MuiSleepStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !owner.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, owner).IsNull;
}

internal enum MuiSleepStateField : byte
{
	Magic,
	Depth,
	SavedDisabled,
	Request,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSleepStateFieldCursor
{
	internal APTR Record;
	internal MuiSleepStateField Field;
}

internal static class MuiSleepStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSleepStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiSleepStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform, record,
			field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform, record,
			field, value);
	}
}

// Fixed sleep state is transferred as a named record. Numeric guest positions
// are confined to this ABI adapter; the compatibility cursor above remains
// available only to legacy callers and malformed-state diagnostics.
internal static class MuiSleepStateRecordMemoryCodec
{
	private static bool TryResolve(MuiSleepStateField field, out uint offset)
	{
		switch (field)
		{
			case MuiSleepStateField.Magic:
				offset = MuiSleepStateRecord.MagicOffset;
				return true;
			case MuiSleepStateField.Depth:
				offset = MuiSleepStateRecord.DepthOffset;
				return true;
			case MuiSleepStateField.SavedDisabled:
				offset = MuiSleepStateRecord.SavedDisabledOffset;
				return true;
			case MuiSleepStateField.Request:
				offset = MuiSleepStateRecord.RequestOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset)
			return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiSleepStateRecord.Size) &&
			platform.IsMapped(address, MuiSleepStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSleepStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiSleepStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiSleepStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiSleepStateField.Magic, out var magic) ||
			!MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiSleepStateField.Depth, out value.Depth) ||
			!MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiSleepStateField.SavedDisabled, out value.SavedDisabled) ||
			!MuiSleepStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
				MuiSleepStateField.Request, out value.Request)) return false;
		value.Magic = magic;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSleepStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiSleepStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSleepStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiSleepStateRecord.Size) || !MuiSleepStateAdmission.Validate(value))
			return false;
		return MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSleepStateField.Magic, value.Magic) &&
			MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiSleepStateField.Depth, value.Depth) &&
			MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiSleepStateField.SavedDisabled, value.SavedDisabled) &&
			MuiSleepStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
				MuiSleepStateField.Request, value.Request);
	}
}
