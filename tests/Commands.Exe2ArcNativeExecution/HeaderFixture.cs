using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.Exe2ArcNativeExecution;

internal sealed class HeaderInvocation(HeaderCase definition, int slot, M68kCpuModel model)
{
    public HeaderCase Definition { get; } = definition;
    public uint Control { get; } = (uint)(0x20000 + slot * 0x1000);
    public uint StorageAddress { get; } = definition.AtPhysicalAddressLimit
        ? (model == M68kCpuModel.M68000 ? 0x00ffffffu : uint.MaxValue) - (uint)definition.HeaderBytes.Length + 1
        : (uint)(0x30001 + slot * 0x1000);
    public uint ArgumentAddress => Definition.ArgumentAddress ?? StorageAddress;
    public uint Region => Definition.AtPhysicalAddressLimit
        ? (model == M68kCpuModel.M68000 ? 0x00ffff80u : 0xffffff80u) : StorageAddress - 17;
    public uint StackTop { get; } = (uint)(0x50000 + slot * 0x10000);
    public uint LowestStackWrite { get; set; } = (uint)(0x50000 + slot * 0x10000);
    public long Instructions { get; set; }
    public byte[] HeaderSnapshot { get; set; } = [];
    public byte[] ControlSnapshot { get; set; } = [];
    public List<int> HeaderReadOffsets { get; } = [];
}

internal sealed class HeaderFixture(HunkImage image, M68kCpuModel model)
{
    internal const uint LoadAddress = 0x100000;
    internal const uint ReturnAddress = 0x2000;
    private readonly HeaderTestBus bus = new(image);
    public List<object> Returned { get; } = [];
    public int NativeInvocationsStarted { get; private set; }
    public string? ActiveCase { get; private set; }

    public void Run()
    {
        foreach (var batch in HeaderCases.All(model)) Execute(batch);
        bus.AssertImageUnchanged();
        HeaderTestBus.Require(Returned.Count == HeaderCases.ExpectedInvocations &&
            NativeInvocationsStarted == HeaderCases.ExpectedInvocations, "Not every native case returned and passed.");
    }

    private void Execute(HeaderCase[] definitions)
    {
        var invocations = definitions.Select((test, slot) => new HeaderInvocation(test, slot, model)).ToArray();
        var cores = new List<IM68kCore>();
        try
        {
            foreach (var invocation in invocations)
            {
                ActiveCase = invocation.Definition.Id;
                bus.Current = invocation;
                bus.Initialize(invocation);
                var core = M68kCoreFactory.Default.Create(model, bus);
                cores.Add(core);
                core.State.StatusRegister = 0;
                core.BeginSubroutine(LoadAddress, invocation.StackTop, ReturnAddress);
                for (int r = 0; r < 8; r++) core.State.D[r] = (uint)(0xde000000 + r * 16);
                for (int r = 0; r < 7; r++) core.State.A[r] = (uint)(0xae000000 + r * 16);
                core.State.D[0] = 56;
                core.State.A[0] = invocation.Control;
                NativeInvocationsStarted++;
            }
            while (cores.Any(c => c.State.ProgramCounter != ReturnAddress))
            for (int index = 0; index < cores.Count; index++)
            {
                var core = cores[index];
                if (core.State.ProgramCounter == ReturnAddress) continue;
                var invocation = invocations[index];
                ActiveCase = invocation.Definition.Id;
                bus.Current = invocation;
                HeaderTestBus.Require(!core.State.Halted && !core.State.Stopped &&
                    ++invocation.Instructions <= 100_000, "CPU stopped or exceeded the instruction bound.");
                bus.AssertProgramCounter(core.State.ProgramCounter);
                try { core.ExecuteInstruction(); }
                catch (Exception error)
                {
                    throw new InvalidOperationException($"{ActiveCase} PC=${core.State.ProgramCounter:X8}: {error.Message}", error);
                }
            }
            for (int index = 0; index < cores.Count; index++)
            {
                var invocation = invocations[index];
                var core = cores[index];
                var test = invocation.Definition;
                ActiveCase = test.Id;
                HeaderTestBus.Require(core.State.D[0] == (test.Found ? 0u : 10u), "Wrong found/rejected result.");
                HeaderTestBus.Require(core.State.A[7] == invocation.StackTop, "Entry SP was not restored.");
                bus.AssertResultsAndGuards(invocation);
                bus.AssertImageUnchanged();
                Returned.Add(new
                {
                    id = test.Id, interleaved = definitions.Length > 1, format = test.Format == 0 ? "RAR4" : "CAB",
                    headerAddress = invocation.ArgumentAddress.ToString("x8"), headerBytes = test.WindowBytes,
                    suppliedHeaderHex = Convert.ToHexStringLower(test.HeaderBytes), fileOffset = test.FileOffset,
                    fileLength = test.FileLength, result = core.State.D[0],
                    payloadLength = bus.Long(invocation.Control + 32), mappingCalls = bus.Long(invocation.Control + 36),
                    readCalls = bus.Long(invocation.Control + 40), readMask = bus.Long(invocation.Control + 44).ToString("x8"),
                    actualHeaderReadOffsets = invocation.HeaderReadOffsets.ToArray(),
                    forbiddenCalls = bus.Long(invocation.Control + 48), instructions = invocation.Instructions,
                    configuredStackBytes = test.StackBytes, stackBytesWritten = invocation.StackTop - invocation.LowestStackWrite,
                    entryStackRestored = true, guardsAndImageUnchanged = true,
                    physicalAddressBoundaryCase = test.AtPhysicalAddressLimit,
                    finalRegisters = new { d = core.State.D.ToArray(), a = core.State.A.ToArray() }
                });
            }
        }
        finally
        {
            bus.Current = null;
            foreach (var core in cores) core.Dispose();
        }
    }
}
