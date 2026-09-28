using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>Native resource actions with supplied OS vectors and explicit ownership checks.</summary>
internal static class LoadResourceActionsRuntimeSuite
{
    public const string Suite = "loadresource-wb31-actions-runtime";
    private sealed record Case(string Id, string Kind, string Name = "LIBS:test.library",
        bool Keep = true, int LoadError = 0, bool MissingLocale = false,
        string? FailAllocation = null, bool OpenFailure = false, bool DiskfontMissing = false,
        int ExpectedError = 0, uint MessageId = 0, int RegistryType = -1,
        bool CacheRetained = false, bool Consume = false, bool Duplicate = false,
        bool Localized = false, bool SecondSegment = false, bool NullCatalog = false);

    private static readonly Case[] Cases = [
        new("library-lock", "library", RegistryType: 0),
        new("library-open-close", "library", Keep: false),
        new("library-open-failure-samples-error-after-diagnostic", "library", OpenFailure: true,
            ExpectedError: 555, MessageId: 0xc359),
        new("library-record-allocation-failure-closes-with-success", "library", FailAllocation: "Registry"),
        new("cache-allocation-failure-unloads", "library", FailAllocation: "Cache", ExpectedError: 103),
        new("cache-lock-failure-unloads", "lock-failure", ExpectedError: 103),
        new("load-failure-preserves-error-before-diagnostic", "load-failure", LoadError: 202,
            ExpectedError: 202, MessageId: 0xc359),
        new("catalog-fallback-121-lock", "catalog", "LOCALE:test.catalog", LoadError: 121, RegistryType: 3),
        new("catalog-fallback-205-open-close", "catalog", "LOCALE:test.catalog", Keep: false, LoadError: 205),
        new("catalog-fallback-212-lock", "catalog", "LOCALE:test.catalog", LoadError: 212, RegistryType: 3),
        new("catalog-missing-locale-keeps-loader-error", "catalog", LoadError: 121, MissingLocale: true, ExpectedError: 121),
        new("catalog-open-failure-samples-error-after-diagnostic", "catalog", LoadError: 205, OpenFailure: true,
            ExpectedError: 555, MessageId: 0xc35b),
        new("catalog-record-allocation-failure-closes-with-success", "catalog", LoadError: 212, FailAllocation: "Registry"),
        new("device-lock-only-caches", "device", CacheRetained: true),
        new("device-no-lock-still-caches", "device", Keep: false, CacheRetained: true),
        new("device-cache-consumed-by-actual-hook", "device", CacheRetained: true, Consume: true),
        new("font-lock", "font", "FONTS:topaz/8", RegistryType: 2),
        new("font-open-close", "font", "FONTS:topaz/8", Keep: false),
        new("font-name-allocation-failure", "font", "FONTS:topaz/8", FailAllocation: "FontName", ExpectedError: 103),
        new("font-diskfont-library-failure", "font", "FONTS:topaz/8", DiskfontMissing: true,
            ExpectedError: 555, MessageId: 0xc35a),
        new("font-open-failure-reports-derived-name", "font", "FONTS:topaz/8", OpenFailure: true,
            ExpectedError: 555, MessageId: 0xc359),
        new("font-record-allocation-failure-closes-with-success", "font", "FONTS:topaz/8", FailAllocation: "Registry"),
        new("font-colon-final-separator", "font", "FONTS:12", RegistryType: 2),
        new("font-no-separator-replaces-index-zero", "font", "16", RegistryType: 2),
        new("resident-unsupported-type", "unsupported", ExpectedError: 212, MessageId: 0xc35b),
        new("segment-without-resource", "unrecognized", ExpectedError: 212, MessageId: 0xc35b),
        new("false-resident-matchword-stalls-segment-scan", "false-resident", ExpectedError: 212, MessageId: 0xc35b),
        new("resident-in-second-segment", "library", SecondSegment: true, RegistryType: 0),
        new("duplicate-registry-name-skips-load", "library", Duplicate: true, RegistryType: 0, MessageId: 0xc358),
        new("localized-diagnostic", "load-failure", LoadError: 202, ExpectedError: 202, MessageId: 0xc359, Localized: true),
        new("null-message-catalog-still-queries-locale", "load-failure", LoadError: 202, ExpectedError: 202, MessageId: 0xc359, NullCatalog: true),
        new("diagnostic-without-locale", "load-failure", LoadError: 202, ExpectedError: 202, MessageId: 0xc359, MissingLocale: true),
        new("failed-message-catalog-preserves-printf-error", "library", OpenFailure: true,
            ExpectedError: 333, MessageId: 0xc359, NullCatalog: true),
        new("library-failure-without-locale-samples-printf-error", "library", OpenFailure: true,
            ExpectedError: 333, MessageId: 0xc359, MissingLocale: true)
    ];

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
            foreach (var test in Cases)
                observations.Add(new Fixture(image, model, test).Execute());
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
            scope = "Generated HUNK resource actions, real native registry/cache/hook, source-shaped lazy message catalog calls, and supplied Exec/DOS/Utility/Graphics/Locale/Diskfont vectors. DOS LoadSeg follows the installed native hook before delegating to the supplied saved vector. Checks classification, ownership, allocation failures, diagnostic text, error sampling after CloseCatalog, failed message open and missing Locale. Stored catalog pointer is deliberately retained after close, matching source instructions; its guest validity is unproven. Library/font open vectors do not recursively invoke DOS LoadSeg; device consumption invokes the actual native hook. Original guest behavior, full process integration, malformed font suffix behavior and code lifetime remain open.",
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
        private const uint ExecBase = 0x4000, DosBase = 0x8000, UtilityBase = 0x9000,
            GraphicsBase = 0xa000, LocaleBase = 0xb000, DiskfontBase = 0xc000,
            Return = 0x2000, SavedVector = 0x2400, Resource = 0x18000, MessageCatalog = 0x1c000;
        private readonly CommandTestBus bus = new();
        private readonly Invocation owner;
        private readonly M68kCpuModel model;
        private readonly Case test;
        private readonly int codeLength;
        private readonly uint task, port, context, translated, catalogState;
        private readonly HashSet<uint> gateways = [];
        private readonly Dictionary<uint, string> allocations = [];
        private readonly HashSet<uint> linked = [];
        private readonly List<uint> segments = [];
        private readonly HashSet<uint> locks = [];
        private readonly List<string> events = [];
        private uint state, vector = SavedVector, segment, fontName;
        private int forbidDepth, semaphoreDepth, ioError = 619, loadCalls, unloadCalls,
            resourceOpens, resourceCloses, diskfontOpens, diskfontCloses, registryAdds,
            cacheAdds, messageCalls, catalogQueries, messageOpens, messageCloses, allocFailures, sameLockCalls;
        private bool resourceLive, diskfontLive, dosLive;
        private readonly StringBuilder output = new();

