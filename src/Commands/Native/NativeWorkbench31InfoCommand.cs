using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 Info implementation.  The v38.2 binary exposes the
/// classic single DEVICE argument and the mounted-disk/volume report.  This
/// body keeps DOS-list names in invocation-owned storage, releases the list
/// lock before calling filesystem handlers, and uses only public DOS/Exec
/// vectors.  Handler-specific status text and the complete multi-device
/// report remain subject to original-guest comparison before admission.
/// </summary>
public static class NativeWorkbench31InfoCommand
{
    public const string Template = "DEVICE";
    public const uint ResultCount = 1;

    private const uint WorkspaceBytes = 4096;
    private const uint RecordBytes = 8;
    private const uint RecordLimit = 32;
    private const uint NameAreaStart = WorkspaceBytes / 2;
    private const uint ListFlags = (uint)(DosListLockFlags.Read |
        DosListLockFlags.Assigns | DosListLockFlags.Volumes |
        DosListLockFlags.Devices);
    private const uint EntryFlags = (uint)(DosListLockFlags.Volumes |
        DosListLockFlags.Devices);
    private const uint CtrlCMask = 1u << 12;

    private struct Row
    {
        public uint Name;
        public uint Size;
        public uint UnitSuffix;
        public uint Used;
        public uint Free;
        public uint Full;
        public uint Errors;
        public uint Status;
        public uint Volume;

        public static APTR AddressOf(ref Row row) =>
            throw new System.NotSupportedException(
                "Info.Row.AddressOf is lowered by CopperSharp.");
    }

    private struct Pair
    {
        public uint Name;
        public uint Status;

        public static APTR AddressOf(ref Pair pair) =>
            throw new System.NotSupportedException(
                "Info.Pair.AddressOf is lowered by CopperSharp.");
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

        APTR workspace = APTR.Null;
        var result = DOS.RETURN_WARN;
        var error = 0;
        var listLocked = false;
        var recordCount = 0u;
        var nameCursor = WorkspaceBytes;
        var requested = APTR.Null;

