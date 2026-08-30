/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Neutral Listview scroller geometry shared by draw and pointer input.  The
// public Listview still owns no Prop object; this named value keeps its track,
// thumb, and bounded first-row range coherent without a managed geometry node.
internal struct MuiListviewScrollerGeometry
{
	internal int TrackLeft;
	internal int TrackTop;
	internal int TrackRight;
	internal int TrackBottom;
	internal int ThumbLeft;
	internal int ThumbTop;
	internal int ThumbRight;
	internal int ThumbBottom;
	internal uint First;
	internal uint MaxFirst;
}

// Neutral horizontal Listview scroller geometry. The List owns the policy and
// content/view widths; this value only joins the named track/thumb rectangles
// used by layout, drawing, and the future horizontal input seam.
internal struct MuiListviewHorizontalScrollerGeometry
{
	internal int TrackLeft;
	internal int TrackTop;
	internal int TrackRight;
	internal int TrackBottom;
	internal int ThumbLeft;
	internal int ThumbTop;
	internal int ThumbRight;
	internal int ThumbBottom;
	internal uint ContentWidth;
	internal uint ViewWidth;
	internal uint ScrollX;
	internal uint MaxScrollX;
}

// Guest-resident horizontal Listview scroller projection.  Keep the track,
// thumb, and bounded child-scroll values together so drawing and pointer input
// consume one canonical geometry record after child/layout synchronization.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewHorizontalScrollerState
{
	internal const uint Size = 52;
	internal const uint Cookie = 0x4C564852u; // 'LVHR'
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint TrackLeftOffset = 4;
	internal const uint TrackTopOffset = 8;
	internal const uint TrackRightOffset = 12;
	internal const uint TrackBottomOffset = 16;
	internal const uint ThumbLeftOffset = 20;
	internal const uint ThumbTopOffset = 24;
	internal const uint ThumbRightOffset = 28;
	internal const uint ThumbBottomOffset = 32;
	internal const uint ContentWidthOffset = 36;
	internal const uint ViewWidthOffset = 40;
	internal const uint ScrollXOffset = 44;
	internal const uint MaxScrollXOffset = 48;

	internal uint Magic;
	internal int TrackLeft;
	internal int TrackTop;
	internal int TrackRight;
	internal int TrackBottom;
	internal int ThumbLeft;
	internal int ThumbTop;
	internal int ThumbRight;
	internal int ThumbBottom;
	internal uint ContentWidth;
	internal uint ViewWidth;
	internal uint ScrollX;
	internal uint MaxScrollX;
}

internal enum MuiListviewHorizontalScrollerField : byte
{
	Magic,
	TrackLeft,
	TrackTop,
	TrackRight,
	TrackBottom,
	ThumbLeft,
	ThumbTop,
	ThumbRight,
	ThumbBottom,
	ContentWidth,
	ViewWidth,
	ScrollX,
	MaxScrollX,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewHorizontalScrollerFieldCursor
{
	internal APTR Record;
	internal MuiListviewHorizontalScrollerField Field;
}

internal static class MuiListviewHorizontalScrollerMemoryCodec
{
	private static bool TryResolve(
		MuiListviewHorizontalScrollerField field, out uint offset,
		out uint recordSize)
	{
		recordSize = MuiListviewHorizontalScrollerState.Size;
		if (field == MuiListviewHorizontalScrollerField.Magic)
			offset = MuiListviewHorizontalScrollerState.MagicOffset;
		else if (field == MuiListviewHorizontalScrollerField.TrackLeft)
			offset = MuiListviewHorizontalScrollerState.TrackLeftOffset;
		else if (field == MuiListviewHorizontalScrollerField.TrackTop)
			offset = MuiListviewHorizontalScrollerState.TrackTopOffset;
		else if (field == MuiListviewHorizontalScrollerField.TrackRight)
			offset = MuiListviewHorizontalScrollerState.TrackRightOffset;
		else if (field == MuiListviewHorizontalScrollerField.TrackBottom)
			offset = MuiListviewHorizontalScrollerState.TrackBottomOffset;
		else if (field == MuiListviewHorizontalScrollerField.ThumbLeft)
			offset = MuiListviewHorizontalScrollerState.ThumbLeftOffset;
		else if (field == MuiListviewHorizontalScrollerField.ThumbTop)
			offset = MuiListviewHorizontalScrollerState.ThumbTopOffset;
		else if (field == MuiListviewHorizontalScrollerField.ThumbRight)
			offset = MuiListviewHorizontalScrollerState.ThumbRightOffset;
		else if (field == MuiListviewHorizontalScrollerField.ThumbBottom)
			offset = MuiListviewHorizontalScrollerState.ThumbBottomOffset;
		else if (field == MuiListviewHorizontalScrollerField.ContentWidth)
			offset = MuiListviewHorizontalScrollerState.ContentWidthOffset;
		else if (field == MuiListviewHorizontalScrollerField.ViewWidth)
			offset = MuiListviewHorizontalScrollerState.ViewWidthOffset;
		else if (field == MuiListviewHorizontalScrollerField.ScrollX)
			offset = MuiListviewHorizontalScrollerState.ScrollXOffset;
		else if (field == MuiListviewHorizontalScrollerField.MaxScrollX)
			offset = MuiListviewHorizontalScrollerState.MaxScrollXOffset;
		else
		{
			offset = 0;
				recordSize = 0;
				return false;
			}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var recordSize) ||
			record.IsNull || record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, recordSize)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address,
			MuiListviewHorizontalScrollerState.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, field, out var raw))
			return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, field, unchecked((uint)value));
}

