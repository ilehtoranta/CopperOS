using System.Buffers.Binary;
using System.Text;
using Copper68k;

namespace CopperOS.Commands.NativeExecution;

internal sealed class CommandTestBus : IM68kBus
{
    private readonly Dictionary<uint, Action<M68kCpuState>> gateways = new();
    private readonly Dictionary<uint, Allocation> allocations = new();
    private uint nextAllocation = 0x60000;
    private uint protectedStart;
    private uint protectedLength;
    private byte[] protectedSnapshot = [];

    public byte[] Memory { get; } = new byte[0x200000];
    public Invocation? Current { get; set; }
    public int NativeWrites { get; private set; }
    public int NativeReads { get; private set; }

    public void LoadAndProtect(uint address, byte[] code)
    {
        code.CopyTo(Memory.AsSpan(Offset(address, code.Length)));
        protectedStart = address;
        protectedLength = (uint)code.Length;
        protectedSnapshot = (byte[])code.Clone();
    }

    public void AssertImageUnchanged()
    {
        Require(Memory.AsSpan((int)protectedStart, (int)protectedLength).SequenceEqual(protectedSnapshot),
            "Shared code/constants changed.");
    }

    public void RegisterGateway(uint address, Action<M68kCpuState> handler)
    {
        gateways.Add(address, handler);
        Word(address, 0xff00);
        Long(address + 2, 1);
    }

    public bool HasHostGateway(uint address) => gateways.ContainsKey(address);

    public bool TryInvokeHostGateway(uint instructionProgramCounter, uint token, M68kCpuState state)
    {
        if (!gateways.TryGetValue(instructionProgramCounter, out var handler)) return false;
        Require(token == 1, "Unexpected host gateway token.");
        handler(state);
        return true;
    }

    public byte ReadByte(uint address, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeRead(address, 1, accessKind);
        return Memory[Offset(address, 1)];
    }
    public ushort ReadWord(uint address, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeRead(address, 2, accessKind);
        return Word(address);
    }
    public uint ReadLong(uint address, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeRead(address, 4, accessKind);
        return Long(address);
    }
    public void WriteByte(uint address, byte value, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeWrite(address, 1);
        Memory[Offset(address, 1)] = value;
    }
    public void WriteWord(uint address, ushort value, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeWrite(address, 2);
        Word(address, value);
    }
    public void WriteLong(uint address, uint value, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckNativeWrite(address, 4);
        Long(address, value);
    }
    public void ResetExternalDevices(long cycle) => throw new InvalidOperationException("Command executed RESET.");

    public ushort Word(uint address) => BinaryPrimitives.ReadUInt16BigEndian(Memory.AsSpan(Offset(address, 2), 2));
    public void Word(uint address, ushort value) => BinaryPrimitives.WriteUInt16BigEndian(Memory.AsSpan(Offset(address, 2), 2), value);
    public uint Long(uint address) => BinaryPrimitives.ReadUInt32BigEndian(Memory.AsSpan(Offset(address, 4), 4));
    public void Long(uint address, uint value) => BinaryPrimitives.WriteUInt32BigEndian(Memory.AsSpan(Offset(address, 4), 4), value);
    public string CString(uint address)
    {
        var start = Offset(address, 1);
        var end = start;
        while (end < Memory.Length && Memory[end] != 0 && end - start < 4096) end++;
        Require(end < Memory.Length && Memory[end] == 0, "Unterminated C string.");
        return Encoding.Latin1.GetString(Memory, start, end - start);
    }

    public uint Allocate(Invocation owner, uint size, string kind, bool clear)
    {
        Require(size > 0 && size < 0x10000, "Unexpected fixture allocation size.");
        var address = checked((nextAllocation + 23) & ~7u);
        nextAllocation = checked(address + size + 16);
        Require(nextAllocation < 0xf0000, "Fixture allocation arena exhausted.");
        Memory.AsSpan((int)address - 16, checked((int)size + 32)).Fill(0xa7);
        Memory.AsSpan((int)address, (int)size).Fill(clear ? (byte)0 : (byte)0xcd);
        allocations.Add(address, new Allocation(owner, address, size, kind));
        return address;
    }

    public Allocation OwnedAllocation(Invocation owner, uint address, string kind)
    {
        Require(allocations.TryGetValue(address, out var allocation) &&
            ReferenceEquals(allocation.Owner, owner) && allocation.Kind == kind,
            $"Invalid {kind} ownership at ${address:X8}.");
        return allocation!;
    }

