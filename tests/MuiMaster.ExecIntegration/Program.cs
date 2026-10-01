using System.Security.Cryptography;
using System.Text.Json;
using Amiga;
using Copper68k;
using CopperSharp.Compiler;
using CopperSharp.Targets.Amiga;
using CopperStart.Exec;
using CopperStart.Intuition;
using Roots = CopperOS.MuiMaster.NativeRoot.MuiLibraryIntegrationRoots;
using ProviderRoots = CopperOS.MuiMaster.NativeRoot.MuiNativeProviderIntegrationRoots;

namespace CopperOS.MuiMaster.ExecIntegration;

internal static class Program
{
    private const uint ClientLoad = 0x00400000;
    private const uint KernelLoad = 0x00600000;
    private const uint LibraryLoad = 0x00500000;
    private const uint IntuitionLoad = 0x00700000;
    private const uint Stack = 0x00F00000;
    private const uint ReturnPc = 0x00300000;
    private const string ClientEntry =
        "CopperOS.MuiMaster.NativeRoot.MuiExecIntegrationRoots::PrivateRootLifecycle";
    private const string Usage = "usage: ExecIntegration [68000|68020|68040] [library | providers | classes | packaged <hunk-path>] [--output <directory>]";

    public static int Main(string[] args)
    {
        try
        {
            string? outputDirectory = null;
            if (args.Length >= 2 && args[^2] == "--output")
            {
                outputDirectory = args[^1];
                if (string.IsNullOrWhiteSpace(outputDirectory))
                    throw new ArgumentException(Usage);
                args = args[..^2];
            }
            var target = args.Length == 0 ? M68kCpuTarget.M68000 : args[0] switch
            {
                "68000" => M68kCpuTarget.M68000,
                "68020" => M68kCpuTarget.M68020,
                "68040" => M68kCpuTarget.M68040,
                _ => throw new ArgumentException(Usage),
            };
            var library = args.Length > 1 && args[1] == "library";
            var packaged = args.Length > 1 && args[1] == "packaged";
            var classes = args.Length > 1 && args[1] == "classes";
            var providers = args.Length > 1 && args[1] == "providers";
            if ((packaged && args.Length != 3) || (!packaged && args.Length > 2) ||
                (args.Length > 1 && !library && !packaged && !classes && !providers)) throw new ArgumentException(Usage);
            Run(target, library || packaged || providers,
                packaged ? Path.GetFullPath(args[2]) : null, classes, providers,
                outputDirectory);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static void Run(M68kCpuTarget target, bool libraryScenario,
        string? packagedPath, bool classes, bool providers,
        string? requestedOutputDirectory)
    {
        var packaged = packagedPath is null ? null : PackagedHunk.Read(packagedPath);
        var clientAssembly = typeof(global::CopperOS.MuiMaster.NativeRoot.MuiNativeRoots).Assembly.Location;
        var harnessAssembly = typeof(ExecBootstrap).Assembly.Location;
        string[] dependencies =
        [
            typeof(ExecMemoryCore).Assembly.Location,
            typeof(IntuitionBoopsiOwnedEntrypoints).Assembly.Location,
            typeof(APTR).Assembly.Location,
            typeof(ExecBaseCodec).Assembly.Location,
            typeof(M68kCompiler).Assembly.Location,
        ];
        var inputs = dependencies.Append(clientAssembly).Append(harnessAssembly)
            .Append(typeof(AmigaM68kCompiler).Assembly.Location)
            .Append(typeof(M68kCoreFactory).Assembly.Location)
            .Distinct().ToDictionary(path => path, HashFile);
        if (packagedPath is not null) inputs.Add(packagedPath, HashBytes(packaged!.Image));
        var request = new M68kCompilationRequest
        {
            AssemblyPath = clientAssembly,
            EntryPoint = packaged is not null || classes || providers
                ? "CopperOS.MuiMaster.NativeRoot.MuiLibraryIntegrationRoots::LibraryImageEntry"
                : libraryScenario
                ? "CopperOS.MuiMaster.NativeRoot.MuiLibraryIntegrationRoots::LibraryLifecycle"
                : ClientEntry,
            Cpu = target,
            FloatingPoint = M68kFloatingPointMode.Disabled,
            ClrPolicy = M68kClrPolicy.Always,
            ExceptionMode = M68kExceptionMode.Yolo,
            OutputFormat = M68kOutputFormat.Hunk,
            RuntimeProfile = M68kRuntimeProfile.Freestanding,
            MemoryManagement = M68kMemoryManagement.None,
            IncludedExportNames = providers ? [ProviderRoots.ClientExport] : classes ?
                ["copperos.mui.test.class-client", "copperos.mui.test.class.base-dispatch",
                 "copperos.mui.test.class.custom-dispatch", "copperos.mui.custom.dispatch"] :
                packaged is null ? [] : [Roots.PackagedExport],
            ManagedAssemblyPaths = dependencies.Distinct().ToArray(),
        };
        Console.WriteLine($"Compiling independent MUI client for {target}...");
        var client = AmigaM68kCompiler.Compile(request);
        Validate(client, "MUI client");
        if (client.NativeCompatibility.ReachableAssemblies.Any(assembly =>
            assembly.Name.StartsWith("CopperStart.", StringComparison.Ordinal)))
            throw new InvalidDataException("MUI client reaches CopperStart implementation code.");

        M68kCompilationResult? library = null;
        if (libraryScenario && packaged is null)
        {
            Console.WriteLine($"Compiling separate cold MUI library for {target}...");
            library = AmigaM68kCompiler.Compile(request with
            {
                EntryPoint = "CopperOS.MuiMaster.NativeRoot.MuiLibraryIntegrationRoots::LibraryImageEntry",
                IncludedExportNames = new[]
                {
                    "copperos.mui.library.init", "copperos.mui.library.open",
                    "copperos.mui.library.close", "copperos.mui.library.expunge",
                    "copperos.mui.library.reserved", "copperos.mui.library.unsupported",
                    "copperos.mui.library.get-class", "copperos.mui.library.free-class",
                    "copperos.mui.library.create-custom-class",
                    "copperos.mui.library.delete-custom-class",
                    "copperos.mui.library.new-object-a",
                    "copperos.mui.library.dispose-object",
                    "copperos.mui.library.make-object-a",
                    "copperos.mui.library.error",
                    "copperos.mui.library.set-error",
                    "copperos.mui.library.request-idcmp",
                    "copperos.mui.library.reject-idcmp",
                    "copperos.mui.library.request-a",
                    "copperos.mui.library.request-object-a",
                    "copperos.mui.library.redraw",
                    "copperos.mui.library.layout",
                    "copperos.mui.library.obtain-pen",
                    "copperos.mui.library.release-pen",
                    "copperos.mui.library.add-clipping",
                    "copperos.mui.library.remove-clipping",
                    "copperos.mui.library.add-clip-region",
                    "copperos.mui.library.remove-clip-region",
                    "copperos.mui.library.begin-refresh",
                    "copperos.mui.library.end-refresh",
                    "copperos.mui.library.get-rgb-color",
                }.Concat(new[] { "copperos.mui.custom.dispatch" })
                  .Concat(providers ? [ProviderRoots.AdapterExport] : Array.Empty<string>()).ToArray(),
            });
            Validate(library, "MUI library");
            if (library.NativeCompatibility.ReachableAssemblies.Any(assembly =>
                assembly.Name.StartsWith("CopperStart.", StringComparison.Ordinal)))
                throw new InvalidDataException("MUI library reaches CopperStart implementation code.");
            if (providers && (client.Symbols.Any(symbol => symbol.Name.Contains("MuiNativeProviderOwner", StringComparison.Ordinal) ||
                    symbol.Name.Contains("MuiNativeClassLeaseCore", StringComparison.Ordinal) ||
                    symbol.Name.Contains("MuiNativeServiceAccessCore", StringComparison.Ordinal) ||
                    symbol.Name.Contains("MuiNativeOwnedClassServices", StringComparison.Ordinal)) ||
                !library.Symbols.Any(symbol => symbol.Name.Contains("MuiNativeProviderOwner", StringComparison.Ordinal)) ||
                !library.Symbols.Any(symbol => symbol.Name.Contains("MuiNativeServiceAccessCore", StringComparison.Ordinal)) ||
                !library.Symbols.Any(symbol => symbol.Name.Contains("MuiNativeOwnedClassServices", StringComparison.Ordinal))))
                throw new InvalidDataException("Provider ownership implementation is absent from its library or duplicated in the client.");
        }

        M68kCompilationResult? intuition = null;
        if (classes)
        {
            Console.WriteLine($"Compiling separate production BOOPSI subsystem for {target}...");
            intuition = AmigaM68kCompiler.Compile(request with
            {
                AssemblyPath = typeof(IntuitionBoopsiOwnedEntrypoints).Assembly.Location,
                EntryPoint = "CopperStart.Intuition.IntuitionBoopsiOwnedEntrypoints::ImageEntry",
                // Supplying sibling implementation assemblies can mark metadata
                // lookups as reachable even without emitted methods. The actual
                // subsystem needs only SDK code; do not offer another OS implementation.
                ManagedAssemblyPaths = dependencies.Where(path =>
                    !Path.GetFileName(path).StartsWith("CopperStart.", StringComparison.Ordinal)).ToArray(),
                IncludedExportNames = new[] { "InitializeStorage", "DestroyStorage", "MakeClass", "FreeClass",
                    "AddClass", "RemoveClass", "NewObjectA", "DisposeObject", "DoMethodA", "DoSuperMethodA", "CoerceMethodA" }
                    .Select(name => IntuitionBoopsiOwnedEntrypoints.Prefix + name).ToArray(),
            });
            Validate(intuition, "Production BOOPSI subsystem");
            if (intuition.NativeCompatibility.ReachableAssemblies.Any(assembly =>
                assembly.Name.StartsWith("CopperStart.", StringComparison.Ordinal) && assembly.Name != "CopperStart.Intuition"))
                throw new InvalidDataException("BOOPSI subsystem reaches another CopperStart implementation instead of native OS calls: " +
                    string.Join(", ", intuition.NativeCompatibility.ReachableAssemblies.Select(assembly => assembly.Name)) + "; symbols: " +
                    string.Join(", ", intuition.Symbols.Where(symbol => symbol.Name.Contains("CopperStart.Exec", StringComparison.Ordinal)).Select(symbol => symbol.Name)));
        }

        Console.WriteLine($"Compiling separate native Exec kernel for {target}...");
        var kernel = AmigaM68kCompiler.Compile(request with
        {
            AssemblyPath = harnessAssembly,
            EntryPoint = "CopperOS.MuiMaster.ExecIntegration.ExecBootstrap::KernelEntry",
            IncludedExportNames = providers ? [ExecBootstrap.ExportName, ProviderBootstrap.ExportName] : classes ?
                [ExecBootstrap.ExportName, BoopsiBootstrap.CreateExport, BoopsiBootstrap.DestroyExport,
                 BoopsiBootstrap.AvailableExport] : [ExecBootstrap.ExportName],
        });
        Validate(kernel, "Exec kernel");
        if (classes && kernel.NativeCompatibility.ReachableAssemblies.Any(assembly => assembly.Name == "CopperStart.Intuition"))
            throw new InvalidDataException("Exec bootstrap duplicated the separate BOOPSI implementation.");
        foreach (var input in inputs)
            if (HashFile(input.Key) != input.Value)
                throw new InvalidDataException($"Input changed during compilation: {input.Key}");

        var output = requestedOutputDirectory is null
            ? Path.Combine(AppContext.BaseDirectory, "artifacts", target.ToString(),
                providers ? "providers" : classes ? "classes" : packaged is not null ? "packaged" : libraryScenario ? "library" : ".")
            : Path.GetFullPath(requestedOutputDirectory);
        if (requestedOutputDirectory is not null && Directory.Exists(output) &&
            Directory.EnumerateFileSystemEntries(output).Any())
            throw new IOException("The requested output directory must be empty.");
        Directory.CreateDirectory(output);
        Save(output, classes ? "mui-class-client" : libraryScenario ? "mui-library-client" : "mui-private-root-client", client);
        if (library is not null) Save(output, "mui-library", library);
        if (packaged is not null) File.WriteAllBytes(Path.Combine(output, "copperos-muimaster.library"), packaged.Image);
        Save(output, "exec-kernel", kernel);
        if (intuition is not null) Save(output, "intuition-boopsi", intuition);
        File.WriteAllText(Path.Combine(output, "inputs.json"), JsonSerializer.Serialize(inputs));
        Console.WriteLine($"Compiled {target}: client={client.Image.Length} bytes, library={packaged?.Image.Length ?? library?.Image.Length ?? 0} bytes, kernel={kernel.Image.Length} bytes, BOOPSI={intuition?.Image.Length ?? 0} bytes; freestanding qualification passed.");
        if (target != M68kCpuTarget.M68000)
        {
            Console.WriteLine("PASS compile-only; native execution is qualified on MC68000.");
            return;
        }

        var libraryBytes = (uint)(packaged?.Code.Length ?? library?.Code.Length ?? 0);
        if ((ulong)ClientLoad + (uint)client.Code.Length > LibraryLoad ||
            (ulong)LibraryLoad + libraryBytes > KernelLoad ||
            (ulong)KernelLoad + (uint)kernel.Code.Length >= (classes ? IntuitionLoad : Stack - 0x10000) ||
            (ulong)IntuitionLoad + (uint)(intuition?.Code.Length ?? 0) >= Stack - 0x10000)
            throw new InvalidDataException("Native images overlap their reserved execution regions.");
        var bus = new NativeExecBus();
        bus.Load(client, ClientLoad);
        bus.Load(kernel, KernelLoad);
        if (intuition is not null) bus.Load(intuition, IntuitionLoad);
        var packagedResident = packaged?.Install(bus, LibraryLoad);
        if (library is not null)
        {
            bus.Load(library, LibraryLoad);
            LibraryFixture.Prepare(bus, library, LibraryLoad);
            // This slot belongs to the separate library image. Never run its
            // EntryPoint: only the cold export adapters may initialize it.
            foreach (var slot in library.Symbols.Where(symbol => symbol.Name == "_ExecBase"))
                if (bus.ReadLong(LibraryLoad + slot.Address) != 0)
                    throw new InvalidDataException("Library Exec base was initialized before cold Init.");
        }
        using var cpu = M68kCoreFactory.Default.Create(M68kCpuModel.M68000, bus);
        var vectorStart = ExecBootstrap.SysBase - ExecNativeVectorTableCore.NegativeSize;
        var vectorEnd = ExecNativeVectorTableCore.FirstByteAfterDirectRuntime(
            APTR.FromPointer(ExecBootstrap.SysBase)).Raw;
        var ranges = new (uint Start, uint Length)[]
        {
            (ClientLoad, (uint)client.Code.Length),
            (KernelLoad, (uint)kernel.Code.Length),
            (IntuitionLoad, (uint)(intuition?.Code.Length ?? 0)),
            (LibraryLoad, libraryBytes),
            (vectorStart, vectorEnd - vectorStart),
            // Four real JMP vectors are allocated by Exec.MakeLibrary in the
            // heap; instruction checks admit that arena, never host callbacks.
            (ExecBootstrap.HeapStart, libraryScenario || classes ? ExecBootstrap.HeapBytes : 0),
        };
        var bootstrap = kernel.Symbols.Single(symbol => symbol.Name == ExecBootstrap.ExportName);
        var setup = Execute(cpu, bus, KernelLoad + bootstrap.Address, ranges, 1);
        if (providers)
        {
            RunProviders(cpu, bus, ranges, client, kernel, library!, setup, output);
            return;
        }
        if (classes)
        {
            RunClasses(cpu, bus, ranges, client, kernel, intuition!, setup, output);
            return;
        }
        NativeSample? executableGuard = null;
        if (packaged is not null)
            executableGuard = Execute(cpu, bus, LibraryLoad, ranges, uint.MaxValue);
        var coldInit = packagedResident?.InitEntry ?? (library is null ? 0 : LibraryLoad + library.Symbols.Single(symbol =>
            symbol.Name == "copperos.mui.library.init").Address);
        var openEntry = packagedResident?.OpenEntry ?? (library is null ? 0 : LibraryLoad + library.Symbols.Single(symbol =>
            symbol.Name == "copperos.mui.library.open").Address);
        var clientEntry = packaged is null ? client.EntryPoint : client.Symbols.Single(symbol =>
            symbol.Name == Roots.PackagedExport).Address;
        var idcmpBoundaries = new Dictionary<uint, string>();
        if (library is not null)
        {
            foreach (var export in new[] { "request-idcmp", "reject-idcmp" })
            {
                var name = "copperos.mui.library." + export;
                var symbol = library.Symbols.Single(candidate => candidate.Name == name);
                idcmpBoundaries.Add(LibraryLoad + symbol.Address, name);
            }
        }
        else if (packagedResident is not null)
        {
            foreach (var (lvo, name) in new[]
            {
                (-90, "copperos.mui.library.request-idcmp"),
                (-96, "copperos.mui.library.reject-idcmp"),
            })
            {
                var vectorIndex = MuiResidentMetadata.ManagementVectorCount +
                    ((MuiResidentMetadata.FirstLvo - lvo) /
                     MuiResidentMetadata.VectorStride);
                var entryAddress = bus.ReadLong(packagedResident.FunctionTableAddress +
                    (uint)(vectorIndex * sizeof(uint)));
                idcmpBoundaries.Add(entryAddress, name);
            }
        }
        NativeSample? rejectedExtent = null;
        if (packaged is not null)
        {
            rejectedExtent = Execute(cpu, bus, ClientLoad + clientEntry, ranges, 171,
                coldInit, openEntry, LibraryLoad, Resident.Size - 1);
            if (rejectedExtent.ObservedEntryVisits != 0)
                throw new InvalidDataException("Invalid packaged extent invoked the resident initializer.");
        }
        var sample = Execute(cpu, bus, ClientLoad + clientEntry, ranges, 42, coldInit, openEntry,
            packaged is null ? 0 : LibraryLoad, packaged is null ? 0 : libraryBytes,
            observedBoundaries: idcmpBoundaries,
            observedOwnerAllocationBytes: libraryScenario ? Roots.LibraryOwnerBytes : 0);
        foreach (var name in idcmpBoundaries.Values.Distinct())
            if (sample.BoundaryVisits.GetValueOrDefault(name) != 1)
                throw new InvalidDataException($"Expected one guest call through {name}; observed " +
                    sample.BoundaryVisits.GetValueOrDefault(name) + ".");
        if (libraryScenario && (sample.ObservedEntryVisits != 6 || sample.OwnerAllocationRequests != 5))
            throw new InvalidDataException($"Expected four successful initializers, one root-allocation failure and one owner-allocation failure; observed {sample.ObservedEntryVisits} Init entries and {sample.OwnerAllocationRequests} owner-sized allocation requests.");
        if (bus.HostTrapAttempts != 0) throw new InvalidDataException("Native run attempted a host trap.");
        File.WriteAllText(Path.Combine(output, "execution.json"), JsonSerializer.Serialize(new
        {
            Cpu = "MC68000", Scope = packaged is not null
                ? "On-disk HUNK load/relocation, native embedded Resident discovery and cold MUI lifecycle via real Exec; no DOS LoadSeg or MUI class dispatch"
                : libraryScenario
                ? "Cold resident MUI Init/Open/Close/Expunge via independent Exec and library images; no disk loader or MUI class dispatch"
                : "Exec-owned MUI private-root lifecycle; no resident/disk loader or Intuition integration",
            ExecBootstrap.HeapStart, ExecBootstrap.HeapBytes, Setup = setup, Client = sample,
            bus.HostTrapAttempts, ClientSha256 = HashBytes(client.Image), KernelSha256 = HashBytes(kernel.Image),
            LibrarySha256 = packaged is not null ? HashBytes(packaged.Image) : library is null ? null : HashBytes(library.Image),
            PackagedPath = packagedPath, PackagedResident = packagedResident, PackagedCodeBytes = packaged?.Code.Length,
            ExecutableGuard = executableGuard,
            RejectedExtent = rejectedExtent,
            LibraryStartupExecuted = false,
        }));
        Console.WriteLine($"PASS M68000 real Exec return=42 instructions={sample.Instructions} cycles={sample.Cycles} host-traps=0; bootstrap instructions={setup.Instructions} cycles={setup.Cycles}");
    }

    private static void RunProviders(IM68kCore cpu, NativeExecBus bus,
        (uint Start, uint Length)[] ranges, M68kCompilationResult client,
        M68kCompilationResult kernel, M68kCompilationResult library, NativeSample setup, string output)
    {
        uint LibraryExport(string name) => LibraryLoad + library.Symbols.Single(symbol => symbol.Name == name).Address;
        var taskContext = Execute(cpu, bus, KernelLoad + kernel.Symbols.Single(symbol =>
            symbol.Name == ProviderBootstrap.ExportName).Address, ranges, 1);
        var adapter = LibraryExport(ProviderRoots.AdapterExport);
        var boundaries = new Dictionary<uint, string> { [adapter] = ProviderRoots.AdapterExport };
        var sample = Execute(cpu, bus, ClientLoad + client.Symbols.Single(symbol =>
            symbol.Name == ProviderRoots.ClientExport).Address, ranges, 42,
            observedEntry: LibraryExport("copperos.mui.library.init"),
            observedOpenEntry: LibraryExport("copperos.mui.library.open"), initialA0: adapter,
            observedBoundaries: boundaries, observedOwnerAllocationBytes: Roots.LibraryOwnerBytes,
            ordinaryTask: true);
        if (sample.ObservedEntryVisits != 1 || sample.OwnerAllocationRequests != 1 ||
            sample.BoundaryVisits.GetValueOrDefault(ProviderRoots.AdapterExport) != 11 ||
            sample.LibraryOpens.Count != 4 || sample.LibraryOpens.Count(call => call.Name == "utility.library") != 2 ||
            sample.LibraryOpens.Count(call => call.Name == "copperos-muimaster.library") != 2 ||
            sample.LibraryOpens.Any(call => call.Version != 0 || (call.Status & 0x2700) != 0 ||
                call.InterruptNesting != -1 || call.TaskNesting != -1))
            throw new InvalidDataException("Provider probe did not reach precisely two ordinary-context real Exec utility lookup failures.");
        if (bus.HostTrapAttempts != 0) throw new InvalidDataException("Provider run attempted a host trap.");
        File.WriteAllText(Path.Combine(output, "execution.json"), JsonSerializer.Serialize(new
        {
            Cpu = "MC68000",
            Scope = "Independent production MUI owner admission and repeated missing Utility acquisition through real Exec; no successful provider set or packaged/public MUI vector claim",
            Setup = setup, TaskContext = taskContext, Client = sample, Adapter = adapter,
            bus.HostTrapAttempts, ClientSha256 = HashBytes(client.Image), KernelSha256 = HashBytes(kernel.Image),
            LibrarySha256 = HashBytes(library.Image), LibraryStartupExecuted = false,
        }));
        Console.WriteLine($"PASS M68000 provider owner return=42 instructions={sample.Instructions} cycles={sample.Cycles} utility-lookups=2 host-traps=0");
    }

    private static void RunClasses(IM68kCore cpu, NativeExecBus bus,
        (uint Start, uint Length)[] ranges, M68kCompilationResult client,
        M68kCompilationResult kernel, M68kCompilationResult intuition, NativeSample setup, string output)
    {
        uint KernelExport(string name) => KernelLoad + kernel.Symbols.Single(symbol => symbol.Name == name).Address;
        APTR IntuitionExport(string name) => APTR.FromPointer(IntuitionLoad + intuition.Symbols.Single(symbol =>
            symbol.Name == IntuitionBoopsiOwnedEntrypoints.Prefix + name).Address);
        BoopsiBootstrapBindingsCodec.Write(bus, new BoopsiBootstrapBindings
        {
            Initialize = IntuitionExport("InitializeStorage"),
            MakeClass = IntuitionExport("MakeClass"), FreeClass = IntuitionExport("FreeClass"),
            AddClass = IntuitionExport("AddClass"), RemoveClass = IntuitionExport("RemoveClass"),
            NewObject = IntuitionExport("NewObjectA"), DisposeObject = IntuitionExport("DisposeObject"),
        });
        var boundaries = intuition.Symbols.Where(symbol => symbol.Name.StartsWith(IntuitionBoopsiOwnedEntrypoints.Prefix,
            StringComparison.Ordinal)).ToDictionary(symbol => IntuitionLoad + symbol.Address, symbol => symbol.Name);
        foreach (var symbol in intuition.Symbols.Where(symbol =>
            symbol.Name.Contains("BoopsiClassRegistryCore::FindClass<", StringComparison.Ordinal) ||
            symbol.Name.Contains("BoopsiClassRegistryCore::ClassIdsEqual<", StringComparison.Ordinal) ||
            symbol.Name.Contains("IntuitionMakeClassCore::TryBuildRequest<", StringComparison.Ordinal)))
            boundaries.Add(IntuitionLoad + symbol.Address, symbol.Name);
        foreach (var symbol in client.Symbols.Where(symbol => symbol.Name is "copperos.mui.custom.dispatch" or
            "copperos.mui.test.class.base-dispatch" or "copperos.mui.test.class.custom-dispatch"))
            boundaries.Add(ClientLoad + symbol.Address, symbol.Name);
        var available = KernelExport(BoopsiBootstrap.AvailableExport);
        var before = Execute(cpu, bus, available, ranges, null);
        // Detect accidental reuse of the old fixed BOOPSI registry; it is not
        // state for this owned subsystem and must remain entirely untouched.
        for (uint offset = 0; offset < BoopsiClassRegistryCore.StateSize; offset += 4)
            bus.WriteLong(IntuitionBoopsiNativeEntrypoints.StateAddress + offset, 0xA5C37E19);
        var created = Execute(cpu, bus, KernelExport(BoopsiBootstrap.CreateExport), ranges, null,
            initialA0: BoopsiBootstrapBindingsCodec.Address, observedBoundaries: boundaries);
        var library = created.Result;
        if ((library & 3) != 0 || library < ExecBootstrap.HeapStart + IntuitionBoopsiOwnedEntrypoints.NegativeBytes ||
            (ulong)library + IntuitionBoopsiOwnedEntrypoints.PositiveBytes > ExecBootstrap.HeapStart + ExecBootstrap.HeapBytes)
            throw new InvalidDataException("Owned BOOPSI subsystem allocation failed or escaped the Exec heap.");
        var rootNameOffset = client.Code.AsSpan().IndexOf("rootclass\0"u8);
        if (rootNameOffset < 0)
            throw new InvalidDataException("MUI class client does not contain its rootclass query string.");
        var nativeMemory = new ExecIntegrationIntuitionMemory(bus);
        var ownedRegistry = APTR.FromPointer(library + IntuitionBase.Size);
        var rootClass = BoopsiClassRegistryCore.FindClass(ref nativeMemory,
            ownedRegistry, APTR.FromPointer(ClientLoad + (uint)rootNameOffset));
        if (rootClass.IsNull)
            throw new InvalidDataException("Host codec view cannot resolve the initialized native rootclass registry entry.");
        var rootClassRecord = BOOPSIGuestCodec.ReadClass(ref nativeMemory, rootClass);
        Console.WriteLine($"Verified native rootclass record ${rootClass.Raw:X8}, id='{bus.TextAt(rootClassRecord.cl_ID.Raw)}'.");
        var sample = Execute(cpu, bus,
            ClientLoad + client.Symbols.Single(symbol => symbol.Name == "copperos.mui.test.class-client").Address,
            ranges, 42, initialA0: library,
            initialA1: IntuitionExport("DoSuperMethodA").Raw,
            observedBoundaries: boundaries);
        var destroyed = Execute(cpu, bus, KernelExport(BoopsiBootstrap.DestroyExport), ranges, 1,
            initialA0: library, initialA1: IntuitionExport("DestroyStorage").Raw,
            observedBoundaries: boundaries);
        var after = Execute(cpu, bus, available, ranges, before.Result);
        for (uint offset = 0; offset < BoopsiClassRegistryCore.StateSize; offset += 4)
            if (bus.ReadLong(IntuitionBoopsiNativeEntrypoints.StateAddress + offset) != 0xA5C37E19)
                throw new InvalidDataException("Owned BOOPSI execution touched the legacy fixed registry.");
        if (bus.HostTrapAttempts != 0) throw new InvalidDataException("Native class run attempted a host trap.");
        File.WriteAllText(Path.Combine(output, "execution.json"), JsonSerializer.Serialize(new
        {
            Cpu = "MC68000",
            Scope = "MUI native class provider and actual owned CopperStart BOOPSI subsystem via SDK LVOs; not published Intuition lifecycle or packaged MUI class-vector admission",
            Setup = setup, ClassClient = sample, OwnedBoopsiCreate = created, OwnedBoopsiDestroy = destroyed,
            HeapBefore = before.Result, HeapAfter = after.Result, LegacyRegistryUntouched = true,
            LibraryBase = library, IntuitionBoopsiOwnedEntrypoints.PositiveBytes, IntuitionBoopsiOwnedEntrypoints.NegativeBytes,
            bus.HostTrapAttempts, ClientSha256 = HashBytes(client.Image), KernelSha256 = HashBytes(kernel.Image),
            IntuitionSha256 = HashBytes(intuition.Image), IntuitionCodeBytes = intuition.Code.Length,
            IntuitionLoad, IntuitionStartupExecuted = false,
        }));
        Console.WriteLine($"PASS M68000 native MUI/owned BOOPSI classes return=42 instructions={sample.Instructions} cycles={sample.Cycles} heap={before.Result}->{after.Result} host-traps=0.");
    }

    private static void Validate(M68kCompilationResult result, string name)
    {
        if (result.Code.Length == 0 || result.Image.Length == 0 ||
            !result.FrameworkAnalysis.IsCompatible || result.FrameworkFeatures.Count != 0 ||
            result.FrameworkAnalysis.Members.Count != 0 ||
            result.FrameworkAnalysis.ManagedAllocationSites.Count != 0 ||
            result.NativeCompatibility.ExceptionRegionCount != 0 ||
            result.NativeCompatibility.RuntimeFeatures.Count != 0 ||
            result.NativeCompatibility.RuntimeHelpers.Count != 0 ||
            result.NativeCompatibility.ExternalNativeTargets.Count != 0 ||
            result.Relocations.Any(relocation => relocation.Target.StartsWith("runtime:type-descriptor:", StringComparison.Ordinal)))
            throw new InvalidDataException($"{name} failed freestanding native qualification.");
    }

    private static NativeSample Execute(IM68kCore cpu, NativeExecBus bus, uint entry,
        (uint Start, uint Length)[] ranges, uint? expected, uint observedEntry = 0, uint observedOpenEntry = 0,
        uint initialA0 = 0, uint initialD0 = 0, uint initialA1 = 0,
        IReadOnlyDictionary<uint, string>? observedBoundaries = null,
        uint observedOwnerAllocationBytes = 0, bool ordinaryTask = false)
    {
        bus.WriteLong(Stack, ReturnPc);
        cpu.Reset(entry, Stack);
        if (ordinaryTask)
        {
            cpu.State.ResetStackPointers(Stack - 0x20000, Stack, false);
            cpu.State.StatusRegister = 0;
        }
        cpu.State.A[0] = initialA0;
        cpu.State.A[1] = initialA1;
        cpu.State.D[0] = initialD0;
        var startCycles = cpu.State.Cycles;
        var startSr = cpu.State.StatusRegister;
        var instructions = 0;
        var observedVisits = 0;
        var openVisits = 0;
        var ownerAllocationRequests = 0;
        var calls = new List<string>();
        var libraryOpens = new List<LibraryOpenObservation>();
        var boundaryVisits = new Dictionary<string, int>();
        var boundaries = new Stack<NativeBoundary>();
        var lastPcs = new uint[32];
        while (cpu.State.ProgramCounter != ReturnPc)
        {
            var pc = cpu.State.ProgramCounter;
            if (boundaries.Count != 0 && pc == boundaries.Peek().ReturnAddress)
            {
                var boundary = boundaries.Pop();
                calls.Add($"return {boundary.Name} D0=${cpu.State.D[0]:X8}");
                for (var index = 2; index < 8; index++)
                    if (cpu.State.D[index] != boundary.Data[index - 2])
                        throw new InvalidDataException($"{boundary.Name} clobbered D{index}: ${boundary.Data[index - 2]:X8} -> ${cpu.State.D[index]:X8}.");
                for (var index = 2; index < 7; index++)
                    if (cpu.State.A[index] != boundary.Address[index - 2])
                        throw new InvalidDataException($"{boundary.Name} clobbered A{index}: ${boundary.Address[index - 2]:X8} -> ${cpu.State.A[index]:X8}.");
            }
            if (pc == ExecBootstrap.SysBase - 102 || (observedEntry != 0 && pc == observedEntry))
                boundaries.Push(new NativeBoundary(pc == observedEntry ? "MUI Init" : "Exec InitResident",
                    bus.ReadLong(cpu.State.A[7]),
                    Enumerable.Range(2, 6).Select(index => cpu.State.D[index]).ToArray(),
                    Enumerable.Range(2, 5).Select(index => cpu.State.A[index]).ToArray()));
            if (observedBoundaries is not null && observedBoundaries.TryGetValue(pc, out var boundaryName))
            {
                boundaryVisits.TryGetValue(boundaryName, out var visits);
                boundaryVisits[boundaryName] = visits + 1;
                var classNames = boundaryName == IntuitionBoopsiOwnedEntrypoints.Prefix + "MakeClass"
                    ? $" class='{bus.TextAt(cpu.State.A[0])}' super='{bus.TextAt(cpu.State.A[1])}'"
                    : string.Empty;
                calls.Add($"{boundaryName} A0={cpu.State.A[0]:X8} A1={cpu.State.A[1]:X8} A2={cpu.State.A[2]:X8} A6={cpu.State.A[6]:X8} D0={cpu.State.D[0]:X8}{classNames}");
                boundaries.Push(new NativeBoundary(boundaryName, bus.ReadLong(cpu.State.A[7]),
                    Enumerable.Range(2, 6).Select(index => cpu.State.D[index]).ToArray(),
                    Enumerable.Range(2, 5).Select(index => cpu.State.A[index]).ToArray()));
            }
            if (pc == ExecBootstrap.SysBase - 552)
            {
                var name = bus.TextAt(cpu.State.A[1]);
                var cycle = 0L;
                // Passive ABI observation; SDK-owned offsets stay at this bus
                // boundary and never participate in provider behavior.
                var interruptNesting = unchecked((sbyte)bus.ReadByte(ExecBootstrap.SysBase + ExecLayout.ExecBase.IDNestCount, ref cycle, default));
                var taskNesting = unchecked((sbyte)bus.ReadByte(ExecBootstrap.SysBase + ExecLayout.ExecBase.TaskDisableNestCount, ref cycle, default));
                libraryOpens.Add(new LibraryOpenObservation(name, cpu.State.D[0], cpu.State.StatusRegister, interruptNesting, taskNesting));
                calls.Add($"OpenLibrary A1=${cpu.State.A[1]:X8} '{name}', D0={cpu.State.D[0]}");
            }
            if (observedEntry != 0 && pc == observedEntry) observedVisits++;
            if (observedOpenEntry != 0 && pc == observedOpenEntry) openVisits++;
            if (observedOwnerAllocationBytes != 0 && pc == (uint)(ExecBootstrap.SysBase + ExecLvo.AllocMem) &&
                cpu.State.D[0] == observedOwnerAllocationBytes) ownerAllocationRequests++;
            if (pc >= ExecBootstrap.HeapStart && pc < ExecBootstrap.HeapStart + ExecBootstrap.HeapBytes)
            {
                var cycle = 0L;
                var target = bus.ReadLong(pc + 2);
                if (bus.ReadWord(pc, ref cycle, default) != 0x4EF9 || !ranges.Any(range =>
                    range.Start >= ClientLoad && target >= range.Start && (ulong)target + 2 <= (ulong)range.Start + range.Length))
                    throw new InvalidDataException($"Heap execution was not a native library JMP vector at ${pc:X8}.");
            }
            if (cpu.State.Halted || instructions >= 20_000_000 ||
                cpu.State.Cycles - startCycles > 300_000_000 || (pc & 1) != 0 ||
                !ranges.Any(range => pc >= range.Start &&
                    (ulong)pc + 2 <= (ulong)range.Start + range.Length))
                throw new InvalidDataException($"Native execution escaped or stalled: PC=${pc:X8}, D0=${cpu.State.D[0]:X8}, instructions={instructions}; " +
                    $"A0={cpu.State.A[0]:X8} A1={cpu.State.A[1]:X8} A2={cpu.State.A[2]:X8} A3={cpu.State.A[3]:X8} A6={cpu.State.A[6]:X8}; " +
                    $"recent PCs={string.Join(',', Enumerable.Range(Math.Max(0, instructions - lastPcs.Length), Math.Min(instructions, lastPcs.Length)).Select(index => lastPcs[index % lastPcs.Length].ToString("X8")))}; " +
                    string.Join("; ", calls));
            lastPcs[instructions % lastPcs.Length] = pc;
            cpu.ExecuteInstruction();
            instructions++;
        }
        if (boundaries.Count != 0 || (expected.HasValue && cpu.State.D[0] != expected.Value) || cpu.State.A[7] != Stack + 4 ||
            (cpu.State.StatusRegister & 0xFF00) != (startSr & 0xFF00))
            throw new InvalidDataException($"Native ABI/result failure: D0=${cpu.State.D[0]:X8} expected=${expected:X8}, SP=${cpu.State.A[7]:X8}, SR=${cpu.State.StatusRegister:X4} initial=${startSr:X4}; init-visits={observedVisits}, open-visits={openVisits}; {bus.LibraryDiagnostic()}; {string.Join("; ", calls)}.");
        return new NativeSample(instructions, cpu.State.Cycles - startCycles, cpu.State.D[0], observedVisits,
            ownerAllocationRequests, boundaryVisits, libraryOpens);
    }

    private static void Save(string output, string name, M68kCompilationResult result)
    {
        File.WriteAllBytes(Path.Combine(output, name + ".hunk"), result.Image);
        File.WriteAllText(Path.Combine(output, name + ".map"), result.Map);
        File.WriteAllText(Path.Combine(output, name + ".framework.json"),
            JsonSerializer.Serialize(result.FrameworkAnalysis));
        File.WriteAllText(Path.Combine(output, name + ".native.json"),
            JsonSerializer.Serialize(result.NativeCompatibility));
    }

    private static string HashFile(string path) => HashBytes(File.ReadAllBytes(path));
    private static string HashBytes(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private sealed record NativeSample(int Instructions, long Cycles, uint Result, int ObservedEntryVisits,
        int OwnerAllocationRequests, IReadOnlyDictionary<string, int> BoundaryVisits,
        IReadOnlyList<LibraryOpenObservation> LibraryOpens);
    private sealed record LibraryOpenObservation(string Name, uint Version, ushort Status,
        sbyte InterruptNesting, sbyte TaskNesting);
    private sealed record NativeBoundary(string Name, uint ReturnAddress, uint[] Data, uint[] Address);

    // Host-only view for typed inspection of the executed native fixture. It
    // keeps diagnostics on the same named SDK records as guest code.
    private struct ExecIntegrationIntuitionMemory(NativeExecBus bus) : IIntuitionMemoryPlatform
    {
        public bool IsMapped(APTR address, uint byteSize) => address.IsNotNull &&
            byteSize != 0 && address.Raw <= uint.MaxValue - byteSize &&
            byteSize <= 0x01000000u && address.Raw <= 0x01000000u - byteSize;

        public byte ReadUInt8(APTR address, int offset = 0)
        {
            var cycle = 0L;
            return bus.ReadByte(checked(address.Raw + (uint)offset), ref cycle, default);
        }

        public ushort ReadUInt16(APTR address, int offset = 0)
        {
            var cycle = 0L;
            return bus.ReadWord(checked(address.Raw + (uint)offset), ref cycle, default);
        }

        public uint ReadUInt32(APTR address, int offset = 0) =>
            bus.ReadLong(checked(address.Raw + (uint)offset));

        public void WriteUInt8(APTR address, int offset, byte value)
        {
            var cycle = 0L;
            bus.WriteByte(checked(address.Raw + (uint)offset), value, ref cycle, default);
        }

        public void WriteUInt16(APTR address, int offset, ushort value)
        {
            var cycle = 0L;
            bus.WriteWord(checked(address.Raw + (uint)offset), value, ref cycle, default);
        }

        public void WriteUInt32(APTR address, int offset, uint value) =>
            bus.WriteLong(checked(address.Raw + (uint)offset), value);

        public void Clear(APTR address, uint byteSize)
        {
            for (var index = 0u; index < byteSize; index++)
                WriteUInt8(address, checked((int)index), 0);
        }

        public void Copy(APTR source, APTR destination, uint byteSize)
        {
            for (var index = 0u; index < byteSize; index++)
                WriteUInt8(destination, checked((int)index),
                    ReadUInt8(source, checked((int)index)));
        }
    }
}
