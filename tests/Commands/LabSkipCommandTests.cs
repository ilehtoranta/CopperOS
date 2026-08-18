using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class LabSkipCommandTests
{
    [Fact]
    public void Lab_registers_one_label_after_validating_the_line()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "build";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = LabCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("build", platform.Store.LastLabel);
        Assert.Equal(1, platform.Store.LabelDefineCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("build extra")]
    [InlineData("\"unterminated")]
    public void Lab_rejects_missing_extra_or_malformed_labels(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = commandLine.Length == 0
            ? APTR.Null
            : platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = LabCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.LabelDefineCount);
    }

    [Fact]
    public void Skip_supports_next_label_named_label_and_BACK()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        int result = SkipCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(string.Empty, platform.Store.LastSkipLabel);
        Assert.Equal((uint)0, platform.Store.SkipBack);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);

        string commandLine = "build BACK";
        APTR source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);
        result = SkipCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("build", platform.Store.LastSkipLabel);
        Assert.Equal((uint)1, platform.Store.SkipBack);
        Assert.Equal(2, platform.Store.ReadArgsCount);
        Assert.Equal(2, platform.Store.FreeArgsCount);

        platform = new EchoCommandTests.TestShellPlatform();
        commandLine = "BACK";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);
        result = SkipCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(string.Empty, platform.Store.LastSkipLabel);
        Assert.Equal((uint)1, platform.Store.SkipBack);
    }

    [Theory]
    [InlineData("build WRONG")]
    [InlineData("BACK extra")]
    [InlineData("\"unterminated")]
    public void Skip_rejects_unknown_or_malformed_options_without_requesting_a_jump(
        string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = SkipCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.SkipCount);
    }

    [Fact]
    public void Lab_and_Skip_map_script_owner_failures()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.LabelDefineFailure = true;
        string commandLine = "label";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = LabCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);
        Assert.Equal((int)ShellCommandResult.Fail, result);

        platform = new EchoCommandTests.TestShellPlatform();
        platform.Store.SkipFailure = true;
        source = platform.Store.PutAt(16, "label");
        invocation = CreateInvocation(source, 5);
        result = SkipCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);
        Assert.Equal((int)ShellCommandResult.Fail, result);
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
