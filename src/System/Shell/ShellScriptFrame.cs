using Amiga;
using System;

namespace CopperOS.Shell;

/// <summary>Guest-resident control flags for one bounded script frame.</summary>
[Flags]
public enum ShellScriptFrameFlags : uint
{
    None = 0,
    Active = 1,
    Skipping = 2,
    ConditionFalse = 4,
    ElseSeen = 8,
    QuitRequested = 16,
    EndRequested = 32,
    FailureLimitSet = 64,
}

/// <summary>Pending control request recorded in a script frame.</summary>
public enum ShellScriptFrameControl : uint
{
    None = 0,
    Else = 1,
    EndIf = 2,
    EndSkip = 3,
    EndCli = 4,
    EndShell = 5,
    Quit = 6,
}

/// <summary>
/// Fixed-width state for one active Shell script frame.
///
/// The structure contains only guest pointers, handles, integers, and flags.
/// It is intentionally mutable so a native frame codec can update it without
/// managed allocation or an object graph.
/// </summary>
public struct ShellScriptFrameState
{
    public APTR Parent;
    public APTR Cli;
    public BPTR Input;
    public BPTR Output;
    public BPTR Error;
    public BPTR CurrentDirectory;
    public uint CurrentLine;
    public uint CurrentOffset;
    public uint FailureLimit;
    public int LastResult;
    public int QuitResult;
    public uint Condition;
    public ShellScriptFrameControl Control;
    public ShellScriptFrameFlags Flags;
    public uint LabelCount;
    public APTR ControlTop;
    public APTR InputState;
    public APTR LabelTop;
    public APTR SignalState;
    public APTR PendingCommand;
    public uint PendingNextLine;
    public uint PendingNextOffset;
}

/// <summary>
/// Encodes and updates a script frame in caller-supplied guest memory.
/// </summary>
public static class ShellScriptFrameCodec
{
    public const uint Magic = 0x5343_4652;
    public const uint Version = 1;
    public const uint Size = 96;

