using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record FormatEntryCase(
    bool BannedName = false,
    bool DeviceMissing = false,
    int ParserError = 0,
    bool AllocationFailure = false,
    bool Media = false,
    bool Quick = false,
    bool NoIcons = false);

internal sealed class FormatNativeLayout(uint device, uint name)
{
    public uint Device { get; } = device;
    public uint Name { get; } = name;
    public uint DeviceScratchBytes { get; set; }
    public uint DeviceScratch { get; set; }
    public uint List { get; set; }
    public uint Node { get; set; }
    public uint FsPort { get; set; }
    public uint Startup { get; set; }
    public uint StartupDevice { get; set; }
    public uint Environment { get; set; }
    public uint DeviceNameBytes { get; set; }
    public uint DeviceName { get; set; }
    public uint MessagePort { get; set; }
    public uint IoRequest { get; set; }
    public uint FormatBuffer { get; set; }
    public uint FormatBufferBytes { get; set; }
    public List<(uint Address, string Kind)> FixtureAllocations { get; } = [];
    public Dictionary<ulong, byte[]> Media { get; } = new();
    public int ReadArgsCalls { get; set; }
    public int FreeArgsCalls { get; set; }
    public int LockCalls { get; set; }
    public int FindCalls { get; set; }
    public int UnlockCalls { get; set; }
    public int PrintFaultCalls { get; set; }
    public int InhibitCalls { get; set; }
    public int FormatPacketCalls { get; set; }
    public int DiskInfoCalls { get; set; }
    public int DelayCalls { get; set; }
    public int MessagePortCreates { get; set; }
    public int MessagePortDeletes { get; set; }
    public int IoRequestCreates { get; set; }
    public int IoRequestDeletes { get; set; }
    public int OpenDeviceCalls { get; set; }
    public int CloseDeviceCalls { get; set; }
    public int FormatWrites { get; set; }
    public int Readbacks { get; set; }
    public int UpdateCalls { get; set; }
    public int ClearCalls { get; set; }
    public int MotorCalls { get; set; }
}

internal sealed partial class ProbeFixture
{
    public const string FormatEntrySuite =
        "morphos-format-native-entry-boundary-fixture";

