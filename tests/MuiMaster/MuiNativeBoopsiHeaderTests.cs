using System.Buffers.Binary;
using Amiga;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiNativeBoopsiHeaderTests
{
    [Fact]
    public void NativeInstancePointerResolvesItsPrecedingHeaderNotInstanceData()
    {
        var memory = new Memory();
        var obj = APTR.FromPointer(0x100);
        var actual = APTR.FromPointer(0x40);
        var decoy = APTR.FromPointer(0x80);
        BOOPSIGuestCodec.WriteObjectHeader(ref memory, APTR.FromPointer(obj.Raw - _Object.Size),
            new _Object { o_Class = actual });
        // A plausible pointer in user instance data must never select a class.
        memory.WriteUInt32(obj, 8, decoy.Raw);
        Assert.True(MuiNativeBoopsiDispatch.TryGetObjectClass(ref memory, obj, out var resolved));
        Assert.Equal(actual, resolved);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(4u)]
    [InlineData(10u)]
    [InlineData(0x1000u)]
    public void UnderflowOrUnmappedHeaderIsRejected(uint pointer)
    {
        var memory = new Memory();
        Assert.False(MuiNativeBoopsiDispatch.TryGetObjectClass(ref memory, APTR.FromPointer(pointer), out _));
    }

    private struct Memory : IAmigaGuestMemory
    {
        private readonly byte[] bytes;
        public Memory() { bytes = new byte[1024]; }
        public bool IsMapped(APTR address, uint count) => address.Raw != 0 && count != 0 &&
            (ulong)address.Raw + count <= (uint)bytes.Length;
        public byte ReadUInt8(APTR a, int o = 0) => bytes[(int)a.Raw + o];
        public ushort ReadUInt16(APTR a, int o = 0) => BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan((int)a.Raw + o, 2));
        public uint ReadUInt32(APTR a, int o = 0) => BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan((int)a.Raw + o, 4));
        public void WriteUInt8(APTR a, int o, byte v) => bytes[(int)a.Raw + o] = v;
        public void WriteUInt16(APTR a, int o, ushort v) => BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan((int)a.Raw + o, 2), v);
        public void WriteUInt32(APTR a, int o, uint v) => BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan((int)a.Raw + o, 4), v);
        public void Clear(APTR a, uint count) => bytes.AsSpan((int)a.Raw, (int)count).Clear();
        public void Copy(APTR source, APTR destination, uint count) =>
            bytes.AsSpan((int)source.Raw, (int)count).CopyTo(bytes.AsSpan((int)destination.Raw, (int)count));
    }
}
