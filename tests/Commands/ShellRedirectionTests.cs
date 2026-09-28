using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellRedirectionTests
{
    [Fact]
    public void Parser_extracts_bounded_targets_and_removes_operators()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string sourceText = "Echo hello >out *>>err <input";
        APTR source = platform.Store.PutAt(16, sourceText);
        ShellRedirectionWorkspace workspace = new(
            new APTR(100), 128,
            new APTR(260), 64,
            new APTR(340), 64,
            new APTR(420), 64);

        Assert.True(ShellRedirectionParser.Parse(
            ref platform,
            source,
            (uint)sourceText.Length,
            in workspace,
            out var spec,
            out var commandLength));

        Assert.Equal("Echo hello   ", platform.Store.ReadText(
            workspace.Command, commandLength));
        Assert.Equal("input", platform.Store.ReadText(
            spec.InputPath, spec.InputLength));
        Assert.Equal("out", platform.Store.ReadText(
            spec.OutputPath, spec.OutputLength));
        Assert.Equal("err", platform.Store.ReadText(
            spec.ErrorPath, spec.ErrorLength));
        Assert.Equal((uint)0, spec.OutputAppend);
        Assert.Equal((uint)1, spec.ErrorAppend);
    }

    [Fact]
    public void Parser_preserves_quoted_operators_and_rejects_duplicate_streams()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        const string quotedText = "Echo \"a>b\"";
        APTR source = platform.Store.PutAt(16, quotedText);
        ShellRedirectionWorkspace workspace = CreateWorkspace();

        Assert.True(ShellRedirectionParser.Parse(
            ref platform, source, (uint)quotedText.Length,
            in workspace, out var spec, out var commandLength));
        Assert.True(spec.IsEmpty);
        Assert.Equal(quotedText, platform.Store.ReadText(
            workspace.Command, commandLength));

        const string errorRedirectText = "Echo hello *>err";
        source = platform.Store.PutAt(16, errorRedirectText);
        Assert.True(ShellRedirectionParser.Parse(ref platform, source,
            (uint)errorRedirectText.Length, in workspace, out var errorSpec,
            out commandLength));
        Assert.Equal("err", platform.Store.ReadText(errorSpec.ErrorPath,
            errorSpec.ErrorLength));
        Assert.Equal((uint)0, errorSpec.ErrorAppend);

        const string mergeErrorText = "Echo hello >out *<>";
        source = platform.Store.PutAt(16, mergeErrorText);
        Assert.True(ShellRedirectionParser.Parse(ref platform, source,
            (uint)mergeErrorText.Length, in workspace, out var mergeSpec,
            out commandLength));
        Assert.Equal("out", platform.Store.ReadText(mergeSpec.OutputPath,
            mergeSpec.OutputLength));
        Assert.True(mergeSpec.ErrorPath.IsNull);
        Assert.Equal(1u, mergeSpec.ErrorToOutput);
        Assert.True(mergeSpec.HasError);

        const string duplicateErrorRoute = "Echo hello *<> *>err";
        source = platform.Store.PutAt(16, duplicateErrorRoute);
        Assert.False(ShellRedirectionParser.Parse(ref platform, source,
            (uint)duplicateErrorRoute.Length, in workspace, out _, out _));

        const string oldUnixErrorRedirect = "Echo hello 2>err";
        source = platform.Store.PutAt(16, oldUnixErrorRedirect);
        Assert.True(ShellRedirectionParser.Parse(ref platform, source,
            (uint)oldUnixErrorRedirect.Length, in workspace, out var literalSpec,
            out commandLength));
        Assert.True(literalSpec.IsEmpty);
        Assert.Equal(oldUnixErrorRedirect, platform.Store.ReadText(
            workspace.Command, commandLength));

        const string adjacentOperatorText = "Echo hello>err";
        source = platform.Store.PutAt(16, adjacentOperatorText);
        Assert.True(ShellRedirectionParser.Parse(ref platform, source,
            (uint)adjacentOperatorText.Length, in workspace, out var adjacentSpec,
            out commandLength));
        Assert.True(adjacentSpec.IsEmpty);
        Assert.Equal(adjacentOperatorText, platform.Store.ReadText(
            workspace.Command, commandLength));

        const string escapedQuoteText = "Echo \"a*\"b\"";
        source = platform.Store.PutAt(16, escapedQuoteText);
        Assert.True(ShellRedirectionParser.Parse(
            ref platform, source, (uint)escapedQuoteText.Length,
            in workspace, out _, out commandLength));
        Assert.Equal(escapedQuoteText, platform.Store.ReadText(
            workspace.Command, commandLength));

        const string duplicateText = "Echo >one >two";
        source = platform.Store.PutAt(16, duplicateText);
        Assert.False(ShellRedirectionParser.Parse(
            ref platform, source, (uint)duplicateText.Length,
            in workspace, out _, out _));

        ShellRedirectionWorkspace overlapping = new(
            new APTR(16), 128,
            new APTR(260), 64,
            new APTR(340), 64,
            new APTR(420), 64);
        Assert.False(ShellRedirectionParser.Parse(
            ref platform, source, (uint)duplicateText.Length,
            in overlapping, out _, out _));
    }

    [Fact]
    public void Transaction_rolls_back_already_opened_streams_on_failure()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.RedirectionOutputFailure = true;
        APTR inputPath = platform.Store.PutAt(32, "in");
        APTR outputPath = platform.Store.PutAt(48, "out");
        ShellRedirectionSpec spec = new(
            inputPath, 2,
            outputPath, 3, 0,
            APTR.Null, 0, 0);
        ShellScriptFrameState frame = new()
        {
            Cli = new APTR(8),
            Input = new BPTR(1),
            Output = new BPTR(1),
            Error = new BPTR(1),
        };

        Assert.False(ShellRedirectionTransaction.TryOpen(
            ref platform, in frame, in spec, out var handles));
        Assert.Equal(1, platform.Store.RedirectionOpenCount);
        Assert.Equal(1, platform.Store.RedirectionCloseCount);
        Assert.Equal((uint)0, handles.Owned);
    }

    [Fact]
    public void Error_to_output_shares_the_output_handle_without_double_ownership()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR outputPath = platform.Store.PutAt(48, "out");
        ShellRedirectionSpec spec = new(
            APTR.Null, 0,
            outputPath, 3, 0,
            APTR.Null, 0, 0)
        {
            ErrorToOutput = 1,
        };
        ShellScriptFrameState frame = new()
        {
            Cli = new APTR(8),
            Input = new BPTR(1),
            Output = new BPTR(2),
            Error = new BPTR(3),
        };

        Assert.True(ShellRedirectionTransaction.TryOpen(
            ref platform, in frame, in spec, out var handles));
        Assert.Equal(handles.Output.Raw, handles.Error.Raw);
        Assert.Equal(2u, handles.Owned);
        Assert.True(ShellRedirectionTransaction.Close(
            ref platform, in frame, ref handles));
        Assert.Equal(1, platform.Store.RedirectionCloseCount);
    }

    [Fact]
    public void Step_applies_output_redirection_only_for_the_current_command()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Echo hello >out\n";
        APTR frame = InitializeFrame(ref platform);
        ShellCommandWorkspace command = new(
            new APTR(400), 96,
            new APTR(520), 96,
            new APTR(640), 96,
            new APTR(760), 96,
            new APTR(880), 96,
            new APTR(1000), 96);
        ShellRedirectionWorkspace redirection = new(
            new APTR(1600), 256,
            new APTR(1900), 64,
            new APTR(2000), 64,
            new APTR(2100), 64);
        ShellScriptStepWorkspace workspace = new(
            new APTR(1100), 256,
            new APTR(1400), 64,
            in command,
            in redirection);

        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace,
                out var result));
        Assert.Equal(ShellInternalCommand.Echo, result.Command);
        Assert.Equal("out", platform.Store.RedirectionOutputPath);
        Assert.Equal(1, platform.Store.RedirectionOpenCount);
        Assert.Equal(1, platform.Store.RedirectionCloseCount);
        Assert.Equal("hello\n", platform.Store.OutputText);
    }

    [Fact]
    public void Step_passes_inherited_and_temporary_streams_to_external_commands()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "external >out\n";
        APTR frame = InitializeFrame(ref platform);
        ShellCommandWorkspace command = new(
            new APTR(400), 96,
            new APTR(520), 96,
            new APTR(640), 96,
            new APTR(760), 96,
            new APTR(880), 96,
            new APTR(1000), 96);
        ShellRedirectionWorkspace redirection = new(
            new APTR(1600), 256,
            new APTR(1900), 64,
            new APTR(2000), 64,
            new APTR(2100), 64);
        ShellScriptStepWorkspace workspace = new(
            new APTR(1100), 256,
            new APTR(1400), 64,
            in command,
            in redirection);

        Assert.Equal(ShellScriptStepStatus.Executed,
            ShellScriptEngine.Step(ref platform, frame, in workspace,
                out var result));
        Assert.Equal(ShellInternalCommand.Unknown, result.Command);
        Assert.Equal("external ", platform.Store.LastScriptExternalCommand);
        Assert.Equal((uint)1, platform.Store.LastScriptInput.Raw);
        Assert.Equal((uint)21, platform.Store.LastScriptOutput.Raw);
        Assert.Equal((uint)1, platform.Store.LastScriptError.Raw);
        Assert.Equal(1, platform.Store.RedirectionCloseCount);
    }

    private static ShellRedirectionWorkspace CreateWorkspace() =>
        new(
            new APTR(100), 128,
            new APTR(260), 64,
            new APTR(340), 64,
            new APTR(420), 64);

    private static APTR InitializeFrame(
        ref EchoCommandTests.TestShellPlatform platform)
    {
        APTR frame = new(3000);
        ShellScriptFrameState state = new()
        {
            Cli = new APTR(8),
            Input = new BPTR(1),
            Output = new BPTR(1),
            Error = new BPTR(1),
            CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(
            ref platform, frame, in state));
        return frame;
    }
}
