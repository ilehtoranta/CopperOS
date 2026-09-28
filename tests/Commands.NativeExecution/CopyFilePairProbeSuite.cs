using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>Supplied-vector receipt for one Copy source/destination pair.</summary>
internal sealed record CopyFilePairProbeCase(bool SourceOpens, bool DestinationOpens,
    int[] Reads, int[] Writes, int Error = Invocation.InitialIoError,
    bool BreakBeforeRead = false, bool ValidBuffer = true);

internal sealed class CopyFilePairNativeLayout(uint control, uint sourceName,
    uint destinationName, uint buffer)
{
    public uint Control { get; } = control;
    public uint SourceName { get; } = sourceName;
    public uint DestinationName { get; } = destinationName;
    public uint Buffer { get; } = buffer;
    public int SourceOpens { get; set; }
    public int DestinationOpens { get; set; }
    public int Closes { get; set; }
    public int Signals { get; set; }
    public int Reads { get; set; }
    public int Writes { get; set; }
    public int Deletes { get; set; }
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 128;
}

internal sealed partial class ProbeFixture
{
    public const string CopyFilePairProbeSuite = "copy-file-pair-native-entry-vector-fixture";
    private const uint CopyFilePairCtrlCMask = 1u << 12, SourceHandle = 0x130, DestinationHandle = 0x140;

