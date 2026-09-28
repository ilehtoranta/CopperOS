using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record DevListEntryCase(int Entries = 0,
    bool AllocationFailure = false, bool CtrlC = false);

internal sealed record DevListNativeLayout(uint List, uint Sentinel,
    uint Node, uint Name)
{
    public uint Control => List;
}

internal sealed partial class ProbeFixture
{
    public const string DevListEntrySuite =
        "morphos320-devlist-native-entry-vector-fixture";

    private const string DevListHeader =
        "   Address  Version  Rev  OpenCnt  Flags  Name\n";

    private List<object> RunDevListEntryCases()
    {
        ProbeCase[] cases =
        [
            DevCase("empty-list"),
            DevCase("one-device", new(1)),
            DevCase("three-devices", new(3)),
            DevCase("ctrl-c", new(3, CtrlC: true),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.Break),
            DevCase("allocation-failure", new(AllocationFailure: true),
                result: DOS.RETURN_FAIL,
                error: 0,
                output: "Not Enough memory for device buffer\n"),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { DevList = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { DevList = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { DevList = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { DevList = new(), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            DevCase("interleaved-left", new(1),
                output: DevOutput(1)),
            DevCase("interleaved-right", new(1),
                output: DevOutput(1))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase DevCase(string name,
        DevListEntryCase? definition = null, int result = DOS.RETURN_OK,
        int error = 0, string output = "")
    {
        definition ??= new();
        if (output.Length == 0)
            output = definition.AllocationFailure
                ? "Not Enough memory for device buffer\n"
                : definition.Entries == 0 ? DevListHeader
                : DevOutput(definition.CtrlC ? 1 : definition.Entries);
        return new(name, "", result, error, output)
        {
            DevList = definition
        };
    }

    private static string DevOutput(int entries)
    {
        var output = new StringBuilder(DevListHeader);
        for (var slot = 0; slot < entries; slot++)
        {
            var name = slot == 0 ? "Dev" : "Dev" + slot;
            output.Append(string.Format(
                "0x{0:x8}  {1,7} {2,4}  {3,7}   0x{4,2:x2}  {5}\n",
                0x6000u + 32u + (uint)slot * 0x80u,
                50 + slot, 3 + slot, 2 + slot, 0x12 + slot, name));
        }
        return output.ToString();
    }

    private void PrepareDevListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.DevList ?? new();
        Require(definition.Entries is >= 0 and <= 3,
            "DevList fixture only supports bounded empty-to-three-node lists.");
        var list = 0x6000u;
        var sentinel = list + (uint)ExecLayout.List.Tail;
        var firstNode = list + 32;
        var firstName = list + 512;
        var layout = new DevListNativeLayout(list, sentinel, firstNode, firstName);
        invocation.DevListLayout = layout;

        var execList = 0x4000u + (uint)ExecLayout.ExecBase.DeviceList;
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
            Bus.Word(node + (uint)ExecLayout.Library.Version,
                (ushort)(50 + slot));
            Bus.Word(node + (uint)ExecLayout.Library.Revision,
                (ushort)(3 + slot));
            Bus.Word(node + (uint)ExecLayout.Library.OpenCount,
                (ushort)(2 + slot));
            Bus.Memory[node + (uint)ExecLayout.Library.Flags] =
                (byte)(0x12 + slot);
            var bytes = Encoding.Latin1.GetBytes(
                $"{(slot == 0 ? "Dev" : "Dev" + slot)}\0");
            bytes.CopyTo(Bus.Memory.AsSpan((int)name));
        }
    }

    private void RegisterDevListEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "DevList FPuts stream differs.");
            var text = Bus.CString(state.D[2]);
            Require(text == DevListHeader ||
                text == "Not Enough memory for device buffer\n",
                "DevList FPuts text differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.DevListFPutsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) ==
                "0x%08.ix  %7ld %4ld  %7ld   0x%02lx  %s\n",
                "DevList VPrintf format differs.");
            var args = state.D[2];
            var address = Bus.Long(args);
            var name = Bus.CString(Bus.Long(args + 4));
            var version = Bus.Long(args + 8);
            var revision = Bus.Long(args + 12);
            var openCount = Bus.Long(args + 16);
            var flags = Bus.Long(args + 20);
            var slot = checked((int)((address - 0x6000u - 32u) / 0x80u));
            var expectedName = slot == 0 ? "Dev" : "Dev" + slot;
            Require(address == 0x6000u + 32u + (uint)slot * 0x80u &&
                name == expectedName && version == (uint)(50 + slot) &&
                revision == (uint)(3 + slot) &&
                openCount == (uint)(2 + slot) && flags == (uint)(0x12 + slot),
                "DevList device fields differ.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                $"0x{address:x8}  {version,7} {revision,4}  {openCount,7}   0x{flags,2:x2}  {name}\n"));
            invocation.DevListVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (_, invocation) => { invocation.DevListPrintFaultCalls++; return 0; });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyDevListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.DevList!;
        if (invocation.Definition.Workbench)
        {
            Require(invocation.DevListAllocVecCalls == 0 &&
                invocation.DevListForbidCalls == 1,
                $"{invocation.Definition.Name}: DevList Workbench startup crossed into the body.");
            return;
        }
        if (invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.DevListAllocVecCalls == 0 &&
                invocation.DevListForbidCalls == 0,
                $"{invocation.Definition.Name}: DevList crossed an invalid startup boundary.");
            return;
        }
        if (invocation.Definition.MissingDos)
        {
            Require(invocation.DevListAllocVecCalls == 0,
                "DevList used Exec after DOS open failure.");
            return;
        }
        Require(invocation.DevListAllocVecCalls == 1,
            "DevList allocation count differs.");
        if (definition.AllocationFailure)
        {
            Require(invocation.DevListFreeVecCalls == 0 &&
                invocation.DevListForbidCalls == 0 &&
                invocation.DevListFPutsCalls == 1,
                "DevList allocation failure cleanup differs.");
            return;
        }
        var rows = definition.CtrlC ? 1 : definition.Entries;
        Require(invocation.DevListFreeVecCalls == 1 &&
            invocation.DevListForbidCalls == 1 &&
            invocation.DevListPermitCalls == 1 &&
            invocation.DevListFPutsCalls == 1 &&
            invocation.DevListVPrintfCalls == rows &&
            invocation.DevListSetSignalCalls == rows &&
            invocation.DevListPrintFaultCalls == (definition.CtrlC ? 1 : 0),
            "DevList list/output lifecycle differs.");
    }
}
