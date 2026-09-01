using System.Text;
using Amiga;
using Copper68k;
using CopperOS.Commands.NativeExecution;
using static CopperOS.Commands.MakeDirNativeExecution.MakeDirTestBus;

namespace CopperOS.Commands.MakeDirNativeExecution;

internal sealed class MakeDirFixture
{
    public const string SuiteId = "makedir-classic-reference-vector-fixture";
    public const uint LoadAddress = 0x100000;
    public const uint ExecBase = 0x4000;
    public const uint ReturnAddress = 0x2000;
    private readonly M68kCpuModel model;
    private readonly bool original;
    public HunkImage Image { get; }
    public MakeDirTestBus Bus { get; } = new();
    public List<MakeDirObservation> Observations { get; } = [];
    public int NativeInvocationsStarted { get; private set; }
    public object? LastFailure { get; private set; }

    public MakeDirFixture(HunkImage image, M68kCpuModel model, bool original)
    {
        Image = image;
        this.model = model;
        this.original = original;
        Bus.Long(4, ExecBase);
        Bus.LoadAndProtect(LoadAddress, image.Code);
        RegisterExec();
        RegisterDos(0x8000);
        RegisterDos(0x9000);
    }

    public void Run(IEnumerable<CaseBatch> batches)
    {
        foreach (var batch in batches) Execute(batch);
        Bus.AssertImageUnchanged();
    }

    private void Execute(CaseBatch batch)
    {
        Require(batch.Cases.Length is 1 or 2 && batch.Interleaved == (batch.Cases.Length == 2),
            "Invalid execution batch; concurrent callers must actually interleave instructions.");
        var invocations = batch.Cases.Select((c, slot) => new MakeDirInvocation(c, slot, original)).ToArray();
        var cores = new List<IM68kCore>();
        MakeDirInvocation? active = null;
        IM68kCore? activeCore = null;
        try
        {
            foreach (var invocation in invocations)
            {
                active = invocation;
                ValidateDefinition(invocation.Definition);
                Prepare(invocation);
                var cpu = M68kCoreFactory.Default.Create(model, Bus);
                cores.Add(cpu);
                activeCore = cpu;
                cpu.State.StatusRegister = 0; // CLI user mode, not a hardware-reset experiment.
                cpu.BeginSubroutine(LoadAddress, invocation.StackTop, ReturnAddress);
                for (var register = 0; register < 8; register++) cpu.State.D[register] = DataSentinel(register);
                for (var register = 0; register < 7; register++) cpu.State.A[register] = AddressSentinel(register);
                cpu.State.D[0] = (uint)Encoding.ASCII.GetByteCount("not-parsed\n");
                cpu.State.A[0] = invocation.Arguments;
            }

            while (cores.Any(cpu => cpu.State.ProgramCounter != ReturnAddress))
            {
                for (var index = 0; index < cores.Count; index++)
                {
                    var cpu = cores[index];
                    if (cpu.State.ProgramCounter == ReturnAddress) continue;
                    var invocation = invocations[index];
                    active = invocation;
                    activeCore = cpu;
                    Bus.Activate(invocation);
                    Require(!cpu.State.Halted && !cpu.State.Stopped, "CPU halted or stopped.");
                    Bus.AssertProgramCounter(cpu.State.ProgramCounter);
                    if (invocation.Instructions == 0) NativeInvocationsStarted++;
                    Require(++invocation.Instructions <= 200_000, "Native instruction bound exceeded.");
                    cpu.ExecuteInstruction();
                }
            }

            for (var index = 0; index < cores.Count; index++)
            {
                active = invocations[index];
                activeCore = cores[index];
                Bus.Activate(active);
                Observations.Add(Verify(active, activeCore, batch.Interleaved));
            }
        }
        catch (Exception error)
        {
            LastFailure = new
            {
                artifact = original ? "original-wb31-37.2" : "generated-probe",
                caseId = active?.Definition.Id,
                instructions = active?.Instructions ?? 0,
                pc = activeCore?.State.ProgramCounter,
                sp = activeCore?.State.A[7],
                d0 = activeCore?.State.D[0],
                ioError = active is null ? (int?)null : Bus.IoError(active),
                events = active?.Events,
                semanticEvents = active?.SemanticEvents,
                failure = error.Message
            };
            throw new InvalidOperationException($"{(original ? "original" : "generated")} {active?.Definition.Id}: {error.Message}", error);
        }
        finally
        {
            foreach (var cpu in cores) cpu.Dispose();
            Bus.Activate(null);
        }
    }

