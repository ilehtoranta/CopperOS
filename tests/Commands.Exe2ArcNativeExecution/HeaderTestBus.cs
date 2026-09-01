using System.Buffers.Binary;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.Exe2ArcNativeExecution;

internal sealed class HeaderTestBus : IM68kBus
{
    private readonly byte[] memory = new byte[0x200000];
    private readonly byte[] high24 = new byte[128];
    private readonly byte[] high32 = new byte[128];
    private readonly HunkImage image;
    public HeaderInvocation? Current { get; set; }

    public HeaderTestBus(HunkImage image)
    {
        this.image = image;
        image.Code.CopyTo(Span(HeaderFixture.LoadAddress, image.Code.Length));
    }

    public bool HasHostGateway(uint address) => false;
    public bool TryInvokeHostGateway(uint pc, uint token, M68kCpuState state) => false;
    public void ResetExternalDevices(long cycle) => throw new InvalidOperationException("Component executed RESET.");

    public byte ReadByte(uint address, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 1, false, accessKind); return Span(address, 1)[0]; }
    public ushort ReadWord(uint address, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 2, false, accessKind); return BinaryPrimitives.ReadUInt16BigEndian(Span(address, 2)); }
    public uint ReadLong(uint address, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 4, false, accessKind); return Long(address); }
    public void WriteByte(uint address, byte value, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 1, true, accessKind); Span(address, 1)[0] = value; }
    public void WriteWord(uint address, ushort value, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 2, true, accessKind); BinaryPrimitives.WriteUInt16BigEndian(Span(address, 2), value); }
    public void WriteLong(uint address, uint value, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 4, true, accessKind); Long(address, value); }

    public void Initialize(HeaderInvocation invocation)
    {
        var test = invocation.Definition;
        Span(invocation.Region, 128).Fill(0xa9);
        test.HeaderBytes.CopyTo(Span(invocation.StorageAddress, test.HeaderBytes.Length));
        invocation.HeaderSnapshot = Span(invocation.Region, 128).ToArray();
        Span(invocation.Control - 16, 88).Fill(0xc7);
        Span(invocation.StackTop - test.StackBytes - 16, checked((int)test.StackBytes + 32)).Fill(0xb6);
        Long(invocation.Control, test.Format);
        Long(invocation.Control + 4, invocation.ArgumentAddress);
        Long(invocation.Control + 8, test.WindowBytes);
        Long(invocation.Control + 12, test.FileOffset);
        Long(invocation.Control + 16, test.FileLength);
        Long(invocation.Control + 20, invocation.StorageAddress);
        Long(invocation.Control + 24, test.MappedBytes ?? (uint)test.HeaderBytes.Length);
        Long(invocation.Control + 28, test.MappingEnabled ? 1u : 0u);
        Long(invocation.Control + 32, 0xdeadbeef);
        Long(invocation.Control + 36, 0);
        Long(invocation.Control + 40, 0);
        Long(invocation.Control + 44, 0);
        Long(invocation.Control + 48, 0);
        Long(invocation.Control + 52, 0xbad0bad0);
        invocation.ControlSnapshot = Span(invocation.Control, 32).ToArray();
    }

    public void AssertResultsAndGuards(HeaderInvocation invocation)
    {
        var test = invocation.Definition;
        Require(Long(invocation.Control + 32) == test.PayloadLength, "Wrong payload length.");
        Require(Long(invocation.Control + 36) == test.MappingCalls, "Wrong mapping-call count.");
        Require(Long(invocation.Control + 40) == test.ReadCalls, "Wrong byte-read callback count.");
        Require(Long(invocation.Control + 44) == test.ReadMask, "Wrong byte-read offset bitmap.");
        Require(Long(invocation.Control + 48) == 0, "Production used a forbidden memory interface method.");
        Require(Long(invocation.Control + 52) == 0x45584148, "Probe did not return from the production predicate.");
        Require(invocation.HeaderReadOffsets.Count == test.ReadCalls,
            "Guest bus byte-read count differs from the expected actual header reads.");
        uint observedMask = 0;
        foreach (var offset in invocation.HeaderReadOffsets) observedMask |= 1u << offset;
        Require(observedMask == test.ReadMask, "Guest bus read offsets differ from the expected header fields.");
        Require(Span(invocation.Control, 32).SequenceEqual(invocation.ControlSnapshot), "Caller input control fields changed.");
        Require(Span(invocation.Control - 16, 16).IndexOfAnyExcept((byte)0xc7) < 0 &&
            Span(invocation.Control + 56, 16).IndexOfAnyExcept((byte)0xc7) < 0, "Control guard changed.");
        Require(Span(invocation.Region, 128).SequenceEqual(invocation.HeaderSnapshot), "Borrowed header or its guards changed.");
        Require(Span(invocation.StackTop - test.StackBytes - 16, 16).IndexOfAnyExcept((byte)0xb6) < 0 &&
            Span(invocation.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0, "Stack guard changed.");
    }

    public void AssertImageUnchanged() => Require(
        Span(HeaderFixture.LoadAddress, image.Code.Length).SequenceEqual(image.Code), "Shared native image changed.");
    public void AssertProgramCounter(uint pc) => Require(
        Contains(HeaderFixture.LoadAddress, (uint)image.Code.Length, pc, 2),
        $"PC escaped the generated image: ${pc:X8}.");

    private void Check(uint address, uint bytes, bool write, M68kBusAccessKind accessKind)
    {
        var owner = Current ?? throw new InvalidOperationException("No active header invocation.");
        bool instruction = accessKind == M68kBusAccessKind.CpuInstructionFetch;
        bool inCode = !write && Contains(HeaderFixture.LoadAddress, (uint)image.Code.Length, address, bytes);
        // RTS prefetches its caller before the loop sees the sentinel. These
        // bytes are fetch-only and can never execute due to AssertProgramCounter.
        bool callerPrefetch = !write && instruction && Contains(HeaderFixture.ReturnAddress, 8, address, bytes);
        bool inStack = !instruction && Contains(owner.StackTop - owner.Definition.StackBytes,
            owner.Definition.StackBytes, address, bytes);
        bool inControl = !instruction && Contains(owner.Control + (write ? 32u : 0u),
            write ? 24u : 56u, address, bytes);
        bool inHeader = !write && !instruction && Contains(owner.StorageAddress,
            (uint)owner.Definition.HeaderBytes.Length, address, bytes);
        Require(inCode || callerPrefetch || inStack || inControl || inHeader,
            $"Unowned native {(write ? "write" : "read")} at ${address:X8} ({bytes} bytes, {accessKind}).");
        if (inHeader)
        {
            Require(bytes == 1, "Header was read using a word/long instead of endian-independent byte accesses.");
            owner.HeaderReadOffsets.Add(checked((int)(address - owner.StorageAddress)));
        }
        if (write && inStack) owner.LowestStackWrite = Math.Min(owner.LowestStackWrite, address);
    }

    private Span<byte> Span(uint address, int bytes)
    {
        if (address >= 0xffffff80 && (ulong)address + (uint)bytes <= 0x100000000UL)
            return high32.AsSpan(checked((int)(address - 0xffffff80)), bytes);
        if (address >= 0x00ffff80 && (ulong)address + (uint)bytes <= 0x01000000UL)
            return high24.AsSpan(checked((int)(address - 0x00ffff80)), bytes);
        if (bytes < 0 || address > memory.Length || bytes > memory.Length - (long)address)
            throw new InvalidOperationException($"Unmapped fixture address ${address:X8} ({bytes} bytes).");
        return memory.AsSpan(checked((int)address), bytes);
    }

    public uint Long(uint address) => BinaryPrimitives.ReadUInt32BigEndian(Span(address, 4));
    private void Long(uint address, uint value) => BinaryPrimitives.WriteUInt32BigEndian(Span(address, 4), value);
    private static bool Contains(uint start, uint length, uint address, uint bytes) =>
        address >= start && (ulong)address + bytes <= (ulong)start + length;
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
