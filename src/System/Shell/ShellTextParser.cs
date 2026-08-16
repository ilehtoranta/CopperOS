using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Mutable cursor over a bounded guest command line.
/// </summary>
public struct ShellTextCursor
{
    public ShellTextCursor(APTR source, uint length)
    {
        Source = source;
        Length = length;
        Position = 0;
    }

    public APTR Source;
    public uint Length;
    public uint Position;
}

/// <summary>
/// Result of reading one decoded Shell token.
/// </summary>
public enum ShellTextTokenResult : int
{
    Token = 0,
    End = 1,
    Unmapped = 2,
    Malformed = 3,
    TooLong = 4,
}

/// <summary>
/// Bounded, allocation-free tokenization for Shell command semantics.
///
/// The parser removes double quotes and decodes the documented Amiga star
/// escapes used by command text.  It reports whether a token used quoting or
/// escaping so command templates can distinguish a literal "FIRST" from the
/// FIRST keyword.
/// </summary>
public static class ShellTextParser
{
    public const uint MaximumSourceLength = 65_535;

    public const uint TokenUsedSyntax = 1;

    public static int NextToken<TPlatform>(
        ref TPlatform platform,
        ref ShellTextCursor cursor,
        APTR destination,
        uint destinationCapacity,
        out uint tokenLength,
        out uint tokenFlags)
        where TPlatform : struct, IShellPlatform
    {
        tokenLength = 0;
        tokenFlags = 0;

        if (cursor.Length > MaximumSourceLength ||
            cursor.Position > cursor.Length ||
            destinationCapacity == 0 ||
            destination.IsNull ||
            (cursor.Length != 0 &&
             cursor.Source.Raw > uint.MaxValue - cursor.Length) ||
            destination.Raw > uint.MaxValue - destinationCapacity ||
            !platform.IsMapped(destination, destinationCapacity))
            return (int)ShellTextTokenResult.Unmapped;

        if (cursor.Length != 0 &&
            (cursor.Source.IsNull || !platform.IsMapped(cursor.Source, cursor.Length)))
            return (int)ShellTextTokenResult.Unmapped;

        while (cursor.Position < cursor.Length &&
            IsWhitespace(Read(ref platform, cursor.Source, cursor.Position)))
            cursor.Position++;

        if (cursor.Position >= cursor.Length ||
            Read(ref platform, cursor.Source, cursor.Position) == ';')
        {
            platform.WriteUInt8(destination, 0, 0);
            return (int)ShellTextTokenResult.End;
        }

        uint output = 0;
        uint flags = 0;
        uint quoted = 0;

        while (cursor.Position < cursor.Length)
        {
            byte value = Read(ref platform, cursor.Source, cursor.Position);
            if (value == 0)
                return (int)ShellTextTokenResult.Malformed;

            if (quoted == 0 && IsWhitespace(value))
            {
                cursor.Position++;
                break;
            }

            if (quoted == 0 && value == ';')
                break;

            cursor.Position++;

            if (value == '"')
            {
                flags |= TokenUsedSyntax;
                quoted ^= 1;
                continue;
            }

            if (value == '*')
            {
                flags |= TokenUsedSyntax;
                if (cursor.Position >= cursor.Length)
                    return (int)ShellTextTokenResult.Malformed;

                value = Read(ref platform, cursor.Source, cursor.Position++);
                if (value == 0)
                    return (int)ShellTextTokenResult.Malformed;

                if (value is (byte)'e' or (byte)'E') value = 0x1B;
                else if (value is (byte)'n' or (byte)'N') value = (byte)'\n';
                else if (value is (byte)'r' or (byte)'R') value = (byte)'\r';
                else if (value is (byte)'t' or (byte)'T') value = (byte)'\t';
            }

            if (output >= destinationCapacity - 1)
                return (int)ShellTextTokenResult.TooLong;

            platform.WriteUInt8(destination, (int)output, value);
            output++;
        }

        if (quoted != 0)
            return (int)ShellTextTokenResult.Malformed;

        platform.WriteUInt8(destination, (int)output, 0);
        tokenLength = output;
        tokenFlags = flags;
        return (int)ShellTextTokenResult.Token;
    }

