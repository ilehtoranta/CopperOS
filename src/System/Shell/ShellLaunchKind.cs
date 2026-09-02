namespace CopperOS.Shell;

/// <summary>Distinguishes invocation-owned interactive child-shell launches.</summary>
public enum ShellLaunchKind : int
{
    NewCli = 1,
    NewShell = 2,
    /// <summary>External MorphOS C:CLI launcher.</summary>
    Cli = 3,
}
