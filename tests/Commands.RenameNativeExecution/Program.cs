using System.Security.Cryptography;
using System.Text.Json;
using Amiga;
using Copper68k;
using CopperOS.Commands.NativeExecution;
using static CopperOS.Commands.RenameNativeExecution.RenameTestBus;

namespace CopperOS.Commands.RenameNativeExecution;

internal static class Program
{
    internal const string OriginalSha256 = "ebff9d5f1401ca67fe9542bde304c603d21366f81852716c236c5c7ca1e49579";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static int Main(string[] args)
    {
        RenameFixture? fixture = null;
        string? originalHash = null;
        AuditFile[] sourcesBefore = [], sourcesAfter = [], binariesBefore = [], binariesAfter = [];
        var status = "failed";
        string? failure = null;
        var snapshotsUnchanged = false;
        try
        {
            Require(args.Length == 4,
                "usage: RenameNativeExecution <private-original.hunk> <68000|68020|68040> <report.json> <rename-classic-original-vector-fixture>");
            Require(args[3] == RenameFixture.SuiteId, "Unknown suite; no substitute suite is selected.");
            var model = args[1] switch
            {
                "68000" => M68kCpuModel.M68000,
                "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040,
                _ => throw new ArgumentException("Unsupported native CPU target.")
            };
            ValidatePrivateReference(args[0]);
            ValidateReportPath(args[2], args[0]);
            Require(new FileInfo(args[0]).Length == 1140, "Original must be the pinned 1140-byte Rename 37.2 member.");
            originalHash = Hash(args[0]);
            Require(originalHash == OriginalSha256, "Original Rename hash mismatch; zero-case success is forbidden.");
            sourcesBefore = SourceSnapshot();
            binariesBefore = BinarySnapshot();
            var image = HunkImage.Load(args[0], RenameFixture.LoadAddress);
            Require(image.Sha256 == originalHash && image.Code.Length == 1104, "Original changed or HUNK layout differs.");
            var batches = RenameCases.All();
            fixture = new(image, model);
            fixture.Run(batches);
            Require(fixture.Observations.Count == RenameCases.ExpectedReturned &&
                fixture.GuardStops.Count == RenameCases.ExpectedGuardStops &&
                fixture.NativeInvocationsStarted == RenameCases.ExpectedReturned + RenameCases.ExpectedGuardStops,
                "Not every declared original invocation or bounded guard experiment executed.");
            Require(Hash(args[0]) == originalHash, "Original input changed during execution.");
            sourcesAfter = SourceSnapshot();
            binariesAfter = BinarySnapshot();
            snapshotsUnchanged = sourcesBefore.SequenceEqual(sourcesAfter) && binariesBefore.SequenceEqual(binariesAfter);
            Require(snapshotsUnchanged, "Sources, linked inputs or executor/runtime binaries changed during the run.");
            status = "passed";
        }
        catch (Exception error)
        {
            failure = error.ToString();
            Console.Error.WriteLine($"FAIL original Rename supplied vectors: {error.Message}");
        }

        var report = new
        {
            schemaVersion = 1,
            status,
            scope = "original-only-supplied-vector-safe-subset-and-bounded-guard-observations",
            suite = args.ElementAtOrDefault(3),
            expectedSuite = RenameFixture.SuiteId,
            cpu = args.ElementAtOrDefault(1),
            failure,
            reference = new
            {
                path = ReportPath(args.ElementAtOrDefault(0)),
                version = "rename 37.2 (30.5.91)",
                expectedBytes = 1140,
                expectedSha256 = OriginalSha256,
                actualSha256 = originalHash,
                payloadCopiedToRepositoryOrReceipt = false
            },
            runtime = new
            {
                instructionExecutor = "Copper68k 1.4.0",
                executionMode = "Interpreter",
                timingSelection = "Public default for the declared CPU; no timing bypass or fallback.",
                hostRuntimeVersion = Environment.Version.ToString(),
                binariesBefore,
                binariesAfter
            },
            sourceAudit = new
            {
                sourceFilesBefore = sourcesBefore,
                sourceFilesAfter = sourcesAfter,
                sourceAndBinarySnapshotsUnchanged = snapshotsUnchanged,
                reproducibleBuildBindingEstablished = false,
                description = "Read-only source and actually loaded assembly-path hashes around this direct run; not a source-bound rebuild receipt."
            },
            evidence = new
            {
                originalCommandMachineCodeExecuted = (fixture?.NativeInvocationsStarted ?? 0) > 0,
                generatedCommandMachineCodeExecuted = false,
                realKickstartExecution = false,
                realCopperStartExecution = false,
                realCliLaunch = false,
                realDosParser = false,
                realDosMatcher = false,
                realFilesystemHandler = false,
                realDosFormatter = false,
                cycleTimingQualified = false,
                completeStdoutCaptured = false,
                workbenchLaunchQualified = false,
                minimumStackQualified = false,
                shippingOrPureApproval = false,
                originalMatchEndUnstartedOrRepeatedBehaviorQualified = false,
                inputObservation = "Exact template/NULL-D3 ABI and post-ReadArgs strings/switch are supplied; no command line is parsed.",
                matcherObservation = "Ordered supplied AnchorPath fields and an opaque owned search allocation; no real wildcard engine or original MatchEnd semantics.",
                filesystemObservation = "Exact Lock/Examine/SameLock/Rename inputs and supplied raw BPTR/results; no directory or file mutations.",
                outputObservation = "Three verified command-owned formats rendered only by a test vector; PrintFault requests are recorded without DOS fault rendering.",
                ioErrorObservation = "Selected error capture/requests are separate from final ambient IoErr, which cleanup vectors deliberately poison in most cases.",
                entryRegisterConvention = "NDK 3.1 STARTUP.ASM:264-267 permits image-entry registers except SP to change. D0 and SP are asserted on returned cases; other registers are recorded, and public-library ABI checks/volatile clobbers remain active.",
                guardObservation = "Expected stops prevent unqualified MatchEnd use, live-search anchor release and unchecked buffer accesses. Stopped commands do not count as returned, passed, resource-safe or equivalent to original DOS behavior."
            },
            expectedReturnedCases = RenameCases.ExpectedReturned,
            expectedGuardExperiments = RenameCases.ExpectedGuardStops,
            originalNativeInvocationsStarted = fixture?.NativeInvocationsStarted ?? 0,
            originalReturnedCasesAccepted = fixture?.Observations.Count ?? 0,
            originalExpectedGuardStopsObserved = fixture?.GuardStops.Count ?? 0,
            generatedNativeInvocations = 0,
            comparisonsPerformed = 0,
            imageLoads = fixture?.Bus.ImageLoads ?? 0,
            loadedCodeBytes = fixture?.Image.Code.Length ?? 0,
            nativeReads = fixture?.Bus.NativeReads ?? 0,
            nativeWrites = fixture?.Bus.NativeWrites ?? 0,
            readsBySuppliedVectors = fixture?.Bus.GatewayReads ?? 0,
            returnedCases = fixture?.Observations.ToArray() ?? [],
            guardStops = fixture?.GuardStops.ToArray() ?? [],
            lastFailure = fixture?.LastFailure
        };
        try
        {
            Require(args.Length >= 3, "No report path available for a failure receipt.");
            ValidateReportPath(args[2], args.ElementAtOrDefault(0));
            WriteReceipt(args[2], args.ElementAtOrDefault(0), JsonSerializer.Serialize(report, JsonOptions) + "\n");
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Could not safely write Rename receipt: {error.Message}");
            return 1;
        }
        if (status != "passed") return 1;
        Console.WriteLine($"PASS {args[1]} original-only safe subset: {fixture!.Observations.Count} returned cases; " +
            $"{fixture.GuardStops.Count} separately bounded guard observations; zero generated or full-OS claims.");
        return 0;
    }

