using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 Delete command frontend.  The matcher and object worker are
/// shared with the already source-ordered Copy DELETE path, while this entry
/// owns the Delete-specific ReadArgs lease and invocation workspace.
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

                NativeMorphOSCopyPatternSetup.RunDeleteSources(ref options,
                    APTR.FromPointer(workspace.Raw + ClassifierOffset), ref state);

                // Delete returns WARN when no object was removed.  The shared
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
}
