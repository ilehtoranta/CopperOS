using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-observed MorphOS 50.1 Info command body. The public DOS-list and
/// Info paths are kept invocation-local; the entry owns startup provider
/// leases, while exact packed-binary output and alternate data providers
/// remain open.
/// </summary>
public static class NativeMorphOSInfoCommand
{
    public const string Template =
        "DISKS/S,VOLS=VOLUMES/S,GOODONLY/S,BLOCKS/S,VERBOSE/S,DEVICES/M";
    public const uint ResultCount = 6;

    private const uint BufferBytes = 8192;
    private const uint PatternBytes = 512;
    private const uint FilterPatternBytes = 128;
    private const uint NameBufferBytes = 108;
    private const uint RecordBytes = 36;
    private const uint DateFormatBytes = 64;
    private const int DateFormatOffset = 96;
    private const int DateOutputOffset = 160;
    private const int DateFormatHookOffset = 288;
    private const uint DosTypeTableMinimum = 16;
    private const uint DosDiskType = 0x444f5300;
    private const uint ListFlags = (uint)(DosListLockFlags.Read |
        DosListLockFlags.Assigns | DosListLockFlags.Volumes |
        DosListLockFlags.Devices);
    private const uint EntryFlags = (uint)(DosListLockFlags.Volumes |
        DosListLockFlags.Devices);
    private const uint CtrlCMask = 1u << 12;

    private struct NameCells
    {
        public uint Name;

        public static APTR AddressOf(ref NameCells cells) =>
            throw new System.NotSupportedException(
                "Info.NameCells.AddressOf is lowered by CopperSharp.");
    }

    // DOS writes encoded pattern bytes through this stack address; the fields
    // provide storage only and are never consumed as managed values.
#pragma warning disable CS0649
    private struct FilterPatternCells
    {
        public uint Word00;
        public uint Word01;
        public uint Word02;
        public uint Word03;
        public uint Word04;
        public uint Word05;
        public uint Word06;
        public uint Word07;
        public uint Word08;
        public uint Word09;
        public uint Word10;
        public uint Word11;
        public uint Word12;
        public uint Word13;
        public uint Word14;
        public uint Word15;
        public uint Word16;
        public uint Word17;
        public uint Word18;
        public uint Word19;
        public uint Word20;
        public uint Word21;
        public uint Word22;
        public uint Word23;
        public uint Word24;
        public uint Word25;
        public uint Word26;
        public uint Word27;
        public uint Word28;
        public uint Word29;
        public uint Word30;
        public uint Word31;

        public static APTR AddressOf(ref FilterPatternCells cells) =>
            throw new System.NotSupportedException(
                "Info.FilterPatternCells.AddressOf is lowered by CopperSharp.");
    }
#pragma warning restore CS0649

    private struct VolumeNameCells
    {
        public uint Name;
        public uint Mounted;

        public static APTR AddressOf(ref VolumeNameCells cells) =>
            throw new System.NotSupportedException(
                "Info.VolumeNameCells.AddressOf is lowered by CopperSharp.");
    }

    private struct VolumeDateCells
    {
        public uint Day;
        public uint Date;
        public uint Time;

        public static APTR AddressOf(ref VolumeDateCells cells) =>
            throw new System.NotSupportedException(
                "Info.VolumeDateCells.AddressOf is lowered by CopperSharp.");
    }

    private struct StatusCells
    {
        public uint Full;
        public uint Errors;
        public uint State;
        public uint Type;
        public uint Name;

        public static APTR AddressOf(ref StatusCells cells) =>
            throw new System.NotSupportedException(
                "Info.StatusCells.AddressOf is lowered by CopperSharp.");
    }

    private struct BlockCells
    {
        public uint TotalHigh;
        public uint TotalLow;
        public uint UsedHigh;
        public uint UsedLow;
        public uint FreeHigh;
        public uint FreeLow;
        public uint BlockSize;

        public static APTR AddressOf(ref BlockCells cells) =>
            throw new System.NotSupportedException(
                "Info.BlockCells.AddressOf is lowered by CopperSharp.");
    }

    private struct QuadValue
    {
        public uint High;
        public uint Low;

        public static APTR AddressOf(ref QuadValue value) =>
            throw new System.NotSupportedException(
                "Info.QuadValue.AddressOf is lowered by CopperSharp.");
    }

    private struct NumberCells
    {
        public uint Integer;
        public uint Fraction;
        public uint Suffix;

        public static APTR AddressOf(ref NumberCells value) =>
            throw new System.NotSupportedException(
                "Info.NumberCells.AddressOf is lowered by CopperSharp.");
    }

    private struct VerboseCells
    {
        public uint Name;
        public uint Unit;

