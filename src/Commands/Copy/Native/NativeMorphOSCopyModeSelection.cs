namespace CopperOS.Commands.Native;

/// <summary>
/// Source-observed MorphOS Copy mode selection and invalid-combination gate.
/// The caller normalizes positional TO before calling this routine and owns all
/// parser, matcher, filesystem, metadata, and output behavior.
/// </summary>
public static class NativeMorphOSCopyModeSelection
{
    public const int Copy = 0;
    public const int Move = 1;
    public const int Delete = 2;
    public const int MakeDir = 3;
    public const int Link = 4;

    public const uint ForceLink = 1u << 4;
    public const uint ForceDelete = 1u << 5;
    public const uint SoftLink = 1u << 20;
    public const uint DeleteMode = 1u << 0, MoveMode = 1u << 1, MakeDirectory = 1u << 2,
        HardLink = 1u << 3, SoftLinkMode = 1u << 4, All = 1u << 5, Direct = 1u << 6,
        InputOmitted = 1u << 7, HasTarget = 1u << 8, HasPattern = 1u << 9,
        RestrictedDirectOption = 1u << 10, CompatibilityForce = 1u << 11,
        ForceOverwrite = 1u << 12, DontOverwrite = 1u << 13;

    /// <summary>
    /// Selects one source mode. `hasRestrictedDirectOption` represents parsed
    /// Copy options not allowed by DIRECT after the source's always-present
    /// protection, QUIET, VERBOSE, and ERRWARN flags are removed.
    /// </summary>
    public static bool TrySelect(uint controls, int inputCount, out int mode, out uint flags)
    {
        var deleteMode = (controls & DeleteMode) != 0;
        var moveMode = (controls & MoveMode) != 0;
        var makeDir = (controls & MakeDirectory) != 0;
        var hardLink = (controls & HardLink) != 0;
        var softLink = (controls & SoftLinkMode) != 0;
        var all = (controls & All) != 0;
        var direct = (controls & Direct) != 0;
        var inputOmitted = (controls & InputOmitted) != 0;
        var hasTarget = (controls & HasTarget) != 0;
        var hasPattern = (controls & HasPattern) != 0;
        var hasRestrictedDirectOption = (controls & RestrictedDirectOption) != 0;
        var compatibilityForce = (controls & CompatibilityForce) != 0;
        var forceOverwrite = (controls & ForceOverwrite) != 0;
        var dontOverwrite = (controls & DontOverwrite) != 0;
        var compatibilityForceChangesFlags = compatibilityForce &&
            (deleteMode || hardLink || softLink);
        mode = Copy;
        flags = 0;
        var selections = 0;
        if (deleteMode) { selections++; mode = Delete; }
        if (moveMode) { selections++; mode = Move; }
        if (makeDir) { selections++; mode = MakeDir; }
        if (hardLink) { selections++; mode = Link; }
        if (softLink) { selections++; mode = Link; flags |= SoftLink | ForceLink; }
        if (compatibilityForce)
        {
            if (deleteMode) flags |= ForceDelete;
            if (hardLink || softLink) flags |= ForceLink;
        }
        if (selections > 1 || inputCount < 0 ||
            inputOmitted && mode == MakeDir || softLink && all ||
            dontOverwrite && forceOverwrite ||
            !hasTarget && mode != Delete && mode != MakeDir ||
            direct && (inputOmitted || inputCount == 0 || hasPattern ||
                hasRestrictedDirectOption || compatibilityForceChangesFlags || mode != Delete &&
                (mode != Copy || !hasTarget || inputCount != 1)))
            return false;
        return true;
    }
}
