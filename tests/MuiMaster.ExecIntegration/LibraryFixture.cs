using Amiga;
using CopperSharp.Compiler;
using Roots = CopperOS.MuiMaster.NativeRoot.MuiLibraryIntegrationRoots;

namespace CopperOS.MuiMaster.ExecIntegration;

// Passive load-time data only. All library and allocator behavior executes as
// native code through the separately compiled images.
internal static class LibraryFixture
{
    internal static void Prepare(NativeExecBus bus, M68kCompilationResult library, uint load)
    {
        var memory = new FixtureMemory(bus);
        var resident = new Resident
        {
            MatchWord = 0x4AFC,
            MatchTag = APTR.FromPointer(Roots.ResidentAddress),
            EndSkip = APTR.FromPointer(Roots.IdAddress + 64),
            Flags = ResidentFlags.AutoInit,
            Version = 0,
            Type = (byte)NodeType.Library,
            Name = STRPTR.FromPointer(Roots.NameAddress),
            IdString = STRPTR.FromPointer(Roots.IdAddress),
            Init = APTR.FromPointer(Roots.AutoInitAddress),
        };
        ExecResidentCodec.Write(ref memory, resident.MatchTag, resident);
        var autoInit = new ResidentAutoInit
        {
            DataSize = Roots.LibraryPositiveBytes,
            FunctionTable = APTR.FromPointer(Roots.FunctionTableAddress),
            StructureTable = APTR.Null,
            InitFunction = Export(library, load, "init"),
        };
        ExecResidentAutoInitCodec.Write(ref memory, resident.Init, autoInit);
        var vectors = new string[MuiResidentMetadata.FunctionTableEntryCount];
        vectors[0] = "open";
        vectors[1] = "close";
        vectors[2] = "expunge";
        vectors[3] = "reserved";
        for (var index = MuiResidentMetadata.ManagementVectorCount;
            index < vectors.Length; index++)
        {
            var lvo = MuiResidentMetadata.FirstLvo +
                (index - MuiResidentMetadata.ManagementVectorCount) *
                -MuiResidentMetadata.VectorStride;
            vectors[index] = lvo switch
            {
                -30 => "new-object-a",
                -36 => "dispose-object",
                -120 => "make-object-a",
                -66 => "error",
                -72 => "set-error",
                -78 => "get-class",
                -84 => "free-class",
                -90 => "request-idcmp",
                -96 => "reject-idcmp",
                -108 => "create-custom-class",
                -114 => "delete-custom-class",
                _ => "unsupported",
            };
        }
        for (var index = 0; index < vectors.Length; index++)
            memory.WriteUInt32(autoInit.FunctionTable, index * 4,
                Export(library, load, vectors[index]).Raw);
        memory.WriteUInt32(autoInit.FunctionTable, vectors.Length * 4, uint.MaxValue);
        WriteText(ref memory, resident.Name.Address, "copperos-muimaster.library");
        WriteText(ref memory, resident.IdString.Address,
            "CopperOS MUI development lifecycle fixture 0.1");
    }

    private static APTR Export(M68kCompilationResult library, uint load, string suffix) =>
        APTR.FromPointer(checked(load + library.Symbols.Single(symbol =>
            symbol.Name == "copperos.mui.library." + suffix).Address));

    private static void WriteText(ref FixtureMemory memory, APTR address, string text)
    {
        for (var index = 0; index < text.Length; index++)
            memory.WriteUInt8(address, index, checked((byte)text[index]));
        memory.WriteUInt8(address, text.Length, 0);
    }

    private struct FixtureMemory(NativeExecBus bus) : IAmigaGuestMemory
    {
        public bool IsMapped(APTR address, uint byteSize) => byteSize != 0 &&
            address.Raw >= Roots.ResidentAddress &&
            (ulong)address.Raw + byteSize <= Roots.IdAddress + 64;

        public byte ReadUInt8(APTR address, int offset = 0)
        {
            var cycle = 0L;
            return bus.ReadByte(checked(address.Raw + (uint)offset), ref cycle, default);
        }

        public ushort ReadUInt16(APTR address, int offset = 0)
        {
            var cycle = 0L;
            return bus.ReadWord(checked(address.Raw + (uint)offset), ref cycle, default);
        }

        public uint ReadUInt32(APTR address, int offset = 0) =>
            bus.ReadLong(checked(address.Raw + (uint)offset));

        public void WriteUInt8(APTR address, int offset, byte value)
        {
            var cycle = 0L;
            bus.WriteByte(checked(address.Raw + (uint)offset), value, ref cycle, default);
        }

        public void WriteUInt16(APTR address, int offset, ushort value)
        {
            var cycle = 0L;
            bus.WriteWord(checked(address.Raw + (uint)offset), value, ref cycle, default);
        }

        public void WriteUInt32(APTR address, int offset, uint value) =>
            bus.WriteLong(checked(address.Raw + (uint)offset), value);

        public void Clear(APTR address, uint byteCount)
        {
            for (var index = 0; (uint)index < byteCount; index++) WriteUInt8(address, index, 0);
        }

        public void Copy(APTR source, APTR destination, uint byteCount)
        {
            for (var index = 0; (uint)index < byteCount; index++)
                WriteUInt8(destination, index, ReadUInt8(source, index));
        }
    }
}
