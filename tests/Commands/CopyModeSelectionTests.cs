using CopperOS.Commands.Native;

namespace CopperOS.Commands.Tests;

public sealed class CopyModeSelectionTests
{
    [Fact]
    public void Selects_each_single_source_mode_and_compatibility_force()
    {
        AssertMode(NativeMorphOSCopyModeSelection.Copy, 0, false, false, false, false, false, false, false, 1, true, false, false, false);
        AssertMode(NativeMorphOSCopyModeSelection.Move, 0, false, true, false, false, false, false, false, 1, true, false, false, false);
        AssertMode(NativeMorphOSCopyModeSelection.Delete, NativeMorphOSCopyModeSelection.ForceDelete, true, false, false, false, false, false, false, 1, false, false, false, true);
        AssertMode(NativeMorphOSCopyModeSelection.MakeDir, 0, false, false, true, false, false, false, false, 1, false, false, false, false);
        AssertMode(NativeMorphOSCopyModeSelection.Link, NativeMorphOSCopyModeSelection.SoftLink | NativeMorphOSCopyModeSelection.ForceLink, false, false, false, false, true, false, false, 1, true, false, false, false);
    }

    [Fact]
    public void Rejects_source_observed_conflicts_and_direct_restrictions()
    {
        Assert.False(Select(true, true, false, false, false, false, false, false, 1, false, false, false, false));
        Assert.False(Select(false, false, false, false, true, true, false, false, 1, true, false, false, false));
        Assert.False(Select(false, false, true, false, false, false, false, true, 0, false, false, false, false));
        Assert.False(Select(false, false, false, false, false, false, false, false, 1, false, false, false, false));
        Assert.False(Select(false, false, false, false, false, false, true, false, 2, true, false, false, false));
        Assert.False(Select(false, false, false, false, false, false, true, false, 1, true, true, false, false));
        Assert.False(Select(false, false, false, false, false, false, true, false, 1, true, false, true, false));
        Assert.False(NativeMorphOSCopyModeSelection.TrySelect(
            NativeMorphOSCopyModeSelection.ForceOverwrite |
            NativeMorphOSCopyModeSelection.DontOverwrite |
            NativeMorphOSCopyModeSelection.HasTarget, 1, out _, out _));
        Assert.False(NativeMorphOSCopyModeSelection.TrySelect(
            NativeMorphOSCopyModeSelection.DeleteMode |
            NativeMorphOSCopyModeSelection.Direct |
            NativeMorphOSCopyModeSelection.CompatibilityForce, 1, out _, out _));
    }

    private static void AssertMode(int expectedMode, uint expectedFlags,
        bool deleteMode, bool moveMode, bool makeDir, bool hardLink, bool softLink,
        bool all, bool inputOmitted, int inputCount, bool hasTarget, bool hasPattern,
        bool restricted, bool force)
    {
        Assert.True(NativeMorphOSCopyModeSelection.TrySelect(Controls(deleteMode, moveMode,
            makeDir, hardLink, softLink, all, false, inputOmitted, hasTarget,
            hasPattern, restricted, force), inputCount, out var mode, out var flags));
        Assert.Equal(expectedMode, mode);
        Assert.Equal(expectedFlags, flags);
    }

    private static bool Select(bool deleteMode, bool moveMode, bool makeDir,
        bool hardLink, bool softLink, bool all, bool direct, bool inputOmitted,
        int inputCount, bool hasTarget, bool hasPattern, bool restricted, bool force) =>
        NativeMorphOSCopyModeSelection.TrySelect(Controls(deleteMode, moveMode, makeDir,
            hardLink, softLink, all, direct, inputOmitted, hasTarget, hasPattern,
            restricted, force), inputCount, out _, out _);

    private static uint Controls(bool d,bool m,bool md,bool h,bool s,bool a,bool x,bool o,bool t,bool p,bool r,bool f) =>
        (d?1u:0)*NativeMorphOSCopyModeSelection.DeleteMode | (m?1u:0)*NativeMorphOSCopyModeSelection.MoveMode | (md?1u:0)*NativeMorphOSCopyModeSelection.MakeDirectory | (h?1u:0)*NativeMorphOSCopyModeSelection.HardLink | (s?1u:0)*NativeMorphOSCopyModeSelection.SoftLinkMode | (a?1u:0)*NativeMorphOSCopyModeSelection.All | (x?1u:0)*NativeMorphOSCopyModeSelection.Direct | (o?1u:0)*NativeMorphOSCopyModeSelection.InputOmitted | (t?1u:0)*NativeMorphOSCopyModeSelection.HasTarget | (p?1u:0)*NativeMorphOSCopyModeSelection.HasPattern | (r?1u:0)*NativeMorphOSCopyModeSelection.RestrictedDirectOption | (f?1u:0)*NativeMorphOSCopyModeSelection.CompatibilityForce;
}
