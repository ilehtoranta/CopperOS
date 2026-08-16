using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Bounded unsigned decimal parsing for command keyword values.
/// </summary>
public static class ShellNumberParser
{
    public static bool TryParseUnsigned<TPlatform>(
        ref TPlatform platform,
        APTR text,
        uint length,
        out uint value)
        where TPlatform : struct, IShellPlatform
    {
        value = 0;
        if (length == 0 || text.IsNull ||
            text.Raw > uint.MaxValue - length ||
            !platform.IsMapped(text, length))
            return false;

        for (uint index = 0; index < length; index++)
        {
            byte digit = platform.ReadUInt8(text, (int)index);
            if (digit < (byte)'0' || digit > (byte)'9')
                return false;
            uint valueDigit = (uint)(digit - (byte)'0');
            if (value > (uint.MaxValue - valueDigit) / 10)
                return false;
            value = value * 10 + valueDigit;
        }
        return true;
    }
}
