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
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSelectgroupActiveStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiSelectgroupActiveStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

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
	private static bool TryResolveFieldIndex(MuiSelectgroupActiveStateField field,
		out uint index)
	{
		if (field == MuiSelectgroupActiveStateField.Magic)
			index = 0;
		else if (field == MuiSelectgroupActiveStateField.Active)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiSelectgroupActiveStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiSelectgroupActiveStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSelectgroupActiveStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiSelectgroupActiveStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiSelectgroupActiveStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiSelectgroupActiveStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSelectgroupActiveStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiSelectgroupActiveStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiSelectgroupActiveStateField.Magic)
			value = state.Magic;
		else if (field == MuiSelectgroupActiveStateField.Active)
			value = state.Active;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSelectgroupActiveStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiSelectgroupActiveStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiSelectgroupActiveStateField.Magic)
			state.Magic = value;
		else if (field == MuiSelectgroupActiveStateField.Active)
			state.Active = value;
		else return false;
		return MuiSelectgroupActiveStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}
}

internal static class MuiSelectgroupActiveStateRecordCodec
{
	// Declaration-order guest record: { Magic, Active }.  Production exchange
	// stays on the named record through the bounded sequential cursor.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiSelectgroupActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSelectgroupActiveStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Active) && MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiSelectgroupActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiSelectgroupActiveStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Active) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiSelectgroupActiveStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

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
		return WriteRecord(ref platform, address, value);
	}
}
