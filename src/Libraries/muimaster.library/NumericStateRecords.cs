/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Numeric-family value state. Fixed-width fields preserve MorphOS's
// signed 32-bit numeric values and are consumed by Numeric, Slider, Knob, and
// Levelmeter behavior.
public struct MuiNumericState
{
	public uint Minimum;
	public uint Maximum;
	public uint Value;
	public uint Default;
	public uint Reverse;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNumericStateRecord
{
	internal const uint Size = 24;
	internal const uint FieldSize = 4;
	internal const uint Cookie = 0x4D4E5354u; // 'MNST'

	internal uint Magic;
	internal uint Minimum;
	internal uint Maximum;
	internal uint Value;
	internal uint Default;
	internal uint Reverse;
}

internal enum MuiNumericStateField : byte
{
	Magic,
	Minimum,
	Maximum,
	Value,
	Default,
	Reverse,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiNumericStateFieldCursor
{
	internal APTR Record;
	internal MuiNumericStateField Field;
}

internal static class MuiNumericStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNumericStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNumericStateFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNumericStateRecordMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNumericStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiNumericStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
}

// Struct-first guest-memory adapter. Numeric consumers use the complete
// named range/value record; this bounded adapter is the only layer that
// translates its fixed guest layout into addresses. The cursor codec remains
// available for compatibility and malformed-state diagnostics.
internal static class MuiNumericStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiNumericStateField field,
		out uint index)
	{
		if (field == MuiNumericStateField.Magic)
			index = 0;
		else if (field == MuiNumericStateField.Minimum)
			index = 1;
		else if (field == MuiNumericStateField.Maximum)
			index = 2;
		else if (field == MuiNumericStateField.Value)
			index = 3;
		else if (field == MuiNumericStateField.Default)
			index = 4;
		else if (field == MuiNumericStateField.Reverse)
			index = 5;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiNumericStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiNumericStateFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiNumericStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiNumericStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiNumericStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiNumericStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiNumericStateField.Magic)
			value = state.Magic;
		else if (field == MuiNumericStateField.Minimum)
			value = state.Minimum;
		else if (field == MuiNumericStateField.Maximum)
			value = state.Maximum;
		else if (field == MuiNumericStateField.Value)
			value = state.Value;
		else if (field == MuiNumericStateField.Default)
			value = state.Default;
		else if (field == MuiNumericStateField.Reverse)
			value = state.Reverse;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiNumericStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiNumericStateField.Magic)
			state.Magic = value;
		else if (field == MuiNumericStateField.Minimum)
			state.Minimum = value;
		else if (field == MuiNumericStateField.Maximum)
			state.Maximum = value;
		else if (field == MuiNumericStateField.Value)
			state.Value = value;
		else if (field == MuiNumericStateField.Default)
			state.Default = value;
		else if (field == MuiNumericStateField.Reverse)
			state.Reverse = value;
		else return false;
		return MuiNumericStateRecordCodec.WriteRecord(ref platform, record, state);
	}
}

internal static class MuiNumericStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiNumericStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNumericStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Minimum) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Maximum) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Value) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Default) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Reverse) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiNumericStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiNumericStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Minimum) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Maximum) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Default) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reverse) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiNumericStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNumericStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiNumericStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNumericStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiNumericStateAdmission.Validate(value) &&
			WriteRecord(ref platform, address, value);
	}
}

// Keep the fixed-width wire fields lossless for corruption inspection, but
// admit the named state only when the MorphOS BOOL projection is canonical.
// Range relationships remain consumer-specific because Numeric clips values
// at the public operation boundary.
internal static class MuiNumericStateAdmission
{
	internal static bool Validate(MuiNumericStateRecord value) =>
		value.Magic == MuiNumericStateRecord.Cookie && value.Reverse <= 1;

	internal static bool Validate(MuiNumericState value) =>
		value.Reverse <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiNumericStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing range-only call sites. New Numeric state
// boundaries use MuiNumericStateAdmission directly so live ownership is explicit.
internal static class MuiNumericStateValidation
{
	internal static bool IsValidRecord(MuiNumericStateRecord value) =>
		MuiNumericStateAdmission.Validate(value);

	internal static bool IsValidState(MuiNumericState value) =>
		MuiNumericStateAdmission.Validate(value);
}
