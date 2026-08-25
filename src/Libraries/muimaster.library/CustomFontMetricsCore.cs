/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

namespace CopperOS.MuiMaster;

// Deterministic integer projection used by headless/native fixture providers
// until a real rasterizer supplies exact metrics.  The projection deliberately
// has no floating point, exceptions, allocations, or managed font state.
public static class MuiCustomFontMetricsCore
{
	public static MuiCustomFontMetrics FromSpec(MuiCustomFontSpec spec)
	{
		var height = 8;
		if ((spec.ValueFlags & MuiCustomFontSpecFlags.HasSize) != 0)
		{
			if (spec.SizeMode == MuiCustomFontSpecFlags.SizeAbsolute)
				height = ClampPositive(spec.Size);
			else if (spec.SizeMode == MuiCustomFontSpecFlags.SizeRelative)
				height = SaturatingAdd(8, spec.Size);
		}

		var glyphWidth = height / 2;
		if (glyphWidth < 1) glyphWidth = 1;
		if ((spec.StyleFlags & MuiCustomFontSpecFlags.Bold) != 0 &&
			glyphWidth < 32767) glyphWidth++;
		MuiCustomFontMetrics result = default;
		result.GlyphWidth = glyphWidth;
		result.Height = height;
		return result;
	}

	private static int ClampPositive(int value)
	{
		if (value < 1) return 1;
		return value > 32767 ? 32767 : value;
	}

	private static int SaturatingAdd(int left, int right)
	{
		if (right > 0 && left > 32767 - right) return 32767;
		if (right < 0 && right < 1 - left) return 1;
		return ClampPositive(left + right);
	}
}
