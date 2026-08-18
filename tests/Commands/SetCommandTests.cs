using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class SetCommandTests
{
    [Fact]
    public void Sets_a_final_string_without_requiring_quotes()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "Greeting hello world from CopperOS";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = SetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(144),
            96);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("Greeting", platform.Store.LocalVariableName);
        Assert.Equal("hello world from CopperOS", platform.Store.LocalVariableValue);
        Assert.Equal(1, platform.Store.LocalSetCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Decodes_quotes_and_star_escapes_in_the_final_string()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "Greeting \"hello *n world\"";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = SetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(144),
            96);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("hello \n world", platform.Store.LocalVariableValue);
    }

    [Fact]
    public void Allows_a_named_empty_value_and_lists_without_arguments()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "Empty";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = SetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(144),
            96);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("Empty", platform.Store.LocalVariableName);
        Assert.Equal(string.Empty, platform.Store.LocalVariableValue);

        platform = new EchoCommandTests.TestShellPlatform();
        platform.Store.LocalVariableListing = "Greeting=hello\n";
        invocation = CreateInvocation(APTR.Null, 0, new BPTR(1));
        result = SetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(144),
            96);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(0, platform.Store.LocalSetCount);
        Assert.Equal("Greeting=hello\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.LocalListCount);
    }

    [Fact]
    public void Rejects_unterminated_final_quotes_and_preserves_atomicity()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.LocalVariableName = "Existing";
        platform.Store.LocalVariableValue = "old";
        string commandLine = "New \"unterminated";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = SetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(144),
            96);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal("Existing", platform.Store.LocalVariableName);
        Assert.Equal("old", platform.Store.LocalVariableValue);
        Assert.Equal(0, platform.Store.LocalSetCount);
    }

    [Fact]
    public void Releases_arguments_before_a_failed_local_listing()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.LocalListFailure = true;
        CommandInvocation invocation = CreateInvocation(
            APTR.Null, 0, new BPTR(1));

        int result = SetCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            48,
            new APTR(144),
            96);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
        Assert.Equal(0, platform.Store.LocalListCount);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    private static CommandInvocation CreateInvocation(
        APTR source,
        int length,
        BPTR output = default) =>
        new(
            source,
            (uint)length,
            APTR.Null,
            APTR.Null,
            BPTR.Null,
            output,
            BPTR.Null,
            BPTR.Null,
            new APTR(8),
            0,
            0);
}
