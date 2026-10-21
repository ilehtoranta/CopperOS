using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Bounded operations that connect a frame to its guest control-record chain.
/// Record allocation and reclamation remain outside this type: the caller
/// supplies a guest slot and owns it after the chain is closed.
/// </summary>
public static class ShellScriptControlTransitions
{
    public static bool TryOpen<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        APTR record,
        ShellScriptBlockKind kind,
        uint startLine,
        uint startOffset,
        uint conditionFalse)
        where TPlatform : struct, IShellPlatform
    {
        if (record.IsNull || conditionFalse > 1 ||
            !ShellScriptFrameCodec.TryRead(ref platform, frame,
                out var frameState) ||
            Overlaps(record, ShellScriptControlCodec.Size,
                frame, ShellScriptFrameCodec.Size))
            return false;

        var flags = kind == ShellScriptBlockKind.Skip
            ? ShellScriptBlockFlags.Skipping
            : conditionFalse != 0
                ? ShellScriptBlockFlags.ConditionFalse |
                    ShellScriptBlockFlags.Skipping
                : ShellScriptBlockFlags.None;
        var effectiveSkipping = (flags &
            ShellScriptBlockFlags.Skipping) != 0;
        if (frameState.ControlTop.IsNotNull)
        {
            if (Overlaps(record, ShellScriptControlCodec.Size,
                    frameState.ControlTop, ShellScriptControlCodec.Size) ||
                !TryReadSkipping(ref platform, frameState.ControlTop.Raw,
                    out var parentSkipping))
                return false;
            effectiveSkipping |= parentSkipping != 0;
        }

        ShellScriptBlockState block = new()
        {
            Parent = frameState.ControlTop,
            Kind = kind,
            Flags = flags,
            StartLine = startLine,
            StartOffset = startOffset,
        };
        if (!ShellScriptControlCodec.Initialize(
                ref platform, record, in block) ||
            !ShellScriptFrameCodec.TrySetControlTop(
                ref platform, frame, record))
            return false;

        if (ShellScriptFrameCodec.TrySetBranchState(
            ref platform,
            frame,
            (flags & ShellScriptBlockFlags.ConditionFalse) != 0 ? 1u : 0u,
            effectiveSkipping ? 1u : 0u,
            (flags & ShellScriptBlockFlags.ElseSeen) != 0 ? 1u : 0u))
            return true;

        ShellScriptFrameCodec.TrySetControlTop(
            ref platform, frame, frameState.ControlTop);
        return false;
    }

    public static bool TryElse<TPlatform>(
        ref TPlatform platform,
        APTR frame)
        where TPlatform : struct, IShellPlatform
    {
        if (!ShellScriptFrameCodec.TryRead(ref platform, frame,
                out var frameState) || frameState.ControlTop.IsNull ||
            !ShellScriptControlCodec.TryToggleElse(
                ref platform, frameState.ControlTop) ||
            !ShellScriptControlCodec.TryReadLink(
                ref platform, frameState.ControlTop, out var blockParentRaw,
                out _, out var blockFlags))
            return false;

        var effectiveSkipping = (blockFlags &
            ShellScriptBlockFlags.Skipping) != 0;
        var blockParent = APTR.FromPointer(blockParentRaw);
        if (blockParent.IsNotNull)
        {
            if (!TryReadSkipping(ref platform, blockParentRaw,
                    out var parentSkipping))
                return false;
            effectiveSkipping |= parentSkipping != 0;
        }
        return ShellScriptFrameCodec.TryRecordControl(
                ref platform, frame, ShellControlAction.Else, 0) &&
            ShellScriptFrameCodec.TrySetBranchState(
                ref platform,
                frame,
                (blockFlags & ShellScriptBlockFlags.ConditionFalse) != 0
                    ? 1u : 0u,
                effectiveSkipping ? 1u : 0u,
                (blockFlags & ShellScriptBlockFlags.ElseSeen) != 0
                    ? 1u : 0u);
    }

    public static bool TryClose<TPlatform>(
        ref TPlatform platform,
        APTR frame,
        ShellScriptBlockKind expectedKind,
        out APTR closedRecord)
        where TPlatform : struct, IShellPlatform
    {
        closedRecord = APTR.Null;
        if (!ShellScriptFrameCodec.TryReadControlTop(ref platform, frame,
                out var controlTopRaw) || controlTopRaw == 0 ||
            !ShellScriptControlCodec.TryReadLinkRaw(
                ref platform, controlTopRaw, out var blockParentRaw,
                out var blockKind, out _ ) ||
            blockKind != expectedKind)
            return false;

        uint conditionFalse = 0;
        uint skipping = 0;
        uint elseSeen = 0;
        if (blockParentRaw != 0)
        {
            if (!ShellScriptControlCodec.TryReadLinkRaw(
                    ref platform, blockParentRaw, out _, out _,
                    out var parentFlags))
                return false;
            conditionFalse = (parentFlags &
                ShellScriptBlockFlags.ConditionFalse) != 0 ? 1u : 0u;
            if (!TryReadSkipping(ref platform, blockParentRaw, out skipping))
                return false;
            elseSeen = (parentFlags & ShellScriptBlockFlags.ElseSeen) != 0
                ? 1u : 0u;
        }

        closedRecord = APTR.FromPointer(controlTopRaw);
        if (!ShellScriptFrameCodec.TrySetControlTopRaw(
                ref platform, frame, blockParentRaw))
            return false;
        if (ShellScriptFrameCodec.TrySetBranchStateRaw(
                ref platform, frame, conditionFalse, skipping, elseSeen))
            return true;

        ShellScriptFrameCodec.TrySetControlTopRaw(
            ref platform, frame, controlTopRaw);
        return false;
    }

    // Each record stores its own branch decision. A true inner IF must not
    // discard a false ancestor when entering, switching, or closing a block.
    private static bool TryReadSkipping<TPlatform>(ref TPlatform platform,
        uint recordRaw, out uint skipping)
        where TPlatform : struct, IShellPlatform
    {
        skipping = 0;
        while (recordRaw != 0)
        {
            if (!ShellScriptControlCodec.TryReadLinkRaw(ref platform,
                    recordRaw, out var parentRaw, out _, out var flags))
                return false;
            if ((flags & ShellScriptBlockFlags.Skipping) != 0)
                skipping = 1;
            recordRaw = parentRaw;
        }
        return true;
    }

    private static bool Overlaps(
        APTR first,
        uint firstSize,
        APTR second,
        uint secondSize)
    {
        if (first.Raw > uint.MaxValue - firstSize ||
            second.Raw > uint.MaxValue - secondSize)
            return true;
        return first.Raw < second.Raw + secondSize &&
            second.Raw < first.Raw + firstSize;
    }
}
