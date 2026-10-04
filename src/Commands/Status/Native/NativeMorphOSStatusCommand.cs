using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Source-observed MorphOS 50.6 Status command body.</summary>
public static class NativeMorphOSStatusCommand
{
    public const string Template =
        "PROCESS/N,FULL/S,TCB/S,CLI=ALL/S,COM=COMMAND/K";
    public const uint ResultCount = 5;

    private const uint CtrlCMask = 1u << 12;

    private struct Fields
    {
        public uint Number;
        public uint Stack;
        public uint GlobalVector;
        public uint Priority;
        public uint Command;
        public uint Name;

        public static APTR AddressOf(ref Fields fields) =>
            throw new System.NotSupportedException(
                "Status.Fields.AddressOf is lowered by CopperSharp.");
    }

    private struct QueryTags
    {
        public TagItem Process;
        public TagItem Command;
        public TagItem Sorted;
        public TagItem Done;

        public static APTR AddressOf(ref QueryTags tags) =>
            throw new System.NotSupportedException(
                "Status.QueryTags.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Uses MorphOS' CLI snapshot API when the DOS library advertises it and
    /// retains the classic DOS CLI-list fallback for older systems. Formatting
    /// happens after each snapshot has been obtained; no output is issued while
    /// Exec list protection is held.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return arguments.ReturnLevel;
        }

        var processSlot = ReadPointer(ref arguments, 0);
        var hasProcess = processSlot.IsNotNull;
        var process = hasProcess ? APTR.ReadUInt32(processSlot, 0) : 0u;
        var full = ReadSwitch(ref arguments, 1);
        var tcb = ReadSwitch(ref arguments, 2);
        var cli = ReadSwitch(ref arguments, 3);
        var command = ReadPointer(ref arguments, 4);
        var found = false;
        var result = DOS.RETURN_OK;

        if (SupportsCliData())
        {
            var tags = default(QueryTags);
            tags.Process = hasProcess
                ? TagItem.Create((uint)CLIDataTag.CLINumber,
                    process)
                : TagItem.Create(ExecConstants.TagIgnore, 0);
            tags.Command = command.IsNotNull
                ? TagItem.Create((uint)CLIDataTag.CommandName,
                    command.Raw)
                : TagItem.Create(ExecConstants.TagIgnore, 0);
            tags.Sorted = TagItem.Create((uint)CLIDataTag.Sorted, 1);
            tags.Done = TagItem.Done;
            var data = DOS.QueryCLIDataTagList(
                QueryTags.AddressOf(ref tags));
            if (data.IsNotNull)
            {
                var count = APTR.ReadUInt32(data,
                    DosLayout.CLIData.NumberOfCLIs);
                for (var index = 0u; index < count; index++)
                {
                    // The released MorphOS source polls Ctrl-C before it
                    // marks or renders each snapshot entry. Preserve that
                    // boundary so an already-pending break emits no partial
                    // row and leaves the result precedence unchanged.
                    if (BreakPending(ref result)) break;
                    var item = APTR.FromPointer(APTR.ReadUInt32(data,
                        DosLayout.CLIData.CLIs + unchecked((int)index * 4)));
                    if (item.IsNull) continue;
                    found = true;
                    if (command.IsNotNull)
                    {
                        EmitNumber(item, 0);
                    }
                    else
                    {
                        EmitCliData(item, full != 0, tcb != 0);
                    }
                }
                DOS.FreeCLIData(data);
            }
        }
        else
        {
            EnumerateLegacy(process, hasProcess, full != 0, tcb != 0, command,
                ref found, ref result);
        }

        if (!found && command.IsNotNull)
            result = DOS.RETURN_WARN;
        else if (!found && hasProcess)
        {
            var errorArguments = default(Fields);
            errorArguments.Number = process;
            DOS.VPrintf("Process %ld does not exist\n",
                Fields.AddressOf(ref errorArguments));
            result = DOS.RETURN_FAIL;
        }

