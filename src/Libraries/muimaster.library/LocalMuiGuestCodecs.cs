/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;
using Amiga;

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
		for (var index = 0u; index < maximumLength; index++)
		{
			if (value.Raw > uint.MaxValue - index) return false;
			var address = APTR.FromPointer(value.Raw + index);
			if (!memory.IsMapped(address, 1)) return false;
			if (memory.ReadUInt8(address, 0) != 0) continue;
			length = index;
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
		for (var index = 0u; index < maximumLength; index++)
		{
			if (left.Raw > uint.MaxValue - index ||
				right.Raw > uint.MaxValue - index) return false;
			var leftAddress = APTR.FromPointer(left.Raw + index);
			var rightAddress = APTR.FromPointer(right.Raw + index);
			if (!memory.IsMapped(leftAddress, 1) ||
				!memory.IsMapped(rightAddress, 1)) return false;
			var leftByte = memory.ReadUInt8(leftAddress, 0);
			var rightByte = memory.ReadUInt8(rightAddress, 0);
			if (leftByte != rightByte) return true;
			if (leftByte != 0) continue;
			equal = true;
			return true;
		}
		return false;
	}
}
}

namespace Amiga.MUI
{

// The only public MUIMaster guest packet absent from the current SDK package
// are the small layout packet types. Keep their values as named structs so
// the MUI hook bridge never depends on an anonymous offset table.
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
	public uint lm_Type;
	public APTR lm_Children;
	public MUI_MinMax lm_MinMax;
	public MUI_LayoutDimensions lm_Layout;
}

public static class MUI_LayoutMsgCodec
{
	public const uint Size = MUI_LayoutMsg.Size;

	public static bool TryRead<TMemory>(ref TMemory memory, APTR address,
		out MUI_LayoutMsg value) where TMemory : struct, IAmigaGuestMemory
	{
		value = default;
		if (address.IsNull || !memory.IsMapped(address, Size)) return false;
		value.lm_Type = memory.ReadUInt32(address, 0);
		value.lm_Children = APTR.FromPointer(memory.ReadUInt32(address, 4));
		value.lm_MinMax.MinWidth = unchecked((short)memory.ReadUInt16(address, 8));
		value.lm_MinMax.MinHeight = unchecked((short)memory.ReadUInt16(address, 10));
		value.lm_MinMax.MaxWidth = unchecked((short)memory.ReadUInt16(address, 12));
		value.lm_MinMax.MaxHeight = unchecked((short)memory.ReadUInt16(address, 14));
		value.lm_MinMax.DefWidth = unchecked((short)memory.ReadUInt16(address, 16));
		value.lm_MinMax.DefHeight = unchecked((short)memory.ReadUInt16(address, 18));
		value.lm_Layout.Width = unchecked((int)memory.ReadUInt32(address, 20));
		value.lm_Layout.Height = unchecked((int)memory.ReadUInt32(address, 24));
		value.lm_Layout.priv5 = memory.ReadUInt32(address, 28);
		value.lm_Layout.priv6 = memory.ReadUInt32(address, 32);
		return true;
	}

	public static bool Write<TMemory>(ref TMemory memory, APTR address,
		MUI_LayoutMsg value) where TMemory : struct, IAmigaGuestMemory
	{
		if (address.IsNull || !memory.IsMapped(address, Size)) return false;
		memory.WriteUInt32(address, 0, value.lm_Type);
		memory.WriteUInt32(address, 4, value.lm_Children.Raw);
		memory.WriteUInt16(address, 8, unchecked((ushort)value.lm_MinMax.MinWidth));
		memory.WriteUInt16(address, 10, unchecked((ushort)value.lm_MinMax.MinHeight));
		memory.WriteUInt16(address, 12, unchecked((ushort)value.lm_MinMax.MaxWidth));
		memory.WriteUInt16(address, 14, unchecked((ushort)value.lm_MinMax.MaxHeight));
		memory.WriteUInt16(address, 16, unchecked((ushort)value.lm_MinMax.DefWidth));
		memory.WriteUInt16(address, 18, unchecked((ushort)value.lm_MinMax.DefHeight));
		memory.WriteUInt32(address, 20, unchecked((uint)value.lm_Layout.Width));
		memory.WriteUInt32(address, 24, unchecked((uint)value.lm_Layout.Height));
		memory.WriteUInt32(address, 28, value.lm_Layout.priv5);
		memory.WriteUInt32(address, 32, value.lm_Layout.priv6);
		return true;
	}
}
}
