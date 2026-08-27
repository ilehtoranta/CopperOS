/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

namespace CopperOS.MuiMaster;

// Guest-resident Stringscroll runtime state exposed as one named record. The
// public String attribute still points at the object-owned guest buffer; the
// metric and pixel-scroll fields are private implementation values, but their
// lifetime and invariants are expressed here rather than as positional fields.
public struct MuiStringscrollState
{
	public APTR String;
	public uint ContentWidth;
	public uint ContentHeight;
	public uint ScrollX;
	public uint ScrollY;
}

// Guest-resident Stringscroll content/scroll state. String points at the
// object-owned Dataspace copy; metrics and pixel offsets are kept together so
// recompute, input, drawing, and getters share one typed projection.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollStateRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x53535354u; // 'SSST'

	internal uint Magic;
	internal APTR String;
	internal uint ContentWidth;
	internal uint ContentHeight;
	internal uint ScrollX;
	internal uint ScrollY;
}

internal enum MuiStringscrollStateField : byte
{
	Magic,
	String,
	ContentWidth,
	ContentHeight,
	ScrollX,
	ScrollY,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollStateFieldCursor
{
	internal APTR Record;
	internal MuiStringscrollStateField Field;
}

internal static class MuiStringscrollStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringscrollStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiStringscrollStateField.Magic: offset = 0; return true;
			case MuiStringscrollStateField.String: offset = 4; return true;
			case MuiStringscrollStateField.ContentWidth: offset = 8; return true;
			case MuiStringscrollStateField.ContentHeight: offset = 12; return true;
			case MuiStringscrollStateField.ScrollX: offset = 16; return true;
			case MuiStringscrollStateField.ScrollY: offset = 20; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringscrollStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiStringscrollStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringscrollStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringscrollStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringscrollStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiStringscrollStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollStateRecord.Size) ||
			!MuiStringscrollStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollStateField.Magic, out var magic))
			return false;
		value.Magic = magic;
		if (!MuiStringscrollStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollStateField.String, out var text) ||
			!MuiStringscrollStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollStateField.ContentWidth,
				out value.ContentWidth) ||
			!MuiStringscrollStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollStateField.ContentHeight,
				out value.ContentHeight) ||
			!MuiStringscrollStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollStateField.ScrollX, out value.ScrollX) ||
			!MuiStringscrollStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollStateField.ScrollY, out value.ScrollY))
			return false;
		value.String = APTR.FromPointer(text);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringscrollStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiStringscrollStateRecord.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollStateRecord.Size) || value.Magic !=
			MuiStringscrollStateRecord.Cookie) return false;
		return MuiStringscrollStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollStateField.Magic, value.Magic) &&
			MuiStringscrollStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollStateField.String, value.String.Raw) &&
			MuiStringscrollStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollStateField.ContentWidth,
				value.ContentWidth) &&
			MuiStringscrollStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollStateField.ContentHeight,
				value.ContentHeight) &&
			MuiStringscrollStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollStateField.ScrollX, value.ScrollX) &&
			MuiStringscrollStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollStateField.ScrollY, value.ScrollY);
	}
}

// Stringscroll policy flags are the public BOOL attributes that control
// minimum-size, border, and input behavior. HorizBar and VertBar are
// initializer-only object pointers and live in the separate typed scrollbar
// record below. Keeping the BOOL policy together prevents layout/input
// consumers from silently disagreeing about a canonical value.
public struct MuiStringscrollPolicyState
{
	public uint HorizBar;
	public uint NoInput;
	public uint SetMin;
	public uint SetVMin;
	public uint UseWinBorder;
	public uint VertBar;
	public uint VertScrollerOnly;
}

// Guest-resident canonical BOOL policy for Stringscroll. The public policy
// attributes are mirrored for ABI compatibility, while all policy consumers
// use this named record after construction. Keeping the flags together
// prevents input, layout, and scrollbar drawing from drifting across raw
// words; object-pointer identity is kept by MuiStringscrollScrollbarRecord.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollPolicyRecord
{
	internal const uint Size = 32;
	internal const uint Cookie = 0x5353504Cu; // 'SSPL'

	internal uint Magic;
	internal uint HorizBar;
	internal uint NoInput;
	internal uint SetMin;
	internal uint SetVMin;
	internal uint UseWinBorder;
	internal uint VertBar;
	internal uint VertScrollerOnly;
}

internal enum MuiStringscrollPolicyField : byte
{
	Magic,
	HorizBar,
	NoInput,
	SetMin,
	SetVMin,
	UseWinBorder,
	VertBar,
	VertScrollerOnly,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollPolicyFieldCursor
{
	internal APTR Record;
	internal MuiStringscrollPolicyField Field;
}

internal static class MuiStringscrollPolicyFieldCursorCodec
{
	private static bool TryResolve(MuiStringscrollPolicyField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiStringscrollPolicyField.Magic: offset = 0; return true;
			case MuiStringscrollPolicyField.HorizBar: offset = 4; return true;
			case MuiStringscrollPolicyField.NoInput: offset = 8; return true;
			case MuiStringscrollPolicyField.SetMin: offset = 12; return true;
			case MuiStringscrollPolicyField.SetVMin: offset = 16; return true;
			case MuiStringscrollPolicyField.UseWinBorder: offset = 20; return true;
			case MuiStringscrollPolicyField.VertBar: offset = 24; return true;
			case MuiStringscrollPolicyField.VertScrollerOnly: offset = 28; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringscrollPolicyFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiStringscrollPolicyRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollPolicyField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringscrollPolicyFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollPolicyField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringscrollPolicyFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringscrollPolicyRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiStringscrollPolicyRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollPolicyRecord.Size) ||
			!MuiStringscrollPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollPolicyField.Magic, out var magic))
			return false;
		value.Magic = magic;
		if (!MuiStringscrollPolicyFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollPolicyField.HorizBar, out value.HorizBar) ||
			!MuiStringscrollPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollPolicyField.NoInput, out value.NoInput) ||
			!MuiStringscrollPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollPolicyField.SetMin, out value.SetMin) ||
			!MuiStringscrollPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollPolicyField.SetVMin, out value.SetVMin) ||
			!MuiStringscrollPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollPolicyField.UseWinBorder,
				out value.UseWinBorder) ||
			!MuiStringscrollPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollPolicyField.VertBar, out value.VertBar) ||
			!MuiStringscrollPolicyFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollPolicyField.VertScrollerOnly,
				out value.VertScrollerOnly))
			return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringscrollPolicyRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiStringscrollPolicyRecord.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollPolicyRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollPolicyRecord.Size) || value.Magic !=
			MuiStringscrollPolicyRecord.Cookie) return false;
		return MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollPolicyField.Magic, value.Magic) &&
			MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollPolicyField.HorizBar, value.HorizBar) &&
			MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollPolicyField.NoInput, value.NoInput) &&
			MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollPolicyField.SetMin, value.SetMin) &&
			MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollPolicyField.SetVMin, value.SetVMin) &&
			MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollPolicyField.UseWinBorder,
				value.UseWinBorder) &&
			MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollPolicyField.VertBar, value.VertBar) &&
			MuiStringscrollPolicyFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollPolicyField.VertScrollerOnly,
				value.VertScrollerOnly);
	}
}

// The named policy stores normalized MorphOS BOOL projections. The codec keeps
// wire fields lossless for corruption tests, while semantic admission rejects
// values outside the canonical 0/1 domain before policy consumers normalize or
// act on them.
internal static class MuiStringscrollPolicyValidation
{
	internal static bool IsValidRecord(MuiStringscrollPolicyRecord value) =>
		value.HorizBar <= 1 && value.NoInput <= 1 && value.SetMin <= 1 &&
		value.SetVMin <= 1 && value.UseWinBorder <= 1 && value.VertBar <= 1 &&
		value.VertScrollerOnly <= 1;

	internal static bool IsValidState(MuiStringscrollPolicyState value) =>
		value.HorizBar <= 1 && value.NoInput <= 1 && value.SetMin <= 1 &&
		value.SetVMin <= 1 && value.UseWinBorder <= 1 && value.VertBar <= 1 &&
		value.VertScrollerOnly <= 1;
}

// Stringscroll's HorizBar and VertBar attributes are initializer-only
// scrollbar object pointers in MorphOS MUI. Keep those APTR values in their
// own named guest-resident record; the policy record below continues to hold
// normalized BOOL enable flags for layout and input decisions.
public struct MuiStringscrollScrollbarState
{
	public APTR HorizBar;
	public APTR VertBar;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollScrollbarRecord
{
	internal const uint Size = 12;
	internal const uint Cookie = 0x53534252u; // 'SSBR'

	internal uint Magic;
	internal APTR HorizBar;
	internal APTR VertBar;
}

internal enum MuiStringscrollScrollbarField : byte
{
	Magic,
	HorizBar,
	VertBar,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollScrollbarFieldCursor
{
	internal APTR Record;
	internal MuiStringscrollScrollbarField Field;
}

internal static class MuiStringscrollScrollbarFieldCursorCodec
{
	private static bool TryResolve(MuiStringscrollScrollbarField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiStringscrollScrollbarField.Magic: offset = 0; return true;
			case MuiStringscrollScrollbarField.HorizBar: offset = 4; return true;
			case MuiStringscrollScrollbarField.VertBar: offset = 8; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringscrollScrollbarFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiStringscrollScrollbarRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollScrollbarField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringscrollScrollbarFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollScrollbarField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringscrollScrollbarFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringscrollScrollbarRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiStringscrollScrollbarRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollScrollbarRecord.Size) ||
			!MuiStringscrollScrollbarFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollScrollbarField.Magic, out var magic))
			return false;
		value.Magic = magic;
		if (!MuiStringscrollScrollbarFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollScrollbarField.HorizBar, out var horizBar) ||
			!MuiStringscrollScrollbarFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollScrollbarField.VertBar, out var vertBar))
			return false;
		value.HorizBar = APTR.FromPointer(horizBar);
		value.VertBar = APTR.FromPointer(vertBar);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringscrollScrollbarRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiStringscrollScrollbarRecord.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollScrollbarRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollScrollbarRecord.Size) || value.Magic !=
			MuiStringscrollScrollbarRecord.Cookie) return false;
		return MuiStringscrollScrollbarFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollScrollbarField.Magic, value.Magic) &&
			MuiStringscrollScrollbarFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollScrollbarField.HorizBar,
				value.HorizBar.Raw) &&
			MuiStringscrollScrollbarFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollScrollbarField.VertBar,
				value.VertBar.Raw);
	}
}

// The optional child scrollbar objects are kept in a separate named record.
// Supplied MorphOS scrollbar pointers are caller-owned; automatically-created
// children are marked in OwnedMask and retired by Stringscroll cleanup.
public struct MuiStringscrollCompositionState
{
	public APTR Horizontal;
	public APTR Vertical;
	public uint OwnedMask;

	public const uint HorizontalOwned = 1u;
	public const uint VerticalOwned = 2u;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollCompositionRecord
{
	internal const uint Size = 24;
	internal const uint Cookie = 0x53534350u; // 'SSCP'

	internal uint Magic;
	internal APTR Horizontal;
	internal APTR Vertical;
	internal uint OwnedMask;
	internal uint LastHorizontalFirst;
	internal uint LastVerticalFirst;
}

internal enum MuiStringscrollCompositionField : byte
{
	Magic,
	Horizontal,
	Vertical,
	OwnedMask,
	LastHorizontalFirst,
	LastVerticalFirst,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollCompositionFieldCursor
{
	internal APTR Record;
	internal MuiStringscrollCompositionField Field;
}

internal static class MuiStringscrollCompositionFieldCursorCodec
{
	private static bool TryResolve(MuiStringscrollCompositionField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiStringscrollCompositionField.Magic: offset = 0; return true;
			case MuiStringscrollCompositionField.Horizontal: offset = 4; return true;
			case MuiStringscrollCompositionField.Vertical: offset = 8; return true;
			case MuiStringscrollCompositionField.OwnedMask: offset = 12; return true;
			case MuiStringscrollCompositionField.LastHorizontalFirst: offset = 16; return true;
			case MuiStringscrollCompositionField.LastVerticalFirst: offset = 20; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringscrollCompositionFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiStringscrollCompositionRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollCompositionField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringscrollCompositionFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollCompositionField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringscrollCompositionFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringscrollCompositionRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiStringscrollCompositionRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollCompositionRecord.Size) ||
			!MuiStringscrollCompositionFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollCompositionField.Magic, out var magic))
			return false;
		value.Magic = magic;
		if (!MuiStringscrollCompositionFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollCompositionField.Horizontal,
			out var horizontal) ||
			!MuiStringscrollCompositionFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollCompositionField.Vertical, out var vertical) ||
			!MuiStringscrollCompositionFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollCompositionField.OwnedMask,
				out value.OwnedMask) ||
			!MuiStringscrollCompositionFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollCompositionField.LastHorizontalFirst,
				out value.LastHorizontalFirst) ||
			!MuiStringscrollCompositionFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollCompositionField.LastVerticalFirst,
				out value.LastVerticalFirst)) return false;
		value.Horizontal = APTR.FromPointer(horizontal);
		value.Vertical = APTR.FromPointer(vertical);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringscrollCompositionRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiStringscrollCompositionRecord.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollCompositionRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollCompositionRecord.Size) || value.Magic !=
			MuiStringscrollCompositionRecord.Cookie) return false;
		return MuiStringscrollCompositionFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollCompositionField.Magic, value.Magic) &&
			MuiStringscrollCompositionFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollCompositionField.Horizontal,
				value.Horizontal.Raw) &&
			MuiStringscrollCompositionFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollCompositionField.Vertical,
				value.Vertical.Raw) &&
			MuiStringscrollCompositionFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollCompositionField.OwnedMask,
				value.OwnedMask) &&
			MuiStringscrollCompositionFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollCompositionField.LastHorizontalFirst,
				value.LastHorizontalFirst) &&
			MuiStringscrollCompositionFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollCompositionField.LastVerticalFirst,
				value.LastVerticalFirst);
	}
}

// Area geometry is consumed by scrolling, bar reservation, and drawing. Keep
// the signed LONG semantics in one named record so those paths cannot drift
// by reading the public Area attributes independently.
public struct MuiStringscrollLayoutState
{
	public int Left;
	public int Top;
	public int Width;
	public int Height;
}

// Guest-resident Area geometry. The public Area attributes remain the ABI
// projection, but all Stringscroll geometry consumers share this signed,
// named record after construction and layout.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollLayoutStateRecord
{
	internal const uint Size = 20;
	internal const uint Cookie = 0x534c5954u; // 'SLYT'

	internal uint Magic;
	internal int Left;
	internal int Top;
	internal int Width;
	internal int Height;
}

internal enum MuiStringscrollLayoutStateField : byte
{
	Magic,
	Left,
	Top,
	Width,
	Height,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollLayoutStateFieldCursor
{
	internal APTR Record;
	internal MuiStringscrollLayoutStateField Field;
}

internal static class MuiStringscrollLayoutStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringscrollLayoutStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiStringscrollLayoutStateField.Magic: offset = 0; return true;
			case MuiStringscrollLayoutStateField.Left: offset = 4; return true;
			case MuiStringscrollLayoutStateField.Top: offset = 8; return true;
			case MuiStringscrollLayoutStateField.Width: offset = 12; return true;
			case MuiStringscrollLayoutStateField.Height: offset = 16; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringscrollLayoutStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiStringscrollLayoutStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollLayoutStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringscrollLayoutStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollLayoutStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringscrollLayoutStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollLayoutStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, field, out var raw))
			return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollLayoutStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, field, unchecked((uint)value));
}

internal static class MuiStringscrollLayoutStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiStringscrollLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollLayoutStateRecord.Size) ||
			!MuiStringscrollLayoutStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollLayoutStateField.Magic, out var magic))
			return false;
		value.Magic = magic;
		if (!MuiStringscrollLayoutStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiStringscrollLayoutStateField.Left, out value.Left) ||
			!MuiStringscrollLayoutStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiStringscrollLayoutStateField.Top, out value.Top) ||
			!MuiStringscrollLayoutStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiStringscrollLayoutStateField.Width, out value.Width) ||
			!MuiStringscrollLayoutStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiStringscrollLayoutStateField.Height, out value.Height))
			return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringscrollLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiStringscrollLayoutStateRecord.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollLayoutStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollLayoutStateRecord.Size) || value.Magic !=
			MuiStringscrollLayoutStateRecord.Cookie) return false;
		return MuiStringscrollLayoutStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollLayoutStateField.Magic, value.Magic) &&
			MuiStringscrollLayoutStateFieldCursorCodec.TryWriteInt32(ref platform,
			address, MuiStringscrollLayoutStateField.Left, value.Left) &&
			MuiStringscrollLayoutStateFieldCursorCodec.TryWriteInt32(ref platform,
			address, MuiStringscrollLayoutStateField.Top, value.Top) &&
			MuiStringscrollLayoutStateFieldCursorCodec.TryWriteInt32(ref platform,
			address, MuiStringscrollLayoutStateField.Width, value.Width) &&
			MuiStringscrollLayoutStateFieldCursorCodec.TryWriteInt32(ref platform,
			address, MuiStringscrollLayoutStateField.Height, value.Height);
	}
}

// RenderInfo and Font are the two guest pointers needed by Stringscroll's
// drawing path. The rastport is decoded from the named MUI_RenderInfo record;
// callers never need to know its ABI field position.
public struct MuiStringscrollRenderState
{
	public APTR RenderInfo;
	public APTR RastPort;
	public APTR Font;
}

// Guest-resident drawing context. RenderInfo and Font remain public Area
// projections; the decoded RastPort is kept with them so drawing and render
// inspection share one validated, named state record.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollRenderStateRecord
{
	internal const uint Size = 16;
	internal const uint Cookie = 0x53525452u; // 'SRTR'

	internal uint Magic;
	internal APTR RenderInfo;
	internal APTR RastPort;
	internal APTR Font;
}

internal enum MuiStringscrollRenderStateField : byte
{
	Magic,
	RenderInfo,
	RastPort,
	Font,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollRenderStateFieldCursor
{
	internal APTR Record;
	internal MuiStringscrollRenderStateField Field;
}

internal static class MuiStringscrollRenderStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringscrollRenderStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiStringscrollRenderStateField.Magic: offset = 0; return true;
			case MuiStringscrollRenderStateField.RenderInfo: offset = 4; return true;
			case MuiStringscrollRenderStateField.RastPort: offset = 8; return true;
			case MuiStringscrollRenderStateField.Font: offset = 12; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringscrollRenderStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiStringscrollRenderStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollRenderStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringscrollRenderStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollRenderStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringscrollRenderStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}
}

internal static class MuiStringscrollRenderStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiStringscrollRenderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollRenderStateRecord.Size) ||
			!MuiStringscrollRenderStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollRenderStateField.Magic, out var magic))
			return false;
		value.Magic = magic;
		if (!MuiStringscrollRenderStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollRenderStateField.RenderInfo, out var renderInfo) ||
			!MuiStringscrollRenderStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollRenderStateField.RastPort, out var rastPort) ||
			!MuiStringscrollRenderStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollRenderStateField.Font, out var font))
			return false;
		value.RenderInfo = APTR.FromPointer(renderInfo);
		value.RastPort = APTR.FromPointer(rastPort);
		value.Font = APTR.FromPointer(font);
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringscrollRenderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiStringscrollRenderStateRecord.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollRenderStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollRenderStateRecord.Size) || value.Magic !=
			MuiStringscrollRenderStateRecord.Cookie) return false;
		return MuiStringscrollRenderStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollRenderStateField.Magic, value.Magic) &&
			MuiStringscrollRenderStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollRenderStateField.RenderInfo,
			value.RenderInfo.Raw) &&
			MuiStringscrollRenderStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollRenderStateField.RastPort,
				value.RastPort.Raw) &&
			MuiStringscrollRenderStateFieldCursorCodec.TryWriteUInt32(ref platform,
				address, MuiStringscrollRenderStateField.Font, value.Font.Raw);
	}
}

// Derived viewport state is shared by recompute, scrolling, keyboard paging,
// and drawing. BOOL-like visibility fields stay canonical ULONGs at this
// boundary, while dimensions retain signed host-side geometry semantics.
public struct MuiStringscrollViewportState
{
	public int ViewportWidth;
	public int ViewportHeight;
	public uint HorizontalVisible;
	public uint VerticalVisible;
	public uint MaxScrollX;
	public uint MaxScrollY;
}

// Guest-resident derived viewport. Bar visibility, effective dimensions, and
// bounded scroll limits are published together after every content, policy, or
// layout recomputation so input and drawing consume one named projection.
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollViewportStateRecord
{
	internal const uint Size = 28;
	internal const uint Cookie = 0x53565054u; // 'SVPT'

	internal uint Magic;
	internal int ViewportWidth;
	internal int ViewportHeight;
	internal uint HorizontalVisible;
	internal uint VerticalVisible;
	internal uint MaxScrollX;
	internal uint MaxScrollY;
}

internal enum MuiStringscrollViewportStateField : byte
{
	Magic,
	ViewportWidth,
	ViewportHeight,
	HorizontalVisible,
	VerticalVisible,
	MaxScrollX,
	MaxScrollY,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiStringscrollViewportStateFieldCursor
{
	internal APTR Record;
	internal MuiStringscrollViewportStateField Field;
}

internal static class MuiStringscrollViewportStateFieldCursorCodec
{
	private static bool TryResolve(MuiStringscrollViewportStateField field,
		out uint offset)
	{
		switch (field)
		{
			case MuiStringscrollViewportStateField.Magic: offset = 0; return true;
			case MuiStringscrollViewportStateField.ViewportWidth: offset = 4; return true;
			case MuiStringscrollViewportStateField.ViewportHeight: offset = 8; return true;
			case MuiStringscrollViewportStateField.HorizontalVisible: offset = 12; return true;
			case MuiStringscrollViewportStateField.VerticalVisible: offset = 16; return true;
			case MuiStringscrollViewportStateField.MaxScrollX: offset = 20; return true;
			case MuiStringscrollViewportStateField.MaxScrollY: offset = 24; return true;
		}
		offset = 0;
		return false;
	}

	internal static bool TryGetAddress<TPlatform>(ref TPlatform platform,
		MuiStringscrollViewportStateFieldCursor cursor, out APTR address)
		where TPlatform : struct, IMuiGuestMemory
	{
		address = APTR.Null;
		if (!TryResolve(cursor.Field, out var offset) || cursor.Record.IsNull ||
			cursor.Record.Raw > uint.MaxValue - offset ||
			!platform.IsMapped(cursor.Record, MuiStringscrollViewportStateRecord.Size))
			return false;
		address = APTR.FromPointer(cursor.Record.Raw + offset);
		return platform.IsMapped(address, 4);
	}

	internal static bool TryReadUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollViewportStateField field, out uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		var cursor = default(MuiStringscrollViewportStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		value = platform.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollViewportStateField field, uint value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var cursor = default(MuiStringscrollViewportStateFieldCursor);
		cursor.Record = record;
		cursor.Field = field;
		if (!TryGetAddress(ref platform, cursor, out var address)) return false;
		platform.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryReadInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollViewportStateField field, out int value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = 0;
		if (!TryReadUInt32(ref platform, record, field, out var raw))
			return false;
		value = unchecked((int)raw);
		return true;
	}

	internal static bool TryWriteInt32<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollViewportStateField field, int value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryWriteUInt32(ref platform, record, field, unchecked((uint)value));
}

internal static class MuiStringscrollViewportStateRecordCodec
{
	internal static bool TryReadStructural<TPlatform>(ref TPlatform platform,
		APTR address,
		out MuiStringscrollViewportStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollViewportStateRecord.Size) ||
			!MuiStringscrollViewportStateFieldCursorCodec.TryReadUInt32(ref platform,
				address, MuiStringscrollViewportStateField.Magic, out var magic))
			return false;
		value.Magic = magic;
		if (!MuiStringscrollViewportStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiStringscrollViewportStateField.ViewportWidth,
			out value.ViewportWidth) ||
			!MuiStringscrollViewportStateFieldCursorCodec.TryReadInt32(ref platform,
			address, MuiStringscrollViewportStateField.ViewportHeight,
			out value.ViewportHeight) ||
			!MuiStringscrollViewportStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollViewportStateField.HorizontalVisible,
			out value.HorizontalVisible) ||
			!MuiStringscrollViewportStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollViewportStateField.VerticalVisible,
			out value.VerticalVisible) ||
			!MuiStringscrollViewportStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollViewportStateField.MaxScrollX,
			out value.MaxScrollX) ||
			!MuiStringscrollViewportStateFieldCursorCodec.TryReadUInt32(ref platform,
			address, MuiStringscrollViewportStateField.MaxScrollY,
			out value.MaxScrollY))
			return false;
		return true;
	}

	internal static bool TryRead<TPlatform>(ref TPlatform platform, APTR address,
		out MuiStringscrollViewportStateRecord value)
		where TPlatform : struct, IMuiGuestMemory =>
		TryReadStructural(ref platform, address, out value) &&
		value.Magic == MuiStringscrollViewportStateRecord.Cookie;

	internal static bool Write<TPlatform>(ref TPlatform platform, APTR address,
		MuiStringscrollViewportStateRecord value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (address.IsNull || !platform.IsMapped(address,
			MuiStringscrollViewportStateRecord.Size) || value.Magic !=
			MuiStringscrollViewportStateRecord.Cookie) return false;
		return MuiStringscrollViewportStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollViewportStateField.Magic, value.Magic) &&
			MuiStringscrollViewportStateFieldCursorCodec.TryWriteInt32(ref platform,
			address, MuiStringscrollViewportStateField.ViewportWidth,
			value.ViewportWidth) &&
			MuiStringscrollViewportStateFieldCursorCodec.TryWriteInt32(ref platform,
			address, MuiStringscrollViewportStateField.ViewportHeight,
			value.ViewportHeight) &&
			MuiStringscrollViewportStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollViewportStateField.HorizontalVisible,
			value.HorizontalVisible) &&
			MuiStringscrollViewportStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollViewportStateField.VerticalVisible,
			value.VerticalVisible) &&
			MuiStringscrollViewportStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollViewportStateField.MaxScrollX,
			value.MaxScrollX) &&
			MuiStringscrollViewportStateFieldCursorCodec.TryWriteUInt32(ref platform,
			address, MuiStringscrollViewportStateField.MaxScrollY,
			value.MaxScrollY);
	}
}

// One neutral scrollbar track/thumb geometry. Keeping the complete draw
// rectangle together avoids recomputing one axis from private store fields at
// each call site and leaves the proportional arithmetic as a small, named
// value-type seam.
internal struct MuiStringscrollBarGeometry
{
	internal int TrackLeft;
	internal int TrackTop;
	internal int TrackRight;
	internal int TrackBottom;
	internal int ThumbLeft;
	internal int ThumbTop;
	internal int ThumbRight;
	internal int ThumbBottom;
}

// Input geometry for either the legacy neutral track or a composed
// Scrollbar child. The axis ranges are named so pointer routing never has to
// rediscover arrow/Prop/thumb positions from raw attributes.
internal struct MuiStringscrollInputBarGeometry
{
	internal int TrackStart;
	internal int TrackEnd;
	internal int ThumbStart;
	internal int ThumbEnd;
	internal int FirstArrowStart;
	internal int FirstArrowEnd;
	internal int SecondArrowStart;
	internal int SecondArrowEnd;
	internal int CrossStart;
	internal int CrossEnd;
	internal int FirstDelta;
	internal int SecondDelta;
	internal uint Horizontal;
	internal uint HasArrows;
}

// Stringscroll.mui (MorphOS 3.20). This is a deliberately bounded
// implementation of the scrolling string gadget. The string is copied into
// guest-owned dataspace, its content metrics are derived without a managed
// text object, and the visible text is clipped and drawn through the existing
// graphics seam. Optional Scrollbar children are also guest objects with
// named ownership/state records; caller-supplied bars are never disposed.
// Scroll offsets are pixel based and remain clamped whenever the string,
// layout, or scrolling policy changes. No managed allocations, exceptions,
// delegates, or host services are used here.
public static class MuiStringscrollCore
{
	// ---- Public attributes (mui.h / MorphOS Stringscroll.mui) ---------------
	public const uint HorizBar = 0x8042e049u;
	public const uint NoInput = 0x8042b2f3u;
	public const uint SetMin = 0x8042cbbbu;
	public const uint SetVMin = 0x80420115u;
	public const uint String = 0x804256a2u;
	public const uint UseWinBorder = 0x80422a61u;
	public const uint VertBar = 0x804232f8u;
	public const uint VertScrollerOnly = 0x8042873bu;

	// Shared Area attributes.
	private const uint Width = 0x8042b59cu;
	private const uint Height = 0x80423237u;
	private const uint LeftEdge = 0x8042bec6u;
	private const uint TopEdge = 0x8042509bu;
	private const uint RightEdge = 0x8042ba82u;
	private const uint BottomEdge = 0x8042e552u;
	private const uint RenderInfo = 0x7fff0001u;
	private const uint Font = 0x8042be50u;

	// Private guest-resident state. These keys are internal to CopperOS and are
	// retired by MuiStoreCore when the object is disposed.
	private const uint StringKey = 0x0f110001u;
	private const uint ContentWidthKey = 0x0f110002u;
	private const uint ContentHeightKey = 0x0f110003u;
	private const uint ScrollXKey = 0x0f110004u;
	private const uint ScrollYKey = 0x0f110005u;
	private const uint PointerStateKey = 0x0f110006u;
	private const uint PolicyStateKey = 0x0f110007u;
	private const uint StateRecordKey = 0x0f110008u;
	private const uint LayoutStateKey = 0x0f110009u;
	private const uint RenderStateKey = 0x0f11000au;
	private const uint ViewportStateKey = 0x0f11000bu;
	private const uint ScrollbarStateKey = 0x0f11000cu;
	private const uint CompositionStateKey = 0x0f11000du;

	private const uint MaximumStringLength = 65536;
	private const uint CharacterWidth = 8;
	private const uint CharacterHeight = 8;
	private const int ScrollerExtent = 12;
	private const uint MaximumDimension = 10000;
	// Scrollbar type values shared by the typed Scrollbar layout record. Keep
	// these symbolic so input routing does not depend on unexplained offsets.
	private const uint ScrollbarTypeBottom = 1u;
	private const uint ScrollbarTypeTop = 2u;
	private const uint ScrollbarTypeNone = 4u;

	// Intuition mouse-button envelope values used by the pointer part of
	// MUIM_HandleInput.  Stringscroll commits a track click on SELECTUP; the
	// pointer payload itself is decoded through MuiIntuiPointerMessage below.
	private const int KeyNone = -1;
	private const int KeyRelease = -2;
	private const int KeyUp = 2;
	private const int KeyDown = 3;
	private const int KeyPageUp = 4;
	private const int KeyPageDown = 5;
	private const int KeyTop = 6;
	private const int KeyBottom = 7;
	private const int KeyLeft = 8;
	private const int KeyRight = 9;
	private const uint IdcmpMouseButtons = 1u << 3;
	private const uint IdcmpMouseMove = 1u << 2;
	private const ushort SelectDown = 0x0068;
	private const ushort SelectUp = 0x0069;

	// ---- Construction and ownership -----------------------------------------

