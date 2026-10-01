using Amiga;

namespace CopperOS.Commands;

public enum Exe2ArcArchiveType : uint
{
    Unspecified = 0,
    Zip = 1,
    Ace = 2,
    Rar = 3,
    Cabinet = 4,
    Arj = 5,
    Lha = 6,
    Lzh = 7,
}

/// <summary>
/// Matches Exe2Arc's explicit TYPE argument against the exact extension or
/// display name used by its scanner table. Matching is ASCII case-insensitive,
/// has no prefix acceptance, and performs no allocation or DOS call.
/// </summary>
public static class Exe2ArcTypeSelection
{
    public const uint MaximumTypeBytes = 64;

    public static bool TrySelect<TMemory>(ref TMemory memory, APTR type,
        out Exe2ArcArchiveType selected)
        where TMemory : struct, IAmigaGuestMemory
    {
        selected = Exe2ArcArchiveType.Unspecified;
        if (type.IsNull)
            return true;
        uint length = 0;
        for (; length < MaximumTypeBytes; length++)
        {
            byte current = memory.ReadUInt8(type, unchecked((int)length));
            if (current == 0)
                break;
        }
        if (length == MaximumTypeBytes)
            return false;

        selected = MatchesZip(ref memory, type, length)
            ? Exe2ArcArchiveType.Zip
            : MatchesAce(ref memory, type, length)
                ? Exe2ArcArchiveType.Ace
                : MatchesRar(ref memory, type, length)
                    ? Exe2ArcArchiveType.Rar
                    : MatchesCabinet(ref memory, type, length)
                        ? Exe2ArcArchiveType.Cabinet
                        : MatchesArj(ref memory, type, length)
                            ? Exe2ArcArchiveType.Arj
                            : MatchesLha(ref memory, type, length)
                                ? Exe2ArcArchiveType.Lha
                                : MatchesLzh(ref memory, type, length)
                                    ? Exe2ArcArchiveType.Lzh
                                    : Exe2ArcArchiveType.Unspecified;
        return selected != Exe2ArcArchiveType.Unspecified;
    }

    private static bool MatchesZip<TMemory>(ref TMemory memory, APTR value,
        uint length)
        where TMemory : struct, IAmigaGuestMemory
        => Matches3(ref memory, value, length, (byte)'z', (byte)'i', (byte)'p');

    private static bool MatchesAce<TMemory>(ref TMemory memory, APTR value,
        uint length)
        where TMemory : struct, IAmigaGuestMemory
        => Matches3(ref memory, value, length, (byte)'a', (byte)'c', (byte)'e');

    private static bool MatchesRar<TMemory>(ref TMemory memory, APTR value,
        uint length)
        where TMemory : struct, IAmigaGuestMemory
        => Matches3(ref memory, value, length, (byte)'r', (byte)'a', (byte)'r');

    private static bool MatchesCabinet<TMemory>(ref TMemory memory, APTR value,
        uint length)
        where TMemory : struct, IAmigaGuestMemory
        => Matches3(ref memory, value, length, (byte)'c', (byte)'a', (byte)'b') ||
            MatchesCabinetName(ref memory, value, length);

    private static bool MatchesArj<TMemory>(ref TMemory memory, APTR value,
        uint length)
        where TMemory : struct, IAmigaGuestMemory
        => Matches3(ref memory, value, length, (byte)'a', (byte)'r', (byte)'j');

    private static bool MatchesLha<TMemory>(ref TMemory memory, APTR value,
        uint length)
        where TMemory : struct, IAmigaGuestMemory
        => Matches3(ref memory, value, length, (byte)'l', (byte)'h', (byte)'a');

    private static bool MatchesLzh<TMemory>(ref TMemory memory, APTR value,
        uint length)
        where TMemory : struct, IAmigaGuestMemory
        => Matches3(ref memory, value, length, (byte)'l', (byte)'z', (byte)'h') ||
            MatchesAmigaLha(ref memory, value, length);

    private static bool Matches3<TMemory>(ref TMemory memory, APTR value,
        uint length, byte first, byte second, byte third)
        where TMemory : struct, IAmigaGuestMemory =>
        length == 3 && FoldAscii(memory.ReadUInt8(value, 0)) == FoldAscii(first) &&
        FoldAscii(memory.ReadUInt8(value, 1)) == FoldAscii(second) &&
        FoldAscii(memory.ReadUInt8(value, 2)) == FoldAscii(third);

    private static bool MatchesCabinetName<TMemory>(ref TMemory memory,
        APTR value, uint length)
        where TMemory : struct, IAmigaGuestMemory =>
        length == 7 && FoldAscii(memory.ReadUInt8(value, 0)) == (byte)'c' &&
        FoldAscii(memory.ReadUInt8(value, 1)) == (byte)'a' &&
        FoldAscii(memory.ReadUInt8(value, 2)) == (byte)'b' &&
        FoldAscii(memory.ReadUInt8(value, 3)) == (byte)'i' &&
        FoldAscii(memory.ReadUInt8(value, 4)) == (byte)'n' &&
        FoldAscii(memory.ReadUInt8(value, 5)) == (byte)'e' &&
        FoldAscii(memory.ReadUInt8(value, 6)) == (byte)'t';

    private static bool MatchesAmigaLha<TMemory>(ref TMemory memory,
        APTR value, uint length)
        where TMemory : struct, IAmigaGuestMemory =>
        length == 9 && FoldAscii(memory.ReadUInt8(value, 0)) == (byte)'a' &&
        FoldAscii(memory.ReadUInt8(value, 1)) == (byte)'m' &&
        FoldAscii(memory.ReadUInt8(value, 2)) == (byte)'i' &&
        FoldAscii(memory.ReadUInt8(value, 3)) == (byte)'g' &&
        FoldAscii(memory.ReadUInt8(value, 4)) == (byte)'a' &&
        FoldAscii(memory.ReadUInt8(value, 5)) == (byte)'-' &&
        FoldAscii(memory.ReadUInt8(value, 6)) == (byte)'l' &&
        FoldAscii(memory.ReadUInt8(value, 7)) == (byte)'h' &&
        FoldAscii(memory.ReadUInt8(value, 8)) == (byte)'a';

    private static byte FoldAscii(byte value) =>
        value is >= (byte)'A' and <= (byte)'Z'
            ? (byte)(value + ((byte)'a' - (byte)'A'))
            : value;
}
