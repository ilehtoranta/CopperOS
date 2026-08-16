using Amiga;
using System;

namespace CopperOS.Shell;

/// <summary>Lifecycle and admission flags for one DOS-owned resident entry.</summary>
[Flags]
public enum ShellResidentEntryFlags : uint
{
    None = 0,
    VerifiedPure = 1,
    Unsafe = 2,
    System = 4,
    Deferred = 8,
    Loaded = 16,
    RemovePending = 32,
}

/// <summary>
/// Fixed-width state for a resident command. Names and the segment owner are
/// guest pointers; the resident owner controls their allocation and lifetime.
/// </summary>
public struct ShellResidentEntryState
{
    public APTR Next;
    public APTR Name;
    public uint NameLength;
    public APTR File;
    public uint FileLength;
    public APTR Alias;
    public uint AliasLength;
    public BPTR Segment;
    public APTR SegmentOwner;
    public ShellResidentEntryFlags Flags;
    public uint UseCount;
}

/// <summary>
/// Codec for one guest-resident Resident record. It intentionally does not
/// own a list head, segment allocation, or command strings.
/// </summary>
public static class ShellResidentEntryCodec
{
    public const uint Magic = 0x5352_4553;
    public const uint Version = 1;
    public const uint Size = 56;

    public static bool Initialize<TPlatform>(
        ref TPlatform platform,
        APTR entry,
        in ShellResidentEntryState initial)
        where TPlatform : struct, IShellPlatform
    {
        if (!CanWrite(ref platform, entry) ||
            !ShellResidentPolicy.IsValid(in initial) ||
            !ValidTextPointers(ref platform, in initial))
            return false;
        platform.Clear(entry, Size);
        platform.WriteUInt32(entry, 0, Magic);
        platform.WriteUInt32(entry, 4, Version);
        WriteState(ref platform, entry, in initial);
        return true;
    }

    public static bool TryRead<TPlatform>(
        ref TPlatform platform,
        APTR entry,
        out ShellResidentEntryState state)
        where TPlatform : struct, IShellPlatform
    {
        state = default;
        if (!CanWrite(ref platform, entry) ||
            platform.ReadUInt32(entry, 0) != Magic ||
            platform.ReadUInt32(entry, 4) != Version)
            return false;
        state.Next = APTR.FromPointer(platform.ReadUInt32(entry, 8));
        state.Name = APTR.FromPointer(platform.ReadUInt32(entry, 12));
        state.NameLength = platform.ReadUInt32(entry, 16);
        state.File = APTR.FromPointer(platform.ReadUInt32(entry, 20));
        state.FileLength = platform.ReadUInt32(entry, 24);
        state.Alias = APTR.FromPointer(platform.ReadUInt32(entry, 28));
        state.AliasLength = platform.ReadUInt32(entry, 32);
        state.Segment = BPTR.FromRaw(platform.ReadUInt32(entry, 36));
        state.SegmentOwner = APTR.FromPointer(
            platform.ReadUInt32(entry, 40));
        state.Flags = (ShellResidentEntryFlags)platform.ReadUInt32(entry, 44);
        state.UseCount = platform.ReadUInt32(entry, 48);
        return ShellResidentPolicy.IsValid(in state) &&
            ValidTextPointers(ref platform, in state);
    }

    public static bool TryAcquire<TPlatform>(
        ref TPlatform platform,
        APTR entry)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, entry, out var state) ||
            !ShellResidentPolicy.CanAcquire(in state))
            return false;
        state.UseCount++;
        return WriteState(ref platform, entry, in state);
    }

    public static bool TryRelease<TPlatform>(
        ref TPlatform platform,
        APTR entry)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, entry, out var state) ||
            state.UseCount == 0)
            return false;
        state.UseCount--;
        return WriteState(ref platform, entry, in state);
    }

    public static bool TryMarkRemoval<TPlatform>(
        ref TPlatform platform,
        APTR entry)
        where TPlatform : struct, IShellPlatform
    {
        if (!TryRead(ref platform, entry, out var state) ||
            !ShellResidentPolicy.CanRemove(in state))
            return false;
        state.Flags |= ShellResidentEntryFlags.RemovePending;
        return WriteState(ref platform, entry, in state);
    }

    private static bool WriteState<TPlatform>(
        ref TPlatform platform,
        APTR entry,
        in ShellResidentEntryState state)
        where TPlatform : struct, IShellPlatform
    {
        if (!CanWrite(ref platform, entry) ||
            !ShellResidentPolicy.IsValid(in state) ||
            !ValidTextPointers(ref platform, in state))
            return false;
        platform.WriteUInt32(entry, 8, state.Next.Raw);
        platform.WriteUInt32(entry, 12, state.Name.Raw);
        platform.WriteUInt32(entry, 16, state.NameLength);
        platform.WriteUInt32(entry, 20, state.File.Raw);
        platform.WriteUInt32(entry, 24, state.FileLength);
        platform.WriteUInt32(entry, 28, state.Alias.Raw);
        platform.WriteUInt32(entry, 32, state.AliasLength);
        platform.WriteUInt32(entry, 36, state.Segment.Raw);
        platform.WriteUInt32(entry, 40, state.SegmentOwner.Raw);
        platform.WriteUInt32(entry, 44, (uint)state.Flags);
        platform.WriteUInt32(entry, 48, state.UseCount);
        return true;
    }

    private static bool CanWrite<TPlatform>(
        ref TPlatform platform,
        APTR entry)
        where TPlatform : struct, IShellPlatform =>
        !entry.IsNull && entry.Raw <= uint.MaxValue - Size &&
        platform.IsMapped(entry, Size);

    private static bool ValidTextPointers<TPlatform>(
        ref TPlatform platform,
        in ShellResidentEntryState state)
        where TPlatform : struct, IShellPlatform =>
        ValidText(ref platform, state.Name, state.NameLength, 1) &&
        ValidText(ref platform, state.File, state.FileLength, 0) &&
        ValidText(ref platform, state.Alias, state.AliasLength, 0);

    private static bool ValidText<TPlatform>(
        ref TPlatform platform,
        APTR address,
        uint length,
        uint required)
        where TPlatform : struct, IShellPlatform
    {
        if (required != 0 && (address.IsNull || length == 0))
            return false;
        if (address.IsNull)
            return length == 0;
        if (length == uint.MaxValue ||
            address.Raw > uint.MaxValue - length - 1)
            return false;
        return platform.IsMapped(address, length + 1);
    }
}

