using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellScriptFrameTests
{
    [Fact]
    public void Frame_codec_round_trips_state_and_control_transitions()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Parent = new APTR(100),
            Cli = new APTR(120),
            Input = new BPTR(2),
            Output = new BPTR(3),
            Error = new BPTR(4),
            CurrentDirectory = new BPTR(5),
            Flags = ShellScriptFrameFlags.Active,
            FailureLimit = 10,
            LastResult = 0,
        };

        Assert.True(ShellScriptFrameCodec.Initialize(
            ref platform, frame, in initial));
        Assert.True(ShellScriptFrameCodec.TryRecordResult(
            ref platform, frame, -5));
        Assert.True(ShellScriptFrameCodec.TryRecordCondition(
            ref platform, frame, 2, 1));
        Assert.True(ShellScriptFrameCodec.TrySetCondition(
            ref platform, frame, 3));
        Assert.True(ShellScriptFrameCodec.TrySetFailureLimit(
            ref platform, frame, 20));
        Assert.True(ShellScriptFrameCodec.TryAdvance(
            ref platform, frame, 7, 128));
        Assert.True(ShellScriptFrameCodec.TrySetControlTop(
            ref platform, frame, new APTR(3100)));
        Assert.True(ShellScriptFrameCodec.TryIncrementLabelCount(
            ref platform, frame));
        Assert.True(ShellScriptFrameCodec.TryRecordControl(
            ref platform, frame, ShellControlAction.Quit, 17));

        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var state));
        Assert.Equal(new APTR(100), state.Parent);
        Assert.Equal(new APTR(120), state.Cli);
        Assert.Equal((uint)7, state.CurrentLine);
        Assert.Equal((uint)128, state.CurrentOffset);
        Assert.Equal((uint)20, state.FailureLimit);
        Assert.Equal(-5, state.LastResult);
        Assert.Equal(17, state.QuitResult);
        Assert.Equal((uint)3, state.Condition);
        Assert.Equal(ShellScriptFrameControl.Quit, state.Control);
        Assert.True((state.Flags & ShellScriptFrameFlags.ConditionFalse) != 0);
        Assert.True((state.Flags & ShellScriptFrameFlags.Skipping) != 0);
        Assert.True((state.Flags & ShellScriptFrameFlags.QuitRequested) != 0);
        Assert.True((state.Flags & ShellScriptFrameFlags.FailureLimitSet) != 0);
        Assert.Equal(new APTR(3100), state.ControlTop);
        Assert.Equal((uint)1, state.LabelCount);
    }

    [Fact]
    public void Frame_codec_rejects_invalid_transition_inputs_and_headers()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR frame = new(3000);
        ShellScriptFrameState initial = new();
        Assert.True(ShellScriptFrameCodec.Initialize(
            ref platform, frame, in initial));

        Assert.False(ShellScriptFrameCodec.TryRecordCondition(
            ref platform, frame, 1, 2));
        Assert.False(ShellScriptFrameCodec.TrySetFailureLimit(
            ref platform, frame, 0));
        Assert.False(ShellScriptFrameCodec.TryRecordControl(
            ref platform, frame, (ShellControlAction)0, 0));

        platform.WriteUInt32(frame, 4, ShellScriptFrameCodec.Version + 1);
        Assert.False(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out _));
        Assert.False(ShellScriptFrameCodec.Initialize(
            ref platform, APTR.Null, in initial));
    }

    [Fact]
    public void Nested_control_record_tracks_else_target_and_parent()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3100);
        ShellScriptBlockState initial = new()
        {
            Parent = new APTR(3000),
            Kind = ShellScriptBlockKind.If,
            Flags = ShellScriptBlockFlags.ConditionFalse |
                ShellScriptBlockFlags.Skipping,
            StartLine = 4,
            StartOffset = 32,
        };

        Assert.True(ShellScriptControlCodec.Initialize(
            ref platform, record, in initial));
        Assert.True(ShellScriptControlCodec.TrySetTarget(
            ref platform, record, 9, 96));
        Assert.True(ShellScriptControlCodec.TryToggleElse(
            ref platform, record));
        Assert.True(ShellScriptControlCodec.TryRead(
            ref platform, record, out var state));

        Assert.Equal(new APTR(3000), state.Parent);
        Assert.Equal(ShellScriptBlockKind.If, state.Kind);
        Assert.Equal((uint)4, state.StartLine);
        Assert.Equal((uint)32, state.StartOffset);
        Assert.Equal((uint)9, state.TargetLine);
        Assert.Equal((uint)96, state.TargetOffset);
        Assert.True((state.Flags & ShellScriptBlockFlags.ElseSeen) != 0);
        Assert.False((state.Flags & ShellScriptBlockFlags.ConditionFalse) != 0);
        Assert.False((state.Flags & ShellScriptBlockFlags.Skipping) != 0);

        Assert.True(ShellScriptControlCodec.TryPop(
            ref platform, record, out var parent));
        Assert.Equal(new APTR(3000), parent);
        Assert.False(ShellScriptControlCodec.TryToggleElse(
            ref platform, record));
    }

    [Fact]
    public void Nested_control_record_rejects_invalid_kind_and_header()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3100);
        ShellScriptBlockState invalid = new()
        {
            Kind = (ShellScriptBlockKind)99,
        };

        Assert.False(ShellScriptControlCodec.Initialize(
            ref platform, record, in invalid));
        invalid.Kind = ShellScriptBlockKind.Skip;
        Assert.True(ShellScriptControlCodec.Initialize(
            ref platform, record, in invalid));
        Assert.False(ShellScriptControlCodec.TrySetSkipping(
            ref platform, record, 2));

        platform.WriteUInt32(record, 0, 0);
        Assert.False(ShellScriptControlCodec.TryRead(
            ref platform, record, out _));
    }

    [Fact]
    public void Control_transitions_open_else_and_close_nested_blocks()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(120),
            Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(
            ref platform, frame, in initial));

        APTR ifRecord = new(3100);
        APTR skipRecord = new(3150);
        Assert.True(ShellScriptControlTransitions.TryOpen(
            ref platform, frame, ifRecord, ShellScriptBlockKind.If,
            3, 20, 1));
        Assert.True(ShellScriptControlTransitions.TryOpen(
            ref platform, frame, skipRecord, ShellScriptBlockKind.Skip,
            4, 30, 0));

        Assert.True(ShellScriptControlTransitions.TryClose(
            ref platform, frame, ShellScriptBlockKind.Skip,
            out var closedSkip));
        Assert.Equal(skipRecord, closedSkip);
        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var nested));
        Assert.Equal(ifRecord, nested.ControlTop);
        Assert.True((nested.Flags & ShellScriptFrameFlags.ConditionFalse) != 0);
        Assert.True((nested.Flags & ShellScriptFrameFlags.Skipping) != 0);

        Assert.True(ShellScriptControlTransitions.TryElse(
            ref platform, frame));
        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var afterElse));
        Assert.False((afterElse.Flags & ShellScriptFrameFlags.ConditionFalse) != 0);
        Assert.False((afterElse.Flags & ShellScriptFrameFlags.Skipping) != 0);
        Assert.True((afterElse.Flags & ShellScriptFrameFlags.ElseSeen) != 0);

        Assert.True(ShellScriptControlTransitions.TryClose(
            ref platform, frame, ShellScriptBlockKind.If,
            out var closedIf));
        Assert.Equal(ifRecord, closedIf);
        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var closed));
        Assert.Equal(APTR.Null, closed.ControlTop);
        Assert.False((closed.Flags & ShellScriptFrameFlags.Skipping) != 0);
    }

    [Fact]
    public void Control_transitions_reject_wrong_terminators_and_duplicate_else()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(120),
            Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(
            ref platform, frame, in initial));
        APTR record = new(3100);
        Assert.True(ShellScriptControlTransitions.TryOpen(
            ref platform, frame, record, ShellScriptBlockKind.If,
            1, 0, 0));
        Assert.False(ShellScriptControlTransitions.TryClose(
            ref platform, frame, ShellScriptBlockKind.Skip,
            out _));
        Assert.True(ShellScriptControlTransitions.TryElse(
            ref platform, frame));
        Assert.False(ShellScriptControlTransitions.TryElse(
            ref platform, frame));
    }

    [Fact]
    public void Opening_against_a_malformed_parent_is_failure_atomic()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(120),
            Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(
            ref platform, frame, in initial));

        APTR malformedParent = new(3200);
        Assert.True(ShellScriptFrameCodec.TrySetControlTop(
            ref platform, frame, malformedParent));
        Assert.False(ShellScriptControlTransitions.TryOpen(
            ref platform, frame, new APTR(3100),
            ShellScriptBlockKind.If, 1, 0, 0));
        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var after));
        Assert.Equal(malformedParent, after.ControlTop);
    }
}
