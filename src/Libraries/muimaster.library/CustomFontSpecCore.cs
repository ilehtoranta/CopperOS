/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using Amiga;

namespace CopperOS.MuiMaster;

// Fixed-width result of parsing a MorphOS MUI font-specification string. The
// family is a span into the caller-owned guest string; no managed substring is
// created. Size/color presence is carried separately from the values so a
// later native font capability can apply the exact requested overrides.
public struct MuiCustomFontSpec
{
	public APTR Source;
	public APTR Family;
	public uint FamilyLength;
	public int Size;
	public uint SizeMode;
	public uint StyleFlags;
	public uint TextColor;
	public uint OutlineColor;
	public uint ValueFlags;
}

public static class MuiCustomFontSpecFlags
{
	public const uint HasSize = 1u;
	public const uint HasTextColor = 2u;
	public const uint HasOutlineColor = 4u;

	public const uint SizeAbsolute = 1u;
	public const uint SizeRelative = 2u;

	public const uint Shadow = 1u;
	public const uint Outline = 2u;
	public const uint Glow = 4u;
	public const uint Underline = 8u;
	public const uint Bold = 16u;
	public const uint Italic = 32u;
	public const uint ResetStyles = 64u;
}

internal static class MuiCustomFontSpecCore
{
	private const uint MaximumLength = MuiAreaCustomFontCore.MaximumSpecLength;

	internal static bool TryParse<TPlatform>(ref TPlatform platform, APTR source,
		out MuiCustomFontSpec value)
		where TPlatform : struct, IMuiGuestMemory
	{
		value = default;
		if (source.IsNull || !CStringCodec.TryReadLength(ref platform, source,
			MaximumLength, out var length)) return false;
		value.Source = source;
		value.Family = source;
		value.FamilyLength = length;
		var slash = FindByte(ref platform, source, length, (byte)'/');
		if (slash != uint.MaxValue)
		{
			value.FamilyLength = slash;
			var segmentStart = slash + 1;
			while (segmentStart <= length)
			{
				var segmentEnd = segmentStart;
				while (segmentEnd < length && platform.ReadUInt8(source,
					unchecked((int)segmentEnd)) != (byte)'/') segmentEnd++;
				if (segmentEnd != segmentStart && !ParseSegment(ref platform, source,
					segmentStart, segmentEnd - segmentStart, ref value)) return false;
				if (segmentEnd >= length) break;
				segmentStart = segmentEnd + 1;
			}
		}
		return true;
	}

	private static bool ParseSegment<TPlatform>(ref TPlatform platform,
		APTR source, uint start, uint length, ref MuiCustomFontSpec value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var first = platform.ReadUInt8(source, unchecked((int)start));
		if (first == (byte)'+' || first == (byte)'-')
			return ParseSize(ref platform, source, start, length, true, ref value);
		if (first >= (byte)'0' && first <= (byte)'9')
			return ParseSize(ref platform, source, start, length, false, ref value);
		if (first == (byte)'c' || first == (byte)'C')
			return ParseColor(ref platform, source, start, length, first == (byte)'C',
				ref value);
		if (length != 1) return false;
		var style = first switch
		{
			(byte)'s' => MuiCustomFontSpecFlags.Shadow,
			(byte)'o' => MuiCustomFontSpecFlags.Outline,
			(byte)'g' => MuiCustomFontSpecFlags.Glow,
			(byte)'u' => MuiCustomFontSpecFlags.Underline,
			(byte)'b' => MuiCustomFontSpecFlags.Bold,
			(byte)'i' => MuiCustomFontSpecFlags.Italic,
			(byte)'n' => MuiCustomFontSpecFlags.ResetStyles,
			_ => 0u,
		};
		if (style == 0) return false;
		if (style == MuiCustomFontSpecFlags.ResetStyles)
			value.StyleFlags = style;
		else
			value.StyleFlags |= style;
		return true;
	}

	private static bool ParseSize<TPlatform>(ref TPlatform platform, APTR source,
		uint start, uint length, bool relative, ref MuiCustomFontSpec value)
		where TPlatform : struct, IMuiGuestMemory
	{
		var position = relative ? start + 1 : start;
		if (position >= start + length) return false;
		uint number = 0;
		for (; position < start + length; position++)
		{
			var ch = platform.ReadUInt8(source, unchecked((int)position));
			if (ch < (byte)'0' || ch > (byte)'9') return false;
			var digit = unchecked((uint)(ch - (byte)'0'));
			if (number > (0x7FFFFFFFu - digit) / 10u) return false;
			number = number * 10 + digit;
		}
		var negative = relative && platform.ReadUInt8(source,
			unchecked((int)start)) == (byte)'-';
		if (negative)
		{
			if (number > 0x80000000u) return false;
			value.Size = number == 0x80000000u ? int.MinValue :
				-unchecked((int)number);
		}
		else value.Size = unchecked((int)number);
		value.SizeMode = relative ? MuiCustomFontSpecFlags.SizeRelative :
			MuiCustomFontSpecFlags.SizeAbsolute;
		value.ValueFlags |= MuiCustomFontSpecFlags.HasSize;
		return true;
	}

	private static bool ParseColor<TPlatform>(ref TPlatform platform, APTR source,
		uint start, uint length, bool outline, ref MuiCustomFontSpec value)
		where TPlatform : struct, IMuiGuestMemory
	{
		if (length != 7) return false;
		uint color = 0;
		for (var index = 1; index < 7; index++)
		{
			var nibble = HexNibble(platform.ReadUInt8(source,
				unchecked((int)(start + unchecked((uint)index)))));
			if (nibble < 0) return false;
			color = unchecked((color << 4) | (uint)nibble);
		}
		if (outline)
		{
			value.OutlineColor = color;
			value.ValueFlags |= MuiCustomFontSpecFlags.HasOutlineColor;
		}
		else
		{
			value.TextColor = color;
			value.ValueFlags |= MuiCustomFontSpecFlags.HasTextColor;
		}
		return true;
	}

	private static uint FindByte<TPlatform>(ref TPlatform platform, APTR source,
		uint length, byte target) where TPlatform : struct, IMuiGuestMemory
	{
		for (var index = 0u; index < length; index++)
			if (platform.ReadUInt8(source, unchecked((int)index)) == target)
				return index;
		return uint.MaxValue;
	}

	private static int HexNibble(byte value)
	{
		if (value >= (byte)'0' && value <= (byte)'9') return value - (byte)'0';
		if (value >= (byte)'a' && value <= (byte)'f') return value - (byte)'a' + 10;
		if (value >= (byte)'A' && value <= (byte)'F') return value - (byte)'A' + 10;
		return -1;
	}
}
