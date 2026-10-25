/*
- Copyright (C) 2026 Ilkka Lehtoranta
- SPDX-License-Identifier: MIT
*/

namespace CopperOS.MuiMaster;

// Exact floor(offset * outputSpan / inputSpan), with offset <= inputSpan.
// Keep the product as a quotient/remainder pair so full LONG ranges need
// neither an overflowing ULONG product nor 64-bit runtime arithmetic on 68000.
internal static class MuiNumericScalingCore
{
	internal static uint ScaleOffset(uint offset, uint inputSpan, uint outputSpan)
	{
		if (inputSpan == 0 || offset == 0 || outputSpan == 0) return 0;
		if (offset >= inputSpan) return outputSpan;
		var quotient = 0u;
		var remainder = 0u;
		for (var bit = 0x80000000u; bit != 0; bit >>= 1)
		{
			quotient <<= 1;
			// Double the remainder modulo inputSpan without overflowing it.
			if (remainder >= inputSpan - remainder)
			{
				remainder -= inputSpan - remainder;
				quotient++;
			}
			else remainder += remainder;
			if ((outputSpan & bit) == 0) continue;
			if (remainder >= inputSpan - offset)
			{
				remainder -= inputSpan - offset;
				quotient++;
			}
			else remainder += offset;
		}
		return quotient;
	}
}
