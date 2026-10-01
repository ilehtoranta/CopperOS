using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-bound MorphOS Mount implementation.  The command deliberately keeps
/// the DOS parser, environment, handler BSTR and Expansion transaction in
/// guest-owned storage. The bounded resident slice supports explicit FROM
/// files, the source-observed DOSDrivers and DEVS:MountList searches, wildcard
/// matching, icon tool types, and Workbench startup argument iteration. Real
/// handler/provider behavior remains a separate qualification gate.
/// </summary>
public static class NativeMorphOSMountCommand
{
    public const string Template = "DEVICE/M,FROM/K,DEBUG/S";
    public const uint ResultCount = 3;

    public const string MountTemplate =
        "HANDLER/K,EHANDLER=FILESYSTEM/K,DEVICE/K,UNIT/K,FLAGS/K," +
        "SECTORSIZE=BLOCKSIZE/K,SURFACES/K," +
        "SECTORSPERTRACK=BLOCKSPERTRACK/K,SECTORSPERBLOCK/K,RESERVED/K," +
        "PREALLOC/K,INTERLEAVE/K,LOWCYL/K,HIGHCYL/K,BUFFERS/K,BUFMEMTYPE/K," +
        "MAXTRANSFER/K,MASK/K,BOOTPRI/K,DOSTYPE/K,BAUD/K,CONTROL/K," +
        "STACKSIZE/K,PRIORITY/K,GLOBVEC/K,STARTUP/K,MOUNT=ACTIVATE/K,FORCELOAD/K";
    public const uint MountResultCount = 28;

    private const uint EnvironmentBytes = 80;
    private const uint PacketBytes = 20;
    private const uint SearchPathBytes = 512;
    private const uint MountListItemBytes = 1024;
    private const uint MountListBufferExtra = 2;
    private const uint InfoToolBufferBytes = 4096;
    private const uint RdaNoPrompt = 4;
    private const int DeviceNotFoundError = 5001;
    // workbench.h: DiskObject.do_ToolTypes follows do_DefaultTool at 0x34.
    private const int DiskObjectToolTypesOffset = 0x38;
    // Amiga workbench.h: WBStartup fields follow the 20-byte Message.
    private const int WorkbenchStartupNumArgsOffset = 28;
    private const int WorkbenchStartupArgListOffset = 36;
    private const uint FileSystemContext = (uint)DosObjectType.FileSystemContext;
    // MakeDosNode/AddDosNode are public expansion.library V33 vectors. The
    // captured MorphOS source requests V37; retain that as differential
    // evidence and admit the lowest verified ROM capability floor.
    private const uint ExpansionVersion = 33;
    private const int DefaultDosType = unchecked((int)0x444F5300);
    private const uint DefaultSizeBlock = 512u / 4u;
    private const uint DefaultSurfaces = 2;
    private const uint DefaultSectorsPerBlock = 1;
    private const uint DefaultBlocksPerTrack = 11;
    private const uint DefaultReserved = 2;
    private const uint DefaultHighCylinder = 79;
    private const uint DefaultBuffers = 20;
    private const uint DefaultBufferMemoryType = 1;
    private const uint DefaultMaximumTransfer = 0x7FFF_FFFFu;
    private const uint DefaultMask = 0xFFFF_FFFEu;
    private const uint DefaultBaud = 1200;

    public static int Run(out int ioError)
        => RunProfile(Template, ResultCount, MountTemplate, MountResultCount,
            false, out ioError);

    public static int RunWorkbenchProfile(CString innerTemplate,
        uint innerResultCount, bool workbench, APTR startup,
        out int ioError)
    {
        ioError = 0;
        if (startup.IsNull)
        {
            ioError = (int)DOS.Error.RequiredArgumentMissing;
            return DOS.RETURN_FAIL;
        }

        var argumentCount = unchecked((int)APTR.ReadUInt32(startup,
            WorkbenchStartupNumArgsOffset));
        if (argumentCount < 2)
            return DOS.RETURN_OK;

        var argumentList = APTR.FromPointer(APTR.ReadUInt32(startup,
            WorkbenchStartupArgListOffset));
        if (argumentList.IsNull)
        {
            ioError = (int)DOS.Error.RequiredArgumentMissing;
            return DOS.RETURN_FAIL;
        }

        var expansion = Exec.OpenLibraryRaw(Expansion.Name,
            ExpansionVersion);
        if (expansion.IsNull)
        {
            ioError = (int)DOS.IoErr();
            if (ioError == 0) ioError = (int)DOS.Error.ObjectNotFound;
            return DOS.RETURN_FAIL;
        }
        Expansion.ExpansionLibraryBase = expansion;

        var result = DOS.RETURN_OK;
        var error = 0;
        for (var index = 1; index < argumentCount; index++)
        {
            var argument = APTR.FromPointer(argumentList.Raw +
                unchecked((uint)index * 8));
            var lockValue = BPTR.FromRaw(APTR.ReadUInt32(argument, 0));
            var name = APTR.FromPointer(APTR.ReadUInt32(argument, 4));
            var previous = DOS.CurrentDirRaw(lockValue);
            var deviceName = name.IsNull ? APTR.Null : DOS.FilePart(
                CString.FromPointer(name.Raw)).Address;
            var sourceError = 0;
            if (deviceName.IsNull || !ProcessSourceWithInfo(innerTemplate,
                    innerResultCount, workbench, name, deviceName,
                    out _, out sourceError))
            {
                error = deviceName.IsNull
                    ? (int)DOS.Error.RequiredArgumentMissing
                    : sourceError;
                result = DOS.RETURN_FAIL;
            }
            else
            {
                // The original Workbench loop assigns the result of each
                // startup file in turn; a later success replaces an earlier
                // failed file's status.
                error = 0;
                result = DOS.RETURN_OK;
            }
            DOS.CurrentDirRaw(previous);
        }

        Expansion.ExpansionLibraryBase = APTR.Null;
        Exec.CloseLibrary(expansion);
        if (error != 0)
        {
            ioError = error;
            DOS.SetIoErr((DOS.Error)error);
            DOS.PrintFault((DOS.Error)error, "Mount");
            return DOS.RETURN_FAIL;
        }
        ioError = (int)DOS.IoErr();
        return result;
    }

