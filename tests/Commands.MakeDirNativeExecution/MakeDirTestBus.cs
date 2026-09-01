using System.Buffers.Binary;
using System.Text;
using Amiga;
using Copper68k;

namespace CopperOS.Commands.MakeDirNativeExecution;

internal sealed class MakeDirTestBus : IM68kBus
{
    private const uint ArenaStart = 0x60000;
    private const uint ArenaEnd = 0xf0000;
    private readonly Dictionary<uint, Action<M68kCpuState>> gateways = new();
    private readonly List<Allocation> allocations = [];
    private uint nextAllocation = ArenaStart;
    private uint imageStart;
    private byte[] imageSnapshot = [];

    public byte[] Memory { get; } = new byte[0x200000];
    public MakeDirInvocation? Current { get; private set; }
    public int NativeWrites { get; private set; }
    public int NativeReads { get; private set; }
    public int GatewayReads { get; private set; }
    public int ImageLoads { get; private set; }

    public void LoadAndProtect(uint address, byte[] code)
    {
        Require(ImageLoads == 0 && code.Length > 0, "Each side must load exactly one shared image.");
        code.CopyTo(Memory.AsSpan(Offset(address, code.Length)));
        imageStart = address;
        imageSnapshot = (byte[])code.Clone();
        ImageLoads++;
    }

    public void Activate(MakeDirInvocation? invocation)
    {
        Current = invocation;
        if (invocation is not null)
            Long(MakeDirFixture.ExecBase + (uint)ExecLayout.ExecBase.ThisTask, invocation.Process);
    }

    public int IoError(MakeDirInvocation owner) =>
        unchecked((int)Long(owner.Process + (uint)DosLayout.Process.Result2));

    public void SetIoError(MakeDirInvocation owner, int value) =>
        Long(owner.Process + (uint)DosLayout.Process.Result2, unchecked((uint)value));

    public void AssertImageUnchanged() => Require(
        Memory.AsSpan((int)imageStart, imageSnapshot.Length).SequenceEqual(imageSnapshot),
        "Shared executable or literal bytes changed.");

