/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Selectgroup.Active is a signed selector at the API boundary but the stored
// state is the canonical non-negative child index.  Keep that state in one
// fixed-width guest record; selector interpretation stays in SelectgroupCore.
public struct MuiSelectgroupActiveState
{
	public uint Active;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSelectgroupActiveStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ActiveOffset = 4;
	internal const uint Cookie = 0x534C4750u; // 'SLGP'

	internal uint Magic;
	internal uint Active;
}

internal enum MuiSelectgroupActiveStateField : byte
{
	Magic,
	Active,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSelectgroupActiveStateFieldCursor
{
	internal APTR Record;
	internal MuiSelectgroupActiveStateField Field;
}

internal static class MuiSelectgroupActiveStateValidation
{
	// Active is a canonical child index, never one of the signed -1/-2
	// selectors accepted at the public setter boundary.  Keep the record
	// bounded even while a Selectgroup has no children yet; the live child
	// count is reconciled by SelectgroupCore when it publishes the state.
	internal static bool IsValid(MuiSelectgroupActiveStateRecord value) =>
		value.Magic == MuiSelectgroupActiveStateRecord.Cookie &&
		value.Active <= MuiHeadlessLayout.MaximumTraversal;
}

internal static class MuiSelectgroupActiveStateAdmission
{
	internal static bool Validate(MuiSelectgroupActiveStateRecord value) =>
		MuiSelectgroupActiveStateValidation.IsValid(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiSelectgroupActiveStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal static class MuiSelectgroupActiveStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSelectgroupActiveStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiSelectgroupActiveStateRecordMemoryCodec.TryGetAddress(
			ref platform, cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSelectgroupActiveStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiSelectgroupActiveStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSelectgroupActiveStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiSelectgroupActiveStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Struct-first guest-memory adapter. Selectgroup consumers use the named
// active-index record; this bounded adapter is the only layer that translates
// its fixed guest layout into addresses. The cursor codec remains available
// for compatibility and malformed-state diagnostics.
internal static class MuiSelectgroupActiveStateRecordMemoryCodec
{
	private static bool TryResolve(MuiSelectgroupActiveStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiSelectgroupActiveStateField.Magic:
				offset = MuiSelectgroupActiveStateRecord.MagicOffset;
				return true;
			case MuiSelectgroupActiveStateField.Active:
				offset = MuiSelectgroupActiveStateRecord.ActiveOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiSelectgroupActiveStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiSelectgroupActiveStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address,
			MuiSelectgroupActiveStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSelectgroupActiveStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSelectgroupActiveStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiSelectgroupActiveStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiSelectgroupActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiSelectgroupActiveStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiSelectgroupActiveStateField.Magic, out value.Magic) &&
			MuiSelectgroupActiveStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiSelectgroupActiveStateField.Active, out value.Active);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSelectgroupActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiSelectgroupActiveStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSelectgroupActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiSelectgroupActiveStateAdmission.Validate(value)) return false;
		return MuiSelectgroupActiveStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiSelectgroupActiveStateField.Magic,
			value.Magic) &&
			MuiSelectgroupActiveStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, address, MuiSelectgroupActiveStateField.Active,
			value.Active);
	}
}
