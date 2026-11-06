using Amiga;

namespace CopperOS.Shell;

/// <summary>Public file operations needed to retire a Shell runner.</summary>
public interface IShellRunnerCleanupPlatform : IShellRunnerMemory
{
    void CloseRunnerInput(BPTR input);
    void DeleteRunnerPath(APTR path);
}

/// <summary>Releases resources owned by a completed Shell runner.</summary>
public static class ShellRunnerCleanup
{
    private const uint Magic = 0x5352554e;
    private const uint Version = 9;

    /// <summary>
    /// Consumes a previously validated runner record. Packed workspace is
    /// embedded after the record and is released by the record's owner; all
    /// other allocations and handles are retired here. The caller must own
    /// the runner exclusively and release the record after this returns.
    /// </summary>
    public static bool Release<TPlatform>(ref TPlatform platform, APTR runner,
        bool packedWorkspace) where TPlatform : struct, IShellRunnerCleanupPlatform
    {
        if (!CanRelease(ref platform, runner, packedWorkspace)) return false;

        // Mark consumed before invoking external operations, so a reentrant
        // cleanup cannot close or free the same resources a second time.
        platform.WriteUInt32(runner, 0, 0);
        var inputOwnership = platform.ReadUInt32(runner, 88);
        if (inputOwnership == 1) Close(ref platform, platform.ReadUInt32(runner, 16));
        else if (inputOwnership == 2)
            _ = ShellCommandSource.Release(ref platform, BPTR.FromRaw(platform.ReadUInt32(runner, 16)));
        Close(ref platform, platform.ReadUInt32(runner, 128));
        var temporary = platform.ReadUInt32(runner, 108);
        if (platform.ReadUInt32(runner, 112) != 0)
            platform.DeleteRunnerPath(APTR.FromPointer(temporary));
        if (platform.ReadUInt32(runner, 132) != 0)
            platform.DeleteRunnerPath(APTR.FromPointer(temporary + 256));

        Free(ref platform, runner, 12, ShellScriptFrameCodec.Size);
        if (!packedWorkspace)
        {
            Free(ref platform, runner, 20, ShellScriptWorkspaceLayout.LineBytes);
            Free(ref platform, runner, 24, ShellScriptWorkspaceLayout.NameBytes);
            Free(ref platform, runner, 28, ShellScriptWorkspaceLayout.TokenBytes);
            Free(ref platform, runner, 32, ShellScriptWorkspaceLayout.SmallBytes);
            Free(ref platform, runner, 36, ShellScriptWorkspaceLayout.SmallBytes);
            Free(ref platform, runner, 40, ShellScriptWorkspaceLayout.SmallBytes);
            Free(ref platform, runner, 44, ShellScriptWorkspaceLayout.SmallBytes);
            Free(ref platform, runner, 52, ShellScriptWorkspaceLayout.LineBytes);
            Free(ref platform, runner, 56, ShellScriptWorkspaceLayout.SmallBytes);
            Free(ref platform, runner, 60, ShellScriptWorkspaceLayout.SmallBytes);
            Free(ref platform, runner, 64, ShellScriptWorkspaceLayout.SmallBytes);
            Free(ref platform, runner, 68, ShellScriptWorkspaceLayout.LineBytes);
            Free(ref platform, runner, 72, ShellScriptWorkspaceLayout.SmallBytes);
            Free(ref platform, runner, 108, ShellScriptWorkspaceLayout.SmallBytes);
        }
        Free(ref platform, runner, 48, platform.ReadUInt32(runner, 136));
        Free(ref platform, runner, 92, platform.ReadUInt32(runner, 96));
        Free(ref platform, runner, 100, ShellRunnerBuffers.KeyBytes);
        var sourceLength = platform.ReadUInt32(runner, 120);
        Free(ref platform, runner, 116, sourceLength == 0 ? 0 : sourceLength + 1);
        Free(ref platform, runner, 124, ShellRunnerBuffers.PromptBytes);
        return true;
    }

    /// <summary>Checks the record before its discoverable binding is removed.</summary>
    public static bool CanRelease<TPlatform>(ref TPlatform platform, APTR runner,
        bool packedWorkspace) where TPlatform : struct, IShellRunnerCleanupPlatform =>
        runner.IsNotNull && runner.Raw <= uint.MaxValue - ShellRunnerBuffers.RecordSize &&
        platform.IsMapped(runner, ShellRunnerBuffers.RecordSize) &&
        platform.ReadUInt32(runner, 0) == Magic && platform.ReadUInt32(runner, 4) == Version &&
        platform.ReadUInt32(runner, 88) <= 2 &&
        (platform.ReadUInt32(runner, 88) != 2 ||
            ShellCommandSource.IsValid(ref platform, BPTR.FromRaw(platform.ReadUInt32(runner, 16)))) &&
        (!packedWorkspace || ValidPackedWorkspace(ref platform, runner));

    private static bool ValidPackedWorkspace<TPlatform>(ref TPlatform platform, APTR runner)
        where TPlatform : struct, IShellRunnerCleanupPlatform
    {
        if (runner.Raw > uint.MaxValue - ShellRunnerBuffers.RecordSize - ShellScriptWorkspaceLayout.Bytes)
            return false;
        var start = runner.Raw + ShellRunnerBuffers.RecordSize;
        return platform.IsMapped(APTR.FromPointer(start), ShellScriptWorkspaceLayout.Bytes) &&
            platform.ReadUInt32(runner, 20) == start &&
            platform.ReadUInt32(runner, 24) == start + ShellScriptWorkspaceLayout.Name &&
            platform.ReadUInt32(runner, 28) == start + ShellScriptWorkspaceLayout.Token &&
            platform.ReadUInt32(runner, 32) == start + ShellScriptWorkspaceLayout.First &&
            platform.ReadUInt32(runner, 36) == start + ShellScriptWorkspaceLayout.Second &&
            platform.ReadUInt32(runner, 40) == start + ShellScriptWorkspaceLayout.Third &&
            platform.ReadUInt32(runner, 44) == start + ShellScriptWorkspaceLayout.Fourth &&
            platform.ReadUInt32(runner, 52) == start + ShellScriptWorkspaceLayout.Redirection &&
            platform.ReadUInt32(runner, 56) == start + ShellScriptWorkspaceLayout.Input &&
            platform.ReadUInt32(runner, 60) == start + ShellScriptWorkspaceLayout.Output &&
            platform.ReadUInt32(runner, 64) == start + ShellScriptWorkspaceLayout.Error &&
            platform.ReadUInt32(runner, 68) == start + ShellScriptWorkspaceLayout.Alias &&
            platform.ReadUInt32(runner, 72) == start + ShellScriptWorkspaceLayout.Lookup &&
            platform.ReadUInt32(runner, 108) == start + ShellScriptWorkspaceLayout.Temporary;
    }

    private static void Close<TPlatform>(ref TPlatform platform, uint input)
        where TPlatform : struct, IShellRunnerCleanupPlatform
    {
        if (input != 0) platform.CloseRunnerInput(BPTR.FromRaw(input));
    }

    private static void Free<TPlatform>(ref TPlatform platform, APTR runner, int offset, uint size)
        where TPlatform : struct, IShellRunnerCleanupPlatform
    {
        var address = APTR.FromPointer(platform.ReadUInt32(runner, offset));
        if (address.IsNotNull) platform.FreeRunnerMemory(address, size);
    }
}
