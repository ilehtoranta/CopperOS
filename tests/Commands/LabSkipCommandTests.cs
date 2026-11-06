using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class LabSkipCommandTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void Omitted_skip_target_matches_only_an_unnamed_Lab(bool back, bool unnamed)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Skip\nLab named\n" + (unnamed ? "Lab\nEcho reached\n" : "Echo wrong\n");
        var frame = new APTR(512);
        var initial = new ShellScriptFrameState
        {
            Cli = new APTR(256), Input = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame, in initial));
        int result;
        Assert.True(back
            ? ShellScriptLabelTransitions.TryScanFromBeginning(ref platform, frame,
                APTR.Null, 0, new APTR(1024), 512, new APTR(2048), 256, out result)
            : ShellScriptLabelTransitions.TryScanForward(ref platform, frame,
                APTR.Null, 0, new APTR(1024), 512, new APTR(2048), 256, out result));
        Assert.Equal(unnamed ? 0 : 10, result);
        Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame, out var after));
        Assert.Equal(unnamed ? (uint)platform.Store.ScriptText.IndexOf("Echo reached", StringComparison.Ordinal)
            : (uint)platform.Store.ScriptText.Length, after.CurrentOffset);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EndSkip_leaves_unrelated_If_records_owned_by_the_frame(int kind)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        var frame = new APTR(512);
        var block = new APTR(2560);
        var initial = new ShellScriptFrameState
        {
            Cli = new APTR(256), Input = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active,
        };
        if (kind != 0)
        {
            var control = new ShellScriptBlockState { Kind = (ShellScriptBlockKind)kind };
            Assert.True(ShellScriptControlCodec.Initialize(ref platform, block, in control));
            initial.ControlTop = block;
        }
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame, in initial));
        Assert.True(ShellScriptControlTransitions.TryEndSkip(ref platform, frame, out var closed));
        Assert.Equal(kind == 2 ? block : APTR.Null, closed);
        Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame, out var after));
        Assert.Equal(kind == 1 ? block : APTR.Null, after.ControlTop);
        if (kind != 0) Assert.True(ShellScriptControlCodec.TryRead(ref platform, block, out _));
    }

    [Fact]
    public void Unnamed_Lab_succeeds_without_allocating_a_label_record()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        var invocation = CreateInvocation(APTR.Null, 0);
        Assert.Equal(0, LabCommand.Execute(ref platform, in invocation,
            new APTR(80), 32, new APTR(128), 64));
        Assert.Equal(0, platform.Store.LabelDefineCount);
        Assert.Equal(0, platform.Store.ReadArgsCount);
        Assert.Equal(0, platform.Store.FreeArgsCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("later")]
    public void Forward_skip_stops_at_EndSkip_before_a_later_label(string labelName)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Skip later\nEcho wrong\nEndSkip\nEcho reached\nLab later\n";
        var frame = new APTR(512);
        var initial = new ShellScriptFrameState
        {
            Cli = new APTR(256), Input = new BPTR(1), CurrentLine = 1,
            CurrentOffset = 0, Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame, in initial));
        var label = labelName.Length == 0 ? APTR.Null : platform.Store.PutAt(4000, labelName);
        Assert.True(ShellScriptLabelTransitions.TryScanForward(ref platform, frame,
            label, (uint)labelName.Length, new APTR(1024), 512, new APTR(2048), 256,
            out var result));
        Assert.Equal(5, result);
        Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame, out var after));
        Assert.Equal(4u, after.CurrentLine);
        Assert.Equal((uint)platform.Store.ScriptText.IndexOf("Echo reached", StringComparison.Ordinal),
            after.CurrentOffset);
    }

    [Fact]
    public void Skip_propagates_the_warning_from_an_EndSkip_boundary()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.SkipResult = 5;
        var invocation = CreateInvocation(APTR.Null, 0);
        Assert.Equal(5, SkipCommand.Execute(ref platform, in invocation,
            new APTR(80), 32, new APTR(128), 64));
    }

    [Theory]
    [InlineData(1u)]
    [InlineData(uint.MaxValue)]
    public void Skip_normalizes_the_ReadArgs_BACK_switch(uint trueValue)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.SkipSwitchValue = trueValue;
        var text = "later BACK";
        var source = platform.Store.PutAt(16, text);
        var invocation = CreateInvocation(source, text.Length);
        Assert.Equal(0, SkipCommand.Execute(ref platform, in invocation,
            new APTR(80), 32, new APTR(128), 64));
        Assert.Equal(1u, platform.Store.SkipBack);
    }

    [Fact]
    public void Missing_forward_label_consumes_the_script_and_reports_ERROR()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Skip missing\nEcho wrong\n";
        var frame = new APTR(512);
        var initial = new ShellScriptFrameState
        {
            Cli = new APTR(256), Input = new BPTR(1), CurrentLine = 1,
            Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame, in initial));
        var label = platform.Store.PutAt(4000, "missing");
        Assert.True(ShellScriptLabelTransitions.TryScanForward(ref platform, frame,
            label, 7, new APTR(1024), 512, new APTR(2048), 256, out var result));
        Assert.Equal(10, result);
        Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame, out var after));
        Assert.Equal((uint)platform.Store.ScriptText.Length, after.CurrentOffset);
    }

    [Theory]
    [InlineData("first", 2u)]
    [InlineData("middle", 4u)]
    [InlineData("after", 7u)]
    public void Beginning_search_includes_the_first_line_and_the_whole_source(string name, uint expectedLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "Lab first\nEcho one\nLab middle\nEcho two\nSkip BACK\nLab after\n";
        var frame = new APTR(512);
        var initial = new ShellScriptFrameState
        {
            Cli = new APTR(256), Input = new BPTR(1), CurrentLine = 5,
            CurrentOffset = (uint)platform.Store.ScriptText.IndexOf("Skip BACK", StringComparison.Ordinal),
            Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame, in initial));
        var label = name.Length == 0 ? APTR.Null : platform.Store.PutAt(4000, name);
        Assert.True(ShellScriptLabelTransitions.TryScanFromBeginning(ref platform, frame,
            label, (uint)name.Length, new APTR(1024), 512, new APTR(2048), 256, out var result));
        Assert.Equal(0, result);
        Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame, out var after));
        Assert.Equal(expectedLine, after.CurrentLine);
    }

    [Theory]
    [InlineData("mark", true)]
    [InlineData("", false)]
    [InlineData("missing", false)]
    public void Forward_search_finds_unseen_labels_without_executing_skipped_lines(string name, bool found)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.ScriptText = "SKIP mark\nECHO skipped\nLaB \"MARK\"\nECHO reached\n";
        var frame = new APTR(512);
        var initial = new ShellScriptFrameState
        {
            Cli = new APTR(256), Input = new BPTR(1), CurrentLine = 1,
            CurrentOffset = 0, Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(ref platform, frame, in initial));
        var label = name.Length == 0 ? APTR.Null : platform.Store.PutAt(4000, name);
        Assert.True(ShellScriptLabelTransitions.TryScanForward(ref platform, frame,
            label, (uint)name.Length, new APTR(1024), 512, new APTR(2048), 256,
            out var status));
        Assert.Equal(found ? 0 : 10, status);
        Assert.True(ShellScriptFrameCodec.TryRead(ref platform, frame, out var result));
        Assert.Equal(found ? 4u : 5u, result.CurrentLine);
        Assert.Equal(found ? (uint)platform.Store.ScriptText.IndexOf("ECHO reached", StringComparison.Ordinal)
            : (uint)platform.Store.ScriptText.Length, result.CurrentOffset);
    }

    [Theory]
    [InlineData("build")]
    [InlineData("build extra")]
    [InlineData("\"two words\"")]
    public void Lab_accepts_marker_text_without_parsing_or_registering_it(string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = LabCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(0, platform.Store.LabelDefineCount);
        Assert.Equal(0, platform.Store.ReadArgsCount);
        Assert.Equal(0, platform.Store.FreeArgsCount);
    }

    [Fact]
    public void Skip_supports_next_label_named_label_and_BACK()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        CommandInvocation invocation = CreateInvocation(APTR.Null, 0);

        int result = SkipCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(string.Empty, platform.Store.LastSkipLabel);
        Assert.Equal((uint)0, platform.Store.SkipBack);
        Assert.Equal(1, platform.Store.ReadArgsCount);
        Assert.Equal(1, platform.Store.FreeArgsCount);

        string commandLine = "build BACK";
        APTR source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);
        result = SkipCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal("build", platform.Store.LastSkipLabel);
        Assert.Equal((uint)1, platform.Store.SkipBack);
        Assert.Equal(2, platform.Store.ReadArgsCount);
        Assert.Equal(2, platform.Store.FreeArgsCount);

        platform = new EchoCommandTests.TestShellPlatform();
        commandLine = "BACK";
        source = platform.Store.PutAt(16, commandLine);
        invocation = CreateInvocation(source, commandLine.Length);
        result = SkipCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(string.Empty, platform.Store.LastSkipLabel);
        Assert.Equal((uint)1, platform.Store.SkipBack);
    }

    [Theory]
    [InlineData("build WRONG")]
    [InlineData("BACK extra")]
    [InlineData("\"unterminated")]
    public void Skip_rejects_unknown_or_malformed_options_without_requesting_a_jump(
        string commandLine)
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = SkipCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);

        Assert.Equal((int)ShellCommandResult.Error, result);
        Assert.Equal(0, platform.Store.SkipCount);
    }

    [Fact]
    public void Lab_needs_no_label_owner_while_Skip_reports_scan_failure()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        platform.Store.LabelDefineFailure = true;
        string commandLine = "label";
        APTR source = platform.Store.PutAt(16, commandLine);
        CommandInvocation invocation = CreateInvocation(source, commandLine.Length);

        int result = LabCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);
        Assert.Equal((int)ShellCommandResult.Ok, result);
        Assert.Equal(0, platform.Store.LabelDefineCount);

        platform = new EchoCommandTests.TestShellPlatform();
        platform.Store.SkipFailure = true;
        source = platform.Store.PutAt(16, "label");
        invocation = CreateInvocation(source, 5);
        result = SkipCommand.Execute(
            ref platform,
            in invocation,
            new APTR(80),
            32,
            new APTR(128),
            64);
        Assert.Equal((int)ShellCommandResult.Fail, result);
    }

    private static CommandInvocation CreateInvocation(APTR source, int length) => new(
        source,
        (uint)length,
        APTR.Null,
        APTR.Null,
        BPTR.Null,
        BPTR.Null,
        BPTR.Null,
        BPTR.Null,
        new APTR(8),
        0,
        0);
}
