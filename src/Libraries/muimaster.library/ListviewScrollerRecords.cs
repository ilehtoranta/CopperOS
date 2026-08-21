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

internal static class MuiListviewHorizontalScrollerFieldCursorCodec
{
	private static bool TryResolve(
		MuiListviewHorizontalScrollerField field, out uint offset)
	{
		offset = field switch
		{
			MuiListviewHorizontalScrollerField.Magic => 0,
			MuiListviewHorizontalScrollerField.TrackLeft => 4,
			MuiListviewHorizontalScrollerField.TrackTop => 8,
			MuiListviewHorizontalScrollerField.TrackRight => 12,
			MuiListviewHorizontalScrollerField.TrackBottom => 16,
			MuiListviewHorizontalScrollerField.ThumbLeft => 20,
			MuiListviewHorizontalScrollerField.ThumbTop => 24,
			MuiListviewHorizontalScrollerField.ThumbRight => 28,
			MuiListviewHorizontalScrollerField.ThumbBottom => 32,
			MuiListviewHorizontalScrollerField.ContentWidth => 36,
			MuiListviewHorizontalScrollerField.ViewWidth => 40,
			MuiListviewHorizontalScrollerField.ScrollX => 44,
			MuiListviewHorizontalScrollerField.MaxScrollX => 48,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListviewHorizontalScrollerFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record,
				MuiListviewHorizontalScrollerState.Size)) return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiListviewHorizontalScrollerFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewHorizontalScrollerField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListviewHorizontalScrollerFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
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

internal static class MuiListviewHorizontalScrollerStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewHorizontalScrollerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewHorizontalScrollerState.Size) ||
			!MuiListviewHorizontalScrollerFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.Magic, out var magic) ||
			magic != MuiListviewHorizontalScrollerState.Cookie)
			return false;
		value.Magic = magic;
		return MuiListviewHorizontalScrollerFieldCursorCodec.TryReadInt32(
			ref platform, address,
			MuiListviewHorizontalScrollerField.TrackLeft, out value.TrackLeft) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.TrackTop, out value.TrackTop) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.TrackRight,
				out value.TrackRight) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.TrackBottom,
				out value.TrackBottom) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ThumbLeft, out value.ThumbLeft) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ThumbTop, out value.ThumbTop) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ThumbRight,
				out value.ThumbRight) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ThumbBottom,
				out value.ThumbBottom) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ContentWidth,
				out value.ContentWidth) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ViewWidth,
				out value.ViewWidth) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ScrollX,
				out value.ScrollX) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryReadUInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.MaxScrollX,
				out value.MaxScrollX);
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewHorizontalScrollerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewHorizontalScrollerState.Size) || value.Magic !=
			MuiListviewHorizontalScrollerState.Cookie) return false;
		return MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteUInt32(
			ref platform, address,
			MuiListviewHorizontalScrollerField.Magic, value.Magic) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.TrackLeft, value.TrackLeft) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.TrackTop, value.TrackTop) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.TrackRight,
				value.TrackRight) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.TrackBottom,
				value.TrackBottom) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ThumbLeft, value.ThumbLeft) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ThumbTop, value.ThumbTop) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ThumbRight,
				value.ThumbRight) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ThumbBottom,
				value.ThumbBottom) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ContentWidth,
				value.ContentWidth) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ViewWidth,
				value.ViewWidth) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.ScrollX, value.ScrollX) &&
			MuiListviewHorizontalScrollerFieldCursorCodec.TryWriteUInt32(
				ref platform, address,
				MuiListviewHorizontalScrollerField.MaxScrollX,
				value.MaxScrollX);
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

internal static class MuiListviewHorizontalScrollerDragStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewHorizontalScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewHorizontalScrollerDragState.Size)) return false;
		if (!TryReadUInt32(ref platform, address,
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
		return value.Magic == MuiListviewHorizontalScrollerDragState.Cookie;
	}

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

	private static bool TryResolve(
		MuiListviewHorizontalScrollerDragStateField field, out uint offset)
	{
		offset = field switch
		{
			MuiListviewHorizontalScrollerDragStateField.Magic => 0,
			MuiListviewHorizontalScrollerDragStateField.GrabOffset => 4,
			MuiListviewHorizontalScrollerDragStateField.StartScroll => 8,
			MuiListviewHorizontalScrollerDragStateField.LastPointer => 12,
			MuiListviewHorizontalScrollerDragStateField.Flags => 16,
			_ => uint.MaxValue,
		};
		return offset != uint.MaxValue;
	}

	private static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListviewHorizontalScrollerDragStateFieldCursor cursor,
		out APTR fieldAddress)
		where TPlatform : struct, IMuiGuestMemory
	{
		fieldAddress = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address,
				MuiListviewHorizontalScrollerDragState.Size))
			return false;
		fieldAddress = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(fieldAddress, 4);
	}

	private static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewHorizontalScrollerDragStateField field,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(
			MuiListviewHorizontalScrollerDragStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out var fieldAddress) &&
			ReadUInt32(ref platform, fieldAddress, out value);
	}

	private static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewHorizontalScrollerDragStateField field,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(
			MuiListviewHorizontalScrollerDragStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out var fieldAddress) &&
			WriteUInt32(ref platform, fieldAddress, value);
	}

	private static bool ReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (address.IsNull || !platform.IsMapped(address, 4)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	private static bool WriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address, 4)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
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

internal static class MuiListviewScrollerDragStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListviewScrollerDragStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		uint offset;
		switch (cursor.Field)
		{
			case MuiListviewScrollerDragStateField.Magic: offset = 0; break;
			case MuiListviewScrollerDragStateField.GrabOffset: offset = 4; break;
			case MuiListviewScrollerDragStateField.StartFirst: offset = 8; break;
			case MuiListviewScrollerDragStateField.LastPointer: offset = 12; break;
			case MuiListviewScrollerDragStateField.Flags: offset = 16; break;
			default: return false;
		}
		if (cursor.Address.IsNull || cursor.Address.Raw >
			uint.MaxValue - offset) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewScrollerDragStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiListviewScrollerDragStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out var slot) &&
			Read(ref platform, slot, out value);
	}

	internal static bool TryWrite<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewScrollerDragStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListviewScrollerDragStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		return TryGetAddress(ref platform, cursor, out var slot) &&
			Write(ref platform, slot, value);
	}

	private static bool Read<TPlatform>(ref TPlatform platform, APTR address,
		out uint value) where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (address.IsNull || !platform.IsMapped(address, 4)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	private static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		uint value) where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address, 4)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiListviewScrollerDragStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewScrollerDragState.Size) ||
			!MuiListviewScrollerDragStateFieldCursorCodec.TryRead(ref platform,
				address, MuiListviewScrollerDragStateField.Magic, out value.Magic) ||
			!MuiListviewScrollerDragStateFieldCursorCodec.TryRead(ref platform,
				address, MuiListviewScrollerDragStateField.GrabOffset,
				out var grabOffset) ||
			!MuiListviewScrollerDragStateFieldCursorCodec.TryRead(ref platform,
				address, MuiListviewScrollerDragStateField.StartFirst,
				out var startFirst) ||
			!MuiListviewScrollerDragStateFieldCursorCodec.TryRead(ref platform,
				address, MuiListviewScrollerDragStateField.LastPointer,
				out var lastPointer) ||
			!MuiListviewScrollerDragStateFieldCursorCodec.TryRead(ref platform,
				address, MuiListviewScrollerDragStateField.Flags, out value.Flags))
			return false;
		value.GrabOffset = unchecked((int)grabOffset);
		value.StartFirst = unchecked((int)startFirst);
		value.LastPointer = unchecked((int)lastPointer);
		return value.Magic == MuiListviewScrollerDragState.Cookie;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewScrollerDragState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewScrollerDragState.Size)) return false;
		return MuiListviewScrollerDragStateFieldCursorCodec.TryWrite(ref platform,
			address, MuiListviewScrollerDragStateField.Magic, value.Magic) &&
			MuiListviewScrollerDragStateFieldCursorCodec.TryWrite(ref platform,
				address, MuiListviewScrollerDragStateField.GrabOffset,
				unchecked((uint)value.GrabOffset)) &&
			MuiListviewScrollerDragStateFieldCursorCodec.TryWrite(ref platform,
				address, MuiListviewScrollerDragStateField.StartFirst,
				unchecked((uint)value.StartFirst)) &&
			MuiListviewScrollerDragStateFieldCursorCodec.TryWrite(ref platform,
				address, MuiListviewScrollerDragStateField.LastPointer,
				unchecked((uint)value.LastPointer)) &&
			MuiListviewScrollerDragStateFieldCursorCodec.TryWrite(ref platform,
				address, MuiListviewScrollerDragStateField.Flags, value.Flags);
	}
}