    private List<object> RunFormatEntryCases()
    {
        ProbeCase[] cases =
        [
            new("missing-device", "DEVICE=DH0: NAME=Work\n", DOS.RETURN_ERROR,
                (int)DOS.Error.DeviceNotMounted, "")
            { Format = new(DeviceMissing: true) },
            new("banned-volume", "DEVICE=DH0: NAME=SYS\n", DOS.RETURN_ERROR,
                (int)DOS.Error.InvalidComponentName, "")
            { Format = new(BannedName: true) },
            new("parser-failure", "DEVICE=DH0: NAME=Work\n", DOS.RETURN_ERROR,
                116, "")
            { Format = new(ParserError: 116) },
            new("result-allocation-failure", "DEVICE=DH0: NAME=Work\n",
                DOS.RETURN_FAIL, (int)DOS.Error.NoFreeStore, "")
            { Format = new(AllocationFailure: true) },
            new("full-format-disposable-media", "DEVICE=DH0: NAME=Work NOICONS\n",
                DOS.RETURN_OK, 0, "DH0 disk: insert disk to be formatted\n")
            { Format = new(Media: true, NoIcons: true) },
            new("quick-format-disposable-media", "DEVICE=DH0: NAME=Work QUICK NOICONS\n",
                DOS.RETURN_OK, 0, "DH0 disk: insert disk to be formatted\n")
            { Format = new(Media: true, Quick: true, NoIcons: true) },
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Workbench = true, Format = new() },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { MissingDos = true, Format = new() },
        ];

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-missing-device" },
            cases[1] with { Name = "interleaved-banned-volume" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void PrepareFormatEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Format ??
            throw new InvalidOperationException("Missing Format definition.");
        var layout = new FormatNativeLayout(
            invocation.Arguments + 0x400,
            invocation.Arguments + 0x500);
        invocation.FormatLayout = layout;
        WriteArgument(layout.Device, "DH0:");
        WriteArgument(layout.Name, definition.BannedName ? "SYS" : "Work");
        layout.DeviceScratchBytes = (uint)Bus.CString(layout.Device).Length + 1;
        if (definition.Media)
        {
            layout.List = Bus.Allocate(invocation, 4, "FormatDosList", true);
            layout.Node = Bus.Allocate(invocation, 64, "FormatDeviceNode", true);
            layout.FsPort = Bus.Allocate(invocation, 4, "FormatHandlerPort", true);
            layout.Startup = Bus.Allocate(invocation, 16, "FormatStartup", true);
            layout.StartupDevice = Bus.Allocate(invocation, 32, "FormatStartupDevice", true);
            layout.Environment = Bus.Allocate(invocation, 80, "FormatEnvironment", true);
            layout.FixtureAllocations.Add((layout.List, "FormatDosList"));
            layout.FixtureAllocations.Add((layout.Node, "FormatDeviceNode"));
            layout.FixtureAllocations.Add((layout.FsPort, "FormatHandlerPort"));
            layout.FixtureAllocations.Add((layout.Startup, "FormatStartup"));
            layout.FixtureAllocations.Add((layout.StartupDevice, "FormatStartupDevice"));
            layout.FixtureAllocations.Add((layout.Environment, "FormatEnvironment"));
            var deviceName = Encoding.Latin1.GetBytes(TrackDiskDevice.Name);
            layout.DeviceNameBytes = (uint)deviceName.Length + 1;
            Bus.Long(layout.Node + 8, layout.FsPort);
            Bus.Long(layout.Node + 28, layout.Startup >> 2);
            Bus.Long(layout.Startup, 0);
            Bus.Long(layout.Startup + 4, layout.StartupDevice >> 2);
            Bus.Long(layout.Startup + 8, layout.Environment >> 2);
            Bus.Long(layout.Startup + 12, 0);
            Bus.Memory[layout.StartupDevice] = (byte)deviceName.Length;
            deviceName.CopyTo(
                Bus.Memory.AsSpan((int)layout.StartupDevice + 1));
            Bus.Long(layout.Environment + 0, 17);
            Bus.Long(layout.Environment + 4, 128);
            Bus.Long(layout.Environment + 12, 1);
            Bus.Long(layout.Environment + 20, 1);
            Bus.Long(layout.Environment + 36, 0);
            Bus.Long(layout.Environment + 40, 1);
            Bus.Long(layout.Environment + 48, (uint)Exec.MemoryFlags.Public);
            Bus.Long(layout.Environment + 52, 512);
            Bus.Long(layout.Environment + 56, 0xffff_fffc);
            Bus.Long(layout.Environment + 64, 0);
        }
    }

