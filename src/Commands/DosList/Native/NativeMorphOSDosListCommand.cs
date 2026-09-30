using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-observed MorphOS 3.20 DOSList command body.  The public DOS list
/// locks and traversal vectors are kept in the guest; names are copied to an
/// invocation-owned buffer before rendering so no live list pointer escapes
/// its matching read-lock lease.
/// </summary>
public static class NativeMorphOSDosListCommand
{
    public const string Template =
        "NAME,ADDRESS/N,DEVICES/S,VOLUMES/S,ASSIGNS/S,VERBOSE/S";
    public const uint ResultCount = 6;

    private const uint BufferBytes = 4096;
    private const uint VerboseBufferBytes = 32768;
    private const uint CommonRecordBytes = 36;
    private const uint VerboseRecordBytes = 240;
    private const uint NameScratchBytes = 256;
    private const uint AssignListRecordBytes = 44;
    private const uint VolumeLockRecordBytes = 12;
    private const uint VolumeDateTimeOffset = 208;
    private const uint CtrlCMask = 1u << 12;
    private const uint AssignNameOffset = 20;
    private const uint AssignLockOffset = 24;
    private const uint AssignLockNameOffset = 28;
    private const uint AssignListHeadOffset = 32;
    private const uint VerboseMaskLowOffset = 36;
    private const uint VerboseMaskHighOffset = 40;
    private const uint VerboseValuesOffset = 44;
    private const uint PortAttributeFoundFlag = 0x100u;
    private const uint AssignNameAttributeFoundFlag = 0x200u;
    private const uint AssignNamePresentFlag = 0x400u;
    private const uint AssignLockAttributeFoundFlag = 0x800u;
    private const uint AssignLockNameFoundFlag = 0x1000u;

    private const uint HandlerField = 0;
    private const uint StackSizeField = 1;
    private const uint PriorityField = 2;
    private const uint SegmentListField = 3;
    private const uint GlobalVectorField = 4;
    private const uint SerialIdField = 5;
    private const uint StartupMessageField = 6;
    private const uint StartupDeviceField = 7;
    private const uint StartupUnitField = 8;
    private const uint StartupFlagsField = 9;
    private const uint EnvironmentField = 10;
    private const uint SizeBlockField = 11;
    private const uint SectorOriginField = 12;
    private const uint SurfacesField = 13;
    private const uint SectorsPerBlockField = 14;
    private const uint BlocksPerTrackField = 15;
    private const uint ReservedField = 16;
    private const uint PreAllocField = 17;
    private const uint InterleaveField = 18;
    private const uint LowCylinderField = 19;
    private const uint HighCylinderField = 20;
    private const uint NumberOfBuffersField = 21;
    private const uint BufferMemoryTypeField = 22;
    private const uint MaximumTransferField = 23;
    private const uint MaskField = 24;
    private const uint BootPriorityField = 25;
    private const uint DosTypeField = 26;
    private const uint BaudField = 27;
    private const uint BootBlocksField = 28;
    private const uint ControlField = 29;
    private const uint TableSizeField = 30;
    private const uint StartupField = 31;
    private const uint StartupValueField = 32;
    private const uint AssignTypeField = 33;
    private const uint VolumeTimeField = 34;
    private const uint VolumeDayField = 35;
    private const uint VolumeDateField = 36;
    private const uint VolumeDiskTypeField = 37;
    private const uint VolumeLockListField = 38;
    private const uint VolumeDateAttributeFoundFlag = 0x2000u;
    private const uint VolumeDateFormatSucceededFlag = 0x4000u;

    private struct Cells
    {
        public uint Name;
        public uint Address;

        public static APTR AddressOf(ref Cells cells) =>
            throw new System.NotSupportedException(
                "DosList.Cells.AddressOf is lowered by CopperSharp.");
    }

    private struct ProcessCells
    {
        public uint Process;
        public uint Name;

        public static APTR AddressOf(ref ProcessCells cells) =>
            throw new System.NotSupportedException(
                "DosList.ProcessCells.AddressOf is lowered by CopperSharp.");
    }

    private struct TripleCells
    {
        public uint First;
        public uint Second;
        public uint Third;

        public static APTR AddressOf(ref TripleCells cells) =>
            throw new System.NotSupportedException(
                "DosList.TripleCells.AddressOf is lowered by CopperSharp.");
    }

    private struct AttributeCells
    {
        public uint NameBuffer;
        public uint NameLength;
        public uint PortValue;
        public uint PortLength;
        public uint NameTag;
        public uint NameData;
        public uint PortTag;
        public uint PortData;
        public uint DoneTag;
        public uint DoneData;

