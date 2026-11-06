using Amiga;

namespace CopperOS.Shell;

/// <summary>Operations needed to own a CLI's public PathLock nodes and locks.</summary>
public interface IShellCommandPathPlatform : IAmigaGuestMemory
{
    APTR AllocatePathNode();
    void FreePathNode(APTR node);
    BPTR DuplicatePathLock(BPTR directory);
    void ReleasePathLock(BPTR directory);
    bool TrySamePathLock(BPTR first, BPTR second, out bool same);
}

/// <summary>Public DOS operations for resolving a packed Path command.</summary>
public interface IShellPackedCommandPathPlatform : IShellCommandPathPlatform
{
    bool TryEnterPathRequesters(uint quiet, out APTR process, out APTR previousWindow);
    void ExitPathRequesters(APTR process, APTR previousWindow);
    BPTR LockPathDirectory(APTR name);
    int GetPathError();
    void SetPathError(int error);
}

public enum ShellCommandPathFailure
{
    None, Invalid, Missing, NoMemory, Full, Platform,
}

/// <summary>Cursor for a CLI path, including cycle detection.</summary>
public struct ShellCommandPathCursor
{
    public ShellCommandPathCursor(BPTR head) { Current = head; Slow = head; Fast = head; }
    public BPTR Current;
    internal BPTR Slow;
    internal BPTR Fast;
}

/// <summary>Operations on the public DOS CLI command-path lock list.</summary>
public static class ShellCommandPath
{
    /// <summary>
    /// Returns the next node and directory lock without taking ownership of
    /// either. A zero, overflowing, or unmapped node is rejected.
    /// </summary>
    public static bool TryReadNode<TMemory>(ref TMemory memory, BPTR node,
        out BPTR next, out BPTR directory) where TMemory : struct, IAmigaGuestMemory
    {
        next = BPTR.Null;
        directory = BPTR.Null;
        if (node.IsNull || node.Raw > (uint.MaxValue >> 2)) return false;
        var address = node.Address;
        if (address.Raw > uint.MaxValue - 8 || !DosPathLockCodec.IsMapped(ref memory, address)) return false;
        var entry = DosPathLockCodec.Read(ref memory, address);
        next = entry.Next;
        directory = entry.Lock;
        return true;
    }

    /// <summary>Advances a borrowed path cursor without imposing a list length limit.</summary>
    public static bool TryAdvance<TMemory>(ref TMemory memory, ref ShellCommandPathCursor cursor,
        out BPTR directory) where TMemory : struct, IAmigaGuestMemory
    {
        directory = BPTR.Null;
        if (cursor.Current.IsNull ||
            !TryReadNode(ref memory, cursor.Current, out var next, out directory) || directory.IsNull)
            return false;
        if (cursor.Fast.IsNotNull)
        {
            if (!TryReadNode(ref memory, cursor.Fast, out var fast, out _)) return false;
            if (fast.IsNotNull && !TryReadNode(ref memory, fast, out fast, out _)) return false;
            if (!TryReadNode(ref memory, cursor.Slow, out var slow, out _)) return false;
            if (fast.IsNotNull && fast == slow) return false;
            cursor.Fast = fast;
            cursor.Slow = slow;
        }
        cursor.Current = next;
        return true;
    }

    /// <summary>
    /// Replaces, extends, or removes entries from a CLI path. The requested
    /// list is borrowed; the new CLI list owns independently duplicated locks
    /// and nodes. Failure leaves the CLI and its original list unchanged.
    /// The caller serializes access to the CLI and owns its existing list;
    /// requested nodes and lock handles must be independent of that list.
    /// </summary>
    public static bool TryUpdate<TPlatform>(ref TPlatform platform, APTR cli,
        BPTR requested, uint operation)
        where TPlatform : struct, IShellCommandPathPlatform =>
        TryUpdate(ref platform, cli, requested, operation, out _);

