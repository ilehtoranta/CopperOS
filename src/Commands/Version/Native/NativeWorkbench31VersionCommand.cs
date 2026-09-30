using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 Version command candidate.  The classic v40
/// grammar is kept separate from the MorphOS grammar: NAME is a single
/// argument. Ordinary named lookups implement the original Resident,
/// command-segment, trailing-colon DOS device, and loaded-library providers.
/// </summary>
public static class NativeWorkbench31VersionCommand
{
    public const string Template =
        "NAME,VERSION/N,REVISION/N,FILE/S,FULL/S,UNIT/N,INTERNAL/S,RES/S";
    public const uint ResultCount = 8;
    public const uint FileBufferBytes = 16_385;
    public const uint ScratchBufferBytes = 16_449;

    [AmigaLibrary(Utility.Name, AmigaLibraryBasePolicy.CallerProvided)]
    private static class VersionUtilityApi
    {
        [AmigaLvo(UtilityLvo.Stricmp)]
        [return: M68kRegister(M68kRegister.D0)]
        public static extern int Stricmp(
            [M68kRegister(M68kRegister.A6)] APTR libraryBase,
            [M68kRegister(M68kRegister.A0)] CString query,
            [M68kRegister(M68kRegister.A1)] CString residentName);
    }

    private struct Fields
    {
        public uint Name;
        public uint Version;
        public uint Revision;
        public uint Extra;

        public static APTR AddressOf(ref Fields fields) =>
            throw new System.NotSupportedException(
                "Version.Fields.AddressOf is lowered by CopperSharp.");
    }

