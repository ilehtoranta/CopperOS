using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record InfoEntryCase(bool Disks = false, bool Volumes = false,
    bool GoodOnly = false, bool Blocks = false, bool Verbose = false,
    bool Filter = false, bool DeviceLockSucceeds = true,
    bool InfoSucceeds = true, bool AllocationFailure = false,
    bool CtrlC = false, bool MissingList = false, int ParserError = 0,
    int Entries = 2, string FilterPattern = "DH0:",
    bool WorkbenchVolumes = false, bool UtilityLibraryOpenFailure = false,
    bool LocaleLibraryOpenFailure = false, bool OpenLocaleFailure = false,
    ushort DosVersion = 51, ushort DosRevision = 8,
    uint InfoNumberOfBlocks = 100, uint InfoNumberOfBlocksUsed = 40,
    uint InfoBytesPerBlock = 1024, uint FileSystemBlocksHigh = 0,
    uint FileSystemBlocksLow = 100, uint FileSystemUsedHigh = 0,
    uint FileSystemUsedLow = 40, bool TotalAttributeSucceeds = true,
    bool UsedAttributeSucceeds = true, uint VolumeDays = 1,
    uint VolumeMinutes = 2, uint VolumeTicks = 3,
    bool DateToStrSucceeds = true, uint VolumeDiskType = 0x444f5300,
    uint SecondVolumeDiskType = 0x444f5300, string VolumeTypeName = "OFS",
    string SecondVolumeTypeName = "OFS",
    uint InfoDiskState = (uint)DosDiskState.Validated,
    uint InfoDiskType = 0x444f5300, bool NameFromLockSucceeds = true,
    uint StartupDosType = 0, uint StartupTableSize = 16,
    uint StartupUnit = 7, bool StartupDevicePresent = true,
    string? InfoDateTimeFormat = null,
    int DeviceLockError = 205,
    int InfoError = 205, bool ReverseTraversal = false,
    bool InvalidFilterPattern = false);

internal sealed record InfoNativeLayout(uint Control, uint List,
    uint DeviceNode, uint VolumeNode, uint DeviceName, uint VolumeName,
    uint FilterVector, uint FilterName, uint Startup, uint VerboseName);

internal sealed partial class ProbeFixture
{
    public const string InfoEntrySuite =
        "morphos320-info-native-entry-vector-fixture";
    public const string Workbench31InfoEntrySuite =
        "workbench31-info-native-entry-vector-fixture";

    public static bool IsInfoEntrySuite(string value) =>
        value == InfoEntrySuite || value == Workbench31InfoEntrySuite;

    private const string InfoHeader =
        "Unit     Size     Used     Free Full Errs   State        Type Name\n";
    private const string VerboseOutput = "  -> scsi.device : 7\n";

