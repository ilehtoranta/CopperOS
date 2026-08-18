using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ClsWhyCommandTests
{
    [Fact]
    public void Cls_clears_the_console_and_accepts_reset()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "RESET";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length,
            new BPTR(1), new APTR(8));

        int result = ClsCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)1, platform.Store.ClearConsoleReset);
        Assert.Equal(1, platform.Store.ClearConsoleCount);
    }

    [Fact]
    public void Cls_without_arguments_uses_normal_clear_and_rejects_unknown_switches()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0,
            new BPTR(1), new APTR(8));

        int result = ClsCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)0, platform.Store.ClearConsoleReset);
        Assert.Equal(1, platform.Store.ClearConsoleCount);

        platform = new EchoCommandTests.TestShellPlatform();
        string commandLine = "WRONG";
        APTR source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length,
            new BPTR(1), new APTR(8));
        result = ClsCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);
        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ClearConsoleCount);
    }

    [Fact]
    public void Why_delegates_last_command_diagnostic_to_the_cli_owner()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.WhyText = "Object not found\n";
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0,
            new BPTR(1), new APTR(8));

        int result = WhyCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("Object not found\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.WhyCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Why_rejects_arguments_and_missing_cli_state()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "extra";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length,
            new BPTR(1), new APTR(8));

        int result = WhyCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);
        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.WhyCount);
        Assert.Equal(0, platform.Store.ReadArgsCount);

        platform = new EchoCommandTests.TestShellPlatform();
        invocation = CreateInvocation(APTR.Null, 0, new BPTR(1), APTR.Null);
        result = WhyCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);
        Assert.Equal((int)ShellCommandResult.Fail, result);
    }

    private static CommandInvocation CreateInvocation(
        APTR source,
        int length,
        BPTR output,
        APTR cli) => new(
            source,
            (uint)length,
            APTR.Null,
            APTR.Null,
            BPTR.Null,
            output,
            BPTR.Null,
            BPTR.Null,
            cli,
            0,
            0);
}
