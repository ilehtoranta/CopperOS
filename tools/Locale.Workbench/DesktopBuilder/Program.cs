using System.Security.Cryptography;
using System.Text.Json;
using CopperSharp.Compiler;
using CopperSharp.Targets.Amiga;

var options = new Dictionary<string, string>();
for (int i = 0; i < args.Length; i += 2) options.Add(args[i], args[i + 1]);
var runtime = Path.GetFullPath(options["--runtime"]);
var output = Path.GetFullPath(options["--output"]);
if (Directory.Exists(output)) throw new IOException("Use a fresh output directory");
Directory.CreateDirectory(output);
string Hash(string p) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)));
string[] names = ["CopperStart.DesktopProviders.dll", "CopperStart.Layers.dll", "CopperStart.Utility.dll",
    "CopperSharp.Sdk.Amiga.dll", "CopperSharp.Sdk.Amiga.Support.dll", "CopperSharp.Compiler.dll", "CopperSharp.Targets.Amiga.dll"];
var inputs = names.Select(n => Path.Combine(runtime, n)).ToArray();
var before = inputs.Select(Hash).ToArray();
var exports = JsonSerializer.Deserialize<string[]>(File.ReadAllText(options["--exports"]))!;
bool size = options.GetValueOrDefault("--size", "false") == "true";
var result = AmigaM68kCompiler.Compile(new M68kCompilationRequest
{
    AssemblyPath = inputs[0], EntryPoint = "CopperStart.DesktopProviders.NativeDesktopProviders::DesktopEntry",
    ManagedAssemblyPaths = inputs.Skip(1).ToArray(), Cpu = M68kCpuTarget.M68000,
    ClrPolicy = M68kClrPolicy.Always, ExceptionMode = M68kExceptionMode.Yolo,
    PeepholeOptimization = M68kPeepholeOptimizationMode.FixedPoint,
    CodeSizeOptimizations = size ? new M68kCodeSizeOptions
    {
        RemoveRedundantTransport = true, CompactPrivateStackCopies = true,
        SimplifyLoopPhis = true, CompactGuestMemory = true, NarrowOperations = true,
        EliminateRedundantInitialization = true, SizeFirstCosts = true,
        InlineMemoryHelpers = true, ShareArithmeticCores = true
    } : null,
    OutputFormat = M68kOutputFormat.Hunk, Hunk = new() { IncludeSymbols = false },
    RuntimeProfile = M68kRuntimeProfile.Freestanding, MemoryManagement = M68kMemoryManagement.None,
    IncludedExportNames = exports
});
File.WriteAllText(Path.Combine(output, "CopperDesktop.map"), result.Map);
File.WriteAllText(Path.Combine(output, "compatibility.json"), JsonSerializer.Serialize(result.NativeCompatibility));
if (!result.FrameworkAnalysis.IsCompatible || result.FrameworkAnalysis.ManagedAllocationSites.Count != 0 ||
    result.NativeCompatibility.FatalMachineFaultSiteCount != 0 || result.NativeCompatibility.RuntimeFeatures.Count != 0 ||
    result.NativeCompatibility.RuntimeHelpers.Count != 0 || result.NativeCompatibility.ExternalNativeTargets.Count != 0)
    throw new InvalidDataException("Desktop requires unsupported runtime services");
if (!inputs.Select(Hash).SequenceEqual(before)) throw new InvalidDataException("Compiler inputs changed");
File.WriteAllBytes(Path.Combine(output, "CopperDesktop.hunk"), result.Image);
File.WriteAllText(Path.Combine(output, "desktop.json"), JsonSerializer.Serialize(new
{
    sha256 = Hash(Path.Combine(output, "CopperDesktop.hunk")), entry = result.EntryPoint,
    codeBytes = result.Code.Length, lifetime = "Retain the entire desktop HUNK until reset",
    peepholeOptimization = "FixedPoint", codeSizeOptimizations = size,
    exports, inputs = names.Select((n, i) => new { file = n, sha256 = before[i] })
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Desktop code: {result.Code.Length} bytes; HUNK: {result.Image.Length} bytes");
