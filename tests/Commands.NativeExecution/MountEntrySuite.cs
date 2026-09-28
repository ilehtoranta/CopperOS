using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record MountEntryCase(bool NoDevices = true,
    int StartupArguments = 0, bool NullArgumentList = false,
    bool StartupSourceMissing = false);

internal sealed partial class ProbeFixture
{
    public const string MountEntrySuite =
        "mount-morphos-native-entry-boundary-fixture";
    public const string Workbench31MountEntrySuite =
        "mount-wb31-native-entry-boundary-fixture";

    private List<object> RunMountEntryCases() => RunMountEntryCases(false);

    private List<object> RunWorkbenchMountEntryCases() => RunMountEntryCases(true);

    private List<object> RunMountEntryCases(bool workbench)
    {
        Require(workbench == (suite == Workbench31MountEntrySuite),
            "Mount profile selector does not match the qualification suite.");
        ProbeCase[] cases =
        [
            new("empty-device-vector", "", DOS.RETURN_OK,
                Invocation.InitialIoError, "")
            {
                Mount = new()
            },
            new("parser-failure", "", DOS.RETURN_ERROR, 116, "")
            {
                Mount = new(), ParserError = 116
            },
            new("result-allocation-failure", "", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "")
            {
                Mount = new(), AllocationFailure = true
            }
        ];
        if (workbench)
        {
            cases = cases.Append(
                new ProbeCase("workbench-startup-empty", "", DOS.RETURN_OK, 0, "")
                {
                    Workbench = true, Mount = new()
                }).Append(
                new ProbeCase("workbench-startup-single-argument-noop", "",
                    DOS.RETURN_OK, 0, "")
                {
                    Workbench = true,
                    Mount = new(StartupArguments: 1)
                }).Append(
                new ProbeCase("workbench-startup-source-missing", "",
                    DOS.RETURN_FAIL, (int)DOS.Error.ObjectNotFound, "")
                {
                    Workbench = true,
                    Mount = new(StartupArguments: 2,
                        StartupSourceMissing: true)
                }).Append(
                new ProbeCase("workbench-startup-missing-argument-list", "",
                    DOS.RETURN_FAIL, (int)DOS.Error.RequiredArgumentMissing, "")
                {
                    Workbench = true,
                    Mount = new(StartupArguments: 2, NullArgumentList: true)
                }).ToArray();
        }

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute(
            [cases[0] with { Name = "repeat-empty-device-vector" },
             cases[1] with { Name = "repeat-parser-failure" }], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void PrepareMountEntry(Invocation invocation)
    {
        var mount = invocation.Definition.Mount!;
        if (invocation.Definition.Workbench && mount.StartupArguments != 0)
        {
            Bus.Long(invocation.Message + 28u,
                unchecked((uint)mount.StartupArguments));
            if (mount.StartupSourceMissing)
            {
                var argumentList = invocation.Arguments + 0x200u;
                var source = invocation.Arguments + 0x300u;
                Bus.Long(argumentList, 0);
                Bus.Long(argumentList + 4u, 0);
                Bus.Long(argumentList + 8u, 0x710u);
                Bus.Long(argumentList + 12u, source);
                var sourceBytes = System.Text.Encoding.Latin1.GetBytes(
                    "MISSING:mount\0");
                sourceBytes.CopyTo(Bus.Memory.AsSpan((int)source));
                Bus.Long(invocation.Message + 36u, argumentList);
            }
            else
                Bus.Long(invocation.Message + 36u,
                    mount.NullArgumentList ? 0u : invocation.Arguments);
        }
        // The bounded CLI fixture intentionally exercises the empty DEVICE/M
        // vector. ReadArgs owns and clears the result storage; no source text
        // or provider state is needed for this ABI boundary gate.
    }

    private void VerifyMountEntry(Invocation invocation)
    {
        var definition = invocation.Definition;
        var parserFailure = definition.ParserError != 0;
        var allocationFailure = definition.AllocationFailure;

        if (definition.Workbench)
        {
            if (definition.Mount!.StartupSourceMissing)
            {
                Require(invocation.Reads == 0 && invocation.FreeArgs == 0 &&
                    invocation.Allocations == 0 && invocation.FreeMem == 0,
                    "Mount Workbench startup source failure parsed CLI state.");
                Require(invocation.MountOpenCalls == 1 &&
                    invocation.MountCurrentDirCalls == 2 &&
                    invocation.MountFilePartCalls == 1 &&
                    invocation.MountExpansionOpens == 1 &&
                    invocation.MountExpansionCloses == 1 &&
                    invocation.MountIconOpens == 1 &&
                    invocation.MountPrintFaultCalls == 1,
                    "Mount Workbench startup source failure ownership differs.");
            }
            else
                Require(invocation.Reads == 0 && invocation.FreeArgs == 0 &&
                    invocation.Allocations == 0 && invocation.FreeMem == 0,
                    "Mount Workbench startup must return before CLI parsing.");
            return;
        }

        Require(invocation.Reads == (allocationFailure ? 0 : 1),
            "Mount ReadArgs invocation count differs.");
        Require(invocation.FreeArgs == (parserFailure || allocationFailure ? 0 : 1),
            "Mount RDArgs ownership differs.");
        Require(invocation.Allocations == 1,
            "Mount result allocation count differs.");
        Require(invocation.FreeMem == (allocationFailure ? 0 : 1),
            "Mount result cleanup differs.");
    }

    private void RegisterMountEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Mount!;
            var template = suite == Workbench31MountEntrySuite
                ? NativeWorkbench31MountCommand.Template
                : NativeMorphOSMountCommand.Template;
            var resultCount = suite == Workbench31MountEntrySuite
                ? NativeWorkbench31MountCommand.ResultCount
                : NativeMorphOSMountCommand.ResultCount;
            Require(Bus.CString(state.D[1]) == template &&
                state.D[3] == 0,
                "Mount ReadArgs template/source ABI differs.");
            var results = state.D[2];
            Require((results & 3) == 0 &&
                Bus.OwnedAllocation(invocation, results, "Exec").Size ==
                    resultCount * 4u,
                "Mount result slots must be owned and longword aligned.");
            for (var offset = 0u;
                 offset < resultCount * 4u;
                 offset += 4)
                Require(Bus.Long(results + offset) == 0,
                    "Mount result defaults must be zeroed.");

            invocation.Reads++;
            if (invocation.Definition.ParserError != 0)
            {
                invocation.IoError = invocation.Definition.ParserError;
                return 0;
            }

            Require(definition.NoDevices,
                "The bounded Mount fixture must use an empty DEVICE/M vector.");
            return Bus.Allocate(invocation, 40, "RDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            invocation.IoError = 901;
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr",
            (_, invocation) => unchecked((uint)invocation.IoError));
        if (suite == Workbench31MountEntrySuite)
        {
            Register(baseAddress, DosLvo.CurrentDir, "CurrentDir",
                (state, invocation) =>
                {
                    var definition = invocation.Definition.Mount!;
                    Require(definition.StartupSourceMissing &&
                        state.D[1] is 0x710u or 0x720u,
                        "Mount Workbench startup CurrentDir lock differs.");
                    invocation.MountCurrentDirCalls++;
                    return state.D[1] == 0x710u ? 0x720u : 0x710u;
                });
            Register(baseAddress, DosLvo.FilePart, "FilePart",
                (state, invocation) =>
                {
                    var definition = invocation.Definition.Mount!;
                    Require(definition.StartupSourceMissing &&
                        Bus.CString(state.D[1]) == "MISSING:mount",
                        "Mount Workbench startup FilePart source differs.");
                    invocation.MountFilePartCalls++;
                    return state.D[1] + 8u;
                });
            Register(baseAddress, DosLvo.Open, "Open",
                (state, invocation) =>
                {
                    var definition = invocation.Definition.Mount!;
                    Require(definition.StartupSourceMissing &&
                        Bus.CString(state.D[1]) == "MISSING:mount" &&
                        state.D[2] == (uint)DOS.FileMode.OldFile,
                        "Mount Workbench startup source Open differs.");
                    invocation.MountOpenCalls++;
                    invocation.IoError = (int)DOS.Error.ObjectNotFound;
                    return 0;
                });
        }
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[2]) == "Mount",
                    "Mount fault name differs.");
                invocation.MountPrintFaultCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr",
            (state, invocation) =>
            {
                invocation.IoError = unchecked((int)state.D[1]);
                return 0;
            });
    }
}
