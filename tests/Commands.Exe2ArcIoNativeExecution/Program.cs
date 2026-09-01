using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Copper68k;
using CopperOS.Commands.NativeExecution;

namespace CopperOS.Commands.Exe2ArcIoNativeExecution;

internal static class Program
{
    private const string PinnedRuntimeSha = "8046d9a2083c198647a02d9146b735629890d9a2e14bc313968a3b48139a2cd5";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Main(string[] args)
    {
        IoFixture? fixture = null;
        string? imageHash = null;
        long imageBytes = 0;
        AuditFile[] sourcesBefore = [], sourcesAfter = [], binariesBefore = [], binariesAfter = [];
        string status = "failed";
        string? failure = null;
        bool unchanged = false;
        try
        {
            IoTestBus.Require(args.Length == 4,
                "usage: Exe2ArcIoNativeExecution <generated.hunk> <68000|68020|68040> <new-report.json> <exe2arc-rar4-cab-dos-components>");
            IoTestBus.Require(args[3] == IoCases.Suite, "Unknown suite; no fallback or skipped cases.");
            var model = args[1] switch
            {
                "68000" => M68kCpuModel.M68000,
                "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040,
                _ => throw new ArgumentException("Unsupported CPU target."),
            };
            ValidateNewReportPath(args[2]);
            IoTestBus.Require(Hash(typeof(M68kCoreFactory).Assembly.Location) == PinnedRuntimeSha,
                "The actually loaded Copper68k DLL is not the pinned 1.4.0 fixture runtime.");
            imageHash = Hash(args[0]);
            imageBytes = new FileInfo(args[0]).Length;
            sourcesBefore = SourceSnapshot();
            binariesBefore = BinarySnapshot();
            var image = HunkImage.Load(args[0], IoFixture.LoadAddress);
            IoTestBus.Require(image.Sha256 == imageHash, "HUNK changed during admission.");
            fixture = new(image, model);
            fixture.Run();
            IoTestBus.Require(Hash(args[0]) == imageHash, "Input HUNK changed during execution.");
            sourcesAfter = SourceSnapshot();
            binariesAfter = BinarySnapshot();
            unchanged = sourcesBefore.SequenceEqual(sourcesAfter) && binariesBefore.SequenceEqual(binariesAfter);
            IoTestBus.Require(unchanged, "Raw source or loaded executable/runtime bytes changed during execution.");
            status = "passed";
        }
        catch (Exception error)
        {
            failure = error.ToString();
            Console.Error.WriteLine("FAIL Exe2Arc native DOS components: " + error.Message);
        }
        var report = new
        {
            schemaVersion = 1, status, suite = args.ElementAtOrDefault(3), expectedSuite = IoCases.Suite,
            scope = "generated-RAR4-CAB-scanning-copy-with-supplied-DOS-vectors",
            cpu = args.ElementAtOrDefault(1), inputHunk = args.ElementAtOrDefault(0), imageSha256 = imageHash,
            imageBytes, failure, lastFailure = fixture?.LastFailure, lastCase = fixture?.ActiveCase,
            expectedNativeInvocations = IoCases.ExpectedInvocations,
            nativeInvocationsStarted = fixture?.NativeInvocationsStarted ?? 0,
            nativeInvocationsPassed = fixture?.Returned.Count ?? 0,
            originalInvocations = 0, commandInvocations = 0, comparisons = 0,
            imageLoads = fixture is null ? 0 : 1,
            runtime = new
            {
                instructionExecutor = "Copper68k 1.4.0", pinnedRuntimeSha256 = PinnedRuntimeSha,
                hostRuntimeVersion = Environment.Version.ToString(),
                executionSelection = "Public default interpreter and CPU profile; no timing/core fallback.",
                instructionAddressBits = args.ElementAtOrDefault(1) == "68000" ? 24 : 32,
                buffersBelowPhysical24BitLimit = true,
                unsignedWrapAdmissionCaseIsNotPhysicalMappingProof = true,
                binariesBefore, binariesAfter,
            },
            sourceAudit = new
            {
                sourcesBefore, sourcesAfter, sourceAndBinarySnapshotsUnchanged = unchanged,
                reproducibleBuildBindingEstablished = false,
                description = "Raw source and actually loaded executor/runtime hashes around this direct run; a separate compilation receipt must bind source, managed root, compiler, SDK and HUNK.",
            },
            evidence = new
            {
                publicDosAdapterExecuted = status == "passed", suppliedDosVectors = true,
                exactScriptAndOutputCompared = status == "passed", libraryVolatileRegistersClobbered = true,
                entryOnlyRequiresStackPreservation = true, sharedImageWritesPermitted = false,
                inputOwnershipRemainsCaller = true, outputOwnershipRemainsCaller = true,
                bufferOwnershipRemainsCaller = true, allowedAcquisitionOrCloseGateways = 0,
                realKickstartExecution = false, realCopperStartExecution = false,
                realDosParser = false, realFilesystemHandler = false,
                originalExe2ArcExecution = false, sourceToOriginalPackedBinaryParity = false,
                fullCommand = false, archiveIntegrityQualified = false,
                cycleTimingQualified = false, pureFlagOrShippingQualified = false,
            },
            returned = fixture?.Returned ?? [],
        };
        if (args.Length >= 3)
        {
            try { WriteNewReport(args[2], JsonSerializer.Serialize(report, JsonOptions)); }
            catch (Exception error)
            {
                Console.Error.WriteLine("Report was not written: " + error.Message);
                return 1;
            }
        }
        if (status != "passed") return 1;
        Console.WriteLine($"PASS {args[1]}: {fixture!.Returned.Count} native Exe2Arc DOS component invocations; one shared image.");
        return 0;
    }

