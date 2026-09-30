using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Bounded directory-entry transition from MorphOS Copy PatCopy.</summary>
public static class NativeMorphOSCopyDirectoryEntry
{
    /// <summary>
    /// Processes a directory entry directly from the live matcher. The caller
    /// must dispatch first-directory entries before DIDDIR, and DIDDIR before
    /// ordinary entries, following PatCopy's branch order.
    /// </summary>
    public static void ProcessMatched(APTR anchor, bool first, bool all,
        bool quiet, APTR warningArguments, ref uint commandFlags,
        out bool dispatchWork, out bool enterDeep)
    {
        var allowed = true;
        if (first)
            commandFlags |= 512; // COPYFLAG_VERBOSE
        else if (all)
        {
            var current = APTR.FromPointer(APTR.ReadUInt32(anchor,
                DosLayout.AnchorPath.Current));
            var directory = BPTR.FromRaw(APTR.ReadUInt32(current,
                DosLayout.AChain.Lock));
            var name = APTR.FromPointer(anchor.Raw +
                (uint)DosLayout.AnchorPath.Info + FileInfoBlock.FileNameOffset);
            // Delete's FOLLOWLINKS flag shares this command-state bit with
            // the worker.  When set, do not reject a directory-link descent
            // through the dangling-link warning probe.
            allowed = (commandFlags & (1u << 26)) != 0 ||
                NativeMorphOSCopySoftLinkCheck.CanEnter(directory, name,
                    quiet, warningArguments);
        }
        Process(anchor, first, all, allowed, out dispatchWork, out enterDeep);
    }

    /// <summary>
    /// Applies PatCopy's directory branches after the current AnchorPath and
    /// FIB were copied. The first directory is entered for old Copy syntax but
    /// has no deferred DoWork; later directories defer work and enter only when
    /// ALL is selected and the caller's soft-link check allows entry. The
    /// caller performs the next-iteration Deep increment and applies VERBOSE
    /// for the first directory. Call only for an entry, never APF_DIDDIR.
    /// </summary>
    public static void Process(APTR anchor, bool first, bool all, bool entryAllowed,
        out bool dispatchWork, out bool enterDeep)
    {
        dispatchWork = false;
        enterDeep = false;
        var flags = APTR.ReadUInt8(anchor, DosLayout.AnchorPath.Flags);
        if (first)
        {
            APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                (byte)(flags | (byte)AnchorPathFlags.DoDirectory));
            return;
        }
        dispatchWork = true;
        if (!all || !entryAllowed) return;
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
            (byte)(flags | (byte)AnchorPathFlags.DoDirectory));
        enterDeep = true;
    }
}
