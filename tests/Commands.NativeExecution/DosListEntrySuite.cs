using System.Text;
using System.Globalization;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record DosListEntryCase(bool Devices = false,
    bool Volumes = false, bool Assigns = false, bool Verbose = false,
    string? Name = null, uint Address = 0, int LockFailure = 0,
    bool AllocationFailure = false, bool CtrlC = false,
    int ParserError = 0, int DeviceEntries = 1, int VolumeEntries = 1,
    int AssignEntries = 1, bool MountedProcess = false,
    bool SignalPort = false, bool NameAttributeFailure = false,
    bool PortAttributeFailure = false,
    bool VerboseAttributeFailure = false, string? AssignTarget = null,
    bool AssignNameAttributeFailure = false,
    bool AssignLockAttributeFailure = false,
    bool AssignLockNameFailure = false, uint AssignLockBptr = 0,
    bool AssignListAttributeFailure = false, int AssignListEntries = 0,
    bool VolumeDateAttributeFailure = false,
    bool VolumeDateToStrFailure = false,
    bool VolumeDiskTypeAttributeFailure = false,
    bool VolumeLockListAttributeFailure = false,
    bool VolumeLockNameFailure = false,
    int VolumeLockEntries = 1,
    uint VolumeDays = 700000, uint VolumeMinutes = 321,
    uint VolumeTicks = 45, uint VolumeDiskType = 0x444f5300u);

internal sealed record DosListNativeLayout(uint Control, uint DeviceList,
    uint VolumeList, uint AssignList, uint DeviceNode, uint VolumeNode,
    uint AssignNode, uint DeviceName, uint VolumeName, uint AssignName,
    uint AddressSlot, uint NameText, uint Process, uint ProcessPort,
    uint SignalProcess, uint SignalPort, uint ProcessName,
    uint AssignTarget, uint AssignListRecord, uint AssignFileLock,
    uint AssignListPath, uint VolumeLockFileLock, uint VolumeLockPath)
{
    public uint ControlAddress => Control;
}

internal sealed partial class ProbeFixture
{
    public const string DosListEntrySuite =
        "morphos320-doslist-native-entry-vector-fixture";

    private const string DeviceLine = "<{0}> DeviceNode 0x{1:x}\n";
    private const string AssignLine = "<{0}> AssignNode 0x{1:x}\n";
    private const string ProcessLine = "  Process 0x{0:x} <{1}>\n";

