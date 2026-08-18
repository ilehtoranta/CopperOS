using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class NewShellCommandTests
{
    [Fact]
    public void NewShell_copies_window_and_startup_script_before_launch()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "\"CON://640/480/My Shell/CLOSE\" S:Shell-Startup";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = NewShellCommand.Execute(ref platform, in invocation,
            new APTR(80), 128,
            new APTR(480), 128,
            new APTR(640), 128);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(ShellLaunchKind.NewShell, platform.Store.ShellLaunchKind);
        Assert.Equal("CON://640/480/My Shell/CLOSE", platform.Store.ShellWindow);
        Assert.Equal("S:Shell-Startup", platform.Store.ShellFrom);
        Assert.Equal(1, platform.Store.ShellLaunchCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void NewCLI_without_arguments_uses_the_owner_defaults()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        int result = NewCliCommand.Execute(ref platform, in invocation,
            new APTR(80), 128,
            new APTR(480), 128,
            new APTR(640), 128);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(ShellLaunchKind.NewCli, platform.Store.ShellLaunchKind);
        Assert.Equal(string.Empty, platform.Store.ShellWindow);
        Assert.Equal(string.Empty, platform.Store.ShellFrom);
        Assert.Equal(1, platform.Store.ShellLaunchCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void NewShell_passes_parent_streams_and_directory_as_fixed_width_inheritance()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR continuation = new(2700);
        ShellProcessContinuation initial = new()
        {
            State = ShellProcessContinuationState.Pending,
        };
        Assert.True(ShellProcessContinuationCodec.Initialize(
            ref platform, continuation, in initial));
        CommandInvocation invocation = new(
            APTR.Null,
            0,
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

        int result = NewShellCommand.Execute(ref platform, in invocation,
            new APTR(80), 128,
            new APTR(480), 128,
            new APTR(640), 128);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)2, platform.Store.ShellInput.Raw);
        Assert.Equal((uint)3, platform.Store.ShellOutput.Raw);
        Assert.Equal((uint)4, platform.Store.ShellError.Raw);
        Assert.Equal((uint)5, platform.Store.ShellCurrentDirectory.Raw);
        Assert.Equal((uint)2700, platform.Store.ShellContinuation.Raw);
        Assert.True(ShellProcessContinuationCodec.TryRead(
            ref platform, continuation, out var state));
        Assert.Equal(ShellProcessContinuationState.Running, state.State);
    }

    [Fact]
    public void NewShell_rejects_more_than_two_arguments()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "WINDOW FROM EXTRA";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = NewShellCommand.Execute(ref platform, in invocation,
            new APTR(80), 128,
            new APTR(480), 128,
            new APTR(640), 128);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ShellLaunchCount);
    }

    [Fact]
    public void NewCLI_maps_child_launch_failure_to_fail()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ShellLaunchFailure = true;
        string commandLine = "CON: S:Startup";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = NewCliCommand.Execute(ref platform, in invocation,
            new APTR(80), 128,
            new APTR(480), 128,
            new APTR(640), 128);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
        Assert.Equal(0, platform.Store.ShellLaunchCount);
    }

    private static CommandInvocation CreateInvocation(APTR source, int length) =>
        new(source, (uint)length, APTR.Null, APTR.Null, BPTR.Null,
            BPTR.Null, BPTR.Null, BPTR.Null, new APTR(8), 0, 0);
}
