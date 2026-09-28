using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record RequestChoiceEntryCase(
    bool Timeout = false,
    bool IntuitionOpenFailure = false,
    bool LockFailure = false,
    bool EasyFailure = false,
    int Choice = 1,
    bool TimerOpenFailure = false,
    bool TimerPortFailure = false,
    bool TimerRequestFailure = false,
    bool ControlC = false,
    bool Workbench = false,
    bool MissingDos = false,
    int ParserError = 0);

internal sealed partial class ProbeFixture
{
    public const string RequestChoiceEntrySuite =
        "morphos320-requestchoice-native-entry-vector-fixture";

    private void RegisterRequestChoiceEntryExec()
    {
        Register(IntuitionBase, IntuitionLvo.LockPubScreen, "LockPubScreen",
            (state, invocation) =>
            {
                Require(invocation.Definition.RequestChoice is not null &&
                    state.A[0] == 0, "RequestChoice public-screen ABI differs.");
                invocation.RequestChoiceLocks++;
                Bus.Long(invocation.Process + 0x704, invocation.Process + 0x710);
                return invocation.Definition.RequestChoice!.LockFailure ? 0u :
                    invocation.Process + 0x700;
            });
        Register(IntuitionBase, IntuitionLvo.UnlockPubScreen, "UnlockPubScreen",
            (state, invocation) =>
            {
                Require(state.A[0] == 0 && state.A[1] == invocation.Process + 0x700,
                    "RequestChoice unlock ABI differs.");
                invocation.RequestChoiceUnlocks++;
                return 0;
            });
        Register(IntuitionBase, IntuitionLvo.EasyRequestArgs,
            "EasyRequestArgs", (state, invocation) =>
            {
                Require(state.A[0] == invocation.Process + 0x710 &&
                    state.A[1] != 0 && state.A[2] == 0 && state.A[3] == 0,
                    $"RequestChoice EasyRequestArgs ABI differs: A0=${state.A[0]:X8} A1=${state.A[1]:X8} A2=${state.A[2]:X8} A3=${state.A[3]:X8}.");
                invocation.RequestChoiceEasyCalls++;
                return unchecked((uint)(invocation.Definition.RequestChoice!.EasyFailure
                    ? -1 : invocation.Definition.RequestChoice.Choice));
            });
        Register(IntuitionBase, IntuitionLvo.BuildEasyRequestArgs,
            "BuildEasyRequestArgs", (state, invocation) =>
            {
                Require(state.A[0] == invocation.Process + 0x710 &&
                    state.A[1] != 0 && state.D[0] == 0 && state.A[3] == 0,
                    "RequestChoice BuildEasyRequestArgs ABI differs.");
                invocation.RequestChoiceBuildCalls++;
                if (invocation.Definition.RequestChoice!.EasyFailure) return 0;
                Bus.Long(invocation.Process + 0x704, invocation.Process + 0x710);
                Bus.Long(invocation.Process + 0x710 + 86,
                    invocation.Process + 0x720);
                Bus.Memory[invocation.Process + 0x720 + 15] = 4;
                return invocation.Process + 0x810u;
            });
        Register(IntuitionBase, IntuitionLvo.SysReqHandler,
            "SysReqHandler", (state, invocation) =>
            {
                Require(state.A[0] != 0 && state.A[1] == 0 &&
                    (state.D[0] == 0 || state.D[0] == 1),
                    "RequestChoice SysReqHandler ABI differs.");
                invocation.RequestChoiceSysReqCalls++;
                if (state.D[0] == 1) return unchecked((uint)
                    invocation.Definition.RequestChoice!.Choice);
                return unchecked((uint)(invocation.Definition.RequestChoice!.ControlC
                    ? -2 : invocation.Definition.RequestChoice.Choice));
            });
        Register(IntuitionBase, IntuitionLvo.FreeSysRequest,
            "FreeSysRequest", (state, invocation) =>
            {
                Require(state.A[0] != 0, "RequestChoice FreeSysRequest ABI differs.");
                invocation.RequestChoiceFreeCalls++;
                return 0;
            });
    }

