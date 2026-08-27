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
	internal const uint MagicOffset = 0;
	internal const uint MinimumOffset = 4;
	internal const uint MaximumOffset = 8;
	internal const uint ValueOffset = 12;
	internal const uint DefaultOffset = 16;
	internal const uint ReverseOffset = 20;
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
	{
		return MuiNumericStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		return MuiNumericStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiNumericStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Struct-first guest-memory adapter. Numeric consumers use the complete
// named range/value record; this bounded adapter is the only layer that
// translates its fixed guest layout into addresses. The cursor codec remains
// available for compatibility and malformed-state diagnostics.
internal static class MuiNumericStateRecordMemoryCodec
{
	private static bool TryResolve(MuiNumericStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiNumericStateField.Magic:
				offset = MuiNumericStateRecord.MagicOffset;
				return true;
			case MuiNumericStateField.Minimum:
				offset = MuiNumericStateRecord.MinimumOffset;
				return true;
			case MuiNumericStateField.Maximum:
				offset = MuiNumericStateRecord.MaximumOffset;
				return true;
			case MuiNumericStateField.Value:
				offset = MuiNumericStateRecord.ValueOffset;
				return true;
			case MuiNumericStateField.Default:
				offset = MuiNumericStateRecord.DefaultOffset;
				return true;
			case MuiNumericStateField.Reverse:
				offset = MuiNumericStateRecord.ReverseOffset;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiNumericStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiNumericStateRecord.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiNumericStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiNumericStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiNumericStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiNumericStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, MuiNumericStateField.Magic, out value.Magic) &&
			MuiNumericStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiNumericStateField.Minimum, out value.Minimum) &&
			MuiNumericStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiNumericStateField.Maximum, out value.Maximum) &&
			MuiNumericStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiNumericStateField.Value, out value.Value) &&
			MuiNumericStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiNumericStateField.Default, out value.Default) &&
			MuiNumericStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiNumericStateField.Reverse, out value.Reverse);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiNumericStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiNumericStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiNumericStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiNumericStateAdmission.Validate(value)) return false;
		return MuiNumericStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiNumericStateField.Magic, value.Magic) &&
			MuiNumericStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiNumericStateField.Minimum, value.Minimum) &&
			MuiNumericStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiNumericStateField.Maximum, value.Maximum) &&
			MuiNumericStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiNumericStateField.Value, value.Value) &&
			MuiNumericStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiNumericStateField.Default, value.Default) &&
			MuiNumericStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiNumericStateField.Reverse, value.Reverse);
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
