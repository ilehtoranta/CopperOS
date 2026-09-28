using System.Security.Cryptography;
using System.Text.Json;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>Executes generated hook code against bounded, supplied public vectors.</summary>
internal static class LoadResourceHookRuntimeSuite
{
    public const string Suite = "loadresource-wb31-hook-runtime";
    private static readonly string[] Cases = ["one-shot-alias-remove-idle", "duplicate-install",
        "newer-patch-retains-state", "missing-published-worker", "missing-dos",
        "state-allocation-failure", "record-allocation-failure", "cache-lock-failure",
        "input-lock-failure-delegates", "invalid-null-predecessor-rollback",
        "busy-teardown-retains-state-and-retries"];

    public static int Run(string[] args)
    {
        var reportPath = Path.GetFullPath(args[2]);
        Require(!string.Equals(reportPath, Path.GetFullPath(args[0]), StringComparison.OrdinalIgnoreCase) && !File.Exists(reportPath),
            "Use a fresh report path, different from the input HUNK.");
        var observations = new List<object>();
        string? failure = null, hunkHash = null;
        try
        {
            hunkHash = Hash(args[0]);
            var model = args[1] switch { "68000" => M68kCpuModel.M68000,
                "68020" => M68kCpuModel.M68020, "68040" => M68kCpuModel.M68040,
                _ => throw new ArgumentException("Unknown CPU.") };
            var image = HunkImage.Load(args[0], Fixture.Load);
            for (var scenario = 0; scenario < Cases.Length; scenario++)
                observations.Add(new Fixture(image, model, scenario).Execute());
            Require(Hash(args[0]) == hunkHash, "Input changed during execution.");
        }
        catch (Exception error) { failure = error.ToString(); }
        var passed = failure is null && observations.Count == Cases.Length;
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new {
            schemaVersion = 1, suite = Suite, status = passed ? "passed" : "failed",
            failure, cpu = args[1], hunkSha256 = hunkHash,
            executorSha256 = Hash(typeof(Program).Assembly.Location),
            cpuAssemblySha256 = Hash(typeof(M68kCoreFactory).Assembly.Location),
            realKickstart = false, realFilesystemHandler = false, realScheduling = false,
            shippingOrPureApproval = false, crossInvocationCodeLifetimeProven = false,
            scope = "Generated HUNK lifecycle in supplied Exec/DOS vectors: original saved-vector ABI, alias-based SameLock, one-shot ownership transfer, explicit removal, duplicate install rejection, idle teardown, failure cleanup and newer-patch retention. Full worker integration, scheduling races, reference differential and guest parity remain open.",
            observations
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"{(passed ? "passed" : "failed")}: {Suite} {observations.Count}/{Cases.Length}; {reportPath}");
        if (failure is not null) Console.Error.WriteLine(failure);
        return passed ? 0 : 1;
    }

    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    private sealed class Fixture
    {
        public const uint Load = 0x100000;
        private const uint ExecBase = 0x4000, DosBase = 0x8000, Return = 0x2000,
            SavedVector = 0x2400, NewerVector = 0x2800, PriorUserData = 0x12345678;
        private readonly CommandTestBus bus = new();
        private readonly Invocation owner;
        private readonly M68kCpuModel model;
        private readonly int scenario, codeLength;
        private readonly HashSet<uint> gateways = [];
        private readonly Dictionary<uint, string> locks = [];
        private readonly List<uint> unloaded = [];
        private readonly List<string> events = [];
        private readonly uint task, port;
        private uint vector = SavedVector, hook, nextLock = 0x900, state;
        private int forbidDepth, semaphoreDepth, allocations, opens, closes, savedCalls, semaphoreAttempts;
        private bool retainedNewerState, retainedBusyState;

        public Fixture(HunkImage image, M68kCpuModel model, int scenario)
        {
            this.model = model; this.scenario = scenario; codeLength = image.Code.Length;
            if (scenario == 9) vector = 0;
            owner = new Invocation(new ProbeCase(Cases[scenario], "", 0, 0, ""), 0);
            bus.Current = owner; bus.LoadAndProtect(Load, image.Code); bus.Long(4, ExecBase);
            // Model the published worker and its port as fixture-owned Exec objects.
            // This permits only owned data writes; the entire native image is read-only.
            task = bus.Allocate(owner, 0x400, "FixtureTask", true);
            port = bus.Allocate(owner, 34, "FixturePort", true);
            bus.Long(task + (uint)ExecLayout.Task.UserData, PriorUserData);
            bus.Long(port + (uint)ExecLayout.MsgPort.SignalTask, task);
            bus.Long(ExecBase + (uint)ExecLayout.ExecBase.ThisTask, task);

            Register(ExecBase, ExecLvo.FindTask, "FindTask", s => {
                Require(s.A[1] == 0, "FindTask must select current worker."); return task;
            });
            Register(ExecBase, ExecLvo.FindPort, "FindPort", s => {
                Require(forbidDepth > 0 && bus.CString(s.A[1]) == "\u00ab LoadResource \u00bb", "FindPort ABI/serialization.");
                return scenario == 3 ? 0 : port;
            });
            Register(ExecBase, ExecLvo.Forbid, "Forbid", _ => { forbidDepth++; return 0; });
            Register(ExecBase, ExecLvo.Permit, "Permit", _ => {
                Require(forbidDepth > 0, "Unbalanced Permit."); forbidDepth--; return 0;
            });
            Register(ExecBase, ExecLvo.OpenLibrary, "OpenLibrary", s => {
                Require(s.D[0] == 39 && bus.CString(s.A[1]) == "dos.library", "DOS V39 open ABI.");
                opens++; return scenario == 4 ? 0 : DosBase;
            });
            Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", s => {
                Require(s.A[1] == DosBase && closes < opens && scenario != 4, "DOS close ownership.");
                Require(scenario != 10 || semaphoreAttempts != 1, "Failed nonblocking attempt closed retained DOS.");
                closes++; return 0;
            });
            Register(ExecBase, ExecLvo.AllocVec, "AllocVec", s => {
                allocations++;
                Require(s.D[0] is 84 or 16, "Unexpected hook allocation size.");
                Require(s.D[1] == (s.D[0] == 84 ? 0x10001u : 0), "AllocVec flags.");
                if (scenario == 5 || scenario == 6 && s.D[0] == 16) return 0;
                var address = bus.Allocate(owner, s.D[0], "Hook", (s.D[1] & 0x10000) != 0);
                if (state == 0 && s.D[0] == 84) state = address;
                return address;
            });
            Register(ExecBase, ExecLvo.FreeVec, "FreeVec", s => {
                Require(semaphoreDepth == 0, "Allocation freed while semaphore held.");
                Require(scenario != 10 || semaphoreAttempts != 1, "Failed nonblocking attempt freed retained state.");
                if (s.A[1] == state) Require(vector == (scenario == 9 ? 0 : SavedVector) && bus.Long(task + (uint)ExecLayout.Task.UserData) == PriorUserData,
                    "State freed before hook unlink and task restoration.");
                bus.Release(owner, s.A[1], "Hook"); return 0;
            });
            Register(ExecBase, ExecLvo.InitSemaphore, "InitSemaphore", s => {
                Require(bus.OwnedAllocationContaining(owner, s.A[0], "Hook").Address + 14 == s.A[0], "Semaphore placement."); return 0;
            });
            Register(ExecBase, ExecLvo.ObtainSemaphore, "ObtainSemaphore", s => {
                Require(s.A[0] == state + 14 && semaphoreDepth == 0, "Semaphore identity/recursion."); semaphoreDepth++; return 0;
            });
            Register(ExecBase, ExecLvo.AttemptSemaphore, "AttemptSemaphore", s => {
                Require(s.A[0] == state + 14 && semaphoreDepth == 0 && forbidDepth == 0,
                    "Nonblocking teardown semaphore ABI/ownership/order.");
                semaphoreAttempts++;
                if (scenario == 10)
                {
                    if (semaphoreAttempts == 1) return 0;
                    Require(semaphoreAttempts == 2 && vector == hook && closes == 0 &&
                        bus.Long(task + (uint)ExecLayout.Task.UserData) == state,
                        "Failed nonblocking teardown changed vector, task state or DOS ownership.");
                    bus.OwnedAllocation(owner, state, "Hook"); retainedBusyState = true;
                }
                semaphoreDepth++; return 1;
            });
            Register(ExecBase, ExecLvo.ReleaseSemaphore, "ReleaseSemaphore", s => {
                Require(s.A[0] == state + 14 && semaphoreDepth == 1, "Semaphore release balance."); semaphoreDepth--; return 0;
            });
            Register(ExecBase, ExecLvo.AddTail, "AddTail", s => {
                Require(semaphoreDepth == 1 && s.A[0] == state, "AddTail outside cache lock.");
                var tail = state + 4; var previous = bus.Long(state + 8); var node = s.A[1];
                bus.Long(node, tail); bus.Long(node + 4, previous); bus.Long(previous, node); bus.Long(state + 8, node); return 0;
            });
            Register(ExecBase, ExecLvo.Remove, "Remove", s => {
                Require(semaphoreDepth == 1, "Remove outside cache lock.");
                var node = s.A[1]; var next = bus.Long(node); var previous = bus.Long(node + 4);
                bus.Long(previous, next); bus.Long(next + 4, previous); return 0;
            });
            Register(ExecBase, ExecLvo.SetFunction, "SetFunction", s => {
                Require(s.A[1] == DosBase && unchecked((int)s.A[0]) == -150, "SetFunction library/offset ABI.");
                Require(scenario != 10 || semaphoreAttempts != 1, "Failed nonblocking attempt changed the DOS vector.");
                var previous = vector; vector = s.D[0];
                if (hook == 0) { Require(vector >= Load && vector < Load + codeLength, "Installed vector not in HUNK."); hook = vector; }
                if (scenario == 2 && previous == NewerVector && vector == hook)
                {
                    Require(bus.Long(task + (uint)ExecLayout.Task.UserData) == state && closes == 0,
                        "Failed teardown released state or DOS behind a newer patch.");
                    bus.OwnedAllocation(owner, state, "Hook"); retainedNewerState = true;
                }
                events.Add($"vector:{previous:X8}->{vector:X8}"); return previous;
            });
            Register(DosBase, DosLvo.Lock, "Lock", s => {
                Require(unchecked((int)s.D[2]) == -2, "Lock must be shared.");
                Require(semaphoreDepth == 0, "Filesystem Lock ran while the registry semaphore was held.");
                var path = bus.CString(s.D[1]); events.Add("path:" + path);
                if (scenario == 7 || path == "unavailable") return 0;
                var lock_ = nextLock++; locks.Add(lock_, path == "alias" ? "one" : path); return lock_;
            });
            Register(DosBase, DosLvo.UnLock, "UnLock", s => {
                Require(locks.Remove(s.D[1]), "Unknown or duplicate lock release."); return 0;
            });
            Register(DosBase, DosLvo.SameLock, "SameLock", s => {
                Require(semaphoreDepth == 1 && locks.ContainsKey(s.D[1]) && locks.ContainsKey(s.D[2]), "SameLock lifetime.");
                return locks[s.D[1]] == locks[s.D[2]] ? 0u : 1u;
            });
            Register(DosBase, DosLvo.UnLoadSeg, "UnLoadSeg", s => {
                Require(s.D[1] == 0x222 && semaphoreDepth == 0, "Wrong segment unloaded or semaphore retained."); unloaded.Add(s.D[1]); return 0;
            });
            gateways.Add(SavedVector);
            bus.RegisterGateway(SavedVector, s => {
                Require(s.A[6] == DosBase && opens > closes && semaphoreDepth == 0,
                    "Saved LoadSeg vector ABI/lease/lock.");
                Require(bus.Long(state + 60) == 1, "Saved vector must run inside active-operation lifetime.");
                var path = bus.CString(s.D[1]);
                Require(path is "one" or "different" or "missing" or "unavailable", "Saved vector filename was corrupted.");
                events.Add("saved:" + path); savedCalls++; Clobber(s, 0x777);
            });
        }

        public object Execute()
        {
            bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), (int)owner.StackBytes + 32).Fill(0xb6);
            using var cpu = M68kCoreFactory.Default.Create(model, bus);
            cpu.State.StatusRegister = 0; cpu.BeginSubroutine(Load, owner.StackTop, Return);
            for (var index = 0; index < 8; index++) cpu.State.D[index] = (uint)(0xde000000 + index * 16);
            for (var index = 0; index < 7; index++) cpu.State.A[index] = (uint)(0xae000000 + index * 16);
            cpu.State.D[0] = (uint)scenario; cpu.State.A[0] = 0;
            uint[]? hookRegisters = null;
            uint hookReturn = 0, hookStack = 0;
            var checkedHookReturns = 0;
            try
            {
                while (cpu.State.ProgramCounter != Return)
                {
                    var pc = cpu.State.ProgramCounter;
                    if (hookRegisters is not null && pc == hookReturn)
                    {
                        Require(cpu.State.D.Skip(2).SequenceEqual(hookRegisters.Take(6)) &&
                            cpu.State.A.Skip(2).Take(5).SequenceEqual(hookRegisters.Skip(6)) &&
                            cpu.State.A[7] == hookStack + 4,
                            "LoadSeg export did not preserve D2-D7/A2-A6 or caller stack.");
                        hookRegisters = null; checkedHookReturns++;
                    }
                    if (hook != 0 && pc == hook)
                    {
                        Require(hookRegisters is null, "Unexpected recursive hook invocation.");
                        hookRegisters = cpu.State.D.Skip(2).Concat(cpu.State.A.Skip(2).Take(5)).ToArray();
                        hookStack = cpu.State.A[7]; hookReturn = bus.Long(hookStack);
                    }
                    Require(pc >= Load && pc < Load + codeLength || gateways.Contains(pc), $"Unexpected PC ${pc:X8}.");
                    Require(++owner.Instructions < 100_000 && !cpu.State.Halted && !cpu.State.Stopped, "Execution did not return.");
                    cpu.ExecuteInstruction();
                }
                Require(cpu.State.D[0] == 0, $"Native lifecycle rejected stage {cpu.State.D[0]}.");
                Require(cpu.State.A[7] == owner.StackTop && forbidDepth == 0 && semaphoreDepth == 0, "Stack/scheduling/semaphore balance.");
                Require(bus.Memory.AsSpan((int)owner.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0 &&
                    bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), 16).IndexOfAnyExcept((byte)0xb6) < 0, "Stack guard changed.");
                Require(vector == (scenario == 9 ? 0 : SavedVector) && locks.Count == 0 &&
                    bus.Long(task + (uint)ExecLayout.Task.UserData) == PriorUserData, "Final vector, locks or task data leaked.");
                Require(opens == (scenario == 3 ? 0 : scenario == 1 ? 2 : 1) && closes == (scenario == 4 ? 0 : opens), "Final DOS lease count.");
                Require(savedCalls == (scenario == 0 ? 2 : scenario is 1 or 2 or 10 ? 1 : scenario == 8 ? 3 : 0), "Saved-vector call count.");
                Require(checkedHookReturns == (scenario == 0 ? 3 : scenario is 1 or 2 or 10 ? 1 : scenario == 8 ? 4 : 0),
                    "Exported hook ABI checks were not completed.");
                Require(semaphoreAttempts == (scenario is 3 or 4 or 5 or 9 ? 0 : scenario is 0 or 2 or 8 or 10 ? 2 : 1),
                    "Teardown did not use the expected nonblocking semaphore attempts.");
                Require(unloaded.SequenceEqual(scenario is 0 or 8 ? new[] { 0x222u } : Array.Empty<uint>()), "One-shot/remove ownership differs.");
                Require(scenario != 2 || retainedNewerState, "Newer patch retention was not exercised.");
                Require(scenario != 10 || retainedBusyState, "Busy teardown retention and retry were not exercised.");
                bus.Release(owner, task, "FixtureTask"); bus.Release(owner, port, "FixturePort");
                bus.AssertReleased(owner); bus.AssertImageUnchanged();
                return new { id = Cases[scenario], result = 0, owner.Instructions, allocations, opens, closes,
                    savedCalls, checkedHookReturns, semaphoreAttempts, unloaded, retainedNewerState,
                    retainedBusyState, imageUnchanged = true, events };
            }
            catch (Exception error) { throw new InvalidOperationException($"{Cases[scenario]} PC={cpu.State.ProgramCounter:X8}; events={string.Join(',', events)}", error); }
        }

        private void Register(uint library, short offset, string name, Func<M68kCpuState, uint> handler)
        {
            var address = checked((uint)(library + offset)); gateways.Add(address);
            bus.RegisterGateway(address, s => {
                Require(s.A[6] == library, $"{name}: wrong library base ${s.A[6]:X8}.");
                if (library == DosBase) Require(opens > closes, "DOS vector used outside retained lease.");
                events.Add(name); Clobber(s, handler(s));
            });
        }

        private static void Clobber(M68kCpuState state, uint result)
        {
            state.D[0] = result; state.D[1] = 0xd1d1d1d1;
            state.A[0] = 0xa0a0a0a0; state.A[1] = 0xa1a1a1a1;
            state.StatusRegister = (ushort)((state.StatusRegister & 0xffe0) | 0x001f);
        }
    }
}