    private void RegisterRequestChoiceEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.RequestChoice!;
            Require(Bus.CString(state.D[1]) ==
                NativeMorphOSRequestChoiceCommand.Template && state.D[3] == 0,
                "RequestChoice ReadArgs template/source ABI differs.");
            var results = state.D[2];
            Require((results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size == 24,
                "RequestChoice result slots must be owned and LONG aligned.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 40, "RDArgs", true);
            var title = invocation.Arguments + 0x100;
            var body = invocation.Arguments + 0x120;
            var gadgets = invocation.Arguments + 0x140;
            WriteRequestCString(title, "Title");
            WriteRequestCString(body, definition.Timeout ? "Body %s" : "Body");
            WriteRequestCString(invocation.Arguments + 0x160, "Okay");
            WriteRequestCString(invocation.Arguments + 0x168, "Cancel");
            Bus.Long(gadgets, invocation.Arguments + 0x160);
            Bus.Long(gadgets + 4, invocation.Arguments + 0x168);
            Bus.Long(gadgets + 8, 0);
            Bus.Long(results, title);
            Bus.Long(results + 4, body);
            Bus.Long(results + 8, gadgets);
            Bus.Long(results + 12, 0);
            Bus.Long(results + 16, 0);
            if (definition.Timeout)
            {
                Bus.Long(invocation.Arguments + 0x180, 2);
                Bus.Long(results + 20, invocation.Arguments + 0x180);
            }
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == "%ld\n" && state.D[2] != 0,
                "RequestChoice output format differs.");
            var value = unchecked((int)Bus.Long(state.D[2]));
            var expected = invocation.Definition.RequestChoice!.Choice;
            if (invocation.Definition.RequestChoice.Timeout &&
                !invocation.Definition.RequestChoice.TimerOpenFailure &&
                !invocation.Definition.RequestChoice.TimerPortFailure &&
                !invocation.Definition.RequestChoice.TimerRequestFailure) expected = 0;
            Require(value == expected,
                $"RequestChoice output value differs: got {value}, expected {expected}.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(value + "\n"));
            return unchecked((uint)(value + 1));
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (_, invocation) =>
        {
            invocation.Output.Write(Encoding.Latin1.GetBytes("RequestChoice\n"));
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void RegisterRequestChoiceEntryTimer(uint baseAddress)
    {
        Register(baseAddress, ExecLvo.CreateMsgPort, "CreateMsgPort",
            (_, invocation) => invocation.Definition.RequestChoice!.TimerPortFailure
                ? 0u : Bus.Allocate(invocation, 64, "TimerPort", true));
        Register(baseAddress, ExecLvo.DeleteMsgPort, "DeleteMsgPort",
            (state, invocation) => { Bus.Release(invocation, state.A[0], "TimerPort"); return 0; });
        Register(baseAddress, ExecLvo.CreateIORequest, "CreateIORequest",
            (state, invocation) => invocation.Definition.RequestChoice!.TimerRequestFailure
                ? 0u : Bus.Allocate(invocation, state.D[0], "TimerRequest", true));
        Register(baseAddress, ExecLvo.DeleteIORequest, "DeleteIORequest",
            (state, invocation) => { Bus.Release(invocation, state.A[0], "TimerRequest"); return 0; });
        Register(baseAddress, ExecLvo.OpenDevice, "OpenDevice",
            (state, invocation) =>
            {
                Require(Bus.CString(state.A[0]) == TimerDevice.Name &&
                    state.D[0] == (uint)TimerUnit.VBlank,
                    "RequestChoice timer open ABI differs.");
                invocation.TimerOpenCalls++;
                return invocation.Definition.RequestChoice!.TimerOpenFailure ? (byte)1 : (byte)0;
            });
        Register(baseAddress, ExecLvo.CloseDevice, "CloseDevice",
            (_, invocation) => { invocation.TimerCloseCalls++; return 0; });
        Register(baseAddress, ExecLvo.SendIO, "SendIO", (_, _) => 0);
        Register(baseAddress, ExecLvo.AbortIO, "AbortIO",
            (_, invocation) => { invocation.RequestChoiceTimerAborts++; return 0; });
        Register(baseAddress, ExecLvo.WaitIO, "WaitIO",
            (_, invocation) => { invocation.WaitIoCalls++; return 0; });
        Register(baseAddress, ExecLvo.CheckIO, "CheckIO", (_, _) => 0);
        Register(baseAddress, ExecLvo.Wait, "Wait", (_, invocation) =>
        {
            invocation.RequestChoiceTimerWaits++;
            return invocation.Definition.RequestChoice!.ControlC ? 1u << 12 : 1u;
        });
    }

    private List<object> RunRequestChoiceEntryCases()
    {
        ProbeCase[] cases =
        [
            new("choice", "Title Body Okay Cancel", DOS.RETURN_OK, 0, "1\n")
            { RequestChoice = new() },
            new("percent-body", "Title Body Okay Cancel", DOS.RETURN_OK, 0, "1\n")
            { RequestChoice = new() },
            new("timeout", "Title Body Okay Cancel", DOS.RETURN_OK, 0, "0\n")
            { RequestChoice = new(Timeout: true) },
            new("timeout-control-c", "Title Body Okay Cancel", DOS.RETURN_OK, 0, "0\n")
            { RequestChoice = new(Timeout: true, ControlC: true) },
            new("intuition-open-failure", "", DOS.RETURN_OK, 0, "")
            { RequestChoice = new(IntuitionOpenFailure: true) },
            new("lock-failure", "Title Body Okay Cancel", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "RequestChoice\n")
            { RequestChoice = new(LockFailure: true) },
            new("allocation-failure", "Title Body Okay Cancel", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "RequestChoice\n")
            { RequestChoice = new(EasyFailure: true) },
            new("timer-open-failure", "Title Body Okay Cancel", DOS.RETURN_OK,
                0, "1\n")
            { RequestChoice = new(Timeout: true, TimerOpenFailure: true) },
            new("timer-port-failure", "Title Body Okay Cancel", DOS.RETURN_OK,
                0, "1\n")
            { RequestChoice = new(Timeout: true, TimerPortFailure: true) },
            new("timer-request-failure", "Title Body Okay Cancel", DOS.RETURN_OK,
                0, "1\n")
            { RequestChoice = new(Timeout: true, TimerRequestFailure: true) },
            new("parser-failure", "", DOS.RETURN_FAIL, 103, "RequestChoice\n")
            { RequestChoice = new(ParserError: 103) },
            new("missing-dos", "", DOS.RETURN_FAIL,
                (int)DOS.Error.InvalidResidentLibrary, "")
            { MissingDos = true, WritesOwnProcessError = true,
                RequestChoice = new(MissingDos: true) },
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Workbench = true, RequestChoice = new(Workbench: true) },
        ];
        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([cases[0] with { Name = "choice-repeat" }], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "choice-interleaved-left" },
            cases[1] with { Name = "percent-interleaved-right" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyRequestChoiceEntry(Invocation invocation)
    {
        var definition = invocation.Definition.RequestChoice!;
        if (definition.MissingDos)
        {
            Require(invocation.Reads == 0 && invocation.IntuitionOpens == 0,
                "RequestChoice missing-DOS startup reached command services.");
            return;
        }
        if (definition.Workbench)
        {
            Require(invocation.RequestChoiceLocks == 0 && invocation.Reads == 0,
                "Workbench RequestChoice startup reached command body.");
            return;
        }
        if (definition.IntuitionOpenFailure)
        {
            Require(invocation.RequestChoiceLocks == 0 && invocation.Opens == 1,
                "RequestChoice Intuition open failure was not isolated.");
            return;
        }
        if (definition.ParserError != 0)
        {
            Require(invocation.RequestChoiceLocks == 0 && invocation.FreeArgs == 0,
                "RequestChoice parser failure unexpectedly reached Intuition.");
            return;
        }
        if (definition.EasyFailure)
        {
            Require(invocation.RequestChoiceLocks == 0 &&
                invocation.FreeArgs == 1 && invocation.Allocations == 2,
                $"RequestChoice allocation failure was not contained before Intuition (locks={invocation.RequestChoiceLocks}, freeArgs={invocation.FreeArgs}, allocations={invocation.Allocations}).");
            return;
        }
        if (definition.LockFailure)
        {
            Require(invocation.RequestChoiceLocks == 1 &&
                invocation.RequestChoiceUnlocks == 0 && invocation.FreeArgs == 1,
                "RequestChoice public-screen lock failure was not contained.");
            return;
        }
        Require(invocation.RequestChoiceLocks == 1 &&
            invocation.RequestChoiceUnlocks == 1 && invocation.FreeArgs == 1,
            "RequestChoice requester/parser ownership differs.");
        if (definition.TimerOpenFailure || definition.TimerPortFailure ||
            definition.TimerRequestFailure)
            Require(invocation.RequestChoiceBuildCalls == 1 &&
                invocation.RequestChoiceFreeCalls == 1 &&
                invocation.TimerCloseCalls == 0,
                "RequestChoice timed-request fallback requester lifecycle differs.");
        else if (definition.Timeout)
            Require(invocation.RequestChoiceBuildCalls == 1 &&
                invocation.RequestChoiceFreeCalls == 1 &&
                invocation.TimerOpenCalls == 1 && invocation.TimerCloseCalls == 1,
                "RequestChoice timeout lifecycle differs.");
        else
            Require(invocation.RequestChoiceEasyCalls == 1 &&
                invocation.RequestChoiceBuildCalls == 0,
                "RequestChoice EasyRequest path differs.");
        if (definition.TimerOpenFailure || definition.TimerPortFailure ||
            definition.TimerRequestFailure)
            Require(invocation.TimerOpenCalls == (definition.TimerOpenFailure ? 1 : 0) &&
                invocation.TimerCloseCalls == 0 &&
                invocation.RequestChoiceSysReqCalls == 1 &&
                invocation.RequestChoiceFreeCalls == 1 &&
                invocation.WaitIoCalls == 0,
                "RequestChoice timed-request fallback lifecycle differs.");
    }

    private void WriteRequestCString(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }
}
