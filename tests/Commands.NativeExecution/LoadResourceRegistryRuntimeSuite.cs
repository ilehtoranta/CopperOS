using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>Executes the production registry through compiled native HUNK instructions.</summary>
internal static class LoadResourceRegistryRuntimeSuite
{
    public const string Suite = "loadresource-wb31-registry-runtime";
    private static readonly string[] Cases = ["empty-list-and-missing-name",
        "copied-name-tail-order-remove-and-reuse", "allocation-failure-retains-handle-and-retry",
        "close-library-before-removal", "close-font-before-removal", "close-catalog-before-removal",
        "device-type-no-close", "unknown-type-no-close", "stricmp-word-result",
        "duplicate-name-first-match-and-reuse"];

    public static int Run(string[] args)
    {
        var reportPath = Path.GetFullPath(args[2]);
        Require(!string.Equals(reportPath, Path.GetFullPath(args[0]), StringComparison.OrdinalIgnoreCase) &&
            !File.Exists(reportPath), "Use a fresh report path, different from the input HUNK.");
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
            realKickstart = false, realScheduling = false, referenceCommandExecution = false,
            shippingOrPureApproval = false, crossInvocationCodeLifetimeProven = false,
            scope = "Generated HUNK opened-resource registry in supplied public Exec/Utility/Graphics/Locale vectors: copied name ownership, AddTail insertion, first case-insensitive match with WORD comparison, allocation failure and retry, removal/reuse, type-specific close before unlink/free, and device/unknown no-close cases. Worker/resource integration, guest behavior and image lifetime remain open.",
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
        private const uint ExecBase = 0x4000, UtilityBase = 0x8000,
            GraphicsBase = 0x9000, LocaleBase = 0xa000, Return = 0x2000;
        private readonly CommandTestBus bus = new();
        private readonly Invocation owner;
        private readonly M68kCpuModel model;
        private readonly int scenario, codeLength;
        private readonly uint list;
        private readonly HashSet<uint> gateways = [];
        private readonly Dictionary<uint, Record> records = [];
        private readonly List<string> events = [];
        private int allocations, insertions, removals, closes, comparisons;

        private sealed class Record(string name, byte type, uint handle, bool shouldClose)
        {
            public string Name { get; } = name;
            public byte Type { get; } = type;
            public uint Handle { get; } = handle;
            public bool ShouldClose { get; } = shouldClose;
            public bool Closed { get; set; }
            public bool Removed { get; set; }
        }

        public Fixture(HunkImage image, M68kCpuModel model, int scenario)
        {
            this.model = model; this.scenario = scenario; codeLength = image.Code.Length;
            owner = new Invocation(new ProbeCase(Cases[scenario], "", 0, 0, ""), 0);
            bus.Current = owner; bus.LoadAndProtect(Load, image.Code); bus.Long(4, ExecBase);
            bus.Long(ExecBase + (uint)ExecLayout.ExecBase.ThisTask, owner.Process);
            list = bus.Allocate(owner, 512, "Fixture", false);
            Encoding.Latin1.GetBytes("Alpha.library\0").CopyTo(bus.Memory.AsSpan((int)list + 32));

            Register(ExecBase, ExecLvo.AllocVec, "AllocVec", s => {
                allocations++;
                var expectedName = insertions == 0 ? "Alpha.library" : insertions == 1 ?
                    scenario == 9 ? "alpha.library" : "Beta.font" : "Reuse.catalog";
                Require(s.D[0] == expectedName.Length + 19 && s.D[1] == 0,
                    "Registry allocation size or flags differs from source.");
                if (scenario == 2 && allocations == 1)
                {
                    AssertEmpty(); return 0;
                }
                return bus.Allocate(owner, s.D[0], "Record", false);
            });
            Register(ExecBase, ExecLvo.AddTail, "AddTail", s => {
                var node = s.A[1];
                Require(s.A[0] == list && !records.ContainsKey(node), "Wrong list or duplicate insertion.");
                var allocation = bus.OwnedAllocation(owner, node, "Record");
                Require(bus.Long(node + 10) == node + 18 && bus.Memory[node + 9] == 0xcd,
                    "Name storage pointer or untouched priority byte differs.");
                var name = bus.CString(node + 18); var type = bus.Memory[node + 8];
                var handle = bus.Long(node + 14);
                var expectedName = insertions == 0 ? "Alpha.library" : insertions == 1 ?
                    scenario == 9 ? "alpha.library" : "Beta.font" : "Reuse.catalog";
                var expectedType = insertions == 0 ? scenario == 4 ? 2 : scenario == 5 ? 3 :
                    scenario == 6 ? 1 : scenario == 7 ? 255 : 0 : insertions == 1 ? 2 : 3;
                Require(name == expectedName && allocation.Size == name.Length + 19 && type == expectedType &&
                    handle == (insertions == 0 ? 0x1234u : insertions == 1 ? 0x2345u : 0x3456u),
                    "Registry record name/type/handle shape differs.");
                var rawRemove = insertions == 0 && scenario is 1 or 9;
                records.Add(node, new Record(name, type, handle, !rawRemove && type is 0 or 2 or 3));
                var tail = list + 4; var previous = bus.Long(list + 8);
                Require(bus.Long(previous) == tail && bus.Long(tail) == 0,
                    "Tail sentinel or predecessor is invalid.");
                bus.Long(node, tail); bus.Long(node + 4, previous);
                bus.Long(previous, node); bus.Long(list + 8, node);
                insertions++; return 0;
            });
            Register(UtilityBase, UtilityLvo.Stricmp, "Stricmp", s => {
                comparisons++;
                var record = records.SingleOrDefault(pair => pair.Key + 18 == s.A[1]);
                Require(record.Value is not null && !record.Value.Removed,
                    "Stricmp names must belong to a live linked record.");
                var comparison = string.Compare(bus.CString(s.A[0]), bus.CString(s.A[1]),
                    StringComparison.OrdinalIgnoreCase);
                var low = comparison == 0 ? 0u : comparison < 0 ? 0xffffu : 1u;
                // Workbench tests the signed WORD return, including when upper bits are nonzero.
                return scenario == 8 ? 0xabcd0000u | low : unchecked((uint)comparison);
            });
            Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", s => Close(s.A[1], 0));
            Register(GraphicsBase, -78, "CloseFont", s => Close(s.A[1], 2));
            Register(LocaleBase, -36, "CloseCatalog", s => Close(s.A[0], 3));
            Register(ExecBase, ExecLvo.Remove, "Remove", s => {
                var node = s.A[1]; var record = records[node];
                Require(!record.Removed && record.Closed == record.ShouldClose,
                    "Resource close must precede removal, and raw Remove must not close.");
                var next = bus.Long(node); var previous = bus.Long(node + 4);
                Require(bus.Long(previous) == node && bus.Long(next + 4) == node,
                    "Removal links are inconsistent.");
                bus.Long(previous, next); bus.Long(next + 4, previous);
                record.Removed = true; removals++; return 0;
            });
            Register(ExecBase, ExecLvo.FreeVec, "FreeVec", s => {
                Require(records.TryGetValue(s.A[1], out var record) && record.Removed,
                    "Freed a live or unknown record.");
                bus.Release(owner, s.A[1], "Record"); records.Remove(s.A[1]); return 0;
            });
        }

        private uint Close(uint handle, byte type)
        {
            var pair = records.Single(pair => pair.Value.Handle == handle);
            var record = pair.Value;
            Require(record.Type == type && record.ShouldClose && !record.Closed && !record.Removed,
                "Close dispatch, ownership, or ordering differs.");
            Require(bus.Long(bus.Long(pair.Key + 4)) == pair.Key,
                "Resource closed after it was unlinked.");
            record.Closed = true; closes++; return 0;
        }

        private void AssertEmpty() => Require(bus.Long(list) == list + 4 &&
            bus.Long(list + 4) == 0 && bus.Long(list + 8) == list, "List is not an empty MinList.");

        public object Execute()
        {
            bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), (int)owner.StackBytes + 32).Fill(0xb6);
            using var cpu = M68kCoreFactory.Default.Create(model, bus);
            cpu.State.StatusRegister = 0; cpu.BeginSubroutine(Load, owner.StackTop, Return);
            for (var index = 0; index < 8; index++) cpu.State.D[index] = (uint)(0xde000000 + index * 16);
            for (var index = 0; index < 7; index++) cpu.State.A[index] = (uint)(0xae000000 + index * 16);
            cpu.State.D[0] = (uint)scenario; cpu.State.A[0] = list;
            try
            {
                while (cpu.State.ProgramCounter != Return)
                {
                    var pc = cpu.State.ProgramCounter;
                    Require(pc >= Load && pc < Load + codeLength || gateways.Contains(pc), $"Unexpected PC ${pc:X8}.");
                    Require(++owner.Instructions < 100_000 && !cpu.State.Halted && !cpu.State.Stopped,
                        "Execution did not return.");
                    cpu.ExecuteInstruction();
                }
                Require(cpu.State.D[0] == 0, $"Native registry rejected stage {cpu.State.D[0]}.");
                Require(cpu.State.A[7] == owner.StackTop &&
                    bus.Memory.AsSpan((int)owner.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0 &&
                    bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), 16).IndexOfAnyExcept((byte)0xb6) < 0,
                    "Stack balance or guards changed.");
                var expectedInsertions = scenario == 0 ? 0 : scenario is 1 or 9 ? 3 : 1;
                Require(insertions == expectedInsertions && removals == expectedInsertions &&
                    allocations == expectedInsertions + (scenario == 2 ? 1 : 0) && records.Count == 0,
                    "Record lifetime counts differ.");
                Require(closes == (scenario is 0 or 6 or 7 ? 0 : scenario is 1 or 9 ? 2 : 1),
                    "Close count differs.");
                Require(comparisons == 0 == (scenario == 0), "Name lookup path was not exercised.");
                AssertEmpty();
                Require(bus.Memory.AsSpan((int)list + 12, 20).IndexOfAnyExcept((byte)0xcd) < 0,
                    "MinList initialization wrote beyond 12 bytes.");
                bus.Release(owner, list, "Fixture"); bus.AssertReleased(owner); bus.AssertImageUnchanged();
                return new { id = Cases[scenario], result = 0, owner.Instructions, allocations,
                    insertions, removals, closes, comparisons, sharedImageWrites = 0, events };
            }
            catch (Exception error)
            { throw new InvalidOperationException($"{Cases[scenario]} PC={cpu.State.ProgramCounter:X8}; events={string.Join(',', events)}", error); }
        }

        private void Register(uint library, short offset, string name, Func<M68kCpuState, uint> handler)
        {
            var address = checked((uint)(library + offset)); gateways.Add(address);
            bus.RegisterGateway(address, s => {
                Require(s.A[6] == library, $"{name}: wrong library base ${s.A[6]:X8}.");
                events.Add(name); var result = handler(s);
                s.D[0] = result; s.D[1] = 0xd1d1d1d1; s.A[0] = 0xa0a0a0a0; s.A[1] = 0xa1a1a1a1;
                s.StatusRegister = (ushort)((s.StatusRegister & 0xffe0) | 0x001f);
            });
        }
    }
}
