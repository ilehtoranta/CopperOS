using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Supplied post-ReadArgs and post-MatchFirst vectors for the bounded MorphOS
/// Type text entry. This exercises resident code through the DOS/Exec ABI; it
/// is neither a DOS parser/wildcard implementation nor original Type parity.
/// </summary>
internal sealed record TypeNativeEntryCase(string Source, bool Number,
    bool NoLine, string Output, string? To = null, bool Hex = false,
    bool MatchFound = true, string? SecondSource = null, int? WriteChunk = null,
    string? Option = null, int OpenError = 0, int OutputOpenError = 0,
    bool BreakOnHexPoll = false, ushort DosVersion = 50,
    ushort DosRevision = 67, int ReadError = 0, int WriteError = 0,
    int WriteErrorAfterCalls = 0);

internal sealed class TypeNativeEntryLayout(uint rdArgs)
{
    public uint RdArgs { get; } = rdArgs;
    public int ReadOffset { get; set; }
    public int ReadCalls { get; set; }
    public int WriteCalls { get; set; }
    public bool MatchLive { get; set; }
    public int ActiveSourceIndex { get; set; }
    public int SignalCalls { get; set; }
}

internal sealed partial class ProbeFixture
{
    public const string TypeNativeEntrySuite = "type-native-entry-vector-fixture";
    public const string Workbench31TypeNativeEntrySuite =
        "workbench31-type-native-entry-vector-fixture";

    public static bool IsTypeNativeEntrySuite(string value) =>
        value == TypeNativeEntrySuite || value == Workbench31TypeNativeEntrySuite;

    public static bool IsWorkbench31TypeNativeEntrySuite(string value) =>
        value == Workbench31TypeNativeEntrySuite;
    private const uint TypeWorkspaceBytes = 17176;
    private const uint TypeRdArgsBytes = 256;
    private const uint TypeInputHandle = 0x712;
    private const uint TypeOutputHandle = 0x745;

