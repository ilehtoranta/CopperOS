using Amiga;
using CopperOS.Commands;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class CliCommandTests
{
    [Fact]
    public void Empty_external_cli_launches_a_distinct_child_kind_with_inherited_context()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = new(APTR.Null, 0, APTR.Null, APTR.Null,
            new BPTR(2), new BPTR(3), new BPTR(4), new BPTR(5), new APTR(8), 0, 0);

        int result = CliCommand.Execute(ref platform, in invocation);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(ShellLaunchKind.Cli, platform.Store.ShellLaunchKind);
        Assert.Equal((uint)2, platform.Store.ShellInput.Raw);
        Assert.Equal((uint)3, platform.Store.ShellOutput.Raw);
        Assert.Equal((uint)4, platform.Store.ShellError.Raw);
        Assert.Equal((uint)5, platform.Store.ShellCurrentDirectory.Raw);
        Assert.Equal(1, platform.Store.ShellLaunchCount);
        Assert.Equal(0, platform.Store.ReadArgsCount);
    }

    [Fact]
    public void Candidate_refuses_nonempty_tail_until_the_original_parser_is_captured()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(16, "WINDOW");
        CommandInvocation invocation = new(source, 6, APTR.Null, APTR.Null,
            BPTR.Null, BPTR.Null, BPTR.Null, BPTR.Null, new APTR(8), 0, 0);

        int result = CliCommand.Execute(ref platform, in invocation);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ShellLaunchCount);
    }

    [Fact]
    public void Launch_failure_maps_to_fail_without_creating_a_second_owner()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ShellLaunchFailure = true;
        CommandInvocation invocation = new(APTR.Null, 0, APTR.Null, APTR.Null,
            BPTR.Null, BPTR.Null, BPTR.Null, BPTR.Null, new APTR(8), 0, 0);

        int result = CliCommand.Execute(ref platform, in invocation);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(0, platform.Store.ShellLaunchCount);
    }

    [Fact]
    public void Empty_external_cli_passes_and_starts_the_invocation_continuation()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR continuation = new(2700);
        ShellProcessContinuation initial = new()
        {
            State = ShellProcessContinuationState.Pending,
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, continuation, in initial));
        CommandInvocation invocation = new(APTR.Null, 0, APTR.Null, APTR.Null,
            BPTR.Null, BPTR.Null, BPTR.Null, BPTR.Null, new APTR(8), 0, 0,
            continuation);

        int result = CliCommand.Execute(ref platform, in invocation);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)2700, platform.Store.ShellContinuation.Raw);
        Assert.True(ShellProcessContinuationCodec.TryRead(
            ref platform, continuation, out var state));
        Assert.Equal(ShellProcessContinuationState.Running, state.State);
    }

    [Fact]
    public void Rejected_external_cli_launch_marks_a_pending_continuation_failed()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ShellLaunchFailure = true;
        APTR continuation = new(2700);
        ShellProcessContinuation initial = new()
        {
            State = ShellProcessContinuationState.Pending,
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, continuation, in initial));
        CommandInvocation invocation = new(APTR.Null, 0, APTR.Null, APTR.Null,
            BPTR.Null, BPTR.Null, BPTR.Null, BPTR.Null, new APTR(8), 0, 0,
            continuation);

        Assert.Equal((int)ShellCommandResult.Fail,
            CliCommand.Execute(ref platform, in invocation));
        Assert.True(ShellProcessContinuationCodec.TryRead(
            ref platform, continuation, out var state));
        Assert.Equal(ShellProcessContinuationState.Failed, state.State);
        Assert.Equal((int)ShellCommandResult.Fail, state.Result);
    }
}
