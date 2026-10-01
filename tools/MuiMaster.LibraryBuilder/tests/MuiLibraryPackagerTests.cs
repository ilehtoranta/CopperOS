using System.Buffers.Binary;
using System.Text;
using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.MuiMaster.LibraryBuilder.Tests;

public sealed class MuiLibraryPackagerTests
{
    [Fact]
    public void CompleteVectorTableKeepsMorphosLvoPositions()
    {
        var slots = MuiLibraryPackager.FunctionTableExports;
        Assert.Equal(MuiResidentMetadata.FunctionTableEntryCount, slots.Count);
        Assert.Equal(MuiNativeLibraryEntrypoints.OpenExport, slots[0]);
        Assert.Equal(MuiNativeLibraryEntrypoints.ReservedExport, slots[3]);
        Assert.Equal(MuiNativeLibraryEntrypoints.GetClassExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -78) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.FreeClassExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -84) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.RequestIDCMPExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -90) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.RejectIDCMPExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -96) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.CreateCustomClassExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -108) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.DeleteCustomClassExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -114) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.NewObjectAExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -30) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.DisposeObjectExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -36) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.RequestAExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -42) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.RedrawExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -102) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.LayoutExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -126) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.RequestObjectAExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -756) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.AllocAslRequestExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -48) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.AslRequestExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -54) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.FreeAslRequestExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -60) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.MakeObjectAExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -120) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.ErrorExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -66) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.SetErrorExport,
            slots[MuiResidentMetadata.ManagementVectorCount +
                ((MuiResidentMetadata.FirstLvo - -72) / MuiResidentMetadata.VectorStride)]);
        Assert.Equal(MuiNativeLibraryEntrypoints.RequestObjectAExport, slots[^1]);
    }

    [Theory]
    [InlineData(0x00100000u)]
    [InlineData(0x00500000u)]
    public void SerializedHunkRelocatesTypedResidentAndAllAbiVectors(uint load)
    {
        var input = Input();
        var artifact = MuiLibraryPackager.Build(input.Code, input.Relocations, input.Symbols);
        // Independent file parser consumes the emitted artifact, not artifact.Code
        // or the builder's relocation list. This catches container/addend errors.
        var memory = ReadHunk(artifact.Image, load, out var sites);
        var address = APTR.FromPointer(checked(load + (uint)artifact.Layout.Resident));
        var resident = ExecResidentCodec.Read(ref memory, address);
        Assert.Equal((ushort)0x4AFC, resident.MatchWord);
        Assert.Equal(address.Raw, resident.MatchTag.Raw);
        Assert.Equal(load + (uint)artifact.Layout.EndMarker, resident.EndSkip.Raw);
        Assert.True(memory.IsMapped(resident.EndSkip, 4));
        Assert.Equal(ResidentFlags.AutoInit, resident.Flags);
        Assert.Equal((byte)NodeType.Library, resident.Type);
        Assert.Equal(MuiResidentMetadata.DevelopmentVersion, resident.Version);
        Assert.Equal(MuiResidentMetadata.DevelopmentNameText, memory.Text(resident.Name.Address));
        Assert.Equal("CopperOS MUI development 0.1", memory.Text(resident.IdString.Address));
        Assert.Equal(load + (uint)artifact.Layout.AutoInit, resident.Init.Raw);
        var autoInit = ExecResidentAutoInitCodec.Read(ref memory, resident.Init);
        Assert.Equal((uint)MuiNativeLibraryEntrypoints.PositiveBytes, autoInit.DataSize);
        Assert.True(autoInit.StructureTable.IsNull);
        Assert.Equal(load + (uint)artifact.Layout.FunctionTable, autoInit.FunctionTable.Raw);
        Assert.Equal(load + artifact.Exports[MuiNativeLibraryEntrypoints.InitExport], autoInit.InitFunction.Raw);
        for (var index = 0; index < MuiLibraryPackager.FunctionTableExports.Count; index++)
            Assert.Equal(load + artifact.Exports[MuiLibraryPackager.FunctionTableExports[index]],
                memory.ReadUInt32(autoInit.FunctionTable, index * 4));
        Assert.Equal(uint.MaxValue, memory.ReadUInt32(autoInit.FunctionTable,
            MuiLibraryPackager.FunctionTableExports.Count * 4));
        Assert.Equal(load + (uint)artifact.Layout.CompiledCode + 16,
            memory.ReadUInt32(APTR.FromPointer(load + (uint)artifact.Layout.CompiledCode)));
        Assert.Equal(load + (uint)artifact.Layout.CompiledCode,
            memory.ReadUInt32(APTR.FromPointer(load + (uint)artifact.Layout.CompiledCode), 6));
        Assert.Equal(artifact.Relocations, sites);
        Assert.Equal(input.Relocations.Length +
            MuiLibraryPackager.FunctionTableExports.Count + 7, sites.Count);
        Assert.DoesNotContain(artifact.Layout.AutoInit + ExecLayout.ResidentAutoInit.DataSize, sites);
        Assert.DoesNotContain(artifact.Layout.AutoInit + ExecLayout.ResidentAutoInit.StructureTable, sites);
        Assert.DoesNotContain(artifact.Layout.FunctionTable +
            MuiLibraryPackager.FunctionTableExports.Count * 4, sites);
    }

    [Fact]
    public void FirstEntryIsInertAndOriginalInputIsUnchanged()
    {
        var input = Input();
        var original = (byte[])input.Code.Clone();
        var artifact = MuiLibraryPackager.Build(input.Code, input.Relocations, input.Symbols);
        Assert.Equal(new byte[] { 0x70, 0xFF, 0x4E, 0x75 }, artifact.Code[..4]);
        Assert.DoesNotContain(artifact.Relocations, offset => offset < 4);
        Assert.Equal(original, input.Code);
        Assert.NotEqual(0, artifact.Layout.CompiledCode);
        // Bytes not covered by relocations retain the original code exactly.
        Assert.Equal(original[10..], artifact.Code[artifact.Layout.CompiledCode..][10..original.Length]);
    }

    [Fact]
    public void PackagingIsDeterministicDespiteInputEnumerationOrder()
    {
        var input = Input();
        var first = MuiLibraryPackager.Build(input.Code, input.Relocations, input.Symbols);
        var second = MuiLibraryPackager.Build(input.Code, input.Relocations.Reverse().ToArray(),
            input.Symbols.Reverse().ToArray());
        Assert.Equal(first.Image, second.Image);
        Assert.Equal(first.Code, second.Code);
        Assert.Equal(first.Layout, second.Layout);
        Assert.Equal(first.Exports, second.Exports);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(1)]
    [InlineData(126)]
    [InlineData(128)]
    [InlineData(int.MaxValue)]
    public void InvalidRelocationSitesAreRejected(int offset)
    {
        var input = Input();
        Assert.Throws<InvalidDataException>(() => MuiLibraryPackager.Build(input.Code,
            [new M68kRelocation(offset, "bad")], input.Symbols));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void DuplicateOrOverlappingRelocationsAreRejected(int secondOffset)
    {
        var input = Input();
        Assert.Throws<InvalidDataException>(() => MuiLibraryPackager.Build(input.Code,
            [new M68kRelocation(0, "first"), new M68kRelocation(secondOffset, "second")], input.Symbols));
    }

    [Theory]
    [InlineData(128u)]
    [InlineData(uint.MaxValue)]
    public void RelocationsMayNotTargetOutsideTheLinkedImage(uint target)
    {
        var input = Input();
        BinaryPrimitives.WriteUInt32BigEndian(input.Code, target);
        Assert.Throws<InvalidDataException>(() => MuiLibraryPackager.Build(input.Code,
            input.Relocations, input.Symbols));
    }

    [Theory]
    [InlineData(17u)]
    [InlineData(128u)]
    [InlineData(uint.MaxValue)]
    public void AbiExportMustBeAlignedAndInsideCompiledCode(uint address)
    {
        var input = Input();
        input.Symbols[0] = input.Symbols[0] with { Address = address };
        Assert.Throws<InvalidDataException>(() => MuiLibraryPackager.Build(input.Code,
            input.Relocations, input.Symbols));
    }

    [Fact]
    public void MissingExportDoesNotSilentlyUseManagedMethodSymbol()
    {
        var input = Input();
        input.Symbols[0] = input.Symbols[0] with { Name = "CopperOS.MuiMaster.MuiNativeLibraryEntrypoints::Init" };
        Assert.Throws<InvalidDataException>(() => MuiLibraryPackager.Build(input.Code,
            input.Relocations, input.Symbols));
    }

    [Fact]
    public void AmbiguousExportIsRejected()
    {
        var input = Input();
        Assert.Throws<InvalidDataException>(() => MuiLibraryPackager.Build(input.Code,
            input.Relocations, [.. input.Symbols, input.Symbols[0]]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(63)]
    public void TruncatedOrOddImageIsRejected(int length)
    {
        var input = Input();
        Assert.Throws<InvalidDataException>(() => MuiLibraryPackager.Build(new byte[length], [], input.Symbols));
    }

    private static (byte[] Code, M68kRelocation[] Relocations, M68kSymbol[] Symbols) Input()
    {
        // Keep the synthetic linked image large enough for every exported ABI
        // symbol.  The packager's production export list intentionally grows
        // as public vectors become native; test symbols must remain inside the
        // same bounded code image.
        var code = new byte[128];
        for (var index = 0; index < code.Length; index += 2)
        {
            code[index] = 0x4E;
            code[index + 1] = 0x71; // NOP
        }
        BinaryPrimitives.WriteUInt32BigEndian(code, 16);
        BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan(6), 0);
        return (code, [new(0, "code-label"), new(6, "entry-label")],
            MuiLibraryPackager.ExportNames.Select((name, index) =>
                new M68kSymbol(name, (uint)(16 + 2 * index), 0)).ToArray());
    }

    private static LoadedMemory ReadHunk(byte[] file, uint load, out List<int> sites)
    {
        var cursor = 0;
        uint Long()
        {
            Assert.InRange(cursor, 0, file.Length - 4);
            var value = BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(cursor, 4));
            cursor += 4;
            return value;
        }
        Assert.Equal(0x3F3u, Long());
        Assert.Equal(0u, Long());
        Assert.Equal(1u, Long());
        Assert.Equal(0u, Long());
        Assert.Equal(0u, Long());
        var allocationLongs = Long();
        Assert.Equal(0x3E9u, Long());
        Assert.Equal(allocationLongs, Long());
        var codeBytes = checked((int)allocationLongs * 4);
        var code = file.AsSpan(cursor, codeBytes).ToArray();
        cursor += codeBytes;
        Assert.Equal(0x3ECu, Long());
        sites = [];
        for (var count = Long(); count != 0; count = Long())
        {
            Assert.Equal(0u, Long());
            for (uint index = 0; index < count; index++)
            {
                var offset = checked((int)Long());
                Assert.InRange(offset, 0, code.Length - 4);
                Assert.Equal(0, offset & 1);
                Assert.DoesNotContain(offset, sites);
                var target = BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan(offset, 4));
                Assert.True(target < code.Length);
                BinaryPrimitives.WriteUInt32BigEndian(code.AsSpan(offset, 4), checked(target + load));
                sites.Add(offset);
            }
        }
        Assert.Equal(0x3F2u, Long());
        Assert.Equal(file.Length, cursor);
        return new LoadedMemory(code, load);
    }

    private readonly struct LoadedMemory(byte[] code, uint load) : IAmigaGuestMemory
    {
        private Span<byte> Slice(APTR address, int offset, int count) =>
            code.AsSpan(checked((int)(address.Raw - load) + offset), count);
        public string Text(APTR address)
        {
            var bytes = Slice(address, 0, checked(code.Length - (int)(address.Raw - load)));
            return Encoding.ASCII.GetString(bytes[..bytes.IndexOf((byte)0)]);
        }
        public bool IsMapped(APTR address, uint byteSize) => address.Raw >= load && byteSize != 0 &&
            (ulong)address.Raw - load + byteSize <= (uint)code.Length;
        public byte ReadUInt8(APTR address, int offset = 0) => Slice(address, offset, 1)[0];
        public ushort ReadUInt16(APTR address, int offset = 0) => BinaryPrimitives.ReadUInt16BigEndian(Slice(address, offset, 2));
        public uint ReadUInt32(APTR address, int offset = 0) => BinaryPrimitives.ReadUInt32BigEndian(Slice(address, offset, 4));
        public void WriteUInt8(APTR address, int offset, byte value) => throw new NotSupportedException();
        public void WriteUInt16(APTR address, int offset, ushort value) => throw new NotSupportedException();
        public void WriteUInt32(APTR address, int offset, uint value) => throw new NotSupportedException();
        public void Clear(APTR address, uint byteCount) => throw new NotSupportedException();
        public void Copy(APTR source, APTR destination, uint byteCount) => throw new NotSupportedException();
    }
}
