using Amiga;

namespace CopperOS.Shell;

/// <summary>Single-pass Shell environment substitution using caller-owned work buffers.</summary>
public static class ShellVariableExpansion
{
    public static bool TryExpand<TPlatform>(ref TPlatform platform, APTR cli,
        APTR source, uint length, APTR destination, uint capacity,
        APTR name, uint nameCapacity, APTR value, uint valueCapacity,
        out uint written, out bool changed) where TPlatform : struct, IShellPlatform
    {
        written = 0;
        changed = false;
        if (capacity == 0 || nameCapacity == 0 || valueCapacity == 0 ||
            !platform.IsMapped(source, length) || !platform.IsMapped(destination, capacity) ||
            !platform.IsMapped(name, nameCapacity) || !platform.IsMapped(value, valueCapacity)) return false;
        for (var position = 0u; position < length;)
        {
            var start = position;
            var ch = platform.ReadUInt8(source, (int)position++);
            if (ch == (byte)'*' && position < length)
            {
                if (!Append(ref platform, source, start, 2, destination, capacity, ref written)) return false;
                position++;
                continue;
            }
            if (ch != (byte)'$')
            {
                if (!Append(ref platform, source, start, 1, destination, capacity, ref written)) return false;
                continue;
            }
            var braced = position < length && platform.ReadUInt8(source, (int)position) == (byte)'{';
            if (braced) position++;
            var first = position;
            while (position < length)
            {
                ch = platform.ReadUInt8(source, (int)position);
                if (braced ? ch == (byte)'}' : !AlphaNumeric(ch)) break;
                position++;
            }
            var count = position - first;
            var complete = !braced || position < length;
            if (braced && complete) position++;
            var found = false;
            var valueLength = 0u;
            if (complete && count != 0)
            {
                if (count >= nameCapacity) return false;
                platform.Copy(APTR.FromPointer(source.Raw + first), name, count);
                platform.WriteUInt8(name, (int)count, 0);
                found = platform.TryGetLocalVariable(cli, name, count, value, valueCapacity, out valueLength) ||
                    platform.TryGetGlobalVariable(name, count, value, valueCapacity, out valueLength);
            }
            if (found)
            {
                if (valueLength > valueCapacity ||
                    !Append(ref platform, value, 0, valueLength, destination, capacity, ref written)) return false;
                changed = true;
            }
            else if (!Append(ref platform, source, start, position - start,
                destination, capacity, ref written)) return false;
        }
        platform.WriteUInt8(destination, (int)written, 0);
        return true;
    }

    private static bool AlphaNumeric(byte ch) => ch is >= (byte)'a' and <= (byte)'z' or
        >= (byte)'A' and <= (byte)'Z' or >= (byte)'0' and <= (byte)'9';

    private static bool Append<TPlatform>(ref TPlatform platform, APTR source,
        uint offset, uint length, APTR destination, uint capacity, ref uint written)
        where TPlatform : struct, IShellPlatform
    {
        if (written >= capacity || length >= capacity - written) return false;
        if (length != 0) platform.Copy(APTR.FromPointer(source.Raw + offset),
            APTR.FromPointer(destination.Raw + written), length);
        written += length;
        return true;
    }
}