        if (result != DOS.RETURN_OK)
        {
            ioError = (int)DOS.IoErr();
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
        }
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static uint ReadSwitch(ref NativeCommandArguments arguments, uint index)
    {
        return arguments.TryGetResult(index, out var value) ? value : 0;
    }

    private static APTR ReadPointer(ref NativeCommandArguments arguments,
        uint index)
    {
        return arguments.TryGetResult(index, out var value)
            ? APTR.FromPointer(value) : APTR.Null;
    }

    private static bool SupportsCliData()
    {
        var dos = DOS.DOSLibraryBase;
        if (dos.IsNull) return false;
        var version = APTR.ReadUInt16(dos, ExecLayout.Library.Version);
        var revision = APTR.ReadUInt16(dos, ExecLayout.Library.Revision);
        return version > 51 || version == 51 && revision >= 51;
    }

    private static void EmitNumber(APTR item, int offset)
    {
        var fields = default(Fields);
        fields.Number = APTR.ReadUInt32(item,
            DosLayout.CLIDataItem.CLINumber);
        DOS.VPrintf("%2ld\n", Fields.AddressOf(ref fields));
    }

    private static void EmitCliData(APTR item, bool full, bool tcb)
    {
        var fields = default(Fields);
        fields.Number = APTR.ReadUInt32(item,
            DosLayout.CLIDataItem.CLINumber);
        fields.Stack = APTR.ReadUInt32(item,
            DosLayout.CLIDataItem.DefaultStack) << 2;
        fields.GlobalVector = APTR.ReadUInt32(item,
            DosLayout.CLIDataItem.GlobalVector);
        fields.Priority = unchecked((uint)(int)(sbyte)APTR.ReadUInt8(item,
            DosLayout.CLIDataItem.Priority));
        fields.Command = item.Raw + DosLayout.CLIDataItem.Command;
        DOS.VPrintf("Process %2ld: ", Fields.AddressOf(ref fields));
        if (tcb || full)
        {
            DOS.VPrintf("stk %7ld, gv %3ld, pri %3ld",
                Fields.AddressOf(ref fields));
        }
        if (tcb && !full)
        {
            DOS.VPrintf("\n", Fields.AddressOf(ref fields));
        }
        else if ((APTR.ReadUInt8(item, DosLayout.CLIDataItem.Flags) & 1) != 0)
        {
            DOS.VPrintf(" Loaded as command: %s\n",
                Fields.AddressOf(ref fields));
        }
        else
        {
            DOS.VPrintf(" No Command loaded\n",
                Fields.AddressOf(ref fields));
        }
    }

