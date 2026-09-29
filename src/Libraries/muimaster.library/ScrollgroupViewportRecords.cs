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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ViewportWidthOffset = 4;
	internal const uint ViewportHeightOffset = 8;
	internal const uint ContentWidthOffset = 12;
	internal const uint ContentHeightOffset = 16;
	internal const uint MaximumScrollXOffset = 20;
	internal const uint MaximumScrollYOffset = 24;
	internal const uint ScrollLeftOffset = 28;
	internal const uint ScrollTopOffset = 32;
	internal const uint HorizontalBarVisibleOffset = 36;
	internal const uint VerticalBarVisibleOffset = 40;
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

internal static class MuiScrollgroupViewportStateValidation
{
	internal static bool IsValid(MuiScrollgroupViewportStateRecord value)
	{
		if (value.Magic != MuiScrollgroupViewportStateRecord.Cookie ||
			value.ViewportWidth < 0 || value.ViewportHeight < 0 ||
			value.ContentWidth < 0 || value.ContentHeight < 0 ||
			value.MaximumScrollX < 0 || value.MaximumScrollY < 0 ||
			value.ScrollLeft < 0 || value.ScrollTop < 0 ||
			value.MaximumScrollX > value.ContentWidth ||
			value.MaximumScrollY > value.ContentHeight ||
			value.ScrollLeft > value.MaximumScrollX ||
			value.ScrollTop > value.MaximumScrollY ||
			value.HorizontalBarVisible > 1 || value.VerticalBarVisible > 1)
			return false;
		return true;
	}
}

internal static class MuiScrollgroupViewportStateAdmission
{
	internal static bool Validate(MuiScrollgroupViewportStateRecord value) =>
		MuiScrollgroupViewportStateValidation.IsValid(value);

	internal static bool ValidateLive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiScrollgroupViewportStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		Validate(value) &&
		!MuiHeadlessObjectCore.FindObject(ref platform, state, obj).IsNull;
}

internal static class MuiScrollgroupViewportFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollgroupViewportFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
		=> TryGetAddress(ref platform, cursor, out address, out _);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollgroupViewportFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiScrollgroupViewportStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor, out address, out fieldSize);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupViewportField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupViewportStateRecordMemoryCodec.TryReadUInt32(ref platform,
			address, field, out value);
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupViewportField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiScrollgroupViewportStateRecordMemoryCodec.TryWriteUInt32(ref platform,
			address, field, value);
	}
}

// Struct-first guest-memory adapter. Viewport geometry, scroll positions, and
// visibility flags remain named semantic fields; bounded fixed-layout
// translation is isolated here.
internal static class MuiScrollgroupViewportStateRecordMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiScrollgroupViewportField field,
		out uint index)
	{
		if (field == MuiScrollgroupViewportField.Magic)
			index = 0;
		else if (field == MuiScrollgroupViewportField.ViewportWidth)
			index = 1;
		else if (field == MuiScrollgroupViewportField.ViewportHeight)
			index = 2;
		else if (field == MuiScrollgroupViewportField.ContentWidth)
			index = 3;
		else if (field == MuiScrollgroupViewportField.ContentHeight)
			index = 4;
		else if (field == MuiScrollgroupViewportField.MaximumScrollX)
			index = 5;
		else if (field == MuiScrollgroupViewportField.MaximumScrollY)
			index = 6;
		else if (field == MuiScrollgroupViewportField.ScrollLeft)
			index = 7;
		else if (field == MuiScrollgroupViewportField.ScrollTop)
			index = 8;
		else if (field == MuiScrollgroupViewportField.HorizontalBarVisible)
			index = 9;
		else if (field == MuiScrollgroupViewportField.VerticalBarVisible)
			index = 10;
		else
		{
			index = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupViewportField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiScrollgroupViewportFieldCursor);
		cursor.Address = record;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiScrollgroupViewportFieldCursor cursor, out APTR address,
		out uint fieldSize)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolveFieldIndex(cursor.Field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, cursor.Address,
				MuiScrollgroupViewportStateRecord.Size, out var structCursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref structCursor,
				MuiScrollgroupViewportStateRecord.FieldSize, out var candidate))
				return false;
			if (current == index)
			{
				address = candidate;
				fieldSize = MuiScrollgroupViewportStateRecord.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupViewportField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupViewportField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiScrollgroupViewportStateRecordCodec
{
	// Declaration-order guest record: magic, viewport/content geometry,
	// scroll extents/positions, then visibility BOOLs.  Signed LONG values are
	// transported as their lossless ULONG bit patterns by the cursor.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiScrollgroupViewportStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiScrollgroupViewportStateRecord.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var viewportWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var viewportHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var contentWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var contentHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var maximumScrollX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var maximumScrollY) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var scrollLeft) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var scrollTop) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.HorizontalBarVisible) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.VerticalBarVisible)) return false;
		value.ViewportWidth = unchecked((int)viewportWidth);
		value.ViewportHeight = unchecked((int)viewportHeight);
		value.ContentWidth = unchecked((int)contentWidth);
		value.ContentHeight = unchecked((int)contentHeight);
		value.MaximumScrollX = unchecked((int)maximumScrollX);
		value.MaximumScrollY = unchecked((int)maximumScrollY);
		value.ScrollLeft = unchecked((int)scrollLeft);
		value.ScrollTop = unchecked((int)scrollTop);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiScrollgroupViewportStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiScrollgroupViewportStateRecord.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.ViewportWidth)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.ViewportHeight)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.ContentWidth)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.ContentHeight)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.MaximumScrollX)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.MaximumScrollY)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.ScrollLeft)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.ScrollTop)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.HorizontalBarVisible) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.VerticalBarVisible) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiScrollgroupViewportStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiScrollgroupViewportStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadStructural(ref platform, address, out value) &&
		MuiScrollgroupViewportStateAdmission.Validate(value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiScrollgroupViewportStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiScrollgroupViewportStateAdmission.Validate(value)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
