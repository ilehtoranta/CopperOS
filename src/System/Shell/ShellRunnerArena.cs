using Amiga;

namespace CopperOS.Shell;

/// <summary>Allocates the runner record and optional fixed workspace for Shell.</summary>
public static class ShellRunnerArena
{
    public static bool TryAllocate<TPlatform>(ref TPlatform platform, bool packed,
        out APTR runner, out APTR workspace) where TPlatform : struct, IShellRunnerMemory
    {
        runner = APTR.Null; workspace = APTR.Null;
        var size = ShellRunnerBuffers.RecordSize + (packed ? ShellScriptWorkspaceLayout.Bytes : 0);
        var allocation = platform.AllocateRunnerMemory(size);
        if (allocation.IsNull || (allocation.Raw & 3) != 0 ||
            allocation.Raw > uint.MaxValue - size || !platform.IsMapped(allocation, size))
        {
            if (allocation.IsNotNull) platform.FreeRunnerMemory(allocation, size);
            return false;
        }
        platform.Clear(allocation, size);
        runner = allocation;
        if (packed) workspace = APTR.FromPointer(allocation.Raw + ShellRunnerBuffers.RecordSize);
        return true;
    }

    /// <summary>Releases the record after all dependent resources are retired.</summary>
    public static void Free<TPlatform>(ref TPlatform platform, APTR runner, bool packed)
        where TPlatform : struct, IShellRunnerMemory
    {
        if (runner.IsNotNull)
            platform.FreeRunnerMemory(runner, ShellRunnerBuffers.RecordSize +
                (packed ? ShellScriptWorkspaceLayout.Bytes : 0));
    }
}
