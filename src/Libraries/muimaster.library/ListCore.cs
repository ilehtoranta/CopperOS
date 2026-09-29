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

// Named cursor for the bounded class-name STRPTR used by collection and
// Family classification. The consumer supplies only a logical byte index;
// the adapter owns the 64-byte bound, overflow guard, and mapped-byte check.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiClassNameByteCursor
{
	internal const uint MaximumLength = 64;
	internal APTR Name;
	internal uint Index;
}

internal static class MuiClassNameByteCursorCodec
{
	internal static bool TryGetByte<TPlatform>(ref TPlatform platform,
		MuiClassNameByteCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Name;
		shared.Index = cursor.Index;
		shared.Limit = MuiClassNameByteCursor.MaximumLength;
		return MuiCStringByteCursorCodec.TryGetAddress(ref platform, shared,
			out address);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiClassNameByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetByte(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}

	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		MuiClassNameByteCursor cursor, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetByte(ref platform, cursor, out var address)) return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}
}

// Named cursor for a bounded List FORMAT byte scan. The parser carries the
// guest STRPTR and its already-validated length; all byte addressing and
// mapped-memory checks stay inside this adapter.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListFormatByteCursor
{
	internal const uint MaximumLength = 4096;
	internal APTR Format;
	internal uint Index;
}

internal static class MuiListFormatByteCursorCodec
{
	internal static bool TryReadAt<TPlatform>(ref TPlatform platform,
		APTR format, int index, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (index < 0) return false;
		var cursor = default(MuiListFormatByteCursor);
		cursor.Format = format;
		cursor.Index = (uint)index;
		return TryReadByte(ref platform, cursor, out value);
	}

	internal static bool TryGetByte<TPlatform>(ref TPlatform platform,
		MuiListFormatByteCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Format;
		shared.Index = cursor.Index;
		shared.Limit = MuiListFormatByteCursor.MaximumLength;
		return MuiCStringByteCursorCodec.TryGetAddress(ref platform, shared,
			out address);
	}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		MuiListFormatByteCursor cursor, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetByte(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt8(address, 0);
		return true;
	}
}

// Named bounded destination cursor for List FORMAT PREPARSE strings. The
// preparser carries the allocated guest base, logical output index, and
// capacity; all destination range and mapping checks stay in this adapter.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListPreparseByteCursor
{
	internal const uint MaximumLength = MuiListFormatByteCursor.MaximumLength + 1;
	internal APTR Base;
	internal uint Index;
	internal uint Capacity;
}

internal static class MuiListPreparseByteCursorCodec
{
	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		MuiListPreparseByteCursor cursor, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (cursor.Capacity == 0 || cursor.Capacity >
			MuiListPreparseByteCursor.MaximumLength) return false;
		var shared = default(MuiCStringByteCursor);
		shared.Base = cursor.Base;
		shared.Index = cursor.Index;
		shared.Limit = cursor.Capacity;
		if (!MuiCStringByteCursorCodec.TryGetAddress(ref platform, shared,
			out var address)) return false;
		platform.WriteUInt8(address, 0, value);
		return true;
	}
}

// Fixed guest-owned List header. The slot array, capacity/count metadata, and
// image-chain head travel together so List operations consume named fields
// rather than repeating private header offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListHeaderState
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x4C495354u; // 'LIST'
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint IndexOffset = 4;
	internal const uint CapacityOffset = 8;
	internal const uint CountOffset = 12;
	internal const uint ImagesOffset = 16;

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

internal static class MuiListHeaderMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiListHeaderField field,
		out uint index)
	{
		index = field switch
		{
			MuiListHeaderField.Magic => 0,
			MuiListHeaderField.Index => 1,
			MuiListHeaderField.Capacity => 2,
			MuiListHeaderField.Count => 3,
			MuiListHeaderField.Images => 4,
			_ => uint.MaxValue,
		};
		return index != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListHeaderField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryGetAddress(ref platform, record, field, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListHeaderField field, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = 0;
		if (!TryResolveFieldIndex(field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, record,
				MuiListHeaderState.Size, out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiListHeaderState.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				size = MuiListHeaderState.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHeaderField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiListHeaderCodec.TryReadStructural(ref platform, address,
			out var header)) return false;
		switch (field)
		{
			case MuiListHeaderField.Magic:
				value = header.Magic; return true;
			case MuiListHeaderField.Index:
				value = header.Index.Raw; return true;
			case MuiListHeaderField.Capacity:
				value = header.Capacity; return true;
			case MuiListHeaderField.Count:
				value = header.Count; return true;
			case MuiListHeaderField.Images:
				value = header.Images.Raw; return true;
		}
		return false;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHeaderField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListHeaderCodec.TryReadStructural(ref platform, address,
			out var header)) return false;
		switch (field)
		{
			case MuiListHeaderField.Magic:
				header.Magic = value; break;
			case MuiListHeaderField.Index:
				header.Index = APTR.FromPointer(value); break;
			case MuiListHeaderField.Capacity:
				header.Capacity = value; break;
			case MuiListHeaderField.Count:
				header.Count = value; break;
			case MuiListHeaderField.Images:
				header.Images = APTR.FromPointer(value); break;
			default:
				return false;
		}
		return MuiListHeaderCodec.WriteRecord(ref platform, address, header);
	}
}

internal static class MuiListHeaderFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListHeaderFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListHeaderMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListHeaderFieldCursor cursor, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListHeaderMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Field, out address, out size);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHeaderField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListHeaderMemoryCodec.TryReadUInt32(ref platform, address, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHeaderField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListHeaderMemoryCodec.TryWriteUInt32(ref platform, address, field,
			value);
}

internal static class MuiListHeaderCodec
{
	// The List header is a fixed five-field guest record. Keep the declaration
	// order explicit so production paths exchange one named struct while the
	// older field adapter remains available for bounded corruption tests.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListHeaderState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListHeaderState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var index) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Capacity) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Count) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var images)) return false;
		value.Index = APTR.FromPointer(index);
		value.Images = APTR.FromPointer(images);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHeaderState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListHeaderState.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Index.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Capacity) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Count) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Images.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListHeaderState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadStorage(ref platform, address, out value) ||
			value.Magic != MuiListHeaderState.Cookie)
		{
			value = default;
			return false;
		}
		return true;
	}

	// Read all named fields without trusting the runtime cookie. The List core
	// applies its bounded capacity/index validation separately so disposal can
	// recover a structurally valid header after cookie damage.
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListHeaderState value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	// Keep the older storage-named entry point for ListCore's recovery path;
	// both names intentionally share the same lossless structural decoder.
	internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListHeaderState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListHeaderState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListHeaderState.Size) || value.Magic != MuiListHeaderState.Cookie)
			return false;
		return WriteRecord(ref platform, address, value);
	}
}

// A List adopted by Listview keeps the reverse relationship in a bounded
// guest record.  The record is state, not a second public object-layout word;
// selection propagation can therefore reject malformed ownership without
// consulting an untrusted scalar pointer.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewOwnerState
{
	internal const uint Size = 8;
	internal const uint Cookie = 0x4C564F57u; // 'LVOW'
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint OwnerOffset = 4;

	internal uint Magic;
	internal APTR Owner;
}

internal enum MuiListviewOwnerStateField : byte
{
	Magic,
	Owner,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListviewOwnerStateFieldCursor
{
	internal APTR Record;
	internal MuiListviewOwnerStateField Field;
}

internal static class MuiListviewOwnerStateMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiListviewOwnerStateField field,
		out uint index)
	{
		index = field switch
		{
			MuiListviewOwnerStateField.Magic => 0,
			MuiListviewOwnerStateField.Owner => 1,
			_ => uint.MaxValue,
		};
		return index != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewOwnerStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryGetAddress(ref platform, record, field, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewOwnerStateField field, out APTR address,
		out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = 0;
		if (!TryResolveFieldIndex(field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, record,
				MuiListviewOwnerState.Size, out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiListviewOwnerState.FieldSize, out var candidate))
				return false;
			if (current == index)
			{
				address = candidate;
				size = MuiListviewOwnerState.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewOwnerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiListviewOwnerStateCodec.TryReadStructural(ref platform, record,
			out var owner)) return false;
		if (field == MuiListviewOwnerStateField.Magic)
			value = owner.Magic;
		else if (field == MuiListviewOwnerStateField.Owner)
			value = owner.Owner.Raw;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewOwnerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListviewOwnerStateCodec.TryReadStructural(ref platform, record,
			out var owner)) return false;
		if (field == MuiListviewOwnerStateField.Magic)
			owner.Magic = value;
		else if (field == MuiListviewOwnerStateField.Owner)
			owner.Owner = APTR.FromPointer(value);
		else return false;
		return MuiListviewOwnerStateCodec.WriteRecord(ref platform, record,
			owner);
	}
}

internal static class MuiListviewOwnerStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListviewOwnerStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewOwnerStateMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListviewOwnerStateFieldCursor cursor, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewOwnerStateMemoryCodec.TryGetAddress(ref platform,
			cursor.Record, cursor.Field, out address, out size);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewOwnerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewOwnerStateMemoryCodec.TryReadUInt32(ref platform, record,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiListviewOwnerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListviewOwnerStateMemoryCodec.TryWriteUInt32(ref platform, record,
			field, value);
}

internal static class MuiListviewOwnerStateCodec
{
	// Listview ownership is a fixed two-ULONG relationship record. Keep the
	// owner pointer beside its cookie in declaration order so adoption and
	// teardown exchange one named struct rather than raw offsets.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListviewOwnerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListviewOwnerState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var owner)) return false;
		value.Owner = APTR.FromPointer(owner);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiListviewOwnerState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListviewOwnerState.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Owner.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListviewOwnerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadRecord(ref platform, address, out value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListviewOwnerState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiListviewOwnerState.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListviewOwnerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewOwnerState.Size) || value.Magic !=
			MuiListviewOwnerState.Cookie) return false;
		return WriteRecord(ref platform, address, value);
	}

	internal static bool Clear<TPlatform>(ref TPlatform platform, APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListviewOwnerState.Size)) return false;
		platform.Clear(address, MuiListviewOwnerState.Size);
		return true;
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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint PolicyOffset = 4;
	internal const uint ContentWidthOffset = 8;
	internal const uint ViewWidthOffset = 12;
	internal const uint VisibleOffset = 16;
	internal const uint ScrollXOffset = 20;
	internal const uint MaxScrollXOffset = 24;

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

internal static class MuiListHScrollerStateMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiListHScrollerStateField field,
		out uint index)
	{
		index = field switch
		{
			MuiListHScrollerStateField.Magic => 0,
			MuiListHScrollerStateField.Policy => 1,
			MuiListHScrollerStateField.ContentWidth => 2,
			MuiListHScrollerStateField.ViewWidth => 3,
			MuiListHScrollerStateField.Visible => 4,
			MuiListHScrollerStateField.ScrollX => 5,
			MuiListHScrollerStateField.MaxScrollX => 6,
			_ => uint.MaxValue,
		};
		return index != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListHScrollerStateField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryGetAddress(ref platform, record, field, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListHScrollerStateField field, out APTR address,
		out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = 0;
		if (!TryResolveFieldIndex(field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, record,
				MuiListHScrollerState.Size, out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiListHScrollerState.FieldSize, out var candidate))
				return false;
			if (current == index)
			{
				address = candidate;
				size = MuiListHScrollerState.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHScrollerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiListHScrollerStateCodec.TryReadStructural(ref platform, address,
			out var state)) return false;
		if (field == MuiListHScrollerStateField.Magic)
			value = state.Magic;
		else if (field == MuiListHScrollerStateField.Policy)
			value = state.Policy;
		else if (field == MuiListHScrollerStateField.ContentWidth)
			value = state.ContentWidth;
		else if (field == MuiListHScrollerStateField.ViewWidth)
			value = state.ViewWidth;
		else if (field == MuiListHScrollerStateField.Visible)
			value = state.Visible;
		else if (field == MuiListHScrollerStateField.ScrollX)
			value = state.ScrollX;
		else if (field == MuiListHScrollerStateField.MaxScrollX)
			value = state.MaxScrollX;
		else return false;
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHScrollerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListHScrollerStateCodec.TryReadStructural(ref platform, address,
			out var state)) return false;
		if (field == MuiListHScrollerStateField.Magic)
			state.Magic = value;
		else if (field == MuiListHScrollerStateField.Policy)
			state.Policy = value;
		else if (field == MuiListHScrollerStateField.ContentWidth)
			state.ContentWidth = value;
		else if (field == MuiListHScrollerStateField.ViewWidth)
			state.ViewWidth = value;
		else if (field == MuiListHScrollerStateField.Visible)
			state.Visible = value;
		else if (field == MuiListHScrollerStateField.ScrollX)
			state.ScrollX = value;
		else if (field == MuiListHScrollerStateField.MaxScrollX)
			state.MaxScrollX = value;
		else return false;
		return MuiListHScrollerStateCodec.WriteRecord(ref platform, address,
			state);
	}
}

internal static class MuiListHScrollerStateFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListHScrollerStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListHScrollerStateMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListHScrollerStateFieldCursor cursor, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListHScrollerStateMemoryCodec.TryGetAddress(ref platform,
			cursor.Address, cursor.Field, out address, out size);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHScrollerStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListHScrollerStateMemoryCodec.TryReadUInt32(ref platform, address,
			field, out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHScrollerStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListHScrollerStateMemoryCodec.TryWriteUInt32(ref platform, address,
			field, value);
}

internal static class MuiListHScrollerStateCodec
{
	// Horizontal-scroller state is a fixed seven-ULONG record. Keep policy,
	// dimensions, and scroll range in declaration order so composition consumes
	// one named struct instead of separate scalar offsets.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListHScrollerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListHScrollerState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Policy) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ContentWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ViewWidth) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Visible) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.ScrollX) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.MaxScrollX)) return false;
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiListHScrollerState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListHScrollerState.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Policy) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ContentWidth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ViewWidth) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Visible) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ScrollX) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.MaxScrollX) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListHScrollerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return TryReadRecord(ref platform, address, out value);
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListHScrollerState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiListHScrollerState.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListHScrollerState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListHScrollerState.Size) || value.Magic !=
			MuiListHScrollerState.Cookie) return false;
		return WriteRecord(ref platform, address, value);
	}
}

// One guest-resident entry in the contiguous List index. The surrounding
// array is an explicit ABI boundary; each fixed-size element is decoded as a
// named record so consumers never repeat its member offsets.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiListSlotState
{
	internal const uint Size = 8;
	internal const uint FieldSize = 4;
	internal const uint EntryOffset = 0;
	internal const uint FlagsOffset = 4;

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

internal static class MuiListSlotMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiListSlotField field,
		out uint index)
	{
		index = field switch
		{
			MuiListSlotField.Entry => 0,
			MuiListSlotField.Flags => 1,
			_ => uint.MaxValue,
		};
		return index != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListSlotField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryGetAddress(ref platform, record, field, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListSlotField field, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = 0;
		if (!TryResolveFieldIndex(field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, record,
				MuiListSlotState.Size, out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiListSlotState.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				size = MuiListSlotState.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListSlotField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiListSlotCodec.TryReadStructural(ref platform, address,
			out var state)) return false;
		switch (field)
		{
			case MuiListSlotField.Entry:
				value = state.Entry.Raw;
				return true;
			case MuiListSlotField.Flags:
				value = state.Flags;
				return true;
			default:
				return false;
		}
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListSlotField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListSlotCodec.TryReadStructural(ref platform, address,
			out var state)) return false;
		switch (field)
		{
			case MuiListSlotField.Entry:
				state.Entry = APTR.FromPointer(value);
				break;
			case MuiListSlotField.Flags:
				state.Flags = value;
				break;
			default:
				return false;
		}
		return MuiListSlotCodec.WriteRecord(ref platform, address, state);
	}
}

internal static class MuiListSlotFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListSlotFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListSlotMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListSlotFieldCursor cursor, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListSlotMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Field, out address, out size);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListSlotField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListSlotMemoryCodec.TryReadUInt32(ref platform, address, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListSlotField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListSlotMemoryCodec.TryWriteUInt32(ref platform, address, field,
			value);
}

internal static class MuiListSlotCodec
{
	// Each index element is a fixed two-ULONG record. Exchange both named
	// fields sequentially in production; vector arithmetic stays in the
	// bounded adapter above for callers that need an element address.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListSlotState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListSlotState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var entry) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags)) return false;
		value.Entry = APTR.FromPointer(entry);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiListSlotState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListSlotState.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Entry.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Flags) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListSlotState value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListSlotState value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListSlotState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListSlotState.Size)) return false;
		return WriteRecord(ref platform, address, value);
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
	internal const uint FieldSize = 4;
	internal const uint MagicOffset = 0;
	internal const uint ImageObjectOffset = 4;
	internal const uint FlagsOffset = 8;
	internal const uint NextOffset = 12;

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

internal static class MuiListImageMemoryCodec
{
	private static bool TryResolveFieldIndex(MuiListImageField field,
		out uint index)
	{
		index = field switch
		{
			MuiListImageField.Magic => 0,
			MuiListImageField.ImageObject => 1,
			MuiListImageField.Flags => 2,
			MuiListImageField.Next => 3,
			_ => uint.MaxValue,
		};
		return index != uint.MaxValue;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListImageField field, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		return TryGetAddress(ref platform, record, field, out address, out _);
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		APTR record, MuiListImageField field, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		size = 0;
		if (!TryResolveFieldIndex(field, out var index) ||
			!MuiGuestStructCursor.TryCreate(ref platform, record,
				MuiListImageState.Size, out var cursor)) return false;
		for (var current = 0u; current <= index; current++)
		{
			if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
				MuiListImageState.FieldSize, out var candidate)) return false;
			if (current == index)
			{
				address = candidate;
				size = MuiListImageState.FieldSize;
				return true;
			}
		}
		return false;
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListImageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!MuiListImageCodec.TryReadStructural(ref platform, address,
			out var state)) return false;
		switch (field)
		{
			case MuiListImageField.Magic:
				value = state.Magic;
				return true;
			case MuiListImageField.ImageObject:
				value = state.ImageObject.Raw;
				return true;
			case MuiListImageField.Flags:
				value = state.Flags;
				return true;
			case MuiListImageField.Next:
				value = state.Next.Raw;
				return true;
			default:
				return false;
		}
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListImageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiListImageCodec.TryReadStructural(ref platform, address,
			out var state)) return false;
		switch (field)
		{
			case MuiListImageField.Magic:
				state.Magic = value;
				break;
			case MuiListImageField.ImageObject:
				state.ImageObject = APTR.FromPointer(value);
				break;
			case MuiListImageField.Flags:
				state.Flags = value;
				break;
			case MuiListImageField.Next:
				state.Next = APTR.FromPointer(value);
				break;
			default:
				return false;
		}
		return MuiListImageCodec.WriteRecord(ref platform, address, state);
	}
}

internal static class MuiListImageFieldCursorCodec
{
	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListImageFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListImageMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Field, out address);

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiListImageFieldCursor cursor, out APTR address, out uint size)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListImageMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Field, out address, out size);

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListImageField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListImageMemoryCodec.TryReadUInt32(ref platform, address, field,
			out value);

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR address, MuiListImageField field, uint value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListImageMemoryCodec.TryWriteUInt32(ref platform, address, field,
			value);
}

