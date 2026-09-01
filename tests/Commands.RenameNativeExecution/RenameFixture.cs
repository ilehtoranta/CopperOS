using System.Buffers.Binary;
using System.Text;
using Amiga;
using Copper68k;
using CopperOS.Commands.NativeExecution;
using static CopperOS.Commands.RenameNativeExecution.RenameTestBus;

namespace CopperOS.Commands.RenameNativeExecution;

internal sealed class RenameFixture
{
    public const string SuiteId = "rename-classic-original-vector-fixture";
    public const uint LoadAddress = 0x100000;
    public const uint ExecBase = 0x4000;
    public const uint ReturnAddress = 0x2000;
    private static readonly uint[] AllocationSizes = [80, 538, 256, 256];
    private static readonly uint[] AllocationFlags = [0, 0x10000, 0, 0x10000];
    private static readonly string[] AllocationKinds = ["unused80", "anchor", "source", "destination"];
    private static readonly int[] FreeOrder = [1, 0, 3, 2];
    private readonly M68kCpuModel model;
    public HunkImage Image { get; }
    public RenameTestBus Bus { get; } = new();
    public List<RenameResult> Observations { get; } = [];
    public List<RenameGuardObservation> GuardStops { get; } = [];
    public int NativeInvocationsStarted { get; private set; }
    public object? LastFailure { get; private set; }

    public RenameFixture(HunkImage image, M68kCpuModel model)
    {
        Image = image;
        this.model = model;
        Bus.Long(4, ExecBase);
        Bus.LoadAndProtect(LoadAddress, image.Code);
        RegisterExec();
        RegisterDos(0x8000);
        RegisterDos(0x9000);
    }

    public void Run(IEnumerable<RenameBatch> batches)
    {
        foreach (var batch in batches) Execute(batch);
        Bus.AssertImageUnchanged();
    }

    private void Execute(RenameBatch batch)
    {
        Require(batch.Cases.Length is 1 or 2 && batch.Interleaved == (batch.Cases.Length == 2),
            "Interleaved batches must actually contain two native callers.");
        Require(!batch.Interleaved || batch.Cases.All(c => c.ExpectedGuard is null),
            "Guard experiments are separate bounded invocations.");
        var invocations = batch.Cases.Select((c, slot) => new RenameInvocation(c, slot)).ToArray();
        var cores = new List<IM68kCore>();
        RenameInvocation? active = null;
        IM68kCore? activeCore = null;
        try
        {
            foreach (var invocation in invocations)
            {
                active = invocation;
                Prepare(invocation);
                var cpu = M68kCoreFactory.Default.Create(model, Bus);
                cores.Add(cpu);
                activeCore = cpu;
                cpu.State.StatusRegister = 0;
                cpu.BeginSubroutine(LoadAddress, invocation.StackTop, ReturnAddress);
                for (var register = 0; register < 8; register++) cpu.State.D[register] = DataSentinel(register);
                for (var register = 0; register < 7; register++) cpu.State.A[register] = AddressSentinel(register);
                cpu.State.D[0] = 11;
                cpu.State.A[0] = invocation.Arguments;
            }

            while (cores.Any(cpu => cpu.State.ProgramCounter != ReturnAddress))
            {
                for (var index = 0; index < cores.Count; index++)
                {
                    var cpu = cores[index];
                    if (cpu.State.ProgramCounter == ReturnAddress) continue;
                    active = invocations[index];
                    activeCore = cpu;
                    Bus.Activate(active);
                    Require(!cpu.State.Halted && !cpu.State.Stopped, "CPU stopped or halted.");
                    Bus.AssertProgramCounter(cpu.State.ProgramCounter);
                    if (active.Instructions == 0) NativeInvocationsStarted++;
                    Require(++active.Instructions <= 200_000, "Native instruction bound exceeded.");
                    cpu.ExecuteInstruction();
                }
            }

            for (var index = 0; index < cores.Count; index++)
            {
                active = invocations[index];
                activeCore = cores[index];
                Bus.Activate(active);
                Require(active.Definition.ExpectedGuard is null, "Expected guarded path unexpectedly returned.");
                Observations.Add(Verify(active, activeCore, batch.Interleaved));
            }
        }
        catch (RenameReferenceGuard guard) when (invocations.Length == 1 && active is not null &&
            activeCore is not null && active.Definition.ExpectedGuard == guard.Kind &&
            active.Definition.ExpectedGuardOrigin == guard.Origin)
        {
            AssertStorage(active);
            var frame = activeCore.State.A[5];
            int? selected = frame >= 28 && Bus.IsOwnStack(active, frame - 28, 4)
                ? unchecked((int)Bus.Long(frame - 28)) : null;
            GuardStops.Add(new(active.Definition.Id, ContractId(active.Definition.Id), guard.Kind, guard.Origin,
                guard.Message, active.Instructions, activeCore.State.ProgramCounter, activeCore.State.A[7],
                activeCore.State.D[0], Bus.IoError(active), selected, false, false, true,
                active.MatchFirstCalls, active.MatchNextCalls, active.MatchEndCalls, active.SearchLive,
                Bus.LiveAllocations(active), active.OwnedLocks.Order().ToArray(), active.Output.ToArray(),
                active.Faults.ToArray(), active.Calls.ToArray()));
            // These are stopped experiments, not returned commands. Retain the
            // unreleased guest allocations in the receipt; do not clean them up
            // through fake native calls or count the path as resource-safe.
        }
        catch (Exception error)
        {
            LastFailure = new
            {
                caseId = active?.Definition.Id,
                instructions = active?.Instructions ?? 0,
                pc = activeCore?.State.ProgramCounter,
                sp = activeCore?.State.A[7],
                d0 = activeCore?.State.D[0],
                ioError = active is null ? (int?)null : Bus.IoError(active),
                events = active?.Events,
                calls = active?.Calls,
                failure = error.ToString()
            };
            throw new InvalidOperationException($"Original Rename {active?.Definition.Id}: {error.Message}", error);
        }
        finally
        {
            foreach (var cpu in cores) cpu.Dispose();
            Bus.Activate(null);
        }
    }

