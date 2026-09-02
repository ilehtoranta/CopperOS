using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class TypeTextFormatterTests
{
    [Theory]
    [InlineData("", false, false, "\n")]
    [InlineData("abc", false, false, "abc\n")]
    [InlineData("abc\n", false, false, "abc\n")]
    [InlineData("abc", false, true, "abc")]
    [InlineData("", false, true, "")]
    [InlineData("a\nb", true, false, "    1 a\n    2 b\n")]
    [InlineData("a\n", true, false, "    1 a\n")]
    [InlineData("", true, false, "    1 \n")]
    [InlineData("", true, true, "    1 ")]
    public void Formats_observed_text_number_and_noline_cases(string input,
        bool number, bool noLine, string expected)
    {
        var memory = new TestMemory(256);
        var source = new APTR(8);
        var destination = new APTR(96);
        var bytes = Encoding.Latin1.GetBytes(input);
        bytes.CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        Assert.True(TypeTextFormatter.TryFormat(ref memory, source,
            (uint)bytes.Length, number, noLine, destination, 128, out var written));
        Assert.Equal(expected, Encoding.Latin1.GetString(memory.Bytes,
            (int)destination.Raw, (int)written));
    }

    [Fact]
    public void Rejects_overlap_unmapped_and_short_destination_without_writes()
    {
        var memory = new TestMemory(128);
        var source = new APTR(8);
        Encoding.Latin1.GetBytes("a\nb").CopyTo(memory.Bytes.AsSpan(8));
        memory.Bytes.AsSpan(64, 24).Fill(0xa5);

        Assert.False(TypeTextFormatter.TryFormat(ref memory, source, 3, true,
            false, source, 64, out _));
        Assert.False(TypeTextFormatter.TryFormat(ref memory, source, 3, true,
            false, new APTR(64), 8, out _));
        Assert.False(TypeTextFormatter.TryFormat(ref memory, new APTR(126), 3,
            false, false, new APTR(64), 24, out _));
        Assert.All(memory.Bytes.AsSpan(64, 24).ToArray(), value =>
            Assert.Equal((byte)0xa5, value));
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

