using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands.Tests;

public sealed class ShellScriptPromptParserTests
{
    [Fact]
    public void Segments_preserve_literals_and_return_backtick_command_as_a_slice()
    {
        var platform = new EchoCommandTests.TestShellPlatform();
        var text = new APTR(5000);
        Write(ref platform, text, "A%N`Echo two words`Z");
        var template = new ShellScriptPromptTemplate
        {
            Text = text,
            Length = 20,
        };
        var cursor = default(ShellScriptPromptCursor);

        Assert.True(ShellScriptPromptParser.TryNext(ref platform,
            in template, in cursor, out var literal));
        Assert.Equal(ShellScriptPromptSegmentKind.Literal, literal.Kind);
        Assert.Equal(text, literal.Text);
        Assert.Equal(3u, literal.Length);
        cursor = literal.Next;

        Assert.True(ShellScriptPromptParser.TryNext(ref platform,
            in template, in cursor, out var command));
        Assert.Equal(ShellScriptPromptSegmentKind.Command, command.Kind);
        Assert.Equal(APTR.FromPointer(text.Raw + 4), command.Text);
        Assert.Equal(14u, command.Length);
        cursor = command.Next;

        Assert.True(ShellScriptPromptParser.TryNext(ref platform,
            in template, in cursor, out var suffix));
        Assert.Equal(ShellScriptPromptSegmentKind.Literal, suffix.Kind);
        Assert.Equal(APTR.FromPointer(text.Raw + 19), suffix.Text);
        Assert.Equal(1u, suffix.Length);
        cursor = suffix.Next;

        Assert.True(ShellScriptPromptParser.TryNext(ref platform,
            in template, in cursor, out var end));
        Assert.Equal(ShellScriptPromptSegmentKind.End, end.Kind);
    }

    [Fact]
    public void Unmatched_backtick_is_reported_without_advancing_the_cursor()
    {
        var platform = new EchoCommandTests.TestShellPlatform();
        var text = new APTR(5200);
        Write(ref platform, text, "prefix `Echo");
        var template = new ShellScriptPromptTemplate
        {
            Text = text,
            Length = 12,
        };
        var cursor = new ShellScriptPromptCursor { Position = 7 };

        Assert.False(ShellScriptPromptParser.TryNext(ref platform,
            in template, in cursor, out _));
        Assert.Equal(7u, cursor.Position);
    }

    [Fact]
    public void Empty_command_is_a_valid_zero_length_span_and_advances()
    {
        var platform = new EchoCommandTests.TestShellPlatform();
        var text = new APTR(5400);
        Write(ref platform, text, "``");
        var template = new ShellScriptPromptTemplate
        {
            Text = text,
            Length = 2,
        };
        var cursor = default(ShellScriptPromptCursor);

        Assert.True(ShellScriptPromptParser.TryNext(ref platform,
            in template, in cursor, out var command));
        Assert.Equal(ShellScriptPromptSegmentKind.Command, command.Kind);
        Assert.Equal(0u, command.Length);
        Assert.Equal(2u, command.Next.Position);
    }

    [Fact]
    public void Template_and_cursor_bounds_are_validated()
    {
        var platform = new EchoCommandTests.TestShellPlatform();
        var text = new APTR(5600);
        Write(ref platform, text, "x");
        var template = new ShellScriptPromptTemplate
        {
            Text = text,
            Length = 1,
        };
        var outside = new ShellScriptPromptCursor { Position = 2 };
        Assert.False(ShellScriptPromptParser.TryNext(ref platform,
            in template, in outside, out _));

        template.Length = ShellScriptPromptParser.MaximumTemplateLength + 1;
        var start = default(ShellScriptPromptCursor);
        Assert.False(ShellScriptPromptParser.TryNext(ref platform,
            in template, in start, out _));
    }

    private static void Write(ref EchoCommandTests.TestShellPlatform platform,
        APTR destination, string value)
    {
        for (var index = 0; index < value.Length; index++)
            platform.WriteUInt8(destination, index, (byte)value[index]);
    }
}
