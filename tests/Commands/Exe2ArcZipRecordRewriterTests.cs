using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcZipRecordRewriterTests
{
    [Fact]
    public void Central_record_offset_is_rebased_in_place()
    {
        var memory = new RecordMemory(Exe2ArcZipRecordKind.Central, 100);

        Assert.True(Exe2ArcZipRecordRewriter.TryRewrite(ref memory,
            memory.Record, Exe2ArcZipRecordRewriter.CentralRecordBytes, 30,
            out var kind));
        Assert.Equal(Exe2ArcZipRecordKind.Central, kind);
        Assert.Equal(130u, memory.Read32(42));
        Assert.Equal(0xcc, memory.Guard);
    }

    [Fact]
    public void End_record_offset_is_rebased_in_place()
    {
        var memory = new RecordMemory(Exe2ArcZipRecordKind.EndOfCentralDirectory,
            100);

        Assert.True(Exe2ArcZipRecordRewriter.TryRewrite(ref memory,
            memory.Record, Exe2ArcZipRecordRewriter.EndRecordBytes, 30,
            out var kind));
        Assert.Equal(Exe2ArcZipRecordKind.EndOfCentralDirectory, kind);
        Assert.Equal(130u, memory.Read32(16));
        Assert.Equal(0xcc, memory.Guard);
    }

    [Fact]
    public void Local_record_is_recognized_without_mutation()
    {
        var memory = new RecordMemory(Exe2ArcZipRecordKind.Local, 100);
        var before = memory.Bytes.ToArray();

        Assert.True(Exe2ArcZipRecordRewriter.TryRewrite(ref memory,
            memory.Record, Exe2ArcZipRecordRewriter.LocalRecordBytes, 30,
            out var kind));
        Assert.Equal(Exe2ArcZipRecordKind.Local, kind);
        Assert.Equal(before, memory.Bytes);
    }

    [Fact]
    public void Unknown_record_and_offset_overflow_are_rejected_without_writes()
    {
        var unknown = new RecordMemory(Exe2ArcZipRecordKind.Unknown, 100);
        var beforeUnknown = unknown.Bytes.ToArray();
        Assert.False(Exe2ArcZipRecordRewriter.TryRewrite(ref unknown,
            unknown.Record, 46, 30, out var unknownKind));
        Assert.Equal(Exe2ArcZipRecordKind.Unknown, unknownKind);
        Assert.Equal(beforeUnknown, unknown.Bytes);

        var overflow = new RecordMemory(Exe2ArcZipRecordKind.Central,
            uint.MaxValue - 10);
        var beforeOverflow = overflow.Bytes.ToArray();
        Assert.False(Exe2ArcZipRecordRewriter.TryRewrite(ref overflow,
            overflow.Record, 46, 30, out var overflowKind));
        Assert.Equal(Exe2ArcZipRecordKind.Central, overflowKind);
        Assert.Equal(beforeOverflow, overflow.Bytes);
    }

    [Fact]
    public void Short_or_unmapped_record_is_rejected_before_access()
    {
        var memory = new RecordMemory(Exe2ArcZipRecordKind.Central, 100);
        var before = memory.Bytes.ToArray();

        Assert.False(Exe2ArcZipRecordRewriter.TryRewrite(ref memory,
            memory.Record, 45, 30, out var kind));
        Assert.Equal(Exe2ArcZipRecordKind.Central, kind);
        Assert.Equal(before, memory.Bytes);
    }

    private struct RecordMemory : IAmigaGuestMemory
    {
        private const uint Base = 0x6000;
        private const int Offset = 0x100;
        private readonly byte[] _bytes;

        public RecordMemory(Exe2ArcZipRecordKind kind, uint value)
        {
            _bytes = Enumerable.Repeat((byte)0xcc, 512).ToArray();
            Record = new APTR(Base + Offset);
            Write32(kind switch
            {
                Exe2ArcZipRecordKind.Local => 0,
                Exe2ArcZipRecordKind.Central => 42,
                Exe2ArcZipRecordKind.EndOfCentralDirectory => 16,
                _ => 4,
            }, value);
            Write32(0, kind switch
            {
                Exe2ArcZipRecordKind.Local => 0x04034b50u,
                Exe2ArcZipRecordKind.Central => 0x02014b50u,
                Exe2ArcZipRecordKind.EndOfCentralDirectory => 0x06054b50u,
                _ => 0x12345678u,
            });
        }

        public APTR Record { get; }
        public byte[] Bytes => _bytes;
        public byte Guard => _bytes[^1];

        public uint Read32(int offset) =>
            (uint)_bytes[Offset + offset] |
            ((uint)_bytes[Offset + offset + 1] << 8) |
            ((uint)_bytes[Offset + offset + 2] << 16) |
            ((uint)_bytes[Offset + offset + 3] << 24);

        private void Write32(int offset, uint value)
        {
            _bytes[Offset + offset] = (byte)value;
            _bytes[Offset + offset + 1] = (byte)(value >> 8);
            _bytes[Offset + offset + 2] = (byte)(value >> 16);
            _bytes[Offset + offset + 3] = (byte)(value >> 24);
        }

        public bool IsMapped(APTR address, uint byteSize) =>
            address.Raw >= Base && address.Raw - Base + byteSize <= _bytes.Length;
        public byte ReadUInt8(APTR address, int offset = 0) =>
            _bytes[checked((int)(address.Raw - Base) + offset)];
        public ushort ReadUInt16(APTR address, int offset = 0) =>
            throw new NotSupportedException();
        public uint ReadUInt32(APTR address, int offset = 0) =>
            throw new NotSupportedException();
        public void WriteUInt8(APTR address, int offset, byte value) =>
            _bytes[checked((int)(address.Raw - Base) + offset)] = value;
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
