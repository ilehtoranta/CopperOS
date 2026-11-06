using Amiga;

namespace CopperOS.Shell;

/// <summary>A V40 LAB is a source marker; SKIP reads it from the command source.</summary>
public static class LabCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR stableLabelBuffer,
        uint stableLabelCapacity)
        where TPlatform : struct, IShellPlatform
    {
        // The original command accepts empty, repeated and multiword markers.
        // Registering them at execution time rejects valid loops and adds
        // allocations that a source-scanning SKIP does not need.
        return invocation.Cli.IsNotNull
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