    private static uint DataSentinel(int index) => (uint)(0xde000000 + index * 16);
    private static uint AddressSentinel(int index) => (uint)(0xae000000 + index * 16);
    private static string ContractId(string id) => id.Split('.')[0];
    private static string Hex(byte[] bytes) => Convert.ToHexStringLower(bytes);

    private void Prepare(RenameInvocation invocation)
    {
        var c = invocation.Definition;
        Require(c.Sources.Length is >= 1 and <= 3 && c.StackBytes is >= 4096 and <= 32768 &&
            (c.StackBytes & 3) == 0, "Invalid finite Rename case shape.");
        foreach (var value in c.Sources.Append(c.Destination))
            Require(value.All(ch => ch is > '\0' and <= '\u00ff'), "Case text must be exact non-NUL Latin-1.");
        Bus.Activate(invocation);
        Bus.Memory.AsSpan((int)invocation.Process - 16, 0x420).Fill(0xb6);
        Bus.Memory.AsSpan((int)invocation.Process, 0x400).Clear();
        // This is supplied vector context, not a real-DOS launch fixture.
        Bus.Long(invocation.Process + (uint)DosLayout.Process.CommandLineInterface, 0x101);
        Bus.SetIoError(invocation, RenameInvocation.InitialIoError);
        invocation.ProcessSnapshot = Bus.Memory.AsSpan((int)invocation.Process, 0x400).ToArray();
        Bus.Memory.AsSpan((int)invocation.Arguments - 16, 0x420).Fill(0xb6);
        Bus.Memory.AsSpan((int)invocation.Arguments, 0x400).Clear();
        Encoding.ASCII.GetBytes("not-parsed\n").CopyTo(Bus.Memory.AsSpan((int)invocation.Arguments));
        invocation.ArgumentSnapshot = Bus.Memory.AsSpan((int)invocation.Arguments, 0x400).ToArray();
        Bus.Memory.AsSpan((int)(invocation.StackTop - invocation.StackBytes - 16),
            (int)invocation.StackBytes + 32).Fill(0xb6);
    }

