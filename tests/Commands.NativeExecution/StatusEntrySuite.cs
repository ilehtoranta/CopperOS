using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record StatusEntryCase(int Entries = 0,
    int Process = 0, bool Full = false, bool Tcb = false,
    string? Command = null, bool ParserFailure = false,
    bool CtrlC = false, bool Modern = false);

internal sealed record StatusNativeLayout(uint Root, uint List, uint Cpl,
    uint Array, uint Process, uint Cli, uint Command, uint TaskName,
    uint ProcessSlot, uint CommandSlot, uint ModernArray,
    uint ModernItem, uint ModernCommand)
{
    public uint Control => Root;
}

internal sealed partial class ProbeFixture
{
    public const string StatusEntrySuite =
        "morphos320-status-native-entry-vector-fixture";
    public const string Workbench31StatusEntrySuite =
        "workbench31-status-native-entry-vector-fixture";

    public static bool IsStatusEntrySuite(string value) =>
        value == StatusEntrySuite || value == Workbench31StatusEntrySuite;

    public static bool IsWorkbench31StatusEntrySuite(string value) =>
        value == Workbench31StatusEntrySuite;

    private List<object> RunStatusEntryCases()
    {
        ProbeCase[] cases =
        [
            StatusCase("empty-list"),
            StatusCase("one-process", new(1),
                output: "Process  1: Loaded as command: Run\n"),
            StatusCase("three-processes", new(3),
                output: "Process  1: Loaded as command: Run\n" +
                    "Process  2: Loaded as command: Run1\n" +
                    "Process  3: Loaded as command: Run2\n"),
            StatusCase("tcb", new(1, Tcb: true),
                output: "Process  1: stk     256, gv  42, pri   5\n"),
            StatusCase("full", new(1, Full: true),
                output: "Process  1: stk     256, gv  42, pri   5 Loaded as command: Run\n"),
            StatusCase("command-filter", new(1, Command: "Run"),
                output: " 1\n"),
            StatusCase("command-filter-case-insensitive",
                new(1, Command: "rUn"), output: " 1\n"),
            StatusCase("missing-process", new(Process: 9),
                result: DOS.RETURN_FAIL,
                output: "Process 9 does not exist\n"),
            StatusCase("parser-failure", new(ParserFailure: true),
                result: DOS.RETURN_ERROR, error: 116),
            StatusCase("ctrl-c", new(1, CtrlC: true),
                result: DOS.RETURN_FAIL, error: (int)DOS.Error.Break,
                output: ""),
            StatusCase("modern-provider", new(2, Modern: true),
                output: "Process  1:  Loaded as command: Run\n" +
                    "Process  2:  Loaded as command: Run1\n"),
            StatusCase("modern-process-filter", new(2, Process: 2, Modern: true),
                output: "Process  2:  Loaded as command: Run1\n"),
            StatusCase("modern-command-filter", new(2, Command: "Run1", Modern: true),
                output: " 2\n"),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Status = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Status = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Status = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { Status = new(), MissingDos = true },
        ];
        if (suite != StatusEntrySuite)
            cases = cases.Where(test => test.Status?.Modern != true).ToArray();

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            StatusCase("interleaved-left", new(1),
                output: "Process  1: Loaded as command: Run\n"),
            StatusCase("interleaved-right", new(1, Full: true),
                output: "Process  1: stk     256, gv  42, pri   5 Loaded as command: Run\n")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase StatusCase(string name,
        StatusEntryCase? definition = null, int result = DOS.RETURN_OK,
        int error = 0, string? output = null)
    {
        definition ??= new();
        if (output is null)
            output = definition.ParserFailure || definition.Entries == 0
                ? "" : definition.Command is not null ? " 1\n"
                : definition.Tcb && !definition.Full
                    ? "Process  1: stk     256, gv  42, pri   5\n"
                    : definition.Full
                        ? "Process  1: stk     256, gv  42, pri   5 Loaded as command: Run\n"
                        : "Process  1: Loaded as command: Run\n";
        return new(name, definition.Command is not null ? $"COM={definition.Command}" :
            definition.Full ? "FULL" : definition.Tcb ? "TCB" :
            definition.Process != 0 ? $"PROCESS={definition.Process}" : "",
            result, error, output)
        { Status = definition };
    }

    private void PrepareStatusEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Status ?? new();
        Require(definition.Entries is >= 0 and <= 3 && definition.Process >= 0,
            "Status fixture only supports bounded empty-to-three-process lists.");
        var root = 0x5000u;
        var list = root + (uint)DosLayout.RootNode.CliList;
        var cpl = 0x6000u;
        var array = 0x6100u;
        var process = 0x7000u;
        var cli = 0x7200u;
        var command = 0x7300u;
        var taskName = 0x7400u;
        var processSlot = 0x7500u;
        var commandSlot = 0x7510u;
        var modernArray = 0x7900u;
        var modernItem = 0x7a00u;
        var modernCommand = 0x7c00u;
        invocation.StatusLayout = new StatusNativeLayout(root, list, cpl,
            array, process, cli, command, taskName, processSlot, commandSlot,
            modernArray, modernItem, modernCommand);
        Bus.Long(invocation.DosBase + (uint)DosLayout.DosLibrary.Root, root);
        var tail = list + (uint)ExecLayout.MinList.Tail;
        Bus.Long(list, definition.Entries == 0 ? tail : cpl);
        Bus.Long(cpl + (uint)ExecLayout.MinList.Tail, 0);
        Bus.Long(list + (uint)ExecLayout.MinList.TailPred,
            definition.Entries == 0 ? list : cpl);
        Bus.Long(cpl + (uint)ExecLayout.Node.Predecessor, list);
        Bus.Long(cpl + (uint)ExecLayout.Node.Successor, tail);
        Bus.Long(cpl + (uint)DosLayout.CliProcList.First, 1);
        Bus.Long(cpl + (uint)DosLayout.CliProcList.Array, array);
        Bus.Long(array, unchecked((uint)definition.Entries));
        if (definition.Entries != 0)
        {
            for (var slot = 0; slot < definition.Entries; slot++)
            {
                var processAddress = process + (uint)slot * 0x200u;
                var cliAddress = cli + (uint)slot * 0x200u;
                var commandAddress = command + (uint)slot * 0x100u;
                var taskNameAddress = taskName + (uint)slot * 0x40u;
                Bus.Long(array + 4 + (uint)slot * 4,
                    processAddress + (uint)DosLayout.Process.MessagePort);
                Bus.Long(processAddress +
                    (uint)DosLayout.Process.CommandLineInterface,
                    cliAddress >> 2);
                Bus.Long(processAddress + (uint)DosLayout.Process.GlobalVector,
                    0x7600u + (uint)slot * 4u);
                Bus.Long(processAddress + (uint)ExecLayout.Node.Name,
                    taskNameAddress);
                Bus.Memory[processAddress + (uint)ExecLayout.Node.Priority] =
                    (byte)(5 + slot);
                Bus.Long(cliAddress +
                    (uint)DosLayout.CommandLineInterface.DefaultStack, 64);
                Bus.Long(cliAddress + (uint)DosLayout.CommandLineInterface.Module,
                    1);
                Bus.Long(cliAddress +
                    (uint)DosLayout.CommandLineInterface.CommandName,
                    commandAddress >> 2);
                var commandName = slot == 0 ? "Run" : "Run" + slot;
                Bus.Memory[commandAddress] = (byte)commandName.Length;
                Encoding.Latin1.GetBytes(commandName).CopyTo(
                    Bus.Memory.AsSpan((int)commandAddress + 1));
                Encoding.Latin1.GetBytes("Task\0").CopyTo(
                    Bus.Memory.AsSpan((int)taskNameAddress));
                Bus.Long(0x7600u + (uint)slot * 4u, (uint)(42 + slot));
            }
        }
        if (definition.Process != 0)
        {
            Bus.Long(invocation.Arguments + 0x20, unchecked((uint)definition.Process));
        }
        if (definition.Command is not null)
            Encoding.Latin1.GetBytes(definition.Command + "\0").CopyTo(
                Bus.Memory.AsSpan((int)commandSlot));
        if (definition.Modern)
        {
            for (var slot = 0; slot < definition.Entries; slot++)
            {
                var itemAddress = modernItem + (uint)slot * 0x40u;
                var commandAddress = modernCommand + (uint)slot * 0x40u;
                Bus.Long(itemAddress + (uint)DosLayout.CLIDataItem.CLINumber,
                    (uint)(slot + 1));
                Bus.Long(itemAddress + (uint)DosLayout.CLIDataItem.DefaultStack,
                    64);
                Bus.Long(itemAddress + (uint)DosLayout.CLIDataItem.GlobalVector,
                    (uint)(42 + slot));
                Bus.Memory[itemAddress + (uint)DosLayout.CLIDataItem.Priority] =
                    (byte)(5 + slot);
                Bus.Memory[itemAddress + (uint)DosLayout.CLIDataItem.Flags] = 1;
                var commandName = slot == 0 ? "Run" : "Run" + slot;
                Encoding.Latin1.GetBytes(commandName + "\0").CopyTo(
                    Bus.Memory.AsSpan((int)(itemAddress +
                        (uint)DosLayout.CLIDataItem.Command)));
                Bus.Long(modernArray + (uint)slot * 4u, itemAddress);
            }
        }
    }

