using Amiga;

namespace CopperOS.Shell;

// Named state for the allocation-free decimal writer shared by commands that
// display a nonnegative Shell setting.
internal struct ShellUnsignedLineWriterState
{
    internal BPTR Output;
    internal uint Remaining;
    internal uint Place;
    internal bool Started;
}

internal static class ShellUnsignedOutput
{
    internal static int WriteLine<TPlatform>(
        ref TPlatform platform,
        BPTR output,
        uint value)
        where TPlatform : struct, IShellPlatform
    {
        var result = WriteValue(ref platform, output, value);
        if (result != (int)ShellCommandResult.Ok) return result;
        return platform.WriteByte(output, (byte)'\n') < 0
            ? (int)ShellCommandResult.Error : (int)ShellCommandResult.Ok;
    }

    internal static int WriteStackLine<TPlatform>(ref TPlatform platform,
        BPTR output, uint value) where TPlatform : struct, IShellPlatform
    {
        if (!WritePacked(ref platform, output, 0x43757272, 4) ||
            !WritePacked(ref platform, output, 0x656e7420, 4) ||
            !WritePacked(ref platform, output, 0x73746163, 4) ||
            !WritePacked(ref platform, output, 0x6b207369, 4) ||
            !WritePacked(ref platform, output, 0x7a652069, 4) ||
            !WritePacked(ref platform, output, 0x73200000, 2))
            return (int)ShellCommandResult.Error;
        var result = WriteValue(ref platform, output, value);
        if (result != (int)ShellCommandResult.Ok) return result;
        return WritePacked(ref platform, output, 0x20627974, 4) &&
               WritePacked(ref platform, output, 0x65730a00, 3)
            ? (int)ShellCommandResult.Ok : (int)ShellCommandResult.Error;
    }

    private static bool WritePacked<TPlatform>(ref TPlatform platform,
        BPTR output, uint text, uint length) where TPlatform : struct, IShellPlatform
    {
        for (var index = 0u; index < length; index++)
            if (platform.WriteByte(output, (byte)(text >> (int)(24 - index * 8))) < 0) return false;
        return true;
    }

    private static int WriteValue<TPlatform>(ref TPlatform platform,
        BPTR output, uint value) where TPlatform : struct, IShellPlatform
    {
        var writer = new ShellUnsignedLineWriterState
        {
            Output = output,
            Remaining = value,
            Place = 1_000_000_000,
        };

        do
        {
            // A uint has at most ten decimal digits. Removing one place
            // needs at most nine subtractions and no variable division.
            byte digit = 0;
            while (digit < 9 && writer.Remaining >= writer.Place)
            {
                writer.Remaining -= writer.Place;
                digit++;
            }
            if (writer.Started || digit != 0 || writer.Place == 1)
            {
                if (platform.WriteByte(writer.Output,
                        (byte)('0' + digit)) < 0)
                    return (int)ShellCommandResult.Error;
                writer.Started = true;
            }
            writer.Place /= 10;
        }
        while (writer.Place != 0);

        return (int)ShellCommandResult.Ok;
    }
}
