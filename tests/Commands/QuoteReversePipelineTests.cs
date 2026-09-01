using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class QuoteReversePipelineTests
{
    [Fact]
    public void Reverses_supported_stages_in_reverse_declared_order()
    {
        var memory = new TestMemory(384); var source = new APTR(8); var rules = new APTR(80); var first = new APTR(128); var second = new APTR(256);
        Encoding.Latin1.GetBytes("NDE=").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        memory.Bytes[(int)rules.Raw] = (byte)QuoteForwardRule.Hex;
        memory.Bytes[(int)rules.Raw + 1] = (byte)QuoteForwardRule.Base64;

        Assert.True(QuoteReversePipeline.TryApply(ref memory, source, 4, rules, 2, first, 96, second, 96, out var output, out var count));
        Assert.Equal(second, output);
        Assert.Equal("A", Encoding.Latin1.GetString(memory.Bytes, (int)output.Raw, (int)count));
    }

    [Fact]
    public void Rejects_unimplemented_rules_before_writing_scratch()
    {
        var memory = new TestMemory(256); var source = new APTR(8); var rules = new APTR(48); var first = new APTR(80); var second = new APTR(160);
        Encoding.Latin1.GetBytes("41").CopyTo(memory.Bytes.AsSpan((int)source.Raw));
        memory.Bytes[(int)rules.Raw] = (byte)QuoteForwardRule.Hex;
        memory.Bytes[(int)rules.Raw + 1] = (byte)QuoteForwardRule.ReadItem;
        memory.Bytes.AsSpan((int)first.Raw, 64).Fill(0xa5); memory.Bytes.AsSpan((int)second.Raw, 64).Fill(0xa5);

        Assert.False(QuoteReversePipeline.TryApply(ref memory, source, 2, rules, 2, first, 64, second, 64, out _, out _));
        Assert.All(memory.Bytes.AsSpan((int)first.Raw, 64).ToArray(), value => Assert.Equal((byte)0xa5, value));
        Assert.All(memory.Bytes.AsSpan((int)second.Raw, 64).ToArray(), value => Assert.Equal((byte)0xa5, value));
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
