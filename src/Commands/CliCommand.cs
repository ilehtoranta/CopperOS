using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands;

/// <summary>
/// Bounded external MorphOS <c>C:CLI</c> launcher candidate. It has a distinct
/// launch identity from the Shell's NewCLI/NewShell internals and delegates
/// process, stream, directory, and continuation ownership to the Shell/DOS
/// owner. The packed 3.20 command's nonempty-tail behavior remains unverified.
/// </summary>
public static class CliCommand
{
    public static int Execute<TPlatform>(ref TPlatform platform,
        in CommandInvocation invocation)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || invocation.ArgumentLength != 0)
            return (int)ShellCommandResult.Error;

        if (invocation.Continuation.IsNotNull &&
            !ShellProcessContinuationTransitions.TryStart(
                ref platform, invocation.Continuation))
            return (int)ShellCommandResult.Error;

        var launched = platform.TryCreateShell(invocation.Cli,
            ShellLaunchKind.Cli, invocation.Input, invocation.Output,
            invocation.Error, invocation.CurrentDirectory, invocation.Continuation,
            APTR.Null, 0, APTR.Null, 0);
        if (!launched && invocation.Continuation.IsNotNull)
            ShellProcessContinuationTransitions.TryFail(ref platform,
                invocation.Continuation, (int)ShellCommandResult.Fail);
        return launched
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