        public static APTR AddressOf(ref AttributeCells cells) =>
            throw new System.NotSupportedException(
                "DosList.AttributeCells.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Implements the released source's device, volume, and assign pass
    /// ordering.  Each pass owns only its matching public read lock.  The
    /// Common node, process, and source-backed assign rows use public DOS
    /// object attributes. Device and assign data is copied before the list
    /// lock is released, then rendered from invocation-owned storage.
    /// </summary>
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

        var name = ReadPointer(arguments, 0);
        var address = ReadPointer(arguments, 1);
        var wantedAddress = address.IsNotNull
            ? APTR.ReadUInt32(address, 0) : 0u;
        var devices = ReadSwitch(arguments, 2) != 0;
        var volumes = ReadSwitch(arguments, 3) != 0;
        var assigns = ReadSwitch(arguments, 4) != 0;
        var verbose = ReadSwitch(arguments, 5) != 0;

        if (!devices && !volumes && !assigns)
        {
            devices = true;
            volumes = true;
            assigns = true;
        }

        var bufferBytes = verbose ? VerboseBufferBytes : BufferBytes;
        var buffer = Exec.AllocVec(bufferBytes,
            (uint)Exec.MemoryFlags.Any);
        if (buffer.IsNull)
        {
            arguments.Release();
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            ioError = (int)DOS.Error.NoFreeStore;
            DOS.PrintFault(DOS.Error.NoFreeStore, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_OK;
        var error = 0;
        if (devices)
            result = RunPass(DosListLockFlags.Devices, name,
                wantedAddress, verbose, buffer, bufferBytes, ref error);
        if (error == 0 && volumes)
            result = RunPass(DosListLockFlags.Volumes, name,
                wantedAddress, verbose, buffer, bufferBytes, ref error);
        if (error == 0 && assigns)
            result = RunPass(DosListLockFlags.Assigns, name,
                wantedAddress, verbose, buffer, bufferBytes, ref error);

        Exec.FreeVec(buffer);
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

    private static int RunPass(DosListLockFlags kind,
        APTR wantedName, uint wantedAddress, bool verbose, APTR buffer,
        uint bufferBytes, ref int ioError)
    {
        var lockFlags = (uint)(kind | DosListLockFlags.Read);
        var cursor = DOS.AttemptLockDosList(lockFlags);
        if (cursor.IsNull) return DOS.RETURN_OK;

        var recordBytes = verbose ? VerboseRecordBytes : CommonRecordBytes;
        var count = 0u;
        var stringEnd = buffer.Raw + bufferBytes - NameScratchBytes;
        var nameScratch = APTR.FromPointer(stringEnd);
        var result = DOS.RETURN_OK;
        while (true)
        {
            var node = DOS.NextDosEntry(cursor, (uint)kind);
            if (node.IsNull) break;

            if (wantedAddress != 0 && wantedAddress != node.Raw)
                continue;

            var attributes = default(AttributeCells);
            var nameFound = TryGetNodeName(kind, node, nameScratch,
                ref attributes);
            if (wantedName.IsNotNull &&
                (!nameFound || !NameMatches(
                    APTR.ReadUInt32(AttributeCells.AddressOf(ref attributes), 0),
                    wantedName)))
                continue;

            var record = APTR.FromPointer(buffer.Raw + count * recordBytes);
            var recordEnd = record.Raw + recordBytes;
            if (recordEnd > stringEnd)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }
            APTR.WriteUInt32(record,
                unchecked((int)AssignNameOffset), 0);
            APTR.WriteUInt32(record,
                unchecked((int)AssignLockOffset), 0);
            APTR.WriteUInt32(record,
                unchecked((int)AssignLockNameOffset), 0);
            APTR.WriteUInt32(record,
                unchecked((int)AssignListHeadOffset), 0);
            if (verbose)
            {
                APTR.WriteUInt32(record,
                    unchecked((int)VerboseMaskLowOffset), 0);
                APTR.WriteUInt32(record,
                    unchecked((int)VerboseMaskHighOffset), 0);
            }
            var rowName = 0u;
            if (nameFound && !TryCopyCString(nameScratch, recordEnd,
                    ref stringEnd, out rowName))
            {
                ioError = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }

            var portFound = false;
            var processAddress = 0u;
            var processName = 0u;
            var recordFlags = (uint)kind;
            if (kind == DosListLockFlags.Assigns)
            {
                if (!CaptureAssign(node, record, nameScratch, recordEnd,
                        ref stringEnd, verbose, ref attributes,
                        ref recordFlags, ref portFound,
                        ref processAddress, ref processName))
                {
                    ioError = (int)DOS.Error.NoFreeStore;
                    result = DOS.RETURN_FAIL;
                    break;
                }
            }
            else
            {
                portFound = TryGetNodePort(kind, node, ref attributes);
                processAddress = portFound
                    ? ResolvePortProcess(APTR.FromPointer(
                        attributes.PortValue)) : 0u;
                if (processAddress != 0)
                {
                    var processNameRaw = APTR.ReadUInt32(
                        APTR.FromPointer(processAddress),
                        ExecLayout.Node.Name);
                    if (processNameRaw == 0)
                        processAddress = 0;
                    else if (!TryCopyCString(
                        APTR.FromPointer(processNameRaw), recordEnd,
                        ref stringEnd, out processName))
                    {
                        ioError = (int)DOS.Error.NoFreeStore;
                        result = DOS.RETURN_FAIL;
                        break;
                    }
                }
                if (portFound) recordFlags |= PortAttributeFoundFlag;
            }

            APTR.WriteUInt32(record, 0, rowName);
            APTR.WriteUInt32(record, 4, node.Raw);
            APTR.WriteUInt32(record, 8, processAddress);
            APTR.WriteUInt32(record, 12, processName);
            if (verbose)
            {
                if (kind == DosListLockFlags.Devices &&
                    !CaptureDeviceVerbose(node, record, nameScratch,
                        recordEnd, ref stringEnd, ref attributes))
                {
                    ioError = (int)DOS.Error.NoFreeStore;
                    result = DOS.RETURN_FAIL;
                    break;
                }
                if (kind == DosListLockFlags.Volumes &&
                    !CaptureVolumeVerbose(node, record, nameScratch,
                        recordEnd, ref stringEnd, ref attributes,
                        ref recordFlags))
                {
                    ioError = (int)DOS.Error.NoFreeStore;
                    result = DOS.RETURN_FAIL;
                    break;
                }
            }
            APTR.WriteUInt32(record, 16, recordFlags);
            count++;

            if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
            {
                ioError = (int)DOS.Error.Break;
                result = DOS.RETURN_FAIL;
                break;
            }
        }
        DOS.UnLockDosList(lockFlags);
        if (ioError != 0) return result;

        var cells = default(Cells);
        var processCells = default(ProcessCells);
        var tripleCells = default(TripleCells);
        for (var index = 0u; index < count; index++)
        {
            var record = APTR.FromPointer(buffer.Raw + index * recordBytes);
            cells.Name = APTR.ReadUInt32(record, 0);
            cells.Address = APTR.ReadUInt32(record, 4);
            var recordKind = APTR.ReadUInt32(record, 16);
            var isAssign = (recordKind & 0xffu) > 8u;
            if (isAssign)
            {
                RenderAssign(record, ref cells, ref processCells,
                    ref tripleCells, verbose);
                if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                {
                    ioError = (int)DOS.Error.Break;
                    return DOS.RETURN_FAIL;
                }
                continue;
            }
            if (cells.Name != 0 && isAssign)
                DOS.VPrintf("<%s> AssignNode 0x%lx\n",
                    Cells.AddressOf(ref cells));
            else if (cells.Name != 0)
                DOS.VPrintf("<%s> DeviceNode 0x%lx\n",
                    Cells.AddressOf(ref cells));
            else if (isAssign)
                DOS.VPrintf("AssignNode 0x%lx\n",
                    APTR.FromPointer(
                        Cells.AddressOf(ref cells).Raw + 4u));
            else
                DOS.VPrintf("DeviceNode 0x%lx\n",
                    APTR.FromPointer(
                        Cells.AddressOf(ref cells).Raw + 4u));

            processCells.Process = APTR.ReadUInt32(record, 8);
            if (processCells.Process == 0)
            {
                if ((recordKind & 0x100u) != 0)
                    DOS.FPuts(DOS.Output(), "  Not Mounted\n");
            }
            else
            {
                processCells.Name = APTR.ReadUInt32(record, 12);
                DOS.VPrintf("  Process 0x%lx <%s>\n",
                    ProcessCells.AddressOf(ref processCells));
            }

            if (!verbose) continue;

            if (kind == DosListLockFlags.Devices)
                RenderDeviceVerbose(record, ref tripleCells);
            else if (kind == DosListLockFlags.Volumes)
                RenderVolumeVerbose(record, ref tripleCells);

            if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
            {
                ioError = (int)DOS.Error.Break;
                return DOS.RETURN_FAIL;
            }
        }
        return result;
    }

    private static bool CaptureAssign(APTR node, APTR record,
        APTR scratch, uint recordEnd, ref uint stringEnd, bool verbose,
        ref AttributeCells cells, ref uint recordFlags, ref bool portFound,
        ref uint processAddress, ref uint processName)
    {
        var assignAddress = AttributeCells.AddressOf(ref cells);
        cells.NameBuffer = scratch.Raw;
        cells.NameLength = NameScratchBytes;
        cells.NameTag = (uint)DosObjectTag.AssignNodeAssignName;
        cells.NameData = assignAddress.Raw;
        cells.PortTag = 0;
        APTR.WriteUInt8(scratch, 0, 0);
        if (DOS.GetDosObjectAttrTagList(
                (uint)DosObjectType.AssignNode, node,
                APTR.FromPointer(assignAddress.Raw + 16u)) != 0)
        {
            recordFlags |= AssignNameAttributeFoundFlag;
            var returnedName = cells.NameBuffer;
            if (returnedName != 0)
            {
                if (!TryCopyCString(APTR.FromPointer(returnedName),
                        recordEnd, ref stringEnd,
                        out var assignNameCopy)) return false;
                recordFlags |= AssignNamePresentFlag;
                APTR.WriteUInt32(record,
                    unchecked((int)AssignNameOffset), assignNameCopy);
            }
            else
            {
                portFound = TryGetNodePort(DosListLockFlags.Assigns,
                    node, ref cells);
                if (portFound)
                {
                    recordFlags |= PortAttributeFoundFlag;
                    processAddress = ResolvePortProcess(
                        APTR.FromPointer(cells.PortValue));
                    if (processAddress != 0)
                    {
                        var processNameRaw = APTR.ReadUInt32(
                            APTR.FromPointer(processAddress),
                            ExecLayout.Node.Name);
                        if (processNameRaw == 0)
                            processAddress = 0;
                        else if (!TryCopyCString(
                            APTR.FromPointer(processNameRaw), recordEnd,
                            ref stringEnd, out processName)) return false;
                    }
                }

                if (TryGetScalarAttribute(DosObjectType.AssignNode, node,
                        DosObjectTag.AssignNodeLock, ref cells,
                        out var lockBptr))
                {
                    recordFlags |= AssignLockAttributeFoundFlag;
                    APTR.WriteUInt32(record,
                        unchecked((int)AssignLockOffset), lockBptr);
                    if (TryCaptureLockName(BPTR.FromRaw(lockBptr), scratch,
                            recordEnd, ref stringEnd, out var lockName))
                    {
                        recordFlags |= AssignLockNameFoundFlag;
                        APTR.WriteUInt32(record,
                            unchecked((int)AssignLockNameOffset), lockName);
                    }
                }
            }
        }

        if (verbose && TryGetScalarAttribute(DosObjectType.AssignNode,
                node, DosObjectTag.AssignNodeType, ref cells,
                out var assignType))
            StoreVerboseField(record, AssignTypeField, assignType);

        return CaptureAssignList(node, record, scratch, recordEnd,
            ref stringEnd, verbose, ref cells);
    }

    private static bool CaptureAssignList(APTR node, APTR record,
        APTR scratch, uint recordEnd, ref uint stringEnd, bool verbose,
        ref AttributeCells cells)
    {
        if (!TryGetScalarAttribute(DosObjectType.AssignNode, node,
                DosObjectTag.AssignNodeAssignList, ref cells,
                out var assignList)) return true;

        var head = 0u;
        var tail = 0u;
        var remaining = (stringEnd - recordEnd) / AssignListRecordBytes;
        var current = assignList;
        while (current != 0)
        {
            if (remaining == 0) return false;
            remaining--;
            var lockBptr = APTR.ReadUInt32(APTR.FromPointer(current),
                DosLayout.AssignList.Lock);
            var nextAssign = APTR.ReadUInt32(APTR.FromPointer(current),
                DosLayout.AssignList.Next);
            if (!TryCaptureLockName(BPTR.FromRaw(lockBptr), scratch,
                    recordEnd, ref stringEnd, out var lockName))
                lockName = 0;

            var rowAddress = (stringEnd - AssignListRecordBytes) & ~3u;
            if (rowAddress < recordEnd) return false;
            stringEnd = rowAddress;
            var row = APTR.FromPointer(rowAddress);
            APTR.WriteUInt32(row, 0, 0);
            APTR.WriteUInt32(row, 4, lockBptr);
            APTR.WriteUInt32(row, 8, lockName);
            if (head == 0) head = rowAddress;
            else APTR.WriteUInt32(APTR.FromPointer(tail), 0, rowAddress);
            tail = rowAddress;

            if (verbose && lockBptr != 0)
            {
                var fileLock = APTR.FromPointer(lockBptr << 2);
                if (Exec.TypeOfMem(fileLock) != 0)
                {
                    var link = APTR.ReadUInt32(fileLock,
                        DosLayout.FileLock.Link);
                    var key = APTR.ReadUInt32(fileLock,
                        DosLayout.FileLock.Key);
                    var access = APTR.ReadUInt32(fileLock,
                        DosLayout.FileLock.Access);
                    var task = APTR.ReadUInt32(fileLock,
                        DosLayout.FileLock.Task);
                    var volume = APTR.ReadUInt32(fileLock,
                        DosLayout.FileLock.Volume);
                    APTR.WriteUInt32(row, 12, fileLock.Raw);
                    APTR.WriteUInt32(row, 16, link);
                    APTR.WriteUInt32(row, 20, key);
                    APTR.WriteUInt32(row, 24, access);
                    APTR.WriteUInt32(row, 36, volume);
                    var process = ResolvePortProcess(APTR.FromPointer(task));
                    if (process != 0)
                    {
                        var processNameRaw = APTR.ReadUInt32(
                            APTR.FromPointer(process), ExecLayout.Node.Name);
                        if (processNameRaw != 0 &&
                            TryCopyCString(APTR.FromPointer(processNameRaw),
                                recordEnd, ref stringEnd,
                                out var copiedProcessName))
                        {
                            APTR.WriteUInt32(row, 28, process);
                            APTR.WriteUInt32(row, 32,
                                copiedProcessName);
                        }
                        else if (processNameRaw != 0) return false;
                    }
                }
            }
            current = nextAssign;
        }

        APTR.WriteUInt32(record,
            unchecked((int)AssignListHeadOffset), head);
        return true;
    }

    private static bool TryCaptureLockName(BPTR lockBptr, APTR scratch,
        uint recordEnd, ref uint stringEnd, out uint copiedName)
    {
        copiedName = 0;
        if (lockBptr.IsNull) return false;
        for (var index = 0u; index < NameScratchBytes; index++)
            APTR.WriteUInt8(scratch, unchecked((int)index), 0);
        if (DOS.NameFromLock(lockBptr, scratch,
                unchecked((int)NameScratchBytes - 1)) == 0) return false;
        APTR.WriteUInt8(scratch, unchecked((int)NameScratchBytes - 1), 0);
        return TryCopyCString(scratch, recordEnd, ref stringEnd,
            out copiedName);
    }

    private static void RenderAssign(APTR record, ref Cells cells,
        ref ProcessCells processCells, ref TripleCells tripleCells,
        bool verbose)
    {
        var name = APTR.ReadUInt32(record, 0);
        var node = APTR.ReadUInt32(record, 4);
        var flags = APTR.ReadUInt32(record, 16);
        var assignName = APTR.ReadUInt32(record,
            unchecked((int)AssignNameOffset));
        if (name != 0)
        {
            cells.Name = name;
            cells.Address = node;
            DOS.VPrintf("<%s> AssignNode 0x%lx\n",
                Cells.AddressOf(ref cells));
            if ((flags & AssignNameAttributeFoundFlag) != 0 &&
                (flags & AssignNamePresentFlag) != 0)
            {
                tripleCells.First = name;
                tripleCells.Second = node;
                tripleCells.Third = assignName;
                DOS.VPrintf("<%s> AssignNode 0x%lx defered to <%s>\n",
                    TripleCells.AddressOf(ref tripleCells));
            }
            else if ((flags & AssignNameAttributeFoundFlag) != 0)
            {
                DOS.VPrintf("<%s> AssignNode 0x%lx\n",
                    Cells.AddressOf(ref cells));
                if ((flags & PortAttributeFoundFlag) != 0)
                {
                    processCells.Process = APTR.ReadUInt32(record, 8);
                    if (processCells.Process == 0)
                        DOS.FPuts(DOS.Output(),
                            "  Not Mounted\n");
                    else
                    {
                        processCells.Name = APTR.ReadUInt32(record, 12);
                        DOS.VPrintf("  Process 0x%lx <%s>\n",
                            ProcessCells.AddressOf(ref processCells));
                    }
                }
                if ((flags & AssignLockAttributeFoundFlag) != 0)
                {
                    var lockName = APTR.ReadUInt32(record,
                        unchecked((int)AssignLockNameOffset));
                    tripleCells.First = APTR.ReadUInt32(record,
                        unchecked((int)AssignLockOffset));
                    tripleCells.Second = lockName;
                    if ((flags & AssignLockNameFoundFlag) != 0)
                        DOS.VPrintf("  Lock 0x%lx <%s>\n",
                            TripleCells.AddressOf(ref tripleCells));
                    else
                        DOS.VPrintf("    Lock 0x%lx <Not Resolvable>\n",
                            APTR.FromPointer(
                                TripleCells.AddressOf(ref tripleCells).Raw));
                }
                else
                    DOS.FPuts(DOS.Output(),
                        "    Lock argument failed\n");
            }
        }

        if (verbose && HasVerboseField(record, AssignTypeField))
            DOS.VPrintf("  Type %ld\n",
                VerboseFieldAddress(record, AssignTypeField));

        var list = APTR.ReadUInt32(record,
            unchecked((int)AssignListHeadOffset));
        while (list != 0)
        {
            var row = APTR.FromPointer(list);
            var lockBptr = APTR.ReadUInt32(row, 4);
            var lockName = APTR.ReadUInt32(row, 8);
            tripleCells.First = lockBptr;
            tripleCells.Second = lockName;
            if (lockName != 0)
                DOS.VPrintf("  + Lock 0x%lx <%s>\n",
                    TripleCells.AddressOf(ref tripleCells));
            else
                DOS.VPrintf("  + Lock 0x%lx <Not Resolvable>\n",
                    TripleCells.AddressOf(ref tripleCells).Raw);

            if (verbose)
            {
                var fileLock = APTR.ReadUInt32(row, 12);
                if (fileLock != 0)
                {
                    tripleCells.First = fileLock;
                    tripleCells.Second = APTR.ReadUInt32(row, 16);
                    tripleCells.Third = APTR.ReadUInt32(row, 20);
                    DOS.VPrintf("    Link 0x%lx Key 0x%lx Access %ld\n",
                        TripleCells.AddressOf(ref tripleCells));
                    processCells.Process = APTR.ReadUInt32(row, 28);
                    if (processCells.Process == 0)
                        DOS.FPuts(DOS.Output(),
                            "    No Filesystem MsgPort\n");
                    else
                    {
                        processCells.Name = APTR.ReadUInt32(row, 32);
                        DOS.VPrintf("    Process 0x%lx <%s>\n",
                            ProcessCells.AddressOf(ref processCells));
                    }
                }
            }
            list = APTR.ReadUInt32(row, 0);
        }
    }

    private static bool CaptureDeviceVerbose(APTR node, APTR record,
        APTR scratch, uint recordEnd, ref uint stringEnd,
        ref AttributeCells cells)
    {
        if (!CaptureStringField(DosObjectType.DeviceNode, node,
                DosObjectTag.DeviceNodeHandler, HandlerField, true,
                record, scratch, recordEnd, ref stringEnd, ref cells,
                out _)) return false;
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DeviceNodeStackSize, StackSizeField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DeviceNodePriority, PriorityField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DeviceNodeSegmentList, SegmentListField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DeviceNodeGlobalVector, GlobalVectorField,
            record, ref cells);
        if (!CaptureStringField(DosObjectType.DeviceNode, node,
                DosObjectTag.DeviceNodeSerialId, SerialIdField, true,
                record, scratch, recordEnd, ref stringEnd, ref cells,
                out _)) return false;
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.FileSysStartupMessage, StartupMessageField,
            record, ref cells);

        if (!CaptureStringField(DosObjectType.DeviceNode, node,
                DosObjectTag.FileSysStartupDevice, StartupDeviceField,
                false, record, scratch, recordEnd, ref stringEnd,
                ref cells, out var startupDeviceFound)) return false;
        if (startupDeviceFound && TryGetScalarAttribute(
                DosObjectType.DeviceNode, node,
                DosObjectTag.FileSysStartupUnit, ref cells,
                out var startupUnit))
        {
            StoreVerboseField(record, StartupUnitField, startupUnit);
            if (TryGetScalarAttribute(DosObjectType.DeviceNode, node,
                    DosObjectTag.FileSysStartupFlags, ref cells,
                    out var startupFlags))
                StoreVerboseField(record, StartupFlagsField, startupFlags);
        }

        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironment, EnvironmentField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentSizeBlock, SizeBlockField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentSectorOrigin, SectorOriginField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentSurfaces, SurfacesField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentSectorsPerBlock,
            SectorsPerBlockField, record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentBlocksPerTrack,
            BlocksPerTrackField, record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentReservedBlocks, ReservedField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentPreAlloc, PreAllocField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentInterleave, InterleaveField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentLowCylinder, LowCylinderField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentHighCylinder, HighCylinderField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentNumberOfBuffers,
            NumberOfBuffersField, record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentBufferMemoryType,
            BufferMemoryTypeField, record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentMaximumTransfer,
            MaximumTransferField, record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentMask, MaskField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentBootPriority, BootPriorityField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentDosType, DosTypeField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentBaud, BaudField,
            record, ref cells);
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentBootBlocks, BootBlocksField,
            record, ref cells);
        if (!CaptureStringField(DosObjectType.DeviceNode, node,
                DosObjectTag.DosEnvironmentControl, ControlField, true,
                record, scratch, recordEnd, ref stringEnd, ref cells,
                out _)) return false;
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DosEnvironmentTableSize, TableSizeField,
            record, ref cells);
        if (!CaptureStringField(DosObjectType.DeviceNode, node,
                DosObjectTag.DeviceNodeStartup, StartupField, true,
                record, scratch, recordEnd, ref stringEnd, ref cells,
                out _)) return false;
        CaptureScalarField(DosObjectType.DeviceNode, node,
            DosObjectTag.DeviceNodeStartupValue, StartupValueField,
            record, ref cells);
        return true;
    }

    private static bool CaptureVolumeVerbose(APTR node, APTR record,
        APTR scratch, uint recordEnd, ref uint stringEnd,
        ref AttributeCells cells, ref uint recordFlags)
    {
        var dateTime = APTR.FromPointer(record.Raw + VolumeDateTimeOffset);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Stamp,
            0xffff_ffffu);
        APTR.WriteUInt32(dateTime,
            DosLayout.DateTime.Stamp + DosLayout.DateStamp.Minutes,
            0xffff_ffffu);
        APTR.WriteUInt32(dateTime,
            DosLayout.DateTime.Stamp + DosLayout.DateStamp.Ticks,
            0xffff_ffffu);

        var address = AttributeCells.AddressOf(ref cells);
        cells.PortValue = dateTime.Raw;
        cells.PortLength = DosLayout.DateStamp.Size;
        cells.PortTag = (uint)DosObjectTag.VolumeNodeDate;
        cells.PortData = address.Raw + 8u;
        cells.DoneTag = 0;
        cells.DoneData = 0;
        if (DOS.GetDosObjectAttrTagList(
                (uint)DosObjectType.VolumeNode, node,
                APTR.FromPointer(address.Raw + 24u)) != 0)
        {
            recordFlags |= VolumeDateAttributeFoundFlag;
            var timeBuffer = APTR.FromPointer(scratch.Raw);
            var dayBuffer = APTR.FromPointer(scratch.Raw + 64u);
            var dateBuffer = APTR.FromPointer(scratch.Raw + 128u);
            for (var index = 0u; index < 64u; index++)
            {
                APTR.WriteUInt8(timeBuffer, unchecked((int)index), 0);
                APTR.WriteUInt8(dayBuffer, unchecked((int)index), 0);
                APTR.WriteUInt8(dateBuffer, unchecked((int)index), 0);
            }
            APTR.WriteUInt8(dateTime, DosLayout.DateTime.Format,
                (byte)DosDateFormat.Dos);
            APTR.WriteUInt8(dateTime, DosLayout.DateTime.Flags, 0);
            APTR.WriteUInt32(dateTime, DosLayout.DateTime.Day,
                timeBuffer.Raw);
            APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date,
                dayBuffer.Raw);
            APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time,
                dateBuffer.Raw);
            if (DOS.DateToStr(dateTime) != 0)
            {
                if (!TryCopyCString(timeBuffer, recordEnd,
                        ref stringEnd, out var timeCopy) ||
                    !TryCopyCString(dayBuffer, recordEnd,
                        ref stringEnd, out var dayCopy) ||
                    !TryCopyCString(dateBuffer, recordEnd,
                        ref stringEnd, out var dateCopy)) return false;
                StoreVerboseField(record, VolumeTimeField, timeCopy);
                StoreVerboseField(record, VolumeDayField, dayCopy);
                StoreVerboseField(record, VolumeDateField, dateCopy);
                recordFlags |= VolumeDateFormatSucceededFlag;
            }
        }

        if (TryGetScalarAttribute(DosObjectType.VolumeNode, node,
                DosObjectTag.VolumeNodeDiskType, ref cells,
                out var diskType))
            StoreVerboseField(record, VolumeDiskTypeField, diskType);

        if (TryGetScalarAttribute(DosObjectType.VolumeNode, node,
                DosObjectTag.VolumeNodeLockList, ref cells,
                out var lockList))
        {
            var head = 0u;
            var tail = 0u;
            var current = lockList << 2;
            var remaining = (stringEnd - recordEnd) /
                VolumeLockRecordBytes;
            while (current != 0)
            {
                if (remaining == 0) return false;
                remaining--;
                var fileLock = APTR.FromPointer(current);
                if (!TryCaptureLockName(
                        BPTR.FromRaw(current >> 2), scratch, recordEnd,
                        ref stringEnd, out var lockName))
                    lockName = 0;
                var nextLock = APTR.ReadUInt32(fileLock,
                    DosLayout.FileLock.Link);
                var rowAddress = (stringEnd - VolumeLockRecordBytes) & ~3u;
                if (rowAddress < recordEnd) return false;
                stringEnd = rowAddress;
                var row = APTR.FromPointer(rowAddress);
                APTR.WriteUInt32(row, 0, 0);
                APTR.WriteUInt32(row, 4, current);
                APTR.WriteUInt32(row, 8, lockName);
                if (head == 0) head = rowAddress;
                else APTR.WriteUInt32(APTR.FromPointer(tail), 0,
                    rowAddress);
                tail = rowAddress;
                current = nextLock << 2;
            }
            StoreVerboseField(record, VolumeLockListField, head);
        }
        return true;
    }

    private static void CaptureScalarField(DosObjectType objectType,
        APTR node, DosObjectTag tag, uint field, APTR record,
        ref AttributeCells cells)
    {
        if (TryGetScalarAttribute(objectType, node, tag, ref cells,
                out var value))
            StoreVerboseField(record, field, value);
    }

    private static bool TryGetScalarAttribute(DosObjectType objectType,
        APTR node, DosObjectTag tag, ref AttributeCells cells,
        out uint value)
    {
        value = 0;
        var address = AttributeCells.AddressOf(ref cells);
        cells.PortValue = 0;
        cells.PortLength = 4;
        cells.PortTag = (uint)tag;
        cells.PortData = address.Raw + 8u;
        cells.DoneTag = 0;
        cells.DoneData = 0;
        if (DOS.GetDosObjectAttrTagList((uint)objectType, node,
                APTR.FromPointer(address.Raw + 24u)) == 0)
            return false;
        value = cells.PortValue;
        return true;
    }

    private static bool CaptureStringField(DosObjectType objectType,
        APTR node, DosObjectTag tag, uint field, bool skipEmpty,
        APTR record, APTR scratch, uint recordEnd, ref uint stringEnd,
        ref AttributeCells cells, out bool found)
    {
        var address = AttributeCells.AddressOf(ref cells);
        cells.NameBuffer = scratch.Raw;
        cells.NameLength = NameScratchBytes;
        cells.NameTag = (uint)tag;
        cells.NameData = address.Raw;
        cells.PortTag = 0;
        APTR.WriteUInt8(scratch, 0, 0);
        found = DOS.GetDosObjectAttrTagList((uint)objectType, node,
            APTR.FromPointer(address.Raw + 16u)) != 0;
        if (!found || (skipEmpty && APTR.ReadUInt8(scratch, 0) == 0))
            return true;
        if (!TryCopyCString(scratch, recordEnd, ref stringEnd,
                out var stringCopy)) return false;
        StoreVerboseField(record, field, stringCopy);
        return true;
    }

    private static void StoreVerboseField(APTR record, uint field,
        uint value)
    {
        var valueOffset = unchecked((int)(VerboseValuesOffset + field * 4u));
        APTR.WriteUInt32(record, valueOffset, value);
        var maskOffset = field < 32
            ? unchecked((int)VerboseMaskLowOffset)
            : unchecked((int)VerboseMaskHighOffset);
        var bit = 1u << (int)(field < 32 ? field : field - 32u);
        APTR.WriteUInt32(record, maskOffset,
            APTR.ReadUInt32(record, maskOffset) | bit);
    }

    private static bool HasVerboseField(APTR record, uint field)
    {
        var maskOffset = field < 32
            ? unchecked((int)VerboseMaskLowOffset)
            : unchecked((int)VerboseMaskHighOffset);
        var bit = 1u << (int)(field < 32 ? field : field - 32u);
        return (APTR.ReadUInt32(record, maskOffset) & bit) != 0;
    }

    private static APTR VerboseFieldAddress(APTR record, uint field) =>
        APTR.FromPointer(record.Raw + VerboseValuesOffset + field * 4u);

    private static void RenderDeviceVerbose(APTR record,
        ref TripleCells tripleCells)
    {
        if (HasVerboseField(record, HandlerField))
            DOS.VPrintf("  Handler <%s>\n",
                VerboseFieldAddress(record, HandlerField));
        if (HasVerboseField(record, StackSizeField))
            DOS.VPrintf("  StackSize %ld\n",
                VerboseFieldAddress(record, StackSizeField));
        if (HasVerboseField(record, PriorityField))
            DOS.VPrintf("  Priority %ld\n",
                VerboseFieldAddress(record, PriorityField));
        if (HasVerboseField(record, SegmentListField))
            DOS.VPrintf("  SegList 0x%lx\n",
                VerboseFieldAddress(record, SegmentListField));
        if (HasVerboseField(record, GlobalVectorField))
            DOS.VPrintf("  GlobalVec %ld\n",
                VerboseFieldAddress(record, GlobalVectorField));
        if (HasVerboseField(record, SerialIdField))
            DOS.VPrintf("  SerialID <%s>\n",
                VerboseFieldAddress(record, SerialIdField));
        if (HasVerboseField(record, StartupMessageField))
            DOS.VPrintf("  FileSysStartupMsg 0x%lx\n",
                VerboseFieldAddress(record, StartupMessageField));
        if (HasVerboseField(record, StartupDeviceField) &&
            HasVerboseField(record, StartupUnitField) &&
            HasVerboseField(record, StartupFlagsField))
        {
            tripleCells.First = APTR.ReadUInt32(record,
                unchecked((int)(VerboseValuesOffset + StartupDeviceField * 4u)));
            tripleCells.Second = APTR.ReadUInt32(record,
                unchecked((int)(VerboseValuesOffset + StartupUnitField * 4u)));
            tripleCells.Third = APTR.ReadUInt32(record,
                unchecked((int)(VerboseValuesOffset + StartupFlagsField * 4u)));
            DOS.VPrintf("    <%s:%ld> Flags 0x%lx\n",
                TripleCells.AddressOf(ref tripleCells));
        }
        if (HasVerboseField(record, EnvironmentField))
            DOS.VPrintf("  Environment 0x%lx\n",
                VerboseFieldAddress(record, EnvironmentField));
        RenderDeviceScalar(record, SizeBlockField, "    SizeBlock %ld\n");
        RenderDeviceScalar(record, SectorOriginField, "    SecOrg %ld\n");
        RenderDeviceScalar(record, SurfacesField, "    Surfaces %ld\n");
        RenderDeviceScalar(record, SectorsPerBlockField,
            "    SectorsPerBlock %ld\n");
        RenderDeviceScalar(record, BlocksPerTrackField,
            "    BlocksPerTrack %ld\n");
        RenderDeviceScalar(record, ReservedField, "    Reserved %ld\n");
        RenderDeviceScalar(record, PreAllocField, "    PreAlloc %ld\n");
        RenderDeviceScalar(record, InterleaveField,
            "    Interleave %ld\n");
        RenderDeviceScalar(record, LowCylinderField, "    LowCyl %ld\n");
        RenderDeviceScalar(record, HighCylinderField, "    HighCyl %ld\n");
        RenderDeviceScalar(record, NumberOfBuffersField,
            "    NumBuffers %ld\n");
        RenderDeviceScalar(record, BufferMemoryTypeField,
            "    BufMemType %ld\n");
        RenderDeviceScalar(record, MaximumTransferField,
            "    MaxTransfer 0x%lx\n");
        RenderDeviceScalar(record, MaskField, "    Mask 0x%lx\n");
        RenderDeviceScalar(record, BootPriorityField, "    BootPri %ld\n");
        RenderDeviceScalar(record, DosTypeField, "    DosType 0x%lx\n");
        RenderDeviceScalar(record, BaudField, "    Baud %ld\n");
        RenderDeviceScalar(record, BootBlocksField,
            "    BootBlocks %ld\n");
        if (HasVerboseField(record, ControlField))
            DOS.VPrintf("    Control <%s>\n",
                VerboseFieldAddress(record, ControlField));
        RenderDeviceScalar(record, TableSizeField,
            "    TableSize %ld\n");
        if (HasVerboseField(record, StartupField))
            DOS.VPrintf("  Startup <%s>\n",
                VerboseFieldAddress(record, StartupField));
        RenderDeviceScalar(record, StartupValueField,
            "  Startup %ld\n");
    }

    private static void RenderVolumeVerbose(APTR record,
        ref TripleCells tripleCells)
    {
        var recordFlags = APTR.ReadUInt32(record, 16);
        if ((recordFlags & VolumeDateAttributeFoundFlag) != 0)
        {
            if ((recordFlags & VolumeDateFormatSucceededFlag) != 0)
            {
                tripleCells.First = APTR.ReadUInt32(record,
                    unchecked((int)(VerboseValuesOffset +
                        VolumeTimeField * 4u)));
                tripleCells.Second = APTR.ReadUInt32(record,
                    unchecked((int)(VerboseValuesOffset +
                        VolumeDayField * 4u)));
                tripleCells.Third = APTR.ReadUInt32(record,
                    unchecked((int)(VerboseValuesOffset +
                        VolumeDateField * 4u)));
                DOS.VPrintf("  %s %s %s\n",
                    TripleCells.AddressOf(ref tripleCells));
            }
            else
            {
                DOS.FPuts(DOS.Output(), "  <illegal date>\n");
                var dateTime = APTR.FromPointer(record.Raw +
                    VolumeDateTimeOffset);
                tripleCells.First = APTR.ReadUInt32(dateTime,
                    DosLayout.DateTime.Stamp + DosLayout.DateStamp.Days);
                tripleCells.Second = APTR.ReadUInt32(dateTime,
                    DosLayout.DateTime.Stamp + DosLayout.DateStamp.Minutes);
                tripleCells.Third = APTR.ReadUInt32(dateTime,
                    DosLayout.DateTime.Stamp + DosLayout.DateStamp.Ticks);
                DOS.VPrintf("  Days %ld Minutes %ld Ticks %ld\n",
                    TripleCells.AddressOf(ref tripleCells));
            }
        }
        if (HasVerboseField(record, VolumeDiskTypeField))
            DOS.VPrintf("  DiskType 0x%lx\n",
                VerboseFieldAddress(record, VolumeDiskTypeField));
        if (!HasVerboseField(record, VolumeLockListField)) return;

        var list = APTR.ReadUInt32(record,
            unchecked((int)(VerboseValuesOffset +
                VolumeLockListField * 4u)));
        while (list != 0)
        {
            var row = APTR.FromPointer(list);
            tripleCells.First = APTR.ReadUInt32(row, 4);
            tripleCells.Second = APTR.ReadUInt32(row, 8);
            if (tripleCells.Second != 0)
                DOS.VPrintf("    Lock 0x%lx <%s>\n",
                    TripleCells.AddressOf(ref tripleCells));
            else
                DOS.VPrintf("    Lock 0x%lx <Not Resolvable>\n",
                    TripleCells.AddressOf(ref tripleCells));
            list = APTR.ReadUInt32(row, 0);
        }
    }

    private static void RenderDeviceScalar(APTR record, uint field,
        CString format)
    {
        if (HasVerboseField(record, field))
            DOS.VPrintf(format, VerboseFieldAddress(record, field));
    }

    private static bool TryGetNodeName(DosListLockFlags kind,
        APTR node, APTR scratch, ref AttributeCells cells)
    {
        var objectType = kind switch
        {
            DosListLockFlags.Devices => DosObjectType.DeviceNode,
            DosListLockFlags.Volumes => DosObjectType.VolumeNode,
            _ => DosObjectType.AssignNode,
        };
        var nameTag = kind switch
        {
            DosListLockFlags.Devices => DosObjectTag.DeviceNodeName,
            DosListLockFlags.Volumes => DosObjectTag.VolumeNodeName,
            _ => DosObjectTag.AssignNodeName,
        };
        var address = AttributeCells.AddressOf(ref cells);
        cells.NameBuffer = scratch.Raw;
        cells.NameLength = NameScratchBytes;
        cells.NameTag = (uint)nameTag;
        cells.NameData = address.Raw;
        cells.PortTag = 0;
        APTR.WriteUInt8(scratch, 0, 0);

        var found = DOS.GetDosObjectAttrTagList((uint)objectType, node,
            APTR.FromPointer(address.Raw + 16u)) != 0;
        if (!found) APTR.WriteUInt8(scratch, 0, 0);
        return found;
    }

    private static bool TryGetNodePort(DosListLockFlags kind,
        APTR node, ref AttributeCells cells)
    {
        var objectType = kind switch
        {
            DosListLockFlags.Devices => DosObjectType.DeviceNode,
            DosListLockFlags.Volumes => DosObjectType.VolumeNode,
            _ => DosObjectType.AssignNode,
        };
        var portTag = kind switch
        {
            DosListLockFlags.Devices => DosObjectTag.DeviceNodeMessagePort,
            DosListLockFlags.Volumes => DosObjectTag.VolumeNodeMessagePort,
            _ => DosObjectTag.AssignNodeMessagePort,
        };

        var address = AttributeCells.AddressOf(ref cells);
        cells.PortValue = 0;
        cells.PortLength = 4;
        cells.PortTag = (uint)portTag;
        cells.PortData = address.Raw + 8u;
        cells.DoneTag = 0;
        cells.DoneData = 0;
        return DOS.GetDosObjectAttrTagList((uint)objectType, node,
            APTR.FromPointer(address.Raw + 24u)) != 0;
    }

    private static bool NameMatches(uint nameRaw, APTR wanted)
    {
        if (wanted.IsNull) return true;
        for (var index = 0; index < 255; index++)
        {
            var candidate = APTR.ReadUInt8(APTR.FromPointer(nameRaw), index);
            var expected = APTR.ReadUInt8(wanted, index);
            if (Fold(candidate) != Fold(expected)) return false;
            if (candidate == 0) return true;
        }
        return false;
    }

    private static bool TryCopyCString(APTR source, uint recordEnd,
        ref uint stringEnd, out uint copy)
    {
        copy = 0;
        var length = 0u;
        while (length < NameScratchBytes - 1u &&
            APTR.ReadUInt8(source, unchecked((int)length)) != 0)
            length++;
        if (length == NameScratchBytes - 1u) return false;

        var required = length + 1u;
        if (recordEnd > stringEnd || required > stringEnd - recordEnd)
            return false;

        stringEnd -= required;
        copy = stringEnd;
        for (var index = 0u; index <= length; index++)
            APTR.WriteUInt8(APTR.FromPointer(copy), unchecked((int)index),
                APTR.ReadUInt8(source, unchecked((int)index)));
        return true;
    }

    private static uint ResolvePortProcess(APTR port)
    {
        if (port.IsNull || Exec.TypeOfMem(port) == 0) return 0;

        var action = (PortFlags)(APTR.ReadUInt8(port,
            ExecLayout.MsgPort.Flags) & (byte)PortFlags.ActionMask);
        var process = action == PortFlags.Signal
            ? APTR.ReadUInt32(port, ExecLayout.MsgPort.SignalTask)
            : port.Raw >= (uint)DosLayout.Process.MessagePort
                ? port.Raw - (uint)DosLayout.Process.MessagePort
                : 0u;
        if (process == 0 || Exec.TypeOfMem(APTR.FromPointer(process)) == 0)
            return 0;

        var nodeType = (NodeType)APTR.ReadUInt8(APTR.FromPointer(process),
            ExecLayout.Node.Type);
        return nodeType is NodeType.Task or NodeType.Process ? process : 0u;
    }

    private static byte Fold(byte value) => value is >= (byte)'a' and
        <= (byte)'z' ? (byte)(value - ((byte)'a' - (byte)'A')) : value;

    private static uint ReadSwitch(NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value) ? value : 0;

    private static APTR ReadPointer(NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value)
        ? APTR.FromPointer(value) : APTR.Null;
}
