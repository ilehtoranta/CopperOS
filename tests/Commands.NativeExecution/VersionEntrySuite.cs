using System;
using System.Text;
using Amiga;
using Copper68k;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record VersionEntryCase(
    bool System = false,
    bool Resident = false,
    string Name = "dos.library",
    bool ResidentFound = true,
    bool File = false,
    bool Md5 = false,
    bool Full = false,
    bool Unit = false,
    bool Internal = false,
    bool VersionLibraryAvailable = true,
    bool RexxLibraryAvailable = false,
    bool AmbientRexxPortAvailable = false,
    string? AmbientRexxResult = null,
    bool AmbientCopyAllocationFailure = false,
    string? AmbientPathVariable = null,
    string? AmbientPathFileContent = null,
    string? AmbientMossysFileContent = null,
    string? AmbientSysFileContent = null,
    string? AmbientExpectedVersion = null,
    bool AmbientIsFileSystem = false,
    bool AmbientLoadSegSuccess = false,
    string? AmbientLoadedResidentName = null,
    string? AmbientLoadedResidentIdString = null,
    bool AmbientLoadedResidentInSecondSegment = false,
    bool FileIsFileSystem = false,
    bool FileLoadSegSuccess = false,
    string? FileLoadedResidentName = null,
    string? FileLoadedResidentIdString = null,
    int FileLoadedResidentVersion = 38,
    bool FileLoadedResidentInSecondSegment = false,
    string? LibraryFileHitPath = null,
    string? LibraryFileContent = null,
    bool DirectFileHit = false,
    string? WorkbenchFileHitPath = null,
    bool CommandSegmentFound = false,
    int CommandSegmentSystem = 0,
    int CommandSegmentUseCount = 1,
    string? CommandSegmentVersionText = null,
    bool ShellCommandResidentFound = false,
    bool ExecLibraryFound = false,
    bool UtilityLibraryAvailable = true,
    string ExecLibraryName = "test.library",
    string ExecLibraryIdString = "$VER: test.library 12.3 (07.06.2023) extra",
    int ExecLibraryVersion = 12,
    int ExecLibraryRevision = 3,
    bool ExecDeviceFound = false,
    string ExecDeviceName = "test.device",
    string ExecDeviceIdString = "$VER: test.device 14.2 (08.06.2023) device",
    int ExecDeviceVersion = 14,
    int ExecDeviceRevision = 2,
    bool VersionLibraryListed = true,
    string WorkbenchLibraryName = "version.library",
    string? WorkbenchIdString = null,
    string ResidentName = "dos.library",
    string? ResidentIdString = null,
    int ResidentVersion = 50,
    int ResidentFlags = 0,
    int ResidentRevision = 0,
    bool ResidentTableContinuation = false,
    bool ResidentTableDecoy = false,
    bool ResidentNullIdString = false,
    bool ResidentCopyAllocationFailure = false,
    bool ResidentComparisonNonzeroLowWordZero = false,
    string? WorkbenchFullExtra = null,
    bool WorkbenchExtraAllocationFailure = false,
    uint? WorkbenchDateSeconds = null,
    int WorkbenchDate2AmigaExpected = 1,
    int WorkbenchDateParseExpected = 0,
    int WorkbenchDateFormatExpected = 1,
    int ResidentParseAllocationFailureAt = 0,
    int SystemParseAllocationFailureAt = 0,
    int MorphOSVersion = 3,
    int MorphOSRevision = 20,
    int KickstartVersion = 50,
    int KickstartRevision = 34,
    int WorkbenchVersion = 39,
    int WorkbenchRevision = 1,
    int? RequestedVersion = null,
    int? RequestedRevision = null,
    int ParserError = 0,
    string FileContent = "$VER: fixture 12.34\n",
    int FileOpenError = 0,
    bool FileReadFailure = false,
    int FileReadFailureAt = -1,
    int AllocationFailureAt = 0,
    bool CtrlC = false,
    bool VolumeDeviceProcFound = false,
    bool VolumeDeviceIsVolume = false,
    bool VolumeDeviceFound = true,
    bool VolumeSegmentFound = true,
    bool VolumeResidentFound = true,
    bool WorkbenchDeviceListLookup = false,
    bool WorkbenchDeviceListEntryFound = true,
    bool WorkbenchDeviceListHasStartup = true,
    bool WorkbenchDeviceListSegmentFound = true,
    bool WorkbenchDeviceListResidentFound = true,
    uint VolumeTask = 0x7123,
    string[]? Names = null);

internal sealed record VersionNativeLayout(uint Names, uint Name,
    uint NumericVersion, uint NumericRevision)
{
    public uint Control => Names;
    public uint WorkbenchParsedName { get; set; }
    public string? WorkbenchParsedNameAtFreeArgs { get; set; }
    public uint FileBuffer { get; set; }
    public uint ScratchBuffer { get; set; }
    public uint Md5Context { get; set; }
    public uint VersionLibraryBase { get; set; }
    public uint ExecLibraryBase { get; set; }
    public uint ExecLibraryName { get; set; }
    public uint ExecLibraryIdString { get; set; }
    public uint ExecLibraryVersionTextBytes { get; set; }
    public uint ExecLibraryNameCopyBytes { get; set; }
    public uint ExecLibraryVersionText { get; set; }
    public uint ExecLibraryVersionScratch { get; set; }
    public uint ExecLibraryNameCopy { get; set; }
    public uint ExecDeviceBase { get; set; }
    public uint ExecDeviceVersionTextBytes { get; set; }
    public uint ExecDeviceNameCopyBytes { get; set; }
    public uint ExecDeviceNameCopy { get; set; }
    public uint ExecDeviceVersionText { get; set; }
    public uint ExecDeviceVersionScratch { get; set; }
    public int UtilityStricmpCalls { get; set; }
    public int WorkbenchNameAllocationCalls { get; set; }
    public int WorkbenchNameFreeCalls { get; set; }
    public uint WorkbenchOwnedName { get; set; }
    public uint WorkbenchOwnedLibraryId { get; set; }
    public int WorkbenchLibraryIdAllocationCalls { get; set; }
    public int WorkbenchLibraryIdFreeCalls { get; set; }
    public uint WorkbenchOwnedExtra { get; set; }
    public int WorkbenchExtraAllocationCalls { get; set; }
    public int WorkbenchExtraFreeCalls { get; set; }
    public int WorkbenchDate2AmigaCalls { get; set; }
    public int WorkbenchNewlineCalls { get; set; }
    public uint WorkbenchBorrowedName { get; set; }
    public uint WorkbenchBorrowedIdString { get; set; }
    public byte[] WorkbenchNameBefore { get; set; } = [];
    public byte[] WorkbenchIdBefore { get; set; } = [];
    public uint VolumeDeviceProc { get; set; }
    public uint VolumeLookupNode { get; set; }
    public uint VolumeListCursor { get; set; }
    public uint VolumePriorDeviceNode { get; set; }
    public uint VolumeDeviceNode { get; set; }
    public uint VolumeSegmentAddress { get; set; }
    public uint VolumeResident { get; set; }
    public int GetDeviceProcCalls { get; set; }
    public int FreeDeviceProcCalls { get; set; }
    public int LockDosListCalls { get; set; }
    public int FindDosEntryCalls { get; set; }
    public int NextDosEntryCalls { get; set; }
    public int UnLockDosListCalls { get; set; }
    public uint RexxLibraryBase { get; set; }
    public uint AmbientReplyPort { get; set; }
    public uint AmbientHostMessage { get; set; }
    public uint AmbientCommandArgString { get; set; }
    public uint AmbientResultArgString { get; set; }
    public uint AmbientPort { get; set; }
    public uint AmbientResultText { get; set; }
    public uint AmbientFileHandle { get; set; }
    public uint AmbientFileBuffer { get; set; }
    public uint AmbientFileScratch { get; set; }
    public uint AmbientSegmentRaw { get; set; }
    public uint AmbientSegmentAddress { get; set; }
    public uint AmbientSegmentSecondRaw { get; set; }
    public uint AmbientSegmentSecondAddress { get; set; }
    public uint AmbientSegmentResident { get; set; }
    public uint AmbientResidentName { get; set; }
    public uint AmbientResidentIdString { get; set; }
    public uint AmbientResidentText { get; set; }
    public uint AmbientResidentTextBytes { get; set; }
    public string? AmbientFileContent { get; set; }
    public string? AmbientFileName { get; set; }
    public int AmbientFilePosition { get; set; }
    public int AmbientFileReadCalls { get; set; }
    public int AmbientFileOpenAttempts { get; set; }
    public int AmbientFileOpenSuccesses { get; set; }
    public int AmbientFileCloseCalls { get; set; }
    public int AmbientFileAllocMemCalls { get; set; }
    public int AmbientFileAllocMemRequests { get; set; }
    public int AmbientFileAllocMemSuccesses { get; set; }
    public int AmbientFileFreeMemCalls { get; set; }
    public int AmbientGetVarCalls { get; set; }
    public int AmbientIsFileSystemCalls { get; set; }
    public int AmbientLoadSegCalls { get; set; }
    public int AmbientUnLoadSegCalls { get; set; }
    public int AmbientResidentTextAllocCalls { get; set; }
    public int AmbientResidentTextAllocRequests { get; set; }
    public int AmbientResidentTextAllocSuccesses { get; set; }
    public int AmbientResidentTextFreeCalls { get; set; }
    public bool AmbientFileActive { get; set; }
    public bool AmbientSegmentActive { get; set; }
    public int AmbientPortCreateCalls { get; set; }
    public int AmbientPortDeleteCalls { get; set; }
    public int AmbientMessageCreateCalls { get; set; }
    public int AmbientMessageDeleteCalls { get; set; }
    public int AmbientCommandCreateCalls { get; set; }
    public int AmbientCommandDeleteCalls { get; set; }
    public int AmbientResultDeleteCalls { get; set; }
    public int AmbientPortLookups { get; set; }
    public int AmbientPutCalls { get; set; }
    public int AmbientWaitCalls { get; set; }
    public int AmbientGetCalls { get; set; }
    public int AmbientCopyAllocCalls { get; set; }
    public int AmbientCopyFreeCalls { get; set; }
    public int AmbientSetVarCalls { get; set; }
    public int AmbientLibraryOpenCalls { get; set; }
    public int AmbientLibraryCloseCalls { get; set; }
    public uint SystemVersionText { get; set; }
    public uint SystemVersionScratch { get; set; }
    public uint SystemVersionTextBytes { get; set; }
    public uint ResidentVersionText { get; set; }
    public uint ResidentVersionScratch { get; set; }
    public uint ResidentVersionTextBytes { get; set; }
    public uint FileHandle { get; set; }
    public string? FileName { get; set; }
    public uint LibrarySearchPathBuffer { get; set; }
    public uint LibrarySearchPathBytes { get; set; }
    public int LibrarySearchPathAllocCalls { get; set; }
    public int LibrarySearchPathFreeCalls { get; set; }
    public List<string> LibrarySearchPaths { get; } = [];
    public string? LibraryFileName { get; set; }
    public string? LibraryFileContent { get; set; }
    public bool LibraryFileActive { get; set; }
    public int LibraryFileOpenAttempts { get; set; }
    public int LibraryFileOpenSuccesses { get; set; }
    public int LibraryFileCloseCalls { get; set; }
    public int LibraryFileReadCalls { get; set; }
    public int DirectFileOpenAttempts { get; set; }
    public uint CommandSegmentNode { get; set; }
    public uint CommandSegmentAddress { get; set; }
    public uint CommandSegmentRaw { get; set; }
    public uint CommandSegmentVersionTextBytes { get; set; }
    public uint CommandSegmentText { get; set; }
    public uint CommandSegmentScratch { get; set; }
    public int FindSegmentCalls { get; set; }
    public int FilePartCalls { get; set; }
    public int FilePosition { get; set; }
    public int FileReadCalls { get; set; }
    public int FileOpenCalls { get; set; }
    public int FileCloseCalls { get; set; }
    public List<string> WorkbenchNamedFilePaths { get; } = [];
    public int WorkbenchNamedFileOpenSuccesses { get; set; }
    public int WorkbenchPathAllocationCalls { get; set; }
    public int WorkbenchPathFreeCalls { get; set; }
    public List<(uint Pointer, uint Size)> WorkbenchOwnedProviderPaths { get; } = [];
    public int Seek64Calls { get; set; }
    public int SetSignalCalls { get; set; }
}

internal sealed partial class ProbeFixture
{
    private const uint VersionFileBytes = 16_385;
    private const uint VersionScratchBytes = 16_449;
    public const string VersionEntrySuite =
        "morphos320-version-native-entry-vector-fixture";
    public const string Workbench31VersionEntrySuite =
        "workbench31-version-native-entry-vector-fixture";
    private const uint VersionRexxLibraryBase = 0x7000;

    public static bool IsVersionEntrySuite(string value) =>
        value == VersionEntrySuite || value == Workbench31VersionEntrySuite;

    public static bool IsWorkbench31VersionEntrySuite(string value) =>
        value == Workbench31VersionEntrySuite;

