using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-observed MorphOS Version command boundary.  The system-version and
/// resident paths use public DOS/Exec state. Direct FILE scans and checksum
/// state are invocation-owned. Ambient ARexx discovery uses the public
/// rexxsyslib.library and Exec message-port APIs.
/// </summary>
public static class NativeMorphOSVersionCommand
{
    public const string Template =
        "NAME/M,MD5SUM/S,VERSION/N,REVISION/N,FILE/S,FULL/S,RES/S";
    public const uint ResultCount = 7;

    private const uint MaxNames = 256;
    private const uint FileBufferBytes = 16_385;
    private const uint FilePayloadBytes = FileBufferBytes - 1;
    private const uint AmbientPathBytes = 256;
    public const uint ScratchBufferBytes = FileBufferBytes + 64;
    private const uint Md5ContextBytes = 96;
    private const uint CtrlCMask = 1u << 12;
    private const ushort ResidentMatchWord = 0x4afc;
    private const uint ExtendedResidentFlag = 1u << 6;
    private const int InternalSegmentUseCount = -2;
    private const int DisabledSegmentUseCount = -999;
    private const uint RexxCommandWithResult = 0x0102_0000;
    private const uint RexxCommandBytes = 7;

    private static class RexxMessageLayout
    {
        public const int Result1 = 32;
        public const int Result2 = 36;
        public const int Arguments = 40;
        public const int Action = 28;
    }

    [AmigaLibrary(RexxSysLib.Name,
        AmigaLibraryBasePolicy.CallerProvided)]
    private static class AmbientRexxApi
    {
        [AmigaLvo(-126)]
        [return: M68kRegister(M68kRegister.D0)]
        public static extern uint CreateArgstring(
            [M68kRegister(M68kRegister.A6)] APTR libraryBase,
            [M68kRegister(M68kRegister.A0)] CString value,
            [M68kRegister(M68kRegister.D0)] uint length);

        [AmigaLvo(-132)]
        public static extern void DeleteArgstring(
            [M68kRegister(M68kRegister.A6)] APTR libraryBase,
            [M68kRegister(M68kRegister.A0)] APTR value);

        [AmigaLvo(-138)]
        [return: M68kRegister(M68kRegister.D0)]
        public static extern uint LengthArgstring(
            [M68kRegister(M68kRegister.A6)] APTR libraryBase,
            [M68kRegister(M68kRegister.A0)] APTR value);

        [AmigaLvo(-144)]
        [return: M68kRegister(M68kRegister.D0)]
        public static extern APTR CreateRexxMsg(
            [M68kRegister(M68kRegister.A6)] APTR libraryBase,
            [M68kRegister(M68kRegister.A0)] APTR replyPort,
            [M68kRegister(M68kRegister.A1)] CString extension,
            [M68kRegister(M68kRegister.D0)] uint hostName);

        [AmigaLvo(-150)]
        public static extern void DeleteRexxMsg(
            [M68kRegister(M68kRegister.A6)] APTR libraryBase,
            [M68kRegister(M68kRegister.A0)] APTR message);
    }

    [AmigaLibrary(Utility.Name,
        AmigaLibraryBasePolicy.CallerProvided)]
    private static class VersionUtilityApi
    {
        [AmigaLvo(UtilityLvo.Stricmp)]
        [return: M68kRegister(M68kRegister.D0)]
        public static extern int Stricmp(
            [M68kRegister(M68kRegister.A6)] APTR libraryBase,
            [M68kRegister(M68kRegister.A0)] CString left,
            [M68kRegister(M68kRegister.A1)] CString right);
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

    private struct FileFields
    {
        public uint Name;
        public uint NameSpace;
        public uint Version;
        public uint Revision;
        public uint Padding;
        public uint Date;
        public uint ExtraLineFeed;
        public uint Extra;

        public static APTR AddressOf(ref FileFields fields) =>
            throw new System.NotSupportedException(
                "Version.FileFields.AddressOf is lowered by CopperSharp.");
    }

    private struct FileVersion
    {
        public APTR Name;
        public APTR VersionText;
        public APTR RevisionText;
        public APTR Padding;
        public APTR Date;
        public APTR ExtraLineFeed;
        public APTR Extra;
        public int Version;
        public int Revision;
    }

    private struct FileLease
    {
        public BPTR File;
        public APTR Buffer;
        public APTR Scratch;
        public APTR Md5Context;
    }

    private struct DigestFields
    {
        public uint A;
        public uint B;
        public uint C;
        public uint D;

        public static APTR AddressOf(ref DigestFields fields) =>
            throw new System.NotSupportedException(
                "Version.DigestFields.AddressOf is lowered by CopperSharp.");
    }

    private struct NumberCell
    {
        public uint Value;

        public static APTR AddressOf(ref NumberCell cell) =>
            throw new System.NotSupportedException(
                "Version.NumberCell.AddressOf is lowered by CopperSharp.");
    }

    private struct AmbientTextCells
    {
        public uint A;
        public uint B;
        public uint C;
        public uint D;
        public uint E;
        public uint F;
        public uint G;
        public uint H;

        public static APTR AddressOf(ref AmbientTextCells cells) =>
            throw new System.NotSupportedException(
                "Version.AmbientTextCells.AddressOf is lowered by CopperSharp.");
    }

#pragma warning disable CS0649
    private struct AmbientPathCells
    {
        public uint A;
        public uint B;
        public uint C;
        public uint D;
        public uint E;
        public uint F;
        public uint G;
        public uint H;
        public uint I;
        public uint J;
        public uint K;
        public uint L;
        public uint M;
        public uint N;
        public uint O;
        public uint P;
        public uint Q;
        public uint R;
        public uint S;
        public uint T;
        public uint U;
        public uint V;
        public uint W;
        public uint X;
        public uint Y;
        public uint Z;
        public uint AA;
        public uint AB;
        public uint AC;
        public uint AD;
        public uint AE;
        public uint AF;
        public uint AG;
        public uint AH;
        public uint AI;
        public uint AJ;
        public uint AK;
        public uint AL;
        public uint AM;
        public uint AN;
        public uint AO;
        public uint AP;
        public uint AQ;
        public uint AR;
        public uint AS;
        public uint AT;
        public uint AU;
        public uint AV;
        public uint AW;
        public uint AX;
        public uint AY;
        public uint AZ;
        public uint BA;
        public uint BB;
        public uint BC;
        public uint BD;
        public uint BE;
        public uint BF;
        public uint BG;
        public uint BH;
        public uint BI;
        public uint BJ;
        public uint BK;
        public uint BL;

        public static APTR AddressOf(ref AmbientPathCells cells) =>
            throw new System.NotSupportedException(
                "Version.AmbientPathCells.AddressOf is lowered by CopperSharp.");
    }
#pragma warning restore CS0649

    private struct OneStringFields
    {
        public uint Value;

        public static APTR AddressOf(ref OneStringFields fields) =>
            throw new System.NotSupportedException(
                "Version.OneStringFields.AddressOf is lowered by CopperSharp.");
    }

    // A byte-addressed DOS DateTime view.  The fields reserve enough stack
    // storage; writing by the public DOS layout avoids host-ABI padding.
    private struct DateCells
    {
        public uint A;
        public uint B;
        public uint C;
        public uint D;
        public uint E;
        public uint F;
        public uint G;