    private static uint DataSentinel(int index) => (uint)(0xde000000 + index * 16);
    private static uint AddressSentinel(int index) => (uint)(0xae000000 + index * 16);

    private static void ValidateDefinition(MakeDirCase c)
    {
        Require(c.StackBytes is >= 4096 and <= 0x8000 && (c.StackBytes & 3) == 0, "Invalid fixture stack size.");
        Require((c.Names ?? []).SequenceEqual(c.Steps.Select(s => s.Name)), "Names and independent directory script differ.");
        foreach (var name in c.Names ?? [])
            Require(name.All(ch => ch is > '\0' and <= '\u00ff') || name.Length == 0,
                "Fixture names must be byte-exact Latin-1 without embedded NUL.");
        Require(c.Steps.All(s => s.ExistingLock == 0 || s.CreatedLock == 0), "A step cannot acquire both lock results.");
    }

    private void Prepare(MakeDirInvocation invocation)
    {
        Bus.Activate(invocation);
        Bus.Memory.AsSpan((int)invocation.Process - 16, 0x420).Fill(0xb6);
        Bus.Memory.AsSpan((int)invocation.Process, 0x400).Clear();
        Bus.Long(invocation.Process + (uint)DosLayout.Process.CommandLineInterface, 0x101);
        Bus.SetIoError(invocation, MakeDirInvocation.InitialIoError);
        invocation.ProcessSnapshot = Bus.Memory.AsSpan((int)invocation.Process, 0x400).ToArray();
        Bus.Memory.AsSpan((int)invocation.Arguments - 16, 0x420).Fill(0xb6);
        Bus.Memory.AsSpan((int)invocation.Arguments, 0x400).Clear();
        Encoding.ASCII.GetBytes("not-parsed\n").CopyTo(Bus.Memory.AsSpan((int)invocation.Arguments));
        invocation.ArgumentSnapshot = Bus.Memory.AsSpan((int)invocation.Arguments, 0x400).ToArray();
        Bus.Memory.AsSpan((int)(invocation.StackTop - invocation.StackBytes - 16),
            (int)invocation.StackBytes + 32).Fill(0xb6);
    }