    private List<object> RunDosListEntryCases()
    {
        ProbeCase[] cases =
        [
            DosCase("default", new(), output: DosOutput(true, true, true)),
            DosCase("three-node-lists", new(DeviceEntries: 3,
                VolumeEntries: 3, AssignEntries: 3),
                output: DosOutput(true, true, true, entries: 3)),
            DosCase("devices", new(Devices: true, Volumes: false,
                Assigns: false), "DEVICES", DosOutput(true, false, false)),
            DosCase("volumes", new(Volumes: true), "VOLUMES",
                DosOutput(false, true, false)),
            DosCase("assigns", new(Assigns: true), "ASSIGNS",
                DosOutput(false, false, true)),
            DosCase("name-filter", new(Name: "DH0"), "NAME DH0",
                DosOutput(true, false, false)),
            DosCase("address-filter", new(Address: 0x00031000),
                "ADDRESS 0x00031000", DosOutput(true, false, false)),
            DosCase("verbose", new(Verbose: true), "VERBOSE",
                DosOutput(true, true, true, true)),
            DosCase("volume-verbose-date-format-failure", new(
                Volumes: true, Verbose: true,
                VolumeDateToStrFailure: true), "VOLUMES VERBOSE",
                DosOutput(false, true, false, true,
                    volumeDateToStrFailure: true)),
            DosCase("volume-verbose-optional-attribute-failures", new(
                Volumes: true, Verbose: true,
                VolumeDateAttributeFailure: true,
                VolumeDiskTypeAttributeFailure: true,
                VolumeLockListAttributeFailure: true), "VOLUMES VERBOSE",
                DosOutput(false, true, false, true,
                    volumeAttributeFailure: true)),
            DosCase("volume-verbose-unresolvable-lock", new(
                Volumes: true, Verbose: true,
                VolumeLockNameFailure: true), "VOLUMES VERBOSE",
                DosOutput(false, true, false, true,
                    volumeLockNameFailure: true)),
            DosCase("volume-verbose-multiple-locks", new(
                Volumes: true, Verbose: true,
                VolumeLockEntries: 3), "VOLUMES VERBOSE",
                DosOutput(false, true, false, true,
                    volumeLockEntries: 3)),
            DosCase("verbose-optional-device-attribute-failure", new(
                Devices: true, Volumes: false, Assigns: false,
                Verbose: true, VerboseAttributeFailure: true),
                "DEVICES VERBOSE", DosOutput(true, false, false, true,
                    verboseAttributeFailure: true)),
            DosCase("assign-deferred-target", new(Assigns: true,
                AssignTarget: "DH0:"), "ASSIGNS",
                "<SYS> AssignNode 0x33000\n" +
                "<SYS> AssignNode 0x33000 defered to <DH0:>\n"),
            DosCase("assign-null-target-lock-list-verbose", new(
                Assigns: true, Verbose: true, AssignLockBptr: 0x0003_9000u >> 2,
                AssignListEntries: 1), "ASSIGNS VERBOSE",
                "<SYS> AssignNode 0x33000\n" +
                "<SYS> AssignNode 0x33000\n" +
                "  Not Mounted\n" +
                "  Lock 0xe400 <DH0:>\n" +
                "  Type 1\n" +
                "  + Lock 0xe400 <DH0:>\n" +
                "    Link 0x39000 Key 0x0 Access 0\n" +
                "    No Filesystem MsgPort\n"),
            DosCase("assign-name-query-failure", new(Assigns: true,
                AssignNameAttributeFailure: true), "ASSIGNS",
                "<SYS> AssignNode 0x33000\n"),
            DosCase("assign-lock-query-failure", new(Assigns: true,
                AssignLockAttributeFailure: true), "ASSIGNS",
                "<SYS> AssignNode 0x33000\n" +
                "<SYS> AssignNode 0x33000\n" +
                "  Not Mounted\n" +
                "    Lock argument failed\n"),
            DosCase("assign-lock-name-failure", new(Assigns: true,
                AssignLockBptr: 0x0003_9000u >> 2,
                AssignLockNameFailure: true), "ASSIGNS",
                "<SYS> AssignNode 0x33000\n" +
                "<SYS> AssignNode 0x33000\n" +
                "  Not Mounted\n" +
                "    Lock 0xe400 <Not Resolvable>\n"),
            DosCase("assign-null-mounted-process", new(Assigns: true,
                MountedProcess: true), "ASSIGNS",
                "<SYS> AssignNode 0x33000\n" +
                "<SYS> AssignNode 0x33000\n" +
                "  Process 0xf00000 <FileSystem>\n" +
                "    Lock 0x0 <Not Resolvable>\n"),
            DosCase("mounted-device-process-port", new(Devices: true,
                Volumes: false, Assigns: false, MountedProcess: true),
                "DEVICES", DosOutput(true, false, false,
                    processAddress: 0x00f00000u)),
            DosCase("mounted-device-signal-port", new(Devices: true,
                Volumes: false, Assigns: false, MountedProcess: true,
                SignalPort: true), "DEVICES",
                DosOutput(true, false, false, processAddress: 0x00f00200u)),
            DosCase("name-attribute-unavailable", new(Devices: true,
                Volumes: false, Assigns: false,
                NameAttributeFailure: true), "DEVICES",
                DosOutput(true, false, false, missingName: true)),
            DosCase("port-attribute-unavailable", new(Devices: true,
                Volumes: false, Assigns: false,
                PortAttributeFailure: true), "DEVICES",
                DosOutput(true, false, false, missingPort: true)),
            DosCase("name-filter-name-attribute-unavailable", new(
                Devices: true, Volumes: false, Assigns: false, Name: "DH0",
                NameAttributeFailure: true), "NAME DH0 DEVICES", ""),
            DosCase("missing-device-list", new(LockFailure: 1),
                output: DosOutput(false, true, true)),
            DosCase("missing-volume-list", new(LockFailure: 2),
                output: DosOutput(true, false, true)),
            DosCase("allocation-failure", new(AllocationFailure: true),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.NoFreeStore),
            DosCase("ctrl-c", new(CtrlC: true), result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.Break),
            DosCase("parser-failure", new(ParserError: 116),
                result: DOS.RETURN_ERROR, error: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { DosList = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { DosList = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { DosList = new(), EntryLength = 4, NullArgumentPointer = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            DosCase("interleaved-left", new(Devices: true), "DEVICES",
                DosOutput(true, false, false)),
            DosCase("interleaved-right", new(Assigns: true), "ASSIGNS",
                DosOutput(false, false, true))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase DosCase(string name, DosListEntryCase definition,
        string arguments = "", string output = "", int result = DOS.RETURN_OK,
        int error = 0)
    {
        return new(name, arguments, result, error, output)
        { DosList = definition };
    }

    private static string DosOutput(bool device, bool volume, bool assign,
        bool verbose = false, int entries = 1,
        uint processAddress = 0, bool missingName = false,
        bool missingPort = false, bool verboseAttributeFailure = false,
        bool volumeDateToStrFailure = false,
        bool volumeAttributeFailure = false,
        bool volumeLockNameFailure = false,
        int volumeLockEntries = 1)
    {
        var text = new StringBuilder();
        if (device)
        {
            for (var slot = 0; slot < entries; slot++)
            {
                var name = slot == 0 ? "DH0" : "DH" + slot;
                text.Append(missingName
                    ? string.Format("DeviceNode 0x{0:x}\n",
                        0x00031000u + (uint)slot * 0x100u)
                    : string.Format(DeviceLine, name,
                        0x00031000u + (uint)slot * 0x100u));
                if (!missingPort)
                    text.Append(processAddress == 0 ? "  Not Mounted\n" :
                        string.Format(ProcessLine, processAddress, "FileSystem"));
                if (verbose)
                    text.Append(VerboseDeviceRows(
                        verboseAttributeFailure));
            }
        }
        if (volume)
        {
            for (var slot = 0; slot < entries; slot++)
            {
                var name = slot == 0 ? "Work" : "Work" + slot;
                text.Append(string.Format(DeviceLine, name,
                    0x00032000u + (uint)slot * 0x100u));
                text.Append("  Not Mounted\n");
                if (verbose && !volumeAttributeFailure)
                {
                    if (volumeDateToStrFailure)
                        text.Append("  <illegal date>\n" +
                            "  Days 700000 Minutes 321 Ticks 45\n");
                    else
                        text.Append("  12:34:56 Mon 01-Jan-90\n");
                    text.Append("  DiskType 0x444f5300\n");
                    for (var lockSlot = 0; lockSlot < volumeLockEntries;
                        lockSlot++)
                    {
                        var lockAddress = 0x0003b000u +
                            (uint)lockSlot * 0x100u;
                        text.Append(volumeLockNameFailure
                            ? string.Format(
                                "    Lock 0x{0:x} <Not Resolvable>\n",
                                lockAddress)
                            : string.Format("    Lock 0x{0:x} <DH0:>\n",
                                lockAddress));
                    }
                }
            }
        }
        if (assign)
        {
            for (var slot = 0; slot < entries; slot++)
            {
                var name = slot == 0 ? "SYS" : "SYS" + slot;
                text.Append(string.Format(AssignLine, name,
                    0x00033000u + (uint)slot * 0x100u));
                text.Append(string.Format(AssignLine, name,
                    0x00033000u + (uint)slot * 0x100u));
                text.Append("  Not Mounted\n");
                text.Append("    Lock 0x0 <Not Resolvable>\n");
                if (verbose) text.Append("  Type 1\n");
            }
        }
        return text.ToString();
    }

    private static string VerboseDeviceRows(bool omitSerialId) =>
        "  Handler <scsi.device>\n" +
        "  StackSize 4096\n" +
        "  Priority -5\n" +
        "  SegList 0xdead\n" +
        "  GlobalVec -1\n" +
        (omitSerialId ? "" : "  SerialID <V49.1>\n") +
        "  FileSysStartupMsg 0xf01000\n" +
        "    <scsi.device:2> Flags 0x1234\n" +
        "  Environment 0xf02000\n" +
        "    SizeBlock 512\n" +
        "    SecOrg 0\n" +
        "    Surfaces 2\n" +
        "    SectorsPerBlock 1\n" +
        "    BlocksPerTrack 32\n" +
        "    Reserved 2\n" +
        "    PreAlloc 3\n" +
        "    Interleave 1\n" +
        "    LowCyl 0\n" +
        "    HighCyl 79\n" +
        "    NumBuffers 30\n" +
        "    BufMemType 1\n" +
        "    MaxTransfer 0x7fffffff\n" +
        "    Mask 0x7ffffffe\n" +
        "    BootPri -5\n" +
        "    DosType 0x444f5301\n" +
        "    Baud 9600\n" +
        "    BootBlocks 2\n" +
        "    Control <head=1>\n" +
        "    TableSize 20\n" +
        "  Startup <FileSystem startup>\n" +
        "  Startup 65537\n";

    private void PrepareDosListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.DosList ?? new();
        Require(definition.DeviceEntries is >= 0 and <= 3 &&
            definition.VolumeEntries is >= 0 and <= 3 &&
            definition.AssignEntries is >= 0 and <= 3,
            "DOSList fixture only supports bounded empty-to-three-node lists.");

        var control = invocation.Process + 0x200;
        var layout = new DosListNativeLayout(control, 0x30000,
            0x30020, 0x30040, 0x31000, 0x32000, 0x33000,
            control + 64, control + 256, control + 448, control + 640,
            control + 704, 0x00f00000u, 0x00f00000u +
                (uint)DosLayout.Process.MessagePort, 0x00f00200u,
            0x00f00400u, 0x00f00600u, 0x00037000u, 0x00038000u,
            0x00039000u, 0x0003a000u, 0x0003b000u, 0x0003c000u);
        invocation.DosListLayout = layout;

        for (var slot = 0; slot < 3; slot++)
        {
            PutBString(layout.DeviceName + (uint)slot * 0x20u,
                slot == 0 ? "DH0" : "DH" + slot);
            PutBString(layout.VolumeName + (uint)slot * 0x20u,
                slot == 0 ? "Work" : "Work" + slot);
            PutBString(layout.AssignName + (uint)slot * 0x20u,
                slot == 0 ? "SYS" : "SYS" + slot);
        }
        PutCString(layout.NameText, definition.Name ?? "");
        PutCString(layout.AssignTarget, definition.AssignTarget ?? "");
        Bus.Long(layout.AddressSlot, definition.Address);
        Require(definition.AssignListEntries is >= 0 and <= 3,
            "DOSList fixture only supports zero-to-three assign list locks.");
        Require(definition.VolumeLockEntries is >= 0 and <= 3,
            "DOSList fixture only supports zero-to-three volume locks.");
        for (var slot = 0; slot < 3; slot++)
        {
            var offset = (uint)slot * 0x100u;
            Bus.Long(layout.DeviceNode + offset +
                (uint)DosLayout.DosList.Name,
                (layout.DeviceName + (uint)slot * 0x20u) >> 2);
            Bus.Long(layout.VolumeNode + offset +
                (uint)DosLayout.DosList.Name,
                (layout.VolumeName + (uint)slot * 0x20u) >> 2);
            Bus.Long(layout.AssignNode + offset +
                (uint)DosLayout.DosList.Name,
                (layout.AssignName + (uint)slot * 0x20u) >> 2);
            Bus.Long(layout.DeviceNode + offset +
                (uint)DosLayout.DosList.Task,
                slot == 0 && definition.MountedProcess
                    ? definition.SignalPort ? layout.SignalPort :
                        layout.ProcessPort
                    : 0u);
            Bus.Long(layout.VolumeNode + offset +
                (uint)DosLayout.DosList.Task, 0);
            Bus.Long(layout.AssignNode + offset +
                (uint)DosLayout.DosList.Task,
                slot == 0 && definition.MountedProcess
                    ? definition.SignalPort ? layout.SignalPort :
                        layout.ProcessPort
                    : 0u);
            Bus.Long(layout.DeviceNode + offset +
                (uint)DosLayout.DosList.Type, (uint)DosListType.Device);
            Bus.Long(layout.VolumeNode + offset +
                (uint)DosLayout.DosList.Type, (uint)DosListType.Volume);
            Bus.Long(layout.AssignNode + offset +
                (uint)DosLayout.DosList.Type, 1);
        }

        var assignListAddress = 0u;
        for (var slot = definition.AssignListEntries - 1; slot >= 0; slot--)
        {
            var listAddress = layout.AssignListRecord + (uint)slot * 0x20u;
            var fileLock = layout.AssignFileLock + (uint)slot * 0x100u;
            var lockBptr = fileLock >> 2;
            var lockPath = layout.AssignListPath + (uint)slot * 0x100u;
            PutCString(lockPath, "DH0:");
            Bus.Long(listAddress + (uint)DosLayout.AssignList.Next,
                assignListAddress);
            Bus.Long(listAddress + (uint)DosLayout.AssignList.Lock,
                lockBptr);
            Bus.Long(fileLock + (uint)DosLayout.FileLock.Link, 0);
            Bus.Long(fileLock + (uint)DosLayout.FileLock.Key, 0);
            Bus.Long(fileLock + (uint)DosLayout.FileLock.Access, 0);
            Bus.Long(fileLock + (uint)DosLayout.FileLock.Task, 0);
            Bus.Long(fileLock + (uint)DosLayout.FileLock.Volume, 0);
            assignListAddress = listAddress;
        }

        var nextVolumeLock = 0u;
        for (var slot = definition.VolumeLockEntries - 1; slot >= 0; slot--)
        {
            var fileLock = layout.VolumeLockFileLock + (uint)slot * 0x100u;
            var lockPath = layout.VolumeLockPath + (uint)slot * 0x100u;
            PutCString(lockPath, "DH0:");
            Bus.Long(fileLock + (uint)DosLayout.FileLock.Link,
                nextVolumeLock);
            Bus.Long(fileLock + (uint)DosLayout.FileLock.Key, 0);
            Bus.Long(fileLock + (uint)DosLayout.FileLock.Access, 0);
            Bus.Long(fileLock + (uint)DosLayout.FileLock.Task, 0);
            Bus.Long(fileLock + (uint)DosLayout.FileLock.Volume, 0);
            nextVolumeLock = fileLock >> 2;
        }

        PrepareProcess(layout.Process, layout.ProcessPort,
            layout.ProcessName, signalPort: false);
        PrepareProcess(layout.SignalProcess, layout.SignalPort,
            layout.ProcessName, signalPort: true);
    }

    private void PrepareProcess(uint process, uint port, uint processName,
        bool signalPort)
    {
        PutCString(processName, "FileSystem");
        Bus.Long(process + (uint)ExecLayout.Node.Name, processName);
        Bus.Memory[process + (uint)ExecLayout.Node.Type] =
            (byte)NodeType.Process;
        Bus.Memory[port + (uint)ExecLayout.MsgPort.Flags] =
            (byte)(signalPort ? PortFlags.Signal : PortFlags.SoftInterrupt);
        if (signalPort)
            Bus.Long(port + (uint)ExecLayout.MsgPort.SignalTask, process);
    }

    private void RegisterDosListEntryExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state,
            invocation) =>
        {
            Require(state.D[0] == 0 && state.D[1] == 0,
                "DOSList Ctrl-C query ABI differs.");
            invocation.DosListSetSignalCalls++;
            return invocation.Definition.DosList!.CtrlC ? 1u << 12 : 0;
        });
        Register(ExecBase, ExecLvo.TypeOfMem, "TypeOfMem", (state,
            invocation) =>
        {
            var layout = invocation.DosListLayout!;
            var address = state.A[1];
            invocation.Events.Add($"DosListTypeOfMem:{address:x8}");
            return address == layout.ProcessPort ||
                address == layout.Process ||
                address == layout.SignalPort ||
                address == layout.SignalProcess ||
                address >= layout.AssignFileLock &&
                    address < layout.AssignFileLock + 0x300u ? 1u : 0u;
        });
    }

    private void RegisterDosListEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.DosList!;
            Require(Bus.CString(state.D[1]) ==
                NativeMorphOSDosListCommand.Template,
                "DOSList template differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2],
                "DosListResults").Size == 24,
                "DOSList results differ.");
            invocation.DosListReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }

