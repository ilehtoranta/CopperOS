using System.Security.Cryptography;
using System.Text.Json;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.Exe2ArcFrontendNativeExecution;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Main(string[] args)
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
            Require(args[3] == FrontendCases.Suite, "Unknown fixture suite.");
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
            fixture = new(image, model);
            fixture.Run();
            Require(Hash(args[0]) == imageHash, "HUNK changed during execution.");
            status = "passed";
        }
        catch (Exception error)
        {
            failure = error.ToString();
            Console.Error.WriteLine($"FAIL Exe2Arc frontend smoke: {error.Message}");
        }

        var report = new
        {
            schemaVersion = 1,
            status,
            suite = args.ElementAtOrDefault(3),
            expectedSuite = FrontendCases.Suite,
            cpu = args.ElementAtOrDefault(1),
            inputHunk = args.ElementAtOrDefault(0),
            imageSha256 = imageHash,
            imageBytes,
            failure,
            instructionExecutor = "Copper68k 1.4.0",
            evidence = new
            {
                fixture = "supplied DOS/Exec vectors for parser and result-allocation failure paths",
                realKickstartExecution = false,
                realCopperStartExecution = false,
                realDosParser = false,
                realFilesystemHandler = false,
                originalGuestExecution = false,
                redirectedOutputParity = false,
                shippingOrPureApproval = false,
                scope = "Full resident Exe2Arc entry ownership smoke for parser failure paths only; no input file is opened. Unknown-type and filesystem paths remain open."
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
        Console.WriteLine($"PASS {args[1]}: {fixture!.Results.Count} Exe2Arc frontend smoke invocations; no guest or shipping claim.");
        return 0;
    }

    private static string Hash(string path) =>
        Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