    private void VerifyStatusEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Status!;
        if (invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos)
            return;
        Require(invocation.StatusReadArgsCalls == 1,
            "Status ReadArgs count differs.");
        Require(invocation.StatusFreeArgsCalls ==
            (definition.ParserFailure ? 0 : 1),
            "Status FreeArgs ownership differs.");
        Require(invocation.StatusVPrintfCalls >=
            (definition.CtrlC || definition.Entries == 0 ? 0 : 1),
            "Status output path was not reached.");
        if (definition.Modern)
        {
            Require(invocation.StatusModernQueryCalls == 1,
                "Status QueryCLIDataTagList count differs.");
            Require(invocation.StatusModernFreeCalls == 1,
                "Status FreeCLIData ownership differs.");
        }
        if (definition.CtrlC)
            Require(invocation.StatusSetSignalCalls == 1,
                "Status Ctrl-C query count differs.");
    }

    private void RegisterStatusEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == NativeMorphOSStatusCommand.Template &&
                state.D[2] != 0 && state.D[3] == 0,
                "Status ReadArgs ABI differs.");
            invocation.StatusReadArgsCalls++;
            var definition = invocation.Definition.Status!;
            if (definition.ParserFailure)
            {
                invocation.IoError = 116;
                return 0;
            }
            var resultArray = state.D[2];
            if (definition.Process != 0)
            {
                Bus.Long(resultArray, invocation.StatusLayout!.ProcessSlot);
                Bus.Long(invocation.StatusLayout.ProcessSlot,
                    unchecked((uint)definition.Process));
            }
            if (definition.Full) Bus.Long(resultArray + 4, uint.MaxValue);
            if (definition.Tcb) Bus.Long(resultArray + 8, uint.MaxValue);
            if (definition.Command is not null)
            {
                Bus.Long(resultArray + 16, invocation.StatusLayout!.CommandSlot);
            }
            invocation.IoError = 0;
            return Bus.Allocate(invocation, 40, "StatusRDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "StatusRDArgs");
            invocation.StatusFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (_, _) => 0);
        Register(baseAddress, DosLvo.QueryCLIDataTagList,
            "QueryCLIDataTagList", (state, invocation) =>
            {
                Require(invocation.Definition.Status!.Modern,
                    "Status used QueryCLIDataTagList below DOS 51.51.");
                var tags = state.D[1];
                Require(tags != 0, "Status QueryCLIDataTagList tag list is null.");
                var processTag = Bus.Long(tags);
                var processValue = Bus.Long(tags + 4);
                var commandTag = Bus.Long(tags + 8);
                var commandValue = Bus.Long(tags + 12);
                var sortedTag = Bus.Long(tags + 16);
                var sortedValue = Bus.Long(tags + 20);
                var doneTag = Bus.Long(tags + 24);
                var doneValue = Bus.Long(tags + 28);
                var definition = invocation.Definition.Status!;
                var expectedProcessTag = definition.Process != 0
                    ? (uint)CLIDataTag.CLINumber : ExecConstants.TagIgnore;
                var expectedCommandTag = definition.Command is not null
                    ? (uint)CLIDataTag.CommandName : ExecConstants.TagIgnore;
                Require(processTag == expectedProcessTag &&
                    processValue == unchecked((uint)definition.Process) &&
                    commandTag == expectedCommandTag &&
                    (definition.Command is null
                        ? commandValue == 0
                        : Bus.CString(commandValue) == definition.Command) &&
                    sortedTag == (uint)CLIDataTag.Sorted && sortedValue == 1 &&
                    doneTag == 0 && doneValue == 0,
                    $"Status QueryCLIDataTagList tags differ: " +
                    $"{processTag:X8}/{processValue:X8} " +
                    $"{commandTag:X8}/{commandValue:X8} " +
                    $"{sortedTag:X8}/{sortedValue:X8} " +
                    $"{doneTag:X8}/{doneValue:X8}.");
                invocation.StatusModernQueryCalls++;
                var data = Bus.Allocate(invocation,
                    (uint)(DosLayout.CLIData.Size +
                        definition.Entries * 4),
                    "StatusCLIData", true);
                var emitted = 0;
                for (var slot = 0; slot < definition.Entries; slot++)
                {
                    var item = Bus.Long(invocation.StatusLayout!.ModernArray +
                        (uint)slot * 4u);
                    var number = Bus.Long(item + (uint)DosLayout.CLIDataItem.CLINumber);
                    if (definition.Process != 0 && number !=
                        unchecked((uint)definition.Process)) continue;
                    if (definition.Command is not null &&
                        !string.Equals(Bus.CString(item +
                            (uint)DosLayout.CLIDataItem.Command), definition.Command,
                            StringComparison.OrdinalIgnoreCase)) continue;
                    Bus.Long(data + (uint)DosLayout.CLIData.CLIs +
                        (uint)emitted * 4u, item);
                    emitted++;
                }
                Bus.Long(data + (uint)DosLayout.CLIData.NumberOfCLIs,
                    unchecked((uint)emitted));
                return data;
            });
        Register(baseAddress, DosLvo.FreeCLIData, "FreeCLIData",
            (state, invocation) =>
            {
                Require(invocation.Definition.Status!.Modern,
                    "Status used FreeCLIData below DOS 51.51.");
                Bus.Release(invocation, state.D[1], "StatusCLIData");
                invocation.StatusModernFreeCalls++;
                return 0;
            });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            var format = Bus.CString(state.D[1]);
            var values = state.D[2];
            var number = Bus.Long(values);
            var stack = Bus.Long(values + 4);
            var global = Bus.Long(values + 8);
            var priority = unchecked((int)Bus.Long(values + 12));
            var command = Bus.Long(values + 16);
            var name = Bus.Long(values + 20);
            var output = format switch
            {
                "%2ld\n" => $"{unchecked((int)number),2}\n",
                "Process %2ld: " => $"Process {unchecked((int)number),2}: ",
                "stk %7ld, gv %3ld, pri %3ld" =>
                    $"stk {unchecked((int)stack),7}, gv {unchecked((int)global),3}, pri {priority,3}",
                "stk %7ld, gv %3ld, pri %3ld\n" =>
                    $"stk {unchecked((int)stack),7}, gv {unchecked((int)global),3}, pri {priority,3}\n",
                "stk %7ld, gv %3ld, pri %3ld " =>
                    $"stk {unchecked((int)stack),7}, gv {unchecked((int)global),3}, pri {priority,3} ",
                "\n" => "\n",
                " Loaded as command: %s\n" =>
                    " Loaded as command: " + Bus.CString(command) + "\n",
                " Loaded as command: %b\n" =>
                    " Loaded as command: " + ReadBString(command) + "\n",
                "Loaded as command: %b\n" =>
                    "Loaded as command: " + ReadBString(command) + "\n",
                " No Command loaded\n" => " No Command loaded\n",
                "No Command loaded\n" => "No Command loaded\n",
                "%s has no CLI\n" => Bus.CString(name) + " has no CLI\n",
                "Process %ld does not exist\n" =>
                    $"Process {unchecked((int)number)} does not exist\n",
                _ => throw new InvalidOperationException(
                    $"Unexpected Status VPrintf format: {format}")
            };
            invocation.Output.Write(Encoding.Latin1.GetBytes(output));
            invocation.StatusVPrintfCalls++;
            return unchecked((uint)output.Length);
        });
    }

    private string ReadBString(uint raw)
    {
        if (raw == 0) return "";
        var address = raw << 2;
        var length = Bus.Memory[address];
        return Encoding.Latin1.GetString(Bus.Memory,
            (int)address + 1, length);
    }
}
