using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class AskCommandTests
{
    [Fact]
    public void Ask_decodes_the_prompt_and_delegates_input_and_condition_state()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "Do you own a Pegasos? *n";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = new(
            source,
            (uint)commandLine.Length,
            APTR.Null,
            APTR.Null,
            new BPTR(2),
            new BPTR(1),
            BPTR.Null,
            BPTR.Null,
            new APTR(8),
            0,
            0);

        int result = AskCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            96);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("Do you own a Pegasos? \n", platform.Store.AskPrompt);
        Assert.Equal(1, platform.Store.AskCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\"unterminated")]
    public void Ask_rejects_missing_or_malformed_prompts(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = commandLine.Length == 0
            ? APTR.Null
            : platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = new(
            source,
            (uint)commandLine.Length,
            APTR.Null,
            APTR.Null,
            new BPTR(2),
            new BPTR(1),
            BPTR.Null,
            BPTR.Null,
            new APTR(8),
            0,
            0);

        int result = AskCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            96);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.AskCount);
    }

    [Fact]
    public void Ask_requires_cli_and_inherited_input_and_output()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "Continue?";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = new(
            source,
            (uint)commandLine.Length,
            APTR.Null,
            APTR.Null,
            BPTR.Null,
            new BPTR(1),
            BPTR.Null,
            BPTR.Null,
            new APTR(8),
            0,
            0);

        int result = AskCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            96);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(0, platform.Store.AskCount);
    }

    [Fact]
    public void Ask_maps_the_interactive_owner_failure()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AskFailure = true;
        string commandLine = "Continue?";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = new(
            source,
            (uint)commandLine.Length,
            APTR.Null,
            APTR.Null,
            new BPTR(2),
            new BPTR(1),
            BPTR.Null,
            BPTR.Null,
            new APTR(8),
            0,
            0);

        int result = AskCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            96);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(0, platform.Store.AskCount);
    }
}