// Compatibility wrapper retained for callers that still construct the typed
// Listview horizontal scroller cursor. Live state serialization uses the
// direct record adapter.
internal static class MuiListviewHorizontalScrollerFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListviewHorizontalScrollerFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewHorizontalScrollerMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewHorizontalScrollerMemoryCodec.TryReadUInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewHorizontalScrollerMemoryCodec.TryWriteUInt32(ref platform,
			record, field, value);

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, out int value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewHorizontalScrollerMemoryCodec.TryReadInt32(ref platform,
			record, field, out value);

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewHorizontalScrollerMemoryCodec.TryWriteInt32(ref platform,
			record, field, value);
}

internal static class MuiListviewHorizontalScrollerStateCodec
{
	// Declaration-order horizontal scroller record: magic, track/thumb
	// rectangles, then content/view/scroll extents. Signed coordinates are
	// transported losslessly as ULONG bit patterns.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewHorizontalScrollerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListviewHorizontalScrollerState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var trackLeft) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var trackTop) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var trackRight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var trackBottom) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var thumbLeft) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var thumbTop) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var thumbRight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var thumbBottom) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ContentWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ViewWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ScrollX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MaxScrollX)) return false;
		value.TrackLeft = unchecked((int)trackLeft);
		value.TrackTop = unchecked((int)trackTop);
		value.TrackRight = unchecked((int)trackRight);
		value.TrackBottom = unchecked((int)trackBottom);
		value.ThumbLeft = unchecked((int)thumbLeft);
		value.ThumbTop = unchecked((int)thumbTop);
		value.ThumbRight = unchecked((int)thumbRight);
		value.ThumbBottom = unchecked((int)thumbBottom);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewHorizontalScrollerState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListviewHorizontalScrollerState.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.TrackLeft)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.TrackTop)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.TrackRight)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.TrackBottom)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.ThumbLeft)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.ThumbTop)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.ThumbRight)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.ThumbBottom)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ContentWidth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ViewWidth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ScrollX) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MaxScrollX) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewHorizontalScrollerState value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewHorizontalScrollerState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiListviewHorizontalScrollerState.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewHorizontalScrollerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewHorizontalScrollerState.Size) || value.Magic !=
			MuiListviewHorizontalScrollerState.Cookie) return false;
		return WriteRecord(ref platform, address, value);
	}
}

// Guest-resident state for a horizontal thumb drag. The List remains the
// authority for ScrollX; this record only retains the pointer grab and the
// starting offset needed by the Listview gesture.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewHorizontalScrollerDragState
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x48534452u; // 'HSDR'
	internal const uint ActiveFlag = 1;
	internal const uint CapturedFlag = 2;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint GrabOffsetOffset = 4;
	internal const uint StartScrollOffset = 8;
	internal const uint LastPointerOffset = 12;
	internal const uint FlagsOffset = 16;

	internal uint Magic;
	internal int GrabOffset;
	internal uint StartScroll;
	internal int LastPointer;
	internal uint Flags;
}

