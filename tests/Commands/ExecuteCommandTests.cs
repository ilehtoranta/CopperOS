using Amiga;
using CopperOS.Commands;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ExecuteCommandTests
{
    [Fact]
    public void Execute_passes_one_script_file_to_the_active_shell_engine()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "S:Startup";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = ExecuteCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("S:Startup", platform.Store.ExecutedScript);
        Assert.Equal(1, platform.Store.ExecuteCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Execute_decodes_quoted_file_names_and_rejects_extra_arguments()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "\"S:My Startup\"";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = ExecuteCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("S:My Startup", platform.Store.ExecutedScript);

        platform = new EchoCommandTests.TestShellPlatform();
        commandLine = "S:Startup extra";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);
        result = ExecuteCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ExecuteCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("\"unterminated")]
    public void Execute_rejects_missing_or_malformed_file_arguments(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = commandLine.Length == 0
            ? APTR.Null
            : platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = ExecuteCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ExecuteCount);
    }

    [Fact]
    public void Execute_maps_script_owner_failure()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ExecuteFailure = true;
        string commandLine = "S:Startup";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = ExecuteCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(0, platform.Store.ExecuteCount);
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
