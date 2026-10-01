using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record PortListEntryCase(int Entries = 0,
    bool AllocationFailure = false, bool CtrlC = false);

internal sealed record PortListNativeLayout(uint List, uint Sentinel,
    uint Node, uint Name, uint Task, uint TaskName)
{
    public uint Control => List;
}

internal sealed partial class ProbeFixture
{
    public const string PortListEntrySuite =
        "morphos320-portlist-native-entry-vector-fixture";

    private const string PortListHeader =
        "   Address\t                          Name\tSignal\t      Task\t                      TaskName\n";
    private const string PortListSeparator =
        "------------------------------------------------------------------------------------------------------\n";

    private List<object> RunPortListEntryCases()
    {
        ProbeCase[] cases =
        [
            PortCase("empty-list"),
            PortCase("one-port", new(1)),
            PortCase("three-ports", new(3)),
            PortCase("ctrl-c", new(3, CtrlC: true),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.Break),
            PortCase("allocation-failure", new(AllocationFailure: true),
                result: DOS.RETURN_FAIL, error: Invocation.InitialIoError),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { PortList = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { PortList = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { PortList = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { PortList = new(), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            PortCase("interleaved-left", new(1), output: PortOutput(1)),
            PortCase("interleaved-right", new(1), output: PortOutput(1))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase PortCase(string name,
        PortListEntryCase? definition = null, int result = DOS.RETURN_OK,
        int error = 0, string output = "")
    {
        definition ??= new();
        if (output.Length == 0)
            output = definition.AllocationFailure ? "" :
                definition.Entries == 0 ? PortListHeader + PortListSeparator :
                PortOutput(definition.CtrlC ? 1 : definition.Entries);
        return new(name, "", result, error, output)
        {
            PortList = definition
        };
    }

    private static string PortOutput(int entries)
    {
        var output = new StringBuilder(PortListHeader + PortListSeparator);
        for (var slot = 0; slot < entries; slot++)
        {
            var name = PortName(slot);
            var taskName = TaskName(slot);
            var address = 0x6020u + (uint)slot * 0x100u;
            var task = 0x7200u + (uint)slot * 0x100u;
            output.Append(
                $"0x{address:x8}\t{name.PadLeft(30)}\t{5 + slot,2}\t0x{task:x}\t{taskName.PadLeft(30)}\n");
        }
        return output.ToString();
    }

    private static string PortName(int slot) => slot == 0 ? "Port" : "Port" + slot;

    private static string TaskName(int slot) => slot == 0 ? "Task" : "Task" + slot;

    private void PreparePortListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.PortList ?? new();
        Require(definition.Entries is >= 0 and <= 3,
            "PortList fixture only supports bounded empty-to-three-port lists.");
        var list = 0x6000u;
        var sentinel = list + (uint)ExecLayout.List.Tail;
        var firstNode = list + 32;
        var firstName = 0x7000u;
        var firstTask = 0x7200u;
        var firstTaskName = 0x7400u;
        invocation.PortListLayout = new PortListNativeLayout(
            list, sentinel, firstNode, firstName, firstTask, firstTaskName);

        var execList = 0x4000u + (uint)ExecLayout.ExecBase.PortList;
        Bus.Long(execList, definition.Entries == 0 ? sentinel : firstNode);
        Bus.Long(list + (uint)ExecLayout.List.Tail, 0);
        Bus.Long(list + (uint)ExecLayout.List.TailPred,
            definition.Entries == 0 ? list :
            firstNode + (uint)(definition.Entries - 1) * 0x100u);
        Bus.Memory[list + (uint)ExecLayout.List.Type] = 0;
        Bus.Long(sentinel + (uint)ExecLayout.Node.Successor, 0);
        for (var slot = 0; slot < definition.Entries; slot++)
        {
            var node = firstNode + (uint)slot * 0x100u;
            var name = firstName + (uint)slot * 0x40u;
            var task = firstTask + (uint)slot * 0x100u;
            var taskName = firstTaskName + (uint)slot * 0x40u;
            var successor = slot + 1 == definition.Entries
                ? sentinel : node + 0x100u;
            var predecessor = slot == 0 ? list : node - 0x100u;
            Bus.Long(node + (uint)ExecLayout.Node.Successor, successor);
            Bus.Long(node + (uint)ExecLayout.Node.Predecessor, predecessor);
            Bus.Long(node + (uint)ExecLayout.Node.Name, name);
            Bus.Memory[node + (uint)ExecLayout.MsgPort.SignalBit] =
                (byte)(5 + slot);
            Bus.Long(node + (uint)ExecLayout.MsgPort.SignalTask, task);
            Bus.Long(task + (uint)ExecLayout.Node.Name, taskName);
            Encoding.Latin1.GetBytes($"{PortName(slot)}\0").CopyTo(
                Bus.Memory.AsSpan((int)name));
            Encoding.Latin1.GetBytes($"{TaskName(slot)}\0").CopyTo(
                Bus.Memory.AsSpan((int)taskName));
        }
    }

    private void RegisterPortListEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "PortList FPuts stream differs.");
            var text = Bus.CString(state.D[2]);
            Require(text == PortListHeader || text == PortListSeparator,
                "PortList FPuts text differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.PortListFPutsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) ==
                "0x%08.lx\t%30s\t%2ld\t0x%lx\t%30s\n",
                "PortList VPrintf format differs.");
            var args = state.D[2];
            var address = Bus.Long(args);
            var name = Bus.CString(Bus.Long(args + 4));
            var signal = Bus.Long(args + 8);
            var task = Bus.Long(args + 12);
            var taskName = Bus.CString(Bus.Long(args + 16));
            var slot = checked((int)((address - 0x6020u) / 0x100u));
            Require(address == 0x6020u + (uint)slot * 0x100u &&
                name == PortName(slot) && signal == (uint)(5 + slot) &&
                task == 0x7200u + (uint)slot * 0x100u &&
                taskName == TaskName(slot),
                $"PortList message-port fields differ (slot={slot}, name={name}, signal={signal}, task=0x{task:x}, taskName={taskName}).");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                $"0x{address:x8}\t{name.PadLeft(30)}\t{signal,2}\t0x{task:x}\t{taskName.PadLeft(30)}\n"));
            invocation.PortListVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (_, invocation) => { invocation.PortListPrintFaultCalls++; return 0; });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyPortListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.PortList!;
        if (invocation.Definition.Workbench)
        {
            Require(invocation.PortListAllocVecCalls == 0 &&
                invocation.PortListForbidCalls == 1,
                $"{invocation.Definition.Name}: PortList Workbench startup crossed into the body.");
            return;
        }
        if (invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.PortListAllocVecCalls == 0 &&
                invocation.PortListForbidCalls == 0,
                $"{invocation.Definition.Name}: PortList crossed an invalid startup boundary.");
            return;
        }
        if (invocation.Definition.MissingDos)
        {
            Require(invocation.PortListAllocVecCalls == 0,
                "PortList used Exec after DOS open failure.");
            return;
        }
        Require(invocation.PortListAllocVecCalls == 1,
            "PortList allocation count differs.");
        if (definition.AllocationFailure)
        {
            Require(invocation.PortListFreeVecCalls == 0 &&
                invocation.PortListForbidCalls == 0 &&
                invocation.PortListFPutsCalls == 0 &&
                invocation.PortListPrintFaultCalls == 1,
                "PortList allocation failure cleanup differs.");
            return;
        }
        Require(invocation.PortListFreeVecCalls == 1 &&
            invocation.PortListForbidCalls == 1 &&
            invocation.PortListPermitCalls == 1 &&
            invocation.PortListFPutsCalls == 2 &&
            invocation.PortListVPrintfCalls == (definition.CtrlC ? 1 : definition.Entries) &&
            invocation.PortListSetSignalCalls == (definition.CtrlC ? 1 : definition.Entries) &&
            invocation.PortListPrintFaultCalls == (definition.CtrlC ? 1 : 0),
            $"PortList list/output lifecycle differs ({invocation.PortListFreeVecCalls},{invocation.PortListForbidCalls},{invocation.PortListPermitCalls},{invocation.PortListFPutsCalls},{invocation.PortListVPrintfCalls},{invocation.PortListSetSignalCalls},{invocation.PortListPrintFaultCalls}; entries={definition.Entries}, ctrl={definition.CtrlC}).");
    }
}
