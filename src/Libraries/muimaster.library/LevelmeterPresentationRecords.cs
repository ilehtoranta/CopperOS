/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Levelmeter presentation state. Numeric range/value data remains in
// MuiNumericState; this record carries the Gauge_Horiz orientation consumed by
// Levelmeter rendering.
public struct MuiLevelmeterPresentationState
{
	public uint Horizontal;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterPresentationStateRecord
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint HorizontalOffset = 4;
	internal const uint Cookie = 0x4D4C564Cu; // 'MLVL'

	internal uint Magic;
	internal uint Horizontal;
}

internal enum MuiLevelmeterPresentationStateField : byte
{
	Magic,
	Horizontal,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiLevelmeterPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiLevelmeterPresentationStateField Field;
}

internal static class MuiLevelmeterPresentationStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLevelmeterPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLevelmeterPresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiLevelmeterPresentationStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiLevelmeterPresentationStateRecordMemoryCodec.TryReadUInt32(
			ref platform, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiLevelmeterPresentationStateRecordMemoryCodec.TryWriteUInt32(
			ref platform, record, field, value);
	}
}

// Struct-first guest-memory adapter. Levelmeter consumers use the named
// record; this bounded adapter is the only layer that translates its fixed
// guest layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiLevelmeterPresentationStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiLevelmeterPresentationStateField field,
		out uint index)
	{
		if (field == MuiLevelmeterPresentationStateField.Magic)
			index = 0;
		else if (field == MuiLevelmeterPresentationStateField.Horizontal)
			index = 1;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterPresentationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiLevelmeterPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiLevelmeterPresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiLevelmeterPresentationStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiLevelmeterPresentationStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiLevelmeterPresentationStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiLevelmeterPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiLevelmeterPresentationStateField.Magic)
			value = state.Magic;
		else if (field == MuiLevelmeterPresentationStateField.Horizontal)
			value = state.Horizontal;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiLevelmeterPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiLevelmeterPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiLevelmeterPresentationStateField.Magic)
			state.Magic = value;
		else if (field == MuiLevelmeterPresentationStateField.Horizontal)
			state.Horizontal = value;
		else return false;
		return MuiLevelmeterPresentationStateRecordCodec.WriteRecord(ref platform,
			record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiLevelmeterPresentationStateRecord.Size -
			MuiLevelmeterPresentationStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiLevelmeterPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiLevelmeterPresentationStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, offset, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiLevelmeterPresentationStateRecordCodec
{
	// Production access is sequential and struct-shaped. The field-address
	// adapters above remain available for compatibility diagnostics only.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiLevelmeterPresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var horizontal) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Magic = magic;
		value.Horizontal = horizontal;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value) &&
		MuiLevelmeterPresentationStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiLevelmeterPresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Horizontal)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiLevelmeterPresentationStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}

// Keep the wire field lossless for malformed-state diagnostics. Levelmeter's
// Gauge_Horiz value is a MorphOS BOOL and must be canonical before layout or
// drawing consumers use the named presentation state.
internal static class MuiLevelmeterPresentationStateAdmission
{
	internal static bool Validate(MuiLevelmeterPresentationStateRecord value) =>
		value.Magic == MuiLevelmeterPresentationStateRecord.Cookie &&
		value.Horizontal <= 1;

	internal static bool Validate(MuiLevelmeterPresentationState value) =>
		value.Horizontal <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiLevelmeterPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing Levelmeter presentation call sites. New
// state boundaries use MuiLevelmeterPresentationStateAdmission directly so
// live ownership is explicit.
internal static class MuiLevelmeterPresentationStateValidation
{
	internal static bool IsValidRecord(MuiLevelmeterPresentationStateRecord value) =>
		MuiLevelmeterPresentationStateAdmission.Validate(value);

	internal static bool IsValidState(MuiLevelmeterPresentationState value) =>
		MuiLevelmeterPresentationStateAdmission.Validate(value);
}
