using System.Text;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>Direct resident control-block cases for the bounded Type text formatter.</summary>
internal sealed record TypeTextProbeCase(string Source, bool Number, bool NoLine,
    string Output, uint Capacity = 256, bool Reject = false);

internal sealed record TypeTextNativeLayout(uint Control, uint Source,
    uint Destination);

internal sealed partial class ProbeFixture
{
    public const string TypeTextProbeSuite = "type-text-probe-fixture";
    private const uint TypeControlBytes = 44;
    private const uint TypeBufferBytes = 256;

    private List<object> RunTypeTextProbeCases()
    {
        ProbeCase[] cases =
        [
            Type("empty-adds-lf", "", false, false, "\n"),
            Type("unterminated", "abc", false, false, "abc\n"),
            Type("noline", "abc", false, true, "abc"),
            Type("numbered-lines", "a\nb\n", true, false, "    1 a\n    2 b\n"),
            Type("numbered-noline", "a\nb", true, true, "    1 a\n    2 b"),
            Type("capacity-failure", "abc", false, false, "", 3, true),
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[3] with { Name = "repeat-numbered-lines" },
            cases[2] with { Name = "repeat-noline" }
        ], true));
        reports.AddRange(Execute([
            cases[5] with { Name = "interleaved-capacity-failure", StackBytes = 4096 },
            cases[1] with { Name = "interleaved-unterminated" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Type(string name, string source, bool number,
        bool noLine, string output, uint capacity = TypeBufferBytes,
        bool reject = false) => new(name, "", reject ? 10 : 0,
            Invocation.InitialIoError, "")
        {
            EntryLength = (int)TypeControlBytes,
            TypeText = new(source, number, noLine, output, capacity, reject)
        };

    private void PrepareTypeTextProbe(Invocation invocation)
    {
        var definition = invocation.Definition.TypeText ??
            throw new InvalidOperationException("Missing Type text definition.");
        var source = Encoding.Latin1.GetBytes(definition.Source);
        Require(source.Length <= TypeBufferBytes && definition.Capacity <= TypeBufferBytes,
            "Type text fixture input exceeds bounded control storage.");
        var layout = new TypeTextNativeLayout(
            Bus.Allocate(invocation, TypeControlBytes, "TypeTextFixture", true),
            Bus.Allocate(invocation, TypeBufferBytes, "TypeTextFixture", false),
            Bus.Allocate(invocation, TypeBufferBytes, "TypeTextFixture", false));
        invocation.TypeTextLayout = layout;
        Bus.Memory.AsSpan((int)layout.Source, (int)TypeBufferBytes).Fill(0xa5);
        Bus.Memory.AsSpan((int)layout.Destination, (int)TypeBufferBytes).Fill(0xa5);
        source.CopyTo(Bus.Memory.AsSpan((int)layout.Source));
        Bus.Long(layout.Control, layout.Source);
        Bus.Long(layout.Control + 4, (uint)source.Length);
        Bus.Long(layout.Control + 8, definition.Number ? 1u : 0u);
        Bus.Long(layout.Control + 12, definition.NoLine ? 1u : 0u);
        Bus.Long(layout.Control + 16, layout.Destination);
        Bus.Long(layout.Control + 20, definition.Capacity);
        Bus.Long(layout.Control + 28, layout.Source);
        Bus.Long(layout.Control + 32, layout.Destination + TypeBufferBytes - layout.Source);
    }

    private void VerifyTypeTextProbe(Invocation invocation)
    {
        var definition = invocation.Definition.TypeText ??
            throw new InvalidOperationException("Missing Type text definition.");
        var layout = invocation.TypeTextLayout ??
            throw new InvalidOperationException("Missing Type text storage.");
        Require(invocation.Opens == 0 && invocation.Closes == 0 &&
            invocation.Reads == 0 && invocation.FreeArgs == 0 &&
            invocation.Allocations == 0 && invocation.FreeMem == 0,
            "Type text probe must not use DOS or allocate resident state.");
        if (definition.Reject)
        {
            Require(Bus.Long(layout.Control + 24) == 0 && Bus.Long(layout.Control + 40) == 0 &&
                Bus.Memory.AsSpan((int)layout.Destination, (int)TypeBufferBytes)
                    .IndexOfAnyExcept((byte)0xa5) < 0,
                "Rejected Type text probe must not publish completion or modify destination.");
            ReleaseTypeTextStorage(invocation, layout);
            return;
        }
        var expected = Encoding.Latin1.GetBytes(definition.Output);
        Require(Bus.Long(layout.Control + 24) == expected.Length &&
            Bus.Memory.AsSpan((int)layout.Destination, expected.Length).SequenceEqual(expected) &&
            Bus.Long(layout.Control + 40) == 0x54545046,
            "Type text probe output/control publication differs from the bounded case.");
        ReleaseTypeTextStorage(invocation, layout);
    }

    private void ReleaseTypeTextStorage(Invocation invocation,
        TypeTextNativeLayout layout)
    {
        Bus.Release(invocation, layout.Destination, "TypeTextFixture", TypeBufferBytes);
        Bus.Release(invocation, layout.Source, "TypeTextFixture", TypeBufferBytes);
        Bus.Release(invocation, layout.Control, "TypeTextFixture", TypeControlBytes);
        invocation.TypeTextLayout = null;
    }
}
