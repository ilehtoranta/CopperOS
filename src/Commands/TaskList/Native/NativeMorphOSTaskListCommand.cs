using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-observed MorphOS TaskList command boundary.  The standard task
/// snapshot uses public Exec task lists and MorphOS task attributes.  Stack
/// frames are captured from stopped tasks under Forbid and formatted after
/// Permit. REGCHECK classifies saved GPRs using source-ordered Exec, task,
/// process, port, semaphore, INTERNAL, and SegTracker paths.
/// </summary>
public static class NativeMorphOSTaskListCommand
{
    public const string Template =
        "NAME,ADDRESS/N,VERBOSE/S,STACKTRACE/S,STACKLEVEL/N,NORUN/S,NOWAIT/S,NOREADY/S,INTERNAL/S,REGDUMP/S,REGCHECK/S";
    public const uint ResultCount = 11;

    private const uint RecordBytes = 1552;
    private const uint StackFrameEntryBytes = 148;
    private const uint StackFrameNameBytes = 128;
    private const uint StackFramePrefixEntries = 3;
    private const uint DefaultStackLevel = 30;
    private const uint PpcStackScanBytes = 8192;
    private const uint RegisterCheckScratchBytes = 1024;
    // sizeof(struct ETask) for the pinned MorphOS SDK's #pragma pack(2)
    // declaration, used by the captured stackdump.c source. This is the
    // compiled source extent, not a claim about the private allocation size.
    private const uint MorphOSETaskCompiledSize = 114;
    private const uint SegTrackerFunctionOffset = 46;
    private const uint InitialBufferBytes = 128 * 1024;
    private const uint CtrlCMask = 1u << 12;
    private const uint NtProcess = 13;
    private const int PidOffset = 4;
    private const int TypeOffset = 8;
    private const int StateOffset = 12;
    private const int PriorityOffset = 16;
    private const int M68kStackSizeOffset = 20;
    private const int M68kStackUsedOffset = 24;
    private const int NamePointerOffset = 28;
    private const int NameOffset = 32;
    private const int CliNamePointerOffset = 288;
    private const int CliNameOffset = 292;
    private const int PpcStackSizeOffset = 548;
    private const int PpcStackUsedOffset = 552;
    private const int M68kStackPointerOffset = 556;
    private const int M68kStackUpperOffset = 560;
    private const int M68kStackLowerOffset = 564;
    private const int PpcStackUpperOffset = 568;
    private const int PpcStackLowerOffset = 572;
    private const int SignalWaitOffset = 576;
    private const int SignalReceivedOffset = 580;
    private const int SignalExceptionOffset = 584;
    private const int PpcSrr0Offset = 588;
    private const int PpcLrOffset = 592;
    private const int PpcCtrOffset = 596;
    private const int PpcCrOffset = 600;
    private const int PpcXerOffset = 604;
    private const int PpcGprOffset = 608;
    private const int PpcFprOffset = 736;
    private const int PpcVscrOffset = 1504;
    private const int PpcAltivecOffset = 1520;
    private const int PpcVsaveOffset = 1524;
    private const int RegisterTagsOffset = 1528;
    private const int StackFrameCountOffset = RegisterTagsOffset;
    private const int StackAddressValidMaskOffset = RegisterTagsOffset + 4;
    private const uint PpcRegisterBytes = 32 * 4;
    private const uint PpcFloatingRegisterBytes = 32 * 8;
    private const uint TaskInfoTagDummy = 0x80110000;
    private const uint SystemInfoTaskExitCode = 0x219;
    private const uint SystemInfoTaskExitCodeM68k = 0x21A;
    private const uint SystemInfoEmulationStart = 0x230;
    private const uint SystemInfoEmulationSize = 0x231;
    private const uint SystemInfoModuleStart = 0x232;
    private const uint SystemInfoModuleSize = 0x233;
    private const uint TraceSrr0Index = 0;
    private const uint TraceLrIndex = 1;
    private const uint TraceCtrIndex = 2;

    [M68kImport("intrinsic:m68k-read-stack-pointer")]
    private static extern uint ReadCurrentStackPointer();

    // Public MorphOS Exec TASKINFOTYPE selectors from the MorphOS SDK headers.
    private enum TaskAttribute : uint
    {
        Priority = 0x02,
        Type = 0x03,
        State = 0x04,
        SignalWait = 0x07,
        SignalReceived = 0x08,
        SignalException = 0x09,
        StackSizeM68k = 0x0E,
        StackSizePpc = 0x0F,
        UsedStackSizeM68k = 0x10,
        UsedStackSizePpc = 0x11,
        ProcessIdCli = 0x24,
        PpcStackLower = 0x26,
        PpcStackUpper = 0x27,
        M68kStackLower = 0x28,
        M68kStackUpper = 0x29,
        PpcSrr0 = 0x100,
        PpcLr = 0x102,
        PpcCtr = 0x103,
        PpcCr = 0x104,
        PpcXer = 0x105,
        PpcGpr = 0x106,
        PpcFpr = 0x107,
        PpcVscr = 0x109,
        PpcVsave = 0x10B,
    }

    private struct OutputFields
    {
        public uint Pid;
        public uint Address;
        public uint Type;
        public uint Priority;
        public uint State;
        public uint StackSize;
        public uint StackUsed;
        public uint PpcStackSize;
        public uint PpcStackUsed;
        public uint Name;
        public uint CliName;

        public static APTR AddressOf(ref OutputFields fields) =>
            throw new System.NotSupportedException(
                "TaskList.OutputFields.AddressOf is lowered by CopperSharp.");
    }

    private struct RegisterHeaderFields
    {
        public uint Srr0;
        public uint Lr;
        public uint Ctr;
        public uint Cr;
        public uint Xer;

        public static APTR AddressOf(ref RegisterHeaderFields fields) =>
            throw new System.NotSupportedException(
                "TaskList.RegisterHeaderFields.AddressOf is lowered by CopperSharp.");
    }

    private struct RegisterGroupFields
    {
        public uint Start;
        public uint R0;
        public uint R1;
        public uint R2;
        public uint R3;
        public uint R4;
        public uint R5;
        public uint R6;
        public uint R7;

        public static APTR AddressOf(ref RegisterGroupFields fields) =>
            throw new System.NotSupportedException(
                "TaskList.RegisterGroupFields.AddressOf is lowered by CopperSharp.");
    }

    private struct SystemBoundaryFields
    {
        public uint EmulationStart;
        public uint EmulationSize;
        public uint ModuleStart;
        public uint ModuleSize;
        public uint TaskExitCode;
        public uint TaskExitCodeM68k;

        public static APTR AddressOf(ref SystemBoundaryFields fields) =>
            throw new System.NotSupportedException(
                "TaskList.SystemBoundaryFields.AddressOf is lowered by CopperSharp.");
    }

    private struct StackAddressFields
    {
        public uint Prefix;
        public uint Address;
        public uint Name;
        public uint Segment;
        public uint Offset;

        public static APTR AddressOf(ref StackAddressFields fields) =>
            throw new System.NotSupportedException(
                "TaskList.StackAddressFields.AddressOf is lowered by CopperSharp.");
    }

    private struct StackFrameFields
    {
        public uint Index;
        public uint Address;
        public uint Name;
        public uint Segment;
        public uint Offset;

        public static APTR AddressOf(ref StackFrameFields fields) =>
            throw new System.NotSupportedException(
                "TaskList.StackFrameFields.AddressOf is lowered by CopperSharp.");
    }

    private struct TagDoneFields
    {
        public uint Tag;
        public uint Data;

