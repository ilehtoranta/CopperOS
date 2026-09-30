/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// The display rectangle passed to Virtgroup by its parent is not the same as
// the virtual content rectangle published by MuiVirtgroupLayoutStateRecord.
// Keep that distinction explicit so pointer dragging can relayout the group
// without recovering a viewport from positional object storage.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiVirtgroupDisplayStateRecord
{
	internal const uint Size = 20;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint LeftOffset = 4;
	internal const uint TopOffset = 8;
	internal const uint WidthOffset = 12;
	internal const uint HeightOffset = 16;
	internal const uint Cookie = 0x56474450u; // 'VGDP'

	internal uint Magic;
	internal int Left;
	internal int Top;
	internal int Width;
	internal int Height;
}

// Guest-resident transient state for one Virtgroup mouse drag. The state is
// intentionally a fixed named record; no host object, delegate, or managed
// collection participates in the input path.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiVirtgroupPointerStateRecord
{
	internal const uint Size = 32;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint FlagsOffset = 4;
	internal const uint StartXOffset = 8;
	internal const uint StartYOffset = 12;
	internal const uint StartLeftOffset = 16;
	internal const uint StartTopOffset = 20;
	internal const uint LastXOffset = 24;
	internal const uint LastYOffset = 28;
	internal const uint Cookie = 0x5647494Eu; // 'VGIN'
	internal const uint ActiveFlag = 1;
	internal const uint CapturedFlag = 2;

	internal uint Magic;
	internal uint Flags;
	internal int StartX;
	internal int StartY;
	internal int StartLeft;
	internal int StartTop;
	internal int LastX;
	internal int LastY;
}

internal static class MuiVirtgroupDisplayStateValidation
{
	internal static bool IsValid(MuiVirtgroupDisplayStateRecord value) =>
		value.Magic == MuiVirtgroupDisplayStateRecord.Cookie &&
		value.Width >= 0 && value.Height >= 0;
}

