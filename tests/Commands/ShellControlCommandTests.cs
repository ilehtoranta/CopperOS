using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellControlCommandTests
{
    [Fact]
    public void No_argument_control_commands_delegate_fixed_width_actions()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        Assert.Equal((int)ShellCommandResult.Ok,
            ElseCommand.Execute(ref platform, in invocation, new APTR(80), 32));
        Assert.Equal(ShellControlAction.Else, platform.Store.LastControlAction);

        Assert.Equal((int)ShellCommandResult.Ok,
            EndIfCommand.Execute(ref platform, in invocation, new APTR(80), 32));
        Assert.Equal(ShellControlAction.EndIf, platform.Store.LastControlAction);

        Assert.Equal((int)ShellCommandResult.Ok,
            EndSkipCommand.Execute(ref platform, in invocation, new APTR(80), 32));
        Assert.Equal(ShellControlAction.EndSkip, platform.Store.LastControlAction);

        Assert.Equal((int)ShellCommandResult.Ok,
            EndCliCommand.Execute(ref platform, in invocation, new APTR(80), 32));
        Assert.Equal(ShellControlAction.EndCli, platform.Store.LastControlAction);

        Assert.Equal((int)ShellCommandResult.Ok,
            EndShellCommand.Execute(ref platform, in invocation, new APTR(80), 32));
        Assert.Equal(ShellControlAction.EndShell, platform.Store.LastControlAction);
        Assert.Equal(5, platform.Store.ControlCount);
        Assert.Equal(5, platform.Store.ReadArgsCount);
        Assert.Equal(5, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void No_argument_control_commands_reject_arguments()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "unexpected";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = ElseCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ControlCount);
        Assert.Equal(0, platform.Store.ReadArgsCount);
    }

    [Fact]
    public void Quit_uses_zero_by_default_and_accepts_a_bounded_return_code()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        int result = QuitCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(ShellControlAction.Quit, platform.Store.LastControlAction);
        Assert.Equal(0, platform.Store.LastControlReturnCode);

        string commandLine = "20";
        APTR source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);
        result = QuitCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(20, platform.Store.LastControlReturnCode);
        Assert.Equal(2, platform.Store.ControlCount);
        Assert.Equal(2, platform.Store.ReadArgsCount);
        Assert.Equal(2, platform.Store.FreeArgsCount);
    }

    [Theory]
    [InlineData("bad")]
    [InlineData("2147483648")]
    [InlineData("20 extra")]
    public void Quit_rejects_invalid_or_extra_return_codes(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = QuitCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ControlCount);
    }

    [Fact]
    public void Control_owner_failure_maps_to_fail_without_recording_a_request()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ControlFailure = true;
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        int result = EndShellCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(0, platform.Store.ControlCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    private static CommandInvocation CreateInvocation(APTR source, int length) => new(
        source,
        (uint)length,
        APTR.Null,
        APTR.Null,
        BPTR.Null,
        BPTR.Null,
        BPTR.Null,
        BPTR.Null,
        new APTR(8),
        0,
        0);
}
