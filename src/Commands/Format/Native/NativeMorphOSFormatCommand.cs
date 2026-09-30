using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 Format body derived from the inspected 50.9 source.  The
/// command keeps parser, device and trackdisk ownership inside one invocation;
/// it never formats a host path.  Filesystem handlers remain the authority for
/// quick/SFS initialization, while the classic trackdisk path owns the
/// destructive write/read verification.
/// </summary>
public static class NativeMorphOSFormatCommand
{
    public const string Template =
        "DEVICE=DRIVE/A/K,NAME/A/K,OFS/S,FFS/S,SFS/S,MSDOS/S," +
        "INTL=INTERNATIONAL=CASESENSITIVE/S," +
        "NOINTL=NOINTERNATIONAL/S,DIRCACHE/S,NODIRCACHE/S," +
        "LNFS=LONGFILENAMES/S,NOLNFS=NOLONGFILENAMES/S," +
        "NOICONS/S,QUICK/S,NORECYCLED/S,SHOWRECYCLED/S";
    public const uint ResultCount = 16;

    private const uint CtrlCMask = 1u << 12;
    private const uint ListFlags = (uint)(DosListLockFlags.Devices | DosListLockFlags.Read);
    private const uint DeviceFindFlags = (uint)DosListLockFlags.Devices;
    private const uint DeviceTaskOffset = 8;
    private const uint DeviceStartupOffset = 28;
    private const uint StartupUnitOffset = 0;
    private const uint StartupDeviceOffset = 4;
    private const uint StartupEnvironmentOffset = 8;
    private const uint StartupFlagsOffset = 12;
    private const uint DosTypeOffset = 64;
    private const uint DosTableSizeOffset = 0;
    private const uint SizeBlockOffset = 4;
    private const uint SurfacesOffset = 12;
    private const uint BlocksPerTrackOffset = 20;
    private const uint LowCylinderOffset = 36;
    private const uint HighCylinderOffset = 40;
    private const uint BufferMemoryTypeOffset = 48;
    private const uint MaximumTransferOffset = 52;
    private const uint MaskOffset = 56;

    private const uint DosDisk = 0x444F5300u;
    private const uint UnreadableDisk = 0x42414400u;
    private const uint FfsDisk = 0x444F5301u;
    private const uint SfsDisk = 0x53465300u;
    private const uint MsdosDisk = 0x4D53444Fu;
    private const uint SfsPacketBase = 0x00F0_0000u;
    private const int ActionSfsFormat = unchecked((int)(SfsPacketBase + 103));
    private const uint AsfBase = 0x8000_0000u;
    private const uint AsfName = AsfBase + 1;
    private const uint AsfNoRecycled = AsfBase + 2;
    private const uint AsfCaseSensitive = AsfBase + 3;
    private const uint AsfShowRecycled = AsfBase + 4;
    private const int DiskObjectCurrentXOffset = 58;
    private const int DiskObjectCurrentYOffset = 62;

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

        APTR deviceScratch = APTR.Null;
        APTR deviceName = APTR.Null;
        APTR messagePort = APTR.Null;
        APTR ioRequest = APTR.Null;
        APTR formatBuffer = APTR.Null;
        APTR infoBuffer = APTR.Null;
        APTR iconLibrary = APTR.Null;
        APTR iconPath = APTR.Null;
        uint deviceScratchBytes = 0;
        uint deviceNameBytes = 0;
        uint formatBufferBytes = 0;
        uint iconPathBytes = 0;
        var deviceOpen = false;
        var inhibited = false;
        var writesDone = false;
        var result = DOS.RETURN_ERROR;
        var error = 0;
        var fsPort = APTR.Null;

