using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record CopyPatternClassifierProbeCase(int MatchFirstResult,
    bool IsWild, int Result, int MatchEnds);

internal sealed class CopyPatternClassifierNativeLayout(uint control)
{
    public uint Control { get; } = control;
    public int MatchFirsts, MatchEnds;
    public bool Contains(uint address, int size) => address >= Control &&
        (ulong)address + (uint)size <= (ulong)Control + 512;
}

internal sealed partial class ProbeFixture
{
    public const string CopyPatternClassifierProbeSuite =
        "copy-pattern-classifier-native-entry-vector-fixture";

    private List<object> RunCopyPatternClassifierProbeCases()
    {
        var cases = new[]
        {
            C("wild", 0, true, 1, 1),
            C("literal", 0, false, 0, 1),
            C("matcher-failure", (int)DOS.Error.ObjectNotFound, false, -1, 0),
        };
        var results = new List<object>();
        foreach (var item in cases) results.AddRange(Execute([item], false));
        results.AddRange(Execute([
            cases[0] with { Name = "interleaved-wild" },
            cases[1] with { Name = "interleaved-literal" },
        ], true));
        Bus.AssertImageUnchanged();
        return results;
    }

    private static ProbeCase C(string name, int first, bool wild, int result,
        int ends) => new(name, "", DOS.RETURN_OK, 0, "")
    {
        EntryLength = 12,
        CopyPatternClassifier = new(first, wild, result, ends),
    };

    private void PrepareCopyPatternClassifierProbe(Invocation invocation)
    {
        var control = invocation.Arguments;
        Bus.Memory.AsSpan((int)control, 512).Clear();
        invocation.CopyPatternClassifierLayout = new(control);
        Encoding.Latin1.GetBytes("SYS:#?").CopyTo(Bus.Memory.AsSpan((int)(control + 64)));
        Bus.Long(control, control + 64);
        Bus.Long(control + 4, control + 128);
    }

    private void VerifyCopyPatternClassifierProbe(Invocation invocation)
    {
        var expected = invocation.Definition.CopyPatternClassifier!;
        var layout = invocation.CopyPatternClassifierLayout!;
        Require(unchecked((int)Bus.Long(layout.Control + 8)) == expected.Result &&
            layout.MatchFirsts == 1 && layout.MatchEnds == expected.MatchEnds,
            $"{invocation.Definition.Name}: Copy IsMatchPattern lifecycle differs.");
        invocation.CopyPatternClassifierLayout = null;
    }

    private void RegisterCopyPatternClassifierProbeDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state, invocation) =>
        {
            var expected = invocation.Definition.CopyPatternClassifier!;
            var layout = invocation.CopyPatternClassifierLayout!;
            var anchor = state.D[2];
            Require(Bus.CString(state.D[1]) == "SYS:#?" &&
                Bus.Long(anchor + (uint)DosLayout.AnchorPath.BreakBits) == 0 &&
                Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] ==
                    (byte)AnchorPathFlags.DoWild &&
                Bus.Word(anchor + (uint)DosLayout.AnchorPath.StringLength) == 0,
                "Copy IsMatchPattern MatchFirst ABI differs.");
            layout.MatchFirsts++;
            if (expected.MatchFirstResult != 0)
                return unchecked((uint)expected.MatchFirstResult);
            Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] =
                (byte)(AnchorPathFlags.DoWild | (expected.IsWild
                    ? AnchorPathFlags.IsWild : AnchorPathFlags.None));
            return 0;
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state, invocation) =>
        {
            var layout = invocation.CopyPatternClassifierLayout!;
            Require(state.D[1] == layout.Control + 128,
                "Copy IsMatchPattern MatchEnd ABI differs.");
            layout.MatchEnds++;
            return 0;
        });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }
}
