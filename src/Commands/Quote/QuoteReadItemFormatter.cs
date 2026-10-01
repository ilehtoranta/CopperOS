using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Encodes one byte string as a single AmigaDOS ReadItem value. This is the
/// release-specific READITEM stage used by MorphOS Quote; command argument,
/// source, rule-pipeline, and output ownership stay outside this primitive.
/// </summary>
public static class QuoteReadItemFormatter
{
    /// <summary>
    /// Writes a bounded ReadItem representation. A value containing <c>=</c>
    /// is quoted, matching the MorphOS 3.20 release-note regression. No NUL
    /// terminator is written and the returned count is the exact output size.
    /// </summary>
    public static bool TryEncode<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR destination, uint capacity, out uint byteCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        byteCount = 0;
        if (source.IsNull || destination.IsNull ||
            source.Raw > uint.MaxValue - sourceLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(source, sourceLength) ||
            !memory.IsMapped(destination, capacity) ||
            Overlaps(source, sourceLength, destination, capacity))
            return false;

        var quoted = sourceLength == 0;
        for (var index = 0u; index < sourceLength; index++)
        {
            var value = memory.ReadUInt8(source, (int)index);
            if (RequiresQuotes(value)) quoted = true;
        }
        uint required = quoted ? 2u : sourceLength;
        if (quoted)
        {
            for (var index = 0u; index < sourceLength; index++)
            {
                var value = memory.ReadUInt8(source, (int)index);
                if (value is (byte)'*' or (byte)'"' or (byte)'\n' or 27)
                    required++;
                required++;
            }
        }
        if (required > capacity) return false;

        var offset = 0u;
        if (quoted) memory.WriteUInt8(destination, (int)offset++, (byte)'"');
        for (var index = 0u; index < sourceLength; index++)
        {
            var value = memory.ReadUInt8(source, (int)index);
            if (quoted && value is (byte)'*' or (byte)'"' or (byte)'\n' or 27)
            {
                memory.WriteUInt8(destination, (int)offset++, (byte)'*');
                value = value switch
                {
                    (byte)'\n' => (byte)'N',
                    27 => (byte)'E',
                    _ => value,
                };
            }
            memory.WriteUInt8(destination, (int)offset++, value);
        }
        if (quoted) memory.WriteUInt8(destination, (int)offset++, (byte)'"');
        byteCount = offset;
        return true;
    }

    private static bool RequiresQuotes(byte value) => value <= (byte)' ' ||
        value is (byte)'=' or (byte)';' or (byte)'*' or (byte)'"';

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
        right.Raw < left.Raw + leftLength;
}
