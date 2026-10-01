using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Supplied-vector receipt for the bounded Copy streaming core. This is not a
/// Copy command parser, matcher, metadata, or directory-traversal fixture.
/// </summary>
internal sealed record CopyLoopProbeCase(int BufferSize, int[] Reads, int[] Writes,
    int Error = Invocation.InitialIoError, bool BreakBeforeRead = false);

internal sealed class CopyLoopNativeLayout(uint buffer)
{
    public uint Buffer { get; } = buffer;
    public int SignalCalls { get; set; }
    public int ReadCalls { get; set; }
    public int WriteCalls { get; set; }
}

internal sealed partial class ProbeFixture
{
    public const string CopyLoopProbeSuite = "copyloop-native-entry-vector-fixture";
    public const string JoinAppendLoopProbeSuite = "join-append-loop-native-entry-vector-fixture";
    private const uint CopyLoopCtrlCMask = 1u << 12;
    private const uint CopyLoopFrom = 0x130;
    private const uint CopyLoopTo = 0x140;

    private List<object> RunCopyLoopProbeCases()
    {
        ProbeCase[] cases =
        [
            Case("empty-input-writes-eof", 16, [0], [0]),
            Case("two-chunks-and-eof", 16, [16, 3, 0], [16, 3, 0]),
            Case("read-failure", 16, [-1], [], 205),
            Case("short-write", 16, [8], [7], 221),
            Case("write-failure", 16, [8], [-1], 222),
            Case("ctrl-c-before-read", 16, [], [], (int)DOS.Error.Break, true),
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute(
        [
            cases[1] with { Name = "interleaved-chunks" },
            cases[3] with { Name = "interleaved-short-write" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Case(string name, int bufferSize, int[] reads, int[] writes,
        int error = Invocation.InitialIoError, bool breakBeforeRead = false) =>
        new(name, "", breakBeforeRead ? DOS.RETURN_FAIL :
            reads.Length > 0 && reads[^1] == 0 && writes.Length == reads.Length &&
            writes.SequenceEqual(reads) ? DOS.RETURN_OK : DOS.RETURN_FAIL,
            breakBeforeRead ? (int)DOS.Error.Break :
            reads.Length > 0 && reads[^1] == 0 && writes.Length == reads.Length &&
            writes.SequenceEqual(reads) ? 0 : error, "")
        {
            EntryLength = 16,
            CopyLoop = new(bufferSize, reads, writes, error, breakBeforeRead)
        };

    private void PrepareCopyLoopProbe(Invocation invocation)
    {
        var definition = invocation.Definition.CopyLoop ??
            throw new InvalidOperationException("Missing Copy loop definition.");
        Require(definition.BufferSize > 0 && definition.BufferSize <= 64,
            "Copy loop fixture buffer is outside its bounded range.");
        var layout = new CopyLoopNativeLayout(invocation.Arguments + 32);
        invocation.CopyLoopLayout = layout;
        Bus.Long(invocation.Arguments, CopyLoopFrom);
        Bus.Long(invocation.Arguments + 4, CopyLoopTo);
        Bus.Long(invocation.Arguments + 8, layout.Buffer);
        Bus.Long(invocation.Arguments + 12, unchecked((uint)definition.BufferSize));
    }

    private void VerifyCopyLoopProbe(Invocation invocation)
    {
        var definition = invocation.Definition.CopyLoop ??
            throw new InvalidOperationException("Missing Copy loop definition.");
        var layout = invocation.CopyLoopLayout ??
            throw new InvalidOperationException("Missing Copy loop storage.");
        var expectedSignals = definition.BreakBeforeRead ? 1 : definition.Reads.Length;
        var expectedWrites = definition.BreakBeforeRead || definition.Reads.Contains(-1)
            ? 0 : definition.Writes.Length;
        Require(layout.SignalCalls == expectedSignals && layout.ReadCalls ==
            (definition.BreakBeforeRead ? 0 : definition.Reads.Length) &&
            layout.WriteCalls == expectedWrites,
            "Copy loop transfer or signal count differs from the supplied stream contract.");
        var observesError = definition.Reads.Contains(-1) ||
            definition.Writes.Where((value, index) => value != definition.Reads[index]).Any();
        Require(invocation.Events.Count(item => item == "IoErr") == (observesError ? 1 : 0),
            "Copy loop error observation differs from the supplied stream contract.");
        invocation.CopyLoopLayout = null;
    }

    private void RegisterCopyLoopProbeExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
        {
            var definition = invocation.Definition.CopyLoop ??
                throw new InvalidOperationException("Copy loop SetSignal without a definition.");
            var layout = invocation.CopyLoopLayout ??
                throw new InvalidOperationException("Copy loop SetSignal without storage.");
            Require(state.D[0] == 0 && state.D[1] == 0,
                "Copy loop must observe Ctrl-C without changing task signals.");
            layout.SignalCalls++;
            return definition.BreakBeforeRead && layout.SignalCalls == 1 ? CopyLoopCtrlCMask : 0;
        });
    }

    private void RegisterCopyLoopProbeDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.Read, "Read", (state, invocation) =>
        {
            var definition = invocation.Definition.CopyLoop!;
            var layout = invocation.CopyLoopLayout!;
            Require(layout.ReadCalls < definition.Reads.Length && state.D[1] == CopyLoopFrom &&
                state.D[2] == layout.Buffer && unchecked((int)state.D[3]) == definition.BufferSize,
                "Copy loop Read ABI differs from the supplied stream contract.");
            var count = definition.Reads[layout.ReadCalls];
            Require(count == -1 || count >= 0 && count <= definition.BufferSize,
                "Copy loop supplied Read count is invalid.");
            if (count > 0)
                for (var index = 0; index < count; index++)
                    Bus.Memory[layout.Buffer + (uint)index] = (byte)(0x40 + layout.ReadCalls + index);
            layout.ReadCalls++;
            if (count == -1) invocation.IoError = definition.Error;
            return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            var definition = invocation.Definition.CopyLoop!;
            var layout = invocation.CopyLoopLayout!;
            Require(layout.WriteCalls < definition.Writes.Length && state.D[1] == CopyLoopTo &&
                state.D[2] == layout.Buffer, "Copy loop Write handle or buffer differs.");
            var expectedRead = definition.Reads[layout.WriteCalls];
            var result = definition.Writes[layout.WriteCalls];
            Require(expectedRead >= 0 && unchecked((int)state.D[3]) == expectedRead,
                "Copy loop Write size differs from its preceding Read.");
            Require(result == -1 || result >= 0 && result <= expectedRead,
                "Copy loop supplied Write result is invalid.");
            if (expectedRead > 0)
                for (var index = 0; index < expectedRead; index++)
                    Require(Bus.Memory[layout.Buffer + (uint)index] ==
                        (byte)(0x40 + layout.WriteCalls + index),
                        "Copy loop Write did not receive the preceding Read bytes.");
            layout.WriteCalls++;
            if (result != expectedRead) invocation.IoError = definition.Error;
            return unchecked((uint)result);
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }
}
