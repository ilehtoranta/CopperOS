using System.Buffers.Binary;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.Exe2ArcIoNativeExecution;

internal sealed class IoTestBus : IM68kBus
{
    private readonly byte[] memory = new byte[0x400000];
    private readonly HunkImage image;
    private readonly Dictionary<uint, (uint Base, string Operation)> gateways = [];
    private readonly Dictionary<uint, byte[]> vectorSnapshots = [];
    public IoInvocation? Current { get; set; }

    public IoTestBus(HunkImage image)
    {
        this.image = image;
        image.Code.CopyTo(Span(IoFixture.LoadAddress, image.Code.Length));
        for (uint slot = 0; slot < 2; slot++)
        {
            uint dosBase = 0x18000 + slot * 0x1000;
            foreach ((int lvo, string operation) in new[] { (-42, "Read"), (-48, "Write"), (-66, "Seek"), (-132, "IoErr") })
            {
                uint address = (uint)(dosBase + lvo);
                gateways.Add(address, (dosBase, operation));
                Word(address, 0xff00);
                Long(address + 2, 1);
            }
        }
        foreach (uint address in gateways.Keys)
            vectorSnapshots.Add(address, Span(address, 6).ToArray());
    }

    public void Initialize(IoInvocation invocation)
    {
        var test = invocation.Definition;
        Span(invocation.Control - 16, 136).Fill(0xc7);
        Span(invocation.Scratch - 16, 102432).Fill(0xa9);
        Span(invocation.StackTop - test.StackBytes - 16, checked((int)test.StackBytes + 32)).Fill(0xb6);
        Long(invocation.Control, test.Operation);
        Long(invocation.Control + 4, test.NullInput ? 0 : invocation.InputHandle);
        Long(invocation.Control + 8, test.NullOutput ? 0 : invocation.OutputHandle);
        Long(invocation.Control + 12, test.ScratchArgument ?? invocation.Scratch);
        Long(invocation.Control + 16, test.Capacity);
        Long(invocation.Control + 20, test.Length);
        Long(invocation.Control + 24, invocation.DosBase);
        invocation.ControlSnapshot = Span(invocation.Control, 28).ToArray();
    }

