using System.Buffers.Binary;
using System.Text;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.EvalNativeExecution;

internal sealed class NumericTestBus : IM68kBus
{
    private readonly byte[] memory = new byte[0x200000];
    private readonly byte[] highMemory = new byte[128];
    private readonly byte[] high24Memory = new byte[128];
    private readonly HunkImage image;
    private readonly uint loadAddress;
    public NumericInvocation? Current { get; set; }

    public NumericTestBus(HunkImage image, uint loadAddress)
    {
        this.image = image;
        this.loadAddress = loadAddress;
        image.Code.CopyTo(Span(loadAddress, image.Code.Length));
    }

    // No Exec/DOS gateways: every arithmetic and memory operation executes as
    // guest instructions. This fixture makes no claim about original OS startup.
    public bool HasHostGateway(uint address) => false;
    public bool TryInvokeHostGateway(uint pc, uint token, M68kCpuState state) => false;
    public void ResetExternalDevices(long cycle) => throw new InvalidOperationException("Component executed RESET.");

    public byte ReadByte(uint address, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 1, false); return Span(address, 1)[0]; }
    public ushort ReadWord(uint address, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 2, false); return BinaryPrimitives.ReadUInt16BigEndian(Span(address, 2)); }
    public uint ReadLong(uint address, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 4, false); return Long(address); }
    public void WriteByte(uint address, byte value, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 1, true); Span(address, 1)[0] = value; }
    public void WriteWord(uint address, ushort value, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 2, true); BinaryPrimitives.WriteUInt16BigEndian(Span(address, 2), value); }
    public void WriteLong(uint address, uint value, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 4, true); Long(address, value); }

    public void Initialize(NumericInvocation invocation)
    {
        var test = invocation.Definition;
        Span(invocation.Region, (int)invocation.RegionBytes).Fill(0xa5);
        Span(invocation.Control - 16, 68).Fill(0xc7);
        Span(invocation.StackTop - test.StackBytes - 16, (int)test.StackBytes + 32).Fill(0xb6);
        Long(invocation.Control, (uint)test.Format);
        Long(invocation.Control + 4, test.Flags);
        Long(invocation.Control + 8, (uint)(test.Value >> 32));
        Long(invocation.Control + 12, (uint)test.Value);
        Long(invocation.Control + 16, invocation.Destination);
        Long(invocation.Control + 20, test.Capacity);
        Long(invocation.Control + 24, test.BoundaryRegion.HasValue ? invocation.Region : invocation.DefaultDestination);
        Long(invocation.Control + 28, test.MappedBytes ?? (test.BoundaryRegion.HasValue ? 128u : 64u));
        Long(invocation.Control + 32, 0xdeadbeef);
        invocation.ControlSnapshot = Span(invocation.Control, 32).ToArray();
    }

    public void AssertOutput(NumericInvocation invocation)
    {
        var expected = Encoding.ASCII.GetBytes(invocation.Definition.Expected ?? "");
        Require(Long(invocation.Control + 32) == expected.Length, $"{invocation.Definition.Name}: wrong byte count.");
        Require(Span(invocation.Control, 32).SequenceEqual(invocation.ControlSnapshot), "Input fields changed.");
        Require(Span(invocation.Control - 16, 16).IndexOfAnyExcept((byte)0xc7) < 0 &&
            Span(invocation.Control + 36, 16).IndexOfAnyExcept((byte)0xc7) < 0, "Control guard changed.");
        var region = Span(invocation.Region, (int)invocation.RegionBytes);
        var outputOffset = expected.Length == 0 ? -1 : checked((int)(invocation.Destination - invocation.Region));
        for (var index = 0; index < region.Length; index++)
        {
            var wanted = index >= outputOffset && outputOffset >= 0 && index - outputOffset < expected.Length
                ? expected[index - outputOffset] : (byte)0xa5;
            Require(region[index] == wanted,
                $"{invocation.Definition.Name}: output/guard byte {index} is ${region[index]:X2}, expected ${wanted:X2}.");
        }
        Require(Span(invocation.StackTop - invocation.Definition.StackBytes - 16, 16).IndexOfAnyExcept((byte)0xb6) < 0 &&
            Span(invocation.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0, "Stack guard changed.");
    }

    public void AssertImageUnchanged() => Require(Span(loadAddress, image.Code.Length).SequenceEqual(image.Code),
        "Shared image was modified.");

    private void Check(uint address, uint bytes, bool write)
    {
        var owner = Current ?? throw new InvalidOperationException("No active numeric invocation.");
        var inStack = Contains(owner.StackTop - owner.Definition.StackBytes, owner.Definition.StackBytes, address, bytes);
        var inOutput = Contains(owner.Region, owner.RegionBytes, address, bytes);
        var inControl = Contains(owner.Control + (write ? 32u : 0u), write ? 4u : 36u, address, bytes);
        var inCode = !write && Contains(loadAddress, (uint)image.Code.Length, address, bytes);
        // RTS may prefetch its caller before the instruction loop sees the
        // return sentinel. This read-only window is never executed by the test.
        var inCallerPrefetch = !write && Contains(0x2000, 8, address, bytes);
        Require(inStack || inOutput || inControl || inCode || inCallerPrefetch,
            $"Unowned native {(write ? "write" : "read")} at ${address:X8} ({bytes} bytes).");
        if (write && inStack) owner.LowestStackWrite = Math.Min(owner.LowestStackWrite, address);
    }

    private static bool Contains(uint start, uint length, uint address, uint bytes) =>
        address >= start && (ulong)address + bytes <= (ulong)start + length;

    private Span<byte> Span(uint address, int bytes)
    {
        if (address >= 0xffffff80u && (ulong)address + (uint)bytes <= 0x100000000UL)
            return highMemory.AsSpan((int)(address - 0xffffff80u), bytes);
        if (address >= 0x00ffff80u && (ulong)address + (uint)bytes <= 0x01000000UL)
            return high24Memory.AsSpan((int)(address - 0x00ffff80u), bytes);
        if (address > memory.Length || bytes < 0 || bytes > memory.Length - (long)address)
            throw new InvalidOperationException($"Unmapped address ${address:X8}, {bytes} bytes.");
        return memory.AsSpan((int)address, bytes);
    }
    private uint Long(uint address) => BinaryPrimitives.ReadUInt32BigEndian(Span(address, 4));
    private void Long(uint address, uint value) => BinaryPrimitives.WriteUInt32BigEndian(Span(address, 4), value);

    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
