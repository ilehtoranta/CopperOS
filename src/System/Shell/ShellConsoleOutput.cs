using Amiga;

namespace CopperOS.Shell;

/// <summary>Console clear and reset sequences supported by the V40 console device.</summary>
public static class ShellConsoleOutput
{
    public static bool Clear<TPlatform>(ref TPlatform platform, BPTR output, uint reset)
        where TPlatform : struct, IShellPlatform
    {
        if (output.IsNull || reset > 1) return false;
        if (reset == 0) return platform.WriteByte(output, 0x0c) >= 0;
        // RIS resets character modes and display defaults; form feed only
        // clears the window. Both go through the selected DOS output stream.
        return platform.WriteByte(output, 0x1b) >= 0 &&
            platform.WriteByte(output, (byte)'c') >= 0;
    }
}
