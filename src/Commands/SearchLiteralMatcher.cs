using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Bounded literal-search primitive for the source-observed MorphOS Search
/// command. It owns no DOS state, buffers, handles, or locale state.
/// </summary>
public static class SearchLiteralMatcher
{
    /// <summary>
    /// Searches a caller-owned byte range for a nonempty literal. When
    /// <paramref name="caseSensitive"/> is false, this bounded primitive folds
    /// ASCII letters only; the command boundary must use locale.library for the
    /// full source-observed locale behavior. Invalid guest ranges return false
    /// and leave <paramref name="found"/> clear.
    /// </summary>
    public static bool TryContains<TMemory>(ref TMemory memory, APTR text,
        uint textLength, APTR pattern, uint patternLength, bool caseSensitive,
        out bool found) where TMemory : struct, IAmigaGuestMemory
    {
        found = false;
        if (text.IsNull || pattern.IsNull || patternLength == 0 ||
            text.Raw > uint.MaxValue - textLength ||
            pattern.Raw > uint.MaxValue - patternLength ||
            !memory.IsMapped(text, textLength) ||
            !memory.IsMapped(pattern, patternLength))
            return false;

        if (patternLength > textLength) return true;
        var limit = textLength - patternLength;
        for (var start = 0u; start <= limit; start++)
        {
            var matched = true;
            for (var index = 0u; index < patternLength; index++)
            {
                var left = memory.ReadUInt8(text, (int)(start + index));
                var right = memory.ReadUInt8(pattern, (int)index);
                if (!caseSensitive)
                {
                    left = FoldAscii(left);
                    right = FoldAscii(right);
                }
                if (left == right) continue;
                matched = false;
                break;
            }
            if (!matched) continue;
            found = true;
            return true;
        }
        return true;
    }

    private static byte FoldAscii(byte value) => value >= (byte)'a' && value <= (byte)'z'
        ? (byte)(value - ('a' - 'A')) : value;
}