        public static APTR AddressOf(ref VerboseCells cells) =>
            throw new System.NotSupportedException(
                "Info.VerboseCells.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Snapshots public volume/device names while the DOS list read lock is
    /// held, then performs filesystem Info calls and rendering after unlock.
    /// This preserves the source's lock lifetime without printing under it.
    /// </summary>
    public static int Run(uint locale, out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        var disks = ReadSwitch(arguments, 0);
        var volumes = ReadSwitch(arguments, 1);
        var goodOnly = ReadSwitch(arguments, 2) != 0;
        var blocks = ReadSwitch(arguments, 3) != 0;
        var verbose = ReadSwitch(arguments, 4) != 0;
        var filters = ReadPointer(arguments, 5);
        var hasFilters = filters.IsNotNull &&
            APTR.ReadUInt32(filters, 0) != 0;
        var result = DOS.RETURN_WARN;
        var error = 0;
        var buffer = APTR.Null;

        if (disks == 0 && volumes == 0 &&
            (filters.IsNull || APTR.ReadUInt32(filters, 0) == 0))
        {
            disks = 1;
            volumes = 1;
        }
        var showDevices = disks != 0 || hasFilters;
        var selectedIoError = 0;

        var filterPatternScratch = default(FilterPatternCells);
        if (!ValidateFilterPatterns(filters, ref filterPatternScratch))
        {
            error = (int)DOS.IoErr();
            arguments.Release();
            DOS.SetIoErr((DOS.Error)error);
            ioError = error;
            return DOS.RETURN_ERROR;
        }

        buffer = Exec.AllocVec(BufferBytes, (uint)Exec.MemoryFlags.Any);
        if (buffer.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            arguments.Release();
            DOS.SetIoErr((DOS.Error)error);
            ioError = error;
            return DOS.RETURN_FAIL;
        }

        var count = 0u;
        var cursor = DOS.LockDosList(ListFlags);
        var patternText = APTR.FromPointer(buffer.Raw + BufferBytes -
            PatternBytes * 2);
        var patternBuffer = APTR.FromPointer(buffer.Raw + BufferBytes -
            PatternBytes);
        var nameFromLockBuffer = APTR.FromPointer(patternText.Raw -
            NameBufferBytes);
        var nameEnd = nameFromLockBuffer.Raw;
        var listError = 0;
        if (cursor.IsNotNull)
        {
            var node = cursor;
            while (true)
            {
                node = DOS.NextDosEntry(node, EntryFlags);
                if (node.IsNull) break;

                var type = APTR.ReadUInt32(node,
                    DosLayout.DosList.Type);
                if (type != (uint)DosListType.Device &&
                    type != (uint)DosListType.Volume)
                    continue;

                // Inactive device nodes are deliberately skipped, as in the
                // released source's protection against broken mount entries.
                if (type == (uint)DosListType.Device &&
                    APTR.ReadUInt32(node, DosLayout.DosList.Task) == 0)
                    continue;

                var name = APTR.ReadUInt32(node, DosLayout.DosList.Name);
                if (name == 0) continue;
                var bstr = BPTR.FromRaw(name).Address;
                var length = APTR.ReadUInt8(bstr, 0);
                var recordEnd = buffer.Raw + count * RecordBytes +
                    RecordBytes;
                var required = (uint)length + 2u;
                if (recordEnd > nameEnd || required > nameEnd - recordEnd)
                {
                    listError = (int)DOS.Error.NoFreeStore;
                    break;
                }

                nameEnd -= required;
                var copied = APTR.FromPointer(nameEnd);
                for (var index = 0u; index < length; index++)
                    APTR.WriteUInt8(copied, unchecked((int)index),
                        APTR.ReadUInt8(bstr, unchecked((int)index + 1)));
                APTR.WriteUInt8(copied, length, (byte)':');
                APTR.WriteUInt8(copied, length + 1, 0);
                if (!MatchesFilters(filters, copied, patternText,
                        patternBuffer))
                {
                    nameEnd += required;
                    continue;
                }
                if (type == (uint)DosListType.Volume)
                    APTR.WriteUInt8(copied, length, 0);

                var startupRaw = type == (uint)DosListType.Device
                    ? APTR.ReadUInt32(node,
                        DosLayout.DeviceNode.Startup)
                    : 0;
                var startup = startupRaw != 0
                    ? BPTR.FromRaw(startupRaw).Address : APTR.Null;
                var startupDosType = DosDiskType;
                if (startup.Raw > 0x400)
                {
                    var unit = APTR.ReadUInt32(startup,
                        DosLayout.FileSysStartupMsg.Unit);
                    if ((unit & 0xff000000u) == 0)
                    {
                        var environment = BPTR.FromRaw(APTR.ReadUInt32(
                            startup, DosLayout.FileSysStartupMsg.Environment)).Address;
                        if (environment.IsNotNull &&
                            APTR.ReadUInt32(environment,
                                DosLayout.DosEnvec.TableSize) >=
                                DosTypeTableMinimum)
                        {
                            var environmentDosType = APTR.ReadUInt32(
                                environment, DosLayout.DosEnvec.DosType);
                            if (environmentDosType != 0)
                                startupDosType = environmentDosType;
                        }
                    }
                }

                var verboseCopy = 0u;
                var verboseUnit = 0u;
                var verboseMode = 0u;
                if (verbose && type == (uint)DosListType.Device &&
                    startup.Raw > 0x400)
                {
                    var unit = APTR.ReadUInt32(startup,
                        DosLayout.FileSysStartupMsg.Unit);
                    var deviceRaw = APTR.ReadUInt32(startup,
                        DosLayout.FileSysStartupMsg.Device);
                    if ((unit & 0xff000000u) == 0 && deviceRaw != 0)
                    {
                        var deviceBstr = BPTR.FromRaw(deviceRaw).Address;
                        var deviceLength = APTR.ReadUInt8(deviceBstr, 0);
                        var verboseRequired = (uint)deviceLength + 1u;
                        if (recordEnd <= nameEnd &&
                            verboseRequired <= nameEnd - recordEnd)
                        {
                            nameEnd -= verboseRequired;
                            var verboseAddress = APTR.FromPointer(nameEnd);
                            for (var index = 0u; index < deviceLength;
                                 index++)
                                APTR.WriteUInt8(verboseAddress,
                                    unchecked((int)index),
                                APTR.ReadUInt8(deviceBstr,
                                        unchecked((int)index + 1)));
                            APTR.WriteUInt8(verboseAddress, deviceLength, 0);
                            verboseCopy = verboseAddress.Raw;
                            verboseUnit = unit;
                            verboseMode = 1;
                        }
                    }
                    else
                    {
                        // The released source falls back to treating the
                        // startup structure itself as a BSTR when GetDevStr
                        // cannot provide a device/unit string.
                        var startupLength = APTR.ReadUInt8(startup, 0);
                        var verboseRequired = (uint)startupLength + 1u;
                        if (recordEnd <= nameEnd &&
                            verboseRequired <= nameEnd - recordEnd)
                        {
                            nameEnd -= verboseRequired;
                            var verboseAddress = APTR.FromPointer(nameEnd);
                            for (var index = 0u; index < startupLength;
                                 index++)
                                APTR.WriteUInt8(verboseAddress,
                                    unchecked((int)index),
                                    APTR.ReadUInt8(startup,
                                        unchecked((int)index + 1)));
                            APTR.WriteUInt8(verboseAddress, startupLength, 0);
                            verboseCopy = verboseAddress.Raw;
                            verboseMode = 2;
                        }
                    }
                }

                var record = APTR.FromPointer(buffer.Raw + count * RecordBytes);
                APTR.WriteUInt32(record, 0, copied.Raw);
                APTR.WriteUInt32(record, 4, type);
                APTR.WriteUInt32(record, 8, type == (uint)DosListType.Volume
                    ? APTR.ReadUInt32(node, DosLayout.DeviceList.DiskType)
                    : startupDosType);
                APTR.WriteUInt32(record, 12, node.Raw);
                APTR.WriteUInt32(record, 16, verboseCopy);
                APTR.WriteUInt32(record, 20, verboseUnit);
                APTR.WriteUInt32(record, 24,
                    type == (uint)DosListType.Volume
                        ? APTR.ReadUInt32(node,
                            DosLayout.DeviceList.VolumeDate)
                        : verboseMode);
                APTR.WriteUInt32(record, 28,
                    type == (uint)DosListType.Volume
                        ? APTR.ReadUInt32(node,
                            DosLayout.DeviceList.VolumeDate + 4)
                        : 0);
                APTR.WriteUInt32(record, 32,
                    type == (uint)DosListType.Volume
                        ? APTR.ReadUInt32(node,
                            DosLayout.DeviceList.VolumeDate + 8)
                        : 0);
                count++;

            }
            DOS.UnLockDosList(ListFlags);
        }

        if (listError != 0)
        {
            Exec.FreeVec(buffer);
            arguments.Release();
            DOS.SetIoErr((DOS.Error)listError);
            ioError = listError;
            return DOS.RETURN_FAIL;
        }

        SortRecords(buffer, count);
        result = RenderRecords(buffer, count, showDevices, volumes, goodOnly,
            blocks, verbose, locale, nameFromLockBuffer, patternText,
            patternBuffer, out selectedIoError, out error);

        Exec.FreeVec(buffer);
        arguments.Release();
        if (error != 0)
        {
            ioError = error;
            DOS.SetIoErr((DOS.Error)error);
            return DOS.RETURN_FAIL;
        }
        ioError = selectedIoError;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static int RenderRecords(APTR buffer, uint count,
        bool showDevices, uint volumes, bool goodOnly, bool blocks,
        bool verbose, uint locale, APTR nameFromLockBuffer,
        APTR patternText, APTR patternBuffer,
        out int selectedIoError, out int error)
    {
        selectedIoError = 0;
        error = 0;
        var result = DOS.RETURN_WARN;
        var deviceHeader = false;
        if (showDevices)
        {
            DOS.FPuts(DOS.Output(),
                "Unit     Size     Used     Free Full Errs   State        Type Name\n");
            deviceHeader = true;
        }

        var statusCells = default(StatusCells);
        var blockCells = default(BlockCells);
        var verboseCells = default(VerboseCells);
        var volumeNameCells = default(VolumeNameCells);
        var nameCells = default(NameCells);
        var volumeHeader = false;
        var volumeNameWidth = MaxNameLength(buffer, count,
            (uint)DosListType.Volume, 15);
        var volumeFormat = patternBuffer;
        BuildNameFormat(volumeFormat, volumeNameWidth + 1, true);
        var dateTime = patternText;
        var dateDay = APTR.FromPointer(dateTime.Raw + 32);
        var dateText = APTR.FromPointer(dateTime.Raw + 48);
        var dateClock = APTR.FromPointer(dateTime.Raw + 64);
        var dateFormat = APTR.FromPointer(dateTime.Raw + DateFormatOffset);
        var dateOutput = APTR.FromPointer(dateTime.Raw + DateOutputOffset);
        var dateFormatHook = APTR.FromPointer(dateTime.Raw +
            DateFormatHookOffset);
        var hasDateFormat = false;
        if (locale != 0 && DOS.GetVar(CString.ToUInt32("info_datetime"),
                dateFormat, (int)DateFormatBytes, 0) > 0)
            hasDateFormat = true;

        for (var pass = 0u; pass < 2; pass++)
        {
            for (var index = 0u; index < count; index++)
            {
                var record = APTR.FromPointer(buffer.Raw + index * RecordBytes);
                var type = APTR.ReadUInt32(record, 4);
                var isDevicePass = pass == 0;
                if (isDevicePass &&
                    (type != (uint)DosListType.Device || !showDevices))
                    continue;
                if (!isDevicePass &&
                    (type != (uint)DosListType.Volume || volumes == 0))
                    continue;

                var name = APTR.ReadUInt32(record, 0);
                if (isDevicePass)
                {
                var path = CString.FromPointer(name);
                if (DOS.IsFileSystem(path) == 0) continue;
                var locked = DOS.LockRaw(path, DOS.LockMode.Shared);
                if (locked.IsNull)
                {
                    var fault = (int)DOS.IoErr();
                    selectedIoError = fault;
                    if (!goodOnly)
                    {
                        DOS.VPrintf("%-4s", record);
                        DOS.PrintFault((DOS.Error)fault,
                            CString.ToUInt32("\t"));
                    }
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
                if (ok == 0)
                {
                    var fault = (int)DOS.IoErr();
                    selectedIoError = fault;
                    DOS.UnLock(locked);
                    Exec.FreeMem(info, InfoData.Size);
                    if (!goodOnly)
                    {
                        DOS.VPrintf("%-4s", record);
                        DOS.PrintFault((DOS.Error)fault,
                            CString.ToUInt32("\t"));
                    }
                    continue;
                }

                var infoTotal = APTR.ReadUInt32(info,
                    DosLayout.InfoData.NumberOfBlocks);
                var infoUsed = APTR.ReadUInt32(info,
                    DosLayout.InfoData.NumberOfBlocksUsed);
                if (infoUsed > infoTotal) infoUsed = infoTotal;
                var total = new QuadValue { High = 0, Low = infoTotal };
                var used = new QuadValue { High = 0, Low = infoUsed };
                var blockSize = APTR.ReadUInt32(info,
                    DosLayout.InfoData.BytesPerBlock);
                if (used.High > total.High ||
                    (used.High == total.High && used.Low > total.Low))
                    used = total;

                var dosVersion = APTR.ReadUInt16(DOS.DOSLibraryBase,
                    ExecLayout.Library.Version);
                var dosRevision = APTR.ReadUInt16(DOS.DOSLibraryBase,
                    ExecLayout.Library.Revision);
                var canQueryWideCounts = dosVersion > 51 ||
                    (dosVersion == 51 && dosRevision >= 8);
                var gotTotal = false;
                var gotUsed = false;
                if (canQueryWideCounts)
                {
                    gotTotal = DOS.GetFileSysAttr(path,
                        FileSystemQueryAttribute.NumBlocks,
                        QuadValue.AddressOf(ref total), 8) != 0;
                    gotUsed = DOS.GetFileSysAttr(path,
                        FileSystemQueryAttribute.NumBlocksUsed,
                        QuadValue.AddressOf(ref used), 8) != 0;
                    if (gotTotal && gotUsed && IsGreater(used, total))
                        used = total;
                    if (!gotTotal)
                        total = new QuadValue { High = 0, Low = infoTotal };
                    if (!gotUsed)
                        used = new QuadValue { High = 0, Low = infoUsed };
                }

                var sizeKb = MultiplyAndShiftToKiloBytes(total, blockSize);
                var usedKb = MultiplyAndShiftToKiloBytes(used, blockSize);
                var freeKb = IsGreaterOrEqual(sizeKb, usedKb)
                    ? Subtract(sizeKb, usedKb) : default;
                var full = PercentUsed(sizeKb, usedKb);
                statusCells.Full = full;
                statusCells.Errors = APTR.ReadUInt32(info,
                    DosLayout.InfoData.NumberOfSoftErrors);
                var diskState = APTR.ReadUInt32(info,
                    DosLayout.InfoData.DiskState);
                statusCells.State = diskState ==
                        (uint)DosDiskState.WriteProtected
                    ? CString.ToUInt32("read only")
                    : diskState == (uint)DosDiskState.Validating
                        ? CString.ToUInt32("validating")
                        : diskState == (uint)DosDiskState.Validated
                            ? CString.ToUInt32("read/write")
                            : CString.ToUInt32("");
                var diskType = APTR.ReadUInt32(info,
                    DosLayout.InfoData.DiskType);
                var deviceDosType = APTR.ReadUInt32(record, 8);
                if ((deviceDosType & DosDiskType) != DosDiskType)
                    diskType = deviceDosType;
                statusCells.Type = FileSystemName(diskType,
                    APTR.FromPointer(dateClock.Raw + 32)).Raw;
                DOS.VPrintf("%-4s", record);
                PrintNumber(sizeKb);
                PrintNumber(usedKb);
                PrintNumber(freeKb);
                var volumeName = APTR.FromPointer(
                    APTR.ReadUInt32(record, 0));
                if (DOS.NameFromLock(locked, nameFromLockBuffer,
                        (int)NameBufferBytes) != 0)
                {
                    var length = 0u;
                    while (length < NameBufferBytes &&
                        APTR.ReadUInt8(nameFromLockBuffer,
                            unchecked((int)length)) != 0)
                        length++;
                    if (length != 0 && length < NameBufferBytes &&
                        APTR.ReadUInt8(nameFromLockBuffer,
                            unchecked((int)length - 1)) == (byte)':')
                        APTR.WriteUInt8(nameFromLockBuffer,
                            unchecked((int)length - 1), 0);
                    volumeName = nameFromLockBuffer;
                }
                statusCells.Name = volumeName.Raw;
                DOS.VPrintf("%4ld%% %4ld %-11s%8s %s\n",
                    StatusCells.AddressOf(ref statusCells));
                var verboseName = APTR.ReadUInt32(record, 16);
                if (verbose && verboseName != 0)
                {
                    if (APTR.ReadUInt32(record, 24) == 1)
                    {
                        verboseCells.Name = verboseName;
                        verboseCells.Unit = APTR.ReadUInt32(record, 20);
                        DOS.VPrintf("  -> %s : %ld\n",
                            VerboseCells.AddressOf(ref verboseCells));
                    }
                    else
                    {
                        nameCells.Name = verboseName;
                        DOS.VPrintf("  -> %s\n",
                            NameCells.AddressOf(ref nameCells));
                    }
                }
                if (blocks)
                {
                    blockCells.TotalHigh = total.High;
                    blockCells.TotalLow = total.Low;
                    blockCells.UsedHigh = used.High;
                    blockCells.UsedLow = used.Low;
                    var freeBlocks = IsGreaterOrEqual(total, used)
                        ? Subtract(total, used) : default;
                    blockCells.FreeHigh = freeBlocks.High;
                    blockCells.FreeLow = freeBlocks.Low;
                    blockCells.BlockSize = blockSize;
                    DOS.VPrintf(
                        "\nTotal blocks: %-10llu  Blocks used: %llu\n" +
                        " Blocks free: %-10llu    Blocksize: %lu\n",
                        BlockCells.AddressOf(ref blockCells));
                }
                DOS.UnLock(locked);
                Exec.FreeMem(info, InfoData.Size);
                result = DOS.RETURN_OK;
            }
                else
                {
                if (!volumeHeader)
                {
                    if (deviceHeader)
                        DOS.FPuts(DOS.Output(), "\n");
                    DOS.FPuts(DOS.Output(), "Volumes available:\n");
                    volumeHeader = true;
                }
                volumeNameCells.Name = name;
                volumeNameCells.Mounted = CString.ToUInt32("[Mounted]");
                DOS.VPrintf(CString.FromPointer(volumeFormat.Raw),
                    VolumeNameCells.AddressOf(ref volumeNameCells));

                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Stamp,
                    APTR.ReadUInt32(record, 24));
                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Stamp + 4,
                    APTR.ReadUInt32(record, 28));
                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Stamp + 8,
                    APTR.ReadUInt32(record, 32));
                RenderVolumeDate(locale, hasDateFormat ? 1u : 0u,
                    record, dateTime, dateDay, dateText, dateClock,
                    dateFormat, dateOutput, dateFormatHook);

                var diskType = APTR.ReadUInt32(record, 8);
                if (diskType != 0)
                {
                    var diskTypeName = FileSystemName(diskType,
                        APTR.FromPointer(dateClock.Raw + 32));
                    nameCells.Name = diskTypeName.Raw;
                    DOS.VPrintf(" <%s>", NameCells.AddressOf(ref nameCells));
                }
                DOS.FPuts(DOS.Output(), "\n");
                }

                if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                {
                    error = (int)DOS.Error.Break;
                    break;
                }
            }
            if (error != 0) break;
        }

        return result;
    }

