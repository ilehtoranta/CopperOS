using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class StackCommandTests
{
    [Theory]
    [InlineData(0, "0\n")]
    [InlineData(9, "9\n")]
    [InlineData(10, "10\n")]
    [InlineData(99, "99\n")]
    [InlineData(100, "100\n")]
    [InlineData(8192, "8192\n")]
    [InlineData(999_999_999, "999999999\n")]
    [InlineData(1_000_000_000, "1000000000\n")]
    [InlineData(int.MaxValue, "2147483647\n")]
    public void No_argument_reports_the_current_cli_default_stack(int stackBytes,
        string expected)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.DefaultStack = stackBytes;
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
        Assert.Equal(expected, platform.Store.OutputText);
        Assert.Equal(0, platform.Store.WriteStackCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
        Assert.Equal("SIZE/N", platform.Store.LastReadArgsTemplate);
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
    [InlineData("4294967295")]
    [InlineData("-2147483648")]
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

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Rejects_an_invalid_ReadArgs_number_value(bool unmapped,
        bool misaligned)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.InvalidReadArgsNumberValueAddress = unmapped;
        platform.Store.MisalignedReadArgsNumberValueAddress = misaligned;
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

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(8192, platform.Store.DefaultStack);
        Assert.Equal(0, platform.Store.WriteStackCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
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
