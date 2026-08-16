namespace CopperOS.Shell;

/// <summary>Lifecycle of one top-level Execute script runner.</summary>
public enum ShellScriptExecutionStatus : int
{
    Failed = 0,
    Completed = 1,
    Pending = 2,
}
