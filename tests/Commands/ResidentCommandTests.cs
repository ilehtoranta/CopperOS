using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ResidentCommandTests
{
    [Fact]
    public void Resident_copies_names_and_forwards_mutation_flags()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "MyCmd SYS:C/MyCmd ADD FORCE SYSTEM DEFER";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128,
            new APTR(480), 64,
            new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("MyCmd", platform.Store.ResidentName);
        Assert.Equal("SYS:C/MyCmd", platform.Store.ResidentFile);
        Assert.Equal(string.Empty, platform.Store.ResidentAlias);
        Assert.Equal((uint)1, platform.Store.ResidentAdd);
        Assert.Equal((uint)1, platform.Store.ResidentForce);
        Assert.Equal((uint)1, platform.Store.ResidentSystem);
        Assert.Equal((uint)1, platform.Store.ResidentDefer);
        Assert.Equal(1, platform.Store.ResidentCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Resident_normalizes_DOS_ReadArgs_true_switch_values()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ResidentSwitchValue = uint.MaxValue;
        const string commandLine = "MyCmd SYS:C/MyCmd ADD FORCE SYSTEM DEFER";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source,
            commandLine.Length);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 64, new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(1u, platform.Store.ResidentAdd);
        Assert.Equal(1u, platform.Store.ResidentForce);
        Assert.Equal(1u, platform.Store.ResidentSystem);
        Assert.Equal(1u, platform.Store.ResidentDefer);
    }

    [Fact]
    public void Resident_defaults_file_admission_to_replace()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string commandLine = "MyCmd SYS:C/MyCmd";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source,
            commandLine.Length);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128,
            new APTR(480), 64,
            new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(0u, platform.Store.ResidentAdd);
        Assert.Equal(1u, platform.Store.ResidentReplace);
    }

    [Fact]
    public void Resident_alias_without_file_defaults_to_add()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string commandLine = "MyAlias ALIAS ExistingCommand";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source,
            commandLine.Length);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 64, new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("MyAlias", platform.Store.ResidentName);
        Assert.Equal(string.Empty, platform.Store.ResidentFile);
        Assert.Equal("ExistingCommand", platform.Store.ResidentAlias);
        Assert.Equal(1u, platform.Store.ResidentAdd);
        Assert.Equal(0u, platform.Store.ResidentReplace);
    }

    [Fact]
    public void Resident_rejects_alias_combined_with_backing_file()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string commandLine =
            "MyAlias SYS:C/MyAlias ALIAS ExistingCommand";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source,
            commandLine.Length);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 64, new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ResidentCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Resident_allows_listing_without_name_or_file()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128,
            new APTR(480), 64,
            new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(string.Empty, platform.Store.ResidentName);
        Assert.Equal(string.Empty, platform.Store.ResidentFile);
        Assert.Equal(1, platform.Store.ResidentCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Resident_rejects_add_without_file_instead_of_listing()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string commandLine = "MyCmd ADD";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source,
            commandLine.Length);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 64, new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.ResidentCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Resident_rejects_conflicting_mutation_switches()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string commandLine = "MyCmd REMOVE REPLACE";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source,
            commandLine.Length);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 64, new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.InternalCommandStateChangeCount);
        Assert.Equal(0, platform.Store.ResidentCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Resident_normalizes_DOS_ReadArgs_true_system_listing_switch()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ResidentSwitchValue = uint.MaxValue;
        const string commandLine = "MyCmd SYSTEM";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source,
            commandLine.Length);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128, new APTR(480), 64, new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(string.Empty, platform.Store.OutputText);
        Assert.Equal(1, platform.Store.ResidentCount);
    }

    [Fact]
    public void Resident_maps_registry_failure_to_fail()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ResidentFailure = true;
        string commandLine = "REMOVE MyCmd";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128,
            new APTR(480), 64,
            new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
        Assert.Equal(0, platform.Store.ResidentCount);
    }

    [Fact]
    public void Resident_remove_disables_internal_command_resolution()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string commandLine = "Echo REMOVE";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source,
            commandLine.Length);

        int result = ResidentCommand.Execute(ref platform, in invocation,
            new APTR(80), 128,
            new APTR(480), 64,
            new APTR(560), 128,
            new APTR(704), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(1, platform.Store.InternalCommandStateChangeCount);
        Assert.Equal(ShellInternalCommand.Unknown,
            ShellInternalCommandResolver.ResolveAvailable(ref platform,
                invocation.Cli, new APTR(480), 4));
    }

    [Fact]
    public void Resident_replace_reactivates_removed_internal_command()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string removeLine = "Echo REMOVE";
        APTR removeSource = platform.Store.PutAt(16, removeLine);
        CommandInvocation invocation = CreateInvocation(removeSource,
            removeLine.Length);
        Assert.Equal((int)ShellCommandResult.Ok, ResidentCommand.Execute(
            ref platform, in invocation, new APTR(80), 128, new APTR(480), 64,
            new APTR(560), 128, new APTR(704), 64));

        const string replaceLine = "Echo REPLACE";
        APTR replaceSource = platform.Store.PutAt(16, replaceLine);
        invocation = CreateInvocation(replaceSource, replaceLine.Length);
        Assert.Equal((int)ShellCommandResult.Ok, ResidentCommand.Execute(
            ref platform, in invocation, new APTR(80), 128, new APTR(480), 64,
            new APTR(560), 128, new APTR(704), 64));

        Assert.Equal(ShellInternalCommand.Echo,
            ShellInternalCommandResolver.ResolveAvailable(ref platform,
                invocation.Cli, new APTR(480), 4));
    }

    [Fact]
    public void Resident_listing_includes_active_internal_names_but_not_system_only()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        Assert.Equal((int)ShellCommandResult.Ok, ResidentCommand.Execute(
            ref platform, in invocation, new APTR(80), 128, new APTR(480), 64,
            new APTR(560), 128, new APTR(704), 64));
        Assert.Contains("Echo\n", platform.Store.OutputText);

        platform.Store.Output.Clear();
        const string systemLine = "SYSTEM";
        APTR source = platform.Store.PutAt(16, systemLine);
        invocation = CreateInvocation(source, systemLine.Length);
        Assert.Equal((int)ShellCommandResult.Ok, ResidentCommand.Execute(
            ref platform, in invocation, new APTR(80), 128, new APTR(480), 64,
            new APTR(560), 128, new APTR(704), 64));
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    private static CommandInvocation CreateInvocation(APTR source, int length) =>
        new(source, (uint)length, APTR.Null, APTR.Null, BPTR.Null,
            new BPTR(1), BPTR.Null, BPTR.Null, new APTR(8), 0, 0);
}