    private static void SortRecords(APTR buffer, uint count)
    {
        for (var index = 1u; index < count; index++)
        {
            var key = APTR.FromPointer(buffer.Raw + index * RecordBytes);
            var keyName = APTR.ReadUInt32(key, 0);
            var keyType = APTR.ReadUInt32(key, 4);
            var keyDosType = APTR.ReadUInt32(key, 8);
            var keyNode = APTR.ReadUInt32(key, 12);
            var keyVerbose = APTR.ReadUInt32(key, 16);
            var keyUnit = APTR.ReadUInt32(key, 20);
            var keyDay = APTR.ReadUInt32(key, 24);
            var keyDate = APTR.ReadUInt32(key, 28);
            var keyTime = APTR.ReadUInt32(key, 32);
            var insert = index;
            while (insert != 0)
            {
                var previous = APTR.FromPointer(buffer.Raw +
                    (insert - 1) * RecordBytes);
                var order = unchecked((uint)Utility.Stricmp(keyName,
                    APTR.ReadUInt32(previous, 0)));
                if (order != 0 && (order & 0x80000000u) == 0)
                    break;

                var destination = APTR.FromPointer(buffer.Raw +
                    insert * RecordBytes);
                for (var offset = 0u; offset < RecordBytes; offset += 4)
                    APTR.WriteUInt32(destination, unchecked((int)offset),
                        APTR.ReadUInt32(previous, unchecked((int)offset)));
                insert--;
            }

            var destinationKey = APTR.FromPointer(buffer.Raw +
                insert * RecordBytes);
            APTR.WriteUInt32(destinationKey, 0, keyName);
            APTR.WriteUInt32(destinationKey, 4, keyType);
            APTR.WriteUInt32(destinationKey, 8, keyDosType);
            APTR.WriteUInt32(destinationKey, 12, keyNode);
            APTR.WriteUInt32(destinationKey, 16, keyVerbose);
            APTR.WriteUInt32(destinationKey, 20, keyUnit);
            APTR.WriteUInt32(destinationKey, 24, keyDay);
            APTR.WriteUInt32(destinationKey, 28, keyDate);
            APTR.WriteUInt32(destinationKey, 32, keyTime);
        }
    }

