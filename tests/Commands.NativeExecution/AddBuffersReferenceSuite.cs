using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

// Execute both binaries against identical supplied public-vector responses.
// Do not seed expected command results from the replacement implementation.
internal static class AddBuffersReferenceSuite
{
    public const string Suite = "addbuffers-wb31-reference-vector-fixture";
    private const string OriginalHash = "49be9c1cf1fb87c60fe8d6cc30c73aed6615654763833f3247fc17aecdc6b1ca";
    internal sealed record Case(string Id, int? Count, int HandlerResult, int HandlerError = 0,
        int ParserError = 0, bool MissingDos = false, int? OutputResult = null,
        int CleanupError = 0, int QueryResult = 23, int QueryError = 19,
        bool AllocationFailure = false, bool FaultFailure = false);
    internal sealed record Observation(string Id, int Result, int Error, string Output,
        string[] Semantics, string[] Events, int Instructions);

    private static Case[] Cases() =>
    [
        new("positive-count", 3, 7), new("omitted-count", null, -1, 12),
        new("explicit-zero", 0, -1, 12), new("negative-count", -3, -1, 9),
        new("count-min", int.MinValue, -1, 4), new("count-max", int.MaxValue, -1, 4),
        new("handler-failure", 1, 0, 205), new("handler-failure-zero-error", 1, 0),
        new("parse-required", null, 0, ParserError: 116),
        new("parse-number", null, 0, ParserError: 115),
        new("parse-extra", null, 0, ParserError: 118),
        new("missing-dos", null, 0, MissingDos: true),
        new("short-output", 3, -1, 7, OutputResult: 2),
        new("failed-output", 3, -1, 7, OutputResult: -1),
        new("cleanup-clobber-success", 3, -1, 7, CleanupError: 901),
        new("cleanup-clobber-failure", 3, 0, 205, CleanupError: 901),
        new("omitted-positive", null, 23, 19), new("omitted-zero", null, 0, 205),
        new("query-minus-one", 3, 1, QueryResult: -1, QueryError: 23),
        new("query-zero", 3, 1, QueryResult: 0, QueryError: 205),
        new("query-one", 3, 1, QueryResult: 1),
        new("query-negative", 3, 1, QueryResult: -2),
        new("omitted-negative", null, -2),
        new("change-negative", 3, -2),
        new("query-max", 3, 1, QueryResult: int.MaxValue),
        new("fault-output-failure", 3, 0, 205, FaultFailure: true),
        new("parser-fault-output-failure", null, 0, ParserError: 116, FaultFailure: true),
    ];

