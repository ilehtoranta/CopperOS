using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Caller-owned buffer for one bounded command-alias expansion. Alias lookup,
/// replacement storage, and recursive-depth policy remain with DOS/CLI.
/// </summary>
public struct ShellScriptAliasWorkspace
{
    public ShellScriptAliasWorkspace(APTR line, uint capacity)
    {
        Line = line;
        Capacity = capacity;
    }

	public APTR Line { get; set; }
	public uint Capacity { get; set; }

    public bool IsEnabled => !Line.IsNull && Capacity >= 2;
}
