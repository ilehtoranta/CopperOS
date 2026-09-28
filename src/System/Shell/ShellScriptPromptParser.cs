using Amiga;
using System.Runtime.InteropServices;

namespace CopperOS.Shell;

/// <summary>Bounded view of the text portion of a CLI prompt template.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 8)]
public struct ShellScriptPromptTemplate
{
    public APTR Text;
    public uint Length;
}

/// <summary>Resumable position in a prompt template.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 4)]
public struct ShellScriptPromptCursor
{
    public uint Position;
}

/// <summary>The next kind of text found in a MorphOS prompt template.</summary>
public enum ShellScriptPromptSegmentKind : uint
{
    Literal = 0,
    Command = 1,
    End = 2,
}

/// <summary>One literal or backtick-delimited command span.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2, Size = 16)]
public struct ShellScriptPromptSegment
{
    public ShellScriptPromptSegmentKind Kind;
    public APTR Text;
    public uint Length;
    public ShellScriptPromptCursor Next;
}

/// <summary>
/// Reads prompt templates without allocating or retaining parser state in a
/// managed object. Command spans are returned as source slices so DOS can
/// pass them through the normal Shell command pipeline.
/// </summary>
public static class ShellScriptPromptParser
{
    public const uint MaximumTemplateLength = byte.MaxValue;

    public static bool TryNext<TPlatform>(ref TPlatform platform,
        in ShellScriptPromptTemplate template,
        in ShellScriptPromptCursor cursor,
        out ShellScriptPromptSegment segment)
        where TPlatform : struct, IShellPlatform
    {
        segment = default;
        if (template.Length > MaximumTemplateLength ||
            cursor.Position > template.Length ||
            (template.Length != 0 &&
                (template.Text.IsNull ||
                 template.Text.Raw > uint.MaxValue - template.Length ||
                 !platform.IsMapped(template.Text, template.Length))))
            return false;

        if (cursor.Position == template.Length)
        {
            segment.Kind = ShellScriptPromptSegmentKind.End;
            segment.Next = cursor;
            return true;
        }

        var start = cursor.Position;
        var current = Read(ref platform, template.Text, start);
        if (current == (byte)'`')
        {
            var commandStart = start + 1;
            var close = commandStart;
            while (close < template.Length &&
                Read(ref platform, template.Text, close) != (byte)'`')
                close++;
            if (close == template.Length) return false;

            segment.Kind = ShellScriptPromptSegmentKind.Command;
            segment.Text = At(template.Text, commandStart);
            segment.Length = close - commandStart;
            segment.Next.Position = close + 1;
            return true;
        }

        var end = start + 1;
        while (end < template.Length &&
            Read(ref platform, template.Text, end) != (byte)'`')
            end++;

        segment.Kind = ShellScriptPromptSegmentKind.Literal;
        segment.Text = At(template.Text, start);
        segment.Length = end - start;
        segment.Next.Position = end;
        return true;
    }

    private static byte Read<TPlatform>(ref TPlatform platform, APTR text,
        uint position) where TPlatform : struct, IShellPlatform =>
        platform.ReadUInt8(text, unchecked((int)position));

    private static APTR At(APTR text, uint position) =>
        APTR.FromPointer(unchecked(text.Raw + position));
}