    public bool HasHostGateway(uint address) => gateways.ContainsKey(address);
    public bool TryInvokeHostGateway(uint pc, uint token, M68kCpuState state)
    {
        if (!gateways.TryGetValue(pc, out var gateway)) return false;
        var owner = Current ?? throw new InvalidOperationException("Vector without active borrower.");
        Require(token == 1 && state.A[6] == gateway.Base && gateway.Base == owner.DosBase,
            "Wrong gateway token, A6 or caller's borrowed DOS base.");
        Require(owner.StepIndex < owner.Definition.Script.Length, "Unexpected extra DOS call: " + gateway.Operation);
        IoStep step = owner.Definition.Script[owner.StepIndex++];
        Require(step.Operation == gateway.Operation, $"Expected {step.Operation}, got {gateway.Operation}.");
        if (step.Operation == "IoErr")
        {
            Require(owner.CaptureRequired && step.Result == owner.AmbientIoErr, "Wrong or stale IoErr capture.");
            owner.CaptureRequired = false;
            owner.IoErrCalls++;
        }
        else
        {
            Require(!owner.CaptureRequired, "A -1 result lost its immediate IoErr capture.");
            uint expectedHandle = step.Operation == "Write" ? owner.OutputHandle : owner.InputHandle;
            Require(state.D[1] == expectedHandle, "DOS D1 is not the caller's raw BPTR.");
            if (step.Operation == "Seek")
            {
                Require(unchecked((int)state.D[2]) == step.Argument && unchecked((int)state.D[3]) == step.Mode,
                    "Seek position/mode differs from the independent script.");
                if (step.Result >= 0)
                {
                    Require(step.Result == owner.Cursor, "Successful Seek must return the old position.");
                    long destination = step.Mode == -1 ? step.Argument : (long)owner.Cursor + step.Argument;
                    Require(destination >= 0 && destination <= owner.Definition.Input.Length, "Script seeks outside inert input.");
                    owner.Cursor = (int)destination;
                }
            }
            else
            {
                Require(state.D[2] == owner.Scratch && unchecked((int)state.D[3]) == step.Argument &&
                    step.Argument is > 0 and <= 102400, "Read/Write D2/D3 scratch/request mismatch.");
                if (step.Operation == "Read")
                {
                    owner.Initialized = step.Result > 0 ? Math.Min(step.Result, step.Argument) : 0;
                    Require(owner.Initialized <= owner.Definition.Input.Length - owner.Cursor, "Read script exceeds inert input.");
                    owner.Definition.Input.AsSpan(owner.Cursor, owner.Initialized).CopyTo(Span(owner.Scratch, owner.Initialized));
                    owner.Cursor += owner.Initialized;
                    owner.ReadCalls++;
                }
                else
                {
                    Require(step.Argument <= owner.Initialized, "Write consumed uninitialized or short-read scratch.");
                    if (step.Result > 0 && step.Result <= step.Argument)
                        owner.Output.AddRange(Span(owner.Scratch, step.Result).ToArray());
                    owner.WriteCalls++;
                }
            }
            owner.CaptureRequired = step.Result == -1;
            owner.AmbientIoErr = owner.CaptureRequired ? step.Error : unchecked((int)0xace01234);
        }
        owner.Events.Add(new
        {
            operation = step.Operation, a6 = state.A[6], d1 = state.D[1], d2 = state.D[2], d3 = state.D[3],
            result = step.Result, inputCursorAfter = owner.Cursor, outputBytesAfter = owner.Output.Count,
        });
        state.D[0] = unchecked((uint)step.Result);
        // Original library call ABI, distinct from whole-image entry freedom.
        state.D[1] = 0xd1d1d1d1;
        state.A[0] = 0xa0a0a0a0;
        state.A[1] = 0xa1a1a1a1;
        state.StatusRegister = (ushort)((state.StatusRegister & 0xffe0) | 0x001f);
        return true;
    }

