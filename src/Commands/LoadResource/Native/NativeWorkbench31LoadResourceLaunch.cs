using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Client-side service lookup, detached worker SegList handoff and first real
/// request. The packaging contract is a client CODE hunk followed by an
/// independent worker CODE hunk; no worker reference may point into the client.
/// This component is not a complete command entry or a hook-unload proof.
/// </summary>
public static class NativeWorkbench31LoadResourceLaunch
{
#pragma warning disable CS0649 // Stack-owned tag cells initialized through explicit APTR stores.
    private struct Tags
    {
        public uint A, B, C, D, E, F, G, H, I, J, K, L;
        public uint M, N, O, P, Q, R, S, T, U, V, W, X;

        public static APTR AddressOf(ref Tags value) =>
            throw new System.NotSupportedException("Tags.AddressOf is lowered by CopperSharp.");
    }
#pragma warning restore CS0649

    /// <summary>
    /// The DOS library and parsed arguments are borrowed. serviceName must be
    /// the original Latin-1 service name in worker-owned storage, not client
    /// code or stack storage: it can remain in NP_Name after this client exits.
    /// commandSegment is cli_Module from the current CLI. A newly created
    /// worker must wait for its initial process-port request before it may
    /// return. Parser storage stays live through the normal protocol reply.
    /// </summary>
    public static int Send(CString serviceName, BPTR commandSegment,
        ref NativeCommandArguments arguments, out int ioError)
    {
        ioError = arguments.IoError;
        if (!arguments.IsSuccess ||
            !arguments.TryGetResult(0, out _) ||
            !arguments.TryGetResult(1, out _) ||
            !arguments.TryGetResult(2, out _) ||
            CString.ToUInt32(serviceName) == 0 ||
            Exec.FindTask(CString.FromPointer(0)).IsNull)
            return DOS.RETURN_FAIL;

        Exec.Forbid();
        var service = Exec.FindPort(serviceName);
        if (service.IsNull)
        {
            service = StartWorker(serviceName, commandSegment, out ioError);
            if (service.IsNull)
            {
                // Capture CreateNewProc's error before the diagnostic. The
                // original reports it while its client Forbid is still held.
                DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
                Exec.Permit();
                return DOS.RETURN_FAIL;
            }
        }

        var result = NativeWorkbench31LoadResourceProtocol.Send(service,
            ref arguments, out ioError);
        Exec.Permit();
        return result;
    }

    private static APTR StartWorker(CString serviceName, BPTR commandSegment,
        out int ioError)
    {
        ioError = (int)DOS.Error.ObjectWrongType;
        // These checks reject an incorrectly packaged/invoked candidate; the
        // original HUNK assumes a valid CLI/module and does not define them.
        if (commandSegment.IsNull)
            return APTR.Null;
        var header = commandSegment.Address;
        var worker = BPTR.FromRaw(APTR.ReadUInt32(header, 0));
        if (worker.IsNull || worker.Raw == commandSegment.Raw)
            return APTR.Null;

        var storage = default(Tags);
        var tags = Tags.AddressOf(ref storage);
        Tag(tags, 0, DosNewProcessTag.SegmentList, worker.Raw);
        Tag(tags, 8, DosNewProcessTag.FreeSegmentList, 1);
        Tag(tags, 16, DosNewProcessTag.Input, 0);
        Tag(tags, 24, DosNewProcessTag.Output, 0);
        Tag(tags, 32, DosNewProcessTag.CurrentDirectory, 0);
        Tag(tags, 40, DosNewProcessTag.StackSize, 3000);
        Tag(tags, 48, DosNewProcessTag.Name, CString.ToUInt32(serviceName));
        Tag(tags, 56, DosNewProcessTag.Priority, 0);
        Tag(tags, 64, DosNewProcessTag.WindowPointer, 0);
        Tag(tags, 72, DosNewProcessTag.HomeDirectory, 0);
        Tag(tags, 80, DosNewProcessTag.CopyVariables, 0);
        APTR.WriteUInt32(tags, 88, 0); // TAG_DONE; no NP_Entry or post-3.1 tags.

        // NP_FreeSeglist transfers ownership on success. Detach first so the
        // two owners never share an unloadable chain while DOS can schedule.
        // A failed CreateNewProc leaves supplied resources with its caller.
        APTR.WriteUInt32(header, 0, 0);
        var process = DOS.CreateNewProc(tags);
        if (process.IsNull)
        {
            ioError = unchecked((int)DOS.IoErr());
            APTR.WriteUInt32(header, 0, worker.Raw);
            return APTR.Null;
        }

        ioError = 0;
        return APTR.FromPointer(process.Raw + DosLayout.Process.MessagePort);
    }

    private static void Tag(APTR tags, int offset, DosNewProcessTag tag, uint value)
    {
        APTR.WriteUInt32(tags, offset, (uint)tag);
        APTR.WriteUInt32(tags, offset + 4, value);
    }
}
