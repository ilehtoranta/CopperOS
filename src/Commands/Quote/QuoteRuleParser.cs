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
        if (Matches(ref memory, source, start, length, QuoteForwardRule.ReadItem)) { rule = QuoteForwardRule.ReadItem; return true; }
        if (Matches(ref memory, source, start, length, QuoteForwardRule.MatchPattern)) { rule = QuoteForwardRule.MatchPattern; return true; }
        if (Matches(ref memory, source, start, length, QuoteForwardRule.Uri)) { rule = QuoteForwardRule.Uri; return true; }
        if (Matches(ref memory, source, start, length, QuoteForwardRule.Hex)) { rule = QuoteForwardRule.Hex; return true; }
        if (Matches(ref memory, source, start, length, QuoteForwardRule.Base64)) { rule = QuoteForwardRule.Base64; return true; }
        if (Matches(ref memory, source, start, length, QuoteForwardRule.Arexx)) { rule = QuoteForwardRule.Arexx; return true; }
        if (Matches(ref memory, source, start, length, QuoteForwardRule.Sh)) { rule = QuoteForwardRule.Sh; return true; }
        if (Matches(ref memory, source, start, length, QuoteForwardRule.Js)) { rule = QuoteForwardRule.Js; return true; }
        if (Matches(ref memory, source, start, length, QuoteForwardRule.C)) { rule = QuoteForwardRule.C; return true; }
        rule = default;
        return false;
    }

    private static bool Matches<TMemory>(ref TMemory memory, APTR source,
        uint start, uint length, QuoteForwardRule rule)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (length != RuleLength(rule)) return false;
        for (var index = 0u; index < length; index++)
        {
            var actual = memory.ReadUInt8(source, (int)(start + index));
            if (ToUpperAscii(actual) != RuleByte(rule, index)) return false;
        }
        return true;
    }

    private static uint RuleLength(QuoteForwardRule rule) => rule switch
    {
        QuoteForwardRule.ReadItem => 8,
        QuoteForwardRule.MatchPattern => 12,
        QuoteForwardRule.Uri or QuoteForwardRule.Hex => 3,
        QuoteForwardRule.Base64 => 6,
        QuoteForwardRule.Arexx => 5,
        QuoteForwardRule.Sh or QuoteForwardRule.Js => 2,
        QuoteForwardRule.C => 1,
        _ => 0,
    };

    private static byte RuleByte(QuoteForwardRule rule, uint index)
    {
        if (rule == QuoteForwardRule.ReadItem)
        {
            if (index == 0) return (byte)'R'; if (index == 1) return (byte)'E';
            if (index == 2) return (byte)'A'; if (index == 3) return (byte)'D';
            if (index == 4) return (byte)'I'; if (index == 5) return (byte)'T';
            if (index == 6) return (byte)'E'; return (byte)'M';
        }
        if (rule == QuoteForwardRule.MatchPattern)
        {
            if (index == 0) return (byte)'M'; if (index == 1) return (byte)'A';
            if (index == 2) return (byte)'T'; if (index == 3) return (byte)'C';
            if (index == 4) return (byte)'H'; if (index == 5) return (byte)'P';
            if (index == 6) return (byte)'A'; if (index == 7) return (byte)'T';
            if (index == 8) return (byte)'T'; if (index == 9) return (byte)'E';
            if (index == 10) return (byte)'R'; return (byte)'N';
        }
        if (rule == QuoteForwardRule.Uri)
            return index == 0 ? (byte)'U' : index == 1 ? (byte)'R' : (byte)'I';
        if (rule == QuoteForwardRule.Hex)
            return index == 0 ? (byte)'H' : index == 1 ? (byte)'E' : (byte)'X';
        if (rule == QuoteForwardRule.Base64)
        {
            if (index == 0) return (byte)'B'; if (index == 1) return (byte)'A';
            if (index == 2) return (byte)'S'; if (index == 3) return (byte)'E';
            if (index == 4) return (byte)'6'; return (byte)'4';
        }
        if (rule == QuoteForwardRule.Arexx)
        {
            if (index == 0) return (byte)'A'; if (index == 1) return (byte)'R';
            if (index == 2) return (byte)'E'; if (index == 3) return (byte)'X';
            return (byte)'X';
        }
        if (rule == QuoteForwardRule.Sh)
            return index == 0 ? (byte)'S' : (byte)'H';
        if (rule == QuoteForwardRule.Js)
            return index == 0 ? (byte)'J' : (byte)'S';
        return (byte)'C';
    }

    private static byte ToUpperAscii(byte value) => value is >= (byte)'a' and <= (byte)'z'
        ? (byte)(value - ((byte)'a' - (byte)'A')) : value;

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
        right.Raw < left.Raw + leftLength;
}