    public static int Run(string[] args)
    {
        var original = new List<Observation>();
        var generated = new List<Observation>();
        var comparisons = new List<object>();
        Observation? allocationFailure = null;
        string? failure = null;
        string? generatedHash = null;
        var status = "failed";
        var reportPath = Path.GetFullPath(args[2]);
        // Validate output before any failure-report write, including aliasing of inputs.
        Require(!string.Equals(reportPath, Path.GetFullPath(args[0]), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(reportPath, Path.GetFullPath(args[4]), StringComparison.OrdinalIgnoreCase),
            "Report must not overwrite an input HUNK.");
        Require(!File.Exists(reportPath), "Use a fresh reference report path; historical evidence is immutable.");
        try
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root is not null && !File.Exists(Path.Combine(root.FullName, "CopperOS.sln"))) root = root.Parent;
            Require(root is not null, "Cannot establish repository boundary.");
            var relative = Path.GetRelativePath(root!.FullName, Path.GetFullPath(args[4]));
            Require(Path.IsPathRooted(relative) || relative.StartsWith(".." + Path.DirectorySeparatorChar),
                "Original command must stay outside the repository.");
            Require(new FileInfo(args[4]).Length == 444 && Hash(args[4]) == OriginalHash,
                "Original must match the pinned AddBuffers 37.2 member.");
            generatedHash = Hash(args[0]);
            Require(generatedHash != OriginalHash, "Original cannot stand in for the replacement.");
            var model = args[1] switch { "68000" => M68kCpuModel.M68000, "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040, _ => throw new ArgumentException("Unknown CPU.") };
            // Reference CPU is fixed and reported explicitly. The original's
            // 68020 run is retained separately as unsupported by this executor.
            var reference = new Fixture(HunkImage.Load(args[4], Fixture.Load), M68kCpuModel.M68000);
            var candidate = new Fixture(HunkImage.Load(args[0], Fixture.Load), model);
            foreach (var test in Cases())
            {
                original.Add(reference.Execute(test));
                generated.Add(candidate.Execute(test));
            }
            allocationFailure = candidate.Execute(new Case("generated-allocation-failure", 3, 1, AllocationFailure: true));
            Require(allocationFailure.Result == 20 && allocationFailure.Error == 103 &&
                allocationFailure.Semantics.SequenceEqual(new[] { "PrintFault:103:<null>" }), "Allocation failure policy differs.");
            for (var index = 0; index < original.Count; index++)
            {
                var a = original[index]; var b = generated[index];
                comparisons.Add(new { id = a.Id, passed = Equivalent(a, b),
                    result = a.Result == b.Result, error = a.Error == b.Error,
                    output = a.Output == b.Output, semantics = a.Semantics.SequenceEqual(b.Semantics) });
            }
            Require(Hash(args[4]) == OriginalHash && Hash(args[0]) == generatedHash, "Input changed during execution.");
            Require(original.Count == Cases().Length && original.Zip(generated).All(pair => Equivalent(pair.First, pair.Second)),
                "Original and replacement command observations differ; inspect the preserved comparisons.");
            status = "passed";
        }
        catch (Exception error) { failure = error.ToString(); }
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new {
            schemaVersion = 1, suite = Suite, status, failure, cpu = args[1], referenceCpu = "68000", originalSha256 = OriginalHash,
            generatedSha256 = generatedHash, executorSha256 = Hash(typeof(Program).Assembly.Location),
            cpuAssemblySha256 = Hash(typeof(M68kCoreFactory).Assembly.Location),
            realDosParser = false, realFilesystemHandler = false, realDosFormatter = false,
            shippingOrPureApproval = false, original, generated, comparisons, allocationFailure
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"{status}: AddBuffers original {original.Count}, generated {generated.Count}; {reportPath}");
        if (failure is not null) Console.Error.WriteLine(failure);
        return status == "passed" ? 0 : 1;
    }

