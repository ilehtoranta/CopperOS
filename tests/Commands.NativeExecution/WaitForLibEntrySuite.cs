using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record WaitForLibEntryCase(
    string LibraryName,
    int Interval = 1,
    int Loop = 11,
    bool LibraryFound = true,
    bool CtrlC = false,
    int ParserError = 0);

internal sealed partial class ProbeFixture
{
    public const string WaitForLibEntrySuite =
        "morphos320-waitforlib-native-entry-vector-fixture";

    private const string WaitForLibTemplate =
        "LIBNAME/A,I=INTERVAL/K/N,L=LOOP/K/N";
    private const uint WaitForLibControlC = 1u << 12;

    private List<object> RunWaitForLibEntryCases()
    {
        ProbeCase[] cases =
        [
            new("library-present", "DOS", DOS.RETURN_OK, 0, "")
            { WaitForLib = new("DOS") },
            new("missing-after-three-checks", "MISSING I=2 L=3",
                DOS.RETURN_FAIL, 0, "")
            { WaitForLib = new("MISSING", 2, 3, LibraryFound: false) },
            new("ctrl-c", "MISSING I=1 L=0", DOS.RETURN_WARN,
                (int)DOS.Error.Break, "")
            { WaitForLib = new("MISSING", CtrlC: true, LibraryFound: false) },
            new("negative-interval", "MISSING I=-1",
                DOS.RETURN_FAIL, (int)DOS.Error.BadNumber, "")
            { WaitForLib = new("MISSING", -1, LibraryFound: false) },
            new("tick-overflow", "MISSING I=2147483647 L=2",
                DOS.RETURN_FAIL, (int)DOS.Error.ObjectTooLarge, "")
            { WaitForLib = new("MISSING", int.MaxValue, 2,
                LibraryFound: false) },
            new("parser-failure", "MISSING", DOS.RETURN_ERROR, 116, "")
            { WaitForLib = new("MISSING", ParserError: 116) },
            new("workbench-startup", "DOS", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
                { WaitForLib = new("DOS"), Workbench = true },
            new("missing-default-loop", "MISSING", DOS.RETURN_FAIL, 0, "")
                { WaitForLib = new("MISSING", LibraryFound: false) },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "present-left" },
            cases[1] with { Name = "missing-right" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void RegisterWaitForLibEntryExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal",
            (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "WaitForLib signal query ABI differs.");
                invocation.WaitForLibSignalCalls++;
                return invocation.Definition.WaitForLib!.CtrlC
                    ? WaitForLibControlC : 0;
            });
    }

    private void RegisterWaitForLibEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs",
            (state, invocation) =>
            {
                var definition = invocation.Definition.WaitForLib!;
                Require(Bus.CString(state.D[1]) == WaitForLibTemplate &&
                    state.D[3] == 0 &&
                    Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 12,
                    "WaitForLib ReadArgs ABI differs.");
                for (var offset = 0u; offset < 12; offset += 4)
                    Require(Bus.Long(state.D[2] + offset) == 0,
                        "WaitForLib result slots were not cleared.");
                invocation.Reads++;
                if (definition.ParserError != 0)
                {
                    invocation.IoError = definition.ParserError;
                    return 0;
                }

                var rdArgs = Bus.Allocate(invocation, 40, "RDArgs", true);
                var name = invocation.Arguments + 0x400;
                WriteWaitForLibString(name, definition.LibraryName);
                Bus.Long(state.D[2], name);
                if (definition.Interval != 1)
                {
                    var interval = invocation.Arguments + 0x500;
                    Bus.Long(interval, unchecked((uint)definition.Interval));
                    Bus.Long(state.D[2] + 4, interval);
                }
                if (definition.Loop != 11)
                {
                    var loop = invocation.Arguments + 0x504;
                    Bus.Long(loop, unchecked((uint)definition.Loop));
                    Bus.Long(state.D[2] + 8, loop);
                }
                invocation.WaitForLibReadArgs = state.D[2];
                return rdArgs;
            });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs",
            (state, invocation) =>
            {
                Bus.Release(invocation, state.D[1], "RDArgs");
                invocation.FreeArgs++;
                return 0;
            });
        Register(baseAddress, DosLvo.Delay, "Delay",
            (state, invocation) =>
            {
                var definition = invocation.Definition.WaitForLib!;
                Require(state.D[1] == unchecked((uint)(definition.Interval * 50)),
                    "WaitForLib Delay tick conversion differs.");
                invocation.WaitForLibDelayCalls++;
                invocation.WaitForLibDelayTicks = unchecked((int)state.D[1]);
                return 0;
            });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[2]) == "WaitForLib",
                    "WaitForLib fault header differs.");
                invocation.WaitForLibPrintFaultCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.IoErr, "IoErr",
            (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr",
            (state, invocation) =>
            {
                invocation.IoError = unchecked((int)state.D[1]);
                return 0;
            });
    }

    private void VerifyWaitForLibEntry(Invocation invocation)
    {
        var definition = invocation.Definition.WaitForLib!;
        if (definition.ParserError != 0 || invocation.Definition.Workbench)
        {
            Require(invocation.Reads == 0 || definition.ParserError != 0,
                "WaitForLib parsed an invalid startup boundary.");
            Require(invocation.WaitForLibFindCalls == 0,
                "WaitForLib looked up a library after parser/startup failure.");
            return;
        }

        Require(invocation.Reads == 1 && invocation.FreeArgs == 1,
            "WaitForLib parser lifetime differs.");
        if (definition.Interval < 0)
        {
            Require(invocation.WaitForLibFindCalls == 0,
                "WaitForLib queried a library after rejecting a negative interval.");
            return;
        }
        Require(invocation.WaitForLibFindCalls > 0,
            $"WaitForLib did not query the requested library for {invocation.Definition.Name}.");
        if (definition.CtrlC)
            Require(invocation.WaitForLibSignalCalls == 1 &&
                invocation.WaitForLibDelayCalls == 0,
                "WaitForLib Ctrl-C teardown differs.");
        else if (!definition.LibraryFound)
            Require(invocation.WaitForLibSignalCalls >= 1,
                "WaitForLib did not poll Ctrl-C before retry.");
    }

    private void WriteWaitForLibString(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }
}