        public static APTR AddressOf(ref DateCells cells) =>
            throw new System.NotSupportedException(
                "Version.DateCells.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Implements the source-bound system, RES and direct FILE paths.
    /// Unsupported providers return an explicit error rather than inspecting
    /// host files or silently approximating the source command.
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

        var result = DOS.RETURN_OK;
        var error = 0;
        do
        {
            var names = ReadPointer(ref arguments, 0);
            var md5 = ReadSwitch(ref arguments, 1);
            var requestedVersion = ReadNumber(ref arguments, 2,
                out var hasVersion);
            var requestedRevision = ReadNumber(ref arguments, 3,
                out var hasRevision);
            var file = ReadSwitch(ref arguments, 4);
            var full = ReadSwitch(ref arguments, 5);
            var resident = ReadSwitch(ref arguments, 6);

            if (names.IsNull)
            {
                result = RunSystemVersion(md5, full, requestedVersion,
                    hasVersion, requestedRevision, hasRevision,
                    out ioError);
                break;
            }

            if (file != 0)
            {
                if (resident != 0)
                {
                    DOS.PrintFault(DOS.Error.ObjectNotFound,
                        CString.FromPointer(0));
                    result = DOS.RETURN_FAIL;
                    break;
                }

                result = RunFileNames(names, md5, full, requestedVersion,
                    hasVersion, requestedRevision, hasRevision,
                    out ioError);
                break;
            }

            var cursor = names;
            var singleName = APTR.ReadUInt32(cursor, 4) == 0;
            var count = 0u;
            var utilityBase = APTR.Null;
            while (count < MaxNames)
            {
                var name = APTR.ReadUInt32(cursor,
                    unchecked((int)(count * 4)));
                if (name == 0) break;

                var lookupResidentName = STRPTR.ToUInt32(DOS.FilePart(
                    CString.FromPointer(name)));
                var lookupName = APTR.FromPointer(name);
                var lookupNameLength = CStringLengthUnbounded(lookupName);
                var isVolumeName = lookupNameLength != 0 &&
                    APTR.ReadUInt8(APTR.FromPointer(name +
                        lookupNameLength - 1u), 0) == (uint)':';
                var module = isVolumeName ? APTR.Null :
                    APTR.FromPointer(Exec.FindResident(
                        CString.FromPointer(lookupResidentName)));
                var libraryText = APTR.Null;
                var libraryTextBytes = 0u;
                var libraryScratch = APTR.Null;
                var libraryNameCopy = APTR.Null;
                var libraryNameCopyBytes = 0u;
                var libraryVersion = default(FileVersion);
                var libraryVersionReady = false;
                var libraryVersionParsed = false;
                var libraryFound = false;
                var libraryNumericVersion = 0u;
                var libraryNumericRevision = 0u;
                var fallbackStatus = -1;
                var fallbackDigestAvailable = false;
                var fallbackDigest = default(DigestFields);
                var segmentVersion = default(FileVersion);
                var segmentText = APTR.Null;
                var segmentTextBytes = 0u;
                var segmentScratch = APTR.Null;
                var segmentVersionReady = false;
                if (module.IsNull)
                {
                    if (!isVolumeName)
                    {
                    if (utilityBase.IsNull)
                    {
                        utilityBase = Exec.OpenLibraryRaw(Utility.Name, 37);
                        if (utilityBase.IsNull)
                        {
                            error = (int)DOS.Error.InvalidResidentLibrary;
                            break;
                        }
                    }

                    var execBase = APTR.FromPointer(APTR.ReadUInt32(
                        APTR.FromPointer(4), 0));
                    var libraryList = APTR.FromPointer(execBase.Raw +
                        (uint)ExecLayout.ExecBase.LibraryList);
                    Exec.Forbid();
                    var library = FindLibraryNode(libraryList,
                        lookupResidentName, utilityBase);
                    if (library.IsNotNull)
                    {
                        libraryFound = true;
                        libraryNumericVersion = (uint)APTR.ReadUInt16(library,
                            ExecLayout.Library.Version);
                        libraryNumericRevision = (uint)APTR.ReadUInt16(library,
                            ExecLayout.Library.Revision);
                        if (full != 0)
                        {
                            libraryVersionParsed = TryCreateLibraryVersion(
                                library, libraryNumericVersion,
                                libraryNumericRevision,
                                out libraryVersion, out libraryText,
                                out libraryTextBytes, out libraryScratch);
                        }

                        if (!libraryVersionParsed)
                        {
                            if (libraryScratch.IsNotNull)
                            {
                                Exec.FreeMem(libraryScratch,
                                    ScratchBufferBytes);
                                libraryScratch = APTR.Null;
                            }
                            if (libraryText.IsNotNull)
                            {
                                Exec.FreeMem(libraryText,
                                    libraryTextBytes);
                                libraryText = APTR.Null;
                            }
                            libraryTextBytes = 0;
                            libraryVersion = default;
                        }

                        var libraryName = APTR.FromPointer(
                            APTR.ReadUInt32(library,
                                ExecLayout.Node.Name));
                        libraryNameCopyBytes = CStringLength(libraryName,
                            FileBufferBytes - 1u) + 1u;
                        libraryNameCopy = Exec.AllocMem(
                            libraryNameCopyBytes, Exec.MemoryFlags.Any);
                        if (libraryNameCopy.IsNull)
                        {
                            error = (int)DOS.Error.NoFreeStore;
                        }
                        else
                        {
                            for (var index = 0u;
                                index + 1u < libraryNameCopyBytes; index++)
                                APTR.WriteUInt8(libraryNameCopy,
                                    unchecked((int)index),
                                    APTR.ReadUInt8(libraryName,
                                        unchecked((int)index)));
                            APTR.WriteUInt8(libraryNameCopy,
                                unchecked((int)(libraryNameCopyBytes - 1u)),
                                0);
                            libraryVersion.Name = libraryNameCopy;
                            libraryVersion.Version = unchecked((int)
                                libraryNumericVersion);
                            libraryVersion.Revision =
                                unchecked((int)libraryNumericRevision);
                            libraryVersionReady = true;
                        }
                    }
                    Exec.Permit();

                    if (libraryFound && error != 0)
                    {
                        if (libraryScratch.IsNotNull)
                            Exec.FreeMem(libraryScratch,
                                ScratchBufferBytes);
                        if (libraryText.IsNotNull)
                            Exec.FreeMem(libraryText, libraryTextBytes);
                        if (libraryNameCopy.IsNotNull)
                            Exec.FreeMem(libraryNameCopy,
                                libraryNameCopyBytes);
                        break;
                    }

                    if (libraryVersionReady)
                    {
                        if (md5 != 0) PrintNoMd5Available();
                        if (full != 0 && libraryVersionParsed)
                        {
                            PrintFileVersion(libraryVersion, full, md5);
                        }
                        else
                        {
                            var fields = default(Fields);
                            fields.Name = libraryVersion.Name.Raw;
                            fields.Version = unchecked((uint)
                                libraryVersion.Version);
                            fields.Revision = unchecked((uint)
                                libraryVersion.Revision);
                            DOS.VPrintf("%s %lu.%lu\n",
                                Fields.AddressOf(ref fields));
                        }
                        if (libraryScratch.IsNotNull)
                            Exec.FreeMem(libraryScratch,
                                ScratchBufferBytes);
                        if (libraryText.IsNotNull)
                            Exec.FreeMem(libraryText, libraryTextBytes);
                        if (libraryNameCopy.IsNotNull)
                            Exec.FreeMem(libraryNameCopy,
                                libraryNameCopyBytes);
                        if (singleName)
                            result = CompareRequested(
                                unchecked((uint)libraryVersion.Version),
                                unchecked((uint)libraryVersion.Revision),
                                requestedVersion, hasVersion,
                                requestedRevision, hasRevision);
                        count++;
                        continue;
                    }

                    fallbackStatus = RunMorphOSDirectoryFileCandidates(
                            APTR.FromPointer(lookupResidentName), md5, full,
                            requestedVersion, hasVersion,
                            requestedRevision, hasRevision,
                            ref fallbackDigestAvailable,
                            ref fallbackDigest, false,
                            out var libraryFileVersionReady,
                            out var libraryFileVersion, out var libraryFileIoError);
                    if (libraryFileIoError != 0) ioError = libraryFileIoError;
                    var libraryFileVersionFound =
                        fallbackStatus == DOS.RETURN_OK &&
                        libraryFileVersionReady;
                    if (libraryFileVersionFound)
                    {
                        if (singleName)
                            result = CompareRequested(
                                unchecked((uint)libraryFileVersion.Version),
                                unchecked((uint)libraryFileVersion.Revision),
                                requestedVersion, hasVersion,
                                requestedRevision, hasRevision);
                        count++;
                        continue;
                    }

                    var deviceList = APTR.FromPointer(execBase.Raw +
                        (uint)ExecLayout.ExecBase.DeviceList);
                    var deviceFound = false;
                    var deviceVersionReady = false;
                    var deviceVersionParsed = false;
                    var deviceVersion = default(FileVersion);
                    var deviceText = APTR.Null;
                    var deviceTextBytes = 0u;
                    var deviceScratch = APTR.Null;
                    var deviceNameCopy = APTR.Null;
                    var deviceNameCopyBytes = 0u;
                    var deviceVersionNumber = 0u;
                    var deviceRevisionNumber = 0u;

                    Exec.Forbid();
                    var device = FindLibraryNode(deviceList,
                        lookupResidentName, utilityBase);
                    fallbackStatus = -1;
                    if (device.IsNotNull)
                    {
                        deviceFound = true;
                        deviceVersionNumber = (uint)APTR.ReadUInt16(device,
                            ExecLayout.Library.Version);
                        deviceRevisionNumber = (uint)APTR.ReadUInt16(device,
                            ExecLayout.Library.Revision);
                        if (full != 0)
                        {
                            deviceVersionParsed = TryCreateLibraryVersion(
                                device, deviceVersionNumber,
                                deviceRevisionNumber, out deviceVersion,
                                out deviceText, out deviceTextBytes,
                                out deviceScratch);
                        }

                        if (!deviceVersionParsed)
                        {
                            if (deviceScratch.IsNotNull)
                            {
                                Exec.FreeMem(deviceScratch,
                                    ScratchBufferBytes);
                                deviceScratch = APTR.Null;
                            }
                            if (deviceText.IsNotNull)
                            {
                                Exec.FreeMem(deviceText, deviceTextBytes);
                                deviceText = APTR.Null;
                            }
                            deviceTextBytes = 0;
                            deviceVersion = default;
                        }

                        var deviceName = APTR.FromPointer(
                            APTR.ReadUInt32(device,
                                ExecLayout.Node.Name));
                        deviceNameCopyBytes = CStringLength(deviceName,
                            FileBufferBytes - 1u) + 1u;
                        deviceNameCopy = Exec.AllocMem(deviceNameCopyBytes,
                            Exec.MemoryFlags.Any);
                        if (deviceNameCopy.IsNull)
                        {
                            error = (int)DOS.Error.NoFreeStore;
                        }
                        else
                        {
                            for (var index = 0u;
                                index + 1u < deviceNameCopyBytes; index++)
                                APTR.WriteUInt8(deviceNameCopy,
                                    unchecked((int)index),
                                    APTR.ReadUInt8(deviceName,
                                        unchecked((int)index)));
                            APTR.WriteUInt8(deviceNameCopy,
                                unchecked((int)(deviceNameCopyBytes - 1u)),
                                0);
                            deviceVersion.Name = deviceNameCopy;
                            deviceVersion.Version = unchecked((int)
                                deviceVersionNumber);
                            deviceVersion.Revision = unchecked((int)
                                deviceRevisionNumber);
                            deviceVersionReady = true;
                        }
                    }
                    Exec.Permit();

                    if (deviceFound && error != 0)
                    {
                        if (deviceScratch.IsNotNull)
                            Exec.FreeMem(deviceScratch,
                                ScratchBufferBytes);
                        if (deviceText.IsNotNull)
                            Exec.FreeMem(deviceText, deviceTextBytes);
                        if (deviceNameCopy.IsNotNull)
                            Exec.FreeMem(deviceNameCopy,
                                deviceNameCopyBytes);
                        break;
                    }

                    if (deviceVersionReady)
                    {
                        if (md5 != 0) PrintNoMd5Available();
                        if (full != 0 && deviceVersionParsed)
                        {
                            PrintFileVersion(deviceVersion, full, md5);
                        }
                        else
                        {
                            var fields = default(Fields);
                            fields.Name = deviceVersion.Name.Raw;
                            fields.Version = unchecked((uint)
                                deviceVersion.Version);
                            fields.Revision = unchecked((uint)
                                deviceVersion.Revision);
                            DOS.VPrintf("%s %lu.%lu\n",
                                Fields.AddressOf(ref fields));
                        }
                        if (deviceScratch.IsNotNull)
                            Exec.FreeMem(deviceScratch,
                                ScratchBufferBytes);
                        if (deviceText.IsNotNull)
                            Exec.FreeMem(deviceText, deviceTextBytes);
                        if (deviceNameCopy.IsNotNull)
                            Exec.FreeMem(deviceNameCopy,
                                deviceNameCopyBytes);
                        if (singleName)
                            result = CompareRequested(
                                unchecked((uint)deviceVersion.Version),
                                unchecked((uint)deviceVersion.Revision),
                                requestedVersion, hasVersion,
                                requestedRevision, hasRevision);
                        count++;
                        continue;
                    }

                    fallbackStatus = RunMorphOSDirectoryFileCandidates(
                            APTR.FromPointer(lookupResidentName), md5, full,
                            requestedVersion, hasVersion,
                            requestedRevision, hasRevision,
                            ref fallbackDigestAvailable,
                            ref fallbackDigest, true,
                            out var deviceFileVersionReady,
                            out var deviceFileVersion,
                            out var deviceFileIoError);
                    if (deviceFileIoError != 0) ioError = deviceFileIoError;
                    var deviceFileVersionFound =
                        fallbackStatus == DOS.RETURN_OK &&
                        deviceFileVersionReady;
                    if (deviceFileVersionFound)
                    {
                        if (singleName)
                            result = CompareRequested(
                                unchecked((uint)deviceFileVersion.Version),
                                unchecked((uint)deviceFileVersion.Revision),
                                requestedVersion, hasVersion,
                                requestedRevision, hasRevision);
                        count++;
                        continue;
                    }

                    if (resident == 0 && fallbackStatus == -1 &&
                        !isVolumeName)
                    {
                        fallbackStatus = RunOneFile(lookupName, md5, full,
                            false, requestedVersion, hasVersion,
                            requestedRevision, hasRevision,
                            out var directFileIoError, true, false,
                            out var directFileVersionReady,
                            out var directFileVersion,
                            out var directFileDigestAvailable,
                            out var directFileDigest);
                        if (directFileDigestAvailable)
                        {
                            fallbackDigestAvailable = true;
                            fallbackDigest = directFileDigest;
                        }
                        if (directFileIoError != 0)
                            ioError = directFileIoError;
                        if (directFileVersionReady)
                        {
                            if (singleName)
                                result = CompareRequested(
                                    unchecked((uint)directFileVersion.Version),
                                    unchecked((uint)directFileVersion.Revision),
                                    requestedVersion, hasVersion,
                                    requestedRevision, hasRevision);
                            count++;
                            continue;
                        }
                    }
                    }

                    if (resident == 0 && fallbackStatus == -1 &&
                        isVolumeName)
                    {
                        fallbackStatus = RunMorphOSVolumeResident(
                            APTR.FromPointer(name), out module);
                    }

                    if (fallbackStatus == -1)
                    {
                    Exec.Forbid();
                    var commandSegment = DOS.FindSegment(
                        CString.FromPointer(name), APTR.Null, 0);
                    if (commandSegment.IsNull)
                        commandSegment = DOS.FindSegment(
                            CString.FromPointer(name), APTR.Null, 1);

                    if (commandSegment.IsNotNull)
                    {
                        var useCount = unchecked((int)APTR.ReadUInt32(
                            commandSegment, DosLayout.Segment.UseCount));
                        if (useCount == InternalSegmentUseCount ||
                            useCount == DisabledSegmentUseCount)
                        {
                            Exec.Permit();
                            module = APTR.FromPointer(Exec.FindResident(
                                CString.FromPointer(
                                    CString.ToUInt32("shellcmd"))));
                            Exec.Forbid();
                        }
                        else
                        {
                            segmentScratch = Exec.AllocMem(
                                ScratchBufferBytes, Exec.MemoryFlags.Any);
                            if (segmentScratch.IsNull)
                            {
                                error = (int)DOS.Error.NoFreeStore;
                            }
                            else
                            {
                                var segmentList = BPTR.FromRaw(
                                    APTR.ReadUInt32(commandSegment,
                                        DosLayout.Segment.SegmentList));
                                var segmentStatus =
                                    ReadCommandSegmentVersion(segmentList,
                                        segmentScratch,
                                        out segmentVersion,
                                        out segmentText,
                                        out segmentTextBytes,
                                        out var foundTag);
                                segmentVersionReady =
                                    segmentStatus == DOS.RETURN_OK;
                                if (!segmentVersionReady && foundTag)
                                    result = DOS.RETURN_ERROR;
                                else if (!segmentVersionReady &&
                                    segmentStatus == DOS.RETURN_FAIL)
                                    error = (int)DOS.Error.NoFreeStore;
                            }
                        }
                    }
                    Exec.Permit();
                    }

                    if (segmentVersionReady)
                    {
                        if (md5 != 0) PrintNoMd5Available();
                        PrintFileVersion(segmentVersion, full, md5);
                        if (singleName)
                            result = CompareRequested(
                                unchecked((uint)segmentVersion.Version),
                                unchecked((uint)segmentVersion.Revision),
                                requestedVersion, hasVersion,
                                requestedRevision, hasRevision);
                        if (segmentText.IsNotNull)
                            Exec.FreeMem(segmentText, segmentTextBytes);
                        if (segmentScratch.IsNotNull)
                            Exec.FreeMem(segmentScratch,
                                ScratchBufferBytes);
                        count++;
                        continue;
                    }

                    if (segmentText.IsNotNull)
                        Exec.FreeMem(segmentText, segmentTextBytes);
                    if (segmentScratch.IsNotNull)
                        Exec.FreeMem(segmentScratch, ScratchBufferBytes);
                    if (module.IsNull)
                    {
                        if (md5 != 0 && fallbackDigestAvailable)
                        {
                            PrintMd5Digest(fallbackDigest);
                            var noVersion = default(OneStringFields);
                            noVersion.Value = name;
                            DOS.VPrintf("%s\n",
                                OneStringFields.AddressOf(ref noVersion));
                            result = singleName
                                ? DOS.RETURN_FAIL : DOS.RETURN_OK;
                            count++;
                            continue;
                        }
                        if (error == 0 && result != DOS.RETURN_ERROR)
                        {
                            if (fallbackStatus == -1)
                                error = (int)DOS.Error.ObjectNotFound;
                            else
                                result = fallbackStatus;
                        }
                        if (error != 0) break;
                        count++;
                        continue;
                    }
                }

                var residentName = APTR.FromPointer(APTR.ReadUInt32(module,
                    ExecLayout.Resident.Name));
                var residentId = APTR.FromPointer(APTR.ReadUInt32(module,
                    ExecLayout.Resident.IdString));
                var hasTail = TryFindResidentVersionTail(residentId,
                    out var tail);
                var nameLength = residentName.IsNull ? 0u :
                    CStringLength(residentName, FileBufferBytes - 1u);
                var tailLength = hasTail
                    ? FindLineEnd(tail.Raw) - tail.Raw : 0u;
                var residentTextBytes = nameLength + tailLength + 1u;
                var residentText = APTR.Null;
                var residentScratch = APTR.Null;
                var residentParseError = residentTextBytes > FileBufferBytes
                    ? (int)DOS.Error.LineTooLong : 0;
                var parsed = default(FileVersion);

                if (residentParseError == 0)
                {
                    residentText = Exec.AllocMem(residentTextBytes,
                        Exec.MemoryFlags.Any);
                    if (residentText.IsNull)
                        residentParseError = (int)DOS.Error.NoFreeStore;
                }
                if (residentParseError == 0)
                {
                    residentScratch = Exec.AllocMem(ScratchBufferBytes,
                        Exec.MemoryFlags.Any);
                    if (residentScratch.IsNull)
                        residentParseError = (int)DOS.Error.NoFreeStore;
                }
                if (residentParseError == 0)
                {
                    for (var index = 0u; index < nameLength; index++)
                        APTR.WriteUInt8(residentText,
                            unchecked((int)index),
                            APTR.ReadUInt8(residentName,
                                unchecked((int)index)));
                    for (var index = 0u; index < tailLength; index++)
                        APTR.WriteUInt8(residentText,
                            unchecked((int)(nameLength + index)),
                            APTR.ReadUInt8(tail,
                                unchecked((int)index)));
                    APTR.WriteUInt8(residentText,
                        unchecked((int)(nameLength + tailLength)), 0);
                    ParseFileVersion(residentText, residentScratch,
                        out parsed);
                }
                else
                {
                    DOS.PrintFault((DOS.Error)residentParseError,
                        CString.FromPointer(0));
                    if (residentScratch.IsNotNull)
                    {
                        Exec.FreeMem(residentScratch,
                            ScratchBufferBytes);
                        residentScratch = APTR.Null;
                    }
                }

                if (md5 != 0) PrintNoMd5Available();
                int version;
                int revision;
                if (residentParseError == 0)
                {
                    PrintFileVersion(parsed, full, md5);
                    version = parsed.Version;
                    revision = parsed.Revision;
                }
                else
                {
                    version = APTR.ReadUInt8(module,
                        ExecLayout.Resident.Version);
                    var residentFlags = APTR.ReadUInt8(module,
                        ExecLayout.Resident.Flags);
                    revision = (residentFlags & ExtendedResidentFlag) != 0
                        ? APTR.ReadUInt16(module, (int)Resident.Size) : -1;
                    var fallback = default(Fields);
                    fallback.Name = residentName.Raw;
                    fallback.Version = unchecked((uint)version);
                    fallback.Revision = unchecked((uint)revision);
                    DOS.VPrintf("%s %ld.%ld\n",
                        Fields.AddressOf(ref fallback));
                }

                if (residentScratch.IsNotNull)
                    Exec.FreeMem(residentScratch, ScratchBufferBytes);
                if (residentText.IsNotNull)
                    Exec.FreeMem(residentText, residentTextBytes);

                if (singleName)
                    result = CompareRequested(unchecked((uint)version),
                        unchecked((uint)revision),
                        requestedVersion, hasVersion, requestedRevision,
                        hasRevision);

                count++;
            }

            if (utilityBase.IsNotNull)
                Exec.CloseLibrary(utilityBase);

            if (error != 0) break;
            if (count == MaxNames && APTR.ReadUInt32(cursor,
                    unchecked((int)(count * 4))) != 0)
            {
                error = (int)DOS.Error.LineTooLong;
            }
        }
        while (false);

        arguments.Release();
        if (error != 0)
        {
            ioError = error;
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            result = DOS.RETURN_FAIL;
        }
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static int RunFileNames(APTR names, uint md5, uint full,
        int requestedVersion, bool hasVersion, int requestedRevision,
        bool hasRevision, out int ioError)
    {
        ioError = 0;
        var cursor = names;
        var singleName = APTR.ReadUInt32(cursor, 4) == 0;
        var result = DOS.RETURN_OK;
        var count = 0u;

        while (count < MaxNames)
        {
            var name = APTR.ReadUInt32(cursor,
                unchecked((int)(count * 4)));
            if (name == 0) break;

            var fileResult = RunOneFile(APTR.FromPointer(name), md5, full,
                singleName, requestedVersion, hasVersion,
                requestedRevision, hasRevision, out var fileError);
            result = fileResult;
            if (fileError != 0) ioError = fileError;
            count++;
        }

        if (count == MaxNames && APTR.ReadUInt32(cursor,
                unchecked((int)(count * 4))) != 0)
        {
            ioError = (int)DOS.Error.LineTooLong;
            DOS.PrintFault(DOS.Error.LineTooLong, CString.FromPointer(0));
            result = DOS.RETURN_FAIL;
        }

        return result;
    }

    private static int RunOneFile(APTR name, uint md5, uint full,
        bool singleName,
        int requestedVersion, bool hasVersion, int requestedRevision,
        bool hasRevision, out int ioError)
    {
        return RunOneFile(name, md5, full, singleName, requestedVersion,
            hasVersion, requestedRevision, hasRevision, out ioError,
            false, true, out _, out _, out _, out _);
    }

    private static int RunOneFile(APTR name, uint md5, uint full,
        bool singleName,
        int requestedVersion, bool hasVersion, int requestedRevision,
        bool hasRevision, out int ioError, bool quietNotFound,
        bool compareRequested, out bool versionFound,
        out FileVersion discoveredVersion, out bool digestAvailable,
        out DigestFields digest)
    {
        ioError = 0;
        versionFound = false;
        discoveredVersion = default;
        digestAvailable = false;
        digest = default;
        var file = DOS.OpenRaw(CString.FromPointer(name),
            DOS.FileMode.OldFile);
        if (file.IsNull)
        {
            var openError = (int)DOS.IoErr();
            if (openError == (int)DOS.Error.ObjectWrongType ||
                openError == (int)DOS.Error.ObjectNotFound)
                openError = (int)DOS.Error.ObjectNotFound;
            if (quietNotFound &&
                openError == (int)DOS.Error.ObjectNotFound)
                return -1;
            DOS.PrintFault((DOS.Error)openError, CString.FromPointer(0));
            ioError = openError;
            return DOS.RETURN_FAIL;
        }

        var lease = new FileLease { File = file };
        var result = DOS.RETURN_FAIL;
        var error = 0;
        var found = false;
        var version = default(FileVersion);

        lease.Buffer = Exec.AllocMem(FileBufferBytes,
            Exec.MemoryFlags.Public);
        if (lease.Buffer.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            goto cleanup;
        }

        lease.Scratch = Exec.AllocMem(ScratchBufferBytes,
            Exec.MemoryFlags.Public);
        if (lease.Scratch.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            goto cleanup;
        }

        if (md5 != 0)
        {
            lease.Md5Context = Exec.AllocMem(Md5ContextBytes,
                Exec.MemoryFlags.Public);
            if (lease.Md5Context.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                goto cleanup;
            }
            InitializeMd5(lease.Md5Context);
        }

        error = ScanForVersionTag(ref lease, md5 != 0, out found,
            out var payloadLength);
        if (error != 0)
        {
            result = DOS.RETURN_FAIL;
            goto cleanup;
        }
        if (md5 != 0)
        {
            digest = ReadMd5Digest(lease.Md5Context);
            digestAvailable = true;
        }

        if (!found)
        {
            result = DOS.RETURN_ERROR;
            var fileName = CString.FromPointer(name);
            if (DOS.IsFileSystem(fileName) != 0)
            {
                DOS.Close(lease.File);
                lease.File = BPTR.Null;

                var loadedSegment = BPTR.FromRaw(
                    NativeMorphOSVersionDosRaw.LoadSeg(fileName));
                if (loadedSegment.IsNotNull)
                {
                    result = CreateLoadedResidentVersion(loadedSegment,
                        lease.Scratch, out version, out var residentText,
                        out var residentTextBytes);
                    if (result == DOS.RETURN_OK)
                    {
                        versionFound = true;
                        discoveredVersion = version;
                        if (md5 != 0) PrintMd5Digest(lease.Md5Context);
                        PrintFileVersion(version, full, md5);
                        result = compareRequested && singleName
                            ? CompareRequested(
                                unchecked((uint)version.Version),
                                unchecked((uint)version.Revision),
                                requestedVersion, hasVersion,
                                requestedRevision, hasRevision)
                            : DOS.RETURN_OK;
                    }
                    if (residentText.IsNotNull)
                        Exec.FreeMem(residentText, residentTextBytes);
                    NativeMorphOSVersionDosRaw.UnLoadSeg(loadedSegment);
                }
            }

            if (md5 == 0 && result != DOS.RETURN_OK &&
                result != DOS.RETURN_WARN)
            {
                var diagnostic = default(OneStringFields);
                diagnostic.Value = name.Raw;
                DOS.VPrintf("Could not find version information for '%s'\n",
                    OneStringFields.AddressOf(ref diagnostic));
            }
            goto cleanup;
        }

        APTR.WriteUInt8(lease.Buffer, unchecked((int)payloadLength), 0);
        if (!ParseFileVersion(lease.Buffer, lease.Scratch,
                out version))
        {
            error = (int)DOS.Error.NoFreeStore;
            result = DOS.RETURN_FAIL;
            goto cleanup;
        }

        versionFound = true;
        discoveredVersion = version;
        if (md5 != 0) PrintMd5Digest(lease.Md5Context);
        PrintFileVersion(version, full, md5);
        result = compareRequested && singleName
            ? CompareRequested(unchecked((uint)version.Version),
                unchecked((uint)version.Revision), requestedVersion,
                hasVersion, requestedRevision, hasRevision)
            : DOS.RETURN_OK;

    cleanup:
        ReleaseFileLease(ref lease);
        if (error != 0)
        {
            ioError = error;
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
        }
        return result;
    }

    private static int RunMorphOSDirectoryFileCandidates(APTR filePart,
        uint md5, uint full, int requestedVersion, bool hasVersion,
        int requestedRevision, bool hasRevision,
        ref bool fallbackDigestAvailable, ref DigestFields fallbackDigest,
        bool deviceDirectory,
        out bool versionFound, out FileVersion discoveredVersion,
        out int ioError)
    {
        versionFound = false;
        discoveredVersion = default;
        ioError = 0;
        var nameBytes = CStringLengthUnbounded(filePart);
        if (nameBytes == 0) return -1;
        if (nameBytes == uint.MaxValue || nameBytes > uint.MaxValue - 17u)
        {
            ioError = (int)DOS.Error.LineTooLong;
            DOS.PrintFault(DOS.Error.LineTooLong,
                CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var path = Exec.AllocVec(12u + nameBytes + 5u,
            (uint)Exec.MemoryFlags.Public);
        if (path.IsNull) return -1;

        var hasElfSuffix = nameBytes > 4u &&
            APTR.ReadUInt8(APTR.FromPointer(filePart.Raw + nameBytes - 4u),
                0) == (uint)'.' &&
            APTR.ReadUInt8(APTR.FromPointer(filePart.Raw + nameBytes - 3u),
                0) == (uint)'e' &&
            APTR.ReadUInt8(APTR.FromPointer(filePart.Raw + nameBytes - 2u),
                0) == (uint)'l' &&
            APTR.ReadUInt8(APTR.FromPointer(filePart.Raw + nameBytes - 1u),
                0) == (uint)'f';
        var status = -1;
        for (var candidate = 0; candidate < 4; candidate++)
        {
            var systemPath = candidate < 2;
            var bareName = (candidate & 1) != 0;
            var copiedNameBytes = bareName && hasElfSuffix
                ? nameBytes - 4u : nameBytes;
            var appendElf = !bareName && !hasElfSuffix;
            var prefix = 0u;
            var prefixBytes = 0u;
            if (deviceDirectory && systemPath)
            {
                prefix = CString.ToUInt32("MOSSYS:DEVS/");
                prefixBytes = 12u;
            }
            else if (deviceDirectory)
            {
                prefix = CString.ToUInt32("DEVS:");
                prefixBytes = 5u;
            }
            else if (systemPath)
            {
                prefix = CString.ToUInt32("MOSSYS:LIBS/");
                prefixBytes = 12u;
            }
            else
            {
                prefix = CString.ToUInt32("LIBS:");
                prefixBytes = 5u;
            }
            WriteFileCandidatePath(path, APTR.FromPointer(prefix),
                prefixBytes, filePart, copiedNameBytes, appendElf);

            var candidateStatus = RunOneFile(path, md5, full, false,
                requestedVersion, hasVersion, requestedRevision,
                hasRevision, out var candidateIoError, true, false,
                out var candidateFound, out var candidateVersion,
                out var candidateDigestAvailable,
                out var candidateDigest);
            status = candidateStatus;
            ioError = candidateIoError;
            if (candidateDigestAvailable)
            {
                fallbackDigestAvailable = true;
                fallbackDigest = candidateDigest;
            }
            if (!candidateFound) continue;

            versionFound = true;
            discoveredVersion = candidateVersion;
            break;
        }

        Exec.FreeVec(path);
        return status;
    }

    private static void WriteFileCandidatePath(APTR destination,
        APTR prefix, uint prefixBytes, APTR filePart, uint filePartBytes,
        bool appendElf)
    {
        var cursor = 0u;
        for (var index = 0u; index < prefixBytes; index++)
        {
            APTR.WriteUInt8(destination, unchecked((int)cursor),
                APTR.ReadUInt8(prefix, unchecked((int)index)));
            cursor++;
        }
        for (var index = 0u; index < filePartBytes; index++)
        {
            APTR.WriteUInt8(destination, unchecked((int)cursor),
                APTR.ReadUInt8(filePart, unchecked((int)index)));
            cursor++;
        }
        if (appendElf)
        {
            var suffix = APTR.FromPointer(CString.ToUInt32(".elf"));
            for (var index = 0u; index < 4u; index++)
            {
                APTR.WriteUInt8(destination, unchecked((int)cursor),
                    APTR.ReadUInt8(suffix, unchecked((int)index)));
                cursor++;
            }
        }
        APTR.WriteUInt8(destination, unchecked((int)cursor), 0);
    }

    private static int ScanForVersionTag(ref FileLease lease, bool md5,
        out bool found, out uint payloadLength)
    {
        found = false;
        payloadLength = 0;
        var buffered = 0u;
        var seekEndHack = !md5;

        for (;;)
        {
            if ((Exec.SetSignal(0u, CtrlCMask) & CtrlCMask) != 0)
            {
                DOS.SetIoErr(DOS.Error.Break);
                return (int)DOS.Error.Break;
            }

            var room = FileBufferBytes - buffered;
            var read = DOS.Read(lease.File,
                APTR.FromPointer(lease.Scratch.Raw + buffered),
                unchecked((int)room));
            if (read < 0) return (int)DOS.IoErr();
            if ((uint)read > room) return (int)DOS.Error.LineTooLong;
            if (read == 0)
            {
                if (md5) FinalizeMd5(lease.Md5Context);
                return 0;
            }

            if (md5)
                UpdateMd5(lease.Md5Context,
                    APTR.FromPointer(lease.Scratch.Raw + buffered),
                    unchecked((uint)read));

            buffered += unchecked((uint)read);
            if (seekEndHack)
            {
                seekEndHack = false;
                if (buffered == FileBufferBytes &&
                    APTR.ReadUInt8(lease.Scratch, 0) == 0x7f &&
                    APTR.ReadUInt8(lease.Scratch, 1) == (uint)'M' &&
                    APTR.ReadUInt8(lease.Scratch, 2) == (uint)'O' &&
                    APTR.ReadUInt8(lease.Scratch, 3) == (uint)'S' &&
                    APTR.ReadUInt8(lease.Scratch, 4) == 0 &&
                    APTR.ReadUInt8(lease.Scratch, 5) == 0)
                {
                    var seek = M68kRuntime.SplitInt64(DOS.Seek64(
                        lease.File, -unchecked((long)FileBufferBytes),
                        (int)DosConstants.OffsetEnd), out var seekHigh);
                    if (seekHigh == uint.MaxValue && seek == uint.MaxValue)
                        return (int)DOS.IoErr();
                    buffered = 0;
                    continue;
                }
            }

            for (var offset = 0u; offset + 5u <= buffered; offset++)
            {
                if (APTR.ReadUInt8(lease.Scratch,
                        unchecked((int)offset)) != (uint)'$' ||
                    APTR.ReadUInt8(lease.Scratch,
                        unchecked((int)(offset + 1))) != (uint)'V' ||
                    APTR.ReadUInt8(lease.Scratch,
                        unchecked((int)(offset + 2))) != (uint)'E' ||
                    APTR.ReadUInt8(lease.Scratch,
                        unchecked((int)(offset + 3))) != (uint)'R' ||
                    APTR.ReadUInt8(lease.Scratch,
                        unchecked((int)(offset + 4))) != (uint)':')
                    continue;

                var remaining = buffered - offset - 5u;
                for (var index = 0u; index < remaining; index++)
                    APTR.WriteUInt8(lease.Buffer,
                        unchecked((int)index),
                        APTR.ReadUInt8(lease.Scratch,
                            unchecked((int)(offset + 5u + index))));

                if ((Exec.SetSignal(0u, CtrlCMask) & CtrlCMask) != 0)
                {
                    DOS.SetIoErr(DOS.Error.Break);
                    return (int)DOS.Error.Break;
                }

                var tailRoom = FilePayloadBytes - remaining;
                var tailRead = DOS.Read(lease.File,
                    APTR.FromPointer(lease.Buffer.Raw + remaining),
                    unchecked((int)tailRoom));
                if (tailRead < 0) return (int)DOS.IoErr();
                if ((uint)tailRead > tailRoom)
                    return (int)DOS.Error.LineTooLong;

                if (md5 && tailRead > 0)
                    UpdateMd5(lease.Md5Context,
                        APTR.FromPointer(lease.Buffer.Raw + remaining),
                        unchecked((uint)tailRead));

                payloadLength = remaining + unchecked((uint)tailRead);
                found = true;
                return md5 ? DrainAndFinalizeMd5(ref lease) : 0;
            }

            // Keep the four bytes that may be the start of a tag split over
            // the next DOS Read.  The source keeps five; retaining five is
            // harmless and follows its exact overlap window.
            var carry = 5u;
            if (buffered < carry) carry = buffered;
            var carryStart = buffered - carry;
            for (var index = 0u; index < carry; index++)
                APTR.WriteUInt8(lease.Scratch,
                    unchecked((int)index),
                    APTR.ReadUInt8(lease.Scratch,
                        unchecked((int)(carryStart + index))));
            buffered = carry;
        }
    }

    private static bool ParseFileVersion(APTR buffer, APTR scratch,
        out FileVersion parsed)
    {
        parsed = default;
        var start = SkipWhitespace(buffer.Raw);
        var lineEnd = FindLineEnd(start);
        var position = start;
        var matched = false;
        var number = default(NumberCell);

        while (position < lineEnd)
        {
            var value = APTR.ReadUInt8(APTR.FromPointer(position), 0);
            if (value == (uint)' ' || value == (uint)'\t')
            {
                var candidate = position + 1;
                if (TryParseLong(candidate, ref number, out var majorEnd,
                        out var major))
                {
                    var afterMajor = SkipSpaces(majorEnd);
                    var revisionEnd = afterMajor;
                    var revisionText = 0u;
                    var revision = -1;
                    if (APTR.ReadUInt8(APTR.FromPointer(afterMajor), 0) ==
                        (uint)'.')
                    {
                        if (TryParseLong(afterMajor + 1, ref number,
                                out var parsedRevisionEnd,
                                out var parsedRevision))
                        {
                            revisionText = majorEnd;
                            revisionEnd = parsedRevisionEnd;
                            revision = parsedRevision;
                        }
                        else
                        {
                            revisionEnd = afterMajor;
                        }
                    }

                    parsed.Name = APTR.FromPointer(start);
                    var nameEnd = position;
                    while (nameEnd > start && IsWhitespace(
                            APTR.ReadUInt8(APTR.FromPointer(nameEnd - 1), 0)))
                        nameEnd--;
                    APTR.WriteUInt8(APTR.FromPointer(nameEnd), 0, 0);

                    parsed.VersionText = APTR.FromPointer(candidate);
                    var scratchCursor = 0u;
                    if (revisionText != 0)
                    {
                        var revisionLength = revisionEnd - revisionText;
                        for (var index = 0u; index < revisionLength; index++)
                            APTR.WriteUInt8(scratch,
                                unchecked((int)index),
                                APTR.ReadUInt8(
                                    APTR.FromPointer(revisionText),
                                    unchecked((int)index)));
                        APTR.WriteUInt8(scratch,
                            unchecked((int)revisionLength), 0);
                        parsed.RevisionText = scratch;
                        scratchCursor = revisionLength + 1u;
                    }
                    APTR.WriteUInt8(APTR.FromPointer(majorEnd), 0, 0);
                    parsed.Version = major;
                    parsed.Revision = revision;

                    var afterVersion = revisionText != 0
                        ? revisionEnd
                        : APTR.ReadUInt8(APTR.FromPointer(afterMajor), 0) ==
                            (uint)'.' ? afterMajor : afterMajor;
                    if (APTR.ReadUInt8(APTR.FromPointer(afterMajor), 0) ==
                        (uint)'.' && revisionText == 0)
                        afterVersion = afterMajor;

                    parsed.Padding = APTR.FromPointer(afterVersion);
                    var dateMarker = FindDateOrLineEnd(afterVersion,
                        lineEnd);
                    var hadDateMarker = dateMarker < lineEnd &&
                        APTR.ReadUInt8(APTR.FromPointer(dateMarker), 0) ==
                            (uint)'(';
                    var formattedDate = APTR.Null;
                    var afterDate = afterVersion;
                    var dateParsed = hadDateMarker && TryParseAndFormatDate(
                        APTR.FromPointer(scratch.Raw + scratchCursor),
                        ScratchBufferBytes - scratchCursor,
                        afterVersion, dateMarker, lineEnd,
                        out formattedDate, out afterDate);
                    var preservedExtra = APTR.Null;
                    if (hadDateMarker && !dateParsed)
                    {
                        var extraStart = SkipSpaces(afterVersion);
                        var extraEnd = FindLineEnd(extraStart);
                        if (extraEnd > extraStart)
                        {
                            var extraLength = extraEnd - extraStart;
                            if (scratchCursor + extraLength + 1u <=
                                ScratchBufferBytes)
                            {
                                preservedExtra = APTR.FromPointer(
                                    scratch.Raw + scratchCursor);
                                for (var index = 0u; index < extraLength;
                                    index++)
                                    APTR.WriteUInt8(preservedExtra,
                                        unchecked((int)index),
                                        APTR.ReadUInt8(
                                            APTR.FromPointer(extraStart),
                                            unchecked((int)index)));
                                APTR.WriteUInt8(preservedExtra,
                                    unchecked((int)extraLength), 0);
                            }
                        }
                    }
                    if (dateParsed)
                    {
                        parsed.Date = formattedDate;
                        var padEnd = dateMarker;
                        if (padEnd > afterVersion && APTR.ReadUInt8(
                                APTR.FromPointer(padEnd - 1), 0) ==
                            (uint)' ')
                            padEnd--;
                        APTR.WriteUInt8(APTR.FromPointer(padEnd), 0, 0);
                        position = afterDate;
                    }
                    else
                    {
                        var padEnd = hadDateMarker ? dateMarker : lineEnd;
                        if (hadDateMarker && padEnd > afterVersion &&
                            APTR.ReadUInt8(APTR.FromPointer(padEnd - 1), 0) ==
                                (uint)' ')
                            padEnd--;
                        APTR.WriteUInt8(APTR.FromPointer(padEnd), 0, 0);
                        position = afterVersion;
                    }

                    if (hadDateMarker)
                    {
                        if (preservedExtra.IsNotNull)
                        {
                            parsed.Extra = preservedExtra;
                            parsed.ExtraLineFeed = APTR.FromPointer(
                                CString.ToUInt32("\n"));
                        }
                        else if (dateParsed)
                        {
                            position = SkipSpaces(position);
                            var extraEnd = FindLineEnd(position);
                            if (extraEnd > position)
                            {
                                parsed.Extra = APTR.FromPointer(position);
                                APTR.WriteUInt8(
                                    APTR.FromPointer(extraEnd), 0, 0);
                                parsed.ExtraLineFeed = APTR.FromPointer(
                                    CString.ToUInt32("\n"));
                            }
                        }
                    }

                    matched = true;
                    break;
                }
            }
            position++;
        }

        if (!matched)
        {
            var end = lineEnd;
            while (end > start && IsWhitespace(
                    APTR.ReadUInt8(APTR.FromPointer(end - 1), 0)))
                end--;
            APTR.WriteUInt8(APTR.FromPointer(end), 0, 0);
            parsed.Name = APTR.FromPointer(start);
            parsed.Version = 0;
            parsed.Revision = 0;
        }
        return true;
    }

    private static bool TryParseLong(uint text, ref NumberCell cell,
        out uint end, out int value)
    {
        end = text;
        value = 0;
        var parsed = DOS.StrToLong(CString.FromPointer(
            APTR.FromPointer(text)), NumberCell.AddressOf(ref cell));
        if (parsed <= 0) return false;
        end = text + unchecked((uint)parsed);
        value = unchecked((int)cell.Value);
        return true;
    }

    private static bool TryParseAndFormatDate(APTR output,
        uint outputCapacity, uint headerStart, uint marker, uint lineEnd,
        out APTR formatted,
        out uint afterDate)
    {
        formatted = APTR.Null;
        afterDate = headerStart;
        var inner = marker + 1;
        var close = inner;
        while (close < lineEnd)
        {
            var c = APTR.ReadUInt8(APTR.FromPointer(close), 0);
            if (c == (uint)')' || c == (uint)'\r' || c == (uint)'\n')
                break;
            close++;
        }
        if (close >= lineEnd || APTR.ReadUInt8(
                APTR.FromPointer(close), 0) != (uint)')')
            return false;

        var dateEnd = close;
        var prefixLength = inner - headerStart;
        var dateLength = close - inner;
        if (prefixLength + dateLength + 32u >= outputCapacity)
            return false;
        for (var index = 0u; index < prefixLength; index++)
            APTR.WriteUInt8(output, unchecked((int)index),
                APTR.ReadUInt8(APTR.FromPointer(headerStart),
                    unchecked((int)index)));
        var dateText = APTR.FromPointer(output.Raw + prefixLength);
        for (var index = 0u; index < dateLength; index++)
            APTR.WriteUInt8(dateText, unchecked((int)index),
                APTR.ReadUInt8(APTR.FromPointer(inner),
                    unchecked((int)index)));
        APTR.WriteUInt8(dateText, unchecked((int)dateLength), 0);

        for (var index = 0u; index < dateLength; index++)
        {
            var c = APTR.ReadUInt8(dateText, unchecked((int)index));
            if (c == (uint)'.' || c == (uint)'/')
                APTR.WriteUInt8(dateText, unchecked((int)index),
                    (byte)'-');
            else if (!IsAsciiAlphaNumeric(c))
            {
                dateEnd = inner + index;
                APTR.WriteUInt8(dateText, unchecked((int)index), 0);
                break;
            }
        }

        var cells = default(DateCells);
        var dateTime = DateCells.AddressOf(ref cells);
        APTR.WriteUInt8(dateTime,
            unchecked((int)DosLayout.DateTime.Format), 3);
        APTR.WriteUInt8(dateTime,
            unchecked((int)DosLayout.DateTime.Flags), 0);
        APTR.WriteUInt32(dateTime,
            unchecked((int)DosLayout.DateTime.Day), 0);
        APTR.WriteUInt32(dateTime,
            unchecked((int)DosLayout.DateTime.Date), dateText.Raw);
        APTR.WriteUInt32(dateTime,
            unchecked((int)DosLayout.DateTime.Time), 0);
        if (DOS.StrToDate(dateTime) == 0)
        {
            APTR.WriteUInt8(dateTime,
                unchecked((int)DosLayout.DateTime.Format), 0);
            if (DOS.StrToDate(dateTime) == 0) return false;
        }

        var formattedText = dateText;
        APTR.WriteUInt32(dateTime,
            unchecked((int)DosLayout.DateTime.Stamp + 4), 0);
        APTR.WriteUInt32(dateTime,
            unchecked((int)DosLayout.DateTime.Stamp + 8), 0);
        APTR.WriteUInt8(dateTime,
            unchecked((int)DosLayout.DateTime.Format), 4);
        APTR.WriteUInt32(dateTime,
            unchecked((int)DosLayout.DateTime.Date),
            formattedText.Raw);
        if (DOS.DateToStr(dateTime) == 0) return false;

        var formattedCapacity = outputCapacity - prefixLength;
        var formattedLength = CStringLength(formattedText,
            formattedCapacity - 2u);
        if (formattedLength >= formattedCapacity - 1u)
            return false;
        APTR.WriteUInt8(output,
            unchecked((int)(prefixLength + formattedLength)), (byte)')');
        APTR.WriteUInt8(output,
            unchecked((int)(prefixLength + formattedLength + 1u)), 0);
        formatted = output;
        afterDate = dateEnd + 1u;
        return true;
    }

    private static void PrintNoMd5Available() =>
        DOS.PutStr("<no md5sum available>             ");

    private static int RunSystemVersion(uint md5, uint full,
        int requestedVersion, bool hasVersion, int requestedRevision,
        bool hasRevision, out int ioError)
    {
        ioError = 0;
        if (md5 != 0) PrintNoMd5Available();

        var execBase = APTR.FromPointer(APTR.ReadUInt32(
            APTR.FromPointer(4), 0));
        if (execBase.IsNull)
        {
            ioError = (int)DOS.Error.InvalidResidentLibrary;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var morphOsResident = APTR.FromPointer(Exec.FindResident(
            CString.FromPointer(CString.ToUInt32("MorphOS"))));
        var morphVersion = 0u;
        var morphRevision = 0u;
        var ambientStatus = -1;
        if (morphOsResident.IsNotNull)
        {
            var idString = APTR.FromPointer(APTR.ReadUInt32(morphOsResident,
                ExecLayout.Resident.IdString));
            if (!TryReadVersionAndRevision(idString, out morphVersion,
                    out morphRevision))
            {
                morphVersion = APTR.ReadUInt8(morphOsResident,
                    ExecLayout.Resident.Version);
                morphRevision = 0;
            }

            var morphFields = default(Fields);
            morphFields.Name = CString.ToUInt32("MorphOS");
            morphFields.Version = morphVersion;
            morphFields.Revision = morphRevision;
            DOS.VPrintf("%s %lu.%lu", Fields.AddressOf(ref morphFields));
            ambientStatus = TryReadAmbientVersion(out var ambientVersion,
                out var ambientRevision);
            if (ambientStatus == DOS.RETURN_OK)
                PrintAmbientVersion(ambientVersion, ambientRevision);
            else
                DOS.PutStr(", ");
        }

        var fields = default(Fields);
        var kickVersion = (uint)APTR.ReadUInt16(execBase,
            ExecLayout.Library.Version);
        var kickRevision = (uint)APTR.ReadUInt16(execBase,
            ExecLayout.ExecBase.SoftVer);
        fields.Name = CString.ToUInt32("Kickstart");
        fields.Version = kickVersion;
        fields.Revision = kickRevision;
        DOS.VPrintf("%s %lu.%lu", Fields.AddressOf(ref fields));

        var versionLibrary = Exec.OpenLibraryRaw(
            CString.FromPointer(CString.ToUInt32("version.library")), 0);
        if (versionLibrary.IsNull)
        {
            if (ambientStatus != -1)
            {
                DOS.PutStr("\n");
                return DOS.RETURN_WARN;
            }
            return -1;
        }

        if (versionLibrary.IsNotNull)
        {
            var libraryList = APTR.FromPointer(execBase.Raw +
                (uint)ExecLayout.ExecBase.LibraryList);
            Exec.Forbid();
            var versionNode = FindLibraryNode(libraryList,
                CString.ToUInt32("version.library"));
            if (versionNode.IsNull)
            {
                Exec.Permit();
                Exec.CloseLibrary(versionLibrary);
                if (ambientStatus != -1)
                {
                    DOS.PutStr("\n");
                    return DOS.RETURN_WARN;
                }
                return -1;
            }

            var workbenchVersion = (uint)APTR.ReadUInt16(versionNode,
                ExecLayout.Library.Version);
            var workbenchRevision = (uint)APTR.ReadUInt16(versionNode,
                ExecLayout.Library.Revision);
            var versionId = APTR.FromPointer(APTR.ReadUInt32(versionNode,
                ExecLayout.Library.IdString));
            var libraryName = APTR.FromPointer(APTR.ReadUInt32(versionNode,
                ExecLayout.Node.Name));
            var versionTail = APTR.Null;
            var hasVersionTail = TryFindLibraryVersionTail(versionId,
                out versionTail);
            var nameLength = libraryName.IsNull ? 0u :
                CStringLength(libraryName, FileBufferBytes - 1u);
            var tailLength = hasVersionTail
                ? FindLineEnd(versionTail.Raw) - versionTail.Raw : 0u;
            var versionTextBytes = nameLength + tailLength + 1u;
            var systemVersionText = APTR.Null;
            var systemVersionScratch = APTR.Null;
            var systemParseError = versionTextBytes > FileBufferBytes
                ? (int)DOS.Error.LineTooLong : 0;
            var workbenchParsed = default(FileVersion);

            if (systemParseError == 0)
            {
                systemVersionText = Exec.AllocMem(versionTextBytes,
                    Exec.MemoryFlags.Public);
                if (systemVersionText.IsNull)
                    systemParseError = (int)DOS.Error.NoFreeStore;
            }
            if (systemParseError == 0)
            {
                systemVersionScratch = Exec.AllocMem(ScratchBufferBytes,
                    Exec.MemoryFlags.Public);
                if (systemVersionScratch.IsNull)
                    systemParseError = (int)DOS.Error.NoFreeStore;
            }
            if (systemParseError == 0)
            {
                for (var index = 0u; index < nameLength; index++)
                    APTR.WriteUInt8(systemVersionText,
                        unchecked((int)index), APTR.ReadUInt8(libraryName,
                            unchecked((int)index)));
                for (var index = 0u; index < tailLength; index++)
                    APTR.WriteUInt8(systemVersionText,
                        unchecked((int)(nameLength + index)),
                        APTR.ReadUInt8(versionTail,
                            unchecked((int)index)));
                APTR.WriteUInt8(systemVersionText,
                    unchecked((int)(nameLength + tailLength)), 0);
                ParseFileVersion(systemVersionText, systemVersionScratch,
                    out workbenchParsed);
            }
            Exec.Permit();
            Exec.CloseLibrary(versionLibrary);

            if (systemParseError != 0)
            {
                if (systemVersionScratch.IsNotNull)
                    Exec.FreeMem(systemVersionScratch, ScratchBufferBytes);
                if (systemVersionText.IsNotNull)
                    Exec.FreeMem(systemVersionText, versionTextBytes);
                if (ambientStatus != -1)
                {
                    DOS.PutStr("\n");
                    return DOS.RETURN_WARN;
                }
                ioError = systemParseError;
                DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
                return DOS.RETURN_FAIL;
            }

            if (ambientStatus != -1)
            {
                Exec.FreeMem(systemVersionScratch, ScratchBufferBytes);
                Exec.FreeMem(systemVersionText, versionTextBytes);
                DOS.PutStr("\n");
                return DOS.RETURN_WARN;
            }

            DOS.PutStr(", ");
            if (workbenchParsed.VersionText.IsNotNull &&
                workbenchParsed.Version == unchecked((int)workbenchVersion) &&
                workbenchParsed.Revision == unchecked((int)workbenchRevision))
            {
                workbenchParsed.Name = APTR.FromPointer(
                    CString.ToUInt32("Workbench"));
                PrintFileVersion(workbenchParsed, full, md5);
            }
            else
            {
                fields.Name = CString.ToUInt32("Workbench");
                fields.Version = workbenchVersion;
                fields.Revision = workbenchRevision;
                DOS.VPrintf("%s %lu.%lu\n",
                    Fields.AddressOf(ref fields));
            }
            Exec.FreeMem(systemVersionScratch, ScratchBufferBytes);
            Exec.FreeMem(systemVersionText, versionTextBytes);

            return CompareRequested(workbenchVersion, workbenchRevision,
                requestedVersion, hasVersion, requestedRevision,
                hasRevision);
        }

        // When makewbversion() fails, the source retains makekickversion()'s
        // parsed fields and the command prints those after its already-emitted
        // Kickstart prefix.
        fields.Name = CString.ToUInt32("Kickstart");
        fields.Version = kickVersion;
        fields.Revision = kickRevision;
        DOS.VPrintf("%s %lu.%lu\n", Fields.AddressOf(ref fields));
        return CompareRequested(kickVersion, kickRevision,
            requestedVersion, hasVersion, requestedRevision, hasRevision);
    }

    private static int TryReadAmbientVersion(out uint version,
        out uint revision)
    {
        version = 0;
        revision = 0;
        var self = Exec.FindTask(CString.FromPointer(0));
        var oldWindowPointer = APTR.ReadUInt32(self,
            DosLayout.Process.WindowPointer);
        APTR.WriteUInt32(self, DosLayout.Process.WindowPointer,
            uint.MaxValue);

        var response = QueryAmbientVersion();
        var status = -1;
        if (response.IsNotNull)
        {
            var versionCell = default(NumberCell);
            var parsed = TryParseLong(response.Raw, ref versionCell,
                out var afterVersion, out var parsedVersion) &&
                APTR.ReadUInt8(APTR.FromPointer(afterVersion), 0) ==
                    (uint)'.';
            var parsedRevision = 0;
            if (parsed)
            {
                var revisionCell = default(NumberCell);
                parsed = TryParseLong(afterVersion + 1u,
                    ref revisionCell, out _, out parsedRevision);
            }

            Exec.FreeVec(response);
            if (parsed)
            {
                version = unchecked((uint)parsedVersion);
                revision = unchecked((uint)parsedRevision);
                status = DOS.RETURN_OK;
            }
        }

        if (status != DOS.RETURN_OK)
        {
            var pathCells = default(AmbientPathCells);
            var path = AmbientPathCells.AddressOf(ref pathCells);
            for (var index = 0u; index < AmbientPathBytes; index++)
                APTR.WriteUInt8(path, unchecked((int)index), 0);

            var pathLength = DOS.GetVar(
                CString.FromPointer(CString.ToUInt32("ambient_path")),
                path, unchecked((int)AmbientPathBytes), 0);
            if (pathLength >= 0)
                status = ReadAmbientFile(CString.FromPointer(path.Raw),
                    out version, out revision);

            if (status != DOS.RETURN_OK)
                status = ReadAmbientFile(CString.FromPointer(
                    CString.ToUInt32("mossys:ambient/ambient")),
                    out version, out revision);
            if (status != DOS.RETURN_OK)
                status = ReadAmbientFile(CString.FromPointer(
                    CString.ToUInt32("sys:system/ambient/ambient")),
                    out version, out revision);
        }

        if (status == DOS.RETURN_OK)
            SetAmbientVersion(version, revision);
        else
        {
            version = 0;
            revision = 0;
        }

        APTR.WriteUInt32(self, DosLayout.Process.WindowPointer,
            oldWindowPointer);
        return status;
    }

    private static int ReadAmbientFile(CString name, out uint version,
        out uint revision)
    {
        version = 0;
        revision = 0;
        var file = DOS.OpenRaw(name, DOS.FileMode.OldFile);
        if (file.IsNull)
        {
            var openError = (int)DOS.IoErr();
            if (openError == (int)DOS.Error.ObjectNotFound ||
                openError == (int)DOS.Error.ObjectWrongType)
                return -1;
            DOS.PrintFault((DOS.Error)openError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var lease = new FileLease { File = file };
        var status = DOS.RETURN_FAIL;
        var error = 0;
        var found = false;
        var parsed = default(FileVersion);
        lease.Buffer = Exec.AllocMem(FileBufferBytes,
            Exec.MemoryFlags.Public);
        if (lease.Buffer.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            goto cleanup;
        }

        lease.Scratch = Exec.AllocMem(ScratchBufferBytes,
            Exec.MemoryFlags.Public);
        if (lease.Scratch.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            goto cleanup;
        }

        error = ScanForVersionTag(ref lease, false, out found,
            out var payloadLength);
        if (error != 0)
            goto cleanup;

        if (!found)
        {
            status = DOS.RETURN_ERROR;
            if (DOS.IsFileSystem(name) != 0)
            {
                DOS.Close(lease.File);
                lease.File = BPTR.Null;

                var loadedSegment = BPTR.FromRaw(
                    NativeMorphOSVersionDosRaw.LoadSeg(name));
                if (loadedSegment.IsNotNull)
                {
                    status = ReadLoadedResidentVersion(
                        loadedSegment, lease.Scratch, out version,
                        out revision);
                    NativeMorphOSVersionDosRaw.UnLoadSeg(loadedSegment);
                }
            }

            if (status != DOS.RETURN_OK)
            {
                var diagnostic = default(OneStringFields);
                diagnostic.Value = CString.ToUInt32(name);
                DOS.VPrintf("Could not find version information for '%s'\n",
                    OneStringFields.AddressOf(ref diagnostic));
            }
            goto cleanup;
        }

        APTR.WriteUInt8(lease.Buffer, unchecked((int)payloadLength), 0);
        if (!ParseFileVersion(lease.Buffer, lease.Scratch, out parsed))
        {
            error = (int)DOS.Error.NoFreeStore;
            goto cleanup;
        }

        version = unchecked((uint)parsed.Version);
        revision = unchecked((uint)parsed.Revision);
        status = DOS.RETURN_OK;

    cleanup:
        ReleaseFileLease(ref lease);
        if (error != 0)
        {
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            status = DOS.RETURN_FAIL;
            version = 0;
            revision = 0;
        }
        return status;
    }

    private static int ReadLoadedResidentVersion(BPTR segment,
        APTR scratch, out uint version, out uint revision)
    {
        version = 0;
        revision = 0;
        var status = CreateLoadedResidentVersion(segment, scratch,
            out var parsed, out var residentText, out var residentTextBytes);
        if (residentText.IsNotNull)
            Exec.FreeMem(residentText, residentTextBytes);
        if (status != DOS.RETURN_OK) return status;
        version = unchecked((uint)parsed.Version);
        revision = unchecked((uint)parsed.Revision);
        return DOS.RETURN_OK;
    }

    private static bool TryCreateLibraryVersion(APTR library,
        uint version, uint revision, out FileVersion parsed,
        out APTR text, out uint textBytes, out APTR scratch)
    {
        parsed = default;
        text = APTR.Null;
        textBytes = 0;
        scratch = APTR.Null;
        var idString = APTR.FromPointer(APTR.ReadUInt32(library,
            ExecLayout.Library.IdString));
        if (!TryFindLibraryVersionTail(idString, out var tail))
            return false;

        var libraryName = APTR.FromPointer(APTR.ReadUInt32(library,
            ExecLayout.Node.Name));
        var nameLength = CStringLength(libraryName,
            FileBufferBytes - 1u);
        var tailLength = FindLineEnd(tail.Raw) - tail.Raw;
        textBytes = nameLength + tailLength + 1u;
        if (textBytes > FileBufferBytes) return false;

        text = Exec.AllocMem(textBytes, Exec.MemoryFlags.Public);
        if (text.IsNull) return false;
        scratch = Exec.AllocMem(ScratchBufferBytes,
            Exec.MemoryFlags.Public);
        if (scratch.IsNull) return false;

        for (var index = 0u; index < nameLength; index++)
            APTR.WriteUInt8(text, unchecked((int)index),
                APTR.ReadUInt8(libraryName, unchecked((int)index)));
        for (var index = 0u; index < tailLength; index++)
            APTR.WriteUInt8(text, unchecked((int)(nameLength + index)),
                APTR.ReadUInt8(tail, unchecked((int)index)));
        APTR.WriteUInt8(text, unchecked((int)(nameLength + tailLength)), 0);

        if (!ParseFileVersion(text, scratch, out parsed) ||
            parsed.Version != unchecked((int)version) ||
            parsed.Revision != unchecked((int)revision))
            return false;
        return true;
    }

    private static int CreateLoadedResidentVersion(BPTR segment,
        APTR scratch, out FileVersion parsed, out APTR residentText,
        out uint residentTextBytes)
    {
        parsed = default;
        residentText = APTR.Null;
        residentTextBytes = 0;
        var resident = FindLibResident(segment.Raw);
        if (resident.IsNull) return DOS.RETURN_ERROR;

        var residentName = APTR.FromPointer(APTR.ReadUInt32(resident,
            ExecLayout.Resident.Name));
        var residentId = APTR.FromPointer(APTR.ReadUInt32(resident,
            ExecLayout.Resident.IdString));
        var hasTail = TryFindResidentVersionTail(residentId, out var tail);
        var nameLength = residentName.IsNull ? 0u :
            CStringLengthUnbounded(residentName);
        var tailLength = hasTail
            ? FindLineEnd(tail.Raw) - tail.Raw : 0u;
        if (nameLength > uint.MaxValue - tailLength ||
            nameLength + tailLength == uint.MaxValue)
        {
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            DOS.PrintFault(DOS.Error.NoFreeStore,
                CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }
        residentTextBytes = nameLength + tailLength + 1u;

        residentText = Exec.AllocMem(residentTextBytes,
            Exec.MemoryFlags.Any);
        if (residentText.IsNull)
        {
            DOS.PrintFault(DOS.Error.NoFreeStore,
                CString.FromPointer(0));
            residentTextBytes = 0;
            return DOS.RETURN_FAIL;
        }

        for (var index = 0u; index < nameLength; index++)
            APTR.WriteUInt8(residentText, unchecked((int)index),
                APTR.ReadUInt8(residentName, unchecked((int)index)));
        for (var index = 0u; index < tailLength; index++)
            APTR.WriteUInt8(residentText,
                unchecked((int)(nameLength + index)),
                APTR.ReadUInt8(tail, unchecked((int)index)));
        APTR.WriteUInt8(residentText,
            unchecked((int)(nameLength + tailLength)), 0);

        ParseFileVersion(residentText, scratch, out parsed);
        return DOS.RETURN_OK;
    }

    private static int ReadCommandSegmentVersion(BPTR segment,
        APTR scratch, out FileVersion parsed, out APTR ownedText,
        out uint ownedTextBytes, out bool foundTag)
    {
        parsed = default;
        ownedText = APTR.Null;
        ownedTextBytes = 0;
        foundTag = false;
        var current = segment.Raw;
        while (current != 0)
        {
            var memory = APTR.FromPointer(current << 2);
            var segmentWords = APTR.ReadUInt32(memory, -4);
            if (segmentWords >= 3 &&
                segmentWords <= (uint.MaxValue - memory.Raw + 8u) / 4u)
            {
                var segmentEnd = memory.Raw + segmentWords * 4u - 8u;
                var cursor = memory.Raw + 4u;
                while (cursor + 5u <= segmentEnd)
                {
                    if (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) ==
                            (uint)'$' &&
                        APTR.ReadUInt8(APTR.FromPointer(cursor + 1), 0) ==
                            (uint)'V' &&
                        APTR.ReadUInt8(APTR.FromPointer(cursor + 2), 0) ==
                            (uint)'E' &&
                        APTR.ReadUInt8(APTR.FromPointer(cursor + 3), 0) ==
                            (uint)'R' &&
                        APTR.ReadUInt8(APTR.FromPointer(cursor + 4), 0) ==
                            (uint)':')
                    {
                        var textStart = cursor + 5u;
                        var textEnd = textStart;
                        while (textEnd < segmentEnd &&
                            APTR.ReadUInt8(APTR.FromPointer(textEnd), 0) != 0)
                            textEnd++;
                        if (textEnd > textStart)
                        {
                            foundTag = true;
                            var textLength = textEnd - textStart;
                            if (textLength == uint.MaxValue)
                            {
                                DOS.SetIoErr(DOS.Error.NoFreeStore);
                                return DOS.RETURN_FAIL;
                            }
                            ownedTextBytes = textLength + 1u;
                            ownedText = Exec.AllocMem(ownedTextBytes,
                                Exec.MemoryFlags.Any);
                            if (ownedText.IsNull)
                            {
                                ownedTextBytes = 0;
                                DOS.SetIoErr(DOS.Error.NoFreeStore);
                                return DOS.RETURN_FAIL;
                            }
                            for (var index = 0u; index < textLength; index++)
                                APTR.WriteUInt8(ownedText,
                                    unchecked((int)index),
                                    APTR.ReadUInt8(
                                        APTR.FromPointer(textStart),
                                        unchecked((int)index)));
                            APTR.WriteUInt8(ownedText,
                                unchecked((int)textLength), 0);
                            ParseFileVersion(ownedText, scratch, out parsed);
                            return parsed.VersionText.IsNotNull
                                ? DOS.RETURN_OK : DOS.RETURN_ERROR;
                        }
                    }
                    cursor++;
                }
            }
            current = APTR.ReadUInt32(memory, 0);
        }

        DOS.SetIoErr(DOS.Error.ObjectNotFound);
        return DOS.RETURN_ERROR;
    }

    private static APTR FindLibResident(uint segmentRaw)
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
                            ExecLayout.Resident.MatchWord) == ResidentMatchWord &&
                        APTR.ReadUInt32(resident,
                            ExecLayout.Resident.MatchTag) == resident.Raw)
                        return resident;
                    cursor += 2u;
                }
            }
            currentSegment = APTR.ReadUInt32(memory, 0);
        }

        DOS.SetIoErr(DOS.Error.ObjectNotFound);
        return APTR.Null;
    }

