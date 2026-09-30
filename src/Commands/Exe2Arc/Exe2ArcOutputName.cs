using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Builds Exe2Arc's generated output name in caller-owned guest memory. The
/// source replaces the last dot in the final path component (or appends one
/// when no suffix exists) and then adds the selected lower-case extension.
/// This helper keeps that rule while rejecting unterminated/over-capacity
/// strings instead of reproducing the source's unbounded scratch writes.
/// </summary>
public static class Exe2ArcOutputName
{
    /// <summary>
    /// Safe input-path admission bound. ReadArgs normally supplies a valid
    /// NUL-terminated string; this finite bound prevents a malformed pointer
    /// from making the helper scan indefinitely. It is not a claim about the
    /// original command's undocumented path limit.
    /// </summary>
    public const uint MaximumSourceBytes = 4096;

    /// <summary>
    /// Writes the generated name, including its terminating NUL, to
    /// <paramref name="destination"/>. The extension must be a nonempty
    /// NUL-terminated string and is copied without case conversion; callers
    /// select the source's lower-case extension before calling this method.
    /// No allocation or DOS call is performed.
    /// </summary>
    public static bool TryBuild<TMemory>(ref TMemory memory, APTR source,
        APTR destination, uint destinationCapacity, APTR extension,
        out uint bytesWritten)
        where TMemory : struct, IAmigaGuestMemory
    {
        bytesWritten = 0;
        if (source.IsNull || destination.IsNull || destinationCapacity == 0)
            return false;

        if (extension.IsNull ||
            !TryLength(ref memory, extension, 256, out uint extensionLength) ||
            extensionLength == 0)
            return false;

        if (!TryLength(ref memory, source, MaximumSourceBytes,
                out uint sourceLength))
            return false;

        uint required = sourceLength + 1u + extensionLength + 1u;
        if (required < sourceLength || required > destinationCapacity ||
            !memory.IsMapped(destination, required))
            return false;

        uint suffixStart = sourceLength;
        uint cursor = sourceLength;
        while (cursor != 0)
        {
            cursor--;
            byte value = memory.ReadUInt8(source, unchecked((int)cursor));
            if (value == (byte)'/')
                break;
            if (value == (byte)':')
                break;
            if (value == (byte)'.')
            {
                suffixStart = cursor;
                break;
            }
        }

        uint write = suffixStart;
        for (uint index = 0; index < write; index++)
            memory.WriteUInt8(destination, unchecked((int)index),
                memory.ReadUInt8(source, unchecked((int)index)));
        memory.WriteUInt8(destination, unchecked((int)write), (byte)'.');
        write++;
        for (uint index = 0; index < extensionLength; index++)
            memory.WriteUInt8(destination, unchecked((int)write++),
                memory.ReadUInt8(extension, unchecked((int)index)));
        memory.WriteUInt8(destination, unchecked((int)write), 0);
        bytesWritten = write + 1;
        return true;
    }

    private static bool TryLength<TMemory>(ref TMemory memory, APTR address,
        uint maximumBytes, out uint length)
        where TMemory : struct, IAmigaGuestMemory
    {
        length = 0;
        for (uint index = 0; index < maximumBytes; index++)
        {
            if (memory.ReadUInt8(address, unchecked((int)index)) == 0)
            {
                length = index;
                return true;
            }
        }
        return false;
    }
}
