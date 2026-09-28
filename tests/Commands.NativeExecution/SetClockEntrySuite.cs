using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record SetClockEntryCase(
    string Name,
    string Mode,
    bool ParserFailure = false,
    bool MissingResource = false,
    bool PortAllocationFailure = false,
    bool RequestAllocationFailure = false,
    bool DeviceOpenFailure = false,
    bool TimerIoFailure = false,
    bool UtcProviders = false,
    int Error = 0,
    int Result = DOS.RETURN_OK,
    string Output = "");

internal sealed partial class ProbeFixture
{
    public const string SetClockEntrySuite =
        "setclock-morphos-native-entry-vector-fixture";
    public const string Workbench31SetClockEntrySuite =
        "setclock-wb31-native-entry-vector-fixture";

    private const uint SetClockBattClockBase = 0xc000;
    private const uint SetClockTimerBase = 0xd000;
    private const uint SetClockBattClockSeconds = 0x12345678;
    private const uint SetClockSystemSeconds = 0x87654321;

    private List<object> RunSetClockEntryCases()
    {
        var cases = new List<ProbeCase>
        {
            SetClockCase("load", "LOAD"),
            SetClockCase("save", "SAVE"),
            SetClockCase("reset", "RESET"),
            SetClockCase("missing-action", "", error: 116,
                result: DOS.RETURN_FAIL),
            SetClockCase("parser-failure", "LOAD", parserFailure: true,
                error: 115, result: DOS.RETURN_FAIL),
            SetClockCase("resource-missing", "LOAD", missingResource: true,
                error: 122, result: DOS.RETURN_FAIL),
            SetClockCase("port-allocation-failure", "LOAD",
                portAllocationFailure: true, error: 103,
                result: DOS.RETURN_FAIL),
            SetClockCase("request-allocation-failure", "LOAD",
                requestAllocationFailure: true, error: 103,
                result: DOS.RETURN_FAIL),
            SetClockCase("device-open-failure", "LOAD",
                deviceOpenFailure: true, error: 122,
                result: DOS.RETURN_FAIL),
            SetClockCase("timer-io-failure", "LOAD",
                timerIoFailure: true,
                result: DOS.RETURN_FAIL,
                output: "Error: Could not set system time!\n"),
            SetClockCase("utc-load", "LOAD", utcProviders: true),
            SetClockCase("utc-save", "SAVE", utcProviders: true),
            SetClockCase("utc-reset", "RESET", utcProviders: true),
        };
        if (suite == Workbench31SetClockEntrySuite)
        {
            cases.RemoveRange(10, 3);
            cases.Add(new ProbeCase("workbench-startup", "LOAD",
                DOS.RETURN_ERROR, (int)DOS.Error.ObjectWrongType, "")
            {
                Workbench = true,
                SetClock = new SetClockEntryCase("workbench-startup", "LOAD")
            });
            cases.Add(new ProbeCase("missing-dos", "LOAD", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            {
                MissingDos = true,
                SetClock = new SetClockEntryCase("missing-dos", "LOAD")
            });
        }
        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "interleaved-load" },
            cases[1] with { Name = "interleaved-save" }
        ], true));
        reports.AddRange(Execute([
            cases[3] with { Name = "interleaved-missing" },
            cases[2] with { Name = "interleaved-reset" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase SetClockCase(string name, string mode,
        bool parserFailure = false, bool missingResource = false,
        bool portAllocationFailure = false,
        bool requestAllocationFailure = false,
        bool deviceOpenFailure = false, bool timerIoFailure = false,
        bool utcProviders = false, int error = 0,
        int result = DOS.RETURN_OK, string output = "")
    {
        var arguments = mode.Length == 0 ? "\n" : mode + "\n";
        return new ProbeCase(name, arguments, result, error, output)
        {
            SetClock = new SetClockEntryCase(name, mode, parserFailure,
                missingResource, portAllocationFailure,
                requestAllocationFailure, deviceOpenFailure,
                timerIoFailure, utcProviders, error, result, output)
        };
    }

    private void PrepareSetClockEntry(Invocation invocation)
    {
        invocation.SetClockLayout = new SetClockNativeLayout(
            SetClockBattClockBase, SetClockTimerBase);
        Bus.Word(SetClockBattClockBase + (uint)ExecLayout.Library.Version,
            invocation.Definition.SetClock!.UtcProviders ? (ushort)52 : (ushort)51);
        Bus.Word(SetClockTimerBase + (uint)ExecLayout.Library.Version,
            invocation.Definition.SetClock.UtcProviders ? (ushort)52 : (ushort)51);
    }

    private void VerifySetClockEntry(Invocation invocation)
    {
        var definition = invocation.Definition.SetClock!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.Reads == 0 && invocation.FreeArgs == 0 &&
                invocation.SetClockOpenResourceCalls == 0 &&
                invocation.SetClockMsgPortCreates == 0 &&
                invocation.SetClockIoRequestCreates == 0 &&
                invocation.SetClockOpenDeviceCalls == 0,
                "SetClock startup boundary reached the command body.");
            invocation.SetClockLayout = null;
            return;
        }
        var parserFailure = definition.ParserFailure;
        var resources = !parserFailure && !definition.MissingResource;
        var portCreated = resources;
        var requestCreated = resources && !definition.PortAllocationFailure;
        var deviceAttempted = requestCreated && !definition.RequestAllocationFailure;
        var deviceOpened = deviceAttempted && !definition.DeviceOpenFailure;
        var operation = deviceOpened;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "SetClock parser lifetime differs.");
        Require(invocation.Allocations == 1 +
            (definition.Mode == "SAVE" && operation ? 1 : 0) &&
            invocation.FreeMem == invocation.Allocations,
            "SetClock temporary allocation lifetime differs.");
        Require(invocation.SetClockOpenResourceCalls == (parserFailure ? 0 : 1) &&
            invocation.SetClockMsgPortCreates == (portCreated ? 1 : 0) &&
            invocation.SetClockMsgPortDeletes == (requestCreated ? 1 : 0) &&
            invocation.SetClockIoRequestCreates == (requestCreated ? 1 : 0) &&
            invocation.SetClockIoRequestDeletes == (deviceAttempted ? 1 : 0) &&
            invocation.SetClockOpenDeviceCalls == (deviceAttempted ? 1 : 0) &&
            invocation.SetClockCloseDeviceCalls == (deviceOpened ? 1 : 0),
            $"SetClock Exec resource lifetime differs (mode={definition.Mode}, parser={definition.ParserFailure}, missing={definition.MissingResource}, resource={invocation.SetClockOpenResourceCalls}, port={invocation.SetClockMsgPortCreates}/{invocation.SetClockMsgPortDeletes}, request={invocation.SetClockIoRequestCreates}/{invocation.SetClockIoRequestDeletes}, device={invocation.SetClockOpenDeviceCalls}/{invocation.SetClockCloseDeviceCalls}; expected {resources}/{portCreated}/{requestCreated}/{deviceAttempted}/{deviceOpened}).");
        Require(invocation.SetClockReadCalls ==
                (operation && !definition.UtcProviders &&
                    definition.Mode == "LOAD" ? 1 : 0) &&
            invocation.SetClockWriteCalls ==
                (operation && !definition.UtcProviders &&
                    definition.Mode == "SAVE" ? 1 : 0) &&
            invocation.SetClockResetCalls ==
                (operation && definition.Mode == "RESET" ? 1 : 0) &&
            invocation.SetClockGetSysTimeCalls ==
                (operation && !definition.UtcProviders &&
                    definition.Mode == "SAVE" ? 1 : 0) &&
            invocation.SetClockReadUtcCalls ==
                (operation && definition.UtcProviders &&
                    definition.Mode == "LOAD" ? 1 : 0) &&
            invocation.SetClockWriteUtcCalls ==
                (operation && definition.UtcProviders &&
                    definition.Mode == "SAVE" ? 1 : 0) &&
            invocation.SetClockGetUtcSysTimeCalls ==
                (operation && definition.UtcProviders &&
                    definition.Mode == "SAVE" ? 1 : 0) &&
            invocation.SetClockDoIoCalls ==
                (operation && definition.Mode == "LOAD" ? 1 : 0),
            "SetClock operation vector calls differ.");
        Require(invocation.SetClockPutStrCalls ==
                (operation && definition.TimerIoFailure ? 1 : 0),
            "SetClock timer diagnostic output differs.");
        invocation.SetClockLayout = null;
    }

    private void RegisterSetClockEntryExec()
    {
        Register(ExecBase, ExecLvo.OpenResource, "OpenResource",
            (state, invocation) =>
            {
                Require(Bus.CString(state.A[1]) == "battclock.resource",
                    "SetClock resource name differs.");
                invocation.SetClockOpenResourceCalls++;
                return invocation.Definition.SetClock!.MissingResource
                    ? 0u : SetClockBattClockBase;
            });
        Register(ExecBase, ExecLvo.CreateMsgPort, "CreateMsgPort",
            (state, invocation) =>
            {
                Require(state.A[6] == ExecBase,
                    "SetClock CreateMsgPort A6 differs.");
                invocation.SetClockMsgPortCreates++;
                if (invocation.Definition.SetClock!.PortAllocationFailure)
                    return 0;
                var port = Bus.Allocate(invocation, MsgPort.Size,
                    "SetClockMsgPort", true);
                return port;
            });
        Register(ExecBase, ExecLvo.DeleteMsgPort, "DeleteMsgPort",
            (state, invocation) =>
            {
                Bus.Release(invocation, state.A[0], "SetClockMsgPort");
                invocation.SetClockMsgPortDeletes++;
                return 0;
            });
        Register(ExecBase, ExecLvo.CreateIORequest, "CreateIORequest",
            (state, invocation) =>
            {
                Require(state.A[0] != 0 && state.D[0] == TimerRequest.Size,
                    "SetClock CreateIORequest ABI differs.");
                invocation.SetClockIoRequestCreates++;
                if (invocation.Definition.SetClock!.RequestAllocationFailure)
                    return 0;
                return Bus.Allocate(invocation, TimerRequest.Size,
                    "SetClockTimerRequest", true);
            });
        Register(ExecBase, ExecLvo.DeleteIORequest, "DeleteIORequest",
            (state, invocation) =>
            {
                Bus.Release(invocation, state.A[0], "SetClockTimerRequest");
                invocation.SetClockIoRequestDeletes++;
                return 0;
            });
        Register(ExecBase, ExecLvo.OpenDevice, "OpenDevice",
            (state, invocation) =>
            {
                Require(Bus.CString(state.A[0]) == TimerDevice.Name &&
                    state.D[0] == (uint)TimerUnit.VBlank && state.D[1] == 0 &&
                    state.A[1] != 0,
                    "SetClock timer.device OpenDevice ABI differs.");
                invocation.SetClockOpenDeviceCalls++;
                if (invocation.Definition.SetClock!.DeviceOpenFailure)
                    return 1;
                Bus.Long(state.A[1] + (uint)ExecLayout.IORequest.Device,
                    SetClockTimerBase);
                return 0;
            });
        Register(ExecBase, ExecLvo.DoIO, "DoIO", (state, invocation) =>
        {
            var definition = invocation.Definition.SetClock!;
            var expectedCommand = definition.UtcProviders
                ? (ushort)((ushort)TimerCommand.SetSystemTime + 2)
                : (ushort)TimerCommand.SetSystemTime;
            Require(state.A[1] != 0 &&
                Bus.Word(state.A[1] + (uint)ExecLayout.IORequest.Command) ==
                expectedCommand &&
                (Bus.Memory[state.A[1] + (uint)ExecLayout.IORequest.Flags] &
                    (byte)IOFlags.Quick) != 0,
                $"SetClock timer request ABI differs (cmd={Bus.Word(state.A[1] + (uint)ExecLayout.IORequest.Command)}, flags={Bus.Memory[state.A[1] + (uint)ExecLayout.IORequest.Flags]}).");
            invocation.SetClockDoIoCalls++;
            Bus.Memory[state.A[1] + (uint)ExecLayout.IORequest.Error] =
                invocation.Definition.SetClock!.TimerIoFailure ? (byte)1 : (byte)0;
            return 0;
        });
        Register(ExecBase, ExecLvo.CloseDevice, "CloseDevice",
            (state, invocation) =>
            {
                Require(state.A[1] != 0,
                    "SetClock closed a null timer request.");
                invocation.SetClockCloseDeviceCalls++;
                return 0;
            });

        Bus.RegisterGateway(SetClockBattClockBase - 12u, state =>
        {
            var invocation = Bus.Current!;
            Require(state.A[6] == SetClockBattClockBase,
                "ReadBattClock A6 differs.");
            invocation.SetClockReadCalls++;
            state.D[0] = SetClockBattClockSeconds;
        });
        Bus.RegisterGateway(SetClockBattClockBase - 18u, state =>
        {
            var invocation = Bus.Current!;
            Require(state.A[6] == SetClockBattClockBase &&
                state.D[0] == SetClockSystemSeconds,
                "WriteBattClock ABI differs.");
            invocation.SetClockWriteCalls++;
        });
        Bus.RegisterGateway(SetClockBattClockBase - 6u, state =>
        {
            var invocation = Bus.Current!;
            Require(state.A[6] == SetClockBattClockBase,
                "ResetBattClock A6 differs.");
            invocation.SetClockResetCalls++;
        });
        Bus.RegisterGateway(SetClockBattClockBase - 40u, state =>
        {
            var invocation = Bus.Current!;
            Require(state.A[6] == SetClockBattClockBase,
                "ReadUTCBattClock A6 differs.");
            invocation.SetClockReadUtcCalls++;
            state.D[0] = SetClockBattClockSeconds;
        });
        Bus.RegisterGateway(SetClockBattClockBase - 46u, state =>
        {
            var invocation = Bus.Current!;
            Require(state.A[6] == SetClockBattClockBase &&
                state.D[0] == SetClockSystemSeconds,
                "WriteUTCBattClock ABI differs.");
            invocation.SetClockWriteUtcCalls++;
        });
        Bus.RegisterGateway(SetClockTimerBase - 66u, state =>
        {
            var invocation = Bus.Current!;
            Require(state.A[6] == SetClockTimerBase && state.A[0] != 0,
                "GetSysTime ABI differs.");
            Bus.Long(state.A[0], SetClockSystemSeconds);
            Bus.Long(state.A[0] + 4, 0);
            invocation.SetClockGetSysTimeCalls++;
        });
        Bus.RegisterGateway(SetClockTimerBase - 88u, state =>
        {
            var invocation = Bus.Current!;
            Require(state.A[6] == SetClockTimerBase && state.A[0] != 0,
                "GetUTCSysTime ABI differs.");
            Bus.Long(state.A[0], SetClockSystemSeconds);
            Bus.Long(state.A[0] + 4, 0);
            invocation.SetClockGetUtcSysTimeCalls++;
        });
    }

    private void RegisterSetClockDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs",
            (state, invocation) =>
            {
                var definition = invocation.Definition.SetClock!;
                var template = suite == Workbench31SetClockEntrySuite
                    ? NativeWorkbench31SetClockCommand.Template
                    : NativeMorphOSSetClockCommand.Template;
                Require(Bus.CString(state.D[1]) == template &&
                    state.D[3] == 0 &&
                    Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 12,
                    "SetClock ReadArgs ABI differs.");
                for (var offset = 0u; offset < 12; offset += 4)
                    Require(Bus.Long(state.D[2] + offset) == 0,
                        "SetClock result slots were not cleared.");
                invocation.Reads++;
                if (definition.ParserFailure)
                {
                    invocation.IoError = 115;
                    return 0;
                }
                var result = definition.Mode switch
                {
                    "LOAD" => 0u,
                    "SAVE" => 4u,
                    "RESET" => 8u,
                    _ => 0u
                };
                if (definition.Mode.Length != 0)
                    Bus.Long(state.D[2] + result, uint.MaxValue);
                return Bus.Allocate(invocation, 40, "RDArgs", true);
            });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs",
            (state, invocation) =>
            {
                Bus.Release(invocation, state.D[1], "RDArgs");
                invocation.FreeArgs++;
                return 0;
            });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            invocation.SetClockPutStrCalls++;
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                Bus.CString(state.D[1])));
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (_, _) => 0);
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }
}

internal sealed class SetClockNativeLayout(uint battClock, uint timer)
{
    public uint BattClock { get; } = battClock;
    public uint Timer { get; } = timer;
}