        do
        {
            if (!arguments.TryGetResult(0, out var rawRequested))
            {
                error = (int)DOS.Error.BadTemplate;
                result = DOS.RETURN_FAIL;
                break;
            }
            requested = APTR.FromPointer(rawRequested);

            workspace = Exec.AllocVec(WorkspaceBytes,
                (uint)Exec.MemoryFlags.Any);
            if (workspace.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }

            var records = workspace;
            nameCursor = WorkspaceBytes;
            if (requested.IsNotNull)
            {
                var length = CStringLength(requested);
                if (length == 0 || length + 1 > WorkspaceBytes - NameAreaStart)
                {
                    error = (int)DOS.Error.BadTemplate;
                    result = DOS.RETURN_FAIL;
                    break;
                }
                nameCursor -= length + 1;
                var copied = APTR.FromPointer(workspace.Raw + nameCursor);
                CopyCString(requested, copied, length + 1);
                APTR.WriteUInt32(records, 0, copied.Raw);
                APTR.WriteUInt32(records, 4, (uint)DosListType.Device);
                recordCount = 1;
            }
            else
            {
                var cursor = DOS.LockDosList(ListFlags);
                if (cursor.IsNotNull)
                {
                    listLocked = true;
                    var node = cursor;
                    while (recordCount < RecordLimit)
                    {
                        node = DOS.NextDosEntry(node, EntryFlags);
                        if (node.IsNull) break;
                        var type = APTR.ReadUInt32(node,
                            DosLayout.DosList.Type);
                        if (type != (uint)DosListType.Device &&
                            type != (uint)DosListType.Volume)
                            continue;
                        if (type == (uint)DosListType.Device &&
                            APTR.ReadUInt32(node, DosLayout.DosList.Task) == 0)
                            continue;
                        var nameRaw = APTR.ReadUInt32(node,
                            DosLayout.DosList.Name);
                        if (nameRaw == 0) continue;
                        var bstr = BPTR.FromRaw(nameRaw).Address;
                        var length = APTR.ReadUInt8(bstr, 0);
                        if (length == 0 || (uint)length + 2u > nameCursor -
                                NameAreaStart)
                            break;
                        nameCursor -= (uint)length + 2u;
                        var copied = APTR.FromPointer(workspace.Raw + nameCursor);
                        for (var index = 0u; index < length; index++)
                            APTR.WriteUInt8(copied, unchecked((int)index),
                                APTR.ReadUInt8(bstr, unchecked((int)index + 1)));
                        APTR.WriteUInt8(copied, length, (byte)':');
                        APTR.WriteUInt8(copied, length + 1, 0);
                        var record = APTR.FromPointer(workspace.Raw +
                            recordCount * RecordBytes);
                        APTR.WriteUInt32(record, 0, copied.Raw);
                        APTR.WriteUInt32(record, 4, type);
                        recordCount++;
                        if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                        {
                            error = (int)DOS.Error.Break;
                            break;
                        }
                    }
                    DOS.UnLockDosList(ListFlags);
                    listLocked = false;
                }
            }

            if (error != 0) break;
            if (recordCount == 0)
            {
                error = (int)DOS.Error.ObjectNotFound;
                break;
            }

            DOS.FPuts(DOS.Output(), "Mounted disks:\n");
            DOS.FPuts(DOS.Output(),
                "Unit Size Used Free Full Errs   Status   Name\n");
            var unitSuffix = APTR.FromPointer(workspace.Raw + 2048);
            var readWrite = APTR.FromPointer(workspace.Raw + 2052);
            var noDisk = APTR.FromPointer(workspace.Raw + 2064);
            var unreadable = APTR.FromPointer(workspace.Raw + 2080);
            var volumeName = APTR.FromPointer(workspace.Raw + 2096);
            PutK(unitSuffix);
            PutReadWrite(readWrite);
            PutNoDisk(noDisk);
            PutUnreadable(unreadable);
            APTR.WriteUInt8(volumeName, 0, 0);
            var volumeHeaderEmitted = false;

            for (var index = 0u; index < recordCount; index++)
            {
                var record = APTR.FromPointer(workspace.Raw +
                    index * RecordBytes);
                var name = APTR.FromPointer(APTR.ReadUInt32(record, 0));
                var type = APTR.ReadUInt32(record, 4);
                if (type == (uint)DosListType.Volume)
                {
                    if (!volumeHeaderEmitted)
                    {
                        DOS.FPuts(DOS.Output(), "\nVolumes available:\n");
                        volumeHeaderEmitted = true;
                    }
                    DOS.VPrintf("%s [Mounted]\n", name);
                    continue;
                }

                var locked = DOS.LockRaw(CString.FromPointer(name),
                    DOS.LockMode.Shared);
                if (locked.IsNull)
                {
                    var pair = new Pair { Name = name.Raw, Status = noDisk.Raw };
                    DOS.VPrintf("%s %s\n", Pair.AddressOf(ref pair));
                    continue;
                }

                var info = Exec.AllocMem(InfoData.Size,
                    Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
                if (info.IsNull)
                {
                    DOS.UnLock(locked);
                    error = (int)DOS.Error.NoFreeStore;
                    break;
                }
                var ok = DOS.Info(locked, info);
                DOS.UnLock(locked);
                if (ok == 0)
                {
                    Exec.FreeMem(info, InfoData.Size);
                    var pair = new Pair { Name = name.Raw, Status = unreadable.Raw };
                    DOS.VPrintf("%s %s\n", Pair.AddressOf(ref pair));
                    continue;
                }

                var total = APTR.ReadUInt32(info,
                    DosLayout.InfoData.NumberOfBlocks);
                var used = APTR.ReadUInt32(info,
                    DosLayout.InfoData.NumberOfBlocksUsed);
                var blockSize = APTR.ReadUInt32(info,
                    DosLayout.InfoData.BytesPerBlock);
                var sizeKb = (total * blockSize) >> 10;
                var usedKb = (used * blockSize) >> 10;
                var freeKb = sizeKb >= usedKb ? sizeKb - usedKb : 0;
                var full = sizeKb == 0 ? 0 : (usedKb * 100) / sizeKb;
                var row = new Row
                {
                    Name = name.Raw,
                    Size = sizeKb,
                    UnitSuffix = unitSuffix.Raw,
                    Used = usedKb,
                    Free = freeKb,
                    Full = full,
                    Errors = APTR.ReadUInt32(info,
                        DosLayout.InfoData.NumberOfSoftErrors),
                    Status = readWrite.Raw,
                    Volume = name.Raw,
                };
                DOS.VPrintf(
                    "%-8s%5ld%s%8ld%8ld %3ld%% %3ld  %-10s %-s\n",
                    Row.AddressOf(ref row));
                Exec.FreeMem(info, InfoData.Size);
                result = DOS.RETURN_OK;
                if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                {
                    error = (int)DOS.Error.Break;
                    break;
                }
            }
        }
        while (false);

        if (listLocked) DOS.UnLockDosList(ListFlags);
        if (workspace.IsNotNull) Exec.FreeVec(workspace);
        arguments.Release();
        ioError = error;
        DOS.SetIoErr((DOS.Error)error);
        if (error != 0)
        {
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            return error == (int)DOS.Error.Break ? DOS.RETURN_WARN :
                DOS.RETURN_FAIL;
        }
        return result;
    }

