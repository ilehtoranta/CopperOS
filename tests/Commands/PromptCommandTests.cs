using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class PromptCommandTests
{
    [Fact]
    public void Prompt_without_argument_requests_the_default_prompt()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        int result = PromptCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)1, platform.Store.PromptReset);
        Assert.Equal(string.Empty, platform.Store.PromptValue);
        Assert.Equal(1, platform.Store.PromptCount);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Prompt_decodes_the_remaining_argument_and_preserves_spaces()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "Custom *n prompt";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = PromptCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)0, platform.Store.PromptReset);
        Assert.Equal("Custom \n prompt", platform.Store.PromptValue);
        Assert.Equal(1, platform.Store.PromptCount);
    }

    [Fact]
    public void Prompt_accepts_a_quoted_empty_template_without_resetting()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        string commandLine = "\"\"";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = PromptCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal((uint)0, platform.Store.PromptReset);
        Assert.Equal(string.Empty, platform.Store.PromptValue);
        Assert.Equal(1, platform.Store.PromptCount);
    }

    [Fact]
    public void Prompt_rejects_malformed_quoted_text_without_changing_state()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.PromptValue = "existing";
        string commandLine = "\"unterminated";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = PromptCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal("existing", platform.Store.PromptValue);
        Assert.Equal(0, platform.Store.PromptCount);
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
