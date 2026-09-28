using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    public const string Workbench31BreakEntrySuite =
        "workbench31-break-native-entry-vector-fixture";
    public const string Workbench31ChangeTaskPriEntrySuite =
        "workbench31-changetaskpri-native-entry-vector-fixture";

    private void RegisterWorkbench31BreakEntryExec()
    {
        // Workbench 3.1 uses only the classic DOS/Exec vectors.  In
        // particular, do not install the MorphOS pointer-indirect PID slot.
    }

    private void RegisterWorkbench31ChangeTaskPriEntryExec()
    {
        // The classic profile has no MorphOS-only Exec extension to install.
    }

    private List<object> RunWorkbench31BreakEntryCases()
    {
        ProbeCase[] cases =
        [
            BreakCase("process-default", 7, null, DOS.RETURN_OK, 0, 0),
            BreakCase("process-all", 8, null, DOS.RETURN_OK, 0,
                BreakCtrlC | BreakCtrlD | BreakCtrlE | BreakCtrlF,
                all: true),
            BreakCase("process-combined", 9, null, DOS.RETURN_OK, 0,
                BreakCtrlD | BreakCtrlF, d: true, f: true),
            BreakCase("process-c", 11, null, DOS.RETURN_OK, 0,
                BreakCtrlC, c: true),
            BreakCase("process-d", 12, null, DOS.RETURN_OK, 0,
                BreakCtrlD, d: true),
            BreakCase("process-e", 13, null, DOS.RETURN_OK, 0,
                BreakCtrlE, e: true),
            BreakCase("process-f", 14, null, DOS.RETURN_OK, 0,
                BreakCtrlF, f: true),
            BreakCase("process-cd", 15, null, DOS.RETURN_OK, 0,
                BreakCtrlC | BreakCtrlD, c: true, d: true),
            BreakCase("process-ce", 16, null, DOS.RETURN_OK, 0,
                BreakCtrlC | BreakCtrlE, c: true, e: true),
            BreakCase("process-cf", 17, null, DOS.RETURN_OK, 0,
                BreakCtrlC | BreakCtrlF, c: true, f: true),
            BreakCase("process-de", 18, null, DOS.RETURN_OK, 0,
                BreakCtrlD | BreakCtrlE, d: true, e: true),
            BreakCase("process-ef", 19, null, DOS.RETURN_OK, 0,
                BreakCtrlE | BreakCtrlF, e: true, f: true),
            BreakCase("process-cde", 20, null, DOS.RETURN_OK, 0,
                BreakCtrlC | BreakCtrlD | BreakCtrlE,
                c: true, d: true, e: true),
            BreakCase("process-cdf", 21, null, DOS.RETURN_OK, 0,
                BreakCtrlC | BreakCtrlD | BreakCtrlF,
                c: true, d: true, f: true),
            BreakCase("process-cef", 22, null, DOS.RETURN_OK, 0,
                BreakCtrlC | BreakCtrlE | BreakCtrlF,
                c: true, e: true, f: true),
            BreakCase("process-def", 23, null, DOS.RETURN_OK, 0,
                BreakCtrlD | BreakCtrlE | BreakCtrlF,
                d: true, e: true, f: true),
            BreakCase("process-cdef", 24, null, DOS.RETURN_OK, 0,
                BreakCtrlC | BreakCtrlD | BreakCtrlE | BreakCtrlF,
                c: true, d: true, e: true, f: true),
            BreakCase("process-zero", 0, null, DOS.RETURN_FAIL, 0, 0,
                processFound: false,
                output: "Process 0 does not exist\n"),
            BreakCase("process-missing", 10, null, DOS.RETURN_FAIL, 0, 0,
                processFound: false,
                output: "Process 10 does not exist\n"),
            BreakCase("parser-failure", 7, null, DOS.RETURN_FAIL, 116, 0,
                parserError: 116)
        ];
        cases =
        [
            .. cases,
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            {
                Break = new(7, null, false, false, false, false, false),
                Workbench = true
            },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            {
                Break = new(7, null, false, false, false, false, false),
                MissingDos = true
            }
        ];
        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "interleaved-process" },
            cases[4] with { Name = "interleaved-missing" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyWorkbench31BreakEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Break!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.Reads == 0 && invocation.FreeArgs == 0 &&
                invocation.Allocations == 0 && invocation.FreeMem == 0 &&
                invocation.BreakFindCliProcCalls == 0 &&
                invocation.BreakSignals == 0,
                "Workbench Break crossed an invalid startup boundary.");
            return;
        }
        var parserFailure = definition.ParserError != 0;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "Workbench Break parser lifetime differs.");
        Require(invocation.Allocations == 1 && invocation.FreeMem == 1 &&
            invocation.AllocationRequests.SequenceEqual(new uint[] { 24 }),
            "Workbench Break result allocation cleanup differs.");
        var lookup = !parserFailure;
        Require(invocation.BreakFindCliProcCalls == (lookup ? 1 : 0) &&
            invocation.BreakFindTaskByPIDCalls == 0 &&
            invocation.BreakFindPortCalls == 0,
            "Workbench Break used a non-classic target lookup.");
        var found = lookup && definition.ProcessFound;
        Require(invocation.BreakSignals == (found ? 1 : 0),
            "Workbench Break Signal call count differs.");
        if (found)
        {
            var mask = definition.All ?
                BreakCtrlC | BreakCtrlD | BreakCtrlE | BreakCtrlF :
                (definition.C ? BreakCtrlC : 0) |
                (definition.D ? BreakCtrlD : 0) |
                (definition.E ? BreakCtrlE : 0) |
                (definition.F ? BreakCtrlF : 0);
            if (mask == 0) mask = BreakCtrlC;
            Require(invocation.BreakSignalMask == mask,
                "Workbench Break signal mask differs.");
        }
        Require(invocation.Events.Count(x => x == "Forbid") ==
                (parserFailure ? 0 : 1) &&
            invocation.Events.Count(x => x == "Permit") ==
                (parserFailure ? 0 : 1),
            "Workbench Break protection lifetime differs.");
    }

    private List<object> RunWorkbench31ChangeTaskPriEntryCases()
    {
        ProbeCase[] cases =
        [
            ChangeTaskPriCase("current-success", 1, null,
                DOS.RETURN_OK, 0),
            ChangeTaskPriCase("process-success", -4, 7,
                DOS.RETURN_OK, 0),
            ChangeTaskPriCase("priority-low-bound", -128, null,
                DOS.RETURN_OK, 0),
            ChangeTaskPriCase("priority-high-bound", 127, null,
                DOS.RETURN_OK, 0),
            ChangeTaskPriCase("priority-too-low", -129, null,
                DOS.RETURN_FAIL, 0,
                output: "Priority out of range (-128 to +127)\n"),
            ChangeTaskPriCase("priority-too-high", 128, null,
                DOS.RETURN_FAIL, 0,
                output: "Priority out of range (-128 to +127)\n"),
            ChangeTaskPriCase("range-before-missing-process", 128, 999999,
                DOS.RETURN_FAIL, 0, processFound: false,
                output: "Priority out of range (-128 to +127)\n"),
            ChangeTaskPriCase("process-missing", 2, 9,
                DOS.RETURN_FAIL, 0, processFound: false,
                output: "Process 9 does not exist\n"),
            ChangeTaskPriCase("parser-failure", 2, 9,
                DOS.RETURN_FAIL, 116, parserError: 116),
            ChangeTaskPriCase("explicit-process-zero-missing", 4, 0,
                DOS.RETURN_FAIL, 0, processFound: false,
                output: "Process 0 does not exist\n")
        ];
        cases =
        [
            .. cases,
            ChangeTaskPriCase("workbench-startup", 2, null,
                DOS.RETURN_ERROR, (int)DOS.Error.ObjectWrongType)
                with { Workbench = true },
            ChangeTaskPriCase("missing-dos", 2, null,
                DOS.RETURN_FAIL, Invocation.InitialIoError)
                with { MissingDos = true }
        ];
        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[1] with { Name = "interleaved-process" },
            cases[6] with { Name = "interleaved-missing" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyWorkbench31ChangeTaskPriEntry(Invocation invocation)
    {
        var definition = invocation.Definition.ChangeTaskPri!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.Reads == 0 && invocation.FreeArgs == 0 &&
                invocation.Allocations == 0 && invocation.FreeMem == 0 &&
                invocation.BreakFindCliProcCalls == 0 &&
                invocation.ChangeTaskPriCalls == 0,
                "Workbench ChangeTaskPri crossed an invalid startup boundary.");
            return;
        }
        var parserFailure = definition.ParserError != 0;
        var rangeFailure = !parserFailure &&
            (definition.Priority < -128 || definition.Priority > 127);
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "Workbench ChangeTaskPri parser lifetime differs.");
        Require(invocation.Allocations == 1 && invocation.FreeMem == 1 &&
            invocation.AllocationRequests.SequenceEqual(new uint[] { 8 }),
            "Workbench ChangeTaskPri result allocation cleanup differs.");
        var processLookup = !parserFailure && !rangeFailure &&
            definition.Process is not null;
        Require(invocation.BreakFindCliProcCalls ==
                (processLookup ? 1 : 0) &&
            invocation.ChangeTaskPriFindTaskByPIDCalls == 0,
            "Workbench ChangeTaskPri used a non-classic process lookup.");
        var found = !parserFailure && !rangeFailure &&
            (!processLookup || definition.ProcessFound);
        Require(invocation.ChangeTaskPriCalls ==
                (found && !rangeFailure ? 1 : 0),
            "Workbench ChangeTaskPri SetTaskPri call count differs.");
        if (found && !rangeFailure)
            Require(invocation.ChangeTaskPriPriority == definition.Priority,
                "Workbench ChangeTaskPri priority value differs.");
        var protectsTaskList = !parserFailure && !rangeFailure;
        Require(invocation.Events.Count(x => x == "Forbid") ==
                (protectsTaskList ? 1 : 0) &&
            invocation.Events.Count(x => x == "Permit") ==
                (protectsTaskList ? 1 : 0),
            "Workbench ChangeTaskPri protection lifetime differs.");
    }
}
