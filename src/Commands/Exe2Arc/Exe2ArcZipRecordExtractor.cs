using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Streams the three ZIP record families emitted by Exe2Arc after a successful
/// SFX scan. It uses the caller's positioned input/output handles and scratch;
/// diagnostics, close/delete and final command return remain outside this
/// component. An optional signal owner supplies the source ZIP-only Ctrl-C
/// poll and ERROR_BREAK assignment.
/// </summary>
public static class Exe2ArcZipRecordExtractor
{
    private const uint SignatureBytes = 4;

    public static Exe2ArcIoStatus Extract<TIo>(ref TIo io, BPTR input,
        BPTR output, APTR scratch, uint scratchCapacity, uint fileLength,
        uint archiveOffset, uint correction, out uint outputBytes,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo, IExe2ArcBreakSource
    {
        outputBytes = 0;
        observation = default;
        if (fileLength > int.MaxValue || archiveOffset >= fileLength)
            return Exe2ArcIoStatus.NoMatch;
        if (input.IsNull || output.IsNull)
            return Exe2ArcIoStatus.InvalidHandle;
        if (!Exe2ArcIoBounds.HasScratch(ref io, scratch, scratchCapacity))
            return Exe2ArcIoStatus.InvalidBuffer;

        uint consumed = 0;
        uint maximum = fileLength - archiveOffset;
        if (maximum < SignatureBytes)
            return Exe2ArcIoStatus.IoStopped;
        while (consumed <= maximum - SignatureBytes)
        {
            if (ObserveBreak(ref io, ref observation, maximum,
                    ref outputBytes))
                return Exe2ArcIoStatus.Completed;

            int count = io.Read(input, scratch, (int)SignatureBytes);
            observation = Exe2ArcIoBounds.Observe(ref io,
                Exe2ArcIoStage.PayloadRead, count, SignatureBytes, outputBytes);
            if (count != (int)SignatureBytes)
                return Exe2ArcIoStatus.IoStopped;
            consumed += SignatureBytes;

            if (!Exe2ArcZipRecordRewriter.TryClassify(ref io, scratch,
                    out var kind))
                return Exe2ArcIoStatus.InvalidRecord;

            uint tailBytes = kind switch
            {
                Exe2ArcZipRecordKind.Local => 26,
                Exe2ArcZipRecordKind.Central => 42,
                Exe2ArcZipRecordKind.EndOfCentralDirectory => 18,
                _ => 0,
            };
            if (tailBytes == 0 || tailBytes > maximum - consumed)
                return Exe2ArcIoStatus.IoStopped;

            APTR tail = new(scratch.Raw + SignatureBytes);
            count = io.Read(input, tail, (int)tailBytes);
            observation = Exe2ArcIoBounds.Observe(ref io,
                Exe2ArcIoStage.PayloadRead, count, tailBytes, outputBytes);
            if (count != (int)tailBytes)
                return Exe2ArcIoStatus.IoStopped;
            consumed += tailBytes;

            uint recordBytes = SignatureBytes + tailBytes;
            if (!Exe2ArcZipRecordRewriter.TryRewrite(ref io, scratch,
                    recordBytes, correction, out var rewrittenKind) ||
                rewrittenKind != kind)
                return Exe2ArcIoStatus.InvalidRecord;

            uint dataBytes;
            switch (kind)
            {
                case Exe2ArcZipRecordKind.Local:
                    if (!AddLengths(ReadLittleEndian(ref io, scratch, 18),
                            ReadLittleEndian16(ref io, scratch, 26),
                            ReadLittleEndian16(ref io, scratch, 28),
                            out dataBytes))
                        return Exe2ArcIoStatus.InvalidRecord;
                    if (!TryWriteHeader(ref io, output, scratch, 30,
                            ref outputBytes, ref observation))
                        return Exe2ArcIoStatus.IoStopped;
                    break;

                case Exe2ArcZipRecordKind.Central:
                    if (!AddLengths(ReadLittleEndian16(ref io, scratch, 28),
                            ReadLittleEndian16(ref io, scratch, 30),
                            ReadLittleEndian16(ref io, scratch, 32),
                            out dataBytes))
                        return Exe2ArcIoStatus.InvalidRecord;
                    if (!TryWriteHeader(ref io, output, scratch, 46,
                            ref outputBytes, ref observation))
                        return Exe2ArcIoStatus.IoStopped;
                    break;

                case Exe2ArcZipRecordKind.EndOfCentralDirectory:
                    dataBytes = ReadLittleEndian16(ref io, scratch, 20);
                    if (!TryWriteHeader(ref io, output, scratch, 22,
                            ref outputBytes, ref observation))
                        return Exe2ArcIoStatus.IoStopped;
                    break;

                default:
                    return Exe2ArcIoStatus.InvalidRecord;
            }

            if (dataBytes != 0)
            {
                var copied = Exe2ArcPayloadCopy.Copy(ref io, input, output,
                    scratch, scratchCapacity, dataBytes, out var copyObservation);
                observation = copyObservation;
                outputBytes += copyObservation.BytesCompleted;
                if (copied != Exe2ArcIoStatus.Completed)
                    return copied;
                if (dataBytes > maximum - consumed)
                    return Exe2ArcIoStatus.IoStopped;
                consumed += dataBytes;
            }

            if (kind == Exe2ArcZipRecordKind.EndOfCentralDirectory)
            {
                // ExtractZIP performs its final SetSignal check after the
                // EOCD record has been copied, so a signal raised at that
                // boundary still receives the source success/count policy.
                if (ObserveBreak(ref io, ref observation, maximum,
                        ref outputBytes))
                    return Exe2ArcIoStatus.Completed;
                return Exe2ArcIoStatus.Completed;
            }
        }

        if (ObserveBreak(ref io, ref observation, maximum,
                ref outputBytes))
            return Exe2ArcIoStatus.Completed;

        return Exe2ArcIoStatus.IoStopped;
    }

    private static bool ObserveBreak<TIo>(ref TIo io,
        ref Exe2ArcIoObservation observation, uint sourceBytes,
        ref uint reportedBytes)
        where TIo : struct, IExe2ArcIo, IExe2ArcBreakSource
    {
        if (!io.IsBreakPending())
            return false;

        const int breakError = (int)DOS.Error.Break;
        io.SetIoErr(breakError);
        observation = observation.WithBreak(breakError);
        // ExtractZIP returns filesize-start after its loop, even when Ctrl-C
        // stopped the record walk. Preserve that source-visible count so the
        // frontend reports success and retains the partial output.
        reportedBytes = sourceBytes;
        return true;
    }

    private static bool TryWriteHeader<TIo>(ref TIo io, BPTR output,
        APTR scratch, int bytes, ref uint outputBytes,
        ref Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo
    {
        int written = io.Write(output, scratch, bytes);
        uint completed = written > 0 && written <= bytes
            ? (uint)written : 0;
        observation = Exe2ArcIoBounds.Observe(ref io,
            Exe2ArcIoStage.PayloadWrite, written, (uint)bytes,
            outputBytes + completed);
        if (written != bytes)
        {
            outputBytes += completed;
            return false;
        }
        outputBytes += (uint)bytes;
        return true;
    }

    private static bool AddLengths(uint first, uint second, uint third,
        out uint total)
    {
        total = 0;
        if (first > uint.MaxValue - second)
            return false;
        total = first + second;
        if (total > uint.MaxValue - third)
            return false;
        total += third;
        return true;
    }

    private static ushort ReadLittleEndian16<TIo>(ref TIo io, APTR address,
        int offset)
        where TIo : struct, IExe2ArcIo => (ushort)(io.ReadUInt8(address, offset) |
        (io.ReadUInt8(address, offset + 1) << 8));

    private static uint ReadLittleEndian<TIo>(ref TIo io, APTR address,
        int offset)
        where TIo : struct, IExe2ArcIo => (uint)io.ReadUInt8(address, offset) |
        ((uint)io.ReadUInt8(address, offset + 1) << 8) |
        ((uint)io.ReadUInt8(address, offset + 2) << 16) |
        ((uint)io.ReadUInt8(address, offset + 3) << 24);
}