    private List<object> RunInfoEntryCases()
    {
        ProbeCase[] cases =
        [
            InfoCase("default", new(), result: DOS.RETURN_OK,
                output: InfoOutput(true, true)),
            InfoCase("three-node-device-volume-lists", new(Entries: 4),
                result: DOS.RETURN_OK,
                output: InfoOutput(true, true, entries: 2)),
            InfoCase("source-sorted-output", new(Entries: 4,
                ReverseTraversal: true), result: DOS.RETURN_OK,
                output: InfoOutput(true, true, entries: 2)),
            InfoCase("disks", new(Disks: true, Volumes: false), arguments: "DISKS",
                result: DOS.RETURN_OK, output: InfoOutput(true, false)),
            InfoCase("volumes", new(Disks: false, Volumes: true), arguments: "VOLS",
                result: DOS.RETURN_WARN, output: InfoOutput(false, true)),
            InfoCase("volume-date-conversion-failure",
                new(Volumes: true, DateToStrSucceeds: false), arguments: "VOLS",
                result: DOS.RETURN_WARN,
                output: InfoOutput(false, true, dateOutput: false)),
            InfoCase("volume-info-datetime-format",
                new(Volumes: true, InfoDateTimeFormat: "weekday date"),
                arguments: "VOLS", result: DOS.RETURN_WARN,
                output: InfoOutput(false, true,
                    localizedDateText: "localized volume date")),
            InfoCase("volumes-with-device-filter",
                new(Volumes: true, Filter: true),
                arguments: "VOLS DEVICES DH0:", result: DOS.RETURN_OK,
                output: InfoOutput(true, true)),
            .. FileSystemTypeCases(),
            InfoCase("filter", new(Filter: true),
                arguments: "DEVICES DH0:", result: DOS.RETURN_OK,
                output: InfoOutput(true, false)),
            InfoCase("invalid-filter-pattern", new(Filter: true,
                FilterPattern: "[broken", InvalidFilterPattern: true),
                arguments: "DEVICES [broken", result: DOS.RETURN_ERROR,
                error: 205, output: ""),
            InfoCase("device-read-only-state", new(Disks: true,
                Volumes: false, InfoDiskState: (uint)DosDiskState.WriteProtected),
                arguments: "DISKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, false, state: "read only")),
            InfoCase("device-validating-state", new(Disks: true,
                Volumes: false, InfoDiskState: (uint)DosDiskState.Validating),
                arguments: "DISKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, false, state: "validating")),
            InfoCase("device-unknown-state", new(Disks: true,
                Volumes: false, InfoDiskState: 0), arguments: "DISKS",
                result: DOS.RETURN_OK, output: InfoOutput(true, false, state: "")),
            InfoCase("device-filesystem-type", new(Disks: true,
                Volumes: false, InfoDiskType: 0x50465300),
                arguments: "DISKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, false, deviceType: "PFS")),
            InfoCase("startup-filesystem-type", new(Disks: true,
                Volumes: false, InfoDiskType: 0x444f5300,
                StartupDosType: 0x53465300), arguments: "DISKS",
                result: DOS.RETURN_OK,
                output: InfoOutput(true, false, deviceType: "SFS")),
            InfoCase("startup-type-short-environment", new(Disks: true,
                Volumes: false, InfoDiskType: 0x50465300,
                StartupDosType: 0x53465300, StartupTableSize: 15),
                arguments: "DISKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, false, deviceType: "PFS")),
            InfoCase("startup-type-invalid-unit", new(Disks: true,
                Volumes: false, InfoDiskType: 0x50465300,
                StartupDosType: 0x53465300,
                StartupUnit: 0x01000007), arguments: "DISKS",
                result: DOS.RETURN_OK,
                output: InfoOutput(true, false, deviceType: "PFS")),
            InfoCase("startup-classic-dos-type", new(Disks: true,
                Volumes: false, InfoDiskType: 0x50465300,
                StartupDosType: 0x444f5301), arguments: "DISKS",
                result: DOS.RETURN_OK,
                output: InfoOutput(true, false, deviceType: "PFS")),
            InfoCase("device-name-from-lock-fallback", new(Disks: true,
                Volumes: false, NameFromLockSucceeds: false),
                arguments: "DISKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, false, deviceName: "DH0:")),
            InfoCase("device-lock-error", new(Disks: true, Volumes: false,
                DeviceLockSucceeds: false), arguments: "DISKS",
                result: DOS.RETURN_WARN, error: 205,
                output: DeviceFailureOutput()),
            InfoCase("device-info-error", new(Disks: true, Volumes: false,
                InfoSucceeds: false), arguments: "DISKS",
                result: DOS.RETURN_WARN, error: 205,
                output: DeviceFailureOutput()),
            InfoCase("wildcard-filter", new(Filter: true,
                FilterPattern: "DH#?:"),
                arguments: "DEVICES DH#?:", result: DOS.RETURN_OK,
                output: InfoOutput(true, false)),
            InfoCase("good-only", new(Disks: true, Volumes: false,
                GoodOnly: true, DeviceLockSucceeds: false,
                DeviceLockError: 205), arguments: "DISKS GOODONLY",
                result: DOS.RETURN_WARN, error: 205,
                output: InfoHeader),
            InfoCase("blocks", new(Blocks: true),
                arguments: "BLOCKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, true, true)),
            InfoCase("dos-51.7-legacy-counters",
                new(Blocks: true, DosRevision: 7), arguments: "DISKS BLOCKS",
                result: DOS.RETURN_OK,
                output: InfoOutput(true, false, true)),
            InfoCase("dos-51.8-wide-counters",
                new(Disks: true, Volumes: false, Blocks: true,
                    InfoBytesPerBlock: 1, FileSystemBlocksHigh: 1,
                    FileSystemBlocksLow: 0, FileSystemUsedHigh: 0,
                    FileSystemUsedLow: 0x80000000),
                arguments: "DISKS BLOCKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, false, true, wideCounters: true)),
            InfoCase("m-counter-format",
                new(Disks: true, Volumes: false, Blocks: true,
                    FileSystemBlocksLow: 4096, FileSystemUsedLow: 2048),
                arguments: "DISKS BLOCKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, false, true, scaledCounters: "M")),
            InfoCase("p-counter-format",
                new(Disks: true, Volumes: false, Blocks: true,
                    InfoBytesPerBlock: 1048576, FileSystemBlocksHigh: 1,
                    FileSystemBlocksLow: 0, FileSystemUsedLow: 0x80000000),
                arguments: "DISKS BLOCKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, false, true, scaledCounters: "P")),
            InfoCase("partial-wide-counter-fallback",
                new(Disks: true, Volumes: false, Blocks: true,
                    InfoBytesPerBlock: 1024, FileSystemBlocksHigh: 1,
                    FileSystemBlocksLow: 0, TotalAttributeSucceeds: true,
                    UsedAttributeSucceeds: false),
                arguments: "DISKS BLOCKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, false, true,
                    wideCounters: true, partialFallback: true)),
            InfoCase("wide-counter-used-clamp",
                new(Disks: true, Volumes: false, Blocks: true,
                    FileSystemBlocksLow: 100, FileSystemUsedLow: 120),
                arguments: "DISKS BLOCKS", result: DOS.RETURN_OK,
                output: InfoOutput(true, false, true, usedClamped: true)),
            InfoCase("verbose-provider", new(Verbose: true),
                arguments: "VERBOSE", result: DOS.RETURN_OK,
                output: InfoOutput(true, true, verbose: true)),
            InfoCase("verbose-startup-string-fallback", new(Verbose: true,
                StartupUnit: 0x01410007, StartupDevicePresent: false),
                arguments: "VERBOSE", result: DOS.RETURN_OK,
                output: InfoOutput(true, true, verbose: true,
                    verboseFallback: "A")),
            InfoCase("allocation-failure", new(AllocationFailure: true),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.NoFreeStore,
                output: ""),
            InfoCase("utility-library-open-failure",
                new(UtilityLibraryOpenFailure: true), result: DOS.RETURN_FAIL),
            InfoCase("locale-library-open-failure",
                new(LocaleLibraryOpenFailure: true), result: DOS.RETURN_OK,
                output: InfoOutput(true, true)),
            InfoCase("open-locale-failure",
                new(OpenLocaleFailure: true), result: DOS.RETURN_OK,
                output: InfoOutput(true, true)),
            InfoCase("ctrl-c", new(CtrlC: true), result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.Break, output: InfoOutput(true, false)),
            InfoCase("missing-list", new(MissingList: true), output: InfoHeader),
            InfoCase("parser-failure", new(ParserError: 116),
                result: DOS.RETURN_ERROR, error: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Info = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Info = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Info = new(), EntryLength = 4, NullArgumentPointer = true },
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            InfoCase("interleaved-left", new(Disks: true, Volumes: false),
                arguments: "DISKS",
                result: DOS.RETURN_OK, output: InfoOutput(true, false)),
            InfoCase("interleaved-right", new(Disks: false, Volumes: true),
                arguments: "VOLS",
                result: DOS.RETURN_WARN, output: InfoOutput(false, true))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private List<object> RunWorkbench31InfoEntryCases()
    {
        ProbeCase[] cases =
        [
            InfoCase("default", new(), result: DOS.RETURN_OK,
                output: WorkbenchInfoOutput()),
            InfoCase("multiple-devices", new(Entries: 4),
                result: DOS.RETURN_OK,
                output: WorkbenchInfoOutput(entries: 2)),
            InfoCase("devices-and-volumes", new(Entries: 4,
                WorkbenchVolumes: true),
                result: DOS.RETURN_OK,
                output: WorkbenchInfoOutput(entries: 2, volumes: true)),
            InfoCase("device", new(), arguments: "DH0:",
                result: DOS.RETURN_OK,
                output: WorkbenchInfoOutput()),
            InfoCase("no-disk", new(DeviceLockSucceeds: false),
                arguments: "DH0:",
                output: "Mounted disks:\n" +
                "Unit Size Used Free Full Errs   Status   Name\n" +
                "DH0: No disk present\n"),
            InfoCase("unreadable", new(InfoSucceeds: false),
                arguments: "DH0:",
                output: "Mounted disks:\n" +
                "Unit Size Used Free Full Errs   Status   Name\n" +
                "DH0: Unreadable disk\n"),
            InfoCase("allocation-failure", new(AllocationFailure: true),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.NoFreeStore),
            InfoCase("parser-failure", new(ParserError: 116),
                result: DOS.RETURN_ERROR, error: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Info = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Info = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Info = new(), EntryLength = 4, NullArgumentPointer = true },
        ];
        cases =
        [
            .. cases,
            new("missing-dos", "DH0:", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { Info = new(), MissingDos = true }
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            InfoCase("interleaved-left", new(), arguments: "DH0:",
                result: DOS.RETURN_OK,
                output: WorkbenchInfoOutput()),
            InfoCase("interleaved-right", new(), arguments: "DH0:",
                result: DOS.RETURN_OK,
                output: WorkbenchInfoOutput())
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase InfoCase(string name, InfoEntryCase definition,
        string arguments = "", int result = DOS.RETURN_WARN, int error = 0,
        string output = "")
    {
        return new(name, arguments, result, error, output)
        { Info = definition };
    }

    private static string InfoOutput(bool device, bool volume,
        bool blocks = false, bool verbose = false, int entries = 1,
        bool wideCounters = false, bool partialFallback = false,
        bool usedClamped = false, string scaledCounters = "",
        bool dateOutput = true, string volumeTypeName = "OFS",
        string secondVolumeTypeName = "OFS", string state = "read/write",
        string deviceType = "OFS", string? deviceName = null,
        string? verboseFallback = null,
        string? localizedDateText = null)
    {
        var text = new StringBuilder();
        if (device)
        {
            text.Append(InfoHeader);
            for (var slot = 0; slot < entries; slot++)
            {
                var shownName = deviceName ?? $"DH{slot}";
                if (scaledCounters == "M")
                    text.Append($"DH{slot}:     4.0M     2.0M     2.0M  50%    2 {state.PadRight(11)}{deviceType.PadLeft(8)} {shownName}\n");
                else if (scaledCounters == "P")
                    text.Append($"DH{slot}:     4.0P     2.0P     2.0P  50%    2 {state.PadRight(11)}{deviceType.PadLeft(8)} {shownName}\n");
                else if (wideCounters)
                    text.Append(partialFallback
                        ? $"DH{slot}:     4.0T      40K     4.0T   0%    2 {state.PadRight(11)}{deviceType.PadLeft(8)} {shownName}\n"
                        : $"DH{slot}:     4.0G     2.0G     2.0G  50%    2 {state.PadRight(11)}{deviceType.PadLeft(8)} {shownName}\n");
                else if (usedClamped)
                    text.Append($"DH{slot}:     100K     100K       0K 100%    2 {state.PadRight(11)}{deviceType.PadLeft(8)} {shownName}\n");
                else
                    text.Append($"DH{slot}:     100K      40K      60K  40%    2 {state.PadRight(11)}{deviceType.PadLeft(8)} {shownName}\n");
                if (verbose)
                    text.Append(verboseFallback is null
                        ? $"  -> scsi.device : {7 + slot}\n"
                        : $"  -> {verboseFallback}\n");
                if (blocks)
                {
                    var totalBlocks = "100";
                    var usedBlocks = "40";
                    var freeBlocks = "60";
                    var blockSize = "1024";
                    if (scaledCounters == "M")
                    {
                        totalBlocks = "4096";
                        usedBlocks = "2048";
                        freeBlocks = "2048";
                    }
                    else if (scaledCounters == "P" ||
                        wideCounters && !partialFallback)
                    {
                        totalBlocks = "4294967296";
                        usedBlocks = "2147483648";
                        freeBlocks = "2147483648";
                        if (scaledCounters == "P") blockSize = "1048576";
                        else blockSize = "1";
                    }
                    else if (partialFallback)
                    {
                        totalBlocks = "4294967296";
                        usedBlocks = "40";
                        freeBlocks = "4294967256";
                    }
                    else if (usedClamped)
                    {
                        usedBlocks = "100";
                        freeBlocks = "0";
                    }
                    text.Append("\nTotal blocks: " + totalBlocks.PadRight(10) +
                        "  Blocks used: " + usedBlocks + "\n Blocks free: " +
                        freeBlocks.PadRight(10) + "    Blocksize: " +
                        blockSize + "\n");
                }
            }
        }
        if (volume)
        {
            text.Append(device ? "\nVolumes available:\n" :
                "Volumes available:\n");
            for (var slot = 0; slot < entries; slot++)
                text.Append(VolumeOutput(slot, dateOutput,
                    slot == 0 ? volumeTypeName : secondVolumeTypeName,
                    localizedDateText));
        }
        return text.ToString();
    }

    private static string VolumeOutput(int slot, bool dateOutput,
        string typeName, string? localizedDateText = null) =>
        $"DH{slot}".PadRight(16) + "[Mounted]".PadRight(10) +
        (localizedDateText is not null ? localizedDateText : dateOutput
            ? "created " + "Mon".PadLeft(11) + ", " +
                "01-Jan-90".PadRight(10) + " 12:34:56"
            : "") + $" <{typeName}>\n";

    private static string DeviceFailureOutput(string device = "DH0:") =>
        InfoHeader + device.PadRight(4) + "\tDisk error\n";

    private static ProbeCase[] FileSystemTypeCases()
    {
        (uint Id, string Name)[] types =
        [
            (0x444f5300, "OFS"), (0x444f5301, "FFS"),
            (0x444f5302, "OFS-INT"), (0x444f5303, "FFS-INT"),
            (0x444f5304, "OFS-DC"), (0x444f5305, "FFS-DC"),
            (0x444f5306, "OFS-LNFS"), (0x444f5307, "FFS-LNFS"),
            (0x4d534400, "MS-DOS"), (0x41434400, "CDFS"),
            // CD01 appears as CACHECDFS before the later CD-ISO table row.
            (0x43443031, "CDFS"), (0x662dabac, "CDFS"),
            (0x4e444f53, "NO DOS"), (0x4d414300, "Mac"),
            (0x4d4e5801, "Minix"), (0x514c3541, "QL720k"),
            (0x514c3542, "QL1.4M"), (0x43505c4d, "CP/M"),
            (0x5a585303, "+3Dos"), (0x5a585300, "Disciple "),
            (0x5a585301, "UniDos"), (0x5a585302, "SamDos"),
            (0x5a585304, "Opus"), (0x50324130, "NETWORK"),
            (0x53465300, "SFS"), (0x41465300, "AFS"),
            (0x50465300, "PFS"), (0x42464653, "BFFS"),
            (0x43444653, "CDFS"), (0x43443030, "CD-HSF"),
            (0x43444441, "CDDA"), (0x45585402, "Ext2"),
            (0x58543203, "Ext3"), (0x4e544653, "NTFS"),
            (0x58465300, "SGI-XFS"), (0x4846532b, "Mac-HFS+"),
            (0x534d4200, "SmbFS"), (0x534d4202, "Smb2FS"),
            (0x54524600, "TrashFS"), (0x50465302, "PFS2"),
            (0x50445300, "PDS0"), (0x6d755046, "muPF"),
            (0x4d4e5802, "MNX2"), (0x554e4b4e, "UNKN")
        ];
        var cases = new ProbeCase[types.Length / 2];
        for (var index = 0; index < cases.Length; index++)
        {
            var first = types[index * 2];
            var second = types[index * 2 + 1];
            cases[index] = InfoCase(
                $"volume-file-system-types-{index + 1:D2}",
                new(Volumes: true, Entries: 4,
                    VolumeDiskType: first.Id,
                    SecondVolumeDiskType: second.Id,
                    VolumeTypeName: first.Name,
                    SecondVolumeTypeName: second.Name),
                arguments: "VOLS", result: DOS.RETURN_WARN,
                output: InfoOutput(false, true, entries: 2,
                    volumeTypeName: first.Name,
                    secondVolumeTypeName: second.Name));
        }
        return cases;
    }

    private static string WorkbenchInfoOutput(int entries = 1,
        bool volumes = false)
    {
        var text = new StringBuilder(
            "Mounted disks:\n" +
            "Unit Size Used Free Full Errs   Status   Name\n");
        for (var slot = 0; slot < entries; slot++)
            text.Append($"DH{slot}:  100K      40      60  40%   2  " +
                $"Read/Write DH{slot}:\n");
        if (volumes)
        {
            text.Append("\nVolumes available:\n");
            for (var slot = 0; slot < entries; slot++)
                text.Append($"DH{slot}: [Mounted]\n");
        }
        return text.ToString();
    }

    private void PrepareInfoEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Info ?? new();
        Require(definition.Entries is 0 or 2 or 4,
            "Info fixture only supports bounded zero-to-two device/volume pairs.");
        // Keep fixture-only DOS-list storage in the invocation's process
        // extension area so it follows the same lifetime as the command's
        // startup workspace and does not masquerade as a command allocation.
        var control = invocation.Process + 0x200;
        var list = control + 32;
        var deviceNode = control + 96;
        var volumeNode = control + 512;
        var deviceName = control + 1024;
        var volumeName = control + 1280;
        var filterVector = control + 1536;
        var filterName = control + 1552;
        var startup = control + 1600;
        var verboseName = control + 1728;
        invocation.InfoLayout = new(control, list, deviceNode, volumeNode,
            deviceName, volumeName, filterVector, filterName, startup,
            verboseName);

        Bus.Long(list + (uint)DosLayout.DosList.Next, 0);
        for (var slot = 0; slot < 2; slot++)
        {
            var offset = (uint)slot * 0x100u;
            var deviceNameAddress = deviceName + (uint)slot * 0x40u;
            var volumeNameAddress = volumeName + (uint)slot * 0x40u;
            var startupAddress = startup + (uint)slot * 0x40u;
            var environmentAddress = control + 1856u +
                (uint)slot * 128u;
            var verboseNameAddress = verboseName + (uint)slot * 0x40u;
            Bus.Long(deviceNode + offset + (uint)DosLayout.DosList.Next, 0);
            Bus.Long(volumeNode + offset + (uint)DosLayout.DosList.Next, 0);
            Bus.Long(deviceNode + offset + (uint)DosLayout.DosList.Type,
                (uint)DosListType.Device);
            Bus.Long(volumeNode + offset + (uint)DosLayout.DosList.Type,
                (uint)DosListType.Volume);
            Bus.Long(deviceNode + offset + (uint)DosLayout.DosList.Task, 0x1234);
            Bus.Long(volumeNode + offset + (uint)DosLayout.DosList.Task, 0x1234);
            PutBString(deviceNameAddress, slot == 0 ? "DH0" : "DH1");
            PutBString(volumeNameAddress, slot == 0 ? "DH0" : "DH1");
            Bus.Long(deviceNode + offset + (uint)DosLayout.DosList.Name,
                deviceNameAddress >> 2);
            Bus.Long(volumeNode + offset + (uint)DosLayout.DosList.Name,
                volumeNameAddress >> 2);
            Bus.Long(volumeNode + offset + (uint)DosLayout.DeviceList.DiskType,
                slot == 0 ? definition.VolumeDiskType :
                    definition.SecondVolumeDiskType);
            Bus.Long(volumeNode + offset +
                (uint)DosLayout.DeviceList.VolumeDate +
                (uint)DosLayout.DateStamp.Days, definition.VolumeDays);
            Bus.Long(volumeNode + offset +
                (uint)DosLayout.DeviceList.VolumeDate +
                (uint)DosLayout.DateStamp.Minutes, definition.VolumeMinutes);
            Bus.Long(volumeNode + offset +
                (uint)DosLayout.DeviceList.VolumeDate +
                (uint)DosLayout.DateStamp.Ticks, definition.VolumeTicks);
            Bus.Long(startupAddress + (uint)DosLayout.FileSysStartupMsg.Unit,
                definition.StartupUnit + (uint)slot);
            PutBString(verboseNameAddress, "scsi.device");
            Bus.Long(startupAddress + (uint)DosLayout.FileSysStartupMsg.Device,
                definition.StartupDevicePresent
                    ? verboseNameAddress >> 2 : 0);
            Bus.Long(startupAddress +
                (uint)DosLayout.FileSysStartupMsg.Environment,
                environmentAddress >> 2);
            Bus.Long(environmentAddress +
                (uint)DosLayout.DosEnvec.TableSize,
                definition.StartupTableSize);
            Bus.Long(environmentAddress +
                (uint)DosLayout.DosEnvec.DosType,
                definition.StartupDosType);
            Bus.Long(deviceNode + offset + (uint)DosLayout.DeviceNode.Startup,
                startupAddress >> 2);
        }
        Bus.Long(filterVector, definition.Filter ? filterName : 0);
        Bus.Long(filterVector + 4, 0);
        PutCString(filterName, definition.FilterPattern);
    }

    private void RegisterInfoEntryExec()
    {
        Register(InfoUtilityBase, UtilityLvo.Stricmp, "Stricmp",
            (state, invocation) =>
            {
                Require(state.A[0] != 0 && state.A[1] != 0,
                    "Info Utility Stricmp string pointers differ.");
                invocation.InfoStricmpCalls++;
                return unchecked((uint)string.Compare(
                    Bus.CString(state.A[0]), Bus.CString(state.A[1]),
                    StringComparison.OrdinalIgnoreCase));
            });
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
        {
            Require(state.D[0] == 0 && state.D[1] == 0,
                "Info Ctrl-C query ABI differs.");
            invocation.InfoSetSignalCalls++;
            return invocation.Definition.Info!.CtrlC ? 1u << 12 : 0;
        });
        Register(InfoLocaleBase, -156, "OpenLocale", (state, invocation) =>
        {
            Require(state.A[0] == 0 && invocation.InfoOpenLocaleCalls == 0,
                "Info OpenLocale must select the default locale once.");
            invocation.InfoOpenLocaleCalls++;
            return invocation.Definition.Info!.OpenLocaleFailure
                ? 0u : InfoLocaleObject;
        });
        Register(InfoLocaleBase, -42, "CloseLocale", (state, invocation) =>
        {
            var failedOpen = invocation.Definition.Info!.OpenLocaleFailure;
            Require(invocation.InfoLocaleLibraryOpens == 1 &&
                state.A[0] == (failedOpen ? 0u : InfoLocaleObject) &&
                invocation.InfoOpenLocaleCalls == 1 &&
                invocation.InfoCloseLocaleCalls == 0,
                "Info closed a locale object with different ownership.");
            invocation.InfoCloseLocaleCalls++;
            return 0;
        });
        Register(InfoLocaleBase, -60, "FormatDate", (state, invocation) =>
        {
            var definition = invocation.Definition.Info!;
            Require(definition.InfoDateTimeFormat is not null &&
                state.A[0] == InfoLocaleObject &&
                Bus.CString(state.A[1]) == definition.InfoDateTimeFormat &&
                state.A[2] != 0 && Bus.Long(state.A[2]) == definition.VolumeDays &&
                Bus.Long(state.A[2] + 4) == definition.VolumeMinutes &&
                Bus.Long(state.A[2] + 8) == definition.VolumeTicks &&
                state.A[3] != 0 &&
                Bus.Long(state.A[3] + (uint)UtilityLayout.Hook.Entry) != 0 &&
                Bus.Long(state.A[3] + (uint)UtilityLayout.Hook.SubEntry) == 0 &&
                Bus.Long(state.A[3] + (uint)UtilityLayout.Hook.Data) != 0,
                "Info Locale FormatDate or output-hook ABI differs.");
            var output = Bus.Long(state.A[3] +
                (uint)UtilityLayout.Hook.Data);
            var text = "localized volume date";
            PutCString(output, text);
            Bus.Long(state.A[3] + (uint)UtilityLayout.Hook.Data,
                output + (uint)text.Length);
            invocation.LocaleFormatDateCalls++;
            return 0;
        });
    }

    private void RegisterWorkbench31InfoEntryExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state, invocation) =>
        {
            Require(state.D[0] == 0 && state.D[1] == 0,
                "Workbench Info Ctrl-C query ABI differs.");
            invocation.InfoSetSignalCalls++;
            return invocation.Definition.Info!.CtrlC ? 1u << 12 : 0;
        });
    }

    private void RegisterWorkbench31InfoEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Info!;
            Require(Bus.CString(state.D[1]) == "DEVICE" &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 4,
                "Workbench Info template or result storage differs.");
            invocation.InfoReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var requested = invocation.Definition.Arguments.Length == 0
                ? 0u : invocation.InfoLayout!.DeviceName;
            if (requested != 0) PutCString(requested, "DH0:");
            Bus.Long(state.D[2], requested);
            invocation.InfoRdArgs = Bus.Allocate(invocation, 40,
                "InfoRdArgs", true);
            return invocation.InfoRdArgs.Value;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "InfoRdArgs");
            invocation.InfoFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.LockDosList, "LockDosList", (state, invocation) =>
        {
            Require(state.D[1] == ((uint)DosListLockFlags.Read |
                (uint)DosListLockFlags.Assigns | (uint)DosListLockFlags.Volumes |
                (uint)DosListLockFlags.Devices), "Workbench Info list lock flags differ.");
            invocation.InfoLockCalls++;
            return invocation.Definition.Info!.MissingList ? 0u :
                invocation.InfoLayout!.List;
        });
        Register(baseAddress, DosLvo.NextDosEntry, "NextDosEntry", (state, invocation) =>
        {
            Require(state.D[2] == ((uint)DosListLockFlags.Volumes |
                (uint)DosListLockFlags.Devices), "Workbench Info list flags differ.");
            invocation.InfoNextCalls++;
            var pairCount = invocation.Definition.Info!.Entries / 2;
            if (invocation.InfoNextCalls <= pairCount)
                return invocation.InfoLayout!.DeviceNode +
                    (uint)(invocation.InfoNextCalls - 1) * 0x100u;
            if (invocation.Definition.Info.WorkbenchVolumes &&
                invocation.InfoNextCalls <= pairCount * 2)
                return invocation.InfoLayout!.VolumeNode +
                    (uint)(invocation.InfoNextCalls - pairCount - 1) * 0x100u;
            return 0u;
        });
        Register(baseAddress, DosLvo.UnLockDosList, "UnLockDosList", (state, invocation) =>
        {
            Require(state.D[1] == ((uint)DosListLockFlags.Read |
                (uint)DosListLockFlags.Assigns | (uint)DosListLockFlags.Volumes |
                (uint)DosListLockFlags.Devices), "Workbench Info list unlock flags differ.");
            invocation.InfoUnlockCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (state, invocation) =>
        {
            var name = Bus.CString(state.D[1]);
            Require(name is "DH0:" or "DH1:" &&
                state.D[2] == unchecked((uint)DOS.LockMode.Shared),
                "Workbench Info lock ABI differs.");
            invocation.InfoPathLockCalls++;
            var slot = name == "DH1:" ? 1u : 0u;
            return invocation.Definition.Info!.DeviceLockSucceeds
                ? 0x131u + slot : 0u;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            Require(state.D[1] is 0x131u or 0x132u,
                "Workbench Info unlock ABI differs.");
            invocation.InfoPathUnlockCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Info, "Info", (state, invocation) =>
        {
            Require(state.D[1] is 0x131u or 0x132u && state.D[2] != 0,
                "Workbench Info data ABI differs.");
            invocation.InfoCalls++;
            if (!invocation.Definition.Info!.InfoSucceeds)
            {
                invocation.IoError = 205;
                return 0;
            }
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.NumberOfSoftErrors, 2);
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.NumberOfBlocks, 100);
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.NumberOfBlocksUsed, 40);
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.BytesPerBlock, 1024);
            return 1;
        });
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            var text = Bus.CString(state.D[2]);
            Require(text == "Mounted disks:\n" ||
                text == "Unit Size Used Free Full Errs   Status   Name\n" ||
                text == "\nVolumes available:\n", "Workbench Info header differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.InfoFPutsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            var format = Bus.CString(state.D[1]);
            var args = state.D[2];
            if (format == "%-8s%5ld%s%8ld%8ld %3ld%% %3ld  %-10s %-s\n")
            {
                var name = Bus.CString(Bus.Long(args));
                Require(name is "DH0:" or "DH1:" &&
                    Bus.Long(args + 4) == 100 && Bus.CString(Bus.Long(args + 8)) == "K" &&
                    Bus.Long(args + 12) == 40 && Bus.Long(args + 16) == 60 &&
                    Bus.Long(args + 20) == 40 && Bus.Long(args + 24) == 2 &&
                    Bus.CString(Bus.Long(args + 28)) == "Read/Write" &&
                    Bus.CString(Bus.Long(args + 32)) == name,
                    "Workbench Info row differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"{name}  100K      40      60  40%   2  Read/Write {name}\n"));
            }
            else
            {
                Require(format == "%s %s\n" || format == "%s [Mounted]\n",
                    "Workbench Info status format differs.");
                var mounted = format == "%s [Mounted]\n";
                var name = Bus.CString(Bus.Long(args));
                Require(name is "DH0:" or "DH1:",
                    "Workbench Info status name differs.");
                var text = mounted
                    ? name + " [Mounted]\n"
                    : name + " " + Bus.CString(Bus.Long(args + 4)) + "\n";
                invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            }
            invocation.InfoVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                if (state.D[2] == 0)
                    Require(state.D[1] == unchecked((uint)invocation.IoError),
                        "Info outer PrintFault did not publish IoErr.");
                else
                    Require(state.D[2] == CString.ToUInt32("\t") &&
                        state.D[1] == unchecked((uint)invocation.IoError),
                        "Info device PrintFault did not preserve its error.");
                invocation.InfoPrintFaultCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) => invocation.OutputBptr);
    }

    private void VerifyWorkbench31InfoEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Info!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.InfoPathLockCalls == 0 && invocation.InfoLockCalls == 0,
                "Workbench Info crossed an invalid startup boundary.");
            return;
        }
        if (definition.ParserError != 0 || definition.AllocationFailure) return;
        Require(invocation.InfoReadArgsCalls == 1 && invocation.InfoFreeArgsCalls == 1,
            "Workbench Info parser ownership differs.");
        var expectedDevices = invocation.Definition.Arguments.Length == 0
            ? Math.Max(1, definition.Entries / 2) : 1;
        if (invocation.Definition.Arguments.Length == 0)
            Require(invocation.InfoLockCalls == 1 && invocation.InfoUnlockCalls == 1,
                "Workbench Info list lock lifetime differs.");
        Require(invocation.InfoPathLockCalls == expectedDevices,
            "Workbench Info did not lock the selected device.");
        if (definition.DeviceLockSucceeds)
            Require(invocation.InfoCalls == expectedDevices &&
                invocation.InfoPathUnlockCalls == expectedDevices,
                "Workbench Info handler ownership differs.");
    }