    private MakeDirObservation Verify(MakeDirInvocation invocation, IM68kCore cpu, bool interleaved)
    {
        var c = invocation.Definition;
        var result = unchecked((int)cpu.State.D[0]);
        var error = Bus.IoError(invocation);
        Require(result == c.ExpectedResult, $"Return {result}, expected {c.ExpectedResult}.");
        Require(error == c.ExpectedError, $"IoErr {error}, expected {c.ExpectedError}.");
        Require(invocation.VPrintfBytes.SequenceEqual(Encoding.Latin1.GetBytes(c.ExpectedVPrintfBytes)),
            "VPrintf accepted bytes differ from the independent expected bytes.");
        Require(invocation.FaultRequests.Select(r => r.Code).SequenceEqual(c.ExpectedFaultCodes), "Wrong PrintFault request codes/count.");
        Require(invocation.FaultRequests.All(r => r.Header == 0), "PrintFault header was not null.");
        Require(cpu.State.A[7] == invocation.StackTop, "Native return did not restore SP.");
        // NDK STARTUP.ASM:264-267 permits a command image to change every
        // register except SP. The library callee-save group is an observation
        // here, not an image-entry acceptance condition. Public vector ABI
        // assertions and volatile clobbers remain in Register below.
        var nonvolatileRegistersRestored = true;
        for (var register = 2; register < 8; register++)
            nonvolatileRegistersRestored &= cpu.State.D[register] == DataSentinel(register);
        for (var register = 2; register < 7; register++)
            nonvolatileRegistersRestored &= cpu.State.A[register] == AddressSentinel(register);
        Guard(invocation.StackTop - invocation.StackBytes - 16);
        Guard(invocation.StackTop);
        Guard(invocation.Process - 16);
        Guard(invocation.Process + 0x400);
        Guard(invocation.Arguments - 16);
        Guard(invocation.Arguments + 0x400);
        Require(Bus.Memory.AsSpan((int)invocation.Arguments, 0x400).SequenceEqual(invocation.ArgumentSnapshot), "CLI input bytes changed.");
        var process = (byte[])invocation.ProcessSnapshot.Clone();
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(
            process.AsSpan(DosLayout.Process.Result2, 4), c.ExpectedError);
        Require(Bus.Memory.AsSpan((int)invocation.Process, 0x400).SequenceEqual(process), "Unexpected Process write or missing pr_Result2 update.");
        Require(invocation.Opens == 1 && invocation.Closes == (c.MissingDos ? 0 : 1), "Unbalanced DOS open/close.");
        var reachedParser = !c.MissingDos && !c.AllocationFailure;
        Require(invocation.ReadArgsCalls == (reachedParser ? 1 : 0), "Wrong ReadArgs call count.");
        Require(invocation.FreeArgsCalls == (reachedParser && c.ParserError == 0 ? 1 : 0), "Wrong FreeArgs call count.");
        Require(invocation.AllocationAttempts == (!original && !c.MissingDos ? 1 : 0), "Unexpected compiler or argument allocation.");
        Require(invocation.FreeMemCalls == (!original && !c.MissingDos && !c.AllocationFailure ? 1 : 0), "Wrong result-slot free count.");
        Require(invocation.DirectoryIndex == c.Steps.Length && invocation.OwnedLock == 0 &&
            !invocation.AwaitingCreate && invocation.ExpectedDiagnostic is null, "Skipped directory work, diagnostic or lock cleanup.");
        Require(invocation.MissingNamePrinted == (reachedParser && c.ParserError == 0 && c.Names is null), "Missing or spurious no-name diagnostic.");
        Bus.AssertReleased(invocation);
        Bus.AssertImageUnchanged();
        return new(original ? "original-wb31-37.2" : "generated-probe", c.Id, c.Id.Split('.')[0], interleaved,
            result, error, c.ExpectedResult, c.ExpectedError, Hex(invocation.VPrintfBytes.ToArray()), invocation.Instructions,
            invocation.StackBytes, invocation.StackTop - invocation.LowestStackWrite, invocation.Opens, invocation.Closes,
            invocation.ReadArgsCalls, invocation.FreeArgsCalls, invocation.AllocationAttempts, invocation.FreeMemCalls,
            nonvolatileRegistersRestored, true, true, invocation.VPrintfCalls.ToArray(), invocation.FaultRequests.ToArray(),
            invocation.DirectoryCalls.ToArray(), invocation.SemanticEvents.ToArray(), invocation.Events.ToArray());
    }

    private void Guard(uint address) => Require(Bus.Memory.AsSpan((int)address, 16).IndexOfAnyExcept((byte)0xb6) < 0,
        $"Fixture guard changed at ${address:X8}.");

    private void Register(uint baseAddress, short offset, string name, Func<M68kCpuState, MakeDirInvocation, uint> handler)
    {
        Bus.RegisterGateway(checked((uint)(baseAddress + offset)), state =>
        {
            var invocation = Bus.Current ?? throw new InvalidOperationException("Vector without active process.");
            Require(state.A[6] == baseAddress, $"{name}: wrong A6 base.");
            if (baseAddress != ExecBase)
            {
                Require(baseAddress == invocation.DosBase, "DOS base crossed invocation ownership.");
                RequireLiveDos(invocation);
            }
            invocation.Events.Add(name);
            var value = handler(state, invocation);
            state.D[0] = value;
            state.D[1] = 0xd1d1d1d1;
            state.A[0] = 0xa0a0a0a0;
            state.A[1] = 0xa1a1a1a1;
            state.StatusRegister = (ushort)((state.StatusRegister & 0xffe0) | 0x001f);
        });
    }

    private static void RequireLiveDos(MakeDirInvocation invocation) => Require(
        invocation.Opens == 1 && invocation.Closes == 0 && !invocation.Definition.MissingDos,
        "Public DOS call without a live owned library lease.");

