using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>Runs the actual compiled Send body against supplied Exec/DOS vectors.</summary>
internal static class LoadResourceProtocolRuntimeSuite
{
    public const string Suite = "loadresource-wb31-protocol-runtime";
    private const uint Load = 0x100000, Return = 0x2000, ExecBase = 0x4000;
    private sealed record Case(string Id, string[] Names, uint Lock = 0, uint Unlock = 0,
        int ReplyResult = 0, int ReplyError = 0, int ParserError = 0,
        bool AllocationFailure = false, bool MissingDos = false,
        bool MissingTask = false, bool MissingPort = false, bool InactiveLease = false,
        bool NullStreams = false, int CleanupError = 0);

    private sealed class State(Case test, int slot)
    {
        public Case Test { get; } = test;
        public Invocation Owner { get; } = new(new ProbeCase(test.Id, "", 0, 0, ""), slot);
        public uint ServicePort => Test.MissingPort ? 0u : 0x18000u + (uint)Owner.Slot * 0x100;
        public uint Directory => Test.NullStreams ? 0u : 0x1234u + (uint)Owner.Slot;
        public uint Input => Test.NullStreams ? 0u : 0x2345u + (uint)Owner.Slot;
        public uint Output => Test.NullStreams ? 0u : 0x3456u + (uint)Owner.Slot;
        public uint Parser;
        public uint Names;
        public uint Request;
        public byte[] ParserSnapshot = [];
        public byte[] RequestSnapshot = [];
        public int Stage;
        public bool Sends => !Test.MissingDos && !Test.AllocationFailure &&
            Test.ParserError == 0 && !Test.InactiveLease && !Test.MissingPort && !Test.MissingTask;
    }

