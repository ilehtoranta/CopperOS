using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>FailAt</c> command.
///
/// The command changes the active command-sequence failure threshold. The
/// sequence owner is responsible for restoring its default when the sequence
/// ends; this command does not retain a second Shell-side copy of that state.
/// </summary>
public static class FailatCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || tokenBuffer.IsNull || tokenCapacity == 0)
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Failat, 4,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 4, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var failureAddress = APTR.FromPointer(platform.ReadUInt32(resultArray));
        var failureLimit = failureAddress.IsNotNull
            ? platform.ReadUInt32(failureAddress)
            : 0;
        platform.FreeArgs(rdArgs);
        if (failureAddress.IsNull || failureLimit == 0)
            return (int)ShellCommandResult.Error;

        return platform.TryWriteCliFailureLimit(
                invocation.Cli,
                failureLimit)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