    private List<object> RunTypeNativeEntryCases()
    {
        ProbeCase[] cases =
        [
            TypeEntry("text", "alpha", false, false, "alpha\n"),
            TypeEntry("numbered", "a\nb", true, false, "    1 a\n    2 b\n"),
            TypeEntry("ignored-opt", "a", false, false,
                "Option 'x' ignored\na\n", option: "x"),
            TypeEntry("multiple", "a", false, false, "a\nb\n", secondSource: "b"),
            TypeEntry("noline-to", "alpha", false, true, "alpha", "RAM:out"),
            new ProbeCase("no-match", "", DOS.RETURN_ERROR, 0,
                "TYPE: can't open SRC:input\n")
            {
                TypeEntry = new TypeNativeEntryCase("", false, false, "", MatchFound: false)
            },
            new ProbeCase("input-open-failure", "", DOS.RETURN_ERROR, 205,
                "TYPE: can't open SRC:input\n")
            {
                TypeEntry = new TypeNativeEntryCase("", false, false, "",
                    OpenError: 205)
            },
            new ProbeCase("to-open-failure", "", DOS.RETURN_ERROR, 205, "")
            {
                TypeEntry = new TypeNativeEntryCase("", false, false, "",
                    To: "RAM:out", OutputOpenError: 205)
            },
            new ProbeCase("hex-break", "", DOS.RETURN_ERROR,
                (int)DOS.Error.Break, FullHexRows(15))
            {
                TypeEntry = new TypeNativeEntryCase(new string('A', 240), false,
                    false, "", Hex: true, BreakOnHexPoll: true)
            },
            new ProbeCase("hex-partial-row", "", DOS.RETURN_OK, 0,
                "0000: 00207F" + new string(' ', 30) + ". .\n\n")
            {
                TypeEntry = new TypeNativeEntryCase("\0 \x7f", false, false,
                    "", Hex: true, WriteChunk: 5)
            },
            new ProbeCase("hex-number-warning", "", DOS.RETURN_WARN, 0,
                "Type can't do both HEX and NUMBER\n")
            {
                TypeEntry = new TypeNativeEntryCase("", true, false, "",
                    Hex: true)
            },
            new ProbeCase("text-read-error", "", DOS.RETURN_ERROR, 205,
                "TYPE: can't open SRC:input\n")
            {
                TypeEntry = new TypeNativeEntryCase("alpha", false, false,
                    "", ReadError: 205)
            },
            new ProbeCase("text-write-error", "", DOS.RETURN_ERROR, 214,
                "alTYPE: can't open SRC:input\n")
            {
                TypeEntry = new TypeNativeEntryCase("alpha", false, false,
                    "", WriteChunk: 2, WriteError: 214,
                    WriteErrorAfterCalls: 1)
            }
        ];
        if (suite == TypeNativeEntrySuite)
        {
            cases =
            [
                .. cases,
                new ProbeCase("literal-softlink-provider-too-old", "",
                    DOS.RETURN_ERROR, (int)DOS.Error.NotImplemented, "")
                {
                    TypeEntry = new TypeNativeEntryCase("", false, false,
                        "", DosVersion: 50, DosRevision: 66)
                }
            ];
        }
        if (IsWorkbench31TypeNativeEntrySuite(suite))
        {
            cases =
            [
                .. cases,
                new("workbench-startup", "", DOS.RETURN_ERROR,
                    (int)DOS.Error.ObjectWrongType, "")
                {
                    TypeEntry = new TypeNativeEntryCase("", false, false, ""),
                    Workbench = true
                },
                new("missing-dos", "", DOS.RETURN_FAIL,
                    Invocation.InitialIoError, "")
                {
                    TypeEntry = new TypeNativeEntryCase("", false, false, ""),
                    MissingDos = true
                }
            ];
        }
        if (IsWorkbench31TypeNativeEntrySuite(suite))
        {
            cases = cases.Select(test => test.TypeEntry is { } entry
                ? test with
                {
                    Output = entry.NoLine ? entry.Output + "\n" : test.Output,
                    TypeEntry = entry with { NoLine = false }
                }
                : test).ToArray();
        }
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "interleaved-text", StackBytes = 4096 },
            cases[1] with { Name = "interleaved-numbered" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase TypeEntry(string name, string source, bool number,
        bool noLine, string output, string? to = null, string? secondSource = null,
        string? option = null) =>
        new(name, "", DOS.RETURN_OK, 0, output)
        {
            TypeEntry = new TypeNativeEntryCase(source, number, noLine, output, to,
                SecondSource: secondSource, Option: option)
        };

    private void PrepareTypeNativeEntry(Invocation invocation)
    {
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
            return;
        var definition = invocation.Definition.TypeEntry ??
            throw new InvalidOperationException("Missing Type native entry definition.");
        var rdArgs = Bus.Allocate(invocation, TypeRdArgsBytes, "TypeRDArgs", true);
        invocation.TypeEntryLayout = new TypeNativeEntryLayout(rdArgs);
        Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Version,
            definition.DosVersion);
        Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Revision,
            definition.DosRevision);
        var names = rdArgs + 32;
        var source = rdArgs + 64;
        Bus.Long(names, source);
        Bus.Long(names + 4, definition.SecondSource is null ? 0 : rdArgs + 80);
        Bus.Long(names + 8, 0);
        Put(source, "SRC:input");
        if (definition.SecondSource is not null) Put(rdArgs + 80, "SRC:second");
        if (definition.To is not null) Put(rdArgs + 96, definition.To);
        if (definition.Option is not null) Put(rdArgs + 128, definition.Option);
    }

    private void VerifyTypeNativeEntry(Invocation invocation)
    {
        var definition = invocation.Definition.TypeEntry ??
            throw new InvalidOperationException("Missing Type native entry definition.");
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.TypeEntryLayout is null && invocation.Reads == 0 &&
                invocation.FreeArgs == 0 && invocation.Allocations == 0 &&
                invocation.FreeMem == 0 && invocation.FileOpens == 0 &&
                invocation.FileCloses == 0,
                "Type crossed an invalid startup boundary.");
            return;
        }
        var warning = definition.Hex && definition.Number;
        var unsupportedExtension =
            !IsWorkbench31TypeNativeEntrySuite(suite) &&
            (definition.DosVersion < 50 || definition.DosVersion == 50 &&
                definition.DosRevision < 67);
        var noWorkspace = warning || unsupportedExtension;
        var outputFailure = definition.OutputOpenError != 0;
        var breakFailure = definition.BreakOnHexPoll;
        var ioFailure = definition.ReadError != 0 || definition.WriteError != 0;
        Require(invocation.Reads == 1 && invocation.FreeArgs == 1,
            "Type must retain and release exactly one supplied ReadArgs lease.");
        Require(invocation.Allocations == (noWorkspace ? 1 : 2) &&
            invocation.FreeMem == (noWorkspace ? 1 : 2),
            "Type result/workspace allocation lifetime differs from its bounded path.");
        var matchedInputs = definition.SecondSource is null ? 1 : 2;
        Require(invocation.FileOpens == (noWorkspace || !definition.MatchFound ? 0 :
                outputFailure ? 1 :
                (ioFailure ? 1 : matchedInputs) +
                    (definition.To is null ? 0 : 1)) &&
            invocation.FileCloses == (noWorkspace || !definition.MatchFound || outputFailure ? 0 :
                definition.OpenError != 0 ? 0 :
                (ioFailure ? 1 : matchedInputs) +
                    (definition.To is null ? 0 : 1)),
            "Type owned stream lifetime differs from the supplied vector.");
        var matcherCalls = noWorkspace || outputFailure ? 0 : definition.MatchFound ? matchedInputs : 1;
        Require(CountTypeEvent(invocation, "MatchFirst") == matcherCalls &&
            CountTypeEvent(invocation, "MatchEnd") == matcherCalls &&
            CountTypeEvent(invocation, "MatchNext") ==
                (noWorkspace || outputFailure || breakFailure || ioFailure ||
                    !definition.MatchFound || definition.OpenError != 0 ? 0 : matchedInputs),
            "Type wildcard vector calls differ from the supplied FROM traversal.");
        Require(invocation.Events.IndexOf("ReadArgs") < invocation.Events.IndexOf("FreeArgs") &&
            invocation.Events.IndexOf("FreeArgs") < invocation.Events.IndexOf("CloseLibrary"),
            "Type released ReadArgs after closing dos.library.");
        var setIoErrCalls = unsupportedExtension ? 3 :
            definition.OpenError == 0 && !breakFailure && !ioFailure ? 3 : 4;
        Require(CountTypeEvent(invocation, "SetIoErr") == setIoErrCalls,
            $"Type error restoration count differs from the supplied path; observed {CountTypeEvent(invocation, "SetIoErr")}.");
        Require(invocation.TypeEntrySignals == (breakFailure ? 1 : 0),
            "Type HEX Ctrl-C polling differs from the supplied row cadence.");
        if (unsupportedExtension)
            Require(CountTypeEvent(invocation, "PrintFault") == 1,
                "Type must report an unavailable MorphOS AnchorPath extension.");
    }

    private static int CountTypeEvent(Invocation invocation, string name) =>
        invocation.Events.Count(value => value == name);

    private void RegisterTypeNativeEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.TypeEntry ??
                throw new InvalidOperationException("Missing Type native entry definition.");
            var layout = invocation.TypeEntryLayout ??
                throw new InvalidOperationException("Type ReadArgs without supplied storage.");
            var workbench = IsWorkbench31TypeNativeEntrySuite(suite);
            var resultBytes = workbench ? 20u : 24u;
            Require(Bus.CString(state.D[1]) == (workbench
                    ? "FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S"
                    : "FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S,NOLINE/S") &&
                state.D[3] == 0, "Type ReadArgs template/source ABI mismatch.");
            var results = state.D[2];
            Require((results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size == resultBytes,
                "Type result slots have an unexpected size.");
            for (var offset = 0u; offset < resultBytes; offset += 4)
                Require(Bus.Long(results + offset) == 0,
                "Type result slots were not cleared before ReadArgs.");
            invocation.Reads++;
            Bus.Long(results, layout.RdArgs + 32);
            if (definition.To is not null) Bus.Long(results + 4, layout.RdArgs + 96);
            if (definition.Option is not null) Bus.Long(results + 8, layout.RdArgs + 128);
            if (definition.Hex) Bus.Long(results + 12, uint.MaxValue);
            if (definition.Number) Bus.Long(results + 16, uint.MaxValue);
            if (!workbench && definition.NoLine)
                Bus.Long(results + 20, uint.MaxValue);
            return layout.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            var layout = invocation.TypeEntryLayout ??
                throw new InvalidOperationException("Type FreeArgs without supplied storage.");
            Require(state.D[1] == layout.RdArgs, "Type FreeArgs lease differs from ReadArgs result.");
            Bus.Release(invocation, layout.RdArgs, "TypeRDArgs", TypeRdArgsBytes);
            invocation.TypeEntryLayout = null;
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) => invocation.OutputBptr);
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            var definition = invocation.Definition.TypeEntry ??
                throw new InvalidOperationException("Missing Type native entry definition.");
            Require(definition.Hex && definition.Number &&
                Bus.CString(state.D[1]) == "Type can't do both HEX and NUMBER\n",
                "Type HEX/NUMBER warning bytes differ from the supplied source vector.");
            var bytes = Encoding.Latin1.GetBytes(Bus.CString(state.D[1]));
            invocation.Output.Write(bytes, 0, bytes.Length);
            return unchecked((uint)bytes.Length);
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            var definition = invocation.Definition.TypeEntry ??
                throw new InvalidOperationException("Missing Type native entry definition.");
            string expected;
            if (definition.Option == "x")
            {
                var workbench = IsWorkbench31TypeNativeEntrySuite(suite);
                var resultBytes = workbench ? 20u : 24u;
                var scratchOffset = workbench ? 16u : 20u;
                Require(Bus.CString(state.D[1]) == "Option '%lc' ignored\n" &&
                    Bus.OwnedAllocation(invocation, state.D[2] - scratchOffset, "Exec").Size == resultBytes &&
                    Bus.Long(state.D[2]) == (uint)'x',
                    "Type ignored-OPT diagnostic ABI differs from the supplied source vector.");
                expected = "Option 'x' ignored\n";
            }
            else
            {
                Require((!definition.MatchFound || definition.OpenError != 0 ||
                        definition.ReadError != 0 || definition.WriteError != 0) &&
                    Bus.CString(state.D[1]) == "TYPE: can't open %s\n" &&
                    Bus.CString(Bus.Long(state.D[2])) == "SRC:input",
                    "Type no-match diagnostic ABI differs from the supplied source vector.");
                expected = "TYPE: can't open SRC:input\n";
            }
            var bytes = Encoding.Latin1.GetBytes(expected);
            invocation.Output.Write(bytes, 0, bytes.Length);
            return unchecked((uint)bytes.Length);
        });
        Register(baseAddress, DosLvo.Open, "Open", (state, invocation) =>
        {
            var definition = invocation.Definition.TypeEntry ??
                throw new InvalidOperationException("Missing Type native entry definition.");
            var expectedOutput = definition.To is not null && invocation.FileOpens == 0;
            Require(state.D[2] == (uint)(expectedOutput ? DOS.FileMode.NewFile : DOS.FileMode.OldFile),
                "Type Open mode differs from the supplied stream vector.");
            var inputIndex = invocation.FileOpens - (definition.To is null ? 0 : 1);
            var expectedInput = inputIndex == 0 ? "SRC:input" : "SRC:second";
            Require(Bus.CString(state.D[1]) == (expectedOutput ? definition.To : expectedInput),
                "Type Open path differs from the supplied matcher result.");
            invocation.FileOpens++;
            if (expectedOutput && definition.OutputOpenError != 0)
            {
                invocation.IoError = definition.OutputOpenError;
                return 0;
            }
            if (!expectedOutput && definition.OpenError != 0)
            {
                invocation.IoError = definition.OpenError;
                return 0;
            }
            return expectedOutput ? TypeOutputHandle : TypeInputHandle;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state, invocation) =>
        {
            var definition = invocation.Definition.TypeEntry ??
                throw new InvalidOperationException("Missing Type native entry definition.");
            var expectedError = definition.OpenError != 0 ? definition.OpenError :
                definition.OutputOpenError != 0 ? definition.OutputOpenError :
                definition.ReadError != 0 ? definition.ReadError :
                definition.WriteError != 0 ? definition.WriteError :
                definition.BreakOnHexPoll ? (int)DOS.Error.Break :
                !IsWorkbench31TypeNativeEntrySuite(suite) &&
                    (definition.DosVersion < 50 ||
                     definition.DosVersion == 50 &&
                        definition.DosRevision < 67)
                    ? (int)DOS.Error.NotImplemented : 0;
            Require(expectedError != 0 && unchecked((int)state.D[1]) == expectedError &&
                state.D[2] == 0, "Type PrintFault ABI differs from the supplied failure.");
            return 0;
        });
        Register(baseAddress, DosLvo.Close, "Close", (state, invocation) =>
        {
            var definition = invocation.Definition.TypeEntry ??
                throw new InvalidOperationException("Missing Type native entry definition.");
            Require(state.D[1] == TypeInputHandle ||
                definition.To is not null && state.D[1] == TypeOutputHandle,
                "Type closed a handle outside its supplied vector.");
            invocation.FileCloses++;
            return 1;
        });
        Register(baseAddress, DosLvo.Read, "Read", (state, invocation) =>
        {
            var definition = invocation.Definition.TypeEntry ??
                throw new InvalidOperationException("Missing Type native entry definition.");
            var layout = invocation.TypeEntryLayout ??
                throw new InvalidOperationException("Type Read after its arguments were freed.");
            var expectedCapacity = definition.Hex ? 8176u : 8192u;
            Require(state.D[1] == TypeInputHandle && state.D[2] != 0 &&
                state.D[3] == expectedCapacity,
                "Type Read does not use the bounded supplied stream ABI.");
            var bytes = Encoding.Latin1.GetBytes(definition.Source);
            if (layout.ActiveSourceIndex == 1)
                bytes = Encoding.Latin1.GetBytes(definition.SecondSource!);
            if (definition.ReadError != 0)
            {
                invocation.IoError = definition.ReadError;
                layout.ReadCalls++;
                return unchecked((uint)-1);
            }
            var count = Math.Min(bytes.Length - layout.ReadOffset, checked((int)state.D[3]));
            if (count > 0)
            {
                bytes.AsSpan(layout.ReadOffset, count).CopyTo(Bus.Memory.AsSpan((int)state.D[2], count));
                layout.ReadOffset += count;
            }
            layout.ReadCalls++;
            return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            var definition = invocation.Definition.TypeEntry ??
                throw new InvalidOperationException("Missing Type native entry definition.");
            var layout = invocation.TypeEntryLayout ??
                throw new InvalidOperationException("Type Write without its argument owner.");
            var expected = definition.To is null ? invocation.OutputBptr : TypeOutputHandle;
            Require(state.D[1] == expected && state.D[3] > 0,
                "Type Write handle or byte count differs from its supplied stream.");
            var count = definition.WriteChunk is int chunk
                ? Math.Min(chunk, checked((int)state.D[3])) : checked((int)state.D[3]);
            layout.WriteCalls++;
            if (definition.WriteError != 0 &&
                layout.WriteCalls > definition.WriteErrorAfterCalls)
            {
                invocation.IoError = definition.WriteError;
                return unchecked((uint)-1);
            }
            invocation.Output.Write(Bus.Memory, checked((int)state.D[2]), count);
            return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state, invocation) =>
        {
            var definition = invocation.Definition.TypeEntry ??
                throw new InvalidOperationException("Missing Type native entry definition.");
            var index = CountTypeEvent(invocation, "MatchFirst") - 1;
            var expected = index == 0 ? "SRC:input" : "SRC:second";
            var literalSoftLinks = !IsWorkbench31TypeNativeEntrySuite(suite) &&
                (definition.DosVersion > 50 || definition.DosVersion == 50 &&
                    definition.DosRevision >= 67);
            var expectedStringLength = 512u |
                (literalSoftLinks ? 0x8000u : 0u);
            Require(index is 0 or 1 && (index == 0 || definition.SecondSource is not null) &&
                Bus.CString(state.D[1]) == expected &&
                Bus.Word(state.D[2] + (uint)DosLayout.AnchorPath.StringLength) ==
                    expectedStringLength &&
                Bus.Memory[checked((int)(state.D[2] +
                    (uint)DosLayout.AnchorPath.Reserved))] ==
                    (literalSoftLinks ? 2 : 0),
                "Type MatchFirst path or initialized AnchorPath differs.");
            if (!definition.MatchFound) return (uint)DOS.Error.NoMoreEntries;
            Put(state.D[2] + (uint)DosLayout.AnchorPath.PathBuffer, expected);
            var layout = invocation.TypeEntryLayout ??
                throw new InvalidOperationException("Type MatchFirst after arguments were freed.");
            layout.MatchLive = true;
            layout.ActiveSourceIndex = index;
            layout.ReadOffset = 0;
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state, invocation) =>
        {
            var layout = invocation.TypeEntryLayout ??
                throw new InvalidOperationException("Type MatchNext after arguments were freed.");
            Require(layout.MatchLive, "Type MatchNext without a live MatchFirst vector.");
            return (uint)DOS.Error.NoMoreEntries;
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state, invocation) =>
        {
            var layout = invocation.TypeEntryLayout ??
                throw new InvalidOperationException("Type MatchEnd after arguments were freed.");
            layout.MatchLive = false;
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            var previous = invocation.IoError;
            invocation.IoError = unchecked((int)state.D[1]);
            Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, state.D[1]);
            return unchecked((uint)previous);
        });
    }

    private void Put(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }

    private void RegisterTypeNativeEntryExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
        {
            var definition = invocation.Definition.TypeEntry ??
                throw new InvalidOperationException("Missing Type native entry definition.");
            Require(definition.BreakOnHexPoll && state.D[0] == 0 &&
                state.D[1] == (1u << 12) && invocation.TypeEntrySignals == 0,
                "Type HEX must poll only Ctrl-C at the supplied row boundary.");
            invocation.TypeEntrySignals++;
            return 1u << 12;
        });
    }

    private static string FullHexRows(int count)
    {
        var output = new StringBuilder(count * 59);
        for (var offset = 0; offset < count * 16; offset += 16)
            output.AppendFormat("{0:X4}: 41414141 41414141 41414141 41414141 AAAAAAAAAAAAAAAA\n", offset);
        return output.ToString();
    }
}
