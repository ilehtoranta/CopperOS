using Amiga;
using CopperOS.Commands;

namespace CopperOS.Commands.Tests;

public sealed class EvalNumericFormatterTests
{
    private const byte Guard = 0xa5;

    [Theory]
    [InlineData(0L, false, "0")]
    [InlineData(0L, true, "0\n")]
    [InlineData(1L, false, "1")]
    [InlineData(-1L, false, "-1")]
    [InlineData(9L, false, "9")]
    [InlineData(10L, false, "10")]
    [InlineData(-10L, true, "-10\n")]
    [InlineData(100L, false, "100")]
    [InlineData(2147483648L, false, "2147483648")]
    [InlineData(4294967295L, false, "4294967295")]
    [InlineData(4294967296L, false, "4294967296")]
    [InlineData(9007199254740993L, false, "9007199254740993")]
    [InlineData(long.MaxValue, false, "9223372036854775807")]
    [InlineData(long.MinValue, false, "-9223372036854775808")]
    [InlineData(long.MinValue, true, "-9223372036854775808\n")]
    public void Decimal_writes_exact_signed_bytes(long value, bool lineFeed,
        string expected)
    {
        TestMemory memory = new(0x1000, 64);
        APTR destination = new(0x1001);

        bool result = EvalNumericFormatter.TryWriteDecimal(ref memory, value,
            destination, EvalNumericFormatter.MaximumByteCount, lineFeed,
            out uint byteCount);

        Assert.True(result);
        AssertOutput(memory, destination, expected, byteCount);
    }

    [Theory]
    [InlineData(0UL, false, false, "0")]
    [InlineData(0UL, false, true, "0\n")]
    [InlineData(0UL, true, false, "0x0")]
    [InlineData(0UL, true, true, "0x0\n")]
    [InlineData(15UL, false, false, "f")]
    [InlineData(16UL, false, false, "10")]
    [InlineData(0x0123456789abcdefUL, false, false, "123456789abcdef")]
    [InlineData(0xabcdef1234567890UL, true, false, "0xabcdef1234567890")]
    [InlineData(0x8000000000000000UL, false, false, "8000000000000000")]
    [InlineData(ulong.MaxValue, false, false, "ffffffffffffffff")]
    [InlineData(ulong.MaxValue, true, true, "0xffffffffffffffff\n")]
    public void Hexadecimal_writes_lowercase_unsigned_patterns(ulong value,
        bool prefix, bool lineFeed, string expected)
    {
        TestMemory memory = new(0x1000, 64);
        APTR destination = new(0x1001);

        bool result = EvalNumericFormatter.TryWriteHexadecimal(ref memory, value,
            destination, EvalNumericFormatter.MaximumByteCount, prefix, lineFeed,
            out uint byteCount);

        Assert.True(result);
        AssertOutput(memory, destination, expected, byteCount);
    }

    [Theory]
    [InlineData(0UL, false, "0")]
    [InlineData(0UL, true, "0\n")]
    [InlineData(7UL, false, "7")]
    [InlineData(8UL, false, "10")]
    [InlineData(63UL, false, "77")]
    [InlineData(64UL, false, "100")]
    [InlineData(4294967295UL, false, "37777777777")]
    [InlineData(0x8000000000000000UL, false, "1000000000000000000000")]
    [InlineData(ulong.MaxValue, false, "1777777777777777777777")]
    [InlineData(ulong.MaxValue, true, "1777777777777777777777\n")]
    public void Octal_writes_unsigned_patterns_without_a_prefix(ulong value,
        bool lineFeed, string expected)
    {
        TestMemory memory = new(0x1000, 64);
        APTR destination = new(0x1001);

        bool result = EvalNumericFormatter.TryWriteOctal(ref memory, value,
            destination, EvalNumericFormatter.MaximumByteCount, lineFeed,
            out uint byteCount);

        Assert.True(result);
        AssertOutput(memory, destination, expected, byteCount);
    }