        do
        {
            if (!TryReadFlags(ref arguments, out var device, out var name,
                    out var ofs, out var ffs, out var sfs, out var msdos,
                    out var intl, out var noIntl, out var dircache,
                    out var noDircache, out var lnfs, out var noLnfs,
                    out var noIcons, out var quick, out var noRecycled,
                    out var showRecycled))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            var nameAddress = APTR.FromPointer(name);
            if (IsBanned(nameAddress))
            {
                error = (int)DOS.Error.InvalidComponentName;
                DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
                break;
            }

            var deviceAddress = APTR.FromPointer(device);
            var deviceLength = CStringLength(deviceAddress);
            if (deviceLength == 0 || deviceLength > 255)
            {
                error = (int)DOS.Error.InvalidComponentName;
                break;
            }

            deviceScratchBytes = deviceLength + 1;
            deviceScratch = Exec.AllocMem(deviceScratchBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (deviceScratch.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }
            CopyCString(deviceAddress, deviceScratch, deviceScratchBytes);
            for (var index = 0u; index < deviceLength; index++)
            {
                if (APTR.ReadUInt8(deviceScratch, unchecked((int)index)) == (byte)':')
                {
                    APTR.WriteUInt8(deviceScratch, unchecked((int)index), 0);
                    break;
                }
            }

            var list = DOS.LockDosList(ListFlags);
            var node = list.IsNull ? APTR.Null : DOS.FindDosEntry(list,
                CString.FromPointer(deviceScratch), DeviceFindFlags);
            if (node.IsNull)
            {
                if (list.IsNotNull) DOS.UnLockDosList(ListFlags);
                error = (int)DOS.Error.DeviceNotMounted;
                DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
                break;
            }

            fsPort = APTR.FromPointer(APTR.ReadUInt32(node,
                unchecked((int)DeviceTaskOffset)));
            var startup = BPTR.FromRaw(APTR.ReadUInt32(node,
                unchecked((int)DeviceStartupOffset))).Address;
            if (startup.Raw <= 0x400 ||
                (APTR.ReadUInt32(startup, unchecked((int)StartupUnitOffset)) & 0xFF00_0000u) != 0)
            {
                DOS.UnLockDosList(ListFlags);
                error = (int)DOS.Error.ObjectWrongType;
                break;
            }

            var startupDevice = BPTR.FromRaw(APTR.ReadUInt32(startup,
                unchecked((int)StartupDeviceOffset))).Address;
            var startupEnvironment = BPTR.FromRaw(APTR.ReadUInt32(startup,
                unchecked((int)StartupEnvironmentOffset))).Address;
            if (startupDevice.IsNull || startupEnvironment.IsNull)
            {
                DOS.UnLockDosList(ListFlags);
                error = (int)DOS.Error.ObjectWrongType;
                break;
            }

            var bstrLength = APTR.ReadUInt8(startupDevice, 0);
            if (bstrLength == 0)
            {
                DOS.UnLockDosList(ListFlags);
                error = (int)DOS.Error.ObjectWrongType;
                break;
            }
            deviceNameBytes = (uint)bstrLength + 1;
            deviceName = Exec.AllocMem(deviceNameBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (deviceName.IsNull)
            {
                DOS.UnLockDosList(ListFlags);
                error = (int)DOS.Error.NoFreeStore;
                break;
            }
            for (var index = 0u; index < bstrLength; index++)
                APTR.WriteUInt8(deviceName, unchecked((int)index),
                    APTR.ReadUInt8(startupDevice, unchecked((int)index + 1)));
            APTR.WriteUInt8(deviceName, bstrLength, 0);

            var tableSize = APTR.ReadUInt32(startupEnvironment,
                unchecked((int)DosTableSizeOffset));
            if (tableSize < 11)
            {
                DOS.UnLockDosList(ListFlags);
                error = (int)DOS.Error.ObjectWrongType;
                break;
            }
            var unit = APTR.ReadUInt32(startup, unchecked((int)StartupUnitOffset));
            var deviceFlags = APTR.ReadUInt32(startup,
                unchecked((int)StartupFlagsOffset));
            var sizeBlock = APTR.ReadUInt32(startupEnvironment,
                unchecked((int)SizeBlockOffset));
            var surfaces = APTR.ReadUInt32(startupEnvironment,
                unchecked((int)SurfacesOffset));
            var blocksPerTrack = APTR.ReadUInt32(startupEnvironment,
                unchecked((int)BlocksPerTrackOffset));
            var lowCylinder = APTR.ReadUInt32(startupEnvironment,
                unchecked((int)LowCylinderOffset));
            var highCylinder = APTR.ReadUInt32(startupEnvironment,
                unchecked((int)HighCylinderOffset));
            var bufferMemoryType = tableSize >= 13
                ? APTR.ReadUInt32(startupEnvironment,
                    unchecked((int)BufferMemoryTypeOffset))
                : (uint)Exec.MemoryFlags.Public;
            var maximumTransfer = tableSize >= 14
                ? APTR.ReadUInt32(startupEnvironment,
                    unchecked((int)MaximumTransferOffset))
                : 0x0000_FE00u;
            var mask = tableSize >= 15
                ? APTR.ReadUInt32(startupEnvironment,
                    unchecked((int)MaskOffset))
                : 0xFFFF_FFFCu;
            var dosType = tableSize >= 17
                ? APTR.ReadUInt32(startupEnvironment,
                    unchecked((int)DosTypeOffset))
                : 0;
            DOS.UnLockDosList(ListFlags);

            dosType = SelectDosType(dosType, ofs, ffs, sfs, msdos,
                intl, noIntl, dircache, noDircache, lnfs, noLnfs);
            if (dosType == 0)
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            var input = DOS.Input();
            var output = DOS.Output();
            DOS.PutStr(CString.FromPointer(deviceScratch));
            DOS.PutStr(" disk: insert disk to be formatted\n");
            DOS.Flush(output);
            if (input.IsNull || DOS.IsInteractive(input) == 0)
            {
                error = (int)DOS.Error.ObjectWrongType;
                break;
            }
            if (DOS.WaitForChar(input, 100) == 0)
            {
                if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                {
                    error = (int)DOS.Error.Break;
                    result = DOS.RETURN_WARN;
                }
                break;
            }
            _ = DOS.FGetC(input);

            if (fsPort.IsNull || DOS.DoPkt(fsPort,
                    (int)DosPacketAction.Inhibit, unchecked((int)uint.MaxValue),
                    0, 0, 0, 0) == 0)
            {
                error = (int)DOS.IoErr();
                if (error == 0) error = (int)DOS.Error.DiskWriteProtected;
                break;
            }
            inhibited = true;

            messagePort = Exec.CreateMsgPort();
            if (messagePort.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }
            ioRequest = Exec.CreateIORequest(messagePort, IOStdReq.Size);
            if (ioRequest.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }
            if (Exec.OpenDevice(CString.FromPointer(deviceName), unit, ioRequest,
                    deviceFlags) != 0)
            {
                error = (int)DOS.Error.InvalidResidentLibrary;
                break;
            }
            deviceOpen = true;

            if (quick != 0)
            {
                result = DOS.RETURN_OK;
            }
            else
            {
                result = FormatPartition(ioRequest, dosType, sizeBlock,
                    surfaces, blocksPerTrack, lowCylinder, highCylinder,
                    maximumTransfer, mask, bufferMemoryType, writesDone,
                    ref formatBuffer, ref formatBufferBytes, out writesDone,
                    out error) ? DOS.RETURN_OK : DOS.RETURN_FAIL;
            }

            if (result == DOS.RETURN_OK && sfs != 0)
            {
                result = SfsFormat(fsPort, nameAddress, intl, noRecycled,
                    showRecycled, out error) ? DOS.RETURN_OK : DOS.RETURN_FAIL;
            }
            else if (result == DOS.RETURN_OK)
            {
                result = QuickFormat(fsPort, nameAddress, dosType, out error)
                    ? DOS.RETURN_OK : DOS.RETURN_FAIL;
            }

            if (inhibited && fsPort.IsNotNull)
            {
                _ = DOS.DoPkt(fsPort, (int)DosPacketAction.Inhibit, 0, 0, 0, 0, 0);
                inhibited = false;
            }
            if (result == DOS.RETURN_OK)
            {
                infoBuffer = Exec.AllocMem(36u,
                    Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
                if (infoBuffer.IsNull)
                {
                    error = (int)DOS.Error.NoFreeStore;
                    result = DOS.RETURN_FAIL;
                }
                else if (!WaitValidated(fsPort, infoBuffer, dosType,
                    out error))
                {
                    result = DOS.RETURN_FAIL;
                }
            }
            if (result == DOS.RETURN_OK && noIcons == 0)
            {
                // The source ignores CreateIcons' boolean result after a
                // successful format; preserve that policy while retaining
                // every icon-library allocation for cleanup.
                _ = CreateIcons(nameAddress, ref iconLibrary, ref iconPath,
                    ref iconPathBytes, out _);
            }
            if (deviceOpen && writesDone)
            {
                SetRequest(ioRequest, TrackDiskCommand.Update, APTR.Null, 0, 0);
                Exec.DoIO(ioRequest);
                SetRequest(ioRequest, TrackDiskCommand.Clear, APTR.Null, 0, 0);
                Exec.DoIO(ioRequest);
            }
            if (deviceOpen)
            {
                SetRequest(ioRequest, TrackDiskCommand.Motor, APTR.Null, 0, 0);
                Exec.DoIO(ioRequest);
            }
        }
        while (false);

        if (inhibited && fsPort.IsNotNull)
            _ = DOS.DoPkt(fsPort, (int)DosPacketAction.Inhibit, 0, 0, 0, 0, 0);
        inhibited = false;
        if (deviceOpen && ioRequest.IsNotNull) Exec.CloseDevice(ioRequest);
        if (ioRequest.IsNotNull) Exec.DeleteIORequest(ioRequest);
        if (messagePort.IsNotNull) Exec.DeleteMsgPort(messagePort);
        if (infoBuffer.IsNotNull) Exec.FreeMem(infoBuffer, 36u);
        if (formatBuffer.IsNotNull) Exec.FreeMem(formatBuffer, formatBufferBytes);
        if (iconLibrary.IsNotNull)
        {
            Icon.IconLibraryBase = APTR.Null;
            Exec.CloseLibrary(iconLibrary);
        }
        if (iconPath.IsNotNull) Exec.FreeMem(iconPath, iconPathBytes);
        if (deviceName.IsNotNull) Exec.FreeMem(deviceName, deviceNameBytes);
        if (deviceScratch.IsNotNull) Exec.FreeMem(deviceScratch, deviceScratchBytes);
        arguments.Release();
        if (result == DOS.RETURN_ERROR && error == 0) error = (int)DOS.Error.ObjectWrongType;
        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool TryReadFlags(ref NativeCommandArguments arguments,
        out uint device, out uint name, out uint ofs, out uint ffs,
        out uint sfs, out uint msdos, out uint intl, out uint noIntl,
        out uint dircache, out uint noDircache, out uint lnfs, out uint noLnfs,
        out uint noIcons, out uint quick, out uint noRecycled,
        out uint showRecycled)
    {
        device = name = ofs = ffs = sfs = msdos = intl = noIntl = 0;
        dircache = noDircache = lnfs = noLnfs = noIcons = quick = 0;
        noRecycled = showRecycled = 0;
        return arguments.TryGetResult(0, out device) &&
            arguments.TryGetResult(1, out name) &&
            arguments.TryGetResult(2, out ofs) &&
            arguments.TryGetResult(3, out ffs) &&
            arguments.TryGetResult(4, out sfs) &&
            arguments.TryGetResult(5, out msdos) &&
            arguments.TryGetResult(6, out intl) &&
            arguments.TryGetResult(7, out noIntl) &&
            arguments.TryGetResult(8, out dircache) &&
            arguments.TryGetResult(9, out noDircache) &&
            arguments.TryGetResult(10, out lnfs) &&
            arguments.TryGetResult(11, out noLnfs) &&
            arguments.TryGetResult(12, out noIcons) &&
            arguments.TryGetResult(13, out quick) &&
            arguments.TryGetResult(14, out noRecycled) &&
            arguments.TryGetResult(15, out showRecycled);
    }

    private static uint SelectDosType(uint existing, uint ofs, uint ffs,
        uint sfs, uint msdos, uint intl, uint noIntl, uint dircache,
        uint noDircache, uint lnfs, uint noLnfs)
    {
        var value = existing;
        if (value == 0)
            value = sfs != 0 ? SfsDisk : msdos != 0 ? MsdosDisk :
                ofs != 0 ? DosDisk : FfsDisk;
        if ((value & 0xFFFF_FF00u) != DosDisk) return value;
        if (ofs != 0) value = DosDisk;
        else if (ffs != 0) value |= 1;
        if (intl != 0 && noIntl == 0) value |= 2;
        else if (noIntl != 0 && intl == 0) value &= ~2u;
        if (dircache != 0 && noDircache == 0) value |= 4;
        else if (noDircache != 0 && dircache == 0) value &= ~4u;
        if (lnfs != 0 && noLnfs == 0) value |= 6;
        else if (noLnfs != 0 && lnfs == 0) value &= ~4u;
        return value;
    }

    private static bool QuickFormat(APTR port, APTR name, uint dosType,
        out int error)
    {
        error = 0;
        var length = CStringLength(name);
        if (length >= 255) { error = (int)DOS.Error.InvalidComponentName; return false; }
        var bstr = Exec.AllocMem(length + 1,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (bstr.IsNull) { error = (int)DOS.Error.NoFreeStore; return false; }
        APTR.WriteUInt8(bstr, 0, unchecked((byte)length));
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(bstr, unchecked((int)index + 1),
                APTR.ReadUInt8(name, unchecked((int)index)));
        var packetResult = DOS.DoPkt2(port, (int)DosPacketAction.Format,
            unchecked((int)BPTR.FromAddress(bstr).Raw), unchecked((int)dosType));
        error = packetResult == 0 ? (int)DOS.IoErr() : 0;
        Exec.FreeMem(bstr, length + 1);
        return packetResult != 0;
    }

    private static bool SfsFormat(APTR port, APTR name, uint intl,
        uint noRecycled, uint showRecycled, out int error)
    {
        error = 0;
        var count = 1u + (intl != 0 ? 1u : 0u) +
            (noRecycled != 0 ? 1u : 0u) + (showRecycled != 0 ? 1u : 0u) + 1u;
        var storage = Exec.AllocMem(count * TagItem.Size,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (storage.IsNull) { error = (int)DOS.Error.NoFreeStore; return false; }
        var index = 0u;
        APTR.WriteUInt32(storage, 0, AsfName);
        APTR.WriteUInt32(storage, 4, name.Raw);
        index++;
        if (intl != 0) { WriteTag(storage, index++, AsfCaseSensitive, 1); }
        if (noRecycled != 0) { WriteTag(storage, index++, AsfNoRecycled, 1); }
        if (showRecycled != 0) { WriteTag(storage, index++, AsfShowRecycled, 1); }
        WriteTag(storage, index, 0, 0);
        var packetResult = DOS.DoPkt1(port, ActionSfsFormat,
            unchecked((int)storage.Raw));
        error = packetResult == 0 ? (int)DOS.IoErr() : 0;
        Exec.FreeMem(storage, count * TagItem.Size);
        return packetResult != 0;
    }

    private static void WriteTag(APTR address, uint index, uint tag, uint data)
    {
        var offset = unchecked((int)(index * TagItem.Size));
        APTR.WriteUInt32(address, offset, tag);
        APTR.WriteUInt32(address, offset + 4, data);
    }

    private static bool CreateIcons(APTR volume, ref APTR iconLibrary,
        ref APTR iconPath, ref uint iconPathBytes, out int error)
    {
        error = 0;
        iconLibrary = Exec.OpenLibraryRaw(Icon.Name, 37);
        if (iconLibrary.IsNull) return false;
        Icon.IconLibraryBase = iconLibrary;

        var length = CStringLength(volume);
        iconPathBytes = length + 11;
        iconPath = Exec.AllocMem(iconPathBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (iconPath.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            return false;
        }
        CopyCString(volume, iconPath, length + 1);
        APTR.WriteUInt8(iconPath, unchecked((int)length), (byte)':');
        WriteAscii(iconPath, length + 1, "Disk");

        var diskIcon = Icon.GetDefDiskObject((int)IconDefaultType.Disk);
        if (diskIcon == 0)
        {
            error = (int)DOS.IoErr();
            return false;
        }
        APTR.WriteUInt32(APTR.FromPointer(diskIcon),
            DiskObjectCurrentXOffset,
            unchecked((uint)WorkbenchConstants.NoIconPosition));
        APTR.WriteUInt32(APTR.FromPointer(diskIcon),
            DiskObjectCurrentYOffset,
            unchecked((uint)WorkbenchConstants.NoIconPosition));
        var putDisk = Icon.PutDiskObject(CString.FromPointer(iconPath),
            diskIcon);
        Icon.FreeDiskObject(diskIcon);
        if (putDisk == 0)
        {
            error = (int)DOS.IoErr();
            return false;
        }

        WriteAscii(iconPath, length + 1, "Trashcan");
        var trashLock = DOS.CreateDirRaw(CString.FromPointer(iconPath));
        if (trashLock.IsNull)
        {
            error = (int)DOS.IoErr();
            return false;
        }
        DOS.UnLock(trashLock);
        var trashIcon = Icon.GetDefDiskObject((int)IconDefaultType.Garbage);
        if (trashIcon == 0)
        {
            error = (int)DOS.IoErr();
            return false;
        }
        var putTrash = Icon.PutDiskObject(CString.FromPointer(iconPath),
            trashIcon);
        Icon.FreeDiskObject(trashIcon);
        if (putTrash == 0)
        {
            error = (int)DOS.IoErr();
            return false;
        }
        return true;
    }

    private static void WriteAscii(APTR destination, uint offset, string text)
    {
        for (var index = 0u; index < (uint)text.Length; index++)
            APTR.WriteUInt8(destination, unchecked((int)(offset + index)),
                (byte)text[unchecked((int)index)]);
        APTR.WriteUInt8(destination,
            unchecked((int)(offset + (uint)text.Length)), 0);
    }

    private static bool WaitValidated(APTR port, APTR info, uint dosType,
        out int error)
    {
        error = 0;
        for (var count = 0; count < 100; count++)
        {
            if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
            {
                error = (int)DOS.Error.Break;
                return false;
            }
            DOS.Delay(15);
            if (DOS.DoPkt(port, (int)DosPacketAction.DiskInfo,
                    unchecked((int)BPTR.FromAddress(info).Raw), 0, 0, 0, 0) == 0)
                continue;
            var state = APTR.ReadUInt32(info, DosLayout.InfoData.DiskState);
            if (state == (uint)DosDiskState.Validating) continue;
            var actual = APTR.ReadUInt32(info, DosLayout.InfoData.DiskType);
            if (actual == dosType ||
                (actual >= DosDisk && actual <= DosDisk + 6)) return true;
            if (count > 1) break;
        }
        error = (int)DOS.Error.NotADosDisk;
        return false;
    }

    private static bool FormatPartition(APTR request, uint dosType,
        uint sizeBlock, uint surfaces, uint blocksPerTrack, uint lowCylinder,
        uint highCylinder, uint maximumTransfer, uint mask, uint memoryType,
        bool writesDoneBefore, ref APTR storage, ref uint storageBytes,
        out bool writesDone, out int error)
    {
        writesDone = writesDoneBefore;
        error = 0;
        if (sizeBlock == 0 || surfaces == 0 || blocksPerTrack == 0 ||
            highCylinder <= lowCylinder)
        { error = (int)DOS.Error.ObjectWrongType; return false; }
        var blockSize = sizeBlock * 4u;
        var trackSize = blockSize * blocksPerTrack * surfaces;
        if (trackSize == 0) { error = (int)DOS.Error.ObjectTooLarge; return false; }
        Multiply32(highCylinder, trackSize, out var extentHigh, out _);
        var useTd64 = extentHigh != 0;
        var bufferSize = maximumTransfer == 0 || maximumTransfer > trackSize
            ? trackSize : maximumTransfer;
        bufferSize -= bufferSize % blockSize;
        if (bufferSize < blockSize) bufferSize = blockSize;
        var alignment = Alignment(mask);
        if (bufferSize > (uint.MaxValue - alignment) / 2)
        { error = (int)DOS.Error.ObjectTooLarge; return false; }
        storageBytes = bufferSize * 2 + alignment;
        storage = Exec.AllocMem(storageBytes,
            (Exec.MemoryFlags)memoryType | Exec.MemoryFlags.Public);
        if (storage.IsNull) { error = (int)DOS.Error.NoFreeStore; return false; }
        var write = APTR.FromPointer(Align(storage.Raw, alignment));
        var read = APTR.FromPointer(Align(write.Raw + bufferSize, alignment));
        for (var index = 0u; index < bufferSize / 4; index++)
            APTR.WriteUInt32(write, unchecked((int)(index * 4)),
                (dosType & 0xFFFF_FF00u) | (index & 0xFF));
        var cylinders = highCylinder - lowCylinder;
        for (var cylinder = 0u; cylinder < cylinders; cylinder++)
        {
            for (var mode = 1; mode >= 0; mode--)
            {
                if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                { error = (int)DOS.Error.Break; return false; }
                for (var position = 0u; position < trackSize; position += bufferSize)
                {
                    var length = trackSize - position;
                    if (length > bufferSize) length = bufferSize;
                    var cylinderStart = lowCylinder + cylinder;
                    Multiply32(cylinderStart, trackSize, out var offsetHigh,
                        out var offsetLow);
                    if (uint.MaxValue - offsetLow < position) offsetHigh++;
                    offsetLow += position;
                    var block = mode != 0 ? write : read;
                    if (cylinder == 0 && position == 0 && mode != 0)
                        APTR.WriteUInt32(write, 0, UnreadableDisk);
                    if (useTd64)
                    {
                        SetRequest64(request,
                            mode != 0 ? TrackDiskCommand.Format64 :
                                TrackDiskCommand.Read64,
                            block, length, offsetHigh, offsetLow);
                    }
                    else
                    {
                        SetRequest(request,
                            mode != 0 ? TrackDiskCommand.Format :
                                TrackDiskCommand.Read,
                            block, length, offsetLow);
                    }
                    Exec.DoIO(request);
                    var deviceError = APTR.ReadUInt8(request,
                        ExecLayout.IOStdReq.Error);
                    if (deviceError != 0)
                    { error = MapTrackDiskError(deviceError); return false; }
                    if (mode == 0 && !EqualBytes(read, write, length))
                    { error = (int)DOS.Error.NotADosDisk; return false; }
                    if (mode != 0) writesDone = true;
                }
            }
        }
        return true;
    }

    private static void SetRequest(APTR request, TrackDiskCommand command,
        APTR data, uint length, uint offset)
    {
        APTR.WriteUInt16(request, ExecLayout.IORequest.Command,
            (ushort)command);
        APTR.WriteUInt8(request, ExecLayout.IORequest.Flags, 0);
        APTR.WriteUInt32(request, ExecLayout.IOStdReq.Length, length);
        APTR.WriteUInt32(request, ExecLayout.IOStdReq.Data, data.Raw);
        APTR.WriteUInt32(request, ExecLayout.IOStdReq.Offset, offset);
    }

    private static void SetRequest64(APTR request, TrackDiskCommand command,
        APTR data, uint length, uint highOffset, uint lowOffset)
    {
        APTR.WriteUInt16(request, ExecLayout.IORequest.Command,
            (ushort)command);
        APTR.WriteUInt8(request, ExecLayout.IORequest.Flags, 0);
        APTR.WriteUInt32(request, ExecLayout.IOStdReq.Length, length);
        APTR.WriteUInt32(request, ExecLayout.IOStdReq.Data, data.Raw);
        APTR.WriteUInt32(request, ExecLayout.IOStdReq.Actual, highOffset);
        APTR.WriteUInt32(request, ExecLayout.IOStdReq.Offset, lowOffset);
    }

    private static void Multiply32(uint left, uint right, out uint high,
        out uint low)
    {
        var leftLow = left & 0xFFFFu;
        var leftHigh = left >> 16;
        var rightLow = right & 0xFFFFu;
        var rightHigh = right >> 16;
        var product0 = leftLow * rightLow;
        var product1 = leftHigh * rightLow;
        var product2 = leftLow * rightHigh;
        var middle = (product0 >> 16) + (product1 & 0xFFFFu) +
            (product2 & 0xFFFFu);
        low = (product0 & 0xFFFFu) | (middle << 16);
        high = (product1 >> 16) + (product2 >> 16) + (middle >> 16) +
            (leftHigh * rightHigh);
    }

    private static int MapTrackDiskError(byte error) => error switch
    {
        (byte)TrackDiskError.WriteProtected => (int)DOS.Error.DiskWriteProtected,
        (byte)TrackDiskError.DiskChanged => (int)DOS.Error.NoDisk,
        (byte)TrackDiskError.SeekError => (int)DOS.Error.SeekError,
        (byte)TrackDiskError.NoMemory => (int)DOS.Error.NoFreeStore,
        _ => (int)DOS.Error.NotADosDisk,
    };

    private static uint Alignment(uint mask)
    {
        for (var bit = 0u; bit < 24; bit++)
            if ((mask & (1u << unchecked((int)bit))) != 0)
                return 1u << unchecked((int)bit);
        return 4;
    }

    private static uint Align(uint value, uint alignment) =>
        (value + alignment - 1) & ~(alignment - 1);

    private static bool EqualBytes(APTR first, APTR second, uint length)
    {
        for (var index = 0u; index < length; index++)
            if (APTR.ReadUInt8(first, unchecked((int)index)) !=
                APTR.ReadUInt8(second, unchecked((int)index))) return false;
        return true;
    }

    private static bool IsBanned(APTR value)
    {
        return EqualsAsciiIgnoreCase(value, "MOSSYS") ||
               EqualsAsciiIgnoreCase(value, "SYS") ||
               EqualsAsciiIgnoreCase(value, "L") ||
               EqualsAsciiIgnoreCase(value, "DEVS") ||
               EqualsAsciiIgnoreCase(value, "LIBS") ||
               EqualsAsciiIgnoreCase(value, "S") ||
               EqualsAsciiIgnoreCase(value, "C");
    }

    private static bool EqualsAsciiIgnoreCase(APTR value, string text)
    {
        var length = CStringLength(value);
        if (length != (uint)text.Length) return false;
        for (var index = 0u; index < length; index++)
        {
            var left = APTR.ReadUInt8(value, unchecked((int)index));
            var right = (byte)text[unchecked((int)index)];
            if (left >= (byte)'a' && left <= (byte)'z') left -= 32;
            if (right >= (byte)'a' && right <= (byte)'z') right -= 32;
            if (left != right) return false;
        }
        return true;
    }

    private static uint CStringLength(APTR value)
    {
        var length = 0u;
        while (APTR.ReadUInt8(value, unchecked((int)length)) != 0) length++;
        return length;
    }

    private static void CopyCString(APTR source, APTR destination, uint capacity)
    {
        if (capacity == 0) return;
        var index = 0u;
        for (; index + 1 < capacity; index++)
        {
            var value = APTR.ReadUInt8(source, unchecked((int)index));
            APTR.WriteUInt8(destination, unchecked((int)index), value);
            if (value == 0) return;
        }
        APTR.WriteUInt8(destination, unchecked((int)index), 0);
    }
}
