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
	{
		return MuiScrollgroupViewportStateRecordMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Field, out address);
	}

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
	private static bool TryResolve(MuiScrollgroupViewportField field,
		out uint offset)
	{
		if (field == MuiScrollgroupViewportField.Magic)
			offset = MuiScrollgroupViewportStateRecord.MagicOffset;
		else if (field == MuiScrollgroupViewportField.ViewportWidth)
			offset = MuiScrollgroupViewportStateRecord.ViewportWidthOffset;
		else if (field == MuiScrollgroupViewportField.ViewportHeight)
			offset = MuiScrollgroupViewportStateRecord.ViewportHeightOffset;
		else if (field == MuiScrollgroupViewportField.ContentWidth)
			offset = MuiScrollgroupViewportStateRecord.ContentWidthOffset;
		else if (field == MuiScrollgroupViewportField.ContentHeight)
			offset = MuiScrollgroupViewportStateRecord.ContentHeightOffset;
		else if (field == MuiScrollgroupViewportField.MaximumScrollX)
			offset = MuiScrollgroupViewportStateRecord.MaximumScrollXOffset;
		else if (field == MuiScrollgroupViewportField.MaximumScrollY)
			offset = MuiScrollgroupViewportStateRecord.MaximumScrollYOffset;
		else if (field == MuiScrollgroupViewportField.ScrollLeft)
			offset = MuiScrollgroupViewportStateRecord.ScrollLeftOffset;
		else if (field == MuiScrollgroupViewportField.ScrollTop)
			offset = MuiScrollgroupViewportStateRecord.ScrollTopOffset;
		else if (field == MuiScrollgroupViewportField.HorizontalBarVisible)
			offset = MuiScrollgroupViewportStateRecord.HorizontalBarVisibleOffset;
		else if (field == MuiScrollgroupViewportField.VerticalBarVisible)
			offset = MuiScrollgroupViewportStateRecord.VerticalBarVisibleOffset;
		else
		{
			offset = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiScrollgroupViewportField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset) || record.IsNull ||
			record.Raw > uint.MaxValue - offset) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(record, MuiScrollgroupViewportStateRecord.Size) &&
			platform.IsMapped(address, MuiScrollgroupViewportStateRecord.FieldSize);
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
