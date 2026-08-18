using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class PathCommandTests
{
    [Fact]
    public void Path_without_arguments_lists_the_current_search_path()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.CommandPathListing = "SYS:C\nSYS:Tools\n";
        CommandInvocation invocation = CreateInvocation(
            APTR.Null,
            0,
            new BPTR(1));

        int result = PathCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            80,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(platform.Store.CommandPathListing, platform.Store.OutputText);
        Assert.Equal(0, platform.Store.CommandPathUpdateCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Path_replaces_the_search_path_by_default()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "SYS:C \"SYS:My Tools\"";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = PathCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            80,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)PathCommand.OperationReset,
            platform.Store.CommandPathOperation);
        Assert.Equal((uint)0, platform.Store.CommandPathQuiet);
        Assert.Equal(2, platform.Store.CommandPathEntries.Count);
        Assert.Equal("SYS:C", platform.Store.CommandPathEntries[0]);
        Assert.Equal("SYS:My Tools", platform.Store.CommandPathEntries[1]);
    }

    [Fact]
    public void Path_supports_add_remove_reset_and_quiet_switches()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "SYS:Tools ADD QUIET";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = PathCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            80,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)PathCommand.OperationAdd,
            platform.Store.CommandPathOperation);
        Assert.Equal((uint)1, platform.Store.CommandPathQuiet);

        platform = new EchoCommandTests.TestShellPlatform();
        commandLine = "SYS:Old REMOVE";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);
        result = PathCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            80,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)PathCommand.OperationRemove,
            platform.Store.CommandPathOperation);

        platform = new EchoCommandTests.TestShellPlatform();
        commandLine = "RESET";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);
        result = PathCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            80,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)PathCommand.OperationReset,
            platform.Store.CommandPathOperation);
        Assert.Empty(platform.Store.CommandPathEntries);
    }

    [Fact]
    public void Path_show_is_exclusive_and_quoted_keywords_remain_paths()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "\"SHOW\"";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = PathCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            80,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Single(platform.Store.CommandPathEntries);
        Assert.Equal("SHOW", platform.Store.CommandPathEntries[0]);

        platform = new EchoCommandTests.TestShellPlatform();
        commandLine = "SHOW SYS:C";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length, new BPTR(1));
        result = PathCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            80,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.CommandPathUpdateCount);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    [Fact]
    public void Path_rejects_conflicting_switches_and_capacity_overflow_atomically()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "SYS:C ADD REMOVE";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = PathCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            80,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.CommandPathUpdateCount);

        platform = new EchoCommandTests.TestShellPlatform();
        commandLine = "A B C";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);
        result = PathCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            80,
            new APTR(160),
            4);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.CommandPathUpdateCount);
    }

    [Fact]
    public void Path_maps_dos_owner_failures()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.CommandPathUpdateFailure = true;
        string commandLine = "SYS:C";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = PathCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            80,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(0, platform.Store.CommandPathUpdateCount);
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