    public byte ReadByte(uint address, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 1, false, accessKind); return Span(address, 1)[0]; }
    public ushort ReadWord(uint address, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 2, false, accessKind); return BinaryPrimitives.ReadUInt16BigEndian(Span(address, 2)); }
    public uint ReadLong(uint address, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 4, false, accessKind); return Long(address); }
    public void WriteByte(uint address, byte value, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 1, true, accessKind); Span(address, 1)[0] = value; }
    public void WriteWord(uint address, ushort value, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 2, true, accessKind); Word(address, value); }
    public void WriteLong(uint address, uint value, ref long cycle, M68kBusAccessKind accessKind)
    { Check(address, 4, true, accessKind); Long(address, value); }
    public void ResetExternalDevices(long cycle) => throw new InvalidOperationException("Component executed RESET.");

    private void Check(uint address, uint bytes, bool write, M68kBusAccessKind kind)
    {
        var owner = Current ?? throw new InvalidOperationException("No active component invocation.");
        bool instruction = kind == M68kBusAccessKind.CpuInstructionFetch;
        bool code = !write && Contains(IoFixture.LoadAddress, (uint)image.Code.Length, address, bytes);
        bool callerPrefetch = !write && instruction && Contains(IoFixture.ReturnAddress, 8, address, bytes);
        // The six-byte supplied gateway fetches two following words before
        // returning. Like the established MakeDir fixture, admit only these
        // ten fetch bytes; AssertProgramCounter still permits only its start.
        bool vector = false;
        if (!code && !write && instruction)
            foreach (var pair in gateways)
                if (pair.Value.Base == owner.DosBase && Contains(pair.Key, 10, address, bytes))
                {
                    vector = true;
                    break;
                }
        bool stack = !instruction && Contains(owner.StackTop - owner.Definition.StackBytes,
            owner.Definition.StackBytes, address, bytes);
        bool control = !instruction && Contains(owner.Control + (write ? 28u : 0u),
            write ? 76u : 104u, address, bytes);
        bool scratch = !write && !instruction && Contains(owner.Scratch, (uint)owner.Initialized, address, bytes);
        if (!(code || callerPrefetch || vector || stack || control || scratch))
            throw new InvalidOperationException(
                $"Unowned native {(write ? "write" : "read")} ${address:X8}, {bytes} bytes, {kind}.");
        if (scratch)
        {
            Require(bytes == 1, "Scanner did not use endian-independent byte reads.");
            owner.NativeScratchReads++;
        }
        if (write && stack) owner.LowestStackWrite = Math.Min(owner.LowestStackWrite, address);
    }

    public void AssertResults(IoInvocation owner)
    {
        var expected = owner.Definition;
        Require(owner.StepIndex == expected.Script.Length && !owner.CaptureRequired, "Missing scripted DOS calls/error capture.");
        Require(owner.Output.SequenceEqual(expected.Output), "Exact copied output bytes differ.");
        Require(owner.Definition.Input.SequenceEqual(owner.InputSnapshot), "Borrowed input fixture changed.");
        Require(Long(owner.Control + 28) == expected.ScanStatus, "Wrong scan status.");
        Require(Long(owner.Control + 32) == expected.Offset, "Wrong archive offset.");
        Require(Long(owner.Control + 36) == expected.PayloadLength, "Wrong payload length.");
        Require(Long(owner.Control + 68) == expected.CopyStatus, "Wrong copy status.");
        AssertObservation(owner.Control + 40, expected.ScanIo);
        AssertObservation(owner.Control + 72, expected.CopyIo);
        Require(Long(owner.Control + 100) == 0x45584149, "Probe did not finish production calls.");
        Require(Span(owner.Control, 28).SequenceEqual(owner.ControlSnapshot), "Caller input controls changed.");
        Guard(owner.Control - 16, 0xc7);
        Guard(owner.Control + 104, 0xc7);
        Guard(owner.Scratch - 16, 0xa9);
        Guard(owner.Scratch + 102400, 0xa9);
        Guard(owner.StackTop - expected.StackBytes - 16, 0xb6);
        Guard(owner.StackTop, 0xb6);
        AssertImageAndVectorsUnchanged();
    }

    private void AssertObservation(uint address, ExpectedIo expected)
    {
        uint[] values = [expected.HasResult, expected.Stage, unchecked((uint)expected.RawResult),
            expected.RequestedBytes, expected.BytesCompleted, expected.Captured, unchecked((uint)expected.Error)];
        for (int index = 0; index < values.Length; index++)
            Require(Long(address + (uint)index * 4) == values[index],
                $"Wrong observation field {index}: {Long(address + (uint)index * 4):X8}, expected {values[index]:X8}.");
    }

    public void AssertImageAndVectorsUnchanged()
    {
        Require(Span(IoFixture.LoadAddress, image.Code.Length).SequenceEqual(image.Code), "Shared native image changed.");
        foreach (var vector in vectorSnapshots)
            Require(Span(vector.Key, vector.Value.Length).SequenceEqual(vector.Value), "A public vector changed.");
    }
    public void AssertProgramCounter(uint pc)
    {
        if (!Contains(IoFixture.LoadAddress, (uint)image.Code.Length, pc, 2) && !gateways.ContainsKey(pc))
            throw new InvalidOperationException($"Native PC escaped image or registered vectors: ${pc:X8}.");
    }
    private void Guard(uint address, byte value) => Require(Span(address, 16).IndexOfAnyExcept(value) < 0,
        $"Guard changed at ${address:X8}.");
    private Span<byte> Span(uint address, int size)
    {
        Require(size >= 0 && (ulong)address + (uint)size <= (uint)memory.Length, "Unmapped fixture address.");
        return memory.AsSpan((int)address, size);
    }
    public uint Long(uint address) => BinaryPrimitives.ReadUInt32BigEndian(Span(address, 4));
    private void Long(uint address, uint value) => BinaryPrimitives.WriteUInt32BigEndian(Span(address, 4), value);
    private void Word(uint address, ushort value) => BinaryPrimitives.WriteUInt16BigEndian(Span(address, 2), value);
    private static bool Contains(uint start, uint length, uint address, uint bytes) =>
        address >= start && (ulong)address + bytes <= (ulong)start + length;
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
