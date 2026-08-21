/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Class identity for the MG08 collection classes. Determined from the
// registered class name rather than from any private MorphOS vector, so no
// MorphOS compatibility is advertised. Listtree and the remaining scrolling
// companions are still deferred; Dirlist/Volumelist use this backbone and
// Stringscroll is implemented as a separate leaf collection.
public enum MuiCollectionClass
{
	Unknown = 0,
	List,
	Listview,
	Floattext,
	Dirlist,
	Volumelist,
	Stringscroll,
}

// Fixed guest-owned List header. The slot array, capacity/count metadata, and
// image-chain head travel together so List operations consume named fields
// rather than repeating private header offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListHeaderState
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x4C495354u; // 'LIST'

	internal uint Magic;
	internal APTR Index;
	internal uint Capacity;
	internal uint Count;
	internal APTR Images;
}

internal enum MuiListHeaderField : byte
{
	Magic,
	Index,
	Capacity,
	Count,
	Images,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListHeaderFieldCursor
{
	internal APTR Address;
	internal MuiListHeaderField Field;
}

internal static class MuiListHeaderFieldCursorCodec
{
	private static bool TryResolve(MuiListHeaderField field, out uint offset)
	{
		switch (field)
		{
			case MuiListHeaderField.Magic:
				offset = 0;
				return true;
			case MuiListHeaderField.Index:
				offset = 4;
				return true;
			case MuiListHeaderField.Capacity:
				offset = 8;
				return true;
			case MuiListHeaderField.Count:
				offset = 12;
				return true;
			case MuiListHeaderField.Images:
				offset = 16;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListHeaderFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiListHeaderState.Size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHeaderField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiListHeaderFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHeaderField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListHeaderFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiListHeaderCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListHeaderState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiListHeaderState.Size) ||
			!MuiListHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiListHeaderField.Magic, out var magic) ||
			magic != MuiListHeaderState.Cookie)
			return false;
		value.Magic = MuiListHeaderState.Cookie;
		if (!MuiListHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiListHeaderField.Index, out var index) ||
			!MuiListHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiListHeaderField.Capacity, out value.Capacity) ||
			!MuiListHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiListHeaderField.Count, out value.Count) ||
			!MuiListHeaderFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiListHeaderField.Images, out var images)) return false;
		value.Index = APTR.FromPointer(index);
		value.Images = APTR.FromPointer(images);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListHeaderState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListHeaderState.Size) || value.Magic != MuiListHeaderState.Cookie)
			return false;
		return MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiListHeaderField.Magic, value.Magic) &&
			MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiListHeaderField.Index, value.Index.Raw) &&
			MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiListHeaderField.Capacity, value.Capacity) &&
			MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiListHeaderField.Count, value.Count) &&
			MuiListHeaderFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiListHeaderField.Images, value.Images.Raw);
	}
}

// MUIA_List_HScrollerVisibility is an undocumented MorphOS policy attribute,
// but its value is still part of the public List ABI. Keep the policy together
// with the derived viewport decision in one guest-resident record so later
// horizontal-scroller composition can consume named state rather than another
// private word convention.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListHScrollerState
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x48435352u; // 'HCSR'

	internal uint Magic;
	internal uint Policy;
	internal uint ContentWidth;
	internal uint ViewWidth;
	internal uint Visible;
	internal uint ScrollX;
	internal uint MaxScrollX;
}

internal enum MuiListHScrollerStateField : byte
{
	Magic,
	Policy,
	ContentWidth,
	ViewWidth,
	Visible,
	ScrollX,
	MaxScrollX,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListHScrollerStateFieldCursor
{
	internal APTR Address;
	internal MuiListHScrollerStateField Field;
}

internal static class MuiListHScrollerStateFieldCursorCodec
{
	private static bool TryResolve(MuiListHScrollerStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiListHScrollerStateField.Magic:
				offset = 0;
				return true;
			case MuiListHScrollerStateField.Policy:
				offset = 4;
				return true;
			case MuiListHScrollerStateField.ContentWidth:
				offset = 8;
				return true;
			case MuiListHScrollerStateField.ViewWidth:
				offset = 12;
				return true;
			case MuiListHScrollerStateField.Visible:
				offset = 16;
				return true;
			case MuiListHScrollerStateField.ScrollX:
				offset = 20;
				return true;
			case MuiListHScrollerStateField.MaxScrollX:
				offset = 24;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListHScrollerStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiListHScrollerState.Size))
			return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHScrollerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiListHScrollerStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHScrollerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListHScrollerStateFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiListHScrollerStateCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListHScrollerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiListHScrollerState.Size) ||
			!MuiListHScrollerStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListHScrollerStateField.Magic, out var magic) ||
			magic != MuiListHScrollerState.Cookie)
			return false;
		value.Magic = magic;
		if (!MuiListHScrollerStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiListHScrollerStateField.Policy, out value.Policy) ||
			!MuiListHScrollerStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListHScrollerStateField.ContentWidth,
				out value.ContentWidth) ||
			!MuiListHScrollerStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListHScrollerStateField.ViewWidth,
				out value.ViewWidth) ||
			!MuiListHScrollerStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListHScrollerStateField.Visible, out value.Visible) ||
			!MuiListHScrollerStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListHScrollerStateField.ScrollX, out value.ScrollX) ||
			!MuiListHScrollerStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListHScrollerStateField.MaxScrollX,
				out value.MaxScrollX))
			return false;
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListHScrollerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListHScrollerState.Size) || value.Magic !=
			MuiListHScrollerState.Cookie) return false;
		return MuiListHScrollerStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiListHScrollerStateField.Magic, value.Magic) &&
			MuiListHScrollerStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListHScrollerStateField.Policy, value.Policy) &&
			MuiListHScrollerStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListHScrollerStateField.ContentWidth,
				value.ContentWidth) &&
			MuiListHScrollerStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListHScrollerStateField.ViewWidth,
				value.ViewWidth) &&
			MuiListHScrollerStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListHScrollerStateField.Visible, value.Visible) &&
			MuiListHScrollerStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListHScrollerStateField.ScrollX, value.ScrollX) &&
			MuiListHScrollerStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListHScrollerStateField.MaxScrollX,
				value.MaxScrollX);
	}
}

// One guest-resident entry in the contiguous List index. The surrounding
// array is an explicit ABI boundary; each fixed-size element is decoded as a
// named record so consumers never repeat its member offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListSlotState
{
	internal const uint Size = 8;

	internal APTR Entry;
	internal uint Flags;
}

internal enum MuiListSlotField : byte
{
	Entry,
	Flags,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListSlotFieldCursor
{
	internal APTR Address;
	internal MuiListSlotField Field;
}

internal static class MuiListSlotFieldCursorCodec
{
	private static bool TryResolve(MuiListSlotField field, out uint offset)
	{
		switch (field)
		{
			case MuiListSlotField.Entry:
				offset = 0;
				return true;
			case MuiListSlotField.Flags:
				offset = 4;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListSlotFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiListSlotState.Size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListSlotField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiListSlotFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListSlotField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListSlotFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiListSlotCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListSlotState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiListSlotState.Size)) return false;
		if (!MuiListSlotFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiListSlotField.Entry, out var entry) ||
			!MuiListSlotFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiListSlotField.Flags, out value.Flags)) return false;
		value.Entry = APTR.FromPointer(entry);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListSlotState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListSlotState.Size)) return false;
		return MuiListSlotFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiListSlotField.Entry, value.Entry.Raw) &&
			MuiListSlotFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiListSlotField.Flags, value.Flags);
	}
}

// Opaque guest handle returned by MUIM_List_CreateImage. The caller-owned
// image object is not retained as a host object; the chain is purely guest
// state and is bounded by the List core.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListImageState
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x4C494D47u; // 'LIMG'

	internal uint Magic;
	internal APTR ImageObject;
	internal uint Flags;
	internal APTR Next;
}

internal enum MuiListImageField : byte
{
	Magic,
	ImageObject,
	Flags,
	Next,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListImageFieldCursor
{
	internal APTR Address;
	internal MuiListImageField Field;
}

internal static class MuiListImageFieldCursorCodec
{
	private static bool TryResolve(MuiListImageField field, out uint offset)
	{
		switch (field)
		{
			case MuiListImageField.Magic:
				offset = 0;
				return true;
			case MuiListImageField.ImageObject:
				offset = 4;
				return true;
			case MuiListImageField.Flags:
				offset = 8;
				return true;
			case MuiListImageField.Next:
				offset = 12;
				return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListImageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
			cursor.Address.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Address, MuiListImageState.Size)) return false;
		address = APTR.FromPointer(cursor.Address.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListImageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiListImageFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		value = platform.ReadUInt32(fieldAddress, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListImageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListImageFieldCursor);
		cursor.Address = address;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
		platform.WriteUInt32(fieldAddress, 0, value);
		return true;
	}
}

internal static class MuiListImageCodec
{
	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListImageState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiListImageState.Size) ||
			!MuiListImageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiListImageField.Magic, out var magic) ||
			magic != MuiListImageState.Cookie)
			return false;
		value.Magic = MuiListImageState.Cookie;
		if (!MuiListImageFieldCursorCodec.TryReadUInt32(ref platform, address,
			MuiListImageField.ImageObject, out var imageObject) ||
			!MuiListImageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiListImageField.Flags, out value.Flags) ||
			!MuiListImageFieldCursorCodec.TryReadUInt32(ref platform, address,
				MuiListImageField.Next, out var next)) return false;
		value.ImageObject = APTR.FromPointer(imageObject);
		value.Next = APTR.FromPointer(next);
		return true;
	}

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListImageState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListImageState.Size) || value.Magic != MuiListImageState.Cookie)
			return false;
		return MuiListImageFieldCursorCodec.TryWriteUInt32(ref platform, address,
			MuiListImageField.Magic, value.Magic) &&
			MuiListImageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiListImageField.ImageObject, value.ImageObject.Raw) &&
			MuiListImageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiListImageField.Flags, value.Flags) &&
			MuiListImageFieldCursorCodec.TryWriteUInt32(ref platform, address,
				MuiListImageField.Next, value.Next.Raw);
	}
}

// A native-safe, guest-resident List backbone. Entries live behind a single
// contiguous APTR index owned in guest memory, giving bounded O(1)
// MUIM_List_GetEntry while insertion/removal stay O(n) shifts. No managed
// allocations, arrays, collections, delegates, LINQ, or exceptions are used;
// every mutation is expressed through the guest-memory platform seam. Ownership
// is failure-atomic: a construct hook that cannot be honoured rolls back the
// slot it was reserving, and object disposal destructs every surviving entry
// before the index and header blocks are released.
public static class MuiListCore
{
	// The List index is a contiguous guest table of named entry/flag records.
	// Keep traversal state explicit so callers do not reconstruct private slot
	// offsets and so malformed indices fail before any guest access.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListSlotCursor
	{
		internal const uint EntrySize = MuiListSlotState.Size;
		internal const uint MaximumEntries = 0x00100000u;
		internal APTR Base;
		internal uint Index;
	}

	internal static class MuiListSlotCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListSlotCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Base.IsNull || cursor.Index >=
				MuiListSlotCursor.MaximumEntries || cursor.Index >
				(uint.MaxValue - cursor.Base.Raw) /
				MuiListSlotCursor.EntrySize) return false;
			var offset = cursor.Index * MuiListSlotCursor.EntrySize;
			if (cursor.Base.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(cursor.Base.Raw + offset);
			return platform.IsMapped(address, MuiListSlotCursor.EntrySize);
		}
	}

	// Caller-supplied entry vectors use the same four-byte pointer-slot record
	// as StringArray tables, but their public List bound is the larger entry
	// limit. Keep that distinction explicit instead of weakening the bounded
	// StringArray cursor or rebuilding vector offsets at each call site.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListPointerVectorCursor
	{
		internal const uint EntrySize = MuiListPointerSlotRecord.Size;
		internal const uint MaximumEntries = 0x00100000u;
		internal APTR Base;
		internal uint Index;
	}

	internal static class MuiListPointerVectorCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListPointerVectorCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Base.IsNull || cursor.Index >=
				MuiListPointerVectorCursor.MaximumEntries || cursor.Index >
				(uint.MaxValue - cursor.Base.Raw) /
				MuiListPointerVectorCursor.EntrySize) return false;
			var offset = cursor.Index * MuiListPointerVectorCursor.EntrySize;
			if (cursor.Base.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(cursor.Base.Raw + offset);
			return platform.IsMapped(address,
				MuiListPointerVectorCursor.EntrySize);
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListEditState
	{
		internal const uint Size = 24;
		internal uint Magic;
		internal int Row;
		internal int Column;
		internal APTR Entry;
		internal APTR EditObject;
		internal uint Flags;
	}

	internal enum MuiListEditField : byte
	{
		Magic,
		Row,
		Column,
		Entry,
		EditObject,
		Flags,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListEditFieldCursor
	{
		internal APTR Address;
		internal MuiListEditField Field;
	}

	internal static class MuiListEditFieldCursorCodec
	{
		private static bool TryResolve(MuiListEditField field, out uint offset)
		{
			switch (field)
			{
				case MuiListEditField.Magic:
					offset = 0;
					return true;
				case MuiListEditField.Row:
					offset = 4;
					return true;
				case MuiListEditField.Column:
					offset = 8;
					return true;
				case MuiListEditField.Entry:
					offset = 12;
					return true;
				case MuiListEditField.EditObject:
					offset = 16;
					return true;
				case MuiListEditField.Flags:
					offset = 20;
					return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListEditFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
				cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address, MuiListEditState.Size)) return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListEditField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListEditFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListEditField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListEditFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListEditStateCodec
	{
		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListEditState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListEditState.Size) || value.Magic != EditStateCookie)
				return false;
			return MuiListEditFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListEditField.Magic, value.Magic) &&
				MuiListEditFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListEditField.Row, unchecked((uint)value.Row)) &&
				MuiListEditFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListEditField.Column, unchecked((uint)value.Column)) &&
				MuiListEditFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListEditField.Entry, value.Entry.Raw) &&
				MuiListEditFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListEditField.EditObject, value.EditObject.Raw) &&
				MuiListEditFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListEditField.Flags, value.Flags);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListEditState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (block.IsNull || !platform.IsMapped(block,
				MuiListEditState.Size) ||
				!MuiListEditFieldCursorCodec.TryReadUInt32(ref platform, block,
					MuiListEditField.Magic, out var magic) || magic != EditStateCookie)
				return false;
			value.Magic = EditStateCookie;
			if (!MuiListEditFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListEditField.Row, out var row) ||
				!MuiListEditFieldCursorCodec.TryReadUInt32(ref platform, block,
					MuiListEditField.Column, out var column) ||
				!MuiListEditFieldCursorCodec.TryReadUInt32(ref platform, block,
					MuiListEditField.Entry, out var entry) ||
				!MuiListEditFieldCursorCodec.TryReadUInt32(ref platform, block,
					MuiListEditField.EditObject, out var editObject) ||
				!MuiListEditFieldCursorCodec.TryReadUInt32(ref platform, block,
					MuiListEditField.Flags, out value.Flags)) return false;
			value.Row = unchecked((int)row);
			value.Column = unchecked((int)column);
			value.Entry = APTR.FromPointer(entry);
			value.EditObject = APTR.FromPointer(editObject);
			return true;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnGeometry
	{
		public const uint Size = 8;
		public uint Offset;
		public uint Width;
	}

	internal enum MuiListColumnGeometryField : byte
	{
		Offset,
		Width,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnGeometryFieldCursor
	{
		internal APTR Address;
		internal MuiListColumnGeometryField Field;
	}

	internal static class MuiListColumnGeometryFieldCursorCodec
	{
		private static bool TryResolve(MuiListColumnGeometryField field,
			out uint offset)
		{
			switch (field)
			{
				case MuiListColumnGeometryField.Offset:
					offset = 0;
					return true;
				case MuiListColumnGeometryField.Width:
					offset = 4;
					return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnGeometryFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
				cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address, MuiListColumnGeometry.Size)) return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnGeometryField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListColumnGeometryFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnGeometryField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListColumnGeometryFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListColumnGeometryCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListColumnGeometry value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListColumnGeometry.Size)) return false;
			if (!MuiListColumnGeometryFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListColumnGeometryField.Offset, out value.Offset) ||
				!MuiListColumnGeometryFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListColumnGeometryField.Width, out value.Width))
				return false;
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnGeometry value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListColumnGeometry.Size)) return false;
			return MuiListColumnGeometryFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListColumnGeometryField.Offset, value.Offset) &&
				MuiListColumnGeometryFieldCursorCodec.TryWriteUInt32(ref platform,
					address, MuiListColumnGeometryField.Width, value.Width);
		}
	}

	// Layout publishes a bounded table of {offset,width} records. Keep the
	// geometry index as a named cursor so both the public projection and the
	// cached layout reader share one overflow-checked guest boundary.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnGeometryCursor
	{
		internal const uint EntrySize = MuiListColumnGeometry.Size;
		internal const uint MaximumEntries = MaximumColumns;
		internal APTR Base;
		internal uint Index;
	}

	internal static class MuiListColumnGeometryCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListColumnGeometryCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Base.IsNull || cursor.Index >=
				MuiListColumnGeometryCursor.MaximumEntries || cursor.Index >
				(uint.MaxValue - cursor.Base.Raw) /
				MuiListColumnGeometryCursor.EntrySize) return false;
			var offset = cursor.Index * MuiListColumnGeometryCursor.EntrySize;
			if (cursor.Base.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(cursor.Base.Raw + offset);
			return platform.IsMapped(address,
				MuiListColumnGeometryCursor.EntrySize);
		}
	}

	// Column hiding is derived from the current rectangle and FORMAT minimums.
	// Eight named masks cover the bounded 256-column geometry without a managed
	// array or a dependence on private descriptor offsets.
	private struct MuiListHiddenColumns
	{
		public uint Low;
		public uint High;
		public uint Word2;
		public uint Word3;
		public uint Word4;
		public uint Word5;
		public uint Word6;
		public uint Word7;
	}

	internal enum MuiListStateRecordKind : byte
	{
		TitleArray,
		TitleValue,
		SelectionSignal,
		FormatPolicy,
		FontPolicy,
		Redraw,
		ActiveCursor,
		ColumnVisibility,
		ColumnOrder,
		Viewport,
		InteractionPolicy,
		ClickState,
		HookPolicy,
		SortState,
		PresentationPolicy,
	}

	internal enum MuiListStateField : byte
	{
		Magic,
		Pointers,
		Count,
		TitleValue,
		SelectionValue,
		FormatValue,
		MaxColumnsValue,
		FormatColumnsValue,
		FontValue,
		Dirty,
		Requests,
		HasActive,
		Active,
		Low,
		High,
		Word2,
		Word3,
		Word4,
		Word5,
		Word6,
		Word7,
		Values,
		Reserved,
		TopPixel,
		VisiblePixel,
		TotalPixel,
		First,
		LineHeight,
		Visible,
		DropMark,
		Input,
		MultiSelect,
		ScrollerPos,
		ClickColumn,
		DoubleClick,
		AgainClick,
		Clicks,
		DefClickColumn,
		ConstructHook,
		DestructHook,
		DisplayHook,
		CompareHook,
		MultiTestHook,
		SortColumn,
		TitleClick,
		Editable,
		Quiet,
		AdjustHeight,
		AdjustWidth,
		Stripes,
		ShowDropMarks,
		DragSortable,
		DragType,
		AutoVisible,
		AutoLineHeight,
		MinLineHeight,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListStateFieldCursor
	{
		internal APTR Address;
		internal MuiListStateRecordKind Record;
		internal MuiListStateField Field;
	}

	internal static class MuiListStateFieldCursorCodec
	{
		private static bool TryResolve(MuiListStateRecordKind record,
			MuiListStateField field, out uint offset, out uint size)
		{
			offset = 0;
			size = 0;
			switch (record)
			{
				case MuiListStateRecordKind.TitleArray:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 12;
							return true;
						case MuiListStateField.Pointers:
							offset = 4;
							size = 12;
							return true;
						case MuiListStateField.Count:
							offset = 8;
							size = 12;
							return true;
					}
					break;
				case MuiListStateRecordKind.TitleValue:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 8;
							return true;
						case MuiListStateField.TitleValue:
							offset = 4;
							size = 8;
							return true;
					}
					break;
				case MuiListStateRecordKind.SelectionSignal:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 8;
							return true;
						case MuiListStateField.SelectionValue:
							offset = 4;
							size = 8;
							return true;
					}
					break;
				case MuiListStateRecordKind.FormatPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 16;
							return true;
						case MuiListStateField.FormatValue:
							offset = 4;
							size = 16;
							return true;
						case MuiListStateField.MaxColumnsValue:
							offset = 8;
							size = 16;
							return true;
						case MuiListStateField.FormatColumnsValue:
							offset = 12;
							size = 16;
							return true;
					}
					break;
				case MuiListStateRecordKind.FontPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 8;
							return true;
						case MuiListStateField.FontValue:
							offset = 4;
							size = 8;
							return true;
					}
					break;
				case MuiListStateRecordKind.Redraw:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 12;
							return true;
						case MuiListStateField.Dirty:
							offset = 4;
							size = 12;
							return true;
						case MuiListStateField.Requests:
							offset = 8;
							size = 12;
							return true;
					}
					break;
				case MuiListStateRecordKind.ActiveCursor:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 12;
							return true;
						case MuiListStateField.HasActive:
							offset = 4;
							size = 12;
							return true;
						case MuiListStateField.Active:
							offset = 8;
							size = 12;
							return true;
					}
					break;
				case MuiListStateRecordKind.ColumnVisibility:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 36;
							return true;
						case MuiListStateField.Low:
							offset = 4;
							size = 36;
							return true;
						case MuiListStateField.High:
							offset = 8;
							size = 36;
							return true;
						case MuiListStateField.Word2:
							offset = 12;
							size = 36;
							return true;
						case MuiListStateField.Word3:
							offset = 16;
							size = 36;
							return true;
						case MuiListStateField.Word4:
							offset = 20;
							size = 36;
							return true;
						case MuiListStateField.Word5:
							offset = 24;
							size = 36;
							return true;
						case MuiListStateField.Word6:
							offset = 28;
							size = 36;
							return true;
						case MuiListStateField.Word7:
							offset = 32;
							size = 36;
							return true;
					}
					break;
				case MuiListStateRecordKind.ColumnOrder:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 16;
							return true;
						case MuiListStateField.Count:
							offset = 4;
							size = 16;
							return true;
						case MuiListStateField.Values:
							offset = 8;
							size = 16;
							return true;
						case MuiListStateField.Reserved:
							offset = 12;
							size = 16;
							return true;
					}
					break;
				case MuiListStateRecordKind.Viewport:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 32;
							return true;
						case MuiListStateField.TopPixel:
							offset = 4;
							size = 32;
							return true;
						case MuiListStateField.VisiblePixel:
							offset = 8;
							size = 32;
							return true;
						case MuiListStateField.TotalPixel:
							offset = 12;
							size = 32;
							return true;
						case MuiListStateField.First:
							offset = 16;
							size = 32;
							return true;
						case MuiListStateField.LineHeight:
							offset = 20;
							size = 32;
							return true;
						case MuiListStateField.Visible:
							offset = 24;
							size = 32;
							return true;
						case MuiListStateField.DropMark:
							offset = 28;
							size = 32;
							return true;
					}
					break;
				case MuiListStateRecordKind.InteractionPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 16;
							return true;
						case MuiListStateField.Input:
							offset = 4;
							size = 16;
							return true;
						case MuiListStateField.MultiSelect:
							offset = 8;
							size = 16;
							return true;
						case MuiListStateField.ScrollerPos:
							offset = 12;
							size = 16;
							return true;
					}
					break;
				case MuiListStateRecordKind.ClickState:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 24;
							return true;
						case MuiListStateField.ClickColumn:
							offset = 4;
							size = 24;
							return true;
						case MuiListStateField.DoubleClick:
							offset = 8;
							size = 24;
							return true;
						case MuiListStateField.AgainClick:
							offset = 12;
							size = 24;
							return true;
						case MuiListStateField.Clicks:
							offset = 16;
							size = 24;
							return true;
						case MuiListStateField.DefClickColumn:
							offset = 20;
							size = 24;
							return true;
					}
					break;
				case MuiListStateRecordKind.HookPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 24;
							return true;
						case MuiListStateField.ConstructHook:
							offset = 4;
							size = 24;
							return true;
						case MuiListStateField.DestructHook:
							offset = 8;
							size = 24;
							return true;
						case MuiListStateField.DisplayHook:
							offset = 12;
							size = 24;
							return true;
						case MuiListStateField.CompareHook:
							offset = 16;
							size = 24;
							return true;
						case MuiListStateField.MultiTestHook:
							offset = 20;
							size = 24;
							return true;
					}
					break;
				case MuiListStateRecordKind.SortState:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 12;
							return true;
						case MuiListStateField.SortColumn:
							offset = 4;
							size = 12;
							return true;
						case MuiListStateField.TitleClick:
							offset = 8;
							size = 12;
							return true;
					}
					break;
				case MuiListStateRecordKind.PresentationPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							offset = 0;
							size = 48;
							return true;
						case MuiListStateField.Editable:
							offset = 4;
							size = 48;
							return true;
						case MuiListStateField.Quiet:
							offset = 8;
							size = 48;
							return true;
						case MuiListStateField.AdjustHeight:
							offset = 12;
							size = 48;
							return true;
						case MuiListStateField.AdjustWidth:
							offset = 16;
							size = 48;
							return true;
						case MuiListStateField.Stripes:
							offset = 20;
							size = 48;
							return true;
						case MuiListStateField.ShowDropMarks:
							offset = 24;
							size = 48;
							return true;
						case MuiListStateField.DragSortable:
							offset = 28;
							size = 48;
							return true;
						case MuiListStateField.DragType:
							offset = 32;
							size = 48;
							return true;
						case MuiListStateField.AutoVisible:
							offset = 36;
							size = 48;
							return true;
						case MuiListStateField.AutoLineHeight:
							offset = 40;
							size = 48;
							return true;
						case MuiListStateField.MinLineHeight:
							offset = 44;
							size = 48;
							return true;
					}
					break;
			}
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListStateFieldCursor cursor, out APTR address)
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
			APTR address, MuiListStateRecordKind record,
			MuiListStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListStateFieldCursor);
			cursor.Address = address;
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListStateRecordKind record,
			MuiListStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListStateFieldCursor);
			cursor.Address = address;
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	// Explicit MUIA_List_HideColumn/ShowColumn state is retained in one
	// guest-resident record.  The fixed eight-word mask matches the bounded
	// 256-column geometry contract and is combined with FORMAT minimum-width
	// hiding at layout time.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	private struct MuiListColumnVisibilityState
	{
		public const uint Size = 36;
		public uint Magic;
		public uint Low;
		public uint High;
		public uint Word2;
		public uint Word3;
		public uint Word4;
		public uint Word5;
		public uint Word6;
		public uint Word7;
	}

	// MUIA_List_ColumnOrder is a caller-facing BYTE* permutation. Keep the
	// copied byte payload behind one named guest record so the List never relies
	// on a caller buffer remaining live and never hides state in descriptor
	// offsets. The payload is ABI bytes by definition; all ownership metadata is
	// typed here.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	private struct MuiListColumnOrderState
	{
		public const uint Size = 16;
		public uint Magic;
		public uint Count;
		public APTR Values;
		public uint Reserved;
	}

	// ColumnOrder is a caller-facing BYTE* permutation. Keep each byte
	// addressable through a bounded cursor so source parsing and guest-owned
	// comparison/lookup never reconstruct a raw byte offset independently.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnOrderByteCursor
	{
		internal const uint EntrySize = 1;
		internal const uint MaximumEntries = MaximumColumns;
		internal APTR Base;
		internal uint Index;
	}

	internal static class MuiListColumnOrderByteCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListColumnOrderByteCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Base.IsNull || cursor.Index >=
				MuiListColumnOrderByteCursor.MaximumEntries || cursor.Index >
				uint.MaxValue - cursor.Base.Raw) return false;
			address = APTR.FromPointer(cursor.Base.Raw + cursor.Index);
			return platform.IsMapped(address,
				MuiListColumnOrderByteCursor.EntrySize);
		}
	}

	// Parsed FORMAT columns are guest-resident records.  Keep the semantic
	// fields named here and confine the 68k big-endian wire layout to the
	// Read/WriteFormatDescriptor codecs below.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFormatDescriptor
	{
		internal const uint Size = 40;
		internal uint Delta;
		internal uint Weight;
		internal uint MinWidth;
		internal uint MaxWidth;
		internal uint Column;
		internal uint Flags;
		internal APTR Preparse;
		internal uint PreparseLength;
		// When ReadArgs-style quoted escapes are decoded, PREPARSE points at a
		// private guest copy. Keep that ownership in the named descriptor record
		// so replacement and disposal never have to infer it from raw offsets.
		internal APTR PreparseStorage;
		internal uint PreparseStorageLength;
	}

	// FORMAT descriptors are a bounded guest table of the named records above.
	// Keep the descriptor index explicit so parsing, validation, and layout
	// lookup all reject malformed columns before deriving a private address.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFormatDescriptorCursor
	{
		internal const uint EntrySize = MuiListFormatDescriptor.Size;
		internal const uint MaximumEntries = 256;
		internal APTR Base;
		internal uint Index;
	}

	internal static class MuiListFormatDescriptorCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListFormatDescriptorCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Base.IsNull || cursor.Index >=
				MuiListFormatDescriptorCursor.MaximumEntries || cursor.Index >
				(uint.MaxValue - cursor.Base.Raw) /
				MuiListFormatDescriptorCursor.EntrySize) return false;
			var offset = cursor.Index *
				MuiListFormatDescriptorCursor.EntrySize;
			if (cursor.Base.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(cursor.Base.Raw + offset);
			return platform.IsMapped(address,
				MuiListFormatDescriptorCursor.EntrySize);
		}
	}

	// A FORMAT value is a ReadArgs item, not a managed string. The source span
	// remains guest-addressed while DecodedLength records the value that would
	// be produced by DOS ReadItem's quoted star escapes.
	private struct MuiListFormatValue
	{
		public int Start;
		public int End;
		public uint DecodedLength;
		public byte Quoted;
	}

	private struct MuiListFormatScanState
	{
		public byte InToken;
		public byte EqualSeen;
		public byte Quoted;
	}

	// Explicit MINWIDTH=-1/MAXWIDTH=-1 values are resolved from measured
	// displayed entries during Layout.  Keep the width array behind one named
	// guest record so the geometry path never needs private managed state.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnMetricsState
	{
		internal const uint Size = 16;
		internal uint Magic;
		internal uint Width;
		internal uint Columns;
		internal APTR Values;
	}

	internal enum MuiListColumnMetricsField : byte
	{
		Magic,
		Width,
		Columns,
		Values,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnMetricsFieldCursor
	{
		internal APTR Address;
		internal MuiListColumnMetricsField Field;
	}

	internal static class MuiListColumnMetricsFieldCursorCodec
	{
		private static bool TryResolve(MuiListColumnMetricsField field,
			out uint offset)
		{
			switch (field)
			{
				case MuiListColumnMetricsField.Magic:
					offset = 0;
					return true;
				case MuiListColumnMetricsField.Width:
					offset = 4;
					return true;
				case MuiListColumnMetricsField.Columns:
					offset = 8;
					return true;
				case MuiListColumnMetricsField.Values:
					offset = 12;
					return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricsFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
				cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address, MuiListColumnMetricsState.Size))
				return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnMetricsField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListColumnMetricsFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnMetricsField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListColumnMetricsFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress)) return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListColumnMetricsStateCodec
	{
		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR block, MuiListColumnMetricsState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListColumnMetricsState.Size) ||
				value.Magic != ColumnMetricsCookie) return false;
			return MuiListColumnMetricsFieldCursorCodec.TryWriteUInt32(ref platform,
				block, MuiListColumnMetricsField.Magic, value.Magic) &&
				MuiListColumnMetricsFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListColumnMetricsField.Width, value.Width) &&
				MuiListColumnMetricsFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListColumnMetricsField.Columns, value.Columns) &&
				MuiListColumnMetricsFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListColumnMetricsField.Values, value.Values.Raw);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnMetricsState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (block.IsNull || !platform.IsMapped(block,
				MuiListColumnMetricsState.Size) ||
				!MuiListColumnMetricsFieldCursorCodec.TryReadUInt32(ref platform, block,
					MuiListColumnMetricsField.Magic, out var magic) ||
				magic != ColumnMetricsCookie) return false;
			value.Magic = ColumnMetricsCookie;
			if (!MuiListColumnMetricsFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListColumnMetricsField.Width, out value.Width) ||
				!MuiListColumnMetricsFieldCursorCodec.TryReadUInt32(ref platform, block,
					MuiListColumnMetricsField.Columns, out value.Columns) ||
				!MuiListColumnMetricsFieldCursorCodec.TryReadUInt32(ref platform, block,
					MuiListColumnMetricsField.Values, out var values)) return false;
			value.Values = APTR.FromPointer(values);
			return value.Columns != 0 &&
				value.Columns <= MaximumGeometryColumns && value.Values.IsNotNull;
		}
	}

	// Each Values entry is a guest ULONG containing the measured width for one
	// derived column. Keep the array element named so metric consumers do not
	// reach into an anonymous four-byte slot.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnMetricValue
	{
		internal const uint Size = 4;
		internal uint Value;
	}

	internal enum MuiListColumnMetricField : byte
	{
		Value,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnMetricFieldCursor
	{
		internal APTR Record;
		internal MuiListColumnMetricField Field;
	}

	internal static class MuiListColumnMetricFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Field != MuiListColumnMetricField.Value ||
				cursor.Record.IsNull || !platform.IsMapped(cursor.Record,
					MuiListColumnMetricValue.Size)) return false;
			address = cursor.Record;
			return true;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnMetricField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListColumnMetricFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnMetricField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListColumnMetricFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListColumnMetricCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListColumnMetricValue value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			return MuiListColumnMetricFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListColumnMetricField.Value, out value.Value);
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnMetricValue value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return MuiListColumnMetricFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListColumnMetricField.Value, value.Value);
		}
	}

	// Measured column widths are a bounded guest ULONG table. Keep its cursor
	// separate from pointer tables so geometry code names the element boundary
	// and rejects an out-of-range column before touching guest memory.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnMetricCursor
	{
		internal const uint EntrySize = MuiListColumnMetricValue.Size;
		internal const uint MaximumEntries = MaximumColumns;
		internal APTR Base;
		internal uint Index;
	}

	internal static class MuiListColumnMetricCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Base.IsNull || cursor.Index >=
				MuiListColumnMetricCursor.MaximumEntries || cursor.Index >
				(uint.MaxValue - cursor.Base.Raw) /
				MuiListColumnMetricCursor.EntrySize) return false;
			var offset = cursor.Index * MuiListColumnMetricCursor.EntrySize;
			if (cursor.Base.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(cursor.Base.Raw + offset);
			return platform.IsMapped(address,
				MuiListColumnMetricCursor.EntrySize);
		}
	}

	// MUIA_List_TitleArray owns a private pointer table, but not the strings it
	// references. Keep the ownership and count in one named guest record so the
	// List core never has to infer them from scattered pointer arithmetic.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	private struct MuiListTitleArrayState
	{
		public const uint Size = 12;
		public uint Magic;
		public APTR Pointers;
		public uint Count;
	}

	// MUIA_List_Title is either a caller-owned STRPTR or TRUE for the
	// display-hook title form. Keep that scalar projection in a named record so
	// title-row geometry and drawing do not repeatedly decode an anonymous word.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListTitleState
	{
		internal const uint Size = 8;
		internal const uint Cookie = 0x4C544954u; // 'LTIT'

		internal uint Magic;
		internal uint Value;
	}

	// MUIA_List_SelectChange is a getter-only edge signal. Keep its toggled
	// value in a named record so List selection mutations and Listview forwarding
	// do not rely on a raw attribute word as their synchronization source.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListSelectionSignalState
	{
		internal const uint Size = 8;
		internal const uint Cookie = 0x4C534947u; // 'LSIG'

		internal uint Magic;
		internal uint Value;
	}

	// FORMAT is caller-owned text; MaxColumns is normalized construction/runtime
	// policy; and Columns is the installed descriptor count. Keep these related
	// projections together without taking ownership of the caller's string.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFormatPolicyState
	{
		internal const uint Size = 16;
		internal const uint Cookie = 0x4C464D54u; // 'LFMT'

		internal uint Magic;
		internal APTR Format;
		internal uint MaxColumns;
		internal uint Columns;
	}

	// MUIA_Font is a caller-owned TextFont pointer inherited by List. Keep the
	// pointer in a named record so display, measurement, and runtime updates
	// share one typed projection without taking ownership of the font object.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFontState
	{
		internal const uint Size = 8;
		internal const uint Cookie = 0x4C464E54u; // 'LFNT'

		internal uint Magic;
		internal APTR Font;
	}

	// TitleArray is an inline guest pointer table. Keep each four-byte slot as a
	// named record so ownership and display-copy paths never read anonymous
	// words outside this codec boundary.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListPointerSlotRecord
	{
		internal const uint Size = 4;
		internal APTR Value;
	}

	internal enum MuiListPointerSlotField : byte
	{
		Value,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListPointerSlotFieldCursor
	{
		internal APTR Record;
		internal MuiListPointerSlotField Field;
	}

	internal static class MuiListPointerSlotFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListPointerSlotFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Field != MuiListPointerSlotField.Value ||
				cursor.Record.IsNull || !platform.IsMapped(cursor.Record,
					MuiListPointerSlotRecord.Size)) return false;
			address = cursor.Record;
			return true;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPointerSlotField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListPointerSlotFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPointerSlotField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListPointerSlotFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListPointerSlotCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListPointerSlotRecord record)
			where TPlatform : struct, IMuiGuestMemory
		{
			record = default;
			if (!MuiListPointerSlotFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListPointerSlotField.Value, out var value)) return false;
			record.Value = APTR.FromPointer(value);
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPointerSlotRecord record)
			where TPlatform : struct, IMuiGuestMemory
		{
			return MuiListPointerSlotFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListPointerSlotField.Value, record.Value.Raw);
		}
	}

	// Internal display buffers include one leading ULONG for the MorphOS display
	// hook row number and one trailing slot for the column terminator.  Expose
	// the logical array separately so callers never need to reason about that
	// storage prefix or its byte count.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	private struct MuiListDisplayArrayStorage
	{
		internal APTR Storage;
		internal APTR Array;
		internal uint ByteSize;
	}

	private static bool TryAllocateDisplayArray<TPlatform>(ref TPlatform platform,
		out MuiListDisplayArrayStorage value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		value.ByteSize = (MaximumDrawColumns + 2) * MuiListPointerSlotRecord.Size;
		value.Storage = MuiHeadlessMemory.Allocate(ref platform, value.ByteSize);
		if (value.Storage.IsNull) return false;
		var cursor = default(MuiListPointerSlotCursor);
		cursor.Base = value.Storage;
		cursor.Index = 1;
		if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
			out value.Array))
		{
			platform.Free(value.Storage, value.ByteSize);
			value = default;
			return false;
		}
		return true;
	}

	private static void ClearDisplayArray<TPlatform>(ref TPlatform platform,
		MuiListDisplayArrayStorage value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (value.Storage.IsNotNull)
			platform.Clear(value.Storage, value.ByteSize);
	}

	private static void FreeDisplayArray<TPlatform>(ref TPlatform platform,
		MuiListDisplayArrayStorage value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (value.Storage.IsNotNull)
			platform.Free(value.Storage, value.ByteSize);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListPointerSlotCursor
	{
		internal const uint EntrySize = MuiListPointerSlotRecord.Size;
		internal const uint MaximumEntries = MaximumColumns + 1;
		internal APTR Base;
		internal uint Index;
	}

	internal static class MuiListPointerSlotCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListPointerSlotCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Base.IsNull || cursor.Index >=
				MuiListPointerSlotCursor.MaximumEntries || cursor.Index >
				(uint.MaxValue - cursor.Base.Raw) /
				MuiListPointerSlotCursor.EntrySize) return false;
			var offset = cursor.Index * MuiListPointerSlotCursor.EntrySize;
			if (cursor.Base.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(cursor.Base.Raw + offset);
			return platform.IsMapped(address,
				MuiListPointerSlotCursor.EntrySize);
		}
	}

	// Caller-owned records placed in a List are self-describing: the first
	// ULONG is the total guest allocation size. Keep that header named so
	// disposal validates the record through a codec instead of reaching into
	// an anonymous offset.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListOwnedRecordHeader
	{
		internal const uint Size = 4;
		internal uint Length;
	}

	internal enum MuiListOwnedRecordHeaderField : byte
	{
		Length,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListOwnedRecordHeaderFieldCursor
	{
		internal APTR Record;
		internal MuiListOwnedRecordHeaderField Field;
	}

	internal static class MuiListOwnedRecordHeaderFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListOwnedRecordHeaderFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (cursor.Field != MuiListOwnedRecordHeaderField.Length ||
				cursor.Record.IsNull || !platform.IsMapped(cursor.Record,
					MuiListOwnedRecordHeader.Size)) return false;
			address = cursor.Record;
			return true;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListOwnedRecordHeaderField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListOwnedRecordHeaderFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			value = platform.ReadUInt32(address, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListOwnedRecordHeaderField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListOwnedRecordHeaderFieldCursor);
			cursor.Record = record;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var address)) return false;
			platform.WriteUInt32(address, 0, value);
			return true;
		}
	}

	internal static class MuiListOwnedRecordHeaderCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListOwnedRecordHeader value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			return MuiListOwnedRecordHeaderFieldCursorCodec.TryReadUInt32(
				ref platform, address, MuiListOwnedRecordHeaderField.Length,
				out value.Length);
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListOwnedRecordHeader value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return MuiListOwnedRecordHeaderFieldCursorCodec.TryWriteUInt32(
				ref platform, address, MuiListOwnedRecordHeaderField.Length,
				value.Length);
		}
	}

	// Quiet/redraw coalescing is state, not a property of the List index. Keep
	// it in one named guest record so mutation paths never need to grow the
	// packed entry header or infer fields from private offsets.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	private struct MuiListRedrawState
	{
		public const uint Size = 12;
		public uint Magic;
		public uint Dirty;
		public uint Requests;
	}

	// The public MUIA_List_Active value is zero for an empty MorphOS 3.20 list,
	// so a named cursor record keeps both the selected row and presence bit. It
	// distinguishes that projection from a real row zero after insertion.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListActiveState
	{
		public const uint Size = 12;
		public const uint Cookie = 0x41435456u; // 'ACTV'

		public uint Magic;
		public uint HasActive;
		public uint Active;
	}

	// The public pixel viewport attributes are derived from the same bounded
	// row geometry as Layout and hit-testing. Keep the values in one named
	// guest record so callers never depend on private attribute offsets.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListViewportState
	{
		public const uint Size = 32;
		public uint Magic;
		public uint TopPixel;
		public uint VisiblePixel;
		public uint TotalPixel;
		public uint First;
		public uint LineHeight;
		public uint Visible;
		public uint DropMark;
	}

	// MorphOS keeps List interaction policy at construction time. Keep the
	// BOOL/enum values together in a named guest record so direct List state is
	// not reconstructed from three unrelated attribute reads by future input or
	// composite-scroller consumers.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListInteractionPolicyState
	{
		internal const uint Size = 16;
		internal const uint Cookie = 0x4C49504Fu; // 'LIPO'

		internal uint Magic;
		internal uint Input;
		internal uint MultiSelect;
		internal uint ScrollerPos;
	}

	// Direct List click projections share the same numeric attributes as
	// Listview, but MorphOS keeps them on each List object as well. Keep the
	// click result and default keyboard column together so Listview forwarding
	// can publish one coherent child projection without raw attribute offsets.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListClickState
	{
		internal const uint Size = 24;
		internal const uint Cookie = 0x4C434C4Bu; // 'LCLK'

		internal uint Magic;
		internal uint ClickColumn;
		internal uint DoubleClick;
		internal uint AgainClick;
		internal uint Clicks;
		internal uint DefClickColumn;
	}

	// Construct, destruct, display, compare, and multi-select hooks form one
	// List policy. Keep their guest pointers in a named record so insertion,
	// rendering, sorting, editing, and selection all consume one coherent
	// hook configuration instead of rereading unrelated scalar attributes.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListHookPolicyState
	{
		internal const uint Size = 24;
		internal const uint Cookie = 0x4C484F4Bu; // 'LHOK'

		internal uint Magic;
		internal uint ConstructHook;
		internal uint DestructHook;
		internal uint DisplayHook;
		internal uint CompareHook;
		internal uint MultiTestHook;
	}

	// SortColumn and TitleClick are the two public projections produced by
	// format-driven title interaction. Keep the selected column and the last
	// title-click column together so sorting and title notifications cannot
	// drift across separate scalar stores.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListSortState
	{
		internal const uint Size = 12;
		internal const uint Cookie = 0x4C534F52u; // 'LSOR'

		internal uint Magic;
		internal uint SortColumn;
		internal uint TitleClick;
	}

	// List presentation and interaction switches share one normalized policy.
	// Keeping these values in a guest-resident record makes drawing, editing,
	// drag validation, and viewport navigation consume the same state instead
	// of rereading unrelated public attributes.  The public attributes remain
	// projections of this record for ABI compatibility.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListPresentationPolicyState
	{
		internal const uint Size = 48;
		internal const uint Cookie = 0x4C504F4Cu; // 'LPOL'

		internal uint Magic;
		internal uint Editable;
		internal uint Quiet;
		internal uint AdjustHeight;
		internal uint AdjustWidth;
		internal uint Stripes;
		internal uint ShowDropMarks;
		internal uint DragSortable;
		internal uint DragType;
		internal uint AutoVisible;
		internal uint AutoLineHeight;
		internal uint MinLineHeight;
	}

	// MUIA_List_Pool, MUIA_List_PoolPuddleSize, and
	// MUIA_List_PoolThreshSize are one construction policy. Keep the policy in
	// a named guest record so future internal-pool allocation can consume the
	// same state without scattering three independent attribute reads through
	// construct/destruct paths. The current freestanding profile accepts a
	// caller-owned pool and otherwise leaves allocation to the platform seam;
	// it does not invent an Exec pool in managed code.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListPoolPolicyState
	{
		internal const uint Size = 20;
		internal const uint Cookie = 0x504F4F4Cu; // 'POOL'

		internal uint Magic;
		internal APTR Pool;
		internal uint PuddleSize;
		internal uint ThresholdSize;
		internal uint UsesExternalPool;
	}

	internal enum MuiListPoolPolicyField : byte
	{
		Magic,
		Pool,
		PuddleSize,
		ThresholdSize,
		UsesExternalPool,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListPoolPolicyFieldCursor
	{
		internal APTR Address;
		internal MuiListPoolPolicyField Field;
	}

	internal static class MuiListPoolPolicyFieldCursorCodec
	{
		private static bool TryResolve(MuiListPoolPolicyField field,
			out uint offset)
		{
			switch (field)
			{
				case MuiListPoolPolicyField.Magic:
					offset = 0;
					return true;
				case MuiListPoolPolicyField.Pool:
					offset = 4;
					return true;
				case MuiListPoolPolicyField.PuddleSize:
					offset = 8;
					return true;
				case MuiListPoolPolicyField.ThresholdSize:
					offset = 12;
					return true;
				case MuiListPoolPolicyField.UsesExternalPool:
					offset = 16;
					return true;
			}
			offset = 0;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListPoolPolicyFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (!TryResolve(cursor.Field, out var offset) || cursor.Address.IsNull ||
				cursor.Address.Raw > uint.MaxValue - offset ||
				!platform.IsMapped(cursor.Address, MuiListPoolPolicyState.Size))
				return false;
			address = APTR.FromPointer(cursor.Address.Raw + offset);
			return platform.IsMapped(address, 4);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPoolPolicyField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			var cursor = default(MuiListPoolPolicyFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			value = platform.ReadUInt32(fieldAddress, 0);
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPoolPolicyField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			var cursor = default(MuiListPoolPolicyFieldCursor);
			cursor.Address = address;
			cursor.Field = field;
			if (!TryGetAddress(ref platform, cursor, out var fieldAddress))
				return false;
			platform.WriteUInt32(fieldAddress, 0, value);
			return true;
		}
	}

	internal static class MuiListPoolPolicyStateCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListPoolPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (address.IsNull || !platform.IsMapped(address,
				MuiListPoolPolicyState.Size) ||
				!MuiListPoolPolicyFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListPoolPolicyField.Magic, out var magic) ||
				magic != MuiListPoolPolicyState.Cookie)
				return false;
			value.Magic = MuiListPoolPolicyState.Cookie;
			if (!MuiListPoolPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiListPoolPolicyField.Pool, out var pool) ||
				!MuiListPoolPolicyFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListPoolPolicyField.PuddleSize,
					out value.PuddleSize) ||
				!MuiListPoolPolicyFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListPoolPolicyField.ThresholdSize,
					out value.ThresholdSize) ||
				!MuiListPoolPolicyFieldCursorCodec.TryReadUInt32(ref platform,
					address, MuiListPoolPolicyField.UsesExternalPool,
					out var external)) return false;
			value.Pool = APTR.FromPointer(pool);
			value.UsesExternalPool = external == 0 ? 0u : 1u;
			return true;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPoolPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListPoolPolicyState.Size) ||
				value.Magic != MuiListPoolPolicyState.Cookie) return false;
			return MuiListPoolPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiListPoolPolicyField.Magic, value.Magic) &&
				MuiListPoolPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListPoolPolicyField.Pool, value.Pool.Raw) &&
				MuiListPoolPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListPoolPolicyField.PuddleSize, value.PuddleSize) &&
				MuiListPoolPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListPoolPolicyField.ThresholdSize, value.ThresholdSize) &&
				MuiListPoolPolicyFieldCursorCodec.TryWriteUInt32(ref platform, address,
					MuiListPoolPolicyField.UsesExternalPool,
					value.UsesExternalPool == 0 ? 0u : 1u);
		}
	}

	private enum MuiListFormatField
	{
		Delta,
		Weight,
		MinWidth,
		MaxWidth,
		Column,
		Flags,
		Preparse,
		PreparseLength,
	}

	private enum MuiListTextAlignment
	{
		Left,
		Center,
		Right,
	}

	// ---- Public attribute identifiers (autodoc MUI_List.doc) -----------------
	private const uint Active = 0x8042391cu;          // [ISG] LONG
	private const uint Editable = 0x8042f9b9u;       // [ISG] BOOL
	private const uint Entries = 0x80421654u;         // [..G] LONG
	private const uint First = 0x804238d4u;           // [.SG] LONG
	private const uint Visible = 0x8042191fu;         // [..G] LONG
	// MorphOS publishes -1 when the List has no visible geometry (for example
	// while its window is iconified). Keep the ABI sentinel named so composite
	// scroller code never mistakes it for a huge row capacity.
	internal const uint VisibleOff = uint.MaxValue;
	private const uint ConstructHook = 0x8042894fu;   // [IS.] struct Hook *
	private const uint DestructHook = 0x804297ceu;    // [IS.] struct Hook *
	private const uint DisplayHook = 0x8042b4d5u;     // [IS.] struct Hook *
	private const uint CompareHook = 0x80425c14u;     // [IS.] struct Hook *
	private const uint SourceArray = 0x8042c0a0u;     // [I..] APTR
	private const uint Pool = 0x80423431u;            // [I.G] APTR
	private const uint Quiet = 0x8042d8c7u;           // [.SG] BOOL
	private const uint SelectChange = 0x8042178fu;    // [..G] BOOL
	private const uint Input = 0x8042682du;           // [I..] BOOL
	private const uint MultiSelect = 0x80427e08u;     // [I..] LONG
	private const uint ScrollerPos = 0x8042b1b4u;     // [I..] LONG
	private const uint AgainClick = 0x804214c2u;      // [ISG] BOOL
	private const uint ClickColumn = 0x8042d1b3u;     // [.SG] LONG
	private const uint DefClickColumn = 0x8042b296u;  // [ISG] LONG
	private const uint DoubleClick = 0x80424635u;     // [ISG] BOOL
	private const uint MultiTestHook = 0x8042c2c6u;   // [IS.] struct Hook *
	private const uint SortColumn = 0x8042cafbu;      // [ISG] LONG
	private const uint PoolPuddleSize = 0x8042a4ebu;  // [I..] ULONG
	private const uint PoolThreshSize = 0x8042c48cu;   // [I..] ULONG
	private const uint InsertPosition = 0x8042d0cdu;  // [..G] LONG
	private const uint Format = 0x80423c0au;           // [ISG] STRPTR
	private const uint MaxColumns = 0x8042a98bu;       // [I..] LONG
	private const uint AdjustHeight = 0x8042850du;     // [I..] BOOL
	private const uint AdjustWidth = 0x8042354au;      // [I..] BOOL
	private const uint Stripes = 0x8042a308u;          // [ISG] BOOL
	private const uint DropMark = 0x8042aba6u;         // [..G] LONG
	private const uint ShowDropMarks = 0x8042c6f3u;    // [ISG] BOOL
	private const uint DragSortable = 0x80426099u;     // [ISG] BOOL
	private const uint DragType = 0x80425cd3u;         // [ISG] LONG
	private const uint AutoVisible = 0x8042a445u;      // [ISG] BOOL
	private const uint MinLineHeight = 0x8042d1c3u;    // [I..] LONG
	private const uint AutoLineHeight = 0x8042bc08u;   // [ISG] BOOL
	private const uint LineHeight = 0x80425880u;       // [..G] ULONG
	private const uint Title = 0x80423e66u;            // [ISG] STRPTR/BOOL
	private const uint TitleArray = 0x80427d95u;       // [ISG] STRPTR *
	private const uint TitleClick = 0x80422fd9u;       // [.SG] LONG
	private const uint HScrollerVisibility = 0x804280a6u; // [I..] LONG
	private const uint HideColumn = 0x80428052u;       // [IS.] LONG
	private const uint ShowColumn = 0x8042c840u;       // [IS.] LONG
	private const uint ColumnOrder = 0x9d5100f6u;      // [.SG] BYTE*
	private const uint TopPixel = 0x80429df3u;         // [.SG] LONG
	private const uint TotalPixel = 0x8042a8f5u;        // [..G] ULONG
	private const uint VisiblePixel = 0x804273e9u;      // [..G] ULONG
	private const uint LeftEdge = 0x8042bec6u;
	private const uint TopEdge = 0x8042509bu;
	private const uint Width = 0x8042b59cu;
	private const uint Height = 0x80423237u;
	private const uint RightEdge = 0x8042ba82u;
	private const uint BottomEdge = 0x8042e552u;
	private const uint Font = 0x8042be50u;
	private const uint RenderInfo = 0x7fff0001u;
	private const uint RowHeight = 8;
	private const uint MaximumLineHeight = 4096;
	private const uint MaximumAdjustHeight = 32767;
	private const uint MaximumAdjustWidth = 32767;
	private const uint StripePen = 2;
	private const uint DropMarkPen = 3;
	private const uint MaximumAutoLines = 256;
	private const uint MaximumDrawColumns = MaximumColumns;

	// ---- MUIV_List_* selectors ----------------------------------------------
	private const uint ActiveOff = 0xFFFFFFFFu;       // MUIV_List_Active_Off (-1)
	private const int ActiveTop = -2;                 // MUIV_List_Active_Top
	private const int ActiveBottom = -3;              // MUIV_List_Active_Bottom
	private const int ActiveUp = -4;                  // MUIV_List_Active_Up
	private const int ActiveDown = -5;                // MUIV_List_Active_Down
	private const int ActivePageUp = -6;              // MUIV_List_Active_PageUp
	private const int ActivePageDown = -7;            // MUIV_List_Active_PageDown
	private const int InsertTop = 0;
	private const int InsertActive = -1;
	private const int InsertSorted = -2;
	private const int InsertBottom = -3;
	private const int RemoveFirst = 0;
	private const int RemoveActive = -1;
	private const int RemoveLast = -2;
	private const int RemoveSelected = -3;
	private const int GetEntryActive = -1;
	private const int SelectActive = -1;
	private const int SelectAll = -2;
	private const uint SelectOff = 0;
	private const uint SelectOn = 1;
	private const uint SelectToggle = 2;
	private const uint SelectAsk = 3;
	private const int NextSelectedStart = -1;
	private const int NextSelectedEnd = -1;
	private const int RedrawActive = -1;
	private const int RedrawAll = -2;
	private const int RedrawEntry = -3;
	private const int MoveActive = -1;
	private const int MoveBottom = -2;
	private const int MoveNext = -3;
	private const int MovePrevious = -4;
	private const int ExchangeActive = -1;
	private const int ExchangeBottom = -2;
	private const int ExchangeNext = -3;
	private const int ExchangePrevious = -4;
	private const int JumpActive = -1;
	private const int JumpBottom = -2;
	private const int JumpDown = -3;
	private const int JumpUp = -4;
	private const int EditActive = -1;
	private const uint EndEditDone = 0;
	private const uint EndEditAbort = 1;
	private const uint EndEditPrev = 2;
	private const uint EndEditNext = 3;
	private const uint EndEditUp = 4;
	private const uint EndEditDown = 5;
	// MUI_List_TestPos_Result flags from libraries/mui.h. These describe the
	// pointer's relation to the list cell, not the entry selection state.
	private const uint TestPosAbove = MuiListTestPosResult.FlagAbove;
	private const uint TestPosBelow = MuiListTestPosResult.FlagBelow;
	private const uint TestPosLeft = MuiListTestPosResult.FlagLeft;
	private const uint TestPosRight = MuiListTestPosResult.FlagRight;
	// MUIV_List_*Hook_String share the value -1; StringArray shares -2.
	private const uint HookString = 0xFFFFFFFFu;
	private const uint HookStringArray = 0xFFFFFFFEu;
	private const uint StringContents = 0x80428FFDu;

	// ---- Private per-object state --------------------------------------------
	// The list header pointer is parked in the object attribute list under a
	// reserved key so it travels with the object and is retired on disposal.
	private const uint ListHeaderKey = 0x7F080001u;
	private const uint FormatColumnsKey = 0x7F080002u;
	private const uint FormatDescriptorKey = 0x7F080003u;
	private const uint ColumnLayoutKey = 0x7F080004u;
	private const uint ColumnLayoutWidthKey = 0x7F080005u;
	private const uint EditStateKey = 0x7F080006u;
	private const uint TitleArrayStateKey = 0x7F080007u;
	private const uint TitleStateKey = 0x7F080016u;
	private const uint SelectionSignalKey = 0x7F080017u;
	private const uint FormatPolicyKey = 0x7F080018u;
	private const uint FontStateKey = 0x7F080019u;
	private const uint RedrawStateKey = 0x7F080008u;
	private const uint ActiveStateKey = 0x7F08000Fu;
	private const uint ColumnMetricsKey = 0x7F080009u;
	private const uint ViewportStateKey = 0x7F08000Au;
	private const uint ColumnVisibilityKey = 0x7F08000Bu;
	private const uint ColumnOrderKey = 0x7F08000Cu;
	private const uint HScrollerStateKey = 0x7F08000Du;
		private const uint PoolPolicyKey = 0x7F080010u;
		private const uint InteractionPolicyKey = 0x7F080011u;
		private const uint ClickStateKey = 0x7F080012u;
		private const uint HookPolicyKey = 0x7F080013u;
		private const uint SortStateKey = 0x7F080014u;
		private const uint PresentationPolicyKey = 0x7F080015u;
	// A Listview-owned child records its composite parent here.  This is an
	// internal named attribute, not a public ABI offset; it lets child-list
	// selection changes be projected back to MUIA_Listview_SelectChange.
	internal const uint ListviewOwnerKey = 0x7F08000Eu;
	private const uint EditStateCookie = 0x4C454449u; // 'LEDI'
	private const uint TitleArrayStateCookie = 0x5449544Cu; // 'TITL'
	private const uint TitleStateCookie = MuiListTitleState.Cookie;
	private const uint SelectionSignalCookie = MuiListSelectionSignalState.Cookie;
	private const uint FormatPolicyCookie = MuiListFormatPolicyState.Cookie;
	private const uint FontStateCookie = MuiListFontState.Cookie;
	private const uint RedrawStateCookie = 0x52454452u; // 'REDR'
	private const uint ActiveStateCookie = MuiListActiveState.Cookie;
	private const uint ColumnMetricsCookie = 0x434D4554u; // 'CMET'
	private const uint ViewportStateCookie = 0x56505754u; // 'VPWT'
	private const uint ColumnVisibilityCookie = 0x434F4C56u; // 'COLV'
	private const uint ColumnOrderCookie = 0x434F5244u; // 'CORD'
	private const uint HScrollerAuto = 0;
	private const uint HScrollerAlways = 1;
	private const uint HScrollerNever = 2;
	private const uint DefaultPoolPuddleSize = 2008;
	private const uint DefaultPoolThreshSize = 1024;

	// Header block (guest owned). Fixed size, never grows.
	private const uint HeaderSize = MuiListHeaderState.Size;

	// Index slot (guest owned, contiguous). Eight bytes keeps GetEntry O(1).
	private const uint SlotSize = MuiListSlotState.Size;
	private const uint SlotSelected = 1;    // entry is selected
	private const uint SlotOwnedString = 2; // entry buffer allocated by us
	private const uint SlotOwnedStringArray = 4; // pointer table + strings owned by us
	private const uint SlotOwnedRecord = 8; // self-describing guest record owned by us
	private const uint ImageRecordSize = MuiListImageState.Size;
	private const uint MaximumImages = 256;

	private const uint InitialCapacity = 8;
	private const uint MaximumEntries = 0x00100000u; // hard bound on growth
	private const uint MaximumArrayEntries = 256;
	private const uint DefaultMaxColumns = 64;
	private const uint MaximumColumns = 256;
	private const uint FormatDescriptorSize = MuiListFormatDescriptor.Size;
	private const uint DescriptorBar = 1;
	private const uint DescriptorSortable = 2;
	private const uint DescriptorDescending = 4;
	private const uint DescriptorMinPixel = 8;
	private const uint DescriptorMaxPixel = 16;
	private const uint DescriptorMinContent = 32;
	private const uint DescriptorMaxContent = 64;
	private const uint DescriptorWeightContent = 128;
	private const uint ColumnGeometryRecordSize = MuiListColumnGeometry.Size;
	private const uint MaximumGeometryColumns = MaximumColumns;
	private const uint MaximumStringLength = 4096;
	private const int DropMarkNone = -1;
	private const uint DragTypeNone = 0;
	private const uint DragTypeImmediate = 1;
	// Self-describing owned records (Dirlist FileInfoBlock-like entries) store
	// their total allocation size in the first word; this bounds the free path.
	private const uint MaximumRecordSize = 65536;
	private const uint MultiSelectNone = 0;
	private const uint MultiSelectDefault = 1;
	private const uint MultiSelectShifted = 2;
	private const uint MultiSelectAlways = 3;
	private const uint ScrollerPosDefault = 0;
	private const uint ScrollerPosLeft = 1;
	private const uint ScrollerPosRight = 2;
	private const uint ScrollerPosNone = 3;

	// ---- Class determination -------------------------------------------------

	public static MuiCollectionClass Classify<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return MuiCollectionClass.Unknown;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return MuiCollectionClass.Unknown;
		var classRecord = objectValue.Class;
		return ClassifyRecord(ref platform, classRecord);
	}

	public static MuiCollectionClass ClassifyRecord<TPlatform>(ref TPlatform platform,
		APTR classRecord) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessClassCodec.TryRead(ref platform, classRecord,
			out var classValue))
			return MuiCollectionClass.Unknown;
		return ClassifyName(ref platform, classValue.Name);
	}

	// FNV-1a over the lowercased, ".mui"-terminated class name, mirroring the
	// MG07 common-control classifier. Bounded and mapping-safe; only the exact
	// registered collection names resolve, so no MorphOS vector is consulted.
	private static MuiCollectionClass ClassifyName<TPlatform>(ref TPlatform platform,
		APTR name) where TPlatform : struct, IMuiGuestMemory
	{
		if (name.IsNull) return MuiCollectionClass.Unknown;
		uint hash = 2166136261u;
		var length = 0;
		for (; length < 64; length++)
		{
			if (!platform.IsMapped(name, (uint)length + 1))
				return MuiCollectionClass.Unknown;
			var ch = platform.ReadUInt8(name, length);
			if (ch == 0) break;
			hash = (hash ^ Lower(ch)) * 16777619u;
		}
		if (length < 5 || length == 64 ||
			platform.ReadUInt8(name, length - 4) != (byte)'.' ||
			Lower(platform.ReadUInt8(name, length - 3)) != (byte)'m' ||
			Lower(platform.ReadUInt8(name, length - 2)) != (byte)'u' ||
			Lower(platform.ReadUInt8(name, length - 1)) != (byte)'i')
			return MuiCollectionClass.Unknown;
		switch (hash)
		{
			case 0x52923142u: return MuiCollectionClass.List;
			case 0x81882BAFu: return MuiCollectionClass.Listview;
			case 0x4C11226Du: return MuiCollectionClass.Floattext;
			case 0xCEE3040Fu: return MuiCollectionClass.Dirlist;
			case 0x8C0D7E94u: return MuiCollectionClass.Volumelist;
			case 0x90EF502Au: return MuiCollectionClass.Stringscroll;
		}
		return MuiCollectionClass.Unknown;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte Lower(byte ch) =>
		ch >= (byte)'A' && ch <= (byte)'Z' ? unchecked((byte)(ch + 32)) : ch;

	// True for every collection class that owns the shared guest-resident List
	// backbone: List itself and its Floattext/Dirlist/Volumelist subclasses.
	internal static bool IsListBacked(MuiCollectionClass cls) =>
		cls == MuiCollectionClass.List || cls == MuiCollectionClass.Floattext ||
		cls == MuiCollectionClass.Dirlist || cls == MuiCollectionClass.Volumelist;

	// Keep the public [..G] and [I..] contracts at the class-aware boundary.
	// Internal publication uses SetInternal/MuiHeadlessObjectCore directly, so
	// this gate protects application OM_SET/OM_UPDATE calls without replacing
	// the named guest-resident state records with ad-hoc mutability flags.
	private static bool IsReadOnlyOrConstructionAttribute(uint attribute) =>
		attribute == Entries || attribute == Visible ||
		attribute == SelectChange || attribute == InsertPosition ||
		attribute == Input || attribute == MultiSelect ||
		attribute == ScrollerPos ||
		attribute == DropMark || attribute == LineHeight ||
		attribute == TotalPixel ||
		attribute == VisiblePixel || attribute == MaxColumns ||
		attribute == SourceArray;

	// Class-aware runtime setters for the first List attributes. The generic
	// headless store remains the raw backing store; this hook only claims a
	// fully constructed List and writes normalized values through that raw seam.
	// Construction tags are intentionally applied raw, then normalized once the
	// guest-resident List header and source entries exist.
	private static bool IsClassAwareAttribute(uint attribute) =>
		attribute == Active || attribute == Editable || attribute == First ||
		attribute == Quiet || attribute == Format || attribute == MaxColumns ||
		attribute == SortColumn || attribute == AutoLineHeight ||
		attribute == Stripes || attribute == ShowDropMarks ||
		attribute == DragSortable || attribute == DragType ||
		attribute == AutoVisible || attribute == TitleArray || attribute == Title ||
		attribute == HideColumn || attribute == ShowColumn ||
		attribute == ColumnOrder || attribute == HScrollerVisibility ||
		attribute == AgainClick || attribute == ClickColumn ||
		attribute == DefClickColumn || attribute == DoubleClick ||
		attribute == ConstructHook || attribute == DestructHook ||
		attribute == DisplayHook || attribute == CompareHook ||
		attribute == MultiTestHook || attribute == TitleClick;

	private static bool IsPresentationPolicyAttribute(uint attribute) =>
		attribute == Editable || attribute == Stripes ||
		attribute == ShowDropMarks || attribute == DragSortable ||
		attribute == DragType || attribute == AutoVisible ||
		attribute == AutoLineHeight;

	internal static bool TrySetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var classRecord = objectValue.Class;
		var collectionClass = ClassifyRecord(ref platform, classRecord);
		if (!IsListBacked(collectionClass))
			return false;
		var obj = objectValue.Boopsi;
		if (collectionClass == MuiCollectionClass.Floattext &&
			MuiFloattextCore.IsStateAttribute(attribute))
			return MuiFloattextCore.TrySetAttribute(ref platform, state, record,
				attribute, value, notify);
		if (Header(ref platform, state, obj).IsNull) return false;
		if (attribute == AgainClick || attribute == ClickColumn ||
			attribute == DefClickColumn || attribute == DoubleClick)
			return ApplyClickStateAttribute(ref platform, state, record, obj,
				attribute, value, notify);
		if (attribute == ConstructHook || attribute == DestructHook ||
			attribute == DisplayHook || attribute == CompareHook ||
			attribute == MultiTestHook)
			return ApplyHookPolicyAttribute(ref platform, state, record, obj,
				attribute, value, notify);
		if (attribute == TitleClick)
			return ApplySortStateAttribute(ref platform, state, record, obj,
				attribute, value, notify);
		if (attribute == Quiet)
			return ApplyQuiet(ref platform, state, record, obj, value, notify);
		if (IsPresentationPolicyAttribute(attribute))
			return ApplyPresentationPolicyAttribute(ref platform, state, record,
				obj, attribute, value, notify);
		if (attribute == Active)
			return ApplyActive(ref platform, state, record, obj,
				unchecked((int)value), notify);
		if (attribute == First)
			return ApplyFirst(ref platform, state, record, obj,
				unchecked((int)value), notify);
		if (attribute == Font)
			return ApplyFont(ref platform, state, record, obj,
				APTR.FromPointer(value), notify);
		if (attribute == Format)
			return ApplyFormat(ref platform, state, record, obj,
				APTR.FromPointer(value), notify);
		if (attribute == MaxColumns)
			return ApplyMaxColumns(ref platform, state, record, obj, value, notify);
		if (attribute == SortColumn)
			return ApplySortColumn(ref platform, state, record, obj, value, notify);
		if (attribute == TitleArray)
			return ApplyTitleArray(ref platform, state, record, obj,
				APTR.FromPointer(value), notify);
		if (attribute == Title)
			// [ISG] STRPTR (or BOOL TRUE == "use first entry as the title"). The
			// pointer stays caller-owned, mirroring Format; only the neutral title
			// row (drawn through the display hook) consumes it.
			return ApplyTitle(ref platform, state, record, obj, value, notify);
		if (attribute == HideColumn)
			return ApplyColumnVisibility(ref platform, state, record, obj, value,
				true, notify);
		if (attribute == ShowColumn)
			return ApplyColumnVisibility(ref platform, state, record, obj, value,
				false, notify);
		if (attribute == ColumnOrder)
			return ApplyColumnOrder(ref platform, state, record, obj,
				APTR.FromPointer(value), notify);
		return false;
	}

	// ---- Construction / lifecycle --------------------------------------------


	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// MorphOS documents HScrollerVisibility as [I..]: construction-only.
		// Runtime horizontal-scroller recomputation will be added with the
		// composite scroller seam; do not silently turn OM_SET into a policy
		// mutation before that state machine exists.
		if (attribute == HScrollerVisibility) return false;
		// MUIA_List_MinLineHeight is [I..] in MorphOS: construction tags may
		// provide it, but OM_SET/OM_UPDATE must not mutate the live list metric.
		if (attribute == MinLineHeight || attribute == LineHeight) return false;
		if (attribute == AdjustHeight) return false;
		if (attribute == AdjustWidth) return false;
		// MUIA_List_Pool is [I.G] and the two size tags are [I..]. These values
		// describe an allocator fixed at construction time; do not let a later
		// OM_SET mutate the named policy record underneath live entries.
		if (attribute == Pool) return false;
		// MUIA_List_PoolPuddleSize and MUIA_List_PoolThreshSize are [I..].
		// Their values describe an allocator that is fixed at construction time;
		// do not let a later OM_SET mutate the named policy record underneath live
		// entries.
		if (attribute == PoolPuddleSize || attribute == PoolThreshSize) return false;
		// The direct List interaction policy is construction-only as a whole.
		// Rejecting these lower-level writes as well keeps the named policy record
		// authoritative instead of allowing an internal scalar mutation to drift
		// away from it.
		if (attribute == Input || attribute == MultiSelect ||
			attribute == ScrollerPos) return false;
		if (attribute == DropMark) return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (IsClassAwareAttribute(attribute))
		{
			var applied = TrySetAttribute(ref platform, state, record, attribute,
				value, notify);
			// Active and First can move the visible window without a Layout pass.
			// Keep the guest-resident viewport record and public pixel projections
			// coherent for direct List callers as well as Listview input paths.
			if (applied && (attribute == Active || attribute == First))
				RefreshViewportMetrics(ref platform, state, obj);
			return applied;
		}
		if (TrySetAttribute(ref platform, state, record, attribute, value, notify))
			return true;
		return MuiHeadlessObjectCore.SetRecordAttribute(ref platform, state, record,
			attribute, value, notify);
	}

	// Public OM_SET/OM_UPDATE entry point.  The lower-level SetAttribute seam is
	// also used by listview interaction and persistence code to publish derived
	// state (for example First and the viewport projections), so keep the
	// MorphOS [..G]/[I..] boundary at the dispatcher-facing entry point rather
	// than making those internal transitions depend on an ambient mutability
	// flag.  All state remains in the named List records above.
	public static bool SetRuntimeAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (IsReadOnlyOrConstructionAttribute(attribute)) return false;
		return SetAttribute(ref platform, state, obj, attribute, value, notify);
	}

	// Struct-backed qualification seam for the two MorphOS column visibility
	// controls. The normal OM_SET path above remains the public ABI route; this
	// narrow entry avoids pulling the generic attribute resolver into a focused
	// freestanding closure while exercising the same guest visibility record.
	public static bool SetColumnVisibility<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column, bool hide)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || Header(ref platform, state, obj).IsNull)
			return false;
		return ApplyColumnVisibility(ref platform, state, record, obj, column,
			hide, false);
	}

	// Focused freestanding seam for the BYTE* ColumnOrder attribute. The normal
	// SetAttribute path remains the public ABI route; this typed entry keeps a
	// native qualification closure from pulling in the generic tag resolver.
	public static bool SetColumnOrder<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || Header(ref platform, state, obj).IsNull)
			return false;
		return ApplyColumnOrder(ref platform, state, record, obj, source, false);
	}

	// Standalone struct seam used by the native qualification root. It writes
	// the same guest order record used by List, but accepts caller-provided
	// storage so the focused closure does not need the full List FORMAT parser.
	public static bool WriteColumnOrder<TPlatform>(ref TPlatform platform,
		APTR storage, APTR values, APTR source, uint columns)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (columns == 0 || columns > MaximumGeometryColumns ||
			storage.IsNull || values.IsNull ||
			!platform.IsMapped(storage, MuiListColumnOrderState.Size)) return false;
		var valueBytes = ColumnOrderValueBytes(columns);
		if (!platform.IsMapped(values, valueBytes) ||
			!PopulateColumnOrderValues(ref platform, values, source, columns))
			return false;
		var stateValue = default(MuiListColumnOrderState);
		stateValue.Magic = ColumnOrderCookie;
		stateValue.Count = columns;
		stateValue.Values = values;
		stateValue.Reserved = valueBytes;
		WriteColumnOrderState(ref platform, storage, stateValue);
		return true;
	}

	public static uint GetColumnOrderDisplayColumn<TPlatform>(
		ref TPlatform platform, APTR storage, uint displayColumn, uint fallback)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadColumnOrderState(ref platform, storage, out var value) ||
			displayColumn >= value.Count) return fallback;
		var cursor = default(MuiListColumnOrderByteCursor);
		cursor.Base = value.Values;
		cursor.Index = displayColumn;
		if (!MuiListColumnOrderByteCursorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return fallback;
		var resolved = platform.ReadUInt8(address, 0);
		return resolved;
	}

	// Create a List and normalize its construction. Class-aware defaults are
	// applied, the guest-resident index is allocated, and any MUIA_List_SourceArray
	// is materialized. Construction is failure-atomic: a failure at any stage
	// disposes the object and returns Null so no half-built list is observable.
	public static APTR CreateList<TPlatform>(ref TPlatform platform, APTR state,
		APTR classRecord, APTR tags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			classRecord, tags);
		if (obj.IsNull) return APTR.Null;
		if (!Construct(ref platform, state, classRecord, obj))
		{
			MuiCollectionLifecycle.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		return obj;
	}

	// Attach and initialize the fixed header/index state for a freshly created
	// List object. Safe to call once; non-List classes are ignored.
	public static bool Construct<TPlatform>(ref TPlatform platform, APTR state,
		APTR classRecord, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cls = ClassifyRecord(ref platform, classRecord);
		if (!IsListBacked(cls))
			return true;
		if (Header(ref platform, state, obj).IsNotNull) return true;

		var header = MuiHeadlessMemory.Allocate(ref platform, HeaderSize);
		if (header.IsNull) return false;
		var index = MuiHeadlessMemory.Allocate(ref platform,
			InitialCapacity * SlotSize);
		if (index.IsNull)
		{
			platform.Clear(header, HeaderSize);
			platform.Free(header, HeaderSize);
			return false;
		}
		var redrawState = MuiHeadlessMemory.Allocate(ref platform,
			MuiListRedrawState.Size);
		if (redrawState.IsNull)
		{
			platform.Clear(index, InitialCapacity * SlotSize);
			platform.Free(index, InitialCapacity * SlotSize);
			platform.Clear(header, HeaderSize);
			platform.Free(header, HeaderSize);
			return false;
		}
		var redraw = default(MuiListRedrawState);
		redraw.Magic = RedrawStateCookie;
		WriteRedrawState(ref platform, redrawState, redraw);
		var headerValue = default(MuiListHeaderState);
		headerValue.Magic = MuiListHeaderState.Cookie;
		headerValue.Index = index;
		headerValue.Capacity = InitialCapacity;
		if (!MuiListHeaderCodec.Write(ref platform, header, headerValue))
		{
			platform.Clear(redrawState, MuiListRedrawState.Size);
			platform.Free(redrawState, MuiListRedrawState.Size);
			platform.Clear(index, InitialCapacity * SlotSize);
			platform.Free(index, InitialCapacity * SlotSize);
			platform.Clear(header, HeaderSize);
			platform.Free(header, HeaderSize);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ListHeaderKey, header.Raw, false))
		{
			platform.Clear(redrawState, MuiListRedrawState.Size);
			platform.Free(redrawState, MuiListRedrawState.Size);
			platform.Clear(index, InitialCapacity * SlotSize);
			platform.Free(index, InitialCapacity * SlotSize);
			platform.Clear(header, HeaderSize);
			platform.Free(header, HeaderSize);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			RedrawStateKey, redrawState.Raw, false))
		{
			platform.Clear(redrawState, MuiListRedrawState.Size);
			platform.Free(redrawState, MuiListRedrawState.Size);
			platform.Clear(index, InitialCapacity * SlotSize);
			platform.Free(index, InitialCapacity * SlotSize);
			platform.Clear(header, HeaderSize);
			platform.Free(header, HeaderSize);
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				ListHeaderKey, 0, false);
			return false;
		}

		// Class defaults.
		EnsureDefault(ref platform, state, obj, Active, ActiveOff);
		EnsureDefault(ref platform, state, obj, Editable, 0);
		SetInternal(ref platform, state, obj, Entries, 0);
		EnsureDefault(ref platform, state, obj, First, 0);
		SetInternal(ref platform, state, obj, Visible, 0);
		SetInternal(ref platform, state, obj, SelectChange, 0);
		EnsureDefault(ref platform, state, obj, SortColumn, 0);
		EnsureDefault(ref platform, state, obj, Quiet, 0);
		EnsureDefault(ref platform, state, obj, AdjustHeight, 0);
		EnsureDefault(ref platform, state, obj, AdjustWidth, 0);
		EnsureDefault(ref platform, state, obj, Stripes, 0);
		EnsureDefault(ref platform, state, obj, ShowDropMarks, 1);
		EnsureDefault(ref platform, state, obj, DropMark,
			unchecked((uint)DropMarkNone));
		EnsureDefault(ref platform, state, obj, DragSortable, 0);
		EnsureDefault(ref platform, state, obj, DragType, DragTypeNone);
		EnsureDefault(ref platform, state, obj, AutoVisible, 0);
		EnsureDefault(ref platform, state, obj, MinLineHeight, RowHeight);
		EnsureDefault(ref platform, state, obj, AutoLineHeight, 0);
		SetInternal(ref platform, state, obj, LineHeight, RowHeight);
		EnsureDefault(ref platform, state, obj, Title, 0);
		EnsureDefault(ref platform, state, obj, TitleClick,
			unchecked((uint)-1));
		EnsureDefault(ref platform, state, obj, HScrollerVisibility,
			HScrollerAuto);
		EnsureDefault(ref platform, state, obj, PoolPuddleSize,
			DefaultPoolPuddleSize);
		EnsureDefault(ref platform, state, obj, PoolThreshSize,
			DefaultPoolThreshSize);
		EnsureDefault(ref platform, state, obj, DefClickColumn, 0);
		if (!EnsurePoolPolicy(ref platform, state, obj) ||
			!EnsureInteractionPolicy(ref platform, state, obj) ||
			!EnsureClickState(ref platform, state, obj) ||
			!EnsureHookPolicy(ref platform, state, obj) ||
			!EnsureSortState(ref platform, state, obj) ||
			!EnsurePresentationPolicy(ref platform, state, obj) ||
			!EnsureTitleState(ref platform, state, obj) ||
			!EnsureSelectionSignalState(ref platform, state, obj) ||
			!EnsureFormatPolicyState(ref platform, state, obj) ||
			!EnsureFontState(ref platform, state, obj)) return false;

		// Materialize MUIA_List_SourceArray if present. Failure here rolls the
		// whole construction back through CleanupRecords + DisposeObject.
		var source = Read(ref platform, state, obj, SourceArray, 0);
		if (source != 0 && !InsertSource(ref platform, state, obj,
			APTR.FromPointer(source)))
			return false;
		if (!EnsureActiveState(ref platform, state, obj)) return false;
		NormalizeConstructedState(ref platform, state, obj);
		return true;
	}

	// Apply construction-time List semantics after SourceArray materialization.
	// Tags are stored before the List header exists, so this is the first point
	// at which Active/First/Quiet can be interpreted safely.
	private static void NormalizeConstructedState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return;
		var active = unchecked((int)Read(ref platform, state, obj, Active,
			ActiveOff));
		if (EntryCount(ref platform, state, obj) != 0 && active != -1)
			SetActivePresence(ref platform, state, obj, true);
		ApplyActive(ref platform, state, record, obj, active, false);
		var first = unchecked((int)Read(ref platform, state, obj, First, 0));
		ApplyFirst(ref platform, state, record, obj, first, false);
		// EnsurePresentationPolicy normalized these construction values before
		// SourceArray materialization. Keep the named record authoritative rather
		// than repeating the public scalar normalization here.
		var dropMark = unchecked((int)Read(ref platform, state, obj, DropMark,
			unchecked((uint)DropMarkNone)));
		var count = EntryCount(ref platform, state, obj);
		if (dropMark < DropMarkNone) dropMark = DropMarkNone;
		if (dropMark > unchecked((int)count)) dropMark = unchecked((int)count);
		SetRaw(ref platform, state, record, DropMark,
			unchecked((uint)dropMark), false);
		var lineHeight = PresentationPolicyValue(ref platform, state, obj,
			MinLineHeight, RowHeight);
		if (lineHeight < RowHeight) lineHeight = RowHeight;
		if (lineHeight > MaximumLineHeight) lineHeight = MaximumLineHeight;
		SetRaw(ref platform, state, record, MinLineHeight, lineHeight, false);
		EnsurePresentationPolicy(ref platform, state, obj);
		RefreshLineHeight(ref platform, state, obj);
		NormalizeFormat(ref platform, state, record, obj);
		NormalizeColumnVisibility(ref platform, state, record, obj);
		NormalizeColumnOrder(ref platform, state, record, obj);
		NormalizeTitleArray(ref platform, state, record, obj);
		NormalizeHScrollerVisibility(ref platform, state, record, obj);
		NormalizePoolPolicy(ref platform, state, obj);
	}

	private static uint NormalizeHScrollerPolicy(uint value) =>
		value == HScrollerAlways || value == HScrollerNever
			? value : HScrollerAuto;

	private static bool EnsureHScrollerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint policy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HScrollerStateKey, 0));
		if (MuiListHScrollerStateCodec.TryRead(ref platform, block,
			out var current))
		{
			current.Policy = NormalizeHScrollerPolicy(policy);
			return MuiListHScrollerStateCodec.Write(ref platform, block, current);
		}

		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListHScrollerState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListHScrollerState);
		value.Magic = MuiListHScrollerState.Cookie;
		value.Policy = NormalizeHScrollerPolicy(policy);
		if (!MuiListHScrollerStateCodec.Write(ref platform, block, value))
		{
			platform.Clear(block, MuiListHScrollerState.Size);
			platform.Free(block, MuiListHScrollerState.Size);
			return false;
		}
		SetInternal(ref platform, state, obj, HScrollerStateKey, block.Raw);
		return true;
	}

	private static void NormalizeHScrollerVisibility<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var policy = NormalizeHScrollerPolicy(Read(ref platform, state, obj,
			HScrollerVisibility, HScrollerAuto));
		SetRaw(ref platform, state, record, HScrollerVisibility, policy, false);
		EnsureHScrollerState(ref platform, state, obj, policy);
	}

	private static bool EnsurePoolPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			PoolPolicyKey, 0));
		var value = default(MuiListPoolPolicyState);
		if (MuiListPoolPolicyStateCodec.TryRead(ref platform, block, out value))
		{
			value.Pool = APTR.FromPointer(Read(ref platform, state, obj, Pool, 0));
			value.PuddleSize = Read(ref platform, state, obj, PoolPuddleSize,
				DefaultPoolPuddleSize);
			value.ThresholdSize = Read(ref platform, state, obj, PoolThreshSize,
				DefaultPoolThreshSize);
			value.UsesExternalPool = value.Pool.IsNotNull ? 1u : 0u;
			return MuiListPoolPolicyStateCodec.Write(ref platform, block, value);
		}

		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListPoolPolicyState.Size);
		if (block.IsNull) return false;
		value = default;
		value.Magic = MuiListPoolPolicyState.Cookie;
		value.Pool = APTR.FromPointer(Read(ref platform, state, obj, Pool, 0));
		value.PuddleSize = Read(ref platform, state, obj, PoolPuddleSize,
			DefaultPoolPuddleSize);
		value.ThresholdSize = Read(ref platform, state, obj, PoolThreshSize,
			DefaultPoolThreshSize);
		value.UsesExternalPool = value.Pool.IsNotNull ? 1u : 0u;
		if (!MuiListPoolPolicyStateCodec.Write(ref platform, block, value))
		{
			platform.Clear(block, MuiListPoolPolicyState.Size);
			platform.Free(block, MuiListPoolPolicyState.Size);
			return false;
		}
		SetInternal(ref platform, state, obj, PoolPolicyKey, block.Raw);
		return true;
	}

	private static void NormalizePoolPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		// The two size tags are construction-only and are deliberately preserved
		// verbatim. A zero supplied by an application remains distinguishable from
		// an omitted tag; the future Exec pool adapter can apply its own validity
		// rules without changing the public MUI state.
		EnsurePoolPolicy(ref platform, state, obj);
	}

	private static uint NormalizeMultiSelect(uint value) =>
		value <= MultiSelectAlways ? value : MultiSelectDefault;

	private static uint NormalizeScrollerPos(uint value) =>
		value <= ScrollerPosNone ? value : ScrollerPosDefault;

	private static bool EnsureInteractionPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			InteractionPolicyKey, 0));
		var value = default(MuiListInteractionPolicyState);
		value.Magic = MuiListInteractionPolicyState.Cookie;
		var hasInput = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			obj, Input, out var input);
		var hasMultiSelect = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, MultiSelect, out var multiSelect);
		var hasScrollerPos = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, ScrollerPos, out var scrollerPos);
		value.Input = hasInput && input != 0 ? 1u : 0u;
		if (!hasInput) value.Input = 1;
		value.MultiSelect = NormalizeMultiSelect(hasMultiSelect ? multiSelect :
			MultiSelectDefault);
		value.ScrollerPos = NormalizeScrollerPos(hasScrollerPos ? scrollerPos :
			ScrollerPosDefault);

		if (block.IsNotNull && MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.InteractionPolicy,
			MuiListStateField.Magic, out var magic) &&
			magic == MuiListInteractionPolicyState.Cookie)
		{
			if (!MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.InteractionPolicy, MuiListStateField.Input,
				value.Input) || !MuiListStateFieldCursorCodec.TryWriteUInt32(
				ref platform, block, MuiListStateRecordKind.InteractionPolicy,
				MuiListStateField.MultiSelect, value.MultiSelect) ||
				!MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListStateRecordKind.InteractionPolicy,
					MuiListStateField.ScrollerPos, value.ScrollerPos)) return false;
		}
		else
		{
			block = MuiHeadlessMemory.Allocate(ref platform,
				MuiListInteractionPolicyState.Size);
			if (block.IsNull) return false;
			if (!MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.InteractionPolicy, MuiListStateField.Magic,
				value.Magic) || !MuiListStateFieldCursorCodec.TryWriteUInt32(
				ref platform, block, MuiListStateRecordKind.InteractionPolicy,
				MuiListStateField.Input, value.Input) ||
				!MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListStateRecordKind.InteractionPolicy,
					MuiListStateField.MultiSelect, value.MultiSelect) ||
				!MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
					MuiListStateRecordKind.InteractionPolicy,
					MuiListStateField.ScrollerPos, value.ScrollerPos))
			{
				platform.Clear(block, MuiListInteractionPolicyState.Size);
				platform.Free(block, MuiListInteractionPolicyState.Size);
				return false;
			}
			SetInternal(ref platform, state, obj, InteractionPolicyKey, block.Raw);
		}

		if (hasInput) SetInternal(ref platform, state, obj, Input, value.Input);
		if (hasMultiSelect)
			SetInternal(ref platform, state, obj, MultiSelect, value.MultiSelect);
		if (hasScrollerPos)
			SetInternal(ref platform, state, obj, ScrollerPos, value.ScrollerPos);
		return true;
	}

	internal static bool TryGetInteractionPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListInteractionPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			InteractionPolicyKey, 0));
		if (block.IsNull || !MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.InteractionPolicy,
			MuiListStateField.Magic, out var magic) ||
			magic != MuiListInteractionPolicyState.Cookie) return false;
		value.Magic = magic;
		return MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.InteractionPolicy, MuiListStateField.Input,
			out value.Input) && MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.InteractionPolicy,
			MuiListStateField.MultiSelect, out value.MultiSelect) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.InteractionPolicy,
				MuiListStateField.ScrollerPos, out value.ScrollerPos);
	}

	private static bool EnsureClickState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		if (block.IsNotNull && MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.ClickState,
			MuiListStateField.Magic, out var magic) &&
			magic == MuiListClickState.Cookie) return true;

		block = MuiHeadlessMemory.Allocate(ref platform, MuiListClickState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListClickState);
		value.Magic = MuiListClickState.Cookie;
		var hasClickColumn = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, ClickColumn, out var clickColumn);
		var hasDoubleClick = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, DoubleClick, out var doubleClick);
		var hasAgainClick = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, AgainClick, out var againClick);
		var hasDefClickColumn = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, DefClickColumn, out var defClickColumn);
		value.ClickColumn = hasClickColumn ? clickColumn : 0u;
		value.DoubleClick = hasDoubleClick && doubleClick != 0 ? 1u : 0u;
		value.AgainClick = hasAgainClick && againClick != 0 ? 1u : 0u;
		value.Clicks = 0;
		value.DefClickColumn = hasDefClickColumn ? defClickColumn : 0u;
		if (!WriteClickState(ref platform, block, value))
		{
			platform.Clear(block, MuiListClickState.Size);
			platform.Free(block, MuiListClickState.Size);
			return false;
		}
		SetInternal(ref platform, state, obj, ClickStateKey, block.Raw);
		if (hasDoubleClick) SetInternal(ref platform, state, obj, DoubleClick,
			value.DoubleClick);
		if (hasAgainClick) SetInternal(ref platform, state, obj, AgainClick,
			value.AgainClick);
		return true;
	}

	private static bool ApplyClickStateAttribute<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj,
		uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureClickState(ref platform, state, obj)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		if (!TryReadClickState(ref platform, block, out var clickState))
			return false;
		var normalized = value;
		if (attribute == AgainClick || attribute == DoubleClick)
			normalized = value == 0 ? 0u : 1u;
		if (attribute == ClickColumn) clickState.ClickColumn = value;
		else if (attribute == AgainClick) clickState.AgainClick = normalized;
		else if (attribute == DoubleClick) clickState.DoubleClick = normalized;
		else if (attribute == DefClickColumn)
			clickState.DefClickColumn = value;
		else return false;
		if (!WriteClickState(ref platform, block, clickState) ||
			!SetRaw(ref platform, state, record, attribute, normalized, notify))
			return false;
		return true;
	}

	// Listview input owns the user gesture, but MorphOS publishes the resulting
	// click projections on the child List as well. Keep that publication behind
	// one named List state seam so the composite cannot leave child and parent
	// click attributes disagreeing.
	internal static bool PublishClickState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column, uint clicks, bool doubleClick,
		bool againClick, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !EnsureClickState(ref platform, state, obj))
			return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		if (!TryReadClickState(ref platform, block, out var value)) return false;
		value.ClickColumn = column;
		value.Clicks = clicks;
		value.DoubleClick = doubleClick ? 1u : 0u;
		value.AgainClick = againClick ? 1u : 0u;
		return WriteClickState(ref platform, block, value) &&
			SetRaw(ref platform, state, record, ClickColumn, column, false) &&
			SetRaw(ref platform, state, record, DoubleClick,
				doubleClick ? 1u : 0u, notify && doubleClick) &&
			SetRaw(ref platform, state, record, AgainClick,
				againClick ? 1u : 0u, notify);
	}

	internal static bool TryGetClickState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListClickState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		return block.IsNotNull && TryReadClickState(ref platform, block,
			out value);
	}

	private static bool EnsureHookPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HookPolicyKey, 0));
		var value = default(MuiListHookPolicyState);
		value.Magic = MuiListHookPolicyState.Cookie;
		var hasConstruct = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, ConstructHook, out var constructHook);
		var hasDestruct = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, DestructHook, out var destructHook);
		var hasDisplay = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, DisplayHook, out var displayHook);
		var hasCompare = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, CompareHook, out var compareHook);
		var hasMultiTest = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, MultiTestHook, out var multiTestHook);
		value.ConstructHook = hasConstruct ? constructHook : 0;
		value.DestructHook = hasDestruct ? destructHook : 0;
		value.DisplayHook = hasDisplay ? displayHook : 0;
		value.CompareHook = hasCompare ? compareHook : 0;
		value.MultiTestHook = hasMultiTest ? multiTestHook : 0;

		if (block.IsNotNull && MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.HookPolicy,
			MuiListStateField.Magic, out var magic) &&
			magic == MuiListHookPolicyState.Cookie)
		{
			if (!WriteHookPolicy(ref platform, block, value)) return false;
		}
		else
		{
			if (block.IsNotNull && platform.IsMapped(block,
				MuiListHookPolicyState.Size))
			{
				platform.Clear(block, MuiListHookPolicyState.Size);
				platform.Free(block, MuiListHookPolicyState.Size);
			}
			block = MuiHeadlessMemory.Allocate(ref platform,
				MuiListHookPolicyState.Size);
			if (block.IsNull || !WriteHookPolicy(ref platform, block, value))
			{
				if (block.IsNotNull)
				{
					platform.Clear(block, MuiListHookPolicyState.Size);
					platform.Free(block, MuiListHookPolicyState.Size);
				}
				return false;
			}
			SetInternal(ref platform, state, obj, HookPolicyKey, block.Raw);
		}

		// Keep explicit construction tags normalized through the same raw seam;
		// omitted hook values remain omitted public attributes.
		if (hasConstruct) SetInternal(ref platform, state, obj, ConstructHook,
			value.ConstructHook);
		if (hasDestruct) SetInternal(ref platform, state, obj, DestructHook,
			value.DestructHook);
		if (hasDisplay) SetInternal(ref platform, state, obj, DisplayHook,
			value.DisplayHook);
		if (hasCompare) SetInternal(ref platform, state, obj, CompareHook,
			value.CompareHook);
		if (hasMultiTest) SetInternal(ref platform, state, obj, MultiTestHook,
			value.MultiTestHook);
		return true;
	}

	private static bool ApplyHookPolicyAttribute<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj,
		uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureHookPolicy(ref platform, state, obj)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HookPolicyKey, 0));
		if (!TryReadHookPolicy(ref platform, block, out var hookPolicy))
			return false;
		switch (attribute)
		{
			case ConstructHook: hookPolicy.ConstructHook = value; break;
			case DestructHook: hookPolicy.DestructHook = value; break;
			case DisplayHook: hookPolicy.DisplayHook = value; break;
			case CompareHook: hookPolicy.CompareHook = value; break;
			case MultiTestHook: hookPolicy.MultiTestHook = value; break;
			default: return false;
		}
		return WriteHookPolicy(ref platform, block, hookPolicy) &&
			SetRaw(ref platform, state, record, attribute, value, notify);
	}

	internal static bool TryGetHookPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListHookPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HookPolicyKey, 0));
		return block.IsNotNull && TryReadHookPolicy(ref platform, block,
			out value);
	}

	// Internal consumers use this typed projection instead of rereading hook
	// attributes independently. The raw fallback is needed only before List
	// construction has installed its hook policy record.
	internal static uint HookPolicyValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryGetHookPolicy(ref platform, state, obj, out var value))
		{
			switch (attribute)
			{
				case ConstructHook: return value.ConstructHook;
				case DestructHook: return value.DestructHook;
				case DisplayHook: return value.DisplayHook;
				case CompareHook: return value.CompareHook;
				case MultiTestHook: return value.MultiTestHook;
			}
		}
		return Read(ref platform, state, obj, attribute, 0);
	}

	private static bool WriteHookPolicy<TPlatform>(ref TPlatform platform,
		APTR block, MuiListHookPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (block.IsNull || value.Magic != MuiListHookPolicyState.Cookie)
			return false;
		return MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.HookPolicy, MuiListStateField.Magic,
			value.Magic) && MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiListStateRecordKind.HookPolicy,
			MuiListStateField.ConstructHook, value.ConstructHook) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.HookPolicy,
				MuiListStateField.DestructHook, value.DestructHook) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.HookPolicy,
				MuiListStateField.DisplayHook, value.DisplayHook) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.HookPolicy,
				MuiListStateField.CompareHook, value.CompareHook) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.HookPolicy,
				MuiListStateField.MultiTestHook, value.MultiTestHook);
	}

	private static bool TryReadHookPolicy<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListHookPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.HookPolicy,
			MuiListStateField.Magic, out var magic) ||
			magic != MuiListHookPolicyState.Cookie) return false;
		value.Magic = magic;
		return MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.HookPolicy, MuiListStateField.ConstructHook,
			out value.ConstructHook) && MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.HookPolicy,
			MuiListStateField.DestructHook, out value.DestructHook) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.HookPolicy,
				MuiListStateField.DisplayHook, out value.DisplayHook) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.HookPolicy,
				MuiListStateField.CompareHook, out value.CompareHook) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.HookPolicy,
				MuiListStateField.MultiTestHook, out value.MultiTestHook);
	}

	private static bool EnsureSortState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SortStateKey, 0));
		var value = default(MuiListSortState);
		value.Magic = MuiListSortState.Cookie;
		var hasSortColumn = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, SortColumn, out var sortColumn);
		var hasTitleClick = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, TitleClick, out var titleClick);
		value.SortColumn = hasSortColumn ? sortColumn : 0u;
		value.TitleClick = hasTitleClick ? titleClick : unchecked((uint)-1);

		if (block.IsNotNull && MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.SortState,
			MuiListStateField.Magic, out var magic) &&
			magic == MuiListSortState.Cookie)
		{
			if (!WriteSortState(ref platform, block, value)) return false;
		}
		else
		{
			if (block.IsNotNull && platform.IsMapped(block,
				MuiListSortState.Size))
			{
				platform.Clear(block, MuiListSortState.Size);
				platform.Free(block, MuiListSortState.Size);
			}
			block = MuiHeadlessMemory.Allocate(ref platform,
				MuiListSortState.Size);
			if (block.IsNull || !WriteSortState(ref platform, block, value))
			{
				if (block.IsNotNull)
				{
					platform.Clear(block, MuiListSortState.Size);
					platform.Free(block, MuiListSortState.Size);
				}
				return false;
			}
			SetInternal(ref platform, state, obj, SortStateKey, block.Raw);
		}
		if (hasSortColumn) SetInternal(ref platform, state, obj, SortColumn,
			value.SortColumn);
		if (hasTitleClick) SetInternal(ref platform, state, obj, TitleClick,
			value.TitleClick);
		return true;
	}

	private static bool ApplySortStateAttribute<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj,
		uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureSortState(ref platform, state, obj)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SortStateKey, 0));
		if (!TryReadSortState(ref platform, block, out var sortState))
			return false;
		if (attribute == SortColumn)
			sortState.SortColumn = NormalizeSortColumn(ref platform, state, obj,
				value);
		else if (attribute == TitleClick)
			sortState.TitleClick = value;
		else return false;
		return WriteSortState(ref platform, block, sortState) &&
			SetRaw(ref platform, state, record, attribute,
				attribute == SortColumn ? sortState.SortColumn : value, notify);
	}

	private static uint NormalizeSortColumn<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var columns = FormatColumnCount(ref platform, state, obj);
		if (columns == 0) columns = 1;
		return value >= columns ? columns - 1 : value;
	}

	private static bool SetSortColumnState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsureSortState(ref platform, state, obj)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SortStateKey, 0));
		if (!TryReadSortState(ref platform, block, out var sortState))
			return false;
		sortState.SortColumn = value;
		return WriteSortState(ref platform, block, sortState);
	}

	internal static bool TryGetSortState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListSortState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SortStateKey, 0));
		return block.IsNotNull && TryReadSortState(ref platform, block,
			out value);
	}

	private static uint SortColumnValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryGetSortState(ref platform, state, obj, out var value)
			? value.SortColumn
			: Read(ref platform, state, obj, SortColumn, 0);
	}

	private static bool WriteSortState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListSortState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (block.IsNull || value.Magic != MuiListSortState.Cookie) return false;
		return MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.SortState, MuiListStateField.Magic,
			value.Magic) && MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiListStateRecordKind.SortState,
			MuiListStateField.SortColumn, value.SortColumn) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.SortState,
				MuiListStateField.TitleClick, value.TitleClick);
	}

	private static bool TryReadSortState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListSortState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.SortState,
			MuiListStateField.Magic, out var magic) ||
			magic != MuiListSortState.Cookie) return false;
		value.Magic = magic;
		return MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.SortState, MuiListStateField.SortColumn,
			out value.SortColumn) && MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.SortState,
			MuiListStateField.TitleClick, out value.TitleClick);
	}

	private static bool WriteClickState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListClickState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (block.IsNull || value.Magic != MuiListClickState.Cookie)
			return false;
		return MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ClickState, MuiListStateField.Magic,
			value.Magic) && MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiListStateRecordKind.ClickState,
			MuiListStateField.ClickColumn, value.ClickColumn) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.ClickState,
				MuiListStateField.DoubleClick, value.DoubleClick == 0 ? 0u : 1u) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.ClickState,
				MuiListStateField.AgainClick, value.AgainClick == 0 ? 0u : 1u) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.ClickState, MuiListStateField.Clicks,
				value.Clicks) && MuiListStateFieldCursorCodec.TryWriteUInt32(
				ref platform, block, MuiListStateRecordKind.ClickState,
				MuiListStateField.DefClickColumn, value.DefClickColumn);
	}

	private static bool TryReadClickState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListClickState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.ClickState,
			MuiListStateField.Magic, out var magic) ||
			magic != MuiListClickState.Cookie) return false;
		value.Magic = magic;
		return MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.ClickState, MuiListStateField.ClickColumn,
			out value.ClickColumn) && MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.ClickState,
			MuiListStateField.DoubleClick, out value.DoubleClick) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ClickState,
				MuiListStateField.AgainClick, out value.AgainClick) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ClickState, MuiListStateField.Clicks,
				out value.Clicks) && MuiListStateFieldCursorCodec.TryReadUInt32(
				ref platform, block, MuiListStateRecordKind.ClickState,
				MuiListStateField.DefClickColumn, out value.DefClickColumn);
	}

	internal static bool TryGetPoolPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListPoolPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			PoolPolicyKey, 0));
		return MuiListPoolPolicyStateCodec.TryRead(ref platform, block, out value);
	}

	private static APTR PoolFor<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryGetPoolPolicy(ref platform, state, obj, out var policy))
			return policy.Pool;
		return APTR.FromPointer(Read(ref platform, state, obj, Pool, 0));
	}

	// The format pointer remains caller-owned, as in the public MUI attribute;
	// only its bounded column count is derived into guest state. Empty and NULL
	// formats are the documented single-column default.
	private static void NormalizeFormat<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var maximum = MaxColumnsCursor(ref platform, state, obj);
		var format = FormatValueCursor(ref platform, state, obj);
		if (format.IsNotNull && !TryReadCStringLength(ref platform, format,
			MaximumStringLength, out _))
		{
			format = APTR.Null;
			SetRaw(ref platform, state, record, Format, 0, false);
		}
		if (!InstallFormatDescriptors(ref platform, state, record, obj, format,
			maximum, false, false))
		{
			SetRaw(ref platform, state, record, FormatDescriptorKey, 0, false);
			SetRaw(ref platform, state, record, FormatColumnsKey, 1, false);
			SetFormatPolicyState(ref platform, state, obj, format, maximum, 1);
		}
		ApplySortColumn(ref platform, state, record, obj,
			SortColumnValue(ref platform, state, obj), false);
	}

	// Construction tags arrive as a caller-owned STRPTR* array. MorphOS copies
	// the pointer table privately while keeping the pointed-to strings external;
	// invalid or unterminated input is ignored rather than leaving a dangling
	// private record behind.
	private static void NormalizeTitleArray<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var source = APTR.FromPointer(Read(ref platform, state, obj,
			TitleArray, 0));
		if (!ApplyTitleArray(ref platform, state, record, obj, source, false))
			SetRaw(ref platform, state, record, TitleArray, 0, false);
	}

	private static bool ApplyTitle<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, APTR obj, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!SetRaw(ref platform, state, record, Title, value, notify))
			return false;
		SetTitleState(ref platform, state, obj, value);
		return true;
	}

	private static bool ApplyTitleArray<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, APTR source, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var fresh = APTR.Null;
		if (source.IsNotNull)
		{
			fresh = BuildTitleArrayState(ref platform, source);
			if (fresh.IsNull) return false;
		}
		var raw = 0u;
		if (fresh.IsNotNull && TryReadTitleArrayStateBlock(ref platform, fresh,
			out var stateValue)) raw = stateValue.Pointers.Raw;
		if (!SetRaw(ref platform, state, record, TitleArray, raw, notify))
		{
			FreeTitleArrayState(ref platform, fresh);
			return false;
		}
		var old = APTR.FromPointer(Read(ref platform, state, obj,
			TitleArrayStateKey, 0));
		SetInternal(ref platform, state, obj, TitleArrayStateKey, fresh.Raw);
		FreeTitleArrayState(ref platform, old);
		return true;
	}

	private static void NormalizeColumnVisibility<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hide = Read(ref platform, state, obj, HideColumn, uint.MaxValue);
		if (hide < MaximumGeometryColumns)
			ApplyColumnVisibility(ref platform, state, record, obj, hide, true,
				false);
		var show = Read(ref platform, state, obj, ShowColumn, uint.MaxValue);
		if (show < MaximumGeometryColumns)
			ApplyColumnVisibility(ref platform, state, record, obj, show, false,
				false);
	}

	private static bool ApplyColumnVisibility<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, uint column, bool hide, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (column >= MaximumGeometryColumns) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnVisibilityKey, 0));
		var fresh = false;
		if (!TryReadColumnVisibilityState(ref platform, block,
			out var value))
		{
			block = MuiHeadlessMemory.Allocate(ref platform,
				MuiListColumnVisibilityState.Size);
			if (block.IsNull) return false;
			value = default;
			value.Magic = ColumnVisibilityCookie;
			fresh = true;
		}
		var previous = value;
		var mask = default(MuiListHiddenColumns);
		mask.Low = value.Low;
		mask.High = value.High;
		mask.Word2 = value.Word2;
		mask.Word3 = value.Word3;
		mask.Word4 = value.Word4;
		mask.Word5 = value.Word5;
		mask.Word6 = value.Word6;
		mask.Word7 = value.Word7;
		var wasHidden = IsHidden(mask, column);
		if (hide) Hide(ref mask, column);
		else Unhide(ref mask, column);
		value.Low = mask.Low;
		value.High = mask.High;
		value.Word2 = mask.Word2;
		value.Word3 = mask.Word3;
		value.Word4 = mask.Word4;
		value.Word5 = mask.Word5;
		value.Word6 = mask.Word6;
		value.Word7 = mask.Word7;
		var changed = wasHidden != hide;
		WriteColumnVisibilityState(ref platform, block, value);
		if (fresh) SetInternal(ref platform, state, obj,
			ColumnVisibilityKey, block.Raw);
		if (!SetRaw(ref platform, state, record,
			hide ? HideColumn : ShowColumn, column, notify))
		{
			WriteColumnVisibilityState(ref platform, block, previous);
			if (fresh)
			{
				SetInternal(ref platform, state, obj, ColumnVisibilityKey, 0);
				platform.Clear(block, MuiListColumnVisibilityState.Size);
				platform.Free(block, MuiListColumnVisibilityState.Size);
			}
			return false;
		}
		if (!changed) return true;
		FreeColumnLayout(ref platform, state, obj);
		FreeColumnMetrics(ref platform, state, obj);
		RequestMutationRedraw(ref platform, state, obj);
		return true;
	}

	private static void NormalizeColumnOrder<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var source = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnOrder, 0));
		if (!ApplyColumnOrder(ref platform, state, record, obj, source, false))
			SetRaw(ref platform, state, record, ColumnOrder, 0, false);
	}

	private static bool ApplyColumnOrder<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, APTR source, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var old = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnOrderKey, 0));
		var fresh = source.IsNotNull
			? BuildColumnOrderState(ref platform, state, obj, source)
			: APTR.Null;
		if (source.IsNotNull && fresh.IsNull) return false;
		var changed = !ColumnOrderStatesEqual(ref platform, old, fresh);

		var raw = 0u;
		if (fresh.IsNotNull && TryReadColumnOrderState(ref platform, fresh,
			out var freshValue)) raw = freshValue.Values.Raw;
		if (!SetRaw(ref platform, state, record, ColumnOrder, raw, notify))
		{
			FreeColumnOrderState(ref platform, fresh);
			return false;
		}
		SetInternal(ref platform, state, obj, ColumnOrderKey, fresh.Raw);
		FreeColumnOrderState(ref platform, old);
		if (!changed) return true;
		FreeColumnLayout(ref platform, state, obj);
		FreeColumnMetrics(ref platform, state, obj);
		RequestMutationRedraw(ref platform, state, obj);
		return true;
	}

	private static bool ColumnOrderStatesEqual<TPlatform>(
		ref TPlatform platform, APTR left, APTR right)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (left.Raw == right.Raw) return true;
		var leftValid = TryReadColumnOrderState(ref platform, left,
			out var leftValue);
		var rightValid = TryReadColumnOrderState(ref platform, right,
			out var rightValue);
		if (!leftValid || !rightValid) return leftValid == rightValid;
		if (leftValue.Count != rightValue.Count) return false;
		var leftCursor = default(MuiListColumnOrderByteCursor);
		leftCursor.Base = leftValue.Values;
		var rightCursor = default(MuiListColumnOrderByteCursor);
		rightCursor.Base = rightValue.Values;
		for (var index = 0u; index < leftValue.Count; index++)
		{
			leftCursor.Index = index;
			rightCursor.Index = index;
			if (!MuiListColumnOrderByteCursorCodec.TryGetEntry(ref platform,
				leftCursor, out var leftAddress) ||
				!MuiListColumnOrderByteCursorCodec.TryGetEntry(ref platform,
					rightCursor, out var rightAddress) ||
				platform.ReadUInt8(leftAddress, 0) != platform.ReadUInt8(rightAddress,
					0))
				return false;
		}
		return true;
	}

	// Copy a bounded BYTE* permutation into guest-owned storage. MorphOS uses a
	// 0xff byte as the natural end marker; a complete un-terminated permutation
	// is also accepted when exactly all derived columns are supplied. Missing
	// trailing columns are filled in identity order, while duplicate/out-of-range
	// columns fail atomically.
	private static bool PopulateColumnOrderValues<TPlatform>(
		ref TPlatform platform, APTR values, APTR source, uint columns)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (source.IsNull) return false;
		var seen = default(MuiListHiddenColumns);
		var sourceCursor = default(MuiListColumnOrderByteCursor);
		sourceCursor.Base = source;
		var copied = 0u;
		for (var index = 0u; index < columns; index++)
		{
			sourceCursor.Index = index;
			if (!MuiListColumnOrderByteCursorCodec.TryGetEntry(ref platform,
				sourceCursor, out var slot)) return false;
			var value = platform.ReadUInt8(slot, 0);
			if (value == 0xFF) break;
			if (value >= columns || IsHidden(seen, value)) return false;
			Hide(ref seen, value);
			WriteColumnOrderByte(ref platform, values, copied, value);
			copied++;
		}
		for (var value = 0u; copied < columns && value < columns; value++)
			if (!IsHidden(seen, value))
			{
				Hide(ref seen, value);
				WriteColumnOrderByte(ref platform, values, copied,
					unchecked((byte)value));
				copied++;
			}
		return copied == columns;
	}

	private static APTR BuildColumnOrderState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var columns = GeometryColumnCount(ref platform, state, obj);
		if (columns == 0 || columns > MaximumGeometryColumns) return APTR.Null;
		var valueBytes = ColumnOrderValueBytes(columns);
		var values = MuiHeadlessMemory.Allocate(ref platform, valueBytes);
		if (values.IsNull) return APTR.Null;
		ClearColumnOrderBytes(ref platform, values, valueBytes);
		if (!PopulateColumnOrderValues(ref platform, values, source, columns))
		{
			ClearColumnOrderBytes(ref platform, values, valueBytes);
			platform.Free(values, valueBytes);
			return APTR.Null;
		}

		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListColumnOrderState.Size);
		if (block.IsNull)
		{
			ClearColumnOrderBytes(ref platform, values, valueBytes);
			platform.Free(values, valueBytes);
			return APTR.Null;
		}
		var valueState = default(MuiListColumnOrderState);
		valueState.Magic = ColumnOrderCookie;
		valueState.Count = columns;
		valueState.Values = values;
		valueState.Reserved = valueBytes;
		WriteColumnOrderState(ref platform, block, valueState);
		return block;
	}

	private static bool ApplyFont<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, APTR obj, APTR font, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!SetRaw(ref platform, state, record, Font, font.Raw, notify))
			return false;
		SetFontState(ref platform, state, obj, font);
		FreeColumnLayout(ref platform, state, obj);
		FreeColumnMetrics(ref platform, state, obj);
		RefreshLineHeight(ref platform, state, obj);
		RequestMutationRedraw(ref platform, state, obj);
		return true;
	}

	private static bool ApplyFormat<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, APTR obj, APTR format, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (format.IsNotNull && !TryReadCStringLength(ref platform, format,
			MaximumStringLength, out _)) return false;
		var maximum = MaxColumnsCursor(ref platform, state, obj);
		if (!InstallFormatDescriptors(ref platform, state, record, obj, format,
			maximum, notify, false)) return false;
		if (!SetRaw(ref platform, state, record, Format, format.Raw, notify))
			return false;
		return ApplySortColumn(ref platform, state, record, obj,
			SortColumnValue(ref platform, state, obj), notify);
	}

	private static bool ApplyMaxColumns<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var maximum = NormalizeColumnLimit(value);
		var format = FormatValueCursor(ref platform, state, obj);
		if (!InstallFormatDescriptors(ref platform, state, record, obj, format,
			maximum, false, notify)) return false;
		return ApplySortColumn(ref platform, state, record, obj,
			SortColumnValue(ref platform, state, obj), notify);
	}

	// SortColumn is a named FORMAT-derived state value. Keep it inside the
	// currently installed descriptor range so StringArray and custom compare
	// hooks never receive a column that the List cannot display.
	private static bool ApplySortColumn<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var normalized = NormalizeSortColumn(ref platform, state, obj, value);
		if (!SetRaw(ref platform, state, record, SortColumn, normalized,
			notify)) return false;
		return SetSortColumnState(ref platform, state, obj, normalized);
	}

	// MorphOS Quiet suppresses intermediate refreshes and releases one
	// coalesced refresh when it is cleared. The pending bit and request count
	// live in the named guest redraw record rather than in private offsets.
	private static bool ApplyQuiet<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var wasQuiet = PresentationPolicyValue(ref platform, state, obj,
			Quiet, 0) != 0;
		var nowQuiet = value != 0;
		if (!ApplyPresentationPolicyAttribute(ref platform, state, record, obj,
			Quiet, nowQuiet ? 1u : 0u, notify)) return false;
		if (!wasQuiet || nowQuiet) return true;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			RedrawStateKey, 0));
		if (!TryReadRedrawState(ref platform, block, out var redraw) ||
			redraw.Dirty == 0) return true;
		redraw.Dirty = 0;
		redraw.Requests = SaturatingAdd(redraw.Requests, 1);
		WriteRedrawState(ref platform, block, redraw);
		return true;
	}

	private static uint NormalizePolicyBool(uint value) => value == 0 ? 0u : 1u;

	private static uint NormalizePolicyDragType(uint value) =>
		value == DragTypeImmediate ? DragTypeImmediate : DragTypeNone;

	private static uint NormalizePolicyMinLineHeight(uint value)
	{
		if (value < RowHeight) return RowHeight;
		return value > MaximumLineHeight ? MaximumLineHeight : value;
	}

	private static bool EnsurePresentationPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			PresentationPolicyKey, 0));
		var value = default(MuiListPresentationPolicyState);
		value.Magic = MuiListPresentationPolicyState.Cookie;
		var hasEditable = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, Editable, out var editable);
		var hasQuiet = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, Quiet, out var quiet);
		var hasAdjustHeight = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, AdjustHeight, out var adjustHeight);
		var hasAdjustWidth = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, AdjustWidth, out var adjustWidth);
		var hasStripes = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, Stripes, out var stripes);
		var hasShowDropMarks = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, ShowDropMarks, out var showDropMarks);
		var hasDragSortable = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, DragSortable, out var dragSortable);
		var hasDragType = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, DragType, out var dragType);
		var hasAutoVisible = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, AutoVisible, out var autoVisible);
		var hasAutoLineHeight = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, AutoLineHeight, out var autoLineHeight);
		var hasMinLineHeight = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, MinLineHeight, out var minLineHeight);
		value.Editable = hasEditable ? NormalizePolicyBool(editable) : 0u;
		value.Quiet = hasQuiet ? NormalizePolicyBool(quiet) : 0u;
		value.AdjustHeight = hasAdjustHeight
			? NormalizePolicyBool(adjustHeight) : 0u;
		value.AdjustWidth = hasAdjustWidth ? NormalizePolicyBool(adjustWidth) : 0u;
		value.Stripes = hasStripes ? NormalizePolicyBool(stripes) : 0u;
		value.ShowDropMarks = hasShowDropMarks
			? NormalizePolicyBool(showDropMarks) : 1u;
		value.DragSortable = hasDragSortable
			? NormalizePolicyBool(dragSortable) : 0u;
		value.DragType = hasDragType
			? NormalizePolicyDragType(dragType) : DragTypeNone;
		value.AutoVisible = hasAutoVisible
			? NormalizePolicyBool(autoVisible) : 0u;
		value.AutoLineHeight = hasAutoLineHeight
			? NormalizePolicyBool(autoLineHeight) : 0u;
		value.MinLineHeight = hasMinLineHeight
			? NormalizePolicyMinLineHeight(minLineHeight) : RowHeight;

		if (block.IsNotNull && TryReadPresentationPolicy(ref platform, block,
			out var current) && current.Magic == value.Magic)
		{
			if (!WritePresentationPolicy(ref platform, block, value)) return false;
		}
		else
		{
			if (block.IsNotNull && platform.IsMapped(block,
				MuiListPresentationPolicyState.Size))
			{
				platform.Clear(block, MuiListPresentationPolicyState.Size);
				platform.Free(block, MuiListPresentationPolicyState.Size);
			}
			block = MuiHeadlessMemory.Allocate(ref platform,
				MuiListPresentationPolicyState.Size);
			if (block.IsNull || !WritePresentationPolicy(ref platform, block,
				value))
			{
				if (block.IsNotNull)
				{
					platform.Clear(block, MuiListPresentationPolicyState.Size);
					platform.Free(block, MuiListPresentationPolicyState.Size);
				}
				return false;
			}
			SetInternal(ref platform, state, obj, PresentationPolicyKey,
				block.Raw);
		}
		if (hasEditable) SetInternal(ref platform, state, obj, Editable,
			value.Editable);
		if (hasQuiet) SetInternal(ref platform, state, obj, Quiet, value.Quiet);
		if (hasAdjustHeight) SetInternal(ref platform, state, obj, AdjustHeight,
			value.AdjustHeight);
		if (hasAdjustWidth) SetInternal(ref platform, state, obj, AdjustWidth,
			value.AdjustWidth);
		if (hasStripes) SetInternal(ref platform, state, obj, Stripes,
			value.Stripes);
		if (hasShowDropMarks) SetInternal(ref platform, state, obj,
			ShowDropMarks, value.ShowDropMarks);
		if (hasDragSortable) SetInternal(ref platform, state, obj, DragSortable,
			value.DragSortable);
		if (hasDragType) SetInternal(ref platform, state, obj, DragType,
			value.DragType);
		if (hasAutoVisible) SetInternal(ref platform, state, obj, AutoVisible,
			value.AutoVisible);
		if (hasAutoLineHeight) SetInternal(ref platform, state, obj,
			AutoLineHeight, value.AutoLineHeight);
		if (hasMinLineHeight) SetInternal(ref platform, state, obj,
			MinLineHeight, value.MinLineHeight);
		return true;
	}

	private static bool ApplyPresentationPolicyAttribute<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj,
		uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!EnsurePresentationPolicy(ref platform, state, obj)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			PresentationPolicyKey, 0));
		if (!TryReadPresentationPolicy(ref platform, block, out var policy))
			return false;
		var normalized = value;
		switch (attribute)
		{
			case Editable:
				policy.Editable = normalized = NormalizePolicyBool(value);
				break;
			case Quiet:
				policy.Quiet = normalized = NormalizePolicyBool(value);
				break;
			case AdjustHeight:
				policy.AdjustHeight = normalized = NormalizePolicyBool(value);
				break;
			case AdjustWidth:
				policy.AdjustWidth = normalized = NormalizePolicyBool(value);
				break;
			case Stripes:
				policy.Stripes = normalized = NormalizePolicyBool(value);
				break;
			case ShowDropMarks:
				policy.ShowDropMarks = normalized = NormalizePolicyBool(value);
				break;
			case DragSortable:
				policy.DragSortable = normalized = NormalizePolicyBool(value);
				break;
			case DragType:
				policy.DragType = normalized = NormalizePolicyDragType(value);
				break;
			case AutoVisible:
				policy.AutoVisible = normalized = NormalizePolicyBool(value);
				break;
			case AutoLineHeight:
				policy.AutoLineHeight = normalized = NormalizePolicyBool(value);
				break;
			case MinLineHeight:
				policy.MinLineHeight = normalized = NormalizePolicyMinLineHeight(value);
				break;
			default:
				return false;
		}
		if (!WritePresentationPolicy(ref platform, block, policy) ||
			!SetRaw(ref platform, state, record, attribute, normalized, notify))
			return false;
		return attribute != AutoLineHeight ||
			RefreshLineHeight(ref platform, state, obj);
	}

	internal static bool TryGetPresentationPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListPresentationPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			PresentationPolicyKey, 0));
		return block.IsNotNull && TryReadPresentationPolicy(ref platform, block,
			out value);
	}

	private static uint PresentationPolicyValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint attribute,
		uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryGetPresentationPolicy(ref platform, state, obj, out var policy))
		{
			switch (attribute)
			{
				case Editable: return policy.Editable;
				case Quiet: return policy.Quiet;
				case AdjustHeight: return policy.AdjustHeight;
				case AdjustWidth: return policy.AdjustWidth;
				case Stripes: return policy.Stripes;
				case ShowDropMarks: return policy.ShowDropMarks;
				case DragSortable: return policy.DragSortable;
				case DragType: return policy.DragType;
				case AutoVisible: return policy.AutoVisible;
				case AutoLineHeight: return policy.AutoLineHeight;
				case MinLineHeight: return policy.MinLineHeight;
			}
		}
		return Read(ref platform, state, obj, attribute, fallback);
	}

	private static bool WritePresentationPolicy<TPlatform>(ref TPlatform platform,
		APTR block, MuiListPresentationPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (block.IsNull || value.Magic != MuiListPresentationPolicyState.Cookie)
			return false;
		return MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.PresentationPolicy, MuiListStateField.Magic,
			value.Magic) && MuiListStateFieldCursorCodec.TryWriteUInt32(
			ref platform, block, MuiListStateRecordKind.PresentationPolicy,
			MuiListStateField.Editable, value.Editable) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.Quiet, value.Quiet) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.AdjustHeight, value.AdjustHeight) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.AdjustWidth, value.AdjustWidth) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.Stripes, value.Stripes) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.ShowDropMarks, value.ShowDropMarks) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.DragSortable, value.DragSortable) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.DragType, value.DragType) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.AutoVisible, value.AutoVisible) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.AutoLineHeight, value.AutoLineHeight) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.MinLineHeight, value.MinLineHeight);
	}

	private static bool TryReadPresentationPolicy<TPlatform>(
		ref TPlatform platform, APTR block,
		out MuiListPresentationPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block,
			MuiListPresentationPolicyState.Size)) return false;
		if (!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.PresentationPolicy, MuiListStateField.Magic,
			out var magic) || magic != MuiListPresentationPolicyState.Cookie)
			return false;
		value.Magic = magic;
		return MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.PresentationPolicy, MuiListStateField.Editable,
			out value.Editable) && MuiListStateFieldCursorCodec.TryReadUInt32(
			ref platform, block, MuiListStateRecordKind.PresentationPolicy,
			MuiListStateField.Quiet, out value.Quiet) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.AdjustHeight, out value.AdjustHeight) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.AdjustWidth, out value.AdjustWidth) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.Stripes, out value.Stripes) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.ShowDropMarks, out value.ShowDropMarks) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.DragSortable, out value.DragSortable) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.DragType, out value.DragType) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.AutoVisible, out value.AutoVisible) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.AutoLineHeight, out value.AutoLineHeight) &&
			MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.PresentationPolicy,
				MuiListStateField.MinLineHeight, out value.MinLineHeight);
	}

	private static uint NormalizeColumnLimit(uint value) =>
		value == 0 ? 1u : value > MaximumColumns ? MaximumColumns : value;

	private static bool TryCountFormatColumns<TPlatform>(ref TPlatform platform,
		APTR format, uint maximum, out uint columns)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		columns = 1;
		if (format.IsNull) return true;
		if (!TryReadCStringLength(ref platform, format,
			MaximumStringLength, out var length)) return false;
		var scan = default(MuiListFormatScanState);
		for (var i = 0u; i < length; i++)
		{
			var value = platform.ReadUInt8(format, (int)i);
			if (scan.Quoted != 0)
			{
				// DOS ReadItem treats '*' specially only in a quoted item. The
				// escaped byte is data, including an escaped quote or comma.
				if (value == (byte)'*')
				{
					if (i + 1 >= length) return false;
					i++;
					continue;
				}
				if (value == (byte)'"') scan.Quoted = 0;
				continue;
			}
			if (value == (byte)',')
			{
				if (columns < maximum) columns++;
				scan.InToken = 0;
				scan.EqualSeen = 0;
				continue;
			}
			if (IsSpace(value))
			{
				scan.InToken = 0;
				scan.EqualSeen = 0;
				continue;
			}
			if (scan.InToken == 0) scan.InToken = 1;
			if (value == (byte)'=')
			{
				scan.EqualSeen = 1;
				continue;
			}
			// ReadArgs accepts both KEY=VALUE and KEY VALUE.  Quoted
			// values therefore open a quoted item regardless of whether an
			// equals sign preceded them; the bounded splitter only needs to
			// preserve commas until the closing quote is seen.
			if (value == (byte)'"')
				scan.Quoted = 1;
		}
		return scan.Quoted == 0;
	}

	private static bool InstallFormatDescriptors<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, APTR format, uint maximum,
		bool notifyFormat, bool notifyMaximum)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryCountFormatColumns(ref platform, format, maximum,
			out var count)) return false;
		var block = BuildFormatDescriptors(ref platform, format, count);
		if (block.IsNull) return false;
		var old = APTR.FromPointer(Read(ref platform, state, obj,
			FormatDescriptorKey, 0));
		var oldCount = FormatColumnsCursor(ref platform, state, obj);
		// Format/MaxColumns changes invalidate any geometry published by the
		// previous Layout pass; retire it before replacing the descriptors.
		FreeColumnLayout(ref platform, state, obj);
		FreeColumnMetrics(ref platform, state, obj);
		if (!SetRaw(ref platform, state, record, MaxColumns, maximum,
			notifyMaximum) ||
			!SetRaw(ref platform, state, record, FormatDescriptorKey, block.Raw,
				false) ||
			!SetRaw(ref platform, state, record, FormatColumnsKey, count, false))
		{
			FreeFormatDescriptors(ref platform, block, count);
			return false;
		}
		SetFormatPolicyState(ref platform, state, obj, format, maximum, count);
		FreeFormatDescriptors(ref platform, old, oldCount);
		return true;
	}

	private static APTR BuildFormatDescriptors<TPlatform>(ref TPlatform platform,
		APTR format, uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var safeCount = count == 0 ? 1u : count;
		var block = MuiHeadlessMemory.Allocate(ref platform,
			safeCount * FormatDescriptorSize);
		if (block.IsNull) return APTR.Null;
		var descriptorCursor = default(MuiListFormatDescriptorCursor);
		descriptorCursor.Base = block;
		for (var i = 0u; i < safeCount; i++)
		{
			descriptorCursor.Index = i;
			if (!MuiListFormatDescriptorCursorCodec.TryGetEntry(ref platform,
				descriptorCursor, out var descriptor))
			{
				FreeFormatDescriptors(ref platform, block, safeCount);
				return APTR.Null;
			}
			var value = default(MuiListFormatDescriptor);
			value.Delta = 4;
			value.Weight = 100;
			value.MinWidth = unchecked((uint)-1);
			value.MaxWidth = unchecked((uint)-1);
			value.Column = i;
			WriteFormatDescriptor(ref platform, descriptor, ref value);
		}
		if (format.IsNull) return block;
		if (!TryReadCStringLength(ref platform, format,
			MaximumStringLength, out var length))
		{
			FreeFormatDescriptors(ref platform, block, safeCount);
			return APTR.Null;
		}
		var start = 0;
		var ordinal = 0u;
		var scan = default(MuiListFormatScanState);
		for (var i = 0u; i <= length && ordinal < safeCount; i++)
		{
			var separator = i == length;
			if (!separator)
			{
				var separatorByte = platform.ReadUInt8(format, (int)i);
				if (scan.Quoted != 0)
				{
					if (separatorByte == (byte)'*')
					{
						if (i + 1 >= length)
						{
							FreeFormatDescriptors(ref platform, block, safeCount);
							return APTR.Null;
						}
						i++;
						continue;
					}
					if (separatorByte == (byte)'"') scan.Quoted = 0;
				}
				else if (separatorByte == (byte)',')
				{
					separator = true;
					scan.InToken = 0;
					scan.EqualSeen = 0;
				}
				else if (IsSpace(separatorByte))
				{
					scan.InToken = 0;
					scan.EqualSeen = 0;
				}
				else
				{
					if (scan.InToken == 0) scan.InToken = 1;
					if (separatorByte == (byte)'=') scan.EqualSeen = 1;
					else if (separatorByte == (byte)'"') scan.Quoted = 1;
				}
			}
			if (!separator) continue;
			descriptorCursor.Index = ordinal;
			if (!MuiListFormatDescriptorCursorCodec.TryGetEntry(ref platform,
				descriptorCursor, out var descriptor))
			{
				FreeFormatDescriptors(ref platform, block, safeCount);
				return APTR.Null;
			}
			var value = default(MuiListFormatDescriptor);
			ReadFormatDescriptor(ref platform, descriptor, out value);
			if (!ParseFormatSegment(ref platform, format, start, (int)i,
				ref value, ordinal))
			{
				ReleaseFormatDescriptorValue(ref platform, ref value);
				FreeFormatDescriptors(ref platform, block, safeCount);
				return APTR.Null;
			}
			WriteFormatDescriptor(ref platform, descriptor, ref value);
			ordinal++;
			start = (int)i + 1;
		}
		if (scan.Quoted != 0)
		{
			FreeFormatDescriptors(ref platform, block, safeCount);
			return APTR.Null;
		}
		if (!ValidateFormatDescriptors(ref platform, block, safeCount))
		{
			FreeFormatDescriptors(ref platform, block, safeCount);
			return APTR.Null;
		}
		return block;
	}

	// MorphOS ReadArgs FORMAT entries may remap display columns with COL, but
	// two visible entries may not claim the same source column. Keep this rule
	// on the named descriptor records so replacement remains failure-atomic and
	// the layout path never has to infer ownership from raw wire offsets.
	private static bool ValidateFormatDescriptors<TPlatform>(ref TPlatform platform,
		APTR block, uint count)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListFormatDescriptorCursor);
		cursor.Base = block;
		for (var current = 0u; current < count; current++)
		{
			cursor.Index = current;
			if (!MuiListFormatDescriptorCursorCodec.TryGetEntry(ref platform,
				cursor, out var currentAddress)) return false;
			ReadFormatDescriptor(ref platform, currentAddress,
				out var currentDescriptor);
			for (var previous = 0u; previous < current; previous++)
			{
				cursor.Index = previous;
				if (!MuiListFormatDescriptorCursorCodec.TryGetEntry(ref platform,
					cursor, out var previousAddress)) return false;
				ReadFormatDescriptor(ref platform, previousAddress,
					out var previousDescriptor);
				if (previousDescriptor.Column == currentDescriptor.Column)
					return false;
			}
		}
		return true;
	}

	private static bool ParseFormatSegment<TPlatform>(ref TPlatform platform,
		APTR format, int start, int end, ref MuiListFormatDescriptor descriptor,
		uint ordinal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cursor = start;
		while (cursor < end)
		{
			while (cursor < end && IsSpace(platform.ReadUInt8(format, cursor)))
				cursor++;
			if (cursor >= end) break;
			var keyEnd = cursor;
			while (keyEnd < end && !IsSpace(platform.ReadUInt8(format, keyEnd)) &&
				platform.ReadUInt8(format, keyEnd) != (byte)'=') keyEnd++;
			if (keyEnd == cursor) return false;
			var afterKey = keyEnd;
			while (afterKey < end && IsSpace(platform.ReadUInt8(format, afterKey)))
				afterKey++;
			var hasValue = false;
			var valueStart = afterKey;
			var valueEnd = afterKey;
			var quotedValue = false;
			if (afterKey < end && platform.ReadUInt8(format, afterKey) ==
				(byte)'=')
			{
				hasValue = true;
				valueStart = afterKey + 1;
				while (valueStart < end &&
					IsSpace(platform.ReadUInt8(format, valueStart))) valueStart++;
				if (!TryReadFormatValueRange(ref platform, format, valueStart,
					end, out valueEnd, out quotedValue)) return false;
			}
			else if (!IsFormatSwitch(ref platform, format, cursor, keyEnd))
			{
				// ReadArgs keyword arguments may be written as KEY VALUE as
				// well as KEY=VALUE.  Consume exactly one following item; a
				// quoted item may contain spaces and commas.
				if (!TryReadFormatValueRange(ref platform, format, afterKey,
					end, out valueEnd, out quotedValue)) return false;
				hasValue = true;
				valueStart = afterKey;
			}
			var nextCursor = hasValue ? valueEnd : keyEnd;
			var valueDataStart = valueStart;
			var valueDataEnd = valueEnd;
			if (quotedValue)
			{
				valueDataStart++;
				valueDataEnd--;
			}
			var value = default(MuiListFormatValue);
			value.Start = valueDataStart;
			value.End = valueDataEnd;
			value.Quoted = quotedValue ? (byte)1 : (byte)0;
			if (!TryPrepareFormatValue(ref platform, format, ref value))
				return false;
			if (IsColumn(ref platform, format, cursor, keyEnd))
			{
				if (!hasValue || !TryParseNumber(ref platform, format,
					ref value, out var column) || column < 0)
					return false;
				descriptor.Column = unchecked((uint)column);
			}
			else if (IsDelta(ref platform, format, cursor, keyEnd))
			{
				if (!hasValue || !TryParseNumber(ref platform, format,
					ref value, out var delta) || delta < 0)
					return false;
				descriptor.Delta = unchecked((uint)delta);
			}
			else if (IsWeight(ref platform, format, cursor, keyEnd))
			{
				if (!hasValue || !TryParseNumber(ref platform, format,
					ref value, out var weight) || weight < -1)
					return false;
				descriptor.Weight = unchecked((uint)weight);
				if (weight == -1)
					SetDescriptorFlag(ref descriptor, DescriptorWeightContent);
				else
					ClearDescriptorFlag(ref descriptor, DescriptorWeightContent);
			}
			else if (IsMinWidth(ref platform, format, cursor, keyEnd))
			{
				if (!hasValue || !WriteWidth(ref platform, format, ref value,
					ref descriptor, MuiListFormatField.MinWidth,
					DescriptorMinPixel)) return false;
			}
			else if (IsMaxWidth(ref platform, format, cursor, keyEnd))
			{
				if (!hasValue || !WriteWidth(ref platform, format, ref value,
					ref descriptor, MuiListFormatField.MaxWidth,
					DescriptorMaxPixel)) return false;
			}
			else if (IsBar(ref platform, format, cursor, keyEnd))
			{
				if (hasValue) return false;
				SetDescriptorFlag(ref descriptor, DescriptorBar);
			}
			else if (IsSortable(ref platform, format, cursor, keyEnd))
			{
				if (hasValue) return false;
				SetDescriptorFlag(ref descriptor, DescriptorSortable);
			}
			else if (IsOrder(ref platform, format, cursor, keyEnd))
			{
				if (!hasValue) return false;
				if (IsDescendingValue(ref platform, format, ref value))
					SetDescriptorFlag(ref descriptor, DescriptorDescending);
				else if (IsAscendingValue(ref platform, format, ref value))
					ClearDescriptorFlag(ref descriptor, DescriptorDescending);
				else return false;
			}
			else if (IsPreparse(ref platform, format, cursor, keyEnd))
			{
				if (!hasValue || value.Start >= value.End) return false;
				if (!InstallPreparseValue(ref platform, format, ref value,
					ref descriptor)) return false;
			}
			else return false;
			cursor = nextCursor;
		}
		return true;
	}

	private static bool IsFormatSwitch<TPlatform>(ref TPlatform platform,
		APTR text, int start, int end)
		where TPlatform : struct, IMuiGuestMemory =>
		IsBar(ref platform, text, start, end) ||
		IsSortable(ref platform, text, start, end);

	// Reads one bounded ReadArgs item beginning at start.  The returned range
	// includes quote delimiters; the caller records Quoted so the decoder can
	// apply CopperStart's quoted-star escape rules.  No managed text is created.
	private static bool TryReadFormatValueRange<TPlatform>(ref TPlatform platform,
		APTR text, int start, int end, out int valueEnd, out bool quoted)
		where TPlatform : struct, IMuiGuestMemory
	{
		valueEnd = start;
		quoted = false;
		if (start >= end) return false;
		var first = platform.ReadUInt8(text, start);
		if (first == (byte)'"')
		{
			quoted = true;
			var cursor = start + 1;
			while (cursor < end)
			{
				var value = platform.ReadUInt8(text, cursor++);
				if (value == (byte)'*')
				{
					if (cursor >= end) return false;
					cursor++;
					continue;
				}
				if (value != (byte)'"') continue;
				if (cursor < end && !IsSpace(platform.ReadUInt8(text, cursor)))
					return false;
				valueEnd = cursor;
				return true;
			}
			return false;
		}
		var unquoted = start;
		while (unquoted < end && !IsSpace(platform.ReadUInt8(text, unquoted)))
		{
			if (platform.ReadUInt8(text, unquoted) == (byte)'"') return false;
			unquoted++;
		}
		if (unquoted == start) return false;
		valueEnd = unquoted;
		return true;
	}

	private static bool IsSpace(byte value) => value == (byte)' ' ||
		value == (byte)'\t';

	private static bool IsColumn<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end) where TPlatform : struct, IMuiGuestMemory =>
		IsToken3(ref platform, text, start, end, (byte)'C', (byte)'O', (byte)'L') ||
		IsToken1(ref platform, text, start, end, (byte)'C');

	private static bool IsDelta<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end) where TPlatform : struct, IMuiGuestMemory =>
		IsToken5(ref platform, text, start, end, (byte)'D', (byte)'E',
			(byte)'L', (byte)'T', (byte)'A') ||
		IsToken1(ref platform, text, start, end, (byte)'D');

	private static bool IsWeight<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end) where TPlatform : struct, IMuiGuestMemory =>
		IsToken6(ref platform, text, start, end, (byte)'W', (byte)'E',
			(byte)'I', (byte)'G', (byte)'H', (byte)'T') ||
		IsToken1(ref platform, text, start, end, (byte)'W');

	private static bool IsBar<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end) where TPlatform : struct, IMuiGuestMemory =>
		IsToken3(ref platform, text, start, end, (byte)'B', (byte)'A', (byte)'R');

	private static bool IsOrder<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end) where TPlatform : struct, IMuiGuestMemory =>
		IsToken5(ref platform, text, start, end, (byte)'O', (byte)'R',
			(byte)'D', (byte)'E', (byte)'R') ||
		IsToken1(ref platform, text, start, end, (byte)'O');

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint ReadGuestLong<TPlatform>(ref TPlatform platform,
		APTR address, int offset) where TPlatform : struct, IMuiGuestMemory =>
		((uint)platform.ReadUInt8(address, offset) << 24) |
		((uint)platform.ReadUInt8(address, offset + 1) << 16) |
		((uint)platform.ReadUInt8(address, offset + 2) << 8) |
		platform.ReadUInt8(address, offset + 3);

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static uint ReadGuestLongLower<TPlatform>(ref TPlatform platform,
		APTR address, int offset) where TPlatform : struct, IMuiGuestMemory =>
		((uint)Lower(platform.ReadUInt8(address, offset)) << 24) |
		((uint)Lower(platform.ReadUInt8(address, offset + 1)) << 16) |
		((uint)Lower(platform.ReadUInt8(address, offset + 2)) << 8) |
		Lower(platform.ReadUInt8(address, offset + 3));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteGuestLong<TPlatform>(ref TPlatform platform,
		APTR address, int offset, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		platform.WriteUInt8(address, offset, (byte)(value >> 24));
		platform.WriteUInt8(address, offset + 1, (byte)(value >> 16));
		platform.WriteUInt8(address, offset + 2, (byte)(value >> 8));
		platform.WriteUInt8(address, offset + 3, (byte)value);
	}

	// The descriptor wire format is a fixed sequence of big-endian ULONGs.
	// Keeping this marshalling in one place lets all format/layout code use the
	// named record above instead of repeating guest offsets.
	internal static void ReadFormatDescriptor<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListFormatDescriptor value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default(MuiListFormatDescriptor);
		value.Delta = ReadGuestLong(ref platform, address, 0);
		value.Weight = ReadGuestLong(ref platform, address, 4);
		value.MinWidth = ReadGuestLong(ref platform, address, 8);
		value.MaxWidth = ReadGuestLong(ref platform, address, 12);
		value.Column = ReadGuestLong(ref platform, address, 16);
		value.Flags = ReadGuestLong(ref platform, address, 20);
		value.Preparse = APTR.FromPointer(ReadGuestLong(ref platform, address, 24));
		value.PreparseLength = ReadGuestLong(ref platform, address, 28);
		value.PreparseStorage = APTR.FromPointer(ReadGuestLong(ref platform,
			address, 32));
		value.PreparseStorageLength = ReadGuestLong(ref platform, address, 36);
	}

	internal static void WriteFormatDescriptor<TPlatform>(ref TPlatform platform,
		APTR address, ref MuiListFormatDescriptor value)
		where TPlatform : struct, IMuiGuestMemory
	{
		WriteGuestLong(ref platform, address, 0, value.Delta);
		WriteGuestLong(ref platform, address, 4, value.Weight);
		WriteGuestLong(ref platform, address, 8, value.MinWidth);
		WriteGuestLong(ref platform, address, 12, value.MaxWidth);
		WriteGuestLong(ref platform, address, 16, value.Column);
		WriteGuestLong(ref platform, address, 20, value.Flags);
		WriteGuestLong(ref platform, address, 24, value.Preparse.Raw);
		WriteGuestLong(ref platform, address, 28, value.PreparseLength);
		WriteGuestLong(ref platform, address, 32, value.PreparseStorage.Raw);
		WriteGuestLong(ref platform, address, 36, value.PreparseStorageLength);
	}

	private static bool TryReadCStringLength<TPlatform>(ref TPlatform platform,
		APTR value, uint maximumLength, out uint length)
		where TPlatform : struct, IMuiGuestMemory
	{
		length = 0;
		if (value.IsNull) return false;
		for (var index = 0u; index < maximumLength; index++)
		{
			if (value.Raw > uint.MaxValue - index) return false;
			var address = APTR.FromPointer(value.Raw + index);
			if (!platform.IsMapped(address, 1)) return false;
			if (platform.ReadUInt8(address) != 0) continue;
			length = index;
			return true;
		}
		return false;
	}

	private static bool IsToken3<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, byte a, byte b, byte c)
		where TPlatform : struct, IMuiGuestMemory => end - start == 3 &&
		Lower(platform.ReadUInt8(text, start)) == Lower(a) &&
		Lower(platform.ReadUInt8(text, start + 1)) == Lower(b) &&
		Lower(platform.ReadUInt8(text, start + 2)) == Lower(c);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsToken4<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, byte a, byte b, byte c, byte d)
		where TPlatform : struct, IMuiGuestMemory => end - start == 4 &&
		Lower(platform.ReadUInt8(text, start)) == Lower(a) &&
		Lower(platform.ReadUInt8(text, start + 1)) == Lower(b) &&
		Lower(platform.ReadUInt8(text, start + 2)) == Lower(c) &&
		Lower(platform.ReadUInt8(text, start + 3)) == Lower(d);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsToken1<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, byte a)
		where TPlatform : struct, IMuiGuestMemory => end - start == 1 &&
		Lower(platform.ReadUInt8(text, start)) == Lower(a);

	private static bool IsToken5<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, byte a, byte b, byte c, byte d, byte e)
		where TPlatform : struct, IMuiGuestMemory => end - start == 5 &&
		Lower(platform.ReadUInt8(text, start)) == Lower(a) &&
		Lower(platform.ReadUInt8(text, start + 1)) == Lower(b) &&
		Lower(platform.ReadUInt8(text, start + 2)) == Lower(c) &&
		Lower(platform.ReadUInt8(text, start + 3)) == Lower(d) &&
		Lower(platform.ReadUInt8(text, start + 4)) == Lower(e);

	private static bool IsToken6<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, byte a, byte b, byte c, byte d, byte e, byte f)
		where TPlatform : struct, IMuiGuestMemory => end - start == 6 &&
		Lower(platform.ReadUInt8(text, start)) == Lower(a) &&
		Lower(platform.ReadUInt8(text, start + 1)) == Lower(b) &&
		Lower(platform.ReadUInt8(text, start + 2)) == Lower(c) &&
		Lower(platform.ReadUInt8(text, start + 3)) == Lower(d) &&
		Lower(platform.ReadUInt8(text, start + 4)) == Lower(e) &&
		Lower(platform.ReadUInt8(text, start + 5)) == Lower(f);

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool IsToken8<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (end - start != 8) return false;
		return ReadGuestLongLower(ref platform, text, start) == first &&
			ReadGuestLongLower(ref platform, text, start + 4) == second;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsMinWidth<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end) where TPlatform : struct, IMuiGuestMemory =>
		IsToken8(ref platform, text, start, end, 0x6D696E77u, 0x69647468u) ||
		IsToken3(ref platform, text, start, end, (byte)'M', (byte)'I', (byte)'W');

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsMaxWidth<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end) where TPlatform : struct, IMuiGuestMemory =>
		IsToken8(ref platform, text, start, end, 0x6D617877u, 0x69647468u) ||
		IsToken3(ref platform, text, start, end, (byte)'M', (byte)'A', (byte)'W');

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsSortable<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end) where TPlatform : struct, IMuiGuestMemory =>
		IsToken8(ref platform, text, start, end, 0x736F7274u, 0x61626C65u);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsPreparse<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end) where TPlatform : struct, IMuiGuestMemory =>
		IsToken8(ref platform, text, start, end, 0x70726570u, 0x61727365u) ||
		IsToken1(ref platform, text, start, end, (byte)'P');

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool WriteWidth<TPlatform>(ref TPlatform platform, APTR text,
		ref MuiListFormatValue value, ref MuiListFormatDescriptor descriptor,
		MuiListFormatField field, uint pixelFlag)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var pixels = value.DecodedLength >= 2 &&
			TryReadDecodedByteAt(ref platform, text, ref value,
				value.DecodedLength - 2, out var penultimate) &&
			TryReadDecodedByteAt(ref platform, text, ref value,
				value.DecodedLength - 1, out var last) &&
			Lower(penultimate) == (byte)'p' && Lower(last) == (byte)'x';
		var numericLength = pixels ? value.DecodedLength - 2 :
			value.DecodedLength;
		if (!TryParseNumber(ref platform, text, ref value, numericLength,
			out var parsed)) return false;
		var raw = unchecked((uint)parsed);
		var contentFlag = field == MuiListFormatField.MinWidth
			? DescriptorMinContent : DescriptorMaxContent;
		if (field == MuiListFormatField.MinWidth)
			descriptor.MinWidth = raw;
		else
			descriptor.MaxWidth = raw;
		if (parsed == -1)
		{
			SetDescriptorFlag(ref descriptor, contentFlag);
			ClearDescriptorFlag(ref descriptor, pixelFlag);
		}
		else
			ClearDescriptorFlag(ref descriptor, contentFlag);
		if (parsed != -1 && pixels)
			SetDescriptorFlag(ref descriptor, pixelFlag);
		else if (parsed != -1)
			ClearDescriptorFlag(ref descriptor, pixelFlag);
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void SetDescriptorFlag(ref MuiListFormatDescriptor descriptor,
		uint flag) => descriptor.Flags |= flag;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ClearDescriptorFlag(ref MuiListFormatDescriptor descriptor,
		uint flag) => descriptor.Flags &= ~flag;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TokenValueEquals<TPlatform>(ref TPlatform platform,
		APTR text, int start, int end, byte a, byte b, byte c, byte d)
		where TPlatform : struct, IMuiGuestMemory => end - start == 4 &&
		Lower(platform.ReadUInt8(text, start)) == Lower(a) &&
		Lower(platform.ReadUInt8(text, start + 1)) == Lower(b) &&
		Lower(platform.ReadUInt8(text, start + 2)) == Lower(c) &&
		Lower(platform.ReadUInt8(text, start + 3)) == Lower(d);

	private static bool IsDescendingValue<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		DecodedToken4Equals(ref platform, text, ref value, (byte)'D',
			(byte)'E', (byte)'S', (byte)'C') ||
		(value.DecodedLength == 10 &&
			DecodedToken8Equals(ref platform, text, ref value, 0x64657363u,
				0x656e6469u) &&
			DecodedByteEquals(ref platform, text, ref value, 8, (byte)'n') &&
			DecodedByteEquals(ref platform, text, ref value, 9, (byte)'g'));

	private static bool IsAscendingValue<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		DecodedToken3Equals(ref platform, text, ref value, (byte)'A',
			(byte)'S', (byte)'C') ||
		(value.DecodedLength == 9 &&
			DecodedToken8Equals(ref platform, text, ref value, 0x61736365u,
				0x6e64696eu) &&
			DecodedByteEquals(ref platform, text, ref value, 8, (byte)'g'));

	private static bool TryParseNumber<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue formatValue, out int value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryParseNumber(ref platform, text, ref formatValue,
			formatValue.DecodedLength, out value);

	private static bool TryParseNumber<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue formatValue, uint length,
		out int value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (length == 0 || length > formatValue.DecodedLength) return false;
		var sourceCursor = formatValue.Start;
		var negative = false;
		if (!TryReadDecodedByte(ref platform, text, ref formatValue,
			ref sourceCursor, out var sign)) return false;
		var cursor = 1u;
		if (sign == (byte)'-' || sign == (byte)'+')
		{
			negative = sign == (byte)'-';
		}
		var any = false;
		if (sign != (byte)'-' && sign != (byte)'+')
		{
			if (sign < (byte)'0' || sign > (byte)'9') return false;
			any = true;
			value = sign - (byte)'0';
		}
		while (cursor < length)
		{
			if (!TryReadDecodedByte(ref platform, text, ref formatValue,
				ref sourceCursor, out var ch)) return false;
			if (ch < (byte)'0' || ch > (byte)'9') break;
			any = true;
			var digit = ch - (byte)'0';
			if (value > 100000000) value = 100000000;
			else value = value * 10 + digit;
			cursor++;
		}
		if (!any || cursor != length) return false;
		value = negative ? -value : value;
		return true;
	}

	// CopperStart's dos.library/ReadItem decodes star escapes only while it is
	// inside a quoted item: *e becomes ESC, *n becomes LF, and every other
	// escaped byte loses the star. Keep that rule as a tiny value-type cursor so
	// FORMAT parsing never creates a managed string or an exception path.
	private static bool TryReadDecodedByte<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue value, ref int sourceCursor,
		out byte decoded)
		where TPlatform : struct, IMuiGuestMemory
	{
		decoded = 0;
		if (sourceCursor >= value.End) return false;
		var current = platform.ReadUInt8(text, sourceCursor++);
		if (value.Quoted == 0 || current != (byte)'*')
		{
			decoded = current;
			return true;
		}
		if (sourceCursor >= value.End) return false;
		current = platform.ReadUInt8(text, sourceCursor++);
		decoded = current switch
		{
			(byte)'e' or (byte)'E' => 0x1B,
			(byte)'n' or (byte)'N' => (byte)'\n',
			_ => current,
		};
		return true;
	}

	private static bool TryPrepareFormatValue<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var sourceCursor = value.Start;
		var decoded = 0u;
		while (sourceCursor < value.End)
		{
			if (!TryReadDecodedByte(ref platform, text, ref value,
				ref sourceCursor, out _)) return false;
			decoded++;
			if (decoded > MaximumStringLength) return false;
		}
		value.DecodedLength = decoded;
		return true;
	}

	private static bool TryReadDecodedByteAt<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue value, uint index, out byte decoded)
		where TPlatform : struct, IMuiGuestMemory
	{
		decoded = 0;
		if (index >= value.DecodedLength) return false;
		var sourceCursor = value.Start;
		for (var current = 0u; current <= index; current++)
			if (!TryReadDecodedByte(ref platform, text, ref value,
				ref sourceCursor, out decoded)) return false;
		return true;
	}

	private static bool DecodedByteEquals<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue value, uint index, byte expected)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadDecodedByteAt(ref platform, text, ref value, index,
			out var actual) && Lower(actual) == Lower(expected);

	private static bool DecodedToken3Equals<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue value, byte a, byte b, byte c)
		where TPlatform : struct, IMuiGuestMemory =>
		value.DecodedLength == 3 &&
		DecodedByteEquals(ref platform, text, ref value, 0, a) &&
		DecodedByteEquals(ref platform, text, ref value, 1, b) &&
		DecodedByteEquals(ref platform, text, ref value, 2, c);

	private static bool DecodedToken4Equals<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue value, byte a, byte b, byte c, byte d)
		where TPlatform : struct, IMuiGuestMemory =>
		value.DecodedLength == 4 &&
		DecodedByteEquals(ref platform, text, ref value, 0, a) &&
		DecodedByteEquals(ref platform, text, ref value, 1, b) &&
		DecodedByteEquals(ref platform, text, ref value, 2, c) &&
		DecodedByteEquals(ref platform, text, ref value, 3, d);

	private static bool DecodedToken8Equals<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue value, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		return value.DecodedLength >= 8 &&
			DecodedByteEquals(ref platform, text, ref value, 0,
				(byte)(first >> 24)) &&
			DecodedByteEquals(ref platform, text, ref value, 1,
				(byte)(first >> 16)) &&
			DecodedByteEquals(ref platform, text, ref value, 2,
				(byte)(first >> 8)) &&
			DecodedByteEquals(ref platform, text, ref value, 3,
				(byte)first) &&
			DecodedByteEquals(ref platform, text, ref value, 4,
				(byte)(second >> 24)) &&
			DecodedByteEquals(ref platform, text, ref value, 5,
				(byte)(second >> 16)) &&
			DecodedByteEquals(ref platform, text, ref value, 6,
				(byte)(second >> 8)) &&
			DecodedByteEquals(ref platform, text, ref value, 7,
				(byte)second);
	}

	private static bool InstallPreparseValue<TPlatform>(ref TPlatform platform,
		APTR text, ref MuiListFormatValue value,
		ref MuiListFormatDescriptor descriptor)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var bytes = value.DecodedLength + 1;
		var storage = MuiHeadlessMemory.Allocate(ref platform, bytes);
		if (storage.IsNull) return false;
		var sourceCursor = value.Start;
		var output = 0u;
		while (output < value.DecodedLength)
		{
			if (!TryReadDecodedByte(ref platform, text, ref value,
				ref sourceCursor, out var decoded))
			{
				platform.Clear(storage, bytes);
				platform.Free(storage, bytes);
				return false;
			}
			platform.WriteUInt8(storage, (int)output++, decoded);
		}
		platform.WriteUInt8(storage, (int)output, 0);
		if (descriptor.PreparseStorage.IsNotNull)
		{
			var old = descriptor.PreparseStorage;
			var oldBytes = descriptor.PreparseStorageLength;
			if (old.IsNotNull && oldBytes != 0)
			{
				platform.Clear(old, oldBytes);
				platform.Free(old, oldBytes);
			}
		}
		descriptor.Preparse = storage;
		descriptor.PreparseLength = value.DecodedLength;
		descriptor.PreparseStorage = storage;
		descriptor.PreparseStorageLength = bytes;
		return true;
	}

	private static void ReleaseFormatDescriptorValue<TPlatform>(
		ref TPlatform platform, ref MuiListFormatDescriptor value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (value.PreparseStorage.IsNull || value.PreparseStorageLength == 0)
			return;
		var storage = value.PreparseStorage;
		platform.Clear(storage, value.PreparseStorageLength);
		platform.Free(storage, value.PreparseStorageLength);
		value.Preparse = APTR.Null;
		value.PreparseLength = 0;
		value.PreparseStorage = APTR.Null;
		value.PreparseStorageLength = 0;
	}

	private static void FreeFormatDescriptors<TPlatform>(ref TPlatform platform,
		APTR block, uint count) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull) return;
		var safeCount = count == 0 || count > MaximumColumns ? 1u : count;
		var size = safeCount * FormatDescriptorSize;
		if (platform.IsMapped(block, size))
		{
			var cursor = default(MuiListFormatDescriptorCursor);
			cursor.Base = block;
			for (var column = 0u; column < safeCount; column++)
			{
				cursor.Index = column;
				if (!MuiListFormatDescriptorCursorCodec.TryGetEntry(ref platform,
					cursor, out var descriptor)) continue;
				ReadFormatDescriptor(ref platform, descriptor, out var value);
				if (value.PreparseStorage.IsNull ||
					value.PreparseStorageLength == 0) continue;
				var storage = value.PreparseStorage;
				platform.Clear(storage, value.PreparseStorageLength);
				platform.Free(storage, value.PreparseStorageLength);
			}
		}
		platform.Clear(block, size);
		platform.Free(block, size);
	}

	private static APTR BuildTitleArrayState<TPlatform>(ref TPlatform platform,
		APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = ReadTitleArrayCount(ref platform, source);
		if (count == uint.MaxValue) return APTR.Null;
		var pointerBytes = (count + 1) * MuiListPointerSlotRecord.Size;
		var pointers = MuiHeadlessMemory.Allocate(ref platform, pointerBytes);
		if (pointers.IsNull) return APTR.Null;
		var sourceCursor = default(MuiListPointerSlotCursor);
		sourceCursor.Base = source;
		var destinationCursor = default(MuiListPointerSlotCursor);
		destinationCursor.Base = pointers;
		for (var column = 0u; column <= count; column++)
		{
			sourceCursor.Index = column;
			destinationCursor.Index = column;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
				sourceCursor, out var sourceSlot) ||
				!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
				destinationCursor, out var destinationSlot))
			{
				platform.Clear(pointers, pointerBytes);
				platform.Free(pointers, pointerBytes);
				return APTR.Null;
			}
			if (!MuiListPointerSlotCodec.TryRead(ref platform, sourceSlot,
				out var sourceValue))
			{
				platform.Clear(pointers, pointerBytes);
				platform.Free(pointers, pointerBytes);
				return APTR.Null;
			}
			var destinationValue = default(MuiListPointerSlotRecord);
			destinationValue.Value = sourceValue.Value;
			if (!MuiListPointerSlotCodec.Write(ref platform, destinationSlot,
				destinationValue))
			{
				platform.Clear(pointers, pointerBytes);
				platform.Free(pointers, pointerBytes);
				return APTR.Null;
			}
		}
		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListTitleArrayState.Size);
		if (block.IsNull)
		{
			platform.Clear(pointers, pointerBytes);
			platform.Free(pointers, pointerBytes);
			return APTR.Null;
		}
		var titleState = default(MuiListTitleArrayState);
		titleState.Magic = TitleArrayStateCookie;
		titleState.Pointers = pointers;
		titleState.Count = count;
		if (!WriteTitleArrayState(ref platform, block, titleState))
		{
			platform.Clear(block, MuiListTitleArrayState.Size);
			platform.Free(block, MuiListTitleArrayState.Size);
			platform.Clear(pointers, pointerBytes);
			platform.Free(pointers, pointerBytes);
			return APTR.Null;
		}
		return block;
	}

	private static uint ReadTitleArrayCount<TPlatform>(ref TPlatform platform,
		APTR source)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (source.IsNull) return 0;
		var cursor = default(MuiListPointerSlotCursor);
		cursor.Base = source;
		for (var column = 0u; column <= MaximumColumns; column++)
		{
			cursor.Index = column;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
				out var slot)) return uint.MaxValue;
			if (!MuiListPointerSlotCodec.TryRead(ref platform, slot,
				out var value)) return uint.MaxValue;
			if (value.Value.IsNull)
				return column;
		}
		return uint.MaxValue;
	}

	private static bool WriteTitleArrayState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListTitleArrayState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (block.IsNull || !platform.IsMapped(block,
			MuiListTitleArrayState.Size) || value.Magic != TitleArrayStateCookie)
			return false;
		return MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.TitleArray, MuiListStateField.Magic,
			value.Magic) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.TitleArray, MuiListStateField.Pointers,
				value.Pointers.Raw) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.TitleArray, MuiListStateField.Count,
				value.Count);
	}

	private static bool WriteTitleState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListTitleState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListTitleState.Size) ||
			value.Magic != TitleStateCookie) return false;
		return MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.TitleValue, MuiListStateField.Magic,
			value.Magic) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.TitleValue, MuiListStateField.TitleValue,
				value.Value);
	}

	private static bool TryReadTitleState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListTitleState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block, MuiListTitleState.Size) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.TitleValue, MuiListStateField.Magic,
				out var magic) || magic != TitleStateCookie ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.TitleValue, MuiListStateField.TitleValue,
				out value.Value)) return false;
		value.Magic = magic;
		return true;
	}

	private static bool EnsureTitleState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			TitleStateKey, 0));
		if (TryReadTitleState(ref platform, block, out _)) return true;
		block = MuiHeadlessMemory.Allocate(ref platform, MuiListTitleState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListTitleState);
		value.Magic = TitleStateCookie;
		value.Value = Read(ref platform, state, obj, Title, 0);
		if (!WriteTitleState(ref platform, block, value))
		{
			platform.Clear(block, MuiListTitleState.Size);
			platform.Free(block, MuiListTitleState.Size);
			return false;
		}
		SetInternal(ref platform, state, obj, TitleStateKey, block.Raw);
		return true;
	}

	private static void SetTitleState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			TitleStateKey, 0));
		if (!TryReadTitleState(ref platform, block, out var title)) return;
		title.Value = value;
		WriteTitleState(ref platform, block, title);
	}

	private static void FreeTitleState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListTitleState.Size))
			return;
		platform.Clear(block, MuiListTitleState.Size);
		platform.Free(block, MuiListTitleState.Size);
	}

	private static bool WriteSelectionSignalState<TPlatform>(
		ref TPlatform platform, APTR block, MuiListSelectionSignalState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (block.IsNull || !platform.IsMapped(block,
			MuiListSelectionSignalState.Size) || value.Magic !=
			SelectionSignalCookie) return false;
		return MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.SelectionSignal, MuiListStateField.Magic,
			value.Magic) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.SelectionSignal,
				MuiListStateField.SelectionValue, value.Value == 0 ? 0u : 1u);
	}

	private static bool TryReadSelectionSignalState<TPlatform>(
		ref TPlatform platform, APTR block,
		out MuiListSelectionSignalState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block,
			MuiListSelectionSignalState.Size) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.SelectionSignal, MuiListStateField.Magic,
				out var magic) || magic != SelectionSignalCookie ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.SelectionSignal,
				MuiListStateField.SelectionValue, out value.Value)) return false;
		value.Magic = magic;
		value.Value = value.Value == 0 ? 0u : 1u;
		return true;
	}

	private static bool EnsureSelectionSignalState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		if (TryReadSelectionSignalState(ref platform, block, out _)) return true;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListSelectionSignalState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListSelectionSignalState);
		value.Magic = SelectionSignalCookie;
		value.Value = Read(ref platform, state, obj, SelectChange, 0);
		if (!WriteSelectionSignalState(ref platform, block, value))
		{
			platform.Clear(block, MuiListSelectionSignalState.Size);
			platform.Free(block, MuiListSelectionSignalState.Size);
			return false;
		}
		SetInternal(ref platform, state, obj, SelectionSignalKey, block.Raw);
		return true;
	}

	private static void SetSelectionSignalState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		if (!TryReadSelectionSignalState(ref platform, block, out var signal))
			return;
		signal.Value = value;
		WriteSelectionSignalState(ref platform, block, signal);
	}

	private static void FreeSelectionSignalState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block,
			MuiListSelectionSignalState.Size)) return;
		platform.Clear(block, MuiListSelectionSignalState.Size);
		platform.Free(block, MuiListSelectionSignalState.Size);
	}

	private static bool WriteFormatPolicyState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListFormatPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (block.IsNull || !platform.IsMapped(block,
			MuiListFormatPolicyState.Size) || value.Magic != FormatPolicyCookie)
			return false;
		return MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.FormatPolicy, MuiListStateField.Magic,
			value.Magic) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.FormatPolicy,
				MuiListStateField.FormatValue, value.Format.Raw) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.FormatPolicy,
				MuiListStateField.MaxColumnsValue, value.MaxColumns) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.FormatPolicy,
				MuiListStateField.FormatColumnsValue, value.Columns);
	}

	private static bool TryReadFormatPolicyState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListFormatPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block,
			MuiListFormatPolicyState.Size) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.FormatPolicy, MuiListStateField.Magic,
				out var magic) || magic != FormatPolicyCookie ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.FormatPolicy,
				MuiListStateField.FormatValue, out var format) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.FormatPolicy,
				MuiListStateField.MaxColumnsValue, out value.MaxColumns) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.FormatPolicy,
				MuiListStateField.FormatColumnsValue, out value.Columns))
			return false;
		value.Magic = magic;
		value.Format = APTR.FromPointer(format);
		return true;
	}

	private static bool EnsureFormatPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FormatPolicyKey, 0));
		if (TryReadFormatPolicyState(ref platform, block, out _)) return true;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListFormatPolicyState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListFormatPolicyState);
		value.Magic = FormatPolicyCookie;
		value.Format = APTR.FromPointer(Read(ref platform, state, obj, Format, 0));
		value.MaxColumns = NormalizeColumnLimit(Read(ref platform, state, obj,
			MaxColumns, DefaultMaxColumns));
		value.Columns = Read(ref platform, state, obj, FormatColumnsKey, 1);
		if (!WriteFormatPolicyState(ref platform, block, value))
		{
			platform.Clear(block, MuiListFormatPolicyState.Size);
			platform.Free(block, MuiListFormatPolicyState.Size);
			return false;
		}
		SetInternal(ref platform, state, obj, FormatPolicyKey, block.Raw);
		return true;
	}

	private static void SetFormatPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR format, uint maximum, uint columns)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FormatPolicyKey, 0));
		if (!TryReadFormatPolicyState(ref platform, block, out var value)) return;
		value.Format = format;
		value.MaxColumns = maximum;
		value.Columns = columns;
		WriteFormatPolicyState(ref platform, block, value);
	}

	private static void FreeFormatPolicyState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block,
			MuiListFormatPolicyState.Size)) return;
		platform.Clear(block, MuiListFormatPolicyState.Size);
		platform.Free(block, MuiListFormatPolicyState.Size);
	}

	private static bool WriteFontState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListFontState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListFontState.Size) ||
			value.Magic != FontStateCookie) return false;
		return MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.FontPolicy, MuiListStateField.Magic,
			value.Magic) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.FontPolicy, MuiListStateField.FontValue,
				value.Font.Raw);
	}

	private static bool TryReadFontState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListFontState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block, MuiListFontState.Size) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.FontPolicy, MuiListStateField.Magic,
				out var magic) || magic != FontStateCookie ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.FontPolicy, MuiListStateField.FontValue,
				out var font)) return false;
		value.Magic = magic;
		value.Font = APTR.FromPointer(font);
		return true;
	}

	private static bool EnsureFontState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FontStateKey, 0));
		if (TryReadFontState(ref platform, block, out _)) return true;
		block = MuiHeadlessMemory.Allocate(ref platform, MuiListFontState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListFontState);
		value.Magic = FontStateCookie;
		value.Font = APTR.FromPointer(Read(ref platform, state, obj, Font, 0));
		if (!WriteFontState(ref platform, block, value))
		{
			platform.Clear(block, MuiListFontState.Size);
			platform.Free(block, MuiListFontState.Size);
			return false;
		}
		SetInternal(ref platform, state, obj, FontStateKey, block.Raw);
		return true;
	}

	private static void SetFontState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR font)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FontStateKey, 0));
		if (!TryReadFontState(ref platform, block, out var value)) return;
		value.Font = font;
		WriteFontState(ref platform, block, value);
	}

	private static void FreeFontState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListFontState.Size))
			return;
		platform.Clear(block, MuiListFontState.Size);
		platform.Free(block, MuiListFontState.Size);
	}

	private static bool TryReadTitleArrayStateBlock<TPlatform>(
		ref TPlatform platform, APTR block, out MuiListTitleArrayState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block, MuiListTitleArrayState.Size) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.TitleArray, MuiListStateField.Magic,
				out var magic) || magic != TitleArrayStateCookie) return false;
		value.Magic = TitleArrayStateCookie;
		if (!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.TitleArray, MuiListStateField.Pointers,
			out var pointers) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.TitleArray, MuiListStateField.Count,
				out value.Count)) return false;
		value.Pointers = APTR.FromPointer(pointers);
		return value.Count <= MaximumColumns && value.Pointers.IsNotNull &&
			platform.IsMapped(value.Pointers,
			(value.Count + 1) * MuiListPointerSlotRecord.Size);
	}

	private static void FreeTitleArrayState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListTitleArrayState.Size))
			return;
		if (TryReadTitleArrayStateBlock(ref platform, block, out var value))
		{
			var bytes = (value.Count + 1) * MuiListPointerSlotRecord.Size;
			var pointers = value.Pointers;
			platform.Clear(pointers, bytes);
			platform.Free(pointers, bytes);
		}
		platform.Clear(block, MuiListTitleArrayState.Size);
		platform.Free(block, MuiListTitleArrayState.Size);
	}

	private static void WriteRedrawState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListRedrawState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Redraw, MuiListStateField.Magic, value.Magic);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Redraw, MuiListStateField.Dirty, value.Dirty);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Redraw, MuiListStateField.Requests,
			value.Requests);
	}

	private static bool TryReadRedrawState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListRedrawState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block, MuiListRedrawState.Size) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.Redraw, MuiListStateField.Magic,
				out var magic) || magic != RedrawStateCookie) return false;
		value.Magic = RedrawStateCookie;
		if (!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.Redraw, MuiListStateField.Dirty,
			out var dirty) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.Redraw, MuiListStateField.Requests,
				out value.Requests)) return false;
		value.Dirty = dirty == 0 ? 0u : 1u;
		return true;
	}

	private static void WriteColumnVisibilityState<TPlatform>(
		ref TPlatform platform, APTR block, MuiListColumnVisibilityState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Magic,
			value.Magic);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Low,
			value.Low);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnVisibility, MuiListStateField.High,
			value.High);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word2,
			value.Word2);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word3,
			value.Word3);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word4,
			value.Word4);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word5,
			value.Word5);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word6,
			value.Word6);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word7,
			value.Word7);
	}

	private static bool TryReadColumnVisibilityState<TPlatform>(
		ref TPlatform platform, APTR block,
		out MuiListColumnVisibilityState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block,
			MuiListColumnVisibilityState.Size) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Magic,
				out var magic) || magic != ColumnVisibilityCookie) return false;
		value.Magic = ColumnVisibilityCookie;
		if (!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Low,
			out value.Low) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnVisibility, MuiListStateField.High,
				out value.High) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word2,
				out value.Word2) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word3,
				out value.Word3) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word4,
				out value.Word4) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word5,
				out value.Word5) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word6,
				out value.Word6) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnVisibility, MuiListStateField.Word7,
				out value.Word7)) return false;
		return true;
	}

	private static void FreeColumnVisibilityState<TPlatform>(
		ref TPlatform platform, APTR block)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block,
			MuiListColumnVisibilityState.Size)) return;
		platform.Clear(block, MuiListColumnVisibilityState.Size);
		platform.Free(block, MuiListColumnVisibilityState.Size);
	}

	private static void WriteColumnOrderState<TPlatform>(
		ref TPlatform platform, APTR block, MuiListColumnOrderState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnOrder, MuiListStateField.Magic,
			value.Magic);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnOrder, MuiListStateField.Count,
			value.Count);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnOrder, MuiListStateField.Values,
			value.Values.Raw);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnOrder, MuiListStateField.Reserved,
			value.Reserved);
	}

	private static bool TryReadColumnOrderState<TPlatform>(
		ref TPlatform platform, APTR block,
		out MuiListColumnOrderState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block,
			MuiListColumnOrderState.Size) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnOrder, MuiListStateField.Magic,
				out var magic) || magic != ColumnOrderCookie) return false;
		value.Magic = ColumnOrderCookie;
		if (!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.ColumnOrder, MuiListStateField.Count,
			out value.Count) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnOrder, MuiListStateField.Values,
				out var values) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ColumnOrder, MuiListStateField.Reserved,
				out value.Reserved)) return false;
		value.Values = APTR.FromPointer(values);
		var valueBytes = ColumnOrderValueBytes(value.Count);
		return value.Count != 0 && value.Count <= MaximumGeometryColumns &&
			value.Values.IsNotNull && value.Reserved == valueBytes &&
			platform.IsMapped(value.Values, valueBytes);
	}

	private static void FreeColumnOrderState<TPlatform>(
		ref TPlatform platform, APTR block)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block,
			MuiListColumnOrderState.Size)) return;
		if (TryReadColumnOrderState(ref platform, block, out var value))
		{
			var values = value.Values;
			ClearColumnOrderBytes(ref platform, values, value.Reserved);
			platform.Free(values, value.Reserved);
		}
		WriteColumnOrderState(ref platform, block, default);
		platform.Free(block, MuiListColumnOrderState.Size);
	}

	private static void ClearColumnOrderBytes<TPlatform>(ref TPlatform platform,
		APTR values, uint count) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListColumnOrderByteCursor);
		cursor.Base = values;
		for (var index = 0u; index < count; index += 4)
		{
			cursor.Index = index;
			if (!MuiListColumnOrderByteCursorCodec.TryGetEntry(ref platform,
				cursor, out var word)) return;
			platform.WriteUInt32(word, 0, 0);
		}
	}

	private static uint ColumnOrderValueBytes(uint columns) =>
		(columns + 3u) & ~3u;

	private static void WriteColumnOrderByte<TPlatform>(ref TPlatform platform,
		APTR values, uint index, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListColumnOrderByteCursor);
		cursor.Base = values;
		cursor.Index = index & ~3u;
		if (!MuiListColumnOrderByteCursorCodec.TryGetEntry(ref platform, cursor,
			out var word)) return;
		var shift = unchecked((int)((3u - (index & 3u)) * 8u));
		var mask = 0xFFu << shift;
		var current = platform.ReadUInt32(word, 0);
		platform.WriteUInt32(word, 0,
			(current & ~mask) | (unchecked((uint)value) << shift));
	}

	private static void FreeRedrawState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListRedrawState.Size))
			return;
		platform.Clear(block, MuiListRedrawState.Size);
		platform.Free(block, MuiListRedrawState.Size);
	}

	private static bool TryReadActiveState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListActiveState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block, MuiListActiveState.Size) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ActiveCursor, MuiListStateField.Magic,
				out var magic) || magic != ActiveStateCookie ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ActiveCursor, MuiListStateField.HasActive,
				out value.HasActive) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.ActiveCursor, MuiListStateField.Active,
				out value.Active)) return false;
		value.Magic = magic;
		value.HasActive = value.HasActive == 0 ? 0u : 1u;
		return true;
	}

	private static bool WriteActiveState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListActiveState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListActiveState.Size) ||
			value.Magic != ActiveStateCookie) return false;
		return MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ActiveCursor, MuiListStateField.Magic,
			value.Magic) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.ActiveCursor, MuiListStateField.HasActive,
				value.HasActive == 0 ? 0u : 1u) &&
			MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
				MuiListStateRecordKind.ActiveCursor, MuiListStateField.Active,
				value.Active);
	}

	private static bool EnsureActiveState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ActiveStateKey, 0));
		if (TryReadActiveState(ref platform, block, out _)) return true;
		block = MuiHeadlessMemory.Allocate(ref platform, MuiListActiveState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListActiveState);
		value.Magic = ActiveStateCookie;
		value.Active = Read(ref platform, state, obj, Active, ActiveOff);
		if (!WriteActiveState(ref platform, block, value))
		{
			platform.Clear(block, MuiListActiveState.Size);
			platform.Free(block, MuiListActiveState.Size);
			return false;
		}
		SetInternal(ref platform, state, obj, ActiveStateKey, block.Raw);
		return true;
	}

	private static void SetActivePresence<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, bool hasActive)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ActiveStateKey, 0));
		if (!TryReadActiveState(ref platform, block, out var value)) return;
		value.HasActive = hasActive ? 1u : 0u;
		WriteActiveState(ref platform, block, value);
	}

	private static void SetActiveCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint active, bool hasActive)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ActiveStateKey, 0));
		if (!TryReadActiveState(ref platform, block, out var value)) return;
		value.Active = active;
		value.HasActive = hasActive ? 1u : 0u;
		WriteActiveState(ref platform, block, value);
	}

	private static void FreeActiveState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListActiveState.Size))
			return;
		platform.Clear(block, MuiListActiveState.Size);
		platform.Free(block, MuiListActiveState.Size);
	}

	private static void WriteViewportState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListViewportState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Viewport, MuiListStateField.Magic, value.Magic);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Viewport, MuiListStateField.TopPixel,
			value.TopPixel);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Viewport, MuiListStateField.VisiblePixel,
			value.VisiblePixel);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Viewport, MuiListStateField.TotalPixel,
			value.TotalPixel);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Viewport, MuiListStateField.First,
			value.First);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Viewport, MuiListStateField.LineHeight,
			value.LineHeight);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Viewport, MuiListStateField.Visible,
			value.Visible);
		MuiListStateFieldCursorCodec.TryWriteUInt32(ref platform, block,
			MuiListStateRecordKind.Viewport, MuiListStateField.DropMark,
			value.DropMark);
	}

	// Struct-first qualification seam for the public pixel metrics. The List
	// integration below supplies the normalized row values; this bounded writer
	// keeps the 68k record contract independently testable without a host object
	// or managed layout state.
	public static bool WriteViewportMetrics<TPlatform>(ref TPlatform platform,
		APTR storage, uint first, uint visible, uint entries, uint lineHeight,
		uint titleRows)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (storage.IsNull || !platform.IsMapped(storage,
			MuiListViewportState.Size)) return false;
		var effectiveLineHeight = lineHeight == 0 ? 1u : lineHeight;
		var value = default(MuiListViewportState);
		value.Magic = ViewportStateCookie;
		value.First = first;
		value.LineHeight = effectiveLineHeight;
		value.Visible = visible;
		value.DropMark = unchecked((uint)DropMarkNone);
		value.TopPixel = SaturatingMultiply(first, effectiveLineHeight);
		value.VisiblePixel = SaturatingMultiply(
			SaturatingAdd(visible, titleRows), effectiveLineHeight);
		value.TotalPixel = SaturatingMultiply(
			SaturatingAdd(entries, titleRows), effectiveLineHeight);
		WriteViewportState(ref platform, storage, value);
		return true;
	}

	private static bool TryReadViewportState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListViewportState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (block.IsNull || !platform.IsMapped(block, MuiListViewportState.Size) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.Viewport, MuiListStateField.Magic,
				out var magic) || magic != ViewportStateCookie) return false;
		value.Magic = ViewportStateCookie;
		if (!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
			MuiListStateRecordKind.Viewport, MuiListStateField.TopPixel,
			out value.TopPixel) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.Viewport, MuiListStateField.VisiblePixel,
				out value.VisiblePixel) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.Viewport, MuiListStateField.TotalPixel,
				out value.TotalPixel) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.Viewport, MuiListStateField.First,
				out value.First) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.Viewport, MuiListStateField.LineHeight,
				out value.LineHeight) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.Viewport, MuiListStateField.Visible,
				out value.Visible) ||
			!MuiListStateFieldCursorCodec.TryReadUInt32(ref platform, block,
				MuiListStateRecordKind.Viewport, MuiListStateField.DropMark,
				out value.DropMark)) return false;
		return true;
	}

	private static void FreeViewportState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListViewportState.Size))
			return;
		platform.Clear(block, MuiListViewportState.Size);
		platform.Free(block, MuiListViewportState.Size);
	}

	public static uint FormatColumnCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		FormatColumnsCursor(ref platform, state, obj);

	internal static bool TryGetFormatPolicyState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListFormatPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FormatPolicyKey, 0));
		return TryReadFormatPolicyState(ref platform, block, out value);
	}

	internal static bool TryGetFontState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListFontState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FontStateKey, 0));
		return TryReadFontState(ref platform, block, out value);
	}

	private static APTR FontCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryGetFontState(ref platform, state, obj, out var value)
			? value.Font
			: APTR.FromPointer(Read(ref platform, state, obj, Font, 0));
	}

	private static APTR FormatValueCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FormatPolicyKey, 0));
		return TryReadFormatPolicyState(ref platform, block, out var value)
			? value.Format
			: APTR.FromPointer(Read(ref platform, state, obj, Format, 0));
	}

	private static uint MaxColumnsCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FormatPolicyKey, 0));
		return TryReadFormatPolicyState(ref platform, block, out var value)
			? value.MaxColumns
			: NormalizeColumnLimit(Read(ref platform, state, obj, MaxColumns,
				DefaultMaxColumns));
	}

	private static uint FormatColumnsCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FormatPolicyKey, 0));
		return TryReadFormatPolicyState(ref platform, block, out var value)
			? value.Columns
			: Read(ref platform, state, obj, FormatColumnsKey, 1);
	}

	public static bool GetFormatColumn<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column, APTR storage)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (storage.IsNull || !platform.IsMapped(storage, FormatDescriptorSize))
			return false;
		var count = FormatColumnCount(ref platform, state, obj);
		if (column >= count) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FormatDescriptorKey, 0));
		if (block.IsNull) return false;
		var descriptorColumn = OrderedDescriptorColumn(ref platform, state, obj,
			column);
		if (descriptorColumn >= count) descriptorColumn = column;
		var cursor = default(MuiListFormatDescriptorCursor);
		cursor.Base = block;
		cursor.Index = descriptorColumn;
		if (!MuiListFormatDescriptorCursorCodec.TryGetEntry(ref platform, cursor,
			out var descriptor)) return false;
		var value = default(MuiListFormatDescriptor);
		ReadFormatDescriptor(ref platform, descriptor, out value);
		WriteFormatDescriptor(ref platform, storage, ref value);
		return true;
	}

	// Expose the derived display-to-source mapping to the collection adapter and
	// native qualification seam without exposing the private descriptor wire
	// layout. Draw/measurement use the same named-record mapping below.
	public static uint GetFormatDisplaySourceColumn<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint displayColumn)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		DisplaySourceColumn(ref platform, state, obj, displayColumn);

	// Derive the current bounded column geometry into caller-provided guest
	// storage. Each record is {offset,width}; offsets are relative to the List
	// left edge. The calculation is intentionally integer-only and uses the
	// already parsed guest-resident descriptors, so no managed layout state is
	// introduced between Layout and Draw.
	public static bool GetColumnGeometry<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int width, APTR storage)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (width < 0 || storage.IsNull) return false;
		var columns = GeometryColumnCount(ref platform, state, obj);
		if (!platform.IsMapped(storage, columns * ColumnGeometryRecordSize))
			return false;
		var hidden = HiddenColumns(ref platform, state, obj, width, columns);
		var totalWidth = unchecked((uint)width);
		var totalDelta = VisibleDeltaTotal(ref platform, state, obj, columns,
			hidden);
		var remaining = totalWidth > totalDelta
			? totalWidth - totalDelta : 0u;
		var remainingWeight = VisibleWeightTotal(ref platform, state, obj,
			columns, hidden);
		if (remainingWeight == 0) remainingWeight = 1;
		var offset = 0u;
		var cursor = default(MuiListColumnGeometryCursor);
		cursor.Base = storage;
		for (var column = 0u; column < columns; column++)
		{
			var columnWidth = 0u;
			if (!IsHidden(hidden, column))
			{
				var weight = ColumnWeightValue(ref platform, state, obj, column);
				var usesContentWeight = ColumnUsesContentWeight(ref platform, state,
					obj, column);
				var share = usesContentWeight
					? ColumnMetric(ref platform, state, obj, width, column)
					: IsLastVisible(hidden, column, columns) || remainingWeight == 0
						? remaining : remaining * weight / remainingWeight;
				var minimum = ColumnLimit(ref platform, state, obj, column, width,
					MuiListFormatField.MinWidth, DescriptorMinPixel);
				var maximum = ColumnLimit(ref platform, state, obj, column, width,
					MuiListFormatField.MaxWidth, DescriptorMaxPixel);
				if (share < minimum) share = minimum;
				if (maximum != uint.MaxValue && share > maximum) share = maximum;
				if (share > remaining) share = remaining;
				if (share == 0 && remaining != 0 && column + 1 <= columns)
					share = 1;
				columnWidth = share;
				remaining -= share;
				if (!usesContentWeight)
					remainingWeight = remainingWeight > weight
						? remainingWeight - weight : 0;
			}
			cursor.Index = column;
			if (!MuiListColumnGeometryCursorCodec.TryGetEntry(ref platform, cursor,
				out var record)) return false;
			var geometry = default(MuiListColumnGeometry);
			geometry.Offset = offset;
			geometry.Width = columnWidth;
			if (!MuiListColumnGeometryCodec.Write(ref platform, record, geometry))
				return false;
			if (columnWidth != 0)
			{
				offset = SaturatingAdd(offset, columnWidth);
				if (!IsLastVisible(hidden, column, columns))
					offset = SaturatingAdd(offset,
						ColumnDelta(ref platform, state, obj, column));
			}
		}
		return true;
	}

	private static bool InstallColumnLayout<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int width)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var columns = GeometryColumnCount(ref platform, state, obj);
		var bytes = columns * ColumnGeometryRecordSize;
		var block = MuiHeadlessMemory.Allocate(ref platform, bytes);
		if (block.IsNull) return false;
		if (!GetColumnGeometry(ref platform, state, obj, width, block))
		{
			platform.Free(block, bytes);
			return false;
		}
		var old = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnLayoutKey, 0));
		var oldColumns = GeometryColumnCount(ref platform, state, obj);
		SetInternal(ref platform, state, obj, ColumnLayoutKey, block.Raw);
		SetInternal(ref platform, state, obj, ColumnLayoutWidthKey,
			unchecked((uint)width));
		if (old.IsNotNull)
			platform.Free(old, oldColumns * ColumnGeometryRecordSize);
		return true;
	}

	private static void FreeColumnLayout<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnLayoutKey, 0));
		if (block.IsNull) return;
		var columns = GeometryColumnCount(ref platform, state, obj);
		platform.Clear(block, columns * ColumnGeometryRecordSize);
		platform.Free(block, columns * ColumnGeometryRecordSize);
		ClearInternal(ref platform, state, obj, ColumnLayoutKey);
		ClearInternal(ref platform, state, obj, ColumnLayoutWidthKey);
	}

	private static void FreeColumnMetrics<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnMetricsKey, 0));
		if (!MuiListColumnMetricsStateCodec.TryRead(ref platform, block,
			out var value))
		{
			ClearInternal(ref platform, state, obj, ColumnMetricsKey);
			return;
		}
		var values = value.Values;
		var bytes = value.Columns * MuiListColumnMetricValue.Size;
		if (platform.IsMapped(values, bytes))
		{
			platform.Clear(values, bytes);
			platform.Free(values, bytes);
		}
		platform.Clear(block, MuiListColumnMetricsState.Size);
		platform.Free(block, MuiListColumnMetricsState.Size);
		ClearInternal(ref platform, state, obj, ColumnMetricsKey);
	}

	private static void FreeHScrollerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HScrollerStateKey, 0));
		if (block.IsNotNull && platform.IsMapped(block,
			MuiListHScrollerState.Size))
		{
			platform.Clear(block, MuiListHScrollerState.Size);
			platform.Free(block, MuiListHScrollerState.Size);
		}
		ClearInternal(ref platform, state, obj, HScrollerStateKey);
	}

	private static void FreeImages<TPlatform>(ref TPlatform platform, APTR header)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = ReadHeaderImages(ref platform, header);
		for (var count = 0u; current.IsNotNull && count < MaximumImages; count++)
		{
			if (!MuiListImageCodec.TryRead(ref platform, current,
				out var image)) break;
			var next = image.Next;
			platform.Clear(current, ImageRecordSize);
			platform.Free(current, ImageRecordSize);
			current = next;
		}
		WriteHeaderImages(ref platform, header, APTR.Null);
	}

	private static uint GeometryColumnCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = FormatColumnCount(ref platform, state, obj);
		if (count == 0) count = 1;
		return count > MaximumGeometryColumns ? MaximumGeometryColumns : count;
	}

	internal static int ContentLayoutWidth<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int viewportWidth)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (viewportWidth <= 0 || !TryGetHScrollerState(ref platform, state, obj,
			out var hState) || hState.ContentWidth <= unchecked((uint)viewportWidth) ||
			hState.Visible == 0) return viewportWidth;
		return hState.ContentWidth > int.MaxValue
			? int.MaxValue : unchecked((int)hState.ContentWidth);
	}

	internal static uint HorizontalScrollX<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		TryGetHScrollerState(ref platform, state, obj, out var hState)
			? hState.ScrollX : 0;

	private static bool IsHidden(MuiListHiddenColumns hidden, uint column)
	{
		var word = column >> 5;
		if (word >= 8) return false;
		var bit = 1u << (int)(column & 31);
		return (ReadHiddenWord(hidden, word) & bit) != 0;
	}

	private static void Hide(ref MuiListHiddenColumns hidden, uint column)
	{
		var word = column >> 5;
		if (word >= 8) return;
		var bit = 1u << (int)(column & 31);
		WriteHiddenWord(ref hidden, word, ReadHiddenWord(hidden, word) | bit);
	}

	private static void Unhide(ref MuiListHiddenColumns hidden, uint column)
	{
		var word = column >> 5;
		if (word >= 8) return;
		var bit = 1u << (int)(column & 31);
		WriteHiddenWord(ref hidden, word, ReadHiddenWord(hidden, word) & ~bit);
	}

	private static uint ReadHiddenWord(MuiListHiddenColumns hidden, uint word) =>
		word switch
		{
			0 => hidden.Low,
			1 => hidden.High,
			2 => hidden.Word2,
			3 => hidden.Word3,
			4 => hidden.Word4,
			5 => hidden.Word5,
			6 => hidden.Word6,
			7 => hidden.Word7,
			_ => 0,
		};

	private static void WriteHiddenWord(ref MuiListHiddenColumns hidden,
		uint word, uint value)
	{
		switch (word)
		{
			case 0: hidden.Low = value; break;
			case 1: hidden.High = value; break;
			case 2: hidden.Word2 = value; break;
			case 3: hidden.Word3 = value; break;
			case 4: hidden.Word4 = value; break;
			case 5: hidden.Word5 = value; break;
			case 6: hidden.Word6 = value; break;
			case 7: hidden.Word7 = value; break;
		}
	}

	private static bool IsLastVisible(MuiListHiddenColumns hidden,
		uint column, uint columns)
	{
		for (var next = column + 1; next < columns; next++)
			if (!IsHidden(hidden, next)) return false;
		return true;
	}

	private static uint VisibleDeltaTotal<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint columns, MuiListHiddenColumns hidden)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var total = 0u;
		for (var column = 0u; column < columns; column++)
			if (!IsHidden(hidden, column) &&
				!IsLastVisible(hidden, column, columns))
				total = SaturatingAdd(total,
					ColumnDelta(ref platform, state, obj, column));
		return total;
	}

	private static uint VisibleWeightTotal<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint columns, MuiListHiddenColumns hidden)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var total = 0u;
		for (var column = 0u; column < columns; column++)
			if (!IsHidden(hidden, column) &&
				!ColumnUsesContentWeight(ref platform, state, obj, column))
				total = SaturatingAdd(total,
					ColumnWeightValue(ref platform, state, obj, column));
		return total;
	}

	// MorphOS hides a non-first column when its minimum cannot fit in the
	// remaining rectangle, then redistributes that space over the columns that
	// remain. The first column is never hidden; it is clipped instead. Re-run
	// the bounded pass after each hide so a later column can become visible once
	// an earlier impossible column has been removed.
	private static MuiListHiddenColumns HiddenColumns<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, int width, uint columns)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hidden = default(MuiListHiddenColumns);
		var visibilityBlock = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnVisibilityKey, 0));
		if (TryReadColumnVisibilityState(ref platform, visibilityBlock,
			out var visibility))
		{
			hidden.Low = visibility.Low;
			hidden.High = visibility.High;
			hidden.Word2 = visibility.Word2;
			hidden.Word3 = visibility.Word3;
			hidden.Word4 = visibility.Word4;
			hidden.Word5 = visibility.Word5;
			hidden.Word6 = visibility.Word6;
			hidden.Word7 = visibility.Word7;
		}
		if (width <= 0 || columns <= 1) return hidden;
		for (var pass = 0u; pass < columns; pass++)
		{
			var totalWidth = unchecked((uint)width);
			var totalDelta = VisibleDeltaTotal(ref platform, state, obj,
				columns, hidden);
			var remaining = totalWidth > totalDelta
				? totalWidth - totalDelta : 0u;
			var remainingWeight = VisibleWeightTotal(ref platform, state, obj,
				columns, hidden);
			if (remainingWeight == 0) remainingWeight = 1;
			var changed = false;
			for (var column = 0u; column < columns; column++)
			{
				if (IsHidden(hidden, column)) continue;
				var weight = ColumnWeightValue(ref platform, state, obj, column);
				var usesContentWeight = ColumnUsesContentWeight(ref platform,
					state, obj, column);
				var share = usesContentWeight
					? ColumnMetric(ref platform, state, obj, width, column)
					: IsLastVisible(hidden, column, columns) || remainingWeight == 0
						? remaining : remaining * weight / remainingWeight;
				var minimum = ColumnLimit(ref platform, state, obj, column, width,
					MuiListFormatField.MinWidth, DescriptorMinPixel);
				if (column != 0 && minimum > remaining)
				{
					Hide(ref hidden, column);
					changed = true;
					continue;
				}
				if (share < minimum) share = minimum;
				var maximum = ColumnLimit(ref platform, state, obj, column, width,
					MuiListFormatField.MaxWidth, DescriptorMaxPixel);
				if (maximum != uint.MaxValue && share > maximum) share = maximum;
				if (share > remaining) share = remaining;
				remaining -= share;
				if (!usesContentWeight)
					remainingWeight = remainingWeight > weight
						? remainingWeight - weight : 0;
			}
			if (!changed) return hidden;
		}
		return hidden;
	}

	private static uint ColumnOffset<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int width, uint columns, uint target)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hidden = HiddenColumns(ref platform, state, obj, width, columns);
		return ColumnOffset(ref platform, state, obj, width, columns, target,
			hidden);
	}

	private static uint ColumnOffset<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int width, uint columns, uint target,
		MuiListHiddenColumns hidden)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var offset = 0u;
		for (var column = 0u; column < target; column++)
		{
			if (IsHidden(hidden, column)) continue;
			offset = SaturatingAdd(offset,
			ColumnWidth(ref platform, state, obj, width, columns, column));
			if (!IsLastVisible(hidden, column, columns))
				offset = SaturatingAdd(offset,
					ColumnDelta(ref platform, state, obj, column));
		}
		return offset;
	}

	private static uint ColumnWidth<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int width, uint columns, uint target)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (columns == 0 || target >= columns || width <= 0) return 0;
		var hidden = HiddenColumns(ref platform, state, obj, width, columns);
		return ColumnWidth(ref platform, state, obj, width, columns, target,
			hidden);
	}

	private static uint ColumnWidth<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int width, uint columns, uint target,
		MuiListHiddenColumns hidden)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (columns == 0 || target >= columns || width <= 0) return 0;
		if (IsHidden(hidden, target)) return 0;
		var totalWidth = unchecked((uint)width);
		var totalDelta = VisibleDeltaTotal(ref platform, state, obj, columns,
			hidden);
		var remaining = totalWidth > totalDelta ? totalWidth - totalDelta : 0u;
		var remainingWeight = VisibleWeightTotal(ref platform, state, obj,
			columns, hidden);
		if (remainingWeight == 0) remainingWeight = 1;
		var result = 0u;
		for (var column = 0u; column < columns; column++)
		{
			if (IsHidden(hidden, column)) continue;
			var weight = ColumnWeightValue(ref platform, state, obj, column);
			var usesContentWeight = ColumnUsesContentWeight(ref platform, state,
				obj, column);
			var share = usesContentWeight
				? ColumnMetric(ref platform, state, obj, width, column)
				: IsLastVisible(hidden, column, columns) || remainingWeight == 0
					? remaining : remaining * weight / remainingWeight;
			var minimum = ColumnLimit(ref platform, state, obj, column, width,
				MuiListFormatField.MinWidth, DescriptorMinPixel);
			var maximum = ColumnLimit(ref platform, state, obj, column, width,
				MuiListFormatField.MaxWidth, DescriptorMaxPixel);
			if (share < minimum) share = minimum;
			if (maximum != uint.MaxValue && share > maximum) share = maximum;
			if (share > remaining) share = remaining;
			if (share == 0 && remaining != 0 && column + 1 <= columns)
				share = 1;
			if (column == target) result = share;
			remaining -= share;
			if (!usesContentWeight)
				remainingWeight = remainingWeight > weight
					? remainingWeight - weight : 0;
		}
		return result;
	}

	private static uint ColumnDelta<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		DescriptorValue(ref platform, state, obj, column,
			MuiListFormatField.Delta, 4);

	private static uint ColumnWeightValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var value = DescriptorValue(ref platform, state, obj, column,
			MuiListFormatField.Weight, 100);
		return value == 0 ? 1 : value;
	}

	private static bool ColumnUsesContentWeight<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		(DescriptorValue(ref platform, state, obj, column,
			MuiListFormatField.Flags, 0) & DescriptorWeightContent) != 0;

	private static uint ColumnLimit<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column, int width,
		MuiListFormatField field,
		uint pixelFlag)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var value = DescriptorValue(ref platform, state, obj, column,
			field, uint.MaxValue);
		var flags = DescriptorValue(ref platform, state, obj, column,
			MuiListFormatField.Flags, 0);
		var contentFlag = field == MuiListFormatField.MinWidth
			? DescriptorMinContent : DescriptorMaxContent;
		if (value == uint.MaxValue)
		{
			if ((flags & contentFlag) != 0)
				return ColumnMetric(ref platform, state, obj, width, column);
			return field == MuiListFormatField.MinWidth ? 0u : value;
		}
		if ((flags & pixelFlag) != 0) return value;
		// MorphOS Format uses percentage widths unless the optional `px` suffix
		// was present. Clamp malformed values above 100% to the available list
		// width rather than allowing a multiplier to create impossible geometry.
		if (width <= 0 || value >= 100) return width <= 0 ? 0u : unchecked((uint)width);
		return PercentageWidth(width, value);
	}

	private static uint PercentageWidth(int width, uint percentage)
	{
		// Split the product so the bounded 32-bit path cannot overflow for a
		// malformed but positive host rectangle near INT_MAX.
		var pixels = unchecked((uint)width);
		var whole = pixels / 100u;
		var remainder = pixels % 100u;
		return whole * percentage + remainder * percentage / 100u;
	}

	// Resolve a display column to the descriptor column selected by the
	// guest-owned ColumnOrder permutation. An absent or malformed order is the
	// identity mapping, preserving the ordinary FORMAT behavior.
	private static uint OrderedDescriptorColumn<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint displayColumn)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnOrderKey, 0));
		if (!TryReadColumnOrderState(ref platform, block, out var order) ||
			displayColumn >= order.Count) return displayColumn;
		var cursor = default(MuiListColumnOrderByteCursor);
		cursor.Base = order.Values;
		cursor.Index = displayColumn;
		if (!MuiListColumnOrderByteCursorCodec.TryGetEntry(ref platform, cursor,
			out var address)) return displayColumn;
		var value = platform.ReadUInt8(address, 0);
		return value;
	}

	private static uint DescriptorValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column, MuiListFormatField field,
		uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = GeometryColumnCount(ref platform, state, obj);
		if (column >= count) return fallback;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FormatDescriptorKey, 0));
		if (block.IsNull || !platform.IsMapped(block,
			count * FormatDescriptorSize)) return fallback;
		var descriptorColumn = OrderedDescriptorColumn(ref platform, state, obj,
			column);
		if (descriptorColumn >= count) descriptorColumn = column;
		var cursor = default(MuiListFormatDescriptorCursor);
		cursor.Base = block;
		cursor.Index = descriptorColumn;
		if (!MuiListFormatDescriptorCursorCodec.TryGetEntry(ref platform, cursor,
			out var descriptorAddress)) return fallback;
		var descriptor = default(MuiListFormatDescriptor);
		ReadFormatDescriptor(ref platform, descriptorAddress, out descriptor);
		return field switch
		{
			MuiListFormatField.Delta => descriptor.Delta,
			MuiListFormatField.Weight => descriptor.Weight,
			MuiListFormatField.MinWidth => descriptor.MinWidth,
			MuiListFormatField.MaxWidth => descriptor.MaxWidth,
			MuiListFormatField.Column => descriptor.Column,
			MuiListFormatField.Flags => descriptor.Flags,
			MuiListFormatField.Preparse => descriptor.Preparse.Raw,
			MuiListFormatField.PreparseLength => descriptor.PreparseLength,
			_ => fallback,
		};
	}

	// FORMAT's COL field changes which source StringArray column is displayed
	// at a given derived column. Keep the mapping in the named descriptor
	// record; the only raw arithmetic below is the guest pointer-table ABI.
	private static uint DisplaySourceColumn<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint displayColumn)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var source = DescriptorValue(ref platform, state, obj, displayColumn,
			MuiListFormatField.Column, displayColumn);
		return source < MaximumDrawColumns ? source : MaximumDrawColumns;
	}

	// PREPARSE is a guest string owned by the FORMAT descriptor. The graphics
	// seam currently needs only MUI's documented horizontal controls: ESC-c for
	// centered text and ESC-r for right-aligned text. Accept the literal
	// "\\33c"/"\\33r" spelling too, because command/configuration sources can
	// provide the escape as four guest bytes instead of a pre-decoded ESC.
	private static MuiListTextAlignment FormatTextAlignment<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint column)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var address = DescriptorValue(ref platform, state, obj, column,
			MuiListFormatField.Preparse, 0);
		var length = DescriptorValue(ref platform, state, obj, column,
			MuiListFormatField.PreparseLength, 0);
		if (address == 0 || length == 0) return MuiListTextAlignment.Left;
		var preparse = APTR.FromPointer(address);
		if (!platform.IsMapped(preparse, length))
			return MuiListTextAlignment.Left;
		var first = platform.ReadUInt8(preparse, 0);
		var codeOffset = -1;
		if (first == 0x1Bu && length >= 2)
			codeOffset = 1;
		else if (first == (byte)'*' && length >= 3 &&
			(platform.ReadUInt8(preparse, 1) == (byte)'e' ||
				platform.ReadUInt8(preparse, 1) == (byte)'E'))
			codeOffset = 2;
		else if (first == (byte)'\\' && length >= 4 &&
			platform.ReadUInt8(preparse, 1) == (byte)'3' &&
			platform.ReadUInt8(preparse, 2) == (byte)'3')
			codeOffset = 3;
		if (codeOffset < 0) return MuiListTextAlignment.Left;
		return platform.ReadUInt8(preparse, codeOffset) switch
		{
			(byte)'c' => MuiListTextAlignment.Center,
			(byte)'r' => MuiListTextAlignment.Right,
			_ => MuiListTextAlignment.Left,
		};
	}

	private static uint SaturatingAdd(uint left, uint right) =>
		left > uint.MaxValue - right ? uint.MaxValue : left + right;

	private static bool ApplyActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, APTR obj, int requested, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var count = ReadHeaderCount(ref platform, header);
		var hasCurrent = MuiHeadlessObjectCore.GetAttribute(ref platform, state,
			obj, Active, out _);
		var current = hasCurrent ? ActiveIndex(ref platform, state, obj) : -1;
		var resolved = requested;
		if (count == 0)
			// MorphOS 3.20 publishes zero through MUIA_List_Active for an empty
			// list. ActiveIndex() below keeps the internal selector semantics
			// separate: zero is a public compatibility projection, not a row.
			resolved = 0;
		else
		{
			var visible = unchecked((int)VisibleCursor(ref platform, state, obj));
			var page = visible > 0 ? visible : 1;
			resolved = requested switch
			{
				-1 => -1,
				ActiveTop => 0,
				ActiveBottom => (int)count - 1,
				ActiveUp => current < 0 ? 0 : current - 1,
				ActiveDown => current < 0 ? 0 : current + 1,
				ActivePageUp => current < 0 ? 0 : current - page,
				ActivePageDown => current < 0 ? 0 : current + page,
				_ => requested,
			};
			if (resolved < 0) resolved = 0;
			if ((uint)resolved >= count) resolved = (int)count - 1;
		}

		var first = unchecked((int)FirstCursor(ref platform, state, obj));
		var firstForNormalization = resolved >= 0 && first < 0 ? 0 : first;
		var normalizedFirst = NormalizeFirst(ref platform, state, obj,
			firstForNormalization,
			resolved, count);
		var activeChanged = !hasCurrent || current != resolved;
		var firstChanged = first != normalizedFirst;
		if (activeChanged && !SetRaw(ref platform, state, record, Active,
			unchecked((uint)resolved), notify)) return false;
		if (firstChanged && !SetRaw(ref platform, state, record, First,
			unchecked((uint)normalizedFirst), notify)) return false;
		SetActiveCursor(ref platform, state, obj,
			unchecked((uint)resolved), count != 0 && resolved >= 0);
		return true;
	}

	private static bool ApplyFirst<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, APTR obj, int requested, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = EntryCount(ref platform, state, obj);
		var active = ActiveIndex(ref platform, state, obj);
		var normalized = NormalizeFirst(ref platform, state, obj, requested,
			active, count);
		var hasCurrent = MuiHeadlessObjectCore.GetAttribute(ref platform, state,
			obj, First, out var currentRaw);
		var current = unchecked((int)(hasCurrent ? currentRaw : 0));
		return hasCurrent && current == normalized || SetRaw(ref platform, state,
			record, First,
			unchecked((uint)normalized), notify);
	}

	private static int NormalizeFirst<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int requested, int active, uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// NormalizeFirst is also called from Layout immediately after the raw
		// Visible projection is changed and before the named viewport record is
		// republished. Use that authoritative transition value here; steady-state
		// consumers use VisibleCursor below.
		var visible = Read(ref platform, state, obj, Visible, 0);
		if (visible == VisibleOff) return -1;
		if (count == 0) return 0;
		if (requested == -1) return -1;
		var visibleRows = unchecked((int)visible);
		var maxFirst = visibleRows > 0 && (uint)visibleRows < count
			? (int)count - visibleRows : 0;
		var first = requested < 0 ? 0 : requested;
		if (active >= 0 && visibleRows > 0)
		{
			if (active < first) first = active;
			else if (active >= first + visibleRows)
				first = active - visibleRows + 1;
		}
		if (first > maxFirst) first = maxFirst;
		return first;
	}

	// Retire the guest-resident state during object disposal. Every surviving
	// entry is destructed (honouring the destruct hook / owned-string ownership)
	// before the index and header blocks are freed. Invoked from
	// MuiCollectionLifecycle.DisposeObject; a no-op for non-List objects.
	internal static void CleanupRecords<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		// Clear the optional composite link before the child record is retired.
		// CleanupTree visits children before their owning Listview, so this also
		// prevents a late teardown callback from observing a stale parent.
		ClearInternal(ref platform, state, obj, ListviewOwnerKey);
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return;
		CancelEditState(ref platform, state, obj);
		var index = ReadHeaderIndex(ref platform, header);
		var capacity = ReadHeaderCapacity(ref platform, header);
		var count = ReadHeaderCount(ref platform, header);
		var pool = PoolFor(ref platform, state, obj);
		var formatBlock = APTR.FromPointer(Read(ref platform, state, obj,
			FormatDescriptorKey, 0));
		var formatCount = FormatColumnsCursor(ref platform, state, obj);
		var titleArrayState = APTR.FromPointer(Read(ref platform, state, obj,
			TitleArrayStateKey, 0));
		for (var i = 0u; i < count && i < MaximumEntries; i++)
			DestructSlot(ref platform, state, obj, index, i, pool);
		FreeImages(ref platform, header);
		FreeColumnLayout(ref platform, state, obj);
		FreeColumnMetrics(ref platform, state, obj);
		FreeFormatDescriptors(ref platform, formatBlock, formatCount);
		FreeTitleArrayState(ref platform, titleArrayState);
		ClearInternal(ref platform, state, obj, TitleArrayStateKey);
		ClearInternal(ref platform, state, obj, TitleArray);
		var titleState = APTR.FromPointer(Read(ref platform, state, obj,
			TitleStateKey, 0));
		FreeTitleState(ref platform, titleState);
		ClearInternal(ref platform, state, obj, TitleStateKey);
		var selectionSignal = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		FreeSelectionSignalState(ref platform, selectionSignal);
		ClearInternal(ref platform, state, obj, SelectionSignalKey);
		var formatPolicy = APTR.FromPointer(Read(ref platform, state, obj,
			FormatPolicyKey, 0));
		FreeFormatPolicyState(ref platform, formatPolicy);
		ClearInternal(ref platform, state, obj, FormatPolicyKey);
		var fontState = APTR.FromPointer(Read(ref platform, state, obj,
			FontStateKey, 0));
		FreeFontState(ref platform, fontState);
		ClearInternal(ref platform, state, obj, FontStateKey);
		var redrawState = APTR.FromPointer(Read(ref platform, state, obj,
			RedrawStateKey, 0));
		FreeRedrawState(ref platform, redrawState);
		ClearInternal(ref platform, state, obj, RedrawStateKey);
		var viewportState = APTR.FromPointer(Read(ref platform, state, obj,
			ViewportStateKey, 0));
		FreeViewportState(ref platform, viewportState);
		ClearInternal(ref platform, state, obj, ViewportStateKey);
		var columnVisibilityState = APTR.FromPointer(Read(ref platform, state,
			obj, ColumnVisibilityKey, 0));
		FreeColumnVisibilityState(ref platform, columnVisibilityState);
		ClearInternal(ref platform, state, obj, ColumnVisibilityKey);
		var columnOrderState = APTR.FromPointer(Read(ref platform, state,
			obj, ColumnOrderKey, 0));
		FreeColumnOrderState(ref platform, columnOrderState);
		ClearInternal(ref platform, state, obj, ColumnOrderKey);
		FreeHScrollerState(ref platform, state, obj);
		var activeState = APTR.FromPointer(Read(ref platform, state, obj,
			ActiveStateKey, 0));
		FreeActiveState(ref platform, activeState);
		ClearInternal(ref platform, state, obj, ActiveStateKey);
		var poolPolicy = APTR.FromPointer(Read(ref platform, state, obj,
			PoolPolicyKey, 0));
		if (poolPolicy.IsNotNull && platform.IsMapped(poolPolicy,
			MuiListPoolPolicyState.Size))
		{
			platform.Clear(poolPolicy, MuiListPoolPolicyState.Size);
			platform.Free(poolPolicy, MuiListPoolPolicyState.Size);
		}
		ClearInternal(ref platform, state, obj, PoolPolicyKey);
		var interactionPolicy = APTR.FromPointer(Read(ref platform, state, obj,
			InteractionPolicyKey, 0));
		if (interactionPolicy.IsNotNull && platform.IsMapped(interactionPolicy,
			MuiListInteractionPolicyState.Size))
		{
			platform.Clear(interactionPolicy,
				MuiListInteractionPolicyState.Size);
			platform.Free(interactionPolicy,
				MuiListInteractionPolicyState.Size);
		}
		ClearInternal(ref platform, state, obj, InteractionPolicyKey);
		var clickState = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		if (clickState.IsNotNull && platform.IsMapped(clickState,
			MuiListClickState.Size))
		{
			platform.Clear(clickState, MuiListClickState.Size);
			platform.Free(clickState, MuiListClickState.Size);
		}
		ClearInternal(ref platform, state, obj, ClickStateKey);
		var hookPolicy = APTR.FromPointer(Read(ref platform, state, obj,
			HookPolicyKey, 0));
		if (hookPolicy.IsNotNull && platform.IsMapped(hookPolicy,
			MuiListHookPolicyState.Size))
		{
			platform.Clear(hookPolicy, MuiListHookPolicyState.Size);
			platform.Free(hookPolicy, MuiListHookPolicyState.Size);
		}
		ClearInternal(ref platform, state, obj, HookPolicyKey);
		var sortState = APTR.FromPointer(Read(ref platform, state, obj,
			SortStateKey, 0));
		if (sortState.IsNotNull && platform.IsMapped(sortState,
			MuiListSortState.Size))
		{
			platform.Clear(sortState, MuiListSortState.Size);
			platform.Free(sortState, MuiListSortState.Size);
		}
		ClearInternal(ref platform, state, obj, SortStateKey);
		var presentationPolicy = APTR.FromPointer(Read(ref platform, state, obj,
			PresentationPolicyKey, 0));
		if (presentationPolicy.IsNotNull && platform.IsMapped(presentationPolicy,
			MuiListPresentationPolicyState.Size))
		{
			platform.Clear(presentationPolicy,
				MuiListPresentationPolicyState.Size);
			platform.Free(presentationPolicy,
				MuiListPresentationPolicyState.Size);
		}
		ClearInternal(ref platform, state, obj, PresentationPolicyKey);
		if (index.IsNotNull && capacity != 0)
		{
			platform.Clear(index, capacity * SlotSize);
			platform.Free(index, capacity * SlotSize);
		}
		WriteHeaderIndex(ref platform, header, APTR.Null);
		WriteHeaderCount(ref platform, header, 0);
		platform.Clear(header, HeaderSize);
		platform.Free(header, HeaderSize);
		ClearInternal(ref platform, state, obj, ListHeaderKey);
	}

	// ---- Query ---------------------------------------------------------------

	// Bounded O(1) lookup. Resolves MUIV_List_GetEntry_Active, validates the
	// range, publishes the entry to the optional storage word, and returns it.
	public static APTR GetEntry<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int pos, APTR entryStorage)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		var entry = APTR.Null;
		if (header.IsNotNull)
		{
			var count = ReadHeaderCount(ref platform, header);
			var resolved = pos == GetEntryActive
				? ActiveIndex(ref platform, state, obj) : pos;
			if (resolved >= 0 && (uint)resolved < count)
				entry = SlotEntryAt(ref platform, header, (uint)resolved);
		}
		if (entryStorage.IsNotNull)
		{
			var entryValue = default(MuiListPointerSlotRecord);
			entryValue.Value = entry;
			MuiListPointerSlotCodec.Write(ref platform, entryStorage,
				entryValue);
		}
		return entry;
	}

	public static uint EntryCount<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		return header.IsNull ? 0 : ReadHeaderCount(ref platform, header);
	}

	// Resolve the MorphOS HScrollerVisibility policy without introducing a
	// managed delegate or collection. Auto is deliberately a strict overflow
	// test; equal content and viewport widths do not need a scrollbar.
	public static bool ResolveHScrollerVisibility(uint policy,
		uint contentWidth, uint viewWidth)
	{
		policy = NormalizeHScrollerPolicy(policy);
		if (policy == HScrollerAlways) return true;
		if (policy == HScrollerNever) return false;
		return contentWidth > viewWidth;
	}

	// The public attribute remains construction-only, while the derived
	// viewport state is intentionally queryable by Listview and future native
	// horizontal-scroller composition through this named record.
	internal static bool TryGetHScrollerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListHScrollerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HScrollerStateKey, 0));
		if (MuiListHScrollerStateCodec.TryRead(ref platform, block, out value))
		{
			value.Policy = NormalizeHScrollerPolicy(value.Policy);
			return true;
		}
		value = default;
		value.Magic = MuiListHScrollerState.Cookie;
		value.Policy = NormalizeHScrollerPolicy(Read(ref platform, state, obj,
			HScrollerVisibility, HScrollerAuto));
		return EnsureHScrollerState(ref platform, state, obj, value.Policy);
	}

	internal static bool SetHScrollerViewport<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint contentWidth, uint viewWidth)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryGetHScrollerState(ref platform, state, obj, out var value))
			return false;
		value.ContentWidth = contentWidth;
		value.ViewWidth = viewWidth;
		value.Visible = ResolveHScrollerVisibility(value.Policy, contentWidth,
			viewWidth) ? 1u : 0u;
		value.MaxScrollX = contentWidth > viewWidth ? contentWidth - viewWidth : 0;
		if (value.ScrollX > value.MaxScrollX) value.ScrollX = value.MaxScrollX;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HScrollerStateKey, 0));
		return MuiListHScrollerStateCodec.Write(ref platform, block, value);
	}

	internal static bool SetHScrollerScroll<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint requested)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryGetHScrollerState(ref platform, state, obj, out var value))
			return false;
		var target = requested > value.MaxScrollX ? value.MaxScrollX : requested;
		if (target == value.ScrollX) return true;
		value.ScrollX = target;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HScrollerStateKey, 0));
		if (!MuiListHScrollerStateCodec.Write(ref platform, block, value))
			return false;
		MuiHeadlessMemory.Mutated(ref platform, state);
		return true;
	}

	// Host/native qualification helper for the same coalesced refresh state
	// that MorphOS exposes only through the visible redraw side effect.
	public static uint RedrawRequests<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			RedrawStateKey, 0));
		return TryReadRedrawState(ref platform, block, out var redraw)
			? redraw.Requests : 0;
	}

	// Internal drag/drop seam for the future input dispatcher. MUIA_List_DropMark
	// is a read-only public result attribute, so callers cannot mutate it through
	// SetAttribute; this bounded method is the only producer-facing path and
	// keeps the insertion position in the guest attribute record.
	public static bool SetDropMark<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int position) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var count = ReadHeaderCount(ref platform, header);
		var target = position;
		if (target < DropMarkNone) target = DropMarkNone;
		if (target > unchecked((int)count)) target = unchecked((int)count);
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			DropMark, unchecked((uint)target), false)) return false;
		SetViewportDropMark(ref platform, state, obj,
			unchecked((uint)target));
		return true;
	}

	// Internal drag-sort seam for the future input dispatcher.  MorphOS keeps
	// drag sorting opt-in through MUIA_List_DragSortable and MUIA_List_DragType;
	// this producer validates both flags and uses the existing struct-backed
	// Move implementation so selection flags and ownership remain intact.
	public static bool DragMove<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int from, int to) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (PresentationPolicyValue(ref platform, state, obj, DragSortable, 0) == 0 ||
			PresentationPolicyValue(ref platform, state, obj, DragType,
				DragTypeNone) == DragTypeNone)
			return false;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var count = ReadHeaderCount(ref platform, header);
		CancelEditState(ref platform, state, obj);
		var source = from;
		var destination = to;
		if (source < 0 || destination < 0 ||
			(uint)source >= count || (uint)destination > count)
			return false;
		if ((SlotFlagsAt(ref platform, header, unchecked((uint)source)) &
			SlotSelected) != 0 && SelectedCount(ref platform, header) > 1)
			return DragMoveSelection(ref platform, state, obj, header, count,
				source, destination);
		if ((uint)destination == count)
			return MoveToEnd(ref platform, state, obj, header, count, source);
		return Move(ref platform, state, obj, source, destination);
	}

	private static bool MoveToEnd<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR header, uint count, int source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (source < 0 || (uint)source >= count) return false;
		if ((uint)source == count - 1) return true;
		var entry = SlotEntryAt(ref platform, header, unchecked((uint)source));
		var flags = SlotFlagsAt(ref platform, header, unchecked((uint)source));
		for (var index = unchecked((uint)source); index + 1 < count; index++)
			WriteSlot(ref platform, header, index,
				SlotEntryAt(ref platform, header, index + 1),
				SlotFlagsAt(ref platform, header, index + 1));
		WriteSlot(ref platform, header, count - 1, entry, flags);
		MuiHeadlessMemory.Mutated(ref platform, state);
		RequestMutationRedraw(ref platform, state, obj);
		return true;
	}

	// Move all selected entries as one stable group when the drag starts on a
	// selected row. The temporary guest buffer is an array of named
	// MuiListSlotState records, so the host never materializes managed entry or
	// flag arrays. Dropping below the anchor places the group after the target;
	// dropping above places it before the target, matching single-entry Move.
	private static bool DragMoveSelection<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR header, uint count, int source, int destination)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var selectedCount = SelectedCount(ref platform, header);
		if (selectedCount < 2 || source == destination) return true;
		var append = unchecked((uint)destination) == count;
		if (!append)
		{
			var targetFlags = SlotFlagsAt(ref platform, header,
				unchecked((uint)destination));
			if ((targetFlags & SlotSelected) != 0) return true;
		}
		if (count > uint.MaxValue / MuiListSlotState.Size) return false;
		var bytes = count * MuiListSlotState.Size;
		var scratch = MuiHeadlessMemory.Allocate(ref platform, bytes);
		if (scratch.IsNull) return false;
		var sourceCursor = default(MuiListSlotCursor);
		sourceCursor.Base = ReadHeaderIndex(ref platform, header);
		var scratchCursor = default(MuiListSlotCursor);
		scratchCursor.Base = scratch;
		for (var index = 0u; index < count; index++)
		{
			sourceCursor.Index = index;
			scratchCursor.Index = index;
			if (!MuiListSlotCursorCodec.TryGetEntry(ref platform, sourceCursor,
				out var sourceAddress) || !MuiListSlotCodec.TryRead(ref platform,
					sourceAddress, out var value) ||
				!MuiListSlotCursorCodec.TryGetEntry(ref platform, scratchCursor,
					out var scratchAddress) || !MuiListSlotCodec.Write(ref platform,
					scratchAddress, value))
			{
				platform.Clear(scratch, bytes);
				platform.Free(scratch, bytes);
				return false;
			}
		}

		var output = 0u;
		var beforeTarget = source > destination;
		if (append)
		{
			for (var index = 0u; index < count; index++)
				if (!WriteNextUnselected(ref platform, scratch, index,
					ref output, header))
				{
					platform.Clear(scratch, bytes);
					platform.Free(scratch, bytes);
					return false;
				}
			if (!WriteSelected(ref platform, scratch, count, ref output, header))
			{
				platform.Clear(scratch, bytes);
				platform.Free(scratch, bytes);
				return false;
			}
		}
		else if (beforeTarget)
		{
			for (var index = 0u; index < unchecked((uint)destination); index++)
				if (!WriteNextUnselected(ref platform, scratch, index,
					ref output, header))
				{
					platform.Clear(scratch, bytes);
					platform.Free(scratch, bytes);
					return false;
				}
			if (!WriteSelected(ref platform, scratch, count, ref output, header) ||
				!WriteUnselectedFrom(ref platform, scratch, count,
					unchecked((uint)destination), ref output, header))
			{
				platform.Clear(scratch, bytes);
				platform.Free(scratch, bytes);
				return false;
			}
		}
		else
		{
			for (var index = 0u; index <= unchecked((uint)destination); index++)
				if (!WriteNextUnselected(ref platform, scratch, index,
					ref output, header))
				{
					platform.Clear(scratch, bytes);
					platform.Free(scratch, bytes);
					return false;
				}
			if (!WriteSelected(ref platform, scratch, count, ref output, header) ||
				!WriteUnselectedFrom(ref platform, scratch, count,
					unchecked((uint)destination + 1u), ref output, header))
			{
				platform.Clear(scratch, bytes);
				platform.Free(scratch, bytes);
				return false;
			}
		}
		var complete = output == count;
		platform.Clear(scratch, bytes);
		platform.Free(scratch, bytes);
		if (!complete) return false;
		MuiHeadlessMemory.Mutated(ref platform, state);
		RequestMutationRedraw(ref platform, state, obj);
		return true;
	}

	private static uint SelectedCount<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = ReadHeaderCount(ref platform, header);
		var selected = 0u;
		for (var index = 0u; index < count; index++)
			if ((SlotFlagsAt(ref platform, header, index) & SlotSelected) != 0)
				selected++;
		return selected;
	}

	private static bool TryReadScratchSlot<TPlatform>(ref TPlatform platform,
		APTR scratch, uint index, out MuiListSlotState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		var cursor = default(MuiListSlotCursor);
		cursor.Base = scratch;
		cursor.Index = index;
		return MuiListSlotCursorCodec.TryGetEntry(ref platform, cursor,
			out var address) && MuiListSlotCodec.TryRead(ref platform, address,
			out value);
	}

	private static bool WriteNextUnselected<TPlatform>(ref TPlatform platform,
		APTR scratch, uint index, ref uint output, APTR header)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadScratchSlot(ref platform, scratch, index, out var value))
			return false;
		if ((value.Flags & SlotSelected) != 0) return true;
		return TryWriteSlot(ref platform, header, output++, value);
	}

	private static bool WriteSelected<TPlatform>(ref TPlatform platform,
		APTR scratch, uint count, ref uint output, APTR header)
		where TPlatform : struct, IMuiGuestMemory
	{
		for (var index = 0u; index < count; index++)
		{
			if (!TryReadScratchSlot(ref platform, scratch, index, out var value))
				return false;
			if ((value.Flags & SlotSelected) == 0) continue;
			if (!TryWriteSlot(ref platform, header, output++, value)) return false;
		}
		return true;
	}

	private static bool WriteUnselectedFrom<TPlatform>(ref TPlatform platform,
		APTR scratch, uint count, uint start, ref uint output, APTR header)
		where TPlatform : struct, IMuiGuestMemory
	{
		for (var index = start; index < count; index++)
		{
			if (!TryReadScratchSlot(ref platform, scratch, index, out var value))
				return false;
			if ((value.Flags & SlotSelected) != 0) continue;
			if (!TryWriteSlot(ref platform, header, output++, value)) return false;
		}
		return true;
	}

	// MorphOS 3.20 inline-editing seam.  A matching active session reuses its
	// guest editor; otherwise the base implementation creates a String.mui
	// editor initialized from the selected entry or StringArray column.  
	// Subclasses can overload the method later without changing the guest ABI.
	public static APTR CreateEditObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int row, int column, APTR entry)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsListBacked(Classify(ref platform, state, obj)) ||
			PresentationPolicyValue(ref platform, state, obj, Editable, 0) == 0)
			return APTR.Null;
		if (!TryResolveEditTarget(ref platform, state, obj, row, column,
			out var resolvedRow, out var resolvedColumn, out var stored))
			return APTR.Null;
		return CreateEditObjectRaw(ref platform, state, obj,
			resolvedRow, resolvedColumn, entry, stored);
	}

	private static APTR CreateEditObjectRaw<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int row, int column, APTR entry, APTR stored)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadEditState(ref platform, state, obj, out var current) &&
			current.Row == row &&
			current.Column == column)
		{
			PlaceEditObject(ref platform, state, obj, row, column,
				current.EditObject);
			return current.EditObject;
		}
		var source = entry.IsNotNull ? entry : stored;
		var hook = HookPolicyValue(ref platform, state, obj, ConstructHook);
		if (hook == HookStringArray)
		{
			var sourceColumn = DisplaySourceColumn(ref platform, state, obj,
				unchecked((uint)column));
			source = sourceColumn < MaximumArrayEntries
				? ArrayEntryAt(ref platform, source, sourceColumn) : APTR.Null;
		}
		if (source.IsNull || !platform.IsMapped(source, 1)) return APTR.Null;
		var editObject = APTR.FromPointer(
			MuiCommonControlCore.CreateInlineStringObjectRaw(ref platform,
				state, source));
		if (editObject.IsNotNull)
			PlaceEditObject(ref platform, state, obj, row, column, editObject);
		return editObject;
	}

	// Enter edit mode for one row/column.  There is exactly one guest-resident
	// session per list.  Starting another edit atomically retires the previous
	// default editor before installing the replacement.
	public static bool Edit<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int row, int column)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsListBacked(Classify(ref platform, state, obj)) ||
			PresentationPolicyValue(ref platform, state, obj, Editable, 0) == 0)
			return false;
		if (!TryResolveEditTarget(ref platform, state, obj, row, column,
			out var resolvedRow, out var resolvedColumn, out var entry)) return false;
		if (TryReadEditState(ref platform, state, obj, out var current) &&
			current.Row == resolvedRow && current.Column == resolvedColumn)
			return true;
		CancelEditState(ref platform, state, obj);
		var editObject = CreateEditObject(ref platform, state, obj,
			resolvedRow, resolvedColumn, entry);
		if (editObject.IsNull) return false;
		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListEditState.Size);
		if (block.IsNull)
		{
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, editObject);
			return false;
		}
		MuiListEditState editState = default;
		editState.Magic = EditStateCookie;
		editState.Row = resolvedRow;
		editState.Column = resolvedColumn;
		editState.Entry = entry;
		editState.EditObject = editObject;
		editState.Flags = 0;
		MuiListEditStateCodec.Write(ref platform, block, editState);
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			EditStateKey, block.Raw, false))
		{
			platform.Clear(block, MuiListEditState.Size);
			platform.Free(block, MuiListEditState.Size);
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, editObject);
			return false;
		}
		return true;
	}

	// Complete the current editor handshake.  Updating an arbitrary compound
	// entry belongs to the subclass's MUIM_List_EditDone override; the base
	// implementation only validates the ABI/session and retires its default
	// String object, matching the documented overload point.
	public static bool EditDone<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int row, int column, APTR entry, APTR editObject)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadEditState(ref platform, state, obj, out var current) ||
			current.Row != row || current.Column != column) return false;
		if (entry.IsNotNull && entry != current.Entry) return false;
		if (editObject.IsNotNull && editObject != current.EditObject)
			return false;
		if (!CommitDefaultStringEdit(ref platform, state, obj, current))
			return false;
		RefreshLineHeight(ref platform, state, obj);
		CancelEditState(ref platform, state, obj);
		return true;
	}

	// The base List class can commit the built-in String.mui editor and one
	// StringArray column. Compound entries and arbitrary subclass hooks retain
	// the documented EditDone overload point instead of being rewritten here.
	private static bool CommitDefaultStringEdit<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, MuiListEditState current)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hook = HookPolicyValue(ref platform, state, obj, ConstructHook);
		if (hook == HookString && current.Column == 0)
			return CommitStringEdit(ref platform, state, obj, current);
		if (hook == HookStringArray)
			return CommitStringArrayEdit(ref platform, state, obj, current);
		return true;
	}

	private static bool CommitStringEdit<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiListEditState current)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull || current.Row < 0) return false;
		var count = ReadHeaderCount(ref platform, header);
		if ((uint)current.Row >= count ||
			SlotEntryAt(ref platform, header, (uint)current.Row) !=
			current.Entry) return false;
		if (!TryReadStringContentsRaw(ref platform, state,
			current.EditObject, out var contentsRaw)) return false;
		var contents = APTR.FromPointer(contentsRaw);
		var pool = PoolFor(ref platform, state, obj);
		var replacement = Construct(ref platform, state, obj, contents, pool,
			out var replacementOwnership);
		if (replacement.IsNull || replacementOwnership == 0) return false;
		var slotFlags = SlotFlagsAt(ref platform, header, (uint)current.Row);
		var oldOwnership = slotFlags & (SlotOwnedString | SlotOwnedStringArray |
			SlotOwnedRecord);
		Destruct(ref platform, state, obj, current.Entry,
			oldOwnership, pool);
		WriteSlot(ref platform, header, (uint)current.Row, replacement,
			(slotFlags & SlotSelected) | replacementOwnership);
		Publish(ref platform, state, obj, count);
		return true;
	}

	private static bool CommitStringArrayEdit<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiListEditState current)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull || current.Row < 0 || current.Column < 0) return false;
		var count = ReadHeaderCount(ref platform, header);
		if ((uint)current.Row >= count ||
			SlotEntryAt(ref platform, header, (uint)current.Row) !=
			current.Entry) return false;
		var oldEntry = current.Entry;
		if (!TryReadStringArrayCount(ref platform, oldEntry, out var columns) ||
			(uint)current.Column >= FormatColumnCount(ref platform, state, obj))
			return false;
		var sourceColumn = DisplaySourceColumn(ref platform, state, obj,
			unchecked((uint)current.Column));
		if (sourceColumn >= columns) return false;
		if (!TryReadStringContentsRaw(ref platform, state,
			current.EditObject, out var contentsRaw)) return false;

		var tableSize = (columns + 1) * MuiListPointerSlotRecord.Size;
		var source = MuiHeadlessMemory.Allocate(ref platform, tableSize);
		if (source.IsNull) return false;
		var oldCursor = default(MuiListPointerSlotCursor);
		oldCursor.Base = oldEntry;
		var sourceCursor = default(MuiListPointerSlotCursor);
		sourceCursor.Base = source;
		for (var column = 0u; column < columns; column++)
		{
			oldCursor.Index = column;
			sourceCursor.Index = column;
			var value = default(MuiListPointerSlotRecord);
			if (column == sourceColumn)
				value.Value = APTR.FromPointer(contentsRaw);
			else if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
				oldCursor, out var oldSlot) ||
				!MuiListPointerSlotCodec.TryRead(ref platform, oldSlot, out value))
			{
				platform.Clear(source, tableSize);
				platform.Free(source, tableSize);
				return false;
			}
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
				sourceCursor, out var destinationSlot))
			{
				platform.Clear(source, tableSize);
				platform.Free(source, tableSize);
				return false;
			}
			if (!MuiListPointerSlotCodec.Write(ref platform, destinationSlot,
				value))
			{
				platform.Clear(source, tableSize);
				platform.Free(source, tableSize);
				return false;
			}
		}
		sourceCursor.Index = columns;
		if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
			sourceCursor, out var terminator))
		{
			platform.Clear(source, tableSize);
			platform.Free(source, tableSize);
			return false;
		}
		if (!MuiListPointerSlotCodec.Write(ref platform, terminator, default))
		{
			platform.Clear(source, tableSize);
			platform.Free(source, tableSize);
			return false;
		}

		var pool = PoolFor(ref platform, state, obj);
		var replacement = Construct(ref platform, state, obj, source, pool,
			out var replacementOwnership);
		platform.Clear(source, tableSize);
		platform.Free(source, tableSize);
		if (replacement.IsNull || replacementOwnership == 0) return false;

		var slotFlags = SlotFlagsAt(ref platform, header, (uint)current.Row);
		var oldOwnership = slotFlags & (SlotOwnedString | SlotOwnedStringArray |
			SlotOwnedRecord);
		Destruct(ref platform, state, obj, oldEntry, oldOwnership, pool);
		WriteSlot(ref platform, header, (uint)current.Row, replacement,
			(slotFlags & SlotSelected) | replacementOwnership);
		Publish(ref platform, state, obj, count);
		return true;
	}

	// The edit handshake already identifies the editor object. Read its normal
	// guest String contents attribute here so the List commit seam remains
	// independent from the renderer-heavy common-control implementation.
	private static bool TryReadStringContentsRaw<TPlatform>(ref TPlatform platform,
		APTR state, APTR editor, out uint contents)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		contents = 0;
		if (editor.IsNull || !MuiHeadlessObjectCore.GetAttribute(ref platform,
			state, editor, StringContents, out var raw)) return false;
		if (raw == 0 || !platform.IsMapped(APTR.FromPointer(raw), 1)) return false;
		contents = raw;
		return true;
	}

	// Finish, abort, or move the current edit session.  Prev/Up and Next/Down
	// share the MorphOS selectors; an out-of-range navigation target simply
	// leaves the list out of edit mode after the current edit is accepted.
	public static bool EndEdit<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint mode) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadEditState(ref platform, state, obj, out var current))
			return false;
		if (mode == EndEditAbort)
		{
			CancelEditState(ref platform, state, obj);
			return true;
		}
		if (mode == EndEditDone)
			return EditDone(ref platform, state, obj, current.Row, current.Column,
				current.Entry, current.EditObject);
		if (mode != EndEditPrev && mode != EndEditNext && mode != EndEditUp &&
			mode != EndEditDown) return false;
		var delta = mode == EndEditPrev || mode == EndEditUp ? -1 : 1;
		var target = current.Row + delta;
		var column = current.Column;
		if (!EditDone(ref platform, state, obj, current.Row, current.Column,
			current.Entry, current.EditObject))
			return false;
		if (target < 0 || (uint)target >= EntryCount(ref platform, state, obj))
			return true;
		return Edit(ref platform, state, obj, target, column);
	}

	private static uint EffectiveLineHeight<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var baseline = BaseLineHeight(ref platform, state, obj);
		if (PresentationPolicyValue(ref platform, state, obj,
			AutoLineHeight, 0) == 0)
			return baseline;
		var value = LineHeightCursor(ref platform, state, obj,
			Read(ref platform, state, obj, LineHeight, baseline));
		if (value < baseline) value = baseline;
		return value > MaximumLineHeight ? MaximumLineHeight : value;
	}

	private static uint BaseLineHeight<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var value = PresentationPolicyValue(ref platform, state, obj,
			MinLineHeight, RowHeight);
		if (value < RowHeight) value = RowHeight;
		return value > MaximumLineHeight ? MaximumLineHeight : value;
	}

	private static bool RefreshLineHeight<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var value = BaseLineHeight(ref platform, state, obj);
		if (PresentationPolicyValue(ref platform, state, obj,
			AutoLineHeight, 0) != 0)
		{
			var lines = ComputeAutoLines(ref platform, state, obj);
			if (lines > 1 && value > MaximumLineHeight / lines)
				value = MaximumLineHeight;
			else
				value *= lines;
		}
		SetInternal(ref platform, state, obj, LineHeight, value);
		SetViewportLineHeight(ref platform, state, obj, value);
		return true;
	}

	private static uint ComputeAutoLines<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return 1;
		var count = ReadHeaderCount(ref platform, header);
		var hook = HookPolicyValue(ref platform, state, obj, ConstructHook);
		var maximum = 1u;
		for (var row = 0u; row < count && row < MaximumEntries; row++)
		{
			var entry = SlotEntryAt(ref platform, header, row);
			if (hook == HookStringArray)
			{
				var cursor = default(MuiListPointerSlotCursor);
				cursor.Base = entry;
				for (var column = 0u; column < MaximumDrawColumns; column++)
				{
					cursor.Index = column;
					if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
						cursor, out var address)) break;
					if (!MuiListPointerSlotCodec.TryRead(ref platform, address,
						out var slotValue)) break;
					var text = slotValue.Value;
					if (text.IsNull) break;
					var lines = TextLineCount(ref platform, text);
					if (lines > maximum) maximum = lines;
				}
			}
			else if (hook == 0 || hook == HookString)
			{
				var lines = TextLineCount(ref platform, entry);
				if (lines > maximum) maximum = lines;
			}
		}
		return maximum;
	}

	private static uint TextLineCount<TPlatform>(ref TPlatform platform,
		APTR text) where TPlatform : struct, IMuiGuestMemory
	{
		if (text.IsNull) return 1;
		var lines = 1u;
		for (var index = 0u; index < MaximumStringLength; index++)
		{
			if (text.Raw > uint.MaxValue - index) break;
			var address = APTR.FromPointer(text.Raw + index);
			if (!platform.IsMapped(address, 1)) break;
			var value = platform.ReadUInt8(address, 0);
			if (value == 0) break;
			if (value == (byte)'\n' && lines < MaximumAutoLines) lines++;
		}
		return lines;
	}

	internal static uint TitleRowCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var titleState = APTR.FromPointer(Read(ref platform, state, obj,
			TitleArrayStateKey, 0));
		if (titleState.IsNotNull)
			return TryReadTitleArrayStateBlock(ref platform, titleState,
				out var value) && value.Count != 0 ? 1u : 0u;
		return TitleValueCursor(ref platform, state, obj) == 0 ? 0u : 1u;
	}

	internal static uint TitleValueCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			TitleStateKey, 0));
		return TryReadTitleState(ref platform, block, out var value)
			? value.Value
			: Read(ref platform, state, obj, Title, 0);
	}

	internal static bool TryGetTitleState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListTitleState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			TitleStateKey, 0));
		return TryReadTitleState(ref platform, block, out value);
	}

	private static short AdjustedHeight<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint lineHeight)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var rows = EntryCount(ref platform, state, obj);
		if (TitleRowCount(ref platform, state, obj) != 0 && rows != uint.MaxValue)
			rows++;
		if (rows == 0) return unchecked((short)lineHeight);
		if (rows > MaximumAdjustHeight / lineHeight)
			return unchecked((short)MaximumAdjustHeight);
		var total = rows * lineHeight;
		return unchecked((short)(total > MaximumAdjustHeight
			? MaximumAdjustHeight : total));
	}

	private static bool CopyTitleArrayPointers<TPlatform>(ref TPlatform platform,
		MuiListTitleArrayState value, APTR destination)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (destination.IsNull || !platform.IsMapped(destination,
			(MaximumDrawColumns + 1) * MuiListPointerSlotRecord.Size)) return false;
		var columns = value.Count > MaximumDrawColumns
			? MaximumDrawColumns : value.Count;
		var source = value.Pointers;
		var sourceCursor = default(MuiListPointerSlotCursor);
		sourceCursor.Base = source;
		var destinationCursor = default(MuiListPointerSlotCursor);
		destinationCursor.Base = destination;
		for (var column = 0u; column < columns; column++)
		{
			sourceCursor.Index = column;
			destinationCursor.Index = column;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
				sourceCursor, out var sourceSlot) ||
				!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
					destinationCursor, out var destinationSlot)) return false;
			if (!MuiListPointerSlotCodec.TryRead(ref platform, sourceSlot,
				out var sourceValue)) return false;
			var destinationValue = default(MuiListPointerSlotRecord);
			destinationValue.Value = sourceValue.Value;
			if (!MuiListPointerSlotCodec.Write(ref platform, destinationSlot,
				destinationValue)) return false;
		}
		destinationCursor.Index = columns;
		if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
			destinationCursor, out var terminator)) return false;
		var terminatorValue = default(MuiListPointerSlotRecord);
		if (!MuiListPointerSlotCodec.Write(ref platform, terminator,
			terminatorValue)) return false;
		return true;
	}

	// List geometry is derived from the guest-resident backbone. One fixed row
	// cell is the fallback metric; Area owns the actual rectangle and render-info
	// lifecycle. Full font/column differential behavior remains later work.
	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR storage) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!IsListBacked(Classify(ref platform, state, obj)) ||
			!platform.IsMapped(storage, 12)) return false;
		RefreshLineHeight(ref platform, state, obj);
		var lineHeight = EffectiveLineHeight(ref platform, state, obj);
		var values = MuiAreaLayoutCore.ComputeMinMax(ref platform, state, obj);
		if (values.MinWidth < (short)lineHeight)
			values.MinWidth = (short)lineHeight;
		if (values.MinHeight < (short)lineHeight)
			values.MinHeight = (short)lineHeight;
		if (values.MaxWidth < values.MinWidth) values.MaxWidth = values.MinWidth;
		if (values.MaxHeight < values.MinHeight) values.MaxHeight = values.MinHeight;
		if (values.DefWidth < values.MinWidth) values.DefWidth = values.MinWidth;
		if (values.DefHeight < values.MinHeight) values.DefHeight = values.MinHeight;
		if (PresentationPolicyValue(ref platform, state, obj,
			AdjustHeight, 0) != 0)
		{
			var fixedHeight = AdjustedHeight(ref platform, state, obj,
				lineHeight);
			values.MinHeight = fixedHeight;
			values.MaxHeight = fixedHeight;
			values.DefHeight = fixedHeight;
		}
		if (PresentationPolicyValue(ref platform, state, obj,
			AdjustWidth, 0) != 0)
		{
			if (!TryAdjustedWidth(ref platform, state, obj,
				out var fixedWidth)) return false;
			if (fixedWidth != 0)
			{
				values.MinWidth = fixedWidth;
				values.MaxWidth = fixedWidth;
				values.DefWidth = fixedWidth;
			}
		}
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage, values);
	}

	// MUIA_List_AdjustWidth is construction-only. The documented width is the
	// widest displayed row, so measure the same bounded display strings that the
	// Draw path consumes. This keeps the sizing seam compatible with String,
	// StringArray, and arbitrary display hooks without introducing a host string
	// or managed collection. The platform text metric is the sole font policy.
	private static bool TryAdjustedWidth<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out short width)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		width = 0;
		var count = EntryCount(ref platform, state, obj);
		var titleRows = TitleRowCount(ref platform, state, obj);
		if (count == 0 && titleRows == 0) return true;
		var columns = FormatColumnCount(ref platform, state, obj);
		if (columns == 0) columns = 1;
		if (columns > MaximumDrawColumns) columns = MaximumDrawColumns;
		if (!TryAllocateDisplayArray(ref platform, out var displayStorage))
			return false;
		var displayArray = displayStorage.Array;
		var font = FontCursor(ref platform, state, obj);
		var header = Header(ref platform, state, obj);
		if (header.IsNull)
		{
			ClearDisplayArray(ref platform, displayStorage);
			FreeDisplayArray(ref platform, displayStorage);
			return false;
		}
		var widest = 0u;
		var titleStateBlock = APTR.FromPointer(Read(ref platform, state, obj,
			TitleArrayStateKey, 0));
		if (titleRows != 0)
		{
			ClearDisplayArray(ref platform, displayStorage);
			var displayed = false;
			if (titleStateBlock.IsNotNull &&
				TryReadTitleArrayStateBlock(ref platform, titleStateBlock,
					out var titleArrayState) && titleArrayState.Count != 0)
				displayed = CopyTitleArrayPointers(ref platform, titleArrayState,
					displayArray);
			else
			{
				var titleRaw = TitleValueCursor(ref platform, state, obj);
				// MUIA_List_Title=TRUE is the custom-hook form: pass a NULL
				// entry even when the list has no data rows.
				var titleEntry = titleRaw == 1 ? APTR.Null :
					APTR.FromPointer(titleRaw);
				displayed = Display(ref platform, state, obj, titleEntry,
					displayArray, -1);
			}
			if (displayed && !TryMeasureDisplayArray(ref platform, state, obj,
				displayArray, font, columns, out widest))
			{
				ClearDisplayArray(ref platform, displayStorage);
				FreeDisplayArray(ref platform, displayStorage);
				return false;
			}
		}
		for (var row = 0u; row < count; row++)
		{
			ClearDisplayArray(ref platform, displayStorage);
			var entry = SlotEntryAt(ref platform, header, row);
			if (!Display(ref platform, state, obj, entry, displayArray,
				unchecked((int)row)) || !TryMeasureDisplayArray(ref platform, state,
				obj, displayArray, font, columns, out var rowWidth))
			{
				ClearDisplayArray(ref platform, displayStorage);
				FreeDisplayArray(ref platform, displayStorage);
				return false;
			}
			if (rowWidth > widest) widest = rowWidth;
		}
		ClearDisplayArray(ref platform, displayStorage);
		FreeDisplayArray(ref platform, displayStorage);
		if (widest == 0) return true;
		if (widest > MaximumAdjustWidth) widest = MaximumAdjustWidth;
		width = unchecked((short)widest);
		return true;
	}

	private static bool TryMeasureDisplayArray<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR displayArray, APTR font, uint columns,
		out uint width) where TPlatform : struct, IMuiLayoutPlatform
	{
		width = 0;
		for (var column = 0u; column < columns; column++)
		{
			var sourceColumn = DisplaySourceColumn(ref platform, state, obj,
				column);
			var text = APTR.Null;
			if (sourceColumn < MaximumArrayEntries)
			{
				var cursor = default(MuiListPointerSlotCursor);
				cursor.Base = displayArray;
				cursor.Index = sourceColumn;
				if (MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
					out var displaySlot) && MuiListPointerSlotCodec.TryRead(ref platform,
					displaySlot,
					out var displayValue)) text = displayValue.Value;
			}
			if (text.IsNotNull)
			{
				if (!TryReadCStringLength(ref platform, text,
					MaximumStringLength, out var length)) return false;
				var measured = platform.TextWidth(APTR.Null, font, text,
					unchecked((int)length));
				if (measured > 0) width = SaturatingAdd(width,
					unchecked((uint)measured));
			}
			if (column + 1 < columns)
				width = SaturatingAdd(width, ColumnDelta(ref platform, state, obj,
					column));
			if (width >= MaximumAdjustWidth) return true;
		}
		return true;
	}

	private static bool HasContentWidthDescriptors<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint columns)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		for (var column = 0u; column < columns; column++)
		{
			var flags = DescriptorValue(ref platform, state, obj, column,
				MuiListFormatField.Flags, 0);
			if ((flags & (DescriptorMinContent | DescriptorMaxContent |
				DescriptorWeightContent)) != 0)
				return true;
		}
		return false;
	}

	private static uint ColumnMetric<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int width, uint column)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (width < 0) return 0;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnMetricsKey, 0));
		if (!MuiListColumnMetricsStateCodec.TryRead(ref platform, block,
			out var value) ||
			value.Width != unchecked((uint)width) || column >= value.Columns)
			return 0;
		var values = value.Values;
		if (!platform.IsMapped(values, value.Columns *
			MuiListColumnMetricValue.Size)) return 0;
		var cursor = default(MuiListColumnMetricCursor);
		cursor.Base = values;
		cursor.Index = column;
		return MuiListColumnMetricCursorCodec.TryGetEntry(ref platform, cursor,
			out var slot) && MuiListColumnMetricCodec.TryRead(ref platform, slot,
			out var metric) ? metric.Value : 0;
	}

	// Publish the MorphOS List pixel viewport from the normalized guest row
	// state. TopPixel follows the first data row; title rows occupy viewport and
	// total space but do not move the data cursor. Saturating arithmetic keeps
	// malformed, very large entry counts from wrapping the public ULONGs.
	private static bool RefreshViewportState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var lineHeight = EffectiveLineHeight(ref platform, state, obj);
		if (lineHeight == 0) lineHeight = 1;
		var entries = EntryCount(ref platform, state, obj);
		var visible = Read(ref platform, state, obj, Visible, 0);
		var rawDropMark = Read(ref platform, state, obj, DropMark,
			unchecked((uint)DropMarkNone));
		var dropMark = unchecked((int)rawDropMark);
		if (dropMark < DropMarkNone) dropMark = DropMarkNone;
		if (dropMark > unchecked((int)entries)) dropMark = unchecked((int)entries);
		if (dropMark != unchecked((int)rawDropMark))
			SetInternal(ref platform, state, obj, DropMark,
				unchecked((uint)dropMark));
		var firstRaw = unchecked((int)Read(ref platform, state, obj, First, 0));
		var first = firstRaw < 0 ? 0 : firstRaw;
		if (visible == VisibleOff)
		{
			// The hidden projection carries the MorphOS off sentinel for both
			// row attributes, even if a stale caller value was present before the
			// visibility transition.
			first = -1;
			SetInternal(ref platform, state, obj, First, VisibleOff);
		}
		else if (entries == 0)
		{
			// Normalize the guest cursor at the same boundary as the empty-list
			// Active sentinel; a cleared, previously scrolled list must not retain
			// a stale First value or TopPixel projection.
			first = visible == VisibleOff ? -1 : 0;
			SetInternal(ref platform, state, obj, First,
				unchecked((uint)first));
		}
		else if (visible != 0 && (uint)first >
			(entries > visible ? entries - visible : 0u))
		{
			// A removal can shrink the legal viewport range without a Layout call.
			// Clamp only positive cursors; the documented -1 First sentinel remains
			// intact while its pixel projection stays at zero.
			var maxFirst = entries > visible ? entries - visible : 0u;
			first = unchecked((int)maxFirst);
			SetInternal(ref platform, state, obj, First,
				unchecked((uint)first));
		}
		var titleRows = TitleRowCount(ref platform, state, obj);
		var value = default(MuiListViewportState);
		value.Magic = ViewportStateCookie;
		var firstPixel = first < 0 ? 0u : unchecked((uint)first);
		value.First = unchecked((uint)first);
		value.LineHeight = lineHeight;
		value.Visible = visible;
		value.DropMark = unchecked((uint)dropMark);
		value.TopPixel = SaturatingMultiply(firstPixel, lineHeight);
		var visibleRows = visible == VisibleOff ? 0u : visible;
		value.VisiblePixel = SaturatingMultiply(
			SaturatingAdd(visibleRows, titleRows), lineHeight);
		value.TotalPixel = SaturatingMultiply(
			SaturatingAdd(entries, titleRows), lineHeight);

		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ViewportStateKey, 0));
		if (!TryReadViewportState(ref platform, block, out _))
		{
			block = MuiHeadlessMemory.Allocate(ref platform,
				MuiListViewportState.Size);
			if (block.IsNotNull)
			{
				WriteViewportState(ref platform, block, value);
				if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
					ViewportStateKey, block.Raw, false))
				{
					platform.Clear(block, MuiListViewportState.Size);
					platform.Free(block, MuiListViewportState.Size);
					block = APTR.Null;
				}
			}
		}
		if (block.IsNotNull) WriteViewportState(ref platform, block, value);
		SetInternal(ref platform, state, obj, TopPixel, value.TopPixel);
		SetInternal(ref platform, state, obj, VisiblePixel, value.VisiblePixel);
		SetInternal(ref platform, state, obj, TotalPixel, value.TotalPixel);
		return true;
	}

	private static bool SetViewportLineHeight<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint lineHeight)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ViewportStateKey, 0));
		if (!TryReadViewportState(ref platform, block, out var value))
			return false;
		value.LineHeight = lineHeight;
		WriteViewportState(ref platform, block, value);
		return true;
	}

	private static bool SetViewportDropMark<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint dropMark)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ViewportStateKey, 0));
		if (!TryReadViewportState(ref platform, block, out var value))
			return false;
		value.DropMark = dropMark;
		WriteViewportState(ref platform, block, value);
		return true;
	}

	// Keep the public pixel metrics coherent after a scroller changes First
	// without a full layout pass. The state remains the named guest-resident
	// MuiListViewportState record; callers never need to mirror its fields or
	// address individual words themselves.
	internal static bool RefreshViewportMetrics<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		RefreshViewportState(ref platform, state, obj);

	internal static bool TryGetViewportState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListViewportState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ViewportStateKey, 0));
		return block.IsNotNull && TryReadViewportState(ref platform, block,
			out value);
	}

	// Composite consumers use the named cursor when the viewport record exists;
	// the raw attribute is only a construction/early-lifecycle fallback.
	internal static uint FirstCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryGetViewportState(ref platform, state, obj, out var value)
			? value.First
			: Read(ref platform, state, obj, First, 0);

	// The visible row capacity follows the same publication boundary as First.
	// Keep the raw attribute only as a fallback before the first viewport record
	// exists or while Layout is constructing the next publication.
	internal static uint VisibleCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryGetViewportState(ref platform, state, obj, out var value)
			? value.Visible
			: Read(ref platform, state, obj, Visible, 0);

	// DropMark is a derived drag insertion cue. Prefer the named viewport record
	// after publication; raw state remains the early-lifecycle and transition
	// fallback used before a viewport exists.
	internal static uint DropMarkCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryGetViewportState(ref platform, state, obj, out var value)
			? value.DropMark
			: Read(ref platform, state, obj, DropMark,
				unchecked((uint)DropMarkNone));

	// Effective line height is a derived projection. Prefer the named viewport
	// record once it exists; the raw attribute remains only as an early-lifecycle
	// fallback before the first viewport publication.
	private static uint LineHeightCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryGetViewportState(ref platform, state, obj, out var value) &&
			value.LineHeight != 0 ? value.LineHeight : fallback;

	private static uint SaturatingMultiply(uint left, uint right) =>
		left != 0 && right > uint.MaxValue / left
			? uint.MaxValue : left * right;

	// Resolve explicit -1 width limits from the widest displayed entry in each
	// derived column.  The measurement block is rebuilt on every Layout after
	// entry/format changes have invalidated the previous named guest record.
	// All temporary buffers are guest allocations; no managed collection or
	// runtime object participates in the measurement pass.
	private static bool RefreshColumnMetrics<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int width)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var columns = GeometryColumnCount(ref platform, state, obj);
		if (!HasContentWidthDescriptors(ref platform, state, obj, columns))
		{
			FreeColumnMetrics(ref platform, state, obj);
			return true;
		}

		FreeColumnMetrics(ref platform, state, obj);
		var valueBytes = columns * MuiListColumnMetricValue.Size;
		var values = MuiHeadlessMemory.Allocate(ref platform, valueBytes);
		if (values.IsNull) return true;
		platform.Clear(values, valueBytes);
		if (!TryAllocateDisplayArray(ref platform, out var displayStorage))
		{
			platform.Clear(values, valueBytes);
			platform.Free(values, valueBytes);
			return true;
		}
		var displayArray = displayStorage.Array;

		var header = Header(ref platform, state, obj);
		var count = header.IsNull ? 0u : ReadHeaderCount(ref platform, header);
		var font = FontCursor(ref platform, state, obj);
		var metricCursor = default(MuiListColumnMetricCursor);
		metricCursor.Base = values;
		for (var row = 0u; row < count && row < MaximumEntries; row++)
		{
			ClearDisplayArray(ref platform, displayStorage);
			var entry = SlotEntryAt(ref platform, header, row);
			if (!Display(ref platform, state, obj, entry, displayArray,
				unchecked((int)row))) continue;
			for (var column = 0u; column < columns; column++)
			{
				var sourceColumn = DisplaySourceColumn(ref platform, state, obj,
					column);
				var text = APTR.Null;
				if (sourceColumn < MaximumArrayEntries)
				{
					var cursor = default(MuiListPointerSlotCursor);
					cursor.Base = displayArray;
					cursor.Index = sourceColumn;
					if (MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
						out var displaySlot) && MuiListPointerSlotCodec.TryRead(ref platform,
						displaySlot,
						out var displayValue)) text = displayValue.Value;
				}
				if (text.IsNull || !TryReadCStringLength(ref platform, text,
					MaximumStringLength, out var length)) continue;
				var measured = platform.TextWidth(APTR.Null, font, text,
					unchecked((int)length));
				if (measured <= 0) continue;
				metricCursor.Index = column;
				if (!MuiListColumnMetricCursorCodec.TryGetEntry(ref platform,
					metricCursor, out var slot) ||
					!MuiListColumnMetricCodec.TryRead(ref platform, slot,
					out var metric)) continue;
				if (unchecked((uint)measured) > metric.Value)
				{
					metric.Value = unchecked((uint)measured);
					if (!MuiListColumnMetricCodec.Write(ref platform, slot,
						metric)) return true;
				}
			}
		}
		ClearDisplayArray(ref platform, displayStorage);
		FreeDisplayArray(ref platform, displayStorage);

		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListColumnMetricsState.Size);
		if (block.IsNull)
		{
			platform.Clear(values, valueBytes);
			platform.Free(values, valueBytes);
			return true;
		}
		var metrics = default(MuiListColumnMetricsState);
		metrics.Magic = ColumnMetricsCookie;
		metrics.Width = unchecked((uint)(width < 0 ? 0 : width));
		metrics.Columns = columns;
		metrics.Values = values;
		MuiListColumnMetricsStateCodec.Write(ref platform, block, metrics);
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ColumnMetricsKey, block.Raw, false))
		{
			platform.Clear(block, MuiListColumnMetricsState.Size);
			platform.Free(block, MuiListColumnMetricsState.Size);
			platform.Clear(values, valueBytes);
			platform.Free(values, valueBytes);
			return true;
		}
		return true;
	}

	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!IsListBacked(Classify(ref platform, state, obj)))
			return false;
		if (!MuiAreaLayoutCore.Layout(ref platform, state, obj, left, top, width,
			height)) return false;
		RefreshLineHeight(ref platform, state, obj);
		var lineHeight = EffectiveLineHeight(ref platform, state, obj);
		var notVisible = height <= 0;
		var rows = notVisible ? VisibleOff : unchecked((uint)(height /
			(int)lineHeight));
		// A neutral title row consumes one visible line when MUIA_List_Title is
		// set, so the published data-visible count excludes it.
		if (!notVisible && rows > 0 &&
			TitleRowCount(ref platform, state, obj) != 0) rows--;
		// MUIA_List_Visible describes the geometry's row capacity, not the
		// current entry count. MorphOS keeps the full capacity for short lists;
		// drawing and hit-testing still stop at the named entry count.
		var count = EntryCount(ref platform, state, obj);
		var wasNotVisible = Read(ref platform, state, obj, Visible, 0) ==
			VisibleOff;
		SetInternal(ref platform, state, obj, Visible, rows);
		var first = unchecked((int)Read(ref platform, state, obj, First, 0));
		var active = ActiveIndex(ref platform, state, obj);
		// AutoVisible is a display-time policy: when disabled, laying out a list
		// keeps the caller's first row even if the active entry is elsewhere. A
		// later Active setter still scrolls immediately through ApplyActive.
		var autoVisible = PresentationPolicyValue(ref platform, state, obj,
			AutoVisible, 0) != 0;
		var firstForLayout = first < 0 && (autoVisible || wasNotVisible)
			? 0 : first;
		var normalized = notVisible ? -1 : NormalizeFirst(ref platform, state,
			obj, firstForLayout,
			autoVisible ? active : unchecked((int)ActiveOff), count);
		SetInternal(ref platform, state, obj, First, unchecked((uint)normalized));
		var contentLayoutWidth = ContentLayoutWidth(ref platform, state, obj, width);
		RefreshColumnMetrics(ref platform, state, obj, contentLayoutWidth);
		RefreshViewportState(ref platform, state, obj);
		return InstallColumnLayout(ref platform, state, obj, contentLayoutWidth);
	}

	public static bool Draw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!IsListBacked(Classify(ref platform, state, obj)) ||
			!MuiAreaLayoutCore.Draw(ref platform, state, obj, flags)) return false;
		return DrawRows(ref platform, state, obj);
	}

	// Hit-test the bounded List viewport and publish the public
	// MUI_List_TestPos_Result layout in guest memory. Coordinates are relative
	// to the List content rectangle, matching the MUIM_List_TestPos contract.
	// The implementation deliberately reports only geometry that the current
	// integer renderer owns: row index, visible column, cell-relative offsets,
	// and the four public outside-cell flags. No display-hook or font callback is
	// needed for the hit-test path.
	public static bool TestPos<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int x, int y, APTR result)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryTestPos(ref platform, state, obj, x, y, out var value))
			return false;
		return MuiListTestPosResultCodec.Write(ref platform, result, value);
	}

	// Struct-first hit-test seam used by composite controls.  Production method
	// dispatch publishes the value through TestPos above; Listview input can
	// consume this named result directly without allocating a temporary guest
	// buffer or re-decoding the public record.
	internal static bool TryTestPos<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int x, int y, out MuiListTestPosResult value)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		value = default;
		if (!IsListBacked(Classify(ref platform, state, obj))) return false;

		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
			out var areaGeometry)) return false;
		var width = areaGeometry.Width;
		var height = areaGeometry.Height;
		var contentLayoutWidth = ContentLayoutWidth(ref platform, state, obj,
			width);
		var scrollX = unchecked((int)HorizontalScrollX(ref platform, state, obj));
		// The public method receives object-local coordinates. Keep the local
		// origin explicit here so a future outer composite can translate its
		// event before forwarding the method.
		var flags = 0u;
		var entry = -1;
		var column = -1;
		var xoffset = 0;
		var yoffset = 0;
		if (x < 0) flags |= TestPosLeft;
		else if (width <= 0 || x >= width) flags |= TestPosRight;
		if (y < 0) flags |= TestPosAbove;
		else if (height <= 0 || y >= height) flags |= TestPosBelow;

		if (width > 0 && height > 0 && x >= 0 && x < width && y >= 0 &&
			y < height)
		{
			var lineHeight = EffectiveLineHeight(ref platform, state, obj);
			var rows = unchecked((uint)(height / (int)lineHeight));
			var titleRows = TitleRowCount(ref platform, state, obj) != 0 &&
				rows != 0 ? 1u : 0u;
			var row = unchecked((uint)y) / lineHeight;
			if (row < titleRows)
			{
				yoffset = y - unchecked((int)(row * lineHeight +
					lineHeight / 2));
				var contentX = scrollX > int.MaxValue - x
					? int.MaxValue : x + scrollX;
				ResolveTestPosColumn(ref platform, state, obj, contentLayoutWidth, contentX,
					x, ref flags, out column, out xoffset);
			}
			else
			{
				var dataRow = row - titleRows;
				var firstRaw = FirstCursor(ref platform, state, obj);
				var first = unchecked((int)firstRaw);
				if (first < 0) first = 0;
				var count = EntryCount(ref platform, state, obj);
				var candidate = unchecked((uint)first) + dataRow;
				if (candidate < count)
				{
					entry = unchecked((int)candidate);
					yoffset = y - unchecked((int)(row * lineHeight +
						lineHeight / 2));
					var contentX = scrollX > int.MaxValue - x
						? int.MaxValue : x + scrollX;
					ResolveTestPosColumn(ref platform, state, obj, contentLayoutWidth,
						contentX, x, ref flags, out column, out xoffset);
				}
				else flags |= TestPosBelow;
			}
		}
		value.Entry = entry;
		value.Column = unchecked((short)column);
		value.Flags = unchecked((ushort)flags);
		value.XOffset = unchecked((short)xoffset);
		value.YOffset = unchecked((short)yoffset);
		return true;
	}

	private static void ResolveTestPosColumn<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int layoutWidth, int contentX, int viewportX,
		ref uint flags,
		out int column, out int xoffset)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		column = -1;
		xoffset = 0;
		var columns = FormatColumnCount(ref platform, state, obj);
		if (columns == 0) columns = 1;
		if (columns > MaximumDrawColumns) columns = MaximumDrawColumns;
		var layoutBlock = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnLayoutKey, 0));
		var installedLayoutWidth = Read(ref platform, state, obj,
			ColumnLayoutWidthKey, 0);
		if (layoutBlock.IsNull || installedLayoutWidth !=
			unchecked((uint)layoutWidth) ||
			!platform.IsMapped(layoutBlock, columns * ColumnGeometryRecordSize))
			layoutBlock = APTR.Null;
		for (var current = 0u; current < columns; current++)
		{
			var geometry = default(MuiListColumnGeometry);
			var hasGeometry = layoutBlock.IsNotNull &&
				TryReadColumnGeometryRecord(ref platform, layoutBlock,
					current, out geometry);
			var cellLeft = hasGeometry
				? geometry.Offset
				: ColumnOffset(ref platform, state, obj, layoutWidth, columns,
					current);
			var cellWidth = hasGeometry
				? geometry.Width
				: ColumnWidth(ref platform, state, obj, layoutWidth, columns,
					current);
			var cellEnd = SaturatingAdd(cellLeft, cellWidth);
			if (unchecked((uint)contentX) >= cellLeft &&
				unchecked((uint)contentX) < cellEnd)
			{
				column = unchecked((int)current);
				xoffset = contentX - unchecked((int)cellLeft);
				break;
			}
			if (unchecked((uint)contentX) < cellLeft)
			{
				flags |= TestPosLeft;
				break;
			}
		}
		if (column < 0 && flags == 0) flags |= TestPosRight;
	}

	// Publish MUIA_List_TitleClick and perform the documented sortable-column
	// action. The title row itself is resolved by the named TestPos geometry
	// record; no caller-facing packet offsets are needed here.
	internal static bool HandleTitleClick<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!IsListBacked(Classify(ref platform, state, obj))) return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		var columns = FormatColumnCount(ref platform, state, obj);
		if (columns == 0) columns = 1;
		if (column >= columns) return false;
		if (!ApplySortStateAttribute(ref platform, state, record, obj,
			TitleClick, unchecked((uint)column), true))
			return false;
		var flags = DescriptorValue(ref platform, state, obj, column,
			MuiListFormatField.Flags, 0);
		if ((flags & DescriptorSortable) == 0) return true;
		if (!ApplySortColumn(ref platform, state, record, obj, column, true))
			return false;
		return Sort(ref platform, state, obj);
	}

	// Create the opaque guest-resident handle returned by MUIM_List_CreateImage.
	// The handle deliberately stores only the caller's BOOPSI object pointer and
	// flags; rendering remains the responsibility of the existing display/text
	// seam. Keeping a bounded per-list chain makes DeleteImage and object
	// disposal deterministic without depending on managed identity or a host
	// image object.
	public static APTR CreateImage<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR imageObject, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!IsListBacked(Classify(ref platform, state, obj))) return APTR.Null;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return APTR.Null;
		var current = ReadHeaderImages(ref platform, header);
		var count = 0u;
		while (current.IsNotNull && count++ < MaximumImages)
		{
			if (!MuiListImageCodec.TryRead(ref platform, current,
				out var imageValue))
				return APTR.Null;
			current = imageValue.Next;
		}
		if (current.IsNotNull || count >= MaximumImages) return APTR.Null;
		var handle = MuiHeadlessMemory.Allocate(ref platform, ImageRecordSize);
		if (handle.IsNull) return APTR.Null;
		var imageState = default(MuiListImageState);
		imageState.Magic = MuiListImageState.Cookie;
		imageState.ImageObject = imageObject;
		imageState.Flags = flags;
		imageState.Next = ReadHeaderImages(ref platform, header);
		if (!MuiListImageCodec.Write(ref platform, handle, imageState) ||
			!WriteHeaderImages(ref platform, header, handle))
		{
			platform.Clear(handle, ImageRecordSize);
			platform.Free(handle, ImageRecordSize);
			return APTR.Null;
		}
		return handle;
	}

	// Retire one opaque image handle. The supplied BOOPSI object is not disposed
	// here: MorphOS explicitly leaves that object under the caller's ownership.
	public static bool DeleteImage<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR image)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (image.IsNull || !platform.IsMapped(image, ImageRecordSize) ||
			!IsListBacked(Classify(ref platform, state, obj))) return false;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var previous = APTR.Null;
		var current = ReadHeaderImages(ref platform, header);
		for (var count = 0u; current.IsNotNull && count < MaximumImages; count++)
		{
			if (!MuiListImageCodec.TryRead(ref platform, current,
				out var imageValue)) return false;
			var next = imageValue.Next;
			if (current.Raw == image.Raw)
			{
				if (previous.IsNull)
				{
					if (!WriteHeaderImages(ref platform, header, next))
						return false;
				}
				else
				{
					if (!MuiListImageCodec.TryRead(ref platform, previous,
						out var previousValue)) return false;
					previousValue.Next = next;
					if (!MuiListImageCodec.Write(ref platform, previous,
						previousValue)) return false;
				}
				platform.Clear(current, ImageRecordSize);
				platform.Free(current, ImageRecordSize);
				return true;
			}
			previous = current;
			current = next;
		}
		return false;
	}

	// Test/introspection helper used by the qualification seam.
	public static uint ImageCount<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return 0;
		var current = ReadHeaderImages(ref platform, header);
		var count = 0u;
		while (current.IsNotNull && count < MaximumImages)
		{
			if (!MuiListImageCodec.TryRead(ref platform, current,
				out var image)) return count;
			count++;
			current = image.Next;
		}
		return count;
	}

	// True when the guest-resident list backbone has been constructed for this
	// object (List or the Floattext subclass).
	internal static bool HasBackbone<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		Header(ref platform, state, obj).IsNotNull;

	// Append a caller-supplied, guest-resident string buffer at the bottom

	private static bool DrawRows<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiLayoutPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var info = APTR.FromPointer(Read(ref platform, state, obj, RenderInfo, 0));
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, info,
			out var renderInfo)) return true;
		var rastPort = renderInfo.RastPort;
		if (rastPort.IsNull) return true;
		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
			out var areaGeometry)) return true;
		var left = areaGeometry.Left;
		var top = areaGeometry.Top;
		var width = areaGeometry.Width;
		var height = areaGeometry.Height;
		if (width <= 0 || height <= 0) return true;
		var contentLayoutWidth = ContentLayoutWidth(ref platform, state, obj,
			width);
		var scrollX = unchecked((int)HorizontalScrollX(ref platform, state, obj));
		var columns = FormatColumnCount(ref platform, state, obj);
		if (columns == 0) columns = 1;
		if (columns > MaximumDrawColumns) columns = MaximumDrawColumns;
		var layoutBlock = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnLayoutKey, 0));
		var installedLayoutWidth = Read(ref platform, state, obj,
			ColumnLayoutWidthKey, 0);
		if (layoutBlock.IsNull || installedLayoutWidth !=
			unchecked((uint)contentLayoutWidth) ||
			!platform.IsMapped(layoutBlock, columns * ColumnGeometryRecordSize))
			layoutBlock = APTR.Null;
		if (!TryAllocateDisplayArray(ref platform, out var displayStorage))
			return false;
		var displayArray = displayStorage.Array;
		if (!platform.LockLayer(rastPort))
		{
			FreeDisplayArray(ref platform, displayStorage);
			return false;
		}
		if (!platform.BeginUpdate(rastPort))
		{
			platform.UnlockLayer(rastPort);
			FreeDisplayArray(ref platform, displayStorage);
			return false;
		}
		var clip = platform.PushClip(rastPort, left, top, width, height);
		var count = ReadHeaderCount(ref platform, header);
		var firstSigned = unchecked((int)FirstCursor(ref platform, state, obj));
		var first = firstSigned < 0 ? 0u : unchecked((uint)firstSigned);
		var lineHeight = EffectiveLineHeight(ref platform, state, obj);
		var rows = unchecked((uint)(height / (int)lineHeight));
		var font = FontCursor(ref platform, state, obj);
		var titleRows = 0u;
		var titleStateBlock = APTR.FromPointer(Read(ref platform, state, obj,
			TitleArrayStateKey, 0));
		if (titleStateBlock.IsNotNull && rows != 0 &&
			TryReadTitleArrayStateBlock(ref platform, titleStateBlock,
				out var titleArrayState) && titleArrayState.Count != 0)
		{
			ClearDisplayArray(ref platform, displayStorage);
			if (CopyTitleArrayPointers(ref platform, titleArrayState, displayArray))
			{
				DrawColumns(ref platform, state, obj, layoutBlock, rastPort, font,
					displayArray, columns, left, width, contentLayoutWidth, scrollX,
					top + (int)lineHeight);
				titleRows = 1;
			}
		}
		else if (titleStateBlock.IsNull && TitleValueCursor(ref platform,
			state, obj) != 0 && rows != 0)
		{
			// A neutral MUIA_List_Title row is published through the display hook.
			// MUIA_List_TitleArray takes precedence and bypasses that hook.
			var titleRaw = TitleValueCursor(ref platform, state, obj);
			// MorphOS uses TRUE as the custom-hook form: the display hook receives
			// a NULL entry and supplies the column titles itself. Keep that
			// contract even when the list has no data rows yet.
			var titleEntry = titleRaw == 1 ? APTR.Null :
				APTR.FromPointer(titleRaw);
			ClearDisplayArray(ref platform, displayStorage);
			if (Display(ref platform, state, obj, titleEntry, displayArray, -1))
			{
				DrawColumns(ref platform, state, obj, layoutBlock, rastPort, font,
					displayArray, columns, left, width, contentLayoutWidth, scrollX,
					top + (int)lineHeight);
				titleRows = 1;
			}
		}
		for (var row = 0u; row + titleRows < rows && first + row < count; row++)
		{
			ClearDisplayArray(ref platform, displayStorage);
			var entry = SlotEntryAt(ref platform, header, first + row);
			if (!Display(ref platform, state, obj, entry, displayArray,
				unchecked((int)(first + row)))) continue;
			var rowTop = top + unchecked((int)(row + titleRows + 1) *
				(int)lineHeight);
			if (PresentationPolicyValue(ref platform, state, obj, Stripes, 0) != 0 &&
				((first + row) & 1u) != 0)
			{
				// MorphOS supplies the skin-specific stripe pen. The freestanding
				// profile keeps that styling deterministic through the graphics seam;
				// full palette/skin parity remains outside this compatibility slice.
				platform.SetPen(rastPort, StripePen);
				platform.FillRectangle(rastPort, left, rowTop,
					left + width - 1, rowTop + unchecked((int)lineHeight) - 1);
			}
			DrawColumns(ref platform, state, obj, layoutBlock, rastPort, font,
				displayArray, columns, left, width, contentLayoutWidth, scrollX,
				rowTop);
		}
		DrawDropMark(ref platform, state, obj, rastPort, left, top, width, height,
			lineHeight, first, rows, titleRows);
		platform.PopClip(rastPort, clip);
		platform.EndUpdate(rastPort, true);
		platform.UnlockLayer(rastPort);
		ClearDisplayArray(ref platform, displayStorage);
		FreeDisplayArray(ref platform, displayStorage);
		return true;
	}

	private static void DrawDropMark<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR rastPort, int left, int top, int width, int height,
		uint lineHeight, uint first, uint rows, uint titleRows)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (PresentationPolicyValue(ref platform, state, obj,
			ShowDropMarks, 1) == 0 ||
			width <= 0 || height <= 0 || lineHeight == 0) return;
		var mark = unchecked((int)DropMarkCursor(ref platform, state, obj));
		if (mark < 0) return;
		var visibleDataRows = rows > titleRows ? rows - titleRows : 0;
		var relative = mark <= unchecked((int)first) ? 0u :
			unchecked((uint)(mark - unchecked((int)first)));
		if (relative > visibleDataRows) return;
		var y = top + unchecked((int)(titleRows + relative) *
			(int)lineHeight);
		if (relative == visibleDataRows) y = top + height - 1;
		if (y < top || y >= top + height) return;
		platform.SetPen(rastPort, DropMarkPen);
		platform.DrawLine(rastPort, left, y, left + width - 1, y);
	}

	// Draw every derived column of one already-populated display array at the
	// given baseline. Neutral text emission; per-column pixel widths are MG12.
	private static void DrawColumns<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR layoutBlock, APTR rastPort, APTR font,
		APTR displayArray,
		uint columns, int left, int width, int layoutWidth, int scrollX,
		int baseline)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		for (var column = 0u; column < columns; column++)
		{
			var sourceColumn = DisplaySourceColumn(ref platform, state, obj,
				column);
			var text = APTR.Null;
			if (sourceColumn < MaximumArrayEntries)
			{
				var cursor = default(MuiListPointerSlotCursor);
				cursor.Base = displayArray;
				cursor.Index = sourceColumn;
				if (MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
					out var displaySlot) && MuiListPointerSlotCodec.TryRead(ref platform,
					displaySlot,
					out var displayValue)) text = displayValue.Value;
			}
			var geometry = default(MuiListColumnGeometry);
			var hasGeometry = layoutBlock.IsNotNull &&
				TryReadColumnGeometryRecord(ref platform, layoutBlock, column,
					out geometry);
			var cellLeft = left + unchecked((int)(hasGeometry
				? geometry.Offset
				: ColumnOffset(ref platform, state, obj, layoutWidth, columns,
					column))) - scrollX;
			var cellWidth = hasGeometry
				? geometry.Width
				: ColumnWidth(ref platform, state, obj, layoutWidth, columns, column);
			if (cellWidth == 0 || cellLeft >= left + width ||
				cellLeft + unchecked((int)cellWidth) <= left) continue;
			if (text.IsNotNull && CStringCodec.TryReadLength(ref platform, text,
				MaximumStringLength, out var length))
			{
				var drawLength = unchecked((int)length);
				var textWidth = platform.TextWidth(rastPort, font, text, drawLength);
				if (textWidth > unchecked((int)cellWidth))
				{
					drawLength = unchecked((int)cellWidth) / 8;
					while (drawLength > 0 && platform.TextWidth(rastPort, font,
						text, drawLength) > unchecked((int)cellWidth)) drawLength--;
					textWidth = drawLength == 0 ? 0 : platform.TextWidth(rastPort,
						font, text, drawLength);
				}
				if (drawLength > 0)
				{
					var textLeft = cellLeft;
					var drawnWidth = textWidth < 0 ? 0u : unchecked((uint)textWidth);
					var spare = cellWidth > drawnWidth
						? cellWidth - drawnWidth : 0u;
					var alignment = FormatTextAlignment(ref platform, state, obj,
						column);
					if (alignment == MuiListTextAlignment.Center)
						textLeft += unchecked((int)(spare / 2));
					else if (alignment == MuiListTextAlignment.Right)
						textLeft += unchecked((int)spare);
					platform.DrawText(rastPort, font,
						textLeft, baseline, text, drawLength);
				}
			}
			// FORMAT BAR draws a separator between this cell and the next one.
			// The flag belongs to the named descriptor; geometry remains the
			// display-column layout, so COL reordering cannot move the separator.
			if (column + 1 >= columns ||
				(DescriptorValue(ref platform, state, obj, column,
					MuiListFormatField.Flags, 0) & DescriptorBar) == 0) continue;
			var barX = cellLeft + unchecked((int)cellWidth);
			if (barX < left || barX >= left + width) continue;
			var lineHeight = EffectiveLineHeight(ref platform, state, obj);
			var barTop = baseline - unchecked((int)lineHeight);
			var barBottom = baseline - 1;
			platform.DrawLine(rastPort, barX, barTop, barX, barBottom);
		}
	}

	// entry, tagging it owned so disposal/clear frees it through the normal
	// destruct path. Used by the Floattext backbone to publish wrapped rows
	// without a construct hook. On capacity failure the buffer is destructed and
	// false is returned, so the caller must not free it again.
	internal static bool AppendOwnedString<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR buffer)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (buffer.IsNull) return false;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		return Place(ref platform, state, obj, header, buffer, SlotOwnedString,
			InsertBottom);
	}

	// Append a caller-supplied, self-describing guest record at the bottom
	// entry, tagging it owned so disposal/clear frees it through the normal
	// destruct path. The record's first word must hold its total allocation
	// size. Used by the Dirlist/Volumelist subclasses to publish owned
	// FileInfoBlock-like entries. On capacity failure the record is destructed
	// (freed) and false is returned, so the caller must not free it again.
	internal static bool AppendOwnedRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR record)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (record.IsNull) return false;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		return Place(ref platform, state, obj, header, record, SlotOwnedRecord,
			InsertBottom);
	}

	// ---- Insertion -----------------------------------------------------------

	// Insert one entry. The construct seam produces the stored value; a construct
	// hook that returns Null adds nothing (per autodoc) yet still succeeds.
	public static bool InsertSingle<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR entry, int pos)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		CancelEditState(ref platform, state, obj);
		var pool = PoolFor(ref platform, state, obj);
		var stored = Construct(ref platform, state, obj, entry, pool,
			out var ownership);
		if (stored.IsNull) return true; // nothing added
		return Place(ref platform, state, obj, header, stored, ownership, pos);
	}

	// Insert an array of entries. count == -1 treats the array as Null
	// terminated. The whole batch is failure-atomic: on any failure the entries
	// added by this call are removed and destructed before returning false.
	public static bool Insert<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR entries, int count, int pos)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull || entries.IsNull) return false;
		CancelEditState(ref platform, state, obj);
		var before = ReadHeaderCount(ref platform, header);
		var terminated = count < 0;
		var limit = terminated ? MaximumEntries : (uint)count;
		var cursor = default(MuiListPointerVectorCursor);
		cursor.Base = entries;
		for (var i = 0u; i < limit; i++)
		{
			cursor.Index = i;
			if (!MuiListPointerVectorCursorCodec.TryGetEntry(ref platform,
				cursor, out var slotAddr) ||
				!MuiListPointerSlotCodec.TryRead(ref platform, slotAddr,
				out var slotValue))
			{
				RollbackTo(ref platform, state, obj, header, before);
				return false;
			}
			var entry = slotValue.Value;
			if (terminated && entry.IsNull) break;
			var target = pos < 0 ? pos : pos + (int)i;
			var pool = PoolFor(ref platform, state, obj);
			var stored = Construct(ref platform, state, obj, entry, pool,
				out var ownership);
			if (stored.IsNull) continue; // hook rejected this entry
			if (!Place(ref platform, state, obj, header, stored, ownership, target))
			{
				Destruct(ref platform, state, obj, stored, ownership, pool);
				RollbackTo(ref platform, state, obj, header, before);
				return false;
			}
		}
		return true;
	}

	// Destruct and drop every entry above the recorded baseline count, keeping
	// batch insertion failure-atomic.
	private static void RollbackTo<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR header, uint baseline)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var current = ReadHeaderCount(ref platform, header);
		while (current > baseline)
		{
			RemoveAt(ref platform, state, obj, header, current - 1);
			current = ReadHeaderCount(ref platform, header);
		}
	}

	// ---- Removal / clear -----------------------------------------------------

	public static bool Remove<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int pos) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		CancelEditState(ref platform, state, obj);
		var count = ReadHeaderCount(ref platform, header);
		if (count == 0) return false;
		if (pos == RemoveSelected)
		{
			var removedAny = false;
			var i = ReadHeaderCount(ref platform, header);
			while (i-- != 0)
			{
				if ((SlotFlagsAt(ref platform, header, i) & SlotSelected) != 0)
				{
					RemoveAt(ref platform, state, obj, header, i);
					removedAny = true;
				}
			}
			if (removedAny)
				ToggleSelectChange(ref platform, state, obj);
			return removedAny;
		}
		var index = pos switch
		{
			RemoveFirst => 0,
			RemoveLast => (int)count - 1,
			RemoveActive => ActiveIndex(ref platform, state, obj),
			_ => pos,
		};
		if (index < 0 || (uint)index >= count) return false;
		var selectionChanged = (SlotFlagsAt(ref platform, header,
			unchecked((uint)index)) & SlotSelected) != 0;
		RemoveAt(ref platform, state, obj, header, (uint)index);
		if (selectionChanged)
			ToggleSelectChange(ref platform, state, obj);
		return true;
	}

	public static bool Clear<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		CancelEditState(ref platform, state, obj);
		var index = ReadHeaderIndex(ref platform, header);
		var count = ReadHeaderCount(ref platform, header);
		var selectionChanged = SelectedCount(ref platform, header) != 0;
		var pool = PoolFor(ref platform, state, obj);
		for (var i = 0u; i < count && i < MaximumEntries; i++)
			DestructSlot(ref platform, state, obj, index, i, pool);
		WriteHeaderCount(ref platform, header, 0);
		if (count != 0) platform.Clear(index, count * SlotSize);
		RefreshLineHeight(ref platform, state, obj);
		// MorphOS 3.20 exposes zero for an empty list. ActiveIndex() remains the
		// internal no-row sentinel used by selectors and mutation paths.
		SetActive(ref platform, state, obj, 0);
		Publish(ref platform, state, obj, 0);
		if (selectionChanged)
			ToggleSelectChange(ref platform, state, obj);
		if (count != 0) RequestMutationRedraw(ref platform, state, obj);
		return true;
	}

	// ---- Selection -----------------------------------------------------------

	// Replace the selection with one row as a single user-visible mutation.
	// Listview's exclusive click path used to call Select(All, Off) followed by
	// Select(row, On), which exposed two change notifications for one click.
	// This helper edits the named slot records first, then publishes exactly one
	// SelectChange transition when the final selection differs.
	internal static bool SelectExclusive<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int index)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var count = ReadHeaderCount(ref platform, header);
		if (index < 0 || (uint)index >= count) return false;
		var changed = false;
		for (var i = 0u; i < count; i++)
		{
			var flags = SlotFlagsAt(ref platform, header, i);
			var selected = i == unchecked((uint)index);
			var wasSelected = (flags & SlotSelected) != 0;
			if (selected == wasSelected) continue;
			WriteSlot(ref platform, header, i, SlotEntryAt(ref platform,
				header, i), selected ? flags | SlotSelected : flags & ~SlotSelected);
			changed = true;
		}
		if (changed)
		{
			ToggleSelectChange(ref platform, state, obj);
			RequestMutationRedraw(ref platform, state, obj);
		}
		return true;
	}

	// Update selection state. pos accepts MUIV_List_Select_Active/_All; seltype
	// is Off/On/Toggle/Ask. The optional storage word receives the entry state
	// (post-change, or current for Ask).
	public static bool Select<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int pos, uint seltype, APTR stateStorage)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var count = ReadHeaderCount(ref platform, header);
		if (count == 0)
		{
			if (pos == SelectAll && seltype == SelectAsk && stateStorage.IsNotNull)
			{
				var emptyResult = default(MuiListScalarStorageRecord);
				if (MuiListScalarStorageCodec.Write(ref platform, stateStorage,
					emptyResult)) return true;
			}
			return pos == SelectAll && seltype == SelectAsk;
		}
		var changed = false;
		uint reported = 0;
		if (pos == SelectAll)
		{
			if (seltype == SelectAsk)
			{
				for (var i = 0u; i < count; i++)
					if ((SlotFlagsAt(ref platform, header, i) & SlotSelected) != 0)
						reported++;
			}
			else
			{
				for (var i = 0u; i < count; i++)
					changed |= ApplySelect(ref platform, state, obj, header, i,
						seltype, ref reported);
			}
		}
		else
		{
			var index = pos == SelectActive
				? ActiveIndex(ref platform, state, obj) : pos;
			if (index < 0 || (uint)index >= count) return false;
			changed = ApplySelect(ref platform, state, obj, header, (uint)index,
				seltype, ref reported);
		}
		if (stateStorage.IsNotNull)
		{
			var result = default(MuiListScalarStorageRecord);
			result.Value = reported;
			if (platform.IsMapped(stateStorage, MuiListScalarStorageRecord.Size) &&
				!MuiListScalarStorageCodec.Write(ref platform, stateStorage, result))
				return false;
		}
		if (changed && seltype != SelectAsk)
		{
			ToggleSelectChange(ref platform, state, obj);
			RequestMutationRedraw(ref platform, state, obj);
		}
		return true;
	}

	// Iterate selected entries. *posStorage is seeded with
	// MUIV_List_NextSelected_Start and receives the next selected index or
	// MUIV_List_NextSelected_End when the iteration is exhausted.
	public static bool NextSelected<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR posStorage) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull || posStorage.IsNull ||
			!platform.IsMapped(posStorage, MuiListScalarStorageRecord.Size))
			return false;
		var count = ReadHeaderCount(ref platform, header);
		if (!MuiListScalarStorageCodec.TryRead(ref platform, posStorage,
			out var position)) return false;
		var current = unchecked((int)position.Value);
		var start = current == NextSelectedStart ? 0u : (uint)(current + 1);
		for (var i = start; i < count; i++)
		{
			if ((SlotFlagsAt(ref platform, header, i) & SlotSelected) != 0)
			{
				position.Value = i;
				if (!MuiListScalarStorageCodec.Write(ref platform, posStorage,
					position)) return false;
				return true;
			}
		}
		// MorphOS treats an unselected active row as the implicit selection
		// control uses for keyboard navigation.  Publish that fallback only for
		// the initial cursor value; once it has been returned, the next call must
		// terminate just like an exhausted selected-row iteration.
		if (current == NextSelectedStart)
		{
			var active = ActiveIndex(ref platform, state, obj);
			if (active >= 0 && (uint)active < count)
			{
				position.Value = unchecked((uint)active);
				if (!MuiListScalarStorageCodec.Write(ref platform, posStorage,
					position)) return false;
				return true;
			}
		}
		position.Value = unchecked((uint)NextSelectedEnd);
		if (!MuiListScalarStorageCodec.Write(ref platform, posStorage, position))
			return false;
		return true;
	}

	// ---- Ordering ------------------------------------------------------------

	// Sort the list in place using the compare seam and MUIA_List_SortColumn.
	public static bool Sort<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		CancelEditState(ref platform, state, obj);
		var count = ReadHeaderCount(ref platform, header);
		var column = SortColumnValue(ref platform, state, obj);
		// Insertion sort keeps the pass allocation-free and stable.
		for (var i = 1u; i < count; i++)
		{
			var entry = SlotEntryAt(ref platform, header, i);
			var flags = SlotFlagsAt(ref platform, header, i);
			var j = i;
			while (j > 0)
			{
				var prev = SlotEntryAt(ref platform, header, j - 1);
				if (CompareForSort(ref platform, state, obj, prev, entry, column) <= 0)
					break;
				WriteSlot(ref platform, header, j, prev,
					SlotFlagsAt(ref platform, header, j - 1));
				j--;
			}
			WriteSlot(ref platform, header, j, entry, flags);
		}
		Publish(ref platform, state, obj, count);
		if (count > 1) RequestMutationRedraw(ref platform, state, obj);
		return true;
	}

	// Sort an external, caller-supplied Null-terminated array of entry pointers
	// in place using the compare seam. The list index is left untouched.
	public static bool SortEntries<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR entries) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (entries.IsNull) return false;
		CancelEditState(ref platform, state, obj);
		var column = SortColumnValue(ref platform, state, obj);
		uint count = 0;
		var cursor = default(MuiListPointerVectorCursor);
		cursor.Base = entries;
		while (count < MaximumEntries)
		{
			cursor.Index = count;
			if (!MuiListPointerVectorCursorCodec.TryGetEntry(ref platform, cursor,
				out var addr) || !MuiListPointerSlotCodec.TryRead(ref platform, addr,
				out var slotValue)) return false;
			if (slotValue.Value.IsNull) break;
			count++;
		}
		for (var i = 1u; i < count; i++)
		{
			cursor.Index = i;
			if (!MuiListPointerVectorCursorCodec.TryGetEntry(ref platform, cursor,
				out var entrySlot) || !MuiListPointerSlotCodec.TryRead(ref platform,
				entrySlot,
				out var entryValue)) return false;
			var entry = entryValue.Value;
			var j = i;
			while (j > 0)
			{
				cursor.Index = j - 1;
				if (!MuiListPointerVectorCursorCodec.TryGetEntry(ref platform,
					cursor, out var previousSlot) ||
					!MuiListPointerSlotCodec.TryRead(ref platform, previousSlot,
					out var previousValue)) return false;
				if (CompareForSort(ref platform, state, obj, previousValue.Value,
					entry, column) <= 0) break;
				cursor.Index = j;
				if (!MuiListPointerVectorCursorCodec.TryGetEntry(ref platform, cursor,
					out var shiftSlot) || !MuiListPointerSlotCodec.Write(ref platform,
					shiftSlot,
					previousValue)) return false;
				j--;
			}
			cursor.Index = j;
			if (!MuiListPointerVectorCursorCodec.TryGetEntry(ref platform, cursor,
				out var destinationSlot)) return false;
			var destinationValue = default(MuiListPointerSlotRecord);
			destinationValue.Value = entry;
			if (!MuiListPointerSlotCodec.Write(ref platform, destinationSlot,
				destinationValue)) return false;
		}
		return true;
	}

	// Move a single entry between two positions, honouring the relative
	// MUIV_List_Move_* selectors.
	public static bool Move<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int from, int to) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		CancelEditState(ref platform, state, obj);
		var count = ReadHeaderCount(ref platform, header);
		if (count == 0) return false;
		var source = ResolveEndpoint(ref platform, state, obj, from, count, -1);
		var dest = ResolveEndpoint(ref platform, state, obj, to, count, source);
		if (source < 0 || (uint)source >= count || dest < 0 ||
			(uint)dest >= count) return false;
		if (source == dest) return true;
		var entry = SlotEntryAt(ref platform, header, (uint)source);
		var flags = SlotFlagsAt(ref platform, header, (uint)source);
		if (source < dest)
			for (var i = (uint)source; i < (uint)dest; i++)
				WriteSlot(ref platform, header, i,
					SlotEntryAt(ref platform, header, i + 1),
					SlotFlagsAt(ref platform, header, i + 1));
		else
			for (var i = (uint)source; i > (uint)dest; i--)
				WriteSlot(ref platform, header, i,
					SlotEntryAt(ref platform, header, i - 1),
					SlotFlagsAt(ref platform, header, i - 1));
		WriteSlot(ref platform, header, (uint)dest, entry, flags);
		MuiHeadlessMemory.Mutated(ref platform, state);
		RequestMutationRedraw(ref platform, state, obj);
		return true;
	}

	// Swap two entries, honouring the relative MUIV_List_Exchange_* selectors.
	public static bool Exchange<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int pos1, int pos2)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		CancelEditState(ref platform, state, obj);
		var count = ReadHeaderCount(ref platform, header);
		if (count == 0) return false;
		var a = ResolveEndpoint(ref platform, state, obj, pos1, count, -1);
		var b = ResolveEndpoint(ref platform, state, obj, pos2, count, a);
		if (a < 0 || (uint)a >= count || b < 0 || (uint)b >= count) return false;
		if (a == b) return true;
		var entryA = SlotEntryAt(ref platform, header, (uint)a);
		var flagsA = SlotFlagsAt(ref platform, header, (uint)a);
		WriteSlot(ref platform, header, (uint)a,
			SlotEntryAt(ref platform, header, (uint)b),
			SlotFlagsAt(ref platform, header, (uint)b));
		WriteSlot(ref platform, header, (uint)b, entryA, flagsA);
		MuiHeadlessMemory.Mutated(ref platform, state);
		RequestMutationRedraw(ref platform, state, obj);
		return true;
	}

	// Scroll so the requested entry becomes visible. Backbone semantics record
	// the resolved first-visible line and notify only on change.
	public static bool Jump<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int pos) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var count = ReadHeaderCount(ref platform, header);
		if (count == 0) return true;
		var first = unchecked((int)FirstCursor(ref platform, state, obj));
		var target = pos switch
		{
			JumpActive => ActiveIndex(ref platform, state, obj),
			JumpBottom => (int)count - 1,
			JumpDown => first + 1,
			JumpUp => first - 1,
			_ => pos,
		};
		if (target < 0) target = 0;
		if ((uint)target >= count) target = (int)count - 1;
		SetNotify(ref platform, state, obj, First, unchecked((uint)target));
		// Jump changes the first-visible row without going through Layout. Keep
		// the named viewport record and its public pixel projections coherent at
		// the same operation boundary, so scrollers and immediate Get() calls do
		// not observe a stale TopPixel/VisiblePixel/TotalPixel tuple.
		RefreshViewportMetrics(ref platform, state, obj);
		return true;
	}

	// ---- Redraw --------------------------------------------------------------

	private static void RequestMutationRedraw<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			RedrawStateKey, 0));
		if (!TryReadRedrawState(ref platform, block, out var redraw)) return;
		if (PresentationPolicyValue(ref platform, state, obj, Quiet, 0) != 0)
			redraw.Dirty = 1;
		else
			redraw.Requests = SaturatingAdd(redraw.Requests, 1);
		WriteRedrawState(ref platform, block, redraw);
	}

	// MUIM_List_Redraw only schedules a concrete row while that row is inside
	// the currently published viewport. Keep this policy in one typed helper so
	// the public method does not accidentally turn First/Visible guest values
	// into an unbounded redraw request.
	private static bool IsRedrawTargetVisible<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int position)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (position == RedrawAll) return true;
		var activeRequest = position == RedrawActive;
		if (activeRequest)
			position = ActiveIndex(ref platform, state, obj);
		if (activeRequest && position < 0) return false;
		// Preserve the existing private entry-scope selector and any future
		// negative extension values; only documented concrete row positions are
		// subject to the visibility test below.
		if (position < 0) return true;
		var count = EntryCount(ref platform, state, obj);
		var first = unchecked((int)FirstCursor(ref platform, state, obj));
		var visible = unchecked((int)VisibleCursor(ref platform, state, obj));
		if (first < 0 || visible <= 0 || (uint)position >= count) return false;
		return position >= first && position - first < visible;
	}

	// Schedule a redraw for the requested scope. Requires a graphics-capable
	// platform; only issues a request when the list actually holds state.
	public static bool Redraw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int pos) where TPlatform : struct, IMuiLayoutPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		if (!IsRedrawTargetVisible(ref platform, state, obj, pos)) return true;
		if (PresentationPolicyValue(ref platform, state, obj, Quiet, 0) != 0)
		{
			RequestMutationRedraw(ref platform, state, obj);
			return true;
		}
		var flags = pos switch
		{
			RedrawAll => 0u,
			RedrawActive => 1u,
			RedrawEntry => 2u,
			_ => 3u,
		};
		var scheduled = platform.ScheduleRedraw(obj, flags);
		if (scheduled) RequestMutationRedraw(ref platform, state, obj);
		return scheduled;
	}

	// ---- Construct / destruct / display / compare seams ----------------------

	// Construct seam: NULL hook stores the pointer directly; the builtin String
	// and StringArray hooks duplicate bounded guest-resident data; a real hook is
	// invoked through the callback seam with (pool, entry).
	public static APTR Construct<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR entry, APTR pool, out uint ownership)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		ownership = 0;
		var hook = HookPolicyValue(ref platform, state, obj, ConstructHook);
		if (hook == 0) return entry;
		if (hook == HookString)
		{
			if (entry.IsNull) return APTR.Null;
			var dup = DuplicateString(ref platform, entry);
			ownership = dup.IsNotNull ? SlotOwnedString : 0;
			return dup;
		}
		if (hook == HookStringArray)
		{
			if (entry.IsNull) return APTR.Null;
			var dup = DuplicateStringArray(ref platform, entry);
			ownership = dup.IsNotNull ? SlotOwnedStringArray : 0;
			return dup;
		}
		// Arbitrary construct hook. The hook BASE pointer is delivered (A0) so the
		// callback can reach h_Data (hook+16); the adapter reads h_Entry (hook+8).
		// MUI construct ABI: A2 = pool, A1 = entry, constructed entry in D0.
		return APTR.FromPointer(platform.InvokeHook(APTR.FromPointer(hook), pool,
			entry));
	}

	// Destruct seam: owned buffers/arrays are freed directly; a real destruct
	// hook is invoked through the callback seam with (pool, entry).
	public static void Destruct<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR entry, uint ownership, APTR pool)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (ownership == SlotOwnedString)
		{
			FreeOwnedString(ref platform, entry);
			return;
		}
		if (ownership == SlotOwnedStringArray)
		{
			FreeOwnedStringArray(ref platform, entry);
			return;
		}
		if (ownership == SlotOwnedRecord)
		{
			FreeOwnedRecord(ref platform, entry);
			return;
		}
		var hook = HookPolicyValue(ref platform, state, obj, DestructHook);
		if (hook == 0 || hook == HookString || hook == HookStringArray ||
			entry.IsNull) return;
		// MUI destruct ABI: A0 = hook, A2 = pool, A1 = entry.
		platform.InvokeHook(APTR.FromPointer(hook), pool, entry);
	}

	// Display seam: NULL/String hook publishes the entry pointer into array[0]
	// with a Null terminator; StringArray copies the stored pointer table into
	// the caller's array; a real hook is invoked with (entry, array).  For a real
	// hook the ULONG immediately before array is the named display-row record.
	private static bool TryWriteDisplayRowPrefix<TPlatform>(ref TPlatform platform,
		APTR array, int row) where TPlatform : struct, IMuiGuestMemory
	{
		if (array.IsNull || array.Raw < MuiListDisplayRowRecord.Size)
			return false;
		// This subtraction is the single ABI-boundary operation: all subsequent
		// access is through MuiListDisplayRowRecordCodec rather than an offset.
		var prefix = APTR.FromPointer(array.Raw - MuiListDisplayRowRecord.Size);
		var value = default(MuiListDisplayRowRecord);
		value.Row = row;
		return MuiListDisplayRowRecordCodec.Write(ref platform, prefix, value);
	}

	public static bool Display<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR entry, APTR array, int row)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (array.IsNull || !platform.IsMapped(array, 8)) return false;
		var hook = HookPolicyValue(ref platform, state, obj, DisplayHook);
		if (hook == 0 || hook == HookString)
		{
			var value = default(MuiListPointerSlotRecord);
			value.Value = entry;
			if (!MuiListPointerSlotCodec.Write(ref platform, array, value))
				return false;
			var cursor = default(MuiListPointerSlotCursor);
			cursor.Base = array;
			cursor.Index = 1;
			return MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
				out var terminator) && MuiListPointerSlotCodec.Write(ref platform,
				terminator, default);
		}
		if (hook == HookStringArray)
			return CopyStringArrayPointers(ref platform, entry, array);
		if (!TryWriteDisplayRowPrefix(ref platform, array, row)) return false;
		// MUI display ABI: A0 = hook, A2 = entry, A1 = string array to fill.
		platform.InvokeHook(APTR.FromPointer(hook), entry, array);
		return true;
	}

	// Compare seam: NULL/String hook performs a bounded C-string comparison;
	// StringArray compares the requested column; a real hook is invoked with
	// (entry1, entry2) and its result forwarded.
	public static int Compare<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR entry1, APTR entry2, uint column)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hook = HookPolicyValue(ref platform, state, obj, CompareHook);
		if (hook == 0 || hook == HookString)
			return CompareStrings(ref platform, entry1, entry2);
		if (hook == HookStringArray)
			return CompareStringArrayColumn(ref platform, entry1, entry2,
				DisplaySourceColumn(ref platform, state, obj, column));
		// MUI compare ABI: A0 = hook, A2 = entry1, A1 = entry2, result in D0.
		return unchecked((int)platform.InvokeHook(APTR.FromPointer(hook), entry1,
			entry2));
	}

	// ORDER=DESC is a sorting policy for the selected FORMAT column, not a
	// change to the public MUIM_List_Compare result. Apply it only at the sort
	// boundary while keeping the named descriptor record authoritative.
	private static int CompareForSort<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR entry1, APTR entry2, uint column)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var result = Compare(ref platform, state, obj, entry1, entry2, column);
		var flags = DescriptorValue(ref platform, state, obj, column,
			MuiListFormatField.Flags, 0);
		if ((flags & DescriptorDescending) == 0) return result;
		return result == int.MinValue ? int.MaxValue : -result;
	}

	// ---- Internal helpers ----------------------------------------------------

	// Give the default editor the cell rectangle published by the List layout
	// pass.  A List can be edited before it has a rectangle; in that case the
	// editor remains valid and the next CreateEditObject call retries placement.
	public static bool PlaceEditObject<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int row, int column, APTR editObject)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (editObject.IsNull || row < 0 || column < 0) return false;
		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
			out var areaGeometry)) return false;
		var listWidth = areaGeometry.Width <= 0 ? 0u :
			unchecked((uint)areaGeometry.Width);
		var listHeight = areaGeometry.Height <= 0 ? 0u :
			unchecked((uint)areaGeometry.Height);
		if (listWidth == 0 || listHeight == 0) return false;
		var first = unchecked((int)FirstCursor(ref platform, state, obj));
		if (first < 0 || row < first) return false;
		var rowOffset = unchecked((uint)(row - first));
		var titleRows = TitleRowCount(ref platform, state, obj);
		var rowLine = rowOffset + titleRows;
		var lineHeight = EffectiveLineHeight(ref platform, state, obj);
		if (rowLine < rowOffset || rowLine > uint.MaxValue / lineHeight)
			return false;
		var topOffset = rowLine * lineHeight;
		if (topOffset >= listHeight) return false;
		if (!TryReadColumnGeometry(ref platform, state, obj,
			unchecked((uint)column), out var geometry) || geometry.Width == 0 ||
			geometry.Offset >= listWidth) return false;
		var cellWidth = geometry.Width;
		if (cellWidth > listWidth - geometry.Offset)
			cellWidth = listWidth - geometry.Offset;
		var cellHeight = lineHeight;
		if (cellHeight > listHeight - topOffset)
			cellHeight = listHeight - topOffset;
		if (cellWidth == 0 || cellHeight == 0) return false;
		var listLeft = areaGeometry.Left;
		var listTop = areaGeometry.Top;
		var editLeft = unchecked((uint)(listLeft + unchecked((int)geometry.Offset)));
		var editTop = unchecked((uint)(listTop + unchecked((int)topOffset)));
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, editObject,
			LeftEdge, editLeft, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, editObject,
				TopEdge, editTop, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, editObject,
				Width, cellWidth, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, editObject,
				Height, cellHeight, false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, editObject,
				RightEdge, unchecked(editLeft + cellWidth - 1), false) &&
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, editObject,
				BottomEdge, unchecked(editTop + cellHeight - 1), false);
	}

	private static bool TryReadColumnGeometry<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column, out MuiListColumnGeometry geometry)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		geometry = default;
		var columns = GeometryColumnCount(ref platform, state, obj);
		if (column >= columns) return false;
		var width = Read(ref platform, state, obj, Width, 0);
		if (width == 0) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnLayoutKey, 0));
		var layoutWidth = Read(ref platform, state, obj, ColumnLayoutWidthKey, 0);
		if (block.IsNotNull && layoutWidth == width &&
			platform.IsMapped(block, columns * MuiListColumnGeometry.Size))
		{
			return TryReadColumnGeometryRecord(ref platform, block, column,
				out geometry);
		}
		var widthSigned = unchecked((int)width);
		geometry.Offset = ColumnOffset(ref platform, state, obj, widthSigned,
			columns, column);
		geometry.Width = ColumnWidth(ref platform, state, obj, widthSigned,
			columns, column);
		return true;
	}

	private static bool TryReadColumnGeometryRecord<TPlatform>(
		ref TPlatform platform, APTR block, uint column,
		out MuiListColumnGeometry geometry)
		where TPlatform : struct, IMuiGuestMemory
	{
		geometry = default;
		var cursor = default(MuiListColumnGeometryCursor);
		cursor.Base = block;
		cursor.Index = column;
		return MuiListColumnGeometryCursorCodec.TryGetEntry(ref platform, cursor,
			out var record) && MuiListColumnGeometryCodec.TryRead(ref platform,
			record, out geometry);
	}

	private static bool TryResolveEditTarget<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int row, int column, out int resolvedRow,
		out int resolvedColumn, out APTR entry)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		resolvedRow = row == EditActive ? ActiveIndex(ref platform, state, obj) : row;
		resolvedColumn = column;
		entry = APTR.Null;
		var count = EntryCount(ref platform, state, obj);
		var columns = FormatColumnCount(ref platform, state, obj);
		if (columns == 0) columns = 1;
		if (resolvedRow < 0 || (uint)resolvedRow >= count || column < 0 ||
			(uint)column >= columns) return false;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		entry = SlotEntryAt(ref platform, header, unchecked((uint)resolvedRow));
		return entry.IsNotNull;
	}

	private static bool TryReadEditState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListEditState edit)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			EditStateKey, 0));
		return MuiListEditStateCodec.TryRead(ref platform, block, out edit);
	}

	private static void CancelEditState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			EditStateKey, 0));
		if (block.IsNull) return;
		var editObject = MuiListEditStateCodec.TryRead(ref platform, block,
			out var edit) ? edit.EditObject : APTR.Null;
		if (editObject.IsNotNull && MuiHeadlessObjectCore.FindObject(ref platform,
			state, editObject).IsNotNull)
			MuiHeadlessObjectCore.DisposeObject(ref platform, state, editObject);
		if (platform.IsMapped(block, MuiListEditState.Size))
		{
			platform.Clear(block, MuiListEditState.Size);
			platform.Free(block, MuiListEditState.Size);
		}
	ClearInternal(ref platform, state, obj, EditStateKey);
	}

	private static bool Place<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR header, APTR stored, uint ownership, int pos)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = ReadHeaderCount(ref platform, header);
		var index = ResolveInsert(ref platform, state, obj, header, stored, pos,
			count);
		if (!EnsureCapacity(ref platform, state, obj, header, count + 1))
		{
			var pool = PoolFor(ref platform, state, obj);
			Destruct(ref platform, state, obj, stored, ownership, pool);
			return false;
		}
		// EnsureCapacity mutates the header block in place, so the header pointer
		// itself is stable; re-read defensively in case a subclass relocated it.
		var head = Header(ref platform, state, obj);
		for (var i = count; i > (uint)index; i--)
			WriteSlot(ref platform, head, i,
				SlotEntryAt(ref platform, head, i - 1),
				SlotFlagsAt(ref platform, head, i - 1));
		WriteSlot(ref platform, head, (uint)index, stored, ownership);
		WriteHeaderCount(ref platform, head, count + 1);
		RefreshLineHeight(ref platform, state, obj);
		// An insertion at or before the active entry shifts the active index.
		var active = ActiveIndex(ref platform, state, obj);
		if (active >= index)
			SetActive(ref platform, state, obj, unchecked((uint)(active + 1)));
		SetInternal(ref platform, state, obj, InsertPosition,
			unchecked((uint)index));
		Publish(ref platform, state, obj, count + 1);
		RequestMutationRedraw(ref platform, state, obj);
		return true;
	}

	private static void RemoveAt<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR header, uint index)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var count = ReadHeaderCount(ref platform, header);
		if (index >= count) return;
		var indexArray = ReadHeaderIndex(ref platform, header);
		var pool = PoolFor(ref platform, state, obj);
		DestructSlot(ref platform, state, obj, indexArray, index, pool);
		for (var i = index; i + 1 < count; i++)
			WriteSlot(ref platform, header, i,
				SlotEntryAt(ref platform, header, i + 1),
				SlotFlagsAt(ref platform, header, i + 1));
		WriteSlot(ref platform, header, count - 1, APTR.Null, 0);
		WriteHeaderCount(ref platform, header, count - 1);
		// Keep the active index anchored to the surviving neighbour.
		var active = ActiveIndex(ref platform, state, obj);
		if (active == (int)index)
		{
			if (count - 1 == 0) SetActive(ref platform, state, obj, 0);
			else if ((uint)active >= count - 1)
				SetActive(ref platform, state, obj, count - 2);
		}
		else if (active > (int)index)
			SetActive(ref platform, state, obj, unchecked((uint)(active - 1)));
		RefreshLineHeight(ref platform, state, obj);
		Publish(ref platform, state, obj, count - 1);
		RequestMutationRedraw(ref platform, state, obj);
	}

	private static bool EnsureCapacity<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR header, uint need)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (need > MaximumEntries) return false;
		var capacity = ReadHeaderCapacity(ref platform, header);
		if (capacity >= need) return true;
		var newCapacity = capacity == 0 ? InitialCapacity : capacity;
		while (newCapacity < need)
		{
			if (newCapacity > MaximumEntries / 2) { newCapacity = MaximumEntries; break; }
			newCapacity *= 2;
		}
		if (newCapacity < need) return false;
		var fresh = MuiHeadlessMemory.Allocate(ref platform,
			newCapacity * SlotSize);
		if (fresh.IsNull) return false;
		var old = ReadHeaderIndex(ref platform, header);
		var count = ReadHeaderCount(ref platform, header);
		if (old.IsNotNull && count != 0)
			platform.Copy(old, fresh, count * SlotSize);
		if (old.IsNotNull && capacity != 0)
		{
			platform.Clear(old, capacity * SlotSize);
			platform.Free(old, capacity * SlotSize);
		}
		WriteHeaderIndex(ref platform, header, fresh);
		WriteHeaderCapacity(ref platform, header, newCapacity);
		return true;
	}

	private static int ResolveInsert<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR header, APTR stored, int pos, uint count)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		switch (pos)
		{
			case InsertTop:
				return 0;
			case InsertBottom:
				return (int)count;
			case InsertActive:
				var active = ActiveIndex(ref platform, state, obj);
				return active < 0 || (uint)active > count ? (int)count : active;
			case InsertSorted:
		var column = SortColumnValue(ref platform, state, obj);
				for (var i = 0u; i < count; i++)
					if (Compare(ref platform, state, obj,
						SlotEntryAt(ref platform, header, i), stored, column) > 0)
						return (int)i;
				return (int)count;
			default:
				if (pos < 0) return (int)count;
				return (uint)pos > count ? (int)count : pos;
		}
	}

	private static int ResolveEndpoint<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int value, uint count, int other)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		switch (value)
		{
			case MoveActive: // == ExchangeActive
				return ActiveIndex(ref platform, state, obj);
			case MoveBottom: // == ExchangeBottom
				return (int)count - 1;
			case MoveNext: // == ExchangeNext (valid for the second endpoint)
				return other + 1;
			case MovePrevious: // == ExchangePrevious
				return other - 1;
			default:
				return value; // MoveTop/ExchangeTop == 0, or an explicit index
		}
	}

	private static bool ApplySelect<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR header, uint index, uint seltype, ref uint reported)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var flags = SlotFlagsAt(ref platform, header, index);
		var wasSelected = (flags & SlotSelected) != 0;
		if (!wasSelected && (seltype == SelectOn || seltype == SelectToggle) &&
			!AllowsMultiSelection(ref platform, state, obj, header, index))
		{
			reported = 0;
			return false;
		}
		var nowSelected = seltype switch
		{
			SelectOff => false,
			SelectOn => true,
			SelectToggle => !wasSelected,
			_ => wasSelected, // SelectAsk: no change
		};
		reported = nowSelected ? 1u : 0u;
		if (nowSelected == wasSelected || seltype == SelectAsk) return false;
		WriteSlot(ref platform, header, index,
			SlotEntryAt(ref platform, header, index),
			nowSelected ? flags | SlotSelected : flags & ~SlotSelected);
		return true;
	}

	// MUIA_List_MultiTestHook is consulted only when an operation would add a
	// row to the selection.  Removing an already selected row remains possible,
	// even if a later hook policy would reject that row.  The callback ABI puts
	// the entry in A1 (the message argument of the platform hook seam); A2 is
	// intentionally NULL because the MorphOS hook has no object argument.
	private static bool AllowsMultiSelection<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR header, uint index)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var hook = HookPolicyValue(ref platform, state, obj, MultiTestHook);
		if (hook == 0) return true;
		var entry = SlotEntryAt(ref platform, header, index);
		return platform.InvokeHook(APTR.FromPointer(hook), APTR.Null, entry) != 0;
	}

	private static bool InsertSource<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR source) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		var before = ReadHeaderCount(ref platform, header);
		var cursor = default(MuiListPointerVectorCursor);
		cursor.Base = source;
		for (var i = 0u; i < MaximumEntries; i++)
		{
			cursor.Index = i;
			if (!MuiListPointerVectorCursorCodec.TryGetEntry(ref platform, cursor,
				out var addr) || !MuiListPointerSlotCodec.TryRead(ref platform, addr,
				out var slotValue))
			{
				RollbackTo(ref platform, state, obj, header, before);
				return false;
			}
			var entry = slotValue.Value;
			if (entry.IsNull) return true;
			if (!InsertSingle(ref platform, state, obj, entry, InsertBottom))
			{
				RollbackTo(ref platform, state, obj, header, before);
				return false;
			}
		}
		return true;
	}

	private static void DestructSlot<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR index, uint slot, APTR pool)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var cursor = default(MuiListSlotCursor);
		cursor.Base = index;
		cursor.Index = slot;
		if (!MuiListSlotCursorCodec.TryGetEntry(ref platform, cursor,
			out var address) || !MuiListSlotCodec.TryRead(ref platform, address,
			out var value))
			return;
		var entry = value.Entry;
		var flags = value.Flags;
		Destruct(ref platform, state, obj, entry,
			flags & (SlotOwnedString | SlotOwnedStringArray | SlotOwnedRecord),
			pool);
	}

	private static APTR DuplicateString<TPlatform>(ref TPlatform platform,
		APTR source) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadCStringLength(ref platform, source, MaximumStringLength,
			out var length)) return APTR.Null;
		var size = length + 1;
		var copy = MuiHeadlessMemory.Allocate(ref platform, size);
		if (copy.IsNotNull) platform.Copy(source, copy, size);
		return copy;
	}

	private static bool TryReadStringArrayCount<TPlatform>(
		ref TPlatform platform, APTR source, out uint count)
		where TPlatform : struct, IMuiGuestMemory
	{
		count = 0;
		if (source.IsNull) return false;
		var cursor = default(MuiListPointerSlotCursor);
		cursor.Base = source;
		while (count < MaximumArrayEntries)
		{
			cursor.Index = count;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
				out var slot) || !MuiListPointerSlotCodec.TryRead(ref platform, slot,
				out var value)) return false;
			if (value.Value.IsNull) return true;
			count++;
		}
		cursor.Index = count;
		return MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
			out var terminator) && MuiListPointerSlotCodec.TryRead(ref platform,
			terminator,
			out var terminatorValue) && terminatorValue.Value.IsNull;
	}

	// Copy a NULL-terminated array of C-string pointers into a private guest
	// pointer table and private string buffers. The source array and every
	// string are bounded before any allocation is retained, so malformed input
	// fails without exposing a partial entry.
	private static APTR DuplicateStringArray<TPlatform>(ref TPlatform platform,
		APTR source) where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint count = 0;
		var sourceCursor = default(MuiListPointerSlotCursor);
		sourceCursor.Base = source;
		while (count < MaximumArrayEntries)
		{
			sourceCursor.Index = count;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
				sourceCursor, out var slot) ||
				!MuiListPointerSlotCodec.TryRead(ref platform, slot,
				out var value)) return APTR.Null;
			var text = value.Value;
			if (text.IsNull) break;
			if (!TryReadCStringLength(ref platform, text,
				MaximumStringLength, out _)) return APTR.Null;
			count++;
		}
		if (count == MaximumArrayEntries)
		{
			sourceCursor.Index = count;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
				sourceCursor, out var terminator) ||
				!MuiListPointerSlotCodec.TryRead(ref platform, terminator,
				out var terminatorValue) || !terminatorValue.Value.IsNull)
				return APTR.Null;
		}

		var tableSize = (count + 1) * MuiListPointerSlotRecord.Size;
		var table = MuiHeadlessMemory.Allocate(ref platform, tableSize);
		if (table.IsNull) return APTR.Null;
		var destinationCursor = default(MuiListPointerSlotCursor);
		destinationCursor.Base = table;
		for (var i = 0u; i < count; i++)
		{
			sourceCursor.Index = i;
			destinationCursor.Index = i;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
				sourceCursor, out var sourceSlot) ||
				!MuiListPointerSlotCodec.TryRead(ref platform, sourceSlot,
				out var sourceValue))
			{
				FreeOwnedStringArray(ref platform, table);
				return APTR.Null;
			}
			var text = sourceValue.Value;
			var copy = DuplicateString(ref platform, text);
			if (copy.IsNull)
			{
				FreeOwnedStringArray(ref platform, table);
				return APTR.Null;
			}
			var destinationValue = default(MuiListPointerSlotRecord);
			destinationValue.Value = copy;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
				destinationCursor, out var destinationSlot) ||
				!MuiListPointerSlotCodec.Write(ref platform, destinationSlot,
					destinationValue))
			{
				FreeOwnedStringArray(ref platform, table);
				return APTR.Null;
			}
		}
		return table;
	}

	private static bool CopyStringArrayPointers<TPlatform>(ref TPlatform platform,
		APTR source, APTR destination)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (source.IsNull || destination.IsNull) return false;
		var sourceCursor = default(MuiListPointerSlotCursor);
		sourceCursor.Base = source;
		var destinationCursor = default(MuiListPointerSlotCursor);
		destinationCursor.Base = destination;
		for (var i = 0u; i <= MaximumArrayEntries; i++)
		{
			sourceCursor.Index = i;
			destinationCursor.Index = i;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
				sourceCursor, out var sourceSlot) ||
				!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform,
					destinationCursor, out var destinationSlot) ||
				!MuiListPointerSlotCodec.TryRead(ref platform, sourceSlot,
				out var sourceValue) ||
				!MuiListPointerSlotCodec.Write(ref platform, destinationSlot,
					sourceValue)) return false;
			if (sourceValue.Value.IsNull) return true;
		}
		return false;
	}

	private static int CompareStringArrayColumn<TPlatform>(ref TPlatform platform,
		APTR left, APTR right, uint column)
		where TPlatform : struct, IMuiGuestMemory
	{
		var leftText = ArrayEntryAt(ref platform, left, column);
		var rightText = ArrayEntryAt(ref platform, right, column);
		return CompareStrings(ref platform, leftText, rightText);
	}

	private static APTR ArrayEntryAt<TPlatform>(ref TPlatform platform,
		APTR array, uint column) where TPlatform : struct, IMuiGuestMemory
	{
		if (array.IsNull || column >= MaximumArrayEntries) return APTR.Null;
		var cursor = default(MuiListPointerSlotCursor);
		cursor.Base = array;
		for (var i = 0u; i <= column; i++)
		{
			cursor.Index = i;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
				out var slot) || !MuiListPointerSlotCodec.TryRead(ref platform, slot,
				out var value)) return APTR.Null;
			if (i == column || value.Value.IsNull)
				return i == column ? value.Value : APTR.Null;
		}
		return APTR.Null;
	}

	private static void FreeOwnedString<TPlatform>(ref TPlatform platform,
		APTR entry) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (entry.IsNull) return;
		if (!TryReadCStringLength(ref platform, entry, MaximumStringLength,
			out var length)) return;
		var size = length + 1;
		platform.Clear(entry, size);
		platform.Free(entry, size);
	}

	private static void FreeOwnedRecord<TPlatform>(ref TPlatform platform,
		APTR record) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (record.IsNull) return;
		if (!MuiListOwnedRecordHeaderCodec.TryRead(ref platform, record,
			out var header)) return;
		var size = header.Length;
		if (size < 4 || size > MaximumRecordSize ||
			!platform.IsMapped(record, size)) return;
		platform.Clear(record, size);
		platform.Free(record, size);
	}

	private static void FreeOwnedStringArray<TPlatform>(ref TPlatform platform,
		APTR table) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (table.IsNull) return;
		uint count = 0;
		var cursor = default(MuiListPointerSlotCursor);
		cursor.Base = table;
		while (count < MaximumArrayEntries)
		{
			cursor.Index = count;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
				out var slot) || !MuiListPointerSlotCodec.TryRead(ref platform, slot,
				out var value)) break;
			var text = value.Value;
			if (text.IsNull) break;
			FreeOwnedString(ref platform, text);
			count++;
		}
		var tableSize = (count + 1) * MuiListPointerSlotRecord.Size;
		platform.Clear(table, tableSize);
		platform.Free(table, tableSize);
	}

	private static int CompareStrings<TPlatform>(ref TPlatform platform, APTR left,
		APTR right) where TPlatform : struct, IMuiGuestMemory
	{
		if (left.Raw == right.Raw) return 0;
		if (left.IsNull) return right.IsNull ? 0 : -1;
		if (right.IsNull) return 1;
		for (var i = 0u; i < MaximumStringLength; i++)
		{
			var la = APTR.FromPointer(left.Raw + i);
			var ra = APTR.FromPointer(right.Raw + i);
			if (!platform.IsMapped(la, 1) || !platform.IsMapped(ra, 1)) return 0;
			var lb = platform.ReadUInt8(la, 0);
			var rb = platform.ReadUInt8(ra, 0);
			if (lb != rb) return lb < rb ? -1 : 1;
			if (lb == 0) return 0;
		}
		return 0;
	}

	private static APTR ReadHeaderIndex<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		MuiListHeaderCodec.TryRead(ref platform, header, out var value)
			? value.Index : APTR.Null;

	private static uint ReadHeaderCapacity<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		MuiListHeaderCodec.TryRead(ref platform, header, out var value)
			? value.Capacity : 0;

	private static uint ReadHeaderCount<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		MuiListHeaderCodec.TryRead(ref platform, header, out var value)
			? value.Count : 0;

	private static APTR ReadHeaderImages<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		MuiListHeaderCodec.TryRead(ref platform, header, out var value)
			? value.Images : APTR.Null;

	private static bool WriteHeaderIndex<TPlatform>(ref TPlatform platform,
		APTR header, APTR index) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListHeaderCodec.TryRead(ref platform, header, out var value))
			return false;
		value.Index = index;
		return MuiListHeaderCodec.Write(ref platform, header, value);
	}

	private static bool WriteHeaderCapacity<TPlatform>(ref TPlatform platform,
		APTR header, uint capacity) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListHeaderCodec.TryRead(ref platform, header, out var value))
			return false;
		value.Capacity = capacity;
		return MuiListHeaderCodec.Write(ref platform, header, value);
	}

	private static bool WriteHeaderCount<TPlatform>(ref TPlatform platform,
		APTR header, uint count) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListHeaderCodec.TryRead(ref platform, header, out var value))
			return false;
		value.Count = count;
		return MuiListHeaderCodec.Write(ref platform, header, value);
	}

	private static bool WriteHeaderImages<TPlatform>(ref TPlatform platform,
		APTR header, APTR images) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListHeaderCodec.TryRead(ref platform, header, out var value))
			return false;
		value.Images = images;
		return MuiListHeaderCodec.Write(ref platform, header, value);
	}

	private static APTR Header<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			ListHeaderKey, out var value) || value == 0) return APTR.Null;
		var header = APTR.FromPointer(value);
		if (!MuiListHeaderCodec.TryRead(ref platform, header, out _))
			return APTR.Null;
		return header;
	}

	private static APTR SlotEntryAt<TPlatform>(ref TPlatform platform, APTR header,
		uint index) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var array = ReadHeaderIndex(ref platform, header);
		var cursor = default(MuiListSlotCursor);
		cursor.Base = array;
		cursor.Index = index;
		return MuiListSlotCursorCodec.TryGetEntry(ref platform, cursor,
			out var address) && MuiListSlotCodec.TryRead(ref platform, address,
			out var value)
			? value.Entry : APTR.Null;
	}

	private static uint SlotFlagsAt<TPlatform>(ref TPlatform platform, APTR header,
		uint index) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var array = ReadHeaderIndex(ref platform, header);
		var cursor = default(MuiListSlotCursor);
		cursor.Base = array;
		cursor.Index = index;
		return MuiListSlotCursorCodec.TryGetEntry(ref platform, cursor,
			out var address) && MuiListSlotCodec.TryRead(ref platform, address,
			out var value)
			? value.Flags : 0;
	}

	private static void WriteSlot<TPlatform>(ref TPlatform platform, APTR header,
		uint index, APTR entry, uint flags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var array = ReadHeaderIndex(ref platform, header);
		var cursor = default(MuiListSlotCursor);
		cursor.Base = array;
		cursor.Index = index;
		if (!MuiListSlotCursorCodec.TryGetEntry(ref platform, cursor,
			out var slot)) return;
		var value = default(MuiListSlotState);
		value.Entry = entry;
		value.Flags = flags;
		MuiListSlotCodec.Write(ref platform, slot, value);
	}

	private static bool TryWriteSlot<TPlatform>(ref TPlatform platform,
		APTR header, uint index, MuiListSlotState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var array = ReadHeaderIndex(ref platform, header);
		var cursor = default(MuiListSlotCursor);
		cursor.Base = array;
		cursor.Index = index;
		return MuiListSlotCursorCodec.TryGetEntry(ref platform, cursor,
			out var slot) && MuiListSlotCodec.Write(ref platform, slot, value);
	}

	private static int ActiveIndex<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		// MorphOS 3.20's public empty-list projection is zero, but zero must not
		// become a real row for Active/Remove/Redraw/selection selectors. The
		// named cursor record also distinguishes an empty-list zero from a real
		// row zero immediately after the first insertion.
		if (EntryCount(ref platform, state, obj) == 0) return -1;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ActiveStateKey, 0));
		var raw = unchecked((int)Read(ref platform, state, obj, Active,
			ActiveOff));
		if (TryReadActiveState(ref platform, block, out var cursor))
		{
			// A low-level construction/test writer may publish a nonzero raw
			// projection before the class-aware setter has synchronized the named
			// cursor. Preserve that compatibility path; the canonical empty value
			// remains zero with HasActive clear.
			if (cursor.HasActive == 0)
				return raw == 0 ? -1 : raw;
			return unchecked((int)cursor.Active);
		}
		return raw;
	}

	// Class composites need the selector view of the cursor, not the MorphOS
	// empty-list getter projection. Keep that distinction behind one internal
	// seam so Listview and future collection wrappers do not inspect raw state.
	internal static int ActiveRow<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform =>
		ActiveIndex(ref platform, state, obj);

	internal static bool TryGetActiveState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListActiveState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ActiveStateKey, 0));
		return TryReadActiveState(ref platform, block, out value);
	}

	private static void SetActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint value) where TPlatform : struct, IMuiHeadlessPlatform
	{
		SetNotify(ref platform, state, obj, Active, value);
		SetActiveCursor(ref platform, state, obj, value,
			EntryCount(ref platform, state, obj) != 0 &&
			unchecked((int)value) >= 0);
	}

	private static void ToggleSelectChange<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var value = SelectionSignalValue(ref platform, state, obj);
		var next = value == 0 ? 1u : 0u;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			SelectChange, next, true)) return;
		SetSelectionSignalState(ref platform, state, obj, next);
		// Listview exposes the same selection-change signal as its owned List.
		// Mirror once at the parent boundary; the parent has no owner link, so
		// this cannot recurse back into the child.
		var owner = APTR.FromPointer(Read(ref platform, state, obj,
			ListviewOwnerKey, 0));
		if (owner.IsNotNull && Classify(ref platform, state, owner) ==
			MuiCollectionClass.Listview)
			MuiListviewCore.ToggleSelectionSignal(ref platform, state, owner);
	}

	private static uint SelectionSignalValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		return TryReadSelectionSignalState(ref platform, block, out var signal)
			? signal.Value
			: Read(ref platform, state, obj, SelectChange, 0);
	}

	internal static bool TryGetSelectionSignal<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListSelectionSignalState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		return TryReadSelectionSignalState(ref platform, block, out value);
	}

	private static void Publish<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint count) where TPlatform : struct, IMuiHeadlessPlatform
	{
		// A changed entry set invalidates measured -1 width limits. The next
		// Layout rebuilds the named guest metrics record from the display hook.
		FreeColumnMetrics(ref platform, state, obj);
		SetNotify(ref platform, state, obj, Entries, count);
		// Keep the named viewport record in the same publication boundary so
		// scroller metrics cannot lag behind the guest header count after an
		// insert, remove, or clear operation.
		RefreshViewportState(ref platform, state, obj);
	}

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var value) ? value : fallback;

	private static bool ReadRenderPort<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out APTR rastPort)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		rastPort = APTR.Null;
		var info = APTR.FromPointer(Read(ref platform, state, obj, RenderInfo, 0));
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, info,
			out var renderInfo)) return false;
		rastPort = renderInfo.RastPort;
		return rastPort.IsNotNull;
	}

	private static void SetInternal<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, false);

	private static void ClearInternal<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetExistingAttribute(ref platform, state, obj,
			attribute, 0);

	private static bool SetRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetRecordAttribute(ref platform, state, record,
			attribute, value, notify);

	// Change-only: only writes (and notifies) when the value actually differs.
	private static void SetNotify<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var current) && current == value) return;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, true);
	}

	private static void EnsureDefault<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out _))
			MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
				value, false);
	}
}
