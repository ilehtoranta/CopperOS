using Amiga;

namespace CopperOS.Commands;

/// <summary>Forward Quote rules currently implemented as bounded guest stages.</summary>
public enum QuoteForwardRule : byte
{
    ReadItem = 1,
    Hex = 2,
    Uri = 3,
    Base64 = 4,
    MatchPattern = 5,
    Arexx = 6,
    Sh = 7,
    Js = 8,
    C = 9,
}

/// <summary>
/// Applies supported Quote stages in source order using two caller-owned guest
/// buffers. It allocates nothing and retains no rule or text state.
/// </summary>
public static class QuoteForwardPipeline
{
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

        var current = source;
        var currentLength = sourceLength;
        for (var index = 0u; index < ruleCount; index++)
        {
            var destination = current.Raw == firstBuffer.Raw ? secondBuffer : firstBuffer;
            var capacity = destination.Raw == firstBuffer.Raw ? firstCapacity : secondCapacity;
            var rule = (QuoteForwardRule)memory.ReadUInt8(rules, (int)index);
            bool success = rule switch
            {
                QuoteForwardRule.ReadItem => QuoteReadItemFormatter.TryEncode(ref memory, current, currentLength, destination, capacity, out currentLength),
                QuoteForwardRule.Hex => QuoteHexFormatter.TryEncode(ref memory, current, currentLength, destination, capacity, out currentLength),
                QuoteForwardRule.Uri => QuoteUriFormatter.TryEncode(ref memory, current, currentLength, destination, capacity, out currentLength),
                QuoteForwardRule.Base64 => QuoteBase64Formatter.TryEncode(ref memory, current, currentLength, destination, capacity, out currentLength),
                _ => false,
            };
            if (!success) { outputLength = 0; return false; }
            current = destination;
        }
        output = current;
        outputLength = currentLength;
        return true;
    }

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength) => left.Raw < right.Raw + rightLength && right.Raw < left.Raw + leftLength;
}
