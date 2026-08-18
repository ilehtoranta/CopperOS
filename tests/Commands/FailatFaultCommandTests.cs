using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class FailatFaultCommandTests
{
    [Fact]
    public void Failat_updates_the_cli_failure_limit()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "5";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = FailatCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)5, platform.Store.FailureLimit);
        Assert.Equal(1, platform.Store.WriteFailureLimitCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("bad")]
    [InlineData("5 extra")]
    public void Failat_rejects_missing_invalid_or_extra_arguments(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = commandLine.Length == 0
            ? APTR.Null
            : platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = FailatCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal((uint)10, platform.Store.FailureLimit);
        Assert.Equal(0, platform.Store.WriteFailureLimitCount);
    }

    [Fact]
    public void Failat_maps_a_dos_failure_without_mutating_the_limit()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.WriteFailureLimitFailure = true;
        string commandLine = "20";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = FailatCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32);

        Assert.Equal((int)ShellCommandResult.Fail, result);
        Assert.Equal((uint)10, platform.Store.FailureLimit);
        Assert.Equal(0, platform.Store.WriteFailureLimitCount);
    }

    [Fact]
    public void Fault_passes_multiple_numeric_codes_to_the_dos_owner()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.FaultText = "Fault 204: directory not found\n" +
            "Fault 205: object not found\n";
        string commandLine = "204 205";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = FaultCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)2, platform.Store.FaultCount);
        Assert.Equal((uint)204, platform.Store.FaultCodes[0]);
        Assert.Equal((uint)205, platform.Store.FaultCodes[1]);
        Assert.Equal(platform.Store.FaultText, platform.Store.OutputText);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("204 bad")]
    public void Fault_rejects_missing_or_malformed_codes_without_output(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = commandLine.Length == 0
            ? APTR.Null
            : platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = FaultCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(160),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal((uint)0, platform.Store.FaultCount);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    [Fact]
    public void Fault_rejects_more_codes_than_the_caller_buffer_can_hold()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "1 2";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = FaultCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(160),
            4);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal((uint)0, platform.Store.FaultCount);
        Assert.Equal(string.Empty, platform.Store.OutputText);
    }

    private static CommandInvocation CreateInvocation(APTR source, int length) => new(
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
