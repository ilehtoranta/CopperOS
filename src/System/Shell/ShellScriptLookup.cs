using Amiga;

namespace CopperOS.Shell;

/// <summary>Owner-selected lookup result for one non-internal command.</summary>
public enum ShellScriptLookupKind : uint
{
    NotFound = 0,
    Resident = 1,
    ExplicitFile = 2,
    CurrentDirectory = 3,
    CommandPath = 4,
    Script = 5,
    Malformed = 6,
}

/// <summary>Where DOS found the resolved command candidate.</summary>
public enum ShellScriptLookupOrigin : uint
{
    None = 0,
    Resident = 1,
    ExplicitFile = 2,
    CurrentDirectory = 3,
    CommandPath = 4,
}

/// <summary>
/// Named lookup handoff shared by the Shell engine and its DOS owner. The
/// resolved path points into the caller-owned lookup workspace.
/// </summary>
public struct ShellScriptLookupResult
{
    public ShellScriptLookupKind Kind;
    public ShellScriptLookupOrigin Origin;
    public FileProtection Protection;
    public APTR ResolvedPath;
    public uint PathLength;
}

/// <summary>A bounded byte slice in caller-owned guest memory.</summary>
public struct ShellScriptTextSlice
{
    public ShellScriptTextSlice(APTR data, uint length)
    {
        Data = data;
        Length = length;
    }

    public APTR Data;
    public uint Length;

    public bool IsContainedBy(APTR owner, uint ownerLength)
    {
        if (Length == 0 && Data.IsNull) return true;
        if (owner.IsNull || Data.IsNull ||
            owner.Raw > uint.MaxValue - ownerLength || Data.Raw < owner.Raw)
            return false;
        var relative = Data.Raw - owner.Raw;
        return relative <= ownerLength && Length <= ownerLength - relative;
    }
}

/// <summary>
/// Parsed external command text passed to DOS for launch. The command name is
/// already decoded; Arguments is the bounded raw tail to preserve for the
/// child command.
/// </summary>
public struct ShellScriptCommandInvocation
{
    public APTR Line;
    public uint LineLength;
    public APTR CommandName;
    public uint CommandNameLength;
    public ShellScriptTextSlice Arguments;
}

/// <summary>
/// Caller-owned path storage for a platform lookup result. Resident results
/// may not need a path; file results must provide one when the capacity is
/// enabled.
/// </summary>
public struct ShellScriptLookupWorkspace
{
    public ShellScriptLookupWorkspace(APTR path, uint capacity)
    {
        Path = path;
        Capacity = capacity;
    }

	public APTR Path { get; set; }
	public uint Capacity { get; set; }

    public bool IsEnabled => !Path.IsNull && Capacity >= 2;
}
