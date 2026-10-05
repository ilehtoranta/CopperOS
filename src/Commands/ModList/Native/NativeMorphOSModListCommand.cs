using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Source-observed MorphOS/AROS ModList command body.</summary>
public static class NativeMorphOSModListCommand
{
    public const string Template = "VERBOSE/S";
    private const uint BufferStep = 2048;
    private const uint InitialBuffer = 4096;
    private const uint RecordBytes = 28;
    private const uint CtrlCMask = 1u << 12;
    private const uint ExtendedResidentFlag = 1u << 6;

    private struct Args
    {
        public uint Verbose;

        public static APTR AddressOf(ref Args args) =>
            throw new System.NotSupportedException(
                "ModList.Args.AddressOf is lowered by CopperSharp.");
    }

    private struct Cells
    {
        public uint Address;
        public uint Name;
        public uint Version;
        public uint Revision;
        public uint Priority;

        public static APTR AddressOf(ref Cells cells) =>
            throw new System.NotSupportedException(
                "ModList.Cells.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Parses the source VERBOSE switch, snapshots Exec's resident module
    /// table under Forbid, and emits the source listing with Ctrl-C polling.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        var execBase = APTR.FromPointer(
            APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var args = default(Args);
        args.Verbose = 0;
        var rda = DOS.ReadArgs(Template, Args.AddressOf(ref args), APTR.Null);
        if (rda.IsNull)
        {
            ioError = (int)DOS.IoErr();
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var size = InitialBuffer;
        for (;; size += BufferStep)
        {
            var buffer = Exec.AllocVec(size, (uint)Exec.MemoryFlags.Any);
            if (buffer.IsNull)
            {
                DOS.FPuts(DOS.Output(),
                    "Not Enough memory for resident buffer\n");
                DOS.FreeArgs(rda);
                return DOS.RETURN_FAIL;
            }

            var recordCount = 0u;
            var recordCursor = buffer;
            var end = buffer.Raw + size;
            var table = APTR.ReadUInt32(execBase,
                ExecLayout.ExecBase.ResModules);
            var fits = true;
            Exec.Forbid();
            while (table != 0)
            {
                var resident = APTR.ReadUInt32(APTR.FromPointer(table), 0);
                table += 4;
                if (resident == 0)
                    break;
                if ((resident & 0x80000000u) != 0)
                {
                    table = resident & 0x7fffffffu;
                    continue;
                }

                var recordEnd = recordCursor.Raw + RecordBytes;
                if (recordEnd > end)
                {
                    fits = false;
                    break;
                }

                var residentPtr = APTR.FromPointer(resident);
                var flags = APTR.ReadUInt8(residentPtr,
                    ExecLayout.Resident.Flags);
                var revision = 0u;
                var hasRevision = 0u;
                if ((flags & ExtendedResidentFlag) != 0)
                {
                    revision = APTR.ReadUInt16(residentPtr,
                        (int)Resident.Size);
                    hasRevision = 1;
                }
                else
                {
                    var ids = APTR.ReadUInt32(residentPtr,
                        ExecLayout.Resident.IdString);
                    if (TryReadRevision(APTR.FromPointer(ids), out var parsed))
                    {
                        revision = parsed;
                        hasRevision = 1;
                    }
                }

                APTR.WriteUInt32(recordCursor, 0, resident);
                APTR.WriteUInt32(recordCursor, 4,
                    APTR.ReadUInt32(residentPtr, ExecLayout.Resident.Name));
                APTR.WriteUInt32(recordCursor, 8,
                    APTR.ReadUInt8(residentPtr, ExecLayout.Resident.Version));
                APTR.WriteUInt32(recordCursor, 12, revision);
                APTR.WriteUInt32(recordCursor, 16,
                    unchecked((uint)(int)(sbyte)APTR.ReadUInt8(residentPtr,
                        ExecLayout.Resident.Priority)));
                APTR.WriteUInt32(recordCursor, 20, flags);
                APTR.WriteUInt32(recordCursor, 24, hasRevision);
                recordCursor = APTR.FromPointer(recordEnd);
                recordCount++;
            }
            Exec.Permit();

            if (!fits)
            {
                Exec.FreeVec(buffer);
                continue;
            }

            DOS.FPuts(DOS.Output(),
                "address                    name                version   pri    flags\n"
                + "--------------------------------------------------------------------------\n");
            var cells = default(Cells);
            var result = DOS.RETURN_OK;
            for (var index = 0u; index < recordCount; index++)
            {
                var record = APTR.FromPointer(buffer.Raw + index * RecordBytes);
                cells.Address = APTR.ReadUInt32(record, 0);
                cells.Name = APTR.ReadUInt32(record, 4);
                cells.Version = APTR.ReadUInt32(record, 8);
                cells.Revision = APTR.ReadUInt32(record, 12);
                cells.Priority = APTR.ReadUInt32(record, 16);
                var flags = APTR.ReadUInt32(record, 20);
                var hasRevision = APTR.ReadUInt32(record, 24) != 0;
                if (!hasRevision)
                    cells.Revision = cells.Priority;
                DOS.VPrintf(hasRevision
                    ? "0x%08.lx\t%30.s\t%3lu.%-3lu\t%4.ld\t<"
                    : "0x%08.lx\t%30.s\t%3lu\t%4.ld\t<",
                    Cells.AddressOf(ref cells));
                var first = true;
                for (var bit = 7; bit >= 0; bit--)
                {
                    if ((flags & (1u << bit)) == 0)
                        continue;
                    if (!first)
                        DOS.FPuts(DOS.Output(), " | ");
                    first = false;
                    switch (bit)
                    {
                        case 0: DOS.FPuts(DOS.Output(), "ColdStart"); break;
                        case 1: DOS.FPuts(DOS.Output(), "SingleTask"); break;
                        case 2: DOS.FPuts(DOS.Output(), "AfterDos"); break;
                        case 3: DOS.FPuts(DOS.Output(), "PPC"); break;
                        case 4: DOS.FPuts(DOS.Output(), "Bit4"); break;
                        case 5: DOS.FPuts(DOS.Output(), "*Asynchron*"); break;
                        case 6: DOS.FPuts(DOS.Output(), "Extended"); break;
                        default: DOS.FPuts(DOS.Output(), "AutoInit"); break;
                    }
                }
                if (first)
                    DOS.FPuts(DOS.Output(), "NEVER");
                DOS.FPuts(DOS.Output(), ">\n");
                if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                {
                    result = DOS.RETURN_FAIL;
                    DOS.SetIoErr(DOS.Error.Break);
                    break;
                }
            }
            Exec.FreeVec(buffer);
            DOS.FreeArgs(rda);
            if (result != DOS.RETURN_OK)
            {
                ioError = (int)DOS.IoErr();
                DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            }
            return result;
        }
    }

    private static bool TryReadRevision(APTR ids, out uint revision)
    {
        revision = 0;
        if (ids.IsNull)
            return false;
        var cursor = ids.Raw;
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) != 0 &&
            APTR.ReadUInt8(APTR.FromPointer(cursor), 0) != 32)
            cursor++;
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) != 0 &&
            (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) < (uint)'0' ||
                APTR.ReadUInt8(APTR.FromPointer(cursor), 0) > (uint)'9'))
            cursor++;
        var major = 0u;
        var digits = 0u;
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) >= (uint)'0' &&
            APTR.ReadUInt8(APTR.FromPointer(cursor), 0) <= (uint)'9')
        {
            major = major * 10 + APTR.ReadUInt8(APTR.FromPointer(cursor), 0) - (uint)'0';
            cursor++;
            digits++;
        }
        if (digits == 0 || APTR.ReadUInt8(APTR.FromPointer(cursor), 0) != (uint)'.')
            return false;
        cursor++;
        digits = 0;
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) >= (uint)'0' &&
            APTR.ReadUInt8(APTR.FromPointer(cursor), 0) <= (uint)'9')
        {
            revision = revision * 10 + APTR.ReadUInt8(APTR.FromPointer(cursor), 0) - (uint)'0';
            cursor++;
            digits++;
        }
        return digits != 0;
    }
}
