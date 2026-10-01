using System.Text;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>Direct resident control-block cases for the Search literal matcher.</summary>
internal sealed record SearchLiteralProbeCase(string Text, string Pattern,
    bool CaseSensitive, bool Found, bool Reject = false);

internal sealed record SearchLiteralNativeLayout(uint Control, uint Text,
    uint Pattern, byte[] ExpectedText, byte[] ExpectedPattern);

internal sealed partial class ProbeFixture
{
    public const string SearchLiteralProbeSuite = "search-literal-probe-fixture";
    private const uint SearchControlBytes = 36;
    private const uint SearchBufferBytes = 256;

    private List<object> RunSearchLiteralProbeCases()
    {
        ProbeCase[] cases =
        [
            Search("start", "needle hay", "needle", true, true),
            Search("middle", "a needle b", "needle", true, true),
            Search("ascii-nocase", "Alpha", "alpha", false, true),
            Search("nonmatch", "alpha", "beta", true, false),
            Search("empty-pattern", "alpha", "", true, false, true),
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[2] with { Name = "repeat-ascii-nocase" },
            cases[3] with { Name = "repeat-nonmatch" }
        ], true));
        reports.AddRange(Execute([
            cases[4] with { Name = "interleaved-empty-pattern", StackBytes = 4096 },
            cases[1] with { Name = "interleaved-middle" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Search(string name, string text, string pattern,
        bool caseSensitive, bool found, bool reject = false) => new(name, "",
            reject ? 10 : 0, Invocation.InitialIoError, "")
        {
            EntryLength = (int)SearchControlBytes,
            SearchLiteral = new(text, pattern, caseSensitive, found, reject)
        };

    private void PrepareSearchLiteralProbe(Invocation invocation)
    {
        var definition = invocation.Definition.SearchLiteral ??
            throw new InvalidOperationException("Missing Search literal definition.");
        var text = Encoding.Latin1.GetBytes(definition.Text);
        var pattern = Encoding.Latin1.GetBytes(definition.Pattern);
        Require(text.Length <= SearchBufferBytes && pattern.Length <= SearchBufferBytes,
            "Search literal fixture input exceeds bounded control storage.");
        var layout = new SearchLiteralNativeLayout(
            Bus.Allocate(invocation, SearchControlBytes, "SearchLiteralFixture", true),
            Bus.Allocate(invocation, SearchBufferBytes, "SearchLiteralFixture", false),
            Bus.Allocate(invocation, SearchBufferBytes, "SearchLiteralFixture", false),
            new byte[SearchBufferBytes], new byte[SearchBufferBytes]);
        invocation.SearchLiteralLayout = layout;
        layout.ExpectedText.AsSpan().Fill(0xa5);
        layout.ExpectedPattern.AsSpan().Fill(0xa5);
        text.CopyTo(layout.ExpectedText);
        pattern.CopyTo(layout.ExpectedPattern);
        layout.ExpectedText.CopyTo(Bus.Memory, (int)layout.Text);
        layout.ExpectedPattern.CopyTo(Bus.Memory, (int)layout.Pattern);
        Bus.Long(layout.Control, layout.Text);
        Bus.Long(layout.Control + 4, (uint)text.Length);
        Bus.Long(layout.Control + 8, layout.Pattern);
        Bus.Long(layout.Control + 12, (uint)pattern.Length);
        Bus.Long(layout.Control + 16, definition.CaseSensitive ? 1u : 0u);
        Bus.Long(layout.Control + 20, layout.Text);
        Bus.Long(layout.Control + 24, layout.Pattern + SearchBufferBytes - layout.Text);
    }

    private void VerifySearchLiteralProbe(Invocation invocation)
    {
        var definition = invocation.Definition.SearchLiteral ??
            throw new InvalidOperationException("Missing Search literal definition.");
        var layout = invocation.SearchLiteralLayout ??
            throw new InvalidOperationException("Missing Search literal storage.");
        Require(invocation.Opens == 0 && invocation.Closes == 0 &&
            invocation.Reads == 0 && invocation.FreeArgs == 0 &&
            invocation.Allocations == 0 && invocation.FreeMem == 0,
            "Search literal probe must not use DOS or allocate resident state.");
        Require(Bus.Memory.AsSpan((int)layout.Text, (int)SearchBufferBytes)
                .SequenceEqual(layout.ExpectedText) &&
            Bus.Memory.AsSpan((int)layout.Pattern, (int)SearchBufferBytes)
                .SequenceEqual(layout.ExpectedPattern),
            "Search literal probe must not modify either caller-owned range.");
        if (definition.Reject)
        {
            Require(Bus.Long(layout.Control + 28) == 0 && Bus.Long(layout.Control + 32) == 0,
                "Rejected Search literal probe must not publish completion.");
        }
        else
        {
            Require(Bus.Long(layout.Control + 28) == (definition.Found ? 1u : 0u) &&
                Bus.Long(layout.Control + 32) == 0x534C5046,
                "Search literal probe result/control publication differs from the bounded case.");
        }
        Bus.Release(invocation, layout.Pattern, "SearchLiteralFixture", SearchBufferBytes);
        Bus.Release(invocation, layout.Text, "SearchLiteralFixture", SearchBufferBytes);
        Bus.Release(invocation, layout.Control, "SearchLiteralFixture", SearchControlBytes);
        invocation.SearchLiteralLayout = null;
    }
}