    [Theory]
    [InlineData(NumberFormat.Decimal)]
    [InlineData(NumberFormat.Hexadecimal)]
    [InlineData(NumberFormat.Octal)]
    public void Exact_capacity_includes_decorations_but_needs_no_terminator(
        NumberFormat format)
    {
        string expected = MaximumOutput(format);
        TestMemory memory = new(0x1000, expected.Length + 2);
        APTR destination = new(0x1001);

        bool result = WriteMaximum(format, ref memory, destination,
            (uint)expected.Length, out uint byteCount);

        Assert.True(result);
        AssertOutput(memory, destination, expected, byteCount);
    }

    [Theory]
    [InlineData(NumberFormat.Decimal)]
    [InlineData(NumberFormat.Hexadecimal)]
    [InlineData(NumberFormat.Octal)]
    public void One_byte_short_capacity_writes_nothing(NumberFormat format)
    {
        TestMemory memory = new(0x1000, 64);

        bool result = WriteMaximum(format, ref memory, new APTR(0x1001),
            (uint)MaximumOutput(format).Length - 1, out uint byteCount);

        AssertRejected(memory, result, byteCount, expectedMappingChecks: 0);
    }

    [Theory]
    [InlineData(NumberFormat.Decimal)]
    [InlineData(NumberFormat.Hexadecimal)]
    [InlineData(NumberFormat.Octal)]
    public void Zero_capacity_writes_nothing(NumberFormat format)
    {
        TestMemory memory = new(0x1000, 64);

        bool result = WriteMaximum(format, ref memory, new APTR(0x1001), 0,
            out uint byteCount);

        AssertRejected(memory, result, byteCount, expectedMappingChecks: 0);
    }

    [Theory]
    [InlineData(NumberFormat.Decimal)]
    [InlineData(NumberFormat.Hexadecimal)]
    [InlineData(NumberFormat.Octal)]
    public void Null_destination_is_rejected_before_mapping(NumberFormat format)
    {
        TestMemory memory = new(0, 64);

        bool result = WriteMaximum(format, ref memory, APTR.Null, 64,
            out uint byteCount);

        AssertRejected(memory, result, byteCount, expectedMappingChecks: 0);
    }

    [Theory]
    [InlineData(NumberFormat.Decimal)]
    [InlineData(NumberFormat.Hexadecimal)]
    [InlineData(NumberFormat.Octal)]
    public void Unmapped_buffer_writes_nothing(NumberFormat format)
    {
        TestMemory memory = new(0x1000, 64) { RejectMapping = true };

        bool result = WriteMaximum(format, ref memory, new APTR(0x1001),
            EvalNumericFormatter.MaximumByteCount, out uint byteCount);

        AssertRejected(memory, result, byteCount, expectedMappingChecks: 1);
    }

    [Theory]
    [InlineData(0xfffffff0u, 64u)]
    [InlineData(2u, uint.MaxValue)]
    [InlineData(uint.MaxValue, 2u)]
    public void Wrapping_capacity_is_rejected_even_when_the_number_would_fit(
        uint address, uint capacity)
    {
        TestMemory memory = new(0x1000, 64) { AssumeMapped = true };

        bool result = EvalNumericFormatter.TryWriteDecimal(ref memory, 0,
            new APTR(address), capacity, false, out uint byteCount);

        AssertRejected(memory, result, byteCount, expectedMappingChecks: 0);
    }

    [Fact]
    public void Complete_declared_capacity_must_be_mapped()
    {
        TestMemory memory = new(0x1000, 4);

        bool result = EvalNumericFormatter.TryWriteDecimal(ref memory, 0,
            new APTR(0x1000), 5, false, out uint byteCount);

        AssertRejected(memory, result, byteCount, expectedMappingChecks: 1);
    }

