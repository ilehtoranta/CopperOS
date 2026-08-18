using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class RunCommandTests
{
    [Fact]
    public void Run_reads_options_and_copies_the_full_command_before_starting()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "DETACH QUIET STACK 8192 PRI -3 Echo hello world";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = RunCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 256);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("Echo hello world", platform.Store.RunCommand);
        Assert.Equal((uint)1, platform.Store.RunDetach);
        Assert.Equal((uint)1, platform.Store.RunQuiet);
        Assert.Equal((uint)8192, platform.Store.RunStack);
        Assert.Equal((uint)1, platform.Store.RunStackPresent);
        Assert.Equal(-3, platform.Store.RunPriority);
        Assert.Equal((uint)1, platform.Store.RunPriorityPresent);
        Assert.Equal(1, platform.Store.RunCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("STACK 4096")]
    [InlineData("PRI 2")]
    [InlineData("DETACH")]
    public void Run_requires_a_command_after_options(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = RunCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 256);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.RunCount);
    }

    [Fact]
    public void Run_maps_process_owner_failure_to_fail()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.RunFailure = true;
        string commandLine = "Echo hello";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = RunCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 256);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
        Assert.Equal(0, platform.Store.RunCount);
    }

    [Fact]
    public void Run_passes_parent_streams_and_directory_to_the_process_owner()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR continuation = new(2700);
        ShellProcessContinuation initial = new()
        {
            State = ShellProcessContinuationState.Pending,
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, continuation, in initial));
        const string commandLine = "Echo hello";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = new(
            source,
            (uint)commandLine.Length,
            APTR.Null,
            APTR.Null,
            new BPTR(2),
            new BPTR(3),
            new BPTR(4),
            new BPTR(5),
            new APTR(8),
            0,
            0,
            continuation);

        int result = RunCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 256);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)2, platform.Store.RunInput.Raw);
        Assert.Equal((uint)3, platform.Store.RunOutput.Raw);
        Assert.Equal((uint)4, platform.Store.RunError.Raw);
        Assert.Equal((uint)5, platform.Store.RunCurrentDirectory.Raw);
        Assert.Equal((uint)2700, platform.Store.RunContinuation.Raw);
        Assert.True(ShellProcessContinuationCodec.TryRead(
            ref platform, continuation, out var state));
        Assert.Equal(ShellProcessContinuationState.Running, state.State);
    }

    [Fact]
    public void Run_marks_a_pending_continuation_failed_when_launch_is_rejected()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.RunFailure = true;
        APTR continuation = new(2700);
        ShellProcessContinuation initial = new()
        {
            State = ShellProcessContinuationState.Pending,
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, continuation, in initial));
        const string commandLine = "Echo hello";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = new(
            source,
            (uint)commandLine.Length,
            APTR.Null,
            APTR.Null,
            BPTR.Null,
            BPTR.Null,
            BPTR.Null,
            BPTR.Null,
            new APTR(8),
            0,
            0,
            continuation);

        Assert.Equal((int)ShellCommandResult.Fail,
            RunCommand.Execute(ref platform, in invocation,
                new APTR(80), 128, new APTR(480), 256));
        Assert.True(ShellProcessContinuationCodec.TryRead(
            ref platform, continuation, out var state));
        Assert.Equal(ShellProcessContinuationState.Failed, state.State);
        Assert.Equal((int)ShellCommandResult.Fail, state.Result);
    }

    private static CommandInvocation CreateInvocation(APTR source, int length) =>
        new(source, (uint)length, APTR.Null, APTR.Null, BPTR.Null,
            BPTR.Null, BPTR.Null, BPTR.Null, new APTR(8), 0, 0);
}
