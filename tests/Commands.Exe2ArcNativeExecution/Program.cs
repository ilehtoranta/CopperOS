using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.Exe2ArcNativeExecution;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Main(string[] args)
    {
        HeaderFixture? fixture = null;
        string? imageHash = null;
        long imageBytes = 0;
        AuditFile[] sourcesBefore = [], sourcesAfter = [], binariesBefore = [], binariesAfter = [];
        string status = "failed";
        string? failure = null;
        bool unchanged = false;
        try
        {
            HeaderTestBus.Require(args.Length == 4,
                "usage: Exe2ArcNativeExecution <generated.hunk> <68000|68020|68040> <new-report.json> <exe2arc-header-components>");
            HeaderTestBus.Require(args[3] == HeaderCases.Suite, "Unknown suite; no fallback or skipped cases.");
            var model = args[1] switch
            {
                "68000" => M68kCpuModel.M68000,
                "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040,
                _ => throw new ArgumentException("Unsupported CPU target.")
            };
            ValidateNewReportPath(args[2]);
            imageHash = Hash(args[0]);
            imageBytes = new FileInfo(args[0]).Length;
            sourcesBefore = SourceSnapshot();
            binariesBefore = BinarySnapshot();
            var image = HunkImage.Load(args[0], HeaderFixture.LoadAddress);
            HeaderTestBus.Require(image.Sha256 == imageHash, "HUNK changed during admission.");
            fixture = new(image, model);
            fixture.Run();
            HeaderTestBus.Require(Hash(args[0]) == imageHash, "Input HUNK changed during execution.");
            sourcesAfter = SourceSnapshot();
            binariesAfter = BinarySnapshot();
            unchanged = sourcesBefore.SequenceEqual(sourcesAfter) && binariesBefore.SequenceEqual(binariesAfter);
            HeaderTestBus.Require(unchanged, "Source or actually loaded binary bytes changed during execution.");
            status = "passed";
        }
        catch (Exception error)
        {
            failure = error.ToString();
            Console.Error.WriteLine($"FAIL Exe2Arc native header component: {error.Message}");
        }

        var report = new
        {
            schemaVersion = 1, status, suite = args.ElementAtOrDefault(3), expectedSuite = HeaderCases.Suite,
            scope = "generated-one-candidate-RAR4-CAB-components", cpu = args.ElementAtOrDefault(1),
            inputHunk = args.ElementAtOrDefault(0), imageSha256 = imageHash, imageBytes, failure,
            lastCase = fixture?.ActiveCase, expectedNativeInvocations = HeaderCases.ExpectedInvocations,
            nativeInvocationsStarted = fixture?.NativeInvocationsStarted ?? 0,
            nativeInvocationsPassed = fixture?.Returned.Count ?? 0, originalInvocations = 0, comparisons = 0,
            imageLoads = fixture is null ? 0 : 1,
            runtime = new
            {
                instructionExecutor = "Copper68k 1.4.0", hostRuntimeVersion = Environment.Version.ToString(),
                executionSelection = "Public default interpreter and CPU timing profile; no timing or core fallback.",
                instructionAddressBits = args.ElementAtOrDefault(1) == "68000" ? 24 : 32,
                binariesBefore, binariesAfter
            },
            sourceAudit = new
            {
                sourcesBefore, sourcesAfter, sourceAndBinarySnapshotsUnchanged = unchanged,
                reproducibleBuildBindingEstablished = false,
                description = "Read-only raw source and loaded executor/runtime hashes around this direct run. The native compilation receipt must separately bind source, compiler, SDK and HUNK."
            },
            evidence = new
            {
                headerFieldsObservedByActualByteReads = status == "passed",
                hostGateways = 0, sharedImageWritesPermitted = false, borrowedHeaderWritesPermitted = false,
                realKickstartExecution = false, realCopperStartExecution = false, realDosParser = false,
                originalExe2ArcExecution = false, sourceToOriginalPackedBinaryParity = false,
                fullScannerOrCommand = false, fileIoOrPayloadCopy = false, archiveIntegrityQualified = false,
                cycleTimingQualified = false, shippingOrPureApproval = false, minimumStackQualified = false,
                boundaryDescription = "Successful last-byte reads end at $00FFFFFF on 68000 and $FFFFFFFF on 68020/040. Logical uint overflow rejection runs on all CPUs before mapping or reads. No $FFFFFFFF physical mapping is claimed for 68000.",
                entryAbi = "D0 result and restored SP are required. Other whole-image entry registers are recorded without imposing library-export preservation.",
                sourcePredicateLimits = "CAB length1/table0 may match; no CAB minimum/reserved-field validation is invented. Offset zero rejects one candidate only and makes no scanner-continuation decision."
            },
            returned = fixture?.Returned.ToArray() ?? []
        };
        try
        {
            HeaderTestBus.Require(args.Length >= 3, "No report path supplied.");
            WriteNewReport(args[2], JsonSerializer.Serialize(report, JsonOptions) + "\n");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Could not safely publish component receipt: {error.Message}");
            return 1;
        }
        if (status != "passed") return 1;
        Console.WriteLine($"PASS {args[1]}: {fixture!.Returned.Count} native Exe2Arc header component invocations; one shared image, no gateways.");
        return 0;
    }

    private static AuditFile[] SourceSnapshot()
    {
        var root = FindRoot();
        var executor = Path.Combine(root, "tests", "Commands.Exe2ArcNativeExecution");
        var probe = Path.Combine(root, "tests", "Commands.Exe2ArcNativeRoot");
        var files = Directory.EnumerateFiles(executor, "*.cs", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(probe, "*.cs", SearchOption.TopDirectoryOnly))
            .Append(Path.Combine(executor, "CopperOS.Commands.Exe2ArcNativeExecution.csproj"))
            .Append(Path.Combine(probe, "CopperOS.Commands.Exe2ArcNativeRoot.csproj"))
            .Append(Path.Combine(root, "tests", "Commands.NativeExecution", "HunkImage.cs"))
            .Append(Path.Combine(root, "src", "Commands", "Exe2Arc", "Exe2ArcHeaderProbe.cs"))
            .Append(Path.Combine(root, "CopperOS.Portable.props"));
        return files.Order(StringComparer.Ordinal).Select(p => new AuditFile(Path.GetFullPath(p), Hash(p))).ToArray();
    }

    private static AuditFile[] BinarySnapshot() => new[]
    {
        typeof(Program).Assembly.Location,
        typeof(M68kCoreFactory).Assembly.Location,
        Path.ChangeExtension(typeof(Program).Assembly.Location, ".deps.json"),
        Path.ChangeExtension(typeof(Program).Assembly.Location, ".runtimeconfig.json")
    }.Order(StringComparer.Ordinal).Select(p => new AuditFile(Path.GetFullPath(p), Hash(p))).ToArray();

    private static string FindRoot()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
            for (var directory = new DirectoryInfo(Path.GetFullPath(start)); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "CopperOS.Portable.props"))) return directory.FullName;
        throw new InvalidOperationException("Cannot locate the source audit root.");
    }

    private static void ValidateNewReportPath(string path)
    {
        var full = Path.GetFullPath(path);
        HeaderTestBus.Require(string.Equals(Path.GetExtension(full), ".json", StringComparison.OrdinalIgnoreCase),
            "Report must be a new JSON file.");
        // Do not truncate any existing directory entry, including input aliases,
        // hardlinks or symlinks, even when admission failed before the HUNK hash.
        HeaderTestBus.Require(!Path.Exists(full), "Report path already exists; use a new receipt path.");
    }

    private static void WriteNewReport(string path, string json)
    {
        ValidateNewReportPath(path);
        var full = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(full)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".exe2arc-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                output.Write(bytes);
                output.Flush(true);
            }
            // No overwrite: a path created or aliased since admission fails
            // closed without replacing its content. Publication is atomic.
            File.Move(temporary, full, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private sealed record AuditFile(string Path, string Sha256);
}
