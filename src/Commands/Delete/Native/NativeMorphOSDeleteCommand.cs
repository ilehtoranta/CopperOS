using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 Delete command frontend. The matcher is shared with the
/// source-ordered Copy DELETE path and uses a deletion-only object worker.
/// This entry owns the Delete-specific ReadArgs lease and invocation workspace.
///
/// Parent-protection retry, exact packed-binary diagnostics, and the complete
/// hard/soft-link policy still require their own reference captures.
/// </summary>
public static class NativeMorphOSDeleteCommand
{
    public const string Template =
        "FILE/M/A,ALL/S,QUIET/S,FORCE/S,FOLLOWLINKS/S";
    public const uint ResultCount = 5;
    public const string WorkbenchTemplate = "FILE/M/A,ALL/S,QUIET/S,FORCE/S";
    public const uint WorkbenchResultCount = 4;

    private const uint ClassifierOffset = 0;
    private const uint PathOffset = 284; // Align(AnchorPath.Size).
    private const uint FibOffset = PathOffset + 2048;
    private const uint WarningOffset = FibOffset + FileInfoBlock.SizeInBytes;
    private const uint WorkspaceBytes = WarningOffset + 8;
    private const uint AllFlag = 1;
    private const uint QuietFlag = 1u << 8;
    private const uint ForceFlag = 1u << 5;
    // Private bit retained only by this frontend; Copy does not set it.
    private const uint FollowLinksFlag = 1u << 26;
    private const uint ProcessedFlag = 1u << 22;

    /// <summary>
    /// Executes Delete through public DOS matcher, lock, protection and
    /// DeleteFile calls.  The caller owns DOS library startup and final exit.
    /// </summary>
    public static int Run() => RunProfile(true);

    /// <summary>
    /// Runs the classic Workbench 3.1 four-slot Delete boundary. The object
    /// worker is shared, but FOLLOWLINKS is intentionally absent until the
    /// Workbench binary establishes that option.
    /// </summary>
    public static int RunWorkbench31() => RunProfile(false);