    public static int Run(string[] args)
    {
        var path = Path.GetFullPath(args[2]);
        Require(!File.Exists(path), "Use a fresh runtime receipt path.");
        Require(!string.Equals(path, Path.GetFullPath(args[0]), StringComparison.OrdinalIgnoreCase),
            "Receipt must not overwrite the HUNK.");
        var reports = new List<object>();
        string? failure = null;
        string? imageHash = null;
        var writes = 0;
        try
        {
            var model = args[1] switch
            {
                "68000" => M68kCpuModel.M68000, "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040, _ => throw new ArgumentException("Unknown CPU.")
            };
            var image = HunkImage.Load(args[0], Load);
            imageHash = image.Sha256;
            var fixture = new Fixture(image, model);
            Case[] cases =
            [
                new("list-no-names", []),
                new("one-name", ["LIBS:utility.library"]),
                new("multiple-names-lock", ["Fonts:topaz.font", "Locale:Catalogs/sys.catalog"], Lock: uint.MaxValue),
                new("unlock", ["LIBS:utility.library"], Unlock: uint.MaxValue),
                new("both-switches-forwarded", ["DEVS:sample.device"], Lock: uint.MaxValue, Unlock: uint.MaxValue),
                new("warn-reply", [], ReplyResult: 5, ReplyError: 205),
                new("error-reply", ["missing"], ReplyResult: 10, ReplyError: 103),
                new("fail-reply", [], ReplyResult: 20, ReplyError: 212),
                new("full-long-reply", ["Données:Été.library"], ReplyResult: -1, ReplyError: int.MinValue),
                new("null-borrowed-handles", [], NullStreams: true),
                new("cleanup-clobbers-error", ["LIBS:utility.library"], ReplyResult: 10, ReplyError: 205, CleanupError: 901),
                new("parser-failure", [], ParserError: 116),
                new("result-allocation-failure", [], AllocationFailure: true),
                new("missing-dos", [], MissingDos: true),
                new("missing-task", [], MissingTask: true),
                new("missing-port", [], MissingPort: true),
                new("inactive-parser-lease", [], InactiveLease: true)
            ];
            foreach (var test in cases) reports.AddRange(fixture.Execute([test], false));
            reports.AddRange(fixture.Execute([
                cases[2] with { Id = "interleaved-lock" },
                cases[3] with { Id = "interleaved-unlock", ReplyResult = 10, ReplyError = 202 }
            ], true));
            writes = fixture.NativeWrites;
        }
        catch (Exception error) { failure = error.ToString(); }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            schemaVersion = 1, suite = Suite, status = failure is null ? "passed" : "failed",
            failure, cpu = args[1], imageSha256 = imageHash,
            executorSha256 = Hash(typeof(Program).Assembly.Location),
            instructionCoreSha256 = Hash(typeof(M68kCoreFactory).Assembly.Location),
            realKickstartExecution = false, realDosParser = false, realWorkerExecution = false,
            referenceCommandBehavior = false, shippingOrPureApproval = false,
            sharedImageWrites = failure is null ? 0 : (int?)null,
            nativeWrites = writes, passed = failure is null ? reports.Count : 0, cases = reports
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"{(failure is null ? "PASS" : "FAIL")} {args[1]} {Suite}: {reports.Count} invocations.");
        if (failure is not null) Console.Error.WriteLine(failure);
        return failure is null ? 0 : 1;
    }

    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    private sealed class Fixture
    {
        private readonly CommandTestBus bus = new();
        private readonly Dictionary<Invocation, State> states = [];
        private readonly HashSet<uint> gateways = [];
        private readonly M68kCpuModel model;
        private readonly int codeLength;
        public int NativeWrites => bus.NativeWrites;
        private State Current => states[bus.Current!];

        public Fixture(HunkImage image, M68kCpuModel model)
        {
            this.model = model;
            codeLength = image.Code.Length;
            bus.LoadAndProtect(Load, image.Code);
            bus.Long(4, ExecBase);
            Register(ExecBase, ExecLvo.OpenLibrary, "OpenLibrary", (s, c) =>
            {
                Require(bus.CString(s.A[1]) == "dos.library" && s.D[0] == 39 && c.Owner.Opens++ == 0,
                    "DOS open ABI differs.");
                return c.Test.MissingDos ? 0 : c.Owner.DosBase;
            });
            Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", (s, c) =>
            {
                Require(s.A[1] == c.Owner.DosBase && c.Owner.Closes++ == 0, "DOS close ownership differs.");
                bus.AssertReleased(c.Owner);
                return 0;
            });
            Register(ExecBase, ExecLvo.AllocMem, "AllocMem", (s, c) =>
            {
                Require(s.D[0] == 12 && s.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "ReadArgs result allocation differs.");
                c.Owner.Allocations++;
                return c.Test.AllocationFailure ? 0 : bus.Allocate(c.Owner, 12, "Exec", true);
            });
            Register(ExecBase, ExecLvo.FreeMem, "FreeMem", (s, c) =>
            {
                Require(!c.Sends || c.Stage == 3, "Result storage released before the reply was consumed.");
                bus.Release(c.Owner, s.A[1], "Exec", s.D[0]); c.Owner.FreeMem++;
                if (c.Test.CleanupError != 0) c.Owner.IoError = c.Test.CleanupError;
                return 0;
            });
            Register(ExecBase, ExecLvo.FindTask, "FindTask", (s, c) =>
            {
                Require(s.A[1] == 0, "FindTask must request the current process.");
                return c.Test.MissingTask ? 0 : c.Owner.Process;
            });
            Register(ExecBase, ExecLvo.PutMsg, "PutMsg", (s, c) =>
            {
                Require(c.Sends && c.Stage == 0 && s.A[0] == c.ServicePort,
                    "Request sent on the wrong path or to the wrong worker port.");
                c.Request = s.A[1];
                Require(c.Request >= c.Owner.StackTop - c.Owner.StackBytes && c.Request + 54 <= c.Owner.StackTop,
                    "Request is not held on the calling process stack.");
                Require(bus.Memory[c.Request + 8] == (byte)NodeType.Message &&
                    bus.Long(c.Request + 14) == c.Owner.Port && bus.Word(c.Request + 18) == 54 &&
                    bus.Word(c.Request + 20) == 0 && bus.Long(c.Request + 30) == c.Directory &&
                    bus.Long(c.Request + 34) == c.Input && bus.Long(c.Request + 38) == c.Output &&
                    bus.Long(c.Request + 42) == c.Names && bus.Long(c.Request + 46) == c.Test.Lock &&
                    bus.Long(c.Request + 50) == c.Test.Unlock,
                    "Captured 54-byte request ABI differs: " + Convert.ToHexString(bus.Memory.AsSpan((int)c.Request, 54)) +
                    $"; expected reply={c.Owner.Port:X8}, cwd={c.Directory:X8}, input={c.Input:X8}, output={c.Output:X8}, names={c.Names:X8}.");
                AssertParserLive(c);
                c.RequestSnapshot = bus.Memory.AsSpan((int)c.Request, 54).ToArray();
                c.Stage = 1;
                return 0;
            });
            Register(ExecBase, ExecLvo.WaitPort, "WaitPort", (s, c) =>
            {
                Require(c.Stage == 1 && s.A[0] == c.Owner.Port, "WaitPort order or caller port differs.");
                AssertParserLive(c);
                Require(bus.Memory.AsSpan((int)c.Request, 54).SequenceEqual(c.RequestSnapshot),
                    "Request changed before worker handling.");
                c.Stage = 2;
                return c.Request;
            });
            Register(ExecBase, ExecLvo.GetMsg, "GetMsg", (s, c) =>
            {
                Require(c.Stage == 2 && s.A[0] == c.Owner.Port, "GetMsg order or caller port differs.");
                AssertParserLive(c);
                // Supply replies only when GetMsg completes, detecting early result reads.
                bus.Long(c.Request + 22, unchecked((uint)c.Test.ReplyResult));
                bus.Long(c.Request + 26, unchecked((uint)c.Test.ReplyError));
                c.Stage = 3;
                return c.Request;
            });
            foreach (var dos in new[] { 0x8000u, 0x9000u }) RegisterDos(dos);
        }

        private void RegisterDos(uint dos)
        {
            Register(dos, DosLvo.ReadArgs, "ReadArgs", (s, c) =>
            {
                Require(bus.CString(s.D[1]) == "NAME/M,LOCK/S,UNLOCK/S" && s.D[3] == 0 && c.Owner.Reads++ == 0,
                    "ReadArgs template or call differs.");
                Require(bus.OwnedAllocation(c.Owner, s.D[2], "Exec").Size == 12 &&
                    bus.Long(s.D[2]) == 0 && bus.Long(s.D[2] + 4) == 0 && bus.Long(s.D[2] + 8) == 0,
                    "Result storage was not cleared.");
                if (c.Test.ParserError != 0) { c.Owner.IoError = c.Test.ParserError; return 0; }
                c.Parser = bus.Allocate(c.Owner, 512, "RDArgs", true);
                c.Names = c.Test.Names.Length == 0 ? 0 : c.Parser + 32;
                var text = c.Parser + 64;
                for (var index = 0; index < c.Test.Names.Length; index++)
                {
                    bus.Long(c.Names + (uint)index * 4, text);
                    var bytes = Encoding.Latin1.GetBytes(c.Test.Names[index] + "\0");
                    bytes.CopyTo(bus.Memory.AsSpan((int)text)); text += (uint)bytes.Length;
                }
                bus.Long(s.D[2], c.Names); bus.Long(s.D[2] + 4, c.Test.Lock); bus.Long(s.D[2] + 8, c.Test.Unlock);
                c.ParserSnapshot = bus.Memory.AsSpan((int)c.Parser, 512).ToArray();
                return c.Parser;
            });
            Register(dos, DosLvo.FreeArgs, "FreeArgs", (s, c) =>
            {
                Require(s.D[1] == c.Parser && c.Owner.FreeArgs++ == 0 && (!c.Sends || c.Stage == 3),
                    "Parser lease released before reply or more than once.");
                AssertParserLive(c);
                bus.Release(c.Owner, c.Parser, "RDArgs");
                if (c.Test.CleanupError != 0) c.Owner.IoError = c.Test.CleanupError;
                return 0;
            });
            Register(dos, DosLvo.Input, "Input", (_, c) => c.Input);
            Register(dos, DosLvo.Output, "Output", (_, c) => c.Output);
            Register(dos, DosLvo.IoErr, "IoErr", (_, c) => unchecked((uint)c.Owner.IoError));
            Register(dos, DosLvo.SetIoErr, "SetIoErr", (s, c) =>
            {
                var previous = c.Owner.IoError; c.Owner.IoError = unchecked((int)s.D[1]);
                return unchecked((uint)previous);
            });
        }

        private void AssertParserLive(State c)
        {
            Require(bus.OwnedAllocation(c.Owner, c.Parser, "RDArgs").Size == 512 &&
                bus.Memory.AsSpan((int)c.Parser, 512).SequenceEqual(c.ParserSnapshot),
                "Borrowed NAME/M pointers were changed or released during the request.");
        }

        public List<object> Execute(Case[] cases, bool interleaved)
        {
            var cores = new List<IM68kCore>();
            var calls = cases.Select((test, index) => new State(test, index)).ToArray();
            var reports = new List<object>();
            try
            {
                foreach (var call in calls)
                {
                    var owner = call.Owner; states.Add(owner, call); bus.Current = owner;
                    bus.Memory.AsSpan((int)owner.Process, 0x400).Clear();
                    bus.Long(owner.Process + (uint)DosLayout.Process.CurrentDirectory, call.Directory);
                    bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), (int)owner.StackBytes + 32).Fill(0xb6);
                    var cpu = M68kCoreFactory.Default.Create(model, bus); cores.Add(cpu);
                    cpu.State.StatusRegister = 0; cpu.BeginSubroutine(Load, owner.StackTop, Return);
                    for (var i = 0; i < 8; i++) cpu.State.D[i] = (uint)(0xde000000 + i * 16);
                    for (var i = 0; i < 7; i++) cpu.State.A[i] = (uint)(0xae000000 + i * 16);
                    cpu.State.D[0] = call.Test.InactiveLease ? 1u : 0;
                    cpu.State.A[0] = call.ServicePort;
                }
                while (cores.Any(cpu => cpu.State.ProgramCounter != Return))
                {
                    for (var i = 0; i < cores.Count; i++)
                    {
                        var cpu = cores[i]; var call = calls[i];
                        if (cpu.State.ProgramCounter == Return) continue;
                        bus.Current = call.Owner;
                        bus.Long(ExecBase + (uint)ExecLayout.ExecBase.ThisTask, call.Owner.Process);
                        Require(++call.Owner.Instructions < 100_000 && !cpu.State.Halted && !cpu.State.Stopped,
                            $"{call.Test.Id}: native execution did not finish.");
                        var pc = cpu.State.ProgramCounter;
                        Require(pc >= Load && pc < Load + codeLength || gateways.Contains(pc), $"Unexpected PC ${pc:X8}.");
                        cpu.ExecuteInstruction();
                    }
                }
                for (var i = 0; i < cores.Count; i++)
                {
                    var call = calls[i]; var owner = call.Owner; var test = call.Test; var cpu = cores[i];
                    var expectedResult = call.Sends ? test.ReplyResult : test.ParserError != 0 ? 10 : 20;
                    var expectedError = call.Sends ? test.ReplyError : test.MissingDos ? Invocation.InitialIoError :
                        test.ParserError != 0 ? test.ParserError : test.AllocationFailure ? 103 : 0;
                    Require(unchecked((int)cpu.State.D[0]) == expectedResult && owner.IoError == expectedError,
                        $"{test.Id}: reply result/error differs ({cpu.State.D[0]}/{owner.IoError}, expected {expectedResult}/{expectedError}).");
                    var parsed = !test.MissingDos && !test.AllocationFailure && !test.InactiveLease;
                    Require(owner.Reads == (parsed ? 1 : 0) && owner.FreeArgs == (parsed && test.ParserError == 0 ? 1 : 0) &&
                        owner.Opens == 1 && owner.Closes == (test.MissingDos ? 0 : 1) && call.Stage == (call.Sends ? 3 : 0),
                        $"{test.Id}: parser/library/message lifetime differs.");
                    Require(cpu.State.A[7] == owner.StackTop &&
                        bus.Memory.AsSpan((int)owner.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0 &&
                        bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), 16).IndexOfAnyExcept((byte)0xb6) < 0,
                        "Caller stack was not restored or its guards changed.");
                    bus.AssertReleased(owner); bus.AssertImageUnchanged();
                    reports.Add(new { name = test.Id, instructionInterleaved = interleaved,
                        result = unchecked((int)cpu.State.D[0]), ioErr = owner.IoError,
                        requestBytes = call.Sends ? 54 : 0, request = call.Request,
                        instructions = owner.Instructions, events = owner.Events });
                }
                return reports;
            }
            finally
            {
                foreach (var cpu in cores) cpu.Dispose();
                foreach (var call in calls) { states.Remove(call.Owner); call.Owner.Output.Dispose(); }
                bus.Current = null;
            }
        }

        private void Register(uint library, short offset, string name, Func<M68kCpuState, State, uint> handler)
        {
            var address = checked((uint)(library + offset)); gateways.Add(address);
            bus.RegisterGateway(address, cpu =>
            {
                var call = Current;
                Require(cpu.A[6] == library, $"{name}: library base ABI differs.");
                if (library != ExecBase)
                    Require(library == call.Owner.DosBase && call.Owner.Opens == 1 && call.Owner.Closes == 0,
                        $"{name}: DOS base belongs to a different caller or closed lease.");
                call.Owner.Events.Add(name);
                var result = handler(cpu, call);
                cpu.D[0] = result; cpu.D[1] = 0xd1d1d1d1; cpu.A[0] = 0xa0a0a0a0; cpu.A[1] = 0xa1a1a1a1;
                cpu.StatusRegister = (ushort)((cpu.StatusRegister & 0xffe0) | 0x001f);
            });
        }
    }
}
