using Amiga;

namespace CopperOS.Shell;

/// <summary>Guest-resident position for one named script label.</summary>
public struct ShellScriptLabelState
{
    public APTR Parent;
    public APTR Name;
    public uint NameLength;
    public uint Line;
    public uint Offset;
}

/// <summary>Codec for one fixed-width label record.</summary>
public static class ShellScriptLabelCodec
{
    public const uint Magic = 0x5343_4C42;
    public const uint Version = 1;
    public const uint Size = 28;

    public static bool Initialize<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellScriptLabelState initial)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidName(ref platform, initial.Name, initial.NameLength) ||
            record.IsNull || !platform.IsMapped(record, Size))
            return false;
        platform.Clear(record, Size);
        platform.WriteUInt32(record, 0, Magic);
        platform.WriteUInt32(record, 4, Version);
        WriteState(ref platform, record, in initial);
        return true;
    }

    public static bool TryRead<TPlatform>(
        ref TPlatform platform,
        APTR record,
        out ShellScriptLabelState state)
        where TPlatform : struct, IShellPlatform
    {
        state = default;
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            platform.ReadUInt32(record, 0) != Magic ||
            platform.ReadUInt32(record, 4) != Version)
            return false;
        state.Parent = APTR.FromPointer(platform.ReadUInt32(record, 8));
        state.Name = APTR.FromPointer(platform.ReadUInt32(record, 12));
        state.NameLength = platform.ReadUInt32(record, 16);
        state.Line = platform.ReadUInt32(record, 20);
        state.Offset = platform.ReadUInt32(record, 24);
        return ValidName(ref platform, state.Name, state.NameLength);
    }

    public static bool NamesEqualNoCase<TPlatform>(
        ref TPlatform platform,
        APTR first,
        uint firstLength,
        APTR second,
        uint secondLength)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidName(ref platform, first, firstLength) ||
            !ValidName(ref platform, second, secondLength) ||
            firstLength != secondLength)
            return false;
        for (var index = 0u; index < firstLength; index++)
        {
            var left = Lower(platform.ReadUInt8(first, (int)index));
            var right = Lower(platform.ReadUInt8(second, (int)index));
            if (left != right)
                return false;
        }
        return true;
    }

    private static bool ValidName<TPlatform>(
        ref TPlatform platform,
        APTR name,
        uint length)
        where TPlatform : struct, IShellPlatform =>
        !name.IsNull && length != 0 &&
        name.Raw <= uint.MaxValue - length &&
        platform.IsMapped(name, length);

    private static byte Lower(byte value) => value is >= (byte)'A' and <=
        (byte)'Z' ? (byte)(value + 32) : value;

    private static bool WriteState<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellScriptLabelState state)
        where TPlatform : struct, IShellPlatform
    {
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            !ValidName(ref platform, state.Name, state.NameLength))
            return false;
        platform.WriteUInt32(record, 8, state.Parent.Raw);
        platform.WriteUInt32(record, 12, state.Name.Raw);
        platform.WriteUInt32(record, 16, state.NameLength);
        platform.WriteUInt32(record, 20, state.Line);
        platform.WriteUInt32(record, 24, state.Offset);
        return true;
    }
}
