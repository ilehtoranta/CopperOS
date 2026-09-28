using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Formats one byte stream in the separately observed MorphOS Type HEX
/// presentation. This byte transform owns no DOS state or stream handles.
/// </summary>
public static class TypeHexFormatter
{
    /// <summary>
    /// Writes uppercase sixteen-byte rows with an offset, hexadecimal bytes,
    /// and printable-byte column. A nonempty partial final row is followed by
    /// one blank line. The destination remains unchanged on invalid mapping,
    /// overlap, or insufficient capacity.
    /// </summary>
    public static bool TryFormat<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR destination, uint capacity, out uint written)
        where TMemory : struct, IAmigaGuestMemory
    {
        written = 0;
        if (source.IsNull || destination.IsNull ||
            source.Raw > uint.MaxValue - sourceLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(source, sourceLength) ||
            !memory.IsMapped(destination, capacity) ||
            Overlaps(source, sourceLength, destination, capacity) ||
            !TryCount(sourceLength, out var required) || required > capacity)
            return false;

        var output = 0u;
        for (var offset = 0u; offset < sourceLength;)
        {
            var remaining = sourceLength - offset;
            var count = remaining < 16 ? remaining : 16u;
            WriteOffset(ref memory, destination, ref output, offset);
            Write(ref memory, destination, ref output, (byte)':');
            Write(ref memory, destination, ref output, (byte)' ');
            for (var index = 0u; index < 16; index++)
            {
                if (index < count)
                {
                    var value = memory.ReadUInt8(source, (int)(offset + index));
                    WriteHex(ref memory, destination, ref output, value);
                }
                else
                {
                    Write(ref memory, destination, ref output, (byte)' ');
                    Write(ref memory, destination, ref output, (byte)' ');
                }
                if ((index & 3) == 3) Write(ref memory, destination, ref output, (byte)' ');
            }
            for (var index = 0u; index < count; index++)
            {
                var value = memory.ReadUInt8(source, (int)(offset + index));
                Write(ref memory, destination, ref output, IsPrintable(value) ? value : (byte)'.');
            }
            Write(ref memory, destination, ref output, (byte)'\n');
            if (count != 16) Write(ref memory, destination, ref output, (byte)'\n');
            offset += count;
        }
        written = output;
        return true;
    }

    private static bool TryCount(uint sourceLength, out uint required)
    {
        required = 0;
        for (var offset = 0u; offset < sourceLength;)
        {
            var remaining = sourceLength - offset;
            var count = remaining < 16 ? remaining : 16u;
            var rowBytes = (uint)(OffsetDigits(offset) + 55 + (count == 16 ? 0 : 1));
            if (required > uint.MaxValue - rowBytes) return false;
            required += rowBytes;
            offset += count;
        }
        return true;
    }

    private static int OffsetDigits(uint offset) => offset < 0x10000 ? 4 :
        offset < 0x100000 ? 5 : offset < 0x1000000 ? 6 :
        offset < 0x10000000 ? 7 : 8;

    private static void WriteOffset<TMemory>(ref TMemory memory,
        APTR destination, ref uint output, uint offset)
        where TMemory : struct, IAmigaGuestMemory
    {
        var digits = OffsetDigits(offset);
        for (var shift = (digits - 1) * 4; shift >= 0; shift -= 4)
            Write(ref memory, destination, ref output,
                Hex((byte)(offset >> shift)));
    }

    private static void WriteHex<TMemory>(ref TMemory memory, APTR destination,
        ref uint output, byte value) where TMemory : struct, IAmigaGuestMemory
    {
        Write(ref memory, destination, ref output, Hex((byte)(value >> 4)));
        Write(ref memory, destination, ref output, Hex(value));
    }

    private static byte Hex(byte value)
    {
        var nibble = (byte)(value & 15);
        return nibble < 10 ? (byte)(nibble + '0') :
            (byte)(nibble - 10 + 'A');
    }

    private static bool IsPrintable(byte value) => (value & 0x7f) >= 0x20 &&
        value != 0x7f;

    private static void Write<TMemory>(ref TMemory memory, APTR destination,
        ref uint output, byte value) where TMemory : struct, IAmigaGuestMemory =>
        memory.WriteUInt8(destination, (int)output++, value);

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
            right.Raw < left.Raw + leftLength;
}
