using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class IfCommandTests
{
    [Fact]
    public void If_previous_result_condition_records_warn_threshold()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "WARN NOREQ";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = IfCommand.Execute(
            ref platform,
            in invocation,
            new APTR(320),
            128,
            new APTR(480),
            48,
            new APTR(544),
            48);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)ShellIfCondition.PreviousResult,
            platform.Store.IfCondition);
        Assert.Equal((uint)ShellCommandResult.Warn, platform.Store.IfThreshold);
        Assert.Equal((uint)1, platform.Store.IfNoRequester);
        Assert.Equal((uint)0, platform.Store.IfNumeric);
        Assert.Equal(1, platform.Store.IfCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void If_multiple_result_thresholds_use_the_lowest_level()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "FAIL ERROR WARN";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = IfCommand.Execute(
            ref platform,
            in invocation,
            new APTR(320),
            128,
            new APTR(480),
            48,
            new APTR(544),
            48);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)ShellCommandResult.Warn,
            platform.Store.IfThreshold);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void If_comparison_conditions_preserve_decoded_operands_and_NOT()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "NOT \"Hello World\" EQ \"hello world\" NOREQ";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = IfCommand.Execute(
            ref platform,
            in invocation,
            new APTR(320),
            128,
            new APTR(480),
            48,
            new APTR(544),
            48);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)ShellIfCondition.Equal, platform.Store.IfCondition);
        Assert.Equal((uint)1, platform.Store.IfNegate);
        Assert.Equal((uint)1, platform.Store.IfNoRequester);
        Assert.Equal("Hello World", platform.Store.IfLeft);
        Assert.Equal("hello world", platform.Store.IfRight);
        Assert.Equal((uint)0, platform.Store.IfNumeric);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void If_exists_accepts_one_path_operand()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "EXISTS Work/Prog";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = IfCommand.Execute(
            ref platform,
            in invocation,
            new APTR(320),
            128,
            new APTR(480),
            48,
            new APTR(544),
            48);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)ShellIfCondition.Exists, platform.Store.IfCondition);
        Assert.Equal("Work/Prog", platform.Store.IfLeft);
        Assert.Equal(string.Empty, platform.Store.IfRight);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void If_VAL_marks_the_comparison_as_numeric()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "500 GT 200 VAL";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = IfCommand.Execute(
            ref platform,
            in invocation,
            new APTR(320),
            128,
            new APTR(480),
            48,
            new APTR(544),
            48);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)ShellIfCondition.Greater,
            platform.Store.IfCondition);
        Assert.Equal((uint)1, platform.Store.IfNumeric);
        Assert.Equal("500", platform.Store.IfLeft);
        Assert.Equal("200", platform.Store.IfRight);
    }

    [Theory]
    [InlineData("")]
    [InlineData("EQ only-one")]
    [InlineData("EQ one two extra")]
    [InlineData("WARN UNKNOWN")]
    [InlineData("UNKNOWN")]
    public void If_rejects_missing_extra_conflicting_or_unknown_conditions(
        string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = commandLine.Length == 0
            ? APTR.Null
            : platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = IfCommand.Execute(
            ref platform,
            in invocation,
            new APTR(320),
            128,
            new APTR(480),
            48,
            new APTR(544),
            48);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.IfCount);
    }

    [Fact]
    public void If_maps_the_script_owner_failure()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.IfFailure = true;
        string commandLine = "FAIL";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = IfCommand.Execute(
            ref platform,
            in invocation,
            new APTR(320),
            128,
            new APTR(480),
            48,
            new APTR(544),
            48);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(0, platform.Store.IfCount);
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
