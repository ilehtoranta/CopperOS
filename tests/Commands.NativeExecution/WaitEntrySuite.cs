using static CopperOS.Commands.NativeExecution.CommandTestBus;
using System.Text;
using Amiga;

namespace CopperOS.Commands.NativeExecution;

internal sealed record WaitEntryCase(int? Number, bool Seconds, bool Minutes,
    string? Until, int ParserError = 0, bool BreakPending = false,
    bool TimerOpenFailure = false, bool AllocationFailure = false,
    bool TimerIoFailure = false);

internal sealed partial class ProbeFixture
{
    public const string Workbench31WaitEntrySuite =
        "workbench31-wait-native-entry-vector-fixture";

    private void RegisterWorkbench31WaitEntryExec()
    {
        // Workbench and MorphOS share the Exec timer/signal ABI for the
        // long-wait path.  Keep the registration explicit for this suite so
        // the resident body cannot silently fall back to an unregistered
        // vector.
        RegisterMorphOSWaitEntryExec();
    }

    private List<object> RunWorkbench31WaitEntryCases()
    {
        ProbeCase[] cases =
        [
            new("default-one-second", "", DOS.RETURN_OK, 0, "")
            { Wait = new(null, false, false, null) },
            new("explicit-seconds", "7 SEC\n", DOS.RETURN_OK, 0, "")
            { Wait = new(7, true, false, null) },
            new("explicit-minutes", "3 MINS\n", DOS.RETURN_OK, 0, "")
            { Wait = new(3, false, true, null) },
            new("missing-number-minute-default", "MIN\n", DOS.RETURN_OK, 0, "")
            { Wait = new(null, false, true, null) },
            new("zero-seconds", "0 SEC\n", DOS.RETURN_OK, 0, "")
            { Wait = new(0, true, false, null) },
            new("both-units-minute-precedence", "2 SEC MIN\n", DOS.RETURN_OK, 0, "")
            { Wait = new(2, true, true, null) },
            new("until-time", "UNTIL 12:34\n", DOS.RETURN_OK, 0, "")
            { Wait = new(null, false, false, "12:34") },
            new("until-invalid", "UNTIL 12:345\n", DOS.RETURN_FAIL,
                (int)DOS.Error.BadTemplate, "")
            { Wait = new(null, false, false, "12:345") },
            new("negative-number", "-1\n", DOS.RETURN_FAIL,
                (int)DOS.Error.BadNumber, "")
            { Wait = new(-1, false, false, null) },
            new("parser-failure", "7\n", DOS.RETURN_ERROR, 116, "")
            { Wait = new(7, false, false, null, ParserError: 116) },
            new("timer-open-failure", "7 SEC\n", DOS.RETURN_FAIL,
                (int)DOS.Error.NotImplemented, "")
            { Wait = new(7, true, false, null, TimerOpenFailure: true) },
            new("break-pending", "7 SEC\n", DOS.RETURN_WARN, 0, "")
            { Wait = new(7, true, false, null, BreakPending: true) },
            new("allocation-failure", "7 SEC\n", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "")
            { Wait = new(7, true, false, null, AllocationFailure: true) },
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Workbench = true, Wait = new(null, false, false, null) },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { MissingDos = true, Wait = new(null, false, false, null) },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[1] with { Name = "interleaved-seconds" },
            cases[2] with { Name = "interleaved-minutes" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyWorkbench31WaitEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Wait!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.Reads == 0 && invocation.FreeArgs == 0 &&
                invocation.Allocations == 0 && invocation.FreeMem == 0 &&
                invocation.WaitDelayCalls == 0 &&
                invocation.WaitMsgPortCreates == 0 &&
                invocation.WaitMsgPortDeletes == 0 &&
                invocation.WaitIoRequestCreates == 0 &&
                invocation.WaitIoRequestDeletes == 0 &&
                invocation.TimerOpenCalls == 0 && invocation.TimerCloseCalls == 0 &&
                invocation.WaitSendIoCalls == 0 && invocation.WaitSignalWaitCalls == 0 &&
                invocation.WaitCheckIoCalls == 0 && invocation.WaitAbortIoCalls == 0 &&
                invocation.WaitIoCalls == 0 && invocation.WaitPrintFaultCalls == 0 &&
                invocation.WaitPutStrCalls == 0,
                "Wait startup boundary reached the command body.");
            return;
        }
        var parserFailure = definition.ParserError != 0;
        var allocationFailure = definition.AllocationFailure;
        var invalidUntil = definition.Until is not null && definition.Until.Length > 5;
        var executes = !parserFailure && !allocationFailure && !invalidUntil &&
            (definition.Number is null || definition.Number >= 0);
        var delay = definition.Until is not null ? 34 * 60 * 50 :
            (definition.Number ?? 1) * (definition.Minutes ? 3000 : 50);
        var timer = executes && delay > 50;
        Require(invocation.Reads == (allocationFailure ? 0 : 1) &&
            invocation.FreeArgs == (parserFailure || allocationFailure ? 0 : 1),
            "Wait parser ownership differs.");
        var expectedAllocations = allocationFailure ? 1 :
            definition.Until is not null ? 2 : 1;
        Require(invocation.Allocations == expectedAllocations &&
            invocation.FreeMem == (allocationFailure ? 0 : expectedAllocations) &&
            invocation.AllocationRequests.SequenceEqual(definition.Until is not null
                ? new uint[] { 16, 64 } : new uint[] { 16 }),
            "Wait result allocation cleanup differs.");
        Require(invocation.WaitDelayCalls == (executes && !timer ? 1 : 0) &&
            invocation.WaitDelayTicks == (executes && !timer ? delay : 0),
            "Wait Delay path differs.");
        Require(invocation.WaitMsgPortCreates == (timer ? 1 : 0) &&
            invocation.WaitMsgPortDeletes == (timer ? 1 : 0) &&
            invocation.WaitIoRequestCreates == (timer ? 1 : 0) &&
            invocation.WaitIoRequestDeletes == (timer ? 1 : 0),
            "Wait timer resource lifetime differs.");
        Require(invocation.TimerOpenCalls == (timer ? 1 : 0) &&
            invocation.TimerCloseCalls == (timer && !definition.TimerOpenFailure ? 1 : 0),
            "Wait timer open/close path differs.");
        Require(invocation.WaitSendIoCalls == (timer && !definition.TimerOpenFailure ? 1 : 0) &&
            invocation.WaitSignalWaitCalls == (timer && !definition.TimerOpenFailure ? 1 : 0),
            "Wait asynchronous timer path differs.");
        Require(invocation.WaitCheckIoCalls == (timer && definition.BreakPending ? 1 : 0) &&
            invocation.WaitAbortIoCalls == (timer && definition.BreakPending ? 1 : 0) &&
            invocation.WaitIoCalls == (timer && definition.BreakPending ? 1 : 0),
            "Wait Ctrl-C cancellation differs.");
        Require(invocation.WaitPrintFaultCalls == (parserFailure || allocationFailure ? 1 : 0) &&
            invocation.WaitPutStrCalls == (invalidUntil || definition.TimerOpenFailure ? 1 : 0),
            $"Wait diagnostics differ for {invocation.Definition.Name} (fault={invocation.WaitPrintFaultCalls}, expectedFault={(parserFailure || allocationFailure ? 1 : 0)}, put={invocation.WaitPutStrCalls}, expectedPut={(invalidUntil || definition.TimerOpenFailure ? 1 : 0)}, events={string.Join(',', invocation.Events)}).");
    }

    private void RegisterWorkbench31WaitDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Wait!;
            Require(Bus.CString(state.D[1]) ==
                "/N,SEC=SECS/S,MIN=MINS/S,UNTIL/K" && state.D[3] == 0,
                "Wait ReadArgs template or ABI differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 16,
                "Wait result storage differs.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 40, "WaitRDArgs", true);
            if (definition.Number is int number)
            {
                var numberAddress = invocation.Arguments + 0x400;
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
            invocation.IoError = 901;
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.Delay, "Delay", (state, invocation) =>
        {
            var definition = invocation.Definition.Wait!;
            Require(definition.ParserError == 0 && definition.Until is null,
                "Wait Delay reached an invalid parser path.");
            var expected = (definition.Number ?? 1) *
                (definition.Minutes ? 3000 : 50);
            Require(unchecked((int)state.D[1]) == expected,
                "Wait Delay ABI or tick conversion differs.");
            invocation.WaitDelayCalls++;
            invocation.WaitDelayTicks = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.DateStamp, "DateStamp", (state, invocation) =>
        {
            Require(invocation.Definition.Wait!.Until is not null && state.D[1] != 0,
                "Wait DateStamp ABI differs.");
            Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Days, 0);
            Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Minutes, 720);
            Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Ticks, 0);
            invocation.DateStampCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.StrToDate, "StrToDate", (state, invocation) =>
        {
            Require(invocation.Definition.Wait!.Until is not null && state.D[1] != 0,
                "Wait StrToDate ABI differs.");
            Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Minutes, 754);
            Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Ticks, 0);
            invocation.StrToDateCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Wait!;
                Require(unchecked((int)state.D[1]) == invocation.Definition.Error &&
                    Bus.CString(state.D[2]) == "Wait",
                    "Wait PrintFault ABI differs.");
                invocation.WaitPrintFaultCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            var text = Bus.CString(state.D[1]);
            var definition = invocation.Definition.Wait!;
            Require(text == "Time should be HH:MM" ||
                text == "Wait: Could not open timer.device!",
                "Wait diagnostic text differs.");
            Require(definition.Until is not null || definition.TimerOpenFailure,
                "Wait emitted an unexpected diagnostic.");
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