            var result = state.D[2];
            var text = invocation.Definition.Arguments;
            Bus.Long(result, text.StartsWith("NAME ",
                StringComparison.OrdinalIgnoreCase)
                ? invocation.DosListLayout!.NameText : 0);
            Bus.Long(result + 4, text.StartsWith("ADDRESS ",
                StringComparison.OrdinalIgnoreCase)
                ? invocation.DosListLayout!.AddressSlot : 0);
            Bus.Long(result + 8, text.Contains("DEVICES",
                StringComparison.OrdinalIgnoreCase) ? 1u : 0);
            Bus.Long(result + 12, text.Contains("VOLUMES",
                StringComparison.OrdinalIgnoreCase) ? 1u : 0);
            Bus.Long(result + 16, text.Contains("ASSIGNS",
                StringComparison.OrdinalIgnoreCase) ? 1u : 0);
            Bus.Long(result + 20, text.Contains("VERBOSE",
                StringComparison.OrdinalIgnoreCase) ? 1u : 0);
            return Bus.Allocate(invocation, 24, "DosListRdArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            Bus.Release(invocation, state.D[1], "DosListRdArgs");
            invocation.DosListFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.AttemptLockDosList,
            "AttemptLockDosList", (state, invocation) =>
        {
            var definition = invocation.Definition.DosList!;
            Require((state.D[1] & (uint)DosListLockFlags.Read) != 0,
                "DOSList lock must include Read.");
            invocation.DosListAttemptLockCalls++;
            invocation.DosListCurrentKind = state.D[1] &
                ((uint)DosListLockFlags.Devices |
                 (uint)DosListLockFlags.Volumes |
                 (uint)DosListLockFlags.Assigns);
            invocation.Events.Add($"DosListLock:{state.D[1]}:{invocation.DosListCurrentKind}");
            var refused = (invocation.Definition.Name == "missing-device-list" &&
                    invocation.DosListCurrentKind ==
                        (uint)DosListLockFlags.Devices) ||
                (invocation.Definition.Name == "missing-volume-list" &&
                    invocation.DosListCurrentKind ==
                        (uint)DosListLockFlags.Volumes) ||
                (invocation.Definition.Name == "missing-assign-list" &&
                    invocation.DosListCurrentKind ==
                        (uint)DosListLockFlags.Assigns);
            if (refused) return 0;
            var list = invocation.DosListCurrentKind ==
                (uint)DosListLockFlags.Devices
                ? invocation.DosListLayout!.DeviceList
                : invocation.DosListCurrentKind ==
                    (uint)DosListLockFlags.Volumes
                    ? invocation.DosListLayout!.VolumeList
                    : invocation.DosListLayout!.AssignList;
            return list;
        });
        Register(baseAddress, DosLvo.NextDosEntry, "NextDosEntry",
            (state, invocation) =>
            {
                var kind = state.D[2];
                Require(state.D[1] != 0 && kind ==
                    invocation.DosListCurrentKind,
                    $"DOSList traversal flags differ: requested={kind}, locked={invocation.DosListCurrentKind}.");
                invocation.DosListNextCalls++;
                var definition = invocation.Definition.DosList!;
                if (kind ==
                    (uint)DosListLockFlags.Devices)
                {
                    invocation.DosListDeviceNextCalls++;
                    return invocation.DosListDeviceNextCalls <=
                        definition.DeviceEntries
                        ? invocation.DosListLayout!.DeviceNode +
                            (uint)(invocation.DosListDeviceNextCalls - 1) * 0x100u : 0u;
                }
                if (kind ==
                    (uint)DosListLockFlags.Volumes)
                {
                    invocation.DosListVolumeNextCalls++;
                    return invocation.DosListVolumeNextCalls <=
                        definition.VolumeEntries
                        ? invocation.DosListLayout!.VolumeNode +
                            (uint)(invocation.DosListVolumeNextCalls - 1) * 0x100u : 0u;
                }
                invocation.DosListAssignNextCalls++;
                return invocation.DosListAssignNextCalls <=
                    definition.AssignEntries
                    ? invocation.DosListLayout!.AssignNode +
                        (uint)(invocation.DosListAssignNextCalls - 1) * 0x100u : 0u;
            });
        Register(baseAddress, DosLvo.UnLockDosList, "UnLockDosList",
            (state, invocation) =>
            {
                Require(state.D[1] == ((uint)DosListLockFlags.Read |
                    invocation.DosListCurrentKind),
                    "DOSList unlock flags differ.");
                invocation.DosListUnlockCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.GetDosObjectAttr,
            "GetDosObjectAttrTagList", (state, invocation) =>
            {
                var layout = invocation.DosListLayout!;
                var objectType = (DosObjectType)state.D[1];
                var node = state.D[2];
                var tagList = state.D[3];
                var (nameTag, portTag, expectedName) = node switch
                {
                    >= 0x31000 and < 0x31300 =>
                        (DosObjectTag.DeviceNodeName,
                         DosObjectTag.DeviceNodeMessagePort,
                         NodeName("DH", node, 0x31000)),
                    >= 0x32000 and < 0x32300 =>
                        (DosObjectTag.VolumeNodeName,
                         DosObjectTag.VolumeNodeMessagePort,
                         NodeName("Work", node, 0x32000)),
                    >= 0x33000 and < 0x33300 =>
                        (DosObjectTag.AssignNodeName,
                         DosObjectTag.AssignNodeMessagePort,
                         NodeName("SYS", node, 0x33000)),
                    _ => throw new InvalidOperationException(
                        "DOSList object attribute node is unknown.")
                };
                var expectedType = node switch
                {
                    >= 0x31000 and < 0x31300 => DosObjectType.DeviceNode,
                    >= 0x32000 and < 0x32300 => DosObjectType.VolumeNode,
                    _ => DosObjectType.AssignNode
                };
                Require(objectType == expectedType,
                    "DOSList object attribute type differs.");
                if (Bus.Long(tagList) == (uint)nameTag)
                {
                    Require(Bus.Long(tagList + 8) == 0,
                        "DOSList name query must terminate after one tag.");
                    var nameAttribute = Bus.Long(tagList + 4);
                    var nameBuffer = Bus.Long(nameAttribute);
                    var nameCapacity = Bus.Long(nameAttribute + 4);
                    var nameBytes = Encoding.Latin1.GetBytes(expectedName + "\0");
                    Require(nameCapacity >= (uint)nameBytes.Length,
                        "DOSList name attribute buffer is too short.");
                    invocation.Events.Add($"DosListNameAttr:{node:x8}");
                    if (invocation.Definition.DosList!.NameAttributeFailure)
                        return 0;
                    nameBytes.CopyTo(Bus.Memory.AsSpan((int)nameBuffer));
                    return 1;
                }

                var requestedTag = (DosObjectTag)Bus.Long(tagList);
                Require(Bus.Long(tagList + 8) == 0,
                    "DOSList attribute query must terminate after one tag.");
                var attribute = Bus.Long(tagList + 4);
                if (requestedTag == portTag)
                {
                    var portValue = Bus.Long(node +
                        (uint)DosLayout.DosList.Task);
                    Bus.Long(attribute, portValue);
                    Bus.Long(attribute + 4, 4);
                    invocation.Events.Add($"DosListPortAttr:{node:x8}");
                    if (invocation.Definition.DosList!.PortAttributeFailure)
                        return 0;
                    return 1;
                }

                if (objectType == DosObjectType.AssignNode)
                {
                    var definition = invocation.Definition.DosList!;
                    uint value;
                    switch (requestedTag)
                    {
                        case DosObjectTag.AssignNodeAssignName:
                            if (definition.AssignNameAttributeFailure)
                                return 0;
                            if (definition.AssignTarget is null)
                            {
                                Bus.Long(attribute, 0);
                                return 1;
                            }
                            var targetBuffer = Bus.Long(attribute);
                            var targetCapacity = Bus.Long(attribute + 4);
                            var targetBytes = Encoding.Latin1.GetBytes(
                                definition.AssignTarget + "\0");
                            Require(targetCapacity >=
                                (uint)targetBytes.Length,
                                "DOSList assign-name buffer is too short.");
                            targetBytes.CopyTo(
                                Bus.Memory.AsSpan((int)targetBuffer));
                            invocation.Events.Add(
                                $"DosListAssignAttr:{requestedTag}");
                            return 1;
                        case DosObjectTag.AssignNodeLock:
                            if (definition.AssignLockAttributeFailure)
                                return 0;
                            value = definition.AssignLockBptr;
                            break;
                        case DosObjectTag.AssignNodeAssignList:
                            if (definition.AssignListAttributeFailure)
                                return 0;
                            value = definition.AssignListEntries == 0 ? 0u :
                                layout.AssignListRecord;
                            break;
                        case DosObjectTag.AssignNodeType:
                            value = 1;
                            break;
                        default:
                            throw new InvalidOperationException(
                                $"DOSList requested unsupported assign attribute {requestedTag}.");
                    }
                    Bus.Long(attribute, value);
                    Bus.Long(attribute + 4, 4);
                    invocation.Events.Add($"DosListAssignAttr:{requestedTag}");
                    return 1;
                }

                if (objectType == DosObjectType.VolumeNode)
                {
                    var definition = invocation.Definition.DosList!;
                    uint value;
                    switch (requestedTag)
                    {
                        case DosObjectTag.VolumeNodeDate:
                            if (definition.VolumeDateAttributeFailure)
                                return 0;
                            var stamp = Bus.Long(attribute);
                            Require(Bus.Long(attribute + 4) ==
                                DosLayout.DateStamp.Size,
                                "DOSList volume date buffer length differs.");
                            Bus.Long(stamp + DosLayout.DateStamp.Days,
                                definition.VolumeDays);
                            Bus.Long(stamp + DosLayout.DateStamp.Minutes,
                                definition.VolumeMinutes);
                            Bus.Long(stamp + DosLayout.DateStamp.Ticks,
                                definition.VolumeTicks);
                            invocation.Events.Add("DosListVolumeDate");
                            return 1;
                        case DosObjectTag.VolumeNodeDiskType:
                            if (definition.VolumeDiskTypeAttributeFailure)
                                return 0;
                            value = definition.VolumeDiskType;
                            break;
                        case DosObjectTag.VolumeNodeLockList:
                            if (definition.VolumeLockListAttributeFailure)
                                return 0;
                            value = definition.VolumeLockEntries == 0 ? 0u :
                                layout.VolumeLockFileLock >> 2;
                            break;
                        default:
                            throw new InvalidOperationException(
                                $"DOSList requested unsupported volume attribute {requestedTag}.");
                    }
                    Bus.Long(attribute, value);
                    Bus.Long(attribute + 4, 4);
                    invocation.Events.Add($"DosListVolumeAttr:{requestedTag}");
                    return 1;
                }

                Require(objectType == DosObjectType.DeviceNode,
                    "DOSList requested device verbose data for a non-device node.");
                var supportedAttribute = TryDosListVerboseAttribute(
                    requestedTag, out var textValue, out var scalarValue);
                Require(supportedAttribute,
                    $"DOSList requested unsupported verbose attribute {requestedTag}.");
                invocation.Events.Add($"DosListVerboseAttr:{requestedTag}");
                if (invocation.Definition.DosList!.VerboseAttributeFailure &&
                    requestedTag == DosObjectTag.DeviceNodeSerialId)
                    return 0;
                if (textValue is not null)
                {
                    var destination = Bus.Long(attribute);
                    var capacity = Bus.Long(attribute + 4);
                    var bytes = Encoding.Latin1.GetBytes(textValue + "\0");
                    Require(capacity >= (uint)bytes.Length,
                        "DOSList verbose string buffer is too short.");
                    bytes.CopyTo(Bus.Memory.AsSpan((int)destination));
                }
                else
                {
                    Bus.Long(attribute, scalarValue);
                    Bus.Long(attribute + 4, 4);
                }
                return 1;
            });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state,
            invocation) =>
        {
            var format = Bus.CString(state.D[1]);
            var args = state.D[2];
            if (format == "  Process 0x%lx <%s>\n")
            {
                var process = Bus.Long(args);
                var name = Bus.CString(Bus.Long(args + 4));
                Require((process == invocation.DosListLayout!.Process ||
                    process == invocation.DosListLayout.SignalProcess) &&
                    name == "FileSystem",
                    "DOSList mounted-process row differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    string.Format(ProcessLine, process, name)));
            }
            else if (IsDosListNodeFormat(format))
            {
                var hasName = format.Contains("%s", StringComparison.Ordinal);
                var name = hasName ? Bus.CString(Bus.Long(args)) : "";
                var address = hasName ? Bus.Long(args + 4) : Bus.Long(args);
                var expectedName = NodeNameForAddress(address);
                var expectedKind = address >= 0x33000 && address < 0x33300;
                var expectedFormat = hasName
                    ? expectedKind ? "<%s> AssignNode 0x%lx\n" :
                        "<%s> DeviceNode 0x%lx\n"
                    : expectedKind ? "AssignNode 0x%lx\n" :
                        "DeviceNode 0x%lx\n";
                Require(expectedName is not null && format == expectedFormat &&
                    (!hasName || name == expectedName),
                    $"DOSList rendered node differs: format={format}, name={name}, address={address:x8}.");
                var output = hasName
                    ? expectedKind ? string.Format(AssignLine, name, address) :
                        string.Format(DeviceLine, name, address)
                    : string.Format(expectedKind ? "AssignNode 0x{0:x}\n" :
                        "DeviceNode 0x{0:x}\n", address);
                invocation.Output.Write(Encoding.Latin1.GetBytes(output));
            }
            else
            {
                Require(IsDosListVerboseFormat(format),
                    $"DOSList emitted unknown VPrintf format: {format}");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    FormatDosListVPrintf(format, args)));
            }
            invocation.DosListVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.NameFromLock, "NameFromLock",
            (state, invocation) =>
            {
                var lockBptr = state.D[1];
                invocation.Events.Add($"DosListNameFromLock:{lockBptr:x8}");
                var definition = invocation.Definition.DosList!;
                var isVolumeLock = lockBptr >=
                        (invocation.DosListLayout!.VolumeLockFileLock >> 2) &&
                    lockBptr < ((invocation.DosListLayout.VolumeLockFileLock +
                        0x300u) >> 2);
                if (lockBptr == 0 || definition.AssignLockNameFailure ||
                    isVolumeLock && definition.VolumeLockNameFailure)
                    return 0;
                var bytes = Encoding.Latin1.GetBytes("DH0:\0");
                Require(state.D[3] >= (uint)bytes.Length,
                    "DOSList NameFromLock buffer is too short.");
                bytes.CopyTo(Bus.Memory.AsSpan((int)state.D[2]));
                return 1;
            });
        Register(baseAddress, DosLvo.DateToStr, "DateToStr",
            (state, invocation) =>
            {
                var definition = invocation.Definition.DosList!;
                var dateTime = state.D[1];
                Require(Bus.Memory[dateTime +
                        (uint)DosLayout.DateTime.Format] ==
                        (byte)DosDateFormat.Dos &&
                    Bus.Memory[dateTime +
                        (uint)DosLayout.DateTime.Flags] == 0 &&
                    Bus.Long(dateTime +
                        (uint)DosLayout.DateTime.Stamp +
                        (uint)DosLayout.DateStamp.Days) ==
                        definition.VolumeDays &&
                    Bus.Long(dateTime +
                        (uint)DosLayout.DateTime.Stamp +
                        (uint)DosLayout.DateStamp.Minutes) ==
                        definition.VolumeMinutes &&
                    Bus.Long(dateTime +
                        (uint)DosLayout.DateTime.Stamp +
                        (uint)DosLayout.DateStamp.Ticks) ==
                        definition.VolumeTicks,
                    "DOSList DateToStr structure or stamp differs.");
                var time = Bus.Long(dateTime +
                    (uint)DosLayout.DateTime.Day);
                var day = Bus.Long(dateTime +
                    (uint)DosLayout.DateTime.Date);
                var date = Bus.Long(dateTime +
                    (uint)DosLayout.DateTime.Time);
                Require(time != 0 && day != 0 && date != 0,
                    "DOSList DateToStr output buffers differ.");
                invocation.DateToStrCalls++;
                if (definition.VolumeDateToStrFailure) return 0;
                PutCString(time, "12:34:56");
                PutCString(day, "Mon");
                PutCString(date, "01-Jan-90");
                return 1;
            });
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            var text = Bus.CString(state.D[2]);
            Require(text is "  Not Mounted\n" or
                "    Lock argument failed\n" or
                "    No Filesystem MsgPort\n" or
                "  <illegal date>\n",
                "DOSList verbose text differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.DosListFPutsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (_,
            invocation) =>
        {
            invocation.DosListPrintFaultCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state,
            invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
    }

    private void VerifyDosListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.DosList!;
        if (invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.DosListAttemptLockCalls == 0,
                "DOSList crossed an invalid startup boundary.");
            return;
        }
        if (definition.ParserError != 0 || definition.AllocationFailure)
        {
            Require(invocation.DosListAttemptLockCalls == 0,
                "DOSList traversed after an early failure.");
            return;
        }

        var selected = (definition.Devices ? 1 : 0) +
            (definition.Volumes ? 1 : 0) + (definition.Assigns ? 1 : 0);
        if (selected == 0) selected = 3;
        var refused = definition.LockFailure != 0 ? 1 : 0;
        var expectedAttempts = definition.CtrlC ? 1 : selected;
        Require(invocation.DosListReadArgsCalls == 1 &&
            invocation.DosListFreeArgsCalls == 1 &&
            invocation.DosListAllocMemCalls == 1 &&
            invocation.DosListFreeMemCalls == 1 &&
            invocation.DosListAllocVecCalls == 1 &&
            invocation.DosListFreeVecCalls == 1 &&
            invocation.DosListAttemptLockCalls == expectedAttempts &&
            invocation.DosListUnlockCalls == expectedAttempts - refused,
            $"DOSList parser/list ownership differs: ReadArgs={invocation.DosListReadArgsCalls}, FreeArgs={invocation.DosListFreeArgsCalls}, AllocMem={invocation.DosListAllocMemCalls}, FreeMem={invocation.DosListFreeMemCalls}, AllocVec={invocation.DosListAllocVecCalls}, FreeVec={invocation.DosListFreeVecCalls}, lock={invocation.DosListAttemptLockCalls}/{expectedAttempts}, unlock={invocation.DosListUnlockCalls}/{expectedAttempts - refused}.");
        if (definition.CtrlC)
        {
            Require(invocation.DosListPrintFaultCalls == 1,
                "DOSList Ctrl-C fault publication differs.");
            return;
        }
        var selectedEntries = (definition.Devices || selected == 3
                ? definition.DeviceEntries : 0) +
            (definition.Volumes || selected == 3
                ? definition.VolumeEntries : 0) +
            (definition.Assigns || selected == 3
                ? definition.AssignEntries : 0);
        var nameAttributes = invocation.Events.Count(e =>
            e.StartsWith("DosListNameAttr:", StringComparison.Ordinal));
        var portAttributes = invocation.Events.Count(e =>
            e.StartsWith("DosListPortAttr:", StringComparison.Ordinal));
        Require(nameAttributes <= selectedEntries &&
            portAttributes <= nameAttributes,
            "DOSList requested attributes for an unenumerated node.");
    }

    private static string NodeName(string prefix, uint node, uint first)
    {
        var slot = (node - first) / 0x100u;
        return prefix == "DH" ? prefix + slot :
            slot == 0 ? prefix : prefix + slot;
    }

    private static string? NodeNameForAddress(uint address) => address switch
    {
        >= 0x31000 and < 0x31300 => NodeName("DH", address, 0x31000),
        >= 0x32000 and < 0x32300 => NodeName("Work", address, 0x32000),
        >= 0x33000 and < 0x33300 => NodeName("SYS", address, 0x33000),
        _ => null
    };

    private static bool TryDosListVerboseAttribute(DosObjectTag tag,
        out string? text, out uint value)
    {
        text = null;
        value = 0;
        switch (tag)
        {
            case DosObjectTag.DeviceNodeHandler: text = "scsi.device"; break;
            case DosObjectTag.DeviceNodeStackSize: value = 4096; break;
            case DosObjectTag.DeviceNodePriority: value = 0xffff_fffb; break;
            case DosObjectTag.DeviceNodeSegmentList: value = 0xdead; break;
            case DosObjectTag.DeviceNodeGlobalVector: value = 0xffff_ffff; break;
            case DosObjectTag.DeviceNodeSerialId: text = "V49.1"; break;
            case DosObjectTag.FileSysStartupMessage: value = 0x00f0_1000; break;
            case DosObjectTag.FileSysStartupDevice: text = "scsi.device"; break;
            case DosObjectTag.FileSysStartupUnit: value = 2; break;
            case DosObjectTag.FileSysStartupFlags: value = 0x1234; break;
            case DosObjectTag.DosEnvironment: value = 0x00f0_2000; break;
            case DosObjectTag.DosEnvironmentSizeBlock: value = 512; break;
            case DosObjectTag.DosEnvironmentSectorOrigin: value = 0; break;
            case DosObjectTag.DosEnvironmentSurfaces: value = 2; break;
            case DosObjectTag.DosEnvironmentSectorsPerBlock: value = 1; break;
            case DosObjectTag.DosEnvironmentBlocksPerTrack: value = 32; break;
            case DosObjectTag.DosEnvironmentReservedBlocks: value = 2; break;
            case DosObjectTag.DosEnvironmentPreAlloc: value = 3; break;
            case DosObjectTag.DosEnvironmentInterleave: value = 1; break;
            case DosObjectTag.DosEnvironmentLowCylinder: value = 0; break;
            case DosObjectTag.DosEnvironmentHighCylinder: value = 79; break;
            case DosObjectTag.DosEnvironmentNumberOfBuffers: value = 30; break;
            case DosObjectTag.DosEnvironmentBufferMemoryType: value = 1; break;
            case DosObjectTag.DosEnvironmentMaximumTransfer: value = 0x7fff_ffff; break;
            case DosObjectTag.DosEnvironmentMask: value = 0x7fff_fffe; break;
            case DosObjectTag.DosEnvironmentBootPriority: value = 0xffff_fffb; break;
            case DosObjectTag.DosEnvironmentDosType: value = 0x444f_5301; break;
            case DosObjectTag.DosEnvironmentBaud: value = 9600; break;
            case DosObjectTag.DosEnvironmentBootBlocks: value = 2; break;
            case DosObjectTag.DosEnvironmentControl: text = "head=1"; break;
            case DosObjectTag.DosEnvironmentTableSize: value = 20; break;
            case DosObjectTag.DeviceNodeStartup: text = "FileSystem startup"; break;
            case DosObjectTag.DeviceNodeStartupValue: value = 65537; break;
            default: return false;
        }
        return true;
    }

    private static bool IsDosListNodeFormat(string format) => format is
        "<%s> AssignNode 0x%lx\n" or
        "<%s> DeviceNode 0x%lx\n" or
        "AssignNode 0x%lx\n" or
        "DeviceNode 0x%lx\n";

    private static bool IsDosListVerboseFormat(string format) => format is
        "  Handler <%s>\n" or "  StackSize %ld\n" or
        "  Priority %ld\n" or "  SegList 0x%lx\n" or
        "  GlobalVec %ld\n" or "  SerialID <%s>\n" or
        "  FileSysStartupMsg 0x%lx\n" or
        "    <%s:%ld> Flags 0x%lx\n" or
        "  Environment 0x%lx\n" or "    SizeBlock %ld\n" or
        "    SecOrg %ld\n" or "    Surfaces %ld\n" or
        "    SectorsPerBlock %ld\n" or "    BlocksPerTrack %ld\n" or
        "    Reserved %ld\n" or "    PreAlloc %ld\n" or
        "    Interleave %ld\n" or "    LowCyl %ld\n" or
        "    HighCyl %ld\n" or "    NumBuffers %ld\n" or
        "    BufMemType %ld\n" or "    MaxTransfer 0x%lx\n" or
        "    Mask 0x%lx\n" or "    BootPri %ld\n" or
        "    DosType 0x%lx\n" or "    Baud %ld\n" or
        "    BootBlocks %ld\n" or "    Control <%s>\n" or
        "    TableSize %ld\n" or "  Startup <%s>\n" or
        "  Startup %ld\n" or
        "<%s> AssignNode 0x%lx defered to <%s>\n" or
        "  Lock 0x%lx <%s>\n" or
        "    Lock 0x%lx <Not Resolvable>\n" or
        "  + Lock 0x%lx <%s>\n" or
        "  + Lock 0x%lx <Not Resolvable>\n" or
        "    Link 0x%lx Key 0x%lx Access %ld\n" or
        "  Type %ld\n" or "  %s %s %s\n" or
        "  <illegal date>\n" or
        "  Days %ld Minutes %ld Ticks %ld\n" or
        "  DiskType 0x%lx\n" or
        "    Lock 0x%lx <%s>\n" or
        "    Lock 0x%lx <Not Resolvable>\n";

    private string FormatDosListVPrintf(string format, uint arguments)
    {
        var output = new StringBuilder();
        var argumentIndex = 0;
        for (var index = 0; index < format.Length; index++)
        {
            if (format[index] != '%')
            {
                output.Append(format[index]);
                continue;
            }
            Require(index + 1 < format.Length,
                "DOSList VPrintf format ends after percent.");
            var code = format[++index];
            var value = Bus.Long(arguments + (uint)argumentIndex++ * 4u);
            if (code == 's')
            {
                output.Append(Bus.CString(value));
                continue;
            }
            if (code == 'l' && index + 1 < format.Length)
            {
                code = format[++index];
                output.Append(code switch
                {
                    'd' => unchecked((int)value).ToString(
                        CultureInfo.InvariantCulture),
                    'x' => value.ToString("x", CultureInfo.InvariantCulture),
                    _ => throw new InvalidOperationException(
                        $"Unsupported DOSList format conversion: %l{code}")
                });
                continue;
            }
            throw new InvalidOperationException(
                $"Unsupported DOSList format conversion: %{code}");
        }
        return output.ToString();
    }

}
