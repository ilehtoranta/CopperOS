using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record WaitForNotificationEntryCase(
    string[] Names,
    bool Quiet = false,
    bool Continue = false,
    int FailedIndex = -1,
    bool CtrlC = false,
    int ParserError = 0);

internal sealed partial class ProbeFixture
{
    public const string WaitForNotificationEntrySuite =
        "morphos320-waitfornotification-native-entry-vector-fixture";

    private const string WaitForNotificationTemplate =
        "NAME/A/M,QUIET/S,CONTINUE=CNT/S";
    private const uint WaitForNotificationControlC = 1u << 12;
    private const byte WaitForNotificationSignal = 6;

    private List<object> RunWaitForNotificationEntryCases()
    {
        ProbeCase[] cases =
        [
            new("single-name", "Work:log", DOS.RETURN_OK, 0, "")
            { WaitForNotification = new(["Work:log"]) },
            new("multiple-names", "Work:log SYS:Prefs", DOS.RETURN_OK, 0, "")
            { WaitForNotification = new(["Work:log", "SYS:Prefs"]) },
            new("quiet", "Work:log QUIET", DOS.RETURN_OK, 0, "")
            { WaitForNotification = new(["Work:log"], Quiet: true) },
            new("failed-registration", "Missing", DOS.RETURN_FAIL,
                (int)DOS.Error.ObjectNotFound, "")
            { WaitForNotification = new(["Missing"], FailedIndex: 0) },
            new("late-registration-failure", "Work:log Missing",
                DOS.RETURN_FAIL, (int)DOS.Error.ObjectNotFound, "")
            { WaitForNotification = new(["Work:log", "Missing"],
                FailedIndex: 1) },
            new("continue-registration", "Missing Work:log CNT",
                DOS.RETURN_OK, 0, "")
            { WaitForNotification = new(["Missing", "Work:log"],
                Continue: true, FailedIndex: 0) },
            new("all-registration-failures", "Missing Other CNT",
                DOS.RETURN_FAIL, (int)DOS.Error.ObjectNotFound, "")
            { WaitForNotification = new(["Missing", "Other"],
                Continue: true, FailedIndex: -2) },
            new("ctrl-c", "Work:log", DOS.RETURN_WARN,
                (int)DOS.Error.Break, "")
            { WaitForNotification = new(["Work:log"], CtrlC: true) },
            new("parser-failure", "Work:log", DOS.RETURN_ERROR, 116, "")
            { WaitForNotification = new(["Work:log"], ParserError: 116) },
            new("workbench-startup", "Work:log", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { WaitForNotification = new(["Work:log"]), Workbench = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "single-left" },
            cases[1] with { Name = "multiple-right" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void RegisterWaitForNotificationEntryExec()
    {
        Register(ExecBase, ExecLvo.AllocSignal, "AllocSignal",
            (state, invocation) =>
            {
                Require(state.D[0] == uint.MaxValue,
                    "WaitForNotification must allocate any signal bit.");
                invocation.WaitForNotificationAllocSignalCalls++;
                return WaitForNotificationSignal;
            });
        Register(ExecBase, ExecLvo.FreeSignal, "FreeSignal",
            (state, invocation) =>
            {
                Require(state.D[0] == WaitForNotificationSignal,
                    "WaitForNotification freed the wrong signal bit.");
                invocation.WaitForNotificationFreeSignalCalls++;
                return 0;
            });
        Register(ExecBase, ExecLvo.Wait, "Wait",
            (state, invocation) =>
            {
                var definition = invocation.Definition.WaitForNotification!;
                Require(state.D[0] ==
                    (WaitForNotificationControlC |
                        (1u << WaitForNotificationSignal)),
                    "WaitForNotification signal mask differs.");
                invocation.WaitForNotificationWaitCalls++;
                return definition.CtrlC ? WaitForNotificationControlC :
                    1u << WaitForNotificationSignal;
            });
    }

    private void RegisterWaitForNotificationEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs",
            (state, invocation) =>
            {
                var definition = invocation.Definition.WaitForNotification!;
                Require(Bus.CString(state.D[1]) ==
                    WaitForNotificationTemplate && state.D[3] == 0 &&
                    Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size ==
                        12,
                    "WaitForNotification ReadArgs ABI differs.");
                for (var offset = 0u; offset < 12; offset += 4)
                    Require(Bus.Long(state.D[2] + offset) == 0,
                        "WaitForNotification result slots were not cleared.");
                invocation.Reads++;
                if (definition.ParserError != 0)
                {
                    invocation.IoError = definition.ParserError;
                    return 0;
                }

                var rdArgs = Bus.Allocate(invocation, 40, "RDArgs", true);
                var vector = invocation.Arguments + 0x300;
                for (var index = 0; index < definition.Names.Length; index++)
                {
                    var name = invocation.Arguments + 0x400u +
                        (uint)index * 0x40u;
                    WriteWaitForNotificationString(name,
                        definition.Names[index]);
                    Bus.Long(vector + (uint)index * 4u, name);
                }
                Bus.Long(vector + (uint)definition.Names.Length * 4u, 0);
                Bus.Long(state.D[2], vector);
                if (definition.Quiet) Bus.Long(state.D[2] + 4, uint.MaxValue);
                if (definition.Continue) Bus.Long(state.D[2] + 8, uint.MaxValue);
                invocation.WaitForNotificationReadArgs = state.D[2];
                return rdArgs;
            });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs",
            (state, invocation) =>
            {
                Bus.Release(invocation, state.D[1], "RDArgs");
                invocation.FreeArgs++;
                return 0;
            });
        Register(baseAddress, DosLvo.StartNotify, "StartNotify",
            (state, invocation) =>
            {
                var definition = invocation.Definition.WaitForNotification!;
                var request = state.D[1];
                Require(request != 0 &&
                    Bus.OwnedAllocationContaining(invocation, request, "Exec").Size ==
                        definition.Names.Length * (uint)DosLayout.NotifyRequest.Size,
                    "WaitForNotification request allocation differs.");
                var name = APTR.FromPointer(
                    Bus.Long(request + (uint)DosLayout.NotifyRequest.Name));
                Require(Bus.Long(request + (uint)DosLayout.NotifyRequest.Flags) ==
                    (uint)(DosNotifyFlags.SendSignal |
                        DosNotifyFlags.NotifyInitial) &&
                    Bus.Long(request + (uint)DosLayout.NotifyRequest.Target) ==
                        invocation.Process &&
                    Bus.Memory[(int)(request +
                        (uint)DosLayout.NotifyRequest.Target +
                        (uint)DosLayout.NotifyRequestTarget.SignalNumber)] ==
                        WaitForNotificationSignal,
                    "WaitForNotification request fields differ.");
                invocation.WaitForNotificationStartNotifyCalls++;
                var index = definition.Names
                    .Select((value, position) =>
                        (value, position))
                    .First(pair => Bus.CString(name.Raw) == pair.value).position;
                if (definition.FailedIndex == -2 || index == definition.FailedIndex)
                {
                    invocation.IoError = (int)DOS.Error.ObjectNotFound;
                    return 0;
                }
                invocation.WaitForNotificationStarted++;
                return 1;
            });
        Register(baseAddress, DosLvo.EndNotify, "EndNotify",
            (state, invocation) =>
            {
                Require(state.D[1] != 0,
                    "WaitForNotification ended a null request.");
                invocation.WaitForNotificationEndNotifyCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[2]) == "WaitForNotification",
                    "WaitForNotification fault header differs.");
                invocation.WaitForNotificationPrintFaultCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.IoErr, "IoErr",
            (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr",
            (state, invocation) =>
            {
                invocation.IoError = unchecked((int)state.D[1]);
                return 0;
            });
    }

    private void VerifyWaitForNotificationEntry(Invocation invocation)
    {
        var definition = invocation.Definition.WaitForNotification!;
        if (definition.ParserError != 0 || invocation.Definition.Workbench)
        {
            Require(invocation.WaitForNotificationStartNotifyCalls == 0,
                "WaitForNotification registered after parser/startup failure.");
            return;
        }

        Require(invocation.Reads == 1 && invocation.FreeArgs == 1,
            "WaitForNotification parser lifetime differs.");
        var expectedStarted = definition.FailedIndex == -1
            ? definition.Names.Length
                : definition.FailedIndex == -2
                    ? 0
                : definition.Continue
                ? Math.Max(0, definition.Names.Length - 1)
                : Math.Max(0, definition.FailedIndex);
        Require(invocation.WaitForNotificationStarted == expectedStarted,
            "WaitForNotification registration count differs.");
        Require(invocation.WaitForNotificationEndNotifyCalls == expectedStarted,
            "WaitForNotification EndNotify lifetime differs.");
        Require(invocation.WaitForNotificationAllocSignalCalls == 1 &&
            invocation.WaitForNotificationFreeSignalCalls == 1,
            "WaitForNotification signal lifetime differs.");
        var expectedWait = definition.FailedIndex >= 0 && !definition.Continue
            ? 0 : expectedStarted == 0 ? 0 : 1;
        Require(invocation.WaitForNotificationWaitCalls == expectedWait,
            "WaitForNotification wait count differs.");
    }

    private void WriteWaitForNotificationString(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }
}
