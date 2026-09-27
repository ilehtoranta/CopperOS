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

        return platform.WriteByte(writer.Output, (byte)'\n') < 0
            ? (int)ShellCommandResult.Error
            : (int)ShellCommandResult.Ok;
    }
}
