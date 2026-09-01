/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster
{

// Shared bounded cursor for C-string scans that cross the guest-memory
// boundary.  Callers keep a named base/index/limit value; only this adapter
// performs address arithmetic and mapped-byte admission.  The limit matches
// the largest C-string bound used by the MorphOS MUI surface (64 KiB).
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiCStringByteCursor
{
	internal const uint MaximumLength = 65536;
	internal APTR Base;
	internal uint Index;
	internal uint Limit;
}

internal static class MuiCStringByteCursorCodec
{
	internal static bool TryGetRange<TMemory>(ref TMemory memory,
		MuiCStringByteCursor cursor, uint byteCount, out APTR address)
		where TMemory : struct, IAmigaGuestMemory
	{
		address = APTR.Null;
		var text = APTR.FromPointer(cursor.Base.Raw);
		if (text.IsNull || cursor.Limit > MuiCStringByteCursor.MaximumLength ||
			cursor.Index > cursor.Limit || byteCount > cursor.Limit - cursor.Index ||
			text.Raw > uint.MaxValue - cursor.Index) return false;
		address = APTR.FromPointer(text.Raw + cursor.Index);
		return memory.IsMapped(address, byteCount);
	}

	internal static bool TryGetAddress<TMemory>(ref TMemory memory,
		MuiCStringByteCursor cursor, out APTR address)
		where TMemory : struct, IAmigaGuestMemory
	{
		address = APTR.Null;
		var text = APTR.FromPointer(cursor.Base.Raw);
		if (text.IsNull || cursor.Limit == 0 ||
			cursor.Limit > MuiCStringByteCursor.MaximumLength ||
			cursor.Index >= cursor.Limit ||
			text.Raw > uint.MaxValue - cursor.Index) return false;
		address = APTR.FromPointer(text.Raw + cursor.Index);
		return memory.IsMapped(address, 1);
	}

	internal static bool TryReadByte<TMemory>(ref TMemory memory,
		MuiCStringByteCursor cursor, out byte value)
		where TMemory : struct, IAmigaGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref memory, cursor, out var address)) return false;
		value = memory.ReadUInt8(address, 0);
		return true;
	}

	internal static bool TryWriteByte<TMemory>(ref TMemory memory,
		MuiCStringByteCursor cursor, byte value)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (!TryGetAddress(ref memory, cursor, out var address)) return false;
		memory.WriteUInt8(address, 0, value);
		return true;
	}
}

}

namespace Amiga
{

/// <summary>
/// Narrow, bounded C-string operations used by the MUI host model. The
/// published SDK package currently omits this guest-memory helper, so the MUI
/// project keeps the value-type implementation at its own ABI boundary.
/// </summary>
public static class CStringCodec
{
	public static bool TryReadLength<TMemory>(ref TMemory memory, APTR value,
		uint maximumLength, out uint length)
		where TMemory : struct, IAmigaGuestMemory
	{
		length = 0;
		if (value.IsNull) return false;
		var cursor = default(MuiCStringByteCursor);
		cursor.Base = value;
		cursor.Limit = maximumLength;
		for (cursor.Index = 0; cursor.Index < maximumLength; cursor.Index++)
		{
			if (!MuiCStringByteCursorCodec.TryReadByte(ref memory, cursor,
				out var current)) return false;
			if (current != 0) continue;
			length = cursor.Index;
			return true;
		}
		return false;
	}

	public static bool TryEquals<TMemory>(ref TMemory memory, APTR left,
		APTR right, uint maximumLength, out bool equal)
		where TMemory : struct, IAmigaGuestMemory
	{
		equal = left.Raw == right.Raw;
		if (equal) return true;
		if (left.IsNull || right.IsNull) return false;
		var leftCursor = default(MuiCStringByteCursor);
		leftCursor.Base = left;
		leftCursor.Limit = maximumLength;
		var rightCursor = default(MuiCStringByteCursor);
		rightCursor.Base = right;
		rightCursor.Limit = maximumLength;
		for (leftCursor.Index = 0; leftCursor.Index < maximumLength;
			leftCursor.Index++)
		{
			rightCursor.Index = leftCursor.Index;
			if (!MuiCStringByteCursorCodec.TryReadByte(ref memory, leftCursor,
				out var leftByte) || !MuiCStringByteCursorCodec.TryReadByte(
				ref memory, rightCursor, out var rightByte)) return false;
			if (leftByte != rightByte) return true;
			if (leftByte != 0) continue;
			equal = true;
			return true;
		}
		return false;
	}

