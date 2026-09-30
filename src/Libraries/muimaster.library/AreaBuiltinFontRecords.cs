/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// MUIA_BuiltinFont is an unsigned ABI value whose documented selectors are
// negative signed constants (MUIV_BuiltinFont_*). Keep the raw ULONG bit
// pattern so the guest can distinguish an explicit Inherit selector from an
// absent tag without relying on a private object offset.
public struct MuiAreaBuiltinFontState
{
	public uint Selector;
	public uint Present;
	public uint Generation;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaBuiltinFontStateRecord
{
	internal const uint Size = 16;
	internal const uint FieldSize = 4;
	internal const uint Cookie = 0x4D424652u; // 'MBFR'

	internal uint Magic;
	internal uint Selector;
	internal uint Present;
	internal uint Generation;
}

// A BuiltinFont record is a persisted projection, not an arbitrary scratch
// buffer.  Keep the MorphOS selector as a lossless ULONG (the documented
// MUIV_BuiltinFont values are signed constants) while admitting only the
// canonical presence flag and a non-zero generation.  Live ownership is
// checked at the consumer boundary so a stale Dataspace block cannot be
// mistaken for the current object's state.
internal static class MuiAreaBuiltinFontStateAdmission
{
	internal static bool Validate(MuiAreaBuiltinFontState value) =>
		value.Present <= 1 && value.Generation != 0;

	internal static bool Validate(MuiAreaBuiltinFontStateRecord value)
	{
		if (value.Magic != MuiAreaBuiltinFontStateRecord.Cookie) return false;
		var state = default(MuiAreaBuiltinFontState);
		state.Selector = value.Selector;
		state.Present = value.Present;
		state.Generation = value.Generation;
		return Validate(state);
	}

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaBuiltinFontStateField : byte
{
	Magic,
	Selector,
	Present,
	Generation,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaBuiltinFontStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaBuiltinFontStateField Field;
}

internal static class MuiAreaBuiltinFontStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaBuiltinFontStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaBuiltinFontStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaBuiltinFontStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaBuiltinFontStateRecordMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiAreaBuiltinFontStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);
	}
}

// Fixed Area BuiltinFont state is transferred as a named record. The bounded
// cursor walks the complete packed struct before selecting a field; the
// compatibility cursor above remains available only to legacy callers and
// malformed-state diagnostics.
internal static class MuiAreaBuiltinFontStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiAreaBuiltinFontStateField field,
		out uint index)
	{
		if (field == MuiAreaBuiltinFontStateField.Magic) index = 0;
		else if (field == MuiAreaBuiltinFontStateField.Selector) index = 1;
		else if (field == MuiAreaBuiltinFontStateField.Present) index = 2;
		else if (field == MuiAreaBuiltinFontStateField.Generation) index = 3;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaBuiltinFontStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaBuiltinFontStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Record,
				MuiAreaBuiltinFontStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaBuiltinFontStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaBuiltinFontStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaBuiltinFontStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaBuiltinFontStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaBuiltinFontStateField.Selector)
			value = state.Selector;
		else if (field == MuiAreaBuiltinFontStateField.Present)
			value = state.Present;
		else if (field == MuiAreaBuiltinFontStateField.Generation)
			value = state.Generation;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaBuiltinFontStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaBuiltinFontStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaBuiltinFontStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaBuiltinFontStateField.Selector)
			state.Selector = value;
		else if (field == MuiAreaBuiltinFontStateField.Present)
			state.Present = value;
		else if (field == MuiAreaBuiltinFontStateField.Generation)
			state.Generation = value;
		else return false;
		return MuiAreaBuiltinFontStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}
}

internal static class MuiAreaBuiltinFontStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaBuiltinFontStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Selector) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Present) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Generation)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaBuiltinFontStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Selector) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Present) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Generation) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiAreaBuiltinFontStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaBuiltinFontStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !MuiAreaBuiltinFontStateAdmission.Validate(value))
			return false;
		return WriteRecord(ref platform, address, value);
	}
}