    private static int RunProfile(bool morphos)
    {
        if (DOS.DOSLibraryBase.IsNull)
        {
            DOS.SetIoErr(DOS.Error.ObjectWrongType);
            return DOS.RETURN_FAIL;
        }

        var workspace = Exec.AllocMem(WorkspaceBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (workspace.IsNull)
        {
            DOS.PrintFault(DOS.Error.NoFreeStore, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var arguments = default(NativeCommandArguments);
        var result = DOS.RETURN_FAIL;
        var ioError = (int)DOS.Error.BadTemplate;
        var parsed = false;
        if (morphos)
            parsed = NativeCommandArguments.TryRead(Template, ResultCount,
                out arguments);
        else
            parsed = NativeCommandArguments.TryRead(WorkbenchTemplate,
                WorkbenchResultCount, out arguments);
        if (parsed)
        {
            if (!arguments.TryGetResult(0, out var files) || files == 0 ||
                !arguments.TryGetResult(1, out var all) ||
                !arguments.TryGetResult(2, out var quiet) ||
                !arguments.TryGetResult(3, out var force) ||
                APTR.ReadUInt32(APTR.FromPointer(files), 0) == 0)
            {
                ioError = (int)DOS.Error.BadTemplate;
            }
            else
            {
                var state = new NativeMorphOSCopyTraversalState
                {
                    Path = APTR.FromPointer(workspace.Raw + PathOffset),
                    Fib = APTR.FromPointer(workspace.Raw + FibOffset),
                    WarningArguments = APTR.FromPointer(workspace.Raw + WarningOffset),
                    Destination = BPTR.Null,
                    CurrentDestination = BPTR.Null,
                    Mode = NativeMorphOSCopyModeSelection.Delete,
                    Depth = 1,
                    Result = DOS.RETURN_OK,
                    SecondaryResult = DOS.RETURN_FAIL,
                    Flags = (all != 0 ? AllFlag : 0) |
                        (quiet != 0 ? QuietFlag : 0) |
                        (force != 0 ? ForceFlag : 0),
                };
                if (morphos && arguments.TryGetResult(4, out var followLinks) &&
                    followLinks != 0)
                    state.Flags |= FollowLinksFlag;
                var options = new NativeMorphOSCopyOptions
                {
                    Sources = APTR.FromPointer(files),
                    Pattern = APTR.Null,
                    Target = APTR.Null,
                    Direct = false,
                    NoRequesters = false,
                    InputOmitted = false,
                };

                RunDeleteSources(ref options,
                    APTR.FromPointer(workspace.Raw + ClassifierOffset), ref state);

                // Delete returns WARN when no object was removed. The deletion
                // worker marks each successful DeleteFile with ProcessedFlag.
                if ((state.Flags & ProcessedFlag) == 0 &&
                    state.Result == DOS.RETURN_OK &&
                    state.SecondaryResult == DOS.RETURN_OK)
                    state.Result = DOS.RETURN_WARN;

                result = NativeMorphOSCopyResultPolicy.Complete(ref state);
                ioError = (int)DOS.IoErr();
            }
        }
        else
        {
            result = arguments.ReturnLevel;
            ioError = arguments.IoError;
        }
        arguments.Release();
        Exec.FreeMem(workspace, WorkspaceBytes);
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static void RunDeleteSources(ref NativeMorphOSCopyOptions options,
        APTR classifierAnchor, ref NativeMorphOSCopyTraversalState state)
    {
        // Keep Copy's primary-only outer guard and deferred traversal ordering,
        // but select a deletion-only worker before native reachability analysis.
        var worker = new NativeMorphOSDeleteWork { Reserved = 0 };
        state.SecondaryResult = DOS.RETURN_OK;
        var limit = (state.Flags & 1024) != 0 ? DOS.RETURN_OK : DOS.RETURN_WARN;
        var offset = 0;
        uint source;
        while (state.Result <= limit &&
            (source = APTR.ReadUInt32(options.Sources, offset)) != 0)
        {
            NativeMorphOSCopyTraversal.Run(APTR.FromPointer(source),
                classifierAnchor, ref state, ref worker);
            offset += 4;
        }
    }
}

/// <summary>
/// Copy DoWork's Delete path with the command's fixed operation. The frontend
/// never supplies a destination, metadata flags, or direct-device Copy flags.
/// Matcher traversal, directory descent/exit and result completion stay shared.
/// </summary>
internal struct NativeMorphOSDeleteWork : INativeMorphOSCopyWork
{
    // The native generic ABI requires a four-byte worker representation.
    public uint Reserved;

    public void Execute(APTR name, ref NativeMorphOSCopyTraversalState state)
    {
        if (state.Result > ((state.Flags & 1024) != 0 ? DOS.RETURN_OK : DOS.RETURN_WARN) ||
            state.SecondaryResult != 0) return;
        var quiet = (state.Flags & 256) != 0;
        var verbose = (state.Flags & 512) != 0;
        var entryType = FileInfoBlock.GetDirEntryType(state.Fib.Raw);
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
            if (!quiet)
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
            NativeMorphOSCopyOutput.PrintName(name, unchecked((uint)state.Depth),
                entryType > 0, entryType < 0 || (state.Flags & 1) == 0 ||
                (state.Flags & (1u << 23)) != 0, state.WarningArguments);

        CString error = CString.FromPointer(0);
        CString successText = "";
        if ((state.Flags & (1u << 23)) != 0 || (state.Flags & 1) == 0 || entryType < 0)
        {
            DOS.UnLock(source);
            source = BPTR.Null;
            if ((state.Flags & 32) != 0)
                DOS.SetProtection(CString.FromPointer(state.Path.Raw), 0);
            if (DOS.DeleteFile(CString.FromPointer(state.Path.Raw)) != 0)
                successText = "deleted.";
            else
            {
                state.Result = DOS.RETURN_WARN;
                error = "deleted.";
            }
        }
        // An ALL directory's first pass performs no deletion. Retain DoWork's
        // empty-string completion and QUIET failure behavior verbatim.
        if (CString.ToUInt32(error) != 0 && !quiet)
            NativeMorphOSCopyOutput.PrintNotDone(error, state.WarningArguments);
        else if (CString.ToUInt32(successText) != 0)
        {
            state.Flags |= 1u << 22;
            if (!quiet && verbose) PrintLine(successText, state.WarningArguments);
        }
        if (source.IsNotNull) DOS.UnLock(source);
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
