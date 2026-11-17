using CopperOS.MathTrans;

namespace CopperOS.MathTrans.Tests;

public class FfpConversionsTests
{
    [Theory]
    [InlineData(0x00000000u, 0x00000000u)]
    [InlineData(0x80000041u, 0x3f800000u)]
    [InlineData(0x800000c1u, 0xbf800000u)]
    [InlineData(0xc0000042u, 0x40400000u)]
    [InlineData(0x80000001u, 0x1f800000u)]
    [InlineData(0xffffff7fu, 0x5effffffu)]
    public void KnownBitPatterns(uint ffp, uint ieee)
    {
        Assert.True(FfpConversions.TryToIeee(ffp, out uint actualIeee));
        Assert.Equal(ieee, actualIeee);
        Assert.True(FfpConversions.TryFromIeee(ieee, out uint actualFfp));
        Assert.Equal(ffp, actualFfp);
    }

    [Fact]
    public void NormalizedValuesMatchIndependentNumericInterpretation()
    {
        uint[] fractions = [0, 1, 0x123456, 0x7fffff];
        for (uint exponent = 1; exponent <= 127; exponent++)
        foreach (uint fraction in fractions)
        foreach (uint sign in new uint[] { 0, 0x80 })
        {
            uint mantissa = 0x800000 | fraction;
            uint ffp = (mantissa << 8) | sign | exponent;
            double expected = Math.ScaleB((double)mantissa, (int)exponent - 88);
            if (sign != 0) expected = -expected;
            Assert.True(FfpConversions.TryToIeee(ffp, out uint bits));
            Assert.Equal(expected, (double)BitConverter.UInt32BitsToSingle(bits));
            Assert.True(FfpConversions.TryFromIeee(bits, out uint roundTrip));
            Assert.Equal(ffp, roundTrip);
        }
    }

    [Theory]
    [InlineData(0x00000001u)]
    [InlineData(0x80000000u)]
    [InlineData(0x7fffff41u)]
    public void RejectsNoncanonicalFfp(uint value) =>
        Assert.False(FfpConversions.TryToIeee(value, out _));

    [Theory]
    [InlineData(0x00000001u)]
    [InlineData(0x1f7fffffu)]
    [InlineData(0x5f000000u)]
    [InlineData(0x7f800000u)]
    [InlineData(0x7fc00000u)]
    public void RejectsIeeeOutsideSupportedRange(uint value) =>
        Assert.False(FfpConversions.TryFromIeee(value, out _));

    [Fact]
    public void NegativeIeeeZeroConvertsToCanonicalFfpZero()
    {
        Assert.True(FfpConversions.TryFromIeee(0x80000000u, out uint result));
        Assert.Equal(0u, result);
    }
}
