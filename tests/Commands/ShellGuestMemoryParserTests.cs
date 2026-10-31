using System.Text;
using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellGuestMemoryParserTests
{
    [Fact]
    public void Parsers_accept_memory_only_platform_and_preserve_quoted_text()
    {
        var memory = new Memory(2048);
        var source = new APTR(16);
        const string line = "echo \"hello > world\" >>\"DH0:result file\"";
        Encoding.ASCII.GetBytes(line).CopyTo(memory.Bytes, 16);
        var workspace = new ShellRedirectionWorkspace(new APTR(256), 256,
            new APTR(512), 256, new APTR(768), 256, new APTR(1024), 256);
        Assert.True(ShellRedirectionParser.Parse(ref memory, source, (uint)line.Length,
            in workspace, out var spec, out var length));
        Assert.Equal(1u, spec.OutputAppend);
        Assert.Equal("DH0:result file", Encoding.ASCII.GetString(memory.Bytes, 768, (int)spec.OutputLength));
        var cursor = new ShellTextCursor(workspace.Command, length);
        Assert.Equal((int)ShellTextTokenResult.Token, ShellTextParser.NextToken(ref memory,
            ref cursor, new APTR(1300), 256, out _, out _));
        Assert.Equal((int)ShellTextTokenResult.Token, ShellTextParser.NextToken(ref memory,
            ref cursor, new APTR(1300), 256, out var tokenLength, out var flags));
        Assert.Equal("hello > world", Encoding.ASCII.GetString(memory.Bytes, 1300, (int)tokenLength));
        Assert.Equal(ShellTextParser.TokenUsedSyntax, flags);
    }

    private struct Memory(int size) : IAmigaGuestMemory
    {
        public readonly byte[] Bytes = new byte[size];
        public bool IsMapped(APTR address, uint count) => address.Raw <= Bytes.Length && count <= (uint)Bytes.Length - address.Raw;
        public byte ReadUInt8(APTR address, int offset = 0) => Bytes[checked((int)address.Raw + offset)];
        public ushort ReadUInt16(APTR address, int offset = 0) => (ushort)((ReadUInt8(address, offset) << 8) | ReadUInt8(address, offset + 1));
        public uint ReadUInt32(APTR address, int offset = 0) => ((uint)ReadUInt16(address, offset) << 16) | ReadUInt16(address, offset + 2);
        public void WriteUInt8(APTR address, int offset, byte value) => Bytes[checked((int)address.Raw + offset)] = value;
        public void WriteUInt16(APTR address, int offset, ushort value) { WriteUInt8(address, offset, (byte)(value >> 8)); WriteUInt8(address, offset + 1, (byte)value); }
        public void WriteUInt32(APTR address, int offset, uint value) { WriteUInt16(address, offset, (ushort)(value >> 16)); WriteUInt16(address, offset + 2, (ushort)value); }
        public void Clear(APTR address, uint count) => Array.Clear(Bytes, (int)address.Raw, (int)count);
        public void Copy(APTR source, APTR destination, uint count) => Array.Copy(Bytes, (int)source.Raw, Bytes, (int)destination.Raw, (int)count);
    }
}
