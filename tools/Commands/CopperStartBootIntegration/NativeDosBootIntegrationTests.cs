using System.Security.Cryptography;
using System.Reflection;
using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amiga;
using Copper68k;
using CopperMod.Amiga;
using CopperMod.Amiga.Bus;
using CopperMod.Amiga.Runtime;
using CopperMod.Amiga.Core;
using CopperMod.Amiga.Firmware;
using AmigaBus = CopperMod.Amiga.Bus.Bus;
using CopperMod.Amiga.CopperStart.Dos;

namespace CopperMod.Amiga.Tests;

/// <summary>Opt-in complete-emulator integration; no fixture Exec or packet gateway.</summary>
public sealed class NativeDosBootIntegrationTests
{
    private const string CommandHash = "12d1f36504f5a0f7099bc419e4fffc5f43f138d7b4e8745a2ca907bcb73b5f45";
    private const string DosHash = "64c100d23012526df36f7af0950be0aefe48a33f8d4b4684794d772b67bc2f76";

    [Fact]
    public void ProductionBootInstallsNativeDosAndLaunchesPublicCliMakeLink()
    {
        var output = Path.GetFullPath(Required("COPPER_BOOT_INTEGRATION_OUTPUT"));
        Assert.True(Directory.Exists(output));
        var stage = Required("COPPER_BOOT_INTEGRATION_STAGE");
        var sessions = int.Parse(Environment.GetEnvironmentVariable("COPPER_BOOT_SESSION_COUNT") ?? "1");
        Assert.InRange(sessions, 1, 8);
        Assert.Contains(stage, new[] { "install", "command" });
        var dosPath = Path.GetFullPath(Required("COPPER_BOOT_NATIVE_DOS"));
        var commandPath = Path.GetFullPath(Required("COPPER_BOOT_MAKELINK"));
        Assert.Equal(DosHash, Hash(dosPath));
        Assert.Equal(CommandHash, Hash(commandPath));
        var hostRoot = Path.Combine(output, "host-volume");
        var metadataRoot = Path.Combine(output, "metadata");
        Assert.False(Directory.Exists(hostRoot));
        Directory.CreateDirectory(hostRoot);
        File.WriteAllText(Path.Combine(hostRoot, "target"), "production-boot-payload\n", new UTF8Encoding(false));
        for (var session = 2; session <= sessions; session++)
            File.WriteAllText(Path.Combine(hostRoot, $"session-{session}-target"), "production-boot-payload\n", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(hostRoot, "caller"), "caller-input\n", new UTF8Encoding(false));
        File.Copy(commandPath, Path.Combine(hostRoot, "MakeLink"));

        var machine = new Machine(MachineOptions.ForProfile(MachineProfile.A500Pal512KBoot)
            .WithRealFastRam(8 * 1024 * 1024).WithLiveAgnusDma(false));
        var boot = new AmigaBootController(machine, configuredDosVolumes:
            new[] { new DosHostVolumeMount("DH0", hostRoot, metadataRoot, writable: true) });
        var observations = new List<object>();
        var bootstrapTrace = new List<object>();
        var bootstrapFrees = new List<object>();
        var initResidentCalls = new List<object>();
        var libraryContinuations = new List<object>();
        var libraryOpenEntries = new List<object>();
        uint observedNativeLibrary = 0;
        var pendingResidentReturns = new List<(uint Address, uint Stack)>();
        var registryRanges = File.ReadLines(Path.ChangeExtension(dosPath, ".map"))
            .Select(line => Regex.Match(line, @"^([0-9A-F]{8})\s+(\d+)\s+(CopperStart\.Dos\.(?:DosCliRegistryCore::(?:TryCreate|TryValidate|ReadRangeHeader|ReadPublicRange)|DosNativeOwnedLibraryCore::(?:CompleteInstall|InitializeOwnedDirect|BindDirectLibrary|ValidateBoundFast))[^ ]*)$"))
            .Where(match => match.Success)
            .Select(match => (Offset: Convert.ToUInt32(match.Groups[1].Value, 16), Bytes: uint.Parse(match.Groups[2].Value), Name: match.Groups[3].Value)).ToArray();
        boot.NativeDosBootstrapInstructionObserver = state =>
        {
            var pc = state.ProgramCounter;
            string Bytes(uint address, int count) => machine.Bus.IsMappedMemoryRange(address, count)
                ? Convert.ToHexStringLower(Enumerable.Range(0, count).Select(i => machine.Bus.ReadByte(address + (uint)i)).ToArray()) : "unmapped";
            if (pc is 0x00F08400 or 0x00F08410 or 0x00F08300)
                libraryContinuations.Add(new { pc, gateway = machine.Bus.HasHostGateway(pc), opcode = machine.Bus.ReadWord(pc),
                    d = state.D.ToArray(), a = state.A.ToArray(), stack = Bytes(state.A[7], 32) });
            if (observedNativeLibrary != 0 && (pc == observedNativeLibrary - 6 ||
                pc == machine.Bus.ReadLong(observedNativeLibrary - 4)))
                libraryOpenEntries.Add(new { pc, nativeLibrary = observedNativeLibrary, d0 = state.D[0], a6 = state.A[6],
                    d = state.D.ToArray(), a = state.A.ToArray(), stack = Bytes(state.A[7], 32) });
            for (var index = pendingResidentReturns.Count - 1; index >= 0; index--)
                if (pc == pendingResidentReturns[index].Address && state.A[7] == pendingResidentReturns[index].Stack)
                {
                    initResidentCalls.Add(new { checkpoint = "return", pc, d0 = state.D[0], a6 = state.A[6],
                        libraryBytes = Bytes(state.D[0], 64), execLibraryList = Bytes(machine.Bus.ReadLong(4) + ExecLayout.ExecBase.LibraryList, 14) });
                    observedNativeLibrary = state.D[0];
                    pendingResidentReturns.RemoveAt(index);
                }
            if (pc == unchecked(machine.Bus.ReadLong(4) + (uint)ExecLvo.InitResident))
            {
                var resident = state.A[1];
                var autoInit = machine.Bus.IsMappedMemoryRange(resident, 26) ? machine.Bus.ReadLong(resident + 22) : 0;
                initResidentCalls.Add(new { checkpoint = "entry", pc, resident, autoInit,
                    residentBytes = Bytes(resident, 26), autoInitBytes = Bytes(autoInit, 16), d = state.D.ToArray(), a = state.A.ToArray() });
                pendingResidentReturns.Add((machine.Bus.ReadLong(state.A[7]), state.A[7] + 4));
            }
            var offset = pc - boot.NativeDosImageEntryAddress;
            var range = registryRanges.FirstOrDefault(row => offset >= row.Offset && offset < row.Offset + row.Bytes);
            if (range.Name is not null)
            {
                Assert.True(bootstrapTrace.Count < 8192, "Bootstrap diagnostic capture overflow.");
                bootstrapTrace.Add(new { pc, offset, symbol = range.Name, d = state.D.ToArray(), a = state.A.ToArray(),
                    opcode = machine.Bus.ReadWord(pc), sr = state.StatusRegister });
            }
            if (pc == unchecked(machine.Bus.ReadLong(4) + (uint)ExecLvo.FreeMem) && state.D[0] <= 256 && state.D[0] > 0 && machine.Bus.IsMappedMemoryRange(state.A[1], (int)state.D[0]))
                bootstrapFrees.Add(new { address = state.A[1], bytes = state.D[0],
                    hex = Convert.ToHexStringLower(Enumerable.Range(0, (int)state.D[0]).Select(i => machine.Bus.ReadByte(state.A[1] + (uint)i)).ToArray()) });
        };
        var passed = false;
        string? failure = null;
        try
        {
            boot.StartApplicationSession();
            var initialTask = machine.Bus.ReadLong(machine.Bus.ReadLong(4) + ExecLayout.ExecBase.ThisTask);
            Assert.NotEqual(0u, initialTask);
            Assert.Equal((byte)NodeType.Task, machine.Bus.ReadByte(initialTask + ExecLayout.Node.Type));
            observations.Add(new { checkpoint = "production-boot-task", task = initialTask, type = "Task", fabricatedProcess = false });

            Assert.True(boot.TryInstallNativeDos(File.ReadAllBytes(dosPath), 8_000_000, out var installError), installError);
            boot.NativeDosBootstrapInstructionObserver = null;
            var nativeDos = boot.NativeDosLibraryBase;
            Assert.NotEqual(0u, nativeDos);
            Assert.NotEqual(AmigaKickstartHost.DosLibraryBase, nativeDos);
            using var calls = new PublicBootCalls(machine, boot);
            var storage = calls.Allocate(1024);
            WriteText(machine.Bus, storage, "dos.library");
            var opened = calls.Call(machine.Bus.ReadLong(4), ExecLvo.OpenLibrary,
                state => { state.A[1] = storage; state.D[0] = 36; });
            Assert.Equal(nativeDos, opened);
            var nativeVectors = new[] { DosLvo.Open, DosLvo.Input, DosLvo.Output, DosLvo.LoadSeg,
                DosLvo.RunCommand, DosLvo.ReadArgs, DosLvo.Lock, DosLvo.Examine, DosLvo.MakeLink };
            foreach (var lvo in nativeVectors)
            {
                var vector = unchecked(nativeDos + (uint)lvo);
                Assert.False(machine.Bus.HasHostGateway(vector));
                Assert.Equal((ushort)0x4EF9, machine.Bus.ReadWord(vector));
                Assert.False(machine.Bus.HasHostGateway(machine.Bus.ReadLong(vector + 2)));
            }
            observations.Add(new { checkpoint = "public-open-library-native-dos", nativeDos, opened,
                nativeVectors, instructions = calls.Instructions, installError });

            Assert.True(boot.TryPublishNativeDosConfiguredVolumes(out var publicationError), publicationError);
            observations.Add(new { checkpoint = "configured-volumes-published", publicationError });

            if (stage == "command")
            {
                var baseline = calls.Call(machine.Bus.ReadLong(4), ExecLvo.AvailMem, state => state.D[1] = 0);
                for (var session = 1; session <= sessions; session++)
                {
                    RunLauncher(machine, boot, calls, hostRoot, initialTask, nativeDos, observations, session);
                    var free = calls.Call(machine.Bus.ReadLong(4), ExecLvo.AvailMem, state => state.D[1] = 0);
                    observations.Add(new { checkpoint = "session-fully-released", session, baseline, free });
                    Assert.Equal(baseline, free);
                }
            }

            calls.Call(machine.Bus.ReadLong(4), ExecLvo.CloseLibrary, state => state.A[1] = opened);
            calls.Free(storage, 1024);
            observations.Add(new { checkpoint = "public-caller-register-and-stack-preservation",
                calls = calls.Results.ToArray(), harnessRestoresCallerContext = false });
            passed = true;
        }
        catch (Exception error)
        {
            failure = error.ToString();
            throw;
        }
        finally
        {
            var report = new { schemaVersion = 1, passed, stage, failure, observations,
                machine = new { cpu = machine.Options.CpuBackend.ToString(), machine.Cpu.State.ProgramCounter,
                    machine.Cpu.State.Halted, machine.Cpu.State.Cycles },
                diagnostics = boot.Diagnostics.ToArray(),
                bootstrapTrace, bootstrapFrees, initResidentCalls, libraryContinuations, libraryOpenEntries,
                boot.NativeDosBootstrapInstructions,
                command = Bound(commandPath), nativeDos = Bound(dosPath),
                loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && a.GetName().Name!.StartsWith("Copper", StringComparison.Ordinal) && File.Exists(a.Location))
                    .Select(a => new { name = a.GetName().Name, a.ManifestModule.ModuleVersionId, file = Bound(a.Location) }).ToArray(),
                productionExecAndPendingServices = true, testOnlyExecOrPacketGateway = false,
                manualProcessFields = false, shipping = false, pureAdmission = false,
                fullSystemBootQualified = false,
                scope = stage == "install" ? "Native DOS installation and public OpenLibrary readiness only; no CLI/command execution or filesystem gate claim."
                    : "Bounded application-session bootstrap and native DOS command integration; not a disk startup/Shell loop or complete system boot qualification." };
            File.WriteAllText(Path.Combine(output, "result.json"), JsonSerializer.Serialize(report,
                new JsonSerializerOptions { WriteIndented = true }) + "\n");
            Assert.Equal(DosHash, Hash(dosPath));
            Assert.Equal(CommandHash, Hash(commandPath));
        }
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Opt-in integration input {name} is required.");
    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private static object Bound(string path) => new { path = Path.GetFullPath(path), sha256 = Hash(path), bytes = new FileInfo(path).Length };
    private static void WriteText(AmigaBus bus, uint address, string value) => bus.CopyToMemory(address, Encoding.ASCII.GetBytes(value + "\0"));

