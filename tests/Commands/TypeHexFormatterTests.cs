using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class TypeHexFormatterTests
{
    [Fact]
    public void Formats_empty_input_without_a_row()
    {
        var actual = Format([]);
        Assert.Equal("", actual);
    }

    [Fact]
    public void Formats_partial_row_with_padding_printable_column_and_blank_line()
    {
        var actual = Format(Encoding.Latin1.GetBytes("ABC"));
        Assert.Equal("0000: 414243" + new string(' ', 30) + "ABC\n\n", actual);
    }

    [Fact]
    public void Formats_full_row_with_uppercase_hex_and_control_substitution()
    {
        var bytes = Enumerable.Range(0, 16).Select(value => (byte)value).ToArray();
        var actual = Format(bytes);
        Assert.Equal("0000: 00010203 04050607 08090A0B 0C0D0E0F " +
            new string('.', 16) + "\n", actual);
    }

    [Fact]
    public void Expands_offset_after_four_hex_digits()
    {
        var bytes = new byte[0x10001];
        bytes[^1] = (byte)'Z';
        var actual = Format(bytes);
        Assert.EndsWith("10000: 5A" + new string(' ', 34) + "Z\n\n", actual);
    }

    [Fact]
    public void Rejects_overlap_unmapped_and_short_destination_without_writes()
    {
        var memory = new TestMemory(256);
        var source = new APTR(8);
        memory.Bytes[8] = (byte)'A';
        memory.Bytes.AsSpan(96, 80).Fill(0xa5);

        Assert.False(TypeHexFormatter.TryFormat(ref memory, source, 1, source,
            80, out _));
        Assert.False(TypeHexFormatter.TryFormat(ref memory, source, 1,
            new APTR(96), 10, out _));
        Assert.False(TypeHexFormatter.TryFormat(ref memory, new APTR(255), 2,
            new APTR(96), 80, out _));
        Assert.All(memory.Bytes.AsSpan(96, 80).ToArray(), value =>
            Assert.Equal((byte)0xa5, value));
    }

    private static string Format(byte[] source)
    {
        var memory = new TestMemory(Math.Max(0x300, source.Length + 0x50000));
        var input = new APTR(8);
        var output = new APTR((uint)(source.Length + 32));
        source.CopyTo(memory.Bytes.AsSpan((int)input.Raw));
        Assert.True(TypeHexFormatter.TryFormat(ref memory, input, (uint)source.Length,
            output, (uint)(memory.Bytes.Length - output.Raw), out var written));
        return Encoding.Latin1.GetString(memory.Bytes, (int)output.Raw, (int)written);
    }

    private struct TestMemory(int size) : IAmigaGuestMemory
    {
        public byte[] Bytes { get; } = new byte[size];
        public bool IsMapped(APTR address, uint length) => address.Raw <= (uint)Bytes.Length &&
            length <= (uint)Bytes.Length - address.Raw;
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
