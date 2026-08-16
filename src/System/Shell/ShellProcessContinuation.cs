using Amiga;
using System;

namespace CopperOS.Shell;

/// <summary>Lifecycle states for one DOS-owned child-process continuation.</summary>
public enum ShellProcessContinuationState : uint
{
    Pending = 1,
    Running = 2,
    Completed = 3,
    Aborted = 4,
    Failed = 5,
}

/// <summary>Resources whose lifetime belongs to one child continuation.</summary>
[Flags]
public enum ShellProcessContinuationFlags : uint
{
    None = 0,
    InputOwned = 1,
    OutputOwned = 2,
    ErrorOwned = 4,
    DirectoryOwned = 8,
    RecordOwned = 16,
    ResourcesClosed = 0x8000_0000,
}

/// <summary>
/// Fixed-width guest record for asynchronous Shell/DOS process ownership.
/// The record contains no task, delegate, object, or host handle; DOS owns
/// allocation, scheduling, child identity, and eventual reclamation.
/// </summary>
public struct ShellProcessContinuation
{
    public APTR ParentCli;
    public APTR ChildCli;
    public BPTR Input;
    public BPTR Output;
    public BPTR Error;
    public BPTR CurrentDirectory;
    public APTR Command;
    public uint CommandLength;
    public ShellProcessContinuationState State;
    public int Result;
    public uint Flags;
}

/// <summary>Codec for one guest-resident process continuation.</summary>
public static class ShellProcessContinuationCodec
{
    public const uint Magic = 0x5343_5052;
    public const uint Version = 1;
    public const uint Size = 52;

    public static bool Initialize<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellProcessContinuation initial)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidState(initial.State) || !ValidFlags(
                (ShellProcessContinuationFlags)initial.Flags) ||
            record.IsNull ||
            !platform.IsMapped(record, Size))
            return false;
        platform.Clear(record, Size);
        platform.WriteUInt32(record, 0, Magic);
        platform.WriteUInt32(record, 4, Version);
        WriteState(ref platform, record, in initial);
        return true;
    }

    public static bool TryRead<TPlatform>(
        ref TPlatform platform,
        APTR record,
        out ShellProcessContinuation state)
        where TPlatform : struct, IShellPlatform
    {
        state = default;
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            platform.ReadUInt32(record, 0) != Magic ||
            platform.ReadUInt32(record, 4) != Version)
            return false;
        state.ParentCli = APTR.FromPointer(platform.ReadUInt32(record, 8));
        state.ChildCli = APTR.FromPointer(platform.ReadUInt32(record, 12));
        state.Input = BPTR.FromRaw(platform.ReadUInt32(record, 16));
        state.Output = BPTR.FromRaw(platform.ReadUInt32(record, 20));
        state.Error = BPTR.FromRaw(platform.ReadUInt32(record, 24));
        state.CurrentDirectory = BPTR.FromRaw(
            platform.ReadUInt32(record, 28));
        state.Command = APTR.FromPointer(platform.ReadUInt32(record, 32));
        state.CommandLength = platform.ReadUInt32(record, 36);
        state.State = (ShellProcessContinuationState)
            platform.ReadUInt32(record, 40);
        state.Result = unchecked((int)platform.ReadUInt32(record, 44));
        state.Flags = platform.ReadUInt32(record, 48);
        return ValidState(state.State) && ValidFlags(
            (ShellProcessContinuationFlags)state.Flags);
    }

    public static bool TrySetState<TPlatform>(
        ref TPlatform platform,
        APTR record,
        ShellProcessContinuationState state)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidState(state) ||
            !TryRead(ref platform, record, out var current))
            return false;
        current.State = state;
        return WriteState(ref platform, record, in current);
    }

    public static bool TryRecordResult<TPlatform>(
        ref TPlatform platform,
        APTR record,
        int result,
        ShellProcessContinuationState state)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidState(state) ||
            !TryRead(ref platform, record, out var current))
            return false;
        current.Result = result;
        current.State = state;
        return WriteState(ref platform, record, in current);
    }

    public static bool TrySetFlags<TPlatform>(
        ref TPlatform platform,
        APTR record,
        ShellProcessContinuationFlags flags)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidFlags(flags) ||
            !TryRead(ref platform, record, out var current))
            return false;
        current.Flags = (uint)flags;
        return WriteState(ref platform, record, in current);
    }

    private static bool ValidState(ShellProcessContinuationState state) =>
        state is ShellProcessContinuationState.Pending or
            ShellProcessContinuationState.Running or
            ShellProcessContinuationState.Completed or
            ShellProcessContinuationState.Aborted or
            ShellProcessContinuationState.Failed;

    private static bool ValidFlags(ShellProcessContinuationFlags flags) =>
        ((uint)flags & ~((uint)ShellProcessContinuationFlags.InputOwned |
            (uint)ShellProcessContinuationFlags.OutputOwned |
            (uint)ShellProcessContinuationFlags.ErrorOwned |
            (uint)ShellProcessContinuationFlags.DirectoryOwned |
            (uint)ShellProcessContinuationFlags.RecordOwned |
            (uint)ShellProcessContinuationFlags.ResourcesClosed)) == 0;

    private static bool WriteState<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellProcessContinuation state)
        where TPlatform : struct, IShellPlatform
    {
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            !ValidState(state.State) ||
            !ValidFlags((ShellProcessContinuationFlags)state.Flags))
            return false;
        platform.WriteUInt32(record, 8, state.ParentCli.Raw);
        platform.WriteUInt32(record, 12, state.ChildCli.Raw);
        platform.WriteUInt32(record, 16, state.Input.Raw);
        platform.WriteUInt32(record, 20, state.Output.Raw);
        platform.WriteUInt32(record, 24, state.Error.Raw);
        platform.WriteUInt32(record, 28, state.CurrentDirectory.Raw);
        platform.WriteUInt32(record, 32, state.Command.Raw);
        platform.WriteUInt32(record, 36, state.CommandLength);
        platform.WriteUInt32(record, 40, (uint)state.State);
        platform.WriteUInt32(record, 44, unchecked((uint)state.Result));
        platform.WriteUInt32(record, 48, state.Flags);
        return true;
    }
}