    public static int Run(out int ioError)
    {
        ioError = 0;
        // C/Version opens both libraries before ReadArgs. Keep the Utility
        // lease invocation-owned and alive through FILE FULL formatting.
        var utility = Exec.OpenLibraryRaw(Utility.Name, 37);
        if (utility.IsNull)
        {
            ioError = (int)DOS.Error.InvalidResidentLibrary;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            Exec.CloseLibrary(utility);
            DOS.SetIoErr((DOS.Error)ioError);
            // Original 40.1 keeps its initial RETURN_FAIL on ReadArgs failure.
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_OK;
        var error = 0;
        var ownedName = APTR.Null;
        var ownedIdString = APTR.Null;
        var commandSegmentScratch = APTR.Null;
        var commandSegmentText = APTR.Null;
        var commandSegmentTextBytes = 0u;
        var providerPath = APTR.Null;
        do
        {
            var name = ReadPointer(arguments, 0);
            var requestedVersion = ReadNumber(arguments, 1,
                out var hasVersion);
            var requestedRevision = ReadNumber(arguments, 2,
                out var hasRevision);
            var file = ReadSwitch(arguments, 3);
            var full = ReadSwitch(arguments, 4);
            // Original 40.1 parses UNIT/INTERNAL/RES but never reads their
            // result slots. FILE limits a named lookup to its direct file
            // provider; the no-name system report remains independent of it.

            if (name.IsNull)
            {
                var execBase = APTR.FromPointer(
                    APTR.ReadUInt32(APTR.FromPointer(4), 0));
                var kickstartFields = default(Fields);
                kickstartFields.Name = CString.ToUInt32("Kickstart");
                kickstartFields.Version = APTR.ReadUInt16(execBase,
                    ExecLayout.Library.Version);
                kickstartFields.Revision = APTR.ReadUInt16(execBase,
                    ExecLayout.ExecBase.SoftVer);
                DOS.VPrintf("%s %lu.%lu, ",
                    Fields.AddressOf(ref kickstartFields));

                var versionLibrary = Exec.OpenLibraryRaw(
                    CString.FromPointer(CString.ToUInt32("version.library")),
                    0);
                if (versionLibrary.IsNull)
                {
                    error = (int)DOS.Error.InvalidResidentLibrary;
                    break;
                }

                var workbenchFields = default(Fields);
                workbenchFields.Name = CString.ToUInt32("Workbench");
                workbenchFields.Version = APTR.ReadUInt16(versionLibrary,
                    ExecLayout.Library.Version);
                workbenchFields.Revision = APTR.ReadUInt16(versionLibrary,
                    ExecLayout.Library.Revision);
                if (full != 0)
                {
                    var systemIdString = APTR.FromPointer(APTR.ReadUInt32(
                        versionLibrary, ExecLayout.Library.IdString));
                    error = NativeWorkbench31VersionFull.Print(
                        APTR.FromPointer(workbenchFields.Name),
                        workbenchFields.Version, workbenchFields.Revision,
                        systemIdString, utility);
                }
                else
                {
                    DOS.VPrintf("%s %lu.%lu\n",
                        Fields.AddressOf(ref workbenchFields));
                }
                Exec.CloseLibrary(versionLibrary);

                if (error == 0 && !MatchesRequested(workbenchFields.Version,
                        workbenchFields.Revision,
                        requestedVersion, hasVersion, requestedRevision,
                        hasRevision))
                    result = DOS.RETURN_WARN;
                break;
            }

            if (file != 0)
            {
                result = PrintDirectFileVersion(name, full, utility,
                    requestedVersion, hasVersion, requestedRevision,
                    hasRevision, out error);
                break;
            }

            var module = FindResidentIgnoreCase(name, utility);
            if (module.IsNull)
            {
                var commandSegmentVersion = false;
                var commandSegmentFound = false;
                var commandSegmentName = APTR.Null;
                var segmentVersionNumber = 0u;
                var segmentRevisionNumber = uint.MaxValue;

                // The original protects FindSegment and the returned segment
                // list while it reads the command's version data.
                Exec.Forbid();
                var commandSegment = DOS.FindSegment(
                    CString.FromPointer(name), APTR.Null, 0);
                if (commandSegment.IsNull)
                    commandSegment = DOS.FindSegment(
                        CString.FromPointer(name), APTR.Null, 1);
                commandSegmentFound = commandSegment.IsNotNull;

                if (commandSegment.IsNotNull)
                {
                    var useCount = unchecked((int)APTR.ReadUInt32(
                        commandSegment, DosLayout.Segment.UseCount));
                    if (useCount == -2 || useCount == -999)
                    {
                        module = FindResidentIgnoreCase(
                            APTR.FromPointer(CString.ToUInt32("shell")),
                            utility);
                    }
                    else
                    {
                        commandSegmentScratch = Exec.AllocMem(
                            ScratchBufferBytes, Exec.MemoryFlags.Any);
                        if (commandSegmentScratch.IsNull)
                        {
                            error = (int)DOS.Error.NoFreeStore;
                        }
                        else
                        {
                            var segmentList = BPTR.FromRaw(
                                APTR.ReadUInt32(commandSegment,
                                    DosLayout.Segment.SegmentList));
                            var segmentStatus = ReadCommandSegmentVersion(
                                segmentList, commandSegmentScratch,
                                out commandSegmentText,
                                out commandSegmentTextBytes,
                                out commandSegmentName,
                                out segmentVersionNumber,
                                out segmentRevisionNumber,
                                out var foundTag);
                            commandSegmentVersion =
                                segmentStatus == DOS.RETURN_OK;
                            if (!commandSegmentVersion && foundTag)
                                result = DOS.RETURN_ERROR;
                            else if (!commandSegmentVersion &&
                                segmentStatus == DOS.RETURN_FAIL)
                                error = (int)DOS.Error.NoFreeStore;
                        }
                    }
                }
                Exec.Permit();

                if (error != 0) break;
                if (commandSegmentVersion)
                {
                    result = PrintParsedFileVersion(commandSegmentName,
                        segmentVersionNumber, segmentRevisionNumber,
                        commandSegmentText, full, utility,
                        requestedVersion, hasVersion, requestedRevision,
                        hasRevision, out error);
                    break;
                }
                if (module.IsNull)
                {
                    // The original tries the DOS device list only when no
                    // command segment matched and NAME ends in a colon.
                    if (!commandSegmentFound)
                        module = FindTrailingColonDeviceResident(name);
                    if (module.IsNull && !commandSegmentFound)
                    {
                        // Workbench next searches Exec's loaded LibList by
                        // DOS.FilePart(NAME), while holding Forbid. Snapshot
                        // the text used after Permit so the library can be
                        // removed safely once the list unlocks.
                        Exec.Forbid();
                        var execBase = APTR.FromPointer(
                            APTR.ReadUInt32(APTR.FromPointer(4), 0));
                        var libraryList = APTR.FromPointer(execBase.Raw +
                            (uint)ExecLayout.ExecBase.LibraryList);
                        var providerName = (APTR)DOS.FilePart(
                            CString.FromPointer(name.Raw));
                        var librarySnapshot = default(Fields);
                        librarySnapshot = SnapshotLoadedNode(libraryList,
                            providerName, utility, full, ref ownedName,
                            ref ownedIdString, out error);
                        Exec.Permit();

                        if (error != 0) break;
                        var snapshot = Fields.AddressOf(
                            ref librarySnapshot);
                        if (APTR.ReadUInt32(snapshot, 0) != 0)
                        {
                            result = PrintParsedFileVersion(
                                APTR.FromPointer(APTR.ReadUInt32(snapshot, 0)),
                                APTR.ReadUInt32(snapshot, 4),
                                APTR.ReadUInt32(snapshot, 8),
                                APTR.FromPointer(APTR.ReadUInt32(snapshot, 12)),
                                full, utility,
                                requestedVersion, hasVersion,
                                requestedRevision, hasRevision,
                                out error);
                            break;
                        }

                        // Workbench next searches Exec's DeviceList with the
                        // same basename and comparison helper used for LibList.
                        var deviceSnapshot = default(Fields);
                        Exec.Forbid();
                        var deviceList = APTR.FromPointer(execBase.Raw +
                            (uint)ExecLayout.ExecBase.DeviceList);
                        deviceSnapshot = SnapshotLoadedNode(deviceList,
                            providerName, utility, full, ref ownedName,
                            ref ownedIdString, out error);
                        Exec.Permit();

                        if (error != 0) break;
                        snapshot = Fields.AddressOf(ref deviceSnapshot);
                        if (APTR.ReadUInt32(snapshot, 0) != 0)
                        {
                            result = PrintParsedFileVersion(
                                APTR.FromPointer(APTR.ReadUInt32(snapshot, 0)),
                                APTR.ReadUInt32(snapshot, 4),
                                APTR.ReadUInt32(snapshot, 8),
                                APTR.FromPointer(APTR.ReadUInt32(snapshot, 12)),
                                full, utility,
                                requestedVersion, hasVersion,
                                requestedRevision, hasRevision,
                                out error);
                            break;
                        }

                        // The original next tries the complete NAME as a
                        // file, then LIBS: and DEVS: plus its FilePart.
                        result = PrintDefaultFileVersion(name, full,
                            utility, requestedVersion, hasVersion,
                            requestedRevision, hasRevision,
                            out var fileOpened, out error);
                        if (error != 0 || fileOpened) break;

                        var libraryFilePart = (APTR)DOS.FilePart(
                            CString.FromPointer(name.Raw));
                        providerPath = BuildPrefixedFilePath(true,
                            libraryFilePart, out error);
                        if (error != 0) break;
                        result = PrintDefaultFileVersion(providerPath, full,
                            utility, requestedVersion, hasVersion,
                            requestedRevision, hasRevision,
                            out fileOpened, out error);
                        if (error != 0 || fileOpened) break;
                        Exec.FreeVec(providerPath);
                        providerPath = APTR.Null;

                        var deviceFilePart = (APTR)DOS.FilePart(
                            CString.FromPointer(name.Raw));
                        providerPath = BuildPrefixedFilePath(false,
                            deviceFilePart, out error);
                        if (error != 0) break;
                        result = PrintDefaultFileVersion(providerPath, full,
                            utility, requestedVersion, hasVersion,
                            requestedRevision, hasRevision,
                            out fileOpened, out error);
                        if (error == 0 && !fileOpened)
                        {
                            // The source reports the final DOS provider's
                            // error when every default lookup misses. Preserve
                            // that IoErr for PrintFault instead of replacing
                            // an ordinary missing-object error with a generic
                            // command failure.
                            error = (int)DOS.IoErr();
                            if (error == 0)
                                error = (int)DOS.Error.ObjectNotFound;
                        }
                        break;
                    }
                    if (module.IsNull)
                    {
                        error = (int)DOS.Error.NotImplemented;
                        break;
                    }
                }
            }

            var version = APTR.ReadUInt8(module,
                ExecLayout.Resident.Version);
            var idString = APTR.FromPointer(APTR.ReadUInt32(module,
                ExecLayout.Resident.IdString));
            var revision = TryReadRevision(idString,
                out var parsedRevision) ? parsedRevision : uint.MaxValue;
            var canonicalName = APTR.FromPointer(APTR.ReadUInt32(module,
                ExecLayout.Resident.Name));
            ownedName = CopyName(canonicalName);
            if (ownedName.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }
            var residentFields = default(Fields);
            residentFields.Name = ownedName.Raw;
            residentFields.Version = version;
            residentFields.Revision = revision;
            if (full != 0)
                error = NativeWorkbench31VersionFull.Print(ownedName,
                    version, revision, idString, utility);
            else
                DOS.VPrintf("%s %ld.%ld\n",
                    Fields.AddressOf(ref residentFields));

            if (!MatchesRequested(version, revision, requestedVersion,
                    hasVersion, requestedRevision, hasRevision))
                result = DOS.RETURN_WARN;
        }
        while (false);

        if (commandSegmentText.IsNotNull)
            Exec.FreeMem(commandSegmentText, commandSegmentTextBytes);
        if (commandSegmentScratch.IsNotNull)
            Exec.FreeMem(commandSegmentScratch, ScratchBufferBytes);
        if (providerPath.IsNotNull) Exec.FreeVec(providerPath);
        if (ownedIdString.IsNotNull) Exec.FreeVec(ownedIdString);
        if (ownedName.IsNotNull) Exec.FreeVec(ownedName);
        arguments.Release();
        if (error != 0)
        {
            ioError = error;
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            result = DOS.RETURN_FAIL;
        }
        Exec.CloseLibrary(utility);
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static int PrintDirectFileVersion(APTR fileName, uint full,
        APTR utility, int requestedVersion, bool hasVersion,
        int requestedRevision, bool hasRevision, out int error)
    {
        return PrintFileVersionPath(fileName, full, utility,
            requestedVersion, hasVersion, requestedRevision, hasRevision,
            false, out _, out error);
    }

    private static int PrintDefaultFileVersion(APTR fileName, uint full,
        APTR utility, int requestedVersion, bool hasVersion,
        int requestedRevision, bool hasRevision, out bool fileOpened,
        out int error)
    {
        return PrintFileVersionPath(fileName, full, utility,
            requestedVersion, hasVersion, requestedRevision, hasRevision,
            true, out fileOpened, out error);
    }

    private static int PrintFileVersionPath(APTR fileName, uint full,
        APTR utility, int requestedVersion, bool hasVersion,
        int requestedRevision, bool hasRevision, bool ignoreMissingFile,
        out bool fileOpened, out int error)
    {
        fileOpened = false;
        error = 0;
        var file = DOS.OpenRaw(CString.FromPointer(fileName),
            DOS.FileMode.OldFile);
        if (file.IsNull)
        {
            var openError = (int)DOS.IoErr();
            if (!ignoreMissingFile ||
                openError != (int)DOS.Error.ObjectNotFound)
                error = openError;
            return DOS.RETURN_FAIL;
        }
        fileOpened = true;

        var versionText = APTR.Null;
        var scanBuffer = APTR.Null;
        var segment = BPTR.Null;
        var result = DOS.RETURN_FAIL;
        var found = false;
        var isHunk = false;
        var idString = APTR.Null;
        var version = 0u;
        var revision = uint.MaxValue;
        var canonicalName = APTR.Null;

        versionText = Exec.AllocMem(FileBufferBytes,
            Exec.MemoryFlags.Public);
        if (versionText.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            goto cleanup;
        }

        scanBuffer = Exec.AllocMem(ScratchBufferBytes,
            Exec.MemoryFlags.Public);
        if (scanBuffer.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            goto cleanup;
        }

        error = ScanDirectFileForVersion(file, versionText, scanBuffer,
            out found, out isHunk, out idString);
        if (error != 0) goto cleanup;

        // The original closes the raw file before parsing or LoadSeg.
        DOS.Close(file);
        file = BPTR.Null;

        if (found && TryParseDirectFileVersion(versionText, idString,
                out canonicalName, out version, out revision))
        {
            result = PrintParsedFileVersion(canonicalName, version,
                revision, idString, full, utility, requestedVersion,
                hasVersion, requestedRevision, hasRevision, out error);
            goto cleanup;
        }

        if (isHunk)
        {
            segment = BPTR.FromRaw(
                NativeWorkbench31VersionDosRaw.LoadSeg(
                    CString.FromPointer(fileName)));
            if (segment.IsNotNull)
            {
                var resident = FindLoadedResident(segment.Raw);
                if (resident.IsNotNull)
                {
                    canonicalName = APTR.FromPointer(APTR.ReadUInt32(
                        resident, ExecLayout.Resident.Name));
                    idString = APTR.FromPointer(APTR.ReadUInt32(
                        resident, ExecLayout.Resident.IdString));
                    version = APTR.ReadUInt8(resident,
                        ExecLayout.Resident.Version) & 0xffu;
                    revision = TryReadRevision(idString,
                        out var parsedRevision)
                        ? parsedRevision : uint.MaxValue;
                    if (canonicalName.IsNotNull)
                    {
                        result = PrintParsedFileVersion(canonicalName,
                            version, revision, idString, full, utility,
                            requestedVersion, hasVersion,
                            requestedRevision, hasRevision, out error);
                        goto cleanup;
                    }
                }
            }
        }

        PrintMissingFileVersion(fileName);
        result = DOS.RETURN_FAIL;

    cleanup:
        if (segment.IsNotNull)
            NativeWorkbench31VersionDosRaw.UnLoadSeg(segment);
        if (file.IsNotNull) DOS.Close(file);
        if (scanBuffer.IsNotNull)
            Exec.FreeMem(scanBuffer, ScratchBufferBytes);
        if (versionText.IsNotNull)
            Exec.FreeMem(versionText, FileBufferBytes);
        return error == 0 ? result : DOS.RETURN_FAIL;
    }

    private static APTR BuildPrefixedFilePath(bool libraryPrefix,
        APTR filePart, out int error)
    {
        error = 0;
        var filePartBytes = 0u;
        while (APTR.ReadUInt8(filePart,
                unchecked((int)filePartBytes)) != 0)
        {
            if (filePartBytes == uint.MaxValue - 7u)
            {
                error = (int)DOS.Error.LineTooLong;
                return APTR.Null;
            }
            filePartBytes++;
        }

        const uint prefixBytes = 5;
        var allocationBytes = prefixBytes + filePartBytes + 1u;
        var path = Exec.AllocVec(allocationBytes, 0);
        if (path.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            return APTR.Null;
        }

        var prefixValue = libraryPrefix
            ? CString.ToUInt32("LIBS:") : CString.ToUInt32("DEVS:");
        var prefix = APTR.FromPointer(prefixValue);
        for (var index = 0u; index < prefixBytes; index++)
            APTR.WriteUInt8(path, unchecked((int)index),
                APTR.ReadUInt8(prefix, unchecked((int)index)));
        for (var index = 0u; index < filePartBytes; index++)
            APTR.WriteUInt8(path,
                unchecked((int)(prefixBytes + index)),
                APTR.ReadUInt8(filePart, unchecked((int)index)));
        APTR.WriteUInt8(path,
            unchecked((int)(prefixBytes + filePartBytes)), 0);
        return path;
    }

    private static int PrintParsedFileVersion(APTR canonicalName,
        uint version, uint revision, APTR idString, uint full,
        APTR utility, int requestedVersion, bool hasVersion,
        int requestedRevision, bool hasRevision, out int error)
    {
        error = 0;
        if (full != 0)
        {
            error = NativeWorkbench31VersionFull.Print(canonicalName,
                version, revision, idString, utility);
            if (error != 0) return DOS.RETURN_FAIL;
        }
        else
        {
            var fields = default(Fields);
            fields.Name = canonicalName.Raw;
            fields.Version = version;
            fields.Revision = revision;
            DOS.VPrintf("%s %ld.%ld\n", Fields.AddressOf(ref fields));
        }

        return MatchesRequested(version, revision, requestedVersion,
                hasVersion, requestedRevision, hasRevision)
            ? DOS.RETURN_OK : DOS.RETURN_WARN;
    }

    private static int ReadCommandSegmentVersion(BPTR segmentList,
        APTR scratch, out APTR ownedText, out uint ownedTextBytes,
        out APTR canonicalName, out uint version, out uint revision,
        out bool foundTag)
    {
        ownedText = APTR.Null;
        ownedTextBytes = 0;
        canonicalName = APTR.Null;
        version = 0;
        revision = uint.MaxValue;
        foundTag = false;

        var current = segmentList.Raw;
        while (current != 0)
        {
            var memory = APTR.FromPointer(current << 2);
            var segmentBytes = APTR.ReadUInt32(memory, -4);
            if (segmentBytes >= 8u &&
                segmentBytes <= uint.MaxValue - memory.Raw)
            {
                var segmentEnd = memory.Raw + segmentBytes - 4u;
                var cursor = memory.Raw + 4u;
                while (cursor <= segmentEnd &&
                    segmentEnd - cursor >= 5u)
                {
                    if (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) == '$' &&
                        APTR.ReadUInt8(APTR.FromPointer(cursor + 1u), 0) == 'V' &&
                        APTR.ReadUInt8(APTR.FromPointer(cursor + 2u), 0) == 'E' &&
                        APTR.ReadUInt8(APTR.FromPointer(cursor + 3u), 0) == 'R' &&
                        APTR.ReadUInt8(APTR.FromPointer(cursor + 4u), 0) == ':')
                    {
                        foundTag = true;
                        var textStart = cursor + 5u;
                        var textEnd = textStart;
                        while (textEnd < segmentEnd &&
                            APTR.ReadUInt8(APTR.FromPointer(textEnd), 0) != 0)
                            textEnd++;
                        var textLength = textEnd - textStart;
                        if (textLength == 0 ||
                            textLength >= ScratchBufferBytes)
                            return DOS.RETURN_ERROR;

                        ownedTextBytes = textLength + 1u;
                        ownedText = Exec.AllocMem(ownedTextBytes,
                            Exec.MemoryFlags.Any);
                        if (ownedText.IsNull)
                        {
                            ownedTextBytes = 0;
                            return DOS.RETURN_FAIL;
                        }
                        for (var index = 0u; index < textLength; index++)
                        {
                            var value = APTR.ReadUInt8(
                                APTR.FromPointer(textStart),
                                unchecked((int)index));
                            APTR.WriteUInt8(ownedText,
                                unchecked((int)index), value);
                            APTR.WriteUInt8(scratch,
                                unchecked((int)index), value);
                        }
                        APTR.WriteUInt8(ownedText,
                            unchecked((int)textLength), 0);
                        APTR.WriteUInt8(scratch,
                            unchecked((int)textLength), 0);

                        return TryParseDirectFileVersion(scratch, ownedText,
                            out canonicalName, out version, out revision)
                            ? DOS.RETURN_OK : DOS.RETURN_ERROR;
                    }
                    cursor++;
                }
            }
            current = APTR.ReadUInt32(memory, 0);
        }

        return DOS.RETURN_ERROR;
    }

    private static int ScanDirectFileForVersion(BPTR file,
        APTR versionText, APTR scanBuffer, out bool found,
        out bool isHunk, out APTR idString)
    {
        found = false;
        isHunk = false;
        idString = APTR.Null;
        var buffered = 0u;
        var firstRead = true;

        while (true)
        {
            var room = FileBufferBytes - 1u - buffered;
            if (room == 0) return 0;
            var read = DOS.Read(file,
                APTR.FromPointer(scanBuffer.Raw + buffered),
                unchecked((int)room));
            if (read < 0) return (int)DOS.IoErr();
            if ((uint)read > room)
                return (int)DOS.Error.LineTooLong;
            if (read == 0) return 0;

            if (firstRead)
            {
                firstRead = false;
                isHunk = buffered == 0 && (uint)read >= 4u &&
                    APTR.ReadUInt32(scanBuffer, 0) == 0x000003f3u;
            }

            buffered += unchecked((uint)read);
            APTR.WriteUInt8(scanBuffer, unchecked((int)buffered), 0);
            for (var offset = 0u; offset + 5u <= buffered; offset++)
            {
                if (APTR.ReadUInt8(scanBuffer,
                        unchecked((int)offset)) != (uint)'$' ||
                    APTR.ReadUInt8(scanBuffer,
                        unchecked((int)(offset + 1))) != (uint)'V' ||
                    APTR.ReadUInt8(scanBuffer,
                        unchecked((int)(offset + 2))) != (uint)'E' ||
                    APTR.ReadUInt8(scanBuffer,
                        unchecked((int)(offset + 3))) != (uint)'R' ||
                    APTR.ReadUInt8(scanBuffer,
                        unchecked((int)(offset + 4))) != (uint)':')
                    continue;

                idString = APTR.FromPointer(scanBuffer.Raw + offset);
                var length = buffered - offset;
                for (var index = 0u; index < length; index++)
                    APTR.WriteUInt8(versionText,
                        unchecked((int)index),
                        APTR.ReadUInt8(scanBuffer,
                            unchecked((int)(offset + index))));
                APTR.WriteUInt8(versionText, unchecked((int)length), 0);
                found = true;
                return 0;
            }

            // Retain four bytes to catch a "$VER:" tag crossing DOS.Read
            // boundaries while scanning large files.
            var carry = buffered < 4u ? buffered : 4u;
            var carryStart = buffered - carry;
            for (var index = 0u; index < carry; index++)
                APTR.WriteUInt8(scanBuffer, unchecked((int)index),
                    APTR.ReadUInt8(scanBuffer,
                        unchecked((int)(carryStart + index))));
            buffered = carry;
        }
    }

    private static bool TryParseDirectFileVersion(APTR versionText,
        APTR idString, out APTR canonicalName, out uint version,
        out uint revision)
    {
        canonicalName = APTR.Null;
        version = 0;
        revision = uint.MaxValue;
        var cursor = versionText.Raw;
        if (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) == '$' &&
            APTR.ReadUInt8(APTR.FromPointer(cursor + 1), 0) == 'V' &&
            APTR.ReadUInt8(APTR.FromPointer(cursor + 2), 0) == 'E' &&
            APTR.ReadUInt8(APTR.FromPointer(cursor + 3), 0) == 'R' &&
            APTR.ReadUInt8(APTR.FromPointer(cursor + 4), 0) == ':')
            cursor += 5;
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) is (byte)' ' or (byte)'\t')
            cursor++;
        var nameStart = cursor;
        var nameEnd = cursor;
        var versionStart = 0u;
        while (true)
        {
            var tokenEnd = cursor;
            while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) is not 0 and
                   not (byte)'\n' and not (byte)'\r' and not (byte)' ' and
                   not (byte)'\t')
                cursor++;
            tokenEnd = cursor;
            while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) is (byte)' ' or (byte)'\t')
                cursor++;
            if (IsAsciiDigit(APTR.ReadUInt8(APTR.FromPointer(cursor), 0)))
            {
                nameEnd = tokenEnd;
                versionStart = cursor;
                break;
            }
            var delimiter = APTR.ReadUInt8(APTR.FromPointer(cursor), 0);
            if (delimiter == 0 || delimiter == (byte)'\n' ||
                delimiter == (byte)'\r')
                return false;
        }

        var number = default(Fields);
        var parsedNumber = Fields.AddressOf(ref number);
        if (DOS.StrToLong(CString.FromPointer(versionStart),
                parsedNumber) <= 0)
            return false;
        version = APTR.ReadUInt32(parsedNumber, 0);
        if (TryReadRevision(idString, out var parsedRevision))
            revision = parsedRevision;
        if (nameEnd <= nameStart) return false;
        APTR.WriteUInt8(APTR.FromPointer(nameEnd), 0, 0);
        canonicalName = APTR.FromPointer(nameStart);
        return true;
    }

    private static APTR FindLoadedResident(uint segmentRaw)
    {
        var currentSegment = segmentRaw;
        while (currentSegment != 0)
        {
            var memory = APTR.FromPointer(currentSegment << 2);
            var segmentBytes = APTR.ReadUInt32(memory, -4);
            if (segmentBytes >= 16u)
            {
                var cursor = memory.Raw + 4u;
                var end = memory.Raw + segmentBytes - 16u;
                while (cursor + 6u <= end)
                {
                    var resident = APTR.FromPointer(cursor);
                    if (APTR.ReadUInt16(resident,
                            ExecLayout.Resident.MatchWord) == 0x4afc &&
                        APTR.ReadUInt32(resident,
                            ExecLayout.Resident.MatchTag) == resident.Raw)
                        return resident;
                    cursor += 2u;
                }
            }
            currentSegment = APTR.ReadUInt32(memory, 0);
        }
        return APTR.Null;
    }

    private static APTR FindLoadedLibrary(APTR list, APTR wantedName,
        APTR utility)
    {
        var node = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        var sentinel = list.Raw + (uint)ExecLayout.List.Tail;
        while (node.IsNotNull && node.Raw != sentinel)
        {
            var nodeName = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Name));
            if (nodeName.IsNotNull)
            {
                var comparison = VersionUtilityApi.Stricmp(utility,
                    CString.FromPointer(wantedName.Raw),
                    CString.FromPointer(nodeName.Raw));
                if (comparison == 0) return node;
            }
            node = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Successor));
        }
        return APTR.Null;
    }

    private static Fields SnapshotLoadedNode(APTR list, APTR wantedName,
        APTR utility, uint full, ref APTR ownedName,
        ref APTR ownedIdString, out int error)
    {
        error = 0;
        var snapshot = default(Fields);
        var node = FindLoadedLibrary(list, wantedName, utility);
        if (node.IsNull) return snapshot;

        var canonicalName = APTR.FromPointer(APTR.ReadUInt32(node,
            ExecLayout.Node.Name));
        snapshot.Version = APTR.ReadUInt16(node,
            ExecLayout.Library.Version);
        snapshot.Revision = APTR.ReadUInt16(node,
            ExecLayout.Library.Revision);
        var idString = APTR.FromPointer(APTR.ReadUInt32(node,
            ExecLayout.Library.IdString));
        ownedName = CopyName(canonicalName);
        if (ownedName.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            return snapshot;
        }
        if (full != 0 && idString.IsNotNull)
        {
            ownedIdString = CopyName(idString);
            if (ownedIdString.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                return snapshot;
            }
        }

        snapshot.Name = ownedName.Raw;
        snapshot.Extra = ownedIdString.Raw;
        return snapshot;
    }

    private static APTR FindTrailingColonDeviceResident(APTR name)
    {
        if (name.IsNull) return APTR.Null;

        var length = 0;
        while (APTR.ReadUInt8(name, length) != 0) length++;
        if (length == 0 || APTR.ReadUInt8(name, length - 1) != (byte)':')
            return APTR.Null;

        const uint listFlags = (uint)(DosListLockFlags.Devices |
            DosListLockFlags.Read);
        APTR.WriteUInt8(name, length - 1, 0);
        var list = DOS.LockDosList(listFlags);
        var device = DOS.FindDosEntry(list, CString.FromPointer(name.Raw),
            (uint)DosListLockFlags.Devices);
        DOS.UnLockDosList(listFlags);
        APTR.WriteUInt8(name, length - 1, (byte)':');

        if (device.IsNull || APTR.ReadUInt32(device,
                DosLayout.DeviceNode.Startup) == 0)
            return APTR.Null;

        var segmentList = APTR.ReadUInt32(device,
            DosLayout.DeviceNode.SegmentList);
        return segmentList == 0
            ? APTR.Null : FindLoadedResident(segmentList);
    }

    private static void PrintMissingFileVersion(APTR fileName)
    {
        var diagnostic = default(Fields);
        diagnostic.Name = fileName.Raw;
        DOS.VPrintf("Could not find version information for '%s'\n",
            Fields.AddressOf(ref diagnostic));
    }

    private static APTR ReadPointer(NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value)
            ? APTR.FromPointer(value) : APTR.Null;

    private static uint ReadSwitch(NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value) ? value : 0;

    private static int ReadNumber(NativeCommandArguments arguments,
        uint index, out bool present)
    {
        present = false;
        if (!arguments.TryGetResult(index, out var value) || value == 0)
            return 0;
        present = true;
        return unchecked((int)APTR.ReadUInt32(APTR.FromPointer(value), 0));
    }

    private static bool MatchesRequested(uint version, uint revision,
        int requestedVersion, bool hasVersion, int requestedRevision,
        bool hasRevision)
    {
        // Original Version 40.1 CODE 0x0232-0x0268 uses signed LONG
        // comparisons after printing. A higher major satisfies the request
        // regardless of revision; REVISION alone compares the revision.
        var actualVersion = unchecked((int)version);
        if (hasVersion && actualVersion != requestedVersion)
            return actualVersion > requestedVersion;
        return !hasRevision || unchecked((int)revision) >= requestedRevision;
    }

    private static bool TryReadRevision(APTR ids, out uint revision)
    {
        revision = uint.MaxValue;
        if (ids.IsNull) return false;
        var cursor = ids.Raw;
        if (APTR.ReadUInt8(ids, 0) == '$' &&
            APTR.ReadUInt8(ids, 1) == 'V' &&
            APTR.ReadUInt8(ids, 2) == 'E' &&
            APTR.ReadUInt8(ids, 3) == 'R' &&
            APTR.ReadUInt8(ids, 4) == ':') cursor += 5;
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) == ' ') cursor++;
        // Original 0x05b8-0x0612 scans space-delimited name tokens until a
        // token starts with a decimal digit. It does not truncate the name.
        while (true)
        {
            while (true)
            {
                var c = APTR.ReadUInt8(APTR.FromPointer(cursor), 0);
                if (c == 0 || c == '\n' || c == '\r') return false;
                if (c == ' ') break;
                cursor++;
            }
            while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) == ' ') cursor++;
            if (IsAsciiDigit(APTR.ReadUInt8(APTR.FromPointer(cursor), 0))) break;
        }

        var number = default(Fields);
        var parsed = Fields.AddressOf(ref number);
        var consumed = DOS.StrToLong(CString.FromPointer(cursor), parsed);
        if (consumed <= 0) return false;
        cursor += unchecked((uint)consumed);
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) == ' ') cursor++;
        if (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) != '.') return false;
        consumed = DOS.StrToLong(CString.FromPointer(cursor + 1), parsed);
        if (consumed <= 0) return false;
        revision = APTR.ReadUInt32(parsed, 0);
        return true;
    }

    private static APTR FindResidentIgnoreCase(APTR name, APTR utility)
    {
        var exec = APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));
        var cursor = APTR.ReadUInt32(exec, ExecLayout.ExecBase.ResModules);
        while (cursor != 0)
        {
            var entry = APTR.ReadUInt32(APTR.FromPointer(cursor), 0);
            if (entry == 0) return APTR.Null;
            if ((entry & 0x80000000) != 0)
            {
                cursor = entry & 0x7fffffff;
                continue;
            }
            var resident = APTR.FromPointer(entry);
            var residentName = APTR.ReadUInt32(resident, ExecLayout.Resident.Name);
            if (VersionUtilityApi.Stricmp(utility,
                    CString.FromPointer(name.Raw),
                    CString.FromPointer(residentName)) == 0)
                return resident;
            cursor += 4;
        }
        return APTR.Null;
    }

    private static APTR CopyName(APTR name)
    {
        var length = 0;
        while (APTR.ReadUInt8(name, length) != 0) length++;
        var copy = Exec.AllocVec(unchecked((uint)(length + 1)),
            (uint)Exec.MemoryFlags.Clear);
        if (copy.IsNotNull)
            for (var index = 0; index <= length; index++)
                APTR.WriteUInt8(copy, index, APTR.ReadUInt8(name, index));
        return copy;
    }

    private static bool IsAsciiDigit(uint value) =>
        value >= (uint)'0' && value <= (uint)'9';
}

[AmigaLibrary(DOS.Name)]
internal static class NativeWorkbench31VersionDosRaw
{
    [AmigaLvo(-150)]
    [return: M68kRegister(M68kRegister.D0)]
    public static extern uint LoadSeg(
        [M68kRegister(M68kRegister.D1)] CString name);

    [AmigaLvo(-156)]
    public static extern void UnLoadSeg(
        [M68kRegister(M68kRegister.D1)] BPTR segment);
}