    private static void RunLauncher(Machine machine, AmigaBootController boot, PublicBootCalls calls,
        string hostRoot, uint initialTask, uint nativeDos, List<object> observations, int session)
    {
        var launcherPath = Path.GetFullPath(Required("COPPER_BOOT_LAUNCHER"));
        var mapPath = Path.GetFullPath(Required("COPPER_BOOT_LAUNCHER_MAP"));
        Assert.Equal("9f52ca81f83d6f9edcee9fe0d33768c58ef022f18bdb0ade75e3b7a6607ecdec", Hash(launcherPath));
        var allocations = new List<(uint Address, uint Bytes)>();
        var loader = new AmigaHunkProgramLoader(machine.Bus, bytes =>
        {
            var address = calls.Allocate((uint)bytes);
            allocations.Add((address, (uint)bytes));
            return address;
        });
        var program = loader.Load(File.ReadAllBytes(launcherPath), collectSymbols: true);
        Assert.True(program.TryGetSymbolAddress("copperos.boot-session.start", out var start));
        Assert.True(program.TryGetSymbolAddress("copperos.boot-session.child", out var child));
        var config = calls.Allocate(4096);
        var bus = machine.Bus;
        void Put(uint offset, uint value) => bus.WriteLong(config + offset, value);
        uint Get(uint offset) => bus.ReadLong(config + offset);
        uint Text(uint offset, string value)
        {
            WriteText(bus, config + offset, value);
            return config + offset;
        }
        var prefix = session == 1 ? "" : $"session-{session}-";
        var arguments = $"DH0:{prefix}alias DH0:{prefix}target HARD\n";
        Put(0, 0x4E445331); Put(4, 160);
        Put(64, Text(256, "DH0:MakeLink"));
        Put(68, Text(512, arguments)); Put(72, (uint)arguments.Length);
        Put(76, Text(768, $"DH0:{prefix}alias")); Put(80, config + 2048); Put(84, 512);
        Put(92, 32768); Put(96, 16384);
        Put(120, Text(1024, $"DH0:{prefix}diagnostics")); Put(124, Text(1280, $"DH0:{prefix}target"));
        var vectors = new Dictionary<uint, string>();
        foreach (var (lvo, name) in new (int, string)[]
        {
            (DosLvo.CreateNewProc, "CreateNewProc"), (DosLvo.Cli, "Cli"), (DosLvo.Input, "Input"),
            (DosLvo.Output, "Output"), (DosLvo.Open, "Open"), (DosLvo.Close, "Close"),
            (DosLvo.Read, "Read"), (DosLvo.LoadSeg, "LoadSeg"), (DosLvo.UnLoadSeg, "UnLoadSeg"),
            (DosLvo.RunCommand, "RunCommand"), (DosLvo.ReadArgs, "ReadArgs"),
            (DosLvo.Lock, "Lock"), (DosLvo.Examine, "Examine"), (DosLvo.MakeLink, "MakeLink")
        }) vectors.Add(unchecked(nativeDos + (uint)lvo), name);
        var counts = new Dictionary<string, int>();
        var packets = new List<object>();
        var packetLocations = new List<(uint Port, uint Message, uint Packet)>();
        var pendingPacketObservations = new List<(uint Message, uint Packet, int Action)>();
        var packetReplies = new List<object>();
        var launcherArgumentTrace = new List<object>();
        var executionChunks = new List<object>();
        var waitResumeEntries = new List<object>();
        var taskPublicationEntries = new List<object>();
        var stackSwapTrace = new List<object>();
        var stackTraceRemaining = 0;
        var packetActions = new List<int>();
        var execBase = bus.ReadLong(4);
        var putMsg = unchecked(execBase + (uint)ExecLvo.PutMsg);
        vectors.Add(unchecked(execBase + (uint)ExecLvo.Wait), "Exec.Wait");
        vectors.Add(unchecked(execBase + (uint)ExecLvo.Signal), "Exec.Signal");
        vectors.Add(unchecked(execBase + (uint)ExecLvo.RemTask), "Exec.RemTask");
        vectors.Add(unchecked(execBase + (uint)ExecLvo.AddTask), "Exec.AddTask");
        var commandEntryCount = 0;
        var childEntryCount = 0;
        // Scheduling occurs before an instruction, while Observe runs after it.
        // Count completion of the bound export's first PEA (A5), not the skipped entry PC.
        Assert.Equal((ushort)0x4855, bus.ReadWord(child));
        var chunks = 0;
        var launcherBootstrapInstructions = 0;
        var retired = false;
        var childRemoved = false;
        var childDosResourcesReleased = false;
        uint? retainedNativeProcessRecord = null;
        uint? publishedChildAfterReturn = null;
        uint childCliNumber = 0;
        uint? childCliAfterReturn = null;
        uint childCliSlot = 0;
        uint? childCliSlotAfterReturn = null;
        uint? freeMemoryBeforeSession = null;
        uint? freeMemoryAfterSession = null;
        object TaskSnapshot(uint task)
        {
            if (task == 0 || !bus.IsMappedMemoryRange(task, 92)) return new { task, mapped = false };
            var sp = bus.ReadLong(task + ExecLayout.Task.StackPointer);
            return new { task, mapped = true, nodeType = bus.ReadByte(task + ExecLayout.Node.Type),
                state = bus.ReadByte(task + ExecLayout.Task.State), flags = bus.ReadByte(task + ExecLayout.Task.Flags),
                signalAllocated = bus.ReadLong(task + ExecLayout.Task.SignalAllocated), signalWait = bus.ReadLong(task + ExecLayout.Task.SignalWait),
                signalReceived = bus.ReadLong(task + ExecLayout.Task.SignalReceived), stackPointer = sp,
                stackLower = bus.ReadLong(task + ExecLayout.Task.StackLower), stackUpper = bus.ReadLong(task + ExecLayout.Task.StackUpper),
                userData = bus.ReadLong(task + ExecLayout.Task.UserData),
                stackBytes = bus.IsMappedMemoryRange(sp, 64) ? Convert.ToHexStringLower(Enumerable.Range(0, 64).Select(i => bus.ReadByte(sp + (uint)i)).ToArray()) : "unmapped" };
        }
        void Observe(long previous, long current)
        {
            var state = machine.Cpu.State;
            var pc = state.ProgramCounter;
            if (pc == unchecked(execBase + (uint)ExecLvo.StackSwap)) stackTraceRemaining = 24;
            if (stackTraceRemaining > 0)
            {
                Assert.True(stackSwapTrace.Count < 128, "StackSwap trace overflow.");
                stackTraceRemaining--;
                stackSwapTrace.Add(new { pc, a = state.A.ToArray(), d = state.D.ToArray(),
                    stack = bus.IsMappedMemoryRange(state.A[7], 16)
                        ? Enumerable.Range(0, 4).Select(i => bus.ReadLong(state.A[7] + (uint)i * 4)).ToArray() : null,
                    descriptor = bus.IsMappedMemoryRange(state.A[0], 12)
                        ? Enumerable.Range(0, 3).Select(i => bus.ReadLong(state.A[0] + (uint)i * 4)).ToArray() : null });
            }
            for (var i = pendingPacketObservations.Count - 1; i >= 0; i--)
            {
                var pending = pendingPacketObservations[i];
                if (bus.ReadByte(pending.Message + ExecLayout.Node.Type) != (byte)NodeType.ReplyMessage) continue;
                packetReplies.Add(new { pending.Message, pending.Packet, pending.Action,
                    result1 = bus.ReadLong(pending.Packet + 12), result2 = bus.ReadLong(pending.Packet + 16),
                    readByte = pending.Action == (int)DosPacketAction.Read && bus.ReadLong(pending.Packet + 12) == 1
                        ? (int)bus.ReadByte(bus.ReadLong(pending.Packet + 24)) : -1,
                    arguments = Enumerable.Range(0, 7).Select(n => bus.ReadLong(pending.Packet + 20 + (uint)n * 4)).ToArray(),
                    cycle = current });
                pendingPacketObservations.RemoveAt(i);
            }
            if (pc == 0x00F08500 && waitResumeEntries.Count < 64)
                waitResumeEntries.Add(new { pc, opcode = bus.ReadWord(pc), gateway = bus.HasHostGateway(pc), d0 = state.D[0], a6 = state.A[6], stack = state.A[7],
                    currentTask = bus.ReadLong(execBase + ExecLayout.ExecBase.ThisTask), parent = TaskSnapshot(initialTask), child = TaskSnapshot(Get(12)) });
            if (pc == unchecked(execBase + (uint)ExecLvo.AddTask))
            {
                Assert.True(taskPublicationEntries.Count < 16, "Task publication trace overflow.");
                taskPublicationEntries.Add(new { pc, initialEntry = state.A[2], finalEntry = state.A[3], d1 = state.D[1],
                    a6 = state.A[6], currentTask = bus.ReadLong(execBase + ExecLayout.ExecBase.ThisTask), task = TaskSnapshot(state.A[1]) });
            }
            if (Get(8) == 0 && pc >= program.SegmentBases[0] && pc < program.SegmentBases[0] + allocations[0].Bytes - 4)
            {
                Assert.True(launcherArgumentTrace.Count < 4096, "Launcher argument trace overflow.");
                launcherArgumentTrace.Add(new { pc, offset = pc - program.SegmentBases[0], opcode = bus.ReadWord(pc),
                    d = state.D.ToArray(), a = state.A.ToArray(), configMagic = Get(0), configBytes = Get(4) });
            }
            if (vectors.TryGetValue(pc, out var name)) counts[name] = counts.GetValueOrDefault(name) + 1;
            if (pc == child + 2) childEntryCount++;
            var segment = Get(108);
            if (segment != 0 && pc == (segment << 2) + 4) commandEntryCount++;
            if (pc != putMsg || state.A[1] == 0 || !bus.IsMappedMemoryRange(state.A[1], 20)) return;
            var packet = bus.ReadLong(state.A[1] + ExecLayout.Node.Name);
            if (packet == 0 || !bus.IsMappedMemoryRange(packet, 48) || bus.ReadLong(packet) != state.A[1]) return;
            Assert.True(packets.Count < 16384, "Packet capture overflow; no truncated trace is accepted.");
            var action = unchecked((int)bus.ReadLong(packet + 8));
            packetActions.Add(action);
            packetLocations.Add((state.A[0], state.A[1], packet));
            pendingPacketObservations.Add((state.A[1], packet, action));
            packets.Add(new { action, port = state.A[0], message = state.A[1], packet,
                task = bus.ReadLong(execBase + ExecLayout.ExecBase.ThisTask), cycle = current });
        }
        try
        {
            // Driver/config allocations are held at both checkpoints. Flags 0
            // requests free bytes, not MEMF_TOTAL's installed-memory total.
            freeMemoryBeforeSession = calls.Call(execBase, ExecLvo.AvailMem,
                state => state.D[1] = 0);
            calls.BeginEntry(start, state => { state.A[0] = config; state.A[6] = execBase; });
            Observe(machine.Cpu.State.Cycles, machine.Cpu.State.Cycles);
            for (; launcherBootstrapInstructions < 10_000 && Get(8) == 0 && !calls.AtReturn && !machine.Cpu.State.Halted; launcherBootstrapInstructions++)
                boot.ContinueCopperStartRuntimeUntilCycle(long.MaxValue, 1, Observe);
            Assert.True(Get(8) != 0, "Launcher did not accept its valid config before returning/stalling; no command execution is claimed.");
            const int maximumChunks = 256; // Byte-wise provider reads need over 8M instructions for this 3232-byte HUNK.
            var remainingInstructions = 64_000_000 - launcherBootstrapInstructions;
            for (; chunks < maximumChunks && !machine.Cpu.State.Halted; chunks++)
            {
                var result = boot.ContinueCopperStartRuntimeUntilCycle(long.MaxValue, Math.Min(250_000, remainingInstructions), Observe);
                remainingInstructions -= result.InstructionsExecuted;
                executionChunks.Add(new { chunk = chunks, result.InstructionsExecuted, machine.Cpu.State.ProgramCounter,
                    machine.Cpu.State.Stopped, currentTask = bus.ReadLong(execBase + ExecLayout.ExecBase.ThisTask) });
                retired = Get(16) == 1 && calls.AtReturn && bus.ReadLong(execBase + ExecLayout.ExecBase.ThisTask) == initialTask;
                if (retired) break;
            }
            Assert.False(machine.Cpu.State.Halted, string.Join("\n", boot.Diagnostics));
            Assert.True(retired, "Child did not finish and return execution to the real initial task within 256 x 250000 instructions.");
            // Query the public registry while the signalled child is still live.
            // An absent private process record alone does not prove CLI removal.
            var maximumCli = calls.Call(nativeDos, DosLvo.MaxCli);
            Assert.InRange(maximumCli, 1u, 4096u);
            for (uint number = 1; number <= maximumCli; number++)
            {
                var process = calls.Call(nativeDos, DosLvo.FindCliProc,
                    state => state.D[1] = number);
                if (process != Get(12)) continue;
                Assert.Equal(0u, childCliNumber);
                childCliNumber = number;
            }
            Assert.NotEqual(0u, childCliNumber);
            // This single-child session uses the mandatory first public range.
            // rn_TaskArray is a BPTR; entry zero contains the array capacity.
            var cliRoot = bus.ReadLong(nativeDos + DosLayout.DosLibrary.Root);
            Assert.True(bus.IsMappedMemoryRange(cliRoot, DosLayout.RootNode.Size));
            var cliArray = bus.ReadLong(cliRoot + DosLayout.RootNode.TaskArray) << 2;
            Assert.True(bus.IsMappedMemoryRange(cliArray, 4));
            Assert.InRange(childCliNumber, 1u, bus.ReadLong(cliArray));
            childCliSlot = cliArray + childCliNumber * 4;
            Assert.True(bus.IsMappedMemoryRange(childCliSlot, 4));
            Assert.Equal(Get(12) + DosLayout.Process.MessagePort, bus.ReadLong(childCliSlot));
            // Completion Signal may preempt the child before its final RTS.
            // Yield through public Exec; do not treat the driver's done word as retirement.
            calls.CaptureRetirementTrace = true;
            var parentPriority = calls.Call(execBase, ExecLvo.SetTaskPri, state =>
            { state.A[1] = initialTask; state.D[0] = unchecked((uint)-1); });
            calls.Call(execBase, ExecLvo.SetTaskPri, state =>
            { state.A[1] = initialTask; state.D[0] = parentPriority; });
            var childName = Text(1536, "CopperOS native command session");
            publishedChildAfterReturn = calls.Call(execBase, ExecLvo.FindTask, state => state.A[1] = childName);
            Assert.Equal(0u, publishedChildAfterReturn);
            childRemoved = true;
            Assert.Equal(1u, Get(152));
            Assert.Equal(Get(136), Get(156));
            childCliAfterReturn = calls.Call(nativeDos, DosLvo.FindCliProc,
                state => state.D[1] = childCliNumber);
            Assert.Equal(0u, childCliAfterReturn);
            Assert.Equal(cliRoot, bus.ReadLong(nativeDos + DosLayout.DosLibrary.Root));
            Assert.Equal(cliArray, bus.ReadLong(cliRoot + DosLayout.RootNode.TaskArray) << 2);
            childCliSlotAfterReturn = bus.ReadLong(childCliSlot);
            Assert.Equal(0u, childCliSlotAfterReturn);
            Assert.InRange(calls.Call(nativeDos, DosLvo.MaxCli), childCliNumber, 4096u);
            // Read-only private-owner audit against DosObjectRecords.cs: the
            // state process list is at +8, owned-header Next at +16, Task at +20.
            var nativeState = bus.ReadLong(nativeDos + global::CopperStart.Dos.DosNativeLibraryCore.PrivateExtensionOffset);
            var record = bus.ReadLong(nativeState + 8);
            retainedNativeProcessRecord = 0;
            var visitedRecords = new HashSet<uint>();
            while (record != 0)
            {
                Assert.True(visitedRecords.Count < 4096 && visitedRecords.Add(record), "Invalid native DOS process chain.");
                Assert.True(bus.IsMappedMemoryRange(record, 92));
                Assert.Equal(nativeState, bus.ReadLong(record + 4));
                if (bus.ReadLong(record + 20) == Get(12)) retainedNativeProcessRecord = record;
                record = bus.ReadLong(record + 16);
            }
            childDosResourcesReleased = retainedNativeProcessRecord == 0;
            Assert.True(childDosResourcesReleased, "Native DOS still owns a process record after Exec task removal.");
            Assert.Equal(0u, Get(20)); Assert.Equal(0u, Get(24));
            Assert.Equal(100u, Get(8)); Assert.Equal(0u, Get(28)); Assert.Equal(0u, Get(32));
            Assert.Equal(nativeDos, Get(36)); Assert.Equal(nativeDos, Get(100));
            Assert.Equal(initialTask, Get(144)); Assert.NotEqual(0u, Get(148));
            Assert.NotEqual(0u, Get(12)); Assert.Equal(Get(12), Get(40));
            Assert.Equal((uint)NodeType.Process, Get(132)); Assert.NotEqual(0u, Get(44));
            Assert.NotEqual(0u, Get(48)); Assert.NotEqual(0u, Get(52));
            Assert.Equal(Get(48), Get(56)); Assert.Equal(Get(52), Get(60));
            Assert.NotEqual(0u, Get(112)); Assert.NotEqual(0u, Get(140));
            Assert.Equal(uint.MaxValue, Get(128));
            Assert.Equal(1, commandEntryCount);
            Assert.Equal(1, childEntryCount);
            foreach (var name in new[] { "CreateNewProc", "Cli", "Input", "Output", "LoadSeg", "RunCommand", "ReadArgs", "Lock", "Examine", "MakeLink", "UnLoadSeg", "Exec.AddTask", "Exec.Wait", "Exec.Signal" })
                Assert.True(counts.GetValueOrDefault(name) > 0, $"Native public {name} was not observed.");
            foreach (var action in new[] { DosPacketAction.LocateObject, DosPacketAction.ExamineObject64, DosPacketAction.MakeLink })
                Assert.Contains((int)action, packetActions);
            const string expected = "production-boot-payload\n";
            Assert.Equal((uint)Encoding.ASCII.GetByteCount(expected), Get(88));
            var readback = Enumerable.Range(0, checked((int)Get(88))).Select(i => bus.ReadByte(config + 2048 + (uint)i)).ToArray();
            Assert.Equal(expected, Encoding.ASCII.GetString(readback));
            Assert.False(File.Exists(Path.Combine(hostRoot, prefix + "target")));
            Assert.Equal(expected, File.ReadAllText(Path.Combine(hostRoot, prefix + "alias")));
            Assert.Equal("caller-input\n", File.ReadAllText(Path.Combine(hostRoot, "caller")));
            Assert.Empty(File.ReadAllBytes(Path.Combine(hostRoot, prefix + "diagnostics")));
            Assert.Equal(CommandHash, Hash(Path.Combine(hostRoot, "MakeLink")));
            freeMemoryAfterSession = calls.Call(execBase, ExecLvo.AvailMem,
                state => state.D[1] = 0);
            Assert.Equal(freeMemoryBeforeSession, freeMemoryAfterSession);
        }
        finally
        {
            observations.Add(new { checkpoint = "compiled-public-cli-launcher", launcher = Bound(launcherPath), map = Bound(mapPath),
                session,
                exitCallbackCount = Get(152), exitCallbackCode = Get(156),
                start, child, config, resultWords = Enumerable.Range(0, 40).Select(i => Get((uint)i * 4)).ToArray(),
                chunks, bound = "256 x 250000 instructions", currentTaskReturnedToInitial = retired, publishedChildAfterReturn, childRemoved,
                retainedNativeProcessRecord, childDosResourcesReleased,
                childCliNumber, childCliAfterReturn,
                childCliSlot, childCliSlotAfterReturn,
                freeMemoryBeforeSession, freeMemoryAfterSession,
                retirementOwner = InspectRetirementOwner(boot, machine),
                childEntryCount, commandEntryCount, publicCalls = counts, packets, packetReplies,
                finalPacketMemoryScope = "Distinct observed addresses at stop; completed packets may have been freed and memory reused. These are not completion results.",
                finalPacketStates = packetLocations.Distinct().Select(p => new { p.Port, p.Message, p.Packet,
                    portHead = bus.ReadLong(p.Port + ExecLayout.MsgPort.MessageList),
                    messageReplyPort = bus.ReadLong(p.Message + ExecLayout.Message.ReplyPort),
                    messageType = bus.ReadByte(p.Message + ExecLayout.Node.Type),
                    packetWords = Enumerable.Range(0, 12).Select(i => bus.ReadLong(p.Packet + (uint)i * 4)).ToArray() }).ToArray(),
                launcherArgumentTrace, launcherBootstrapInstructions,
                executionChunks, waitResumeEntries, taskPublicationEntries, stackSwapTrace,
                retirementTraceTail = calls.RetirementTrace.ToArray(),
                finalParentTask = TaskSnapshot(initialTask), finalChildTask = TaskSnapshot(Get(12)),
                scope = "Compiled guest CreateNewProc/CLI, native stream/LoadSeg/RunCommand, production Exec message delivery and boot pending services. No fabricated Process or fixture packet gateway." });
            // No storage is reclaimed while guest execution is live/faulted.
            if (retired && childRemoved && childDosResourcesReleased)
            {
                calls.Free(config, 4096);
                foreach (var allocation in allocations) calls.Free(allocation.Address, allocation.Bytes);
            }
        }
    }

