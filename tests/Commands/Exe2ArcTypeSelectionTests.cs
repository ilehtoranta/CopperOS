using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcTypeSelectionTests
{
    [Fact]
    public void Null_type_leaves_archive_unspecified()
    {
        var memory = new TypeMemory(string.Empty);

        Assert.True(Exe2ArcTypeSelection.TrySelect(ref memory, APTR.Null,
            out var selected));
        Assert.Equal(Exe2ArcArchiveType.Unspecified, selected);
    }

    [Theory]
    [InlineData("ZIP", Exe2ArcArchiveType.Zip)]
    [InlineData("Ace", Exe2ArcArchiveType.Ace)]
    [InlineData("RAR", Exe2ArcArchiveType.Rar)]
    [InlineData("Cabinet", Exe2ArcArchiveType.Cabinet)]
    [InlineData("ArJ", Exe2ArcArchiveType.Arj)]
    [InlineData("LhA", Exe2ArcArchiveType.Lha)]
    [InlineData("AMIGA-LhA", Exe2ArcArchiveType.Lzh)]
    public void Known_extension_or_display_name_is_case_insensitive(string value,
        Exe2ArcArchiveType expected)
    {
        var memory = new TypeMemory(value);

        Assert.True(Exe2ArcTypeSelection.TrySelect(ref memory, memory.Type,
            out var selected));
        Assert.Equal(expected, selected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("zipx")]
    [InlineData("zip ")]
    [InlineData("cabinet-old")]
    public void Unknown_or_prefix_type_is_rejected(string value)
    {
        var memory = new TypeMemory(value);

        Assert.False(Exe2ArcTypeSelection.TrySelect(ref memory, memory.Type,
            out var selected));
        Assert.Equal(Exe2ArcArchiveType.Unspecified, selected);
    }

    [Fact]
    public void Unterminated_type_is_rejected_at_bounded_limit()
    {
        var memory = new TypeMemory(new string('x',
            (int)Exe2ArcTypeSelection.MaximumTypeBytes), terminate: false);

        Assert.False(Exe2ArcTypeSelection.TrySelect(ref memory, memory.Type,
            out var selected));
        Assert.Equal(Exe2ArcArchiveType.Unspecified, selected);
    }

    private struct TypeMemory : IAmigaGuestMemory
    {
        private const uint Base = 0x2000;
        private const int TypeOffset = 0x100;
        private readonly byte[] _bytes;

        public TypeMemory(string value, bool terminate = true)
        {
            _bytes = Enumerable.Repeat((byte)0xcc, 0x400).ToArray();
            Type = new APTR(Base + TypeOffset);
            for (var index = 0; index < value.Length; index++)
                _bytes[TypeOffset + index] = (byte)value[index];
            if (terminate)
                _bytes[TypeOffset + value.Length] = 0;
        }

        public APTR Type { get; }

        public bool IsMapped(APTR address, uint byteSize) =>
            address.Raw >= Base && address.Raw - Base + byteSize <= _bytes.Length;

        public byte ReadUInt8(APTR address, int offset = 0) =>
            _bytes[checked((int)(address.Raw - Base) + offset)];
        public ushort ReadUInt16(APTR address, int offset = 0) =>
            throw new NotSupportedException();
        public uint ReadUInt32(APTR address, int offset = 0) =>
            throw new NotSupportedException();
        public void WriteUInt8(APTR address, int offset, byte value) =>
            throw new NotSupportedException();
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
