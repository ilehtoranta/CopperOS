using Amiga;
using CopperOS.Commands;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class EvalCommandTests
{
    [Fact]
    public void Morphos_profile_evaluates_and_writes_decimal_output()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string sourceText = "1+2*3";
        APTR source = platform.Store.PutAt(16, sourceText);
        CommandInvocation invocation = CommandInvocation.ForOutput(source,
            (uint)sourceText.Length, new BPTR(1));

        var result = EvalCommand.Execute(ref platform, in invocation,
            EvalCommandProfile.MorphOS320, new APTR(128), 64, new APTR(256), 24,
            new APTR(320), 256, new APTR(640), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("7\n", platform.Store.OutputText);
        Assert.Equal("VALUE1/A,OP,VALUE2/M,TO/K,LFORMAT/K,HEX/S",
            platform.Store.LastReadArgsTemplate);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Workbench_profile_accepts_its_exact_template_workspace()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string sourceText = "42";
        APTR source = platform.Store.PutAt(16, sourceText);
        CommandInvocation invocation = CommandInvocation.ForOutput(source,
            (uint)sourceText.Length, new BPTR(1));

        var result = EvalCommand.Execute(ref platform, in invocation,
            EvalCommandProfile.Workbench31, new APTR(128), 36, new APTR(256), 24,
            new APTR(320), 256, new APTR(640), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("42\n", platform.Store.OutputText);
        Assert.Equal("VALUE1/A,OP,VALUE2/M,TO/K,LFORMAT/K",
            platform.Store.LastReadArgsTemplate);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Theory]
    [InlineData("1+2*3+4", "13\n")]
    [InlineData("1+(2*3)", "7\n")]
    [InlineData("-2+3", "1\n")]
    [InlineData("0x10", "16\n")]
    [InlineData("#x10", "16\n")]
    [InlineData("010", "8\n")]
    [InlineData("42 LFORMAT=x=%x", "x=A")]
    [InlineData("42 LFORMAT=x=%X2", "x=2A")]
    [InlineData("9 LFORMAT=n=%n", "n=9")]
    [InlineData("9 LFORMAT=o=%o", "o=1")]
    [InlineData("9 LFORMAT=p=%o2", "p=11")]
    public void Workbench_profile_uses_the_captured_arithmetic_and_hex_lformat_subset(
        string sourceText, string expected)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(16, sourceText);
        CommandInvocation invocation = CommandInvocation.ForOutput(source,
            (uint)sourceText.Length, new BPTR(1));

        var result = EvalCommand.Execute(ref platform, in invocation,
            EvalCommandProfile.Workbench31, new APTR(128), 36, new APTR(256), 24,
            new APTR(320), 256, new APTR(640), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(expected, platform.Store.OutputText);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Morphos_profile_reconstructs_operator_and_multiple_value_slots()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string sourceText = "1 + 2 * 3";
        APTR source = platform.Store.PutAt(16, sourceText);
        CommandInvocation invocation = CommandInvocation.ForOutput(source,
            (uint)sourceText.Length, new BPTR(1));

        var result = EvalCommand.Execute(ref platform, in invocation,
            EvalCommandProfile.MorphOS320, new APTR(128), 42, new APTR(256), 24,
            new APTR(320), 256, new APTR(640), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("7\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Theory]
    [InlineData("42 HEX", "0x2a\n")]
    [InlineData("42 LFORMAT=x=%x", "x=2a")]
    public void Morphos_profile_uses_decoded_formatting_slots(string sourceText,
        string expected)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(16, sourceText);
        CommandInvocation invocation = CommandInvocation.ForOutput(source,
            (uint)sourceText.Length, new BPTR(1));

        var result = EvalCommand.Execute(ref platform, in invocation,
            EvalCommandProfile.MorphOS320, new APTR(128), 64, new APTR(256), 24,
            new APTR(320), 256, new APTR(640), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(expected, platform.Store.OutputText);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Morphos_profile_opens_and_closes_the_to_output()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string sourceText = "42 TO=RAM:result";
        APTR source = platform.Store.PutAt(16, sourceText);
        CommandInvocation invocation = CommandInvocation.ForOutput(source,
            (uint)sourceText.Length, new BPTR(1));

        var result = EvalCommand.Execute(ref platform, in invocation,
            EvalCommandProfile.MorphOS320, new APTR(128), 64, new APTR(256), 24,
            new APTR(320), 256, new APTR(640), 64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("RAM:result", platform.Store.OpenedPath);
        Assert.Equal((uint)2, platform.Store.ClosedHandle.Raw);
        Assert.Equal("42\n", platform.Store.OutputText);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }
}
