using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands;

/// <summary>
/// Writes Eval's numeric output primitives into caller-owned guest storage.
/// This helper does not parse expressions, LFORMAT, command arguments or profiles.
/// </summary>
/// <remarks>
/// No terminator is written. Byte counts include any requested sign, hexadecimal
/// prefix and line feed. The complete supplied buffer span must be non-null,
/// mapped and non-wrapping; a rejected span receives no writes and returns a
/// zero byte count. The caller retains ownership of the buffer and output I/O.
/// </remarks>
public static class EvalNumericFormatter
{
    /// <summary>Space sufficient for every supported value, including a line feed.</summary>
    public const uint MaximumByteCount = 23;

    /// <summary>Writes an unpadded signed 64-bit decimal value.</summary>
    public static bool TryWriteDecimal<TMemory>(
        ref TMemory memory,
        long value,
        APTR destination,
        uint capacity,
        bool appendLineFeed,
        out uint byteCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        uint low = M68kRuntime.SplitInt64(value, out uint high);
        bool negative = (high & 0x80000000) != 0;
        if (negative)
        {
            // Two's-complement magnitude, including long.MinValue. The target
            // uses integer lanes rather than a managed 64-bit arithmetic helper.
            low = unchecked(0u - low);
            high = unchecked(~high + (low == 0 ? 1u : 0u));
        }
        return TryWrite(ref memory, high, low, 10, negative, false,
            destination, capacity, appendLineFeed, out byteCount);
    }

    /// <summary>
    /// Writes an unpadded unsigned 64-bit pattern in lowercase hexadecimal,
    /// optionally preceded by the two bytes 0x.
    /// </summary>
    public static bool TryWriteHexadecimal<TMemory>(
        ref TMemory memory,
        ulong bitPattern,
        APTR destination,
        uint capacity,
        bool includePrefix,
        bool appendLineFeed,
        out uint byteCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        uint low = M68kRuntime.SplitUInt64(bitPattern, out uint high);
        return TryWrite(ref memory, high, low, 16, false, includePrefix,
            destination, capacity, appendLineFeed, out byteCount);
    }

    /// <summary>Writes an unpadded unsigned 64-bit pattern in octal, with no prefix.</summary>
    public static bool TryWriteOctal<TMemory>(
        ref TMemory memory,
        ulong bitPattern,
        APTR destination,
        uint capacity,
        bool appendLineFeed,
        out uint byteCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        uint low = M68kRuntime.SplitUInt64(bitPattern, out uint high);
        return TryWrite(ref memory, high, low, 8, false, false,
            destination, capacity, appendLineFeed, out byteCount);
    }

    private static bool TryWrite<TMemory>(
        ref TMemory memory,
        uint high,
        uint low,
        uint radix,
        bool negative,
        bool includeHexPrefix,
        APTR destination,
        uint capacity,
        bool appendLineFeed,
        out uint byteCount)
        where TMemory : struct, IAmigaGuestMemory
    {
        byteCount = 0;
        uint digitCount = 1;
        uint remainingHigh = high;
        uint remainingLow = low;
        while (remainingHigh != 0 || remainingLow >= radix)
        {
            Divide(ref remainingHigh, ref remainingLow, radix);
            digitCount++;
        }

        uint prefixLength = negative ? 1u : includeHexPrefix ? 2u : 0u;
        uint required = prefixLength + digitCount + (appendLineFeed ? 1u : 0u);
        if (destination.IsNull || capacity < required ||
            destination.Raw > uint.MaxValue - (capacity - 1) ||
            !memory.IsMapped(destination, capacity))
            return false;

        if (negative)
            memory.WriteUInt8(destination, 0, (byte)'-');
        else if (includeHexPrefix)
        {
            memory.WriteUInt8(destination, 0, (byte)'0');
            memory.WriteUInt8(destination, 1, (byte)'x');
        }

        // Fill the already-sized digit region backwards; no scratch allocation
        // or reads from the caller's buffer are needed.
        uint digitOffset = prefixLength + digitCount;
        do
        {
            uint digit = Divide(ref high, ref low, radix);
            digitOffset--;
            memory.WriteUInt8(destination, (int)digitOffset,
                (byte)(digit < 10 ? '0' + digit : 'a' + digit - 10));
        }
        while (high != 0 || low != 0);

        if (appendLineFeed)
            memory.WriteUInt8(destination, (int)(required - 1), (byte)'\n');

        byteCount = required;
        return true;
    }

    // Only the public formatter wrappers select a radix: 8, 10 or 16.
    // Binary bases extract a digit and shift the two words. Decimal appends
    // 16 bits to each remainder below 10, keeping those steps below 20 bits.
    // Literal divisors need no unreachable division-by-zero failure paths.
    private static uint Divide(ref uint high, ref uint low, uint radix)
    {
        if (radix == 16)
        {
            uint digit = low & 15;
            low = (low >> 4) | (high << 28);
            high >>= 4;
            return digit;
        }
        if (radix == 8)
        {
            uint digit = low & 7;
            low = (low >> 3) | (high << 29);
            high >>= 3;
            return digit;
        }

        uint quotientHigh = high / 10;
        uint middle = ((high % 10) << 16) | (low >> 16);
        uint quotientMiddle = middle / 10;
        uint bottom = ((middle % 10) << 16) | (low & 0xffff);
        high = quotientHigh;
        low = (quotientMiddle << 16) | (bottom / 10);
        return bottom % 10;
    }
}
