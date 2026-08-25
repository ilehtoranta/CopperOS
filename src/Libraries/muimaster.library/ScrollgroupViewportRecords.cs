/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollgroupViewportStateRecord
{
	internal const uint Size = 44;
	internal const uint Cookie = 0x53565031u; // 'SVP1'

	internal uint Magic;
	internal int ViewportWidth;
	internal int ViewportHeight;
	internal int ContentWidth;
	internal int ContentHeight;
	internal int MaximumScrollX;
	internal int MaximumScrollY;
	internal int ScrollLeft;
	internal int ScrollTop;
	internal uint HorizontalBarVisible;
	internal uint VerticalBarVisible;
}

internal enum MuiScrollgroupViewportField : byte
{
	Magic,
	ViewportWidth,
	ViewportHeight,
	ContentWidth,
	ContentHeight,
	MaximumScrollX,
	MaximumScrollY,
	ScrollLeft,
	ScrollTop,
	HorizontalBarVisible,
	VerticalBarVisible,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiScrollgroupViewportFieldCursor
{
	internal APTR Address;
	internal MuiScrollgroupViewportField Field;
}

internal static class MuiScrollgroupViewportFieldCursorCodec
{
	private static bool TryResolve(MuiScrollgroupViewportField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiScrollgroupViewportField.Magic:
			case MuiScrollgroupViewportField.ViewportWidth:
			case MuiScrollgroupViewportField.ViewportHeight:
			case MuiScrollgroupViewportField.ContentWidth:
			case MuiScrollgroupViewportField.ContentHeight:
			case MuiScrollgroupViewportField.MaximumScrollX:
			case MuiScrollgroupViewportField.MaximumScrollY:
			case MuiScrollgroupViewportField.ScrollLeft:
			case MuiScrollgroupViewportField.ScrollTop:
			case MuiScrollgroupViewportField.HorizontalBarVisible:
			case MuiScrollgroupViewportField.VerticalBarVisible:
				offset = (uint)field * 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollgroupViewportFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address,
				MuiScrollgroupViewportStateRecord.Size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupViewportField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiScrollgroupViewportFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupViewportField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiScrollgroupViewportFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiScrollgroupViewportStateRecordCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollgroupViewportStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollgroupViewportStateRecord.Size) ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.Magic, out var magic) ||
			magic != MuiScrollgroupViewportStateRecord.Cookie ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.ViewportWidth,
				out var viewportWidth) ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.ViewportHeight,
				out var viewportHeight) ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.ContentWidth,
				out var contentWidth) ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.ContentHeight,
				out var contentHeight) ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.MaximumScrollX,
				out var maximumScrollX) ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.MaximumScrollY,
				out var maximumScrollY) ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.ScrollLeft,
				out var scrollLeft) ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.ScrollTop,
				out var scrollTop) ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.HorizontalBarVisible,
				out value.HorizontalBarVisible) ||
			!MuiScrollgroupViewportFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiScrollgroupViewportField.VerticalBarVisible,
				out value.VerticalBarVisible)) return false;
		value.Magic = magic;
		value.ViewportWidth = unchecked((int)viewportWidth);
		value.ViewportHeight = unchecked((int)viewportHeight);
		value.ContentWidth = unchecked((int)contentWidth);
		value.ContentHeight = unchecked((int)contentHeight);
		value.MaximumScrollX = unchecked((int)maximumScrollX);
		value.MaximumScrollY = unchecked((int)maximumScrollY);
		value.ScrollLeft = unchecked((int)scrollLeft);
		value.ScrollTop = unchecked((int)scrollTop);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollgroupViewportStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiScrollgroupViewportStateRecord.Size) || value.Magic !=
			MuiScrollgroupViewportStateRecord.Cookie) return false;
		return MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiScrollgroupViewportField.Magic, value.Magic) &&
			MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupViewportField.ViewportWidth,
				unchecked((uint)value.ViewportWidth)) &&
			MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupViewportField.ViewportHeight,
				unchecked((uint)value.ViewportHeight)) &&
			MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupViewportField.ContentWidth,
				unchecked((uint)value.ContentWidth)) &&
			MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupViewportField.ContentHeight,
				unchecked((uint)value.ContentHeight)) &&
			MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupViewportField.MaximumScrollX,
				unchecked((uint)value.MaximumScrollX)) &&
			MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupViewportField.MaximumScrollY,
				unchecked((uint)value.MaximumScrollY)) &&
			MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupViewportField.ScrollLeft,
				unchecked((uint)value.ScrollLeft)) &&
			MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupViewportField.ScrollTop,
				unchecked((uint)value.ScrollTop)) &&
			MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupViewportField.HorizontalBarVisible,
				value.HorizontalBarVisible) &&
			MuiScrollgroupViewportFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiScrollgroupViewportField.VerticalBarVisible,
				value.VerticalBarVisible);
	}
}
