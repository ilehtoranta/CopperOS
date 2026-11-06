using Amiga;

namespace CopperOS.Shell;

/// <summary>Shell access to the public fields of an AmigaDOS CLI.</summary>
public static class ShellCliState
{
    public static bool TryReadDefaultStack<TMemory>(ref TMemory memory, APTR cli,
        out int bytes) where TMemory : struct, IAmigaGuestMemory
    {
        bytes = 0;
        if (!Valid(ref memory, cli)) return false;
        var value = DosCommandLineInterfaceCodec.Read(ref memory, cli);
        bytes = unchecked(value.DefaultStack << 2);
        return true;
    }

    public static bool TryWriteDefaultStack<TMemory>(ref TMemory memory, APTR cli,
        int bytes) where TMemory : struct, IAmigaGuestMemory
    {
        if (bytes < 0 || !Valid(ref memory, cli)) return false;
        DosCommandLineInterfaceCodec.WriteDefaultStack(ref memory, cli, bytes >> 2);
        return true;
    }

    public static bool TryReadFailureLimit<TMemory>(ref TMemory memory, APTR cli,
        out uint limit) where TMemory : struct, IAmigaGuestMemory
    {
        limit = 0;
        if (!Valid(ref memory, cli)) return false;
        var value = DosCommandLineInterfaceCodec.Read(ref memory, cli);
        limit = value.FailLevel > 0 ? unchecked((uint)value.FailLevel) :
            ShellScriptFrameCodec.DefaultFailureLimit;
        return true;
    }

    public static bool TryWriteFailureLimit<TMemory>(ref TMemory memory, APTR cli,
        uint limit) where TMemory : struct, IAmigaGuestMemory
    {
        if (limit > int.MaxValue || !Valid(ref memory, cli)) return false;
        DosCommandLineInterfaceCodec.WriteFailLevel(ref memory, cli, unchecked((int)limit));
        return true;
    }

    /// <summary>Clears the length byte of the CLI-owned command-file BSTR.</summary>
    public static bool TryClearCommandFile<TMemory>(ref TMemory memory, APTR cli)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!Valid(ref memory, cli)) return false;
        var name = DosCommandLineInterfaceCodec.Read(ref memory, cli).CommandFile.Address;
        if (name.IsNull || !memory.IsMapped(name, 1)) return false;
        memory.WriteUInt8(name, 0, 0);
        return true;
    }

    private static bool Valid<TMemory>(ref TMemory memory, APTR cli)
        where TMemory : struct, IAmigaGuestMemory =>
        cli.IsNotNull && cli.Raw <= uint.MaxValue - DosCommandLineInterfaceCodec.Size &&
        DosCommandLineInterfaceCodec.IsMapped(ref memory, cli);
}