    public static bool TryUpdate<TPlatform>(ref TPlatform platform, APTR cli,
        BPTR requested, uint operation, out ShellCommandPathFailure failure)
        where TPlatform : struct, IShellCommandPathPlatform
    {
        failure = ShellCommandPathFailure.Invalid;
        if (cli.IsNull || operation > PathCommand.OperationRemove ||
            !DosCommandLineInterfaceCodec.IsMapped(ref platform, cli)) return false;
        var previous = DosCommandLineInterfaceCodec.ReadCommandDirectory(ref platform, cli);
        if (!Validate(ref platform, previous) || !Validate(ref platform, requested) ||
            !Disjoint(ref platform, previous, requested)) return false;

        if (operation == PathCommand.OperationRemove)
        {
            var cursor = requested;
            while (cursor.IsNotNull)
            {
                if (!TryReadNode(ref platform, cursor, out var next, out var directory)) return false;
                if (!Contains(ref platform, previous, directory, out var found))
                { failure = ShellCommandPathFailure.Platform; return false; }
                if (!found) { failure = ShellCommandPathFailure.Missing; return false; }
                cursor = next;
            }
        }

        var candidate = BPTR.Null;
        var tail = APTR.Null;
        var source = operation == PathCommand.OperationReset ? BPTR.Null : previous;
        while (source.IsNotNull)
        {
            if (!TryReadNode(ref platform, source, out var next, out var directory))
            { failure = ShellCommandPathFailure.Invalid; return Fail(ref platform, candidate); }
            var include = true;
            if (operation == PathCommand.OperationRemove &&
                !Contains(ref platform, requested, directory, out include))
            { failure = ShellCommandPathFailure.Platform; return Fail(ref platform, candidate); }
            if (operation == PathCommand.OperationRemove) include = !include;
            if (include && !Append(ref platform, directory, ref candidate, ref tail, out failure))
                return Fail(ref platform, candidate);
            source = next;
        }
        if (operation != PathCommand.OperationRemove)
        {
            source = requested;
            while (source.IsNotNull)
            {
                if (!TryReadNode(ref platform, source, out var next, out var directory))
                { failure = ShellCommandPathFailure.Invalid; return Fail(ref platform, candidate); }
                if (!Contains(ref platform, candidate, directory, out var present))
                { failure = ShellCommandPathFailure.Platform; return Fail(ref platform, candidate); }
                if (!present && !Append(ref platform, directory, ref candidate, ref tail, out failure))
                    return Fail(ref platform, candidate);
                source = next;
            }
        }
        DosCommandLineInterfaceCodec.WriteCommandDirectory(ref platform, cli, candidate);
        ReleaseValidated(ref platform, previous);
        failure = ShellCommandPathFailure.None;
        return true;
    }

    /// <summary>Resolves packed names and applies a Path command transaction.</summary>
    public static bool TryUpdatePacked<TPlatform>(ref TPlatform platform, APTR cli,
        APTR paths, uint bytes, uint count, uint operation, uint quiet)
        where TPlatform : struct, IShellPackedCommandPathPlatform
    {
        if (cli.IsNull || operation > PathCommand.OperationRemove || quiet > 1 ||
            bytes > PathCommand.MaximumPathBytes ||
            (count == 0 && bytes != 0) ||
            (count != 0 && (paths.IsNull || bytes == 0 || paths.Raw > uint.MaxValue - bytes ||
                !platform.IsMapped(paths, bytes))) || !ValidPacked(ref platform, paths, bytes, count))
        {
            platform.SetPathError((int)DOS.Error.ObjectWrongType);
            return false;
        }
        if (!platform.TryEnterPathRequesters(quiet, out var process, out var previousWindow))
        {
            platform.SetPathError((int)DOS.Error.ObjectWrongType);
            return false;
        }
        var head = BPTR.Null;
        var tail = APTR.Null;
        uint position = 0;
        var error = 0;
        for (var index = 0u; index < count; index++)
        {
            var name = APTR.FromPointer(paths.Raw + position);
            while (platform.ReadUInt8(paths, unchecked((int)position++)) != 0) { }
            var directory = platform.LockPathDirectory(name);
            if (directory.IsNull)
            {
                error = platform.GetPathError();
                if (error == 0) error = (int)DOS.Error.ObjectWrongType;
                break;
            }
            var node = platform.AllocatePathNode();
            if (node.IsNull || (node.Raw & 3) != 0 ||
                node.Raw > uint.MaxValue - 8 || !DosPathLockCodec.IsMapped(ref platform, node))
            {
                if (node.IsNotNull) platform.FreePathNode(node);
                platform.ReleasePathLock(directory);
                error = (int)DOS.Error.NoFreeStore;
                break;
            }
            DosPathLockCodec.Write(ref platform, node, new PathLock { Next = BPTR.Null, Lock = directory });
            var link = BPTR.FromAddress(node);
            if (tail.IsNull) head = link;
            else platform.WriteUInt32(tail, 0, link.Raw);
            tail = node;
        }
        platform.ExitPathRequesters(process, previousWindow);
        if (error == 0 && !TryUpdate(ref platform, cli, head, operation, out var failure))
        {
            error = failure switch
            {
                ShellCommandPathFailure.Missing => (int)DOS.Error.ObjectNotFound,
                ShellCommandPathFailure.NoMemory => (int)DOS.Error.NoFreeStore,
                ShellCommandPathFailure.Full => (int)DOS.Error.BufferOverflow,
                ShellCommandPathFailure.Platform => platform.GetPathError(),
                _ => (int)DOS.Error.ObjectWrongType,
            };
            if (error == 0) error = (int)DOS.Error.ObjectWrongType;
        }
        ReleaseValidated(ref platform, head);
        platform.SetPathError(error);
        return error == 0;
    }

