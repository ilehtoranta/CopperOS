using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Invocation-owned state shared by PatCopy and its object worker.</summary>
public struct NativeMorphOSCopyTraversalState
{
    public APTR Path, Fib, Pattern, WarningArguments, DestinationName;
    // Required sixteen-byte scratch when ExtendedExamine is enabled.
    public APTR ExamineTags;
    public BPTR Destination, CurrentDestination;
    public int Mode, Depth, DestinationPathSize, Result, SecondaryResult;
    public uint Flags, MetadataFlags;
    public APTR CopyBuffer;
    public uint BufferSize, CopyBufferBytes;
    public bool ExtendedExamine;
}

public interface INativeMorphOSCopyWork
{
    // The worker implements DoWork, including the primary result threshold
    // (OK for ERRWARN, otherwise WARN) and SecondaryResult guards.
    void Execute(APTR name, ref NativeMorphOSCopyTraversalState state);
}

/// <summary>Filesystem PatCopy matcher lifecycle with streaming deferred work.</summary>
public static class NativeMorphOSCopyTraversal
{
    /// <summary>
    /// PatCopy entry routing. name is writable and terminated; classifierAnchor
    /// is invocation-owned AnchorPath storage, distinct from Path and Fib.
    /// The command validates source length against its 2048-byte Path buffer.
    /// </summary>
    public static void Run(APTR name, APTR classifierAnchor,
        ref NativeMorphOSCopyTraversalState state)
    {
        var worker = new NativeMorphOSCopyWork { Reserved = 0 };
        Run(name, classifierAnchor, ref state, ref worker);
    }

    public static void Run<TWork>(APTR name, APTR classifierAnchor,
        ref NativeMorphOSCopyTraversalState state, ref TWork worker)
        where TWork : struct, INativeMorphOSCopyWork
    {
        // A failed classification (-1) does not select old directory syntax.
        // Classification precedes device probing even for stream sources.
        var first = false;
        if (state.Mode == 0 || (state.Flags & 1) != 0)
            first = NativeMorphOSCopyPatternClassifier.IsMatchPattern(
                CString.FromPointer(name.Raw), classifierAnchor) == 0;

        state.CurrentDestination = state.Destination;
        state.DestinationPathSize = 0;
        if (state.Mode == 0 && !IsFileSystem(name))
        {
            state.Flags |= 1u << 24;
            var offset = 0;
            byte value;
            do
            {
                value = APTR.ReadUInt8(name, offset);
                APTR.WriteUInt8(state.Path, offset, value);
                offset++;
            } while (value != 0);
            worker.Execute(DOS.FilePart(CString.FromPointer(name.Raw)).Address, ref state);
            state.Flags &= ~(1u << 24);
            return;
        }
        RunFileSystem(name, first, ref state, ref worker);
    }

    /// <summary>Test only the device prefix and restore the original name.</summary>
    public static bool IsFileSystem(APTR name)
    {
        var offset = 0;
        while (APTR.ReadUInt8(name, offset) != 0 &&
            APTR.ReadUInt8(name, offset) != (byte)':') offset++;
        if (APTR.ReadUInt8(name, offset) == 0) return true;
        offset++;
        var saved = APTR.ReadUInt8(name, offset);
        APTR.WriteUInt8(name, offset, 0);
        var result = DOS.IsFileSystem(CString.FromPointer(name.Raw)) != 0;
        APTR.WriteUInt8(name, offset, saved);
        return result;
    }

    /// <summary>
    /// Runs filesystem traversal with the actual DoWork implementation.
    /// The command retains the transfer cache across source patterns and
    /// frees it once at command exit.
    /// </summary>
    public static void RunFileSystem(APTR name, bool first,
        ref NativeMorphOSCopyTraversalState state)
    {
        var worker = new NativeMorphOSCopyWork { Reserved = 0 };
        RunFileSystem(name, first, ref state, ref worker);
    }

    /// <summary>
    /// The command must route a non-filesystem COPY source before calling this
    /// method. first is the original IsMatchPattern-derived old-syntax decision.
    /// Path/Fib and the eight-byte warning arguments belong to this invocation.
    /// </summary>
    public static void RunFileSystem<TWork>(APTR name, bool first,
        ref NativeMorphOSCopyTraversalState state, ref TWork worker)
        where TWork : struct, INativeMorphOSCopyWork
    {
        const uint anchorBytes = DosLayout.AnchorPath.Size + 2048;
        state.CurrentDestination = state.Destination;
        state.DestinationPathSize = 0;
        var limit = (state.Flags & 1024) != 0 ? DOS.RETURN_OK : DOS.RETURN_WARN;
        var anchor = Exec.AllocMem(anchorBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (anchor.IsNotNull)
        {
            APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, 1u << 12);
            APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength, 2048);
            var pending = false;
            var deep = false;
            var parentFailed = false;
            var result = DOS.MatchFirst(CString.FromPointer(name.Raw), anchor);
            while (result == 0 && state.Result <= limit && state.SecondaryResult == 0)
            {
                if (parentFailed)
                {
                    result = (int)DOS.IoErr();
                    if (result == 0)
                    {
                        result = (int)DOS.Error.InvalidLock;
                        DOS.SetIoErr((DOS.Error)result);
                    }
                    break;
                }
                if (pending)
                {
                    worker.Execute(APTR.FromPointer(state.Fib.Raw +
                        FileInfoBlock.FileNameOffset), ref state);
                    pending = false;
                }
                parentFailed = NativeMorphOSCopyMatchStep.Process(anchor,
                    state.Path, state.Fib, state.Pattern, state.WarningArguments,
                    state.Mode, (state.Flags & 1) != 0, (state.Flags & 256) != 0,
                    state.Destination, state.MetadataFlags, ref first,
                    ref state.Depth, ref state.CurrentDestination, ref state.Flags,
                    ref state.DestinationPathSize, ref deep, out pending);
                result = DOS.MatchNext(anchor);
            }
            DOS.MatchEnd(anchor);
            if (result != 0 && result != (int)DOS.Error.NoMoreEntries)
            {
                var error = DOS.IoErr();
                state.Flags |= 512;
                APTR.WriteUInt32(state.WarningArguments, 0, name.Raw);
                DOS.VPrintf("%s - ", state.WarningArguments);
                DOS.PrintFault(error, CString.FromPointer(0));
                DOS.SetIoErr(error);
                state.SecondaryResult = DOS.RETURN_FAIL;
            }
            if (pending)
                worker.Execute(APTR.FromPointer(state.Fib.Raw +
                    FileInfoBlock.FileNameOffset), ref state);
            // Exec cleanup is not allowed to replace the matcher/handler
            // result that the caller will publish after this traversal.
            var traversalError = DOS.IoErr();
            Exec.FreeMem(anchor, anchorBytes);
            DOS.SetIoErr(traversalError);
        }
        else
        {
            state.Result = DOS.RETURN_FAIL;
            // Preserve the original (!Flags & QUIET) expression's behavior:
            // QUIET is not bit zero, so this branch emits no allocation fault.
        }
        if (state.CurrentDestination.IsNotNull &&
            state.CurrentDestination.Raw != state.Destination.Raw)
            DOS.UnLock(state.CurrentDestination);
    }
}