    private static uint CStringLength(APTR text)
    {
        var length = 0u;
        while (length < 1023 && APTR.ReadUInt8(text,
                   unchecked((int)length)) != 0) length++;
        return length;
    }

    private static void CopyCString(APTR source, APTR destination,
        uint capacity)
    {
        for (var index = 0u; index < capacity; index++)
        {
            var value = APTR.ReadUInt8(source, unchecked((int)index));
            APTR.WriteUInt8(destination, unchecked((int)index), value);
            if (value == 0) break;
        }
    }

    private static void PutK(APTR destination)
    {
        APTR.WriteUInt8(destination, 0, (byte)'K');
        APTR.WriteUInt8(destination, 1, 0);
    }

    private static void PutReadWrite(APTR destination)
    {
        APTR.WriteUInt8(destination, 0, (byte)'R');
        APTR.WriteUInt8(destination, 1, (byte)'e');
        APTR.WriteUInt8(destination, 2, (byte)'a');
        APTR.WriteUInt8(destination, 3, (byte)'d');
        APTR.WriteUInt8(destination, 4, (byte)'/');
        APTR.WriteUInt8(destination, 5, (byte)'W');
        APTR.WriteUInt8(destination, 6, (byte)'r');
        APTR.WriteUInt8(destination, 7, (byte)'i');
        APTR.WriteUInt8(destination, 8, (byte)'t');
        APTR.WriteUInt8(destination, 9, (byte)'e');
        APTR.WriteUInt8(destination, 10, 0);
    }

    private static void PutNoDisk(APTR destination)
    {
        APTR.WriteUInt8(destination, 0, (byte)'N');
        APTR.WriteUInt8(destination, 1, (byte)'o');
        APTR.WriteUInt8(destination, 2, (byte)' ');
        APTR.WriteUInt8(destination, 3, (byte)'d');
        APTR.WriteUInt8(destination, 4, (byte)'i');
        APTR.WriteUInt8(destination, 5, (byte)'s');
        APTR.WriteUInt8(destination, 6, (byte)'k');
        APTR.WriteUInt8(destination, 7, (byte)' ');
        APTR.WriteUInt8(destination, 8, (byte)'p');
        APTR.WriteUInt8(destination, 9, (byte)'r');
        APTR.WriteUInt8(destination, 10, (byte)'e');
        APTR.WriteUInt8(destination, 11, (byte)'s');
        APTR.WriteUInt8(destination, 12, (byte)'e');
        APTR.WriteUInt8(destination, 13, (byte)'n');
        APTR.WriteUInt8(destination, 14, (byte)'t');
        APTR.WriteUInt8(destination, 15, 0);
    }

    private static void PutUnreadable(APTR destination)
    {
        APTR.WriteUInt8(destination, 0, (byte)'U');
        APTR.WriteUInt8(destination, 1, (byte)'n');
        APTR.WriteUInt8(destination, 2, (byte)'r');
        APTR.WriteUInt8(destination, 3, (byte)'e');
        APTR.WriteUInt8(destination, 4, (byte)'a');
        APTR.WriteUInt8(destination, 5, (byte)'d');
        APTR.WriteUInt8(destination, 6, (byte)'a');
        APTR.WriteUInt8(destination, 7, (byte)'b');
        APTR.WriteUInt8(destination, 8, (byte)'l');
        APTR.WriteUInt8(destination, 9, (byte)'e');
        APTR.WriteUInt8(destination, 10, (byte)' ');
        APTR.WriteUInt8(destination, 11, (byte)'d');
        APTR.WriteUInt8(destination, 12, (byte)'i');
        APTR.WriteUInt8(destination, 13, (byte)'s');
        APTR.WriteUInt8(destination, 14, (byte)'k');
        APTR.WriteUInt8(destination, 15, 0);
    }
}
