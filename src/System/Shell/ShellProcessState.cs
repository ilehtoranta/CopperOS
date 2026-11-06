using Amiga;

namespace CopperOS.Shell;

public enum ShellProcessStateFailure
{
    None, Invalid, AlreadyInstalled, NoMemory,
}

/// <summary>References to Shell-owned stream leases held by one process.</summary>
public struct ShellStreamLeases
{
    public APTR Input;
    public APTR Output;
    public APTR Error;
}

/// <summary>Shell-owned process state retained across successive script runners.</summary>
public static class ShellProcessState
{
    private const uint HeaderSize = 32;
    private const uint CommandFileOffset = HeaderSize;
    private const uint CommandFileBytes = 256;
    private const uint StreamLeasesOffset = HeaderSize + CommandFileBytes;
    private const uint ContinuationHeadOffset = StreamLeasesOffset + 12;
    public const uint Size = ContinuationHeadOffset + 4;
    private const uint Magic = 0x53505354; // SPST
    private const uint BindingMagic = 0x53524244; // SRBD
    private const uint Version = 2;
    private const uint BindingVersion = 1;
    private const uint MaximumBindings = 128;
    private const uint MaximumCommand = 31;

    /// <summary>Installs state below future runner bindings and preserves the prior process value.</summary>
    public static bool TryInstall<TPlatform>(ref TPlatform platform, APTR cli, out APTR state)
        where TPlatform : struct, IShellRunnerBindingPlatform =>
        TryInstall(ref platform, cli, out state, out _);

    public static bool TryInstall<TPlatform>(ref TPlatform platform, APTR cli,
        out APTR state, out ShellProcessStateFailure failure)
        where TPlatform : struct, IShellRunnerBindingPlatform
    {
        state = APTR.Null;
        failure = ShellProcessStateFailure.Invalid;
        if (!ShellRunnerBinding.TryFind(ref platform, cli, out var binding, out _, out _) ||
            binding.IsNotNull || !TryFind(ref platform, cli, out var existing))
            return false;
        if (existing.IsNotNull) { failure = ShellProcessStateFailure.AlreadyInstalled; return false; }
        var process = platform.CurrentRunnerProcess();
        var previous = DosProcessCodec.ReadShellPrivate(ref platform, process);
        var commandFile = DosCommandLineInterfaceCodec.Read(ref platform, cli).CommandFile;
        var oldName = commandFile.Address;
        var oldLength = 0u;
        if (oldName.IsNotNull)
        {
            if (!platform.IsMapped(oldName, 1)) return false;
            oldLength = platform.ReadUInt8(oldName);
            if (oldName.Raw > uint.MaxValue - oldLength - 1 ||
                !platform.IsMapped(oldName, oldLength + 1)) return false;
        }
        var allocated = platform.AllocateRunnerMemory(Size);
        if (allocated.IsNull || (allocated.Raw & 3) != 0 || allocated.Raw > uint.MaxValue - Size ||
            !platform.IsRunnerBindingMemory(allocated, Size))
        {
            if (allocated.IsNotNull) platform.FreeRunnerMemory(allocated, Size);
            failure = ShellProcessStateFailure.NoMemory;
            return false;
        }
        platform.Clear(allocated, Size);
        platform.WriteUInt32(allocated, 0, Magic);
        platform.WriteUInt32(allocated, 4, Version);
        platform.WriteUInt32(allocated, 8, process.Raw);
        platform.WriteUInt32(allocated, 12, cli.Raw);
        platform.WriteUInt32(allocated, 16, previous);
        platform.WriteUInt32(allocated, 20, 0);
        platform.WriteUInt32(allocated, 24, commandFile.Raw);
        platform.WriteUInt32(allocated, 28, 0);
        var ownedName = APTR.FromPointer(allocated.Raw + CommandFileOffset);
        platform.WriteUInt8(ownedName, 0, (byte)oldLength);
        for (var i = 0u; i < oldLength; i++)
            platform.WriteUInt8(ownedName, (int)i + 1, platform.ReadUInt8(oldName, (int)i + 1));
        DosCommandLineInterfaceCodec.WriteCommandFile(ref platform, cli, BPTR.FromAddress(ownedName));
        DosProcessCodec.WriteShellPrivate(ref platform, process, allocated.Raw);
        state = allocated;
        failure = ShellProcessStateFailure.None;
        return true;
    }

