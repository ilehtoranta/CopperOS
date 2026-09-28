using static CopperOS.Commands.NativeExecution.CommandTestBus;
using Amiga;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    public const string MorphOSWaitEntrySuite =
        "morphos320-wait-native-entry-vector-fixture";

    private const uint WaitControlC = 1u << 12;
    private const uint WaitTimerSignalBit = 5;
    private const string WaitTemplate = "TIME/N,SEC=SECS/S,MIN=MINS/S,UNTIL/K";

    private List<object> RunMorphOSWaitEntryCases()
    {
        ProbeCase[] cases =
        [
            new("default-one-second", "", DOS.RETURN_OK, 0, "")
            { Wait = new(null, false, false, null) },
            new("explicit-seconds-short", "1 SEC\n", DOS.RETURN_OK, 0, "")
            { Wait = new(1, true, false, null) },
            new("explicit-seconds-timer", "7 SEC\n", DOS.RETURN_OK, 0, "")
            { Wait = new(7, true, false, null) },
            new("explicit-minutes", "2 MIN\n", DOS.RETURN_OK, 0, "")
            { Wait = new(2, false, true, null) },
            new("both-units-minute-precedence", "2 SEC MIN\n", DOS.RETURN_OK, 0, "")
            { Wait = new(2, true, true, null) },
            new("zero-seconds", "0 SEC\n", DOS.RETURN_OK, 0, "")
            { Wait = new(0, true, false, null) },
            new("until-time", "UNTIL 12:34\n", DOS.RETURN_OK, 0, "")
            { Wait = new(null, false, false, "12:34") },
            new("until-invalid", "UNTIL 12:345\n", DOS.RETURN_FAIL,
                (int)DOS.Error.BadTemplate, "")
            { Wait = new(null, false, false, "12:345") },
            new("negative-number", "-1\n", DOS.RETURN_FAIL,
                (int)DOS.Error.BadNumber, "")
            { Wait = new(-1, false, false, null) },
            new("parser-failure", "7\n", DOS.RETURN_FAIL, 116, "")
            { Wait = new(7, false, false, null, ParserError: 116) },
            new("timer-open-failure", "7 SEC\n", DOS.RETURN_FAIL,
                (int)DOS.Error.NotImplemented, "")
            { Wait = new(7, true, false, null, TimerOpenFailure: true) },
            new("break-pending", "7 SEC\n", DOS.RETURN_WARN, 0, "")
            { Wait = new(7, true, false, null, BreakPending: true) },
            new("allocation-failure", "7 SEC\n", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "")
            { Wait = new(7, true, false, null, AllocationFailure: true) },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[2] with { Name = "interleaved-timer" },
            cases[6] with { Name = "interleaved-until" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyMorphOSWaitEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Wait!;
        var parserFailure = definition.ParserError != 0;
        var allocationFailure = definition.AllocationFailure;
        var invalidUntil = definition.Until is not null && definition.Until.Length > 5;
        var executes = !parserFailure && !allocationFailure && !invalidUntil &&
            (definition.Number is null || definition.Number >= 0);
        var delay = definition.Until is not null ? 34 * 50 :
            (definition.Number ?? 1) * (definition.Minutes ? 3000 : 50);
        var timer = executes && delay > 50;
        var expectedFreeArgs = parserFailure || allocationFailure ? 0 : 1;
        var expectedReads = allocationFailure ? 0 : 1;
        Require(invocation.Reads == expectedReads && invocation.FreeArgs == expectedFreeArgs,
            $"MorphOS Wait parser ownership differs for {invocation.Definition.Name} (reads={invocation.Reads}/{expectedReads}, freeArgs={invocation.FreeArgs}/{expectedFreeArgs}, events={string.Join(',', invocation.Events)}).");
        var expectedAllocations = allocationFailure ? 1 :
            definition.Until is not null ? 2 : 1;
        Require(invocation.Allocations == expectedAllocations &&
            invocation.FreeMem == (allocationFailure ? 0 : expectedAllocations),
            "MorphOS Wait workspace cleanup differs.");
        Require(invocation.WaitDelayCalls == (executes && !timer ? 1 : 0) &&
            invocation.WaitDelayTicks == (executes && !timer ? delay : 0),
            "MorphOS Wait Delay path differs.");
        Require(invocation.WaitMsgPortCreates == (timer ? 1 : 0) &&
            invocation.WaitMsgPortDeletes == (timer ? 1 : 0) &&
            invocation.WaitIoRequestCreates == (timer ? 1 : 0) &&
            invocation.WaitIoRequestDeletes == (timer ? 1 : 0),
            "MorphOS Wait timer resource lifetime differs.");
        Require(invocation.TimerOpenCalls == (timer ? 1 : 0) &&
            invocation.TimerCloseCalls == (timer && !definition.TimerOpenFailure ? 1 : 0),
            "MorphOS Wait timer open/close path differs.");
        Require(invocation.WaitSendIoCalls == (timer && !definition.TimerOpenFailure ? 1 : 0) &&
            invocation.WaitSignalWaitCalls == (timer && !definition.TimerOpenFailure ? 1 : 0),
            "MorphOS Wait asynchronous timer path differs.");
        Require(invocation.WaitCheckIoCalls == (timer && definition.BreakPending ? 1 : 0) &&
            invocation.WaitAbortIoCalls == (timer && definition.BreakPending ? 1 : 0) &&
            invocation.WaitIoCalls == (timer && definition.BreakPending ? 1 : 0),
            "MorphOS Wait Ctrl-C cancellation differs.");
        Require(invocation.WaitPrintFaultCalls ==
            (parserFailure || allocationFailure ? 1 : 0) &&
            invocation.WaitPutStrCalls == (invalidUntil ||
                definition.TimerOpenFailure ? 1 : 0),
            "MorphOS Wait diagnostics differ.");
    }

    private void RegisterMorphOSWaitEntryExec()
    {
        Register(ExecBase, ExecLvo.CreateMsgPort, "CreateMsgPort",
            (state, invocation) =>
            {
                Require(invocation.Definition.Wait is not null,
                    "MorphOS Wait created a port outside its suite.");
                invocation.WaitMsgPortCreates++;
                var port = Bus.Allocate(invocation, MsgPort.Size,
                    "WaitMsgPort", true);
                Bus.Memory[checked((int)port + ExecLayout.MsgPort.SignalBit)] =
                    (byte)WaitTimerSignalBit;
                return port;
            });
        Register(ExecBase, ExecLvo.DeleteMsgPort, "DeleteMsgPort",
            (state, invocation) =>
            {
                Bus.Release(invocation, state.A[0], "WaitMsgPort");
                invocation.WaitMsgPortDeletes++;
                return 0;
            });
        Register(ExecBase, ExecLvo.CreateIORequest, "CreateIORequest",
            (state, invocation) =>
            {
                Require(state.A[0] != 0 && state.D[0] == TimerRequest.Size,
                    "MorphOS Wait CreateIORequest ABI differs.");
                invocation.WaitIoRequestCreates++;
                return Bus.Allocate(invocation, TimerRequest.Size,
                    "WaitTimerRequest", true);
            });
        Register(ExecBase, ExecLvo.DeleteIORequest, "DeleteIORequest",
            (state, invocation) =>
            {
                Bus.Release(invocation, state.A[0], "WaitTimerRequest");
                invocation.WaitIoRequestDeletes++;
                return 0;
            });
        Register(ExecBase, ExecLvo.OpenDevice, "OpenDevice",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Wait!;
                Require(Bus.CString(state.A[0]) == TimerDevice.Name &&
                    state.D[0] == (uint)TimerUnit.VBlank && state.D[1] == 0 &&
                    state.A[1] != 0, "MorphOS Wait OpenDevice ABI differs.");
                invocation.TimerOpenCalls++;
                return definition.TimerOpenFailure ? 1u : 0u;
            });
        Register(ExecBase, ExecLvo.CloseDevice, "CloseDevice",
            (state, invocation) =>
            {
                Require(state.A[1] != 0, "MorphOS Wait closed a null request.");
                invocation.TimerCloseCalls++;
                return 0;
            });
        Register(ExecBase, ExecLvo.SendIO, "SendIO",
            (state, invocation) =>
            {
                Require(state.A[1] != 0 &&
                    Bus.Word(state.A[1] + (uint)ExecLayout.IORequest.Command) ==
                    (ushort)TimerCommand.AddRequest,
                    "MorphOS Wait timer request ABI differs.");
                invocation.WaitSendIoCalls++;
                return 0;
            });
        Register(ExecBase, ExecLvo.Wait, "Wait", (state, invocation) =>
        {
            var definition = invocation.Definition.Wait!;
            var expected = WaitControlC | (1u << (int)WaitTimerSignalBit);
            Require(state.D[0] == expected, "MorphOS Wait signal mask differs.");
            invocation.WaitSignalWaitCalls++;
            invocation.WaitSignalMask = state.D[0];
            return definition.BreakPending ? WaitControlC :
                1u << (int)WaitTimerSignalBit;
        });
        Register(ExecBase, ExecLvo.CheckIO, "CheckIO", (state, invocation) =>
        {
            Require(invocation.Definition.Wait!.BreakPending && state.A[1] != 0,
                "MorphOS Wait checked an unexpected timer request.");
            invocation.WaitCheckIoCalls++;
            return 0;
        });
        Register(ExecBase, ExecLvo.AbortIO, "AbortIO", (state, invocation) =>
        {
            Require(state.A[1] != 0, "MorphOS Wait aborted a null request.");
            invocation.WaitAbortIoCalls++;
            return 0;
        });
        Register(ExecBase, ExecLvo.WaitIO, "WaitIO",
            (state, invocation) =>
            {
                Require(state.A[1] != 0, "MorphOS Wait waited on a null request.");
                invocation.WaitIoCalls++;
                return 0;
            });
    }

    private void RegisterMorphOSWaitDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Wait!;
            Require(Bus.CString(state.D[1]) == WaitTemplate &&
                state.D[3] == 0 && Bus.OwnedAllocation(invocation, state.D[2],
                    "Exec").Size == 16,
                "MorphOS Wait template/result ABI differs.");
            for (var offset = 0u; offset < 16; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "MorphOS Wait result slots were not cleared.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 40, "WaitRDArgs", true);
            if (definition.Number is int number)
            {
                var numberAddress = invocation.Arguments + 0x500;
                Bus.Long(numberAddress, unchecked((uint)number));
                Bus.Long(state.D[2], numberAddress);
            }
            if (definition.Seconds) Bus.Long(state.D[2] + 4, 1);
            if (definition.Minutes) Bus.Long(state.D[2] + 8, 1);
            if (definition.Until is not null)
            {
                var until = invocation.Arguments + 0x400;
                WriteArgument(until, definition.Until);
                Bus.Long(state.D[2] + 12, until);
            }
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "WaitRDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.DateStamp, "DateStamp",
            (state, invocation) =>
            {
                Require(invocation.Definition.Wait!.Until is not null &&
                    state.D[1] != 0, "MorphOS Wait DateStamp ABI differs.");
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Days, 0);
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Minutes, 720);
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Ticks, 0);
                invocation.DateStampCalls++;
                return 1;
            });
        Register(baseAddress, DosLvo.StrToDate, "StrToDate",
            (state, invocation) =>
            {
                Require(invocation.Definition.Wait!.Until is not null &&
                    state.D[1] != 0, "MorphOS Wait StrToDate ABI differs.");
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Minutes, 754);
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Ticks, 0);
                invocation.StrToDateCalls++;
                return 1;
            });
        Register(baseAddress, DosLvo.Delay, "Delay", (state, invocation) =>
        {
            var definition = invocation.Definition.Wait!;
            var expected = definition.Until is not null ? 0 :
                (definition.Number ?? 1) * (definition.Minutes ? 3000 : 50);
            Require(!definition.TimerOpenFailure && !definition.BreakPending &&
                state.D[1] == unchecked((uint)expected),
                "MorphOS Wait Delay ABI or conversion differs.");
            invocation.WaitDelayCalls++;
            invocation.WaitDelayTicks = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[2]) == "Wait" && state.D[2] != 0,
                    "MorphOS Wait PrintFault ABI differs.");
                invocation.WaitPrintFaultCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            var text = Bus.CString(state.D[1]);
            var definition = invocation.Definition.Wait!;
            Require(text == "Time should be HH:MM" ||
                text == "Wait: Could not open timer.device!",
                "MorphOS Wait diagnostic text differs.");
            Require(definition.Until is not null || definition.TimerOpenFailure,
                "MorphOS Wait emitted an unexpected diagnostic.");
            invocation.WaitPutStrCalls++;
            return 0;
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