    public void Release(Invocation owner, uint address, string kind, uint? size = null)
    {
        var allocation = OwnedAllocation(owner, address, kind);
        Require(size is null || size == allocation.Size, "FreeMem size differs from AllocMem.");
        Require(Memory.AsSpan((int)address - 16, 16).IndexOfAnyExcept((byte)0xa7) < 0 &&
            Memory.AsSpan((int)(address + allocation.Size), 16).IndexOfAnyExcept((byte)0xa7) < 0,
            "Allocation guard changed.");
        allocations.Remove(address);
        Memory.AsSpan((int)address, (int)allocation.Size).Fill(0xdd);
    }

    public void AssertReleased(Invocation owner) => Require(!allocations.Values.Any(a => ReferenceEquals(a.Owner, owner)),
        "Invocation leaked guest memory or RDArgs.");

    private void CheckNativeWrite(uint address, int size)
    {
        _ = Offset(address, size);
        Require((ulong)address + (uint)size <= protectedStart || address >= protectedStart + protectedLength,
            $"Native write to shared image at ${address:X8}.");
        var owner = Current ?? throw new InvalidOperationException("No active process for native write.");
        var stackWrite = address >= owner.StackTop - owner.StackBytes && (ulong)address + (uint)size <= owner.StackTop;
        var ioOutputWrite = owner.NativeIo?.ContainsOutput(address, size) == true;
        Require(stackWrite || ioOutputWrite || owner.NativeIo is null && allocations.Values.Any(a => ReferenceEquals(a.Owner, owner) &&
            address >= a.Address && (ulong)address + (uint)size <= (ulong)a.Address + a.Size),
            $"Native write outside invocation-owned storage at ${address:X8}.");
        if (stackWrite) owner.LowestStackWrite = Math.Min(owner.LowestStackWrite, address);
        if (ioOutputWrite) owner.NativeIo!.RecordOutputWrite(address, size);
        NativeWrites++;
    }

    private void CheckNativeRead(uint address, int size, M68kBusAccessKind accessKind)
    {
        _ = Offset(address, size);
        if (Current is { NativeIo: { } io } ioOwner)
        {
            var ownStack = address >= ioOwner.StackTop - ioOwner.StackBytes &&
                (ulong)address + (uint)size <= ioOwner.StackTop;
            var ownProcess = address >= ioOwner.Process &&
                (ulong)address + (uint)size <= (ulong)ioOwner.Process + 0x400;
            var imageRead = address >= protectedStart &&
                (ulong)address + (uint)size <= (ulong)protectedStart + protectedLength;
            var execPointerRead = address >= 4 && (ulong)address + (uint)size <= 8;
            var instructionFetch = accessKind == M68kBusAccessKind.CpuInstructionFetch;
            // The six-byte host-call stub may prefetch two following words.
            // RTS also prefetches its caller before the loop sees the sentinel.
            // These extra bytes are fetch-only and never writable storage.
            var gatewayRead = gateways.Keys.Any(start => address >= start &&
                (ulong)address + (uint)size <= (ulong)start + (instructionFetch ? 10u : 6u));
            var callerPrefetch = instructionFetch && address >= 0x2000 &&
                (ulong)address + (uint)size <= 0x2008;
            Require(ownStack || ownProcess || imageRead || execPointerRead || gatewayRead || callerPrefetch ||
                io.ContainsReadableControl(address, size) || io.ContainsReadablePayload(address, size),
                $"Native I/O read outside the current task's readable storage at ${address:X8}.");
        }
        if (address < nextAllocation && (ulong)address + (uint)size > 0x60000)
        {
            var owner = Current ?? throw new InvalidOperationException("No active process for native allocation read.");
            Require(allocations.Values.Any(a => ReferenceEquals(a.Owner, owner) &&
                address >= a.Address && (ulong)address + (uint)size <= (ulong)a.Address + a.Size),
                $"Native read of freed, guarded, or another invocation's allocation at ${address:X8}.");
        }
        NativeReads++;
    }

    private int Offset(uint address, int size)
    {
        if (size < 0 || address > Memory.Length || size > Memory.Length - (long)address)
            throw new InvalidOperationException($"Unmapped bus address ${address:X8} size {size}.");
        return (int)address;
    }

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal sealed record Allocation(Invocation Owner, uint Address, uint Size, string Kind);
}