        public static APTR AddressOf(ref TagDoneFields fields) =>
            throw new System.NotSupportedException(
                "TaskList.TagDoneFields.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Parses the complete source template, snapshots current/ready/waiting
    /// tasks while Exec is disabled, then formats after protection is released.
    /// Name/address filters and NORUN/NOWAIT/NOREADY are supported.  The
    /// Public task attributes back VERBOSE and REGDUMP. STACKTRACE follows the
    /// source PPC backchain for stopped tasks, validates addresses through
    /// TypeOfMem and captured INTERNAL ranges, and uses the documented legacy
    /// SegTracker function pointer when its semaphore is available. REGCHECK
    /// classifies saved GPRs using the source's symbol, Exec-list, task, port,
    /// and semaphore scans.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        var sysdebug = Exec.OpenLibraryRaw("sysdebug.library", 0);
        if (sysdebug.IsNull)
        {
            ioError = (int)DOS.IoErr();
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            Exec.CloseLibrary(sysdebug);
            return arguments.ReturnLevel;
        }

        var buffer = APTR.Null;
        var result = DOS.RETURN_OK;
        var error = 0;
        do
        {
            var name = ReadPointer(ref arguments, 0);
            var address = ReadNumber(ref arguments, 1, out var hasAddress);
            var verbose = ReadSwitch(ref arguments, 2);
            var stackTrace = ReadSwitch(ref arguments, 3);
            var requestedStackLevel = ReadNumber(ref arguments, 4, out var hasStackLevel);
            if (requestedStackLevel < 0)
            {
                error = (int)DOS.Error.BadNumber;
                break;
            }
            var stackLevel = hasStackLevel
                ? unchecked((uint)requestedStackLevel) : DefaultStackLevel;
            var recordStride = RecordBytes;
            if (stackTrace != 0 && !TryGetRecordStride(stackLevel,
                    out recordStride))
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }
            var noRun = ReadSwitch(ref arguments, 5) != 0;
            var noWait = ReadSwitch(ref arguments, 6) != 0;
            var noReady = ReadSwitch(ref arguments, 7) != 0;
            var internalMode = ReadSwitch(ref arguments, 8);
            var registerDump = ReadSwitch(ref arguments, 9);
            var registerCheck = ReadSwitch(ref arguments, 10);

            var systemBoundaries = new SystemBoundaryFields
            {
                EmulationStart = 0,
                EmulationSize = 0,
                ModuleStart = 0,
                ModuleSize = 0,
                TaskExitCode = 0,
                TaskExitCodeM68k = 0
            };
            var systemBoundariesPointer =
                SystemBoundaryFields.AddressOf(ref systemBoundaries);
            var tagDoneFields = new TagDoneFields { Tag = 0, Data = 0 };
            var tagDone = TagDoneFields.AddressOf(ref tagDoneFields);
            CaptureSystemBoundaries(systemBoundariesPointer, tagDone);

            var bufferBytes = InitialBufferBytes;
            var count = 0u;
            var snapshotComplete = false;
            var registerCheckSegTracker = APTR.Null;
            var registerCheckSegTrackerLocked = false;
            while (!snapshotComplete)
            {
                buffer = Exec.AllocVec(bufferBytes,
                    (uint)Exec.MemoryFlags.Any);
                if (buffer.IsNull)
                {
                    DOS.FPuts(DOS.Output(),
                        "Not Enough memory for task buffer\n");
                    result = DOS.RETURN_FAIL;
                    break;
                }

                count = 0;
                var fits = true;
                var snapshotCapacityBytes = registerCheck != 0
                    ? bufferBytes - RegisterCheckScratchBytes
                    : bufferBytes;
                // NewGetTaskAttrsA does not arbitrate access to foreign
                // tasks. MorphOS requires Forbid protection for this snapshot.
                Exec.Forbid();
                var segTracker = APTR.Null;
                var segTrackerLocked = false;
                if (stackTrace != 0 || registerCheck != 0)
                {
                    segTracker = Exec.FindSemaphore(
                        CString.FromLiteral("SegTracker"));
                    segTrackerLocked = segTracker.IsNotNull &&
                        Exec.AttemptSemaphoreShared(segTracker) != 0;
                }
                if (!noRun)
                    fits = AddTask(Exec.FindTask(CString.FromPointer(0)),
                        name, address, hasAddress, buffer,
                        snapshotCapacityBytes,
                        recordStride, stackTrace != 0, stackLevel,
                        internalMode != 0, systemBoundariesPointer,
                        segTracker, segTrackerLocked, ref count);
                if (fits && !noReady)
                    fits = AddListTasks(ExecBase(),
                        ExecLayout.ExecBase.TaskReady, name, address,
                        hasAddress, buffer, snapshotCapacityBytes,
                        recordStride,
                        stackTrace != 0, stackLevel, internalMode != 0,
                        systemBoundariesPointer, segTracker,
                        segTrackerLocked, ref count);
                if (fits && !noWait)
                    fits = AddListTasks(ExecBase(),
                        ExecLayout.ExecBase.TaskWait, name, address,
                        hasAddress, buffer, snapshotCapacityBytes,
                        recordStride,
                        stackTrace != 0, stackLevel, internalMode != 0,
                        systemBoundariesPointer, segTracker,
                        segTrackerLocked, ref count);
                if (segTrackerLocked && fits && registerCheck != 0)
                {
                    registerCheckSegTracker = segTracker;
                    registerCheckSegTrackerLocked = true;
                }
                else if (segTrackerLocked)
                    Exec.ReleaseSemaphore(segTracker);
                Exec.Permit();

                if (fits)
                {
                    snapshotComplete = true;
                    break;
                }

                Exec.FreeVec(buffer);
                buffer = APTR.Null;
                if (bufferBytes > uint.MaxValue / 2)
                {
                    DOS.FPuts(DOS.Output(),
                        "Not Enough memory for task buffer\n");
                    result = DOS.RETURN_FAIL;
                    break;
                }
                bufferBytes *= 2;
            }
            if (!snapshotComplete) break;

            DOS.FPuts(DOS.Output(),
                "   pid    address type  pri   state 68kstack/used     ppcstack/used     name\n");
            for (var index = 0u; index < count; index++)
            {
                var record = APTR.FromPointer(buffer.Raw + index * recordStride);
                var fields = default(OutputFields);
                fields.Pid = APTR.ReadUInt32(record, PidOffset);
                fields.Address = APTR.ReadUInt32(record, 0);
                var type = APTR.ReadUInt32(record, TypeOffset);
                fields.Type = type == NtProcess
                    ? CString.ToUInt32("proc") : CString.ToUInt32("task");
                fields.Priority = APTR.ReadUInt32(record, PriorityOffset);
                fields.State = StateText(
                    APTR.ReadUInt32(record, StateOffset),
                    APTR.ReadUInt32(record, SignalWaitOffset));
                fields.StackSize = APTR.ReadUInt32(record,
                    M68kStackSizeOffset);
                var stackUsed = APTR.ReadUInt32(record, M68kStackUsedOffset);
                var hasStackUsed = stackUsed != uint.MaxValue;
                fields.StackUsed = hasStackUsed
                    ? stackUsed : CString.ToUInt32("???     ");
                fields.PpcStackSize = APTR.ReadUInt32(record,
                    PpcStackSizeOffset);
                fields.PpcStackUsed = APTR.ReadUInt32(record,
                    PpcStackUsedOffset);
                fields.Name = APTR.ReadUInt32(record, NamePointerOffset);
                if (type == uint.MaxValue)
                {
                    fields.Type = CString.ToUInt32(" cli");
                    fields.CliName = APTR.ReadUInt32(record,
                        CliNamePointerOffset);
                    DOS.VPrintf(hasStackUsed
                        ? "%6lu 0x%08lx %s %4ld %s %8lu/%-8lu %8lu/%-8lu %s [%s]\n"
                        : "%6lu 0x%08lx %s %4ld %s %8lu/%s %8lu/%-8lu %s [%s]\n",
                        OutputFields.AddressOf(ref fields));
                }
                else
                {
                    fields.Type = type == NtProcess
                        ? CString.ToUInt32("proc") : CString.ToUInt32("task");
                    DOS.VPrintf(hasStackUsed
                        ? "%4lu 0x%08lx %s %4ld %s %8lu/%-8lu %8lu/%-8lu %s\n"
                        : "%4lu 0x%08lx %s %4ld %s %8lu/%s %8lu/%-8lu %s\n",
                        OutputFields.AddressOf(ref fields));
                }
                if (verbose != 0)
                    WriteVerbose(record);
                if (stackTrace != 0 && APTR.ReadUInt32(record, StateOffset) !=
                        (uint)TaskState.Running)
                    WriteStackTrace(record, stackLevel, internalMode != 0,
                        systemBoundariesPointer);
                if (registerDump != 0 && APTR.ReadUInt32(record, StateOffset) !=
                        (uint)TaskState.Running)
                    WriteRegisterDump(record);
                if (registerCheck != 0 && APTR.ReadUInt32(record, StateOffset) !=
                        (uint)TaskState.Running)
                    WriteRegisterCheck(record, internalMode != 0,
                        systemBoundariesPointer,
                        APTR.FromPointer(buffer.Raw + bufferBytes -
                            RegisterCheckScratchBytes),
                        registerCheckSegTracker,
                        registerCheckSegTrackerLocked);
                if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                {
                    result = DOS.RETURN_FAIL;
                    error = (int)DOS.Error.Break;
                    DOS.SetIoErr(DOS.Error.Break);
                    break;
                }
            }
            if (registerCheckSegTrackerLocked)
            {
                Exec.ReleaseSemaphore(registerCheckSegTracker);
                registerCheckSegTrackerLocked = false;
            }
        }
        while (false);

        if (buffer.IsNotNull)
            Exec.FreeVec(buffer);
        arguments.Release();
        if (error != 0)
        {
            ioError = error;
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            result = DOS.RETURN_FAIL;
        }
        Exec.CloseLibrary(sysdebug);
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static APTR ExecBase() =>
        APTR.FromPointer(APTR.ReadUInt32(APTR.FromPointer(4), 0));

