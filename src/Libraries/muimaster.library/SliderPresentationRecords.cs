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
	{
		return MuiSliderPresentationStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

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
	private static bool TryResolve(MuiSliderPresentationStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiSliderPresentationStateField.Magic:
				offset = MuiSliderPresentationStateRecord.MagicOffset;
				return true;
			case MuiSliderPresentationStateField.Horizontal:
				offset = MuiSliderPresentationStateRecord.HorizontalOffset;
				return true;
			case MuiSliderPresentationStateField.Quiet:
				offset = MuiSliderPresentationStateRecord.QuietOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiSliderPresentationStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiSliderPresentationStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiSliderPresentationStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSliderPresentationStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiSliderPresentationStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiSliderPresentationStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiSliderPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiSliderPresentationStateField.Magic, out value.Magic)) return false;
		return MuiSliderPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiSliderPresentationStateField.Horizontal,
			out value.Horizontal) &&
			MuiSliderPresentationStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiSliderPresentationStateField.Quiet, out value.Quiet);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiSliderPresentationStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiSliderPresentationStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiSliderPresentationStateAdmission.Validate(value)) return false;
		return MuiSliderPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSliderPresentationStateField.Magic, value.Magic) &&
			MuiSliderPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSliderPresentationStateField.Horizontal, value.Horizontal) &&
			MuiSliderPresentationStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiSliderPresentationStateField.Quiet, value.Quiet);
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
