using System.Buffers.Binary;
using System.Text;
using Amiga;
using Copper68k;

namespace CopperOS.Commands.Exe2ArcFrontendNativeExecution;

internal sealed class FrontendBus : IM68kBus
{
    private readonly Dictionary<uint, Action<M68kCpuState>> gateways = new();
    private readonly Dictionary<uint, Allocation> allocations = new();
    private uint nextAllocation = 0x60000;
    private uint imageStart;
    private uint imageLength;
    private byte[] imageSnapshot = [];

    public byte[] Memory { get; } = new byte[0x1000000];
    public FrontendInvocation? Current { get; set; }
    public int NativeReads { get; private set; }
    public int NativeWrites { get; private set; }

    public void LoadAndProtect(uint address, byte[] code)
    {
        Require(imageLength == 0, "The resident image was loaded more than once.");
        code.CopyTo(Memory.AsSpan(Offset(address, code.Length)));
        imageStart = address;
        imageLength = (uint)code.Length;
        imageSnapshot = (byte[])code.Clone();
        Long(4, 0x4000);
        Long(0x4000 + (uint)ExecLayout.ExecBase.ThisTask, 0x30000);
        Long(0x30000 + (uint)DosLayout.Process.CommandLineInterface, 1);
    }

    public void RegisterGateway(uint address, Action<M68kCpuState> handler)
    {
        gateways.Add(address, handler);
        Word(address, 0xff00);
        Long(address + 2, 1);
    }

    public bool HasHostGateway(uint address) => gateways.ContainsKey(address);

    public bool TryInvokeHostGateway(uint instructionProgramCounter,
        uint token, M68kCpuState state)
    {
        if (!gateways.TryGetValue(instructionProgramCounter, out var handler))
            return false;
        Require(token == 1, "Unexpected host gateway token.");
        handler(state);
        return true;
    }

    public byte ReadByte(uint address, ref long cycle,
        M68kBusAccessKind accessKind)
    {
        CheckRead(address, 1, accessKind);
        NativeReads++;
        return Memory[Offset(address, 1)];
    }

    public ushort ReadWord(uint address, ref long cycle,
        M68kBusAccessKind accessKind)
    {
        CheckRead(address, 2, accessKind);
        NativeReads++;
        return Word(address);
    }

    public uint ReadLong(uint address, ref long cycle,
        M68kBusAccessKind accessKind)
    {
        CheckRead(address, 4, accessKind);
        NativeReads++;
        return Long(address);
    }

    public void WriteByte(uint address, byte value, ref long cycle,
        M68kBusAccessKind accessKind)
    {
        CheckWrite(address, 1);
        Memory[Offset(address, 1)] = value;
    }

    public void WriteWord(uint address, ushort value, ref long cycle,
        M68kBusAccessKind accessKind)
    {
        CheckWrite(address, 2);
        Word(address, value);
    }

    public void WriteLong(uint address, uint value, ref long cycle,
        M68kBusAccessKind accessKind)
    {
        CheckWrite(address, 4);
        Long(address, value);
    }

    public void ResetExternalDevices(long cycle) =>
        throw new InvalidOperationException("The command executed RESET.");

    public ushort Word(uint address) =>
        BinaryPrimitives.ReadUInt16BigEndian(Memory.AsSpan(Offset(address, 2), 2));

