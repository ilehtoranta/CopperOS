using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    public const string BeepEntrySuite =
        "morphos320-beep-native-entry-vector-fixture";

    private void RegisterBeepEntryExec()
    {
        Register(IntuitionBase, IntuitionLvo.DisplayBeep, "DisplayBeep",
            (state, invocation) =>
            {
                Require(invocation.Definition.Beep && state.A[0] == 0,
                    "Beep DisplayBeep ABI differs.");
                invocation.DisplayBeeps++;
                return 0;
            });
    }

    private List<object> RunBeepEntryCases()
    {
        ProbeCase[] cases =
        [
            new("beep", "", DOS.RETURN_OK, Invocation.InitialIoError, "")
            { Beep = true },
            new("ignored-arguments", "ignored arguments\n", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            { Beep = true },
            new("workbench-startup", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            { Beep = true, Workbench = true },
            new("intuition-open-failure", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { Beep = true, BeepOpenFailure = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([cases[0] with { Name = "beep-repeat" }], false));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyBeepEntry(Invocation invocation)
    {
        var failed = invocation.Definition.BeepOpenFailure;
        Require(invocation.IntuitionOpens == 1 &&
            invocation.IntuitionCloses == (failed ? 0 : 1) &&
            invocation.DisplayBeeps == (failed ? 0 : 1),
            "Beep intuition ownership or call count differs.");
        Require(invocation.Opens == 0 && invocation.Closes == 0 &&
            invocation.Reads == 0 && invocation.FreeArgs == 0 &&
            invocation.Allocations == 0 && invocation.FreeMem == 0,
            "Beep unexpectedly used DOS parser or guest allocation.");
    }
}