    [Fact]
    public void Final_guest_address_is_usable_without_an_implicit_NUL()
    {
        TestMemory memory = new(uint.MaxValue, 1);
        APTR destination = new(uint.MaxValue);

        bool result = EvalNumericFormatter.TryWriteDecimal(ref memory, 0,
            destination, 1, false, out uint byteCount);

        Assert.True(result);
        AssertOutput(memory, destination, "0", byteCount);
    }

    [Fact]
    public void Line_feed_can_occupy_the_final_guest_address()
    {
        TestMemory memory = new(uint.MaxValue - 20, 21);
        APTR destination = new(uint.MaxValue - 20);

        bool result = EvalNumericFormatter.TryWriteDecimal(ref memory,
            long.MinValue, destination, 21, true, out uint byteCount);

        Assert.True(result);
        AssertOutput(memory, destination, "-9223372036854775808\n", byteCount);
    }

    [Theory]
    [InlineData(NumberFormat.Decimal)]
    [InlineData(NumberFormat.Hexadecimal)]
    [InlineData(NumberFormat.Octal)]
    public void Mixed_high_and_low_words_match_independent_integer_formatting(NumberFormat format)
    {
        // Exercise carries between both 16-bit divisions and both 32-bit words.
        // The expected bytes use the host's integer formatter, not Divide or
        // any other part of the production conversion algorithm.
        uint sequence = 0x6d2b79f5;
        for (int sample = 0; sample < 256; sample++)
        {
            uint high = NextBits(ref sequence);
            uint low = NextBits(ref sequence);
            ulong value = ((ulong)high << 32) | low;
            bool lineFeed = (sample & 1) != 0;
            bool prefix = (sample & 2) != 0;
            TestMemory memory = new(0x1000, 64);
            APTR destination = new(0x1001);
            uint count;
            bool success;
            string expected;
            if (format == NumberFormat.Decimal)
            {
                success = EvalNumericFormatter.TryWriteDecimal(ref memory,
                    unchecked((long)value), destination, 23, lineFeed, out count);
                expected = unchecked((long)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (format == NumberFormat.Hexadecimal)
            {
                success = EvalNumericFormatter.TryWriteHexadecimal(ref memory,
                    value, destination, 23, prefix, lineFeed, out count);
                expected = (prefix ? "0x" : "") + value.ToString("x", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                success = EvalNumericFormatter.TryWriteOctal(ref memory,
                    value, destination, 23, lineFeed, out count);
                expected = Convert.ToString(unchecked((long)value), 8);
            }
            if (lineFeed) expected += "\n";
            Assert.True(success);
            AssertOutput(memory, destination, expected, count);
        }
    }

    private static uint NextBits(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    [Fact]
    public void Formatting_allocates_no_managed_scratch_storage()
    {
        TestMemory memory = new(0x1000, 64);
        APTR destination = new(0x1001);
        WriteMaximum(NumberFormat.Decimal, ref memory, destination, 23, out _);
        WriteMaximum(NumberFormat.Hexadecimal, ref memory, destination, 23, out _);
        WriteMaximum(NumberFormat.Octal, ref memory, destination, 23, out _);

        bool allSucceeded = true;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 256; i++)
        {
            allSucceeded &= WriteMaximum(NumberFormat.Decimal, ref memory,
                destination, 23, out _);
            allSucceeded &= WriteMaximum(NumberFormat.Hexadecimal, ref memory,
                destination, 23, out _);
            allSucceeded &= WriteMaximum(NumberFormat.Octal, ref memory,
                destination, 23, out _);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allSucceeded);
        Assert.Equal(0L, allocated);
    }

    private static void AssertOutput(TestMemory memory, APTR destination,
        string expected, uint byteCount)
    {
        Assert.Equal((uint)expected.Length, byteCount);
        Assert.Equal(expected.Length, memory.WriteCount);
        Assert.Equal(1, memory.MappingCheckCount);
        int start = checked((int)(destination.Raw - memory.BaseAddress));
        for (int i = 0; i < memory.Bytes.Length; i++)
        {
            byte expectedByte = i >= start && i < start + expected.Length
                ? (byte)expected[i - start]
                : Guard;
            Assert.Equal(expectedByte, memory.Bytes[i]);
        }
    }

    private static void AssertRejected(TestMemory memory, bool result,
        uint byteCount, int expectedMappingChecks)
    {
        Assert.False(result);
        Assert.Equal(0u, byteCount);
        Assert.Equal(0, memory.WriteCount);
        Assert.Equal(expectedMappingChecks, memory.MappingCheckCount);
        Assert.All(memory.Bytes, value => Assert.Equal(Guard, value));
    }

    private static string MaximumOutput(NumberFormat format) => format switch
    {
        NumberFormat.Decimal => "-9223372036854775808\n",
        NumberFormat.Hexadecimal => "0xffffffffffffffff\n",
        NumberFormat.Octal => "1777777777777777777777\n",
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    private static bool WriteMaximum(NumberFormat format, ref TestMemory memory,
        APTR destination, uint capacity, out uint byteCount)
    {
        switch (format)
        {
            case NumberFormat.Decimal:
                return EvalNumericFormatter.TryWriteDecimal(ref memory,
                    long.MinValue, destination, capacity, true, out byteCount);
            case NumberFormat.Hexadecimal:
                return EvalNumericFormatter.TryWriteHexadecimal(ref memory,
                    ulong.MaxValue, destination, capacity, true, true,
                    out byteCount);
            case NumberFormat.Octal:
                return EvalNumericFormatter.TryWriteOctal(ref memory,
                    ulong.MaxValue, destination, capacity, true, out byteCount);
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    public enum NumberFormat
    {
        Decimal,
        Hexadecimal,
        Octal,
    }

    private struct TestMemory : IAmigaGuestMemory
    {
        public TestMemory(uint baseAddress, int size)
        {
            BaseAddress = baseAddress;
            Bytes = new byte[size];
            Array.Fill(Bytes, Guard);
            WriteCount = 0;
            MappingCheckCount = 0;
            RejectMapping = false;
            AssumeMapped = false;
        }

        public readonly uint BaseAddress;
        public readonly byte[] Bytes;
        public int WriteCount;
        public int MappingCheckCount;
        public bool RejectMapping;
        public bool AssumeMapped;

        public bool IsMapped(APTR address, uint byteSize)
        {
            MappingCheckCount++;
            if (RejectMapping)
                return false;
            if (AssumeMapped)
                return true;
            if (address.Raw < BaseAddress)
                return false;
            uint offset = address.Raw - BaseAddress;
            return offset <= (uint)Bytes.Length &&
                byteSize <= (uint)Bytes.Length - offset;
        }

        public void WriteUInt8(APTR address, int offset, byte value)
        {
            if (offset < 0)
                throw new InvalidOperationException("Negative output offset.");
            ulong target = (ulong)address.Raw + (uint)offset;
            if (target > uint.MaxValue || target < BaseAddress)
                throw new InvalidOperationException("Guest address overflow.");
            Bytes[checked((int)(target - BaseAddress))] = value;
            WriteCount++;
        }

        public byte ReadUInt8(APTR address, int offset = 0) =>
            throw new NotSupportedException();
        public ushort ReadUInt16(APTR address, int offset = 0) =>
            throw new NotSupportedException();
        public uint ReadUInt32(APTR address, int offset = 0) =>
            throw new NotSupportedException();
        public void WriteUInt16(APTR address, int offset, ushort value) =>
            throw new NotSupportedException();
        public void WriteUInt32(APTR address, int offset, uint value) =>
            throw new NotSupportedException();
        public void Clear(APTR address, uint byteCount) =>
            throw new NotSupportedException();
        public void Copy(APTR source, APTR destination, uint byteCount) =>
            throw new NotSupportedException();
    }
}
