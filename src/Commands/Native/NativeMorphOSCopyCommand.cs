using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Copy parser, operations and final result lifetime. The caller owns the open
/// DOS library; no parsed value or mutable workspace survives this invocation.
/// </summary>
public static class NativeMorphOSCopyCommand
{
    private const uint WorkspaceBytes = 4772;

    public static int Run()
    {
        // Results (96), default source (12), classifier (282), alignment (2), path (2048),
        // FIB (260), print arguments (8), destination path (2048), examine tags (16).
        var workspace = Exec.AllocMem(WorkspaceBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        // The original attempts its state allocation even when OpenLibrary
        // fails, then releases it without making any DOS call.
        if (DOS.DOSLibraryBase.IsNull)
        {
            if (workspace.IsNotNull) Exec.FreeMem(workspace, WorkspaceBytes);
            return DOS.RETURN_FAIL;
        }
        if (workspace.IsNull)
        {
            DOS.PrintFault(DOS.IoErr(), CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }
        var state = new NativeMorphOSCopyTraversalState();
        state.SecondaryResult = DOS.RETURN_FAIL;
        state.Depth = 1;
        state.BufferSize = 512u * 1024;
        var parsed = NativeMorphOSCopyParser.Read(workspace, out var parser,
            out var arguments);
        if (parsed)
        {
            var requesters = NativeMorphOSCopyRequesters.Capture();
            var admission = NativeMorphOSCopyArgumentGate.Evaluate(ref arguments,
                out var mode, out var flags, out var error);
            NativeMorphOSCopyOptionSetup.Apply(ref arguments, mode, flags,
                APTR.FromPointer(workspace.Raw + 96), ref state, out var options);
            var version = APTR.ReadUInt16(DOS.DOSLibraryBase, ExecLayout.Library.Version);
            var revision = APTR.ReadUInt16(DOS.DOSLibraryBase, ExecLayout.Library.Revision);
            state.ExtendedExamine = version > 51 || version == 51 && revision >= 66;
            requesters.Suppress(options.NoRequesters);
            var classifier = APTR.FromPointer(workspace.Raw + 108);
            // Classic DOS passes the FIB through a BPTR in filesystem packets.
            // Keep it longword aligned; otherwise Examine writes two bytes
            // before the supplied buffer and metadata reads become shifted.
            state.ExamineTags = APTR.FromPointer(workspace.Raw + 4756);
            state.Path = APTR.FromPointer(workspace.Raw + 392);
            state.Fib = APTR.FromPointer(workspace.Raw + 2440);
            state.WarningArguments = APTR.FromPointer(workspace.Raw + 2700);
            state.DestinationName = APTR.FromPointer(workspace.Raw + 2708);
            NativeMorphOSCopyOptionSetup.SelectVerbosity(ref options, classifier, ref state);
            if (admission == DOS.RETURN_OK)
                NativeMorphOSCopyOperations.Run(ref options, classifier, ref state);
            else DOS.SetIoErr((DOS.Error)error);
            requesters.Restore();
        }
        NativeMorphOSCopyParser.Release(ref arguments, ref parser);
        // Final diagnostics observe parser cleanup's IoErr. Transfer storage
        // stays live until Complete has selected/reported the command result.
        var result = NativeMorphOSCopyResultPolicy.Complete(ref state);
        Exec.FreeMem(workspace, WorkspaceBytes);
        return result;
    }
}
