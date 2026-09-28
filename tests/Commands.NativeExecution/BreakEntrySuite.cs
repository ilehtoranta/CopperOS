using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record BreakEntryCase(uint? Process, string? Port,
    bool All = false, bool C = false, bool D = false, bool E = false,
    bool F = false, bool ProcessFound = true, bool PortFound = true,
    int ParserError = 0, bool PidFound = false, ushort ExecVersion = 50,
    ushort ExecRevision = 45);

internal sealed partial class ProbeFixture
{
    public const string BreakEntrySuite =
        "break-native-entry-vector-fixture";

    private const uint BreakCtrlC = 1u << 12;
    private const uint BreakCtrlD = 1u << 13;
    private const uint BreakCtrlE = 1u << 14;
    private const uint BreakCtrlF = 1u << 15;
    private const string ExpectedMorphOsBreakHelp =
        "Break : Set the attention flags of a DOS task\n" +
        "\tPROCESS/N  Process to set flags for\n" +
        "\tPORT       Portname for the Process to set flags for\n" +
        "\tALL/S      Set ALL attention flags\n" +
        "\tC/S        Set the CTRL-C flag\n" +
        "\tD/S        Set the CTRL-D flag\n" +
        "\tE/S        Set the CTRL-E flag\n" +
        "\tF/S        Set the CTRL-F flag\n";

    private List<object> RunBreakEntryCases()
    {
        List<ProbeCase> cases =
        [
            BreakCase("process-default", 7, null, DOS.RETURN_OK, 0, 0,
                processFound: true),
            BreakCase("process-all", 8, null, DOS.RETURN_OK, 0,
                BreakCtrlC | BreakCtrlD | BreakCtrlE | BreakCtrlF,
                all: true, processFound: true),
            BreakCase("process-combined", 9, null, DOS.RETURN_OK, 0,
                BreakCtrlD | BreakCtrlF, d: true, f: true,
                processFound: true),
            BreakCase("process-zero", 0, null, DOS.RETURN_FAIL, 0, 0,
                output: "Break: Process 0 does not exist.\n",
                processFound: false),
            BreakCase("process-missing", 10, null, DOS.RETURN_FAIL, 0, 0,
                output: "Break: Process 10 does not exist.\n",
                processFound: false),
            BreakCase("process-pid-fallback", 11, null, DOS.RETURN_OK, 0, 0,
                processFound: false, pidFound: true),
            BreakCase("pid-fallback-below-50-45-not-used", 13, null,
                DOS.RETURN_FAIL, 0, 0, processFound: false, pidFound: true,
                output: "Break: Process 13 does not exist.\n",
                execVersion: 50, execRevision: 44),
            BreakCase("pid-fallback-major-51", 14, null,
                DOS.RETURN_OK, 0, 0, processFound: false, pidFound: true,
                execVersion: 51, execRevision: 0),
            BreakCase("port-default", null, "FUBAR", DOS.RETURN_OK, 0,
                0, portFound: true),
            BreakCase("port-all", null, "FUBAR", DOS.RETURN_OK, 0,
                BreakCtrlC | BreakCtrlD | BreakCtrlE | BreakCtrlF,
                all: true, portFound: true),
            BreakCase("port-missing", null, "NOPE", DOS.RETURN_FAIL, 0, 0,
                output: "Break: Port \"NOPE\" does not exist.\n",
                portFound: false),
            BreakCase("neither", null, null, DOS.RETURN_FAIL, 0, 0,
                output: "Break: Either PROCESS or PORT is required.\n"),
            BreakCase("parser-failure", 7, null, DOS.RETURN_FAIL, 116, 0,
                parserError: 116)
        ];
        for (var flags = 0; flags < 16; flags++)
        {
            var mask = ((flags & 1) != 0 ? BreakCtrlC : 0) |
                ((flags & 2) != 0 ? BreakCtrlD : 0) |
                ((flags & 4) != 0 ? BreakCtrlE : 0) |
                ((flags & 8) != 0 ? BreakCtrlF : 0);
            cases.Add(BreakCase($"process-mask-{flags:X1}",
                (uint)(20 + flags), null, DOS.RETURN_OK, 0,
                mask == 0 ? BreakCtrlC : mask,
                c: (flags & 1) != 0, d: (flags & 2) != 0,
                e: (flags & 4) != 0, f: (flags & 8) != 0));
        }
        cases.Add(BreakCase("process-target-wins-over-port", 12, "FUBAR",
            DOS.RETURN_FAIL, 0, 0, processFound: false,
            output: "Break: Process 12 does not exist.\n"));
        cases.Add(BreakCase("process-zero-falls-through-to-port", 0, "FUBAR",
            DOS.RETURN_OK, 0, BreakCtrlC, processFound: false));
        cases.Add(BreakCase("process-zero-port-missing-diagnostic-priority",
            0, "NOPE", DOS.RETURN_FAIL, 0, 0, processFound: false,
            portFound: false, output: "Break: Process 0 does not exist.\n"));
        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "interleaved-process" },
            cases[8] with { Name = "interleaved-port-missing" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase BreakCase(string name, uint? process,
        string? port, int result, int error, uint mask,
        bool all = false, bool c = false, bool d = false, bool e = false,
        bool f = false, bool processFound = true, bool portFound = true,
        int parserError = 0, string output = "", bool pidFound = false,
        ushort execVersion = 50, ushort execRevision = 45) =>
        new(name, "", result, error, output)
        {
            Break = new(process, port, all, c, d, e, f, processFound,
                portFound, parserError, pidFound, execVersion, execRevision)
        };

    private void RegisterBreakEntryExec()
    {
        Bus.Word(ExecBase + (uint)ExecLayout.Library.Version, 50);
        Bus.Word(ExecBase + (uint)ExecLayout.Library.Revision, 45);
        RegisterFindTaskByPidGateway();
    }

    private void PrepareBreakEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Break ??
            throw new InvalidOperationException("Missing Break definition.");
        Bus.Word(ExecBase + (uint)ExecLayout.Library.Version,
            definition.ExecVersion);
        Bus.Word(ExecBase + (uint)ExecLayout.Library.Revision,
            definition.ExecRevision);
        var process = invocation.Arguments + 0x400;
        var port = invocation.Arguments + 0x500;
        var messagePort = invocation.Arguments + 0x700;
        if (definition.Process is uint value)
            Bus.Long(process, value);
        if (definition.Port is not null)
            PutBreakString(port, definition.Port);
        Bus.Long(messagePort + (uint)ExecLayout.MsgPort.SignalTask,
            invocation.Process + 0x900);
    }

