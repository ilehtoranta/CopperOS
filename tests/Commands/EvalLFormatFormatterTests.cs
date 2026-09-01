using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class EvalLFormatFormatterTests
{
    [Theory]
    [InlineData("%n", 42L, "42")]
    [InlineData("x=%X4 o=%O", 26L, "x=1a4 o=32")]
    [InlineData("[%n] %% %z %", -1L, "[-1] % %z %")]
    [InlineData("", long.MinValue, "")]
    public void Formats_documented_conversions_and_literal_percent_behavior(
        string format, long value, string expected)
    {
        var memory = new TestMemory(256); var source = new APTR(8); var destination = new APTR(96);
        Encoding.Latin1.GetBytes(format).CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        Assert.True(EvalLFormatFormatter.TryFormat(ref memory, source,
            (uint)format.Length, value, destination, 128, out var count, out var status));
        Assert.Equal(EvalLFormatStatus.Success, status);
        Assert.Equal(expected, Encoding.Latin1.GetString(memory.Bytes, (int)destination.Raw, (int)count));
    }

    [Fact]
    public void Rejects_character_format_short_and_overlap_before_writing()
    {
        var memory = new TestMemory(256); var source = new APTR(8); var destination = new APTR(96);
        memory.Bytes.AsSpan((int)destination.Raw, 32).Fill(0xa5);
        Encoding.Latin1.GetBytes("%c").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        Assert.False(EvalLFormatFormatter.TryFormat(ref memory, source, 2, 65,
            destination, 32, out _, out var characterStatus));
        Assert.Equal(EvalLFormatStatus.UnsupportedConversion, characterStatus);
        Encoding.Latin1.GetBytes("%x").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        Assert.False(EvalLFormatFormatter.TryFormat(ref memory, source, 2, 0x1234,
            destination, 3, out _, out var shortStatus));
        Assert.Equal(EvalLFormatStatus.InsufficientCapacity, shortStatus);
        Assert.False(EvalLFormatFormatter.TryFormat(ref memory, source, 2, 1,
            new APTR(9), 32, out _, out var overlapStatus));
        Assert.Equal(EvalLFormatStatus.InvalidSpan, overlapStatus);
        Assert.All(memory.Bytes.AsSpan((int)destination.Raw, 32).ToArray(), value => Assert.Equal((byte)0xa5, value));
    }

    [Theory]
    [InlineData("%n", long.MinValue, "-9223372036854775808")]
    [InlineData("%x", long.MinValue, "8000000000000000")]
    [InlineData("%o", -1L, "1777777777777777777777")]
    public void Sizes_full_width_conversions_to_the_exact_output_capacity(
        string format, long value, string expected)
    {
        var memory = new TestMemory(256); var source = new APTR(8); var destination = new APTR(96);
        Encoding.Latin1.GetBytes(format).CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        Assert.True(EvalLFormatFormatter.TryFormat(ref memory, source,
            (uint)format.Length, value, destination, (uint)expected.Length,
            out var count, out var status));
        Assert.Equal(EvalLFormatStatus.Success, status);
        Assert.Equal(expected, Encoding.Latin1.GetString(memory.Bytes,
            (int)destination.Raw, (int)count));
    }

    private struct TestMemory(int size) : IAmigaGuestMemory
    {
        public byte[] Bytes { get; } = new byte[size];
        public bool IsMapped(APTR a, uint n) => a.Raw <= (uint)Bytes.Length && n <= (uint)Bytes.Length - a.Raw;
        public byte ReadUInt8(APTR a, int o = 0) => Bytes[checked((int)(a.Raw + (uint)o))];
        public void WriteUInt8(APTR a, int o, byte v) => Bytes[checked((int)(a.Raw + (uint)o))] = v;
        public ushort ReadUInt16(APTR a, int o = 0) => throw new NotSupportedException();
        public uint ReadUInt32(APTR a, int o = 0) => throw new NotSupportedException();
        public void WriteUInt16(APTR a, int o, ushort v) => throw new NotSupportedException();
        public void WriteUInt32(APTR a, int o, uint v) => throw new NotSupportedException();
        public void Clear(APTR a, uint n) => throw new NotSupportedException();
        public void Copy(APTR s, APTR d, uint n) => throw new NotSupportedException();
    }
}
