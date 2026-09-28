using Amiga;

namespace CopperOS.Shell;

internal struct ShellScriptKeyDirectiveState
{
	public byte OpeningBracket { get; set; }
	public byte ClosingBracket { get; set; }
	public byte DollarMarker { get; set; }
	public byte DotMarker { get; set; }
}

/// <summary>
/// Owns the fixed guest encoding appended after an Execute .KEY template.
/// Shell logic reads and updates the named directive-state fields.
/// </summary>
internal static class ShellScriptKeyDirectiveStateCodec
{
	public const uint EncodedSize = 6;
	private const uint Magic0Offset = 0;
	private const uint Magic1Offset = 1;
	private const uint OpeningBracketOffset = 2;
	private const uint ClosingBracketOffset = 3;
	private const uint DollarMarkerOffset = 4;
	private const uint DotMarkerOffset = 5;
	private const byte Magic0 = 0xC3;
	private const byte Magic1 = 0x58;

	public static bool TryInitialize<TPlatform>(ref TPlatform platform,
		APTR template, uint capacity, uint templateLength)
		where TPlatform : struct, IShellPlatform
	{
		if (!TryLocate(ref platform, template, capacity, templateLength,
				out var address)) return false;
		platform.WriteUInt8(address, (int)Magic0Offset, Magic0);
		platform.WriteUInt8(address, (int)Magic1Offset, Magic1);
		WriteState(ref platform, address, new ShellScriptKeyDirectiveState
		{
			OpeningBracket = (byte)'<',
			ClosingBracket = (byte)'>',
			DollarMarker = (byte)'$',
			DotMarker = (byte)'.',
		});
		return true;
	}

	public static bool TryRead<TPlatform>(ref TPlatform platform,
		APTR template, uint capacity, uint templateLength,
		out ShellScriptKeyDirectiveState state)
		where TPlatform : struct, IShellPlatform
	{
		state = default;
		if (!TryLocate(ref platform, template, capacity, templateLength,
				out var address) || !HasMagic(ref platform, address)) return false;
		state = new ShellScriptKeyDirectiveState
		{
			OpeningBracket = platform.ReadUInt8(address,
				(int)OpeningBracketOffset),
			ClosingBracket = platform.ReadUInt8(address,
				(int)ClosingBracketOffset),
			DollarMarker = platform.ReadUInt8(address,
				(int)DollarMarkerOffset),
			DotMarker = platform.ReadUInt8(address, (int)DotMarkerOffset),
		};
		return true;
	}

	public static bool TryWrite<TPlatform>(ref TPlatform platform,
		APTR template, uint capacity, uint templateLength,
		in ShellScriptKeyDirectiveState state)
		where TPlatform : struct, IShellPlatform
	{
		if (!TryLocate(ref platform, template, capacity, templateLength,
				out var address) || !HasMagic(ref platform, address)) return false;
		WriteState(ref platform, address, in state);
		return true;
	}

	private static void WriteState<TPlatform>(ref TPlatform platform,
		APTR address, in ShellScriptKeyDirectiveState state)
		where TPlatform : struct, IShellPlatform
	{
		platform.WriteUInt8(address, (int)OpeningBracketOffset,
			state.OpeningBracket);
		platform.WriteUInt8(address, (int)ClosingBracketOffset,
			state.ClosingBracket);
		platform.WriteUInt8(address, (int)DollarMarkerOffset,
			state.DollarMarker);
		platform.WriteUInt8(address, (int)DotMarkerOffset, state.DotMarker);
	}

	private static bool HasMagic<TPlatform>(ref TPlatform platform,
		APTR address) where TPlatform : struct, IShellPlatform =>
		platform.ReadUInt8(address, (int)Magic0Offset) == Magic0 &&
		platform.ReadUInt8(address, (int)Magic1Offset) == Magic1;

	private static bool TryLocate<TPlatform>(ref TPlatform platform,
		APTR template, uint capacity, uint templateLength, out APTR address)
		where TPlatform : struct, IShellPlatform
	{
		address = APTR.Null;
		if (capacity < EncodedSize + 1 || templateLength == 0 ||
			templateLength > capacity - EncodedSize - 1 || template.IsNull ||
			template.Raw > uint.MaxValue - capacity ||
			!platform.IsMapped(template, capacity)) return false;
		var position = templateLength + 1;
		address = APTR.FromPointer(template.Raw + position);
		return platform.IsMapped(address, EncodedSize);
	}
}

