using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands;

/// <summary>Failure reasons for the bounded Eval LFORMAT candidate.</summary>
public enum EvalLFormatStatus : byte
{
    Success,
    InvalidSpan,
    InsufficientCapacity,
    UnsupportedConversion,
}

/// <summary>
/// Formats the source-observed `%n`, `%x`, and `%o` Eval LFORMAT conversions.
/// It retains no text and performs a complete sizing pass before writing.
/// </summary>
public static class EvalLFormatFormatter
{
    /// <summary>
    /// Writes one LFORMAT result without an implicit newline. Percent literals,
    /// a trailing percent, and unknown conversions remain literal; `%c` stays
    /// explicitly unsupported until its platform behavior is captured.
    /// </summary>
    public static bool TryFormat<TMemory>(ref TMemory memory, APTR format,
        uint formatLength, long value, APTR destination, uint capacity,
        out uint byteCount, out EvalLFormatStatus status)
        where TMemory : struct, IAmigaGuestMemory
    {
        byteCount = 0;
        status = EvalLFormatStatus.InvalidSpan;
        if (format.IsNull || destination.IsNull ||
            format.Raw > uint.MaxValue - formatLength ||
            destination.Raw > uint.MaxValue - capacity ||
            !memory.IsMapped(format, formatLength) ||
            !memory.IsMapped(destination, capacity) ||
            Overlaps(format, formatLength, destination, capacity)) return false;

        if (!TryMeasure(ref memory, format, formatLength, value, out var required,
                out status)) return false;
        if (required > capacity)
        {
            status = EvalLFormatStatus.InsufficientCapacity;
            return false;
        }

        uint output = 0;
        for (var index = 0u; index < formatLength; index++)
        {
            var current = memory.ReadUInt8(format, (int)index);
            if (current != (byte)'%' || index + 1 == formatLength)
            {
                memory.WriteUInt8(destination, (int)output++, current);
                continue;
            }

            var conversion = memory.ReadUInt8(format, (int)++index);
            if (conversion == (byte)'%')
            {
                memory.WriteUInt8(destination, (int)output++, conversion);
                continue;
            }
            var normalized = ToLowerAscii(conversion);
            if (normalized is not ((byte)'n' or (byte)'x' or (byte)'o'))
            {
                memory.WriteUInt8(destination, (int)output++, (byte)'%');
                memory.WriteUInt8(destination, (int)output++, conversion);
                continue;
            }
            if (!WriteNumber(ref memory, destination, output, capacity - output,
                    value, normalized, out var written))
            {
                byteCount = 0;
                status = EvalLFormatStatus.InvalidSpan;
                return false;
            }
            output += written;
        }
        byteCount = output;
        status = EvalLFormatStatus.Success;
        return true;
    }

    private static bool TryMeasure<TMemory>(ref TMemory memory, APTR format,
        uint formatLength, long value, out uint required,
        out EvalLFormatStatus status) where TMemory : struct, IAmigaGuestMemory
    {
        required = 0;
        status = EvalLFormatStatus.Success;
        for (var index = 0u; index < formatLength; index++)
        {
            var current = memory.ReadUInt8(format, (int)index);
            if (current != (byte)'%' || index + 1 == formatLength)
            {
                required++;
                continue;
            }
            var conversion = memory.ReadUInt8(format, (int)++index);
            if (conversion == (byte)'%') { required++; continue; }
            var normalized = ToLowerAscii(conversion);
            if (normalized == (byte)'c')
            {
                status = EvalLFormatStatus.UnsupportedConversion;
                return false;
            }
            if (normalized is (byte)'n' or (byte)'x' or (byte)'o')
                required += NumberLength(value, normalized);
            else required += 2;
            if (required < 2) // unsigned wrap
            {
                status = EvalLFormatStatus.InsufficientCapacity;
                return false;
            }
        }
        return true;
    }

    private static bool WriteNumber<TMemory>(ref TMemory memory, APTR destination,
        uint offset, uint capacity, long value, byte conversion, out uint written)
        where TMemory : struct, IAmigaGuestMemory
    {
        var target = new APTR(destination.Raw + offset);
        return conversion switch
        {
            (byte)'n' => EvalNumericFormatter.TryWriteDecimal(ref memory, value,
                target, capacity, false, out written),
            (byte)'x' => EvalNumericFormatter.TryWriteHexadecimal(ref memory,
                unchecked((ulong)value), target, capacity, false, false, out written),
            (byte)'o' => EvalNumericFormatter.TryWriteOctal(ref memory,
                unchecked((ulong)value), target, capacity, false, out written),
            _ => Fail(out written),
        };
    }

    private static uint NumberLength(long value, byte conversion)
    {
        uint low = M68kRuntime.SplitInt64(value, out uint high);
        if (conversion == (byte)'n')
        {
            bool negative = (high & 0x80000000) != 0;
            if (negative)
            {
                low = unchecked(0u - low);
                high = unchecked(~high + (low == 0 ? 1u : 0u));
            }
            uint digits = 1;
            while (high != 0 || low >= 10)
            {
                DivideByTen(ref high, ref low);
                digits++;
            }
            return digits + (negative ? 1u : 0u);
        }
        if (conversion == (byte)'x') return HexadecimalLength(high, low);
        uint digitsOctal = 1;
        while (high != 0 || low >= 8)
        {
            low = (low >> 3) | (high << 29);
            high >>= 3;
            digitsOctal++;
        }
        return digitsOctal;
    }

    private static uint HexadecimalLength(uint high, uint low)
    {
        uint value = high != 0 ? high : low;
        uint digits = high != 0 ? 9u : 1u;
        while (value >= 16) { value >>= 4; digits++; }
        return digits;
    }

    private static void DivideByTen(ref uint high, ref uint low)
    {
        uint carry = 0;
        uint quotientHigh = DivideWordByTen(high >> 16, ref carry) << 16;
        quotientHigh |= DivideWordByTen(high & 0xffff, ref carry);
        uint quotientLow = DivideWordByTen(low >> 16, ref carry) << 16;
        quotientLow |= DivideWordByTen(low & 0xffff, ref carry);
        high = quotientHigh;
        low = quotientLow;
    }

    private static uint DivideWordByTen(uint word, ref uint carry)
    {
        uint value = carry * 65536 + word;
        carry = value % 10;
        return value / 10;
    }

    private static bool Fail(out uint written) { written = 0; return false; }
    private static byte ToLowerAscii(byte value) => value is >= (byte)'A' and <= (byte)'Z'
        ? (byte)(value + 32) : value;
    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
        right.Raw < left.Raw + leftLength;
}
