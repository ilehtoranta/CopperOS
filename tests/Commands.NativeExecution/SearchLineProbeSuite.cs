using System.Text;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record SearchLineProbeCase(string Source, string Pattern,
    bool CaseSensitive, bool NoNumber, bool Quiet, uint LinesAfter,
    bool Found, string Output, uint Capacity = 256, bool Reject = false);

internal sealed record SearchLineNativeLayout(uint Control, uint Source,
    uint Pattern, uint Destination, byte[] ExpectedSource, byte[] ExpectedPattern);

internal sealed partial class ProbeFixture
{
    public const string SearchLineProbeSuite = "search-line-probe-fixture";
    private const uint SearchLineControlBytes = 60;
    private const uint SearchLineBufferBytes = 256;

    private List<object> RunSearchLineProbeCases()
    {
        ProbeCase[] cases =
        [
            Line("match-following", "first\nneedle\nafter", "needle", true, false, false, 1, true, "     2> needle\n     3: after\n"),
            Line("nonum", "needle\nnone\nneedle", "needle", true, true, false, 0, true, "needle\nneedle\n"),
            Line("quiet-nocase", "none\nNeedle\nneedle", "needle", false, false, true, 2, true, ""),
            Line("control", "x\0needle\tvalue\rneedle", "needle", true, false, false, 0, true, "     1> needle.value\n     1> needle\n"),
            Line("nonmatch", "alpha", "beta", true, false, false, 0, false, ""),
            Line("short-output", "needle", "needle", true, false, false, 0, false, "", 4, true),
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([cases[0] with { Name = "repeat-match" }, cases[4] with { Name = "repeat-nonmatch" }], true));
        reports.AddRange(Execute([cases[5] with { Name = "interleaved-short", StackBytes = 4096 }, cases[1] with { Name = "interleaved-nonum" }], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Line(string name, string source, string pattern,
        bool caseSensitive, bool noNumber, bool quiet, uint linesAfter,
        bool found, string output, uint capacity = 256, bool reject = false) =>
        new(name, "", reject ? 10 : 0, Invocation.InitialIoError, "")
        {
            EntryLength = (int)SearchLineControlBytes,
            SearchLine = new(source, pattern, caseSensitive, noNumber, quiet,
                linesAfter, found, output, capacity, reject)
        };

    private void PrepareSearchLineProbe(Invocation invocation)
    {
        var definition = invocation.Definition.SearchLine ?? throw new InvalidOperationException("Missing Search line definition.");
        var source = Encoding.Latin1.GetBytes(definition.Source);
        var pattern = Encoding.Latin1.GetBytes(definition.Pattern);
        Require(source.Length <= SearchLineBufferBytes && pattern.Length <= SearchLineBufferBytes && definition.Capacity <= SearchLineBufferBytes, "Search line fixture input exceeds bounded control storage.");
        var layout = new SearchLineNativeLayout(Bus.Allocate(invocation, SearchLineControlBytes, "SearchLineFixture", true), Bus.Allocate(invocation, SearchLineBufferBytes, "SearchLineFixture", false), Bus.Allocate(invocation, SearchLineBufferBytes, "SearchLineFixture", false), Bus.Allocate(invocation, SearchLineBufferBytes, "SearchLineFixture", false), new byte[SearchLineBufferBytes], new byte[SearchLineBufferBytes]);
        invocation.SearchLineLayout = layout;
        layout.ExpectedSource.AsSpan().Fill(0xa5); layout.ExpectedPattern.AsSpan().Fill(0xa5);
        source.CopyTo(layout.ExpectedSource); pattern.CopyTo(layout.ExpectedPattern);
        layout.ExpectedSource.CopyTo(Bus.Memory, (int)layout.Source); layout.ExpectedPattern.CopyTo(Bus.Memory, (int)layout.Pattern);
        Bus.Memory.AsSpan((int)layout.Destination, (int)SearchLineBufferBytes).Fill(0xa5);
        Bus.Long(layout.Control, layout.Source); Bus.Long(layout.Control + 4, (uint)source.Length); Bus.Long(layout.Control + 8, layout.Pattern); Bus.Long(layout.Control + 12, (uint)pattern.Length);
        Bus.Long(layout.Control + 16, definition.CaseSensitive ? 1u : 0u); Bus.Long(layout.Control + 20, definition.NoNumber ? 1u : 0u); Bus.Long(layout.Control + 24, definition.Quiet ? 1u : 0u); Bus.Long(layout.Control + 28, definition.LinesAfter);
        Bus.Long(layout.Control + 32, layout.Destination); Bus.Long(layout.Control + 36, definition.Capacity); Bus.Long(layout.Control + 40, layout.Source); Bus.Long(layout.Control + 44, layout.Destination + SearchLineBufferBytes - layout.Source);
    }

    private void VerifySearchLineProbe(Invocation invocation)
    {
        var definition = invocation.Definition.SearchLine ?? throw new InvalidOperationException("Missing Search line definition.");
        var layout = invocation.SearchLineLayout ?? throw new InvalidOperationException("Missing Search line storage.");
        Require(invocation.Opens == 0 && invocation.Closes == 0 && invocation.Reads == 0 && invocation.FreeArgs == 0 && invocation.Allocations == 0 && invocation.FreeMem == 0, "Search line probe must not use DOS or allocate resident state.");
        Require(Bus.Memory.AsSpan((int)layout.Source, (int)SearchLineBufferBytes).SequenceEqual(layout.ExpectedSource) && Bus.Memory.AsSpan((int)layout.Pattern, (int)SearchLineBufferBytes).SequenceEqual(layout.ExpectedPattern), "Search line probe must not modify input ranges.");
        if (definition.Reject) Require(Bus.Long(layout.Control + 48) == 0 && Bus.Long(layout.Control + 52) == 0 && Bus.Long(layout.Control + 56) == 0 && Bus.Memory.AsSpan((int)layout.Destination, (int)SearchLineBufferBytes).IndexOfAnyExcept((byte)0xa5) < 0, "Rejected Search line probe must not publish output.");
        else { var expected = Encoding.Latin1.GetBytes(definition.Output); Require(Bus.Long(layout.Control + 48) == (definition.Found ? 1u : 0u) && Bus.Long(layout.Control + 52) == expected.Length && Bus.Long(layout.Control + 56) == 0x534C4646 && Bus.Memory.AsSpan((int)layout.Destination, expected.Length).SequenceEqual(expected), "Search line probe output/control publication differs from the bounded case."); }
        Bus.Release(invocation, layout.Destination, "SearchLineFixture", SearchLineBufferBytes); Bus.Release(invocation, layout.Pattern, "SearchLineFixture", SearchLineBufferBytes); Bus.Release(invocation, layout.Source, "SearchLineFixture", SearchLineBufferBytes); Bus.Release(invocation, layout.Control, "SearchLineFixture", SearchLineControlBytes); invocation.SearchLineLayout = null;
    }
}
