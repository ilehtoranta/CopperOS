using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ResidentCommandTests
{
    [Fact]
    public void Resident_copies_names_and_forwards_mutation_flags()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "MyCmd SYS:C/MyCmd ALIAS OldCmd ADD FORCE SYSTEM DEFER";
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
        Assert.Equal("OldCmd", platform.Store.ResidentAlias);
        Assert.Equal((uint)1, platform.Store.ResidentAdd);
        Assert.Equal((uint)1, platform.Store.ResidentForce);
        Assert.Equal((uint)1, platform.Store.ResidentSystem);
        Assert.Equal((uint)1, platform.Store.ResidentDefer);
        Assert.Equal(1, platform.Store.ResidentCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
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

    private static CommandInvocation CreateInvocation(APTR source, int length) =>
        new(source, (uint)length, APTR.Null, APTR.Null, BPTR.Null,
            new BPTR(1), BPTR.Null, BPTR.Null, new APTR(8), 0, 0);
}