/// <summary>
/// Validates the lifecycle edges used by the DOS scheduler owner. No method
/// allocates, waits, or invokes a callback; each transition is one bounded
/// guest-record update.
/// </summary>
public static class ShellProcessContinuationTransitions
{
    public static bool TryStart<TPlatform>(
        ref TPlatform platform,
        APTR record)
        where TPlatform : struct, IShellPlatform =>
        TryMove(ref platform, record,
            ShellProcessContinuationState.Pending,
            ShellProcessContinuationState.Running,
            0,
            false);

    public static bool TryComplete<TPlatform>(
        ref TPlatform platform,
        APTR record,
        int result)
        where TPlatform : struct, IShellPlatform =>
        TryMove(ref platform, record,
            ShellProcessContinuationState.Running,
            ShellProcessContinuationState.Completed,
            result,
            true);

    public static bool TryAbort<TPlatform>(
        ref TPlatform platform,
        APTR record,
        int result)
        where TPlatform : struct, IShellPlatform =>
        TryMove(ref platform, record,
            ShellProcessContinuationState.Pending,
            ShellProcessContinuationState.Aborted,
            result,
            true) ||
        TryMove(ref platform, record,
            ShellProcessContinuationState.Running,
            ShellProcessContinuationState.Aborted,
            result,
            true);

    public static bool TryFail<TPlatform>(
        ref TPlatform platform,
        APTR record,
        int result)
        where TPlatform : struct, IShellPlatform =>
        TryMove(ref platform, record,
            ShellProcessContinuationState.Pending,
            ShellProcessContinuationState.Failed,
            result,
            true) ||
        TryMove(ref platform, record,
            ShellProcessContinuationState.Running,
            ShellProcessContinuationState.Failed,
            result,
            true);