internal struct ShellScriptDefaultEntry
{
	public ushort NameLength { get; set; }
	public ushort ValueLength { get; set; }
	public APTR Name { get; set; }
	public APTR Value { get; set; }
	public uint EncodedLength => ShellScriptDefaultEntryCodec.HeaderSize +
		NameLength + ValueLength;
	public bool IsTerminator => NameLength == 0 && ValueLength == 0;
}

/// <summary>
/// Owns the variable-length guest encoding used for Execute .DEF records.
/// Algorithms consume named fields; the two big-endian length offsets stay
/// confined to this codec.
/// </summary>
internal static class ShellScriptDefaultEntryCodec
{
	public const uint HeaderSize = 4;
	private const uint NameLengthOffset = 0;
	private const uint ValueLengthOffset = 2;

	public static bool TryRead<TPlatform>(ref TPlatform platform, APTR storage,
		uint capacity, uint position, out ShellScriptDefaultEntry entry)
		where TPlatform : struct, IShellPlatform
	{
		entry = default;
		if (storage.IsNull || position > capacity ||
			capacity - position < HeaderSize ||
			storage.Raw > uint.MaxValue - position - HeaderSize)
			return false;
		var header = APTR.FromPointer(storage.Raw + position);
		if (!platform.IsMapped(header, HeaderSize)) return false;
		var nameLength = ReadUInt16(ref platform, header, NameLengthOffset);
		var valueLength = ReadUInt16(ref platform, header, ValueLengthOffset);
		var encodedLength = HeaderSize + nameLength + valueLength;
		if (encodedLength > capacity - position ||
			storage.Raw > uint.MaxValue - position - encodedLength ||
			!platform.IsMapped(header, encodedLength))
			return false;
		var name = APTR.FromPointer(header.Raw + HeaderSize);
		entry = new ShellScriptDefaultEntry
		{
			NameLength = nameLength,
			ValueLength = valueLength,
			Name = name,
			Value = APTR.FromPointer(name.Raw + nameLength),
		};
		return true;
	}

	public static bool TryWrite<TPlatform>(ref TPlatform platform, APTR storage,
		uint capacity, uint position, in ShellScriptDefaultEntry entry)
		where TPlatform : struct, IShellPlatform
	{
		var encodedLength = entry.EncodedLength;
		if (storage.IsNull || position > capacity ||
			encodedLength > capacity - position ||
			storage.Raw > uint.MaxValue - position - encodedLength ||
			!platform.IsMapped(APTR.FromPointer(storage.Raw + position),
				encodedLength) ||
			(entry.NameLength != 0 && !IsMapped(ref platform, entry.Name,
				entry.NameLength)) ||
			(entry.ValueLength != 0 && !IsMapped(ref platform, entry.Value,
				entry.ValueLength)))
			return false;
		var header = APTR.FromPointer(storage.Raw + position);
		WriteUInt16(ref platform, header, NameLengthOffset, entry.NameLength);
		WriteUInt16(ref platform, header, ValueLengthOffset, entry.ValueLength);
		if (entry.NameLength != 0)
			platform.Copy(entry.Name, APTR.FromPointer(header.Raw + HeaderSize),
				entry.NameLength);
		if (entry.ValueLength != 0)
			platform.Copy(entry.Value, APTR.FromPointer(header.Raw + HeaderSize +
				entry.NameLength), entry.ValueLength);
		return true;
	}

	private static bool IsMapped<TPlatform>(ref TPlatform platform, APTR value,
		uint length) where TPlatform : struct, IShellPlatform =>
		value.IsNotNull && value.Raw <= uint.MaxValue - length &&
		platform.IsMapped(value, length);

	private static ushort ReadUInt16<TPlatform>(ref TPlatform platform,
		APTR value, uint fieldOffset) where TPlatform : struct, IShellPlatform =>
		(ushort)((platform.ReadUInt8(value, (int)fieldOffset) << 8) |
			platform.ReadUInt8(value, (int)(fieldOffset + 1)));