    private void RegisterInfoEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.GetVar, "GetVar", (state, invocation) =>
        {
            var definition = invocation.Definition.Info!;
            Require(Bus.CString(state.D[1]) == "info_datetime" &&
                state.D[2] != 0 && state.D[3] == 64 && state.D[4] == 0,
                "Info info_datetime GetVar ABI differs.");
            invocation.InfoGetVarCalls++;
            if (definition.InfoDateTimeFormat is null) return 0;
            PutCString(state.D[2], definition.InfoDateTimeFormat);
            return unchecked((uint)definition.InfoDateTimeFormat.Length);
        });
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Info!;
            Require(Bus.CString(state.D[1]) ==
                "DISKS/S,VOLS=VOLUMES/S,GOODONLY/S,BLOCKS/S,VERBOSE/S,DEVICES/M",
                "Info template differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 24,
                "Info results differ.");
            invocation.InfoReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.InfoLayout = null;
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var result = state.D[2];
            var text = invocation.Definition.Arguments;
            Bus.Long(result, text.Contains("DISKS", StringComparison.OrdinalIgnoreCase) ? 1u : 0u);
            Bus.Long(result + 4, text.Contains("VOLS", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("VOLUMES", StringComparison.OrdinalIgnoreCase) ? 1u : 0u);
            Bus.Long(result + 8, definition.GoodOnly ? 1u : 0u);
            Bus.Long(result + 12, definition.Blocks ? 1u : 0u);
            Bus.Long(result + 16, definition.Verbose ? 1u : 0u);
            Bus.Long(result + 20, definition.Filter ? invocation.InfoLayout!.FilterVector : 0);
            invocation.InfoRdArgs = Bus.Allocate(invocation, 24, "InfoRdArgs", true);
            return invocation.InfoRdArgs.Value;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "InfoRdArgs");
            invocation.InfoFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.LockDosList, "LockDosList", (state, invocation) =>
        {
            var definition = invocation.Definition.Info!;
            Require(state.D[1] == ((uint)DosListLockFlags.Read |
                (uint)DosListLockFlags.Assigns | (uint)DosListLockFlags.Volumes |
                (uint)DosListLockFlags.Devices), "Info list lock flags differ.");
            invocation.InfoLockCalls++;
            return definition.MissingList ? 0u : invocation.InfoLayout!.List;
        });
        Register(baseAddress, DosLvo.NextDosEntry, "NextDosEntry", (state, invocation) =>
        {
            var definition = invocation.Definition.Info!;
            Require(state.D[2] == ((uint)DosListLockFlags.Volumes |
                (uint)DosListLockFlags.Devices), "Info list traversal flags differ.");
            invocation.InfoNextCalls++;
            if (definition.CtrlC && invocation.InfoNextCalls == 1)
                return invocation.InfoLayout!.DeviceNode;
            var pairCount = definition.Entries / 2;
            if (invocation.InfoNextCalls <= pairCount)
            {
                var slot = (uint)(invocation.InfoNextCalls - 1);
                if (definition.ReverseTraversal)
                    slot = (uint)(pairCount - invocation.InfoNextCalls);
                return invocation.InfoLayout!.DeviceNode + slot * 0x100u;
            }
            if (invocation.InfoNextCalls <= definition.Entries)
            {
                var slot = (uint)(invocation.InfoNextCalls - pairCount - 1);
                if (definition.ReverseTraversal)
                    slot = (uint)(pairCount -
                        (invocation.InfoNextCalls - pairCount));
                return invocation.InfoLayout!.VolumeNode + slot * 0x100u;
            }
            return 0u;
        });
        Register(baseAddress, DosLvo.UnLockDosList, "UnLockDosList", (state, invocation) =>
        {
            Require(state.D[1] == ((uint)DosListLockFlags.Read |
                (uint)DosListLockFlags.Assigns | (uint)DosListLockFlags.Volumes |
                (uint)DosListLockFlags.Devices), "Info list unlock flags differ.");
            invocation.InfoUnlockCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.IsFileSystem, "IsFileSystem", (state, invocation) =>
        {
            var path = Bus.CString(state.D[1]);
            Require(path is "DH0:" or "DH1:", "Info filesystem name differs.");
            invocation.InfoIsFileSystemCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.ParsePatternNoCase,
            "ParsePatternNoCase", (state, invocation) =>
        {
            var pattern = Bus.CString(state.D[1]);
            Require(state.D[2] != 0 && state.D[3] is 128 or 512,
                "Info pattern parser ABI differs.");
            invocation.InfoParsePatternCalls++;
            if (invocation.Definition.Info!.InvalidFilterPattern &&
                state.D[3] == 128)
            {
                invocation.IoError = invocation.Definition.Info.InfoError;
                return unchecked((uint)-1);
            }
            Encoding.Latin1.GetBytes(pattern).CopyTo(
                Bus.Memory.AsSpan((int)state.D[2]));
            Bus.Memory[state.D[2] + (uint)pattern.Length] = 0;
            return unchecked((uint)(pattern.Length <= 510 ? pattern.Length : -1));
        });
        Register(baseAddress, DosLvo.MatchPatternNoCase,
            "MatchPatternNoCase", (state, invocation) =>
        {
            var pattern = Bus.CString(state.D[1]);
            var text = Bus.CString(state.D[2]);
            invocation.InfoMatchPatternCalls++;
            return GlobMatch(pattern, text) ? 1u : 0u;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (state, invocation) =>
        {
            var path = Bus.CString(state.D[1]);
            Require((path == "DH0:" || path == "DH1:") &&
                state.D[2] == unchecked((uint)DOS.LockMode.Shared),
                "Info lock ABI differs.");
            invocation.InfoPathLockCalls++;
            var definition = invocation.Definition.Info!;
            if (!definition.DeviceLockSucceeds)
            {
                invocation.IoError = definition.DeviceLockError;
                return 0;
            }
            var slot = path == "DH1:" ? 1u : 0u;
            return 0x131u + slot;
        });
        Register(baseAddress, DosLvo.NameFromLock, "NameFromLock",
            (state, invocation) =>
            {
                Require(state.D[1] is 0x131u or 0x132u &&
                    state.D[2] != 0 && state.D[3] == 108,
                    "Info NameFromLock ABI differs.");
                invocation.InfoNameFromLockCalls++;
                invocation.InfoCurrentDeviceName = state.D[1] == 0x132u
                    ? "DH1" : "DH0";
                if (invocation.Definition.Info!.NameFromLockSucceeds)
                    PutCString(state.D[2], state.D[1] == 0x132u
                        ? "DH1:" : "DH0:");
                return invocation.Definition.Info.NameFromLockSucceeds
                    ? 1u : 0u;
            });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            Require(state.D[1] is 0x131u or 0x132u,
                "Info path unlock ABI differs.");
            invocation.InfoPathUnlockCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Info, "Info", (state, invocation) =>
        {
            Require(state.D[1] is 0x131u or 0x132u && state.D[2] != 0,
                "Info data ABI differs.");
            var definition = invocation.Definition.Info!;
            invocation.InfoCalls++;
            if (!definition.InfoSucceeds)
            {
                invocation.IoError = definition.InfoError;
                return 0;
            }
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.NumberOfSoftErrors, 2);
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.NumberOfBlocks,
                definition.InfoNumberOfBlocks);
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.NumberOfBlocksUsed,
                definition.InfoNumberOfBlocksUsed);
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.BytesPerBlock,
                definition.InfoBytesPerBlock);
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.DiskState,
                definition.InfoDiskState);
            Bus.Long(state.D[2] + (uint)DosLayout.InfoData.DiskType,
                definition.InfoDiskType);
            return 1;
        });
        Register(baseAddress, DosLvo.DateToStr, "DateToStr",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Info!;
                var dateTime = state.D[1];
                Require(dateTime != 0 &&
                    Bus.Memory[dateTime + (uint)DosLayout.DateTime.Format] ==
                        (byte)DosDateFormat.Dos &&
                    Bus.Memory[dateTime + (uint)DosLayout.DateTime.Flags] == 0 &&
                    Bus.Long(dateTime + (uint)DosLayout.DateTime.Stamp) ==
                        definition.VolumeDays &&
                    Bus.Long(dateTime + (uint)DosLayout.DateTime.Stamp + 4) ==
                        definition.VolumeMinutes &&
                    Bus.Long(dateTime + (uint)DosLayout.DateTime.Stamp + 8) ==
                        definition.VolumeTicks,
                    "Info DateToStr structure or volume stamp differs.");
                var day = Bus.Long(dateTime + (uint)DosLayout.DateTime.Day);
                var date = Bus.Long(dateTime + (uint)DosLayout.DateTime.Date);
                var time = Bus.Long(dateTime + (uint)DosLayout.DateTime.Time);
                Require(day != 0 && date != 0 && time != 0,
                    "Info DateToStr output buffers differ.");
                PutCString(day, "Mon");
                PutCString(date, "01-Jan-90");
                PutCString(time, "12:34:56");
                invocation.DateToStrCalls++;
                return definition.DateToStrSucceeds ? 1u : 0u;
            });
        Register(baseAddress, DosLvo.GetFileSysAttr, "GetFileSysAttr",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Info!;
                Require(Bus.CString(state.D[1]) is "DH0:" or "DH1:" &&
                    state.D[4] == 8 && state.D[3] != 0 &&
                    (state.D[3] & 3) == 0,
                    "Info GetFileSysAttr path/storage ABI differs.");
                invocation.InfoGetFileSysAttrCalls++;
                var address = state.D[3];
                if (state.D[2] == (uint)FileSystemQueryAttribute.NumBlocks)
                {
                    Bus.Long(address, definition.FileSystemBlocksHigh);
                    Bus.Long(address + 4, definition.FileSystemBlocksLow);
                    return definition.TotalAttributeSucceeds ? 1u : 0u;
                }
                Require(state.D[2] ==
                    (uint)FileSystemQueryAttribute.NumBlocksUsed,
                    "Info GetFileSysAttr attribute differs.");
                Bus.Long(address, definition.FileSystemUsedHigh);
                Bus.Long(address + 4, definition.FileSystemUsedLow);
                return definition.UsedAttributeSucceeds ? 1u : 0u;
            });
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            var text = Bus.CString(state.D[2]);
            Require(text == InfoHeader || text == "Volumes available:\n" ||
                text == "\nVolumes available:\n" || text == "\n" ||
                text == "localized volume date" ||
                text == "Not Enough memory for device/volume buffer\n",
                "Info FPuts text differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.InfoFPutsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            var format = Bus.CString(state.D[1]);
            var args = state.D[2];
            if (format == "%-4s")
            {
                var name = Bus.CString(Bus.Long(args));
                Require(name is "DH0:" or "DH1:" or "Unit",
                    "Info device name differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(name.PadRight(4)));
            }
            else if (format == "%-16s%-10s")
            {
                var name = Bus.CString(Bus.Long(args));
                var mounted = Bus.CString(Bus.Long(args + 4));
                Require((name is "DH0" or "DH1") && mounted == "[Mounted]",
                    "Info mounted-volume prefix differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    name.PadRight(16) + mounted.PadRight(10)));
            }
            else if (format == "created %11s, %-10s %s")
            {
                var day = Bus.CString(Bus.Long(args));
                var date = Bus.CString(Bus.Long(args + 4));
                var time = Bus.CString(Bus.Long(args + 8));
                Require(day == "Mon" && date == "01-Jan-90" &&
                    time == "12:34:56", "Info volume date strings differ.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    "created " + day.PadLeft(11) + ", " +
                    date.PadRight(10) + " " + time));
            }
            else if (format == " <%s>")
            {
                var diskType = Bus.CString(Bus.Long(args));
                var definition = invocation.Definition.Info!;
                var typeIndex = invocation.InfoVolumeTypeCalls++;
                var expectedType = typeIndex == 0
                    ? definition.VolumeTypeName
                    : definition.SecondVolumeTypeName;
                Require(diskType == expectedType,
                    $"Info volume filesystem label differs: {diskType}/{expectedType}.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    " <" + diskType + ">"));
            }
            else if (format == "%8ldK")
            {
                var value = Bus.Long(args);
                Require(value < 1024,
                    "Info used the small-number format for a large value.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    value.ToString().PadLeft(8) + "K"));
            }
            else if (format == "%6ld.%ld%lc")
            {
                var integer = Bus.Long(args);
                var fraction = Bus.Long(args + 4);
                var suffix = Bus.Long(args + 8);
                Require(fraction < 10 && suffix is (uint)'M' or (uint)'G' or
                    (uint)'T' or (uint)'P', "Info scaled-number fields differ.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    integer.ToString().PadLeft(6) + "." + fraction +
                    (char)suffix));
            }
            else if (format == "%4ld%% %4ld %-11s%8s %s\n")
            {
                var full = Bus.Long(args);
                var errors = Bus.Long(args + 4);
                var stateLabel = Bus.CString(Bus.Long(args + 8));
                var type = Bus.CString(Bus.Long(args + 12));
                var name = Bus.CString(Bus.Long(args + 16));
                var definition = invocation.Definition.Info!;
                var expectedState = definition.InfoDiskState ==
                        (uint)DosDiskState.WriteProtected
                    ? "read only"
                    : definition.InfoDiskState ==
                        (uint)DosDiskState.Validating
                        ? "validating"
                        : definition.InfoDiskState ==
                            (uint)DosDiskState.Validated
                            ? "read/write" : "";
                var expectedName = invocation.InfoCurrentDeviceName +
                    (definition.NameFromLockSucceeds ? "" : ":");
                Require((full <= 100) && errors == 2 &&
                    stateLabel == expectedState &&
                    type == ExpectedDeviceType(definition) &&
                    name == expectedName,
                    "Info device status fields differ.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    full.ToString().PadLeft(4) + "% " +
                    errors.ToString().PadLeft(4) + " " +
                    stateLabel.PadRight(11) + type.PadLeft(8) + " " +
                    name + "\n"));
            }
            else if (format == "  -> %s : %ld\n")
            {
                var name = Bus.CString(Bus.Long(args));
                var unit = Bus.Long(args + 4);
                Require(name == "scsi.device" && unit is >= 7 and <= 8,
                    "Info verbose fields differ.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"  -> {name} : {unit}\n"));
            }
            else if (format == "  -> %s\n")
            {
                var name = Bus.CString(Bus.Long(args));
                Require(name == "A",
                    "Info verbose startup-string fallback differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    $"  -> {name}\n"));
            }
            else if (format == "\nTotal blocks: %-10llu  Blocks used: %llu\n" +
                " Blocks free: %-10llu    Blocksize: %lu\n")
            {
                var total = ((ulong)Bus.Long(args) << 32) | Bus.Long(args + 4);
                var used = ((ulong)Bus.Long(args + 8) << 32) | Bus.Long(args + 12);
                var free = ((ulong)Bus.Long(args + 16) << 32) | Bus.Long(args + 20);
                var blockSize = Bus.Long(args + 24);
                Require(total >= used && total - used == free,
                    "Info block totals differ.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    "\nTotal blocks: " + total.ToString().PadRight(10) +
                    "  Blocks used: " + used + "\n Blocks free: " +
                    free.ToString().PadRight(10) + "    Blocksize: " +
                    blockSize + "\n"));
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unexpected Info VPrintf format: {format}.");
            }
            invocation.InfoVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                invocation.InfoPrintFaultCalls++;
                if (state.D[2] != 0)
                {
                    Require(state.D[1] == 205 &&
                        Bus.CString(state.D[2]) == "\t",
                        "Info per-device fault code or prefix differs.");
                    invocation.Output.Write(Encoding.Latin1.GetBytes(
                        "\tDisk error\n"));
                }
                return 0;
            });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) => invocation.OutputBptr);
    }

    private void VerifyInfoEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Info!;
        if (invocation.Definition.Name == "source-sorted-output")
            Require(invocation.InfoStricmpCalls > 0,
                "Info did not sort its DOS-list snapshot with utility Stricmp.");
        if (invocation.Definition.Workbench || invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.InfoLockCalls == 0,
                "Info crossed an invalid startup boundary.");
            return;
        }
        if (definition.UtilityLibraryOpenFailure)
        {
            Require(invocation.InfoUtilityLibraryOpens == 1 &&
                invocation.InfoUtilityLibraryCloses == 0 &&
                invocation.InfoLocaleLibraryOpens == 0 &&
                invocation.InfoOpenLocaleCalls == 0 &&
                invocation.InfoReadArgsCalls == 0,
                "Info utility-open failure crossed the source startup boundary.");
            return;
        }
        Require(invocation.InfoUtilityLibraryOpens == 1 &&
            invocation.InfoUtilityLibraryCloses == 1,
            "Info utility.library ownership differs.");
        if (definition.InvalidFilterPattern)
        {
            Require(invocation.InfoReadArgsCalls == 1 &&
                invocation.InfoFreeArgsCalls == 1 &&
                invocation.InfoParsePatternCalls == 1 &&
                invocation.InfoMatchPatternCalls == 0 &&
                invocation.InfoLockCalls == 0 &&
                invocation.InfoAllocVecCalls == 0 &&
                invocation.InfoPathLockCalls == 0 &&
                invocation.InfoPrintFaultCalls == 1 &&
                invocation.IoError == definition.InfoError,
                "Info malformed-pattern boundary differs from source.");
            invocation.InfoLayout = null;
            return;
        }
        if (definition.LocaleLibraryOpenFailure)
        {
            Require(invocation.InfoLocaleLibraryOpens == 1 &&
                invocation.InfoLocaleLibraryCloses == 0 &&
                invocation.InfoOpenLocaleCalls == 0 &&
                invocation.InfoCloseLocaleCalls == 0,
                "Info locale-library failure crossed the optional-provider boundary.");
        }
        else
        {
            Require(invocation.InfoLocaleLibraryOpens == 1 &&
                invocation.InfoLocaleLibraryCloses == 1 &&
                invocation.InfoOpenLocaleCalls == 1 &&
                invocation.InfoCloseLocaleCalls == 1,
                "Info locale.library/default-locale ownership differs.");
        }
        var hasWideCounterApi = definition.DosVersion > 51 ||
            (definition.DosVersion == 51 && definition.DosRevision >= 8);
        var arguments = invocation.Definition.Arguments;
        var hasVolumeSwitch = arguments.Contains("VOLS",
                StringComparison.OrdinalIgnoreCase) ||
            arguments.Contains("VOLUMES", StringComparison.OrdinalIgnoreCase);
        var hasDiskSwitch = arguments.Contains("DISKS",
            StringComparison.OrdinalIgnoreCase);
        var hasDeviceFilter = arguments.Contains("DEVICES",
            StringComparison.OrdinalIgnoreCase);
        var selectsDisks = hasDeviceFilter || hasDiskSwitch ||
            !hasVolumeSwitch;
        var selectedDeviceRows = ExpectedSelectedDeviceRows(definition);
        var shouldQueryCounters = hasWideCounterApi && selectsDisks &&
            definition.ParserError == 0 && !definition.AllocationFailure &&
            !definition.MissingList && !definition.InvalidFilterPattern &&
            definition.DeviceLockSucceeds && definition.InfoSucceeds;
        var expectedCounterQueries = shouldQueryCounters
            ? selectedDeviceRows * 2
            : 0;
        Require(invocation.InfoGetFileSysAttrCalls == expectedCounterQueries,
            $"Info DOS 51.8 counter-query capability gate differs for " +
            $"{invocation.Definition.Name}: " +
            $"{invocation.InfoGetFileSysAttrCalls}/{expectedCounterQueries}.");
        var shouldResolveDeviceNames = selectsDisks &&
            definition.ParserError == 0 && !definition.AllocationFailure &&
            !definition.MissingList && !definition.InvalidFilterPattern &&
            definition.DeviceLockSucceeds && definition.InfoSucceeds;
        var expectedNameFromLockCalls = shouldResolveDeviceNames
            ? selectedDeviceRows : 0;
        Require(invocation.InfoNameFromLockCalls ==
                expectedNameFromLockCalls,
            $"Info NameFromLock call count differs for " +
            $"{invocation.Definition.Name}: " +
            $"{invocation.InfoNameFromLockCalls}/" +
            $"{expectedNameFromLockCalls}.");
        var selectsVolumes = hasVolumeSwitch ||
            (!hasDiskSwitch && !definition.Filter);
        var shouldFormatVolumeDates = selectsVolumes &&
            definition.ParserError == 0 &&
            !definition.AllocationFailure && !definition.MissingList &&
            !definition.CtrlC;
        var hasLocale = !definition.LocaleLibraryOpenFailure &&
            !definition.OpenLocaleFailure;
        var formatsWithLocale = hasLocale &&
            definition.InfoDateTimeFormat is not null;
        var expectedVolumeDates = shouldFormatVolumeDates && !formatsWithLocale
            ? definition.Entries / 2 : 0;
        Require(invocation.DateToStrCalls == expectedVolumeDates,
            $"Info volume DateToStr count differs for {invocation.Definition.Name}: " +
            $"{invocation.DateToStrCalls}/{expectedVolumeDates}.");
        var expectedLocaleFormatDates = shouldFormatVolumeDates &&
            formatsWithLocale ? definition.Entries / 2 : 0;
        Require(invocation.LocaleFormatDateCalls == expectedLocaleFormatDates,
            $"Info volume FormatDate count differs for {invocation.Definition.Name}: " +
            $"{invocation.LocaleFormatDateCalls}/{expectedLocaleFormatDates}.");
        if (definition.ParserError != 0) return;
        if (definition.AllocationFailure)
            return;
        if (definition.MissingList)
            Require(invocation.InfoLockCalls == 1 && invocation.InfoUnlockCalls == 0,
                "Info missing-list lock lifetime differs.");
        else
            Require(invocation.InfoLockCalls == 1 && invocation.InfoUnlockCalls == 1,
                "Info DOS-list lock lifetime differs.");
        if (!definition.MissingList && !definition.CtrlC)
            Require(invocation.InfoNextCalls >= 1, "Info did not traverse DOS list.");
        if (definition.Filter)
            Require(invocation.InfoParsePatternCalls >= 1 &&
                invocation.InfoMatchPatternCalls >= 1,
                "Info wildcard matching did not use DOS APIs.");
        if (definition.CtrlC)
        {
            Require(invocation.InfoPrintFaultCalls == 1,
                "Info Ctrl-C fault publication differs.");
            return;
        }
        if (definition.Disks || (!definition.Disks && !definition.Volumes))
            Require(invocation.InfoFPutsCalls >= 1, "Info device header missing.");
        var shouldAttemptDevices = selectsDisks &&
            !definition.MissingList && !definition.AllocationFailure &&
            !definition.InvalidFilterPattern;
        var expectedDeviceLocks = shouldAttemptDevices
            ? selectedDeviceRows : 0;
        var expectedInfoCalls = definition.DeviceLockSucceeds
            ? expectedDeviceLocks : 0;
        Require(invocation.InfoPathLockCalls == expectedDeviceLocks &&
            invocation.InfoCalls == expectedInfoCalls &&
            invocation.InfoPathUnlockCalls == expectedInfoCalls,
            $"Info per-device Lock/Info/UnLock ownership differs for " +
            $"{invocation.Definition.Name}: " +
            $"{invocation.InfoPathLockCalls}/{expectedDeviceLocks}," +
            $"{invocation.InfoCalls}/{expectedInfoCalls}," +
            $"{invocation.InfoPathUnlockCalls}/{expectedInfoCalls}.");
        invocation.InfoLayout = null;
    }

    private static int ExpectedSelectedDeviceRows(InfoEntryCase definition)
    {
        var pairCount = definition.Entries / 2;
        if (!definition.Filter) return pairCount;
        var pattern = definition.FilterPattern.EndsWith(':')
            ? definition.FilterPattern : definition.FilterPattern + ":";
        var matches = 0;
        for (var slot = 0; slot < pairCount; slot++)
            if (GlobMatch(pattern, $"DH{slot}:")) matches++;
        return matches;
    }

    private static string ExpectedDeviceType(InfoEntryCase definition)
    {
        var startupType = 0x444f5300u;
        if ((definition.StartupUnit & 0xff000000u) == 0 &&
            definition.StartupTableSize >= 16 &&
            definition.StartupDosType != 0)
            startupType = definition.StartupDosType;
        var diskType = (startupType & 0x444f5300u) == 0x444f5300u
            ? definition.InfoDiskType : startupType;
        return diskType switch
        {
            0x444f5300 => "OFS",
            0x444f5301 => "FFS",
            0x53465300 => "SFS",
            0x50465300 => "PFS",
            _ => throw new InvalidOperationException(
                $"Unexpected Info fixture filesystem type: {diskType:x8}.")
        };
    }

    private void PutBString(uint address, string value)
    {
        Bus.Memory[address] = (byte)value.Length;
        Encoding.Latin1.GetBytes(value).CopyTo(Bus.Memory.AsSpan((int)address + 1));
    }

    private void PutCString(uint address, string value)
    {
        Encoding.Latin1.GetBytes(value).CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)value.Length] = 0;
    }

    private static bool GlobMatch(string pattern, string text)
    {
        var p = 0;
        var t = 0;
        var star = -1;
        var retry = 0;
        while (t < text.Length)
        {
            if (p < pattern.Length &&
                (pattern[p] == '?' ||
                 char.ToUpperInvariant(pattern[p]) ==
                    char.ToUpperInvariant(text[t])))
            {
                p++;
                t++;
                continue;
            }
            if (p + 1 < pattern.Length && pattern[p] == '#' &&
                pattern[p + 1] == '?')
            {
                star = p;
                p += 2;
                retry = t;
                continue;
            }
            if (pattern[p..].StartsWith("*", StringComparison.Ordinal))
            {
                star = p++;
                retry = t;
                continue;
            }
            if (star < 0) return false;
            p = star + (pattern[star] == '#' ? 2 : 1);
            t = ++retry;
        }
        while (p + 1 < pattern.Length && pattern[p] == '#' &&
            pattern[p + 1] == '?') p += 2;
        while (p < pattern.Length && pattern[p] == '*') p++;
        return p == pattern.Length;
    }
}