        public Fixture(HunkImage image, M68kCpuModel model, Case test)
        {
            this.model = model; this.test = test; codeLength = image.Code.Length;
            owner = new Invocation(new ProbeCase(test.Id, "", 0, 0, ""), 0);
            bus.Current = owner; bus.LoadAndProtect(Load, image.Code); bus.Long(4, ExecBase);
            task = bus.Allocate(owner, 0x400, "FixtureTask", true);
            port = bus.Allocate(owner, 34, "FixturePort", true);
            context = bus.Allocate(owner, 512, "FixtureContext", true);
            catalogState = bus.Allocate(owner, 4, "FixtureCatalogState", true);
            translated = bus.Allocate(owner, 64, "FixtureTranslation", true);
            Encoding.Latin1.GetBytes("Translated '%s'\n\0").CopyTo(bus.Memory.AsSpan((int)translated));
            Encoding.Latin1.GetBytes(test.Name + "\0").CopyTo(bus.Memory.AsSpan((int)context + 64));
            bus.Long(context + 16, test.Keep ? 1u : 0u);
            bus.Long(context + 20, catalogState);
            bus.Long(context + 36, test.Consume ? 1u : 0u);
            bus.Long(context + 44, test.Duplicate ? 1u : 0u);
            bus.Long(task + (uint)ExecLayout.Task.UserData, 0x12345678);
            bus.Long(port + (uint)ExecLayout.MsgPort.SignalTask, task);
            bus.Long(ExecBase + (uint)ExecLayout.ExecBase.ThisTask, task);
            resourceLive = test.Duplicate;
            RegisterExec(); RegisterDos(); RegisterResourceLibraries();
            var loadVector = unchecked(DosBase + (uint)DosLvo.LoadSeg);
            gateways.Add(loadVector);
            bus.Word(loadVector, 0x4ef9); bus.Long(loadVector + 2, SavedVector);
        }

