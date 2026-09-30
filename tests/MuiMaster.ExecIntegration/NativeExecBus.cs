using System.Buffers.Binary;
using Copper68k;
using CopperSharp.Compiler;
using Amiga;

namespace CopperOS.MuiMaster.ExecIntegration;

internal sealed class NativeExecBus : IM68kBus
{
    private readonly byte[] _memory = new byte[0x01000000];
    internal int HostTrapAttempts { get; private set; }

    internal void LoadBytes(byte[] bytes, uint load) =>
        bytes.CopyTo(_memory.AsSpan(Offset(load, bytes.Length)));

    internal void Load(M68kCompilationResult image, uint load)
    {
        image.Code.CopyTo(_memory.AsSpan(Offset(load, image.Code.Length)));
        foreach (var relocation in image.Relocations)
        {
            if (relocation.Offset < 0 || relocation.Offset > image.Code.Length - 4)
                throw new InvalidDataException("Relocation is outside its image.");
            var address = checked(load + (uint)relocation.Offset);
            WriteLong(address, checked(ReadLong(address) + load));
        }
    }

    internal uint ReadLong(uint address) =>
        BinaryPrimitives.ReadUInt32BigEndian(_memory.AsSpan(Offset(address, 4), 4));
    internal void WriteLong(uint address, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(_memory.AsSpan(Offset(address, 4), 4), value);

    internal string LibraryDiagnostic()
    {
        var head = ReadLong(ExecBootstrap.SysBase + ExecLayout.ExecBase.LibraryList);
        var name = ReadLong(head + ExecLayout.Node.Name);
        return $"library-head=${head:X8}, name={TextAt(name)}";
    }

    internal string TextAt(uint name)
    {
        if (name >= _memory.Length) return $"invalid-${name:X8}";
        var count = 0;
        while (count < 80 && name + count < _memory.Length && _memory[name + count] != 0) count++;
        return System.Text.Encoding.ASCII.GetString(_memory, (int)name, count);
    }
    public byte ReadByte(uint address, ref long cycle, M68kBusAccessKind accessKind) =>
        _memory[Offset(address, 1)];
    public ushort ReadWord(uint address, ref long cycle, M68kBusAccessKind accessKind) =>
        BinaryPrimitives.ReadUInt16BigEndian(_memory.AsSpan(Offset(address, 2), 2));
    public uint ReadLong(uint address, ref long cycle, M68kBusAccessKind accessKind) =>
        ReadLong(address);
    public void WriteByte(uint address, byte value, ref long cycle, M68kBusAccessKind accessKind) =>
        _memory[Offset(address, 1)] = value;
    public void WriteWord(uint address, ushort value, ref long cycle, M68kBusAccessKind accessKind) =>
        BinaryPrimitives.WriteUInt16BigEndian(_memory.AsSpan(Offset(address, 2), 2), value);
    public void WriteLong(uint address, uint value, ref long cycle, M68kBusAccessKind accessKind) =>
        WriteLong(address, value);
    public bool HasHostTrapStub(uint address) => false;
    public bool TryInvokeHostTrap(uint instructionProgramCounter, ushort trapId, M68kCpuState state)
    {
        HostTrapAttempts++;
        return false;
    }
    public void ResetExternalDevices(long cycle) { }

    private int Offset(uint address, int count)
    {
        if (count < 0 || address > (uint)_memory.Length ||
            (uint)count > (uint)_memory.Length - address)
            throw new InvalidDataException($"Guest range ${address:X8}+{count} exceeds the integration bus.");
        return (int)address;
    }
}
