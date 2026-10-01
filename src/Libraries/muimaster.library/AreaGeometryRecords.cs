/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The six public Area geometry values are kept together as one semantic
// record. Signed coordinates remain explicit while guest storage preserves
// the original 32-bit ULONG representation.
public struct MuiAreaGeometryState
{
	public int Left;
	public int Top;
	public int Width;
	public int Height;
	public int Right;
	public int Bottom;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaGeometryStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x4D414745u; // 'MAGE'
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint LeftOffset = 4;
	internal const uint TopOffset = 8;
	internal const uint WidthOffset = 12;
	internal const uint HeightOffset = 16;
	internal const uint RightOffset = 20;
	internal const uint BottomOffset = 24;

	internal uint Magic;
	internal int Left;
	internal int Top;
	internal int Width;
	internal int Height;
	internal int Right;
	internal int Bottom;
}

internal static class MuiAreaGeometryStateValidation
{
	internal static bool TryExpectedEdge(int origin, int extent, out int edge)
	{
		edge = 0;
		if (extent < 0) return false;
		if (extent == 0)
		{
			// A zero-area layout is a valid disappearance/no-op state. It still
			// has a representable preceding edge when an origin is available; the
			// neutral pre-layout record is handled separately by IsValidState.
			if (origin == int.MinValue) return false;
			edge = origin - 1;
			return true;
		}
		if (origin > int.MaxValue - extent) return false;
		edge = origin + extent - 1;
		return true;
	}

	internal static bool IsValidState(MuiAreaGeometryStateRecord value)
	{
		if (value.Magic != MuiAreaGeometryStateRecord.Cookie || value.Width < 0 ||
			value.Height < 0) return false;
		if (value.Width > 0)
		{
			if (!TryExpectedEdge(value.Left, value.Width, out var right) ||
				right != value.Right) return false;
		}
		if (value.Height > 0)
		{
			if (!TryExpectedEdge(value.Top, value.Height, out var bottom) ||
				bottom != value.Bottom) return false;
		}
		return true;
	}

	internal static bool IsValidRecord(MuiAreaGeometryStateRecord value) =>
		IsValidState(value);
}

internal static class MuiAreaGeometryStateAdmission
{
	internal static bool Validate(MuiAreaGeometryStateRecord value) =>
		MuiAreaGeometryStateValidation.IsValidState(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) && !obj.IsNull &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiAreaGeometryStateField : byte
{
	Magic,
	Left,
	Top,
	Width,
	Height,
	Right,
	Bottom,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiAreaGeometryStateFieldCursor
{
	internal APTR Record;
	internal MuiAreaGeometryStateField Field;
}

internal static class MuiAreaGeometryStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaGeometryStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaGeometryStateFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaGeometryStateRecordMemoryCodec.TryGetFieldAddress(ref platform,
			cursor.Record, cursor.Field, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaGeometryStateRecordMemoryCodec.TryReadFieldUInt32(ref platform,
			record, field, out value);

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaGeometryStateRecordMemoryCodec.TryReadFieldInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiAreaGeometryStateRecordMemoryCodec.TryWriteFieldUInt32(ref platform,
			record, field, value);

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiAreaGeometryStateRecordMemoryCodec.TryWriteFieldInt32(ref platform,
			record, field, value);
}

// Struct-first guest-memory adapter. Geometry consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses, while the legacy field cursor remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiAreaGeometryStateRecordMemoryCodec
{
	private static bool TryGetAddressByOffset<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiAreaGeometryStateRecord.Size -
			MuiAreaGeometryStateRecord.FieldSize ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
				MuiAreaGeometryStateRecord.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address, MuiAreaGeometryStateRecord.FieldSize);
	}

	// Numeric offsets remain available only as a bounded compatibility seam for
	// diagnostics and older callers; live codecs use the typed field overloads.
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddressByOffset(ref platform, record, offset, out address);

	private static bool TryResolveFieldIndex(MuiAreaGeometryStateField field,
		out uint index)
	{
		if (field == MuiAreaGeometryStateField.Magic) index = 0;
		else if (field == MuiAreaGeometryStateField.Left) index = 1;
		else if (field == MuiAreaGeometryStateField.Top) index = 2;
		else if (field == MuiAreaGeometryStateField.Width) index = 3;
		else if (field == MuiAreaGeometryStateField.Height) index = 4;
		else if (field == MuiAreaGeometryStateField.Right) index = 5;
		else if (field == MuiAreaGeometryStateField.Bottom) index = 6;
		else
		{
			index = uint.MaxValue;
			return false;
		}
		return true;
	}

	internal static bool TryGetFieldAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryGetFieldAddress(ref platform, record, field, out address, out _);
	}

	internal static bool TryGetFieldAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, record,
				MuiAreaGeometryStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiAreaGeometryStateRecord.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiAreaGeometryStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddressByOffset(ref platform, record, offset, out var address))
			return false;
		return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	internal static bool TryReadFieldUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiAreaGeometryStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaGeometryStateField.Magic)
			value = state.Magic;
		else if (field == MuiAreaGeometryStateField.Left)
			value = unchecked((uint)state.Left);
		else if (field == MuiAreaGeometryStateField.Top)
			value = unchecked((uint)state.Top);
		else if (field == MuiAreaGeometryStateField.Width)
			value = unchecked((uint)state.Width);
		else if (field == MuiAreaGeometryStateField.Height)
			value = unchecked((uint)state.Height);
		else if (field == MuiAreaGeometryStateField.Right)
			value = unchecked((uint)state.Right);
		else if (field == MuiAreaGeometryStateField.Bottom)
			value = unchecked((uint)state.Bottom);
		else return false;
		return true;
	}

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, offset, out var raw))
			return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryReadFieldInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadFieldUInt32(ref platform, record, field, out var raw))
			return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddressByOffset(ref platform, record, offset, out var address))
			return false;
		return MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);
	}

	internal static bool TryWriteFieldUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaGeometryStateRecordCodec.TryReadStructural(ref platform,
			record, out var state)) return false;
		if (field == MuiAreaGeometryStateField.Magic)
			state.Magic = value;
		else if (field == MuiAreaGeometryStateField.Left)
			state.Left = unchecked((int)value);
		else if (field == MuiAreaGeometryStateField.Top)
			state.Top = unchecked((int)value);
		else if (field == MuiAreaGeometryStateField.Width)
			state.Width = unchecked((int)value);
		else if (field == MuiAreaGeometryStateField.Height)
			state.Height = unchecked((int)value);
		else if (field == MuiAreaGeometryStateField.Right)
			state.Right = unchecked((int)value);
		else if (field == MuiAreaGeometryStateField.Bottom)
			state.Bottom = unchecked((int)value);
		else return false;
		return MuiAreaGeometryStateRecordCodec.WriteRecord(ref platform, record,
			state);
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, offset, unchecked((uint)value));

	internal static bool TryWriteFieldInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteFieldUInt32(ref platform, record, field, unchecked((uint)value));
}

internal static class MuiAreaGeometryStateRecordCodec
{
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaGeometryStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var left) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var top) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var height) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var right) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var bottom)) return false;
		value.Left = unchecked((int)left);
		value.Top = unchecked((int)top);
		value.Width = unchecked((int)width);
		value.Height = unchecked((int)height);
		value.Right = unchecked((int)right);
		value.Bottom = unchecked((int)bottom);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiAreaGeometryStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Left)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Top)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Width)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Height)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Right)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Bottom)) &&
		MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadStructural(ref platform, address, out value) &&
			MuiAreaGeometryStateAdmission.Validate(value);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiAreaGeometryStateRecord value)
	where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiAreaGeometryStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
