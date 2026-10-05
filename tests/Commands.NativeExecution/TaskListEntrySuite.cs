using System.Globalization;
using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record TaskListEntryCase(
    int Ready = 1,
    int Waiting = 1,
    bool NoRun = false,
    bool NoReady = false,
    bool NoWait = false,
    string? Name = null,
    uint? Address = null,
    bool StackTrace = false,
    bool Internal = false,
    bool EmulationFrame = false,
    bool RegisterCheck = false,
    string? RegisterCheckScenario = null,
    int? StackLevel = null,
    bool SegmentLookup = false,
    bool AllocationFailure = false,
    bool CtrlC = false,
    int ParserError = 0,
    bool ReadyFirstCli = false,
    string CliName = "Command",
    bool RegisterDump = false,
    bool Verbose = false,
    bool SysDebugUnavailable = false,
    bool UnknownStackUsed = false,
    bool WaitingDead = false);

internal sealed record TaskListNativeLayout(uint ReadyList, uint WaitList,
    uint ReadyTask, uint WaitingTask, uint ReadyName, uint WaitingName,
    int ReadyCount, int WaitingCount)
{
    public uint Control => ReadyList;
}

internal sealed partial class ProbeFixture
{
    public const string TaskListEntrySuite =
        "morphos320-tasklist-native-entry-vector-fixture";

    private const string TaskListHeader =
        "   pid    address type  pri   state 68kstack/used     ppcstack/used     name\n";
    private const int TaskListBufferCapacity = 84;
    private const uint TaskListSegSemaphore = 0x36000u;
    private const uint TaskListSegFindEntry = 0x7000u;
    private const uint TaskListSegName = 0x35000u;

