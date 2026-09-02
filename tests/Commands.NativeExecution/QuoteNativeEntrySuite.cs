using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Supplied post-ReadArgs vectors for the bounded MorphOS Quote STR entry.
/// The suite runs its resident HUNK through the DOS/Exec ABI and validates
/// only the admitted STR forward path; it is not a DOS parser or a reference
/// comparison for the original Quote command.
/// </summary>
internal sealed record QuoteNativeEntryCase(string Rules, string? Text,
    bool NoLine, bool Reverse, string Output)
{
    public int Result { get; init; } = DOS.RETURN_OK;
    public int IoError { get; init; }
    public int? ExpectedWriteBytes { get; init; }
}

internal sealed partial class ProbeFixture
{
    public const string QuoteNativeEntrySuite = "quote-native-entry-vector-fixture";

    private List<object> RunQuoteNativeEntryCases()
    {
        ProbeCase[] cases =
        [
            Quote("readitem", "READITEM", "A=B", false, "\"A=B\"\n"),
            Quote("hex-noline", "HEX", "abc", true, "616263"),
            Quote("uri-then-hex", "URI HEX", "A B", false, "4125323042\n"),
            Quote("unknown-rule", "UNKNOWN", "abc", false, "") with
                { Result = DOS.RETURN_ERROR, Error = (int)DOS.Error.LineTooLong },
            Quote("unsupported-reverse", "HEX", "abc", false, "") with
                { Result = DOS.RETURN_ERROR, Error = (int)DOS.Error.BadTemplate,
                    QuoteEntry = new QuoteNativeEntryCase("HEX", "abc", false, true, "") },
            Quote("short-write", "HEX", "abc", true, "616") with
                { Result = DOS.RETURN_ERROR, Error = 221, WriteResult = 3,
                    QuoteEntry = new QuoteNativeEntryCase("HEX", "abc", true, false, "616")
                        { ExpectedWriteBytes = 6 } },
            new ProbeCase("readargs-failure", "", DOS.RETURN_ERROR, 118, "")
            {
                QuoteEntry = new QuoteNativeEntryCase("HEX", "abc", true, false, ""),
                ParserError = 118
            },
            new ProbeCase("result-allocation-failure", "", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "")
            {
                QuoteEntry = new QuoteNativeEntryCase("HEX", "abc", true, false, ""),
                AllocationFailure = true
            },
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-readitem" },
            cases[1] with { Name = "repeat-hex-noline" }
        ], true));
        reports.AddRange(Execute([
            cases[6] with { Name = "interleaved-readargs-failure", StackBytes = 4096 },
            cases[2] with { Name = "interleaved-uri-then-hex" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Quote(string name, string rules, string text,
        bool noLine, string output) => new(name, "", DOS.RETURN_OK, 0, output)
        {
            QuoteEntry = new QuoteNativeEntryCase(rules, text, noLine, false, output)
        };

    private void VerifyQuoteNativeEntry(Invocation invocation)
    {
        var definition = invocation.Definition.QuoteEntry ??
            throw new InvalidOperationException("Missing Quote native entry definition.");
        var parsed = invocation.Definition.ParserError == 0 &&
            !invocation.Definition.AllocationFailure;
        Require(invocation.Reads == (invocation.Definition.AllocationFailure ? 0 : 1),
            "Quote ReadArgs count differs from the supplied path.");
        Require(invocation.FreeArgs == (parsed ? 1 : 0),
            "Quote RDArgs lifetime differs from the supplied path.");
        if (!parsed)
        {
            Require(invocation.Allocations == 1 &&
                invocation.FreeMem == (invocation.Definition.ParserError == 0 ? 0 : 1),
                "Quote parser allocation/cleanup differs from the supplied path.");
            return;
        }

        var expectedTransientAllocations = definition.Reverse ? 1 : 6;
        Require(invocation.Allocations == expectedTransientAllocations &&
            invocation.FreeMem == expectedTransientAllocations,
            "Quote must release each parsed result and admitted transient buffer once.");
        Require(invocation.Events.IndexOf("ReadArgs") < invocation.Events.IndexOf("FreeArgs") &&
            invocation.Events.IndexOf("FreeArgs") < invocation.Events.IndexOf("CloseLibrary"),
            "Quote released ReadArgs storage after closing DOS.");
    }

    private void RegisterQuoteNativeEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.QuoteEntry ??
                throw new InvalidOperationException("Missing Quote native entry definition.");
            Require(Bus.CString(state.D[1]) ==
                "RULE/A,FILE/K,VAR/K,STR,NOLINE/S,NOQUOTES/S,FIRSTLINE/S,REVERSE=UNQUOTE/S" &&
                state.D[3] == 0, "Quote ReadArgs template/source ABI mismatch.");
            var results = state.D[2];
            Require((results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size == 32,
                "Quote result slots must be eight owned LONGs.");
            for (var offset = 0u; offset < 32; offset += 4)
                Require(Bus.Long(results + offset) == 0,
                    "Quote result slots were not cleared before ReadArgs.");
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
                    "Quote fixture RDArgs string storage overflow.");
                bytes.CopyTo(Bus.Memory.AsSpan((int)text));
                Bus.Memory[text + (uint)bytes.Length] = 0;
                var address = text;
                text += (uint)bytes.Length + 1;
                return address;
            }

            Bus.Long(results, Put(definition.Rules));
            if (definition.Text is not null) Bus.Long(results + 12, Put(definition.Text));
            if (definition.NoLine) Bus.Long(results + 16, 1);
            if (definition.Reverse) Bus.Long(results + 28, 1);
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
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            var definition = invocation.Definition.QuoteEntry ??
                throw new InvalidOperationException("Missing Quote native entry definition.");
            Require(state.D[1] == invocation.OutputBptr && state.D[3] ==
                (uint)(definition.ExpectedWriteBytes ??
                    Encoding.Latin1.GetByteCount(definition.Output)),
                "Quote Write ABI or output byte count differs from the supplied vector.");
            var count = invocation.Definition.WriteResult ?? checked((int)state.D[3]);
            if (count > 0)
                invocation.Output.Write(Bus.Memory, checked((int)state.D[2]), count);
            if (count != state.D[3]) invocation.IoError = 221;
            return unchecked((uint)count);
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
}
