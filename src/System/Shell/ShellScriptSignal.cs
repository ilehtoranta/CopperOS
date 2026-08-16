using Amiga;
using System;

namespace CopperOS.Shell;

/// <summary>Exec/DOS events which interrupt one Shell script step.</summary>
[Flags]
public enum ShellScriptSignalFlags : uint
{
    None = 0,
    Break = 1,
    CtrlC = 2,
    CtrlD = 4,
    Terminated = 8,
}

/// <summary>Fixed-width event returned by the platform signal boundary.</summary>
public struct ShellScriptSignalEvent
{
    public ShellScriptSignalEvent(
        ShellScriptSignalFlags flags,
        int result,
        uint sequence)
    {
        Flags = flags;
        Result = result;
        Sequence = sequence;
    }

    public ShellScriptSignalFlags Flags { get; set; }
    public int Result { get; set; }
    public uint Sequence { get; set; }
}

/// <summary>Guest-resident pending/acknowledged signal state.</summary>
public struct ShellScriptSignalState
{
    public ShellScriptSignalFlags Pending;
    public int Result;
    public uint Sequence;
    public uint AcknowledgedSequence;
}

/// <summary>Codec for the optional signal record attached to a script frame.</summary>
public static class ShellScriptSignalCodec
{
    public const uint Magic = 0x5343_5347;
    public const uint Version = 1;
    public const uint Size = 24;

    public static bool Initialize<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellScriptSignalState initial)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidFlags(initial.Pending) || record.IsNull ||
            !platform.IsMapped(record, Size))
            return false;
        platform.Clear(record, Size);
        platform.WriteUInt32(record, 0, Magic);
        platform.WriteUInt32(record, 4, Version);
        WriteState(ref platform, record, in initial);
        return true;
    }

    public static bool TryRead<TPlatform>(
        ref TPlatform platform,
        APTR record,
        out ShellScriptSignalState state)
        where TPlatform : struct, IShellPlatform
    {
        state = default;
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            platform.ReadUInt32(record, 0) != Magic ||
            platform.ReadUInt32(record, 4) != Version)
            return false;
        state.Pending = (ShellScriptSignalFlags)
            platform.ReadUInt32(record, 8);
        state.Result = unchecked((int)platform.ReadUInt32(record, 12));
        state.Sequence = platform.ReadUInt32(record, 16);
        state.AcknowledgedSequence = platform.ReadUInt32(record, 20);
        return ValidFlags(state.Pending);
    }

    public static bool TryRecord<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellScriptSignalEvent signal)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidFlags(signal.Flags) ||
            !TryRead(ref platform, record, out var state))
            return false;
        state.Pending = signal.Flags;
        state.Result = signal.Result;
        state.Sequence = signal.Sequence;
        return WriteState(ref platform, record, in state);
    }

    public static bool TryAcknowledge<TPlatform>(
        ref TPlatform platform,
        APTR record,
        uint sequence)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, record, out var state) ||
            state.Sequence != sequence)
            return false;
        state.Pending = ShellScriptSignalFlags.None;
        state.AcknowledgedSequence = sequence;
        return WriteState(ref platform, record, in state);
    }

    private static bool ValidFlags(ShellScriptSignalFlags flags) =>
        ((uint)flags & ~(uint)(ShellScriptSignalFlags.Break |
            ShellScriptSignalFlags.CtrlC |
            ShellScriptSignalFlags.CtrlD |
            ShellScriptSignalFlags.Terminated)) == 0;

    private static bool WriteState<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellScriptSignalState state)
        where TPlatform : struct, IShellPlatform
    {
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            !ValidFlags(state.Pending))
            return false;
        platform.WriteUInt32(record, 8, (uint)state.Pending);
        platform.WriteUInt32(record, 12, unchecked((uint)state.Result));
        platform.WriteUInt32(record, 16, state.Sequence);
        platform.WriteUInt32(record, 20, state.AcknowledgedSequence);
        return true;
    }
}
