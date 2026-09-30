using Amiga;

namespace CopperOS.Commands;

/// <summary>Bounded reverse pipeline for the currently reversible Quote stages.</summary>
public static class QuoteReversePipeline
{
    /// <summary>
    /// Applies supported inverse stages in reverse declared order using two
    /// caller-owned buffers. READITEM and the remaining documented families
    /// are rejected until their exact inverse semantics are implemented.
    /// </summary>
    public static bool TryApply<TMemory>(ref TMemory memory, APTR source,
        uint sourceLength, APTR rules, uint ruleCount, APTR firstBuffer,
        uint firstCapacity, APTR secondBuffer, uint secondCapacity,
        out APTR output, out uint outputLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        output = APTR.Null;
        outputLength = 0;
        if (source.IsNull || rules.IsNull || firstBuffer.IsNull || secondBuffer.IsNull ||
            source.Raw > uint.MaxValue - sourceLength ||
            rules.Raw > uint.MaxValue - ruleCount ||
            firstBuffer.Raw > uint.MaxValue - firstCapacity ||
            secondBuffer.Raw > uint.MaxValue - secondCapacity ||
            !memory.IsMapped(source, sourceLength) || !memory.IsMapped(rules, ruleCount) ||
            !memory.IsMapped(firstBuffer, firstCapacity) || !memory.IsMapped(secondBuffer, secondCapacity) ||
            Overlaps(firstBuffer, firstCapacity, secondBuffer, secondCapacity) ||
            Overlaps(source, sourceLength, firstBuffer, firstCapacity) ||
            Overlaps(source, sourceLength, secondBuffer, secondCapacity)) return false;

        for (var index = 0u; index < ruleCount; index++)
            if (!IsSupported((QuoteForwardRule)memory.ReadUInt8(rules, (int)index))) return false;

        var current = source;
        var currentLength = sourceLength;
        for (var index = ruleCount; index > 0; index--)
        {
            var destination = current.Raw == firstBuffer.Raw ? secondBuffer : firstBuffer;
            var capacity = destination.Raw == firstBuffer.Raw ? firstCapacity : secondCapacity;
            var rule = (QuoteForwardRule)memory.ReadUInt8(rules, (int)(index - 1));
            bool success = rule switch
            {
                QuoteForwardRule.Hex => QuoteHexFormatter.TryDecode(ref memory, current, currentLength, destination, capacity, out currentLength),
                QuoteForwardRule.Uri => QuoteUriFormatter.TryDecode(ref memory, current, currentLength, destination, capacity, out currentLength),
                QuoteForwardRule.Base64 => QuoteBase64Formatter.TryDecode(ref memory, current, currentLength, destination, capacity, out currentLength),
                _ => false,
            };
            if (!success) { outputLength = 0; return false; }
            current = destination;
        }
        output = current;
        outputLength = currentLength;
        return true;
    }

    private static bool IsSupported(QuoteForwardRule rule) => rule is QuoteForwardRule.Hex or
        QuoteForwardRule.Uri or QuoteForwardRule.Base64;

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength &&
        right.Raw < left.Raw + leftLength;
}
