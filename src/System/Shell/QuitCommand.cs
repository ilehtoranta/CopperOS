using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Quit</c> command.
/// </summary>
public static class QuitCommand
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
                tokenCapacity, ReadArgsCommandTemplate.Quit, 4,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 4, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var returnAddress = APTR.FromPointer(platform.ReadUInt32(resultArray));
        var returnCode = returnAddress.IsNotNull
            ? platform.ReadUInt32(returnAddress)
            : 0;
        platform.FreeArgs(rdArgs);
        if (returnCode > int.MaxValue)
            return (int)ShellCommandResult.Error;

        return RequestQuit(ref platform, invocation.Cli, (int)returnCode);
    }

    private static int RequestQuit<TPlatform>(
        ref TPlatform platform,
        APTR cli,
        int returnCode)
        where TPlatform : struct, IShellPlatform =>
        platform.TryRequestShellControl(
                cli,
                ShellControlAction.Quit,
                returnCode)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
}
