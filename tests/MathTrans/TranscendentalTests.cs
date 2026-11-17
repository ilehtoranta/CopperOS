namespace CopperOS.MathTrans.Tests;

public class TranscendentalTests
{
    private struct Arithmetic : IFfpArithmetic
    {
        public uint Add(uint a, uint b) => Encode(Decode(a) + Decode(b));
        public uint Sub(uint a, uint b) => Encode(Decode(a) - Decode(b));
        public uint Mul(uint a, uint b) => Encode(Decode(a) * Decode(b));
        public uint Div(uint a, uint b) => Encode(Decode(a) / Decode(b));
        public uint Float(int n) => Encode(n);
        public int Fix(uint a) => (int)Decode(a);
    }
    private static double Decode(uint ffp)
    {
        Assert.True(FfpConversions.TryToIeee(ffp, out var bits));
        return BitConverter.UInt32BitsToSingle(bits);
    }
    private static uint Encode(double value)
    {
        if (Math.Abs(value) < Math.ScaleB(1, -64)) return 0;
        if (Math.Abs(value) >= Math.ScaleB(1, 63)) return 0xffffff7f | (value < 0 ? 0x80u : 0);
        uint bits = BitConverter.SingleToUInt32Bits((float)value);
        Assert.True(FfpConversions.TryFromIeee(bits, out uint result));
        return result;
    }
    private static void Near(double expected, uint ffp, double tolerance = 0.00002)
        => Assert.InRange(Math.Abs(Decode(ffp) - expected), 0, tolerance * Math.Max(1, Math.Abs(expected)));

    [Theory]
    [InlineData(-3.0)] [InlineData(-1.0)] [InlineData(-0.1)] [InlineData(0.0)]
    [InlineData(0.1)] [InlineData(1.0)] [InlineData(3.0)] [InlineData(10.0)]
    public void TrigonometricAndHyperbolicValues(double x)
    {
        var p = new Arithmetic(); uint a = Encode(x);
        Near(Math.Sin(x), TranscendentalCore.Sin(ref p, a));
        Near(Math.Cos(x), TranscendentalCore.Cos(ref p, a));
        Near(Math.Atan(x), TranscendentalCore.Atan(ref p, a));
        Near(Math.Tanh(x), TranscendentalCore.Tanh(ref p, a));
        Near(Math.Sinh(x), TranscendentalCore.Sinh(ref p, a));
        Near(Math.Cosh(x), TranscendentalCore.Cosh(ref p, a));
        Near(Math.Tan(x), TranscendentalCore.Tan(ref p, a));
    }
    [Theory]
    [InlineData(0.01)] [InlineData(0.5)] [InlineData(1.0)] [InlineData(2.0)] [InlineData(100.0)]
    public void LogExpSqrtAndPower(double x)
    {
        var p = new Arithmetic(); uint a = Encode(x);
        Near(Math.Log(x), TranscendentalCore.Log(ref p, a));
        Near(Math.Log10(x), TranscendentalCore.Log10(ref p, a));
        Near(Math.Sqrt(x), TranscendentalCore.Sqrt(ref p, a));
        Near(Math.Pow(x, 1.5), TranscendentalCore.Pow(ref p, a, Encode(1.5)));
        Near(Math.Exp(Math.Log(x)), TranscendentalCore.Exp(ref p, Encode(Math.Log(x))));
    }
    [Theory]
    [InlineData(-1.0)] [InlineData(-0.5)] [InlineData(0.0)] [InlineData(0.5)] [InlineData(1.0)]
    public void InverseTrigonometricValues(double x)
    {
        var p = new Arithmetic(); uint a = Encode(x);
        Near(Math.Asin(x), TranscendentalCore.Asin(ref p, a));
        Near(Math.Acos(x), TranscendentalCore.Acos(ref p, a));
    }
    [Fact]
    public void SampledOrdinaryInputsAndSmallHyperbolicInputs()
    {
        var p = new Arithmetic();
        for (int i=-200;i<=200;i++)
        {
            double x=i/20.0; uint a=Encode(x);
            Near(Math.Sin(x),TranscendentalCore.Sin(ref p,a),0.00004);
            Near(Math.Cos(x),TranscendentalCore.Cos(ref p,a),0.00004);
            Near(Math.Atan(x),TranscendentalCore.Atan(ref p,a),0.00004);
            Near(Math.Tanh(x),TranscendentalCore.Tanh(ref p,a),0.00004);
        }
        foreach (double x in new[] { 1e-8, -1e-8, 1e-5, -1e-5 })
        {
            uint a=Encode(x);
            Assert.InRange(Math.Abs(Decode(TranscendentalCore.Sinh(ref p,a))-Math.Sinh(x)),0,1e-10);
            Assert.InRange(Math.Abs(Decode(TranscendentalCore.Tanh(ref p,a))-Math.Tanh(x)),0,1e-10);
        }
    }
}
