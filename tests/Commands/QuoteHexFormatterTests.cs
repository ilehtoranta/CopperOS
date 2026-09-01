using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class QuoteHexFormatterTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("A", "41")]
    [InlineData("Work:Pic #1.jpg", "576f726b3a5069632023312e6a7067")]
    public void Encodes_lowercase_hexadecimal(string input, string expected)
    {
        var memory = new TestMemory(128);
        var source = new APTR(8);
        var destination = new APTR(64);
        var bytes = Encoding.Latin1.GetBytes(input);
        bytes.CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        var success = QuoteHexFormatter.TryEncode(ref memory, source,
            (uint)bytes.Length, destination, 48, out var written);

        Assert.True(success);
        Assert.Equal(expected, Encoding.Latin1.GetString(memory.Bytes,
            (int)destination.Raw, (int)written));
    }

    [Fact]
    public void Rejects_overlap_and_short_destination_without_writes()
    {
        var memory = new TestMemory(128);
        var source = new APTR(8);
        Encoding.Latin1.GetBytes("AB").CopyTo(memory.Bytes.AsSpan(8));
        memory.Bytes.AsSpan(32, 16).Fill(0xa5);

        Assert.False(QuoteHexFormatter.TryEncode(ref memory, source, 2,
            source, 32, out _));
        Assert.False(QuoteHexFormatter.TryEncode(ref memory, source, 2,
            new APTR(32), 3, out _));
        Assert.All(memory.Bytes.AsSpan(32, 16).ToArray(), value =>
            Assert.Equal((byte)0xa5, value));
    }

    [Fact]
    public void Decodes_published_hex_and_accepts_uppercase_digits()
    {
        var memory = new TestMemory(256); var source = new APTR(8); var destination = new APTR(96);
        const string encoded = "576F726B3A5069632023312E6A7067";
        Encoding.Latin1.GetBytes(encoded).CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        Assert.True(QuoteHexFormatter.TryDecode(ref memory, source, (uint)encoded.Length, destination, 64, out var count));
        Assert.Equal("Work:Pic #1.jpg", Encoding.Latin1.GetString(memory.Bytes, (int)destination.Raw, (int)count));
    }

    [Fact]
    public void Rejects_odd_malformed_short_and_overlapping_decode_inputs_before_writing()
    {
        var memory = new TestMemory(256); var source = new APTR(8); var destination = new APTR(96);
        memory.Bytes.AsSpan((int)destination.Raw, 32).Fill(0xa5);
        Encoding.Latin1.GetBytes("0").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        Assert.False(QuoteHexFormatter.TryDecode(ref memory, source, 1, destination, 32, out _));
        Encoding.Latin1.GetBytes("0G").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        Assert.False(QuoteHexFormatter.TryDecode(ref memory, source, 2, destination, 32, out _));
        Encoding.Latin1.GetBytes("4142").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        Assert.False(QuoteHexFormatter.TryDecode(ref memory, source, 4, destination, 1, out _));
        Assert.False(QuoteHexFormatter.TryDecode(ref memory, source, 4, new APTR(10), 32, out _));
        Assert.All(memory.Bytes.AsSpan((int)destination.Raw, 32).ToArray(), value => Assert.Equal((byte)0xa5, value));
    }

    private struct TestMemory(int size) : IAmigaGuestMemory
    {
        public byte[] Bytes { get; } = new byte[size];
        public bool IsMapped(APTR address, uint length) =>
            address.Raw <= (uint)Bytes.Length && length <= (uint)Bytes.Length - address.Raw;
        public byte ReadUInt8(APTR address, int offset = 0) =>
            Bytes[checked((int)(address.Raw + (uint)offset))];
        public void WriteUInt8(APTR address, int offset, byte value) =>
            Bytes[checked((int)(address.Raw + (uint)offset))] = value;
        public ushort ReadUInt16(APTR address, int offset = 0) => throw new NotSupportedException();
        public uint ReadUInt32(APTR address, int offset = 0) => throw new NotSupportedException();
        public void WriteUInt16(APTR address, int offset, ushort value) => throw new NotSupportedException();
        public void WriteUInt32(APTR address, int offset, uint value) => throw new NotSupportedException();
        public void Clear(APTR address, uint byteCount) => throw new NotSupportedException();
        public void Copy(APTR source, APTR destination, uint byteCount) => throw new NotSupportedException();
    }
}
