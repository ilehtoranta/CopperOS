namespace CopperOS.Commands.NativeExecution;

/// <summary>
/// Invocation-owned filesystem state for the recursive command fixture.
/// BPTR identities are unique per acquisition, so duplicate ownership cannot
/// accidentally pass because two locks happen to name the same directory.
/// </summary>
internal sealed class CopyRecursiveFileSystem
{
    private readonly HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase)
        { "SYS:", "SYS:first", "SYS:second", "RAM:", "Work:" };
    private readonly Dictionary<string, byte[]> files = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SYS:first/child"] = [1, 2, 3, 4, 5, 6, 7, 8],
        ["SYS:second/child"] = [11, 12, 13, 14, 15, 16, 17, 18],
    };
    private readonly Dictionary<uint, string> locks = [];
    private uint nextLock = 0x800;
    public const uint BorrowedCurrentDirectory = 0x700;
    public uint CurrentDirectory { get; private set; } = BorrowedCurrentDirectory;
    public int CreatedDirectories { get; private set; }
    public int LiveLocks => locks.Count;

    private readonly bool nested;
    public CopyRecursiveFileSystem(bool nested=false,bool dangling=false)
    {
        this.nested=nested;
        if(dangling){directories.Remove("SYS:first");files.Remove("SYS:first/child");}
        if(nested){directories.Add("SYS:first/deep");files["SYS:first/deep/child"]=files["SYS:first/child"];files.Remove("SYS:first/child");}
    }

    public string Resolve(string name)
    {
        if (name.Contains(':')) return name;
        var parent = Name(CurrentDirectory);
        return parent + (parent.EndsWith(':') ? "" : "/") + name;
    }

    public string Name(uint handle) => handle == BorrowedCurrentDirectory ? "Work:"
        : locks.TryGetValue(handle, out var name) ? name
        : throw new InvalidOperationException($"Unknown or released recursive lock {handle:x}.");

    public uint Lock(string name)
    {
        name = Resolve(name);
        if (!directories.Contains(name) && !files.ContainsKey(name)) return 0;
        var handle = nextLock++;
        locks.Add(handle, name);
        return handle;
    }

    public uint CreateDirectory(string name)
    {
        name = Resolve(name);
        if (directories.Contains(name) || files.ContainsKey(name) || !directories.Contains(ParentName(name)))
            throw new InvalidOperationException($"Unexpected recursive directory creation: {name}.");
        directories.Add(name);
        CreatedDirectories++;
        return Lock(name);
    }

    public uint Parent(uint handle)
    {
        var name = Name(handle);
        return name.EndsWith(':') ? 0 : Lock(ParentName(name));
    }

    public void Unlock(uint handle)
    {
        if (handle == 0) return; // DOS UnLock(NULL) is permitted.
        if (handle == CurrentDirectory || !locks.Remove(handle))
            throw new InvalidOperationException($"Invalid recursive lock release: {handle:x}.");
    }

    public uint ChangeDirectory(uint handle)
    {
        if (!directories.Contains(Name(handle)))
            throw new InvalidOperationException("CurrentDir requires a directory lock.");
        var previous = CurrentDirectory;
        CurrentDirectory = handle;
        return previous;
    }

    public bool SameDevice(uint first, uint second) =>
        Name(first).Split(':')[0].Equals(Name(second).Split(':')[0], StringComparison.OrdinalIgnoreCase);

    public byte[] ReadSource(string name) => files.TryGetValue(Resolve(name), out var bytes)
        ? (byte[])bytes.Clone() : throw new InvalidOperationException($"Missing recursive source: {name}.");

    public void WriteDestination(string name, byte[] bytes)
    {
        name = Resolve(name);
        if (!name.StartsWith("RAM:", StringComparison.OrdinalIgnoreCase) || !directories.Contains(ParentName(name)))
            throw new InvalidOperationException($"Wrong recursive output path: {name}.");
        files[name] = (byte[])bytes.Clone();
    }

    public readonly List<string> Deleted = [];
    public void Delete(string name)
    {
        name=Resolve(name);
        if(!name.StartsWith("SYS:")||name.EndsWith(':'))throw new InvalidOperationException("Unexpected recursive deletion.");
        if(files.Remove(name)){Deleted.Add(name);return;}
        if(files.Keys.Any(p=>p.StartsWith(name+"/"))||directories.Any(p=>p.StartsWith(name+"/"))||!directories.Remove(name))
            throw new InvalidOperationException("Recursive directory deleted before children.");
        Deleted.Add(name);
    }

    public void VerifyDeleted()
    {
        string[] expected=nested?["SYS:first/deep/child","SYS:first/deep","SYS:first","SYS:second/child","SYS:second"]:["SYS:first/child","SYS:first","SYS:second/child","SYS:second"];
        if(LiveLocks!=0||CurrentDirectory!=BorrowedCurrentDirectory||CreatedDirectories!=0||!Deleted.SequenceEqual(expected)||files.Keys.Any(p=>p.StartsWith("RAM:")))
            throw new InvalidOperationException("Recursive DELETE changed destination or leaked resources.");
    }

    public void VerifyComplete(bool moved=false)
    {
        if (LiveLocks != 0 || CurrentDirectory != BorrowedCurrentDirectory || CreatedDirectories != (nested?3:2) ||
            !ReadSource(nested?"RAM:first/deep/child":"RAM:first/child").SequenceEqual(new byte[]{1,2,3,4,5,6,7,8}) ||
            !ReadSource("RAM:second/child").SequenceEqual(new byte[]{11,12,13,14,15,16,17,18}))
            throw new InvalidOperationException("Recursive output or lock ownership is incomplete.");
        string[] expected=nested?["SYS:first/deep/child","SYS:first/deep","SYS:first","SYS:second/child","SYS:second"]:["SYS:first/child","SYS:first","SYS:second/child","SYS:second"];
        if(moved?!Deleted.SequenceEqual(expected):Deleted.Count!=0)
            throw new InvalidOperationException("Recursive source deletion order differs.");
    }

    public void VerifyStoppedInsideFirstDirectory(bool copied=false)
    {
        if(LiveLocks!=0||CurrentDirectory!=BorrowedCurrentDirectory||CreatedDirectories!=1||Deleted.Count!=0||
            !directories.Contains("RAM:first")||directories.Contains("RAM:second")||
            (copied ? files.Keys.Count(p=>p.StartsWith("RAM:"))!=1||!ReadSource("RAM:first/child").SequenceEqual(new byte[]{1,2,3,4,5,6,7,8}) : files.Keys.Any(p=>p.StartsWith("RAM:")))||
            !ReadSource("SYS:first/child").SequenceEqual(new byte[]{1,2,3,4,5,6,7,8})||
            !ReadSource("SYS:second/child").SequenceEqual(new byte[]{11,12,13,14,15,16,17,18}))
            throw new InvalidOperationException("Early matcher termination changed pending-file behavior or leaked its destination lock.");
    }

    public void VerifyDanglingSkipped(bool stopped=false)
    {
        if(LiveLocks!=0||CurrentDirectory!=BorrowedCurrentDirectory||CreatedDirectories!=(stopped?0:1)||Deleted.Count!=0||
            directories.Contains("RAM:first")||files.Keys.Count(p=>p.StartsWith("RAM:"))!=(stopped?0:1)||
            (stopped?directories.Contains("RAM:second"):!ReadSource("RAM:second/child").SequenceEqual(new byte[]{11,12,13,14,15,16,17,18}))||
            !ReadSource("SYS:second/child").SequenceEqual(new byte[]{11,12,13,14,15,16,17,18}))
            throw new InvalidOperationException("Dangling-link traversal changed skipped destination or later payload.");
    }

    private static string ParentName(string name)
    {
        var slash = name.LastIndexOf('/');
        return slash >= 0 ? name[..slash] : name[..(name.IndexOf(':') + 1)];
    }
}
