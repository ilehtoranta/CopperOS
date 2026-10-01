using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

// Execute both binaries against identical supplied public-vector responses.
// Do not seed expected command results from the replacement implementation.
internal static class RelabelReferenceSuite
{
    public const string Suite = "relabel-wb31-reference-vector-fixture";
    private const string OriginalHash = "163ea95df394d2c800a161befae0bea664cb6b8a07dffc2b2b1af3141588a3ff";
    internal sealed record Case(string Id, string Drive = "DH0:", string Name = "Work",
        int HandlerResult = 1, int HandlerError = 0, int ParserError = 0,
        bool MissingDos = false, int CleanupError = 0, bool Found = true,
        bool LockFails = false, int? OutputResult = null,
        bool AllocationFailure = false, bool FaultFailure = false, bool ScratchAllocationFailure = false,
        int ListError = 0, int? OutputAccepted = null, int FaultError = 0, int FreeMemError = 0);
    internal sealed record Observation(string Id, int Result, int Error, string Output,
        string[] Semantics, string[] Events, int Instructions, string? DriveAtFree);

    private static Case[] Cases() =>
    [
        new("success"), new("name-colon", Name: "Bad:Name"),
        new("drive-no-colon", Drive: "DH0"), new("empty-drive", Drive: ""),
        new("empty-name", Name: ""), new("only-colon", Drive: ":"),
        new("entry-missing", Found: false),
        new("handler-failure", HandlerResult: 0, HandlerError: 205),
        new("handler-failure-zero-error", HandlerResult: 0),
        new("handler-negative-success", HandlerResult: -1),
        new("parse-required", ParserError: 116), new("parse-extra", ParserError: 118),
        new("missing-dos", MissingDos: true),
        new("cleanup-success", CleanupError: 901),
        new("cleanup-handler-failure", HandlerResult: 0, HandlerError: 205, CleanupError: 901),
        new("cleanup-invalid-name", Name: "Bad:Name", CleanupError: 901),
        new("fault-failure", HandlerResult: 0, HandlerError: 205, FaultFailure: true),
        new("parser-fault-failure", ParserError: 116, FaultFailure: true),
        new("assign-name", Drive: "SYS:"), new("volume-spaces", Drive: "My Disk:", Name: "New Disk"),
        new("volume-latin1", Drive: "Données:", Name: "Été"),
        new("double-colon", Drive: "DH0::"), new("embedded-colon", Drive: "DH0:foo"),
        new("one-character-drive", Drive: "X"), new("maximum-safe-drive", Drive: new string('X', 126) + ":"),
        new("lock-null-found", LockFails: true), new("lock-null-not-found", LockFails: true, Found: false),
        new("invalid-name-output-failure", Name: "Bad:Name", OutputResult: -1),
        new("missing-entry-output-failure", Found: false, OutputResult: -1),
        new("successful-handler-ambient-error", HandlerError: 801),
        new("name-start-colon", Name: ":Name"), new("name-end-colon", Name: "Name:"),
        new("name-only-colon", Name: ":"),
        new("missing-entry-list-error", Found: false, ListError: 801),
        new("failed-empty-output", Name: "Bad:Name", OutputResult: -1, OutputAccepted: 0),
        new("failed-partial-output", Found: false, OutputResult: -1, OutputAccepted: 3),
        new("fault-clobber", HandlerResult: 0, HandlerError: 205, FaultError: 801),
        new("extra-storage-cleanup-clobber", HandlerError: 802, FreeMemError: 903),
    ];

