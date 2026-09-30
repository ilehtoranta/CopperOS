using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Source-observed MorphOS/AROS ResList command body.</summary>
public static class NativeMorphOSResListCommand
{
    private const uint BufferStep = 2048;
    private const uint RecordBytes = 8;
    private const uint CtrlCMask = 1u << 12;

    private struct Cells
    {
        public uint Address;
        public uint Name;

        public static APTR AddressOf(ref Cells cells) =>
            throw new System.NotSupportedException(
                "ResList.Cells.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Snapshots the Exec resource list into a public temporary buffer, prints
    /// the source table, and polls Ctrl-C after each emitted resource.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        var execBase = APTR.FromPointer(
            APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var size = BufferStep;

        for (;; size += BufferStep)
        {
            var buffer = Exec.AllocVec(size, (uint)Exec.MemoryFlags.Any);
            if (buffer.IsNull)
            {
                DOS.FPuts(DOS.Output(),
                    "Not Enough memory for resource buffer\n");
                return DOS.RETURN_FAIL;
            }

            var recordCount = 0u;
            var recordCursor = buffer;
            var stringEnd = buffer.Raw + size;
            var fits = true;
            var list = APTR.FromPointer(execBase.Raw +
                (uint)ExecLayout.ExecBase.ResourceList);
            var node = APTR.FromPointer(APTR.ReadUInt32(list,
                ExecLayout.List.Head));

            Exec.Forbid();
            while (node.IsNotNull &&
                APTR.ReadUInt32(node, ExecLayout.Node.Successor) != 0)
            {
                var recordEnd = recordCursor.Raw + RecordBytes;
                if (recordEnd > stringEnd)
                {
                    fits = false;
                    break;
                }

                var name = APTR.ReadUInt32(node, ExecLayout.Node.Name);
                var nameLength = 0u;
                if (name != 0)
                {
                    var scan = name;
                    while (APTR.ReadUInt8(APTR.FromPointer(scan), 0) != 0)
                    {
                        scan++;
                    }
                    nameLength = scan - name + 1;
                }

                if (nameLength > stringEnd - recordEnd)
                {
                    fits = false;
                    break;
                }

                stringEnd -= nameLength;
                if (nameLength != 0)
                {
                    for (var index = 0u; index < nameLength; index++)
                    {
                        APTR.WriteUInt8(APTR.FromPointer(stringEnd + index),
                            0, APTR.ReadUInt8(APTR.FromPointer(name + index),
                                0));
                    }
                }
                APTR.WriteUInt32(recordCursor, 0, node.Raw);
                APTR.WriteUInt32(recordCursor, 4, stringEnd);
                recordCursor = APTR.FromPointer(recordEnd);
                recordCount++;
                node = APTR.FromPointer(APTR.ReadUInt32(node,
                    ExecLayout.Node.Successor));
            }
            Exec.Permit();

            if (!fits)
            {
                Exec.FreeVec(buffer);
                continue;
            }

            DOS.FPuts(DOS.Output(),
                "address\t\tname\n" +
                "------------------------------------------------------------\n");
            var cells = default(Cells);
            var result = DOS.RETURN_OK;
            for (var index = 0u; index < recordCount; index++)
            {
                var record = APTR.FromPointer(buffer.Raw + index * RecordBytes);
                cells.Address = APTR.ReadUInt32(record, 0);
                cells.Name = APTR.ReadUInt32(record, 4);
                DOS.VPrintf("0x%08.lx\t%s\n", Cells.AddressOf(ref cells));
                if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                {
                    result = DOS.RETURN_FAIL;
                    DOS.SetIoErr(DOS.Error.Break);
                    break;
                }
            }
            Exec.FreeVec(buffer);
            if (result != DOS.RETURN_OK)
            {
                ioError = (int)DOS.IoErr();
                DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            }
            DOS.SetIoErr((DOS.Error)ioError);
            return result;
        }
    }
}
