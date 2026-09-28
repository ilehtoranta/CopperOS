using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record ModListEntryCase(int Entries = 0,
    bool AllocationFailure = false, bool CtrlC = false,
    bool ParserFailure = false, bool IdRevision = false);

internal sealed record ModListNativeLayout(uint Table, uint Resident,
    uint Name, uint IdString)
{
    public uint Control => Table;
}

internal sealed partial class ProbeFixture
{
    public const string ModListEntrySuite =
        "morphos320-modlist-native-entry-vector-fixture";

    private const string ModListHeader =
        "address                    name                version   pri    flags\n" +
        "--------------------------------------------------------------------------\n";
    private const string ModListMemoryError =
        "Not Enough memory for resident buffer\n";

    private List<object> RunModListEntryCases()
    {
        ProbeCase[] cases =
        [
            ModCase("empty-list"),
            ModCase("one-resident", new(1)),
            ModCase("id-string-revision", new(1, IdRevision: true)),
            ModCase("three-residents", new(3)),
            ModCase("ctrl-c", new(3, CtrlC: true),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.Break),
            ModCase("allocation-failure", new(AllocationFailure: true),
                result: DOS.RETURN_FAIL, error: 0,
                output: ModListMemoryError),
            ModCase("parser-failure", new(ParserFailure: true),
                result: DOS.RETURN_FAIL, error: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { ModList = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { ModList = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { ModList = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { ModList = new(), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            ModCase("interleaved-left", new(1), output: ModOutput(1)),
            ModCase("interleaved-right", new(1), output: ModOutput(1))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase ModCase(string name,
        ModListEntryCase? definition = null, int result = DOS.RETURN_OK,
        int error = 0, string output = "")
    {
        definition ??= new();
        if (output.Length == 0)
            output = definition.ParserFailure ? "" : definition.AllocationFailure
                ? ModListMemoryError : definition.Entries == 0
                ? ModListHeader : ModOutput(definition.CtrlC ? 1 :
                    definition.Entries, definition.IdRevision);
        return new(name, definition.IdRevision ? "VERBOSE" : "", result,
            error, output)
        {
            ModList = definition
        };
    }

    private static string ModOutput(int entries, bool idRevision = false)
    {
        var output = new StringBuilder(ModListHeader);
        for (var slot = 0; slot < entries; slot++)
        {
            var name = slot == 0 ? "Mod" : "Mod" + slot;
            var version = 50 + slot;
            var revision = 4 + slot;
            var priority = -1 + slot;
            var flags = slot == 0 && idRevision ? "NEVER" :
                "AutoInit | Extended";
            output.Append(string.Format(
                "0x{0:x8}\t{1}\t {2,2}.{3,-3}\t{4,4}\t<{5}>\n",
                0x6000u + (uint)slot * 0x100u, name.PadLeft(30),
                version, revision, priority, flags));
        }
        return output.ToString();
    }

    private void PrepareModListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.ModList ?? new();
        Require(definition.Entries is >= 0 and <= 3,
            "ModList fixture only supports bounded empty-to-three-resident lists.");
        var table = 0x5000u;
        var resident = 0x6000u;
        var name = 0x7000u;
        var idString = 0x7100u;
        invocation.ModListLayout = new ModListNativeLayout(
            table, resident, name, idString);
        var execTable = 0x4000u + (uint)ExecLayout.ExecBase.ResModules;
        Bus.Long(execTable, table);
        Bus.Long(table + (uint)definition.Entries * 4u, 0);
        for (var slot = 0; slot < definition.Entries; slot++)
        {
            var residentAddress = resident + (uint)slot * 0x100u;
            var nameAddress = name + (uint)slot * 0x40u;
            var idAddress = idString + (uint)slot * 0x80u;
            Bus.Long(table + (uint)slot * 4u, residentAddress);
            Bus.Word(residentAddress + (uint)ExecLayout.Resident.MatchWord,
                ExecConstants.ResidentMatchWord);
            Bus.Memory[residentAddress + (uint)ExecLayout.Resident.Flags] =
                definition.IdRevision ? (byte)0 : (byte)0xc0;
            Bus.Memory[residentAddress + (uint)ExecLayout.Resident.Version] =
                (byte)(50 + slot);
            Bus.Memory[residentAddress + (uint)ExecLayout.Resident.Priority] =
                (byte)(unchecked((sbyte)(-1 + slot)));
            Bus.Long(residentAddress + (uint)ExecLayout.Resident.Name,
                nameAddress);
            Bus.Long(residentAddress + (uint)ExecLayout.Resident.IdString,
                idAddress);
            Bus.Word(residentAddress + (uint)Resident.Size,
                (ushort)(4 + slot));
            var moduleName = slot == 0 ? "Mod" : "Mod" + slot;
            Encoding.Latin1.GetBytes($"{moduleName}\0").CopyTo(
                Bus.Memory.AsSpan((int)nameAddress));
            var id = $"$VER: ModList {50 + slot}.{4 + slot} (13.6.05)\0";
            Encoding.Latin1.GetBytes(id).CopyTo(
                Bus.Memory.AsSpan((int)idAddress));
        }
    }

    private void RegisterModListEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == NativeMorphOSModListCommand.Template &&
                state.D[2] != 0 && state.D[3] == 0,
                "ModList ReadArgs ABI differs.");
            invocation.ModListReadArgsCalls++;
            if (invocation.Definition.ModList!.ParserFailure)
            {
                invocation.IoError = 116;
                return 0;
            }
            return Bus.Allocate(invocation, 16, "ModListRDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "ModListRDArgs");
            invocation.ModListFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "ModList FPuts stream differs.");
            var text = Bus.CString(state.D[2]);
            Require(text == ModListHeader || text == ModListMemoryError ||
                text == " | " || text is "ColdStart" or "SingleTask" or
                "AfterDos" or "PPC" or "Bit4" or "*Asynchron*" or
                "Extended" or "AutoInit" or "NEVER" or ">\n",
                "ModList FPuts text differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.ModListFPutsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            var format = Bus.CString(state.D[1]);
            Require(format == "0x%08.lx\t%30.s\t%3lu.%-3lu\t%4.ld\t<" ||
                format == "0x%08.lx\t%30.s\t%3lu\t%4.ld\t<",
                $"ModList VPrintf format differs: {format}.");
            var args = state.D[2];
            var address = Bus.Long(args);
            var name = Bus.CString(Bus.Long(args + 4));
            var version = Bus.Long(args + 8);
            var revision = Bus.Long(args + 12);
            var priority = unchecked((int)Bus.Long(args + 16));
            var sourceId = Bus.CString(invocation.ModListLayout!.IdString);
            var slot = checked((int)((address - 0x6000u) / 0x100u));
            var expectedName = slot == 0 ? "Mod" : "Mod" + slot;
            Require(address == 0x6000u + (uint)slot * 0x100u &&
                name == expectedName && version == (uint)(50 + slot) &&
                revision == (uint)(4 + slot) && priority == -1 + slot,
                $"ModList resident fields differ (source={sourceId}, format={format}, address=0x{address:x}, name={name}, version={version}, revision={revision}, priority={priority}).");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                $"0x{address:x8}\t{name.PadLeft(30)}\t{version,3}.{revision,-3}\t{priority,4}\t<"));
            invocation.ModListVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (_, invocation) => { invocation.ModListPrintFaultCalls++; return 0; });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifyModListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.ModList!;
        if (invocation.Definition.Workbench)
        {
            Require(invocation.ModListReadArgsCalls == 0 &&
                invocation.ModListForbidCalls == 1,
                "ModList Workbench startup crossed into the body.");
            return;
        }
        if (invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.ModListReadArgsCalls == 0 &&
                invocation.ModListForbidCalls == 0,
                "ModList crossed an invalid startup boundary.");
            return;
        }
        if (invocation.Definition.MissingDos)
        {
            Require(invocation.ModListReadArgsCalls == 0 &&
                invocation.ModListAllocVecCalls == 0,
                "ModList used DOS after an open failure.");
            return;
        }
        Require(invocation.ModListReadArgsCalls == 1,
            "ModList ReadArgs count differs.");
        if (definition.ParserFailure)
        {
            Require(invocation.ModListFreeArgsCalls == 0 &&
                invocation.ModListAllocVecCalls == 0 &&
                invocation.ModListPrintFaultCalls == 1,
                "ModList parser failure cleanup differs.");
            return;
        }
        Require(invocation.ModListFreeArgsCalls == 1 &&
            invocation.ModListAllocVecCalls == 1,
            "ModList parser/buffer ownership differs.");
        if (definition.AllocationFailure)
        {
            Require(invocation.ModListFreeVecCalls == 0 &&
                invocation.ModListForbidCalls == 0 &&
                invocation.ModListFPutsCalls == 1 &&
                invocation.ModListPrintFaultCalls == 0,
                "ModList allocation failure cleanup differs.");
            return;
        }
        var rows = definition.CtrlC ? 1 : definition.Entries;
        Require(invocation.ModListFreeVecCalls == 1 &&
            invocation.ModListForbidCalls == 1 &&
            invocation.ModListPermitCalls == 1 &&
            invocation.ModListVPrintfCalls == rows &&
            invocation.ModListSetSignalCalls == rows &&
            invocation.ModListPrintFaultCalls == (definition.CtrlC ? 1 : 0),
            "ModList list/output lifecycle differs.");
    }
}
