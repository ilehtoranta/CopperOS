using System.Buffers.Binary;
using Amiga;

namespace CopperOS.Commands.Tests;

/// <summary>
/// Inert source-contract fixtures. No wrapper is loaded or executed, and no
/// original-command, complete scanner, CAB/RAR decoder or file-I/O claim follows.
/// </summary>
public sealed class Exe2ArcHeaderProbeTests
{
    private static readonly APTR HeaderAddress = new(0x1001);

    [Theory]
    [InlineData(1u, 9u, 8u)]
    [InlineData(123u, 4096u, 3973u)]
    [InlineData(102397u, 204811u, 102414u)]
    [InlineData(0xfffffff0u, 0xffffffffu, 15u)]
    public void Rar4_reports_every_byte_through_EOF_without_reading_payload(
        uint offset, uint length, uint expected)
    {
        var memory = new HeaderMemory(Rar4());

        bool found = Exe2ArcHeaderProbe.TryGetRar4PayloadLength(ref memory,
            HeaderAddress, 7, offset, length, out uint payload);

        Assert.True(found);
        Assert.Equal(expected, payload);
        Assert.Equal(7, memory.ReadCount);
        Assert.Equal(6, memory.HighestReadOffset);
        Assert.Equal(Rar4(), memory.Bytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Every_Rar4_marker_byte_is_required(int changedByte)
    {
        byte[] bytes = Rar4();
        bytes[changedByte] ^= 0x80;
        var memory = new HeaderMemory(bytes);

        bool found = Exe2ArcHeaderProbe.TryGetRar4PayloadLength(ref memory,
            HeaderAddress, 7, 1, 100, out uint payload);

        Assert.False(found);
        Assert.Equal(0u, payload);
        Assert.InRange(memory.HighestReadOffset, 0, 6);
    }

    [Theory]
    [InlineData(false, 1u, 8u)]
    [InlineData(false, 102393u, 102400u)]
    [InlineData(false, 102395u, 102402u)]
    [InlineData(true, 1u, 21u)]
    [InlineData(true, 102380u, 102400u)]
    [InlineData(true, 102382u, 102402u)]
    public void Complete_EOF_candidate_does_not_reapply_window_admission_threshold(
        bool cabinet, uint offset, uint fileLength)
    {
        // Source-observed candidates inside an already eligible window. This
        // helper does not choose or read windows, so these are not scanner runs.
        byte[] bytes = cabinet ? Cabinet(1, 0) : Rar4();
        var memory = new HeaderMemory(bytes);

        bool found = Probe(cabinet, ref memory, HeaderAddress, (uint)bytes.Length,
            offset, fileLength, out uint payload);

        Assert.True(found);
        Assert.Equal(cabinet ? 1u : 7u, payload);
        Assert.Equal(1, memory.MappingChecks);
        Assert.Equal(cabinet ? 12 : 7, memory.ReadCount);
        Assert.Equal(bytes.Length - 1, memory.HighestReadOffset);
        Assert.Equal(cabinet ? Cabinet(1, 0) : Rar4(), memory.Bytes);
    }

    [Fact]
    public void Rar5_is_not_a_Rar4_match()
    {
        var memory = new HeaderMemory([0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x01, 0x00]);

        bool found = Exe2ArcHeaderProbe.TryGetRar4PayloadLength(ref memory,
            HeaderAddress, 8, 32, 4096, out uint payload);

        Assert.False(found);
        Assert.Equal(0u, payload);
    }

    [Theory]
    [InlineData(21u, 0u, 21u, true)]
    [InlineData(21u, 20u, 30u, true)]
    [InlineData(21u, 21u, 30u, false)]
    [InlineData(22u, 0u, 21u, false)]
    [InlineData(0u, 0u, 30u, false)]
    [InlineData(1u, 0u, 30u, true)]
    [InlineData(0x12345678u, 0x01234567u, 0x20000000u, true)]
    [InlineData(0x80000000u, 0x7fffffffu, 0x80000001u, true)]
    [InlineData(0xffffffffu, 0u, 0xfffffffeu, false)]
    [InlineData(100u, 0xffffffffu, 1000u, false)]
    public void Cabinet_uses_unsigned_little_endian_length_and_table_bounds(
        uint declaredLength, uint tableOffset, uint remaining, bool expected)
    {
        var memory = new HeaderMemory(Cabinet(declaredLength, tableOffset));
        byte[] original = memory.Bytes.ToArray();

        bool found = Exe2ArcHeaderProbe.TryGetCabinetPayloadLength(ref memory,
            HeaderAddress, 20, 1, remaining + 1, out uint payload);

        Assert.Equal(expected, found);
        Assert.Equal(expected ? declaredLength : 0u, payload);
        Assert.Equal(original, memory.Bytes);
        Assert.Equal(19, memory.HighestReadOffset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Every_cabinet_marker_byte_is_required(int changedByte)
    {
        byte[] bytes = Cabinet(24, 20);
        bytes[changedByte] ^= 0x20;
        var memory = new HeaderMemory(bytes);

        bool found = Exe2ArcHeaderProbe.TryGetCabinetPayloadLength(ref memory,
            HeaderAddress, 20, 16, 100, out uint payload);

        Assert.False(found);
        Assert.Equal(0u, payload);
        Assert.InRange(memory.HighestReadOffset, 0, 3);
    }

    [Theory]
    [InlineData(false, 6u, false)]
    [InlineData(false, 7u, true)]
    [InlineData(true, 19u, false)]
    [InlineData(true, 20u, true)]
    public void Candidate_requires_complete_header_independently_of_window_admission(
        bool cabinet, uint remaining, bool expected)
    {
        var memory = new HeaderMemory(cabinet ? Cabinet(1, 0) : Rar4());

        bool found = Probe(cabinet, ref memory, HeaderAddress,
            (uint)memory.Bytes.Length, 1, remaining + 1, out uint payload);

        if (!expected)
            AssertRejectedWithoutRead(memory, found, payload);
        else
        {
            Assert.True(found);
            Assert.Equal(cabinet ? 1u : 7u, payload);
            Assert.Equal(1, memory.MappingChecks);
            Assert.Equal(cabinet ? 12 : 7, memory.ReadCount);
        }
    }

    [Theory]
    [InlineData(false, 0u)]
    [InlineData(false, 6u)]
    [InlineData(true, 0u)]
    [InlineData(true, 19u)]
    public void Incomplete_header_window_is_never_read(bool cabinet, uint headerBytes)
    {
        var memory = new HeaderMemory(cabinet ? Cabinet(21, 0) : Rar4());

        bool found = Probe(cabinet, ref memory, HeaderAddress,
            headerBytes, 1, 100, out uint payload);

        AssertRejectedWithoutRead(memory, found, payload);
    }

    [Theory]
    [InlineData(false, 0u, 100u)]
    [InlineData(false, 100u, 100u)]
    [InlineData(false, 101u, 100u)]
    [InlineData(false, 1u, 0u)]
    [InlineData(true, 0u, 100u)]
    [InlineData(true, 100u, 100u)]
    [InlineData(true, 101u, 100u)]
    [InlineData(true, 1u, 0u)]
    public void Zero_or_out_of_file_candidate_never_wraps_or_reads(
        bool cabinet, uint offset, uint length)
    {
        var memory = new HeaderMemory(cabinet ? Cabinet(21, 0) : Rar4());

        bool found = Probe(cabinet, ref memory, HeaderAddress,
            (uint)memory.Bytes.Length, offset, length, out uint payload);

        AssertRejectedWithoutRead(memory, found, payload);
    }

    [Theory]
    [InlineData(false, 0u, 7u)]
    [InlineData(false, 0xfffffffdu, 7u)]
    [InlineData(false, 0x1001u, 0xffffffffu)]
    [InlineData(true, 0u, 20u)]
    [InlineData(true, 0xfffffff0u, 20u)]
    [InlineData(true, 0x1001u, 0xffffffffu)]
    public void Invalid_address_span_is_rejected_before_mapping_or_reading(
        bool cabinet, uint address, uint windowLength)
    {
        var memory = new HeaderMemory(cabinet ? Cabinet(21, 0) : Rar4());

        bool found = Probe(cabinet, ref memory, new APTR(address),
            windowLength, 1, uint.MaxValue, out uint payload);

        AssertRejectedWithoutRead(memory, found, payload);
        Assert.Equal(0, memory.MappingChecks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unmapped_window_is_rejected_without_touching_memory(bool cabinet)
    {
        var memory = new HeaderMemory(cabinet ? Cabinet(21, 0) : Rar4())
        { RejectMapping = true };

        bool found = Probe(cabinet, ref memory, HeaderAddress,
            (uint)memory.Bytes.Length, 1, 100, out uint payload);

        AssertRejectedWithoutRead(memory, found, payload);
        Assert.Equal(1, memory.MappingChecks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Window_claim_cannot_extend_past_known_EOF(bool cabinet)
    {
        var memory = new HeaderMemory(cabinet ? Cabinet(21, 0) : Rar4());

        bool found = Probe(cabinet, ref memory, HeaderAddress,
            101, 1, 100, out uint payload);

        AssertRejectedWithoutRead(memory, found, payload);
        Assert.Equal(0, memory.MappingChecks);
    }

    [Fact]
    public void Interleaved_candidates_keep_lengths_and_buffers_separate()
    {
        var rar = new HeaderMemory(Rar4());
        var cab = new HeaderMemory(Cabinet(36, 30));
        for (int iteration = 0; iteration < 64; iteration++)
        {
            Assert.True(Probe(false, ref rar, HeaderAddress, 7,
                8, 100, out uint rarLength));
            Assert.True(Probe(true, ref cab, HeaderAddress, 20,
                8, 100, out uint cabinetLength));
            Assert.Equal(92u, rarLength);
            Assert.Equal(36u, cabinetLength);
        }
        Assert.Equal(Rar4(), rar.Bytes);
        Assert.Equal(Cabinet(36, 30), cab.Bytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Final_uint_byte_is_readable_but_one_byte_wrap_is_rejected(
        bool cabinet, bool wraps)
    {
        byte[] bytes = cabinet ? Cabinet(36, 30) : Rar4();
        uint width = (uint)bytes.Length;
        APTR address = new(uint.MaxValue - (width - 1) + (wraps ? 1u : 0u));
        var memory = new HeaderMemory(bytes, address);

        bool found = Probe(cabinet, ref memory, address, width, 8, 100,
            out uint payload);

        if (wraps)
        {
            AssertRejectedWithoutRead(memory, found, payload);
            Assert.Equal(0, memory.MappingChecks);
        }
        else
        {
            Assert.True(found);
            Assert.Equal(cabinet ? 36u : 92u, payload);
            Assert.Equal(1, memory.MappingChecks);
            Assert.Equal(cabinet ? 12 : 7, memory.ReadCount);
            Assert.Equal((int)width - 1, memory.HighestReadOffset);
        }
        Assert.Equal(cabinet ? Cabinet(36, 30) : Rar4(), memory.Bytes);
    }

    [Fact]
    public void Header_predicates_allocate_no_managed_storage()
    {
        var rar = new HeaderMemory(Rar4());
        var cab = new HeaderMemory(Cabinet(36, 30));
        _ = Probe(false, ref rar, HeaderAddress, 7, 8, 100, out _);
        _ = Probe(true, ref cab, HeaderAddress, 20, 8, 100, out _);
        bool allMatched = true;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 256; i++)
        {
            allMatched &= Probe(false, ref rar, HeaderAddress, 7, 8, 100, out uint r);
            allMatched &= Probe(true, ref cab, HeaderAddress, 20, 8, 100, out uint c);
            allMatched &= r == 92 && c == 36;
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(allMatched);
        Assert.Equal(0, allocated);
    }

    private static bool Probe(bool cabinet, ref HeaderMemory memory, APTR header,
        uint headerBytes, uint offset, uint fileLength, out uint payloadLength) =>
        cabinet
            ? Exe2ArcHeaderProbe.TryGetCabinetPayloadLength(ref memory, header,
                headerBytes, offset, fileLength, out payloadLength)
            : Exe2ArcHeaderProbe.TryGetRar4PayloadLength(ref memory, header,
                headerBytes, offset, fileLength, out payloadLength);

    private static void AssertRejectedWithoutRead(HeaderMemory memory,
        bool found, uint payload)
    {
        Assert.False(found);
        Assert.Equal(0u, payload);
        Assert.Equal(0, memory.ReadCount);
        Assert.Equal(-1, memory.HighestReadOffset);
    }

    private static byte[] Rar4() => [0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x00];

    private static byte[] Cabinet(uint length, uint tableOffset)
    {
        byte[] bytes = Enumerable.Repeat((byte)0xa5, 20).ToArray();
        "MSCF"u8.CopyTo(bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), length);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(16, 4), tableOffset);
        return bytes;
    }

    private struct HeaderMemory : IAmigaGuestMemory
    {
        public HeaderMemory(byte[] bytes) : this(bytes, HeaderAddress) { }

        public HeaderMemory(byte[] bytes, APTR baseAddress)
        {
            Bytes = bytes;
            BaseAddress = baseAddress;
            ReadCount = 0;
            HighestReadOffset = -1;
            MappingChecks = 0;
            RejectMapping = false;
        }

        public readonly byte[] Bytes;
        private readonly APTR BaseAddress;
        public int ReadCount;
        public int HighestReadOffset;
        public int MappingChecks;
        public bool RejectMapping;

        public bool IsMapped(APTR address, uint byteSize)
        {
            MappingChecks++;
            return !RejectMapping && address == BaseAddress &&
                byteSize <= (uint)Bytes.Length;
        }

        public byte ReadUInt8(APTR address, int offset = 0)
        {
            if (address != BaseAddress || offset < 0 || offset >= Bytes.Length)
                throw new InvalidOperationException("Read outside supplied header.");
            ReadCount++;
            if (offset > HighestReadOffset)
                HighestReadOffset = offset;
            return Bytes[offset];
        }

        // The probe must use bytes even on an unaligned big-endian guest.
        public ushort ReadUInt16(APTR address, int offset = 0) => throw new NotSupportedException();
        public uint ReadUInt32(APTR address, int offset = 0) => throw new NotSupportedException();
        public void WriteUInt8(APTR address, int offset, byte value) => throw new NotSupportedException();
        public void WriteUInt16(APTR address, int offset, ushort value) => throw new NotSupportedException();
        public void WriteUInt32(APTR address, int offset, uint value) => throw new NotSupportedException();
        public void Clear(APTR address, uint byteCount) => throw new NotSupportedException();
        public void Copy(APTR source, APTR destination, uint byteCount) => throw new NotSupportedException();
    }
}
