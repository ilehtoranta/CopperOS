using Amiga;

namespace CopperOS.Shell;

/// <summary>Shell-owned diagnostic bytes, written before command streams close.</summary>
public static class ShellScriptDiagnostics
{
    public static bool WriteSyntaxError<TPlatform>(ref TPlatform platform, BPTR output)
        where TPlatform : struct, IShellPlatform =>
        !output.IsNull && platform.TryFlushShellOutput(output) && WritePacked(ref platform, output, 0x53796e74, 4) &&
        WritePacked(ref platform, output, 0x61782065, 4) &&
        WritePacked(ref platform, output, 0x72726f72, 4) &&
        WritePacked(ref platform, output, 0x0a000000, 1) && platform.TryFlushShellOutput(output);

    public static bool WriteMissingCommand<TPlatform>(ref TPlatform platform, BPTR output,
        APTR name, uint length) where TPlatform : struct, IShellPlatform =>
        !output.IsNull && platform.TryFlushShellOutput(output) && platform.Write(output, name, length) == (int)length &&
        WritePacked(ref platform, output, 0x3a20556e, 4) &&
        WritePacked(ref platform, output, 0x6b6e6f77, 4) &&
        WritePacked(ref platform, output, 0x6e20636f, 4) &&
        WritePacked(ref platform, output, 0x6d6d616e, 4) &&
        WritePacked(ref platform, output, 0x640a0000, 2) && platform.TryFlushShellOutput(output);

    public static bool WriteReturnCode<TPlatform>(ref TPlatform platform, BPTR output,
        APTR name, uint length, int result) where TPlatform : struct, IShellPlatform
    {
        if (output.IsNull || !platform.TryFlushShellOutput(output) || platform.Write(output, name, length) != (int)length ||
            !WritePacked(ref platform, output, 0x20666169, 4) ||
            !WritePacked(ref platform, output, 0x6c656420, 4) ||
            !WritePacked(ref platform, output, 0x72657475, 4) ||
            !WritePacked(ref platform, output, 0x726e636f, 4) ||
            !WritePacked(ref platform, output, 0x64652000, 3)) return false;
        var magnitude = unchecked((uint)result);
        if (result < 0)
        {
            if (platform.WriteByte(output, (byte)'-') < 0) return false;
            magnitude = unchecked((uint)(-(result + 1)) + 1u);
        }
        // At most nine subtractions per digit; only constant division is
        // needed on the freestanding 68000, as in script-key expansion.
        var divisor = 1_000_000_000u;
        while (divisor > magnitude && divisor > 1) divisor /= 10;
        while (divisor != 0)
        {
            var digit = 0u;
            while (magnitude >= divisor) { magnitude -= divisor; digit++; }
            if (platform.WriteByte(output, (byte)('0' + digit)) < 0) return false;
            divisor /= 10;
        }
        return platform.WriteByte(output, (byte)'\n') >= 0 && platform.TryFlushShellOutput(output);
    }

    private static bool WritePacked<TPlatform>(ref TPlatform platform, BPTR output,
        uint text, uint length) where TPlatform : struct, IShellPlatform
    {
        for (var index = 0u; index < length; index++)
            if (platform.WriteByte(output, (byte)(text >> (int)(24 - index * 8))) < 0) return false;
        return true;
    }

    public static bool WriteDosError<TPlatform>(ref TPlatform platform, BPTR output,
        int error, APTR scratch, uint capacity) where TPlatform : struct, IShellPlatform
    {
        if (output.IsNull || !platform.TryFormatShellError(error, scratch, capacity, out var length))
            return false;
        // V40 internal commands print the DOS catalogue text without a header.
        // CopperStart's catalogue can start with an uppercase ASCII letter.
        var first = platform.ReadUInt8(scratch, 0);
        if (first >= (byte)'A' && first <= (byte)'Z')
            platform.WriteUInt8(scratch, 0, (byte)(first + 32));
        return platform.TryFlushShellOutput(output) && platform.Write(output, scratch, length) == (int)length &&
            platform.WriteByte(output, (byte)'\n') >= 0 && platform.TryFlushShellOutput(output);
    }
}