    public void AssertProgramCounter(uint pc) => Require(
        Contains(imageStart, (uint)imageSnapshot.Length, pc, 2) || gateways.ContainsKey(pc),
        $"Native execution escaped the image or registered public vectors at ${pc:X8}.");

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
        Require(token == 1, "Unexpected vector gateway token.");
        handler(state);
        return true;
    }

    public byte ReadByte(uint address, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckRead(address, 1, accessKind);
        NativeReads++;
        return Memory[Offset(address, 1)];
    }
    public ushort ReadWord(uint address, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckRead(address, 2, accessKind);
        NativeReads++;
        return Word(address);
    }
    public uint ReadLong(uint address, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckRead(address, 4, accessKind);
        NativeReads++;
        return Long(address);
    }
    public void WriteByte(uint address, byte value, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckWrite(address, 1);
        Memory[Offset(address, 1)] = value;
    }
    public void WriteWord(uint address, ushort value, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckWrite(address, 2);
        Word(address, value);
    }
    public void WriteLong(uint address, uint value, ref long cycle, M68kBusAccessKind accessKind)
    {
        CheckWrite(address, 4);
        Long(address, value);
    }
    public void ResetExternalDevices(long cycle) => throw new InvalidOperationException("Command executed RESET.");

    public ushort Word(uint address) => BinaryPrimitives.ReadUInt16BigEndian(Memory.AsSpan(Offset(address, 2), 2));
    public void Word(uint address, ushort value) => BinaryPrimitives.WriteUInt16BigEndian(Memory.AsSpan(Offset(address, 2), 2), value);
    public uint Long(uint address) => BinaryPrimitives.ReadUInt32BigEndian(Memory.AsSpan(Offset(address, 4), 4));
    public void Long(uint address, uint value) => BinaryPrimitives.WriteUInt32BigEndian(Memory.AsSpan(Offset(address, 4), 4), value);

    public uint GatewayLong(uint address)
    {
        CheckRead(address, 4);
        GatewayReads++;
        return Long(address);
    }

    public byte[] GatewayCString(uint address)
    {
        Require(address != 0, "Unexpected null C string.");
        var bytes = new List<byte>();
        for (var index = 0u; index < 8192; index++)
        {
            var next = checked(address + index);
            CheckRead(next, 1);
            GatewayReads++;
            var value = Memory[Offset(next, 1)];
            if (value == 0) return bytes.ToArray();
            bytes.Add(value);
        }
        throw new InvalidOperationException("Unterminated fixture C string.");
    }

    public string GatewayText(uint address) => Encoding.Latin1.GetString(GatewayCString(address));

    public uint Allocate(MakeDirInvocation owner, uint bytes, string kind, bool nativeWritable)
    {
        Require(bytes > 0 && bytes < 0x10000, "Unexpected fixture allocation size.");
        var address = checked((nextAllocation + 23) & ~7u);
        nextAllocation = checked(address + bytes + 16);
        Require(nextAllocation <= ArenaEnd, "Fixture arena exhausted.");
        Memory.AsSpan((int)address - 16, checked((int)bytes + 32)).Fill(0xa7);
        Memory.AsSpan((int)address, (int)bytes).Clear();
        allocations.Add(new(owner, address, bytes, kind, nativeWritable));
        return address;
    }

    public Allocation OwnedAllocation(MakeDirInvocation owner, uint address, string kind)
    {
        var allocation = allocations.SingleOrDefault(a => a.Live && a.Address == address);
        Require(allocation is not null && ReferenceEquals(allocation.Owner, owner) && allocation.Kind == kind,
            $"Foreign, dead, or invalid {kind} allocation at ${address:X8}.");
        return allocation!;
    }

    public void Seal(MakeDirInvocation owner, uint address, string kind)
    {
        var allocation = OwnedAllocation(owner, address, kind);
        allocation.NativeWritable = false;
        allocation.Snapshot = Memory.AsSpan((int)address, (int)allocation.Size).ToArray();
    }

    public void Release(MakeDirInvocation owner, uint address, string kind, uint? bytes = null)
    {
        var allocation = OwnedAllocation(owner, address, kind);
        Require(bytes is null || bytes == allocation.Size, "FreeMem size differs from owned AllocMem.");
        Require(Memory.AsSpan((int)address - 16, 16).IndexOfAnyExcept((byte)0xa7) < 0 &&
            Memory.AsSpan((int)(address + allocation.Size), 16).IndexOfAnyExcept((byte)0xa7) < 0,
            "Guest allocation guard changed.");
        if (allocation.Snapshot is { } snapshot)
            Require(Memory.AsSpan((int)address, snapshot.Length).SequenceEqual(snapshot),
                "Native code modified borrowed argument result/vector/string bytes.");
        allocation.Live = false;
        Memory.AsSpan((int)address, (int)allocation.Size).Fill(0xdd);
    }

    public void AssertReleased(MakeDirInvocation owner) => Require(
        !allocations.Any(a => a.Live && ReferenceEquals(a.Owner, owner)), "Invocation leaked guest memory.");

    public bool IsOwnStack(MakeDirInvocation owner, uint address, int bytes) =>
        Contains(owner.StackTop - owner.StackBytes, owner.StackBytes, address, bytes);

    private void CheckRead(uint address, int bytes, M68kBusAccessKind? accessKind = null)
    {
        _ = Offset(address, bytes);
        var owner = Current ?? throw new InvalidOperationException("Guest read without active invocation.");
        var owned = IsOwnStack(owner, address, bytes) || Contains(owner.Process, 0x400, address, bytes) ||
            Contains(owner.Arguments, 0x400, address, bytes) ||
            allocations.Any(a => a.Live && ReferenceEquals(a.Owner, owner) && Contains(a.Address, a.Size, address, bytes));
        var shared = Contains(imageStart, (uint)imageSnapshot.Length, address, bytes) || Contains(4, 4, address, bytes) ||
            Contains(MakeDirFixture.ExecBase, 0x200, address, bytes) ||
            gateways.Keys.Any(gateway => Contains(gateway, 6, address, bytes));
        // RTS can prefetch its caller before the execution loop observes its
        // return PC. Permit those fetches only; neither data reads nor execution
        // of this sentinel window are allowed by the fixture.
        var callerPrefetch = accessKind == M68kBusAccessKind.CpuInstructionFetch &&
            Contains(MakeDirFixture.ReturnAddress, 8, address, bytes);
        var gatewayPrefetch = accessKind == M68kBusAccessKind.CpuInstructionFetch &&
            gateways.Keys.Any(gateway => Contains(gateway, 10, address, bytes));
        Require(owned || shared || callerPrefetch || gatewayPrefetch,
            $"Read of dead, guarded, foreign, or unmapped invocation storage at ${address:X8}.");
    }

    private void CheckWrite(uint address, int bytes)
    {
        _ = Offset(address, bytes);
        Require(!Overlaps(imageStart, (uint)imageSnapshot.Length, address, bytes), "Native write to shared image.");
        var owner = Current ?? throw new InvalidOperationException("Guest write without active invocation.");
        var stack = IsOwnStack(owner, address, bytes);
        var bootstrapError = owner.Definition.MissingDos && owner.Opens == 1 && owner.Closes == 0 &&
            Contains(owner.Process + (uint)DosLayout.Process.Result2, 4, address, bytes);
        var allocation = allocations.Any(a => a.Live && a.NativeWritable && ReferenceEquals(a.Owner, owner) &&
            Contains(a.Address, a.Size, address, bytes));
        Require(stack || bootstrapError || allocation, $"Native write outside owned storage at ${address:X8}.");
        if (stack) owner.LowestStackWrite = Math.Min(owner.LowestStackWrite, address);
        NativeWrites++;
    }

    private int Offset(uint address, int bytes)
    {
        if (bytes < 0 || address > Memory.Length || bytes > Memory.Length - (long)address)
            throw new InvalidOperationException($"Unmapped guest span ${address:X8}+{bytes}.");
        return (int)address;
    }

    private static bool Contains(uint start, uint length, uint address, int bytes) => bytes >= 0 &&
        address >= start && (ulong)address + (uint)bytes <= (ulong)start + length;
    private static bool Overlaps(uint start, uint length, uint address, int bytes) =>
        (ulong)address < (ulong)start + length && (ulong)address + (uint)bytes > start;

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal sealed class Allocation(MakeDirInvocation owner, uint address, uint size, string kind, bool nativeWritable)
    {
        public MakeDirInvocation Owner { get; } = owner;
        public uint Address { get; } = address;
        public uint Size { get; } = size;
        public string Kind { get; } = kind;
        public bool NativeWritable { get; set; } = nativeWritable;
        public bool Live { get; set; } = true;
        public byte[]? Snapshot { get; set; }
    }
}