internal static class MuiListImageCodec
{
	// Image handles are fixed four-field records. Keep the image object and
	// chain pointers beside their flags in declaration order so production
	// handle lifecycle code exchanges one named struct.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListImageState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListImageState.Size, out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Magic) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var imageObject) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out value.Flags) ||
			!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
				out var next)) return false;
		value.ImageObject = APTR.FromPointer(imageObject);
		value.Next = APTR.FromPointer(next);
		return MuiGuestStructCursor.IsComplete(cursor);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiListImageState value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref platform, address,
			MuiListImageState.Size, out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Magic) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.ImageObject.Raw) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Flags) &&
		MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
			value.Next.Raw) && MuiGuestStructCursor.IsComplete(cursor);

	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListImageState value)
		where TPlatform : struct, IMuiGuestMemory
		=> TryReadRecord(ref platform, address, out value);

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiListImageState value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiListImageState.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiListImageState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiListImageState.Size) || value.Magic != MuiListImageState.Cookie)
			return false;
		return WriteRecord(ref platform, address, value);
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

	// Struct-first guest-memory adapter for the private List entry/flag vector.
	// Complete fixed-size slots are admitted here; the typed cursor below is a
	// compatibility wrapper so no consumer has to reproduce vector arithmetic.
	internal static class MuiListSlotVectorMemoryCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (vector.IsNull || index >= MuiListSlotCursor.MaximumEntries ||
				index > (uint.MaxValue - vector.Raw) /
				MuiListSlotState.Size) return false;
			var offset = index * MuiListSlotState.Size;
			if (vector.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(vector.Raw + offset);
			return platform.IsMapped(address, MuiListSlotState.Size);
		}
	}

	// Production bridge for the private List index vector. The bounded adapter
	// owns slot address arithmetic; mutation and lookup paths exchange complete
	// named entry/flag records.
	internal static class MuiListSlotVectorCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			MuiListSlotCursor cursor, out MuiListSlotState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiListSlotCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			return MuiListSlotCodec.TryRead(ref platform, address, out value);
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			MuiListSlotCursor cursor, MuiListSlotState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListSlotCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			return MuiListSlotCodec.Write(ref platform, address, value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out MuiListSlotState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiListSlotVectorMemoryCodec.TryGetEntry(ref platform, vector,
				index, out var address)) return false;
			return MuiListSlotCodec.TryRead(ref platform, address, out value);
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, MuiListSlotState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListSlotVectorMemoryCodec.TryGetEntry(ref platform, vector,
				index, out var address)) return false;
			return MuiListSlotCodec.Write(ref platform, address, value);
		}
	}

	internal static class MuiListSlotCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListSlotCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiListSlotVectorMemoryCodec.TryGetEntry(ref platform,
				cursor.Base, cursor.Index, out address);
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
			=> MuiListPointerVectorMemoryCodec.TryGetEntry(ref platform,
				cursor.Base, cursor.Index, out address);
	}

	// Struct-first guest-memory adapter for caller-owned List pointer vectors.
	// The large MorphOS entry bound and complete 4-byte slot admission live in
	// this boundary; typed cursors remain compatibility wrappers.
	internal static class MuiListPointerVectorMemoryCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (vector.IsNull || index >=
				MuiListPointerVectorCursor.MaximumEntries || index >
				(uint.MaxValue - vector.Raw) /
				MuiListPointerSlotRecord.Size) return false;
			var offset = index * MuiListPointerSlotRecord.Size;
			if (vector.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(vector.Raw + offset);
			return platform.IsMapped(address, MuiListPointerSlotRecord.Size);
		}
	}

	// Production bridge for caller-owned List entry vectors. The bounded adapter
	// owns slot address arithmetic; consumers exchange the named pointer-slot
	// record (or its scalar APTR projection) instead of exposing slot addresses.
	internal static class MuiListPointerVectorCodec
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			MuiListPointerVectorCursor cursor, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListPointerVectorCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			return MuiListPointerSlotCodec.TryReadValue(ref platform, address,
				out value);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
			MuiListPointerVectorCursor cursor, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListPointerVectorCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			return MuiListPointerSlotCodec.WriteValue(ref platform, address, value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			MuiListPointerVectorCursor cursor,
			out MuiListPointerSlotRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!TryReadValue(ref platform, cursor, out var rawValue)) return false;
			value.Value = APTR.FromPointer(rawValue);
			return true;
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			MuiListPointerVectorCursor cursor,
			MuiListPointerSlotRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryWriteValue(ref platform, cursor, value.Value.Raw);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListPointerVectorMemoryCodec.TryGetEntry(ref platform,
				vector, index, out var address)) return false;
			return MuiListPointerSlotCodec.TryReadValue(ref platform, address,
				out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out MuiListPointerSlotRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!TryReadValue(ref platform, vector, index, out var rawValue))
				return false;
			value.Value = APTR.FromPointer(rawValue);
			return true;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListPointerVectorMemoryCodec.TryGetEntry(ref platform,
				vector, index, out var address)) return false;
			return MuiListPointerSlotCodec.WriteValue(ref platform, address,
				value);
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, MuiListPointerSlotRecord value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryWriteValue(ref platform, vector, index, value.Value.Raw);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListEditState
	{
		internal const uint Size = 24;
		internal const uint Cookie = 0x4C454449u; // 'LEDI'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint RowOffset = 4;
		internal const uint ColumnOffset = 8;
		internal const uint EntryOffset = 12;
		internal const uint EditObjectOffset = 16;
		internal const uint FlagsOffset = 20;
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

	internal static class MuiListEditMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListEditField field,
			out uint index)
		{
			index = field switch
			{
				MuiListEditField.Magic => 0,
				MuiListEditField.Row => 1,
				MuiListEditField.Column => 2,
				MuiListEditField.Entry => 3,
				MuiListEditField.EditObject => 4,
				MuiListEditField.Flags => 5,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListEditField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListEditField field, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListEditState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListEditState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListEditState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListEditField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListEditStateCodec.TryReadRecord(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListEditField.Magic:
					value = state.Magic;
					return true;
				case MuiListEditField.Row:
					value = unchecked((uint)state.Row);
					return true;
				case MuiListEditField.Column:
					value = unchecked((uint)state.Column);
					return true;
				case MuiListEditField.Entry:
					value = state.Entry.Raw;
					return true;
				case MuiListEditField.EditObject:
					value = state.EditObject.Raw;
					return true;
				case MuiListEditField.Flags:
					value = state.Flags;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListEditField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListEditStateCodec.TryReadRecord(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListEditField.Magic:
					state.Magic = value;
					break;
				case MuiListEditField.Row:
					state.Row = unchecked((int)value);
					break;
				case MuiListEditField.Column:
					state.Column = unchecked((int)value);
					break;
				case MuiListEditField.Entry:
					state.Entry = APTR.FromPointer(value);
					break;
				case MuiListEditField.EditObject:
					state.EditObject = APTR.FromPointer(value);
					break;
				case MuiListEditField.Flags:
					state.Flags = value;
					break;
				default:
					return false;
			}
			return MuiListEditStateCodec.WriteRecord(ref platform, address, state);
		}
	}

	internal static class MuiListEditFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListEditFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListEditMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListEditFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListEditMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListEditField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListEditMemoryCodec.TryReadUInt32(ref platform, address, field,
				out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListEditField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListEditMemoryCodec.TryWriteUInt32(ref platform, address, field,
				value);
	}

	internal static class MuiListEditStateCodec
	{
		// Edit state is a fixed six-field record with signed row/column LONGs and
		// two opaque pointers. Preserve the exact guest widths while exchanging the
		// complete declaration-ordered struct through the bounded cursor.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListEditState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListEditState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var row) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var column) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var entry) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var editObject) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Flags)) return false;
			value.Row = unchecked((int)row);
			value.Column = unchecked((int)column);
			value.Entry = APTR.FromPointer(entry);
			value.EditObject = APTR.FromPointer(editObject);
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListEditState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListEditState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Row)) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				unchecked((uint)value.Column)) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Entry.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.EditObject.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListEditState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListEditState.Size) || value.Magic != MuiListEditState.Cookie)
				return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListEditState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListEditState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListEditState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListEditState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
		}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnGeometry
	{
		public const uint Size = 8;
		public const uint FieldSize = 4;
		public const uint OffsetOffset = 0;
		public const uint WidthOffset = 4;
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

	internal static class MuiListColumnGeometryMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListColumnGeometryField field,
			out uint index)
		{
			index = field switch
			{
				MuiListColumnGeometryField.Offset => 0,
				MuiListColumnGeometryField.Width => 1,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnGeometryField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnGeometryField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListColumnGeometry.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListColumnGeometry.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListColumnGeometry.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnGeometryField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListColumnGeometryCodec.TryReadRecord(ref platform, address,
				out var geometry)) return false;
			switch (field)
			{
				case MuiListColumnGeometryField.Offset:
					value = geometry.Offset;
					return true;
				case MuiListColumnGeometryField.Width:
					value = geometry.Width;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnGeometryField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnGeometryCodec.TryReadRecord(ref platform, address,
				out var geometry)) return false;
			switch (field)
			{
				case MuiListColumnGeometryField.Offset:
					geometry.Offset = value;
					break;
				case MuiListColumnGeometryField.Width:
					geometry.Width = value;
					break;
				default:
					return false;
			}
			return MuiListColumnGeometryCodec.WriteRecord(ref platform, address,
				geometry);
		}
	}

	internal static class MuiListColumnGeometryFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnGeometryFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnGeometryMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnGeometryFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnGeometryMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnGeometryField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnGeometryMemoryCodec.TryReadUInt32(ref platform, address,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnGeometryField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnGeometryMemoryCodec.TryWriteUInt32(ref platform, address,
				field, value);
	}

	internal static class MuiListColumnGeometryCodec
	{
		// Geometry entries are fixed two-ULONG records. Keep the production
		// representation sequential and leave address arithmetic to the bounded
		// vector adapter below.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListColumnGeometry value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiListColumnGeometry.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Offset) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Width)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnGeometry value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiListColumnGeometry.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Offset) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Width) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListColumnGeometry value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryReadRecord(ref platform, address, out value);

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnGeometry value)
			where TPlatform : struct, IMuiGuestMemory
			=> WriteRecord(ref platform, address, value);
	}

	// Layout publishes a named owner record for the contiguous geometry vector.
	// Keeping the vector pointer, width, and column count together prevents a
	// stale scalar alias from being paired with an unrelated guest allocation.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnLayoutState
	{
		internal const uint Size = 16;
		internal const uint Cookie = 0x434C4159u; // 'CLAY'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint WidthOffset = 4;
		internal const uint ColumnsOffset = 8;
		internal const uint ValuesOffset = 12;
		internal uint Magic;
		internal uint Width;
		internal uint Columns;
		internal APTR Values;
	}

	internal enum MuiListColumnLayoutField : byte
	{
		Magic,
		Width,
		Columns,
		Values,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnLayoutFieldCursor
	{
		internal APTR Address;
		internal MuiListColumnLayoutField Field;
	}

	internal static class MuiListColumnLayoutMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListColumnLayoutField field,
			out uint index)
		{
			index = field switch
			{
				MuiListColumnLayoutField.Magic => 0,
				MuiListColumnLayoutField.Width => 1,
				MuiListColumnLayoutField.Columns => 2,
				MuiListColumnLayoutField.Values => 3,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnLayoutField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnLayoutField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListColumnLayoutState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListColumnLayoutState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListColumnLayoutState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnLayoutField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListColumnLayoutStateCodec.TryReadRecord(ref platform, address,
				out var layout)) return false;
			switch (field)
			{
				case MuiListColumnLayoutField.Magic:
					value = layout.Magic;
					return true;
				case MuiListColumnLayoutField.Width:
					value = layout.Width;
					return true;
				case MuiListColumnLayoutField.Columns:
					value = layout.Columns;
					return true;
				case MuiListColumnLayoutField.Values:
					value = layout.Values.Raw;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnLayoutField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnLayoutStateCodec.TryReadRecord(ref platform, address,
				out var layout)) return false;
			switch (field)
			{
				case MuiListColumnLayoutField.Magic:
					layout.Magic = value;
					break;
				case MuiListColumnLayoutField.Width:
					layout.Width = value;
					break;
				case MuiListColumnLayoutField.Columns:
					layout.Columns = value;
					break;
				case MuiListColumnLayoutField.Values:
					layout.Values = APTR.FromPointer(value);
					break;
				default:
					return false;
			}
			return MuiListColumnLayoutStateCodec.WriteRecord(ref platform, address,
				layout);
		}
	}

	internal static class MuiListColumnLayoutFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnLayoutFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnLayoutMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnLayoutFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnLayoutMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnLayoutField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnLayoutMemoryCodec.TryReadUInt32(ref platform, address,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnLayoutField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnLayoutMemoryCodec.TryWriteUInt32(ref platform, address,
				field, value);
	}

	internal static class MuiListColumnLayoutStateCodec
	{
		// Layout ownership is a fixed four-ULONG record. Keep structural access
		// in declaration order so geometry consumers receive a named value and
		// only the bounded guest adapter performs wire-address arithmetic.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnLayoutState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListColumnLayoutState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Width) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Columns) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var values)) return false;
			value.Values = APTR.FromPointer(values);
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListColumnLayoutState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListColumnLayoutState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Width) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Columns) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Values.Raw) &&
			MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListColumnLayoutState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListColumnLayoutState.Size) || value.Magic !=
				MuiListColumnLayoutState.Cookie || value.Width > int.MaxValue ||
				value.Columns == 0 || value.Columns > MaximumGeometryColumns ||
				value.Values.IsNull || !platform.IsMapped(value.Values,
					value.Columns * MuiListColumnGeometry.Size)) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnLayoutState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListColumnLayoutState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryReadStructural(ref platform, block, out value) ||
				value.Magic != MuiListColumnLayoutState.Cookie ||
				value.Width > int.MaxValue || value.Columns == 0 ||
				value.Columns > MaximumGeometryColumns || value.Values.IsNull ||
				!platform.IsMapped(value.Values,
					value.Columns * MuiListColumnGeometry.Size))
			{
				value = default;
				return false;
			}
			return true;
		}

		// Teardown may use bounded structural fields after runtime admission has
		// rejected the record. The vector pointer is returned only for a bounded
		// column count; callers still verify its mapping before clearing/freeing.
		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnLayoutState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryReadStructural(ref platform, block, out value) ||
				value.Columns == 0 || value.Columns > MaximumGeometryColumns)
			{
				value = default;
				return false;
			}
			return true;
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

	// Struct-first guest-memory adapter for the fixed {offset,width} geometry
	// vector. Complete records and the 256-column MorphOS bound are enforced at
	// this boundary; the cursor below remains a compatibility wrapper.
	internal static class MuiListColumnGeometryVectorMemoryCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (vector.IsNull || index >= MuiListColumnGeometryCursor.MaximumEntries ||
				index > (uint.MaxValue - vector.Raw) /
				MuiListColumnGeometry.Size) return false;
			var offset = index * MuiListColumnGeometry.Size;
			if (vector.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(vector.Raw + offset);
			return platform.IsMapped(address, MuiListColumnGeometry.Size);
		}
	}

	// Production bridge for the cached List geometry vector. The bounded adapter
	// owns index arithmetic; layout and edit-target consumers exchange complete
	// named {offset,width} records.
	internal static class MuiListColumnGeometryVectorCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			MuiListColumnGeometryCursor cursor, out MuiListColumnGeometry value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiListColumnGeometryCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			return MuiListColumnGeometryCodec.TryRead(ref platform, address,
				out value);
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			MuiListColumnGeometryCursor cursor, MuiListColumnGeometry value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnGeometryCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			return MuiListColumnGeometryCodec.Write(ref platform, address, value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out MuiListColumnGeometry value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiListColumnGeometryVectorMemoryCodec.TryGetEntry(ref platform,
				vector, index, out var address)) return false;
			return MuiListColumnGeometryCodec.TryRead(ref platform, address,
				out value);
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, MuiListColumnGeometry value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnGeometryVectorMemoryCodec.TryGetEntry(ref platform,
				vector, index, out var address)) return false;
			return MuiListColumnGeometryCodec.Write(ref platform, address, value);
		}
	}

	internal static class MuiListColumnGeometryCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListColumnGeometryCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiListColumnGeometryVectorMemoryCodec.TryGetEntry(ref platform,
				cursor.Base, cursor.Index, out address);
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
		InsertPosition,
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
		Position,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListStateFieldCursor
	{
		internal APTR Address;
		internal MuiListStateRecordKind Record;
		internal MuiListStateField Field;
	}

	internal static class MuiListStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListStateRecordKind record,
			MuiListStateField field, out uint index, out uint size)
		{
			index = uint.MaxValue;
			size = 0;
			switch (record)
			{
				case MuiListStateRecordKind.InsertPosition:
					size = MuiListInsertPositionState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.Position: index = 1; return true;
					}
					break;
				case MuiListStateRecordKind.TitleArray:
					size = MuiListTitleArrayState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.Pointers: index = 1; return true;
						case MuiListStateField.Count: index = 2; return true;
					}
					break;
				case MuiListStateRecordKind.TitleValue:
					size = MuiListTitleState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.TitleValue: index = 1; return true;
					}
					break;
				case MuiListStateRecordKind.SelectionSignal:
					size = MuiListSelectionSignalState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.SelectionValue: index = 1; return true;
					}
					break;
				case MuiListStateRecordKind.FormatPolicy:
					size = MuiListFormatPolicyState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.FormatValue: index = 1; return true;
						case MuiListStateField.MaxColumnsValue: index = 2; return true;
						case MuiListStateField.FormatColumnsValue: index = 3; return true;
					}
					break;
				case MuiListStateRecordKind.FontPolicy:
					size = MuiListFontState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.FontValue: index = 1; return true;
					}
					break;
				case MuiListStateRecordKind.Redraw:
					size = MuiListRedrawState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.Dirty: index = 1; return true;
						case MuiListStateField.Requests: index = 2; return true;
					}
					break;
				case MuiListStateRecordKind.ActiveCursor:
					size = MuiListActiveState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.HasActive: index = 1; return true;
						case MuiListStateField.Active: index = 2; return true;
					}
					break;
				case MuiListStateRecordKind.ColumnVisibility:
					size = MuiListColumnVisibilityState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.Low: index = 1; return true;
						case MuiListStateField.High: index = 2; return true;
						case MuiListStateField.Word2: index = 3; return true;
						case MuiListStateField.Word3: index = 4; return true;
						case MuiListStateField.Word4: index = 5; return true;
						case MuiListStateField.Word5: index = 6; return true;
						case MuiListStateField.Word6: index = 7; return true;
						case MuiListStateField.Word7: index = 8; return true;
					}
					break;
				case MuiListStateRecordKind.ColumnOrder:
					size = MuiListColumnOrderState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.Count: index = 1; return true;
						case MuiListStateField.Values: index = 2; return true;
						case MuiListStateField.Reserved: index = 3; return true;
					}
					break;
				case MuiListStateRecordKind.Viewport:
					size = MuiListViewportState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.TopPixel: index = 1; return true;
						case MuiListStateField.VisiblePixel: index = 2; return true;
						case MuiListStateField.TotalPixel: index = 3; return true;
						case MuiListStateField.First: index = 4; return true;
						case MuiListStateField.LineHeight: index = 5; return true;
						case MuiListStateField.Visible: index = 6; return true;
						case MuiListStateField.DropMark: index = 7; return true;
					}
					break;
				case MuiListStateRecordKind.InteractionPolicy:
					size = MuiListInteractionPolicyState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.Input: index = 1; return true;
						case MuiListStateField.MultiSelect: index = 2; return true;
						case MuiListStateField.ScrollerPos: index = 3; return true;
					}
					break;
				case MuiListStateRecordKind.ClickState:
					size = MuiListClickState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.ClickColumn: index = 1; return true;
						case MuiListStateField.DoubleClick: index = 2; return true;
						case MuiListStateField.AgainClick: index = 3; return true;
						case MuiListStateField.Clicks: index = 4; return true;
						case MuiListStateField.DefClickColumn: index = 5; return true;
					}
					break;
				case MuiListStateRecordKind.HookPolicy:
					size = MuiListHookPolicyState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.ConstructHook: index = 1; return true;
						case MuiListStateField.DestructHook: index = 2; return true;
						case MuiListStateField.DisplayHook: index = 3; return true;
						case MuiListStateField.CompareHook: index = 4; return true;
						case MuiListStateField.MultiTestHook: index = 5; return true;
					}
					break;
				case MuiListStateRecordKind.SortState:
					size = MuiListSortState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.SortColumn: index = 1; return true;
						case MuiListStateField.TitleClick: index = 2; return true;
					}
					break;
				case MuiListStateRecordKind.PresentationPolicy:
					size = MuiListPresentationPolicyState.Size;
					switch (field)
					{
						case MuiListStateField.Magic: index = 0; return true;
						case MuiListStateField.Editable: index = 1; return true;
						case MuiListStateField.Quiet: index = 2; return true;
						case MuiListStateField.AdjustHeight: index = 3; return true;
						case MuiListStateField.AdjustWidth: index = 4; return true;
						case MuiListStateField.Stripes: index = 5; return true;
						case MuiListStateField.ShowDropMarks: index = 6; return true;
						case MuiListStateField.DragSortable: index = 7; return true;
						case MuiListStateField.DragType: index = 8; return true;
						case MuiListStateField.AutoVisible: index = 9; return true;
						case MuiListStateField.AutoLineHeight: index = 10; return true;
						case MuiListStateField.MinLineHeight: index = 11; return true;
					}
					break;
			}
			return false;
		}


		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListStateRecordKind recordKind,
			MuiListStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, recordKind, field,
				out address, out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListStateRecordKind recordKind,
			MuiListStateField field, out APTR address, out uint fieldSize)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			fieldSize = 0;
			if (!TryResolveFieldIndex(recordKind, field, out var index,
				out var size) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record, size,
					out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor, 4,
					out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					fieldSize = 4;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListStateRecordKind record,
			MuiListStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			switch (record)
			{
				case MuiListStateRecordKind.InsertPosition:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListInsertPositionStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListInsertPositionStateField.Magic, out value);
						case MuiListStateField.Position:
							return MuiListInsertPositionStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListInsertPositionStateField.Position, out value);
					}
					break;
				case MuiListStateRecordKind.TitleArray:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListTitleArrayStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListTitleArrayStateField.Magic, out value);
						case MuiListStateField.Pointers:
							return MuiListTitleArrayStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListTitleArrayStateField.Pointers, out value);
						case MuiListStateField.Count:
							return MuiListTitleArrayStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListTitleArrayStateField.Count, out value);
					}
					break;
				case MuiListStateRecordKind.TitleValue:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListTitleStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListTitleStateField.Magic, out value);
						case MuiListStateField.TitleValue:
							return MuiListTitleStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListTitleStateField.Value, out value);
					}
					break;
				case MuiListStateRecordKind.SelectionSignal:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListSelectionSignalStateMemoryCodec.TryReadUInt32(
								ref platform, address, MuiListSelectionSignalStateField.Magic,
								out value);
						case MuiListStateField.SelectionValue:
							return MuiListSelectionSignalStateMemoryCodec.TryReadUInt32(
								ref platform, address, MuiListSelectionSignalStateField.Value,
								out value);
					}
					break;
				case MuiListStateRecordKind.FormatPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListFormatPolicyStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListFormatPolicyStateField.Magic, out value);
						case MuiListStateField.FormatValue:
							return MuiListFormatPolicyStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListFormatPolicyStateField.Format, out value);
						case MuiListStateField.MaxColumnsValue:
							return MuiListFormatPolicyStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListFormatPolicyStateField.MaxColumns, out value);
						case MuiListStateField.FormatColumnsValue:
							return MuiListFormatPolicyStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListFormatPolicyStateField.Columns, out value);
					}
					break;
				case MuiListStateRecordKind.FontPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListFontStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListFontStateField.Magic, out value);
						case MuiListStateField.FontValue:
							return MuiListFontStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListFontStateField.Font, out value);
					}
					break;
				case MuiListStateRecordKind.Redraw:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListRedrawStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListRedrawStateField.Magic, out value);
						case MuiListStateField.Dirty:
							return MuiListRedrawStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListRedrawStateField.Dirty, out value);
						case MuiListStateField.Requests:
							return MuiListRedrawStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListRedrawStateField.Requests, out value);
					}
					break;
				case MuiListStateRecordKind.ActiveCursor:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListActiveStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListActiveStateField.Magic, out value);
						case MuiListStateField.HasActive:
							return MuiListActiveStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListActiveStateField.HasActive, out value);
						case MuiListStateField.Active:
							return MuiListActiveStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListActiveStateField.Active, out value);
					}
					break;
				case MuiListStateRecordKind.ColumnVisibility:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListColumnVisibilityStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Magic, out value);
						case MuiListStateField.Low:
							return MuiListColumnVisibilityStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Low, out value);
						case MuiListStateField.High:
							return MuiListColumnVisibilityStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.High, out value);
						case MuiListStateField.Word2:
							return MuiListColumnVisibilityStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word2, out value);
						case MuiListStateField.Word3:
							return MuiListColumnVisibilityStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word3, out value);
						case MuiListStateField.Word4:
							return MuiListColumnVisibilityStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word4, out value);
						case MuiListStateField.Word5:
							return MuiListColumnVisibilityStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word5, out value);
						case MuiListStateField.Word6:
							return MuiListColumnVisibilityStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word6, out value);
						case MuiListStateField.Word7:
							return MuiListColumnVisibilityStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word7, out value);
					}
					break;
				case MuiListStateRecordKind.ColumnOrder:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListColumnOrderStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListColumnOrderStateField.Magic, out value);
						case MuiListStateField.Count:
							return MuiListColumnOrderStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListColumnOrderStateField.Count, out value);
						case MuiListStateField.Values:
							return MuiListColumnOrderStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListColumnOrderStateField.Values, out value);
						case MuiListStateField.Reserved:
							return MuiListColumnOrderStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListColumnOrderStateField.Reserved, out value);
					}
					break;
				case MuiListStateRecordKind.Viewport:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListViewportStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListViewportStateField.Magic, out value);
						case MuiListStateField.TopPixel:
							return MuiListViewportStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListViewportStateField.TopPixel, out value);
						case MuiListStateField.VisiblePixel:
							return MuiListViewportStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListViewportStateField.VisiblePixel, out value);
						case MuiListStateField.TotalPixel:
							return MuiListViewportStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListViewportStateField.TotalPixel, out value);
						case MuiListStateField.First:
							return MuiListViewportStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListViewportStateField.First, out value);
						case MuiListStateField.LineHeight:
							return MuiListViewportStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListViewportStateField.LineHeight, out value);
						case MuiListStateField.Visible:
							return MuiListViewportStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListViewportStateField.Visible, out value);
						case MuiListStateField.DropMark:
							return MuiListViewportStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListViewportStateField.DropMark, out value);
					}
					break;
				case MuiListStateRecordKind.InteractionPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListInteractionPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListInteractionPolicyStateField.Magic, out value);
						case MuiListStateField.Input:
							return MuiListInteractionPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListInteractionPolicyStateField.Input, out value);
						case MuiListStateField.MultiSelect:
							return MuiListInteractionPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListInteractionPolicyStateField.MultiSelect, out value);
						case MuiListStateField.ScrollerPos:
							return MuiListInteractionPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListInteractionPolicyStateField.ScrollerPos, out value);
					}
					break;
				case MuiListStateRecordKind.ClickState:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListClickStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListClickStateField.Magic, out value);
						case MuiListStateField.ClickColumn:
							return MuiListClickStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListClickStateField.ClickColumn, out value);
						case MuiListStateField.DoubleClick:
							return MuiListClickStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListClickStateField.DoubleClick, out value);
						case MuiListStateField.AgainClick:
							return MuiListClickStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListClickStateField.AgainClick, out value);
						case MuiListStateField.Clicks:
							return MuiListClickStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListClickStateField.Clicks, out value);
						case MuiListStateField.DefClickColumn:
							return MuiListClickStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListClickStateField.DefClickColumn, out value);
					}
					break;
				case MuiListStateRecordKind.HookPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListHookPolicyStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListHookPolicyStateField.Magic, out value);
						case MuiListStateField.ConstructHook:
							return MuiListHookPolicyStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListHookPolicyStateField.ConstructHook, out value);
						case MuiListStateField.DestructHook:
							return MuiListHookPolicyStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListHookPolicyStateField.DestructHook, out value);
						case MuiListStateField.DisplayHook:
							return MuiListHookPolicyStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListHookPolicyStateField.DisplayHook, out value);
						case MuiListStateField.CompareHook:
							return MuiListHookPolicyStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListHookPolicyStateField.CompareHook, out value);
						case MuiListStateField.MultiTestHook:
							return MuiListHookPolicyStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListHookPolicyStateField.MultiTestHook, out value);
					}
					break;
				case MuiListStateRecordKind.SortState:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListSortStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListSortStateField.Magic, out value);
						case MuiListStateField.SortColumn:
							return MuiListSortStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListSortStateField.SortColumn, out value);
						case MuiListStateField.TitleClick:
							return MuiListSortStateMemoryCodec.TryReadUInt32(ref platform,
								address, MuiListSortStateField.TitleClick, out value);
					}
					break;
				case MuiListStateRecordKind.PresentationPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.Magic, out value);
						case MuiListStateField.Editable:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.Editable, out value);
						case MuiListStateField.Quiet:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.Quiet, out value);
						case MuiListStateField.AdjustHeight:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.AdjustHeight, out value);
						case MuiListStateField.AdjustWidth:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.AdjustWidth, out value);
						case MuiListStateField.Stripes:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.Stripes, out value);
						case MuiListStateField.ShowDropMarks:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.ShowDropMarks, out value);
						case MuiListStateField.DragSortable:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.DragSortable, out value);
						case MuiListStateField.DragType:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.DragType, out value);
						case MuiListStateField.AutoVisible:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.AutoVisible, out value);
						case MuiListStateField.AutoLineHeight:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.AutoLineHeight, out value);
						case MuiListStateField.MinLineHeight:
							return MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.MinLineHeight, out value);
					}
					break;
			}
			return false;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListStateRecordKind record,
			MuiListStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			switch (record)
			{
				case MuiListStateRecordKind.InsertPosition:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListInsertPositionStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListInsertPositionStateField.Magic, value);
						case MuiListStateField.Position:
							return MuiListInsertPositionStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListInsertPositionStateField.Position, value);
					}
					break;
				case MuiListStateRecordKind.TitleArray:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListTitleArrayStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListTitleArrayStateField.Magic, value);
						case MuiListStateField.Pointers:
							return MuiListTitleArrayStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListTitleArrayStateField.Pointers, value);
						case MuiListStateField.Count:
							return MuiListTitleArrayStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListTitleArrayStateField.Count, value);
					}
					break;
				case MuiListStateRecordKind.TitleValue:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListTitleStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListTitleStateField.Magic, value);
						case MuiListStateField.TitleValue:
							return MuiListTitleStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListTitleStateField.Value, value);
					}
					break;
				case MuiListStateRecordKind.SelectionSignal:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListSelectionSignalStateMemoryCodec.TryWriteUInt32(
								ref platform, address, MuiListSelectionSignalStateField.Magic,
								value);
						case MuiListStateField.SelectionValue:
							return MuiListSelectionSignalStateMemoryCodec.TryWriteUInt32(
								ref platform, address, MuiListSelectionSignalStateField.Value,
								value);
					}
					break;
				case MuiListStateRecordKind.FormatPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListFormatPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListFormatPolicyStateField.Magic, value);
						case MuiListStateField.FormatValue:
							return MuiListFormatPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListFormatPolicyStateField.Format, value);
						case MuiListStateField.MaxColumnsValue:
							return MuiListFormatPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListFormatPolicyStateField.MaxColumns, value);
						case MuiListStateField.FormatColumnsValue:
							return MuiListFormatPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListFormatPolicyStateField.Columns, value);
					}
					break;
				case MuiListStateRecordKind.FontPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListFontStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListFontStateField.Magic, value);
						case MuiListStateField.FontValue:
							return MuiListFontStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListFontStateField.Font, value);
					}
					break;
				case MuiListStateRecordKind.Redraw:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListRedrawStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListRedrawStateField.Magic, value);
						case MuiListStateField.Dirty:
							return MuiListRedrawStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListRedrawStateField.Dirty, value);
						case MuiListStateField.Requests:
							return MuiListRedrawStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListRedrawStateField.Requests, value);
					}
					break;
				case MuiListStateRecordKind.ActiveCursor:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListActiveStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListActiveStateField.Magic, value);
						case MuiListStateField.HasActive:
							return MuiListActiveStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListActiveStateField.HasActive, value);
						case MuiListStateField.Active:
							return MuiListActiveStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListActiveStateField.Active, value);
					}
					break;
				case MuiListStateRecordKind.ColumnVisibility:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListColumnVisibilityStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Magic, value);
						case MuiListStateField.Low:
							return MuiListColumnVisibilityStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Low, value);
						case MuiListStateField.High:
							return MuiListColumnVisibilityStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.High, value);
						case MuiListStateField.Word2:
							return MuiListColumnVisibilityStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word2, value);
						case MuiListStateField.Word3:
							return MuiListColumnVisibilityStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word3, value);
						case MuiListStateField.Word4:
							return MuiListColumnVisibilityStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word4, value);
						case MuiListStateField.Word5:
							return MuiListColumnVisibilityStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word5, value);
						case MuiListStateField.Word6:
							return MuiListColumnVisibilityStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word6, value);
						case MuiListStateField.Word7:
							return MuiListColumnVisibilityStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListColumnVisibilityStateField.Word7, value);
					}
					break;
				case MuiListStateRecordKind.ColumnOrder:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListColumnOrderStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListColumnOrderStateField.Magic, value);
						case MuiListStateField.Count:
							return MuiListColumnOrderStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListColumnOrderStateField.Count, value);
						case MuiListStateField.Values:
							return MuiListColumnOrderStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListColumnOrderStateField.Values, value);
						case MuiListStateField.Reserved:
							return MuiListColumnOrderStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListColumnOrderStateField.Reserved, value);
					}
					break;
				case MuiListStateRecordKind.Viewport:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListViewportStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListViewportStateField.Magic, value);
						case MuiListStateField.TopPixel:
							return MuiListViewportStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListViewportStateField.TopPixel, value);
						case MuiListStateField.VisiblePixel:
							return MuiListViewportStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListViewportStateField.VisiblePixel, value);
						case MuiListStateField.TotalPixel:
							return MuiListViewportStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListViewportStateField.TotalPixel, value);
						case MuiListStateField.First:
							return MuiListViewportStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListViewportStateField.First, value);
						case MuiListStateField.LineHeight:
							return MuiListViewportStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListViewportStateField.LineHeight, value);
						case MuiListStateField.Visible:
							return MuiListViewportStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListViewportStateField.Visible, value);
						case MuiListStateField.DropMark:
							return MuiListViewportStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListViewportStateField.DropMark, value);
					}
					break;
				case MuiListStateRecordKind.InteractionPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListInteractionPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListInteractionPolicyStateField.Magic, value);
						case MuiListStateField.Input:
							return MuiListInteractionPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListInteractionPolicyStateField.Input, value);
						case MuiListStateField.MultiSelect:
							return MuiListInteractionPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListInteractionPolicyStateField.MultiSelect, value);
						case MuiListStateField.ScrollerPos:
							return MuiListInteractionPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListInteractionPolicyStateField.ScrollerPos, value);
					}
					break;
				case MuiListStateRecordKind.ClickState:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListClickStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListClickStateField.Magic, value);
						case MuiListStateField.ClickColumn:
							return MuiListClickStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListClickStateField.ClickColumn, value);
						case MuiListStateField.DoubleClick:
							return MuiListClickStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListClickStateField.DoubleClick, value);
						case MuiListStateField.AgainClick:
							return MuiListClickStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListClickStateField.AgainClick, value);
						case MuiListStateField.Clicks:
							return MuiListClickStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListClickStateField.Clicks, value);
						case MuiListStateField.DefClickColumn:
							return MuiListClickStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListClickStateField.DefClickColumn, value);
					}
					break;
				case MuiListStateRecordKind.HookPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListHookPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListHookPolicyStateField.Magic, value);
						case MuiListStateField.ConstructHook:
							return MuiListHookPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListHookPolicyStateField.ConstructHook, value);
						case MuiListStateField.DestructHook:
							return MuiListHookPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListHookPolicyStateField.DestructHook, value);
						case MuiListStateField.DisplayHook:
							return MuiListHookPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListHookPolicyStateField.DisplayHook, value);
						case MuiListStateField.CompareHook:
							return MuiListHookPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListHookPolicyStateField.CompareHook, value);
						case MuiListStateField.MultiTestHook:
							return MuiListHookPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListHookPolicyStateField.MultiTestHook, value);
					}
					break;
				case MuiListStateRecordKind.SortState:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListSortStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListSortStateField.Magic, value);
						case MuiListStateField.SortColumn:
							return MuiListSortStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListSortStateField.SortColumn, value);
						case MuiListStateField.TitleClick:
							return MuiListSortStateMemoryCodec.TryWriteUInt32(ref platform,
								address, MuiListSortStateField.TitleClick, value);
					}
					break;
				case MuiListStateRecordKind.PresentationPolicy:
					switch (field)
					{
						case MuiListStateField.Magic:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.Magic, value);
						case MuiListStateField.Editable:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.Editable, value);
						case MuiListStateField.Quiet:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.Quiet, value);
						case MuiListStateField.AdjustHeight:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.AdjustHeight, value);
						case MuiListStateField.AdjustWidth:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.AdjustWidth, value);
						case MuiListStateField.Stripes:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.Stripes, value);
						case MuiListStateField.ShowDropMarks:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.ShowDropMarks, value);
						case MuiListStateField.DragSortable:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.DragSortable, value);
						case MuiListStateField.DragType:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.DragType, value);
						case MuiListStateField.AutoVisible:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.AutoVisible, value);
						case MuiListStateField.AutoLineHeight:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.AutoLineHeight, value);
						case MuiListStateField.MinLineHeight:
							return MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(
								ref platform, address,
								MuiListPresentationPolicyStateField.MinLineHeight, value);
					}
					break;
			}
			return false;
		}
	}

	// Compatibility wrapper for callers that still package the address, record,
	// and field as a cursor. New List code uses MuiListStateMemoryCodec directly
	// so the guest record is explicit at every access boundary.
	internal static class MuiListStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
			cursor.Record, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListStateFieldCursor cursor, out APTR address, out uint fieldSize)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Record, cursor.Field, out address, out fieldSize);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListStateRecordKind record,
			MuiListStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListStateMemoryCodec.TryReadUInt32(ref platform, address, record,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListStateRecordKind record,
			MuiListStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListStateMemoryCodec.TryWriteUInt32(ref platform, address, record,
				field, value);
	}

	// Explicit MUIA_List_HideColumn/ShowColumn state is retained in one
	// guest-resident record.  The fixed eight-word mask matches the bounded
	// 256-column geometry contract and is combined with FORMAT minimum-width
	// hiding at layout time.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnVisibilityState
	{
		public const uint Size = 36;
		public const uint Cookie = 0x434F4C56u; // 'COLV'
		public const uint FieldSize = 4;
		public const uint MagicOffset = 0;
		public const uint LowOffset = 4;
		public const uint HighOffset = 8;
		public const uint Word2Offset = 12;
		public const uint Word3Offset = 16;
		public const uint Word4Offset = 20;
		public const uint Word5Offset = 24;
		public const uint Word6Offset = 28;
		public const uint Word7Offset = 32;
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

	internal enum MuiListColumnVisibilityStateField : byte
	{
		Magic,
		Low,
		High,
		Word2,
		Word3,
		Word4,
		Word5,
		Word6,
		Word7,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnVisibilityStateFieldCursor
	{
		internal APTR Address;
		internal MuiListColumnVisibilityStateField Field;
	}

	internal static class MuiListColumnVisibilityStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(
			MuiListColumnVisibilityStateField field, out uint index)
		{
			index = field switch
			{
				MuiListColumnVisibilityStateField.Magic => 0,
				MuiListColumnVisibilityStateField.Low => 1,
				MuiListColumnVisibilityStateField.High => 2,
				MuiListColumnVisibilityStateField.Word2 => 3,
				MuiListColumnVisibilityStateField.Word3 => 4,
				MuiListColumnVisibilityStateField.Word4 => 5,
				MuiListColumnVisibilityStateField.Word5 => 6,
				MuiListColumnVisibilityStateField.Word6 => 7,
				MuiListColumnVisibilityStateField.Word7 => 8,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnVisibilityStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnVisibilityStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListColumnVisibilityState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListColumnVisibilityState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListColumnVisibilityState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnVisibilityStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListColumnVisibilityStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListColumnVisibilityStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListColumnVisibilityStateField.Low:
					value = state.Low;
					return true;
				case MuiListColumnVisibilityStateField.High:
					value = state.High;
					return true;
				case MuiListColumnVisibilityStateField.Word2:
					value = state.Word2;
					return true;
				case MuiListColumnVisibilityStateField.Word3:
					value = state.Word3;
					return true;
				case MuiListColumnVisibilityStateField.Word4:
					value = state.Word4;
					return true;
				case MuiListColumnVisibilityStateField.Word5:
					value = state.Word5;
					return true;
				case MuiListColumnVisibilityStateField.Word6:
					value = state.Word6;
					return true;
				case MuiListColumnVisibilityStateField.Word7:
					value = state.Word7;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnVisibilityStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnVisibilityStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListColumnVisibilityStateField.Magic:
					state.Magic = value;
					break;
				case MuiListColumnVisibilityStateField.Low:
					state.Low = value;
					break;
				case MuiListColumnVisibilityStateField.High:
					state.High = value;
					break;
				case MuiListColumnVisibilityStateField.Word2:
					state.Word2 = value;
					break;
				case MuiListColumnVisibilityStateField.Word3:
					state.Word3 = value;
					break;
				case MuiListColumnVisibilityStateField.Word4:
					state.Word4 = value;
					break;
				case MuiListColumnVisibilityStateField.Word5:
					state.Word5 = value;
					break;
				case MuiListColumnVisibilityStateField.Word6:
					state.Word6 = value;
					break;
				case MuiListColumnVisibilityStateField.Word7:
					state.Word7 = value;
					break;
				default:
					return false;
			}
			return MuiListColumnVisibilityStateCodec.WriteRecord(ref platform,
				address, state);
		}
	}

	internal static class MuiListColumnVisibilityStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnVisibilityStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListColumnVisibilityStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnVisibilityStateFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnVisibilityStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnVisibilityStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnVisibilityStateMemoryCodec.TryReadUInt32(ref platform,
				address, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnVisibilityStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnVisibilityStateMemoryCodec.TryWriteUInt32(ref platform,
				address, field, value);
	}

	internal static class MuiListColumnVisibilityStateCodec
	{
		// Visibility is a fixed nine-ULONG mask record. Keep all mask words in
		// declaration order so callers exchange a named struct instead of a chain
		// of private offsets; the mask's column semantics remain unchanged.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnVisibilityState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListColumnVisibilityState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Low) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.High) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Word2) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Word3) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Word4) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Word5) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Word6) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Word7)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListColumnVisibilityState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListColumnVisibilityState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Low) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.High) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word2) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word3) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word4) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word5) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word6) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Word7) &&
			MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListColumnVisibilityState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListColumnVisibilityState.Size) || value.Magic !=
				MuiListColumnVisibilityState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnVisibilityState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListColumnVisibilityState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListColumnVisibilityState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnVisibilityState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	// MUIA_List_ColumnOrder is a caller-facing BYTE* permutation. Keep the
	// copied byte payload behind one named guest record so the List never relies
	// on a caller buffer remaining live and never hides state in descriptor
	// offsets. The payload is ABI bytes by definition; all ownership metadata is
	// typed here.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnOrderState
	{
		public const uint Size = 16;
		public const uint Cookie = 0x434F5244u; // 'CORD'
		public const uint FieldSize = 4;
		public const uint MagicOffset = 0;
		public const uint CountOffset = 4;
		public const uint ValuesOffset = 8;
		public const uint ReservedOffset = 12;
		public uint Magic;
		public uint Count;
		public APTR Values;
		public uint Reserved;
	}

	internal enum MuiListColumnOrderStateField : byte
	{
		Magic,
		Count,
		Values,
		Reserved,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnOrderStateFieldCursor
	{
		internal APTR Address;
		internal MuiListColumnOrderStateField Field;
	}

	internal static class MuiListColumnOrderStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(
			MuiListColumnOrderStateField field, out uint index)
		{
			index = field switch
			{
				MuiListColumnOrderStateField.Magic => 0,
				MuiListColumnOrderStateField.Count => 1,
				MuiListColumnOrderStateField.Values => 2,
				MuiListColumnOrderStateField.Reserved => 3,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnOrderStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnOrderStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListColumnOrderState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListColumnOrderState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListColumnOrderState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnOrderStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListColumnOrderStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListColumnOrderStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListColumnOrderStateField.Count:
					value = state.Count;
					return true;
				case MuiListColumnOrderStateField.Values:
					value = state.Values.Raw;
					return true;
				case MuiListColumnOrderStateField.Reserved:
					value = state.Reserved;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnOrderStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnOrderStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListColumnOrderStateField.Magic:
					state.Magic = value;
					break;
				case MuiListColumnOrderStateField.Count:
					state.Count = value;
					break;
				case MuiListColumnOrderStateField.Values:
					state.Values = APTR.FromPointer(value);
					break;
				case MuiListColumnOrderStateField.Reserved:
					state.Reserved = value;
					break;
				default:
					return false;
			}
			return MuiListColumnOrderStateCodec.WriteRecord(ref platform, address,
				state);
		}
	}

	internal static class MuiListColumnOrderStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnOrderStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnOrderStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnOrderStateFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnOrderStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnOrderStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnOrderStateMemoryCodec.TryReadUInt32(ref platform, address,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnOrderStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnOrderStateMemoryCodec.TryWriteUInt32(ref platform, address,
				field, value);
	}

	internal static class MuiListColumnOrderStateCodec
	{
		// Column-order ownership is a fixed four-ULONG record. Keep structural
		// access in declaration order; the byte-permutation vector remains behind
		// its separate bounded adapter and is not folded into this record.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnOrderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListColumnOrderState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Count) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var values) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Reserved)) return false;
			value.Values = APTR.FromPointer(values);
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListColumnOrderState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListColumnOrderState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Count) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Values.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Reserved) &&
			MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListColumnOrderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListColumnOrderState.Size) || value.Magic !=
				MuiListColumnOrderState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnOrderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnOrderState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryReadStructural(ref platform, block, out value) ||
				value.Count == 0 || value.Count > MaximumGeometryColumns ||
				value.Values.IsNull || value.Reserved != ColumnOrderValueBytes(value.Count) ||
				!platform.IsMapped(value.Values, value.Reserved))
			{
				value = default;
				return false;
			}
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListColumnOrderState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStorage(ref platform, block, out value) &&
			value.Magic == MuiListColumnOrderState.Cookie;
	}

	// ColumnOrder is a caller-facing BYTE* permutation. Keep each byte in a
	// named record so source parsing and guest-owned comparison/lookup do not
	// reconstruct an anonymous byte offset independently.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnOrderByteRecord
	{
		internal const uint Size = 1;
		internal const uint FieldSize = 1;
		internal const uint ValueOffset = 0;
		internal byte Value;
	}

	internal enum MuiListColumnOrderByteField : byte
	{
		Value,
	}

	// Keep the byte-vector index explicit so malformed caller memory fails
	// before any guest read or write. The cursor is retained as a compatibility
	// wrapper; the struct-first vector adapter below owns the address boundary.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnOrderByteCursor
	{
		internal const uint EntrySize = MuiListColumnOrderByteRecord.Size;
		internal const uint MaximumEntries = MaximumColumns;
		internal APTR Base;
		internal uint Index;
	}

	// Struct-first guest-memory adapter for the caller-owned BYTE* permutation.
	// Every indexed entry is admitted as a complete named byte record and the
	// MorphOS 256-column bound is enforced here.
	internal static class MuiListColumnOrderByteVectorMemoryCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (vector.IsNull || index >= MuiListColumnOrderByteCursor.MaximumEntries ||
				index > (uint.MaxValue - vector.Raw) /
				MuiListColumnOrderByteRecord.Size) return false;
			var offset = index * MuiListColumnOrderByteRecord.Size;
			if (vector.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(vector.Raw + offset);
			return platform.IsMapped(address, MuiListColumnOrderByteRecord.Size);
		}
	}

	// Production bridge for the caller-owned ColumnOrder permutation. The
	// bounded adapter owns byte-vector address arithmetic; ordering consumers
	// exchange only the named one-byte record/value.
	internal static class MuiListColumnOrderByteVectorCodec
	{
		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			MuiListColumnOrderByteCursor cursor, out byte value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListColumnOrderByteCursorCodec.TryGetEntry(ref platform,
				cursor, out var address)) return false;
			return MuiListColumnOrderByteRecordMemoryCodec.TryReadByte(ref platform,
				address, MuiListColumnOrderByteField.Value, out value);
		}

		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out byte value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListColumnOrderByteVectorMemoryCodec.TryGetEntry(
				ref platform, vector, index, out var address)) return false;
			return MuiListColumnOrderByteRecordMemoryCodec.TryReadByte(ref platform,
				address, MuiListColumnOrderByteField.Value, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out MuiListColumnOrderByteRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!TryReadValue(ref platform, vector, index, out var rawValue))
				return false;
			value.Value = rawValue;
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			MuiListColumnOrderByteCursor cursor,
			out MuiListColumnOrderByteRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!TryReadValue(ref platform, cursor, out var rawValue))
				return false;
			value.Value = rawValue;
			return true;
		}

		internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, byte value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnOrderByteVectorMemoryCodec.TryGetEntry(
				ref platform, vector, index, out var address)) return false;
			return MuiListColumnOrderByteRecordMemoryCodec.TryWriteByte(ref platform,
				address, MuiListColumnOrderByteField.Value, value);
		}

		internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
			MuiListColumnOrderByteCursor cursor, byte value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnOrderByteCursorCodec.TryGetEntry(ref platform,
				cursor, out var address)) return false;
			return MuiListColumnOrderByteRecordMemoryCodec.TryWriteByte(ref platform,
				address, MuiListColumnOrderByteField.Value, value);
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, MuiListColumnOrderByteRecord value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryWriteValue(ref platform, vector, index, value.Value);

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			MuiListColumnOrderByteCursor cursor,
			MuiListColumnOrderByteRecord value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryWriteValue(ref platform, cursor, value.Value);
	}

	// Keep the one-byte record's field boundary named as well. Vector index
	// arithmetic belongs to the vector adapter above; this codec owns the
	// complete byte record so consumers never reach into an anonymous offset.
	internal static class MuiListColumnOrderByteRecordMemoryCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnOrderByteField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (field != MuiListColumnOrderByteField.Value || record.IsNull ||
				!platform.IsMapped(record, MuiListColumnOrderByteRecord.Size)) return false;
			if (record.Raw > uint.MaxValue - MuiListColumnOrderByteRecord.ValueOffset)
				return false;
			address = APTR.FromPointer(record.Raw +
				MuiListColumnOrderByteRecord.ValueOffset);
			return platform.IsMapped(address, MuiListColumnOrderByteRecord.FieldSize);
		}

	internal static bool TryReadByte<TPlatform>(ref TPlatform platform,
		APTR record, MuiListColumnOrderByteField field, out byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		return MuiGuestUbyteStorageCodec.TryReadValue(ref platform, address,
			out value);
	}

	internal static bool TryWriteByte<TPlatform>(ref TPlatform platform,
		APTR record, MuiListColumnOrderByteField field, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryGetAddress(ref platform, record, field, out var address))
			return false;
		return MuiGuestUbyteStorageCodec.WriteValue(ref platform, address, value);
	}
}

	internal static class MuiListColumnOrderByteCodec
	{
		// ColumnOrder entries are one-byte named records. The vector adapter owns
		// index arithmetic; this sequential codec owns the complete byte record.
	internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListColumnOrderByteRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		return MuiGuestUbyteStorageCodec.TryReadValue(ref platform, address,
			out value.Value);
	}

	internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
		APTR address, MuiListColumnOrderByteRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiGuestUbyteStorageCodec.WriteValue(ref platform, address, value.Value);

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListColumnOrderByteRecord value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryReadRecord(ref platform, address, out value);

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnOrderByteRecord value)
			where TPlatform : struct, IMuiGuestMemory
			=> WriteRecord(ref platform, address, value);
	}

	internal static class MuiListColumnOrderByteCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListColumnOrderByteCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiListColumnOrderByteVectorMemoryCodec.TryGetEntry(ref platform,
				cursor.Base, cursor.Index, out address);
	}

	// Parsed FORMAT columns are guest-resident records.  Keep the semantic
	// fields named here and confine the 68k big-endian wire layout to the
	// Read/WriteFormatDescriptor codecs below.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFormatDescriptor
	{
		internal const uint Size = 40;
		internal const uint FieldSize = 4;
		internal const uint DeltaOffset = 0;
		internal const uint WeightOffset = 4;
		internal const uint MinWidthOffset = 8;
		internal const uint MaxWidthOffset = 12;
		internal const uint ColumnOffset = 16;
		internal const uint FlagsOffset = 20;
		internal const uint PreparseOffset = 24;
		internal const uint PreparseLengthOffset = 28;
		internal const uint PreparseStorageOffset = 32;
		internal const uint PreparseStorageLengthOffset = 36;
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

	// Struct-first guest-memory adapter for the fixed FORMAT descriptor table.
	// Complete 40-byte records and the MorphOS 256-column bound are enforced at
	// this boundary; parser callers keep only a typed cursor as a compatibility
	// wrapper.
	internal static class MuiListFormatDescriptorVectorMemoryCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (vector.IsNull || index >= MuiListFormatDescriptorCursor.MaximumEntries ||
				index > (uint.MaxValue - vector.Raw) /
				MuiListFormatDescriptor.Size) return false;
			var offset = index * MuiListFormatDescriptor.Size;
			if (vector.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(vector.Raw + offset);
			return platform.IsMapped(address, MuiListFormatDescriptor.Size);
		}
	}

	// Production bridge for the fixed FORMAT descriptor table. The bounded
	// adapter owns slot arithmetic and complete-record admission; format code
	// exchanges only the named 40-byte descriptor.
	internal static class MuiListFormatDescriptorVectorCodec
	{
		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			MuiListFormatDescriptorCursor cursor,
			out MuiListFormatDescriptor value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiListFormatDescriptorCursorCodec.TryGetEntry(ref platform,
				cursor, out var address) ||
				!MuiListFormatDescriptorCodec.TryRead(ref platform, address,
					out value))
			{
				value = default;
				return false;
			}
			return true;
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			MuiListFormatDescriptorCursor cursor,
			MuiListFormatDescriptor value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListFormatDescriptorCursorCodec.TryGetEntry(ref platform,
				cursor, out var address)) return false;
			return MuiListFormatDescriptorCodec.TryWrite(ref platform, address,
				value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out MuiListFormatDescriptor value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiListFormatDescriptorVectorMemoryCodec.TryGetEntry(
				ref platform, vector, index, out var address) ||
				!MuiListFormatDescriptorCodec.TryRead(ref platform, address,
					out value))
			{
				value = default;
				return false;
			}
			return true;
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, MuiListFormatDescriptor value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListFormatDescriptorVectorMemoryCodec.TryGetEntry(
				ref platform, vector, index, out var address)) return false;
			return MuiListFormatDescriptorCodec.TryWrite(ref platform, address,
				value);
		}
	}

	internal static class MuiListFormatDescriptorCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListFormatDescriptorCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiListFormatDescriptorVectorMemoryCodec.TryGetEntry(ref platform,
				cursor.Base, cursor.Index, out address);
	}

	internal enum MuiListFormatDescriptorField : byte
	{
		Delta,
		Weight,
		MinWidth,
		MaxWidth,
		Column,
		Flags,
		Preparse,
		PreparseLength,
		PreparseStorage,
		PreparseStorageLength,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFormatDescriptorFieldCursor
	{
		internal APTR Address;
		internal MuiListFormatDescriptorField Field;
	}

	internal static class MuiListFormatDescriptorMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListFormatDescriptorField field,
			out uint index)
		{
			index = field switch
			{
				MuiListFormatDescriptorField.Delta => 0,
				MuiListFormatDescriptorField.Weight => 1,
				MuiListFormatDescriptorField.MinWidth => 2,
				MuiListFormatDescriptorField.MaxWidth => 3,
				MuiListFormatDescriptorField.Column => 4,
				MuiListFormatDescriptorField.Flags => 5,
				MuiListFormatDescriptorField.Preparse => 6,
				MuiListFormatDescriptorField.PreparseLength => 7,
				MuiListFormatDescriptorField.PreparseStorage => 8,
				MuiListFormatDescriptorField.PreparseStorageLength => 9,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListFormatDescriptorField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListFormatDescriptorField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListFormatDescriptor.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListFormatDescriptor.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListFormatDescriptor.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatDescriptorField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListFormatDescriptorCodec.TryReadRecord(ref platform, address,
				out var descriptor)) return false;
			switch (field)
			{
				case MuiListFormatDescriptorField.Delta:
					value = descriptor.Delta;
					return true;
				case MuiListFormatDescriptorField.Weight:
					value = descriptor.Weight;
					return true;
				case MuiListFormatDescriptorField.MinWidth:
					value = descriptor.MinWidth;
					return true;
				case MuiListFormatDescriptorField.MaxWidth:
					value = descriptor.MaxWidth;
					return true;
				case MuiListFormatDescriptorField.Column:
					value = descriptor.Column;
					return true;
				case MuiListFormatDescriptorField.Flags:
					value = descriptor.Flags;
					return true;
				case MuiListFormatDescriptorField.Preparse:
					value = descriptor.Preparse.Raw;
					return true;
				case MuiListFormatDescriptorField.PreparseLength:
					value = descriptor.PreparseLength;
					return true;
				case MuiListFormatDescriptorField.PreparseStorage:
					value = descriptor.PreparseStorage.Raw;
					return true;
				case MuiListFormatDescriptorField.PreparseStorageLength:
					value = descriptor.PreparseStorageLength;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatDescriptorField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListFormatDescriptorCodec.TryReadRecord(ref platform, address,
				out var descriptor)) return false;
			switch (field)
			{
				case MuiListFormatDescriptorField.Delta:
					descriptor.Delta = value;
					break;
				case MuiListFormatDescriptorField.Weight:
					descriptor.Weight = value;
					break;
				case MuiListFormatDescriptorField.MinWidth:
					descriptor.MinWidth = value;
					break;
				case MuiListFormatDescriptorField.MaxWidth:
					descriptor.MaxWidth = value;
					break;
				case MuiListFormatDescriptorField.Column:
					descriptor.Column = value;
					break;
				case MuiListFormatDescriptorField.Flags:
					descriptor.Flags = value;
					break;
				case MuiListFormatDescriptorField.Preparse:
					descriptor.Preparse = APTR.FromPointer(value);
					break;
				case MuiListFormatDescriptorField.PreparseLength:
					descriptor.PreparseLength = value;
					break;
				case MuiListFormatDescriptorField.PreparseStorage:
					descriptor.PreparseStorage = APTR.FromPointer(value);
					break;
				case MuiListFormatDescriptorField.PreparseStorageLength:
					descriptor.PreparseStorageLength = value;
					break;
				default:
					return false;
			}
			return MuiListFormatDescriptorCodec.WriteRecord(ref platform, address,
				descriptor);
		}
	}

	internal static class MuiListFormatDescriptorFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListFormatDescriptorFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatDescriptorMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListFormatDescriptorFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatDescriptorMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatDescriptorField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatDescriptorMemoryCodec.TryReadUInt32(ref platform, address,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatDescriptorField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatDescriptorMemoryCodec.TryWriteUInt32(ref platform, address,
				field, value);
	}

	internal static class MuiListFormatDescriptorCodec
	{
		// FORMAT descriptors are fixed ten-ULONG records. Keep the declaration
		// order in one sequential codec so parsing and publication use the named
		// descriptor fields rather than reconstructing offsets at each call site.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListFormatDescriptor value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiListFormatDescriptor.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Delta) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Weight) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.MinWidth) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.MaxWidth) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Column) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Flags) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var preparse) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.PreparseLength) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var preparseStorage) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.PreparseStorageLength)) return false;
			value.Preparse = APTR.FromPointer(preparse);
			value.PreparseStorage = APTR.FromPointer(preparseStorage);
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatDescriptor value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiListFormatDescriptor.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Delta) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Weight) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MinWidth) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MaxWidth) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Column) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Flags) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Preparse.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PreparseLength) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PreparseStorage.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PreparseStorageLength) &&
			MuiGuestStructCursor.IsComplete(cursor);

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListFormatDescriptor value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryReadRecord(ref platform, address, out value);

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatDescriptor value)
			where TPlatform : struct, IMuiGuestMemory
			=> WriteRecord(ref platform, address, value);
	}

	// A FORMAT descriptor table owns a contiguous vector of named descriptor
	// records. Keep the vector pointer and bounded count together so consumers
	// cannot accidentally pair a stale scalar alias with an unrelated table.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFormatDescriptorState
	{
		internal const uint Size = 12;
		internal const uint Cookie = 0x46445452u; // 'FDTR'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint ColumnsOffset = 4;
		internal const uint ValuesOffset = 8;
		internal uint Magic;
		internal uint Columns;
		internal APTR Values;
	}

	internal enum MuiListFormatDescriptorStateField : byte
	{
		Magic,
		Columns,
		Values,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFormatDescriptorStateFieldCursor
	{
		internal APTR Address;
		internal MuiListFormatDescriptorStateField Field;
	}

	internal static class MuiListFormatDescriptorStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(
			MuiListFormatDescriptorStateField field, out uint index)
		{
			index = field switch
			{
				MuiListFormatDescriptorStateField.Magic => 0,
				MuiListFormatDescriptorStateField.Columns => 1,
				MuiListFormatDescriptorStateField.Values => 2,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListFormatDescriptorStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListFormatDescriptorStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListFormatDescriptorState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListFormatDescriptorState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListFormatDescriptorState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatDescriptorStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListFormatDescriptorStateCodec.TryReadRecord(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListFormatDescriptorStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListFormatDescriptorStateField.Columns:
					value = state.Columns;
					return true;
				case MuiListFormatDescriptorStateField.Values:
					value = state.Values.Raw;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatDescriptorStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListFormatDescriptorStateCodec.TryReadRecord(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListFormatDescriptorStateField.Magic:
					state.Magic = value;
					break;
				case MuiListFormatDescriptorStateField.Columns:
					state.Columns = value;
					break;
				case MuiListFormatDescriptorStateField.Values:
					state.Values = APTR.FromPointer(value);
					break;
				default:
					return false;
			}
			return MuiListFormatDescriptorStateCodec.WriteRecord(ref platform,
				address, state);
		}
	}

	internal static class MuiListFormatDescriptorStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListFormatDescriptorStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListFormatDescriptorStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListFormatDescriptorStateFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatDescriptorStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatDescriptorStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatDescriptorStateMemoryCodec.TryReadUInt32(ref platform,
				address, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatDescriptorStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatDescriptorStateMemoryCodec.TryWriteUInt32(ref platform,
				address, field, value);
	}

	internal static class MuiListFormatDescriptorStateCodec
	{
		// Descriptor-table state is a fixed three-ULONG record. Keep the vector
		// pointer as raw APTR bits in declaration order; table-size validation
		// remains the admission layer around this structural codec.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListFormatDescriptorState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListFormatDescriptorState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Columns) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var values)) return false;
			value.Values = APTR.FromPointer(values);
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListFormatDescriptorState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListFormatDescriptorState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Columns) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Values.Raw) &&
			MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListFormatDescriptorState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListFormatDescriptorState.Size) || value.Magic !=
				MuiListFormatDescriptorState.Cookie || value.Columns == 0 ||
				value.Columns > MuiListFormatDescriptorCursor.MaximumEntries ||
				value.Values.IsNull || !platform.IsMapped(value.Values,
					value.Columns * MuiListFormatDescriptor.Size)) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListFormatDescriptorState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListFormatDescriptorState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryReadStructural(ref platform, block, out value) ||
				value.Magic != MuiListFormatDescriptorState.Cookie ||
				value.Columns == 0 || value.Columns >
				MuiListFormatDescriptorCursor.MaximumEntries || value.Values.IsNull ||
				!platform.IsMapped(value.Values,
					value.Columns * MuiListFormatDescriptor.Size))
			{
				value = default;
				return false;
			}
			return true;
		}

		// Teardown may reclaim a bounded descriptor vector even when runtime
		// admission rejected its cookie. The count remains bounded and the vector
		// is still checked for mapping by the owning cleanup helper.
			internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
				APTR block, out MuiListFormatDescriptorState value)
				where TPlatform : struct, IMuiGuestMemory
			{
				if (!TryReadStructural(ref platform, block, out value) ||
					value.Columns == 0 || value.Columns >
					MuiListFormatDescriptorCursor.MaximumEntries)
				{
					value = default;
					return false;
				}
				return true;
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
		internal const uint Cookie = 0x434D4554u; // 'CMET'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint WidthOffset = 4;
		internal const uint ColumnsOffset = 8;
		internal const uint ValuesOffset = 12;
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

	internal static class MuiListColumnMetricsMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListColumnMetricsField field,
			out uint index)
		{
			index = field switch
			{
				MuiListColumnMetricsField.Magic => 0,
				MuiListColumnMetricsField.Width => 1,
				MuiListColumnMetricsField.Columns => 2,
				MuiListColumnMetricsField.Values => 3,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnMetricsField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnMetricsField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListColumnMetricsState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListColumnMetricsState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListColumnMetricsState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnMetricsField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListColumnMetricsStateCodec.TryReadRecord(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListColumnMetricsField.Magic:
					value = state.Magic;
					return true;
				case MuiListColumnMetricsField.Width:
					value = state.Width;
					return true;
				case MuiListColumnMetricsField.Columns:
					value = state.Columns;
					return true;
				case MuiListColumnMetricsField.Values:
					value = state.Values.Raw;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnMetricsField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnMetricsStateCodec.TryReadRecord(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListColumnMetricsField.Magic:
					state.Magic = value;
					break;
				case MuiListColumnMetricsField.Width:
					state.Width = value;
					break;
				case MuiListColumnMetricsField.Columns:
					state.Columns = value;
					break;
				case MuiListColumnMetricsField.Values:
					state.Values = APTR.FromPointer(value);
					break;
				default:
					return false;
			}
			return MuiListColumnMetricsStateCodec.WriteRecord(ref platform,
				address, state);
		}
	}

	internal static class MuiListColumnMetricsFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricsFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListColumnMetricsMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricsFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnMetricsMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnMetricsField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnMetricsMemoryCodec.TryReadUInt32(ref platform,
				address, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnMetricsField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnMetricsMemoryCodec.TryWriteUInt32(ref platform,
				address, field, value);
	}

	internal static class MuiListColumnMetricsStateCodec
	{
		// The metrics owner is a fixed four-ULONG record. Keep its fields in
		// declaration order so the production path carries a named value rather
		// than rebuilding the record from private guest offsets.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnMetricsState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListColumnMetricsState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Width) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Columns) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var values)) return false;
			value.Values = APTR.FromPointer(values);
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListColumnMetricsState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListColumnMetricsState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Width) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Columns) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Values.Raw) &&
			MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR block, MuiListColumnMetricsState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListColumnMetricsState.Size) ||
				value.Magic != MuiListColumnMetricsState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnMetricsState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListColumnMetricsState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryReadStructural(ref platform, block, out value) ||
				value.Magic != MuiListColumnMetricsState.Cookie || value.Columns == 0 ||
				value.Columns > MaximumGeometryColumns || value.Values.IsNull ||
				!platform.IsMapped(value.Values,
					value.Columns * MuiListColumnMetricValue.Size))
			{
				value = default;
				return false;
			}
			return true;
		}

		// Teardown must be able to retire the owned Values vector even when a
		// caller or memory fault has damaged only the record cookie. This reader
		// validates the bounded value-type fields and guest allocation separately
		// from the magic used for runtime admission.
			internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
				APTR block, out MuiListColumnMetricsState value)
				where TPlatform : struct, IMuiGuestMemory
			{
				if (!TryReadStructural(ref platform, block, out value) ||
					value.Columns == 0 || value.Columns > MaximumGeometryColumns ||
					value.Values.IsNull || !platform.IsMapped(value.Values,
						value.Columns * MuiListColumnMetricValue.Size))
				{
					value = default;
					return false;
				}
				return true;
			}
	}

	// Each Values entry is a guest ULONG containing the measured width for one
	// derived column. Keep the array element named so metric consumers do not
	// reach into an anonymous four-byte slot.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListColumnMetricValue
	{
		internal const uint Size = 4;
		internal const uint FieldSize = 4;
		internal const uint ValueOffset = 0;
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

	internal static class MuiListColumnMetricMemoryCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnMetricField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (field != MuiListColumnMetricField.Value || record.IsNull ||
				!platform.IsMapped(record,
					MuiListColumnMetricValue.Size)) return false;
			if (record.Raw > uint.MaxValue - MuiListColumnMetricValue.ValueOffset)
				return false;
			address = APTR.FromPointer(record.Raw +
				MuiListColumnMetricValue.ValueOffset);
			return platform.IsMapped(address, MuiListColumnMetricValue.FieldSize);
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnMetricField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (field != MuiListColumnMetricField.Value ||
				!MuiListColumnMetricCodec.TryReadValue(ref platform, record,
					out value)) return false;
			return true;
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnMetricField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (field != MuiListColumnMetricField.Value) return false;
			return MuiListColumnMetricCodec.WriteValue(ref platform, record, value);
		}
	}

	internal static class MuiListColumnMetricFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnMetricMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnMetricField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnMetricMemoryCodec.TryReadUInt32(ref platform,
				record, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListColumnMetricField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListColumnMetricMemoryCodec.TryWriteUInt32(ref platform,
				record, field, value);
	}

	internal static class MuiListColumnMetricCodec
	{
		// A measured width is a single ULONG record. Keep its structural codec
		// sequential so metric consumers exchange a named value rather than an
		// implicit field offset.
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			APTR address, out uint value)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
				out value);

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool WriteValue<TPlatform>(ref TPlatform platform,
			APTR address, uint value)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);

		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListColumnMetricValue value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			uint rawValue;
			if (!TryReadValue(ref platform, address, out rawValue))
			{
				value = default;
				return false;
			}
			value.Value = rawValue;
			return true;
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnMetricValue value)
			where TPlatform : struct, IMuiGuestMemory =>
			WriteValue(ref platform, address, value.Value);

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListColumnMetricValue value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryReadRecord(ref platform, address, out value);

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListColumnMetricValue value)
			where TPlatform : struct, IMuiGuestMemory
			=> WriteRecord(ref platform, address, value);
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

	// Struct-first guest-memory adapter for the measured-column ULONG vector.
	// Complete metric records and the 256-column bound are enforced here; the
	// typed cursor remains a compatibility wrapper for older callers.
	internal static class MuiListColumnMetricVectorMemoryCodec
	{
		// Keep these wire constants local to the adapter. They describe the
		// fixed guest layout and avoid making the freestanding compiler infer
		// record size through a generic nested-constant expression.
		private const uint EntrySize = 4;
		private const uint MaximumEntries = 256;

		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (vector.IsNull || index >= MaximumEntries ||
				index > (uint.MaxValue - vector.Raw) /
				EntrySize) return false;
			var offset = index * EntrySize;
			if (vector.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(vector.Raw + offset);
			// Normalize the platform predicate at the ABI boundary. Some native
			// backends may leave a non-zero truth value from their comparison
			// chain; callers must receive a canonical C-like bool before applying
			// negation or composing another admission check.
			return platform.IsMapped(address, EntrySize) ? true : false;
		}
	}

	// Production bridge for the measured-column vector. The bounded adapter
	// owns slot arithmetic and complete-record admission; content measurement
	// exchanges only the named metric value. Scalar helpers stay inside this
	// record boundary because the freestanding compiler has a known lowering
	// defect for one-ULONG structs passed by value.
	internal static class MuiListColumnMetricVectorCodec
	{
		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricCursor cursor, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListColumnMetricCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			return MuiListColumnMetricCodec.TryReadValue(ref platform, address,
				out value);
		}

		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListColumnMetricVectorMemoryCodec.TryGetEntry(
				ref platform, vector, index, out var address)) return false;
			return MuiListColumnMetricCodec.TryReadValue(ref platform, address,
				out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out MuiListColumnMetricValue value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!TryReadValue(ref platform, vector, index, out var rawValue))
			{
				value = default;
				return false;
			}
			value.Value = rawValue;
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricCursor cursor,
			out MuiListColumnMetricValue value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!TryReadValue(ref platform, cursor, out var rawValue))
				return false;
			value.Value = rawValue;
			return true;
		}

		internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnMetricVectorMemoryCodec.TryGetEntry(
				ref platform, vector, index, out var address)) return false;
			return MuiListColumnMetricCodec.WriteValue(ref platform, address,
				value);
		}

		internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricCursor cursor, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListColumnMetricCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			return MuiListColumnMetricCodec.WriteValue(ref platform, address,
				value);
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, MuiListColumnMetricValue value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryWriteValue(ref platform, vector, index, value.Value);

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricCursor cursor,
			MuiListColumnMetricValue value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryWriteValue(ref platform, cursor, value.Value);
	}

	internal static class MuiListColumnMetricCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListColumnMetricCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiListColumnMetricVectorMemoryCodec.TryGetEntry(ref platform,
				cursor.Base, cursor.Index, out address);
	}

	// MUIA_List_TitleArray owns a private pointer table, but not the strings it
	// references. Keep the ownership and count in one named guest record so the
	// List core never has to infer them from scattered pointer arithmetic.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListTitleArrayState
	{
		public const uint Size = 12;
		public const uint Cookie = 0x5449544Cu; // 'TITL'
		public const uint FieldSize = 4;
		public const uint MagicOffset = 0;
		public const uint PointersOffset = 4;
		public const uint CountOffset = 8;
		public uint Magic;
		public APTR Pointers;
		public uint Count;
	}

	internal enum MuiListTitleArrayStateField : byte
	{
		Magic,
		Pointers,
		Count,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListTitleArrayStateFieldCursor
	{
		internal APTR Address;
		internal MuiListTitleArrayStateField Field;
	}

	internal static class MuiListTitleArrayStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(
			MuiListTitleArrayStateField field, out uint index)
		{
			index = field switch
			{
				MuiListTitleArrayStateField.Magic => 0,
				MuiListTitleArrayStateField.Pointers => 1,
				MuiListTitleArrayStateField.Count => 2,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListTitleArrayStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			return TryGetAddress(ref platform, record, field, out address, out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListTitleArrayStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListTitleArrayState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListTitleArrayState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListTitleArrayState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListTitleArrayStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListTitleArrayStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListTitleArrayStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListTitleArrayStateField.Pointers:
					value = state.Pointers.Raw;
					return true;
				case MuiListTitleArrayStateField.Count:
					value = state.Count;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListTitleArrayStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListTitleArrayStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListTitleArrayStateField.Magic:
					state.Magic = value;
					break;
				case MuiListTitleArrayStateField.Pointers:
					state.Pointers = APTR.FromPointer(value);
					break;
				case MuiListTitleArrayStateField.Count:
					state.Count = value;
					break;
				default:
					return false;
			}
			return MuiListTitleArrayStateCodec.WriteRecord(ref platform, address,
				state);
		}
	}

	internal static class MuiListTitleArrayStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListTitleArrayStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListTitleArrayStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListTitleArrayStateFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListTitleArrayStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListTitleArrayStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListTitleArrayStateMemoryCodec.TryReadUInt32(ref platform, address,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListTitleArrayStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListTitleArrayStateMemoryCodec.TryWriteUInt32(ref platform, address,
				field, value);
	}

	internal static class MuiListTitleArrayStateCodec
	{
		// The title-array metadata is a fixed three-ULONG record. Keep its
		// declaration order in one sequential codec so production callers do not
		// reconstruct Magic/Pointers/Count from field offsets.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListTitleArrayState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListTitleArrayState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var pointers) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Count)) return false;
			value.Pointers = APTR.FromPointer(pointers);
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListTitleArrayState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListTitleArrayState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
					value.Magic) ||
				!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
					value.Pointers.Raw) ||
				!MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
					value.Count)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListTitleArrayState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListTitleArrayState.Size) || value.Magic !=
				MuiListTitleArrayState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListTitleArrayState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListTitleArrayState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!TryReadStructural(ref platform, block, out value) ||
				value.Count > MaximumColumns || value.Pointers.IsNull ||
				!platform.IsMapped(value.Pointers,
					(value.Count + 1) * MuiListPointerSlotRecord.Size))
			{
				value = default;
				return false;
			}
			return true;
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListTitleArrayState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStorage(ref platform, block, out value) &&
			value.Magic == MuiListTitleArrayState.Cookie;
	}

	// MUIA_List_Title is either a caller-owned STRPTR or TRUE for the
	// display-hook title form. Keep that scalar projection in a named record so
	// title-row geometry and drawing do not repeatedly decode an anonymous word.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListTitleState
	{
		internal const uint Size = 8;
		internal const uint Cookie = 0x4C544954u; // 'LTIT'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint ValueOffset = 4;

		internal uint Magic;
		internal uint Value;
	}

	internal enum MuiListTitleStateField : byte
	{
		Magic,
		Value,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListTitleStateFieldCursor
	{
		internal APTR Address;
		internal MuiListTitleStateField Field;
	}

	internal static class MuiListTitleStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListTitleStateField field,
			out uint index)
		{
			index = field switch
			{
				MuiListTitleStateField.Magic => 0,
				MuiListTitleStateField.Value => 1,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListTitleStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			return TryGetAddress(ref platform, record, field, out address, out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListTitleStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListTitleState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListTitleState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListTitleState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListTitleStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListTitleStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListTitleStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListTitleStateField.Value:
					value = state.Value;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListTitleStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListTitleStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListTitleStateField.Magic:
					state.Magic = value;
					break;
				case MuiListTitleStateField.Value:
					state.Value = value;
					break;
				default:
					return false;
			}
			return MuiListTitleStateCodec.WriteRecord(ref platform, address, state);
		}
	}

	internal static class MuiListTitleStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListTitleStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListTitleStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListTitleStateFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListTitleStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListTitleStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListTitleStateMemoryCodec.TryReadUInt32(ref platform, address, field,
				out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListTitleStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListTitleStateMemoryCodec.TryWriteUInt32(ref platform, address, field,
				value);
	}

	internal static class MuiListTitleStateCodec
	{
		// Title metadata is a fixed two-ULONG record. Keep the declaration order
		// in one sequential codec; the field adapter remains only for legacy
		// cursor and malformed-state diagnostics.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListTitleState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			return MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListTitleState.Size, out var cursor) &&
				MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) &&
				MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Value) &&
				MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListTitleState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListTitleState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value) &&
			MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListTitleState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block, MuiListTitleState.Size) ||
				value.Magic != MuiListTitleState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListTitleState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListTitleState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListTitleState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListTitleState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	// MUIA_List_SelectChange is a getter-only edge signal. Keep its toggled
	// value in a named record so List selection mutations and Listview forwarding
	// do not rely on a raw attribute word as their synchronization source.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListSelectionSignalState
	{
		internal const uint Size = 8;
		internal const uint Cookie = 0x4C534947u; // 'LSIG'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint ValueOffset = 4;

		internal uint Magic;
		internal uint Value;
	}

	internal enum MuiListSelectionSignalStateField : byte
	{
		Magic,
		Value,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListSelectionSignalStateFieldCursor
	{
		internal APTR Address;
		internal MuiListSelectionSignalStateField Field;
	}

	internal static class MuiListSelectionSignalStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(
			MuiListSelectionSignalStateField field, out uint index)
		{
			index = field switch
			{
				MuiListSelectionSignalStateField.Magic => 0,
				MuiListSelectionSignalStateField.Value => 1,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListSelectionSignalStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			return TryGetAddress(ref platform, record, field, out address, out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListSelectionSignalStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListSelectionSignalState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListSelectionSignalState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListSelectionSignalState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListSelectionSignalStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListSelectionSignalStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListSelectionSignalStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListSelectionSignalStateField.Value:
					value = state.Value;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListSelectionSignalStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListSelectionSignalStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListSelectionSignalStateField.Magic:
					state.Magic = value;
					break;
				case MuiListSelectionSignalStateField.Value:
					state.Value = value;
					break;
				default:
					return false;
			}
			return MuiListSelectionSignalStateCodec.WriteRecord(ref platform,
				address, state);
		}
	}

	internal static class MuiListSelectionSignalStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListSelectionSignalStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListSelectionSignalStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListSelectionSignalStateFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListSelectionSignalStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListSelectionSignalStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListSelectionSignalStateMemoryCodec.TryReadUInt32(ref platform,
				address, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListSelectionSignalStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListSelectionSignalStateMemoryCodec.TryWriteUInt32(ref platform,
				address, field, value);
	}

	internal static class MuiListSelectionSignalStateCodec
	{
		// SelectChange state is a fixed two-ULONG record. Keep raw structural
		// reads lossless; strict admission continues to validate the cookie and
		// the canonical BOOL separately.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			return MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListSelectionSignalState.Size, out var cursor) &&
				MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) &&
				MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Value) &&
				MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListSelectionSignalState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Value) &&
			MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListSelectionSignalState.Size) || value.Magic !=
				MuiListSelectionSignalState.Cookie) return false;
			value.Value = value.Value == 0 ? 0u : 1u;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListSelectionSignalState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListSelectionSignalState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	// FORMAT is caller-owned text; MaxColumns is normalized construction/runtime
	// policy; and Columns is the installed descriptor count. Keep these related
	// projections together without taking ownership of the caller's string.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFormatPolicyState
	{
		internal const uint Size = 16;
		internal const uint Cookie = 0x4C464D54u; // 'LFMT'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint FormatOffset = 4;
		internal const uint MaxColumnsOffset = 8;
		internal const uint ColumnsOffset = 12;

		internal uint Magic;
		internal APTR Format;
		internal uint MaxColumns;
		internal uint Columns;
	}

	internal enum MuiListFormatPolicyStateField : byte
	{
		Magic,
		Format,
		MaxColumns,
		Columns,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFormatPolicyStateFieldCursor
	{
		internal APTR Address;
		internal MuiListFormatPolicyStateField Field;
	}

	internal static class MuiListFormatPolicyStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(
			MuiListFormatPolicyStateField field, out uint index)
		{
			index = field switch
			{
				MuiListFormatPolicyStateField.Magic => 0,
				MuiListFormatPolicyStateField.Format => 1,
				MuiListFormatPolicyStateField.MaxColumns => 2,
				MuiListFormatPolicyStateField.Columns => 3,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListFormatPolicyStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			return TryGetAddress(ref platform, record, field, out address, out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListFormatPolicyStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListFormatPolicyState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListFormatPolicyState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListFormatPolicyState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatPolicyStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListFormatPolicyStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListFormatPolicyStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListFormatPolicyStateField.Format:
					value = state.Format.Raw;
					return true;
				case MuiListFormatPolicyStateField.MaxColumns:
					value = state.MaxColumns;
					return true;
				case MuiListFormatPolicyStateField.Columns:
					value = state.Columns;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatPolicyStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListFormatPolicyStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListFormatPolicyStateField.Magic:
					state.Magic = value;
					break;
				case MuiListFormatPolicyStateField.Format:
					state.Format = APTR.FromPointer(value);
					break;
				case MuiListFormatPolicyStateField.MaxColumns:
					state.MaxColumns = value;
					break;
				case MuiListFormatPolicyStateField.Columns:
					state.Columns = value;
					break;
				default:
					return false;
			}
			return MuiListFormatPolicyStateCodec.WriteRecord(ref platform, address,
				state);
		}
	}

	internal static class MuiListFormatPolicyStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListFormatPolicyStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatPolicyStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListFormatPolicyStateFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatPolicyStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatPolicyStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatPolicyStateMemoryCodec.TryReadUInt32(ref platform, address,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFormatPolicyStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFormatPolicyStateMemoryCodec.TryWriteUInt32(ref platform, address,
				field, value);
	}

	internal static class MuiListFormatPolicyStateCodec
	{
		// FORMAT policy is a fixed four-ULONG record. Preserve the nullable
		// caller-owned string pointer as raw APTR bits while keeping all field
		// order and bounds handling in the sequential struct codec.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListFormatPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListFormatPolicyState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var format) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.MaxColumns) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Columns)) return false;
			value.Format = APTR.FromPointer(format);
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListFormatPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListFormatPolicyState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Format.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MaxColumns) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Columns) &&
			MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListFormatPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListFormatPolicyState.Size) || value.Magic !=
				MuiListFormatPolicyState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListFormatPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListFormatPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListFormatPolicyState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListFormatPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	// MUIA_Font is a caller-owned TextFont pointer inherited by List. Keep the
	// pointer in a named record so display, measurement, and runtime updates
	// share one typed projection without taking ownership of the font object.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFontState
	{
		internal const uint Size = 8;
		internal const uint Cookie = 0x4C464E54u; // 'LFNT'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint FontOffset = 4;

		internal uint Magic;
		internal APTR Font;
	}

	internal enum MuiListFontStateField : byte
	{
		Magic,
		Font,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListFontStateFieldCursor
	{
		internal APTR Address;
		internal MuiListFontStateField Field;
	}

	internal static class MuiListFontStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListFontStateField field,
			out uint index)
		{
			switch (field)
			{
				case MuiListFontStateField.Magic:
					index = 0;
					return true;
				case MuiListFontStateField.Font:
					index = 1;
					return true;
			}
			index = uint.MaxValue;
			return false;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListFontStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListFontStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListFontState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListFontState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListFontState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFontStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListFontStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListFontStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListFontStateField.Font:
					value = state.Font.Raw;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFontStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListFontStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListFontStateField.Magic:
					state.Magic = value;
					break;
				case MuiListFontStateField.Font:
					state.Font = APTR.FromPointer(value);
					break;
				default:
					return false;
			}
			return MuiListFontStateCodec.WriteRecord(ref platform, address, state);
		}
	}

	internal static class MuiListFontStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListFontStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFontStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListFontStateFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFontStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFontStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFontStateMemoryCodec.TryReadUInt32(ref platform, address, field,
				out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListFontStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListFontStateMemoryCodec.TryWriteUInt32(ref platform, address, field,
				value);
	}

	internal static class MuiListFontStateCodec
	{
		// Font policy is a fixed two-ULONG record. Preserve the borrowed TextFont
		// APTR as raw guest bits while keeping declaration order in the bounded
		// sequential struct codec.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListFontState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListFontState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var font)) return false;
			value.Font = APTR.FromPointer(font);
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListFontState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListFontState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Font.Raw) &&
			MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListFontState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block, MuiListFontState.Size) ||
				value.Magic != MuiListFontState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListFontState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListFontState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListFontState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListFontState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	// TitleArray is an inline guest pointer table. Keep each four-byte slot as a
	// named record so ownership and display-copy paths never read anonymous
	// words outside this codec boundary.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListPointerSlotRecord
	{
		internal const uint Size = 4;
		internal const uint FieldSize = 4;
		internal const uint ValueOffset = 0;
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

	internal static class MuiListPointerSlotMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListPointerSlotField field,
			out uint index)
		{
			index = field == MuiListPointerSlotField.Value ? 0u : uint.MaxValue;
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPointerSlotField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPointerSlotField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListPointerSlotRecord.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListPointerSlotRecord.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListPointerSlotRecord.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPointerSlotField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (field != MuiListPointerSlotField.Value) return false;
			return MuiListPointerSlotCodec.TryReadValue(ref platform, record,
				out value);
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPointerSlotField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (field != MuiListPointerSlotField.Value) return false;
			return MuiListPointerSlotCodec.WriteValue(ref platform, record, value);
		}
	}

	internal static class MuiListPointerSlotFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListPointerSlotFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPointerSlotMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListPointerSlotFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPointerSlotMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPointerSlotField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPointerSlotMemoryCodec.TryReadUInt32(ref platform,
				record, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPointerSlotField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPointerSlotMemoryCodec.TryWriteUInt32(ref platform,
				record, field, value);
	}

	internal static class MuiListPointerSlotCodec
	{
		// A pointer-table entry is one ULONG on the guest wire. Keep the named
		// APTR record as the semantic value and serialize it through the shared
		// packed ULONG storage codec after the bounded slot is admitted.
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			APTR address, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			return MuiGuestUlongStorageCodec.TryReadValue(ref platform, address,
				out value);
		}

		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListPointerSlotRecord record)
			where TPlatform : struct, IMuiGuestMemory
		{
			record = default;
			if (!TryReadValue(ref platform, address, out var value)) return false;
			record.Value = APTR.FromPointer(value);
			return true;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool WriteValue<TPlatform>(ref TPlatform platform,
			APTR address, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return MuiGuestUlongStorageCodec.WriteValue(ref platform, address,
				value);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPointerSlotRecord record)
			where TPlatform : struct, IMuiGuestMemory =>
			WriteValue(ref platform, address, record.Value.Raw);

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListPointerSlotRecord record)
			where TPlatform : struct, IMuiGuestMemory
			=> TryReadRecord(ref platform, address, out record);

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPointerSlotRecord record)
			where TPlatform : struct, IMuiGuestMemory
			=> WriteRecord(ref platform, address, record);
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
		if (!MuiListPointerSlotVectorMemoryCodec.TryGetEntry(ref platform,
			cursor.Base, cursor.Index,
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

	// Struct-first guest-memory adapter for internal pointer-slot vectors used by
	// display, title, and string-array projections. NULL termination remains a
	// consumer rule; this boundary only admits complete 4-byte records within
	// the bounded table.
	internal static class MuiListPointerSlotVectorMemoryCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			if (vector.IsNull || index >= MuiListPointerSlotCursor.MaximumEntries ||
				index > (uint.MaxValue - vector.Raw) /
				MuiListPointerSlotRecord.Size) return false;
			var offset = index * MuiListPointerSlotRecord.Size;
			if (vector.Raw > uint.MaxValue - offset) return false;
			address = APTR.FromPointer(vector.Raw + offset);
			return platform.IsMapped(address, MuiListPointerSlotRecord.Size);
		}
	}

	// Production bridge for internal display/title/string-array vectors. The
	// bounded adapter owns slot address arithmetic; callers exchange only the
	// named pointer-slot record (or its scalar APTR projection).
	internal static class MuiListPointerSlotVectorCodec
	{
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			MuiListPointerSlotCursor cursor, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			return MuiListPointerSlotCodec.TryReadValue(ref platform, address,
				out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			MuiListPointerSlotCursor cursor, out MuiListPointerSlotRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!TryReadValue(ref platform, cursor, out var rawValue)) return false;
			value.Value = APTR.FromPointer(rawValue);
			return true;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
			MuiListPointerSlotCursor cursor, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListPointerSlotCursorCodec.TryGetEntry(ref platform, cursor,
				out var address)) return false;
			return MuiListPointerSlotCodec.WriteValue(ref platform, address, value);
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			MuiListPointerSlotCursor cursor, MuiListPointerSlotRecord value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryWriteValue(ref platform, cursor, value.Value.Raw);

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListPointerSlotVectorMemoryCodec.TryGetEntry(ref platform,
				vector, index, out var address)) return false;
			return MuiListPointerSlotCodec.TryReadValue(ref platform, address,
				out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, out MuiListPointerSlotRecord value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!TryReadValue(ref platform, vector, index, out var rawValue))
				return false;
			value.Value = APTR.FromPointer(rawValue);
			return true;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryWriteValue<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListPointerSlotVectorMemoryCodec.TryGetEntry(ref platform,
				vector, index, out var address)) return false;
			return MuiListPointerSlotCodec.WriteValue(ref platform, address,
				value);
		}

		internal static bool TryWrite<TPlatform>(ref TPlatform platform,
			APTR vector, uint index, MuiListPointerSlotRecord value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryWriteValue(ref platform, vector, index, value.Value.Raw);
	}

	internal static class MuiListPointerSlotCursorCodec
	{
		internal static bool TryGetEntry<TPlatform>(ref TPlatform platform,
			MuiListPointerSlotCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
			=> MuiListPointerSlotVectorMemoryCodec.TryGetEntry(ref platform,
				cursor.Base, cursor.Index, out address);
	}

	// Caller-owned records placed in a List are self-describing: the first
	// ULONG is the total guest allocation size. Keep that header named so
	// disposal validates the record through a codec instead of reaching into
	// an anonymous offset.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListOwnedRecordHeader
	{
		internal const uint Size = 4;
		internal const uint FieldSize = 4;
		internal const uint LengthOffset = 0;
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

	internal static class MuiListOwnedRecordHeaderMemoryCodec
	{
		private static bool TryResolveFieldIndex(
			MuiListOwnedRecordHeaderField field, out uint index)
		{
			index = field == MuiListOwnedRecordHeaderField.Length ? 0u :
				uint.MaxValue;
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListOwnedRecordHeaderField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListOwnedRecordHeaderField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListOwnedRecordHeader.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListOwnedRecordHeader.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListOwnedRecordHeader.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListOwnedRecordHeaderField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (field != MuiListOwnedRecordHeaderField.Length) return false;
			return MuiListOwnedRecordHeaderCodec.TryReadValue(ref platform, record,
				out value);
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListOwnedRecordHeaderField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (field != MuiListOwnedRecordHeaderField.Length) return false;
			return MuiListOwnedRecordHeaderCodec.WriteValue(ref platform, record,
				value);
		}
	}

	internal static class MuiListOwnedRecordHeaderFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListOwnedRecordHeaderFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListOwnedRecordHeaderMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListOwnedRecordHeaderFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListOwnedRecordHeaderMemoryCodec.TryGetAddress(ref platform,
				cursor.Record, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListOwnedRecordHeaderField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListOwnedRecordHeaderMemoryCodec.TryReadUInt32(ref platform,
				record, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR record, MuiListOwnedRecordHeaderField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListOwnedRecordHeaderMemoryCodec.TryWriteUInt32(ref platform,
				record, field, value);
	}

	internal static class MuiListOwnedRecordHeaderCodec
	{
		// The self-describing allocation header is one ULONG wide. Keep the
		// named-record entry points explicit, but route the wire transfer through
		// the no-inline ULONG storage codec because by-value one-field struct calls
		// are not yet reliable on native targets.
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool TryReadValue<TPlatform>(ref TPlatform platform,
			APTR address, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestUlongStorageCodec.TryReadValue(ref platform, address, out value);

		[MethodImpl(MethodImplOptions.NoInlining)]
		internal static bool WriteValue<TPlatform>(ref TPlatform platform,
			APTR address, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestUlongStorageCodec.WriteValue(ref platform, address, value);

		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListOwnedRecordHeader value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			return TryReadValue(ref platform, address, out value.Length);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR address, MuiListOwnedRecordHeader value)
			where TPlatform : struct, IMuiGuestMemory =>
			WriteValue(ref platform, address, value.Length);

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListOwnedRecordHeader value)
			where TPlatform : struct, IMuiGuestMemory
			=> TryReadRecord(ref platform, address, out value);

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListOwnedRecordHeader value)
			where TPlatform : struct, IMuiGuestMemory
			=> WriteRecord(ref platform, address, value);
	}

	// Quiet/redraw coalescing is state, not a property of the List index. Keep
	// it in one named guest record so mutation paths never need to grow the
	// packed entry header or infer fields from private offsets.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListRedrawState
	{
		public const uint Size = 12;
		public const uint Cookie = 0x52454452u; // 'REDR'
		public const uint FieldSize = 4;
		public const uint MagicOffset = 0;
		public const uint DirtyOffset = 4;
		public const uint RequestsOffset = 8;
		public uint Magic;
		public uint Dirty;
		public uint Requests;
	}

	internal enum MuiListRedrawStateField : byte
	{
		Magic,
		Dirty,
		Requests,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListRedrawStateFieldCursor
	{
		internal APTR Address;
		internal MuiListRedrawStateField Field;
	}

	internal static class MuiListRedrawStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListRedrawStateField field,
			out uint index)
		{
			index = field switch
			{
				MuiListRedrawStateField.Magic => 0,
				MuiListRedrawStateField.Dirty => 1,
				MuiListRedrawStateField.Requests => 2,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListRedrawStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListRedrawStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListRedrawState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListRedrawState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListRedrawState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListRedrawStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListRedrawStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListRedrawStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListRedrawStateField.Dirty:
					value = state.Dirty;
					return true;
				case MuiListRedrawStateField.Requests:
					value = state.Requests;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListRedrawStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListRedrawStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListRedrawStateField.Magic:
					state.Magic = value;
					break;
				case MuiListRedrawStateField.Dirty:
					state.Dirty = value;
					break;
				case MuiListRedrawStateField.Requests:
					state.Requests = value;
					break;
				default:
					return false;
			}
			return MuiListRedrawStateCodec.WriteRecord(ref platform, address, state);
		}
	}

	internal static class MuiListRedrawStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListRedrawStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListRedrawStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListRedrawStateFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListRedrawStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListRedrawStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListRedrawStateMemoryCodec.TryReadUInt32(ref platform, address, field,
				out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListRedrawStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListRedrawStateMemoryCodec.TryWriteUInt32(ref platform, address, field,
				value);
	}

	internal static class MuiListRedrawStateCodec
	{
		// Redraw coalescing is a fixed three-ULONG record. Keep the dirty bit and
		// request counter adjacent in declaration order so mutation paths exchange
		// one named state struct rather than unrelated scalar offsets.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListRedrawState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListRedrawState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Dirty) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Requests)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListRedrawState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListRedrawState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Dirty) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Requests) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListRedrawState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block, MuiListRedrawState.Size) ||
				value.Magic != MuiListRedrawState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListRedrawState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListRedrawState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListRedrawState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListRedrawState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	// The public MUIA_List_Active value is zero for an empty MorphOS 3.20 list,
	// so a named cursor record keeps both the selected row and presence bit. It
	// distinguishes that projection from a real row zero after insertion.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListActiveState
	{
		public const uint Size = 12;
		public const uint Cookie = 0x41435456u; // 'ACTV'
		public const uint FieldSize = 4;
		public const uint MagicOffset = 0;
		public const uint HasActiveOffset = 4;
		public const uint ActiveOffset = 8;

		public uint Magic;
		public uint HasActive;
		public uint Active;
	}

	internal enum MuiListActiveStateField : byte
	{
		Magic,
		HasActive,
		Active,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListActiveStateFieldCursor
	{
		internal APTR Address;
		internal MuiListActiveStateField Field;
	}

	internal static class MuiListActiveStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListActiveStateField field,
			out uint index)
		{
			index = field switch
			{
				MuiListActiveStateField.Magic => 0,
				MuiListActiveStateField.HasActive => 1,
				MuiListActiveStateField.Active => 2,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListActiveStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListActiveStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListActiveState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListActiveState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListActiveState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListActiveStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListActiveStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListActiveStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListActiveStateField.HasActive:
					value = state.HasActive;
					return true;
				case MuiListActiveStateField.Active:
					value = state.Active;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListActiveStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListActiveStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListActiveStateField.Magic:
					state.Magic = value;
					break;
				case MuiListActiveStateField.HasActive:
					state.HasActive = value;
					break;
				case MuiListActiveStateField.Active:
					state.Active = value;
					break;
				default:
					return false;
			}
			return MuiListActiveStateCodec.WriteRecord(ref platform, address, state);
		}
	}

	internal static class MuiListActiveStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListActiveStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListActiveStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListActiveStateFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListActiveStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListActiveStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListActiveStateMemoryCodec.TryReadUInt32(ref platform, address, field,
				out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListActiveStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListActiveStateMemoryCodec.TryWriteUInt32(ref platform, address, field,
				value);
	}

	internal static class MuiListActiveStateCodec
	{
		// Active state is a fixed three-ULONG record. Keep the presence bit and
		// selected row together in declaration order so empty-list semantics remain
		// explicit at the named struct boundary.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListActiveState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListActiveState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.HasActive) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Active)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListActiveState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListActiveState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.HasActive) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Active) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListActiveState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block, MuiListActiveState.Size) ||
				value.Magic != MuiListActiveState.Cookie) return false;
			value.HasActive = value.HasActive == 0 ? 0u : 1u;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListActiveState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListActiveState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListActiveState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListActiveState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	// MUIA_List_InsertPosition is a getter-only result published by the last
	// successful insertion. Keep its signed LONG projection in a named record so
	// public Get/OM_GET does not depend on an untyped scalar attribute slot.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListInsertPositionState
	{
		internal const uint Size = 8;
		internal const uint Cookie = 0x4C494E53u; // 'LINS'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint PositionOffset = 4;

		internal uint Magic;
		internal uint Position;
	}

	internal enum MuiListInsertPositionStateField : byte
	{
		Magic,
		Position,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListInsertPositionStateFieldCursor
	{
		internal APTR Address;
		internal MuiListInsertPositionStateField Field;
	}

	internal static class MuiListInsertPositionStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(
			MuiListInsertPositionStateField field, out uint index)
		{
			index = field switch
			{
				MuiListInsertPositionStateField.Magic => 0,
				MuiListInsertPositionStateField.Position => 1,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListInsertPositionStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListInsertPositionStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListInsertPositionState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListInsertPositionState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListInsertPositionState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListInsertPositionStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListInsertPositionStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListInsertPositionStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListInsertPositionStateField.Position:
					value = state.Position;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListInsertPositionStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListInsertPositionStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListInsertPositionStateField.Magic:
					state.Magic = value;
					break;
				case MuiListInsertPositionStateField.Position:
					state.Position = value;
					break;
				default:
					return false;
			}
			return MuiListInsertPositionStateCodec.WriteRecord(ref platform, address,
				state);
		}
	}

	internal static class MuiListInsertPositionStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListInsertPositionStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListInsertPositionStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListInsertPositionStateFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListInsertPositionStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListInsertPositionStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListInsertPositionStateMemoryCodec.TryReadUInt32(ref platform, address,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListInsertPositionStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListInsertPositionStateMemoryCodec.TryWriteUInt32(ref platform,
				address, field, value);
	}

	internal static class MuiListInsertPositionStateCodec
	{
		// Insert-position state is a fixed two-ULONG result record. Keep the
		// signed LONG projection beside its cookie in declaration order so the
		// getter-only publication path exchanges one named struct.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListInsertPositionState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListInsertPositionState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Position)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListInsertPositionState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListInsertPositionState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Position) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListInsertPositionState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListInsertPositionState.Size) || value.Magic !=
				MuiListInsertPositionState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListInsertPositionState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListInsertPositionState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListInsertPositionState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListInsertPositionState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	// The public pixel viewport attributes are derived from the same bounded
	// row geometry as Layout and hit-testing. Keep the values in one named
	// guest record so callers never depend on private attribute offsets.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListViewportState
	{
		public const uint Size = 32;
		public const uint Cookie = 0x56505754u; // 'VPWT'
		public const uint FieldSize = 4;
		public const uint MagicOffset = 0;
		public const uint TopPixelOffset = 4;
		public const uint VisiblePixelOffset = 8;
		public const uint TotalPixelOffset = 12;
		public const uint FirstOffset = 16;
		public const uint LineHeightOffset = 20;
		public const uint VisibleOffset = 24;
		public const uint DropMarkOffset = 28;
		public uint Magic;
		public uint TopPixel;
		public uint VisiblePixel;
		public uint TotalPixel;
		public uint First;
		public uint LineHeight;
		public uint Visible;
		public uint DropMark;
	}

	internal enum MuiListViewportStateField : byte
	{
		Magic,
		TopPixel,
		VisiblePixel,
		TotalPixel,
		First,
		LineHeight,
		Visible,
		DropMark,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListViewportStateFieldCursor
	{
		internal APTR Address;
		internal MuiListViewportStateField Field;
	}

	internal static class MuiListViewportStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListViewportStateField field,
			out uint index)
		{
			index = field switch
			{
				MuiListViewportStateField.Magic => 0,
				MuiListViewportStateField.TopPixel => 1,
				MuiListViewportStateField.VisiblePixel => 2,
				MuiListViewportStateField.TotalPixel => 3,
				MuiListViewportStateField.First => 4,
				MuiListViewportStateField.LineHeight => 5,
				MuiListViewportStateField.Visible => 6,
				MuiListViewportStateField.DropMark => 7,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListViewportStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListViewportStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListViewportState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListViewportState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListViewportState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListViewportStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListViewportStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListViewportStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListViewportStateField.TopPixel:
					value = state.TopPixel;
					return true;
				case MuiListViewportStateField.VisiblePixel:
					value = state.VisiblePixel;
					return true;
				case MuiListViewportStateField.TotalPixel:
					value = state.TotalPixel;
					return true;
				case MuiListViewportStateField.First:
					value = state.First;
					return true;
				case MuiListViewportStateField.LineHeight:
					value = state.LineHeight;
					return true;
				case MuiListViewportStateField.Visible:
					value = state.Visible;
					return true;
				case MuiListViewportStateField.DropMark:
					value = state.DropMark;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListViewportStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListViewportStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListViewportStateField.Magic:
					state.Magic = value;
					break;
				case MuiListViewportStateField.TopPixel:
					state.TopPixel = value;
					break;
				case MuiListViewportStateField.VisiblePixel:
					state.VisiblePixel = value;
					break;
				case MuiListViewportStateField.TotalPixel:
					state.TotalPixel = value;
					break;
				case MuiListViewportStateField.First:
					state.First = value;
					break;
				case MuiListViewportStateField.LineHeight:
					state.LineHeight = value;
					break;
				case MuiListViewportStateField.Visible:
					state.Visible = value;
					break;
				case MuiListViewportStateField.DropMark:
					state.DropMark = value;
					break;
				default:
					return false;
			}
			return MuiListViewportStateCodec.WriteRecord(ref platform, address, state);
		}
	}

	internal static class MuiListViewportStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListViewportStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListViewportStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListViewportStateFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListViewportStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListViewportStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListViewportStateMemoryCodec.TryReadUInt32(ref platform, address,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListViewportStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListViewportStateMemoryCodec.TryWriteUInt32(ref platform, address,
				field, value);
	}

	internal static class MuiListViewportStateCodec
	{
		// Viewport metrics are a fixed eight-ULONG record. Keep pixel, row, and
		// drop-mark fields in declaration order so layout and notification paths
		// exchange one named struct rather than rebuilding private offsets.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListViewportState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListViewportState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.TopPixel) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.VisiblePixel) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.TotalPixel) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.First) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.LineHeight) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Visible) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.DropMark)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListViewportState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListViewportState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TopPixel) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.VisiblePixel) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TotalPixel) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.First) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.LineHeight) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Visible) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DropMark) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListViewportState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListViewportState.Size) || value.Magic !=
				MuiListViewportState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListViewportState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListViewportState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListViewportState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListViewportState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
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
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint InputOffset = 4;
		internal const uint MultiSelectOffset = 8;
		internal const uint ScrollerPosOffset = 12;

		internal uint Magic;
		internal uint Input;
		internal uint MultiSelect;
		internal uint ScrollerPos;
	}

	internal enum MuiListInteractionPolicyStateField : byte
	{
		Magic,
		Input,
		MultiSelect,
		ScrollerPos,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListInteractionPolicyStateFieldCursor
	{
		internal APTR Address;
		internal MuiListInteractionPolicyStateField Field;
	}

	internal static class MuiListInteractionPolicyStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(
			MuiListInteractionPolicyStateField field, out uint index)
		{
			index = field switch
			{
				MuiListInteractionPolicyStateField.Magic => 0,
				MuiListInteractionPolicyStateField.Input => 1,
				MuiListInteractionPolicyStateField.MultiSelect => 2,
				MuiListInteractionPolicyStateField.ScrollerPos => 3,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListInteractionPolicyStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListInteractionPolicyStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListInteractionPolicyState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListInteractionPolicyState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListInteractionPolicyState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListInteractionPolicyStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListInteractionPolicyStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListInteractionPolicyStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListInteractionPolicyStateField.Input:
					value = state.Input;
					return true;
				case MuiListInteractionPolicyStateField.MultiSelect:
					value = state.MultiSelect;
					return true;
				case MuiListInteractionPolicyStateField.ScrollerPos:
					value = state.ScrollerPos;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListInteractionPolicyStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListInteractionPolicyStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListInteractionPolicyStateField.Magic:
					state.Magic = value;
					break;
				case MuiListInteractionPolicyStateField.Input:
					state.Input = value;
					break;
				case MuiListInteractionPolicyStateField.MultiSelect:
					state.MultiSelect = value;
					break;
				case MuiListInteractionPolicyStateField.ScrollerPos:
					state.ScrollerPos = value;
					break;
				default:
					return false;
			}
			return MuiListInteractionPolicyStateCodec.WriteRecord(ref platform, address,
				state);
		}
	}

	internal static class MuiListInteractionPolicyStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListInteractionPolicyStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListInteractionPolicyStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListInteractionPolicyStateFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListInteractionPolicyStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListInteractionPolicyStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListInteractionPolicyStateMemoryCodec.TryReadUInt32(ref platform,
				address, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListInteractionPolicyStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListInteractionPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
				address, field, value);
	}

	internal static class MuiListInteractionPolicyStateCodec
	{
		// Interaction policy is a fixed four-ULONG record. Keep the BOOL and enum
		// values in declaration order so input and scroller consumers exchange one
		// named struct instead of rebuilding private field offsets.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListInteractionPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListInteractionPolicyState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Input) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.MultiSelect) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.ScrollerPos)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListInteractionPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListInteractionPolicyState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Input) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MultiSelect) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ScrollerPos) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListInteractionPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListInteractionPolicyState.Size) || value.Magic !=
				MuiListInteractionPolicyState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListInteractionPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListInteractionPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListInteractionPolicyState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListInteractionPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
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
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint ClickColumnOffset = 4;
		internal const uint DoubleClickOffset = 8;
		internal const uint AgainClickOffset = 12;
		internal const uint ClicksOffset = 16;
		internal const uint DefClickColumnOffset = 20;

		internal uint Magic;
		internal uint ClickColumn;
		internal uint DoubleClick;
		internal uint AgainClick;
		internal uint Clicks;
		internal uint DefClickColumn;
	}

	internal enum MuiListClickStateField : byte
	{
		Magic,
		ClickColumn,
		DoubleClick,
		AgainClick,
		Clicks,
		DefClickColumn,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListClickStateFieldCursor
	{
		internal APTR Address;
		internal MuiListClickStateField Field;
	}

	internal static class MuiListClickStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListClickStateField field,
			out uint index)
		{
			index = field switch
			{
				MuiListClickStateField.Magic => 0,
				MuiListClickStateField.ClickColumn => 1,
				MuiListClickStateField.DoubleClick => 2,
				MuiListClickStateField.AgainClick => 3,
				MuiListClickStateField.Clicks => 4,
				MuiListClickStateField.DefClickColumn => 5,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListClickStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListClickStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListClickState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListClickState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListClickState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListClickStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListClickStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListClickStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListClickStateField.ClickColumn:
					value = state.ClickColumn;
					return true;
				case MuiListClickStateField.DoubleClick:
					value = state.DoubleClick;
					return true;
				case MuiListClickStateField.AgainClick:
					value = state.AgainClick;
					return true;
				case MuiListClickStateField.Clicks:
					value = state.Clicks;
					return true;
				case MuiListClickStateField.DefClickColumn:
					value = state.DefClickColumn;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListClickStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListClickStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListClickStateField.Magic:
					state.Magic = value;
					break;
				case MuiListClickStateField.ClickColumn:
					state.ClickColumn = value;
					break;
				case MuiListClickStateField.DoubleClick:
					state.DoubleClick = value;
					break;
				case MuiListClickStateField.AgainClick:
					state.AgainClick = value;
					break;
				case MuiListClickStateField.Clicks:
					state.Clicks = value;
					break;
				case MuiListClickStateField.DefClickColumn:
					state.DefClickColumn = value;
					break;
				default:
					return false;
			}
			return MuiListClickStateCodec.WriteRecord(ref platform, address, state);
		}
	}

	internal static class MuiListClickStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListClickStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListClickStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListClickStateFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListClickStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListClickStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListClickStateMemoryCodec.TryReadUInt32(ref platform, address, field,
				out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListClickStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListClickStateMemoryCodec.TryWriteUInt32(ref platform, address, field,
				value);
	}

	internal static class MuiListClickStateCodec
	{
		// Click state is a fixed six-ULONG record. Keep structural reads lossless;
		// strict publication canonicalizes the two BOOL fields before the named
		// record is exchanged with guest memory.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListClickState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.ClickColumn) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.DoubleClick) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.AgainClick) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Clicks) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.DefClickColumn)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListClickState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListClickState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ClickColumn) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DoubleClick) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.AgainClick) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Clicks) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DefClickColumn) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block, MuiListClickState.Size) ||
				value.Magic != MuiListClickState.Cookie) return false;
			value.DoubleClick = value.DoubleClick == 0 ? 0u : 1u;
			value.AgainClick = value.AgainClick == 0 ? 0u : 1u;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListClickState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListClickState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListClickState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListClickState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
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
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint ConstructHookOffset = 4;
		internal const uint DestructHookOffset = 8;
		internal const uint DisplayHookOffset = 12;
		internal const uint CompareHookOffset = 16;
		internal const uint MultiTestHookOffset = 20;

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
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint SortColumnOffset = 4;
		internal const uint TitleClickOffset = 8;

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
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint EditableOffset = 4;
		internal const uint QuietOffset = 8;
		internal const uint AdjustHeightOffset = 12;
		internal const uint AdjustWidthOffset = 16;
		internal const uint StripesOffset = 20;
		internal const uint ShowDropMarksOffset = 24;
		internal const uint DragSortableOffset = 28;
		internal const uint DragTypeOffset = 32;
		internal const uint AutoVisibleOffset = 36;
		internal const uint AutoLineHeightOffset = 40;
		internal const uint MinLineHeightOffset = 44;

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

	internal enum MuiListPresentationPolicyStateField : byte
	{
		Magic,
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
	internal struct MuiListPresentationPolicyStateFieldCursor
	{
		internal APTR Address;
		internal MuiListPresentationPolicyStateField Field;
	}

	internal static class MuiListPresentationPolicyStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(
			MuiListPresentationPolicyStateField field, out uint index)
		{
			index = field switch
			{
				MuiListPresentationPolicyStateField.Magic => 0,
				MuiListPresentationPolicyStateField.Editable => 1,
				MuiListPresentationPolicyStateField.Quiet => 2,
				MuiListPresentationPolicyStateField.AdjustHeight => 3,
				MuiListPresentationPolicyStateField.AdjustWidth => 4,
				MuiListPresentationPolicyStateField.Stripes => 5,
				MuiListPresentationPolicyStateField.ShowDropMarks => 6,
				MuiListPresentationPolicyStateField.DragSortable => 7,
				MuiListPresentationPolicyStateField.DragType => 8,
				MuiListPresentationPolicyStateField.AutoVisible => 9,
				MuiListPresentationPolicyStateField.AutoLineHeight => 10,
				MuiListPresentationPolicyStateField.MinLineHeight => 11,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPresentationPolicyStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPresentationPolicyStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListPresentationPolicyState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListPresentationPolicyState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListPresentationPolicyState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPresentationPolicyStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListPresentationPolicyStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListPresentationPolicyStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListPresentationPolicyStateField.Editable:
					value = state.Editable;
					return true;
				case MuiListPresentationPolicyStateField.Quiet:
					value = state.Quiet;
					return true;
				case MuiListPresentationPolicyStateField.AdjustHeight:
					value = state.AdjustHeight;
					return true;
				case MuiListPresentationPolicyStateField.AdjustWidth:
					value = state.AdjustWidth;
					return true;
				case MuiListPresentationPolicyStateField.Stripes:
					value = state.Stripes;
					return true;
				case MuiListPresentationPolicyStateField.ShowDropMarks:
					value = state.ShowDropMarks;
					return true;
				case MuiListPresentationPolicyStateField.DragSortable:
					value = state.DragSortable;
					return true;
				case MuiListPresentationPolicyStateField.DragType:
					value = state.DragType;
					return true;
				case MuiListPresentationPolicyStateField.AutoVisible:
					value = state.AutoVisible;
					return true;
				case MuiListPresentationPolicyStateField.AutoLineHeight:
					value = state.AutoLineHeight;
					return true;
				case MuiListPresentationPolicyStateField.MinLineHeight:
					value = state.MinLineHeight;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPresentationPolicyStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListPresentationPolicyStateCodec.TryReadStructural(ref platform,
				address, out var state)) return false;
			switch (field)
			{
				case MuiListPresentationPolicyStateField.Magic:
					state.Magic = value;
					break;
				case MuiListPresentationPolicyStateField.Editable:
					state.Editable = value;
					break;
				case MuiListPresentationPolicyStateField.Quiet:
					state.Quiet = value;
					break;
				case MuiListPresentationPolicyStateField.AdjustHeight:
					state.AdjustHeight = value;
					break;
				case MuiListPresentationPolicyStateField.AdjustWidth:
					state.AdjustWidth = value;
					break;
				case MuiListPresentationPolicyStateField.Stripes:
					state.Stripes = value;
					break;
				case MuiListPresentationPolicyStateField.ShowDropMarks:
					state.ShowDropMarks = value;
					break;
				case MuiListPresentationPolicyStateField.DragSortable:
					state.DragSortable = value;
					break;
				case MuiListPresentationPolicyStateField.DragType:
					state.DragType = value;
					break;
				case MuiListPresentationPolicyStateField.AutoVisible:
					state.AutoVisible = value;
					break;
				case MuiListPresentationPolicyStateField.AutoLineHeight:
					state.AutoLineHeight = value;
					break;
				case MuiListPresentationPolicyStateField.MinLineHeight:
					state.MinLineHeight = value;
					break;
				default:
					return false;
			}
			return MuiListPresentationPolicyStateCodec.WriteRecord(ref platform,
				address, state);
		}
	}

	internal static class MuiListPresentationPolicyStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListPresentationPolicyStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPresentationPolicyStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListPresentationPolicyStateFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPresentationPolicyStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPresentationPolicyStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPresentationPolicyStateMemoryCodec.TryReadUInt32(ref platform,
				address, field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPresentationPolicyStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPresentationPolicyStateMemoryCodec.TryWriteUInt32(ref platform,
				address, field, value);
	}

	internal static class MuiListPresentationPolicyStateCodec
	{
		// Presentation policy is a fixed twelve-ULONG record. Keep all normalized
		// display and interaction switches in declaration order so drawing,
		// editing, drag, and viewport paths share one named struct boundary.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListPresentationPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListPresentationPolicyState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Editable) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Quiet) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.AdjustHeight) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.AdjustWidth) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Stripes) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.ShowDropMarks) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.DragSortable) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.DragType) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.AutoVisible) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.AutoLineHeight) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.MinLineHeight)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListPresentationPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListPresentationPolicyState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Editable) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Quiet) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.AdjustHeight) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.AdjustWidth) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Stripes) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ShowDropMarks) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DragSortable) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DragType) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.AutoVisible) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.AutoLineHeight) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MinLineHeight) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListPresentationPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListPresentationPolicyState.Size) || value.Magic !=
				MuiListPresentationPolicyState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListPresentationPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListPresentationPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListPresentationPolicyState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListPresentationPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	internal enum MuiListHookPolicyStateField : byte
	{
		Magic,
		ConstructHook,
		DestructHook,
		DisplayHook,
		CompareHook,
		MultiTestHook,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListHookPolicyStateFieldCursor
	{
		internal APTR Address;
		internal MuiListHookPolicyStateField Field;
	}

	internal static class MuiListHookPolicyStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListHookPolicyStateField field,
			out uint index)
		{
			index = field switch
			{
				MuiListHookPolicyStateField.Magic => 0,
				MuiListHookPolicyStateField.ConstructHook => 1,
				MuiListHookPolicyStateField.DestructHook => 2,
				MuiListHookPolicyStateField.DisplayHook => 3,
				MuiListHookPolicyStateField.CompareHook => 4,
				MuiListHookPolicyStateField.MultiTestHook => 5,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListHookPolicyStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListHookPolicyStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListHookPolicyState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListHookPolicyState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListHookPolicyState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListHookPolicyStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListHookPolicyStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListHookPolicyStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListHookPolicyStateField.ConstructHook:
					value = state.ConstructHook;
					return true;
				case MuiListHookPolicyStateField.DestructHook:
					value = state.DestructHook;
					return true;
				case MuiListHookPolicyStateField.DisplayHook:
					value = state.DisplayHook;
					return true;
				case MuiListHookPolicyStateField.CompareHook:
					value = state.CompareHook;
					return true;
				case MuiListHookPolicyStateField.MultiTestHook:
					value = state.MultiTestHook;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListHookPolicyStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListHookPolicyStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListHookPolicyStateField.Magic:
					state.Magic = value;
					break;
				case MuiListHookPolicyStateField.ConstructHook:
					state.ConstructHook = value;
					break;
				case MuiListHookPolicyStateField.DestructHook:
					state.DestructHook = value;
					break;
				case MuiListHookPolicyStateField.DisplayHook:
					state.DisplayHook = value;
					break;
				case MuiListHookPolicyStateField.CompareHook:
					state.CompareHook = value;
					break;
				case MuiListHookPolicyStateField.MultiTestHook:
					state.MultiTestHook = value;
					break;
				default:
					return false;
			}
			return MuiListHookPolicyStateCodec.WriteRecord(ref platform, address, state);
		}
	}

	internal static class MuiListHookPolicyStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListHookPolicyStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListHookPolicyStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListHookPolicyStateFieldCursor cursor, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListHookPolicyStateMemoryCodec.TryGetAddress(ref platform,
				cursor.Address, cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListHookPolicyStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListHookPolicyStateMemoryCodec.TryReadUInt32(ref platform, address,
				field, out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListHookPolicyStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListHookPolicyStateMemoryCodec.TryWriteUInt32(ref platform, address,
				field, value);
	}

	internal static class MuiListHookPolicyStateCodec
	{
		// Hook policy is a fixed six-ULONG pointer record. Keep the hook entry
		// points in declaration order so every List producer consumes one named
		// configuration struct rather than rebuilding private offsets.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListHookPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListHookPolicyState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.ConstructHook) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.DestructHook) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.DisplayHook) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.CompareHook) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.MultiTestHook)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListHookPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListHookPolicyState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ConstructHook) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DestructHook) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.DisplayHook) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.CompareHook) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.MultiTestHook) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListHookPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block,
				MuiListHookPolicyState.Size) || value.Magic !=
				MuiListHookPolicyState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListHookPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListHookPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListHookPolicyState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListHookPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	internal enum MuiListSortStateField : byte
	{
		Magic,
		SortColumn,
		TitleClick,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListSortStateFieldCursor
	{
		internal APTR Address;
		internal MuiListSortStateField Field;
	}

	internal static class MuiListSortStateMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListSortStateField field,
			out uint index)
		{
			index = field switch
			{
				MuiListSortStateField.Magic => 0,
				MuiListSortStateField.SortColumn => 1,
				MuiListSortStateField.TitleClick => 2,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListSortStateField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListSortStateField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListSortState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListSortState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListSortState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListSortStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListSortStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListSortStateField.Magic:
					value = state.Magic;
					return true;
				case MuiListSortStateField.SortColumn:
					value = state.SortColumn;
					return true;
				case MuiListSortStateField.TitleClick:
					value = state.TitleClick;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListSortStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListSortStateCodec.TryReadStructural(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListSortStateField.Magic:
					state.Magic = value;
					break;
				case MuiListSortStateField.SortColumn:
					state.SortColumn = value;
					break;
				case MuiListSortStateField.TitleClick:
					state.TitleClick = value;
					break;
				default:
					return false;
			}
			return MuiListSortStateCodec.WriteRecord(ref platform, address, state);
		}
	}

	internal static class MuiListSortStateFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListSortStateFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListSortStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListSortStateFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListSortStateMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListSortStateField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListSortStateMemoryCodec.TryReadUInt32(ref platform, address, field,
				out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListSortStateField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListSortStateMemoryCodec.TryWriteUInt32(ref platform, address, field,
				value);
	}

	internal static class MuiListSortStateCodec
	{
		// Sort state is a fixed three-ULONG record. Keep the selected and
		// title-click columns adjacent in declaration order so sort notifications
		// consume one named struct rather than unrelated scalar offsets.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListSortState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListSortState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.SortColumn) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.TitleClick)) return false;
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR block, MuiListSortState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, block,
				MuiListSortState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.SortColumn) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.TitleClick) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool Write<TPlatform>(ref TPlatform platform, APTR block,
			MuiListSortState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (block.IsNull || !platform.IsMapped(block, MuiListSortState.Size) ||
				value.Magic != MuiListSortState.Cookie) return false;
			return WriteRecord(ref platform, block, value);
		}

		internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListSortState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, block, out value);
		}

		internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR block,
			out MuiListSortState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value) &&
			value.Magic == MuiListSortState.Cookie;

		internal static bool TryReadStorage<TPlatform>(ref TPlatform platform,
			APTR block, out MuiListSortState value)
			where TPlatform : struct, IMuiGuestMemory =>
			TryReadStructural(ref platform, block, out value);
	}

	// MUIA_List_Pool, MUIA_List_PoolPuddleSize, and
	// MUIA_List_PoolThreshSize are one construction policy. Keep the policy in
	// a named guest record so the native Exec pool handle and its ownership stay
	// explicit. A caller-supplied pool is borrowed; when it is omitted the
	// platform creates the list-owned standard Exec pool used by construct and
	// destruct hooks. No host allocator or managed object is hidden in this
	// record.
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MuiListPoolPolicyState
	{
		internal const uint Size = 20;
		internal const uint Cookie = 0x504F4F4Cu; // 'POOL'
		internal const uint FieldSize = 4;
		internal const uint MagicOffset = 0;
		internal const uint PoolOffset = 4;
		internal const uint PuddleSizeOffset = 8;
		internal const uint ThresholdSizeOffset = 12;
		internal const uint UsesExternalPoolOffset = 16;

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

	internal static class MuiListPoolPolicyMemoryCodec
	{
		private static bool TryResolveFieldIndex(MuiListPoolPolicyField field,
			out uint index)
		{
			index = field switch
			{
				MuiListPoolPolicyField.Magic => 0,
				MuiListPoolPolicyField.Pool => 1,
				MuiListPoolPolicyField.PuddleSize => 2,
				MuiListPoolPolicyField.ThresholdSize => 3,
				MuiListPoolPolicyField.UsesExternalPool => 4,
				_ => uint.MaxValue,
			};
			return index != uint.MaxValue;
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPoolPolicyField field, out APTR address)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryGetAddress(ref platform, record, field, out address,
				out _);
		}

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			APTR record, MuiListPoolPolicyField field, out APTR address,
			out uint size)
			where TPlatform : struct, IMuiGuestMemory
		{
			address = APTR.Null;
			size = 0;
			if (!TryResolveFieldIndex(field, out var index) ||
				!MuiGuestStructCursor.TryCreate(ref platform, record,
					MuiListPoolPolicyState.Size, out var cursor)) return false;
			for (var current = 0u; current <= index; current++)
			{
				if (!MuiGuestStructCursor.TryTake(ref platform, ref cursor,
					MuiListPoolPolicyState.FieldSize, out var candidate)) return false;
				if (current == index)
				{
					address = candidate;
					size = MuiListPoolPolicyState.FieldSize;
					return true;
				}
			}
			return false;
		}

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPoolPolicyField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = 0;
			if (!MuiListPoolPolicyStateCodec.TryReadRecord(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListPoolPolicyField.Magic:
					value = state.Magic;
					return true;
				case MuiListPoolPolicyField.Pool:
					value = state.Pool.Raw;
					return true;
				case MuiListPoolPolicyField.PuddleSize:
					value = state.PuddleSize;
					return true;
				case MuiListPoolPolicyField.ThresholdSize:
					value = state.ThresholdSize;
					return true;
				case MuiListPoolPolicyField.UsesExternalPool:
					value = state.UsesExternalPool;
					return true;
				default:
					return false;
			}
		}

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPoolPolicyField field, uint value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (!MuiListPoolPolicyStateCodec.TryReadRecord(ref platform, address,
				out var state)) return false;
			switch (field)
			{
				case MuiListPoolPolicyField.Magic:
					state.Magic = value;
					break;
				case MuiListPoolPolicyField.Pool:
					state.Pool = APTR.FromPointer(value);
					break;
				case MuiListPoolPolicyField.PuddleSize:
					state.PuddleSize = value;
					break;
				case MuiListPoolPolicyField.ThresholdSize:
					state.ThresholdSize = value;
					break;
				case MuiListPoolPolicyField.UsesExternalPool:
					state.UsesExternalPool = value;
					break;
				default:
					return false;
			}
			return MuiListPoolPolicyStateCodec.WriteRecord(ref platform, address, state);
		}
	}

	internal static class MuiListPoolPolicyFieldCursorCodec
	{
		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListPoolPolicyFieldCursor cursor, out APTR address)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPoolPolicyMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address);

		internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
			MuiListPoolPolicyFieldCursor cursor, out APTR address, out uint size)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPoolPolicyMemoryCodec.TryGetAddress(ref platform, cursor.Address,
				cursor.Field, out address, out size);

		internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPoolPolicyField field, out uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPoolPolicyMemoryCodec.TryReadUInt32(ref platform, address, field,
				out value);

		internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPoolPolicyField field, uint value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiListPoolPolicyMemoryCodec.TryWriteUInt32(ref platform, address, field,
				value);
	}

	internal static class MuiListPoolPolicyStateCodec
	{
		// Pool policy is a fixed five-ULONG record. Keep the opaque Exec handle,
		// allocation sizes, and ownership flag in declaration order; only the
		// bounded guest-memory adapter knows their wire positions.
		internal static bool TryReadRecord<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListPoolPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			value = default;
			if (!MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiListPoolPolicyState.Size, out var cursor) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.Magic) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out var pool) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.PuddleSize) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.ThresholdSize) ||
				!MuiGuestStructCursor.TryReadUInt32(ref platform, ref cursor,
					out value.UsesExternalPool)) return false;
			value.Pool = APTR.FromPointer(pool);
			return MuiGuestStructCursor.IsComplete(cursor);
		}

		internal static bool WriteRecord<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPoolPolicyState value)
			where TPlatform : struct, IMuiGuestMemory =>
			MuiGuestStructCursor.TryCreate(ref platform, address,
				MuiListPoolPolicyState.Size, out var cursor) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Magic) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.Pool.Raw) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.PuddleSize) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.ThresholdSize) &&
			MuiGuestStructCursor.TryWriteUInt32(ref platform, ref cursor,
				value.UsesExternalPool) && MuiGuestStructCursor.IsComplete(cursor);

		internal static bool TryRead<TPlatform>(ref TPlatform platform,
			APTR address, out MuiListPoolPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			return TryReadRecord(ref platform, address, out value) &&
				value.Magic == MuiListPoolPolicyState.Cookie;
		}

		internal static bool Write<TPlatform>(ref TPlatform platform,
			APTR address, MuiListPoolPolicyState value)
			where TPlatform : struct, IMuiGuestMemory
		{
			if (address.IsNull || !platform.IsMapped(address,
				MuiListPoolPolicyState.Size) ||
				value.Magic != MuiListPoolPolicyState.Cookie) return false;
			value.UsesExternalPool = value.UsesExternalPool == 0 ? 0u : 1u;
			return WriteRecord(ref platform, address, value);
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
	public const uint Active = 0x8042391cu;          // [ISG] LONG
	public const uint Editable = 0x8042f9b9u;       // [ISG] BOOL
	public const uint Entries = 0x80421654u;         // [..G] LONG
	public const uint First = 0x804238d4u;           // [.SG] LONG
	public const uint Visible = 0x8042191fu;         // [..G] LONG
	// MorphOS publishes -1 when the List has no visible geometry (for example
	// while its window is iconified). Keep the ABI sentinel named so composite
	// scroller code never mistakes it for a huge row capacity.
	internal const uint VisibleOff = uint.MaxValue;
	private const uint ConstructHook = 0x8042894fu;   // [IS.] struct Hook *
	private const uint DestructHook = 0x804297ceu;    // [IS.] struct Hook *
	private const uint DisplayHook = 0x8042b4d5u;     // [IS.] struct Hook *
	private const uint CompareHook = 0x80425c14u;     // [IS.] struct Hook *
	private const uint SourceArray = 0x8042c0a0u;     // [I..] APTR
	public const uint Pool = 0x80423431u;             // [I.G] APTR
	public const uint Quiet = 0x8042d8c7u;           // [.SG] BOOL
	public const uint SelectChange = 0x8042178fu;    // [..G] BOOL
	private const uint Input = 0x8042682du;           // [I..] BOOL
	private const uint MultiSelect = 0x80427e08u;     // [I..] LONG
	private const uint ScrollerPos = 0x8042b1b4u;     // [I..] LONG
	public const uint AgainClick = 0x804214c2u;      // [ISG] BOOL
	public const uint ClickColumn = 0x8042d1b3u;     // [.SG] LONG
	public const uint DefClickColumn = 0x8042b296u;  // [ISG] LONG
	public const uint DoubleClick = 0x80424635u;     // [ISG] BOOL
	private const uint MultiTestHook = 0x8042c2c6u;   // [IS.] struct Hook *
	public const uint SortColumn = 0x8042cafbu;      // [ISG] LONG
	private const uint PoolPuddleSize = 0x8042a4ebu;  // [I..] ULONG
	private const uint PoolThreshSize = 0x8042c48cu;   // [I..] ULONG
	public const uint InsertPosition = 0x8042d0cdu;   // [..G] LONG
	public const uint Format = 0x80423c0au;           // [ISG] STRPTR
	private const uint MaxColumns = 0x8042a98bu;       // [I..] LONG
	private const uint AdjustHeight = 0x8042850du;     // [I..] BOOL
	private const uint AdjustWidth = 0x8042354au;      // [I..] BOOL
	public const uint Stripes = 0x8042a308u;           // [ISG] BOOL
	public const uint DropMark = 0x8042aba6u;         // [..G] LONG
	public const uint ShowDropMarks = 0x8042c6f3u;     // [ISG] BOOL
	public const uint DragSortable = 0x80426099u;      // [ISG] BOOL
	public const uint DragType = 0x80425cd3u;          // [ISG] LONG
	public const uint AutoVisible = 0x8042a445u;       // [ISG] BOOL
	private const uint MinLineHeight = 0x8042d1c3u;    // [I..] LONG
	public const uint AutoLineHeight = 0x8042bc08u;    // [ISG] BOOL
	public const uint LineHeight = 0x80425880u;       // [..G] ULONG
	public const uint Title = 0x80423e66u;            // [ISG] STRPTR/BOOL
	public const uint TitleArray = 0x80427d95u;        // [ISG] STRPTR *
	public const uint TitleClick = 0x80422fd9u;       // [.SG] LONG
	private const uint HScrollerVisibility = 0x804280a6u; // [I..] LONG
	private const uint HideColumn = 0x80428052u;       // [IS.] LONG
	private const uint ShowColumn = 0x8042c840u;       // [IS.] LONG
	public const uint ColumnOrder = 0x9d5100f6u;       // [.SG] BYTE*
	public const uint TopPixel = 0x80429df3u;         // [.SG] LONG
	public const uint TotalPixel = 0x8042a8f5u;        // [..G] ULONG
	public const uint VisiblePixel = 0x804273e9u;      // [..G] ULONG
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
	internal const uint FormatDescriptorKey = 0x7F080003u;
	internal const uint ColumnLayoutKey = 0x7F080004u;
	private const uint EditStateKey = 0x7F080006u;
	private const uint TitleArrayStateKey = 0x7F080007u;
	private const uint TitleStateKey = 0x7F080016u;
	private const uint SelectionSignalKey = 0x7F080017u;
	private const uint FormatPolicyKey = 0x7F080018u;
	private const uint FontStateKey = 0x7F080019u;
	private const uint InsertPositionStateKey = 0x7F08001Au;
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
	private const uint TitleArrayStateCookie = 0x5449544Cu; // 'TITL'
	private const uint TitleStateCookie = MuiListTitleState.Cookie;
	private const uint SelectionSignalCookie = MuiListSelectionSignalState.Cookie;
	private const uint FormatPolicyCookie = MuiListFormatPolicyState.Cookie;
	private const uint FontStateCookie = MuiListFontState.Cookie;
	private const uint RedrawStateCookie = 0x52454452u; // 'REDR'
	private const uint ActiveStateCookie = MuiListActiveState.Cookie;
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
		var cursor = default(MuiClassNameByteCursor);
		cursor.Name = name;
		var length = 0;
		for (; length < 64; length++)
		{
			cursor.Index = (uint)length;
			if (!MuiClassNameByteCursorCodec.TryReadByte(ref platform, cursor,
				out var ch)) return MuiCollectionClass.Unknown;
			if (ch == 0) break;
			hash = (hash ^ Lower(ch)) * 16777619u;
		}
		if (length < 5 || length == 64) return MuiCollectionClass.Unknown;
		cursor.Index = (uint)(length - 4);
		if (!MuiClassNameByteCursorCodec.TryReadByte(ref platform, cursor,
			out var suffix) || suffix != (byte)'.')
			return MuiCollectionClass.Unknown;
		cursor.Index = (uint)(length - 3);
		if (!MuiClassNameByteCursorCodec.TryReadByte(ref platform, cursor,
			out suffix) || Lower(suffix) != (byte)'m')
			return MuiCollectionClass.Unknown;
		cursor.Index = (uint)(length - 2);
		if (!MuiClassNameByteCursorCodec.TryReadByte(ref platform, cursor,
			out suffix) || Lower(suffix) != (byte)'u')
			return MuiCollectionClass.Unknown;
		cursor.Index = (uint)(length - 1);
		if (!MuiClassNameByteCursorCodec.TryReadByte(ref platform, cursor,
			out suffix) || Lower(suffix) != (byte)'i')
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

	// Generic Get and OM_GET projections for the core List row and viewport
	// values. The named Active, Presentation, SelectionSignal, and Viewport
	// records are authoritative once the List lifecycle has published them;
	// raw attributes remain only as bounded early-lifecycle fallbacks.
	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == Active || attribute == Editable || attribute == Entries ||
		attribute == First || attribute == SelectChange || attribute == Quiet ||
		attribute == InsertPosition ||
		attribute == Stripes || attribute == ShowDropMarks ||
		attribute == DragSortable || attribute == DragType ||
		attribute == AutoVisible || attribute == AutoLineHeight ||
		attribute == TopPixel || attribute == TotalPixel ||
		attribute == Visible || attribute == VisiblePixel ||
		attribute == Pool || attribute == TitleArray || attribute == ColumnOrder ||
		attribute == Format || attribute == SortColumn || attribute == Title ||
		attribute == DropMark || attribute == LineHeight ||
			attribute == AgainClick || attribute == ClickColumn ||
			attribute == DefClickColumn || attribute == DoubleClick ||
			attribute == TitleClick || attribute == ConstructHook ||
			attribute == DestructHook || attribute == DisplayHook ||
			attribute == CompareHook || attribute == MultiTestHook;

	internal static bool TryGetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		var listClass = Classify(ref platform, state, obj);
		if (!IsPublicGetterAttribute(attribute) ||
			!IsListBacked(listClass)) return false;
		if (attribute == Entries)
		{
			value = EntryCount(ref platform, state, obj);
			return true;
		}
		if (attribute == InsertPosition)
		{
			if (!TryReadInsertPositionAdmission(ref platform, state, obj,
				out var insertPosition, out var present)) return false;
			if (!present && (!EnsureInsertPositionState(ref platform, state, obj) ||
				!TryReadInsertPositionAdmission(ref platform, state, obj,
					out insertPosition, out present) || !present)) return false;
			value = insertPosition.Position;
			return true;
		}
		if (attribute == Active)
		{
			if (!TryReadActiveStateAdmission(ref platform, state, obj,
				out _, out var activePresent)) return false;
			if (!activePresent && !EnsureActiveState(ref platform, state, obj))
				return false;
			var count = EntryCount(ref platform, state, obj);
			var active = ActiveIndex(ref platform, state, obj);
			value = count == 0 ? 0u : unchecked((uint)active);
			return true;
		}
		if (attribute == Editable || attribute == Quiet || attribute == Stripes ||
			attribute == ShowDropMarks || attribute == DragSortable ||
			attribute == DragType || attribute == AutoVisible ||
			attribute == AutoLineHeight)
		{
			if (!TryGetPresentationPolicy(ref platform, state, obj, out var policy) &&
				(!EnsurePresentationPolicy(ref platform, state, obj) ||
					!TryGetPresentationPolicy(ref platform, state, obj, out policy)))
				return false;
			value = attribute == Editable ? policy.Editable :
				attribute == Quiet ? policy.Quiet :
				attribute == Stripes ? policy.Stripes :
				attribute == ShowDropMarks ? policy.ShowDropMarks :
				attribute == DragSortable ? policy.DragSortable :
				attribute == DragType ? policy.DragType :
				attribute == AutoVisible ? policy.AutoVisible :
				policy.AutoLineHeight;
			return true;
		}
		if (attribute == SelectChange)
		{
			if (!TryReadSelectionSignalAdmission(ref platform, state, obj,
				out var signal, out var present)) return false;
			if (present)
			{
				value = signal.Value;
				return true;
			}
			if (!EnsureSelectionSignalState(ref platform, state, obj) ||
				!TryReadSelectionSignalAdmission(ref platform, state, obj,
					out signal, out present) || !present) return false;
			value = signal.Value;
			return true;
		}
		if (attribute == Pool)
		{
			if (!TryReadPoolPolicyAdmission(ref platform, state, obj,
				out var pool, out var present)) return false;
			if (!present && (!EnsurePoolPolicy(ref platform, state, obj) ||
				!TryReadPoolPolicyAdmission(ref platform, state, obj,
					out pool, out present) || !present)) return false;
			value = pool.Pool.Raw;
			return true;
		}
		if (attribute == TitleArray)
		{
			if (!TryReadTitleArrayAdmission(ref platform, state, obj,
				out var titleArray, out var present)) return false;
			if (present)
			{
				value = titleArray.Pointers.Raw;
				return true;
			}
		value = ReadRaw(ref platform, state, obj, TitleArray, 0);
			return true;
		}
		if (attribute == ColumnOrder)
		{
			if (!TryReadColumnOrderAdmission(ref platform, state, obj,
				out var columnOrder, out var present)) return false;
			if (present)
			{
				value = columnOrder.Values.Raw;
				return true;
			}
		value = ReadRaw(ref platform, state, obj, ColumnOrder, 0);
			return true;
		}
		if (attribute == Format)
		{
			if (!TryReadFormatProjectionAdmission(ref platform, state, obj,
				out var format, out var present, out _, out _) || !present)
			{
				if (!EnsureFormatPolicyState(ref platform, state, obj) ||
					!TryReadFormatProjectionAdmission(ref platform, state, obj,
						out format, out present, out _, out _) || !present)
					return false;
			}
			value = format.Format.Raw;
			return true;
		}
		if (attribute == SortColumn)
		{
			if (!TryReadSortStateAdmission(ref platform, state, obj,
				out var sort, out var present)) return false;
			if (!present && (!EnsureSortState(ref platform, state, obj) ||
				!TryReadSortStateAdmission(ref platform, state, obj,
					out sort, out present) || !present)) return false;
			value = sort.SortColumn;
			return true;
		}
		if (attribute == Title)
		{
			if (!TryReadTitleStateAdmission(ref platform, state, obj,
				out var title, out var present)) return false;
			if (!present && (!EnsureTitleState(ref platform, state, obj) ||
				!TryReadTitleStateAdmission(ref platform, state, obj,
					out title, out present) || !present)) return false;
			value = title.Value;
			return true;
		}
		if (attribute == AgainClick || attribute == ClickColumn ||
			attribute == DefClickColumn || attribute == DoubleClick)
		{
			if (!TryReadClickStateAdmission(ref platform, state, obj,
				out var click, out var present)) return false;
			if (!present && (!EnsureClickState(ref platform, state, obj) ||
				!TryReadClickStateAdmission(ref platform, state, obj,
					out click, out present) || !present)) return false;
			value = attribute == AgainClick ? click.AgainClick :
				attribute == ClickColumn ? click.ClickColumn :
				attribute == DefClickColumn ? click.DefClickColumn :
				click.DoubleClick;
			return true;
		}
		if (attribute == TitleClick)
		{
			if (!TryReadSortStateAdmission(ref platform, state, obj,
				out var sort, out var present)) return false;
			if (!present && (!EnsureSortState(ref platform, state, obj) ||
				!TryReadSortStateAdmission(ref platform, state, obj,
					out sort, out present) || !present)) return false;
			value = sort.TitleClick;
			return true;
		}
		if (attribute == ConstructHook || attribute == DestructHook ||
			attribute == DisplayHook || attribute == CompareHook ||
			attribute == MultiTestHook)
		{
			if (!TryReadHookPolicyAdmission(ref platform, state, obj,
				out var hooks, out var present)) return false;
			if (!present && (!EnsureHookPolicy(ref platform, state, obj) ||
				!TryReadHookPolicyAdmission(ref platform, state, obj,
					out hooks, out present) || !present)) return false;
			value = attribute == ConstructHook ? hooks.ConstructHook :
				attribute == DestructHook ? hooks.DestructHook :
				attribute == DisplayHook ? hooks.DisplayHook :
				attribute == CompareHook ? hooks.CompareHook :
				hooks.MultiTestHook;
			return true;
		}
		if ((attribute == First || attribute == Visible ||
			attribute == TopPixel || attribute == TotalPixel ||
			attribute == VisiblePixel || attribute == DropMark ||
			attribute == LineHeight) &&
			TryGetViewportState(ref platform, state, obj, out var viewport))
		{
			if (attribute == First &&
				ReadRaw(ref platform, state, obj, First, viewport.First) ==
					VisibleOff)
			{
				// Preserve MorphOS's explicit First=-1 sentinel. The viewport
				// record stores normalized geometry and therefore may contain zero
				// while the public cursor is intentionally off-screen.
				value = VisibleOff;
				return true;
			}
			value = attribute == First ? viewport.First :
				attribute == Visible ? viewport.Visible :
				attribute == TopPixel ? viewport.TopPixel :
				attribute == TotalPixel ? viewport.TotalPixel :
				attribute == VisiblePixel ? viewport.VisiblePixel :
				attribute == DropMark ? viewport.DropMark : viewport.LineHeight;
			return true;
		}
		value = ReadRaw(ref platform, state, obj, attribute,
			attribute == Visible ? 0u :
			attribute == First ? 0u :
			attribute == TopPixel ? 0u :
			attribute == TotalPixel ? 0u : 0u);
		return true;
	}

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
		attribute == Quiet || attribute == Font || attribute == Format ||
		attribute == MaxColumns ||
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
			!IsValidColumnOrderValues(ref platform, value) ||
			displayColumn >= value.Count) return fallback;
		var cursor = default(MuiListColumnOrderByteCursor);
		cursor.Base = value.Values;
		cursor.Index = displayColumn;
		return MuiListColumnOrderByteVectorCodec.TryReadValue(ref platform,
			cursor, out var resolved) ? resolved : fallback;
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
		if (!TryReadHeaderAdmission(ref platform, state, obj,
			out _, out var headerPresent)) return false;
		if (headerPresent) return true;

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
			!EnsureFontState(ref platform, state, obj) ||
			!EnsureInsertPositionState(ref platform, state, obj)) return false;

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
		// ApplyActive resolves construction sentinels and clamps raw requests
		// before the named cursor publishes HasActive. This keeps dynamic range
		// admission strict without rejecting a valid out-of-range construction
		// tag that MorphOS would normalize to the last row.
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
		if (!TryReadHScrollerStateAdmission(ref platform, state, obj,
			out var current, out var present)) return false;
		var normalized = NormalizeHScrollerPolicy(policy);
		if (present)
		{
			var existingBlock = APTR.FromPointer(Read(ref platform, state, obj,
				HScrollerStateKey, 0));
			current.Policy = normalized;
			return MuiListHScrollerStateCodec.Write(ref platform, existingBlock,
				current);
		}

		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListHScrollerState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListHScrollerState);
		value.Magic = MuiListHScrollerState.Cookie;
		value.Policy = normalized;
		if (!MuiListHScrollerStateCodec.Write(ref platform, block, value))
		{
			platform.Clear(block, MuiListHScrollerState.Size);
			platform.Free(block, MuiListHScrollerState.Size);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			HScrollerStateKey, block.Raw, false))
		{
			platform.Clear(block, MuiListHScrollerState.Size);
			platform.Free(block, MuiListHScrollerState.Size);
			return false;
		}
		return true;
	}

	// A published horizontal-scroller record is authoritative guest state. A
	// non-NULL record that fails the cookie/field contract is malformed, not
	// absence; viewport and scroll consumers must fail closed instead of
	// replacing it from the raw HScrollerVisibility projection.
	private static bool TryReadHScrollerStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListHScrollerState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			HScrollerStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListHScrollerStateCodec.TryRead(ref platform, block,
			out value) && IsValidHScrollerState(value);
	}

	// The horizontal-scroller record is derived state, but every field still has
	// a named contract. Policy and visibility are canonical BOOL/enumeration
	// values; scroll position cannot exceed the derived range, and the range is
	// exactly the content/viewport difference. Rejecting incoherent state keeps
	// Listview consumers from treating a stale geometry word as a live scroller.
	private static bool IsValidHScrollerState(MuiListHScrollerState value) =>
		value.Policy <= HScrollerNever && value.Visible <= 1 &&
		value.MaxScrollX == (value.ContentWidth > value.ViewWidth
			? value.ContentWidth - value.ViewWidth : 0u) &&
		value.ScrollX <= value.MaxScrollX;

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
		// Pool ownership is part of the typed policy. A non-NULL record that
		// fails its contract is malformed, not absence; never create a second
		// pool or replace a borrowed handle from raw compatibility attributes.
		if (!TryReadPoolPolicyAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		if (present) return true;
		var block = APTR.Null;
		var value = default(MuiListPoolPolicyState);
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
		if (value.Pool.IsNull)
		{
			// MorphOS supplies a standard Kickstart-compatible pool in A2 even
			// when MUIA_List_Pool was omitted. Keep the handle opaque and let the
			// native Exec provider own its internal layout.
			value.Pool = platform.CreatePool(0, value.PuddleSize,
				value.ThresholdSize);
			if (value.Pool.IsNull)
			{
				platform.Clear(block, MuiListPoolPolicyState.Size);
				platform.Free(block, MuiListPoolPolicyState.Size);
				return false;
			}
		}
		if (!MuiListPoolPolicyStateCodec.Write(ref platform, block, value))
		{
			if (value.UsesExternalPool == 0)
				platform.DeletePool(value.Pool);
			platform.Clear(block, MuiListPoolPolicyState.Size);
			platform.Free(block, MuiListPoolPolicyState.Size);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			PoolPolicyKey, block.Raw, false))
		{
			if (value.UsesExternalPool == 0 && value.Pool.IsNotNull)
				platform.DeletePool(value.Pool);
			platform.Clear(block, MuiListPoolPolicyState.Size);
			platform.Free(block, MuiListPoolPolicyState.Size);
			return false;
		}
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
		// The construction-only interaction record is authoritative after
		// publication. A non-NULL block that fails its field contract is
		// malformed, not absence; do not normalize it from raw aliases.
		if (!TryReadInteractionPolicyAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		if (present) return true;
		var block = APTR.Null;
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

		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListInteractionPolicyState.Size);
		if (block.IsNull) return false;
		if (!MuiListInteractionPolicyStateCodec.Write(ref platform, block, value))
		{
			platform.Clear(block, MuiListInteractionPolicyState.Size);
			platform.Free(block, MuiListInteractionPolicyState.Size);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			InteractionPolicyKey, block.Raw, false))
		{
			platform.Clear(block, MuiListInteractionPolicyState.Size);
			platform.Free(block, MuiListInteractionPolicyState.Size);
			return false;
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
		if (!TryReadInteractionPolicyAdmission(ref platform, state, obj,
			out value, out var present)) return false;
		return present;
	}

	// A published List interaction policy is authoritative guest state. A
	// non-NULL block that fails the cookie/field contract is malformed, not
	// absence; direct List consumers must not fall back to raw construction tags.
	private static bool TryReadInteractionPolicyAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListInteractionPolicyState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			InteractionPolicyKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadInteractionPolicy(ref platform, block, out value) &&
			IsValidInteractionPolicy(value);
	}

	// Interaction values are normalized at construction and are immutable after
	// publication. Keep the BOOL and enum domains at the named record boundary
	// so input and composite-scroller consumers never interpret an arbitrary
	// scalar as a MorphOS selection or scrollbar policy.
	private static bool IsValidInteractionPolicy(
		MuiListInteractionPolicyState value) =>
		value.Input <= 1 && value.MultiSelect <= MultiSelectAlways &&
		value.ScrollerPos <= ScrollerPosNone;

	private static bool TryReadInteractionPolicy<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListInteractionPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListInteractionPolicyStateCodec.TryRead(ref platform, block, out value);

	private static bool EnsureClickState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadClickStateAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		if (present) return true;

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
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ClickStateKey, block.Raw, false))
		{
			platform.Clear(block, MuiListClickState.Size);
			platform.Free(block, MuiListClickState.Size);
			return false;
		}
		return true;
	}

	private static bool ApplyClickStateAttribute<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj,
		uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadClickStateAdmission(ref platform, state, obj,
			out var clickState, out var present)) return false;
		if (!present && (!EnsureClickState(ref platform, state, obj) ||
			!TryReadClickStateAdmission(ref platform, state, obj,
				out clickState, out present) || !present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		var previous = clickState;
		var normalized = value;
		if (attribute == AgainClick || attribute == DoubleClick)
			normalized = value == 0 ? 0u : 1u;
		if (attribute == ClickColumn) clickState.ClickColumn = value;
		else if (attribute == AgainClick) clickState.AgainClick = normalized;
		else if (attribute == DoubleClick) clickState.DoubleClick = normalized;
		else if (attribute == DefClickColumn)
			clickState.DefClickColumn = value;
		else return false;
		if (!WriteClickState(ref platform, block, clickState)) return false;
		if (SetRaw(ref platform, state, record, attribute, normalized, notify))
			return true;
		WriteClickState(ref platform, block, previous);
		return false;
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
		if (record.IsNull) return false;
		if (!TryReadClickStateAdmission(ref platform, state, obj,
			out var value, out var present)) return false;
		if (!present && (!EnsureClickState(ref platform, state, obj) ||
			!TryReadClickStateAdmission(ref platform, state, obj,
				out value, out present) || !present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ClickStateKey, 0));
		var previous = value;
		value.ClickColumn = column;
		value.Clicks = clicks;
		value.DoubleClick = doubleClick ? 1u : 0u;
		value.AgainClick = againClick ? 1u : 0u;
		if (!WriteClickState(ref platform, block, value)) return false;
		if (!SetRaw(ref platform, state, record, ClickColumn, column, false) ||
			!SetRaw(ref platform, state, record, DoubleClick,
				doubleClick ? 1u : 0u, notify && doubleClick) ||
			!SetRaw(ref platform, state, record, AgainClick,
				againClick ? 1u : 0u, notify))
		{
			WriteClickState(ref platform, block, previous);
			return false;
		}
		return true;
	}

	internal static bool TryGetClickState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListClickState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadClickStateAdmission(ref platform, state, obj,
			out value, out var present) && present;
	}

	private static bool EnsureHookPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadHookPolicyAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HookPolicyKey, 0));
		if (present) return true;
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
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			HookPolicyKey, block.Raw, false))
		{
			platform.Clear(block, MuiListHookPolicyState.Size);
			platform.Free(block, MuiListHookPolicyState.Size);
			return false;
		}
		return true;
	}

	private static bool ApplyHookPolicyAttribute<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj,
		uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadHookPolicyAdmission(ref platform, state, obj,
			out var hookPolicy, out var present)) return false;
		if (!present && (!EnsureHookPolicy(ref platform, state, obj) ||
			!TryReadHookPolicyAdmission(ref platform, state, obj,
				out hookPolicy, out present) || !present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			HookPolicyKey, 0));
		var previous = hookPolicy;
		switch (attribute)
		{
			case ConstructHook: hookPolicy.ConstructHook = value; break;
			case DestructHook: hookPolicy.DestructHook = value; break;
			case DisplayHook: hookPolicy.DisplayHook = value; break;
			case CompareHook: hookPolicy.CompareHook = value; break;
			case MultiTestHook: hookPolicy.MultiTestHook = value; break;
			default: return false;
		}
		if (!WriteHookPolicy(ref platform, block, hookPolicy)) return false;
		if (SetRaw(ref platform, state, record, attribute, value, notify))
			return true;
		WriteHookPolicy(ref platform, block, previous);
		return false;
	}

	internal static bool TryGetHookPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListHookPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadHookPolicyAdmission(ref platform, state, obj,
			out value, out var present) && present;
	}

	// Internal consumers use this typed projection instead of rereading hook
	// attributes independently. The raw fallback is needed only before List
	// construction has installed its hook policy record.
	internal static uint HookPolicyValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadHookPolicyAdmission(ref platform, state, obj,
			out var value, out var present)) return 0;
		if (present)
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
		return ReadRaw(ref platform, state, obj, attribute, 0);
	}

	private static bool WriteHookPolicy<TPlatform>(ref TPlatform platform,
		APTR block, MuiListHookPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (block.IsNull || value.Magic != MuiListHookPolicyState.Cookie)
			return false;
		return MuiListHookPolicyStateCodec.Write(ref platform, block, value);
	}

	private static bool TryReadHookPolicy<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListHookPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListHookPolicyStateCodec.TryRead(ref platform, block, out value);
	}

	// A published hook policy is authoritative guest state. A non-NULL record
	// that fails the cookie/field contract is malformed, not absence; hook
	// dispatch must not replace it or fall back to raw hook attributes.
	private static bool TryReadHookPolicyAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListHookPolicyState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			HookPolicyKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadHookPolicy(ref platform, block, out value);
	}

	private static bool EnsureSortState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSortStateAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SortStateKey, 0));
		if (present) return true;
		var value = default(MuiListSortState);
		value.Magic = MuiListSortState.Cookie;
		var hasSortColumn = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, SortColumn, out var sortColumn);
		var hasTitleClick = MuiHeadlessObjectCore.GetRawAttribute(ref platform,
			state, obj, TitleClick, out var titleClick);
		var requestedSortColumn = hasSortColumn ? sortColumn : 0u;
		value.SortColumn = requestedSortColumn >= MaximumColumns
			? MaximumColumns - 1 : requestedSortColumn;
		value.TitleClick = hasTitleClick ? titleClick : unchecked((uint)-1);

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
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			SortStateKey, block.Raw, false))
		{
			platform.Clear(block, MuiListSortState.Size);
			platform.Free(block, MuiListSortState.Size);
			return false;
		}
		return true;
	}

	private static bool ApplySortStateAttribute<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj,
		uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSortStateAdmission(ref platform, state, obj,
			out var sortState, out var present)) return false;
		if (!present && (!EnsureSortState(ref platform, state, obj) ||
			!TryReadSortStateAdmission(ref platform, state, obj,
				out sortState, out present) || !present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SortStateKey, 0));
		var previous = sortState;
		if (attribute == SortColumn)
		{
			if (!TryNormalizeSortColumn(ref platform, state, obj, value,
				out var normalized)) return false;
			sortState.SortColumn = normalized;
		}
		else if (attribute == TitleClick)
			sortState.TitleClick = value;
		else return false;
		if (!WriteSortState(ref platform, block, sortState)) return false;
		if (SetRaw(ref platform, state, record, attribute,
			attribute == SortColumn ? sortState.SortColumn : value, notify))
			return true;
		WriteSortState(ref platform, block, previous);
		return false;
	}

	private static bool TryNormalizeSortColumn<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value, out uint normalized)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		normalized = 0;
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || columns == 0) return false;
		normalized = value >= columns ? columns - 1 : value;
		return true;
	}

	private static bool SetSortColumnState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSortStateAdmission(ref platform, state, obj,
			out var sortState, out var present)) return false;
		if (!present && (!EnsureSortState(ref platform, state, obj) ||
			!TryReadSortStateAdmission(ref platform, state, obj,
				out sortState, out present) || !present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SortStateKey, 0));
		sortState.SortColumn = value;
		return WriteSortState(ref platform, block, sortState);
	}

	internal static bool TryGetSortState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListSortState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadSortStateAdmission(ref platform, state, obj,
			out value, out var present) && present;
	}

	private static bool TryGetSortColumnValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!TryReadSortStateAdmission(ref platform, state, obj,
			out var sort, out var present)) return false;
		value = present ? sort.SortColumn :
			ReadRaw(ref platform, state, obj, SortColumn, 0);
		return true;
	}

	private static uint SortColumnValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryGetSortColumnValue(ref platform, state, obj, out var value)
			? value : 0;
	}

	private static bool WriteSortState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListSortState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListSortStateCodec.Write(ref platform, block, value);
	}

	private static bool TryReadSortState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListSortState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListSortStateCodec.TryRead(ref platform, block, out value);
	}

	// A published sort record is authoritative guest state. A non-NULL record
	// that fails the cookie/field contract is malformed, not absence; sort and
	// title-click consumers must not replace it or fall back to raw projections.
	private static bool TryReadSortStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListSortState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			SortStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadSortState(ref platform, block, out value) &&
			IsValidSortState(value);
	}

	// SortColumn is a format-derived index. Construction and runtime format
	// normalization may later clamp it to the installed descriptor count, but a
	// published state must never carry a value outside the bounded MUI column
	// domain. Keep this check independent of the current format record so the
	// early construction window can still materialize and normalize the state.
	private static bool IsValidSortState(MuiListSortState value) =>
		value.SortColumn < MaximumColumns;

	private static bool WriteClickState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListClickState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListClickStateCodec.Write(ref platform, block, value);

	private static bool TryReadClickState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListClickState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListClickStateCodec.TryRead(ref platform, block, out value);

	// A published click record is authoritative guest state. A non-NULL record
	// that fails the cookie/field contract is malformed, not absence; click
	// getters and Listview forwarding must not replace it from raw projections.
	private static bool TryReadClickStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListClickState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ClickStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadClickState(ref platform, block, out value) &&
			IsValidClickState(value);
	}

	// Click columns and the click counter are signed/opaque public results, but
	// the two click flags are canonical BOOLs in the named record. Reject only
	// impossible flag values here so Listview forwarding cannot publish an
	// arbitrary scalar as a double- or repeat-click result.
	private static bool IsValidClickState(MuiListClickState value) =>
		value.DoubleClick <= 1 && value.AgainClick <= 1;

	internal static bool TryGetPoolPolicy<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListPoolPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPoolPolicyAdmission(ref platform, state, obj,
			out value, out var present)) return false;
		return present;
	}

	private static APTR PoolFor<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPoolPolicyAdmission(ref platform, state, obj,
			out var policy, out var present)) return APTR.Null;
		if (!present)
		{
			if (!EnsurePoolPolicy(ref platform, state, obj) ||
				!TryReadPoolPolicyAdmission(ref platform, state, obj,
					out policy, out present) || !present) return APTR.Null;
		}
		return policy.Pool;
	}

	// A published pool policy is authoritative guest state. A non-NULL record
	// that fails the cookie/field contract is malformed, not absence; callers
	// must not fall back to a raw pool pointer that could change ownership.
	private static bool TryReadPoolPolicyAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListPoolPolicyState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			PoolPolicyKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListPoolPolicyStateCodec.TryRead(ref platform, block,
			out value) && IsValidPoolPolicy(value);
	}

	// A published policy must always carry a usable opaque Exec pool handle. The
	// handle is deliberately not decoded as a host object or by guessed offsets;
	// the platform pool capability owns its representation. The ownership bit and
	// construction sizes remain named fields in the policy record, while a NULL
	// handle is rejected so hooks and pooled allocation cannot receive absence as a
	// live pool.
	private static bool IsValidPoolPolicy(MuiListPoolPolicyState value) =>
		value.Pool.IsNotNull && value.UsesExternalPool <= 1;

	// The format pointer remains caller-owned, as in the public MUI attribute;
	// only its bounded column count is derived into guest state. Empty and NULL
	// formats are the documented single-column default.
	private static void NormalizeFormat<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadFormatPolicyAdmission(ref platform, state, obj,
			out var policy, out var present)) return;
		if (!present && (!EnsureFormatPolicyState(ref platform, state, obj) ||
			!TryReadFormatPolicyAdmission(ref platform, state, obj,
				out policy, out present) || !present)) return;
		var maximum = policy.MaxColumns;
		var format = policy.Format;
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
		if (!TryReadTitleArrayAdmission(ref platform, state, obj,
			out _, out _)) return;
		var source = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			TitleArray, 0));
		if (!ApplyTitleArray(ref platform, state, record, obj, source, false))
			SetRaw(ref platform, state, record, TitleArray, 0, false);
	}

	private static bool ApplyTitle<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, APTR obj, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadTitleStateAdmission(ref platform, state, obj,
			out var title, out var present)) return false;
		if (!present && (!EnsureTitleState(ref platform, state, obj) ||
			!TryReadTitleStateAdmission(ref platform, state, obj,
				out title, out present) || !present)) return false;
		var previousRaw = title.Value;
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			Title, out var raw)) previousRaw = raw;
		if (!SetRaw(ref platform, state, record, Title, value, notify))
			return false;
		if (!SetTitleState(ref platform, state, obj, value))
		{
			SetRaw(ref platform, state, record, Title, previousRaw, false);
			return false;
		}
		return true;
	}

	private static bool ApplyTitleArray<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, APTR source, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// A published pointer-table record is authoritative ownership state. A
		// malformed present block is not absence and cannot be replaced by a raw
		// caller table during a runtime setter.
		if (!TryReadTitleArrayAdmission(ref platform, state, obj,
			out _, out _)) return false;
		var fresh = APTR.Null;
		if (source.IsNotNull)
		{
			fresh = BuildTitleArrayState(ref platform, source);
			if (fresh.IsNull) return false;
		}
		var raw = 0u;
		if (fresh.IsNotNull)
		{
			if (!TryReadTitleArrayStateBlock(ref platform, fresh,
				out var stateValue) ||
				!IsValidTitleArrayValues(ref platform, stateValue))
			{
				FreeTitleArrayState(ref platform, fresh);
				return false;
			}
			raw = stateValue.Pointers.Raw;
		}
		var previousRaw = ReadRaw(ref platform, state, obj, TitleArray, 0);
		if (!SetRaw(ref platform, state, record, TitleArray, raw, notify))
		{
			FreeTitleArrayState(ref platform, fresh);
			return false;
		}
		var old = APTR.FromPointer(Read(ref platform, state, obj,
			TitleArrayStateKey, 0));
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TitleArrayStateKey, fresh.Raw, false))
		{
			SetRaw(ref platform, state, record, TitleArray, previousRaw, false);
			FreeTitleArrayState(ref platform, fresh);
			return false;
		}
		FreeTitleArrayState(ref platform, old);
		return true;
	}

	private static void NormalizeColumnVisibility<TPlatform>(
		ref TPlatform platform, APTR state, APTR record, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadColumnVisibilityAdmission(ref platform, state, obj,
			out _, out _)) return;
		var hide = ReadRaw(ref platform, state, obj, HideColumn, uint.MaxValue);
		if (hide < MaximumGeometryColumns)
			ApplyColumnVisibility(ref platform, state, record, obj, hide, true,
				false);
		var show = ReadRaw(ref platform, state, obj, ShowColumn, uint.MaxValue);
		if (show < MaximumGeometryColumns)
			ApplyColumnVisibility(ref platform, state, record, obj, show, false,
				false);
	}

	private static bool ApplyColumnVisibility<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, uint column, bool hide, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (column >= MaximumGeometryColumns) return false;
		if (!TryReadColumnVisibilityAdmission(ref platform, state, obj,
			out var value, out var present)) return false;
		var block = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			ColumnVisibilityKey, 0));
		var fresh = !present;
		if (fresh)
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
		if (fresh && !MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ColumnVisibilityKey, block.Raw, false))
		{
			platform.Clear(block, MuiListColumnVisibilityState.Size);
			platform.Free(block, MuiListColumnVisibilityState.Size);
			return false;
		}
		if (!SetRaw(ref platform, state, record,
			hide ? HideColumn : ShowColumn, column, notify))
		{
			WriteColumnVisibilityState(ref platform, block, previous);
			if (fresh)
			{
				MuiHeadlessObjectCore.SetExistingAttribute(ref platform, state, obj,
					ColumnVisibilityKey, 0);
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
		if (!TryReadColumnOrderAdmission(ref platform, state, obj,
			out _, out _)) return;
		var source = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			ColumnOrder, 0));
		if (!ApplyColumnOrder(ref platform, state, record, obj, source, false))
			SetRaw(ref platform, state, record, ColumnOrder, 0, false);
	}

	private static bool ApplyColumnOrder<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, APTR source, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// A published permutation record owns the copied byte vector. A malformed
		// present block is not absence and cannot be replaced from the raw pointer
		// during a runtime setter.
		if (!TryReadColumnOrderAdmission(ref platform, state, obj,
			out _, out _)) return false;
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
		var previousRaw = ReadRaw(ref platform, state, obj, ColumnOrder, 0);
		if (!SetRaw(ref platform, state, record, ColumnOrder, raw, notify))
		{
			FreeColumnOrderState(ref platform, fresh);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ColumnOrderKey, fresh.Raw, false))
		{
			SetRaw(ref platform, state, record, ColumnOrder, previousRaw, false);
			FreeColumnOrderState(ref platform, fresh);
			return false;
		}
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
			if (!MuiListColumnOrderByteVectorCodec.TryReadValue(ref platform,
				leftCursor, out var leftByte) ||
				!MuiListColumnOrderByteVectorCodec.TryReadValue(ref platform,
					rightCursor, out var rightByte) ||
				leftByte != rightByte)
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
		var copied = 0u;
		var sourceCursor = default(MuiListColumnOrderByteCursor);
		sourceCursor.Base = source;
		for (var index = 0u; index < columns; index++)
		{
			sourceCursor.Index = index;
			if (!MuiListColumnOrderByteVectorCodec.TryReadValue(ref platform,
				sourceCursor, out var value)) return false;
			if (value == 0xFF) break;
			if (value >= columns || IsHidden(seen, value)) return false;
			Hide(ref seen, value);
			if (!WriteColumnOrderByte(ref platform, values, copied, value))
				return false;
			copied++;
		}
		for (var value = 0u; copied < columns && value < columns; value++)
			if (!IsHidden(seen, value))
			{
				Hide(ref seen, value);
				if (!WriteColumnOrderByte(ref platform, values, copied,
					unchecked((byte)value))) return false;
				copied++;
			}
		return copied == columns;
	}

	private static APTR BuildColumnOrderState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR source)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || columns == 0 || columns > MaximumGeometryColumns)
			return APTR.Null;
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
		if (!TryReadFontStateAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		if (!present && (!EnsureFontState(ref platform, state, obj) ||
			!TryReadFontStateAdmission(ref platform, state, obj,
				out _, out present) || !present)) return false;
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
		if (!TryReadFormatPolicyAdmission(ref platform, state, obj,
			out var policy, out var present)) return false;
		if (!present && (!EnsureFormatPolicyState(ref platform, state, obj) ||
			!TryReadFormatPolicyAdmission(ref platform, state, obj,
				out policy, out present) || !present)) return false;
		var maximum = policy.MaxColumns;
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
		if (!TryReadFormatPolicyAdmission(ref platform, state, obj,
			out var policy, out var present)) return false;
		if (!present && (!EnsureFormatPolicyState(ref platform, state, obj) ||
			!TryReadFormatPolicyAdmission(ref platform, state, obj,
				out policy, out present) || !present)) return false;
		var format = policy.Format;
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
		if (!TryReadSortStateAdmission(ref platform, state, obj,
			out var sortState, out var present)) return false;
		if (!present && (!EnsureSortState(ref platform, state, obj) ||
			!TryReadSortStateAdmission(ref platform, state, obj,
				out sortState, out present) || !present)) return false;
		if (!TryNormalizeSortColumn(ref platform, state, obj, value,
			out var normalized)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SortStateKey, 0));
		var previous = sortState.SortColumn;
		sortState.SortColumn = normalized;
		if (!WriteSortState(ref platform, block, sortState)) return false;
		if (SetRaw(ref platform, state, record, SortColumn, normalized, notify))
			return true;
		sortState.SortColumn = previous;
		WriteSortState(ref platform, block, sortState);
		return false;
	}

	// MorphOS Quiet suppresses intermediate refreshes and releases one
	// coalesced refresh when it is cleared. The pending bit and request count
	// live in the named guest redraw record rather than in private offsets.
	private static bool ApplyQuiet<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, APTR obj, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadRedrawStateAdmission(ref platform, state, obj,
			out var redraw, out var present) || !present) return false;
		var wasQuiet = PresentationPolicyValue(ref platform, state, obj,
			Quiet, 0) != 0;
		var nowQuiet = value != 0;
		if (!ApplyPresentationPolicyAttribute(ref platform, state, record, obj,
			Quiet, nowQuiet ? 1u : 0u, notify)) return false;
		if (!wasQuiet || nowQuiet) return true;
		if (redraw.Dirty == 0) return true;
		redraw.Dirty = 0;
		redraw.Requests = SaturatingAdd(redraw.Requests, 1);
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			RedrawStateKey, 0));
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
		// Once published, the guest-resident policy record is authoritative. A
		// non-NULL record that fails its cookie/field contract is malformed, not
		// an invitation to rebuild state from the raw compatibility projections.
		if (!TryReadPresentationPolicyAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		if (present) return true;
		var block = APTR.Null;
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
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			PresentationPolicyKey, block.Raw, false))
		{
			platform.Clear(block, MuiListPresentationPolicyState.Size);
			platform.Free(block, MuiListPresentationPolicyState.Size);
			return false;
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
		if (!TryReadPresentationPolicyAdmission(ref platform, state, obj,
			out var current, out var present)) return false;
		if (!present && (!EnsurePresentationPolicy(ref platform, state, obj) ||
			!TryReadPresentationPolicyAdmission(ref platform, state, obj,
				out current, out present) || !present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			PresentationPolicyKey, 0));
		var policy = current;
		var previous = policy;
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
		if (!WritePresentationPolicy(ref platform, block, policy)) return false;
		if (!SetRaw(ref platform, state, record, attribute, normalized, notify))
		{
			WritePresentationPolicy(ref platform, block, previous);
			return false;
		}
		return attribute != AutoLineHeight ||
			RefreshLineHeight(ref platform, state, obj);
	}

	internal static bool TryGetPresentationPolicy<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListPresentationPolicyState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadPresentationPolicyAdmission(ref platform, state, obj,
			out value, out var present) && present;
	}

	private static uint PresentationPolicyValue<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint attribute,
		uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPresentationPolicyAdmission(ref platform, state, obj,
			out var policy, out var present)) return 0;
		if (present)
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

	// A published presentation-policy record is authoritative guest state. A
	// non-NULL record that fails the cookie/field contract is malformed, not
	// absence; all policy consumers therefore fail closed instead of repairing
	// from the raw public projections.
	private static bool TryReadPresentationPolicyAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListPresentationPolicyState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			PresentationPolicyKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadPresentationPolicy(ref platform, block, out value) &&
			IsValidPresentationPolicy(value);
	}

	// Presentation values are normalized when the policy is published or
	// mutated. Keep that contract at the named record boundary so a damaged
	// guest field cannot make drawing, line-height, or drag policy consumers
	// interpret an arbitrary scalar as a valid MUI policy.
	private static bool IsValidPresentationPolicy(
		MuiListPresentationPolicyState value) =>
		value.Editable <= 1 && value.Quiet <= 1 &&
		value.AdjustHeight <= 1 && value.AdjustWidth <= 1 &&
		value.Stripes <= 1 && value.ShowDropMarks <= 1 &&
		value.DragSortable <= 1 &&
		(value.DragType == DragTypeNone ||
			value.DragType == DragTypeImmediate) &&
		value.AutoVisible <= 1 && value.AutoLineHeight <= 1 &&
		value.MinLineHeight >= RowHeight &&
		value.MinLineHeight <= MaximumLineHeight;

	private static bool WritePresentationPolicy<TPlatform>(ref TPlatform platform,
		APTR block, MuiListPresentationPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListPresentationPolicyStateCodec.Write(ref platform, block, value);
	}

	private static bool TryReadPresentationPolicy<TPlatform>(
		ref TPlatform platform, APTR block,
		out MuiListPresentationPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		return MuiListPresentationPolicyStateCodec.TryRead(ref platform, block,
			out value);
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
		var formatCursor = default(MuiListFormatByteCursor);
		formatCursor.Format = format;
		for (var i = 0u; i < length; i++)
		{
			formatCursor.Index = i;
			if (!MuiListFormatByteCursorCodec.TryReadByte(ref platform,
				formatCursor, out var value)) return false;
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
		// The named FORMAT policy is authoritative once published. A malformed
		// record is not absence: refuse to install a replacement descriptor set from raw
		// aliases, which would silently repair stale or corrupted guest state.
		if (!TryReadFormatProjectionAdmission(ref platform, state, obj,
			out _, out var present, out _, out _) || !present) return false;
		if (!TryCountFormatColumns(ref platform, format, maximum,
			out var count)) return false;
		var values = BuildFormatDescriptors(ref platform, format, count);
		if (values.IsNull) return false;
		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListFormatDescriptorState.Size);
		if (block.IsNull)
		{
			FreeFormatDescriptors(ref platform, values, count);
			return false;
		}
		var descriptorState = default(MuiListFormatDescriptorState);
		descriptorState.Magic = MuiListFormatDescriptorState.Cookie;
		descriptorState.Columns = count;
		descriptorState.Values = values;
		if (!MuiListFormatDescriptorStateCodec.Write(ref platform, block,
			descriptorState))
		{
			platform.Clear(block, MuiListFormatDescriptorState.Size);
			platform.Free(block, MuiListFormatDescriptorState.Size);
			FreeFormatDescriptors(ref platform, values, count);
			return false;
		}
		var old = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			FormatDescriptorKey, 0));
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
			FreeFormatDescriptorState(ref platform, block, true);
			return false;
		}
		SetFormatPolicyState(ref platform, state, obj, format, maximum, count);
		FreeFormatDescriptorState(ref platform, old);
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
			var value = default(MuiListFormatDescriptor);
			value.Delta = 4;
			value.Weight = 100;
			value.MinWidth = unchecked((uint)-1);
			value.MaxWidth = unchecked((uint)-1);
			value.Column = i;
			descriptorCursor.Index = i;
			if (!MuiListFormatDescriptorVectorCodec.TryWrite(ref platform,
				descriptorCursor, value))
			{
				FreeFormatDescriptors(ref platform, block, safeCount);
				return APTR.Null;
			}
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
		var formatCursor = default(MuiListFormatByteCursor);
		formatCursor.Format = format;
		for (var i = 0u; i <= length && ordinal < safeCount; i++)
		{
			var separator = i == length;
			if (!separator)
			{
				formatCursor.Index = i;
				if (!MuiListFormatByteCursorCodec.TryReadByte(ref platform,
					formatCursor, out var separatorByte))
				{
					FreeFormatDescriptors(ref platform, block, safeCount);
					return APTR.Null;
				}
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
			var value = default(MuiListFormatDescriptor);
			descriptorCursor.Index = ordinal;
			if (!MuiListFormatDescriptorVectorCodec.TryRead(ref platform,
				descriptorCursor, out value))
			{
				FreeFormatDescriptors(ref platform, block, safeCount);
				return APTR.Null;
			}
			if (!ParseFormatSegment(ref platform, format, start, (int)i,
				ref value, ordinal))
			{
				ReleaseFormatDescriptorValue(ref platform, ref value);
				FreeFormatDescriptors(ref platform, block, safeCount);
				return APTR.Null;
			}
			if (!MuiListFormatDescriptorVectorCodec.TryWrite(ref platform,
				descriptorCursor, value))
			{
				ReleaseFormatDescriptorValue(ref platform, ref value);
				FreeFormatDescriptors(ref platform, block, safeCount);
				return APTR.Null;
			}
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
		var currentCursor = default(MuiListFormatDescriptorCursor);
		currentCursor.Base = block;
		var previousCursor = default(MuiListFormatDescriptorCursor);
		previousCursor.Base = block;
		for (var current = 0u; current < count; current++)
		{
			currentCursor.Index = current;
			if (!MuiListFormatDescriptorVectorCodec.TryRead(ref platform,
				currentCursor, out var currentDescriptor)) return false;
			for (var previous = 0u; previous < current; previous++)
			{
				previousCursor.Index = previous;
				if (!MuiListFormatDescriptorVectorCodec.TryRead(ref platform,
					previousCursor, out var previousDescriptor)) return false;
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
			while (cursor < end)
			{
				if (!TryReadFormatByte(ref platform, format, cursor,
					out var current)) return false;
				if (!IsSpace(current)) break;
				cursor++;
			}
			if (cursor >= end) break;
			var keyEnd = cursor;
			while (keyEnd < end)
			{
				if (!TryReadFormatByte(ref platform, format, keyEnd,
					out var keyByte)) return false;
				if (IsSpace(keyByte) || keyByte == (byte)'=') break;
				keyEnd++;
			}
			if (keyEnd == cursor) return false;
			var afterKey = keyEnd;
			while (afterKey < end)
			{
				if (!TryReadFormatByte(ref platform, format, afterKey,
					out var separatorByte)) return false;
				if (!IsSpace(separatorByte)) break;
				afterKey++;
			}
			var hasValue = false;
			var valueStart = afterKey;
			var valueEnd = afterKey;
			var quotedValue = false;
			if (afterKey < end)
			{
				if (!TryReadFormatByte(ref platform, format, afterKey,
					out var equalsByte)) return false;
				if (equalsByte == (byte)'=')
				{
				hasValue = true;
				valueStart = afterKey + 1;
				while (valueStart < end)
				{
					if (!TryReadFormatByte(ref platform, format, valueStart,
						out var valueSpace)) return false;
					if (!IsSpace(valueSpace)) break;
					valueStart++;
				}
				if (!TryReadFormatValueRange(ref platform, format, valueStart,
					end, out valueEnd, out quotedValue)) return false;
				}
			}
			if (!hasValue && !IsFormatSwitch(ref platform, format, cursor, keyEnd))
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
		if (!TryReadFormatByte(ref platform, text, start, out var first))
			return false;
		if (first == (byte)'"')
		{
			quoted = true;
			var cursor = start + 1;
			while (cursor < end)
			{
				if (!TryReadFormatByte(ref platform, text, cursor,
					out var value)) return false;
				cursor++;
				if (value == (byte)'*')
				{
					if (cursor >= end) return false;
					if (!TryReadFormatByte(ref platform, text, cursor,
						out _)) return false;
					cursor++;
					continue;
				}
				if (value != (byte)'"') continue;
				if (cursor < end)
				{
					if (!TryReadFormatByte(ref platform, text, cursor,
						out var trailing)) return false;
					if (!IsSpace(trailing)) return false;
				}
				valueEnd = cursor;
				return true;
			}
			return false;
		}
		var unquoted = start;
		while (unquoted < end)
		{
			if (!TryReadFormatByte(ref platform, text, unquoted,
				out var current)) return false;
			if (IsSpace(current)) break;
			if (current == (byte)'"') return false;
			unquoted++;
		}
		if (unquoted == start) return false;
		valueEnd = unquoted;
		return true;
	}

	private static bool IsSpace(byte value) => value == (byte)' ' ||
		value == (byte)'\t';

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryReadFormatByte<TPlatform>(ref TPlatform platform,
		APTR format, int index, out byte value)
		where TPlatform : struct, IMuiGuestMemory =>
		MuiListFormatByteCursorCodec.TryReadAt(ref platform, format, index,
			out value);

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

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool TryReadGuestLongLower<TPlatform>(ref TPlatform platform,
		APTR address, int offset, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadFormatByte(ref platform, address, offset, out var first) ||
			!TryReadFormatByte(ref platform, address, offset + 1, out var second) ||
			!TryReadFormatByte(ref platform, address, offset + 2, out var third) ||
			!TryReadFormatByte(ref platform, address, offset + 3, out var fourth))
			return false;
		value = ((uint)Lower(first) << 24) |
			((uint)Lower(second) << 16) |
			((uint)Lower(third) << 8) |
			Lower(fourth);
		return true;
	}

	// The descriptor wire format is a fixed sequence of big-endian ULONGs.
	// Keeping this marshalling in one place lets all format/layout code use the
	// named record above instead of repeating guest offsets.
	internal static void ReadFormatDescriptor<TPlatform>(ref TPlatform platform,
		APTR address, out MuiListFormatDescriptor value)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiListFormatDescriptorCodec.TryRead(ref platform, address, out value);
	}

	internal static void WriteFormatDescriptor<TPlatform>(ref TPlatform platform,
		APTR address, ref MuiListFormatDescriptor value)
		where TPlatform : struct, IMuiGuestMemory
	{
		MuiListFormatDescriptorCodec.TryWrite(ref platform, address, value);
	}

	private static bool TryReadCStringLength<TPlatform>(ref TPlatform platform,
		APTR value, uint maximumLength, out uint length)
		where TPlatform : struct, IMuiGuestMemory
	{
		length = 0;
		if (value.IsNull || maximumLength == 0 || maximumLength >
			MuiListFormatByteCursor.MaximumLength) return false;
		var cursor = default(MuiListFormatByteCursor);
		cursor.Format = value;
		for (var index = 0u; index < maximumLength; index++)
		{
			cursor.Index = index;
			if (!MuiListFormatByteCursorCodec.TryReadByte(ref platform, cursor,
				out var current)) return false;
			if (current != 0) continue;
			length = index;
			return true;
		}
		return false;
	}

	private static bool IsToken3<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, byte a, byte b, byte c)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (end - start != 3) return false;
		return TryReadFormatByte(ref platform, text, start, out var first) &&
			TryReadFormatByte(ref platform, text, start + 1, out var second) &&
			TryReadFormatByte(ref platform, text, start + 2, out var third) &&
			Lower(first) == Lower(a) && Lower(second) == Lower(b) &&
			Lower(third) == Lower(c);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsToken4<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, byte a, byte b, byte c, byte d)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (end - start != 4) return false;
		return TryReadFormatByte(ref platform, text, start, out var first) &&
			TryReadFormatByte(ref platform, text, start + 1, out var second) &&
			TryReadFormatByte(ref platform, text, start + 2, out var third) &&
			TryReadFormatByte(ref platform, text, start + 3, out var fourth) &&
			Lower(first) == Lower(a) && Lower(second) == Lower(b) &&
			Lower(third) == Lower(c) && Lower(fourth) == Lower(d);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsToken1<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, byte a)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (end - start != 1) return false;
		return TryReadFormatByte(ref platform, text, start, out var value) &&
			Lower(value) == Lower(a);
	}

	private static bool IsToken5<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, byte a, byte b, byte c, byte d, byte e)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (end - start != 5) return false;
		return TryReadFormatByte(ref platform, text, start, out var first) &&
			TryReadFormatByte(ref platform, text, start + 1, out var second) &&
			TryReadFormatByte(ref platform, text, start + 2, out var third) &&
			TryReadFormatByte(ref platform, text, start + 3, out var fourth) &&
			TryReadFormatByte(ref platform, text, start + 4, out var fifth) &&
		Lower(first) == Lower(a) && Lower(second) == Lower(b) &&
			Lower(third) == Lower(c) && Lower(fourth) == Lower(d) &&
			Lower(fifth) == Lower(e);
	}

	private static bool IsToken6<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, byte a, byte b, byte c, byte d, byte e, byte f)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (end - start != 6) return false;
		return TryReadFormatByte(ref platform, text, start, out var first) &&
			TryReadFormatByte(ref platform, text, start + 1, out var second) &&
			TryReadFormatByte(ref platform, text, start + 2, out var third) &&
			TryReadFormatByte(ref platform, text, start + 3, out var fourth) &&
			TryReadFormatByte(ref platform, text, start + 4, out var fifth) &&
			TryReadFormatByte(ref platform, text, start + 5, out var sixth) &&
			Lower(first) == Lower(a) && Lower(second) == Lower(b) &&
			Lower(third) == Lower(c) && Lower(fourth) == Lower(d) &&
			Lower(fifth) == Lower(e) && Lower(sixth) == Lower(f);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool IsToken8<TPlatform>(ref TPlatform platform, APTR text,
		int start, int end, uint first, uint second)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (end - start != 8) return false;
		return TryReadGuestLongLower(ref platform, text, start, out var firstValue) &&
			TryReadGuestLongLower(ref platform, text, start + 4,
				out var secondValue) && firstValue == first &&
			secondValue == second;
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
		where TPlatform : struct, IMuiGuestMemory
	{
		if (end - start != 4) return false;
		return TryReadFormatByte(ref platform, text, start, out var first) &&
			TryReadFormatByte(ref platform, text, start + 1, out var second) &&
			TryReadFormatByte(ref platform, text, start + 2, out var third) &&
			TryReadFormatByte(ref platform, text, start + 3, out var fourth) &&
			Lower(first) == Lower(a) && Lower(second) == Lower(b) &&
			Lower(third) == Lower(c) && Lower(fourth) == Lower(d);
	}

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
		if (!TryReadFormatByte(ref platform, text, sourceCursor,
			out var current)) return false;
		sourceCursor++;
		if (value.Quoted == 0 || current != (byte)'*')
		{
			decoded = current;
			return true;
		}
		if (sourceCursor >= value.End) return false;
		if (!TryReadFormatByte(ref platform, text, sourceCursor,
			out current)) return false;
		sourceCursor++;
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
		var destinationCursor = default(MuiListPreparseByteCursor);
		destinationCursor.Base = storage;
		destinationCursor.Capacity = bytes;
		while (output < value.DecodedLength)
		{
			if (!TryReadDecodedByte(ref platform, text, ref value,
				ref sourceCursor, out var decoded))
			{
				platform.Clear(storage, bytes);
				platform.Free(storage, bytes);
				return false;
			}
			destinationCursor.Index = output;
			if (!MuiListPreparseByteCursorCodec.TryWriteByte(ref platform,
				destinationCursor, decoded))
			{
				platform.Clear(storage, bytes);
				platform.Free(storage, bytes);
				return false;
			}
			output++;
		}
		destinationCursor.Index = output;
		if (!MuiListPreparseByteCursorCodec.TryWriteByte(ref platform,
			destinationCursor, 0))
		{
			platform.Clear(storage, bytes);
			platform.Free(storage, bytes);
			return false;
		}
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
				if (!MuiListFormatDescriptorVectorCodec.TryRead(ref platform, cursor,
					out var value)) continue;
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

	private static void FreeFormatDescriptorState<TPlatform>(
		ref TPlatform platform, APTR block, bool allowMalformed = false,
		uint boundedCount = 0)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull) return;
		if (!allowMalformed &&
			(!MuiListFormatDescriptorStateCodec.TryRead(ref platform, block,
				out _) || !platform.IsMapped(block,
				MuiListFormatDescriptorState.Size))) return;
		if (!MuiListFormatDescriptorStateCodec.TryReadStorage(ref platform,
			block, out var value))
		{
			if (platform.IsMapped(block, MuiListFormatDescriptorState.Size))
			{
				platform.Clear(block, MuiListFormatDescriptorState.Size);
				platform.Free(block, MuiListFormatDescriptorState.Size);
			}
			return;
		}
		// During teardown a valid FORMAT policy is a second named owner of the
		// vector cardinality. Prefer that bounded count when the descriptor record
		// is structurally readable but disagrees with the policy; otherwise a
		// malformed count could make cleanup walk into an adjacent guest block.
		var count = boundedCount != 0 && boundedCount <= MaximumColumns
			? boundedCount : value.Columns;
		var bytes = count * MuiListFormatDescriptor.Size;
		if (platform.IsMapped(value.Values, bytes))
			FreeFormatDescriptors(ref platform, value.Values, count);
		if (platform.IsMapped(block, MuiListFormatDescriptorState.Size))
		{
			platform.Clear(block, MuiListFormatDescriptorState.Size);
			platform.Free(block, MuiListFormatDescriptorState.Size);
		}
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
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform,
				sourceCursor, out var sourceValue) ||
				!MuiListPointerSlotVectorCodec.TryWrite(ref platform,
					destinationCursor, sourceValue))
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
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform, cursor,
				out var value)) return uint.MaxValue;
			if (value.Value.IsNull)
				return column;
		}
		return uint.MaxValue;
	}

	private static bool WriteTitleArrayState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListTitleArrayState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListTitleArrayStateCodec.Write(ref platform, block, value);

	private static bool WriteTitleState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListTitleState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListTitleStateCodec.Write(ref platform, block, value);

	private static bool TryReadTitleState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListTitleState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListTitleStateCodec.TryRead(ref platform, block, out value);

	// A published title record is authoritative guest state. A non-NULL record
	// that fails the cookie/field contract is malformed, not absence; title
	// getters and drawing cursors must not replace it or fall back to the raw
	// compatibility scalar.
	private static bool TryReadTitleStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListTitleState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			TitleStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadTitleState(ref platform, block, out value) &&
			IsValidTitleValue(ref platform, value);
	}

	// MUIA_List_Title accepts caller-owned text, NULL, or TRUE for the custom
	// title-hook form. Admit the named value only when a non-sentinel pointer is
	// a bounded mapped guest C string; title-row drawing must not dereference a
	// stale compatibility pointer.
	private static bool IsValidTitleValue<TPlatform>(ref TPlatform platform,
		MuiListTitleState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (value.Value == 0 || value.Value == 1) return true;
		return TryReadCStringLength(ref platform,
			APTR.FromPointer(value.Value), MaximumStringLength, out _);
	}

	private static bool EnsureTitleState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadTitleStateAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			TitleStateKey, 0));
		if (present) return true;
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
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			TitleStateKey, block.Raw, false))
		{
			platform.Clear(block, MuiListTitleState.Size);
			platform.Free(block, MuiListTitleState.Size);
			return false;
		}
		return true;
	}

	private static bool SetTitleState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadTitleStateAdmission(ref platform, state, obj,
			out var title, out var present)) return false;
		if (!present)
		{
			if (!EnsureTitleState(ref platform, state, obj) ||
				!TryReadTitleStateAdmission(ref platform, state, obj,
					out title, out present) || !present) return false;
		}
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			TitleStateKey, 0));
		title.Value = value;
		return WriteTitleState(ref platform, block, title);
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
		=> MuiListSelectionSignalStateCodec.Write(ref platform, block, value);

	private static bool TryReadSelectionSignalState<TPlatform>(
		ref TPlatform platform, APTR block,
		out MuiListSelectionSignalState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListSelectionSignalStateCodec.TryRead(ref platform, block, out value);

	private static bool IsValidSelectionSignalState(
		MuiListSelectionSignalState value) => value.Value <= 1;

	// A published selection signal is authoritative guest state.  A non-NULL
	// record that fails the cookie/field contract is malformed, not absence;
	// selection transitions must not repair it from the raw signal alias.
	private static bool TryReadSelectionSignalAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListSelectionSignalState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			SelectionSignalKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadSelectionSignalState(ref platform, block, out value) &&
			IsValidSelectionSignalState(value);
	}

	private static bool EnsureSelectionSignalState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSelectionSignalAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		if (present) return true;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListSelectionSignalState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListSelectionSignalState);
		value.Magic = SelectionSignalCookie;
		value.Value = ReadRaw(ref platform, state, obj, SelectChange, 0);
		if (!WriteSelectionSignalState(ref platform, block, value))
		{
			platform.Clear(block, MuiListSelectionSignalState.Size);
			platform.Free(block, MuiListSelectionSignalState.Size);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			SelectionSignalKey, block.Raw, false))
		{
			platform.Clear(block, MuiListSelectionSignalState.Size);
			platform.Free(block, MuiListSelectionSignalState.Size);
			return false;
		}
		return true;
	}

	private static bool SetSelectionSignalState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSelectionSignalAdmission(ref platform, state, obj,
			out var signal, out var present) || !present) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		signal.Value = value;
		return WriteSelectionSignalState(ref platform, block, signal);
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
		=> MuiListFormatPolicyStateCodec.Write(ref platform, block, value);

	private static bool TryReadFormatPolicyState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListFormatPolicyState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListFormatPolicyStateCodec.TryRead(ref platform, block, out value);

	// A published FORMAT policy is authoritative guest state. A missing key is
	// the only state that may be bootstrapped from the raw FORMAT/MaxColumns
	// aliases; a non-NULL malformed block is rejected so descriptor/layout code
	// cannot repair it by silently rebuilding a second policy record.
	private static bool TryReadFormatPolicyAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListFormatPolicyState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			FormatPolicyKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadFormatPolicyState(ref platform, block, out value) &&
			IsValidFormatPolicy(ref platform, value);
	}

	// FORMAT remains caller-owned text, but a published pointer must still name
	// a bounded, mapped guest C string before descriptor, layout, or display code
	// can consume the named policy. NULL is the documented empty-format form.
	private static bool IsValidFormatPolicy<TPlatform>(ref TPlatform platform,
		MuiListFormatPolicyState value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.MaxColumns != 0 && value.MaxColumns <= MaximumColumns &&
		value.Columns != 0 && value.Columns <= value.MaxColumns &&
		(value.Format.IsNull || TryReadCStringLength(ref platform, value.Format,
			MaximumStringLength, out _));

	private static bool EnsureFormatPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadFormatPolicyAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		if (present) return true;
		var block = APTR.Null;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListFormatPolicyState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListFormatPolicyState);
		value.Magic = FormatPolicyCookie;
		value.Format = APTR.FromPointer(ReadRaw(ref platform, state, obj, Format, 0));
		value.MaxColumns = NormalizeColumnLimit(ReadRaw(ref platform, state, obj,
			MaxColumns, DefaultMaxColumns));
		value.Columns = ReadRaw(ref platform, state, obj, FormatColumnsKey, 1);
		if (!WriteFormatPolicyState(ref platform, block, value))
		{
			platform.Clear(block, MuiListFormatPolicyState.Size);
			platform.Free(block, MuiListFormatPolicyState.Size);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			FormatPolicyKey, block.Raw, false))
		{
			platform.Clear(block, MuiListFormatPolicyState.Size);
			platform.Free(block, MuiListFormatPolicyState.Size);
			return false;
		}
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
		=> MuiListFontStateCodec.Write(ref platform, block, value);

	private static bool TryReadFontState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListFontState value)
		where TPlatform : struct, IMuiHeadlessPlatform
		=> MuiListFontStateCodec.TryRead(ref platform, block, out value);

	// A published Font record is authoritative caller-owned pointer state. A
	// missing key may be bootstrapped from the raw Font alias; a non-NULL block
	// that fails its cookie/field contract is malformed and must fail closed.
	private static bool TryReadFontStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListFontState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			FontStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadFontState(ref platform, block, out value) &&
			IsValidFontState(ref platform, value);
	}

	// MUIA_Font is a borrowed TextFont pointer. NULL means inherited font; an
	// explicit value must at least cover the named guest TextFont structure so
	// measurement and drawing never carry an unmapped pointer into graphics.
	private static bool IsValidFontState<TPlatform>(ref TPlatform platform,
		MuiListFontState value)
		where TPlatform : struct, IMuiGuestMemory =>
		value.Font.IsNull || platform.IsMapped(value.Font, TextFont.Size);

	private static bool EnsureFontState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadFontStateAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		if (present) return true;
		var block = APTR.Null;
		block = MuiHeadlessMemory.Allocate(ref platform, MuiListFontState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListFontState);
		value.Magic = FontStateCookie;
		value.Font = APTR.FromPointer(ReadRaw(ref platform, state, obj, Font, 0));
		if (!WriteFontState(ref platform, block, value))
		{
			platform.Clear(block, MuiListFontState.Size);
			platform.Free(block, MuiListFontState.Size);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			FontStateKey, block.Raw, false))
		{
			platform.Clear(block, MuiListFontState.Size);
			platform.Free(block, MuiListFontState.Size);
			return false;
		}
		return true;
	}

	private static void SetFontState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR font)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadFontStateAdmission(ref platform, state, obj,
			out var value, out var present) || !present) return;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			FontStateKey, 0));
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
		=> MuiListTitleArrayStateCodec.TryRead(ref platform, block, out value);

	// Cleanup and admission share the bounded pointer-table validation, but only
	// the admission reader requires the cookie. This lets teardown retire a
	// record whose magic was corrupted while still rejecting arbitrary pointer
	// and count values before freeing guest memory.
	private static bool TryReadTitleArrayStorage<TPlatform>(
		ref TPlatform platform, APTR block, out MuiListTitleArrayState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListTitleArrayStateCodec.TryReadStorage(ref platform, block, out value);

	private static bool TryReadTitleArrayAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListTitleArrayState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			TitleArrayStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadTitleArrayStateBlock(ref platform, block, out value) &&
			IsValidTitleArrayValues(ref platform, value);
	}

	// The private TitleArray table owns copied pointers, not the strings they
	// reference. Admission therefore validates every named slot as a bounded
	// guest C string and requires the explicit terminator slot to remain NULL.
	// The storage reader stays structural so teardown can still free the table
	// after a caller corrupts one of its borrowed pointers.
	private static bool IsValidTitleArrayValues<TPlatform>(
		ref TPlatform platform, MuiListTitleArrayState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListPointerSlotCursor);
		cursor.Base = value.Pointers;
		for (var column = 0u; column < value.Count; column++)
		{
			cursor.Index = column;
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform, cursor,
				out var entry) || entry.Value.IsNull ||
				!TryReadCStringLength(ref platform, entry.Value,
					MaximumStringLength, out _)) return false;
		}
		cursor.Index = value.Count;
		if (!MuiListPointerSlotVectorCodec.TryRead(ref platform, cursor,
			out var terminatorValue)) return false;
		return terminatorValue.Value.IsNull;
	}

	private static void FreeTitleArrayState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListTitleArrayState.Size))
			return;
		if (TryReadTitleArrayStorage(ref platform, block, out var value))
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
		MuiListRedrawStateCodec.Write(ref platform, block, value);
	}

	private static bool TryReadRedrawState<TPlatform>(ref TPlatform platform,
		APTR block, out MuiListRedrawState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListRedrawStateCodec.TryRead(ref platform, block, out value);

	private static bool IsValidRedrawState(MuiListRedrawState value) =>
		value.Dirty <= 1;

	// A published redraw record is authoritative coalescing state. A non-NULL
	// record that fails its cookie/field contract is malformed, not absence;
	// quiet transitions and redraw scheduling must fail before changing policy
	// or issuing a platform redraw side effect.
	private static bool TryReadRedrawStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListRedrawState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			RedrawStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadRedrawState(ref platform, block, out value) &&
			IsValidRedrawState(value);
	}

	private static void WriteColumnVisibilityState<TPlatform>(
		ref TPlatform platform, APTR block, MuiListColumnVisibilityState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListColumnVisibilityStateCodec.Write(ref platform, block, value);

	private static bool TryReadColumnVisibilityState<TPlatform>(
		ref TPlatform platform, APTR block,
		out MuiListColumnVisibilityState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListColumnVisibilityStateCodec.TryRead(ref platform, block, out value);

	// A published metrics record is authoritative derived guest state. A missing
	// key may be materialized by Layout; a non-NULL malformed record is rejected
	// so refresh and geometry cannot silently replace it from an untrusted block.
	private static bool TryReadColumnMetricsAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListColumnMetricsState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ColumnMetricsKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListColumnMetricsStateCodec.TryRead(ref platform, block,
			out value) && IsValidColumnMetricsState(value);
	}

	// Width is published from the signed host layout rectangle.  Keep the
	// storage reader permissive for teardown, but do not let a post-publication
	// UINT_MAX (or any value above INT_MAX) become an admitted derived metric.
	private static bool IsValidColumnMetricsState(
		MuiListColumnMetricsState value) => value.Width <= int.MaxValue;

	// A published visibility mask is authoritative guest state. A missing key
	// may be materialized by a Hide/Show setter; a non-NULL malformed record is
	// rejected so layout and mutation cannot silently replace its mask.
	private static bool TryReadColumnVisibilityAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListColumnVisibilityState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ColumnVisibilityKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadColumnVisibilityState(ref platform, block, out value);
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
		=> MuiListColumnOrderStateCodec.Write(ref platform, block, value);

	private static bool TryReadColumnOrderState<TPlatform>(
		ref TPlatform platform, APTR block,
		out MuiListColumnOrderState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListColumnOrderStateCodec.TryRead(ref platform, block, out value);

	// Teardown and admission share bounded vector validation, while only the
	// admission reader requires the cookie. A corrupted magic word therefore
	// cannot leak the owned byte vector, yet arbitrary count/pointer values are
	// still rejected before cleanup frees guest memory.
	private static bool TryReadColumnOrderStorage<TPlatform>(
		ref TPlatform platform, APTR block,
		out MuiListColumnOrderState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListColumnOrderStateCodec.TryReadStorage(ref platform, block, out value);

	private static bool TryReadColumnOrderAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListColumnOrderState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ColumnOrderKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadColumnOrderState(ref platform, block, out value) &&
			IsValidColumnOrderValues(ref platform, value);
	}

	// The persisted BYTE* vector is a permutation, not merely a bounded byte
	// array. Keep this semantic check separate from the storage reader so
	// teardown can still recover and free the owned vector when its contents are
	// damaged. Admission rejects duplicate or out-of-range display columns while
	// retaining the named record and raw projection for diagnosis and cleanup.
	private static bool IsValidColumnOrderValues<TPlatform>(
		ref TPlatform platform, MuiListColumnOrderState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var seen = default(MuiListHiddenColumns);
		var cursor = default(MuiListColumnOrderByteCursor);
		cursor.Base = value.Values;
		for (var index = 0u; index < value.Count; index++)
		{
			cursor.Index = index;
			if (!MuiListColumnOrderByteVectorCodec.TryReadValue(ref platform,
				cursor, out var column)) return false;
			if (column >= value.Count || IsHidden(seen, column)) return false;
			Hide(ref seen, column);
		}
		return true;
	}

	private static void FreeColumnOrderState<TPlatform>(
		ref TPlatform platform, APTR block)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block,
			MuiListColumnOrderState.Size)) return;
		if (TryReadColumnOrderStorage(ref platform, block, out var value))
		{
			var values = value.Values;
			ClearColumnOrderBytes(ref platform, values, value.Reserved);
			platform.Free(values, value.Reserved);
		}
		platform.Clear(block, MuiListColumnOrderState.Size);
		platform.Free(block, MuiListColumnOrderState.Size);
	}

	private static void ClearColumnOrderBytes<TPlatform>(ref TPlatform platform,
		APTR values, uint count) where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListColumnOrderByteCursor);
		cursor.Base = values;
		for (var index = 0u; index < count; index++)
		{
			cursor.Index = index;
			if (!MuiListColumnOrderByteVectorCodec.TryWriteValue(ref platform,
				cursor, 0)) return;
		}
	}

	private static uint ColumnOrderValueBytes(uint columns) =>
		(columns + 3u) & ~3u;

	private static bool WriteColumnOrderByte<TPlatform>(ref TPlatform platform,
		APTR values, uint index, byte value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiListColumnOrderByteCursor);
		cursor.Base = values;
		cursor.Index = index;
		return MuiListColumnOrderByteVectorCodec.TryWriteValue(ref platform,
			cursor, value);
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
		=> MuiListActiveStateCodec.TryRead(ref platform, block, out value);

	private static bool IsValidActiveState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiListActiveState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (value.HasActive > 1) return false;
		if (value.HasActive == 0) return true;
		var count = EntryCount(ref platform, state, obj);
		return count != 0 && value.Active < count;
	}

	// A published active cursor is authoritative guest state. A non-NULL
	// record that fails the cookie/field contract is malformed, not absence;
	// active-row consumers must not repair it from the raw Active scalar.
	private static bool TryReadActiveStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListActiveState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ActiveStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadActiveState(ref platform, block, out value) &&
			IsValidActiveState(ref platform, state, obj, value);
	}

	private static bool WriteActiveState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListActiveState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListActiveStateCodec.Write(ref platform, block, value);

	private static bool EnsureActiveState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadActiveStateAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ActiveStateKey, 0));
		if (present) return true;
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
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			ActiveStateKey, block.Raw, false))
		{
			platform.Clear(block, MuiListActiveState.Size);
			platform.Free(block, MuiListActiveState.Size);
			return false;
		}
		return true;
	}

	private static void SetActiveCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint active, bool hasActive)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadActiveStateAdmission(ref platform, state, obj,
			out var value, out var present) || !present) return;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ActiveStateKey, 0));
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

	private static bool WriteInsertPositionState<TPlatform>(
		ref TPlatform platform, APTR block, MuiListInsertPositionState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListInsertPositionStateCodec.Write(ref platform, block, value);

	private static bool TryReadInsertPositionState<TPlatform>(
		ref TPlatform platform, APTR block,
		out MuiListInsertPositionState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListInsertPositionStateCodec.TryRead(ref platform, block, out value);

	// A published insertion result is authoritative guest state. A non-NULL
	// record that fails the cookie/field contract is malformed, not absence;
	// the getter must not replace it or fall back to the raw compatibility scalar.
	private static bool TryReadInsertPositionAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListInsertPositionState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			InsertPositionStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadInsertPositionState(ref platform, block, out value) &&
			IsValidInsertPositionState(value);
	}

	// InsertPosition is the zero-based result of a successful insertion. The
	// list growth path is bounded by MaximumEntries, so preserve the neutral
	// initial value and any stale last-result value after Clear while rejecting
	// an impossible unsigned projection at the named record boundary.
	private static bool IsValidInsertPositionState(
		MuiListInsertPositionState value) => value.Position < MaximumEntries;

	private static bool EnsureInsertPositionState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadInsertPositionAdmission(ref platform, state, obj,
			out _, out var present)) return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			InsertPositionStateKey, 0));
		if (present) return true;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListInsertPositionState.Size);
		if (block.IsNull) return false;
		var value = default(MuiListInsertPositionState);
		value.Magic = MuiListInsertPositionState.Cookie;
		value.Position = ReadRaw(ref platform, state, obj, InsertPosition, 0);
		if (!WriteInsertPositionState(ref platform, block, value))
		{
			platform.Clear(block, MuiListInsertPositionState.Size);
			platform.Free(block, MuiListInsertPositionState.Size);
			return false;
		}
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			InsertPositionStateKey, block.Raw, false))
		{
			platform.Clear(block, MuiListInsertPositionState.Size);
			platform.Free(block, MuiListInsertPositionState.Size);
			return false;
		}
		return true;
	}

	internal static bool TryGetInsertPositionState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListInsertPositionState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadInsertPositionAdmission(ref platform, state, obj,
			out value, out var present) && present;
	}

	private static void SetInsertPosition<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint position)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadInsertPositionAdmission(ref platform, state, obj,
			out var current, out var present) || !present) return;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			InsertPositionStateKey, 0));
		current.Position = position;
		if (!WriteInsertPositionState(ref platform, block, current)) return;
		SetInternal(ref platform, state, obj, InsertPosition, position);
	}

	private static void FreeInsertPositionState<TPlatform>(
		ref TPlatform platform, APTR block)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block,
			MuiListInsertPositionState.Size)) return;
		platform.Clear(block, MuiListInsertPositionState.Size);
		platform.Free(block, MuiListInsertPositionState.Size);
	}

	private static bool WriteViewportState<TPlatform>(ref TPlatform platform,
		APTR block, MuiListViewportState value)
		where TPlatform : struct, IMuiGuestMemory
		=> MuiListViewportStateCodec.Write(ref platform, block, value);

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
		=> MuiListViewportStateCodec.TryRead(ref platform, block, out value);

	private static bool IsValidViewportState(MuiListViewportState value) =>
		value.LineHeight != 0;

	// A published viewport record is authoritative derived state. A non-NULL
	// record that fails the cookie/field contract is malformed, not absence;
	// layout and cursor consumers must not replace it or fall back to raw row
	// attributes while it is present.
	private static bool TryReadViewportStateAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListViewportState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ViewportStateKey, out var rawBlock)) return true;
		var block = APTR.FromPointer(rawBlock);
		present = block.IsNotNull;
		if (!present) return true;
		return TryReadViewportState(ref platform, block, out value) &&
			IsValidViewportState(value);
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
		return TryReadFormatProjectionAdmission(ref platform, state, obj,
			out value, out var present, out _, out _) && present;
	}

	internal static bool TryGetFormatDescriptorState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListFormatDescriptorState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadFormatProjectionAdmission(ref platform, state, obj,
			out _, out var policyPresent, out value, out var present) &&
			policyPresent && present;
	}

	internal static bool TryGetFontState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListFontState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadFontStateAdmission(ref platform, state, obj,
			out value, out var present) && present;
	}

	internal static bool TryGetColumnMetricsState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListColumnMetricsState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadColumnMetricsAdmission(ref platform, state, obj,
			out value, out var present) && present;
	}

	private static APTR FontCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadFontStateAdmission(ref platform, state, obj,
			out var value, out var present)) return APTR.Null;
		return present ? value.Font :
			APTR.FromPointer(ReadRaw(ref platform, state, obj, Font, 0));
	}

	private static APTR FormatValueCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadFormatProjectionAdmission(ref platform, state, obj,
			out var value, out var present, out _, out _)) return APTR.Null;
		return present ? value.Format :
			APTR.FromPointer(ReadRaw(ref platform, state, obj, Format, 0));
	}

	private static uint MaxColumnsCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadFormatProjectionAdmission(ref platform, state, obj,
			out var value, out var present, out _, out _)) return 0;
		return present ? value.MaxColumns :
			NormalizeColumnLimit(ReadRaw(ref platform, state, obj, MaxColumns,
				DefaultMaxColumns));
	}

	private static uint FormatColumnsCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		// A malformed published policy is still authoritative failure state;
		// do not expose a valid descriptor count through a damaged policy row.
		if (!TryReadFormatProjectionAdmission(ref platform, state, obj,
			out var policy, out var policyPresent,
			out var descriptorState, out var descriptorPresent)) return 0;
		// Once the descriptor owner is published, its bounded count is the
		// authoritative FORMAT projection. The policy count is only a
		// construction-time fallback before descriptor publication exists.
		if (descriptorPresent) return policyPresent ? descriptorState.Columns : 0;
		return policyPresent ? policy.Columns :
			ReadRaw(ref platform, state, obj, FormatColumnsKey, 1);
	}

	private static bool TryReadFormatDescriptorAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListFormatDescriptorState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			FormatDescriptorKey, 0));
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListFormatDescriptorStateCodec.TryRead(ref platform, block,
			out value);
	}

	// FORMAT policy and descriptor owner are published as separate named
	// records, but their bounded column contract is one projection. A descriptor
	// owner with a count outside MaxColumns or different from the policy count
	// is structurally readable yet semantically stale; all consumers fail closed
	// before using its vector.
	private static bool TryReadFormatProjectionAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListFormatPolicyState policy, out bool policyPresent,
		out MuiListFormatDescriptorState descriptor, out bool descriptorPresent)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		policy = default;
		descriptor = default;
		if (!TryReadFormatPolicyAdmission(ref platform, state, obj,
			out policy, out policyPresent))
		{
			descriptorPresent = false;
			return false;
		}
		if (!TryReadFormatDescriptorAdmission(ref platform, state, obj,
			out descriptor, out descriptorPresent)) return false;
		if (descriptorPresent && (!policyPresent ||
			descriptor.Columns != policy.Columns ||
			descriptor.Columns > policy.MaxColumns)) return false;
		return true;
	}

	public static bool GetFormatColumn<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column, APTR storage)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (storage.IsNull || !platform.IsMapped(storage, FormatDescriptorSize))
			return false;
		// The descriptor owner and FORMAT policy form one published projection:
		// the policy supplies the bounded range and caller-owned format identity.
		// Do not expose a still-mapped descriptor vector after the policy record
		// has become malformed.
		if (!TryReadFormatProjectionAdmission(ref platform, state, obj,
			out _, out var policyPresent, out var descriptorState,
			out var present) || !policyPresent || !present) return false;
		var count = descriptorState.Columns;
		if (column >= count) return false;
		var block = descriptorState.Values;
		if (!TryGetOrderedDescriptorColumn(ref platform, state, obj, column,
			out var descriptorColumn) || descriptorColumn >= count) return false;
		var value = default(MuiListFormatDescriptor);
		var cursor = default(MuiListFormatDescriptorCursor);
		cursor.Base = block;
		cursor.Index = descriptorColumn;
		return MuiListFormatDescriptorVectorCodec.TryRead(ref platform, cursor,
			out value) &&
			MuiListFormatDescriptorCodec.TryWrite(ref platform, storage, value);
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
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || columns == 0) return false;
		if (!platform.IsMapped(storage, columns * ColumnGeometryRecordSize))
			return false;
		if (!TryGetHiddenColumns(ref platform, state, obj, width, columns,
			out var hidden)) return false;
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
			var geometry = default(MuiListColumnGeometry);
			geometry.Offset = offset;
			geometry.Width = columnWidth;
			if (!MuiListColumnGeometryVectorCodec.TryWrite(ref platform,
				cursor, geometry))
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

	internal static bool TryGetColumnLayoutState<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListColumnLayoutState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadColumnLayoutAdmission(ref platform, state, obj,
			out value, out var present))
			return false;
		return present;
	}

	private static bool InstallColumnLayout<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int width)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// A published layout record is authoritative. Do not replace a malformed
		// record from the derived geometry calculation during a runtime pass.
		if (!TryReadColumnLayoutAdmission(ref platform, state, obj,
			out _, out _)) return false;
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || columns == 0) return false;
		var bytes = columns * ColumnGeometryRecordSize;
		var values = MuiHeadlessMemory.Allocate(ref platform, bytes);
		if (values.IsNull) return false;
		if (!GetColumnGeometry(ref platform, state, obj, width, values))
		{
			platform.Clear(values, bytes);
			platform.Free(values, bytes);
			return false;
		}
		var block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListColumnLayoutState.Size);
		if (block.IsNull)
		{
			platform.Clear(values, bytes);
			platform.Free(values, bytes);
			return false;
		}
		var layout = default(MuiListColumnLayoutState);
		layout.Magic = MuiListColumnLayoutState.Cookie;
		layout.Width = unchecked((uint)width);
		layout.Columns = columns;
		layout.Values = values;
		if (!MuiListColumnLayoutStateCodec.Write(ref platform, block, layout))
		{
			platform.Clear(block, MuiListColumnLayoutState.Size);
			platform.Free(block, MuiListColumnLayoutState.Size);
			platform.Clear(values, bytes);
			platform.Free(values, bytes);
			return false;
		}
		var old = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			ColumnLayoutKey, 0));
		SetInternal(ref platform, state, obj, ColumnLayoutKey, block.Raw);
		if (old.IsNotNull) FreeColumnLayoutBlock(ref platform, old);
		return true;
	}

	private static void FreeColumnLayout<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, bool allowMalformed = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			ColumnLayoutKey, 0));
		if (block.IsNull) return;
		// Runtime invalidation may retire only an admitted named layout record.
		// Teardown opts into the bounded storage reader so a malformed record is
		// still reclaimed without deriving an untrusted byte count.
		if (!allowMalformed &&
			(!TryReadColumnLayoutAdmission(ref platform, state, obj,
				out _, out var present) || !present)) return;
		FreeColumnLayoutBlock(ref platform, block);
		ClearInternal(ref platform, state, obj, ColumnLayoutKey);
	}

	private static void FreeColumnLayoutBlock<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull) return;
		if (!MuiListColumnLayoutStateCodec.TryReadStorage(ref platform, block,
			out var value))
		{
			if (platform.IsMapped(block, MuiListColumnLayoutState.Size))
			{
				platform.Clear(block, MuiListColumnLayoutState.Size);
				platform.Free(block, MuiListColumnLayoutState.Size);
			}
			return;
		}
		var bytes = value.Columns * MuiListColumnGeometry.Size;
		if (platform.IsMapped(value.Values, bytes))
		{
			platform.Clear(value.Values, bytes);
			platform.Free(value.Values, bytes);
		}
		if (platform.IsMapped(block, MuiListColumnLayoutState.Size))
		{
			platform.Clear(block, MuiListColumnLayoutState.Size);
			platform.Free(block, MuiListColumnLayoutState.Size);
		}
	}

	private static bool TryReadColumnLayoutAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListColumnLayoutState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			ColumnLayoutKey, 0));
		present = block.IsNotNull;
		if (!present) return true;
		return MuiListColumnLayoutStateCodec.TryRead(ref platform, block,
			out value);
	}

	private static void FreeColumnMetrics<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, bool allowMalformed = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ColumnMetricsKey, 0));
		// A published non-NULL metrics record is authoritative guest state.
		// Runtime invalidation may retire only an admitted named struct; an
		// invalid cookie, width, count, or values vector is retained so mutation
		// cannot turn malformed state into an ownership/free primitive. Object
		// teardown opts into the bounded storage reader below to reclaim a record
		// that can no longer pass runtime admission.
		if (!allowMalformed &&
			(!TryReadColumnMetricsAdmission(ref platform, state, obj,
				out _, out var present) || !present)) return;
		if (!MuiListColumnMetricsStateCodec.TryReadStorage(ref platform, block,
			out var value))
		{
			if (block.IsNotNull && platform.IsMapped(block,
				MuiListColumnMetricsState.Size))
			{
				platform.Clear(block, MuiListColumnMetricsState.Size);
				platform.Free(block, MuiListColumnMetricsState.Size);
			}
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

	private static void FreePoolPolicyState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block,
			MuiListPoolPolicyState.Size)) return;
		// Deleting a pool is safe only after the complete typed record validates.
		// For malformed state, retire the guest record but do not interpret an
		// untrusted ownership bit or pool handle as a deletion request.
		if (MuiListPoolPolicyStateCodec.TryRead(ref platform, block,
			out var value) && value.UsesExternalPool == 0 && value.Pool.IsNotNull)
			platform.DeletePool(value.Pool);
		platform.Clear(block, MuiListPoolPolicyState.Size);
		platform.Free(block, MuiListPoolPolicyState.Size);
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
		return TryGetGeometryColumnCount(ref platform, state, obj,
			out var count) ? count : 0;
	}

	// Geometry consumers need to distinguish a valid one-column empty FORMAT
	// from a malformed published owner. Keep policy fallback only before the
	// descriptor owner exists; once either named record is malformed, report a
	// failed admission instead of letting callers normalize zero to one.
	private static bool TryGetGeometryColumnCount<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, out uint columns)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		columns = 0;
		if (!TryReadFormatProjectionAdmission(ref platform, state, obj,
			out var policy, out var policyPresent,
			out var descriptor, out var descriptorPresent)) return false;
		// Geometry and FORMAT descriptor projections share the display-column
		// permutation. A malformed published order is authoritative failure
		// state; do not let layout helpers continue with scalar/default widths.
		if (!TryReadColumnOrderAdmission(ref platform, state, obj,
			out _, out _)) return false;
		var count = descriptorPresent ? descriptor.Columns :
			policyPresent ? policy.Columns :
			ReadRaw(ref platform, state, obj, FormatColumnsKey, 1);
		if (count == 0) count = 1;
		columns = count > MaximumGeometryColumns ? MaximumGeometryColumns : count;
		return columns != 0;
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
	private static bool TryGetHiddenColumns<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, int width, uint columns,
		out MuiListHiddenColumns hidden)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		hidden = default;
		if (!TryReadColumnVisibilityAdmission(ref platform, state, obj,
			out var visibility, out var present)) return false;
		if (present)
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
		if (width <= 0 || columns <= 1) return true;
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
			if (!changed) return true;
		}
		return true;
	}

	private static uint ColumnOffset<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, int width, uint columns, uint target)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryGetHiddenColumns(ref platform, state, obj, width, columns,
			out var hidden)) return 0;
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
		if (!TryGetHiddenColumns(ref platform, state, obj, width, columns,
			out var hidden)) return 0;
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
	// guest-owned ColumnOrder permutation. Absence is the ordinary identity
	// mapping; a malformed published state is a hard failure so descriptor and
	// layout consumers cannot silently bypass the named permutation.
	private static bool TryGetOrderedDescriptorColumn<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint displayColumn,
		out uint descriptorColumn)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		descriptorColumn = displayColumn;
		if (!TryReadColumnOrderAdmission(ref platform, state, obj,
			out var order, out var present)) return false;
		if (!present || displayColumn >= order.Count) return true;
		var cursor = default(MuiListColumnOrderByteCursor);
		cursor.Base = order.Values;
		cursor.Index = displayColumn;
		if (!MuiListColumnOrderByteVectorCodec.TryReadValue(ref platform,
			cursor, out var resolved)) return false;
		descriptorColumn = resolved;
		return true;
	}

	private static bool TryDescriptorValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column, MuiListFormatField field,
		out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		var count = GeometryColumnCount(ref platform, state, obj);
		if (column >= count) return false;
		if (!TryReadFormatProjectionAdmission(ref platform, state, obj,
			out _, out var policyPresent, out var descriptorState,
			out var present) || !policyPresent || !present ||
			count > descriptorState.Columns) return false;
		var block = descriptorState.Values;
		if (!TryGetOrderedDescriptorColumn(ref platform, state, obj, column,
			out var descriptorColumn) || descriptorColumn >= count) return false;
		var descriptor = default(MuiListFormatDescriptor);
		var cursor = default(MuiListFormatDescriptorCursor);
		cursor.Base = block;
		cursor.Index = descriptorColumn;
		if (!MuiListFormatDescriptorVectorCodec.TryRead(ref platform, cursor,
			out descriptor)) return false;
		switch (field)
		{
			case MuiListFormatField.Delta:
				value = descriptor.Delta;
				return true;
			case MuiListFormatField.Weight:
				value = descriptor.Weight;
				return true;
			case MuiListFormatField.MinWidth:
				value = descriptor.MinWidth;
				return true;
			case MuiListFormatField.MaxWidth:
				value = descriptor.MaxWidth;
				return true;
			case MuiListFormatField.Column:
				value = descriptor.Column;
				return true;
			case MuiListFormatField.Flags:
				value = descriptor.Flags;
				return true;
			case MuiListFormatField.Preparse:
				value = descriptor.Preparse.Raw;
				return true;
			case MuiListFormatField.PreparseLength:
				value = descriptor.PreparseLength;
				return true;
			default:
				return false;
		}
	}

	private static uint DescriptorValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint column, MuiListFormatField field,
		uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryDescriptorValue(ref platform, state, obj, column, field,
			out var value) ? value : fallback;

	private static bool TryDisplaySourceColumn<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint displayColumn,
		out uint sourceColumn)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		sourceColumn = MaximumDrawColumns;
		// Display-source mapping is a FORMAT projection as well. Admit the
		// policy before the bounded out-of-range identity branch; otherwise a
		// damaged policy could remain observable through a harmless-looking
		// column request outside the descriptor table.
		if (!TryReadFormatProjectionAdmission(ref platform, state, obj,
			out _, out var policyPresent, out var descriptor,
			out var descriptorPresent) || !policyPresent) return false;
		// Lists without FORMAT descriptors retain the ordinary single-column
		// identity projection. The ColumnOrder record is still admitted first so
		// a malformed published permutation cannot hide behind that fallback.
		if (!TryReadColumnOrderAdmission(ref platform, state, obj,
			out var order, out var orderPresent)) return false;
		if (!descriptorPresent)
		{
			if (displayColumn >= MaximumDrawColumns) return false;
			sourceColumn = displayColumn;
			return true;
		}
		// FORMAT descriptors are bounded. Preserve the historical identity
		// projection for a display column outside that bounded set unless a
		// published permutation claims the column; the latter is inconsistent
		// state and must fail closed.
		if (displayColumn >= descriptor.Columns)
		{
			if (orderPresent && displayColumn < order.Count) return false;
			sourceColumn = displayColumn < MaximumDrawColumns
				? displayColumn : MaximumDrawColumns;
			return sourceColumn < MaximumDrawColumns;
		}
		if (!TryDescriptorValue(ref platform, state, obj, displayColumn,
			MuiListFormatField.Column, out var source) ||
			source >= MaximumDrawColumns) return false;
		sourceColumn = source;
		return true;
	}

	// FORMAT's COL field changes which source StringArray column is displayed
	// at a given derived column. Keep the mapping in the named descriptor
	// record; the only raw arithmetic below is the guest pointer-table ABI.
	private static uint DisplaySourceColumn<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint displayColumn)
		where TPlatform : struct, IMuiHeadlessPlatform
		=> TryDisplaySourceColumn(ref platform, state, obj, displayColumn,
			out var source) ? source : MaximumDrawColumns;

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
		if (length < 2) return MuiListTextAlignment.Left;
		var prefixLength = length >= 4 ? 3u : 2u;
		if (!MuiListPreparsePrefixRecordCodec.TryRead(ref platform, preparse,
			prefixLength, out var prefix))
			return MuiListTextAlignment.Left;
		var first = prefix.First;
		var codeOffset = -1;
		if (first == 0x1Bu && length >= 2)
			codeOffset = 1;
		else if (first == (byte)'*' && length >= 3 &&
			(prefix.Second == (byte)'e' || prefix.Second == (byte)'E'))
			codeOffset = 2;
		else if (first == (byte)'\\' && length >= 4 &&
			prefix.Second == (byte)'3' && prefix.Third == (byte)'3')
			codeOffset = 3;
		if (codeOffset < 0) return MuiListTextAlignment.Left;
		if (!MuiListFormatByteCursorCodec.TryReadAt(ref platform, preparse,
			codeOffset, out var code)) return MuiListTextAlignment.Left;
		return code switch
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
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out _, out _)) return false;
		if (!TryReadActiveStateAdmission(ref platform, state, obj,
			out _, out var activePresent) ||
			(!activePresent && !EnsureActiveState(ref platform, state, obj)))
			return false;
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
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out _, out _)) return false;
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
		var ownerBlock = APTR.FromPointer(Read(ref platform, state, obj,
			ListviewOwnerKey, 0));
		FreeListviewOwnerState(ref platform, ownerBlock);
		ClearInternal(ref platform, state, obj, ListviewOwnerKey);
		var rawHeader = APTR.FromPointer(Read(ref platform, state, obj,
			ListHeaderKey, 0));
		var header = Header(ref platform, state, obj);
		if (header.IsNull)
		{
			// Runtime consumers reject a malformed cookie, but disposal may still
			// use a structurally valid header to retire its owned index and slots.
			if (rawHeader.IsNull || !TryReadHeaderStorage(ref platform, rawHeader,
				out _))
			{
				if (rawHeader.IsNotNull && platform.IsMapped(rawHeader, HeaderSize))
				{
					platform.Clear(rawHeader, HeaderSize);
					platform.Free(rawHeader, HeaderSize);
				}
				ClearInternal(ref platform, state, obj, ListHeaderKey);
				return;
			}
			header = rawHeader;
		}
		CancelEditState(ref platform, state, obj);
		var index = ReadHeaderIndex(ref platform, header);
		var capacity = ReadHeaderCapacity(ref platform, header);
		var count = ReadHeaderCount(ref platform, header);
		var pool = PoolFor(ref platform, state, obj);
		var formatPolicy = APTR.FromPointer(Read(ref platform, state, obj,
			FormatPolicyKey, 0));
		var boundedFormatCount = 0u;
		if (TryReadFormatPolicyState(ref platform, formatPolicy,
			out var formatPolicyValue) &&
			IsValidFormatPolicy(ref platform, formatPolicyValue))
			boundedFormatCount = formatPolicyValue.Columns;
		var formatBlock = APTR.FromPointer(ReadRaw(ref platform, state, obj,
			FormatDescriptorKey, 0));
		var titleArrayState = APTR.FromPointer(Read(ref platform, state, obj,
			TitleArrayStateKey, 0));
		for (var i = 0u; i < count && i < MaximumEntries; i++)
			DestructSlot(ref platform, state, obj, index, i, pool);
		FreeImages(ref platform, header);
		FreeColumnLayout(ref platform, state, obj, true);
		FreeColumnMetrics(ref platform, state, obj, true);
		FreeFormatDescriptorState(ref platform, formatBlock, true,
			boundedFormatCount);
		ClearInternal(ref platform, state, obj, FormatDescriptorKey);
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
		var insertPositionState = APTR.FromPointer(Read(ref platform, state, obj,
			InsertPositionStateKey, 0));
		FreeInsertPositionState(ref platform, insertPositionState);
		ClearInternal(ref platform, state, obj, InsertPositionStateKey);
		var poolPolicy = APTR.FromPointer(Read(ref platform, state, obj,
			PoolPolicyKey, 0));
		// Entries must be destructed while the list-owned pool is alive. A
		// supplied MUIA_List_Pool is borrowed and is never deleted here; a
		// malformed record is retired without interpreting untrusted ownership.
		FreePoolPolicyState(ref platform, poolPolicy);
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
		var normalizedPolicy = NormalizeHScrollerPolicy(policy);
		if (normalizedPolicy == HScrollerAlways) return true;
		if (normalizedPolicy == HScrollerNever) return false;
		return contentWidth > viewWidth;
	}

	// The public attribute remains construction-only, while the derived
	// viewport state is intentionally queryable by Listview and future native
	// horizontal-scroller composition through this named record.
	internal static bool TryGetHScrollerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListHScrollerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadHScrollerStateAdmission(ref platform, state, obj,
			out value, out var present)) return false;
		if (!present)
		{
			var policy = NormalizeHScrollerPolicy(ReadRaw(ref platform, state, obj,
				HScrollerVisibility, HScrollerAuto));
			if (!EnsureHScrollerState(ref platform, state, obj, policy) ||
				!TryReadHScrollerStateAdmission(ref platform, state, obj,
					out value, out present) || !present) return false;
		}
		return true;
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
		if (!TryReadRedrawStateAdmission(ref platform, state, obj,
			out var redraw, out var present) || !present) return 0;
		return redraw.Requests;
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
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out _, out _)) return false;
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
			if (!MuiListSlotVectorCodec.TryRead(ref platform, sourceCursor,
				out var value) ||
				!MuiListSlotVectorCodec.TryWrite(ref platform, scratchCursor, value))
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
		return MuiListSlotVectorCodec.TryRead(ref platform, cursor, out value);
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
		editState.Magic = MuiListEditState.Cookie;
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
			else if (!MuiListPointerSlotVectorCodec.TryRead(ref platform,
				oldCursor, out value))
			{
				platform.Clear(source, tableSize);
				platform.Free(source, tableSize);
				return false;
			}
			if (!MuiListPointerSlotVectorCodec.TryWrite(ref platform,
				sourceCursor, value))
			{
				platform.Clear(source, tableSize);
				platform.Free(source, tableSize);
				return false;
			}
		}
		sourceCursor.Index = columns;
		if (!MuiListPointerSlotVectorCodec.TryWrite(ref platform,
			sourceCursor, default))
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
					if (!MuiListPointerSlotVectorCodec.TryRead(ref platform,
						cursor, out var slotValue)) break;
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
		var cursor = default(MuiStringLengthByteCursor);
		cursor.Text = text;
		for (var index = 0u; index < MaximumStringLength; index++)
		{
			cursor.Index = index;
			if (!MuiStringLengthByteCursorCodec.TryReadByte(ref platform, cursor,
				out var value)) break;
			if (value == 0) break;
			if (value == (byte)'\n' && lines < MaximumAutoLines) lines++;
		}
		return lines;
	}

	internal static uint TitleRowCount<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadTitleArrayAdmission(ref platform, state, obj,
			out var titleArray, out var titleArrayPresent)) return 0;
		if (titleArrayPresent)
			return titleArray.Count != 0 ? 1u : 0u;
		return TitleValueCursor(ref platform, state, obj) == 0 ? 0u : 1u;
	}

	internal static uint TitleValueCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadTitleStateAdmission(ref platform, state, obj,
			out var value, out var present)) return 0;
		// A true absence can occur during the low-level construction window;
		// retain the bounded raw projection only for that bootstrap case. A
		// malformed present record is rejected above and therefore contributes no
		// title row or drawing pointer.
		return present ? value.Value : ReadRaw(ref platform, state, obj, Title, 0);
	}

	internal static bool TryGetTitleState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiListTitleState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadTitleStateAdmission(ref platform, state, obj,
			out value, out var present) && present;
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
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform,
				sourceCursor, out var sourceValue) ||
				!MuiListPointerSlotVectorCodec.TryWrite(ref platform,
					destinationCursor, sourceValue))
				return false;
		}
		destinationCursor.Index = columns;
		if (!MuiListPointerSlotVectorCodec.TryWrite(ref platform,
			destinationCursor, default)) return false;
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
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || columns == 0) return false;
		if (!TryReadColumnVisibilityAdmission(ref platform, state, obj,
			out _, out _)) return false;
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
				if (MuiListPointerSlotVectorCodec.TryRead(ref platform,
					cursor, out var displayValue))
					text = displayValue.Value;
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
		if (!TryReadColumnMetricsAdmission(ref platform, state, obj,
			out var value, out var present) || !present ||
			value.Width != unchecked((uint)width) || column >= value.Columns)
			return 0;
		var values = value.Values;
		if (!platform.IsMapped(values, value.Columns *
			MuiListColumnMetricValue.Size)) return 0;
		var cursor = default(MuiListColumnMetricCursor);
		cursor.Base = values;
		cursor.Index = column;
		return MuiListColumnMetricVectorCodec.TryReadValue(ref platform, cursor,
			out var metric) ? metric : 0;
	}

	// Publish the MorphOS List pixel viewport from the normalized guest row
	// state. TopPixel follows the first data row; title rows occupy viewport and
	// total space but do not move the data cursor. Saturating arithmetic keeps
	// malformed, very large entry counts from wrapping the public ULONGs.
	private static bool RefreshViewportState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out _, out _)) return false;
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
		// The public scalar attributes below are updated through SetNotify so an
		// external Prop can follow the documented List TopPixel/VisiblePixel/
		// TotalPixel connection without reading private record words.
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
		// These are public [..G]/[.SG] projections used by the MorphOS external
		// scrollbar recipe. SetNotify is change-only, so repeated viewport
		// refreshes remain quiet while real insertion, layout, or First changes
		// dispatch the listener through the existing guest-resident notification
		// records. No offset-based shadow state is introduced.
		SetNotify(ref platform, state, obj, TopPixel, value.TopPixel);
		SetNotify(ref platform, state, obj, VisiblePixel, value.VisiblePixel);
		SetNotify(ref platform, state, obj, TotalPixel, value.TotalPixel);
		return true;
	}

	private static bool SetViewportLineHeight<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint lineHeight)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out var value, out var present) || !present)
			return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ViewportStateKey, 0));
		value.LineHeight = lineHeight;
		WriteViewportState(ref platform, block, value);
		return true;
	}

	private static bool SetViewportDropMark<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint dropMark)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out var value, out var present) || !present)
			return false;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			ViewportStateKey, 0));
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
		return TryReadViewportStateAdmission(ref platform, state, obj,
			out value, out var present) && present;
	}

	// Composite consumers use the named cursor when the viewport record exists;
	// the raw attribute is only a construction/early-lifecycle fallback.
	internal static uint FirstCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out var value, out var present)) return 0;
		return present ? value.First : Read(ref platform, state, obj, First, 0);
	}

	// The visible row capacity follows the same publication boundary as First.
	// Keep the raw attribute only as a fallback before the first viewport record
	// exists or while Layout is constructing the next publication.
	internal static uint VisibleCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out var value, out var present)) return 0;
		return present ? value.Visible : Read(ref platform, state, obj, Visible, 0);
	}

	// DropMark is a derived drag insertion cue. Prefer the named viewport record
	// after publication; raw state remains the early-lifecycle and transition
	// fallback used before a viewport exists.
	internal static uint DropMarkCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out var value, out var present))
			return unchecked((uint)DropMarkNone);
		return present ? value.DropMark : Read(ref platform, state, obj, DropMark,
			unchecked((uint)DropMarkNone));
	}

	// Effective line height is a derived projection. Prefer the named viewport
	// record once it exists; the raw attribute remains only as an early-lifecycle
	// fallback before the first viewport publication.
	private static uint LineHeightCursor<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out var value, out var present)) return fallback;
		return present && value.LineHeight != 0 ? value.LineHeight : fallback;
	}

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
		// A non-NULL malformed record is not absence. Leave it untouched and
		// let metric consumers fail closed until object teardown retires it
		// through the bounded storage reader.
		if (!TryReadColumnMetricsAdmission(ref platform, state, obj,
			out _, out _)) return true;
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || columns == 0) return true;
		if (!TryReadColumnVisibilityAdmission(ref platform, state, obj,
			out _, out _)) return true;
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
		var metricCursor = default(MuiListColumnMetricCursor);
		metricCursor.Base = values;

		var header = Header(ref platform, state, obj);
		var count = header.IsNull ? 0u : ReadHeaderCount(ref platform, header);
		var font = FontCursor(ref platform, state, obj);
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
					if (MuiListPointerSlotVectorCodec.TryRead(ref platform,
						cursor, out var displayValue))
						text = displayValue.Value;
				}
				if (text.IsNull || !TryReadCStringLength(ref platform, text,
					MaximumStringLength, out var length)) continue;
				var measured = platform.TextWidth(APTR.Null, font, text,
					unchecked((int)length));
				if (measured <= 0) continue;
				metricCursor.Index = column;
				if (!MuiListColumnMetricVectorCodec.TryReadValue(ref platform,
					metricCursor, out var metric)) continue;
				if (unchecked((uint)measured) > metric)
				{
					metric = unchecked((uint)measured);
					if (!MuiListColumnMetricVectorCodec.TryWriteValue(ref platform,
						metricCursor, metric)) return true;
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
		metrics.Magic = MuiListColumnMetricsState.Cookie;
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
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out _, out _)) return false;
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
		var previousVisible = Read(ref platform, state, obj, Visible, 0);
		var previousFirst = Read(ref platform, state, obj, First, 0);
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
		if (!RefreshViewportState(ref platform, state, obj)) return false;
		// Layout may normalize First or change the row capacity without going
		// through OM_SET.  Publish those public projections after the named
		// viewport record and pixel metrics are current, so a callback can read a
		// coherent tuple.  The explicit previous values avoid a second,
		// offset-based shadow state and preserve change-only semantics.
		var publishedVisible = Read(ref platform, state, obj, Visible, rows);
		var publishedFirst = Read(ref platform, state, obj, First,
			unchecked((uint)normalized));
		NotifyViewportTransition(ref platform, state, obj, Visible,
			previousVisible, publishedVisible);
		NotifyViewportTransition(ref platform, state, obj, First,
			previousFirst, publishedFirst);
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
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || columns == 0) return;
		if (!TryReadColumnVisibilityAdmission(ref platform, state, obj,
			out _, out _)) return;
		if (columns > MaximumDrawColumns) columns = MaximumDrawColumns;
		var layoutBlock = APTR.Null;
		if (TryReadColumnLayoutAdmission(ref platform, state, obj,
			out var layout, out var layoutPresent) && layoutPresent &&
			layout.Width == unchecked((uint)layoutWidth) &&
			layout.Columns == columns)
			layoutBlock = layout.Values;
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
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || columns == 0) return false;
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
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || columns == 0) return false;
		if (!TryReadColumnVisibilityAdmission(ref platform, state, obj,
			out _, out _)) return false;
		if (columns > MaximumDrawColumns) columns = MaximumDrawColumns;
		var layoutBlock = APTR.Null;
		if (TryReadColumnLayoutAdmission(ref platform, state, obj,
			out var layout, out var layoutPresent) && layoutPresent &&
			layout.Width == unchecked((uint)contentLayoutWidth) &&
			layout.Columns == columns)
			layoutBlock = layout.Values;
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
		var titleArrayState = default(MuiListTitleArrayState);
		var titleArrayPresent = false;
		if (rows != 0 &&
			TryReadTitleArrayAdmission(ref platform, state, obj,
				out titleArrayState, out titleArrayPresent) &&
			titleArrayPresent && titleArrayState.Count != 0)
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
		else if (!titleArrayPresent && TitleValueCursor(ref platform,
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
				if (MuiListPointerSlotVectorCodec.TryRead(ref platform,
					cursor, out var displayValue))
					text = displayValue.Value;
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
		if (pos == InsertSorted && !TryGetSortColumnValue(ref platform,
			state, obj, out _)) return false;
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
		if (pos == InsertSorted && !TryGetSortColumnValue(ref platform,
			state, obj, out _)) return false;
		CancelEditState(ref platform, state, obj);
		var before = ReadHeaderCount(ref platform, header);
		var terminated = count < 0;
		var limit = terminated ? MaximumEntries : (uint)count;
		var cursor = default(MuiListPointerVectorCursor);
		cursor.Base = entries;
		for (var i = 0u; i < limit; i++)
		{
			cursor.Index = i;
			if (!MuiListPointerVectorCodec.TryRead(ref platform, cursor,
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
				if (MuiListScalarStorageCodec.WriteValue(ref platform, stateStorage,
					0)) return true;
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
				!MuiListScalarStorageCodec.WriteValue(ref platform, stateStorage,
					result.Value))
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
		if (!MuiListScalarStorageCodec.TryReadValue(ref platform, posStorage,
			out var positionRaw)) return false;
		var position = default(MuiListScalarStorageRecord);
		position.Value = positionRaw;
		var current = unchecked((int)position.Value);
		var start = current == NextSelectedStart ? 0u : (uint)(current + 1);
		for (var i = start; i < count; i++)
		{
			if ((SlotFlagsAt(ref platform, header, i) & SlotSelected) != 0)
			{
				position.Value = i;
				if (!MuiListScalarStorageCodec.WriteValue(ref platform, posStorage,
					position.Value)) return false;
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
				if (!MuiListScalarStorageCodec.WriteValue(ref platform, posStorage,
					position.Value)) return false;
				return true;
			}
		}
		position.Value = unchecked((uint)NextSelectedEnd);
		if (!MuiListScalarStorageCodec.WriteValue(ref platform, posStorage,
			position.Value))
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
		if (!TryGetSortColumnValue(ref platform, state, obj,
			out var column)) return false;
		// CompareForSort consumes FORMAT flags and the display-column mapping.
		// Admit the complete named geometry/order projection before allowing the
		// allocation-free insertion sort to mutate guest slots.
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || column >= columns) return false;
		CancelEditState(ref platform, state, obj);
		var count = ReadHeaderCount(ref platform, header);
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
		if (!TryGetSortColumnValue(ref platform, state, obj,
			out var column)) return false;
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || column >= columns) return false;
		CancelEditState(ref platform, state, obj);
		uint count = 0;
		var cursor = default(MuiListPointerVectorCursor);
		cursor.Base = entries;
		while (count < MaximumEntries)
		{
			cursor.Index = count;
			if (!MuiListPointerVectorCodec.TryRead(ref platform, cursor,
				out var slotValue)) return false;
			if (slotValue.Value.IsNull) break;
			count++;
		}
		for (var i = 1u; i < count; i++)
		{
			cursor.Index = i;
			if (!MuiListPointerVectorCodec.TryRead(ref platform, cursor,
				out var entryValue)) return false;
			var entry = entryValue.Value;
			var j = i;
			while (j > 0)
			{
				cursor.Index = j - 1;
				if (!MuiListPointerVectorCodec.TryRead(ref platform, cursor,
					out var previousValue)) return false;
				if (CompareForSort(ref platform, state, obj, previousValue.Value,
					entry, column) <= 0) break;
				cursor.Index = j;
				if (!MuiListPointerVectorCodec.TryWrite(ref platform, cursor,
					previousValue)) return false;
				j--;
			}
			cursor.Index = j;
			var destinationValue = default(MuiListPointerSlotRecord);
			destinationValue.Value = entry;
			if (!MuiListPointerVectorCodec.TryWrite(ref platform, cursor,
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
		if (!TryReadViewportStateAdmission(ref platform, state, obj,
			out _, out _)) return false;
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
		if (!TryReadRedrawStateAdmission(ref platform, state, obj,
			out var redraw, out var present) || !present) return;
		if (PresentationPolicyValue(ref platform, state, obj, Quiet, 0) != 0)
			redraw.Dirty = 1;
		else
			redraw.Requests = SaturatingAdd(redraw.Requests, 1);
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			RedrawStateKey, 0));
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

	// Resolve the entry pointer supplied with MUIV_List_Redraw_Entry without
	// manufacturing a managed mirror of the list. The guest-resident slot
	// vector remains the source of truth and the bounded count keeps malformed
	// pointers from turning redraw qualification into an unbounded walk.
	private static int FindEntryIndex<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR entry)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (entry.IsNull) return -1;
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return -1;
		var count = ReadHeaderCount(ref platform, header);
		for (var i = 0u; i < count; i++)
			if (SlotEntryAt(ref platform, header, i) == entry)
				return unchecked((int)i);
		return -1;
	}

	// Schedule a redraw for the requested scope. Requires a graphics-capable
	// platform; only issues a request when the list actually holds state.
	public static bool Redraw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int pos) where TPlatform : struct, IMuiLayoutPlatform
		=> Redraw(ref platform, state, obj, pos, APTR.Null);

	public static bool Redraw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int pos, APTR entry)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		var header = Header(ref platform, state, obj);
		if (header.IsNull) return false;
		if (!TryReadRedrawStateAdmission(ref platform, state, obj,
			out _, out var redrawPresent) || !redrawPresent) return false;
		var entryRequest = pos == RedrawEntry;
		if (entryRequest)
		{
			pos = FindEntryIndex(ref platform, state, obj, entry);
			if (pos < 0) return true;
			if (!IsRedrawTargetVisible(ref platform, state, obj, pos))
				return true;
		}
		else if (!IsRedrawTargetVisible(ref platform, state, obj, pos))
			return true;
		if (PresentationPolicyValue(ref platform, state, obj, Quiet, 0) != 0)
		{
			RequestMutationRedraw(ref platform, state, obj);
			return true;
		}
		var flags = entryRequest ? 2u : pos switch
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
			var dup = DuplicateString(ref platform, entry, pool);
			ownership = dup.IsNotNull ? SlotOwnedString : 0;
			return dup;
		}
		if (hook == HookStringArray)
		{
			if (entry.IsNull) return APTR.Null;
			var dup = DuplicateStringArray(ref platform, entry, pool);
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
			FreeOwnedString(ref platform, entry, pool);
			return;
		}
		if (ownership == SlotOwnedStringArray)
		{
			FreeOwnedStringArray(ref platform, entry, pool);
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
		return MuiListDisplayRowRecordCodec.WriteValue(ref platform, prefix,
			unchecked((uint)row));
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
			var cursor = default(MuiListPointerSlotCursor);
			cursor.Base = array;
			cursor.Index = 0;
			if (!MuiListPointerSlotVectorCodec.TryWrite(ref platform, cursor,
				value))
				return false;
			cursor.Index = 1;
			return MuiListPointerSlotVectorCodec.TryWrite(ref platform,
				cursor, default);
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
		if (!TryReadColumnVisibilityAdmission(ref platform, state, obj,
			out _, out _)) return false;
		var width = Read(ref platform, state, obj, Width, 0);
		if (width == 0) return false;
		if (!TryReadColumnLayoutAdmission(ref platform, state, obj,
			out var layout, out var layoutPresent)) return false;
		if (layoutPresent && layout.Width == width &&
			layout.Columns == columns)
		{
			return TryReadColumnGeometryRecord(ref platform, layout.Values, column,
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
		return MuiListColumnGeometryVectorCodec.TryRead(ref platform, cursor,
			out geometry);
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
		if (!TryGetGeometryColumnCount(ref platform, state, obj,
			out var columns) || columns == 0) return false;
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
		SetInsertPosition(ref platform, state, obj, unchecked((uint)index));
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
			if (!MuiListPointerVectorCodec.TryRead(ref platform, cursor,
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
		if (!MuiListSlotVectorCodec.TryRead(ref platform, cursor, out var value))
			return;
		var entry = value.Entry;
		var flags = value.Flags;
		Destruct(ref platform, state, obj, entry,
			flags & (SlotOwnedString | SlotOwnedStringArray | SlotOwnedRecord),
			pool);
	}

	private static APTR DuplicateString<TPlatform>(ref TPlatform platform,
		APTR source, APTR pool) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadCStringLength(ref platform, source, MaximumStringLength,
			out var length)) return APTR.Null;
		var size = length + 1;
		var copy = pool.IsNotNull
			? platform.AllocPooled(pool, size)
			: MuiHeadlessMemory.Allocate(ref platform, size);
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
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform, cursor,
				out var value)) return false;
			if (value.Value.IsNull) return true;
			count++;
		}
		cursor.Index = count;
		return MuiListPointerSlotVectorCodec.TryRead(ref platform, cursor,
			out var terminatorValue) && terminatorValue.Value.IsNull;
	}

	// Copy a NULL-terminated array of C-string pointers into a private guest
	// pointer table and private string buffers. The source array and every
	// string are bounded before any allocation is retained, so malformed input
	// fails without exposing a partial entry.
	private static APTR DuplicateStringArray<TPlatform>(ref TPlatform platform,
		APTR source, APTR pool) where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint count = 0;
		var sourceCursor = default(MuiListPointerSlotCursor);
		sourceCursor.Base = source;
		while (count < MaximumArrayEntries)
		{
			sourceCursor.Index = count;
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform,
				sourceCursor, out var value)) return APTR.Null;
			var text = value.Value;
			if (text.IsNull) break;
			if (!TryReadCStringLength(ref platform, text,
				MaximumStringLength, out _)) return APTR.Null;
			count++;
		}
		if (count == MaximumArrayEntries)
		{
			sourceCursor.Index = count;
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform,
				sourceCursor, out var terminatorValue) ||
				!terminatorValue.Value.IsNull)
				return APTR.Null;
		}

		var tableSize = (count + 1) * MuiListPointerSlotRecord.Size;
		var table = pool.IsNotNull
			? platform.AllocPooled(pool, tableSize)
			: MuiHeadlessMemory.Allocate(ref platform, tableSize);
		if (table.IsNull) return APTR.Null;
		var destinationCursor = default(MuiListPointerSlotCursor);
		destinationCursor.Base = table;
		for (var i = 0u; i < count; i++)
		{
			sourceCursor.Index = i;
			destinationCursor.Index = i;
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform,
				sourceCursor, out var sourceValue))
			{
				FreeOwnedStringArray(ref platform, table, pool);
				return APTR.Null;
			}
			var text = sourceValue.Value;
			var copy = DuplicateString(ref platform, text, pool);
			if (copy.IsNull)
			{
				FreeOwnedStringArray(ref platform, table, pool);
				return APTR.Null;
			}
			var destinationValue = default(MuiListPointerSlotRecord);
			destinationValue.Value = copy;
			if (!MuiListPointerSlotVectorCodec.TryWrite(ref platform,
				destinationCursor, destinationValue))
			{
				// The copy is not reachable through the table when its destination
				// slot cannot be published. Release it explicitly before rolling
				// back the already-published entries.
				FreeOwnedString(ref platform, copy, pool);
				FreeOwnedStringArray(ref platform, table, pool);
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
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform,
				sourceCursor, out var sourceValue) ||
				!MuiListPointerSlotVectorCodec.TryWrite(ref platform,
					destinationCursor, sourceValue))
				return false;
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
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform, cursor,
				out var value)) return APTR.Null;
			if (i == column || value.Value.IsNull)
				return i == column ? value.Value : APTR.Null;
		}
		return APTR.Null;
	}

	private static void FreeOwnedString<TPlatform>(ref TPlatform platform,
		APTR entry, APTR pool) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (entry.IsNull) return;
		if (!TryReadCStringLength(ref platform, entry, MaximumStringLength,
			out var length)) return;
		var size = length + 1;
		platform.Clear(entry, size);
		if (pool.IsNotNull)
			platform.FreePooled(pool, entry, size);
		else
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
		APTR table, APTR pool) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (table.IsNull) return;
		uint count = 0;
		var cursor = default(MuiListPointerSlotCursor);
		cursor.Base = table;
		while (count < MaximumArrayEntries)
		{
			cursor.Index = count;
			if (!MuiListPointerSlotVectorCodec.TryRead(ref platform, cursor,
				out var value)) break;
			var text = value.Value;
			if (text.IsNull) break;
			FreeOwnedString(ref platform, text, pool);
			count++;
		}
		var tableSize = (count + 1) * MuiListPointerSlotRecord.Size;
		platform.Clear(table, tableSize);
		if (pool.IsNotNull)
			platform.FreePooled(pool, table, tableSize);
		else
			platform.Free(table, tableSize);
	}

	private static int CompareStrings<TPlatform>(ref TPlatform platform, APTR left,
		APTR right) where TPlatform : struct, IMuiGuestMemory
	{
		if (left.Raw == right.Raw) return 0;
		if (left.IsNull) return right.IsNull ? 0 : -1;
		if (right.IsNull) return 1;
		var leftCursor = default(MuiStringLengthByteCursor);
		leftCursor.Text = left;
		var rightCursor = default(MuiStringLengthByteCursor);
		rightCursor.Text = right;
		for (var i = 0u; i < MaximumStringLength; i++)
		{
			leftCursor.Index = i;
			rightCursor.Index = i;
			if (!MuiStringLengthByteCursorCodec.TryReadByte(ref platform,
				leftCursor, out var lb) ||
				!MuiStringLengthByteCursorCodec.TryReadByte(ref platform,
					rightCursor, out var rb)) return 0;
			if (lb != rb) return lb < rb ? -1 : 1;
			if (lb == 0) return 0;
		}
		return 0;
	}

	private static APTR ReadHeaderIndex<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		TryReadHeaderStorage(ref platform, header, out var value)
			? value.Index : APTR.Null;

	private static uint ReadHeaderCapacity<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		TryReadHeaderStorage(ref platform, header, out var value)
			? value.Capacity : 0;

	private static uint ReadHeaderCount<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		TryReadHeaderStorage(ref platform, header, out var value)
			? value.Count : 0;

	private static APTR ReadHeaderImages<TPlatform>(ref TPlatform platform,
		APTR header) where TPlatform : struct, IMuiGuestMemory =>
		TryReadHeaderStorage(ref platform, header, out var value)
			? value.Images : APTR.Null;

	private static bool TryReadHeaderStorage<TPlatform>(ref TPlatform platform,
		APTR header, out MuiListHeaderState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (!MuiListHeaderCodec.TryReadStorage(ref platform, header,
			out value) || value.Capacity == 0 ||
			value.Capacity > MaximumEntries || value.Count > value.Capacity ||
			value.Index.IsNull || value.Capacity > uint.MaxValue / SlotSize)
		{
			value = default;
			return false;
		}
		var bytes = value.Capacity * SlotSize;
		if (!platform.IsMapped(value.Index, bytes) ||
			(value.Images.IsNotNull && !platform.IsMapped(value.Images,
				ImageRecordSize)))
		{
			value = default;
			return false;
		}
		return true;
	}

	private static bool TryReadHeaderAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListHeaderState value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		present = false;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ListHeaderKey, out var rawHeader)) return true;
		var header = APTR.FromPointer(rawHeader);
		present = header.IsNotNull;
		if (!present) return true;
		if (!TryReadHeaderStorage(ref platform, header, out value) ||
			value.Magic != MuiListHeaderState.Cookie)
		{
			value = default;
			return false;
		}
		return true;
	}

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
		if (!TryReadHeaderAdmission(ref platform, state, obj,
			out _, out var present) || !present) return APTR.Null;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
			ListHeaderKey, out var value) || value == 0) return APTR.Null;
		return APTR.FromPointer(value);
	}

	private static APTR SlotEntryAt<TPlatform>(ref TPlatform platform, APTR header,
		uint index) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var array = ReadHeaderIndex(ref platform, header);
		var cursor = default(MuiListSlotCursor);
		cursor.Base = array;
		cursor.Index = index;
		return MuiListSlotVectorCodec.TryRead(ref platform, cursor, out var value)
			? value.Entry : APTR.Null;
	}

	private static uint SlotFlagsAt<TPlatform>(ref TPlatform platform, APTR header,
		uint index) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var array = ReadHeaderIndex(ref platform, header);
		var cursor = default(MuiListSlotCursor);
		cursor.Base = array;
		cursor.Index = index;
		return MuiListSlotVectorCodec.TryRead(ref platform, cursor, out var value)
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
		var value = default(MuiListSlotState);
		value.Entry = entry;
		value.Flags = flags;
		MuiListSlotVectorCodec.TryWrite(ref platform, cursor, value);
	}

	private static bool TryWriteSlot<TPlatform>(ref TPlatform platform,
		APTR header, uint index, MuiListSlotState value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var array = ReadHeaderIndex(ref platform, header);
		var cursor = default(MuiListSlotCursor);
		cursor.Base = array;
		cursor.Index = index;
		return MuiListSlotVectorCodec.TryWrite(ref platform, cursor, value);
	}

	private static int ActiveIndex<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		// MorphOS 3.20's public empty-list projection is zero, but zero must not
		// become a real row for Active/Remove/Redraw/selection selectors. The
		// named cursor record also distinguishes an empty-list zero from a real
		// row zero immediately after the first insertion.
		if (EntryCount(ref platform, state, obj) == 0) return -1;
		if (!TryReadActiveStateAdmission(ref platform, state, obj,
			out var cursor, out var present)) return -1;
		var raw = unchecked((int)ReadRaw(ref platform, state, obj, Active,
			ActiveOff));
		if (present)
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
		if (!TryReadActiveStateAdmission(ref platform, state, obj,
			out value, out var present)) return false;
		return present;
	}

	private static void SetActive<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint value) where TPlatform : struct, IMuiHeadlessPlatform
	{
		// Clear/Remove transitions publish the empty-list projection after the
		// header count reaches zero. The previous active row is then necessarily
		// outside the dynamic admission range, so reset the named cursor through
		// its structural codec before returning to normal admission.
		if (EntryCount(ref platform, state, obj) == 0)
		{
			var emptyBlock = APTR.FromPointer(Read(ref platform, state, obj,
				ActiveStateKey, 0));
			if (!TryReadActiveState(ref platform, emptyBlock, out var emptyValue))
				return;
			emptyValue.Active = 0;
			emptyValue.HasActive = 0;
			if (!WriteActiveState(ref platform, emptyBlock, emptyValue)) return;
			SetNotify(ref platform, state, obj, Active, 0);
			return;
		}
		if (!TryReadActiveStateAdmission(ref platform, state, obj,
			out _, out var present) ||
			(!present && !EnsureActiveState(ref platform, state, obj))) return;
		SetNotify(ref platform, state, obj, Active, value);
		SetActiveCursor(ref platform, state, obj, value,
			EntryCount(ref platform, state, obj) != 0 &&
			unchecked((int)value) >= 0);
	}

	// A published reverse owner is authoritative guest state.  A non-NULL
	// record that fails the cookie/field contract is malformed, not absence;
	// selection propagation must not consult its raw scalar alias.
	internal static bool TryGetListviewOwner<TPlatform>(ref TPlatform platform,
		APTR state, APTR list, out MuiListviewOwnerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		if (!MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, list,
			ListviewOwnerKey, out var rawBlock)) return false;
		var block = APTR.FromPointer(rawBlock);
		return MuiListviewOwnerStateCodec.TryRead(ref platform, block,
			out value) && IsValidListviewOwner(ref platform, state, list, value);
	}

	// The reverse owner record must name the live Listview that actually adopts
	// this List. A valid cookie alone is insufficient: a stale or unrelated
	// parent pointer could otherwise receive the child's selection signal.
	private static bool IsValidListviewOwner<TPlatform>(ref TPlatform platform,
		APTR state, APTR list, MuiListviewOwnerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (list.IsNull || value.Owner.IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state, list).IsNull ||
			MuiHeadlessObjectCore.FindObject(ref platform, state,
				value.Owner).IsNull ||
			Classify(ref platform, state, list) != MuiCollectionClass.List ||
			Classify(ref platform, state, value.Owner) !=
				MuiCollectionClass.Listview)
			return false;
		return MuiListviewCore.ChildList(ref platform, state, value.Owner).Raw ==
			list.Raw;
	}

	internal static bool SetListviewOwner<TPlatform>(ref TPlatform platform,
		APTR state, APTR list, APTR owner)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (list.IsNull) return false;
		if (owner.IsNotNull)
		{
			var candidate = default(MuiListviewOwnerState);
			candidate.Magic = MuiListviewOwnerState.Cookie;
			candidate.Owner = owner;
			if (!IsValidListviewOwner(ref platform, state, list, candidate))
				return false;
		}
		var hasRaw = MuiHeadlessObjectCore.GetRawAttribute(ref platform, state,
			list, ListviewOwnerKey, out var rawBlock);
		var block = APTR.FromPointer(rawBlock);
		var present = hasRaw && block.IsNotNull;
		if (present)
		{
			if (!MuiListviewOwnerStateCodec.TryRead(ref platform, block,
				out var value)) return false;
			if (owner.IsNull)
			{
				FreeListviewOwnerState(ref platform, block);
				return MuiHeadlessObjectCore.SetExistingAttribute(ref platform, state,
					list, ListviewOwnerKey, 0);
			}
			value.Owner = owner;
			return MuiListviewOwnerStateCodec.Write(ref platform, block, value);
		}
		if (owner.IsNull) return true;
		block = MuiHeadlessMemory.Allocate(ref platform,
			MuiListviewOwnerState.Size);
		if (block.IsNull) return false;
		var fresh = default(MuiListviewOwnerState);
		fresh.Magic = MuiListviewOwnerState.Cookie;
		fresh.Owner = owner;
		if (!MuiListviewOwnerStateCodec.Write(ref platform, block, fresh) ||
			!MuiHeadlessObjectCore.SetAttribute(ref platform, state, list,
				ListviewOwnerKey, block.Raw, false))
		{
			MuiListviewOwnerStateCodec.Clear(ref platform, block);
			platform.Free(block, MuiListviewOwnerState.Size);
			return false;
		}
		return true;
	}

	private static void FreeListviewOwnerState<TPlatform>(ref TPlatform platform,
		APTR block) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (block.IsNull || !platform.IsMapped(block, MuiListviewOwnerState.Size))
			return;
		MuiListviewOwnerStateCodec.Clear(ref platform, block);
		platform.Free(block, MuiListviewOwnerState.Size);
	}

	private static void ToggleSelectChange<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSelectionSignalAdmission(ref platform, state, obj,
			out var signal, out var present)) return;
		if (!present)
		{
			if (!EnsureSelectionSignalState(ref platform, state, obj) ||
				!TryReadSelectionSignalAdmission(ref platform, state, obj,
					out signal, out present) || !present) return;
		}
		var value = signal.Value;
		var next = value == 0 ? 1u : 0u;
		var block = APTR.FromPointer(Read(ref platform, state, obj,
			SelectionSignalKey, 0));
		signal.Value = next;
		if (!WriteSelectionSignalState(ref platform, block, signal)) return;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			SelectChange, next, true))
		{
			signal.Value = value;
			WriteSelectionSignalState(ref platform, block, signal);
			return;
		}
		// Listview exposes the same selection-change signal as its owned List.
		// Mirror once at the parent boundary; the parent has no owner link, so
		// this cannot recurse back into the child.
		if (TryGetListviewOwner(ref platform, state, obj, out var ownerState) &&
			ownerState.Owner.IsNotNull && Classify(ref platform, state,
				ownerState.Owner) ==
			MuiCollectionClass.Listview)
			MuiListviewCore.ToggleSelectionSignal(ref platform, state,
				ownerState.Owner);
	}

	private static uint SelectionSignalValue<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadSelectionSignalAdmission(ref platform, state, obj,
			out var signal, out var present) && present ? signal.Value : 0;
	}

	internal static bool TryGetSelectionSignal<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiListSelectionSignalState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadSelectionSignalAdmission(ref platform, state, obj,
			out value, out var present)) return false;
		return present;
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
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// A public List projection can call into this helper while its named
		// record is still being located. Read the scalar backing value directly
		// for those attributes to keep construction/layout paths non-recursive;
		// other attributes retain the established class-aware resolver behavior.
		if (IsPublicGetterAttribute(attribute))
			return MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj,
				attribute, out var raw) ? raw : fallback;
		return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var value) ? value : fallback;
	}

	// Internal projection code sometimes needs the raw backing value while the
	// public Get dispatcher is resolving a named List record. Keeping this seam
	// explicit prevents a getter from re-entering itself without changing the
	// established class-aware Read behavior used by mutation and layout paths.
	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint fallback)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
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
		if (MuiHeadlessObjectCore.GetRawAttribute(ref platform, state, obj, attribute,
			out var current) && current == value) return;
		MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, true);
	}

	// Layout has already written its normalized public value before the viewport
	// record is refreshed, so SetNotify cannot infer the prior value at this
	// point. Keep the prior/current comparison explicit at the layout boundary
	// and reuse the
	// ordinary named notification core for the actual dispatch.
	private static void NotifyViewportTransition<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, uint attribute,
		uint previous, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (previous == value) return;
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