    /// <summary>Finds state beneath validated nested runner bindings for the current CLI.</summary>
    public static bool TryFind<TPlatform>(ref TPlatform platform, APTR cli, out APTR state)
        where TPlatform : struct, IShellRunnerBindingPlatform
    {
        state = APTR.Null;
        var process = platform.CurrentRunnerProcess();
        if (process.IsNull || cli.IsNull || !DosProcessCodec.IsMapped(ref platform, process) ||
            !DosCommandLineInterfaceCodec.IsMapped(ref platform, cli) ||
            DosProcessCodec.ReadCommandLineInterface(ref platform, process).Address != cli) return false;
        var current = APTR.FromPointer(DosProcessCodec.ReadShellPrivate(ref platform, process));
        for (var depth = 0u; depth < MaximumBindings && current.IsNotNull; depth++)
        {
            if ((current.Raw & 3) != 0 || current.Raw > uint.MaxValue - 4 ||
                !platform.IsRunnerBindingMemory(current, 4)) return true;
            var magic = platform.ReadUInt32(current, 0);
            if (magic != Magic && magic != BindingMagic) return true;
            var size = magic == Magic ? HeaderSize : ShellRunnerBinding.Size;
            if (current.Raw > uint.MaxValue - size || !platform.IsRunnerBindingMemory(current, size) ||
                platform.ReadUInt32(current, 4) != (magic == Magic ? Version : BindingVersion) ||
                platform.ReadUInt32(current, 8) != process.Raw ||
                platform.ReadUInt32(current, 12) != cli.Raw) return false;
            if (magic == Magic) { state = current; return true; }
            if (platform.ReadUInt32(current, 16) == 0) return false;
            current = APTR.FromPointer(platform.ReadUInt32(current, 24));
        }
        return current.IsNull;
    }

    /// <summary>Copies a script path into the Shell-owned CLI command-file BSTR.</summary>
    public static bool TrySetCommandFile<TPlatform>(ref TPlatform platform, APTR cli,
        APTR text, uint length, out bool present) where TPlatform : struct, IShellRunnerBindingPlatform
    {
        present = false;
        if (text.IsNull || length > 255 || text.Raw > uint.MaxValue - (length == 0 ? 1u : length) ||
            !platform.IsMapped(text, length == 0 ? 1u : length) ||
            !TryFind(ref platform, cli, out var state)) return false;
        if (state.IsNull) return true;
        present = true;
        if (state.Raw > uint.MaxValue - Size || !platform.IsRunnerBindingMemory(state, Size)) return false;
        var name = APTR.FromPointer(state.Raw + CommandFileOffset);
        if (DosCommandLineInterfaceCodec.Read(ref platform, cli).CommandFile != BPTR.FromAddress(name)) return false;
        var target = APTR.FromPointer(name.Raw + 1);
        if (text.Raw < target.Raw && text.Raw + length > target.Raw)
        {
            for (var i = length; i != 0; i--)
                platform.WriteUInt8(target, (int)i - 1, platform.ReadUInt8(text, (int)i - 1));
        }
        else
        {
            for (var i = 0u; i < length; i++)
                platform.WriteUInt8(target, (int)i, platform.ReadUInt8(text, (int)i));
        }
        platform.WriteUInt8(name, 0, (byte)length);
        return true;
    }

    /// <summary>Reads the enabled bit; absent state is reported separately.</summary>
    public static bool TryGetCommand<TPlatform>(ref TPlatform platform, APTR cli, uint command,
        out bool present, out uint enabled) where TPlatform : struct, IShellRunnerBindingPlatform
    {
        present = false; enabled = 0;
        if (command == 0 || command > MaximumCommand || !TryFind(ref platform, cli, out var state)) return false;
        if (state.IsNull) return true;
        present = true;
        enabled = (platform.ReadUInt32(state, 20) & (1u << (int)(command - 1))) == 0 ? 1u : 0u;
        return true;
    }

    /// <summary>Changes a built-in command without allocating or modifying DOS-owned state.</summary>
    public static bool TrySetCommand<TPlatform>(ref TPlatform platform, APTR cli, uint command,
        uint enabled, out bool present) where TPlatform : struct, IShellRunnerBindingPlatform
    {
        present = false;
        if (command == 0 || command > MaximumCommand || enabled > 1 ||
            !TryFind(ref platform, cli, out var state)) return false;
        if (state.IsNull) return true;
        present = true;
        var bit = 1u << (int)(command - 1);
        var mask = platform.ReadUInt32(state, 20);
        platform.WriteUInt32(state, 20, enabled == 0 ? mask | bit : mask & ~bit);
        return true;
    }

    /// <summary>Records an explicit request to end this Shell process.</summary>
    public static bool TryRequestEnd<TPlatform>(ref TPlatform platform, APTR cli)
        where TPlatform : struct, IShellRunnerBindingPlatform
    {
        if (!TryFind(ref platform, cli, out var state) || state.IsNull) return false;
        platform.WriteUInt32(state, 28, 1);
        return true;
    }

