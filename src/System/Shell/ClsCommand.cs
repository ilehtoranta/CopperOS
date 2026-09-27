using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Cls [RESET]</c> command.
/// </summary>
public static class ClsCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Output.IsNull || tokenBuffer.IsNull || tokenCapacity == 0)
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Cls,
                ReadArgsWordResultRecord.Size,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, ReadArgsWordResultRecord.Size,
                out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;
        if (!ReadArgsWordResultRecordCodec.TryRead(ref platform,
                resultArray, out var parsed))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        var reset = parsed.Value;
        platform.FreeArgs(rdArgs);

        return platform.ClearConsole(invocation.Output, reset)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
