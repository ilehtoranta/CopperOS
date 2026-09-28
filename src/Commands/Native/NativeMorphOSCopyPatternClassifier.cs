using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-observed Copy wildcard classifier. The caller owns the complete
/// AnchorPath storage so this small stage does not choose Copy's workspace or
/// traversal lifetime.
/// </summary>
public static class NativeMorphOSCopyPatternClassifier
{
    /// <summary>
    /// Matches the source's <c>IsMatchPattern</c> result convention: one for a
    /// wildcard, zero for a literal, and minus one when MatchFirst cannot
    /// classify the name. MatchEnd is owned only after a successful MatchFirst.
    /// </summary>
    public static int IsMatchPattern(CString name, APTR anchor)
    {
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
            (byte)AnchorPathFlags.DoWild);
        APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength, 0);

        if (DOS.MatchFirst(name, anchor) != 0) return -1;
        var matched = (APTR.ReadUInt8(anchor, DosLayout.AnchorPath.Flags) &
            (byte)AnchorPathFlags.IsWild) != 0 ? 1 : 0;
        DOS.MatchEnd(anchor);
        return matched;
    }
}
