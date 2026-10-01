using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record ExtractKickstartEntryCase(
    string Device = "DF0:",
    string Destination = "RAM:Kickstart.rom",
    bool Extract13 = false,
    bool InvalidSuperKickstart = false,
    bool ReadFailure = false,
    bool OpenDeviceFailure = false,
    bool OutputOpenFailure = false,
    bool ShortWrite = false,
    bool BufferAllocationFailure = false,
    bool PortAllocationFailure = false,
    bool RequestAllocationFailure = false,
    int ParserError = 0,
    bool UtilityAvailable = true);

internal sealed record ExtractKickstartNativeLayout(
    uint Control,
    uint Device,
    uint Destination,
    uint Buffer = 0);

internal sealed partial class Invocation
{
    public ExtractKickstartNativeLayout? ExtractKickstartLayout { get; set; }
    public uint ExtractKickstartMsgPort { get; set; }
    public uint ExtractKickstartRequest { get; set; }
    public uint ExtractKickstartBuffer { get; set; }
    public int ExtractKickstartMsgPortCreates { get; set; }
    public int ExtractKickstartMsgPortDeletes { get; set; }
    public int ExtractKickstartRequestCreates { get; set; }
    public int ExtractKickstartRequestDeletes { get; set; }
    public int ExtractKickstartOpenDeviceCalls { get; set; }
    public int ExtractKickstartCloseDeviceCalls { get; set; }
    public int ExtractKickstartDoIoCalls { get; set; }
    public int ExtractKickstartReadCalls { get; set; }
    public int ExtractKickstartMotorCalls { get; set; }
    public int ExtractKickstartOutputOpens { get; set; }
    public int ExtractKickstartOutputCloses { get; set; }
    public int ExtractKickstartWrites { get; set; }
    public int ExtractKickstartProtections { get; set; }
    public int ExtractKickstartInhibitCalls { get; set; }
    public int ExtractKickstartPrintFaults { get; set; }
    public int ExtractKickstartPutStrCalls { get; set; }
    public int ExtractKickstartAllocVecCalls { get; set; }
    public int ExtractKickstartFreeVecCalls { get; set; }
    public int ExtractKickstartUtilityOpens { get; set; }
    public int ExtractKickstartUtilityCloses { get; set; }
    public List<uint> ExtractKickstartReadOffsets { get; } = [];
    public List<uint> ExtractKickstartReadLengths { get; } = [];
    public List<uint> ExtractKickstartWriteLengths { get; } = [];
}

internal sealed partial class ProbeFixture
{
    public const string ExtractKickstartEntrySuite =
        "workbench31-extractkickstart-native-entry-vector-fixture";

    private const uint ExtractKickstartUtilityBase = 0xa000;
    private const uint ExtractKickstartOutputBptr = 0x7a00;

