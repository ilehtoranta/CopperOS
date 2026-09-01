using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands;

/// <summary>
/// Formats the measured Workbench 3.1 numeric LFORMAT subset. A bare percent-X
/// or percent-o conversion emits one low uppercase digit; a following decimal
/// digit selects that many low base-16/base-8 digits. Percent-N is decimal.
/// </summary>
public static class EvalClassicLFormatFormatter
{
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

        uint required = 0;
        for (var index = 0u; index < formatLength; index++)
        {
            var current = memory.ReadUInt8(format, (int)index);
            if (current != (byte)'%' || index + 1 == formatLength)
            {
                required++;
                continue;
            }
            var conversion = memory.ReadUInt8(format, (int)++index);
            if (conversion is (byte)'x' or (byte)'X')
                required += ReadWidth(ref memory, format, formatLength, ref index);
            else if (conversion == (byte)'o')
                required += ReadWidth(ref memory, format, formatLength, ref index);
            else if (conversion == (byte)'n')
                required += DecimalLength(value);
            else
            {
                status = EvalLFormatStatus.UnsupportedConversion;
                return false;
            }
        }
        if (required > capacity)
        {
            status = EvalLFormatStatus.InsufficientCapacity;
            return false;
        }

        uint output = 0;
        for (var index = 0u; index < formatLength; index++)
        {
            var current = memory.ReadUInt8(format, (int)index);
            if (current != (byte)'%')
            {
                memory.WriteUInt8(destination, (int)output++, current);
                continue;
            }
            if (index + 1 == formatLength)
            {
                memory.WriteUInt8(destination, (int)output++, current);
                continue;
            }
            var conversion = memory.ReadUInt8(format, (int)++index);
            if (conversion == (byte)'n')
            {
                if (!EvalNumericFormatter.TryWriteDecimal(ref memory, value,
                        APTR.FromPointer(destination.Raw + output), capacity - output,
                        false, out var written))
                {
                    byteCount = 0;
                    status = EvalLFormatStatus.InvalidSpan;
                    return false;
                }
                output += written;
            }
            else
            {
                var width = ReadWidth(ref memory, format, formatLength, ref index);
                WriteLowDigits(ref memory, destination, ref output, value, width,
                    conversion == (byte)'o' ? 3u : 4u);
            }
        }
        byteCount = output;
        status = EvalLFormatStatus.Success;
        return true;
    }

    private static uint ReadWidth<TMemory>(ref TMemory memory, APTR format,
        uint length, ref uint index) where TMemory : struct, IAmigaGuestMemory
    {
        if (index + 1 == length) return 1;
        var next = memory.ReadUInt8(format, (int)(index + 1));
        if (next is < (byte)'1' or > (byte)'8') return 1;
        index++;
        return (uint)(next - '0');
    }

    private static void WriteLowDigits<TMemory>(ref TMemory memory, APTR destination,
        ref uint output, long value, uint width, uint bits)
        where TMemory : struct, IAmigaGuestMemory
    {
        var low = M68kRuntime.SplitInt64(value, out _);
        var mask = (1u << (int)bits) - 1;
        for (var digit = width; digit != 0; digit--)
        {
            var valueDigit = (low >> (int)((digit - 1) * bits)) & mask;
            memory.WriteUInt8(destination, (int)output++, (byte)(valueDigit < 10
                ? (byte)'0' + valueDigit : (byte)'A' + valueDigit - 10));
        }
    }

    private static uint DecimalLength(long value)
    {
        uint low = M68kRuntime.SplitInt64(value, out uint high);
        var negative = (high & 0x80000000) != 0;
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

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
        right.Raw < left.Raw + leftLength;
}