	private static void WriteUInt16<TPlatform>(ref TPlatform platform,
		APTR value, uint fieldOffset, ushort number)
		where TPlatform : struct, IShellPlatform
	{
		platform.WriteUInt8(value, (int)fieldOffset, (byte)(number >> 8));
		platform.WriteUInt8(value, (int)(fieldOffset + 1), (byte)number);
	}
}

/// <summary>
/// Expands <c>&lt;name&gt;</c> references from a resident Execute <c>.KEY</c>
/// template.  The caller supplies all buffers; DOS remains the authority for
/// argument parsing through ReadArgs.
/// </summary>
public static class ShellScriptKeyExpansion
{
    public const uint TemplateBufferCapacity = 4096;

    public static bool TryInitializeDirectiveState<TPlatform>(
        ref TPlatform platform, APTR template, uint templateLength)
        where TPlatform : struct, IShellPlatform
        => ShellScriptKeyDirectiveStateCodec.TryInitialize(ref platform,
            template, TemplateBufferCapacity, templateLength);

    public static bool TrySetBracket<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, uint opening, byte value)
        where TPlatform : struct, IShellPlatform
    {
        if (opening > 1 || value == 0 ||
            !ShellScriptKeyDirectiveStateCodec.TryRead(ref platform, template,
                TemplateBufferCapacity, templateLength, out var state))
            return false;
        if (opening == 0) state.OpeningBracket = value;
        else state.ClosingBracket = value;
        return ShellScriptKeyDirectiveStateCodec.TryWrite(ref platform,
            template, TemplateBufferCapacity, templateLength, in state);
    }

    public static bool TrySetDollar<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, byte value)
        where TPlatform : struct, IShellPlatform
    {
        if (value == 0 ||
            !ShellScriptKeyDirectiveStateCodec.TryRead(ref platform, template,
                TemplateBufferCapacity, templateLength, out var state))
            return false;
        state.DollarMarker = value;
        return ShellScriptKeyDirectiveStateCodec.TryWrite(ref platform,
            template, TemplateBufferCapacity, templateLength, in state);
    }

    public static bool TrySetDot<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, byte value)
        where TPlatform : struct, IShellPlatform
    {
        if (value == 0 ||
            !ShellScriptKeyDirectiveStateCodec.TryRead(ref platform, template,
                TemplateBufferCapacity, templateLength, out var state))
            return false;
        state.DotMarker = value;
        return ShellScriptKeyDirectiveStateCodec.TryWrite(ref platform,
            template, TemplateBufferCapacity, templateLength, in state);
    }

    public static bool TryGetDot<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, out byte dot)
        where TPlatform : struct, IShellPlatform
    {
        dot = 0;
        if (!ShellScriptKeyDirectiveStateCodec.TryRead(ref platform, template,
                TemplateBufferCapacity, templateLength, out var state))
            return false;
        dot = state.DotMarker;
        return dot != 0;
    }

    /// <summary>Appends a script-wide .DEF value to template-owned storage.</summary>
    public static bool TryDefineDefault<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, APTR name, uint nameLength,
        APTR value, uint valueLength)
        where TPlatform : struct, IShellPlatform
    {
        if (templateLength == 0 || nameLength == 0 || nameLength > 255 ||
            valueLength > 65_535 || templateLength >= TemplateBufferCapacity ||
            !ValidRange(ref platform, template, TemplateBufferCapacity) ||
            !ValidRange(ref platform, name, nameLength) ||
            (valueLength != 0 && !ValidRange(ref platform, value, valueLength)))
            return false;
        // Workbench 3.1 accepts defaults for undeclared names but cannot expose
        // them through ReadArgs, so retain no record. A second default for a
        // formal name is also accepted and leaves the first value in force.
        if (!TryFindTemplateEntry(ref platform, template, templateLength, name,
                nameLength, out _))
            return true;
        if (TryFindDefault(ref platform, template, templateLength, name,
                nameLength, out _, out _))
            return true;
        var position = DefaultStart(templateLength);
        while (position <= TemplateBufferCapacity -
            ShellScriptDefaultEntryCodec.HeaderSize)
        {
            if (!ShellScriptDefaultEntryCodec.TryRead(ref platform, template,
                    TemplateBufferCapacity, position, out var stored))
                return false;
            if (stored.IsTerminator) break;
            position += stored.EncodedLength;
        }
        var addition = new ShellScriptDefaultEntry
        {
            NameLength = (ushort)nameLength,
            ValueLength = (ushort)valueLength,
            Name = name,
            Value = value,
        };
        var required = addition.EncodedLength;
        if (position > TemplateBufferCapacity - required) return false;
        if (!ShellScriptDefaultEntryCodec.TryWrite(ref platform, template,
                TemplateBufferCapacity, position, in addition)) return false;
        var terminator = position + required;
        if (terminator + ShellScriptDefaultEntryCodec.HeaderSize <=
            TemplateBufferCapacity)
        {
            var empty = default(ShellScriptDefaultEntry);
            if (!ShellScriptDefaultEntryCodec.TryWrite(ref platform, template,
                    TemplateBufferCapacity, terminator, in empty)) return false;
        }
        return true;
    }
    public static bool TryExpand<TPlatform>(
        ref TPlatform platform,
        APTR source,
        uint sourceLength,
        APTR arguments,
        uint argumentLength,
        APTR template,
        uint templateLength,
        APTR resultArray,
        uint resultBytes,
        APTR destination,
        uint destinationCapacity,
        out uint destinationLength)
        where TPlatform : struct, IShellPlatform =>
        TryExpand(ref platform, source, sourceLength, arguments,
            argumentLength, template, templateLength, resultArray,
            resultBytes, 0, destination, destinationCapacity,
            out destinationLength);

    public static bool TryExpand<TPlatform>(
        ref TPlatform platform,
        APTR source,
        uint sourceLength,
        APTR arguments,
        uint argumentLength,
        APTR template,
        uint templateLength,
        APTR resultArray,
        uint resultBytes,
        uint shellNumber,
        APTR destination,
        uint destinationCapacity,
        out uint destinationLength)
        where TPlatform : struct, IShellPlatform
    {
        destinationLength = 0;
        if (!ValidRange(ref platform, source, sourceLength) ||
            !ValidRange(ref platform, template, templateLength) ||
            !ValidRange(ref platform, destination, destinationCapacity) ||
            !ValidRange(ref platform, resultArray, resultBytes) ||
            destinationCapacity < 2 || resultBytes < 4 ||
            RangesOverlap(source, sourceLength, destination,
                destinationCapacity))
            return false;
        if (argumentLength != 0 &&
            !ValidRange(ref platform, arguments, argumentLength))
            return false;

        if (!platform.TryReadArgs(arguments, argumentLength, template,
                templateLength, resultArray, resultBytes, out var rdArgs) ||
            rdArgs.IsNull)
            return false;

        if (!ShellReadArgsResultArrayCodec.TryCreate(ref platform,
                resultArray, resultBytes, out var results))
        {
            platform.FreeArgs(rdArgs);
            return false;
        }

        if (!TryGetBrackets(ref platform, template, templateLength,
                out var opening, out var closing))
        {
            platform.FreeArgs(rdArgs);
            return false;
        }
        if (!TryGetDollar(ref platform, template, templateLength,
                out var dollar))
        {
            platform.FreeArgs(rdArgs);
            return false;
        }
        var success = TryCopyExpanded(ref platform, source, sourceLength,
            template, templateLength, in results, destination,
            destinationCapacity, opening, closing, dollar, shellNumber,
            out destinationLength);
        platform.FreeArgs(rdArgs);
        return success;
    }

    private static bool TryCopyExpanded<TPlatform>(ref TPlatform platform,
        APTR source, uint sourceLength, APTR template, uint templateLength,
        in ShellReadArgsResultArray results, APTR destination,
        uint destinationCapacity, byte opening, byte closing, byte dollar,
        uint shellNumber, out uint destinationLength)
        where TPlatform : struct, IShellPlatform
    {
        destinationLength = 0;
        uint written = 0;
        for (var position = 0u; position < sourceLength; position++)
        {
            var value = platform.ReadUInt8(source, (int)position);
            if (value == 0) return false;
            if (value == opening)
            {
                var end = position + 1;
                while (end < sourceLength &&
                    platform.ReadUInt8(source, (int)end) != closing)
                    end++;
                var nameEnd = position + 1;
                while (nameEnd < end && platform.ReadUInt8(source,
                    (int)nameEnd) != dollar)
                    nameEnd++;
                if (end < sourceLength && end - position == 3 &&
                    nameEnd == position + 1 &&
                    platform.ReadUInt8(source, (int)(position + 1)) == dollar &&
                    platform.ReadUInt8(source, (int)(position + 2)) == dollar)
                {
                    if (shellNumber == 0 || !TryCopyUnsignedDecimal(
                            ref platform, destination, destinationCapacity,
                            ref written, shellNumber))
                        return false;
                    position = end;
                    continue;
                }
                if (end < sourceLength && nameEnd != position + 1 &&
                    TryFindTemplateEntry(ref platform, template,
                        templateLength, APTR.FromPointer(source.Raw +
                            position + 1), nameEnd - position - 1,
                        out var entry))
                {
                    if (!ShellReadArgsResultArrayCodec.TryReadPointer(
                            ref platform, in results, entry, out var parsed))
                        return false;
                    if (parsed.IsNull && nameEnd == end && TryFindDefault(
                            ref platform, template, templateLength,
                            APTR.FromPointer(source.Raw + position + 1),
                            nameEnd - position - 1, out _, out var defaultLength) &&
                        defaultLength == 0)
                    {
                        // Workbench 3.1 Execute accepts `.DEF name=` but drops
                        // a later line that expands that explicitly empty value.
                        platform.WriteUInt8(destination, 0, 0);
                        return true;
                    }
                    if ((parsed.IsNull && nameEnd < end && !TryCopyRaw(
                            ref platform, APTR.FromPointer(source.Raw +
                                nameEnd + 1), end - nameEnd - 1, destination,
                            destinationCapacity, ref written)) ||
                        (parsed.IsNotNull && !TryCopyValue(ref platform, parsed,
                            template, templateLength, APTR.FromPointer(
                                source.Raw + position + 1),
                            nameEnd - position - 1, destination,
                            destinationCapacity, ref written)) ||
                        (parsed.IsNull && nameEnd == end && !TryCopyValue(
                            ref platform, parsed, template, templateLength,
                            APTR.FromPointer(source.Raw + position + 1),
                            nameEnd - position - 1, destination,
                            destinationCapacity, ref written)))
                        return false;
                    position = end;
                    continue;
                }
            }
            if (!TryCopyByte(ref platform, destination, destinationCapacity,
                    ref written, value))
                return false;
        }
        platform.WriteUInt8(destination, (int)written, 0);
        destinationLength = written;
        return true;
    }

    private static bool TryCopyUnsignedDecimal<TPlatform>(
        ref TPlatform platform, APTR destination, uint destinationCapacity,
        ref uint written, uint value)
        where TPlatform : struct, IShellPlatform
    {
        var remaining = value;
        var divisor = 1_000_000_000u;
        while (divisor > remaining) divisor /= 10;
        while (divisor != 0)
        {
            byte digit = 0;
            while (remaining >= divisor && digit < 10)
            {
                remaining -= divisor;
                digit++;
            }
            if (!TryCopyByte(ref platform, destination, destinationCapacity,
                    ref written, (byte)('0' + digit)))
                return false;
            divisor /= 10;
        }
        return true;
    }

    private static bool TryFindTemplateEntry<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, APTR name, uint nameLength,
        out uint entry)
        where TPlatform : struct, IShellPlatform
    {
        entry = 0;
        var position = 0u;
        while (position < templateLength)
        {
            var start = position;
            while (position < templateLength &&
                platform.ReadUInt8(template, (int)position) is not
                    (byte)',' and not (byte)'/' and not (byte)'=')
                position++;
            if (position - start == nameLength && EqualsAsciiIgnoreCase(
                    ref platform, APTR.FromPointer(template.Raw + start),
                    name, nameLength))
                return true;
            while (position < templateLength &&
                platform.ReadUInt8(template, (int)position) != (byte)',')
                position++;
            if (position < templateLength) position++;
            entry++;
        }
        return false;
    }

    private static bool TryCopyValue<TPlatform>(ref TPlatform platform,
        APTR value, APTR template, uint templateLength, APTR name,
        uint nameLength, APTR destination, uint destinationCapacity,
        ref uint written)
        where TPlatform : struct, IShellPlatform
    {
        if (value.IsNull && TryFindDefault(ref platform, template,
                templateLength, name, nameLength, out var defaultValue,
                out var defaultLength))
            return TryCopyRaw(ref platform, defaultValue, defaultLength,
                destination, destinationCapacity, ref written);
        if (value.IsNull) return true;
        for (var offset = 0u; offset < 65_535; offset++)
        {
            if (value.Raw > uint.MaxValue - offset ||
                !platform.IsMapped(APTR.FromPointer(value.Raw + offset), 1))
                return false;
            var character = platform.ReadUInt8(value, (int)offset);
            if (character == 0) return true;
            if (!TryCopyByte(ref platform, destination, destinationCapacity,
                    ref written, character))
                return false;
        }
        return false;
    }

    private static bool TryFindDefault<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, APTR name, uint nameLength,
        out APTR value, out uint valueLength)
        where TPlatform : struct, IShellPlatform
    {
        value = APTR.Null;
        valueLength = 0;
        var found = false;
        var position = DefaultStart(templateLength);
        while (position <= TemplateBufferCapacity -
            ShellScriptDefaultEntryCodec.HeaderSize)
        {
            if (!ShellScriptDefaultEntryCodec.TryRead(ref platform, template,
                    TemplateBufferCapacity, position, out var stored))
                return false;
            if (stored.IsTerminator) break;
            if (stored.NameLength == nameLength && EqualsAsciiIgnoreCase(
                    ref platform, stored.Name, name, nameLength))
            {
                value = stored.Value;
                valueLength = stored.ValueLength;
                found = true;
            }
            position += stored.EncodedLength;
        }
        return found;
    }

    private static bool TryCopyRaw<TPlatform>(ref TPlatform platform,
        APTR source, uint length, APTR destination, uint capacity,
        ref uint written) where TPlatform : struct, IShellPlatform
    {
        for (var index = 0u; index < length; index++)
            if (!TryCopyByte(ref platform, destination, capacity, ref written,
                    platform.ReadUInt8(source, (int)index)))
                return false;
        return true;
    }

    private static bool TryCopyByte<TPlatform>(ref TPlatform platform,
        APTR destination, uint capacity, ref uint written, byte value)
        where TPlatform : struct, IShellPlatform
    {
        if (written >= capacity - 1) return false;
        platform.WriteUInt8(destination, (int)written++, value);
        return true;
    }

    private static bool EqualsAsciiIgnoreCase<TPlatform>(ref TPlatform platform,
        APTR left, APTR right, uint length) where TPlatform : struct, IShellPlatform
    {
        for (var index = 0u; index < length; index++)
        {
            var a = platform.ReadUInt8(left, (int)index);
            var b = platform.ReadUInt8(right, (int)index);
            if (a is >= (byte)'a' and <= (byte)'z') a -= 32;
            if (b is >= (byte)'a' and <= (byte)'z') b -= 32;
            if (a != b) return false;
        }
        return true;
    }

    private static bool ValidRange<TPlatform>(ref TPlatform platform, APTR value,
        uint length) where TPlatform : struct, IShellPlatform =>
        value.IsNotNull && value.Raw <= uint.MaxValue - length &&
        platform.IsMapped(value, length);

    private static bool RangesOverlap(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
        right.Raw < left.Raw + leftLength;

    private static uint DefaultStart(uint templateLength) => templateLength +
        1 + ShellScriptKeyDirectiveStateCodec.EncodedSize;

    private static bool TryGetBrackets<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, out byte opening, out byte closing)
        where TPlatform : struct, IShellPlatform
    {
        opening = 0;
        closing = 0;
        if (!ShellScriptKeyDirectiveStateCodec.TryRead(ref platform, template,
                TemplateBufferCapacity, templateLength, out var state))
            return false;
        opening = state.OpeningBracket;
        closing = state.ClosingBracket;
        return opening != 0 && closing != 0 && opening != closing;
    }

    private static bool TryGetDollar<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, out byte dollar)
        where TPlatform : struct, IShellPlatform
    {
        dollar = 0;
        if (!ShellScriptKeyDirectiveStateCodec.TryRead(ref platform, template,
                TemplateBufferCapacity, templateLength, out var state))
            return false;
        dollar = state.DollarMarker;
        return dollar != 0;
    }

}
