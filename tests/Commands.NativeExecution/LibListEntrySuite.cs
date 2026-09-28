using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record LibListEntryCase(int Entries = 0,
    bool AllocationFailure = false, bool CtrlC = false);

internal sealed record LibListNativeLayout(uint List, uint Sentinel,
    uint Node, uint Name)
{
    public uint Control => List;
}

internal sealed partial class ProbeFixture
{
    public const string LibListEntrySuite =
        "morphos320-liblist-native-entry-vector-fixture";

    private const string LibListHeader =
        "   Address  Version  Rev  OpenCnt  Flags  Name\n";

    private List<object> RunLibListEntryCases()
    {
        ProbeCase[] cases =
        [
            LibCase("empty-list"),
            LibCase("one-library", new(1)),
            LibCase("three-libraries", new(3)),
            LibCase("ctrl-c", new(3, CtrlC: true),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.Break),
            LibCase("allocation-failure", new(AllocationFailure: true),
                result: DOS.RETURN_FAIL,
                error: 0,
                output: "Not Enough memory for library buffer\n"),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { LibList = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { LibList = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { LibList = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { LibList = new(), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            LibCase("interleaved-left", new(1),
                output: LibOutput(1)),
            LibCase("interleaved-right", new(1),
                output: LibOutput(1))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase LibCase(string name,
        LibListEntryCase? definition = null, int result = DOS.RETURN_OK,
        int error = 0, string output = "")
    {
        definition ??= new();
        if (output.Length == 0)
            output = definition.AllocationFailure
                ? "Not Enough memory for library buffer\n"
                : definition.Entries == 0 ? LibListHeader
                : LibOutput(definition.CtrlC ? 1 : definition.Entries);
        return new(name, "", result, error, output)
        {
            LibList = definition
        };
    }

    private static string LibOutput(int entries)
    {
        var output = new StringBuilder(LibListHeader);
        for (var slot = 0; slot < entries; slot++)
        {
            var name = LibName(slot);
            var version = 50 + slot;
            var revision = 6 + slot;
            var openCount = 2 + slot;
            var flags = 0x12 + slot;
            output.Append(string.Format(
                "0x{0:x8}  {1,7} {2,4}  {3,7}   0x{4,2:x2}  {5}\n",
                0x6000u + 32u + (uint)slot * 0x80u, version,
                revision, openCount, flags, name));
        }
        return output.ToString();
    }

    private static string LibName(int slot) => slot == 0 ? "Lib" : "Lib" + slot;

    private void PrepareLibListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.LibList ?? new();
        Require(definition.Entries is >= 0 and <= 3,
            "LibList fixture only supports bounded empty-to-three-node lists.");
        var list = 0x6000u;
        var sentinel = list + (uint)ExecLayout.List.Tail;
        var firstNode = list + 32;
        var firstName = list + 512;
        var layout = new LibListNativeLayout(list, sentinel, firstNode, firstName);
        invocation.LibListLayout = layout;

        var execList = 0x4000u + (uint)ExecLayout.ExecBase.LibraryList;
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
                (ushort)(6 + slot));
            Bus.Word(node + (uint)ExecLayout.Library.OpenCount,
                (ushort)(2 + slot));
            Bus.Memory[node + (uint)ExecLayout.Library.Flags] =
                (byte)(0x12 + slot);
            var bytes = Encoding.Latin1.GetBytes($"{LibName(slot)}\0");
            bytes.CopyTo(Bus.Memory.AsSpan((int)name));
        }
    }

    private void RegisterLibListEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "LibList FPuts stream differs.");
            var text = Bus.CString(state.D[2]);
            Require(text == LibListHeader ||
                text == "Not Enough memory for library buffer\n",
                "LibList FPuts text differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.LibListFPutsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) ==
                "0x%08.ix  %7ld %4ld  %7ld   0x%02lx  %s\n",
                "LibList VPrintf format differs.");
            var args = state.D[2];
            var address = Bus.Long(args);
            var name = Bus.CString(Bus.Long(args + 4));
            var version = Bus.Long(args + 8);
            var revision = Bus.Long(args + 12);
            var openCount = Bus.Long(args + 16);
            var flags = Bus.Long(args + 20);
            var slot = checked((int)((address - 0x6000u - 32u) / 0x80u));
            Require(address == 0x6000u + 32u + (uint)slot * 0x80u &&
                name == LibName(slot) && version == (uint)(50 + slot) &&
                revision == (uint)(6 + slot) &&
                openCount == (uint)(2 + slot) && flags == (uint)(0x12 + slot),
                "LibList library fields differ.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                $"0x{address:x8}  {version,7} {revision,4}  {openCount,7}   0x{flags,2:x2}  {name}\n"));
            invocation.LibListVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (_, invocation) => { invocation.LibListPrintFaultCalls++; return 0; });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyLibListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.LibList!;
        if (invocation.Definition.Workbench)
        {
            Require(invocation.LibListAllocVecCalls == 0 &&
                invocation.LibListForbidCalls == 1,
                $"{invocation.Definition.Name}: LibList Workbench startup crossed into the body.");
            return;
        }
        if (invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.LibListAllocVecCalls == 0 &&
                invocation.LibListForbidCalls == 0,
                $"{invocation.Definition.Name}: LibList crossed an invalid startup boundary.");
            return;
        }
        if (invocation.Definition.MissingDos)
        {
            Require(invocation.LibListAllocVecCalls == 0,
                "LibList used Exec after DOS open failure.");
            return;
        }
        Require(invocation.LibListAllocVecCalls == 1,
            "LibList allocation count differs.");
        if (definition.AllocationFailure)
        {
            Require(invocation.LibListFreeVecCalls == 0 &&
                invocation.LibListForbidCalls == 0 &&
                invocation.LibListFPutsCalls == 1,
                "LibList allocation failure cleanup differs.");
            return;
        }
        var rows = definition.CtrlC ? 1 : definition.Entries;
        Require(invocation.LibListFreeVecCalls == 1 &&
            invocation.LibListForbidCalls == 1 &&
            invocation.LibListPermitCalls == 1 &&
            invocation.LibListFPutsCalls == 1 &&
            invocation.LibListVPrintfCalls == rows &&
            invocation.LibListSetSignalCalls == rows &&
            invocation.LibListPrintFaultCalls == (definition.CtrlC ? 1 : 0),
            "LibList list/output lifecycle differs.");
    }
}
