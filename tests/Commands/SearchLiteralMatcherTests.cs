using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class SearchLiteralMatcherTests
{
    [Fact]
    public void Finds_a_literal_at_each_supported_position()
    {
        AssertMatch("needle hay", "needle", true, true);
        AssertMatch("a needle b", "needle", true, true);
        AssertMatch("hay needle", "needle", true, true);
    }

    [Fact]
    public void Respects_case_mode_for_ascii_bytes()
    {
        AssertMatch("Alpha", "alpha", true, false);
        AssertMatch("Alpha", "alpha", false, true);
    }

    [Fact]
    public void Reports_a_valid_nonmatch_without_mutating_the_ranges()
    {
        var memory = new TestMemory(128);
        Write(memory.Bytes, 8, "alpha");
        Write(memory.Bytes, 40, "beta");
        var before = memory.Bytes.ToArray();

        Assert.True(SearchLiteralMatcher.TryContains(ref memory, new APTR(8), 5,
            new APTR(40), 4, true, out var found));
        Assert.False(found);
        Assert.Equal(before, memory.Bytes);
    }

    [Fact]
    public void Rejects_empty_unmapped_and_overflowing_ranges()
    {
        var memory = new TestMemory(64);
        Write(memory.Bytes, 8, "text");
        Write(memory.Bytes, 24, "x");

        Assert.False(SearchLiteralMatcher.TryContains(ref memory, new APTR(8), 4,
            new APTR(24), 0, true, out var emptyFound));
        Assert.False(emptyFound);
        Assert.False(SearchLiteralMatcher.TryContains(ref memory, new APTR(63), 2,
            new APTR(24), 1, true, out var unmappedFound));
        Assert.False(unmappedFound);
        Assert.False(SearchLiteralMatcher.TryContains(ref memory,
            new APTR(uint.MaxValue - 1), 3, new APTR(24), 1, true,
            out var overflowingFound));
        Assert.False(overflowingFound);
    }

    private static void AssertMatch(string text, string pattern, bool expected,
        bool caseSensitive)
    {
        var memory = new TestMemory(128);
        Write(memory.Bytes, 8, text);
        Write(memory.Bytes, 72, pattern);
        Assert.True(SearchLiteralMatcher.TryContains(ref memory, new APTR(8),
            (uint)text.Length, new APTR(72), (uint)pattern.Length,
            caseSensitive, out var found));
        Assert.Equal(expected, found);
    }

    private static void Write(byte[] destination, int offset, string value) =>
        Encoding.Latin1.GetBytes(value).CopyTo(destination, offset);

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
