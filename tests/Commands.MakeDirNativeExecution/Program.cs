using System.Security.Cryptography;
using System.Text.Json;
using Amiga;
using Copper68k;
using CopperOS.Commands.NativeExecution;
using static CopperOS.Commands.MakeDirNativeExecution.MakeDirTestBus;

namespace CopperOS.Commands.MakeDirNativeExecution;

internal static class Program
{
    private const string OriginalSha256 = "23911db49742055d8bddcfe5f8de82cbb2232f8a7ef850d51bfd27f9b54c819b";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static int Main(string[] args)
    {
        MakeDirFixture? original = null;
        MakeDirFixture? generated = null;
        var comparisons = new List<MakeDirComparison>();
        string? generatedHash = null;
        string? referenceHash = null;
        var expectedComparable = 38;
        var expectedGeneratedOnly = 1;
        string? instructionCoreHash = null;
        string? managedExecutorHash = null;
        string? sdkAssemblyHash = null;
        var status = "failed";
        string? failure = null;
        try
        {
            instructionCoreHash = Hash(typeof(M68kCoreFactory).Assembly.Location);
            managedExecutorHash = Hash(typeof(Program).Assembly.Location);
            sdkAssemblyHash = Hash(typeof(APTR).Assembly.Location);
            Require(args.Length == 5,
                "usage: MakeDirNativeExecution <generated.hunk> <68000|68020|68040> <report.json> <makedir-classic-reference-vector-fixture> <private-original.hunk>");
            Require(args[3] == MakeDirFixture.SuiteId, "Unknown MakeDir suite; no alternate suite is silently selected.");
            var model = args[1] switch
            {
                "68000" => M68kCpuModel.M68000,
                "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040,
                _ => throw new ArgumentException("Unsupported native CPU target.")
            };
            ValidatePrivateReference(args[4]);
            Require(File.Exists(args[0]), "Generated HUNK is missing.");
            ValidateReportPath(args[2], args[0], args[4], null);
            referenceHash = Hash(args[4]);
            Require(referenceHash == OriginalSha256 && new FileInfo(args[4]).Length == 464,
                "Private original does not match the pinned 464-byte MakeDir 37.2 member.");
            generatedHash = Hash(args[0]);
            Require(generatedHash != referenceHash, "The original cannot be used as the generated probe.");
            ValidateReportPath(args[2], args[0], args[4], generatedHash);
            var originalImage = HunkImage.Load(args[4], MakeDirFixture.LoadAddress);
            var generatedImage = HunkImage.Load(args[0], MakeDirFixture.LoadAddress);
            Require(originalImage.Sha256 == referenceHash && generatedImage.Sha256 == generatedHash,
                "An input changed between hash verification and HUNK loading.");
            var batches = MakeDirCases.Comparable();
            var ids = batches.SelectMany(batch => batch.Cases).Select(c => c.Id).ToArray();
            Require(ids.Length == expectedComparable && ids.Distinct(StringComparer.Ordinal).Count() == expectedComparable,
                "Missing, duplicated or unexpectedly changed comparable fixture cases.");

            original = new(originalImage, model, true);
            original.Run(batches);
            generated = new(generatedImage, model, false);
            generated.Run(batches);
            Require(original.Observations.Count == expectedComparable && generated.Observations.Count == expectedComparable,
                "Native execution did not produce every comparable case.");
            foreach (var observed in original.Observations)
            {
                var candidate = generated.Observations.Single(c => c.CaseId == observed.CaseId);
                var resultAndError = observed.Result == candidate.Result && observed.IoError == candidate.IoError;
                var output = observed.VPrintfBytesHex == candidate.VPrintfBytesHex;
                var faults = observed.PrintFaultRequests.SequenceEqual(candidate.PrintFaultRequests);
                var callOrder = observed.SemanticEvents.SequenceEqual(candidate.SemanticEvents);
                var passed = resultAndError && output && faults && callOrder;
                comparisons.Add(new(observed.CaseId, passed, resultAndError, output, faults, callOrder, false, false));
                Require(passed, $"Original/generated vector observations differ for {observed.CaseId}.");
            }
            generated.Run([MakeDirCases.GeneratedOnlyAllocationFailure()]);
            Require(generated.Observations.Count == expectedComparable + expectedGeneratedOnly,
                "Generated-only allocation-failure coverage was not executed.");
            Require(Hash(args[4]) == referenceHash && Hash(args[0]) == generatedHash,
                "A HUNK changed during execution; this receipt cannot bind to the supplied files.");
            status = "passed";
        }
        catch (Exception error)
        {
            failure = error.ToString();
            Console.Error.WriteLine($"FAIL MakeDir native/reference vectors: {error.Message}");
        }

        var report = new
        {
            schemaVersion = 1,
            status,
            suite = args.ElementAtOrDefault(3),
            expectedSuite = MakeDirFixture.SuiteId,
            cpu = args.ElementAtOrDefault(1),
            failure,
            instructionExecutor = "Copper68k 1.4.0",
            instructionCorePath = typeof(M68kCoreFactory).Assembly.Location,
            instructionCoreSha256 = instructionCoreHash,
            managedExecutorPath = typeof(Program).Assembly.Location,
            managedExecutorSha256 = managedExecutorHash,
            sdkAssemblyPath = typeof(APTR).Assembly.Location,
            sdkAssemblySha256 = sdkAssemblyHash,
            hostRuntimeVersion = Environment.Version.ToString(),
            evidence = new
            {
                originalCommandMachineCodeExecuted = (original?.NativeInvocationsStarted ?? 0) > 0,
                generatedCommandMachineCodeExecuted = (generated?.NativeInvocationsStarted ?? 0) > 0,
                realKickstartExecution = false,
                realCopperStartExecution = false,
                realDosParser = false,
                realFilesystemHandler = false,
                realDosFormatter = false,
                completeStdoutCaptured = false,
                workbenchLaunchQualified = false,
                minimumStackQualified = false,
                shippingOrPureApproval = false,
                entryRegisterConvention = "NDK 3.1 STARTUP.ASM:264-267 permits image-entry registers except SP to change. D0 is checked as the command result; nonvolatileRegistersRestored is an observation, not a pass requirement. Public-library vector ABI checks remain active.",
                vprintfObservation = "Exact guest format/name bytes, rendered and accepted by a test-only three-format vector.",
                printFaultObservation = "Exact code/header/return requests; DOS fault text is neither rendered nor asserted.",
                inputObservation = "Post-ReadArgs pointer vectors are supplied independently; CLI input bytes are deliberately not parsed.",
                filesystemObservation = "Ordered supplied Lock/CreateDir outcomes and exact BPTR ownership; no real handler or host filesystem operations.",
                originalStorage = "Private input read into memory only; no original bytes included in this report or repository."
            },
            expectedComparableCases = expectedComparable,
            expectedGeneratedOnlyCases = expectedGeneratedOnly,
            original = Side(original, args.ElementAtOrDefault(4), referenceHash),
            generated = Side(generated, args.ElementAtOrDefault(0), generatedHash),
            comparisonsPassed = comparisons.Count(c => c.Passed),
            comparisons,
            generatedOnlyCaseIds = generated?.Observations.Where(c => c.CaseId.Contains("generated-only", StringComparison.Ordinal)).Select(c => c.CaseId).ToArray() ?? []
        };
        try
        {
            Require(args.Length >= 3, "No report path was supplied for the failure receipt.");
            ValidateReportPath(args[2], args.ElementAtOrDefault(0), args.ElementAtOrDefault(4), generatedHash);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
            File.WriteAllText(args[2], JsonSerializer.Serialize(report, JsonOptions) + "\n");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Could not safely write MakeDir receipt: {error.Message}");
            return 1;
        }
        if (status != "passed") return 1;
        Console.WriteLine($"PASS {args[1]}: {original!.Observations.Count} original, {generated!.Observations.Count} generated native invocations; {comparisons.Count} vector comparisons; no full-OS or shipping claim.");
        return 0;
    }

