using System.Buffers.Binary;
using Amiga;

namespace CopperOS.MuiMaster.ExecIntegration;

// Independent, deliberately narrow disk-format loader for the builder's one
// CODE HUNK contract. Neither compiler Code nor compiler Relocations are used.
internal sealed record PackagedHunk(byte[] Image, byte[] Code, uint[] Relocations)
{
    internal static PackagedHunk Read(string path)
    {
        var image = File.ReadAllBytes(path);
        var cursor = 0;
        uint Take()
        {
            if (cursor > image.Length - 4) throw new InvalidDataException("Truncated HUNK word.");
            var value = BinaryPrimitives.ReadUInt32BigEndian(image.AsSpan(cursor, 4));
            cursor += 4;
            return value;
        }

        if (Take() != 1011 || Take() != 0 || Take() != 1 || Take() != 0 || Take() != 0)
            throw new InvalidDataException("Expected one unnamed loadable HUNK.");
        var words = Take();
        if (words == 0 || words > 0x40000 || Take() != 1001 || Take() != words)
            throw new InvalidDataException("Expected a bounded CODE HUNK matching its allocation size.");
        var length = checked((int)words * 4);
        if (length > image.Length - cursor) throw new InvalidDataException("Truncated CODE HUNK.");
        var code = image.AsSpan(cursor, length).ToArray();
        cursor += length;
        if (Take() != 1004) throw new InvalidDataException("Expected HUNK_RELOC32.");
        var relocations = new List<uint>();
        var seen = new HashSet<uint>();
        for (var count = Take(); count != 0; count = Take())
        {
            if (count > (uint)length / 4 || Take() != 0)
                throw new InvalidDataException("Invalid RELOC32 group or target HUNK.");
            for (uint index = 0; index < count; index++)
            {
                var offset = Take();
                if ((offset & 1) != 0 || offset > (uint)length - 4 ||
                    seen.Contains(offset) || (offset >= 2 && seen.Contains(offset - 2)) ||
                    seen.Contains(offset + 2))
                    throw new InvalidDataException("Overlapping, unaligned or out-of-range HUNK relocation.");
                seen.Add(offset);
                if (BinaryPrimitives.ReadUInt32BigEndian(code.AsSpan((int)offset, 4)) > (uint)length)
                    throw new InvalidDataException("Relocation target lies outside the CODE HUNK.");
                relocations.Add(offset);
            }
        }
        if (relocations.Count == 0 || Take() != 1010 || cursor != image.Length)
            throw new InvalidDataException("Expected final HUNK_END and no trailing data.");
        return new PackagedHunk(image, code, relocations.ToArray());
    }

    internal PackagedResident Install(NativeExecBus bus, uint load)
    {
        var relocated = Code.ToArray();
        foreach (var offset in Relocations)
        {
            var field = relocated.AsSpan((int)offset, 4);
            BinaryPrimitives.WriteUInt32BigEndian(field,
                checked(BinaryPrimitives.ReadUInt32BigEndian(field) + load));
        }
        bus.LoadBytes(relocated, load);
        var memory = new LoadedMemory(bus, load, checked((uint)Code.Length));
        PackagedResident? found = null;
        for (uint offset = 0; offset <= Code.Length - Resident.Size; offset += 2)
        {
            var address = APTR.FromPointer(load + offset);
            if (memory.ReadUInt16(address, 0) != 0x4AFC) continue;
            var resident = ExecResidentCodec.Read(ref memory, address);
            if (resident.MatchTag != address) continue;
            if (found is not null || resident.Flags != ResidentFlags.AutoInit ||
                resident.Type != (byte)NodeType.Library || resident.Version != 0 ||
                resident.EndSkip.Raw <= address.Raw || resident.EndSkip.Raw > load + Code.Length ||
                (resident.Init.Raw & 1) != 0 || !memory.IsMapped(resident.Init, ResidentAutoInit.Size) ||
                !memory.IsMapped(resident.Name.Address, 1) ||
                !memory.IsMapped(resident.IdString.Address, 1) ||
                bus.TextAt(resident.Name.Raw) != "copperos-muimaster.library")
                throw new InvalidDataException("Invalid or duplicate embedded development Resident.");
            var autoInit = ExecResidentAutoInitCodec.Read(ref memory, resident.Init);
            if (autoInit.DataSize != NativeRoot.MuiLibraryIntegrationRoots.LibraryPositiveBytes ||
                autoInit.StructureTable.IsNotNull || (autoInit.InitFunction.Raw & 1) != 0 ||
                !memory.IsMapped(autoInit.InitFunction, 2) || (autoInit.FunctionTable.Raw & 1) != 0 ||
                !memory.IsMapped(autoInit.FunctionTable,
                    (uint)MuiResidentMetadata.FunctionTableEntryCount * sizeof(uint) + sizeof(uint)))
                throw new InvalidDataException("Invalid embedded AUTOINIT table.");
            for (var index = 0; index < MuiResidentMetadata.FunctionTableEntryCount; index++)
            {
                var target = memory.ReadUInt32(autoInit.FunctionTable, index * 4);
                if ((target & 1) != 0 || !memory.IsMapped(APTR.FromPointer(target), 2))
                    throw new InvalidDataException("Embedded management vector is not image-local.");
            }
            if (memory.ReadUInt32(autoInit.FunctionTable,
                    MuiResidentMetadata.FunctionTableEntryCount * 4) != uint.MaxValue)
                throw new InvalidDataException("Embedded vector table lacks its terminator.");
            found = new PackagedResident(address.Raw, autoInit.InitFunction.Raw,
                memory.ReadUInt32(autoInit.FunctionTable, 0),
                autoInit.FunctionTable.Raw, Relocations.Length);
        }
        return found ?? throw new InvalidDataException("No embedded Resident was discovered.");
    }

    private struct LoadedMemory(NativeExecBus bus, uint start, uint length) : IAmigaGuestMemory
    {
        public bool IsMapped(APTR address, uint bytes) => bytes != 0 && address.Raw >= start &&
            (ulong)address.Raw + bytes <= (ulong)start + length;
        public byte ReadUInt8(APTR address, int offset)
        {
            var cycle = 0L;
            return bus.ReadByte(checked(address.Raw + (uint)offset), ref cycle, default);
        }
        public ushort ReadUInt16(APTR address, int offset)
        {
            var cycle = 0L;
            return bus.ReadWord(checked(address.Raw + (uint)offset), ref cycle, default);
        }
        public uint ReadUInt32(APTR address, int offset) => bus.ReadLong(checked(address.Raw + (uint)offset));
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
        public void WriteUInt32(APTR address, int offset, uint value) => bus.WriteLong(checked(address.Raw + (uint)offset), value);
        public void Clear(APTR address, uint bytes)
        {
            for (var index = 0; (uint)index < bytes; index++) WriteUInt8(address, index, 0);
        }
        public void Copy(APTR source, APTR destination, uint bytes)
        {
            for (var index = 0; (uint)index < bytes; index++) WriteUInt8(destination, index, ReadUInt8(source, index));
        }
    }
}

internal sealed record PackagedResident(uint Address, uint InitEntry,
    uint OpenEntry, uint FunctionTableAddress, int RelocationCount);
