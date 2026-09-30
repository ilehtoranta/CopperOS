using Amiga;

namespace CopperOS.Commands;

/// <summary>
/// Checks one Exe2Arc RAR4, CAB, ACE, ARJ, or LZH candidate in caller-owned guest memory.
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

    /// <summary>
    /// Recognizes the source's seven-byte <c>**ACE**</c> marker at candidate
    /// offset +7 and returns bytes through EOF.  The marker is deliberately
    /// not required at the candidate start: the executable wrapper occupies
    /// the preceding seven bytes in the source scanner.
    /// </summary>
    public static bool TryGetAcePayloadLength<TMemory>(
        ref TMemory memory,
        APTR header,
        uint headerBytes,
        uint fileOffset,
        uint fileLength,
        out uint payloadLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        payloadLength = 0;
        return fileOffset != 0 && TryInspectAce(ref memory, header, headerBytes,
            fileOffset, fileLength, out payloadLength);
    }

    internal static bool TryInspectAce<TMemory>(ref TMemory memory, APTR header,
        uint headerBytes, uint fileOffset, uint fileLength, out uint payloadLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        payloadLength = 0;
        if (!TryValidateCandidate(ref memory, header, headerBytes,
            fileOffset, fileLength, 14, out uint remaining))
            return false;

        if (memory.ReadUInt8(header, 7) != (byte)'*' ||
            memory.ReadUInt8(header, 8) != (byte)'*' ||
            memory.ReadUInt8(header, 9) != (byte)'A' ||
            memory.ReadUInt8(header, 10) != (byte)'C' ||
            memory.ReadUInt8(header, 11) != (byte)'E' ||
            memory.ReadUInt8(header, 12) != (byte)'*' ||
            memory.ReadUInt8(header, 13) != (byte)'*')
            return false;

        payloadLength = remaining;
        return true;
    }

    /// <summary>
    /// Recognizes the source's ARJ marker and reflected CRC over the variable
    /// header body. The returned payload begins at the marker and runs to EOF.
    /// The complete CRC tail must be present in the supplied guest window;
    /// malformed or truncated candidates are rejected without reading beyond
    /// the mapped span.
    /// </summary>
    public static bool TryGetArjPayloadLength<TMemory>(
        ref TMemory memory,
        APTR header,
        uint headerBytes,
        uint fileOffset,
        uint fileLength,
        out uint payloadLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        payloadLength = 0;
        return fileOffset != 0 && TryInspectArj(ref memory, header, headerBytes,
            fileOffset, fileLength, out payloadLength);
    }

    internal static bool TryInspectArj<TMemory>(ref TMemory memory, APTR header,
        uint headerBytes, uint fileOffset, uint fileLength,
        out uint payloadLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        payloadLength = 0;
        if (!TryValidateCandidate(ref memory, header, headerBytes,
            fileOffset, fileLength, 4, out uint remaining))
            return false;

        if (memory.ReadUInt8(header, 0) != 0x60 ||
            memory.ReadUInt8(header, 1) != 0xea)
            return false;

        uint headerLength = (uint)memory.ReadUInt8(header, 2) |
            ((uint)memory.ReadUInt8(header, 3) << 8);
        if (headerLength > uint.MaxValue - 8 ||
            remaining <= headerLength + 4)
            return false;

        uint requiredBytes = headerLength + 8;
        if (requiredBytes > remaining || headerBytes < requiredBytes ||
            header.Raw > uint.MaxValue - (requiredBytes - 1) ||
            !memory.IsMapped(header, requiredBytes))
            return false;

        uint crc = uint.MaxValue;
        for (uint index = 0; index < headerLength; index++)
        {
            byte value = memory.ReadUInt8(header, (int)(index + 4));
            crc = Crc32Byte(crc, value);
        }

        uint expected = (uint)memory.ReadUInt8(header,
            (int)(headerLength + 4)) |
            ((uint)memory.ReadUInt8(header, (int)(headerLength + 5)) << 8) |
            ((uint)memory.ReadUInt8(header, (int)(headerLength + 6)) << 16) |
            ((uint)memory.ReadUInt8(header, (int)(headerLength + 7)) << 24);
        if (~crc != expected)
            return false;

        payloadLength = remaining;
        return true;
    }

    /// <summary>
    /// Recognizes the source's LZH marker. The level check intentionally reads
    /// byte 20 of the scan window rather than candidate byte 20, matching the
    /// published source's bounded predicate.
    /// </summary>
    public static bool TryGetLzhPayloadLength<TMemory>(
        ref TMemory memory,
        APTR candidate,
        APTR scanWindow,
        uint headerBytes,
        uint fileOffset,
        uint fileLength,
        out uint payloadLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        payloadLength = 0;
        return fileOffset != 0 && TryInspectLzh(ref memory, candidate,
            scanWindow, headerBytes, fileOffset, fileLength,
            out payloadLength);
    }

    internal static bool TryInspectLzh<TMemory>(ref TMemory memory,
        APTR candidate, APTR scanWindow, uint headerBytes, uint fileOffset,
        uint fileLength, out uint payloadLength)
        where TMemory : struct, IAmigaGuestMemory
    {
        payloadLength = 0;
        if (!TryValidateCandidate(ref memory, candidate, headerBytes,
            fileOffset, fileLength, 7, out uint remaining) ||
            headerBytes < 21 || scanWindow.IsNull ||
            scanWindow.Raw > uint.MaxValue - (headerBytes - 1) ||
            !memory.IsMapped(scanWindow, headerBytes))
            return false;

        if (memory.ReadUInt8(candidate, 2) != (byte)'-' ||
            memory.ReadUInt8(candidate, 3) != (byte)'l' ||
            (memory.ReadUInt8(candidate, 4) != (byte)'h' &&
             memory.ReadUInt8(candidate, 4) != (byte)'z') ||
            memory.ReadUInt8(candidate, 6) != (byte)'-' ||
            memory.ReadUInt8(scanWindow, 20) > 2)
            return false;

        payloadLength = remaining;
        return true;
    }

    /// <summary>Recognizes the source's fixed HUNK/SFX LhA signature.</summary>
    public static bool TryGetLhaPayloadStart<TMemory>(ref TMemory memory,
        APTR header, uint headerBytes, out uint start)
        where TMemory : struct, IAmigaGuestMemory
    {
        start = 0;
        if (header.IsNull || headerBytes < 56 ||
            header.Raw > uint.MaxValue - (headerBytes - 1) ||
            !memory.IsMapped(header, headerBytes))
            return false;

        if (memory.ReadUInt8(header, 0) != 0 ||
            memory.ReadUInt8(header, 1) != 0 ||
            memory.ReadUInt8(header, 2) != 3 ||
            memory.ReadUInt8(header, 3) != 0xf3 ||
            memory.ReadUInt8(header, 44) != (byte)'S' ||
            memory.ReadUInt8(header, 45) != (byte)'F' ||
            memory.ReadUInt8(header, 46) != (byte)'X' ||
            memory.ReadUInt8(header, 47) != (byte)'!')
            return false;

        start = ((uint)memory.ReadUInt8(header, 52) << 24) |
            ((uint)memory.ReadUInt8(header, 53) << 16) |
            ((uint)memory.ReadUInt8(header, 54) << 8) |
            memory.ReadUInt8(header, 55);
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

    private static uint Crc32Byte(uint crc, byte value)
    {
        uint current = crc ^ value;
        for (var bit = 0; bit < 8; bit++)
            current = (current & 1) != 0
                ? (current >> 1) ^ 0xedb88320u
                : current >> 1;
        return current;
    }
}
