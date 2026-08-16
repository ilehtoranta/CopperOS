using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Fixed-width stream and directory state inherited by a child CLI. The
/// parent CLI, variables, aliases, command path, failure policy, and stack
/// defaults remain DOS-owned; this record makes the stream boundary explicit
/// without retaining a managed process object.
/// </summary>
public readonly struct ShellChildInheritance
{
    public ShellChildInheritance(
        BPTR input,
        BPTR output,
        BPTR error,
        BPTR currentDirectory)
    {
        Input = input;
        Output = output;
        Error = error;
        CurrentDirectory = currentDirectory;
    }

    public BPTR Input { get; }
    public BPTR Output { get; }
    public BPTR Error { get; }
    public BPTR CurrentDirectory { get; }
}
