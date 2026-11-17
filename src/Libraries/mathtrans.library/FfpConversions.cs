/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.MathTrans;

/// <summary>
/// Exact bit conversion for normalized values common to Motorola FFP and
/// IEEE single precision. Exceptional and noncanonical inputs are deliberately
/// rejected until their V40 behavior has been qualified.
/// </summary>
public static class FfpConversions
{
    public static bool TryToIeee(uint ffp, out uint ieee)
    {
        ieee = 0;
        if (ffp == 0) return true;

        uint exponent = ffp & 0x7f;
        uint mantissa = ffp >> 8;
        if (exponent == 0 || (mantissa & 0x800000) == 0) return false;

        ieee = ((ffp & 0x80) << 24) | ((exponent + 62) << 23) |
               (mantissa & 0x7fffff);
        return true;
    }

    public static bool TryFromIeee(uint ieee, out uint ffp)
    {
        ffp = 0;
        if ((ieee & 0x7fffffff) == 0) return true;

        uint exponent = (ieee >> 23) & 0xff;
        if (exponent < 63 || exponent > 189) return false;

        ffp = ((ieee & 0x7fffff) | 0x800000) << 8 |
              ((ieee >> 24) & 0x80) | (exponent - 62);
        return true;
    }
}
