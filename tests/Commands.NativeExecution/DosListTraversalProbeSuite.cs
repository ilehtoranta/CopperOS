using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record DosListTraversalProbeCase(uint Kind, int Entries,
    bool Acquire, bool BreakAfterFirst = false);
internal sealed record DosListTraversalNativeLayout(uint Control);

internal sealed partial class ProbeFixture
{
    public const string DosListTraversalProbeSuite = "doslist-traversal-probe-fixture";
    private const uint ControlBytes = 24;

    private List<object> RunDosListTraversalProbeCases()
    {
        ProbeCase[] cases = [Case("lock-refused", 4, 0, false), Case("empty", 4, 0, true), Case("devices", 4, 3, true), Case("break", 4, 3, true, true)];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([cases[2] with { Name = "repeat-devices" }, cases[0] with { Name = "repeat-refused" }], true));
        Bus.AssertImageUnchanged(); return reports;
    }

    private static ProbeCase Case(string name, uint kind, int entries, bool acquire, bool breakAfterFirst = false) => new(name, "", breakAfterFirst ? 10 : 0, breakAfterFirst ? (int)Amiga.DOS.Error.Break : Invocation.InitialIoError, "") { EntryLength = (int)ControlBytes, DosListTraversal = new(kind, entries, acquire, breakAfterFirst) };

    private void PrepareDosListTraversalProbe(Invocation invocation)
    {
        var definition = invocation.Definition.DosListTraversal ?? throw new InvalidOperationException("Missing DOS list traversal definition.");
        var layout = new DosListTraversalNativeLayout(Bus.Allocate(invocation, ControlBytes, "DosListTraversalFixture", true));
        invocation.DosListTraversalLayout = layout; Bus.Long(layout.Control, definition.Kind); Bus.Long(layout.Control + 4, invocation.DosBase);
    }

    private void VerifyDosListTraversalProbe(Invocation invocation)
    {
        var definition = invocation.Definition.DosListTraversal ?? throw new InvalidOperationException("Missing DOS list traversal definition.");
        var layout = invocation.DosListTraversalLayout ?? throw new InvalidOperationException("Missing DOS list traversal storage.");
        Require(invocation.Opens == 0 && invocation.Closes == 0 && invocation.Allocations == 0 && invocation.FreeMem == 0, "DOS list traversal must not acquire unrelated state.");
        Require(Bus.Long(layout.Control + 8) == (uint)(definition.BreakAfterFirst ? 1 : definition.Entries) && Bus.Long(layout.Control + 12) == (definition.Acquire ? 1u : 0u) && Bus.Long(layout.Control + 16) == (uint)(definition.BreakAfterFirst ? (int)Amiga.DOS.Error.Break : 0) && Bus.Long(layout.Control + 20) == 0x444C5450, "DOS list traversal control publication differs.");
        Require(invocation.DosListLocks == 1 && invocation.DosListUnlocks == (definition.Acquire ? 1 : 0) && invocation.DosListNext == (definition.BreakAfterFirst ? 1 : definition.Acquire ? definition.Entries + 1 : 0), $"DOS list lock lifecycle differs: locks={invocation.DosListLocks}, unlocks={invocation.DosListUnlocks}, next={invocation.DosListNext}, signals={invocation.DosListSignals}.");
        Bus.Release(invocation, layout.Control, "DosListTraversalFixture", ControlBytes); invocation.DosListTraversalLayout = null;
    }

    private void RegisterDosListTraversalExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
        {
            var definition = invocation.Definition.DosListTraversal ?? throw new InvalidOperationException("Missing DOS list traversal definition.");
            Require(state.D[0] == 0 && state.D[1] == (1u << 12), "DOS list traversal signal ABI differs.");
            invocation.DosListSignals++; return definition.BreakAfterFirst && invocation.DosListSignals == 1 ? 1u << 12 : 0;
        });
    }

    private void RegisterDosListTraversalDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.AttemptLockDosList, "AttemptLockDosList", (state, invocation) => { var d = invocation.Definition.DosListTraversal!; Require(state.D[1] == (d.Kind | 1), "DOS list lock flags differ."); invocation.DosListLocks++; return d.Acquire ? 0x30000u : 0; });
        Register(baseAddress, DosLvo.NextDosEntry, "NextDosEntry", (state, invocation) => { var d = invocation.Definition.DosListTraversal!; Require(state.D[2] == d.Kind, "DOS list kind differs."); invocation.DosListNext++; return invocation.DosListNext <= d.Entries ? (uint)(0x30000 + invocation.DosListNext * 4) : 0; });
        Register(baseAddress, DosLvo.UnLockDosList, "UnLockDosList", (state, invocation) => { var d = invocation.Definition.DosListTraversal!; Require(state.D[1] == (d.Kind | 1), "DOS list unlock flags differ."); invocation.DosListUnlocks++; return 0; });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) => { invocation.IoError = unchecked((int)state.D[1]); return 0; });
    }
}
