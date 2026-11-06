using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned index of live top-level runners. All storage is supplied and
/// released by the Shell; the registry does not allocate or close resources.
/// Callers must serialize access while publishing or removing an entry.
/// </summary>
public static class ShellRunnerRegistry
{
    public const uint RegistrySize = 16;
    public const uint EntrySize = 24;
    public const uint MaximumEntries = 256;
    private const uint RegistryMagic = 0x53525247; // SRRG
    private const uint EntryMagic = 0x53525245; // SRRE
    private const uint Version = 1;

    public static bool Initialize<TMemory>(ref TMemory memory, APTR registry)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!Mapped(ref memory, registry, RegistrySize) ||
            memory.ReadUInt32(registry, 0) == RegistryMagic) return false;
        memory.Clear(registry, RegistrySize);
        memory.WriteUInt32(registry, 0, RegistryMagic);
        memory.WriteUInt32(registry, 4, Version);
        return true;
    }

    /// <summary>Clears an empty registry before its storage is released.</summary>
    public static bool TryDestroy<TMemory>(ref TMemory memory, APTR registry)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (!Scan(ref memory, registry, APTR.Null, APTR.Null, APTR.Null,
            APTR.Null, out var count, out _, out _, out _) || count != 0)
            return false;
        memory.Clear(registry, RegistrySize);
        return true;
    }

    /// <summary>Publishes a previously unlinked entry for one CLI.</summary>
    public static bool TryPublish<TMemory>(ref TMemory memory, APTR registry,
        APTR entry, APTR cli, APTR frame, APTR runner)
        where TMemory : struct, IAmigaGuestMemory
    {
        if (cli.IsNull || frame.IsNull || runner.IsNull ||
            !Mapped(ref memory, entry, EntrySize) ||
            memory.ReadUInt32(entry, 0) == EntryMagic ||
            Overlaps(registry, RegistrySize, entry, EntrySize) ||
            !Scan(ref memory, registry, cli, entry, frame, runner, out var count,
                out var foundCli, out var foundEntry, out _) ||
            foundCli.IsNotNull || foundEntry.IsNotNull || count >= MaximumEntries)
            return false;

        var head = memory.ReadUInt32(registry, 8);
        memory.Clear(entry, EntrySize);
        memory.WriteUInt32(entry, 0, EntryMagic);
        memory.WriteUInt32(entry, 4, registry.Raw);
        memory.WriteUInt32(entry, 8, head);
        memory.WriteUInt32(entry, 12, cli.Raw);
        memory.WriteUInt32(entry, 16, frame.Raw);
        memory.WriteUInt32(entry, 20, runner.Raw);
        memory.WriteUInt32(registry, 8, entry.Raw);
        memory.WriteUInt32(registry, 12, count + 1);
        return true;
    }

    /// <summary>Finds a live entry after validating the entire registry.</summary>
    public static bool TryFind<TMemory>(ref TMemory memory, APTR registry, APTR cli,
        out APTR entry, out APTR frame, out APTR runner)
        where TMemory : struct, IAmigaGuestMemory
    {
        entry = APTR.Null; frame = APTR.Null; runner = APTR.Null;
        if (cli.IsNull || !Scan(ref memory, registry, cli, APTR.Null, APTR.Null, APTR.Null,
            out _, out var found, out _, out _) || found.IsNull) return false;
        entry = found;
        frame = APTR.FromPointer(memory.ReadUInt32(found, 16));
        runner = APTR.FromPointer(memory.ReadUInt32(found, 20));
        return true;
    }

    /// <summary>Unlinks and clears an entry; the caller retains its storage.</summary>
    public static bool TryRemove<TMemory>(ref TMemory memory, APTR registry,
        APTR entry) where TMemory : struct, IAmigaGuestMemory
    {
        if (entry.IsNull || !Scan(ref memory, registry, APTR.Null, entry, APTR.Null, APTR.Null,
            out var count, out _, out var found, out var previous) || found.IsNull)
            return false;
        var next = memory.ReadUInt32(found, 8);
        if (previous.IsNull) memory.WriteUInt32(registry, 8, next);
        else memory.WriteUInt32(previous, 8, next);
        memory.WriteUInt32(registry, 12, count - 1);
        memory.Clear(found, EntrySize);
        return true;
    }

    private static bool Scan<TMemory>(ref TMemory memory, APTR registry,
        APTR wantedCli, APTR wantedEntry, APTR wantedFrame, APTR wantedRunner,
        out uint count, out APTR foundCli,
        out APTR foundEntry, out APTR previousEntry)
        where TMemory : struct, IAmigaGuestMemory
    {
        count = 0; foundCli = APTR.Null; foundEntry = APTR.Null; previousEntry = APTR.Null;
        if (!Mapped(ref memory, registry, RegistrySize) ||
            memory.ReadUInt32(registry, 0) != RegistryMagic ||
            memory.ReadUInt32(registry, 4) != Version) return false;
        count = memory.ReadUInt32(registry, 12);
        if (count > MaximumEntries) return false;
        var current = APTR.FromPointer(memory.ReadUInt32(registry, 8));
        var previous = APTR.Null;
        for (var i = 0u; i < count; i++)
        {
            if (!Mapped(ref memory, current, EntrySize) ||
                Overlaps(registry, RegistrySize, current, EntrySize) ||
                (wantedEntry.IsNotNull && current.Raw != wantedEntry.Raw &&
                 Overlaps(wantedEntry, EntrySize, current, EntrySize)) ||
                memory.ReadUInt32(current, 0) != EntryMagic ||
                memory.ReadUInt32(current, 4) != registry.Raw ||
                memory.ReadUInt32(current, 12) == 0 ||
                memory.ReadUInt32(current, 16) == 0 ||
                memory.ReadUInt32(current, 20) == 0) return false;
            if ((wantedFrame.IsNotNull && memory.ReadUInt32(current, 16) == wantedFrame.Raw) ||
                (wantedRunner.IsNotNull && memory.ReadUInt32(current, 20) == wantedRunner.Raw)) return false;
            if (wantedCli.IsNotNull && memory.ReadUInt32(current, 12) == wantedCli.Raw)
            {
                if (foundCli.IsNotNull) return false;
                foundCli = current;
            }
            if (wantedEntry.IsNotNull && current.Raw == wantedEntry.Raw)
            {
                if (foundEntry.IsNotNull) return false;
                foundEntry = current; previousEntry = previous;
            }
            previous = current;
            current = APTR.FromPointer(memory.ReadUInt32(current, 8));
        }
        return current.IsNull;
    }

    private static bool Mapped<TMemory>(ref TMemory memory, APTR address, uint size)
        where TMemory : struct, IAmigaGuestMemory =>
        address.IsNotNull && (address.Raw & 3) == 0 &&
        address.Raw <= uint.MaxValue - size && memory.IsMapped(address, size);

    private static bool Overlaps(APTR a, uint aSize, APTR b, uint bSize) =>
        a.Raw < b.Raw + bSize && b.Raw < a.Raw + aSize;
}
