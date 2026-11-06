using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Bounded label registration and target lookup for one script frame. Label
/// record/name storage is supplied and owned by the Shell frame.
/// </summary>
public static class ShellScriptLabelTransitions
{
    public const uint MaximumRecords = 1024;

    public static bool TryDefine<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        APTR record,
        APTR name,
        uint nameLength,
        uint line,
        uint offset)
        where TPlatform : struct, IShellPlatform
    {
        if (record.IsNull || !ShellScriptFrameCodec.TryRead(
                ref platform, frame, out var frameState) ||
            frameState.LabelCount == uint.MaxValue ||
            Overlaps(record, ShellScriptLabelCodec.Size,
                frame, ShellScriptFrameCodec.Size) ||
            Overlaps(record, ShellScriptLabelCodec.Size, name, nameLength) ||
            !ValidName(ref platform, name, nameLength))
            return false;

        var cursor = frameState.LabelTop;
        for (var count = 0u; cursor.IsNotNull; count++)
        {
            if (count >= MaximumRecords ||
                Overlaps(record, ShellScriptLabelCodec.Size,
                    cursor, ShellScriptLabelCodec.Size) ||
                !ShellScriptLabelCodec.TryRead(ref platform, cursor,
                    out var existing))
                return false;
            if (ShellScriptLabelCodec.NamesEqualNoCase(
                    ref platform, existing.Name, existing.NameLength,
                    name, nameLength))
                return false;
            cursor = existing.Parent;
        }

        ShellScriptLabelState label = new()
        {
            Parent = frameState.LabelTop,
            Name = name,
            NameLength = nameLength,
            Line = line,
            Offset = offset,
        };
        if (!ShellScriptLabelCodec.Initialize(
                ref platform, record, in label) ||
            !ShellScriptFrameCodec.TrySetLabelTop(
                ref platform, frame, record))
            return false;
        if (ShellScriptFrameCodec.TryIncrementLabelCount(
                ref platform, frame))
            return true;

        ShellScriptFrameCodec.TrySetLabelTop(
            ref platform, frame, frameState.LabelTop);
        return false;
    }

    public static bool TryFind<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        APTR name,
        uint nameLength,
        uint currentLine,
        uint currentOffset,
        uint back,
        out APTR record,
        out uint line,
        out uint offset)
        where TPlatform : struct, IShellPlatform
    {
        record = APTR.Null;
        line = 0;
        offset = 0;
        if (back > 1 || (nameLength != 0 &&
                !ValidName(ref platform, name, nameLength)) ||
            !ShellScriptFrameCodec.TryRead(ref platform, frame,
                out var frameState))
            return false;

        var cursor = frameState.LabelTop;
        var found = false;
        var bestLine = 0u;
        var bestOffset = 0u;
        for (var count = 0u; cursor.IsNotNull; count++)
        {
            if (count >= MaximumRecords ||
                !ShellScriptLabelCodec.TryRead(ref platform, cursor,
                    out var candidate))
                return false;
            if (nameLength != 0 &&
                !ShellScriptLabelCodec.NamesEqualNoCase(
                    ref platform, candidate.Name, candidate.NameLength,
                    name, nameLength))
            {
                cursor = candidate.Parent;
                continue;
            }

            var after = IsAfter(candidate.Line, candidate.Offset,
                currentLine, currentOffset);
            if ((back == 0 && !after) || (back != 0 && after))
            {
                cursor = candidate.Parent;
                continue;
            }
            if (!found || (back == 0
                    ? IsBefore(candidate.Line, candidate.Offset,
                        bestLine, bestOffset)
                    : IsAfter(candidate.Line, candidate.Offset,
                        bestLine, bestOffset)))
            {
                found = true;
                record = cursor;
                bestLine = candidate.Line;
                bestOffset = candidate.Offset;
            }
            cursor = candidate.Parent;
        }

        if (!found)
            return false;
        line = bestLine;
        offset = bestOffset;
        return true;
    }

    public static bool TrySkip<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        APTR name,
        uint nameLength,
        uint back)
        where TPlatform : struct, IShellPlatform
    {
        if (!ShellScriptFrameCodec.TryRead(ref platform, frame,
                out var state) || !TryFind(ref platform, frame, name,
                nameLength, state.CurrentLine, state.CurrentOffset, back,
                out _, out var line, out var offset))
            return false;
        return ShellScriptFrameCodec.TryAdvance(
            ref platform, frame, line, offset);
    }

    /// <summary>Searches the unread script for a LAB target or ENDSKIP boundary.</summary>
    public static bool TryScanForward<TPlatform>(ref TPlatform platform,
        APTR frame, APTR name, uint nameLength, APTR lineBuffer,
        uint lineCapacity, APTR tokenBuffer, uint tokenCapacity)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
        => TryScanForward(ref platform, frame, name, nameLength, lineBuffer,
            lineCapacity, tokenBuffer, tokenCapacity, out _);

    public static bool TryScanForward<TPlatform>(ref TPlatform platform,
        APTR frame, APTR name, uint nameLength, APTR lineBuffer,
        uint lineCapacity, APTR tokenBuffer, uint tokenCapacity, out int result)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
        => TryScan(ref platform, frame, name, nameLength, lineBuffer,
            lineCapacity, tokenBuffer, tokenCapacity, false, out result);

    public static bool TryScanFromBeginning<TPlatform>(ref TPlatform platform,
        APTR frame, APTR name, uint nameLength, APTR lineBuffer,
        uint lineCapacity, APTR tokenBuffer, uint tokenCapacity, out int result)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
        => TryScan(ref platform, frame, name, nameLength, lineBuffer,
            lineCapacity, tokenBuffer, tokenCapacity, true, out result);

    private static bool TryScan<TPlatform>(ref TPlatform platform,
        APTR frame, APTR name, uint nameLength, APTR lineBuffer,
        uint lineCapacity, APTR tokenBuffer, uint tokenCapacity, bool fromBeginning,
        out int result) where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        result = (int)ShellCommandResult.Fail;
        if (!ShellScriptFrameCodec.TryRead(ref platform, frame, out var state) ||
            (nameLength != 0 && !ValidName(ref platform, name, nameLength)) ||
            lineBuffer.IsNull || lineCapacity < 2 || tokenBuffer.IsNull || tokenCapacity < 4 ||
            !platform.IsMapped(lineBuffer, lineCapacity) || !platform.IsMapped(tokenBuffer, tokenCapacity))
            return false;

        var line = state.CurrentLine;
        var offset = state.CurrentOffset;
        var first = true;
        if (fromBeginning) { line = 1; offset = 0; first = false; }
        while (true)
        {
            if (!platform.TryReadScriptLine(state.Cli, state.Input, line, offset,
                    lineBuffer, lineCapacity, out var length, out var nextLine,
                    out var nextOffset, out var eof) || eof > 1 || length >= lineCapacity)
                return false;
            if (!first && length != 0)
            {
                var cursor = new ShellTextCursor(lineBuffer, length);
                var token = ShellTextParser.NextToken(ref platform, ref cursor, tokenBuffer,
                    tokenCapacity, out var commandLength, out _);
                if (token == (int)ShellTextTokenResult.Token &&
                    ShellTextParser.EqualsPacked(ref platform, tokenBuffer, commandLength,
                        0x456e6453, 0x6b697000))
                {
                    if (!ShellScriptFrameCodec.TryAdvance(ref platform, frame, nextLine, nextOffset))
                        return false;
                    result = (int)ShellCommandResult.Warn;
                    return true;
                }
                if (token == (int)ShellTextTokenResult.Token &&
                    commandLength == 3 && Lower(platform.ReadUInt8(tokenBuffer, 0)) == (byte)'l' &&
                    Lower(platform.ReadUInt8(tokenBuffer, 1)) == (byte)'a' &&
                    Lower(platform.ReadUInt8(tokenBuffer, 2)) == (byte)'b')
                {
                    var labelToken = ShellTextParser.NextToken(ref platform, ref cursor,
                        tokenBuffer, tokenCapacity, out var foundLength, out _);
                    // V40's omitted target matches an unnamed LAB, rather
                    // than the next named label. ENDSKIP is handled above.
                    var matches = labelToken == (int)ShellTextTokenResult.End
                        ? nameLength == 0
                        : labelToken == (int)ShellTextTokenResult.Token && foundLength == nameLength &&
                          (nameLength == 0 || ShellScriptLabelCodec.NamesEqualNoCase(ref platform,
                              tokenBuffer, foundLength, name, nameLength));
                    if (matches)
                    {
                        if (!ShellScriptFrameCodec.TryAdvance(ref platform, frame, nextLine, nextOffset)) return false;
                        result = (int)ShellCommandResult.Ok;
                        return true;
                    }
                }
            }
            if (eof != 0)
            {
                if (!ShellScriptFrameCodec.TryAdvance(ref platform, frame, nextLine, nextOffset))
                    return false;
                result = (int)ShellCommandResult.Error;
                return true;
            }
            if (nextOffset <= offset || nextLine <= line) return false;
            first = false;
            line = nextLine;
            offset = nextOffset;
        }
    }

    private static byte Lower(byte value) => value is >= (byte)'A' and <= (byte)'Z'
        ? (byte)(value + 32) : value;

    private static bool ValidName<TPlatform>(
        ref TPlatform platform,
        APTR name,
        uint length)
        where TPlatform : struct, IShellPlatform =>
        !name.IsNull && length != 0 &&
        name.Raw <= uint.MaxValue - length &&
        platform.IsMapped(name, length);

    private static bool IsAfter(uint line, uint offset,
        uint otherLine, uint otherOffset) =>
        line > otherLine || (line == otherLine && offset > otherOffset);

    private static bool IsBefore(uint line, uint offset,
        uint otherLine, uint otherOffset) =>
        line < otherLine || (line == otherLine && offset < otherOffset);

    private static bool Overlaps(
        APTR first,
        uint firstSize,
        APTR second,
        uint secondSize)
    {
        if (first.Raw > uint.MaxValue - firstSize ||
            second.Raw > uint.MaxValue - secondSize)
            return true;
        return first.Raw < second.Raw + secondSize &&
            second.Raw < first.Raw + firstSize;
    }
}