    private static int RunMorphOSVolumeResident(APTR name,
        out APTR resident)
    {
        const uint listFlags = (uint)(DosListLockFlags.Devices |
            DosListLockFlags.Read);

        resident = APTR.Null;
        var status = -1;
        var deviceProc = DOS.GetDeviceProc(CString.FromPointer(name),
            APTR.Null);
        if (deviceProc.IsNotNull)
        {
            var volume = APTR.FromPointer(APTR.ReadUInt32(deviceProc,
                DosLayout.DevProc.DeviceNode));
            var type = APTR.ReadUInt32(volume, DosLayout.DosList.Type);
            var device = APTR.Null;
            var dosListLocked = false;

            if (type == (uint)DosListType.Device)
            {
                device = volume;
            }
            else if (type == (uint)DosListType.Volume)
            {
                var volumeTask = APTR.ReadUInt32(volume,
                    DosLayout.DosList.Task);
                var cursor = DOS.LockDosList(listFlags);
                while (cursor.IsNotNull)
                {
                    cursor = DOS.NextDosEntry(cursor,
                        (uint)DosListLockFlags.Devices);
                    if (cursor.IsNull) break;
                    if (APTR.ReadUInt32(cursor,
                            DosLayout.DosList.Task) != volumeTask)
                        continue;
                    device = cursor;
                    dosListLocked = true;
                    break;
                }

                if (!dosListLocked) DOS.UnLockDosList(listFlags);
            }

            if (device.IsNotNull)
            {
                var segment = APTR.ReadUInt32(device,
                    DosLayout.DosList.Misc);
                if (segment != 0)
                {
                    resident = FindLibResident(segment);
                    status = resident.IsNotNull
                        ? DOS.RETURN_OK : DOS.RETURN_FAIL;
                }
            }

            if (dosListLocked) DOS.UnLockDosList(listFlags);
            DOS.FreeDeviceProc(deviceProc);
        }

        if (status != DOS.RETURN_OK && status != -1)
        {
            var fields = default(OneStringFields);
            fields.Value = name.Raw;
            DOS.VPrintf("Could not find version information for '%s'\n",
                OneStringFields.AddressOf(ref fields));
        }

        return status;
    }

