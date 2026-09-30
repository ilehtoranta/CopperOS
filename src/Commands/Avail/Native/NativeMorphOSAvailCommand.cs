using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 Avail body. MorphOS carries the HUMAN switch and performs a
/// best-effort public-memory flush before querying Exec. All state is kept in
/// guest memory/stack cells so the resident entry remains PURE.
/// </summary>
public static class NativeMorphOSAvailCommand
{
    public const string Template = "CHIP/S,FAST/S,TOTAL/S,FLUSH/S,H=HUMAN/S";
    public const uint ResultCount = 5;
    private const int ExecBaseMaxLocMemOffset = 416;

    private struct Cells
    {
        public uint Value;
        public uint Decimal;
        public uint Suffix;

        public static APTR AddressOf(ref Cells cells) =>
            throw new System.NotSupportedException(
                "Avail.Cells.AddressOf is lowered by CopperSharp.");
    }

    private struct Row
    {
        public uint Type;
        public uint Available;
        public uint InUse;
        public uint Maximum;
        public uint Largest;

        public static APTR AddressOf(ref Row row) =>
            throw new System.NotSupportedException(
                "Avail.Row.AddressOf is lowered by CopperSharp.");
    }

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError,
                CString.FromLiteral("Avail"));
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_OK;
        var error = 0;
        var printError = true;
        var chip = Read(arguments, 0);
        var fast = Read(arguments, 1);
        var total = Read(arguments, 2);
        var flush = Read(arguments, 3);
        var human = Read(arguments, 4);

        do
        {
            if ((chip != 0 ? 1 : 0) + (fast != 0 ? 1 : 0) +
                (total != 0 ? 1 : 0) > 1)
            {
                DOS.PutStr("only one of CHIP, FAST, or TOTAL allowed\n");
                printError = false;
                result = DOS.RETURN_WARN;
                break;
            }

            if (flush != 0)
            {
                var memory = Exec.AllocVec(0x7ffffff0u,
                    (uint)Exec.MemoryFlags.Public);
                if (memory.IsNotNull) Exec.FreeVec(memory);
            }

            if (chip != 0 || fast != 0 || total != 0)
            {
                var flags = chip != 0 ? Exec.MemoryFlags.Chip :
                    fast != 0 ? Exec.MemoryFlags.Fast : Exec.MemoryFlags.Any;
                if (!WriteValue(Exec.AvailMem(flags), human != 0, true))
                {
                    error = (int)DOS.IoErr();
                    result = DOS.RETURN_ERROR;
                }
                break;
            }

            Exec.Forbid();
            var chipAvailable = Exec.AvailMem(Exec.MemoryFlags.Chip);
            var chipMaximum = Exec.AvailMem(Exec.MemoryFlags.Chip |
                Exec.MemoryFlags.Total);
            var chipLargest = Exec.AvailMem(Exec.MemoryFlags.Chip |
                Exec.MemoryFlags.Largest);
            var fastAvailable = Exec.AvailMem(Exec.MemoryFlags.Fast);
            var fastMaximum = Exec.AvailMem(Exec.MemoryFlags.Fast |
                Exec.MemoryFlags.Total);
            var fastLargest = Exec.AvailMem(Exec.MemoryFlags.Fast |
                Exec.MemoryFlags.Largest);
            var totalAvailable = Exec.AvailMem(Exec.MemoryFlags.Any);
            var totalMaximum = Exec.AvailMem(Exec.MemoryFlags.Any |
                Exec.MemoryFlags.Total);
            var totalLargest = Exec.AvailMem(Exec.MemoryFlags.Any |
                Exec.MemoryFlags.Largest);
            Exec.Permit();

            // MorphOS on Pegasos can expose a synthetic chip-memory header.
            // The released command folds that header into TOTAL and suppresses
            // the synthetic CHIP row when ExecBase.MaxLocMem is zero.
            var execBase = APTR.FromPointer(
                APTR.ReadUInt32(APTR.FromPointer(4), 0));
            var maxLocMem = APTR.ReadUInt32(execBase,
                ExecBaseMaxLocMemOffset);
            if (maxLocMem == 0 && chipMaximum != 0)
            {
                totalMaximum = unchecked(totalMaximum - chipMaximum -
                    MemHeader.Size);
                chipAvailable = 0;
                chipMaximum = 0;
                chipLargest = 0;
            }

            if (DOS.PutStr("Type   Available    In-Use   Maximum   Largest\n") < 0)
            {
                error = (int)DOS.IoErr();
                result = DOS.RETURN_ERROR;
                break;
            }

            if (!WriteRow("chip", chipAvailable, chipMaximum,
                    chipLargest, human != 0) ||
                !WriteRow("fast", fastAvailable, fastMaximum,
                    fastLargest, human != 0) ||
                !WriteRow("total", totalAvailable, totalMaximum,
                    totalLargest, human != 0))
            {
                error = (int)DOS.IoErr();
                result = DOS.RETURN_ERROR;
            }
        }
        while (false);

        arguments.Release();
        ioError = error;
        DOS.SetIoErr((DOS.Error)error);
        if (error != 0 && printError)
            DOS.PrintFault((DOS.Error)error, CString.FromLiteral("Avail"));
        return result;
    }

    private static uint Read(NativeCommandArguments arguments, uint index) =>
        arguments.TryGetResult(index, out var value) ? value : 0;

    private static bool WriteValue(uint value, bool human, bool newline)
    {
        var cells = default(Cells);
        var result = -1;
        if (!human)
        {
            cells.Value = value;
            result = DOS.VPrintf("%lu", Cells.AddressOf(ref cells));
        }
        else
        {
            FormatHuman(value, ref cells);
            if (cells.Decimal == 0)
                result = DOS.VPrintf("%lu%s", Cells.AddressOf(ref cells));
            else
                result = DOS.VPrintf("%lu.%lu%s", Cells.AddressOf(ref cells));
        }
        if (newline) DOS.PutStr("\n");
        return result >= 0;
    }

    private static bool WriteRow(CString type, uint available, uint maximum,
        uint largest, bool human)
    {
        var row = default(Row);
        row.Type = CString.ToUInt32(type);
        row.Available = available;
        row.InUse = maximum - available;
        row.Maximum = maximum;
        row.Largest = largest;
        if (!human)
            return DOS.VPrintf("%s %lu %lu %lu %lu\n",
                Row.AddressOf(ref row)) >= 0;

        if (DOS.PutStr(type) < 0 || DOS.PutStr(" ") < 0 ||
            !WriteValue(available, true, false) ||
            DOS.PutStr(" ") < 0 || !WriteValue(row.InUse, true, false) ||
            DOS.PutStr(" ") < 0 || !WriteValue(maximum, true, false) ||
            DOS.PutStr(" ") < 0 || !WriteValue(largest, true, true))
            return false;
        return true;
    }

    private static void FormatHuman(uint value, ref Cells cells)
    {
        const uint kilo = 1024;
        const uint mega = 1024 * kilo;
        const uint giga = 1024 * mega;
        cells.Value = value;
        cells.Decimal = 0;
        cells.Suffix = CString.ToUInt32("B");
        if (value >= giga)
        {
            cells.Value = value >> 30;
            cells.Decimal = (((value % giga) * 10u + 536870912u) /
                giga) % 10;
            cells.Suffix = CString.ToUInt32("G");
        }
        else if (value >= mega)
        {
            cells.Value = value >> 20;
            cells.Decimal = (((value % mega) * 10u + 524288u) /
                mega) % 10;
            cells.Suffix = CString.ToUInt32("M");
        }
        else if (value >= kilo)
        {
            cells.Value = value >> 10;
            cells.Decimal = (((value % kilo) * 10u + 512u) / kilo) % 10;
            cells.Suffix = CString.ToUInt32("K");
        }
    }
}
