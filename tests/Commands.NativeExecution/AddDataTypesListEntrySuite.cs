using System.Text;
using Amiga;
using Copper68k;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record AddDataTypesListEntryCase(
    bool UtilityAvailable = true,
    bool IffParseAvailable = true,
    bool LocaleAvailable = true,
    bool DataTypesAvailable = true,
    bool NamedObjectAvailable = true,
    bool UserDataAvailable = true,
    bool CtrlC = false,
    bool Files = false,
    bool FilesMatch = false,
    bool FilesDirectoryThenFile = false,
    bool FilesNestedDirectoryAfterFirstFile = false,
    bool WorkbenchFiles = false,
    bool WorkbenchArgumentListMissing = false,
    bool WorkbenchArgumentCountOutOfRange = false,
    bool FirstDirectoryAlreadyEntered = false,
    bool ValidDthd = false,
    bool ExistingDescriptor = false,
    bool ExistingDescriptorOpen = false,
    bool ExistingDescriptorSame = false,
    bool ExistingDescriptorHasSegment = false,
    bool ValidDtcd = false,
    bool InternalLoadSegFailure = false,
    bool Quiet = false,
    bool Refresh = false,
    bool RefreshSystemLockAvailable = false,
    bool RefreshMorphOsLockAvailable = false,
    bool RefreshSameLock = false,
    bool RefreshDatesEqual = false,
    bool RefreshEmptyDirectories = false,
    bool RefreshFilesMatch = false,
    bool List = false,
    int ParserError = 0,
    bool ResultAllocationFailure = false,
    bool Workbench31Profile = false,
    bool CreateNamedObject = false,
    bool CatalogAvailable = true,
    bool DosLibraryAvailable = true,
    bool IntuitionAvailable = true,
    int PatternCount = 2,
    uint DthdHeaderBytes = 50,
    uint DthdNameOffset = 32,
    uint DthdBaseNameOffset = 42,
    uint DthdPatternOffset = 47,
    uint DthdMaskOffset = 0,
    ushort DthdMaskLength = 0)
{
    public int FindCalls { get; set; }
    public int NamedObjectReleaseCalls { get; set; }
    public int ObtainCalls { get; set; }
    public int SemaphoreReleaseCalls { get; set; }
    public int UtilityOpenCalls { get; set; }
    public int UtilityCloseCalls { get; set; }
    public int IffParseOpenCalls { get; set; }
    public int IffParseCloseCalls { get; set; }
    public int LocaleOpenCalls { get; set; }
    public int LocaleCloseCalls { get; set; }
    public int DosLibraryOpenCalls { get; set; }
    public int DosLibraryCloseCalls { get; set; }
    public int IntuitionOpenCalls { get; set; }
    public int IntuitionCloseCalls { get; set; }
    public int CatalogOpenCalls { get; set; }
    public int CatalogCloseCalls { get; set; }
    public int NamedObjectAllocationCalls { get; set; }
    public int NamedObjectAddCalls { get; set; }
    public int NamedObjectFreeCalls { get; set; }
    public int InitSemaphoreCalls { get; set; }
    public int DataTypesOpenCalls { get; set; }
    public int DataTypesCloseCalls { get; set; }
    public int ResultAllocationCalls { get; set; }
    public int ResultFreeCalls { get; set; }
    public int ReadArgsCalls { get; set; }
    public int FreeArgsCalls { get; set; }
    public int AllocVecCalls { get; set; }
    public int AddTailCalls { get; set; }
    public int InsertCalls { get; set; }
    public int CheckSignalCalls { get; set; }
    public int FlushCalls { get; set; }
    public int PrintFaultCalls { get; set; }
    public int VPrintfCalls { get; set; }
    public int ExclusionParseCalls { get; set; }
    public int MatchFirstCalls { get; set; }
    public int MatchNextCalls { get; set; }
    public int MatchEndCalls { get; set; }
    public int AnchorFreeCalls { get; set; }
    public int ExclusionFreeCalls { get; set; }
    public int RefreshLockCalls { get; set; }
    public int RefreshUnLockCalls { get; set; }
    public int RefreshSameLockCalls { get; set; }
    public int RefreshDosObjectAllocCalls { get; set; }
    public int RefreshDosObjectFreeCalls { get; set; }
    public int RefreshExamineCalls { get; set; }
    public int RefreshExNextCalls { get; set; }
    public int RefreshCompareDatesCalls { get; set; }
    public int MatchPatternCalls { get; set; }
    public int CurrentDirCalls { get; set; }
    public int WorkbenchDupLockCalls { get; set; }
    public int WorkbenchUnlockCalls { get; set; }
    public int FileOpenCalls { get; set; }
    public int FileCloseCalls { get; set; }
    public int IffAllocCalls { get; set; }
    public int IffFreeCalls { get; set; }
    public int IffOpenCalls { get; set; }
    public int IffCloseCalls { get; set; }
    public int IffInitDosCalls { get; set; }
    public int IffPropChunksCalls { get; set; }
    public int IffCollectionChunksCalls { get; set; }
    public int IffStopOnExitCalls { get; set; }
    public int IffParseCalls { get; set; }
    public int IffFindPropCalls { get; set; }
    public int DatatypeAllocationCalls { get; set; }
    public int DatatypeFreeCalls { get; set; }
    public int ExistingDatatypeFreeCalls { get; set; }
    public int DatatypeRemoveCalls { get; set; }
    public int CodeCopyCalls { get; set; }
    public int CodeBufferFreeCalls { get; set; }
    public int LoaderStateFreeCalls { get; set; }
    public int InternalLoadSegCalls { get; set; }
    public int UnLoadSegCalls { get; set; }
    public uint UnLoadedSegment { get; set; }
    public int CallbackAllocCalls { get; set; }
    public int CallbackFreeCalls { get; set; }
    public uint CallbackAllocationAddress { get; set; }
    public uint LoaderReadCallback { get; set; }
    public uint LoaderAllocCallback { get; set; }
    public uint LoaderFreeCallback { get; set; }
    public uint SharedListAddress { get; set; }
    public uint SharedListBytes { get; set; }
    public uint ExistingDatatypeAddress { get; set; }
    public uint AllocationAddress { get; set; }
    public uint AllocationBytes { get; set; }
    public uint ResultArrayAddress { get; set; }
    public List<string> Events { get; } = [];
    public List<string> RefreshScanPatterns { get; } = [];

    public bool ContainsWrite(uint address, int size)
    {
        if (size <= 0)
            return false;
        var end = (ulong)address + (uint)size;
        return SharedListAddress != 0 && address >= SharedListAddress &&
                end <= (ulong)SharedListAddress + SharedListBytes ||
            AllocationAddress != 0 && address >= AllocationAddress &&
                end <= (ulong)AllocationAddress + AllocationBytes ||
            ExistingDatatypeAddress != 0 &&
                address >= ExistingDatatypeAddress &&
                end <= (ulong)ExistingDatatypeAddress +
                    NativeMorphOSAddDataTypesCommand.CompoundDataTypeSize;
    }
}

internal sealed partial class ProbeFixture
{
    public const string AddDataTypesListEntrySuite =
        "morphos320-adddatatypes-files-iff-dtcd-native-entry-vector-fixture";
    public const string AddDataTypesCallbackProbeSuite =
        "morphos320-adddatatypes-dtcd-callback-wrapper-probe";
    public const string Workbench31AddDataTypesEntrySuite =
        "workbench31-adddatatypes-native-entry-vector-fixture";

    private const uint AddDataTypesUtilityBase = 0xa000;
    private const uint AddDataTypesIffParseBase = 0xa100;
    private const uint AddDataTypesLocaleBase = 0xa120;
    private const uint AddDataTypesWorkbenchIffParseBase = 0xa700;
    private const uint AddDataTypesWorkbenchLocaleBase = 0xa900;
    private const uint AddDataTypesDatatypesBase = 0xa140;
    private const uint AddDataTypesWorkbenchDosBase = 0xad00;
    private const uint AddDataTypesIntuitionBase = 0xab00;
    private const uint AddDataTypesNamedObject = 0xa200;
    private const uint AddDataTypesSharedList = 0xa400;
    private const uint AddDataTypesAllocationBase = 0xc000;
    private const uint AddDataTypesReadArgs = 0xb600;
    private const uint AddDataTypesFilesVector = 0xb700;
    private const uint AddDataTypesFileText0 = 0xb720;
    private const uint AddDataTypesFileText1 = 0xb740;
    private const uint AddDataTypesExclusion = 0xc400;
    private const uint AddDataTypesAnchor = 0xc500;
    private const uint AddDataTypesCurrentChain = 0xc700;
    private const uint AddDataTypesIffHandle = 0xc800;
    private const uint AddDataTypesFileHandle = 0x2468;
    private const uint AddDataTypesLockBptr = 0x3456;
    private const uint AddDataTypesMorphOsLockBptr = 0x4567;
    private const uint AddDataTypesWorkbenchDuplicateLock = 0x5678;
    private const uint AddDataTypesRefreshFib = 0xe100;
    private const uint AddDataTypesOldDirectoryBptr = 0x1234;
    private const uint AddDataTypesStoredProperty = 0xc900;
    private const uint AddDataTypesFileHeader = 0xc920;
    private const uint AddDataTypesStoredCode = 0xc910;
    private const uint AddDataTypesCodeProperty = 0xc940;
    private const uint AddDataTypesCodeBuffer = 0xc600;
    private const uint AddDataTypesLoaderState = 0xc620;
    private const uint AddDataTypesCodeBytes = 8;
    private const uint AddDataTypesLoadedSegment = 0x5123;
    private const uint AddDataTypesExistingSegment = 0x5124;
    private const uint AddDataTypesRegisteredCompound = 0xd000;
    private const uint AddDataTypesExistingCompound = 0xe500;
    private const uint AddDataTypesExistingName = 0xe700;
    private const uint AddDataTypesExistingBaseName = 0xe720;
    private const uint AddDataTypesExistingPattern = 0xe740;
    private const uint AddDataTypesFileHeaderBytes = 50;

    private List<object> RunAddDataTypesCallbackProbeCases()
    {
        var test = new ProbeCase("dtcd-callback-wrappers", "", DOS.RETURN_OK,
            Invocation.InitialIoError, "")
        { AddDataTypesList = new() };
        var reports = Execute([test], false);
        Bus.AssertImageUnchanged();
        return reports;
    }