internal enum MuiListviewHorizontalScrollerDragStateField : byte
{
	Magic,
	GrabOffset,
	StartScroll,
	LastPointer,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewHorizontalScrollerDragStateFieldCursor
{
	internal APTR Address;
	internal MuiListviewHorizontalScrollerDragStateField Field;
}

internal static class MuiListviewHorizontalScrollerDragStateMemoryCodec
{
	private static bool TryResolve(
		MuiListviewHorizontalScrollerDragStateField field, out uint offset,
		out uint recordSize)
	{
		recordSize = MuiListviewHorizontalScrollerDragState.Size;
		if (field == MuiListviewHorizontalScrollerDragStateField.Magic)
			offset = MuiListviewHorizontalScrollerDragState.MagicOffset;
		else if (field == MuiListviewHorizontalScrollerDragStateField.GrabOffset)
			offset = MuiListviewHorizontalScrollerDragState.GrabOffsetOffset;
		else if (field == MuiListviewHorizontalScrollerDragStateField.StartScroll)
			offset = MuiListviewHorizontalScrollerDragState.StartScrollOffset;
		else if (field == MuiListviewHorizontalScrollerDragStateField.LastPointer)
			offset = MuiListviewHorizontalScrollerDragState.LastPointerOffset;
		else if (field == MuiListviewHorizontalScrollerDragStateField.Flags)
			offset = MuiListviewHorizontalScrollerDragState.FlagsOffset;
		else
		{
			offset = 0;
			recordSize = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerDragStateField field,
		out APTR fieldAddress)
		where TPlatform : struct, IMuiGuestMemory
	{
		fieldAddress = APTR.Null;
		if (!TryResolve(field, out var offset, out var recordSize) ||
			record.IsNull || record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, recordSize))
			return false;
		fieldAddress = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(fieldAddress,
			MuiListviewHorizontalScrollerDragState.FieldSize);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewHorizontalScrollerDragStateField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, address, field, out var fieldAddress))
			return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewHorizontalScrollerDragStateField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, address, field, out var fieldAddress))
			return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListviewHorizontalScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewHorizontalScrollerDragState.Size) ||
			!TryReadUInt32(ref platform, address,
				MuiListviewHorizontalScrollerDragStateField.Magic, out value.Magic) ||
			!TryReadUInt32(ref platform, address,
				MuiListviewHorizontalScrollerDragStateField.GrabOffset,
				out var grabOffset) ||
			!TryReadUInt32(ref platform, address,
				MuiListviewHorizontalScrollerDragStateField.StartScroll,
				out value.StartScroll) ||
			!TryReadUInt32(ref platform, address,
				MuiListviewHorizontalScrollerDragStateField.LastPointer,
				out var lastPointer) ||
			!TryReadUInt32(ref platform, address,
				MuiListviewHorizontalScrollerDragStateField.Flags, out value.Flags))
			return false;
		value.GrabOffset = unchecked((int)grabOffset);
		value.LastPointer = unchecked((int)lastPointer);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewHorizontalScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiListviewHorizontalScrollerDragState.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewHorizontalScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewHorizontalScrollerDragState.Size)) return false;
		return TryWriteUInt32(ref platform, address,
			MuiListviewHorizontalScrollerDragStateField.Magic, value.Magic) &&
			TryWriteUInt32(ref platform, address,
				MuiListviewHorizontalScrollerDragStateField.GrabOffset,
				unchecked((uint)value.GrabOffset)) &&
			TryWriteUInt32(ref platform, address,
				MuiListviewHorizontalScrollerDragStateField.StartScroll,
				value.StartScroll) &&
			TryWriteUInt32(ref platform, address,
				MuiListviewHorizontalScrollerDragStateField.LastPointer,
				unchecked((uint)value.LastPointer)) &&
			TryWriteUInt32(ref platform, address,
				MuiListviewHorizontalScrollerDragStateField.Flags, value.Flags);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewHorizontalScrollerDragStateField field,
		uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewHorizontalScrollerDragStateMemoryCodec.TryWriteUInt32(
			ref platform, address, field, value);
}

// Public record codec retained for existing callers while live paths use the
// explicitly named memory adapter above.
internal static class MuiListviewHorizontalScrollerDragStateCodec
{
	// Declaration-order horizontal drag record: magic, signed grab offset,
	// starting scroll, signed pointer, and flags.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListviewHorizontalScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListviewHorizontalScrollerDragState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var grabOffset) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.StartScroll) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var lastPointer) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags)) return false;
		value.GrabOffset = unchecked((int)grabOffset);
		value.LastPointer = unchecked((int)lastPointer);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewHorizontalScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListviewHorizontalScrollerDragState.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.GrabOffset)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.StartScroll) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.LastPointer)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListviewHorizontalScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewHorizontalScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadRecord(ref platform, address, out value) &&
		value.Magic == MuiListviewHorizontalScrollerDragState.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewHorizontalScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory =>
		WriteRecord(ref platform, address, value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewHorizontalScrollerDragStateField field,
		uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewHorizontalScrollerDragStateMemoryCodec.TryWrite(ref platform,
			address, field, value);
}

