using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellProcessContinuationTests
{
    [Fact]
    public void Codec_round_trips_fixed_width_process_state_and_result()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3000);
        ShellProcessContinuation initial = new()
        {
            ParentCli = new APTR(8),
            ChildCli = new APTR(12),
            Input = new BPTR(2),
            Output = new BPTR(3),
            Error = new BPTR(4),
            CurrentDirectory = new BPTR(5),
            Command = new APTR(100),
            CommandLength = 9,
            State = ShellProcessContinuationState.Pending,
            Result = 0,
            Flags = 7,
        };

        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, record, in initial));
        Assert.True(ShellProcessContinuationCodec.TrySetState(
            ref platform, record, ShellProcessContinuationState.Running));
        Assert.True(ShellProcessContinuationCodec.TryRecordResult(
            ref platform, record, 5,
            ShellProcessContinuationState.Completed));
        Assert.True(ShellProcessContinuationCodec.TryRead(
            ref platform, record, out var actual));
        Assert.Equal(new APTR(8), actual.ParentCli);
        Assert.Equal(new APTR(12), actual.ChildCli);
        Assert.Equal(new BPTR(2), actual.Input);
        Assert.Equal(new BPTR(3), actual.Output);
        Assert.Equal(new BPTR(4), actual.Error);
        Assert.Equal(new BPTR(5), actual.CurrentDirectory);
        Assert.Equal(new APTR(100), actual.Command);
        Assert.Equal((uint)9, actual.CommandLength);
        Assert.Equal(ShellProcessContinuationState.Completed,
            actual.State);
        Assert.Equal(5, actual.Result);
        Assert.Equal((uint)7, actual.Flags);
    }

    [Fact]
    public void Codec_rejects_invalid_states_and_bad_headers()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3000);
        ShellProcessContinuation invalid = new()
        {
            State = (ShellProcessContinuationState)99,
        };

        Assert.False(ShellProcessContinuationCodec.Initialize(
            ref platform, record, in invalid));
        invalid.State = ShellProcessContinuationState.Pending;
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, record, in invalid));
        Assert.False(ShellProcessContinuationCodec.TrySetState(
            ref platform, record, (ShellProcessContinuationState)99));
        platform.WriteUInt32(record, 0, 0);
        Assert.False(ShellProcessContinuationCodec.TryRead(
            ref platform, record, out _));
    }

    [Fact]
    public void Transitions_reject_impossible_edges_and_record_terminal_results()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3000);
        ShellProcessContinuation initial = new()
        {
            State = ShellProcessContinuationState.Pending,
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, record, in initial));

        Assert.True(ShellProcessContinuationTransitions.TryStart(
            ref platform, record));
        Assert.False(ShellProcessContinuationTransitions.TryStart(
            ref platform, record));
        Assert.True(ShellProcessContinuationTransitions.TryComplete(
            ref platform, record, 17));
        Assert.False(ShellProcessContinuationTransitions.TryAbort(
            ref platform, record, -1));
        Assert.True(ShellProcessContinuationCodec.TryRead(
            ref platform, record, out var completed));
        Assert.Equal(ShellProcessContinuationState.Completed,
            completed.State);
        Assert.Equal(17, completed.Result);
    }

    [Fact]
    public void Transitions_can_abort_or_fail_before_completion()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3000);
        ShellProcessContinuation initial = new()
        {
            State = ShellProcessContinuationState.Pending,
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, record, in initial));
        Assert.True(ShellProcessContinuationTransitions.TryAbort(
            ref platform, record, -2));
        Assert.False(ShellProcessContinuationTransitions.TryFail(
            ref platform, record, -3));

        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, record, in initial));
        Assert.True(ShellProcessContinuationTransitions.TryFail(
            ref platform, record, -3));
        Assert.True(ShellProcessContinuationCodec.TryRead(
            ref platform, record, out var failed));
        Assert.Equal(ShellProcessContinuationState.Failed, failed.State);
        Assert.Equal(-3, failed.Result);
    }

    [Fact]
    public void Polling_reconciles_a_DOS_completion_without_waiting()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3000);
        ShellProcessContinuation initial = new()
        {
            ParentCli = new APTR(8),
            State = ShellProcessContinuationState.Pending,
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, record, in initial));
        Assert.True(ShellProcessContinuationTransitions.TryStart(
            ref platform, record));
        platform.Store.ContinuationObservedState =
            ShellProcessContinuationState.Completed;
        platform.Store.ContinuationResult = 22;

        Assert.True(ShellProcessContinuationPolling.TryPoll(
            ref platform, new APTR(8), record, out var state,
            out var result));
        Assert.Equal(ShellProcessContinuationState.Completed, state);
        Assert.Equal(22, result);
        Assert.Equal(1, platform.Store.ContinuationPollCount);
    }

    [Fact]
    public void Polling_rejects_an_owner_state_that_disagrees_with_guest_state()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3000);
        ShellProcessContinuation initial = new()
        {
            State = ShellProcessContinuationState.Pending,
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, record, in initial));
        platform.Store.ContinuationObservedState =
            ShellProcessContinuationState.Running;

        Assert.False(ShellProcessContinuationPolling.TryPoll(
            ref platform, new APTR(8), record, out _, out _));
    }

    [Fact]
    public void Teardown_releases_only_marked_resources_after_terminal_state()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3000);
        ShellProcessContinuation initial = new()
        {
            State = ShellProcessContinuationState.Completed,
            Flags = (uint)(ShellProcessContinuationFlags.InputOwned |
                ShellProcessContinuationFlags.OutputOwned |
                ShellProcessContinuationFlags.RecordOwned),
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, record, in initial));

        Assert.True(ShellProcessContinuationTeardown.TryRelease(
            ref platform, new APTR(8), record));
        Assert.Equal((uint)(ShellProcessContinuationFlags.InputOwned |
            ShellProcessContinuationFlags.OutputOwned |
            ShellProcessContinuationFlags.RecordOwned),
            platform.Store.LastReleasedFlags);
        Assert.Equal(1, platform.Store.ContinuationReleaseCount);
        Assert.True(ShellProcessContinuationCodec.TryRead(
            ref platform, record, out var released));
        Assert.True((released.Flags &
            (uint)ShellProcessContinuationFlags.ResourcesClosed) != 0);
        Assert.False(ShellProcessContinuationTeardown.TryRelease(
            ref platform, new APTR(8), record));
    }

    [Fact]
    public void Teardown_keeps_ownership_when_the_DOS_release_fails()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ContinuationReleaseFailure = true;
        APTR record = new(3000);
        ShellProcessContinuation initial = new()
        {
            State = ShellProcessContinuationState.Failed,
            Flags = (uint)ShellProcessContinuationFlags.ErrorOwned,
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, record, in initial));
        Assert.False(ShellProcessContinuationTeardown.TryRelease(
            ref platform, new APTR(8), record));
        Assert.True(ShellProcessContinuationCodec.TryRead(
            ref platform, record, out var pending));
        Assert.False((pending.Flags &
            (uint)ShellProcessContinuationFlags.ResourcesClosed) != 0);
    }
}
