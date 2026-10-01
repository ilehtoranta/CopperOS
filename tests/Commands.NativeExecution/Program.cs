using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Amiga;
using Copper68k;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 4 && args[3] == LoadResourceProtocolRuntimeSuite.Suite)
            return LoadResourceProtocolRuntimeSuite.Run(args);
        if (args.Length == 4 && args[3] == LoadResourceHookRuntimeSuite.Suite)
            return LoadResourceHookRuntimeSuite.Run(args);
        if (args.Length == 4 && args[3] == LoadResourceRegistryRuntimeSuite.Suite)
            return LoadResourceRegistryRuntimeSuite.Run(args);
        if (args.Length == 4 && args[3] == LoadResourceWorkerRuntimeSuite.Suite)
            return LoadResourceWorkerRuntimeSuite.Run(args);
        if (args.Length == 4 && args[3] == LoadResourceActionsRuntimeSuite.Suite)
            return LoadResourceActionsRuntimeSuite.Run(args);
        if (args.Length == 4 && args[3] == LoadResourceLaunchRuntimeSuite.Suite)
            return LoadResourceLaunchRuntimeSuite.Run(args);
        if (args.Length == 4 && args[3] == ExecuteEntrySuite.Suite)
            return ExecuteEntrySuite.Run(args);
        if (args.Length == 5 && args[3] == AddBuffersReferenceSuite.Suite)
            return AddBuffersReferenceSuite.Run(args);
        if (args.Length == 5 && args[3] == RelabelReferenceSuite.Suite)
            return RelabelReferenceSuite.Run(args);
        if (args.Length is not (3 or 4))
        {
            Console.Error.WriteLine("usage: NativeExecution <probe.hunk> <68000|68020|68040> <report.json> [...|copy-metadata-native-entry-vector-fixture]");
            return 2;
        }
        var suite = args.Length == 4 ? args[3] : ProbeFixture.StartupSuite;
        var observationOnly = suite.EndsWith(":single-invocation", StringComparison.Ordinal) ||
            suite.EndsWith(":private-images", StringComparison.Ordinal);
        string? imageHash = null;
        try
        {
            var model = args[1] switch
            {
                "68000" => M68kCpuModel.M68000,
                "68020" => M68kCpuModel.M68020,
                "68040" => M68kCpuModel.M68040,
                _ => throw new ArgumentException("Unsupported CPU qualification target.")
            };
            var image = HunkImage.Load(args[0], ProbeFixture.LoadAddress);
            imageHash = image.Sha256;
            var fixture = new ProbeFixture(image, model, suite);
            var cases = fixture.Run();
            var report = new
            {
                schemaVersion = 1,
                status = observationOnly ? "observed" : "passed",
                observationOnly,
                suite,
                cpu = args[1],
                imageSha256 = fixture.Image.Sha256,
                imageBytes = new FileInfo(args[0]).Length,
                loadedBytes = fixture.Image.Code.Length,
                imageLoads = observationOnly ? cases.Count : 1,
                instructionExecutor = "Copper68k 1.4.0",
                instructionCorePath = typeof(M68kCoreFactory).Assembly.Location,
                instructionCoreSha256 = AssemblyHash(typeof(M68kCoreFactory).Assembly.Location),
                managedExecutorPath = typeof(Program).Assembly.Location,
                managedExecutorSha256 = AssemblyHash(typeof(Program).Assembly.Location),
                hostRuntimeVersion = Environment.Version.ToString(),
                realKickstartExecution = false,
                realCopperStartExecution = false,
                realDosParser = false,
                realDosIo = false,
                referenceCommandBehavior = false,
                shippingOrPureApproval = false,
                minimumStackQualified = false,
                sharedImageWrites = observationOnly ? (int?)null : 0,
                nativeWrites = fixture.Bus.NativeWrites,
                nativeReads = fixture.Bus.NativeReads,
                passed = observationOnly ? 0 : cases.Count,
                observedInvocations = observationOnly ? cases.Count : 0,
                cases
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
            File.WriteAllText(args[2], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            if (observationOnly)
                Console.WriteLine($"OBSERVED {model} {suite}: {cases.Count} fixture invocation(s); no parity or shipping approval.");
            else
                Console.WriteLine($"PASS {model} {suite}: {cases.Count} native invocations; one shared image; no leaked resources or image writes.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"FAIL native command qualification: {error.Message}");
            var failure = new
            {
                schemaVersion = 1, status = "failed", observationOnly, suite,
                cpu = args[1], imageSha256 = imageHash, failure = error.Message,
                shippingOrPureApproval = false
            };
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
                File.WriteAllText(args[2], JsonSerializer.Serialize(failure, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            }
            catch (Exception reportError) { Console.Error.WriteLine($"Could not write failure report: {reportError.Message}"); }
            return 1;
        }
    }

    private static string AssemblyHash(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
}

internal sealed record ProbeCase(string Name, string Arguments, int Result, int Error, string Output)
{
    public int? Number { get; init; }
    public bool FailSwitch { get; init; }
    public bool Workbench { get; init; }
    public bool BeepOpenFailure { get; init; }
    public WaitForPortEntryCase? WaitForPort { get; init; }
    public WaitForLibEntryCase? WaitForLib { get; init; }
    public WaitForNotificationEntryCase? WaitForNotification { get; init; }
    public bool MissingDos { get; init; }
    public bool AllocationFailure { get; init; }
    public int ParserError { get; init; }
    public int? ParserSuccessError { get; init; }
    public int? WriteResult { get; init; }
    public int? EntryLength { get; init; }
    public bool NullArgumentPointer { get; init; }
    public uint StackBytes { get; init; } = 0x4000;
    public ArgumentBoundaryCase? ArgumentBoundary { get; init; }
    public NativeIoCase? NativeIo { get; init; }
    public EvalEntryCase? Eval { get; init; }
    public PathPartEntryCase? PathPart { get; init; }
    public WhichEntryCase? Which { get; init; }
    public QuoteForwardProbeCase? QuoteForward { get; init; }
    public QuoteNativeEntryCase? QuoteEntry { get; init; }
    public TypeTextProbeCase? TypeText { get; init; }
    public TypeTextIoProbeCase? TypeTextIo { get; init; }
    public TypeNativeEntryCase? TypeEntry { get; init; }
    public SearchLiteralProbeCase? SearchLiteral { get; init; }
    public SearchLineProbeCase? SearchLine { get; init; }
    public SearchEntryCase? Search { get; init; }
    public DosListTraversalProbeCase? DosListTraversal { get; init; }
    public DosListEntryCase? DosList { get; init; }
    public FileNoteEntryCase? FileNote { get; init; }
    public BreakEntryCase? Break { get; init; }
    public ChangeTaskPriEntryCase? ChangeTaskPri { get; init; }
    public AddBuffersEntryCase? AddBuffers { get; init; }
    public AddDataTypesListEntryCase? AddDataTypesList { get; init; }
    public AvailEntryCase? Avail { get; init; }
    public FormatEntryCase? Format { get; init; }
    public BindDriversEntryCase? BindDrivers { get; init; }
    public LoadMonDrvsEntryCase? LoadMonDrvs { get; init; }
    public MountEntryCase? Mount { get; init; }
    public DateEntryCase? Date { get; init; }
    public DateEntryCase? MorphOSDate { get; init; }
    public SetDateEntryCase? SetDate { get; init; }
    public WaitEntryCase? Wait { get; init; }
    public bool Beep { get; init; }
    public RequestChoiceEntryCase? RequestChoice { get; init; }
    public RequestFileEntryCase? RequestFile { get; init; }
    public bool Reboot { get; init; }
    public int RebootParserError { get; init; }
    public bool RebootCtrlC { get; init; }
    public ResListEntryCase? ResList { get; init; }
    public LibListEntryCase? LibList { get; init; }
    public DevListEntryCase? DevList { get; init; }
    public PortListEntryCase? PortList { get; init; }
    public ModListEntryCase? ModList { get; init; }
    public StatusEntryCase? Status { get; init; }
    public ProtectEntryCase? Protect { get; init; }
    public TouchEntryCase? Touch { get; init; }
    public SetClockEntryCase? SetClock { get; init; }
    public VersionEntryCase? Version { get; init; }
    public FindResidentEntryCase? FindResident { get; init; }
    public SetKeyboardEntryCase? SetKeyboard { get; init; }
    public SetFontEntryCase? SetFont { get; init; }
    public Check2090EntryCase? Check2090 { get; init; }
    public IconPosEntryCase? IconPos { get; init; }
    public GuessBootDevEntryCase? GuessBootDev { get; init; }
    public ExtractKickstartEntryCase? ExtractKickstart { get; init; }
    public TaskListEntryCase? TaskList { get; init; }
    public InfoEntryCase? Info { get; init; }
    public DiskFreeEntryCase? DiskFree { get; init; }
    public DirEntryCase? Dir { get; init; }
    public MakeDirEntryCase? MakeDir { get; init; }
    public AssignEntryCase? Assign { get; init; }
    public JoinEntryCase? Join { get; init; }
    public ListEntryCase? List { get; init; }
    public LockEntryCase? Lock { get; init; }
    public DiskChangeEntryCase? DiskChange { get; init; }
    public RelabelEntryCase? Relabel { get; init; }
    public MakeLinkEntryCase? MakeLink { get; init; }
    public bool WritesOwnProcessError { get; init; }
    public MorphOSRenameCase? MorphOSRename { get; init; }
    public CopyLoopProbeCase? CopyLoop { get; init; }
    public DeleteObjectProbeCase? DeleteObject { get; init; }
    public DeleteCommandProbeCase? DeleteCommand { get; init; }
    public CopyModeSelectionProbeCase? CopyModeSelection { get; init; }
    public CopyArgumentGateEntryCase? CopyArgumentGate { get; init; }
    public CopyFilePairProbeCase? CopyFilePair { get; init; }
    public CopyDestinationProbeCase? CopyDestination { get; init; }
    public CopyDestinationDirectoriesProbeCase? CopyDestinationDirectories { get; init; }
    public CopyNonFileSystemProbeCase? CopyNonFileSystem { get; init; }
    public CopyLoopGuardProbeCase? CopyLoopGuard { get; init; }
    public CopyMetadataProbeCase? CopyMetadata { get; init; }
    public CopyResultPolicyProbeCase? CopyResultPolicy { get; init; }
    public CopyOpenDestinationProbeCase? CopyOpenDestination { get; init; }
    public CopyPatternClassifierProbeCase? CopyPatternClassifier { get; init; }
    public CopyFlatTraversalProbeCase? CopyFlatTraversal { get; init; }
    public CopyDirectoryExitProbeCase? CopyDirectoryExit { get; init; }
    public CopyDirectoryEntryProbeCase? CopyDirectoryEntry { get; init; }
    public CopyTraversalWorkProbeCase? CopyTraversalWork { get; init; }
    public CopyWorkProbeCase? CopyWork { get; init; }
    public CopyOutputProbeCase? CopyOutput { get; init; }
    public CopyDirectoryOperationProbeCase? CopyDirectoryOperation { get; init; }
    public CopyLinkOperationProbeCase? CopyLinkOperation { get; init; }
    public CopyFileOperationProbeCase? CopyFileOperation { get; init; }
    public CopyFileTransferProbeCase? CopyFileTransfer { get; init; }
    public CopyWorkPreparationProbeCase? CopyWorkPreparation { get; init; }
    public CopyTraversalProbeCase? CopyTraversal { get; init; }
    public CopyMatchStepProbeCase? CopyMatchStep { get; init; }
    public CopySoftLinkProbeCase? CopySoftLink { get; init; }
}

internal sealed partial class Invocation(ProbeCase definition, int slot)
{
    public const int InitialIoError = 31337;
    public ProbeCase Definition { get; } = definition;
    public int Slot { get; } = slot;
    public uint StackBytes => Definition.StackBytes;
    public uint Process { get; } = (uint)(0x10000 + slot * 0x1000);
    public uint Arguments { get; } = (uint)(0x20000 + slot *
        (definition.CopyFlatTraversal is null && definition.CopyMatchStep is null && definition.CopyTraversalWork is null ? 0x1000 : 0x10000));
    public uint StackTop { get; } = (uint)(0x40000 + slot * 0x10000);
    public uint DosBase { get; } = (uint)(0x8000 + slot * 0x1000);
    public uint ExpansionBase => 0xc000;
    public uint ExpansionConfig { get; } = (uint)(0xd000 + slot * 0x1000);
    public uint VersionModule { get; } = (uint)(0x6000 + slot * 0x1000);
    public uint OutputBptr { get; } = (uint)(0x100 + slot);
    public uint Port => Process + (uint)DosLayout.Process.MessagePort;
    public uint Message => Process + 0x300;
    public uint LowestStackWrite { get; set; } = (uint)(0x40000 + slot * 0x10000);
    public int IoError { get; set; } = InitialIoError;
    public int Opens { get; set; }
    public int Closes { get; set; }
    public int FileOpens { get; set; }
    public int FileCloses { get; set; }
    public int Allocations { get; set; }
    public int Reads { get; set; }
    public int FreeArgs { get; set; }
    public int FreeMem { get; set; }
    public int MountOpenCalls { get; set; }
    public int MountCurrentDirCalls { get; set; }
    public int MountFilePartCalls { get; set; }
    public int MountExpansionOpens { get; set; }
    public int MountExpansionCloses { get; set; }
    public int MountIconOpens { get; set; }
    public int MountIconCloses { get; set; }
    public int MountPrintFaultCalls { get; set; }
    public int BindDriversIconOpens { get; set; }
    public int BindDriversIconCloses { get; set; }
    public int BindDriversExpansionOpens { get; set; }
    public int BindDriversExpansionCloses { get; set; }
    public int BindDriversObtainCalls { get; set; }
    public int BindDriversReleaseCalls { get; set; }
    public int BindDriversMatchFirstCalls { get; set; }
    public int BindDriversMatchNextCalls { get; set; }
    public int BindDriversMatchEndCalls { get; set; }
    public int BindDriversNameFromLockCalls { get; set; }
    public int BindDriversAddPartCalls { get; set; }
    public int BindDriversGetDiskObjectNewCalls { get; set; }
    public int BindDriversFindToolTypeCalls { get; set; }
    public int BindDriversFreeDiskObjectCalls { get; set; }
    public int BindDriversLoadSegCalls { get; set; }
    public int BindDriversUnLoadSegCalls { get; set; }
    public int BindDriversSetCurrentBindingCalls { get; set; }
    public int BindDriversInitResidentCalls { get; set; }
    public int BindDriversTypeOfMemCalls { get; set; }
    public int BindDriversPrintFaultCalls { get; set; }
    public int BindDriversSetIoErrCalls { get; set; }
    public int BindDriversLockCalls { get; set; }
    public int BindDriversExamineCalls { get; set; }
    public int BindDriversExNextCalls { get; set; }
    public int BindDriversCurrentDirCalls { get; set; }
    public int BindDriversUnLockCalls { get; set; }
    public int BindDriversGetDiskObjectCalls { get; set; }
    public int BindDriversStrToLongCalls { get; set; }
    public uint WorkbenchBindDriversFib { get; set; }
    public bool BindDriversConfigChainValid { get; set; }
    public uint BindDriversAnchor { get; set; }
    public uint BindDriversCurrent { get; set; }
    public uint BindDriversDiskObject { get; set; }
    public uint BindDriversToolTypeArray { get; set; }
    public uint BindDriversProduct { get; set; }
    public uint BindDriversSegment { get; set; }
    public uint BindDriversSecondSegment { get; set; }
    public uint BindDriversResident { get; set; }
    public List<(uint Previous, int Manufacturer, int Product)>
        BindDriversFindConfigDevQueries { get; } = new();
    public uint BindDriversConfigDevFirst { get; set; }
    public uint BindDriversConfigDevSecond { get; set; }
    public uint BindDriversConfigDevThird { get; set; }
    public int LoadMonDrvsReadArgsCalls { get; set; }
    public int LoadMonDrvsFreeArgsCalls { get; set; }
    public int LoadMonDrvsAddPartCalls { get; set; }
    public int LoadMonDrvsMatchFirstCalls { get; set; }
    public int LoadMonDrvsMatchNextCalls { get; set; }
    public int LoadMonDrvsMatchEndCalls { get; set; }
    public int LoadMonDrvsNameFromLockCalls { get; set; }
    public int LoadMonDrvsLoadSegCalls { get; set; }
    public int LoadMonDrvsUnLoadSegCalls { get; set; }
    public int LoadMonDrvsInitResidentCalls { get; set; }
    public int LoadMonDrvsTypeOfMemCalls { get; set; }
    public uint LoadMonDrvsAnchor { get; set; }
    public uint LoadMonDrvsCurrent { get; set; }
    public uint LoadMonDrvsInfo { get; set; }
    public uint LoadMonDrvsSegment { get; set; }
    public uint LoadMonDrvsSecondSegment { get; set; }
    public uint LoadMonDrvsResident { get; set; }
    public int WaitDelayCalls { get; set; }
    public int WaitDelayTicks { get; set; }
    public int WaitForPortFindCalls { get; set; }
    public int WaitForPortDelayCalls { get; set; }
    public int WaitForPortDelayTicks { get; set; }
    public int WaitForPortSignalCalls { get; set; }
    public int WaitForPortPrintFaultCalls { get; set; }
    public uint WaitForPortReadArgs { get; set; }
    public int WaitForLibFindCalls { get; set; }
    public int WaitForLibDelayCalls { get; set; }
    public int WaitForLibDelayTicks { get; set; }
    public int WaitForLibSignalCalls { get; set; }
    public int WaitForLibPrintFaultCalls { get; set; }
    public uint WaitForLibReadArgs { get; set; }
    public int WaitForNotificationAllocSignalCalls { get; set; }
    public int WaitForNotificationFreeSignalCalls { get; set; }
    public int WaitForNotificationWaitCalls { get; set; }
    public int WaitForNotificationStartNotifyCalls { get; set; }
    public int WaitForNotificationEndNotifyCalls { get; set; }
    public int WaitForNotificationPrintFaultCalls { get; set; }
    public int WaitForNotificationStarted { get; set; }
    public uint WaitForNotificationReadArgs { get; set; }
    public int WaitPrintFaultCalls { get; set; }
    public int WaitMsgPortCreates { get; set; }
    public int WaitMsgPortDeletes { get; set; }
    public int WaitIoRequestCreates { get; set; }
    public int WaitIoRequestDeletes { get; set; }
    public int WaitSendIoCalls { get; set; }
    public int WaitCheckIoCalls { get; set; }
    public int WaitAbortIoCalls { get; set; }
    public int WaitIoCalls { get; set; }
    public int WaitSignalWaitCalls { get; set; }
    public uint WaitSignalMask { get; set; }
    public int WaitPutStrCalls { get; set; }
    public int Replies { get; set; }
    public int WaitPorts { get; set; }
    public int GetMessages { get; set; }
    public int TypeEntrySignals { get; set; }
    public int UtilityOpens { get; set; }
    public int UtilityCloses { get; set; }
    public uint SetKeyboardName { get; set; }
    public uint SetKeyboardSegmentAddress { get; set; }
    public uint SetKeyboardSegmentRaw { get; set; }
    public uint SetKeyboardSegmentAllocation { get; set; }
    public bool SetKeyboardSegmentAllocated { get; set; }
    public int SetKeyboardOpenResourceCalls { get; set; }
    public int SetKeyboardStricmpCalls { get; set; }
    public int SetKeyboardSetDefaultCalls { get; set; }
    public int SetKeyboardReadArgsCalls { get; set; }
    public int SetKeyboardTypeOfMemCalls { get; set; }
    public int SetKeyboardFreeArgsCalls { get; set; }
    public int SetKeyboardAddPartCalls { get; set; }
    public int SetKeyboardLoadSegCalls { get; set; }
    public int SetKeyboardUnLoadSegCalls { get; set; }
    public int SetKeyboardPrintFaultCalls { get; set; }
    public int SetKeyboardUtilityOpens { get; set; }
    public int SetKeyboardUtilityCloses { get; set; }
    public int SetKeyboardKeymapOpens { get; set; }
    public int SetKeyboardKeymapCloses { get; set; }
    public uint SetFontName { get; set; }
    public uint SetFontSourceName { get; set; }
    public uint SetFontSize { get; set; }
    public uint SetFontConsoleTask { get; set; }
    public uint SetFontConsoleStorage { get; set; }
    public uint SetFontWindow { get; set; }
    public uint SetFontRastPort { get; set; }
    public uint SetFontOldFont { get; set; }
    public uint SetFontFont { get; set; }
    public uint SetFontInfoData { get; set; }
    public uint SetFontTextAttr { get; set; }
    public int SetFontGraphicsOpens { get; set; }
    public int SetFontGraphicsCloses { get; set; }
    public int SetFontDiskfontOpens { get; set; }
    public int SetFontDiskfontCloses { get; set; }
    public int SetFontUtilityOpens { get; set; }
    public int SetFontUtilityCloses { get; set; }
    public int SetFontReadArgsCalls { get; set; }
    public int SetFontFreeArgsCalls { get; set; }
    public int SetFontDoPktCalls { get; set; }
    public int SetFontOpenDiskFontCalls { get; set; }
    public int SetFontSetFontCalls { get; set; }
    public int SetFontCloseFontCalls { get; set; }
    public int SetFontForbidCalls { get; set; }
    public int SetFontPermitCalls { get; set; }
    public int SetFontPutStrCalls { get; set; }
    public int SetFontFlushCalls { get; set; }
    public int SetFontPrintFaultCalls { get; set; }
    public int DateStampCalls { get; set; }
    public int DateToStrCalls { get; set; }
    public int StrToDateCalls { get; set; }
    public int TimerOpenCalls { get; set; }
    public int TimerCloseCalls { get; set; }
    public int IntuitionOpens { get; set; }
    public int IntuitionCloses { get; set; }
    public int DisplayBeeps { get; set; }
    public int RequestChoiceLocks { get; set; }
    public int RequestChoiceUnlocks { get; set; }
    public int RequestChoiceEasyCalls { get; set; }
    public int RequestChoiceBuildCalls { get; set; }
    public int RequestChoiceSysReqCalls { get; set; }
    public int RequestChoiceFreeCalls { get; set; }
    public int RequestChoiceTimerWaits { get; set; }
    public int RequestChoiceTimerAborts { get; set; }
    public int AslOpens { get; set; }
    public int AslCloses { get; set; }
    public int RequestFileReadArgsCalls { get; set; }
    public int RequestFileFreeArgsCalls { get; set; }
    public int RequestFileAllocVecCalls { get; set; }
    public int RequestFileFreeVecCalls { get; set; }
    public int RequestFileAllocCalls { get; set; }
    public int RequestFileFreeCalls { get; set; }
    public int RequestFileRequests { get; set; }
    public int RequestFileVPrintfCalls { get; set; }
    public int RequestFilePutStrCalls { get; set; }
    public uint RequestFileLayout { get; set; }
    public uint RequestFileBuffer { get; set; }
    public uint RequestFileRdArgs { get; set; }
    public int RequestFileExecMemCalls { get; set; }
    public int RequestFileFreeMemCalls { get; set; }
    public int LockDeviceProcCalls { get; set; }
    public int LockFreeDeviceProcCalls { get; set; }
    public int LockPacketCalls { get; set; }
    public int LockPacketArgument { get; set; }
    public uint LockPacketKey { get; set; }
    public int LockPrintFaultCalls { get; set; }
    public LockNativeLayout? LockLayout { get; set; }
    public int DiskChangeDeviceProcCalls { get; set; }
    public int DiskChangePacketCalls { get; set; }
    public int DiskChangePrintFaultCalls { get; set; }
    public DiskChangeNativeLayout? DiskChangeLayout { get; set; }
    public int RebootReadArgsCalls { get; set; }
    public int RebootFreeArgsCalls { get; set; }
    public int RebootPrintFaultCalls { get; set; }
    public int RebootSetSignalCalls { get; set; }
    public int RebootColdRebootCalls { get; set; }
    public int ResListAllocVecCalls { get; set; }
    public int ResListFreeVecCalls { get; set; }
    public int ResListForbidCalls { get; set; }
    public int ResListPermitCalls { get; set; }
    public int ResListSetSignalCalls { get; set; }
    public int ResListVPrintfCalls { get; set; }
    public int ResListFPutsCalls { get; set; }
    public int ResListPrintFaultCalls { get; set; }
    public ResListNativeLayout? ResListLayout { get; set; }
    public int LibListAllocVecCalls { get; set; }
    public int LibListFreeVecCalls { get; set; }
    public int LibListForbidCalls { get; set; }
    public int LibListPermitCalls { get; set; }
    public int LibListSetSignalCalls { get; set; }
    public int LibListVPrintfCalls { get; set; }
    public int LibListFPutsCalls { get; set; }
    public int LibListPrintFaultCalls { get; set; }
    public LibListNativeLayout? LibListLayout { get; set; }
    public int DevListAllocVecCalls { get; set; }
    public int DevListFreeVecCalls { get; set; }
    public int DevListForbidCalls { get; set; }
    public int DevListPermitCalls { get; set; }
    public int DevListSetSignalCalls { get; set; }
    public int DevListVPrintfCalls { get; set; }
    public int DevListFPutsCalls { get; set; }
    public int DevListPrintFaultCalls { get; set; }
    public DevListNativeLayout? DevListLayout { get; set; }
    public int PortListAllocVecCalls { get; set; }
    public int PortListFreeVecCalls { get; set; }
    public int PortListForbidCalls { get; set; }
    public int PortListPermitCalls { get; set; }
    public int PortListSetSignalCalls { get; set; }
    public int PortListVPrintfCalls { get; set; }
    public int PortListFPutsCalls { get; set; }
    public int PortListPrintFaultCalls { get; set; }
    public PortListNativeLayout? PortListLayout { get; set; }
    public int ModListReadArgsCalls { get; set; }
    public int ModListFreeArgsCalls { get; set; }
    public int ModListAllocVecCalls { get; set; }
    public int ModListFreeVecCalls { get; set; }
    public int ModListForbidCalls { get; set; }
    public int ModListPermitCalls { get; set; }
    public int ModListSetSignalCalls { get; set; }
    public int ModListVPrintfCalls { get; set; }
    public int ModListFPutsCalls { get; set; }
    public int ModListPrintFaultCalls { get; set; }
    public ModListNativeLayout? ModListLayout { get; set; }
    public int StatusReadArgsCalls { get; set; }
    public int StatusFreeArgsCalls { get; set; }
    public int StatusVPrintfCalls { get; set; }
    public int StatusSetSignalCalls { get; set; }
    public int StatusModernQueryCalls { get; set; }
    public int StatusModernFreeCalls { get; set; }
    public StatusNativeLayout? StatusLayout { get; set; }
    public int ProtectAllocVecCalls { get; set; }
    public int ProtectFreeVecCalls { get; set; }
    public int ProtectSetProtectionCalls { get; set; }
    public ProtectNativeLayout? ProtectLayout { get; set; }
    public int TouchDateStampCalls { get; set; }
    public int TouchMatchFirstCalls { get; set; }
    public int TouchMatchNextCalls { get; set; }
    public int TouchMatchEndCalls { get; set; }
    public int TouchCurrentDirCalls { get; set; }
    public int TouchSetFileDateCalls { get; set; }
    public int TouchOpenCalls { get; set; }
    public int TouchCloseCalls { get; set; }
    public TouchNativeLayout? TouchLayout { get; set; }
    public SetClockNativeLayout? SetClockLayout { get; set; }
    public VersionNativeLayout? VersionLayout { get; set; }
    public FindResidentNativeLayout? FindResidentLayout { get; set; }
    public int VersionFindResidentCalls { get; set; }
    public int VersionVPrintfCalls { get; set; }
    public int VersionPutStrCalls { get; set; }
    public int VersionSystemSeparatorCalls { get; set; }
    public int VersionLibraryOpenCalls { get; set; }
    public int VersionLibraryCloseCalls { get; set; }
    public int VersionUtilityOpenCalls { get; set; }
    public int VersionUtilityCloseCalls { get; set; }
    public int VersionForbidCalls { get; set; }
    public int VersionPermitCalls { get; set; }
    public int VersionPrintFaultCalls { get; set; }
    public int VersionReadArgsCalls { get; set; }
    public int VersionFreeArgsCalls { get; set; }
    public int VersionAllocMemCalls { get; set; }
    public int VersionFreeMemCalls { get; set; }
    public int VersionFileOpens { get; set; }
    public int VersionFileCloses { get; set; }
    public int VersionDateParseCalls { get; set; }
    public int VersionDateFormatCalls { get; set; }
    public int FindResidentFindResidentCalls { get; set; }
    public int FindResidentReadArgsCalls { get; set; }
    public int FindResidentFreeArgsCalls { get; set; }
    public int FindResidentPrintFaultCalls { get; set; }
    public int FindResidentIoErrCalls { get; set; }
    public int FindResidentSetIoErrCalls { get; set; }
    public int Check2090ExpansionOpens { get; set; }
    public int Check2090ExpansionCloses { get; set; }
    public int Check2090FindConfigDevCalls { get; set; }
    public TaskListNativeLayout? TaskListLayout { get; set; }
    public uint TaskListRegisterCheckAddress { get; set; }
    public int TaskListReadArgsCalls { get; set; }
    public int TaskListFreeArgsCalls { get; set; }
    public int TaskListAllocVecCalls { get; set; }
    public int TaskListFreeVecCalls { get; set; }
    public int TaskListForbidCalls { get; set; }
    public int TaskListPermitCalls { get; set; }
    public int TaskListAttrCalls { get; set; }
    public int TaskListTypeAttrCalls { get; set; }
    public int TaskListPidAttrCalls { get; set; }
    public int TaskListRegisterAttrCalls { get; set; }
    public int TaskListSystemAttrCalls { get; set; }
    public List<uint> TaskListSystemAttrSelectors { get; } = [];
    public int TaskListFPutsCalls { get; set; }
    public int TaskListVPrintfCalls { get; set; }
    public int TaskListRegisterVPrintfCalls { get; set; }
    public int TaskListVerboseVPrintfCalls { get; set; }
    public int TaskListTypeOfMemCalls { get; set; }
    public int TaskListPrintFaultCalls { get; set; }
    public int TaskListSysDebugOpenCalls { get; set; }
    public int TaskListSysDebugCloseCalls { get; set; }
    public int TaskListSegTrackerAcquireCalls { get; set; }
    public int TaskListSegTrackerReleaseCalls { get; set; }
    public int TaskListSegTrackerFindCalls { get; set; }
    public int TaskListRegisterCheckRows { get; set; }
    public int TaskListRegisterCheckForbidCalls { get; set; }
    public int TaskListRegisterCheckPermitCalls { get; set; }
    public int TaskListRegisterCheckPpcBoundsCalls { get; set; }
    public bool TaskListSegTrackerHeld { get; set; }
    public InfoNativeLayout? InfoLayout { get; set; }
    public int InfoUtilityLibraryOpens { get; set; }
    public int InfoUtilityLibraryCloses { get; set; }
    public int InfoLocaleLibraryOpens { get; set; }
    public int InfoLocaleLibraryCloses { get; set; }
    public int InfoOpenLocaleCalls { get; set; }
    public int InfoCloseLocaleCalls { get; set; }
    public int InfoStricmpCalls { get; set; }
    public int InfoGetVarCalls { get; set; }
    public int InfoGetFileSysAttrCalls { get; set; }
    public DiskFreeNativeLayout? DiskFreeLayout { get; set; }
    public DosListNativeLayout? DosListLayout { get; set; }
    public ListNativeLayout? ListLayout { get; set; }
    public DirNativeLayout? DirLayout { get; set; }
    public MakeDirEntryNativeLayout? MakeDirLayout { get; set; }
    public AssignNativeLayout? AssignLayout { get; set; }
    public int ListReadArgsCalls { get; set; }
    public int ListFreeArgsCalls { get; set; }
    public int ListAllocMemCalls { get; set; }
    public int ListFreeMemCalls { get; set; }
    public int ListSetSignalCalls { get; set; }
    public int ListMatchFirstCalls { get; set; }
    public int ListMatchNextCalls { get; set; }
    public int ListMatchEndCalls { get; set; }
    public int ListParsePatternCalls { get; set; }
    public List<string> ListParsePatternInputs { get; } = [];
    public int ListMatchPatternCalls { get; set; }
    public int ListDateToStrCalls { get; set; }
    public int ListStrToDateCalls { get; set; }
    public int ListOpenCalls { get; set; }
    public int ListCloseCalls { get; set; }
    public int ListSelectOutputCalls { get; set; }
    public int ListVPrintfCalls { get; set; }
    public int ListFPutsCalls { get; set; }
    public int ListPrintFaultCalls { get; set; }
    public int DosListReadArgsCalls { get; set; }
    public int DosListFreeArgsCalls { get; set; }
    public int DosListAllocMemCalls { get; set; }
    public int DosListFreeMemCalls { get; set; }
    public int DosListAllocVecCalls { get; set; }
    public int DosListFreeVecCalls { get; set; }
    public int DosListAttemptLockCalls { get; set; }
    public int DosListNextCalls { get; set; }
    public int DosListDeviceNextCalls { get; set; }
    public int DosListVolumeNextCalls { get; set; }
    public int DosListAssignNextCalls { get; set; }
    public uint DosListCurrentKind { get; set; }
    public int DosListUnlockCalls { get; set; }
    public int DosListSetSignalCalls { get; set; }
    public int DosListVPrintfCalls { get; set; }
    public int DosListFPutsCalls { get; set; }
    public int DosListPrintFaultCalls { get; set; }
    public uint? InfoRdArgs { get; set; }
    public int InfoReadArgsCalls { get; set; }
    public int InfoFreeArgsCalls { get; set; }
    public int InfoAllocVecCalls { get; set; }
    public int InfoFreeVecCalls { get; set; }
    public int InfoAllocMemCalls { get; set; }
    public int InfoFreeMemCalls { get; set; }
    public int InfoSetSignalCalls { get; set; }
    public int InfoLockCalls { get; set; }
    public int InfoUnlockCalls { get; set; }
    public int InfoNextCalls { get; set; }
    public int InfoIsFileSystemCalls { get; set; }
    public int InfoParsePatternCalls { get; set; }
    public int InfoMatchPatternCalls { get; set; }
    public int InfoPathLockCalls { get; set; }
    public int InfoPathUnlockCalls { get; set; }
    public int InfoNameFromLockCalls { get; set; }
    public string InfoCurrentDeviceName { get; set; } = "";
    public int InfoCalls { get; set; }
    public int InfoFPutsCalls { get; set; }
    public int InfoVolumeTypeCalls { get; set; }
    public int InfoVPrintfCalls { get; set; }
    public int InfoPrintFaultCalls { get; set; }
    public uint? DiskFreeRdArgs { get; set; }
    public int DiskFreeAllocMemCalls { get; set; }
    public int DiskFreeFreeMemCalls { get; set; }
    public int DiskFreeReadArgsCalls { get; set; }
    public int DiskFreeFreeArgsCalls { get; set; }
    public int DiskFreeLockCalls { get; set; }
    public int DiskFreeUnlockCalls { get; set; }
    public int DiskFreeInfoCalls { get; set; }
    public int DiskFreeVPrintfCalls { get; set; }
    public int DiskFreePrintFaultCalls { get; set; }
    public int DirReadArgsCalls { get; set; }
    public int DirFreeArgsCalls { get; set; }
    public int DirAllocVecCalls { get; set; }
    public int DirFreeVecCalls { get; set; }
    public int DirAllocDosObjectCalls { get; set; }
    public int DirFreeDosObjectCalls { get; set; }
    public int DirLockCalls { get; set; }
    public int DirUnlockCalls { get; set; }
    public int DirExAllCalls { get; set; }
    public int DirVPrintfCalls { get; set; }
    public int DirPrintFaultCalls { get; set; }
    public int SetClockOpenResourceCalls { get; set; }
    public int SetClockMsgPortCreates { get; set; }
    public int SetClockMsgPortDeletes { get; set; }
    public int SetClockIoRequestCreates { get; set; }
    public int SetClockIoRequestDeletes { get; set; }
    public int SetClockOpenDeviceCalls { get; set; }
    public int SetClockCloseDeviceCalls { get; set; }
    public int SetClockDoIoCalls { get; set; }
    public int SetClockReadCalls { get; set; }
    public int SetClockWriteCalls { get; set; }
    public int SetClockResetCalls { get; set; }
    public int SetClockGetSysTimeCalls { get; set; }
    public int SetClockReadUtcCalls { get; set; }
    public int SetClockWriteUtcCalls { get; set; }
    public int SetClockGetUtcSysTimeCalls { get; set; }
    public int SetClockPutStrCalls { get; set; }
    public int TimerDoIoCalls { get; set; }
    public int DateFileOpens { get; set; }
    public int DateFileCloses { get; set; }
    public int DateVPrintfCalls { get; set; }
    public int DateVFPrintfCalls { get; set; }
    public int DatePrintFaultCalls { get; set; }
    public int DateWriteCalls { get; set; }
    public int DatePutStrCalls { get; set; }
    public int DateMsgPortCreates { get; set; }
    public int DateMsgPortDeletes { get; set; }
    public int DateIoRequestCreates { get; set; }
    public int DateIoRequestDeletes { get; set; }
    public int LocaleOpens { get; set; }
    public int LocaleCloses { get; set; }
    public int LocaleOpenLocaleCalls { get; set; }
    public int LocaleCloseLocaleCalls { get; set; }
    public int LocaleFormatDateCalls { get; set; }
    public MorphOSDateNativeLayout? MorphOSDateLayout { get; set; }
    public int SetDateAllocVecCalls { get; set; }
    public int SetDateFreeVecCalls { get; set; }
    public int SetDateDateStampCalls { get; set; }
    public int SetDateStrToDateCalls { get; set; }
    public int SetDateMatchFirstCalls { get; set; }
    public int SetDateMatchNextCalls { get; set; }
    public int SetDateMatchEndCalls { get; set; }
    public int SetDateDupLockCalls { get; set; }
    public int SetDateCurrentDirCalls { get; set; }
    public int SetDateUnLockCalls { get; set; }
    public int SetDateFileDateCalls { get; set; }
    public int SetDatePrintFaultCalls { get; set; }
    public bool Forbidden { get; set; }
    public int Instructions { get; set; }
    public NativeIoInvocation? NativeIo { get; set; }
    public QuoteForwardNativeLayout? QuoteForwardLayout { get; set; }
    public TypeTextNativeLayout? TypeTextLayout { get; set; }
    public TypeTextIoNativeLayout? TypeTextIoLayout { get; set; }
    public TypeNativeEntryLayout? TypeEntryLayout { get; set; }
    public SearchLiteralNativeLayout? SearchLiteralLayout { get; set; }
    public SearchLineNativeLayout? SearchLineLayout { get; set; }
    public DosListTraversalNativeLayout? DosListTraversalLayout { get; set; }
    public FileNoteNativeLayout? FileNoteLayout { get; set; }
    public int BreakFindCliProcCalls { get; set; }
    public int BreakFindTaskByPIDCalls { get; set; }
    public int BreakFindPortCalls { get; set; }
    public int BreakSignals { get; set; }
    public uint BreakSignalMask { get; set; }
    public int ChangeTaskPriCalls { get; set; }
    public int ChangeTaskPriFindTaskByPIDCalls { get; set; }
    public int ChangeTaskPriPriority { get; set; }
    public int DosListLocks { get; set; }
    public int DosListUnlocks { get; set; }
    public int DosListNext { get; set; }
    public int DosListSignals { get; set; }
    public AddBuffersNativeLayout? AddBuffersLayout { get; set; }
    public FormatNativeLayout? FormatLayout { get; set; }
    public RelabelNativeLayout? RelabelLayout { get; set; }
    public MakeLinkNativeLayout? MakeLinkLayout { get; set; }
    public CopyLoopNativeLayout? CopyLoopLayout { get; set; }
    public DeleteCommandNativeLayout? DeleteCommandLayout { get; set; }
    public CopyModeSelectionNativeLayout? CopyModeSelectionLayout { get; set; }
    public CopyArgumentGateNativeLayout? CopyArgumentGateLayout { get; set; }
    public CopyFilePairNativeLayout? CopyFilePairLayout { get; set; }
    public CopyDestinationNativeLayout? CopyDestinationLayout { get; set; }
    public CopyDestinationDirectoriesNativeLayout? CopyDestinationDirectoriesLayout { get; set; }
    public CopyNonFileSystemNativeLayout? CopyNonFileSystemLayout { get; set; }
    public CopyLoopGuardNativeLayout? CopyLoopGuardLayout { get; set; }
    public CopyMetadataNativeLayout? CopyMetadataLayout { get; set; }
    public CopyResultPolicyNativeLayout? CopyResultPolicyLayout { get; set; }
    public CopyOpenDestinationNativeLayout? CopyOpenDestinationLayout { get; set; }
    public CopyPatternClassifierNativeLayout? CopyPatternClassifierLayout { get; set; }
    public CopyFlatTraversalNativeLayout? CopyFlatTraversalLayout { get; set; }
    public CopyDirectoryExitNativeLayout? CopyDirectoryExitLayout { get; set; }
    public CopyDirectoryEntryNativeLayout? CopyDirectoryEntryLayout { get; set; }
    public CopyTraversalWorkNativeLayout? CopyTraversalWorkLayout { get; set; }
    public CopyWorkNativeLayout? CopyWorkLayout { get; set; }
    public CopyOutputNativeLayout? CopyOutputLayout { get; set; }
    public CopyDirectoryOperationNativeLayout? CopyDirectoryOperationLayout { get; set; }
    public CopyLinkOperationNativeLayout? CopyLinkOperationLayout { get; set; }
    public CopyFileOperationNativeLayout? CopyFileOperationLayout { get; set; }
    public CopyFileTransferNativeLayout? CopyFileTransferLayout { get; set; }
    public CopyWorkPreparationNativeLayout? CopyWorkPreparationLayout { get; set; }
    public CopyTraversalNativeLayout? CopyTraversalLayout { get; set; }
    public CopyMatchStepNativeLayout? CopyMatchStepLayout { get; set; }
    public CopySoftLinkNativeLayout? CopySoftLinkLayout { get; set; }
    public JoinNativeLayout? JoinLayout { get; set; }
    public List<string> Events { get; } = [];
    public List<uint> AllocationRequests { get; } = [];
    public MemoryStream Output { get; } = new();
}

internal sealed partial class ProbeFixture
{
    public const uint LoadAddress = 0x100000;
    private const uint ExecBase = 0x4000;
    private const uint IntuitionBase = 0x5000;
    private const uint AslBase = 0x6000;
    private const uint DateUtilityBase = 0xa000;
    private const uint DateLocaleBase = 0xb000;
    private const uint InfoUtilityBase = 0xb400;
    private const uint VersionUtilityBase = 0x5f000;
    private const uint InfoLocaleBase = 0xb800;
    private const uint InfoLocaleObject = 0xbc00;
    private const uint TaskListSysDebugBase = 0xc000;
    private const uint FindTaskByPidGateway = 0x7000;
    private const uint ReturnAddress = 0x2000;
    private readonly M68kCpuModel model;
    private readonly string suite;
    private readonly bool bindDriversSingleInvocationProbe;
    private readonly bool bindDriversPrivateImageProbe;
    private readonly bool unifiedCopyRoot;
    private readonly bool explicitCopyParser;
    private readonly bool copyCommandRoot;
    private readonly bool singleCommandRoot;
    private readonly bool directoryCommandRoot;
    private readonly bool recursiveCommandRoot;
    private readonly bool addDataTypesCallbackProbe;
    private readonly bool workbench31AddDataTypes;
    private readonly bool workbench31BindDrivers;
    public HunkImage Image { get; }
    public CommandTestBus Bus { get; } = new();
    private bool IsTransferLoopProbeSuite => suite == CopyLoopProbeSuite ||
        suite == JoinAppendLoopProbeSuite;

    public ProbeFixture(HunkImage image, M68kCpuModel model, string suite)
    {
        addDataTypesCallbackProbe = suite == AddDataTypesCallbackProbeSuite;
        if (addDataTypesCallbackProbe) suite = AddDataTypesListEntrySuite;
        workbench31AddDataTypes = suite == Workbench31AddDataTypesEntrySuite;
        if (workbench31AddDataTypes) suite = AddDataTypesListEntrySuite;
        workbench31BindDrivers = suite == Workbench31BindDriversEntrySuite;
        if (workbench31BindDrivers) suite = BindDriversEntrySuite;
        bindDriversSingleInvocationProbe = suite == BindDriversEntrySuite + ":single-invocation";
        bindDriversPrivateImageProbe = bindDriversSingleInvocationProbe ||
            suite == BindDriversEntrySuite + ":private-images";
        if (bindDriversPrivateImageProbe) suite = BindDriversEntrySuite;
        recursiveCommandRoot=suite.EndsWith("+recursive-command",StringComparison.Ordinal);
        if(recursiveCommandRoot)suite=suite[..^18]+"+directory-command";
        directoryCommandRoot=suite.EndsWith("+directory-command",StringComparison.Ordinal);
        if(directoryCommandRoot){suite=suite[..^18];Require(suite==CopyTargetDispatchSuite,"Directory command selector requires target suite.");}
        singleCommandRoot=suite.EndsWith("+single-command",StringComparison.Ordinal);
        if(singleCommandRoot){suite=suite[..^15];Require(suite==CopySingleTargetSuite,"Single command selector requires target suite.");}
        copyCommandRoot = suite.EndsWith("+command", StringComparison.Ordinal);
        if(copyCommandRoot) {suite=suite[..^8];Require(suite is CopyDirectSuite or CopyParsedDeleteSuite or CopyParsedMakeDirectorySuite,"Command fixture covers parsed operation lifecycle.");suite+="+explicit-parser";}
        explicitCopyParser = suite.EndsWith("+explicit-parser", StringComparison.Ordinal);
        if (explicitCopyParser) suite = suite[..^16] + "+unified-copy";
        unifiedCopyRoot = suite.EndsWith("+unified-copy", StringComparison.Ordinal);
        if (unifiedCopyRoot) { suite = suite[..^13]; Require(suite is CopyDirectSuite or CopyParsedDeleteSuite or CopyParsedMakeDirectorySuite, "Unified Copy layout is only valid for parsed operation suites."); }
            if (suite != WaitForPortEntrySuite && suite != WaitForLibEntrySuite &&
                suite != WaitForNotificationEntrySuite)
            Require(suite == WorkbenchSearchEntrySuite ||
            suite == RequestChoiceEntrySuite ||
            suite == Workbench31RequestChoiceEntrySuite ||
            suite == RequestFileEntrySuite ||
            suite == Workbench31RequestFileEntrySuite ||
            suite == AddDataTypesListEntrySuite ||
            suite is ListEntrySuite or WorkbenchListEntrySuite || IsDirEntrySuite(suite) || IsMakeDirEntrySuite(suite) || IsAssignEntrySuite(suite) || IsJoinEntrySuite(suite) || suite is StartupSuite or ArgumentBoundarySuite or NativeIoSuite or
            EvalEntrySuite or WorkbenchEvalEntrySuite or PathPartEntrySuite or
            WorkbenchWhichEntrySuite or MorphOSWhichEntrySuite or QuoteForwardProbeSuite or
            QuoteNativeEntrySuite or TypeTextProbeSuite or TypeTextIoProbeSuite or
            ExtractKickstartEntrySuite or MountEntrySuite or Workbench31MountEntrySuite or BindDriversEntrySuite or BindDriversProductParserEntrySuite or LoadMonDrvsEntrySuite or
            ExtractKickstartEntrySuite or MountEntrySuite or Workbench31MountEntrySuite or BindDriversEntrySuite or BindDriversProductParserEntrySuite or
            TypeNativeEntrySuite or Workbench31TypeNativeEntrySuite or SearchLiteralProbeSuite or SearchLineProbeSuite or SearchEntrySuite or WorkbenchSearchEntrySuite or DosListTraversalProbeSuite or DosListEntrySuite or FileNoteEntrySuite or WorkbenchFileNoteEntrySuite or TouchEntrySuite or SetClockEntrySuite or Workbench31SetClockEntrySuite or Workbench31SetKeyboardEntrySuite or MorphOSSetKeyboardEntrySuite or Workbench31SetFontEntrySuite or VersionEntrySuite or Workbench31VersionEntrySuite or FindResidentEntrySuite or Check2090EntrySuite or IconPosEntrySuite or GuessBootDevEntrySuite or TaskListEntrySuite or InfoEntrySuite or Workbench31InfoEntrySuite or DiskFreeEntrySuite or BreakEntrySuite or Workbench31BreakEntrySuite or ChangeTaskPriEntrySuite or Workbench31ChangeTaskPriEntrySuite or AddBuffersEntrySuite or Workbench31AvailEntrySuite or MorphOSAvailEntrySuite or FormatEntrySuite or Workbench31DateEntrySuite or MorphOSDateEntrySuite or Workbench31SetDateEntrySuite or MorphOSSetDateEntrySuite or Workbench31WaitEntrySuite or MorphOSWaitEntrySuite or BeepEntrySuite or LockEntrySuite or Workbench31LockEntrySuite or DiskChangeEntrySuite or Workbench31DiskChangeEntrySuite or RebootEntrySuite or Workbench31RebootEntrySuite or ResListEntrySuite or LibListEntrySuite or DevListEntrySuite or PortListEntrySuite or ModListEntrySuite or StatusEntrySuite or Workbench31StatusEntrySuite or ProtectEntrySuite or Workbench31ProtectEntrySuite or RelabelEntrySuite or MakeLinkEntrySuite or WorkbenchMakeLinkEntrySuite or WorkbenchRenameStartupSuite or MorphOSRenameSuite or CopyLoopProbeSuite or JoinAppendLoopProbeSuite or DeleteObjectProbeSuite or DeleteCommandProbeSuite or WorkbenchDeleteCommandProbeSuite or CopyModeSelectionProbeSuite or CopyArgumentGateEntrySuite or CopyOptionSetupSuite or CopyDirectSuite or CopyParsedDeleteSuite or CopyParsedMakeDirectorySuite or CopyFilePairProbeSuite or CopyDestinationProbeSuite or CopyDestinationDirectoriesProbeSuite or CopyNonFileSystemProbeSuite or CopyLoopGuardProbeSuite or CopyMetadataProbeSuite or CopyResultPolicyProbeSuite or CopyCompletionSuite or CopyOpenDestinationProbeSuite or CopyPatternClassifierProbeSuite or CopyFlatTraversalProbeSuite or CopyDirectoryExitProbeSuite or CopyDirectoryEntryProbeSuite or CopyTraversalWorkProbeSuite or CopySourceRoutingProbeSuite or CopyDirectorySourcesSuite or CopyTargetDispatchSuite or CopySingleTargetSuite or CopyWorkProbeSuite or CopyOutputProbeSuite or CopyDirectoryOperationProbeSuite or CopyLinkOperationProbeSuite or CopyFileOperationProbeSuite or CopyTransferProbeSuite or CopyWorkPreparationProbeSuite or CopyTraversalProbeSuite or CopyMatchStepProbeSuite or CopySoftLinkProbeSuite or CopyMatchedDirectoryProbeSuite,
            "Unknown native qualification suite.");
        Image = image;
        this.model = model;
        this.suite = suite;
        Bus.Long(4, ExecBase);
        if (!bindDriversPrivateImageProbe)
            Bus.LoadImage(LoadAddress, image.Code, image.ReadOnlyRanges, image.WritableRanges);
        RegisterExec();
        if (suite == AddDataTypesListEntrySuite)
        {
            RegisterAddDataTypesListEntryExec();
            RegisterAddDataTypesListEntryIffParse();
            if (workbench31AddDataTypes)
                RegisterAddDataTypesListDos(AddDataTypesWorkbenchDosBase);
        }
        if (suite == NativeIoSuite) RegisterIoExec();
        if (suite == TypeTextIoProbeSuite) RegisterTypeTextIoExec();
        if (IsTypeNativeEntrySuite(suite)) RegisterTypeNativeEntryExec();
        if (IsSearchEntrySuite(suite)) RegisterSearchEntryExec();
        if (suite == DosListTraversalProbeSuite) RegisterDosListTraversalExec();
        if (suite == DosListEntrySuite) RegisterDosListEntryExec();
        if (IsFileNoteEntrySuite(suite)) RegisterFileNoteEntryExec();
        if (suite == TouchEntrySuite) RegisterTouchEntryExec();
        if (suite == SetClockEntrySuite || suite == Workbench31SetClockEntrySuite)
            RegisterSetClockEntryExec();
        if (suite == Workbench31SetKeyboardEntrySuite)
            RegisterSetKeyboardEntryExec();
        if (suite == MorphOSSetKeyboardEntrySuite)
            RegisterMorphOSSetKeyboardEntryExec();
        if (suite == Workbench31SetFontEntrySuite)
            RegisterSetFontEntryExec();
        if (IsVersionEntrySuite(suite)) RegisterVersionEntryExec();
        if (suite == FindResidentEntrySuite) RegisterFindResidentEntryExec();
        if (suite == Check2090EntrySuite) RegisterCheck2090EntryExec();
        if (IsBindDriversEntrySuite(suite))
        {
            if (workbench31BindDrivers)
                RegisterWorkbench31BindDriversExpansion(0xc000);
            else if (bindDriversPrivateImageProbe)
                RegisterWorkbenchBindDriversReferenceExpansion(0xc000);
            else
                RegisterBindDriversEntryExpansion(0xc000);
        }
        if (suite == BindDriversEntrySuite)
        {
            if (workbench31BindDrivers)
            {
                RegisterWorkbench31BindDriversExec(ExecBase);
                RegisterWorkbench31BindDriversIcon(Invocation.IconPosBase);
            }
            else if (bindDriversPrivateImageProbe)
            {
                RegisterWorkbenchBindDriversReferenceExec(ExecBase);
                RegisterWorkbenchBindDriversReferenceIcon(Invocation.IconPosBase);
            }
            else
            {
                RegisterBindDriversEntryExec(ExecBase);
                RegisterBindDriversEntryIcon(Invocation.IconPosBase);
            }
        }
        if (suite == LoadMonDrvsEntrySuite)
            RegisterLoadMonDrvsEntryExec();
        if (suite == IconPosEntrySuite) RegisterIconPosEntryIcon();
        if (suite == GuessBootDevEntrySuite) RegisterGuessBootDevEntryExec();
        if (suite == ExtractKickstartEntrySuite) RegisterExtractKickstartEntryExec();
        if (suite == TaskListEntrySuite) RegisterTaskListEntryExec();
        if (suite == InfoEntrySuite) RegisterInfoEntryExec();
        if (suite == Workbench31InfoEntrySuite) RegisterWorkbench31InfoEntryExec();
        if (IsDirEntrySuite(suite)) RegisterDirEntryExec();
        if (IsAssignEntrySuite(suite)) RegisterAssignEntryExec();
        if (IsJoinEntrySuite(suite)) RegisterJoinEntryExec();
        if (suite is ListEntrySuite or WorkbenchListEntrySuite) RegisterListEntryExec();
        if (suite == BreakEntrySuite) RegisterBreakEntryExec();
        if (suite == Workbench31BreakEntrySuite) RegisterWorkbench31BreakEntryExec();
        if (suite == ChangeTaskPriEntrySuite) RegisterChangeTaskPriEntryExec();
        if (suite == Workbench31ChangeTaskPriEntrySuite) RegisterWorkbench31ChangeTaskPriEntryExec();
        if (IsAvailEntrySuite(suite)) RegisterWorkbench31AvailEntryExec();
        if (suite == FormatEntrySuite) RegisterFormatEntryExec();
        if (suite == Workbench31DateEntrySuite) RegisterWorkbench31DateEntryExec();
        if (suite == MorphOSDateEntrySuite) RegisterMorphOSDateEntryExec();
        if (suite == Workbench31SetDateEntrySuite) RegisterWorkbench31SetDateEntryExec();
        if (suite == MorphOSSetDateEntrySuite) RegisterMorphOSSetDateEntryExec();
        if (suite == Workbench31WaitEntrySuite) RegisterWorkbench31WaitEntryExec();
        if (suite == MorphOSWaitEntrySuite) RegisterMorphOSWaitEntryExec();
        if (suite == WaitForPortEntrySuite) RegisterWaitForPortEntryExec();
        if (suite == WaitForLibEntrySuite) RegisterWaitForLibEntryExec();
        if (suite == WaitForNotificationEntrySuite) RegisterWaitForNotificationEntryExec();
        if (suite == BeepEntrySuite) RegisterBeepEntryExec();
        if (suite == RequestChoiceEntrySuite || suite == Workbench31RequestChoiceEntrySuite)
        {
            RegisterRequestChoiceEntryExec();
            if (suite == RequestChoiceEntrySuite)
                RegisterRequestChoiceEntryTimer(ExecBase);
        }
        if (suite == RequestFileEntrySuite || suite == Workbench31RequestFileEntrySuite)
            RegisterRequestFileEntryAsl();
        if (suite == RebootEntrySuite || suite == Workbench31RebootEntrySuite)
        {
            Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "Reboot Ctrl-C query ABI differs.");
                invocation.RebootSetSignalCalls++;
                return invocation.Definition.RebootCtrlC ? 1u << 12 : 0u;
            });
            Register(ExecBase, ExecLvo.ColdReboot, "ColdReboot",
                (_, invocation) => { invocation.RebootColdRebootCalls++; return 0; });
        }
        if (suite == ResListEntrySuite)
        {
            Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "ResList Ctrl-C query ABI differs.");
                invocation.ResListSetSignalCalls++;
                return invocation.Definition.ResList!.CtrlC ? 1u << 12 : 0;
            });
        }
        if (suite == LibListEntrySuite)
        {
            Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "LibList Ctrl-C query ABI differs.");
                invocation.LibListSetSignalCalls++;
                return invocation.Definition.LibList!.CtrlC ? 1u << 12 : 0;
            });
        }
        if (suite == DevListEntrySuite)
        {
            Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "DevList Ctrl-C query ABI differs.");
                invocation.DevListSetSignalCalls++;
                return invocation.Definition.DevList!.CtrlC ? 1u << 12 : 0;
            });
        }
        if (suite == PortListEntrySuite)
        {
            Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "PortList Ctrl-C query ABI differs.");
                invocation.PortListSetSignalCalls++;
                return invocation.Definition.PortList!.CtrlC ? 1u << 12 : 0;
            });
        }
        if (suite == ModListEntrySuite)
        {
            Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "ModList Ctrl-C query ABI differs.");
                invocation.ModListSetSignalCalls++;
                return invocation.Definition.ModList!.CtrlC ? 1u << 12 : 0;
            });
        }
        if (IsStatusEntrySuite(suite))
        {
            Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "Status Ctrl-C query ABI differs.");
                invocation.StatusSetSignalCalls++;
                return invocation.Definition.Status!.CtrlC ? 1u << 12 : 0;
            });
        }
        if (suite == TaskListEntrySuite)
        {
            Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "TaskList Ctrl-C query ABI differs.");
                return invocation.Definition.TaskList!.CtrlC ? 1u << 12 : 0;
            });
        }
        if (suite == TouchEntrySuite)
        {
            Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0x1000,
                    "Touch Ctrl-C query ABI differs.");
                return invocation.Definition.Touch!.CtrlC ? 0x1000u : 0u;
            });
        }
        if (IsTransferLoopProbeSuite) RegisterCopyLoopProbeExec();
        if (suite == CopyFilePairProbeSuite) RegisterCopyFilePairProbeExec();
        if (suite == CopyCompletionSuite) Register(ExecBase,ExecLvo.SetSignal,"SetSignal",(s,i)=>{ Require(s.D[0]==0&&s.D[1]==0&&i.Definition.CopyResultPolicy!.RetVal2==0,"Completion signal query must be secondary-OK only.");return (i.Definition.CopyResultPolicy.Flags&1)!=0?4096u:0; });
        if (IsDeleteCommandEntrySuite(suite)) Register(ExecBase, ExecLvo.SetSignal,
            "SetSignal", (s, i) => { Require(s.D[0] == 0 && s.D[1] == 0,
                "Delete Ctrl-C query must not clear signals."); return 0; });
        if (suite is CopyTraversalWorkProbeSuite or CopySourceRoutingProbeSuite or CopyDirectorySourcesSuite or CopyTargetDispatchSuite or CopySingleTargetSuite or CopyDirectSuite or CopyParsedDeleteSuite or CopyParsedMakeDirectorySuite) RegisterCopyTraversalWorkProbeExec();
        if (suite == CopyWorkProbeSuite) RegisterCopyWorkProbeExec();
        if (suite == CopyFileOperationProbeSuite) RegisterCopyFileOperationProbeExec();
        if (suite == CopyFileTransferProbeSuite) RegisterCopyFileTransferProbeExec();
        RegisterDos(0x8000);
        RegisterDos(0x9000);
    }

    public List<object> Run()
    {
        if (addDataTypesCallbackProbe)
            return RunAddDataTypesCallbackProbeCases();
        if (suite == AddDataTypesListEntrySuite)
            return workbench31AddDataTypes
                ? RunWorkbench31AddDataTypesEntryCases()
                : RunAddDataTypesListEntryCases();
        if (suite == ArgumentBoundarySuite) return RunArgumentBoundaryCases();
        if (suite == NativeIoSuite) return RunNativeIoCases();
        if (suite == EvalEntrySuite) return RunEvalEntryCases();
        if (suite == WorkbenchEvalEntrySuite) return RunWorkbenchEvalEntryCases();
        if (suite == PathPartEntrySuite) return RunPathPartEntryCases();
        if (suite == WorkbenchWhichEntrySuite) return RunWorkbenchWhichEntryCases();
        if (suite == MorphOSWhichEntrySuite) return RunMorphOSWhichEntryCases();
        if (suite == QuoteForwardProbeSuite) return RunQuoteForwardProbeCases();
        if (suite == QuoteNativeEntrySuite) return RunQuoteNativeEntryCases();
        if (suite == TypeTextProbeSuite) return RunTypeTextProbeCases();
        if (suite == TypeTextIoProbeSuite) return RunTypeTextIoProbeCases();
        if (IsTypeNativeEntrySuite(suite)) return RunTypeNativeEntryCases();
        if (suite == SearchLiteralProbeSuite) return RunSearchLiteralProbeCases();
        if (suite == SearchLineProbeSuite) return RunSearchLineProbeCases();
        if (IsSearchEntrySuite(suite)) return RunSearchEntryCases();
        if (suite == DosListTraversalProbeSuite) return RunDosListTraversalProbeCases();
        if (suite == DosListEntrySuite) return RunDosListEntryCases();
        if (suite is ListEntrySuite or WorkbenchListEntrySuite) return RunListEntryCases();
        if (IsFileNoteEntrySuite(suite)) return RunFileNoteEntryCases();
        if (suite == TouchEntrySuite) return RunTouchEntryCases();
        if (suite == SetClockEntrySuite || suite == Workbench31SetClockEntrySuite)
            return RunSetClockEntryCases();
        if (suite == Workbench31SetKeyboardEntrySuite)
            return RunSetKeyboardEntryCases();
        if (suite == MorphOSSetKeyboardEntrySuite)
            return RunMorphOSSetKeyboardEntryCases();
        if (suite == Workbench31SetFontEntrySuite)
            return RunSetFontEntryCases();
        if (suite == VersionEntrySuite) return RunVersionEntryCases();
        if (suite == Workbench31VersionEntrySuite)
            return RunWorkbench31VersionEntryCases();
        if (suite == FindResidentEntrySuite) return RunFindResidentEntryCases();
        if (suite == Check2090EntrySuite) return RunCheck2090EntryCases();
        if (suite == IconPosEntrySuite) return RunIconPosEntryCases();
        if (suite == GuessBootDevEntrySuite) return RunGuessBootDevEntryCases();
        if (suite == ExtractKickstartEntrySuite) return RunExtractKickstartEntryCases();
        if (suite == TaskListEntrySuite) return RunTaskListEntryCases();
        if (suite == InfoEntrySuite) return RunInfoEntryCases();
        if (suite == Workbench31InfoEntrySuite) return RunWorkbench31InfoEntryCases();
        if (suite == DiskFreeEntrySuite) return RunDiskFreeEntryCases();
        if (IsDirEntrySuite(suite)) return RunDirEntryCases();
        if (IsMakeDirEntrySuite(suite)) return RunMakeDirEntryCases();
        if (IsAssignEntrySuite(suite)) return RunAssignEntryCases();
        if (IsJoinEntrySuite(suite)) return RunJoinEntryCases();
        if (suite == BreakEntrySuite) return RunBreakEntryCases();
        if (suite == Workbench31BreakEntrySuite) return RunWorkbench31BreakEntryCases();
        if (suite == ChangeTaskPriEntrySuite) return RunChangeTaskPriEntryCases();
        if (suite == Workbench31ChangeTaskPriEntrySuite) return RunWorkbench31ChangeTaskPriEntryCases();
        if (suite == Workbench31AvailEntrySuite)
            return RunWorkbench31AvailEntryCases();
        if (suite == MorphOSAvailEntrySuite)
            return RunMorphOSAvailEntryCases();
        if (suite == FormatEntrySuite) return RunFormatEntryCases();
        if (suite == MountEntrySuite) return RunMountEntryCases();
        if (suite == Workbench31MountEntrySuite) return RunWorkbenchMountEntryCases();
        if (suite == BindDriversEntrySuite) return RunBindDriversEntryCases();
        if (suite == BindDriversProductParserEntrySuite)
            return RunBindDriversProductParserEntryCases();
        if (suite == LoadMonDrvsEntrySuite) return RunLoadMonDrvsEntryCases();
        if (suite == Workbench31DateEntrySuite) return RunWorkbench31DateEntryCases();
        if (suite == MorphOSDateEntrySuite) return RunMorphOSDateEntryCases();
        if (suite == Workbench31SetDateEntrySuite) return RunWorkbench31SetDateEntryCases();
        if (suite == MorphOSSetDateEntrySuite) return RunMorphOSSetDateEntryCases();
        if (suite == Workbench31WaitEntrySuite) return RunWorkbench31WaitEntryCases();
        if (suite == MorphOSWaitEntrySuite) return RunMorphOSWaitEntryCases();
        if (suite == WaitForPortEntrySuite) return RunWaitForPortEntryCases();
        if (suite == WaitForLibEntrySuite) return RunWaitForLibEntryCases();
        if (suite == WaitForNotificationEntrySuite) return RunWaitForNotificationEntryCases();
        if (suite == BeepEntrySuite) return RunBeepEntryCases();
        if (suite == RequestChoiceEntrySuite) return RunRequestChoiceEntryCases();
        if (suite == Workbench31RequestChoiceEntrySuite)
            return RunWorkbench31RequestChoiceEntryCases();
        if (suite == RequestFileEntrySuite || suite == Workbench31RequestFileEntrySuite)
            return RunRequestFileEntryCases();
        if (suite == LockEntrySuite || suite == Workbench31LockEntrySuite)
            return RunLockEntryCases();
        if (suite == DiskChangeEntrySuite || suite == Workbench31DiskChangeEntrySuite)
            return RunDiskChangeEntryCases();
        if (suite == RebootEntrySuite || suite == Workbench31RebootEntrySuite)
            return RunRebootEntryCases();
        if (suite == ResListEntrySuite) return RunResListEntryCases();
        if (suite == LibListEntrySuite) return RunLibListEntryCases();
        if (suite == DevListEntrySuite) return RunDevListEntryCases();
        if (suite == PortListEntrySuite) return RunPortListEntryCases();
        if (suite == ModListEntrySuite) return RunModListEntryCases();
        if (IsStatusEntrySuite(suite)) return RunStatusEntryCases();
        if (IsProtectEntrySuite(suite)) return RunProtectEntryCases();
        if (suite == AddBuffersEntrySuite) return RunAddBuffersEntryCases();
        if (suite == RelabelEntrySuite) return RunRelabelEntryCases();
        if (suite == MakeLinkEntrySuite) return RunMakeLinkEntryCases();
        if (suite == WorkbenchRenameStartupSuite) return RunWorkbenchRenameStartupCases();
        if (suite == MorphOSRenameSuite) return RunMorphOSRenameCases();
        if (suite == WorkbenchMakeLinkEntrySuite) return RunWorkbenchMakeLinkCases();
        if (IsTransferLoopProbeSuite) return RunCopyLoopProbeCases();
        if (suite == DeleteObjectProbeSuite) return RunDeleteObjectProbeCases();
        if (IsDeleteCommandEntrySuite(suite)) return RunDeleteCommandProbeCases();
        if (suite == CopyModeSelectionProbeSuite) return RunCopyModeSelectionProbeCases();
        if (suite is CopyArgumentGateEntrySuite or CopyOptionSetupSuite or CopyDirectSuite or CopyParsedDeleteSuite or CopyParsedMakeDirectorySuite) return RunCopyArgumentGateEntryCases();
        if (suite == CopyFilePairProbeSuite) return RunCopyFilePairProbeCases();
        if (suite == CopyDestinationProbeSuite) return RunCopyDestinationProbeCases();
        if (suite == CopyDestinationDirectoriesProbeSuite) return RunCopyDestinationDirectoriesProbeCases();
        if (suite == CopyNonFileSystemProbeSuite) return RunCopyNonFileSystemProbeCases();
        if (suite == CopyLoopGuardProbeSuite) return RunCopyLoopGuardProbeCases();
        if (suite == CopyMetadataProbeSuite) return RunCopyMetadataProbeCases();
        if (suite is CopyResultPolicyProbeSuite or CopyCompletionSuite) return RunCopyResultPolicyProbeCases();
        if (suite == CopyOpenDestinationProbeSuite) return RunCopyOpenDestinationProbeCases();
        if (suite == CopyPatternClassifierProbeSuite) return RunCopyPatternClassifierProbeCases();
        if (suite == CopyFlatTraversalProbeSuite) return RunCopyFlatTraversalProbeCases();
        if (suite == CopyDirectoryExitProbeSuite) return RunCopyDirectoryExitProbeCases();
        if (suite == CopyDirectoryEntryProbeSuite) return RunCopyDirectoryEntryProbeCases();
        if (suite is CopyTraversalWorkProbeSuite or CopySourceRoutingProbeSuite or CopyDirectorySourcesSuite or CopyTargetDispatchSuite or CopySingleTargetSuite) return RunCopyTraversalWorkProbeCases();
        if (suite == CopyWorkProbeSuite) return RunCopyWorkProbeCases();
        if (suite == CopyOutputProbeSuite) return RunCopyOutputProbeCases();
        if (suite == CopyDirectoryOperationProbeSuite) return RunCopyDirectoryOperationProbeCases();
        if (suite == CopyLinkOperationProbeSuite) return RunCopyLinkOperationProbeCases();
        if (suite == CopyFileOperationProbeSuite) return RunCopyFileOperationProbeCases();
        if (suite == CopyFileTransferProbeSuite) return RunCopyFileTransferProbeCases();
        if (suite == CopyWorkPreparationProbeSuite) return RunCopyWorkPreparationProbeCases();
        if (suite == CopyTraversalProbeSuite) return RunCopyTraversalProbeCases();
        if (suite == CopyMatchStepProbeSuite) return RunCopyMatchStepProbeCases();
        if (suite is CopySoftLinkProbeSuite or CopyMatchedDirectoryProbeSuite) return RunCopySoftLinkProbeCases();
        // These are supplied DOS results, not a replacement ReadArgs parser.
        // The suite tests native calling conventions, storage, and ownership.
        ProbeCase[] cases =
        [
            new("number", "VALUE 42\n", 42, 0, "VALUE 42\n") { Number = 42 },
            new("numeric-zero-pointer", "VALUE 0\n", 0, 0, "VALUE 0\n") { Number = 0 },
            new("minimum-numeric-value", "VALUE -2147483648\n", int.MinValue, 0, "VALUE -2147483648\n") { Number = int.MinValue },
            new("negative-and-byte-preservation", "VALUE=-7 ÃƒÂ¤ *\"\n", -7, 0, "VALUE=-7 ÃƒÂ¤ *\"\n") { Number = -7 },
            new("missing-number", "\n", 1, 0, "\n"),
            new("empty-arguments", "", 0, 0, ""),
            new("switch-failure", "FAIL\n", 10, 205, "") { FailSwitch = true },
            new("allocation-failure", "VALUE 99\n", 20, 103, "") { Number = 99, AllocationFailure = true },
            new("bad-number", "VALUE abc\n", 10, 115, "") { ParserError = 115 },
            new("too-many-arguments", "VALUE 1 excess\n", 10, 118, "") { ParserError = 118 },
            new("short-write", "VALUE 42\n", 10, 221, "VAL") { Number = 42, WriteResult = 3 },
            new("write-failure", "VALUE 42\n", 10, 221, "") { Number = 42, WriteResult = -1 },
            new("missing-dos", "VALUE 42\n", 20, Invocation.InitialIoError, "") { MissingDos = true },
            new("workbench", "", 0, 0, "") { Workbench = true },
            new("workbench-missing-dos", "", 20, Invocation.InitialIoError, "") { Workbench = true, MissingDos = true },
            new("negative-entry-length", "", 10, 120, "") { EntryLength = -1 },
            new("null-entry-buffer", "", 10, 120, "") { EntryLength = 5, NullArgumentPointer = true }
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        for (var index = 0; index < 4; index++)
        {
            reports.AddRange(Execute([new ProbeCase($"repeat-{index}-failure", "VALUE bad\n", 10, 115, "") { ParserError = 115 }], false));
            reports.AddRange(Execute([new ProbeCase($"repeat-{index}-success", "VALUE 73\n", 73, 0, "VALUE 73\n") { Number = 73 }], false));
        }
        reports.AddRange(Execute([
            new ProbeCase("interleaved-left", "VALUE 17\n", 17, 0, "VALUE 17\n") { Number = 17, StackBytes = 4096 },
            new ProbeCase("interleaved-right", "VALUE 83\n", 83, 0, "VALUE 83\n") { Number = 83 }
        ], true));
        reports.AddRange(Execute([
            new ProbeCase("interleaved-error", "FAIL\n", 10, 205, "") { FailSwitch = true },
            new ProbeCase("interleaved-success", "VALUE 52\n", 52, 0, "VALUE 52\n") { Number = 52 }
        ], true));
        reports.AddRange(Execute([
            new ProbeCase("interleaved-parser-error", "VALUE bad\n", 10, 115, "") { ParserError = 115 },
            new ProbeCase("interleaved-empty-success", "", 0, 0, "")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private List<object> Execute(ProbeCase[] definitions, bool interleaved,
        NativeIoTaskState[]? ioTasks = null)
    {
        if (suite == VersionEntrySuite)
        {
            var libraryList = 0x4000u +
                (uint)ExecLayout.ExecBase.LibraryList;
            var librarySentinel = libraryList +
                (uint)ExecLayout.List.Tail;
            Bus.Long(libraryList + (uint)ExecLayout.List.Head,
                librarySentinel);
            Bus.Long(libraryList + (uint)ExecLayout.List.Tail, 0);
            Bus.Long(libraryList + (uint)ExecLayout.List.TailPred,
                libraryList);
            Bus.Long(librarySentinel +
                (uint)ExecLayout.Node.Successor, 0);
        }
        Require(ioTasks is null || ioTasks.Length == definitions.Length &&
            definitions.All(test => test.NativeIo is not null), "Persistent I/O task inventory does not match its invocations.");
        var invocations = definitions.Select((test, slot) => new Invocation(test, slot)
        {
            NativeIo = test.NativeIo is { } io
                ? new NativeIoInvocation(io, ioTasks?[slot] ?? new NativeIoTaskState(io.InitialError, io.Signals))
                : null
        }).ToArray();
        var cores = new List<IM68kCore>();
        try
        {
            foreach (var invocation in invocations)
            {
                Bus.Current = invocation;
                var test = invocation.Definition;
                Require(invocation.StackBytes >= 256 && invocation.StackBytes <= 0x8000 &&
                    (invocation.StackBytes & 3) == 0, "Stack size is outside the fixture's reserved address range.");
                var bytes = Encoding.Latin1.GetBytes(test.Arguments);
                if (invocation.NativeIo is not null)
                    PrepareNativeIoInvocation(invocation);
                else
                {
                    Bus.Memory.AsSpan((int)invocation.Process, 0x400).Clear();
                    Bus.Long(invocation.Process + (uint)DosLayout.Process.CommandLineInterface, test.Workbench ? 0u : 0x100u);
                    Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, Invocation.InitialIoError);
                    bytes.CopyTo(Bus.Memory.AsSpan((int)invocation.Arguments));
                    Bus.Memory[invocation.Arguments + (uint)bytes.Length] = 0;
                    if (invocation.Definition.Avail is { } avail)
                        Bus.Long(ExecBase + 416, avail.MaxLocalMemory);
                    if (invocation.Definition.QuoteForward is not null)
                        PrepareQuoteForwardProbe(invocation);
                    if (invocation.Definition.TypeText is not null)
                        PrepareTypeTextProbe(invocation);
                    if (invocation.Definition.TypeTextIo is not null)
                        PrepareTypeTextIoProbe(invocation);
                    if (invocation.Definition.TypeEntry is not null)
                        PrepareTypeNativeEntry(invocation);
                    if (invocation.Definition.SearchLiteral is not null)
                        PrepareSearchLiteralProbe(invocation);
                    if (invocation.Definition.SearchLine is not null)
                        PrepareSearchLineProbe(invocation);
                    if (invocation.Definition.Search is not null)
                        PrepareSearchEntry(invocation);
                    if (invocation.Definition.DosListTraversal is not null)
                        PrepareDosListTraversalProbe(invocation);
                    if (invocation.Definition.DosList is not null)
                        PrepareDosListEntry(invocation);
                    if (invocation.Definition.FileNote is not null)
                        PrepareFileNoteEntry(invocation);
                    if (invocation.Definition.Touch is not null)
                        PrepareTouchEntry(invocation);
                    if (invocation.Definition.SetClock is not null)
                        PrepareSetClockEntry(invocation);
                    if (invocation.Definition.SetKeyboard is not null)
                    {
                        if (suite == MorphOSSetKeyboardEntrySuite)
                            PrepareMorphOSSetKeyboardEntry(invocation);
                        else
                            PrepareSetKeyboardEntry(invocation);
                    }
                    if (invocation.Definition.SetFont is not null)
                        PrepareSetFontEntry(invocation);
                    if (invocation.Definition.Version is not null)
                        PrepareVersionEntry(invocation);
                    if (invocation.Definition.FindResident is not null)
                        PrepareFindResidentEntry(invocation);
                    if (invocation.Definition.Check2090 is not null)
                        PrepareCheck2090Entry(invocation);
                    if (invocation.Definition.IconPos is not null)
                        PrepareIconPosEntry(invocation);
                    if (invocation.Definition.GuessBootDev is not null)
                        PrepareGuessBootDevEntry(invocation);
                    if (invocation.Definition.ExtractKickstart is not null)
                        PrepareExtractKickstartEntry(invocation);
                    if (invocation.Definition.TaskList is not null)
                        PrepareTaskListEntry(invocation);
                    if (invocation.Definition.Info is not null)
                        PrepareInfoEntry(invocation);
                    if (invocation.Definition.DiskFree is not null)
                        PrepareDiskFreeEntry(invocation);
                    if (invocation.Definition.Dir is not null)
                        PrepareDirEntry(invocation);
                    if (invocation.Definition.List is not null)
                        PrepareListEntry(invocation);
                    if (invocation.Definition.SetDate is not null)
                        PrepareSetDateEntry(invocation);
                    if (invocation.Definition.MorphOSDate is not null)
                        PrepareMorphOSDateEntry(invocation);
                    if (invocation.Definition.Format is not null)
                        PrepareFormatEntry(invocation);
                    if (invocation.Definition.Mount is not null)
                        PrepareMountEntry(invocation);
                    if (invocation.Definition.LoadMonDrvs is not null)
                        PrepareLoadMonDrvsEntry(invocation);
                    if (invocation.Definition.BindDrivers is not null &&
                        invocation.Definition.BindDrivers.EntryPath !=
                            BindDriversEntryPath.None)
                        PrepareBindDriversEntry(invocation);
                    if (invocation.Definition.Break is not null)
                        PrepareBreakEntry(invocation);
                    if (invocation.Definition.ChangeTaskPri is not null)
                        PrepareChangeTaskPriEntry(invocation);
                    if (invocation.Definition.AddBuffers is not null)
                        PrepareAddBuffersEntry(invocation);
                    if (invocation.Definition.AddDataTypesList is not null)
                        PrepareAddDataTypesListEntry(invocation);
                    if (invocation.Definition.Relabel is not null)
                        PrepareRelabelEntry(invocation);
                    if (invocation.Definition.MakeLink is not null)
                        PrepareMakeLinkEntry(invocation);
                    if (invocation.Definition.ResList is not null)
                        PrepareResListEntry(invocation);
                    if (invocation.Definition.LibList is not null)
                        PrepareLibListEntry(invocation);
                    if (invocation.Definition.DevList is not null)
                        PrepareDevListEntry(invocation);
                    if (invocation.Definition.PortList is not null)
                        PreparePortListEntry(invocation);
                    if (invocation.Definition.ModList is not null)
                        PrepareModListEntry(invocation);
                    if (invocation.Definition.Status is not null)
                        PrepareStatusEntry(invocation);
                    if (invocation.Definition.Protect is not null)
                        PrepareProtectEntry(invocation);
                    if (invocation.Definition.CopyLoop is not null)
                        PrepareCopyLoopProbe(invocation);
                    if (invocation.Definition.DeleteObject is not null)
                        PrepareDeleteObjectProbe(invocation);
                    if (invocation.Definition.DeleteCommand is not null)
                        PrepareDeleteCommand(invocation);
                    if (invocation.Definition.CopyModeSelection is not null)
                        PrepareCopyModeSelectionProbe(invocation);
                    if (invocation.Definition.CopyArgumentGate is not null)
                        PrepareCopyArgumentGateEntry(invocation);
                    if (invocation.Definition.CopyFilePair is not null)
                        PrepareCopyFilePairProbe(invocation);
                    if (invocation.Definition.CopyDestination is not null)
                        PrepareCopyDestinationProbe(invocation);
                    if (invocation.Definition.CopyDestinationDirectories is not null)
                        PrepareCopyDestinationDirectoriesProbe(invocation);
                    if (invocation.Definition.CopyNonFileSystem is not null)
                        PrepareCopyNonFileSystemProbe(invocation);
                    if (invocation.Definition.CopyLoopGuard is not null) PrepareCopyLoopGuardProbe(invocation);
                    if (invocation.Definition.CopyMetadata is not null) PrepareCopyMetadataProbe(invocation);
                    if (invocation.Definition.CopyResultPolicy is not null) PrepareCopyResultPolicyProbe(invocation);
                    if (invocation.Definition.CopyOpenDestination is not null) PrepareCopyOpenDestinationProbe(invocation);
                    if (invocation.Definition.CopyPatternClassifier is not null) PrepareCopyPatternClassifierProbe(invocation);
                    if (invocation.Definition.CopyFlatTraversal is not null) PrepareCopyFlatTraversalProbe(invocation);
                    if (invocation.Definition.CopyDirectoryExit is not null) PrepareCopyDirectoryExitProbe(invocation);
                    if (invocation.Definition.CopyDirectoryEntry is not null) PrepareCopyDirectoryEntryProbe(invocation);
                    if (invocation.Definition.CopyTraversalWork is not null) PrepareCopyTraversalWorkProbe(invocation);
                    if (invocation.Definition.CopyWork is not null) PrepareCopyWorkProbe(invocation);
                    if (invocation.Definition.CopyOutput is not null) PrepareCopyOutputProbe(invocation);
                    if (invocation.Definition.CopyDirectoryOperation is not null) PrepareCopyDirectoryOperationProbe(invocation);
                    if (invocation.Definition.CopyLinkOperation is not null) PrepareCopyLinkOperationProbe(invocation);
                    if (invocation.Definition.CopyFileOperation is not null) PrepareCopyFileOperationProbe(invocation);
                    if (invocation.Definition.CopyFileTransfer is not null) PrepareCopyFileTransferProbe(invocation);
                    if (invocation.Definition.CopyWorkPreparation is not null) PrepareCopyWorkPreparationProbe(invocation);
                    if (invocation.Definition.CopyTraversal is not null) PrepareCopyTraversalProbe(invocation);
                    if (invocation.Definition.CopyMatchStep is not null) PrepareCopyMatchStepProbe(invocation);
                    if (invocation.Definition.CopySoftLink is not null) PrepareCopySoftLinkProbe(invocation);
                }
                Bus.Memory.AsSpan((int)(invocation.StackTop - invocation.StackBytes - 16),
                    (int)invocation.StackBytes + 32).Fill(0xb6);
                if (invocation.Definition.MakeDir is not null)
                    PrepareMakeDirEntry(invocation);
                if (invocation.Definition.Assign is not null)
                    PrepareAssignEntry(invocation);
                if (invocation.Definition.Join is not null)
                    PrepareJoinEntry(invocation);
                var commandAddress = bindDriversPrivateImageProbe
                    ? checked(LoadAddress + (uint)invocation.Slot * 0x2000u)
                    : LoadAddress;
                if (bindDriversPrivateImageProbe)
                {
                    var commandImage = Image.CreateLoadedImage(commandAddress);
                    Bus.LoadImage(commandAddress, commandImage, [],
                        [(0, commandImage.Length)]);
                }
                var cpu = M68kCoreFactory.Default.Create(model, Bus);
                cores.Add(cpu);
                cpu.State.StatusRegister = 0; // Real commands run in user mode.
                cpu.BeginSubroutine(commandAddress, invocation.StackTop, ReturnAddress);
                for (var index = 0; index < 7; index++) cpu.State.A[index] = (uint)(0xae000000 + index * 16);
                for (var index = 0; index < 8; index++) cpu.State.D[index] = (uint)(0xde000000 + index * 16);
                cpu.State.D[0] = unchecked((uint)(test.EntryLength ?? bytes.Length));
                cpu.State.A[0] = test.NullArgumentPointer ? 0 :
                    invocation.QuoteForwardLayout?.Control ??
                    invocation.TypeTextLayout?.Control ??
                    invocation.TypeTextIoLayout?.Control ??
                    invocation.SearchLiteralLayout?.Control ??
                    invocation.SearchLineLayout?.Control ??
                    invocation.SearchEntryLayout?.Control ??
                    invocation.DosListTraversalLayout?.Control ??
                    invocation.DosListLayout?.Control ??
                    invocation.ListLayout?.Control ??
                    invocation.LockLayout?.Control ??
                    invocation.CopyFilePairLayout?.Control ??
                    invocation.CopyDestinationLayout?.Control ??
                    invocation.CopyDestinationDirectoriesLayout?.Control ??
                    invocation.CopyNonFileSystemLayout?.Control ??
                    invocation.CopyLoopGuardLayout?.Control ??
                    invocation.CopyMetadataLayout?.Control ??
                    invocation.CopyResultPolicyLayout?.Control ??
                    invocation.CopyOpenDestinationLayout?.Control ??
                    invocation.CopyPatternClassifierLayout?.Control ??
                    invocation.CopyFlatTraversalLayout?.Control ??
                    invocation.CopyDirectoryExitLayout?.Control ??
                    invocation.CopyDirectoryEntryLayout?.Control ??
                    invocation.CopyTraversalWorkLayout?.Control ??
                    invocation.CopyWorkLayout?.Control ??
                    invocation.CopyOutputLayout?.Control ??
                    invocation.CopyDirectoryOperationLayout?.Control ??
                    invocation.CopyLinkOperationLayout?.Control ??
                    invocation.CopyFileOperationLayout?.Control ??
                    invocation.CopyFileTransferLayout?.Control ??
                    invocation.CopyWorkPreparationLayout?.Control ??
                    invocation.CopyTraversalLayout?.Control ??
                    invocation.CopyMatchStepLayout?.Control ??
                    invocation.CopySoftLinkLayout?.Control ??
                    invocation.CopyModeSelectionLayout?.Control ??
                    invocation.PortListLayout?.Control ??
                    invocation.ModListLayout?.Control ??
                    invocation.StatusLayout?.Control ??
                    invocation.TouchLayout?.Control ??
                    invocation.SetClockLayout?.BattClock ??
                    invocation.VersionLayout?.Names ??
                    invocation.FindResidentLayout?.Control ??
                    invocation.IconPosLayout?.Control ??
                    invocation.GuessBootDevLayout?.Control ??
                    invocation.ExtractKickstartLayout?.Control ??
                    invocation.TaskListLayout?.Control ??
                    invocation.InfoLayout?.Control ??
                    invocation.DiskFreeLayout?.Control ??
                    invocation.DirLayout?.Control ??
                    invocation.MakeDirLayout?.Control ??
                    invocation.AssignLayout?.Control ??
                    invocation.JoinLayout?.Control ??
                    invocation.ProtectLayout?.Control ?? invocation.Arguments;
            }
            while (cores.Any(cpu => cpu.State.ProgramCounter != ReturnAddress))
            {
                for (var index = 0; index < cores.Count; index++)
                {
                    var cpu = cores[index];
                    if (cpu.State.ProgramCounter == ReturnAddress) continue;
                    var invocation = invocations[index];
                    Bus.Current = invocation;
                    Require(!cpu.State.Halted && !cpu.State.Stopped, $"{invocation.Definition.Name}: CPU halted/stopped.");
                    Require(++invocation.Instructions <=
                        (IsSearchEntrySuite(suite) ? 50_000_000 :
                         IsVersionEntrySuite(suite) ? 20_000_000 :
                         recursiveCommandRoot || IsDirEntrySuite(suite) ||
                            suite == TaskListEntrySuite
                            ? 1_000_000 : 200_000),
                        $"{invocation.Definition.Name}: native instruction limit exceeded.");
                    try { cpu.ExecuteInstruction(); }
                    catch (Exception error)
                    {
                        throw new InvalidOperationException($"{invocation.Definition.Name}, PC=${cpu.State.ProgramCounter:X8}: {error.Message}", error);
                    }
                }
            }
            var reports = new List<object>();
            for (var index = 0; index < cores.Count; index++)
            {
                var cpu = cores[index];
                var invocation = invocations[index];
                var test = invocation.Definition;
                if (bindDriversPrivateImageProbe)
                {
                    reports.Add(new
                    {
                        name = test.Name,
                        result = unchecked((int)cpu.State.D[0]),
                        ioErr = invocation.IoError,
                        stdoutHex = Convert.ToHexStringLower(invocation.Output.ToArray()),
                        instructions = invocation.Instructions,
                        events = invocation.Events
                    });
                    continue;
                }
                var actualResult = unchecked((int)cpu.State.D[0]);
                Require(actualResult == test.Result,
                    $"{test.Name}: result {actualResult}, expected {test.Result}; " +
                    $"IoErr {invocation.IoError}; stdout " +
                    $"{Convert.ToHexStringLower(invocation.Output.ToArray())}; " +
                    $"instructions {invocation.Instructions}; PC=${cpu.State.ProgramCounter:X8}; " +
                    $"SP=${cpu.State.A[7]:X8}; D1=${cpu.State.D[1]:X8}; " +
                    $"A0=${cpu.State.A[0]:X8}; A1=${cpu.State.A[1]:X8}; " +
                    $"A2=${cpu.State.A[2]:X8}; A6=${cpu.State.A[6]:X8}; events " +
                    $"{string.Join(',', invocation.Events)}.");
                var observedIoError = (suite == WorkbenchMakeLinkEntrySuite || suite == WorkbenchRenameStartupSuite ||
                    IsWorkbench31MakeDirEntrySuite(suite) || IsAssignEntrySuite(suite) ||
                    IsJoinEntrySuite(suite) || suite == RequestChoiceEntrySuite ||
                    suite == Workbench31RequestChoiceEntrySuite ||
                     (suite == RequestFileEntrySuite ||
                      suite == Workbench31RequestFileEntrySuite)) && test.MissingDos
                    ? unchecked((int)Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2))
                    : invocation.IoError;
                Require(observedIoError == test.Error, $"{test.Name}: IoErr {observedIoError}, expected {test.Error}.");
                var actualOutputBytes = invocation.Output.ToArray();
                var expectedOutputBytes = Encoding.Latin1.GetBytes(test.Output);
                var outputMatches = suite == TaskListEntrySuite
                    ? TaskListOutputMatchesWithLiveStackPointer(test.Output,
                        Encoding.Latin1.GetString(actualOutputBytes), invocation)
                    : expectedOutputBytes.AsSpan().SequenceEqual(actualOutputBytes);
                Require(outputMatches,
                    $"{test.Name}: wrong output bytes; expected {Convert.ToHexStringLower(expectedOutputBytes)}, actual {Convert.ToHexStringLower(actualOutputBytes)}.");
                Require(cpu.State.A[7] == invocation.StackTop, $"{test.Name}: command did not restore SP.");
                Require(Bus.Memory.AsSpan((int)invocation.StackTop, 16).IndexOfAnyExcept((byte)0xb6) < 0, "Stack upper guard changed.");
                Require(Bus.Memory.AsSpan((int)(invocation.StackTop - invocation.StackBytes - 16), 16)
                    .IndexOfAnyExcept((byte)0xb6) < 0, "Stack lower guard changed.");
                if (!addDataTypesCallbackProbe && suite != QuoteForwardProbeSuite && suite != TypeTextProbeSuite &&
                    suite != TypeTextIoProbeSuite && suite != SearchLiteralProbeSuite &&
                    suite != SearchLineProbeSuite && !IsSearchEntrySuite(suite) && suite != DosListTraversalProbeSuite &&
                    suite != CopyModeSelectionProbeSuite && suite != BeepEntrySuite &&
                    !IsBindDriversEntrySuite(suite))
                {
                    Require(invocation.Opens == (copyCommandRoot&&test.Workbench?0:1) && invocation.Closes == (test.MissingDos||copyCommandRoot&&test.Workbench ? 0 : 1), "Unbalanced DOS library lifetime.");
                    Require(invocation.Replies == (test.Workbench ? 1 : 0), "Unbalanced WBStartup message lifetime.");
                    Require(invocation.WaitPorts == (test.Workbench ? 1 : 0) &&
                        invocation.GetMessages == (test.Workbench ? 1 : 0), "Missing or repeated WBStartup queue operations.");
                }
                if (suite == NativeIoSuite)
                    VerifyNativeIo(invocation);
                else if (suite == ArgumentBoundarySuite)
                    VerifyArgumentBoundary(invocation);
                else if (suite == EvalEntrySuite || suite == WorkbenchEvalEntrySuite)
                    VerifyEvalEntry(invocation);
                else if (suite == PathPartEntrySuite)
                    VerifyPathPartEntry(invocation);
                else if (IsWhichEntrySuite(suite))
                    VerifyWorkbenchWhichEntry(invocation);
                else if (suite == QuoteForwardProbeSuite)
                    VerifyQuoteForwardProbe(invocation);
                else if (suite == QuoteNativeEntrySuite)
                    VerifyQuoteNativeEntry(invocation);
                else if (suite == TypeTextProbeSuite)
                    VerifyTypeTextProbe(invocation);
                else if (suite == TypeTextIoProbeSuite)
                    VerifyTypeTextIoProbe(invocation);
                else if (IsTypeNativeEntrySuite(suite))
                    VerifyTypeNativeEntry(invocation);
                else if (suite == SearchLiteralProbeSuite)
                    VerifySearchLiteralProbe(invocation);
                else if (suite == SearchLineProbeSuite)
                    VerifySearchLineProbe(invocation);
                else if (IsSearchEntrySuite(suite))
                    VerifySearchEntry(invocation);
                else if (suite == DosListTraversalProbeSuite)
                    VerifyDosListTraversalProbe(invocation);
                else if (suite == DosListEntrySuite)
                    VerifyDosListEntry(invocation);
                else if (suite is ListEntrySuite or WorkbenchListEntrySuite)
                    VerifyListEntry(invocation);
                else if (IsFileNoteEntrySuite(suite))
                    VerifyFileNoteEntry(invocation);
                else if (suite == TouchEntrySuite)
                    VerifyTouchEntry(invocation);
                else if (suite == SetClockEntrySuite ||
                    suite == Workbench31SetClockEntrySuite)
                    VerifySetClockEntry(invocation);
                else if (suite == Workbench31SetKeyboardEntrySuite)
                    VerifySetKeyboardEntry(invocation);
                else if (suite == MorphOSSetKeyboardEntrySuite)
                    VerifyMorphOSSetKeyboardEntry(invocation);
                else if (suite == Workbench31SetFontEntrySuite)
                    VerifySetFontEntry(invocation);
                else if (IsVersionEntrySuite(suite))
                    VerifyVersionEntry(invocation);
                else if (suite == FindResidentEntrySuite)
                    VerifyFindResidentEntry(invocation);
                else if (suite == Check2090EntrySuite)
                    VerifyCheck2090Entry(invocation);
                else if (suite == IconPosEntrySuite)
                    VerifyIconPosEntry(invocation);
                else if (suite == GuessBootDevEntrySuite)
                    VerifyGuessBootDevEntry(invocation);
                else if (suite == ExtractKickstartEntrySuite)
                    VerifyExtractKickstartEntry(invocation);
                else if (suite == TaskListEntrySuite)
                    VerifyTaskListEntry(invocation);
                else if (suite == InfoEntrySuite)
                    VerifyInfoEntry(invocation);
                else if (suite == Workbench31InfoEntrySuite)
                    VerifyWorkbench31InfoEntry(invocation);
                else if (suite == DiskFreeEntrySuite)
                    VerifyDiskFreeEntry(invocation);
                else if (IsDirEntrySuite(suite))
                    VerifyDirEntry(invocation);
                else if (IsMakeDirEntrySuite(suite))
                    VerifyMakeDirEntry(invocation);
                else if (IsAssignEntrySuite(suite))
                    VerifyAssignEntry(invocation);
                else if (IsJoinEntrySuite(suite))
                    VerifyJoinEntry(invocation);
                else if (suite == BreakEntrySuite)
                    VerifyBreakEntry(invocation);
                else if (suite == Workbench31BreakEntrySuite)
                    VerifyWorkbench31BreakEntry(invocation);
                else if (suite == ChangeTaskPriEntrySuite)
                    VerifyChangeTaskPriEntry(invocation);
                else if (suite == Workbench31ChangeTaskPriEntrySuite)
                    VerifyWorkbench31ChangeTaskPriEntry(invocation);
                else if (suite == AddBuffersEntrySuite)
                    VerifyAddBuffersEntry(invocation);
                else if (addDataTypesCallbackProbe)
                    VerifyAddDataTypesCallbackProbe(invocation);
                else if (suite == AddDataTypesListEntrySuite)
                    VerifyAddDataTypesListEntry(invocation);
                else if (suite == Workbench31AvailEntrySuite)
                    VerifyWorkbench31AvailEntry(invocation);
                else if (suite == MorphOSAvailEntrySuite)
                    VerifyMorphOSAvailEntry(invocation);
                else if (suite == FormatEntrySuite)
                    VerifyFormatEntry(invocation);
                else if (suite == MountEntrySuite || suite == Workbench31MountEntrySuite)
                    VerifyMountEntry(invocation);
                else if (suite == BindDriversEntrySuite)
                    VerifyBindDriversEntry(invocation);
                else if (suite == BindDriversProductParserEntrySuite)
                    VerifyBindDriversProductParserEntry(invocation);
                else if (suite == LoadMonDrvsEntrySuite)
                    VerifyLoadMonDrvsEntry(invocation);
                else if (suite == Workbench31DateEntrySuite)
                    VerifyWorkbench31DateEntry(invocation);
                else if (suite == MorphOSDateEntrySuite)
                    VerifyMorphOSDateEntry(invocation);
                else if (suite == Workbench31SetDateEntrySuite)
                    VerifyWorkbench31SetDateEntry(invocation);
                else if (suite == MorphOSSetDateEntrySuite)
                    VerifyMorphOSSetDateEntry(invocation);
                else if (suite == Workbench31WaitEntrySuite)
                    VerifyWorkbench31WaitEntry(invocation);
                else if (suite == MorphOSWaitEntrySuite)
                    VerifyMorphOSWaitEntry(invocation);
                else if (suite == WaitForPortEntrySuite)
                    VerifyWaitForPortEntry(invocation);
                else if (suite == WaitForLibEntrySuite)
                    VerifyWaitForLibEntry(invocation);
                else if (suite == WaitForNotificationEntrySuite)
                    VerifyWaitForNotificationEntry(invocation);
                else if (suite == BeepEntrySuite)
                    VerifyBeepEntry(invocation);
                else if (suite == RequestChoiceEntrySuite ||
                    suite == Workbench31RequestChoiceEntrySuite)
                    VerifyRequestChoiceEntry(invocation);
                 else if (suite == RequestFileEntrySuite ||
                     suite == Workbench31RequestFileEntrySuite)
                    VerifyRequestFileEntry(invocation);
                else if (suite == LockEntrySuite || suite == Workbench31LockEntrySuite)
                    VerifyLockEntry(invocation);
                else if (suite == DiskChangeEntrySuite || suite == Workbench31DiskChangeEntrySuite)
                    VerifyDiskChangeEntry(invocation);
                else if (suite == RebootEntrySuite || suite == Workbench31RebootEntrySuite)
                    VerifyRebootEntry(invocation);
                else if (suite == ResListEntrySuite)
                    VerifyResListEntry(invocation);
                else if (suite == LibListEntrySuite)
                    VerifyLibListEntry(invocation);
                else if (suite == DevListEntrySuite)
                    VerifyDevListEntry(invocation);
                else if (suite == PortListEntrySuite)
                    VerifyPortListEntry(invocation);
                else if (suite == ModListEntrySuite)
                    VerifyModListEntry(invocation);
                else if (IsStatusEntrySuite(suite))
                    VerifyStatusEntry(invocation);
                else if (IsProtectEntrySuite(suite))
                    VerifyProtectEntry(invocation);
                else if (suite == RelabelEntrySuite)
                    VerifyRelabelEntry(invocation);
                else if (suite == MakeLinkEntrySuite)
                    VerifyMakeLinkEntry(invocation);
                else if (suite == WorkbenchRenameStartupSuite)
                    VerifyWorkbenchRenameStartup(invocation);
                else if (suite == MorphOSRenameSuite)
                    VerifyMorphOSRename(invocation);
                else if (suite == WorkbenchMakeLinkEntrySuite)
                    VerifyWorkbenchMakeLink(invocation);
                else if (IsTransferLoopProbeSuite)
                    VerifyCopyLoopProbe(invocation);
                else if (suite == DeleteObjectProbeSuite)
                    VerifyDeleteObjectProbe(invocation);
                else if (IsDeleteCommandEntrySuite(suite))
                    VerifyDeleteCommand(invocation);
                else if (suite == CopyModeSelectionProbeSuite)
                    VerifyCopyModeSelectionProbe(invocation);
                else if (suite is CopyArgumentGateEntrySuite or CopyOptionSetupSuite or CopyDirectSuite or CopyParsedDeleteSuite or CopyParsedMakeDirectorySuite)
                    VerifyCopyArgumentGateEntry(invocation);
                else if (suite == CopyFilePairProbeSuite)
                    VerifyCopyFilePairProbe(invocation);
                else if (suite == CopyDestinationProbeSuite)
                    VerifyCopyDestinationProbe(invocation);
                else if (suite == CopyDestinationDirectoriesProbeSuite)
                    VerifyCopyDestinationDirectoriesProbe(invocation);
                else if (suite == CopyNonFileSystemProbeSuite)
                    VerifyCopyNonFileSystemProbe(invocation);
                else if (suite == CopyLoopGuardProbeSuite) VerifyCopyLoopGuardProbe(invocation);
                else if (suite == CopyMetadataProbeSuite) VerifyCopyMetadataProbe(invocation);
                else if (suite is CopyResultPolicyProbeSuite or CopyCompletionSuite) VerifyCopyResultPolicyProbe(invocation);
                else if (suite == CopyOpenDestinationProbeSuite) VerifyCopyOpenDestinationProbe(invocation);
                else if (suite == CopyPatternClassifierProbeSuite) VerifyCopyPatternClassifierProbe(invocation);
                else if (suite == CopyFlatTraversalProbeSuite) VerifyCopyFlatTraversalProbe(invocation);
                else if (suite == CopyDirectoryExitProbeSuite) VerifyCopyDirectoryExitProbe(invocation);
                else if (suite == CopyDirectoryEntryProbeSuite) VerifyCopyDirectoryEntryProbe(invocation);
                else if (suite is CopyTraversalWorkProbeSuite or CopySourceRoutingProbeSuite or CopyDirectorySourcesSuite or CopyTargetDispatchSuite or CopySingleTargetSuite) VerifyCopyTraversalWorkProbe(invocation);
                else if (suite == CopyWorkProbeSuite) VerifyCopyWorkProbe(invocation);
                else if (suite == CopyOutputProbeSuite) VerifyCopyOutputProbe(invocation);
                else if (suite == CopyDirectoryOperationProbeSuite) VerifyCopyDirectoryOperationProbe(invocation);
                else if (suite == CopyLinkOperationProbeSuite) VerifyCopyLinkOperationProbe(invocation);
                else if (suite == CopyFileOperationProbeSuite) VerifyCopyFileOperationProbe(invocation);
                else if (suite == CopyFileTransferProbeSuite) VerifyCopyFileTransferProbe(invocation);
                else if (suite == CopyWorkPreparationProbeSuite) VerifyCopyWorkPreparationProbe(invocation);
                else if (suite == CopyTraversalProbeSuite) VerifyCopyTraversalProbe(invocation);
                else if (suite == CopyMatchStepProbeSuite) VerifyCopyMatchStepProbe(invocation);
                else if (suite is CopySoftLinkProbeSuite or CopyMatchedDirectoryProbeSuite) VerifyCopySoftLinkProbe(invocation);
                else
                {
                    var reachedParser = !test.Workbench && !test.MissingDos && !test.AllocationFailure &&
                        (test.EntryLength is null || test.EntryLength == 0 ||
                            (test.EntryLength > 0 && !test.NullArgumentPointer));
                    Require(invocation.Reads == (reachedParser ? 1 : 0), "ReadArgs invocation count differs from the expected path.");
                    Require(invocation.FreeArgs == (reachedParser && test.ParserError == 0 ? 1 : 0),
                        "RDArgs must be freed once on success, and never after parser failure.");
                }
                if (test.Workbench)
                {
                    var waitIndex = invocation.Events.IndexOf("WaitPort");
                    var getIndex = invocation.Events.IndexOf("GetMsg");
                    var openIndex = invocation.Events.IndexOf("OpenLibrary");
                    Require(waitIndex >= 0 && getIndex > waitIndex && (copyCommandRoot?openIndex==-1:openIndex > getIndex),
                        "Wrong Workbench startup ordering.");
                    Require(copyCommandRoot?invocation.Events.TakeLast(3).SequenceEqual(new[]{"Forbid","GetMsg","ReplyMsg"}):invocation.Events.TakeLast(2).SequenceEqual(new[] { "Forbid", "ReplyMsg" }), "Workbench reply was not last under Forbid.");
                }
                if (suite == MorphOSSetKeyboardEntrySuite)
                    ReclaimMorphOSSetKeyboardSegment(invocation);
                if (suite == Workbench31SetFontEntrySuite)
                    ReclaimSetFontConsoleStorage(invocation);
                Bus.AssertReleased(invocation);
                Bus.AssertImageUnchanged();
                reports.Add(new
                {
                    name = test.Name, instructionInterleaved = interleaved,
                    result = unchecked((int)cpu.State.D[0]), ioErr = observedIoError,
                    stdoutHex = Convert.ToHexStringLower(invocation.Output.ToArray()),
                    instructions = invocation.Instructions,
                    configuredStackBytes = invocation.StackBytes,
                    stackBytesWritten = invocation.StackTop - invocation.LowestStackWrite,
                    resultArrayAllocationRequests = invocation.AllocationRequests,
                    readArgsCalls = invocation.Reads,
                    freeArgsCalls = invocation.FreeArgs,
                    freeMemCalls = invocation.FreeMem,
                    nativeIo = invocation.NativeIo?.Report,
                    events = invocation.Events
                });
            }
            return reports;
        }
        finally
        {
            foreach (var cpu in cores) cpu.Dispose();
            foreach (var invocation in invocations) invocation.Output.Dispose();
            Bus.Current = null;
        }
    }

    private void Register(uint baseAddress, short offset, string name,
        Func<M68kCpuState, Invocation, uint> handler,
        bool preserveD1 = false)
    {
        Bus.RegisterGateway(checked((uint)(baseAddress + offset)), state =>
        {
            var invocation = Bus.Current ?? throw new InvalidOperationException("Library vector without a process.");
            var workbenchDosAlias = bindDriversPrivateImageProbe &&
                baseAddress is 0x8000u or 0x9000u;
            if (!bindDriversPrivateImageProbe || baseAddress == ExecBase)
                Require(state.A[6] == baseAddress,
                    $"{name}: incorrect A6 library base (expected ${baseAddress:X8}, got ${state.A[6]:X8}, " +
                    $"vector PC=${state.ProgramCounter:X8}, return=${Bus.Long(state.A[7]):X8}).");
            if (baseAddress != ExecBase && suite != DosListTraversalProbeSuite &&
                !(suite == Workbench31DateEntrySuite && baseAddress == DateUtilityBase) &&
                !(suite == ExtractKickstartEntrySuite && baseAddress == ExtractKickstartUtilityBase) &&
                !(suite == IconPosEntrySuite && baseAddress == Invocation.IconPosBase) &&
                !(suite == GuessBootDevEntrySuite && baseAddress == invocation.GuessBootDevExpansionBase) &&
                !(suite == MorphOSDateEntrySuite && baseAddress == DateLocaleBase) &&
                !(suite == SearchEntrySuite && baseAddress == SearchLocaleBase) &&
                !(suite == InfoEntrySuite &&
                  (baseAddress == InfoLocaleBase ||
                   baseAddress == InfoUtilityBase)) &&
                !(suite == AddDataTypesListEntrySuite &&
                  (baseAddress == AddDataTypesUtilityBase ||
                   baseAddress == AddDataTypesIffParseBase ||
                   baseAddress == AddDataTypesLocaleBase ||
                   baseAddress == AddDataTypesWorkbenchIffParseBase ||
                   baseAddress == AddDataTypesWorkbenchLocaleBase ||
                   baseAddress == AddDataTypesWorkbenchDosBase ||
                   baseAddress == AddDataTypesDatatypesBase)) &&
                !(suite == Check2090EntrySuite && baseAddress == Check2090ExpansionBase) &&
                !(IsBindDriversEntrySuite(suite) && baseAddress == invocation.ExpansionBase) &&
                !(suite == BindDriversEntrySuite && baseAddress == Invocation.IconPosBase) &&
                !((suite == Workbench31SetKeyboardEntrySuite ||
                   suite == MorphOSSetKeyboardEntrySuite) &&
                    (baseAddress == SetKeyboardUtilityBase ||
                     baseAddress == SetKeyboardKeymapBase)) &&
                !workbenchDosAlias &&
                !(suite == Workbench31SetFontEntrySuite &&
                  (baseAddress == SetFontGraphicsBase ||
                   baseAddress == SetFontDiskfontBase ||
                   baseAddress == SetFontUtilityBase)) &&
                !((suite == BeepEntrySuite || suite == RequestChoiceEntrySuite ||
                    suite == Workbench31RequestChoiceEntrySuite) && baseAddress == IntuitionBase) &&
                !((suite == RequestFileEntrySuite ||
                    suite == Workbench31RequestFileEntrySuite) &&
                    baseAddress == AslBase) &&
                !(IsVersionEntrySuite(suite) &&
                  (baseAddress == VersionUtilityBase ||
                   !IsWorkbench31VersionEntrySuite(suite) &&
                   baseAddress == VersionRexxLibraryBase)))
            {
                Require(baseAddress == invocation.DosBase, "DOS base leaked between resident invocations.");
                Require(invocation.Opens == 1 && invocation.Closes == 0 && !invocation.Definition.MissingDos,
                    $"{name}: DOS vector used without a live successfully opened library lease.");
            }
            if (invocation.NativeIo is not null)
                RequireNativeIoGateway(invocation, name);
            // Classic filesystem packets encode a FileInfoBlock pointer as a
            // BPTR. A word-aligned but not longword-aligned fixture pointer
            // must not succeed here when original DOS would truncate it.
            if (name is "Examine" or "ExNext" or "ExamineFH")
                Require(state.D[2] != 0 && (state.D[2] & 3) == 0,
                    $"{name}: FileInfoBlock must be non-null and longword aligned for classic DOS BPTR transport.");
            invocation.Events.Add(name);
            var value = handler(state, invocation);
            // Genuine ABI volatile registers; callers must keep live state elsewhere.
            state.D[0] = value;
            if (!preserveD1) state.D[1] = 0xd1d1d1d1;
            state.A[0] = 0xa0a0a0a0;
            state.A[1] = 0xa1a1a1a1;
        });
    }

    private void RegisterExec()
    {
        if (suite == WorkbenchRenameStartupSuite) RegisterRenameAllocationExec();
        if (suite == MorphOSRenameSuite) RegisterMorphOSRenameExec();
        Register(ExecBase, ExecLvo.FindTask, "FindTask", (state, invocation) =>
        {
            Require(state.A[1] == 0, "FindTask must request the current process.");
            return invocation.Process;
        });
        Register(ExecBase, ExecLvo.FindResident, "FindResident", (state, invocation) =>
        {
            if (suite == FindResidentEntrySuite)
            {
                var findDefinition = invocation.Definition.FindResident!;
                invocation.FindResidentFindResidentCalls++;
                Require(state.A[1] != 0 &&
                    Bus.CString(state.A[1]) == findDefinition.Module,
                    "FindResident module name differs.");
                return findDefinition.ResidentFound ? invocation.VersionModule : 0u;
            }
            Require(IsVersionEntrySuite(suite) && state.A[1] != 0,
                "FindResident was used outside the Version RES path.");
            var definition = invocation.Definition.Version!;
            invocation.VersionFindResidentCalls++;
            var residentLookupName = Bus.CString(state.A[1]);
            var requestedResidentName = definition.System
                ? "MorphOS" : definition.Name;
            if (!definition.System)
            {
                var lastSeparator = Math.Max(
                    definition.Name.LastIndexOf(':'),
                    Math.Max(definition.Name.LastIndexOf('/'),
                        definition.Name.LastIndexOf('\\')));
                requestedResidentName = definition.Name[(lastSeparator + 1)..];
            }
            Require(residentLookupName ==
                    requestedResidentName ||
                residentLookupName == "shellcmd" &&
                    definition.ShellCommandResidentFound,
                "Version resident lookup name differs.");
            var residentFound = residentLookupName == "shellcmd"
                ? definition.ShellCommandResidentFound
                : definition.ResidentFound;
            return residentFound ? invocation.VersionModule : 0u;
        });
        Register(ExecBase, ExecLvo.OpenLibrary, "OpenLibrary", (state, invocation) =>
        {
            if (suite == AddDataTypesListEntrySuite &&
                workbench31AddDataTypes)
                return OpenWorkbench31AddDataTypesLibrary(state,
                    invocation);
            if (suite == AddDataTypesListEntrySuite &&
                Bus.CString(state.A[1]) == Utility.Name)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[0] == 37 && definition.UtilityOpenCalls == 0,
                    "AddDataTypes utility.library capability floor differs.");
                definition.UtilityOpenCalls++;
                definition.Events.Add("open-utility");
                return definition.UtilityAvailable
                    ? AddDataTypesUtilityBase : 0u;
            }
            if (suite == AddDataTypesListEntrySuite &&
                Bus.CString(state.A[1]) == IffParse.Name)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[0] == 37 &&
                    definition.UtilityOpenCalls == 1 &&
                    definition.IffParseOpenCalls == 0,
                    "AddDataTypes iffparse.library floor or order differs.");
                definition.IffParseOpenCalls++;
                definition.Events.Add("open-iffparse");
                return definition.IffParseAvailable
                    ? AddDataTypesIffParseBase : 0u;
            }
            if (suite == AddDataTypesListEntrySuite &&
                Bus.CString(state.A[1]) == Locale.Name)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[0] == 37 &&
                    definition.IffParseOpenCalls == 1 &&
                    definition.LocaleOpenCalls == 0,
                    "AddDataTypes locale.library floor or order differs.");
                definition.LocaleOpenCalls++;
                definition.Events.Add("open-locale");
                return definition.LocaleAvailable
                    ? AddDataTypesLocaleBase : 0u;
            }
            if (suite == AddDataTypesListEntrySuite &&
                Bus.CString(state.A[1]) == Datatypes.Name)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(state.D[0] == 44 &&
                    definition.LocaleOpenCalls == 1 &&
                    definition.DataTypesOpenCalls == 0,
                    "AddDataTypes datatypes.library floor or order differs.");
                definition.DataTypesOpenCalls++;
                definition.Events.Add("open-datatypes");
                return definition.DataTypesAvailable
                    ? AddDataTypesDatatypesBase : 0u;
            }
            if (suite == AddDataTypesListEntrySuite &&
                Bus.CString(state.A[1]) == DOS.Name)
            {
                Require(state.D[0] == 37,
                    "AddDataTypes dos.library capability floor differs.");
                invocation.Opens++;
                invocation.Definition.AddDataTypesList!.Events.Add("open-dos");
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (IsVersionEntrySuite(suite) &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                Bus.CString(state.A[1]) == RexxSysLib.Name)
            {
                var definition = invocation.Definition.Version!;
                var layout = invocation.VersionLayout!;
                Require(state.D[0] == 36 &&
                    layout.AmbientLibraryOpenCalls == 0,
                    "Version Ambient rexxsyslib.library request differs.");
                layout.AmbientLibraryOpenCalls++;
                return definition.RexxLibraryAvailable
                    ? layout.RexxLibraryBase : 0u;
            }
            if (IsVersionEntrySuite(suite) &&
                Bus.CString(state.A[1]) == Utility.Name)
            {
                Require(state.D[0] == 37 &&
                    invocation.VersionUtilityOpenCalls == 0,
                    "Version utility.library capability floor differs.");
                invocation.VersionUtilityOpenCalls++;
                return invocation.Definition.Version!.UtilityLibraryAvailable
                    ? VersionUtilityBase : 0u;
            }
            if (IsVersionEntrySuite(suite) &&
                (!IsWorkbench31VersionEntrySuite(suite) ||
                    invocation.Definition.Version!.System) &&
                Bus.CString(state.A[1]) == "version.library")
            {
                var definition = invocation.Definition.Version!;
                var layout = invocation.VersionLayout!;
                Require(state.D[0] == 0 &&
                    invocation.VersionLibraryOpenCalls == 0,
                    "Version system path changed its version.library request.");
                invocation.VersionLibraryOpenCalls++;
                return definition.VersionLibraryAvailable
                    ? layout.VersionLibraryBase : 0u;
            }
            if (suite == TaskListEntrySuite &&
                Bus.CString(state.A[1]) == "sysdebug.library")
            {
                Require(state.D[0] == 0 &&
                    invocation.TaskListSysDebugOpenCalls == 0,
                    "TaskList sysdebug.library version or ownership differs.");
                invocation.TaskListSysDebugOpenCalls++;
                invocation.Events.Add("TaskList.OpenSysDebug");
                return invocation.Definition.TaskList!.SysDebugUnavailable
                    ? 0u : TaskListSysDebugBase;
            }
            if (suite == InfoEntrySuite &&
                Bus.CString(state.A[1]) == Utility.Name)
            {
                Require(state.D[0] == 37 &&
                    invocation.InfoUtilityLibraryOpens == 0,
                    "Info utility.library capability floor differs.");
                invocation.InfoUtilityLibraryOpens++;
                return invocation.Definition.Info!.UtilityLibraryOpenFailure
                    ? 0u : InfoUtilityBase;
            }
            if (suite == InfoEntrySuite &&
                Bus.CString(state.A[1]) == Locale.Name)
            {
                Require(state.D[0] == 38 &&
                    invocation.InfoUtilityLibraryOpens == 1 &&
                    invocation.InfoLocaleLibraryOpens == 0,
                    "Info locale.library capability floor or order differs.");
                invocation.InfoLocaleLibraryOpens++;
                return invocation.Definition.Info!.LocaleLibraryOpenFailure
                    ? 0u : InfoLocaleBase;
            }
            if ((suite == Workbench31SetKeyboardEntrySuite ||
                 suite == MorphOSSetKeyboardEntrySuite) &&
                Bus.CString(state.A[1]) == Utility.Name)
            {
                Require(state.D[0] == 36 && invocation.SetKeyboardUtilityOpens == 0,
                    "SetKeyboard utility.library capability floor differs.");
                invocation.SetKeyboardUtilityOpens++;
                return invocation.Definition.SetKeyboard!.UtilityAvailable
                    ? SetKeyboardUtilityBase : 0u;
            }
            if ((suite == Workbench31SetKeyboardEntrySuite ||
                 suite == MorphOSSetKeyboardEntrySuite) &&
                Bus.CString(state.A[1]) == Keymap.Name)
            {
                Require(state.D[0] == 36 && invocation.SetKeyboardKeymapOpens == 0,
                    "SetKeyboard keymap.library capability floor differs.");
                invocation.SetKeyboardKeymapOpens++;
                return invocation.Definition.SetKeyboard!.KeymapAvailable
                    ? SetKeyboardKeymapBase : 0u;
            }
            if (suite == Workbench31SetFontEntrySuite &&
                Bus.CString(state.A[1]) == Graphics.Name)
            {
                Require(state.D[0] == 37 && invocation.SetFontGraphicsOpens == 0,
                    "SetFont graphics.library capability floor differs.");
                invocation.SetFontGraphicsOpens++;
                return invocation.Definition.SetFont!.GraphicsAvailable
                    ? SetFontGraphicsBase : 0u;
            }
            if (suite == Workbench31SetFontEntrySuite &&
                Bus.CString(state.A[1]) == Diskfont.Name)
            {
                Require(state.D[0] == 37 && invocation.SetFontDiskfontOpens == 0,
                    "SetFont diskfont.library capability floor differs.");
                invocation.SetFontDiskfontOpens++;
                return invocation.Definition.SetFont!.DiskfontAvailable
                    ? SetFontDiskfontBase : 0u;
            }
            if (suite == Workbench31SetFontEntrySuite &&
                Bus.CString(state.A[1]) == Utility.Name)
            {
                Require(state.D[0] == 37 && invocation.SetFontUtilityOpens == 0,
                    "SetFont utility.library capability floor differs.");
                invocation.SetFontUtilityOpens++;
                return invocation.Definition.SetFont!.UtilityAvailable
                    ? SetFontUtilityBase : 0u;
            }
            if ((suite == MountEntrySuite || suite == Workbench31MountEntrySuite) &&
                Bus.CString(state.A[1]) == Expansion.Name)
            {
                Require(state.D[0] == 33 && invocation.MountExpansionOpens == 0,
                    "Mount expansion.library capability floor differs.");
                invocation.MountExpansionOpens++;
                return invocation.ExpansionBase;
            }
            if (suite == BindDriversProductParserEntrySuite &&
                Bus.CString(state.A[1]) == Expansion.Name)
            {
                Require(state.D[0] == 37 &&
                    invocation.BindDriversExpansionOpens == 0,
                    "BindDrivers PRODUCT parser expansion.library request differs.");
                invocation.BindDriversExpansionOpens++;
                return invocation.ExpansionBase;
            }
            if (suite == BindDriversEntrySuite &&
                Bus.CString(state.A[1]) == Icon.Name)
            {
                if (workbench31BindDrivers)
                    Require(state.D[0] == 37 && invocation.Opens == 1 &&
                        invocation.BindDriversExpansionOpens == 1 &&
                        invocation.BindDriversIconOpens == 0,
                        "Workbench BindDrivers library open order or Icon floor differs.");
                Require(state.D[0] == 37 && invocation.BindDriversIconOpens == 0,
                    "BindDrivers icon.library capability floor differs.");
                invocation.BindDriversIconOpens++;
                return invocation.Definition.BindDrivers!.IconAvailable
                    ? Invocation.IconPosBase : 0u;
            }
            if (suite == BindDriversEntrySuite &&
                Bus.CString(state.A[1]) == Expansion.Name)
            {
                if (workbench31BindDrivers)
                    Require(state.D[0] == 37 && invocation.Opens == 1 &&
                        invocation.BindDriversExpansionOpens == 0,
                        "Workbench BindDrivers library open order or Expansion floor differs.");
                Require(state.D[0] == 37 && invocation.BindDriversExpansionOpens == 0,
                    "BindDrivers expansion.library capability floor differs.");
                invocation.BindDriversExpansionOpens++;
                return invocation.Definition.BindDrivers!.ExpansionAvailable
                    ? invocation.ExpansionBase : 0u;
            }
            if (suite == BindDriversEntrySuite &&
                Bus.CString(state.A[1]) == DOS.Name)
            {
                if (workbench31BindDrivers)
                {
                    Require(state.D[0] == 37 && invocation.Opens == 0 &&
                        invocation.BindDriversExpansionOpens == 0 &&
                        invocation.BindDriversIconOpens == 0,
                        "Workbench BindDrivers DOS library must open first at version 37.");
                    invocation.Opens++;
                    return invocation.Definition.BindDrivers!.MissingDos
                        ? 0u : invocation.DosBase;
                }
                Require(state.D[0] == 37 && invocation.Opens == 0,
                    "BindDrivers dos.library capability floor differs.");
                invocation.Opens++;
                return invocation.Definition.BindDrivers!.MissingDos
                    ? 0u : bindDriversPrivateImageProbe
                        ? 0x9000u : invocation.DosBase;
            }
            if ((suite == MountEntrySuite || suite == Workbench31MountEntrySuite) &&
                Bus.CString(state.A[1]) == Icon.Name)
            {
                Require(state.D[0] == 37 && invocation.MountIconOpens == 0,
                    "Mount icon.library capability floor differs.");
                invocation.MountIconOpens++;
                return 0;
            }
            if (suite == IconPosEntrySuite &&
                Bus.CString(state.A[1]) == Icon.Name)
            {
                Require(state.D[0] == 0 && invocation.IconOpens == 0,
                    "IconPos icon.library minimum version differs.");
                invocation.IconOpens++;
                return invocation.Definition.IconPos!.IconOpenFailure
                    ? 0u : Invocation.IconPosBase;
            }
            if (suite == Check2090EntrySuite &&
                Bus.CString(state.A[1]) == Expansion.Name)
            {
                Require(state.D[0] == 33 &&
                    invocation.Check2090ExpansionOpens == 0,
                    "Check2090 expansion.library capability floor differs.");
                invocation.Check2090ExpansionOpens++;
                return invocation.Definition.Check2090!.ExpansionAvailable
                    ? invocation.ExpansionBase : 0u;
            }
            if (suite == Check2090EntrySuite &&
                Bus.CString(state.A[1]) == "dos.library")
            {
                Require(state.D[0] == 36,
                    "Check2090 requires the DOS36 capability floor.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == GuessBootDevEntrySuite &&
                Bus.CString(state.A[1]) == "utility.library")
            {
                Require(state.D[0] == 0 && invocation.GuessBootDevUtilityOpens == 0,
                    "GuessBootDev utility.library capability floor differs.");
                invocation.GuessBootDevUtilityOpens++;
                return invocation.Definition.GuessBootDev!.UtilityAvailable
                    ? 0xa000u : 0u;
            }
            if (suite == ExtractKickstartEntrySuite &&
                Bus.CString(state.A[1]) == "utility.library")
            {
                var definition = invocation.Definition.ExtractKickstart!;
                Require(state.D[0] == 0 && invocation.ExtractKickstartUtilityOpens == 0,
                    "ExtractKickstart utility.library capability floor differs.");
                invocation.ExtractKickstartUtilityOpens++;
                return definition.UtilityAvailable ? ExtractKickstartUtilityBase : 0u;
            }
            if (suite == GuessBootDevEntrySuite &&
                Bus.CString(state.A[1]) == Expansion.Name)
            {
                Require(state.D[0] == 33 && invocation.GuessBootDevExpansionOpens == 0,
                    "GuessBootDev expansion.library capability floor differs.");
                invocation.GuessBootDevExpansionOpens++;
                return invocation.Definition.GuessBootDev!.ExpansionAvailable
                    ? invocation.GuessBootDevExpansionBase : 0u;
            }
            if (suite == GuessBootDevEntrySuite &&
                Bus.CString(state.A[1]) == "dos.library")
            {
                Require(state.D[0] == 36, "GuessBootDev requires DOS36.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == RebootEntrySuite || suite == Workbench31RebootEntrySuite)
            {
                var minimum = suite == Workbench31RebootEntrySuite ? 36u : 37u;
                Require(Bus.CString(state.A[1]) == "dos.library" && state.D[0] == minimum,
                    "Reboot requires the profile DOS version.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == ResListEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37, "ResList requires DOS37.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == LibListEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37, "LibList requires DOS37.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == DevListEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37, "DevList requires DOS37.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == PortListEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37, "PortList requires DOS37.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == ModListEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37, "ModList requires DOS37.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (IsStatusEntrySuite(suite))
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == (IsWorkbench31StatusEntrySuite(suite) ? 36u : 37u),
                    "Status requires the profile DOS version.");
                invocation.Opens++;
                var modern = invocation.Definition.Status?.Modern == true;
                Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Version,
                    modern ? (ushort)51 : (ushort)50);
                Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Revision,
                    modern ? (ushort)51 : (ushort)6);
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (IsVersionEntrySuite(suite))
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37u,
                    "Version requires the profile DOS version.");
                invocation.Opens++;
                Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Version, 50);
                Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Revision, 6);
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == FindResidentEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 36,
                    "FindResident requires DOS36 capability floor in the bounded Workbench fixture.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == TaskListEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37, "TaskList requires DOS37.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == InfoEntrySuite || suite == Workbench31InfoEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == (suite == Workbench31InfoEntrySuite ? 36u : 37u),
                    "Info requires the profile DOS version.");
                invocation.Opens++;
                if (suite == InfoEntrySuite)
                {
                    var info = invocation.Definition.Info!;
                    Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Version,
                        info.DosVersion);
                    Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Revision,
                        info.DosRevision);
                }
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == DiskFreeEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37, "DiskFree requires DOS37.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (IsDirEntrySuite(suite))
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == (suite == Workbench31DirEntrySuite ? 36u : 37u),
                    "Dir requires the profile DOS version.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (IsMakeDirEntrySuite(suite))
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == (IsWorkbench31MakeDirEntrySuite(suite) ? 36u : 37u),
                    "MakeDir requires the profile DOS version.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (IsAssignEntrySuite(suite))
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == (IsWorkbench31AssignEntrySuite(suite) ? 36u : 37u),
                    "Assign requires the profile DOS version.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (IsJoinEntrySuite(suite))
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == (IsWorkbench31JoinEntrySuite(suite) ? 36u : 37u),
                    "Join requires the profile DOS version.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == DosListEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 51, "DOSList requires DOS51.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == ListEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37, "List requires DOS37.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == SearchEntrySuite &&
                Bus.CString(state.A[1]) == Locale.Name)
            {
                Require(state.D[0] == 37 && invocation.LocaleOpens == 0,
                    "Search requires locale.library 37 once.");
                invocation.LocaleOpens++;
                return invocation.Definition.Search!.LocaleLibraryOpenFailure
                    ? 0u : SearchLocaleBase;
            }
            if (IsSearchEntrySuite(suite))
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == (suite == SearchEntrySuite ? 37u : 36u),
                    "Search requires the profile DOS version.");
                invocation.Opens++;
                if (suite == SearchEntrySuite)
                {
                    Bus.Memory[invocation.DosBase +
                        (uint)ExecLayout.Library.Version] = 51;
                    Bus.Memory[invocation.DosBase +
                        (uint)ExecLayout.Library.Revision] = 28;
                }
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == WorkbenchListEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 36, "Workbench List requires DOS36.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == DiskChangeEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 50,
                    "DiskChange requires DOS50.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == BeepEntrySuite &&
                Bus.CString(state.A[1]) == Intuition.Name)
            {
                Require(state.D[0] == 33 && invocation.IntuitionOpens == 0,
                    "Beep intuition.library minimum version differs.");
                invocation.IntuitionOpens++;
                return invocation.Definition.BeepOpenFailure ? 0u : IntuitionBase;
            }
            if ((suite == RequestChoiceEntrySuite ||
                suite == Workbench31RequestChoiceEntrySuite) &&
                Bus.CString(state.A[1]) == Intuition.Name)
            {
                var minimum = suite == RequestChoiceEntrySuite ? 37u : 33u;
                Require(state.D[0] == minimum && invocation.IntuitionOpens == 0,
                    "RequestChoice intuition.library minimum version differs.");
                invocation.IntuitionOpens++;
                return invocation.Definition.RequestChoice!.IntuitionOpenFailure
                    ? 0u : IntuitionBase;
            }
            if ((suite == RequestFileEntrySuite ||
                suite == Workbench31RequestFileEntrySuite) &&
                Bus.CString(state.A[1]) == ASL.Name)
            {
                var minimum = 36u;
                Require(state.D[0] == minimum && invocation.AslOpens == 0,
                    "RequestFile asl.library minimum version differs.");
                invocation.AslOpens++;
                return invocation.Definition.RequestFile!.AslOpenFailure
                    ? 0u : AslBase;
            }
            if (suite == Workbench31DateEntrySuite &&
                Bus.CString(state.A[1]) == "utility.library")
            {
                Require(state.D[0] == 36, "Date utility.library capability floor differs.");
                Require(invocation.UtilityOpens == 0, "Date opened utility.library repeatedly.");
                invocation.UtilityOpens++;
                return DateUtilityBase;
            }
            if (suite == MorphOSDateEntrySuite &&
                Bus.CString(state.A[1]) == "locale.library")
            {
                Require(state.D[0] == 38 && invocation.LocaleOpens == 0,
                    "Date locale.library minimum version differs.");
                invocation.LocaleOpens++;
                return DateLocaleBase;
            }
            if (suite == MorphOSWhichEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37, "MorphOS Which requires DOS37.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == MorphOSAvailEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 37, "MorphOS Avail requires DOS37.");
                invocation.Opens++;
                return invocation.Definition.MissingDos ? 0u : invocation.DosBase;
            }
            if (suite == RequestChoiceEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 36, "RequestChoice requires the DOS36 capability floor.");
                invocation.Opens++;
                return invocation.Definition.RequestChoice!.MissingDos
                    ? 0u : invocation.DosBase;
            }
            if (suite == Workbench31RequestChoiceEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 36, "Workbench RequestChoice requires DOS36.");
                invocation.Opens++;
                return invocation.Definition.RequestChoice!.MissingDos
                    ? 0u : invocation.DosBase;
            }
            if (suite == RequestFileEntrySuite ||
                suite == Workbench31RequestFileEntrySuite)
            {
                Require(Bus.CString(state.A[1]) == "dos.library" &&
                    state.D[0] == 36u,
                    "RequestFile requires the DOS36 capability floor.");
                invocation.Opens++;
                return invocation.Definition.RequestFile!.MissingDos
                    ? 0u : invocation.DosBase;
            }
            if (suite == MorphOSRenameSuite)
                Require(state.D[0] == 37, "MorphOS Rename requires DOS37.");
            Require(Bus.CString(state.A[1]) == "dos.library" &&
                (state.D[0] == 36 || (suite == ExtractKickstartEntrySuite || suite == AddBuffersEntrySuite || suite == Workbench31VersionEntrySuite || suite == Workbench31AvailEntrySuite || suite == MorphOSAvailEntrySuite || suite == FormatEntrySuite || suite == MountEntrySuite || suite == Workbench31MountEntrySuite || suite == Workbench31DateEntrySuite || suite == MorphOSDateEntrySuite || suite == Workbench31SetDateEntrySuite || suite == MorphOSSetDateEntrySuite || suite == Workbench31WaitEntrySuite || suite == MorphOSWaitEntrySuite || suite == WaitForPortEntrySuite || suite == WaitForLibEntrySuite || suite == WaitForNotificationEntrySuite || suite == FileNoteEntrySuite || suite == TouchEntrySuite || suite == SetClockEntrySuite || suite == Workbench31SetClockEntrySuite || suite == Workbench31SetKeyboardEntrySuite || suite == MorphOSSetKeyboardEntrySuite || suite == Workbench31SetFontEntrySuite || suite == LoadMonDrvsEntrySuite || suite == ProtectEntrySuite || suite == Workbench31ProtectEntrySuite || suite == BreakEntrySuite || suite == ChangeTaskPriEntrySuite || suite == RelabelEntrySuite || suite == LockEntrySuite || suite == Workbench31LockEntrySuite || suite == MakeLinkEntrySuite || suite == MorphOSRenameSuite || suite == TaskListEntrySuite || IsTransferLoopProbeSuite || suite == DeleteObjectProbeSuite || IsDeleteCommandEntrySuite(suite) || suite is CopyArgumentGateEntrySuite or CopyOptionSetupSuite or CopyDirectSuite or CopyParsedDeleteSuite or CopyParsedMakeDirectorySuite || suite == CopyFilePairProbeSuite || suite == CopyDestinationProbeSuite || suite == CopyDestinationDirectoriesProbeSuite || suite == CopyNonFileSystemProbeSuite || suite == CopyLoopGuardProbeSuite || suite == CopyMetadataProbeSuite || suite is CopyResultPolicyProbeSuite or CopyCompletionSuite || suite == CopyOpenDestinationProbeSuite || suite == CopyPatternClassifierProbeSuite || suite == CopyFlatTraversalProbeSuite || suite == CopyDirectoryExitProbeSuite || suite == CopyDirectoryEntryProbeSuite || suite is CopyTraversalWorkProbeSuite or CopySourceRoutingProbeSuite or CopyDirectorySourcesSuite or CopyTargetDispatchSuite or CopySingleTargetSuite or CopyWorkProbeSuite || suite == CopyOutputProbeSuite || suite == CopyDirectoryOperationProbeSuite || suite == CopyLinkOperationProbeSuite || suite == CopyFileOperationProbeSuite || suite == CopyTransferProbeSuite || suite == CopyWorkPreparationProbeSuite || suite == CopyTraversalProbeSuite || suite == CopyMatchStepProbeSuite || suite is CopySoftLinkProbeSuite or CopyMatchedDirectoryProbeSuite) && state.D[0] == 37),
                "Unexpected library or minimum version.");
            invocation.Opens++;
            if(copyCommandRoot) {
                Bus.Word(invocation.DosBase+(uint)ExecLayout.Library.Version,invocation.Definition.CopyArgumentGate!.DosVersion);
                Bus.Word(invocation.DosBase+(uint)ExecLayout.Library.Revision,invocation.Definition.CopyArgumentGate!.DosRevision);
            }
            if (suite == MorphOSSetDateEntrySuite)
            {
                Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Version, 50);
                Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Revision, 67);
            }
            return invocation.Definition.MissingDos ? 0 : invocation.DosBase;
        });
        Register(ExecBase, ExecLvo.CloseLibrary, "CloseLibrary", (state, invocation) =>
        {
            if (suite == AddDataTypesListEntrySuite &&
                workbench31AddDataTypes &&
                CloseWorkbench31AddDataTypesLibrary(state.A[1],
                    invocation))
                return 0xc10ced;
            if (suite == AddDataTypesListEntrySuite &&
                state.A[1] == AddDataTypesDatatypesBase)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(definition.DataTypesOpenCalls == 1 &&
                    definition.DataTypesAvailable &&
                    definition.DataTypesCloseCalls == 0,
                    "AddDataTypes closed an unowned datatypes.library lease.");
                definition.DataTypesCloseCalls++;
                definition.Events.Add("close-datatypes");
                return 0xc10ced;
            }
            if (suite == AddDataTypesListEntrySuite &&
                state.A[1] == AddDataTypesLocaleBase)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(definition.LocaleOpenCalls == 1 &&
                    definition.LocaleAvailable &&
                    definition.LocaleCloseCalls == 0,
                    "AddDataTypes closed an unowned locale.library lease.");
                definition.LocaleCloseCalls++;
                definition.Events.Add("close-locale");
                return 0xc10ced;
            }
            if (suite == AddDataTypesListEntrySuite &&
                state.A[1] == AddDataTypesIffParseBase)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(definition.IffParseOpenCalls == 1 &&
                    definition.IffParseAvailable &&
                    definition.IffParseCloseCalls == 0,
                    "AddDataTypes closed an unowned iffparse.library lease.");
                definition.IffParseCloseCalls++;
                definition.Events.Add("close-iffparse");
                return 0xc10ced;
            }
            if (suite == AddDataTypesListEntrySuite &&
                state.A[1] == AddDataTypesUtilityBase)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(definition.UtilityOpenCalls == 1 &&
                    definition.UtilityAvailable && definition.UtilityCloseCalls == 0,
                    "AddDataTypes closed an unowned utility.library lease.");
                definition.UtilityCloseCalls++;
                definition.Events.Add("close-utility");
                return 0xc10ced;
            }
            if (IsVersionEntrySuite(suite) &&
                state.A[1] == VersionUtilityBase)
            {
                Require(invocation.VersionUtilityOpenCalls == 1 &&
                    invocation.VersionUtilityCloseCalls == 0,
                    "Version closed an unowned utility.library lease.");
                invocation.VersionUtilityCloseCalls++;
                return 0xc10ced;
            }
            if (IsVersionEntrySuite(suite) &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                state.A[1] == invocation.VersionLayout?.RexxLibraryBase)
            {
                var layout = invocation.VersionLayout!;
                Require(layout.AmbientLibraryOpenCalls == 1 &&
                    invocation.Definition.Version!.RexxLibraryAvailable &&
                    layout.AmbientLibraryCloseCalls == 0 &&
                    layout.AmbientCommandDeleteCalls == 1 &&
                    layout.AmbientMessageDeleteCalls == 1 &&
                    layout.AmbientPortDeleteCalls == 1,
                    "Version Ambient rexxsyslib.library lease closed before owned resources.");
                layout.AmbientLibraryCloseCalls++;
                return 0xc10ced;
            }
            if (IsVersionEntrySuite(suite) &&
                (!IsWorkbench31VersionEntrySuite(suite) ||
                    invocation.Definition.Version!.System) &&
                state.A[1] == invocation.VersionLayout?.VersionLibraryBase)
            {
                Require(invocation.VersionLibraryOpenCalls == 1 &&
                    invocation.VersionLibraryCloseCalls == 0 &&
                    invocation.Definition.Version!.VersionLibraryAvailable,
                    "Version closed an unowned version.library lease.");
                invocation.VersionLibraryCloseCalls++;
                return 0xc10ced;
            }
            if (suite == TaskListEntrySuite &&
                state.A[1] == TaskListSysDebugBase)
            {
                Require(invocation.TaskListSysDebugOpenCalls == 1 &&
                    invocation.TaskListSysDebugCloseCalls == 0 &&
                    !invocation.Definition.TaskList!.SysDebugUnavailable,
                    "TaskList closed an unowned sysdebug.library lease.");
                invocation.TaskListSysDebugCloseCalls++;
                invocation.Events.Add("TaskList.CloseSysDebug");
                return 0xc10ced;
            }
            if (suite == InfoEntrySuite && state.A[1] == InfoLocaleBase)
            {
                Require(invocation.InfoLocaleLibraryOpens == 1 &&
                    invocation.InfoLocaleLibraryCloses == 0 &&
                    invocation.InfoCloseLocaleCalls == 1 &&
                    invocation.InfoUtilityLibraryCloses == 0,
                    "Info closed locale.library before its locale or utility lease.");
                invocation.InfoLocaleLibraryCloses++;
                return 0xc10ced;
            }
            if (suite == InfoEntrySuite && state.A[1] == InfoUtilityBase)
            {
                Require(invocation.InfoUtilityLibraryOpens == 1 &&
                    invocation.InfoUtilityLibraryCloses == 0 &&
                    invocation.InfoLocaleLibraryCloses ==
                        (invocation.InfoLocaleLibraryOpens == 1 &&
                         !invocation.Definition.Info!.LocaleLibraryOpenFailure
                            ? 1 : 0),
                    "Info closed utility.library before the optional locale lease.");
                invocation.InfoUtilityLibraryCloses++;
                return 0xc10ced;
            }
            if ((suite == Workbench31SetKeyboardEntrySuite ||
                 suite == MorphOSSetKeyboardEntrySuite) &&
                state.A[1] == SetKeyboardUtilityBase)
            {
                Require(invocation.SetKeyboardUtilityOpens == 1 &&
                    invocation.SetKeyboardUtilityCloses == 0,
                    "SetKeyboard closed an unowned utility.library lease.");
                invocation.SetKeyboardUtilityCloses++;
                return 0xc10ced;
            }
            if ((suite == Workbench31SetKeyboardEntrySuite ||
                 suite == MorphOSSetKeyboardEntrySuite) &&
                state.A[1] == SetKeyboardKeymapBase)
            {
                Require(invocation.SetKeyboardKeymapOpens == 1 &&
                    invocation.SetKeyboardKeymapCloses == 0,
                    "SetKeyboard closed an unowned keymap.library lease.");
                invocation.SetKeyboardKeymapCloses++;
                return 0xc10ced;
            }
            if (suite == Workbench31SetFontEntrySuite &&
                state.A[1] == SetFontGraphicsBase)
            {
                Require(invocation.SetFontGraphicsOpens == 1 &&
                    invocation.SetFontGraphicsCloses == 0,
                    "SetFont closed an unowned graphics.library lease.");
                invocation.SetFontGraphicsCloses++;
                return 0xc10ced;
            }
            if (suite == Workbench31SetFontEntrySuite &&
                state.A[1] == SetFontDiskfontBase)
            {
                Require(invocation.SetFontDiskfontOpens == 1 &&
                    invocation.SetFontDiskfontCloses == 0,
                    "SetFont closed an unowned diskfont.library lease.");
                invocation.SetFontDiskfontCloses++;
                return 0xc10ced;
            }
            if (suite == Workbench31SetFontEntrySuite &&
                state.A[1] == SetFontUtilityBase)
            {
                Require(invocation.SetFontUtilityOpens == 1 &&
                    invocation.SetFontUtilityCloses == 0,
                    "SetFont closed an unowned utility.library lease.");
                invocation.SetFontUtilityCloses++;
                return 0xc10ced;
            }
            if ((suite == MountEntrySuite || suite == Workbench31MountEntrySuite) &&
                state.A[1] == invocation.ExpansionBase)
            {
                Require(invocation.MountExpansionOpens == 1 &&
                    invocation.MountExpansionCloses == 0,
                    "Mount closed an unowned expansion.library lease.");
                invocation.MountExpansionCloses++;
                return 0xc10ced;
            }
            if (IsBindDriversEntrySuite(suite) &&
                state.A[1] == invocation.ExpansionBase)
            {
                Require(invocation.BindDriversExpansionOpens == 1 &&
                    invocation.BindDriversExpansionCloses == 0 &&
                    invocation.Definition.BindDrivers!.ExpansionAvailable,
                    "BindDrivers closed an unowned expansion.library lease.");
                invocation.BindDriversExpansionCloses++;
                return 0xc10ced;
            }
            if (suite == BindDriversEntrySuite &&
                state.A[1] == Invocation.IconPosBase)
            {
                Require(invocation.BindDriversIconOpens == 1 &&
                    invocation.BindDriversIconCloses == 0 &&
                    invocation.Definition.BindDrivers!.IconAvailable,
                    "BindDrivers closed an unowned icon.library lease.");
                invocation.BindDriversIconCloses++;
                return 0xc10ced;
            }
            if (suite == IconPosEntrySuite && state.A[1] == Invocation.IconPosBase)
            {
                Require(invocation.IconOpens == 1 && invocation.IconCloses == 0,
                    "IconPos closed an unowned icon.library lease.");
                invocation.IconCloses++;
                return 0xc10ced;
            }
            if (suite == Check2090EntrySuite &&
                state.A[1] == invocation.ExpansionBase)
            {
                Require(invocation.Check2090ExpansionOpens == 1 &&
                    invocation.Check2090ExpansionCloses == 0 &&
                    invocation.Definition.Check2090!.ExpansionAvailable,
                    "Check2090 closed an unowned expansion.library lease.");
                invocation.Check2090ExpansionCloses++;
                return 0xc10ced;
            }
            if (suite == GuessBootDevEntrySuite &&
                state.A[1] == invocation.GuessBootDevExpansionBase)
            {
                Require(invocation.GuessBootDevExpansionOpens == 1 &&
                    invocation.GuessBootDevExpansionCloses == 0 &&
                    invocation.Definition.GuessBootDev!.ExpansionAvailable,
                    "GuessBootDev closed an unowned expansion.library lease.");
                invocation.GuessBootDevExpansionCloses++;
                return 0xc10ced;
            }
            if (suite == GuessBootDevEntrySuite && state.A[1] == 0xa000u)
            {
                Require(invocation.GuessBootDevUtilityOpens == 1 &&
                    invocation.GuessBootDevUtilityCloses == 0 &&
                    invocation.Definition.GuessBootDev!.UtilityAvailable,
                    "GuessBootDev closed an unowned utility.library lease.");
                invocation.GuessBootDevUtilityCloses++;
                return 0xc10ced;
            }
            if (suite == ExtractKickstartEntrySuite && state.A[1] == ExtractKickstartUtilityBase)
            {
                Require(invocation.ExtractKickstartUtilityOpens == 1 &&
                    invocation.ExtractKickstartUtilityCloses == 0 &&
                    invocation.Definition.ExtractKickstart!.UtilityAvailable,
                    "ExtractKickstart closed an unowned utility.library lease.");
                invocation.ExtractKickstartUtilityCloses++;
                return 0xc10ced;
            }
            if (suite == BeepEntrySuite && state.A[1] == IntuitionBase)
            {
                Require(invocation.IntuitionOpens == 1 && invocation.IntuitionCloses == 0,
                    "Beep closed an unowned intuition.library lease.");
                Bus.AssertReleased(invocation);
                invocation.IntuitionCloses++;
                return 0xc10ced;
            }
            if ((suite == RequestChoiceEntrySuite ||
                suite == Workbench31RequestChoiceEntrySuite) &&
                state.A[1] == IntuitionBase)
            {
                Require(invocation.IntuitionOpens == 1 && invocation.IntuitionCloses == 0,
                    "RequestChoice closed an unowned intuition.library lease.");
                invocation.IntuitionCloses++;
                return 0xc10ced;
            }
            if ((suite == RequestFileEntrySuite ||
                suite == Workbench31RequestFileEntrySuite) &&
                state.A[1] == AslBase)
            {
                Require(invocation.AslOpens == 1 && invocation.AslCloses == 0,
                    "RequestFile closed an unowned asl.library lease.");
                invocation.AslCloses++;
                return 0xc10ced;
            }
            if (suite == Workbench31DateEntrySuite && state.A[1] == DateUtilityBase)
            {
                Require(invocation.UtilityOpens == 1 && invocation.UtilityCloses == 0,
                    "Date closed an unowned utility.library lease.");
                Bus.AssertReleased(invocation);
                invocation.UtilityCloses++;
                return 0xc10ced;
            }
            if (suite == MorphOSDateEntrySuite && state.A[1] == DateLocaleBase)
            {
                Require(invocation.LocaleOpens == 1 && invocation.LocaleCloses == 0,
                    "Date closed an unowned locale.library lease.");
                invocation.LocaleCloses++;
                return 0xc10ced;
            }
            if (suite == SearchEntrySuite && state.A[1] == SearchLocaleBase)
            {
                Require(invocation.LocaleOpens == 1 && invocation.LocaleCloses == 0,
                    "Search closed an unowned locale.library lease.");
                invocation.LocaleCloses++;
                return 0xc10ced;
            }
            var closingWorkbenchDosAlias = bindDriversPrivateImageProbe &&
                state.A[1] is 0x8000u or 0x9000u;
            Require((state.A[1] == invocation.DosBase || closingWorkbenchDosAlias) &&
                !invocation.Definition.MissingDos &&
                invocation.Opens == 1 && invocation.Closes == 0, "Closed unowned or already closed library.");
            if (suite != TypeTextIoProbeSuite && suite != GuessBootDevEntrySuite &&
                suite != FormatEntrySuite && suite != MorphOSSetKeyboardEntrySuite &&
                suite != Workbench31SetFontEntrySuite)
                Bus.AssertReleased(invocation);
            invocation.Closes++;
            return 0xc10ced;
        });
        Register(ExecBase, ExecLvo.AllocMem, "AllocMem", (state, invocation) =>
        {
            if (addDataTypesCallbackProbe)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(definition.CallbackAllocCalls == 0 &&
                    state.D[0] == 32 && state.D[1] ==
                        (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "AddDataTypes allocation callback size/flags differ.");
                definition.CallbackAllocCalls++;
                invocation.AllocationRequests.Add(state.D[0]);
                definition.CallbackAllocationAddress = Bus.Allocate(
                    invocation, state.D[0], "AddDataTypesCallback", true);
                return definition.CallbackAllocationAddress;
            }
            if (suite == AddDataTypesListEntrySuite)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                var resultBytes = (definition.Workbench31Profile
                    ? NativeWorkbench31AddDataTypesCommand.ResultCount
                    : NativeMorphOSAddDataTypesCommand.CliResultCount) * 4;
                Require(definition.ResultAllocationCalls == 0 &&
                    state.D[0] == resultBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "AddDataTypes ReadArgs result array allocation differs.");
                definition.ResultAllocationCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (definition.ResultAllocationFailure) return 0;
                var resultArray = Bus.Allocate(invocation, state.D[0],
                    "Exec", true);
                definition.ResultArrayAddress = resultArray;
                return resultArray;
            }
            if (invocation.Definition.LoadMonDrvs is not null)
            {
                var loadAllocation = invocation.Allocations++;
                var loadBytes = loadAllocation switch
                {
                    0 => NativeMorphOSLoadMonDrvsCommand.ResultCount * 4u,
                    1 => NativeMorphOSLoadMonDrvsCommand.WorkspaceBytes,
                    _ => 0u
                };
                Require(loadBytes != 0 && state.D[0] == loadBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "LoadMonDrvs allocation shape differs.");
                invocation.AllocationRequests.Add(loadBytes);
                return Bus.Allocate(invocation, loadBytes,
                    loadAllocation == 0 ? "LoadMonDrvsResults" : "LoadMonDrvsWorkspace", true);
            }
            if (invocation.Definition.SetFont is not null)
            {
                var allocation = invocation.Allocations++;
                var bytes = allocation switch
                {
                    0 => SetFontResultBytes,
                    1 => SetFontNameBytes,
                    2 => SetFontTextAttrBytes,
                    3 => SetFontInfoDataBytes,
                    _ => 0u
                };
                Require(bytes != 0 && state.D[0] == bytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "SetFont allocation shape differs.");
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.SetFont!.AllocationFailureAt == allocation)
                    return 0;
                var address = Bus.Allocate(invocation, bytes,
                    allocation == 0 ? "SetFontResults" :
                    allocation == 1 ? "SetFontName" :
                    allocation == 2 ? "SetFontTextAttr" : "SetFontInfoData", true);
                if (allocation == 1) invocation.SetFontName = address;
                if (allocation == 2) invocation.SetFontTextAttr = address;
                if (allocation == 3) invocation.SetFontInfoData = address;
                return address;
            }
            if (invocation.Definition.SetKeyboard is not null)
            {
                var allocation = invocation.Allocations++;
                var bytes = allocation == 0 ? 4u : 256u;
                Require(allocation < 2 && state.D[0] == bytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "SetKeyboard result/path allocation differs.");
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, bytes,
                    allocation == 0 ? "Exec" : "SetKeyboardPath", true);
            }
            if (invocation.Definition.BindDrivers is not null)
            {
                Require(state.D[0] == NativeMorphOSBindDriversCommand.WorkspaceBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear) && invocation.Allocations == 0,
                    "BindDrivers workspace allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, state.D[0], "BindDriversWorkspace", true);
            }
            if (invocation.Definition.Format is { } format)
            {
                var layout = invocation.FormatLayout!;
                var allocation = invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (format.AllocationFailure && allocation == 0)
                    return 0;
                uint bytes;
                var clear = true;
                switch (allocation)
                {
                    case 0:
                        bytes = NativeMorphOSFormatCommand.ResultCount * 4u;
                        break;
                    case 1:
                        bytes = layout.DeviceScratchBytes;
                        break;
                    case 2:
                        Require(format.Media, "Format allocated a device name outside media fixture.");
                        bytes = layout.DeviceNameBytes;
                        break;
                    case 3:
                        Require(format.Media,
                            "Format allocated media buffer outside media fixture.");
                        if (format.Quick)
                        {
                            bytes = 5;
                        }
                        else
                        {
                            bytes = state.D[0];
                            clear = false;
                            layout.FormatBufferBytes = bytes;
                        }
                        break;
                    case 4:
                        Require(format.Media, "Format allocated media post-buffer outside media fixture.");
                        bytes = format.Quick ? InfoData.Size : 5;
                        break;
                    case 5:
                        Require(format.Media, "Format allocated InfoData outside media fixture.");
                        bytes = InfoData.Size;
                        break;
                    default:
                        throw new InvalidOperationException("Format allocated an unexpected buffer.");
                }
                Require(state.D[0] == bytes && state.D[1] == (uint)(Exec.MemoryFlags.Public |
                    (clear ? Exec.MemoryFlags.Clear : 0)),
                    "Format allocation shape differs.");
                var value = Bus.Allocate(invocation, bytes, "Exec", clear);
                if (allocation == 1) layout.DeviceScratch = value;
                else if (allocation == 2) layout.DeviceName = value;
                else if (allocation == 3) layout.FormatBuffer = value;
                return value;
            }
            if (invocation.Definition.Mount is not null)
            {
                var resultBytes = suite == Workbench31MountEntrySuite
                    ? NativeWorkbench31MountCommand.ResultCount * 4u
                    : NativeMorphOSMountCommand.ResultCount * 4u;
                Require(invocation.Allocations == 0 &&
                    state.D[0] == resultBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "Mount result allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.AllocationFailure)
                    return 0;
                return Bus.Allocate(invocation, state.D[0], "Exec", true);
            }
            if (suite == ExtractKickstartEntrySuite)
            {
                Require(invocation.Allocations == 0 && state.D[0] ==
                    NativeWorkbench31ExtractKickstartCommand.ResultCount * 4u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "ExtractKickstart result allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, state.D[0], "Exec", true);
            }
            if (invocation.Definition.RequestFile is not null)
            {
                var allocation = invocation.RequestFileExecMemCalls++;
                var bytes = suite == Workbench31RequestFileEntrySuite
                    ? NativeWorkbench31RequestFileCommand.ResultCount * 4u
                    : NativeMorphOSRequestFileCommand.ResultCount * 4u;
                Require(allocation == 0 &&
                    state.D[0] == bytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "RequestFile Exec allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, state.D[0], "Exec", true);
            }
            if (invocation.Definition.RequestChoice is not null)
            {
                var bytes = suite == Workbench31RequestChoiceEntrySuite ? 16u : 24u;
                Require(state.D[0] == bytes && state.D[1] ==
                    (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "RequestChoice ReadArgs result allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, state.D[0], "Exec", true);
            }
            if (invocation.Definition.Search is not null)
            {
                var allocation = invocation.SearchAllocMemCalls++;
                var bytes = allocation == 0
                    ? (suite == "wb31-search-native-entry-vector-fixture"
                        ? NativeWorkbench31SearchCommand.ResultCount
                        : NativeMorphOSSearchCommand.ResultCount) * 4u
                    : allocation == 1
                        ? NativeMorphOSSearchCommand.WorkspaceAllocationBytes
                        : state.D[0];
                var morphosFileBuffer = allocation >= 2 &&
                    suite == "morphos320-search-native-entry-vector-fixture";
                var expectedFlags = morphosFileBuffer
                    ? Exec.MemoryFlags.Any
                    : Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear;
                Require((allocation < 2 && state.D[0] == bytes ||
                        morphosFileBuffer && state.D[0] != 0 ||
                        allocation >= 2 && !morphosFileBuffer &&
                            state.D[0] > 8192) &&
                    state.D[1] == (uint)expectedFlags,
                    "Search allocation shape differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.Search.AllocationFailure && allocation == 1)
                    return 0;
                if (morphosFileBuffer)
                {
                    var fileBufferAttempt = allocation - 2;
                    if (invocation.Definition.Search.BufferAllocationFailure ||
                        fileBufferAttempt <
                            invocation.Definition.Search.BufferAllocationFailures)
                        return 0;
                }
                var searchAllocation = Bus.Allocate(invocation, bytes,
                    allocation == 0 ? "Exec" : allocation == 1
                        ? "SearchWorkspace" : "SearchFileBuffer",
                    !morphosFileBuffer);
                if (allocation == 1)
                    invocation.SearchEntryLayout!.Workspace = searchAllocation;
                if (allocation >= 2)
                {
                    invocation.SearchEntryLayout!.FileBuffer = searchAllocation;
                    invocation.SearchEntryLayout.FileBufferBytes = bytes;
                    invocation.SearchEntryLayout.FileBufferAllocationCount++;
                }
                return searchAllocation;
            }
            if (invocation.Definition.List is not null)
            {
                var allocation = invocation.ListAllocMemCalls++;
                var bytes = allocation == 0
                    ? (suite == WorkbenchListEntrySuite
                        ? NativeWorkbench31ListCommand.ResultCount * 4u
                        : NativeMorphOSListCommand.ResultCount * 4u)
                    : NativeMorphOSListCommand.WorkspaceAllocationBytes;
                Require(allocation < 2 && state.D[0] == bytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    $"List allocation shape differs (count={allocation}, bytes={state.D[0]}).");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.List.AllocationFailure && allocation == 1)
                    return 0;
                var address = Bus.Allocate(invocation, state.D[0],
                    allocation == 0 ? "ListResults" : "ListWorkspace", true);
                if (allocation == 1)
                    invocation.ListLayout!.Workspace = address;
                return address;
            }
            if (invocation.Definition.DosList is not null)
            {
                Require(invocation.DosListAllocMemCalls == 0 &&
                    state.D[0] == 24u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "DOSList ReadArgs result allocation differs.");
                invocation.DosListAllocMemCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, state.D[0],
                    "DosListResults", true);
            }
            if (invocation.Definition.SetClock is not null)
            {
                var bytes = invocation.Allocations == 0 ? 12u : 8u;
                Require(invocation.Allocations < 2 && state.D[0] == bytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "SetClock allocation shape differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, bytes,
                    bytes == 8 ? "SetClockTimeval" : "Exec", true);
            }
            if (invocation.Definition.MorphOSDate is not null)
            {
                var resultBytes = invocation.Allocations == 0 ? 20u : 192u;
                Require(invocation.Allocations < 2 && state.D[0] == resultBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "MorphOS Date workspace allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.AllocationFailure) return 0;
                return Bus.Allocate(invocation, resultBytes, "Exec", true);
            }
            if (invocation.Definition.SetDate is not null)
            {
                var morphOs = suite == MorphOSSetDateEntrySuite;
                var resultArray = invocation.Allocations == 0;
                var dateTimeAllocation = morphOs
                    ? invocation.Allocations == 1
                    : invocation.Allocations == 2;
                var anchorAllocation = morphOs && invocation.Allocations == 2;
                Require(resultArray || dateTimeAllocation || anchorAllocation,
                    "SetDate allocation order differs.");
                var setDateBytes = resultArray ? 20u : dateTimeAllocation
                    ? (uint)DosLayout.DateTime.Size : (uint)DosLayout.AnchorPath.Size;
                Require(state.D[0] == setDateBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    $"SetDate result/DateTime allocation differs (count={invocation.Allocations}, size={state.D[0]}, flags={state.D[1]}).");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.SetDate.AllocationFailure) return 0;
                var kind = resultArray ? "Exec" : dateTimeAllocation
                    ? "SetDateDateTime" : "SetDateAnchor";
                var value = Bus.Allocate(invocation, state.D[0], kind, true);
                if (dateTimeAllocation) invocation.SetDateLayout!.DateTime = value;
                if (anchorAllocation) invocation.SetDateLayout!.Anchor = value;
                return value;
            }
            if (invocation.Definition.Status is not null)
            {
                Require(invocation.Allocations == 0 && state.D[0] == 20u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "Status ReadArgs result allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, state.D[0], "Exec", true);
            }
            if (invocation.Definition.Version is not null)
            {
                var ambientLayout = invocation.VersionLayout!;
                var ambientDefinition = invocation.Definition.Version!;
                if (ambientLayout.AmbientSegmentActive &&
                    (ambientDefinition.AmbientLoadedResidentName is not null &&
                        ambientDefinition.AmbientLoadedResidentIdString is not null ||
                     ambientDefinition.FileLoadedResidentName is not null &&
                        ambientDefinition.FileLoadedResidentIdString is not null) &&
                    ambientLayout.AmbientResidentTextAllocCalls == 0)
                {
                    Require(state.D[0] ==
                            ambientLayout.AmbientResidentTextBytes &&
                        state.D[1] == (uint)Exec.MemoryFlags.Any,
                        "Version Ambient resident text allocation differs.");
                    ambientLayout.AmbientResidentTextAllocCalls++;
                    ambientLayout.AmbientResidentTextAllocRequests++;
                    invocation.VersionAllocMemCalls++;
                    invocation.Allocations++;
                    invocation.AllocationRequests.Add(state.D[0]);
                    var ambientResidentText = Bus.Allocate(invocation,
                        state.D[0], "VersionAmbientResidentText", false);
                    ambientLayout.AmbientResidentText = ambientResidentText;
                    ambientLayout.AmbientResidentTextAllocSuccesses++;
                    return ambientResidentText;
                }
                if (ambientLayout.AmbientFileActive)
                {
                    var ambientAllocation =
                        ambientLayout.AmbientFileAllocMemCalls++;
                    var ambientBytes = ambientAllocation == 0
                        ? 16_385u : 16_449u;
                    Require(ambientAllocation < 2 &&
                        state.D[0] == ambientBytes &&
                        state.D[1] == (uint)Exec.MemoryFlags.Public,
                        "Version Ambient file allocation differs.");
                    invocation.VersionAllocMemCalls++;
                    invocation.Allocations++;
                    invocation.AllocationRequests.Add(state.D[0]);
                    ambientLayout.AmbientFileAllocMemRequests++;
                    var ambientKind = ambientAllocation == 0
                        ? "VersionAmbientFileBuffer"
                        : "VersionAmbientFileScratch";
                    var ambientAddress = Bus.Allocate(invocation,
                        state.D[0], ambientKind, false);
                    if (ambientAllocation == 0)
                    {
                        ambientLayout.AmbientFileBuffer = ambientAddress;
                    }
                    else
                    {
                        ambientLayout.AmbientFileScratch = ambientAddress;
                    }
                    ambientLayout.AmbientFileAllocMemSuccesses++;
                    return ambientAddress;
                }
                var allocation = invocation.VersionAllocMemCalls++;
                var resultBytes = IsWorkbench31VersionEntrySuite(suite)
                    ? 32u : 28u;
                var isResults = allocation == 0;
                var allocationsPerFile = invocation.Definition.Version!.Md5
                    ? 3 : 2;
                var fileCount = invocation.Definition.Version.Names?.Length
                    ?? 1;
                var systemVersionActive = !IsWorkbench31VersionEntrySuite(suite) &&
                    invocation.Definition.Version.System &&
                    invocation.Definition.Version.VersionLibraryAvailable &&
                    invocation.Definition.Version.VersionLibraryListed;
                var residentVersionActive =
                    !IsWorkbench31VersionEntrySuite(suite) &&
                    (invocation.Definition.Version.ResidentFound ||
                     invocation.Definition.Version.ShellCommandResidentFound ||
                     invocation.Definition.Version.VolumeDeviceProcFound &&
                        invocation.Definition.Version.VolumeSegmentFound &&
                        invocation.Definition.Version.VolumeResidentFound &&
                        (!invocation.Definition.Version.VolumeDeviceIsVolume ||
                         invocation.Definition.Version.VolumeDeviceFound)) &&
                    !invocation.Definition.Version.System &&
                    !invocation.Definition.Version.File;
                var commandSegmentVersionActive =
                    (!invocation.Definition.Version.System &&
                     !invocation.Definition.Version.File) &&
                    !invocation.Definition.Version.ResidentFound &&
                    invocation.Definition.Version.CommandSegmentFound &&
                    invocation.Definition.Version.CommandSegmentUseCount != -2 &&
                    invocation.Definition.Version.CommandSegmentUseCount != -999 &&
                    !invocation.Definition.Version.System &&
                    !invocation.Definition.Version.File;
                var namedLibraryVersionActive =
                    !IsWorkbench31VersionEntrySuite(suite) &&
                    !invocation.Definition.Version.System &&
                    !invocation.Definition.Version.File &&
                    !invocation.Definition.Version.ResidentFound &&
                    invocation.Definition.Version.ExecLibraryFound;
                var namedDeviceVersionActive =
                    !IsWorkbench31VersionEntrySuite(suite) &&
                    !invocation.Definition.Version.System &&
                    !invocation.Definition.Version.File &&
                    !invocation.Definition.Version.ResidentFound &&
                    !invocation.Definition.Version.ExecLibraryFound &&
                    invocation.Definition.Version.ExecDeviceFound;
                var namedDeviceTextAllocation = namedDeviceVersionActive &&
                    invocation.Definition.Version!.Full && allocation == 1;
                var namedDeviceScratchAllocation =
                    namedDeviceVersionActive &&
                    invocation.Definition.Version!.Full && allocation == 2;
                var namedLibraryFileActive =
                    !IsWorkbench31VersionEntrySuite(suite) &&
                    !invocation.Definition.Version.System &&
                    !invocation.Definition.Version.File &&
                    !invocation.Definition.Version.ResidentFound &&
                    !invocation.Definition.Version.ExecLibraryFound &&
                    invocation.Definition.Version.LibraryFileHitPath is not null;
                var namedDirectFileActive =
                    !IsWorkbench31VersionEntrySuite(suite) &&
                    !invocation.Definition.Version.System &&
                    !invocation.Definition.Version.File &&
                    !invocation.Definition.Version.Resident &&
                    !invocation.Definition.Version.ResidentFound &&
                    !invocation.Definition.Version.ExecLibraryFound &&
                    !invocation.Definition.Version.ExecDeviceFound &&
                    invocation.Definition.Version.DirectFileHit;
                var fileAllocationIndex = allocation == 0 ? 0
                    : (allocation - 1) % allocationsPerFile + 1;
                var workbenchDirectFileAllocation =
                    IsWorkbench31VersionEntrySuite(suite) &&
                    !invocation.Definition.Version!.System &&
                    (invocation.Definition.Version.File ||
                     !invocation.Definition.Version.File &&
                        (invocation.Definition.Version.DirectFileHit ||
                         invocation.Definition.Version.WorkbenchFileHitPath is not null));
                var systemTextAllocation = systemVersionActive &&
                    allocation == 1 +
                        invocation.VersionLayout!
                            .AmbientFileAllocMemRequests +
                        invocation.VersionLayout
                            .AmbientResidentTextAllocRequests;
                var systemScratchAllocation = systemVersionActive &&
                    allocation == 2 +
                        invocation.VersionLayout!
                            .AmbientFileAllocMemRequests +
                        invocation.VersionLayout
                            .AmbientResidentTextAllocRequests;
                var residentAllocationStep = allocation == 0
                    ? 0 : (allocation - 1) % 2;
                var residentTextAllocation = residentVersionActive &&
                    allocation > 0 && residentAllocationStep == 0;
                var residentScratchAllocation = residentVersionActive &&
                    allocation > 0 && residentAllocationStep == 1;
                var commandSegmentScratchAllocation =
                    commandSegmentVersionActive && allocation == 1;
                var commandSegmentTextAllocation =
                    commandSegmentVersionActive && allocation == 2 &&
                    !string.IsNullOrEmpty(invocation.Definition.Version!
                        .CommandSegmentVersionText);
                var namedLibraryTextAllocation = namedLibraryVersionActive &&
                    invocation.Definition.Version!.Full && allocation == 1;
                var namedLibraryScratchAllocation =
                    namedLibraryVersionActive &&
                    invocation.Definition.Version!.Full && allocation == 2;
                var namedLibraryNameAllocation = namedLibraryVersionActive &&
                    allocation == (invocation.Definition.Version!.Full ? 3 : 1);
                var namedDeviceNameAllocation = namedDeviceVersionActive &&
                    allocation == (invocation.Definition.Version!.Full ? 3 : 1);
                var versionExpectedBytes = systemTextAllocation
                    ? invocation.VersionLayout!.SystemVersionTextBytes
                    : systemScratchAllocation
                        ? 16_449u
                        : commandSegmentScratchAllocation
                            ? 16_449u
                        : commandSegmentTextAllocation
                                ? unchecked((uint)Encoding.Latin1.GetByteCount(
                                    invocation.Definition.Version!
                                        .CommandSegmentVersionText!) + 1)
                        : namedLibraryTextAllocation
                            ? invocation.VersionLayout!
                                .ExecLibraryVersionTextBytes
                            : namedLibraryScratchAllocation
                                ? 16_449u
                        : namedLibraryNameAllocation
                                    ? invocation.VersionLayout!
                                        .ExecLibraryNameCopyBytes
                                    : namedDeviceTextAllocation
                                        ? invocation.VersionLayout!
                                            .ExecDeviceVersionTextBytes
                                    : namedDeviceScratchAllocation
                                        ? 16_449u
                                    : namedDeviceNameAllocation
                                        ? invocation.VersionLayout!
                                            .ExecDeviceNameCopyBytes
                        : residentTextAllocation
                            ? invocation.VersionLayout!
                                .ResidentVersionTextBytes
                            : residentScratchAllocation
                                ? 16_449u
                        : fileAllocationIndex switch
                {
                    0 => resultBytes,
                    1 => 16_385u,
                    2 => 16_449u,
                    3 => 96u,
                    _ => 0u
                };
                var expectedFlags = isResults
                    ? Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear
                    : residentTextAllocation || residentScratchAllocation ||
                        commandSegmentScratchAllocation ||
                        commandSegmentTextAllocation ||
                        namedLibraryNameAllocation ||
                        namedDeviceNameAllocation
                        ? Exec.MemoryFlags.Any : Exec.MemoryFlags.Public;
                var fileAllocation = allocation > 0 &&
                    allocation <= fileCount * allocationsPerFile &&
                    (workbenchDirectFileAllocation ||
                     (invocation.Definition.Version!.File &&
                      !invocation.Definition.Version.Resident ||
                      namedLibraryFileActive || namedDirectFileActive) &&
                     !IsWorkbench31VersionEntrySuite(suite));
                Require((allocation == 0 || fileAllocation ||
                    systemTextAllocation || systemScratchAllocation ||
                    residentTextAllocation || residentScratchAllocation ||
                    commandSegmentScratchAllocation ||
                    commandSegmentTextAllocation ||
                    namedLibraryTextAllocation ||
                    namedLibraryScratchAllocation ||
                    namedLibraryNameAllocation ||
                    namedDeviceTextAllocation ||
                    namedDeviceScratchAllocation ||
                    namedDeviceNameAllocation) &&
                    state.D[0] == versionExpectedBytes &&
                    state.D[1] == (uint)expectedFlags,
                    $"Version result/file-buffer allocation differs (index={allocation}, bytes={state.D[0]}, expected={versionExpectedBytes}, flags={state.D[1]}, expectedFlags={(uint)expectedFlags}, textBytes={invocation.VersionLayout!.SystemVersionTextBytes}).");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (fileAllocation &&
                    invocation.Definition.Version!.AllocationFailureAt == allocation ||
                    systemTextAllocation &&
                    invocation.Definition.Version!.SystemParseAllocationFailureAt == 1 ||
                    systemScratchAllocation &&
                    invocation.Definition.Version!.SystemParseAllocationFailureAt == 2 ||
                    residentTextAllocation &&
                    invocation.Definition.Version!
                        .ResidentParseAllocationFailureAt == 1 ||
                    residentScratchAllocation &&
                    invocation.Definition.Version!
                        .ResidentParseAllocationFailureAt == 2)
                    return 0;
                var versionKind = systemTextAllocation
                    ? "VersionSystemText"
                    : systemScratchAllocation
                        ? "VersionSystemScratch"
                        : residentTextAllocation
                            ? "VersionResidentText"
                            : residentScratchAllocation
                                ? "VersionResidentScratch"
                                : commandSegmentScratchAllocation
                                    ? "VersionCommandSegmentScratch"
                                    : commandSegmentTextAllocation
                                    ? "VersionCommandSegmentText"
                                    : namedLibraryTextAllocation
                                        ? "VersionExecLibraryText"
                                        : namedLibraryScratchAllocation
                                            ? "VersionExecLibraryScratch"
                                            : namedLibraryNameAllocation
                                                ? "VersionExecLibraryName"
                                                : namedDeviceTextAllocation
                                                    ? "VersionExecDeviceText"
                                                : namedDeviceScratchAllocation
                                                    ? "VersionExecDeviceScratch"
                                                : namedDeviceNameAllocation
                                                    ? "VersionExecDeviceName"
                        : fileAllocationIndex switch
                {
                    0 => "Exec",
                    1 => "VersionFileBuffer",
                    2 => "VersionScanBuffer",
                    _ => "VersionMd5Context"
                };
                var address = Bus.Allocate(invocation, state.D[0], versionKind,
                    isResults);
                if (systemTextAllocation)
                    invocation.VersionLayout!.SystemVersionText = address;
                else if (systemScratchAllocation)
                    invocation.VersionLayout!.SystemVersionScratch = address;
                else if (residentTextAllocation)
                    invocation.VersionLayout!.ResidentVersionText = address;
                else if (residentScratchAllocation)
                    invocation.VersionLayout!.ResidentVersionScratch =
                        address;
                else if (commandSegmentScratchAllocation)
                    invocation.VersionLayout!.CommandSegmentScratch = address;
                else if (commandSegmentTextAllocation)
                    invocation.VersionLayout!.CommandSegmentText = address;
                else if (namedLibraryTextAllocation)
                    invocation.VersionLayout!.ExecLibraryVersionText =
                        address;
                else if (namedLibraryScratchAllocation)
                    invocation.VersionLayout!.ExecLibraryVersionScratch =
                        address;
                else if (namedLibraryNameAllocation)
                    invocation.VersionLayout!.ExecLibraryNameCopy = address;
                else if (namedDeviceTextAllocation)
                    invocation.VersionLayout!.ExecDeviceVersionText = address;
                else if (namedDeviceScratchAllocation)
                    invocation.VersionLayout!.ExecDeviceVersionScratch =
                        address;
                else if (namedDeviceNameAllocation)
                    invocation.VersionLayout!.ExecDeviceNameCopy = address;
                else if (fileAllocationIndex == 1)
                    invocation.VersionLayout!.FileBuffer = address;
                else if (fileAllocationIndex == 2)
                    invocation.VersionLayout!.ScratchBuffer = address;
                else if (fileAllocationIndex == 3)
                    invocation.VersionLayout!.Md5Context = address;
                return address;
            }
            if (invocation.Definition.FindResident is not null)
            {
                Require(invocation.Allocations == 0 && state.D[0] == 4u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "FindResident ReadArgs result allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, state.D[0], "Exec", true);
            }
            if (invocation.Definition.TaskList is not null)
            {
                Require(invocation.Allocations == 0 && state.D[0] == 44u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    $"TaskList ReadArgs result allocation differs (bytes={state.D[0]}, flags={state.D[1]}).");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, state.D[0], "Exec", true);
            }
            if (invocation.Definition.DiskFree is { } diskFree)
            {
                var allocation = invocation.DiskFreeAllocMemCalls++;
                var bytes = allocation == 0
                    ? NativeMorphOSDiskFreeCommand.ResultCount * 4u
                    : InfoData.Size;
                Require(allocation < 2 && state.D[0] == bytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "DiskFree allocation shape differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (allocation == 0 && diskFree.AllocationFailure)
                    return 0;
                var kind = allocation == 0 ? "Exec" : "DiskFreeInfo";
                var value = Bus.Allocate(invocation, bytes, kind, true);
                if (allocation == 1) invocation.DiskFreeLayout!.Info = value;
                return value;
            }
            if (invocation.Definition.Info is not null)
            {
                var resultArray = invocation.InfoAllocMemCalls == 0;
                var maxInfoData = Math.Max(1,
                    invocation.Definition.Info.Entries / 2);
                Require(resultArray ||
                    invocation.InfoAllocMemCalls <= maxInfoData,
                    "Info allocation order differs.");
                var bytes = resultArray
                    ? (suite == Workbench31InfoEntrySuite ? 4u : 24u)
                    : InfoData.Size;
                Require(state.D[0] == bytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    $"Info allocation shape differs (bytes={state.D[0]}, count={invocation.Allocations}).");
                invocation.InfoAllocMemCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, bytes,
                    resultArray ? "Exec" : "InfoData", true);
            }
            if (invocation.Definition.MakeDir is { } makeDir)
            {
                var expected = invocation.MakeDirLayout!.AllocMemCalls == 0
                    ? (IsWorkbench31MakeDirEntrySuite(suite)
                        ? Workbench31MakeDirCommand.ResultCount
                        : NativeMorphOSMakeDirCommand.ResultCount) * 4u
                    : FileInfoBlock.SizeInBytes;
                Require(invocation.MakeDirLayout.AllocMemCalls <
                    (makeDir.All ? 2 : 1) && state.D[0] == expected &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "MakeDir allocation shape differs.");
                invocation.MakeDirLayout.AllocMemCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.MakeDirLayout.AllocMemCalls == 1 &&
                    makeDir.AllocationFailure)
                    return 0;
                if (invocation.MakeDirLayout.AllocMemCalls == 2 &&
                    makeDir.FibAllocationFailure)
                    return 0;
                var kind = invocation.MakeDirLayout.AllocMemCalls == 1
                    ? "MakeDirResult" : "MakeDirFib";
                var address = Bus.Allocate(invocation, state.D[0], kind, true);
                if (kind == "MakeDirFib") invocation.MakeDirLayout.Fib = address;
                return address;
            }
            if (invocation.Definition.Join is { } join)
            {
                var layout = invocation.JoinLayout!;
                var allocation = layout.AllocMemCalls;
                var joinBytes = allocation == 0
                    ? NativeMorphOSJoinCommand.WorkspaceAllocationBytes
                    : allocation == 1 ? NativeMorphOSJoinCommand.ResultCount * 4u
                    : NativeMorphOSJoinCommand.TransferBufferBytes;
                Require(allocation < 2 + join.Sources.Length &&
                    state.D[0] == joinBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "Join allocation shape differs.");
                layout.AllocMemCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (join.AllocationFailure == allocation)
                    return 0;
                var joinKind = allocation == 0 ? "JoinWorkspace" :
                    allocation == 1 ? "JoinResults" : "JoinBuffer";
                var value = Bus.Allocate(invocation, state.D[0], joinKind, true);
                if (allocation == 0) layout.Workspace = value;
                if (allocation >= 2) layout.Buffers.Add(value);
                return value;
            }
            if (invocation.Definition.Assign is { } assign)
            {
                var layout = invocation.AssignLayout!;
                    Require(layout.AllocMemCalls == 0 &&
                    state.D[0] == (IsWorkbench31AssignEntrySuite(suite)
                        ? NativeWorkbench31AssignCommand.ResultCount
                        : NativeMorphOSAssignCommand.ResultCount) * 4u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "Assign result allocation shape differs.");
                layout.AllocMemCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return assign.AllocationFailure ? 0u :
                    Bus.Allocate(invocation, state.D[0], "AssignResult", true);
            }
            if (invocation.Definition.Protect is not null)
            {
                Require(invocation.Allocations == 1 && state.D[0] == 24u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    $"Protect ReadArgs result allocation differs (count={invocation.Allocations}, size={state.D[0]}, flags={state.D[1]}).");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return invocation.Definition.Protect.AllocationFailure ? 0u :
                    Bus.Allocate(invocation, state.D[0], "Exec", true);
            }
            if (invocation.Definition.Touch is { } touch)
            {
                var bytes = invocation.Allocations == 0
                    ? 1318u : invocation.Allocations == 1 ? 12u : 0u;
                Require(bytes != 0 && state.D[0] == bytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "Touch workspace/result allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.AllocationFailure) return 0;
                var kind = bytes == 1318u ? "TouchWorkspace" : "Exec";
                var value = Bus.Allocate(invocation, state.D[0], kind, true);
                if (bytes == 1318u) invocation.TouchLayout!.Workspace = value;
                return value;
            }
            if (invocation.Definition.Date is not null)
            {
                var dateExpectedBytes = invocation.Allocations switch
                {
                    0 => 16u,  // ReadArgs result array
                    1 => 26u,  // DateTime
                    2 or 3 or 4 => 16u, // Day, Date, Time buffers
                    5 => 12u,  // VPrintf/VFPrintf argument vector
                    6 => 40u,  // timer.device request, setter path only
                    _ => 0u
                };
                Require(dateExpectedBytes != 0 && state.D[0] == dateExpectedBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "Date allocation shape differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.AllocationFailure) return 0;
                return Bus.Allocate(invocation, dateExpectedBytes, "Exec", true);
            }
            if (invocation.Definition.FileNote is not null)
            {
                var fileNoteBytes = invocation.Allocations == 0 ? 882u : 16u;
                Require(state.D[0] == fileNoteBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "FileNote allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.AllocationFailure) return 0;
                var value = Bus.Allocate(invocation, state.D[0], "Exec", true);
                if (invocation.Allocations == 1 && invocation.FileNoteLayout is { } fileNote)
                    fileNote.Control = value;
                return value;
            }
            if (invocation.Definition.Break is not null)
            {
                var expectedBreakBytes = suite == Workbench31BreakEntrySuite ? 24u : 28u;
                Require(invocation.Allocations == 0 && state.D[0] == expectedBreakBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "Break result allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return invocation.Definition.AllocationFailure ? 0 :
                    Bus.Allocate(invocation, expectedBreakBytes, "Exec", true);
            }
            if (invocation.Definition.Avail is not null)
            {
                var definition = invocation.Definition.Avail;
                var selection = definition.Chip || definition.Fast || definition.Total;
                var morphos = suite == MorphOSAvailEntrySuite;
                var availExpectedBytes = morphos
                    ? NativeMorphOSAvailCommand.ResultCount * 4u
                    : invocation.Allocations == 0 ? 16u : selection ? 4u : 20u;
                Require(state.D[0] == availExpectedBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "Avail allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, availExpectedBytes, "Exec", true);
            }
            if ((suite == MorphOSWaitEntrySuite || suite == Workbench31WaitEntrySuite) && invocation.Definition.Wait is not null)
            {
                var workspace = invocation.Definition.Wait.Until is not null &&
                    invocation.Allocations == 1;
                Require(invocation.Allocations == 0 || workspace,
                    "Wait allocation order differs.");
                var bytes = workspace ? 64u : 16u;
                Require(state.D[0] == bytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "Wait allocation shape differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.Wait.AllocationFailure && !workspace)
                    return 0;
                return Bus.Allocate(invocation, bytes, "Exec", true);
            }
            if (invocation.Definition.Wait is not null)
            {
                Require(invocation.Allocations == 0 && state.D[0] == 16u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "Wait result allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, 16, "Exec", true);
            }
            if (invocation.Definition.Lock is not null)
            {
                Require(invocation.Allocations == 0 && state.D[0] == 16u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "Lock result allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, 16, "Exec", true);
            }
            if (invocation.Definition.DiskChange is not null)
            {
                Require(invocation.Allocations == 0 && state.D[0] == 4u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                    "DiskChange result allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return Bus.Allocate(invocation, 4, "Exec", true);
            }
            if (invocation.Definition.CopyFileTransfer is { } transfer)
            {
                Require(state.D[0] == (2048u >> invocation.Allocations) &&
                    state.D[1] == (uint)Exec.MemoryFlags.Public, "Transfer allocation retry differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Allocations <= transfer.FailAlloc) return 0;
                var buffer = Bus.Allocate(invocation, state.D[0], "Exec", false);
                invocation.CopyFileTransferLayout!.Buffer = buffer;
                return buffer;
            }
            if(recursiveCommandRoot&&RecursiveHasLinkDevice(invocation.Definition.CopyTraversalWork!.SingleFailure)&&invocation.Allocations>=3)
            {
                Require(state.D[0]==512&&state.D[1]==(uint)Exec.MemoryFlags.Public&&invocation.Allocations<5,
                    "Recursive link probe/cache allocation differs.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if(invocation.Definition.CopyTraversalWork!.SingleFailure=="probe-buffer-failure"&&invocation.Allocations==4)
                {invocation.IoError=103;return 0;}
                return Bus.Allocate(invocation,512,"Exec",false);
            }
            var expectedBytes = invocation.Definition.Dir is not null ? 24u : invocation.Definition.DeleteCommand is not null ? invocation.Allocations == 0 ? 2600u : invocation.Allocations == 1 ? (IsWorkbenchDeleteCommandEntrySuite(suite) ? 16u : 20u) : 2330u : recursiveCommandRoot && invocation.Definition.CopyTraversalWork!.SingleFailure=="delete" ? invocation.Allocations==0?4772u:2330u : directoryCommandRoot ? invocation.Allocations==0?4772u:invocation.Allocations==1?(uint)(invocation.Definition.CopyTraversalWork!.TargetName.Length*2+3):(invocation.Allocations==2||invocation.Allocations>=4&&invocation.Allocations<invocation.Definition.CopyTraversalWork!.ExpectedSources+3)&&!invocation.Definition.CopyTraversalWork!.Stream?2330u:512u : singleCommandRoot ? invocation.Allocations==0?4772u:invocation.Allocations==1?21u:invocation.Allocations==2?(uint)(invocation.Definition.CopyTraversalWork!.Source.Length*2+3):512u : copyCommandRoot ? invocation.Allocations==0?4772u:suite==CopyParsedMakeDirectorySuite?19u:suite==CopyParsedDeleteSuite?invocation.Allocations==1&&invocation.Definition.CopyArgumentGate!.HasPattern?7u:2330u:512u : suite == CopySingleTargetSuite ? invocation.Allocations < 2 ? 21u : 512u : suite == CopyTargetDispatchSuite ? invocation.Allocations == 0 ? (uint)(invocation.Definition.CopyTraversalWork!.TargetName.Length*2+3) : invocation.Allocations == 1 ? 2330u : 512u : unifiedCopyRoot && invocation.Allocations == 1 ? 4658u : suite == CopyCompletionSuite ? 512u : invocation.Definition.CopyTraversalWork is not null ? (invocation.Allocations == 0 && !invocation.Definition.CopyTraversalWork.Stream ? 2330u : 512u) : invocation.Definition.CopyWork is not null ? invocation.Definition.CopyWork.Extended && invocation.Allocations==0 ? 16u : 512u : invocation.Definition.CopyLinkOperation is not null ? 2048u : invocation.Definition.CopyFileOperation is not null ? 512u : invocation.Definition.CopyTraversal is not null ? (uint)DosLayout.AnchorPath.Size + 2048u : invocation.Definition.ArgumentBoundary is { } boundary
                ? boundary.AllocationBytes : invocation.Definition.Eval is { } eval
                    ? invocation.Allocations == 0
                        ? eval.WorkbenchProfile ? 20u : 24u
                        : 4096u
                    : invocation.Definition.PathPart is not null
                        ? invocation.Allocations == 0 ? 12u : RequiredScratchBytes(
                            invocation.Definition.PathPart)
                        : invocation.Definition.QuoteEntry is not null
                            ? invocation.Allocations switch
                            {
                                0 => 32u,
                                1 => 4096u,
                                2 => 64u,
                                _ => 4096u
                            }
                    : invocation.Definition.Which is not null
                            ? invocation.Allocations == 0
                                ? (suite == MorphOSWhichEntrySuite ? 24u : 16u)
                                : 1024u
                            : invocation.Definition.CopyArgumentGate is not null
                                ? invocation.Definition.CopyArgumentGate.NormalKind is not null ? invocation.Allocations == 0 ? 96u : invocation.Allocations == 1 ? 2610u : suite == CopyParsedMakeDirectorySuite ? 19u : invocation.Allocations == 2 && invocation.Definition.CopyArgumentGate.HasPattern ? 7u : 2330u : invocation.Allocations == 0 ? 96u : invocation.Allocations == 1 ? 294u : 512u
                            : invocation.Definition.Relabel is { } relabel
                                ? invocation.Allocations == 0 ? 8u : (uint)relabel.Drive.Length
                            : invocation.Definition.MakeLink is not null
                                ? 16u
                            : invocation.Definition.WaitForPort is not null
                                ? 16u
                            : invocation.Definition.WaitForNotification is not null
                                ? invocation.Allocations == 0
                                    ? 12u
                                    : (uint)(invocation.Definition.WaitForNotification.Names.Length *
                                        DosLayout.NotifyRequest.Size)
                            : invocation.Definition.WaitForLib is not null
                                ? 12u
                            : invocation.Definition.TypeEntry is not null
                                ? invocation.Allocations == 0
                                    ? IsWorkbench31TypeNativeEntrySuite(suite) ? 20u : 24u
                                    : 17176u
                            : invocation.Definition.IconPos is not null
                                ? NativeWorkbench31IconPosCommand.ResultCount * 4u
                                : invocation.Definition.GuessBootDev is not null
                                    ? invocation.Allocations == 0 ? NativeWorkbench31GuessBootDevCommand.ResultCount * 4u : NativeWorkbench31GuessBootDevCommand.NameBufferBytes
                                : 8u;
            Require(expectedBytes is not null && state.D[0] == expectedBytes &&
                state.D[1] == (uint)(invocation.Definition.DeleteCommand is not null ? Exec.MemoryFlags.Public|Exec.MemoryFlags.Clear : recursiveCommandRoot && invocation.Definition.CopyTraversalWork!.SingleFailure=="delete" ? Exec.MemoryFlags.Public|Exec.MemoryFlags.Clear : directoryCommandRoot ? invocation.Allocations==1?Exec.MemoryFlags.Any:invocation.Allocations==0||(invocation.Allocations==2||invocation.Allocations>=4&&invocation.Allocations<invocation.Definition.CopyTraversalWork!.ExpectedSources+3)&&!invocation.Definition.CopyTraversalWork!.Stream?Exec.MemoryFlags.Public|Exec.MemoryFlags.Clear:Exec.MemoryFlags.Public : singleCommandRoot ? invocation.Allocations==0?Exec.MemoryFlags.Public|Exec.MemoryFlags.Clear:invocation.Allocations<3?Exec.MemoryFlags.Any:Exec.MemoryFlags.Public : copyCommandRoot ? invocation.Allocations==0?Exec.MemoryFlags.Public|Exec.MemoryFlags.Clear:suite==CopyParsedMakeDirectorySuite||suite==CopyParsedDeleteSuite&&invocation.Allocations==1&&invocation.Definition.CopyArgumentGate!.HasPattern?Exec.MemoryFlags.Any:suite==CopyParsedDeleteSuite?Exec.MemoryFlags.Public|Exec.MemoryFlags.Clear:Exec.MemoryFlags.Public : suite == CopySingleTargetSuite ? invocation.Allocations < 2 ? Exec.MemoryFlags.Any : Exec.MemoryFlags.Public : suite == CopyTargetDispatchSuite ? invocation.Allocations == 0 ? Exec.MemoryFlags.Any : invocation.Allocations == 1 ? Exec.MemoryFlags.Public|Exec.MemoryFlags.Clear : Exec.MemoryFlags.Public : suite == CopyParsedMakeDirectorySuite && invocation.Allocations > 1 ? Exec.MemoryFlags.Any : suite == CopyCompletionSuite ? Exec.MemoryFlags.Public : invocation.Definition.CopyArgumentGate is { NormalKind: not null, HasPattern: true } && invocation.Allocations == 2 ? Exec.MemoryFlags.Any : invocation.Definition.CopyArgumentGate?.DirectKind is not null && invocation.Allocations > 1 ? Exec.MemoryFlags.Public : invocation.Definition.CopyLinkOperation is not null ? Exec.MemoryFlags.Any : (invocation.Definition.CopyTraversalWork is not null && (invocation.Allocations > 0 || invocation.Definition.CopyTraversalWork.Stream)) || invocation.Definition.CopyWork is not null || invocation.Definition.CopySoftLink is not null || invocation.Definition.CopyFileOperation is not null ? Exec.MemoryFlags.Public : Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear),
                $"Unexpected allocation; compiler heap context is not qualified by this suite (expected {expectedBytes}, actual {state.D[0]}, allocation {invocation.Allocations}, suite {suite}).");
            invocation.Allocations++;
            invocation.AllocationRequests.Add(state.D[0]);
            if(copyCommandRoot && invocation.Definition.CopyArgumentGate!.WorkspaceFailure) return 0;
            if (invocation.Definition.PathPart?.ScratchAllocationFailure == true &&
                invocation.Allocations == 2) return 0;
            // The maximum-safe count case must never allocate huge host memory.
            if (invocation.Definition.AllocationFailure) return 0;
            var allocationKind = invocation.Definition.GuessBootDev is not null &&
                invocation.Allocations == 2 ? "GuessBootDevNameBuffer" : "Exec";
            return Bus.Allocate(invocation, state.D[0], allocationKind, true);
        });
        // RegisterRenameAllocationExec already owns AllocVec/FreeVec for this suite;
        // registering the generic pair as well collides on the same gateway vector.
        if (suite != WorkbenchRenameStartupSuite)
        Register(ExecBase, ExecLvo.AllocVec, "AllocVec", (state, invocation) =>
        {
            if (IsWorkbench31VersionEntrySuite(suite))
                return AllocateWorkbenchVersionBuffer(state, invocation);
            if (suite == AddDataTypesListEntrySuite)
            {
                var addDataTypesDefinition = invocation.Definition.AddDataTypesList!;
                var index = addDataTypesDefinition.AllocVecCalls++;
                if (index < 4)
                {
                    var nameLength = index switch
                    {
                        0 => 6u, 1 => 5u, 2 => 3u, 3 => 9u,
                        _ => 0u
                    };
                    Require(nameLength != 0 &&
                        state.D[0] == NativeMorphOSAddDataTypesCommand
                            .CompoundDataTypeSize + nameLength + 1 &&
                        state.D[1] == (uint)(Exec.MemoryFlags.Public |
                            Exec.MemoryFlags.Clear),
                        "AddDataTypes built-in allocation size/flags differ.");
                    return AddDataTypesAllocationBase + (uint)index * 0x100;
                }
                if (state.D[0] == 39)
                {
                    Require(index == 4 &&
                        state.D[1] == (uint)(Exec.MemoryFlags.Public |
                            Exec.MemoryFlags.Clear),
                        "AddDataTypes exclusion-pattern buffer allocation differs.");
                    Bus.Memory.AsSpan((int)AddDataTypesExclusion, 39).Clear();
                    return AddDataTypesExclusion;
                }
                if (state.D[0] == AnchorPath.Size)
                {
                    Require(index >= 5 && state.D[1] == (uint)(Exec.MemoryFlags.Public |
                            Exec.MemoryFlags.Clear),
                        "AddDataTypes AnchorPath allocation differs.");
                    Bus.Memory.AsSpan((int)AddDataTypesAnchor,
                        (int)AnchorPath.Size).Clear();
                    return AddDataTypesAnchor;
                }
                if (addDataTypesDefinition.ValidDtcd &&
                    state.D[0] == 8 && state.D[1] ==
                        (uint)(Exec.MemoryFlags.Public |
                            Exec.MemoryFlags.Clear))
                {
                    Require(index >= (invocation.Definition.Workbench ? 5 : 6) &&
                        addDataTypesDefinition.CodeCopyCalls == 0,
                        "AddDataTypes DTCD code allocation order differs.");
                    Bus.Memory.AsSpan((int)AddDataTypesCodeBuffer, 8).Clear();
                    return AddDataTypesCodeBuffer;
                }
                if (addDataTypesDefinition.ValidDtcd &&
                    state.D[0] == 12 && state.D[1] ==
                        (uint)(Exec.MemoryFlags.Public |
                            Exec.MemoryFlags.Clear))
                {
                    Require(index >= (invocation.Definition.Workbench ? 6 : 7),
                        "AddDataTypes loader-state allocation order differs.");
                    Bus.Memory.AsSpan((int)AddDataTypesLoaderState, 12).Clear();
                    return AddDataTypesLoaderState;
                }
                var headerCase = addDataTypesDefinition.ValidDthd;
                Require(headerCase && state.D[0] ==
                    NativeMorphOSAddDataTypesCommand
                        .CompoundDataTypeInlineHeader +
                        AddDataTypesFileHeaderBytes &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear),
                    "AddDataTypes descriptor allocation differs.");
                addDataTypesDefinition.DatatypeAllocationCalls++;
                Bus.Memory.AsSpan((int)AddDataTypesRegisteredCompound,
                    (int)state.D[0]).Clear();
                return AddDataTypesRegisteredCompound;
            }
            if (IsVersionEntrySuite(suite) &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                invocation.Definition.Version is { } searchVersion &&
                !searchVersion.System && !searchVersion.File &&
                !searchVersion.ResidentFound &&
                !searchVersion.ExecLibraryFound &&
                searchVersion.UtilityLibraryAvailable &&
                searchVersion.ParserError == 0)
            {
                var layout = invocation.VersionLayout!;
                var basenameLength = FilePartLength(searchVersion.Name);
                var expectedBytes = 12u + basenameLength + 5u;
                Require(state.D[0] == expectedBytes &&
                    state.D[1] == (uint)Exec.MemoryFlags.Public &&
                    layout.LibrarySearchPathBuffer == 0 &&
                    layout.LibrarySearchPathFreeCalls ==
                        layout.LibrarySearchPathAllocCalls,
                    "Version directory path-buffer allocation differs.");
                layout.LibrarySearchPathAllocCalls++;
                layout.LibrarySearchPathBytes = state.D[0];
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                layout.LibrarySearchPathBuffer = Bus.Allocate(invocation,
                    state.D[0], "VersionLibrarySearchPath", true);
                return layout.LibrarySearchPathBuffer;
            }
            if (IsVersionEntrySuite(suite) &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                invocation.Definition.Version is { } version &&
                version.AmbientRexxResult is { } ambientResult)
            {
                var layout = invocation.VersionLayout!;
                var expectedBytes = unchecked((uint)
                    Encoding.Latin1.GetByteCount(ambientResult) + 1);
                Require(state.D[0] == expectedBytes &&
                    state.D[1] == (uint)Exec.MemoryFlags.Any &&
                    layout.AmbientCopyAllocCalls == 0,
                    "Version Ambient response copy allocation differs.");
                layout.AmbientCopyAllocCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return version.AmbientCopyAllocationFailure ? 0u :
                    Bus.Allocate(invocation, state.D[0],
                        "VersionAmbientResult", true);
            }
            if (suite == ExtractKickstartEntrySuite)
            {
                var extractDefinition = invocation.Definition.ExtractKickstart!;
                Require(state.D[0] == NativeWorkbench31ExtractKickstartCommand.BufferBytes &&
                    state.D[1] == (uint)Exec.MemoryFlags.Any &&
                    invocation.ExtractKickstartAllocVecCalls == 0,
                    "ExtractKickstart buffer allocation differs.");
                invocation.ExtractKickstartAllocVecCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (extractDefinition.BufferAllocationFailure) return 0;
                var buffer = Bus.Allocate(invocation, state.D[0],
                    "ExtractKickstartBuffer", true);
                invocation.ExtractKickstartBuffer = buffer;
                return buffer;
            }
            if (suite == MorphOSAvailEntrySuite && invocation.Definition.Avail is not null)
            {
                Require(state.D[0] == 0x7ffffff0u &&
                    state.D[1] == (uint)Exec.MemoryFlags.Public,
                    "MorphOS Avail flush allocation differs.");
                // The fixture models the normal no-memory result without
                // reserving a host-sized block; the command treats NULL as a
                // harmless failed expunge probe.
                return 0;
            }
            if (invocation.Definition.RequestFile is not null)
            {
                Require(invocation.RequestFileAllocVecCalls == 0 &&
                    state.D[0] == 512u &&
                    state.D[1] == (uint)(Exec.MemoryFlags.Any | Exec.MemoryFlags.Clear),
                    "RequestFile buffer allocation differs.");
                invocation.RequestFileAllocVecCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                if (invocation.Definition.RequestFile.AllocationFailure) return 0u;
                var buffer = Bus.Allocate(invocation, state.D[0], "RequestFileBuffer", true);
                invocation.RequestFileBuffer = buffer;
                return buffer;
            }
            if (invocation.Definition.RequestChoice is { } requestChoice)
            {
                Require(state.D[1] == (uint)(Exec.MemoryFlags.Public |
                    Exec.MemoryFlags.Clear),
                    "RequestChoice text allocation flags differ.");
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return requestChoice.EasyFailure ? 0u :
                    Bus.Allocate(invocation, state.D[0], "RequestChoiceText", true);
            }
            if (invocation.Definition.Dir is { } dir)
            {
                var anchorBytes = (uint)(DosLayout.AnchorPath.Size + 512);
                var expectedFlags = state.D[0] == anchorBytes
                    ? (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear)
                    : (uint)Exec.MemoryFlags.Any;
                Require(state.D[0] != 0 && state.D[1] == expectedFlags,
                    "Dir workspace allocation differs.");
                invocation.DirAllocVecCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                var allocationFailure = dir.AllocationFailure;
                if (dir.FailAllocVecBytes == state.D[0])
                {
                    var occurrence = invocation.DirLayout!
                        .AllocVecSizeOccurrences.GetValueOrDefault(state.D[0]) + 1;
                    invocation.DirLayout.AllocVecSizeOccurrences[state.D[0]] =
                        occurrence;
                    if (occurrence == dir.FailAllocVecOccurrence)
                        allocationFailure = true;
                }
                if (allocationFailure) return 0u;
                var kind = state.D[0] == 1u ? "DirPath" :
                    state.D[0] == 8192u ? "DirBuffer" :
                    state.D[0] == anchorBytes ? "DirAnchor" : "DirPatternTokens";
                var address = Bus.Allocate(invocation, state.D[0], kind, true);
                invocation.DirLayout!.Vectors[address] = (kind, state.D[0]);
                invocation.DirLayout.AllocVecSuccesses++;
                if (kind == "DirPath") invocation.DirLayout.PathBuffer = address;
                return address;
            }
            if (invocation.Definition.DosList is { } dosList)
            {
                var bytes = dosList.Verbose ? 32768u : 4096u;
                Require(invocation.DosListAllocVecCalls == 0 &&
                    state.D[0] == bytes &&
                    state.D[1] == (uint)Exec.MemoryFlags.Any,
                    "DOSList snapshot allocation differs.");
                invocation.DosListAllocVecCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return dosList.AllocationFailure ? 0u :
                    Bus.Allocate(invocation, state.D[0],
                        "DosListBuffer", true);
            }
            if (invocation.Definition.Info is { } info)
            {
                var bytes = suite == Workbench31InfoEntrySuite ? 4096u : 8192u;
                Require(invocation.InfoAllocVecCalls == 0 && state.D[0] == bytes &&
                    state.D[1] == (uint)Exec.MemoryFlags.Any,
                    "Info snapshot allocation differs.");
                invocation.InfoAllocVecCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return info.AllocationFailure ? 0u :
                    Bus.Allocate(invocation, state.D[0], "InfoBuffer", true);
            }
            if (invocation.Definition.TaskList is not null)
            {
                var expectedBytes = invocation.TaskListAllocVecCalls switch
                {
                    0 => 131072u,
                    1 => 262144u,
                    _ => throw new InvalidOperationException(
                        "TaskList grew its snapshot buffer more than once in this fixture.")
                };
                Require(invocation.Allocations ==
                        invocation.TaskListAllocVecCalls + 1 &&
                    state.D[0] == expectedBytes &&
                    state.D[1] == (uint)Exec.MemoryFlags.Any,
                    $"TaskList snapshot allocation differs (bytes={state.D[0]}, flags={state.D[1]}, allocations={invocation.Allocations}).");
                invocation.TaskListAllocVecCalls++;
                invocation.Allocations++;
                invocation.AllocationRequests.Add(state.D[0]);
                return invocation.Definition.TaskList.AllocationFailure ? 0u :
                    Bus.Allocate(invocation, state.D[0], "TaskListBuffer", true);
            }
            if (invocation.Definition.ResList is { } resList)
            {
                Require(invocation.ResListAllocVecCalls == 0 &&
                    state.D[0] == 2048 && state.D[1] == (uint)Exec.MemoryFlags.Any,
                    "ResList initial buffer allocation differs.");
                invocation.ResListAllocVecCalls++;
                invocation.AllocationRequests.Add(state.D[0]);
                invocation.Allocations++;
                return resList.AllocationFailure ? 0 :
                    Bus.Allocate(invocation, state.D[0], "ResListBuffer", true);
            }
            if (invocation.Definition.LibList is { } libList)
            {
                Require(invocation.LibListAllocVecCalls == 0 &&
                    state.D[0] == 2048 && state.D[1] == (uint)Exec.MemoryFlags.Any,
                    "LibList initial buffer allocation differs.");
                invocation.LibListAllocVecCalls++;
                invocation.AllocationRequests.Add(state.D[0]);
                invocation.Allocations++;
                return libList.AllocationFailure ? 0 :
                    Bus.Allocate(invocation, state.D[0], "LibListBuffer", true);
            }
            if (invocation.Definition.DevList is { } devList)
            {
                Require(invocation.DevListAllocVecCalls == 0 &&
                    state.D[0] == 2048 && state.D[1] == (uint)Exec.MemoryFlags.Any,
                    "DevList initial buffer allocation differs.");
                invocation.DevListAllocVecCalls++;
                invocation.AllocationRequests.Add(state.D[0]);
                invocation.Allocations++;
                return devList.AllocationFailure ? 0 :
                    Bus.Allocate(invocation, state.D[0], "DevListBuffer", true);
            }
            if (invocation.Definition.PortList is { } portList)
            {
                Require(invocation.PortListAllocVecCalls == 0 &&
                    state.D[0] == 2048 && state.D[1] == (uint)Exec.MemoryFlags.Any,
                    "PortList initial buffer allocation differs.");
                invocation.PortListAllocVecCalls++;
                invocation.AllocationRequests.Add(state.D[0]);
                invocation.Allocations++;
                return portList.AllocationFailure ? 0 :
                    Bus.Allocate(invocation, state.D[0], "PortListBuffer", true);
            }
            if (invocation.Definition.ModList is { } modList)
            {
                Require(invocation.ModListAllocVecCalls == 0 &&
                    state.D[0] == 4096 && state.D[1] == (uint)Exec.MemoryFlags.Any,
                    "ModList initial buffer allocation differs.");
                invocation.ModListAllocVecCalls++;
                invocation.AllocationRequests.Add(state.D[0]);
                invocation.Allocations++;
                return modList.AllocationFailure ? 0 :
                    Bus.Allocate(invocation, state.D[0], "ModListBuffer", true);
            }
            if (invocation.Definition.Protect is { } protect)
            {
                Require(invocation.ProtectAllocVecCalls == 0 &&
                    state.D[0] == 794u && state.D[1] == 0x10000,
                    "Protect AnchorPath allocation differs.");
                invocation.ProtectAllocVecCalls++;
                invocation.AllocationRequests.Add(state.D[0]);
                invocation.Allocations++;
                return protect.AllocationFailure ? 0u :
                    Bus.Allocate(invocation, state.D[0], "ProtectAnchor", true);
            }
            var definition = invocation.Definition.SetDate;
            Require(definition is not null && invocation.Allocations == 1 &&
                state.D[0] == DosLayout.AnchorPath.Size && state.D[1] == 0x10001,
                "SetDate AnchorPath allocation differs.");
            invocation.SetDateAllocVecCalls++;
            invocation.Allocations++;
            invocation.AllocationRequests.Add(state.D[0]);
            if (definition!.AllocationFailure) return 0;
            var anchor = Bus.Allocate(invocation, DosLayout.AnchorPath.Size,
                "SetDateAnchor", true);
            invocation.SetDateLayout!.Anchor = anchor;
            return anchor;
        });
        if (suite != WorkbenchRenameStartupSuite)
        Register(ExecBase, ExecLvo.FreeVec, "FreeVec", (state, invocation) =>
        {
            if (IsWorkbench31VersionEntrySuite(suite))
                return FreeWorkbenchVersionBuffer(state, invocation);
            if (suite == AddDataTypesListEntrySuite)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                if (state.A[1] == AddDataTypesRegisteredCompound)
                {
                    definition.DatatypeFreeCalls++;
                    return 0;
                }
                if (state.A[1] == AddDataTypesExistingCompound)
                {
                    Require(definition.ExistingDatatypeFreeCalls == 0,
                        "AddDataTypes deleted its replaced descriptor twice.");
                    definition.ExistingDatatypeFreeCalls++;
                    definition.Events.Add("free-existing-descriptor");
                    return 0;
                }
                if (state.A[1] == AddDataTypesCodeBuffer)
                {
                    Require(definition.CodeBufferFreeCalls == 0,
                        "AddDataTypes freed the DTCD code buffer twice.");
                    definition.CodeBufferFreeCalls++;
                    definition.Events.Add("free-dtcd-code");
                    return 0;
                }
                if (state.A[1] == AddDataTypesLoaderState)
                {
                    Require(definition.LoaderStateFreeCalls == 0,
                        "AddDataTypes freed loader state twice.");
                    definition.LoaderStateFreeCalls++;
                    return 0;
                }
                if (state.A[1] == AddDataTypesAnchor)
                {
                    Require(definition.AnchorFreeCalls <
                        definition.MatchEndCalls,
                        "AddDataTypes freed its anchor before MatchEnd.");
                    definition.AnchorFreeCalls++;
                    definition.Events.Add("free-anchor");
                    return 0;
                }
                Require(state.A[1] == AddDataTypesExclusion &&
                    definition.ExclusionFreeCalls == 0,
                    "AddDataTypes freed an unowned matcher buffer.");
                definition.ExclusionFreeCalls++;
                definition.Events.Add("free-exclusion-pattern");
                return 0;
            }
            if (IsVersionEntrySuite(suite) &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                invocation.VersionLayout is { } searchLayout &&
                searchLayout.LibrarySearchPathBuffer != 0 &&
                state.A[1] == searchLayout.LibrarySearchPathBuffer)
            {
                Require(searchLayout.LibrarySearchPathFreeCalls <
                    searchLayout.LibrarySearchPathAllocCalls,
                    "Version directory path-buffer ownership differs.");
                Bus.Release(invocation, state.A[1],
                    "VersionLibrarySearchPath",
                    searchLayout.LibrarySearchPathBytes);
                searchLayout.LibrarySearchPathFreeCalls++;
                searchLayout.LibrarySearchPathBuffer = 0;
                return 0;
            }
            if (IsVersionEntrySuite(suite) &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                invocation.Definition.Version is { } version &&
                version.AmbientRexxResult is { } ambientResult)
            {
                var layout = invocation.VersionLayout!;
                var expectedBytes = unchecked((uint)
                    Encoding.Latin1.GetByteCount(ambientResult) + 1);
                Require(state.A[1] != 0 &&
                    layout.AmbientCopyAllocCalls == 1 &&
                    !version.AmbientCopyAllocationFailure &&
                    layout.AmbientCopyFreeCalls == 0,
                    "Version Ambient response copy release differs.");
                Bus.Release(invocation, state.A[1],
                    "VersionAmbientResult", expectedBytes);
                layout.AmbientCopyFreeCalls++;
                return 0;
            }
            if (suite == ExtractKickstartEntrySuite)
            {
                Require(invocation.ExtractKickstartAllocVecCalls == 1 &&
                    invocation.ExtractKickstartFreeVecCalls == 0,
                    "ExtractKickstart buffer release shape differs.");
                Bus.Release(invocation, state.A[1], "ExtractKickstartBuffer",
                    NativeWorkbench31ExtractKickstartCommand.BufferBytes);
                invocation.ExtractKickstartFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.RequestFile is not null)
            {
                Require(invocation.RequestFileAllocVecCalls == 1 &&
                    invocation.RequestFileFreeVecCalls == 0,
                    "RequestFile buffer ownership differs.");
                Bus.Release(invocation, state.A[1], "RequestFileBuffer", 512);
                invocation.RequestFileFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.RequestChoice is not null)
            {
                Bus.Release(invocation, state.A[1], "RequestChoiceText");
                return 0;
            }
            if (invocation.Definition.Dir is not null)
            {
                Require(invocation.DirFreeVecCalls < invocation.DirAllocVecCalls,
                    "Dir FreeVec ownership differs.");
                Require(invocation.DirLayout!.Vectors.Remove(state.A[1],
                    out var allocation), "Dir FreeVec pointer is not owned.");
                Bus.Release(invocation, state.A[1], allocation.Kind,
                    allocation.Size);
                invocation.DirFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.DosList is not null)
            {
                Require(invocation.DosListAllocVecCalls == 1 &&
                    invocation.DosListFreeVecCalls == 0,
                    "DOSList FreeVec ownership differs.");
                Bus.Release(invocation, state.A[1], "DosListBuffer");
                invocation.DosListFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.Info is not null)
            {
                Require(invocation.InfoAllocVecCalls == 1 &&
                    invocation.InfoFreeVecCalls == 0,
                    "Info FreeVec ownership differs.");
                Bus.Release(invocation, state.A[1], "InfoBuffer");
                invocation.InfoFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.ResList is not null)
            {
                Require(invocation.ResListAllocVecCalls == 1 &&
                    invocation.ResListFreeVecCalls == 0,
                    "ResList FreeVec ownership differs.");
                Bus.Release(invocation, state.A[1], "ResListBuffer");
                invocation.ResListFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.LibList is not null)
            {
                Require(invocation.LibListAllocVecCalls == 1 &&
                    invocation.LibListFreeVecCalls == 0,
                    "LibList FreeVec ownership differs.");
                Bus.Release(invocation, state.A[1], "LibListBuffer");
                invocation.LibListFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.DevList is not null)
            {
                Require(invocation.DevListAllocVecCalls == 1 &&
                    invocation.DevListFreeVecCalls == 0,
                    "DevList FreeVec ownership differs.");
                Bus.Release(invocation, state.A[1], "DevListBuffer");
                invocation.DevListFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.PortList is not null)
            {
                Require(invocation.PortListAllocVecCalls == 1 &&
                    invocation.PortListFreeVecCalls == 0,
                    "PortList FreeVec ownership differs.");
                Bus.Release(invocation, state.A[1], "PortListBuffer");
                invocation.PortListFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.ModList is not null)
            {
                Require(invocation.ModListAllocVecCalls == 1 &&
                    invocation.ModListFreeVecCalls == 0,
                    "ModList FreeVec ownership differs.");
                Bus.Release(invocation, state.A[1], "ModListBuffer");
                invocation.ModListFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.TaskList is not null)
            {
                Require(invocation.TaskListAllocVecCalls >
                    invocation.TaskListFreeVecCalls,
                    "TaskList FreeVec ownership differs.");
                Bus.Release(invocation, state.A[1], "TaskListBuffer");
                invocation.TaskListFreeVecCalls++;
                return 0;
            }
            if (invocation.Definition.Protect is not null)
            {
                Require(invocation.ProtectAllocVecCalls == 1 &&
                    invocation.ProtectFreeVecCalls == 0,
                    "Protect FreeVec ownership differs.");
                Bus.Release(invocation, state.A[1], "ProtectAnchor");
                invocation.ProtectFreeVecCalls++;
                return 0;
            }
            Require(invocation.Definition.SetDate is not null &&
                invocation.SetDateAllocVecCalls == 1 &&
                invocation.SetDateFreeVecCalls == 0,
                "SetDate FreeVec ownership differs.");
            Bus.Release(invocation, state.A[1], "SetDateAnchor");
            invocation.SetDateFreeVecCalls++;
            return 0;
        });
        Register(ExecBase, ExecLvo.FreeMem, "FreeMem", (state, invocation) =>
        {
            if (addDataTypesCallbackProbe)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                Require(definition.CallbackAllocCalls == 1 &&
                    definition.CallbackFreeCalls == 0 && state.D[0] == 32 &&
                    state.A[1] == definition.CallbackAllocationAddress,
                    "AddDataTypes free callback address/size differs.");
                Bus.Release(invocation, state.A[1], "AddDataTypesCallback",
                    state.D[0]);
                definition.CallbackFreeCalls++;
                invocation.FreeMem++;
                return 0;
            }
            if (suite == AddDataTypesListEntrySuite)
            {
                var definition = invocation.Definition.AddDataTypesList!;
                var resultBytes = (definition.Workbench31Profile
                    ? NativeWorkbench31AddDataTypesCommand.ResultCount
                    : NativeMorphOSAddDataTypesCommand.CliResultCount) * 4;
                Require(definition.ResultFreeCalls == 0 &&
                    definition.ResultAllocationCalls == 1 &&
                    !definition.ResultAllocationFailure &&
                    state.D[0] == resultBytes &&
                    state.A[1] == definition.ResultArrayAddress,
                    "AddDataTypes ReadArgs result array release differs.");
                Bus.Release(invocation, state.A[1], "Exec", state.D[0]);
                definition.ResultFreeCalls++;
                invocation.FreeMem++;
                invocation.IoError = 902;
                return 0xf4ee;
            }
            if (invocation.Definition.Version is not null)
            {
                var layout = invocation.VersionLayout!;
                var versionKind = state.D[0] switch
                {
                    28u when !IsWorkbench31VersionEntrySuite(suite) => "Exec",
                    32u when IsWorkbench31VersionEntrySuite(suite) => "Exec",
                    16_385u when state.A[1] == layout.AmbientFileBuffer =>
                        "VersionAmbientFileBuffer",
                    16_449u when state.A[1] == layout.AmbientFileScratch =>
                        "VersionAmbientFileScratch",
                    _ when state.A[1] == layout.AmbientResidentText &&
                        state.D[0] == layout.AmbientResidentTextBytes =>
                        "VersionAmbientResidentText",
                    16_385u when state.A[1] == layout.FileBuffer =>
                        "VersionFileBuffer",
                    16_449u
                        when state.A[1] == layout.ScratchBuffer =>
                        "VersionScanBuffer",
                    16_449u
                        when state.A[1] == layout.SystemVersionScratch =>
                        "VersionSystemScratch",
                    16_449u
                        when state.A[1] == layout.ResidentVersionScratch =>
                        "VersionResidentScratch",
                    16_449u
                        when state.A[1] == layout.CommandSegmentScratch =>
                        "VersionCommandSegmentScratch",
                    16_449u
                        when state.A[1] ==
                            layout.ExecLibraryVersionScratch =>
                        "VersionExecLibraryScratch",
                    16_449u
                        when state.A[1] ==
                            layout.ExecDeviceVersionScratch =>
                        "VersionExecDeviceScratch",
                    96u when state.A[1] == layout.Md5Context =>
                        "VersionMd5Context",
                    _ when state.D[0] == layout.SystemVersionTextBytes &&
                        state.A[1] == layout.SystemVersionText =>
                        "VersionSystemText",
                    _ when state.D[0] ==
                        layout.ResidentVersionTextBytes &&
                        state.A[1] == layout.ResidentVersionText =>
                        "VersionResidentText",
                    _ when state.D[0] ==
                        layout.CommandSegmentVersionTextBytes &&
                        state.A[1] == layout.CommandSegmentText =>
                        "VersionCommandSegmentText",
                    _ when state.D[0] ==
                        layout.ExecLibraryVersionTextBytes &&
                        state.A[1] == layout.ExecLibraryVersionText =>
                        "VersionExecLibraryText",
                    _ when state.D[0] ==
                        layout.ExecDeviceVersionTextBytes &&
                        state.A[1] == layout.ExecDeviceVersionText =>
                        "VersionExecDeviceText",
                    _ when state.D[0] ==
                        layout.ExecLibraryNameCopyBytes &&
                        state.A[1] == layout.ExecLibraryNameCopy =>
                        "VersionExecLibraryName",
                    _ when state.D[0] == layout.ExecDeviceNameCopyBytes &&
                        state.A[1] == layout.ExecDeviceNameCopy =>
                        "VersionExecDeviceName",
                    _ => ""
                };
                Require(versionKind.Length != 0,
                    "Version freed an unexpected allocation.");
                Bus.Release(invocation, state.A[1], versionKind, state.D[0]);
                invocation.VersionFreeMemCalls++;
                invocation.FreeMem++;
                if (state.A[1] == layout.AmbientFileBuffer ||
                    state.A[1] == layout.AmbientFileScratch)
                    layout.AmbientFileFreeMemCalls++;
                if (state.A[1] == layout.AmbientResidentText)
                    layout.AmbientResidentTextFreeCalls++;
                invocation.IoError = 903;
                return 0xf4ee;
            }
            if (invocation.Definition.SetFont is not null)
            {
                var fontKind = state.D[0] switch
                {
                    SetFontResultBytes => "SetFontResults",
                    SetFontNameBytes => "SetFontName",
                    SetFontTextAttrBytes => "SetFontTextAttr",
                    SetFontInfoDataBytes => "SetFontInfoData",
                    _ => ""
                };
                Require(fontKind.Length != 0, "SetFont freed an unexpected allocation size.");
                Bus.Release(invocation, state.A[1], fontKind, state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            if (invocation.Definition.SetKeyboard is not null)
            {
                var keyboardKind = state.D[0] == 256u ? "SetKeyboardPath" : "Exec";
                Bus.Release(invocation, state.A[1], keyboardKind, state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            if (invocation.Definition.BindDrivers is not null)
            {
                Require(state.D[0] == NativeMorphOSBindDriversCommand.WorkspaceBytes &&
                    invocation.FreeMem == 0,
                    "BindDrivers workspace release differs.");
                Bus.Release(invocation, state.A[1], "BindDriversWorkspace",
                    state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            if (invocation.Definition.LoadMonDrvs is not null)
            {
                var loadKind = state.D[0] switch
                {
                    NativeMorphOSLoadMonDrvsCommand.WorkspaceBytes => "LoadMonDrvsWorkspace",
                    NativeMorphOSLoadMonDrvsCommand.ResultCount * 4u => "LoadMonDrvsResults",
                    _ => ""
                };
                Require(loadKind.Length != 0, "LoadMonDrvs freed an unexpected allocation size.");
                Bus.Release(invocation, state.A[1], loadKind, state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            if (invocation.Definition.Format is not null)
            {
                Bus.Release(invocation, state.A[1], "Exec", state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            if (invocation.Definition.Mount is not null)
            {
                var resultBytes = suite == Workbench31MountEntrySuite
                    ? NativeWorkbench31MountCommand.ResultCount * 4u
                    : NativeMorphOSMountCommand.ResultCount * 4u;
                Require(state.D[0] == resultBytes &&
                    invocation.FreeMem == 0,
                    "Mount result release shape differs.");
                Bus.Release(invocation, state.A[1], "Exec", state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            if (suite == ExtractKickstartEntrySuite)
            {
                Require(state.D[0] == NativeWorkbench31ExtractKickstartCommand.ResultCount * 4u &&
                    invocation.FreeMem == 0,
                    "ExtractKickstart result release shape differs.");
                Bus.Release(invocation, state.A[1], "Exec", state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            if (invocation.Definition.RequestFile is not null)
            {
                var resultBytes = suite == Workbench31RequestFileEntrySuite
                    ? NativeWorkbench31RequestFileCommand.ResultCount * 4u
                    : NativeMorphOSRequestFileCommand.ResultCount * 4u;
                var release = invocation.RequestFileFreeMemCalls++;
                Require(release == 0 &&
                    state.D[0] == resultBytes,
                    "RequestFile result/state free shape differs.");
                Bus.Release(invocation, state.A[1], "Exec", state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            if (invocation.Definition.Join is not null)
            {
                var layout = invocation.JoinLayout!;
                var bytes = state.D[0];
                var joinKind = bytes == NativeMorphOSJoinCommand.WorkspaceAllocationBytes
                    ? "JoinWorkspace" : bytes == NativeMorphOSJoinCommand.ResultCount * 4u
                    ? "JoinResults" : "JoinBuffer";
                Bus.Release(invocation, state.A[1], joinKind, bytes);
                layout.FreeMemCalls++;
                return 0;
            }
            if (invocation.Definition.Assign is not null)
            {
                var layout = invocation.AssignLayout!;
                Require(state.D[0] == (IsWorkbench31AssignEntrySuite(suite)
                    ? NativeWorkbench31AssignCommand.ResultCount
                    : NativeMorphOSAssignCommand.ResultCount) * 4u &&
                    layout.FreeMemCalls == 0,
                    "Assign result free shape differs.");
                Bus.Release(invocation, state.A[1], "AssignResult", state.D[0]);
                layout.FreeMemCalls++;
                invocation.IoError = 902;
                return 0xf4ee;
            }
            if (invocation.Definition.MakeDir is not null)
            {
                var layout = invocation.MakeDirLayout!;
                var makeDirKind = state.D[0] == FileInfoBlock.SizeInBytes
                    ? "MakeDirFib" : "MakeDirResult";
                Bus.Release(invocation, state.A[1], makeDirKind, state.D[0]);
                layout.FreeMemCalls++;
                invocation.IoError = 902;
                return 0xf4ee;
            }
            if (invocation.Definition.Search is not null)
            {
                var layout = invocation.SearchEntryLayout!;
                var searchKind = state.D[0] ==
                    NativeMorphOSSearchCommand.WorkspaceAllocationBytes
                        ? "SearchWorkspace"
                        : state.A[1] == layout.FileBuffer
                            ? "SearchFileBuffer" : "Exec";
                Bus.Release(invocation, state.A[1], searchKind, state.D[0]);
                invocation.SearchFreeMemCalls++;
                return 0;
            }
            if (invocation.Definition.List is not null)
            {
                var listKind = state.D[0] == NativeMorphOSListCommand.WorkspaceAllocationBytes
                    ? "ListWorkspace" : "ListResults";
                Bus.Release(invocation, state.A[1], listKind, state.D[0]);
                invocation.ListFreeMemCalls++;
                return 0;
            }
            if (invocation.Definition.DosList is not null)
            {
                Require(state.D[0] == 24u && invocation.DosListFreeMemCalls == 0,
                    "DOSList result free differs.");
                Bus.Release(invocation, state.A[1], "DosListResults", 24);
                invocation.DosListFreeMemCalls++;
                return 0;
            }
            if (invocation.Definition.DiskFree is not null)
            {
                var diskFreeKind = state.D[0] == InfoData.Size
                    ? "DiskFreeInfo" : "Exec";
                Bus.Release(invocation, state.A[1], diskFreeKind, state.D[0]);
                invocation.DiskFreeFreeMemCalls++;
                return 0;
            }
            if (invocation.Definition.Info is not null)
            {
                var infoKind = state.D[0] == InfoData.Size ? "InfoData" : "Exec";
                Bus.Release(invocation, state.A[1], infoKind, state.D[0]);
                invocation.InfoFreeMemCalls++;
                return 0;
            }
            if (invocation.Definition.SetClock is not null)
            {
                var setClockKind = state.D[0] == 8 ? "SetClockTimeval" : "Exec";
                Bus.Release(invocation, state.A[1], setClockKind, state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            if (invocation.Definition.Touch is not null)
            {
                var touchKind = state.D[0] == 1318u ? "TouchWorkspace" : "Exec";
                Bus.Release(invocation, state.A[1], touchKind, state.D[0]);
                invocation.FreeMem++;
                invocation.IoError = 902;
                return 0xf4ee;
            }
            if (invocation.Definition.WaitForNotification is { } waitForNotification)
            {
                var expectedRequestBytes = (uint)(waitForNotification.Names.Length *
                    DosLayout.NotifyRequest.Size);
                Require(state.D[0] == 12u || state.D[0] == expectedRequestBytes,
                    "WaitForNotification FreeMem size differs.");
                Bus.Release(invocation, state.A[1], "Exec", state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            if (invocation.Definition.GuessBootDev is not null)
            {
                var guessKind = state.D[0] == NativeWorkbench31GuessBootDevCommand.ResultCount * 4u
                    ? "Exec" : "GuessBootDevNameBuffer";
                var expected = guessKind == "Exec"
                    ? NativeWorkbench31GuessBootDevCommand.ResultCount * 4u
                    : NativeWorkbench31GuessBootDevCommand.NameBufferBytes;
                Require(state.D[0] == expected,
                    "GuessBootDev FreeMem size differs.");
                Bus.Release(invocation, state.A[1], guessKind, state.D[0]);
                invocation.FreeMem++;
                return 0;
            }
            var kind = invocation.Definition.SetDate is not null && state.D[0] == DosLayout.DateTime.Size
                ? "SetDateDateTime" : invocation.Definition.SetDate is not null &&
                    suite == MorphOSSetDateEntrySuite && state.D[0] == DosLayout.AnchorPath.Size
                    ? "SetDateAnchor" : "Exec";
            Bus.Release(invocation, state.A[1], kind, state.D[0]);
            invocation.FreeMem++;
            invocation.IoError = 902; // Deliberately poison cleanup to check error preservation.
            return 0xf4ee;
        });
        Register(ExecBase, ExecLvo.WaitPort, "WaitPort", (state, invocation) =>
        {
            if (IsVersionEntrySuite(suite) &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                !invocation.Definition.Workbench)
            {
                var layout = invocation.VersionLayout!;
                Require(state.A[0] == layout.AmbientReplyPort &&
                    layout.AmbientPutCalls == 1 &&
                    layout.AmbientWaitCalls == 0,
                    "Version Ambient reply wait differs.");
                layout.AmbientWaitCalls++;
                return layout.AmbientReplyPort;
            }
            Require(invocation.Definition.Workbench && state.A[0] == invocation.Port, "Wrong Workbench port.");
            Require(invocation.WaitPorts == 0 && invocation.GetMessages == 0 && invocation.Replies == 0,
                "Workbench startup port was waited after its message was consumed or already waited.");
            invocation.WaitPorts++;
            return invocation.Message;
        });
        Register(ExecBase, ExecLvo.GetMsg, "GetMsg", (state, invocation) =>
        {
            if (IsVersionEntrySuite(suite) &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                !invocation.Definition.Workbench)
            {
                var layout = invocation.VersionLayout!;
                Require(state.A[0] == layout.AmbientReplyPort &&
                    layout.AmbientWaitCalls == 1 &&
                    layout.AmbientGetCalls == 0,
                    "Version Ambient reply receive differs.");
                layout.AmbientGetCalls++;
                return layout.AmbientHostMessage;
            }
            Require(invocation.Definition.Workbench && state.A[0] == invocation.Port, "Wrong Workbench message port.");
            Require(invocation.WaitPorts == 1 && invocation.GetMessages == 0 && invocation.Replies == 0,
                "Workbench message consumed without WaitPort or consumed repeatedly.");
            invocation.GetMessages++;
            return invocation.Message;
        });
        Register(ExecBase, ExecLvo.Forbid, "Forbid", (_, invocation) =>
        {
            Require(!invocation.Forbidden, "Repeated Forbid.");
            invocation.Forbidden = true;
            if (suite == TaskListEntrySuite)
            {
                invocation.TaskListForbidCalls++;
                if (invocation.Definition.TaskList!.RegisterCheck &&
                    invocation.TaskListVPrintfCalls > 0)
                    invocation.TaskListRegisterCheckForbidCalls++;
            }
            if (suite == ResListEntrySuite) invocation.ResListForbidCalls++;
            if (suite == LibListEntrySuite) invocation.LibListForbidCalls++;
            if (suite == DevListEntrySuite) invocation.DevListForbidCalls++;
            if (suite == PortListEntrySuite) invocation.PortListForbidCalls++;
            if (suite == ModListEntrySuite) invocation.ModListForbidCalls++;
            if (suite == Workbench31SetFontEntrySuite) invocation.SetFontForbidCalls++;
            if (IsVersionEntrySuite(suite) &&
                !invocation.Definition.Workbench)
                invocation.VersionForbidCalls++;
            return 0;
        });
        Register(ExecBase, ExecLvo.Permit, "Permit", (state, invocation) =>
        {
            if (suite != WorkbenchWhichEntrySuite && suite != MorphOSWhichEntrySuite && suite != BreakEntrySuite &&
                suite != Workbench31BreakEntrySuite &&
                suite != ChangeTaskPriEntrySuite &&
                suite != Workbench31ChangeTaskPriEntrySuite &&
                suite != Workbench31SetFontEntrySuite &&
                suite != MorphOSAvailEntrySuite &&
                suite != MorphOSSetKeyboardEntrySuite &&
                suite != TaskListEntrySuite &&
                suite != ResListEntrySuite && suite != LibListEntrySuite &&
                suite != DevListEntrySuite && suite != PortListEntrySuite &&
                suite != ModListEntrySuite && !IsVersionEntrySuite(suite))
                throw new InvalidOperationException("Command called Permit after startup reply.");
            Require(invocation.Forbidden, "Which called Permit without a preceding Forbid.");
            invocation.Forbidden = false;
            if (suite == TaskListEntrySuite)
            {
                invocation.TaskListPermitCalls++;
                if (invocation.Definition.TaskList!.RegisterCheck &&
                    invocation.TaskListVPrintfCalls > 0)
                    invocation.TaskListRegisterCheckPermitCalls++;
            }
            if (suite == ResListEntrySuite) invocation.ResListPermitCalls++;
            if (suite == LibListEntrySuite) invocation.LibListPermitCalls++;
            if (suite == DevListEntrySuite) invocation.DevListPermitCalls++;
            if (suite == PortListEntrySuite) invocation.PortListPermitCalls++;
            if (suite == ModListEntrySuite) invocation.ModListPermitCalls++;
            if (suite == Workbench31SetFontEntrySuite)
            {
                Require(invocation.SetFontSetFontCalls == 1 &&
                    Bus.Long(invocation.SetFontWindow + SetFontWindowFontOffset) ==
                        invocation.SetFontFont,
                    "SetFont permitted before publishing the new console font.");
                invocation.SetFontPermitCalls++;
            }
            if (IsVersionEntrySuite(suite) &&
                !invocation.Definition.Workbench)
                invocation.VersionPermitCalls++;
            if (IsWorkbench31VersionEntrySuite(suite))
            {
                // Exec functions may clobber volatile address registers.
                // Force clients to retain list-search results explicitly
                // across the Permit boundary.
                state.A[0] = 0;
                state.A[1] = 0;
                state.D[1] = 0;
            }
            return 0;
        });
        Register(ExecBase, ExecLvo.Signal, "Signal", (state, invocation) =>
        {
            Require((suite == BreakEntrySuite || suite == Workbench31BreakEntrySuite) && invocation.Forbidden,
                "Signal was not protected by Forbid.");
            Require(state.A[1] != 0 && state.D[0] != 0,
                "Break signalled a null task or empty mask.");
            invocation.BreakSignals++;
            invocation.BreakSignalMask = state.D[0];
            return 0;
        });
        Register(ExecBase, ExecLvo.SetTaskPri, "SetTaskPri", (state, invocation) =>
        {
            Require((suite == ChangeTaskPriEntrySuite || suite == Workbench31ChangeTaskPriEntrySuite) && invocation.Forbidden &&
                state.A[1] != 0, "SetTaskPri was not protected or targeted.");
            var definition = invocation.Definition.ChangeTaskPri!;
            var priority = unchecked((sbyte)state.D[0]);
            Require(priority == definition.Priority,
                "ChangeTaskPri priority ABI differs.");
            invocation.ChangeTaskPriCalls++;
            invocation.ChangeTaskPriPriority = priority;
            return unchecked((uint)(sbyte)0);
        });
        Register(ExecBase, ExecLvo.FindName, "FindName", (state, invocation) =>
        {
            if (suite == WaitForLibEntrySuite)
            {
                var definition = invocation.Definition.WaitForLib!;
                var execBase = APTR.FromPointer(Bus.Long(4));
                var expectedList = APTR.FromPointer(execBase.Raw +
                    (uint)ExecLayout.ExecBase.LibraryList);
                Require(state.A[0] == expectedList.Raw &&
                    Bus.CString(state.A[1]) == definition.LibraryName,
                    "WaitForLib library-list lookup ABI differs.");
                invocation.WaitForLibFindCalls++;
                return definition.LibraryFound ? invocation.Arguments + 0x740 : 0;
            }
            throw new InvalidOperationException("FindName used outside WaitForLib.");
        });
        Register(ExecBase, ExecLvo.FindPort, "FindPort", (state, invocation) =>
        {
            if (IsVersionEntrySuite(suite) &&
                !IsWorkbench31VersionEntrySuite(suite) &&
                !invocation.Definition.Workbench)
            {
                var layout = invocation.VersionLayout!;
                Require(invocation.Forbidden &&
                    Bus.CString(state.A[1]) == "AMBIENT" &&
                    layout.AmbientPortLookups == 0,
                    "Version Ambient ARexx port lookup differs.");
                layout.AmbientPortLookups++;
                return invocation.Definition.Version!.AmbientRexxPortAvailable
                    ? layout.AmbientPort : 0;
            }
            if (suite == WaitForPortEntrySuite)
            {
                var portDefinition = invocation.Definition.WaitForPort!;
                Require(Bus.CString(state.A[1]) == portDefinition.PortName,
                    "WaitForPort name lookup ABI differs.");
                var check = invocation.WaitForPortFindCalls++;
                var present = portDefinition.PortFound &&
                    (!portDefinition.DisappearAfter.HasValue ||
                     check < portDefinition.DisappearAfter.Value);
                return present ? invocation.Arguments + 0x700 : 0;
            }
            Require(suite == BreakEntrySuite && invocation.Forbidden,
                "Break FindPort was not protected by Forbid.");
            var definition = invocation.Definition.Break!;
            invocation.BreakFindPortCalls++;
            Require(definition.Port is not null &&
                Bus.CString(state.A[1]) == definition.Port,
                "Break port lookup ABI differs.");
            if (!definition.PortFound)
                return 0;
            return invocation.Arguments + 0x700;
        });
        Register(ExecBase, ExecLvo.ReplyMsg, "ReplyMsg", (state, invocation) =>
        {
            Require(invocation.Definition.Workbench && invocation.Forbidden && state.A[1] == invocation.Message &&
                invocation.WaitPorts == 1 && invocation.GetMessages == 1 && invocation.Replies == 0,
                "Wrong, repeated, or unsafe Workbench reply without owned startup message.");
            invocation.Replies++;
            return 0;
        });
    }

    private void RegisterWorkbench31AvailEntryExec()
    {
        Register(ExecBase, ExecLvo.AvailMem, "AvailMem", (state, invocation) =>
        {
            var definition = invocation.Definition.Avail!;
            var request = (Exec.MemoryFlags)state.D[1];
            var classFlags = request & (Exec.MemoryFlags.Chip | Exec.MemoryFlags.Fast);
            var (available, maximum, largest) = classFlags switch
            {
                Exec.MemoryFlags.Chip => (definition.ChipAvailable,
                    definition.ChipMaximum, definition.ChipLargest),
                Exec.MemoryFlags.Fast => (definition.FastAvailable,
                    definition.FastMaximum, definition.FastLargest),
                Exec.MemoryFlags.Any => (definition.TotalAvailable,
                    definition.TotalMaximum, definition.TotalLargest),
                _ => throw new InvalidOperationException("Unexpected AvailMem class.")
            };
            var value = (request & Exec.MemoryFlags.Largest) != 0 ? largest :
                (request & Exec.MemoryFlags.Total) != 0 ? maximum : available;
            return value;
        });
    }

    private void RegisterWorkbench31DateEntryExec()
    {
        Register(ExecBase, ExecLvo.OpenDevice, "OpenDevice", (state, invocation) =>
        {
            var definition = invocation.Definition.Date!;
            Require(state.A[0] != 0 && Bus.CString(state.A[0]) == "timer.device" &&
                state.D[0] == 0 && state.D[1] == 0 && state.A[1] != 0,
                "Date timer.device OpenDevice ABI differs.");
            invocation.TimerOpenCalls++;
            return definition.TimerOpenFailure ? 1u : 0u;
        });
        Register(ExecBase, ExecLvo.DoIO, "DoIO", (state, invocation) =>
        {
            Require(state.A[1] != 0 &&
                Bus.Word(state.A[1] + (uint)ExecLayout.IORequest.Command) == 11,
                "Date timer request command differs.");
            invocation.TimerDoIoCalls++;
            Bus.Memory[state.A[1] + (uint)ExecLayout.IORequest.Error] = 0;
            return 0;
        });
        Register(ExecBase, ExecLvo.CloseDevice, "CloseDevice", (state, invocation) =>
        {
            Require(state.A[1] != 0 && invocation.TimerOpenCalls == 1,
                "Date closed an unowned timer.device request.");
            invocation.TimerCloseCalls++;
            return 0;
        });
        Register(DateUtilityBase, UtilityLvo.UMult32, "UMult32",
            (state, invocation) =>
            {
                Require(invocation.Definition.Date is not null,
                    "UMult32 used outside Date.");
                return unchecked(state.D[0] * state.D[1]);
            });
        Register(DateUtilityBase, UtilityLvo.UDivMod32, "UDivMod32",
            (state, invocation) =>
            {
                Require(invocation.Definition.Date is not null && state.D[1] != 0,
                    "UDivMod32 used outside Date or with zero divisor.");
                return state.D[0] / state.D[1];
            });
    }

    private void RegisterFindTaskByPidGateway()
    {
        Bus.Long(ExecBase - 994u, FindTaskByPidGateway);
        Bus.RegisterGateway(FindTaskByPidGateway, state =>
        {
            var invocation = Bus.Current ?? throw new InvalidOperationException(
                "FindTaskByPID called without a process.");
            Require(state.A[0] == ExecBase && invocation.Forbidden,
                "FindTaskByPID indirect ABI was not protected or supplied ExecBase in A0.");
            if (invocation.Definition.Break is { } breakDefinition)
            {
                Require(suite == BreakEntrySuite &&
                    breakDefinition.Process is uint value && value != 0 &&
                    state.D[0] == value,
                    "Break FindTaskByPID was not targeted.");
                invocation.BreakFindTaskByPIDCalls++;
                invocation.Events.Add("FindTaskByPID");
                state.D[0] = breakDefinition.PidFound ? invocation.Process + 0x900 : 0;
            }
            else
            {
                var changeDefinition = invocation.Definition.ChangeTaskPri!;
                Require(suite == ChangeTaskPriEntrySuite &&
                    changeDefinition.Process is uint changeValue &&
                    state.D[0] == changeValue,
                    "ChangeTaskPri FindTaskByPID was not targeted.");
                invocation.ChangeTaskPriFindTaskByPIDCalls++;
                invocation.Events.Add("FindTaskByPID");
                state.D[0] = changeDefinition.PidFound ?
                    invocation.Process + 0x900 : 0;
            }
            state.D[1] = 0xd1d1d1d1;
            state.A[0] = 0xa0a0a0a0;
            state.A[1] = 0xa1a1a1a1;
        });
    }

    private void RegisterDos(uint baseAddress)
    {
        if (suite == Workbench31SetFontEntrySuite)
        {
            RegisterSetFontEntryDos(baseAddress);
            return;
        }
        if (suite == Workbench31SetKeyboardEntrySuite)
        {
            RegisterSetKeyboardEntryDos(baseAddress);
            return;
        }
        if (suite == MorphOSSetKeyboardEntrySuite)
        {
            RegisterMorphOSSetKeyboardEntryDos(baseAddress);
            return;
        }
        if (suite == ExtractKickstartEntrySuite)
        {
            RegisterExtractKickstartEntryDos(baseAddress);
            return;
        }
        if (suite == MorphOSRenameSuite)
        {
            RegisterMorphOSRenameDos(baseAddress);
            return;
        }
        if (suite == AddDataTypesListEntrySuite)
        {
            RegisterAddDataTypesListDos(baseAddress);
            return;
        }
        if (suite == WorkbenchRenameStartupSuite)
        {
            RegisterWorkbenchRenameStartupDos(baseAddress);
            return;
        }
        if (suite == NativeIoSuite)
        {
            RegisterIoDos(baseAddress);
            return;
        }
        if (suite == EvalEntrySuite || suite == WorkbenchEvalEntrySuite)
        {
            RegisterEvalEntryDos(baseAddress);
            return;
        }
        if (suite == PathPartEntrySuite)
        {
            RegisterPathPartEntryDos(baseAddress);
            return;
        }
        if (IsWhichEntrySuite(suite))
        {
            RegisterWorkbenchWhichEntryDos(baseAddress);
            return;
        }
        if (suite == QuoteNativeEntrySuite)
        {
            RegisterQuoteNativeEntryDos(baseAddress);
            return;
        }
        if (suite == TypeTextIoProbeSuite)
        {
            RegisterTypeTextIoDos(baseAddress);
            return;
        }
        if (IsTypeNativeEntrySuite(suite))
        {
            RegisterTypeNativeEntryDos(baseAddress);
            return;
        }
        if (IsSearchEntrySuite(suite))
        {
            RegisterSearchEntryDos(baseAddress);
            return;
        }
        if (suite == DosListTraversalProbeSuite)
        {
            RegisterDosListTraversalDos(baseAddress);
            return;
        }
        if (suite == DosListEntrySuite)
        {
            RegisterDosListEntryDos(baseAddress);
            return;
        }
        if (suite is ListEntrySuite or WorkbenchListEntrySuite)
        {
            RegisterListEntryDos(baseAddress);
            return;
        }
        if (IsFileNoteEntrySuite(suite))
        {
            RegisterFileNoteEntryDos(baseAddress);
            return;
        }
        if (suite == BreakEntrySuite)
        {
            RegisterBreakEntryDos(baseAddress);
            return;
        }
        if (suite == Workbench31BreakEntrySuite)
        {
            RegisterBreakEntryDos(baseAddress);
            return;
        }
        if (suite == ChangeTaskPriEntrySuite)
        {
            RegisterChangeTaskPriEntryDos(baseAddress);
            return;
        }
        if (suite == Workbench31ChangeTaskPriEntrySuite)
        {
            RegisterChangeTaskPriEntryDos(baseAddress);
            return;
        }
        if (suite == AddBuffersEntrySuite)
        {
            RegisterAddBuffersEntryDos(baseAddress);
            return;
        }
        if (suite == RequestChoiceEntrySuite)
        {
            RegisterRequestChoiceEntryDos(baseAddress);
            return;
        }
        if (suite == Workbench31RequestChoiceEntrySuite)
        {
            RegisterWorkbench31RequestChoiceEntryDos(baseAddress);
            return;
        }
        if (suite == RequestFileEntrySuite ||
            suite == Workbench31RequestFileEntrySuite)
        {
            RegisterRequestFileEntryDos(baseAddress);
            return;
        }
        if (IsAvailEntrySuite(suite))
        {
            if (suite == MorphOSAvailEntrySuite)
                RegisterMorphOSAvailDos(baseAddress);
            else
                RegisterWorkbench31AvailDos(baseAddress);
            return;
        }
        if (suite == FormatEntrySuite)
        {
            RegisterFormatDos(baseAddress);
            return;
        }
        if (suite == MountEntrySuite || suite == Workbench31MountEntrySuite)
        {
            RegisterMountEntryDos(baseAddress);
            return;
        }
        if (suite == BindDriversEntrySuite)
        {
            if (workbench31BindDrivers)
                RegisterWorkbench31BindDriversDos(baseAddress);
            else if (bindDriversPrivateImageProbe)
                RegisterWorkbenchBindDriversReferenceDos(baseAddress);
            else
                RegisterBindDriversEntryDos(baseAddress);
            return;
        }
        if (suite == LoadMonDrvsEntrySuite)
        {
            RegisterLoadMonDrvsEntryDos(baseAddress);
            return;
        }
        if (suite == Workbench31DateEntrySuite)
        {
            RegisterWorkbench31DateDos(baseAddress);
            return;
        }
        if (suite == MorphOSDateEntrySuite)
        {
            RegisterMorphOSDateDos(baseAddress);
            return;
        }
        if (suite == Workbench31SetDateEntrySuite)
        {
            RegisterWorkbench31SetDateDos(baseAddress);
            return;
        }
        if (suite == MorphOSSetDateEntrySuite)
        {
            RegisterMorphOSSetDateDos(baseAddress);
            return;
        }
        if (suite == Workbench31WaitEntrySuite)
        {
            RegisterWorkbench31WaitDos(baseAddress);
            return;
        }
        if (suite == MorphOSWaitEntrySuite)
        {
            RegisterMorphOSWaitDos(baseAddress);
            return;
        }
        if (suite == WaitForPortEntrySuite)
        {
            RegisterWaitForPortEntryDos(baseAddress);
            return;
        }
        if (suite == WaitForLibEntrySuite)
        {
            RegisterWaitForLibEntryDos(baseAddress);
            return;
        }
        if (suite == WaitForNotificationEntrySuite)
        {
            RegisterWaitForNotificationEntryDos(baseAddress);
            return;
        }
        if (suite == RelabelEntrySuite)
        {
            RegisterRelabelEntryDos(baseAddress);
            return;
        }
        if (suite == LockEntrySuite || suite == Workbench31LockEntrySuite)
        {
            RegisterLockEntryDos(baseAddress);
            return;
        }
        if (suite == DiskChangeEntrySuite || suite == Workbench31DiskChangeEntrySuite)
        {
            RegisterDiskChangeEntryDos(baseAddress);
            return;
        }
        if (suite == RebootEntrySuite || suite == Workbench31RebootEntrySuite)
        {
            RegisterRebootEntryDos(baseAddress);
            return;
        }
        if (suite == ResListEntrySuite)
        {
            RegisterResListEntryDos(baseAddress);
            return;
        }
        if (suite == LibListEntrySuite)
        {
            RegisterLibListEntryDos(baseAddress);
            return;
        }
        if (suite == DevListEntrySuite)
        {
            RegisterDevListEntryDos(baseAddress);
            return;
        }
        if (suite == PortListEntrySuite)
        {
            RegisterPortListEntryDos(baseAddress);
            return;
        }
        if (suite == ModListEntrySuite)
        {
            RegisterModListEntryDos(baseAddress);
            return;
        }
        if (IsStatusEntrySuite(suite))
        {
            RegisterStatusEntryDos(baseAddress);
            return;
        }
        if (IsProtectEntrySuite(suite))
        {
            RegisterProtectEntryDos(baseAddress);
            return;
        }
        if (suite == TouchEntrySuite)
        {
            RegisterTouchEntryDos(baseAddress);
            return;
        }
        if (suite == SetClockEntrySuite ||
            suite == Workbench31SetClockEntrySuite)
        {
            RegisterSetClockDos(baseAddress);
            return;
        }
        if (IsVersionEntrySuite(suite))
        {
            RegisterVersionEntryDos(baseAddress);
            return;
        }
        if (suite == FindResidentEntrySuite)
        {
            RegisterFindResidentEntryDos(baseAddress);
            return;
        }
        if (suite == IconPosEntrySuite)
        {
            RegisterIconPosEntryDos(baseAddress);
            return;
        }
        if (suite == GuessBootDevEntrySuite)
        {
            RegisterGuessBootDevEntryDos(baseAddress);
            return;
        }
        if (suite == TaskListEntrySuite)
        {
            RegisterTaskListEntryDos(baseAddress);
            return;
        }
        if (suite == InfoEntrySuite)
        {
            RegisterInfoEntryDos(baseAddress);
            return;
        }
        if (suite == Workbench31InfoEntrySuite)
        {
            RegisterWorkbench31InfoEntryDos(baseAddress);
            return;
        }
        if (suite == DiskFreeEntrySuite)
        {
            RegisterDiskFreeEntryDos(baseAddress);
            return;
        }
        if (IsDirEntrySuite(suite))
        {
            RegisterDirEntryDos(baseAddress);
            return;
        }
        if (IsMakeDirEntrySuite(suite))
        {
            RegisterMakeDirEntryDos(baseAddress);
            return;
        }
        if (IsAssignEntrySuite(suite))
        {
            RegisterAssignEntryDos(baseAddress);
            return;
        }
        if (IsJoinEntrySuite(suite))
        {
            RegisterJoinEntryDos(baseAddress);
            return;
        }
        if (suite == WorkbenchMakeLinkEntrySuite)
        {
            RegisterWorkbenchMakeLinkDos(baseAddress);
            return;
        }
        if (suite == MakeLinkEntrySuite)
        {
            RegisterMakeLinkEntryDos(baseAddress);
            return;
        }
        if (IsTransferLoopProbeSuite)
        {
            RegisterCopyLoopProbeDos(baseAddress);
            return;
        }
        if (suite == DeleteObjectProbeSuite)
        {
            RegisterDeleteObjectProbeDos(baseAddress);
            return;
        }
        if (IsDeleteCommandEntrySuite(suite))
        {
            RegisterDeleteCommandDos(baseAddress);
            return;
        }
        if (suite is CopyArgumentGateEntrySuite or CopyOptionSetupSuite or CopyDirectSuite or CopyParsedDeleteSuite or CopyParsedMakeDirectorySuite)
        {
            RegisterCopyArgumentGateEntryDos(baseAddress);
            return;
        }
        if (suite == CopyFilePairProbeSuite)
        {
            RegisterCopyFilePairProbeDos(baseAddress);
            return;
        }
        if (suite == CopyDestinationProbeSuite)
        {
            RegisterCopyDestinationProbeDos(baseAddress);
            return;
        }
        if (suite == CopyDestinationDirectoriesProbeSuite)
        {
            RegisterCopyDestinationDirectoriesProbeDos(baseAddress);
            return;
        }
        if (suite == CopyNonFileSystemProbeSuite)
        {
            RegisterCopyNonFileSystemProbeDos(baseAddress);
            return;
        }
        if (suite == CopyLoopGuardProbeSuite) { RegisterCopyLoopGuardProbeDos(baseAddress); return; }
        if (suite == CopyMetadataProbeSuite) { RegisterCopyMetadataProbeDos(baseAddress); return; }
        if (suite is CopyResultPolicyProbeSuite or CopyCompletionSuite) { RegisterCopyResultPolicyProbeDos(baseAddress); return; }
        if (suite == CopyOpenDestinationProbeSuite) { RegisterCopyOpenDestinationProbeDos(baseAddress); return; }
        if (suite == CopyPatternClassifierProbeSuite) { RegisterCopyPatternClassifierProbeDos(baseAddress); return; }
        if (suite == CopyFlatTraversalProbeSuite) { RegisterCopyFlatTraversalProbeDos(baseAddress); return; }
        if (suite == CopyDirectoryExitProbeSuite) { RegisterCopyDirectoryExitProbeDos(baseAddress); return; }
        if (suite == CopyDirectoryEntryProbeSuite) { RegisterCopyDirectoryEntryProbeDos(baseAddress); return; }
        if (suite is CopyTraversalWorkProbeSuite or CopySourceRoutingProbeSuite or CopyDirectorySourcesSuite or CopyTargetDispatchSuite or CopySingleTargetSuite) { RegisterCopyTraversalWorkProbeDos(baseAddress); return; }
        if (suite == CopyWorkProbeSuite) { RegisterCopyWorkProbeDos(baseAddress); return; }
        if (suite == CopyOutputProbeSuite) { RegisterCopyOutputProbeDos(baseAddress); return; }
        if (suite == CopyDirectoryOperationProbeSuite) { RegisterCopyDirectoryOperationProbeDos(baseAddress); return; }
        if (suite == CopyLinkOperationProbeSuite) { RegisterCopyLinkOperationProbeDos(baseAddress); return; }
        if (suite == CopyFileOperationProbeSuite) { RegisterCopyFileOperationProbeDos(baseAddress); return; }
        if (suite == CopyFileTransferProbeSuite) { RegisterCopyFileTransferProbeDos(baseAddress); return; }
        if (suite == CopyWorkPreparationProbeSuite) { RegisterCopyWorkPreparationProbeDos(baseAddress); return; }
        if (suite == CopyTraversalProbeSuite) { RegisterCopyTraversalProbeDos(baseAddress); return; }
        if (suite == CopyMatchStepProbeSuite) { RegisterCopyMatchStepProbeDos(baseAddress); return; }
        if (suite is CopySoftLinkProbeSuite or CopyMatchedDirectoryProbeSuite) { RegisterCopySoftLinkProbeDos(baseAddress); return; }
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var boundary = invocation.Definition.ArgumentBoundary;
            var expectedTemplate = boundary is null ? "VALUE/N,FAIL/S" : boundary.Template;
            var expectedBytes = boundary is null ? 8u : boundary.AllocationBytes;
            var resultCount = boundary is null ? 2u : boundary.ResultCount;
            Require(expectedTemplate is not null && Bus.CString(state.D[1]) == expectedTemplate &&
                state.D[3] == 0, "ReadArgs template/source ABI mismatch.");
            var results = state.D[2];
            Require(expectedBytes is 4 or 8 && (results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size == expectedBytes,
                "ReadArgs result slots must be owned and LONG aligned.");
            for (var offset = 0u; offset < expectedBytes; offset += 4)
                Require(Bus.Long(results + offset) == 0, "ReadArgs defaults must be zeroed.");
            invocation.Reads++;
            if (invocation.Definition.ParserError != 0)
            {
                invocation.IoError = invocation.Definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 40, "RDArgs", true);
            if (invocation.Definition.Number is int value)
            {
                Require(resultCount > 0, "Numeric fixture result has no declared result slot.");
                Bus.Long(rdArgs + 32, unchecked((uint)value));
                Bus.Long(results, rdArgs + 32);
            }
            if (invocation.Definition.FailSwitch)
            {
                Require(resultCount > 1, "Switch fixture result has no declared result slot.");
                Bus.Long(results + 4, uint.MaxValue);
            }
            if (invocation.Definition.ParserSuccessError is int successError)
                invocation.IoError = successError;
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) => invocation.OutputBptr);
        Register(baseAddress, DosLvo.Close, "Close", (_, _) => throw new InvalidOperationException("Probe closed a borrowed stream."));
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            Require(invocation.Definition.ArgumentBoundary is null, "Argument boundary probe unexpectedly wrote output.");
            Require(state.D[1] == invocation.OutputBptr, "Borrowed output handle changed between processes.");
            Require(state.D[2] == invocation.Arguments && state.D[3] == Encoding.Latin1.GetByteCount(invocation.Definition.Arguments),
                "D0/A0 startup argument bytes or length were not preserved.");
            var count = invocation.Definition.WriteResult ?? checked((int)state.D[3]);
            if (count > 0) invocation.Output.Write(Bus.Memory, (int)state.D[2], count);
            if (count != state.D[3]) invocation.IoError = 221;
            return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            var previous = invocation.IoError;
            invocation.IoError = unchecked((int)state.D[1]);
            Bus.Long(invocation.Process + (uint)DosLayout.Process.Result2, state.D[1]);
            return unchecked((uint)previous);
        });
    }
}