    public static int Run(string[] args)
    {
        var original = new List<Observation>();
        var generated = new List<Observation>();
        var comparisons = new List<object>();
        Observation? allocationFailure = null;
        Observation? scratchAllocationFailure = null;
        Observation? longDrive = null;
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
            Require(new FileInfo(args[4]).Length == 584 && Hash(args[4]) == OriginalHash,
                "Original must match the pinned Relabel 37.2 member.");
            generatedHash = Hash(args[0]);
            Require(generatedHash != OriginalHash, "Original cannot stand in for the replacement.");
            var model = args[1] switch { "68000" => M68kCpuModel.M68000, "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040, _ => throw new ArgumentException("Unknown CPU.") };
            var reference = new Fixture(HunkImage.Load(args[4], Fixture.Load), model, true);
            var candidate = new Fixture(HunkImage.Load(args[0], Fixture.Load), model);
            foreach (var test in Cases())
            {
                original.Add(reference.Execute(test));
                generated.Add(candidate.Execute(test));
            }
            allocationFailure = candidate.Execute(new Case("generated-allocation-failure", AllocationFailure: true));

            scratchAllocationFailure = candidate.Execute(new Case("generated-scratch-allocation-failure", ScratchAllocationFailure: true));
            foreach (var failedAllocation in new[] { allocationFailure, scratchAllocationFailure })
                Require(failedAllocation.Result == 20 && failedAllocation.Error == 103 &&
                    failedAllocation.Semantics.SequenceEqual(new[] { "PrintFault:103:<null>" }), "Allocation failure policy differs.");
            // Original copies into a fixed 128-byte stack buffer. Do not execute
            // that unsafe overflow; validate the replacement's owned storage.
            longDrive = candidate.Execute(new Case("generated-long-drive", Drive: new string('X', 254) + ":"));
            Require(longDrive.Result == 0 && longDrive.Error == 0 && longDrive.Semantics.SequenceEqual(new[] {
                "LockDosList:29", "FindDosEntry:" + new string('X', 254) + ":28", "UnLockDosList:29",
                "Relabel:" + new string('X', 254) + "::Work" }), "Long drive handling differs.");
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
            schemaVersion = 1, suite = Suite, status, failure, cpu = args[1], referenceCpu = args[1], originalSha256 = OriginalHash,
            generatedSha256 = generatedHash, executorSha256 = Hash(typeof(Program).Assembly.Location),
            cpuAssemblySha256 = Hash(typeof(M68kCoreFactory).Assembly.Location),
            realDosParser = false, realFilesystemHandler = false, realDosFormatter = false,
            shippingOrPureApproval = false,
            normalization = "Compare result/error, diagnostic bytes and ordered public operations; record freed parser text separately because replacement preserves borrowed storage. Extra private allocation/cleanup and IoErr reads/restoration are excluded from ordered semantics.",
            original, generated, comparisons, allocationFailure, scratchAllocationFailure, longDrive
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n");
        Console.WriteLine($"{status}: Relabel original {original.Count}, compared generated {generated.Count}; {reportPath}");
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
        private readonly bool isReference;
        private string? driveAtFree;
        private readonly HashSet<uint> gateways = [];
        private Case current = null!;
        private Invocation owner = null!;
        private readonly List<string> semantics = [];
        private uint rdArgs, drive, name, results;
        private bool listLocked;
        private bool listRequested;
        private byte[] borrowed = [];

        public Fixture(HunkImage image, M68kCpuModel model, bool isReference = false)
        {
            this.model = model; this.isReference = isReference; codeLength = image.Code.Length;
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
                Require(s.D[0] == (owner.Allocations == 0 ? 8u : (uint)(Math.Max(0, current.Drive.Length - 1) + 2)) &&
                    s.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear), "Unexpected allocation.");
                owner.Allocations++; return current.AllocationFailure || current.ScratchAllocationFailure && owner.Allocations == 2 ? 0 : bus.Allocate(owner, s.D[0], "Exec", true);
            });
            Register(ExecBase, ExecLvo.FreeMem, "FreeMem", s => {
                bus.Release(owner, s.A[1], "Exec", s.D[0]); owner.FreeMem++;
                if (current.FreeMemError != 0) Error(current.FreeMemError);
                return 0;
            });
            Register(DosBase, DosLvo.ReadArgs, "ReadArgs", s => {
                Require(bus.CString(s.D[1]) == "DRIVE/A,NAME/A" && s.D[3] == 0 && owner.Reads == 0, "ReadArgs ABI.");
                results = s.D[2];
                Require((results & 3) == 0 && bus.Long(results) == 0 && bus.Long(results + 4) == 0, "Uncleared results.");
                owner.Reads++;
                if (current.ParserError != 0) { Error(current.ParserError); return 0; }
                rdArgs = bus.Allocate(owner, 1024, "RDArgs", true); drive = rdArgs + 32; name = rdArgs + 544;
                Encoding.Latin1.GetBytes(current.Drive + "\0").CopyTo(bus.Memory.AsSpan((int)drive));
                Encoding.Latin1.GetBytes(current.Name + "\0").CopyTo(bus.Memory.AsSpan((int)name));
                bus.Long(results, drive); bus.Long(results + 4, name);
                borrowed = bus.Memory.AsSpan((int)rdArgs, 1024).ToArray(); Error(777); return rdArgs;
            });
            Register(DosBase, DosLvo.FreeArgs, "FreeArgs", s => {
                Require(rdArgs != 0 && s.D[1] == rdArgs && owner.FreeArgs == 0, "FreeArgs ownership.");
                // Record any original parser-buffer mutation instead of forbidding
                // behavior that must first be established from the reference.
                driveAtFree = bus.CString(drive);
                if (!isReference) Require(bus.Memory.AsSpan((int)rdArgs, 1024).SequenceEqual(borrowed), "Replacement changed borrowed parser storage.");
                bus.Release(owner, rdArgs, "RDArgs"); rdArgs = 0; owner.FreeArgs++;
                if (current.CleanupError != 0) Error(current.CleanupError);
                return 0;
            });
            Register(DosBase, DosLvo.LockDosList, "LockDosList", s => {
                Require(!listLocked, "Nested list lock.");
                semantics.Add($"LockDosList:{s.D[1]}"); listRequested = true; listLocked = !current.LockFails;
                return current.LockFails ? 0u : 0x32000u;
            });
            Register(DosBase, DosLvo.FindDosEntry, "FindDosEntry", s => {
                Require(listRequested && s.D[1] == (current.LockFails ? 0u : 0x32000u), "FindDosEntry outside list request.");
                semantics.Add($"FindDosEntry:{bus.CString(s.D[2])}:{s.D[3]}");
                return current.Found ? 0x32100u : 0u;
            });
            Register(DosBase, DosLvo.UnLockDosList, "UnLockDosList", s => {
                Require(listRequested, "Unlock outside list request."); listRequested = false; listLocked = false;
                if (current.ListError != 0) Error(current.ListError);
                semantics.Add($"UnLockDosList:{s.D[1]}"); return 0;
            });
            Register(DosBase, DosLvo.Relabel, "Relabel", s => {
                Require(rdArgs != 0 && !listLocked, "Relabel argument/list lifetime.");
                semantics.Add($"Relabel:{bus.CString(s.D[1])}:{bus.CString(s.D[2])}");
                Error(current.HandlerError); return unchecked((uint)current.HandlerResult);
            });
            Register(DosBase, DosLvo.IoErr, "IoErr", _ => unchecked((uint)Error()));
            Register(DosBase, DosLvo.SetIoErr, "SetIoErr", s => { var old = Error(); Error(unchecked((int)s.D[1])); return unchecked((uint)old); });
            Register(DosBase, DosLvo.PrintFault, "PrintFault", s => {
                var code = unchecked((int)s.D[1]); var header = s.D[2] == 0 ? "<null>" : bus.CString(s.D[2]);
                semantics.Add($"PrintFault:{code}:{header}"); Error(current.FaultError != 0 ? current.FaultError : code); return current.FaultFailure ? 0u : 1u;
            });
            Register(DosBase, DosLvo.PutStr, "PutStr", s => {
                var value = bus.CString(s.D[1]); semantics.Add($"PutStr:{value}");
                var rendered = Encoding.Latin1.GetBytes(value);
                owner.Output.Write(rendered.AsSpan(0, current.OutputAccepted ?? rendered.Length));
                if (current.OutputResult is not null) Error(221);
                return unchecked((uint)(current.OutputResult ?? 0));
            });
        }

        private int Error() => unchecked((int)bus.Long(owner.Process + (uint)DosLayout.Process.Result2));
        private void Error(int value) { bus.Long(owner.Process + (uint)DosLayout.Process.Result2, unchecked((uint)value)); }

        public Observation Execute(Case test)
        {
            current = test; semantics.Clear(); rdArgs = 0; borrowed = []; listLocked = false; listRequested = false; driveAtFree = null;
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
                Require(!listLocked, "List lock leaked.");
                bus.AssertReleased(owner); bus.AssertImageUnchanged();
                return new(test.Id, unchecked((int)cpu.State.D[0]), Error(), Convert.ToHexStringLower(owner.Output.ToArray()),
                    semantics.ToArray(), owner.Events.ToArray(), owner.Instructions, driveAtFree);
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