    /// <summary>
    /// Reads the remaining command line as one decoded final argument. This
    /// implements the bounded portion of a MorphOS <c>/F</c> template: spaces
    /// after the preceding argument are discarded, while spaces within the
    /// remainder are preserved.
    /// </summary>
    public static int ReadFinal<TPlatform>(
        ref TPlatform platform,
        ref ShellTextCursor cursor,
        APTR destination,
        uint destinationCapacity,
        out uint valueLength,
        out uint valueFlags)
        where TPlatform : struct, IShellPlatform
    {
        valueLength = 0;
        valueFlags = 0;

        if (cursor.Length > MaximumSourceLength ||
            cursor.Position > cursor.Length ||
            destinationCapacity == 0 ||
            destination.IsNull ||
            (cursor.Length != 0 &&
             cursor.Source.Raw > uint.MaxValue - cursor.Length) ||
            destination.Raw > uint.MaxValue - destinationCapacity ||
            !platform.IsMapped(destination, destinationCapacity))
            return (int)ShellTextTokenResult.Unmapped;

        if (cursor.Length != 0 &&
            (cursor.Source.IsNull || !platform.IsMapped(cursor.Source, cursor.Length)))
            return (int)ShellTextTokenResult.Unmapped;

        while (cursor.Position < cursor.Length &&
            IsWhitespace(Read(ref platform, cursor.Source, cursor.Position)))
            cursor.Position++;

        if (cursor.Position >= cursor.Length ||
            Read(ref platform, cursor.Source, cursor.Position) == ';')
        {
            platform.WriteUInt8(destination, 0, 0);
            return (int)ShellTextTokenResult.End;
        }

        uint output = 0;
        uint flags = 0;
        uint quoted = 0;
        while (cursor.Position < cursor.Length)
        {
            byte value = Read(ref platform, cursor.Source, cursor.Position);
            if (value == 0)
                return (int)ShellTextTokenResult.Malformed;
            if (quoted == 0 && value == ';')
                break;

            cursor.Position++;
            if (value == '"')
            {
                flags |= TokenUsedSyntax;
                quoted ^= 1;
                continue;
            }
            if (value == '*')
            {
                flags |= TokenUsedSyntax;
                if (cursor.Position >= cursor.Length)
                    return (int)ShellTextTokenResult.Malformed;
                value = Read(ref platform, cursor.Source, cursor.Position++);
                if (value == 0)
                    return (int)ShellTextTokenResult.Malformed;
                if (value is (byte)'e' or (byte)'E') value = 0x1B;
                else if (value is (byte)'n' or (byte)'N') value = (byte)'\n';
                else if (value is (byte)'r' or (byte)'R') value = (byte)'\r';
                else if (value is (byte)'t' or (byte)'T') value = (byte)'\t';
            }

            if (output >= destinationCapacity - 1)
                return (int)ShellTextTokenResult.TooLong;
            platform.WriteUInt8(destination, (int)output, value);
            output++;
        }

        if (quoted != 0)
            return (int)ShellTextTokenResult.Malformed;
        platform.WriteUInt8(destination, (int)output, 0);
        valueLength = output;
        valueFlags = flags;
        return (int)ShellTextTokenResult.Token;
    }

    public static bool EqualsNoCase<TPlatform>(
        ref TPlatform platform,
        APTR value,
        uint length,
        byte first,
        byte second,
        byte third,
        byte fourth,
        byte fifth,
        byte sixth,
        byte seventh,
        byte eighth = 0)
        where TPlatform : struct, IShellPlatform
    {
        if (length == 1) return second == 0 && Match1(ref platform, value, first);
        if (length == 2) return third == 0 && Match2(ref platform, value, first, second);
        if (length == 3) return fourth == 0 && Match3(ref platform, value, first, second, third);
        if (length == 4) return fifth == 0 && Match4(ref platform, value, first, second, third, fourth);
        if (length == 5) return sixth == 0 && Match5(ref platform, value, first, second, third, fourth, fifth);
        if (length == 6) return seventh == 0 && Match6(ref platform, value, first, second, third, fourth, fifth, sixth);
        if (length == 7) return eighth == 0 && Match7(ref platform, value, first, second, third, fourth, fifth, sixth, seventh);
        return length == 8 && Match8(ref platform, value, first, second, third, fourth, fifth, sixth, seventh, eighth);
    }