    private static APTR QueryAmbientVersion()
    {
        var rexxBase = Exec.OpenLibraryRaw(
            CString.FromPointer(CString.ToUInt32(RexxSysLib.Name)), 36);
        var replyPort = APTR.Null;
        var hostMessage = APTR.Null;
        uint commandArgString = 0;
        var response = APTR.Null;
        if (rexxBase.IsNotNull)
        {
            replyPort = Exec.CreateMsgPort();
            if (replyPort.IsNotNull)
            {
                hostMessage = AmbientRexxApi.CreateRexxMsg(rexxBase,
                    replyPort, CString.FromPointer(0),
                    CString.ToUInt32("AMBIENT"));
                if (hostMessage.IsNotNull)
                {
                    commandArgString = AmbientRexxApi.CreateArgstring(
                        rexxBase,
                        CString.FromPointer(CString.ToUInt32("VERSION")),
                        RexxCommandBytes);
                    if (commandArgString != 0)
                    {
                        APTR.WriteUInt32(hostMessage,
                            RexxMessageLayout.Arguments,
                            commandArgString);
                        APTR.WriteUInt32(hostMessage,
                            RexxMessageLayout.Action,
                            RexxCommandWithResult);

                        Exec.Forbid();
                        var rexxPort = Exec.FindPort(
                            CString.FromPointer(CString.ToUInt32("AMBIENT")));
                        if (rexxPort.IsNotNull)
                        {
                            Exec.PutMsg(rexxPort, hostMessage);
                            Exec.Permit();
                            APTR answer;
                            do
                            {
                                Exec.WaitPort(replyPort);
                                answer = Exec.GetMsg(replyPort);
                            }
                            while (answer.IsNull);

                            var resultArgString = APTR.FromPointer(
                                APTR.ReadUInt32(answer,
                                    RexxMessageLayout.Result2));
                            if (APTR.ReadUInt32(answer,
                                    RexxMessageLayout.Result1) == 0 &&
                                resultArgString.IsNotNull)
                            {
                                var responseLength =
                                    AmbientRexxApi.LengthArgstring(rexxBase,
                                        resultArgString);
                                var responseBytes = responseLength + 1u;
                                response = Exec.AllocVec(responseBytes,
                                    (uint)Exec.MemoryFlags.Any);
                                if (response.IsNotNull)
                                {
                                    for (var index = 0u;
                                        index < responseLength; index++)
                                        APTR.WriteUInt8(response,
                                            unchecked((int)index),
                                            APTR.ReadUInt8(resultArgString,
                                                unchecked((int)index)));
                                    APTR.WriteUInt8(response,
                                        unchecked((int)responseLength), 0);
                                }
                                AmbientRexxApi.DeleteArgstring(rexxBase,
                                    resultArgString);
                            }
                        }
                        else
                        {
                            Exec.Permit();
                        }
                    }
                }
            }

            if (commandArgString != 0)
                AmbientRexxApi.DeleteArgstring(rexxBase,
                    APTR.FromPointer(commandArgString));
            if (hostMessage.IsNotNull)
                AmbientRexxApi.DeleteRexxMsg(rexxBase, hostMessage);
            if (replyPort.IsNotNull) Exec.DeleteMsgPort(replyPort);
            Exec.CloseLibrary(rexxBase);
        }

        return response;
    }