    /// <summary>Distinguishes EndCLI from EOF on a noninteractive input.</summary>
    public static bool TryEndRequested<TPlatform>(ref TPlatform platform, APTR cli,
        out bool requested) where TPlatform : struct, IShellRunnerBindingPlatform
    {
        requested = false;
        if (!TryFind(ref platform, cli, out var state) || state.IsNull) return false;
        requested = platform.ReadUInt32(state, 28) != 0;
        return true;
    }

    /// <summary>Reads the stream leases owned by the current Shell process.</summary>
    public static bool TryGetStreamLeases<TPlatform>(ref TPlatform platform, APTR cli,
        out ShellStreamLeases leases) where TPlatform : struct, IShellRunnerBindingPlatform
    {
        leases = default;
        if (!TryFind(ref platform, cli, out var state)) return false;
        if (state.IsNull) return true;
        if (state.Raw > uint.MaxValue - Size || !platform.IsRunnerBindingMemory(state, Size)) return false;
        leases.Input = APTR.FromPointer(platform.ReadUInt32(state, (int)StreamLeasesOffset));
        leases.Output = APTR.FromPointer(platform.ReadUInt32(state, (int)StreamLeasesOffset + 4));
        leases.Error = APTR.FromPointer(platform.ReadUInt32(state, (int)StreamLeasesOffset + 8));
        return true;
    }

    /// <summary>Installs leases before a newly created Shell runs commands.</summary>
    public static bool TrySetStreamLeases<TPlatform>(ref TPlatform platform, APTR cli,
        APTR state, in ShellStreamLeases leases) where TPlatform : struct, IShellRunnerBindingPlatform
    {
        if (state.IsNull || state.Raw > uint.MaxValue - Size ||
            !TryFind(ref platform, cli, out var found) || found != state ||
            !platform.IsRunnerBindingMemory(state, Size)) return false;
        platform.WriteUInt32(state, (int)StreamLeasesOffset, leases.Input.Raw);
        platform.WriteUInt32(state, (int)StreamLeasesOffset + 4, leases.Output.Raw);
        platform.WriteUInt32(state, (int)StreamLeasesOffset + 8, leases.Error.Raw);
        return true;
    }

    /// <summary>Reads the continuation chain owned by the current Shell process.</summary>
    public static bool TryGetContinuationHead<TPlatform>(ref TPlatform platform, APTR cli,
        out bool present, out APTR head) where TPlatform : struct, IShellRunnerBindingPlatform
    {
        present = false;
        head = APTR.Null;
        if (!TryFind(ref platform, cli, out var state)) return false;
        if (state.IsNull) return true;
        if (state.Raw > uint.MaxValue - Size || !platform.IsRunnerBindingMemory(state, Size)) return false;
        present = true;
        head = APTR.FromPointer(platform.ReadUInt32(state, (int)ContinuationHeadOffset));
        return true;
    }

    /// <summary>Changes the current Shell's continuation chain head.</summary>
    public static bool TrySetContinuationHead<TPlatform>(ref TPlatform platform, APTR cli,
        APTR head) where TPlatform : struct, IShellRunnerBindingPlatform
    {
        if (!TryFind(ref platform, cli, out var state) || state.IsNull ||
            state.Raw > uint.MaxValue - Size || !platform.IsRunnerBindingMemory(state, Size)) return false;
        platform.WriteUInt32(state, (int)ContinuationHeadOffset, head.Raw);
        return true;
    }

    /// <summary>Removes state only when all runner bindings have been released.</summary>
    public static bool TryRemove<TPlatform>(ref TPlatform platform, APTR cli, APTR state)
        where TPlatform : struct, IShellRunnerBindingPlatform
    {
        var process = platform.CurrentRunnerProcess();
        if (state.IsNull || process.IsNull || state.Raw > uint.MaxValue - Size ||
            !platform.IsRunnerBindingMemory(state, Size) ||
            !TryFind(ref platform, cli, out var found) || found != state ||
            DosProcessCodec.ReadShellPrivate(ref platform, process) != state.Raw ||
            platform.ReadUInt32(state, (int)ContinuationHeadOffset) != 0) return false;
        var name = APTR.FromPointer(state.Raw + CommandFileOffset);
        if (DosCommandLineInterfaceCodec.Read(ref platform, cli).CommandFile != BPTR.FromAddress(name)) return false;
        var previous = platform.ReadUInt32(state, 16);
        var commandFile = BPTR.FromRaw(platform.ReadUInt32(state, 24));
        DosCommandLineInterfaceCodec.WriteCommandFile(ref platform, cli, commandFile);
        DosProcessCodec.WriteShellPrivate(ref platform, process, previous);
        platform.WriteUInt32(state, 0, 0);
        platform.FreeRunnerMemory(state, Size);
        return true;
    }
}