// Compatibility wrapper retained for callers that still construct the typed
// horizontal drag cursor. Live drag serialization uses the direct adapter.
internal static class MuiListviewHorizontalScrollerDragStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListviewHorizontalScrollerDragStateFieldCursor cursor,
		out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewHorizontalScrollerDragStateMemoryCodec.TryGetAddress(
			ref platform, cursor.Address, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewHorizontalScrollerDragStateField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewHorizontalScrollerDragStateMemoryCodec.TryReadUInt32(
			ref platform, address, field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewHorizontalScrollerDragStateField field,
		uint value) where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewHorizontalScrollerDragStateMemoryCodec.TryWriteUInt32(
			ref platform, address, field, value);
}

// Guest-resident state for one Listview thumb drag.  The list itself remains
// authoritative: this record carries only the pointer grab offset and the
// previous first-row value needed to complete a bounded gesture.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewScrollerDragState
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x4C535344u; // 'LSSD'
	internal const uint ActiveFlag = 1;
	internal const uint CapturedFlag = 2;
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint GrabOffsetOffset = 4;
	internal const uint StartFirstOffset = 8;
	internal const uint LastPointerOffset = 12;
	internal const uint FlagsOffset = 16;

	internal uint Magic;
	internal int GrabOffset;
	internal int StartFirst;
	internal int LastPointer;
	internal uint Flags;
}

internal enum MuiListviewScrollerDragStateField : byte
{
	Magic,
	GrabOffset,
	StartFirst,
	LastPointer,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewScrollerDragStateFieldCursor
{
	internal APTR Address;
	internal MuiListviewScrollerDragStateField Field;
}

internal static class MuiListviewScrollerDragStateMemoryCodec
{
	private static bool TryResolve(MuiListviewScrollerDragStateField field,
		out uint offset, out uint recordSize)
	{
		recordSize = MuiListviewScrollerDragState.Size;
		if (field == MuiListviewScrollerDragStateField.Magic)
			offset = MuiListviewScrollerDragState.MagicOffset;
		else if (field == MuiListviewScrollerDragStateField.GrabOffset)
			offset = MuiListviewScrollerDragState.GrabOffsetOffset;
		else if (field == MuiListviewScrollerDragStateField.StartFirst)
			offset = MuiListviewScrollerDragState.StartFirstOffset;
		else if (field == MuiListviewScrollerDragStateField.LastPointer)
			offset = MuiListviewScrollerDragState.LastPointerOffset;
		else if (field == MuiListviewScrollerDragStateField.Flags)
			offset = MuiListviewScrollerDragState.FlagsOffset;
		else
		{
			offset = 0;
			recordSize = 0;
			return false;
		}
		return true;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewScrollerDragStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(field, out var offset, out var recordSize) ||
			record.IsNull || record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(record, recordSize)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return platform.IsMapped(address,
			MuiListviewScrollerDragState.FieldSize);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR record,
		MuiListviewScrollerDragStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR record,
		MuiListviewScrollerDragStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

// Compatibility wrapper retained for callers that still construct the typed
// Listview vertical drag cursor. Live drag serialization uses the direct
// record adapter.
internal static class MuiListviewScrollerDragStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListviewScrollerDragStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewScrollerDragStateMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Field, out address);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewScrollerDragStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewScrollerDragStateMemoryCodec.TryRead(ref platform, address,
			field, out value);

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewScrollerDragStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewScrollerDragStateMemoryCodec.TryWrite(ref platform, address,
			field, value);
}

internal static class MuiListviewScrollerDragStateCodec
{
	// Declaration-order vertical drag record: magic, signed grab/start/last
	// values, and flags. Keep this transient state in the named record.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListviewScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListviewScrollerDragState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var grabOffset) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var startFirst) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var lastPointer) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags)) return false;
		value.GrabOffset = unchecked((int)grabOffset);
		value.StartFirst = unchecked((int)startFirst);
		value.LastPointer = unchecked((int)lastPointer);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListviewScrollerDragState.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor, value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.GrabOffset)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.StartFirst)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			unchecked((uint)value.LastPointer)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiListviewScrollerDragState.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewScrollerDragState.Size)) return false;
		return WriteRecord(ref platform, address, value);
	}
}
