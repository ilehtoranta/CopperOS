using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class QuoteRuleParserTests
{
    [Fact]
    public void Parses_documented_names_in_order_without_case_distinction()
    {
        var memory = new TestMemory(256); var source = new APTR(8); var output = new APTR(96);
        var text = " readitem\tHeX URI BASE64 MatchPattern AREXX sh JS c ";
        Encoding.Latin1.GetBytes(text).CopyTo(memory.Bytes.AsSpan((int)source.Raw));

        Assert.True(QuoteRuleParser.TryParse(ref memory, source, (uint)text.Length, output, 16, out var count));
        Assert.Equal(9u, count);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, memory.Bytes.AsSpan((int)output.Raw, (int)count).ToArray());
    }

    [Fact]
    public void Rejects_empty_unknown_short_and_overlapping_spans_before_writing()
    {
        var memory = new TestMemory(256); var source = new APTR(8); var output = new APTR(96);
        memory.Bytes.AsSpan((int)output.Raw, 32).Fill(0xa5);
        Assert.False(QuoteRuleParser.TryParse(ref memory, source, 0, output, 32, out _));
        Encoding.Latin1.GetBytes("READITEM UNKNOWN").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        Assert.False(QuoteRuleParser.TryParse(ref memory, source, 16, output, 32, out _));
        Encoding.Latin1.GetBytes("READITEM HEX").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        Assert.False(QuoteRuleParser.TryParse(ref memory, source, 12, output, 1, out _));
        Assert.False(QuoteRuleParser.TryParse(ref memory, source, 12, new APTR(12), 32, out _));
        Assert.All(memory.Bytes.AsSpan((int)output.Raw, 32).ToArray(), value => Assert.Equal((byte)0xa5, value));
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