    private static void PrintAmbientVersion(uint version,
        uint revision)
    {
        DOS.PutStr(", ");
        var fields = default(Fields);
        fields.Name = CString.ToUInt32("Ambient");
        fields.Version = version;
        fields.Revision = revision;
        DOS.VPrintf("%s %lu.%lu", Fields.AddressOf(ref fields));
        DOS.PutStr(", ");
    }

    private static void SetAmbientVersion(uint version, uint revision)
    {
        var cells = default(AmbientTextCells);
        var text = AmbientTextCells.AddressOf(ref cells);
        var position = 0;
        WriteVersionNumber(text, ref position, unchecked((int)version));
        APTR.WriteUInt8(text, position++, (byte)'.');
        WriteVersionNumber(text, ref position, unchecked((int)revision));
        APTR.WriteUInt8(text, position, 0);
        _ = DOS.SetVar(CString.FromPointer(CString.ToUInt32("Ambient")),
            text, -1, (int)GlobalVariableFlags.LocalOnly);
    }

    private static void WriteVersionNumber(APTR text, ref int position,
        int value)
    {
        if (value < 0) APTR.WriteUInt8(text, position++, (byte)'-');
        var digitStart = position;
        var magnitude = value < 0
            ? unchecked(0u - (uint)value) : (uint)value;
        do
        {
            APTR.WriteUInt8(text, position++,
                unchecked((byte)('0' + magnitude % 10u)));
            magnitude /= 10u;
        }
        while (magnitude != 0);

        var left = digitStart;
        var right = position - 1;
        while (left < right)
        {
            var valueAtLeft = APTR.ReadUInt8(text, left);
            var valueAtRight = APTR.ReadUInt8(text, right);
            APTR.WriteUInt8(text, left, valueAtRight);
            APTR.WriteUInt8(text, right, valueAtLeft);
            left++;
            right--;
        }
    }

