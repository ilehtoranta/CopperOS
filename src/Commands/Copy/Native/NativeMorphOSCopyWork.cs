using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>DoWork's object dispatch; mutable state belongs to the invocation.</summary>
public struct NativeMorphOSCopyWork : INativeMorphOSCopyWork
{
    // The native generic ABI requires a four-byte worker representation.
    public uint Reserved;

    public void Execute(APTR name, ref NativeMorphOSCopyTraversalState state)
    {
        if (!NativeMorphOSCopyWorkPreparation.Prepare(name, ref state)) return;
        var quiet = (state.Flags & 256) != 0;
        var verbose = (state.Flags & 512) != 0;
        var directory = FileInfoBlock.GetDirEntryType(state.Fib.Raw) > 0;
        var sourceNoFs = (state.Flags & (1u << 24)) != 0;
        if ((state.Flags & ((1u << 24) | (1u << 25))) != 0)
        {
            var noLock = BPTR.Null;
            var success = Transfer(ref state, false, ref noLock, out var opened);
            var operation = TransferText(state.Mode, opened);
            if (!success && !quiet)
            {
                if (!sourceNoFs) NativeMorphOSCopyOutput.PrintNotDone(operation, state.WarningArguments);
            }
            else
            {
                state.Flags |= 1u << 22;
                if (verbose && !sourceNoFs)
                {
                    NativeMorphOSCopyOutput.PrintName(name, unchecked((uint)state.Depth), false, true, state.WarningArguments);
                    PrintLine(operation, state.WarningArguments);
                }
            }
            return;
        }

        var source = DOS.LockRaw(CString.FromPointer(state.Path.Raw), DOS.LockMode.Shared);
        if (source.IsNull)
        {
            state.Result = DOS.RETURN_WARN;
            if (!quiet) NativeMorphOSCopyOutput.PrintNotDone("read.", state.WarningArguments);
            return;
        }
        var parent = DOS.ParentDirRaw(source);
        if (parent.IsNull)
        {
            state.Result = DOS.RETURN_ERROR;
            if (state.Mode == NativeMorphOSCopyModeSelection.Delete && !quiet)
            {
                APTR.WriteUInt32(state.WarningArguments, 0, state.Path.Raw);
                DOS.VPrintf(" %s ", state.WarningArguments);
                PrintFailurePrefix("deleted.", state.WarningArguments);
                PrintLine("A device cannot be deleted.", state.WarningArguments);
            }
            DOS.UnLock(source);
            return;
        }
        DOS.UnLock(parent);
        if (!quiet && verbose)
            NativeMorphOSCopyOutput.PrintName(name, unchecked((uint)state.Depth), directory,
                FileInfoBlock.GetDirEntryType(state.Fib.Raw) < 0 ||
                ((state.Flags & 1) != 0 ? state.Mode != NativeMorphOSCopyModeSelection.Delete : state.Mode != NativeMorphOSCopyModeSelection.Copy) ||
                (state.Flags & (1u << 23)) != 0, state.WarningArguments);

        CString error = CString.FromPointer(0);
        CString successText = "";
        if ((state.Flags & (1u << 23)) != 0 ||
            (state.Mode == NativeMorphOSCopyModeSelection.Delete &&
            ((state.Flags & 1) == 0 || FileInfoBlock.GetDirEntryType(state.Fib.Raw) < 0)))
        {
            DOS.UnLock(source);
            source = BPTR.Null;
            if ((state.Flags & 32) != 0) DOS.SetProtection(CString.FromPointer(state.Path.Raw), 0);
            if (DOS.DeleteFile(CString.FromPointer(state.Path.Raw)) != 0) successText = "deleted.";
            else { state.Result = DOS.RETURN_WARN; error = "deleted."; }
        }
        else if (state.Mode == NativeMorphOSCopyModeSelection.Delete)
        {
            // First pass through an ALL directory does no object action.
        }
        else if (directory)
        {
            var outcome = NativeMorphOSCopyDirectoryOperation.Run(source, ref state);
            switch (outcome)
            {
                case NativeMorphOSCopyDirectoryOutcome.Created: successText = "   [created]"; break;
                case NativeMorphOSCopyDirectoryOutcome.Renamed: successText = "renamed."; break;
                case NativeMorphOSCopyDirectoryOutcome.Linked: successText = "linked."; break;
                case NativeMorphOSCopyDirectoryOutcome.CreateFailed: error = "   [created]"; break;
                case NativeMorphOSCopyDirectoryOutcome.EnterFailed: error = "entered"; break;
                case NativeMorphOSCopyDirectoryOutcome.RenameFailed: error = "renamed."; break;
                case NativeMorphOSCopyDirectoryOutcome.LinkFailed: error = "linked."; break;
                case NativeMorphOSCopyDirectoryOutcome.DisplayOnly:
                    successText = CString.FromPointer(0);
                    if (!quiet && verbose) DOS.VPrintf("\n", state.WarningArguments);
                    break;
                case NativeMorphOSCopyDirectoryOutcome.InfiniteLoop:
                case NativeMorphOSCopyDirectoryOutcome.ForceLinkRequired:
                    successText = CString.FromPointer(0);
                    if (!quiet)
                    {
                        if (!verbose) NativeMorphOSCopyOutput.PrintName(name, unchecked((uint)state.Depth), true, true, state.WarningArguments);
                        if (outcome == NativeMorphOSCopyDirectoryOutcome.InfiniteLoop)
                        {
                            PrintFailurePrefix("entered", state.WarningArguments);
                            DOS.VPrintf("Infinite loop not allowed.\n", state.WarningArguments);
                        }
                        else
                        {
                            PrintFailurePrefix("linked.", state.WarningArguments);
                            DOS.VPrintf("FORCELINK keyword required.\n", state.WarningArguments);
                        }
                    }
                    break;
            }
        }
        else
        {
            if (NativeMorphOSCopyDestination.Prepare(state.DestinationName, false,
                (state.Flags & 128) != 0, (state.Flags & 64) != 0, state.ExtendedExamine, state.ExamineTags, out _) < 0)
                error = "opened for output";
            else if (state.Mode == NativeMorphOSCopyModeSelection.Move &&
                DOS.Rename(CString.FromPointer(state.Path.Raw), CString.FromPointer(state.DestinationName.Raw)) != 0)
                successText = "renamed.";
            else if (state.Mode == NativeMorphOSCopyModeSelection.Link)
            {
                var soft = (state.Flags & (1u << 20)) != 0;
                if (!soft && NativeMorphOSCopyLinkOperation.Run(source, state.DestinationName, false))
                    successText = "linked.";
                else
                {
                    error = "linked.";
                    state.Result = DOS.RETURN_WARN;
                    if (soft) DOS.SetIoErr(DOS.Error.ObjectWrongType);
                }
            }
            else
            {
                var success = Transfer(ref state, true, ref source, out var opened);
                var operation = TransferText(state.Mode, opened);
                if (success) successText = operation;
                else { error = operation; state.Result = DOS.RETURN_WARN; }
            }
        }

        if (CString.ToUInt32(error) != 0 && !quiet)
            NativeMorphOSCopyOutput.PrintNotDone(error, state.WarningArguments);
        else if (CString.ToUInt32(successText) != 0)
        {
            state.Flags |= 1u << 22;
            if (!quiet && verbose) PrintLine(successText, state.WarningArguments);
            if (FileInfoBlock.GetDirEntryType(state.Fib.Raw) < 0)
                NativeMorphOSCopyMetadata.Apply(state.DestinationName, state.Fib, state.MetadataFlags);
        }
        if (source.IsNotNull) DOS.UnLock(source);
    }

    private static bool Transfer(ref NativeMorphOSCopyTraversalState state, bool release,
        ref BPTR source, out bool opened) => NativeMorphOSCopyFileOperation.Run(
            state.Path, state.DestinationName, state.Mode == NativeMorphOSCopyModeSelection.Move,
            (state.Flags & 32) != 0, release, ref source, state.BufferSize,
            state.ExtendedExamine, ref state.CopyBuffer, ref state.CopyBufferBytes, out opened);

    private static CString TransferText(int mode, bool opened)
    {
        if (!opened) return "opened for output";
        if (mode == NativeMorphOSCopyModeSelection.Move) return "moved.";
        return "copied.";
    }

    private static void PrintLine(CString text, APTR arguments)
    {
        APTR.WriteUInt32(arguments, 0, CString.ToUInt32(text));
        DOS.VPrintf("%s\n", arguments);
    }

    private static void PrintFailurePrefix(CString text, APTR arguments)
    {
        APTR.WriteUInt32(arguments, 0, CString.ToUInt32(text));
        DOS.VPrintf(" not %s: ", arguments);
    }
}
