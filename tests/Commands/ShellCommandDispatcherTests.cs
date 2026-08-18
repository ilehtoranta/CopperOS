using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellCommandDispatcherTests
{
    [Fact]
    public void DispatchByName_routes_echo_through_the_readargs_entry()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "hello";
        APTR source = platform.Store.PutAt(16, commandLine);
        APTR name = platform.Store.PutAt(2000, "Echo");
        CommandInvocation invocation = CreateInvocation(source,
            commandLine.Length, new BPTR(1));
        ShellCommandWorkspace workspace = CreateWorkspace();

        int result = ShellCommandDispatcher.DispatchByName(
            ref platform, in invocation, name, 4, in workspace);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("hello\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void DispatchByName_routes_run_to_the_process_owner()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "Echo hi";
        APTR source = platform.Store.PutAt(16, commandLine);
        APTR name = platform.Store.PutAt(2000, "Run");
        CommandInvocation invocation = CreateInvocation(source,
            commandLine.Length, BPTR.Null);
        ShellCommandWorkspace workspace = CreateWorkspace();

        int result = ShellCommandDispatcher.DispatchByName(
            ref platform, in invocation, name, 3, in workspace);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("Echo hi", platform.Store.RunCommand);
        Assert.Equal(1, platform.Store.RunCount);
    }

    [Fact]
    public void Dispatch_routes_newcli_with_the_correct_launch_identity()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0,
            BPTR.Null);
        ShellCommandWorkspace workspace = CreateWorkspace();

        int result = ShellCommandDispatcher.Dispatch(
            ref platform, in invocation, ShellInternalCommand.NewCLI,
            in workspace);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(ShellLaunchKind.NewCli,
            platform.Store.ShellLaunchKind);
        Assert.Equal(1, platform.Store.ShellLaunchCount);
    }

    [Fact]
    public void DispatchByName_rejects_unknown_commands_before_parsing()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR name = platform.Store.PutAt(2000, "Missing");
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0,
            BPTR.Null);
        ShellCommandWorkspace workspace = CreateWorkspace();

        int result = ShellCommandDispatcher.DispatchByName(
            ref platform, in invocation, name, 7, in workspace);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ReadArgsCount);
    }

    private static ShellCommandWorkspace CreateWorkspace() =>
        new(new APTR(80), 256,
            new APTR(480), 256,
            new APTR(800), 256,
            new APTR(1120), 256,
            new APTR(1440), 256,
            new APTR(1760), 512);

    private static CommandInvocation CreateInvocation(
        APTR source,
        int length,
        BPTR output) => new(
            source,
            (uint)length,
            APTR.Null,
            APTR.Null,
            BPTR.Null,
            output,
            BPTR.Null,
            BPTR.Null,
            new APTR(8),
            0,
            0);
}