    private void VerifyFormatEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Format!;
        var layout = invocation.FormatLayout!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.Reads == 0 && invocation.Allocations == 0 &&
                invocation.FreeArgs == 0 && invocation.FreeMem == 0 &&
                layout.ReadArgsCalls == 0 && layout.PrintFaultCalls == 0,
                "Format startup boundary reached the command body.");
            invocation.FormatLayout = null;
            return;
        }

        if (definition.Media)
        {
            var expectedAllocations = definition.Quick ? 5 : 6;
            Require(invocation.Reads == 1 && invocation.FreeArgs == 1 &&
                invocation.Allocations == expectedAllocations &&
                invocation.FreeMem == expectedAllocations &&
                layout.LockCalls == 1 && layout.FindCalls == 1 &&
                layout.UnlockCalls == 1 && layout.InhibitCalls == 2 &&
                layout.MessagePortCreates == 1 && layout.MessagePortDeletes == 1 &&
                layout.IoRequestCreates == 1 && layout.IoRequestDeletes == 1 &&
                layout.OpenDeviceCalls == 1 && layout.CloseDeviceCalls == 1 &&
                layout.FormatPacketCalls == 1 && layout.DiskInfoCalls >= 1 &&
                layout.MotorCalls == 1,
                $"Format disposable-media ownership differs (alloc={invocation.Allocations}, free={invocation.FreeMem}, lock={layout.LockCalls}/{layout.FindCalls}/{layout.UnlockCalls}, inhibit={layout.InhibitCalls}, packets={layout.FormatPacketCalls}/{layout.DiskInfoCalls}, writes={layout.FormatWrites}, reads={layout.Readbacks}, motor={layout.MotorCalls}).");
            if (definition.Quick)
                Require(layout.FormatWrites == 0 && layout.Readbacks == 0,
                    "Format quick path issued trackdisk media I/O.");
            else
                Require(layout.FormatWrites == 1 && layout.Readbacks == 1,
                    "Format full path did not format and read back disposable media.");
            Require(layout.UpdateCalls == (definition.Quick ? 0 : 1) &&
                layout.ClearCalls == (definition.Quick ? 0 : 1),
                "Format media flush sequence differs.");
            foreach (var allocation in layout.FixtureAllocations)
                Bus.Release(invocation, allocation.Address, allocation.Kind);
            invocation.FormatLayout = null;
            return;
        }

        Require(invocation.Reads == (definition.AllocationFailure ? 0 : 1) &&
            invocation.FreeArgs == (definition.ParserError == 0 &&
                !definition.AllocationFailure ? 1 : 0),
            "Format parser ownership differs.");
        if (definition.AllocationFailure)
        {
            Require(invocation.Allocations == 1 && invocation.FreeMem == 0 &&
                layout.PrintFaultCalls == 1 && layout.LockCalls == 0,
                "Format result-allocation failure cleanup differs.");
        }
        else if (definition.ParserError != 0)
        {
            Require(invocation.Allocations == 1 && invocation.FreeMem == 1 &&
                layout.PrintFaultCalls == 1 && layout.LockCalls == 0,
                "Format early-failure cleanup differs.");
        }
        else
        {
            var expectedAllocations = definition.BannedName ? 1 : 2;
            Require(invocation.Allocations == expectedAllocations &&
                invocation.FreeMem == expectedAllocations &&
                invocation.FreeArgs == 1 && layout.PrintFaultCalls == 1,
                $"Format result/device cleanup differs (name={invocation.Definition.Name}, banned={definition.BannedName}, missing={definition.DeviceMissing}, parser={definition.ParserError}, alloc={invocation.Allocations}, free={invocation.FreeMem}, freeArgs={invocation.FreeArgs}, faults={layout.PrintFaultCalls}, reads={invocation.Reads}, events={string.Join(',', invocation.Events)}).");
            if (!definition.BannedName)
                Require(layout.DeviceScratch != 0 &&
                    layout.DeviceScratchBytes == (uint)Bus.CString(layout.Device).Length + 1,
                    $"Format device scratch ownership differs (name={invocation.Definition.Name}, scratch=${layout.DeviceScratch:X8}, bytes={layout.DeviceScratchBytes}, device={Bus.CString(layout.Device)}).");
        }

        if (definition.DeviceMissing)
        {
            Require(layout.LockCalls == 1 && layout.FindCalls == 1 &&
                layout.UnlockCalls == 1,
                "Format missing-device DOS-list lifetime differs.");
        }
        else
        {
            Require(layout.LockCalls == 0 && layout.FindCalls == 0 &&
                layout.UnlockCalls == 0,
                "Format banned-name path touched the device list.");
        }
        invocation.FormatLayout = null;
    }

    private void RegisterFormatEntryExec()
    {
        Register(ExecBase, ExecLvo.CreateMsgPort, "CreateMsgPort",
            (_, invocation) =>
            {
                var layout = invocation.FormatLayout!;
                Require(invocation.Definition.Format!.Media,
                    "Format created a message port outside media fixture.");
                layout.MessagePortCreates++;
                var port = Bus.Allocate(invocation, MsgPort.Size,
                    "FormatMsgPort", true);
                layout.MessagePort = port;
                return port;
            });
        Register(ExecBase, ExecLvo.DeleteMsgPort, "DeleteMsgPort",
            (state, invocation) =>
            {
                var layout = invocation.FormatLayout!;
                Require(state.A[0] == layout.MessagePort &&
                    layout.MessagePortDeletes == 0,
                    "Format deleted an unowned message port.");
                Bus.Release(invocation, state.A[0], "FormatMsgPort");
                layout.MessagePortDeletes++;
                return 0;
            });
        Register(ExecBase, ExecLvo.CreateIORequest, "CreateIORequest",
            (state, invocation) =>
            {
                var layout = invocation.FormatLayout!;
                Require(state.A[0] == layout.MessagePort &&
                    state.D[0] == IOStdReq.Size,
                    "Format CreateIORequest ABI differs.");
                layout.IoRequestCreates++;
                var request = Bus.Allocate(invocation, IOStdReq.Size,
                    "FormatIORequest", true);
                layout.IoRequest = request;
                return request;
            });
        Register(ExecBase, ExecLvo.DeleteIORequest, "DeleteIORequest",
            (state, invocation) =>
            {
                var layout = invocation.FormatLayout!;
                Require(state.A[0] == layout.IoRequest &&
                    layout.IoRequestDeletes == 0,
                    "Format deleted an unowned IO request.");
                Bus.Release(invocation, state.A[0], "FormatIORequest");
                layout.IoRequestDeletes++;
                return 0;
            });
        Register(ExecBase, ExecLvo.OpenDevice, "OpenDevice",
            (state, invocation) =>
            {
                var layout = invocation.FormatLayout!;
                Require(Bus.CString(state.A[0]) == TrackDiskDevice.Name &&
                    state.D[0] == 0 && state.A[1] == layout.IoRequest,
                    "Format trackdisk OpenDevice ABI differs.");
                layout.OpenDeviceCalls++;
                return 0;
            });
        Register(ExecBase, ExecLvo.CloseDevice, "CloseDevice",
            (state, invocation) =>
            {
                var layout = invocation.FormatLayout!;
                Require(state.A[1] == layout.IoRequest &&
                    layout.CloseDeviceCalls == 0,
                    "Format closed an unowned trackdisk request.");
                layout.CloseDeviceCalls++;
                return 0;
            });
        Register(ExecBase, ExecLvo.DoIO, "DoIO",
            (state, invocation) =>
            {
                var layout = invocation.FormatLayout!;
                Require(state.A[1] == layout.IoRequest,
                    "Format DoIO request differs.");
                var request = state.A[1];
                var command = (TrackDiskCommand)Bus.Word(request +
                    (uint)ExecLayout.IORequest.Command);
                var data = Bus.Long(request + (uint)ExecLayout.IOStdReq.Data);
                var length = Bus.Long(request + (uint)ExecLayout.IOStdReq.Length);
                var low = Bus.Long(request + (uint)ExecLayout.IOStdReq.Offset);
                var high = Bus.Long(request + (uint)ExecLayout.IOStdReq.Actual);
                var offset = ((ulong)high << 32) | low;
                Bus.Memory[request + (uint)ExecLayout.IORequest.Error] = 0;
                switch (command)
                {
                    case TrackDiskCommand.Format or TrackDiskCommand.Format64:
                        Bus.OwnedAllocationContaining(invocation, data, "Exec");
                        Require(length > 0, "Format write length is empty.");
                        layout.Media[offset] = Bus.Memory.AsSpan((int)data,
                            checked((int)length)).ToArray();
                        layout.FormatWrites++;
                        break;
                    case TrackDiskCommand.Read or TrackDiskCommand.Read64:
                        Bus.OwnedAllocationContaining(invocation, data, "Exec");
                        Require(layout.Media.TryGetValue(offset, out var bytes) &&
                            bytes.Length >= length,
                            "Format readback did not target formatted media.");
                        bytes.AsSpan(0, checked((int)length)).CopyTo(
                            Bus.Memory.AsSpan((int)data, checked((int)length)));
                        layout.Readbacks++;
                        break;
                    case TrackDiskCommand.Update:
                        layout.UpdateCalls++;
                        break;
                    case TrackDiskCommand.Clear:
                        layout.ClearCalls++;
                        break;
                    case TrackDiskCommand.Motor:
                        Require(length == 0 && data == 0,
                            "Format motor request differs.");
                        layout.MotorCalls++;
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unexpected Format trackdisk command {command}.");
                }
                return 0;
            });
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal",
            (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "Format Ctrl-C query ABI differs.");
                return 0;
            });
    }

    private void RegisterFormatDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.Delay, "Delay",
            (state, invocation) =>
            {
                Require(state.D[1] == 15, "Format validation delay differs.");
                invocation.FormatLayout!.DelayCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Format!;
            var layout = invocation.FormatLayout!;
            Require(Bus.CString(state.D[1]) == NativeMorphOSFormatCommand.Template &&
                state.D[3] == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size ==
                    NativeMorphOSFormatCommand.ResultCount * 4u,
                "Format template/result ABI differs.");
            for (var offset = 0u; offset < NativeMorphOSFormatCommand.ResultCount * 4u; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "Format result slots were not cleared.");
            layout.ReadArgsCalls++;
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }

            Bus.Long(state.D[2], layout.Device);
            Bus.Long(state.D[2] + 4, layout.Name);
            if (definition.NoIcons)
                Bus.Long(state.D[2] + 12 * 4, uint.MaxValue);
            if (definition.Quick)
                Bus.Long(state.D[2] + 13 * 4, uint.MaxValue);
            var rdArgs = Bus.Allocate(invocation, 40, "FormatRDArgs", true);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "FormatRDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.LockDosList, "LockDosList",
            (state, invocation) =>
            {
                var layout = invocation.FormatLayout!;
                Require(state.D[1] == (uint)(DosListLockFlags.Devices |
                    DosListLockFlags.Read), "Format DOS-list lock flags differ.");
                layout.LockCalls++;
                return invocation.Definition.Format!.Media ? layout.List : 0x58000;
            });
        Register(baseAddress, DosLvo.FindDosEntry, "FindDosEntry",
            (state, invocation) =>
            {
                var layout = invocation.FormatLayout!;
                var definition = invocation.Definition.Format!;
                Require(state.D[1] == (definition.Media ? layout.List : 0x58000) &&
                    state.D[3] == (uint)DosListLockFlags.Devices,
                    "Format device lookup ABI differs.");
                layout.FindCalls++;
                return definition.Media ? layout.Node : 0;
            });
        Register(baseAddress, DosLvo.UnLockDosList, "UnLockDosList",
            (state, invocation) =>
            {
                var layout = invocation.FormatLayout!;
                Require(state.D[1] == (uint)(DosListLockFlags.Devices |
                    DosListLockFlags.Read), "Format DOS-list unlock flags differ.");
                layout.UnlockCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.Input, "Input",
            (_, invocation) => invocation.OutputBptr);
        Register(baseAddress, DosLvo.Output, "Output",
            (_, invocation) => invocation.OutputBptr);
        Register(baseAddress, DosLvo.IsInteractive, "IsInteractive",
            (_, invocation) => invocation.Definition.Format!.Media ? 1u : 0u);
        Register(baseAddress, DosLvo.WaitForChar, "WaitForChar",
            (state, invocation) =>
            {
                Require(state.D[1] == invocation.OutputBptr && state.D[2] == 100,
                    "Format WaitForChar ABI differs.");
                return invocation.Definition.Format!.Media ? 1u : 0u;
            });
        Register(baseAddress, DosLvo.FGetC, "FGetC",
            (state, invocation) =>
            {
                Require(state.D[1] == invocation.OutputBptr,
                    "Format FGetC ABI differs.");
                return '\n';
            });
        Register(baseAddress, DosLvo.PutStr, "PutStr",
            (state, invocation) =>
            {
                if (invocation.Definition.Format!.Media)
                    invocation.Output.Write(Encoding.Latin1.GetBytes(
                        Bus.CString(state.D[1])));
                return 0;
            });
        Register(baseAddress, DosLvo.Flush, "Flush",
            (_, _) => 1);
        Register(baseAddress, DosLvo.DoPkt, "DoPkt",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Format!;
                var layout = invocation.FormatLayout!;
                Require(definition.Media && state.D[1] == layout.FsPort,
                    "Format handler packet port differs.");
                var action = unchecked((int)state.D[2]);
                switch ((DosPacketAction)action)
                {
                    case DosPacketAction.Inhibit:
                        Require(state.D[3] == uint.MaxValue || state.D[3] == 0,
                            "Format inhibit packet state differs.");
                        layout.InhibitCalls++;
                        return 1;
                    case DosPacketAction.Format:
                        Require(state.D[3] != 0 && state.D[4] != 0,
                            "Format handler format packet differs.");
                        layout.FormatPacketCalls++;
                        return 1;
                    case DosPacketAction.DiskInfo:
                        Require(state.D[3] != 0, "Format DiskInfo packet lost InfoData.");
                        var info = BPTR.FromRaw(state.D[3]).Address;
                        Bus.Long(info + (uint)DosLayout.InfoData.DiskState,
                            (uint)DosDiskState.Validated);
                        Bus.Long(info + (uint)DosLayout.InfoData.DiskType,
                            0x444f5301u);
                        layout.DiskInfoCalls++;
                        return 1;
                    default:
                        throw new InvalidOperationException(
                            $"Unexpected Format handler packet action {action}.");
                }
            });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (_, invocation) =>
            {
                invocation.FormatLayout!.PrintFaultCalls++;
                return 1;
            });
        Register(baseAddress, DosLvo.IoErr, "IoErr",
            (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }
}
