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
	private static bool TryResolve(MuiAreaGeometryStateField field,
		out uint offset)
	{
		offset = field switch
		{
			MuiAreaGeometryStateField.Magic => 0,
			MuiAreaGeometryStateField.Left => 4,
			MuiAreaGeometryStateField.Top => 8,
			MuiAreaGeometryStateField.Width => 12,
			MuiAreaGeometryStateField.Height => 16,
			MuiAreaGeometryStateField.Right => 20,
			MuiAreaGeometryStateField.Bottom => 24,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiAreaGeometryStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset || !platform.IsMapped(
				cursor.Record, MuiAreaGeometryStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiAreaGeometryStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, field, out var raw)) return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiAreaGeometryStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiAreaGeometryStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, field, unchecked((uint)value));
}

// Struct-first guest-memory adapter. Geometry consumers use the named record;
// this bounded adapter is the only layer that translates its fixed guest
// layout into addresses, while the legacy field cursor remains available for
// compatibility and malformed-state diagnostics.
internal static class MuiAreaGeometryStateRecordMemoryCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (record.IsNull || offset > MuiAreaGeometryStateRecord.Size - 4 ||
			record.Raw > uint.MaxValue - offset || !platform.IsMapped(record,
			MuiAreaGeometryStateRecord.Size)) return false;
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

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, offset, out var raw)) return false;
		value = unchecked((int)raw);
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

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, uint offset, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, offset, unchecked((uint)value));
}

internal static class MuiAreaGeometryStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiAreaGeometryStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiAreaGeometryStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, 0, out value.Magic) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryReadInt32(ref platform, address,
			4, out value.Left) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryReadInt32(ref platform, address,
			8, out value.Top) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryReadInt32(ref platform, address,
			12, out value.Width) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryReadInt32(ref platform, address,
			16, out value.Height) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryReadInt32(ref platform, address,
			20, out value.Right) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryReadInt32(ref platform, address,
			24, out value.Bottom);
	}

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
		return MuiAreaGeometryStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, 0, value.Magic) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryWriteInt32(ref platform, address,
			4, value.Left) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryWriteInt32(ref platform, address,
			8, value.Top) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryWriteInt32(ref platform, address,
			12, value.Width) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryWriteInt32(ref platform, address,
			16, value.Height) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryWriteInt32(ref platform, address,
			20, value.Right) &&
			MuiAreaGeometryStateRecordMemoryCodec.TryWriteInt32(ref platform, address,
			24, value.Bottom);
	}
}
