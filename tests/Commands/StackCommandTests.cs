using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class StackCommandTests
{
    [Fact]
    public void No_argument_reports_the_current_cli_default_stack()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = new(
            APTR.Null,
            0,
            APTR.Null,
            APTR.Null,
            BPTR.Null,
            new BPTR(1),
            BPTR.Null,
            BPTR.Null,
            new APTR(8),
            0,
            0);

        int result = StackCommand.Execute(
            ref platform,
            in invocation,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("8192\n", platform.Store.OutputText);
        Assert.Equal(0, platform.Store.WriteStackCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Numeric_argument_changes_only_the_future_child_stack()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "16384";
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

        int result = StackCommand.Execute(
            ref platform,
            in invocation,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(16384, platform.Store.DefaultStack);
        Assert.Equal(4096, platform.Store.RunningStack);
        Assert.Equal(1, platform.Store.WriteStackCount);
        Assert.Equal(string.Empty, platform.Store.OutputText);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("abc")]
    [InlineData("16384 extra")]
    public void Rejects_invalid_or_extra_arguments(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
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

        int result = StackCommand.Execute(
            ref platform,
            in invocation,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(8192, platform.Store.DefaultStack);
        Assert.Equal(0, platform.Store.WriteStackCount);
    }

    [Fact]
    public void Rejects_missing_cli_state_without_writing()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = new(
            APTR.Null,
            0,
            APTR.Null,
            APTR.Null,
            BPTR.Null,
            new BPTR(1),
            BPTR.Null,
            BPTR.Null,
            APTR.Null,
            0,
            0);

        int result = StackCommand.Execute(
            ref platform,
            in invocation,
            new APTR(224),
            32);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }
}