    private static void EnumerateLegacy(uint process, bool hasProcess,
        bool full, bool tcb,
        APTR command, ref bool found, ref int result)
    {
        var dos = DOS.DOSLibraryBase;
        var root = APTR.FromPointer(APTR.ReadUInt32(dos,
            DosLayout.DosLibrary.Root));
        if (root.IsNull) return;
        var list = APTR.FromPointer(root.Raw + DosLayout.RootNode.CliList);
        var cpl = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.MinList.Head));
        var current = 1u;
        while (cpl.IsNotNull &&
            APTR.ReadUInt32(cpl, ExecLayout.MinList.Head) != 0)
        {
            var array = APTR.FromPointer(APTR.ReadUInt32(cpl,
                DosLayout.CliProcList.Array));
            var count = APTR.ReadUInt32(array, 0);
            for (var index = 0u; index < count; index++)
            {
                // Match the source's SetSignal boundary: query before
                // inspecting or rendering the process slot.
                if (BreakPending(ref result)) return;
                var port = APTR.ReadUInt32(array,
                    4 + unchecked((int)(index * 4)));
                if (port == 0) continue;
                var task = APTR.FromPointer(port -
                    (uint)DosLayout.Process.MessagePort);
                var cliAddress = APTR.ReadUInt32(task,
                    DosLayout.Process.CommandLineInterface);
                var cli = cliAddress == 0 ? APTR.Null :
                    BPTR.FromRaw(cliAddress).Address;
                if (command.IsNotNull)
                {
                    if (cli.IsNotNull && MatchesCommand(cli, command))
                    {
                        var fields = default(Fields);
                        fields.Number = current + index;
                        DOS.VPrintf("%2ld\n", Fields.AddressOf(ref fields));
                        found = true;
                    }
                }
                else if (!hasProcess || process == current + index)
                {
                    EmitLegacy(task, cli, full, tcb, current + index);
                    found = true;
                }
            }
            current += count;
            cpl = APTR.FromPointer(APTR.ReadUInt32(cpl,
                ExecLayout.MinList.Head));
        }
    }

    private static void EmitLegacy(APTR task, APTR cli, bool full, bool tcb,
        uint number)
    {
        var fields = default(Fields);
        fields.Number = number;
        fields.Stack = cli.IsNull ? 0 : APTR.ReadUInt32(cli,
            DosLayout.CommandLineInterface.DefaultStack) << 2;
        var global = APTR.ReadUInt32(task, DosLayout.Process.GlobalVector);
        fields.GlobalVector = global == 0 ? 0 :
            APTR.ReadUInt32(APTR.FromPointer(global), 0);
        fields.Priority = unchecked((uint)(int)(sbyte)APTR.ReadUInt8(task,
            ExecLayout.Node.Priority));
        fields.Name = APTR.ReadUInt32(task, ExecLayout.Node.Name);
        fields.Command = cli.IsNull ? 0 : APTR.ReadUInt32(cli,
            DosLayout.CommandLineInterface.CommandName);
        DOS.VPrintf("Process %2ld: ", Fields.AddressOf(ref fields));
        if (cli.IsNull)
        {
            DOS.VPrintf("%s has no CLI\n", Fields.AddressOf(ref fields));
            return;
        }
        if (tcb && !full)
        {
            DOS.VPrintf("stk %7ld, gv %3ld, pri %3ld\n",
                Fields.AddressOf(ref fields));
            return;
        }
        if (full)
            DOS.VPrintf("stk %7ld, gv %3ld, pri %3ld ",
                Fields.AddressOf(ref fields));
        if (APTR.ReadUInt32(cli, DosLayout.CommandLineInterface.Module) != 0)
            DOS.VPrintf("Loaded as command: %b\n",
                Fields.AddressOf(ref fields));
        else
            DOS.VPrintf("No Command loaded\n",
                Fields.AddressOf(ref fields));
    }

    private static bool MatchesCommand(APTR cli, APTR command)
    {
        var bstr = APTR.ReadUInt32(cli,
            DosLayout.CommandLineInterface.CommandName);
        if (bstr == 0) return false;
        var length = APTR.ReadUInt8(BPTR.FromRaw(bstr).Address, 0);
        var wanted = command.Raw;
        var count = 0u;
        while (APTR.ReadUInt8(APTR.FromPointer(wanted + count), 0) != 0)
            count++;
        if (count != length) return false;
        for (var index = 0u; index < count; index++)
            if (FoldAsciiCase(APTR.ReadUInt8(
                    APTR.FromPointer(wanted + index), 0)) !=
                FoldAsciiCase(APTR.ReadUInt8(BPTR.FromRaw(bstr).Address,
                    1 + unchecked((int)index))))
                return false;
        return true;
    }

    private static byte FoldAsciiCase(byte value) =>
        value is >= (byte)'a' and <= (byte)'z'
            ? (byte)(value - ('a' - 'A'))
            : value;

    private static bool BreakPending(ref int result)
    {
        if ((Exec.SetSignal(0u, 0u) & CtrlCMask) == 0) return false;
        result = DOS.RETURN_FAIL;
        DOS.SetIoErr(DOS.Error.Break);
        return true;
    }
}