	// B-tree-backed MorphOS maps expose keys in strcmp order. Keep comparison
	// bounded and guest-resident so store ordering never depends on managed
	// strings or a host-side collection.
	public static bool TryCompare<TMemory>(ref TMemory memory, APTR left,
		APTR right, uint maximumLength, out int comparison)
		where TMemory : struct, IAmigaGuestMemory
	{
		comparison = 0;
		if (left.Raw == right.Raw) return true;
		if (left.IsNull || right.IsNull) return false;
		var leftCursor = default(MuiCStringByteCursor);
		leftCursor.Base = left;
		leftCursor.Limit = maximumLength;
		var rightCursor = default(MuiCStringByteCursor);
		rightCursor.Base = right;
		rightCursor.Limit = maximumLength;
		for (leftCursor.Index = 0; leftCursor.Index < maximumLength;
			leftCursor.Index++)
		{
			rightCursor.Index = leftCursor.Index;
			if (!MuiCStringByteCursorCodec.TryReadByte(ref memory, leftCursor,
				out var leftByte) || !MuiCStringByteCursorCodec.TryReadByte(
				ref memory, rightCursor, out var rightByte)) return false;
			if (leftByte < rightByte) { comparison = -1; return true; }
			if (leftByte > rightByte) { comparison = 1; return true; }
			if (leftByte == 0) return true;
		}
		return false;
	}
}
}

