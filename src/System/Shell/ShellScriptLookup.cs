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
