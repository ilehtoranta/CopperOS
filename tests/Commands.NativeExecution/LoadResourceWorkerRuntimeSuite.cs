using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>Compiled worker request dispatch, with supplied public OS vector responses.</summary>
internal static class LoadResourceWorkerRuntimeSuite
{
    public const string Suite = "loadresource-wb31-worker-runtime";
    private sealed record Seed(string Name, byte Type);
    private sealed record Pattern(string Name, string[] Paths, int FirstError = 0,
        int NextError = 232, int LoadError = 202);
    private sealed record Case(string Id, Pattern[]? Patterns = null, Seed[]? Seeds = null,
        ushort Flags = 0, uint Lock = 0, uint Unlock = 0, bool Receive = false,
        bool Locale = false, bool NullCatalog = false, bool Translate = false,
        bool NullHandles = false, bool OpenCatalog = false, int Result = 0, int Error = 0,
        string Output = "", int Visited = 0, int Closed = 0, int Created = 0, int Loads = 0, int Nexts = 0,
        bool FailFirstMessageOpen = false, bool CatalogOpenFailure = false,
        int MessageOpens = 0, int MessageCloses = 0, int MessageQueries = 0);

    private static readonly Seed Library = new("Libs:Alpha.library", 0);
    private static readonly Seed Font = new("Fonts:Topaz/8", 2);
    private static readonly Seed Catalog = new("Locale:example.catalog", 3);
    private static readonly Case[] Cases =
    [
        new("invalid-request-flags", Flags: 1, Result: 20, Error: 212),
        new("empty-registry-list", Output: "No resources currently locked\n"),
        new("ordered-resource-list", Seeds: [Library, Font, Catalog, new("Devs:example.device", 1)],
            Output: "TYPE     NAME\n\nLibrary  Libs:Alpha.library\nFont     Fonts:Topaz/8\nCatalog  Locale:example.catalog\nDevice   Devs:example.device\n"),
        new("present-empty-name-vector", Patterns: []),
        new("unlock-library", [new("Libs:#?", ["LIBS:ALPHA.LIBRARY"])], [Library], Unlock: 1, Visited: 1, Closed: 1, Nexts: 1),
        new("unlock-font", [new("Fonts:#?", [Font.Name])], [Font], Unlock: 1, Visited: 1, Closed: 1, Nexts: 1),
        new("unlock-catalog", [new("Locale:#?", [Catalog.Name])], [Catalog], Unlock: 1, Locale: true, Visited: 1, Closed: 1, Nexts: 1),
        new("unlock-missing-expanded-name", [new("Libs:#?", ["Libs:absent.library"])], Unlock: 1,
            Output: "'Libs:absent.library' is not a locked resource\n", Visited: 1, Nexts: 1),
        new("full-long-unlock-precedes-lock", [new("Libs:#?", [Library.Name])], [Library],
            Lock: uint.MaxValue, Unlock: 0x10000, Visited: 1, Closed: 1, Nexts: 1),
        new("matchfirst-error-stops-later-pattern", [new("missing#?", [], FirstError: 205), new("later", [])],
            Result: 20, Error: 205, Output: "'missing#?' - <fault:205>\n", Visited: 1),
        new("no-more-entries-continues-patterns", [new("empty", [], FirstError: 232), new("also-empty", [], FirstError: 232)], Visited: 2),
        new("matchnext-error-after-unlock", [new("Libs:#?", [Library.Name], NextError: 304), new("later", [])],
            [Library], Unlock: 1, Result: 20, Error: 304, Output: "'Libs:#?' - <fault:304>\n", Visited: 1, Closed: 1, Nexts: 1),
        new("multiple-patterns-reset-anchor", [new("Libs:#?", [Library.Name, "Libs:absent"]), new("Fonts:#?", [Font.Name])],
            [Library, Font], Unlock: 1, Output: "'Libs:absent' is not a locked resource\n", Visited: 2, Closed: 2, Nexts: 3),
        new("already-locked-skips-load", [new("Libs:#?", ["LIBS:ALPHA.LIBRARY"])], [Library],
            Output: "'LIBS:ALPHA.LIBRARY' is already a locked resource\n", Visited: 1, Nexts: 1),
        new("load-error-preserved-through-output-and-cleanup", [new("Libs:#?", ["Libs:bad.library"]), new("later", [])],
            Result: 20, Error: 202, Output: "Error while loading 'Libs:bad.library' - <fault:202>\n", Visited: 1, Loads: 1),
        new("action-232-continues-next-pattern", [new("Libs:#?", ["Libs:bad.library"], LoadError: 232), new("empty", [], FirstError: 232)],
            Output: "Error while loading 'Libs:bad.library' - ", Visited: 2, Loads: 1),
        new("catalog-fallback-without-locale", [new("Locale:#?", ["Locale:missing"], LoadError: 205)],
            Result: 20, Error: 205, Output: "<fault:205>\n", Visited: 1, Loads: 1),
        new("high-request-flag-word", Flags: 0x8000, Result: 20, Error: 212),
        new("localized-empty-list", Locale: true, Translate: true, Output: "Ei lukittuja resursseja\n",
            MessageOpens: 1, MessageCloses: 2, MessageQueries: 1),
        new("null-catalog-default-fallback", Seeds: [Library], Locale: true, NullCatalog: true,
            Output: "TYPE     NAME\n\nLibrary  Libs:Alpha.library\n", MessageOpens: 3, MessageQueries: 5),
        new("receive-invalid-request", Flags: 1, Receive: true, Result: 20, Error: 212),
        new("receive-empty-list", Receive: true, Output: "No resources currently locked\n"),
        new("borrow-null-directory-and-streams", Patterns: [], NullHandles: true),
        new("loadseg-failure-with-zero-ioerr", [new("Libs:#?", ["Libs:bad.library"], LoadError: 0)],
            Output: "Error while loading 'Libs:bad.library' - ", Visited: 1, Loads: 1, Nexts: 1),
        new("lock-high-word-only-does-not-retain", [new("Locale:#?", [Catalog.Name], LoadError: 205)],
            Lock: 0x10000, Locale: true, OpenCatalog: true, Visited: 1, Closed: 1, Loads: 1, Nexts: 1),
        new("lock-low-word-retains-catalog", [new("Locale:#?", [Catalog.Name], LoadError: 205)],
            Lock: 0xffff0001, Locale: true, OpenCatalog: true, Visited: 1, Created: 1, Loads: 1, Nexts: 1),
        new("listing-nested-catalog-closes-retains-stored-pointer", Seeds: [Library, Font], Locale: true,
            Output: "TYPE     NAME\n\nLibrary  Libs:Alpha.library\nFont     Fonts:Topaz/8\n",
            MessageOpens: 1, MessageCloses: 4, MessageQueries: 7),
        new("empty-list-retries-failed-outer-catalog-open", Locale: true, FailFirstMessageOpen: true,
            Output: "No resources currently locked\n", MessageOpens: 2, MessageCloses: 2, MessageQueries: 1),
        new("empty-list-null-catalog-retries-inner-open", Locale: true, NullCatalog: true,
            Output: "No resources currently locked\n", MessageOpens: 2, MessageQueries: 1),
        new("successive-diagnostics-reuse-stored-catalog-after-close",
            [new("Libs:#?", ["Libs:absent.one", "Libs:absent.two"])], Unlock: 1, Locale: true,
            Output: "'Libs:absent.one' is not a locked resource\n'Libs:absent.two' is not a locked resource\n",
            Visited: 1, Nexts: 2, MessageOpens: 1, MessageCloses: 2, MessageQueries: 2),
        new("resource-catalog-open-error-sampled-after-message-catalog-close",
            [new("Locale:#?", [Catalog.Name], LoadError: 205)], Locale: true, OpenCatalog: true,
            CatalogOpenFailure: true, Result: 20, Error: 779, Visited: 1, Loads: 1,
            Output: "'Locale:example.catalog' couldn't be loaded as a resource - <fault:779>\n",
            MessageOpens: 1, MessageCloses: 1, MessageQueries: 1)
    ];

