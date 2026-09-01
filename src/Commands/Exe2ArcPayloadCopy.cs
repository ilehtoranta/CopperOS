using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Exact-chunk payload copy with borrowed input/output and scratch. The caller
/// must first confirm the input position and select the payload length. This
/// primitive does not seek, open, close, flush, delete, retry or poll signals.
/// </summary>
public static class Exe2ArcPayloadCopy
{
    public const uint RequiredScratchBytes = Exe2ArcIoBounds.BufferBytes;

    /// <summary>
    /// A zero-length request completes with no I/O and no scratch access; this
    /// is not the original command's zero-extraction return policy. Nonzero
    /// requests need 102400 borrowed scratch bytes and valid borrowed handles.
    /// Lengths above int.MaxValue are explicitly outside this component gate.
    /// Any short Read prevents Write of that chunk. Any short Write stops.
    /// </summary>
    public static Exe2ArcIoStatus Copy<TIo>(ref TIo io, BPTR input, BPTR output,
        APTR scratch, uint scratchCapacity, uint payloadLength,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo
    {
        observation = default;
        if (payloadLength > int.MaxValue)
            return Exe2ArcIoStatus.UnsupportedRange;
        if (payloadLength == 0)
            return Exe2ArcIoStatus.Completed;
        if (input.IsNull || output.IsNull)
            return Exe2ArcIoStatus.InvalidHandle;
        if (!Exe2ArcIoBounds.HasScratch(ref io, scratch, scratchCapacity))
            return Exe2ArcIoStatus.InvalidBuffer;

        uint completed = 0;
        uint remaining = payloadLength;
        while (remaining != 0)
        {
            uint requested = remaining > RequiredScratchBytes
                ? RequiredScratchBytes : remaining;
            int count = io.Read(input, scratch, (int)requested);
            observation = Exe2ArcIoBounds.Observe(ref io,
                Exe2ArcIoStage.PayloadRead, count, requested, completed);
            if (count != (int)requested)
                return Exe2ArcIoStatus.IoStopped;

            count = io.Write(output, scratch, (int)requested);
            if (count > 0 && (uint)count <= requested)
                completed += (uint)count;
            observation = Exe2ArcIoBounds.Observe(ref io,
                Exe2ArcIoStage.PayloadWrite, count, requested, completed);
            if (count != (int)requested)
                return Exe2ArcIoStatus.IoStopped;
            remaining -= requested;
        }
        return Exe2ArcIoStatus.Completed;
    }
}
