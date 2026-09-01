using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class QuoteUriFormatterTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("AZaz09-._~", "AZaz09-._~")]
    [InlineData("Work:Pic #1.jpg", "Work%3APic%20%231.jpg")]
    public void Encodes_uri_component_bytes(string input, string expected)
    {
        var memory = new TestMemory(160);
        var source = new APTR(8);
        var destination = new APTR(80);
        var bytes = Encoding.Latin1.GetBytes(input);
        bytes.CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        var success = QuoteUriFormatter.TryEncode(ref memory, source,
            (uint)bytes.Length, destination, 64, out var written);

        Assert.True(success);
        Assert.Equal(expected, Encoding.Latin1.GetString(memory.Bytes,
            (int)destination.Raw, (int)written));
    }

    [Fact]
    public void Encodes_high_and_control_bytes_and_rejects_short_output()
    {
        var memory = new TestMemory(160);
        var source = new APTR(8);
        var destination = new APTR(80);
        memory.Bytes[8] = 0;
        memory.Bytes[9] = 0xff;
        memory.Bytes.AsSpan(80, 16).Fill(0xa5);

        Assert.True(QuoteUriFormatter.TryEncode(ref memory, source, 2,
            destination, 6, out var written));
        Assert.Equal("%00%FF", Encoding.Latin1.GetString(memory.Bytes, 80,
            (int)written));
        memory.Bytes.AsSpan(120, 8).Fill(0xa5);
        Assert.False(QuoteUriFormatter.TryEncode(ref memory, source, 2,
            new APTR(120), 5, out _));
        Assert.All(memory.Bytes.AsSpan(120, 8).ToArray(), value =>
            Assert.Equal((byte)0xa5, value));
    }

    [Fact]
    public void Decodes_percent_escapes_and_preserves_plus()
    {
        var memory = new TestMemory(256); var source = new APTR(8); var destination = new APTR(96);
        const string encoded = "Work%3aPic%20%231.jpg+%00%fF";
        Encoding.Latin1.GetBytes(encoded).CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        Assert.True(QuoteUriFormatter.TryDecode(ref memory, source, (uint)encoded.Length, destination, 64, out var count));
        Assert.Equal(new byte[] { (byte)'W',(byte)'o',(byte)'r',(byte)'k',(byte)':',(byte)'P',(byte)'i',(byte)'c',(byte)' ',(byte)'#',(byte)'1',(byte)'.',(byte)'j',(byte)'p',(byte)'g',(byte)'+',0,255 }, memory.Bytes.AsSpan((int)destination.Raw, (int)count).ToArray());
    }

    [Fact]
    public void Rejects_malformed_short_and_overlapping_percent_input_before_writing()
    {
        var memory = new TestMemory(256); var source = new APTR(8); var destination = new APTR(96);
        memory.Bytes.AsSpan((int)destination.Raw, 32).Fill(0xa5);
        Encoding.Latin1.GetBytes("%").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        Assert.False(QuoteUriFormatter.TryDecode(ref memory, source, 1, destination, 32, out _));
        Encoding.Latin1.GetBytes("%0G").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        Assert.False(QuoteUriFormatter.TryDecode(ref memory, source, 3, destination, 32, out _));
        Encoding.Latin1.GetBytes("%41").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        Assert.False(QuoteUriFormatter.TryDecode(ref memory, source, 3, destination, 0, out _));
        Assert.False(QuoteUriFormatter.TryDecode(ref memory, source, 3, new APTR(10), 32, out _));
        Assert.All(memory.Bytes.AsSpan((int)destination.Raw, 32).ToArray(), value => Assert.Equal((byte)0xa5, value));
    }

    private struct TestMemory(int size) : IAmigaGuestMemory
    {
        public byte[] Bytes { get; } = new byte[size];
        public bool IsMapped(APTR address, uint length) =>
            address.Raw <= (uint)Bytes.Length && length <= (uint)Bytes.Length - address.Raw;
        public byte ReadUInt8(APTR address, int offset = 0) => Bytes[checked((int)(address.Raw + (uint)offset))];
        public void WriteUInt8(APTR address, int offset, byte value) => Bytes[checked((int)(address.Raw + (uint)offset))] = value;
        public ushort ReadUInt16(APTR address, int offset = 0) => throw new NotSupportedException();
        public uint ReadUInt32(APTR address, int offset = 0) => throw new NotSupportedException();
        public void WriteUInt16(APTR address, int offset, ushort value) => throw new NotSupportedException();
        public void WriteUInt32(APTR address, int offset, uint value) => throw new NotSupportedException();
        public void Clear(APTR address, uint byteCount) => throw new NotSupportedException();
        public void Copy(APTR source, APTR destination, uint byteCount) => throw new NotSupportedException();
    }
}