    private List<object> RunTaskListEntryCases()
    {
        ProbeCase[] cases =
        [
            TaskCase("unknown-stack-used", new(NoRun: true, NoWait: true, UnknownStackUsed: true),
                output: TaskOutput(false, true, false).Replace("/128     ", "/???     ", StringComparison.Ordinal)),
            TaskCase("unknown-cli-stack-used", new(NoRun: true, NoWait: true, ReadyFirstCli: true, UnknownStackUsed: true),
                output: TaskOutput(false, 1, 0, "Command").Replace("/128     ", "/???     ", StringComparison.Ordinal)),
            TaskCase("waiting-no-signal-dead", new(NoRun: true, NoReady: true, WaitingDead: true),
                output: TaskOutput(false, false, true).Replace("   wait", "   dead", StringComparison.Ordinal)),
            TaskCase("all-three-lists", new(),
                output: TaskOutput(true, true, true)),
            TaskCase("three-ready-waiting", new(Ready: 3, Waiting: 3),
                output: TaskOutput(true, 3, 3)),
            TaskCase("task-buffer-growth", new(Ready: 100, Waiting: 0,
                NoRun: true, NoWait: true),
                output: TaskOutput(false, 100, 0,
                    readyAddressBase: 0x200000u)),
            TaskCase("current-only", new(NoReady: true, NoWait: true),
                output: TaskOutput(true, false, false)),
            TaskCase("ready-only", new(NoRun: true, NoWait: true),
                output: TaskOutput(false, true, false)),
            TaskCase("wait-only", new(NoRun: true, NoReady: true),
                output: TaskOutput(false, false, true)),
            TaskCase("name-filter", new(Name: "Worker"),
                output: TaskOutput(false, true, false)),
            TaskCase("cli-command-name-filter", new(NoRun: true,
                NoWait: true, Name: "Copy", ReadyFirstCli: true,
                CliName: "Copy"),
                output: TaskOutput(false, 1, 0, "Copy")),
            TaskCase("address-filter", new(Address: 0x5000),
                output: TaskOutput(false, true, false)),
            TaskCase("cli-process-row", new(NoRun: true, NoWait: true,
                ReadyFirstCli: true, CliName: "Copy"),
                output: TaskOutput(false, 1, 0, "Copy")),
            TaskCase("cli-long-command-name-row", new(NoRun: true,
                NoWait: true, ReadyFirstCli: true,
                CliName: new string('L', 255)),
                output: TaskOutput(false, 1, 0, new string('L', 255))),
            TaskCase("register-dump", new(NoRun: true, NoWait: true,
                Ready: 1, RegisterDump: true),
                output: TaskOutput(false, 1, 0, registerDump: true)),
            TaskCase("stacktrace-bounded-frame-walk", new(NoRun: true,
                NoWait: true, Ready: 1, StackTrace: true, StackLevel: 3),
                output: TaskOutput(false, 1, 0,
                    stackTrace: TaskListStackTraceOutput())),
            TaskCase("stacktrace-default-stacklevel", new(NoRun: true,
                NoWait: true, Ready: 1, StackTrace: true),
                output: TaskOutput(false, 1, 0,
                    stackTrace: TaskListStackTraceOutput())),
            TaskCase("stacktrace-segtracker-symbol", new(NoRun: true,
                NoWait: true, Ready: 1, StackTrace: true, StackLevel: 3,
                SegmentLookup: true),
                output: TaskOutput(false, 1, 0,
                    stackTrace: TaskListStackTraceSymbolOutput())),
            TaskCase("internal-stacktrace-classification", new(NoRun: true,
                NoWait: true, Ready: 1, StackTrace: true, Internal: true,
                StackLevel: 3),
                output: TaskOutput(false, 1, 0,
                    stackTrace: TaskListStackTraceOutput(internalMode: true))),
            TaskCase("internal-emulation-stacktrace-classification", new(
                NoRun: true, NoWait: true, Ready: 1, StackTrace: true,
                Internal: true, EmulationFrame: true, StackLevel: 3),
                output: TaskOutput(false, 1, 0,
                    stackTrace: TaskListStackTraceOutput(internalMode: true,
                        emulationMode: true))),
            TaskCase("verbose-fields", new(NoRun: true, NoWait: true,
                Ready: 1, Verbose: true),
                output: TaskOutput(false, 1, 0, verbose: true)),
            TaskCase("verbose-name-filter-excludes-current", new(
                Name: "Worker", Verbose: true),
                output: TaskOutput(false, 1, 0, verbose: true)),
            TaskCase("verbose-register-dump-order", new(NoRun: true,
                NoWait: true, Ready: 1, Verbose: true, RegisterDump: true),
                output: TaskOutput(false, 1, 0, registerDump: true,
                    verbose: true)),
            TaskCase("verbose-running-stack-pointer", new(NoReady: true,
                NoWait: true, Verbose: true),
                output: TaskOutput(true, false, false, verbose: true)),
            TaskCase("sysdebug-unavailable", new(SysDebugUnavailable: true),
                result: DOS.RETURN_FAIL,
                error: Invocation.InitialIoError),
            TaskCase("register-check-task-offset", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "task-offset"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x5004 -> Task 0x5000 <Worker> Offset 0x4\n")),
            TaskCase("register-check-task-address", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "task-equal"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x5000 -> Task 0x5000 <Worker>\n")),
            TaskCase("register-check-process-extension", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "process-offset"),
                output: TaskOutput(false, 1, 0,
                    readyType: "proc",
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x5064 -> Task 0x5000 <Worker> Offset 0x64\n")),
            TaskCase("register-check-etask-interior", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "etask-offset"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78005 -> Task 0x5000 <Worker> ETask Offset 0x5\n")),
            TaskCase("register-check-m68k-stack", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "m68k-stack"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x62020 -> Task 0x5000 <Worker> 68kStack Offset 0x20 [0x62000..0x62800]\n")),
            TaskCase("register-check-ppc-stack", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "ppc-stack"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x70020 -> Task 0x5000 <Worker> PPCStack Offset 0x20 [0x70000..0x78000]\n")),
            TaskCase("register-check-process-stream-baddr", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "process-stream"),
                output: TaskOutput(false, 1, 0,
                    readyType: "proc",
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78000 -> Task 0x5000 <Worker> CIS\n")),
            TaskCase("register-check-process-output-raw", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "process-output-raw"),
                output: TaskOutput(false, 1, 0, readyType: "proc",
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78000 -> Task 0x5000 <Worker> COS\n")),
            TaskCase("register-check-process-error-baddr", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "process-error-baddr"),
                output: TaskOutput(false, 1, 0, readyType: "proc",
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78000 -> Task 0x5000 <Worker> CES\n")),
            TaskCase("register-check-process-currentdir-raw", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "process-dir-raw"),
                output: TaskOutput(false, 1, 0, readyType: "proc",
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78000 -> Task 0x5000 <Worker> CurrentDir Lock\n")),
            TaskCase("register-check-process-cli", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                CliName: "Copy",
                RegisterCheckScenario: "process-cli"),
                output: TaskOutput(false, 1, 0,
                    cliName: "Copy",
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x20500 -> Task 0x5000 <Worker> [ Copy ] CLI\n")),
            TaskCase("register-check-process-cli-raw-bptr", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                CliName: "Copy",
                RegisterCheckScenario: "process-cli-raw"),
                output: TaskOutput(false, 1, 0,
                    cliName: "Copy",
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x8140 -> Task 0x5000 <Worker> [ Copy ] CLI\n")),
            TaskCase("register-check-library-node", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "library-node"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78000 -> Library <test.library>\n")),
            TaskCase("register-check-device-node", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "device-node"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78000 -> Device <test.device>\n")),
            TaskCase("register-check-resource-node", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "resource-node"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78000 -> Resource <test.resource>\n")),
            TaskCase("register-check-library-function-table", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "library-function"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x77ffc -> Library <test.library> FuncTable Offset -0x4\n")),
            TaskCase("register-check-library-base", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "library-base"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78004 -> Library <test.library> Base Offset 0x4\n")),
            TaskCase("register-check-port-owner", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "port"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78000 -> Port <test.port> Task 0x5000 <Worker>\n")),
            TaskCase("register-check-semaphore-owner", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "semaphore-owner"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78000 -> Semaphore <test.semaphore> Owner 0x5000 <Worker>\n")),
            TaskCase("register-check-semaphore-no-owner", new(NoRun: true,
                NoWait: true, RegisterCheck: true,
                RegisterCheckScenario: "semaphore-no-owner"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x78000 -> Semaphore <test.semaphore> NoOwner\n")),
            TaskCase("register-check-segtracker-symbol", new(NoRun: true,
                NoWait: true, RegisterCheck: true, SegmentLookup: true,
                RegisterCheckScenario: "symbol"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x00400100 -> worker Hunk 7 Offset 0x00000100\n")),
            TaskCase("register-check-internal-module", new(NoRun: true,
                NoWait: true, RegisterCheck: true, Internal: true,
                RegisterCheckScenario: "internal-module"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x00300100 -> ABOX: Module Offset 0x100\n")),
            TaskCase("register-check-internal-emulation", new(NoRun: true,
                NoWait: true, RegisterCheck: true, Internal: true,
                RegisterCheckScenario: "internal-emulation"),
                output: TaskOutput(false, 1, 0,
                    registerCheckOutput:
                        "     GPR[00] -> Address 0x00240100 -> ABOX: Emulation Offset 0xfff40100\n")),
            TaskCase("allocation-failure", new(AllocationFailure: true),
                result: DOS.RETURN_FAIL,
                output: "Not Enough memory for task buffer\n"),
            TaskCase("ctrl-c", new(CtrlC: true),
                result: DOS.RETURN_FAIL,
                error: (int)DOS.Error.Break,
                output: TaskOutput(true, false, false)),
            TaskCase("parser-failure", new(ParserError: 116),
                result: DOS.RETURN_ERROR, error: 116),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { TaskList = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { TaskList = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { TaskList = new(), EntryLength = 4, NullArgumentPointer = true },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { TaskList = new(), MissingDos = true },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            TaskCase("interleaved-left", new(),
                output: TaskOutput(true, true, true)),
            TaskCase("interleaved-right", new(Name: "Worker"),
                output: TaskOutput(false, true, false))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase TaskCase(string name, TaskListEntryCase definition,
        int result = DOS.RETURN_OK, int error = 0, string output = "")
    {
        var args = new StringBuilder();
        if (definition.Name is not null) args.Append(definition.Name);
        if (definition.Address is uint address)
            args.Append(args.Length == 0 ? "ADDRESS=" : " ADDRESS=")
                .Append(address);
        if (definition.StackTrace) args.Append(" STACKTRACE");
        if (definition.StackLevel is int stackLevel)
            args.Append(" STACKLEVEL=").Append(stackLevel);
        if (definition.Internal) args.Append(" INTERNAL");
        if (definition.RegisterCheck) args.Append(" REGCHECK");
        if (definition.Verbose) args.Append(" VERBOSE");
        if (definition.RegisterDump) args.Append(" REGDUMP");
        if (definition.NoRun) args.Append(" NORUN");
        if (definition.NoReady) args.Append(" NOREADY");
        if (definition.NoWait) args.Append(" NOWAIT");
        return new(name, args.ToString(), result, error,
            output.Length == 0 && definition.ParserError == 0 &&
            !definition.RegisterCheck && !definition.AllocationFailure &&
            !definition.CtrlC && !definition.SysDebugUnavailable
                ? TaskOutput(
                !definition.NoRun && definition.Name is null &&
                    definition.Address is null,
                !definition.NoReady && definition.Name is null &&
                    definition.Address is null || definition.Name == "Worker" ||
                    definition.Address == 0x5000,
                !definition.NoWait && definition.Name is null &&
                    definition.Address is null,
                registerDump: definition.RegisterDump,
                verbose: definition.Verbose) : output)
        { TaskList = definition };
    }

    private static string TaskOutput(bool current, bool ready, bool waiting,
        bool registerDump = false, bool verbose = false)
        => TaskOutput(current, ready ? 1 : 0, waiting ? 1 : 0,
            registerDump: registerDump, verbose: verbose);

    private static string TaskOutput(bool current, int ready, int waiting,
        string? cliName = null, bool registerDump = false,
        bool verbose = false, uint readyAddressBase = 0x5000u,
        string? stackTrace = null, string? registerCheckOutput = null,
        string readyType = "task")
    {
        var output = new StringBuilder(TaskListHeader);
        if (current) output.Append(Row(1, 0x10000, " cli", 5, "    run",
            0x1000, "Shell", "TaskList"));
        if (current && verbose)
            output.Append(TaskListVerboseOutput(0x10000, liveA7: true));
        for (var slot = 0; slot < ready; slot++)
        {
            if (slot == 0 && cliName is not null)
                output.Append(Row((uint)(2 + slot),
                    readyAddressBase + (uint)slot * 0x100u, " cli", 1 + slot,
                    "  ready", 0x800, TaskName("Worker", slot), cliName));
            else
                output.Append(Row((uint)(2 + slot),
                    readyAddressBase + (uint)slot * 0x100u, readyType, 1 + slot,
                    "  ready", 0x800, TaskName("Worker", slot)));
            if (verbose)
                output.Append(TaskListVerboseOutput(0x5000u +
                    (uint)slot * 0x100u));
            if (stackTrace is not null && slot == 0)
                output.Append(stackTrace);
            if (registerDump)
                output.Append(RegisterDumpOutput(0x5000u +
                    (uint)slot * 0x100u));
            if (registerCheckOutput is not null && slot == 0)
                output.Append(registerCheckOutput);
        }
        for (var slot = 0; slot < waiting; slot++)
        {
            output.Append(Row((uint)(5 + slot), 0x6000u + (uint)slot * 0x100u,
                "task", -2 - slot, "   wait", 0x400,
                TaskName("Waiter", slot)));
            if (verbose)
                output.Append(TaskListVerboseOutput(0x6000u +
                    (uint)slot * 0x100u));
            if (registerDump)
                output.Append(RegisterDumpOutput(0x6000u +
                    (uint)slot * 0x100u));
        }
        return output.ToString();
    }

    private static string TaskListVerboseOutput(uint task, bool liveA7 = false)
    {
        var state = task == 0x10000 ? 2u : task < 0x6000 ? 3u : 4u;
        var m68kLower = task == 0x10000 ? 0x60000u :
            task < 0x6000 ? 0x62000u + (task - 0x5000u) * 0x10u :
            0x63000u + (task - 0x6000u) * 0x10u;
        var m68kUpper = task == 0x10000 ? 0x61000u :
            task < 0x6000 ? 0x62800u + (task - 0x5000u) * 0x10u :
            0x63400u + (task - 0x6000u) * 0x10u;
        var ppcLower = task == 0x10000 ? 0xD0000u :
            task < 0x6000 ? 0x70000u + (task - 0x5000u) * 0x100u :
            0xA0000u + (task - 0x6000u) * 0x100u;
        var ppcUpper = ppcLower + 32768u;
        var stackPointer = liveA7 ? "DEADBEEF" :
            m68kUpper.ToString("x8", CultureInfo.InvariantCulture);
        return $":     State {state}\n" +
            ":   SigWait 0x00000000\n" +
            ": SigExcept 0x00000080\n" +
            ":  SigRecvd 0x00000040\n" +
            $":M68k SPUpper 0x{m68kUpper:x8}\n" +
            $":M68k SPLower 0x{m68kLower:x8}\n" +
            $":M68k  SPReg 0x{stackPointer}\n" +
            $": PPC SPUpper 0x{ppcUpper:x8}\n" +
            $": PPC SPLower 0x{ppcLower:x8}\n";
    }

    private static bool TaskListOutputMatchesWithLiveStackPointer(
        string expected, string actual, Invocation invocation)
    {
        const string marker = "DEADBEEF";
        var markerIndex = expected.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0) return expected == actual;
        if (actual.Length != expected.Length ||
            !expected.AsSpan(0, markerIndex).SequenceEqual(
                actual.AsSpan(0, markerIndex)) ||
            !expected.AsSpan(markerIndex + marker.Length).SequenceEqual(
                actual.AsSpan(markerIndex + marker.Length)))
            return false;
        if (!uint.TryParse(actual.AsSpan(markerIndex, marker.Length),
                NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
                out var stackPointer))
            return false;
        var lower = invocation.StackTop >= invocation.StackBytes
            ? invocation.StackTop - invocation.StackBytes : 0;
        return stackPointer >= lower && stackPointer <= invocation.StackTop;
    }

    private static string RegisterDumpOutput(uint task)
    {
        var output = new StringBuilder(RegisterHeaderOutput(
            RegisterValue(task, 0x100), RegisterValue(task, 0x102),
            RegisterValue(task, 0x103), RegisterValue(task, 0x104),
            RegisterValue(task, 0x105)));
        for (var group = 0; group < 4; group++)
        {
            output.Append("     GPR[").Append((group * 8).ToString("D2",
                CultureInfo.InvariantCulture)).Append("]");
            for (var register = 0; register < 8; register++)
                output.Append(" 0x").Append((0x10000000u +
                    (uint)(group * 8 + register)).ToString("x8",
                    CultureInfo.InvariantCulture));
            output.Append('\n');
        }
        return output.ToString();
    }

    private static string TaskListStackTraceOutput(bool internalMode = false,
        bool emulationMode = false)
    {
        if (internalMode)
            return emulationMode
                ? "     StackFrame[ 0].LR -> Address 0x00240100 -> ABOX: Emulation Offset 0x100\n"
                : "     StackFrame[ 0].LR -> Address 0x00300100 -> ABOX: Module Offset 0x100\n";
        return "     StackFrame[ 0].LR -> Address 0x00400100\n" +
            "     StackFrame[ 1].LR -> Address 0x00400200\n";
    }

    private static string TaskListStackTraceSymbolOutput() =>
        "                  SRR0 -> Address 0x00400100 -> worker Hunk 7 Offset 0x00000100\n" +
        "                   CTR -> Address 0x00400100 -> worker Hunk 7 Offset 0x00000100\n" +
        "     StackFrame[ 0].LR -> Address 0x00400100 -> worker Hunk 7 Offset 0x00000100\n" +
        "     StackFrame[ 1].LR -> Address 0x00400200\n";

    private static string TaskListStackFrameLine(uint index, uint address,
        string? description = null, uint offset = 0)
    {
        var line = "     StackFrame[" + index.ToString(
            CultureInfo.InvariantCulture).PadLeft(2) +
            "].LR -> Address 0x" + address.ToString("x8",
                CultureInfo.InvariantCulture);
        if (description is not null)
            line += " -> " + description + " 0x" + offset.ToString("x",
                CultureInfo.InvariantCulture);
        return line + "\n";
    }

    private static string RegisterHeaderOutput(uint srr0, uint lr, uint ctr,
        uint cr, uint xer) => new StringBuilder("     SRR0 0x")
        .Append(srr0.ToString("x8", CultureInfo.InvariantCulture))
        .Append(" LR 0x")
        .Append(lr.ToString("x8", CultureInfo.InvariantCulture))
        .Append(" CTR 0x")
        .Append(ctr.ToString("x", CultureInfo.InvariantCulture))
        .Append(" CR 0x")
        .Append(cr.ToString("x", CultureInfo.InvariantCulture))
        .Append(" XER 0x")
        .Append(xer.ToString("x", CultureInfo.InvariantCulture))
        .Append('\n').ToString();

    private static uint RegisterValue(uint task, uint attribute) =>
        attribute switch
        {
            0x100 => 0x10000000u + task,
            0x102 => 0x20000000u + task,
            0x103 => 0x30000000u + task,
            0x104 => 0x40000000u + (task >> 8),
            0x105 => 0x50000000u + task,
            _ => throw new ArgumentOutOfRangeException(nameof(attribute))
        };

    private static string Row(uint pid, uint address, string type, int priority,
        string state, uint stackSize, string name, string? cliName = null,
        string stackUsed = "128", uint ppcStackSize = 32768,
        uint ppcStackUsed = 512) =>
        $"{(cliName is null ? $"{pid,4}" : $"{pid,6}")} 0x{address:x8} {type} {priority,4} {state} {stackSize,8}/{stackUsed,-8} {ppcStackSize,8}/{ppcStackUsed,-8} {name}{(cliName is null ? "" : $" [{cliName}]")}\n";

    private void PrepareTaskListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.TaskList ?? new();
        Require(definition.Ready is >= 0 and <= 100 &&
            definition.Waiting is >= 0 and <= 3,
            "TaskList fixture supports up to one hundred ready nodes and three waiting nodes.");
        var readyList = ExecBase + (uint)ExecLayout.ExecBase.TaskReady;
        var waitList = ExecBase + (uint)ExecLayout.ExecBase.TaskWait;
        var readyTask = definition.Ready > 3 ? 0x200000u : 0x5000u;
        var readyTaskSlots = Math.Max(3, definition.Ready);
        var waitingTask = readyTaskSlots <= 3 ? 0x6000u :
            readyTask + (uint)readyTaskSlots * 0x100u + 0x1000u;
        var readyName = 0x30000u;
        var waitingName = 0x34000u;
        invocation.TaskListLayout = new(readyList, waitList, readyTask,
            waitingTask, readyName, waitingName, definition.Ready,
            definition.Waiting);

        PrepareTask(invocation.Process, 13, 2, 5, 0x60000, 0x61000,
            invocation.Arguments + 0x800, "Shell");
        // PrepareTask clears the classic Process extension. Restore either a
        // CLI marker or the Workbench startup port expected by the entry.
        if (invocation.Definition.Workbench)
        {
            Bus.Long(invocation.Process +
                (uint)DosLayout.Process.CommandLineInterface, 0);
            Bus.Long(invocation.Process +
                (uint)DosLayout.Process.MessagePort, invocation.Port);
        }
        else
        {
            PrepareCli(invocation.Process, invocation.Arguments + 0xb00,
                invocation.Arguments + 0xb80, "TaskList");
        }
        for (var slot = 0; slot < readyTaskSlots; slot++)
        {
            PrepareTask(readyTask + (uint)slot * 0x100u, 1, 3,
                1 + slot, 0x62000u + (uint)slot * 0x1000u,
                0x62800u + (uint)slot * 0x1000u,
                readyName + (uint)slot * 0x40u, TaskName("Worker", slot));
            if (slot == 0 && definition.ReadyFirstCli)
            {
                var task = readyTask + (uint)slot * 0x100u;
                var cli = invocation.Arguments + 0x500;
                var commandName = cli + 0x40;
                Bus.Memory[task + (uint)ExecLayout.Node.Type] = 13;
                PrepareCli(task, cli, commandName, definition.CliName);
            }
            if (slot < Math.Max(3, definition.Waiting))
            {
                PrepareTask(waitingTask + (uint)slot * 0x100u, 1, 4,
                    -2 - slot, 0x63000u + (uint)slot * 0x1000u,
                    0x63400u + (uint)slot * 0x1000u,
                    waitingName + (uint)slot * 0x40u,
                    TaskName("Waiter", slot));
                if (definition.WaitingDead)
                    Bus.Long(waitingTask + (uint)slot * 0x100u +
                        (uint)ExecLayout.Task.SignalWait, 0);
            }
        }

        InitializeTaskList(ExecBase + (uint)ExecLayout.ExecBase.ResourceList,
            0, 0);
        InitializeTaskList(ExecBase + (uint)ExecLayout.ExecBase.DeviceList,
            0, 0);
        InitializeTaskList(ExecBase + (uint)ExecLayout.ExecBase.LibraryList,
            0, 0);
        InitializeTaskList(ExecBase + (uint)ExecLayout.ExecBase.PortList,
            0, 0);
        InitializeTaskList(readyList, readyTask, definition.Ready);
        InitializeTaskList(waitList, waitingTask, definition.Waiting);
        InitializeTaskList(ExecBase + (uint)ExecLayout.ExecBase.SemaphoreList,
            0, 0);
        if (definition.RegisterCheckScenario is not null)
            invocation.TaskListRegisterCheckAddress =
                PrepareTaskListRegisterCheck(invocation, readyTask,
                    definition.RegisterCheckScenario);
        if (definition.StackTrace)
        {
            var stack = 0x77000u;
            var firstFrame = stack + 0x20u;
            var secondFrame = stack + 0x40u;
            var firstAddress = definition.EmulationFrame ? 0x00240100u :
                definition.Internal ? 0x00300100u : 0x00400100u;
            var secondAddress = definition.Internal ? 0u : 0x00400200u;
            Bus.Long(stack, firstFrame);
            Bus.Long(firstFrame, secondFrame);
            Bus.Long(firstFrame + 4, firstAddress);
            Bus.Long(secondFrame, 0);
            Bus.Long(secondFrame + 4, secondAddress);
            Bus.RegisterNativeReadableRegion(0x77000u, 0x1000u);
            Bus.RegisterNativeReadableRegion(0x400000u, 0x1000u);
        }
        if (definition.SegmentLookup)
        {
            Bus.Long(TaskListSegSemaphore + 46, TaskListSegFindEntry);
            WriteCString(TaskListSegName, "worker");
            Bus.RegisterNativeReadableRegion(TaskListSegSemaphore, 64);
            Bus.RegisterNativeReadableRegion(TaskListSegName, 128);
        }
    }

    private uint PrepareTaskListRegisterCheck(Invocation invocation,
        uint readyTask, string scenario)
    {
        var targetNode = 0x78000u;
        var targetName = 0x78100u;
        switch (scenario)
        {
            case "task-equal":
                return readyTask;
            case "task-offset":
                return readyTask + 4;
            case "process-offset":
                Bus.Memory[readyTask + (uint)ExecLayout.Node.Type] = 13;
                return readyTask + 100;
            case "etask-offset":
                Bus.Long(readyTask +
                    (uint)ExecLayout.Task.TrapAllocated, targetNode);
                return targetNode + 5;
            case "m68k-stack":
                return 0x62020u;
            case "ppc-stack":
                return 0x70020u;
            case "process-stream":
                Bus.Memory[readyTask + (uint)ExecLayout.Node.Type] = 13;
                Bus.Long(readyTask +
                    (uint)DosLayout.Process.CurrentInput,
                    targetNode >> 2);
                return targetNode;
            case "process-output-raw":
                Bus.Memory[readyTask + (uint)ExecLayout.Node.Type] = 13;
                Bus.Long(readyTask + (uint)DosLayout.Process.CurrentOutput,
                    targetNode);
                return targetNode;
            case "process-error-baddr":
                Bus.Memory[readyTask + (uint)ExecLayout.Node.Type] = 13;
                Bus.Long(readyTask + (uint)DosLayout.Process.CurrentError,
                    targetNode >> 2);
                return targetNode;
            case "process-dir-raw":
                Bus.Memory[readyTask + (uint)ExecLayout.Node.Type] = 13;
                Bus.Long(readyTask + (uint)DosLayout.Process.CurrentDirectory,
                    targetNode);
                return targetNode;
            case "process-cli":
                Bus.Memory[readyTask + (uint)ExecLayout.Node.Type] = 13;
                PrepareCli(readyTask, invocation.Arguments + 0x500,
                    invocation.Arguments + 0x540, "Copy");
                return invocation.Arguments + 0x500;
            case "process-cli-raw":
                Bus.Memory[readyTask + (uint)ExecLayout.Node.Type] = 13;
                PrepareCli(readyTask, invocation.Arguments + 0x500,
                    invocation.Arguments + 0x540, "Copy");
                return (invocation.Arguments + 0x500) >> 2;
            case "library-node":
                PrepareRegisterCheckNode(ExecLayout.ExecBase.LibraryList,
                    targetNode, targetName, "test.library");
                return targetNode;
            case "device-node":
                PrepareRegisterCheckNode(ExecLayout.ExecBase.DeviceList,
                    targetNode, targetName, "test.device");
                return targetNode;
            case "resource-node":
                PrepareRegisterCheckNode(ExecLayout.ExecBase.ResourceList,
                    targetNode, targetName, "test.resource");
                return targetNode;
            case "library-function":
                PrepareRegisterCheckNode(ExecLayout.ExecBase.LibraryList,
                    targetNode, targetName, "test.library");
                Bus.Word(targetNode + (uint)ExecLayout.Library.NegativeSize,
                    0x20);
                Bus.Word(targetNode + (uint)ExecLayout.Library.PositiveSize,
                    0x20);
                return targetNode - 4;
            case "library-base":
                PrepareRegisterCheckNode(ExecLayout.ExecBase.LibraryList,
                    targetNode, targetName, "test.library");
                Bus.Word(targetNode + (uint)ExecLayout.Library.PositiveSize,
                    0x20);
                return targetNode + 4;
            case "port":
                PrepareRegisterCheckNode(ExecLayout.ExecBase.PortList,
                    targetNode, targetName, "test.port");
                Bus.Long(targetNode + (uint)ExecLayout.MsgPort.SignalTask,
                    readyTask);
                return targetNode;
            case "semaphore-owner":
                PrepareRegisterCheckNode(ExecLayout.ExecBase.SemaphoreList,
                    targetNode, targetName, "test.semaphore");
                Bus.Long(targetNode +
                    (uint)ExecLayout.SignalSemaphore.Owner, readyTask);
                return targetNode;
            case "semaphore-no-owner":
                PrepareRegisterCheckNode(ExecLayout.ExecBase.SemaphoreList,
                    targetNode, targetName, "test.semaphore");
                return targetNode;
            case "symbol":
                return 0x00400100u;
            case "internal-module":
                return 0x00300100u;
            case "internal-emulation":
                return 0x00240100u;
            default:
                throw new InvalidOperationException(
                    $"Unknown TaskList REGCHECK fixture scenario '{scenario}'.");
        }
    }

    private void PrepareRegisterCheckNode(int listOffset, uint node,
        uint nameAddress, string name)
    {
        Bus.RegisterNativeReadableRegion(node, 0x100);
        Bus.RegisterNativeReadableRegion(nameAddress, 0x100);
        Bus.Memory.AsSpan((int)node, 0x100).Clear();
        Bus.Memory.AsSpan((int)nameAddress, 0x100).Clear();
        var list = ExecBase + (uint)listOffset;
        Bus.Long(list + (uint)ExecLayout.List.Head, node);
        Bus.Long(list + (uint)ExecLayout.List.Tail, 0);
        Bus.Long(list + (uint)ExecLayout.List.TailPred, node);
        Bus.Long(node + (uint)ExecLayout.Node.Successor, list + 4);
        Bus.Long(node + (uint)ExecLayout.Node.Predecessor, list);
        Bus.Long(node + (uint)ExecLayout.Node.Name, nameAddress);
        WriteCString(nameAddress, name);
    }

    private static string TaskName(string stem, int slot) =>
        slot == 0 ? stem : stem + slot;

    private void PrepareCli(uint task, uint cli, uint commandName,
        string name)
    {
        Bus.Long(task + (uint)DosLayout.Process.CommandLineInterface,
            cli >> 2);
        Bus.Long(cli + (uint)DosLayout.CommandLineInterface.CommandName,
            commandName >> 2);
        var bytes = Encoding.Latin1.GetBytes(name);
        var length = Math.Min(bytes.Length, 255);
        Bus.Memory[checked((int)commandName)] = (byte)length;
        bytes.AsSpan(0, length).CopyTo(Bus.Memory.AsSpan(
            checked((int)commandName + 1), length));
    }

    private void PrepareTask(uint task, byte type, byte state, int priority,
        uint lower, uint upper, uint nameAddress, string name)
    {
        Bus.Memory.AsSpan((int)task, 0x180).Clear();
        Bus.Memory[task + (uint)ExecLayout.Node.Type] = type;
        Bus.Memory[task + (uint)ExecLayout.Task.State] = state;
        Bus.Memory[task + (uint)ExecLayout.Node.Priority] = unchecked((byte)priority);
        Bus.Long(task + (uint)ExecLayout.Task.StackLower, lower);
        Bus.Long(task + (uint)ExecLayout.Task.StackUpper, upper);
        Bus.Long(task + (uint)ExecLayout.Task.StackPointer, upper);
        Bus.Long(task + (uint)ExecLayout.Task.SignalWait,
            state == (byte)TaskState.Waiting ? 0x20u : 0u);
        WriteCString(nameAddress, name);
        Bus.Long(task + (uint)ExecLayout.Node.Name, nameAddress);
    }

    private void InitializeTaskList(uint list, uint firstTask, int count)
    {
        Bus.Memory.AsSpan((int)list, 16).Clear();
        var sentinel = list + (uint)ExecLayout.List.Tail;
        Bus.Long(list + (uint)ExecLayout.List.Head,
            count == 0 ? sentinel : firstTask);
        Bus.Long(list + (uint)ExecLayout.List.Tail, 0);
        Bus.Long(list + (uint)ExecLayout.List.TailPred,
            count == 0 ? list : firstTask + (uint)(count - 1) * 0x100u);
        for (var slot = 0; slot < count; slot++)
        {
            var task = firstTask + (uint)slot * 0x100u;
            Bus.Long(task + (uint)ExecLayout.Node.Successor,
                slot + 1 == count ? sentinel : task + 0x100u);
            Bus.Long(task + (uint)ExecLayout.Node.Predecessor,
                slot == 0 ? list : task - 0x100u);
        }
    }

    private void RegisterTaskListEntryExec()
    {
        Register(ExecBase, ExecLvo.NewGetSystemAttrsA,
            "NewGetSystemAttrsA", (state, invocation) =>
            {
                var selectors = new uint[]
                {
                    0x230, 0x231, 0x232, 0x233, 0x219, 0x21A
                };
                Require(state.A[0] != 0 && state.D[0] == 4 &&
                    state.A[1] != 0 && Bus.Long(state.A[1]) == 0 &&
                    Bus.Long(state.A[1] + 4) == 0,
                    "TaskList system-attribute ABI or TAG_DONE list differs.");
                var selectorIndex = invocation.TaskListSystemAttrCalls;
                Require(selectorIndex < selectors.Length &&
                    state.D[1] == selectors[selectorIndex],
                    "TaskList system-attribute selector order differs.");
                var value = state.D[1] switch
                {
                    0x230 => 0x240000u,
                    0x231 => 0x10000u,
                    0x232 => 0x300000u,
                    0x233 => 0x20000u,
                    0x219 => 0xabcdef00u,
                    0x21A => 0xabcdef04u,
                    _ => throw new InvalidOperationException(
                        $"Unexpected TaskList system selector {state.D[1]:x}.")
                };
                Bus.Long(state.A[0], value);
                invocation.TaskListSystemAttrCalls++;
                invocation.TaskListSystemAttrSelectors.Add(state.D[1]);
                invocation.Events.Add("TaskList.SystemAttrs");
                return 4;
            });
        Register(ExecBase, ExecLvo.NewGetTaskAttrsA, "NewGetTaskAttrsA",
            (state, invocation) =>
            {
                if (state.D[1] >= 0x100)
                {
                    var registerTask = state.A[0];
                    Require(registerTask != 0 && state.A[1] != 0 &&
                        Bus.Memory[registerTask + (uint)ExecLayout.Task.State] !=
                            (byte)TaskState.Running && invocation.Forbidden,
                        "TaskList PPC registers were not captured under protection.");
                    var data = state.A[1];
                    switch (state.D[1])
                    {
                        case 0x100:
                        case 0x102:
                        case 0x103:
                        case 0x104:
                        case 0x105:
                            Require(state.D[0] == 4 && state.A[2] == 0,
                                "TaskList PPC scalar register ABI differs.");
                            var scalarValue = RegisterValue(registerTask,
                                state.D[1]);
                            if (invocation.Definition.TaskList!.SegmentLookup &&
                                registerTask ==
                                    invocation.TaskListLayout!.ReadyTask)
                            {
                                scalarValue = state.D[1] switch
                                {
                                    0x100 => 0x00400100u,
                                    0x102 => 0x00400200u,
                                    0x103 => 0x00400100u,
                                    _ => scalarValue
                                };
                            }
                            Bus.Long(data, scalarValue);
                            break;
                        case 0x106:
                        case 0x107:
                            Require(state.D[0] == (state.D[1] == 0x106
                                    ? 128u : 256u) &&
                                state.A[2] != 0 &&
                                Bus.Long(state.A[2]) == 0x80110001u &&
                                Bus.Long(state.A[2] + 4) == 0 &&
                                Bus.Long(state.A[2] + 8) == 0x80110002u &&
                                Bus.Long(state.A[2] + 12) == 32 &&
                                Bus.Long(state.A[2] + 16) == 0 &&
                                Bus.Long(state.A[2] + 20) == 0,
                                "TaskList PPC register-list tags or byte count differ.");
                            for (var index = 0u; index < state.D[0] / 4;
                                 index++)
                            {
                                var registerValue = state.D[1] == 0x106
                                    ? 0x10000000u + index
                                    : 0xf0000000u + index;
                                if (state.D[1] == 0x106 && index == 0 &&
                                    invocation.Definition.TaskList!.RegisterCheck &&
                                    registerTask ==
                                        invocation.TaskListLayout!.ReadyTask)
                                    registerValue =
                                        invocation.TaskListRegisterCheckAddress;
                                if (state.D[1] == 0x106 && index == 1 &&
                                    invocation.Definition.TaskList!.StackTrace &&
                                    registerTask ==
                                        invocation.TaskListLayout!.ReadyTask)
                                    registerValue = 0x77000u;
                                Bus.Long(data + index * 4, registerValue);
                            }
                            break;
                        case 0x109:
                            Require(state.D[0] == 16 && state.A[2] == 0,
                                "TaskList VSCR attribute ABI differs.");
                            for (var index = 0u; index < 4; index++)
                                Bus.Long(data + index * 4,
                                    0x70000000u + index);
                            break;
                        case 0x10B:
                            Require(state.D[0] == 4 && state.A[2] == 0,
                                "TaskList VSAVE attribute ABI differs.");
                            Bus.Long(data, 0x60000000u);
                            break;
                        default:
                            throw new InvalidOperationException(
                                $"Unexpected TaskList PPC attribute {state.D[1]:x}.");
                    }
                    invocation.TaskListRegisterAttrCalls++;
                    invocation.TaskListAttrCalls++;
                    return state.D[0];
                }
                Require(state.A[0] != 0 && state.A[1] != 0 &&
                    state.D[0] == 4 &&
                    state.A[2] == 0 && invocation.Forbidden,
                    "TaskList task-attribute ABI differs.");
                var layout = invocation.TaskListLayout!;
                var task = state.A[0];
                var lower = Bus.Long(task +
                    (uint)ExecLayout.Task.StackLower);
                var upper = Bus.Long(task +
                    (uint)ExecLayout.Task.StackUpper);
                uint value = state.D[1] switch
                {
                    0x02 => unchecked((uint)(int)(sbyte)Bus.Memory[task +
                        (uint)ExecLayout.Node.Priority]),
                    0x03 => Bus.Memory[task + (uint)ExecLayout.Node.Type],
                    0x04 => Bus.Memory[task + (uint)ExecLayout.Task.State],
                    0x07 => Bus.Long(task +
                        (uint)ExecLayout.Task.SignalWait),
                    0x08 => 0x40u,
                    0x09 => 0x80u,
                    0x0E => upper >= lower ? upper - lower : 0u,
                    0x0F => 32768u,
                    0x10 => invocation.Definition.TaskList!.UnknownStackUsed ? uint.MaxValue : 128u,
                    0x11 => 512u,
                    0x24 => TaskListPid(task, invocation.Process, layout),
                    0x26 => PpcStackBounds(task, layout).Lower,
                    0x27 => PpcStackBounds(task, layout).Upper,
                    0x28 => lower,
                    0x29 => upper,
                    _ => throw new InvalidOperationException(
                        $"Unexpected TaskList attribute selector {state.D[1]:x}.")
                };
                Bus.Long(state.A[1], value);
                if (invocation.Definition.TaskList!.RegisterCheck &&
                    invocation.TaskListVPrintfCalls > 0 &&
                    (state.D[1] == 0x26 || state.D[1] == 0x27))
                    invocation.TaskListRegisterCheckPpcBoundsCalls++;
                invocation.TaskListAttrCalls++;
                if (state.D[1] == 0x03)
                    invocation.TaskListTypeAttrCalls++;
                if (state.D[1] == 0x24)
                    invocation.TaskListPidAttrCalls++;
                // Exercise the documented classic-field fallback for the
                // signal-wait selector; all other public selectors succeed.
                return state.D[1] == 0x07 ? 0u : 1u;
            });
        Register(ExecBase, ExecLvo.TypeOfMem, "TypeOfMem", (state,
            invocation) =>
        {
            Require(invocation.Forbidden ||
                invocation.Definition.TaskList!.RegisterCheck,
                "TaskList validated a register pointer outside its supported path.");
            var address = state.A[1];
            invocation.TaskListTypeOfMemCalls++;
            if (address >= 0x240000u && address < 0x250000u ||
                address >= 0x300000u && address < 0x320000u)
                return 0;
            return address != 0 && address < Bus.Memory.Length ? 1u : 0u;
        });
        Register(ExecBase, ExecLvo.FindSemaphore, "FindSemaphore", (state,
            invocation) =>
        {
            Require(Bus.CString(state.A[1]) == "SegTracker" &&
                invocation.Forbidden,
                "TaskList SegTracker lookup ABI or protection differs.");
            return invocation.Definition.TaskList!.SegmentLookup
                ? TaskListSegSemaphore : 0;
        });
        Register(ExecBase, ExecLvo.AttemptSemaphoreShared,
            "AttemptSemaphoreShared", (state, invocation) =>
        {
            Require(state.A[0] == TaskListSegSemaphore &&
                invocation.Forbidden &&
                invocation.Definition.TaskList!.SegmentLookup,
                "TaskList SegTracker shared-lock ABI differs.");
            invocation.TaskListSegTrackerAcquireCalls++;
            invocation.TaskListSegTrackerHeld = true;
            return 1;
        });
        Register(ExecBase, ExecLvo.ReleaseSemaphore, "ReleaseSemaphore",
            (state, invocation) =>
        {
            Require(state.A[0] == TaskListSegSemaphore &&
                invocation.TaskListSegTrackerHeld &&
                (invocation.Forbidden ||
                    invocation.Definition.TaskList!.RegisterCheck),
                "TaskList SegTracker shared-lock release differs.");
            invocation.TaskListSegTrackerReleaseCalls++;
            invocation.TaskListSegTrackerHeld = false;
            return 0;
        });
        Bus.RegisterGateway(TaskListSegFindEntry, state =>
        {
            var invocation = Bus.Current ?? throw new InvalidOperationException(
                "SegTracker callback has no active task.");
            Require((invocation.Definition.TaskList!.SegmentLookup &&
                    invocation.Forbidden ||
                invocation.Definition.TaskList!.RegisterCheck &&
                    invocation.TaskListSegTrackerHeld &&
                    !invocation.Forbidden) && state.A[0] != 0 &&
                state.A[1] != 0 && state.A[2] != 0,
                "TaskList seg_Find register contract differs.");
            invocation.TaskListSegTrackerFindCalls++;
            if (state.A[0] == 0x00400100u)
            {
                _ = Bus.OwnedAllocationContaining(invocation, state.A[1],
                    "TaskListBuffer");
                _ = Bus.OwnedAllocationContaining(invocation, state.A[2],
                    "TaskListBuffer");
                Require(state.A[1] != 0 && state.A[2] != 0 &&
                    state.A[1] != 0x00400100u && state.A[2] != 0x00400100u,
                    $"SegTracker output registers overlap the address (A0=${state.A[0]:X8}, A1=${state.A[1]:X8}, A2=${state.A[2]:X8}, A3=${state.A[3]:X8}).");
                Bus.Long(state.A[1], 7);
                Bus.Long(state.A[2], 0x100);
                state.D[0] = TaskListSegName;
            }
            else
                state.D[0] = 0;
        });
    }

    private static uint TaskListPid(uint task, uint process,
        TaskListNativeLayout layout) => task == process ? 1u :
        task >= layout.ReadyTask && task < layout.ReadyTask +
            (uint)layout.ReadyCount * 0x100u
            ? 2u + (task - layout.ReadyTask) / 0x100u
            : 5u + (task - layout.WaitingTask) / 0x100u;

    private static (uint Lower, uint Upper) PpcStackBounds(uint task,
        TaskListNativeLayout layout)
    {
        var offset = task >= layout.ReadyTask &&
            task < layout.ReadyTask + (uint)layout.ReadyCount * 0x100u
                ? (task - layout.ReadyTask) / 0x100u * 0x100u
                : task >= layout.WaitingTask &&
                    task < layout.WaitingTask +
                        (uint)layout.WaitingCount * 0x100u
                    ? 0x300u + (task - layout.WaitingTask) / 0x100u * 0x100u
                    : 0x600u;
        var lower = 0x70000u + offset * 0x100u;
        return (lower, lower + 32768u);
    }

    private void RegisterTaskListEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.TaskList!;
            Require(Bus.CString(state.D[1]) ==
                "NAME,ADDRESS/N,VERBOSE/S,STACKTRACE/S,STACKLEVEL/N,NORUN/S,NOWAIT/S,NOREADY/S,INTERNAL/S,REGDUMP/S,REGCHECK/S" && state.D[3] == 0 &&
                state.D[2] % 4 == 0 && Bus.OwnedAllocation(invocation,
                    state.D[2], "Exec").Size == 44,
                "TaskList ReadArgs ABI differs.");
            invocation.TaskListReadArgsCalls++;
            invocation.Events.Add("TaskList.ReadArgs");
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 40, "RDArgs", true);
            if (definition.Name is not null)
            {
                var name = invocation.Arguments + 0x900;
                WriteCString(name, definition.Name);
                Bus.Long(state.D[2], name);
            }
            if (definition.Address is uint address)
            {
                var value = invocation.Arguments + 0x940;
                Bus.Long(value, address);
                Bus.Long(state.D[2] + 4, value);
            }
            if (definition.Verbose) Bus.Long(state.D[2] + 8, uint.MaxValue);
            if (definition.StackTrace)
                Bus.Long(state.D[2] + 12, uint.MaxValue);
            if (definition.StackLevel is int stackLevel)
            {
                var value = invocation.Arguments + 0x980;
                Bus.Long(value, unchecked((uint)stackLevel));
                Bus.Long(state.D[2] + 16, value);
            }
            if (definition.NoRun) Bus.Long(state.D[2] + 20, uint.MaxValue);
            if (definition.NoWait) Bus.Long(state.D[2] + 24, uint.MaxValue);
            if (definition.NoReady) Bus.Long(state.D[2] + 28, uint.MaxValue);
            if (definition.RegisterDump)
                Bus.Long(state.D[2] + 36, uint.MaxValue);
            if (definition.Internal) Bus.Long(state.D[2] + 32, uint.MaxValue);
            if (definition.RegisterCheck) Bus.Long(state.D[2] + 40,
                uint.MaxValue);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.TaskListFreeArgsCalls++;
            invocation.Events.Add("TaskList.FreeArgs");
            invocation.IoError = 901;
            return 0xf4ee;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            var text = Bus.CString(state.D[2]);
            Require(state.D[1] == invocation.OutputBptr &&
                (text == TaskListHeader ||
                    text == "Not Enough memory for task buffer\n" ||
                    text.StartsWith("     GPR[", StringComparison.Ordinal)),
                "TaskList header output differs.");
            if (text.StartsWith("     GPR[", StringComparison.Ordinal))
            {
                Require(invocation.Definition.TaskList!.RegisterCheck,
                    "TaskList emitted a register classification without REGCHECK.");
                invocation.TaskListRegisterCheckRows++;
            }
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.TaskListFPutsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state,
            invocation) =>
        {
            var format = Bus.CString(state.D[1]);
            var verboseFormat = format switch
            {
                ":     State %ld\n" => string.Concat(":     State ",
                    Bus.Long(state.D[2]).ToString(
                        CultureInfo.InvariantCulture), "\n"),
                ":   SigWait 0x%08lx\n" => TaskListVerboseHex(
                    ":   SigWait 0x", Bus.Long(state.D[2])),
                ": SigExcept 0x%08lx\n" => TaskListVerboseHex(
                    ": SigExcept 0x", Bus.Long(state.D[2])),
                ":  SigRecvd 0x%08lx\n" => TaskListVerboseHex(
                    ":  SigRecvd 0x", Bus.Long(state.D[2])),
                ":M68k SPUpper 0x%08lx\n" => TaskListVerboseHex(
                    ":M68k SPUpper 0x", Bus.Long(state.D[2])),
                ":M68k SPLower 0x%08lx\n" => TaskListVerboseHex(
                    ":M68k SPLower 0x", Bus.Long(state.D[2])),
                ":M68k  SPReg 0x%08lx\n" => TaskListVerboseHex(
                    ":M68k  SPReg 0x", Bus.Long(state.D[2])),
                ": PPC SPUpper 0x%08lx\n" => TaskListVerboseHex(
                    ": PPC SPUpper 0x", Bus.Long(state.D[2])),
                ": PPC SPLower 0x%08lx\n" => TaskListVerboseHex(
                    ": PPC SPLower 0x", Bus.Long(state.D[2])),
                _ => null
            };
            if (verboseFormat is not null)
            {
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    verboseFormat));
                invocation.TaskListVerboseVPrintfCalls++;
                return 0;
            }
            if (format ==
                "     SRR0 0x%08lx LR 0x%08lx CTR 0x%lx CR 0x%lx XER 0x%lx\n")
            {
                var headerFields = state.D[2];
                var line = RegisterHeaderOutput(Bus.Long(headerFields),
                    Bus.Long(headerFields + 4), Bus.Long(headerFields + 8),
                    Bus.Long(headerFields + 12), Bus.Long(headerFields + 16));
                invocation.Output.Write(Encoding.Latin1.GetBytes(line));
                invocation.TaskListRegisterVPrintfCalls++;
                return 0;
            }
            if (format ==
                "     GPR[%02ld] 0x%08lx 0x%08lx 0x%08lx 0x%08lx 0x%08lx 0x%08lx 0x%08lx 0x%08lx\n")
            {
                var groupFields = state.D[2];
                var line = new StringBuilder("     GPR[")
                    .Append(Bus.Long(groupFields).ToString("D2",
                        CultureInfo.InvariantCulture)).Append("]");
                for (var index = 0u; index < 8; index++)
                    line.Append(" 0x").Append(Bus.Long(groupFields + 4 +
                        index * 4).ToString("x8",
                        CultureInfo.InvariantCulture));
                line.Append('\n');
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    line.ToString()));
                invocation.TaskListRegisterVPrintfCalls++;
                return 0;
            }
            if (format ==
                "%s -> Address 0x%08lx -> %s Hunk %ld Offset 0x%08lx\n")
            {
                var stackFields = state.D[2];
                var line = Bus.CString(Bus.Long(stackFields)) +
                    " -> Address 0x" + Bus.Long(stackFields + 4).ToString(
                        "x8", CultureInfo.InvariantCulture) + " -> " +
                    Bus.CString(Bus.Long(stackFields + 8)) + " Hunk " +
                    unchecked((int)Bus.Long(stackFields + 12)).ToString(
                        CultureInfo.InvariantCulture) + " Offset 0x" +
                    Bus.Long(stackFields + 16).ToString("x8",
                        CultureInfo.InvariantCulture) + "\n";
                invocation.Output.Write(Encoding.Latin1.GetBytes(line));
                return 0;
            }
            if (format ==
                "     StackFrame[%2ld].LR -> Address 0x%08lx -> %s Hunk %ld Offset 0x%08lx\n")
            {
                var stackFields = state.D[2];
                var line = "     StackFrame[" + Bus.Long(stackFields).ToString(
                    CultureInfo.InvariantCulture).PadLeft(2) +
                    "].LR -> Address 0x" + Bus.Long(stackFields + 4).ToString(
                        "x8", CultureInfo.InvariantCulture) + " -> " +
                    Bus.CString(Bus.Long(stackFields + 8)) + " Hunk " +
                    unchecked((int)Bus.Long(stackFields + 12)).ToString(
                        CultureInfo.InvariantCulture) + " Offset 0x" +
                    Bus.Long(stackFields + 16).ToString("x8",
                        CultureInfo.InvariantCulture) + "\n";
                invocation.Output.Write(Encoding.Latin1.GetBytes(line));
                return 0;
            }
            if (format ==
                "     StackFrame[%2ld].LR -> Address 0x%08lx\n")
            {
                var stackFields = state.D[2];
                var line = TaskListStackFrameLine(Bus.Long(stackFields),
                    Bus.Long(stackFields + 4));
                invocation.Output.Write(Encoding.Latin1.GetBytes(line));
                return 0;
            }
            if (format ==
                "     StackFrame[%2ld].LR -> Address 0x%08lx -> %s 0x%lx\n")
            {
                var stackFields = state.D[2];
                var line = TaskListStackFrameLine(Bus.Long(stackFields),
                    Bus.Long(stackFields + 4),
                    Bus.CString(Bus.Long(stackFields + 8)),
                    Bus.Long(stackFields + 16));
                invocation.Output.Write(Encoding.Latin1.GetBytes(line));
                return 0;
            }
            var cliRow = format.Contains(" [%s]", StringComparison.Ordinal);
            var cliPid = format.StartsWith("%6lu", StringComparison.Ordinal);
            var numericUsed = !format.Contains("%8lu/%s",
                StringComparison.Ordinal);
            var expectedFormat = cliRow
                ? numericUsed
                    ? "%6lu 0x%08lx %s %4ld %s %8lu/%-8lu %8lu/%-8lu %s [%s]\n"
                    : "%6lu 0x%08lx %s %4ld %s %8lu/%s %8lu/%-8lu %s [%s]\n"
                : numericUsed
                    ? "%4lu 0x%08lx %s %4ld %s %8lu/%-8lu %8lu/%-8lu %s\n"
                    : "%4lu 0x%08lx %s %4ld %s %8lu/%s %8lu/%-8lu %s\n";
            Require(cliRow == cliPid && format == expectedFormat &&
                numericUsed != invocation.Definition.TaskList!.UnknownStackUsed,
                "TaskList row format differs.");
            var fields = state.D[2];
            var pid = Bus.Long(fields);
            var address = Bus.Long(fields + 4);
            var type = Bus.CString(Bus.Long(fields + 8));
            var priority = unchecked((int)Bus.Long(fields + 12));
            var taskState = Bus.CString(Bus.Long(fields + 16));
            var stack = Bus.Long(fields + 20);
            var used = numericUsed
                ? Bus.Long(fields + 24).ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                : Bus.CString(Bus.Long(fields + 24));
            var ppcStack = Bus.Long(fields + 28);
            var ppcUsed = Bus.Long(fields + 32);
            var name = Bus.CString(Bus.Long(fields + 36));
            var cliName = cliRow ? Bus.CString(Bus.Long(fields + 40)) : null;
            if (cliRow)
                Require(type == " cli" && cliName == (pid == 1
                    ? "TaskList" : invocation.Definition.TaskList!.CliName),
                    "TaskList CLI row name or classification differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(Row(pid, address,
                type, priority, taskState, stack, name, cliName,
                used, ppcStack, ppcUsed)));
            Require(numericUsed ? used == "128" : used == "???     ",
                "TaskList stack-used value differs.");
            invocation.TaskListVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state,
            invocation) =>
        {
            Require(state.D[2] == 0, "TaskList PrintFault header differs.");
            invocation.TaskListPrintFaultCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state,
            invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private static string TaskListVerboseHex(string prefix, uint value) =>
        prefix + value.ToString("x8", CultureInfo.InvariantCulture) + "\n";

    private void VerifyTaskListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.TaskList!;
        var boundary = invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos;
        if (boundary)
        {
            Require(invocation.TaskListReadArgsCalls == 0 &&
                invocation.TaskListAllocVecCalls == 0 &&
                invocation.TaskListSysDebugOpenCalls == 0 &&
                invocation.TaskListSysDebugCloseCalls == 0,
                "TaskList crossed an invalid startup boundary.");
            return;
        }
        if (definition.SysDebugUnavailable)
        {
            Require(invocation.TaskListSysDebugOpenCalls == 1 &&
                invocation.TaskListSysDebugCloseCalls == 0 &&
                invocation.TaskListReadArgsCalls == 0 &&
                invocation.TaskListAllocVecCalls == 0 &&
                invocation.TaskListPrintFaultCalls == 1 &&
                invocation.Events.Contains("TaskList.OpenSysDebug") &&
                !invocation.Events.Contains("TaskList.ReadArgs"),
                "TaskList continued after the sysdebug.library startup failure.");
            return;
        }
        var parser = definition.ParserError != 0;
        var sysdebugOpen = invocation.Events.IndexOf("TaskList.OpenSysDebug");
        var readArgs = invocation.Events.IndexOf("TaskList.ReadArgs");
        var freeArgs = invocation.Events.IndexOf("TaskList.FreeArgs");
        var sysdebugClose = invocation.Events.IndexOf("TaskList.CloseSysDebug");
        Require(invocation.TaskListSysDebugOpenCalls == 1 &&
            invocation.TaskListSysDebugCloseCalls == 1 &&
            sysdebugOpen >= 0 && readArgs > sysdebugOpen &&
            sysdebugClose > readArgs &&
            (parser ? freeArgs < 0 : freeArgs > readArgs &&
                sysdebugClose > freeArgs),
            "TaskList sysdebug.library lease order or release differs.");
        Require(invocation.TaskListReadArgsCalls == 1 &&
            invocation.TaskListFreeArgsCalls == (parser ? 0 : 1),
            "TaskList parser ownership differs.");
        if (parser) return;
        var systemAttrsFirst = invocation.Events.IndexOf(
            "TaskList.SystemAttrs");
        var systemAttrsLast = invocation.Events.LastIndexOf(
            "TaskList.SystemAttrs");
        Require(invocation.TaskListSystemAttrCalls == 6 &&
            invocation.TaskListSystemAttrSelectors.SequenceEqual(new uint[]
                { 0x230, 0x231, 0x232, 0x233, 0x219, 0x21A }) &&
            systemAttrsFirst > readArgs && systemAttrsLast < freeArgs,
            "TaskList system-boundary attributes or ordering differ.");
        var currentMatchesName = definition.Name is null or "Shell" or "TaskList";
        var currentMatchesAddress = definition.Address is null or 0x10000;
        var currentIncluded = !definition.NoRun && currentMatchesName &&
            currentMatchesAddress;
        if (definition.AllocationFailure)
        {
            Require(invocation.TaskListAllocVecCalls == 1 &&
                invocation.TaskListFreeVecCalls == 0 &&
                invocation.TaskListFPutsCalls == 1,
                "TaskList allocation failure lifecycle differs.");
            return;
        }
        var expectedCandidates = (definition.NoRun ? 0 : 1) +
            (definition.NoReady ? 0 : definition.Ready) +
            (definition.NoWait ? 0 : definition.Waiting);
        var growsBuffer = definition.Ready > TaskListBufferCapacity;
        var snapshotAttempts = growsBuffer ? 2 : 1;
        var expectedTypeAttrCalls = expectedCandidates +
            (growsBuffer ? TaskListBufferCapacity : 0);
        var expectedPidAttrCalls = (definition.CtrlC
            ? expectedCandidates : invocation.TaskListVPrintfCalls) +
            (growsBuffer ? TaskListBufferCapacity : 0);
        var expectedNonRunningCaptures = expectedPidAttrCalls -
            (currentIncluded ? snapshotAttempts : 0);
        var expectedRegisterLines = definition.RegisterDump
            ? 5 * (invocation.TaskListVPrintfCalls -
                (currentIncluded ? 1 : 0)) : 0;
        var expectedVerboseLines = definition.Verbose
            ? 9 * invocation.TaskListVPrintfCalls : 0;
        Require(invocation.TaskListAllocVecCalls == snapshotAttempts &&
            invocation.TaskListFreeVecCalls == snapshotAttempts &&
            invocation.TaskListForbidCalls == snapshotAttempts +
                invocation.TaskListRegisterCheckForbidCalls &&
            invocation.TaskListPermitCalls == snapshotAttempts +
                invocation.TaskListRegisterCheckPermitCalls &&
            invocation.TaskListRegisterCheckForbidCalls ==
                invocation.TaskListRegisterCheckPermitCalls &&
            invocation.TaskListTypeAttrCalls == expectedTypeAttrCalls &&
            invocation.TaskListAttrCalls ==
                invocation.TaskListTypeAttrCalls +
                    14 * expectedPidAttrCalls +
                    9 * expectedNonRunningCaptures +
                    invocation.TaskListRegisterCheckPpcBoundsCalls &&
            invocation.TaskListRegisterAttrCalls ==
                9 * expectedNonRunningCaptures &&
            invocation.TaskListRegisterVPrintfCalls ==
                expectedRegisterLines &&
            invocation.TaskListVerboseVPrintfCalls ==
                expectedVerboseLines &&
            invocation.TaskListPidAttrCalls == expectedPidAttrCalls &&
            invocation.TaskListFPutsCalls == 1 +
                invocation.TaskListRegisterCheckRows &&
            invocation.TaskListPrintFaultCalls == (definition.CtrlC ? 1 : 0),
            $"TaskList snapshot/output lifecycle differs (alloc={invocation.TaskListAllocVecCalls}/{snapshotAttempts}, forbid={invocation.TaskListForbidCalls}/{invocation.TaskListPermitCalls}, regchecklock={invocation.TaskListRegisterCheckForbidCalls}/{invocation.TaskListRegisterCheckPermitCalls}, typeattrs={invocation.TaskListTypeAttrCalls}/{expectedTypeAttrCalls}, ready={definition.Ready}, waiting={definition.Waiting}, noRun={definition.NoRun}, noReady={definition.NoReady}, noWait={definition.NoWait}, candidates={expectedCandidates}, capacity={TaskListBufferCapacity}, grows={growsBuffer}, pidattrs={invocation.TaskListPidAttrCalls}/{expectedPidAttrCalls}, attrs={invocation.TaskListAttrCalls}/{invocation.TaskListTypeAttrCalls + 14 * expectedPidAttrCalls + 9 * expectedNonRunningCaptures + invocation.TaskListRegisterCheckPpcBoundsCalls}, regattrs={invocation.TaskListRegisterAttrCalls}/{9 * expectedNonRunningCaptures}, regbounds={invocation.TaskListRegisterCheckPpcBoundsCalls}, reglines={invocation.TaskListRegisterVPrintfCalls}/{expectedRegisterLines}, verbose={invocation.TaskListVerboseVPrintfCalls}/{expectedVerboseLines}, rows={invocation.TaskListVPrintfCalls}, header={invocation.TaskListFPutsCalls}/{1 + invocation.TaskListRegisterCheckRows}, regrows={invocation.TaskListRegisterCheckRows}, faults={invocation.TaskListPrintFaultCalls}, ctrlc={definition.CtrlC}).");
        Require(!definition.StackTrace ||
            invocation.TaskListTypeOfMemCalls > 0,
            "TaskList STACKTRACE did not validate captured addresses.");
        if (definition.RegisterCheck)
            Require(invocation.TaskListRegisterCheckRows > 0 &&
                invocation.TaskListRegisterCheckPpcBoundsCalls > 0 &&
                invocation.TaskListRegisterCheckPpcBoundsCalls % 2 == 0 &&
                invocation.TaskListRegisterCheckForbidCalls ==
                    invocation.TaskListRegisterCheckPermitCalls,
                "TaskList REGCHECK did not perform its protected full-register scan.");
        if (definition.SegmentLookup && definition.StackTrace)
            Require(invocation.TaskListSegTrackerAcquireCalls == 1 &&
                invocation.TaskListSegTrackerReleaseCalls == 1 &&
                invocation.TaskListSegTrackerFindCalls == 5,
                "TaskList SegTracker lock, callback count or release differs.");
        else if (definition.SegmentLookup && definition.RegisterCheck)
            Require(invocation.TaskListSegTrackerAcquireCalls == 1 &&
                invocation.TaskListSegTrackerReleaseCalls == 1 &&
                invocation.TaskListSegTrackerFindCalls == 1 &&
                !invocation.TaskListSegTrackerHeld,
                "TaskList REGCHECK SegTracker lock or callback differs.");
    }
}
