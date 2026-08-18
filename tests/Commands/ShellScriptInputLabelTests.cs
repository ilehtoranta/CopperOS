using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellScriptInputLabelTests
{
    [Fact]
    public void Input_codec_records_one_bounded_line_and_cursor()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR record = new(3000);
        ShellScriptInputState initial = new()
        {
            Handle = new BPTR(1),
            Buffer = new APTR(100),
            Capacity = 128,
        };

        Assert.True(ShellScriptInputCodec.Initialize(
            ref platform, record, in initial));
        Assert.True(ShellScriptInputCodec.TryRecordLine(
            ref platform, record, 5, 32, 4, 0));
        Assert.True(ShellScriptInputCodec.TrySetCursor(
            ref platform, record, 4));
        Assert.True(ShellScriptInputCodec.TrySetError(
            ref platform, record, 7));
        Assert.True(ShellScriptInputCodec.TryRead(
            ref platform, record, out var state));

        Assert.Equal(new BPTR(1), state.Handle);
        Assert.Equal(new APTR(100), state.Buffer);
        Assert.Equal((uint)128, state.Capacity);
        Assert.Equal((uint)4, state.Length);
        Assert.Equal((uint)4, state.Cursor);
        Assert.Equal((uint)5, state.Line);
        Assert.Equal((uint)32, state.Offset);
        Assert.Equal((uint)7, state.Error);
        Assert.False(ShellScriptInputCodec.TryRecordLine(
            ref platform, record, 6, 40, 128, 0));
    }

    [Fact]
    public void Label_index_rejects_duplicates_and_finds_nearest_targets()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8),
            CurrentLine = 5,
            CurrentOffset = 50,
            Flags = ShellScriptFrameFlags.Active,
        };
        Assert.True(ShellScriptFrameCodec.Initialize(
            ref platform, frame, in initial));

        APTR startName = platform.Store.PutAt(100, "start");
        APTR middleName = platform.Store.PutAt(120, "middle");
        APTR endName = platform.Store.PutAt(140, "end");
        APTR duplicateName = platform.Store.PutAt(160, "MIDDLE");
        APTR startRecord = new(3100);
        APTR middleRecord = new(3140);
        APTR endRecord = new(3180);
        APTR duplicateRecord = new(3220);

        Assert.True(ShellScriptLabelTransitions.TryDefine(
            ref platform, frame, startRecord, startName, 5, 1, 0));
        Assert.True(ShellScriptLabelTransitions.TryDefine(
            ref platform, frame, middleRecord, middleName, 6, 5, 40));
        Assert.True(ShellScriptLabelTransitions.TryDefine(
            ref platform, frame, endRecord, endName, 3, 8, 80));
        Assert.True(ShellScriptLabelCodec.TryRead(
            ref platform, middleRecord, out var middleState));
        Assert.Equal("middle", platform.Store.ReadText(
            middleState.Name, middleState.NameLength));
        Assert.Equal((uint)5, middleState.Line);
        Assert.Equal((uint)40, middleState.Offset);
        Assert.Equal("MIDDLE", platform.Store.ReadText(
            duplicateName, 6));
        Assert.True(ShellScriptLabelCodec.NamesEqualNoCase(
            ref platform, middleState.Name, middleState.NameLength,
            duplicateName, 6));
        Assert.False(ShellScriptLabelTransitions.TryDefine(
            ref platform, frame, duplicateRecord, duplicateName, 6, 5, 9));

        Assert.True(ShellScriptLabelTransitions.TryFind(
            ref platform, frame, APTR.Null, 0, 5, 50, 0,
            out var forward, out var forwardLine, out var forwardOffset));
        Assert.Equal(endRecord.Raw, forward.Raw);
        Assert.Equal((uint)8, forwardLine);
        Assert.Equal((uint)80, forwardOffset);

        Assert.True(ShellScriptLabelTransitions.TryFind(
            ref platform, frame, APTR.Null, 0, 5, 50, 1,
            out var backward, out var backwardLine, out var backwardOffset));
        Assert.Equal(middleRecord.Raw, backward.Raw);
        Assert.Equal((uint)5, backwardLine);
        Assert.Equal((uint)40, backwardOffset);

        APTR named = platform.Store.PutAt(180, "START");
        Assert.True(ShellScriptLabelTransitions.TrySkip(
            ref platform, frame, named, 5, 1));
        Assert.True(ShellScriptFrameCodec.TryRead(
            ref platform, frame, out var state));
        Assert.Equal((uint)1, state.CurrentLine);
        Assert.Equal((uint)0, state.CurrentOffset);
        Assert.Equal((uint)3, state.LabelCount);
    }

    [Fact]
    public void Label_index_rejects_malformed_parent_chains()
    {
        EchoCommandTests.TestShellPlatform platform = new();
        APTR frame = new(3000);
        ShellScriptFrameState initial = new()
        {
            Cli = new APTR(8),
            Flags = ShellScriptFrameFlags.Active,
            LabelTop = new APTR(3100),
        };
        Assert.True(ShellScriptFrameCodec.Initialize(
            ref platform, frame, in initial));
        APTR name = platform.Store.PutAt(100, "x");
        Assert.False(ShellScriptLabelTransitions.TryFind(
            ref platform, frame, name, 1, 1, 0, 0,
            out _, out _, out _));
    }
}
