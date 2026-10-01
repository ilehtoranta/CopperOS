using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record WaitForPortEntryCase(
    string PortName,
    int Interval = 1,
    int Loop = 10,
    bool Disappear = false,
    bool PortFound = true,
    int? DisappearAfter = null,
    bool CtrlC = false,
    int ParserError = 0);

internal sealed partial class ProbeFixture
{
    public const string WaitForPortEntrySuite =
        "morphos320-waitforport-native-entry-vector-fixture";

    private const string WaitForPortTemplate =
        "PORTNAME/A,I=INTERVAL/K/N,L=LOOP/K/N,D=DISAPPEAR/S";
    private const uint WaitForPortControlC = 1u << 12;

    private List<object> RunWaitForPortEntryCases()
    {
        ProbeCase[] cases =
        [
            new("port-present", "SYSLOG", DOS.RETURN_OK, 0, "")
            { WaitForPort = new("SYSLOG") },
            new("missing-after-three-checks", "MISSING I=2 L=3",
                DOS.RETURN_FAIL, 0, "")
            { WaitForPort = new("MISSING", 2, 3, PortFound: false) },
            new("disappear-after-one-check", "SERVER I=1 L=4 D",
                DOS.RETURN_OK, 0, "")
            { WaitForPort = new("SERVER", 1, 4, Disappear: true,
                DisappearAfter: 1) },
            new("ctrl-c", "MISSING I=1 L=0", DOS.RETURN_WARN,
                (int)DOS.Error.Break, "")
            { WaitForPort = new("MISSING", CtrlC: true, PortFound: false) },
            new("negative-interval", "MISSING I=-1",
                DOS.RETURN_FAIL, (int)DOS.Error.BadNumber, "")
            { WaitForPort = new("MISSING", -1, PortFound: false) },
            new("tick-overflow", "MISSING I=2147483647 L=2",
                DOS.RETURN_FAIL, (int)DOS.Error.ObjectTooLarge, "")
            { WaitForPort = new("MISSING", int.MaxValue, 2,
                PortFound: false) },
            new("parser-failure", "MISSING", DOS.RETURN_ERROR, 116, "")
            { WaitForPort = new("MISSING", ParserError: 116) },
            new("workbench-startup", "SYSLOG", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { WaitForPort = new("SYSLOG"), Workbench = true },
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

    private void RegisterWaitForPortEntryExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal",
            (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.D[1] == 0,
                    "WaitForPort signal query ABI differs.");
                invocation.WaitForPortSignalCalls++;
                return invocation.Definition.WaitForPort!.CtrlC
                    ? WaitForPortControlC : 0;
            });
    }

    private void RegisterWaitForPortEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs",
            (state, invocation) =>
            {
                var definition = invocation.Definition.WaitForPort!;
                Require(Bus.CString(state.D[1]) == WaitForPortTemplate &&
                    state.D[3] == 0 &&
                    Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 16,
                    "WaitForPort ReadArgs ABI differs.");
                for (var offset = 0u; offset < 16; offset += 4)
                    Require(Bus.Long(state.D[2] + offset) == 0,
                        "WaitForPort result slots were not cleared.");
                invocation.Reads++;
                if (definition.ParserError != 0)
                {
                    invocation.IoError = definition.ParserError;
                    return 0;
                }

                var rdArgs = Bus.Allocate(invocation, 40, "RDArgs", true);
                var name = invocation.Arguments + 0x400;
                WriteWaitForPortString(name, definition.PortName);
                Bus.Long(state.D[2], name);
                if (definition.Interval != 1)
                {
                    var interval = invocation.Arguments + 0x500;
                    Bus.Long(interval, unchecked((uint)definition.Interval));
                    Bus.Long(state.D[2] + 4, interval);
                }
                if (definition.Loop != 10)
                {
                    var loop = invocation.Arguments + 0x504;
                    Bus.Long(loop, unchecked((uint)definition.Loop));
                    Bus.Long(state.D[2] + 8, loop);
                }
                if (definition.Disappear) Bus.Long(state.D[2] + 12, uint.MaxValue);
                invocation.WaitForPortReadArgs = state.D[2];
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
                var definition = invocation.Definition.WaitForPort!;
                Require(state.D[1] == unchecked((uint)(definition.Interval * 50)),
                    "WaitForPort Delay tick conversion differs.");
                invocation.WaitForPortDelayCalls++;
                invocation.WaitForPortDelayTicks = unchecked((int)state.D[1]);
                return 0;
            });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[2]) == "WaitForPort",
                    "WaitForPort fault header differs.");
                invocation.WaitForPortPrintFaultCalls++;
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

    private void VerifyWaitForPortEntry(Invocation invocation)
    {
        var definition = invocation.Definition.WaitForPort!;
        if (definition.ParserError != 0 || invocation.Definition.Workbench)
        {
            Require(invocation.Reads == 0 || definition.ParserError != 0,
                "WaitForPort parsed an invalid startup boundary.");
            Require(invocation.WaitForPortFindCalls == 0,
                "WaitForPort looked up a port after parser/startup failure.");
            return;
        }

        Require(invocation.Reads == 1 && invocation.FreeArgs == 1,
            "WaitForPort parser lifetime differs.");
        if (definition.Interval < 0)
        {
            Require(invocation.WaitForPortFindCalls == 0,
                "WaitForPort queried a port after rejecting a negative interval.");
            return;
        }
        Require(invocation.WaitForPortFindCalls > 0,
            $"WaitForPort did not query the requested port for {invocation.Definition.Name} (reads={invocation.Reads}, frees={invocation.FreeArgs}, result={invocation.Definition.Result}).");
        if (definition.CtrlC)
            Require(invocation.WaitForPortSignalCalls == 1 &&
                invocation.WaitForPortDelayCalls == 0,
                $"WaitForPort Ctrl-C teardown differs for {invocation.Definition.Name}: signals={invocation.WaitForPortSignalCalls}, delays={invocation.WaitForPortDelayCalls}, finds={invocation.WaitForPortFindCalls}.");
        else if (!(definition.PortFound == !definition.Disappear &&
                   (!definition.DisappearAfter.HasValue ||
                    definition.DisappearAfter.Value > 0)))
            Require(invocation.WaitForPortSignalCalls >= 1,
                "WaitForPort did not poll Ctrl-C.");
    }

    private void WriteWaitForPortString(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }
}
