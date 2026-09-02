using System.Text;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>Direct native control-block cases for the Quote forward probe.</summary>
internal sealed record QuoteForwardProbeCase(string Source, string Rules,
    string Output, bool Reject = false);

internal sealed record QuoteForwardNativeLayout(uint Control, uint Source,
    uint Rules, uint RuleList, uint First, uint Second);

internal sealed partial class ProbeFixture
{
    public const string QuoteForwardProbeSuite = "quote-forward-probe-fixture";
    private const uint QuoteControlBytes = 60;
    private const uint QuoteBufferBytes = 256;

    private List<object> RunQuoteForwardProbeCases()
    {
        ProbeCase[] cases =
        [
            Quote("readitem-hex", "A=B", "READITEM HEX", "22413d4222"),
            Quote("uri-hex", "A B", "URI HEX", "4125323042"),
            Quote("base64", "abc", "BASE64", "YWJj"),
            Quote("unknown-rule", "abc", "UNKNOWN", "", true),
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-readitem-hex" },
            cases[1] with { Name = "repeat-uri-hex" }
        ], true));
        reports.AddRange(Execute([
            cases[3] with { Name = "interleaved-reject", StackBytes = 4096 },
            cases[2] with { Name = "interleaved-base64" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Quote(string name, string source, string rules,
        string output, bool reject = false) => new(name, "",
            reject ? 10 : 0, Invocation.InitialIoError, "")
        {
            EntryLength = (int)QuoteControlBytes,
            QuoteForward = new(source, rules, output, reject)
        };

    private void PrepareQuoteForwardProbe(Invocation invocation)
    {
        var definition = invocation.Definition.QuoteForward ??
            throw new InvalidOperationException("Missing Quote forward definition.");
        var source = Encoding.Latin1.GetBytes(definition.Source);
        var rules = Encoding.Latin1.GetBytes(definition.Rules);
        Require(source.Length <= QuoteBufferBytes && rules.Length <= QuoteBufferBytes,
            "Quote fixture input exceeds bounded control storage.");
        var layout = new QuoteForwardNativeLayout(
            Bus.Allocate(invocation, QuoteControlBytes, "QuoteFixture", true),
            Bus.Allocate(invocation, QuoteBufferBytes, "QuoteFixture", false),
            Bus.Allocate(invocation, QuoteBufferBytes, "QuoteFixture", false),
            Bus.Allocate(invocation, QuoteBufferBytes, "QuoteFixture", false),
            Bus.Allocate(invocation, QuoteBufferBytes, "QuoteFixture", false),
            Bus.Allocate(invocation, QuoteBufferBytes, "QuoteFixture", false));
        invocation.QuoteForwardLayout = layout;
        Bus.Memory.AsSpan((int)layout.Source, (int)QuoteBufferBytes).Fill(0xa5);
        Bus.Memory.AsSpan((int)layout.Rules, (int)QuoteBufferBytes).Fill(0xa5);
        Bus.Memory.AsSpan((int)layout.RuleList, (int)QuoteBufferBytes).Fill(0xa5);
        Bus.Memory.AsSpan((int)layout.First, (int)QuoteBufferBytes).Fill(0xa5);
        Bus.Memory.AsSpan((int)layout.Second, (int)QuoteBufferBytes).Fill(0xa5);
        source.CopyTo(Bus.Memory.AsSpan((int)layout.Source));
        rules.CopyTo(Bus.Memory.AsSpan((int)layout.Rules));
        Bus.Long(layout.Control, layout.Source);
        Bus.Long(layout.Control + 4, (uint)source.Length);
        Bus.Long(layout.Control + 8, layout.Rules);
        Bus.Long(layout.Control + 12, (uint)rules.Length);
        Bus.Long(layout.Control + 16, layout.RuleList);
        Bus.Long(layout.Control + 20, QuoteBufferBytes);
        Bus.Long(layout.Control + 24, layout.First);
        Bus.Long(layout.Control + 28, QuoteBufferBytes);
        Bus.Long(layout.Control + 32, layout.Second);
        Bus.Long(layout.Control + 36, QuoteBufferBytes);
        Bus.Long(layout.Control + 48, layout.Source);
        Bus.Long(layout.Control + 52, layout.Second + QuoteBufferBytes - layout.Source);
    }

    private void VerifyQuoteForwardProbe(Invocation invocation)
    {
        var definition = invocation.Definition.QuoteForward ??
            throw new InvalidOperationException("Missing Quote forward definition.");
        var layout = invocation.QuoteForwardLayout ??
            throw new InvalidOperationException("Missing Quote forward storage.");
        Require(invocation.Opens == 0 && invocation.Closes == 0 &&
            invocation.Reads == 0 && invocation.FreeArgs == 0 &&
            invocation.Allocations == 0 && invocation.FreeMem == 0,
            "Quote forward probe must not use DOS or allocate resident state.");
        if (definition.Reject)
        {
            Require(Bus.Long(layout.Control + 56) == 0 &&
                Bus.Memory.AsSpan((int)layout.First, (int)QuoteBufferBytes)
                    .IndexOfAnyExcept((byte)0xa5) < 0 &&
                Bus.Memory.AsSpan((int)layout.Second, (int)QuoteBufferBytes)
                    .IndexOfAnyExcept((byte)0xa5) < 0,
                "Rejected Quote probe must not publish completion or modify scratch buffers.");
            ReleaseQuoteForwardStorage(invocation, layout);
            return;
        }
        var output = Bus.Long(layout.Control + 40);
        var length = Bus.Long(layout.Control + 44);
        Require(output is var address && (address == layout.First || address == layout.Second) && length ==
            Encoding.Latin1.GetByteCount(definition.Output) &&
            Encoding.Latin1.GetString(Bus.Memory, checked((int)output),
                checked((int)length)) == definition.Output &&
            Bus.Long(layout.Control + 56) == 0x51544650,
            "Quote forward probe output/control publication differs from the bounded case.");
        ReleaseQuoteForwardStorage(invocation, layout);
    }

    private void ReleaseQuoteForwardStorage(Invocation invocation,
        QuoteForwardNativeLayout layout)
    {
        Bus.Release(invocation, layout.Second, "QuoteFixture", QuoteBufferBytes);
        Bus.Release(invocation, layout.First, "QuoteFixture", QuoteBufferBytes);
        Bus.Release(invocation, layout.RuleList, "QuoteFixture", QuoteBufferBytes);
        Bus.Release(invocation, layout.Rules, "QuoteFixture", QuoteBufferBytes);
        Bus.Release(invocation, layout.Source, "QuoteFixture", QuoteBufferBytes);
        Bus.Release(invocation, layout.Control, "QuoteFixture", QuoteControlBytes);
        invocation.QuoteForwardLayout = null;
    }
}
