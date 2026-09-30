using Amiga;

namespace CopperOS.Commands;

public enum Exe2ArcZipRecordKind : uint
{
    Unknown = 0,
    Local = 1,
    Central = 2,
    EndOfCentralDirectory = 3,
}

/// <summary>
/// Rewrites the two ZIP little-endian offsets adjusted by Exe2Arc's SFX
/// correction. The caller owns record reads, output writes and record lengths;
/// this helper only validates and mutates a mapped guest record in place.
/// </summary>
public static class Exe2ArcZipRecordRewriter
{
    public const uint LocalRecordBytes = 30;
    public const uint CentralRecordBytes = 46;
    public const uint EndRecordBytes = 22;

    public static bool TryClassify<TMemory>(ref TMemory memory, APTR record,
        out Exe2ArcZipRecordKind kind)
        where TMemory : struct, IAmigaGuestMemory
    {
        kind = Exe2ArcZipRecordKind.Unknown;
        if (record.IsNull || !memory.IsMapped(record, 4))
            return false;
        uint signature = ReadLittleEndian(ref memory, record, 0);
        kind = signature switch
        {
            0x04034b50 => Exe2ArcZipRecordKind.Local,
            0x02014b50 => Exe2ArcZipRecordKind.Central,
            0x06054b50 => Exe2ArcZipRecordKind.EndOfCentralDirectory,
            _ => Exe2ArcZipRecordKind.Unknown,
        };
        return kind != Exe2ArcZipRecordKind.Unknown;
    }

    public static bool TryRewrite<TMemory>(ref TMemory memory, APTR record,
        uint recordBytes, uint correction, out Exe2ArcZipRecordKind kind)
        where TMemory : struct, IAmigaGuestMemory
    {
        kind = Exe2ArcZipRecordKind.Unknown;
        if (record.IsNull || recordBytes == 0 ||
            record.Raw > uint.MaxValue - (recordBytes - 1) ||
            recordBytes < 4 || !memory.IsMapped(record, recordBytes))
            return false;

        uint signature = ReadLittleEndian(ref memory, record, 0);
        switch (signature)
        {
            case 0x04034b50:
                kind = Exe2ArcZipRecordKind.Local;
                if (recordBytes < LocalRecordBytes)
                    return false;
                return true;

            case 0x02014b50:
                kind = Exe2ArcZipRecordKind.Central;
                if (recordBytes < CentralRecordBytes)
                    return false;
                return TryAddOffset(ref memory, record, 42, correction);

            case 0x06054b50:
                kind = Exe2ArcZipRecordKind.EndOfCentralDirectory;
                if (recordBytes < EndRecordBytes)
                    return false;
                return TryAddOffset(ref memory, record, 16, correction);

            default:
                return false;
        }
    }

    private static bool TryAddOffset<TMemory>(ref TMemory memory, APTR record,
        int offset, uint correction)
        where TMemory : struct, IAmigaGuestMemory
    {
        uint value = ReadLittleEndian(ref memory, record, offset);
        if (value > uint.MaxValue - correction)
            return false;
        WriteLittleEndian(ref memory, record, offset, value + correction);
        return true;
    }

    private static uint ReadLittleEndian<TMemory>(ref TMemory memory,
        APTR address, int offset)
        where TMemory : struct, IAmigaGuestMemory =>
        (uint)memory.ReadUInt8(address, offset) |
        ((uint)memory.ReadUInt8(address, offset + 1) << 8) |
        ((uint)memory.ReadUInt8(address, offset + 2) << 16) |
        ((uint)memory.ReadUInt8(address, offset + 3) << 24);

    private static void WriteLittleEndian<TMemory>(ref TMemory memory,
        APTR address, int offset, uint value)
        where TMemory : struct, IAmigaGuestMemory
    {
        memory.WriteUInt8(address, offset, (byte)value);
        memory.WriteUInt8(address, offset + 1, (byte)(value >> 8));
        memory.WriteUInt8(address, offset + 2, (byte)(value >> 16));
        memory.WriteUInt8(address, offset + 3, (byte)(value >> 24));
    }
}
