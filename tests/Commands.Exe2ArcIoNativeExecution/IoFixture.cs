using System.Security.Cryptography;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.Exe2ArcIoNativeExecution;

internal sealed class IoInvocation(IoCase definition, int slot)
{
    public IoCase Definition { get; } = definition;
    public uint Control { get; } = (uint)(0x20000 + slot * 0x1000);
    public uint Scratch { get; } = (uint)(0x200001 + slot * 0x20000);
    public uint StackTop { get; } = (uint)(0x50000 + slot * 0x10000);
    public uint DosBase { get; } = (uint)(0x18000 + slot * 0x1000);
    public uint InputHandle { get; } = (uint)(0x1137 + slot * 0x100);
    public uint OutputHandle { get; } = (uint)(0x2579 + slot * 0x100);
    public int Cursor { get; set; } = definition.InitialPosition;
    public int Initialized { get; set; }
    public int StepIndex { get; set; }
    public int IoErrCalls { get; set; }
    public int ReadCalls { get; set; }
    public int WriteCalls { get; set; }
    public int AmbientIoErr { get; set; } = 0x13572468;
    public bool CaptureRequired { get; set; }
    public uint LowestStackWrite { get; set; } = (uint)(0x50000 + slot * 0x10000);
    public long Instructions { get; set; }
    public long NativeScratchReads { get; set; }
    public byte[] InputSnapshot { get; } = definition.Input.ToArray();
    public byte[] ControlSnapshot { get; set; } = [];
    public List<byte> Output { get; } = [];
    public List<object> Events { get; } = [];
}

internal sealed class IoFixture(HunkImage image, M68kCpuModel model)
{
    internal const uint LoadAddress = 0x100000;
    internal const uint ReturnAddress = 0x2000;
    private readonly IoTestBus bus = new(image);
    public List<object> Returned { get; } = [];
    public int NativeInvocationsStarted { get; private set; }
    public string? ActiveCase { get; private set; }
    public object? LastFailure { get; private set; }

    public void Run()
    {
        var batches = IoCases.All().ToArray();
        IoTestBus.Require(batches.Sum(b => b.Length) == IoCases.ExpectedInvocations, "Finite invocation count drifted.");
        IoTestBus.Require(batches.SelectMany(b => b).Select(c => c.Id).Distinct().Count() == IoCases.ExpectedInvocations,
            "Case IDs are not unique.");
        foreach (var batch in batches) Execute(batch);
        bus.AssertImageAndVectorsUnchanged();
        IoTestBus.Require(Returned.Count == IoCases.ExpectedInvocations &&
            NativeInvocationsStarted == IoCases.ExpectedInvocations, "Not every native component invocation passed.");
    }

    private void Execute(IoCase[] definitions)
    {
        var invocations = definitions.Select((test, slot) => new IoInvocation(test, slot)).ToArray();
        var cores = new List<IM68kCore>();
        IoInvocation? active = null;
        IM68kCore? activeCore = null;
        try
        {
            foreach (var invocation in invocations)
            {
                active = invocation;
                ActiveCase = invocation.Definition.Id;
                bus.Current = invocation;
                bus.Initialize(invocation);
                var core = M68kCoreFactory.Default.Create(model, bus);
                cores.Add(core);
                activeCore = core;
                core.State.StatusRegister = 0;
                core.BeginSubroutine(LoadAddress, invocation.StackTop, ReturnAddress);
                for (int register = 0; register < 8; register++) core.State.D[register] = (uint)(0xde000000 + register * 16);
                for (int register = 0; register < 7; register++) core.State.A[register] = (uint)(0xae000000 + register * 16);
                core.State.D[0] = 104;
                core.State.A[0] = invocation.Control;
                NativeInvocationsStarted++;
            }
            while (cores.Any(c => c.State.ProgramCounter != ReturnAddress))
            for (int index = 0; index < cores.Count; index++)
            {
                var core = cores[index];
                if (core.State.ProgramCounter == ReturnAddress) continue;
                var invocation = invocations[index];
                active = invocation;
                activeCore = core;
                ActiveCase = invocation.Definition.Id;
                bus.Current = invocation;
                IoTestBus.Require(!core.State.Halted && !core.State.Stopped &&
                    ++invocation.Instructions <= 80_000_000, "CPU stopped or exceeded the declared instruction bound.");
                bus.AssertProgramCounter(core.State.ProgramCounter);
                core.ExecuteInstruction();
            }
            for (int index = 0; index < cores.Count; index++)
            {
                var invocation = invocations[index];
                var core = cores[index];
                active = invocation;
                activeCore = core;
                ActiveCase = invocation.Definition.Id;
                bus.Current = invocation;
                IoTestBus.Require(core.State.D[0] == invocation.Definition.Result, "Wrong component result.");
                IoTestBus.Require(core.State.A[7] == invocation.StackTop, "Whole-entry SP was not restored.");
                bus.AssertResults(invocation);
                byte[] output = invocation.Output.ToArray();
                Returned.Add(new
                {
                    id = invocation.Definition.Id, interleaved = definitions.Length > 1,
                    operation = invocation.Definition.Operation, result = core.State.D[0],
                    inputBytes = invocation.Definition.Input.Length, inputSha256 = Sha(invocation.Definition.Input),
                    fileOrPayloadLength = invocation.Definition.Length,
                    inputCursorAfter = invocation.Cursor,
                    scanStatus = bus.Long(invocation.Control + 28), archiveOffset = bus.Long(invocation.Control + 32),
                    payloadLength = bus.Long(invocation.Control + 36), scanObservation = ReadObservation(invocation.Control + 40),
                    copyStatus = bus.Long(invocation.Control + 68), copyObservation = ReadObservation(invocation.Control + 72),
                    outputBytes = output.Length, outputSha256 = Sha(output),
                    exactOutputCompared = true, events = invocation.Events,
                    readCalls = invocation.ReadCalls, writeCalls = invocation.WriteCalls, ioErrCalls = invocation.IoErrCalls,
                    nativeScratchByteReads = invocation.NativeScratchReads, instructions = invocation.Instructions,
                    configuredStackBytes = invocation.Definition.StackBytes,
                    stackBytesWritten = invocation.StackTop - invocation.LowestStackWrite,
                    entryStackRestored = true, guardsImageAndVectorsUnchanged = true,
                    finalRegisters = new { d = core.State.D.ToArray(), a = core.State.A.ToArray() },
                });
            }
        }
        catch (Exception error)
        {
            LastFailure = new
            {
                id = active?.Definition.Id, instructions = active?.Instructions,
                pc = activeCore?.State.ProgramCounter, d0 = activeCore?.State.D[0], sp = activeCore?.State.A[7],
                inputCursor = active?.Cursor, lastScriptIndex = active?.StepIndex, events = active?.Events,
                outputBytes = active?.Output.Count, error = error.Message,
            };
            throw new InvalidOperationException($"{ActiveCase}: {error.Message}", error);
        }
        finally
        {
            bus.Current = null;
            foreach (var core in cores) core.Dispose();
        }
    }

    private object ReadObservation(uint address) => new
    {
        hasResult = bus.Long(address) != 0, stage = bus.Long(address + 4),
        rawResult = unchecked((int)bus.Long(address + 8)), requestedBytes = bus.Long(address + 12),
        bytesCompleted = bus.Long(address + 16), ioErrCaptured = bus.Long(address + 20) != 0,
        ioError = unchecked((int)bus.Long(address + 24)),
    };
    private static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