    private List<object> RunCopyFilePairProbeCases()
    {
        ProbeCase[] cases =
        [
            Case("stream-success", true, true, [8, 0], [8, 0]),
            Case("source-open-failure", false, true, [], [], 205),
            Case("destination-open-failure", true, false, [], [], 206),
            Case("read-failure", true, true, [-1], [], 207),
            Case("short-write", true, true, [8], [7], 208),
            Case("ctrl-c-before-read", true, true, [], [], (int)DOS.Error.Break, true),
            Case("invalid-buffer", true, true, [], [], (int)DOS.Error.BadTemplate, false, false),
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute(
        [
            cases[0] with { Name = "interleaved-success" },
            cases[3] with { Name = "interleaved-read-failure" },
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Case(string name, bool sourceOpens, bool destinationOpens,
        int[] reads, int[] writes, int error = Invocation.InitialIoError,
        bool breakBeforeRead = false, bool validBuffer = true) =>
        new(name, "", sourceOpens && destinationOpens && !breakBeforeRead &&
            reads.Length > 0 && reads[^1] == 0 && writes.SequenceEqual(reads)
                ? DOS.RETURN_OK : DOS.RETURN_FAIL,
            sourceOpens && destinationOpens && !breakBeforeRead &&
            reads.Length > 0 && reads[^1] == 0 && writes.SequenceEqual(reads)
                ? 0 : error, "")
        { EntryLength = 16, CopyFilePair = new(sourceOpens, destinationOpens, reads, writes, error, breakBeforeRead, validBuffer) };

    private void PrepareCopyFilePairProbe(Invocation invocation)
    {
        var definition = invocation.Definition.CopyFilePair!;
        var control = invocation.Arguments;
        Bus.Memory.AsSpan((int)control, 128).Clear();
        var layout = new CopyFilePairNativeLayout(control, control + 32, control + 48,
            control + 64);
        invocation.CopyFilePairLayout = layout;
        Encoding.Latin1.GetBytes("Work:Source").CopyTo(Bus.Memory.AsSpan((int)layout.SourceName));
        Bus.Memory[layout.SourceName + 11] = 0;
        Encoding.Latin1.GetBytes("RAM:Destination").CopyTo(Bus.Memory.AsSpan((int)layout.DestinationName));
        Bus.Memory[layout.DestinationName + 15] = 0;
        Bus.Long(control, layout.SourceName);
        Bus.Long(control + 4, layout.DestinationName);
        Bus.Long(control + 8, definition.ValidBuffer ? layout.Buffer : 0);
        Bus.Long(control + 12, 16);
    }

    private void VerifyCopyFilePairProbe(Invocation invocation)
    {
        var definition = invocation.Definition.CopyFilePair!;
        var layout = invocation.CopyFilePairLayout!;
        Require(layout.DestinationOpens == (definition.ValidBuffer ? 1 : 0) &&
            layout.SourceOpens == (definition.ValidBuffer && definition.DestinationOpens ? 1 : 0),
            "Copy file-pair Open order differs.");
        var expectedCloses = !definition.ValidBuffer || !definition.DestinationOpens ? 0 :
            definition.SourceOpens ? 2 : 1;
        Require(layout.Closes == expectedCloses, "Copy file-pair Close ownership differs.");
        Require(layout.Reads == (definition.ValidBuffer && definition.SourceOpens && definition.DestinationOpens && !definition.BreakBeforeRead ? definition.Reads.Length : 0) &&
            layout.Writes == (definition.ValidBuffer && definition.SourceOpens && definition.DestinationOpens && !definition.BreakBeforeRead && !definition.Reads.Contains(-1) ? definition.Writes.Length : 0),
            "Copy file-pair stream counts differ.");
        var transferFailed = definition.ValidBuffer && definition.SourceOpens &&
            definition.DestinationOpens && !(definition.Reads.Length > 0 &&
            definition.Reads[^1] == 0 && definition.Writes.SequenceEqual(definition.Reads)) &&
            !(!definition.BreakBeforeRead && definition.Reads.Length == 0);
        var sourceFailed = definition.ValidBuffer && definition.DestinationOpens && !definition.SourceOpens;
        Require(layout.Deletes == (transferFailed || sourceFailed ? 1 : 0),
            "Copy file-pair partial-destination cleanup differs.");
        invocation.CopyFilePairLayout = null;
    }

    private void RegisterCopyFilePairProbeExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
        {
            var d = invocation.Definition.CopyFilePair!; var l = invocation.CopyFilePairLayout!;
            Require(state.D[0] == 0 && state.D[1] == 0, "Copy file-pair signal ABI differs.");
            l.Signals++; return d.BreakBeforeRead && l.Signals == 1 ? CopyFilePairCtrlCMask : 0;
        });
    }

    private void RegisterCopyFilePairProbeDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.Open, "Open", (state, invocation) =>
        {
            var d = invocation.Definition.CopyFilePair!; var l = invocation.CopyFilePairLayout!;
            if (l.DestinationOpens == 0)
            {
                Require(Bus.CString(state.D[1]) == "RAM:Destination" && state.D[2] == (uint)DOS.FileMode.NewFile, "Copy destination Open ABI differs.");
                var opens = d.DestinationOpens;
                l.DestinationOpens++; invocation.IoError = d.Error; return opens ? DestinationHandle : 0;
            }
            Require(Bus.CString(state.D[1]) == "Work:Source" && state.D[2] == (uint)DOS.FileMode.OldFile, "Copy source Open ABI differs.");
            var sourceOpens = d.SourceOpens;
            l.SourceOpens++; invocation.IoError = d.Error; return sourceOpens ? SourceHandle : 0;
        });
        Register(baseAddress, DosLvo.Close, "Close", (state, invocation) =>
        {
            var l = invocation.CopyFilePairLayout!;
            var d = invocation.Definition.CopyFilePair!;
            Require(state.D[1] == (l.Closes == 0 && d.DestinationOpens ? DestinationHandle : SourceHandle), "Copy file-pair Close order differs.");
            l.Closes++; invocation.IoError = 902; return 1;
        });
        Register(baseAddress, DosLvo.Read, "Read", (state, invocation) =>
        {
            var d = invocation.Definition.CopyFilePair!; var l = invocation.CopyFilePairLayout!;
            Require(l.Reads < d.Reads.Length && state.D[1] == SourceHandle && state.D[2] == l.Buffer && state.D[3] == 16, "Copy file-pair Read ABI differs.");
            var count=d.Reads[l.Reads++]; if(count==-1) invocation.IoError=d.Error; return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            var d = invocation.Definition.CopyFilePair!; var l = invocation.CopyFilePairLayout!;
            Require(l.Writes < d.Writes.Length && state.D[1] == DestinationHandle && state.D[2] == l.Buffer && state.D[3] == unchecked((uint)d.Reads[l.Writes]), "Copy file-pair Write ABI differs.");
            var count=d.Writes[l.Writes++]; if(count != unchecked((int)state.D[3])) invocation.IoError=d.Error; return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.DeleteFile, "DeleteFile", (state, invocation) =>
        {
            var l = invocation.CopyFilePairLayout!;
            Require(l.Closes == (invocation.Definition.CopyFilePair!.SourceOpens ? 2 : 1) && Bus.CString(state.D[1]) == "RAM:Destination",
                "Copy partial-destination deletion ordering differs.");
            l.Deletes++; invocation.IoError = 901; return 1;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) => { invocation.IoError=unchecked((int)state.D[1]); return 0; });
    }
}
