using Amiga;

namespace CopperOS.Commands;

/// <summary>Bounded percent-encoding stage for MorphOS Quote URI components.</summary>
public static class QuoteUriFormatter
{
    /// <summary>
    /// Encodes raw bytes as an RFC 3986 URI component using uppercase percent
    /// digits. Unreserved ASCII bytes are copied unchanged; no terminator or
    /// line feed is appended.
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

        uint required = 0;
        for (var index = 0u; index < sourceLength; index++)
        {
            required += IsUnreserved(memory.ReadUInt8(source, (int)index)) ? 1u : 3u;
            if (required > capacity) return false;
        }

        uint offset = 0;
        for (var index = 0u; index < sourceLength; index++)
        {
            var value = memory.ReadUInt8(source, (int)index);
            if (IsUnreserved(value))
            {
                memory.WriteUInt8(destination, (int)offset++, value);
                continue;
            }
            memory.WriteUInt8(destination, (int)offset++, (byte)'%');
            memory.WriteUInt8(destination, (int)offset++, Digit((byte)(value >> 4)));
            memory.WriteUInt8(destination, (int)offset++, Digit((byte)(value & 15)));
        }
        byteCount = offset;
        return true;
    }

    /// <summary>
    /// Decodes strict percent escapes in one URI component. Non-percent bytes,
    /// including <c>+</c>, are preserved; malformed or truncated escapes are
    /// rejected before writing output.
    /// </summary>
    public static bool TryDecode<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR destination, uint capacity, out uint byteCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        byteCount = 0;
        if (source.IsNull || destination.IsNull ||
            source.Raw > uint.MaxValue - sourceLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(source, sourceLength) ||
            !memory.IsMapped(destination, capacity) ||
            Overlaps(source, sourceLength, destination, capacity)) return false;

        uint required = 0;
        for (var index = 0u; index < sourceLength; index++)
        {
            if (memory.ReadUInt8(source, (int)index) == (byte)'%')
            {
                if (index + 2 >= sourceLength ||
                    Decode(memory.ReadUInt8(source, (int)(index + 1))) < 0 ||
                    Decode(memory.ReadUInt8(source, (int)(index + 2))) < 0) return false;
                index += 2;
            }
            required++;
            if (required > capacity) return false;
        }

        uint output = 0;
        for (var index = 0u; index < sourceLength; index++)
        {
            var value = memory.ReadUInt8(source, (int)index);
            if (value == (byte)'%')
            {
                var high = Decode(memory.ReadUInt8(source, (int)++index));
                var low = Decode(memory.ReadUInt8(source, (int)++index));
                value = (byte)(high << 4 | low);
            }
            memory.WriteUInt8(destination, (int)output++, value);
        }
        byteCount = output;
        return true;
    }

    private static bool IsUnreserved(byte value) =>
        value is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z' or
        >= (byte)'0' and <= (byte)'9' or (byte)'-' or (byte)'.' or (byte)'_' or (byte)'~';

    private static byte Digit(byte value) => (byte)(value < 10
        ? (byte)'0' + value
        : (byte)'A' + value - 10);

    private static int Decode(byte value) => value is >= (byte)'0' and <= (byte)'9'
        ? value - '0' : value is >= (byte)'a' and <= (byte)'f' ? value - 'a' + 10
        : value is >= (byte)'A' and <= (byte)'F' ? value - 'A' + 10 : -1;

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
        right.Raw < left.Raw + leftLength;
}