    private static AuditFile[] SourceSnapshot()
    {
        var root = FindRoot();
        var executor = Path.Combine(root, "tests", "Commands.Exe2ArcIoNativeExecution");
        var probe = Path.Combine(root, "tests", "Commands.Exe2ArcIoNativeRoot");
        var files = Directory.EnumerateFiles(executor, "*.cs", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(probe, "*.cs", SearchOption.TopDirectoryOnly))
            .Append(Path.Combine(executor, "CopperOS.Commands.Exe2ArcIoNativeExecution.csproj"))
            .Append(Path.Combine(probe, "CopperOS.Commands.Exe2ArcIoNativeRoot.csproj"))
            .Append(Path.Combine(root, "tests", "Commands.NativeExecution", "HunkImage.cs"))
            .Append(Path.Combine(root, "src", "Commands", "Exe2ArcHeaderProbe.cs"))
            .Append(Path.Combine(root, "src", "Commands", "Exe2ArcIo.cs"))
            .Append(Path.Combine(root, "src", "Commands", "Exe2ArcForwardScanner.cs"))
            .Append(Path.Combine(root, "src", "Commands", "Exe2ArcPayloadCopy.cs"))
            .Append(Path.Combine(root, "src", "Commands", "Native", "NativeExe2ArcIo.cs"))
            .Append(Path.Combine(root, "CopperOS.Portable.props"));
        return files.Order(StringComparer.Ordinal).Select(p => new AuditFile(Path.GetFullPath(p), Hash(p))).ToArray();
    }
    private static AuditFile[] BinarySnapshot() => new[]
    {
        typeof(Program).Assembly.Location, typeof(M68kCoreFactory).Assembly.Location,
        System.Reflection.Assembly.Load("CopperFloat").Location,
        Path.ChangeExtension(typeof(Program).Assembly.Location, ".deps.json"),
        Path.ChangeExtension(typeof(Program).Assembly.Location, ".runtimeconfig.json"),
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
        IoTestBus.Require(string.Equals(Path.GetExtension(full), ".json", StringComparison.OrdinalIgnoreCase),
            "Report must be a new JSON file.");
        IoTestBus.Require(!Path.Exists(full), "Report path already exists; use a new receipt path.");
    }
    private static void WriteNewReport(string path, string json)
    {
        ValidateNewReportPath(path);
        var full = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(full)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".exe2arc-io-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                output.Write(Encoding.UTF8.GetBytes(json));
                output.Flush(true);
            }
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