    /// <summary>
    /// Runs the shared Mount transaction with a profile-specific DOS grammar.
    /// Workbench 3.1 has a separately captured outer/inner template and slot
    /// order; its resident entry calls this path with <paramref name="workbench"/>
    /// set so the record mapping remains distinct from MorphOS.
    /// </summary>
    public static int RunProfile(CString outerTemplate, uint outerResultCount,
        CString innerTemplate, uint innerResultCount, bool workbench,
        out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(outerTemplate, outerResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "Mount");
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_FAIL;
        var error = 0;
        APTR expansionBase = APTR.Null;
        var expansionOpened = false;
        var records = 0;

        do
        {
            var from = APTR.Null;
            if (TryGetPointer(arguments, 1, out var fromValue) &&
                fromValue.IsNotNull)
                from = fromValue;

            var deviceVector = APTR.Null;
            var hasDevices = TryGetPointer(arguments, 0, out deviceVector) &&
                deviceVector.IsNotNull && APTR.ReadUInt32(deviceVector, 0) != 0;
            if (!hasDevices)
            {
                // The released CLI only processes FROM inside the DEVICE/M
                // loop. An empty multiple argument is therefore a no-op.
                result = DOS.RETURN_OK;
                break;
            }

            expansionBase = Exec.OpenLibraryRaw(Expansion.Name,
                ExpansionVersion);
            if (expansionBase.IsNull)
            {
                error = (int)DOS.IoErr();
                if (error == 0) error = (int)DOS.Error.ObjectNotFound;
                break;
            }
            expansionOpened = true;
            Expansion.ExpansionLibraryBase = expansionBase;

            var vectorIndex = 0u;
            while (error == 0)
            {
                var requested = APTR.Null;
                if (hasDevices)
                {
                    requested = APTR.FromPointer(APTR.ReadUInt32(
                        deviceVector, unchecked((int)(vectorIndex * 4))));
                    if (requested.IsNull) break;
                }

                if (from.IsNotNull && IsDeviceRequest(requested))
                {
                    if (!TryProcessMountList(innerTemplate,
                            innerResultCount, workbench, from, requested,
                            out var sourceRecords, out var sourceError))
                    {
                        error = sourceError;
                        break;
                    }
                    records += sourceRecords;
                    result = DOS.RETURN_OK;
                }
                else if (IsDeviceRequest(requested))
                {
                    if (!TryResolveDeviceSource(requested, out var source,
                            out var sourceOwned, out error))
                    {
                        // SearchTable exhaustion falls through to the
                        // released command's DEVS:MountList lookup. Keep
                        // allocation/parser failures distinct from a normal
                        // missing DOSDriver definition.
                        if (error != (int)DOS.Error.ObjectNotFound ||
                            !TryProcessMountList(innerTemplate,
                                innerResultCount, workbench,
                                APTR.FromPointer(CString.ToUInt32(
                                    CString.FromLiteral("DEVS:MountList"))),
                                requested, out var listRecords,
                                out var listError))
                            break;

                        records += listRecords;
                        error = 0;
                        DOS.SetIoErr((DOS.Error)0);
                        result = DOS.RETURN_OK;
                        vectorIndex++;
                        continue;
                    }

                    var deviceName = DOS.FilePart(
                        CString.FromPointer(source)).Address;
                    var sourceError = 0;
                    if (deviceName.IsNull || !ProcessSourceWithInfo(innerTemplate,
                            innerResultCount, workbench, source, deviceName,
                            out var sourceRecords, out sourceError))
                    {
                        error = deviceName.IsNull
                            ? (int)DOS.Error.RequiredArgumentMissing
                            : sourceError;
                        if (sourceOwned) Exec.FreeMem(source, SearchPathBytes);
                        break;
                    }
                    if (sourceOwned) Exec.FreeMem(source, SearchPathBytes);
                    records += sourceRecords;
                    result = DOS.RETURN_OK;
                }
                else
                {
                    var anchorBytes = (uint)DosLayout.AnchorPath.Size +
                        SearchPathBytes;
                    var anchor = Exec.AllocMem(anchorBytes,
                        Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
                    if (anchor.IsNull)
                    {
                        error = (int)DOS.Error.NoFreeStore;
                        break;
                    }
                    InitializeAnchor(anchor);
                    var match = DOS.MatchFirst(
                        CString.FromPointer(requested), anchor);
                    var matched = false;
                    while (match == 0)
                    {
                        matched = true;
                        var source = APTR.FromPointer(anchor.Raw +
                            (uint)DosLayout.AnchorPath.PathBuffer);
                        var deviceName = DOS.FilePart(
                            CString.FromPointer(source)).Address;
                        var sourceError = 0;
                        if (deviceName.IsNull || !ProcessSourceWithInfo(innerTemplate,
                                innerResultCount, workbench, source, deviceName,
                                out var sourceRecords, out sourceError))
                        {
                            error = deviceName.IsNull
                                ? (int)DOS.Error.RequiredArgumentMissing
                                : sourceError;
                            break;
                        }
                        records += sourceRecords;
                        result = DOS.RETURN_OK;
                        match = DOS.MatchNext(anchor);
                    }
                    DOS.MatchEnd(anchor);
                    Exec.FreeMem(anchor, anchorBytes);
                    if (error == 0 && match != (int)DOS.Error.NoMoreEntries)
                        error = match;
                    if (error == 0 && !matched)
                        error = (int)DOS.Error.ObjectNotFound;
                    if (error == 0)
                        DOS.SetIoErr((DOS.Error)0);
                }

                if (!hasDevices) break;
                vectorIndex++;
            }
        }
        while (false);

        if (expansionOpened)
        {
            Expansion.ExpansionLibraryBase = APTR.Null;
            Exec.CloseLibrary(expansionBase);
        }
        arguments.Release();

        if (error != 0)
        {
            ioError = error;
            DOS.SetIoErr((DOS.Error)error);
            DOS.PrintFault((DOS.Error)error, "Mount");
            return DOS.RETURN_FAIL;
        }

        ioError = (int)DOS.IoErr();
        return result;
    }

    private static bool TryProcessMountList(CString innerTemplate,
        uint innerResultCount, bool workbench, APTR mountListPath,
        APTR requested, out int sourceRecords, out int ioError)
    {
        sourceRecords = 0;
        ioError = 0;
        if (requested.IsNull || mountListPath.IsNull)
        {
            ioError = (int)DOS.Error.RequiredArgumentMissing;
            return false;
        }

        if (!TryReadFileBuffer(mountListPath, out var buffer,
                out var bufferBytes, out ioError))
            return false;

        APTR itemBuffer = APTR.Null;
        APTR resultArray = APTR.Null;
        APTR rdArgs = APTR.Null;
        APTR deviceName = APTR.Null;
        var resultBytes = innerResultCount == 0 ? sizeof(uint) :
            innerResultCount * sizeof(uint);
        var matched = false;

        do
        {
            PrepareMountList(buffer, bufferBytes);
            itemBuffer = Exec.AllocMem(MountListItemBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            resultArray = Exec.AllocMem(resultBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            rdArgs = DOS.AllocDosObject((uint)DosObjectType.RdArgs,
                APTR.Null);
            if (itemBuffer.IsNull || resultArray.IsNull || rdArgs.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                break;
            }

            var requestedLength = CStringLength(requested);
            if (requestedLength == 0 || requestedLength >= SearchPathBytes)
            {
                ioError = (int)DOS.Error.LineTooLong;
                break;
            }
            if (APTR.ReadUInt8(requested,
                    unchecked((int)requestedLength - 1)) == ':')
                requestedLength--;
            if (requestedLength == 0 || requestedLength >= SearchPathBytes)
            {
                ioError = (int)DOS.Error.RequiredArgumentMissing;
                break;
            }

            deviceName = Exec.AllocMem(requestedLength + 1,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (deviceName.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                break;
            }
            for (var index = 0u; index < requestedLength; index++)
                APTR.WriteUInt8(deviceName, unchecked((int)index),
                    APTR.ReadUInt8(requested, unchecked((int)index)));
            APTR.WriteUInt8(deviceName, unchecked((int)requestedLength), 0);

            APTR.WriteUInt32(rdArgs, DosLayout.RDArgs.Source +
                DosLayout.CSource.Buffer, buffer.Raw);
            APTR.WriteUInt32(rdArgs, DosLayout.RDArgs.Source +
                DosLayout.CSource.Length, bufferBytes);
            APTR.WriteUInt32(rdArgs, DosLayout.RDArgs.Source +
                DosLayout.CSource.CurrentCharacter, 0);
            APTR.WriteUInt32(rdArgs, DosLayout.RDArgs.Flags, RdaNoPrompt);

            while (APTR.ReadUInt32(rdArgs, DosLayout.RDArgs.Source +
                       DosLayout.CSource.CurrentCharacter) < bufferBytes)
            {
                var itemResult = DOS.ReadItem(itemBuffer,
                    unchecked((int)MountListItemBytes), rdArgs);
                if (itemResult == (int)DOS.ItemKind.Error)
                {
                    ioError = (int)DOS.IoErr();
                    break;
                }

                var itemLength = itemResult > 0
                    ? CStringLengthBounded(itemBuffer, MountListItemBytes)
                    : 0;
                if (itemLength > 0 &&
                    APTR.ReadUInt8(itemBuffer,
                        unchecked((int)itemLength - 1)) == ':' &&
                    DeviceNameEquals(itemBuffer, itemLength - 1,
                        deviceName, requestedLength))
                {
                    matched = true;
                    for (var index = 0u; index < resultBytes; index += 4)
                        APTR.WriteUInt32(resultArray,
                            unchecked((int)index), 0);

                    if (!NativeCommandArguments.TryReadBorrowed(
                            innerTemplate, resultArray, innerResultCount,
                            rdArgs, out var record))
                    {
                        ioError = record.IoError;
                        record.ReleaseBorrowed();
                        break;
                    }

                    var mounted = ProcessRecord(record, deviceName,
                        workbench, out var recordError);
                    record.ReleaseBorrowed();
                    if (!mounted)
                    {
                        ioError = recordError;
                        break;
                    }

                    sourceRecords = 1;
                    ioError = 0;
                    break;
                }

                SkipMountListLine(rdArgs, bufferBytes);
            }
        }
        while (false);

        if (!matched && ioError == 0)
            ioError = DeviceNotFoundError;
        if (deviceName.IsNotNull)
            Exec.FreeMem(deviceName, CStringLength(deviceName) + 1);
        if (rdArgs.IsNotNull)
            DOS.FreeDosObject((uint)DosObjectType.RdArgs, rdArgs);
        if (resultArray.IsNotNull)
            Exec.FreeMem(resultArray, resultBytes);
        if (itemBuffer.IsNotNull)
            Exec.FreeMem(itemBuffer, MountListItemBytes);
        Exec.FreeMem(buffer, bufferBytes + MountListBufferExtra);
        return ioError == 0 && sourceRecords != 0;
    }

    private static bool TryReadFileBuffer(APTR path, out APTR buffer,
        out uint bufferBytes, out int ioError)
    {
        buffer = APTR.Null;
        bufferBytes = 0;
        ioError = 0;
        var file = DOS.OpenRaw(CString.FromPointer(path),
            DOS.FileMode.OldFile);
        if (file.IsNull)
        {
            ioError = (int)DOS.IoErr();
            return false;
        }

        var fileSize = DOS.Seek(file, 0, (int)DosConstants.OffsetEnd);
        if (fileSize < 0 || DOS.Seek(file, 0,
                (int)DosConstants.OffsetBeginning) < 0)
        {
            ioError = (int)DOS.IoErr();
            DOS.Close(file);
            return false;
        }
        if ((uint)fileSize > uint.MaxValue - MountListBufferExtra)
        {
            ioError = (int)DOS.Error.LineTooLong;
            DOS.Close(file);
            return false;
        }

        bufferBytes = unchecked((uint)fileSize);
        buffer = Exec.AllocMem(bufferBytes + MountListBufferExtra,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (buffer.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            DOS.Close(file);
            bufferBytes = 0;
            return false;
        }

        var remaining = bufferBytes;
        var cursor = buffer;
        while (remaining != 0)
        {
            var read = DOS.Read(file, cursor, unchecked((int)remaining));
            if (read <= 0)
            {
                ioError = read == -1 ? (int)DOS.IoErr() :
                    (int)DOS.Error.SeekError;
                Exec.FreeMem(buffer, bufferBytes + MountListBufferExtra);
                buffer = APTR.Null;
                bufferBytes = 0;
                DOS.Close(file);
                return false;
            }
            remaining -= unchecked((uint)read);
            cursor = APTR.FromPointer(cursor.Raw + unchecked((uint)read));
        }
        DOS.Close(file);

        if (bufferBytes == 0 || APTR.ReadUInt8(buffer,
                unchecked((int)bufferBytes - 1)) != '\n')
            APTR.WriteUInt8(buffer, unchecked((int)bufferBytes++),
                (byte)'\n');
        APTR.WriteUInt8(buffer, unchecked((int)bufferBytes), 0);
        return true;
    }

    private static void PrepareMountList(APTR buffer, uint length)
    {
        var index = 0u;
        while (index < length)
        {
            var value = APTR.ReadUInt8(buffer, unchecked((int)index));
            if (value == '/' && index + 1 < length &&
                APTR.ReadUInt8(buffer, unchecked((int)index + 1)) == '*')
            {
                APTR.WriteUInt8(buffer, unchecked((int)index++), (byte)' ');
                APTR.WriteUInt8(buffer, unchecked((int)index++), (byte)' ');
                while (index < length)
                {
                    if (APTR.ReadUInt8(buffer, unchecked((int)index)) == '*')
                    {
                        APTR.WriteUInt8(buffer, unchecked((int)index++),
                            (byte)' ');
                        if (index < length && APTR.ReadUInt8(buffer,
                                unchecked((int)index)) == '/')
                        {
                            APTR.WriteUInt8(buffer,
                                unchecked((int)index++), (byte)' ');
                            break;
                        }
                    }
                    else
                        APTR.WriteUInt8(buffer,
                            unchecked((int)index++), (byte)' ');
                }
                continue;
            }
            if (value == '"')
            {
                index++;
                while (index < length && APTR.ReadUInt8(buffer,
                           unchecked((int)index)) != '"') index++;
                if (index < length) index++;
                continue;
            }
            if (value == '\n' || value == ';')
                APTR.WriteUInt8(buffer, unchecked((int)index), (byte)' ');
            else if (value == '#')
                APTR.WriteUInt8(buffer, unchecked((int)index), (byte)'\n');
            index++;
        }

        if (length != 0 && APTR.ReadUInt8(buffer,
                unchecked((int)length - 1)) != '\n')
            APTR.WriteUInt8(buffer, unchecked((int)length - 1), (byte)'\n');
    }

    private static void SkipMountListLine(APTR rdArgs, uint sourceLength)
    {
        var cursor = APTR.ReadUInt32(rdArgs, DosLayout.RDArgs.Source +
            DosLayout.CSource.CurrentCharacter);
        while (cursor < sourceLength)
        {
            var value = APTR.ReadUInt8(APTR.FromPointer(
                APTR.ReadUInt32(rdArgs, DosLayout.RDArgs.Source +
                    DosLayout.CSource.Buffer)), unchecked((int)cursor++));
            if (value == '\n') break;
        }
        APTR.WriteUInt32(rdArgs, DosLayout.RDArgs.Source +
            DosLayout.CSource.CurrentCharacter, cursor);
    }

    private static uint CStringLengthBounded(APTR source, uint maximum)
    {
        if (source.IsNull) return 0;
        for (var index = 0u; index < maximum; index++)
            if (APTR.ReadUInt8(source, unchecked((int)index)) == 0)
                return index;
        return maximum;
    }

    private static bool DeviceNameEquals(APTR left, uint leftLength,
        APTR right, uint rightLength)
    {
        if (leftLength != rightLength) return false;
        for (var index = 0u; index < leftLength; index++)
        {
            var l = APTR.ReadUInt8(left, unchecked((int)index));
            var r = APTR.ReadUInt8(right, unchecked((int)index));
            if (l >= 'a' && l <= 'z') l = unchecked((byte)(l - 32));
            if (r >= 'a' && r <= 'z') r = unchecked((byte)(r - 32));
            if (l != r) return false;
        }
        return true;
    }

    private static bool ProcessSourceWithInfo(CString innerTemplate,
        uint innerResultCount, bool workbench, APTR source, APTR deviceName,
        out int sourceRecords, out int ioError)
    {
        if (ProcessSource(innerTemplate, innerResultCount, workbench, source,
                deviceName, out sourceRecords, out ioError))
            return true;

        // readmountfile only falls through to the icon.library path when the
        // mount file itself was not found. Parse errors from a real file must
        // remain visible to the caller.
        if (ioError != (int)DOS.Error.ObjectNotFound)
            return false;

        var originalError = ioError;
        if (TryProcessInfo(innerTemplate, innerResultCount, workbench, source,
                deviceName, out sourceRecords, out var infoError))
            return true;
        ioError = infoError == 0 ? originalError : infoError;
        return false;
    }

    private static bool TryProcessInfo(CString innerTemplate,
        uint innerResultCount, bool workbench, APTR source, APTR deviceName,
        out int sourceRecords, out int ioError)
    {
        sourceRecords = 0;
        ioError = 0;
        if (source.IsNull || deviceName.IsNull)
        {
            ioError = (int)DOS.Error.RequiredArgumentMissing;
            return false;
        }

        var iconLibrary = Exec.OpenLibraryRaw(Icon.Name, 37);
        if (iconLibrary.IsNull)
        {
            ioError = (int)DOS.IoErr();
            if (ioError == 0) ioError = (int)DOS.Error.ObjectNotFound;
            return false;
        }
        Icon.IconLibraryBase = iconLibrary;

        uint diskObject = 0;
        APTR toolBuffer = Exec.AllocMem(InfoToolBufferBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        APTR resultArray = APTR.Null;
        APTR rdArgs = APTR.Null;
        var resultBytes = innerResultCount == 0 ? sizeof(uint) :
            innerResultCount * sizeof(uint);
        var toolLength = 0u;
        var hasTool = false;

        do
        {
            if (toolBuffer.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                break;
            }

            diskObject = Icon.GetDiskObject(CString.FromPointer(source));
            if (diskObject == 0)
            {
                ioError = (int)DOS.IoErr();
                if (ioError == 0) ioError = (int)DOS.Error.ObjectNotFound;
                break;
            }

            var disk = APTR.FromPointer(diskObject);
            var toolTypes = APTR.FromPointer(APTR.ReadUInt32(disk,
                DiskObjectToolTypesOffset));
            if (toolTypes.IsNull)
            {
                ioError = (int)DOS.Error.ObjectNotFound;
                break;
            }

            // ReadMountArgs is called once for every eligible tool type in
            // the source and accumulates values into one parameter block. A
            // single space-separated ReadArgs source preserves that aggregate
            // result while keeping parser and tool text guest-owned.
            for (var index = 0u; index < InfoToolBufferBytes / sizeof(uint);
                 index++)
            {
                var tool = APTR.FromPointer(APTR.ReadUInt32(toolTypes,
                    unchecked((int)(index * sizeof(uint)))));
                if (tool.IsNull) break;

                var length = CStringLengthBounded(tool, InfoToolBufferBytes);
                if (length == InfoToolBufferBytes)
                {
                    ioError = (int)DOS.Error.LineTooLong;
                    break;
                }
                if (length == 0 || IsIgnoredInfoToolType(tool, length))
                    continue;

                var separator = hasTool ? 1u : 0u;
                if (length > InfoToolBufferBytes - separator - 2 ||
                    toolLength > InfoToolBufferBytes -
                        (length + separator + 2))
                {
                    ioError = (int)DOS.Error.LineTooLong;
                    break;
                }
                if (hasTool)
                    APTR.WriteUInt8(toolBuffer,
                        unchecked((int)toolLength++), (byte)' ');
                for (var charIndex = 0u; charIndex < length; charIndex++)
                    APTR.WriteUInt8(toolBuffer,
                        unchecked((int)toolLength++),
                        APTR.ReadUInt8(tool, unchecked((int)charIndex)));
                hasTool = true;
            }
            if (ioError != 0) break;
            if (!hasTool)
            {
                ioError = (int)DOS.Error.ObjectNotFound;
                break;
            }

            APTR.WriteUInt8(toolBuffer, unchecked((int)toolLength++),
                (byte)'\n');
            APTR.WriteUInt8(toolBuffer, unchecked((int)toolLength), 0);

            resultArray = Exec.AllocMem(resultBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            rdArgs = DOS.AllocDosObject((uint)DosObjectType.RdArgs,
                APTR.Null);
            if (resultArray.IsNull || rdArgs.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                break;
            }

            APTR.WriteUInt32(rdArgs, DosLayout.RDArgs.Source +
                DosLayout.CSource.Buffer, toolBuffer.Raw);
            APTR.WriteUInt32(rdArgs, DosLayout.RDArgs.Source +
                DosLayout.CSource.Length, toolLength);
            APTR.WriteUInt32(rdArgs, DosLayout.RDArgs.Source +
                DosLayout.CSource.CurrentCharacter, 0);
            APTR.WriteUInt32(rdArgs, DosLayout.RDArgs.Flags, RdaNoPrompt);

            if (!NativeCommandArguments.TryReadBorrowed(innerTemplate,
                    resultArray, innerResultCount, rdArgs, out var record))
            {
                ioError = record.IoError;
                record.ReleaseBorrowed();
                break;
            }

            var mounted = ProcessRecord(record, deviceName, workbench,
                out var recordError);
            record.ReleaseBorrowed();
            if (!mounted)
            {
                ioError = recordError;
                break;
            }
            sourceRecords = 1;
        }
        while (false);

        if (diskObject != 0)
            Icon.FreeDiskObject(diskObject);
        if (rdArgs.IsNotNull)
            DOS.FreeDosObject((uint)DosObjectType.RdArgs, rdArgs);
        if (resultArray.IsNotNull)
            Exec.FreeMem(resultArray, resultBytes);
        if (toolBuffer.IsNotNull)
            Exec.FreeMem(toolBuffer, InfoToolBufferBytes);
        Icon.IconLibraryBase = APTR.Null;
        Exec.CloseLibrary(iconLibrary);

        return ioError == 0 && sourceRecords != 0;
    }

    private static bool IsIgnoredInfoToolType(APTR tool, uint length)
    {
        var first = APTR.ReadUInt8(tool, 0);
        if (first == '(' || first == '*') return true;
        return length >= 4 && first == 'I' &&
            APTR.ReadUInt8(tool, 1) == 'M' &&
            APTR.ReadUInt8(tool, 3) == '=';
    }

    private static bool ProcessSource(CString innerTemplate,
        uint innerResultCount, bool workbench, APTR source, APTR deviceName,
        out int sourceRecords, out int ioError)
    {
        sourceRecords = 0;
        ioError = 0;
        var inputFile = DOS.OpenRaw(CString.FromPointer(source),
            DOS.FileMode.OldFile);
        if (inputFile.IsNull)
        {
            ioError = (int)DOS.IoErr();
            return false;
        }

        var previousInput = DOS.SelectInput(inputFile);
        while (true)
        {
            if (!NativeCommandArguments.TryRead(innerTemplate,
                    innerResultCount, out var record))
            {
                var parseError = record.IoError;
                record.Release();
                // ReadArgs reports end-of-file with no error on classic DOS.
                if (parseError == 0 && sourceRecords != 0) break;
                if (parseError == 0) parseError = (int)DOS.Error.BadTemplate;
                ioError = parseError;
                break;
            }

            var mounted = ProcessRecord(record, deviceName, workbench,
                out var recordError);
            record.Release();
            if (!mounted)
            {
                ioError = recordError;
                break;
            }
            sourceRecords++;
        }

        DOS.SelectInput(previousInput);
        DOS.Close(inputFile);
        if (ioError == 0 && sourceRecords == 0)
        {
            ioError = (int)DOS.Error.NoMoreEntries;
            return false;
        }
        return ioError == 0;
    }

    private static bool IsDeviceRequest(APTR requested)
    {
        var length = CStringLength(requested);
        return length != 0 &&
            APTR.ReadUInt8(requested, unchecked((int)length - 1)) == ':';
    }

    private static void InitializeAnchor(APTR anchor)
    {
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Base, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Current, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits,
            1u << 12);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.FoundBreak, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Reserved, 0);
        APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength,
            unchecked((ushort)SearchPathBytes));
    }

    private enum RecordSlot
    {
        Handler, EHandler, FileSystem, Device, Unit, Flags, SectorSize, Surfaces,
        BlocksPerTrack, SectorsPerBlock, Reserved, PreAlloc, Interleave,
        LowCylinder, HighCylinder, Buffers, BufferMemoryType, MaximumTransfer,
        Mask, BootPriority, DosType, Baud, Control, StackSize, Priority,
        GlobalVector, Startup, Activate, ForceLoad
    }

    private static uint Slot(RecordSlot slot, bool workbench) =>
        (uint)(workbench ? slot switch
        {
            RecordSlot.Handler => 22,
            RecordSlot.EHandler => 29,
            RecordSlot.FileSystem => 26,
            RecordSlot.Device => 19,
            RecordSlot.Unit => 20,
            RecordSlot.Flags => 21,
            RecordSlot.SectorSize => 1,
            RecordSlot.Surfaces => 3,
            RecordSlot.SectorsPerBlock => 4,
            RecordSlot.BlocksPerTrack => 5,
            RecordSlot.Reserved => 6,
            RecordSlot.PreAlloc => 7,
            RecordSlot.Interleave => 8,
            RecordSlot.LowCylinder => 9,
            RecordSlot.HighCylinder => 10,
            RecordSlot.Buffers => 11,
            RecordSlot.BufferMemoryType => 12,
            RecordSlot.MaximumTransfer => 13,
            RecordSlot.Mask => 14,
            RecordSlot.BootPriority => 15,
            RecordSlot.DosType => 16,
            RecordSlot.Baud => 17,
            RecordSlot.Control => 18,
            RecordSlot.StackSize => 23,
            RecordSlot.Priority => 24,
            RecordSlot.GlobalVector => 25,
            RecordSlot.Startup => 27,
            RecordSlot.Activate => 28,
            RecordSlot.ForceLoad => 30,
            _ => 0
        } : slot switch
        {
            RecordSlot.Handler => 0,
            RecordSlot.EHandler => 1,
            RecordSlot.FileSystem => 1,
            RecordSlot.Device => 2,
            RecordSlot.Unit => 3,
            RecordSlot.Flags => 4,
            RecordSlot.SectorSize => 5,
            RecordSlot.Surfaces => 6,
            RecordSlot.BlocksPerTrack => 7,
            RecordSlot.SectorsPerBlock => 8,
            RecordSlot.Reserved => 9,
            RecordSlot.PreAlloc => 10,
            RecordSlot.Interleave => 11,
            RecordSlot.LowCylinder => 12,
            RecordSlot.HighCylinder => 13,
            RecordSlot.Buffers => 14,
            RecordSlot.BufferMemoryType => 15,
            RecordSlot.MaximumTransfer => 16,
            RecordSlot.Mask => 17,
            RecordSlot.BootPriority => 18,
            RecordSlot.DosType => 19,
            RecordSlot.Baud => 20,
            RecordSlot.Control => 21,
            RecordSlot.StackSize => 22,
            RecordSlot.Priority => 23,
            RecordSlot.GlobalVector => 24,
            RecordSlot.Startup => 25,
            RecordSlot.Activate => 26,
            RecordSlot.ForceLoad => 27,
            _ => 0
        });

    private static bool ProcessRecord(NativeCommandArguments arguments,
        APTR deviceName, bool workbench, out int ioError)
    {
        ioError = 0;
        var handlerSlot = Slot(RecordSlot.Handler, workbench);
        var eHandlerSlot = Slot(RecordSlot.EHandler, workbench);
        var fileSystemSlot = Slot(RecordSlot.FileSystem, workbench);
        if (!TryGetPointer(arguments, handlerSlot, out var handler) || handler.IsNull)
        {
            // Workbench has separate FILESYSTEM and EHANDLER slots, while
            // MorphOS exposes FILESYSTEM as the EHANDLER spelling alias.
            if (!TryGetPointer(arguments, fileSystemSlot, out handler) || handler.IsNull)
            {
                if (!TryGetPointer(arguments, eHandlerSlot, out handler) || handler.IsNull)
                {
                    ioError = (int)DOS.Error.RequiredArgumentMissing;
                    return false;
                }
            }
        }
        var dosTypeSlot = Slot(RecordSlot.DosType, workbench);
        var hasDosType = HasValue(arguments, dosTypeSlot);
        if (!TryGetNumber(arguments, dosTypeSlot, DefaultDosType, false, out var dosType) ||
            (!hasDosType && handler.IsNull))
        {
            ioError = hasDosType ? (int)DOS.Error.BadNumber :
                (int)DOS.Error.RequiredArgumentMissing;
            return false;
        }

        var number = Exec.AllocMem(sizeof(uint),
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        var environment = APTR.Null;
        var packet = APTR.Null;
        var deviceString = APTR.Null;
        var deviceStringBytes = 0u;
        var unitString = APTR.Null;
        var unitStringBytes = 0u;
        var flagsString = APTR.Null;
        var flagsStringBytes = 0u;
        var controlString = APTR.Null;
        var controlStringBytes = 0u;
        var handlerString = APTR.Null;
        var handlerStringBytes = 0u;
        var startupString = APTR.Null;
        var startupStringBytes = 0u;
        APTR node = APTR.Null;
        var nodePublished = false;

        var mounted = false;
        do
        {
            if (number.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                break;
            }

            var bootPriority = 0;
            if (!TryGetNumber(arguments, Slot(RecordSlot.BootPriority, workbench),
                    0, false, out bootPriority) ||
                bootPriority < sbyte.MinValue || bootPriority > sbyte.MaxValue)
            {
                ioError = (int)DOS.Error.BadNumber;
                break;
            }

            var stackSize = 8192;
            var priority = 5;
            var globalVector = -1;
            if (!TryGetNumber(arguments, Slot(RecordSlot.StackSize, workbench),
                    stackSize, false, out stackSize) ||
                !TryGetNumber(arguments, Slot(RecordSlot.Priority, workbench),
                    priority, false, out priority) ||
                !TryGetNumber(arguments, Slot(RecordSlot.GlobalVector, workbench),
                    globalVector, false, out globalVector) ||
                globalVector < -3 || globalVector > -1)
            {
                ioError = (int)DOS.Error.BadNumber;
                break;
            }

            var startup = 0;
            var startupSlot = Slot(RecordSlot.Startup, workbench);
            var startupProvided = HasValue(arguments, startupSlot);
            if (startupProvided &&
                !TryGetNumber(arguments, startupSlot, startup, false, out startup))
            {
                if (!TryGetPointer(arguments, startupSlot, out var startupText) ||
                    !TryCopyBString(startupText, out startupString,
                        out startupStringBytes, out ioError)) break;
            }
            else if (startupProvided &&
                     (startup < 0 || startup > byte.MaxValue))
            {
                ioError = (int)DOS.Error.BadNumber;
                break;
            }

            environment = Exec.AllocMem(EnvironmentBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            packet = Exec.AllocMem(PacketBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (environment.IsNull || packet.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                break;
            }

            if (!TryGetCString(arguments, Slot(RecordSlot.Device, workbench),
                    out var deviceText) ||
                (!deviceText.IsNull && !TryCopyCString(deviceText,
                    out deviceString, out deviceStringBytes, out ioError)))
                break;
            if (!TryGetNumericOrCString(arguments,
                    Slot(RecordSlot.Unit, workbench), number,
                    out var unit, out unitString, out unitStringBytes,
                    out ioError)) break;
            if (!TryGetNumericOrCString(arguments,
                    Slot(RecordSlot.Flags, workbench), number,
                    out var flags, out flagsString, out flagsStringBytes,
                    out ioError)) break;
            var controlSlot = Slot(RecordSlot.Control, workbench);
            if (HasValue(arguments, controlSlot))
            {
                if (!TryGetPointer(arguments, controlSlot, out var controlText) ||
                    !TryCopyBString(controlText, out controlString,
                        out controlStringBytes, out ioError)) break;
            }

            WriteEnvironment(arguments, environment, number, bootPriority,
                dosType, controlString, workbench);
            APTR.WriteUInt32(packet, 0, deviceName.Raw);
            APTR.WriteUInt32(packet, 4, deviceString.Raw);
            APTR.WriteUInt32(packet, 8, unitString.IsNull ? unit :
                unitString.Raw);
            APTR.WriteUInt32(packet, 12, flagsString.IsNull ? flags :
                flagsString.Raw);
            APTR.WriteUInt32(packet, 16, environment.Raw);

            node = APTR.FromPointer(Expansion.MakeDosNode(packet));
            if (node.IsNull)
            {
                ioError = (int)DOS.IoErr();
                if (ioError == 0) ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            // MakeDosNode creates a DeviceNode but does not copy the handler
            // name. The released command keeps a public BSTR in dn_Handler;
            // copy the ReadArgs-owned C string before releasing the parser.
            if (!handler.IsNull &&
                !TryCopyBString(handler, out handlerString,
                    out handlerStringBytes, out ioError)) break;

            var forceLoad = 0;
            if (!TryGetNumber(arguments, Slot(RecordSlot.ForceLoad, workbench),
                    0, false, out forceLoad))
            {
                ioError = (int)DOS.Error.BadNumber;
                break;
            }
            if (!handlerString.IsNull &&
                (forceLoad != 0 || APTR.ReadUInt32(node, 32) == 0))
            {
                APTR.WriteUInt32(node, 16,
                    BPTR.FromAddress(handlerString).Raw);
            }
            else if (!handlerString.IsNull)
            {
                // A provider may return a preloaded segment. In that case
                // the source releases its temporary handler BSTR unless
                // FORCELOAD explicitly requests it to be retained.
                Exec.FreeMem(handlerString, handlerStringBytes);
                handlerString = APTR.Null;
                handlerStringBytes = 0;
            }

            APTR.WriteUInt32(node, 20, unchecked((uint)stackSize));
            APTR.WriteUInt32(node, 24, unchecked((uint)priority));
            if (startupProvided)
            {
                APTR.WriteUInt32(node, 28, startupString.IsNull
                    ? unchecked((uint)startup)
                    : BPTR.FromAddress(startupString).Raw);
            }
            APTR.WriteUInt32(node, 36, unchecked((uint)globalVector));

            var activate = ReadNumber(arguments,
                Slot(RecordSlot.Activate, workbench), number);
            var addFlags = activate != 0 ? 1u : 0u; // ADNF_STARTPROC

            if (Expansion.AddDosNode(bootPriority, addFlags, node) == 0)
            {
                ioError = (int)DOS.IoErr();
                if (ioError == 0) ioError = (int)DOS.Error.ObjectExists;
                break;
            }
            nodePublished = true;
            ioError = 0;
            mounted = true;
        }
        while (false);

        if (!node.IsNull && !nodePublished)
            DOS.FreeDosObject(FileSystemContext, node);
        if (!nodePublished && !deviceString.IsNull)
            Exec.FreeMem(deviceString, deviceStringBytes);
        if (!nodePublished && !unitString.IsNull)
            Exec.FreeMem(unitString, unitStringBytes);
        if (!nodePublished && !flagsString.IsNull)
            Exec.FreeMem(flagsString, flagsStringBytes);
        if (!nodePublished && !controlString.IsNull)
            Exec.FreeMem(controlString, controlStringBytes);
        if (!nodePublished && !handlerString.IsNull)
            Exec.FreeMem(handlerString, handlerStringBytes);
        if (!nodePublished && !startupString.IsNull)
            Exec.FreeMem(startupString, startupStringBytes);
        if (!packet.IsNull) Exec.FreeMem(packet, PacketBytes);
        if (!environment.IsNull) Exec.FreeMem(environment, EnvironmentBytes);
        if (!number.IsNull) Exec.FreeMem(number, sizeof(uint));
        return mounted;
    }

    private static void WriteEnvironment(NativeCommandArguments arguments,
        APTR environment, APTR scratch, int bootPriority, int dosType,
        APTR controlString, bool workbench)
    {
        WriteLong(environment, 0, 19);
        var sizeBlockSlot = Slot(RecordSlot.SectorSize, workbench);
        var sizeBlock = ReadNumber(arguments, sizeBlockSlot, scratch);
        if (!HasValue(arguments, sizeBlockSlot)) sizeBlock = DefaultSizeBlock;
        else sizeBlock >>= 2; // source accepts bytes and stores longwords
        WriteLong(environment, 1, sizeBlock);
        WriteLong(environment, 2, 0);
        var surfacesSlot = Slot(RecordSlot.Surfaces, workbench);
        var sectorsPerBlockSlot = Slot(RecordSlot.SectorsPerBlock, workbench);
        var blocksPerTrackSlot = Slot(RecordSlot.BlocksPerTrack, workbench);
        var reservedSlot = Slot(RecordSlot.Reserved, workbench);
        var preAllocSlot = Slot(RecordSlot.PreAlloc, workbench);
        var interleaveSlot = Slot(RecordSlot.Interleave, workbench);
        var lowCylinderSlot = Slot(RecordSlot.LowCylinder, workbench);
        var highCylinderSlot = Slot(RecordSlot.HighCylinder, workbench);
        var buffersSlot = Slot(RecordSlot.Buffers, workbench);
        var bufferMemoryTypeSlot = Slot(RecordSlot.BufferMemoryType, workbench);
        var maximumTransferSlot = Slot(RecordSlot.MaximumTransfer, workbench);
        var maskSlot = Slot(RecordSlot.Mask, workbench);
        WriteLong(environment, 3, HasValue(arguments, surfacesSlot) ?
            ReadNumber(arguments, surfacesSlot, scratch) : DefaultSurfaces);
        WriteLong(environment, 4, HasValue(arguments, sectorsPerBlockSlot) ?
            ReadNumber(arguments, sectorsPerBlockSlot, scratch) : DefaultSectorsPerBlock);
        WriteLong(environment, 5, HasValue(arguments, blocksPerTrackSlot) ?
            ReadNumber(arguments, blocksPerTrackSlot, scratch) : DefaultBlocksPerTrack);
        WriteLong(environment, 6, HasValue(arguments, reservedSlot) ?
            ReadNumber(arguments, reservedSlot, scratch) : DefaultReserved);
        WriteLong(environment, 7, ReadNumber(arguments, preAllocSlot, scratch));
        WriteLong(environment, 8, ReadNumber(arguments, interleaveSlot, scratch));
        WriteLong(environment, 9, ReadNumber(arguments, lowCylinderSlot, scratch));
        WriteLong(environment, 10, HasValue(arguments, highCylinderSlot) ?
            ReadNumber(arguments, highCylinderSlot, scratch) : DefaultHighCylinder);
        WriteLong(environment, 11, HasValue(arguments, buffersSlot) ?
            ReadNumber(arguments, buffersSlot, scratch) : DefaultBuffers);
        WriteLong(environment, 12, HasValue(arguments, bufferMemoryTypeSlot) ?
            ReadNumber(arguments, bufferMemoryTypeSlot, scratch) : DefaultBufferMemoryType);
        WriteLong(environment, 13, HasValue(arguments, maximumTransferSlot) ?
            ReadNumber(arguments, maximumTransferSlot, scratch) : DefaultMaximumTransfer);
        WriteLong(environment, 14, HasValue(arguments, maskSlot) ?
            ReadNumber(arguments, maskSlot, scratch) : DefaultMask);
        WriteLong(environment, 15, bootPriority);
        WriteLong(environment, 16, dosType);
        var baudSlot = Slot(RecordSlot.Baud, workbench);
        WriteLong(environment, 17, HasValue(arguments, baudSlot) ?
            ReadNumber(arguments, baudSlot, scratch) : DefaultBaud);
        WriteLong(environment, 18, controlString.IsNull ? 0u :
            BPTR.FromAddress(controlString).Raw);
        // MOUNT/ACTIVATE controls AddDosNode's ADNF_STARTPROC flag. The
        // source initializes de_BootBlocks to zero and never stores Activate
        // in the DosEnvec.
        WriteLong(environment, 19, 0);
    }

    private static void WriteLong(APTR address, int index, int value) =>
        APTR.WriteUInt32(address, index * 4, unchecked((uint)value));

    private static void WriteLong(APTR address, int index, uint value) =>
        APTR.WriteUInt32(address, index * 4, value);

    private static bool TryGetPointer(NativeCommandArguments arguments,
        uint index, out APTR value)
    {
        value = APTR.Null;
        return arguments.TryGetResult(index, out var raw) &&
            (value = APTR.FromPointer(raw)).IsNotNull;
    }

    private static bool HasValue(NativeCommandArguments arguments, uint index) =>
        arguments.TryGetResult(index, out var value) && value != 0;

    private static bool TryResolveDeviceSource(APTR requested,
        out APTR source, out bool sourceOwned, out int ioError)
    {
        source = APTR.Null;
        sourceOwned = false;
        ioError = 0;
        if (requested.IsNull)
        {
            ioError = (int)DOS.Error.RequiredArgumentMissing;
            return false;
        }

        var length = CStringLength(requested);
        if (length == 0 || length >= SearchPathBytes)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }

        // A non-device argument is a mount-file name.  Probe it with the
        // public DOS Open/Close pair and let the normal source parser reopen
        // it after the Expansion lease is established.
        if (APTR.ReadUInt8(requested, unchecked((int)length - 1)) != ':')
        {
            var probe = DOS.OpenRaw(CString.FromPointer(requested),
                DOS.FileMode.OldFile);
            if (probe.IsNull)
            {
                ioError = (int)DOS.IoErr();
                return false;
            }
            DOS.Close(probe);
            source = requested;
            return true;
        }

        var candidate = Exec.AllocMem(SearchPathBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (candidate.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            return false;
        }

        // The source's SearchTable order is part of the CLI behavior.  The
        // first empty prefix deliberately probes the current directory before
        // the four DOSDrivers locations.
        for (var prefix = 0; prefix < 5; prefix++)
        {
            if (!WriteSearchCandidate(candidate, requested, length, prefix))
            {
                ioError = (int)DOS.Error.LineTooLong;
                break;
            }

            var probe = DOS.OpenRaw(CString.FromPointer(candidate),
                DOS.FileMode.OldFile);
            if (probe.IsNotNull)
            {
                DOS.Close(probe);
                source = candidate;
                sourceOwned = true;
                return true;
            }
            var probeError = (int)DOS.IoErr();
            if (TryProbeInfoIcon(candidate))
            {
                source = candidate;
                sourceOwned = true;
                return true;
            }
            ioError = probeError;
        }

        Exec.FreeMem(candidate, SearchPathBytes);
        if (ioError == 0) ioError = (int)DOS.Error.ObjectNotFound;
        return false;
    }

    private static bool TryProbeInfoIcon(APTR source)
    {
        var iconLibrary = Exec.OpenLibraryRaw(Icon.Name, 37);
        if (iconLibrary.IsNull) return false;
        Icon.IconLibraryBase = iconLibrary;
        var diskObject = Icon.GetDiskObject(CString.FromPointer(source));
        var found = diskObject != 0;
        if (found) Icon.FreeDiskObject(diskObject);
        Icon.IconLibraryBase = APTR.Null;
        Exec.CloseLibrary(iconLibrary);
        return found;
    }

    private static bool WriteSearchCandidate(APTR destination, APTR requested,
        uint requestedLength, int prefixIndex)
    {
        var prefix = prefixIndex switch
        {
            0 => CString.FromLiteral(""),
            1 => CString.FromLiteral("DEVS:DOSDrivers/"),
            2 => CString.FromLiteral("MOSSYS:DEVS/DOSDrivers/"),
            3 => CString.FromLiteral("SYS:Storage/DOSDrivers/"),
            _ => CString.FromLiteral("MOSSYS:Storage/DOSDrivers/")
        };
        var prefixAddress = APTR.FromPointer(CString.ToUInt32(prefix));
        var prefixLength = CStringLength(prefixAddress);
        var deviceLength = requestedLength - 1; // remove the trailing ':'
        if (prefixLength + deviceLength + 1 > SearchPathBytes)
            return false;

        for (var index = 0u; index < prefixLength; index++)
            APTR.WriteUInt8(destination, unchecked((int)index),
                APTR.ReadUInt8(prefixAddress, unchecked((int)index)));
        for (var index = 0u; index < deviceLength; index++)
            APTR.WriteUInt8(destination,
                unchecked((int)(prefixLength + index)),
                APTR.ReadUInt8(requested, unchecked((int)index)));
        APTR.WriteUInt8(destination,
            unchecked((int)(prefixLength + deviceLength)), 0);
        return true;
    }

    private static uint CStringLength(APTR source)
    {
        if (source.IsNull) return 0;
        for (var length = 0u; length < SearchPathBytes; length++)
            if (APTR.ReadUInt8(source, unchecked((int)length)) == 0)
                return length;
        return SearchPathBytes;
    }

    private static bool TryGetNumber(NativeCommandArguments arguments, uint index,
        int defaultValue, bool required, out int value)
    {
        value = defaultValue;
        if (!arguments.TryGetResult(index, out var raw)) return false;
        var text = APTR.FromPointer(raw);
        if (text.IsNull) return !required;
        var scratch = Exec.AllocMem(sizeof(uint),
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (scratch.IsNull) return false;
        var parsed = DOS.StrToLong(CString.FromPointer(text), scratch);
        if (parsed <= 0)
        {
            Exec.FreeMem(scratch, sizeof(uint));
            return false;
        }
        value = unchecked((int)APTR.ReadUInt32(scratch, 0));
        Exec.FreeMem(scratch, sizeof(uint));
        return true;
    }

    private static uint ReadNumber(NativeCommandArguments arguments, uint index,
        APTR scratch)
    {
        if (!arguments.TryGetResult(index, out var raw) || raw == 0 ||
            scratch.IsNull)
            return 0;
        var text = APTR.FromPointer(raw);
        if (DOS.StrToLong(CString.FromPointer(text), scratch) <= 0)
            return 0;
        return APTR.ReadUInt32(scratch, 0);
    }

    private static bool TryGetCString(NativeCommandArguments arguments,
        uint index, out APTR value)
    {
        value = APTR.Null;
        if (!arguments.TryGetResult(index, out var raw)) return true;
        value = APTR.FromPointer(raw);
        return true;
    }

    private static bool TryGetNumericOrCString(
        NativeCommandArguments arguments, uint index, APTR scratch,
        out uint value, out APTR textCopy, out uint textCopyBytes,
        out int ioError)
    {
        value = 0;
        textCopy = APTR.Null;
        textCopyBytes = 0;
        ioError = 0;
        if (!TryGetCString(arguments, index, out var source) ||
            source.IsNull) return true;
        if (!scratch.IsNull &&
            DOS.StrToLong(CString.FromPointer(source), scratch) > 0)
        {
            value = APTR.ReadUInt32(scratch, 0);
            return true;
        }
        return TryCopyCString(source, out textCopy, out textCopyBytes,
            out ioError);
    }

    private static bool TryCopyCString(APTR source, out APTR destination,
        out uint bytes, out int ioError)
    {
        destination = APTR.Null;
        bytes = 0;
        ioError = 0;
        if (source.IsNull)
        {
            ioError = (int)DOS.Error.RequiredArgumentMissing;
            return false;
        }

        var length = 0u;
        for (; length <= byte.MaxValue; length++)
        {
            if (APTR.ReadUInt8(source, unchecked((int)length)) == 0) break;
        }
        if (length > byte.MaxValue)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }

        bytes = length + 1;
        destination = Exec.AllocMem(bytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (destination.IsNull)
        {
            bytes = 0;
            ioError = (int)DOS.Error.NoFreeStore;
            return false;
        }
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(destination, unchecked((int)index),
                APTR.ReadUInt8(source, unchecked((int)index)));
        APTR.WriteUInt8(destination, unchecked((int)length), 0);
        return true;
    }

    private static bool TryCopyBString(APTR source, out APTR destination,
        out uint bytes, out int ioError)
    {
        destination = APTR.Null;
        bytes = 0;
        ioError = 0;
        if (source.IsNull)
        {
            ioError = (int)DOS.Error.RequiredArgumentMissing;
            return false;
        }

        var length = 0u;
        for (; length <= byte.MaxValue; length++)
        {
            if (APTR.ReadUInt8(source, unchecked((int)length)) == 0) break;
        }
        if (length > byte.MaxValue)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }

        bytes = length + 2;
        destination = Exec.AllocMem(bytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (destination.IsNull)
        {
            bytes = 0;
            ioError = (int)DOS.Error.NoFreeStore;
            return false;
        }
        APTR.WriteUInt8(destination, 0, unchecked((byte)length));
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(destination, unchecked((int)index + 1),
                APTR.ReadUInt8(source, unchecked((int)index)));
        APTR.WriteUInt8(destination, unchecked((int)length + 1), 0);
        return true;
    }
}
