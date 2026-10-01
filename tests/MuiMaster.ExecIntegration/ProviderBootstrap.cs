using Amiga;
using CopperSharp.Compiler;
using CopperStart.Exec;

namespace CopperOS.MuiMaster.ExecIntegration;

// Only the providers fixture opts into normal task admission. Earlier fixture
// modes retain their original bootstrap state and execution context.
public static class ProviderBootstrap
{
    public const string ExportName = "copperos.mui.test.provider-task-context";

    [M68kExport(ExportName)]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint InitializeTaskContext()
    {
        var memory = new CopperSharpRomMemoryPlatform(ExecBootstrap.SysBase);
        var execBase = APTR.FromPointer(ExecBootstrap.SysBase);
        if (ExecBaseCodec.ReadInterruptDisableNesting(ref memory, execBase) != 0 ||
            ExecBaseCodec.ReadTaskDisableNesting(ref memory, execBase) != 0) return 0;
        var task = ExecBaseCodec.ReadThisTask(ref memory, execBase);
        if (task.Raw != ExecBootstrap.FixtureTask ||
            ExecTaskCodec.ReadState(ref memory, task) != TaskState.Running) return 0;
        var record = ExecTaskCodec.Read(ref memory, task);
        record.IDNestCount = -1;
        record.TaskDisableNestCount = -1;
        ExecTaskCodec.Write(ref memory, task, record);
        // Public Exec calls change the initial zero nesting to the normal -1.
        global::Amiga.Exec.Enable();
        global::Amiga.Exec.Permit();
        return ExecBaseCodec.ReadInterruptDisableNesting(ref memory, execBase) == -1 &&
            ExecBaseCodec.ReadTaskDisableNesting(ref memory, execBase) == -1 ? 1u : 0u;
    }
}
