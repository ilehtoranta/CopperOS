using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Source-contract forward RAR4/CAB searches over borrowed DOS handles and a
/// caller-owned 102400-byte scratch area. No allocation, output copy, resource
/// acquisition/release, diagnostic, command return or cancellation policy.
/// </summary>
/// <remarks>
/// Qualified input sizes are at most int.MaxValue, so absolute Seek arguments
/// remain nonnegative signed LONGs. This bounded component domain is not a
/// claim about the original command's large-file behavior. The input may be
/// repositioned on every outcome; the caller retains all handle ownership.
/// </remarks>
public static class Exe2ArcForwardScanner
{
    public const uint RequiredScratchBytes = Exe2ArcIoBounds.BufferBytes;

    /// <summary>
    /// Completed confirms a nonzero marker and the final absolute Seek. Only
    /// Completed and MatchedNotPositioned retain offset/length; all other
    /// outcomes clear them. Never copy a MatchedNotPositioned result.
    /// </summary>
    public static Exe2ArcIoStatus ScanRar4<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength,
        out uint archiveOffset, out uint payloadLength,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo =>
        Scan(ref io, input, scratch, scratchCapacity, fileLength, false,
            out archiveOffset, out payloadLength, out observation);

    /// <summary>
    /// Uses the source's limited MSCF/length/table predicate, not CAB integrity
    /// validation. A declared length of one with a zero table offset can match.
    /// </summary>
    public static Exe2ArcIoStatus ScanCabinet<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength,
        out uint archiveOffset, out uint payloadLength,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo =>
        Scan(ref io, input, scratch, scratchCapacity, fileLength, true,
            out archiveOffset, out payloadLength, out observation);

    private static Exe2ArcIoStatus Scan<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength, bool cabinet,
        out uint archiveOffset, out uint payloadLength,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo
    {
        archiveOffset = 0;
        payloadLength = 0;
        observation = default;
        if (fileLength > int.MaxValue)
            return Exe2ArcIoStatus.UnsupportedRange;

        uint headerBytes = cabinet ? 20u : 7u;
        if (fileLength <= headerBytes)
            return Exe2ArcIoStatus.NoMatch;
        if (input.IsNull)
            return Exe2ArcIoStatus.InvalidHandle;
        if (!Exe2ArcIoBounds.HasScratch(ref io, scratch, scratchCapacity))
            return Exe2ArcIoStatus.InvalidBuffer;

        uint windowStart = 0;
        while (fileLength - windowStart > headerBytes)
        {
            uint windowBytes = fileLength - windowStart;
            if (windowBytes > RequiredScratchBytes)
                windowBytes = RequiredScratchBytes;

            int seek = io.Seek(input, (int)windowStart, -1);
            observation = Exe2ArcIoBounds.Observe(ref io,
                Exe2ArcIoStage.WindowSeek, seek, 0, 0);
            if (seek < 0)
                return Exe2ArcIoStatus.IoStopped;

            int count = io.Read(input, scratch, (int)windowBytes);
            observation = Exe2ArcIoBounds.Observe(ref io,
                Exe2ArcIoStage.WindowRead, count, windowBytes, 0);
            if (count != (int)windowBytes)
                return Exe2ArcIoStatus.IoStopped;

            uint lastCandidate = windowBytes - headerBytes;
            for (uint index = 0; index <= lastCandidate; index++)
            {
                APTR candidate = new(scratch.Raw + index);
                uint offset = windowStart + index;
                uint length;
                bool recognized = cabinet
                    ? Exe2ArcHeaderProbe.TryInspectCabinet(ref io, candidate,
                        headerBytes, offset, fileLength, out length)
                    : Exe2ArcHeaderProbe.TryInspectRar4(ref io, candidate,
                        headerBytes, offset, fileLength, out length);
                if (!recognized)
                    continue;

                // Source-observed intermediate seek is relative to the end of
                // this read, not a direct seek to the selected candidate.
                seek = io.Seek(input, -(int)index, 0);
                observation = Exe2ArcIoBounds.Observe(ref io,
                    Exe2ArcIoStage.CandidateSeek, seek, 0, 0);
                if (seek < 0)
                    return Exe2ArcIoStatus.IoStopped;
                if (offset == 0)
                    return Exe2ArcIoStatus.OffsetZero;

                archiveOffset = offset;
                payloadLength = length;
                seek = io.Seek(input, (int)offset, -1);
                observation = Exe2ArcIoBounds.Observe(ref io,
                    Exe2ArcIoStage.PayloadSeek, seek, 0, 0);
                // The source ignores this failure. Expose it instead of
                // claiming that subsequent copying is positioned safely.
                return seek < 0 ? Exe2ArcIoStatus.MatchedNotPositioned
                    : Exe2ArcIoStatus.Completed;
            }

            // Inclusive candidate end implies headerBytes - 1 overlap. An EOF
            // tail of exactly headerBytes will not admit another window.
            windowStart += lastCandidate + 1;
        }
        return Exe2ArcIoStatus.NoMatch;
    }
}