namespace Amiga.MUI
{

// Older pinned packages omit these packets. Current SDK packages advertise
// their ownership through buildTransitive props; source SDK builds also use
// the authoritative types. Keep this compatibility fallback only when that
// capability is absent, not merely because the SDK came from a package.
#if !COPPEROS_SDK_MUI_LAYOUT
[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MUI_MinMax
{
	public short MinWidth;
	public short MinHeight;
	public short MaxWidth;
	public short MaxHeight;
	public short DefWidth;
	public short DefHeight;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MUI_LayoutDimensions
{
	public int Width;
	public int Height;
	public uint priv5;
	public uint priv6;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
public struct MUI_LayoutMsg
{
	public const uint Size = 36;
	internal const uint FieldSize32 = 4;
	internal const uint FieldSize16 = 2;
	internal const uint TypeOffset = 0;
	internal const uint ChildrenOffset = 4;
	internal const uint MinWidthOffset = 8;
	internal const uint MinHeightOffset = 10;
	internal const uint MaxWidthOffset = 12;
	internal const uint MaxHeightOffset = 14;
	internal const uint DefWidthOffset = 16;
	internal const uint DefHeightOffset = 18;
	internal const uint WidthOffset = 20;
	internal const uint HeightOffset = 24;
	internal const uint Private5Offset = 28;
	internal const uint Private6Offset = 32;
	public uint lm_Type;
	public APTR lm_Children;
	public MUI_MinMax lm_MinMax;
	public MUI_LayoutDimensions lm_Layout;
}
#endif

internal enum MUI_LayoutMsgField : byte
{
	Type,
	Children,
	MinWidth,
	MinHeight,
	MaxWidth,
	MaxHeight,
	DefWidth,
	DefHeight,
	Width,
	Height,
	Private5,
	Private6,
}

// The layout packet's mixed-width wire positions are confined to this
// adapter-owned table. The public packet itself is always exchanged as the
// named MUI_LayoutMsg struct (from the fallback above or the local SDK).
internal static class MUI_LayoutMsgLayout
{
	internal const uint FieldSize32 = 4;
	internal const uint FieldSize16 = 2;
	internal const uint TypeOffset = 0;
	internal const uint ChildrenOffset = 4;
	internal const uint MinWidthOffset = 8;
	internal const uint MinHeightOffset = 10;
	internal const uint MaxWidthOffset = 12;
	internal const uint MaxHeightOffset = 14;
	internal const uint DefWidthOffset = 16;
	internal const uint DefHeightOffset = 18;
	internal const uint WidthOffset = 20;
	internal const uint HeightOffset = 24;
	internal const uint Private5Offset = 28;
	internal const uint Private6Offset = 32;
}

// Struct-first adapter for the MorphOS layout hook packet. The hook bridge
// exchanges the named MUI_LayoutMsg value; this bounded layer alone translates
// its nested mixed-width guest representation.
internal static class MUI_LayoutMsgMemoryCodec
{
	private static bool TryResolve(MUI_LayoutMsgField field,
		out uint offset, out uint fieldSize)
	{
		switch (field)
		{
			case MUI_LayoutMsgField.Type:
				offset = MUI_LayoutMsgLayout.TypeOffset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize32; return true;
			case MUI_LayoutMsgField.Children:
				offset = MUI_LayoutMsgLayout.ChildrenOffset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize32; return true;
			case MUI_LayoutMsgField.MinWidth:
				offset = MUI_LayoutMsgLayout.MinWidthOffset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize16; return true;
			case MUI_LayoutMsgField.MinHeight:
				offset = MUI_LayoutMsgLayout.MinHeightOffset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize16; return true;
			case MUI_LayoutMsgField.MaxWidth:
				offset = MUI_LayoutMsgLayout.MaxWidthOffset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize16; return true;
			case MUI_LayoutMsgField.MaxHeight:
				offset = MUI_LayoutMsgLayout.MaxHeightOffset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize16; return true;
			case MUI_LayoutMsgField.DefWidth:
				offset = MUI_LayoutMsgLayout.DefWidthOffset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize16; return true;
			case MUI_LayoutMsgField.DefHeight:
				offset = MUI_LayoutMsgLayout.DefHeightOffset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize16; return true;
			case MUI_LayoutMsgField.Width:
				offset = MUI_LayoutMsgLayout.WidthOffset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize32; return true;
			case MUI_LayoutMsgField.Height:
				offset = MUI_LayoutMsgLayout.HeightOffset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize32; return true;
			case MUI_LayoutMsgField.Private5:
				offset = MUI_LayoutMsgLayout.Private5Offset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize32; return true;
			case MUI_LayoutMsgField.Private6:
				offset = MUI_LayoutMsgLayout.Private6Offset;
				fieldSize = MUI_LayoutMsgLayout.FieldSize32; return true;
		}
		offset = 0;
		fieldSize = 0;
		return false;
	}

	internal static bool TryGetAddress<TMemory>(ref TMemory memory,
		APTR record, MUI_LayoutMsgField field, out APTR address,
		out uint fieldSize) where TMemory : struct, IAmigaGuestMemory
	{
		address = APTR.Null;
		fieldSize = 0;
		if (!TryResolve(field, out var offset, out fieldSize) || record.IsNull ||
			record.Raw > uint.MaxValue - offset ||
			!memory.IsMapped(record, MUI_LayoutMsg.Size)) return false;
		address = APTR.FromPointer(record.Raw + offset);
		return memory.IsMapped(address, fieldSize);
	}

	internal static bool TryReadUInt32<TMemory>(ref TMemory memory,
		APTR record, MUI_LayoutMsgField field, out uint value)
		where TMemory : struct, IAmigaGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref memory, record, field, out var address,
			out var fieldSize) || fieldSize != MUI_LayoutMsgLayout.FieldSize32) return false;
		value = memory.ReadUInt32(address, 0);
		return true;
	}

	internal static bool TryReadUInt16<TMemory>(ref TMemory memory,
		APTR record, MUI_LayoutMsgField field, out ushort value)
		where TMemory : struct, IAmigaGuestMemory
	{
		value = 0;
		if (!TryGetAddress(ref memory, record, field, out var address,
			out var fieldSize) || fieldSize != MUI_LayoutMsgLayout.FieldSize16) return false;
		value = memory.ReadUInt16(address, 0);
		return true;
	}

	internal static bool TryWriteUInt32<TMemory>(ref TMemory memory,
		APTR record, MUI_LayoutMsgField field, uint value)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (!TryGetAddress(ref memory, record, field, out var address,
			out var fieldSize) || fieldSize != MUI_LayoutMsgLayout.FieldSize32) return false;
		memory.WriteUInt32(address, 0, value);
		return true;
	}

