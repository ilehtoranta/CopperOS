using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Supplied post-ReadArgs vectors for the native Eval entry. This suite does
/// not parse CLI text or claim a DOS parser; it verifies the command's public
/// vector ABI, resource ownership, output bytes, and shared-image reuse.
/// </summary>
internal sealed record EvalEntryCase(string First, string? Operator,
    string[] Following, string? To, string? Format, bool Hex, string Output)
{
    public bool WorkbenchProfile { get; init; }
    public int Result { get; init; } = DOS.RETURN_OK;
    public int IoError { get; init; }
}

internal sealed partial class ProbeFixture
{
    public const string EvalEntrySuite = "eval-native-entry-vector-fixture";
    public const string WorkbenchEvalEntrySuite = "eval-wb31-native-entry-vector-fixture";

    private List<object> RunEvalEntryCases()
    {
        ProbeCase[] cases =
        [
            Eval("decimal-expression", "1+2*3", null, [], null, null, false, "7\n"),
            Eval("operand-vector", "1", "+", ["2", "*", "3"], null, null, false, "7\n"),
            Eval("modulo-word-prefix", "9", "mo", ["4"], null, null, false, "1\n"),
            Eval("xor-word-prefix", "7", "xo", ["3"], null, null, false, "4\n"),
            Eval("equivalence-word-prefix", "5", "eq", ["3"], null, null, false, "-7\n"),
            Eval("left-shift-word-prefix", "1", "ls", ["4"], null, null, false, "16\n"),
            Eval("right-shift-word-prefix", "32", "rs", ["3"], null, null, false, "4\n"),
            Eval("hex", "42", null, [], null, null, true, "0x2a\n"),
            Eval("lformat", "42", null, [], null, "x=%x", true, "x=2a"),
            Eval("to-output", "42", null, [], "RAM:result", null, false, "42\n"),
            Eval("bad-expression", "1+", null, [], null, null, false, "") with
                { Result = DOS.RETURN_ERROR },
            new ProbeCase("readargs-failure", "", DOS.RETURN_ERROR, 118, "")
            {
                Eval = new("1", null, [], null, null, false, ""),
                ParserError = 118
            },
            new ProbeCase("result-allocation-failure", "", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "")
            {
                Eval = new("1", null, [], null, null, false, ""),
                AllocationFailure = true
            },
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        for (var round = 0; round < 2; round++)
        {
            reports.AddRange(Execute([cases[5] with { Name = $"repeat-{round}-failure" }], false));
            reports.AddRange(Execute([cases[1] with { Name = $"repeat-{round}-success" }], false));
        }
        reports.AddRange(Execute([
            cases[0] with { Name = "interleaved-decimal", StackBytes = 4096 },
            cases[4] with { Name = "interleaved-to" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private List<object> RunWorkbenchEvalEntryCases()
    {
        ProbeCase[] cases =
        [
            WorkbenchEval("left-to-right", "1+2*3+4", null, [], null, null, "13\n"),
            WorkbenchEval("operand-vector", "1", "|", ["2", "&", "4"], null, null, "0\n"),
            WorkbenchEval("x-format", "42", null, [], null, "x=%x", "x=A"),
            WorkbenchEval("o-format", "9", null, [], null, "o=%o2", "o=11"),
            WorkbenchEval("to-output", "42", null, [], "RAM:result", null, "42\n"),
            WorkbenchEval("prefix-caret", "2^3", null, [], null, null, "2\n"),
            new ProbeCase("readargs-failure", "", DOS.RETURN_ERROR, 118, "")
            {
                Eval = new("1", null, [], null, null, false, "")
                    { WorkbenchProfile = true }, ParserError = 118
            },
            new ProbeCase("result-allocation-failure", "", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "")
            {
                Eval = new("1", null, [], null, null, false, "")
                    { WorkbenchProfile = true }, AllocationFailure = true
            },
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-left-to-right" },
            cases[5] with { Name = "repeat-prefix-caret" }
        ], true));
        reports.AddRange(Execute([
            cases[2] with { Name = "interleaved-format", StackBytes = 4096 },
            cases[4] with { Name = "interleaved-to" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Eval(string name, string first, string? op,
        string[] following, string? to, string? format, bool hex, string output) =>
        new(name, "", DOS.RETURN_OK, 0, output)
        {
            Eval = new EvalEntryCase(first, op, following, to, format, hex, output)
        };

    private static ProbeCase WorkbenchEval(string name, string first, string? op,
        string[] following, string? to, string? format, string output) =>
        new(name, "", DOS.RETURN_OK, 0, output)
        {
            Eval = new EvalEntryCase(first, op, following, to, format, false, output)
                { WorkbenchProfile = true }
        };

    private void VerifyEvalEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Eval ?? throw new InvalidOperationException("Missing Eval definition.");
        bool parsed = invocation.Definition.ParserError == 0 && !invocation.Definition.AllocationFailure;
        Require(invocation.Reads == (invocation.Definition.AllocationFailure ? 0 : 1),
            "Eval ReadArgs call count differs from the supplied path.");
        Require(invocation.FreeArgs == (parsed ? 1 : 0),
            "Eval RDArgs lifetime differs from the supplied path.");
        if (!parsed)
        {
            Require(invocation.Allocations == (invocation.Definition.AllocationFailure ? 1 : 1),
                "Eval parser setup allocation count differs from the supplied path.");
            Require(invocation.FreeMem == (invocation.Definition.ParserError == 0 ? 0 : 1),
                "Eval parser setup cleanup differs from the supplied path.");
            return;
        }

        Require(invocation.Allocations == 3 && invocation.FreeMem == 3,
            "Eval must release result, expression and output allocations exactly once.");
        Require(invocation.FileOpens == (definition.To is null ? 0 : 1) &&
            invocation.FileCloses == (definition.To is null ? 0 : 1),
            "Eval TO output handle lifetime differs from the supplied path.");
        Require(invocation.Events.IndexOf("ReadArgs") < invocation.Events.IndexOf("FreeArgs") &&
            invocation.Events.IndexOf("FreeArgs") < invocation.Events.IndexOf("CloseLibrary"),
            "Eval released arguments after parser use or after closing DOS.");
    }

    private void RegisterEvalEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Eval ?? throw new InvalidOperationException("Missing Eval definition.");
            var expectedTemplate = definition.WorkbenchProfile
                ? "VALUE1/A,OP,VALUE2/M,TO/K,LFORMAT/K"
                : "VALUE1/A,OP,VALUE2/M,TO/K,LFORMAT/K,HEX/S";
            var expectedBytes = definition.WorkbenchProfile ? 20u : 24u;
            Require(Bus.CString(state.D[1]) == expectedTemplate && state.D[3] == 0,
                $"Eval ReadArgs template/source ABI mismatch: '{Bus.CString(state.D[1])}'.");
            var results = state.D[2];
            Require((results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size == expectedBytes,
                "Eval result slots must match the selected profile.");
            for (var offset = 0u; offset < expectedBytes; offset += 4)
                Require(Bus.Long(results + offset) == 0, "Eval result slots were not zeroed before ReadArgs.");
            invocation.Reads++;
            if (invocation.Definition.ParserError != 0)
            {
                invocation.IoError = invocation.Definition.ParserError;
                return 0;
            }

            var rdArgs = Bus.Allocate(invocation, 512, "RDArgs", true);
            var text = rdArgs + 128;
            uint Put(string value)
            {
                var bytes = Encoding.Latin1.GetBytes(value);
                Require(text + (uint)bytes.Length + 1 <= rdArgs + 512,
                    "Eval fixture RDArgs string storage overflow.");
                bytes.CopyTo(Bus.Memory.AsSpan((int)text));
                Bus.Memory[text + (uint)bytes.Length] = 0;
                var address = text;
                text += (uint)bytes.Length + 1;
                return address;
            }

            Bus.Long(results, Put(definition.First));
            if (definition.Operator is not null) Bus.Long(results + 4, Put(definition.Operator));
            if (definition.Following.Length != 0)
            {
                var vector = rdArgs + 64;
                Require(definition.Following.Length < 12, "Eval fixture vector bound exceeded.");
                for (var index = 0; index < definition.Following.Length; index++)
                    Bus.Long(vector + (uint)(index * 4), Put(definition.Following[index]));
                Bus.Long(vector + (uint)(definition.Following.Length * 4), 0);
                Bus.Long(results + 8, vector);
            }
            if (definition.To is not null) Bus.Long(results + 12, Put(definition.To));
            if (definition.Format is not null) Bus.Long(results + 16, Put(definition.Format));
            if (definition.Hex)
            {
                Require(!definition.WorkbenchProfile,
                    "Workbench Eval has no HEX result slot.");
                Bus.Long(results + 20, 1);
            }
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) => invocation.OutputBptr);
        Register(baseAddress, DosLvo.Open, "Open", (state, invocation) =>
        {
            var definition = invocation.Definition.Eval ?? throw new InvalidOperationException("Missing Eval definition.");
            Require(definition.To is not null && Bus.CString(state.D[1]) == definition.To &&
                state.D[2] == (uint)DOS.FileMode.NewFile && invocation.FileOpens == 0,
                "Eval TO open ABI differs from the supplied path.");
            invocation.FileOpens++;
            return invocation.OutputBptr + 0x400;
        });
        Register(baseAddress, DosLvo.Close, "Close", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr + 0x400 && invocation.FileOpens == 1 &&
                invocation.FileCloses == 0, "Eval closed an unowned TO handle.");
            invocation.FileCloses++;
            return 1;
        });
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            var definition = invocation.Definition.Eval ?? throw new InvalidOperationException("Missing Eval definition.");
            var expectedHandle = definition.To is null ? invocation.OutputBptr : invocation.OutputBptr + 0x400;
            Require(state.D[1] == expectedHandle && state.D[3] == Encoding.Latin1.GetByteCount(definition.Output),
                "Eval write ABI or output length differs from the supplied path.");
            invocation.Output.Write(Bus.Memory, checked((int)state.D[2]), checked((int)state.D[3]));
            return state.D[3];
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            var previous = invocation.IoError;
            invocation.IoError = unchecked((int)state.D[1]);
            Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, state.D[1]);
            return unchecked((uint)previous);
        });
    }
}
