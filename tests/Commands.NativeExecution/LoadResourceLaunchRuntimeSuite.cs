using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal static class LoadResourceLaunchRuntimeSuite
{
    public const string Suite = "loadresource-wb31-launch-runtime";
    private sealed record Case(string Id, bool Existing = false, bool CreateFailure = false,
        int CreateError = 103, string Module = "valid", bool Inactive = false,
        bool ParserFailure = false, bool MissingTask = false, bool NullName = false,
        bool NullHandles = false, int ReplyResult = 0, int ReplyError = 0)
    {
        public bool Parsed => !Inactive && !ParserFailure;
        public bool Enters => Parsed && !MissingTask && !NullName;
        public bool Creates => Enters && !Existing && Module == "valid";
        public bool Sends => Enters && (Existing || Module == "valid" && !CreateFailure);
        public bool Prints => Enters && !Sends;
        public int Result => Sends ? ReplyResult : 20;
        public int Error => ParserFailure ? 116 : !Enters ? 0 : Sends ? ReplyError : Creates ? CreateError : 212;
    }
    private static readonly Case[] Cases = [
        new("existing-worker-request", Existing: true),
        new("existing-worker-failure", Existing: true, ReplyResult: 20, ReplyError: 212),
        new("new-worker-detaches-before-create"),
        new("new-worker-error-reply", ReplyResult: 10, ReplyError: 205),
        new("failed-create-restores-original-chain", CreateFailure: true),
        new("failed-create-preserves-zero-error", CreateFailure: true, CreateError: 0),
        new("existing-service-needs-no-new-module", Existing: true, Module: "null"),
        new("missing-command-module", Module: "null"),
        new("missing-worker-tail", Module: "empty"),
        new("self-linked-tail-rejected", Module: "self"),
        new("inactive-parser-lease", Inactive: true),
        new("failed-parser-lease", ParserFailure: true),
        new("missing-caller-task", MissingTask: true),
        new("null-service-name", NullName: true),
        new("new-worker-null-borrowed-handles", NullHandles: true)
    ];

    public static int Run(string[] args)
    {
        var path = Path.GetFullPath(args[2]);
        Require(!File.Exists(path) && !string.Equals(path, Path.GetFullPath(args[0]), StringComparison.OrdinalIgnoreCase),
            "Use a fresh report path, different from the HUNK.");
        var observations = new List<object>(); string? failure = null, hash = null;
        try
        {
            hash = Hash(args[0]);
            var model = args[1] switch { "68000" => M68kCpuModel.M68000, "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040, _ => throw new ArgumentException("Unknown CPU.") };
            var image = HunkImage.Load(args[0], Fixture.Load);
            foreach (var test in Cases) observations.Add(new Fixture(image, model, test).Execute());
            Require(Hash(args[0]) == hash, "HUNK changed during execution.");
        }
        catch (Exception error) { failure = error.ToString(); }
        var passed = failure is null && observations.Count == Cases.Length;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new {
            schemaVersion = 1, suite = Suite, status = passed ? "passed" : "failed", failure,
            cpu = args[1], hunkSha256 = hash, executorSha256 = Hash(typeof(Program).Assembly.Location),
            cpuAssemblySha256 = Hash(typeof(M68kCoreFactory).Assembly.Location),
            realKickstart = false, realScheduling = false, realCreateNewProc = false,
            referenceCommandExecution = false, shippingOrPureApproval = false,
            crossInvocationCodeLifetimeProven = false,
            scope = "Compiled production client launch and synchronous request with supplied Exec/DOS vectors. Verifies exact classic process tags, detach-before-create/rollback, existing-service reuse, actual first request rather than startup acknowledgment, argument lease through reply, error capture and Forbid balance. SegList headers are fixture-owned data, not executing child code. Child scheduling, actual DOS exit/unload, complete client entry and callback quiescence remain open.",
            observations
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"{(passed ? "passed" : "failed")}: {Suite} {observations.Count}/{Cases.Length}; {path}");
        if (failure is not null) Console.Error.WriteLine(failure);
        return passed ? 0 : 1;
    }
    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    private sealed class Fixture
    {
        public const uint Load = 0x100000;
        private const uint Return = 0x2000, ExecBase = 0x4000, DosBase = 0x8000,
            ExistingPort = 0x18000, Child = 0x19000;
        private readonly CommandTestBus bus = new();
        private readonly Invocation owner;
        private readonly M68kCpuModel model;
        private readonly Case test;
        private readonly int codeLength;
        private readonly HashSet<uint> gateways = [];
        private readonly List<string> events = [];
        private readonly uint context, head, tail, name, names, originalLink;
        private uint array, parser, request;
        private int forbid, creates, finds, sends, stage, faults, parses;
        private bool argsLive;
        private uint ReplyPort => owner.Process + (uint)DosLayout.Process.MessagePort;

        public Fixture(HunkImage image, M68kCpuModel model, Case test)
        {
            this.model = model; this.test = test; codeLength = image.Code.Length;
            owner = new Invocation(new ProbeCase(test.Id, "", 0, 0, "") { StackBytes = 4096 }, 0);
            bus.Current = owner; bus.LoadAndProtect(Load, image.Code); bus.Long(4, ExecBase);
            bus.Long(ExecBase + (uint)ExecLayout.ExecBase.ThisTask, owner.Process);
            bus.Long(owner.Process + (uint)DosLayout.Process.CurrentDirectory, test.NullHandles ? 0u : 0x456);
            context = bus.Allocate(owner, 128, "Fixture", true);
            head = bus.Allocate(owner, 16, "ClientHeader", true);
            tail = bus.Allocate(owner, 64, "WorkerHeader", true);
            name = tail + 32; Put(name, "« LoadResource »");
            names = context + 32; bus.Long(names, context + 48); Put(context + 48, "Libs:Alpha.library");
            originalLink = test.Module == "empty" ? 0 : test.Module == "self" ? head >> 2 : tail >> 2;
            bus.Long(head, originalLink);
            bus.Long(context, test.Module == "null" ? 0 : head >> 2);
            bus.Long(context + 4, test.NullName ? 0 : name);
            RegisterVectors();
        }
        private void Put(uint address, string value) => Encoding.Latin1.GetBytes(value + '\0').CopyTo(bus.Memory.AsSpan((int)address));

        private void RegisterVectors()
        {
            Register(ExecBase, ExecLvo.AllocMem, "AllocMem", s => {
                Require(!test.Inactive && s.D[0] == 12 && s.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear), "Result allocation differs.");
                array = bus.Allocate(owner, 12, "Results", true); return array;
            });
            Register(ExecBase, ExecLvo.FreeMem, "FreeMem", s => {
                Require(!argsLive && forbid == 0, "Result array freed while request/parser is live.");
                bus.Release(owner, s.A[1], "Results", s.D[0]); array = 0; owner.IoError = 777; return 0;
            });
            Register(DosBase, DosLvo.ReadArgs, "ReadArgs", s => {
                Require(bus.CString(s.D[1]) == "NAME/M,LOCK/S,UNLOCK/S" && s.D[2] == array && s.D[3] == 0 && parses++ == 0,
                    "Original ReadArgs grammar/call differs.");
                if (test.ParserFailure) { owner.IoError = 116; return 0; }
                parser = bus.Allocate(owner, 32, "Parser", true); argsLive = true;
                bus.Long(array, names); bus.Long(array + 4, uint.MaxValue); bus.Long(array + 8, 0); return parser;
            });
            Register(DosBase, DosLvo.FreeArgs, "FreeArgs", s => {
                Require(s.D[1] == parser && argsLive && forbid == 0 && (!test.Sends || stage == 3), "Parser lease freed before reply/Permit.");
                Require(bus.Long(array) == names && bus.Long(array + 4) == uint.MaxValue && bus.Long(array + 8) == 0 &&
                    bus.Long(names) == context + 48 && bus.Long(names + 4) == 0 && bus.CString(context + 48) == "Libs:Alpha.library",
                    "Client launch changed borrowed parser results or NAME/M payload.");
                bus.Release(owner, parser, "Parser"); parser = 0; argsLive = false; owner.IoError = 778; return 0;
            });
            Register(DosBase, DosLvo.IoErr, "IoErr", _ => unchecked((uint)owner.IoError));
            Register(DosBase, DosLvo.SetIoErr, "SetIoErr", s => { var old = owner.IoError; owner.IoError = unchecked((int)s.D[1]); return unchecked((uint)old); });
            Register(ExecBase, ExecLvo.FindTask, "FindTask", s => { Require(s.A[1] == 0, "FindTask ABI."); return test.MissingTask ? 0 : owner.Process; });
            Register(ExecBase, ExecLvo.Forbid, "Forbid", _ => { Require(test.Enters && forbid == 0, "Unexpected/nested Forbid."); forbid++; return 0; });
            Register(ExecBase, ExecLvo.Permit, "Permit", _ => {
                Require(forbid == 1 && (!test.Sends || stage == 3), "Permit before completed transaction."); forbid--; owner.IoError = 779; return 0;
            });
            Register(ExecBase, ExecLvo.FindPort, "FindPort", s => {
                Require(forbid == 1 && argsLive && bus.CString(s.A[1]) == "« LoadResource »" && finds++ == 0, "Service identity/lookup serialization differs.");
                return test.Existing ? ExistingPort : 0;
            });
            Register(DosBase, DosLvo.CreateNewProc, "CreateNewProc", s => {
                Require(test.Creates && forbid == 1 && argsLive && creates++ == 0 && bus.Long(head) == 0,
                    "Worker chain not detached before CreateNewProc, or duplicate creation.");
                (uint Tag, uint Value)[] expected = [
                    (0x800003e9, tail >> 2), (0x800003ea, 1), (0x800003ec, 0), (0x800003ed, 0),
                    (0x800003f2, 0), (0x800003f3, 3000), (0x800003f4, name), (0x800003f5, 0),
                    (0x800003f7, 0), (0x800003f8, 0), (0x800003f9, 0)
                ];
                Require(s.D[1] >= owner.StackTop - owner.StackBytes && s.D[1] + 92 <= owner.StackTop, "Tags not on caller stack.");
                for (var i = 0; i < expected.Length; i++) Require(bus.Long(s.D[1] + (uint)i * 8) == expected[i].Tag &&
                    bus.Long(s.D[1] + (uint)i * 8 + 4) == expected[i].Value, "Wrong classic CreateNewProc tags or post-3.1 tag used.");
                Require(bus.Long(s.D[1] + 88) == 0 && bus.CString(name) == "« LoadResource »", "Tag termination/name lifetime differs.");
                owner.IoError = test.CreateError; return test.CreateFailure ? 0 : Child;
            });
            Register(DosBase, DosLvo.Input, "Input", _ => test.NullHandles ? 0u : 0x123u);
            Register(DosBase, DosLvo.Output, "Output", _ => test.NullHandles ? 0u : 0x234u);
            Register(ExecBase, ExecLvo.PutMsg, "PutMsg", s => {
                Require(test.Sends && forbid == 1 && argsLive && sends++ == 0 && stage == 0 &&
                    s.A[0] == (test.Existing ? ExistingPort : Child + (uint)DosLayout.Process.MessagePort), "Initial request target/ownership differs.");
                request = s.A[1];
                Require(request >= owner.StackTop - owner.StackBytes && request + 54 <= owner.StackTop,
                    "Request is not wholly within the waiting caller's stack.");
                Require(bus.Memory[request + 8] == (byte)NodeType.Message && bus.Long(request + 14) == ReplyPort &&
                    bus.Word(request + 18) == 54 && bus.Word(request + 20) == 0 && bus.Long(request + 42) == names &&
                    bus.Long(request + 46) == uint.MaxValue && bus.Long(request + 50) == 0, "Sent startup acknowledgment instead of real resource request.");
                Require(bus.Long(request + 30) == (test.NullHandles ? 0u : 0x456) &&
                    bus.Long(request + 34) == (test.NullHandles ? 0u : 0x123) && bus.Long(request + 38) == (test.NullHandles ? 0u : 0x234), "Borrowed context differs.");
                Require(bus.Long(head) == (test.Existing ? originalLink : 0), "Wrong module owner at handoff.");
                stage = 1; return 0;
            });
            Register(ExecBase, ExecLvo.WaitPort, "WaitPort", s => {
                Require(s.A[0] == ReplyPort && stage == 1 && forbid == 1 && argsLive, "WaitPort or parser lifetime differs.");
                Require(bus.CString(bus.Long(names)) == "Libs:Alpha.library", "Parser names not live while worker processes request.");
                stage = 2; return request;
            });
            Register(ExecBase, ExecLvo.GetMsg, "GetMsg", s => {
                Require(s.A[0] == ReplyPort && stage == 2 && argsLive, "GetMsg before actual reply.");
                bus.Long(request + 22, unchecked((uint)test.ReplyResult)); bus.Long(request + 26, unchecked((uint)test.ReplyError));
                stage = 3; owner.IoError = 780; return request;
            });
            Register(DosBase, DosLvo.PrintFault, "PrintFault", s => {
                Require(test.Prints && forbid == 1 && s.D[1] == (uint)test.Error && s.D[2] == 0 &&
                    bus.Long(head) == originalLink, "Failure error was clobbered, chain not restored, or diagnostic order differs.");
                faults++; owner.IoError = 781; return 1;
            });
        }

        public object Execute()
        {
            bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), (int)owner.StackBytes + 32).Fill(0xb6);
            using var cpu = M68kCoreFactory.Default.Create(model, bus);
            cpu.State.StatusRegister = 0; cpu.BeginSubroutine(Load, owner.StackTop, Return);
            for (var i = 0; i < 8; i++) cpu.State.D[i] = (uint)(0xde000000 + i * 16);
            for (var i = 0; i < 7; i++) cpu.State.A[i] = (uint)(0xae000000 + i * 16);
            cpu.State.D[0] = test.Inactive ? 1u : 0; cpu.State.A[0] = context;
            try
            {
                while (cpu.State.ProgramCounter != Return)
                {
                    var pc = cpu.State.ProgramCounter;
                    Require(pc >= Load && pc < Load + codeLength || gateways.Contains(pc), $"Unexpected PC ${pc:X8}.");
                    Require(++owner.Instructions < 100_000 && !cpu.State.Halted && !cpu.State.Stopped, "Execution did not return.");
                    cpu.ExecuteInstruction();
                }
                Require(cpu.State.D[0] == (uint)test.Result && bus.Long(context + 8) == (uint)test.Error && cpu.State.A[7] == owner.StackTop,
                    "Client result/error or stack balance differs.");
                Require(bus.Memory.AsSpan((int)owner.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0 &&
                    bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), 16).IndexOfAnyExcept((byte)0xb6) < 0, "Stack guard changed.");
                Require(forbid == 0 && creates == (test.Creates ? 1 : 0) && finds == (test.Enters ? 1 : 0) &&
                    sends == (test.Sends ? 1 : 0) && faults == (test.Prints ? 1 : 0) && !argsLive && array == 0 && parser == 0,
                    "Ownership or operation counts differ.");
                var transferred = test.Creates && !test.CreateFailure;
                Require(bus.Long(head) == (transferred ? 0 : originalLink), "Successful transfer or failed rollback is wrong.");
                Require(bus.CString(name) == "« LoadResource »" && bus.Long(tail) == 0, "Worker tail/name changed.");
                bus.Release(owner, head, "ClientHeader");
                // Host verifies retained data after the simulated client owner releases its header.
                // This is not a claim that real DOS ran/unloaded the separate child image.
                if (transferred) Require(bus.CString(name) == "« LoadResource »", "Worker name depended on client storage.");
                bus.Release(owner, tail, "WorkerHeader"); bus.Release(owner, context, "Fixture");
                bus.AssertReleased(owner); bus.AssertImageUnchanged();
                return new { id = test.Id, result = test.Result, ioErr = test.Error, owner.Instructions,
                    creates, sends, faults, transferred, sharedImageWrites = 0,
                    stackBytesWritten = owner.StackTop - owner.LowestStackWrite, events };
            }
            catch (Exception error) { throw new InvalidOperationException($"{test.Id} PC={cpu.State.ProgramCounter:X8}; events={string.Join(',', events)}", error); }
        }
        private void Register(uint library, short offset, string name, Func<M68kCpuState, uint> handler)
        {
            var address = checked((uint)(library + offset)); gateways.Add(address);
            bus.RegisterGateway(address, s => {
                Require(s.A[6] == library, $"{name}: wrong library base."); events.Add(name); var result = handler(s);
                s.D[0] = result; s.D[1] = 0xd1d1d1d1; s.A[0] = 0xa0a0a0a0; s.A[1] = 0xa1a1a1a1;
                s.StatusRegister = (ushort)((s.StatusRegister & 0xffe0) | 0x001f);
            });
        }
    }
}
