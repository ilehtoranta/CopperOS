using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class QuoteBase64FormatterTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("f", "Zg==")]
    [InlineData("fo", "Zm8=")]
    [InlineData("foo", "Zm9v")]
    [InlineData("Work:Pic #1.jpg", "V29yazpQaWMgIzEuanBn")]
    public void Encodes_padded_standard_base64(string input, string expected)
    {
        var memory = new TestMemory(160); var source = new APTR(8); var destination = new APTR(80);
        var bytes = Encoding.Latin1.GetBytes(input); bytes.CopyTo(memory.Bytes.AsSpan(8));
        Assert.True(QuoteBase64Formatter.TryEncode(ref memory, source, (uint)bytes.Length, destination, 64, out var written));
        Assert.Equal(expected, Encoding.Latin1.GetString(memory.Bytes, 80, (int)written));
    }

    [Fact]
    public void Rejects_overlap_and_short_destination_without_writes()
    {
        var memory = new TestMemory(160); Encoding.Latin1.GetBytes("foo").CopyTo(memory.Bytes.AsSpan(8)); memory.Bytes.AsSpan(80, 8).Fill(0xa5);
        Assert.False(QuoteBase64Formatter.TryEncode(ref memory, new APTR(8), 3, new APTR(8), 64, out _));
        Assert.False(QuoteBase64Formatter.TryEncode(ref memory, new APTR(8), 3, new APTR(80), 3, out _));
        Assert.All(memory.Bytes.AsSpan(80, 8).ToArray(), value => Assert.Equal((byte)0xa5, value));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("Zg==", "f")]
    [InlineData("Zm8=", "fo")]
    [InlineData("Zm9v", "foo")]
    [InlineData("V29yazpQaWMgIzEuanBn", "Work:Pic #1.jpg")]
    public void Decodes_canonical_standard_base64(string input, string expected)
    {
        var memory = new TestMemory(160); var source = new APTR(8); var destination = new APTR(80);
        var bytes = Encoding.Latin1.GetBytes(input); bytes.CopyTo(memory.Bytes.AsSpan(8));
        Assert.True(QuoteBase64Formatter.TryDecode(ref memory, source, (uint)bytes.Length, destination, 64, out var written));
        Assert.Equal(expected, Encoding.Latin1.GetString(memory.Bytes, 80, (int)written));
    }

    [Theory]
    [InlineData("Zg=")]
    [InlineData("Zg=A")]
    [InlineData("Zh==")]
    [InlineData("Zm9=")]
    [InlineData("Zm9*")]
    public void Rejects_malformed_base64_before_writing(string input)
    {
        var memory = new TestMemory(160); Encoding.Latin1.GetBytes(input).CopyTo(memory.Bytes.AsSpan(8)); memory.Bytes.AsSpan(80, 16).Fill(0xa5);
        Assert.False(QuoteBase64Formatter.TryDecode(ref memory, new APTR(8), (uint)input.Length, new APTR(80), 16, out _));
        Assert.All(memory.Bytes.AsSpan(80, 16).ToArray(), value => Assert.Equal((byte)0xa5, value));
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
