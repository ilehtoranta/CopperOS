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
                tokenCapacity, ReadArgsCommandTemplate.Quit,
                ReadArgsPointerResultRecord.Size,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, ReadArgsPointerResultRecord.Size,
                out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        if (!ReadArgsPointerResultRecordCodec.TryRead(ref platform,
                resultArray, out var parsed))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        var returnAddress = parsed.Value;
        var returnCode = 0u;
        if (returnAddress.IsNotNull)
        {
            if (!ReadArgsLongValueRecordCodec.TryRead(ref platform,
                    returnAddress, out var returnCodeValue))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
            returnCode = unchecked((uint)returnCodeValue.Value);
        }
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