    [M68kExport("copperos.morphos.info.formatdate.putchar")]
    public static void FormatDatePutChar(
        [M68kRegister(M68kRegister.A0)] APTR hook,
        [M68kRegister(M68kRegister.A1)] uint character)
    {
        var cursor = APTR.FromPointer(APTR.ReadUInt32(hook,
            UtilityLayout.Hook.Data));
        APTR.WriteUInt8(cursor, 0, unchecked((byte)character));
        APTR.WriteUInt32(hook, UtilityLayout.Hook.Data, cursor.Raw + 1);
    }

    private static void RenderVolumeDate(uint locale, uint hasDateFormat,
        APTR record, APTR dateTime, APTR dateDay, APTR dateText,
        APTR dateClock, APTR dateFormat, APTR dateOutput,
        APTR dateFormatHook)
    {
        if (hasDateFormat != 0)
        {
            APTR.WriteUInt8(dateOutput, 0, 0);
            APTR.WriteUInt32(dateFormatHook, UtilityLayout.Hook.Entry,
                APTR.ExportAddress(
                    "copperos.morphos.info.formatdate.putchar").Raw);
            APTR.WriteUInt32(dateFormatHook, UtilityLayout.Hook.SubEntry, 0);
            APTR.WriteUInt32(dateFormatHook, UtilityLayout.Hook.Data,
                dateOutput.Raw);
            Locale.FormatDate(locale, CString.FromPointer(dateFormat.Raw),
                record.Raw + 24, dateFormatHook.Raw);
            var dateOutputEnd = APTR.ReadUInt32(dateFormatHook,
                UtilityLayout.Hook.Data);
            APTR.WriteUInt8(APTR.FromPointer(dateOutputEnd), 0, 0);
            DOS.FPuts(DOS.Output(), CString.FromPointer(dateOutput.Raw));
            return;
        }

        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Format,
            (byte)DosDateFormat.Dos);
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Flags, 0);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Day, dateDay.Raw);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, dateText.Raw);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, dateClock.Raw);
        if (DOS.DateToStr(dateTime.Raw) != 0)
        {
            var cells = new VolumeDateCells
            {
                Day = dateDay.Raw,
                Date = dateText.Raw,
                Time = dateClock.Raw
            };
            DOS.VPrintf("created %11s, %-10s %s",
                VolumeDateCells.AddressOf(ref cells));
        }
    }

    private static uint ReadSwitch(NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value) ? value : 0;

    private static APTR ReadPointer(NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value)
        ? APTR.FromPointer(value) : APTR.Null;

    private static bool ValidateFilterPatterns(APTR filters,
        ref FilterPatternCells scratch)
    {
        if (filters.IsNull || APTR.ReadUInt32(filters, 0) == 0) return true;
        var buffer = FilterPatternCells.AddressOf(ref scratch);
        for (var index = 0u; index < 64; index++)
        {
            var raw = APTR.ReadUInt32(filters, unchecked((int)(index * 4)));
            if (raw == 0) break;
            if (DOS.ParsePatternNoCase(CString.FromPointer(raw), buffer,
                    (int)FilterPatternBytes) == -1)
                return false;
        }
        return true;
    }

    private static bool MatchesFilters(APTR filters, APTR text,
        APTR patternText, APTR patternBuffer)
    {
        if (filters.IsNull || APTR.ReadUInt32(filters, 0) == 0) return true;
        for (var index = 0u; index < 64; index++)
        {
            var raw = APTR.ReadUInt32(filters, unchecked((int)(index * 4)));
            if (raw == 0) break;
            var filter = APTR.FromPointer(raw);
            var filterLength = 0u;
            while (filterLength < PatternBytes - 2 &&
                APTR.ReadUInt8(filter, unchecked((int)filterLength)) != 0)
                filterLength++;
            if (filterLength >= PatternBytes - 2) continue;
            for (var character = 0u; character < filterLength; character++)
                APTR.WriteUInt8(patternText, unchecked((int)character),
                    APTR.ReadUInt8(filter, unchecked((int)character)));
            if (filterLength == 0 ||
                APTR.ReadUInt8(filter, unchecked((int)filterLength - 1)) !=
                    (byte)':')
            {
                APTR.WriteUInt8(patternText,
                    unchecked((int)filterLength), (byte)':');
                filterLength++;
            }
            APTR.WriteUInt8(patternText,
                unchecked((int)filterLength), 0);
            if (DOS.ParsePatternNoCase(
                    CString.FromPointer(patternText), patternBuffer,
                    unchecked((int)PatternBytes)) < 0)
                continue;
            if (DOS.MatchPatternNoCase(
                    CString.FromPointer(patternBuffer),
                    CString.FromPointer(text)) != 0)
                return true;
        }
        return false;
    }

    private static uint MaxNameLength(APTR buffer, uint count, uint type,
        uint minimum)
    {
        var maximum = minimum;
        for (var index = 0u; index < count; index++)
        {
            var record = APTR.FromPointer(buffer.Raw + index * RecordBytes);
            if (APTR.ReadUInt32(record, 4) != type) continue;
            var name = APTR.FromPointer(APTR.ReadUInt32(record, 0));
            var length = 0u;
            while (length < 255 &&
                APTR.ReadUInt8(name, unchecked((int)length)) != 0)
                length++;
            if (length > maximum) maximum = length;
        }
        return maximum;
    }

    private static void BuildNameFormat(APTR buffer, uint width,
        bool mountedColumn)
    {
        var offset = 0;
        APTR.WriteUInt8(buffer, offset++, (byte)'%');
        APTR.WriteUInt8(buffer, offset++, (byte)'-');
        var divisor = 1u;
        while (divisor <= width / 10u) divisor *= 10u;
        while (divisor != 0)
        {
            var digit = (width / divisor) % 10u;
            APTR.WriteUInt8(buffer, offset++, unchecked((byte)('0' + digit)));
            divisor /= 10u;
        }
        APTR.WriteUInt8(buffer, offset++, (byte)'s');
        if (mountedColumn)
        {
            APTR.WriteUInt8(buffer, offset++, (byte)'%');
            APTR.WriteUInt8(buffer, offset++, (byte)'-');
            APTR.WriteUInt8(buffer, offset++, (byte)'1');
            APTR.WriteUInt8(buffer, offset++, (byte)'0');
            APTR.WriteUInt8(buffer, offset++, (byte)'s');
        }
        APTR.WriteUInt8(buffer, offset, 0);
    }

    private static APTR FileSystemName(uint diskType, APTR fallback)
    {
        // Keep this mapping in the order of MorphOS 3.20's GetFSysStr table.
        // A few upstream IDs are duplicated, so the first matching label wins.
        if (diskType == 0x444f5300) return APTR.FromPointer(
            CString.ToUInt32("OFS"));
        if (diskType == 0x444f5301) return APTR.FromPointer(
            CString.ToUInt32("FFS"));
        if (diskType == 0x444f5302) return APTR.FromPointer(
            CString.ToUInt32("OFS-INT"));
        if (diskType == 0x444f5303) return APTR.FromPointer(
            CString.ToUInt32("FFS-INT"));
        if (diskType == 0x444f5304) return APTR.FromPointer(
            CString.ToUInt32("OFS-DC"));
        if (diskType == 0x444f5305) return APTR.FromPointer(
            CString.ToUInt32("FFS-DC"));
        if (diskType == 0x444f5306) return APTR.FromPointer(
            CString.ToUInt32("OFS-LNFS"));
        if (diskType == 0x444f5307) return APTR.FromPointer(
            CString.ToUInt32("FFS-LNFS"));
        if (diskType == 0x4d534400) return APTR.FromPointer(
            CString.ToUInt32("MS-DOS"));
        if (diskType == 0x41434400 || diskType == 0x43443031 ||
            diskType == 0x662dabac)
            return APTR.FromPointer(CString.ToUInt32("CDFS"));
        if (diskType == 0x4e444f53)
            return APTR.FromPointer(CString.ToUInt32("NO DOS"));
        if (diskType == 0x4d414300)
            return APTR.FromPointer(CString.ToUInt32("Mac"));
        if (diskType == 0x4d4e5801)
            return APTR.FromPointer(CString.ToUInt32("Minix"));
        if (diskType == 0x514c3541)
            return APTR.FromPointer(CString.ToUInt32("QL720k"));
        if (diskType == 0x514c3542)
            return APTR.FromPointer(CString.ToUInt32("QL1.4M"));
        if (diskType == 0x43505c4d)
            return APTR.FromPointer(CString.ToUInt32("CP/M"));
        if (diskType == 0x5a585303)
            return APTR.FromPointer(CString.ToUInt32("+3Dos"));
        if (diskType == 0x5a585300)
            return APTR.FromPointer(CString.ToUInt32("Disciple "));
        if (diskType == 0x5a585301)
            return APTR.FromPointer(CString.ToUInt32("UniDos"));
        if (diskType == 0x5a585302)
            return APTR.FromPointer(CString.ToUInt32("SamDos"));
        if (diskType == 0x5a585304)
            return APTR.FromPointer(CString.ToUInt32("Opus"));
        if (diskType == 0x50324130)
            return APTR.FromPointer(CString.ToUInt32("NETWORK"));
        if (diskType == 0x53465300)
            return APTR.FromPointer(CString.ToUInt32("SFS"));
        if (diskType == 0x41465300)
            return APTR.FromPointer(CString.ToUInt32("AFS"));
        if (diskType == 0x50465300)
            return APTR.FromPointer(CString.ToUInt32("PFS"));
        if (diskType == 0x42464653)
            return APTR.FromPointer(CString.ToUInt32("BFFS"));
        if (diskType == 0x43444653)
            return APTR.FromPointer(CString.ToUInt32("CDFS"));
        // 0x43443031 is also the earlier CACHECDFS ID above, so the source
        // table returns CDFS before reaching its later CD-ISO entry.
        if (diskType == 0x43443030)
            return APTR.FromPointer(CString.ToUInt32("CD-HSF"));
        if (diskType == 0x43444441)
            return APTR.FromPointer(CString.ToUInt32("CDDA"));
        if (diskType == 0x45585402)
            return APTR.FromPointer(CString.ToUInt32("Ext2"));
        if (diskType == 0x58543203)
            return APTR.FromPointer(CString.ToUInt32("Ext3"));
        if (diskType == 0x4e544653)
            return APTR.FromPointer(CString.ToUInt32("NTFS"));
        if (diskType == 0x58465300)
            return APTR.FromPointer(CString.ToUInt32("SGI-XFS"));
        if (diskType == 0x4846532b)
            return APTR.FromPointer(CString.ToUInt32("Mac-HFS+"));
        if (diskType == 0x534d4200)
            return APTR.FromPointer(CString.ToUInt32("SmbFS"));
        if (diskType == 0x534d4202)
            return APTR.FromPointer(CString.ToUInt32("Smb2FS"));
        if (diskType == 0x54524600)
            return APTR.FromPointer(CString.ToUInt32("TrashFS"));

        APTR.WriteUInt32(fallback, 0, diskType);
        var last = APTR.ReadUInt8(fallback, 3);
        if (last < (byte)' ')
            APTR.WriteUInt8(fallback, 3, unchecked((byte)(last + '0')));
        APTR.WriteUInt8(fallback, 4, 0);
        return fallback;
    }

    private static uint PercentUsed(QuadValue total, QuadValue used)
    {
        if (total.High == 0 && total.Low == 0) return 0;
        if (IsGreater(used, total)) return 100;
        var shift = ScaleShift(total);
        var scaledTotal = ShiftToUInt(total, shift);
        var scaledUsed = ShiftToUInt(used, shift);
        if (scaledTotal == 0) return 0;
        return (scaledUsed * 100u + scaledTotal / 2u) / scaledTotal;
    }

    private static void PrintNumber(QuadValue kiloBytes)
    {
        var fields = default(NumberCells);
        if (kiloBytes.High == 0 && kiloBytes.Low < 1024)
        {
            fields.Integer = kiloBytes.Low;
            DOS.VPrintf("%8ldK", NumberCells.AddressOf(ref fields));
            return;
        }

        var shift = 0u;
        fields.Suffix = (uint)'M';
        if (kiloBytes.High >= 256)
        {
            shift = 30;
            fields.Suffix = (uint)'P';
        }
        else if (kiloBytes.High != 0 || kiloBytes.Low >= 1073741824)
        {
            shift = 20;
            fields.Suffix = (uint)'T';
        }
        else if (kiloBytes.Low >= 1048576)
        {
            shift = 10;
            fields.Suffix = (uint)'G';
        }

        var scaled = ShiftToUInt(kiloBytes, shift);
        var hundredths = ((scaled * 100u) >> 10);
        fields.Integer = hundredths / 100u;
        var remainder = hundredths % 100u;
        fields.Fraction = remainder / 10u;
        if (remainder % 10u >= 5u && ++fields.Fraction > 9u)
        {
            fields.Fraction = 0;
            fields.Integer++;
        }
        DOS.VPrintf("%6ld.%ld%lc", NumberCells.AddressOf(ref fields));
    }

    private static uint ScaleShift(QuadValue value)
    {
        if (value.High >= 256) return 30;
        if (value.High != 0 || value.Low >= 1073741824) return 20;
        if (value.Low >= 1048576) return 10;
        return 0;
    }

    private static uint ShiftToUInt(QuadValue value, uint shift)
    {
        if (shift == 30)
            return (value.High << 2) | (value.Low >> 30);
        if (shift == 20)
            return (value.High << 12) | (value.Low >> 20);
        if (shift == 10)
            return (value.High << 22) | (value.Low >> 10);
        return value.Low;
    }

    private static QuadValue MultiplyAndShiftToKiloBytes(
        QuadValue value, uint factor)
    {
        var lowProduct = Multiply32(value.Low, factor);
        var highProduct = Multiply32(value.High, factor);
        var productHigh = lowProduct.High + highProduct.Low;
        return new QuadValue
        {
            High = productHigh >> 10,
            Low = (productHigh << 22) | (lowProduct.Low >> 10)
        };
    }

    private static QuadValue Multiply32(uint left, uint right)
    {
        var leftLow = left & 0xffffu;
        var leftHigh = left >> 16;
        var rightLow = right & 0xffffu;
        var rightHigh = right >> 16;
        var lowProduct = leftLow * rightLow;
        var crossOne = leftLow * rightHigh;
        var crossTwo = leftHigh * rightLow;
        var lowAfterOne = lowProduct + (crossOne << 16);
        var carryOne = lowAfterOne < lowProduct ? 1u : 0u;
        var low = lowAfterOne + (crossTwo << 16);
        var carryTwo = low < lowAfterOne ? 1u : 0u;
        var high = leftHigh * rightHigh + (crossOne >> 16) +
            (crossTwo >> 16) + carryOne + carryTwo;
        return new QuadValue { High = high, Low = low };
    }

    private static bool IsGreater(QuadValue left, QuadValue right) =>
        left.High > right.High ||
        (left.High == right.High && left.Low > right.Low);

    private static bool IsEqual(QuadValue left, QuadValue right) =>
        left.High == right.High && left.Low == right.Low;

    private static bool IsGreaterOrEqual(QuadValue left, QuadValue right) =>
        !IsGreater(right, left);

    private static QuadValue Subtract(QuadValue left, QuadValue right)
    {
        var borrow = left.Low < right.Low ? 1u : 0u;
        return new QuadValue
        {
            High = left.High - right.High - borrow,
            Low = unchecked(left.Low - right.Low)
        };
    }
}
