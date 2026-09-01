using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellScriptDirectiveTests
{
    [Theory]
    [InlineData(".KEY name/A", ShellScriptDirectiveKind.Key, 5u)]
    [InlineData(".k name", ShellScriptDirectiveKind.Key, 3u)]
    [InlineData(".DEFAULT name value", ShellScriptDirectiveKind.Default, 9u)]
    [InlineData(".DEF name value", ShellScriptDirectiveKind.Default, 5u)]
    [InlineData(".BRA {", ShellScriptDirectiveKind.Bra, 5u)]
    [InlineData(".KET }", ShellScriptDirectiveKind.Ket, 5u)]
    [InlineData(".DOLLAR #", ShellScriptDirectiveKind.Dollar, 8u)]
    [InlineData(".DOL #", ShellScriptDirectiveKind.Dollar, 5u)]
    [InlineData(".DOT !", ShellScriptDirectiveKind.Dot, 5u)]
    public void Parses_documented_directives(string line,
        ShellScriptDirectiveKind expected, uint argumentOffset)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(32, line);

        Assert.True(ShellScriptDirective.TryParse(ref platform, source,
            (uint)line.Length, out var actual, out var offset));

        Assert.Equal(expected, actual);
        Assert.Equal(argumentOffset, offset);
    }

    [Theory]
    [InlineData(". comment")]
    [InlineData(".\\")]
    public void Parses_documented_comment_lines(string line)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(32, line);

        Assert.True(ShellScriptDirective.TryParse(ref platform, source,
            (uint)line.Length, out var actual, out _));

        Assert.Equal(ShellScriptDirectiveKind.Comment, actual);
    }

    [Theory]
    [InlineData(".")]
    [InlineData(".KEYING value")]
    [InlineData(".UNKNOWN value")]
    public void Rejects_unknown_or_ambiguous_dot_lines(string line)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(32, line);

        Assert.True(ShellScriptDirective.TryParse(ref platform, source,
            (uint)line.Length, out var actual, out _));

        Assert.Equal(ShellScriptDirectiveKind.Invalid, actual);
    }

    [Fact]
    public void Engine_skips_documented_comment_lines_without_external_lookup()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = ". comment\n.\\\n; another comment\nEcho done\n";
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8), Input = new BPTR(1), Output = new BPTR(1),
            Error = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame,
            in initial));
        ShellCommandWorkspace command = new(new APTR(400), 96,
            new APTR(520), 96, new APTR(640), 96, new APTR(760), 96,
            new APTR(880), 96, new APTR(1000), 96);
        ShellScriptStepWorkspace workspace = new(new APTR(1100), 256,
            new APTR(1400), 64, in command);

        ShellScriptRunResult result = ShellScriptEngine.Run(ref platform, frame,
            in workspace, 8);

        Assert.Equal(ShellScriptStepStatus.EndOfFile, result.Status);
        Assert.Equal("done\n", platform.Store.OutputText);
        Assert.Equal(0, platform.Store.ScriptLookupCount);
    }
}
