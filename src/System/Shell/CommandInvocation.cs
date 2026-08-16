using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Fixed-width state passed to a command invocation.
///
/// The layout deliberately contains guest pointers and DOS handles rather than
/// host strings, streams, or object references.  Native entry lowering will
/// map the argument length/text and library-base fields to the Amiga command
/// registers; the remaining fields are populated from the current CLI.
/// </summary>
public readonly struct CommandInvocation
{
    public CommandInvocation(
        APTR argumentText,
        uint argumentLength,
        APTR dosBase,
        APTR execBase,
        BPTR input,
        BPTR output,
        BPTR error,
        BPTR currentDirectory,
        APTR cli,
        int returnLevel,
        int ioError)
        : this(
            argumentText,
            argumentLength,
            dosBase,
            execBase,
            input,
            output,
            error,
            currentDirectory,
            cli,
            returnLevel,
            ioError,
            APTR.Null)
    {
    }

    public CommandInvocation(
        APTR argumentText,
        uint argumentLength,
        APTR dosBase,
        APTR execBase,
        BPTR input,
        BPTR output,
        BPTR error,
        BPTR currentDirectory,
        APTR cli,
        int returnLevel,
        int ioError,
        APTR continuation)
    {
        ArgumentText = argumentText;
        ArgumentLength = argumentLength;
        DosBase = dosBase;
        ExecBase = execBase;
        Input = input;
        Output = output;
        Error = error;
        CurrentDirectory = currentDirectory;
        Cli = cli;
        ReturnLevel = returnLevel;
        IoError = ioError;
        Continuation = continuation;
    }

    public APTR ArgumentText { get; }
    public uint ArgumentLength { get; }
    public APTR DosBase { get; }
    public APTR ExecBase { get; }
    public BPTR Input { get; }
    public BPTR Output { get; }
    public BPTR Error { get; }
    public BPTR CurrentDirectory { get; }
    public APTR Cli { get; }
    public int ReturnLevel { get; }
    public int IoError { get; }
    public APTR Continuation { get; }

    /// <summary>
    /// Creates the smallest useful invocation for a command semantic test.
    /// Native entry roots should use the full constructor so inherited CLI
    /// state is never silently discarded.
    /// </summary>
    public static CommandInvocation ForOutput(
        APTR argumentText,
        uint argumentLength,
        BPTR output) => new(
            argumentText,
            argumentLength,
            APTR.Null,
            APTR.Null,
            BPTR.Null,
            output,
            BPTR.Null,
            BPTR.Null,
            APTR.Null,
            0,
            0);
}
