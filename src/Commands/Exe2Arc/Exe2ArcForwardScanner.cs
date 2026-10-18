using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Source-contract forward RAR4/CAB/ACE/ARJ searches over borrowed DOS handles and a
/// caller-owned 102400-byte scratch area. No allocation, output copy, resource
/// acquisition/release, diagnostic, command return or cancellation policy.
/// </summary>
/// <remarks>
/// Qualified input sizes are at most int.MaxValue, so absolute Seek arguments
/// remain nonnegative signed LONGs. This bounded component domain is not a
/// claim about the original command's large-file behavior. The input may be
/// repositioned on every outcome; the caller retains all handle ownership.
/// The caller keeps the admitted scratch allocation mapped until return; byte
/// reads and the borrowed file operations must not revoke that allocation.
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
        Scan(ref io, input, scratch, scratchCapacity, fileLength, ScanKind.Rar4,
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
        Scan(ref io, input, scratch, scratchCapacity, fileLength, ScanKind.Cabinet,
            out archiveOffset, out payloadLength, out observation);

    /// <summary>
    /// Scans for the source's <c>**ACE**</c> marker at candidate offset +7.
    /// The returned payload begins at the candidate wrapper offset and runs to
    /// EOF, matching ACE's shared copy-to-EOF extractor.
    /// </summary>
    public static Exe2ArcIoStatus ScanAce<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength,
        out uint archiveOffset, out uint payloadLength,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo =>
        Scan(ref io, input, scratch, scratchCapacity, fileLength, ScanKind.Ace,
            out archiveOffset, out payloadLength, out observation);

    /// <summary>
    /// Scans for the source's ARJ marker and valid reflected CRC header. The
    /// returned payload begins at the marker and runs to EOF.
    /// </summary>
    public static Exe2ArcIoStatus ScanArj<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength,
        out uint archiveOffset, out uint payloadLength,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo =>
        Scan(ref io, input, scratch, scratchCapacity, fileLength, ScanKind.Arj,
            out archiveOffset, out payloadLength, out observation);

    /// <summary>
    /// Scans for the source's LZH marker and its scan-window level predicate.
    /// The returned payload begins at the candidate and runs to EOF.
    /// </summary>
    public static Exe2ArcIoStatus ScanLzh<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength,
        out uint archiveOffset, out uint payloadLength,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo =>
        Scan(ref io, input, scratch, scratchCapacity, fileLength, ScanKind.Lzh,
            out archiveOffset, out payloadLength, out observation);

    private static Exe2ArcIoStatus Scan<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength, ScanKind kind,
        out uint archiveOffset, out uint payloadLength,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo
    {
        archiveOffset = 0;
        payloadLength = 0;
        observation = default;
        if (fileLength > int.MaxValue)
            return Exe2ArcIoStatus.UnsupportedRange;

        uint headerBytes = kind switch
        {
            ScanKind.Cabinet => 20u,
            ScanKind.Ace => 14u,
            ScanKind.Arj => 50u,
            ScanKind.Lzh => 21u,
            _ => 7u,
        };
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
            uint index = FindCandidate(ref io, scratch, lastCandidate, windowStart,
                fileLength, kind, out uint length);
            if (index != uint.MaxValue)
            {
                uint offset = windowStart + index;
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

    // Scan admits the entire scratch span once. For index <= windowBytes -
    // headerBytes, every fixed header fits both the mapped scratch and file.
    // No I/O or mapping change occurs between these byte probes. ARJ retains
    // its variable-length CRC-tail checks in the admitted predicate.
    private static uint FindCandidate<TIo>(ref TIo io, APTR scratch,
        uint lastCandidate, uint windowStart, uint fileLength, ScanKind kind,
        out uint length) where TIo : struct, IExe2ArcIo
    {
        length = 0;
        switch (kind)
        {
            case ScanKind.Cabinet:
                for (uint index = 0; index <= lastCandidate; index++)
                {
                    APTR candidate = new(scratch.Raw + index);
                    if (Exe2ArcHeaderProbe.TryInspectMappedCabinet(ref io, candidate, fileLength - windowStart - index, out length))
                    {
                        return index;
                    }
                }
                break;
            case ScanKind.Ace:
                for (uint index = 0; index <= lastCandidate; index++)
                {
                    APTR candidate = new(scratch.Raw + index);
                    if (Exe2ArcHeaderProbe.MatchesMappedAce(ref io, candidate))
                    {
                        length = fileLength - windowStart - index;
                        return index;
                    }
                }
                break;
            case ScanKind.Arj:
                for (uint index = 0; index <= lastCandidate; index++)
                {
                    APTR candidate = new(scratch.Raw + index);
                    if (Exe2ArcHeaderProbe.TryInspectMappedArj(ref io, candidate, 50, fileLength - windowStart - index, out length))
                    {
                        return index;
                    }
                }
                break;
            case ScanKind.Lzh:
                for (uint index = 0; index <= lastCandidate; index++)
                {
                    APTR candidate = new(scratch.Raw + index);
                    if (Exe2ArcHeaderProbe.MatchesMappedLzh(ref io, candidate, scratch))
                    {
                        length = fileLength - windowStart - index;
                        return index;
                    }
                }
                break;
            default:
                for (uint index = 0; index <= lastCandidate; index++)
                {
                    APTR candidate = new(scratch.Raw + index);
                    if (Exe2ArcHeaderProbe.MatchesMappedRar4(ref io, candidate))
                    {
                        length = fileLength - windowStart - index;
                        return index;
                    }
                }
                break;
        }
        return uint.MaxValue;
    }

    private enum ScanKind : byte
    {
        Rar4,
        Cabinet,
        Ace,
        Arj,
        Lzh,
    }
}