	internal static bool TryWriteUInt16<TMemory>(ref TMemory memory,
		APTR record, MUI_LayoutMsgField field, ushort value)
		where TMemory : struct, IAmigaGuestMemory
	{
		if (!TryGetAddress(ref memory, record, field, out var address,
			out var fieldSize) || fieldSize != MUI_LayoutMsgLayout.FieldSize16) return false;
		memory.WriteUInt16(address, 0, value);
		return true;
	}
}

public static class MUI_LayoutMsgCodec
{
	public const uint Size = MUI_LayoutMsg.Size;

	// Sequential record codec used by every production MUI layout-hook path.
	// The named packet is read in declaration order; the mixed-width wire
	// positions remain an implementation detail of the bounded cursor adapter.
	public static bool TryReadRecord<TMemory>(ref TMemory memory, APTR address,
		out MUI_LayoutMsg value) where TMemory : struct, IAmigaGuestMemory
	{
		value = default;
		if (!MuiGuestStructCursor.TryCreate(ref memory, address, Size,
			out var cursor) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var type) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var children) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out var minWidth) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out var minHeight) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out var maxWidth) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out var maxHeight) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out var defWidth) ||
			!MuiGuestStructCursor.TryReadUInt16(ref memory, ref cursor,
				out var defHeight) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var width) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var height) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var private5) ||
			!MuiGuestStructCursor.TryReadUInt32(ref memory, ref cursor,
				out var private6) ||
			!MuiGuestStructCursor.IsComplete(cursor)) return false;

		value.lm_Type = type;
		value.lm_Children = APTR.FromPointer(children);
		value.lm_MinMax.MinWidth = unchecked((short)minWidth);
		value.lm_MinMax.MinHeight = unchecked((short)minHeight);
		value.lm_MinMax.MaxWidth = unchecked((short)maxWidth);
		value.lm_MinMax.MaxHeight = unchecked((short)maxHeight);
		value.lm_MinMax.DefWidth = unchecked((short)defWidth);
		value.lm_MinMax.DefHeight = unchecked((short)defHeight);
		value.lm_Layout.Width = unchecked((int)width);
		value.lm_Layout.Height = unchecked((int)height);
		value.lm_Layout.priv5 = private5;
		value.lm_Layout.priv6 = private6;
		return true;
	}

	public static bool WriteRecord<TMemory>(ref TMemory memory, APTR address,
		MUI_LayoutMsg value) where TMemory : struct, IAmigaGuestMemory =>
		MuiGuestStructCursor.TryCreate(ref memory, address, Size,
			out var cursor) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.lm_Type) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.lm_Children.Raw) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			unchecked((ushort)value.lm_MinMax.MinWidth)) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			unchecked((ushort)value.lm_MinMax.MinHeight)) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			unchecked((ushort)value.lm_MinMax.MaxWidth)) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			unchecked((ushort)value.lm_MinMax.MaxHeight)) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			unchecked((ushort)value.lm_MinMax.DefWidth)) &&
		MuiGuestStructCursor.TryWriteUInt16(ref memory, ref cursor,
			unchecked((ushort)value.lm_MinMax.DefHeight)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			unchecked((uint)value.lm_Layout.Width)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			unchecked((uint)value.lm_Layout.Height)) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.lm_Layout.priv5) &&
		MuiGuestStructCursor.TryWriteUInt32(ref memory, ref cursor,
			value.lm_Layout.priv6) && MuiGuestStructCursor.IsComplete(cursor);

	public static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MUI_LayoutMsg value) where TMemory : struct, IAmigaGuestMemory
		=> TryReadRecord(ref memory, address, out value);

	public static bool Write<TMemory>(ref TMemory memory, APTR address,
		MUI_LayoutMsg value) where TMemory : struct, IAmigaGuestMemory
		=> WriteRecord(ref memory, address, value);
}
}