    public void Word(uint address, ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(Memory.AsSpan(Offset(address, 2), 2), value);

    public uint Long(uint address) =>
        BinaryPrimitives.ReadUInt32BigEndian(Memory.AsSpan(Offset(address, 4), 4));

    public void Long(uint address, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(Memory.AsSpan(Offset(address, 4), 4), value);

    public string CString(uint address)
    {
        var start = Offset(address, 1);
        var end = start;
        while (end < Memory.Length && Memory[end] != 0 && end - start < 4096)
            end++;
        Require(end < Memory.Length && Memory[end] == 0,
            "Unterminated guest C string.");
        return Encoding.Latin1.GetString(Memory, start, end - start);
    }

    public uint Allocate(FrontendInvocation owner, uint bytes, string kind)
    {
        Require(bytes > 0 && bytes < 0x1000000,
            "Unexpected guest allocation size.");
        var address = checked((nextAllocation + 23) & ~7u);
        nextAllocation = checked(address + bytes + 16);
        Require(nextAllocation < 0xf00000, "Fixture allocation arena exhausted.");
        Memory.AsSpan((int)address - 16, checked((int)bytes + 32)).Fill(0xa7);
        Memory.AsSpan((int)address, checked((int)bytes)).Clear();
        allocations.Add(address, new(owner, address, bytes, kind));
        return address;
    }

    public void Release(FrontendInvocation owner, uint address, string kind,
        uint? bytes = null)
    {
        if (!allocations.TryGetValue(address, out var allocation) ||
            !ReferenceEquals(allocation.Owner, owner) || allocation.Kind != kind)
            throw new InvalidOperationException(
                $"Invalid {kind} release at ${address:X8}.");
        Require(bytes is null || bytes == allocation.Size,
            $"Wrong {kind} release size.");
        Require(Memory.AsSpan((int)address - 16, 16).IndexOfAnyExcept((byte)0xa7) < 0 &&
            Memory.AsSpan((int)(address + allocation.Size), 16)
                .IndexOfAnyExcept((byte)0xa7) < 0,
            $"{kind} allocation guard changed.");
        allocations.Remove(address);
        Memory.AsSpan((int)address, checked((int)allocation.Size)).Fill(0xdd);
    }

    public void AssertReleased(FrontendInvocation owner) => Require(
        allocations.Values.All(a => !ReferenceEquals(a.Owner, owner)),
        "The frontend leaked invocation-owned memory.");

    public void AssertImageUnchanged() => Require(
        Memory.AsSpan((int)imageStart, checked((int)imageLength))
            .SequenceEqual(imageSnapshot),
        "The resident image or its literals changed.");

    public void AssertProgramCounter(uint pc) => Require(
        Contains(imageStart, imageLength, pc, 2) ||
        gateways.Keys.Any(g => Contains(g, 6, pc, 2)) || pc == 0x2000,
        $"Native PC escaped the image or registered vectors at ${pc:X8}.");

    private void CheckRead(uint address, int bytes, M68kBusAccessKind kind)
    {
        _ = Offset(address, bytes);
        var instruction = kind == M68kBusAccessKind.CpuInstructionFetch;
        var image = Contains(imageStart, imageLength, address, bytes);
        var vector = gateways.Keys.Any(g => Contains(g, instruction ? 10u : 6u,
            address, bytes));
        var caller = instruction && Contains(0x2000, 8, address, bytes);
        var shared = Contains(4, 4, address, bytes) ||
            Contains(0x4000, 0x200, address, bytes);
        var process = Contains(0x30000, 0x800, address, bytes);
        var stack = Current is not null && Contains(Current.StackTop - Current.StackBytes,
            Current.StackBytes, address, bytes);
        var owned = Current is not null && allocations.Values.Any(a =>
            ReferenceEquals(a.Owner, Current) && Contains(a.Address, a.Size,
                address, bytes));
        var staticInput = Contains(0x35000, 0x1000, address, bytes);
        Require(image || vector || caller || shared || process || stack || owned ||
            staticInput, $"Read outside fixture-owned storage at ${address:X8}.");
    }

    private void CheckWrite(uint address, int bytes)
    {
        _ = Offset(address, bytes);
        Require(!Contains(imageStart, imageLength, address, bytes),
            "Native code wrote the resident image.");
        var current = Current ?? throw new InvalidOperationException(
            "Native write without an active invocation.");
        var stack = Contains(current.StackTop - current.StackBytes, current.StackBytes,
            address, bytes);
        var process = Contains(0x30000, 0x800, address, bytes);
        var owned = allocations.Values.Any(a => ReferenceEquals(a.Owner, current) &&
            a.Kind != "RDArgs" && Contains(a.Address, a.Size, address, bytes));
        Require(stack || process || owned,
            $"Native write outside invocation storage at ${address:X8}.");
        NativeWrites++;
    }

    private int Offset(uint address, int bytes)
    {
        Require(bytes >= 0 && address <= Memory.Length &&
            bytes <= Memory.Length - (long)address,
            $"Unmapped guest span ${address:X8}+{bytes}.");
        return checked((int)address);
    }

    private static bool Contains(uint start, uint length, uint address, int bytes) =>
        bytes >= 0 && address >= start &&
        (ulong)address + (uint)bytes <= (ulong)start + length;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal sealed record Allocation(FrontendInvocation Owner, uint Address,
        uint Size, string Kind);
}