internal static class MuiVirtgroupDisplayStateAdmission
{
	internal static bool Validate(MuiVirtgroupDisplayStateRecord value) =>
		MuiVirtgroupDisplayStateValidation.IsValid(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiVirtgroupDisplayStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal static class MuiVirtgroupPointerStateValidation
{
	internal static bool IsValid(MuiVirtgroupPointerStateRecord value)
	{
		if (value.Magic != MuiVirtgroupPointerStateRecord.Cookie ||
			(value.Flags & ~(MuiVirtgroupPointerStateRecord.ActiveFlag |
				MuiVirtgroupPointerStateRecord.CapturedFlag)) != 0)
			return false;
		return (value.Flags & MuiVirtgroupPointerStateRecord.CapturedFlag) == 0 ||
			(value.Flags & MuiVirtgroupPointerStateRecord.ActiveFlag) != 0;
	}
}

internal static class MuiVirtgroupPointerStateAdmission
{
	internal static bool Validate(MuiVirtgroupPointerStateRecord value) =>
		MuiVirtgroupPointerStateValidation.IsValid(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal enum MuiVirtgroupInputRecordKind : byte
{
	Display,
	Pointer,
}

internal enum MuiVirtgroupInputField : byte
{
	Magic,
	Flags,
	Left,
	Top,
	Width,
	Height,
	StartX,
	StartY,
	StartLeft,
	StartTop,
	LastX,
	LastY,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiVirtgroupInputFieldCursor
{
	internal APTR Address;
	internal MuiVirtgroupInputRecordKind Record;
	internal MuiVirtgroupInputField Field;
}

internal static class MuiVirtgroupInputFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiVirtgroupInputFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupInputRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Record, cursor.Field, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiVirtgroupInputFieldCursor cursor, out APTR address, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupInputRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Record, cursor.Field, out address, out fieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform,
			address, record, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, record, field, value);
	}
}

// Struct-first guest-memory adapter. Display and pointer records keep their
// distinct named structs; this bounded adapter is the only fixed-layout
// translation shared by the two input paths.
internal static class MuiVirtgroupInputRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field, out uint index, out uint recordSize)
	{
		index = uint.MaxValue;
		recordSize = 0;
		if (record == MuiVirtgroupInputRecordKind.Display)
		{
			if (field == MuiVirtgroupInputField.Magic)
			{
				index = 0; recordSize = MuiVirtgroupDisplayStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.Left)
			{
				index = 1; recordSize = MuiVirtgroupDisplayStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.Top)
			{
				index = 2; recordSize = MuiVirtgroupDisplayStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.Width)
			{
				index = 3; recordSize = MuiVirtgroupDisplayStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.Height)
			{
				index = 4; recordSize = MuiVirtgroupDisplayStateRecord.Size;
				return true;
			}
			else return false;
		}
		else if (record == MuiVirtgroupInputRecordKind.Pointer)
		{
			if (field == MuiVirtgroupInputField.Magic)
			{
				index = 0; recordSize = MuiVirtgroupPointerStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.Flags)
			{
				index = 1; recordSize = MuiVirtgroupPointerStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.StartX)
			{
				index = 2; recordSize = MuiVirtgroupPointerStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.StartY)
			{
				index = 3; recordSize = MuiVirtgroupPointerStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.StartLeft)
			{
				index = 4; recordSize = MuiVirtgroupPointerStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.StartTop)
			{
				index = 5; recordSize = MuiVirtgroupPointerStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.LastX)
			{
				index = 6; recordSize = MuiVirtgroupPointerStateRecord.Size;
				return true;
			}
			else if (field == MuiVirtgroupInputField.LastY)
			{
				index = 7; recordSize = MuiVirtgroupPointerStateRecord.Size;
				return true;
			}
			else return false;
		}
		else return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field,
		out APTR fieldAddress)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryGetAddress(ref platform, address, record, field,
			out fieldAddress, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field,
		out APTR fieldAddress, out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		fieldAddress = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(record, field, out var index,
			out var recordSize) ||
			!MuiGuestStructCursor.TryCreate(ref platform, address, recordSize,
				out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
				out var candidate)) return false;
			if (current == index)
			{
				fieldAddress = candidate;
				fieldSize = 4;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field,
		out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress)) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, address, record, field,
			out var fieldAddress)) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiVirtgroupDisplayStateRecordCodec
{
	// Declaration-order display rectangle: Magic, signed Left/Top, then
	// Width/Height. Signed LONGs travel as raw ULONG bit patterns.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiVirtgroupDisplayStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiVirtgroupDisplayStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var left) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var top) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var height)) return false;
		value.Left = unchecked((int)left);
		value.Top = unchecked((int)top);
		value.Width = unchecked((int)width);
		value.Height = unchecked((int)height);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupDisplayStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiVirtgroupDisplayStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Left)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Top)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Width)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.Height)) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiVirtgroupDisplayStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiVirtgroupDisplayStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiVirtgroupDisplayStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiVirtgroupDisplayStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiVirtgroupDisplayStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}

internal static class MuiVirtgroupPointerStateRecordCodec
{
	// Declaration-order pointer-drag record: magic/flags followed by the six
	// signed coordinates. The cursor keeps this transient record typed and
	// bounded without retaining managed state.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiVirtgroupPointerStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var startX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var startY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var startLeft) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var startTop) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var lastX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var lastY)) return false;
		value.StartX = unchecked((int)startX);
		value.StartY = unchecked((int)startY);
		value.StartLeft = unchecked((int)startLeft);
		value.StartTop = unchecked((int)startTop);
		value.LastX = unchecked((int)lastX);
		value.LastY = unchecked((int)lastY);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiVirtgroupPointerStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.StartX)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.StartY)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.StartLeft)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.StartTop)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.LastX)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.LastY)) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiVirtgroupPointerStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiVirtgroupPointerStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