        private void RegisterExec()
        {
            Register(ExecBase, ExecLvo.FindTask, "FindTask", s => {
                Require(s.A[1] == 0, "FindTask current process ABI."); return task;
            });
            Register(ExecBase, ExecLvo.FindPort, "FindPort", s => {
                Require(forbidDepth > 0 && bus.CString(s.A[1]) == "\u00ab LoadResource \u00bb", "Worker lookup ABI."); return port;
            });
            Register(ExecBase, ExecLvo.Forbid, "Forbid", _ => { forbidDepth++; return 0; });
            Register(ExecBase, ExecLvo.Permit, "Permit", _ => {
                Require(forbidDepth > 0, "Permit balance."); forbidDepth--; return 0;
            });
            Register(ExecBase, ExecLvo.OpenLibrary, "OpenLibrary", s => {
                var name = bus.CString(s.A[1]); events.Add("open:" + name);
                if (name == "dos.library")
                { Require(s.D[0] == 39 && !dosLive, "DOS retained lease."); dosLive = true; return DosBase; }
                if (name == "diskfont.library")
                {
                    Require(test.Kind == "font" && s.D[0] == 37 && !diskfontLive,
                        "Diskfont open version or ownership.");
                    AssertFontMutation(); diskfontOpens++;
                    if (test.DiskfontMissing) { ioError = 311; return 0; }
                    diskfontLive = true; return DiskfontBase;
                }
                Require(name == test.Name && s.D[0] == 0 && test.Kind == "library" && !resourceLive,
                    "Resource library open ABI or classification.");
                AssertCached(); resourceOpens++;
                if (test.OpenFailure) { ioError = 311; return 0; }
                resourceLive = true; return Resource;
            });
            Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", s => {
                if (s.A[1] == DosBase)
                {
                    Require(dosLive && !resourceLive && !diskfontLive && allocations.Count == 0,
                        "DOS released before owned resources."); dosLive = false; return 0;
                }
                if (s.A[1] == DiskfontBase)
                { Require(diskfontLive, "Diskfont closed twice."); diskfontLive = false; diskfontCloses++; ioError = 444; return 0; }
                return CloseResource(s.A[1], 0);
            });
            Register(ExecBase, ExecLvo.AllocVec, "AllocVec", s => {
                var kind = state == 0 ? "State" : s.D[0] == 16 ? "Cache" :
                    s.D[0] == test.Name.Length + 19 ? "Registry" :
                    test.Kind == "font" && s.D[0] == test.Name.Length + 5 ? "FontName" : "Unexpected";
                Require(kind != "Unexpected" && s.D[1] == (kind == "State" ? 0x10001u : 0u) &&
                    (kind != "State" || s.D[0] == 84), "Allocation size/flags differs.");
                events.Add("allocate:" + kind);
                if (kind == test.FailAllocation) { allocFailures++; return 0; }
                var address = bus.Allocate(owner, s.D[0], kind, kind == "State");
                allocations.Add(address, kind);
                if (kind == "State") state = address;
                if (kind == "FontName") fontName = address;
                return address;
            });
            Register(ExecBase, ExecLvo.FreeVec, "FreeVec", s => {
                Require(semaphoreDepth == 0 && !linked.Contains(s.A[1]), "Freed linked allocation or held semaphore.");
                var kind = allocations[s.A[1]];
                if (kind == "State") Require(vector == SavedVector &&
                    bus.Long(task + (uint)ExecLayout.Task.UserData) == 0x12345678, "Hook state freed before unlink.");
                bus.Release(owner, s.A[1], kind); allocations.Remove(s.A[1]); ioError = 444; return 0;
            });
            Register(ExecBase, ExecLvo.InitSemaphore, "InitSemaphore", s => {
                Require(s.A[0] == state + 14, "Semaphore location."); return 0;
            });
            Register(ExecBase, ExecLvo.ObtainSemaphore, "ObtainSemaphore", s => {
                Require(s.A[0] == state + 14 && semaphoreDepth == 0, "Semaphore obtain."); semaphoreDepth++; return 0;
            });
            Register(ExecBase, ExecLvo.AttemptSemaphore, "AttemptSemaphore", s => {
                Require(s.A[0] == state + 14 && semaphoreDepth == 0 && forbidDepth == 0,
                    "Semaphore attempt."); semaphoreDepth++; return 1;
            });
            Register(ExecBase, ExecLvo.ReleaseSemaphore, "ReleaseSemaphore", s => {
                Require(s.A[0] == state + 14 && semaphoreDepth == 1, "Semaphore release."); semaphoreDepth--; return 0;
            });
            Register(ExecBase, ExecLvo.AddTail, "AddTail", s => {
                var list = s.A[0]; var node = s.A[1];
                Require(!linked.Contains(node), "Duplicate record insertion.");
                if (list == state)
                {
                    Require(allocations[node] == "Cache" && semaphoreDepth == 1 &&
                        locks.Contains(bus.Long(node + 8)) && bus.Long(node + 12) == segment,
                        "Cache ownership or record shape."); cacheAdds++;
                }
                else
                {
                    Require(list == context && allocations[node] == "Registry" && semaphoreDepth == 0 &&
                        test.RegistryType >= 0 && bus.Memory[node + 8] == test.RegistryType &&
                        bus.Long(node + 10) == node + 18 && bus.CString(node + 18) == test.Name &&
                        bus.Long(node + 14) == Resource && resourceLive,
                        "Opened-resource registry ownership/type/name differs."); registryAdds++;
                }
                var previous = bus.Long(list + 8); var tail = list + 4;
                bus.Long(node, tail); bus.Long(node + 4, previous); bus.Long(previous, node); bus.Long(list + 8, node);
                linked.Add(node); return 0;
            });
            Register(ExecBase, ExecLvo.Remove, "Remove", s => {
                var node = s.A[1]; var kind = allocations[node];
                Require(linked.Remove(node) && (kind == "Cache" ? semaphoreDepth == 1 :
                    kind == "Registry" && semaphoreDepth == 0 && !resourceLive), "Record unlink ownership/order.");
                var next = bus.Long(node); var previous = bus.Long(node + 4);
                bus.Long(previous, next); bus.Long(next + 4, previous); return 0;
            });
            Register(ExecBase, ExecLvo.SetFunction, "SetFunction", s => {
                Require(s.A[1] == DosBase && unchecked((int)s.A[0]) == -150 && forbidDepth > 0,
                    "LoadSeg vector install/restore ABI.");
                var previous = vector; vector = s.D[0];
                bus.Long(unchecked(DosBase + (uint)DosLvo.LoadSeg) + 2, vector);
                return previous;
            });
        }

        private void RegisterDos()
        {
            Register(DosBase, DosLvo.LoadSeg, "LoadSeg", s => {
                Require(bus.CString(s.D[1]) == test.Name && !test.Duplicate && loadCalls++ == 0,
                    "LoadSeg name/call count.");
                if (test.LoadError != 0) { ioError = test.LoadError; return 0; }
                var first = AllocateSegment(); segment = (first + 4) >> 2;
                bus.Long(context + 32, segment);
                var target = first;
                if (test.SecondSegment)
                { target = AllocateSegment(); bus.Long(first + 4, (target + 4) >> 2); }
                if (test.Kind is "library" or "device" or "unsupported" or "lock-failure")
                    Resident(target + 36, test.Kind == "device" ? (byte)3 : test.Kind == "unsupported" ? (byte)5 : (byte)9);
                if (test.Kind == "false-resident")
                { bus.Word(first + 12, 0x4afc); bus.Long(first + 14, 0xdeadbeef); Resident(first + 52, 9); }
                if (test.Kind == "font") bus.Memory[first + 20] = 12;
                return segment;
            }, SavedVector);
            Register(DosBase, DosLvo.UnLoadSeg, "UnLoadSeg", s => {
                Require(s.D[1] == segment && segments.Count > 0 && semaphoreDepth == 0,
                    "Unknown/double SegList unload or held semaphore.");
                unloadCalls++; foreach (var address in segments) bus.Release(owner, address, "Segment");
                segments.Clear(); ioError = 444; return 0;
            });
            Register(DosBase, DosLvo.Lock, "Lock", s => {
                Require(bus.CString(s.D[1]) == test.Name && unchecked((int)s.D[2]) == -2 && semaphoreDepth == 0,
                    "Cache Lock ABI or semaphore ordering.");
                if (test.Kind == "lock-failure") { ioError = 205; return 0; }
                var value = 0x700u + (uint)locks.Count; Require(locks.Add(value), "Repeated lock handle."); return value;
            });
            Register(DosBase, DosLvo.UnLock, "UnLock", s => {
                Require(locks.Remove(s.D[1]) && semaphoreDepth == 0, "Lock release ownership."); ioError = 444; return 0;
            });
            Register(DosBase, DosLvo.SameLock, "SameLock", s => {
                Require(test.Consume && locks.Contains(s.D[1]) && locks.Contains(s.D[2]) && semaphoreDepth == 1,
                    "One-shot comparison ownership."); sameLockCalls++; return 0;
            });
            Register(DosBase, DosLvo.IoErr, "IoErr", _ => unchecked((uint)ioError));
            Register(DosBase, DosLvo.FilePart, "FilePart", s => {
                Require(test.Kind == "font" && bus.CString(s.D[1]) == test.Name, "Font FilePart input.");
                return s.D[1] + (uint)(Math.Max(test.Name.LastIndexOf('/'), test.Name.LastIndexOf(':')) + 1);
            });
            Register(DosBase, DosLvo.StrToLong, "StrToLong", s => {
                var suffix = test.Name[(Math.Max(test.Name.LastIndexOf('/'), test.Name.LastIndexOf(':')) + 1)..];
                Require(bus.CString(s.D[1]) == suffix && s.D[2] >= owner.StackTop - owner.StackBytes &&
                    s.D[2] + 12 <= owner.StackTop, "StrToLong suffix or local output storage.");
                bus.Long(s.D[2], uint.Parse(suffix)); return (uint)suffix.Length;
            });
            Register(DosBase, DosLvo.VPrintf, "VPrintf", s => {
                Require(test.MessageId != 0 && messageCalls++ == 0 && s.D[2] >= owner.StackTop - owner.StackBytes &&
                    s.D[2] + 4 <= owner.StackTop, "Unexpected diagnostic or nonlocal argument array.");
                var format = bus.CString(s.D[1]); var name = bus.CString(bus.Long(s.D[2]));
                var expectedName = test.MessageId == 0xc35a ? "" : test.Kind == "font" && test.OpenFailure ? FontFilename() : test.Name;
                Require(name == expectedName && format == (test.Localized ? "Translated '%s'\n" : DefaultMessage(test.MessageId)),
                    "Diagnostic format or supplied name differs.");
                output.Append(format.Replace("%s", name, StringComparison.Ordinal));
                ioError = 333; return (uint)output.Length;
            });
        }

        private void RegisterResourceLibraries()
        {
            Register(UtilityBase, UtilityLvo.Stricmp, "Stricmp", s => {
                Require(test.Duplicate && bus.CString(s.A[0]) == test.Name && bus.CString(s.A[1]) == test.Name,
                    "Unexpected registry comparison."); return 0;
            });
            Register(LocaleBase, -150, "OpenCatalogA", s => {
                if (bus.CString(s.A[1]) == "sys/c.catalog")
                {
                    Require(!test.MissingLocale && test.MessageId != 0 && messageOpens++ == 0 &&
                        bus.Long(catalogState) == 0 && s.A[0] == 0 && s.A[2] == 0,
                        "Lazy message catalog open ABI/name/tags differs.");
                    ioError = 111;
                    return test.NullCatalog ? 0 : MessageCatalog;
                }
                Require(test.Kind == "catalog" && !test.MissingLocale && s.A[0] == 0 &&
                    bus.CString(s.A[1]) == test.Name && bus.Long(s.A[2]) == 0x80090001 &&
                    bus.Long(s.A[2] + 4) == 0 && bus.Long(s.A[2] + 8) == 0,
                    "Catalog fallback tags/ABI differs.");
                resourceOpens++; if (test.OpenFailure) { ioError = 311; return 0; }
                resourceLive = true; return Resource;
            });
            Register(LocaleBase, -36, "CloseCatalog", s => {
                if (s.A[0] == MessageCatalog)
                {
                    Require(messageOpens == 1 && messageCalls == 1 && messageCloses++ == 0 &&
                        bus.Long(catalogState) == MessageCatalog && !test.NullCatalog,
                        "Message catalog close must follow VPrintf with the stored pointer.");
                    ioError = 555; return 0;
                }
                return CloseResource(s.A[0], 3);
            });
            Register(LocaleBase, -72, "GetCatalogStr", s => {
                Require(!test.MissingLocale && s.A[0] == (test.NullCatalog ? 0 : MessageCatalog) &&
                    s.D[0] == test.MessageId && bus.CString(s.A[1]) == DefaultMessage(test.MessageId),
                    "Catalog diagnostic id/default/lease differs.");
                catalogQueries++; ioError = 222; return test.Localized ? translated : s.A[1];
            });
            Register(GraphicsBase, -78, "CloseFont", s => CloseResource(s.A[1], 2));
            Register(DiskfontBase, -30, "OpenDiskFont", s => {
                Require(diskfontLive && test.Kind == "font" &&
                    bus.CString(bus.Long(s.A[0])) == FontFilename() && bus.Long(s.A[0]) == fontName &&
                    bus.Word(s.A[0] + 4) == ushort.Parse(test.Name[(Math.Max(test.Name.LastIndexOf('/'), test.Name.LastIndexOf(':')) + 1)..]) &&
                    bus.Memory[s.A[0] + 6] == 0 && bus.Memory[s.A[0] + 7] == 2,
                    "Diskfont TextAttr/name/flags differs.");
                AssertCached(); AssertFontMutation(); resourceOpens++;
                if (test.OpenFailure) { ioError = 311; return 0; }
                resourceLive = true; return Resource;
            });
        }

        private uint AllocateSegment()
        {
            var address = bus.Allocate(owner, 132, "Segment", true);
            bus.Long(address, 128); segments.Add(address); return address;
        }

        private void Resident(uint address, byte type)
        { bus.Word(address, 0x4afc); bus.Long(address + 2, address); bus.Memory[address + 12] = type; }

        private void AssertCached() => Require(cacheAdds == 1 && bus.Long(state) != state + 4 &&
            locks.Count != 0 && segment != 0, "Resource open lost its cached SegList.");

        private void AssertFontMutation() => Require(bus.Word((segment << 2) + 22) == 0x0f80,
            "Font classification did not write the DFH_ID to the loaded segment.");

        private string FontFilename()
        {
            var split = Math.Max(0, Math.Max(test.Name.LastIndexOf('/'), test.Name.LastIndexOf(':')));
            return test.Name[..split] + ".font";
        }

        private static string DefaultMessage(uint id) => id switch {
            0xc358 => "'%s' is already a locked resource\n", 0xc359 => "Error while loading '%s' - ",
            0xc35a => "Requires diskfont.library V37 - ", 0xc35b => "'%s' couldn't be loaded as a resource - ",
            _ => throw new InvalidOperationException("Unexpected action diagnostic id.") };

        private uint CloseResource(uint handle, int type)
        {
            Require(handle == Resource && resourceLive &&
                type == (test.Kind == "font" ? 2 : test.Kind == "catalog" ? 3 : 0),
                "Resource close kind or ownership differs.");
            resourceLive = false; resourceCloses++; ioError = 444; return 0;
        }

        public object Execute()
        {
            bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), (int)owner.StackBytes + 32).Fill(0xb6);
            using var cpu = M68kCoreFactory.Default.Create(model, bus);
            cpu.State.StatusRegister = 0; cpu.BeginSubroutine(Load, owner.StackTop, Return);
            for (var index = 0; index < 8; index++) cpu.State.D[index] = (uint)(0xde000000 + index * 16);
            for (var index = 0; index < 7; index++) cpu.State.A[index] = (uint)(0xae000000 + index * 16);
            cpu.State.D[0] = test.MissingLocale ? 1u : 0u; cpu.State.A[0] = context;
            try
            {
                while (cpu.State.ProgramCounter != Return)
                {
                    var pc = cpu.State.ProgramCounter;
                    Require(pc >= Load && pc < Load + codeLength || gateways.Contains(pc), $"Unexpected PC ${pc:X8}.");
                    Require(++owner.Instructions < 250_000 && !cpu.State.Halted && !cpu.State.Stopped,
                        "Native execution did not return."); cpu.ExecuteInstruction();
                }
                Require(unchecked((int)cpu.State.D[0]) == test.ExpectedError &&
                    unchecked((int)bus.Long(context + 12)) == test.ExpectedError,
                    $"Action result changed across cleanup: {cpu.State.D[0]}/{bus.Long(context + 12)}, expected {test.ExpectedError}.");
                Require(bus.Long(context + 24) == (test.RegistryType >= 0 ? 0u : 1u) &&
                    bus.Long(context + 28) == (test.CacheRetained ? 0u : 1u),
                    "Registry/cache state immediately after the action differs.");
                Require(loadCalls == (test.Duplicate ? 0 : 1) && registryAdds == (test.RegistryType >= 0 ? 1 : 0) &&
                    cacheAdds == (!test.Duplicate && test.LoadError == 0 && test.FailAllocation != "Cache" &&
                        test.Kind != "lock-failure" ? 1 : 0), "Action load/cache/registry counts differ.");
                Require(allocFailures == (test.FailAllocation is null ? 0 : 1) &&
                    messageCalls == (test.MessageId == 0 ? 0 : 1) &&
                    catalogQueries == (test.MessageId != 0 && !test.MissingLocale ? 1 : 0),
                    "Failure injection or diagnostic was not exercised.");
                Require(messageOpens == (test.MessageId != 0 && !test.MissingLocale ? 1 : 0) &&
                    messageCloses == (messageOpens == 1 && !test.NullCatalog ? 1 : 0) &&
                    bus.Long(catalogState) == (messageCloses == 1 ? MessageCatalog : 0),
                    "Lazy catalog call counts or deliberately uncleared pointer differs.");
                Require(!resourceLive && !diskfontLive && !dosLive && allocations.Count == 0 &&
                    linked.Count == 0 && locks.Count == 0 && vector == SavedVector &&
                    forbidDepth == 0 && semaphoreDepth == 0, "Final ownership or scheduling state leaked.");
                var expectedOpens = !test.Duplicate && (test.Kind is "library" or "font" or "catalog") &&
                    test.FailAllocation is not ("Cache" or "FontName") && !test.DiskfontMissing &&
                    !(test.Kind == "catalog" && test.MissingLocale) ? 1 : 0;
                Require(resourceOpens == expectedOpens && resourceCloses == (test.Duplicate ? 1 :
                    expectedOpens == 1 && !test.OpenFailure ? 1 : 0), "Resource open/close counts differ.");
                Require(diskfontOpens == (test.Kind == "font" && test.FailAllocation != "FontName" ? 1 : 0) &&
                    diskfontCloses == (diskfontOpens == 1 && !test.DiskfontMissing ? 1 : 0),
                    "Diskfont lease counts differ.");
                Require(sameLockCalls == (test.Consume ? 1 : 0) && bus.Long(context + 40) == (test.Consume ? segment : 0),
                    "Native one-shot hook transfer differs.");
                Require(unloadCalls == (!test.Duplicate && test.LoadError == 0 && !test.Consume ? 1 : 0),
                    "SegList unload ownership differs.");
                if (test.Consume)
                { foreach (var address in segments) bus.Release(owner, address, "Segment"); segments.Clear(); }
                Require(segments.Count == 0 && cpu.State.A[7] == owner.StackTop &&
                    bus.Memory.AsSpan((int)owner.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0 &&
                    bus.Memory.AsSpan((int)(owner.StackTop - owner.StackBytes - 16), 16).IndexOfAnyExcept((byte)0xb6) < 0,
                    "Segments or caller stack leaked/corrupted.");
                bus.Release(owner, context, "FixtureContext"); bus.Release(owner, task, "FixtureTask");
                bus.Release(owner, port, "FixturePort"); bus.Release(owner, translated, "FixtureTranslation");
                bus.Release(owner, catalogState, "FixtureCatalogState");
                bus.AssertReleased(owner); bus.AssertImageUnchanged();
                return new { id = test.Id, result = test.ExpectedError, owner.Instructions,
                    loadCalls, unloadCalls, cacheAdds, registryAdds, resourceOpens, resourceCloses,
                    diskfontOpens, diskfontCloses, allocFailures, messageCalls, catalogQueries, messageOpens, messageCloses,
                    sameLockCalls, output = output.ToString(), sharedImageWrites = 0, events };
            }
            catch (Exception error)
            { throw new InvalidOperationException($"{test.Id} PC={cpu.State.ProgramCounter:X8}; events={string.Join(',', events)}", error); }
        }

        private void Register(uint library, short offset, string name, Func<M68kCpuState, uint> handler,
            uint? entry = null)
        {
            var address = entry ?? checked((uint)(library + offset)); gateways.Add(address);
            bus.RegisterGateway(address, s => {
                Require(s.A[6] == library, $"{name}: wrong library base ${s.A[6]:X8}.");
                if (library == DosBase) Require(dosLive, "DOS vector used outside lease.");
                events.Add(name); var result = handler(s);
                s.D[0] = result; s.D[1] = 0xd1d1d1d1; s.A[0] = 0xa0a0a0a0; s.A[1] = 0xa1a1a1a1;
                s.StatusRegister = (ushort)((s.StatusRegister & 0xffe0) | 0x001f);
            });
        }
    }
}
