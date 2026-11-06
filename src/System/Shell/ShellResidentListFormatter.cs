using Amiga;

namespace CopperOS.Shell;

/// <summary>Memory, output, and Shell state used to format resident listings.</summary>
public interface IShellResidentListWriter : IAmigaGuestMemory
{
    int WriteByte(BPTR handle, byte value);
    bool TryGetInternalCommandEnabled(APTR cli, uint commandIdentity, out uint enabled);
}

/// <summary>Formats the V40 Resident listing through the Shell's output interface.</summary>
public static class ShellResidentListFormatter
{
    public static bool WriteHeader<TPlatform>(ref TPlatform platform, BPTR output)
        where TPlatform : struct, IShellResidentListWriter =>
        Word(ref platform, output, 0x4e414d45, 4) && Spaces(ref platform, output, 14) &&
        Word(ref platform, output, 0x55534520, 4) && Word(ref platform, output, 0x434f554e, 4) &&
        Word(ref platform, output, 0x540a0a00, 3);

    public static bool WriteInternals<TPlatform>(ref TPlatform platform, APTR cli, BPTR output)
        where TPlatform : struct, IShellResidentListWriter
    {
        for (var identity = 1u; identity <= 31; identity++)
        {
            if (!ShellInternalCommandResolver.TryGetNameRecord((ShellInternalCommand)identity, out var name) ||
                !platform.TryGetInternalCommandEnabled(cli, identity, out var enabled)) return false;
            if (enabled == 0) continue;
            if (!Word(ref platform, output, name.FirstWord, name.Length < 4 ? name.Length : 4) ||
                (name.Length > 4 && !Word(ref platform, output, name.SecondWord, name.Length - 4)) ||
                !InternalSuffix(ref platform, output, name.Length)) return false;
        }
        return Word(ref platform, output, 0x2e6b6574, 4) && InternalSuffix(ref platform, output, 4) &&
            Word(ref platform, output, 0x2e627261, 4) && InternalSuffix(ref platform, output, 4) &&
            Word(ref platform, output, 0x2e6b6579, 4) && InternalSuffix(ref platform, output, 4);
    }

    public static bool WriteEntry<TPlatform>(ref TPlatform platform, BPTR output,
        APTR name, uint length, int useCount) where TPlatform : struct, IShellResidentListWriter
    {
        if (name.IsNull || !platform.IsMapped(name, length)) return false;
        for (var i = 0u; i < length; i++)
            if (platform.WriteByte(output, platform.ReadUInt8(name, (int)i)) < 0) return false;
        if (useCount == -2) return InternalSuffix(ref platform, output, length);
        if (!Spaces(ref platform, output, length < 18 ? 18 - length : 0)) return false;
        if (useCount == -1)
            return Word(ref platform, output, 0x53595354, 4) && Word(ref platform, output, 0x454d0a00, 3);
        if (useCount < 0) return false;
        // The Shell owns one use of an admitted resident; V40 displays the
        // count above that ownership, including -1 for an AddSegment count 0.
        var value = useCount == 0 ? 1u : (uint)(useCount - 1);
        var digits = useCount == 0 ? 2u : 1u;
        for (var n = value; n >= 10; n /= 10) digits++;
        return Spaces(ref platform, output, digits < 4 ? 4 - digits : 0) &&
            (useCount != 0 || platform.WriteByte(output, (byte)'-') >= 0) &&
            Number(ref platform, output, value) && platform.WriteByte(output, (byte)'\n') >= 0;
    }

    private static bool InternalSuffix<TPlatform>(ref TPlatform platform, BPTR output, uint length)
        where TPlatform : struct, IShellResidentListWriter =>
        Spaces(ref platform, output, length < 18 ? 18 - length : 0) &&
        Word(ref platform, output, 0x494e5445, 4) && Word(ref platform, output, 0x524e414c, 4) &&
        platform.WriteByte(output, (byte)'\n') >= 0;

    private static bool Spaces<TPlatform>(ref TPlatform platform, BPTR output, uint count)
        where TPlatform : struct, IShellResidentListWriter
    {
        for (var i = 0u; i < count; i++) if (platform.WriteByte(output, (byte)' ') < 0) return false;
        return true;
    }

    private static bool Word<TPlatform>(ref TPlatform platform, BPTR output, uint value, uint count)
        where TPlatform : struct, IShellResidentListWriter
    {
        for (var i = 0u; i < count; i++)
            if (platform.WriteByte(output, (byte)(value >> (int)(24 - i * 8))) < 0) return false;
        return true;
    }

    private static bool Number<TPlatform>(ref TPlatform platform, BPTR output, uint value)
        where TPlatform : struct, IShellResidentListWriter
    {
        var place = 1_000_000_000u;
        var started = false;
        do
        {
            byte digit = 0;
            while (digit < 9 && value >= place) { value -= place; digit++; }
            if (started || digit != 0 || place == 1)
            {
                if (platform.WriteByte(output, (byte)('0' + digit)) < 0) return false;
                started = true;
            }
            place /= 10;
        } while (place != 0);
        return true;
    }
}