    private List<object> RunExtractKickstartEntryCases()
    {
        ProbeCase[] cases =
        [
            new("valid-13", "DF0: RAM:Kickstart13.rom 1.3\n", 0, 0, "")
            {
                ExtractKickstart = new(Extract13: true)
            },
            new("valid-legacy", "df1: RAM:Kickstart.rom\n", 0, 0, "")
            {
                ExtractKickstart = new(Device: "df1:")
            },
            new("invalid-superkickstart", "DF0: RAM:bad.rom\n", 5, 212,
                "DF0: does not contain a SuperKickstart disk - ")
            {
                ExtractKickstart = new(InvalidSuperKickstart: true)
            },
            new("read-failure", "DF0: RAM:read.rom\n", 10, 219,
                "Couldn't read from DF0: - ")
            {
                ExtractKickstart = new(ReadFailure: true)
            },
            new("open-device-failure", "DF0: RAM:device.rom\n", 10, 218,
                "Couldn't read from DF0: - ")
            {
                ExtractKickstart = new(OpenDeviceFailure: true)
            },
            new("output-open-failure", "DF0: RAM:missing.rom\n", 15, 205,
                "Couldn't write RAM:Kickstart.rom - ")
            {
                ExtractKickstart = new(OutputOpenFailure: true)
            },
            new("short-write", "DF0: RAM:short.rom\n", 15, 205,
                "Couldn't write RAM:Kickstart.rom - ")
            {
                ExtractKickstart = new(ShortWrite: true)
            },
            new("bad-device", "DH0: RAM:bad-device.rom\n", 20, 212, "")
            {
                ExtractKickstart = new(Device: "DH0:")
            },
            new("parser-failure", "DF0: RAM:parser.rom\n", 20, 115, "")
            {
                ExtractKickstart = new(ParserError: 115)
            },
            new("buffer-allocation-failure", "DF0: RAM:no-buffer.rom\n", 20, 0, "")
            {
                ExtractKickstart = new(BufferAllocationFailure: true)
            },
            new("port-allocation-failure", "DF0: RAM:no-port.rom\n", 20, 0, "")
            {
                ExtractKickstart = new(PortAllocationFailure: true)
            },
            new("request-allocation-failure", "DF0: RAM:no-request.rom\n", 20, 0, "")
            {
                ExtractKickstart = new(RequestAllocationFailure: true)
            },
            new("missing-dos", "DF0: RAM:no-dos.rom\n", 20,
                Invocation.InitialIoError, "")
            {
                MissingDos = true,
                ExtractKickstart = new()
            },
            new("missing-utility", "DF0: RAM:no-utility.rom\n", 20, 0, "")
            {
                ExtractKickstart = new(UtilityAvailable: false)
            },
            new("workbench-startup", "DF0: RAM:wb.rom\n", 10, 212, "")
            {
                Workbench = true,
                ExtractKickstart = new()
            }
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            new ProbeCase("interleaved-13", "DF0: RAM:i13.rom 1.3\n", 0, 0, "")
            {
                ExtractKickstart = new(Extract13: true), StackBytes = 4096
            },
            new ProbeCase("interleaved-invalid", "DF1: RAM:ibad.rom\n", 5, 212,
                "DF1: does not contain a SuperKickstart disk - ")
            {
                ExtractKickstart = new(Device: "DF1:", InvalidSuperKickstart: true)
            }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void PrepareExtractKickstartEntry(Invocation invocation)
    {
        var definition = invocation.Definition.ExtractKickstart!;
        var control = invocation.Arguments + 0x300;
        var device = invocation.Arguments + 0x400;
        var destination = invocation.Arguments + 0x440;
        WriteCString(device, definition.Device);
        WriteCString(destination, definition.Destination);
        Bus.Long(control, device);
        Bus.Long(control + 4, destination);
        Bus.Long(control + 8, definition.Extract13 ? uint.MaxValue : 0);
        invocation.ExtractKickstartLayout = new(control, device, destination);
    }

    private void VerifyExtractKickstartEntry(Invocation invocation)
    {
        var definition = invocation.Definition.ExtractKickstart!;
        var layout = invocation.ExtractKickstartLayout!;
        var startupFailure = invocation.Definition.Workbench ||
            invocation.Definition.MissingDos || !definition.UtilityAvailable;
        var utilityAttempted = invocation.Definition.Workbench || invocation.Definition.MissingDos ? 0 : 1;
        var utilityClosed = invocation.Definition.Workbench || invocation.Definition.MissingDos ||
            !definition.UtilityAvailable ? 0 : 1;
        var parserFailure = definition.ParserError != 0;
        var parsed = !startupFailure && !parserFailure;
        var deviceValid = definition.Device.Length >= 4 &&
            (definition.Device[0] is 'd' or 'D') &&
            (definition.Device[1] is 'f' or 'F') &&
            char.IsDigit(definition.Device[2]) && definition.Device[3] == ':';
        var buffer = parsed && deviceValid && !definition.BufferAllocationFailure;
        var port = buffer && !definition.PortAllocationFailure;
        var request = port && !definition.RequestAllocationFailure;
        var deviceOpened = request && !definition.OpenDeviceFailure;
        var readBoot = deviceOpened;
        var readMetadata = readBoot && !definition.ReadFailure && !definition.InvalidSuperKickstart;
        var output = readMetadata && !definition.OutputOpenFailure;
        var writes = output ? (definition.Extract13 ? 1 : 3) : 0;
        if (definition.ShortWrite && output) writes = 1;

        Require(invocation.Opens == (invocation.Definition.MissingDos ? 1 : 1) &&
            invocation.Closes == (invocation.Definition.MissingDos ? 0 : 1),
            "ExtractKickstart DOS library lifetime differs.");
        Require(invocation.ExtractKickstartUtilityOpens == utilityAttempted &&
            invocation.ExtractKickstartUtilityCloses == utilityClosed,
            $"ExtractKickstart utility.library lifetime differs (open={invocation.ExtractKickstartUtilityOpens}/{utilityAttempted}, close={invocation.ExtractKickstartUtilityCloses}/{utilityClosed}, startupFailure={startupFailure}, wb={invocation.Definition.Workbench}).");
        Require(invocation.Reads == (startupFailure ? 0 : 1) &&
            invocation.FreeArgs == (parsed ? 1 : 0),
            "ExtractKickstart ReadArgs ownership differs.");
        Require(invocation.ExtractKickstartAllocVecCalls == (parsed && deviceValid ? 1 : 0) &&
            invocation.ExtractKickstartMsgPortCreates == (buffer ? 1 : 0) &&
            invocation.ExtractKickstartMsgPortDeletes == (port ? 1 : 0) &&
            invocation.ExtractKickstartRequestCreates == (port ? 1 : 0) &&
            invocation.ExtractKickstartRequestDeletes == (request ? 1 : 0),
            $"ExtractKickstart Exec allocation lifetime differs (parsed={parsed}, buffer={buffer}, port={port}, request={request}; vec={invocation.ExtractKickstartAllocVecCalls}, port={invocation.ExtractKickstartMsgPortCreates}/{invocation.ExtractKickstartMsgPortDeletes}, req={invocation.ExtractKickstartRequestCreates}/{invocation.ExtractKickstartRequestDeletes}).");
        Require(invocation.ExtractKickstartOpenDeviceCalls == (request ? 1 : 0) &&
            invocation.ExtractKickstartCloseDeviceCalls == (deviceOpened ? 1 : 0),
            "ExtractKickstart device lifetime differs.");
        Require(invocation.ExtractKickstartOutputOpens == (readMetadata ? 1 : 0) &&
            invocation.ExtractKickstartOutputCloses == (output ? 1 : 0) &&
            invocation.ExtractKickstartWrites == writes &&
            invocation.ExtractKickstartProtections == (output ? 1 : 0),
            "ExtractKickstart output lifetime differs.");
        Require(invocation.ExtractKickstartInhibitCalls == (deviceOpened || request ? 2 : 0),
            "ExtractKickstart inhibit lease lifetime differs.");
        Require(invocation.ExtractKickstartPrintFaults ==
            ((invocation.IoError != 0 && !invocation.Definition.Workbench &&
              !invocation.Definition.MissingDos &&
              definition.UtilityAvailable) ? 1 : 0),
            "ExtractKickstart PrintFault count differs.");
        Require(layout.Control != 0 && layout.Device != 0 && layout.Destination != 0,
            "ExtractKickstart parser control layout was not prepared.");
        invocation.ExtractKickstartLayout = null;
    }

    private void RegisterExtractKickstartEntryExec()
    {
        Register(ExecBase, ExecLvo.CreateMsgPort, "CreateMsgPort", (_, invocation) =>
        {
            var definition = invocation.Definition.ExtractKickstart!;
            invocation.ExtractKickstartMsgPortCreates++;
            if (definition.PortAllocationFailure) return 0;
            var port = Bus.Allocate(invocation, MsgPort.Size,
                "ExtractKickstartMsgPort", true);
            invocation.ExtractKickstartMsgPort = port;
            return port;
        });
        Register(ExecBase, ExecLvo.DeleteMsgPort, "DeleteMsgPort", (state, invocation) =>
        {
            Require(state.A[0] == invocation.ExtractKickstartMsgPort,
                "ExtractKickstart deleted the wrong message port.");
            Bus.Release(invocation, state.A[0], "ExtractKickstartMsgPort");
            invocation.ExtractKickstartMsgPortDeletes++;
            return 0;
        });
        Register(ExecBase, ExecLvo.CreateIORequest, "CreateIORequest", (state, invocation) =>
        {
            var definition = invocation.Definition.ExtractKickstart!;
            Require(state.A[0] == invocation.ExtractKickstartMsgPort &&
                state.D[0] == IOStdReq.Size,
                "ExtractKickstart CreateIORequest ABI differs.");
            invocation.ExtractKickstartRequestCreates++;
            if (definition.RequestAllocationFailure) return 0;
            var request = Bus.Allocate(invocation, IOStdReq.Size,
                "ExtractKickstartIORequest", true);
            invocation.ExtractKickstartRequest = request;
            return request;
        });
        Register(ExecBase, ExecLvo.DeleteIORequest, "DeleteIORequest", (state, invocation) =>
        {
            Require(state.A[0] == invocation.ExtractKickstartRequest,
                "ExtractKickstart deleted the wrong IO request.");
            Bus.Release(invocation, state.A[0], "ExtractKickstartIORequest");
            invocation.ExtractKickstartRequestDeletes++;
            return 0;
        });
        Register(ExecBase, ExecLvo.OpenDevice, "OpenDevice", (state, invocation) =>
        {
            var definition = invocation.Definition.ExtractKickstart!;
            Require(Bus.CString(state.A[0]) == TrackDiskDevice.Name &&
                state.D[0] <= 9 && state.D[1] == 0 &&
                state.A[1] == invocation.ExtractKickstartRequest,
                $"ExtractKickstart trackdisk.device OpenDevice ABI differs (name={Bus.CString(state.A[0])}, unit={state.D[0]}, flags={state.D[1]}, req=${state.A[1]:X8}/${invocation.ExtractKickstartRequest:X8}).");
            invocation.ExtractKickstartOpenDeviceCalls++;
            return definition.OpenDeviceFailure ? 1u : 0u;
        });
        Register(ExecBase, ExecLvo.CloseDevice, "CloseDevice", (state, invocation) =>
        {
            Require(state.A[1] == invocation.ExtractKickstartRequest,
                "ExtractKickstart closed the wrong device request.");
            invocation.ExtractKickstartCloseDeviceCalls++;
            return 0;
        });
        Register(ExecBase, ExecLvo.DoIO, "DoIO", (state, invocation) =>
        {
            var definition = invocation.Definition.ExtractKickstart!;
            Require(state.A[1] == invocation.ExtractKickstartRequest,
                "ExtractKickstart DoIO request differs.");
            var request = state.A[1];
            var command = Bus.Word(request + (uint)ExecLayout.IORequest.Command);
            if (command == (ushort)TrackDiskCommand.Motor)
            {
                Require(Bus.Long(request + (uint)ExecLayout.IOStdReq.Length) == 0 &&
                    Bus.Long(request + (uint)ExecLayout.IOStdReq.Data) == 0,
                    "ExtractKickstart motor request differs.");
                invocation.ExtractKickstartMotorCalls++;
                invocation.ExtractKickstartDoIoCalls++;
                return 0;
            }
            Require(command == (ushort)TrackDiskCommand.Read,
                "ExtractKickstart issued an unexpected trackdisk command.");
            var buffer = Bus.Long(request + (uint)ExecLayout.IOStdReq.Data);
            var length = Bus.Long(request + (uint)ExecLayout.IOStdReq.Length);
            var offset = Bus.Long(request + (uint)ExecLayout.IOStdReq.Offset);
            Bus.OwnedAllocationContaining(invocation, buffer,
                "ExtractKickstartBuffer");
            Require(length > 0 && length <= NativeWorkbench31ExtractKickstartCommand.BufferBytes,
                "ExtractKickstart read length is outside the resident buffer.");
            invocation.ExtractKickstartReadCalls++;
            invocation.ExtractKickstartDoIoCalls++;
            invocation.ExtractKickstartReadOffsets.Add(offset);
            invocation.ExtractKickstartReadLengths.Add(length);
            var failed = definition.ReadFailure && invocation.ExtractKickstartReadCalls == 1;
            Bus.Memory[request + (uint)ExecLayout.IOStdReq.Error] = failed ? (byte)1 : (byte)0;
            if (failed) return 0;
            Bus.Memory.AsSpan((int)buffer, (int)length).Fill((byte)(offset >> 10));
            if (offset == 0 && length >= 8 && !definition.InvalidSuperKickstart)
            {
                Bus.Long(buffer, 0x4b49434b);
                Bus.Long(buffer + 4, 0x53555030);
            }
            if (offset == 0 && length >= 16)
            {
                Bus.Long(buffer + 8, 0x10000);
                Bus.Long(buffer + 12, 0x1000);
            }
            return 0;
        });
    }

    private void RegisterExtractKickstartEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.ExtractKickstart!;
            var layout = invocation.ExtractKickstartLayout!;
            Require(Bus.CString(state.D[1]) == NativeWorkbench31ExtractKickstartCommand.Template &&
                state.D[3] == 0 && Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size ==
                NativeWorkbench31ExtractKickstartCommand.ResultCount * 4,
                "ExtractKickstart ReadArgs ABI differs.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            for (var offset = 0u; offset < NativeWorkbench31ExtractKickstartCommand.ResultCount * 4; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "ExtractKickstart ReadArgs result slots were not cleared.");
            Bus.Long(state.D[2], layout.Device);
            Bus.Long(state.D[2] + 4, layout.Destination);
            Bus.Long(state.D[2] + 8, definition.Extract13 ? uint.MaxValue : 0);
            return Bus.Allocate(invocation, 40, "RDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.Inhibit, "Inhibit", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == invocation.Definition.ExtractKickstart!.Device &&
                (state.D[2] == 0 || state.D[2] == uint.MaxValue),
                "ExtractKickstart Inhibit ABI differs.");
            invocation.ExtractKickstartInhibitCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.OpenRaw, "OpenRaw", (state, invocation) =>
        {
            var definition = invocation.Definition.ExtractKickstart!;
            Require(Bus.CString(state.D[1]) == definition.Destination &&
                state.D[2] == (uint)DOS.FileMode.NewFile,
                "ExtractKickstart output OpenRaw ABI differs.");
            invocation.ExtractKickstartOutputOpens++;
            if (definition.OutputOpenFailure)
            {
                invocation.IoError = 205;
                return 0;
            }
            invocation.IoError = 0;
            return ExtractKickstartOutputBptr;
        });
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            var definition = invocation.Definition.ExtractKickstart!;
            Require(state.D[1] == ExtractKickstartOutputBptr &&
                state.D[2] == invocation.ExtractKickstartBuffer,
                "ExtractKickstart output Write handle or buffer differs.");
            var length = state.D[3];
            Require(length > 0 && length <= NativeWorkbench31ExtractKickstartCommand.BufferBytes,
                "ExtractKickstart output Write length differs.");
            invocation.ExtractKickstartWrites++;
            invocation.ExtractKickstartWriteLengths.Add(length);
            if (definition.ShortWrite)
            {
                invocation.IoError = 205;
                return 0;
            }
            invocation.IoError = 0;
            return length;
        });
        Register(baseAddress, DosLvo.Close, "Close", (state, invocation) =>
        {
            Require(state.D[1] == ExtractKickstartOutputBptr,
                "ExtractKickstart closed the wrong output handle.");
            invocation.ExtractKickstartOutputCloses++;
            return 1;
        });
        Register(baseAddress, DosLvo.SetProtection, "SetProtection", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == invocation.Definition.ExtractKickstart!.Destination &&
                state.D[2] == 2,
                "ExtractKickstart output protection differs.");
            invocation.ExtractKickstartProtections++;
            return 1;
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            var text = Bus.CString(state.D[1]);
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.ExtractKickstartPutStrCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state, invocation) =>
        {
            Require(state.D[2] == 0, "ExtractKickstart PrintFault header must be NULL.");
            invocation.ExtractKickstartPrintFaults++;
            return 1;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }
}