    private List<object> RunAddDataTypesListEntryCases()
    {
        ProbeCase[] cases =
        [
            AddDataTypesListCase("initialize-builtins-and-list",
                new(List: true), arguments: "LIST"),
            AddDataTypesListCase("list-control-c",
                new(CtrlC: true, List: true), error: (int)DOS.Error.Break,
                arguments: "LIST"),
            AddDataTypesListCase("no-options", new()),
            AddDataTypesListCase("files-option-slot",
                new(Files: true), arguments: "datatype-a datatype-b"),
            AddDataTypesListCase("files-matchfirst-no-entries",
                new(Files: true, PatternCount: 1), arguments: "datatype-a"),
            AddDataTypesListCase("files-match-one-iff-no-header",
                new(Files: true, FilesMatch: true, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("files-match-short-dthd-header-rejected",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    DthdHeaderBytes: 31, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("files-match-dthd-name-offset-out-of-range",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    DthdNameOffset: 50, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("files-match-dthd-pattern-not-terminated",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    DthdPatternOffset: 49, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("files-match-dthd-mask-range-out-of-bounds",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    DthdMaskOffset: 48, DthdMaskLength: 2,
                    PatternCount: 1), arguments: "datatype-a"),
            AddDataTypesListCase("files-match-valid-dthd-registers",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    PatternCount: 1), arguments: "datatype-a"),
            AddDataTypesListCase("files-match-open-duplicate-preserved",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    ExistingDescriptor: true, ExistingDescriptorOpen: true,
                    ExistingDescriptorSame: true, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("files-match-same-duplicate-updates-ids",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    ExistingDescriptor: true, ExistingDescriptorSame: true,
                    PatternCount: 1), arguments: "datatype-a"),
            AddDataTypesListCase("files-match-replaces-closed-duplicate",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    ExistingDescriptor: true, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("files-match-replaces-closed-segmented-duplicate-unloads-old-segment",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    ExistingDescriptor: true,
                    ExistingDescriptorHasSegment: true, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("files-match-open-segmented-duplicate-keeps-live-segment",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    ValidDtcd: true, ExistingDescriptor: true,
                    ExistingDescriptorOpen: true,
                    ExistingDescriptorSame: true,
                    ExistingDescriptorHasSegment: true, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("files-match-same-segmented-duplicate-unloads-candidate-segment",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    ValidDtcd: true, ExistingDescriptor: true,
                    ExistingDescriptorSame: true,
                    ExistingDescriptorHasSegment: true, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("files-match-valid-dthd-dtcd-loads",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    ValidDtcd: true, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("files-enters-first-directory-once",
                new(Files: true, FilesMatch: true,
                    FilesDirectoryThenFile: true, ValidDthd: true,
                    PatternCount: 1), arguments: "datatype-a"),
            AddDataTypesListCase("files-clears-diddir-without-reentering",
                new(Files: true, FilesMatch: true,
                    FilesDirectoryThenFile: true,
                    FirstDirectoryAlreadyEntered: true,
                    PatternCount: 1), arguments: "datatype-a"),
            AddDataTypesListCase("files-scans-past-nested-directory-without-recursing",
                new(Files: true, FilesMatch: true,
                    FilesDirectoryThenFile: true,
                    FilesNestedDirectoryAfterFirstFile: true,
                    PatternCount: 1), arguments: "datatype-a"),
            new ProbeCase("workbench-startup-loads-argument-under-its-lock",
                "", DOS.RETURN_OK, Invocation.InitialIoError, "")
            {
                Workbench = true,
                AddDataTypesList = new(WorkbenchFiles: true,
                    PatternCount: 1)
            },
            new ProbeCase("workbench-startup-loads-dthd-dtcd-with-morphos-stack",
                "", DOS.RETURN_OK, Invocation.InitialIoError, "")
            {
                Workbench = true,
                AddDataTypesList = new(WorkbenchFiles: true,
                    ValidDthd: true, ValidDtcd: true, PatternCount: 1)
            },
            new ProbeCase("workbench-startup-no-file-arguments",
                "", DOS.RETURN_OK, Invocation.InitialIoError, "")
            {
                Workbench = true,
                AddDataTypesList = new(PatternCount: 1)
            },
            new ProbeCase("workbench-startup-missing-argument-list",
                "", DOS.RETURN_FAIL,
                (int)DOS.Error.RequiredArgumentMissing, "")
            {
                Workbench = true,
                AddDataTypesList = new(
                    WorkbenchArgumentListMissing: true,
                    PatternCount: 1)
            },
            new ProbeCase("workbench-startup-rejects-wide-argument-range-wrap",
                "", DOS.RETURN_FAIL,
                (int)DOS.Error.RequiredArgumentMissing, "")
            {
                Workbench = true,
                AddDataTypesList = new(
                    WorkbenchArgumentCountOutOfRange: true,
                    PatternCount: 1)
            },
            new ProbeCase("workbench-startup-utility-open-failure",
                "", DOS.RETURN_FAIL, Invocation.InitialIoError, "")
            {
                Workbench = true,
                AddDataTypesList = new(UtilityAvailable: false,
                    PatternCount: 1)
            },
            AddDataTypesListCase("files-match-dtcd-load-failure-cleans-up",
                new(Files: true, FilesMatch: true, ValidDthd: true,
                    ValidDtcd: true, InternalLoadSegFailure: true,
                    PatternCount: 1), arguments: "datatype-a"),
            AddDataTypesListCase("quiet-option-slot",
                new(Quiet: true), arguments: "QUIET"),
            AddDataTypesListCase("refresh-option-slot",
                new(Refresh: true), arguments: "REFRESH"),
            AddDataTypesListCase("refresh-same-lock-scanned-once",
                new(Refresh: true, RefreshSystemLockAvailable: true,
                    RefreshMorphOsLockAvailable: true,
                    RefreshSameLock: true), arguments: "REFRESH"),
            AddDataTypesListCase("refresh-unchanged-directories-skipped",
                new(Refresh: true, RefreshSystemLockAvailable: true,
                    RefreshMorphOsLockAvailable: true,
                    RefreshDatesEqual: true), arguments: "REFRESH"),
            AddDataTypesListCase("refresh-empty-directories-still-scanned",
                new(Refresh: true, RefreshSystemLockAvailable: true,
                    RefreshMorphOsLockAvailable: true,
                    RefreshDatesEqual: true,
                    RefreshEmptyDirectories: true), arguments: "REFRESH"),
            AddDataTypesListCase("all-option-slots",
                new(Files: true, Quiet: true, Refresh: true, List: true),
                arguments: "datatype-a datatype-b QUIET REFRESH LIST"),
            AddDataTypesListCase("readargs-error",
                new(ParserError: (int)DOS.Error.BadTemplate), DOS.RETURN_FAIL,
                (int)DOS.Error.BadTemplate),
            AddDataTypesListCase("result-array-allocation-failure",
                new(ResultAllocationFailure: true), DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore),
            AddDataTypesListCase("utility-open-failure",
                new(UtilityAvailable: false), DOS.RETURN_FAIL),
            AddDataTypesListCase("iffparse-open-failure",
                new(IffParseAvailable: false), DOS.RETURN_FAIL),
            AddDataTypesListCase("locale-open-failure",
                new(LocaleAvailable: false), DOS.RETURN_FAIL),
            AddDataTypesListCase("datatypes-open-failure",
                new(DataTypesAvailable: false), DOS.RETURN_FAIL),
            AddDataTypesListCase("named-object-missing",
                new(NamedObjectAvailable: false), DOS.RETURN_FAIL),
            AddDataTypesListCase("named-object-null-userdata",
                new(UserDataAvailable: false), DOS.RETURN_FAIL)
        ];

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private List<object> RunWorkbench31AddDataTypesEntryCases()
    {
        ProbeCase[] cases =
        [
            AddDataTypesListCase("classic-cli-existing-shared-object",
                new(Workbench31Profile: true), arguments: ""),
            AddDataTypesListCase("classic-files-slot-scans-pattern",
                new(Workbench31Profile: true, Files: true,
                    PatternCount: 1), arguments: "datatype-a"),
            AddDataTypesListCase("classic-files-multiple-patterns",
                new(Workbench31Profile: true, Files: true,
                    PatternCount: 2), arguments: "datatype-a datatype-b"),
            AddDataTypesListCase("classic-files-loads-and-registers-dthd",
                new(Workbench31Profile: true, Files: true,
                    FilesMatch: true, ValidDthd: true, PatternCount: 1),
                arguments: "datatype-a"),
            AddDataTypesListCase("classic-files-loads-dtcd-with-4096-byte-stack",
                new(Workbench31Profile: true, Files: true,
                    FilesMatch: true, ValidDthd: true, ValidDtcd: true,
                    PatternCount: 1), arguments: "datatype-a"),
            AddDataTypesListCase("classic-readargs-quiet-slot-and-optional-catalog",
                new(Workbench31Profile: true, CatalogAvailable: false,
                    Quiet: true), arguments: "QUIET"),
            AddDataTypesListCase("classic-refresh-changed-date-scans-wildcard",
                new(Workbench31Profile: true, Refresh: true,
                    RefreshSystemLockAvailable: true), arguments: "REFRESH"),
            AddDataTypesListCase("classic-files-quiet-refresh-slots-and-precedence",
                new(Workbench31Profile: true, Files: true, Quiet: true,
                    Refresh: true, RefreshSystemLockAvailable: true),
                arguments: "datatype-a QUIET REFRESH"),
            AddDataTypesListCase("classic-refresh-unchanged-date-skips-wildcard",
                new(Workbench31Profile: true, Refresh: true,
                    RefreshSystemLockAvailable: true,
                    RefreshDatesEqual: true), arguments: "REFRESH"),
            AddDataTypesListCase("classic-refresh-empty-directory-scans-wildcard",
                new(Workbench31Profile: true, Refresh: true,
                    RefreshSystemLockAvailable: true,
                    RefreshEmptyDirectories: true), arguments: "REFRESH"),
            AddDataTypesListCase("classic-refresh-missing-date-lock-scans-wildcard",
                new(Workbench31Profile: true, Refresh: true,
                    RefreshSystemLockAvailable: false), arguments: "REFRESH"),
            AddDataTypesListCase("classic-creates-and-publishes-shared-object",
                new(Workbench31Profile: true, CreateNamedObject: true),
                arguments: ""),
            AddDataTypesListCase("classic-created-object-with-null-user-space-is-released",
                new(Workbench31Profile: true, CreateNamedObject: true,
                    UserDataAvailable: false), DOS.RETURN_FAIL),
            AddDataTypesListCase("classic-locale-open-failure-unwinds-leases",
                new(Workbench31Profile: true, LocaleAvailable: false),
                DOS.RETURN_FAIL),
            AddDataTypesListCase("classic-readargs-failure-releases-shared-state",
                new(Workbench31Profile: true,
                    ParserError: (int)DOS.Error.BadTemplate),
                DOS.RETURN_FAIL, (int)DOS.Error.BadTemplate)
        ];

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase AddDataTypesListCase(string name,
        AddDataTypesListEntryCase definition, int result = DOS.RETURN_OK,
        int? error = null, string arguments = "") =>
        new(name, arguments, result, error ??
            (definition.CtrlC && definition.List
                ? (int)DOS.Error.Break
                : definition.ParserError != 0
                    ? definition.ParserError
                    : definition.ResultAllocationFailure
                        ? (int)DOS.Error.NoFreeStore
                        : Invocation.InitialIoError),
            definition.CtrlC && definition.List ||
                !definition.List || !definition.UtilityAvailable ||
                !definition.NamedObjectAvailable || !definition.UserDataAvailable
                ? ""
                : "ascii, \"ascii\"\n" +
                    "binary, \"binary\"\n" +
                    "directory, \"directory\"\n" +
                    "iff, \"iff\"\n")
        { AddDataTypesList = definition };

    private void PrepareAddDataTypesListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.AddDataTypesList!;
        definition.SharedListAddress = AddDataTypesSharedList;
        definition.SharedListBytes = definition.Workbench31Profile
            ? NativeWorkbench31AddDataTypesCommand.NamedListUserSpaceBytes
            : NativeMorphOSAddDataTypesCommand.DataTypesListSize;
        definition.AllocationAddress = AddDataTypesAllocationBase;
        definition.AllocationBytes = 0x2000;
        Bus.Memory.AsSpan((int)AddDataTypesNamedObject, 0x400).Clear();
        Bus.Long(AddDataTypesNamedObject,
            definition.UserDataAvailable && !definition.CreateNamedObject
                ? AddDataTypesSharedList : 0);
        foreach (var offset in new[]
        {
            NativeMorphOSAddDataTypesCommand.DataTypesListSorted,
            NativeMorphOSAddDataTypesCommand.DataTypesListBinary,
            NativeMorphOSAddDataTypesCommand.DataTypesListAscii,
            NativeMorphOSAddDataTypesCommand.DataTypesListIff,
            NativeMorphOSAddDataTypesCommand.DataTypesListMisc
        })
            InitializeEmptyList(AddDataTypesSharedList + (uint)offset);
        if (definition.Refresh)
            Bus.Long(invocation.Process +
                (uint)DosLayout.Process.WindowPointer, 0x12345678);
        definition.ExistingDatatypeAddress = definition.ExistingDescriptor
            ? AddDataTypesExistingCompound : 0;

        Bus.Memory.AsSpan((int)AddDataTypesFilesVector, 0x100).Clear();
        Bus.Memory.AsSpan((int)AddDataTypesCurrentChain, 0x200).Clear();
        Bus.Memory.AsSpan((int)AddDataTypesStoredProperty, 0x100).Clear();
        if (definition.ValidDtcd)
        {
            Bus.Long(AddDataTypesStoredCode, AddDataTypesCodeBytes);
            Bus.Long(AddDataTypesStoredCode + 4, AddDataTypesCodeProperty);
            for (var index = 0u; index < AddDataTypesCodeBytes; index++)
                Bus.Memory[AddDataTypesCodeProperty + index] =
                    unchecked((byte)(0x70 + index));
        }
        if (definition.ValidDthd)
        {
            Bus.Long(AddDataTypesStoredProperty,
                definition.DthdHeaderBytes);
            Bus.Long(AddDataTypesStoredProperty + 4,
                AddDataTypesFileHeader);
            Bus.Long(AddDataTypesFileHeader,
                definition.DthdNameOffset);
            Bus.Long(AddDataTypesFileHeader + 4,
                definition.DthdBaseNameOffset);
            Bus.Long(AddDataTypesFileHeader + 8,
                definition.DthdPatternOffset);
            Bus.Long(AddDataTypesFileHeader + 12,
                definition.DthdMaskOffset);
            Bus.Long(AddDataTypesFileHeader + 16, 0x74657374);
            Bus.Long(AddDataTypesFileHeader + 20, 0x64617461);
            Bus.Word(AddDataTypesFileHeader + 24,
                definition.DthdMaskLength);
            Bus.Word(AddDataTypesFileHeader + 26, 0);
            Bus.Word(AddDataTypesFileHeader + 28, 0);
            Bus.Word(AddDataTypesFileHeader + 30, 0);
            WriteAddDataTypesCString(AddDataTypesFileHeader + 32,
                "test-type");
            WriteAddDataTypesCString(AddDataTypesFileHeader + 42,
                "test");
            WriteAddDataTypesCString(AddDataTypesFileHeader +
                definition.DthdPatternOffset, "#?");
        }
        if (definition.Files)
        {
            Bus.Long(AddDataTypesFilesVector, AddDataTypesFileText0);
            if (definition.PatternCount > 1)
                Bus.Long(AddDataTypesFilesVector + 4,
                    AddDataTypesFileText1);
            WriteAddDataTypesCString(AddDataTypesFileText0, "datatype-a");
            WriteAddDataTypesCString(AddDataTypesFileText1, "datatype-b");
        }

        if (invocation.Definition.Workbench)
        {
            Bus.Long(invocation.Message + 28,
                definition.WorkbenchArgumentCountOutOfRange
                    ? 0x0001_0001u
                    : definition.WorkbenchArgumentListMissing
                        || definition.WorkbenchFiles ? 2u : 1u);
            Bus.Long(invocation.Message + 36,
                definition.WorkbenchArgumentListMissing
                    ? 0u
                    : definition.WorkbenchArgumentCountOutOfRange
                        ? 0xffff_0000u : invocation.Arguments);
            if (!definition.WorkbenchArgumentListMissing &&
                !definition.WorkbenchArgumentCountOutOfRange)
            {
                Bus.Long(invocation.Arguments, AddDataTypesLockBptr);
                Bus.Long(invocation.Arguments + 4, AddDataTypesFileText0);
                WriteAddDataTypesCString(AddDataTypesFileText0,
                    "AddDataTypes");
                if (definition.WorkbenchFiles)
                {
                    Bus.Long(invocation.Arguments + 8,
                        AddDataTypesMorphOsLockBptr);
                    Bus.Long(invocation.Arguments + 12,
                        AddDataTypesFileText1);
                    WriteAddDataTypesCString(AddDataTypesFileText1,
                        "datatype-b");
                }
            }
        }
    }

    private void SeedExistingDatatype(AddDataTypesListEntryCase definition)
    {
        var compound = AddDataTypesExistingCompound;
        var header = compound + (uint)NativeMorphOSAddDataTypesCommand
            .CompoundDataTypeInlineHeader;
        var name = definition.ExistingDescriptorSame
            ? "TEST-TYPE"
            : "test-type";
        var baseName = definition.ExistingDescriptorSame
            ? "test"
            : "legacy";
        Bus.Memory.AsSpan((int)compound,
            (int)NativeMorphOSAddDataTypesCommand.CompoundDataTypeSize).Clear();
        Bus.Long(compound +
            (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeHeaderPointer,
            header);
        Bus.Long(compound +
            (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeLength,
            NativeMorphOSAddDataTypesCommand.CompoundDataTypeInlineHeader +
                NativeMorphOSAddDataTypesCommand.DataTypeHeaderSize);
        Bus.Long(compound +
            (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeFlagLong, 1);
        Bus.Long(compound +
            (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeOpenCount,
            definition.ExistingDescriptorOpen ? 1u : 0u);
        if (definition.ExistingDescriptorHasSegment)
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeSegment,
                AddDataTypesExistingSegment);
        Bus.Long(header, AddDataTypesExistingName);
        Bus.Long(header + 4, AddDataTypesExistingBaseName);
        Bus.Long(header + 8, AddDataTypesExistingPattern);
        Bus.Long(header + 12, 0);
        Bus.Long(header + 16, 0x6f6c6421);
        Bus.Long(header + 20, 0x70726576);
        Bus.Word(header + 24, 0);
        Bus.Word(header + 26, 0);
        Bus.Word(header + 28, 0);
        Bus.Word(header + 30, 0);
        Bus.Long(compound +
            (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode1 + 10,
            AddDataTypesExistingName);
        Bus.Long(compound +
            (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode2 + 10,
            AddDataTypesExistingName);
        WriteAddDataTypesCString(AddDataTypesExistingName, name);
        WriteAddDataTypesCString(AddDataTypesExistingBaseName, baseName);
        WriteAddDataTypesCString(AddDataTypesExistingPattern, "#?");
        InitializeEmptyList(compound +
            (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeToolList);
        AddTail(AddDataTypesSharedList +
            (uint)NativeMorphOSAddDataTypesCommand.DataTypesListBinary,
            compound);
        AddTail(AddDataTypesSharedList +
            (uint)NativeMorphOSAddDataTypesCommand.DataTypesListSorted,
            compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode2);
    }

    private void RegisterAddDataTypesListEntryExec()
    {
        Register(ExecBase, ExecLvo.CopyMem, "CopyMem",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                if (addDataTypesCallbackProbe)
                {
                    Require(state.D[0] is 1 or 5 &&
                        definition.CodeCopyCalls < 2,
                        "AddDataTypes read callback copied an unexpected byte count.");
                    Bus.Memory.AsSpan((int)state.A[0],
                        (int)state.D[0]).CopyTo(Bus.Memory.AsSpan(
                            (int)state.A[1], (int)state.D[0]));
                    definition.CodeCopyCalls++;
                    return 0;
                }
                Require(definition.ValidDtcd && state.A[0] ==
                        AddDataTypesCodeProperty && state.A[1] ==
                        AddDataTypesCodeBuffer && state.D[0] ==
                        AddDataTypesCodeBytes,
                    "AddDataTypes did not preserve and copy the DTCD property.");
                Bus.Memory.AsSpan((int)state.A[0], (int)state.D[0]).CopyTo(
                    Bus.Memory.AsSpan((int)state.A[1], (int)state.D[0]));
                definition.CodeCopyCalls++;
                return 0;
            });
        Register(ExecBase, ExecLvo.ObtainSemaphore, "ObtainSemaphore",
            (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesSharedList,
                    "AddDataTypes obtained a semaphore outside the shared list.");
                var definition = invocation.Definition.AddDataTypesList!;
                definition.ObtainCalls++;
                definition.Events.Add("obtain-semaphore");
                return 0;
            });
        Register(ExecBase, ExecLvo.InitSemaphore, "InitSemaphore",
            (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesSharedList &&
                    invocation.Definition.AddDataTypesList!
                        .Workbench31Profile &&
                    invocation.Definition.AddDataTypesList!
                        .CreateNamedObject,
                    "Workbench AddDataTypes initialized a semaphore outside a newly allocated shared list.");
                invocation.Definition.AddDataTypesList!.InitSemaphoreCalls++;
                invocation.Definition.AddDataTypesList.Events.Add(
                    "init-semaphore");
                return 0;
            });
        Register(ExecBase, ExecLvo.ReleaseSemaphore, "ReleaseSemaphore",
            (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesSharedList,
                    "AddDataTypes released a semaphore outside the shared list.");
                var definition = invocation.Definition.AddDataTypesList!;
                definition.SemaphoreReleaseCalls++;
                definition.Events.Add("release-semaphore");
                return 0;
            });
        Register(ExecBase, ExecLvo.AddTail, "AddTail", (state, invocation) =>
        {
            var list = state.A[0];
            var node = state.A[1];
            Require(list >= AddDataTypesSharedList && node >=
                AddDataTypesAllocationBase,
                "AddDataTypes AddTail list/node address differs.");
            AddTail(list, node);
            invocation.Definition.AddDataTypesList!.AddTailCalls++;
            return 0;
        });
        Register(ExecBase, ExecLvo.Insert, "Insert", (state, invocation) =>
        {
            var list = state.A[0];
            var node = state.A[1];
            Insert(list, node, state.A[2]);
            invocation.Definition.AddDataTypesList!.InsertCalls++;
            return 0;
        });
        Register(ExecBase, ExecLvo.Remove, "Remove", (state, invocation) =>
        {
            var definition = invocation.Definition.AddDataTypesList!;
            Require(definition.ExistingDescriptor &&
                (state.A[1] == AddDataTypesExistingCompound ||
                    state.A[1] == AddDataTypesExistingCompound +
                        (uint)NativeMorphOSAddDataTypesCommand
                            .CompoundDataTypeNode2),
                "AddDataTypes removed a node outside the replaced descriptor.");
            RemoveNode(state.A[1]);
            definition.DatatypeRemoveCalls++;
            return 0;
        });

        Register(AddDataTypesUtilityBase, UtilityLvo.FindNamedObject,
            "FindNamedObject", (state, invocation) =>
            {
                Require(state.A[0] == 0 && state.A[2] == 0 &&
                    Bus.CString(state.A[1]) == "DataTypesList",
                    "AddDataTypes named-object lookup ABI differs.");
                var definition = invocation.Definition.AddDataTypesList!;
                definition.FindCalls++;
                definition.Events.Add("find-named-object");
                return definition.NamedObjectAvailable &&
                    !definition.CreateNamedObject
                    ? AddDataTypesNamedObject : 0;
            });
        Register(AddDataTypesUtilityBase, UtilityLvo.AllocNamedObjectA,
            "AllocNamedObjectA", (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                var tags = state.A[1];
                Require(definition.Workbench31Profile &&
                    definition.CreateNamedObject &&
                    Bus.CString(state.A[0]) == "DataTypesList" &&
                    Bus.Long(tags) == 0x0fa0 &&
                    Bus.Long(tags + 4) == 1 &&
                    Bus.Long(tags + 8) == 0x0fa1 &&
                    Bus.Long(tags + 12) ==
                        NativeWorkbench31AddDataTypesCommand
                            .NamedListUserSpaceBytes &&
                    Bus.Long(tags + 16) == 0x0fa3 &&
                    Bus.Long(tags + 20) == 3 &&
                    Bus.Long(tags + 24) == 0 &&
                    Bus.Long(tags + 28) == 0,
                    "Workbench AddDataTypes named-object tag contract differs.");
                definition.NamedObjectAllocationCalls++;
                definition.Events.Add("alloc-named-object");
                Bus.Long(AddDataTypesNamedObject,
                    definition.UserDataAvailable
                        ? AddDataTypesSharedList : 0);
                return AddDataTypesNamedObject;
            });
        Register(AddDataTypesUtilityBase, UtilityLvo.AddNamedObject,
            "AddNamedObject", (state, invocation) =>
            {
                Require(invocation.Definition.AddDataTypesList!
                        .Workbench31Profile &&
                    invocation.Definition.AddDataTypesList
                        .CreateNamedObject &&
                    state.A[0] == 0 &&
                    state.A[1] == AddDataTypesNamedObject,
                    "Workbench AddDataTypes published the wrong named object.");
                invocation.Definition.AddDataTypesList!
                    .NamedObjectAddCalls++;
                invocation.Definition.AddDataTypesList.Events.Add(
                    "add-named-object");
                return 1;
            });
        Register(AddDataTypesUtilityBase, UtilityLvo.FreeNamedObject,
            "FreeNamedObject", (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesNamedObject,
                    "Workbench AddDataTypes freed the wrong named object.");
                invocation.Definition.AddDataTypesList!
                    .NamedObjectFreeCalls++;
                invocation.Definition.AddDataTypesList.Events.Add(
                    "free-named-object");
                return 0;
            });
        Register(AddDataTypesUtilityBase, UtilityLvo.ReleaseNamedObject,
            "ReleaseNamedObject", (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesNamedObject,
                    "AddDataTypes released the wrong named object.");
                var definition = invocation.Definition.AddDataTypesList!;
                definition.NamedObjectReleaseCalls++;
                definition.Events.Add("release-named-object");
                return 0;
            });
        Register(AddDataTypesUtilityBase, UtilityLvo.Stricmp, "Stricmp",
            (state, _) => unchecked((uint)Math.Sign(string.Compare(
                Bus.CString(state.A[0]), Bus.CString(state.A[1]),
                StringComparison.OrdinalIgnoreCase))));
        var localeBase = workbench31AddDataTypes
            ? AddDataTypesWorkbenchLocaleBase : AddDataTypesLocaleBase;
        Register(localeBase, -150, "OpenCatalogA",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(definition.Workbench31Profile &&
                    state.A[0] == 0 && state.A[2] == 0 &&
                    Bus.CString(state.A[1]) == "sys/c.catalog",
                    "Workbench AddDataTypes catalog request differs.");
                definition.CatalogOpenCalls++;
                definition.Events.Add("open-catalog");
                return definition.CatalogAvailable ? 0xabcdu : 0u;
            });
        Register(localeBase, -36, "CloseCatalog",
            (state, invocation) =>
            {
                Require(state.A[0] == 0xabcd,
                    "Workbench AddDataTypes closed an invalid catalog.");
                invocation.Definition.AddDataTypesList!.CatalogCloseCalls++;
                invocation.Definition.AddDataTypesList.Events.Add(
                    "close-catalog");
                return 0;
            });
    }

    private uint OpenWorkbench31AddDataTypesLibrary(M68kCpuState state,
        Invocation invocation)
    {
        var definition = invocation.Definition.AddDataTypesList!;
        var name = Bus.CString(state.A[1]);
        if (name == DOS.Name && state.D[0] == 37)
        {
            Require(invocation.Opens == 0 && invocation.Closes == 0,
                "Workbench AddDataTypes outer DOS open order differs.");
            invocation.Opens++;
            definition.Events.Add("open-dos-wrapper");
            return invocation.Definition.MissingDos ? 0u :
                invocation.DosBase;
        }

        if (name == DOS.Name && state.D[0] == 39)
        {
            Require(invocation.Opens == 1 &&
                definition.DosLibraryOpenCalls == 0,
                "Workbench AddDataTypes dos.library v39 open differs.");
            definition.DosLibraryOpenCalls++;
            definition.Events.Add("open-dos");
            return definition.DosLibraryAvailable
                ? AddDataTypesWorkbenchDosBase : 0u;
        }

        if (name == Utility.Name)
        {
            Require(state.D[0] == 39 &&
                definition.DosLibraryOpenCalls == 1 &&
                definition.UtilityOpenCalls == 0,
                "Workbench AddDataTypes utility.library floor/order differs.");
            definition.UtilityOpenCalls++;
            definition.Events.Add("open-utility");
            return definition.UtilityAvailable
                ? AddDataTypesUtilityBase : 0u;
        }

        if (name == Intuition.Name)
        {
            Require(state.D[0] == 39 &&
                definition.UtilityOpenCalls == 1 &&
                definition.IntuitionOpenCalls == 0,
                "Workbench AddDataTypes intuition.library floor/order differs.");
            definition.IntuitionOpenCalls++;
            definition.Events.Add("open-intuition");
            return definition.IntuitionAvailable
                ? AddDataTypesIntuitionBase : 0u;
        }

        if (name == IffParse.Name)
        {
            Require(state.D[0] == 37 &&
                definition.IntuitionOpenCalls == 1 &&
                definition.IffParseOpenCalls == 0,
                "Workbench AddDataTypes iffparse.library floor/order differs.");
            definition.IffParseOpenCalls++;
            definition.Events.Add("open-iffparse");
            return definition.IffParseAvailable
                ? AddDataTypesWorkbenchIffParseBase : 0u;
        }

        if (name == Locale.Name)
        {
            Require(state.D[0] == 38 &&
                definition.IffParseOpenCalls == 1 &&
                definition.LocaleOpenCalls == 0,
                "Workbench AddDataTypes locale.library floor/order differs.");
            definition.LocaleOpenCalls++;
            definition.Events.Add("open-locale");
            return definition.LocaleAvailable
                ? AddDataTypesWorkbenchLocaleBase : 0u;
        }

        throw new InvalidOperationException(
            $"Workbench AddDataTypes opened unexpected library '{name}' v{state.D[0]}.");
    }

    private bool CloseWorkbench31AddDataTypesLibrary(uint baseAddress,
        Invocation invocation)
    {
        var definition = invocation.Definition.AddDataTypesList!;
        if (baseAddress == AddDataTypesWorkbenchLocaleBase)
        {
            Require(definition.LocaleOpenCalls == 1 &&
                definition.LocaleAvailable && definition.LocaleCloseCalls == 0,
                "Workbench AddDataTypes closed an unowned locale lease.");
            definition.LocaleCloseCalls++;
            definition.Events.Add("close-locale");
            return true;
        }
        if (baseAddress == AddDataTypesWorkbenchIffParseBase)
        {
            Require(definition.IffParseOpenCalls == 1 &&
                definition.IffParseAvailable &&
                definition.IffParseCloseCalls == 0,
                "Workbench AddDataTypes closed an unowned iffparse lease.");
            definition.IffParseCloseCalls++;
            definition.Events.Add("close-iffparse");
            return true;
        }
        if (baseAddress == AddDataTypesIntuitionBase)
        {
            Require(definition.IntuitionOpenCalls == 1 &&
                definition.IntuitionAvailable &&
                definition.IntuitionCloseCalls == 0,
                "Workbench AddDataTypes closed an unowned intuition lease.");
            definition.IntuitionCloseCalls++;
            definition.Events.Add("close-intuition");
            return true;
        }
        if (baseAddress == AddDataTypesUtilityBase)
        {
            Require(definition.UtilityOpenCalls == 1 &&
                definition.UtilityAvailable && definition.UtilityCloseCalls == 0,
                "Workbench AddDataTypes closed an unowned utility lease.");
            definition.UtilityCloseCalls++;
            definition.Events.Add("close-utility");
            return true;
        }
        if (baseAddress == AddDataTypesWorkbenchDosBase)
        {
            Require(definition.DosLibraryOpenCalls == 1 &&
                definition.DosLibraryAvailable &&
                definition.DosLibraryCloseCalls == 0,
                "Workbench AddDataTypes closed an unowned dos.library v39 lease.");
            definition.DosLibraryCloseCalls++;
            definition.Events.Add("close-dos");
            return true;
        }
        return false;
    }

    private void VerifyAddDataTypesCallbackProbe(Invocation invocation)
    {
        var definition = invocation.Definition.AddDataTypesList!;
        Require(definition.CodeCopyCalls == 2 &&
            definition.CallbackAllocCalls == 1 &&
            definition.CallbackFreeCalls == 1 &&
            definition.CallbackAllocationAddress != 0 &&
            invocation.Opens == 0 && invocation.Closes == 0 &&
            invocation.Allocations == 0 && invocation.Reads == 0 &&
            invocation.FreeArgs == 0,
            "AddDataTypes exported callback adapters did not execute cleanly through the SDK wrappers.");
    }

    private void RegisterAddDataTypesListEntryIffParse()
    {
        var iffParseBase = workbench31AddDataTypes
            ? AddDataTypesWorkbenchIffParseBase
            : AddDataTypesIffParseBase;
        Register(iffParseBase, -30, "AllocIFF",
            (_, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                definition.IffAllocCalls++;
                definition.Events.Add("alloc-iff");
                Bus.Long(AddDataTypesIffHandle, AddDataTypesFileHandle);
                return AddDataTypesIffHandle;
            });
        Register(iffParseBase, -36, "OpenIFF",
            (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesIffHandle && state.D[0] ==
                    (uint)IffParse.IFFF_READ &&
                    Bus.Long(AddDataTypesIffHandle) == AddDataTypesFileHandle,
                    "AddDataTypes OpenIFF handle/mode/stream differs.");
                invocation.Definition.AddDataTypesList!.IffOpenCalls++;
                invocation.Definition.AddDataTypesList!.Events.Add("open-iff");
                return 0;
            });
        Register(iffParseBase, -42, "ParseIFF",
            (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesIffHandle &&
                    state.D[0] == (uint)IffParse.IFFPARSE_SCAN,
                    "AddDataTypes ParseIFF mode differs.");
                invocation.Definition.AddDataTypesList!.IffParseCalls++;
                invocation.Definition.AddDataTypesList!.Events.Add("parse-iff");
                return unchecked((uint)IffError.Eoc);
            });
        Register(iffParseBase, -48, "CloseIFF",
            (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesIffHandle,
                    "AddDataTypes closed the wrong IFF handle.");
                invocation.Definition.AddDataTypesList!.IffCloseCalls++;
                invocation.Definition.AddDataTypesList!.Events.Add("close-iff");
                return 0;
            });
        Register(iffParseBase, -54, "FreeIFF",
            (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesIffHandle,
                    "AddDataTypes freed the wrong IFF handle.");
                invocation.Definition.AddDataTypesList!.IffFreeCalls++;
                invocation.Definition.AddDataTypesList!.Events.Add("free-iff");
                return 0;
            });
        Register(iffParseBase, -120, "PropChunks",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                var pair = state.A[1];
                Require(state.A[0] == AddDataTypesIffHandle &&
                    state.D[0] == 2 &&
                    Bus.Long(pair) == 0x44545950 &&
                    Bus.Long(pair + 4) == 0x44544844 &&
                    Bus.Long(pair + 8) == 0x44545950 &&
                    Bus.Long(pair + 12) == 0x44544344,
                    "AddDataTypes PropChunks descriptor set differs.");
                definition.IffPropChunksCalls++;
                return 0;
            });
        Register(iffParseBase, -144, "CollectionChunks",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                var pair = state.A[1];
                Require(state.A[0] == AddDataTypesIffHandle &&
                    state.D[0] == 1 && Bus.Long(pair) == 0x44545950 &&
                    Bus.Long(pair + 4) == 0x4454544c,
                    "AddDataTypes CollectionChunks descriptor differs.");
                definition.IffCollectionChunksCalls++;
                return 0;
            });
        Register(iffParseBase, -150, "StopOnExit",
            (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesIffHandle &&
                    state.D[0] == 0x44545950 && state.D[1] == 0x464f524d,
                    "AddDataTypes StopOnExit target differs.");
                invocation.Definition.AddDataTypesList!.IffStopOnExitCalls++;
                return 0;
            });
        Register(iffParseBase, -156, "FindProp",
            (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesIffHandle &&
                    state.D[0] == 0x44545950 &&
                    state.D[1] is 0x44544844 or 0x44544344,
                    "AddDataTypes FindProp query differs.");
                var definition = invocation.Definition.AddDataTypesList!;
                definition.IffFindPropCalls++;
                if (state.D[1] == 0x44544844 && definition.ValidDthd)
                {
                    if (definition.ExistingDescriptor)
                        SeedExistingDatatype(definition);
                    return AddDataTypesStoredProperty;
                }
                if (state.D[1] == 0x44544344 && definition.ValidDtcd)
                    return AddDataTypesStoredCode;
                return 0;
            });
        Register(iffParseBase, -234, "InitIFFasDOS",
            (state, invocation) =>
            {
                Require(state.A[0] == AddDataTypesIffHandle,
                    "AddDataTypes initialized the wrong IFF handle.");
                invocation.Definition.AddDataTypesList!.IffInitDosCalls++;
                return 0;
            });
    }

    private void RegisterAddDataTypesListDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.UnLoadSeg, "UnLoadSeg",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[1] is AddDataTypesLoadedSegment or
                    AddDataTypesExistingSegment &&
                    definition.UnLoadSegCalls == 0,
                    "AddDataTypes unloaded an unknown or already-released segment.");
                definition.UnLoadSegCalls++;
                definition.UnLoadedSegment = state.D[1];
                definition.Events.Add("unload-segment");
                return 0;
            });
        Register(baseAddress, DosLvo.Lock, "Lock",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                var path = Bus.CString(state.D[1]);
                Require(state.D[2] == unchecked((uint)(int)DOS.LockMode.Read),
                    "AddDataTypes REFRESH did not request a shared read lock.");
                definition.RefreshLockCalls++;
                if (path == "DEVS:DataTypes/")
                    return definition.RefreshSystemLockAvailable
                        ? AddDataTypesLockBptr : 0;
                if (path == "MOSSYS:Devs/DataTypes/")
                    return definition.RefreshMorphOsLockAvailable
                        ? AddDataTypesMorphOsLockBptr : 0;
                if (path == "DEVS:DataTypes" &&
                    definition.Workbench31Profile)
                {
                    definition.Events.Add("refresh-date-scan-classic");
                    return definition.RefreshSystemLockAvailable
                        ? AddDataTypesLockBptr : 0;
                }
                if (path == "DEVS:DataTypes")
                {
                    definition.Events.Add("refresh-date-scan-system");
                    return AddDataTypesLockBptr;
                }
                if (path == "MOSSYS:Devs/DataTypes")
                {
                    definition.Events.Add("refresh-date-scan-morphos");
                    return AddDataTypesMorphOsLockBptr;
                }
                throw new InvalidOperationException(
                    $"Unexpected AddDataTypes REFRESH lock path: {path}.");
            });
        Register(baseAddress, DosLvo.DupLock, "DupLock",
            (state, invocation) =>
            {
                Require(invocation.Definition.Workbench &&
                    state.D[1] == AddDataTypesLockBptr,
                    "AddDataTypes Workbench startup duplicated the wrong first-argument lock.");
                invocation.Definition.AddDataTypesList!.WorkbenchDupLockCalls++;
                return AddDataTypesWorkbenchDuplicateLock;
            });
        Register(baseAddress, DosLvo.UnLock, "UnLock",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                if (invocation.Definition.Workbench)
                {
                    Require(state.D[1] ==
                        AddDataTypesWorkbenchDuplicateLock,
                        "AddDataTypes Workbench startup unlocked a borrowed WBArg lock.");
                    definition.WorkbenchUnlockCalls++;
                    return 0;
                }
                Require(state.D[1] is AddDataTypesLockBptr or
                    AddDataTypesMorphOsLockBptr,
                    "AddDataTypes REFRESH unlocked an unknown lock.");
                definition.RefreshUnLockCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.SameLock, "SameLock",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[1] == AddDataTypesLockBptr &&
                    state.D[2] == AddDataTypesMorphOsLockBptr,
                    "AddDataTypes REFRESH compared the wrong directory locks.");
                definition.RefreshSameLockCalls++;
                return definition.RefreshSameLock ? 0u : uint.MaxValue;
            });
        Register(baseAddress, DosLvo.AllocDosObject, "AllocDosObject",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[1] == (uint)DosObjectType.FileInfoBlock &&
                    state.D[2] == 0,
                    "AddDataTypes REFRESH FIB allocation arguments differ.");
                definition.RefreshDosObjectAllocCalls++;
                Bus.Memory.AsSpan((int)AddDataTypesRefreshFib,
                    FileInfoBlock.SizeInBytes).Clear();
                return AddDataTypesRefreshFib;
            });
        Register(baseAddress, DosLvo.FreeDosObject, "FreeDosObject",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[1] == (uint)DosObjectType.FileInfoBlock &&
                    state.D[2] == AddDataTypesRefreshFib,
                    "AddDataTypes REFRESH freed the wrong FIB.");
                definition.RefreshDosObjectFreeCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.Examine, "Examine",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[1] is AddDataTypesLockBptr or
                    AddDataTypesMorphOsLockBptr &&
                    state.D[2] == AddDataTypesRefreshFib,
                    "AddDataTypes REFRESH Examine arguments differ.");
                definition.RefreshExamineCalls++;
                var fibStamp = AddDataTypesRefreshFib +
                    (uint)FileInfoBlock.DateDaysOffset;
                var listStamp = AddDataTypesSharedList +
                    (uint)NativeMorphOSAddDataTypesCommand.DataTypesListDateStamp;
                for (var offset = 0; offset < DosLayout.DateStamp.Size;
                     offset += 4)
                    Bus.Long(fibStamp + (uint)offset,
                        definition.RefreshDatesEqual
                            ? Bus.Long(listStamp + (uint)offset)
                            : (uint)(offset + 2));
                return 1;
            });
        Register(baseAddress, DosLvo.ExNext, "ExNext",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[1] is AddDataTypesLockBptr or
                    AddDataTypesMorphOsLockBptr &&
                    state.D[2] == AddDataTypesRefreshFib,
                    "AddDataTypes REFRESH ExNext arguments differ.");
                definition.RefreshExNextCalls++;
                return definition.RefreshEmptyDirectories ? 0u : 1u;
            });
        Register(baseAddress, DosLvo.CompareDates, "CompareDates",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[1] == AddDataTypesRefreshFib +
                        (uint)FileInfoBlock.DateDaysOffset &&
                    state.D[2] == AddDataTypesSharedList +
                        (uint)NativeMorphOSAddDataTypesCommand.DataTypesListDateStamp,
                    "AddDataTypes REFRESH CompareDates arguments differ.");
                definition.RefreshCompareDatesCalls++;
                for (var offset = 0; offset < DosLayout.DateStamp.Size;
                     offset += 4)
                    if (Bus.Long(state.D[1] + (uint)offset) !=
                        Bus.Long(state.D[2] + (uint)offset))
                        return uint.MaxValue;
                return 0;
            });
        Register(baseAddress, DosLvo.InternalLoadSeg, "InternalLoadSeg",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(definition.ValidDtcd &&
                    definition.InternalLoadSegCalls == 0 &&
                    state.D[0] == AddDataTypesLoaderState &&
                    state.A[0] == 0 &&
                    Bus.Long(state.D[0]) == AddDataTypesCodeBuffer &&
                    Bus.Long(state.D[0] + 4) == AddDataTypesCodeBytes &&
                    Bus.Long(state.D[0] + 8) == 0 &&
                    state.A[1] != 0 && state.A[2] != 0 &&
                    Bus.Long(state.A[2]) ==
                        (definition.Workbench31Profile
                            ? NativeWorkbench31AddDataTypesCommand
                                .EmbeddedCodeStackSize
                            : NativeMorphOSAddDataTypesCommand
                                .MorphOSEmbeddedCodeStackSize),
                    "AddDataTypes InternalLoadSeg callback/state/stack ABI differs.");
                definition.LoaderReadCallback = Bus.Long(state.A[1]);
                definition.LoaderAllocCallback = Bus.Long(state.A[1] + 4);
                definition.LoaderFreeCallback = Bus.Long(state.A[1] + 8);
                Require(definition.LoaderReadCallback >= LoadAddress &&
                    definition.LoaderReadCallback < LoadAddress + 0x10000 &&
                    definition.LoaderAllocCallback >= LoadAddress &&
                    definition.LoaderAllocCallback < LoadAddress + 0x10000 &&
                    definition.LoaderFreeCallback >= LoadAddress &&
                    definition.LoaderFreeCallback < LoadAddress + 0x10000 &&
                    (definition.LoaderReadCallback & 1) == 0 &&
                    (definition.LoaderAllocCallback & 1) == 0 &&
                    (definition.LoaderFreeCallback & 1) == 0 &&
                    definition.LoaderReadCallback != definition.LoaderAllocCallback &&
                    definition.LoaderReadCallback != definition.LoaderFreeCallback &&
                    definition.LoaderAllocCallback != definition.LoaderFreeCallback,
                    "AddDataTypes InternalLoadSeg callback table is not resident code.");
                definition.InternalLoadSegCalls++;
                definition.Events.Add("internal-load-seg");
                return definition.InternalLoadSegFailure
                    ? 0u : AddDataTypesLoadedSegment;
            });
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                var expectedTemplate = definition.Workbench31Profile
                    ? NativeWorkbench31AddDataTypesCommand.Template
                    : NativeMorphOSAddDataTypesCommand.Template;
                var resultCount = definition.Workbench31Profile
                    ? NativeWorkbench31AddDataTypesCommand.ResultCount
                    : NativeMorphOSAddDataTypesCommand.CliResultCount;
                Require(Bus.CString(state.D[1]) == expectedTemplate &&
                    state.D[3] == 0,
                    "AddDataTypes ReadArgs template or RDArgs source differs.");
                var resultBytes = resultCount * 4;
                var results = state.D[2];
                Require(Bus.OwnedAllocation(invocation, results, "Exec").Size ==
                        resultBytes && (results & 3) == 0,
                    "AddDataTypes ReadArgs result allocation differs.");
                for (var offset = 0u; offset < resultBytes; offset += 4)
                    Require(Bus.Long(results + offset) == 0,
                        "AddDataTypes ReadArgs slots were not zero-initialized.");

                definition.ReadArgsCalls++;
                definition.Events.Add("read-args");
                if (definition.ParserError != 0)
                {
                    invocation.IoError = definition.ParserError;
                    return 0;
                }

                if (definition.Files)
                    Bus.Long(results, AddDataTypesFilesVector);
                if (definition.Quiet)
                    Bus.Long(results + 4, uint.MaxValue);
                if (definition.Refresh)
                    Bus.Long(results + 8, uint.MaxValue);
                if (definition.List && !definition.Workbench31Profile)
                    Bus.Long(results + 12, uint.MaxValue);
                return AddDataTypesReadArgs;
            });
        Register(baseAddress, DosLvo.ParsePatternNoCase,
            "ParsePatternNoCase", (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(Bus.CString(state.D[1]) == "#?.(info|backdrop)" &&
                    state.D[2] == AddDataTypesExclusion && state.D[3] == 39,
                    "AddDataTypes exclusion pattern compilation differs.");
                definition.ExclusionParseCalls++;
                definition.Events.Add("parse-exclusion-pattern");
                return 1;
            });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                var pattern = Bus.CString(state.D[1]);
                var isRefreshPattern = pattern is "DEVS:DataTypes" or
                    "MOSSYS:Devs/DataTypes" or "DEVS:DataTypes/#?";
                Require(state.D[2] == AddDataTypesAnchor &&
                    Bus.Long(state.D[2] + 8) ==
                        (1u << 12) &&
                    (pattern is "datatype-a" or "datatype-b" ||
                        isRefreshPattern),
                    "AddDataTypes MatchFirst pattern or AnchorPath differs.");
                definition.MatchFirstCalls++;
                definition.Events.Add("match-first");
                if (isRefreshPattern)
                    definition.RefreshScanPatterns.Add(pattern);
                var filesMatch = isRefreshPattern
                    ? definition.RefreshFilesMatch : definition.FilesMatch;
                if (!filesMatch)
                    return (uint)DOS.Error.NoMoreEntries;

                var anchor = state.D[2];
                var fib = anchor + (uint)DosLayout.AnchorPath.Info;
                Bus.Long(anchor + (uint)DosLayout.AnchorPath.Current,
                    AddDataTypesCurrentChain);
                Bus.Long(AddDataTypesCurrentChain +
                    (uint)DosLayout.AChain.Lock, AddDataTypesLockBptr);
                if (!isRefreshPattern &&
                    (definition.FilesDirectoryThenFile ||
                        definition.FilesNestedDirectoryAfterFirstFile))
                {
                    Bus.Long(fib + (uint)FileInfoBlock.DirEntryTypeOffset, 2);
                    WriteAddDataTypesCString(fib +
                        (uint)FileInfoBlock.FileNameOffset, "first-directory");
                    Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] =
                        (byte)(0x80 | (definition.FirstDirectoryAlreadyEntered
                            ? (byte)AnchorPathFlags.DidDirectory : 0));
                    return 0;
                }
                Bus.Long(fib + (uint)FileInfoBlock.DirEntryTypeOffset,
                    unchecked((uint)-3));
                WriteAddDataTypesCString(fib +
                    (uint)FileInfoBlock.FileNameOffset,
                    "descriptor.datatype");
                return 0;
            });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                definition.MatchNextCalls++;
                if (definition.FilesDirectoryThenFile &&
                    definition.MatchNextCalls == 1)
                {
                    Require(state.D[1] == AddDataTypesAnchor,
                        "AddDataTypes MatchNext received the wrong AnchorPath.");
                    var flags = Bus.Memory[AddDataTypesAnchor +
                        (uint)DosLayout.AnchorPath.Flags];
                    var expected = definition.FirstDirectoryAlreadyEntered
                        ? 0x80 : 0x80 |
                            (byte)AnchorPathFlags.DoDirectory;
                    Require(flags == expected &&
                        (flags & (byte)AnchorPathFlags.DidDirectory) == 0,
                        "AddDataTypes first-directory flags differ or recurse.");
                    if (definition.FilesNestedDirectoryAfterFirstFile)
                    {
                        var flagAddress = AddDataTypesAnchor +
                            (uint)DosLayout.AnchorPath.Flags;
                        Bus.Memory[flagAddress] = (byte)(
                            (flags & ~(byte)AnchorPathFlags.DoDirectory) |
                            (byte)AnchorPathFlags.DidDirectory);
                    }
                    var fib = AddDataTypesAnchor +
                        (uint)DosLayout.AnchorPath.Info;
                    Bus.Long(fib + (uint)FileInfoBlock.DirEntryTypeOffset,
                        unchecked((uint)-3));
                    WriteAddDataTypesCString(fib +
                        (uint)FileInfoBlock.FileNameOffset,
                        "descriptor.datatype");
                    return 0;
                }
                if (definition.FilesNestedDirectoryAfterFirstFile &&
                    definition.MatchNextCalls == 2)
                {
                    var flags = Bus.Memory[AddDataTypesAnchor +
                        (uint)DosLayout.AnchorPath.Flags];
                    Require((flags & (byte)AnchorPathFlags.DoDirectory) == 0,
                        "AddDataTypes attempted directory recursion after its first file.");
                    var fib = AddDataTypesAnchor +
                        (uint)DosLayout.AnchorPath.Info;
                    Bus.Long(fib + (uint)FileInfoBlock.DirEntryTypeOffset, 2);
                    WriteAddDataTypesCString(fib +
                        (uint)FileInfoBlock.FileNameOffset,
                        "nested-directory");
                    return 0;
                }
                if (definition.FilesNestedDirectoryAfterFirstFile &&
                    definition.MatchNextCalls == 3)
                {
                    var flags = Bus.Memory[AddDataTypesAnchor +
                        (uint)DosLayout.AnchorPath.Flags];
                    Require((flags & (byte)AnchorPathFlags.DoDirectory) == 0,
                        "AddDataTypes re-entered a later nested directory.");
                    var fib = AddDataTypesAnchor +
                        (uint)DosLayout.AnchorPath.Info;
                    Bus.Long(fib + (uint)FileInfoBlock.DirEntryTypeOffset,
                        unchecked((uint)-3));
                    WriteAddDataTypesCString(fib +
                        (uint)FileInfoBlock.FileNameOffset,
                        "descriptor.datatype");
                    return 0;
                }
                return (uint)DOS.Error.NoMoreEntries;
            });
        Register(baseAddress, DosLvo.MatchPatternNoCase,
            "MatchPatternNoCase", (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[1] == AddDataTypesExclusion &&
                    Bus.CString(state.D[2]) == "descriptor.datatype",
                    "AddDataTypes exclusion matcher arguments differ.");
                definition.MatchPatternCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd",
            (state, invocation) =>
            {
                Require(state.D[1] == AddDataTypesAnchor,
                    "AddDataTypes MatchEnd anchor differs.");
                var definition = invocation.Definition.AddDataTypesList!;
                definition.MatchEndCalls++;
                definition.Events.Add("match-end");
                return 0;
            });
        Register(baseAddress, DosLvo.Open, "Open",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                var expectedName = invocation.Definition.Workbench
                    ? "datatype-b" : "descriptor.datatype";
                Require(Bus.CString(state.D[1]) == expectedName &&
                    state.D[2] == (uint)DOS.FileMode.OldFile,
                    "AddDataTypes opened the wrong matched descriptor.");
                definition.FileOpenCalls++;
                definition.Events.Add("open-descriptor");
                return AddDataTypesFileHandle;
            });
        Register(baseAddress, DosLvo.Close, "Close",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[1] == AddDataTypesFileHandle,
                    "AddDataTypes closed the wrong descriptor file.");
                definition.FileCloseCalls++;
                definition.Events.Add("close-descriptor");
                return 1;
            });
        Register(baseAddress, DosLvo.CurrentDir, "CurrentDir",
            (state, invocation) =>
            {
                var definition = invocation.Definition.AddDataTypesList!;
                definition.CurrentDirCalls++;
                if (invocation.Definition.Workbench)
                {
                    var expectedLock = definition.CurrentDirCalls switch
                    {
                        1 => AddDataTypesWorkbenchDuplicateLock,
                        2 => definition.WorkbenchFiles
                            ? AddDataTypesMorphOsLockBptr
                            : AddDataTypesOldDirectoryBptr,
                        3 => AddDataTypesWorkbenchDuplicateLock,
                        4 => AddDataTypesOldDirectoryBptr,
                        _ => 0u
                    };
                    Require(state.D[1] == expectedLock,
                        "AddDataTypes Workbench CurrentDir lock/restore order differs.");
                    return definition.CurrentDirCalls switch
                    {
                        1 => AddDataTypesOldDirectoryBptr,
                        2 => AddDataTypesWorkbenchDuplicateLock,
                        3 => AddDataTypesMorphOsLockBptr,
                        4 => AddDataTypesWorkbenchDuplicateLock,
                        _ => 0u
                    };
                }
                if ((definition.CurrentDirCalls & 1) != 0)
                {
                    Require(state.D[1] == AddDataTypesLockBptr,
                        "AddDataTypes did not enter the matched file directory.");
                    return AddDataTypesOldDirectoryBptr;
                }
                Require(state.D[1] == AddDataTypesOldDirectoryBptr,
                    "AddDataTypes did not restore the previous directory.");
                return AddDataTypesLockBptr;
            });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs",
            (state, invocation) =>
            {
                Require(state.D[1] == AddDataTypesReadArgs,
                    "AddDataTypes freed the wrong RDArgs lease.");
                var definition = invocation.Definition.AddDataTypesList!;
                Require(definition.FreeArgsCalls == 0,
                    "AddDataTypes freed RDArgs more than once.");
                definition.FreeArgsCalls++;
                definition.Events.Add("free-args");
                invocation.IoError = 901;
                return 0;
            });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.CheckSignal, "CheckSignal",
            (state, invocation) =>
            {
                Require(state.D[1] == 0x1000,
                    "AddDataTypes LIST checked the wrong break mask.");
                var definition = invocation.Definition.AddDataTypesList!;
                definition.CheckSignalCalls++;
                return definition.CtrlC ? 0x1000u : 0u;
            });
        Register(baseAddress, DosLvo.Flush, "Flush", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "AddDataTypes LIST flushed a non-output stream.");
            invocation.Definition.AddDataTypesList!.FlushCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(state.D[2] == 0,
                    "AddDataTypes PrintFault header must be null.");
                var definition = invocation.Definition.AddDataTypesList!;
                var expectedError = definition.ParserError != 0
                    ? definition.ParserError
                    : definition.ResultAllocationFailure
                        ? (int)DOS.Error.NoFreeStore
                        : (int)DOS.Error.Break;
                Require(state.D[1] == (uint)expectedError,
                    "AddDataTypes PrintFault error differs.");
                definition.PrintFaultCalls++;
                invocation.IoError = expectedError;
                return 0;
            });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr",
            (state, invocation) =>
            {
                invocation.IoError = unchecked((int)state.D[1]);
                return 0;
            });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[1]) == "%s, \"%s\"\n",
                    "AddDataTypes LIST format differs.");
                var args = state.D[2];
                var baseName = Bus.CString(Bus.Long(args));
                var name = Bus.CString(Bus.Long(args + 4));
                Require(baseName == name && name is "ascii" or "binary" or
                    "directory" or "iff",
                    "AddDataTypes LIST node/header names differ.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{baseName}, \"{name}\"\n"));
                invocation.Definition.AddDataTypesList!.VPrintfCalls++;
                return 0;
            });
    }

    private void VerifyAddDataTypesListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.AddDataTypesList!;
        if (definition.Workbench31Profile)
        {
            VerifyWorkbench31AddDataTypesEntry(invocation);
            return;
        }
        if (invocation.Definition.MissingDos)
        {
            Require(definition.UtilityOpenCalls == 0 &&
                definition.IffParseOpenCalls == 0 &&
                definition.LocaleOpenCalls == 0 &&
                definition.DataTypesOpenCalls == 0 &&
                definition.FindCalls == 0 && definition.ObtainCalls == 0 &&
                definition.ResultAllocationCalls == 0 &&
                definition.ReadArgsCalls == 0,
                "AddDataTypes crossed the missing-DOS boundary.");
            return;
        }

        var failedLibrary = !definition.UtilityAvailable ||
            !definition.IffParseAvailable || !definition.LocaleAvailable ||
            !definition.DataTypesAvailable;
        if (failedLibrary)
        {
            Require(definition.UtilityOpenCalls == 1 &&
                definition.IffParseOpenCalls == (definition.UtilityAvailable ? 1 : 0) &&
                definition.LocaleOpenCalls ==
                    (definition.UtilityAvailable && definition.IffParseAvailable ? 1 : 0) &&
                definition.DataTypesOpenCalls ==
                    (definition.UtilityAvailable && definition.IffParseAvailable &&
                     definition.LocaleAvailable ? 1 : 0) &&
                definition.UtilityCloseCalls ==
                    (definition.UtilityAvailable ? 1 : 0) &&
                definition.IffParseCloseCalls ==
                    (definition.UtilityAvailable && definition.IffParseAvailable ? 1 : 0) &&
                definition.LocaleCloseCalls ==
                    (definition.UtilityAvailable && definition.IffParseAvailable &&
                     definition.LocaleAvailable ? 1 : 0) &&
                definition.DataTypesCloseCalls == 0 &&
                definition.FindCalls == 0 && definition.ObtainCalls == 0 &&
                definition.ResultAllocationCalls == 0 &&
                definition.ReadArgsCalls == 0,
                "AddDataTypes library failure crossed a dependency boundary or leaked a lease.");
            return;
        }

        Require(definition.UtilityOpenCalls == 1 &&
            definition.UtilityCloseCalls == 1 && definition.FindCalls == 1 &&
            definition.IffParseOpenCalls == 1 &&
            definition.IffParseCloseCalls == 1 &&
            definition.LocaleOpenCalls == 1 &&
            definition.LocaleCloseCalls == 1 &&
            definition.DataTypesOpenCalls == 1 &&
            definition.DataTypesCloseCalls == 1 &&
            definition.NamedObjectReleaseCalls ==
                (definition.NamedObjectAvailable ? 1 : 0),
            "AddDataTypes library/named-object ownership differs.");

        if (!definition.NamedObjectAvailable || !definition.UserDataAvailable)
        {
            Require(definition.ObtainCalls == 0 &&
                definition.SemaphoreReleaseCalls == 0 &&
                definition.AllocVecCalls == 0 &&
                definition.ResultAllocationCalls == 0 &&
                definition.ReadArgsCalls == 0,
                "AddDataTypes used an absent shared datatype list.");
            return;
        }

        var parserFailed = definition.ParserError != 0;
        var resultAllocationFailed = definition.ResultAllocationFailure;
        var filesMatched = definition.FilesMatch && definition.Files &&
            !definition.Refresh && !parserFailed && !resultAllocationFailed;
        var workbenchFileMatched = invocation.Definition.Workbench &&
            definition.WorkbenchFiles;
        var dthdPropertyPresent = definition.ValidDthd &&
            (filesMatched || workbenchFileMatched);
        var validDthd = dthdPropertyPresent &&
            IsAcceptedDthdHeader(definition);
        var validDtcd = definition.ValidDtcd && validDthd;
        var descriptorCanBeAdded = validDthd &&
            (!validDtcd || !definition.InternalLoadSegFailure);
        var oldDescriptorReplaced = descriptorCanBeAdded &&
            definition.ExistingDescriptor &&
            !definition.ExistingDescriptorOpen &&
            !definition.ExistingDescriptorSame;
        var oldDescriptorRetained = validDthd &&
            definition.ExistingDescriptor && !oldDescriptorReplaced;
        var descriptorRegistered = descriptorCanBeAdded &&
            (!definition.ExistingDescriptor || oldDescriptorReplaced);
        var descriptorListed = descriptorRegistered ||
            oldDescriptorRetained;
        var discardedDuplicate = descriptorCanBeAdded &&
            definition.ExistingDescriptor &&
            (definition.ExistingDescriptorOpen ||
                definition.ExistingDescriptorSame);
        var candidateFreed = validDtcd &&
            definition.InternalLoadSegFailure || discardedDuplicate;
        var oldSegmentUnloaded = oldDescriptorReplaced &&
            definition.ExistingDescriptorHasSegment;
        var candidateSegmentUnloaded = validDtcd &&
            !definition.InternalLoadSegFailure && discardedDuplicate;
        var refreshExecuted = definition.Refresh && !parserFailed &&
            !resultAllocationFailed;
        var refreshSystemDateScan = refreshExecuted &&
            definition.RefreshSystemLockAvailable &&
            !(definition.RefreshMorphOsLockAvailable &&
                definition.RefreshSameLock);
        var refreshMorphOsDateScan = refreshExecuted &&
            definition.RefreshMorphOsLockAvailable;
        var refreshDateScans = (refreshSystemDateScan ? 1 : 0) +
            (refreshMorphOsDateScan ? 1 : 0);
        var refreshScanCount = refreshExecuted &&
            (!definition.RefreshDatesEqual ||
                definition.RefreshEmptyDirectories)
            ? refreshDateScans : 0;
        var expectedRefreshPatterns = new List<string>();
        if (refreshSystemDateScan && refreshScanCount > 0)
            expectedRefreshPatterns.Add("DEVS:DataTypes");
        if (refreshMorphOsDateScan && refreshScanCount > 0)
            expectedRefreshPatterns.Add("MOSSYS:Devs/DataTypes");
        Require(definition.ObtainCalls == 1 &&
            definition.SemaphoreReleaseCalls == 1 &&
            definition.AllocVecCalls == 4 +
                (definition.Files && !definition.Refresh
                    ? 1 + definition.PatternCount : 0) +
                (refreshExecuted ? 1 + refreshScanCount : 0) +
                (validDthd ? 1 : 0) + (validDtcd ? 2 : 0) &&
            definition.AddTailCalls == 4 &&
            definition.InsertCalls == 4 + (descriptorRegistered ? 2 : 0),
            "AddDataTypes shared-list initialization is incomplete.");
        Require(definition.Events.IndexOf("release-named-object") <
            definition.Events.IndexOf("obtain-semaphore") &&
            (definition.FreeArgsCalls == 0 ||
                definition.Events.IndexOf("free-args") <
                    definition.Events.IndexOf("release-semaphore")) &&
            definition.Events.IndexOf("release-semaphore") <
            definition.Events.IndexOf("close-datatypes") &&
            definition.Events.IndexOf("close-datatypes") <
                definition.Events.IndexOf("close-locale") &&
            definition.Events.IndexOf("close-locale") <
                definition.Events.IndexOf("close-iffparse") &&
            definition.Events.IndexOf("close-iffparse") <
                definition.Events.IndexOf("close-utility"),
            "AddDataTypes argument/list/library release order differs.");

        var cliStartup = !invocation.Definition.Workbench;
        Require(definition.ResultAllocationCalls == (cliStartup ? 1 : 0) &&
            definition.ResultFreeCalls ==
                (cliStartup && !resultAllocationFailed ? 1 : 0) &&
            definition.ReadArgsCalls ==
                (cliStartup && !resultAllocationFailed ? 1 : 0) &&
            definition.FreeArgsCalls ==
                (cliStartup && !parserFailed && !resultAllocationFailed ? 1 : 0),
            "AddDataTypes ReadArgs lease ownership differs.");

        var names = ReadListNames(AddDataTypesSharedList +
            (uint)NativeMorphOSAddDataTypesCommand.DataTypesListSorted,
            NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode2);
        var expectedExternalName = oldDescriptorRetained &&
            definition.ExistingDescriptorSame
            ? "TEST-TYPE"
            : "test-type";
        var expectedSortedNames = descriptorListed
            ? new[] { "ascii", "binary", "directory", "iff",
                expectedExternalName }
            : new[] { "ascii", "binary", "directory", "iff" };
        Require(names.SequenceEqual(expectedSortedNames),
            "AddDataTypes built-in descriptors are not globally sorted.");
        var listAttempted = definition.List && !parserFailed &&
            !resultAllocationFailed;
        var expectedRows = listAttempted && !definition.CtrlC ? 4 : 0;
        var matchedDescriptorCount = filesMatched
            ? definition.FilesDirectoryThenFile &&
                definition.FilesNestedDirectoryAfterFirstFile ? 2 : 1
            : 0;
        var matchedFileCount = workbenchFileMatched
            ? 1 : matchedDescriptorCount;
        var scannerEntries = filesMatched
            ? definition.FilesNestedDirectoryAfterFirstFile ? 4 :
                definition.FilesDirectoryThenFile ? 2 : 1
            : 0;
        Require(definition.CheckSignalCalls ==
                (listAttempted ? definition.CtrlC ? 1 : 4 : 0) +
                scannerEntries &&
            definition.VPrintfCalls == expectedRows &&
            definition.FlushCalls == (listAttempted && definition.CtrlC ? 1 : 0) &&
            definition.PrintFaultCalls ==
                (parserFailed || resultAllocationFailed ||
                    listAttempted && definition.CtrlC ? 1 : 0),
            "AddDataTypes LIST break/output call sequence differs.");
        var scanning = definition.Files && !definition.Refresh &&
            !parserFailed && !resultAllocationFailed;
        Require(definition.ExclusionParseCalls ==
                (scanning ? 1 : 0) + (refreshExecuted ? 1 : 0) &&
            definition.MatchFirstCalls ==
                (scanning ? definition.PatternCount : 0) + refreshScanCount &&
            definition.MatchEndCalls ==
                (scanning ? definition.PatternCount : 0) + refreshScanCount &&
            definition.MatchNextCalls == scannerEntries &&
            definition.MatchPatternCalls == matchedDescriptorCount &&
            definition.AnchorFreeCalls ==
                (scanning ? definition.PatternCount : 0) + refreshScanCount &&
            definition.ExclusionFreeCalls ==
                (scanning ? 1 : 0) + (refreshExecuted ? 1 : 0),
            "AddDataTypes FILES matcher setup/cleanup differs.");
        var outerRefreshLocks = refreshExecuted ? 2 : 0;
        var expectedRefreshLockCalls = outerRefreshLocks + refreshDateScans;
        var expectedRefreshUnlockCalls =
            (definition.RefreshSystemLockAvailable ? 1 : 0) +
            (definition.RefreshMorphOsLockAvailable ? 1 : 0) +
            refreshDateScans;
        Require(definition.RefreshLockCalls == expectedRefreshLockCalls &&
            definition.RefreshUnLockCalls == expectedRefreshUnlockCalls &&
            definition.RefreshSameLockCalls ==
                (refreshExecuted && definition.RefreshSystemLockAvailable &&
                    definition.RefreshMorphOsLockAvailable ? 1 : 0) &&
            definition.RefreshDosObjectAllocCalls == refreshDateScans &&
            definition.RefreshDosObjectFreeCalls == refreshDateScans &&
            definition.RefreshExamineCalls == refreshDateScans &&
            definition.RefreshExNextCalls == refreshDateScans &&
            definition.RefreshCompareDatesCalls ==
                (definition.RefreshEmptyDirectories ? 0 : refreshDateScans) &&
            definition.RefreshScanPatterns.SequenceEqual(
                expectedRefreshPatterns),
            "AddDataTypes REFRESH lock/date/scan behavior differs.");
        if (refreshExecuted)
        {
            Require(Bus.Long(invocation.Process +
                    (uint)DosLayout.Process.WindowPointer) == 0x12345678,
                "AddDataTypes REFRESH did not restore pr_WindowPtr.");
            var listStamp = AddDataTypesSharedList +
                (uint)NativeMorphOSAddDataTypesCommand.DataTypesListDateStamp;
            Require(Bus.Long(listStamp) ==
                    (refreshDateScans > 0 && !definition.RefreshDatesEqual
                        ? 2u : 0u) &&
                Bus.Long(listStamp + 4) ==
                    (refreshDateScans > 0 && !definition.RefreshDatesEqual
                        ? 6u : 0u) &&
                Bus.Long(listStamp + 8) ==
                    (refreshDateScans > 0 && !definition.RefreshDatesEqual
                        ? 10u : 0u),
                "AddDataTypes REFRESH did not update or preserve the list date stamp.");
        }
        var expectedCurrentDirCalls = workbenchFileMatched
            ? 4 : matchedDescriptorCount * 2;
        var workbenchLockOwned = invocation.Definition.Workbench &&
            !definition.WorkbenchArgumentListMissing &&
            !definition.WorkbenchArgumentCountOutOfRange;
        if (workbenchLockOwned)
            expectedCurrentDirCalls = definition.WorkbenchFiles ? 4 : 2;
        Require(definition.WorkbenchDupLockCalls ==
                (workbenchLockOwned ? 1 : 0) &&
            definition.WorkbenchUnlockCalls ==
                (workbenchLockOwned ? 1 : 0) &&
            definition.CurrentDirCalls == expectedCurrentDirCalls &&
            definition.FileOpenCalls == matchedFileCount &&
            definition.FileCloseCalls == matchedFileCount &&
            definition.IffAllocCalls == matchedFileCount &&
            definition.IffFreeCalls == matchedFileCount &&
            definition.IffOpenCalls == matchedFileCount &&
            definition.IffCloseCalls == matchedFileCount &&
            definition.IffInitDosCalls == matchedFileCount &&
            definition.IffPropChunksCalls == matchedFileCount &&
            definition.IffCollectionChunksCalls == matchedFileCount &&
            definition.IffStopOnExitCalls == matchedFileCount &&
            definition.IffParseCalls == matchedFileCount &&
            definition.IffFindPropCalls ==
                matchedFileCount *
                    (dthdPropertyPresent && definition.DthdHeaderBytes >=
                        NativeMorphOSAddDataTypesCommand.DataTypeHeaderSize
                            ? 2 : 1) &&
            definition.DatatypeAllocationCalls == (validDthd ? 1 : 0) &&
            definition.DatatypeFreeCalls == (candidateFreed ? 1 : 0) &&
            definition.ExistingDatatypeFreeCalls ==
                (oldDescriptorReplaced ? 1 : 0) &&
            definition.DatatypeRemoveCalls ==
                (oldDescriptorReplaced ? 2 : 0) &&
            definition.CodeCopyCalls == (validDtcd ? 1 : 0) &&
            definition.InternalLoadSegCalls == (validDtcd ? 1 : 0) &&
            definition.LoaderStateFreeCalls == (validDtcd ? 1 : 0) &&
            definition.CodeBufferFreeCalls ==
                (validDtcd && definition.InternalLoadSegFailure ||
                    candidateSegmentUnloaded ? 1 : 0) &&
            definition.UnLoadSegCalls ==
                (oldSegmentUnloaded || candidateSegmentUnloaded ? 1 : 0) &&
            definition.UnLoadedSegment ==
                (oldSegmentUnloaded ? AddDataTypesExistingSegment :
                    candidateSegmentUnloaded ? AddDataTypesLoadedSegment : 0),
            "AddDataTypes matched-file IFF parse lifecycle differs.");
        if (filesMatched)
            Require(definition.Events.IndexOf("close-iff") <
                    definition.Events.IndexOf("close-descriptor") &&
                definition.Events.IndexOf("close-descriptor") <
                    definition.Events.IndexOf("free-iff") &&
                definition.Events.IndexOf("free-iff") <
                    definition.Events.IndexOf("match-end"),
                "AddDataTypes IFF/file/matcher cleanup order differs.");
        VerifyBuiltins(descriptorListed, expectedExternalName);
        if (descriptorRegistered)
            VerifyRegisteredDthd();
        if (oldDescriptorRetained)
            VerifyExistingDthd(updateIds: descriptorCanBeAdded &&
                definition.ExistingDescriptorSame &&
                !definition.ExistingDescriptorOpen);
        if (oldDescriptorReplaced)
            VerifyReplacedExistingDthd();
        if (definition.ExistingDescriptorHasSegment &&
            !oldDescriptorReplaced)
            Require(Bus.Long(AddDataTypesExistingCompound +
                    (uint)NativeMorphOSAddDataTypesCommand
                        .CompoundDataTypeSegment) ==
                AddDataTypesExistingSegment,
                "AddDataTypes changed the segment retained by an open or same descriptor.");
        if (oldSegmentUnloaded)
            Require(definition.Events.IndexOf("unload-segment") >= 0 &&
                definition.Events.IndexOf("unload-segment") <
                    definition.Events.IndexOf("free-existing-descriptor"),
                "AddDataTypes freed a replaced descriptor before unloading its segment.");
        if (candidateSegmentUnloaded)
            Require(definition.Events.IndexOf("free-dtcd-code") >= 0 &&
                definition.Events.IndexOf("free-dtcd-code") <
                    definition.Events.IndexOf("unload-segment"),
                "AddDataTypes unloaded a candidate segment before releasing its copied loader input.");
        if (validDtcd && !definition.InternalLoadSegFailure)
            VerifyLoadedDtcd();
    }

    private void VerifyWorkbench31AddDataTypesEntry(Invocation invocation)
    {
        var definition = invocation.Definition.AddDataTypesList!;
        if (invocation.Definition.MissingDos)
        {
            Require(invocation.Opens == 0 && invocation.Closes == 0 &&
                definition.DosLibraryOpenCalls == 0 &&
                definition.UtilityOpenCalls == 0 &&
                definition.FindCalls == 0 && definition.ReadArgsCalls == 0,
                "Workbench AddDataTypes crossed its missing outer-DOS boundary.");
            return;
        }

        var openedDos = definition.DosLibraryOpenCalls == 1;
        var openedUtility = definition.UtilityOpenCalls == 1;
        var openedIntuition = definition.IntuitionOpenCalls == 1;
        var openedIff = definition.IffParseOpenCalls == 1;
        var openedLocale = definition.LocaleOpenCalls == 1;
        var allLibraries = openedDos && openedUtility && openedIntuition &&
            openedIff && openedLocale;
        var closeCatalogIndex = definition.Events.IndexOf("close-catalog");
        var closeLocaleIndex = definition.Events.IndexOf("close-locale");
        var localeFailure = openedDos && openedUtility && openedIntuition &&
            openedIff && definition.LocaleOpenCalls == 1 &&
            !definition.LocaleAvailable;
        if (localeFailure)
        {
            Require(invocation.Opens == 1 && invocation.Closes == 1 &&
                definition.DosLibraryCloseCalls == 1 &&
                definition.UtilityCloseCalls == 1 &&
                definition.IntuitionCloseCalls == 1 &&
                definition.IffParseCloseCalls == 1 &&
                definition.LocaleCloseCalls == 0 &&
                definition.CatalogOpenCalls == 0 &&
                definition.FindCalls == 0 && definition.ReadArgsCalls == 0 &&
                definition.Events.IndexOf("close-iffparse") <
                    definition.Events.IndexOf("close-intuition") &&
                definition.Events.IndexOf("close-intuition") <
                    definition.Events.IndexOf("close-utility") &&
                definition.Events.IndexOf("close-utility") <
                    definition.Events.IndexOf("close-dos"),
                "Workbench AddDataTypes locale-open failure did not unwind the acquired libraries in reverse order.");
            return;
        }

        Require(allLibraries && invocation.Opens == 1 &&
            invocation.Closes == 1 && definition.DosLibraryCloseCalls == 1 &&
            definition.UtilityCloseCalls == 1 &&
            definition.IntuitionCloseCalls == 1 &&
            definition.IffParseCloseCalls == 1 &&
            definition.LocaleCloseCalls == 1 &&
            definition.CatalogOpenCalls == 1 &&
            definition.CatalogCloseCalls ==
                (definition.CatalogAvailable ? 1 : 0) &&
            definition.Events.IndexOf("open-dos") <
                definition.Events.IndexOf("open-utility") &&
            definition.Events.IndexOf("open-utility") <
                definition.Events.IndexOf("open-intuition") &&
            definition.Events.IndexOf("open-intuition") <
                definition.Events.IndexOf("open-iffparse") &&
            definition.Events.IndexOf("open-iffparse") <
                definition.Events.IndexOf("open-locale") &&
            definition.Events.IndexOf("open-locale") <
                definition.Events.IndexOf("open-catalog") &&
            (closeCatalogIndex < 0 ||
                closeCatalogIndex < closeLocaleIndex),
            "Workbench AddDataTypes dependency order, optional catalog, or lease cleanup differs.");

        if (definition.CreateNamedObject &&
            !definition.UserDataAvailable)
        {
            var releaseObjectIndex =
                definition.Events.IndexOf("release-named-object");
            Require(invocation.Opens == 1 && invocation.Closes == 1 &&
                definition.FindCalls == 1 &&
                definition.NamedObjectAllocationCalls == 1 &&
                definition.NamedObjectReleaseCalls == 1 &&
                definition.NamedObjectAddCalls == 0 &&
                definition.NamedObjectFreeCalls == 0 &&
                definition.InitSemaphoreCalls == 0 &&
                definition.ObtainCalls == 0 &&
                definition.SemaphoreReleaseCalls == 0 &&
                definition.ReadArgsCalls == 0 &&
                definition.ResultAllocationCalls == 0 &&
                definition.ResultFreeCalls == 0 &&
                definition.AllocVecCalls == 0 &&
                definition.AddTailCalls == 0 &&
                definition.InsertCalls == 0 &&
                releaseObjectIndex >= 0 &&
                releaseObjectIndex < closeLocaleIndex,
                "Workbench AddDataTypes leaked an allocated named object with no user space.");
            return;
        }

        var classicFilesScan = definition.Files && !definition.Refresh &&
            definition.ParserError == 0 &&
            !definition.ResultAllocationFailure;
        var classicFilesMatched = classicFilesScan &&
            definition.FilesMatch;
        var classicValidDthd = classicFilesMatched &&
            definition.ValidDthd && IsAcceptedDthdHeader(definition);
        var classicValidDtcd = classicValidDthd && definition.ValidDtcd;
        var classicDateScanAvailable =
            definition.Refresh && definition.RefreshSystemLockAvailable;
        var classicScanRequired = definition.Refresh &&
            (!definition.RefreshSystemLockAvailable ||
                !definition.RefreshDatesEqual ||
                definition.RefreshEmptyDirectories);
        Require(definition.FindCalls == 1 &&
            definition.NamedObjectReleaseCalls == 1 &&
            definition.ObtainCalls == 1 &&
            definition.SemaphoreReleaseCalls == 1 &&
            definition.AllocVecCalls == 4 +
                (definition.Refresh ? 1 : 0) +
                (classicScanRequired ? 1 : 0) +
                (classicFilesScan ? 1 + definition.PatternCount : 0) +
                (classicValidDthd ? 1 : 0) +
                (classicValidDtcd ? 2 : 0) &&
            definition.AddTailCalls == 4 &&
            definition.InsertCalls == 4 +
                (classicValidDthd ? 2 : 0) &&
            definition.ReadArgsCalls == 1 && definition.FreeArgsCalls ==
                (definition.ParserError == 0 ? 1 : 0) &&
            definition.ResultAllocationCalls == 1 &&
            definition.ResultFreeCalls == 1,
            "Workbench AddDataTypes CLI/list/ReadArgs ownership differs.");

        var expectedClassicScans = new List<string>();
        if (classicScanRequired)
            expectedClassicScans.Add("DEVS:DataTypes/#?");
        var classicMatcherSetup = definition.Refresh || classicFilesScan;
        Require(definition.ExclusionParseCalls ==
                (classicMatcherSetup ? 1 : 0) &&
            definition.ExclusionFreeCalls ==
                (classicMatcherSetup ? 1 : 0) &&
            definition.RefreshLockCalls ==
                (definition.Refresh ? 1 : 0) &&
            definition.RefreshUnLockCalls ==
                (classicDateScanAvailable ? 1 : 0) &&
            definition.RefreshDosObjectAllocCalls ==
                (classicDateScanAvailable ? 1 : 0) &&
            definition.RefreshDosObjectFreeCalls ==
                (classicDateScanAvailable ? 1 : 0) &&
            definition.RefreshExamineCalls ==
                (classicDateScanAvailable ? 1 : 0) &&
            definition.RefreshExNextCalls ==
                (classicDateScanAvailable ? 1 : 0) &&
            definition.RefreshCompareDatesCalls ==
                (classicDateScanAvailable &&
                    !definition.RefreshEmptyDirectories ? 1 : 0) &&
            definition.MatchFirstCalls ==
                (classicScanRequired ? 1 : 0) +
                    (classicFilesScan ? definition.PatternCount : 0) &&
            definition.MatchEndCalls ==
                (classicScanRequired ? 1 : 0) +
                    (classicFilesScan ? definition.PatternCount : 0) &&
            definition.AnchorFreeCalls ==
                (classicScanRequired ? 1 : 0) +
                    (classicFilesScan ? definition.PatternCount : 0) &&
            definition.MatchNextCalls == (classicFilesMatched ? 1 : 0) &&
            definition.MatchPatternCalls ==
                (classicFilesMatched ? 1 : 0) &&
            definition.RefreshScanPatterns.SequenceEqual(
                expectedClassicScans),
            $"Workbench AddDataTypes REFRESH date-gate/cleanup differs: " +
            $"Parse/Free={definition.ExclusionParseCalls}/{definition.ExclusionFreeCalls}, " +
            $"Lock/Unlock={definition.RefreshLockCalls}/{definition.RefreshUnLockCalls}, " +
            $"FIB={definition.RefreshDosObjectAllocCalls}/{definition.RefreshDosObjectFreeCalls}, " +
            $"Examine/ExNext/Compare={definition.RefreshExamineCalls}/{definition.RefreshExNextCalls}/{definition.RefreshCompareDatesCalls}, " +
            $"MatchFirst/End/AnchorFree/Next={definition.MatchFirstCalls}/{definition.MatchEndCalls}/{definition.AnchorFreeCalls}/{definition.MatchNextCalls}, " +
            $"Patterns=[{string.Join(",", definition.RefreshScanPatterns)}].");
        if (definition.Refresh)
        {
            var listStamp = AddDataTypesSharedList +
                (uint)NativeMorphOSAddDataTypesCommand.DataTypesListDateStamp;
            var copiedDateStamp = classicDateScanAvailable &&
                !definition.RefreshDatesEqual &&
                !definition.RefreshEmptyDirectories;
            Require(Bus.Long(invocation.Process +
                    (uint)DosLayout.Process.WindowPointer) == 0x12345678 &&
                Bus.Long(listStamp) == (copiedDateStamp ? 2u : 0u) &&
                Bus.Long(listStamp + 4) == (copiedDateStamp ? 6u : 0u) &&
                Bus.Long(listStamp + 8) == (copiedDateStamp ? 10u : 0u),
                "Workbench AddDataTypes REFRESH did not restore requester state or update the date stamp.");
        }

        if (definition.CreateNamedObject)
        {
            Require(definition.NamedObjectAllocationCalls == 1 &&
                definition.NamedObjectAddCalls == 1 &&
                definition.NamedObjectFreeCalls == 0 &&
                definition.InitSemaphoreCalls == 1 &&
                definition.Events.IndexOf("find-named-object") <
                    definition.Events.IndexOf("alloc-named-object") &&
                definition.Events.IndexOf("init-semaphore") <
                    definition.Events.IndexOf("add-named-object") &&
                definition.Events.IndexOf("add-named-object") <
                    definition.Events.IndexOf("release-named-object"),
                "Workbench AddDataTypes did not initialize and publish its newly allocated shared object.");
        }
        else
        {
            Require(definition.NamedObjectAllocationCalls == 0 &&
                definition.NamedObjectAddCalls == 0 &&
                definition.InitSemaphoreCalls == 0,
                "Workbench AddDataTypes rebuilt an existing shared object.");
        }

        var releaseSemaphoreIndex =
            definition.Events.IndexOf("release-semaphore");
        var closeUtilityIndex =
            definition.Events.IndexOf("close-utility");
        Require(definition.Events.IndexOf("release-named-object") <
            definition.Events.IndexOf("obtain-semaphore") &&
            definition.Events.IndexOf("free-args") <
                releaseSemaphoreIndex &&
            (closeCatalogIndex >= 0
                ? releaseSemaphoreIndex < closeCatalogIndex &&
                    closeCatalogIndex < closeLocaleIndex
                : releaseSemaphoreIndex < closeLocaleIndex) &&
            definition.Events.IndexOf("close-locale") <
                definition.Events.IndexOf("close-iffparse") &&
            definition.Events.IndexOf("close-iffparse") <
                definition.Events.IndexOf("close-intuition") &&
            definition.Events.IndexOf("close-intuition") <
            closeUtilityIndex && closeUtilityIndex <
                definition.Events.IndexOf("close-dos"),
            "Workbench AddDataTypes shared-list/parser/library cleanup order differs.");
        Require(definition.AllocVecCalls == 4 +
                (definition.Refresh ? 1 : 0) +
                (classicScanRequired ? 1 : 0) +
                (classicFilesScan ? 1 + definition.PatternCount : 0) +
                (classicValidDthd ? 1 : 0) +
                (classicValidDtcd ? 2 : 0),
            "Workbench AddDataTypes REFRESH temporary allocations differ.");
        var expectedMatchedFiles = classicFilesMatched ? 1 : 0;
        Require(definition.FileOpenCalls == expectedMatchedFiles &&
            definition.FileCloseCalls == expectedMatchedFiles &&
            definition.IffAllocCalls == expectedMatchedFiles &&
            definition.IffFreeCalls == expectedMatchedFiles &&
            definition.IffOpenCalls == expectedMatchedFiles &&
            definition.IffCloseCalls == expectedMatchedFiles &&
            definition.IffInitDosCalls == expectedMatchedFiles &&
            definition.IffPropChunksCalls == expectedMatchedFiles &&
            definition.IffCollectionChunksCalls == expectedMatchedFiles &&
            definition.IffStopOnExitCalls == expectedMatchedFiles &&
            definition.IffParseCalls == expectedMatchedFiles &&
            definition.IffFindPropCalls == expectedMatchedFiles *
                (definition.ValidDthd ? 2 : 1) &&
            definition.DatatypeAllocationCalls ==
                (classicValidDthd ? 1 : 0) &&
            definition.CodeCopyCalls == (classicValidDtcd ? 1 : 0) &&
            definition.InternalLoadSegCalls == (classicValidDtcd ? 1 : 0) &&
            definition.LoaderStateFreeCalls == (classicValidDtcd ? 1 : 0) &&
            definition.InsertCalls == 4 + (classicValidDthd ? 2 : 0) &&
            definition.CurrentDirCalls == expectedMatchedFiles * 2 &&
            (expectedMatchedFiles == 0 ||
                definition.Events.IndexOf("close-iff") <
                    definition.Events.IndexOf("close-descriptor")),
            "Workbench AddDataTypes FILES/IFF lifecycle differs.");
        if (classicValidDthd)
            VerifyRegisteredDthd();
        if (classicValidDtcd)
            VerifyLoadedDtcd();
        VerifyBuiltins(classicValidDthd,
            classicValidDthd ? "test-type" : "");
    }

    private static bool IsAcceptedDthdHeader(
        AddDataTypesListEntryCase definition)
    {
        var headerBytes = definition.DthdHeaderBytes;
        if (headerBytes < NativeMorphOSAddDataTypesCommand.DataTypeHeaderSize)
            return false;

        static bool StringIsBounded(uint offset, uint textBytes,
            uint bytes) =>
            offset >= NativeMorphOSAddDataTypesCommand.DataTypeHeaderSize &&
            offset < bytes && textBytes < bytes - offset;

        if (!StringIsBounded(definition.DthdNameOffset, 9, headerBytes) ||
            !StringIsBounded(definition.DthdBaseNameOffset, 4, headerBytes) ||
            !StringIsBounded(definition.DthdPatternOffset, 2, headerBytes))
            return false;

        return definition.DthdMaskLength == 0 ||
            definition.DthdMaskOffset >=
                NativeMorphOSAddDataTypesCommand.DataTypeHeaderSize &&
            definition.DthdMaskOffset <= headerBytes &&
            (uint)definition.DthdMaskLength * 2 <=
                headerBytes - definition.DthdMaskOffset;
    }

    private void VerifyBuiltins(bool externalDescriptorPresent,
        string externalDescriptorName)
    {
        (string Name, ushort Flags, uint Id, uint Group, int TypeList)[] expected =
        {
            ("binary", 0, MakeId((byte)'b', (byte)'i', (byte)'n', (byte)'a'),
                MakeId((byte)'s', (byte)'y', (byte)'s', (byte)'t'),
                NativeMorphOSAddDataTypesCommand.DataTypesListBinary),
            ("ascii", 1, MakeId((byte)'a', (byte)'s', (byte)'c', (byte)'i'),
                MakeId((byte)'t', (byte)'e', (byte)'x', (byte)'t'),
                NativeMorphOSAddDataTypesCommand.DataTypesListAscii),
            ("iff", 2, MakeId((byte)'i', (byte)'f', (byte)'f', 0),
                MakeId((byte)'s', (byte)'y', (byte)'s', (byte)'t'),
                NativeMorphOSAddDataTypesCommand.DataTypesListIff),
            ("directory", 3, MakeId((byte)'d', (byte)'i', (byte)'r', (byte)'e'),
                MakeId((byte)'s', (byte)'y', (byte)'s', (byte)'t'),
                NativeMorphOSAddDataTypesCommand.DataTypesListMisc)
        };

        foreach (var item in expected)
        {
            var node = FindSortedNode(item.Name);
            var compound = NativeMorphOSAddDataTypesCommand
                .CompoundFromSortedNode(APTR.FromPointer(node));
            var namePointer = Bus.Long(node + 10);
            var header = compound.Raw +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeInlineHeader;
            var typeNames = ReadListNames(AddDataTypesSharedList +
                (uint)item.TypeList,
                NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode1);
            var expectedTypeNames = externalDescriptorPresent &&
                item.Name == "binary"
                ? new[] { "binary", externalDescriptorName }
                : new[] { item.Name };
            var nameLength = (uint)item.Name.Length;
            Require(compound.IsNotNull && namePointer ==
                compound.Raw + NativeMorphOSAddDataTypesCommand.CompoundDataTypeSize &&
                Bus.Long(compound.Raw +
                    (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeHeaderPointer) == header &&
                Bus.Long(header) == namePointer && Bus.Long(header + 4) == namePointer &&
                Bus.CString(namePointer) == item.Name &&
                Bus.Long(header + 16) == item.Group &&
                Bus.Long(header + 20) == item.Id &&
                Bus.Word(header + 28) == item.Flags &&
                Bus.Word(header + 30) == 0 &&
                Bus.Long(compound.Raw +
                    (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeLength) ==
                    NativeMorphOSAddDataTypesCommand.CompoundDataTypeSize + nameLength + 1 &&
                typeNames.SequenceEqual(expectedTypeNames),
                "AddDataTypes built-in node/header layout differs.");

            var toolList = compound.Raw +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeToolList;
            Require(Bus.Long(toolList) == toolList + 4 &&
                Bus.Long(toolList + 4) == 0 &&
                Bus.Long(toolList + 8) == toolList,
                "AddDataTypes built-in ToolList is not initialized.");
        }
    }

    private void VerifyRegisteredDthd()
    {
        var sortedNode = FindSortedNode("test-type");
        var compound = AddDataTypesRegisteredCompound;
        var inlineHeader = compound +
            (uint)NativeMorphOSAddDataTypesCommand
                .CompoundDataTypeInlineHeader;
        var payload = compound +
            NativeMorphOSAddDataTypesCommand.CompoundDataTypeSize;
        var binaryTypes = ReadListNames(AddDataTypesSharedList +
            (uint)NativeMorphOSAddDataTypesCommand.DataTypesListBinary,
            NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode1);
        Require(sortedNode == compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode2 &&
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeHeaderPointer) ==
                inlineHeader &&
            Bus.Long(inlineHeader) == payload &&
            Bus.Long(inlineHeader + 4) == payload + 10 &&
            Bus.Long(inlineHeader + 8) == payload + 15 &&
            Bus.Long(inlineHeader + 12) == 0 &&
            Bus.Long(inlineHeader + 16) == 0x74657374 &&
            Bus.Long(inlineHeader + 20) == 0x64617461 &&
            Bus.Word(inlineHeader + 24) == 0 &&
            Bus.Word(inlineHeader + 28) == 0 &&
            Bus.Word(inlineHeader + 30) == 0 &&
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeLength) ==
                NativeMorphOSAddDataTypesCommand.CompoundDataTypeInlineHeader +
                    AddDataTypesFileHeaderBytes &&
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeFlagLong) == 1 &&
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeParsePatternMemory) == 0 &&
            Bus.CString(payload) == "test-type" &&
            Bus.CString(payload + 10) == "test" &&
            Bus.CString(payload + 15) == "#?" &&
            Bus.Long(compound + 10) == payload &&
            Bus.Long(compound + 24) == payload &&
            binaryTypes.SequenceEqual(new[] { "binary", "test-type" }),
            "AddDataTypes valid DTHD payload copy or list registration differs.");
    }

    private void VerifyExistingDthd(bool updateIds)
    {
        var compound = AddDataTypesExistingCompound;
        var header = compound + (uint)NativeMorphOSAddDataTypesCommand
            .CompoundDataTypeInlineHeader;
        var sortedNode = FindSortedNode("test-type");
        var binaryTypes = ReadListNames(AddDataTypesSharedList +
            (uint)NativeMorphOSAddDataTypesCommand.DataTypesListBinary,
            NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode1);
        Require(sortedNode == compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode2 &&
            Bus.CString(Bus.Long(header)) == "TEST-TYPE" &&
            Bus.CString(Bus.Long(header + 4)) == "test" &&
            Bus.CString(Bus.Long(header + 8)) == "#?" &&
            Bus.Long(header + 16) ==
                (updateIds ? 0x74657374u : 0x6f6c6421u) &&
            Bus.Long(header + 20) ==
                (updateIds ? 0x64617461u : 0x70726576u) &&
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeOpenCount) ==
                (Bus.Current!.Definition.AddDataTypesList!.ExistingDescriptorOpen
                    ? 1u : 0u) &&
            binaryTypes.SequenceEqual(new[] { "binary", "TEST-TYPE" }),
            $"AddDataTypes preserved or updated the existing DTHD descriptor incorrectly: " +
                $"name={Bus.CString(Bus.Long(header))}, " +
                $"base={Bus.CString(Bus.Long(header + 4))}, " +
                $"pattern={Bus.CString(Bus.Long(header + 8))}, " +
                $"group={Bus.Long(header + 16):x8}, id={Bus.Long(header + 20):x8}, " +
                $"masklen={Bus.Word(header + 24)}, flags={Bus.Word(header + 28)}, " +
                $"priority={Bus.Word(header + 30)}, " +
                $"open={Bus.Long(compound + (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeOpenCount)}, " +
                $"binary=[{string.Join(",", binaryTypes)}], " +
                $"sorted={sortedNode:x8}, expected={compound + (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode2:x8}");
    }

    private void VerifyReplacedExistingDthd()
    {
        var compound = AddDataTypesExistingCompound;
        Require(Bus.Long(compound) == 0 && Bus.Long(compound + 4) == 0 &&
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode2) == 0 &&
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode2 + 4) == 0 &&
            FindSortedNode("test-type") == AddDataTypesRegisteredCompound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeNode2,
            "AddDataTypes did not unlink and replace a changed closed descriptor.");
    }

    private void VerifyLoadedDtcd()
    {
        var compound = AddDataTypesRegisteredCompound;
        Require(Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeCodeChunk) ==
                AddDataTypesCodeBuffer &&
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeCodeChunkSize) ==
                AddDataTypesCodeBytes &&
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeSegment) ==
                AddDataTypesLoadedSegment &&
            Bus.Long(compound +
                (uint)NativeMorphOSAddDataTypesCommand.CompoundDataTypeFunction) ==
                ((AddDataTypesLoadedSegment << 2) + 4) &&
            Bus.Memory.AsSpan((int)AddDataTypesCodeBuffer,
                (int)AddDataTypesCodeBytes).SequenceEqual(
                    Bus.Memory.AsSpan((int)AddDataTypesCodeProperty,
                        (int)AddDataTypesCodeBytes)) &&
            LoaderCallbacksAreResident(),
            "AddDataTypes DTCD segment metadata or owned copy differs.");
    }

    private bool LoaderCallbacksAreResident()
    {
        var definition = Bus.Current!.Definition.AddDataTypesList!;
        return definition.LoaderReadCallback >= LoadAddress &&
            definition.LoaderReadCallback < LoadAddress + 0x10000 &&
            definition.LoaderAllocCallback >= LoadAddress &&
            definition.LoaderAllocCallback < LoadAddress + 0x10000 &&
            definition.LoaderFreeCallback >= LoadAddress &&
            definition.LoaderFreeCallback < LoadAddress + 0x10000;
    }

    private uint FindSortedNode(string name)
    {
        var node = Bus.Long(AddDataTypesSharedList +
            (uint)NativeMorphOSAddDataTypesCommand.DataTypesListSorted);
        while (node != 0 && Bus.Long(node) != 0)
        {
            if (string.Equals(Bus.CString(Bus.Long(node + 10)), name,
                    StringComparison.OrdinalIgnoreCase))
                return node;
            node = Bus.Long(node);
        }
        return 0;
    }

    private List<string> ReadListNames(uint list, int nodeOffset)
    {
        var result = new List<string>();
        var node = Bus.Long(list);
        while (node != 0 && Bus.Long(node) != 0)
        {
            var compound = node - (uint)nodeOffset;
            result.Add(Bus.CString(Bus.Long(node + 10)));
            node = Bus.Long(node);
            Require(compound >= AddDataTypesAllocationBase,
                "AddDataTypes list node is not embedded in a compound descriptor.");
        }
        return result;
    }

    private void AddTail(uint list, uint node)
    {
        var predecessorSlot = Bus.Long(list + 8);
        var successorSlot = list + 4;
        Bus.Long(node, successorSlot);
        Bus.Long(node + 4, predecessorSlot);
        Bus.Long(predecessorSlot, node);
        Bus.Long(list + 8, node);
    }

    private void Insert(uint list, uint node, uint predecessor)
    {
        var predecessorSlot = predecessor == 0 ? list : predecessor;
        var successor = Bus.Long(predecessorSlot);
        Bus.Long(node, successor);
        Bus.Long(node + 4, predecessorSlot);
        Bus.Long(predecessorSlot, node);
        if (successor == list + 4)
            Bus.Long(list + 8, node);
        else
            Bus.Long(successor + 4, node);
    }

    private void RemoveNode(uint node)
    {
        var successor = Bus.Long(node);
        var predecessor = Bus.Long(node + 4);
        Require(successor != 0 && predecessor != 0,
            "AddDataTypes removed an unlinked node.");
        Bus.Long(predecessor, successor);
        Bus.Long(successor + 4, predecessor);
    }

    private static uint MakeId(byte a, byte b, byte c, byte d) =>
        ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | d;

    private void InitializeEmptyList(uint list)
    {
        // NewList: Head=&Tail, Tail=NULL, TailPred=&Head.
        Bus.Long(list, list + 4);
        Bus.Long(list + 4, 0);
        Bus.Long(list + 8, list);
    }

    private void WriteAddDataTypesCString(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }
}