    private RenameResult Verify(RenameInvocation invocation, IM68kCore cpu, bool interleaved)
    {
        var c = invocation.Definition;
        var result = unchecked((int)cpu.State.D[0]);
        var error = Bus.IoError(invocation);
        Require(result == c.ExpectedResult, $"Returned {result}, expected {c.ExpectedResult}.");
        Require(error == c.ExpectedFinalIoError, $"Final supplied IoErr {error}, expected {c.ExpectedFinalIoError}.");
        Require(invocation.StepIndex == c.Steps.Length, "An expected public-vector operation was skipped.");
        Require(cpu.State.A[7] == invocation.StackTop, "Image entry did not restore SP.");
        var restored = true;
        for (var index = 2; index < 8; index++) restored &= cpu.State.D[index] == DataSentinel(index);
        for (var index = 2; index < 7; index++) restored &= cpu.State.A[index] == AddressSentinel(index);
        Require(invocation.Opens == 1 && invocation.Closes == (c.MissingDos ? 0 : 1), "DOS lease mismatch.");
        var parserReached = !c.MissingDos && !c.BreakPending && c.FailAllocation == 0;
        Require(invocation.CheckSignalCalls == (c.MissingDos ? 0 : 1), "Unexpected CheckSignal count.");
        Require(invocation.AllocationAttempts == (c.MissingDos || c.BreakPending ? 0 : c.FailAllocation == 0 ? 4 : c.FailAllocation),
            "Allocation failure ordinal/order differs.");
        Require(invocation.ReadArgsCalls == (parserReached ? 1 : 0), "ReadArgs count differs.");
        Require(invocation.FreeArgsCalls == (parserReached && !c.ParserFails ? 1 : 0), "FreeArgs count differs.");
        Require(invocation.FreeVecCalls == (c.MissingDos ? 0 : 4) &&
            invocation.CleanupUnlockCalls == (c.MissingDos ? 0 : 1), "Partial-allocation/null cleanup differs.");
        Require(!invocation.SearchLive && invocation.SearchToken == 0 && invocation.OwnedLocks.Count == 0,
            "Returned command leaked search or lock ownership.");
        Bus.AssertReleased(invocation);
        AssertStorage(invocation);
        return new(c.Id, ContractId(c.Id), interleaved, c.SyntheticProviderCombination, true,
            result, error, c.ExpectedResult, c.ExpectedFinalIoError, invocation.Instructions, c.StackBytes,
            invocation.StackTop - invocation.LowestStackWrite, true, restored, true, invocation.Opens,
            invocation.Closes, invocation.AllocationAttempts, invocation.FreeVecCalls, invocation.ReadArgsCalls,
            invocation.FreeArgsCalls, invocation.MatchFirstCalls, invocation.MatchNextCalls, invocation.MatchEndCalls,
            Hex(invocation.AcceptedVPrintfBytes.ToArray()), invocation.Output.ToArray(), invocation.Faults.ToArray(),
            invocation.Calls.ToArray());
    }

    private void AssertStorage(RenameInvocation invocation)
    {
        foreach (var address in new[] { invocation.StackTop - invocation.StackBytes - 16, invocation.StackTop,
            invocation.Process - 16, invocation.Process + 0x400, invocation.Arguments - 16, invocation.Arguments + 0x400 })
            Require(Bus.Memory.AsSpan((int)address, 16).IndexOfAnyExcept((byte)0xb6) < 0, "Fixture guard changed.");
        Require(Bus.Memory.AsSpan((int)invocation.Arguments, 0x400).SequenceEqual(invocation.ArgumentSnapshot),
            "Unparsed entry argument bytes changed.");
        var process = (byte[])invocation.ProcessSnapshot.Clone();
        BinaryPrimitives.WriteInt32BigEndian(process.AsSpan(DosLayout.Process.Result2, 4), Bus.IoError(invocation));
        Require(Bus.Memory.AsSpan((int)invocation.Process, 0x400).SequenceEqual(process), "Unexpected Process write.");
        Bus.AssertAllocationGuards(invocation);
        Bus.AssertImageUnchanged();
    }

    private RenameStep Take(RenameInvocation invocation, RenameOperation operation)
    {
        Require(invocation.StepIndex < invocation.Definition.Steps.Length, $"Unexpected {operation} after script end.");
        var step = invocation.Definition.Steps[invocation.StepIndex];
        Require(step.Operation == operation, $"Expected {step.Operation}, observed {operation}, at script index {invocation.StepIndex}.");
        invocation.StepIndex++;
        return step;
    }

    private static void LiveDos(RenameInvocation invocation) => Require(
        invocation.Opens == 1 && invocation.Closes == 0 && !invocation.Definition.MissingDos,
        "Public DOS operation without a successful live owned OpenLibrary.");

