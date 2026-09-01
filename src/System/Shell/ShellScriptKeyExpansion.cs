using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Expands <c>&lt;name&gt;</c> references from a resident Execute <c>.KEY</c>
/// template.  The caller supplies all buffers; DOS remains the authority for
/// argument parsing through ReadArgs.
/// </summary>
public static class ShellScriptKeyExpansion
{
    public const uint TemplateBufferCapacity = 4096;
    private const uint DirectiveHeaderSize = 6;
    private const byte DirectiveMagic0 = 0xC3;
    private const byte DirectiveMagic1 = 0x58;

    public static bool TryInitializeDirectiveState<TPlatform>(
        ref TPlatform platform, APTR template, uint templateLength)
        where TPlatform : struct, IShellPlatform
    {
        if (templateLength == 0 || templateLength >= TemplateBufferCapacity ||
            !ValidRange(ref platform, template, TemplateBufferCapacity))
            return false;
        var header = templateLength + 1;
        platform.WriteUInt8(template, (int)header, DirectiveMagic0);
        platform.WriteUInt8(template, (int)(header + 1), DirectiveMagic1);
        platform.WriteUInt8(template, (int)(header + 2), (byte)'<');
        platform.WriteUInt8(template, (int)(header + 3), (byte)'>');
        platform.WriteUInt8(template, (int)(header + 4), (byte)'$');
        platform.WriteUInt8(template, (int)(header + 5), (byte)'.');
        return true;
    }

