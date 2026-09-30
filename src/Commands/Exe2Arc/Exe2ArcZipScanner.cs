using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Bounded ZIP SFX detector from Exe2Arc's EOCD/backward scan. It identifies
/// the first local-header offset and the prefix correction required by the
/// later ZIP rewriting owner; it does not rewrite or extract records.
/// </summary>
public static class Exe2ArcZipScanner
{
    public const uint EocdMinimumBytes = 22;
    public const uint CentralEntryTailBytes = 42;
    public const uint RequiredScratchBytes = Exe2ArcIoBounds.BufferBytes;

    /// <summary>
    /// Returns the selected nonzero local-header offset and prefix correction.
    /// A candidate EOCD stops the source scan even when its central entry is
    /// malformed; this method therefore never continues to an earlier marker.
    /// </summary>
    public static Exe2ArcIoStatus Scan<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength,
        out uint archiveOffset, out uint payloadLength, out uint correction,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo
    {
        archiveOffset = 0;
        payloadLength = 0;
        correction = 0;
        observation = default;
        if (fileLength > int.MaxValue)
            return Exe2ArcIoStatus.UnsupportedRange;
        if (fileLength <= EocdMinimumBytes)
            return Exe2ArcIoStatus.NoMatch;
        if (input.IsNull)
            return Exe2ArcIoStatus.InvalidHandle;
        if (!Exe2ArcIoBounds.HasScratch(ref io, scratch, scratchCapacity))
            return Exe2ArcIoStatus.InvalidBuffer;

        int seek = io.Seek(input, 0, 2);
        observation = Exe2ArcIoBounds.Observe(ref io,
            Exe2ArcIoStage.WindowSeek, seek, 0, 0);
        if (seek < 0)
            return Exe2ArcIoStatus.IoStopped;

        uint remaining = fileLength;
        while (remaining > EocdMinimumBytes)
        {
            uint windowBytes = remaining > RequiredScratchBytes
                ? RequiredScratchBytes : remaining;
            uint windowStart = remaining - windowBytes;
            // The scan domain is bounded by int.MaxValue and the scratch
            // window, so this conversion cannot overflow.
            seek = io.Seek(input, (int)windowStart, -1);
            observation = Exe2ArcIoBounds.Observe(ref io,
                Exe2ArcIoStage.WindowSeek, seek, 0, 0);
            if (seek < 0)
                return Exe2ArcIoStatus.IoStopped;

            int count = io.Read(input, scratch, (int)windowBytes);
            observation = Exe2ArcIoBounds.Observe(ref io,
                Exe2ArcIoStage.WindowRead, count, windowBytes, 0);
            if (count != (int)windowBytes)
                return Exe2ArcIoStatus.IoStopped;

            // windowBytes is strictly larger than EocdMinimumBytes here and
            // is bounded by RequiredScratchBytes.
            int candidate = (int)windowBytes - (int)EocdMinimumBytes;
            bool sawEocd = false;
            for (; candidate >= 0; candidate--)
            {
                if (memoryByte(ref io, scratch, candidate) != (byte)'P' ||
                    memoryByte(ref io, scratch, candidate + 1) != (byte)'K' ||
                    memoryByte(ref io, scratch, candidate + 2) != 5 ||
                    memoryByte(ref io, scratch, candidate + 3) != 6)
                    continue;

                sawEocd = true;
                uint centralSize = ReadLittleEndian(ref io, scratch, candidate + 12);
                uint declaredCentral = ReadLittleEndian(ref io, scratch, candidate + 16);
                // The bounded scan domain is at most int.MaxValue, so the
                // physical offsets fit in one guest LONG. Keeping this as
                // uint also avoids a register-pair operation in resident
                // CopperSharp output.
                uint physicalEocd = windowStart + (uint)candidate;
                if (centralSize > physicalEocd)
                    return Exe2ArcIoStatus.NoMatch;
                uint physicalCentral = physicalEocd - centralSize;
                // A wrapped source correction is an unsafe archive layout;
                // reject it instead of manufacturing a huge guest seek.
                if (declaredCentral > physicalCentral ||
                    physicalCentral > int.MaxValue - 4 ||
                    physicalCentral + 4 + CentralEntryTailBytes > fileLength)
                    return Exe2ArcIoStatus.NoMatch;

                correction = (uint)(physicalCentral - declaredCentral);
                seek = io.Seek(input, (int)physicalCentral + 4, -1);
                observation = Exe2ArcIoBounds.Observe(ref io,
                    Exe2ArcIoStage.CandidateSeek, seek, 0, 0);
                if (seek < 0)
                    return Exe2ArcIoStatus.IoStopped;
                count = io.Read(input, scratch, (int)CentralEntryTailBytes);
                observation = Exe2ArcIoBounds.Observe(ref io,
                    Exe2ArcIoStage.PayloadRead, count, CentralEntryTailBytes, 0);
                if (count != (int)CentralEntryTailBytes)
                    return Exe2ArcIoStatus.IoStopped;

                uint localOffset = ReadLittleEndian(ref io, scratch, 38);
                if (localOffset > uint.MaxValue - correction)
                    return Exe2ArcIoStatus.NoMatch;
                uint start = localOffset + correction;
                if (start == 0)
                    return Exe2ArcIoStatus.OffsetZero;
                if (start >= fileLength || start > int.MaxValue)
                    return Exe2ArcIoStatus.NoMatch;

                archiveOffset = start;
                payloadLength = fileLength - start;
                seek = io.Seek(input, (int)start, -1);
                observation = Exe2ArcIoBounds.Observe(ref io,
                    Exe2ArcIoStage.PayloadSeek, seek, 0, 0);
                return seek < 0 ? Exe2ArcIoStatus.MatchedNotPositioned
                    : Exe2ArcIoStatus.Completed;
            }

            if (sawEocd)
                return Exe2ArcIoStatus.NoMatch;
            uint advance = windowBytes - (EocdMinimumBytes - 1);
            if (advance >= remaining)
                break;
            remaining -= advance;
        }
        return Exe2ArcIoStatus.NoMatch;
    }

    private static byte memoryByte<TIo>(ref TIo io, APTR scratch, int offset)
        where TIo : struct, IExe2ArcIo => io.ReadUInt8(scratch, offset);

    private static uint ReadLittleEndian<TIo>(ref TIo io, APTR address, int offset)
        where TIo : struct, IExe2ArcIo =>
        (uint)io.ReadUInt8(address, offset) |
        ((uint)io.ReadUInt8(address, offset + 1) << 8) |
        ((uint)io.ReadUInt8(address, offset + 2) << 16) |
        ((uint)io.ReadUInt8(address, offset + 3) << 24);
}
