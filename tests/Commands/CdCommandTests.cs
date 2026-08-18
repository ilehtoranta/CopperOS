using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class CdCommandTests
{
    [Fact]
    public void Cd_without_arguments_prints_the_dos_owned_current_directory()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.CurrentDirectory = "SYS:Work";
        CommandInvocation invocation = CreateInvocation(
            APTR.Null,
            0,
            new BPTR(1));

        int result = CdCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("SYS:Work\n", platform.Store.OutputText);
        Assert.Equal(0, platform.Store.ChangeDirectoryCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Cd_changes_the_current_directory_without_requiring_output()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "RAM:Work";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(
            source,
            commandLine.Length,
            BPTR.Null);

        int result = CdCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("RAM:Work", platform.Store.CurrentDirectory);
        Assert.Equal(1, platform.Store.ChangeDirectoryCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Cd_decodes_a_quoted_path_and_rejects_extra_tokens()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "\"DH0:My Dir\"";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(
            source,
            commandLine.Length,
            BPTR.Null);

        int result = CdCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("DH0:My Dir", platform.Store.CurrentDirectory);

        platform = new EchoCommandTests.TestShellPlatform();
        commandLine = "RAM: one-more";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(
            source,
            commandLine.Length,
            BPTR.Null);
        result = CdCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal("SYS:", platform.Store.CurrentDirectory);
        Assert.Equal(0, platform.Store.ChangeDirectoryCount);
    }

    [Fact]
    public void Cd_maps_directory_lookup_and_output_failures_deterministically()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.CurrentDirectoryFailure = true;
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        int result = CdCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Fail, result);

        platform = new EchoCommandTests.TestShellPlatform();
        platform.Store.ChangeDirectoryFailure = true;
        string commandLine = "RAM:";
        APTR source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length, BPTR.Null);
        result = CdCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ChangeDirectoryCount);
    }

    private static CommandInvocation CreateInvocation(
        APTR source,
        int length,
        BPTR output = default) => new(
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
