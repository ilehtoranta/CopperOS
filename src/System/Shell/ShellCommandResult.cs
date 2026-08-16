namespace CopperOS.Shell;

/// <summary>
/// AmigaDOS command return levels.  These values are part of the command
/// contract and are intentionally represented without exceptions.
/// </summary>
public enum ShellCommandResult : int
{
    Ok = 0,
    Warn = 5,
    Error = 10,
    Fail = 20,
    /// <summary>
    /// The command has yielded to the DOS scheduler and must be resumed by
    /// the owning CLI. This is an internal Shell/DOS handoff, not an AmigaDOS
    /// return level exposed to child programs.
    /// </summary>
    Pending = 30,
}
