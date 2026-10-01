using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Formats one Type text stream using the independently observed MorphOS 50.6
/// NUMBER and NOLINE behavior. DOS traversal and stream ownership stay outside
/// this pure byte transform.
/// </summary>
public static class TypeTextFormatter
{
    /// <summary>
    /// Copies source to destination, optionally prefixing each logical line with
    /// the observed five-column line number and trailing space. When NOLINE is
    /// clear, appends one LF if the source is empty or does not end in LF.
    /// The destination is unchanged when the complete result cannot fit.
    /// </summary>
    public static bool TryFormat<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, bool number, bool noLine, APTR destination,
        uint capacity, out uint written)
        where TMemory : struct, IAmigaGuestMemory
    {
        written = 0;
        if (source.IsNull || destination.IsNull ||
            source.Raw > uint.MaxValue - sourceLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(source, sourceLength) ||
            !memory.IsMapped(destination, capacity) ||
            Overlaps(source, sourceLength, destination, capacity) ||
            !TryCount(ref memory, source, sourceLength, number, noLine,
                out var required) || required > capacity)
            return false;

        uint output = 0;
        ushort line = 0;
        if (number)
            WriteLineNumber(ref memory, destination, ref output, unchecked(++line));

        byte previous = 0;
        for (var input = 0u; input < sourceLength; input++)
        {
            var value = memory.ReadUInt8(source, (int)input);
            if (number && previous == (byte)'\n')
                WriteLineNumber(ref memory, destination, ref output, unchecked(++line));
            memory.WriteUInt8(destination, (int)output++, value);
            previous = value;
        }
        if (!noLine && previous != (byte)'\n')
            memory.WriteUInt8(destination, (int)output++, (byte)'\n');
        written = output;
        return true;
    }

    private static bool TryCount<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, bool number, bool noLine, out uint required)
        where TMemory : struct, IAmigaGuestMemory
    {
        required = sourceLength;
        if (!noLine && (sourceLength == 0 ||
            memory.ReadUInt8(source, (int)(sourceLength - 1)) != (byte)'\n'))
        {
            if (required == uint.MaxValue) return false;
            required++;
        }
        if (!number) return true;

        uint lineCount = 1;
        for (var index = 0u; index < sourceLength; index++)
            if (memory.ReadUInt8(source, (int)index) == (byte)'\n' &&
                index + 1 < sourceLength)
            {
                if (lineCount == uint.MaxValue) return false;
                lineCount++;
            }
        return lineCount <= (uint.MaxValue - required) / 6
            ? (required += lineCount * 6) >= lineCount * 6
            : false;
    }

    private static void WriteLineNumber<TMemory>(ref TMemory memory,
        APTR destination, ref uint output, ushort line)
        where TMemory : struct, IAmigaGuestMemory
    {
        var current = (uint)line;
        var divisor = 10000;
        var emitted = false;
        while (divisor != 0)
        {
            var digit = current / (uint)divisor;
            memory.WriteUInt8(destination, (int)output++,
                digit != 0 || emitted ? (byte)(digit + '0') : (byte)' ');
            if (digit != 0 || emitted)
            {
                current %= (uint)divisor;
                emitted = true;
            }
            divisor /= 10;
        }
        memory.WriteUInt8(destination, (int)output++, (byte)' ');
    }

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
            right.Raw < left.Raw + leftLength;
}
