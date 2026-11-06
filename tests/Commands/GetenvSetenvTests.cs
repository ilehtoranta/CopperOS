using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class GetenvSetenvTests
{
    [Theory]
    [InlineData(1u)]
    [InlineData(uint.MaxValue)]
    public void Save_switch_normalizes_ReadArgs_true_before_global_services(uint trueValue)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.SaveSwitchValue = trueValue;
        var text = "BIVar SAVE second";
        var source = platform.Store.PutAt(16, text);
        var invocation = CreateInvocation(source, text.Length, BPTR.Null);
        Assert.Equal(0, SetenvCommand.Execute(ref platform, in invocation,
            new APTR(80), 40, new APTR(120), 32, new APTR(160), 80));
        Assert.Equal(1u, platform.Store.GlobalSaveFlag);
        text = "BIVar SAVE";
        source = platform.Store.PutAt(16, text);
        invocation = CreateInvocation(source, text.Length, BPTR.Null);
        Assert.Equal(0, UnsetenvCommand.Execute(ref platform, in invocation,
            new APTR(80), 40, new APTR(128), 32));
        Assert.Equal(1u, platform.Store.GlobalRemoveSaveFlag);
    }

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
        platform.Store.GlobalListResult = (int)ShellCommandResult.Warn;
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

        Assert.Equal((int)ShellCommandResult.Warn, result);
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

    [Fact]
    public void Setenv_preserves_listing_warning_and_command_streams()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.GlobalListResult = (int)ShellCommandResult.Warn;
        var input = BPTR.FromRaw(2);
        var error = BPTR.FromRaw(3);
        var invocation = new CommandInvocation(APTR.Null, 0, APTR.Null, APTR.Null,
            input, BPTR.FromRaw(1), error, BPTR.Null, APTR.Null, 0, 0,
            APTR.Null, 0, error);

        var result = SetenvCommand.Execute(ref platform, in invocation,
            new APTR(80), 40, new APTR(120), 32, new APTR(160), 80);

        Assert.Equal((int)ShellCommandResult.Warn, result);
        Assert.Equal(input, platform.Store.GlobalListInput);
        Assert.Equal(error, platform.Store.GlobalListError);
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
