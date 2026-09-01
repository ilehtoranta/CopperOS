using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class QuoteReadItemFormatterTests
{
    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("A=B", "\"A=B\"")]
    [InlineData("two words", "\"two words\"")]
    [InlineData("", "\"\"")]
    [InlineData("a*b", "\"a**b\"")]
    [InlineData("a\"b", "\"a*\"b\"")]
    [InlineData("a\nb", "\"a*Nb\"")]
    public void Encodes_one_ReadItem_value(string input, string expected)
    {
        var memory = new TestMemory(128);
        var source = new APTR(8);
        var destination = new APTR(64);
        var bytes = Encoding.Latin1.GetBytes(input);
        bytes.CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        var success = QuoteReadItemFormatter.TryEncode(ref memory, source,
            (uint)bytes.Length, destination, 48, out var written);

        Assert.True(success);
        Assert.Equal(expected, Encoding.Latin1.GetString(memory.Bytes,
            (int)destination.Raw, (int)written));
    }

    [Fact]
    public void Rejects_overlapping_or_short_destinations_without_writes()
    {
        var memory = new TestMemory(128);
        var source = new APTR(8);
        Encoding.Latin1.GetBytes("A=B").CopyTo(memory.Bytes.AsSpan(8));
        memory.Bytes.AsSpan(32, 20).Fill(0xa5);

        Assert.False(QuoteReadItemFormatter.TryEncode(ref memory, source, 3,
            source, 32, out _));
        Assert.False(QuoteReadItemFormatter.TryEncode(ref memory, source, 3,
            new APTR(32), 4, out _));
        Assert.All(memory.Bytes.AsSpan(32, 20).ToArray(), value =>
            Assert.Equal((byte)0xa5, value));
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
        public ushort ReadUInt16(APTR address, int offset = 0) =>
            throw new NotSupportedException();
        public uint ReadUInt32(APTR address, int offset = 0) =>
            throw new NotSupportedException();
        public void WriteUInt16(APTR address, int offset, ushort value) =>
            throw new NotSupportedException();
        public void WriteUInt32(APTR address, int offset, uint value) =>
            throw new NotSupportedException();
        public void Clear(APTR address, uint byteCount) =>
            throw new NotSupportedException();
        public void Copy(APTR source, APTR destination, uint byteCount) =>
            throw new NotSupportedException();
    }
}
