using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Checks one Exe2Arc RAR4 or CAB candidate in caller-owned guest memory.
/// This is a bounded header primitive, not a file scanner or archive decoder.
/// </summary>
/// <remarks>
/// The caller supplies the candidate's nonzero file offset, total file length,
/// and an already-read header window. Only that window must be mapped; payload
/// bytes are not read. A false result always returns a zero payload length.
/// No memory is written or allocated. The caller owns scanning order, window
/// admission/overlap, file I/O, output and cleanup. A complete candidate may
/// end exactly at EOF inside an already admitted window. The source's strict
/// remaining-length check belongs to the window's start, not each candidate.
/// In particular, rejection of offset
/// zero does not authorize scanning past a zero-offset marker.
///
/// These independently written predicates implement the source-observed
/// bounded rules in contracts/Exe2Arc.md. Packed MorphOS binary correspondence,
/// other formats, command options and native execution remain separate gates.
/// </remarks>
public static class Exe2ArcHeaderProbe
{
    /// <summary>
    /// Recognizes the seven-byte RAR4 marker and returns bytes through EOF.
    /// A candidate needs at least seven bytes remaining. The caller separately
    /// admits source-compatible windows with more than seven bytes remaining
    /// at their read position; this helper does not perform that admission.
    /// RAR5, CRCs, passwords and compressed members are not recognized here.
    /// </summary>
    public static bool TryGetRar4PayloadLength<TMemory>(
        ref TMemory memory,
        APTR header,
        uint headerBytes,
        uint fileOffset,
        uint fileLength,
        out uint payloadLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        payloadLength = 0;
        return fileOffset != 0 && TryInspectRar4(ref memory, header, headerBytes,
            fileOffset, fileLength, out payloadLength);
    }

    // The forward scanner must recognize a zero-offset marker so it can stop,
    // even though that marker is not an accepted SFX payload. Keep this raw
    // inspection internal; public one-candidate rejection still makes no claim
    // that an enclosing scanner may continue past a rejected zero offset.
    internal static bool TryInspectRar4<TMemory>(ref TMemory memory, APTR header,
        uint headerBytes, uint fileOffset, uint fileLength, out uint payloadLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        payloadLength = 0;
        if (!TryValidateCandidate(ref memory, header, headerBytes,
            fileOffset, fileLength, 7, out uint remaining))
            return false;

        if (memory.ReadUInt8(header, 0) != 0x52 ||
            memory.ReadUInt8(header, 1) != 0x61 ||
            memory.ReadUInt8(header, 2) != 0x72 ||
            memory.ReadUInt8(header, 3) != 0x21 ||
            memory.ReadUInt8(header, 4) != 0x1a ||
            memory.ReadUInt8(header, 5) != 0x07 ||
            memory.ReadUInt8(header, 6) != 0x00)
            return false;

        payloadLength = remaining;
        return true;
    }

    /// <summary>
    /// Recognizes MSCF, a fitting LE32 cabinet length at +8, and an LE32
    /// file-table offset at +16 strictly below that length. Returns the declared
    /// length, excluding any trailer. It does not validate cabinet integrity or
    /// impose additional minimum-length/reserved-field requirements.
    /// </summary>
    public static bool TryGetCabinetPayloadLength<TMemory>(
        ref TMemory memory,
        APTR header,
        uint headerBytes,
        uint fileOffset,
        uint fileLength,
        out uint payloadLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        payloadLength = 0;
        return fileOffset != 0 && TryInspectCabinet(ref memory, header, headerBytes,
            fileOffset, fileLength, out payloadLength);
    }

    internal static bool TryInspectCabinet<TMemory>(ref TMemory memory, APTR header,
        uint headerBytes, uint fileOffset, uint fileLength, out uint payloadLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        payloadLength = 0;
        if (!TryValidateCandidate(ref memory, header, headerBytes,
            fileOffset, fileLength, 20, out uint remaining))
            return false;

        if (memory.ReadUInt8(header, 0) != (byte)'M' ||
            memory.ReadUInt8(header, 1) != (byte)'S' ||
            memory.ReadUInt8(header, 2) != (byte)'C' ||
            memory.ReadUInt8(header, 3) != (byte)'F')
            return false;

        uint declaredLength = ReadLittleEndianUInt32(ref memory, header, 8);
        uint fileTableOffset = ReadLittleEndianUInt32(ref memory, header, 16);
        if (declaredLength > remaining || fileTableOffset >= declaredLength)
            return false;

        payloadLength = declaredLength;
        return true;
    }

    private static bool TryValidateCandidate<TMemory>(
        ref TMemory memory,
        APTR header,
        uint headerBytes,
        uint fileOffset,
        uint fileLength,
        uint requiredHeaderBytes,
        out uint remaining)
        where TMemory : struct, IAmigaGuestMemory
    {
        remaining = 0;
        if (fileOffset >= fileLength)
            return false;

        uint available = fileLength - fileOffset;
        if (available < requiredHeaderBytes ||
            headerBytes < requiredHeaderBytes || headerBytes > available ||
            header.IsNull || header.Raw > uint.MaxValue - (headerBytes - 1) ||
            !memory.IsMapped(header, headerBytes))
            return false;

        remaining = available;
        return true;
    }

    private static uint ReadLittleEndianUInt32<TMemory>(
        ref TMemory memory, APTR address, int offset)
        where TMemory : struct, IAmigaGuestMemory =>
        (uint)memory.ReadUInt8(address, offset) |
        ((uint)memory.ReadUInt8(address, offset + 1) << 8) |
        ((uint)memory.ReadUInt8(address, offset + 2) << 16) |
        ((uint)memory.ReadUInt8(address, offset + 3) << 24);
}
