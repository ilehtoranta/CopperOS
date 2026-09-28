using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    public const string RebootEntrySuite =
        "morphos320-reboot-native-entry-vector-fixture";
    public const string Workbench31RebootEntrySuite =
        "workbench31-reboot-native-entry-vector-fixture";

    private List<object> RunRebootEntryCases()
    {
        ProbeCase[] cases =
        [
            RebootCase("success"),
            RebootCase("ignored-command-tail", arguments: "ignored"),
            RebootCase("ctrl-c", result: DOS.RETURN_ERROR,
                error: (int)DOS.Error.Break, ctrlC: true),
            RebootCase("parser-failure", result: 666,
                error: 116, parserError: 116),
            RebootCase("dos-open-failure", result: DOS.RETURN_FAIL,
                error: Invocation.InitialIoError, missingDos: true),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Reboot = true, Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Reboot = true, EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Reboot = true, EntryLength = 4, NullArgumentPointer = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            RebootCase("interleaved-left"),
            RebootCase("interleaved-right", arguments: "tail")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase RebootCase(string name, string arguments = "",
        int result = 666, int error = 0, int parserError = 0,
        bool ctrlC = false, bool missingDos = false) =>
        new(name, arguments, result, error, "")
        {
            Reboot = true,
            RebootParserError = parserError,
            RebootCtrlC = ctrlC,
            MissingDos = missingDos
        };

    private void RegisterRebootEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == NativeMorphOSRebootCommand.Template &&
                state.D[2] != 0 && state.D[3] == 0,
                "Reboot ReadArgs ABI differs.");
            invocation.RebootReadArgsCalls++;
            var definition = invocation.Definition;
            if (definition.RebootParserError != 0)
            {
                invocation.IoError = definition.RebootParserError;
                return 0;
            }
            return Bus.Allocate(invocation, 16, "RDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.RebootFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (_, invocation) => { invocation.RebootPrintFaultCalls++; return 0; });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyRebootEntry(Invocation invocation)
    {
        var definition = invocation.Definition;
        if (definition.Workbench || definition.EntryLength is < 0 ||
            definition.NullArgumentPointer)
        {
            Require(invocation.RebootReadArgsCalls == 0 &&
                invocation.RebootSetSignalCalls == 0 &&
                invocation.RebootColdRebootCalls == 0,
                "Reboot parsed an invalid startup boundary.");
            return;
        }
        if (definition.MissingDos)
        {
            Require(invocation.RebootReadArgsCalls == 0 &&
                invocation.RebootColdRebootCalls == 0,
                "Reboot used DOS after an open failure.");
            return;
        }
        var parsed = definition.RebootParserError == 0;
        Require(invocation.RebootReadArgsCalls == 1 &&
            invocation.RebootFreeArgsCalls == (parsed ? 1 : 0),
            "Reboot parser lifetime differs.");
        Require(invocation.RebootSetSignalCalls == 1 &&
            invocation.RebootPrintFaultCalls ==
                (definition.RebootCtrlC ? 1 : 0) &&
            invocation.RebootColdRebootCalls ==
                (definition.RebootCtrlC ? 0 : 1),
            "Reboot control flow differs.");
    }
}