    private static bool Equivalent(Observation a, Observation b) => a.Id == b.Id &&
        a.Result == b.Result && a.Error == b.Error && a.Output == b.Output && a.Semantics.SequenceEqual(b.Semantics);
    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    private sealed class Fixture
    {
        public const uint Load = 0x100000;
        private const uint ExecBase = 0x4000, DosBase = 0x8000, Return = 0x2000;
        private readonly CommandTestBus bus = new();
        private readonly M68kCpuModel model;
        private readonly int codeLength;
        private readonly HashSet<uint> gateways = [];
        private Case current = null!;
        private Invocation owner = null!;
        private readonly List<string> semantics = [];
        private uint rdArgs, drive, number, results;
        private byte[] borrowed = [];

        public Fixture(HunkImage image, M68kCpuModel model)
        {
            this.model = model; codeLength = image.Code.Length;
            bus.LoadAndProtect(Load, image.Code); bus.Long(4, ExecBase);
            Register(ExecBase, ExecLvo.FindTask, "FindTask", s => { Require(s.A[1] == 0, "FindTask name."); return owner.Process; });
            Register(ExecBase, ExecLvo.OpenLibrary, "OpenLibrary", s => {
                Require(bus.CString(s.A[1]) == "dos.library" && s.D[0] == 36 && owner.Opens == 0, "DOS open ABI.");
                owner.Opens++; return current.MissingDos ? 0 : DosBase;
            });
            Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", s => {
                Require(s.A[1] == DosBase && owner.Opens == 1 && owner.Closes == 0 && !current.MissingDos, "DOS close ownership.");
                bus.AssertReleased(owner); owner.Closes++; return 0;
            });
            Register(ExecBase, ExecLvo.AllocMem, "AllocMem", s => {
                Require(s.D[0] == 8 && s.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear), "Unexpected allocation.");
                owner.Allocations++; return current.AllocationFailure ? 0 : bus.Allocate(owner, s.D[0], "Exec", true);
            });
            Register(ExecBase, ExecLvo.FreeMem, "FreeMem", s => {
                bus.Release(owner, s.A[1], "Exec", s.D[0]); owner.FreeMem++; return 0;
            });
            Register(DosBase, DosLvo.ReadArgs, "ReadArgs", s => {
                Require(bus.CString(s.D[1]) == "DRIVE/A,BUFFERS/N" && s.D[3] == 0 && owner.Reads == 0, "ReadArgs ABI.");
                results = s.D[2];
                Require((results & 3) == 0 && bus.Long(results) == 0 && bus.Long(results + 4) == 0, "Uncleared results.");
                owner.Reads++;
                if (current.ParserError != 0) { Error(current.ParserError); return 0; }
                rdArgs = bus.Allocate(owner, 128, "RDArgs", true); drive = rdArgs + 32; number = rdArgs + 96;
                Encoding.Latin1.GetBytes("DF0:\0").CopyTo(bus.Memory.AsSpan((int)drive));
                bus.Long(number, unchecked((uint)(current.Count ?? 0)));
                bus.Long(results, drive); bus.Long(results + 4, current.Count is null ? 0 : number);
                borrowed = bus.Memory.AsSpan((int)rdArgs, 128).ToArray(); Error(777); return rdArgs;
            });
            Register(DosBase, DosLvo.FreeArgs, "FreeArgs", s => {
                Require(rdArgs != 0 && s.D[1] == rdArgs && owner.FreeArgs == 0, "FreeArgs ownership.");
                Require(bus.Memory.AsSpan((int)rdArgs, 128).SequenceEqual(borrowed), "Borrowed arguments changed.");
                bus.Release(owner, rdArgs, "RDArgs"); rdArgs = 0; owner.FreeArgs++;
                if (current.CleanupError != 0) Error(current.CleanupError);
                return 0;
            });
            Register(DosBase, DosLvo.AddBuffers, "AddBuffers", s => {
                var call = owner.Events.Count(e => e == "AddBuffers");
                Require(rdArgs != 0 && s.D[1] == drive && call <= 2 &&
                    unchecked((int)s.D[2]) == (call == 1 ? current.Count ?? 0 : 0), "AddBuffers ABI/lifetime.");
                semantics.Add($"AddBuffers:DF0::{unchecked((int)s.D[2])}");
                Error(call == 1 ? current.HandlerError : current.QueryError);
                return unchecked((uint)(call == 1 ? current.HandlerResult : current.QueryResult));
            });
            Register(DosBase, DosLvo.IoErr, "IoErr", _ => unchecked((uint)Error()));
            Register(DosBase, DosLvo.SetIoErr, "SetIoErr", s => { var old = Error(); Error(unchecked((int)s.D[1])); return unchecked((uint)old); });
            Register(DosBase, DosLvo.PrintFault, "PrintFault", s => {
                var code = unchecked((int)s.D[1]); var header = s.D[2] == 0 ? "<null>" : bus.CString(s.D[2]);
                semantics.Add($"PrintFault:{code}:{header}"); Error(code); return current.FaultFailure ? 0u : 1u;
            });
            Register(DosBase, DosLvo.VPrintf, "VPrintf", s => {
                var format = bus.CString(s.D[1]);
                Require(format == "%s has %ld buffers\n", $"Unexpected format: {format}");
                var name = bus.CString(bus.Long(s.D[2])); var count = unchecked((int)bus.Long(s.D[2] + 4));
                var rendered = Encoding.Latin1.GetBytes($"{name} has {count} buffers\n");
                var returned = current.OutputResult ?? rendered.Length;
                semantics.Add($"VPrintf:{name}:{count}:{returned}");
                owner.Output.Write(rendered.AsSpan(0, Math.Max(0, returned)));
                if (current.OutputResult is not null) Error(221);
                return unchecked((uint)returned);
            });
        }

        private int Error() => unchecked((int)bus.Long(owner.Process + (uint)DosLayout.Process.Result2));
        private void Error(int value) { bus.Long(owner.Process + (uint)DosLayout.Process.Result2, unchecked((uint)value)); }

        public Observation Execute(Case test)
        {
            current = test; semantics.Clear(); rdArgs = 0; borrowed = [];
            owner = new Invocation(new ProbeCase(test.Id, "not-parsed\n", 0, 0, "") {
                MissingDos = test.MissingDos, WritesOwnProcessError = true }, 0);
            bus.Current = owner; bus.Memory.AsSpan((int)owner.Process, 0x400).Clear();
            bus.Long(ExecBase + (uint)ExecLayout.ExecBase.ThisTask, owner.Process);
            bus.Long(owner.Process + (uint)DosLayout.Process.CommandLineInterface, 0x100); Error(777);
            Encoding.ASCII.GetBytes("not-parsed\n\0").CopyTo(bus.Memory.AsSpan((int)owner.Arguments));
            bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), (int)owner.StackBytes + 32).Fill(0xb6);
            using var cpu = M68kCoreFactory.Default.Create(model, bus);
            cpu.State.StatusRegister = 0; cpu.BeginSubroutine(Load, owner.StackTop, Return);
            for (var i = 0; i < 8; i++) cpu.State.D[i] = (uint)(0xde000000 + i * 16);
            for (var i = 0; i < 7; i++) cpu.State.A[i] = (uint)(0xae000000 + i * 16);
            cpu.State.D[0] = 11; cpu.State.A[0] = owner.Arguments;
            try
            {
                while (cpu.State.ProgramCounter != Return)
                {
                    var pc = cpu.State.ProgramCounter;
                    Require(pc >= Load && pc < Load + codeLength || gateways.Contains(pc), $"Unexpected PC ${pc:X8}.");
                    Require(++owner.Instructions < 100_000 && !cpu.State.Halted && !cpu.State.Stopped, "Execution did not return.");
                    cpu.ExecuteInstruction();
                }
                Require(cpu.State.A[7] == owner.StackTop, "Stack pointer not restored.");
                Require(bus.Memory.AsSpan((int)owner.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0 &&
                    bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), 16).IndexOfAnyExcept((byte)0xb6) < 0, "Stack guard changed.");
                Require(owner.Opens == 1 && owner.Closes == (test.MissingDos ? 0 : 1), "Library lifetime.");
                Require(owner.Reads == (test.MissingDos || test.AllocationFailure ? 0 : 1) &&
                    owner.FreeArgs == (test.MissingDos || test.AllocationFailure || test.ParserError != 0 ? 0 : 1), "Parser lifetime.");
                bus.AssertReleased(owner); bus.AssertImageUnchanged();
                return new(test.Id, unchecked((int)cpu.State.D[0]), Error(), Convert.ToHexStringLower(owner.Output.ToArray()),
                    semantics.ToArray(), owner.Events.ToArray(), owner.Instructions);
            }
            catch (Exception e) { throw new InvalidOperationException($"{test.Id} PC={cpu.State.ProgramCounter:X8}; events={string.Join(',', owner.Events)}", e); }
        }

        private void Register(uint baseAddress, short offset, string name, Func<M68kCpuState, uint> handler)
        {
            var address = checked((uint)(baseAddress + offset)); gateways.Add(address);
            bus.RegisterGateway(address, s => {
                Require(s.A[6] == baseAddress, $"{name}: wrong library base.");
                if (baseAddress == DosBase) Require(owner.Opens == 1 && owner.Closes == 0 && !current.MissingDos, "DOS used outside lease.");
                owner.Events.Add(name); var value = handler(s);
                s.D[0] = value; s.D[1] = 0xd1d1d1d1; s.A[0] = 0xa0a0a0a0; s.A[1] = 0xa1a1a1a1;
                s.StatusRegister = (ushort)((s.StatusRegister & 0xffe0) | 0x001f);
            });
        }
    }
}