    public static bool EqualsPacked<TPlatform>(ref TPlatform platform,
        APTR value, uint length, uint firstWord, uint secondWord)
        where TPlatform : struct, IShellPlatform
    {
        if (length == 0 || length > 8) return false;
        for (var index = 0u; index < length; index++)
        {
            var expected = index < 4
                ? (firstWord >> unchecked((int)((3u - index) * 8))) & 0xFFu
                : (secondWord >> unchecked((int)((7u - index) * 8))) & 0xFFu;
            // Guest scalars are big-endian; the byte at the requested address
            // is therefore the most-significant byte of a 32-bit read.  The
            // previous low-byte extraction compared each character with the
            // fourth byte in the window and made every packed lookup fail.
            var actual = platform.ReadUInt32(value, unchecked((int)index)) >> 24;
            if (actual is >= (uint)'A' and <= (uint)'Z') actual += 32;
            if (expected is >= (uint)'A' and <= (uint)'Z') expected += 32;
            if (actual != expected) return false;
        }
        return true;
    }

    private static bool Match1<TPlatform>(ref TPlatform platform, APTR value, byte first)
        where TPlatform : struct, IShellPlatform => EqualByte(ref platform, value, 0, first);
    private static bool Match2<TPlatform>(ref TPlatform platform, APTR value, byte first, byte second)
        where TPlatform : struct, IShellPlatform => Match1(ref platform, value, first) && EqualByte(ref platform, value, 1, second);
    private static bool Match3<TPlatform>(ref TPlatform platform, APTR value, byte first, byte second, byte third)
        where TPlatform : struct, IShellPlatform => Match2(ref platform, value, first, second) && EqualByte(ref platform, value, 2, third);
    private static bool Match4<TPlatform>(ref TPlatform platform, APTR value, byte first, byte second, byte third, byte fourth)
        where TPlatform : struct, IShellPlatform => Match3(ref platform, value, first, second, third) && EqualByte(ref platform, value, 3, fourth);
    private static bool Match5<TPlatform>(ref TPlatform platform, APTR value, byte first, byte second, byte third, byte fourth, byte fifth)
        where TPlatform : struct, IShellPlatform => Match4(ref platform, value, first, second, third, fourth) && EqualByte(ref platform, value, 4, fifth);
    private static bool Match6<TPlatform>(ref TPlatform platform, APTR value, byte first, byte second, byte third, byte fourth, byte fifth, byte sixth)
        where TPlatform : struct, IShellPlatform => Match5(ref platform, value, first, second, third, fourth, fifth) && EqualByte(ref platform, value, 5, sixth);
    private static bool Match7<TPlatform>(ref TPlatform platform, APTR value, byte first, byte second, byte third, byte fourth, byte fifth, byte sixth, byte seventh)
        where TPlatform : struct, IShellPlatform => Match6(ref platform, value, first, second, third, fourth, fifth, sixth) && EqualByte(ref platform, value, 6, seventh);
    private static bool Match8<TPlatform>(ref TPlatform platform, APTR value, byte first, byte second, byte third, byte fourth, byte fifth, byte sixth, byte seventh, byte eighth)
        where TPlatform : struct, IShellPlatform => Match7(ref platform, value, first, second, third, fourth, fifth, sixth, seventh) && EqualByte(ref platform, value, 7, eighth);

    private static bool EqualByte<TPlatform>(ref TPlatform platform, APTR value,
        int offset, byte expected) where TPlatform : struct, IShellPlatform
    {
        var actual = (uint)platform.ReadUInt8(value, offset);
        var expectedValue = (uint)expected;
        if (actual is >= (uint)'A' and <= (uint)'Z') actual += 32;
        if (expectedValue is >= (uint)'A' and <= (uint)'Z') expectedValue += 32;
        return actual == expectedValue;
    }

    private static byte Read<TPlatform>(
        ref TPlatform platform,
        APTR source,
        uint position)
        where TPlatform : struct, IShellPlatform =>
        platform.ReadUInt8(source, (int)position);

    private static bool IsWhitespace(byte value) =>
        value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';

    private static byte Lower(byte value) => value is >= (byte)'A' and <= (byte)'Z'
        ? (byte)(value + 32)
        : value;
}