    public static bool Initialize<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        in ShellScriptFrameState initial)
        where TPlatform : struct, IShellPlatform
    {
        if (frame.IsNull || !platform.IsMapped(frame, Size))
            return false;
        platform.Clear(frame, Size);
        WriteUInt32(ref platform, frame, 0, Magic);
        WriteUInt32(ref platform, frame, 4, Version);
        WriteState(ref platform, frame, in initial);
        return true;
    }

    public static bool TryRead<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        out ShellScriptFrameState state)
        where TPlatform : struct, IShellPlatform
    {
        state = default;
        if (frame.IsNull || !platform.IsMapped(frame, Size) ||
            platform.ReadUInt32(frame, 0) != Magic ||
            platform.ReadUInt32(frame, 4) != Version)
            return false;
        state.Parent = APTR.FromPointer(platform.ReadUInt32(frame, 8));
        state.Cli = APTR.FromPointer(platform.ReadUInt32(frame, 12));
        state.Input = BPTR.FromRaw(platform.ReadUInt32(frame, 16));
        state.Output = BPTR.FromRaw(platform.ReadUInt32(frame, 20));
        state.Error = BPTR.FromRaw(platform.ReadUInt32(frame, 24));
        state.CurrentDirectory = BPTR.FromRaw(
            platform.ReadUInt32(frame, 28));
        state.CurrentLine = platform.ReadUInt32(frame, 32);
        state.CurrentOffset = platform.ReadUInt32(frame, 36);
        state.FailureLimit = platform.ReadUInt32(frame, 40);
        state.LastResult = unchecked((int)platform.ReadUInt32(frame, 44));
        state.QuitResult = unchecked((int)platform.ReadUInt32(frame, 48));
        state.Condition = platform.ReadUInt32(frame, 52);
        state.Control = (ShellScriptFrameControl)platform.ReadUInt32(frame, 56);
        state.Flags = (ShellScriptFrameFlags)platform.ReadUInt32(frame, 60);
        state.LabelCount = platform.ReadUInt32(frame, 64);
        state.ControlTop = APTR.FromPointer(platform.ReadUInt32(frame, 68));
        state.InputState = APTR.FromPointer(platform.ReadUInt32(frame, 72));
        state.LabelTop = APTR.FromPointer(platform.ReadUInt32(frame, 76));
        state.SignalState = APTR.FromPointer(platform.ReadUInt32(frame, 80));
        state.PendingCommand = APTR.FromPointer(platform.ReadUInt32(frame, 84));
        state.PendingNextLine = platform.ReadUInt32(frame, 88);
        state.PendingNextOffset = platform.ReadUInt32(frame, 92);
        return true;
    }

    /// <summary>Reads only the control-chain head for freestanding callers.</summary>
    public static bool TryReadControlTop<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        out uint controlTopRaw)
        where TPlatform : struct, IShellPlatform
    {
        controlTopRaw = 0;
        if (frame.IsNull || !platform.IsMapped(frame, Size) ||
            platform.ReadUInt32(frame, 0) != Magic ||
            platform.ReadUInt32(frame, 4) != Version)
            return false;
        controlTopRaw = platform.ReadUInt32(frame, 68);
        return true;
    }

    public static bool TryRecordResult<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        int result)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, frame, out var state))
            return false;
        state.LastResult = result;
        return WriteState(ref platform, frame, in state);
    }

    public static bool TryRecordCondition<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        uint condition,
        uint conditionFalse)
        where TPlatform : struct, IShellPlatform
    {
        if (conditionFalse > 1 || !TryRead(ref platform, frame, out var state))
            return false;
        state.Condition = condition;
        if (conditionFalse != 0)
            state.Flags |= ShellScriptFrameFlags.ConditionFalse |
                ShellScriptFrameFlags.Skipping;
        else
            state.Flags &= ~(ShellScriptFrameFlags.ConditionFalse |
                ShellScriptFrameFlags.Skipping);
        return WriteState(ref platform, frame, in state);
    }

    /// <summary>Updates the condition value without changing branch flags.</summary>
    public static bool TrySetCondition<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        uint condition)
        where TPlatform : struct, IShellPlatform
    {
        if (condition == 0 || !TryRead(ref platform, frame, out var state))
            return false;
        state.Condition = condition;
        return WriteState(ref platform, frame, in state);
    }

    public static bool TryRecordControl<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        ShellControlAction action,
        int returnCode)
        where TPlatform : struct, IShellPlatform
    {
        if (action is < ShellControlAction.Else or > ShellControlAction.Quit ||
            !TryRead(ref platform, frame, out var state))
            return false;
        state.Control = (ShellScriptFrameControl)action;
        if (action == ShellControlAction.Else)
            state.Flags |= ShellScriptFrameFlags.ElseSeen;
        if (action == ShellControlAction.Quit)
        {
            state.Flags |= ShellScriptFrameFlags.QuitRequested;
            state.QuitResult = returnCode;
        }
        if (action is ShellControlAction.EndCli or ShellControlAction.EndShell)
            state.Flags |= ShellScriptFrameFlags.EndRequested;
        return WriteState(ref platform, frame, in state);
    }

    public static bool TrySetFailureLimit<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        uint failureLimit)
        where TPlatform : struct, IShellPlatform
    {
        if (failureLimit == 0 || !TryRead(ref platform, frame, out var state))
            return false;
        state.FailureLimit = failureLimit;
        state.Flags |= ShellScriptFrameFlags.FailureLimitSet;
        return WriteState(ref platform, frame, in state);
    }

    public static bool TryAdvance<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        uint line,
        uint offset)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, frame, out var state))
            return false;
        state.CurrentLine = line;
        state.CurrentOffset = offset;
        return WriteState(ref platform, frame, in state);
    }

    public static bool TrySetControlTop<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        APTR controlTop)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, frame, out var state))
            return false;
        state.ControlTop = controlTop;
        return WriteState(ref platform, frame, in state);
    }

    /// <summary>Scalar control-head update for the freestanding path.</summary>
    public static bool TrySetControlTopRaw<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        uint controlTopRaw)
        where TPlatform : struct, IShellPlatform
    {
        if (frame.IsNull || !platform.IsMapped(frame, Size) ||
            platform.ReadUInt32(frame, 0) != Magic ||
            platform.ReadUInt32(frame, 4) != Version)
            return false;
        platform.WriteUInt32(frame, 68, controlTopRaw);
        return true;
    }

    public static bool TrySetInputState<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        APTR inputState)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, frame, out var state))
            return false;
        state.InputState = inputState;
        return WriteState(ref platform, frame, in state);
    }

    public static bool TrySetLabelTop<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        APTR labelTop)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, frame, out var state))
            return false;
        state.LabelTop = labelTop;
        return WriteState(ref platform, frame, in state);
    }

    public static bool TrySetSignalState<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        APTR signalState)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, frame, out var state))
            return false;
        state.SignalState = signalState;
        return WriteState(ref platform, frame, in state);
    }

    /// <summary>Publishes or clears the external-command continuation.</summary>
    public static bool TrySetPendingCommand<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        APTR continuation,
        uint nextLine,
        uint nextOffset)
        where TPlatform : struct, IShellPlatform
    {
        if (continuation.IsNotNull && nextLine == 0)
            return false;
        if (!TryRead(ref platform, frame, out var state))
            return false;
        state.PendingCommand = continuation;
        state.PendingNextLine = continuation.IsNull ? 0 : nextLine;
        state.PendingNextOffset = continuation.IsNull ? 0 : nextOffset;
        return WriteState(ref platform, frame, in state);
    }

    public static bool TryApplySignal<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        ShellScriptSignalFlags signal,
        int result)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidSignal(signal) ||
            !TryRead(ref platform, frame, out var state))
            return false;
        state.LastResult = result;
        if ((signal & ShellScriptSignalFlags.Terminated) != 0)
        {
            state.Flags &= ~ShellScriptFrameFlags.Active;
            state.Flags |= ShellScriptFrameFlags.EndRequested;
        }
        else if ((signal & (ShellScriptSignalFlags.Break |
                ShellScriptSignalFlags.CtrlC)) != 0)
        {
            state.Flags |= ShellScriptFrameFlags.QuitRequested;
            state.QuitResult = result;
        }
        else if ((signal & ShellScriptSignalFlags.CtrlD) != 0)
        {
            state.Flags |= ShellScriptFrameFlags.EndRequested;
        }
        return WriteState(ref platform, frame, in state);
    }

    public static bool TrySetBranchState<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        uint conditionFalse,
        uint skipping,
        uint elseSeen)
        where TPlatform : struct, IShellPlatform
    {
        if (conditionFalse > 1 || skipping > 1 || elseSeen > 1 ||
            !TryRead(ref platform, frame, out var state))
            return false;
        var branchFlags = ShellScriptFrameFlags.ConditionFalse |
            ShellScriptFrameFlags.Skipping | ShellScriptFrameFlags.ElseSeen;
        state.Flags &= ~branchFlags;
        if (conditionFalse != 0)
            state.Flags |= ShellScriptFrameFlags.ConditionFalse;
        if (skipping != 0)
            state.Flags |= ShellScriptFrameFlags.Skipping;
        if (elseSeen != 0)
            state.Flags |= ShellScriptFrameFlags.ElseSeen;
        return WriteState(ref platform, frame, in state);
    }

    /// <summary>Scalar branch-flag update for the freestanding path.</summary>
    public static bool TrySetBranchStateRaw<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        uint conditionFalse,
        uint skipping,
        uint elseSeen)
        where TPlatform : struct, IShellPlatform
    {
        if (conditionFalse > 1 || skipping > 1 || elseSeen > 1 ||
            frame.IsNull || !platform.IsMapped(frame, Size) ||
            platform.ReadUInt32(frame, 0) != Magic ||
            platform.ReadUInt32(frame, 4) != Version)
            return false;
        var flags = (ShellScriptFrameFlags)platform.ReadUInt32(frame, 60);
        var branchFlags = ShellScriptFrameFlags.ConditionFalse |
            ShellScriptFrameFlags.Skipping | ShellScriptFrameFlags.ElseSeen;
        flags &= ~branchFlags;
        if (conditionFalse != 0) flags |= ShellScriptFrameFlags.ConditionFalse;
        if (skipping != 0) flags |= ShellScriptFrameFlags.Skipping;
        if (elseSeen != 0) flags |= ShellScriptFrameFlags.ElseSeen;
        platform.WriteUInt32(frame, 60, (uint)flags);
        return true;
    }

    public static bool TryIncrementLabelCount<TPlatform>(
        ref TPlatform platform,
        APTR frame)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, frame, out var state) ||
            state.LabelCount == uint.MaxValue)
            return false;
        state.LabelCount++;
        return WriteState(ref platform, frame, in state);
    }

    private static bool WriteState<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        in ShellScriptFrameState state)
        where TPlatform : struct, IShellPlatform
    {
        if (frame.IsNull || !platform.IsMapped(frame, Size))
            return false;
        platform.WriteUInt32(frame, 8, state.Parent.Raw);
        platform.WriteUInt32(frame, 12, state.Cli.Raw);
        platform.WriteUInt32(frame, 16, state.Input.Raw);
        platform.WriteUInt32(frame, 20, state.Output.Raw);
        platform.WriteUInt32(frame, 24, state.Error.Raw);
        platform.WriteUInt32(frame, 28, state.CurrentDirectory.Raw);
        platform.WriteUInt32(frame, 32, state.CurrentLine);
        platform.WriteUInt32(frame, 36, state.CurrentOffset);
        platform.WriteUInt32(frame, 40, state.FailureLimit);
        platform.WriteUInt32(frame, 44, unchecked((uint)state.LastResult));
        platform.WriteUInt32(frame, 48, unchecked((uint)state.QuitResult));
        platform.WriteUInt32(frame, 52, state.Condition);
        platform.WriteUInt32(frame, 56, (uint)state.Control);
        platform.WriteUInt32(frame, 60, (uint)state.Flags);
        platform.WriteUInt32(frame, 64, state.LabelCount);
        platform.WriteUInt32(frame, 68, state.ControlTop.Raw);
        platform.WriteUInt32(frame, 72, state.InputState.Raw);
        platform.WriteUInt32(frame, 76, state.LabelTop.Raw);
        platform.WriteUInt32(frame, 80, state.SignalState.Raw);
        platform.WriteUInt32(frame, 84, state.PendingCommand.Raw);
        platform.WriteUInt32(frame, 88, state.PendingNextLine);
        platform.WriteUInt32(frame, 92, state.PendingNextOffset);
        return true;
    }

    private static bool ValidSignal(ShellScriptSignalFlags signal) =>
        ((uint)signal & ~(uint)(ShellScriptSignalFlags.Break |
            ShellScriptSignalFlags.CtrlC |
            ShellScriptSignalFlags.CtrlD |
            ShellScriptSignalFlags.Terminated)) == 0 && signal != 0;

    private static void WriteUInt32<TPlatform>(
        ref TPlatform platform,
        APTR address,
        int offset,
        uint value)
        where TPlatform : struct, IShellPlatform =>
        platform.WriteUInt32(address, offset, value);
}
