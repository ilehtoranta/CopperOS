using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class GetCommandTests
{
    [Fact]
    public void Reads_a_local_variable_and_writes_a_line()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.LocalVariableName = "Greeting";
        platform.Store.LocalVariableValue = "hello world";
        string commandLine = "Greeting";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = GetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(144),
            80);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("hello world\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.ReadArgsCount);
    }

    [Fact]
    public void Variable_names_are_case_insensitive_but_missing_names_fail()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.LocalVariableName = "Greeting";
        platform.Store.LocalVariableValue = "hello";
        string commandLine = "gReEtInG";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = GetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(144),
            80);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("hello\n", platform.Store.OutputText);

        platform = new EchoCommandTests.TestShellPlatform();
        commandLine = "Missing";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);

        result = GetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(144),
            80);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Greeting extra")]
    public void Enforces_the_required_single_name(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.LocalVariableName = "Greeting";
        platform.Store.LocalVariableValue = "hello";
        APTR source = commandLine.Length == 0
            ? APTR.Null
            : platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = GetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(144),
            80);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    private static CommandInvocation CreateInvocation(APTR source, int length) =>
        new(
            source,
            (uint)length,
            APTR.Null,
            APTR.Null,
            BPTR.Null,
            new BPTR(1),
            BPTR.Null,
            BPTR.Null,
            new APTR(8),
            0,
            0);
}