    private void RegisterExec()
    {
        Register(ExecBase, ExecLvo.OpenLibrary, "OpenLibrary", (state, invocation) =>
        {
            Require(invocation.Opens == 0 && Bus.GatewayText(state.A[1]) == "dos.library" && state.D[0] == 36,
                "Expected exactly one dos.library V36 open.");
            invocation.Opens++;
            return invocation.Definition.MissingDos ? 0 : invocation.DosBase;
        });
        Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", (state, invocation) =>
        {
            RequireLiveDos(invocation);
            Require(state.A[1] == invocation.DosBase && invocation.OwnedLock == 0, "Wrong library or live lock at close.");
            Bus.AssertReleased(invocation);
            invocation.Closes++;
            return 0xc10ced;
        });
        Register(ExecBase, ExecLvo.AllocMem, "AllocMem", (state, invocation) =>
        {
            RequireLiveDos(invocation);
            Require(!original && invocation.AllocationAttempts == 0 && invocation.ReadArgsCalls == 0 &&
                state.D[0] == 4 && state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                "Unexpected allocation: only the generated four-byte argument slot is admitted.");
            invocation.AllocationAttempts++;
            return invocation.Definition.AllocationFailure ? 0 : Bus.Allocate(invocation, 4, "Exec", true);
        });
        Register(ExecBase, ExecLvo.FreeMem, "FreeMem", (state, invocation) =>
        {
            RequireLiveDos(invocation);
            Require(!original && invocation.FreeMemCalls == 0 && invocation.ReadArgsCalls == 1 &&
                (invocation.Definition.ParserError != 0 || invocation.FreeArgsCalls == 1), "Wrong result-slot cleanup order.");
            Bus.Release(invocation, state.A[1], "Exec", state.D[0]);
            invocation.FreeMemCalls++;
            Bus.SetIoError(invocation, 902);
            return 0xf4ee;
        });
        foreach (var vector in new[] { ExecLvo.FindTask, ExecLvo.WaitPort, ExecLvo.GetMsg, ExecLvo.Forbid, ExecLvo.ReplyMsg })
            Register(ExecBase, vector, "ExcludedLaunchVector", (_, _) =>
                throw new InvalidOperationException("CLI fixture reached an unqualified Workbench/startup operation."));
    }

    private void RegisterDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) => ReadArgs(state, invocation));
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Require(invocation.ReadArgsCalls == 1 && invocation.FreeArgsCalls == 0 && invocation.RdArgs != 0 &&
                state.D[1] == invocation.RdArgs && invocation.DirectoryIndex == invocation.Definition.Steps.Length &&
                invocation.OwnedLock == 0 && invocation.ExpectedDiagnostic is null, "Wrong or premature RDArgs release.");
            Require(Bus.GatewayLong(invocation.ResultSlot) == invocation.NameVector, "Result slot changed while the lease was live.");
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgsCalls++;
            Bus.SetIoError(invocation, 901);
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (state, invocation) => Lock(state, invocation));
        Register(baseAddress, DosLvo.CreateDir, "CreateDir", (state, invocation) => CreateDir(state, invocation));
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            Require(invocation.OwnedLock != 0 && state.D[1] == invocation.OwnedLock && invocation.ExpectedDiagnostic is null,
                "Foreign, doubled, converted or premature BPTR unlock.");
            var index = invocation.DirectoryIndex;
            var name = invocation.Definition.Steps[index].Name;
            invocation.DirectoryCalls.Add(new("UnLock", Hex(Encoding.Latin1.GetBytes(name)), invocation.NamePointers[index], null, state.D[1], 904));
            invocation.SemanticEvents.Add($"UnLock:{state.D[1]:X8}");
            invocation.OwnedLock = 0;
            invocation.DirectoryIndex++;
            Bus.SetIoError(invocation, 904);
            return 0xdead;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) => VPrintf(state, invocation));
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state, invocation) =>
        {
            Require(state.D[2] == 0, "PrintFault header must be null.");
            var code = unchecked((int)state.D[1]);
            var success = invocation.Definition.PrintFaultFailure ? 0 : 1;
            invocation.FaultRequests.Add(new(code, state.D[2], success));
            invocation.SemanticEvents.Add($"PrintFault:{code}:00000000");
            Bus.SetIoError(invocation, code); // NDK API contract; no host rendering of DOS text.
            return (uint)success;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) => unchecked((uint)Bus.IoError(invocation)));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            var previous = Bus.IoError(invocation);
            Bus.SetIoError(invocation, unchecked((int)state.D[1]));
            return unchecked((uint)previous);
        });
        foreach (var vector in new[] { DosLvo.Close, DosLvo.CurrentDir, DosLvo.DeleteFile })
            Register(baseAddress, vector, "ForbiddenFileOperation", (_, _) =>
                throw new InvalidOperationException("MakeDir closed a borrowed stream, changed directory or rolled back output."));
    }

    private uint ReadArgs(M68kCpuState state, MakeDirInvocation invocation)
    {
        Require(invocation.ReadArgsCalls == 0 && Bus.GatewayText(state.D[1]) == "NAME/M" && state.D[3] == 0,
            "ReadArgs template or source ABI mismatch.");
        var resultSlot = state.D[2];
        Require((resultSlot & 3) == 0 && Bus.GatewayLong(resultSlot) == 0, "NAME result slot is not aligned and cleared.");
        if (original) Require(Bus.IsOwnStack(invocation, resultSlot, 4), "Original result slot was not on its stack.");
        else Require(Bus.OwnedAllocation(invocation, resultSlot, "Exec").Size == 4, "Generated result slot is not its owned allocation.");
        invocation.ResultSlot = resultSlot;
        invocation.ReadArgsCalls++;
        if (invocation.Definition.ParserError != 0)
        {
            Bus.SetIoError(invocation, invocation.Definition.ParserError);
            return 0;
        }

        var names = invocation.Definition.Names;
        var vectors = names is null ? 0 : checked((names.Length + 1) * 4);
        var stringBytes = names is null ? 0 : names.Sum(name => Encoding.Latin1.GetByteCount(name) + 1);
        var rdArgs = Bus.Allocate(invocation, checked((uint)(32 + vectors + stringBytes)), "RDArgs", false);
        invocation.RdArgs = rdArgs;
        if (names is not null)
        {
            invocation.NameVector = rdArgs + 32;
            invocation.NamePointers = new uint[names.Length];
            var next = checked(invocation.NameVector + (uint)vectors);
            for (var index = 0; index < names.Length; index++)
            {
                var bytes = Encoding.Latin1.GetBytes(names[index]);
                invocation.NamePointers[index] = next;
                Bus.Long(invocation.NameVector + (uint)index * 4, next);
                bytes.CopyTo(Bus.Memory.AsSpan((int)next));
                next = checked(next + (uint)bytes.Length + 1);
            }
        }
        Bus.Long(resultSlot, invocation.NameVector);
        Bus.Seal(invocation, rdArgs, "RDArgs");
        if (!original) Bus.Seal(invocation, resultSlot, "Exec");
        Bus.SetIoError(invocation, 777); // Successful parsing need not leave zero ambient IoErr.
        return rdArgs;
    }

    private DirectoryStep Step(MakeDirInvocation invocation, uint namePointer)
    {
        Require(invocation.RdArgs != 0 && invocation.FreeArgsCalls == 0 &&
            invocation.DirectoryIndex < invocation.Definition.Steps.Length, "Directory call outside the supplied live name list.");
        Require(namePointer == invocation.NamePointers[invocation.DirectoryIndex], "DOS name pointer is not the exact supplied CString.");
        var step = invocation.Definition.Steps[invocation.DirectoryIndex];
        Require(Bus.GatewayText(namePointer) == step.Name, "DOS name bytes changed.");
        return step;
    }

    private uint Lock(M68kCpuState state, MakeDirInvocation invocation)
    {
        var step = Step(invocation, state.D[1]);
        Require(!invocation.AwaitingCreate && invocation.OwnedLock == 0 && invocation.ExpectedDiagnostic is null &&
            unchecked((int)state.D[2]) == -2, "Lock order or ACCESS_READ register mismatch.");
        var error = step.ExistingLock == 0 ? step.LockError : 711;
        invocation.DirectoryCalls.Add(new("Lock", Hex(Encoding.Latin1.GetBytes(step.Name)), state.D[1], -2, step.ExistingLock, error));
        invocation.SemanticEvents.Add($"Lock:{Hex(Encoding.Latin1.GetBytes(step.Name))}:-2:{step.ExistingLock:X8}");
        invocation.AwaitingCreate = step.ExistingLock == 0;
        invocation.OwnedLock = step.ExistingLock;
        if (step.ExistingLock != 0) invocation.ExpectedDiagnostic = "exists";
        Bus.SetIoError(invocation, error);
        return step.ExistingLock;
    }

    private uint CreateDir(M68kCpuState state, MakeDirInvocation invocation)
    {
        var step = Step(invocation, state.D[1]);
        Require(invocation.AwaitingCreate && invocation.OwnedLock == 0 && invocation.ExpectedDiagnostic is null,
            "CreateDir did not follow a failed Lock.");
        var error = step.CreatedLock == 0 ? step.CreateError : 712;
        invocation.DirectoryCalls.Add(new("CreateDir", Hex(Encoding.Latin1.GetBytes(step.Name)), state.D[1], null, step.CreatedLock, error));
        invocation.SemanticEvents.Add($"CreateDir:{Hex(Encoding.Latin1.GetBytes(step.Name))}:{step.CreatedLock:X8}");
        invocation.AwaitingCreate = false;
        invocation.OwnedLock = step.CreatedLock;
        if (step.CreatedLock == 0) invocation.ExpectedDiagnostic = "create";
        Bus.SetIoError(invocation, error);
        return step.CreatedLock;
    }

    private uint VPrintf(M68kCpuState state, MakeDirInvocation invocation)
    {
        Require(invocation.RdArgs != 0 && invocation.FreeArgsCalls == 0, "VPrintf outlived its argument lease.");
        var format = Bus.GatewayCString(state.D[1]);
        var formatText = Encoding.Latin1.GetString(format);
        uint? namePointer = null;
        byte[]? nameBytes = null;
        byte[] rendered;
        if (formatText == "No name given\n")
        {
            Require(invocation.Definition.Names is null && state.D[2] == 0 && !invocation.MissingNamePrinted,
                "Wrong missing-NAME format arguments or duplicate message.");
            invocation.MissingNamePrinted = true;
            rendered = format;
        }
        else
        {
            var kind = invocation.ExpectedDiagnostic;
            Require(kind is "exists" or "create", "Spurious name diagnostic.");
            Require(formatText == (kind == "exists" ? "%s already exists\n" : "Can't create directory %s\n"),
                "Unexpected command-owned VPrintf format bytes.");
            namePointer = Bus.GatewayLong(state.D[2]);
            _ = Step(invocation, namePointer.Value);
            nameBytes = Bus.GatewayCString(namePointer.Value);
            // Only the three verified formats are rendered. This does not
            // purport to implement the original DOS formatter or fault text.
            rendered = kind == "exists"
                ? [.. nameBytes, .. Encoding.ASCII.GetBytes(" already exists\n")]
                : [.. Encoding.ASCII.GetBytes("Can't create directory "), .. nameBytes, 10];
            invocation.ExpectedDiagnostic = null;
            if (kind == "create") invocation.DirectoryIndex++;
        }
        var returned = invocation.Definition.VPrintfResult ?? rendered.Length;
        Require(returned >= -1 && returned <= rendered.Length, "Invalid supplied VPrintf outcome.");
        var accepted = rendered.AsSpan(0, Math.Max(0, returned)).ToArray();
        invocation.VPrintfBytes.AddRange(accepted);
        invocation.VPrintfCalls.Add(new(Hex(format), state.D[2], namePointer, nameBytes is null ? null : Hex(nameBytes),
            Hex(rendered), Hex(accepted), returned));
        invocation.SemanticEvents.Add($"VPrintf:{Hex(format)}:{(nameBytes is null ? "null" : Hex(nameBytes))}:{Hex(accepted)}:{returned}");
        // Successful output does not promise to preserve ambient IoErr. This
        // also rejects moving the first CreateDir error capture after VPrintf.
        Bus.SetIoError(invocation, returned == rendered.Length ? 903 : 221);
        return unchecked((uint)returned);
    }

    private static string Hex(byte[] bytes) => Convert.ToHexStringLower(bytes);
}
