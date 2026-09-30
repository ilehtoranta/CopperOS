using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Borrowed parser values; valid only while the ReadArgs lease is live.</summary>
public struct NativeMorphOSCopyOptions
{
    public APTR Sources, Target, Pattern;
    public bool Direct, NoRequesters, InputOmitted;
}

/// <summary>Source option setup without releasing or reparsing DOS results.</summary>
public static class NativeMorphOSCopyOptionSetup
{
    /// <summary>
    /// Call once after successful admission. defaultSource owns twelve writable
    /// bytes for an empty source string and its terminated pointer vector.
    /// Mutates the DOS-owned FROM vector to remove a positional destination.
    /// Caller supplies fresh traversal state and owns its workspace separately.
    /// </summary>
    public static void Apply(ref NativeCommandArguments arguments, int mode,
        uint modeFlags, APTR defaultSource, ref NativeMorphOSCopyTraversalState state,
        out NativeMorphOSCopyOptions options)
    {
        options = default;
        state.Mode = mode;
        state.Depth = 1;
        state.Result = DOS.RETURN_OK;
        state.SecondaryResult = DOS.RETURN_FAIL;
        state.BufferSize = 512u * 1024;
        state.Flags = modeFlags | 2048u;
        arguments.TryGetResult(NativeMorphOSCopyArgumentGate.From, out var from);
        arguments.TryGetResult(NativeMorphOSCopyArgumentGate.To, out var to);
        arguments.TryGetResult(NativeMorphOSCopyArgumentGate.Pattern, out var pattern);
        arguments.TryGetResult(NativeMorphOSCopyArgumentGate.Buffer, out var buffer);
        if (buffer != 0)
        {
            var units = unchecked((int)APTR.ReadUInt32(APTR.FromPointer(buffer), 0));
            if (units > 0) state.BufferSize = unchecked((uint)units * 512u);
        }
        options.InputOmitted = from == 0;
        if (from == 0)
        {
            APTR.WriteUInt32(defaultSource, 0, defaultSource.Raw + 8);
            APTR.WriteUInt32(defaultSource, 4, 0);
            APTR.WriteUInt32(defaultSource, 8, 0);
            from = defaultSource.Raw;
        }
        options.Sources = APTR.FromPointer(from);
        if (mode != NativeMorphOSCopyModeSelection.Delete &&
            mode != NativeMorphOSCopyModeSelection.MakeDir && to == 0 &&
            APTR.ReadUInt32(options.Sources, 4) != 0)
        {
            var offset = 4;
            while (APTR.ReadUInt32(options.Sources, offset) != 0) offset += 4;
            offset -= 4;
            to = APTR.ReadUInt32(options.Sources, offset);
            APTR.WriteUInt32(options.Sources, offset, 0);
        }
        options.Target = APTR.FromPointer(to);
        options.Pattern = APTR.FromPointer(pattern);
        for (uint slot = 4; slot < NativeMorphOSCopyArgumentGate.ResultCount; slot++)
        {
            arguments.TryGetResult(slot, out var value);
            if (value == 0) continue;
            switch (slot)
            {
                case 4: state.Flags |= 1; break;
                case 5: options.Direct = true; break;
                case 6: state.Flags |= 2 | 8 | 2048; break;
                case 7: state.Flags |= 2; break;
                case 8: state.Flags |= 4; break;
                case 9: state.Flags |= 4096; break;
                case 10: state.Flags |= 8; break;
                case 11: state.Flags |= 256; break;
                case 12: options.NoRequesters = true; break;
                case 13: state.Flags |= 1024; break;
                case 19: state.Flags |= 16; break;
                case 20: state.Flags |= 32; break;
                case 21: state.Flags |= 64; break;
                case 22: state.Flags |= 128; break;
            }
        }
        // QUIET does not imply NOREQ in the original enabled source branch.
        // PROTECTION stays set with PROX; SetData gives PROTECTION precedence.
        state.MetadataFlags = NativeMorphOSCopyMetadata.Protection;
        if ((state.Flags & 2) != 0) state.MetadataFlags |= NativeMorphOSCopyMetadata.Dates;
        if ((state.Flags & 4) != 0) state.MetadataFlags |= NativeMorphOSCopyMetadata.NoProtection;
        if ((state.Flags & 8) != 0) state.MetadataFlags |= NativeMorphOSCopyMetadata.Comment;
        if ((state.Flags & 4096) != 0) state.MetadataFlags |= NativeMorphOSCopyMetadata.ProtectionX;
    }

    /// <summary>Original always-verbose selection, after source normalization.</summary>
    public static void SelectVerbosity(ref NativeMorphOSCopyOptions options,
        APTR classifierAnchor, ref NativeMorphOSCopyTraversalState state)
    {
        if ((state.Flags & 256) != 0) return;
        if (APTR.ReadUInt32(options.Sources, 0) != 0 &&
            APTR.ReadUInt32(options.Sources, 4) != 0) state.Flags |= 512;
        var offset = 0;
        uint source;
        while ((source = APTR.ReadUInt32(options.Sources, offset)) != 0)
        {
            if (NativeMorphOSCopyPatternClassifier.IsMatchPattern(
                    CString.FromPointer(source), classifierAnchor) != 0)
                state.Flags |= 512;
            offset += 4;
        }
    }
}
