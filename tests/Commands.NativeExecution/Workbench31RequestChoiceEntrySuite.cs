using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    public const string Workbench31RequestChoiceEntrySuite =
        "wb31-requestchoice-native-entry-vector-fixture";

    private void RegisterWorkbench31RequestChoiceEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.RequestChoice!;
            Require(Bus.CString(state.D[1]) ==
                NativeWorkbench31RequestChoiceCommand.Template && state.D[3] == 0,
                "Workbench RequestChoice ReadArgs template/source ABI differs.");
            var results = state.D[2];
            Require((results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size == 16,
                "Workbench RequestChoice result slots must be owned and LONG aligned.");
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
            WriteRequestCString(body, "Body");
            WriteRequestCString(invocation.Arguments + 0x160, "Okay");
            WriteRequestCString(invocation.Arguments + 0x168, "Cancel");
            Bus.Long(gadgets, invocation.Arguments + 0x160);
            Bus.Long(gadgets + 4, invocation.Arguments + 0x168);
            Bus.Long(gadgets + 8, 0);
            Bus.Long(results, title);
            Bus.Long(results + 4, body);
            Bus.Long(results + 8, gadgets);
            Bus.Long(results + 12, 0);
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
                "Workbench RequestChoice output format differs.");
            var value = unchecked((int)Bus.Long(state.D[2]));
            var expected = invocation.Definition.RequestChoice!.Choice;
            Require(value == expected,
                $"Workbench RequestChoice output differs: got {value}, expected {expected}.");
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

    private List<object> RunWorkbench31RequestChoiceEntryCases()
    {
        ProbeCase[] cases =
        [
            new("choice", "Title Body Okay Cancel", DOS.RETURN_OK, 0, "1\n")
            { RequestChoice = new() },
            new("percent-body", "Title Body Okay Cancel", DOS.RETURN_OK, 0, "1\n")
            { RequestChoice = new() },
            new("intuition-open-failure", "", DOS.RETURN_OK, 0, "")
            { RequestChoice = new(IntuitionOpenFailure: true) },
            new("lock-failure", "Title Body Okay Cancel", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "RequestChoice\n")
            { RequestChoice = new(LockFailure: true) },
            new("allocation-failure", "Title Body Okay Cancel", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "RequestChoice\n")
            { RequestChoice = new(EasyFailure: true) },
            new("parser-failure", "", DOS.RETURN_ERROR, 103, "RequestChoice\n")
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
}
