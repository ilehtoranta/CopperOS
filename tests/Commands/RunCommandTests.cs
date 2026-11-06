using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class RunCommandTests
{
    [Theory]
    [InlineData(false, false, ShellRunOutputDestination.Nil)]
    [InlineData(false, true, ShellRunOutputDestination.ExplicitNil)]
    [InlineData(true, false, ShellRunOutputDestination.Console)]
    [InlineData(true, true, ShellRunOutputDestination.ExplicitNil)]
    public void WorkerOutputUsesTheConsoleOrNilUnlessNilWasExplicit(bool hasConsole, bool nilOutput, ShellRunOutputDestination expected)
        => Assert.Equal(expected, ShellRunOutputPolicy.Destination(hasConsole, nilOutput));

    [Theory]
    [InlineData(2u, false, false, true, 0u, true)]
    [InlineData(2u, true, false, false, 2u, false)]
    [InlineData(2u, false, true, true, 4u, false)]
    [InlineData(0u, false, false, true, 0u, false)]
    [InlineData(3u, false, false, true, 1u, true)]
    public void Run_output_policy_preserves_child_and_launcher_ownership(
        uint owned, bool nil, bool sharedError, bool useStandard,
        uint expectedOwned, bool closeLauncher)
    {
        var launcher = BPTR.FromRaw(11);
        var standard = BPTR.FromRaw(12);
        var error = BPTR.FromRaw(sharedError ? 11u : 13u);
        ShellRunOutputPolicy.Select(launcher, error, standard, owned, nil,
            out var child, out var transferred, out var close);
        Assert.Equal(useStandard ? standard : launcher, child);
        Assert.Equal(expectedOwned, transferred);
        Assert.Equal(closeLauncher, close);
    }

    [Fact]
    public void Run_passes_the_full_command_line_without_interpreting_option_words()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "DETACH QUIET STACK 8192 PRI -3 Echo hello world";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = RunCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 256);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(commandLine, platform.Store.RunCommand);
        Assert.Equal(0u, platform.Store.RunDetach);
        Assert.Equal(0u, platform.Store.RunQuiet);
        Assert.Equal(0u, platform.Store.RunStackPresent);
        Assert.Equal(0u, platform.Store.RunPriorityPresent);
        Assert.Equal(1, platform.Store.RunCount);
        Assert.Equal(0, platform.Store.ReadArgsCount);
    }

    [Fact]
    public void Run_preserves_quoted_and_escaped_child_text()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string line = "DETACH STACK 8192 Echo \"hello there\" *>errors";
        APTR source = platform.Store.PutAt(16, line);
        CommandInvocation invocation = CreateInvocation(source, line.Length);

        Assert.Equal((int)ShellCommandResult.Ok, RunCommand.Execute(ref platform,
            in invocation, new APTR(80), 128, new APTR(480), 256));
        Assert.Equal(line, platform.Store.RunCommand);
    }

    [Theory]
    [InlineData("COMMAND Echo \"FIRST\" >out")]
    [InlineData("COMMAND=Echo \"FIRST\" >out")]
    [InlineData("DETACH Echo child >out")]
    [InlineData("QUIET Echo child >out")]
    [InlineData("STACK 4096 Echo child >out")]
    [InlineData("PRI 2 Echo child >out")]
    public void Run_treats_command_as_part_of_the_child_line(string line)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(16, line);
        CommandInvocation invocation = CreateInvocation(source, line.Length);

        Assert.Equal((int)ShellCommandResult.Ok, RunCommand.Execute(ref platform,
            in invocation, new APTR(80), 128, new APTR(480), 256));
        Assert.Equal(line, platform.Store.RunCommand);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("; comment")]
    public void Run_launches_an_empty_child_when_no_command_is_supplied(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = RunCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 256);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(1, platform.Store.RunCount);
        Assert.Equal(string.Empty, platform.Store.RunCommand);
    }

    [Fact]
    public void Run_accepts_the_null_argument_pointer_used_by_the_command_stepper()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);
        Assert.Equal((int)ShellCommandResult.Ok, RunCommand.Execute(ref platform,
            in invocation, new APTR(80), 128, new APTR(480), 256));
        Assert.Equal(1, platform.Store.RunCount);
        Assert.Equal(string.Empty, platform.Store.RunCommand);
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
        Assert.Equal(0, platform.Store.ReadArgsCount);
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