	public static APTR CreateStringscroll<TPlatform>(ref TPlatform platform,
		APTR state, APTR classRecord, APTR tags)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.ClassifyRecord(ref platform, classRecord) !=
			MuiCollectionClass.Stringscroll) return APTR.Null;
		var obj = MuiHeadlessObjectCore.CreateObjectA(ref platform, state,
			classRecord, tags);
		if (obj.IsNull) return APTR.Null;
		if (!Setup(ref platform, state, obj, tags))
		{
			MuiCollectionLifecycle.DisposeObject(ref platform, state, obj);
			return APTR.Null;
		}
		return obj;
	}

	private static bool Setup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR tags) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		var horizontalBar = APTR.Null;
		var verticalBar = APTR.Null;
		if (!TryGetInitializerPointer(ref platform, tags, HorizBar,
			out horizontalBar) || !TryGetInitializerPointer(ref platform, tags,
			VertBar, out verticalBar)) return false;
		EnsureDefault(ref platform, state, record, HorizBar, 1);
		EnsureDefault(ref platform, state, record, NoInput, 0);
		EnsureDefault(ref platform, state, record, SetMin, 0);
		EnsureDefault(ref platform, state, record, SetVMin, 0);
		EnsureDefault(ref platform, state, record, UseWinBorder, 0);
		EnsureDefault(ref platform, state, record, VertBar, 1);
		EnsureDefault(ref platform, state, record, VertScrollerOnly, 0);
		if (!EnsureScrollbarRecord(ref platform, state, obj, horizontalBar,
			verticalBar))
			return false;
		if (!NormalizePolicyState(ref platform, state, record)) return false;
		if (!EnsurePolicyRecord(ref platform, state, obj, record)) return false;
		if (!EnsureCompositionRecord(ref platform, state, obj)) return false;
		SetRaw(ref platform, state, record, ScrollXKey, 0, false);
		SetRaw(ref platform, state, record, ScrollYKey, 0, false);

		var source = APTR.FromPointer(ReadRaw(ref platform, record, String, 0));
		if (source.IsNotNull && !OwnString(ref platform, state, obj, source))
			return false;
		var copy = MuiStoreCore.DataspaceFind(ref platform, state, obj, StringKey);
		if (!SetRaw(ref platform, state, record, String, copy.Raw, false))
			return false;
		if (!EnsureStateRecord(ref platform, state, obj)) return false;
		if (!EnsureLayoutRecord(ref platform, state, obj)) return false;
		if (!EnsureRenderRecord(ref platform, state, obj)) return false;
		return Recompute(ref platform, state, obj);
	}

	private static bool TryGetInitializerPointer<TPlatform>(ref TPlatform platform,
		APTR tags, uint requested, out APTR value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = APTR.Null;
		var cursor = default(MuiAslTagItemCursor);
		cursor.Base = tags;
		uint visited = 0;
		while (cursor.Base.IsNotNull && visited++ < MuiAslTagListCore.MaximumSteps)
		{
			if (!MuiAslTagItemVectorCodec.TryGetEntry(ref platform, cursor,
				out var current) || !MuiAslTagItemCodec.TryRead(ref platform, current,
				out var item)) return false;
			if (item.Tag == MuiAslTagListCore.TagDone) return true;
			if (item.Tag == MuiAslTagListCore.TagMore)
			{
				if (item.Data == 0) return true;
				cursor.Base = APTR.FromPointer(item.Data);
				cursor.Index = 0;
				continue;
			}
			if (item.Tag == MuiAslTagListCore.TagSkip)
			{
				if (item.Data == uint.MaxValue ||
					!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, item.Data + 1))
					return false;
				continue;
			}
			if (item.Tag != MuiAslTagListCore.TagIgnore && item.Tag == requested)
			{
				value = APTR.FromPointer(item.Data);
				return true;
			}
		if (!MuiAslTagItemVectorCodec.TryAdvance(ref cursor, 1)) return false;
		}
		return tags.IsNull;
	}

	private static bool OwnString<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR source) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!CStringCodec.TryReadLength(ref platform, source,
			MaximumStringLength, out var length)) return false;
		return MuiStoreCore.DataspaceAdd(ref platform, state, obj, StringKey,
			source, unchecked((int)(length + 1)));
	}

	// ---- Generic object seams -------------------------------------------------

	// Called by MuiHeadlessObjectCore before its generic attribute store. It
	// claims only Stringscroll attributes so unrelated Area attributes continue
	// through the normal object store.
	internal static bool TrySetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var classRecord = objectValue.Class;
		if (MuiListCore.ClassifyRecord(ref platform, classRecord) !=
			MuiCollectionClass.Stringscroll) return false;
		var obj = objectValue.Boopsi;
		return SetKnown(ref platform, state, record, obj, attribute, value, notify);
	}

	internal static bool TryGetAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return false;
		var classRecord = objectValue.Class;
		if (MuiListCore.ClassifyRecord(ref platform, classRecord) !=
			MuiCollectionClass.Stringscroll) return false;
		var obj = objectValue.Boopsi;
		if (attribute == String)
		{
			// The public String getter must use the canonical named state rather
			// than exposing the private ownership key directly.  This keeps a
			// malformed present record from leaking a stale pointer and verifies
			// that the object-owned copy and its typed projection still agree.
			if (!TryReadOwnedString(ref platform, state, obj, out var owned))
				return false;
			value = owned.Raw;
			return true;
		}
		if (!IsPolicyAttribute(attribute) ||
			!TryReadPolicyState(ref platform, state, obj, out var policy))
			return false;
		value = attribute == HorizBar ? policy.HorizBar :
			attribute == NoInput ? policy.NoInput :
			attribute == SetMin ? policy.SetMin :
			attribute == SetVMin ? policy.SetVMin :
			attribute == UseWinBorder ? policy.UseWinBorder :
			attribute == VertBar ? policy.VertBar : policy.VertScrollerOnly;
		return true;
	}

	internal static bool IsPolicyAttribute(uint attribute) =>
		attribute == HorizBar || attribute == NoInput || attribute == SetMin ||
		attribute == SetVMin || attribute == UseWinBorder ||
		attribute == VertBar || attribute == VertScrollerOnly;

	internal static bool IsPublicGetterAttribute(uint attribute) =>
		attribute == String || IsPolicyAttribute(attribute);

	public static bool SetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, uint value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Stringscroll)
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, notify);
		// A recognized Stringscroll attribute must not fall through to the raw
		// object store when its typed transition fails.  Falling through would
		// report success while bypassing the named state and could publish a
		// replacement pointer after malformed-state rejection.
		if (IsKnownAttribute(attribute))
			return SetKnown(ref platform, state, record, obj, attribute, value,
				notify);
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj, attribute,
			value, notify);
	}

	private static bool IsKnownAttribute(uint attribute) =>
		attribute == String || IsPolicyAttribute(attribute) || attribute == Width ||
		attribute == Height || attribute == RenderInfo || attribute == Font;

	// Public OM_SET is narrower than the internal class-aware setter. MorphOS
	// exposes the Stringscroll text as a getter and all scrollbar/input policy
	// attributes as construction-only; keep those writes on the initialization
	// path while allowing unrelated Area attributes to use the generic store.
	public static bool SetRuntimeAttribute<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, uint attribute, uint value, bool notify = false)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (MuiListCore.Classify(ref platform, state, obj) !=
			MuiCollectionClass.Stringscroll)
			return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
				attribute, value, notify);
		if (attribute == String || IsPolicyAttribute(attribute)) return false;
		return MuiHeadlessObjectCore.SetAttribute(ref platform, state, obj,
			attribute, value, notify);
	}

	public static bool GetAttribute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint attribute, out uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = 0;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		var isStringscroll = MuiListCore.Classify(ref platform, state, obj) ==
			MuiCollectionClass.Stringscroll;
		if (isStringscroll && IsPublicGetterAttribute(attribute))
			return TryGetAttribute(ref platform, state, record, attribute, out value);
		if (TryGetAttribute(ref platform, state, record, attribute, out value))
			return true;
		return MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj,
			attribute, out value);
	}

	private static bool SetKnown<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, APTR obj, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (attribute == String)
		{
			// String replacement is a named-state transition.  Validate the current
			// guest record before allocating or publishing a replacement so malformed
			// state cannot lose the live owned buffer as a side effect of a failed set.
			// TryReadState intentionally has a raw-store fallback for compatibility
			// with objects created before the typed record existed.  Once a record is
			// present, however, a malformed length or payload is an admission failure,
			// not permission to fall back to raw state.
			var stateRecord = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				StateRecordKey);
			var stateRecordLength = MuiStoreCore.DataspaceLength(ref platform, state,
				obj, StateRecordKey);
			if (stateRecord.IsNotNull || stateRecordLength != 0)
			{
				if (stateRecordLength != unchecked((int)MuiStringscrollStateRecord.Size) ||
					!MuiStringscrollStateRecordCodec.TryRead(ref platform, stateRecord,
						out _)) return false;
			}
			if (!TryReadState(ref platform, state, obj, out var currentState))
				return false;
			if (!currentState.String.IsNull && !CStringCodec.TryReadLength(
				ref platform, currentState.String, MaximumStringLength, out _))
				return false;
			// Recompute consumes the named layout and composition records after the
			// owned string has been replaced.  Admit those value-type inputs before
			// OwnString can retire the previous guest buffer; a malformed dependent
			// record must not turn a failed setter into a successful ownership
			// transition followed by a false return.
			if (!ValidateRecomputeInputs(ref platform, state, obj)) return false;
			var source = APTR.FromPointer(value);
			if (source.IsNull)
			{
				MuiStoreCore.DataspaceRemove(ref platform, state, obj, StringKey);
				if (!SetRaw(ref platform, state, record, String, 0, notify))
					return false;
			}
			else
			{
				if (!OwnString(ref platform, state, obj, source)) return false;
				var copy = MuiStoreCore.DataspaceFind(ref platform, state, obj,
					StringKey);
				if (!SetRaw(ref platform, state, record, String, copy.Raw, notify))
					return false;
			}
			if (!SyncStateRecord(ref platform, state, obj)) return false;
			return Recompute(ref platform, state, obj);
		}
		if (attribute == HorizBar || attribute == NoInput || attribute == SetMin ||
			attribute == SetVMin || attribute == UseWinBorder || attribute == VertBar ||
			attribute == VertScrollerOnly)
		{
			// Policy reads retain a raw-store fallback for compatibility with older
			// objects.  Once the named policy record exists, malformed state must be
			// rejected before WritePolicyState can publish a partial raw projection.
			var policyBlock = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				PolicyStateKey);
			var policyLength = MuiStoreCore.DataspaceLength(ref platform, state,
				obj, PolicyStateKey);
			if (policyBlock.IsNotNull || policyLength != 0)
			{
				if (policyLength != unchecked((int)MuiStringscrollPolicyRecord.Size) ||
					!MuiStringscrollPolicyRecordCodec.TryRead(ref platform, policyBlock,
						out _)) return false;
			}
			if (!TryReadPolicyState(ref platform, state, obj, out var policy))
				return false;
			var scrollbars = default(MuiStringscrollScrollbarState);
			if ((attribute == HorizBar || attribute == VertBar) &&
				!TryReadScrollbarState(ref platform, state, obj,
					out scrollbars)) return false;
			if (attribute == HorizBar || attribute == VertBar)
			{
				// Changing a scrollbar policy also reconciles the dependent child
				// composition.  A present composition record must therefore validate
				// before any policy or scrollbar projection is published; absence is
				// retained as the lazy-construction case handled by ReconcileComposition.
				var compositionBlock = MuiStoreCore.DataspaceFind(ref platform, state,
					obj, CompositionStateKey);
				var compositionLength = MuiStoreCore.DataspaceLength(ref platform,
					state, obj, CompositionStateKey);
				if (compositionBlock.IsNotNull || compositionLength != 0)
				{
					if (compositionLength != unchecked((int)
						MuiStringscrollCompositionRecord.Size) ||
						!MuiStringscrollCompositionRecordCodec.TryRead(ref platform,
							compositionBlock, out _)) return false;
				}
			}
			// Every policy transition ends in Recompute.  Admit the same named
			// layout/composition inputs before publishing policy projections so a
			// malformed dependent record cannot leave a policy change visible after
			// the recompute path returns failure.
			if (!ValidateRecomputeInputs(ref platform, state, obj)) return false;
			var normalized = value == 0 ? 0u : 1u;
			if (attribute == HorizBar)
			{
				policy.HorizBar = normalized;
				scrollbars.HorizBar = APTR.FromPointer(value);
			}
			else if (attribute == NoInput) policy.NoInput = normalized;
			else if (attribute == SetMin) policy.SetMin = normalized;
			else if (attribute == SetVMin) policy.SetVMin = normalized;
			else if (attribute == UseWinBorder) policy.UseWinBorder = normalized;
			else if (attribute == VertBar)
			{
				policy.VertBar = normalized;
				scrollbars.VertBar = APTR.FromPointer(value);
			}
			else policy.VertScrollerOnly = normalized;
			if (!WritePolicyState(ref platform, state, record, policy, attribute,
				notify)) return false;
			if ((attribute == HorizBar || attribute == VertBar) &&
				!WriteScrollbarState(ref platform, state, obj, scrollbars))
				return false;
			if (!WritePolicyRecord(ref platform, state, obj, policy)) return false;
			if ((attribute == HorizBar || attribute == VertBar) &&
				!ReconcileComposition(ref platform, state, obj, scrollbars, policy))
				return false;
			return Recompute(ref platform, state, obj);
		}
		if (attribute == Width || attribute == Height)
		{
			// Geometry is a named-state transition.  An existing layout record must
			// be valid before the public Area projection is changed; otherwise a
			// malformed record could leave raw Width/Height ahead of typed state.
			var layoutBlock = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				LayoutStateKey);
			var layoutLength = MuiStoreCore.DataspaceLength(ref platform, state,
				obj, LayoutStateKey);
			var hasLayoutRecord = layoutBlock.IsNotNull || layoutLength != 0;
			var previousLayout = default(MuiStringscrollLayoutStateRecord);
			if (hasLayoutRecord && (layoutLength !=
				unchecked((int)MuiStringscrollLayoutStateRecord.Size) ||
				!MuiStringscrollLayoutStateRecordCodec.TryRead(ref platform, layoutBlock,
					out previousLayout))) return false;
			var previousRaw = ReadRaw(ref platform, record, attribute, 0);
			if (!SetRaw(ref platform, state, record, attribute, value, notify))
				return false;
			if (!SyncLayoutRecord(ref platform, state, obj))
			{
				SetRaw(ref platform, state, record, attribute, previousRaw, false);
				if (hasLayoutRecord)
					MuiStringscrollLayoutStateRecordCodec.Write(ref platform,
						layoutBlock, previousLayout);
				return false;
			}
			return Recompute(ref platform, state, obj);
		}
		if (attribute == RenderInfo || attribute == Font)
		{
			// Render context follows the same named-record admission rule as
			// geometry.  Keep the public pointer and decoded RastPort record
			// coherent if a malformed existing record is encountered.
			var renderBlock = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				RenderStateKey);
			var renderLength = MuiStoreCore.DataspaceLength(ref platform, state,
				obj, RenderStateKey);
			var hasRenderRecord = renderBlock.IsNotNull || renderLength != 0;
			var previousRender = default(MuiStringscrollRenderStateRecord);
			if (hasRenderRecord && (renderLength !=
				unchecked((int)MuiStringscrollRenderStateRecord.Size) ||
				!MuiStringscrollRenderStateRecordCodec.TryRead(ref platform, renderBlock,
					out previousRender))) return false;
			var previousRaw = ReadRaw(ref platform, record, attribute, 0);
			if (!SetRaw(ref platform, state, record, attribute, value, notify))
				return false;
			if (!SyncRenderRecord(ref platform, state, obj))
			{
				SetRaw(ref platform, state, record, attribute, previousRaw, false);
				if (hasRenderRecord)
					MuiStringscrollRenderStateRecordCodec.Write(ref platform,
						renderBlock, previousRender);
				return false;
			}
			return true;
		}
		return false;
	}

	// ---- Metrics and bounded scrolling ---------------------------------------

	private static bool ValidateRecomputeInputs<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadLayoutState(ref platform, state, obj, out _)) return false;
		if (!TryReadCompositionAdmission(ref platform, state, obj,
			out var composition, out var present)) return false;
		if (!present) return true;
		return ValidateCompositionChild(ref platform, state, composition.Horizontal) &&
			ValidateCompositionChild(ref platform, state, composition.Vertical);
	}

	private static bool ValidateCompositionChild<TPlatform>(ref TPlatform platform,
		APTR state, APTR child)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (child.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			child).IsNull) return true;
		return MuiCommonControlCore.TryReadPropRangeState(ref platform, state,
			child, out _);
	}

	public static bool Recompute<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadState(ref platform, state, obj, out var scrollState))
			return false;
		var text = scrollState.String;
		if (!TryMeasureUtf8(ref platform, text, out var maxWidth, out var lines))
			return false;
		scrollState.ContentWidth = SaturatingDimension(maxWidth * CharacterWidth);
		scrollState.ContentHeight = SaturatingDimension(lines * CharacterHeight);
		if (!AdoptCompositionScrollOffsets(ref platform, state, obj,
			ref scrollState)) return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !TryReadLayoutState(ref platform, state, obj,
			out var layout)) return false;
		if (!TryComputeViewportState(ref platform, record, scrollState, layout,
			out var viewport)) return false;
		if (!WriteViewportStateRecord(ref platform, state, obj, viewport))
			return false;
		if (scrollState.ScrollX > viewport.MaxScrollX)
			scrollState.ScrollX = viewport.MaxScrollX;
		if (scrollState.ScrollY > viewport.MaxScrollY)
			scrollState.ScrollY = viewport.MaxScrollY;
		if (!SyncCompositionChildren(ref platform, state, obj, scrollState,
			viewport)) return false;
		return WriteState(ref platform, state, obj, scrollState, false);
	}

	private static bool SyncCompositionChildren<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringscrollState scrollState,
		MuiStringscrollViewportState viewport)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadCompositionAdmission(ref platform, state, obj,
			out var composition, out var present)) return false;
		if (!present) return true;
		if (!SyncCompositionChild(ref platform, state, composition.Horizontal,
			viewport.HorizontalVisible != 0, scrollState.ContentWidth,
			unchecked((uint)viewport.ViewportWidth), scrollState.ScrollX,
			out composition.LastHorizontalFirst)) return false;
		if (!SyncCompositionChild(ref platform, state, composition.Vertical,
			viewport.VerticalVisible != 0, scrollState.ContentHeight,
			unchecked((uint)viewport.ViewportHeight), scrollState.ScrollY,
			out composition.LastVerticalFirst)) return false;
		return WriteCompositionRecord(ref platform, state, obj, composition);
	}

	private static bool AdoptCompositionScrollOffsets<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		ref MuiStringscrollState scrollState)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadCompositionAdmission(ref platform, state, obj,
			out var composition, out var present)) return false;
		if (!present) return true;
		var horizontal = scrollState.ScrollX;
		var vertical = scrollState.ScrollY;
		if (!AdoptCompositionChildFirst(ref platform, state, composition.Horizontal,
			composition.LastHorizontalFirst, scrollState.ScrollX,
			out horizontal) ||
			!AdoptCompositionChildFirst(ref platform, state, composition.Vertical,
			composition.LastVerticalFirst, scrollState.ScrollY,
			out vertical)) return false;
		scrollState.ScrollX = horizontal;
		scrollState.ScrollY = vertical;
		return true;
	}

	private static bool AdoptCompositionChildFirst<TPlatform>(
		ref TPlatform platform, APTR state, APTR child, uint lastPublished,
		uint parentFirst, out uint result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = parentFirst;
		if (child.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			child).IsNull) return true;
		if (!MuiCommonControlCore.TryReadPropRangeState(ref platform, state, child,
			out var range)) return false;
		if (range.First != lastPublished && range.First != parentFirst)
			result = range.First;
		return true;
	}

	private static bool SyncCompositionChild<TPlatform>(ref TPlatform platform,
		APTR state, APTR child, bool shown, uint entries, uint visible,
		uint first, out uint effectiveFirst)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		effectiveFirst = first;
		if (child.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			child).IsNull) return true;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, child,
			MuiCommonControlCore.ShowMe, shown ? 1u : 0u, false)) return false;
		var handled = false;
		if (!MuiCommonControlCore.TrySetHeadlessPropAttribute(ref platform, state,
			child, MuiCommonControlCore.PropEntries, entries, true,
			out handled) || !handled) return false;
		if (!MuiCommonControlCore.TrySetHeadlessPropAttribute(ref platform, state,
			child, MuiCommonControlCore.PropVisible, visible, true,
			out handled) || !handled) return false;
		if (!MuiCommonControlCore.TrySetHeadlessPropAttribute(ref platform, state,
			child, MuiCommonControlCore.PropFirst, first, true, out handled) ||
			!handled) return false;
		if (!MuiCommonControlCore.TryReadPropRangeState(ref platform, state, child,
			out var range)) return false;
		effectiveFirst = range.First;
		return true;
	}

	// Shared UTF-8 metric seam used by Recompute and the focused native
	// qualification root. It returns visual codepoint columns and logical lines
	// while retaining malformed bytes as one-column fallback characters.
	internal static bool TryMeasureUtf8<TPlatform>(ref TPlatform platform,
		APTR text, out uint maxColumns, out uint lines)
		where TPlatform : struct, IMuiGuestMemory
	{
		maxColumns = 0;
		lines = 1;
		if (text.IsNull) return true;
		if (!CStringCodec.TryReadLength(ref platform, text, MaximumStringLength,
			out var length)) return false;
		uint current = 0;
		var index = 0u;
		while (index < length)
		{
			var ch = platform.ReadUInt8(APTR.FromPointer(text.Raw + index), 0);
			if (ch == (byte)'\r')
			{
				index++;
				continue;
			}
			if (ch == (byte)'\n')
			{
				if (current > maxColumns) maxColumns = current;
				current = 0;
				lines++;
				index++;
				continue;
			}
			if (!TryReadUtf8(ref platform, text, index, length, out _,
				out var bytes)) bytes = 1;
			current++;
			index += bytes;
		}
		if (current > maxColumns) maxColumns = current;
		return true;
	}

	// Count logical UTF-8 columns for single-line String.mui editing. Newline
	// bytes are retained as one logical character here; String.mui remains a
	// single-line control, while Stringscroll.mui uses TryMeasureUtf8 for line
	// metrics. Malformed sequences continue to consume one byte.
	internal static bool TryCountUtf8Columns<TPlatform>(ref TPlatform platform,
		APTR text, out uint columns)
		where TPlatform : struct, IMuiGuestMemory
	{
		columns = 0;
		if (text.IsNull) return true;
		if (!CStringCodec.TryReadLength(ref platform, text, MaximumStringLength,
			out var length)) return false;
		var index = 0u;
		while (index < length)
		{
			if (!TryReadUtf8(ref platform, text, index, length, out _,
				out var bytes)) bytes = 1;
			columns++;
			index += bytes;
		}
		return true;
	}

	public static bool SetScroll<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int x, int y) where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPolicyState(ref platform, state, obj, out var policy) ||
			policy.NoInput != 0) return false;
		if (!Recompute(ref platform, state, obj)) return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !TryReadState(ref platform, state, obj,
			out var scrollState) || !TryReadLayoutState(ref platform, state, obj,
			out var layout) || !TryComputeViewportState(ref platform, record,
			scrollState, layout, out var viewport)) return false;
		var maxX = viewport.MaxScrollX;
		var maxY = viewport.MaxScrollY;
		var targetX = x;
		var targetY = y;
		if (targetX < 0) targetX = 0;
		if (targetY < 0) targetY = 0;
		if ((uint)targetX > maxX) targetX = unchecked((int)maxX);
		if ((uint)targetY > maxY) targetY = unchecked((int)maxY);
		scrollState.ScrollX = unchecked((uint)targetX);
		scrollState.ScrollY = unchecked((uint)targetY);
		// SetScroll writes the updated offsets directly (so notification semantics
		// remain one-shot). Synchronize any composed Scrollbar children from this
		// same typed state before publishing it, rather than requiring a later
		// Layout or Draw pass to repair PropFirst.
		if (!SyncCompositionChildren(ref platform, state, obj, scrollState,
			viewport)) return false;
		return WriteState(ref platform, state, obj, scrollState, true);
	}

	public static bool ScrollBy<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int deltaX, int deltaY)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !TryReadState(ref platform, state, obj,
			out var scrollState)) return false;
		var x = unchecked((int)scrollState.ScrollX);
		var y = unchecked((int)scrollState.ScrollY);
		return SetScroll(ref platform, state, obj, x + deltaX, y + deltaY);
	}

	// MUIM_HandleInput uses the preprocessed MUIKEY values from mui.h. Keyboard
	// navigation and pointer track clicks share the same bounded pixel scroll
	// state; the IntuiMessage is decoded through the named pointer record rather
	// than by reaching into its ABI offsets at this call site.
	public static bool HandleInput<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR intuiMessage, int muiKey)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		// MUIKEY_RELEASE is the synthetic cancellation edge for an active
		// pointer gesture.  Handle it before NoInput so a policy change cannot
		// strand the guest-resident drag record.
		if (muiKey == KeyRelease)
			return CancelPointerDrag(ref platform, state, obj);
		if (!TryReadPolicyState(ref platform, state, obj, out var policy) ||
			policy.NoInput != 0) return false;
		if (!GetScrollState(ref platform, state, obj, out var oldX, out var oldY,
			out var maxX, out var maxY)) return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !TryReadState(ref platform, state, obj,
			out var scrollState) || !TryReadLayoutState(ref platform, state, obj,
			out var layout) || !TryComputeViewportState(ref platform, record,
				scrollState, layout, out var viewport)) return false;
		if (muiKey == KeyNone)
		{
			if (!MuiIntuiMessageCodec.TryReadPointer(ref platform, intuiMessage,
				out var pointer)) return false;
			return HandlePointer(ref platform, state, obj, pointer, scrollState,
				viewport, layout, oldX, oldY);
		}
		var viewportHeight = viewport.ViewportHeight;
		var targetX = oldX;
		var targetY = oldY;
		switch (muiKey)
		{
			case KeyUp: targetY -= (int)CharacterHeight; break;
			case KeyDown: targetY += (int)CharacterHeight; break;
			case KeyPageUp: targetY -= viewportHeight; break;
			case KeyPageDown: targetY += viewportHeight; break;
			case KeyLeft: targetX -= (int)CharacterWidth; break;
			case KeyRight: targetX += (int)CharacterWidth; break;
			case KeyTop: targetX = 0; targetY = 0; break;
			case KeyBottom: targetX = maxX; targetY = maxY; break;
			default: return false;
		}
		if (!SetScroll(ref platform, state, obj, targetX, targetY)) return false;
		return GetScrollState(ref platform, state, obj, out var newX, out var newY,
			out _, out _) && (newX != oldX || newY != oldY);
	}

	private static bool CancelPointerDrag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPointerState(ref platform, state, obj, out var value) ||
			(value.Flags & MuiStringscrollPointerState.ActiveFlag) == 0)
			return false;
		ReleasePointerIfCaptured(ref platform, obj, value);
		return MuiStoreCore.DataspaceRemove(ref platform, state, obj,
			PointerStateKey);
	}

	// Window deactivation is a cancellation edge for a thumb gesture just as
	// MUIKEY_RELEASE is. Keep the public window hook on the typed state path so
	// it can clear only an active guest record and never infer a private offset.
	internal static bool CancelPointerDragForWindow<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		CancelPointerDrag(ref platform, state, obj);

	// Direct headless disposal is a terminal path as well as the collection
	// lifecycle wrapper. Remove any typed pointer state before StoreCore.ClearAll
	// reclaims the object's Dataspace, releasing optional native capture through
	// the same named record used by normal cancellation.
	internal static bool Cleanup<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var compositionValid = TryReadCompositionAdmission(ref platform, state, obj,
			out var composition, out var compositionPresent);
		if (compositionValid && compositionPresent)
		{
			DisposeOwnedScrollbar(ref platform, state, composition.Horizontal,
				(composition.OwnedMask & MuiStringscrollCompositionState.HorizontalOwned) != 0);
			DisposeOwnedScrollbar(ref platform, state, composition.Vertical,
				(composition.OwnedMask & MuiStringscrollCompositionState.VerticalOwned) != 0);
			MuiStoreCore.DataspaceRemove(ref platform, state, obj,
				CompositionStateKey);
		}
		if (!TryReadPointerState(ref platform, state, obj, out var value))
			return compositionValid;
		ReleasePointerIfCaptured(ref platform, obj, value);
		return compositionValid && MuiStoreCore.DataspaceRemove(ref platform, state,
			obj, PointerStateKey);
	}

	private static MuiPointerCaptureKind CaptureKind(uint axis) =>
		axis == MuiStringscrollPointerState.VerticalAxis ?
			MuiPointerCaptureKind.VerticalScroller :
			MuiPointerCaptureKind.HorizontalScroller;

	private static bool CapturePointer<TPlatform>(ref TPlatform platform,
		APTR obj, uint axis, int startX, int startY)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sample = default(MuiPointerCaptureSample);
		sample.Object = obj;
		sample.Kind = CaptureKind(axis);
		sample.StartX = startX;
		sample.StartY = startY;
		return platform.CaptureMuiPointer(ref sample);
	}

	private static void ReleasePointer<TPlatform>(ref TPlatform platform,
		APTR obj, MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var sample = default(MuiPointerCaptureSample);
		sample.Object = obj;
		sample.Kind = CaptureKind(value.Axis);
		sample.StartX = value.StartX;
		sample.StartY = value.StartY;
		_ = platform.ReleaseMuiPointer(ref sample);
	}

	private static void ReleasePointerIfCaptured<TPlatform>(ref TPlatform platform,
		APTR obj, MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if ((value.Flags & MuiStringscrollPointerState.CapturedFlag) != 0)
			ReleasePointer(ref platform, obj, value);
	}

	private static bool ReleasePointerDrag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPointerState(ref platform, state, obj, out var value) ||
			(value.Flags & MuiStringscrollPointerState.ActiveFlag) == 0)
			return false;
		ReleasePointerIfCaptured(ref platform, obj, value);
		return MuiStoreCore.DataspaceRemove(ref platform, state, obj,
			PointerStateKey);
	}

	private static bool TryBuildInputBarGeometry<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiStringscrollState scrollState, MuiStringscrollViewportState viewport,
		MuiStringscrollLayoutState layout, uint axis,
		out MuiStringscrollInputBarGeometry result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var horizontal = axis == MuiStringscrollPointerState.HorizontalAxis;
		if (horizontal)
		{
			if (viewport.HorizontalVisible == 0 || layout.Height < ScrollerExtent)
				return false;
		}
		else if (viewport.VerticalVisible == 0 || layout.Width < ScrollerExtent)
			return false;
		if (!TryReadCompositionAdmission(ref platform, state, obj,
			out var composition, out var compositionPresent)) return false;
		var child = horizontal ? composition.Horizontal : composition.Vertical;
		var hasChild = compositionPresent && child.IsNotNull &&
			MuiHeadlessObjectCore.FindObject(ref platform, state, child).IsNotNull;
		if (!hasChild)
		{
			var legacyHorizontal = default(MuiStringscrollBarGeometry);
			var legacyVertical = default(MuiStringscrollBarGeometry);
			if (horizontal && !TryBuildHorizontalBar(scrollState, viewport,
				layout.Left, layout.Top, layout.Height, out legacyHorizontal))
				return false;
			if (!horizontal && !TryBuildVerticalBar(scrollState, viewport,
				layout.Left, layout.Top, layout.Width, out legacyVertical))
				return false;
			if (horizontal)
			{
				result.TrackStart = legacyHorizontal.TrackLeft;
				result.TrackEnd = legacyHorizontal.TrackRight;
				result.ThumbStart = legacyHorizontal.ThumbLeft;
				result.ThumbEnd = legacyHorizontal.ThumbRight;
				result.CrossStart = legacyHorizontal.TrackTop;
				result.CrossEnd = legacyHorizontal.TrackBottom;
				result.Horizontal = 1;
			}
			else
			{
				result.TrackStart = legacyVertical.TrackTop;
				result.TrackEnd = legacyVertical.TrackBottom;
				result.ThumbStart = legacyVertical.ThumbTop;
				result.ThumbEnd = legacyVertical.ThumbBottom;
				result.CrossStart = legacyVertical.TrackLeft;
				result.CrossEnd = legacyVertical.TrackRight;
			}
			return true;
		}

		if (!MuiCommonControlCore.TryReadScrollbarLayoutState(ref platform, state,
			child, out var scrollbarLayout) ||
			!MuiCommonControlCore.TryReadPropRangeState(ref platform, state, child,
			out var range)) return false;
		var childHorizontal = scrollbarLayout.Horizontal != 0;
		if (childHorizontal != horizontal) return false;
		var origin = horizontal ? layout.Left : layout.Top;
		var crossStart = horizontal ? layout.Top + layout.Height - ScrollerExtent :
			layout.Left + layout.Width - ScrollerExtent;
		var total = horizontal ? viewport.ViewportWidth : viewport.ViewportHeight;
		if (total <= 0) return false;
		var arrows = scrollbarLayout.Type != ScrollbarTypeNone;
		var arrowExtent = arrows ? 16 : 0;
		var propLength = total - (scrollbarLayout.Type == ScrollbarTypeBottom ||
			scrollbarLayout.Type == ScrollbarTypeTop ? arrowExtent : arrowExtent * 2);
		if (propLength < 0) propLength = 0;
		var propStart = origin;
		var firstStart = origin;
		var secondStart = origin;
		if (arrows)
		{
			if (scrollbarLayout.Type == ScrollbarTypeBottom)
			{
				firstStart = origin + propLength;
				secondStart = firstStart + arrowExtent;
			}
			else if (scrollbarLayout.Type == ScrollbarTypeTop)
			{
				firstStart = origin;
				secondStart = origin + arrowExtent;
				propStart = origin + arrowExtent * 2;
			}
			else
			{
				firstStart = origin;
				propStart = origin + arrowExtent;
				secondStart = propStart + propLength;
			}
		}
		var knob = range.Entries == 0 ? propLength :
			unchecked((int)(range.Visible * (uint)propLength / range.Entries));
		var offset = range.Entries == 0 ? 0 :
			unchecked((int)(range.First * (uint)propLength / range.Entries));
		if (knob <= 0 && propLength > 0) knob = 1;
		if (offset < 0) offset = 0;
		if (offset + knob > propLength) offset = propLength - knob;
		if (offset < 0) offset = 0;
		result.TrackStart = propStart;
		result.TrackEnd = propStart + propLength - 1;
		if (horizontal)
		{
			result.ThumbStart = propStart + offset;
			result.ThumbEnd = result.ThumbStart + knob - 1;
			result.CrossStart = crossStart;
			result.CrossEnd = crossStart + ScrollerExtent - 1;
		}
		else
		{
			result.ThumbStart = propStart + propLength - offset - knob;
			result.ThumbEnd = result.ThumbStart + knob - 1;
			result.CrossStart = crossStart;
			result.CrossEnd = crossStart + ScrollerExtent - 1;
		}
		result.Horizontal = horizontal ? 1u : 0u;
		result.HasArrows = arrows ? 1u : 0u;
		if (arrows)
		{
			result.FirstArrowStart = firstStart;
			result.FirstArrowEnd = firstStart + arrowExtent - 1;
			result.SecondArrowStart = secondStart;
			result.SecondArrowEnd = secondStart + arrowExtent - 1;
			result.FirstDelta = scrollbarLayout.Type == ScrollbarTypeBottom ? 1 : -1;
			result.SecondDelta = scrollbarLayout.Type == ScrollbarTypeTop ? -1 : 1;
		}
		return true;
	}

	private static bool ContainsInputBar(MuiStringscrollInputBarGeometry geometry,
		int x, int y, int start, int end)
	{
		var axis = geometry.Horizontal != 0 ? x : y;
		var cross = geometry.Horizontal != 0 ? y : x;
		return axis >= start && axis <= end && cross >= geometry.CrossStart &&
			cross <= geometry.CrossEnd;
	}

	private static bool ContainsInputBarAny(MuiStringscrollInputBarGeometry geometry,
		int x, int y)
	{
		var start = geometry.TrackStart;
		var end = geometry.TrackEnd;
		if (geometry.HasArrows != 0)
		{
			if (geometry.FirstArrowStart < start) start = geometry.FirstArrowStart;
			if (geometry.SecondArrowStart < start) start = geometry.SecondArrowStart;
			if (geometry.FirstArrowEnd > end) end = geometry.FirstArrowEnd;
			if (geometry.SecondArrowEnd > end) end = geometry.SecondArrowEnd;
		}
		return ContainsInputBar(geometry, x, y, start, end);
	}

	// SELECTDOWN on a thumb arms a guest-resident drag record; MOUSEMOVE updates
	// the corresponding bounded pixel offset and SELECTUP releases it. A plain
	// SELECTUP on a track retains the click-to-centre behavior from MG208. The
	// bottom-right overlap is assigned to the vertical track first, as it owns
	// the right edge.
	private static bool HandlePointer<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiIntuiPointerMessage pointer,
		MuiStringscrollState scrollState, MuiStringscrollViewportState viewport,
		MuiStringscrollLayoutState layout, int oldX, int oldY)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (pointer.Class == IdcmpMouseMove)
			return UpdatePointerDrag(ref platform, state, obj, pointer,
				scrollState, viewport, layout, oldX, oldY);
		if (pointer.Class != IdcmpMouseButtons) return false;
		if (pointer.Code == SelectDown)
			return BeginPointerDrag(ref platform, state, obj, pointer, scrollState,
				viewport, layout);
		if (pointer.Code != SelectUp) return false;
		if (TryReadPointerState(ref platform, state, obj, out var active) &&
			(active.Flags & MuiStringscrollPointerState.ActiveFlag) != 0)
		{
			UpdatePointerDrag(ref platform, state, obj, pointer, scrollState,
				viewport, layout, oldX, oldY);
			return ReleasePointerDrag(ref platform, state, obj);
		}
		return HandleTrackClick(ref platform, state, obj, pointer, scrollState,
			viewport, layout, oldX, oldY);
	}

	private static bool HandleTrackClick<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiIntuiPointerMessage pointer,
		MuiStringscrollState scrollState, MuiStringscrollViewportState viewport,
		MuiStringscrollLayoutState layout, int oldX, int oldY)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryBuildInputBarGeometry(ref platform, state, obj, scrollState,
			viewport, layout, MuiStringscrollPointerState.VerticalAxis,
			out var vertical) && ContainsInputBarAny(vertical, pointer.MouseX,
			pointer.MouseY))
		{
			if (vertical.HasArrows != 0)
			{
				var axis = pointer.MouseY;
				if (axis >= vertical.FirstArrowStart && axis <= vertical.FirstArrowEnd)
					return SetScroll(ref platform, state, obj, oldX,
						oldY + vertical.FirstDelta * StepForScrollbar(ref platform,
							state, obj, false));
				if (axis >= vertical.SecondArrowStart && axis <= vertical.SecondArrowEnd)
					return SetScroll(ref platform, state, obj, oldX,
						oldY + vertical.SecondDelta * StepForScrollbar(ref platform,
							state, obj, false));
			}
			if (!ContainsInputBar(vertical, pointer.MouseX, pointer.MouseY,
				vertical.TrackStart, vertical.TrackEnd)) return false;
			var targetY = TrackPosition(pointer.MouseY, vertical.TrackStart,
				vertical.TrackEnd, vertical.ThumbStart, vertical.ThumbEnd,
				viewport.MaxScrollY);
			if (targetY == oldY) return false;
			return SetScroll(ref platform, state, obj, oldX, targetY);
		}
		if (TryBuildInputBarGeometry(ref platform, state, obj, scrollState,
			viewport, layout, MuiStringscrollPointerState.HorizontalAxis,
			out var horizontal) && ContainsInputBarAny(horizontal, pointer.MouseX,
			pointer.MouseY))
		{
			if (horizontal.HasArrows != 0)
			{
				var axis = pointer.MouseX;
				if (axis >= horizontal.FirstArrowStart && axis <= horizontal.FirstArrowEnd)
					return SetScroll(ref platform, state, obj,
						oldX + horizontal.FirstDelta * StepForScrollbar(ref platform,
							state, obj, true), oldY);
				if (axis >= horizontal.SecondArrowStart && axis <= horizontal.SecondArrowEnd)
					return SetScroll(ref platform, state, obj,
						oldX + horizontal.SecondDelta * StepForScrollbar(ref platform,
							state, obj, true), oldY);
			}
			if (!ContainsInputBar(horizontal, pointer.MouseX, pointer.MouseY,
				horizontal.TrackStart, horizontal.TrackEnd)) return false;
			var targetX = TrackPosition(pointer.MouseX, horizontal.TrackStart,
				horizontal.TrackEnd, horizontal.ThumbStart, horizontal.ThumbEnd,
				viewport.MaxScrollX);
			if (targetX == oldX) return false;
			return SetScroll(ref platform, state, obj, targetX, oldY);
		}
		return false;
	}

	private static int StepForScrollbar<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadCompositionAdmission(ref platform, state, obj,
			out var composition, out var present) || !present) return 1;
		var child = horizontal ? composition.Horizontal : composition.Vertical;
		if (child.IsNull || !MuiCommonControlCore.TryReadPropPolicyState(
			ref platform, state, child, out var policy) || policy.DeltaFactor == 0)
			return 1;
		return policy.DeltaFactor > int.MaxValue ? int.MaxValue :
			unchecked((int)policy.DeltaFactor);
	}

	private static bool BeginPointerDrag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiIntuiPointerMessage pointer,
		MuiStringscrollState scrollState, MuiStringscrollViewportState viewport,
		MuiStringscrollLayoutState layout)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		uint axis;
		int coordinate;
		MuiStringscrollInputBarGeometry geometry;
		if (TryBuildInputBarGeometry(ref platform, state, obj, scrollState,
			viewport, layout, MuiStringscrollPointerState.VerticalAxis,
			out geometry) && ContainsInputBar(geometry, pointer.MouseX,
			pointer.MouseY, geometry.ThumbStart, geometry.ThumbEnd))
		{
			axis = MuiStringscrollPointerState.VerticalAxis;
			coordinate = pointer.MouseY;
		}
		else if (TryBuildInputBarGeometry(ref platform, state, obj, scrollState,
			viewport, layout, MuiStringscrollPointerState.HorizontalAxis,
			out geometry) && ContainsInputBar(geometry, pointer.MouseX,
			pointer.MouseY, geometry.ThumbStart, geometry.ThumbEnd))
		{
			axis = MuiStringscrollPointerState.HorizontalAxis;
			coordinate = pointer.MouseX;
		}
		else return false;

		var block = EnsurePointerState(ref platform, state, obj);
		if (block.IsNull) return false;
		var value = default(MuiStringscrollPointerState);
		value.Magic = MuiStringscrollPointerState.Cookie;
		value.Axis = axis;
		value.GrabOffset = coordinate - geometry.ThumbStart;
		value.StartScroll = axis == MuiStringscrollPointerState.VerticalAxis ?
			unchecked((int)scrollState.ScrollY) : unchecked((int)scrollState.ScrollX);
		value.StartX = pointer.MouseX;
		value.StartY = pointer.MouseY;
		value.LastPointer = coordinate;
		value.Flags = MuiStringscrollPointerState.ActiveFlag;
		if (CapturePointer(ref platform, obj, axis, pointer.MouseX,
			pointer.MouseY)) value.Flags |= MuiStringscrollPointerState.CapturedFlag;
		if (MuiStringscrollPointerStateCodec.Write(ref platform, block, value))
			return true;
		ReleasePointerIfCaptured(ref platform, obj, value);
		MuiStoreCore.DataspaceRemove(ref platform, state, obj, PointerStateKey);
		return false;
	}

	private static bool UpdatePointerDrag<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiIntuiPointerMessage pointer,
		MuiStringscrollState scrollState, MuiStringscrollViewportState viewport,
		MuiStringscrollLayoutState layout, int oldX, int oldY)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPointerState(ref platform, state, obj, out var value) ||
			(value.Flags & MuiStringscrollPointerState.ActiveFlag) == 0)
			return false;
		var coordinate = value.Axis == MuiStringscrollPointerState.VerticalAxis ?
			pointer.MouseY : pointer.MouseX;
		if (!TryBuildInputBarGeometry(ref platform, state, obj, scrollState,
			viewport, layout, value.Axis, out var geometry)) return false;
		var trackStart = geometry.TrackStart;
		var trackEnd = geometry.TrackEnd;
		var thumbLength = geometry.ThumbEnd - geometry.ThumbStart + 1;
		uint maximum;
		if (value.Axis == MuiStringscrollPointerState.VerticalAxis)
			maximum = viewport.MaxScrollY;
		else if (value.Axis == MuiStringscrollPointerState.HorizontalAxis)
			maximum = viewport.MaxScrollX;
		else return false;
		var target = TrackPositionFromGrab(coordinate, trackStart, trackEnd,
			thumbLength, value.GrabOffset, maximum);
		value.LastPointer = coordinate;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PointerStateKey);
		if (!MuiStringscrollPointerStateCodec.Write(ref platform, block, value))
			return false;
		if (value.Axis == MuiStringscrollPointerState.VerticalAxis)
		{
			if (target == oldY) return true;
			return SetScroll(ref platform, state, obj, oldX, target);
		}
		if (target == oldX) return true;
		return SetScroll(ref platform, state, obj, target, oldY);
	}

	private static bool TryReadPointerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollPointerState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PointerStateKey);
		return MuiStringscrollPointerStateCodec.TryRead(ref platform, block,
			out value);
	}

	private static APTR EnsurePointerState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PointerStateKey);
		if (MuiStringscrollPointerStateCodec.TryRead(ref platform, block,
			out _)) return block;
		if (!MuiStoreCore.DataspaceResize(ref platform, state, obj,
			PointerStateKey, unchecked((int)MuiStringscrollPointerState.Size)))
			return APTR.Null;
		block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PointerStateKey);
		return block;
	}

	private static bool Contains(int left, int top, int right, int bottom,
		int x, int y) => x >= left && x <= right && y >= top && y <= bottom;

	private static int TrackPosition(int pointer, int trackStart, int trackEnd,
		int thumbStart, int thumbEnd, uint maximum)
	{
		var trackLength = trackEnd - trackStart + 1;
		var thumbLength = thumbEnd - thumbStart + 1;
		var travel = trackLength - thumbLength;
		if (maximum == 0 || travel <= 0) return 0;
		var desired = pointer - trackStart - thumbLength / 2;
		if (desired < 0) desired = 0;
		if (desired > travel) desired = travel;
		return unchecked((int)((uint)desired * maximum / unchecked((uint)travel)));
	}

	private static int TrackPositionFromGrab(int pointer, int trackStart,
		int trackEnd, int thumbLength, int grabOffset, uint maximum)
	{
		var travel = trackEnd - trackStart + 1 - thumbLength;
		if (maximum == 0 || travel <= 0) return 0;
		var desired = pointer - trackStart - grabOffset;
		if (desired < 0) desired = 0;
		if (desired > travel) desired = travel;
		return unchecked((int)((uint)desired * maximum / unchecked((uint)travel)));
	}

	public static bool GetScrollState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out int x, out int y, out int maxX, out int maxY)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		x = 0; y = 0; maxX = 0; maxY = 0;
		if (!TryReadState(ref platform, state, obj, out var scrollState))
			return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !TryReadLayoutState(ref platform, state, obj,
			out var layout) || !TryComputeViewportState(ref platform, record,
			scrollState, layout, out var viewport)) return false;
		x = unchecked((int)scrollState.ScrollX);
		y = unchecked((int)scrollState.ScrollY);
		maxX = unchecked((int)viewport.MaxScrollX);
		maxY = unchecked((int)viewport.MaxScrollY);
		return true;
	}

	// Struct-first inspection seam used by host/native qualification. It reads
	// the object-owned String buffer and all derived metric/scroll values as one
	// logical state, without exposing the private store-key layout to callers.
	public static bool TryReadState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, out MuiStringscrollState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		// Preserve the legacy raw fallback only when the named record is absent.
		// Once a record exists, a wrong length or invalid cookie is malformed
		// typed state and must fail closed before scrolling or recomputation can
		// publish derived raw values.
		var stateBlock = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateRecordKey);
		var stateLength = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateRecordKey);
		if (stateBlock.IsNotNull || stateLength != 0)
		{
			if (stateLength != unchecked((int)MuiStringscrollStateRecord.Size) ||
				!MuiStringscrollStateRecordCodec.TryRead(ref platform, stateBlock,
					out var stored)) return false;
			result.String = stored.String;
			result.ContentWidth = stored.ContentWidth;
			result.ContentHeight = stored.ContentHeight;
			result.ScrollX = stored.ScrollX;
			result.ScrollY = stored.ScrollY;
			return true;
		}
		result.String = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringKey);
		result.ContentWidth = ReadRaw(ref platform, record, ContentWidthKey, 0);
		result.ContentHeight = ReadRaw(ref platform, record, ContentHeightKey, 0);
		result.ScrollX = ReadRaw(ref platform, record, ScrollXKey, 0);
		result.ScrollY = ReadRaw(ref platform, record, ScrollYKey, 0);
		return true;
	}

	// Public struct-first inspection seam for the signed Area geometry consumed
	// by all Stringscroll layout, scrolling, and rendering paths.
	public static bool TryReadLayoutState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollLayoutState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var layoutBlock = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		var layoutLength = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LayoutStateKey);
		if (layoutBlock.IsNotNull || layoutLength != 0)
		{
			if (layoutLength != unchecked((int)MuiStringscrollLayoutStateRecord.Size) ||
				!MuiStringscrollLayoutStateRecordCodec.TryRead(ref platform, layoutBlock,
					out var stored)) return false;
			result.Left = stored.Left;
			result.Top = stored.Top;
			result.Width = stored.Width;
			result.Height = stored.Height;
			return true;
		}
		return TryReadLayoutStateRaw(ref platform, state, obj, out result);
	}

	private static bool TryReadLayoutStateRaw<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollLayoutState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
			out var geometry)) return false;
		result.Left = geometry.Left;
		result.Top = geometry.Top;
		result.Width = geometry.Width;
		result.Height = geometry.Height;
		return true;
	}

	// Public struct-first inspection seam for the guest render context. The
	// MUI_RenderInfo ABI is decoded by its shared named codec, so the
	// Stringscroll implementation does not carry a private rastport offset.
	public static bool TryReadRenderState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollRenderState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		var renderBlock = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RenderStateKey);
		var renderLength = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			RenderStateKey);
		if (renderBlock.IsNotNull || renderLength != 0)
		{
			if (renderLength != unchecked((int)MuiStringscrollRenderStateRecord.Size) ||
				!MuiStringscrollRenderStateRecordCodec.TryRead(ref platform, renderBlock,
					out var stored)) return false;
			if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, stored.RenderInfo,
				out var info) || info.RastPort.IsNull || stored.RastPort.Raw !=
				info.RastPort.Raw) return false;
			result.RenderInfo = stored.RenderInfo;
			result.RastPort = stored.RastPort;
			result.Font = stored.Font;
			return true;
		}
		return TryReadRenderStateRaw(ref platform, record, out result);
	}

	private static bool TryReadRenderStateRaw<TPlatform>(ref TPlatform platform,
		APTR record, out MuiStringscrollRenderState result)
		where TPlatform : struct, IMuiGuestMemory
	{
		result = default;
		if (record.IsNull) return false;
		result.RenderInfo = APTR.FromPointer(ReadRaw(ref platform, record,
			RenderInfo, 0));
		if (!MuiDrawingRenderInfoCodec.TryRead(ref platform, result.RenderInfo,
			out var info) || info.RastPort.IsNull) return false;
		result.RastPort = info.RastPort;
		result.Font = APTR.FromPointer(ReadRaw(ref platform, record, Font, 0));
		return true;
	}

	// Public seam for the derived viewport and bounded scroll limits. It reads
	// the already-owned text metrics, layout, and policy state without exposing
	// the private metric store keys or duplicating the bar-reservation rules.
	public static bool TryReadViewportState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollViewportState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (!TryReadState(ref platform, state, obj, out var scrollState))
			return false;
		var viewportBlock = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ViewportStateKey);
		var viewportLength = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ViewportStateKey);
		if (viewportBlock.IsNotNull || viewportLength != 0)
		{
			if (viewportLength != unchecked((int)
				MuiStringscrollViewportStateRecord.Size) ||
				!MuiStringscrollViewportStateRecordCodec.TryRead(ref platform,
					viewportBlock, out var stored)) return false;
			result.ViewportWidth = stored.ViewportWidth;
			result.ViewportHeight = stored.ViewportHeight;
			result.HorizontalVisible = stored.HorizontalVisible;
			result.VerticalVisible = stored.VerticalVisible;
			result.MaxScrollX = stored.MaxScrollX;
			result.MaxScrollY = stored.MaxScrollY;
			return true;
		}
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		return record.IsNotNull && TryReadLayoutState(ref platform, state, obj,
			out var layout) && TryComputeViewportState(ref platform, record,
			scrollState, layout, out result);
	}

	private static bool TryComputeViewportState<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollState scrollState,
		MuiStringscrollLayoutState layout, out MuiStringscrollViewportState result)
		where TPlatform : struct, IMuiGuestMemory
	{
		result = default;
		if (record.IsNull) return false;
		result.HorizontalVisible = HorizontalVisible(ref platform, record,
			scrollState, layout) ? 1u : 0u;
		result.VerticalVisible = VerticalVisible(ref platform, record,
			scrollState, layout) ? 1u : 0u;
		var viewportWidth = layout.Width;
		var viewportHeight = layout.Height;
		if (result.VerticalVisible != 0) viewportWidth -= ScrollerExtent;
		if (result.HorizontalVisible != 0) viewportHeight -= ScrollerExtent;
		if (viewportWidth < 0) viewportWidth = 0;
		if (viewportHeight < 0) viewportHeight = 0;
		result.ViewportWidth = viewportWidth;
		result.ViewportHeight = viewportHeight;
		result.MaxScrollX = scrollState.ContentWidth > (uint)viewportWidth ?
			scrollState.ContentWidth - (uint)viewportWidth : 0u;
		result.MaxScrollY = scrollState.ContentHeight > (uint)viewportHeight ?
			scrollState.ContentHeight - (uint)viewportHeight : 0u;
		return true;
	}

	private static bool WriteState<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, MuiStringscrollState value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		if (!SetRaw(ref platform, state, record, ContentWidthKey,
			value.ContentWidth, false) ||
			!SetRaw(ref platform, state, record, ContentHeightKey,
				value.ContentHeight, false) ||
			!SetRaw(ref platform, state, record, ScrollXKey, value.ScrollX,
				notify) ||
			!SetRaw(ref platform, state, record, ScrollYKey, value.ScrollY,
				notify)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateRecordKey);
		if (!MuiStringscrollStateRecordCodec.TryRead(ref platform, block,
			out var stored)) return false;
		stored.String = value.String;
		stored.ContentWidth = value.ContentWidth;
		stored.ContentHeight = value.ContentHeight;
		stored.ScrollX = value.ScrollX;
		stored.ScrollY = value.ScrollY;
		return MuiStringscrollStateRecordCodec.Write(ref platform, block, stored);
	}

	private static bool TryReadStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateRecordKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			StateRecordKey) != unchecked((int)MuiStringscrollStateRecord.Size))
			return false;
		return MuiStringscrollStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadStateRecord(ref platform, state, obj, out _)) return true;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiStringscrollStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiStringscrollStateRecord.Size);
		var value = default(MuiStringscrollStateRecord);
		value.Magic = MuiStringscrollStateRecord.Cookie;
		value.String = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringKey);
		value.ContentWidth = Read(ref platform, state, obj, ContentWidthKey, 0);
		value.ContentHeight = Read(ref platform, state, obj, ContentHeightKey, 0);
		value.ScrollX = Read(ref platform, state, obj, ScrollXKey, 0);
		value.ScrollY = Read(ref platform, state, obj, ScrollYKey, 0);
		var written = MuiStringscrollStateRecordCodec.Write(ref platform, scratch,
			value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			StateRecordKey, scratch,
			unchecked((int)MuiStringscrollStateRecord.Size));
		platform.Clear(scratch, MuiStringscrollStateRecord.Size);
		platform.Free(scratch, MuiStringscrollStateRecord.Size);
		return added;
	}

	private static bool SyncStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadStateRecord(ref platform, state, obj, out var value))
			return false;
		value.String = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringKey);
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StateRecordKey);
		return MuiStringscrollStateRecordCodec.Write(ref platform, block, value);
	}

	private static bool TryReadRenderRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollRenderStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RenderStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			RenderStateKey) != unchecked((int)MuiStringscrollRenderStateRecord.Size))
			return false;
		return MuiStringscrollRenderStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureRenderRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadRenderRecord(ref platform, state, obj, out _)) return true;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiStringscrollRenderStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiStringscrollRenderStateRecord.Size);
		var value = default(MuiStringscrollRenderStateRecord);
		value.Magic = MuiStringscrollRenderStateRecord.Cookie;
		value.RenderInfo = APTR.FromPointer(ReadRaw(ref platform, record,
			RenderInfo, 0));
		value.Font = APTR.FromPointer(ReadRaw(ref platform, record, Font, 0));
		if (MuiDrawingRenderInfoCodec.TryRead(ref platform, value.RenderInfo,
			out var info)) value.RastPort = info.RastPort;
		var written = MuiStringscrollRenderStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			RenderStateKey, scratch,
			unchecked((int)MuiStringscrollRenderStateRecord.Size));
		platform.Clear(scratch, MuiStringscrollRenderStateRecord.Size);
		platform.Free(scratch, MuiStringscrollRenderStateRecord.Size);
		return added;
	}

	private static bool SyncRenderRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadRenderRecord(ref platform, state, obj, out var value))
			return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		value.RenderInfo = APTR.FromPointer(ReadRaw(ref platform, record,
			RenderInfo, 0));
		value.Font = APTR.FromPointer(ReadRaw(ref platform, record, Font, 0));
		value.RastPort = APTR.Null;
		if (MuiDrawingRenderInfoCodec.TryRead(ref platform, value.RenderInfo,
			out var info)) value.RastPort = info.RastPort;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			RenderStateKey);
		return MuiStringscrollRenderStateRecordCodec.Write(ref platform, block,
			value);
	}

	private static bool TryReadViewportStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiStringscrollViewportStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ViewportStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ViewportStateKey) != unchecked((int)MuiStringscrollViewportStateRecord.Size))
			return false;
		return MuiStringscrollViewportStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool WriteViewportStateRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		MuiStringscrollViewportState value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var stored = default(MuiStringscrollViewportStateRecord);
		stored.Magic = MuiStringscrollViewportStateRecord.Cookie;
		stored.ViewportWidth = value.ViewportWidth;
		stored.ViewportHeight = value.ViewportHeight;
		stored.HorizontalVisible = value.HorizontalVisible;
		stored.VerticalVisible = value.VerticalVisible;
		stored.MaxScrollX = value.MaxScrollX;
		stored.MaxScrollY = value.MaxScrollY;
		if (TryReadViewportStateRecord(ref platform, state, obj, out _))
		{
			var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
				ViewportStateKey);
			return MuiStringscrollViewportStateRecordCodec.Write(ref platform,
				block, stored);
		}
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiStringscrollViewportStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiStringscrollViewportStateRecord.Size);
		var written = MuiStringscrollViewportStateRecordCodec.Write(ref platform,
			scratch, stored);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			ViewportStateKey, scratch,
			unchecked((int)MuiStringscrollViewportStateRecord.Size));
		platform.Clear(scratch, MuiStringscrollViewportStateRecord.Size);
		platform.Free(scratch, MuiStringscrollViewportStateRecord.Size);
		return added;
	}

	private static bool TryReadLayoutRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			LayoutStateKey) != unchecked((int)MuiStringscrollLayoutStateRecord.Size))
			return false;
		return MuiStringscrollLayoutStateRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureLayoutRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadLayoutRecord(ref platform, state, obj, out _)) return true;
		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
			out var geometry)) return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiStringscrollLayoutStateRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiStringscrollLayoutStateRecord.Size);
		var value = default(MuiStringscrollLayoutStateRecord);
		value.Magic = MuiStringscrollLayoutStateRecord.Cookie;
		value.Left = geometry.Left;
		value.Top = geometry.Top;
		value.Width = geometry.Width;
		value.Height = geometry.Height;
		var written = MuiStringscrollLayoutStateRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			LayoutStateKey, scratch,
			unchecked((int)MuiStringscrollLayoutStateRecord.Size));
		platform.Clear(scratch, MuiStringscrollLayoutStateRecord.Size);
		platform.Free(scratch, MuiStringscrollLayoutStateRecord.Size);
		return added;
	}

	private static bool SyncLayoutRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadLayoutRecord(ref platform, state, obj, out var value))
			return false;
		if (!MuiAreaLayoutCore.TryReadGeometryState(ref platform, state, obj,
			out var geometry)) return false;
		value.Left = geometry.Left;
		value.Top = geometry.Top;
		value.Width = geometry.Width;
		value.Height = geometry.Height;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			LayoutStateKey);
		return MuiStringscrollLayoutStateRecordCodec.Write(ref platform, block,
			value);
	}

	internal static bool TryGetLayoutRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollLayoutStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadLayoutRecord(ref platform, state, obj, out value);

	internal static bool TryGetRenderRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollRenderStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadRenderRecord(ref platform, state, obj, out value);

	internal static bool TryGetViewportRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollViewportStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadViewportStateRecord(ref platform, state, obj, out value);

	internal static bool TryGetStateRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollStateRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadStateRecord(ref platform, state, obj, out value);

	public static bool TryReadPolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollPolicyState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull) return false;
		// Keep the raw compatibility projection only for a truly absent named
		// policy record. A present malformed record must not silently redefine
		// policy for scrolling or input consumers.
		var policyBlock = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		var policyLength = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey);
		if (policyBlock.IsNotNull || policyLength != 0)
		{
			if (policyLength != unchecked((int)MuiStringscrollPolicyRecord.Size) ||
				!MuiStringscrollPolicyRecordCodec.TryRead(ref platform, policyBlock,
					out var stored) ||
				!MuiStringscrollPolicyValidation.IsValidRecord(stored)) return false;
			result.HorizBar = stored.HorizBar;
			result.NoInput = stored.NoInput;
			result.SetMin = stored.SetMin;
			result.SetVMin = stored.SetVMin;
			result.UseWinBorder = stored.UseWinBorder;
			result.VertBar = stored.VertBar;
			result.VertScrollerOnly = stored.VertScrollerOnly;
			return true;
		}
		return TryReadPolicyState(ref platform, record, out result);
	}

	private static bool TryReadPolicyRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollPolicyRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			PolicyStateKey) != unchecked((int)MuiStringscrollPolicyRecord.Size))
			return false;
		return MuiStringscrollPolicyRecordCodec.TryRead(ref platform, block,
			out value) && MuiStringscrollPolicyValidation.IsValidRecord(value);
	}

	private static bool EnsurePolicyRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR objectRecord)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadPolicyRecord(ref platform, state, obj, out _)) return true;
		if (!TryReadPolicyState(ref platform, objectRecord, out var policy))
			return false;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiStringscrollPolicyRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiStringscrollPolicyRecord.Size);
		var value = default(MuiStringscrollPolicyRecord);
		value.Magic = MuiStringscrollPolicyRecord.Cookie;
		value.HorizBar = policy.HorizBar;
		value.NoInput = policy.NoInput;
		value.SetMin = policy.SetMin;
		value.SetVMin = policy.SetVMin;
		value.UseWinBorder = policy.UseWinBorder;
		value.VertBar = policy.VertBar;
		value.VertScrollerOnly = policy.VertScrollerOnly;
		var written = MuiStringscrollPolicyRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			PolicyStateKey, scratch,
			unchecked((int)MuiStringscrollPolicyRecord.Size));
		platform.Clear(scratch, MuiStringscrollPolicyRecord.Size);
		platform.Free(scratch, MuiStringscrollPolicyRecord.Size);
		return added;
	}

	private static bool WritePolicyRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringscrollPolicyState policy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!MuiStringscrollPolicyValidation.IsValidState(policy)) return false;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			PolicyStateKey);
		var value = default(MuiStringscrollPolicyRecord);
		value.Magic = MuiStringscrollPolicyRecord.Cookie;
		value.HorizBar = policy.HorizBar;
		value.NoInput = policy.NoInput;
		value.SetMin = policy.SetMin;
		value.SetVMin = policy.SetVMin;
		value.UseWinBorder = policy.UseWinBorder;
		value.VertBar = policy.VertBar;
		value.VertScrollerOnly = policy.VertScrollerOnly;
		return MuiStringscrollPolicyRecordCodec.Write(ref platform, block, value);
	}

	internal static bool TryGetPolicyRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollPolicyRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadPolicyRecord(ref platform, state, obj, out value);

	public static bool TryReadScrollbarState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollScrollbarState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (!TryReadScrollbarRecord(ref platform, state, obj, out var stored))
			return false;
		result.HorizBar = stored.HorizBar;
		result.VertBar = stored.VertBar;
		return true;
	}

	private static bool TryReadScrollbarRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollScrollbarRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ScrollbarStateKey);
		if (MuiStoreCore.DataspaceLength(ref platform, state, obj,
			ScrollbarStateKey) != unchecked((int)MuiStringscrollScrollbarRecord.Size))
			return false;
		return MuiStringscrollScrollbarRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool EnsureScrollbarRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, APTR horizontalBar, APTR verticalBar)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (TryReadScrollbarRecord(ref platform, state, obj, out _)) return true;
		var value = default(MuiStringscrollScrollbarRecord);
		value.Magic = MuiStringscrollScrollbarRecord.Cookie;
		value.HorizBar = horizontalBar;
		value.VertBar = verticalBar;
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiStringscrollScrollbarRecord.Size);
		if (scratch.IsNull) return false;
		platform.Clear(scratch, MuiStringscrollScrollbarRecord.Size);
		var written = MuiStringscrollScrollbarRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			ScrollbarStateKey, scratch,
			unchecked((int)MuiStringscrollScrollbarRecord.Size));
		platform.Clear(scratch, MuiStringscrollScrollbarRecord.Size);
		platform.Free(scratch, MuiStringscrollScrollbarRecord.Size);
		return added;
	}

	private static bool WriteScrollbarState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringscrollScrollbarState scrollbars)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			ScrollbarStateKey);
		var value = default(MuiStringscrollScrollbarRecord);
		value.Magic = MuiStringscrollScrollbarRecord.Cookie;
		value.HorizBar = scrollbars.HorizBar;
		value.VertBar = scrollbars.VertBar;
		return MuiStringscrollScrollbarRecordCodec.Write(ref platform, block, value);
	}

	internal static bool TryGetScrollbarRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollScrollbarRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		TryReadScrollbarRecord(ref platform, state, obj, out value);

	public static bool TryReadCompositionState<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out MuiStringscrollCompositionState result)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		result = default;
		if (!TryReadCompositionAdmission(ref platform, state, obj,
			out var stored, out var present) || !present)
			return false;
		result.Horizontal = stored.Horizontal;
		result.Vertical = stored.Vertical;
		result.OwnedMask = stored.OwnedMask;
		return true;
	}

	private static bool TryReadCompositionRecord<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiStringscrollCompositionRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		return TryReadCompositionAdmission(ref platform, state, obj, out value,
			out var present) && present;
	}

	// Admission distinguishes an absent composition record from a malformed
	// present record. Consumers may fall back only for absence; malformed state
	// must fail closed instead of silently disabling composition.
	private static bool TryReadCompositionAdmission<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj,
		out MuiStringscrollCompositionRecord value, out bool present)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = default;
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			CompositionStateKey);
		var length = MuiStoreCore.DataspaceLength(ref platform, state, obj,
			CompositionStateKey);
		present = block.IsNotNull || length != 0;
		if (!present) return true;
		if (block.IsNull || length != unchecked((int)MuiStringscrollCompositionRecord.Size))
			return false;
		return MuiStringscrollCompositionRecordCodec.TryRead(ref platform, block,
			out value);
	}

	private static bool WriteCompositionRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringscrollCompositionRecord value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var block = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			CompositionStateKey);
		return MuiStringscrollCompositionRecordCodec.Write(ref platform, block,
			value);
	}

	private static bool EnsureCompositionRecord<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadCompositionAdmission(ref platform, state, obj, out _,
			out var present)) return false;
		if (present) return true;
		if (!TryReadScrollbarState(ref platform, state, obj, out var scrollbars) ||
			!TryReadPolicyState(ref platform, state, obj, out var policy)) return false;
		var value = default(MuiStringscrollCompositionRecord);
		value.Magic = MuiStringscrollCompositionRecord.Cookie;
		value.Horizontal = scrollbars.HorizBar;
		value.Vertical = scrollbars.VertBar;
		if (value.Horizontal.IsNull && policy.HorizBar != 0)
		{
			value.Horizontal = CreateAutomaticScrollbar(ref platform, state, obj,
				true);
			if (value.Horizontal.IsNotNull)
				value.OwnedMask |= MuiStringscrollCompositionState.HorizontalOwned;
		}
		if (value.Vertical.IsNull && policy.VertBar != 0)
		{
			value.Vertical = CreateAutomaticScrollbar(ref platform, state, obj,
				false);
			if (value.Vertical.IsNotNull)
				value.OwnedMask |= MuiStringscrollCompositionState.VerticalOwned;
		}
		var scratch = MuiHeadlessMemory.Allocate(ref platform,
			MuiStringscrollCompositionRecord.Size);
		if (scratch.IsNull)
		{
			DisposeOwnedScrollbar(ref platform, state, value.Horizontal,
				(value.OwnedMask & MuiStringscrollCompositionState.HorizontalOwned) != 0);
			DisposeOwnedScrollbar(ref platform, state, value.Vertical,
				(value.OwnedMask & MuiStringscrollCompositionState.VerticalOwned) != 0);
			return false;
		}
		platform.Clear(scratch, MuiStringscrollCompositionRecord.Size);
		var written = MuiStringscrollCompositionRecordCodec.Write(ref platform,
			scratch, value);
		var added = written && MuiStoreCore.DataspaceAdd(ref platform, state, obj,
			CompositionStateKey, scratch,
			unchecked((int)MuiStringscrollCompositionRecord.Size));
		platform.Clear(scratch, MuiStringscrollCompositionRecord.Size);
		platform.Free(scratch, MuiStringscrollCompositionRecord.Size);
		if (added) return true;
		DisposeOwnedScrollbar(ref platform, state, value.Horizontal,
			(value.OwnedMask & MuiStringscrollCompositionState.HorizontalOwned) != 0);
		DisposeOwnedScrollbar(ref platform, state, value.Vertical,
			(value.OwnedMask & MuiStringscrollCompositionState.VerticalOwned) != 0);
		return false;
	}

	private static bool ReconcileComposition<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringscrollScrollbarState scrollbars,
		MuiStringscrollPolicyState policy)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadCompositionAdmission(ref platform, state, obj,
			out var value, out var present)) return false;
		if (!present)
			return EnsureCompositionRecord(ref platform, state, obj);
		ReconcileCompositionAxis(ref platform, state, obj, ref value.Horizontal,
			ref value.OwnedMask, MuiStringscrollCompositionState.HorizontalOwned,
			scrollbars.HorizBar, policy.HorizBar != 0, true);
		ReconcileCompositionAxis(ref platform, state, obj, ref value.Vertical,
			ref value.OwnedMask, MuiStringscrollCompositionState.VerticalOwned,
			scrollbars.VertBar, policy.VertBar != 0, false);
		return WriteCompositionRecord(ref platform, state, obj, value);
	}

	private static void ReconcileCompositionAxis<TPlatform>(ref TPlatform platform,
		APTR state, APTR owner, ref APTR current, ref uint ownedMask,
		uint ownedFlag, APTR supplied, bool enabled, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var owned = (ownedMask & ownedFlag) != 0;
		if (!enabled)
		{
			DisposeOwnedScrollbar(ref platform, state, current, owned);
			current = APTR.Null;
			ownedMask &= ~ownedFlag;
			return;
		}
		if (supplied.IsNotNull)
		{
			if (owned && current.Raw != supplied.Raw)
				DisposeOwnedScrollbar(ref platform, state, current, true);
			current = supplied;
			ownedMask &= ~ownedFlag;
			return;
		}
		if (current.IsNotNull && owned) return;
		if (current.IsNotNull) return;
		current = CreateAutomaticScrollbar(ref platform, state, owner, horizontal);
		if (current.IsNotNull) ownedMask |= ownedFlag;
	}

	private static APTR FindScrollbarClass<TPlatform>(ref TPlatform platform,
		APTR state) where TPlatform : struct, IMuiHeadlessPlatform
	{
		var name = MuiHeadlessMemory.Allocate(ref platform, 14);
		if (name.IsNull) return APTR.Null;
		platform.WriteUInt8(name, 0, (byte)'s');
		platform.WriteUInt8(name, 1, (byte)'c');
		platform.WriteUInt8(name, 2, (byte)'r');
		platform.WriteUInt8(name, 3, (byte)'o');
		platform.WriteUInt8(name, 4, (byte)'l');
		platform.WriteUInt8(name, 5, (byte)'l');
		platform.WriteUInt8(name, 6, (byte)'b');
		platform.WriteUInt8(name, 7, (byte)'a');
		platform.WriteUInt8(name, 8, (byte)'r');
		platform.WriteUInt8(name, 9, (byte)'.');
		platform.WriteUInt8(name, 10, (byte)'m');
		platform.WriteUInt8(name, 11, (byte)'u');
		platform.WriteUInt8(name, 12, (byte)'i');
		platform.WriteUInt8(name, 13, 0);
		var classRecord = MuiHeadlessObjectCore.FindClassByName(ref platform, state,
			name);
		platform.Clear(name, 14);
		platform.Free(name, 14);
		return classRecord;
	}

	private static APTR CreateAutomaticScrollbar<TPlatform>(
		ref TPlatform platform, APTR state, APTR owner, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		var classRecord = FindScrollbarClass(ref platform, state);
		if (classRecord.IsNull) return APTR.Null;
		var tags = MuiHeadlessMemory.Allocate(ref platform, 12);
		if (tags.IsNull) return APTR.Null;
		platform.WriteUInt32(tags, 0, MuiCommonControlCore.GroupHoriz);
		platform.WriteUInt32(tags, 4, horizontal ? 1u : 0u);
		platform.WriteUInt32(tags, 8, 0);
		var child = MuiCommonControlCore.CreateControl(ref platform, state,
			classRecord, tags);
		platform.Clear(tags, 12);
		platform.Free(tags, 12);
		return child;
	}

	private static void DisposeOwnedScrollbar<TPlatform>(ref TPlatform platform,
		APTR state, APTR child, bool owned)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!owned || child.IsNull || MuiHeadlessObjectCore.FindObject(ref platform,
			state, child).IsNull) return;
		MuiHeadlessObjectCore.DisposeObject(ref platform, state, child);
	}

	private static bool TryReadPolicyState<TPlatform>(ref TPlatform platform,
		APTR record, out MuiStringscrollPolicyState result)
		where TPlatform : struct, IMuiGuestMemory
	{
		result = default;
		result.HorizBar = ReadRaw(ref platform, record, HorizBar, 0) == 0 ?
			0u : 1u;
		result.NoInput = ReadRaw(ref platform, record, NoInput, 0) == 0 ?
			0u : 1u;
		result.SetMin = ReadRaw(ref platform, record, SetMin, 0) == 0 ?
			0u : 1u;
		result.SetVMin = ReadRaw(ref platform, record, SetVMin, 0) == 0 ?
			0u : 1u;
		result.UseWinBorder = ReadRaw(ref platform, record, UseWinBorder, 0) == 0 ?
			0u : 1u;
		result.VertBar = ReadRaw(ref platform, record, VertBar, 0) == 0 ?
			0u : 1u;
		result.VertScrollerOnly = ReadRaw(ref platform, record,
			VertScrollerOnly, 0) == 0 ? 0u : 1u;
		return true;
	}

	private static bool TryReadOwnedString<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, out APTR value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		value = APTR.Null;
		if (!TryReadState(ref platform, state, obj, out var stateValue))
			return false;
		var owned = MuiStoreCore.DataspaceFind(ref platform, state, obj,
			StringKey);
		if (stateValue.String.IsNull)
		{
			// NULL is the valid empty state.  A non-NULL ownership block beside it
			// would indicate a broken typed transition, so reject the divergence.
			if (owned.IsNotNull) return false;
			return true;
		}
		if (owned.IsNull || owned.Raw != stateValue.String.Raw ||
			!CStringCodec.TryReadLength(ref platform, owned, MaximumStringLength,
				out _)) return false;
		value = owned;
		return true;
	}

	private static bool NormalizePolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR record)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadPolicyState(ref platform, record, out var policy)) return false;
		return SetRaw(ref platform, state, record, HorizBar, policy.HorizBar, false) &&
			SetRaw(ref platform, state, record, NoInput, policy.NoInput, false) &&
			SetRaw(ref platform, state, record, SetMin, policy.SetMin, false) &&
			SetRaw(ref platform, state, record, SetVMin, policy.SetVMin, false) &&
			SetRaw(ref platform, state, record, UseWinBorder,
				policy.UseWinBorder, false) &&
			SetRaw(ref platform, state, record, VertBar, policy.VertBar, false) &&
			SetRaw(ref platform, state, record, VertScrollerOnly,
				policy.VertScrollerOnly, false);
	}

	// Publish one canonical policy record back to the public attribute store.
	// The changed attribute is written last so SetRaw can preserve the existing
	// one-attribute notification contract without making the policy consumers
	// depend on positional store entries.
	private static bool WritePolicyState<TPlatform>(ref TPlatform platform,
		APTR state, APTR record, MuiStringscrollPolicyState policy,
		uint changedAttribute, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (changedAttribute != HorizBar && !SetRaw(ref platform, state, record,
			HorizBar, policy.HorizBar, false)) return false;
		if (changedAttribute != NoInput && !SetRaw(ref platform, state, record,
			NoInput, policy.NoInput, false)) return false;
		if (changedAttribute != SetMin && !SetRaw(ref platform, state, record,
			SetMin, policy.SetMin, false)) return false;
		if (changedAttribute != SetVMin && !SetRaw(ref platform, state, record,
			SetVMin, policy.SetVMin, false)) return false;
		if (changedAttribute != UseWinBorder && !SetRaw(ref platform, state,
			record, UseWinBorder, policy.UseWinBorder, false)) return false;
		if (changedAttribute != VertBar && !SetRaw(ref platform, state, record,
			VertBar, policy.VertBar, false)) return false;
		if (changedAttribute != VertScrollerOnly && !SetRaw(ref platform, state,
			record, VertScrollerOnly, policy.VertScrollerOnly, false)) return false;
		if (changedAttribute == HorizBar)
			return SetRaw(ref platform, state, record, HorizBar,
				policy.HorizBar, notify);
		if (changedAttribute == NoInput)
			return SetRaw(ref platform, state, record, NoInput, policy.NoInput,
				notify);
		if (changedAttribute == SetMin)
			return SetRaw(ref platform, state, record, SetMin, policy.SetMin,
				notify);
		if (changedAttribute == SetVMin)
			return SetRaw(ref platform, state, record, SetVMin, policy.SetVMin,
				notify);
		if (changedAttribute == UseWinBorder)
			return SetRaw(ref platform, state, record, UseWinBorder,
				policy.UseWinBorder, notify);
		if (changedAttribute == VertBar)
			return SetRaw(ref platform, state, record, VertBar, policy.VertBar,
				notify);
		if (changedAttribute == VertScrollerOnly)
			return SetRaw(ref platform, state, record, VertScrollerOnly,
				policy.VertScrollerOnly, notify);
		return false;
	}

	private static bool HorizontalPolicy<TPlatform>(ref TPlatform platform,
		APTR record) where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadPolicyState(ref platform, record, out var policy))
			return false;
		return policy.HorizBar != 0 && policy.VertScrollerOnly == 0 &&
			policy.UseWinBorder == 0;
	}

	private static bool VerticalPolicy<TPlatform>(ref TPlatform platform,
		APTR record) where TPlatform : struct, IMuiGuestMemory
	{
		if (!TryReadPolicyState(ref platform, record, out var policy))
			return false;
		return (policy.VertBar != 0 || policy.VertScrollerOnly != 0) &&
			policy.UseWinBorder == 0;
	}

	// Bar visibility is driven by overflow, not merely by the enable flags. The
	// two bars affect each other's viewport, so iterate the monotonic dependency
	// twice: once for the vertical reservation and once for the horizontal one.
	// This keeps short strings free of empty bars while still showing both bars
	// when either axis makes the other overflow.
	private static bool HorizontalVisible<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollState scrollState,
		MuiStringscrollLayoutState layout)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!HorizontalPolicy(ref platform, record)) return false;
		if (layout.Width <= 0 || layout.Height <= 0) return false;
		var width = unchecked((uint)layout.Width);
		var height = unchecked((uint)layout.Height);
		var contentWidth = scrollState.ContentWidth;
		var contentHeight = scrollState.ContentHeight;
		var vertical = false;
		var horizontal = false;
		for (var i = 0; i < 2; i++)
		{
			horizontal = contentWidth > Available(width, vertical);
			vertical = VerticalPolicy(ref platform, record) &&
				contentHeight > Available(height, horizontal);
		}
		return horizontal;
	}

	private static bool VerticalVisible<TPlatform>(ref TPlatform platform,
		APTR record, MuiStringscrollState scrollState,
		MuiStringscrollLayoutState layout)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (!VerticalPolicy(ref platform, record)) return false;
		if (layout.Width <= 0 || layout.Height <= 0) return false;
		var width = unchecked((uint)layout.Width);
		var height = unchecked((uint)layout.Height);
		var contentWidth = scrollState.ContentWidth;
		var contentHeight = scrollState.ContentHeight;
		var vertical = false;
		var horizontal = false;
		for (var i = 0; i < 2; i++)
		{
			horizontal = HorizontalPolicy(ref platform, record) &&
				contentWidth > Available(width, vertical);
			vertical = contentHeight > Available(height, horizontal);
		}
		return vertical;
	}

	private static uint Available(uint dimension, bool reserve) =>
		!reserve ? dimension : dimension > ScrollerExtent ?
			dimension - ScrollerExtent : 0;

	private static uint SaturatingDimension(uint value) =>
		value > MaximumDimension ? MaximumDimension : value;

	// ---- Layout and rendering -------------------------------------------------

	public static bool AskMinMax<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, APTR storage) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!platform.IsMapped(storage, 12) || !Recompute(ref platform, state, obj))
			return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !TryReadState(ref platform, state, obj,
			out var scrollState) || !TryReadPolicyState(ref platform, record,
			out var policy)) return false;
		var minWidth = policy.SetMin != 0 ?
			scrollState.ContentWidth :
			CharacterWidth;
		var minHeight = policy.SetVMin != 0 ?
			scrollState.ContentHeight :
			CharacterHeight;
		if (minWidth > 32767) minWidth = 32767;
		if (minHeight > 32767) minHeight = 32767;
		MuiMinMaxValues values = default;
		values.MinWidth = unchecked((short)minWidth);
		values.MinHeight = unchecked((short)minHeight);
		values.MaxWidth = 10000;
		values.MaxHeight = 10000;
		values.DefWidth = values.MinWidth;
		values.DefHeight = values.MinHeight;
		return MuiAreaLayoutCore.WriteMinMax(ref platform, storage, values);
	}

	public static bool Layout<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, int left, int top, int width, int height)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!MuiAreaLayoutCore.Layout(ref platform, state, obj, left, top, width,
			height) || !SyncLayoutRecord(ref platform, state, obj) ||
			!Recompute(ref platform, state, obj)) return false;
		return LayoutCompositionChildren(ref platform, state, obj);
	}

	private static bool LayoutCompositionChildren<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadCompositionAdmission(ref platform, state, obj,
			out var composition, out var present)) return false;
		if (!present ||
			!TryReadLayoutState(ref platform, state, obj, out var layout) ||
			!TryReadViewportState(ref platform, state, obj, out var viewport))
			return true;
		if (!LayoutCompositionChild(ref platform, state, composition.Horizontal,
			layout.Left, layout.Top + layout.Height - ScrollerExtent,
			viewport.ViewportWidth, ScrollerExtent,
			viewport.HorizontalVisible != 0)) return false;
		return LayoutCompositionChild(ref platform, state, composition.Vertical,
			layout.Left + layout.Width - ScrollerExtent, layout.Top,
			ScrollerExtent, viewport.ViewportHeight,
			viewport.VerticalVisible != 0);
	}

	private static bool LayoutCompositionChild<TPlatform>(ref TPlatform platform,
		APTR state, APTR child, int left, int top, int width, int height,
		bool shown) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (child.IsNull || MuiHeadlessObjectCore.FindObject(ref platform, state,
			child).IsNull) return true;
		var resolvedWidth = shown ? width : 0;
		var resolvedHeight = shown ? height : 0;
		return MuiCommonControlCore.LayoutScrollbar(ref platform, state, child,
			left, top, resolvedWidth, resolvedHeight);
	}

	public static bool Draw<TPlatform>(ref TPlatform platform, APTR state,
		APTR obj, uint flags) where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!Recompute(ref platform, state, obj)) return false;
		var record = MuiHeadlessObjectCore.FindObject(ref platform, state, obj);
		if (record.IsNull || !TryReadState(ref platform, state, obj,
			out var scrollState) || !TryReadLayoutState(ref platform, state, obj,
			out var layout) || !TryReadRenderState(ref platform, state, obj,
			out var renderState)) return false;
		var rastPort = renderState.RastPort;
		var text = scrollState.String;
		var length = 0u;
		if (text.IsNotNull && !CStringCodec.TryReadLength(ref platform, text,
			MaximumStringLength, out length)) return false;
		var left = layout.Left;
		var top = layout.Top;
		var width = layout.Width;
		var height = layout.Height;
		if (!TryComputeViewportState(ref platform, record, scrollState, layout,
			out var viewport)) return false;
		MuiAreaTextColorCore.Apply(ref platform, state, obj, rastPort);
		var viewportWidth = viewport.ViewportWidth;
		var viewportHeight = viewport.ViewportHeight;
		if (width <= 0 || height <= 0) return false;
		var hasHorizontalChild = HasLiveCompositionChild(ref platform, state, obj,
			true);
		var hasVerticalChild = HasLiveCompositionChild(ref platform, state, obj,
			false);
		if ((hasHorizontalChild || hasVerticalChild) &&
			!DrawCompositionChildren(ref platform, state, obj, renderState,
				viewport)) return false;
		if (!platform.LockLayer(rastPort)) return false;
		if (!platform.BeginUpdate(rastPort))
		{
			platform.UnlockLayer(rastPort);
			return false;
		}
		var clip = platform.PushClip(rastPort, left, top, width, height);
		var x = scrollState.ScrollX;
		var y = scrollState.ScrollY;
		if (text.IsNotNull && length != 0 && viewportWidth != 0 &&
			viewportHeight != 0)
		{
			var font = renderState.Font;
			var firstLine = y / CharacterHeight;
			var firstColumn = x / CharacterWidth;
			var visibleColumns = unchecked((uint)(viewportWidth / (int)CharacterWidth));
			var line = 0u;
			var position = 0u;
			var baseline = top + (int)CharacterHeight;
			while (position < length && baseline <= top + viewportHeight)
			{
				var end = position;
				while (end < length && platform.ReadUInt8(
					APTR.FromPointer(text.Raw + end), 0) != (byte)'\n') end++;
				var logicalEnd = end;
				if (logicalEnd > position && platform.ReadUInt8(
					APTR.FromPointer(text.Raw + logicalEnd - 1), 0) == (byte)'\r')
					logicalEnd--;
				if (line >= firstLine)
				{
					var start = ByteOffsetForColumns(ref platform, text, position,
						logicalEnd, firstColumn);
					var drawEnd = ByteOffsetForColumns(ref platform, text, start,
						logicalEnd, visibleColumns);
					var available = drawEnd > start ? drawEnd - start : 0u;
					if (available != 0)
						platform.DrawText(rastPort, font,
							left, baseline, APTR.FromPointer(text.Raw + start),
							unchecked((int)available));
					baseline += (int)CharacterHeight;
				}
				position = end < length ? end + 1 : end;
				line++;
			}
		}
		platform.SetPen(rastPort, 4);
		if (!hasHorizontalChild && viewport.HorizontalVisible != 0 &&
			height >= ScrollerExtent)
		{
			var horizontal = default(MuiStringscrollBarGeometry);
			if (TryBuildHorizontalBar(scrollState, viewport, left, top, height,
				out horizontal))
			{
				platform.FillRectangle(rastPort, horizontal.TrackLeft,
					horizontal.TrackTop, horizontal.TrackRight,
					horizontal.TrackBottom);
				platform.SetPen(rastPort, 2);
				platform.FillRectangle(rastPort, horizontal.ThumbLeft,
					horizontal.ThumbTop, horizontal.ThumbRight,
					horizontal.ThumbBottom);
			}
		}
		if (!hasVerticalChild && viewport.VerticalVisible != 0 &&
			width >= ScrollerExtent)
		{
			var vertical = default(MuiStringscrollBarGeometry);
			if (TryBuildVerticalBar(scrollState, viewport, left, top, width,
				out vertical))
			{
				platform.SetPen(rastPort, 4);
				platform.FillRectangle(rastPort, vertical.TrackLeft,
					vertical.TrackTop, vertical.TrackRight,
					vertical.TrackBottom);
				platform.SetPen(rastPort, 2);
				platform.FillRectangle(rastPort, vertical.ThumbLeft,
					vertical.ThumbTop, vertical.ThumbRight,
					vertical.ThumbBottom);
			}
		}
		platform.PopClip(rastPort, clip);
		platform.EndUpdate(rastPort, true);
		platform.UnlockLayer(rastPort);
		return true;
	}

	private static bool HasLiveCompositionChild<TPlatform>(
		ref TPlatform platform, APTR state, APTR obj, bool horizontal)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (!TryReadCompositionAdmission(ref platform, state, obj,
			out var composition, out var present) || !present)
			return false;
		var child = horizontal ? composition.Horizontal : composition.Vertical;
		return child.IsNotNull && MuiHeadlessObjectCore.FindObject(ref platform,
			state, child).IsNotNull;
	}

	private static bool DrawCompositionChildren<TPlatform>(ref TPlatform platform,
		APTR state, APTR obj, MuiStringscrollRenderState renderState,
		MuiStringscrollViewportState viewport)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (!TryReadCompositionAdmission(ref platform, state, obj,
			out var composition, out var present)) return false;
		if (!present)
			return true;
		if (viewport.HorizontalVisible != 0 && composition.Horizontal.IsNotNull &&
			!DrawCompositionChild(ref platform, state, composition.Horizontal,
				renderState)) return false;
		if (viewport.VerticalVisible != 0 && composition.Vertical.IsNotNull &&
			!DrawCompositionChild(ref platform, state, composition.Vertical,
				renderState)) return false;
		return true;
	}

	private static bool DrawCompositionChild<TPlatform>(ref TPlatform platform,
		APTR state, APTR child, MuiStringscrollRenderState renderState)
		where TPlatform : struct, IMuiLayoutPlatform
	{
		if (MuiHeadlessObjectCore.FindObject(ref platform, state, child).IsNull)
			return true;
		if (!MuiHeadlessObjectCore.SetAttribute(ref platform, state, child,
			RenderInfo, renderState.RenderInfo.Raw, false)) return false;
		return MuiCommonControlCore.DrawControl(ref platform, state, child, 0);
	}

	// Build a proportional horizontal thumb from the already-normalized
	// viewport. All values are bounded by the layout dimensions and use integer
	// arithmetic so the 68k path has no floating-point or managed dependency.
	private static bool TryBuildHorizontalBar(
		MuiStringscrollState scrollState, MuiStringscrollViewportState viewport,
		int left, int top, int height, out MuiStringscrollBarGeometry result)
	{
		result = default;
		if (viewport.ViewportWidth <= 0 || height < ScrollerExtent ||
			viewport.MaxScrollX == 0 || scrollState.ContentWidth == 0)
			return false;
		var trackLength = viewport.ViewportWidth;
		var thumbLength = ThumbLength(trackLength, unchecked((int)
			viewport.ViewportWidth), scrollState.ContentWidth);
		if (thumbLength <= 0) return false;
		var travel = trackLength - thumbLength;
		var start = ScaleThumbOffset(travel, scrollState.ScrollX,
			viewport.MaxScrollX);
		result.TrackLeft = left;
		result.TrackTop = top + height - ScrollerExtent;
		result.TrackRight = left + trackLength - 1;
		result.TrackBottom = top + height - 1;
		result.ThumbLeft = left + start;
		result.ThumbTop = result.TrackTop;
		result.ThumbRight = result.ThumbLeft + thumbLength - 1;
		result.ThumbBottom = result.TrackBottom;
		return true;
	}

	// Build a proportional vertical thumb using the same policy as the
	// horizontal axis, with the track occupying the reserved right edge.
	private static bool TryBuildVerticalBar(
		MuiStringscrollState scrollState, MuiStringscrollViewportState viewport,
		int left, int top, int width, out MuiStringscrollBarGeometry result)
	{
		result = default;
		if (viewport.ViewportHeight <= 0 || width < ScrollerExtent ||
			viewport.MaxScrollY == 0 || scrollState.ContentHeight == 0)
			return false;
		var trackLength = viewport.ViewportHeight;
		var thumbLength = ThumbLength(trackLength, unchecked((int)
			viewport.ViewportHeight), scrollState.ContentHeight);
		if (thumbLength <= 0) return false;
		var travel = trackLength - thumbLength;
		var start = ScaleThumbOffset(travel, scrollState.ScrollY,
			viewport.MaxScrollY);
		result.TrackLeft = left + width - ScrollerExtent;
		result.TrackTop = top;
		result.TrackRight = left + width - 1;
		result.TrackBottom = top + trackLength - 1;
		result.ThumbLeft = result.TrackLeft;
		result.ThumbTop = top + start;
		result.ThumbRight = result.TrackRight;
		result.ThumbBottom = result.ThumbTop + thumbLength - 1;
		return true;
	}

	private static int ThumbLength(int trackLength, int viewportLength,
		uint contentLength)
	{
		if (trackLength <= 0 || viewportLength <= 0 || contentLength == 0)
			return 0;
		var length = trackLength * viewportLength /
			unchecked((int)contentLength);
		var minimum = ScrollerExtent / 2;
		if (length < minimum) length = minimum;
		if (length > trackLength) length = trackLength;
		return length;
	}

	private static int ScaleThumbOffset(int travel, uint position, uint maximum)
	{
		if (travel <= 0 || maximum == 0) return 0;
		if (position > maximum) position = maximum;
		return unchecked((int)((uint)travel * position / maximum));
	}

	// Decode enough UTF-8 to keep metrics and horizontal scrolling aligned with
	// MorphOS Stringscroll's codepoint-oriented behavior. Malformed sequences
	// remain visible as one-byte characters instead of turning a guest string
	// into a fatal object failure.
	internal static bool TryReadUtf8<TPlatform>(ref TPlatform platform, APTR text,
		uint index, uint length, out uint codePoint, out uint byteCount)
		where TPlatform : struct, IMuiGuestMemory
	{
		codePoint = 0;
		byteCount = 1;
		if (index >= length) return false;
		var first = platform.ReadUInt8(APTR.FromPointer(text.Raw + index), 0);
		if (first < 0x80)
		{
			codePoint = first;
			return true;
		}
		if (first >= 0xC2 && first <= 0xDF && index + 1 < length)
		{
			var second = platform.ReadUInt8(APTR.FromPointer(text.Raw + index + 1), 0);
			if ((second & 0xC0) == 0x80)
			{
				codePoint = (uint)(first & 0x1F) << 6 |
					(uint)(second & 0x3F);
				byteCount = 2;
				return true;
			}
		}
		if (first >= 0xE0 && first <= 0xEF && index + 2 < length)
		{
			var second = platform.ReadUInt8(APTR.FromPointer(text.Raw + index + 1), 0);
			var third = platform.ReadUInt8(APTR.FromPointer(text.Raw + index + 2), 0);
			var validSecond = (second & 0xC0) == 0x80 &&
				(first != 0xE0 || second >= 0xA0) &&
				(first != 0xED || second <= 0x9F);
			if (validSecond && (third & 0xC0) == 0x80)
			{
				codePoint = (uint)(first & 0x0F) << 12 |
					(uint)(second & 0x3F) << 6 | (uint)(third & 0x3F);
				byteCount = 3;
				return true;
			}
		}
		if (first >= 0xF0 && first <= 0xF4 && index + 3 < length)
		{
			var second = platform.ReadUInt8(APTR.FromPointer(text.Raw + index + 1), 0);
			var third = platform.ReadUInt8(APTR.FromPointer(text.Raw + index + 2), 0);
			var fourth = platform.ReadUInt8(APTR.FromPointer(text.Raw + index + 3), 0);
			var validSecond = (second & 0xC0) == 0x80 &&
				(first != 0xF0 || second >= 0x90) &&
				(first != 0xF4 || second <= 0x8F);
			if (validSecond && (third & 0xC0) == 0x80 &&
				(fourth & 0xC0) == 0x80)
			{
				codePoint = (uint)(first & 0x07) << 18 |
					(uint)(second & 0x3F) << 12 |
					(uint)(third & 0x3F) << 6 | (uint)(fourth & 0x3F);
				byteCount = 4;
				return true;
			}
		}
		codePoint = first;
		return false;
	}

	internal static uint ByteOffsetForColumns<TPlatform>(ref TPlatform platform,
		APTR text, uint start, uint end, uint columns)
		where TPlatform : struct, IMuiGuestMemory
	{
		var index = start;
		var remaining = columns;
		while (index < end && remaining != 0)
		{
			if (!TryReadUtf8(ref platform, text, index, end, out _,
				out var bytes)) bytes = 1;
			index += bytes;
			remaining--;
		}
		return index;
	}

	// ---- Small raw-store helpers ---------------------------------------------

	private static bool EnsureDefault<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value)
		where TPlatform : struct, IMuiHeadlessPlatform
	{
		if (ReadRaw(ref platform, record, attribute, uint.MaxValue) != uint.MaxValue)
			return true;
		return SetRaw(ref platform, state, record, attribute, value, false);
	}

	private static uint ReadRaw<TPlatform>(ref TPlatform platform, APTR record,
		uint attribute, uint fallback) where TPlatform : struct, IMuiGuestMemory
	{
		if (!MuiHeadlessObjectCodec.TryRead(ref platform, record,
			out var objectValue)) return fallback;
		var current = objectValue.Attributes;
		uint visited = 0;
		while (current.IsNotNull && visited++ < MuiHeadlessLayout.MaximumTraversal)
		{
			if (!MuiHeadlessAttributeCodec.TryRead(ref platform, current,
				out var attributeValue)) return fallback;
			if (attributeValue.Id == attribute) return attributeValue.Value;
			current = attributeValue.Next;
		}
		return fallback;
	}

	private static bool SetRaw<TPlatform>(ref TPlatform platform, APTR state,
		APTR record, uint attribute, uint value, bool notify)
		where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.SetRecordAttribute(ref platform, state, record,
			attribute, value, notify);

	private static uint Read<TPlatform>(ref TPlatform platform, APTR state, APTR obj,
		uint attribute, uint fallback) where TPlatform : struct, IMuiHeadlessPlatform =>
		MuiHeadlessObjectCore.GetAttribute(ref platform, state, obj, attribute,
			out var value) ? value : fallback;
}