    private static bool AddListTasks(APTR execBase, int listOffset,
        APTR wantedName, int wantedAddress, bool hasAddress, APTR buffer,
        uint bufferBytes, uint recordStride, bool stackTrace,
        uint stackLevel, bool internalMode, APTR systemBoundaries,
        APTR segTracker, bool segTrackerLocked, ref uint count)
    {
        var list = APTR.FromPointer(execBase.Raw + (uint)listOffset);
        var node = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        while (node.IsNotNull && APTR.ReadUInt32(node,
                ExecLayout.Node.Successor) != 0)
        {
            if (!AddTask(node, wantedName, wantedAddress, hasAddress, buffer,
                    bufferBytes, recordStride, stackTrace, stackLevel,
                    internalMode, systemBoundaries, segTracker,
                    segTrackerLocked, ref count))
                return false;
            node = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Successor));
        }
        return true;
    }

    private static bool AddTask(APTR task, APTR wantedName,
        int wantedAddress, bool hasAddress, APTR buffer, uint bufferBytes,
        uint recordStride, bool stackTrace, uint stackLevel,
        bool internalMode, APTR systemBoundaries, APTR segTracker,
        bool segTrackerLocked, ref uint count)
    {
        if (task.IsNull) return true;
        var bytesUsed = count * recordStride;
        if (bytesUsed > bufferBytes || recordStride > bufferBytes - bytesUsed)
            return false;
        var record = APTR.FromPointer(buffer.Raw + bytesUsed);
        APTR.WriteUInt32(record, 0, task.Raw);
        var taskName = APTR.FromPointer(APTR.ReadUInt32(task,
            ExecLayout.Node.Name));
        var nodeType = APTR.ReadUInt8(task, ExecLayout.Node.Type);
        var taskType = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + TypeOffset), TaskAttribute.Type,
            nodeType);
        var cliBptr = taskType == NtProcess
            ? APTR.ReadUInt32(task, DosLayout.Process.CommandLineInterface)
            : 0u;
        var commandName = APTR.Null;
        if (cliBptr != 0)
        {
            var cli = BPTR.FromRaw(cliBptr).Address;
            commandName = BPTR.FromRaw(APTR.ReadUInt32(cli,
                DosLayout.CommandLineInterface.CommandName)).Address;
        }
        if (wantedName.IsNotNull && !EqualsCString(taskName, wantedName) &&
            !EqualsBString(wantedName, commandName))
            return true;
        if (hasAddress && task.Raw != unchecked((uint)wantedAddress))
            return true;

        var pid = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PidOffset),
            TaskAttribute.ProcessIdCli, 0);
        APTR.WriteUInt32(record, PidOffset, pid);
        APTR.WriteUInt32(record, TypeOffset, taskType);

        var priorityFallback = unchecked((uint)(int)(sbyte)
            APTR.ReadUInt8(task, ExecLayout.Node.Priority));
        var priority = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PriorityOffset),
            TaskAttribute.Priority, priorityFallback);
        APTR.WriteUInt32(record, PriorityOffset, priority);

        var stateFallback = APTR.ReadUInt8(task, ExecLayout.Task.State);
        var state = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + StateOffset), TaskAttribute.State,
            stateFallback);
        APTR.WriteUInt32(record, StateOffset, state);

        var lower = APTR.ReadUInt32(task, ExecLayout.Task.StackLower);
        var upper = APTR.ReadUInt32(task, ExecLayout.Task.StackUpper);
        var stack = APTR.ReadUInt32(task, ExecLayout.Task.StackPointer);
        var m68kStackSizeFallback = upper >= lower ? upper - lower : 0;
        var m68kStackSize = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + M68kStackSizeOffset),
            TaskAttribute.StackSizeM68k, m68kStackSizeFallback);
        APTR.WriteUInt32(record, M68kStackSizeOffset, m68kStackSize);

        var m68kStackUsedFallback = uint.MaxValue;
        if (state != (uint)TaskState.Running && stack >= lower &&
            stack <= upper)
            m68kStackUsedFallback = upper - stack;
        var m68kStackUsed = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + M68kStackUsedOffset),
            TaskAttribute.UsedStackSizeM68k, m68kStackUsedFallback);
        APTR.WriteUInt32(record, M68kStackUsedOffset, m68kStackUsed);

        // Stopped tasks use tc_SPReg. VERBOSE replaces the current task's
        // saved value with a live A7 sample immediately before printing it.
        APTR.WriteUInt32(record, M68kStackPointerOffset, stack);
        var m68kUpper = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + M68kStackUpperOffset),
            TaskAttribute.M68kStackUpper, upper);
        APTR.WriteUInt32(record, M68kStackUpperOffset, m68kUpper);
        var m68kLower = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + M68kStackLowerOffset),
            TaskAttribute.M68kStackLower, lower);
        APTR.WriteUInt32(record, M68kStackLowerOffset, m68kLower);

        var ppcStackSize = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PpcStackSizeOffset),
            TaskAttribute.StackSizePpc, 0);
        APTR.WriteUInt32(record, PpcStackSizeOffset, ppcStackSize);
        var ppcUpper = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PpcStackUpperOffset),
            TaskAttribute.PpcStackUpper, 0);
        APTR.WriteUInt32(record, PpcStackUpperOffset, ppcUpper);
        var ppcLower = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PpcStackLowerOffset),
            TaskAttribute.PpcStackLower, 0);
        APTR.WriteUInt32(record, PpcStackLowerOffset, ppcLower);
        var ppcStackUsed = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PpcStackUsedOffset),
            TaskAttribute.UsedStackSizePpc, 0);
        APTR.WriteUInt32(record, PpcStackUsedOffset, ppcStackUsed);

        var signalWait = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + SignalWaitOffset),
            TaskAttribute.SignalWait,
            APTR.ReadUInt32(task, ExecLayout.Task.SignalWait));
        APTR.WriteUInt32(record, SignalWaitOffset, signalWait);
        var signalReceived = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + SignalReceivedOffset),
            TaskAttribute.SignalReceived,
            APTR.ReadUInt32(task, ExecLayout.Task.SignalReceived));
        APTR.WriteUInt32(record, SignalReceivedOffset, signalReceived);
        var signalException = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + SignalExceptionOffset),
            TaskAttribute.SignalException,
            APTR.ReadUInt32(task, ExecLayout.Task.SignalException));
        APTR.WriteUInt32(record, SignalExceptionOffset, signalException);

        if (state != (uint)TaskState.Running)
            ReadPpcRegisters(task, record);
        if (stackTrace && state != (uint)TaskState.Running)
            CapturePpcStackTrace(record, stackLevel, internalMode,
                systemBoundaries, segTracker, segTrackerLocked);

        var nameBuffer = APTR.FromPointer(record.Raw + NameOffset);
        CopyCString(taskName, nameBuffer, 255);
        APTR.WriteUInt32(record, NamePointerOffset, nameBuffer.Raw);

        if (taskType == NtProcess && cliBptr != 0)
        {
            APTR.WriteUInt32(record, TypeOffset, uint.MaxValue);
            var cliName = APTR.FromPointer(record.Raw + CliNameOffset);
            var cliLength = commandName.IsNull
                ? 0u
                : APTR.ReadUInt8(commandName, 0);
            for (var index = 0u; index < cliLength; index++)
                APTR.WriteUInt8(APTR.FromPointer(cliName.Raw + index), 0,
                    APTR.ReadUInt8(APTR.FromPointer(
                        commandName.Raw + index + 1), 0));
            APTR.WriteUInt8(APTR.FromPointer(cliName.Raw + cliLength), 0, 0);
            APTR.WriteUInt32(record, CliNamePointerOffset, cliName.Raw);
        }
        count++;
        return true;
    }

    private static bool TryGetRecordStride(uint stackLevel,
        out uint recordStride)
    {
        recordStride = RecordBytes;
        var maxEntries = (uint.MaxValue - RecordBytes) /
            StackFrameEntryBytes;
        if (maxEntries <= StackFramePrefixEntries + 1 ||
            stackLevel > maxEntries - StackFramePrefixEntries - 1)
            return false;
        var entryCount = StackFramePrefixEntries + stackLevel + 1;
        recordStride = RecordBytes + entryCount * StackFrameEntryBytes;
        return true;
    }

    private static void CapturePpcStackTrace(APTR record,
        uint stackLevel, bool internalMode, APTR systemBoundaries,
        APTR segTracker, bool segTrackerLocked)
    {
        APTR.WriteUInt32(record, StackFrameCountOffset, 0);
        APTR.WriteUInt32(record, StackAddressValidMaskOffset, 0);

        var srr0 = APTR.ReadUInt32(record, PpcSrr0Offset);
        var lr = APTR.ReadUInt32(record, PpcLrOffset);
        var ctr = APTR.ReadUInt32(record, PpcCtrOffset);
        if (CaptureStackAddress(record, TraceSrr0Index, srr0,
                internalMode, systemBoundaries, segTracker,
                segTrackerLocked))
            SetStackAddressValid(record, TraceSrr0Index);
        if (CaptureStackAddress(record, TraceLrIndex, lr,
                internalMode, systemBoundaries, segTracker,
                segTrackerLocked))
            SetStackAddressValid(record, TraceLrIndex);
        if (CaptureStackAddress(record, TraceCtrIndex, ctr,
                internalMode, systemBoundaries, segTracker,
                segTrackerLocked))
            SetStackAddressValid(record, TraceCtrIndex);

        if (stackLevel == 0) return;

        var stack = APTR.ReadUInt32(record, PpcGprOffset + 4);
        var stackLower = APTR.ReadUInt32(record, PpcStackLowerOffset);
        var stackUpper = APTR.ReadUInt32(record, PpcStackUpperOffset);
        uint stackEnd;
        if (stack >= stackLower && stack < stackUpper)
            stackEnd = stackUpper;
        else
        {
            if (stack > uint.MaxValue - PpcStackScanBytes) return;
            stackEnd = stack + PpcStackScanBytes;
        }
        if (stack >= stackEnd || stackEnd - stack < 8) return;

        var current = stack;
        var frameCount = 0u;
        var walked = 0u;
        // The source bounds the number of emitted frames with MaxLevel.  The
        // hop bound also prevents malformed backchains with invalid LRs from
        // making this protected snapshot loop without progress.
        while (current < stackEnd && frameCount < stackLevel &&
            walked <= stackLevel)
        {
            if ((current & 3) != 0 || current > stackEnd - 8 ||
                !IsValidAddress(current, internalMode, systemBoundaries))
                break;

            var next = APTR.ReadUInt32(APTR.FromPointer(current), 0);
            if (next < stack || next <= current || next >= stackEnd ||
                (next & 3) != 0 || next > stackEnd - 8 ||
                !IsValidAddress(next, internalMode, systemBoundaries))
                break;

            var returnAddress = APTR.ReadUInt32(
                APTR.FromPointer(next + 4), 0);
            if (CaptureStackAddress(record,
                    StackFramePrefixEntries + frameCount, returnAddress,
                    internalMode, systemBoundaries, segTracker,
                    segTrackerLocked))
                frameCount++;

            current = next;
            walked++;
        }

        APTR.WriteUInt32(record, StackFrameCountOffset, frameCount);
    }

    private static void SetStackAddressValid(APTR record, uint index)
    {
        var mask = APTR.ReadUInt32(record, StackAddressValidMaskOffset);
        APTR.WriteUInt32(record, StackAddressValidMaskOffset,
            mask | (1u << (int)index));
    }

    private static bool CaptureStackAddress(APTR record, uint index,
        uint address, bool internalMode, APTR systemBoundaries,
        APTR segTracker, bool segTrackerLocked)
    {
        if (!IsValidAddress(address, internalMode, systemBoundaries))
            return false;

        var entry = StackFrameEntry(record, index);
        APTR.WriteUInt32(entry, 0, 0);
        APTR.WriteUInt32(entry, 4, 0);
        APTR.WriteUInt32(entry, 8, address);
        APTR.WriteUInt32(entry, 12, 0);
        APTR.WriteUInt32(entry, 16, 0);
        var nameBuffer = APTR.FromPointer(entry.Raw + 20);
        APTR.WriteUInt8(nameBuffer, 0, 0);

        if (segTrackerLocked && segTracker.IsNotNull)
        {
            var finder = APTR.FromPointer(APTR.ReadUInt32(segTracker,
                (int)SegTrackerFunctionOffset));
            if (finder.IsNotNull && Exec.TypeOfMem(finder) != 0)
            {
                var name = SegFindIndirectCall(finder,
                    APTR.FromPointer(address),
                    APTR.FromPointer(entry.Raw + 12),
                    APTR.FromPointer(entry.Raw + 16));
                if (name.IsNotNull && Exec.TypeOfMem(name) != 0)
                {
                    CopyCString(name, nameBuffer, StackFrameNameBytes - 1);
                    APTR.WriteUInt32(entry, 0, nameBuffer.Raw);
                }
                else
                {
                    APTR.WriteUInt32(entry, 12, 0);
                    APTR.WriteUInt32(entry, 16, 0);
                }
            }
        }
        return true;
    }

    private static bool IsValidAddress(uint address, bool internalMode,
        APTR systemBoundaries)
    {
        if (address == 0) return false;
        if (internalMode && IsInBoundaryRange(address,
                APTR.ReadUInt32(systemBoundaries, 0),
                APTR.ReadUInt32(systemBoundaries, 4)))
            return true;
        if (internalMode && IsInBoundaryRange(address,
                APTR.ReadUInt32(systemBoundaries, 8),
                APTR.ReadUInt32(systemBoundaries, 12)))
            return true;
        return Exec.TypeOfMem(APTR.FromPointer(address)) != 0;
    }

    private static bool IsInBoundaryRange(uint address, uint start,
        uint size) => size != 0 && address >= start &&
        (ulong)address < (ulong)start + size;

    private static APTR StackFrameEntry(APTR record, uint index) =>
        APTR.FromPointer(record.Raw + RecordBytes +
            index * StackFrameEntryBytes);

    private static void WriteStackTrace(APTR record, uint stackLevel,
        bool internalMode, APTR systemBoundaries)
    {
        var validMask = APTR.ReadUInt32(record, StackAddressValidMaskOffset);
        if ((validMask & (1u << (int)TraceSrr0Index)) != 0)
            WriteStackAddress(record, TraceSrr0Index,
                CString.ToUInt32("                  SRR0"), internalMode,
                systemBoundaries, false, 0);
        if ((validMask & (1u << (int)TraceLrIndex)) != 0)
            WriteStackAddress(record, TraceLrIndex,
                CString.ToUInt32("                    LR"), internalMode,
                systemBoundaries, false, 0);
        if ((validMask & (1u << (int)TraceCtrIndex)) != 0)
            WriteStackAddress(record, TraceCtrIndex,
                CString.ToUInt32("                   CTR"), internalMode,
                systemBoundaries, false, 0);

        var frameCount = APTR.ReadUInt32(record, StackFrameCountOffset);
        if (frameCount > stackLevel) frameCount = stackLevel;
        for (var index = 0u; index < frameCount; index++)
            WriteStackAddress(record, StackFramePrefixEntries + index, 0,
                internalMode, systemBoundaries, true, index);
    }

    private static void WriteStackAddress(APTR record, uint entryIndex,
        uint prefix, bool internalMode, APTR systemBoundaries,
        bool stackFrame, uint frameIndex)
    {
        var entry = StackFrameEntry(record, entryIndex);
        var address = APTR.ReadUInt32(entry, 8);
        var name = APTR.ReadUInt32(entry, 0);
        var segment = APTR.ReadUInt32(entry, 12);
        var offset = APTR.ReadUInt32(entry, 16);
        if (name != 0)
        {
            if (stackFrame)
            {
                var fields = new StackFrameFields
                {
                    Index = frameIndex,
                    Address = address,
                    Name = name,
                    Segment = segment,
                    Offset = offset
                };
                DOS.VPrintf(
                    "     StackFrame[%2ld].LR -> Address 0x%08lx -> %s Hunk %ld Offset 0x%08lx\n",
                    StackFrameFields.AddressOf(ref fields));
            }
            else
            {
                var fields = new StackAddressFields
                {
                    Prefix = prefix,
                    Address = address,
                    Name = name,
                    Segment = segment,
                    Offset = offset
                };
                DOS.VPrintf(
                    "%s -> Address 0x%08lx -> %s Hunk %ld Offset 0x%08lx\n",
                    StackAddressFields.AddressOf(ref fields));
            }
            return;
        }

        if (!stackFrame)
        {
            if (internalMode)
                WriteInternalAddress(prefix, address, systemBoundaries);
            return;
        }

        if (address == APTR.ReadUInt32(systemBoundaries, 16))
        {
            WriteStackSpecialFrame(frameIndex, address,
                "exec.library/TaskExitCode");
            return;
        }
        if (address == APTR.ReadUInt32(systemBoundaries, 20))
        {
            WriteStackSpecialFrame(frameIndex, address,
                "exec.library/TaskExitCode_M68k");
            return;
        }
        if (internalMode && IsInBoundaryRange(address,
                APTR.ReadUInt32(systemBoundaries, 0),
                APTR.ReadUInt32(systemBoundaries, 4)))
        {
            WriteStackFrameOffset(frameIndex, address,
                "ABOX: Emulation Offset",
                address - APTR.ReadUInt32(systemBoundaries, 0));
            return;
        }
        if (internalMode && IsInBoundaryRange(address,
                APTR.ReadUInt32(systemBoundaries, 8),
                APTR.ReadUInt32(systemBoundaries, 12)))
        {
            WriteStackFrameOffset(frameIndex, address,
                "ABOX: Module Offset",
                address - APTR.ReadUInt32(systemBoundaries, 8));
            return;
        }
        WriteStackFrameRaw(frameIndex, address);
    }

    private static void WriteInternalAddress(uint prefix, uint address,
        APTR systemBoundaries)
    {
        if (IsInBoundaryRange(address, APTR.ReadUInt32(systemBoundaries, 0),
                APTR.ReadUInt32(systemBoundaries, 4)))
        {
            var fields = new StackAddressFields
            {
                Prefix = prefix,
                Address = address,
                Offset = address - APTR.ReadUInt32(systemBoundaries, 8)
            };
            DOS.VPrintf(
                "%s -> Address 0x%08lx -> ABOX: Emulation Offset 0x%lx\n",
                StackAddressFields.AddressOf(ref fields));
        }
        else if (IsInBoundaryRange(address,
                     APTR.ReadUInt32(systemBoundaries, 8),
                     APTR.ReadUInt32(systemBoundaries, 12)))
        {
            var fields = new StackAddressFields
            {
                Prefix = prefix,
                Address = address,
                Offset = address - APTR.ReadUInt32(systemBoundaries, 8)
            };
            DOS.VPrintf(
                "%s -> Address 0x%08lx -> ABOX: Module Offset 0x%lx\n",
                StackAddressFields.AddressOf(ref fields));
        }
    }

    private static void WriteStackSpecialFrame(uint index, uint address,
        CString module)
    {
        var fields = new StackFrameFields
        {
            Index = index,
            Address = address,
            Name = CString.ToUInt32(module)
        };
        DOS.VPrintf(
            "     StackFrame[%2ld].LR -> Address 0x%08lx -> ABOX: Module <%s>\n",
            StackFrameFields.AddressOf(ref fields));
    }

    private static void WriteStackFrameOffset(uint index, uint address,
        CString description, uint offset)
    {
        var fields = new StackFrameFields
        {
            Index = index,
            Address = address,
            Name = CString.ToUInt32(description),
            Offset = offset
        };
        DOS.VPrintf(
            "     StackFrame[%2ld].LR -> Address 0x%08lx -> %s 0x%lx\n",
            StackFrameFields.AddressOf(ref fields));
    }

    private static void WriteStackFrameRaw(uint index, uint address)
    {
        var fields = new StackFrameFields
        {
            Index = index,
            Address = address
        };
        DOS.VPrintf(
            "     StackFrame[%2ld].LR -> Address 0x%08lx\n",
            StackFrameFields.AddressOf(ref fields));
    }

    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern APTR SegFindIndirectCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A0)] APTR address,
        [M68kRegister(M68kRegister.A1)] APTR segment,
        [M68kRegister(M68kRegister.A2)] APTR offset);

    private static void ReadPpcRegisters(APTR task, APTR record)
    {
        _ = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PpcSrr0Offset),
            TaskAttribute.PpcSrr0, 0);
        _ = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PpcLrOffset),
            TaskAttribute.PpcLr, 0);
        _ = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PpcCtrOffset),
            TaskAttribute.PpcCtr, 0);
        _ = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PpcCrOffset),
            TaskAttribute.PpcCr, 0);
        _ = ReadTaskAttribute(task,
            APTR.FromPointer(record.Raw + PpcXerOffset),
            TaskAttribute.PpcXer, 0);

        var tags = APTR.FromPointer(record.Raw + RegisterTagsOffset);
        APTR.WriteUInt32(tags, 0, TaskInfoTagDummy + 1);
        APTR.WriteUInt32(tags, 4, 0);
        APTR.WriteUInt32(tags, 8, TaskInfoTagDummy + 2);
        APTR.WriteUInt32(tags, 12, 32);
        APTR.WriteUInt32(tags, 16, 0);
        APTR.WriteUInt32(tags, 20, 0);

        var gpr = APTR.FromPointer(record.Raw + PpcGprOffset);
        ClearWords(gpr, PpcRegisterBytes / 4);
        _ = Exec.NewGetTaskAttrsA(task, gpr, PpcRegisterBytes,
            (uint)TaskAttribute.PpcGpr, tags);

        var fpr = APTR.FromPointer(record.Raw + PpcFprOffset);
        ClearWords(fpr, PpcFloatingRegisterBytes / 4);
        _ = Exec.NewGetTaskAttrsA(task, fpr, PpcFloatingRegisterBytes,
            (uint)TaskAttribute.PpcFpr, tags);

        var vsave = APTR.FromPointer(record.Raw + PpcVsaveOffset);
        _ = ReadTaskAttribute(task, vsave, TaskAttribute.PpcVsave, 0);
        var vscr = APTR.FromPointer(record.Raw + PpcVscrOffset);
        ClearWords(vscr, 4);
        _ = Exec.NewGetTaskAttrsA(task, vscr, 16,
            (uint)TaskAttribute.PpcVscr, APTR.Null);
        // The captured MorphOS source intentionally compiles Altivec support
        // out with #if 1, while still requesting VSAVE and VSCR.
        APTR.WriteUInt32(record, PpcAltivecOffset, 0);
    }

    private static void ClearWords(APTR address, uint count)
    {
        for (var index = 0u; index < count; index++)
            APTR.WriteUInt32(APTR.FromPointer(address.Raw + index * 4), 0, 0);
    }

    private static void WriteRegisterDump(APTR record)
    {
        var header = default(RegisterHeaderFields);
        header.Srr0 = APTR.ReadUInt32(record, PpcSrr0Offset);
        header.Lr = APTR.ReadUInt32(record, PpcLrOffset);
        header.Ctr = APTR.ReadUInt32(record, PpcCtrOffset);
        header.Cr = APTR.ReadUInt32(record, PpcCrOffset);
        header.Xer = APTR.ReadUInt32(record, PpcXerOffset);
        DOS.VPrintf(
            "     SRR0 0x%08lx LR 0x%08lx CTR 0x%lx CR 0x%lx XER 0x%lx\n",
            RegisterHeaderFields.AddressOf(ref header));

        for (var group = 0u; group < 4; group++)
        {
            var first = (int)(group * 8);
            var fields = default(RegisterGroupFields);
            fields.Start = (uint)first;
            fields.R0 = APTR.ReadUInt32(record,
                PpcGprOffset + first * 4);
            fields.R1 = APTR.ReadUInt32(record,
                PpcGprOffset + (first + 1) * 4);
            fields.R2 = APTR.ReadUInt32(record,
                PpcGprOffset + (first + 2) * 4);
            fields.R3 = APTR.ReadUInt32(record,
                PpcGprOffset + (first + 3) * 4);
            fields.R4 = APTR.ReadUInt32(record,
                PpcGprOffset + (first + 4) * 4);
            fields.R5 = APTR.ReadUInt32(record,
                PpcGprOffset + (first + 5) * 4);
            fields.R6 = APTR.ReadUInt32(record,
                PpcGprOffset + (first + 6) * 4);
            fields.R7 = APTR.ReadUInt32(record,
                PpcGprOffset + (first + 7) * 4);
            DOS.VPrintf(
                "     GPR[%02ld] 0x%08lx 0x%08lx 0x%08lx 0x%08lx 0x%08lx 0x%08lx 0x%08lx 0x%08lx\n",
                RegisterGroupFields.AddressOf(ref fields));
        }
    }

    private static void WriteRegisterCheck(APTR record, bool internalMode,
        APTR systemBoundaries, APTR scratch, APTR segTracker,
        bool segTrackerLocked)
    {
        var execBase = ExecBase();
        for (var register = 0u; register < 32; register++)
        {
            var address = APTR.ReadUInt32(record,
                PpcGprOffset + (int)(register * 4));
            if (ShowRegisterPointer(register, address, internalMode,
                    systemBoundaries, scratch, segTracker,
                    segTrackerLocked))
                continue;

            if (ScanExecNodeRegister(execBase, ExecLayout.ExecBase.LibraryList,
                    address, register, scratch,
                    CString.ToUInt32("Library")) ||
                ScanExecNodeRegister(execBase, ExecLayout.ExecBase.DeviceList,
                    address, register, scratch,
                    CString.ToUInt32("Device")) ||
                ScanExecNodeRegister(execBase, ExecLayout.ExecBase.ResourceList,
                    address, register, scratch,
                    CString.ToUInt32("Resource")))
            {
                PrintRegisterCheckLine(scratch);
                continue;
            }

            if (ScanExecLibraryRegister(execBase,
                    ExecLayout.ExecBase.LibraryList, address, register,
                    scratch, CString.ToUInt32("Library")) ||
                ScanExecLibraryRegister(execBase,
                    ExecLayout.ExecBase.DeviceList, address, register,
                    scratch, CString.ToUInt32("Device")))
            {
                PrintRegisterCheckLine(scratch);
                continue;
            }

            if (ScanTaskRegister(execBase, address, register, scratch))
            {
                PrintRegisterCheckLine(scratch);
                continue;
            }

            if (ScanExecPortRegister(execBase, address, register, scratch))
            {
                PrintRegisterCheckLine(scratch);
                continue;
            }

            if (ScanExecSemaphoreRegister(execBase, address, register,
                    scratch))
                PrintRegisterCheckLine(scratch);
        }
    }

    private static bool ShowRegisterPointer(uint register, uint address,
        bool internalMode, APTR systemBoundaries, APTR scratch,
        APTR segTracker, bool segTrackerLocked)
    {
        var emulationStart = APTR.ReadUInt32(systemBoundaries, 0);
        var emulationSize = APTR.ReadUInt32(systemBoundaries, 4);
        var moduleStart = APTR.ReadUInt32(systemBoundaries, 8);
        var moduleSize = APTR.ReadUInt32(systemBoundaries, 12);
        var inEmulation = IsInBoundaryRange(address, emulationStart,
            emulationSize);
        var inModule = IsInBoundaryRange(address, moduleStart, moduleSize);
        if (address == 0 && !inEmulation && !inModule)
            return false;
        if (!inEmulation && !inModule &&
            Exec.TypeOfMem(APTR.FromPointer(address)) == 0)
            return false;

        if (segTrackerLocked && segTracker.IsNotNull)
        {
            var finder = APTR.FromPointer(APTR.ReadUInt32(segTracker,
                (int)SegTrackerFunctionOffset));
            if (finder.IsNotNull && Exec.TypeOfMem(finder) != 0)
            {
                var symbolFieldsPointer = APTR.FromPointer(scratch.Raw +
                    RegisterCheckScratchBytes - 24);
                APTR.WriteUInt32(symbolFieldsPointer, 0, register);
                APTR.WriteUInt32(symbolFieldsPointer, 4, address);
                APTR.WriteUInt32(symbolFieldsPointer, 8, 0);
                APTR.WriteUInt32(symbolFieldsPointer, 12, 0);
                APTR.WriteUInt32(symbolFieldsPointer, 16, 0);
                var name = SegFindIndirectCall(finder,
                    APTR.FromPointer(address),
                    APTR.FromPointer(symbolFieldsPointer.Raw + 12),
                    APTR.FromPointer(symbolFieldsPointer.Raw + 16));
                if (name.IsNotNull && Exec.TypeOfMem(name) != 0)
                {
                    BuildRegisterSymbolLine(scratch, register, address, name,
                        APTR.ReadUInt32(symbolFieldsPointer, 12),
                        APTR.ReadUInt32(symbolFieldsPointer, 16));
                    PrintRegisterCheckLine(scratch);
                    return true;
                }
            }
        }

        if (internalMode && (inEmulation || inModule))
        {
            var offset = address - moduleStart;
            var cursor = StartRegisterCheckLine(scratch, register, address,
                8);
            cursor = AppendText(scratch, cursor,
                CString.ToUInt32(" -> ABOX: "));
            cursor = AppendText(scratch, cursor,
                inEmulation
                    ? CString.ToUInt32("Emulation Offset 0x")
                    : CString.ToUInt32("Module Offset 0x"));
            cursor = AppendHex(scratch, cursor, offset, 1);
            FinishRegisterCheckLine(scratch, cursor);
            PrintRegisterCheckLine(scratch);
            return true;
        }
        return false;
    }

    private static bool ScanExecNodeRegister(APTR execBase,
        int listOffset, uint address, uint register, APTR scratch,
        uint listName)
    {
        Exec.Forbid();
        var list = APTR.FromPointer(execBase.Raw + (uint)listOffset);
        var node = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        var found = false;
        while (node.IsNotNull && APTR.ReadUInt32(node,
                   ExecLayout.Node.Successor) != 0)
        {
            if (node.Raw == address)
            {
                var cursor = StartRegisterCheckLine(scratch, register,
                    address, 1);
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32(" -> "));
                cursor = AppendText(scratch, cursor, listName);
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32(" <"));
                cursor = AppendCString(scratch, cursor,
                    APTR.ReadUInt32(node, ExecLayout.Node.Name));
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32(">"));
                FinishRegisterCheckLine(scratch, cursor);
                found = true;
                break;
            }
            node = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Successor));
        }
        Exec.Permit();
        return found;
    }

    private static bool ScanExecLibraryRegister(APTR execBase,
        int listOffset, uint address, uint register, APTR scratch,
        uint listName)
    {
        Exec.Forbid();
        var list = APTR.FromPointer(execBase.Raw + (uint)listOffset);
        var node = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        var found = false;
        while (node.IsNotNull && APTR.ReadUInt32(node,
                   ExecLayout.Node.Successor) != 0)
        {
            var negativeSize = APTR.ReadUInt16(node,
                ExecLayout.Library.NegativeSize);
            var positiveSize = APTR.ReadUInt16(node,
                ExecLayout.Library.PositiveSize);
            var tableStart = node.Raw >= negativeSize
                ? node.Raw - negativeSize : 0u;
            if (address < node.Raw && address > tableStart)
            {
                var cursor = StartRegisterCheckLine(scratch, register,
                    address, 1);
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32(" -> "));
                cursor = AppendText(scratch, cursor, listName);
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32(" <"));
                cursor = AppendCString(scratch, cursor,
                    APTR.ReadUInt32(node, ExecLayout.Node.Name));
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32("> FuncTable Offset -0x"));
                cursor = AppendHex(scratch, cursor, node.Raw - address, 1);
                FinishRegisterCheckLine(scratch, cursor);
                found = true;
                break;
            }
            if (address >= node.Raw && address - node.Raw < positiveSize)
            {
                var cursor = StartRegisterCheckLine(scratch, register,
                    address, 1);
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32(" -> "));
                cursor = AppendText(scratch, cursor, listName);
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32(" <"));
                cursor = AppendCString(scratch, cursor,
                    APTR.ReadUInt32(node, ExecLayout.Node.Name));
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32("> Base Offset 0x"));
                cursor = AppendHex(scratch, cursor, address - node.Raw, 1);
                FinishRegisterCheckLine(scratch, cursor);
                found = true;
                break;
            }
            node = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Successor));
        }
        Exec.Permit();
        return found;
    }

    private static bool ScanTaskRegister(APTR execBase, uint address,
        uint register, APTR scratch)
    {
        Exec.Forbid();
        var task = Exec.FindTask(CString.FromPointer(0));
        var found = ScanOneTaskRegister(task, address, register, scratch);
        if (!found)
            found = ScanTaskListRegister(execBase,
                ExecLayout.ExecBase.TaskReady, address, register, scratch);
        if (!found)
            found = ScanTaskListRegister(execBase,
                ExecLayout.ExecBase.TaskWait, address, register, scratch);
        Exec.Permit();
        return found;
    }

    private static bool ScanTaskListRegister(APTR execBase, int listOffset,
        uint address, uint register, APTR scratch)
    {
        var list = APTR.FromPointer(execBase.Raw + (uint)listOffset);
        var task = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        while (task.IsNotNull && APTR.ReadUInt32(task,
                   ExecLayout.Node.Successor) != 0)
        {
            if (ScanOneTaskRegister(task, address, register, scratch))
                return true;
            task = APTR.FromPointer(APTR.ReadUInt32(task,
                ExecLayout.Node.Successor));
        }
        return false;
    }

    private static bool ScanOneTaskRegister(APTR task, uint address,
        uint register, APTR scratch)
    {
        if (task.IsNull) return false;
        var taskType = APTR.ReadUInt8(task, ExecLayout.Node.Type);
        if (address == task.Raw)
        {
            BuildRegisterTaskLine(scratch, register, address, task, 0, 0);
            return true;
        }
        var taskSize = (uint)(ExecLayout.Task.UserData + 4);
        if (address > task.Raw && address - task.Raw < taskSize)
        {
            BuildRegisterTaskOffsetLine(scratch, register, address, task,
                CString.ToUInt32(" Offset 0x"), address - task.Raw);
            return true;
        }
        if (taskType == NtProcess && address > task.Raw &&
            address - task.Raw < DosLayout.Process.Size)
        {
            BuildRegisterTaskOffsetLine(scratch, register, address, task,
                CString.ToUInt32(" Offset 0x"), address - task.Raw);
            return true;
        }

        var extendedTask = APTR.ReadUInt32(task,
            ExecLayout.Task.TrapAllocated);
        if (extendedTask != 0 && address > extendedTask &&
            address - extendedTask < MorphOSETaskCompiledSize)
        {
            BuildRegisterTaskOffsetLine(scratch, register, address, task,
                CString.ToUInt32(" ETask Offset 0x"),
                address - extendedTask);
            return true;
        }

        var m68kLower = APTR.ReadUInt32(task, ExecLayout.Task.StackLower);
        var m68kUpper = APTR.ReadUInt32(task, ExecLayout.Task.StackUpper);
        if (address >= m68kLower && address < m68kUpper)
        {
            BuildRegisterTaskStackLine(scratch, register, address, task,
                CString.ToUInt32("68kStack"), m68kLower, m68kUpper);
            return true;
        }

        var ppcLower = ReadTaskAttribute(task, scratch,
            TaskAttribute.PpcStackLower, 0);
        var ppcUpper = ReadTaskAttribute(task,
            APTR.FromPointer(scratch.Raw + 4),
            TaskAttribute.PpcStackUpper, 0);
        if (address >= ppcLower && address < ppcUpper)
        {
            BuildRegisterTaskStackLine(scratch, register, address, task,
                CString.ToUInt32("PPCStack"), ppcLower, ppcUpper);
            return true;
        }

        if (taskType == NtProcess)
        {
            var currentInput = APTR.ReadUInt32(task,
                DosLayout.Process.CurrentInput);
            if (MatchesProcessBptr(address, currentInput))
            {
                BuildRegisterTaskSuffixLine(scratch, register, address, task,
                    CString.ToUInt32(" CIS"));
                return true;
            }
            var currentOutput = APTR.ReadUInt32(task,
                DosLayout.Process.CurrentOutput);
            if (MatchesProcessBptr(address, currentOutput))
            {
                BuildRegisterTaskSuffixLine(scratch, register, address, task,
                    CString.ToUInt32(" COS"));
                return true;
            }
            var currentError = APTR.ReadUInt32(task,
                DosLayout.Process.CurrentError);
            if (MatchesProcessBptr(address, currentError))
            {
                BuildRegisterTaskSuffixLine(scratch, register, address, task,
                    CString.ToUInt32(" CES"));
                return true;
            }
            var currentDirectory = APTR.ReadUInt32(task,
                DosLayout.Process.CurrentDirectory);
            if (MatchesProcessBptr(address, currentDirectory))
            {
                BuildRegisterTaskSuffixLine(scratch, register, address, task,
                    CString.ToUInt32(" CurrentDir Lock"));
                return true;
            }
            var cli = APTR.ReadUInt32(task,
                DosLayout.Process.CommandLineInterface);
            if (MatchesProcessBptr(address, cli))
            {
                BuildRegisterTaskSuffixLine(scratch, register, address, task,
                    CString.ToUInt32(" CLI"));
                return true;
            }
        }
        return false;
    }

    private static bool ScanExecPortRegister(APTR execBase,
        uint address, uint register, APTR scratch)
    {
        Exec.Forbid();
        var list = APTR.FromPointer(execBase.Raw +
            (uint)ExecLayout.ExecBase.PortList);
        var node = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        var found = false;
        while (node.IsNotNull && APTR.ReadUInt32(node,
                   ExecLayout.Node.Successor) != 0)
        {
            if (node.Raw == address)
            {
                var cursor = StartRegisterCheckLine(scratch, register,
                    address, 1);
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32(" -> Port <"));
                cursor = AppendCString(scratch, cursor,
                    APTR.ReadUInt32(node, ExecLayout.Node.Name));
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32("> Task "));
                cursor = AppendTaskName(scratch, cursor,
                    APTR.ReadUInt32(node, ExecLayout.MsgPort.SignalTask));
                FinishRegisterCheckLine(scratch, cursor);
                found = true;
                break;
            }
            node = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Successor));
        }
        Exec.Permit();
        return found;
    }

    private static bool ScanExecSemaphoreRegister(APTR execBase,
        uint address, uint register, APTR scratch)
    {
        Exec.Forbid();
        var list = APTR.FromPointer(execBase.Raw +
            (uint)ExecLayout.ExecBase.SemaphoreList);
        var node = APTR.FromPointer(APTR.ReadUInt32(list,
            ExecLayout.List.Head));
        var found = false;
        while (node.IsNotNull && APTR.ReadUInt32(node,
                   ExecLayout.Node.Successor) != 0)
        {
            if (node.Raw == address)
            {
                var cursor = StartRegisterCheckLine(scratch, register,
                    address, 1);
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32(" -> Semaphore <"));
                cursor = AppendCString(scratch, cursor,
                    APTR.ReadUInt32(node, ExecLayout.Node.Name));
                cursor = AppendText(scratch, cursor,
                    CString.ToUInt32("> "));
                var owner = APTR.ReadUInt32(node,
                    ExecLayout.SignalSemaphore.Owner);
                if (owner == 0)
                    cursor = AppendText(scratch, cursor,
                        CString.ToUInt32("NoOwner"));
                else
                {
                    cursor = AppendText(scratch, cursor,
                        CString.ToUInt32("Owner "));
                    cursor = AppendTaskName(scratch, cursor, owner);
                }
                FinishRegisterCheckLine(scratch, cursor);
                found = true;
                break;
            }
            node = APTR.FromPointer(APTR.ReadUInt32(node,
                ExecLayout.Node.Successor));
        }
        Exec.Permit();
        return found;
    }

    private static bool MatchesProcessBptr(uint address, uint bptr) =>
        bptr != 0 && (address == bptr || address == (bptr << 2));

    private static void BuildRegisterSymbolLine(APTR scratch,
        uint register, uint address, APTR name, uint segment, uint offset)
    {
        var cursor = StartRegisterCheckLine(scratch, register, address, 8);
        cursor = AppendText(scratch, cursor, CString.ToUInt32(" -> "));
        cursor = AppendCString(scratch, cursor, name.Raw);
        cursor = AppendText(scratch, cursor,
            CString.ToUInt32(" Hunk "));
        cursor = AppendSigned(scratch, cursor, segment);
        cursor = AppendText(scratch, cursor,
            CString.ToUInt32(" Offset 0x"));
        cursor = AppendHex(scratch, cursor, offset, 8);
        FinishRegisterCheckLine(scratch, cursor);
    }

    private static void BuildRegisterTaskLine(APTR scratch, uint register,
        uint address, APTR task, uint suffix, uint offset)
    {
        var cursor = StartRegisterCheckLine(scratch, register, address, 1);
        cursor = AppendText(scratch, cursor, CString.ToUInt32(" -> Task "));
        cursor = AppendTaskName(scratch, cursor, task.Raw);
        if (suffix != 0)
        {
            cursor = AppendText(scratch, cursor, suffix);
            cursor = AppendHex(scratch, cursor, offset, 1);
        }
        FinishRegisterCheckLine(scratch, cursor);
    }

    private static void BuildRegisterTaskOffsetLine(APTR scratch,
        uint register, uint address, APTR task, uint suffix, uint offset)
    {
        var cursor = StartRegisterCheckLine(scratch, register, address, 1);
        cursor = AppendText(scratch, cursor, CString.ToUInt32(" -> Task "));
        cursor = AppendTaskName(scratch, cursor, task.Raw);
        cursor = AppendText(scratch, cursor, suffix);
        cursor = AppendHex(scratch, cursor, offset, 1);
        FinishRegisterCheckLine(scratch, cursor);
    }

    private static void BuildRegisterTaskSuffixLine(APTR scratch,
        uint register, uint address, APTR task, uint suffix)
    {
        var cursor = StartRegisterCheckLine(scratch, register, address, 1);
        cursor = AppendText(scratch, cursor, CString.ToUInt32(" -> Task "));
        cursor = AppendTaskName(scratch, cursor, task.Raw);
        cursor = AppendText(scratch, cursor, suffix);
        FinishRegisterCheckLine(scratch, cursor);
    }

    private static void BuildRegisterTaskStackLine(APTR scratch,
        uint register, uint address, APTR task, uint stackName,
        uint lower, uint upper)
    {
        var cursor = StartRegisterCheckLine(scratch, register, address, 1);
        cursor = AppendText(scratch, cursor, CString.ToUInt32(" -> Task "));
        cursor = AppendTaskName(scratch, cursor, task.Raw);
        cursor = AppendText(scratch, cursor, CString.ToUInt32(" "));
        cursor = AppendText(scratch, cursor, stackName);
        cursor = AppendText(scratch, cursor, CString.ToUInt32(" Offset 0x"));
        cursor = AppendHex(scratch, cursor, address - lower, 1);
        cursor = AppendText(scratch, cursor, CString.ToUInt32(" [0x"));
        cursor = AppendHex(scratch, cursor, lower, 1);
        cursor = AppendText(scratch, cursor, CString.ToUInt32("..0x"));
        cursor = AppendHex(scratch, cursor, upper, 1);
        cursor = AppendText(scratch, cursor, CString.ToUInt32("]"));
        FinishRegisterCheckLine(scratch, cursor);
    }

    private static uint AppendTaskName(APTR scratch, uint cursor,
        uint taskAddress)
    {
        var position = AppendText(scratch, cursor,
            CString.ToUInt32("0x"));
        position = AppendHex(scratch, position, taskAddress, 1);
        position = AppendText(scratch, position,
            CString.ToUInt32(" <"));
        if (taskAddress == 0)
        {
            position = AppendText(scratch, position,
                CString.ToUInt32(">"));
            return position;
        }
        var task = APTR.FromPointer(taskAddress);
        position = AppendCString(scratch, position,
            APTR.ReadUInt32(task, ExecLayout.Node.Name));
        position = AppendText(scratch, position,
            CString.ToUInt32(">"));
        if (APTR.ReadUInt8(task, ExecLayout.Node.Type) == NtProcess)
        {
            var cliBptr = APTR.ReadUInt32(task,
                DosLayout.Process.CommandLineInterface);
            if (cliBptr != 0)
            {
                var cli = BPTR.FromRaw(cliBptr).Address;
                var commandBptr = APTR.ReadUInt32(cli,
                    DosLayout.CommandLineInterface.CommandName);
                if (commandBptr != 0)
                {
                    var commandName = BPTR.FromRaw(commandBptr).Address;
                    position = AppendText(scratch, position,
                        CString.ToUInt32(" [ "));
                    position = AppendBString(scratch, position,
                        commandName.Raw);
                    position = AppendText(scratch, position,
                        CString.ToUInt32(" ]"));
                }
            }
        }
        return position;
    }

    private static uint StartRegisterCheckLine(APTR scratch,
        uint register, uint address, uint addressWidth)
    {
        APTR.WriteUInt8(scratch, 0, 0);
        var cursor = AppendText(scratch, 0,
            CString.ToUInt32("     GPR["));
        if (register < 10)
        {
            cursor = AppendByte(scratch, cursor, (byte)'0');
            cursor = AppendByte(scratch, cursor,
                (byte)('0' + register));
        }
        else
        {
            cursor = AppendByte(scratch, cursor,
                (byte)('0' + register / 10));
            cursor = AppendByte(scratch, cursor,
                (byte)('0' + register % 10));
        }
        cursor = AppendText(scratch, cursor,
            CString.ToUInt32("] -> Address 0x"));
        return AppendHex(scratch, cursor, address, addressWidth);
    }

    private static uint AppendText(APTR scratch, uint cursor,
        uint cString) => AppendCString(scratch, cursor, cString);

    private static uint AppendCString(APTR scratch, uint cursor,
        uint cString)
    {
        var position = cursor;
        if (cString == 0) return position;
        var sourceIndex = 0u;
        while (position < RegisterCheckScratchBytes - 2)
        {
            var value = APTR.ReadUInt8(
                APTR.FromPointer(cString + sourceIndex), 0);
            if (value == 0) break;
            position = AppendByte(scratch, position, value);
            sourceIndex++;
        }
        return position;
    }

    private static uint AppendBString(APTR scratch, uint cursor,
        uint bString)
    {
        var position = cursor;
        if (bString == 0) return position;
        var length = APTR.ReadUInt8(APTR.FromPointer(bString), 0);
        for (var index = 0u; index < length &&
             position < RegisterCheckScratchBytes - 2; index++)
            position = AppendByte(scratch, position, APTR.ReadUInt8(
                APTR.FromPointer(bString + index + 1), 0));
        return position;
    }

    private static uint AppendHex(APTR scratch, uint cursor, uint value,
        uint minimumWidth)
    {
        var position = cursor;
        var digits = 1u;
        var probe = value;
        while (probe > 0xFu)
        {
            probe >>= 4;
            digits++;
        }
        var width = digits > minimumWidth ? digits : minimumWidth;
        var hexDigits = CString.ToUInt32("0123456789abcdef");
        for (var index = width; index > 0 &&
             position < RegisterCheckScratchBytes - 2; index--)
        {
            var shift = (index - 1) * 4;
            var digit = (value >> (int)shift) & 0xFu;
            var character = APTR.ReadUInt8(
                APTR.FromPointer(hexDigits + digit), 0);
            position = AppendByte(scratch, position, character);
        }
        return position;
    }

    private static uint AppendSigned(APTR scratch, uint cursor, uint value)
    {
        var number = value;
        var position = cursor;
        if ((number & 0x80000000u) != 0)
        {
            position = AppendByte(scratch, position, (byte)'-');
            number = 0u - number;
        }
        return AppendUnsigned(scratch, position, number);
    }

    private static uint AppendUnsigned(APTR scratch, uint cursor,
        uint value)
    {
        var position = cursor;
        var number = value;
        var place = 1000000000u;
        var started = false;
        for (var index = 0; index < 10 &&
             position < RegisterCheckScratchBytes - 2; index++)
        {
            var digit = 0u;
            while (number >= place)
            {
                number -= place;
                digit++;
            }
            if (digit != 0 || started || index == 9)
            {
                position = AppendByte(scratch, position,
                    (byte)('0' + digit));
                started = true;
            }
            if (index == 0) place = 100000000u;
            else if (index == 1) place = 10000000u;
            else if (index == 2) place = 1000000u;
            else if (index == 3) place = 100000u;
            else if (index == 4) place = 10000u;
            else if (index == 5) place = 1000u;
            else if (index == 6) place = 100u;
            else if (index == 7) place = 10u;
            else place = 1u;
        }
        return position;
    }

    private static uint AppendByte(APTR scratch, uint cursor, byte value)
    {
        if (cursor >= RegisterCheckScratchBytes - 1) return cursor;
        APTR.WriteUInt8(APTR.FromPointer(scratch.Raw + cursor), 0, value);
        return cursor + 1;
    }

    private static void FinishRegisterCheckLine(APTR scratch, uint cursor)
    {
        var position = AppendByte(scratch, cursor, (byte)'\n');
        APTR.WriteUInt8(APTR.FromPointer(scratch.Raw + position), 0, 0);
    }

    private static void PrintRegisterCheckLine(APTR scratch)
    {
        _ = DOS.FPuts(DOS.Output(), CString.FromPointer(scratch.Raw));
    }

    private static void WriteVerbose(APTR record)
    {
        if (APTR.ReadUInt32(record, StateOffset) ==
            (uint)TaskState.Running)
            APTR.WriteUInt32(record, M68kStackPointerOffset,
                ReadCurrentStackPointer());
        DOS.VPrintf(":     State %ld\n",
            APTR.FromPointer(record.Raw + StateOffset));
        DOS.VPrintf(":   SigWait 0x%08lx\n",
            APTR.FromPointer(record.Raw + SignalWaitOffset));
        DOS.VPrintf(": SigExcept 0x%08lx\n",
            APTR.FromPointer(record.Raw + SignalExceptionOffset));
        DOS.VPrintf(":  SigRecvd 0x%08lx\n",
            APTR.FromPointer(record.Raw + SignalReceivedOffset));
        DOS.VPrintf(":M68k SPUpper 0x%08lx\n",
            APTR.FromPointer(record.Raw + M68kStackUpperOffset));
        DOS.VPrintf(":M68k SPLower 0x%08lx\n",
            APTR.FromPointer(record.Raw + M68kStackLowerOffset));
        DOS.VPrintf(":M68k  SPReg 0x%08lx\n",
            APTR.FromPointer(record.Raw + M68kStackPointerOffset));
        DOS.VPrintf(": PPC SPUpper 0x%08lx\n",
            APTR.FromPointer(record.Raw + PpcStackUpperOffset));
        DOS.VPrintf(": PPC SPLower 0x%08lx\n",
            APTR.FromPointer(record.Raw + PpcStackLowerOffset));
    }

    private static uint ReadTaskAttribute(APTR task, APTR data,
        TaskAttribute attribute, uint fallback)
    {
        APTR.WriteUInt32(data, 0, fallback);
        if (Exec.NewGetTaskAttrsA(task, data, 4, (uint)attribute,
                APTR.Null) == 0)
            APTR.WriteUInt32(data, 0, fallback);
        return APTR.ReadUInt32(data, 0);
    }

    private static void CaptureSystemBoundaries(APTR data, APTR tags)
    {
        _ = Exec.NewGetSystemAttrsA(
            APTR.FromPointer(data.Raw), 4, SystemInfoEmulationStart, tags);
        _ = Exec.NewGetSystemAttrsA(
            APTR.FromPointer(data.Raw + 4), 4, SystemInfoEmulationSize, tags);
        _ = Exec.NewGetSystemAttrsA(
            APTR.FromPointer(data.Raw + 8), 4, SystemInfoModuleStart, tags);
        _ = Exec.NewGetSystemAttrsA(
            APTR.FromPointer(data.Raw + 12), 4, SystemInfoModuleSize, tags);
        _ = Exec.NewGetSystemAttrsA(
            APTR.FromPointer(data.Raw + 16), 4, SystemInfoTaskExitCode, tags);
        _ = Exec.NewGetSystemAttrsA(
            APTR.FromPointer(data.Raw + 20), 4, SystemInfoTaskExitCodeM68k, tags);
    }

    private static uint CopyCString(APTR source, APTR destination,
        uint maximumLength)
    {
        var length = 0u;
        if (source.IsNotNull)
        {
            while (length < maximumLength)
            {
                var value = APTR.ReadUInt8(
                    APTR.FromPointer(source.Raw + length), 0);
                if (value == 0) break;
                APTR.WriteUInt8(APTR.FromPointer(destination.Raw + length), 0,
                    value);
                length++;
            }
        }
        APTR.WriteUInt8(APTR.FromPointer(destination.Raw + length), 0, 0);
        return length;
    }

    private static bool EqualsCString(APTR left, APTR right)
    {
        if (left.IsNull || right.IsNull) return left.Raw == right.Raw;
        var index = 0u;
        while (true)
        {
            var a = APTR.ReadUInt8(APTR.FromPointer(left.Raw + index), 0);
            var b = APTR.ReadUInt8(APTR.FromPointer(right.Raw + index), 0);
            if (a != b) return false;
            if (a == 0) return true;
            index++;
        }
    }

    private static bool EqualsBString(APTR cString, APTR bString)
    {
        if (cString.IsNull || bString.IsNull) return false;
        var length = APTR.ReadUInt8(bString, 0);
        for (var index = 0u; index < length; index++)
        {
            if (APTR.ReadUInt8(APTR.FromPointer(cString.Raw + index), 0) !=
                APTR.ReadUInt8(APTR.FromPointer(bString.Raw + index + 1), 0))
                return false;
        }
        return APTR.ReadUInt8(APTR.FromPointer(cString.Raw + length), 0) == 0;
    }

    private static uint StateText(uint state, uint signalWait) => state switch
    {
        (uint)TaskState.Running => CString.ToUInt32("    run"),
        (uint)TaskState.Ready => CString.ToUInt32("  ready"),
        (uint)TaskState.Waiting => CString.ToUInt32(
            signalWait == 0 ? "   dead" : "   wait"),
        (uint)TaskState.Exception => CString.ToUInt32(" except"),
        (uint)TaskState.Removed => CString.ToUInt32("removed"),
        _ => CString.ToUInt32("-------")
    };

    private static APTR ReadPointer(ref NativeCommandArguments arguments,
        uint index)
    {
        return arguments.TryGetResult(index, out var value)
            ? APTR.FromPointer(value) : APTR.Null;
    }

    private static uint ReadSwitch(ref NativeCommandArguments arguments,
        uint index)
    {
        return arguments.TryGetResult(index, out var value) ? value : 0;
    }

    private static int ReadNumber(ref NativeCommandArguments arguments,
        uint index, out bool present)
    {
        present = false;
        if (!arguments.TryGetResult(index, out var value) || value == 0)
            return 0;
        present = true;
        return unchecked((int)APTR.ReadUInt32(APTR.FromPointer(value), 0));
    }
}
