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
	private static bool TryResolve(MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field, out uint offset, out uint size)
	{
		size = record == MuiVirtgroupInputRecordKind.Display
			? MuiVirtgroupDisplayStateRecord.Size
			: MuiVirtgroupPointerStateRecord.Size;
		switch (record)
		{
			case MuiVirtgroupInputRecordKind.Display:
				offset = field switch
				{
					MuiVirtgroupInputField.Magic => 0,
					MuiVirtgroupInputField.Left => 4,
					MuiVirtgroupInputField.Top => 8,
					MuiVirtgroupInputField.Width => 12,
					MuiVirtgroupInputField.Height => 16,
					_ => uint.MaxValue,
				};
				break;
			case MuiVirtgroupInputRecordKind.Pointer:
				offset = field switch
				{
					MuiVirtgroupInputField.Magic => 0,
					MuiVirtgroupInputField.Flags => 4,
					MuiVirtgroupInputField.StartX => 8,
					MuiVirtgroupInputField.StartY => 12,
					MuiVirtgroupInputField.StartLeft => 16,
					MuiVirtgroupInputField.StartTop => 20,
					MuiVirtgroupInputField.LastX => 24,
					MuiVirtgroupInputField.LastY => 28,
					_ => uint.MaxValue,
				};
				break;
			default:
				offset = uint.MaxValue;
				break;
		}
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiVirtgroupInputFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Record, cursor.Field, out var offset,
			out var size) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiVirtgroupInputFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiVirtgroupInputRecordKind record,
		MuiVirtgroupInputField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiVirtgroupInputFieldCursor);
		cursor.Address = address;
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiVirtgroupDisplayStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiVirtgroupDisplayStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiVirtgroupDisplayStateRecord.Size) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Magic,
				out var magic) || magic != MuiVirtgroupDisplayStateRecord.Cookie ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Left,
				out var left) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Top,
				out var top) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Width,
				out var width) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Height,
				out var height)) return false;
		value.Magic = magic;
		value.Left = unchecked((int)left);
		value.Top = unchecked((int)top);
		value.Width = unchecked((int)width);
		value.Height = unchecked((int)height);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiVirtgroupDisplayStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiVirtgroupDisplayStateRecord.Size) || value.Magic !=
			MuiVirtgroupDisplayStateRecord.Cookie) return false;
		return MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiVirtgroupInputRecordKind.Display,
			MuiVirtgroupInputField.Magic, value.Magic) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Left,
				unchecked((uint)value.Left)) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Top,
				unchecked((uint)value.Top)) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Width,
				unchecked((uint)value.Width)) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Display, MuiVirtgroupInputField.Height,
				unchecked((uint)value.Height));
	}
}

internal static class MuiVirtgroupPointerStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiVirtgroupPointerStateRecord.Size) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.Magic,
				out value.Magic) || value.Magic !=
			MuiVirtgroupPointerStateRecord.Cookie ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.Flags,
				out value.Flags) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartX,
				out var startX) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartY,
				out var startY) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartLeft,
				out var startLeft) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartTop,
				out var startTop) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.LastX,
				out var lastX) ||
			!MuiVirtgroupInputFieldCursorCodec.TryReadUInt32(ref platform, address,
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

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiVirtgroupPointerStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiVirtgroupPointerStateRecord.Size) || value.Magic !=
			MuiVirtgroupPointerStateRecord.Cookie) return false;
		return MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiVirtgroupInputRecordKind.Pointer,
			MuiVirtgroupInputField.Magic, value.Magic) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.Flags,
				value.Flags) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartX,
				unchecked((uint)value.StartX)) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartY,
				unchecked((uint)value.StartY)) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartLeft,
				unchecked((uint)value.StartLeft)) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.StartTop,
				unchecked((uint)value.StartTop)) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.LastX,
				unchecked((uint)value.LastX)) &&
			MuiVirtgroupInputFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiVirtgroupInputRecordKind.Pointer, MuiVirtgroupInputField.LastY,
				unchecked((uint)value.LastY));
	}
}
