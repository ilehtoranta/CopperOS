using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Direct-control receipt for Copy's source-observed mode gate. It does not
/// exercise ReadArgs, positional target handling, or any command I/O.
/// </summary>
internal sealed record CopyModeSelectionProbeCase(uint Controls, int Inputs,
    bool Valid, int Mode, uint Flags);

internal sealed class CopyModeSelectionNativeLayout(uint control)
{
    public uint Control { get; } = control;
}

internal sealed partial class ProbeFixture
{
    public const string CopyModeSelectionProbeSuite = "copy-mode-selection-native-entry-vector-fixture";

    private List<object> RunCopyModeSelectionProbeCases()
    {
        ProbeCase[] cases =
        [
            Case("copy", NativeMorphOSCopyModeSelection.HasTarget, 1, true,
                NativeMorphOSCopyModeSelection.Copy, 0),
            Case("move", NativeMorphOSCopyModeSelection.MoveMode | NativeMorphOSCopyModeSelection.HasTarget, 1, true,
                NativeMorphOSCopyModeSelection.Move, 0),
            Case("delete-force", NativeMorphOSCopyModeSelection.DeleteMode | NativeMorphOSCopyModeSelection.CompatibilityForce, 1, true,
                NativeMorphOSCopyModeSelection.Delete, NativeMorphOSCopyModeSelection.ForceDelete),
            Case("makedir", NativeMorphOSCopyModeSelection.MakeDirectory, 1, true,
                NativeMorphOSCopyModeSelection.MakeDir, 0),
            Case("hard-link-force", NativeMorphOSCopyModeSelection.HardLink | NativeMorphOSCopyModeSelection.HasTarget | NativeMorphOSCopyModeSelection.CompatibilityForce, 1, true,
                NativeMorphOSCopyModeSelection.Link, NativeMorphOSCopyModeSelection.ForceLink),
            Case("soft-link", NativeMorphOSCopyModeSelection.SoftLinkMode | NativeMorphOSCopyModeSelection.HasTarget, 1, true,
                NativeMorphOSCopyModeSelection.Link, NativeMorphOSCopyModeSelection.SoftLink | NativeMorphOSCopyModeSelection.ForceLink),
            Case("direct-copy", NativeMorphOSCopyModeSelection.Direct | NativeMorphOSCopyModeSelection.HasTarget, 1, true,
                NativeMorphOSCopyModeSelection.Copy, 0),
            Case("conflicting-modes", NativeMorphOSCopyModeSelection.DeleteMode | NativeMorphOSCopyModeSelection.MoveMode, 1, false,
                NativeMorphOSCopyModeSelection.Move, 0),
            Case("direct-many-inputs", NativeMorphOSCopyModeSelection.Direct | NativeMorphOSCopyModeSelection.HasTarget, 2, false,
                NativeMorphOSCopyModeSelection.Copy, 0),
            Case("implicit-makedir", NativeMorphOSCopyModeSelection.MakeDirectory | NativeMorphOSCopyModeSelection.InputOmitted, 0, false,
                NativeMorphOSCopyModeSelection.MakeDir, 0),
            Case("overwrite-conflict", NativeMorphOSCopyModeSelection.ForceOverwrite | NativeMorphOSCopyModeSelection.DontOverwrite | NativeMorphOSCopyModeSelection.HasTarget, 1, false,
                NativeMorphOSCopyModeSelection.Copy, 0),
            Case("direct-delete-force", NativeMorphOSCopyModeSelection.DeleteMode | NativeMorphOSCopyModeSelection.Direct | NativeMorphOSCopyModeSelection.CompatibilityForce, 1, false,
                NativeMorphOSCopyModeSelection.Delete, NativeMorphOSCopyModeSelection.ForceDelete),
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute(
        [
            Case("interleaved-soft-all", NativeMorphOSCopyModeSelection.SoftLinkMode | NativeMorphOSCopyModeSelection.All | NativeMorphOSCopyModeSelection.HasTarget, 1, false,
                NativeMorphOSCopyModeSelection.Link, NativeMorphOSCopyModeSelection.SoftLink | NativeMorphOSCopyModeSelection.ForceLink),
            Case("interleaved-missing-target", 0, 1, false, NativeMorphOSCopyModeSelection.Copy, 0),
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Case(string name, uint controls, int inputs, bool valid,
        int mode, uint flags) => new(name, "", valid ? DOS.RETURN_OK : DOS.RETURN_FAIL,
            Invocation.InitialIoError, "")
        {
            EntryLength = 12,
            CopyModeSelection = new(controls, inputs, valid, mode, flags)
        };

    private void PrepareCopyModeSelectionProbe(Invocation invocation)
    {
        var definition = invocation.Definition.CopyModeSelection ??
            throw new InvalidOperationException("Missing Copy mode-selection definition.");
        var layout = new CopyModeSelectionNativeLayout(Bus.Allocate(invocation, 12,
            "CopyModeControl", true));
        invocation.CopyModeSelectionLayout = layout;
        Bus.Long(layout.Control, definition.Controls);
        Bus.Long(layout.Control + 4, unchecked((uint)definition.Inputs));
    }

    private void VerifyCopyModeSelectionProbe(Invocation invocation)
    {
        var definition = invocation.Definition.CopyModeSelection ??
            throw new InvalidOperationException("Missing Copy mode-selection definition.");
        var layout = invocation.CopyModeSelectionLayout ??
            throw new InvalidOperationException("Missing Copy mode-selection storage.");
        Require(Bus.Long(layout.Control) == (definition.Valid ? 1u : 0u) &&
            unchecked((int)Bus.Long(layout.Control + 4)) == definition.Mode &&
            Bus.Long(layout.Control + 8) == definition.Flags,
            "Copy mode-selection control result differs from the supplied contract.");
        Bus.Release(invocation, layout.Control, "CopyModeControl", 12);
        invocation.CopyModeSelectionLayout = null;
    }
}
