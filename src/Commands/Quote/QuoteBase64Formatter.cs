using Amiga;

namespace CopperOS.Commands;

/// <summary>Bounded forward Base64 stage for MorphOS Quote pipelines.</summary>
public static class QuoteBase64Formatter
{
    /// <summary>Encodes raw bytes as padded standard Base64 without a line feed.</summary>
    public static bool TryEncode<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR destination, uint capacity, out uint byteCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        byteCount = 0;
        if (source.IsNull || destination.IsNull || sourceLength > uint.MaxValue - 2 ||
            source.Raw > uint.MaxValue - sourceLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(source, sourceLength) ||
            !memory.IsMapped(destination, capacity) ||
            Overlaps(source, sourceLength, destination, capacity)) return false;

        var groups = (sourceLength + 2) / 3;
        if (groups > uint.MaxValue / 4 || groups * 4 > capacity) return false;
        var offset = 0u;
        for (var index = 0u; index < sourceLength; index += 3)
        {
            var remaining = sourceLength - index;
            var first = memory.ReadUInt8(source, (int)index);
            var second = remaining > 1 ? memory.ReadUInt8(source, (int)(index + 1)) : (byte)0;
            var third = remaining > 2 ? memory.ReadUInt8(source, (int)(index + 2)) : (byte)0;
            memory.WriteUInt8(destination, (int)offset++, Alphabet((byte)(first >> 2)));
            memory.WriteUInt8(destination, (int)offset++, Alphabet((byte)((first & 3) << 4 | second >> 4)));
            memory.WriteUInt8(destination, (int)offset++, remaining > 1 ? Alphabet((byte)((second & 15) << 2 | third >> 6)) : (byte)'=');
            memory.WriteUInt8(destination, (int)offset++, remaining > 2 ? Alphabet((byte)(third & 63)) : (byte)'=');
        }
        byteCount = offset;
        return true;
    }

    /// <summary>
    /// Decodes structurally valid padded standard Base64. It rejects malformed
    /// alphabet, placement, and non-canonical padding before writing output.
    /// </summary>
    public static bool TryDecode<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR destination, uint capacity, out uint byteCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        byteCount = 0;
        if (source.IsNull || destination.IsNull || sourceLength % 4 != 0 ||
            source.Raw > uint.MaxValue - sourceLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(source, sourceLength) ||
            !memory.IsMapped(destination, capacity) ||
            Overlaps(source, sourceLength, destination, capacity)) return false;

        uint required = 0;
        for (var index = 0u; index < sourceLength; index += 4)
        {
            var first = Decode(memory.ReadUInt8(source, (int)index));
            var second = Decode(memory.ReadUInt8(source, (int)(index + 1)));
            var thirdByte = memory.ReadUInt8(source, (int)(index + 2));
            var fourthByte = memory.ReadUInt8(source, (int)(index + 3));
            var third = Decode(thirdByte);
            var fourth = Decode(fourthByte);
            var final = index + 4 == sourceLength;
            if (first < 0 || second < 0 || !final && (third < 0 || fourth < 0)) return false;
            if (thirdByte == '=')
            {
                if (!final || fourthByte != '=' || (second & 15) != 0) return false;
                required += 1;
            }
            else if (fourthByte == '=')
            {
                if (!final || third < 0 || (third & 3) != 0) return false;
                required += 2;
            }
            else
            {
                if (third < 0 || fourth < 0) return false;
                required += 3;
            }
        }
        if (required > capacity) return false;

        uint offset = 0;
        for (var index = 0u; index < sourceLength; index += 4)
        {
            var first = Decode(memory.ReadUInt8(source, (int)index));
            var second = Decode(memory.ReadUInt8(source, (int)(index + 1)));
            var thirdByte = memory.ReadUInt8(source, (int)(index + 2));
            var fourthByte = memory.ReadUInt8(source, (int)(index + 3));
            var third = thirdByte == '=' ? 0 : Decode(thirdByte);
            var fourth = fourthByte == '=' ? 0 : Decode(fourthByte);
            memory.WriteUInt8(destination, (int)offset++, (byte)(first << 2 | second >> 4));
            if (thirdByte != '=')
                memory.WriteUInt8(destination, (int)offset++, (byte)((second & 15) << 4 | third >> 2));
            if (fourthByte != '=')
                memory.WriteUInt8(destination, (int)offset++, (byte)((third & 3) << 6 | fourth));
        }
        byteCount = offset;
        return true;
    }

    private static byte Alphabet(byte value) => value < 26 ? (byte)('A' + value) :
        value < 52 ? (byte)('a' + value - 26) : value < 62 ? (byte)('0' + value - 52) :
        value == 62 ? (byte)'+' : (byte)'/';

    private static int Decode(byte value) => value is >= (byte)'A' and <= (byte)'Z' ? value - 'A' :
        value is >= (byte)'a' and <= (byte)'z' ? value - 'a' + 26 :
        value is >= (byte)'0' and <= (byte)'9' ? value - '0' + 52 :
        value == (byte)'+' ? 62 : value == (byte)'/' ? 63 : -1;

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength && right.Raw < left.Raw + leftLength;
}