    private static bool TryMove<TPlatform>(
        ref TPlatform platform,
        APTR record,
        ShellProcessContinuationState expected,
        ShellProcessContinuationState next,
        int result,
        bool writeResult)
        where TPlatform : struct, IShellPlatform
    {
        if (!ShellProcessContinuationCodec.TryRead(
                ref platform, record, out var current) ||
            current.State != expected)
            return false;
        return writeResult
            ? ShellProcessContinuationCodec.TryRecordResult(
                ref platform, record, result, next)
            : ShellProcessContinuationCodec.TrySetState(
                ref platform, record, next);
    }
}

/// <summary>Non-blocking reconciliation of one DOS completion observation.</summary>
public static class ShellProcessContinuationPolling
{
    public static bool TryPoll<TPlatform>(
        ref TPlatform platform,
        APTR cli,
        APTR continuation,
        out ShellProcessContinuationState state,
        out int result)
        where TPlatform : struct, IShellPlatform
    {
        state = default;
        result = 0;
        if (cli.IsNull || continuation.IsNull ||
            !platform.TryPollShellContinuation(
                cli, continuation, out var observed, out var observedResult) ||
            !ValidState(observed) ||
            !ShellProcessContinuationCodec.TryRead(
                ref platform, continuation, out var current))
            return false;

        if (observed is ShellProcessContinuationState.Completed)
        {
            if (!ShellProcessContinuationTransitions.TryComplete(
                    ref platform, continuation, observedResult))
                return false;
        }
        else if (observed is ShellProcessContinuationState.Aborted)
        {
            if (!ShellProcessContinuationTransitions.TryAbort(
                    ref platform, continuation, observedResult))
                return false;
        }
        else if (observed is ShellProcessContinuationState.Failed)
        {
            if (!ShellProcessContinuationTransitions.TryFail(
                    ref platform, continuation, observedResult))
                return false;
        }
        else if (observed != ShellProcessContinuationState.Pending &&
                 observed != ShellProcessContinuationState.Running)
            return false;
        else if (current.State != observed)
            return false;

        if (!ShellProcessContinuationCodec.TryRead(
                ref platform, continuation, out var after))
            return false;
        state = after.State;
        result = after.Result;
        return true;
    }

    private static bool ValidState(ShellProcessContinuationState state) =>
        state is ShellProcessContinuationState.Pending or
            ShellProcessContinuationState.Running or
            ShellProcessContinuationState.Completed or
            ShellProcessContinuationState.Aborted or
            ShellProcessContinuationState.Failed;
}

/// <summary>
/// Requests DOS-owned release of resources marked on a terminal continuation.
/// A failed release leaves the ownership flags intact so the owner can retry.
/// </summary>
public static class ShellProcessContinuationTeardown
{
    public static bool TryRelease<TPlatform>(
        ref TPlatform platform,
        APTR cli,
        APTR continuation)
        where TPlatform : struct, IShellPlatform
    {
        if (cli.IsNull || continuation.IsNull ||
            !ShellProcessContinuationCodec.TryRead(
                ref platform, continuation, out var current) ||
            current.State is ShellProcessContinuationState.Pending or
                ShellProcessContinuationState.Running ||
            (current.Flags &
                (uint)ShellProcessContinuationFlags.ResourcesClosed) != 0)
            return false;

        var owned = current.Flags & ~(uint)
            ShellProcessContinuationFlags.ResourcesClosed;
        var closedFlags = (ShellProcessContinuationFlags)(current.Flags |
            (uint)ShellProcessContinuationFlags.ResourcesClosed);
        if (!ShellProcessContinuationCodec.TrySetFlags(
                ref platform, continuation, closedFlags))
            return false;
        if (!platform.TryReleaseShellContinuation(
                cli, continuation, owned))
        {
            ShellProcessContinuationCodec.TrySetFlags(
                ref platform,
                continuation,
                (ShellProcessContinuationFlags)current.Flags);
            return false;
        }
        // Marking closed before the callback permits DOS to reclaim a record
        // that carries RecordOwned without a post-release guest write.
        return true;
    }
}