    // Read-only diagnostic of the actual deferred owner; never invokes cleanup,
    // changes a context, or supplies authorization to the retirement path.
    private static object InspectRetirementOwner(AmigaBootController boot, Machine machine)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static object Field(object owner, string name) => owner.GetType().GetField(name, flags)!.GetValue(owner)!;
        static object? Query(object owner, string name, params object?[] args) =>
            owner.GetType().GetMethod(name, flags)!.Invoke(owner, args);
        var service = Field(boot, "_execTaskServices");
        var context = Field(service, "_context");
        var memoryOwner = Field(service, "_memoryContext");
        var current = machine.Bus.ReadLong(machine.Bus.ReadLong(4) + ExecLayout.ExecBase.ThisTask);
        var pending = new List<object>();
        foreach (var removal in (IEnumerable)Field(service, "_deferredReap"))
        {
            var storage = Field(removal, "Storage");
            var cpu = (M68kCpuState)Field(removal, "Cpu");
            var blockingWords = new List<object>();
            var scanUpper = machine.Bus.ReadLong(current + ExecLayout.Task.StackUpper);
            foreach (var bank in new[] { ("a7", cpu.A[7]), ("usp", cpu.UserStackPointer),
                ("isp", cpu.InterruptStackPointer), ("msp", cpu.MasterStackPointer) })
            {
                var pointer = bank.Item2;
                if (pointer == 0 || (pointer & 1) != 0 || pointer > scanUpper || scanUpper - pointer > 1024 * 1024) continue;
                for (var address = pointer; scanUpper - address >= 4; address += 2)
                {
                    if (!machine.Bus.IsMappedMemoryRange(address, 4)) break;
                    var value = machine.Bus.ReadLong(address);
                    if (Query(storage, "Touches", value, false) is not true) continue;
                    blockingWords.Add(new { bank = bank.Item1, pointer, address, value });
                    break;
                }
            }
            pending.Add(new { task = Field(removal, "Task"),
                blockingWords,
                canComplete = Query(service, "CanCompleteRetirement", removal),
                commonChecks = Query(service, "CommonRetirementChecks", removal),
                storageValid = Field(storage, "_valid"),
                storageMatches = Query(storage, "Matches", context, memoryOwner),
                framePointersClear = Query(storage, "FrameIsClear", context, current, cpu, false),
                frameContinuationsClear = Query(storage, "FrameIsClear", context, current, cpu, true),
                ownedSpans = ((IEnumerable)Field(storage, "_ownedSpans")).Cast<object>().Select(x => x.ToString()).ToArray(),
                pc = cpu.ProgramCounter, previousPc = cpu.LastInstructionProgramCounter,
                sp = cpu.A[7], usp = cpu.UserStackPointer, isp = cpu.InterruptStackPointer, msp = cpu.MasterStackPointer,
                current, lower = machine.Bus.ReadLong(current + ExecLayout.Task.StackLower),
                upper = machine.Bus.ReadLong(current + ExecLayout.Task.StackUpper) });
        }
        return pending;
    }

    private sealed class PublicBootCalls : IDisposable
    {
        private readonly Machine _machine;
        private readonly AmigaBootController _boot;
        private readonly uint _sentinel = AmigaBootController.BootBlockAddress;
        internal long Instructions { get; private set; }
        internal List<object> Results { get; } = new();
        internal bool CaptureRetirementTrace;
        internal Queue<object> RetirementTrace { get; } = new();
        internal bool AtReturn => _machine.Cpu.State.ProgramCounter == _sentinel;
        internal PublicBootCalls(Machine machine, AmigaBootController boot)
        {
            _machine = machine; _boot = boot;
            // The application boot session has no disk payload here. This is
            // an ordinary guest return loop, never a host execution gateway.
            machine.Bus.WriteWord(_sentinel, 0x60FE);
        }
        internal uint Allocate(uint bytes)
        {
            var address = Call(_machine.Bus.ReadLong(4), ExecLvo.AllocMem,
                s => { s.D[0] = bytes; s.D[1] = 0x10001; });
            Assert.NotEqual(0u, address);
            return address;
        }
        internal void Free(uint address, uint bytes) => Call(_machine.Bus.ReadLong(4), ExecLvo.FreeMem,
            s => { s.A[1] = address; s.D[0] = bytes; });
        internal uint Call(uint library, int lvo, Action<M68kCpuState>? prepare = null)
        {
            var state = _machine.Cpu.State;
            var callerStack = state.A[7];
            state.A[6] = library;
            BeginEntry(unchecked(library + (uint)lvo), prepare);
            for (var n = 0; n < 100_000 && state.ProgramCounter != _sentinel && !state.Halted; n++)
            {
                _boot.ContinueCopperStartRuntimeUntilCycle(long.MaxValue, 1);
                Instructions++;
                if (CaptureRetirementTrace)
                {
                    if (RetirementTrace.Count == 64) RetirementTrace.Dequeue();
                    RetirementTrace.Enqueue(new { pc = state.ProgramCounter, previousPc = state.LastInstructionProgramCounter,
                        currentTask = _machine.Bus.ReadLong(_machine.Bus.ReadLong(4) + ExecLayout.ExecBase.ThisTask),
                        a = state.A.ToArray(), d = state.D.ToArray(),
                        stack = _machine.Bus.IsMappedMemoryRange(state.A[7], 16)
                            ? Enumerable.Range(0, 4).Select(i => _machine.Bus.ReadLong(state.A[7] + (uint)i * 4)).ToArray() : null });
                }
            }
            Assert.False(state.Halted, string.Join("\n", _boot.Diagnostics));
            Assert.Equal(_sentinel, state.ProgramCounter);
            Assert.Equal(library, state.A[6]);
            Assert.Equal(callerStack, state.A[7]);
            Results.Add(new { library, lvo, callerStack, returnedStack = state.A[7], returnedA6 = state.A[6], d0 = state.D[0] });
            return state.D[0];
        }
        internal void BeginEntry(uint entry, Action<M68kCpuState>? prepare)
        {
            var state = _machine.Cpu.State;
            Assert.False(state.Halted);
            prepare?.Invoke(state);
            state.A[7] -= 4;
            _machine.Bus.WriteLong(state.A[7], _sentinel);
            state.ProgramCounter = entry;
        }
        public void Dispose() { }
    }
}
