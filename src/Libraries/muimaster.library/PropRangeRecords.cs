/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Shared Prop/Scrollbar range state. MorphOS exposes these as LONG values;
// the fixed-width ULONG fields preserve their guest wire representation while
// movement, clamping, and drawing consume one named value.
public struct MuiPropRangeState
{
	public uint Entries;
	public uint Visible;
	public uint First;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPropRangeStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4D505247u; // 'MPRG'

	internal uint Magic;
	internal uint Entries;
	internal uint Visible;
	internal uint First;
}

internal enum MuiPropRangeStateField : byte
{
	Magic,
	Entries,
	Visible,
	First,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiPropRangeStateFieldCursor
{
	internal APTR Record;
	internal MuiPropRangeStateField Field;
}

internal static class MuiPropRangeStateFieldCursorCodec
{
	private static bool TryResolve(MuiPropRangeStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiPropRangeStateField.Magic => 0,
			MuiPropRangeStateField.Entries => 4,
			MuiPropRangeStateField.Visible => 8,
			MuiPropRangeStateField.First => 12,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPropRangeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiPropRangeStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropRangeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiPropRangeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropRangeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiPropRangeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Struct-first guest-memory adapter. Range values remain named semantic
// fields; bounded fixed guest-layout translation is isolated here.
internal static class MuiPropRangeStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiPropRangeStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiPropRangeStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, 4);
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

internal static class MuiPropRangeStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiPropRangeStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic)) return false;
		return MuiPropRangeStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 4, out value.Entries) &&
			MuiPropRangeStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
			8, out value.Visible) &&
			MuiPropRangeStateRecordMemoryCodec.TryReadUInt32(ref platform, address,
			12, out value.First);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		MuiPropRangeStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiPropRangeStateAdmission.Validate(value)) return false;
		return MuiPropRangeStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiPropRangeStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			4, value.Entries) &&
			MuiPropRangeStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			8, value.Visible) &&
			MuiPropRangeStateRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			12, value.First);
	}
}

// Preserve the LONG-valued range fields losslessly for malformed-state
// diagnostics, but admit only non-negative values whose First position is
// reachable from Entries and Visible. The public record keeps ULONG wire
// storage so the guest ABI remains lossless.
internal static class MuiPropRangeStateAdmission
{
	private const uint LongMaximum = 0x7fffffffu;

	internal static bool Validate(MuiPropRangeStateRecord value)
	{
		if (value.Magic != MuiPropRangeStateRecord.Cookie) return false;
		var state = default(MuiPropRangeState);
		state.Entries = value.Entries;
		state.Visible = value.Visible;
		state.First = value.First;
		return Validate(state);
	}

	internal static bool Validate(MuiPropRangeState value)
	{
		if (value.Entries > LongMaximum || value.Visible > LongMaximum ||
			value.First > LongMaximum) return false;
		var last = value.Entries > value.Visible ?
			value.Entries - value.Visible : 0u;
		return value.First <= last;
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;

	internal static uint ClampFirst(MuiPropRangeState value)
	{
		var last = value.Entries > value.Visible ?
			value.Entries - value.Visible : 0u;
		if (value.First > LongMaximum) return 0;
		return value.First > last ? last : value.First;
	}

	internal static uint ClampRequestedFirst(MuiPropRangeState range, uint value)
	{
		if (value > LongMaximum) return 0;
		range.First = value;
		return ClampFirst(range);
	}
}

// Compatibility alias for existing range-only call sites. New range
// boundaries use MuiPropRangeStateAdmission directly so live ownership is
// explicit at the consumer edge.
internal static class MuiPropRangeStateValidation
{
	internal static bool IsValidRecord(MuiPropRangeStateRecord value) =>
		MuiPropRangeStateAdmission.Validate(value);

	internal static bool IsValidState(MuiPropRangeState value) =>
		MuiPropRangeStateAdmission.Validate(value);

	internal static uint ClampFirst(MuiPropRangeState value) =>
		MuiPropRangeStateAdmission.ClampFirst(value);

	internal static uint ClampRequestedFirst(MuiPropRangeState range, uint value) =>
		MuiPropRangeStateAdmission.ClampRequestedFirst(range, value);
}
