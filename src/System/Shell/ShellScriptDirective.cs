using Amiga;

namespace CopperOS.Shell;

/// <summary>One recognized Execute script directive.</summary>
public enum ShellScriptDirectiveKind : byte
{
    None = 0,
    Comment = 1,
    Key = 2,
    Default = 3,
    Bra = 4,
    Ket = 5,
    Dollar = 6,
    Dot = 7,
    Invalid = 255,
}

/// <summary>Bounded lexical classification for Execute dot lines.</summary>
public static class ShellScriptDirective
{
    /// <summary>
    /// Classifies only a line whose first byte is a dot. The caller keeps
    /// directive state and value ownership; this helper neither allocates nor
    /// interprets a KEY template or substituted text.
    /// </summary>
    public static bool TryParse<TPlatform>(
        ref TPlatform platform,
        APTR source,
        uint length,
        out ShellScriptDirectiveKind kind,
        out uint argumentOffset)
        where TPlatform : struct, IShellPlatform => TryParse(ref platform,
            source, length, (byte)'.', out kind, out argumentOffset);

    /// <summary>Classifies a line using the runner's current dot character.</summary>
    public static bool TryParse<TPlatform>(
        ref TPlatform platform,
        APTR source,
        uint length,
        byte dot,
        out ShellScriptDirectiveKind kind,
        out uint argumentOffset)
        where TPlatform : struct, IShellPlatform
    {
        kind = ShellScriptDirectiveKind.None;
        argumentOffset = 0;
        if (length == 0) return true;
        if (source.IsNull || source.Raw > uint.MaxValue - length ||
            !platform.IsMapped(source, length))
            return false;
        if (dot == 0 || platform.ReadUInt8(source, 0) != dot) return true;
        if (length == 1)
        {
            kind = ShellScriptDirectiveKind.Invalid;
            return true;
        }

        var second = platform.ReadUInt8(source, 1);
        if (IsSpace(second) || second == (byte)'\\')
        {
            kind = ShellScriptDirectiveKind.Comment;
            argumentOffset = 2;
            return true;
        }

        uint end = 1;
        while (end < length && !IsSpace(platform.ReadUInt8(source, (int)end)))
            end++;
        argumentOffset = end;
        while (argumentOffset < length &&
            IsSpace(platform.ReadUInt8(source, (int)argumentOffset)))
            argumentOffset++;

        var name = APTR.FromPointer(source.Raw + 1);
        var nameLength = end - 1;
        kind = ShellTextParser.EqualsPacked(ref platform, name, nameLength,
                0x4B455900u, 0) ||
            ShellTextParser.EqualsPacked(ref platform, name, nameLength,
                0x4B000000u, 0)
            ? ShellScriptDirectiveKind.Key
            : ShellTextParser.EqualsPacked(ref platform, name, nameLength,
                    0x44454641u, 0x554C5400u) ||
                ShellTextParser.EqualsPacked(ref platform, name, nameLength,
                    0x44454600u, 0)
                ? ShellScriptDirectiveKind.Default
                : ShellTextParser.EqualsPacked(ref platform, name, nameLength,
                    0x42524100u, 0)
                    ? ShellScriptDirectiveKind.Bra
                    : ShellTextParser.EqualsPacked(ref platform, name,
                        nameLength, 0x4B455400u, 0)
                        ? ShellScriptDirectiveKind.Ket
                        : ShellTextParser.EqualsPacked(ref platform, name,
                            nameLength, 0x444F4C4Cu, 0x41520000u) ||
                            ShellTextParser.EqualsPacked(ref platform, name,
                                nameLength, 0x444F4C00u, 0)
                            ? ShellScriptDirectiveKind.Dollar
                            : ShellTextParser.EqualsPacked(ref platform, name,
                                nameLength, 0x444F5400u, 0)
                                ? ShellScriptDirectiveKind.Dot
                                : ShellScriptDirectiveKind.Invalid;
        return true;
    }

    private static bool IsSpace(byte value) =>
        value is (byte)' ' or (byte)'\t';
}
