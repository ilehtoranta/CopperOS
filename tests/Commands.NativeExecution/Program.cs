using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length is not (3 or 4))
        {
            Console.Error.WriteLine("usage: NativeExecution <probe.hunk> <68000|68020|68040> <report.json> [command-startup-vector-fixture|command-argument-boundary-vector-fixture|command-io-vector-fixture|eval-native-entry-vector-fixture|eval-wb31-native-entry-vector-fixture|pathpart-native-entry-vector-fixture|which-wb31-native-entry-vector-fixture|quote-forward-probe-fixture|quote-native-entry-vector-fixture]");
            return 2;
        }
        var suite = args.Length == 4 ? args[3] : ProbeFixture.StartupSuite;
        string? imageHash = null;
        try
        {
            var model = args[1] switch
            {
                "68000" => M68kCpuModel.M68000,
                "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040,
                _ => throw new ArgumentException("Unsupported CPU qualification target.")
            };
            var image = HunkImage.Load(args[0], ProbeFixture.LoadAddress);
            imageHash = image.Sha256;
            var fixture = new ProbeFixture(image, model, suite);
            var cases = fixture.Run();
            var report = new
            {
                schemaVersion = 1,
                status = "passed",
                suite,
                cpu = args[1],
                imageSha256 = fixture.Image.Sha256,
                imageBytes = new FileInfo(args[0]).Length,
                loadedBytes = fixture.Image.Code.Length,
                imageLoads = 1,
                instructionExecutor = "Copper68k 1.4.0",
                instructionCorePath = typeof(M68kCoreFactory).Assembly.Location,
                instructionCoreSha256 = AssemblyHash(typeof(M68kCoreFactory).Assembly.Location),
                managedExecutorPath = typeof(Program).Assembly.Location,
                managedExecutorSha256 = AssemblyHash(typeof(Program).Assembly.Location),
                hostRuntimeVersion = Environment.Version.ToString(),
                realKickstartExecution = false,
                realCopperStartExecution = false,
                realDosParser = false,
                realDosIo = false,
                referenceCommandBehavior = false,
                shippingOrPureApproval = false,
                minimumStackQualified = false,
                sharedImageWrites = 0,
                nativeWrites = fixture.Bus.NativeWrites,
                nativeReads = fixture.Bus.NativeReads,
                passed = cases.Count,
                cases
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
            File.WriteAllText(args[2], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            Console.WriteLine($"PASS {model} {suite}: {cases.Count} native invocations; one shared image; no leaked resources or image writes.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL native command qualification: {error.Message}");
            var failure = new
            {
                schemaVersion = 1, status = "failed", suite,
                cpu = args[1], imageSha256 = imageHash, failure = error.Message,
                shippingOrPureApproval = false
            };
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
                File.WriteAllText(args[2], JsonSerializer.Serialize(failure, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            }
            catch (Exception reportError) { Console.Error.WriteLine($"Could not write failure report: {reportError.Message}"); }
            return 1;
        }
    }

    private static string AssemblyHash(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
}

internal sealed record ProbeCase(string Name, string Arguments, int Result, int Error, string Output)
{
    public int? Number { get; init; }
    public bool FailSwitch { get; init; }
    public bool Workbench { get; init; }
    public bool MissingDos { get; init; }
    public bool AllocationFailure { get; init; }
    public int ParserError { get; init; }
    public int? ParserSuccessError { get; init; }
    public int? WriteResult { get; init; }
    public int? EntryLength { get; init; }
    public bool NullArgumentPointer { get; init; }
    public uint StackBytes { get; init; } = 0x4000;
    public ArgumentBoundaryCase? ArgumentBoundary { get; init; }
    public NativeIoCase? NativeIo { get; init; }
    public EvalEntryCase? Eval { get; init; }
    public PathPartEntryCase? PathPart { get; init; }
    public WhichEntryCase? Which { get; init; }
    public QuoteForwardProbeCase? QuoteForward { get; init; }
    public QuoteNativeEntryCase? QuoteEntry { get; init; }
}

internal sealed class Invocation(ProbeCase definition, int slot)
{
    public const int InitialIoError = 31337;
    public ProbeCase Definition { get; } = definition;
    public uint StackBytes => Definition.StackBytes;
    public uint Process { get; } = (uint)(0x10000 + slot * 0x1000);
    public uint Arguments { get; } = (uint)(0x20000 + slot * 0x1000);
    public uint StackTop { get; } = (uint)(0x40000 + slot * 0x10000);
    public uint DosBase { get; } = (uint)(0x8000 + slot * 0x1000);
    public uint OutputBptr { get; } = (uint)(0x100 + slot);
    public uint Port => Process + (uint)DosLayout.Process.MessagePort;
    public uint Message => Process + 0x300;
    public uint LowestStackWrite { get; set; } = (uint)(0x40000 + slot * 0x10000);
    public int IoError { get; set; } = InitialIoError;
    public int Opens { get; set; }
    public int Closes { get; set; }
    public int FileOpens { get; set; }
    public int FileCloses { get; set; }
    public int Allocations { get; set; }
    public int Reads { get; set; }
    public int FreeArgs { get; set; }
    public int FreeMem { get; set; }
    public int Replies { get; set; }
    public int WaitPorts { get; set; }
    public int GetMessages { get; set; }
    public bool Forbidden { get; set; }
    public int Instructions { get; set; }
    public NativeIoInvocation? NativeIo { get; set; }
    public QuoteForwardNativeLayout? QuoteForwardLayout { get; set; }
    public List<string> Events { get; } = [];
    public List<uint> AllocationRequests { get; } = [];
    public MemoryStream Output { get; } = new();
}

internal sealed partial class ProbeFixture
{
    public const uint LoadAddress = 0x100000;
    private const uint ExecBase = 0x4000;
    private const uint ReturnAddress = 0x2000;
    private readonly M68kCpuModel model;
    private readonly string suite;
    public HunkImage Image { get; }
    public CommandTestBus Bus { get; } = new();

    public ProbeFixture(HunkImage image, M68kCpuModel model, string suite)
    {
        Require(suite is StartupSuite or ArgumentBoundarySuite or NativeIoSuite or
            EvalEntrySuite or WorkbenchEvalEntrySuite or PathPartEntrySuite or
            WorkbenchWhichEntrySuite or QuoteForwardProbeSuite or
            QuoteNativeEntrySuite,
            "Unknown native qualification suite.");
        Image = image;
        this.model = model;
        this.suite = suite;
        Bus.Long(4, ExecBase);
        Bus.LoadAndProtect(LoadAddress, image.Code);
        RegisterExec();
        if (suite == NativeIoSuite) RegisterIoExec();
        RegisterDos(0x8000);
        RegisterDos(0x9000);
    }

    public List<object> Run()
    {
        if (suite == ArgumentBoundarySuite) return RunArgumentBoundaryCases();
        if (suite == NativeIoSuite) return RunNativeIoCases();
        if (suite == EvalEntrySuite) return RunEvalEntryCases();
        if (suite == WorkbenchEvalEntrySuite) return RunWorkbenchEvalEntryCases();
        if (suite == PathPartEntrySuite) return RunPathPartEntryCases();
        if (suite == WorkbenchWhichEntrySuite) return RunWorkbenchWhichEntryCases();
        if (suite == QuoteForwardProbeSuite) return RunQuoteForwardProbeCases();
        if (suite == QuoteNativeEntrySuite) return RunQuoteNativeEntryCases();
        // These are supplied DOS results, not a replacement ReadArgs parser.
        // The suite tests native calling conventions, storage, and ownership.
        ProbeCase[] cases =
        [
            new("number", "VALUE 42\n", 42, 0, "VALUE 42\n") { Number = 42 },
            new("numeric-zero-pointer", "VALUE 0\n", 0, 0, "VALUE 0\n") { Number = 0 },
            new("minimum-numeric-value", "VALUE -2147483648\n", int.MinValue, 0, "VALUE -2147483648\n") { Number = int.MinValue },
            new("negative-and-byte-preservation", "VALUE=-7 ä *\"\n", -7, 0, "VALUE=-7 ä *\"\n") { Number = -7 },
            new("missing-number", "\n", 1, 0, "\n"),
            new("empty-arguments", "", 0, 0, ""),
            new("switch-failure", "FAIL\n", 10, 205, "") { FailSwitch = true },
            new("allocation-failure", "VALUE 99\n", 20, 103, "") { Number = 99, AllocationFailure = true },
            new("bad-number", "VALUE abc\n", 10, 115, "") { ParserError = 115 },
            new("too-many-arguments", "VALUE 1 excess\n", 10, 118, "") { ParserError = 118 },
            new("short-write", "VALUE 42\n", 10, 221, "VAL") { Number = 42, WriteResult = 3 },
            new("write-failure", "VALUE 42\n", 10, 221, "") { Number = 42, WriteResult = -1 },
            new("missing-dos", "VALUE 42\n", 20, Invocation.InitialIoError, "") { MissingDos = true },
            new("workbench", "", 0, 0, "") { Workbench = true },
            new("workbench-missing-dos", "", 20, Invocation.InitialIoError, "") { Workbench = true, MissingDos = true },
            new("negative-entry-length", "", 10, 120, "") { EntryLength = -1 },
            new("null-entry-buffer", "", 10, 120, "") { EntryLength = 5, NullArgumentPointer = true }
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        for (var index = 0; index < 4; index++)
        {
            reports.AddRange(Execute([new ProbeCase($"repeat-{index}-failure", "VALUE bad\n", 10, 115, "") { ParserError = 115 }], false));
            reports.AddRange(Execute([new ProbeCase($"repeat-{index}-success", "VALUE 73\n", 73, 0, "VALUE 73\n") { Number = 73 }], false));
        }
        reports.AddRange(Execute([
            new ProbeCase("interleaved-left", "VALUE 17\n", 17, 0, "VALUE 17\n") { Number = 17, StackBytes = 4096 },
            new ProbeCase("interleaved-right", "VALUE 83\n", 83, 0, "VALUE 83\n") { Number = 83 }
        ], true));
        reports.AddRange(Execute([
            new ProbeCase("interleaved-error", "FAIL\n", 10, 205, "") { FailSwitch = true },
            new ProbeCase("interleaved-success", "VALUE 52\n", 52, 0, "VALUE 52\n") { Number = 52 }
        ], true));
        reports.AddRange(Execute([
            new ProbeCase("interleaved-parser-error", "VALUE bad\n", 10, 115, "") { ParserError = 115 },
            new ProbeCase("interleaved-empty-success", "", 0, 0, "")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private List<object> Execute(ProbeCase[] definitions, bool interleaved,
        NativeIoTaskState[]? ioTasks = null)
    {
        Require(ioTasks is null || ioTasks.Length == definitions.Length &&
            definitions.All(test => test.NativeIo is not null), "Persistent I/O task inventory does not match its invocations.");
        var invocations = definitions.Select((test, slot) => new Invocation(test, slot)
        {
            NativeIo = test.NativeIo is { } io
                ? new NativeIoInvocation(io, ioTasks?[slot] ?? new NativeIoTaskState(io.InitialError, io.Signals))
                : null
        }).ToArray();
        var cores = new List<IM68kCore>();
        try
        {
            foreach (var invocation in invocations)
            {
                Bus.Current = invocation;
                var test = invocation.Definition;
                Require(invocation.StackBytes >= 256 && invocation.StackBytes <= 0x8000 &&
                    (invocation.StackBytes & 3) == 0, "Stack size is outside the fixture's reserved address range.");
                var bytes = Encoding.Latin1.GetBytes(test.Arguments);
                if (invocation.NativeIo is not null)
                    PrepareNativeIoInvocation(invocation);
                else
                {
                    Bus.Memory.AsSpan((int)invocation.Process, 0x400).Clear();
                    Bus.Long(invocation.Process + (uint)DosLayout.Process.CommandLineInterface, test.Workbench ? 0u : 0x100u);
                    Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, Invocation.InitialIoError);
                    bytes.CopyTo(Bus.Memory.AsSpan((int)invocation.Arguments));
                    Bus.Memory[invocation.Arguments + (uint)bytes.Length] = 0;
                    if (invocation.Definition.QuoteForward is not null)
                        PrepareQuoteForwardProbe(invocation);
                }
                Bus.Memory.AsSpan((int)(invocation.StackTop - invocation.StackBytes - 16),
                    (int)invocation.StackBytes + 32).Fill(0xb6);
                var cpu = M68kCoreFactory.Default.Create(model, Bus);
                cores.Add(cpu);
                cpu.State.StatusRegister = 0; // Real commands run in user mode.
                cpu.BeginSubroutine(LoadAddress, invocation.StackTop, ReturnAddress);
                for (var index = 0; index < 7; index++) cpu.State.A[index] = (uint)(0xae000000 + index * 16);
                for (var index = 0; index < 8; index++) cpu.State.D[index] = (uint)(0xde000000 + index * 16);
                cpu.State.D[0] = unchecked((uint)(test.EntryLength ?? bytes.Length));
                cpu.State.A[0] = test.NullArgumentPointer ? 0 :
                    invocation.QuoteForwardLayout?.Control ?? invocation.Arguments;
            }
            while (cores.Any(cpu => cpu.State.ProgramCounter != ReturnAddress))
            {
                for (var index = 0; index < cores.Count; index++)
                {
                    var cpu = cores[index];
                    if (cpu.State.ProgramCounter == ReturnAddress) continue;
                    var invocation = invocations[index];
                    Bus.Current = invocation;
                    Require(!cpu.State.Halted && !cpu.State.Stopped, $"{invocation.Definition.Name}: CPU halted/stopped.");
                    Require(++invocation.Instructions <= 200_000, $"{invocation.Definition.Name}: native instruction limit exceeded.");
                    try { cpu.ExecuteInstruction(); }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException($"{invocation.Definition.Name}, PC=${cpu.State.ProgramCounter:X8}: {error.Message}", error);
                    }
                }
            }
            var reports = new List<object>();
            for (var index = 0; index < cores.Count; index++)
            {
                var cpu = cores[index];
                var invocation = invocations[index];
                var test = invocation.Definition;
                Require(unchecked((int)cpu.State.D[0]) == test.Result, $"{test.Name}: result {unchecked((int)cpu.State.D[0])}, expected {test.Result}.");
                Require(invocation.IoError == test.Error, $"{test.Name}: IoErr {invocation.IoError}, expected {test.Error}.");
                Require(Encoding.Latin1.GetString(invocation.Output.ToArray()) == test.Output, $"{test.Name}: wrong output bytes.");
                Require(cpu.State.A[7] == invocation.StackTop, $"{test.Name}: command did not restore SP.");
                Require(Bus.Memory.AsSpan((int)invocation.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0, "Stack upper guard changed.");
                Require(Bus.Memory.AsSpan((int)(invocation.StackTop - invocation.StackBytes - 16), 16)
                    .IndexOfAnyExcept((byte)0xb6) < 0, "Stack lower guard changed.");
                if (suite != QuoteForwardProbeSuite)
                {
                    Require(invocation.Opens == 1 && invocation.Closes == (test.MissingDos ? 0 : 1), "Unbalanced DOS library lifetime.");
                    Require(invocation.Replies == (test.Workbench ? 1 : 0), "Unbalanced WBStartup message lifetime.");
                    Require(invocation.WaitPorts == (test.Workbench ? 1 : 0) &&
                        invocation.GetMessages == (test.Workbench ? 1 : 0), "Missing or repeated WBStartup queue operations.");
                }
                if (suite == NativeIoSuite)
                    VerifyNativeIo(invocation);
                else if (suite == ArgumentBoundarySuite)
                    VerifyArgumentBoundary(invocation);
                else if (suite == EvalEntrySuite || suite == WorkbenchEvalEntrySuite)
                    VerifyEvalEntry(invocation);
                else if (suite == PathPartEntrySuite)
                    VerifyPathPartEntry(invocation);
                else if (suite == WorkbenchWhichEntrySuite)
                    VerifyWorkbenchWhichEntry(invocation);
                else if (suite == QuoteForwardProbeSuite)
                    VerifyQuoteForwardProbe(invocation);
                else if (suite == QuoteNativeEntrySuite)
                    VerifyQuoteNativeEntry(invocation);
                else
                {
                    var reachedParser = !test.Workbench && !test.MissingDos && !test.AllocationFailure &&
                        (test.EntryLength is null || test.EntryLength == 0 ||
                            (test.EntryLength > 0 && !test.NullArgumentPointer));
                    Require(invocation.Reads == (reachedParser ? 1 : 0), "ReadArgs invocation count differs from the expected path.");
                    Require(invocation.FreeArgs == (reachedParser && test.ParserError == 0 ? 1 : 0),
                        "RDArgs must be freed once on success, and never after parser failure.");
                }
                if (test.Workbench)
                {
                    var waitIndex = invocation.Events.IndexOf("WaitPort");
                    var getIndex = invocation.Events.IndexOf("GetMsg");
                    var openIndex = invocation.Events.IndexOf("OpenLibrary");
                    Require(waitIndex >= 0 && getIndex > waitIndex && openIndex > getIndex,
                        "Wrong Workbench startup ordering.");
                    Require(invocation.Events.TakeLast(2).SequenceEqual(new[] { "Forbid", "ReplyMsg" }), "Workbench reply was not last under Forbid.");
                }
                Bus.AssertReleased(invocation);
                Bus.AssertImageUnchanged();
                reports.Add(new
                {
                    name = test.Name, instructionInterleaved = interleaved,
                    result = unchecked((int)cpu.State.D[0]), ioErr = invocation.IoError,
                    stdoutHex = Convert.ToHexStringLower(invocation.Output.ToArray()),
                    instructions = invocation.Instructions,
                    configuredStackBytes = invocation.StackBytes,
                    stackBytesWritten = invocation.StackTop - invocation.LowestStackWrite,
                    resultArrayAllocationRequests = invocation.AllocationRequests,
                    readArgsCalls = invocation.Reads,
                    freeArgsCalls = invocation.FreeArgs,
                    freeMemCalls = invocation.FreeMem,
                    nativeIo = invocation.NativeIo?.Report,
                    events = invocation.Events
                });
            }
            return reports;
        }
        finally
        {
            foreach (var cpu in cores) cpu.Dispose();
            foreach (var invocation in invocations) invocation.Output.Dispose();
            Bus.Current = null;
        }
    }

    private void Register(uint baseAddress, short offset, string name, Func<M68kCpuState, Invocation, uint> handler)
    {
        Bus.RegisterGateway(checked((uint)(baseAddress + offset)), state =>
        {
            var invocation = Bus.Current ?? throw new InvalidOperationException("Library vector without a process.");
            Require(state.A[6] == baseAddress, $"{name}: incorrect A6 library base.");
            if (baseAddress != ExecBase)
            {
                Require(baseAddress == invocation.DosBase, "DOS base leaked between resident invocations.");
                Require(invocation.Opens == 1 && invocation.Closes == 0 && !invocation.Definition.MissingDos,
                    $"{name}: DOS vector used without a live successfully opened library lease.");
            }
            if (invocation.NativeIo is not null)
                RequireNativeIoGateway(invocation, name);
            invocation.Events.Add(name);
            var value = handler(state, invocation);
            // Genuine ABI volatile registers; callers must keep live state elsewhere.
            state.D[0] = value;
            state.D[1] = 0xd1d1d1d1;
            state.A[0] = 0xa0a0a0a0;
            state.A[1] = 0xa1a1a1a1;
        });
    }

    private void RegisterExec()
    {
        Register(ExecBase, ExecLvo.FindTask, "FindTask", (state, invocation) =>
        {
            Require(state.A[1] == 0, "FindTask must request the current process.");
            return invocation.Process;
        });
        Register(ExecBase, ExecLvo.OpenLibrary, "OpenLibrary", (state, invocation) =>
        {
            Require(Bus.CString(state.A[1]) == "dos.library" && state.D[0] == 36, "Unexpected library or minimum version.");
            invocation.Opens++;
            return invocation.Definition.MissingDos ? 0 : invocation.DosBase;
        });
        Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", (state, invocation) =>
        {
            Require(state.A[1] == invocation.DosBase && !invocation.Definition.MissingDos &&
                invocation.Opens == 1 && invocation.Closes == 0, "Closed unowned or already closed library.");
            Bus.AssertReleased(invocation);
            invocation.Closes++;
            return 0xc10ced;
        });
        Register(ExecBase, ExecLvo.AllocMem, "AllocMem", (state, invocation) =>
        {
            var expectedBytes = invocation.Definition.ArgumentBoundary is { } boundary
                ? boundary.AllocationBytes : invocation.Definition.Eval is { } eval
                    ? invocation.Allocations == 0
                        ? eval.WorkbenchProfile ? 20u : 24u
                        : 4096u
                    : invocation.Definition.PathPart is not null
                        ? invocation.Allocations == 0 ? 12u : 1024u
                        : invocation.Definition.QuoteEntry is not null
                            ? invocation.Allocations switch
                            {
                                0 => 32u,
                                1 => 4096u,
                                2 => 64u,
                                _ => 4096u
                            }
                        : invocation.Definition.Which is not null
                            ? invocation.Allocations == 0 ? 16u : 1024u : 8u;
            Require(expectedBytes is not null && state.D[0] == expectedBytes &&
                state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                "Unexpected allocation; compiler heap context is not qualified by this suite.");
            invocation.Allocations++;
            invocation.AllocationRequests.Add(state.D[0]);
            // The maximum-safe count case must never allocate huge host memory.
            return invocation.Definition.AllocationFailure ? 0 : Bus.Allocate(invocation, state.D[0], "Exec", true);
        });
        Register(ExecBase, ExecLvo.FreeMem, "FreeMem", (state, invocation) =>
        {
            Bus.Release(invocation, state.A[1], "Exec", state.D[0]);
            invocation.FreeMem++;
            invocation.IoError = 902; // Deliberately poison cleanup to check error preservation.
            return 0xf4ee;
        });
        Register(ExecBase, ExecLvo.WaitPort, "WaitPort", (state, invocation) =>
        {
            Require(invocation.Definition.Workbench && state.A[0] == invocation.Port, "Wrong Workbench port.");
            Require(invocation.WaitPorts == 0 && invocation.GetMessages == 0 && invocation.Replies == 0,
                "Workbench startup port was waited after its message was consumed or already waited.");
            invocation.WaitPorts++;
            return invocation.Message;
        });
        Register(ExecBase, ExecLvo.GetMsg, "GetMsg", (state, invocation) =>
        {
            Require(invocation.Definition.Workbench && state.A[0] == invocation.Port, "Wrong Workbench message port.");
            Require(invocation.WaitPorts == 1 && invocation.GetMessages == 0 && invocation.Replies == 0,
                "Workbench message consumed without WaitPort or consumed repeatedly.");
            invocation.GetMessages++;
            return invocation.Message;
        });
        Register(ExecBase, ExecLvo.Forbid, "Forbid", (_, invocation) =>
        {
            Require(!invocation.Forbidden, "Repeated Forbid.");
            invocation.Forbidden = true;
            return 0;
        });
        Register(ExecBase, ExecLvo.Permit, "Permit", (_, invocation) =>
        {
            if (suite != WorkbenchWhichEntrySuite)
                throw new InvalidOperationException("Command called Permit after startup reply.");
            Require(invocation.Forbidden, "Which called Permit without a preceding Forbid.");
            invocation.Forbidden = false;
            return 0;
        });
        Register(ExecBase, ExecLvo.ReplyMsg, "ReplyMsg", (state, invocation) =>
        {
            Require(invocation.Definition.Workbench && invocation.Forbidden && state.A[1] == invocation.Message &&
                invocation.WaitPorts == 1 && invocation.GetMessages == 1 && invocation.Replies == 0,
                "Wrong, repeated, or unsafe Workbench reply without owned startup message.");
            invocation.Replies++;
            return 0;
        });
    }

    private void RegisterDos(uint baseAddress)
    {
        if (suite == NativeIoSuite)
        {
            RegisterIoDos(baseAddress);
            return;
        }
        if (suite == EvalEntrySuite || suite == WorkbenchEvalEntrySuite)
        {
            RegisterEvalEntryDos(baseAddress);
            return;
        }
        if (suite == PathPartEntrySuite)
        {
            RegisterPathPartEntryDos(baseAddress);
            return;
        }
        if (suite == WorkbenchWhichEntrySuite)
        {
            RegisterWorkbenchWhichEntryDos(baseAddress);
            return;
        }
        if (suite == QuoteNativeEntrySuite)
        {
            RegisterQuoteNativeEntryDos(baseAddress);
            return;
        }
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var boundary = invocation.Definition.ArgumentBoundary;
            var expectedTemplate = boundary is null ? "VALUE/N,FAIL/S" : boundary.Template;
            var expectedBytes = boundary is null ? 8u : boundary.AllocationBytes;
            var resultCount = boundary is null ? 2u : boundary.ResultCount;
            Require(expectedTemplate is not null && Bus.CString(state.D[1]) == expectedTemplate &&
                state.D[3] == 0, "ReadArgs template/source ABI mismatch.");
            var results = state.D[2];
            Require(expectedBytes is 4 or 8 && (results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size == expectedBytes,
                "ReadArgs result slots must be owned and LONG aligned.");
            for (var offset = 0u; offset < expectedBytes; offset += 4)
                Require(Bus.Long(results + offset) == 0, "ReadArgs defaults must be zeroed.");
            invocation.Reads++;
            if (invocation.Definition.ParserError != 0)
            {
                invocation.IoError = invocation.Definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 40, "RDArgs", true);
            if (invocation.Definition.Number is int value)
            {
                Require(resultCount > 0, "Numeric fixture result has no declared result slot.");
                Bus.Long(rdArgs + 32, unchecked((uint)value));
                Bus.Long(results, rdArgs + 32);
            }
            if (invocation.Definition.FailSwitch)
            {
                Require(resultCount > 1, "Switch fixture result has no declared result slot.");
                Bus.Long(results + 4, uint.MaxValue);
            }
            if (invocation.Definition.ParserSuccessError is int successError)
                invocation.IoError = successError;
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) => invocation.OutputBptr);
        Register(baseAddress, DosLvo.Close, "Close", (_, _) => throw new InvalidOperationException("Probe closed a borrowed stream."));
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            Require(invocation.Definition.ArgumentBoundary is null, "Argument boundary probe unexpectedly wrote output.");
            Require(state.D[1] == invocation.OutputBptr, "Borrowed output handle changed between processes.");
            Require(state.D[2] == invocation.Arguments && state.D[3] == Encoding.Latin1.GetByteCount(invocation.Definition.Arguments),
                "D0/A0 startup argument bytes or length were not preserved.");
            var count = invocation.Definition.WriteResult ?? checked((int)state.D[3]);
            if (count > 0) invocation.Output.Write(Bus.Memory, (int)state.D[2], count);
            if (count != state.D[3]) invocation.IoError = 221;
            return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            var previous = invocation.IoError;
            invocation.IoError = unchecked((int)state.D[1]);
            Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, state.D[1]);
            return unchecked((uint)previous);
        });
    }
}
