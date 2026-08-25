/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

using System.Runtime.InteropServices;

namespace CopperOS.MuiMaster;

// MUIA_Group_*Spacing accepts either a pixel count or one of the signed
// special values MUIV_Group_Spacing_Default / MUIV_Group_Spacing_Percent(p).
// Keep both the caller value and the resolved value in a named record so
// consumers never reinterpret a guest LONG as an unsigned pixel offset.
internal enum MuiGroupSpacingKind : byte
{
	Pixels = 0,
	Default = 1,
	Percent = 2,
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupSpacingValue
{
	internal int Raw;
	internal int Payload;
	internal int Pixels;
	internal MuiGroupSpacingKind Kind;
}

[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct MuiGroupSpacingSelection
{
	internal int AvailableWidth;
	internal int AvailableHeight;
	internal MuiGroupSpacingValue Horizontal;
	internal MuiGroupSpacingValue Vertical;
}

internal static class MuiGroupSpacingCore
{
	internal const int DefaultValue = -100;
	private const int MaximumPixels = 10000;
	private const int MaximumPercent = 100;

	internal static MuiGroupSpacingValue Decode(uint raw)
	{
		var result = default(MuiGroupSpacingValue);
		result.Raw = unchecked((int)raw);
		if (result.Raw == DefaultValue)
		{
			result.Kind = MuiGroupSpacingKind.Default;
			return result;
		}
		if (result.Raw < 0)
		{
			var percent = result.Raw == int.MinValue ? int.MaxValue : -result.Raw;
			result.Kind = MuiGroupSpacingKind.Percent;
			result.Payload = ClampPercent(percent);
			return result;
		}
		result.Kind = MuiGroupSpacingKind.Pixels;
		result.Payload = ClampPixels(result.Raw);
		result.Pixels = result.Payload;
		return result;
	}

	// Preserve valid special inputs and bounded pixel values in the guest
	// policy record. Invalid negative values are retained as percentage inputs
	// and bounded when a layout pass resolves them.
	internal static uint NormalizeRaw(uint raw)
	{
		var value = Decode(raw);
		if (value.Kind != MuiGroupSpacingKind.Pixels)
			return raw;
		return unchecked((uint)value.Pixels);
	}

	// Min/max calculation has no parent extent from which to resolve a
	// percentage. Treat percentage/default spacing as zero here; the actual
	// layout pass resolves it against the supplied group rectangle.
	internal static int ResolveForMinMax(uint raw) => Decode(raw).Pixels;

	internal static MuiGroupSpacingValue ResolveForLayout(uint raw,
		int available)
	{
		var result = Decode(raw);
		if (result.Kind != MuiGroupSpacingKind.Percent) return result;
		var extent = available < 0 ? 0 : available;
		// Keep the freestanding path in bounded 32-bit arithmetic. Payload is
		// clamped to 0..100, so quotient*payload cannot overflow an int and the
		// remainder term is at most 9,900.
		var quotient = extent / 100;
		var remainder = extent % 100;
		result.Pixels = quotient * result.Payload +
			remainder * result.Payload / 100;
		return result;
	}

	internal static MuiGroupSpacingSelection ResolveSelection(uint horizontal,
		uint vertical, int width, int height)
	{
		var result = default(MuiGroupSpacingSelection);
		result.AvailableWidth = width < 0 ? 0 : width;
		result.AvailableHeight = height < 0 ? 0 : height;
		result.Horizontal = ResolveForLayout(horizontal, result.AvailableWidth);
		result.Vertical = ResolveForLayout(vertical, result.AvailableHeight);
		return result;
	}

	private static int ClampPixels(int value) => value < 0 ? 0 :
		value > MaximumPixels ? MaximumPixels : value;

	private static int ClampPercent(int value) => value < 0 ? 0 :
		value > MaximumPercent ? MaximumPercent : value;
}
