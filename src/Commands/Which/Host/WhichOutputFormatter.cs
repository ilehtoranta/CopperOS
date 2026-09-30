using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands;

/// <summary>Reference-observed display category for one <c>Which</c> result.</summary>
public enum WhichOutputKind : byte
{
    Path = 0,
    Internal = 1,
    Resident = 2,
}

/// <summary>
/// Formats one classic Which output line in caller-owned guest storage. Lookup,
/// resident lifetime and option selection remain with their shared DOS owners.
/// </summary>
public static class WhichOutputFormatter
{
    private const uint MaximumTextLength = 65_535;

    public static bool TryWrite<TPlatform>(ref TPlatform platform, BPTR output,
        WhichOutputKind kind, APTR name, uint nameLength, APTR resolvedPath,
        uint resolvedPathLength, APTR buffer, uint bufferCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (output.IsNull || buffer.IsNull || bufferCapacity < 2 ||
            !ValidSpan(ref platform, buffer, bufferCapacity))
            return false;

        APTR source;
        uint sourceLength;
        uint prefixLength;
        switch (kind)
        {
            case WhichOutputKind.Path:
                source = resolvedPath;
                sourceLength = resolvedPathLength;
                prefixLength = 0;
                break;
            case WhichOutputKind.Internal:
                source = name;
                sourceLength = nameLength;
                prefixLength = 9; // "INTERNAL "
                break;
            case WhichOutputKind.Resident:
                source = name;
                sourceLength = nameLength;
                prefixLength = 4; // "RES "
                break;
            default:
                return false;
        }

        if (!ValidSpan(ref platform, source, sourceLength) ||
            sourceLength > uint.MaxValue - prefixLength - 1 ||
            prefixLength + sourceLength + 1 > bufferCapacity ||
            Overlaps(buffer, bufferCapacity, source, sourceLength))
            return false;

        if (kind == WhichOutputKind.Internal)
        {
            WriteInternalPrefix(ref platform, buffer);
        }
        else if (kind == WhichOutputKind.Resident)
        {
            platform.WriteUInt8(buffer, 0, (byte)'R');
            platform.WriteUInt8(buffer, 1, (byte)'E');
            platform.WriteUInt8(buffer, 2, (byte)'S');
            platform.WriteUInt8(buffer, 3, (byte)' ');
        }
        for (var index = 0u; index < sourceLength; index++)
            platform.WriteUInt8(buffer, unchecked((int)(prefixLength + index)),
                platform.ReadUInt8(source, unchecked((int)index)));
        var length = prefixLength + sourceLength;
        platform.WriteUInt8(buffer, unchecked((int)length), (byte)'\n');
        return platform.Write(output, buffer, length + 1) == (int)(length + 1);
    }

    private static bool ValidSpan<TPlatform>(ref TPlatform platform, APTR value,
        uint length) where TPlatform : struct, IShellPlatform =>
        value.IsNotNull && length != 0 && length <= MaximumTextLength &&
        value.Raw <= uint.MaxValue - length && platform.IsMapped(value, length);

    private static bool Overlaps(APTR left, uint leftLength, APTR right,
        uint rightLength)
    {
        var leftEnd = left.Raw + leftLength;
        var rightEnd = right.Raw + rightLength;
        return left.Raw < rightEnd && right.Raw < leftEnd;
    }

    private static void WriteInternalPrefix<TPlatform>(ref TPlatform platform,
        APTR destination)
        where TPlatform : struct, IShellPlatform
    {
        platform.WriteUInt8(destination, 0, (byte)'I');
        platform.WriteUInt8(destination, 1, (byte)'N');
        platform.WriteUInt8(destination, 2, (byte)'T');
        platform.WriteUInt8(destination, 3, (byte)'E');
        platform.WriteUInt8(destination, 4, (byte)'R');
        platform.WriteUInt8(destination, 5, (byte)'N');
        platform.WriteUInt8(destination, 6, (byte)'A');
        platform.WriteUInt8(destination, 7, (byte)'L');
        platform.WriteUInt8(destination, 8, (byte)' ');
    }
}
