using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Bounded LhA HUNK/SFX detector from Exe2Arc's fixed 100-byte probe. It
/// borrows the caller's input and scratch and owns no handles or cleanup.
/// Positive short reads are rejected before inspecting uninitialized bytes;
/// this is the explicit safety decision for the source's unsafe short-read
/// path.
/// </summary>
public static class Exe2ArcLhaScanner
{
    public const uint ProbeBytes = 100;
    public const uint RequiredScratchBytes = Exe2ArcIoBounds.BufferBytes;

    public static Exe2ArcIoStatus Scan<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength,
        out uint archiveOffset, out uint payloadLength,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo
    {
        archiveOffset = 0;
        payloadLength = 0;
        observation = default;
        if (fileLength > int.MaxValue)
            return Exe2ArcIoStatus.UnsupportedRange;
        if (input.IsNull)
            return Exe2ArcIoStatus.InvalidHandle;
        if (!Exe2ArcIoBounds.HasScratch(ref io, scratch, scratchCapacity))
            return Exe2ArcIoStatus.InvalidBuffer;

        int count = io.Read(input, scratch, (int)ProbeBytes);
        observation = Exe2ArcIoBounds.Observe(ref io,
            Exe2ArcIoStage.WindowRead, count, ProbeBytes, 0);
        if (count != (int)ProbeBytes)
            return Exe2ArcIoStatus.IoStopped;

        if (!Exe2ArcHeaderProbe.TryGetLhaPayloadStart(ref io, scratch,
                ProbeBytes, out uint start) || start == 0)
            return Exe2ArcIoStatus.NoMatch;
        if (start >= fileLength)
            return Exe2ArcIoStatus.NoMatch;
        if (start > int.MaxValue)
            return Exe2ArcIoStatus.UnsupportedRange;

        int seek = io.Seek(input, (int)start, -1);
        observation = Exe2ArcIoBounds.Observe(ref io,
            Exe2ArcIoStage.PayloadSeek, seek, 0, 0);
        if (seek < 0)
            return Exe2ArcIoStatus.IoStopped;

        archiveOffset = start;
        payloadLength = fileLength - start;

        // The source repeats the absolute seek after accepting the start and
        // ignores its result. Expose that unsafe final positioning separately.
        seek = io.Seek(input, (int)start, -1);
        observation = Exe2ArcIoBounds.Observe(ref io,
            Exe2ArcIoStage.PayloadSeek, seek, 0, 0);
        return seek < 0 ? Exe2ArcIoStatus.MatchedNotPositioned
            : Exe2ArcIoStatus.Completed;
    }
}