    public static bool TrySetBracket<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, uint opening, byte value)
        where TPlatform : struct, IShellPlatform
    {
        if (opening > 1 || value == 0 || !TryGetDirectiveHeader(ref platform,
                template, templateLength, out var header))
            return false;
        platform.WriteUInt8(template, (int)(header + 2 + opening), value);
        return true;
    }

    public static bool TrySetDollar<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, byte value)
        where TPlatform : struct, IShellPlatform
    {
        if (value == 0 || !TryGetDirectiveHeader(ref platform, template,
                templateLength, out var header))
            return false;
        platform.WriteUInt8(template, (int)(header + 4), value);
        return true;
    }

    public static bool TrySetDot<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, byte value)
        where TPlatform : struct, IShellPlatform
    {
        if (value == 0 || !TryGetDirectiveHeader(ref platform, template,
                templateLength, out var header))
            return false;
        platform.WriteUInt8(template, (int)(header + 5), value);
        return true;
    }

    public static bool TryGetDot<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, out byte dot)
        where TPlatform : struct, IShellPlatform
    {
        dot = 0;
        if (!TryGetDirectiveHeader(ref platform, template, templateLength,
                out var header)) return false;
        dot = platform.ReadUInt8(template, (int)(header + 5));
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
        while (position + 4 <= TemplateBufferCapacity)
        {
            var storedNameLength = ReadUInt16(ref platform, template, position);
            var storedValueLength = ReadUInt16(ref platform, template,
                position + 2);
            if (storedNameLength == 0 && storedValueLength == 0) break;
            var recordLength = 4u + storedNameLength + storedValueLength;
            if (position > TemplateBufferCapacity - recordLength) return false;
            position += recordLength;
        }
        var required = 4u + nameLength + valueLength;
        if (position > TemplateBufferCapacity - required) return false;
        WriteUInt16(ref platform, template, position, (ushort)nameLength);
        WriteUInt16(ref platform, template, position + 2, (ushort)valueLength);
        platform.Copy(name, APTR.FromPointer(template.Raw + position + 4),
            nameLength);
        if (valueLength != 0)
            platform.Copy(value, APTR.FromPointer(template.Raw + position + 4 +
                nameLength), valueLength);
        var terminator = position + required;
        if (terminator + 4 <= TemplateBufferCapacity)
            platform.Clear(APTR.FromPointer(template.Raw + terminator), 4);
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
            template, templateLength, resultArray, resultBytes, destination,
            destinationCapacity, opening, closing, dollar, out destinationLength);
        platform.FreeArgs(rdArgs);
        return success;
    }

    private static bool TryCopyExpanded<TPlatform>(ref TPlatform platform,
        APTR source, uint sourceLength, APTR template, uint templateLength,
        APTR resultArray, uint resultBytes, APTR destination,
        uint destinationCapacity, byte opening, byte closing, byte dollar,
        out uint destinationLength)
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
                if (end < sourceLength && nameEnd != position + 1 &&
                    TryFindTemplateEntry(ref platform, template,
                        templateLength, APTR.FromPointer(source.Raw +
                            position + 1), nameEnd - position - 1,
                        out var entry))
                {
                    if (entry >= resultBytes / 4) return false;
                    var slot = APTR.FromPointer(resultArray.Raw + entry * 4);
                    var parsed = APTR.FromPointer(platform.ReadUInt32(slot, 0));
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
                        (parsed.IsNotNull && !TryCopyValue(ref platform, slot,
                            template, templateLength, APTR.FromPointer(
                                source.Raw + position + 1),
                            nameEnd - position - 1, destination,
                            destinationCapacity, ref written)) ||
                        (parsed.IsNull && nameEnd == end && !TryCopyValue(
                            ref platform, slot, template, templateLength,
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
        APTR resultSlot, APTR template, uint templateLength, APTR name,
        uint nameLength, APTR destination, uint destinationCapacity,
        ref uint written)
        where TPlatform : struct, IShellPlatform
    {
        var value = APTR.FromPointer(platform.ReadUInt32(resultSlot, 0));
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
        while (position + 4 <= TemplateBufferCapacity)
        {
            var storedNameLength = ReadUInt16(ref platform, template, position);
            var storedValueLength = ReadUInt16(ref platform, template,
                position + 2);
            if (storedNameLength == 0 && storedValueLength == 0) break;
            var recordLength = 4u + storedNameLength + storedValueLength;
            if (position > TemplateBufferCapacity - recordLength) return false;
            var storedName = APTR.FromPointer(template.Raw + position + 4);
            if (storedNameLength == nameLength && EqualsAsciiIgnoreCase(
                    ref platform, storedName, name, nameLength))
            {
                value = APTR.FromPointer(storedName.Raw + storedNameLength);
                valueLength = storedValueLength;
                found = true;
            }
            position += recordLength;
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
        1 + DirectiveHeaderSize;

    private static bool TryGetBrackets<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, out byte opening, out byte closing)
        where TPlatform : struct, IShellPlatform
    {
        opening = 0;
        closing = 0;
        if (!TryGetDirectiveHeader(ref platform, template, templateLength,
                out var header)) return false;
        opening = platform.ReadUInt8(template, (int)(header + 2));
        closing = platform.ReadUInt8(template, (int)(header + 3));
        return opening != 0 && closing != 0 && opening != closing;
    }

    private static bool TryGetDollar<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, out byte dollar)
        where TPlatform : struct, IShellPlatform
    {
        dollar = 0;
        if (!TryGetDirectiveHeader(ref platform, template, templateLength,
                out var header)) return false;
        dollar = platform.ReadUInt8(template, (int)(header + 4));
        return dollar != 0;
    }

    private static bool TryGetDirectiveHeader<TPlatform>(ref TPlatform platform,
        APTR template, uint templateLength, out uint header)
        where TPlatform : struct, IShellPlatform
    {
        header = 0;
        if (templateLength == 0 || templateLength >
            TemplateBufferCapacity - DirectiveHeaderSize - 1 ||
            !ValidRange(ref platform, template, TemplateBufferCapacity))
            return false;
        header = templateLength + 1;
        return platform.ReadUInt8(template, (int)header) == DirectiveMagic0 &&
            platform.ReadUInt8(template, (int)(header + 1)) == DirectiveMagic1;
    }

    private static ushort ReadUInt16<TPlatform>(ref TPlatform platform,
        APTR value, uint offset) where TPlatform : struct, IShellPlatform =>
        (ushort)((platform.ReadUInt8(value, (int)offset) << 8) |
            platform.ReadUInt8(value, (int)(offset + 1)));

    private static void WriteUInt16<TPlatform>(ref TPlatform platform,
        APTR value, uint offset, ushort number)
        where TPlatform : struct, IShellPlatform
    {
        platform.WriteUInt8(value, (int)offset, (byte)(number >> 8));
        platform.WriteUInt8(value, (int)(offset + 1), (byte)number);
    }
}
