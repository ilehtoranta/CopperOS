using System.Security.Cryptography;
using System.Text.Json;
using CopperSharp.Compiler;
using CopperSharp.Targets.Amiga;

var options = new Dictionary<string, string>();
for (int i = 0; i < args.Length; i += 2) options.Add(args[i], args[i + 1]);
string runtime = Path.GetFullPath(options["--runtime"]), output = Path.GetFullPath(options["--output"]);
if (Directory.Exists(output)) throw new IOException("Use a fresh output directory");
Directory.CreateDirectory(output);
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
string[] names = ["CopperStart.Shell.dll", "CopperOS.Shell.Dos.dll", "CopperStart.Dos.dll", "CopperStart.Exec.dll",
    "CopperSharp.Sdk.Amiga.dll", "CopperSharp.Sdk.Amiga.Support.dll", "CopperSharp.Compiler.dll", "CopperStart.Utility.dll", "CopperStart.Icon.dll"];
string[] inputs = names.Select(n => n == "CopperStart.Dos.dll" ? Path.GetFullPath(options["--client"]) : Path.Combine(runtime, n)).ToArray();
var before = inputs.Select(Hash).ToArray();
bool size = options.GetValueOrDefault("--size", "false") == "true";
var result = AmigaM68kCompiler.Compile(new M68kCompilationRequest
{
    AssemblyPath = inputs[0], EntryPoint = "CopperStart.Shell.NativeShellBoot::TaskEntry",
    ManagedAssemblyPaths = inputs.Skip(1).ToArray(),
    Cpu = M68kCpuTarget.M68000, ClrPolicy = M68kClrPolicy.Always,
    ExceptionMode = M68kExceptionMode.Yolo, PeepholeOptimization = M68kPeepholeOptimizationMode.FixedPoint,
    RomSizeOptimizations = size ? null : new M68kRomSizeOptions(),
    CodeSizeOptimizations = size ? new M68kCodeSizeOptions {
        RemoveRedundantTransport = true, CompactPrivateStackCopies = true,
        SimplifyLoopPhis = true, CompactGuestMemory = true, NarrowOperations = true,
        EliminateRedundantInitialization = true, SizeFirstCosts = true,
        InlineMemoryHelpers = true, ShareArithmeticCores = true
    } : null, OutputFormat = M68kOutputFormat.Hunk,
    RuntimeProfile = M68kRuntimeProfile.Freestanding, MemoryManagement = M68kMemoryManagement.None,
    Hunk = new HunkOutputOptions { IncludeSymbols = false },
    IncludedExportNames = ["copperos.shell.entry", "copperos.shell.child", "copperos.shell.queue-handler-task",
        "copperos.shell.execute-begin", "copperos.shell.execute-poll", "copperos.shell.execute-park",
        "copperstart.dos.process-return", "copperstart.shell.run", "copperstart.utility.dispatch",
        "copperstart.library.open-disk", "copperstart.library.loader-task", "copperstart.library.expunge-disk", "copperstart.library.close-disk", "copperstart.icon.dispatch", "copperstart.expansion.dispatch"]
});
File.WriteAllText(Path.Combine(output, "CopperShell.map"), result.Map);
File.WriteAllText(Path.Combine(output, "compatibility.json"), JsonSerializer.Serialize(result.NativeCompatibility, new JsonSerializerOptions { WriteIndented = true }));
var packetHome = System.Text.RegularExpressions.Regex.Match(result.Map,
    @"FRAME-SLOT method=CopperStart\.Dos\.DosNativeClientService::Create kind=local index=0 offset=\d+ size=(\d+) address-taken=yes");
if (packetHome.Success && int.Parse(packetHome.Groups[1].Value) < 136)
    throw new InvalidDataException("Native process packet stack home is smaller than its wire record");
if (!result.FrameworkAnalysis.IsCompatible || result.FrameworkAnalysis.ManagedAllocationSites.Count != 0 ||
    result.NativeCompatibility.FatalMachineFaultSiteCount != 0 || result.NativeCompatibility.RuntimeFeatures.Count != 0 ||
    result.NativeCompatibility.RuntimeHelpers.Count != 0 || result.NativeCompatibility.ExternalNativeTargets.Count != 0)
    throw new InvalidDataException("Shell requires unsupported runtime services");
if (!inputs.Select(Hash).SequenceEqual(before)) throw new InvalidDataException("Compiler inputs changed during compilation");
File.WriteAllBytes(Path.Combine(output, "CopperShell"), result.Image);
Directory.CreateDirectory(Path.Combine(output, "client"));
File.Copy(inputs[2], Path.Combine(output, "client", names[2]));
File.WriteAllText(Path.Combine(output, "shell-manifest.json"), JsonSerializer.Serialize(new
{
    sha256 = Hash(Path.Combine(output, "CopperShell")), peepholeOptimization = "FixedPoint", codeSizeOptimizations = size,
    dosClient = new { originalDosSha256 = Hash(Path.Combine(runtime, names[2])), serviceVersion = 2, path = "client/" + names[2] },
    inputs = names.Select((n, i) => new { file = n, sha256 = before[i] })
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Shell code: {result.Code.Length} bytes; HUNK: {result.Image.Length} bytes");
