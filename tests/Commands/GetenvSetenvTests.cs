using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class GetenvSetenvTests
{
    [Fact]
    public void Getenv_reads_a_global_value_and_appends_a_line()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.GlobalVariableName = "MOS";
        platform.Store.GlobalVariableValue = "MorphOS RULEZ";
        string commandLine = "mos";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length,
            new BPTR(1));

        int result = GetenvCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            40,
            new APTR(144),
            80);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("MorphOS RULEZ\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Setenv_preserves_the_final_string_and_supports_save()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "MOS MorphOS RULEZ !";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length,
            BPTR.Null);

        int result = SetenvCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            40,
            new APTR(120),
            32,
            new APTR(160),
            80);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("MOS", platform.Store.GlobalVariableName);
        Assert.Equal("MorphOS RULEZ !", platform.Store.GlobalVariableValue);
        Assert.Equal((uint)0, platform.Store.GlobalSaveFlag);

        platform = new EchoCommandTests.TestShellPlatform();
        commandLine = "MOS SAVE MorphOS after reboot";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length, BPTR.Null);

        result = SetenvCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            40,
            new APTR(120),
            32,
            new APTR(160),
            80);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("MorphOS after reboot", platform.Store.GlobalVariableValue);
        Assert.Equal((uint)1, platform.Store.GlobalSaveFlag);
    }

    [Fact]
    public void Quoted_save_is_part_of_the_final_value_not_the_switch()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "MOS \"SAVE value\"";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length,
            BPTR.Null);

        int result = SetenvCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            40,
            new APTR(120),
            32,
            new APTR(160),
            80);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("SAVE value", platform.Store.GlobalVariableValue);
        Assert.Equal((uint)0, platform.Store.GlobalSaveFlag);
    }

    [Theory]
    [InlineData("")]
    [InlineData("MOS extra")]
    public void Global_commands_reject_missing_or_extra_names(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = commandLine.Length == 0
            ? APTR.Null
            : platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length,
            new BPTR(1));

        int result = GetenvCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            40,
            new APTR(144),
            80);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    [Fact]
    public void Setenv_without_arguments_lists_global_variables()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.GlobalVariableListing = "MOS=MorphOS\n";
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0,
            new BPTR(1));

        int result = SetenvCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            40,
            new APTR(120),
            32,
            new APTR(160),
            80);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(0, platform.Store.GlobalSetCount);
        Assert.Equal("MOS=MorphOS\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.GlobalListCount);
    }

    [Fact]
    public void Setenv_releases_arguments_before_a_failed_global_listing()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.GlobalListFailure = true;
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0,
            new BPTR(1));

        int result = SetenvCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            40,
            new APTR(120),
            32,
            new APTR(160),
            80);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
        Assert.Equal(0, platform.Store.GlobalListCount);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    private static CommandInvocation CreateInvocation(
        APTR source,
        int length,
        BPTR output) =>
        new(
            source,
            (uint)length,
            APTR.Null,
            APTR.Null,
            BPTR.Null,
            output,
            BPTR.Null,
            BPTR.Null,
            APTR.Null,
            0,
            0);
}
