using Amiga;
using System;

namespace CopperOS.Shell;

/// <summary>Nested block kinds stored in the guest control chain.</summary>
public enum ShellScriptBlockKind : uint
{
    If = 1,
    Skip = 2,
}

/// <summary>State flags for one nested script block.</summary>
[Flags]
public enum ShellScriptBlockFlags : uint
{
    None = 0,
    ConditionFalse = 1,
    ElseSeen = 2,
    Skipping = 4,
    Backward = 8,
}

/// <summary>
/// Fixed-width state for one nested If/Skip block.  Parent is a guest pointer
/// to the previous record, so nested depth is bounded by DOS-owned storage
/// rather than a managed stack.
/// </summary>
public struct ShellScriptBlockState
{
    public APTR Parent;
    public ShellScriptBlockKind Kind;
    public ShellScriptBlockFlags Flags;
    public uint StartLine;
    public uint StartOffset;
    public uint TargetLine;
    public uint TargetOffset;
}

/// <summary>
/// Encodes and transitions one guest-resident nested script block record.
/// </summary>
public static class ShellScriptControlCodec
{
    public const uint Magic = 0x5343_424C;
    public const uint Version = 1;
    public const uint Size = 36;

    public static bool Initialize<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellScriptBlockState initial)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidKind(initial.Kind) || record.IsNull ||
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
        out ShellScriptBlockState state)
        where TPlatform : struct, IShellPlatform
    {
        state = default;
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            platform.ReadUInt32(record, 0) != Magic ||
            platform.ReadUInt32(record, 4) != Version)
            return false;
        state.Parent = APTR.FromPointer(platform.ReadUInt32(record, 8));
        state.Kind = (ShellScriptBlockKind)platform.ReadUInt32(record, 12);
        state.Flags = (ShellScriptBlockFlags)platform.ReadUInt32(record, 16);
        state.StartLine = platform.ReadUInt32(record, 20);
        state.StartOffset = platform.ReadUInt32(record, 24);
        state.TargetLine = platform.ReadUInt32(record, 28);
        state.TargetOffset = platform.ReadUInt32(record, 32);
        return ValidKind(state.Kind);
    }

    /// <summary>
    /// Scalar view used by the freestanding Shell path.  Keeping the link,
    /// kind, and flags as separate 32-bit results avoids an aggregate
    /// <c>ldobj</c> in the 68k backend while preserving the managed codec API.
    /// </summary>
    public static bool TryReadLink<TPlatform>(
        ref TPlatform platform,
        APTR record,
        out uint parentRaw,
        out ShellScriptBlockKind kind,
        out ShellScriptBlockFlags flags)
        where TPlatform : struct, IShellPlatform
    {
        parentRaw = 0;
        kind = (ShellScriptBlockKind)0;
        flags = ShellScriptBlockFlags.None;
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            platform.ReadUInt32(record, 0) != Magic ||
            platform.ReadUInt32(record, 4) != Version)
            return false;
        parentRaw = platform.ReadUInt32(record, 8);
        kind = (ShellScriptBlockKind)platform.ReadUInt32(record, 12);
        flags = (ShellScriptBlockFlags)platform.ReadUInt32(record, 16);
        return ValidKind(kind);
    }

    public static bool TryReadLinkRaw<TPlatform>(
        ref TPlatform platform,
        uint recordRaw,
        out uint parentRaw,
        out ShellScriptBlockKind kind,
        out ShellScriptBlockFlags flags)
        where TPlatform : struct, IShellPlatform
    {
        parentRaw = 0;
        kind = (ShellScriptBlockKind)0;
        flags = ShellScriptBlockFlags.None;
        return recordRaw != 0 && TryReadLink(ref platform,
            APTR.FromPointer(recordRaw), out parentRaw, out kind, out flags);
    }

    public static bool TryToggleElse<TPlatform>(
        ref TPlatform platform,
        APTR record)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, record, out var state) ||
            state.Kind != ShellScriptBlockKind.If ||
            (state.Flags & ShellScriptBlockFlags.ElseSeen) != 0)
            return false;
        state.Flags ^= ShellScriptBlockFlags.ConditionFalse;
        state.Flags ^= ShellScriptBlockFlags.Skipping;
        state.Flags |= ShellScriptBlockFlags.ElseSeen;
        return WriteState(ref platform, record, in state);
    }

    public static bool TrySetSkipping<TPlatform>(
        ref TPlatform platform,
        APTR record,
        uint skipping)
        where TPlatform : struct, IShellPlatform
    {
        if (skipping > 1 || !TryRead(ref platform, record, out var state))
            return false;
        if (skipping != 0)
            state.Flags |= ShellScriptBlockFlags.Skipping;
        else
            state.Flags &= ~ShellScriptBlockFlags.Skipping;
        return WriteState(ref platform, record, in state);
    }

    public static bool TrySetTarget<TPlatform>(
        ref TPlatform platform,
        APTR record,
        uint line,
        uint offset)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, record, out var state))
            return false;
        state.TargetLine = line;
        state.TargetOffset = offset;
        return WriteState(ref platform, record, in state);
    }

    public static bool TryPop<TPlatform>(
        ref TPlatform platform,
        APTR record,
        out APTR parent)
        where TPlatform : struct, IShellPlatform
    {
        parent = APTR.Null;
        if (!TryRead(ref platform, record, out var state))
            return false;
        parent = state.Parent;
        return true;
    }

    private static bool ValidKind(ShellScriptBlockKind kind) =>
        kind is ShellScriptBlockKind.If or ShellScriptBlockKind.Skip;

    private static bool WriteState<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellScriptBlockState state)
        where TPlatform : struct, IShellPlatform
    {
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            !ValidKind(state.Kind))
            return false;
        platform.WriteUInt32(record, 8, state.Parent.Raw);
        platform.WriteUInt32(record, 12, (uint)state.Kind);
        platform.WriteUInt32(record, 16, (uint)state.Flags);
        platform.WriteUInt32(record, 20, state.StartLine);
        platform.WriteUInt32(record, 24, state.StartOffset);
        platform.WriteUInt32(record, 28, state.TargetLine);
        platform.WriteUInt32(record, 32, state.TargetOffset);
        return true;
    }
}
