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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint EntriesOffset = 4;
	internal const uint VisibleOffset = 8;
	internal const uint FirstOffset = 12;
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
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPropRangeStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPropRangeStateFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiPropRangeStateRecordMemoryCodec.TryGetAddress(ref platform, cursor,
			out address, out fieldSize);

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
	private static bool TryResolveFieldIndex(MuiPropRangeStateField field,
		out uint index)
	{
		if (field == MuiPropRangeStateField.Magic)
			index = 0;
		else if (field == MuiPropRangeStateField.Entries)
			index = 1;
		else if (field == MuiPropRangeStateField.Visible)
			index = 2;
		else if (field == MuiPropRangeStateField.First)
			index = 3;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropRangeStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiPropRangeStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiPropRangeStateFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiPropRangeStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiPropRangeStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiPropRangeStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropRangeStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiPropRangeStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiPropRangeStateField.Magic)
			value = state.Magic;
		else if (field == MuiPropRangeStateField.Entries)
			value = state.Entries;
		else if (field == MuiPropRangeStateField.Visible)
			value = state.Visible;
		else if (field == MuiPropRangeStateField.First)
			value = state.First;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiPropRangeStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiPropRangeStateRecordCodec.TryReadStructural(ref platform, record,
			out var state)) return false;
		if (field == MuiPropRangeStateField.Magic)
			state.Magic = value;
		else if (field == MuiPropRangeStateField.Entries)
			state.Entries = value;
		else if (field == MuiPropRangeStateField.Visible)
			state.Visible = value;
		else if (field == MuiPropRangeStateField.First)
			state.First = value;
		else return false;
		return MuiPropRangeStateRecordCodec.WriteRecord(ref platform, record, state);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiPropRangeStateRecord.Size -
			MuiPropRangeStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiPropRangeStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiPropRangeStateRecord.FieldSize);
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
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPropRangeStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Entries) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Visible) &&
			MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.First) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiPropRangeStateRecord.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Entries) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Visible) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.First) &&
			MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiPropRangeStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadRecord(ref platform, address, out value);
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
		return MuiPropRangeStateAdmission.Validate(value) &&
			WriteRecord(ref platform, address, value);
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
