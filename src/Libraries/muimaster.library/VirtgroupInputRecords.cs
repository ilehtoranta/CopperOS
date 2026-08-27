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
			cursor.Address, cursor.Record, cursor.Field, out address);
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
	private static bool TryResolve(MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field, out uint offset, out uint size)
	{
		offset = 0;
		size = 0;
		if (record == MuiVirtgroupInputRecordKind.Display)
		{
			size = MuiVirtgroupDisplayStateRecord.Size;
			if (field == MuiVirtgroupInputField.Magic)
				offset = MuiVirtgroupDisplayStateRecord.MagicOffset;
			else if (field == MuiVirtgroupInputField.Left)
				offset = MuiVirtgroupDisplayStateRecord.LeftOffset;
			else if (field == MuiVirtgroupInputField.Top)
				offset = MuiVirtgroupDisplayStateRecord.TopOffset;
			else if (field == MuiVirtgroupInputField.Width)
				offset = MuiVirtgroupDisplayStateRecord.WidthOffset;
			else if (field == MuiVirtgroupInputField.Height)
				offset = MuiVirtgroupDisplayStateRecord.HeightOffset;
			else return false;
		}
		else if (record == MuiVirtgroupInputRecordKind.Pointer)
		{
			size = MuiVirtgroupPointerStateRecord.Size;
			if (field == MuiVirtgroupInputField.Magic)
				offset = MuiVirtgroupPointerStateRecord.MagicOffset;
			else if (field == MuiVirtgroupInputField.Flags)
				offset = MuiVirtgroupPointerStateRecord.FlagsOffset;
			else if (field == MuiVirtgroupInputField.StartX)
				offset = MuiVirtgroupPointerStateRecord.StartXOffset;
			else if (field == MuiVirtgroupInputField.StartY)
				offset = MuiVirtgroupPointerStateRecord.StartYOffset;
			else if (field == MuiVirtgroupInputField.StartLeft)
				offset = MuiVirtgroupPointerStateRecord.StartLeftOffset;
			else if (field == MuiVirtgroupInputField.StartTop)
				offset = MuiVirtgroupPointerStateRecord.StartTopOffset;
			else if (field == MuiVirtgroupInputField.LastX)
				offset = MuiVirtgroupPointerStateRecord.LastXOffset;
			else if (field == MuiVirtgroupInputField.LastY)
				offset = MuiVirtgroupPointerStateRecord.LastYOffset;
			else return false;
		}
		else return false;
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field,
		out APTR fieldAddress)
		where TPlatform : struct, IMuiGuestMemory
	{
		fieldAddress = APTR.Null;
		if (!TryResolve(record, field, out var offset, out var size) ||
			address.IsNull || offset > size - 4 ||
			address.Raw > uint.MaxValue - offset || !platform.IsMapped(address,
			size)) return false;
		fieldAddress = APTR.FromPointer(address.Raw + offset);
		var fieldSize = record == MuiVirtgroupInputRecordKind.Display
			? MuiVirtgroupDisplayStateRecord.FieldSize
			: MuiVirtgroupPointerStateRecord.FieldSize;
		return platform.IsMapped(fieldAddress, fieldSize);
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
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiVirtgroupDisplayStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Magic,
			out var magic) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Left,
			out var left) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Top,
			out var top) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Width,
			out var width) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Height,
			out var height)) return false;
		value.Magic = magic;
		value.Left = unchecked((int)left);
		value.Top = unchecked((int)top);
		value.Width = unchecked((int)width);
		value.Height = unchecked((int)height);
		return true;
	}

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
		return MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiVirtgroupInputRecordKind.Display,
			MuiVirtgroupInputField.Magic, value.Magic) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Left,
			unchecked((uint)value.Left)) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Top,
			unchecked((uint)value.Top)) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Width,
			unchecked((uint)value.Width)) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Height,
			unchecked((uint)value.Height));
	}
}

internal static class MuiVirtgroupPointerStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.Magic,
			out value.Magic) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.Flags,
			out value.Flags) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartX,
			out var startX) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartY,
			out var startY) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartLeft,
			out var startLeft) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartTop,
			out var startTop) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.LastX,
			out var lastX) ||
			!MuiVirtgroupInputRecordMemoryCodec.TryReadUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.LastY,
			out var lastY)) return false;
		value.StartX = unchecked((int)startX);
		value.StartY = unchecked((int)startY);
		value.StartLeft = unchecked((int)startLeft);
		value.StartTop = unchecked((int)startTop);
		value.LastX = unchecked((int)lastX);
		value.LastY = unchecked((int)lastY);
		return true;
	}

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
		return MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, MuiVirtgroupInputRecordKind.Pointer,
			MuiVirtgroupInputField.Magic, value.Magic) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.Flags,
			value.Flags) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartX,
			unchecked((uint)value.StartX)) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartY,
			unchecked((uint)value.StartY)) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartLeft,
			unchecked((uint)value.StartLeft)) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartTop,
			unchecked((uint)value.StartTop)) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.LastX,
			unchecked((uint)value.LastX)) &&
			MuiVirtgroupInputRecordMemoryCodec.TryWriteUInt32(ref platform, address,
			MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.LastY,
			unchecked((uint)value.LastY));
	}
}