    private void Register(uint baseAddress, short lvo, string name,
        Func<M68kCpuState, RenameInvocation, List<string>, uint> handler)
    {
        var address = checked((uint)(baseAddress + lvo));
        Bus.RegisterGateway(address, state =>
        {
            var invocation = Bus.Current ?? throw new InvalidOperationException("Vector has no active caller.");
            Require(state.A[6] == baseAddress, $"{name}: wrong A6 library base.");
            if (baseAddress != ExecBase)
            {
                Require(baseAddress == invocation.DosBase, "DOS lease crossed invocation ownership.");
                LiveDos(invocation);
            }
            var before = new[] { state.D[0], state.D[1], state.D[2], state.D[3], state.A[1], state.A[6] };
            var savedD = state.D.Skip(2).ToArray();
            var savedA = state.A.Skip(2).Take(5).ToArray();
            var oldError = Bus.IoError(invocation);
            var notes = new List<string>();
            uint? returned = null;
            invocation.Events.Add(name);
            try
            {
                returned = handler(state, invocation, notes);
                Require(state.D.Skip(2).SequenceEqual(savedD) && state.A.Skip(2).Take(5).SequenceEqual(savedA),
                    "The supplied library vector corrupted a nonvolatile register.");
                state.D[0] = returned.Value;
                state.D[1] = 0xd1d1d1d1;
                state.A[0] = 0xa0a0a0a0;
                state.A[1] = 0xa1a1a1a1;
                state.StatusRegister = (ushort)((state.StatusRegister & 0xffe0) | 0x001f);
            }
            finally
            {
                invocation.Calls.Add(new(name, address, before[0], before[1], before[2], before[3], before[4], before[5],
                    returned, oldError, Bus.IoError(invocation), returned.HasValue, notes.ToArray()));
            }
        });
    }

