using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class SearchLineFormatterTests
{
    [Fact]
    public void Formats_numbered_matches_and_requested_following_lines()
    {
        var actual = Format("first\nneedle\nafter\nlast", "needle", true,
            false, false, 1, out var found);
        Assert.True(found);
        Assert.Equal("     2> needle\n     3: after\n", actual);
    }

    [Fact]
    public void Formats_each_matching_line_and_honors_nonum()
    {
        var actual = Format("needle\nnone\nneedle", "needle", true,
            true, false, 0, out var found);
        Assert.True(found);
        Assert.Equal("needle\nneedle\n", actual);
    }

    [Fact]
    public void Stops_quiet_search_after_first_match_without_output()
    {
        var actual = Format("none\nNeedle\nneedle", "needle", false,
            false, true, 3, out var found);
        Assert.True(found);
        Assert.Equal("", actual);
    }

    [Fact]
    public void Splits_control_bytes_and_replaces_tab_in_selected_output()
    {
        var actual = Format("x\0needle\tvalue\rneedle", "needle", true,
            false, false, 0, out var found);
        Assert.True(found);
        Assert.Equal("     1> needle.value\n     1> needle\n", actual);
    }

    [Fact]
    public void Rejects_short_or_overlapping_destination_without_writes()
    {
        var memory = new TestMemory(256);
        Write(memory.Bytes, 8, "needle");
        Write(memory.Bytes, 64, "needle");
        memory.Bytes.AsSpan(128, 64).Fill(0xa5);

        Assert.False(SearchLineFormatter.TryFormat(ref memory, new APTR(8), 6,
            new APTR(64), 6, true, false, false, 0, new APTR(128), 4,
            out _, out _));
        Assert.False(SearchLineFormatter.TryFormat(ref memory, new APTR(8), 6,
            new APTR(64), 6, true, false, false, 0, new APTR(8), 64,
            out _, out _));
        Assert.All(memory.Bytes.AsSpan(128, 64).ToArray(), value =>
            Assert.Equal((byte)0xa5, value));
    }

    private static string Format(string source, string pattern, bool caseSensitive,
        bool noNumber, bool quiet, uint linesAfter, out bool found)
    {
        var input = Encoding.Latin1.GetBytes(source);
        var needle = Encoding.Latin1.GetBytes(pattern);
        var memory = new TestMemory(1024);
        input.CopyTo(memory.Bytes.AsSpan(8));
        needle.CopyTo(memory.Bytes.AsSpan(400));
        Assert.True(SearchLineFormatter.TryFormat(ref memory, new APTR(8),
            (uint)input.Length, new APTR(400), (uint)needle.Length, caseSensitive,
            noNumber, quiet, linesAfter, new APTR(600), 400, out found,
            out var written));
        return Encoding.Latin1.GetString(memory.Bytes, 600, (int)written);
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