    private static bool ValidPacked<TPlatform>(ref TPlatform platform, APTR paths,
        uint bytes, uint count) where TPlatform : struct, IAmigaGuestMemory
    {
        uint position = 0;
        for (var index = 0u; index < count; index++)
        {
            var start = position;
            while (position < bytes && platform.ReadUInt8(paths, unchecked((int)position)) != 0)
                position++;
            if (position == start || position == bytes) return false;
            position++;
        }
        return position == bytes;
    }

    private static bool Validate<TPlatform>(ref TPlatform platform, BPTR head)
        where TPlatform : struct, IShellCommandPathPlatform
    {
        var start = head;
        var walk = new ShellCommandPathCursor(head);
        while (walk.Current.IsNotNull)
        {
            if (!TryAdvance(ref platform, ref walk, out _)) return false;
        }
        while (start.IsNotNull)
        {
            if (!TryReadNode(ref platform, start, out var next, out var firstLock)) return false;
            var cursor = next;
            while (cursor.IsNotNull)
            {
                if (NodesOverlap(start, cursor)) return false;
                if (!TryReadNode(ref platform, cursor, out var nextCursor, out var otherLock) ||
                    firstLock.Raw == otherLock.Raw) return false;
                cursor = nextCursor;
            }
            start = next;
        }
        return true;
    }

    private static bool Disjoint<TPlatform>(ref TPlatform platform, BPTR first, BPTR second)
        where TPlatform : struct, IShellCommandPathPlatform
    {
        var current = first;
        while (current.IsNotNull)
        {
            if (!TryReadNode(ref platform, current, out var nextFirst, out var firstLock)) return false;
            var cursor = second;
            while (cursor.IsNotNull)
            {
                if (NodesOverlap(current, cursor)) return false;
                if (!TryReadNode(ref platform, cursor, out var next, out var secondLock) ||
                    firstLock.Raw == secondLock.Raw) return false;
                cursor = next;
            }
            current = nextFirst;
        }
        return true;
    }

    private static bool NodesOverlap(BPTR first, BPTR second) =>
        first.Raw >= second.Raw ? first.Raw - second.Raw < 2 : second.Raw - first.Raw < 2;

    private static bool Contains<TPlatform>(ref TPlatform platform, BPTR head,
        BPTR directory, out bool found) where TPlatform : struct, IShellCommandPathPlatform
    {
        found = false;
        var cursor = head;
        while (cursor.IsNotNull)
        {
            if (!TryReadNode(ref platform, cursor, out var next, out var item) ||
                !platform.TrySamePathLock(item, directory, out var same)) return false;
            if (same) { found = true; return true; }
            cursor = next;
        }
        return true;
    }

    private static bool Append<TPlatform>(ref TPlatform platform, BPTR directory,
        ref BPTR head, ref APTR tail, out ShellCommandPathFailure failure)
        where TPlatform : struct, IShellCommandPathPlatform
    {
        failure = ShellCommandPathFailure.NoMemory;
        var copy = platform.DuplicatePathLock(directory);
        if (copy.IsNull) return false;
        var node = platform.AllocatePathNode();
        if (node.IsNull || (node.Raw & 3) != 0 ||
            node.Raw > uint.MaxValue - 8 || !DosPathLockCodec.IsMapped(ref platform, node))
        {
            if (node.IsNotNull) platform.FreePathNode(node);
            platform.ReleasePathLock(copy);
            return false;
        }
        DosPathLockCodec.Write(ref platform, node, new PathLock { Next = BPTR.Null, Lock = copy });
        var link = BPTR.FromAddress(node);
        if (tail.IsNull) head = link;
        else platform.WriteUInt32(tail, 0, link.Raw);
        tail = node;
        failure = ShellCommandPathFailure.None;
        return true;
    }

    private static bool Fail<TPlatform>(ref TPlatform platform, BPTR candidate)
        where TPlatform : struct, IShellCommandPathPlatform
    {
        ReleaseValidated(ref platform, candidate);
        return false;
    }

    private static void ReleaseValidated<TPlatform>(ref TPlatform platform, BPTR head)
        where TPlatform : struct, IShellCommandPathPlatform
    {
        var cursor = head;
        while (cursor.IsNotNull)
        {
            var node = cursor.Address;
            var value = DosPathLockCodec.Read(ref platform, node);
            cursor = value.Next;
            platform.ReleasePathLock(value.Lock);
            platform.FreePathNode(node);
        }
    }
}
