using Amiga;

namespace CopperOS.Commands;

/// <summary>Bounded lowercase hexadecimal stage for MorphOS Quote pipelines.</summary>
public static class QuoteHexFormatter
{
    /// <summary>
    /// Encodes bytes as lowercase hexadecimal with no terminator or line feed.
    /// The caller owns both spans and output I/O.
    /// </summary>
    public static bool TryEncode<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR destination, uint capacity, out uint byteCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        byteCount = 0;
        if (source.IsNull || destination.IsNull || sourceLength > uint.MaxValue / 2 ||
            source.Raw > uint.MaxValue - sourceLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(source, sourceLength) ||
            !memory.IsMapped(destination, capacity) ||
            sourceLength * 2 > capacity ||
            Overlaps(source, sourceLength, destination, capacity))
            return false;

        for (var index = 0u; index < sourceLength; index++)
        {
            var value = memory.ReadUInt8(source, (int)index);
            memory.WriteUInt8(destination, (int)(index * 2), Digit((byte)(value >> 4)));
            memory.WriteUInt8(destination, (int)(index * 2 + 1), Digit((byte)(value & 15)));
        }
        byteCount = sourceLength * 2;
        return true;
    }

    /// <summary>
    /// Decodes ASCII hexadecimal pairs. Upper- and lowercase digits are
    /// accepted; odd or malformed input is rejected before writing output.
    /// No terminator or line feed is appended.
    /// </summary>
    public static bool TryDecode<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR destination, uint capacity, out uint byteCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        byteCount = 0;
        if (source.IsNull || destination.IsNull || sourceLength % 2 != 0 ||
            source.Raw > uint.MaxValue - sourceLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(source, sourceLength) ||
            !memory.IsMapped(destination, capacity) ||
            Overlaps(source, sourceLength, destination, capacity)) return false;

        var required = sourceLength / 2;
        if (required > capacity) return false;
        for (var index = 0u; index < sourceLength; index++)
            if (Decode(memory.ReadUInt8(source, (int)index)) < 0) return false;

        for (var index = 0u; index < required; index++)
        {
            var high = Decode(memory.ReadUInt8(source, (int)(index * 2)));
            var low = Decode(memory.ReadUInt8(source, (int)(index * 2 + 1)));
            memory.WriteUInt8(destination, (int)index, (byte)(high << 4 | low));
        }
        byteCount = required;
        return true;
    }

    private static byte Digit(byte value) => (byte)(value < 10
        ? (byte)'0' + value
        : (byte)'a' + value - 10);

    private static int Decode(byte value) => value is >= (byte)'0' and <= (byte)'9'
        ? value - '0' : value is >= (byte)'a' and <= (byte)'f' ? value - 'a' + 10
        : value is >= (byte)'A' and <= (byte)'F' ? value - 'A' + 10 : -1;

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
        right.Raw < left.Raw + leftLength;
}
