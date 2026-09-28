using Amiga;

namespace CopperOS.Commands.Tests;

public sealed class Exe2ArcOutputNameTests
{
    [Theory]
    [InlineData("RAM:in/demo.exe", "ace", "RAM:in/demo.ace")]
    [InlineData("RAM:in/demo", "rar", "RAM:in/demo.rar")]
    [InlineData("demo", "rar", "demo.rar")]
    [InlineData("RAM:dir.v1/file", "cab", "RAM:dir.v1/file.cab")]
    [InlineData(".profile", "lzh", ".lzh")]
    [InlineData("RAM:in/archive.tar.gz", "zip", "RAM:in/archive.tar.zip")]
    public void Source_suffix_rule_is_applied_in_guest_memory(string source,
        string extension, string expected)
    {
        var memory = new NameMemory(source, extension);
        var found = Exe2ArcOutputName.TryBuild(ref memory, memory.Source,
            memory.Destination, 512, memory.Extension,
            out uint bytes);

        Assert.True(found);
        Assert.Equal((uint)(expected.Length + 1), bytes);
        Assert.Equal(expected, memory.DestinationText);
        Assert.Equal(0xcc, memory.GuardByte);
    }

    [Fact]
    public void Capacity_rejection_does_not_write_destination()
    {
        var memory = new NameMemory("RAM:in/demo.exe", "ace");
        var before = memory.Bytes.ToArray();

        Assert.False(Exe2ArcOutputName.TryBuild(ref memory, memory.Source,
            memory.Destination, 4, memory.Extension,
            out uint bytes));

        Assert.Equal(0u, bytes);
        Assert.Equal(before, memory.Bytes);
    }

    [Fact]
    public void Unterminated_source_is_rejected_without_unbounded_scan()
    {
        var memory = new NameMemory(new string('x',
            (int)Exe2ArcOutputName.MaximumSourceBytes), "ace", terminateSource: false);
        var before = memory.Bytes.ToArray();

        Assert.False(Exe2ArcOutputName.TryBuild(ref memory, memory.Source,
            memory.Destination, 512, memory.Extension,
            out uint bytes));

        Assert.Equal(0u, bytes);
        Assert.Equal(before, memory.Bytes);
    }

    private struct NameMemory : IAmigaGuestMemory
    {
        private const uint Base = 0x1000;
        private const int SourceOffset = 0x100;
        private const int ExtensionOffset = 0x300;
        private const int DestinationOffset = 0x500;
        private readonly byte[] _bytes;

        public NameMemory(string source, string extension,
            bool terminateSource = true)
        {
            _bytes = Enumerable.Repeat((byte)0xcc, 0x2000).ToArray();
            Source = new APTR(Base + SourceOffset);
            Extension = new APTR(Base + ExtensionOffset);
            Destination = new APTR(Base + DestinationOffset);
            for (var index = 0; index < source.Length; index++)
                _bytes[SourceOffset + index] = (byte)source[index];
            if (terminateSource)
                _bytes[SourceOffset + source.Length] = 0;
            for (var index = 0; index < extension.Length; index++)
                _bytes[ExtensionOffset + index] = (byte)extension[index];
            _bytes[ExtensionOffset + extension.Length] = 0;
        }

        public APTR Source { get; }
        public APTR Extension { get; }
        public APTR Destination { get; }
        public byte GuardByte => _bytes[DestinationOffset + 511];
        public string DestinationText
        {
            get
            {
                var length = 0;
                while (length < 512 && _bytes[DestinationOffset + length] != 0)
                    length++;
                return System.Text.Encoding.ASCII.GetString(_bytes,
                    DestinationOffset, length);
            }
        }
        public byte[] Bytes => _bytes;

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