    private void RegisterExec()
    {
        Register(ExecBase, ExecLvo.OpenLibrary, "OpenLibrary", (s, i, n) =>
        {
            Require(i.Opens == 0 && s.D[0] == 36 && Bus.GatewayText(s.A[1]) == "dos.library", "Expected one DOS V36 open.");
            i.Opens++;
            return i.Definition.MissingDos ? 0 : i.DosBase;
        });
        Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", (s, i, n) =>
        {
            LiveDos(i);
            Require(s.A[1] == i.DosBase && i.FreeVecCalls == 4 && i.OwnedLocks.Count == 0 &&
                !i.SearchLive && i.StepIndex == i.Definition.Steps.Length, "Premature or foreign library close.");
            Bus.AssertReleased(i);
            i.Closes++;
            if (i.Definition.PoisonCleanup) Bus.SetIoError(i, 904);
            return 0xc10ced;
        });
        Register(ExecBase, ExecLvo.AllocVec, "AllocVec", (s, i, n) =>
        {
            LiveDos(i);
            var index = i.AllocationAttempts;
            Require(i.CheckSignalCalls == 1 && !i.Definition.BreakPending && i.ReadArgsCalls == 0 && index < 4 &&
                (i.Definition.FailAllocation == 0 || index < i.Definition.FailAllocation), "Unexpected allocation phase.");
            Require(s.D[0] == AllocationSizes[index] && s.D[1] == AllocationFlags[index], "Wrong original AllocVec size/flags/order.");
            i.AllocationAttempts++;
            n.Add($"kind={AllocationKinds[index]};bytes={s.D[0]};flags={s.D[1]:x8}");
            if (i.AllocationAttempts == i.Definition.FailAllocation) return 0;
            var value = Bus.Allocate(i, s.D[0], AllocationKinds[index], true, cleared: (s.D[1] & 0x10000) != 0);
            i.VecAllocations[index] = value;
            return value;
        });
        Register(ExecBase, ExecLvo.FreeVec, "FreeVec", (s, i, n) =>
        {
            LiveDos(i);
            Require(i.CleanupStarted && i.CleanupUnlockCalls == 1 && i.FreeVecCalls < 4 &&
                i.StepIndex == i.Definition.Steps.Length, "Unexpected FreeVec phase.");
            var index = FreeOrder[i.FreeVecCalls];
            Require(s.A[1] == i.VecAllocations[index], "Wrong FreeVec order, foreign pointer or BPTR conversion.");
            n.Add($"kind={AllocationKinds[index]};pointer={s.A[1]:x8}");
            if (index == 1 && i.SearchLive)
                throw new RenameReferenceGuard("search-not-ended-before-anchor-free", "supplied-vector",
                    "Original tried to free its AnchorPath while a supplied search attempt still owned storage; MatchEnd was omitted.");
            if (s.A[1] != 0) Bus.Release(i, s.A[1], AllocationKinds[index]);
            i.FreeVecCalls++;
            if (i.Definition.PoisonCleanup) Bus.SetIoError(i, 902);
            return 0xf4ee;
        });
    }

    private void RegisterDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.CheckSignal, "CheckSignal", (s, i, n) =>
        {
            Require(s.D[1] == 0x1000 && i.CheckSignalCalls == 0 && i.AllocationAttempts == 0, "Wrong initial break check.");
            i.CheckSignalCalls++;
            return i.Definition.BreakPending ? 0x1000u : 0;
        });
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", ReadArgs);
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (s, i, n) =>
        {
            Require(i.RdArgs != 0 && i.FreeArgsCalls == 0 && s.D[1] == i.RdArgs &&
                i.StepIndex == i.Definition.Steps.Length, "Foreign or premature FreeArgs.");
            Bus.Release(i, i.RdArgs, "RDArgs");
            i.FreeArgsCalls++;
            i.CleanupStarted = true;
            if (i.Definition.PoisonCleanup) Bus.SetIoError(i, 903);
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (s, i, n) =>
        {
            var step = Take(i, RenameOperation.Lock);
            Require(unchecked((int)s.D[2]) == -2 && Bus.GatewayText(s.D[1]) == step.First,
                "Lock name or ACCESS_READ differs.");
            Require(i.LockCalls == 0 ? s.D[1] == i.ToPointer : i.FromPointers.Contains(s.D[1]), "Lock did not borrow an exact parser string.");
            if (i.LockCalls++ == 0) i.DestinationLock = step.RawBptr;
            if (step.RawBptr != 0) Require(i.OwnedLocks.Add(step.RawBptr), "Supplied lock is already owned.");
            n.Add($"name={Hex(Bus.GatewayCString(s.D[1]))};rawBPTR={step.RawBptr:x8}");
            Bus.SetIoError(i, step.RawBptr == 0 ? 205 : 777);
            return step.RawBptr;
        });
        Register(baseAddress, DosLvo.Examine, "Examine", (s, i, n) =>
        {
            var step = Take(i, RenameOperation.Examine);
            Require(s.D[1] == step.RawBptr && i.OwnedLocks.Contains(s.D[1]) && (s.D[2] & 3) == 0 &&
                Bus.IsOwnStack(i, s.D[2], 260), "Examine raw BPTR/FIB alignment or ownership differs.");
            Bus.Memory.AsSpan((int)s.D[2], 260).Clear();
            Bus.Long(s.D[2] + 4, unchecked((uint)step.EntryType));
            n.Add($"fibDirEntryType={step.EntryType}");
            Bus.SetIoError(i, 777);
            return unchecked((uint)step.Result);
        });
        Register(baseAddress, DosLvo.SameLock, "SameLock", (s, i, n) =>
        {
            var step = Take(i, RenameOperation.SameLock);
            Require(s.D[1] == step.RawBptr && s.D[2] == step.OtherBptr && i.OwnedLocks.Contains(s.D[1]) &&
                i.OwnedLocks.Contains(s.D[2]), "SameLock did not receive the exact owned raw BPTRs.");
            return unchecked((uint)step.Result);
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", UnLock);
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", MatchFirst);
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", MatchNext);
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", MatchEnd);
        Register(baseAddress, DosLvo.ParsePattern, "ParsePattern", (s, i, n) =>
        {
            var step = Take(i, RenameOperation.ParsePattern);
            Require(Bus.GatewayText(s.D[1]) == step.First && i.FromPointers.Contains(s.D[1]) &&
                s.D[2] == i.SourceBuffer && s.D[3] == 256, "ParsePattern pointer/capacity differs.");
            if (step.WriteOutput) WriteCString(i, i.SourceBuffer, "source", step.Second!);
            n.Add($"input={Hex(Bus.GatewayCString(s.D[1]))};suppliedOutput={(step.Second is null ? "unterminated" : Hex(Encoding.Latin1.GetBytes(step.Second)))}");
            Bus.SetIoError(i, step.Result < 0 ? 120 : 777);
            return unchecked((uint)step.Result);
        });
        Register(baseAddress, DosLvo.NameFromLock, "NameFromLock", (s, i, n) =>
        {
            var step = Take(i, RenameOperation.NameFromLock);
            Require(s.D[1] == step.RawBptr && i.OwnedLocks.Contains(s.D[1]) &&
                s.D[2] == i.DestinationBuffer && s.D[3] == 256, "NameFromLock pointer/capacity differs.");
            if (step.WriteOutput) WriteCString(i, i.DestinationBuffer, "destination", step.First!);
            n.Add($"suppliedPrefix={(step.First is null ? "unchanged-cleared-buffer" : Hex(Encoding.Latin1.GetBytes(step.First)))}");
            Bus.SetIoError(i, step.Result == 0 ? 214 : 777);
            return unchecked((uint)step.Result);
        });
        Register(baseAddress, DosLvo.Rename, "Rename", (s, i, n) =>
        {
            var step = Take(i, RenameOperation.Rename);
            var oldName = Bus.GatewayCString(s.D[1]);
            var newName = Bus.GatewayCString(s.D[2]);
            Require(Encoding.Latin1.GetString(oldName) == step.First && Encoding.Latin1.GetString(newName) == step.Second,
                "Rename paths differ from the supplied independent script.");
            Require(s.D[1] == i.SourceBuffer && (s.D[2] == i.ToPointer || s.D[2] == i.DestinationBuffer),
                "Rename did not use the owned copied source and expected destination storage.");
            n.Add($"old={Hex(oldName)};new={Hex(newName)};suppliedOnly=true");
            Bus.SetIoError(i, step.IoError);
            return unchecked((uint)step.Result);
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (s, i, n) =>
        {
            var step = Take(i, RenameOperation.IoErr);
            var error = Bus.IoError(i);
            Require(error == step.Result, "Wrong immediate IoErr capture/order.");
            return unchecked((uint)error);
        });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (s, i, n) =>
        {
            var step = Take(i, RenameOperation.SetIoErr);
            Require(unchecked((int)s.D[1]) == step.Result, "Selected IoErr was not restored before common handling.");
            var previous = Bus.IoError(i);
            Bus.SetIoError(i, step.Result);
            return unchecked((uint)previous);
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", VPrintf);
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (s, i, n) =>
        {
            var step = Take(i, RenameOperation.PrintFault);
            Require(s.D[2] == 0 && unchecked((int)s.D[1]) == step.IoError, "Wrong PrintFault selected code/header.");
            i.Faults.Add(new(step.IoError, s.D[2], step.Result));
            Bus.SetIoError(i, step.IoError);
            return unchecked((uint)step.Result);
        });
    }

    private uint ReadArgs(M68kCpuState s, RenameInvocation i, List<string> notes)
    {
        Require(i.ReadArgsCalls == 0 && i.AllocationAttempts == 4 && i.VecAllocations.All(a => a != 0) &&
            Bus.GatewayText(s.D[1]) == "FROM/A/M,TO=AS/A,QUIET/S" && s.D[3] == 0,
            "ReadArgs template, optional source or acquisition phase differs.");
        Require((s.D[2] & 3) == 0 && Bus.IsOwnStack(i, s.D[2], 12) &&
            Enumerable.Range(0, 3).All(index => Bus.GatewayLong(s.D[2] + (uint)index * 4) == 0),
            "ReadArgs requires three cleared, aligned stack result cells.");
        i.ReadArgsCalls++;
        i.ResultSlots = s.D[2];
        if (i.Definition.ParserFails)
        {
            Bus.SetIoError(i, i.Definition.ParserError);
            return 0;
        }
        var strings = i.Definition.Sources.Append(i.Definition.Destination).ToArray();
        var vectorBytes = (i.Definition.Sources.Length + 1) * 4;
        var bytes = 32 + vectorBytes + strings.Sum(t => Encoding.Latin1.GetByteCount(t) + 1);
        i.RdArgs = Bus.Allocate(i, checked((uint)bytes), "RDArgs", false);
        i.FromVector = i.RdArgs + 32;
        i.FromPointers = new uint[i.Definition.Sources.Length];
        var next = i.FromVector + (uint)vectorBytes;
        for (var index = 0; index < strings.Length; index++)
        {
            var value = Encoding.Latin1.GetBytes(strings[index]);
            value.CopyTo(Bus.Memory.AsSpan((int)next));
            if (index < i.FromPointers.Length)
            {
                i.FromPointers[index] = next;
                Bus.Long(i.FromVector + (uint)index * 4, next);
            }
            else i.ToPointer = next;
            notes.Add($"suppliedString{index}={Hex(value)}");
            next += (uint)value.Length + 1;
        }
        Bus.Long(i.ResultSlots, i.FromVector);
        Bus.Long(i.ResultSlots + 4, i.ToPointer);
        Bus.Long(i.ResultSlots + 8, i.Definition.Quiet);
        Bus.Seal(i, i.RdArgs, "RDArgs");
        Bus.SetIoError(i, 777);
        return i.RdArgs;
    }

    private uint UnLock(M68kCpuState s, RenameInvocation i, List<string> notes)
    {
        if (i.StepIndex < i.Definition.Steps.Length && i.Definition.Steps[i.StepIndex].Operation == RenameOperation.UnLock)
        {
            var step = Take(i, RenameOperation.UnLock);
            Require(s.D[1] == step.RawBptr && s.D[1] != 0 && s.D[1] != i.DestinationLock && i.OwnedLocks.Remove(s.D[1]),
                "Temporary source lock was not released exactly once.");
            notes.Add("temporary-source-lock");
        }
        else
        {
            Require(i.StepIndex == i.Definition.Steps.Length && i.CleanupUnlockCalls == 0 &&
                (i.RdArgs == 0 || i.FreeArgsCalls == 1) && s.D[1] == i.DestinationLock,
                "Cleanup destination/null UnLock order differs.");
            if (s.D[1] != 0) Require(i.OwnedLocks.Remove(s.D[1]), "Foreign or duplicate destination unlock.");
            i.CleanupUnlockCalls++;
            i.CleanupStarted = true;
            notes.Add("cleanup-destination-or-null-lock");
        }
        if (i.Definition.PoisonCleanup) Bus.SetIoError(i, 901);
        return 0xdead;
    }

    private uint MatchFirst(M68kCpuState s, RenameInvocation i, List<string> notes)
    {
        var step = Take(i, RenameOperation.MatchFirst);
        Require(i.RdArgs != 0 && i.FreeArgsCalls == 0 && s.D[2] == i.Anchor && i.FromPointers.Contains(s.D[1]) &&
            Bus.GatewayText(s.D[1]) == step.First, "MatchFirst pointer/argument/lifetime differs.");
        Require(Bus.Long(i.Anchor + 8) == 0x1000 && (Bus.Memory[(int)i.Anchor + 16] & 1) != 0 &&
            Bus.Word(i.Anchor + 18) == 255, "Original AnchorPath break mask, DOWILD or path capacity differs.");
        Require(!i.SearchLive && i.SearchToken == 0, "Search restarted without ending the previous attempt.");
        i.MatchFirstCalls++;
        i.SearchLive = true;
        i.SearchToken = Bus.Allocate(i, 16, "Search", false);
        Bus.Seal(i, i.SearchToken, "Search");
        if (step.Result == 0) FillMatch(i, step.FullPath!, step.Leaf!, step.Wild);
        notes.Add($"pattern={Hex(Bus.GatewayCString(s.D[1]))};attempt={i.MatchFirstCalls};opaqueSuppliedSearch={i.SearchToken:x8}");
        Bus.SetIoError(i, step.IoError);
        return unchecked((uint)step.Result);
    }

    private uint MatchNext(M68kCpuState s, RenameInvocation i, List<string> notes)
    {
        var step = Take(i, RenameOperation.MatchNext);
        Require(i.SearchLive && s.D[1] == i.Anchor, "MatchNext has no live owned search.");
        i.MatchNextCalls++;
        // Also overwrite fields at end/error, so delayed path copying cannot
        // accidentally pass because the previous match remained unchanged.
        FillMatch(i, step.FullPath!, step.Leaf!, (Bus.Memory[(int)i.Anchor + 16] & 2) != 0);
        notes.Add($"suppliedNextPath={Hex(Encoding.Latin1.GetBytes(step.FullPath!))};suppliedResult={step.Result}");
        Bus.SetIoError(i, step.IoError);
        return unchecked((uint)step.Result);
    }

    private uint MatchEnd(M68kCpuState s, RenameInvocation i, List<string> notes)
    {
        _ = Take(i, RenameOperation.MatchEnd);
        Require(s.D[1] == i.Anchor && i.Anchor != 0, "MatchEnd anchor differs.");
        i.MatchEndCalls++;
        if (i.MatchFirstCalls == 0)
            throw new RenameReferenceGuard("match-end-before-match-first", "supplied-vector",
                "Original called MatchEnd on a cleared AnchorPath which never received MatchFirst; original DOS behavior is unqualified.");
        if (!i.SearchLive)
            throw new RenameReferenceGuard("repeated-match-end", "supplied-vector",
                "Original called MatchEnd after this search was already ended; no idempotent provider behavior is assumed.");
        Bus.Release(i, i.SearchToken, "Search");
        i.SearchToken = 0;
        i.SearchLive = false;
        Bus.SetIoError(i, 777);
        return 0xeeee;
    }

    private void FillMatch(RenameInvocation i, string fullPath, string leaf, bool wild)
    {
        _ = Bus.OwnedAllocation(i, i.Anchor, "anchor");
        var full = Encoding.Latin1.GetBytes(fullPath);
        var name = Encoding.Latin1.GetBytes(leaf);
        Require(full.Length < 255 && name.Length < 108, "Supplied matcher output exceeds the documented fields.");
        Bus.Memory.AsSpan((int)i.Anchor + 28, 108).Clear();
        Bus.Memory.AsSpan((int)i.Anchor + 280, 258).Clear();
        name.CopyTo(Bus.Memory.AsSpan((int)i.Anchor + 28));
        full.CopyTo(Bus.Memory.AsSpan((int)i.Anchor + 280));
        Bus.Memory[(int)i.Anchor + 16] = (byte)((Bus.Memory[(int)i.Anchor + 16] & ~2) | (wild ? 2 : 0));
    }

    private void WriteCString(RenameInvocation i, uint address, string kind, string text)
    {
        var allocation = Bus.OwnedAllocation(i, address, kind);
        var bytes = Encoding.Latin1.GetBytes(text);
        Require(bytes.Length + 1 <= allocation.Size, "Supplied vector would overrun its output allocation.");
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[(int)address + bytes.Length] = 0;
    }

    private uint VPrintf(M68kCpuState s, RenameInvocation i, List<string> notes)
    {
        var step = Take(i, RenameOperation.VPrintf);
        Require(i.RdArgs != 0 && i.FreeArgsCalls == 0 && (s.D[2] & 3) == 0 &&
            Bus.IsOwnStack(i, s.D[2], step.Second is null ? 4 : 8), "VPrintf argument vector/lifetime differs.");
        var format = Bus.GatewayCString(s.D[1]);
        Require(Encoding.Latin1.GetString(format) == step.Format, "Command-owned VPrintf format differs.");
        var pointers = new List<uint>();
        var values = new List<byte[]>();
        foreach (var expected in step.Second is null ? new[] { step.First! } : new[] { step.First!, step.Second })
        {
            var pointer = Bus.GatewayLong(s.D[2] + (uint)pointers.Count * 4);
            var value = Bus.GatewayCString(pointer);
            Require(Encoding.Latin1.GetString(value) == expected, "VPrintf borrowed/copied name bytes differ.");
            pointers.Add(pointer);
            values.Add(value);
        }
        byte[] rendered = step.Format switch
        {
            "Can't rename %s as %s because " => [.. Encoding.ASCII.GetBytes("Can't rename "), .. values[0],
                .. Encoding.ASCII.GetBytes(" as "), .. values[1], .. Encoding.ASCII.GetBytes(" because ")],
            "Destination \"%s\" is not a directory.\n" => [.. Encoding.ASCII.GetBytes("Destination \""), .. values[0],
                .. Encoding.ASCII.GetBytes("\" is not a directory.\n")],
            "Renaming %s as %s\n" => [.. Encoding.ASCII.GetBytes("Renaming "), .. values[0],
                .. Encoding.ASCII.GetBytes(" as "), .. values[1], 10],
            _ => throw new InvalidOperationException("Unverified output format.")
        };
        Require(rendered.SequenceEqual(Encoding.Latin1.GetBytes(step.Rendered!)), "Independent expected output bytes differ.");
        var returned = step.OutputCount ?? rendered.Length;
        Require(returned >= -1 && returned <= rendered.Length, "Invalid supplied output result.");
        var accepted = rendered.AsSpan(0, Math.Max(0, returned)).ToArray();
        i.AcceptedVPrintfBytes.AddRange(accepted);
        i.Output.Add(new(Hex(format), s.D[2], pointers.ToArray(), values.Select(Hex).ToArray(),
            Hex(rendered), Hex(accepted), returned));
        Bus.SetIoError(i, returned == rendered.Length ? 903 : 221);
        return unchecked((uint)returned);
    }
}
