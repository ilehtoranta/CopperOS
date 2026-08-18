using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class AliasCommandTests
{
    [Fact]
    public void Alias_sets_a_decoded_final_replacement()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "hi echo \"Hello !\"";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = AliasCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            96);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("hi", platform.Store.AliasName);
        Assert.Equal("echo Hello !", platform.Store.AliasValue);
        Assert.Equal(1, platform.Store.AliasSetCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Alias_without_arguments_lists_aliases()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AliasListing = "hi = echo Hello !\n";
        CommandInvocation invocation = CreateInvocation(
            APTR.Null,
            0,
            new BPTR(1));

        int result = AliasCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            96);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(platform.Store.AliasListing, platform.Store.OutputText);
        Assert.Equal(0, platform.Store.AliasSetCount);
    }

    [Fact]
    public void Alias_accepts_an_explicit_empty_replacement()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "empty \"\"";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = AliasCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            96);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("empty", platform.Store.AliasName);
        Assert.Equal(string.Empty, platform.Store.AliasValue);
    }

    [Fact]
    public void Alias_rejects_malformed_replacement_without_mutation()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AliasName = "old";
        platform.Store.AliasValue = "echo old";
        string commandLine = "new \"unterminated";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = AliasCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            96);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal("old", platform.Store.AliasName);
        Assert.Equal("echo old", platform.Store.AliasValue);
        Assert.Equal(0, platform.Store.AliasSetCount);
    }

    [Fact]
    public void Unalias_removes_one_name_after_validating_extra_arguments()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AliasName = "hi";
        platform.Store.AliasValue = "echo Hello";
        string commandLine = "hi";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = UnaliasCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(string.Empty, platform.Store.AliasName);
        Assert.Equal(1, platform.Store.AliasRemoveCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);

        platform = new EchoCommandTests.TestShellPlatform();
        platform.Store.AliasName = "hi";
        commandLine = "hi extra";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);
        result = UnaliasCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            32);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal("hi", platform.Store.AliasName);
        Assert.Equal(0, platform.Store.AliasRemoveCount);
    }

    [Fact]
    public void Unalias_without_arguments_lists_aliases()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AliasListing = "hi = echo Hello\n";
        CommandInvocation invocation = CreateInvocation(
            APTR.Null,
            0,
            new BPTR(1));

        int result = UnaliasCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(platform.Store.AliasListing, platform.Store.OutputText);
    }

    [Fact]
    public void Alias_and_Unalias_map_owner_failures()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.AliasSetFailure = true;
        string commandLine = "hi echo";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = AliasCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            96);
        Assert.Equal((int)ShellCommandResult.Fail, result);

        platform = new EchoCommandTests.TestShellPlatform();
        platform.Store.AliasName = "hi";
        platform.Store.AliasRemoveFailure = true;
        source = platform.Store.PutAt(16, "hi");
        invocation = CreateInvocation(source, 2);
        result = UnaliasCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            32);
        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal("hi", platform.Store.AliasName);
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
