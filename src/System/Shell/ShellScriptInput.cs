using Amiga;

namespace CopperOS.Shell;

/// <summary>Guest-resident metadata for one bounded script input buffer.</summary>
public struct ShellScriptInputState
{
    public BPTR Handle;
    public APTR Buffer;
    public uint Capacity;
    public uint Length;
    public uint Cursor;
    public uint Line;
    public uint Offset;
    public uint EndOfFile;
    public uint Error;
}

/// <summary>
/// Encodes script input metadata without storing the script itself.  The
/// actual bytes remain in the caller/platform-owned bounded buffer.
/// </summary>
public static class ShellScriptInputCodec
{
    public const uint Magic = 0x5343_494E;
    public const uint Version = 1;
    public const uint Size = 44;

    public static bool Initialize<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellScriptInputState initial)
        where TPlatform : struct, IShellPlatform
    {
        if (!ValidBuffer(ref platform, initial.Buffer, initial.Capacity) ||
            record.IsNull || !platform.IsMapped(record, Size) ||
            initial.Length >= initial.Capacity ||
            initial.Cursor > initial.Length || initial.EndOfFile > 1)
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
        out ShellScriptInputState state)
        where TPlatform : struct, IShellPlatform
    {
        state = default;
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            platform.ReadUInt32(record, 0) != Magic ||
            platform.ReadUInt32(record, 4) != Version)
            return false;
        state.Handle = BPTR.FromRaw(platform.ReadUInt32(record, 8));
        state.Buffer = APTR.FromPointer(platform.ReadUInt32(record, 12));
        state.Capacity = platform.ReadUInt32(record, 16);
        state.Length = platform.ReadUInt32(record, 20);
        state.Cursor = platform.ReadUInt32(record, 24);
        state.Line = platform.ReadUInt32(record, 28);
        state.Offset = platform.ReadUInt32(record, 32);
        state.EndOfFile = platform.ReadUInt32(record, 36);
        state.Error = platform.ReadUInt32(record, 40);
        return ValidBuffer(ref platform, state.Buffer, state.Capacity) &&
            state.Length < state.Capacity && state.Cursor <= state.Length &&
            state.EndOfFile <= 1;
    }

    public static bool TryRecordLine<TPlatform>(
        ref TPlatform platform,
        APTR record,
        uint line,
        uint offset,
        uint length,
        uint endOfFile)
        where TPlatform : struct, IShellPlatform
    {
        if (endOfFile > 1 || !TryRead(ref platform, record, out var state) ||
            length >= state.Capacity)
            return false;
        state.Length = length;
        state.Cursor = 0;
        state.Line = line;
        state.Offset = offset;
        state.EndOfFile = endOfFile;
        state.Error = 0;
        return WriteState(ref platform, record, in state);
    }

    public static bool TrySetCursor<TPlatform>(
        ref TPlatform platform,
        APTR record,
        uint cursor)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, record, out var state) ||
            cursor > state.Length)
            return false;
        state.Cursor = cursor;
        return WriteState(ref platform, record, in state);
    }

    public static bool TrySetError<TPlatform>(
        ref TPlatform platform,
        APTR record,
        uint error)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, record, out var state))
            return false;
        state.Error = error;
        return WriteState(ref platform, record, in state);
    }

    private static bool ValidBuffer<TPlatform>(
        ref TPlatform platform,
        APTR buffer,
        uint capacity)
        where TPlatform : struct, IShellPlatform =>
        !buffer.IsNull && capacity >= 2 &&
        buffer.Raw <= uint.MaxValue - capacity &&
        platform.IsMapped(buffer, capacity);

    private static bool WriteState<TPlatform>(
        ref TPlatform platform,
        APTR record,
        in ShellScriptInputState state)
        where TPlatform : struct, IShellPlatform
    {
        if (record.IsNull || !platform.IsMapped(record, Size) ||
            !ValidBuffer(ref platform, state.Buffer, state.Capacity) ||
            state.Length >= state.Capacity || state.Cursor > state.Length ||
            state.EndOfFile > 1)
            return false;
        platform.WriteUInt32(record, 8, state.Handle.Raw);
        platform.WriteUInt32(record, 12, state.Buffer.Raw);
        platform.WriteUInt32(record, 16, state.Capacity);
        platform.WriteUInt32(record, 20, state.Length);
        platform.WriteUInt32(record, 24, state.Cursor);
        platform.WriteUInt32(record, 28, state.Line);
        platform.WriteUInt32(record, 32, state.Offset);
        platform.WriteUInt32(record, 36, state.EndOfFile);
        platform.WriteUInt32(record, 40, state.Error);
        return true;
    }
}
