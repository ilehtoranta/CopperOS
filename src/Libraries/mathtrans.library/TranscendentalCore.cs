/* Copyright (C) 2026 Ilkka Lehtoranta. SPDX-License-Identifier: MIT */
namespace CopperOS.MathTrans;

/// <summary>Basic FFP arithmetic, supplied by mathffp.library on the target.</summary>
public interface IFfpArithmetic
{
    uint Add(uint a, uint b);
    uint Sub(uint a, uint b);
    uint Mul(uint a, uint b);
    uint Div(uint a, uint b);
    uint Float(int value);
    int Fix(uint value);
}

/// <summary>FFP transcendental algorithms. Inputs and outputs are raw FFP words.</summary>
public static class TranscendentalCore
{
    public const uint One = 0x80000041, Two = 0x80000042, Half = 0x80000040;
    public const uint Pi = 0xc90fdb42, HalfPi = 0xc90fdb41, QuarterPi = 0xc90fdb40;
    private const uint TwoPi = 0xc90fdb43, Ln2 = 0xb1721840, InvLn10 = 0xde5bd93f;
    private const uint Max = 0xffffff7f;
    private static uint Neg(uint x) => x == 0 ? 0 : x ^ 0x80;
    private static uint Abs(uint x) => x & ~0x80u;
    private static bool Greater(uint a, uint b) => (a & 0x7f) != (b & 0x7f) ? (a & 0x7f) > (b & 0x7f) : (a >> 8) > (b >> 8);
    private static uint Scale(uint a, int power)
    {
        if (a == 0) return 0;
        int e = (int)(a & 0x7f) + power;
        if (e <= 0) return 0;
        if (e > 127) return Max | (a & 0x80);
        return (a & ~0x7fu) | (uint)e;
    }
    private static uint Reduce<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    {
        uint a = Abs(x);
        if (Greater(a, TwoPi))
        {
            // The representable input range is bounded before entry. Split
            // 2*pi into binary-exact pieces so a large multiple does not lose
            // the low bits through a single rounded FFP multiplication.
            int quotient = p.Fix(p.Div(a, TwoPi));
            uint q = p.Float(quotient);
            a = p.Sub(a, p.Mul(q, 0xc8000043)); // 6.25
            a = p.Sub(a, p.Mul(q, 0x8000003c)); // 1/32
            a = p.Sub(a, p.Mul(q, 0x80000038)); // 1/512
            a = p.Sub(a, p.Mul(q, 0x95777ab1)); // remaining signed part
        }
        if ((a & 0x80) == 0 && Greater(a, Pi)) a = p.Sub(a, TwoPi);
        else if ((a & 0x80) != 0 && Greater(Abs(a), Pi)) a = p.Add(a, TwoPi);
        return (x & 0x80) != 0 ? Neg(a) : a;
    }
    public static uint Sin<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    {
        x = Reduce(ref p, x);
        uint xx = p.Mul(x, x), term = x, sum = x;
        for (int n = 1; n <= 8; n++)
        { term = Neg(p.Div(p.Mul(term, xx), p.Float((2*n)*(2*n+1)))); sum = p.Add(sum, term); }
        return sum;
    }
    public static uint Cos<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    {
        x = Reduce(ref p, x);
        uint xx = p.Mul(x, x), term = One, sum = One;
        for (int n = 1; n <= 8; n++)
        { term = Neg(p.Div(p.Mul(term, xx), p.Float((2*n-1)*(2*n)))); sum = p.Add(sum, term); }
        return sum;
    }
    public static uint Tan<T>(ref T p, uint x) where T : struct, IFfpArithmetic => p.Div(Sin(ref p, x), Cos(ref p, x));
    public static uint Atan<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    {
        bool negative = (x & 0x80) != 0; x = Abs(x);
        bool inverse = Greater(x, One);
        if (inverse) x = p.Div(One, x);
        bool shifted = Greater(x, 0xd413cd3f); // tan(pi/8)
        if (shifted) x = p.Div(p.Sub(x, One), p.Add(x, One));
        uint xx = p.Mul(x, x), power = x, sum = x;
        for (int n = 1; n <= 12; n++)
        { power = Neg(p.Mul(power, xx)); sum = p.Add(sum, p.Div(power, p.Float(2*n+1))); }
        if (shifted) sum = p.Add(QuarterPi, sum);
        if (inverse) sum = p.Sub(HalfPi, sum);
        return negative ? Neg(sum) : sum;
    }
    public static uint Sqrt<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    {
        x = Abs(x);
        if (x == 0) return 0;
        int exponent = (int)(x & 0x7f) - 65;
        int half = exponent >= 0 ? exponent / 2 : (exponent - 1) / 2;
        uint estimate = Scale(One, half);
        for (int i = 0; i < 7; i++) estimate = p.Mul(Half, p.Add(estimate, p.Div(x, estimate)));
        return estimate;
    }
    public static uint Exp<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    {
        if (Greater(Abs(x), 0xb1721846)) return (x & 0x80) != 0 ? 0u : Max; // |x| > 64 ln(2)
        int n = p.Fix(p.Div(x, Ln2));
        uint r = p.Sub(x, p.Mul(p.Float(n), Ln2)), sum = One, term = One;
        for (int i = 1; i <= 13; i++) { term = p.Div(p.Mul(term, r), p.Float(i)); sum = p.Add(sum, term); }
        return Scale(sum, n);
    }
    public static uint Log<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    {
        x = Abs(x);
        if (x == 0) return uint.MaxValue;
        int n = (int)(x & 0x7f) - 65;
        uint m = (x & ~0x7fu) | 65u;
        uint z = p.Div(p.Sub(m, One), p.Add(m, One)), zz = p.Mul(z,z), term = z, sum = z;
        for (int i = 3; i <= 25; i += 2) { term = p.Mul(term, zz); sum = p.Add(sum, p.Div(term, p.Float(i))); }
        return p.Add(p.Mul(Two, sum), p.Mul(p.Float(n), Ln2));
    }
    public static uint Log10<T>(ref T p, uint x) where T : struct, IFfpArithmetic => p.Mul(Log(ref p, x), InvLn10);
    public static uint Pow<T>(ref T p, uint x, uint exponent) where T : struct, IFfpArithmetic
    {
        if (exponent == 0) return One;
        if (x == 0) return (exponent & 0x80) != 0 ? Max : 0;
        x = Abs(x);
        return Exp(ref p, p.Mul(exponent, Log(ref p, x)));
    }
    public static uint Asin<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    {
        if (Greater(Abs(x), One)) return uint.MaxValue;
        if (Abs(x) == One) return (x & 0x80) != 0 ? Neg(HalfPi) : HalfPi;
        uint root = p.Mul(Sqrt(ref p, p.Sub(One, Abs(x))), Sqrt(ref p, p.Add(One, Abs(x))));
        return Atan(ref p, p.Div(x, root));
    }
    public static uint Acos<T>(ref T p, uint x) where T : struct, IFfpArithmetic => Greater(Abs(x), One) ? uint.MaxValue : p.Sub(HalfPi, Asin(ref p, x));
    public static uint Sinh<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    {
        if ((x & 0x7f) <= 50) return x;
        uint e = Exp(ref p, x);
        if ((x & 0x80) == 0 && e == Max) return Max;
        // The reference divides by exp(x), including when that has underflowed
        // to zero. On a real 68000 mathffp raises a divide-by-zero trap, then
        // produces its overflow result if the task's trap handler resumes it.
        uint reciprocal = p.Div(One, e);
        return Scale(p.Sub(e, reciprocal), -1);
    }
    public static uint Cosh<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    {
        uint e = Exp(ref p, Abs(x));
        if (e == Max) return Max;
        return Scale(p.Add(e, p.Div(One, e)), -1);
    }
    public static uint Tanh<T>(ref T p, uint x) where T : struct, IFfpArithmetic
    { if ((x & 0x7f) <= 50) return x;
      if (Greater(Abs(x), 0xa0000044)) return (x & 0x80) != 0 ? Neg(One) : One;
      uint e = Exp(ref p, p.Mul(Two, Abs(x))); uint result = p.Div(p.Sub(e, One), p.Add(e, One)); return (x & 0x80) != 0 ? Neg(result) : result; }
}
