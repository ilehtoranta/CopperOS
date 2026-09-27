using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class EvalExpressionEvaluatorTests
{
    [Theory]
    [InlineData("1+2*3", 7L)]
    [InlineData("(1+2)*3", 9L)]
    [InlineData("2^3^2", 512L)]
    [InlineData("-2^2", -4L)]
    [InlineData("~0", -1L)]
    [InlineData("0x8000000000000000 | 1", -9223372036854775807L)]
    [InlineData("0xffffffffffffffff & 0x8000000000000000", long.MinValue)]
    [InlineData("~0x8000000000000000", long.MaxValue)]
    [InlineData("32 rsh 3", 4L)]
    [InlineData("1 lsh 4", 16L)]
    [InlineData("5 eqv 3", -7L)]
    [InlineData("7 xor 3", 4L)]
    [InlineData("0x8000000000000000 xor 1", -9223372036854775807L)]
    [InlineData("0x7fffffffffffffff / 3", 3074457345618258602L)]
    [InlineData("0x8000000000000000 / 1", long.MinValue)]
    [InlineData("0x8000000000000000 % 3", -2L)]
    [InlineData("-7 / 3", -2L)]
    [InlineData("-7 % 3", -1L)]
    [InlineData("-1 lsh 63", long.MinValue)]
    [InlineData("#x10 + 010", 24L)]
    [InlineData("08 + 1", 9L)]
    [InlineData("'A + 1", 66L)]
    [InlineData("0xffffffffffffffff", -1L)]
    [InlineData("9 M 4", 1L)]
    [InlineData("9 mo 4", 1L)]
    [InlineData("7 xo 3", 4L)]
    [InlineData("5 eq 3", -7L)]
    [InlineData("1 ls 4", 16L)]
    [InlineData("32 rs 3", 4L)]
    public void Evaluates_the_bounded_source_observed_integer_subset(string text,
        long expected)
    {
        var memory = new TestMemory(256); var source = new APTR(8);
        Encoding.Latin1.GetBytes(text).CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        Assert.True(EvalExpressionEvaluator.TryEvaluate(ref memory, source,
            (uint)text.Length, out var actual, out var status));
        Assert.Equal(EvalExpressionStatus.Success, status);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", EvalExpressionStatus.Malformed)]
    [InlineData("1 +", EvalExpressionStatus.Malformed)]
    [InlineData("1 MOD 2", EvalExpressionStatus.Malformed)]
    [InlineData("9 MO 4", EvalExpressionStatus.Malformed)]
    [InlineData("7 XO 3", EvalExpressionStatus.Malformed)]
    [InlineData("5 EQ 3", EvalExpressionStatus.Malformed)]
    [InlineData("1 LS 4", EvalExpressionStatus.Malformed)]
    [InlineData("32 RS 3", EvalExpressionStatus.Malformed)]
    [InlineData("1 / 0", EvalExpressionStatus.DivideByZero)]
    [InlineData("1 lsh 64", EvalExpressionStatus.InvalidShift)]
    [InlineData("9223372036854775807 + 1", EvalExpressionStatus.Overflow)]
    [InlineData("0x7fffffffffffffff * 2", EvalExpressionStatus.Overflow)]
    [InlineData("0x8000000000000000 / -1", EvalExpressionStatus.Overflow)]
    [InlineData("0x4000000000000000 lsh 2", EvalExpressionStatus.Overflow)]
    [InlineData("2 ^ 31", EvalExpressionStatus.Overflow)]
    [InlineData("0x", EvalExpressionStatus.Malformed)]
    public void Rejects_malformed_and_undefined_cases_with_explicit_status(
        string text, EvalExpressionStatus expected)
    {
        var memory = new TestMemory(256); var source = new APTR(8);
        Encoding.Latin1.GetBytes(text).CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        Assert.False(EvalExpressionEvaluator.TryEvaluate(ref memory, source,
            (uint)text.Length, out _, out var status));
        Assert.Equal(expected, status);
    }

    [Fact]
    public void Rejects_null_wrapping_and_unmapped_source_before_reading()
    {
        var memory = new TestMemory(64) { RejectMapping = true };
        Assert.False(EvalExpressionEvaluator.TryEvaluate(ref memory, APTR.Null,
            0, out _, out var nullStatus));
        Assert.Equal(EvalExpressionStatus.Malformed, nullStatus);
        Assert.False(EvalExpressionEvaluator.TryEvaluate(ref memory,
            new APTR(uint.MaxValue), 2, out _, out var wrappingStatus));
        Assert.Equal(EvalExpressionStatus.Malformed, wrappingStatus);
        Assert.False(EvalExpressionEvaluator.TryEvaluate(ref memory,
            new APTR(8), 1, out _, out var unmappedStatus));
        Assert.Equal(EvalExpressionStatus.Malformed, unmappedStatus);
    }

    private struct TestMemory(int size) : IAmigaGuestMemory
    {
        public byte[] Bytes { get; } = new byte[size];
        public bool RejectMapping { get; init; }
        public bool IsMapped(APTR address, uint length) => !RejectMapping &&
            address.Raw <= (uint)Bytes.Length && length <= (uint)Bytes.Length - address.Raw;
        public byte ReadUInt8(APTR address, int offset = 0) => Bytes[checked((int)(address.Raw + (uint)offset))];
        public void WriteUInt8(APTR address, int offset, byte value) => throw new NotSupportedException();
        public ushort ReadUInt16(APTR address, int offset = 0) => throw new NotSupportedException();
        public uint ReadUInt32(APTR address, int offset = 0) => throw new NotSupportedException();
        public void WriteUInt16(APTR address, int offset, ushort value) => throw new NotSupportedException();
        public void WriteUInt32(APTR address, int offset, uint value) => throw new NotSupportedException();
        public void Clear(APTR address, uint byteCount) => throw new NotSupportedException();
        public void Copy(APTR source, APTR destination, uint byteCount) => throw new NotSupportedException();
    }
}
