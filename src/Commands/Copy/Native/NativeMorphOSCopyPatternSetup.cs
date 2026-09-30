using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Normal-operation PATTERN preparation and DELETE source dispatch.</summary>
public static class NativeMorphOSCopyPatternSetup
{
    /// <summary>
    /// Requires fresh Pattern state. The returned allocation size belongs to
    /// this invocation; successful storage remains live through all sources.
    /// Failure does not replace allocation/parser/cleanup IoErr, except that
    /// an explicitly empty pattern sets the source's ERROR_BAD_TEMPLATE.
    /// </summary>
    public static bool Prepare(APTR pattern, ref NativeMorphOSCopyTraversalState state,
        out uint allocationBytes)
    {
        allocationBytes = 0;
        state.Pattern = APTR.Null;
        if (pattern.IsNull) return true;
        if (APTR.ReadUInt8(pattern, 0) == 0)
        {
            DOS.SetIoErr(DOS.Error.BadTemplate);
            return false;
        }
        uint length = 0;
        while (APTR.ReadUInt8(pattern, unchecked((int)length)) != 0) length++;
        allocationBytes = unchecked(length * 2 + 3);
        state.Pattern = Exec.AllocMem(allocationBytes, Exec.MemoryFlags.Any);
        if (state.Pattern.IsNull) return false;
        if (DOS.ParsePatternNoCase(CString.FromPointer(pattern.Raw), state.Pattern,
                unchecked((int)allocationBytes)) < 0)
        {
            Exec.FreeMem(state.Pattern, allocationBytes);
            state.Pattern = APTR.Null;
            return false;
        }
        return true;
    }

    /// <summary>Original normal DELETE loop, after successful pattern setup.</summary>
    public static void RunDeleteSources(ref NativeMorphOSCopyOptions options,
        APTR classifierAnchor, ref NativeMorphOSCopyTraversalState state)
    {
        state.SecondaryResult = DOS.RETURN_OK;
        var limit = (state.Flags & 1024) != 0 ? DOS.RETURN_OK : DOS.RETURN_WARN;
        var offset = 0;
        uint source;
        // The original outer loop tests primary only; DoWork guards secondary.
        while (state.Result <= limit &&
            (source = APTR.ReadUInt32(options.Sources, offset)) != 0)
        {
            NativeMorphOSCopyTraversal.Run(APTR.FromPointer(source), classifierAnchor, ref state);
            offset += 4;
        }
    }

    public static void Release(ref NativeMorphOSCopyTraversalState state, uint allocationBytes)
    {
        if (state.Pattern.IsNotNull)
        {
            Exec.FreeMem(state.Pattern, allocationBytes);
            state.Pattern = APTR.Null;
        }
    }
}
