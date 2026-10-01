/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Slider-only presentation state. Numeric values remain in
// MuiNumericState; this record carries the orientation and quiet-display
// policy that are consumed together by Slider layout and drawing.
public struct MuiSliderPresentationState
{
	public uint Horizontal;
	public uint Quiet;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSliderPresentationStateRecord
{
	internal const uint Size = 12;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint HorizontalOffset = 4;
	internal const uint QuietOffset = 8;
	internal const uint Cookie = 0x4D534C44u; // 'MSLD'

	internal uint Magic;
	internal uint Horizontal;
	internal uint Quiet;
}

internal enum MuiSliderPresentationStateField : byte
{
	Magic,
	Horizontal,
	Quiet,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiSliderPresentationStateFieldCursor
{
	internal APTR Record;
	internal MuiSliderPresentationStateField Field;
}

internal static class MuiSliderPresentationStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSliderPresentationStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSliderPresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiSliderPresentationStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSliderPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiSliderPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSliderPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiSliderPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Orientation and quiet-display policy
// remain named semantic fields; fixed guest-layout translation is bounded here.
internal static class MuiSliderPresentationStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiSliderPresentationStateField field,
		out uint index)
	{
		if (field == MuiSliderPresentationStateField.Magic)
			index = 0;
		else if (field == MuiSliderPresentationStateField.Horizontal)
			index = 1;
		else if (field == MuiSliderPresentationStateField.Quiet)
			index = 2;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiSliderPresentationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiSliderPresentationStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiSliderPresentationStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiSliderPresentationStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiSliderPresentationStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiSliderPresentationStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSliderPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiSliderPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiSliderPresentationStateField.Magic)
			value = state.Magic;
		else if (field == MuiSliderPresentationStateField.Horizontal)
			value = state.Horizontal;
		else if (field == MuiSliderPresentationStateField.Quiet)
			value = state.Quiet;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSliderPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiSliderPresentationStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiSliderPresentationStateField.Magic)
			state.Magic = value;
		else if (field == MuiSliderPresentationStateField.Horizontal)
			state.Horizontal = value;
		else if (field == MuiSliderPresentationStateField.Quiet)
			state.Quiet = value;
		else return false;
		return MuiSliderPresentationStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiSliderPresentationStateRecordCodec
{
	// Production access is sequential and struct-shaped. The field-address
	// adapters above remain available for compatibility diagnostics only.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiSliderPresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var horizontal) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var quiet) || !MuiGuestStructCursor.IsComplete(cursor))
			return false;
		value.Magic = magic;
		value.Horizontal = horizontal;
		value.Quiet = quiet;
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value) &&
		MuiSliderPresentationStateAdmission.Validate(value);

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if ((address.Raw & 1u) != 0 ||
			!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiSliderPresentationStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Horizontal) ||
			!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Quiet)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiSliderPresentationStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}

// Keep the wire record lossless for malformed-state diagnostics. Both
// presentation fields are MorphOS BOOL projections and must be canonical
// before Slider layout, input, or drawing consumers use them.
internal static class MuiSliderPresentationStateAdmission
{
	internal static bool Validate(MuiSliderPresentationStateRecord value) =>
		value.Magic == MuiSliderPresentationStateRecord.Cookie &&
		value.Horizontal <= 1 && value.Quiet <= 1;

	internal static bool Validate(MuiSliderPresentationState value) =>
		value.Horizontal <= 1 && value.Quiet <= 1;

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

// Compatibility alias for existing Slider presentation call sites. New state
// boundaries use MuiSliderPresentationStateAdmission directly so live
// ownership is explicit.
internal static class MuiSliderPresentationStateValidation
{
	internal static bool IsValidRecord(MuiSliderPresentationStateRecord value) =>
		MuiSliderPresentationStateAdmission.Validate(value);

	internal static bool IsValidState(MuiSliderPresentationState value) =>
		MuiSliderPresentationStateAdmission.Validate(value);
}
