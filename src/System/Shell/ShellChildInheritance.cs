using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Fixed-width stream and directory state inherited by a child CLI. The
/// parent CLI, variables, aliases, command path, failure policy, and stack
/// defaults remain DOS-owned. The typed CLI snapshot carries the scalar
/// defaults needed by the launch policy without retaining a managed process
/// object or exposing serialized field offsets.
/// </summary>
public readonly struct ShellChildInheritance
{
    public ShellChildInheritance(
        BPTR input,
        BPTR output,
        BPTR error,
        BPTR currentDirectory,
        CommandLineInterface cliState)
    {
        Input = input;
        Output = output;
        Error = error;
        CurrentDirectory = currentDirectory;
        CliState = cliState;
    }

    public BPTR Input { get; }
    public BPTR Output { get; }
    public BPTR Error { get; }
    public BPTR CurrentDirectory { get; }
    public CommandLineInterface CliState { get; }
}
