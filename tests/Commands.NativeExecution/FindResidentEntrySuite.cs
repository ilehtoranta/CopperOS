using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record FindResidentEntryCase(
    string Module = "dos.library",
    bool ResidentFound = true,
    bool ResultSlotMissing = false,
    int ParserError = 0);

internal sealed record FindResidentNativeLayout(uint Control, uint Module);

internal sealed partial class ProbeFixture
{
    public const string FindResidentEntrySuite =
        "workbench31-findresident-native-entry-vector-fixture";

    private List<object> RunFindResidentEntryCases()
    {
        ProbeCase[] cases =
        [
            FindResidentCase("found-resident", new(Module: "dos.library")),
            FindResidentCase("found-custom-resident", new(Module: "exec.library")),
            FindResidentCase("missing-resident",
                new(Module: "missing.library", ResidentFound: false),
                result: DOS.RETURN_WARN,
                error: (int)DOS.Error.ObjectNotFound),
            FindResidentCase("missing-result-slot",
                new(Module: "dos.library", ResultSlotMissing: true),
                result: DOS.RETURN_WARN,
                error: (int)DOS.Error.RequiredArgumentMissing),
            FindResidentCase("parser-failure",
                new(ParserError: 116), result: DOS.RETURN_ERROR, error: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { FindResident = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { FindResident = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { FindResident = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "dos.library", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { FindResident = new(), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            FindResidentCase("interleaved-found", new(Module: "dos.library")),
            FindResidentCase("interleaved-missing",
                new(Module: "missing.library", ResidentFound: false),
                result: DOS.RETURN_WARN,
                error: (int)DOS.Error.ObjectNotFound)
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase FindResidentCase(string name,
        FindResidentEntryCase definition, int result = DOS.RETURN_OK,
        int error = 0) => new(name, definition.Module, result, error, "")
        { FindResident = definition };

    private void PrepareFindResidentEntry(Invocation invocation)
    {
        var definition = invocation.Definition.FindResident ?? new();
        var control = invocation.Arguments + 0x300;
        var module = invocation.Arguments + 0x340;
        invocation.FindResidentLayout = new(control, module);
        WriteCString(module, definition.Module);
        Bus.Long(control, definition.ResultSlotMissing ? 0u : module);
    }

    private void RegisterFindResidentEntryExec()
    {
        // The common Exec registration below handles the lookup vector. This
        // method exists to keep suite-specific registration explicit, like the
        // other native command entries.
    }

    private void RegisterFindResidentEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.FindResident!;
            var layout = invocation.FindResidentLayout!;
            Require(Bus.CString(state.D[1]) == "MODULE/A" &&
                state.D[3] == 0 && state.D[2] % 4 == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 4 &&
                Bus.Long(state.D[2]) == 0,
                "FindResident ReadArgs ABI differs.");
            invocation.FindResidentReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }

            var rdArgs = Bus.Allocate(invocation, 40, "RDArgs", true);
            Bus.Long(state.D[2], definition.ResultSlotMissing ? 0u : layout.Module);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FindResidentFreeArgsCalls++;
            invocation.IoError = 901;
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state,
            invocation) =>
        {
            Require(state.D[2] == 0,
                "FindResident PrintFault header differs.");
            invocation.FindResidentPrintFaultCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
        {
            invocation.FindResidentIoErrCalls++;
            return unchecked((uint)invocation.IoError);
        });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state,
            invocation) =>
        {
            invocation.FindResidentSetIoErrCalls++;
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyFindResidentEntry(Invocation invocation)
    {
        var definition = invocation.Definition.FindResident!;
        var boundary = invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos;
        if (boundary)
        {
            Require(invocation.FindResidentReadArgsCalls == 0 &&
                invocation.FindResidentFindResidentCalls == 0 &&
                invocation.FindResidentPrintFaultCalls == 0,
                "FindResident crossed an invalid startup boundary.");
            return;
        }

        var parser = definition.ParserError != 0;
        Require(invocation.FindResidentReadArgsCalls == 1 &&
            invocation.FindResidentFreeArgsCalls == (parser ? 0 : 1) &&
            invocation.Allocations == 1 && invocation.FreeMem == 1,
            $"FindResident parser/result ownership differs (reads={invocation.FindResidentReadArgsCalls}, freeArgs={invocation.FindResidentFreeArgsCalls}, allocations={invocation.Allocations}, freeMem={invocation.FreeMem}, parser={parser}).");
        if (parser)
        {
            Require(invocation.FindResidentFindResidentCalls == 0 &&
                invocation.FindResidentPrintFaultCalls == 0 &&
                invocation.FindResidentIoErrCalls == 1 &&
                invocation.FindResidentSetIoErrCalls == 2,
                "FindResident parser failure crossed the lookup path.");
            return;
        }

        var missingArgument = definition.ResultSlotMissing;
        var missingResident = !definition.ResidentFound;
        Require(invocation.FindResidentFindResidentCalls ==
            (missingArgument ? 0 : 1),
            "FindResident lookup count differs.");
        Require(invocation.FindResidentPrintFaultCalls ==
            (missingArgument || missingResident ? 1 : 0),
            "FindResident PrintFault count differs.");
        Require(invocation.FindResidentIoErrCalls == 1 &&
            invocation.FindResidentSetIoErrCalls ==
            3,
            $"FindResident IoErr ownership differs (ioerr={invocation.FindResidentIoErrCalls}, set={invocation.FindResidentSetIoErrCalls}, missingArgument={missingArgument}, missingResident={missingResident}).");
    }
}
