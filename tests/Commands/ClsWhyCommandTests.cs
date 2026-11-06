using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ClsWhyCommandTests
{
    [Theory]
    [InlineData(0u, "\f")]
    [InlineData(1u, "\u001bc")]
    public void Console_output_distinguishes_window_clear_from_reset(uint reset, string expected)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        Assert.True(ShellConsoleOutput.Clear(ref platform, new BPTR(1), reset));
        Assert.Equal(expected, platform.Store.OutputText);
    }

    [Fact]
    public void Console_output_rejects_invalid_streams_and_reset_values()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        Assert.False(ShellConsoleOutput.Clear(ref platform, BPTR.Null, 0));
        Assert.False(ShellConsoleOutput.Clear(ref platform, new BPTR(1), 2));
        Assert.Empty(platform.Store.OutputText);
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "\u001b")]
    public void Console_reset_reports_a_failed_write_without_sending_later_bytes(int failedByte, string written)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.FailByteWriteAt = failedByte;
        Assert.False(ShellConsoleOutput.Clear(ref platform, new BPTR(1), 1));
        Assert.Equal(written, platform.Store.OutputText);
        Assert.Equal(failedByte + 1, platform.Store.ByteWriteAttempts);
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(uint.MaxValue)]
    public void Cls_clears_the_console_and_normalizes_the_dos_reset_switch(uint trueValue)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ResetSwitchValue = trueValue;
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

        Assert.Equal((int)ShellCommandResult.Warn, result);
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
