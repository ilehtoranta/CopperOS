using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record Check2090EntryCase(
    bool ConfigFound = true,
    bool ConfigFlagSet = false,
    bool ExpansionAvailable = true);

internal sealed partial class ProbeFixture
{
    public const string Check2090EntrySuite =
        "workbench31-check2090-native-entry-vector-fixture";

    private const uint Check2090ExpansionBase = 0xc000;

    private List<object> RunCheck2090EntryCases()
    {
        ProbeCase[] cases =
        [
            Check2090Case("controller-unflagged", new(),
                NativeWorkbench31Check2090Command.ResultControllerReady),
            Check2090Case("controller-flagged", new(ConfigFlagSet: true),
                NativeWorkbench31Check2090Command.ResultControllerNeedsCheck),
            Check2090Case("controller-absent", new(ConfigFound: false),
                NativeWorkbench31Check2090Command.ResultNoController),
            Check2090Case("missing-expansion", new(ExpansionAvailable: false),
                NativeWorkbench31Check2090Command.ResultOpenFailure),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Check2090 = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Check2090 = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Check2090 = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { Check2090 = new(), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            Check2090Case("interleaved-ready", new(),
                NativeWorkbench31Check2090Command.ResultControllerReady),
            Check2090Case("interleaved-absent", new(ConfigFound: false),
                NativeWorkbench31Check2090Command.ResultNoController)
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Check2090Case(string name,
        Check2090EntryCase definition, int result, int error = 0) =>
        new(name, "", result, error, "") { Check2090 = definition };

    private void PrepareCheck2090Entry(Invocation invocation)
    {
        var definition = invocation.Definition.Check2090 ?? new();
        Bus.Memory[invocation.ExpansionConfig + 0x10] =
            definition.ConfigFlagSet ? (byte)0x10 : (byte)0;
    }

    private void RegisterCheck2090EntryExec()
    {
        Register(Check2090ExpansionBase, -72, "FindConfigDev",
            (state, invocation) =>
            {
                var definition = invocation.Definition.Check2090!;
                Require(state.A[0] == 0 && state.D[0] == 0x202 &&
                    state.D[1] == 1 &&
                    state.A[6] == Check2090ExpansionBase,
                    "Check2090 FindConfigDev ABI differs.");
                invocation.Check2090FindConfigDevCalls++;
                return definition.ConfigFound ? invocation.ExpansionConfig : 0;
            });
    }

    private void VerifyCheck2090Entry(Invocation invocation)
    {
        var definition = invocation.Definition.Check2090!;
        var boundary = invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos;
        if (boundary)
        {
            Require(invocation.Check2090ExpansionOpens == 0 &&
                invocation.Check2090ExpansionCloses == 0 &&
                invocation.Check2090FindConfigDevCalls == 0,
                "Check2090 crossed an invalid startup boundary.");
            return;
        }

        Require(invocation.Check2090ExpansionOpens == 1,
            "Check2090 did not make one expansion.library lease.");
        if (!definition.ExpansionAvailable)
        {
            Require(invocation.Check2090ExpansionCloses == 0 &&
                invocation.Check2090FindConfigDevCalls == 0,
                "Check2090 used expansion.library after an open failure.");
            return;
        }

        Require(invocation.Check2090ExpansionCloses == 1 &&
            invocation.Check2090FindConfigDevCalls == 1,
            "Check2090 expansion lookup lifetime differs.");
    }
}
