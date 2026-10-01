/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Gauge progress state. Fixed-width fields preserve the MorphOS guest
// values while construction, divide handling, clamping, and drawing consume
// one named value instead of separate anonymous attribute reads.
public struct MuiGaugeState
{
	public uint Maximum;
	public uint Current;
	public uint Divide;
	public uint Horizontal;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGaugeStateRecord
{
	internal const uint Size = 20;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint MaximumOffset = 4;
	internal const uint CurrentOffset = 8;
	internal const uint DivideOffset = 12;
	internal const uint HorizontalOffset = 16;
	internal const uint Cookie = 0x4D474155u; // 'MGAU'

	internal uint Magic;
	internal uint Maximum;
	internal uint Current;
	internal uint Divide;
	internal uint Horizontal;
}

internal enum MuiGaugeStateField : byte
{
	Magic,
	Maximum,
	Current,
	Divide,
	Horizontal,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGaugeStateFieldCursor
{
	internal APTR Record;
	internal MuiGaugeStateField Field;
}

internal static class MuiGaugeStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGaugeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGaugeStateFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiGaugeStateRecordMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGaugeStateRecordMemoryCodec.TryReadUInt32(ref platform, record,
			field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGaugeStateRecordMemoryCodec.TryWriteUInt32(ref platform, record,
			field, value);
	}
}

// Struct-first guest-memory adapter. Gauge consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses. The cursor codec remains for compatibility and
// malformed-state diagnostics.
internal static class MuiGaugeStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiGaugeStateField field,
		out uint index)
	{
		if (field == MuiGaugeStateField.Magic)
			index = 0;
		else if (field == MuiGaugeStateField.Maximum)
			index = 1;
		else if (field == MuiGaugeStateField.Current)
			index = 2;
		else if (field == MuiGaugeStateField.Divide)
			index = 3;
		else if (field == MuiGaugeStateField.Horizontal)
			index = 4;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiGaugeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiGaugeStateFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiGaugeStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiGaugeStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiGaugeStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiGaugeStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiGaugeStateField.Magic)
			value = state.Magic;
		else if (field == MuiGaugeStateField.Maximum)
			value = state.Maximum;
		else if (field == MuiGaugeStateField.Current)
			value = state.Current;
		else if (field == MuiGaugeStateField.Divide)
			value = state.Divide;
		else if (field == MuiGaugeStateField.Horizontal)
			value = state.Horizontal;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiGaugeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiGaugeStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiGaugeStateField.Magic)
			state.Magic = value;
		else if (field == MuiGaugeStateField.Maximum)
			state.Maximum = value;
		else if (field == MuiGaugeStateField.Current)
			state.Current = value;
		else if (field == MuiGaugeStateField.Divide)
			state.Divide = value;
		else if (field == MuiGaugeStateField.Horizontal)
			state.Horizontal = value;
		else return false;
		return MuiGaugeStateRecordCodec.WriteRecord(ref platform, record, state);
	}
}

internal static class MuiGaugeStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiGaugeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGaugeStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Maximum) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Current) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Divide) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Horizontal) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiGaugeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiGaugeStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Maximum) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Current) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Divide) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Horizontal) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGaugeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiGaugeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiGaugeStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiGaugeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGaugeStateAdmission.Validate(value) &&
			WriteRecord(ref platform, address, value);
	}
}

// The wire codec remains lossless for malformed-state diagnostics. Gauge's
// Horizontal is a MorphOS BOOL; Maximum, Current, and Divide retain their
// existing operation-specific clamping and divide-by-zero semantics.
internal static class MuiGaugeStateAdmission
{
	internal static bool Validate(MuiGaugeStateRecord value) =>
		value.Magic == MuiGaugeStateRecord.Cookie && value.Horizontal <= 1;

	internal static bool Validate(MuiGaugeState value) =>
		value.Horizontal <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiGaugeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing range-only call sites. New Gauge state
// boundaries use MuiGaugeStateAdmission directly so live ownership is explicit.
internal static class MuiGaugeStateValidation
{
	internal static bool IsValidRecord(MuiGaugeStateRecord value) =>
		MuiGaugeStateAdmission.Validate(value);

	internal static bool IsValidState(MuiGaugeState value) =>
		MuiGaugeStateAdmission.Validate(value);
}