/// <summary>
/// Pure resident-entry policy shared by the command owner and registry.
/// Admission is explicit: an unverified forced entry is Unsafe and never
/// silently promoted to VerifiedPure.
/// </summary>
public static class ShellResidentPolicy
{
    private const ShellResidentEntryFlags KnownFlags =
        ShellResidentEntryFlags.VerifiedPure |
        ShellResidentEntryFlags.Unsafe |
        ShellResidentEntryFlags.System |
        ShellResidentEntryFlags.Deferred |
        ShellResidentEntryFlags.Loaded |
        ShellResidentEntryFlags.RemovePending;

    public static bool IsValid(in ShellResidentEntryState state)
    {
        if (state.Name.IsNull || state.NameLength == 0 ||
            state.Name.Raw > uint.MaxValue - state.NameLength ||
            state.File.IsNull != (state.FileLength == 0) ||
            (!state.Alias.IsNull && state.AliasLength == 0) ||
            (state.Alias.IsNull && state.AliasLength != 0) ||
            state.File.Raw > uint.MaxValue - state.FileLength ||
            (!state.Alias.IsNull &&
             state.Alias.Raw > uint.MaxValue - state.AliasLength))
            return false;

        if (((uint)state.Flags & ~(uint)KnownFlags) != 0 ||
            (state.Flags & (ShellResidentEntryFlags.VerifiedPure |
                ShellResidentEntryFlags.Unsafe)) ==
            (ShellResidentEntryFlags.VerifiedPure |
                ShellResidentEntryFlags.Unsafe))
            return false;

        var loaded = (state.Flags & ShellResidentEntryFlags.Loaded) != 0;
        var deferred = (state.Flags & ShellResidentEntryFlags.Deferred) != 0;
        if (loaded == state.Segment.IsNull ||
            (!loaded && !deferred) ||
            (loaded && state.SegmentOwner.IsNull))
            return false;
        return true;
    }

    public static bool CanAcquire(in ShellResidentEntryState state) =>
        IsValid(in state) && state.UseCount != uint.MaxValue &&
        (state.Flags & (ShellResidentEntryFlags.Loaded |
            ShellResidentEntryFlags.RemovePending)) ==
        ShellResidentEntryFlags.Loaded;

    public static bool CanRemove(in ShellResidentEntryState state) =>
        IsValid(in state) && state.UseCount == 0 &&
        (state.Flags & ShellResidentEntryFlags.RemovePending) == 0;

    public static bool TryAdmit(
        ref ShellResidentEntryState state,
        uint verifiedPure,
        uint force,
        uint system,
        uint deferred,
        BPTR segment,
        APTR segmentOwner)
    {
        if (verifiedPure > 1 || force > 1 || system > 1 || deferred > 1 ||
            (verifiedPure != 0 && force != 0) || segment.IsNull !=
            (deferred != 0) || (deferred == 0 && segmentOwner.IsNull))
            return false;

        var flags = ShellResidentEntryFlags.None;
        if (verifiedPure != 0)
            flags |= ShellResidentEntryFlags.VerifiedPure;
        else if (force != 0)
            flags |= ShellResidentEntryFlags.Unsafe;
        if (system != 0)
            flags |= ShellResidentEntryFlags.System;
        if (deferred != 0)
            flags |= ShellResidentEntryFlags.Deferred;
        else
            flags |= ShellResidentEntryFlags.Loaded;

        state.Segment = segment;
        state.SegmentOwner = segmentOwner;
        state.Flags = flags;
        state.UseCount = 0;
        return IsValid(in state);
    }
}
