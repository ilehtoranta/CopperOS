using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record ResListEntryCase(int Entries = 0,
    bool AllocationFailure = false, bool CtrlC = false);

internal sealed record ResListNativeLayout(uint List, uint Sentinel,
    uint Node, uint Name)
{
    public uint Control => List;
}

internal sealed partial class ProbeFixture
{
    public const string ResListEntrySuite =
        "morphos320-reslist-native-entry-vector-fixture";

    private const string ResListHeader =
        "address\t\tname\n" +
        "------------------------------------------------------------\n";

    private List<object> RunResListEntryCases()
    {
        ProbeCase[] cases =
        [
            ResCase("empty-list"),
            ResCase("one-resource", new(1)),
            ResCase("three-resources", new(3)),
            ResCase("ctrl-c", new(3, CtrlC: true),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.Break),
            ResCase("allocation-failure", new(AllocationFailure: true),
                result: DOS.RETURN_FAIL,
                error: 0,
                output: "Not Enough memory for resource buffer\n"),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { ResList = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { ResList = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { ResList = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { ResList = new(), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            ResCase("interleaved-left", new(1),
                output: ResOutput(1)),
            ResCase("interleaved-right", new(1),
                output: ResOutput(1))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase ResCase(string name,
        ResListEntryCase? definition = null, int result = DOS.RETURN_OK,
        int error = 0, string output = "")
    {
        definition ??= new();
        if (output.Length == 0)
            output = definition.AllocationFailure
                ? "Not Enough memory for resource buffer\n"
                : definition.Entries == 0 ? ResListHeader
                : ResOutput(definition.CtrlC ? 1 : definition.Entries);
        return new(name, "", result, error, output)
        {
            ResList = definition
        };
    }

    private static string ResOutput(int entries)
    {
        var output = new StringBuilder(ResListHeader);
        for (var slot = 0; slot < entries; slot++)
        {
            var name = slot == 0 ? "Res" : "Res" + slot;
            output.Append(string.Format("0x{0:x8}\t{1}\n",
                0x6000u + 32u + (uint)slot * 0x80u, name));
        }
        return output.ToString();
    }

    private void PrepareResListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.ResList ?? new();
        Require(definition.Entries is >= 0 and <= 3,
            "ResList fixture only supports bounded empty-to-three-node lists.");
        var list = 0x6000u;
        var sentinel = list + (uint)ExecLayout.List.Tail;
        var firstNode = list + 32;
        var firstName = list + 512;
        var layout = new ResListNativeLayout(list, sentinel, firstNode, firstName);
        invocation.ResListLayout = layout;

        var execList = 0x4000u + (uint)ExecLayout.ExecBase.ResourceList;
        Bus.Long(execList, definition.Entries == 0 ? sentinel : firstNode);
        Bus.Long(list + (uint)ExecLayout.List.Tail, 0);
        Bus.Long(list + (uint)ExecLayout.List.TailPred,
            definition.Entries == 0 ? list :
            firstNode + (uint)(definition.Entries - 1) * 0x80u);
        Bus.Memory[list + (uint)ExecLayout.List.Type] = 0;
        Bus.Long(sentinel + (uint)ExecLayout.Node.Successor, 0);
        for (var slot = 0; slot < definition.Entries; slot++)
        {
            var node = firstNode + (uint)slot * 0x80u;
            var name = firstName + (uint)slot * 32u;
            var successor = slot + 1 == definition.Entries
                ? sentinel : node + 0x80u;
            var predecessor = slot == 0 ? list : node - 0x80u;
            Bus.Long(node + (uint)ExecLayout.Node.Successor, successor);
            Bus.Long(node + (uint)ExecLayout.Node.Predecessor, predecessor);
            Bus.Long(node + (uint)ExecLayout.Node.Name, name);
            var bytes = Encoding.Latin1.GetBytes(
                $"{(slot == 0 ? "Res" : "Res" + slot)}\0");
            bytes.CopyTo(Bus.Memory.AsSpan((int)name));
        }
    }

    private void RegisterResListEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "ResList FPuts stream differs.");
            var text = Bus.CString(state.D[2]);
            Require(text == ResListHeader ||
                text == "Not Enough memory for resource buffer\n",
                "ResList FPuts text differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.ResListFPutsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == "0x%08.lx\t%s\n",
                "ResList VPrintf format differs.");
            var args = state.D[2];
            var address = Bus.Long(args);
            var name = Bus.CString(Bus.Long(args + 4));
            var slot = checked((int)((address - 0x6000u - 32u) / 0x80u));
            var expectedName = slot == 0 ? "Res" : "Res" + slot;
            Require(address == 0x6000u + 32u + (uint)slot * 0x80u &&
                name == expectedName, "ResList resource name differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                $"0x{address:x8}\t{name}\n"));
            invocation.ResListVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (_, invocation) => { invocation.ResListPrintFaultCalls++; return 0; });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyResListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.ResList!;
        if (invocation.Definition.Workbench)
        {
            Require(invocation.ResListAllocVecCalls == 0 &&
                invocation.ResListForbidCalls == 1,
                $"{invocation.Definition.Name}: ResList Workbench startup crossed into the body.");
            return;
        }
        if (invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.ResListAllocVecCalls == 0 &&
                invocation.ResListForbidCalls == 0,
                $"{invocation.Definition.Name}: ResList crossed an invalid startup boundary.");
            return;
        }
        if (invocation.Definition.MissingDos)
        {
            Require(invocation.ResListAllocVecCalls == 0,
                "ResList used Exec after DOS open failure.");
            return;
        }
        Require(invocation.ResListAllocVecCalls == 1,
            "ResList allocation count differs.");
        if (definition.AllocationFailure)
        {
            Require(invocation.ResListFreeVecCalls == 0 &&
                invocation.ResListForbidCalls == 0 &&
                invocation.ResListFPutsCalls == 1,
                "ResList allocation failure cleanup differs.");
            return;
        }
        var rows = definition.CtrlC ? 1 : definition.Entries;
        Require(invocation.ResListFreeVecCalls == 1 &&
            invocation.ResListForbidCalls == 1 &&
            invocation.ResListPermitCalls == 1 &&
            invocation.ResListFPutsCalls == 1 &&
            invocation.ResListVPrintfCalls == rows &&
            invocation.ResListSetSignalCalls == rows &&
            invocation.ResListPrintFaultCalls == (definition.CtrlC ? 1 : 0),
            "ResList list/output lifecycle differs.");
    }
}
