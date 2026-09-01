using System.Buffers.Binary;
using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record ArgumentBoundaryCase(
    uint Selector, string? Template, uint ResultCount, uint? AllocationBytes);

internal sealed partial class ProbeFixture
{
    public const string StartupSuite = "command-startup-vector-fixture";
    public const string ArgumentBoundarySuite = "command-argument-boundary-vector-fixture";
    public const int ArgumentBoundaryInvocationCount = 25;

    private List<object> RunArgumentBoundaryCases()
    {
        var badTemplate = (int)DOS.Error.BadTemplate;
        var noFreeStore = (int)DOS.Error.NoFreeStore;
        var releasedError = (int)DOS.Error.ObjectInUse;
        ProbeCase[] cases =
        [
            Boundary("empty-zero-results", 0, "", 0, 4, DOS.RETURN_OK, releasedError),
            Boundary("empty-parser-failure", 0, "", 0, 4, DOS.RETURN_ERROR, 118) with { ParserError = 118 },
            Boundary("empty-allocation-failure", 0, "", 0, 4, DOS.RETURN_FAIL, noFreeStore) with { AllocationFailure = true },
            Boundary("null-template-one-slot", 1, null, 1, null, DOS.RETURN_ERROR, badTemplate),
            Boundary("null-template-zero-slots", 8, null, 0, null, DOS.RETURN_ERROR, badTemplate),
            Boundary("count-multiplication-overflow", 2, "VALUE/N", 0x40000000, null, DOS.RETURN_ERROR, badTemplate),
            Boundary("count-rounding-overflow", 3, "VALUE/N", 0x3fffffff, null, DOS.RETURN_ERROR, badTemplate),
            Boundary("maximum-safe-count-allocation-failure", 4, "VALUE/N", 0x3ffffffe, 0xfffffff8,
                DOS.RETURN_FAIL, noFreeStore) with { AllocationFailure = true },
            Boundary("single-slot-allocation-failure", 6, "VALUE/N", 1, 4,
                DOS.RETURN_FAIL, noFreeStore) with { AllocationFailure = true },
            Boundary("parser-bad-number", 5, "VALUE/N", 1, 4, DOS.RETURN_ERROR, 115) with { ParserError = 115 },
            Boundary("parser-too-many-arguments", 5, "VALUE/N", 1, 4, DOS.RETURN_ERROR, 118) with { ParserError = 118 },
            Boundary("success-current-error-and-release", 6, "VALUE/N", 1, 4,
                DOS.RETURN_OK, releasedError, 42, 34567),
            Boundary("success-numeric-zero", 6, "VALUE/N", 1, 4, DOS.RETURN_OK, releasedError, 0),
            Boundary("success-minimum-number", 6, "VALUE/N", 1, 4, DOS.RETURN_OK, releasedError, int.MinValue),
            Boundary("default-lease-release", 7, null, 0, null, DOS.RETURN_OK, releasedError)
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        for (var index = 0; index < 2; index++)
        {
            reports.AddRange(Execute([cases[9] with { Name = $"cc04.arguments/repeat-{index}-failure" }], false));
            reports.AddRange(Execute([cases[11] with { Name = $"cc04.arguments/repeat-{index}-success" }], false));
        }
        reports.AddRange(Execute([
            cases[0] with { Name = "cc04.arguments/interleaved-empty-4k", StackBytes = 4096 },
            cases[11] with { Name = "cc04.arguments/interleaved-number-16k", StackBytes = 16384 }
        ], true));
        reports.AddRange(Execute([
            cases[7] with { Name = "cc04.arguments/interleaved-maximum-allocation-failure-4k", StackBytes = 4096 },
            cases[12] with { Name = "cc04.arguments/interleaved-zero-success-16k", StackBytes = 16384 }
        ], true));
        reports.AddRange(Execute([
            cases[9] with { Name = "cc04.arguments/interleaved-parser-failure-4k", StackBytes = 4096 },
            cases[14] with { Name = "cc04.arguments/interleaved-default-16k", StackBytes = 16384 }
        ], true));
        Require(reports.Count == ArgumentBoundaryInvocationCount, "Argument-boundary case inventory changed without updating its contract.");
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Boundary(string name, uint selector, string? template,
        uint resultCount, uint? allocationBytes, int result, int error,
        int? number = null, int? parserSuccessError = null)
    {
        Span<byte> launch = stackalloc byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(launch, 0x43433034); // Private CC04 protocol, not DOS input.
        BinaryPrimitives.WriteUInt32BigEndian(launch[4..], selector);
        BinaryPrimitives.WriteInt32BigEndian(launch[8..], number ?? 0);
        BinaryPrimitives.WriteInt32BigEndian(launch[12..], parserSuccessError ?? Invocation.InitialIoError);
        return new ProbeCase($"cc04.arguments/{name}", Encoding.Latin1.GetString(launch), result, error, "")
        {
            ArgumentBoundary = new ArgumentBoundaryCase(selector, template, resultCount, allocationBytes),
            Number = number,
            ParserSuccessError = parserSuccessError
        };
    }

    private static void VerifyArgumentBoundary(Invocation invocation)
    {
        var test = invocation.Definition;
        var boundary = test.ArgumentBoundary ?? throw new InvalidOperationException("Missing argument-boundary contract.");
        var allocated = boundary.AllocationBytes is not null;
        var parsed = allocated && !test.AllocationFailure;
        var succeeded = parsed && test.ParserError == 0;
        Require(invocation.Allocations == (allocated ? 1 : 0), "Unexpected argument result allocation count.");
        Require(invocation.Reads == (parsed ? 1 : 0), "Unexpected boundary ReadArgs count.");
        Require(invocation.FreeArgs == (succeeded ? 1 : 0), "Unexpected boundary FreeArgs count.");
        Require(invocation.FreeMem == (parsed ? 1 : 0), "Unexpected boundary FreeMem count.");
        Require(invocation.AllocationRequests.SequenceEqual(allocated ? [boundary.AllocationBytes!.Value] : []),
            "Argument result allocation request was truncated or rounded before Exec.");

        // Exact gateway ordering also detects hidden vector calls on failed,
        // default, or repeated Release. Cleanup deliberately poisons IoErr.
        List<string> expected = ["FindTask", "OpenLibrary"];
        if (boundary.Selector == 7)
            expected.AddRange(["SetIoErr", "IoErr", "SetIoErr", "CloseLibrary"]);
        else if (succeeded)
            expected.AddRange([
                "AllocMem", "ReadArgs", "IoErr", "SetIoErr",
                "IoErr", "FreeArgs", "FreeMem", "SetIoErr", "IoErr",
                "SetIoErr", "IoErr", "SetIoErr", "CloseLibrary"
            ]);
        else
        {
            if (allocated) expected.Add("AllocMem");
            if (parsed) expected.AddRange(["ReadArgs", "IoErr", "FreeMem"]);
            expected.AddRange(["SetIoErr", "IoErr", "SetIoErr", "IoErr", "SetIoErr", "CloseLibrary"]);
        }
        Require(invocation.Events.SequenceEqual(expected),
            $"{test.Name}: unexpected argument lifetime vector sequence: {string.Join(",", invocation.Events)}.");
    }
}