    private static AuditFile[] SourceSnapshot()
    {
        var root = FindRepositoryRoot();
        var project = Path.Combine(root, "tests", "Commands.RenameNativeExecution");
        var paths = Directory.EnumerateFiles(project, "*.cs", SearchOption.TopDirectoryOnly)
            .Append(Path.Combine(project, "CopperOS.Commands.RenameNativeExecution.csproj"))
            .Append(Path.Combine(root, "tests", "Commands.NativeExecution", "HunkImage.cs"))
            .Append(Path.Combine(root, "CopperOS.Portable.props"));
        return paths.Order(StringComparer.Ordinal).Select(p => new AuditFile(Path.GetFullPath(p), Hash(p))).ToArray();
    }

    private static AuditFile[] BinarySnapshot() => new[]
    {
        typeof(Program).Assembly.Location,
        typeof(M68kCoreFactory).Assembly.Location,
        typeof(APTR).Assembly.Location
    }.Order(StringComparer.Ordinal).Select(p => new AuditFile(Path.GetFullPath(p), Hash(p))).ToArray();

    private static void ValidatePrivateReference(string path)
    {
        Require(File.Exists(path), "Private original Rename is missing; this is not a skipped reference test.");
        // Opening the existing files resolves directory junctions, symbolic
        // links and drive aliases. A lexical D: versus C: comparison is not
        // sufficient to keep the licensed reference outside this repository.
        var marker = PhysicalFilePath.Resolve(Path.Combine(FindRepositoryRoot(), "CopperOS.Portable.props"));
        var relative = Path.GetRelativePath(Path.GetDirectoryName(marker)!, PhysicalFilePath.Resolve(path));
        Require(relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative),
            "Original executable must remain outside the repository and its snapshots.");
    }

    private static string FindRepositoryRoot()
    {
        foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
            for (var directory = new DirectoryInfo(Path.GetFullPath(start)); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "CopperOS.Portable.props"))) return directory.FullName;
        throw new InvalidOperationException("Cannot establish the repository boundary.");
    }

    private static void ValidateReportPath(string report, string? original)
    {
        var target = Path.GetFullPath(report);
        Require(string.Equals(Path.GetExtension(target), ".json", StringComparison.OrdinalIgnoreCase), "Receipt must be a JSON path.");
        Require(original is null || !string.Equals(target, ReportPath(original), StringComparison.OrdinalIgnoreCase),
            "Report must not overwrite the original input.");
        if (File.Exists(target))
        {
            Require(original is null || !File.Exists(original) ||
                !string.Equals(PhysicalFilePath.Resolve(target), PhysicalFilePath.Resolve(original), StringComparison.OrdinalIgnoreCase),
                "Report resolves to the existing input through a filesystem alias.");
            var current = Hash(target);
            Require(current != OriginalSha256, "Report target contains protected original bytes.");
            Require(original is null || !File.Exists(original) || current != Hash(original),
                "Report target aliases or contains the existing input, even if reference validation failed.");
        }
    }

    private static void WriteReceipt(string report, string? original, string contents)
    {
        var target = Path.GetFullPath(report);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporary = target + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
                writer.Write(contents);
            ValidateReportPath(target, original);
            // Replace a directory entry rather than truncating an existing
            // file: even an unexpected hardlink cannot modify its other names.
            File.Move(temporary, target, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string? ReportPath(string? path)
    {
        if (path is null) return null;
        try { return Path.GetFullPath(path); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException) { return path; }
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    private sealed record AuditFile(string Path, string Sha256);
}
