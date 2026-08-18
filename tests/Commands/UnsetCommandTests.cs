using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class UnsetCommandTests
{
    [Fact]
    public void Removes_a_named_local_variable()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.LocalVariableName = "Greeting";
        platform.Store.LocalVariableValue = "hello";
        string commandLine = "greeting";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length,
            new APTR(8));

        int result = UnsetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(128),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(string.Empty, platform.Store.LocalVariableName);
        Assert.Equal(1, platform.Store.LocalRemoveCount);
    }

    [Fact]
    public void Unset_rejects_extra_names_and_lists_without_arguments()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.LocalVariableName = "Greeting";
        string commandLine = "Greeting extra";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length,
            new APTR(8));

        int result = UnsetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(128),
            32);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal("Greeting", platform.Store.LocalVariableName);
        Assert.Equal(0, platform.Store.LocalRemoveCount);

        platform = new EchoCommandTests.TestShellPlatform();
        platform.Store.LocalVariableListing = "Greeting=hello\n";
        invocation = CreateInvocation(APTR.Null, 0, new APTR(8),
            new BPTR(1));
        result = UnsetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(128),
            32);
        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("Greeting=hello\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.LocalListCount);
    }

    [Fact]
    public void Unsetenv_accepts_optional_save_for_persistent_removal()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.GlobalVariableName = "MOS";
        platform.Store.GlobalVariableValue = "value";
        string commandLine = "mos SAVE";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length,
            APTR.Null);

        int result = UnsetenvCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            40,
            new APTR(128),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(string.Empty, platform.Store.GlobalVariableName);
        Assert.Equal((uint)1, platform.Store.GlobalRemoveSaveFlag);
        Assert.Equal(1, platform.Store.GlobalRemoveCount);
    }

    [Fact]
    public void Unsetenv_rejects_unknown_switches_and_lists_without_arguments()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.GlobalVariableName = "MOS";
        string commandLine = "MOS NOW";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length,
            APTR.Null);

        int result = UnsetenvCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            40,
            new APTR(128),
            32);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal("MOS", platform.Store.GlobalVariableName);
        Assert.Equal(0, platform.Store.GlobalRemoveCount);

        platform = new EchoCommandTests.TestShellPlatform();
        platform.Store.GlobalVariableListing = "MOS=value\n";
        invocation = CreateInvocation(APTR.Null, 0, APTR.Null,
            new BPTR(1));
        result = UnsetenvCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            40,
            new APTR(128),
            32);
        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("MOS=value\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.GlobalListCount);
    }

    private static CommandInvocation CreateInvocation(
        APTR source,
        int length,
        APTR cli,
        BPTR output = default) => new(
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
