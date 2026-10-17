using System.Security.Cryptography;
using System.Reflection;
using System.Text.Json;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.Exe2ArcFrontendNativeExecution;

internal static class FrontendRuntimeSuite
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string[] args)
    {
        FrontendFixture? fixture = null;
        string? imageHash = null;
        long imageBytes = 0;
        string status = "failed";
        string? failure = null;
        try
        {
            Require(args.Length == 4,
                "usage: Exe2ArcFrontendNativeExecution <hunk> <68000|68020|68040> <report.json> exe2arc-frontend-native-smoke");
            Require(args[3] is FrontendCases.Suite or FrontendCases.RuntimeSuite or FrontendCases.TypeDiagnosticSuite, "Unknown fixture suite.");
            var model = args[1] switch
            {
                "68000" => M68kCpuModel.M68000,
                "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040,
                _ => throw new ArgumentException("Unsupported CPU target."),
            };
            Require(File.Exists(args[0]), "Resident HUNK is missing.");
            Require(!File.Exists(args[2]), "The receipt path must be new.");
            imageHash = Hash(args[0]);
            imageBytes = new FileInfo(args[0]).Length;
            var image = HunkImage.Load(args[0], FrontendFixture.LoadAddress);
            Require(image.Sha256 == imageHash, "HUNK changed during admission.");
            fixture = new(image, model, args[3] == FrontendCases.RuntimeSuite, args[3] == FrontendCases.TypeDiagnosticSuite);
            fixture.Run();
            Require(Hash(args[0]) == imageHash, "HUNK changed during execution.");
            status = "passed";
        }
        catch (Exception error)
        {
            failure = error.ToString();
            Console.Error.WriteLine($"FAIL Exe2Arc runtime fixture: {error.Message}");
        }

        var report = new
        {
            schemaVersion = 1,
            status,
            suite = args.ElementAtOrDefault(3),
            expectedSuite = args.ElementAtOrDefault(3),
            cpu = args.ElementAtOrDefault(1),
            inputHunk = args.ElementAtOrDefault(0),
            imageSha256 = imageHash,
            imageBytes,
            failure,
            instructionExecutor = "Copper68k " + typeof(FrontendRuntimeSuite).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "Copper68kPackageVersion").Value,
            instructionCoreSha256 = Hash(typeof(M68kCoreFactory).Assembly.Location),
            managedExecutorSha256 = Hash(typeof(FrontendRuntimeSuite).Assembly.Location),
            compilerMapSha256 = args.Length > 0 && File.Exists(args[0]+".map") ? Hash(args[0]+".map") : null,
            imageLoads = 1,
            sharedImageWrites = 0,
            evidence = new
            {
                fixture = "supplied DOS/Exec vectors; inert input files, checked output bytes and exact allocation/file/message ownership",
                realKickstartExecution = false,
                realCopperStartExecution = false,
                realDosParser = false,
                realFilesystemHandler = false,
                originalGuestExecution = false,
                redirectedOutputParity = false,
                shippingOrPureApproval = false,
                scope = args.ElementAtOrDefault(3) == FrontendCases.RuntimeSuite ? "Full shipping entry: seven scanner/extractor paths, failure injection, CLI/Workbench startup, resource cleanup and repeated/instruction-interleaved invocation. No original-command parity claim." : args.ElementAtOrDefault(3) == FrontendCases.TypeDiagnosticSuite ? "Type diagnostic probe only." : "Parser failure and result-allocation smoke only."
            },
            cases = fixture?.Results ?? [],
            lastFailure = fixture?.LastFailure,
        };
        try
        {
            Require(args.Length >= 3, "No receipt path supplied.");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
            File.WriteAllText(args[2], JsonSerializer.Serialize(report, JsonOptions) + "\n");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Could not write smoke receipt: {error.Message}");
            return 1;
        }
        if (status != "passed") return 1;
        Console.WriteLine($"PASS {args[1]}: {fixture!.Results.Count} Exe2Arc runtime fixture invocations; no guest or shipping claim.");
        return 0;
    }

    private static string Hash(string path) =>
        Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
