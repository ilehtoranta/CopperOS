namespace CopperOS.Shell;

/// <summary>
/// Fixed-width control requests consumed by the active Shell/script frame.
/// </summary>
public enum ShellControlAction : int
{
    Else = 1,
    EndIf = 2,
    EndSkip = 3,
    EndCli = 4,
    EndShell = 5,
    Quit = 6,
}
