using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Formats the line-oriented literal portion of MorphOS Search using explicit
/// caller-owned guest ranges. Locale and DOS traversal stay at the command
/// boundary.
/// </summary>
public static class SearchLineFormatter
{
    /// <summary>
    /// Finds nonempty literal matches in logical lines and writes their selected
    /// output. LF, NUL, and ASCII control bytes other than TAB terminate a line.
    /// When <paramref name="quiet"/> is set, matching stops at the first match
    /// and produces no line output. ASCII-only folding is used when CASE is
    /// clear; locale-library folding remains the native command's obligation.
    /// No destination bytes are changed if validation or capacity checks fail.
    /// </summary>
    public static bool TryFormat<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR pattern, uint patternLength, bool caseSensitive,
        bool noNumber, bool quiet, uint linesAfter, APTR destination,
        uint capacity, out bool found, out uint written)
        where TMemory : struct, IAmigaGuestMemory
    {
        found = false;
        written = 0;
        if (source.IsNull || pattern.IsNull || destination.IsNull ||
            patternLength == 0 || source.Raw > uint.MaxValue - sourceLength ||
            pattern.Raw > uint.MaxValue - patternLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(source, sourceLength) ||
            !memory.IsMapped(pattern, patternLength) ||
            !memory.IsMapped(destination, capacity) ||
            Overlaps(source, sourceLength, destination, capacity) ||
            !TryMeasure(ref memory, source, sourceLength, pattern, patternLength,
                caseSensitive, noNumber, quiet, linesAfter, out found,
                out var required) || required > capacity)
            return false;

        var output = 0u;
        WriteSelected(ref memory, source, sourceLength, pattern, patternLength,
            caseSensitive, noNumber, quiet, linesAfter, destination, ref output);
        written = output;
        return true;
    }

    private static bool TryMeasure<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR pattern, uint patternLength, bool caseSensitive,
        bool noNumber, bool quiet, uint linesAfter, out bool found,
        out uint required) where TMemory : struct, IAmigaGuestMemory
    {
        found = false;
        required = 0;
        var offset = 0u;
        var lineNumber = 1u;
        var following = 0u;
        while (TryReadLine(ref memory, source, sourceLength, ref offset,
            ref lineNumber, out var currentLine, out var start, out var length))
        {
            if (!SearchLiteralMatcher.TryContains(ref memory,
                    new APTR(source.Raw + start), length, pattern, patternLength,
                    caseSensitive, out var matches)) return false;
            if (matches)
            {
                found = true;
                if (quiet) return true;
                following = linesAfter;
            }
            else if (following != 0) following--;
            else continue;

            var prefix = noNumber ? 0u : NumberWidth(currentLine) + 2;
            if (required > uint.MaxValue - prefix ||
                required + prefix > uint.MaxValue - length - 1) return false;
            required += prefix + length + 1;
        }
        return true;
    }

    private static void WriteSelected<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR pattern, uint patternLength, bool caseSensitive,
        bool noNumber, bool quiet, uint linesAfter, APTR destination,
        ref uint output) where TMemory : struct, IAmigaGuestMemory
    {
        var offset = 0u;
        var lineNumber = 1u;
        var following = 0u;
        while (TryReadLine(ref memory, source, sourceLength, ref offset,
            ref lineNumber, out var currentLine, out var start, out var length))
        {
            SearchLiteralMatcher.TryContains(ref memory, new APTR(source.Raw + start),
                length, pattern, patternLength, caseSensitive, out var matches);
            var marker = (byte)'>';
            if (matches)
            {
                if (quiet) return;
                following = linesAfter;
            }
            else if (following != 0)
            {
                following--;
                marker = (byte)':';
            }
            else continue;

            if (!noNumber) WriteNumber(ref memory, destination, ref output,
                currentLine, marker);
            for (var index = 0u; index < length; index++)
            {
                var value = memory.ReadUInt8(source, (int)(start + index));
                memory.WriteUInt8(destination, (int)output++,
                    IsPrintable(value) ? value : (byte)'.');
            }
            memory.WriteUInt8(destination, (int)output++, (byte)'\n');
        }
    }

    private static bool TryReadLine<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, ref uint offset, ref uint nextLineNumber,
        out uint lineNumber, out uint start, out uint length)
        where TMemory : struct, IAmigaGuestMemory
    {
        lineNumber = 0;
        start = 0;
        length = 0;
        if (offset >= sourceLength) return false;
        lineNumber = nextLineNumber;
        start = offset;
        while (offset < sourceLength)
        {
            var value = memory.ReadUInt8(source, (int)offset++);
            if (!IsDelimiter(value)) continue;
            length = offset - start - 1;
            if (value == (byte)'\n') nextLineNumber++;
            return true;
        }
        length = offset - start;
        return true;
    }

    private static uint NumberWidth(uint value)
    {
        if (value >= 1_000_000_000) return 10;
        if (value >= 100_000_000) return 9;
        if (value >= 10_000_000) return 8;
        if (value >= 1_000_000) return 7;
        return 6;
    }

    private static void WriteNumber<TMemory>(ref TMemory memory, APTR destination,
        ref uint output, uint value, byte marker) where TMemory : struct, IAmigaGuestMemory
    {
        var width = NumberWidth(value);
        var divisor = InitialDivisor(width);
        var emitted = false;
        var current = value;
        for (var index = 0u; index < width; index++)
        {
            var digit = 0u;
            while (current >= divisor)
            {
                current -= divisor;
                digit++;
            }
            memory.WriteUInt8(destination, (int)output++, digit != 0 || emitted ||
                divisor == 1 ? (byte)(digit + '0') : (byte)' ');
            if (digit != 0 || divisor == 1) emitted = true;
            divisor = NextDivisor(divisor);
        }
        memory.WriteUInt8(destination, (int)output++, marker);
        memory.WriteUInt8(destination, (int)output++, (byte)' ');
    }

    private static uint InitialDivisor(uint width)
    {
        if (width == 10) return 1_000_000_000;
        if (width == 9) return 100_000_000;
        if (width == 8) return 10_000_000;
        if (width == 7) return 1_000_000;
        return 100_000;
    }

    private static uint NextDivisor(uint divisor)
    {
        if (divisor == 1_000_000_000) return 100_000_000;
        if (divisor == 100_000_000) return 10_000_000;
        if (divisor == 10_000_000) return 1_000_000;
        if (divisor == 1_000_000) return 100_000;
        if (divisor == 100_000) return 10_000;
        if (divisor == 10_000) return 1_000;
        if (divisor == 1_000) return 100;
        if (divisor == 100) return 10;
        return 1;
    }

    private static bool IsDelimiter(byte value) => value == (byte)'\n' ||
        value == 0 || (value != (byte)'\t' && (value < 0x20 || value == 0x7f));
    private static bool IsPrintable(byte value) => value >= 0x20 && value <= 0x7e;
    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
            right.Raw < left.Raw + leftLength;
}
