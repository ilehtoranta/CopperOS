using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>The source-ordered body of one successful PatCopy match.</summary>
public static class NativeMorphOSCopyMatchStep
{
    /// <summary>
    /// Call after executing the previous pending DoWork. pathSnapshot must hold
    /// 2048 bytes and fibSnapshot a complete FileInfoBlock. The same buffers
    /// are reused for each match; the next DoWork consumes them before this
    /// method overwrites them. Pending depth advances after the previous work.
    /// Returns the parent-directory failure that the loop checks next time.
    /// </summary>
    public static bool Process(APTR anchor, APTR pathSnapshot, APTR fibSnapshot,
        APTR pattern, APTR warningArguments, int mode, bool all, bool quiet,
        BPTR destination, uint metadataFlags, ref bool first, ref int depth,
        ref BPTR currentDestination, ref uint commandFlags,
        ref int destinationPathSize, ref bool pendingDeep, out bool pendingWork)
    {
        if (pendingDeep) depth = (depth + 1) & 255;
        pendingDeep = false;
        pendingWork = false;
        commandFlags &= ~(1u << 23);
        for (var offset = 0; offset < 2048; offset++)
            APTR.WriteUInt8(pathSnapshot, offset, APTR.ReadUInt8(anchor,
                DosLayout.AnchorPath.PathBuffer + offset));
        for (var offset = 0; offset < FileInfoBlock.SizeInBytes; offset++)
            APTR.WriteUInt8(fibSnapshot, offset, APTR.ReadUInt8(anchor,
                DosLayout.AnchorPath.Info + offset));

        var directory = FileInfoBlock.GetDirEntryType(fibSnapshot.Raw) > 0;
        if (first && directory)
        {
            NativeMorphOSCopyDirectoryEntry.ProcessMatched(anchor, true, all,
                quiet, warningArguments, ref commandFlags,
                out pendingWork, out pendingDeep);
        }
        else if ((APTR.ReadUInt8(anchor, DosLayout.AnchorPath.Flags) &
            (byte)AnchorPathFlags.DidDirectory) != 0)
        {
            NativeMorphOSCopyDirectoryExit.ProcessMatched(anchor, fibSnapshot,
                mode, ref depth, destination, ref currentDestination,
                ref commandFlags, ref destinationPathSize, metadataFlags,
                out pendingWork, out var parentFailed);
            if (parentFailed) return true;
        }
        else if (directory)
        {
            NativeMorphOSCopyDirectoryEntry.ProcessMatched(anchor, false, all,
                quiet, warningArguments, ref commandFlags,
                out pendingWork, out pendingDeep);
        }
        else
        {
            pendingWork = pattern.IsNull || DOS.MatchPatternNoCase(
                CString.FromPointer(pattern.Raw), CString.FromPointer(anchor.Raw +
                    (uint)DosLayout.AnchorPath.Info + FileInfoBlock.FileNameOffset)) != 0;
        }
        first = false;
        return false;
    }
}