    private static object Side(MakeDirFixture? fixture, string? path, string? hash) => new
    {
        inputPath = ReportPath(path),
        imageSha256 = hash,
        imageLoads = fixture?.Bus.ImageLoads ?? 0,
        loadedBytes = fixture?.Image.Code.Length ?? 0,
        nativeInvocationsStarted = fixture?.NativeInvocationsStarted ?? 0,
        nativeInvocationsPassed = fixture?.Observations.Count ?? 0,
        nativeReads = fixture?.Bus.NativeReads ?? 0,
        nativeWrites = fixture?.Bus.NativeWrites ?? 0,
        readsBySuppliedVectors = fixture?.Bus.GatewayReads ?? 0,
        lastFailure = fixture?.LastFailure,
        cases = fixture?.Observations.ToArray() ?? []
    };

    private static void ValidatePrivateReference(string path)
    {
        Require(File.Exists(path), "Private original MakeDir is missing; zero-case success is forbidden.");
        var root = FindRepositoryRoot();
        Require(root is not null, "Cannot establish the repository boundary for private reference validation.");
        var relative = Path.GetRelativePath(root!, Path.GetFullPath(path));
        Require(relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative),
            "The original command must remain outside the repository and its snapshots.");
    }

    private static string? FindRepositoryRoot()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(Path.GetFullPath(start)); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "CopperOS.Portable.props"))) return dir.FullName;
        }
        return null;
    }

    private static void ValidateReportPath(string report, string? generated, string? original, string? generatedHash)
    {
        var target = Path.GetFullPath(report);
        foreach (var input in new[] { generated, original })
            Require(input is null || !string.Equals(target, ReportPath(input), StringComparison.OrdinalIgnoreCase),
                "A report cannot overwrite an input HUNK.");
        // Content protection also covers existing hardlinks/path aliases. Read
        // each current input independently of setup progress: a bad reference
        // can fail before generatedHash is captured, but must not let a failure
        // receipt overwrite an aliased generated image (or invalid reference).
        if (File.Exists(target))
        {
            var prior = Hash(target);
            Require(prior != OriginalSha256 && (generatedHash is null || prior != generatedHash),
                "The report target contains a protected input image.");
            foreach (var input in new[] { generated, original })
                Require(input is null || !File.Exists(input) || prior != Hash(input),
                    "The report target aliases or contains an existing input image.");
        }
    }

    private static string? ReportPath(string? path)
    {
        if (path is null) return null;
        try { return Path.GetFullPath(path); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path; // Preserve malformed input text without suppressing the failure receipt.
        }
    }

    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
}
