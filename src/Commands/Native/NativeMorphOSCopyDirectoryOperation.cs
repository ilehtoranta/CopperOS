using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Outcomes consumed by the original DoWork diagnostic branches.</summary>
public enum NativeMorphOSCopyDirectoryOutcome
{
    DisplayOnly, Entered, Created, Renamed, Linked,
    InfiniteLoop, CreateFailed, EnterFailed, RenameFailed, ForceLinkRequired, LinkFailed
}

/// <summary>DoWork's directory branch after source/parent validation.</summary>
public static class NativeMorphOSCopyDirectoryOperation
{
    /// <summary>
    /// The source lock remains caller-owned. Updates CurDest and path cache
    /// only when ALL selects directory traversal. The caller owns reporting,
    /// DONE and final source-lock release for every outcome.
    /// </summary>
    public static NativeMorphOSCopyDirectoryOutcome Run(BPTR source,
        ref NativeMorphOSCopyTraversalState state)
    {
        var all = (state.Flags & 1) != 0;
        if ((all || state.Mode == NativeMorphOSCopyModeSelection.Move ||
            state.Mode == NativeMorphOSCopyModeSelection.Link) &&
            NativeMorphOSCopyLoopGuard.HasLoop(source, state.CurrentDestination))
        {
            state.Result = DOS.RETURN_ERROR;
            return NativeMorphOSCopyDirectoryOutcome.InfiniteLoop;
        }
        var target = NativeMorphOSCopyDestination.Prepare(state.DestinationName,
            true, (state.Flags & 128) != 0, (state.Flags & 64) != 0, state.ExtendedExamine, state.ExamineTags, out _);
        if (target < 0)
        {
            state.Result = DOS.RETURN_ERROR;
            return NativeMorphOSCopyDirectoryOutcome.CreateFailed;
        }
        if (all)
        {
            var previous = state.CurrentDestination;
            state.DestinationPathSize = 0;
            var outcome = NativeMorphOSCopyDirectoryOutcome.Entered;
            if (target == NativeMorphOSCopyDestination.Directory)
            {
                state.CurrentDestination = DOS.LockRaw(
                    CString.FromPointer(state.DestinationName.Raw), DOS.LockMode.Shared);
                if (state.CurrentDestination.IsNull)
                    outcome = NativeMorphOSCopyDirectoryOutcome.EnterFailed;
            }
            else
            {
                state.CurrentDestination = DOS.CreateDirRaw(CString.FromPointer(state.DestinationName.Raw));
                if (state.CurrentDestination.IsNull)
                    outcome = NativeMorphOSCopyDirectoryOutcome.CreateFailed;
                else
                {
                    DOS.UnLock(state.CurrentDestination);
                    state.CurrentDestination = DOS.LockRaw(
                        CString.FromPointer(state.DestinationName.Raw), DOS.LockMode.Shared);
                    outcome = state.CurrentDestination.IsNull
                        ? NativeMorphOSCopyDirectoryOutcome.EnterFailed
                        : NativeMorphOSCopyDirectoryOutcome.Created;
                }
            }
            if (state.CurrentDestination.IsNull)
            {
                state.Result = DOS.RETURN_ERROR;
                state.CurrentDestination = previous;
            }
            else if (previous.Raw != state.Destination.Raw)
                DOS.UnLock(previous);
            return outcome;
        }
        if (state.Mode == NativeMorphOSCopyModeSelection.Move)
        {
            if (DOS.Rename(CString.FromPointer(state.Path.Raw),
                CString.FromPointer(state.DestinationName.Raw)) != 0)
                return NativeMorphOSCopyDirectoryOutcome.Renamed;
            state.Result = DOS.RETURN_WARN;
            return NativeMorphOSCopyDirectoryOutcome.RenameFailed;
        }
        if (state.Mode == NativeMorphOSCopyModeSelection.Link)
        {
            if ((state.Flags & 16) == 0)
            {
                state.Result = DOS.RETURN_WARN;
                return NativeMorphOSCopyDirectoryOutcome.ForceLinkRequired;
            }
            if (NativeMorphOSCopyLinkOperation.Run(source, state.DestinationName,
                (state.Flags & (1u << 20)) != 0))
                return NativeMorphOSCopyDirectoryOutcome.Linked;
            state.Result = DOS.RETURN_WARN;
            return NativeMorphOSCopyDirectoryOutcome.LinkFailed;
        }
        return NativeMorphOSCopyDirectoryOutcome.DisplayOnly;
    }
}
