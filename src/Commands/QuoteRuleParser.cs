using Amiga;

namespace CopperOS.Commands;

/// <summary>Bounded candidate parser for the documented Quote RULE sequence.</summary>
public static class QuoteRuleParser
{
    /// <summary>
    /// Parses ASCII rule names separated by spaces or tabs into a caller-owned
    /// byte list. Rule spelling is compared without ASCII case distinction.
    /// It validates the whole sequence and required capacity before writing.
    /// </summary>
    public static bool TryParse<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR destination, uint capacity, out uint ruleCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        ruleCount = 0;
        if (source.IsNull || destination.IsNull ||
            source.Raw > uint.MaxValue - sourceLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(source, sourceLength) ||
            !memory.IsMapped(destination, capacity) ||
            Overlaps(source, sourceLength, destination, capacity)) return false;

        uint position = 0;
        uint count = 0;
        while (true)
        {
            SkipWhitespace(ref memory, source, sourceLength, ref position);
            if (position == sourceLength) break;
            uint tokenStart = position;
            while (position < sourceLength && !IsWhitespace(memory.ReadUInt8(source, (int)position)))
                position++;
            if (!TryGetRule(ref memory, source, tokenStart, position - tokenStart,
                    out _) || count == capacity)
                return false;
            count++;
        }
        if (count == 0) return false;

        position = 0;
        uint output = 0;
        while (true)
        {
            SkipWhitespace(ref memory, source, sourceLength, ref position);
            if (position == sourceLength) break;
            uint tokenStart = position;
            while (position < sourceLength && !IsWhitespace(memory.ReadUInt8(source, (int)position)))
                position++;
            TryGetRule(ref memory, source, tokenStart, position - tokenStart,
                out var rule);
            memory.WriteUInt8(destination, (int)output++, (byte)rule);
        }
        ruleCount = output;
        return true;
    }

    private static void SkipWhitespace<TMemory>(ref TMemory memory, APTR source,
        uint length, ref uint position) where TMemory : struct, IAmigaGuestMemory
    {
        while (position < length && IsWhitespace(memory.ReadUInt8(source, (int)position)))
            position++;
    }

    private static bool IsWhitespace(byte value) => value is (byte)' ' or (byte)'\t';

    private static bool TryGetRule<TMemory>(ref TMemory memory, APTR source,
        uint start, uint length, out QuoteForwardRule rule)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (EqualsAsciiIgnoreCase(ref memory, source, start, length, "READITEM")) { rule = QuoteForwardRule.ReadItem; return true; }
        if (EqualsAsciiIgnoreCase(ref memory, source, start, length, "MATCHPATTERN")) { rule = QuoteForwardRule.MatchPattern; return true; }
        if (EqualsAsciiIgnoreCase(ref memory, source, start, length, "URI")) { rule = QuoteForwardRule.Uri; return true; }
        if (EqualsAsciiIgnoreCase(ref memory, source, start, length, "HEX")) { rule = QuoteForwardRule.Hex; return true; }
        if (EqualsAsciiIgnoreCase(ref memory, source, start, length, "BASE64")) { rule = QuoteForwardRule.Base64; return true; }
        if (EqualsAsciiIgnoreCase(ref memory, source, start, length, "AREXX")) { rule = QuoteForwardRule.Arexx; return true; }
        if (EqualsAsciiIgnoreCase(ref memory, source, start, length, "SH")) { rule = QuoteForwardRule.Sh; return true; }
        if (EqualsAsciiIgnoreCase(ref memory, source, start, length, "JS")) { rule = QuoteForwardRule.Js; return true; }
        if (EqualsAsciiIgnoreCase(ref memory, source, start, length, "C")) { rule = QuoteForwardRule.C; return true; }
        rule = default;
        return false;
    }

    private static bool EqualsAsciiIgnoreCase<TMemory>(ref TMemory memory,
        APTR source, uint start, uint length, string value)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (length != (uint)value.Length) return false;
        for (var index = 0u; index < length; index++)
        {
            var actual = memory.ReadUInt8(source, (int)(start + index));
            var expected = (byte)value[(int)index];
            if (ToUpperAscii(actual) != expected) return false;
        }
        return true;
    }

    private static byte ToUpperAscii(byte value) => value is >= (byte)'a' and <= (byte)'z'
        ? (byte)(value - ((byte)'a' - (byte)'A')) : value;

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
        right.Raw < left.Raw + leftLength;
}