    public static int Run(string[] args)
    {
        var path = Path.GetFullPath(args[2]);
        Require(!File.Exists(path) && !string.Equals(path, Path.GetFullPath(args[0]), StringComparison.OrdinalIgnoreCase),
            "Use a fresh runtime receipt path, different from the HUNK.");
        var observations = new List<object>();
        string? failure = null, hash = null;
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
            realKickstart = false, realScheduling = false, referenceCommandExecution = false,
            shippingOrPureApproval = false, crossInvocationCodeLifetimeProven = false,
            scope = "Actual compiled single-request worker dispatch/receive with supplied DOS/Exec/Utility/Locale/Graphics responses. Covers request offsets, borrowed context restoration, matcher ownership/error precedence, listing, UNLOCK ordering, duplicate detection, low-WORD LOCK retention, and source-shaped lazy message catalog calls with nested repeated closes, null-open retry and error sampling after CloseCatalog. Caller supplies library leases, cleared catalog state and seeded registry; host reclaims retained registry after assertions. Stored catalog pointer remains unchanged after CloseCatalog exactly as source instructions; guest pointer validity and reference-count effects are unproven. Does not prove process startup/exit, guest behavior, real handlers or image lifetime.",
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
        private const uint Return = 0x2000, ExecBase = 0x4000, UtilityBase = 0x8000,
            GraphicsBase = 0x9000, LocaleBase = 0xa000, DosBase = 0xb000,
            ServicePort = 0x18000, MessageCatalog = 0x23450, ResourceCatalog = 0x45670;
        private readonly CommandTestBus bus = new();
        private readonly Invocation owner;
        private readonly Case test;
        private readonly M68kCpuModel model;
        private readonly int codeLength;
        private readonly HashSet<uint> gateways = [];
        private readonly List<uint> storage = [];
        private readonly List<(uint Address, byte[] Bytes)> borrowedPayload = [];
        private readonly Dictionary<uint, Seed> records = [];
        private readonly HashSet<uint> closedNodes = [];
        private readonly List<string> events = [];
        private readonly StringBuilder output = new();
        private readonly uint control, message, list, translated, catalogState;
        private readonly byte[] requestSnapshot;
        private uint directory = 0x1101, input = 0x2202, stream = 0x3303, anchor;
        private int contexts, replies, firsts, nexts, ends, pathIndex, closes, created, loads, faults, queries,
            messageOpens, messageCloses, queriesAfterClose;
        private bool matchActive;
        private Pattern Active => test.Patterns![firsts - 1];

        public Fixture(HunkImage image, M68kCpuModel model, Case test)
        {
            this.model = model; this.test = test; codeLength = image.Code.Length;
            owner = new Invocation(new ProbeCase(test.Id, "", 0, 0, "") { StackBytes = 3000 }, 0);
            bus.Current = owner; bus.LoadAndProtect(Load, image.Code); bus.Long(4, ExecBase);
            bus.Long(ExecBase + (uint)ExecLayout.ExecBase.ThisTask, owner.Process);
            control = Storage(24); message = Storage(54); list = Storage(12);
            catalogState = Storage(4);
            translated = Text("Ei lukittuja resursseja\n");
            bus.Long(list, list + 4); bus.Long(list + 4, 0); bus.Long(list + 8, list);
            foreach (var seed in test.Seeds ?? [])
            {
                var node = bus.Allocate(owner, (uint)seed.Name.Length + 19, "Record", false);
                bus.Memory[node + 8] = seed.Type; bus.Long(node + 10, node + 18);
                bus.Long(node + 14, node + 0x10000); Put(node + 18, seed.Name);
                Append(node, seed);
            }
            bus.Word(message + 20, test.Flags); bus.Long(message + 22, 0xdeadbeef); bus.Long(message + 26, 0xcafef00d);
            bus.Long(message + 30, test.NullHandles ? 0u : 0x4404);
            bus.Long(message + 34, test.NullHandles ? 0u : 0x5505);
            bus.Long(message + 38, test.NullHandles ? 0u : 0x6606);
            if (test.Patterns is not null)
            {
                var names = Storage((uint)(test.Patterns.Length + 1) * 4);
                for (var index = 0; index < test.Patterns.Length; index++)
                {
                    var name = test.Patterns[index].Name;
                    var address = Text(name);
                    bus.Long(names + (uint)index * 4, address);
                    borrowedPayload.Add((address, bus.Memory.AsSpan((int)address, name.Length + 1).ToArray()));
                }
                borrowedPayload.Add((names, bus.Memory.AsSpan((int)names, (test.Patterns.Length + 1) * 4).ToArray()));
                bus.Long(message + 42, names);
            }
            bus.Long(message + 46, test.Lock); bus.Long(message + 50, test.Unlock);
            requestSnapshot = bus.Memory.AsSpan((int)message, 54).ToArray();
            bus.Long(control, message); bus.Long(control + 4, list);
            bus.Long(control + 8, catalogState); bus.Long(control + 12, DosBase);
            bus.Long(control + 16, test.Locale ? LocaleBase : 0); bus.Long(control + 20, ServicePort);
            RegisterVectors();
        }

        private uint Storage(uint bytes) { var address = bus.Allocate(owner, bytes, "Fixture", true); storage.Add(address); return address; }
        private uint Text(string value) { var address = Storage((uint)value.Length + 1); Put(address, value); return address; }
        private void Put(uint address, string value) => Encoding.Latin1.GetBytes(value + '\0').CopyTo(bus.Memory.AsSpan((int)address));

        private void Append(uint node, Seed seed)
        {
            var previous = bus.Long(list + 8);
            bus.Long(node, list + 4); bus.Long(node + 4, previous);
            bus.Long(previous, node); bus.Long(list + 8, node); records.Add(node, seed);
        }

        private void RegisterVectors()
        {
            Register(DosBase, DosLvo.CurrentDir, "CurrentDir", s => Switch(s, ref directory, 0, 3, 30, 0x1101));
            Register(DosBase, DosLvo.SelectInput, "SelectInput", s => Switch(s, ref input, 1, 4, 34, 0x2202));
            Register(DosBase, DosLvo.SelectOutput, "SelectOutput", s => Switch(s, ref stream, 2, 5, 38, 0x3303));
            Register(ExecBase, ExecLvo.WaitPort, "WaitPort", s => {
                Require(test.Receive && s.A[0] == ServicePort && events.Count == 1, "WaitPort ABI or order differs."); return message;
            });
            Register(ExecBase, ExecLvo.GetMsg, "GetMsg", s => {
                Require(test.Receive && s.A[0] == ServicePort && events.Count == 2, "GetMsg ABI or order differs."); return message;
            });
            Register(ExecBase, ExecLvo.ReplyMsg, "ReplyMsg", s => {
                Require(s.A[1] == message && ++replies == 1 && !matchActive && ends == firsts,
                    "Reply ownership or MatchEnd ordering differs.");
                Require(contexts == (test.Flags == 0 ? 6 : 0) && directory == 0x1101 && input == 0x2202 && stream == 0x3303,
                    "Reply occurred before restoration or invalid request borrowed context.");
                Require(bus.Long(message + 22) == (uint)test.Result && bus.Long(message + 26) == (uint)test.Error,
                    "Reply result/IoErr differs.");
                var actual = bus.Memory.AsSpan((int)message, 54).ToArray();
                requestSnapshot.AsSpan(22, 8).CopyTo(actual.AsSpan(22, 8));
                Require(actual.SequenceEqual(requestSnapshot), "Worker changed sender-owned fields outside result/IoErr.");
                foreach (var (address, bytes) in borrowedPayload)
                    Require(bus.Memory.AsSpan((int)address, bytes.Length).SequenceEqual(bytes),
                        "Worker changed sender-owned NAME/M pointer array or strings.");
                // Sender may immediately reclaim the request. Any later native read/write must fail.
                bus.Release(owner, message, "Fixture"); storage.Remove(message); return 0;
            });
            Register(DosBase, DosLvo.MatchFirst, "MatchFirst", s => {
                AssertBorrowed(); Require(!matchActive && firsts < test.Patterns!.Length, "Overlapping or excess pattern.");
                var plan = test.Patterns![firsts]; Require(bus.CString(s.D[1]) == plan.Name, "Original pattern changed.");
                anchor = s.D[2]; Require(anchor >= owner.StackTop - owner.StackBytes && anchor + 538 <= owner.StackTop,
                    "Anchor not on the worker stack.");
                var expected = new byte[538]; expected[10] = 0x10; expected[16] = 1; expected[18] = 1;
                Require(bus.Memory.AsSpan((int)anchor, 538).SequenceEqual(expected), "Anchor clear/flags/break/length differs.");
                firsts++; pathIndex = 0; matchActive = true;
                if (plan.FirstError == 0) { Require(plan.Paths.Length > 0, "Fixture missing first path."); Put(anchor + 280, plan.Paths[0]); }
                owner.IoError = plan.FirstError; return unchecked((uint)plan.FirstError);
            });
            Register(DosBase, DosLvo.MatchNext, "MatchNext", s => {
                AssertBorrowed(); Require(matchActive && s.D[1] == anchor, "MatchNext anchor/lifetime differs."); nexts++;
                if (++pathIndex < Active.Paths.Length) { Put(anchor + 280, Active.Paths[pathIndex]); return 0; }
                owner.IoError = Active.NextError; return unchecked((uint)Active.NextError);
            });
            Register(DosBase, DosLvo.MatchEnd, "MatchEnd", s => {
                AssertBorrowed(); Require(matchActive && s.D[1] == anchor, "MatchEnd missing ownership.");
                matchActive = false; ends++; bus.Memory.AsSpan((int)anchor, 538).Fill(0x9b); owner.IoError = 901; return 0;
            });
            Register(DosBase, DosLvo.LoadSeg, "LoadSeg", s => {
                AssertBorrowed(); Require(matchActive && bus.CString(s.D[1]) == Active.Paths[pathIndex] && test.Unlock == 0,
                    "LoadSeg must use expanded name and not run for UNLOCK.");
                loads++; owner.IoError = Active.LoadError; return 0;
            });
            Register(DosBase, DosLvo.IoErr, "IoErr", _ => unchecked((uint)owner.IoError));
            Register(DosBase, DosLvo.VPrintf, "VPrintf", s => {
                AssertBorrowed(); var format = bus.CString(s.D[1]);
                Require(test.Patterns is null || matchActive, "Pattern diagnostic printed after MatchEnd.");
                var text = format == "%-9s%s\n" ? bus.CString(bus.Long(s.D[2])).PadRight(9) + bus.CString(bus.Long(s.D[2] + 4)) + "\n" :
                    format.Contains("%s", StringComparison.Ordinal) ? format.Replace("%s", bus.CString(bus.Long(s.D[2])), StringComparison.Ordinal) : format;
                output.Append(text); owner.IoError = 777; return (uint)text.Length;
            });
            Register(DosBase, DosLvo.PrintFault, "PrintFault", s => {
                AssertBorrowed(); Require(s.D[1] == test.Error && s.D[2] == 0 && test.Error != 0,
                    "PrintFault must use stored error with null header.");
                Require(!matchActive && ends == firsts, "PrintFault occurred before MatchEnd.");
                faults++; output.Append($"<fault:{s.D[1]}>\n"); owner.IoError = 888; return 1;
            });
            Register(UtilityBase, UtilityLvo.Stricmp, "Stricmp", s => {
                Require(records.Keys.Any(node => node + 18 == s.A[1]), "Lookup used an unowned record name.");
                return unchecked((uint)string.Compare(bus.CString(s.A[0]), bus.CString(s.A[1]), StringComparison.OrdinalIgnoreCase));
            });
            Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", s => CloseRecord(s.A[1], 0));
            Register(GraphicsBase, -78, "CloseFont", s => CloseRecord(s.A[1], 2));
            Register(LocaleBase, -36, "CloseCatalog", s => {
                if (s.A[0] == MessageCatalog)
                {
                    Require(test.Locale && messageOpens > 0 && bus.Long(catalogState) == MessageCatalog,
                        "Message catalog close must use its unchanged stored pointer.");
                    messageCloses++; owner.IoError = 779; return 0;
                }
                if (test.OpenCatalog && s.A[0] == ResourceCatalog) { Require(test.Created == 0 && created == 0, "Closed a retained catalog."); closes++; return 0; }
                return CloseRecord(s.A[0], 3);
            });
            Register(ExecBase, ExecLvo.Remove, "Remove", s => {
                Require(records.ContainsKey(s.A[1]) && closedNodes.Contains(s.A[1]), "Unlinked before close.");
                var next = bus.Long(s.A[1]); var previous = bus.Long(s.A[1] + 4);
                Require(bus.Long(previous) == s.A[1] && bus.Long(next + 4) == s.A[1], "Broken registry links.");
                bus.Long(previous, next); bus.Long(next + 4, previous); return 0;
            });
            Register(ExecBase, ExecLvo.FreeVec, "FreeVec", s => {
                Require(records.Remove(s.A[1]) && closedNodes.Remove(s.A[1]) && bus.Long(bus.Long(s.A[1] + 4)) != s.A[1], "Freed before close/remove.");
                bus.Release(owner, s.A[1], "Record"); owner.IoError = 902; return 0;
            });
            Register(LocaleBase, -72, "GetCatalogStr", s => {
                Require(test.Locale && s.A[0] == bus.Long(catalogState), "Message catalog lookup ABI differs."); queries++;
                if (messageCloses > 0) queriesAfterClose++;
                var expected = s.D[0] switch {
                    0xc350 => "Library", 0xc351 => "Device", 0xc352 => "Font", 0xc353 => "Catalog",
                    0xc354 => "TYPE", 0xc355 => "%-9s%s\n", 0xc356 => "NAME\n",
                    0xc357 => "No resources currently locked\n", 0xc358 => "'%s' is already a locked resource\n",
                    0xc359 => "Error while loading '%s' - ", 0xc35a => "Requires diskfont.library V37 - ",
                    0xc35b => "'%s' couldn't be loaded as a resource - ", 0xc35c => "'%s' is not a locked resource\n",
                    0xc35d => "'%s' - ", _ => throw new InvalidOperationException("Uncaptured catalog ID.")
                };
                Require(bus.CString(s.A[1]) == expected, "Catalog ID/default pair differs from captured table.");
                if (test.Translate) { Require(s.D[0] == 0xc357 && bus.CString(s.A[1]) == "No resources currently locked\n", "Wrong translated id/default."); return translated; }
                return s.A[1];
            });
            Register(LocaleBase, -150, "OpenCatalogA", s => {
                if (bus.CString(s.A[1]) == "sys/c.catalog")
                {
                    Require(test.Locale && s.A[0] == 0 && s.A[2] == 0 && bus.Long(catalogState) == 0,
                        "Lazy message catalog open name/ABI/tags differs.");
                    messageOpens++; owner.IoError = 776;
                    return test.NullCatalog || test.FailFirstMessageOpen && messageOpens == 1 ? 0 : MessageCatalog;
                }
                Require(test.OpenCatalog && s.A[0] == 0 && bus.CString(s.A[1]) == Catalog.Name &&
                    bus.Long(s.A[2]) == 0x80090001 && bus.Long(s.A[2] + 4) == 0 && bus.Long(s.A[2] + 8) == 0,
                    "Resource catalog open ABI/tags differs.");
                if (test.CatalogOpenFailure) { owner.IoError = 775; return 0; }
                return ResourceCatalog;
            });
            Register(ExecBase, ExecLvo.AllocVec, "AllocVec", s => {
                Require(test.Created == 1 && s.D[0] == Catalog.Name.Length + 19 && s.D[1] == 0, "Unexpected record allocation.");
                return bus.Allocate(owner, s.D[0], "Record", false);
            });
            Register(ExecBase, ExecLvo.AddTail, "AddTail", s => {
                Require(test.Created == 1 && s.A[0] == list && bus.Memory[s.A[1] + 8] == 3 &&
                    bus.Long(s.A[1] + 10) == s.A[1] + 18 && bus.CString(s.A[1] + 18) == Catalog.Name &&
                    bus.Long(s.A[1] + 14) == ResourceCatalog, "Retained catalog record differs.");
                Append(s.A[1], Catalog); created++; return 0;
            });
        }

        private uint Switch(M68kCpuState s, ref uint current, int acquire, int restore, uint offset, uint original)
        {
            Require(contexts == acquire || contexts == restore, "Borrow/restore context order differs.");
            var restoring = contexts == restore;
            Require(s.D[1] == (restoring ? original : bus.Long(message + offset)), "Wrong borrowed/restored handle.");
            contexts++; var old = current; current = s.D[1]; owner.IoError = 903; return old;
        }

        private void AssertBorrowed() => Require(contexts == 3 && directory == (test.NullHandles ? 0u : 0x4404) &&
            input == (test.NullHandles ? 0u : 0x5505) && stream == (test.NullHandles ? 0u : 0x6606),
            "Operation used worker context instead of caller context.");

        private uint CloseRecord(uint handle, byte type)
        {
            AssertBorrowed(); var node = records.Keys.Single(n => bus.Long(n + 14) == handle);
            Require(records[node].Type == type && closedNodes.Add(node) && bus.Long(bus.Long(node + 4)) == node,
                "Resource close type/order differs."); closes++; owner.IoError = 904; return 0;
        }

        public object Execute()
        {
            bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), (int)owner.StackBytes + 32).Fill(0xb6);
            using var cpu = M68kCoreFactory.Default.Create(model, bus);
            cpu.State.StatusRegister = 0; cpu.BeginSubroutine(Load, owner.StackTop, Return);
            for (var i = 0; i < 8; i++) cpu.State.D[i] = (uint)(0xde000000 + i * 16);
            for (var i = 0; i < 7; i++) cpu.State.A[i] = (uint)(0xae000000 + i * 16);
            cpu.State.D[0] = test.Receive ? 1u : 0u; cpu.State.A[0] = control;
            try
            {
                while (cpu.State.ProgramCounter != Return)
                {
                    var pc = cpu.State.ProgramCounter;
                    Require(pc >= Load && pc < Load + codeLength || gateways.Contains(pc), $"Unexpected PC ${pc:X8}.");
                    Require(++owner.Instructions < 150_000 && !cpu.State.Halted && !cpu.State.Stopped, "Execution did not return.");
                    cpu.ExecuteInstruction();
                }
                Require(cpu.State.D[0] == 0 && cpu.State.A[7] == owner.StackTop &&
                    bus.Memory.AsSpan((int)owner.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0 &&
                    bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), 16).IndexOfAnyExcept((byte)0xb6) < 0,
                    "Native result, stack balance or stack guard differs.");
                Require(replies == 1 && firsts == test.Visited && ends == firsts && nexts == test.Nexts && !matchActive && loads == test.Loads,
                    "Reply, matching or load count differs.");
                Require(faults == (test.Flags == 0 && test.Error != 0 ? 1 : 0) && closes == test.Closed && created == test.Created,
                    "Diagnostic or resource count differs.");
                Require(output.ToString() == test.Output, $"Output differs: {JsonSerializer.Serialize(output.ToString())}.");
                Require(closedNodes.Count == 0 && records.Count == (test.Seeds?.Length ?? 0) - (test.OpenCatalog ? 0 : test.Closed) + test.Created,
                    "Retained registry ownership differs.");
                Require(!test.Locale || test.Patterns is not null || queries > 0, "Locale path not exercised.");
                Require(messageOpens == test.MessageOpens && messageCloses == test.MessageCloses &&
                    queries == test.MessageQueries && bus.Long(catalogState) == (test.MessageCloses > 0 ? MessageCatalog : 0),
                    "Lazy message catalog counts or uncleared slot differs.");
                Require(test.Id != "listing-nested-catalog-closes-retains-stored-pointer" || queriesAfterClose == 4,
                    "Listing did not exercise GetCatalogStr with a previously closed stored pointer.");
                Require(test.Id != "successive-diagnostics-reuse-stored-catalog-after-close" || queriesAfterClose == 1,
                    "Successive diagnostics did not reuse the previously closed stored pointer.");
                var retained = records.Values.Select(seed => new { seed.Name, seed.Type }).ToArray();
                // Process coordinator owns retained records. Reclaim fixture memory only after validating that ownership.
                foreach (var node in records.Keys) bus.Release(owner, node, "Record");
                foreach (var address in storage) bus.Release(owner, address, "Fixture");
                bus.AssertReleased(owner); bus.AssertImageUnchanged();
                return new { id = test.Id, result = test.Result, ioErr = test.Error, owner.Instructions,
                    stackBytes = owner.StackBytes, stackBytesWritten = owner.StackTop - owner.LowestStackWrite,
                    firsts, nexts, ends, replies, closes, created, loads, faults, queries,
                    messageOpens, messageCloses, queriesAfterClose,
                    output = output.ToString(), retained, sharedImageWrites = 0, events };
            }
            catch (Exception error) { throw new InvalidOperationException($"{test.Id} PC={cpu.State.ProgramCounter:X8}; events={string.Join(',', events)}", error); }
        }

        private void Register(uint library, short offset, string name, Func<M68kCpuState, uint> handler)
        {
            var address = checked((uint)(library + offset)); gateways.Add(address);
            bus.RegisterGateway(address, s => {
                Require(replies == 0 && s.A[6] == library, $"{name}: called after reply or wrong library base.");
                events.Add(name); var result = handler(s);
                s.D[0] = result; s.D[1] = 0xd1d1d1d1; s.A[0] = 0xa0a0a0a0; s.A[1] = 0xa1a1a1a1;
                s.StatusRegister = (ushort)((s.StatusRegister & 0xffe0) | 0x001f);
            });
        }
    }
}