    private void VerifyBreakEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Break!;
        var parserFailure = definition.ParserError != 0;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "Break parser lifetime differs.");
        var morphOs = suite == BreakEntrySuite;
        Require(invocation.ProcessControlRdArgsAllocations == (morphOs ? 1 : 0) &&
            invocation.ProcessControlRdArgsFrees == (morphOs ? 1 : 0),
            "Break explicit DOS_RDARGS lifetime differs.");
        Require(invocation.Allocations == 1 && invocation.FreeMem == 1 &&
            invocation.AllocationRequests.SequenceEqual(new uint[] { 28 }),
            "Break result allocation cleanup differs.");
        var processLookup = !parserFailure &&
            definition.Process is uint value && value != 0;
        var supportsPidLookup = definition.ExecVersion > 50 ||
            definition.ExecVersion == 50 && definition.ExecRevision >= 45;
        var usesPidFallback = processLookup && !definition.ProcessFound &&
            supportsPidLookup;
        var portLookup = !parserFailure && !processLookup &&
            definition.Port is not null;
        Require(invocation.BreakFindCliProcCalls == (processLookup ? 1 : 0) &&
            invocation.BreakFindTaskByPIDCalls ==
                (usesPidFallback ? 1 : 0) &&
            invocation.BreakFindPortCalls == (portLookup ? 1 : 0),
            $"Break target lookup path differs for {invocation.Definition.Name} (parser={definition.ParserError}, process={definition.Process}, port={definition.Port}, cli={invocation.BreakFindCliProcCalls}, portCalls={invocation.BreakFindPortCalls}, expectedCli={(processLookup ? 1 : 0)}, expectedPort={(portLookup ? 1 : 0)}, events={string.Join(',', invocation.Events)}).");
        var found = !parserFailure &&
            ((processLookup && (definition.ProcessFound ||
                    usesPidFallback && definition.PidFound)) ||
             (portLookup && definition.PortFound));
        Require(invocation.BreakSignals == (found ? 1 : 0),
            "Break Signal call count differs.");
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
                "Break signal mask differs.");
        }
        Require(invocation.Events.Count(x => x == "Forbid") ==
                (parserFailure ? 0 : 1) &&
            invocation.Events.Count(x => x == "Permit") ==
                (parserFailure ? 0 : 1),
            "Break protection lifetime differs.");
    }

    private void RegisterBreakEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.AllocDosObject, "AllocDosObject",
            (state, invocation) =>
            {
                Require(suite == BreakEntrySuite &&
                    state.D[1] == (uint)DosObjectType.RdArgs && state.D[2] == 0,
                    "MorphOS Break DOS_RDARGS allocation arguments differ.");
                invocation.ProcessControlRdArgsAllocations++;
                invocation.ProcessControlRdArgsObject = Bus.Allocate(
                    invocation, (uint)DosLayout.RDArgs.Size,
                    "ProcessControlDOS_RDArgs", true);
                return invocation.ProcessControlRdArgsObject;
            });
        Register(baseAddress, DosLvo.FreeDosObject, "FreeDosObject",
            (state, invocation) =>
            {
                Require(suite == BreakEntrySuite &&
                    state.D[1] == (uint)DosObjectType.RdArgs &&
                    state.D[2] == invocation.ProcessControlRdArgsObject,
                    "MorphOS Break freed the wrong DOS_RDARGS object.");
                Bus.Release(invocation, state.D[2],
                    "ProcessControlDOS_RDArgs");
                invocation.ProcessControlRdArgsFrees++;
                return 0;
            });
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Break!;
            var workbench = suite == Workbench31BreakEntrySuite;
            var expectedTemplate = workbench
                ? "PROCESS/A/N,ALL/S,C/S,D/S,E/S,F/S"
                : "PROCESS/N,PORT,ALL/S,C/S,D/S,E/S,F/S";
            var resultBytes = workbench ? 24u : 28u;
            Require(Bus.CString(state.D[1]) == expectedTemplate &&
                state.D[3] == (workbench ? 0 :
                    invocation.ProcessControlRdArgsObject),
                "Break ReadArgs template ABI differs.");
            if (!workbench)
            {
                var extendedHelp = Bus.Long(state.D[3] +
                    (uint)DosLayout.RDArgs.ExtendedHelp);
                Require(extendedHelp != 0 &&
                    Bus.CString(extendedHelp) == ExpectedMorphOsBreakHelp,
                    "MorphOS Break ReadArgs extended help differs.");
            }
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == resultBytes,
                "Break result storage differs.");
            for (var offset = 0u; offset < resultBytes; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "Break result slots were not cleared.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = workbench
                ? Bus.Allocate(invocation, 40, "RDArgs", true)
                : state.D[3];
            var process = invocation.Arguments + 0x400;
            var port = invocation.Arguments + 0x500;
            if (definition.Process is not null)
                Bus.Long(state.D[2], process);
            if (!workbench && definition.Port is not null)
                Bus.Long(state.D[2] + 4, port);
            var switchOffset = workbench ? 4u : 8u;
            Bus.Long(state.D[2] + switchOffset,
                definition.All ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + switchOffset + 4,
                definition.C ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + switchOffset + 8,
                definition.D ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + switchOffset + 12,
                definition.E ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + switchOffset + 16,
                definition.F ? uint.MaxValue : 0);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            if (suite == BreakEntrySuite)
                Require(state.D[1] == invocation.ProcessControlRdArgsObject,
                    "MorphOS Break FreeArgs did not receive its caller-owned RDArgs.");
            else
                Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.FindCliProc, "FindCliProc",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Break!;
                var workbench = suite == Workbench31BreakEntrySuite;
                Require(invocation.Forbidden &&
                    definition.Process is uint value &&
                    (workbench || value != 0) && state.D[1] == value,
                    "Break FindCliProc target or Forbid protection differs.");
                invocation.BreakFindCliProcCalls++;
                return definition.ProcessFound ? invocation.Process + 0x900 : 0;
            });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "Break FPuts stream differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                Bus.CString(state.D[2])));
            return 0;
        });
        Register(baseAddress, DosLvo.VFPrintf, "VFPrintf",
            (state, invocation) =>
            {
                Require(state.D[1] == invocation.OutputBptr,
                    "Break VFPrintf stream differs.");
                var format = Bus.CString(state.D[2]);
                var definition = invocation.Definition.Break!;
                var workbench = suite == Workbench31BreakEntrySuite;
                var processFormat = workbench
                    ? "Process %ld does not exist\n"
                    : "Break: Process %ld does not exist.\n";
                if (format == processFormat)
                {
                    var process = definition.Process ?? 0;
                    Require(definition.Process is not null &&
                        Bus.Long(state.D[3]) == process,
                    "Break process diagnostic argument differs.");
                    invocation.Output.Write(Encoding.Latin1.GetBytes(
                        workbench
                            ? $"Process {process} does not exist\n"
                            : $"Break: Process {process} does not exist.\n"));
                }
                else if (format == "Break: Port \"%s\" does not exist.\n")
                {
                    Require(definition.Port is not null &&
                        Bus.CString(state.D[3]) == definition.Port,
                        "Break port diagnostic argument differs.");
                    invocation.Output.Write(Encoding.Latin1.GetBytes(
                        $"Break: Port \"{definition.Port}\" does not exist.\n"));
                }
                else
                    throw new InvalidOperationException(
                        "Unexpected Break VFPrintf format.");
                return 0;
            });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(invocation.Definition.Break!.ParserError != 0 &&
                    unchecked((int)state.D[1]) == invocation.Definition.Break.ParserError,
                    "Break PrintFault error differs.");
                if (suite == Workbench31BreakEntrySuite &&
                    invocation.Definition.Break.ParserError != 0)
                    Require(state.D[2] == 0,
                        "Workbench Break ReadArgs fault must not include a header.");
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

    private void PutBreakString(uint address, string value)
    {
        Encoding.Latin1.GetBytes(value).CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)value.Length] = 0;
    }
}

internal sealed partial class Invocation
{
    public int ProcessControlRdArgsAllocations { get; set; }
    public int ProcessControlRdArgsFrees { get; set; }
    public uint ProcessControlRdArgsObject { get; set; }
}