    private List<object> RunVersionEntryCases()
    {
        const string systemVersion =
            "MorphOS 3.20, Kickstart 50.34, Workbench 39.1\n";
        ProbeCase[] cases =
        [
            VersionCase("system-version", new(System: true),
                output: systemVersion),
            VersionCase("system-version-res-without-name",
                new(System: true, Resident: true),
                output: systemVersion),
            VersionCase("system-version-mismatch",
                new(System: true, RequestedVersion: 99),
                result: DOS.RETURN_WARN,
                output: systemVersion),
            VersionCase("system-version-lower-major",
                new(System: true, RequestedVersion: 38),
                output: systemVersion),
            VersionCase("system-version-lower-revision",
                new(System: true, RequestedVersion: 39,
                    RequestedRevision: 0),
                output: systemVersion),
            VersionCase("system-version-higher-revision",
                new(System: true, RequestedVersion: 39,
                    RequestedRevision: 2),
                result: DOS.RETURN_WARN,
                output: systemVersion),
            VersionCase("system-revision-only-higher",
                new(System: true, RequestedRevision: 2),
                result: DOS.RETURN_WARN,
                output: systemVersion),
            VersionCase("system-full", new(System: true, Full: true),
                output: "MorphOS 3.20, Kickstart 50.34, Workbench 39.1 (07-Jun-2023)\nextra\n"),
            VersionCase("system-full-version-mismatch",
                new(System: true, Full: true,
                    WorkbenchIdString: "$VER: version.library 40.2\n"),
                output: "MorphOS 3.20, Kickstart 50.34, Workbench 39.1\n"),
            VersionCase("system-ambient-arexx-version",
                new(System: true, RexxLibraryAvailable: true,
                    AmbientRexxPortAvailable: true,
                    AmbientRexxResult: "5.2"),
                result: DOS.RETURN_WARN,
                output: "MorphOS 3.20, Ambient 5.2, Kickstart 50.34\n"),
            VersionCase("system-ambient-configured-file-version",
                new(System: true,
                    AmbientPathVariable: "SYS:Tools/Ambient",
                    AmbientPathFileContent:
                        "$VER: Ambient 6.14\n",
                    AmbientExpectedVersion: "6.14"),
                result: DOS.RETURN_WARN,
                output: "MorphOS 3.20, Ambient 6.14, Kickstart 50.34\n"),
            VersionCase("system-ambient-mossys-file-version",
                new(System: true,
                    AmbientMossysFileContent:
                        "$VER: Ambient 7.3\n",
                    AmbientExpectedVersion: "7.3"),
                result: DOS.RETURN_WARN,
                output: "MorphOS 3.20, Ambient 7.3, Kickstart 50.34\n"),
            VersionCase("system-ambient-sys-file-version",
                new(System: true,
                    AmbientSysFileContent:
                        "$VER: Ambient 8.5\n",
                    AmbientExpectedVersion: "8.5"),
                result: DOS.RETURN_WARN,
                output: "MorphOS 3.20, Ambient 8.5, Kickstart 50.34\n"),
            VersionCase("system-ambient-mossys-loadseg-resident-version",
                new(System: true,
                    AmbientMossysFileContent: "Ambient executable fixture\n",
                    AmbientExpectedVersion: "9.6",
                    AmbientIsFileSystem: true,
                    AmbientLoadSegSuccess: true,
                    AmbientLoadedResidentName: "AmbientBinary",
                    AmbientLoadedResidentIdString:
                        "$VER: AmbientBinary 9.6"),
                result: DOS.RETURN_WARN,
                output: "MorphOS 3.20, Ambient 9.6, Kickstart 50.34\n"),
            VersionCase("system-ambient-mossys-loadseg-linked-resident-version",
                new(System: true,
                    AmbientMossysFileContent: "Ambient executable fixture\n",
                    AmbientExpectedVersion: "9.6",
                    AmbientIsFileSystem: true,
                    AmbientLoadSegSuccess: true,
                    AmbientLoadedResidentName: "AmbientBinary",
                    AmbientLoadedResidentIdString:
                        "$VER: AmbientBinary 9.6",
                    AmbientLoadedResidentInSecondSegment: true),
                result: DOS.RETURN_WARN,
                output: "MorphOS 3.20, Ambient 9.6, Kickstart 50.34\n"),
            VersionCase("system-ambient-mossys-nonfilesystem-falls-to-sys",
                new(System: true,
                    AmbientMossysFileContent: "not an executable\n",
                    AmbientSysFileContent: "$VER: Ambient 10.2\n",
                    AmbientExpectedVersion: "10.2"),
                result: DOS.RETURN_WARN,
                output: "MorphOS 3.20Could not find version information for 'mossys:ambient/ambient'\n, Ambient 10.2, Kickstart 50.34\n"),
            VersionCase("system-ambient-mossys-loadseg-failure-falls-to-sys",
                new(System: true,
                    AmbientMossysFileContent: "not an executable\n",
                    AmbientSysFileContent: "$VER: Ambient 10.2\n",
                    AmbientExpectedVersion: "10.2",
                    AmbientIsFileSystem: true),
                result: DOS.RETURN_WARN,
                output: "MorphOS 3.20Could not find version information for 'mossys:ambient/ambient'\n, Ambient 10.2, Kickstart 50.34\n"),
            VersionCase("system-ambient-mossys-loadseg-no-resident-falls-to-sys",
                new(System: true,
                    AmbientMossysFileContent: "not an executable\n",
                    AmbientSysFileContent: "$VER: Ambient 10.2\n",
                    AmbientExpectedVersion: "10.2",
                    AmbientIsFileSystem: true,
                    AmbientLoadSegSuccess: true),
                result: DOS.RETURN_WARN,
                output: "MorphOS 3.20Could not find version information for 'mossys:ambient/ambient'\n, Ambient 10.2, Kickstart 50.34\n"),
            VersionCase("system-ambient-arexx-result-malformed",
                new(System: true, RexxLibraryAvailable: true,
                    AmbientRexxPortAvailable: true,
                    AmbientRexxResult: "5x2"),
                output: systemVersion),
            VersionCase("system-ambient-arexx-copy-allocation-failure",
                new(System: true, RexxLibraryAvailable: true,
                    AmbientRexxPortAvailable: true,
                    AmbientRexxResult: "5.2",
                    AmbientCopyAllocationFailure: true),
                output: systemVersion),
            VersionCase("system-ambient-arexx-port-missing",
                new(System: true, RexxLibraryAvailable: true),
                output: systemVersion),
            VersionCase("system-ambient-version-library-unavailable",
                new(System: true, RexxLibraryAvailable: true,
                    AmbientRexxPortAvailable: true,
                    AmbientRexxResult: "5.2",
                    VersionLibraryAvailable: false),
                result: DOS.RETURN_WARN,
                output: "MorphOS 3.20, Ambient 5.2, Kickstart 50.34\n"),
            VersionCase("system-library-name-case-insensitive",
                new(System: true,
                    WorkbenchLibraryName: "Version.Library"),
                output: systemVersion),
            VersionCase("system-library-prefers-dotted-version-token",
                new(System: true, Full: true,
                    WorkbenchIdString: "$VER: version.library build 2022 release 39.1 (07.06.2023) extra"),
                output: "MorphOS 3.20, Kickstart 50.34, Workbench 39.1 (07-Jun-2023)\nextra\n"),
            VersionCase("system-version-text-allocation-failure",
                new(System: true, SystemParseAllocationFailureAt: 1),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.NoFreeStore,
                output: "MorphOS 3.20, Kickstart 50.34"),
            VersionCase("system-version-scratch-allocation-failure",
                new(System: true, SystemParseAllocationFailureAt: 2),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.NoFreeStore,
                output: "MorphOS 3.20, Kickstart 50.34"),
            VersionCase("system-version-library-unavailable",
                new(System: true, VersionLibraryAvailable: false),
                result: -1,
                output: "MorphOS 3.20, Kickstart 50.34"),
            VersionCase("system-library-not-in-exec-list",
                new(System: true, VersionLibraryListed: false),
                result: -1,
                output: "MorphOS 3.20, Kickstart 50.34"),
            VersionCase("system-without-morphos-resident",
                new(System: true, ResidentFound: false),
                output: "Kickstart 50.34, Workbench 39.1\n"),
            VersionCase("resident-version", new(Resident: true),
                output: "dos.library 50.6\n"),
            VersionCase("resident-path-uses-filepart",
                new(Resident: true, Name: "SYS:Libs/dos.library"),
                output: "dos.library 50.6\n"),
            VersionCase("default-name-path-resolves-resident-by-filepart",
                new(Name: "SYS:Libs/dos.library"),
                output: "dos.library 50.6\n"),
            VersionCase("default-name-resolves-library-from-exec-list",
                new(Name: "SYS:Libs/test.library",
                    ResidentFound: false, ExecLibraryFound: true,
                    Full: true,
                    ExecLibraryName: "Test.Library",
                    ExecLibraryIdString:
                        "$VER: Test.Library 12.3 (07.06.2023) extra",
                    ExecLibraryVersion: 12, ExecLibraryRevision: 3),
                output: "Test.Library 12.3 (07-Jun-2023)\nextra\n"),
            VersionCase("default-name-utility-library-unavailable",
                new(Name: "missing.library", ResidentFound: false,
                    UtilityLibraryAvailable: false),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.InvalidResidentLibrary),
            VersionCase("default-name-mossys-libs-elf-preference",
                new(Name: "SYS:Libs/tool.library",
                    ResidentFound: false, Full: true,
                    RequestedVersion: 18,
                    LibraryFileHitPath: "MOSSYS:LIBS/tool.library",
                    LibraryFileContent:
                        "$VER: tool.library 18.7 (07.06.2023) extra"),
                output:
                    "tool.library 18.7 (07-Jun-2023)\nextra\n"),
            VersionCase("default-name-existing-elf-suffix-fallback",
                new(Name: "tool.elf", ResidentFound: false,
                    LibraryFileHitPath: "MOSSYS:LIBS/tool",
                    LibraryFileContent: "$VER: tool 8.3"),
                output: "tool 8.3\n"),
            VersionCase("default-name-libs-search-follows-mossys",
                new(Name: "driver.library", ResidentFound: false,
                    LibraryFileHitPath: "LIBS:driver.library.elf",
                    LibraryFileContent: "$VER: driver.library 6.1"),
                output: "driver.library 6.1\n"),
            VersionCase("default-name-library-search-md5",
                new(Name: "scsi.device", ResidentFound: false,
                    Md5: true,
                    LibraryFileHitPath:
                        "MOSSYS:LIBS/scsi.device.elf",
                    LibraryFileContent: "$VER: scsi.device 2.4"),
                output: Md5Hex("$VER: scsi.device 2.4") +
                    "  scsi.device 2.4\n"),
            VersionCase("default-name-device-list-hit",
                new(Name: "scsi.device", ResidentFound: false,
                    ExecDeviceFound: true,
                    ExecDeviceName: "scsi.device",
                    ExecDeviceVersion: 42, ExecDeviceRevision: 5),
                output: "scsi.device 42.5\n"),
            VersionCase("default-name-device-list-full-idstring",
                new(Name: "scsi.device", ResidentFound: false, Full: true,
                    ExecDeviceFound: true,
                    ExecDeviceName: "scsi.device",
                    ExecDeviceIdString:
                        "$VER: scsi.device 42.5 (07.06.2023) device extra",
                    ExecDeviceVersion: 42, ExecDeviceRevision: 5),
                output:
                    "scsi.device 42.5 (07-Jun-2023)\ndevice extra\n"),
            VersionCase("default-name-devs-search-follows-device-list",
                new(Name: "mouse.device", ResidentFound: false,
                    LibraryFileHitPath:
                        "MOSSYS:DEVS/mouse.device.elf",
                    LibraryFileContent: "$VER: mouse.device 9.2"),
                output: "mouse.device 9.2\n"),
            VersionCase("default-name-direct-file-after-device-dirs",
                new(Name: "direct-tool", ResidentFound: false,
                    DirectFileHit: true,
                    FileContent: "$VER: direct-tool 3.9"),
                output: "direct-tool 3.9\n"),
            VersionCase("default-name-segment-after-direct-file-miss",
                new(Name: "cmd-tool", ResidentFound: false,
                    CommandSegmentFound: true,
                    CommandSegmentVersionText: " cmd-tool 6.8"),
                output: "cmd-tool 6.8\n"),
            VersionCase("volume-device-handler-resident",
                new(Name: "DH0:", ResidentFound: false,
                    VolumeDeviceProcFound: true,
                    VolumeSegmentFound: true,
                    VolumeResidentFound: true,
                    UtilityLibraryAvailable: false,
                    ResidentName: "scsi.device",
                    ResidentIdString: "$VER: scsi.device 42.5"),
                output: "scsi.device 42.5\n"),
            VersionCase("volume-handler-maps-task-to-device-node",
                new(Name: "DH1:", ResidentFound: false,
                    VolumeDeviceProcFound: true,
                    VolumeDeviceIsVolume: true,
                    VolumeDeviceFound: true,
                    VolumeSegmentFound: true,
                    VolumeResidentFound: true,
                    Md5: true, RequestedVersion: 4,
                    ResidentName: "trackdisk.device",
                    ResidentIdString: "$VER: trackdisk.device 3.7"),
                result: DOS.RETURN_WARN,
                output: "<no md5sum available>             trackdisk.device 3.7\n"),
            VersionCase("volume-without-device-node-falls-back-to-segment",
                new(Name: "DH2:", ResidentFound: false,
                    VolumeDeviceProcFound: true,
                    VolumeDeviceIsVolume: true,
                    VolumeDeviceFound: false,
                    CommandSegmentFound: true,
                    CommandSegmentVersionText: " DH2: 6.8"),
                output: "DH2: 6.8\n"),
            VersionCase("volume-device-without-handler-segment-falls-back",
                new(Name: "DH3:", ResidentFound: false,
                    VolumeDeviceProcFound: true,
                    VolumeSegmentFound: false,
                    CommandSegmentFound: true,
                    CommandSegmentVersionText: " DH3: 7.1"),
                output: "DH3: 7.1\n"),
            VersionCase("volume-without-device-process-falls-back",
                new(Name: "DH5:", ResidentFound: false,
                    CommandSegmentFound: true,
                    CommandSegmentVersionText: " DH5: 8.2"),
                output: "DH5: 8.2\n"),
            VersionCase("volume-without-device-or-segment-reports-not-found",
                new(Name: "DH6:", ResidentFound: false),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("volume-handler-segment-without-resident-fails",
                new(Name: "DH4:", ResidentFound: false,
                    VolumeDeviceProcFound: true,
                    VolumeDeviceIsVolume: true,
                    VolumeDeviceFound: true,
                    VolumeSegmentFound: true,
                    VolumeResidentFound: false),
                result: DOS.RETURN_FAIL,
                output: "Could not find version information for 'DH4:'\n"),
            VersionCase("resident-full", new(Resident: true, Full: true),
                output: "dos.library 50.6\n(fixture)\n"),
            VersionCase("resident-id-string-version-is-authoritative",
                new(Resident: true, Full: true, ResidentVersion: 50,
                    ResidentIdString: "$VER: dos.library 42.7 (07.06.2023) extra"),
                output: "dos.library 42.7 (07-Jun-2023)\nextra\n"),
            VersionCase("resident-without-version-text",
                new(Resident: true, ResidentIdString:
                    "$VER: dos.library build-only"),
                output: "dos.library \n"),
            VersionCase("resident-extended-revision-allocation-fallback",
                new(Resident: true, ResidentVersion: 44,
                    ResidentFlags: 1 << 6, ResidentRevision: 9,
                    ResidentParseAllocationFailureAt: 1),
                output: "dos.library 44.9\n"),
            VersionCase("resident-classic-revision-scratch-fallback",
                new(Resident: true, ResidentVersion: 44,
                    ResidentRevision: 9,
                    ResidentParseAllocationFailureAt: 2),
                output: "dos.library 44.-1\n"),
            VersionCase("resident-version-higher-request",
                new(Resident: true, RequestedVersion: 51),
                result: DOS.RETURN_WARN,
                output: "dos.library 50.6\n"),
            VersionCase("resident-version-lower-request",
                new(Resident: true, RequestedVersion: 49),
                output: "dos.library 50.6\n"),
            VersionCase("missing-resident",
                new(Resident: true, ResidentFound: false),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("resident-command-segment-version",
                new(Resident: true, Name: "resident-tool",
                    ResidentFound: false, CommandSegmentFound: true,
                    CommandSegmentVersionText: " resident-tool 6.3"),
                output: "resident-tool 6.3\n"),
            VersionCase("resident-command-segment-system-list-version",
                new(Resident: true, Name: "resident-tool",
                    ResidentFound: false, CommandSegmentFound: true,
                    CommandSegmentSystem: 1,
                    CommandSegmentVersionText: " resident-tool 6.4"),
                output: "resident-tool 6.4\n"),
            VersionCase("internal-command-segment-uses-shellcmd-resident",
                new(Resident: true, Name: "internal-tool",
                    ResidentFound: false, CommandSegmentFound: true,
                    CommandSegmentUseCount: -2,
                    ShellCommandResidentFound: true,
                    ResidentName: "shellcmd",
                    ResidentIdString: "$VER: shellcmd 50.6 (fixture)"),
                output: "shellcmd 50.6\n"),
            VersionCase("disabled-command-segment-uses-shellcmd-resident",
                new(Resident: true, Name: "disabled-tool",
                    ResidentFound: false, CommandSegmentFound: true,
                    CommandSegmentUseCount: -999,
                    ShellCommandResidentFound: true,
                    ResidentName: "shellcmd",
                    ResidentIdString: "$VER: shellcmd 50.6 (fixture)"),
                output: "shellcmd 50.6\n"),
            VersionCase("file-version", new(File: true,
                FileContent: "$VER: file-tool 12.34\n"),
                output: "file-tool 12.34\n"),
            VersionCase("file-version-multiple-names", new(File: true,
                Names: ["first.bin", "second.bin"],
                FileContent: "$VER: file-tool 12.34\n"),
                output: "file-tool 12.34\nfile-tool 12.34\n"),
            VersionCase("file-version-full-date", new(File: true, Full: true,
                FileContent: "$VER: dated-tool 12.34 (07.06.2023) extra\n"),
                output: "dated-tool 12.34 (07-Jun-2023)\nextra\n"),
            VersionCase("file-version-full-without-date",
                new(File: true, Full: true,
                    FileContent: "$VER: nodate-tool 12.34 extra\n"),
                output: "nodate-tool 12.34 extra\n"),
            VersionCase("file-md5sum", new(File: true, Md5: true,
                FileContent: "$VER: md5-tool 12.34\n"),
                output: Md5Hex("$VER: md5-tool 12.34\n") +
                    "  md5-tool 12.34\n"),
            VersionCase("file-md5sum-full-date", new(File: true,
                Md5: true, Full: true,
                FileContent: "$VER: dated-md5 1.2 (01.01.2024) extra\n"),
                output: Md5Hex(
                    "$VER: dated-md5 1.2 (01.01.2024) extra\n") +
                    "  dated-md5 1.2 (07-Jun-2023) extra\n"),
            VersionCase("file-md5sum-multiple-blocks",
                new(File: true, Md5: true,
                    FileContent: new string('x', 16_382) +
                        "$VER: splitmd5 3.7\n" + new string('y', 700)),
                output: Md5Hex(new string('x', 16_382) +
                    "$VER: splitmd5 3.7\n" + new string('y', 700)) +
                    "  splitmd5 3.7\n"),
            VersionCase("file-md5sum-failure-after-version",
                new(File: true, Md5: true,
                    FileReadFailureAt: 2),
                result: DOS.RETURN_FAIL, error: 205),
            VersionCase("file-md5sum-ctrl-c",
                new(File: true, Md5: true, CtrlC: true),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.Break),
            VersionCase("file-md5sum-allocation-failure",
                new(File: true, Md5: true, AllocationFailureAt: 3),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.NoFreeStore),
            VersionCase("system-md5sum-unavailable",
                new(System: true, Md5: true),
                output: "<no md5sum available>             " + systemVersion),
            VersionCase("resident-md5sum-unavailable",
                new(Resident: true, Md5: true),
                output: "<no md5sum available>             dos.library 50.6\n"),
            VersionCase("file-version-mismatch", new(File: true,
                RequestedVersion: 99,
                FileContent: "$VER: file-tool 12.34\n"),
                result: DOS.RETURN_WARN,
                output: "file-tool 12.34\n"),
            VersionCase("file-version-cross-read-boundary", new(File: true,
                FileContent: new string('x', 16_382) +
                    "$VER: split-tool 3.7\n"),
                output: "split-tool 3.7\n"),
            VersionCase("file-morphos-v0-seek-to-end", new(File: true,
                FileContent: "\u007fMOS\0\0" + new string('x', 16_380) +
                    "$VER: seek-tool 4.2\n"),
                output: "seek-tool 4.2\n"),
            VersionCase("file-no-version-tag", new(File: true,
                FileContent: "plain binary content\n"),
                result: DOS.RETURN_ERROR,
                output: "Could not find version information for 'dos.library'\n"),
            VersionCase("file-no-version-tag-md5-suppresses-diagnostic",
                new(File: true, Md5: true,
                    FileContent: "plain binary content\n"),
                result: DOS.RETURN_ERROR),
            VersionCase("file-loadseg-resident-version", new(File: true,
                Full: true,
                FileContent: "plain executable content\n",
                FileIsFileSystem: true, FileLoadSegSuccess: true,
                FileLoadedResidentName: "resident-file-tool",
                FileLoadedResidentIdString:
                    "$VER: resident-file-tool 22.7 (07.06.2023) extra"),
                output: "resident-file-tool 22.7 (07-Jun-2023)\nextra\n"),
            VersionCase("file-loadseg-resident-md5", new(File: true,
                Md5: true, FileContent: "plain executable content\n",
                FileIsFileSystem: true, FileLoadSegSuccess: true,
                FileLoadedResidentName: "resident-file-tool",
                FileLoadedResidentIdString:
                    "$VER: resident-file-tool 22.7"),
                output: Md5Hex("plain executable content\n") +
                    "  resident-file-tool 22.7\n"),
            VersionCase("file-loadseg-without-resident", new(File: true,
                FileContent: "plain executable content\n",
                FileIsFileSystem: true, FileLoadSegSuccess: true),
                result: DOS.RETURN_ERROR,
                output: "Could not find version information for 'dos.library'\n"),
            VersionCase("file-open-failure", new(File: true,
                FileOpenError: (int)DOS.Error.ObjectNotFound),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("file-read-failure", new(File: true,
                FileReadFailure: true), result: DOS.RETURN_FAIL,
                error: 205),
            VersionCase("file-allocation-failure", new(File: true,
                AllocationFailureAt: 2), result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.NoFreeStore),
            VersionCase("file-ctrl-c", new(File: true, CtrlC: true),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.Break),
            VersionCase("default-name-md5-unavailable-for-resident",
                new(Md5: true),
                output: "<no md5sum available>             dos.library 50.6\n"),
            VersionCase("parser-failure", new(ParserError: 116),
                result: DOS.RETURN_ERROR, error: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Version = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Version = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Version = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { Version = new(System: true), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            VersionCase("interleaved-system", new(System: true),
                output: systemVersion),
            VersionCase("interleaved-resident", new(Resident: true),
                output: "dos.library 50.6\n")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private List<object> RunWorkbench31VersionEntryCases()
    {
        const string systemVersion = "Kickstart 50.34, Workbench 39.1\n";
        ProbeCase[] cases =
        [
            VersionCase("system-version", new(System: true),
                output: systemVersion),
            VersionCase("system-version-mismatch",
                new(System: true, RequestedVersion: 99),
                result: DOS.RETURN_WARN, output: systemVersion),
            VersionCase("system-lower-major",
                new(System: true, RequestedVersion: 38),
                output: systemVersion),
            VersionCase("system-equal-version-revision",
                new(System: true, RequestedVersion: 39,
                    RequestedRevision: 1), output: systemVersion),
            VersionCase("system-equal-major-lower-revision",
                new(System: true, RequestedVersion: 39,
                    RequestedRevision: 0), output: systemVersion),
            VersionCase("system-equal-major-higher-revision",
                new(System: true, RequestedVersion: 39,
                    RequestedRevision: 2),
                result: DOS.RETURN_WARN, output: systemVersion),
            VersionCase("system-higher-major-ignores-revision",
                new(System: true, RequestedVersion: 38,
                    RequestedRevision: int.MaxValue), output: systemVersion),
            VersionCase("system-lower-major-cannot-be-rescued-by-revision",
                new(System: true, RequestedVersion: 40,
                    RequestedRevision: -1),
                result: DOS.RETURN_WARN, output: systemVersion),
            VersionCase("system-revision-only-lower",
                new(System: true, RequestedRevision: 0), output: systemVersion),
            VersionCase("system-revision-only-equal",
                new(System: true, RequestedRevision: 1), output: systemVersion),
            VersionCase("system-revision-only-higher",
                new(System: true, RequestedRevision: 2),
                result: DOS.RETURN_WARN, output: systemVersion),
            VersionCase("system-signed-negative-major",
                new(System: true, RequestedVersion: int.MinValue,
                    RequestedRevision: int.MaxValue), output: systemVersion),
            VersionCase("system-signed-negative-revision",
                new(System: true, RequestedVersion: 39,
                    RequestedRevision: int.MinValue), output: systemVersion),
            VersionCase("resident-version", new(Resident: true),
                output: "dos.library 50.6\n"),
            VersionCase("resident-original-values-min39",
                new(Resident: true, ResidentVersion: 40,
                    ResidentIdString: "$VER: dos.library 40.3\n",
                    RequestedVersion: 39), output: "dos.library 40.3\n"),
            VersionCase("resident-original-values-min999",
                new(Resident: true, ResidentVersion: 40,
                    ResidentIdString: "$VER: dos.library 40.3\n",
                    RequestedVersion: 999),
                result: DOS.RETURN_WARN, output: "dos.library 40.3\n"),
            VersionCase("resident-equal-major-higher-revision",
                new(Resident: true, RequestedVersion: 50,
                    RequestedRevision: 7),
                result: DOS.RETURN_WARN, output: "dos.library 50.6\n"),
            VersionCase("resident-revision-only-higher",
                new(Resident: true, RequestedRevision: 7),
                result: DOS.RETURN_WARN, output: "dos.library 50.6\n"),
            VersionCase("resident-full", new(Resident: true, Full: true,
                    ResidentIdString: "$VER: dos.library 50.6 (1.4.93)"),
                output: "dos.library 50.6 (04/01/93)\n"),
            VersionCase("resident-full-warning-retains-output",
                new(Resident: true, Full: true, RequestedVersion: 99,
                    ResidentIdString: "$VER: dos.library 50.6 (1.4.93)"),
                result: DOS.RETURN_WARN, output: "dos.library 50.6 (04/01/93)\n"),
            VersionCase("full-numeric-date-owned-extra", new(Full: true,
                    ResidentIdString: "$VER: dos.library 50.6 (1.4.93) note\rignored",
                    WorkbenchFullExtra: "note"),
                output: "dos.library 50.6 (04/01/93)\nnote\n"),
            VersionCase("full-fallback-date-retains-parenthesis", new(Full: true,
                    ResidentIdString: "$VER: dos.library 50.6 (04-Jan-93) note",
                    WorkbenchFullExtra: "(04-Jan-93) note", WorkbenchDate2AmigaExpected: 0,
                    WorkbenchDateParseExpected: 1),
                output: "dos.library 50.6 (01/04/93)\n(04-Jan-93) note\n"),
            VersionCase("full-invalid-numeric-date-keeps-extra", new(Full: true,
                    ResidentIdString: "$VER: dos.library 50.6 (4.13.93) note",
                    WorkbenchFullExtra: "note", WorkbenchDateParseExpected: 1,
                    WorkbenchDateFormatExpected: 0),
                output: "dos.library 50.6\nnote\n"),
            VersionCase("full-no-dot-extra", new(Full: true,
                    ResidentIdString: "$VER: dos.library 50 note",
                    WorkbenchFullExtra: "note", WorkbenchDate2AmigaExpected: 0,
                    WorkbenchDateFormatExpected: 0),
                output: "dos.library 50.-1\nnote\n"),
            VersionCase("full-extra-allocation-failure", new(Full: true,
                    ResidentIdString: "$VER: dos.library 50.6 (4.1.93) note",
                    WorkbenchFullExtra: "note", WorkbenchExtraAllocationFailure: true,
                    WorkbenchDateFormatExpected: 0),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.NoFreeStore),
            VersionCase("full-signed-date-seconds", new(Full: true,
                    ResidentIdString: "$VER: dos.library 50.6 (4.1.93)",
                    WorkbenchDateSeconds: uint.MaxValue),
                output: "dos.library 50.6 (01/01/78)\n"),
            VersionCase("full-null-id", new(Full: true, ResidentNullIdString: true,
                    WorkbenchDate2AmigaExpected: 0, WorkbenchDateFormatExpected: 0),
                output: "dos.library 50.-1\n"),
            VersionCase("missing-resident",
                new(Resident: true, ResidentFound: false),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("default-resident-first-provider", new(),
                output: "dos.library 50.6\n"),
            VersionCase("default-command-segment-first-list",
                new(Name: "segment-tool", ResidentFound: false,
                    CommandSegmentFound: true,
                    CommandSegmentVersionText: " segment-tool 6.8"),
                output: "segment-tool 6.8\n"),
            VersionCase("default-command-segment-system-list",
                new(Name: "system-segment-tool", ResidentFound: false,
                    CommandSegmentFound: true, CommandSegmentSystem: 1,
                    CommandSegmentVersionText: " system-segment-tool 7.4"),
                output: "system-segment-tool 7.4\n"),
            VersionCase("default-command-segment-full-date-extra",
                new(Name: "full-segment-tool", ResidentFound: false,
                    CommandSegmentFound: true, Full: true,
                    CommandSegmentVersionText:
                        " full-segment-tool 6.8 (1.4.93) extra",
                    WorkbenchFullExtra: "extra"),
                output: "full-segment-tool 6.8 (04/01/93)\nextra\n"),
            VersionCase("internal-command-segment-uses-shell-resident",
                new(Name: "internal-tool", ResidentFound: false,
                    CommandSegmentFound: true, CommandSegmentUseCount: -2,
                    ShellCommandResidentFound: true, ResidentName: "shell",
                    ResidentVersion: 40,
                    ResidentIdString: "$VER: shell 40.6"),
                output: "shell 40.6\n"),
            VersionCase("disabled-command-segment-uses-shell-resident",
                new(Name: "disabled-tool", ResidentFound: false,
                    CommandSegmentFound: true, CommandSegmentUseCount: -999,
                    ShellCommandResidentFound: true, ResidentName: "shell",
                    ResidentVersion: 40,
                    ResidentIdString: "$VER: shell 40.6"),
                output: "shell 40.6\n"),
            VersionCase("trailing-colon-device-handler-resident",
                new(Name: "DF0:", ResidentFound: false,
                    WorkbenchDeviceListLookup: true,
                    ResidentName: "filesystem", ResidentVersion: 40,
                    ResidentIdString: "$VER: filesystem 40.1 (fixture)"),
                output: "filesystem 40.1\n"),
            VersionCase("res-trailing-colon-device-handler-resident",
                new(Name: "DF0:", Resident: true, ResidentFound: false,
                    WorkbenchDeviceListLookup: true,
                    ResidentName: "filesystem", ResidentVersion: 40,
                    ResidentIdString: "$VER: filesystem 40.1 (fixture)"),
                output: "filesystem 40.1\n"),
            VersionCase("trailing-colon-device-handler-full",
                new(Name: "DF0:", ResidentFound: false, Full: true,
                    WorkbenchDeviceListLookup: true,
                    ResidentName: "filesystem", ResidentVersion: 40,
                    ResidentIdString:
                        "$VER: filesystem 40.1 (1.4.93) handler",
                    WorkbenchFullExtra: "handler"),
                output: "filesystem 40.1 (04/01/93)\nhandler\n"),
            VersionCase("trailing-colon-device-entry-without-startup",
                new(Name: "DF0:", ResidentFound: false,
                    WorkbenchDeviceListLookup: true,
                    WorkbenchDeviceListHasStartup: false),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("trailing-colon-device-without-segment-list",
                new(Name: "DF0:", ResidentFound: false,
                    WorkbenchDeviceListLookup: true,
                    WorkbenchDeviceListSegmentFound: false),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("trailing-colon-device-handler-without-resident",
                new(Name: "DF0:", ResidentFound: false,
                    WorkbenchDeviceListLookup: true,
                    WorkbenchDeviceListResidentFound: false),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("trailing-colon-name-not-in-device-list",
                new(Name: "DF0:", ResidentFound: false,
                    WorkbenchDeviceListLookup: true,
                    WorkbenchDeviceListEntryFound: false),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("default-uppercase-canonical-name",
                new(Name: "DOS.LIBRARY"), output: "dos.library 50.6\n"),
            VersionCase("res-uppercase-canonical-name",
                new(Name: "DOS.LIBRARY", Resident: true), output: "dos.library 50.6\n"),
            VersionCase("resident-canonical-case-is-retained",
                new(ResidentName: "DoS.Library"), output: "DoS.Library 50.6\n"),
            VersionCase("complete-name-does-not-use-basename",
                new(Name: "LIBS:dos.library"),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.ObjectNotFound),
            VersionCase("resident-high-bit-continuation",
                new(ResidentTableContinuation: true, ResidentTableDecoy: true),
                output: "dos.library 50.6\n"),
            VersionCase("stricmp-tests-entire-long",
                new(ResidentTableDecoy: true, ResidentComparisonNonzeroLowWordZero: true),
                output: "dos.library 50.6\n"),
            VersionCase("long-resident-name-is-not-truncated",
                new(Name: new string('a', 260) + ".library",
                    ResidentName: new string('a', 260) + ".library"),
                output: new string('a', 260) + ".library 50.6\n"),
            VersionCase("resident-byte-major-is-authoritative",
                new(ResidentVersion: 255, ResidentIdString: "$VER: dos.library 1.260"),
                output: "dos.library 255.260\n"),
            VersionCase("resident-missing-id-unknown-revision",
                new(ResidentVersion: 40, ResidentNullIdString: true),
                output: "dos.library 40.-1\n"),
            VersionCase("resident-invalid-revision-is-minus-one",
                new(ResidentVersion: 40, ResidentIdString: "$VER: dos.library 40.x"),
                output: "dos.library 40.-1\n"),
            VersionCase("resident-signed-revision",
                new(ResidentVersion: 40, ResidentIdString: "$VER: dos.library 40.-2"),
                output: "dos.library 40.-2\n"),
            VersionCase("resident-skip-nonnumeric-name-tokens",
                new(ResidentVersion: 40, ResidentIdString: "$VER: dos.library build9 40.3"),
                output: "dos.library 40.3\n"),
            VersionCase("resident-space-before-dot",
                new(ResidentVersion: 40, ResidentIdString: "$VER: dos.library 40 .3"),
                output: "dos.library 40.3\n"),
            VersionCase("default-liblist-basename-from-path",
                new(Name: "LIBS:test.library", ResidentFound: false,
                    ExecLibraryFound: true), output: "test.library 12.3\n"),
            VersionCase("default-liblist-full-uses-library-idstring",
                new(Name: "LIBS:test.library", ResidentFound: false,
                    ExecLibraryFound: true, Full: true,
                    ExecLibraryIdString:
                        "$VER: test.library 12.3 (07.06.93) extra",
                    WorkbenchFullExtra: "extra"),
                output: "test.library 12.3 (06/07/93)\nextra\n"),
            VersionCase("default-devicelist-basename-from-path",
                new(Name: "DEVS:test.device", ResidentFound: false,
                    ExecDeviceFound: true), output: "test.device 14.2\n"),
            VersionCase("default-devicelist-full-uses-device-idstring",
                new(Name: "DEVS:test.device", ResidentFound: false,
                    ExecDeviceFound: true, Full: true,
                    ExecDeviceIdString:
                        "$VER: test.device 14.2 (08.06.93) device",
                    WorkbenchFullExtra: "device"),
                output: "test.device 14.2 (06/08/93)\ndevice\n"),
            VersionCase("default-direct-file-path-version-library",
                new(Name: "LIBS:version.library", ResidentFound: false,
                    DirectFileHit: true,
                    FileContent: "$VER: version.library 40.42"),
                output: "version.library 40.42\n"),
            VersionCase("default-libs-basename-file-provider",
                new(Name: "version.library", ResidentFound: false,
                    WorkbenchFileHitPath: "LIBS:version.library",
                    FileContent: "$VER: version.library 40.42"),
                output: "version.library 40.42\n"),
            VersionCase("default-devs-basename-file-provider",
                new(Name: "fallback.device", ResidentFound: false,
                    WorkbenchFileHitPath: "DEVS:fallback.device",
                    FileContent: "$VER: fallback.device 8.3"),
                output: "fallback.device 8.3\n"),
            VersionCase("default-liblist-miss-keeps-provider-order",
                new(Name: "LIBS:missing.library", ResidentFound: false,
                    ExecLibraryFound: true),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("default-bare-name-provider-miss",
                new(Name: "missing-device-copper-test.device",
                    ResidentFound: false),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("utility-library-unavailable",
                new(UtilityLibraryAvailable: false),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.InvalidResidentLibrary),
            VersionCase("canonical-name-allocation-failure",
                new(ResidentCopyAllocationFailure: true),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.NoFreeStore),
            VersionCase("system-version-file-switch",
                new(System: true, File: true), output: systemVersion),
            VersionCase("file-direct-version-tag",
                new(Name: "C:Avail", File: true,
                    FileContent: "$VER: avail 40.1 (9.2.93)\n"),
                output: "avail 40.1\n"),
            VersionCase("file-direct-full-date-extra",
                new(Name: "C:Avail", File: true, Full: true,
                    FileContent: "$VER: avail 40.1 (9.2.93) extra\n",
                    WorkbenchFullExtra: "extra"),
                output: "avail 40.1 (02/09/93)\nextra\n"),
            VersionCase("file-direct-version-warning-after-output",
                new(Name: "C:Avail", File: true, RequestedVersion: 999,
                    FileContent: "$VER: avail 40.1 (9.2.93)\n"),
                result: DOS.RETURN_WARN, output: "avail 40.1\n"),
            VersionCase("file-switch-restricts-to-direct-path",
                new(Name: "exec.library", File: true,
                    FileOpenError: (int)DOS.Error.ObjectNotFound),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("file-switch-does-not-query-trailing-colon-device",
                new(Name: "DF0:", File: true, ResidentFound: false,
                    FileOpenError: (int)DOS.Error.ObjectNotFound),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("file-switch-with-res-still-uses-file",
                new(Name: "C:Avail", File: true, Resident: true,
                    FileContent: "$VER: avail 40.1\n"),
                output: "avail 40.1\n"),
            VersionCase("file-open-failure",
                new(Name: "C:MissingVersionFile", File: true,
                    FileOpenError: (int)DOS.Error.ObjectNotFound),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.ObjectNotFound),
            VersionCase("file-read-failure",
                new(Name: "C:BrokenVersionFile", File: true,
                    FileContent: "$VER: avail 40.1\n",
                    FileReadFailure: true), result: DOS.RETURN_FAIL,
                error: 205),
            VersionCase("file-without-tag-diagnostic",
                new(Name: "DEVS:system-configuration", File: true,
                    FileContent: "plain system data\n"),
                result: DOS.RETURN_FAIL,
                output: "Could not find version information for 'DEVS:system-configuration'\n"),
            VersionCase("file-hunk-loadseg-resident",
                new(Name: "DEVS:clipboard.device", File: true,
                    FileContent: "\0\0\u0003\u00f3plain HUNK binary\n",
                    FileLoadSegSuccess: true,
                    FileLoadedResidentName: "clipboard.device",
                    FileLoadedResidentVersion: 38,
                    FileLoadedResidentIdString:
                        "$VER: clipboard.device 38.8"),
                output: "clipboard.device 38.8\n"),
            VersionCase("file-hunk-loadseg-linked-resident",
                new(Name: "DEVS:clipboard.device", File: true,
                    FileContent: "\0\0\u0003\u00f3plain HUNK binary\n",
                    FileLoadSegSuccess: true,
                    FileLoadedResidentName: "clipboard.device",
                    FileLoadedResidentVersion: 38,
                    FileLoadedResidentIdString:
                        "$VER: clipboard.device 38.8",
                    FileLoadedResidentInSecondSegment: true),
                output: "clipboard.device 38.8\n"),
            VersionCase("file-hunk-loadseg-without-resident",
                new(Name: "DEVS:clipboard.device", File: true,
                    FileContent: "\0\0\u0003\u00f3plain HUNK binary\n",
                    FileLoadSegSuccess: true),
                result: DOS.RETURN_FAIL,
                output: "Could not find version information for 'DEVS:clipboard.device'\n"),
            VersionCase("file-buffer-allocation-failure",
                new(Name: "C:Avail", File: true,
                    FileContent: "$VER: avail 40.1\n",
                    AllocationFailureAt: 1),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.NoFreeStore),
            VersionCase("file-scan-buffer-allocation-failure",
                new(Name: "C:Avail", File: true,
                    FileContent: "$VER: avail 40.1\n",
                    AllocationFailureAt: 2),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.NoFreeStore),
            VersionCase("file-tag-crosses-read-boundary",
                new(Name: "C:LargeVersionFile", File: true,
                    FileContent: new string('x', 16_383) +
                        "$VER: avail 40.1\n"),
                output: "avail 40.1\n"),
            VersionCase("unit-does-not-change-original-search", new(Unit: true),
                output: "dos.library 50.6\n"),
            VersionCase("internal-does-not-change-original-search", new(Internal: true),
                output: "dos.library 50.6\n"),
            VersionCase("parser-failure", new(ParserError: 116),
                result: DOS.RETURN_FAIL, error: 116),
            VersionCase("numeric-parser-failure", new(ParserError: 115),
                result: DOS.RETURN_FAIL, error: 115),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Version = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Version = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Version = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { Version = new(System: true), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            VersionCase("interleaved-system", new(System: true),
                output: systemVersion),
            VersionCase("interleaved-resident", new(Resident: true),
                output: "dos.library 50.6\n")
        ], true));
        reports.AddRange(Execute([
            VersionCase("interleaved-named-uppercase", new(Name: "DOS.LIBRARY"),
                output: "dos.library 50.6\n"),
            VersionCase("interleaved-named-minimum-warning", new(RequestedVersion: 99),
                result: DOS.RETURN_WARN, output: "dos.library 50.6\n")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase VersionCase(string name,
        VersionEntryCase definition, int result = DOS.RETURN_OK,
        int error = 0, string output = "")
    {
        var names = definition.Names ?? [definition.Name];
        var args = definition.System ? "" : string.Join(" ", names) +
            (definition.Resident ? " RES" : definition.File ? " FILE" : "");
        if (definition.Md5) args += " MD5SUM";
        if (definition.Full) args += " FULL";
        if (definition.Unit) args += " UNIT=1";
        if (definition.Internal) args += " INTERNAL";
        if (definition.RequestedVersion is int version)
            args += $" VERSION={version}";
        if (definition.RequestedRevision is int revision)
            args += $" REVISION={revision}";
        return new(name, args, result, error, output)
        { Version = definition };
    }

    private static string Md5Hex(string value) =>
        Convert.ToHexString(System.Security.Cryptography.MD5.HashData(
            Encoding.Latin1.GetBytes(value)));

    private static uint FilePartLength(string value)
    {
        var separator = Math.Max(value.LastIndexOf(':'),
            Math.Max(value.LastIndexOf('/'), value.LastIndexOf('\\')));
        return unchecked((uint)(value.Length - separator - 1));
    }

    private static string[] MorphOSDirectoryCandidatePaths(string value,
        bool includeDeviceDirectories)
    {
        var separator = Math.Max(value.LastIndexOf(':'),
            Math.Max(value.LastIndexOf('/'), value.LastIndexOf('\\')));
        var name = value[(separator + 1)..];
        var hasElfSuffix = name.EndsWith(".elf", StringComparison.Ordinal);
        var suffixed = hasElfSuffix ? name : name + ".elf";
        var bare = hasElfSuffix ? name[..^4] : name;
        var libraryPaths = new[]
        {
            "MOSSYS:LIBS/" + suffixed,
            "MOSSYS:LIBS/" + bare,
            "LIBS:" + suffixed,
            "LIBS:" + bare
        };
        if (!includeDeviceDirectories) return libraryPaths;
        return [.. libraryPaths,
            "MOSSYS:DEVS/" + suffixed,
            "MOSSYS:DEVS/" + bare,
            "DEVS:" + suffixed,
            "DEVS:" + bare];
    }

    private void PrepareVersionEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Version ?? new();
        if (!definition.System && !definition.ResidentFound)
        {
            InitializeEmptyExecList(0x4000u +
                (uint)ExecLayout.ExecBase.LibraryList);
            InitializeEmptyExecList(0x4000u +
                (uint)ExecLayout.ExecBase.DeviceList);
        }
        var names = invocation.Arguments + 0x300;
        var workbench = IsWorkbench31VersionEntrySuite(suite);
        var name = invocation.Arguments + (workbench ? 0xa00u : 0x340u);
        var numericVersion = invocation.Arguments + 0x500;
        var numericRevision = invocation.Arguments + 0x504;
        invocation.VersionLayout = new(names, name, numericVersion,
            numericRevision);
        invocation.VersionLayout.RexxLibraryBase = VersionRexxLibraryBase;
        invocation.VersionLayout.AmbientReplyPort = invocation.Arguments + 0x800;
        invocation.VersionLayout.AmbientHostMessage = 0;
        invocation.VersionLayout.AmbientCommandArgString = invocation.Arguments + 0x8c0;
        invocation.VersionLayout.AmbientPort = invocation.Arguments + 0x940;
        invocation.VersionLayout.AmbientResultText = invocation.Arguments + 0x980;
        invocation.VersionLayout.AmbientResultArgString =
            definition.AmbientRexxResult is null ? 0u :
                invocation.VersionLayout.AmbientResultText;
        if (definition.VolumeDeviceProcFound ||
            definition.WorkbenchDeviceListLookup)
        {
            var layout = invocation.VersionLayout;
            layout.VolumeDeviceProc = invocation.Arguments + 0x1600;
            layout.VolumeLookupNode = invocation.Arguments + 0x1640;
            layout.VolumeListCursor = invocation.Arguments + 0x1680;
            layout.VolumePriorDeviceNode = invocation.Arguments + 0x16c0;
            layout.VolumeDeviceNode = invocation.Arguments + 0x1700;
            layout.VolumeSegmentAddress = invocation.Arguments + 0x1800;
            Bus.Memory.AsSpan(checked((int)layout.VolumeDeviceProc), 0x520)
                .Clear();
            Bus.Long(layout.VolumeDeviceProc +
                    (uint)DosLayout.DevProc.DeviceNode,
                layout.VolumeLookupNode);
            Bus.Long(layout.VolumeLookupNode +
                    (uint)DosLayout.DosList.Type,
                definition.VolumeDeviceIsVolume
                    ? (uint)DosListType.Volume
                    : (uint)DosListType.Device);
            Bus.Long(layout.VolumeLookupNode +
                    (uint)DosLayout.DosList.Task,
                definition.VolumeTask);

            if (definition.WorkbenchDeviceListLookup)
            {
                Bus.Long(layout.VolumeDeviceNode +
                        (uint)DosLayout.DosList.Type,
                    (uint)DosListType.Device);
                Bus.Long(layout.VolumeDeviceNode +
                        (uint)DosLayout.DosList.Task,
                    definition.VolumeTask);
                Bus.Long(layout.VolumeDeviceNode +
                        (uint)DosLayout.DeviceNode.Startup,
                    definition.WorkbenchDeviceListHasStartup ? 1u : 0u);
            }

            if (definition.VolumeDeviceIsVolume)
            {
                Bus.Long(layout.VolumePriorDeviceNode +
                        (uint)DosLayout.DosList.Next,
                    definition.VolumeDeviceFound
                        ? layout.VolumeDeviceNode : 0);
                Bus.Long(layout.VolumePriorDeviceNode +
                        (uint)DosLayout.DosList.Task,
                    definition.VolumeTask ^ 1u);
                if (definition.VolumeDeviceFound)
                {
                    Bus.Long(layout.VolumeDeviceNode +
                            (uint)DosLayout.DosList.Type,
                        (uint)DosListType.Device);
                    Bus.Long(layout.VolumeDeviceNode +
                            (uint)DosLayout.DosList.Task,
                        definition.VolumeTask);
                }
            }

            var segmentFound = definition.VolumeDeviceProcFound
                ? definition.VolumeSegmentFound
                : definition.WorkbenchDeviceListLookup &&
                    definition.WorkbenchDeviceListSegmentFound;
            var residentFound = definition.VolumeDeviceProcFound
                ? definition.VolumeResidentFound
                : definition.WorkbenchDeviceListLookup &&
                    definition.WorkbenchDeviceListResidentFound;
            if (segmentFound)
            {
                var segment = layout.VolumeSegmentAddress;
                Bus.Memory.AsSpan(checked((int)(segment - 4)), 132).Clear();
                Bus.Long(segment - 4, 128);
                Bus.Long(layout.VolumeLookupNode +
                        (uint)DosLayout.DosList.Misc,
                    definition.VolumeDeviceIsVolume
                        ? 0u : segment >> 2);
                if (definition.VolumeDeviceIsVolume &&
                    definition.VolumeDeviceFound)
                    Bus.Long(layout.VolumeDeviceNode +
                        (uint)DosLayout.DosList.Misc,
                        segment >> 2);

                if (definition.WorkbenchDeviceListLookup)
                    Bus.Long(layout.VolumeDeviceNode +
                            (uint)DosLayout.DeviceNode.SegmentList,
                        segment >> 2);

                if (residentFound)
                {
                    var resident = segment + 4;
                    var volumeResidentName = invocation.Arguments + 0x1880;
                    var volumeResidentId = invocation.Arguments + 0x18c0;
                    WriteCString(volumeResidentName,
                        definition.ResidentName);
                    WriteCString(volumeResidentId,
                        definition.ResidentIdString ??
                        "$VER: dos.library 50.6 (fixture)");
                    Bus.Word(resident +
                            (uint)ExecLayout.Resident.MatchWord,
                        0x4afc);
                    Bus.Long(resident +
                            (uint)ExecLayout.Resident.MatchTag,
                        resident);
                    Bus.Memory[resident +
                        (uint)ExecLayout.Resident.Version] =
                            unchecked((byte)definition.ResidentVersion);
                    Bus.Long(resident +
                            (uint)ExecLayout.Resident.Name,
                        volumeResidentName);
                    Bus.Long(resident +
                            (uint)ExecLayout.Resident.IdString,
                        volumeResidentId);
                    layout.VolumeResident = resident;
                }
            }
        }
        WriteCString(invocation.VersionLayout.AmbientCommandArgString,
            "VERSION");
        if (definition.AmbientRexxResult is { } ambientResult)
            WriteCString(invocation.VersionLayout.AmbientResultText,
                ambientResult);
        if (definition.AmbientLoadSegSuccess ||
            definition.FileLoadSegSuccess)
        {
            var segmentAddress = invocation.Arguments + 0xb00;
            var hasSecondSegment = definition.FileLoadSegSuccess
                ? definition.FileLoadedResidentInSecondSegment
                : definition.AmbientLoadedResidentInSecondSegment;
            // Keep the linked fixture segment clear of the ordinary
            // Workbench Resident name at arguments + 0xc00.
            var secondSegmentAddress = invocation.Arguments + 0xf00;
            Bus.Memory.AsSpan(checked((int)(segmentAddress - 4)), 132).Clear();
            Bus.Long(segmentAddress - 4, 128);
            Bus.Long(segmentAddress, hasSecondSegment
                ? secondSegmentAddress >> 2 : 0);
            if (hasSecondSegment)
            {
                Bus.Memory.AsSpan(checked((int)(secondSegmentAddress - 4)),
                    132).Clear();
                Bus.Long(secondSegmentAddress - 4, 128);
                Bus.Long(secondSegmentAddress, 0);
                invocation.VersionLayout.AmbientSegmentSecondAddress =
                    secondSegmentAddress;
                invocation.VersionLayout.AmbientSegmentSecondRaw =
                    secondSegmentAddress >> 2;
            }
            invocation.VersionLayout.AmbientSegmentAddress = segmentAddress;
            invocation.VersionLayout.AmbientSegmentRaw = segmentAddress >> 2;
            var loadedName = definition.AmbientLoadedResidentName ??
                definition.FileLoadedResidentName;
            var loadedId = definition.AmbientLoadedResidentIdString ??
                definition.FileLoadedResidentIdString;
            if (loadedName is not null && loadedId is not null)
            {
                var loadedNameAddress = invocation.Arguments + 0xa20;
                var loadedIdAddress = invocation.Arguments + 0xa60;
                var residentSegmentAddress = hasSecondSegment
                    ? secondSegmentAddress : segmentAddress;
                var residentAddress = residentSegmentAddress + 4;
                WriteCString(loadedNameAddress, loadedName);
                WriteCString(loadedIdAddress, loadedId);
                Bus.Word(residentAddress +
                    (uint)ExecLayout.Resident.MatchWord,
                    0x4afc);
                Bus.Long(residentAddress +
                    (uint)ExecLayout.Resident.MatchTag, residentAddress);
                Bus.Long(residentAddress +
                    (uint)ExecLayout.Resident.Name, loadedNameAddress);
                Bus.Memory[checked((int)(residentAddress +
                    (uint)ExecLayout.Resident.Version))] =
                    definition.FileLoadSegSuccess
                        ? unchecked((byte)definition.FileLoadedResidentVersion)
                        : unchecked((byte)definition.ResidentVersion);
                Bus.Long(residentAddress +
                    (uint)ExecLayout.Resident.IdString, loadedIdAddress);
                invocation.VersionLayout.AmbientSegmentResident = residentAddress;
                invocation.VersionLayout.AmbientResidentName = loadedNameAddress;
                invocation.VersionLayout.AmbientResidentIdString = loadedIdAddress;
                var tail = FindResidentTail(loadedId);
                invocation.VersionLayout.AmbientResidentTextBytes = unchecked(
                    (uint)(Encoding.Latin1.GetByteCount(loadedName) +
                        Encoding.Latin1.GetByteCount(tail) + 1));
            }
        }
        if (definition.CommandSegmentFound)
        {
            var segmentNode = invocation.Arguments + 0x1a00;
            var segmentAddress = invocation.Arguments + 0x1b00;
            Bus.Memory.AsSpan(checked((int)(segmentAddress - 4)), 520).Clear();
            Bus.Long(segmentAddress - 4, 128);
            Bus.Long(segmentAddress, 0);
            if (definition.CommandSegmentVersionText is { } versionText)
                WriteCString(segmentAddress + 4, "$VER:" + versionText);
            Bus.Long(segmentNode + (uint)DosLayout.Segment.UseCount,
                unchecked((uint)definition.CommandSegmentUseCount));
            Bus.Long(segmentNode + (uint)DosLayout.Segment.SegmentList,
                segmentAddress >> 2);
            invocation.VersionLayout.CommandSegmentNode = segmentNode;
            invocation.VersionLayout.CommandSegmentAddress = segmentAddress;
            invocation.VersionLayout.CommandSegmentRaw = segmentAddress >> 2;
            if (definition.CommandSegmentVersionText is { } segmentVersionText)
                invocation.VersionLayout.CommandSegmentVersionTextBytes =
                    unchecked((uint)(Encoding.Latin1.GetByteCount(
                        segmentVersionText) + 1));
        }
        Bus.Long(invocation.Process + (uint)DosLayout.Process.WindowPointer,
            0x1234_5678);
        if (!definition.System)
        {
            var values = definition.Names ?? [definition.Name];
            for (var index = 0; index < values.Length; index++)
            {
                var text = name + unchecked((uint)(index * 0x40));
                WriteCString(text, values[index]);
                if (index == 0) invocation.VersionLayout =
                    invocation.VersionLayout with { Name = text };
                Bus.Long(names + unchecked((uint)(index * 4)), text);
            }
            Bus.Long(names + unchecked((uint)(values.Length * 4)), 0);
        }

        Bus.Memory.AsSpan((int)invocation.VersionModule, 128).Clear();
        Bus.Long(4, 0x4000);
        Bus.Word(0x4000 + (uint)ExecLayout.Library.Version,
            unchecked((ushort)definition.KickstartVersion));
        Bus.Word(0x4000 + (uint)ExecLayout.ExecBase.SoftVer,
            unchecked((ushort)definition.KickstartRevision));
        Bus.Memory[invocation.VersionModule +
            (uint)ExecLayout.Resident.Version] =
                unchecked((byte)definition.ResidentVersion);
        Bus.Memory[invocation.VersionModule +
            (uint)ExecLayout.Resident.Flags] =
                unchecked((byte)definition.ResidentFlags);
        var idString = invocation.Arguments + (workbench ? 0xe00u : 0x3c0u);
        WriteCString(idString, definition.System
            ? $"$VER: MorphOS {definition.MorphOSVersion}.{definition.MorphOSRevision} (fixture)"
            : definition.ResidentIdString ?? "$VER: dos.library 50.6 (fixture)");
        var residentName = invocation.Arguments + (workbench ? 0xc00u : 0x5c0u);
        WriteCString(residentName, definition.System
            ? "MorphOS" : definition.ResidentName);
        Bus.Long(invocation.VersionModule +
            (uint)ExecLayout.Resident.Name, residentName);
        Bus.Word(invocation.VersionModule + Resident.Size,
            unchecked((ushort)definition.ResidentRevision));
        Bus.Long(invocation.VersionModule +
            (uint)ExecLayout.Resident.IdString,
            definition.ResidentNullIdString ? 0 : idString);
        if (workbench)
        {
            var layout = invocation.VersionLayout!;
            layout.WorkbenchBorrowedName = residentName;
            layout.WorkbenchBorrowedIdString = idString;
            layout.WorkbenchNameBefore = Encoding.Latin1.GetBytes(
                (definition.System ? "MorphOS" : definition.ResidentName) + "\0");
            layout.WorkbenchIdBefore = Encoding.Latin1.GetBytes(
                (definition.System
                    ? $"$VER: MorphOS {definition.MorphOSVersion}.{definition.MorphOSRevision} (fixture)"
                    : definition.ResidentIdString ?? "$VER: dos.library 50.6 (fixture)") + "\0");
            var table = invocation.VersionModule + 0x80;
            Bus.Long(ExecBase + (uint)ExecLayout.ExecBase.ResModules, table);
            if (definition.ResidentTableDecoy)
            {
                var decoy = invocation.VersionModule + 0x100;
                WriteCString(invocation.Arguments + 0x960, "unrelated.library");
                Bus.Long(decoy + (uint)ExecLayout.Resident.Name,
                    invocation.Arguments + 0x960);
                Bus.Long(table, decoy);
                table += 4;
            }
            if (definition.ResidentTableContinuation)
            {
                Bus.Long(table, (invocation.VersionModule + 0xa0) | 0x80000000);
                table = invocation.VersionModule + 0xa0;
            }
            var workbenchShellSegmentResident = workbench &&
                definition.CommandSegmentFound &&
                definition.CommandSegmentUseCount is -2 or -999 &&
                definition.ShellCommandResidentFound;
            Bus.Long(table, definition.ResidentFound ||
                workbenchShellSegmentResident ? invocation.VersionModule : 0);
            Bus.Long(table + 4, 0);
        }
        var residentTail = FindResidentTail(definition.System
            ? $"$VER: MorphOS {definition.MorphOSVersion}.{definition.MorphOSRevision} (fixture)"
            : definition.ResidentIdString ?? "$VER: dos.library 50.6 (fixture)");
        invocation.VersionLayout.ResidentVersionTextBytes = unchecked(
            (uint)(Encoding.Latin1.GetByteCount(definition.System
                ? "MorphOS" : definition.ResidentName) +
                Encoding.Latin1.GetByteCount(residentTail) + 1));
        if (definition.System)
        {
            var layout = invocation.VersionLayout!;
            layout.VersionLibraryBase = invocation.Arguments + 0x700;
            var versionIdString = invocation.Arguments + 0x440;
            var versionName = invocation.Arguments + 0x540;
            var versionId = definition.WorkbenchIdString ??
                $"$VER: version.library {definition.WorkbenchVersion}.{definition.WorkbenchRevision} (07.06.2023) extra";
            WriteCString(versionIdString, versionId);
            WriteCString(versionName, definition.WorkbenchLibraryName);
            var versionTail = FindVersionTail(versionId);
            layout.SystemVersionTextBytes = unchecked((uint)
                (Encoding.Latin1.GetByteCount(
                    definition.WorkbenchLibraryName) +
                 Encoding.Latin1.GetByteCount(versionTail) + 1));
            Bus.Word(layout.VersionLibraryBase +
                    (uint)ExecLayout.Library.Version,
                unchecked((ushort)definition.WorkbenchVersion));
            Bus.Word(layout.VersionLibraryBase +
                    (uint)ExecLayout.Library.Revision,
                unchecked((ushort)definition.WorkbenchRevision));
            Bus.Long(layout.VersionLibraryBase +
                    (uint)ExecLayout.Library.IdString, versionIdString);

            var libraryList = 0x4000u +
                (uint)ExecLayout.ExecBase.LibraryList;
            var librarySentinel = libraryList +
                (uint)ExecLayout.List.Tail;
            var listed = definition.VersionLibraryAvailable &&
                definition.VersionLibraryListed;
            Bus.Long(libraryList + (uint)ExecLayout.List.Head,
                listed ? layout.VersionLibraryBase : librarySentinel);
            Bus.Long(libraryList + (uint)ExecLayout.List.Tail, 0);
            Bus.Long(libraryList + (uint)ExecLayout.List.TailPred,
                listed ? layout.VersionLibraryBase : libraryList);
            Bus.Long(librarySentinel + (uint)ExecLayout.Node.Successor, 0);
            if (listed)
            {
                Bus.Long(layout.VersionLibraryBase +
                        (uint)ExecLayout.Node.Successor, librarySentinel);
                Bus.Long(layout.VersionLibraryBase +
                        (uint)ExecLayout.Node.Predecessor, libraryList);
                Bus.Long(layout.VersionLibraryBase +
                        (uint)ExecLayout.Node.Name, versionName);
            }
        }
        else if (definition.ExecLibraryFound)
        {
            var layout = invocation.VersionLayout!;
            var libraryList = 0x4000u +
                (uint)ExecLayout.ExecBase.LibraryList;
            var librarySentinel = libraryList +
                (uint)ExecLayout.List.Tail;
            layout.ExecLibraryBase = invocation.Arguments + 0x1300;
            var libraryName = invocation.Arguments + 0x1380;
            var libraryIdString = invocation.Arguments + 0x13c0;
            layout.ExecLibraryName = libraryName;
            layout.ExecLibraryIdString = libraryIdString;
            WriteCString(libraryName, definition.ExecLibraryName);
            WriteCString(libraryIdString, definition.ExecLibraryIdString);
            if (IsWorkbench31VersionEntrySuite(suite))
            {
                layout.WorkbenchBorrowedName = libraryName;
                layout.WorkbenchBorrowedIdString = libraryIdString;
                layout.WorkbenchNameBefore = Encoding.Latin1.GetBytes(
                    definition.ExecLibraryName + "\0");
                layout.WorkbenchIdBefore = Encoding.Latin1.GetBytes(
                    definition.ExecLibraryIdString + "\0");
            }
            var libraryTail = FindVersionTail(
                definition.ExecLibraryIdString);
            layout.ExecLibraryVersionTextBytes = unchecked((uint)
                (Encoding.Latin1.GetByteCount(definition.ExecLibraryName) +
                 Encoding.Latin1.GetByteCount(libraryTail) + 1));
            layout.ExecLibraryNameCopyBytes = unchecked((uint)
                Encoding.Latin1.GetByteCount(definition.ExecLibraryName) +
                1);
            Bus.Long(libraryList + (uint)ExecLayout.List.Head,
                layout.ExecLibraryBase);
            Bus.Long(libraryList + (uint)ExecLayout.List.Tail, 0);
            Bus.Long(libraryList + (uint)ExecLayout.List.TailPred,
                layout.ExecLibraryBase);
            Bus.Long(librarySentinel + (uint)ExecLayout.Node.Successor, 0);
            Bus.Long(layout.ExecLibraryBase +
                    (uint)ExecLayout.Node.Successor, librarySentinel);
            Bus.Long(layout.ExecLibraryBase +
                    (uint)ExecLayout.Node.Predecessor, libraryList);
            Bus.Long(layout.ExecLibraryBase +
                    (uint)ExecLayout.Node.Name, libraryName);
            Bus.Word(layout.ExecLibraryBase +
                    (uint)ExecLayout.Library.Version,
                unchecked((ushort)definition.ExecLibraryVersion));
            Bus.Word(layout.ExecLibraryBase +
                    (uint)ExecLayout.Library.Revision,
                unchecked((ushort)definition.ExecLibraryRevision));
            Bus.Long(layout.ExecLibraryBase +
                    (uint)ExecLayout.Library.IdString, libraryIdString);
        }
        else if (definition.ExecDeviceFound)
        {
            var layout = invocation.VersionLayout!;
            var deviceList = 0x4000u +
                (uint)ExecLayout.ExecBase.DeviceList;
            var deviceSentinel = deviceList + (uint)ExecLayout.List.Tail;
            layout.ExecDeviceBase = invocation.Arguments + 0x1500;
            var deviceName = invocation.Arguments + 0x1580;
            var deviceIdString = invocation.Arguments + 0x15c0;
            WriteCString(deviceName, definition.ExecDeviceName);
            WriteCString(deviceIdString, definition.ExecDeviceIdString);
            var deviceTail = FindVersionTail(definition.ExecDeviceIdString);
            layout.ExecDeviceVersionTextBytes = unchecked((uint)
                (Encoding.Latin1.GetByteCount(definition.ExecDeviceName) +
                 Encoding.Latin1.GetByteCount(deviceTail) + 1));
            layout.ExecDeviceNameCopyBytes = unchecked((uint)
                Encoding.Latin1.GetByteCount(definition.ExecDeviceName) + 1);
            Bus.Long(deviceList + (uint)ExecLayout.List.Head,
                layout.ExecDeviceBase);
            Bus.Long(deviceList + (uint)ExecLayout.List.Tail, 0);
            Bus.Long(deviceList + (uint)ExecLayout.List.TailPred,
                layout.ExecDeviceBase);
            Bus.Long(deviceSentinel + (uint)ExecLayout.Node.Successor, 0);
            Bus.Long(layout.ExecDeviceBase +
                    (uint)ExecLayout.Node.Successor, deviceSentinel);
            Bus.Long(layout.ExecDeviceBase +
                    (uint)ExecLayout.Node.Predecessor, deviceList);
            Bus.Long(layout.ExecDeviceBase +
                    (uint)ExecLayout.Node.Name, deviceName);
            if (IsWorkbench31VersionEntrySuite(suite))
            {
                layout.WorkbenchBorrowedName = deviceName;
                layout.WorkbenchBorrowedIdString = deviceIdString;
                layout.WorkbenchNameBefore = Encoding.Latin1.GetBytes(
                    definition.ExecDeviceName + "\0");
                layout.WorkbenchIdBefore = Encoding.Latin1.GetBytes(
                    definition.ExecDeviceIdString + "\0");
            }
            Bus.Word(layout.ExecDeviceBase +
                    (uint)ExecLayout.Library.Version,
                unchecked((ushort)definition.ExecDeviceVersion));
            Bus.Word(layout.ExecDeviceBase +
                    (uint)ExecLayout.Library.Revision,
                unchecked((ushort)definition.ExecDeviceRevision));
            Bus.Long(layout.ExecDeviceBase +
                    (uint)ExecLayout.Library.IdString, deviceIdString);
        }
    }

    private void InitializeEmptyExecList(uint list)
    {
        var sentinel = list + (uint)ExecLayout.List.Tail;
        Bus.Long(list + (uint)ExecLayout.List.Head, sentinel);
        Bus.Long(list + (uint)ExecLayout.List.Tail, 0);
        Bus.Long(list + (uint)ExecLayout.List.TailPred, list);
        Bus.Long(sentinel + (uint)ExecLayout.Node.Successor, 0);
    }

    private static string FindResidentTail(string value)
    {
        for (var index = 0; index + 1 < value.Length; index++)
        {
            if (value[index] is not (' ' or '\t')) continue;
            var start = index + 1;
            if (start < value.Length && value[start] is '+' or '-') start++;
            var digitsEnd = start;
            while (digitsEnd < value.Length &&
                value[digitsEnd] >= '0' && value[digitsEnd] <= '9')
                digitsEnd++;
            if (digitsEnd == start) continue;
            var end = index + 1;
            while (end < value.Length && value[end] is not ('\r' or '\n'))
                end++;
            return value[index..end];
        }
        return "";
    }

    private void WriteCString(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value + "\0");
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
    }

    private static string FindVersionTail(string value)
    {
        for (var dottedOnly = true; ; dottedOnly = false)
        {
            for (var index = 0; index + 1 < value.Length; index++)
            {
                var current = value[index];
                if (current is not (' ' or '\t')) continue;
                var numberStart = index + 1;
                if (value[numberStart] is '+' or '-') numberStart++;
                var digitsEnd = numberStart;
                while (digitsEnd < value.Length &&
                    value[digitsEnd] >= '0' && value[digitsEnd] <= '9')
                    digitsEnd++;
                if (digitsEnd == numberStart ||
                    dottedOnly && (digitsEnd >= value.Length ||
                        value[digitsEnd] != '.'))
                    continue;
                var end = index + 1;
                while (end < value.Length && value[end] is not ('\r' or '\n'))
                    end++;
                return value[index..end];
            }
            if (!dottedOnly) return "";
        }
    }

    private static bool IsValidAmbientResult(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var offset = 0;
        if (!ConsumeSignedDigits(value, ref offset) ||
            offset >= value.Length || value[offset++] != '.' ||
            !ConsumeSignedDigits(value, ref offset))
            return false;
        return true;
    }

    private static bool HasAmbientFileVersion(string? content)
    {
        if (string.IsNullOrEmpty(content)) return false;
        var tag = content.IndexOf("$VER:", StringComparison.Ordinal);
        if (tag < 0) return false;
        var valueStart = tag + 5;
        while (valueStart < content.Length)
        {
            while (valueStart < content.Length &&
                char.IsWhiteSpace(content[valueStart])) valueStart++;
            var valueEnd = valueStart;
            while (valueEnd < content.Length &&
                !char.IsWhiteSpace(content[valueEnd])) valueEnd++;
            if (IsValidAmbientResult(content[valueStart..valueEnd]))
                return true;
            if (valueEnd == content.Length) break;
            valueStart = valueEnd + 1;
        }
        return false;
    }

    private static bool ConsumeSignedDigits(string value, ref int offset)
    {
        if (offset < value.Length && value[offset] is '+' or '-') offset++;
        var start = offset;
        while (offset < value.Length && value[offset] is >= '0' and <= '9')
            offset++;
        return offset > start;
    }

    private void RegisterVersionEntryExec()
    {
        if (IsWorkbench31VersionEntrySuite(suite))
        {
                Register(VersionUtilityBase, UtilityLvo.Stricmp,
                "WorkbenchVersionStricmp", (state, invocation) =>
                {
                    var definition = invocation.Definition.Version!;
                    var layout = invocation.VersionLayout!;
                    Require(state.A[6] == VersionUtilityBase &&
                        invocation.VersionUtilityOpenCalls == 1 &&
                        invocation.VersionUtilityCloseCalls == 0,
                        "Workbench full-name Stricmp query/lease differs.");
                    var queryName = Bus.CString(state.A[0]);
                    var comparedName = Bus.CString(state.A[1]);
                    var filePartIndex = Math.Max(definition.Name.LastIndexOf(':'),
                        Math.Max(definition.Name.LastIndexOf('/'),
                            definition.Name.LastIndexOf('\\')));
                    var loadedNodeQuery = layout.FilePartCalls != 0 &&
                        queryName == definition.Name[(filePartIndex + 1)..] &&
                        (definition.ExecLibraryFound &&
                            comparedName == definition.ExecLibraryName ||
                         definition.ExecDeviceFound &&
                            comparedName == definition.ExecDeviceName);
                    if (loadedNodeQuery)
                    {
                        layout.UtilityStricmpCalls++;
                        return string.Equals(queryName, comparedName,
                            StringComparison.OrdinalIgnoreCase) ? 0u : 1u;
                    }
                    Require(queryName == definition.Name ||
                        definition.CommandSegmentFound &&
                        definition.CommandSegmentUseCount is -2 or -999 &&
                        queryName == "shell",
                        "Workbench Resident Stricmp query differs.");
                    Require(comparedName == definition.ResidentName ||
                        definition.ResidentTableDecoy && comparedName == "unrelated.library",
                        "Workbench Stricmp did not read a Resident name.");
                    layout.UtilityStricmpCalls++;
                    var expectedMatch = queryName == "shell"
                        ? definition.ShellCommandResidentFound
                        : definition.ResidentFound;
                    return expectedMatch && string.Equals(queryName,
                        comparedName, StringComparison.OrdinalIgnoreCase) ? 0u :
                        definition.ResidentComparisonNonzeroLowWordZero ? 0x10000u : 1u;
                });
        }
        if (IsWorkbench31VersionEntrySuite(suite))
            Register(VersionUtilityBase, UtilityLvo.Date2Amiga,
                "WorkbenchVersionDate2Amiga", (state, invocation) =>
                {
                    var definition = invocation.Definition.Version!;
                    var layout = invocation.VersionLayout!;
                    var clock = state.A[0];
                    Require(state.A[6] == VersionUtilityBase &&
                        invocation.VersionUtilityCloseCalls == 0 &&
                        Bus.Word(clock) == 0 && Bus.Word(clock + 2) == 0 &&
                        Bus.Word(clock + 4) == 0 && Bus.Word(clock + 12) == 0,
                        "Workbench Date2Amiga clock or lease differs.");
                    layout.WorkbenchDate2AmigaCalls++;
                    if (definition.WorkbenchDateSeconds is uint supplied) return supplied;
                    try
                    {
                        var date = new DateTime(Bus.Word(clock + 10),
                            Bus.Word(clock + 8), Bus.Word(clock + 6));
                        return checked((uint)(date - new DateTime(1978, 1, 1)).TotalSeconds);
                    }
                    catch (ArgumentOutOfRangeException) { return 0; }
                });
        if (!IsWorkbench31VersionEntrySuite(suite))
            Register(VersionUtilityBase, UtilityLvo.Stricmp,
                "VersionUtilityStricmp", (state, invocation) =>
                {
                    var layout = invocation.VersionLayout!;
                    var definition = invocation.Definition.Version!;
                    var lastSeparator = Math.Max(
                        definition.Name.LastIndexOf(':'),
                        Math.Max(definition.Name.LastIndexOf('/'),
                            definition.Name.LastIndexOf('\\')));
                    var lookupName = definition.Name[(lastSeparator + 1)..];
                    var comparedName = Bus.CString(state.A[0]);
                    var requestedName = Bus.CString(state.A[1]);
                    Require(state.A[6] == VersionUtilityBase &&
                        requestedName == lookupName &&
                        (comparedName == definition.ExecLibraryName ||
                            comparedName == definition.ExecDeviceName),
                        "Version utility Stricmp arguments differ.");
                    layout.UtilityStricmpCalls++;
                    return string.Equals(comparedName, requestedName,
                        StringComparison.OrdinalIgnoreCase)
                            ? 0u : 1u;
                });
        Register(ExecBase, ExecLvo.CreateMsgPort, "CreateMsgPort",
            (_, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                layout.AmbientPortCreateCalls++;
                return invocation.Definition.Version!.RexxLibraryAvailable
                    ? layout.AmbientReplyPort : 0;
            });
        Register(ExecBase, ExecLvo.DeleteMsgPort, "DeleteMsgPort",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                Require(state.A[0] == layout.AmbientReplyPort &&
                    layout.AmbientPortCreateCalls == 1 &&
                    layout.AmbientPortDeleteCalls == 0,
                    "Version Ambient reply-port ownership differs.");
                layout.AmbientPortDeleteCalls++;
                return 0;
            });
        Register(ExecBase, ExecLvo.PutMsg, "PutMsg", (state,
            invocation) =>
        {
            var layout = invocation.VersionLayout!;
            var definition = invocation.Definition.Version!;
            Require(definition.AmbientRexxPortAvailable &&
                state.A[0] == layout.AmbientPort &&
                state.A[1] == layout.AmbientHostMessage &&
                Bus.Long(state.A[1] + 28) == 0x0102_0000 &&
                Bus.Long(state.A[1] + 40) ==
                    layout.AmbientCommandArgString &&
                Bus.CString(layout.AmbientCommandArgString) == "VERSION",
                "Version Ambient ARexx request differs.");
            layout.AmbientPutCalls++;
            Bus.Long(state.A[1] + 32, 0);
            Bus.Long(state.A[1] + 36,
                layout.AmbientResultArgString);
            return 0;
        });
        Register(VersionRexxLibraryBase, -126, "CreateArgstring",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                Require(state.A[0] != 0 && state.D[0] == 7 &&
                    Bus.CString(state.A[0]) == "VERSION" &&
                    layout.AmbientCommandCreateCalls == 0,
                    "Version Ambient command argstring differs.");
                layout.AmbientCommandCreateCalls++;
                return layout.AmbientCommandArgString;
            });
        Register(VersionRexxLibraryBase, -132, "DeleteArgstring",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                if (state.A[0] == layout.AmbientCommandArgString)
                {
                    Require(layout.AmbientCommandCreateCalls == 1 &&
                        layout.AmbientCommandDeleteCalls == 0,
                        "Version Ambient command argstring release differs.");
                    layout.AmbientCommandDeleteCalls++;
                }
                else
                {
                    Require(state.A[0] == layout.AmbientResultArgString &&
                        layout.AmbientPutCalls == 1 &&
                        layout.AmbientResultDeleteCalls == 0,
                        "Version Ambient result argstring release differs.");
                    layout.AmbientResultDeleteCalls++;
                }
                return 0;
            });
        Register(VersionRexxLibraryBase, -138, "LengthArgstring",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                var result = invocation.Definition.Version!.AmbientRexxResult;
                Require(state.A[0] == layout.AmbientResultArgString &&
                    result is not null,
                    "Version Ambient result length request differs.");
                return unchecked((uint)Encoding.Latin1.GetByteCount(result!));
            });
        Register(VersionRexxLibraryBase, -144, "CreateRexxMsg",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                Require(state.A[0] == layout.AmbientReplyPort &&
                    state.A[1] == 0 && state.D[0] != 0 &&
                    Bus.CString(state.D[0]) == "AMBIENT" &&
                    layout.AmbientMessageCreateCalls == 0,
                    "Version Ambient RexxMsg creation differs.");
                layout.AmbientMessageCreateCalls++;
                layout.AmbientHostMessage = Bus.Allocate(invocation, 128,
                    "VersionRexxMessage", true);
                return layout.AmbientHostMessage;
            });
        Register(VersionRexxLibraryBase, -150, "DeleteRexxMsg",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                Require(state.A[0] == layout.AmbientHostMessage &&
                    layout.AmbientMessageCreateCalls == 1 &&
                    layout.AmbientCommandDeleteCalls == 1 &&
                    layout.AmbientMessageDeleteCalls == 0,
                    "Version Ambient RexxMsg release differs.");
                Bus.Release(invocation, state.A[0],
                    "VersionRexxMessage", 128);
                layout.AmbientMessageDeleteCalls++;
                return 0;
            });
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state,
            invocation) =>
        {
            Require(state.D[0] == 0 && state.D[1] == 1u << 12,
                "Version Ctrl-C polling ABI differs.");
            var layout = invocation.VersionLayout!;
            layout.SetSignalCalls++;
            return invocation.Definition.Version!.CtrlC &&
                layout.SetSignalCalls == 1 ? 1u << 12 : 0;
        });
    }

    private void RegisterVersionEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.Version!;
            var layout = invocation.VersionLayout!;
            var workbench = IsWorkbench31VersionEntrySuite(suite);
            Require(Bus.CString(state.D[1]) ==
                (workbench
                    ? "NAME,VERSION/N,REVISION/N,FILE/S,FULL/S,UNIT/N,INTERNAL/S,RES/S"
                    : "NAME/M,MD5SUM/S,VERSION/N,REVISION/N,FILE/S,FULL/S,RES/S") &&
                state.D[3] == 0 && state.D[2] % 4 == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size ==
                    (workbench ? 32u : 28u),
                "Version ReadArgs ABI differs.");
            invocation.VersionReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }

            var rdArgsSize = workbench && !definition.System ? 512u : 40u;
            var rdArgs = Bus.Allocate(invocation, rdArgsSize, "RDArgs", true);
            if (!definition.System)
            {
                if (workbench)
                {
                    // ReadArgs owns its parsed command-line strings until
                    // FreeArgs. The classic trailing-colon search temporarily
                    // terminates NAME in this writable storage.
                    var parsedName = rdArgs + 64;
                    WriteCString(parsedName, Bus.CString(layout.Name));
                    layout.WorkbenchParsedName = parsedName;
                    Bus.Long(state.D[2], parsedName);
                }
                else
                    Bus.Long(state.D[2], layout.Names);
            }
            if (!workbench && definition.Md5)
                Bus.Long(state.D[2] + 4, uint.MaxValue);
            if (definition.RequestedVersion is int version)
            {
                Bus.Long(layout.NumericVersion, unchecked((uint)version));
                Bus.Long(state.D[2] + (workbench ? 4u : 8u),
                    layout.NumericVersion);
            }
            if (definition.RequestedRevision is int revision)
            {
                Bus.Long(layout.NumericRevision,
                    unchecked((uint)revision));
                Bus.Long(state.D[2] + (workbench ? 8u : 12u),
                    layout.NumericRevision);
            }
            if (definition.File)
                Bus.Long(state.D[2] + (workbench ? 12u : 16u), uint.MaxValue);
            if (definition.Full)
                Bus.Long(state.D[2] + (workbench ? 16u : 20u), uint.MaxValue);
            if (workbench && definition.Unit)
                Bus.Long(state.D[2] + 20, layout.NumericVersion);
            if (workbench && definition.Internal)
                Bus.Long(state.D[2] + 24, uint.MaxValue);
            if (definition.Resident)
                Bus.Long(state.D[2] + (workbench ? 28u : 24u), uint.MaxValue);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            if (IsWorkbench31VersionEntrySuite(suite) &&
                invocation.VersionLayout!.WorkbenchParsedName != 0)
                invocation.VersionLayout.WorkbenchParsedNameAtFreeArgs =
                    Bus.CString(invocation.VersionLayout.WorkbenchParsedName);
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.VersionFreeArgsCalls++;
            invocation.IoError = 901;
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state,
            invocation) =>
        {
            var format = Bus.CString(state.D[1]);
            Require(format is "%s %lu.%lu\n" or "%s %lu.%lu" or
                "%s %lu.%lu, " or
                "%s %ld.%ld\n" or "%s %ld.%ld" or " (%s)" or "\n%s" or
                "%s%s\n" or "%s%s%s%s%s\n" or
                "%s%s%s%s%s%s%s%s\n" or
                "%08lX%08lX%08lX%08lX  " or
                "Could not find version information for '%s'\n",
                "Version output format differs.");
            var fields = state.D[2];
            if (format == "%08lX%08lX%08lX%08lX  ")
            {
                var output = new StringBuilder();
                for (var index = 0; index < 4; index++)
                    output.Append(Bus.Long(fields + unchecked((uint)(index * 4)))
                        .ToString("X8", System.Globalization.CultureInfo.InvariantCulture));
                output.Append("  ");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    output.ToString()));
                invocation.VersionVPrintfCalls++;
                return 0;
            }
            if (format == "Could not find version information for '%s'\n")
            {
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"Could not find version information for '{Bus.CString(Bus.Long(fields))}'\n"));
                invocation.VersionVPrintfCalls++;
                return 0;
            }
            if (format is "%s%s%s%s%s\n" or "%s%s%s%s%s%s%s%s\n")
            {
                var count = format == "%s%s%s%s%s\n" ? 5 : 8;
                var output = new StringBuilder();
                for (var index = 0; index < count; index++)
                {
                    var pointer = Bus.Long(fields + unchecked((uint)(index * 4)));
                    if (pointer != 0) output.Append(Bus.CString(pointer));
                }
                output.Append('\n');
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    output.ToString()));
                invocation.VersionVPrintfCalls++;
                return 0;
            }
            if (format == "%s%s\n")
            {
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    Bus.CString(Bus.Long(fields)) +
                    Bus.CString(Bus.Long(fields + 4)) + "\n"));
                invocation.VersionVPrintfCalls++;
                return 0;
            }
            if (format == "%s %lu.%lu")
            {
                var systemName = Bus.CString(Bus.Long(fields));
                var systemVersion = Bus.Long(fields + 4);
                var systemRevision = Bus.Long(fields + 8);
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{systemName} {systemVersion}.{systemRevision}"));
                invocation.VersionVPrintfCalls++;
                return 0;
            }
            if (format == "%s %lu.%lu, ")
            {
                var systemName = Bus.CString(Bus.Long(fields));
                var systemVersion = Bus.Long(fields + 4);
                var systemRevision = Bus.Long(fields + 8);
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{systemName} {systemVersion}.{systemRevision}, "));
                invocation.VersionVPrintfCalls++;
                return 0;
            }
            if (format is " (%s)" or "\n%s")
            {
                var pointer = Bus.Long(fields);
                if (format == "\n%s")
                    Require(pointer == invocation.VersionLayout!.WorkbenchOwnedExtra &&
                        invocation.VersionLayout.WorkbenchExtraFreeCalls == 0,
                        "Workbench FULL extra must use its live owned copy.");
                var text = Bus.CString(pointer);
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    format == " (%s)" ? $" ({text})" : $"\n{text}"));
                invocation.VersionVPrintfCalls++;
                return 0;
            }
            if (format is "%s %ld.%ld\n" or "%s %ld.%ld")
            {
                var layout = invocation.VersionLayout!;
                var definition = invocation.Definition.Version!;
                var outputName = Bus.Long(fields);
                var fileTagName = definition.File &&
                    outputName >= layout.FileBuffer &&
                    (ulong)outputName < (ulong)layout.FileBuffer + VersionFileBytes;
                var namedFileTagName =
                    IsWorkbench31VersionEntrySuite(suite) &&
                    !definition.File && layout.WorkbenchNamedFileOpenSuccesses != 0 &&
                    outputName >= layout.FileBuffer &&
                    (ulong)outputName <
                        (ulong)layout.FileBuffer + VersionFileBytes;
                var loadedFileResidentName = definition.File &&
                    outputName == layout.AmbientResidentName && outputName != 0;
                var commandSegmentName = definition.CommandSegmentFound &&
                    layout.CommandSegmentScratch != 0 &&
                    outputName >= layout.CommandSegmentScratch &&
                    (ulong)outputName <
                        (ulong)layout.CommandSegmentScratch +
                            16_449u;
                var workbenchName = (outputName == layout.WorkbenchOwnedName &&
                        layout.WorkbenchNameFreeCalls == 0) || fileTagName ||
                    namedFileTagName ||
                    loadedFileResidentName || commandSegmentName;
                var morphOsResidentName =
                    !IsWorkbench31VersionEntrySuite(suite) &&
                    outputName != 0 && Bus.CString(outputName) ==
                        definition.ResidentName;
                Require(IsWorkbench31VersionEntrySuite(suite)
                    ? workbenchName : morphOsResidentName,
                    "Version output name must remain live for the selected provider.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{Bus.CString(outputName)} " +
                    $"{unchecked((int)Bus.Long(fields + 4))}." +
                    $"{unchecked((int)Bus.Long(fields + 8))}" +
                    (format.EndsWith('\n') ? "\n" : "")));
                invocation.VersionVPrintfCalls++;
                return 0;
            }
            var name = Bus.CString(Bus.Long(fields));
            var version = Bus.Long(fields + 4);
            var revision = Bus.Long(fields + 8);
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                $"{name} {version}.{revision}\n"));
            invocation.VersionVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state,
            invocation) =>
        {
            var text = Bus.CString(state.D[1]);
            Require(text is "<no md5sum available>             " or ", " or "\n",
                "Version PutStr text differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            if (text == "\n" && IsWorkbench31VersionEntrySuite(suite))
                invocation.VersionLayout!.WorkbenchNewlineCalls++;
            if (text == ", ")
                invocation.VersionSystemSeparatorCalls++;
            else if (text != "\n")
                invocation.VersionPutStrCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state,
            invocation) =>
        {
            invocation.VersionPrintFaultCalls++;
            Require(state.D[2] == 0 && (!IsWorkbench31VersionEntrySuite(suite) ||
                unchecked((int)state.D[1]) == invocation.Definition.Error),
                $"Version PrintFault error or header differs " +
                $"(actual={unchecked((int)state.D[1])}, " +
                $"expected={invocation.Definition.Error}, " +
                $"case={invocation.Definition.Name}, " +
                $"segmentCalls={invocation.VersionLayout!.FindSegmentCalls}, " +
                $"segmentScratch={invocation.VersionLayout.CommandSegmentScratch:X8}, " +
                $"segmentText={invocation.VersionLayout.CommandSegmentText:X8}, " +
                $"segmentAddress={invocation.VersionLayout.CommandSegmentAddress:X8}, " +
                $"segmentData={Convert.ToHexString(Bus.Memory.AsSpan((int)invocation.VersionLayout.CommandSegmentAddress, 48))}, " +
                $"scratchData={(invocation.VersionLayout.CommandSegmentScratch == 0 ? "<none>" : Convert.ToHexString(Bus.Memory.AsSpan((int)invocation.VersionLayout.CommandSegmentScratch, 32)))}).");
            return 0;
        });
        Register(baseAddress, DosLvo.GetVar, "GetVar", (state,
            invocation) =>
        {
            var layout = invocation.VersionLayout!;
            var definition = invocation.Definition.Version!;
            Require(Bus.CString(state.D[1]) == "ambient_path" &&
                state.D[2] != 0 && state.D[3] == 256 && state.D[4] == 0 &&
                layout.AmbientGetVarCalls == 0,
                "Version Ambient path GetVar ABI differs.");
            layout.AmbientGetVarCalls++;
            if (definition.AmbientPathVariable is not { } value)
                return uint.MaxValue;
            var bytes = Encoding.Latin1.GetBytes(value + "\0");
            Require(bytes.Length <= state.D[3],
                "Version Ambient path exceeds its native buffer.");
            bytes.CopyTo(Bus.Memory.AsSpan(checked((int)state.D[2])));
            return unchecked((uint)(bytes.Length - 1));
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state,
            invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.GetDeviceProc, "GetDeviceProc",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                var definition = invocation.Definition.Version!;
                Require(Bus.CString(state.D[1]) == definition.Name &&
                    state.D[2] == 0 && layout.GetDeviceProcCalls == 0,
                    "Version volume GetDeviceProc arguments differ.");
                layout.GetDeviceProcCalls++;
                return definition.VolumeDeviceProcFound
                    ? layout.VolumeDeviceProc : 0;
            });
        Register(baseAddress, DosLvo.FreeDeviceProc, "FreeDeviceProc",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                Require(state.D[1] == layout.VolumeDeviceProc &&
                    layout.GetDeviceProcCalls == 1 &&
                    layout.FreeDeviceProcCalls == 0,
                    "Version volume DevProc release differs.");
                layout.FreeDeviceProcCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.LockDosList, "LockDosList",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                var definition = invocation.Definition.Version!;
                var workbenchDeviceLookup =
                    IsWorkbench31VersionEntrySuite(suite) &&
                    definition.WorkbenchDeviceListLookup;
                Require(state.D[1] == (uint)(DosListLockFlags.Devices |
                        DosListLockFlags.Read) &&
                    (workbenchDeviceLookup ||
                        definition.VolumeDeviceIsVolume) &&
                    layout.LockDosListCalls == 0,
                "Version volume device-list lock flags differ.");
                layout.LockDosListCalls++;
                return layout.VolumeListCursor;
            });
        Register(baseAddress, DosLvo.FindDosEntry, "FindDosEntry",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                var definition = invocation.Definition.Version!;
                Require(IsWorkbench31VersionEntrySuite(suite) &&
                    definition.WorkbenchDeviceListLookup &&
                    state.D[1] == layout.VolumeListCursor &&
                    Bus.CString(state.D[2]) == definition.Name[..^1] &&
                    state.D[3] == (uint)DosListLockFlags.Devices &&
                    layout.LockDosListCalls == 1 &&
                    layout.FindDosEntryCalls == 0 &&
                    layout.UnLockDosListCalls == 0,
                    "Workbench trailing-colon FindDosEntry ABI or name mutation differs.");
                layout.FindDosEntryCalls++;
                return definition.WorkbenchDeviceListEntryFound
                    ? layout.VolumeDeviceNode : 0u;
            });
        Register(baseAddress, DosLvo.NextDosEntry, "NextDosEntry",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                var definition = invocation.Definition.Version!;
                Require(state.D[2] == (uint)DosListLockFlags.Devices &&
                    layout.LockDosListCalls == 1 &&
                    layout.UnLockDosListCalls == 0,
                    "Version volume NextDosEntry flags or lock lifetime differ.");
                layout.NextDosEntryCalls++;
                if (state.D[1] == layout.VolumeListCursor)
                    return layout.VolumePriorDeviceNode;
                if (state.D[1] == layout.VolumePriorDeviceNode)
                    return definition.VolumeDeviceFound
                        ? layout.VolumeDeviceNode : 0;
                throw new InvalidOperationException(
                    "Version volume DOS-list traversal cursor differs.");
            });
        Register(baseAddress, DosLvo.UnLockDosList, "UnLockDosList",
            (state, invocation) =>
            {
                var layout = invocation.VersionLayout!;
                var definition = invocation.Definition.Version!;
                var workbenchDeviceLookup =
                    IsWorkbench31VersionEntrySuite(suite) &&
                    definition.WorkbenchDeviceListLookup;
                Require(state.D[1] == (uint)(DosListLockFlags.Devices |
                        DosListLockFlags.Read) &&
                    layout.LockDosListCalls == 1 &&
                    layout.UnLockDosListCalls == 0 &&
                    (workbenchDeviceLookup || definition.VolumeDeviceIsVolume),
                    "Version volume device-list unlock differs.");
                layout.UnLockDosListCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.SetVar, "SetVar", (state,
            invocation) =>
            {
                var layout = invocation.VersionLayout!;
                var definition = invocation.Definition.Version!;
                Require(Bus.CString(state.D[1]) == "Ambient" &&
                    state.D[2] != 0 && state.D[3] == uint.MaxValue &&
                    state.D[4] == (uint)GlobalVariableFlags.LocalOnly &&
                    layout.AmbientSetVarCalls == 0 &&
                    Bus.CString(state.D[2]) ==
                        (definition.AmbientExpectedVersion ??
                            definition.AmbientRexxResult),
                    "Version Ambient local version variable differs.");
                layout.AmbientSetVarCalls++;
                return 1;
            });
        Register(baseAddress, DosLvo.OpenRaw, "OpenRaw", (state,
            invocation) =>
        {
            var layout = invocation.VersionLayout!;
            var definition = invocation.Definition.Version!;
            if (definition.System &&
                !IsWorkbench31VersionEntrySuite(suite))
            {
                var name = Bus.CString(state.D[1]);
                var contents = name switch
                {
                    var configured when definition.AmbientPathVariable is not null &&
                        configured == definition.AmbientPathVariable =>
                            definition.AmbientPathFileContent,
                    "mossys:ambient/ambient" =>
                        definition.AmbientMossysFileContent,
                    "sys:system/ambient/ambient" =>
                        definition.AmbientSysFileContent,
                    _ => null
                };
                if (contents is not null ||
                    name is "mossys:ambient/ambient" or
                        "sys:system/ambient/ambient" ||
                    definition.AmbientPathVariable is not null &&
                        name == definition.AmbientPathVariable)
                {
                    Require(state.D[2] == (uint)DOS.FileMode.OldFile,
                        "Version Ambient file-open mode differs.");
                    layout.AmbientFileOpenAttempts++;
                    if (contents is null)
                    {
                        invocation.IoError = (int)DOS.Error.ObjectNotFound;
                        return 0;
                    }
                    layout.AmbientFileActive = true;
                    layout.AmbientFileContent = contents;
                    layout.AmbientFileName = name;
                    layout.AmbientFileHandle = 0x520;
                    layout.AmbientFilePosition = 0;
                    layout.AmbientFileAllocMemCalls = 0;
                    layout.AmbientFileOpenSuccesses++;
                    return layout.AmbientFileHandle;
                }
            }
            var workbenchDirectFile =
                IsWorkbench31VersionEntrySuite(suite) &&
                !definition.System && definition.File;
            if (workbenchDirectFile)
            {
                Require(Bus.CString(state.D[1]) == definition.Name &&
                    state.D[2] == (uint)DOS.FileMode.OldFile &&
                    layout.FileOpenCalls == 0,
                    "Workbench FILE direct open name, mode or order differs.");
                layout.FileOpenCalls++;
                invocation.VersionFileOpens++;
                if (definition.FileOpenError != 0)
                {
                    invocation.IoError = definition.FileOpenError;
                    return 0;
                }
                layout.FileHandle = 0x510;
                layout.FileName = definition.Name;
                layout.FilePosition = 0;
                layout.FileReadCalls = 0;
                return layout.FileHandle;
            }
            var workbenchDefaultFile =
                IsWorkbench31VersionEntrySuite(suite) &&
                !definition.System && !definition.File;
            if (workbenchDefaultFile)
            {
                Require(state.D[2] == (uint)DOS.FileMode.OldFile,
                    "Workbench default file open mode differs.");
                var path = Bus.CString(state.D[1]);
                layout.WorkbenchNamedFilePaths.Add(path);
                layout.FileOpenCalls++;
                invocation.VersionFileOpens++;
                var hitPath = definition.WorkbenchFileHitPath ??
                    (definition.DirectFileHit ? definition.Name : null);
                if (definition.FileOpenError != 0 &&
                    (hitPath is null || hitPath == path))
                {
                    invocation.IoError = definition.FileOpenError;
                    return 0;
                }
                if (path != hitPath)
                {
                    invocation.IoError = (int)DOS.Error.ObjectNotFound;
                    return 0;
                }
                layout.WorkbenchNamedFileOpenSuccesses++;
                layout.FileHandle = 0x510;
                layout.FileName = path;
                layout.FilePosition = 0;
                layout.FileReadCalls = 0;
                return layout.FileHandle;
            }
            var libraryFileSearch =
                !IsWorkbench31VersionEntrySuite(suite) &&
                !definition.System && !definition.File &&
                !definition.ResidentFound &&
                !definition.ExecLibraryFound &&
                definition.UtilityLibraryAvailable &&
                layout.LibrarySearchPathBuffer != 0 &&
                Bus.CString(state.D[1]) is var libraryPath &&
                (libraryPath.StartsWith("MOSSYS:LIBS/",
                     StringComparison.Ordinal) ||
                 libraryPath.StartsWith("LIBS:",
                     StringComparison.Ordinal) ||
                 libraryPath.StartsWith("MOSSYS:DEVS/",
                     StringComparison.Ordinal) ||
                 libraryPath.StartsWith("DEVS:",
                     StringComparison.Ordinal));
            if (libraryFileSearch)
            {
                Require(state.D[2] == (uint)DOS.FileMode.OldFile,
                    "Version library candidate open mode differs.");
                var path = Bus.CString(state.D[1]);
                layout.LibrarySearchPaths.Add(path);
                layout.LibraryFileOpenAttempts++;
                if (path != definition.LibraryFileHitPath)
                {
                    invocation.IoError =
                        (int)DOS.Error.ObjectNotFound;
                    return 0;
                }
                Require(definition.LibraryFileContent is not null,
                    "Version library hit has no fixture content.");
                layout.LibraryFileName = path;
                layout.LibraryFileContent =
                    definition.LibraryFileContent;
                layout.LibraryFileActive = true;
                layout.LibraryFileOpenSuccesses++;
                layout.FileHandle = 0x530;
                layout.FileName = path;
                layout.FilePosition = 0;
                layout.FileReadCalls = 0;
                layout.FileOpenCalls++;
                invocation.VersionFileOpens++;
                return layout.FileHandle;
            }
            var namedDirectFileFallback =
                !IsWorkbench31VersionEntrySuite(suite) &&
                !definition.System && !definition.File &&
                !definition.Resident && !definition.ResidentFound &&
                !definition.ExecLibraryFound && !definition.ExecDeviceFound &&
                definition.UtilityLibraryAvailable &&
                definition.ParserError == 0 &&
                Bus.CString(state.D[1]) == definition.Name;
            if (namedDirectFileFallback)
            {
                Require(state.D[2] == (uint)DOS.FileMode.OldFile,
                    "Version direct fallback open mode differs.");
                layout.DirectFileOpenAttempts++;
                if (!definition.DirectFileHit)
                {
                    invocation.IoError =
                        (int)DOS.Error.ObjectNotFound;
                    return 0;
                }
            }
            var names = definition.Names ?? [definition.Name];
            var expectedName = names[layout.FileOpenCalls];
            Require(Bus.CString(state.D[1]) == expectedName &&
                state.D[2] == (uint)DOS.FileMode.OldFile,
                $"Version file-open request differs: '{Bus.CString(state.D[1])}' " +
                $"(searchPath=0x{layout.LibrarySearchPathBuffer:X8}, " +
                $"attempts={layout.LibraryFileOpenAttempts}).");
            layout.FileOpenCalls++;
            invocation.VersionFileOpens++;
            if (definition.FileOpenError != 0)
            {
                invocation.IoError = definition.FileOpenError;
                return 0;
            }
            layout.FileHandle = 0x510;
            layout.FileName = expectedName;
            layout.FilePosition = 0;
            layout.FileReadCalls = 0;
            return layout.FileHandle;
        });
        Register(baseAddress, DosLvo.IsFileSystem, "IsFileSystem", (state,
            invocation) =>
        {
            var layout = invocation.VersionLayout!;
            var definition = invocation.Definition.Version!;
            var ambient = definition.System &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                layout.AmbientFileActive &&
                Bus.CString(state.D[1]) == layout.AmbientFileName;
            var file = definition.File &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                layout.FileCloseCalls < layout.FileOpenCalls &&
                Bus.CString(state.D[1]) == layout.FileName;
            var libraryFile = layout.LibraryFileActive &&
                Bus.CString(state.D[1]) == layout.LibraryFileName;
            Require(ambient || file || libraryFile,
                "Version Ambient IsFileSystem path or call order differs.");
            layout.AmbientIsFileSystemCalls++;
            return (ambient ? definition.AmbientIsFileSystem :
                file ? definition.FileIsFileSystem : false) ? 1u : 0u;
        });
        Register(baseAddress, DosLvo.FilePart, "FilePart", (state,
            invocation) =>
        {
            var layout = invocation.VersionLayout!;
            var definition = invocation.Definition.Version!;
            Require(!definition.System && !definition.File &&
                Bus.CString(state.D[1]) == definition.Name,
                "Version FilePart lookup path differs.");
            layout.FilePartCalls++;
            var start = state.D[1];
            var cursor = start;
            while (cursor < Bus.Memory.Length && Bus.Memory[cursor] != 0)
            {
                if (Bus.Memory[cursor] is (byte)':' or (byte)'/' or
                    (byte)'\\')
                    start = cursor + 1;
                cursor++;
            }
            return start;
        });
        Register(baseAddress, DosLvo.FindSegment, "FindSegment", (state,
            invocation) =>
        {
            var layout = invocation.VersionLayout!;
            var definition = invocation.Definition.Version!;
            Require(!definition.System && !definition.File &&
                invocation.Forbidden && state.D[2] == 0 &&
                layout.FindSegmentCalls < 2 &&
                state.D[3] == unchecked((uint)layout.FindSegmentCalls) &&
                Bus.CString(state.D[1]) ==
                    (definition.Names?[0] ?? definition.Name),
                "Version command FindSegment lookup order differs.");
            layout.FindSegmentCalls++;
            return definition.CommandSegmentFound &&
                state.D[3] == unchecked((uint)definition.CommandSegmentSystem)
                    ? layout.CommandSegmentNode : 0u;
        });
        Register(baseAddress, -150, "LoadSeg", (state, invocation) =>
        {
            var layout = invocation.VersionLayout!;
            var definition = invocation.Definition.Version!;
            var ambient = definition.System &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                !layout.AmbientFileActive &&
                Bus.CString(state.D[1]) == layout.AmbientFileName;
            var file = definition.File &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                layout.FileCloseCalls == layout.FileOpenCalls &&
                Bus.CString(state.D[1]) == layout.FileName;
            var workbenchFile = definition.File &&
                IsWorkbench31VersionEntrySuite(suite) &&
                layout.FileCloseCalls == layout.FileOpenCalls &&
                Bus.CString(state.D[1]) == layout.FileName &&
                definition.FileContent.Length >= 4 &&
                definition.FileContent[0] == '\0' &&
                definition.FileContent[1] == '\0' &&
                definition.FileContent[2] == '\u0003' &&
                definition.FileContent[3] == '\u00f3';
            Require((ambient || file || workbenchFile) &&
                layout.AmbientLoadSegCalls == 0,
                "Version Ambient LoadSeg path or close ordering differs.");
            layout.AmbientLoadSegCalls++;
            if (!(ambient ? definition.AmbientLoadSegSuccess :
                definition.FileLoadSegSuccess)) return 0;
            layout.AmbientSegmentActive = true;
            return layout.AmbientSegmentRaw;
        });
        Register(baseAddress, -156, "UnLoadSeg", (state, invocation) =>
        {
            var layout = invocation.VersionLayout!;
            Require(layout.AmbientSegmentActive &&
                state.D[1] == layout.AmbientSegmentRaw &&
                layout.AmbientUnLoadSegCalls == 0,
                "Version Ambient UnLoadSeg segment differs.");
            layout.AmbientSegmentActive = false;
            layout.AmbientUnLoadSegCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Read, "Read", (state,
            invocation) =>
        {
            var layout = invocation.VersionLayout!;
            var definition = invocation.Definition.Version!;
            if (layout.AmbientFileActive &&
                state.D[1] == layout.AmbientFileHandle)
            {
                var ambientInFileBuffer =
                    state.D[2] >= layout.AmbientFileBuffer &&
                    (ulong)state.D[2] < (ulong)layout.AmbientFileBuffer +
                        VersionFileBytes;
                var ambientInScratchBuffer =
                    state.D[2] >= layout.AmbientFileScratch &&
                    (ulong)state.D[2] < (ulong)layout.AmbientFileScratch +
                        VersionScratchBytes;
                Require(state.D[2] != 0 &&
                    (ambientInFileBuffer || ambientInScratchBuffer) &&
                    state.D[3] > 0,
                    "Version Ambient file-read buffer differs.");
                var ambientBytes = Encoding.Latin1.GetBytes(
                    layout.AmbientFileContent ?? "");
                var ambientCount = Math.Min(checked((int)state.D[3]),
                    Math.Max(0, ambientBytes.Length -
                        layout.AmbientFilePosition));
                if (ambientCount > 0)
                    ambientBytes.AsSpan(layout.AmbientFilePosition,
                        ambientCount).CopyTo(Bus.Memory.AsSpan(
                            checked((int)state.D[2]), ambientCount));
                layout.AmbientFilePosition += ambientCount;
                layout.AmbientFileReadCalls++;
                return unchecked((uint)ambientCount);
            }
            var inFileBuffer = state.D[2] >= layout.FileBuffer &&
            (ulong)state.D[2] < (ulong)layout.FileBuffer +
                VersionFileBytes;
            var inScratchBuffer = state.D[2] >= layout.ScratchBuffer &&
                (ulong)state.D[2] < (ulong)layout.ScratchBuffer +
                    VersionScratchBytes;
            Require(state.D[1] == layout.FileHandle && state.D[2] != 0 &&
                (inFileBuffer || inScratchBuffer) &&
                state.D[3] > 0,
                "Version file-read buffer or handle differs.");
            var call = layout.FileReadCalls++;
            if (layout.LibraryFileActive)
                layout.LibraryFileReadCalls++;
            if (definition.FileReadFailure && call == 0 ||
                definition.FileReadFailureAt == call)
            {
                invocation.IoError = 205;
                return uint.MaxValue;
            }
            var content = layout.LibraryFileActive
                ? layout.LibraryFileContent! : definition.FileContent;
            var bytes = Encoding.Latin1.GetBytes(content);
            var count = Math.Min(checked((int)state.D[3]),
                Math.Max(0, bytes.Length - layout.FilePosition));
            if (count > 0)
                bytes.AsSpan(layout.FilePosition, count).CopyTo(
                    Bus.Memory.AsSpan(checked((int)state.D[2]), count));
            layout.FilePosition += count;
            return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.Close, "Close", (state,
            invocation) =>
        {
            var layout = invocation.VersionLayout!;
            if (layout.AmbientFileActive &&
                state.D[1] == layout.AmbientFileHandle)
            {
                layout.AmbientFileActive = false;
                layout.AmbientFileCloseCalls++;
                return 1;
            }
            if (layout.LibraryFileActive &&
                state.D[1] == layout.FileHandle)
            {
                layout.LibraryFileActive = false;
                layout.LibraryFileCloseCalls++;
            }
            Require(state.D[1] == layout.FileHandle &&
                layout.FileCloseCalls < layout.FileOpenCalls,
                "Version closed an unexpected file handle.");
            layout.FileCloseCalls++;
            invocation.VersionFileCloses++;
            invocation.IoError = 902;
            return 1;
        });
        Register(baseAddress, DosLvo.Seek64, "Seek64", (state,
            invocation) =>
        {
            var layout = invocation.VersionLayout!;
            var length = Encoding.Latin1.GetByteCount(
                invocation.Definition.Version!.FileContent);
            Require(state.D[1] == layout.FileHandle &&
                state.D[2] == uint.MaxValue &&
                state.D[3] == unchecked((uint)(-16_385)) &&
                unchecked((int)state.D[4]) ==
                    (int)DosConstants.OffsetEnd &&
                layout.FilePosition == 16_385,
                "Version v0 end-seek ABI differs.");
            layout.Seek64Calls++;
            layout.FilePosition = length - 16_385;
            state.D[1] = 0;
            return 16_385;
        }, preserveD1: true);
        Register(baseAddress, DosLvo.StrToLong, "StrToLong", (state,
            invocation) =>
        {
            var address = checked((int)state.D[1]);
            var cursor = 0;
            var negative = false;
            var first = (char)Bus.Memory[address];
            if (first == '+' || first == '-')
            {
                negative = first == '-';
                cursor++;
            }
            var firstDigit = cursor;
            var number = 0;
            while (address + cursor < Bus.Memory.Length)
            {
                var digit = (char)Bus.Memory[address + cursor];
                if (digit < '0' || digit > '9') break;
                number = unchecked(number * 10 + digit - '0');
                cursor++;
            }
            if (cursor == firstDigit) return uint.MaxValue;
            if (negative) number = -number;
            Bus.Long(state.D[2], unchecked((uint)number));
            return unchecked((uint)cursor);
        });
        Register(baseAddress, DosLvo.StrToDate, "StrToDate", (state,
            invocation) =>
        {
            var dateTime = state.D[1];
            Require(dateTime != 0 &&
                Bus.Memory[dateTime + (uint)DosLayout.DateTime.Flags] == 0 &&
                Bus.Long(dateTime + (uint)DosLayout.DateTime.Date) != 0,
                "Version StrToDate structure differs.");
            invocation.VersionDateParseCalls++;
            var format = Bus.Memory[dateTime +
                (uint)DosLayout.DateTime.Format];
            Require(format is 3 or 0,
                "Version StrToDate format order differs.");
            var date = Bus.CString(Bus.Long(dateTime +
                (uint)DosLayout.DateTime.Date));
            if (IsWorkbench31VersionEntrySuite(suite))
            {
                Require(format == 0, "Workbench fallback date format differs.");
                if (!DateTime.TryParseExact(date, "dd-MMM-yy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var parsed)) return 0;
                Bus.Long(dateTime, checked((uint)(parsed - new DateTime(1978, 1, 1)).Days));
                Bus.Long(dateTime + 4, 0);
                Bus.Long(dateTime + 8, 0);
                return 1;
            }
            if (date.Length != 10 || date[2] != '-' || date[5] != '-')
                return 0;
            Bus.Long(dateTime + (uint)DosLayout.DateTime.Stamp, 1);
            Bus.Long(dateTime + (uint)DosLayout.DateTime.Stamp + 4, 0);
            Bus.Long(dateTime + (uint)DosLayout.DateTime.Stamp + 8, 0);
            invocation.IoError = 0;
            return format == 3 ? 1u : 0u;
        });
        Register(baseAddress, DosLvo.DateToStr, "DateToStr", (state,
            invocation) =>
        {
            var dateTime = state.D[1];
            if (IsWorkbench31VersionEntrySuite(suite))
            {
                Require(Bus.Memory[dateTime + (uint)DosLayout.DateTime.Format] == 4 &&
                    Bus.Memory[dateTime + (uint)DosLayout.DateTime.Flags] == 0 &&
                    Bus.Long(dateTime + (uint)DosLayout.DateTime.Day) == 0 &&
                    Bus.Long(dateTime + (uint)DosLayout.DateTime.Time) == 0 &&
                    Bus.Long(dateTime + 4) == 0 &&
                    Bus.Long(dateTime + 8) == (invocation.Definition.Version!.WorkbenchDateSeconds == uint.MaxValue
                        ? unchecked((uint)-50) : 0),
                    "Workbench DateToStr date record differs.");
                var date = new DateTime(1978, 1, 1).AddDays(unchecked((int)Bus.Long(dateTime)));
                WriteCString(Bus.Long(dateTime + (uint)DosLayout.DateTime.Date),
                    date.ToString("MM/dd/yy", System.Globalization.CultureInfo.InvariantCulture));
                invocation.VersionDateFormatCalls++;
                return 1;
            }
            Require(Bus.Memory[dateTime +
                    (uint)DosLayout.DateTime.Format] == 4 &&
                Bus.Memory[dateTime +
                    (uint)DosLayout.DateTime.Flags] == 0 &&
                Bus.Long(dateTime + (uint)DosLayout.DateTime.Stamp) == 1 &&
                Bus.Long(dateTime + (uint)DosLayout.DateTime.Stamp + 4) == 0 &&
                Bus.Long(dateTime + (uint)DosLayout.DateTime.Stamp + 8) == 0 &&
                Bus.Long(dateTime + (uint)DosLayout.DateTime.Date) != 0,
                "Version DateToStr format differs.");
            WriteCString(Bus.Long(dateTime + (uint)DosLayout.DateTime.Date),
                "07-Jun-2023");
            invocation.VersionDateFormatCalls++;
            return 1;
        });
    }

    private void VerifyVersionEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Version!;
        var boundary = invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos;
        if (boundary)
        {
            Require(invocation.VersionReadArgsCalls == 0 &&
                invocation.VersionFindResidentCalls == 0,
                "Version crossed an invalid startup boundary.");
            return;
        }

        if (IsWorkbench31VersionEntrySuite(suite))
        {
            VerifyWorkbenchVersionEntry(invocation);
            return;
        }

        var parser = definition.ParserError != 0;
        var workbench = IsWorkbench31VersionEntrySuite(suite);
        var fileActive = definition.File && !definition.Resident &&
            !workbench;
        var openSucceeds = definition.FileOpenError == 0;
        var fileCount = definition.Names?.Length ?? 1;
        var allocationsPerFile = definition.Md5 ? 3 : 2;
        var fileAllocRequests = fileActive && !parser && openSucceeds
            ? definition.AllocationFailureAt switch
            {
                1 => 1,
                2 => 2,
                3 when definition.Md5 => 3,
                _ => fileCount * allocationsPerFile
            }
            : 0;
        var fileBuffersFreed = fileActive && !parser && openSucceeds
            ? definition.AllocationFailureAt switch
            {
                1 => 0,
                2 => 1,
                3 when definition.Md5 => 2,
                _ => fileCount * allocationsPerFile
            }
            : 0;
        var namedLookup = !workbench && !definition.System &&
            !definition.File;
        var volumeLookup = namedLookup && !definition.Resident &&
            !definition.ResidentFound && !definition.ExecLibraryFound &&
            !definition.ExecDeviceFound && !parser &&
            definition.Name.EndsWith(":", StringComparison.Ordinal);
        var volumeDeviceNodeSelected = !definition.VolumeDeviceIsVolume ||
            definition.VolumeDeviceFound;
        var volumeResidentSuccess = volumeLookup &&
            definition.VolumeDeviceProcFound && volumeDeviceNodeSelected &&
            definition.VolumeSegmentFound && definition.VolumeResidentFound;
        var volumeHandlerFailure = volumeLookup &&
            definition.VolumeDeviceProcFound && volumeDeviceNodeSelected &&
            definition.VolumeSegmentFound && !definition.VolumeResidentFound;
        var namedLibraryFileSearch = namedLookup &&
            !definition.ResidentFound && !definition.ExecLibraryFound &&
            !parser && definition.UtilityLibraryAvailable &&
            FilePartLength(definition.Name) != 0;
        var namedLibraryFilePaths = namedLibraryFileSearch
            ? MorphOSDirectoryCandidatePaths(definition.Name,
                !definition.ExecDeviceFound) : [];
        var namedLibraryFileHit = namedLibraryFileSearch &&
            definition.LibraryFileHitPath is not null;
        var namedLibraryFileHitIndex = namedLibraryFileHit
            ? Array.IndexOf(namedLibraryFilePaths,
                definition.LibraryFileHitPath!) : -1;
        Require(!namedLibraryFileHit || namedLibraryFileHitIndex >= 0,
            "Version library fixture hit path is outside the source search order.");
        var expectedLibraryFileOpenAttempts = namedLibraryFileHit
            ? namedLibraryFileHitIndex + 1
            : namedLibraryFilePaths.Length;
        var namedLibraryFileAllocationRequests = namedLibraryFileHit
            ? definition.Md5 ? 3 : 2
            : 0;
        var namedDirectoryPathAllocations = namedLibraryFileSearch ? 1 : 0;
        if (namedLibraryFileSearch && !definition.ExecDeviceFound &&
            (!namedLibraryFileHit || namedLibraryFileHitIndex >= 4))
            namedDirectoryPathAllocations++;
        var namedDirectFileSearch = namedLookup &&
            !definition.ResidentFound && !definition.Resident &&
            !definition.ExecLibraryFound && !definition.ExecDeviceFound &&
            !namedLibraryFileHit && !parser &&
            definition.UtilityLibraryAvailable &&
            FilePartLength(definition.Name) != 0 &&
            !definition.Name.EndsWith(":", StringComparison.Ordinal);
        var namedDirectFileHit = namedDirectFileSearch &&
            definition.DirectFileHit;
        var namedDirectFileAllocationRequests = namedDirectFileHit
            ? allocationsPerFile : 0;
        var commandSegmentVersionSuccess = !workbench &&
            definition.CommandSegmentFound &&
            !namedLibraryFileHit &&
            !string.IsNullOrEmpty(definition.CommandSegmentVersionText);
        var residentLookup = definition.Resident && !definition.System;
        var nameLookupAvailable = namedLookup || residentLookup;
        var namedLibraryVersionSuccess = namedLookup &&
            !definition.ResidentFound && definition.ExecLibraryFound &&
            !parser;
        var namedDeviceVersionSuccess = namedLookup &&
            !definition.ResidentFound && !definition.ExecLibraryFound &&
            definition.ExecDeviceFound &&
            !namedLibraryFileHit && !parser;
        var namedLibraryFullParse = namedLibraryVersionSuccess &&
            definition.Full;
        var namedDeviceFullParse = namedDeviceVersionSuccess &&
            definition.Full;
        var namedExecListNodeSuccess = namedLibraryVersionSuccess ||
            namedDeviceVersionSuccess;
        var namedLibraryAllocationRequests = namedExecListNodeSuccess
            ? namedLibraryFullParse || namedDeviceFullParse ? 3 : 1
            : 0;
        var commandSegmentLookup = namedLookup &&
            !definition.ResidentFound && !definition.ExecLibraryFound &&
            !parser &&
            (definition.UtilityLibraryAvailable || volumeLookup) &&
            !namedLibraryFileHit && !namedDeviceVersionSuccess &&
            !namedDirectFileHit && !volumeResidentSuccess &&
            !volumeHandlerFailure;
        var shellCommandSegmentLookup = commandSegmentLookup &&
            definition.CommandSegmentFound &&
            definition.CommandSegmentUseCount is -2 or -999;
        var shellCommandResidentSuccess = shellCommandSegmentLookup &&
            definition.ShellCommandResidentFound;
        var commandSegmentAllocRequests = commandSegmentLookup &&
            definition.CommandSegmentFound && !shellCommandSegmentLookup &&
            !parser
                ? 1 + (!string.IsNullOrEmpty(
                    definition.CommandSegmentVersionText) ? 1 : 0)
                : 0;
        var systemParse = !workbench && definition.System &&
            definition.VersionLibraryAvailable &&
            definition.VersionLibraryListed && !parser;
        var systemParseFailure = systemParse &&
            definition.SystemParseAllocationFailureAt != 0;
        var systemLookup = definition.System && !workbench;
        var ambientAttempt = systemLookup && definition.ResidentFound;
        var ambientPortLookup = ambientAttempt &&
            definition.RexxLibraryAvailable;
        var ambientMessageSent = ambientPortLookup &&
            definition.AmbientRexxPortAvailable;
        var ambientResultProvided = ambientMessageSent &&
            definition.AmbientRexxResult is not null;
        var ambientCopyAllocated = ambientResultProvided &&
            !definition.AmbientCopyAllocationFailure;
        var ambientRexxSuccess = ambientCopyAllocated &&
            IsValidAmbientResult(definition.AmbientRexxResult);
        var ambientFileCandidateContents = ambientAttempt &&
            !ambientRexxSuccess
                ? (definition.AmbientPathVariable is not null
                    ? new string?[] { definition.AmbientPathFileContent,
                        definition.AmbientMossysFileContent,
                        definition.AmbientSysFileContent }
                    : new string?[] { definition.AmbientMossysFileContent,
                        definition.AmbientSysFileContent })
                : [];
        var expectedAmbientFileOpenAttempts = 0;
        var expectedAmbientFileOpenSuccesses = 0;
        var expectedAmbientIsFileSystemCalls = 0;
        var expectedAmbientLoadSegCalls = 0;
        var expectedAmbientUnLoadSegCalls = 0;
        var expectedAmbientResidentTextAllocRequests = 0;
        var expectedAmbientFileDiagnostics = 0;
        var fileLoadSegCandidate = !workbench && definition.File &&
            !definition.FileContent.Contains("$VER:",
                StringComparison.Ordinal) &&
            definition.FileOpenError == 0 && !definition.FileReadFailure &&
            definition.FileReadFailureAt < 0 &&
            definition.AllocationFailureAt == 0 && !definition.CtrlC &&
            !definition.Resident;
        var fileLoadedResidentSuccess = false;
        if (fileLoadSegCandidate)
        {
            expectedAmbientIsFileSystemCalls++;
            if (definition.FileIsFileSystem)
            {
                expectedAmbientLoadSegCalls++;
                if (definition.FileLoadSegSuccess)
                {
                    expectedAmbientUnLoadSegCalls++;
                    fileLoadedResidentSuccess =
                        definition.FileLoadedResidentName is not null &&
                        definition.FileLoadedResidentIdString is not null;
                    if (fileLoadedResidentSuccess)
                        expectedAmbientResidentTextAllocRequests++;
                }
            }
        }
        foreach (var candidate in ambientFileCandidateContents)
        {
            expectedAmbientFileOpenAttempts++;
            if (candidate is not null)
                expectedAmbientFileOpenSuccesses++;
            var loadedResidentSuccess = false;
            if (candidate is not null &&
                !HasAmbientFileVersion(candidate))
            {
                expectedAmbientIsFileSystemCalls++;
                if (definition.AmbientIsFileSystem)
                {
                    expectedAmbientLoadSegCalls++;
                    if (definition.AmbientLoadSegSuccess)
                    {
                        expectedAmbientUnLoadSegCalls++;
                        if (definition.AmbientLoadedResidentName is not null &&
                            definition.AmbientLoadedResidentIdString is not null)
                        {
                            expectedAmbientResidentTextAllocRequests++;
                            loadedResidentSuccess = true;
                        }
                    }
                }
                if (!loadedResidentSuccess)
                    expectedAmbientFileDiagnostics++;
            }
            if (HasAmbientFileVersion(candidate) || loadedResidentSuccess)
                break;
        }
        var ambientFileSuccess = definition.AmbientExpectedVersion is not null &&
            !ambientRexxSuccess;
        var ambientSuccess = ambientRexxSuccess || ambientFileSuccess;
        var ambientFileAllocMemRequests = expectedAmbientFileOpenSuccesses * 2;
        var ambientFileFreeMemCalls = ambientFileAllocMemRequests;
        var systemAllocationRequests = systemParse
            ? definition.SystemParseAllocationFailureAt switch
            {
                1 => 1,
                _ => 2
            }
            : 0;
        var systemBuffersFreed = systemParse
            ? definition.SystemParseAllocationFailureAt switch
            {
                1 => 0,
                2 => 1,
                _ => 2
            }
            : 0;
        var residentParse = namedLookup &&
            (definition.ResidentFound || shellCommandResidentSuccess ||
             volumeResidentSuccess) &&
            !parser;
        var residentAllocationRequests = residentParse
            ? definition.ResidentParseAllocationFailureAt switch
            {
                1 => 1,
                _ => 2
            }
            : 0;
        var residentBuffersFreed = residentParse
            ? definition.ResidentParseAllocationFailureAt switch
            {
                1 => 0,
                2 => 1,
                _ => 2
            }
            : 0;
        var systemIdString = definition.WorkbenchIdString ??
            $"$VER: version.library {definition.WorkbenchVersion}.{definition.WorkbenchRevision} (07.06.2023) extra";
        var systemDateCalls = systemParse &&
            definition.SystemParseAllocationFailureAt == 0 &&
            systemIdString.Contains("(07.06.2023)",
                StringComparison.Ordinal) ? 1 : 0;
        var residentIdString = definition.ResidentIdString ??
            "$VER: dos.library 50.6 (fixture)";
        var residentDateParseCalls = residentParse &&
            definition.ResidentParseAllocationFailureAt == 0 &&
            residentIdString.Contains('(') &&
            !residentIdString.Contains("(07.06.2023)",
                StringComparison.Ordinal) ? 2 : residentParse &&
            definition.ResidentParseAllocationFailureAt == 0 &&
            residentIdString.Contains("(07.06.2023)",
                StringComparison.Ordinal) ? 1 : 0;
        var residentDateFormatCalls = residentParse &&
            definition.ResidentParseAllocationFailureAt == 0 &&
            residentIdString.Contains("(07.06.2023)",
                StringComparison.Ordinal) ? 1 : 0;
        var namedLibraryDateParseCalls = namedLibraryFullParse &&
            definition.ExecLibraryIdString.Contains("(07.06.2023)",
                StringComparison.Ordinal) ? 1 : namedLibraryFullParse &&
            definition.ExecLibraryIdString.Contains('(') ? 2 : 0;
        var namedLibraryDateFormatCalls = namedLibraryFullParse &&
            definition.ExecLibraryIdString.Contains("(07.06.2023)",
                StringComparison.Ordinal) ? 1 : 0;
        var namedDeviceDateParseCalls = namedDeviceFullParse &&
            definition.ExecDeviceIdString.Contains("(07.06.2023)",
                StringComparison.Ordinal) ? 1 : namedDeviceFullParse &&
            definition.ExecDeviceIdString.Contains('(') ? 2 : 0;
        var namedDeviceDateFormatCalls = namedDeviceFullParse &&
            definition.ExecDeviceIdString.Contains("(07.06.2023)",
                StringComparison.Ordinal) ? 1 : 0;
        var namedLibraryFileDateCalls = namedLibraryFileHit &&
            definition.LibraryFileContent!.Contains("(",
                StringComparison.Ordinal) ? 1 : 0;
        var expectedAllocMem = parser ? 1 :
            1 + fileAllocRequests + systemAllocationRequests +
            residentAllocationRequests + ambientFileAllocMemRequests +
            expectedAmbientResidentTextAllocRequests +
            commandSegmentAllocRequests + namedLibraryAllocationRequests +
            namedLibraryFileAllocationRequests +
            namedDirectFileAllocationRequests;
        var expectedAllocations = expectedAllocMem +
            (ambientResultProvided ? 1 : 0) +
            namedDirectoryPathAllocations;
        var expectedFreeMem = parser ? 1 :
            1 + fileBuffersFreed + systemBuffersFreed +
            residentBuffersFreed + ambientFileFreeMemCalls +
            expectedAmbientResidentTextAllocRequests +
            commandSegmentAllocRequests + namedLibraryAllocationRequests +
            namedLibraryFileAllocationRequests +
            namedDirectFileAllocationRequests;
        Require(invocation.VersionReadArgsCalls == 1 &&
            invocation.VersionFreeArgsCalls == (parser ? 0 : 1) &&
            invocation.Allocations == expectedAllocations &&
            invocation.FreeMem == expectedFreeMem &&
            invocation.VersionAllocMemCalls == expectedAllocMem &&
            invocation.VersionFreeMemCalls == expectedFreeMem,
            $"Version parser/result ownership differs (reads={invocation.VersionReadArgsCalls}, freeArgs={invocation.FreeArgs}, allocations={invocation.Allocations}, freeMem={invocation.FreeMem}, parser={parser}).");
        if (parser) return;

        Require(invocation.VersionLayout!.LibrarySearchPathAllocCalls ==
                namedDirectoryPathAllocations &&
            invocation.VersionLayout.LibrarySearchPathFreeCalls ==
                namedDirectoryPathAllocations &&
            invocation.VersionLayout.LibrarySearchPathBuffer == 0 &&
            invocation.VersionLayout.LibraryFileOpenAttempts ==
                expectedLibraryFileOpenAttempts &&
            invocation.VersionLayout.LibrarySearchPaths.SequenceEqual(
                namedLibraryFilePaths.Take(
                    expectedLibraryFileOpenAttempts)) &&
            invocation.VersionLayout.LibraryFileOpenSuccesses ==
                (namedLibraryFileHit ? 1 : 0) &&
            invocation.VersionLayout.LibraryFileCloseCalls ==
                (namedLibraryFileHit ? 1 : 0) &&
            invocation.VersionLayout.LibraryFileReadCalls ==
                (namedLibraryFileHit
                    ? definition.Md5 ? 3 : 2 : 0) &&
            !invocation.VersionLayout.LibraryFileActive,
            "Version MOSSYS:LIBS path order or file lifecycle differs.");
        Require(invocation.VersionLayout.DirectFileOpenAttempts ==
                (namedDirectFileSearch ? 1 : 0),
            "Version terminal direct-file fallback position differs.");
        if (namedDirectFileHit)
        {
            Require(invocation.VersionFileOpens == 1 &&
                invocation.VersionLayout.FileOpenCalls == 1 &&
                invocation.VersionLayout.FileCloseCalls == 1 &&
                invocation.VersionLayout.FileReadCalls ==
                    (definition.Md5 ? 3 : 2) &&
                invocation.VersionLayout.FileName == definition.Name,
                "Version direct-file fallback lifecycle differs.");
        }

        var fileFailure = definition.File &&
            (definition.FileOpenError != 0 || definition.FileReadFailure ||
                definition.FileReadFailureAt >= 0 ||
                definition.AllocationFailureAt != 0 || definition.CtrlC ||
                definition.Resident);
        var fileNoTag = definition.File &&
            !definition.FileContent.Contains("$VER:",
                StringComparison.Ordinal) && !fileFailure &&
            !fileLoadedResidentSuccess;
        var unsupported = definition.Md5 && !definition.File &&
                !definition.System && !nameLookupAvailable ||
            definition.Unit || definition.Internal ||
            workbench && (definition.File ||
                !definition.System && !definition.Resident);
        var layout = invocation.VersionLayout!;
        var expectedFindSegmentCalls = commandSegmentLookup
            ? definition.CommandSegmentFound
                ? definition.CommandSegmentSystem + 1 : 2
            : 0;
        var expectedFilePartCalls = namedLookup
            ? fileCount : 0;
        var expectedVolumeDeviceProcCalls = volumeLookup ? 1 : 0;
        var expectedVolumeDeviceProcFrees = volumeLookup &&
            definition.VolumeDeviceProcFound ? 1 : 0;
        var expectedVolumeListLocks = volumeLookup &&
            definition.VolumeDeviceProcFound &&
            definition.VolumeDeviceIsVolume ? 1 : 0;
        var expectedVolumeNextCalls = expectedVolumeListLocks != 0 ? 2 : 0;
        var expectedVolumeListUnlocks = expectedVolumeListLocks;
        Require(layout.AmbientLibraryOpenCalls == (ambientAttempt ? 1 : 0) &&
            layout.AmbientLibraryCloseCalls ==
                (ambientAttempt && definition.RexxLibraryAvailable ? 1 : 0) &&
            layout.AmbientPortCreateCalls ==
                (ambientPortLookup ? 1 : 0) &&
            layout.AmbientPortDeleteCalls ==
                (ambientPortLookup ? 1 : 0) &&
            layout.AmbientMessageCreateCalls ==
                (ambientPortLookup ? 1 : 0) &&
            layout.AmbientMessageDeleteCalls ==
                (ambientPortLookup ? 1 : 0) &&
            layout.AmbientCommandCreateCalls ==
                (ambientPortLookup ? 1 : 0) &&
            layout.AmbientCommandDeleteCalls ==
                (ambientPortLookup ? 1 : 0) &&
            layout.AmbientPortLookups == (ambientPortLookup ? 1 : 0) &&
            layout.AmbientPutCalls == (ambientMessageSent ? 1 : 0) &&
            layout.AmbientWaitCalls == (ambientMessageSent ? 1 : 0) &&
            layout.AmbientGetCalls == (ambientMessageSent ? 1 : 0) &&
            layout.AmbientResultDeleteCalls ==
                (ambientResultProvided ? 1 : 0) &&
            layout.AmbientCopyAllocCalls ==
                (ambientResultProvided ? 1 : 0) &&
            layout.AmbientCopyFreeCalls ==
                (ambientCopyAllocated ? 1 : 0) &&
            layout.AmbientGetVarCalls ==
                (ambientAttempt && !ambientRexxSuccess ? 1 : 0) &&
            layout.AmbientFileOpenAttempts ==
                expectedAmbientFileOpenAttempts &&
            layout.AmbientFileOpenSuccesses ==
                expectedAmbientFileOpenSuccesses &&
            layout.AmbientFileCloseCalls ==
                expectedAmbientFileOpenSuccesses &&
            layout.AmbientFileReadCalls ==
                expectedAmbientFileOpenSuccesses * 2 &&
            layout.AmbientFileAllocMemRequests ==
                ambientFileAllocMemRequests &&
            layout.AmbientFileAllocMemSuccesses ==
                ambientFileAllocMemRequests &&
            layout.AmbientFileFreeMemCalls == ambientFileFreeMemCalls &&
            layout.AmbientIsFileSystemCalls ==
                expectedAmbientIsFileSystemCalls &&
            layout.AmbientLoadSegCalls == expectedAmbientLoadSegCalls &&
            layout.AmbientUnLoadSegCalls == expectedAmbientUnLoadSegCalls &&
            layout.AmbientResidentTextAllocRequests ==
                expectedAmbientResidentTextAllocRequests &&
            layout.AmbientResidentTextAllocSuccesses ==
                expectedAmbientResidentTextAllocRequests &&
            layout.AmbientResidentTextFreeCalls ==
                expectedAmbientResidentTextAllocRequests &&
            layout.FindSegmentCalls == expectedFindSegmentCalls &&
            layout.FilePartCalls == expectedFilePartCalls &&
            layout.GetDeviceProcCalls == expectedVolumeDeviceProcCalls &&
            layout.FreeDeviceProcCalls == expectedVolumeDeviceProcFrees &&
            layout.LockDosListCalls == expectedVolumeListLocks &&
            layout.NextDosEntryCalls == expectedVolumeNextCalls &&
            layout.UnLockDosListCalls == expectedVolumeListUnlocks &&
            !layout.AmbientFileActive && !layout.AmbientSegmentActive &&
            layout.AmbientSetVarCalls == (ambientSuccess ? 1 : 0) &&
            Bus.Long(invocation.Process +
                (uint)DosLayout.Process.WindowPointer) == 0x1234_5678,
            $"Version Ambient message ownership, reply handling or window-pointer restoration differs (Rexx={layout.AmbientResultDeleteCalls}/{(ambientResultProvided ? 1 : 0)}, copyFree={layout.AmbientCopyFreeCalls}/{(ambientCopyAllocated ? 1 : 0)}, GetVar={layout.AmbientGetVarCalls}/{(ambientAttempt && !ambientRexxSuccess ? 1 : 0)}, opens={layout.AmbientFileOpenAttempts}/{expectedAmbientFileOpenAttempts}, opened={layout.AmbientFileOpenSuccesses}/{expectedAmbientFileOpenSuccesses}, closes={layout.AmbientFileCloseCalls}/{expectedAmbientFileOpenSuccesses}, reads={layout.AmbientFileReadCalls}/{expectedAmbientFileOpenSuccesses * 2}, alloc={layout.AmbientFileAllocMemRequests}/{ambientFileAllocMemRequests}, freed={layout.AmbientFileFreeMemCalls}/{ambientFileFreeMemCalls}, SetVar={layout.AmbientSetVarCalls}/{(ambientSuccess ? 1 : 0)}, window=0x{Bus.Long(invocation.Process + (uint)DosLayout.Process.WindowPointer):X8}).");
        var expectedFindResidentCalls = volumeLookup
            ? shellCommandSegmentLookup ? 1 : 0
            : !definition.File && (nameLookupAvailable || systemLookup)
                ? shellCommandSegmentLookup && !namedLibraryFileHit
                    ? 2 : 1 : 0;
        Require(invocation.VersionFindResidentCalls ==
            expectedFindResidentCalls,
            "Version resident lookup count differs.");
        Require(invocation.VersionPrintFaultCalls == (unsupported ||
            nameLookupAvailable && !definition.ResidentFound &&
                !namedLibraryFileHit &&
                !commandSegmentVersionSuccess &&
                !namedLibraryVersionSuccess &&
                !namedDeviceVersionSuccess &&
                !namedDirectFileHit &&
                !volumeResidentSuccess &&
                !volumeHandlerFailure &&
                !shellCommandResidentSuccess || fileFailure ||
            systemParseFailure && !ambientSuccess ||
            residentParse && definition.ResidentParseAllocationFailureAt != 0
                ? 1 : 0),
            "Version PrintFault count differs.");
        var fileOutput = definition.File && !fileFailure && !fileNoTag &&
            !unsupported && !residentLookup;
        var expectedVPrintf = definition.File
            ? fileNoTag ? definition.Md5 ? 0 : fileCount : fileOutput
                ? fileCount * (definition.Md5 ? 2 : 1) : 0
            : systemLookup
                ? !unsupported
                    ? (definition.ResidentFound ? 1 : 0) + 1 +
                        expectedAmbientFileDiagnostics +
                        (ambientSuccess ? 1 :
                        definition.VersionLibraryAvailable &&
                            definition.VersionLibraryListed &&
                            !systemParseFailure ? 1 : 0)
                    : 0
                : namedLibraryFileHit
                    ? definition.Md5 ? 2 : 1
                : namedDirectFileHit
                    ? definition.Md5 ? 2 : 1
                : volumeHandlerFailure ? 1
                : !unsupported && (!nameLookupAvailable || definition.ResidentFound ||
                    namedLibraryVersionSuccess ||
                    namedDeviceVersionSuccess ||
                    volumeResidentSuccess ||
                    namedLibraryFileHit ||
                    commandSegmentVersionSuccess ||
                    shellCommandResidentSuccess) ? 1 : 0;
        Require(invocation.VersionVPrintfCalls == expectedVPrintf,
            $"{definition.Name}: Version output count differs " +
            $"({invocation.VersionVPrintfCalls}/{expectedVPrintf}).");
        var expectedPutStr = definition.Md5 && !definition.File &&
            (definition.System || nameLookupAvailable &&
                (definition.ResidentFound || shellCommandResidentSuccess ||
                 volumeResidentSuccess))
                ? 1 : 0;
        Require(invocation.VersionPutStrCalls == expectedPutStr,
            "Version MD5 placeholder output count differs.");
        var expectedSeparators = systemLookup
            ? ambientSuccess ? 2 : (definition.ResidentFound ? 1 : 0) +
                (definition.VersionLibraryAvailable &&
                    definition.VersionLibraryListed &&
                    !systemParseFailure ? 1 : 0)
            : 0;
        Require(invocation.VersionSystemSeparatorCalls == expectedSeparators,
            "Version system component separator count differs.");
        var expectedVersionLibraryOpens = systemLookup ? 1 : 0;
        Require(invocation.VersionLibraryOpenCalls == expectedVersionLibraryOpens &&
            invocation.VersionLibraryCloseCalls ==
                (expectedVersionLibraryOpens != 0 &&
                    definition.VersionLibraryAvailable ? 1 : 0),
            "Version system version.library lifecycle differs.");
        var expectedForbid = systemLookup &&
            definition.VersionLibraryAvailable ? 1 : 0;
        if (namedLookup && !volumeLookup && !definition.ResidentFound && !parser &&
            definition.UtilityLibraryAvailable)
        {
            expectedForbid++;
            if (!namedLibraryVersionSuccess &&
                (!namedLibraryFileHit || namedLibraryFileHitIndex >= 4))
                expectedForbid++;
        }
        if (commandSegmentLookup)
            expectedForbid += shellCommandSegmentLookup ? 2 : 1;
        if (ambientPortLookup) expectedForbid++;
        Require(invocation.VersionForbidCalls == expectedForbid &&
            invocation.VersionPermitCalls == expectedForbid &&
            !invocation.Forbidden,
            $"{definition.Name}: Version LibList/DeviceList protection differs " +
            $"(Forbid={invocation.VersionForbidCalls}/{expectedForbid}, " +
            $"Permit={invocation.VersionPermitCalls}/{expectedForbid}).");
        var expectedUtilityOpen = namedLookup && !volumeLookup &&
            !definition.ResidentFound && !parser ? 1 : 0;
        var expectedUtilityClose = expectedUtilityOpen != 0 &&
            definition.UtilityLibraryAvailable ? 1 : 0;
        Require(invocation.VersionUtilityOpenCalls == expectedUtilityOpen &&
            invocation.VersionUtilityCloseCalls == expectedUtilityClose &&
            layout.UtilityStricmpCalls ==
                (namedLibraryVersionSuccess ? 1 : 0) +
                    (namedDeviceVersionSuccess ? 1 : 0),
            $"{definition.Name}: Version Utility comparison lease or call count differs " +
            $"(open {invocation.VersionUtilityOpenCalls}/{expectedUtilityOpen}, " +
            $"close {invocation.VersionUtilityCloseCalls}/{expectedUtilityClose}, " +
            $"Stricmp {layout.UtilityStricmpCalls}/{(namedLibraryVersionSuccess ? 1 : 0) + (namedDeviceVersionSuccess ? 1 : 0)}).");
        var fileDateCalls = fileActive && !fileFailure &&
            definition.FileContent.Contains("(", StringComparison.Ordinal)
                ? fileCount : 0;
        var namedDirectFileDateCalls = namedDirectFileHit &&
            definition.FileContent.Contains("(", StringComparison.Ordinal)
                ? 1 : 0;
        var fileResidentDateCalls = fileLoadedResidentSuccess &&
            definition.FileLoadedResidentIdString!.Contains("(",
                StringComparison.Ordinal) ? 1 : 0;
        Require(invocation.VersionDateParseCalls ==
                fileDateCalls + fileResidentDateCalls + systemDateCalls +
                    residentDateParseCalls +
                    namedLibraryDateParseCalls +
                    namedDeviceDateParseCalls +
                    namedLibraryFileDateCalls +
                    namedDirectFileDateCalls &&
            invocation.VersionDateFormatCalls ==
                    fileDateCalls + fileResidentDateCalls + systemDateCalls +
                    residentDateFormatCalls +
                    namedLibraryDateFormatCalls +
                    namedDeviceDateFormatCalls +
                    namedLibraryFileDateCalls +
                    namedDirectFileDateCalls,
            "Version date utility calls differ.");
        if (fileActive && !parser)
        {
            var expectedOpens = fileCount;
            Require(invocation.VersionFileOpens == expectedOpens &&
                invocation.VersionFileCloses ==
                    (openSucceeds ? fileCount : 0),
                "Version file handle lifecycle differs.");
            var hasMorphOsV0Header = definition.FileContent.Length >= 6 &&
                definition.FileContent[0] == '\u007f' &&
                definition.FileContent[1] == 'M' &&
                definition.FileContent[2] == 'O' &&
                definition.FileContent[3] == 'S' &&
                definition.FileContent[4] == '\0' &&
                definition.FileContent[5] == '\0';
            Require(invocation.VersionLayout!.Seek64Calls ==
                    (hasMorphOsV0Header && !fileFailure &&
                        !definition.Md5 ? 1 : 0),
                "Version v0 file seek optimization differs.");
        }
        invocation.VersionLayout = null;
    }

    private uint AllocateWorkbenchVersionBuffer(M68kCpuState state,
        Invocation invocation)
    {
        var definition = invocation.Definition.Version!;
        var layout = invocation.VersionLayout!;
        var expectedPathAllocations =
            ExpectedWorkbenchNamedFilePaths(definition).Count - 1;
        if (layout.WorkbenchPathAllocationCalls < expectedPathAllocations)
        {
            var separator = Math.Max(definition.Name.LastIndexOf(':'),
                Math.Max(definition.Name.LastIndexOf('/'),
                    definition.Name.LastIndexOf('\\')));
            var basenameBytes = Encoding.Latin1.GetByteCount(
                definition.Name[(separator + 1)..]);
            var bytes = unchecked((uint)(6 + basenameBytes));
            Require(state.D[0] == bytes && state.D[1] == 0,
                "Workbench default provider path allocation differs.");
            layout.WorkbenchPathAllocationCalls++;
            var path = Bus.Allocate(invocation, state.D[0],
                "WorkbenchVersionPath", true);
            layout.WorkbenchOwnedProviderPaths.Add((path, state.D[0]));
            return path;
        }
        Require(state.D[1] == (uint)Exec.MemoryFlags.Clear,
            "Workbench text allocation must be cleared.");
        var loadedNodeMatched = IsWorkbenchLibraryMatch(definition) ||
            IsWorkbenchDeviceMatch(definition);
        var commandSegmentFullExtra = definition.CommandSegmentFound &&
            definition.CommandSegmentUseCount is not (-2 or -999) &&
            definition.Full;
        if (loadedNodeMatched && definition.Full &&
            layout.WorkbenchNameAllocationCalls == 1 &&
            layout.WorkbenchLibraryIdAllocationCalls == 0)
        {
            var idString = IsWorkbenchLibraryMatch(definition)
                ? definition.ExecLibraryIdString
                : definition.ExecDeviceIdString;
            Require(state.D[0] == Encoding.Latin1.GetByteCount(idString) + 1,
                "Workbench loaded-list FULL IdString snapshot size differs.");
            layout.WorkbenchLibraryIdAllocationCalls++;
            layout.WorkbenchOwnedLibraryId = Bus.Allocate(invocation,
                state.D[0], "WorkbenchVersionIdString", true);
            return layout.WorkbenchOwnedLibraryId;
        }
        if ((definition.File || commandSegmentFullExtra) && definition.Full &&
            definition.WorkbenchFullExtra is not null &&
            layout.WorkbenchNameAllocationCalls == 0)
        {
            Require(state.D[0] == Encoding.Latin1.GetByteCount(
                    definition.WorkbenchFullExtra) + 1 &&
                layout.WorkbenchExtraAllocationCalls == 0,
                "Workbench FILE FULL extra allocation differs.");
            layout.WorkbenchExtraAllocationCalls++;
            if (definition.WorkbenchExtraAllocationFailure) return 0;
            layout.WorkbenchOwnedExtra = Bus.Allocate(invocation,
                state.D[0], "WorkbenchVersionExtra", true);
            return layout.WorkbenchOwnedExtra;
        }
        if ((layout.WorkbenchNameAllocationCalls != 0 ||
                layout.WorkbenchLibraryIdAllocationCalls != 0) &&
            definition.Full && definition.WorkbenchFullExtra is not null)
        {
            Require(definition.WorkbenchFullExtra is not null &&
                state.D[0] == Encoding.Latin1.GetByteCount(definition.WorkbenchFullExtra) + 1 &&
                layout.WorkbenchExtraAllocationCalls == 0,
                "Workbench FULL extra allocation differs.");
            layout.WorkbenchExtraAllocationCalls++;
            if (definition.WorkbenchExtraAllocationFailure) return 0;
            layout.WorkbenchOwnedExtra = Bus.Allocate(invocation,
                state.D[0], "WorkbenchVersionExtra", true);
            return layout.WorkbenchOwnedExtra;
        }
        Require(state.D[0] == layout.WorkbenchNameBefore.Length,
            "Workbench canonical-name allocation differs.");
        layout.WorkbenchNameAllocationCalls++;
        if (definition.ResidentCopyAllocationFailure) return 0;
        layout.WorkbenchOwnedName = Bus.Allocate(invocation,
            state.D[0], "WorkbenchVersionName", true);
        return layout.WorkbenchOwnedName;
    }

    private uint FreeWorkbenchVersionBuffer(M68kCpuState state,
        Invocation invocation)
    {
        var layout = invocation.VersionLayout!;
        for (var index = 0;
            index < layout.WorkbenchOwnedProviderPaths.Count; index++)
        {
            var path = layout.WorkbenchOwnedProviderPaths[index];
            if (path.Pointer != state.A[1]) continue;
            Bus.Release(invocation, path.Pointer,
                "WorkbenchVersionPath", path.Size);
            layout.WorkbenchOwnedProviderPaths.RemoveAt(index);
            layout.WorkbenchPathFreeCalls++;
            return 0;
        }
        if (state.A[1] == layout.WorkbenchOwnedExtra && state.A[1] != 0)
        {
            Require(layout.WorkbenchExtraFreeCalls == 0 && layout.WorkbenchNameFreeCalls == 0,
                "Workbench FULL extra release order differs.");
            Bus.Release(invocation, state.A[1], "WorkbenchVersionExtra");
            layout.WorkbenchExtraFreeCalls++;
            return 0;
        }
        if (state.A[1] == layout.WorkbenchOwnedLibraryId &&
            state.A[1] != 0)
        {
            Require(layout.WorkbenchLibraryIdFreeCalls == 0,
                "Workbench LibList IdString snapshot was released twice.");
            Bus.Release(invocation, state.A[1],
                "WorkbenchVersionIdString");
            layout.WorkbenchLibraryIdFreeCalls++;
            return 0;
        }
        Require(state.A[1] == layout.WorkbenchOwnedName &&
            layout.WorkbenchNameFreeCalls == 0,
            "Workbench canonical-name release differs.");
        Bus.Release(invocation, state.A[1], "WorkbenchVersionName");
        layout.WorkbenchNameFreeCalls++;
        return 0;
    }

    private static bool IsWorkbenchLibraryMatch(VersionEntryCase definition)
    {
        if (!definition.ExecLibraryFound || definition.System ||
            definition.File || definition.CommandSegmentFound ||
            definition.ResidentFound && string.Equals(definition.Name,
                definition.ResidentName, StringComparison.OrdinalIgnoreCase))
            return false;

        var deviceMatched = definition.WorkbenchDeviceListLookup &&
            definition.WorkbenchDeviceListEntryFound &&
            definition.WorkbenchDeviceListHasStartup &&
            definition.WorkbenchDeviceListSegmentFound &&
            definition.WorkbenchDeviceListResidentFound;
        if (deviceMatched) return false;

        var separator = Math.Max(definition.Name.LastIndexOf(':'),
            Math.Max(definition.Name.LastIndexOf('/'),
                definition.Name.LastIndexOf('\\')));
        var basename = definition.Name[(separator + 1)..];
        return string.Equals(basename, definition.ExecLibraryName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWorkbenchDeviceMatch(VersionEntryCase definition)
    {
        if (!definition.ExecDeviceFound || definition.System ||
            definition.File || definition.CommandSegmentFound ||
            definition.ResidentFound && string.Equals(definition.Name,
                definition.ResidentName, StringComparison.OrdinalIgnoreCase))
            return false;

        var dosDeviceMatched = definition.WorkbenchDeviceListLookup &&
            definition.WorkbenchDeviceListEntryFound &&
            definition.WorkbenchDeviceListHasStartup &&
            definition.WorkbenchDeviceListSegmentFound &&
            definition.WorkbenchDeviceListResidentFound;
        if (dosDeviceMatched) return false;

        var separator = Math.Max(definition.Name.LastIndexOf(':'),
            Math.Max(definition.Name.LastIndexOf('/'),
                definition.Name.LastIndexOf('\\')));
        var basename = definition.Name[(separator + 1)..];
        return string.Equals(basename, definition.ExecDeviceName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> ExpectedWorkbenchNamedFilePaths(
        VersionEntryCase definition)
    {
        var paths = new List<string>();
        if (definition.System || definition.File ||
            definition.CommandSegmentFound)
            return paths;
        var residentMatched = definition.ResidentFound &&
            string.Equals(definition.Name, definition.ResidentName,
                StringComparison.OrdinalIgnoreCase);
        var dosDeviceMatched = definition.WorkbenchDeviceListLookup &&
            definition.WorkbenchDeviceListEntryFound &&
            definition.WorkbenchDeviceListHasStartup &&
            definition.WorkbenchDeviceListSegmentFound &&
            definition.WorkbenchDeviceListResidentFound;
        if (residentMatched || dosDeviceMatched ||
            IsWorkbenchLibraryMatch(definition) ||
            IsWorkbenchDeviceMatch(definition))
            return paths;

        var separator = Math.Max(definition.Name.LastIndexOf(':'),
            Math.Max(definition.Name.LastIndexOf('/'),
                definition.Name.LastIndexOf('\\')));
        var basename = definition.Name[(separator + 1)..];
        var candidates = new[]
        {
            definition.Name,
            "LIBS:" + basename,
            "DEVS:" + basename
        };
        var hitPath = definition.WorkbenchFileHitPath ??
            (definition.DirectFileHit ? definition.Name : null);
        foreach (var candidate in candidates)
        {
            paths.Add(candidate);
            if (definition.FileOpenError != 0 &&
                (hitPath is null || hitPath == candidate))
                break;
            if (candidate == hitPath) break;
        }
        return paths;
    }

    private void VerifyWorkbenchVersionEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Version!;
        var layout = invocation.VersionLayout!;
        var utilityAvailable = definition.UtilityLibraryAvailable;
        var parsed = utilityAvailable && definition.ParserError == 0;
        var systemLookup = parsed && definition.System;
        Require(invocation.VersionLibraryOpenCalls ==
                (systemLookup ? 1 : 0) &&
            invocation.VersionLibraryCloseCalls ==
                (systemLookup && definition.VersionLibraryAvailable ? 1 : 0),
            "Workbench system version.library lifecycle differs.");
        var lookup = parsed && !definition.File && !definition.System;
        var residentMatched = lookup && definition.ResidentFound &&
            string.Equals(definition.Name, definition.ResidentName,
                StringComparison.OrdinalIgnoreCase);
        var commandSegmentLookup = lookup && !residentMatched;
        var commandSegmentFound = commandSegmentLookup &&
            definition.CommandSegmentFound;
        var shellCommandSegment = commandSegmentFound &&
            definition.CommandSegmentUseCount is -2 or -999;
        var shellMatched = shellCommandSegment &&
            definition.ShellCommandResidentFound;
        var commandSegmentVersion = commandSegmentFound &&
            !shellCommandSegment &&
            !string.IsNullOrEmpty(definition.CommandSegmentVersionText);
        var deviceListLookup = lookup && !residentMatched &&
            !commandSegmentFound && definition.WorkbenchDeviceListLookup;
        var deviceListResidentMatched = deviceListLookup &&
            definition.WorkbenchDeviceListEntryFound &&
            definition.WorkbenchDeviceListHasStartup &&
            definition.WorkbenchDeviceListSegmentFound &&
            definition.WorkbenchDeviceListResidentFound;
        var libraryListLookup = lookup && !residentMatched &&
            !commandSegmentFound &&
            (!deviceListLookup || !deviceListResidentMatched);
        var libraryMatched = libraryListLookup &&
            IsWorkbenchLibraryMatch(definition);
        var execDeviceListLookup = libraryListLookup && !libraryMatched;
        var execDeviceMatched = execDeviceListLookup &&
            IsWorkbenchDeviceMatch(definition);
        var matched = residentMatched || shellMatched ||
            deviceListResidentMatched || libraryMatched || execDeviceMatched;
        var copy = matched && !definition.ResidentCopyAllocationFailure;
        var filePath = parsed && definition.File && !definition.System;
        var fileOpen = filePath && definition.FileOpenError == 0;
        var namedFilePaths = parsed
            ? ExpectedWorkbenchNamedFilePaths(definition) : [];
        var namedFileSearch = namedFilePaths.Count != 0;
        var namedHitPath = definition.WorkbenchFileHitPath ??
            (definition.DirectFileHit ? definition.Name : null);
        var namedFileOpened = namedFileSearch &&
            definition.FileOpenError == 0 && namedHitPath is not null &&
            namedFilePaths[^1] == namedHitPath;
        var activeFileOpened = fileOpen || namedFileOpened;
        var fileAllocationFailure = activeFileOpened &&
            definition.AllocationFailureAt is 1 or 2;
        var fileBuffersReady = activeFileOpened &&
            definition.AllocationFailureAt != 1 &&
            definition.AllocationFailureAt != 2;
        var fileReadFailure = fileBuffersReady &&
            (definition.FileReadFailure || definition.FileReadFailureAt >= 0);
        var fileBytes = Encoding.Latin1.GetByteCount(definition.FileContent);
        var fileHunk = fileBytes >= 4 && definition.FileContent[0] == '\0' &&
            definition.FileContent[1] == '\0' &&
            definition.FileContent[2] == '\u0003' &&
            definition.FileContent[3] == '\u00f3';
        var fileTag = definition.FileContent.Contains("$VER:",
            StringComparison.Ordinal);
        var fileLoadSeg = fileBuffersReady && !fileReadFailure &&
            !fileTag && fileHunk;
        var fileLoadedResident = fileLoadSeg &&
            definition.FileLoadSegSuccess &&
            definition.FileLoadedResidentName is not null &&
            definition.FileLoadedResidentIdString is not null;
        var fileMissing = fileBuffersReady && !fileReadFailure &&
            !fileTag && !fileLoadedResident;
        var fileHasVersion = fileBuffersReady && !fileReadFailure &&
            (fileTag || fileLoadedResident);
        var unsupported = lookup && !matched && !commandSegmentVersion &&
            !namedFileOpened;
        var extra = (copy || fileHasVersion || commandSegmentVersion) && definition.Full &&
            definition.WorkbenchFullExtra is not null;
        var failed = !utilityAvailable || !parsed || unsupported ||
            matched && definition.ResidentCopyAllocationFailure ||
            filePath && definition.FileOpenError != 0 ||
            namedFileSearch && definition.FileOpenError != 0 ||
            fileReadFailure || fileAllocationFailure ||
            extra && definition.WorkbenchExtraAllocationFailure;
        Require(invocation.VersionReadArgsCalls ==
                (utilityAvailable ? 1 : 0) &&
            invocation.VersionFreeArgsCalls == (parsed ? 1 : 0) &&
            invocation.VersionFindResidentCalls == 0 &&
            layout.FindSegmentCalls == (commandSegmentLookup
                ? definition.CommandSegmentFound
                    ? definition.CommandSegmentSystem + 1 : 2
                : 0) && layout.FilePartCalls ==
                    (libraryListLookup ? 1 : 0) +
                    (namedFilePaths.Count > 1
                        ? namedFilePaths.Count - 1 : 0) &&
            invocation.VersionForbidCalls ==
                (commandSegmentLookup ? 1 : 0) +
                (libraryListLookup ? 1 : 0) +
                (execDeviceListLookup ? 1 : 0) &&
            invocation.VersionPermitCalls ==
                (commandSegmentLookup ? 1 : 0) +
                (libraryListLookup ? 1 : 0) +
                (execDeviceListLookup ? 1 : 0) &&
            layout.LockDosListCalls == (deviceListLookup ? 1 : 0) &&
            layout.FindDosEntryCalls == (deviceListLookup ? 1 : 0) &&
            layout.NextDosEntryCalls == 0 &&
            layout.UnLockDosListCalls == (deviceListLookup ? 1 : 0),
            "Workbench parser, first-provider or fallthrough boundary differs.");
        var expectedStricmpCalls = lookup && utilityAvailable
            ? (definition.ResidentTableDecoy ? 1 : 0) +
                (definition.ResidentFound || shellMatched ? 1 : 0) +
                (shellMatched
                    ? (definition.ResidentTableDecoy ? 1 : 0) + 1
                    : 0) +
                (libraryListLookup && definition.ExecLibraryFound ? 1 : 0) +
                (execDeviceListLookup && definition.ExecDeviceFound ? 1 : 0)
            : 0;
        Require(invocation.VersionUtilityOpenCalls == 1 &&
            invocation.VersionUtilityCloseCalls ==
                (utilityAvailable ? 1 : 0) &&
            layout.UtilityStricmpCalls == expectedStricmpCalls,
            $"Workbench Resident table traversal or Utility lease differs " +
            $"(open={invocation.VersionUtilityOpenCalls}/1, " +
            $"close={invocation.VersionUtilityCloseCalls}/{(utilityAvailable ? 1 : 0)}, " +
            $"Stricmp={layout.UtilityStricmpCalls}/{expectedStricmpCalls}, " +
            $"case={definition.Name}).");
        var libraryIdCopy = (libraryMatched || execDeviceMatched) && definition.Full &&
            !definition.ResidentCopyAllocationFailure;
        Require(layout.WorkbenchNameAllocationCalls == (matched ? 1 : 0) &&
            layout.WorkbenchNameFreeCalls == (copy ? 1 : 0) &&
            layout.WorkbenchLibraryIdAllocationCalls ==
                (libraryIdCopy ? 1 : 0) &&
            layout.WorkbenchLibraryIdFreeCalls ==
                (libraryIdCopy ? 1 : 0),
            "Workbench canonical-name ownership differs.");
        var fileAllocAttempts = activeFileOpened
            ? definition.AllocationFailureAt == 1 ? 1 : 2 : 0;
        var fileAllocSuccesses = fileAllocAttempts -
            (definition.AllocationFailureAt is 1 or 2 ? 1 : 0);
        var commandSegmentAllocations = commandSegmentFound &&
            !shellCommandSegment ? 1 +
                (!string.IsNullOrEmpty(definition.CommandSegmentVersionText)
                    ? 1 : 0) : 0;
        var expectedAllocMem = utilityAvailable
            ? 1 + fileAllocAttempts + commandSegmentAllocations : 0;
        var expectedFreeMem = utilityAvailable
            ? 1 + fileAllocSuccesses + commandSegmentAllocations : 0;
        Require(invocation.VersionAllocMemCalls == expectedAllocMem &&
            invocation.VersionFreeMemCalls == expectedFreeMem &&
            invocation.Allocations == expectedAllocMem &&
            invocation.FreeMem == expectedFreeMem,
            $"Workbench parser scratch ownership differs " +
            $"(allocMem={invocation.VersionAllocMemCalls}/{expectedAllocMem}, " +
            $"freeMem={invocation.VersionFreeMemCalls}/{expectedFreeMem}, " +
            $"allocations={invocation.Allocations}/{expectedFreeMem}, " +
            $"free={invocation.FreeMem}/{expectedFreeMem}, " +
            $"file={definition.Name}, open={fileOpen}, " +
            $"allocationFailure={definition.AllocationFailureAt}).");
        if (filePath || namedFileSearch)
        {
            var tagOffset = definition.FileContent.IndexOf("$VER:",
                StringComparison.Ordinal);
            var expectedReads = !activeFileOpened || !fileBuffersReady ? 0
                : fileReadFailure ? 1
                : tagOffset >= 0
                    ? tagOffset + 5 <= 16_384 ? 1
                        : 1 + (int)Math.Ceiling(
                            (tagOffset + 5 - 16_384) / 16_380d)
                    : fileBytes <= 16_384 ? 2
                        : 2 + (int)Math.Ceiling(
                            (fileBytes - 16_384) / 16_380d);
            var expectedOpenAttempts = filePath ? 1 :
                namedFilePaths.Count;
            var expectedOpenSuccesses = (fileOpen ? 1 : 0) +
                (namedFileOpened ? 1 : 0);
            Require(invocation.VersionFileOpens == expectedOpenAttempts &&
                invocation.VersionFileCloses == expectedOpenSuccesses &&
                layout.FileOpenCalls == expectedOpenAttempts &&
                layout.FileCloseCalls == expectedOpenSuccesses &&
                layout.FileReadCalls == expectedReads &&
                layout.AmbientLoadSegCalls == (fileLoadSeg ? 1 : 0) &&
                layout.AmbientUnLoadSegCalls ==
                    (fileLoadSeg && definition.FileLoadSegSuccess ? 1 : 0),
                $"Workbench file open, scan, close or HUNK LoadSeg lifecycle differs " +
                $"(open={invocation.VersionFileOpens}/{expectedOpenAttempts}, close={invocation.VersionFileCloses}/{expectedOpenSuccesses}, " +
                $"raw={layout.FileOpenCalls}/{expectedOpenAttempts}:{layout.FileCloseCalls}/{expectedOpenSuccesses}, " +
                $"read={layout.FileReadCalls}/{expectedReads}, " +
                $"load={layout.AmbientLoadSegCalls}/{(fileLoadSeg ? 1 : 0)}, " +
                $"unload={layout.AmbientUnLoadSegCalls}/{(fileLoadSeg && definition.FileLoadSegSuccess ? 1 : 0)}, " +
                $"path={definition.Name}, tag={fileTag}, hunk={fileHunk}, allocationFailure={definition.AllocationFailureAt}).");
            Require(layout.WorkbenchNamedFilePaths.SequenceEqual(
                    filePath ? [] : namedFilePaths) &&
                layout.WorkbenchNamedFileOpenSuccesses ==
                    (namedFileOpened ? 1 : 0),
                "Workbench named file provider order or match differs.");
        }
        else
        {
            Require(invocation.VersionFileOpens == 0 &&
                invocation.VersionFileCloses == 0 &&
                layout.AmbientLoadSegCalls == 0 &&
                layout.AmbientUnLoadSegCalls == 0 &&
                layout.WorkbenchNamedFilePaths.Count == 0,
                "Workbench non-FILE path performed direct file work.");
        }
        var expectedPathAllocations = namedFilePaths.Count > 1
            ? namedFilePaths.Count - 1 : 0;
        Require(layout.WorkbenchPathAllocationCalls ==
                expectedPathAllocations &&
            layout.WorkbenchPathFreeCalls == expectedPathAllocations &&
            layout.WorkbenchOwnedProviderPaths.Count == 0,
            "Workbench default path ownership differs.");
        Require(layout.WorkbenchExtraAllocationCalls == (extra ? 1 : 0) &&
            layout.WorkbenchExtraFreeCalls == (extra && !definition.WorkbenchExtraAllocationFailure ? 1 : 0),
            "Workbench FULL extra ownership differs.");
        if ((copy || fileHasVersion || commandSegmentVersion) && definition.Full)
        {
            var printed = !definition.WorkbenchExtraAllocationFailure;
            Require(layout.WorkbenchDate2AmigaCalls == definition.WorkbenchDate2AmigaExpected &&
                invocation.VersionDateParseCalls == definition.WorkbenchDateParseExpected &&
                invocation.VersionDateFormatCalls == definition.WorkbenchDateFormatExpected &&
                invocation.VersionVPrintfCalls == (printed ? 1 + definition.WorkbenchDateFormatExpected +
                    (extra ? 1 : 0) : 0) && layout.WorkbenchNewlineCalls == (printed ? 1 : 0),
                "Workbench FULL calls or source-shaped output sequence differ.");
        }
        Require(invocation.VersionPrintFaultCalls == (failed ? 1 : 0),
            "Workbench error diagnostic count differs.");
        Require(Bus.Memory.AsSpan((int)layout.WorkbenchBorrowedName,
                layout.WorkbenchNameBefore.Length).SequenceEqual(layout.WorkbenchNameBefore) &&
            Bus.Memory.AsSpan((int)layout.WorkbenchBorrowedIdString,
                layout.WorkbenchIdBefore.Length).SequenceEqual(layout.WorkbenchIdBefore) &&
            (!deviceListLookup || layout.WorkbenchParsedNameAtFreeArgs ==
                definition.Name),
            "Workbench lookup modified borrowed Resident text.");
        var expectedVPrintf = fileMissing ? 1
            : fileHasVersion
                ? definition.Full
                    ? definition.WorkbenchExtraAllocationFailure && extra
                        ? 0 : 1 + definition.WorkbenchDateFormatExpected +
                            (extra ? 1 : 0)
                    : 1
            : copy || commandSegmentVersion || deviceListResidentMatched
                ? definition.Full
                    ? definition.WorkbenchExtraAllocationFailure && extra
                        ? 0 : 1 + definition.WorkbenchDateFormatExpected +
                            (extra ? 1 : 0)
                    : 1
            : parsed && definition.System ? 2 : 0;
        Require(invocation.VersionVPrintfCalls == expectedVPrintf,
            "Workbench output count differs.");
    }
}
