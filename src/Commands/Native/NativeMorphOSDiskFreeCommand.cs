using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 DiskFree source boundary.  The command keeps the optional
/// volume name, InfoData block and output record in guest memory and uses only
/// public DOS Lock/Info calls.  The exact localized output strings remain a
/// guest differential gate; the numeric modes and ownership contract are
/// source-bound here.
/// </summary>
public static class NativeMorphOSDiskFreeCommand
{
    public const string Template = "VOLUME,NOPOSTFIX/S,PERCENT/S";
    public const uint ResultCount = 3;

    private struct OutputCells
    {
        public uint Value;
        public uint Suffix;

        public static APTR AddressOf(ref OutputCells cells) =>
            throw new System.NotSupportedException(
                "DiskFree.OutputCells.AddressOf is lowered by CopperSharp.");
    }

    private struct WideCells
    {
        public uint High;
        public uint Low;

        public static APTR AddressOf(ref WideCells cells) =>
            throw new System.NotSupportedException(
                "DiskFree.WideCells.AddressOf is lowered by CopperSharp.");
    }

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return arguments.ReturnLevel;
        }

        var volume = APTR.Null;
        var lockValue = BPTR.Null;
        var info = APTR.Null;
        var result = DOS.RETURN_FAIL;
        var error = 0;
        var cells = default(OutputCells);
        var wide = default(WideCells);

        do
        {
            if (arguments.TryGetResult(0, out var rawVolume) &&
                rawVolume != 0)
                volume = APTR.FromPointer(rawVolume);

            // DOS treats an empty name as the current directory's volume;
            // preserving that call also avoids a host-side current-directory
            // substitute.
            var lockName = volume.IsNull
                ? CString.FromLiteral("")
                : CString.FromPointer(volume);
            lockValue = DOS.LockRaw(lockName, DOS.LockMode.Shared);
            if (lockValue.IsNull)
            {
                error = (int)DOS.IoErr();
                if (error == 0) error = (int)DOS.Error.ObjectNotFound;
                break;
            }

            info = Exec.AllocMem(InfoData.Size,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (info.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }

            if (DOS.Info(lockValue, info) == 0)
            {
                error = (int)DOS.IoErr();
                if (error == 0) error = (int)DOS.Error.ObjectWrongType;
                break;
            }

            var totalBlocks = APTR.ReadUInt32(info,
                DosLayout.InfoData.NumberOfBlocks);
            var usedBlocks = APTR.ReadUInt32(info,
                DosLayout.InfoData.NumberOfBlocksUsed);
            var blockBytes = APTR.ReadUInt32(info,
                DosLayout.InfoData.BytesPerBlock);
            if (usedBlocks > totalBlocks) usedBlocks = totalBlocks;
            var freeBlocks = totalBlocks - usedBlocks;
            Multiply32(freeBlocks, blockBytes, out wide.High, out wide.Low);

            var percent = 0u;
            if (totalBlocks != 0)
            {
                Multiply32(freeBlocks, 100u, out var scaledHigh,
                    out var scaledLow);
                percent = DivideWideBy32(scaledHigh, scaledLow,
                    totalBlocks);
            }
            var noPostfix = arguments.TryGetResult(1, out var noPostfixRaw) &&
                noPostfixRaw != 0;
            var percentMode = arguments.TryGetResult(2, out var percentRaw) &&
                percentRaw != 0;

            if (percentMode)
            {
                cells.Value = percent;
                DOS.VPrintf("%ld%%\n", OutputCells.AddressOf(ref cells));
            }
            else if (noPostfix)
            {
                // NOPOSTFIX is the script-friendly byte count mode.  A
                // 64-bit result is rendered through the DOS %llu formatter;
                // no managed conversion or host formatting is introduced.
                DOS.VPrintf("%llu\n", WideCells.AddressOf(ref wide));
            }
            else
            {
                var value = SelectDisplayValue(ref wide, out var suffix);
                cells.Value = value;
                cells.Suffix = CString.ToUInt32(suffix);
                DOS.VPrintf("%ld %s\n", OutputCells.AddressOf(ref cells));
            }
            result = DOS.RETURN_OK;
        }
        while (false);

        if (lockValue.IsNotNull) DOS.UnLock(lockValue);
        if (info.IsNotNull) Exec.FreeMem(info, InfoData.Size);
        arguments.Release();

        ioError = error;
        DOS.SetIoErr((DOS.Error)error);
        if (error != 0)
        {
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }
        return result;
    }

    private static void Multiply32(uint left, uint right, out uint high,
        out uint low)
    {
        var leftLow = left & 0xFFFFu;
        var leftHigh = left >> 16;
        var rightLow = right & 0xFFFFu;
        var rightHigh = right >> 16;
        var p0 = leftLow * rightLow;
        var p1 = leftHigh * rightLow;
        var p2 = leftLow * rightHigh;
        var p3 = leftHigh * rightHigh;
        var middle = (p0 >> 16) + (p1 & 0xFFFFu) +
            (p2 & 0xFFFFu);
        low = (p0 & 0xFFFFu) | (middle << 16);
        high = p3 + (p1 >> 16) + (p2 >> 16) + (middle >> 16);
    }

    private static uint DivideWideBy32(uint high, uint low, uint divisor)
    {
        if (divisor == 0) return uint.MaxValue;

        var quotient = 0u;
        var remainder = 0u;
        for (var bit = 63; bit >= 0; bit--)
        {
            var source = bit >= 32 ? high : low;
            var shift = bit >= 32 ? bit - 32 : bit;
            var inputBit = (source >> shift) & 1u;
            var carry = remainder >> 31;
            remainder <<= 1;
            remainder |= inputBit;
            if (carry != 0 || remainder >= divisor)
            {
                remainder -= divisor;
                if (bit < 32) quotient |= 1u << bit;
            }
        }
        return quotient;
    }

    private static uint SelectDisplayValue(ref WideCells value,
        out CString suffix)
    {
        const uint Kilo = 1024;
        const uint Mega = 1024 * Kilo;
        const uint Giga = 1024 * Mega;
        if (value.High != 0 || value.Low >= Giga)
        {
            suffix = CString.FromLiteral("GB");
            return value.High != 0 ? uint.MaxValue : value.Low / Giga;
        }
        if (value.Low >= Mega)
        {
            suffix = CString.FromLiteral("MB");
            return value.Low / Mega;
        }
        suffix = CString.FromLiteral("KB");
        return value.Low / Kilo;
    }
}
