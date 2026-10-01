/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// Balance.mui exposes one initializer/getter policy value. Keep the complete
// MorphOS LONG in a fixed-width guest record so construction, generic Get, and
// persistence synchronization share one typed representation.
public struct MuiBalancePolicyState
{
	public uint Quiet;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBalancePolicyStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint QuietOffset = 4;
	internal const uint Cookie = 0x42414C4Eu; // 'BALN'

	internal uint Magic;
	internal uint Quiet;
}

internal enum MuiBalancePolicyStateField : byte
{
	Magic,
	Quiet,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiBalancePolicyStateFieldCursor
{
	internal APTR Record;
	internal MuiBalancePolicyStateField Field;
}

internal static class MuiBalancePolicyStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBalancePolicyStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBalancePolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiBalancePolicyStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBalancePolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		return MuiBalancePolicyStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBalancePolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiBalancePolicyStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Balance policy consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiBalancePolicyStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiBalancePolicyStateField field,
		out uint index)
	{
		if (field == MuiBalancePolicyStateField.Magic)
			index = 0;
		else if (field == MuiBalancePolicyStateField.Quiet)
		{
			index = 1;
		}
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiBalancePolicyStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiBalancePolicyStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiBalancePolicyStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiBalancePolicyStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiBalancePolicyStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiBalancePolicyStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBalancePolicyStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiBalancePolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBalancePolicyStateField.Magic)
			value = state.Magic;
		else if (field == MuiBalancePolicyStateField.Quiet)
			value = state.Quiet;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiBalancePolicyStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiBalancePolicyStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiBalancePolicyStateField.Magic)
			state.Magic = value;
		else if (field == MuiBalancePolicyStateField.Quiet)
			state.Quiet = value;
		else return false;
		return MuiBalancePolicyStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	// Legacy raw-offset adapter retained for bounded diagnostics and older
	// callers. Typed field access above is the preferred API.
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiBalancePolicyStateRecord.Size -
			MuiBalancePolicyStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiBalancePolicyStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiBalancePolicyStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
		return true;
	}
}

internal static class MuiBalancePolicyStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBalancePolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBalancePolicyStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Quiet)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiBalancePolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiBalancePolicyStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Quiet) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiBalancePolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiBalancePolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiBalancePolicyStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiBalancePolicyStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiBalancePolicyStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}

// Keep the Balance policy wire field lossless for malformed-state diagnostics.
// MUIA_Balance_Quiet is an opaque MorphOS LONG, so its full bit pattern is
// admitted after the record cookie and live owner are checked.
internal static class MuiBalancePolicyStateAdmission
{
	internal static bool Validate(MuiBalancePolicyStateRecord value) =>
		value.Magic == MuiBalancePolicyStateRecord.Cookie;

	// MorphOS documents MUIA_Balance_Quiet as LONG, so every 32-bit guest
	// pattern is retained. The value is intentionally opaque because the
	// attribute remains undocumented beyond its wire type.
	internal static bool Validate(MuiBalancePolicyState value) => true;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiBalancePolicyStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing Balance policy call sites. New state
// boundaries use MuiBalancePolicyStateAdmission directly so live ownership is
// explicit.
internal static class MuiBalancePolicyStateValidation
{
	internal static bool IsValidRecord(MuiBalancePolicyStateRecord value) =>
		MuiBalancePolicyStateAdmission.Validate(value);

	internal static bool IsValidState(MuiBalancePolicyState value) =>
		MuiBalancePolicyStateAdmission.Validate(value);
}
