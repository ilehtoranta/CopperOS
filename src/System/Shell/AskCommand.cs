using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Ask</c> command.
/// </summary>
public static class AskCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR promptBuffer,
        uint promptCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || invocation.Input.IsNull ||
            invocation.Output.IsNull || promptBuffer.IsNull ||
            promptCapacity == 0)
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, promptBuffer,
                promptCapacity, ReadArgsCommandTemplate.Ask, 4,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, promptBuffer, templateLength,
                resultArray, 4, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var prompt = APTR.FromPointer(platform.ReadUInt32(resultArray));
        if (!ReadArgsCommandSupport.CopyCString(ref platform, prompt,
                promptBuffer, promptCapacity, out var promptLength) ||
            promptLength == 0)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        platform.FreeArgs(rdArgs);

        return platform.TryAsk(
                invocation.Cli,
                invocation.Input,
                invocation.Output,
                promptBuffer,
                promptLength)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
