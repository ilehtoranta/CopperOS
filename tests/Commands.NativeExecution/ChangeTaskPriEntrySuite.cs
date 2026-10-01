using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record ChangeTaskPriEntryCase(int Priority,
    uint? Process, bool ProcessFound = true, int ParserError = 0,
    bool PidFound = false, uint CurrentErrorStream = 0,
    ushort ExecVersion = 50, ushort ExecRevision = 45);

internal sealed partial class ProbeFixture
{
    public const string ChangeTaskPriEntrySuite =
        "changetaskpri-native-entry-vector-fixture";
    private const string ExpectedMorphOsChangeTaskPriHelp =
        "ChangeTaskPri : Change the priority of a CLI task\n" +
        "\tPRI=PRIORITY/A/N  New priority of task\n" +
        "\tPROCESS/K        Optional process number of change\n";

    private List<object> RunChangeTaskPriEntryCases()
    {
        ProbeCase[] cases =
        [
            ChangeTaskPriCase("current-success", 1, null, DOS.RETURN_OK, 0),
            ChangeTaskPriCase("process-success", -4, 7, DOS.RETURN_OK, 0),
            ChangeTaskPriCase("process-pid-fallback", 3, 8, DOS.RETURN_OK, 0,
                processFound: false, pidFound: true),
            ChangeTaskPriCase("pid-fallback-below-50-45-not-used", 3, 11,
                DOS.RETURN_FAIL, 0, processFound: false, pidFound: true,
                output: "ChangeTaskPri: Process 11 does not exist.\n",
                execVersion: 50, execRevision: 44),
            ChangeTaskPriCase("pid-fallback-major-51", 3, 12,
                DOS.RETURN_OK, 0, processFound: false, pidFound: true,
                execVersion: 51, execRevision: 0),
            ChangeTaskPriCase("priority-low-bound", -128, null,
                DOS.RETURN_OK, 0),
            ChangeTaskPriCase("priority-high-bound", 127, null,
                DOS.RETURN_OK, 0),
            ChangeTaskPriCase("priority-too-low", -129, null,
                DOS.RETURN_FAIL, 207, parserFault: true),
            ChangeTaskPriCase("priority-too-high", 128, null,
                DOS.RETURN_FAIL, 207, parserFault: true),
            ChangeTaskPriCase("process-missing", 2, 9, DOS.RETURN_FAIL, 0,
                processFound: false,
                output: "ChangeTaskPri: Process 9 does not exist.\n"),
            ChangeTaskPriCase("process-missing-current-error-stream", 2, 10,
                DOS.RETURN_FAIL, 0, processFound: false,
                output: "ChangeTaskPri: Process 10 does not exist.\n",
                currentErrorStream: 0x510),
            ChangeTaskPriCase("parser-failure", 2, 9, DOS.RETURN_FAIL,
                116, parserError: 116),
            // MorphOS FindTaskByPID(0) is the explicit current-task selector.
            // Keep this distinct from omitting PROCESS, which takes the
            // classic FindTask(NULL) path above.
            ChangeTaskPriCase("process-zero-pid-current-task", 4, 0,
                DOS.RETURN_OK, 0, processFound: false, pidFound: true),
        ];
        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[1] with { Name = "interleaved-process" },
            cases[9] with { Name = "interleaved-missing" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase ChangeTaskPriCase(string name, int priority,
        uint? process, int result, int error, bool processFound = true,
        int parserError = 0, bool parserFault = false, string output = "",
        bool pidFound = false, uint currentErrorStream = 0,
        ushort execVersion = 50, ushort execRevision = 45) =>
        new(name, "", result, error, output)
        {
            Break = null,
            ChangeTaskPri = new(priority, process, processFound, parserError,
                pidFound, currentErrorStream, execVersion, execRevision),
            // MorphOS uses PrintFault for range errors; Workbench uses its
            // profile-specific custom text diagnostic instead.
            ParserError = parserError,
            FailSwitch = parserFault
        };

    private void RegisterChangeTaskPriEntryExec()
    {
        Bus.Word(ExecBase + (uint)ExecLayout.Library.Version, 50);
        Bus.Word(ExecBase + (uint)ExecLayout.Library.Revision, 45);
        RegisterFindTaskByPidGateway();
    }

    private void PrepareChangeTaskPriEntry(Invocation invocation)
    {
        var definition = invocation.Definition.ChangeTaskPri ??
            throw new InvalidOperationException(
                "Missing ChangeTaskPri definition.");
        Bus.Word(ExecBase + (uint)ExecLayout.Library.Version,
            definition.ExecVersion);
        Bus.Word(ExecBase + (uint)ExecLayout.Library.Revision,
            definition.ExecRevision);
        var priority = invocation.Arguments + 0x400;
        var process = invocation.Arguments + 0x500;
        Bus.Long(priority, unchecked((uint)definition.Priority));
        if (definition.Process is uint value)
            Bus.Long(process, value);
        Bus.Long(invocation.Process + (uint)DosLayout.Process.CurrentError,
            definition.CurrentErrorStream);
    }

    private void VerifyChangeTaskPriEntry(Invocation invocation)
    {
        var definition = invocation.Definition.ChangeTaskPri!;
        var parserFailure = definition.ParserError != 0;
        var rangeFailure = !parserFailure &&
            (definition.Priority < -128 || definition.Priority > 127);
        var supportsPidLookup = definition.ExecVersion > 50 ||
            definition.ExecVersion == 50 && definition.ExecRevision >= 45;
        var usesPidFallback = !parserFailure &&
            definition.Process is not null && !definition.ProcessFound &&
            supportsPidLookup;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "ChangeTaskPri parser lifetime differs.");
        var morphOs = suite == ChangeTaskPriEntrySuite;
        Require(invocation.ProcessControlRdArgsAllocations == (morphOs ? 1 : 0) &&
            invocation.ProcessControlRdArgsFrees == (morphOs ? 1 : 0),
            "ChangeTaskPri explicit DOS_RDARGS lifetime differs.");
        Require(invocation.Allocations == 1 && invocation.FreeMem == 1 &&
            invocation.AllocationRequests.SequenceEqual(new uint[] { 8 }),
            "ChangeTaskPri result allocation cleanup differs.");
        var processLookup = !parserFailure && definition.Process is not null;
        Require(invocation.BreakFindCliProcCalls == (processLookup ? 1 : 0) &&
            invocation.ChangeTaskPriFindTaskByPIDCalls ==
                (usesPidFallback ? 1 : 0),
            "ChangeTaskPri process lookup count differs.");
        var found = !parserFailure &&
            (!processLookup || definition.ProcessFound ||
                usesPidFallback && definition.PidFound);
        Require(invocation.ChangeTaskPriCalls ==
                (found && !rangeFailure ? 1 : 0),
            "ChangeTaskPri SetTaskPri call count differs.");
        if (found && !rangeFailure)
            Require(invocation.ChangeTaskPriPriority == definition.Priority,
                "ChangeTaskPri priority value differs.");
        Require(invocation.Events.Count(x => x == "Forbid") ==
                (parserFailure ? 0 : 1) &&
            invocation.Events.Count(x => x == "Permit") ==
                (parserFailure ? 0 : 1),
            "ChangeTaskPri protection lifetime differs.");
    }

    private void RegisterChangeTaskPriEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.AllocDosObject, "AllocDosObject",
            (state, invocation) =>
            {
                Require(suite == ChangeTaskPriEntrySuite &&
                    state.D[1] == (uint)DosObjectType.RdArgs && state.D[2] == 0,
                    "MorphOS ChangeTaskPri DOS_RDARGS allocation arguments differ.");
                invocation.ProcessControlRdArgsAllocations++;
                invocation.ProcessControlRdArgsObject = Bus.Allocate(
                    invocation, (uint)DosLayout.RDArgs.Size,
                    "ProcessControlDOS_RDArgs", true);
                return invocation.ProcessControlRdArgsObject;
            });
        Register(baseAddress, DosLvo.FreeDosObject, "FreeDosObject",
            (state, invocation) =>
            {
                Require(suite == ChangeTaskPriEntrySuite &&
                    state.D[1] == (uint)DosObjectType.RdArgs &&
                    state.D[2] == invocation.ProcessControlRdArgsObject,
                    "MorphOS ChangeTaskPri freed the wrong DOS_RDARGS object.");
                Bus.Release(invocation, state.D[2],
                    "ProcessControlDOS_RDArgs");
                invocation.ProcessControlRdArgsFrees++;
                return 0;
            });
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.ChangeTaskPri!;
            var morphOs = suite == ChangeTaskPriEntrySuite;
            Require(Bus.CString(state.D[1]) ==
                "PRI=PRIORITY/A/N,PROCESS/K/N" && state.D[3] ==
                    (morphOs ? invocation.ProcessControlRdArgsObject : 0),
                "ChangeTaskPri ReadArgs template ABI differs.");
            if (morphOs)
            {
                var extendedHelp = Bus.Long(state.D[3] +
                    (uint)DosLayout.RDArgs.ExtendedHelp);
                Require(extendedHelp != 0 &&
                    Bus.CString(extendedHelp) ==
                        ExpectedMorphOsChangeTaskPriHelp,
                    "MorphOS ChangeTaskPri ReadArgs extended help differs.");
            }
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 8,
                "ChangeTaskPri result storage differs.");
            for (var offset = 0u; offset < 8; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "ChangeTaskPri result slots were not cleared.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = morphOs
                ? state.D[3]
                : Bus.Allocate(invocation, 40, "RDArgs", true);
            Bus.Long(state.D[2], invocation.Arguments + 0x400);
            if (definition.Process is not null)
                Bus.Long(state.D[2] + 4, invocation.Arguments + 0x500);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            if (suite == ChangeTaskPriEntrySuite)
                Require(state.D[1] == invocation.ProcessControlRdArgsObject,
                    "MorphOS ChangeTaskPri FreeArgs did not receive its caller-owned RDArgs.");
            else
                Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.FindCliProc, "FindCliProc",
            (state, invocation) =>
            {
                var definition = invocation.Definition.ChangeTaskPri!;
                Require(invocation.Forbidden &&
                    definition.Process is uint value &&
                    state.D[1] == value,
                    "ChangeTaskPri FindCliProc target or Forbid protection differs.");
                invocation.BreakFindCliProcCalls++;
                return definition.ProcessFound ? invocation.Process + 0x900 : 0;
            });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.VFPrintf, "VFPrintf",
            (state, invocation) =>
            {
                var definition = invocation.Definition.ChangeTaskPri!;
                var expectedStream = suite == ChangeTaskPriEntrySuite &&
                    definition.CurrentErrorStream != 0
                    ? definition.CurrentErrorStream
                    : invocation.OutputBptr;
                var workbench = suite == Workbench31ChangeTaskPriEntrySuite;
                var expectedFormat = workbench
                    ? "Process %ld does not exist\n"
                    : "ChangeTaskPri: Process %ld does not exist.\n";
                Require(state.D[1] == expectedStream &&
                    Bus.CString(state.D[2]) == expectedFormat,
                    "ChangeTaskPri diagnostic stream or format differs.");
                var process = definition.Process ?? 0;
                Require(Bus.Long(state.D[3]) == process,
                    "ChangeTaskPri diagnostic argument differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    workbench
                        ? $"Process {process} does not exist\n"
                        : $"ChangeTaskPri: Process {process} does not exist.\n"));
                return 0;
            });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            Require(suite == Workbench31ChangeTaskPriEntrySuite &&
                Bus.CString(state.D[1]) ==
                    "Priority out of range (-128 to +127)\n",
                "ChangeTaskPri range diagnostic differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                "Priority out of range (-128 to +127)\n"));
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(unchecked((int)state.D[1]) ==
                    invocation.Definition.Error,
                    "ChangeTaskPri PrintFault error differs.");
                if (suite == Workbench31ChangeTaskPriEntrySuite &&
                    invocation.Definition.ChangeTaskPri!.ParserError != 0)
                    Require(state.D[2] == 0,
                        "Workbench ChangeTaskPri ReadArgs fault must not include a header.");
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
}
