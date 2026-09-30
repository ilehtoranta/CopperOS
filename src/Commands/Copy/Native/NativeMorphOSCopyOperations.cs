using Amiga;
namespace CopperOS.Commands.Native;

/// <summary>All admitted MorphOS Copy operation branches, sharing one parser lease.</summary>
public static class NativeMorphOSCopyOperations
{
    /// <summary>
    /// Requires completed source normalization/verbosity selection and writable
    /// invocation workspace. The caller retains the parser, requester state,
    /// DOS and transfer cache through execution and finalizes them afterward.
    /// </summary>
    public static void Run(ref NativeMorphOSCopyOptions options, APTR classifier,
        ref NativeMorphOSCopyTraversalState state)
    {
        if (state.Mode == NativeMorphOSCopyModeSelection.MakeDir)
        {
            NativeMorphOSCopyMakeDirectory.Run(ref options, ref state);
            return;
        }
        if (options.Direct)
        {
            NativeMorphOSCopyDirect.Run(ref options, ref state);
            return;
        }
        // args.verbose is established before operations. PatCopy may later
        // set COPYFLAG_VERBOSE independently; that must not enable this notice.
        var argumentVerbose = (state.Flags & 512) != 0;
        if (NativeMorphOSCopyPatternSetup.Prepare(options.Pattern, ref state, out var patternBytes))
        {
            if (state.Mode == NativeMorphOSCopyModeSelection.Delete)
                NativeMorphOSCopyPatternSetup.RunDeleteSources(ref options, classifier, ref state);
            else
                NativeMorphOSCopyTargetDispatch.Run(ref options, classifier, ref state);
        }
        if ((state.Flags & (1u << 22)) == 0 && argumentVerbose &&
            state.Result == DOS.RETURN_OK && state.SecondaryResult == DOS.RETURN_OK)
            DOS.VPrintf("No file was processed.\n", state.WarningArguments);
        NativeMorphOSCopyPatternSetup.Release(ref state, patternBytes);
    }
}
