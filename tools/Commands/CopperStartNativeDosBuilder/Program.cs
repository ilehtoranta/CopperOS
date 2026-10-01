using System.Security.Cryptography;
using System.Text.Json;
using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Targets.Amiga;
using CopperStart.Dos;
using CopperStart.Exec;

if (args.Length != 1) throw new ArgumentException("Expected one new native output directory.");
var output = Path.GetFullPath(args[0]);
if (Directory.Exists(output)) throw new IOException("Output already exists; preserve previous evidence.");
Directory.CreateDirectory(output);
var inputs = new[] { typeof(DosCore).Assembly, typeof(ExecMemoryCore).Assembly,
    typeof(CopperStart.Devices.TimerDeviceCore).Assembly, typeof(CopperStart.Layers.LayersNativeRoot).Assembly,
    typeof(APTR).Assembly, typeof(LayersResidentCodec).Assembly, typeof(M68kCompiler).Assembly,
    typeof(AmigaM68kCompiler).Assembly, typeof(Program).Assembly }
    .Distinct().Select(assembly => new { name = assembly.GetName().Name, path = assembly.Location,
        mvid = assembly.ManifestModule.ModuleVersionId, sha256 = Hash(assembly.Location) }).ToArray();
var request = new M68kCompilationRequest
{
    AssemblyPath = typeof(DosNativeProductionRoot).Assembly.Location,
    EntryPoint = "CopperStart.Dos.DosNativeProductionRoot::SystemEntry",
    Cpu = M68kCpuTarget.M68000, ClrPolicy = M68kClrPolicy.Always,
    ExceptionMode = M68kExceptionMode.Yolo, PeepholeOptimization = M68kPeepholeOptimizationMode.Disabled,
    OutputFormat = M68kOutputFormat.Hunk, RuntimeProfile = M68kRuntimeProfile.Freestanding,
    MemoryManagement = M68kMemoryManagement.None,
    IncludedExportNames = ["copperstart.dos.install-system", "copperstart.dos.init-owned-system", "copperstart.dos.process-return"],
    ManagedAssemblyPaths = [typeof(ExecMemoryCore).Assembly.Location,
        typeof(CopperStart.Devices.TimerDeviceCore).Assembly.Location, typeof(DosCore).Assembly.Location,
        typeof(CopperStart.Layers.LayersNativeRoot).Assembly.Location, typeof(APTR).Assembly.Location,
        typeof(LayersResidentCodec).Assembly.Location, typeof(M68kCompiler).Assembly.Location]
};
File.WriteAllText(Path.Combine(output, "request.json"), JsonSerializer.Serialize(request, JsonOptions()));
var result = AmigaM68kCompiler.Compile(request);
File.WriteAllBytes(Path.Combine(output, "dos.hunk"), result.Image);
File.WriteAllText(Path.Combine(output, "dos.map"), result.Map);
File.WriteAllText(Path.Combine(output, "native-compatibility.json"), JsonSerializer.Serialize(result.NativeCompatibility, JsonOptions()));
foreach (var reachable in result.NativeCompatibility.ReachableAssemblies)
{
    var input = inputs.Single(item => item.name == reachable.Name);
    if (input.mvid != reachable.Mvid || input.sha256 != reachable.Sha256)
        throw new InvalidDataException($"Compiler loaded an unexpected assembly: {reachable.Name}");
}
foreach (var input in inputs)
    if (Hash(input.path) != input.sha256) throw new IOException($"Input changed: {input.path}");
if (result.FrameworkAnalysis.ManagedAllocationSites.Count != 0)
    throw new InvalidDataException("Native DOS image contains managed allocations.");
foreach (var required in new[] { "copperstart.dos.install-system", "copperstart.dos.init-owned-system", "copperstart.dos.process-return" })
    if (result.Symbols.Count(symbol => symbol.Name == required) != 1)
        throw new InvalidDataException($"Missing or ambiguous native export: {required}");
var receipt = new { status = "built-not-executed", inputs,
    outputs = new[] { "dos.hunk", "dos.map", "native-compatibility.json", "request.json" }
        .Select(name => { var path = Path.Combine(output, name); return new { path, sha256 = Hash(path), bytes = new FileInfo(path).Length }; }).ToArray(),
    managedAllocationCount = result.FrameworkAnalysis.ManagedAllocationSites.Count,
    shipping = false, pureAdmission = false, runtimeQualified = false,
    scope = "Compile-only production DOS image. No test fixture or CPU/Exec/packet execution occurs in this producer." };
File.WriteAllText(Path.Combine(output, "build.json"), JsonSerializer.Serialize(receipt, JsonOptions()));
Console.WriteLine($"Native DOS built: {result.Image.Length} bytes, SHA256 {Hash(Path.Combine(output, "dos.hunk"))}");
static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
static JsonSerializerOptions JsonOptions() => new() { WriteIndented = true };
