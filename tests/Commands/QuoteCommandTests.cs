using Amiga;
using CopperOS.Commands;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class QuoteCommandTests
{
    [Fact]
    public void Str_source_uses_dos_template_and_writes_the_forward_result()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string sourceText = "READITEM A=B";
        var source = platform.Store.PutAt(16, sourceText);
        var invocation = CommandInvocation.ForOutput(source, (uint)sourceText.Length,
            new BPTR(1));

        var result = QuoteCommand.Execute(ref platform, in invocation,
            new APTR(128), 74, new APTR(256), 32, new APTR(320), 128,
            new APTR(512), 16, new APTR(640), 128, new APTR(800), 128,
            new APTR(960), 128);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("\"A=B\"\n", platform.Store.OutputText);
        Assert.Equal("RULE/A,FILE/K,VAR/K,STR,NOLINE/S,NOQUOTES/S,FIRSTLINE/S,REVERSE=UNQUOTE/S",
            platform.Store.LastReadArgsTemplate);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Noline_suppresses_only_the_documented_line_advance()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string sourceText = "HEX abc NOLINE";
        var source = platform.Store.PutAt(16, sourceText);
        var invocation = CommandInvocation.ForOutput(source, (uint)sourceText.Length,
            new BPTR(1));

        var result = QuoteCommand.Execute(ref platform, in invocation,
            new APTR(128), 74, new APTR(256), 32, new APTR(320), 128,
            new APTR(512), 16, new APTR(640), 128, new APTR(800), 128,
            new APTR(960), 128);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("616263", platform.Store.OutputText);
    }

    [Fact]
    public void Var_source_uses_the_cli_local_value_before_the_global_fallback()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.LocalVariableName = "value";
        platform.Store.LocalVariableValue = "A";
        platform.Store.GlobalVariableName = "value";
        platform.Store.GlobalVariableValue = "B";
        const string sourceText = "HEX VAR=value";
        var source = platform.Store.PutAt(16, sourceText);
        var invocation = new CommandInvocation(source, (uint)sourceText.Length,
            APTR.Null, APTR.Null, BPTR.Null, new BPTR(1), BPTR.Null,
            BPTR.Null, new APTR(4), 0, 0);

        var result = QuoteCommand.Execute(ref platform, in invocation,
            new APTR(128), 74, new APTR(256), 32, new APTR(320), 128,
            new APTR(512), 16, new APTR(640), 128, new APTR(800), 128,
            new APTR(960), 128);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("41\n", platform.Store.OutputText);
    }
}