    private static APTR FindLibraryNode(APTR list, uint wantedName)
    {
        var node = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        while (node.IsNotNull &&
            APTR.ReadUInt32(node, ExecLayout.Node.Successor) != 0)
        {
            var nodeName = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Name));
            if (nodeName.IsNotNull &&
                GuestNameEqualsIgnoreCase(nodeName, wantedName))
                return node;
            node = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Successor));
        }
        return APTR.Null;
    }

    private static APTR FindLibraryNode(APTR list, uint wantedName,
        APTR utilityBase)
    {
        var node = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        while (node.IsNotNull &&
            APTR.ReadUInt32(node, ExecLayout.Node.Successor) != 0)
        {
            var nodeName = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Name));
            if (nodeName.IsNotNull &&
                VersionUtilityApi.Stricmp(utilityBase,
                    CString.FromPointer(nodeName.Raw),
                    CString.FromPointer(wantedName)) == 0)
                return node;
            node = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Successor));
        }
        return APTR.Null;
    }

    private static bool GuestNameEqualsIgnoreCase(APTR left,
        uint rightAddress)
    {
        var offset = 0;
        for (;; offset++)
        {
            var leftChar = (uint)APTR.ReadUInt8(left, offset);
            var rightChar = (uint)APTR.ReadUInt8(
                APTR.FromPointer(rightAddress), offset);
            if (leftChar >= (uint)'A' && leftChar <= (uint)'Z')
                leftChar += (uint)('a' - 'A');
            if (rightChar >= (uint)'A' && rightChar <= (uint)'Z')
                rightChar += (uint)('a' - 'A');
            if (leftChar != rightChar) return false;
            if (leftChar == 0) return true;
        }
    }

    private static bool TryFindLibraryVersionTail(APTR ids,
        out APTR tail)
    {
        tail = APTR.Null;
        if (ids.IsNull) return false;

        var lineEnd = FindLineEnd(ids.Raw);
        for (var cursor = ids.Raw; cursor < lineEnd; cursor++)
        {
            var current = APTR.ReadUInt8(APTR.FromPointer(cursor), 0);
            if (current != (uint)' ' && current != (uint)'\t') continue;
            var digits = cursor + 1u;
            if (APTR.ReadUInt8(APTR.FromPointer(digits), 0) == (uint)'+' ||
                APTR.ReadUInt8(APTR.FromPointer(digits), 0) == (uint)'-')
                digits++;
            var digitsStart = digits;
            while (digits < lineEnd)
            {
                var digit = APTR.ReadUInt8(APTR.FromPointer(digits), 0);
                if (digit < (uint)'0' || digit > (uint)'9') break;
                digits++;
            }
            if (digits == digitsStart || digits >= lineEnd ||
                APTR.ReadUInt8(APTR.FromPointer(digits), 0) != (uint)'.')
                continue;
            tail = APTR.FromPointer(cursor);
            return true;
        }

        for (var cursor = ids.Raw; cursor < lineEnd; cursor++)
        {
            var current = APTR.ReadUInt8(APTR.FromPointer(cursor), 0);
            if (current != (uint)' ' && current != (uint)'\t') continue;
            var digits = cursor + 1u;
            if (APTR.ReadUInt8(APTR.FromPointer(digits), 0) == (uint)'+' ||
                APTR.ReadUInt8(APTR.FromPointer(digits), 0) == (uint)'-')
                digits++;
            var digitsStart = digits;
            while (digits < lineEnd)
            {
                var digit = APTR.ReadUInt8(APTR.FromPointer(digits), 0);
                if (digit < (uint)'0' || digit > (uint)'9') break;
                digits++;
            }
            if (digits == digitsStart) continue;
            tail = APTR.FromPointer(cursor);
            return true;
        }
        return false;
    }

    private static void PrintMd5Digest(APTR context)
    {
        PrintMd5Digest(ReadMd5Digest(context));
    }

    private static DigestFields ReadMd5Digest(APTR context)
    {
        var fields = default(DigestFields);
        fields.A = ReverseBytes(APTR.ReadUInt32(context, 8));
        fields.B = ReverseBytes(APTR.ReadUInt32(context, 12));
        fields.C = ReverseBytes(APTR.ReadUInt32(context, 16));
        fields.D = ReverseBytes(APTR.ReadUInt32(context, 20));
        return fields;
    }

    private static void PrintMd5Digest(DigestFields fields)
    {
        DOS.VPrintf("%08lX%08lX%08lX%08lX  ",
            DigestFields.AddressOf(ref fields));
    }

    private static uint ReverseBytes(uint value) =>
        (value >> 24) | ((value >> 8) & 0x0000ff00u) |
        ((value << 8) & 0x00ff0000u) | (value << 24);

    private static void InitializeMd5(APTR context)
    {
        APTR.WriteUInt32(context, 0, 0);
        APTR.WriteUInt32(context, 4, 0);
        APTR.WriteUInt32(context, 8, 0x67452301u);
        APTR.WriteUInt32(context, 12, 0xefcdab89u);
        APTR.WriteUInt32(context, 16, 0x98badcfeu);
        APTR.WriteUInt32(context, 20, 0x10325476u);
        APTR.WriteUInt32(context, 24, 0);
    }

    private static void UpdateMd5(APTR context, APTR input, uint length)
    {
        var inputAddress = input.Raw;
        var remaining = length;
        var totalLow = APTR.ReadUInt32(context, 0);
        var nextLow = unchecked(totalLow + remaining);
        var totalHigh = APTR.ReadUInt32(context, 4);
        if (nextLow < totalLow) totalHigh = unchecked(totalHigh + 1u);
        APTR.WriteUInt32(context, 0, nextLow);
        APTR.WriteUInt32(context, 4, totalHigh);

        var buffered = APTR.ReadUInt32(context, 24);
        while (remaining != 0)
        {
            if (buffered == 0 && remaining >= 64)
            {
                TransformMd5(context,
                    APTR.FromPointer(inputAddress));
                inputAddress += 64u;
                remaining -= 64u;
                continue;
            }

            var room = 64u - buffered;
            var copied = remaining < room ? remaining : room;
            var source = inputAddress;
            var target = context.Raw + 28u + buffered;
            for (var index = 0u; index < copied; index++)
                APTR.WriteUInt8(APTR.FromPointer(target + index), 0,
                    APTR.ReadUInt8(APTR.FromPointer(source + index), 0));
            inputAddress = source + copied;
            buffered += copied;
            remaining -= copied;
            if (buffered == 64u)
            {
                TransformMd5(context,
                    APTR.FromPointer(context.Raw + 28u));
                buffered = 0;
            }
        }
        APTR.WriteUInt32(context, 24, buffered);
    }

    private static int DrainAndFinalizeMd5(ref FileLease lease)
    {
        for (;;)
        {
            if ((Exec.SetSignal(0u, CtrlCMask) & CtrlCMask) != 0)
            {
                DOS.SetIoErr(DOS.Error.Break);
                return (int)DOS.Error.Break;
            }

            var read = DOS.Read(lease.File, lease.Scratch,
                unchecked((int)FileBufferBytes));
            if (read < 0) return (int)DOS.IoErr();
            if ((uint)read > FileBufferBytes)
                return (int)DOS.Error.LineTooLong;
            if (read == 0) break;
            UpdateMd5(lease.Md5Context, lease.Scratch,
                unchecked((uint)read));
        }

        FinalizeMd5(lease.Md5Context);
        return 0;
    }

    private static void FinalizeMd5(APTR context)
    {
        var totalLow = APTR.ReadUInt32(context, 0);
        var totalHigh = APTR.ReadUInt32(context, 4);
        var bitLow = unchecked(totalLow << 3);
        var bitHigh = unchecked((totalHigh << 3) | (totalLow >> 29));
        var buffered = APTR.ReadUInt32(context, 24);
        var block = APTR.FromPointer(context.Raw + 28u);
        APTR.WriteUInt8(APTR.FromPointer(block.Raw + buffered), 0, 0x80);
        buffered++;
        if (buffered > 56u)
        {
            while (buffered < 64u)
            {
                APTR.WriteUInt8(APTR.FromPointer(block.Raw + buffered),
                    0, 0);
                buffered++;
            }
            TransformMd5(context, block);
            buffered = 0;
        }
        while (buffered < 56u)
        {
            APTR.WriteUInt8(APTR.FromPointer(block.Raw + buffered), 0, 0);
            buffered++;
        }
        for (var index = 0u; index < 4u; index++)
        {
            APTR.WriteUInt8(APTR.FromPointer(block.Raw + 56u + index), 0,
                unchecked((byte)(bitLow >> unchecked((int)(index * 8u)))));
            APTR.WriteUInt8(APTR.FromPointer(block.Raw + 60u + index), 0,
                unchecked((byte)(bitHigh >> unchecked((int)(index * 8u)))));
        }
        TransformMd5(context, block);
    }

    private static void TransformMd5(APTR context, APTR block)
    {
        var a = APTR.ReadUInt32(context, 8);
        var b = APTR.ReadUInt32(context, 12);
        var c = APTR.ReadUInt32(context, 16);
        var d = APTR.ReadUInt32(context, 20);
        var originalA = a;
        var originalB = b;
        var originalC = c;
        var originalD = d;

        for (var index = 0u; index < 64u; index++)
        {
            uint f;
            uint word;
            if (index < 16u)
            {
                f = (b & c) | (~b & d);
                word = index;
            }
            else if (index < 32u)
            {
                f = (d & b) | (~d & c);
                word = (5u * index + 1u) & 15u;
            }
            else if (index < 48u)
            {
                f = b ^ c ^ d;
                word = (3u * index + 5u) & 15u;
            }
            else
            {
                f = c ^ (b | ~d);
                word = (7u * index) & 15u;
            }

            var messageWord = ReadMd5Word(block, word * 4u);
            var sum = unchecked(a + f + Md5Constant(index) + messageWord);
            var shift = Md5Shift(index);
            var rotated = unchecked((sum << unchecked((int)shift)) |
                (sum >> unchecked((int)(32u - shift))));
            var next = unchecked(b + rotated);
            a = d;
            d = c;
            c = b;
            b = next;
        }

        APTR.WriteUInt32(context, 8, unchecked(originalA + a));
        APTR.WriteUInt32(context, 12, unchecked(originalB + b));
        APTR.WriteUInt32(context, 16, unchecked(originalC + c));
        APTR.WriteUInt32(context, 20, unchecked(originalD + d));
    }

    private static uint ReadMd5Word(APTR block, uint offset) =>
        (uint)APTR.ReadUInt8(APTR.FromPointer(block.Raw + offset), 0) |
        ((uint)APTR.ReadUInt8(
            APTR.FromPointer(block.Raw + offset + 1u), 0) << 8) |
        ((uint)APTR.ReadUInt8(
            APTR.FromPointer(block.Raw + offset + 2u), 0) << 16) |
        ((uint)APTR.ReadUInt8(
            APTR.FromPointer(block.Raw + offset + 3u), 0) << 24);

    private static uint Md5Shift(uint index)
    {
        var step = index & 3u;
        if (index < 16u)
        {
            if (step == 0u) return 7u;
            if (step == 1u) return 12u;
            if (step == 2u) return 17u;
            return 22u;
        }
        if (index < 32u)
        {
            if (step == 0u) return 5u;
            if (step == 1u) return 9u;
            if (step == 2u) return 14u;
            return 20u;
        }
        if (index < 48u)
        {
            if (step == 0u) return 4u;
            if (step == 1u) return 11u;
            if (step == 2u) return 16u;
            return 23u;
        }
        if (step == 0u) return 6u;
        if (step == 1u) return 10u;
        if (step == 2u) return 15u;
        return 21u;
    }

    private static uint Md5Constant(uint index) => index switch
    {
        0 => 0xd76aa478u, 1 => 0xe8c7b756u, 2 => 0x242070dbu, 3 => 0xc1bdceeeu,
        4 => 0xf57c0fafu, 5 => 0x4787c62au, 6 => 0xa8304613u, 7 => 0xfd469501u,
        8 => 0x698098d8u, 9 => 0x8b44f7afu, 10 => 0xffff5bb1u, 11 => 0x895cd7beu,
        12 => 0x6b901122u, 13 => 0xfd987193u, 14 => 0xa679438eu, 15 => 0x49b40821u,
        16 => 0xf61e2562u, 17 => 0xc040b340u, 18 => 0x265e5a51u, 19 => 0xe9b6c7aau,
        20 => 0xd62f105du, 21 => 0x02441453u, 22 => 0xd8a1e681u, 23 => 0xe7d3fbc8u,
        24 => 0x21e1cde6u, 25 => 0xc33707d6u, 26 => 0xf4d50d87u, 27 => 0x455a14edu,
        28 => 0xa9e3e905u, 29 => 0xfcefa3f8u, 30 => 0x676f02d9u, 31 => 0x8d2a4c8au,
        32 => 0xfffa3942u, 33 => 0x8771f681u, 34 => 0x6d9d6122u, 35 => 0xfde5380cu,
        36 => 0xa4beea44u, 37 => 0x4bdecfa9u, 38 => 0xf6bb4b60u, 39 => 0xbebfbc70u,
        40 => 0x289b7ec6u, 41 => 0xeaa127fau, 42 => 0xd4ef3085u, 43 => 0x04881d05u,
        44 => 0xd9d4d039u, 45 => 0xe6db99e5u, 46 => 0x1fa27cf8u, 47 => 0xc4ac5665u,
        48 => 0xf4292244u, 49 => 0x432aff97u, 50 => 0xab9423a7u, 51 => 0xfc93a039u,
        52 => 0x655b59c3u, 53 => 0x8f0ccc92u, 54 => 0xffeff47du, 55 => 0x85845dd1u,
        56 => 0x6fa87e4fu, 57 => 0xfe2ce6e0u, 58 => 0xa3014314u, 59 => 0x4e0811a1u,
        60 => 0xf7537e82u, 61 => 0xbd3af235u, 62 => 0x2ad7d2bbu, 63 => 0xeb86d391u,
        _ => 0u
    };

    private static void PrintFileVersion(FileVersion version, uint full,
        uint md5)
    {
        var fields = default(FileFields);
        fields.Name = version.Name.Raw;
        fields.NameSpace = version.Name.IsNull ||
            APTR.ReadUInt8(version.Name, 0) == 0
                ? CString.ToUInt32("") : CString.ToUInt32(" ");
        fields.Version = version.VersionText.Raw;
        fields.Revision = version.RevisionText.Raw;
        fields.Padding = version.Padding.Raw;
        fields.Date = version.Date.Raw;
        fields.ExtraLineFeed = md5 != 0 && full != 0
            ? CString.ToUInt32(" ") : version.ExtraLineFeed.Raw;
        fields.Extra = version.Extra.Raw;

        if (full != 0)
            DOS.VPrintf("%s%s%s%s%s%s%s%s\n",
                FileFields.AddressOf(ref fields));
        else
            DOS.VPrintf("%s%s%s%s%s\n",
                FileFields.AddressOf(ref fields));
    }

    private static void ReleaseFileLease(ref FileLease lease)
    {
        if (lease.Md5Context.IsNotNull)
        {
            Exec.FreeMem(lease.Md5Context, Md5ContextBytes);
            lease.Md5Context = APTR.Null;
        }
        if (lease.Scratch.IsNotNull)
        {
            Exec.FreeMem(lease.Scratch, ScratchBufferBytes);
            lease.Scratch = APTR.Null;
        }
        if (lease.Buffer.IsNotNull)
        {
            Exec.FreeMem(lease.Buffer, FileBufferBytes);
            lease.Buffer = APTR.Null;
        }
        if (lease.File.IsNotNull)
        {
            DOS.Close(lease.File);
            lease.File = default;
        }
    }

    private static uint SkipWhitespace(uint cursor)
    {
        var position = cursor;
        while (IsWhitespace(APTR.ReadUInt8(APTR.FromPointer(position), 0)))
            position++;
        return position;
    }

    private static uint SkipSpaces(uint cursor)
    {
        var position = cursor;
        while (APTR.ReadUInt8(APTR.FromPointer(position), 0) == (uint)' ')
            position++;
        return position;
    }

    private static uint FindLineEnd(uint cursor)
    {
        var position = cursor;
        while (true)
        {
            var c = APTR.ReadUInt8(APTR.FromPointer(position), 0);
            if (c == 0 || c == (uint)'\r' || c == (uint)'\n')
                return position;
            position++;
        }
    }

    private static uint FindDateOrLineEnd(uint cursor, uint lineEnd)
    {
        var position = cursor;
        while (position < lineEnd)
        {
            var c = APTR.ReadUInt8(APTR.FromPointer(position), 0);
            if (c == (uint)'(') return position;
            position++;
        }
        return lineEnd;
    }

    private static uint CStringLength(APTR text, uint limit)
    {
        for (var index = 0u; index < limit; index++)
            if (APTR.ReadUInt8(text, unchecked((int)index)) == 0)
                return index;
        return limit;
    }

    private static uint CStringLengthUnbounded(APTR text)
    {
        var length = 0u;
        while (APTR.ReadUInt8(APTR.FromPointer(text.Raw + length), 0) != 0)
        {
            if (length == uint.MaxValue - text.Raw) return uint.MaxValue;
            length++;
        }
        return length;
    }

    private static bool IsWhitespace(uint value) =>
        value == (uint)' ' || value == (uint)'\t' ||
        value == (uint)'\r' || value == (uint)'\n' ||
        value == (uint)'\v' || value == (uint)'\f';

    private static bool IsAsciiAlphaNumeric(uint value) =>
        value >= (uint)'0' && value <= (uint)'9' ||
        value >= (uint)'A' && value <= (uint)'Z' ||
        value >= (uint)'a' && value <= (uint)'z';

    private static APTR ReadPointer(ref NativeCommandArguments arguments,
        uint index)
    {
        return arguments.TryGetResult(index, out var value)
            ? APTR.FromPointer(value) : APTR.Null;
    }

    private static uint ReadSwitch(ref NativeCommandArguments arguments,
        uint index)
    {
        return arguments.TryGetResult(index, out var value) ? value : 0;
    }

    private static int ReadNumber(ref NativeCommandArguments arguments,
        uint index, out bool present)
    {
        present = false;
        if (!arguments.TryGetResult(index, out var value) || value == 0)
            return 0;
        present = true;
        return unchecked((int)APTR.ReadUInt32(APTR.FromPointer(value), 0));
    }

    private static int CompareRequested(uint version, uint revision,
        int requestedVersion, bool hasVersion, int requestedRevision,
        bool hasRevision)
    {
        var actualVersion = unchecked((int)version);
        var actualRevision = unchecked((int)revision);
        if (hasVersion)
        {
            if (requestedVersion > actualVersion)
                return DOS.RETURN_WARN;
            if (requestedVersion == actualVersion && hasRevision &&
                requestedRevision > actualRevision)
                return DOS.RETURN_WARN;
        }
        else if (hasRevision && requestedRevision > actualRevision)
        {
            return DOS.RETURN_WARN;
        }

        return DOS.RETURN_OK;
    }

    private static bool TryReadRevision(APTR ids, out uint revision)
    {
        return TryReadVersionAndRevision(ids, out _, out revision);
    }

    private static bool TryReadVersionAndRevision(APTR ids,
        out uint version, out uint revision)
    {
        version = 0;
        revision = 0;
        if (ids.IsNull) return false;
        var cursor = ids.Raw;
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) != 0 &&
            APTR.ReadUInt8(APTR.FromPointer(cursor), 0) != (uint)' ')
            cursor++;
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) != 0 &&
            (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) < (uint)'0' ||
                APTR.ReadUInt8(APTR.FromPointer(cursor), 0) > (uint)'9'))
            cursor++;
        var digits = 0u;
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) >= (uint)'0' &&
            APTR.ReadUInt8(APTR.FromPointer(cursor), 0) <= (uint)'9')
        {
            version = version * 10 + APTR.ReadUInt8(
                APTR.FromPointer(cursor), 0) - (uint)'0';
            cursor++;
            digits++;
        }
        if (digits == 0 ||
            APTR.ReadUInt8(APTR.FromPointer(cursor), 0) != (uint)'.')
        {
            version = 0;
            return false;
        }
        cursor++;
        digits = 0;
        while (APTR.ReadUInt8(APTR.FromPointer(cursor), 0) >= (uint)'0' &&
            APTR.ReadUInt8(APTR.FromPointer(cursor), 0) <= (uint)'9')
        {
            revision = revision * 10 + APTR.ReadUInt8(
                APTR.FromPointer(cursor), 0) - (uint)'0';
            cursor++;
            digits++;
        }
        return digits != 0;
    }

    private static bool TryFindResidentVersionTail(APTR ids, out APTR tail)
    {
        tail = APTR.Null;
        if (ids.IsNull) return false;

        var cursor = ids.Raw;
        while (true)
        {
            var c = APTR.ReadUInt8(APTR.FromPointer(cursor), 0);
            if (c == 0) return false;
            if ((c == (uint)' ' || c == (uint)'\t') &&
                IsAsciiDigit(APTR.ReadUInt8(
                    APTR.FromPointer(cursor + 1), 0)))
            {
                tail = APTR.FromPointer(cursor);
                return true;
            }
            cursor++;
        }
    }

    private static bool IsAsciiDigit(uint value) =>
        value >= (uint)'0' && value <= (uint)'9';
}

[AmigaLibrary(DOS.Name)]
internal static class NativeMorphOSVersionDosRaw
{
    [AmigaLvo(-150)]
    [return: M68kRegister(M68kRegister.D0)]
    public static extern uint LoadSeg(
        [M68kRegister(M68kRegister.D1)] CString name);

    [AmigaLvo(-156)]
    public static extern void UnLoadSeg(
        [M68kRegister(M68kRegister.D1)] BPTR segment);
}
