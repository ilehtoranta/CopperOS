using Amiga;

namespace CopperOS.Shell;

public enum ShellRunnerBindingFailure
{
    None, Invalid, AlreadyBound, NoMemory,
}

/// <summary>Public process and Exec memory services used by a Shell runner binding.</summary>
public interface IShellRunnerBindingPlatform : IShellRunnerMemory
{
    APTR CurrentRunnerProcess();
    bool IsRunnerBindingMemory(APTR address, uint size);
}

/// <summary>
/// Owns the active runner's binding in the current Process's Shell-private
/// field. The prior value is restored when the runner is unpublished.
/// </summary>
public static class ShellRunnerBinding
{
    public const uint Size = 28;
    private const uint Magic = 0x53524244; // SRBD
    private const uint Version = 1;

    /// <summary>Finds the runner belonging to this process and CLI, if any.</summary>
    public static bool TryFind<TPlatform>(ref TPlatform platform, APTR cli,
        out APTR binding, out APTR frame, out APTR runner)
        where TPlatform : struct, IShellRunnerBindingPlatform
    {
        binding = APTR.Null; frame = APTR.Null; runner = APTR.Null;
        var process = platform.CurrentRunnerProcess();
        if (process.IsNull || cli.IsNull || !DosProcessCodec.IsMapped(ref platform, process) ||
            DosProcessCodec.ReadCommandLineInterface(ref platform, process).Address != cli) return false;
        var candidate = APTR.FromPointer(DosProcessCodec.ReadShellPrivate(ref platform, process));
        if (candidate.IsNull || (candidate.Raw & 3) != 0 ||
            candidate.Raw > uint.MaxValue - Size || !platform.IsRunnerBindingMemory(candidate, Size) ||
            platform.ReadUInt32(candidate, 0) != Magic) return true;
        if (platform.ReadUInt32(candidate, 4) != Version ||
            platform.ReadUInt32(candidate, 8) != process.Raw ||
            platform.ReadUInt32(candidate, 12) != cli.Raw ||
            platform.ReadUInt32(candidate, 16) == 0) return false;
        binding = candidate;
        frame = APTR.FromPointer(platform.ReadUInt32(candidate, 16));
        runner = APTR.FromPointer(platform.ReadUInt32(candidate, 20));
        return true;
    }

    /// <summary>Publishes one Shell-owned runner for the current process.</summary>
    public static bool TryPublish<TPlatform>(ref TPlatform platform, APTR cli,
        APTR frame, APTR runner) where TPlatform : struct, IShellRunnerBindingPlatform
        => TryPublish(ref platform, cli, frame, runner, out _);

    public static bool TryPublish<TPlatform>(ref TPlatform platform, APTR cli,
        APTR frame, APTR runner, out ShellRunnerBindingFailure failure)
        where TPlatform : struct, IShellRunnerBindingPlatform
    {
        failure = ShellRunnerBindingFailure.Invalid;
        if (frame.IsNull || runner.IsNull ||
            !TryFind(ref platform, cli, out var existing, out _, out _))
            return false;
        if (existing.IsNotNull) { failure = ShellRunnerBindingFailure.AlreadyBound; return false; }
        return Publish(ref platform, cli, frame, runner, out failure);
    }

    /// <summary>Pushes a frame-only binding, preserving any enclosing frame.</summary>
    public static bool TryPublishFrame<TPlatform>(ref TPlatform platform, APTR cli,
        APTR frame, out ShellRunnerBindingFailure failure)
        where TPlatform : struct, IShellRunnerBindingPlatform
    {
        failure = ShellRunnerBindingFailure.Invalid;
        if (frame.IsNull || frame.Raw > uint.MaxValue - 4 || !platform.IsMapped(frame, 4) ||
            !TryFind(ref platform, cli, out var existing, out var current, out _))
            return false;
        if (existing.IsNotNull && current == frame) { failure = ShellRunnerBindingFailure.None; return true; }
        return Publish(ref platform, cli, frame, APTR.Null, out failure);
    }

    private static bool Publish<TPlatform>(ref TPlatform platform, APTR cli,
        APTR frame, APTR runner, out ShellRunnerBindingFailure failure)
        where TPlatform : struct, IShellRunnerBindingPlatform
    {
        failure = ShellRunnerBindingFailure.Invalid;
        var process = platform.CurrentRunnerProcess();
        var previous = DosProcessCodec.ReadShellPrivate(ref platform, process);
        var binding = platform.AllocateRunnerMemory(Size);
        if (binding.IsNull || (binding.Raw & 3) != 0 || binding.Raw > uint.MaxValue - Size ||
            !platform.IsRunnerBindingMemory(binding, Size))
        {
            if (binding.IsNotNull) platform.FreeRunnerMemory(binding, Size);
            failure = ShellRunnerBindingFailure.NoMemory;
            return false;
        }
        platform.WriteUInt32(binding, 0, Magic);
        platform.WriteUInt32(binding, 4, Version);
        platform.WriteUInt32(binding, 8, process.Raw);
        platform.WriteUInt32(binding, 12, cli.Raw);
        platform.WriteUInt32(binding, 16, frame.Raw);
        platform.WriteUInt32(binding, 20, runner.Raw);
        platform.WriteUInt32(binding, 24, previous);
        DosProcessCodec.WriteShellPrivate(ref platform, process, binding.Raw);
        failure = ShellRunnerBindingFailure.None;
        return true;
    }

    /// <summary>Restores the previous Shell-private value and frees the binding.</summary>
    public static bool TryRemove<TPlatform>(ref TPlatform platform, APTR cli,
        APTR frame, APTR runner) where TPlatform : struct, IShellRunnerBindingPlatform
    {
        if (runner.IsNull || !TryFind(ref platform, cli, out var binding, out var foundFrame, out var foundRunner) ||
            binding.IsNull || foundFrame != frame || foundRunner != runner) return false;
        return Remove(ref platform, binding);
    }

    /// <summary>Pops a frame-only binding and restores the previous value.</summary>
    public static bool TryRemoveFrame<TPlatform>(ref TPlatform platform, APTR cli,
        APTR frame) where TPlatform : struct, IShellRunnerBindingPlatform
    {
        if (!TryFind(ref platform, cli, out var binding, out var foundFrame, out var runner) ||
            binding.IsNull || foundFrame != frame || runner.IsNotNull) return false;
        return Remove(ref platform, binding);
    }

    private static bool Remove<TPlatform>(ref TPlatform platform, APTR binding)
        where TPlatform : struct, IShellRunnerBindingPlatform
    {
        var process = platform.CurrentRunnerProcess();
        var previous = platform.ReadUInt32(binding, 24);
        DosProcessCodec.WriteShellPrivate(ref platform, process, previous);
        platform.WriteUInt32(binding, 0, 0);
        platform.FreeRunnerMemory(binding, Size);
        return true;
    }
}
