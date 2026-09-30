using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Owns Exe2Arc's scanner dispatch and source order. It borrows the input
/// handle and scratch area; it does not open, close, copy, diagnose or delete
/// anything. A scanner result other than <see cref="Exe2ArcIoStatus.NoMatch"/>
/// is terminal, including an offset-zero marker and an unsafe final seek.
/// </summary>
public static class Exe2ArcScannerSelection
{
    /// <summary>
    /// Selects one archive scanner. With an unspecified TYPE the source order
    /// is ZIP, ACE, RAR, CAB, ARJ, LhA, then LZH. A selected scanner's
    /// no-match result permits the next scanner; every other result stops the
    /// selection immediately.
    /// </summary>
    public static Exe2ArcIoStatus Scan<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength,
        Exe2ArcArchiveType requested, out Exe2ArcArchiveType selected,
        out uint archiveOffset, out uint payloadLength, out uint correction,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo
    {
        selected = Exe2ArcArchiveType.Unspecified;
        archiveOffset = 0;
        payloadLength = 0;
        correction = 0;
        observation = default;

        if (requested != Exe2ArcArchiveType.Unspecified)
            return ScanOne(ref io, input, scratch, scratchCapacity, fileLength,
                requested, out selected, out archiveOffset, out payloadLength,
                out correction, out observation);

        // Keep the order as immediates instead of a mutable static table: this
        // owner is linked into resident/pure candidates and must not acquire a
        // shared writable data dependency merely to dispatch seven scanners.
        for (var index = 1u; index <= 7u; index++)
        {
            var type = (Exe2ArcArchiveType)index;
            var status = ScanOne(ref io, input, scratch, scratchCapacity,
                fileLength, type, out selected, out archiveOffset,
                out payloadLength, out correction, out observation);
            if (status != Exe2ArcIoStatus.NoMatch)
                return status;
        }

        selected = Exe2ArcArchiveType.Unspecified;
        return Exe2ArcIoStatus.NoMatch;
    }

    private static Exe2ArcIoStatus ScanOne<TIo>(ref TIo io, BPTR input,
        APTR scratch, uint scratchCapacity, uint fileLength,
        Exe2ArcArchiveType type, out Exe2ArcArchiveType selected,
        out uint archiveOffset, out uint payloadLength, out uint correction,
        out Exe2ArcIoObservation observation)
        where TIo : struct, IExe2ArcIo
    {
        selected = type;
        correction = 0;
        if (input.IsNull)
        {
            archiveOffset = 0;
            payloadLength = 0;
            observation = default;
            return Exe2ArcIoStatus.InvalidHandle;
        }

        // The original frontend seeks to the file start before every table
        // entry, including the fixed LhA prefix probe. Keep that boundary at
        // the dispatch owner so a preceding scanner cannot leak its cursor.
        var initialSeek = io.Seek(input, 0, -1);
        observation = Exe2ArcIoBounds.Observe(ref io,
            Exe2ArcIoStage.DispatchSeek, initialSeek, 0, 0);
        if (initialSeek < 0)
        {
            archiveOffset = 0;
            payloadLength = 0;
            return Exe2ArcIoStatus.IoStopped;
        }

        switch (type)
        {
            case Exe2ArcArchiveType.Zip:
                return Exe2ArcZipScanner.Scan(ref io, input, scratch,
                    scratchCapacity, fileLength, out archiveOffset,
                    out payloadLength, out correction, out observation);
            case Exe2ArcArchiveType.Ace:
                return Exe2ArcForwardScanner.ScanAce(ref io, input, scratch,
                    scratchCapacity, fileLength, out archiveOffset,
                    out payloadLength, out observation);
            case Exe2ArcArchiveType.Rar:
                return Exe2ArcForwardScanner.ScanRar4(ref io, input, scratch,
                    scratchCapacity, fileLength, out archiveOffset,
                    out payloadLength, out observation);
            case Exe2ArcArchiveType.Cabinet:
                return Exe2ArcForwardScanner.ScanCabinet(ref io, input, scratch,
                    scratchCapacity, fileLength, out archiveOffset,
                    out payloadLength, out observation);
            case Exe2ArcArchiveType.Arj:
                return Exe2ArcForwardScanner.ScanArj(ref io, input, scratch,
                    scratchCapacity, fileLength, out archiveOffset,
                    out payloadLength, out observation);
            case Exe2ArcArchiveType.Lha:
                return Exe2ArcLhaScanner.Scan(ref io, input, scratch,
                    scratchCapacity, fileLength, out archiveOffset,
                    out payloadLength, out observation);
            case Exe2ArcArchiveType.Lzh:
                return Exe2ArcForwardScanner.ScanLzh(ref io, input, scratch,
                    scratchCapacity, fileLength, out archiveOffset,
                    out payloadLength, out observation);
            default:
                archiveOffset = 0;
                payloadLength = 0;
                observation = default;
                return Exe2ArcIoStatus.NoMatch;
        }
    }
}
