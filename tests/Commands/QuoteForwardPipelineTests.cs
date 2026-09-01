using System.Text;
using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class QuoteForwardPipelineTests
{
    [Fact]
    public void Applies_rules_in_declared_order_through_two_owned_buffers()
    {
        var memory = new TestMemory(256); var source = new APTR(8); var rules = new APTR(48); var first = new APTR(80); var second = new APTR(160);
        Encoding.Latin1.GetBytes("A=B").CopyTo(memory.Bytes.AsSpan(8));
        memory.Bytes[48] = (byte)QuoteForwardRule.ReadItem; memory.Bytes[49] = (byte)QuoteForwardRule.Hex;

        Assert.True(QuoteForwardPipeline.TryApply(ref memory, source, 3, rules, 2, first, 64, second, 64, out var output, out var length));
        Assert.Equal(second, output);
        Assert.Equal("22413d4222", Encoding.Latin1.GetString(memory.Bytes, (int)output.Raw, (int)length));
    }

    [Fact]
    public void Rejects_unknown_rules_and_overlapping_scratch_buffers()
    {
        var memory = new TestMemory(256); var source = new APTR(8); var rules = new APTR(48);
        memory.Bytes[8] = (byte)'A'; memory.Bytes[48] = 99; memory.Bytes.AsSpan(80, 64).Fill(0xa5);
        Assert.False(QuoteForwardPipeline.TryApply(ref memory, source, 1, rules, 1, new APTR(80), 64, new APTR(160), 64, out _, out _));
        Assert.False(QuoteForwardPipeline.TryApply(ref memory, source, 1, rules, 0, new APTR(80), 64, new APTR(100), 64, out _, out _));
        Assert.All(memory.Bytes.AsSpan(80, 64).ToArray(), value => Assert.Equal((byte)0xa5, value));
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
