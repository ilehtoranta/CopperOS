using Amiga;
using CopperSharp.Compiler;
using CopperStart.Exec;

namespace CopperOS.MuiMaster.ExecIntegration;

// Test-only bootstrap compiled into a separate image. MUI contains neither
// this code nor CopperStart implementations; its calls cross the Exec LVOs.
public static class ExecBootstrap
{
    public const uint SysBase = 0x00010000;
    public const uint HeapStart = 0x00100000;
    public const uint HeapBytes = 0x00090000;
    public const uint FixtureTask = 0x00030000;
    public const string ExportName = "copperos.mui.test.exec-bootstrap";

    public static uint KernelEntry() => 0;

    [M68kExport(ExportName)]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint Bootstrap()
    {
        var memory = new CopperSharpRomMemoryPlatform(SysBase);
        var sysBase = APTR.FromPointer(SysBase);
        memory.Clear(sysBase, ExecBase.Size);
        APTR.WriteUInt32(APTR.FromPointer(4), 0, SysBase);
        ExecMemoryCore.AddMemList<CopperSharpRomMemoryPlatform, TlsfPolicy>(
            ref memory, sysBase, HeapBytes, global::Amiga.Exec.MemoryFlags.Public,
            0, APTR.FromPointer(HeapStart), APTR.Null);
        global::Amiga.Task taskRecord = default;
        taskRecord.Node.Type = (byte)NodeType.Task;
        taskRecord.State = TaskState.Ready;
        taskRecord.SignalAllocated = 0x1FFFFFFF;
        var task = APTR.FromPointer(FixtureTask);
        ExecTaskCodec.Write(ref memory, task, taskRecord);
        var ready = ExecBaseCodec.TaskReadyAddress(sysBase);
        ExecListCore.Initialize(ref memory, ready);
        ExecListCore.Initialize(ref memory, ExecBaseCodec.TaskWaitAddress(sysBase));
        ExecListCore.Initialize(ref memory, ExecBaseCodec.LibraryListAddress(sysBase));
        ExecListCore.AddTail(ref memory, ready, task);
        if (ExecSchedulerCore.Dispatch(ref memory, sysBase) != task) return 0;
        return ExecNativeProductionEntrypoints.InstallVectorSurfaceEntry(SysBase);
    }
}
