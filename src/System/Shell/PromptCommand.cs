using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Prompt</c> command.
/// </summary>
public static class PromptCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR valueBuffer,
        uint valueCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || valueBuffer.IsNull || valueCapacity == 0)
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, valueBuffer,
                valueCapacity, ReadArgsCommandTemplate.Prompt,
                ReadArgsPointerResultRecord.Size,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, valueBuffer, templateLength,
                resultArray, ReadArgsPointerResultRecord.Size,
                out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        if (!ReadArgsPointerResultRecordCodec.TryRead(ref platform,
                resultArray, out var parsed))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        var value = parsed.Value;
        var reset = value.IsNull ? 1u : 0u;
        uint valueLength = 0;
        if (value.IsNotNull && !ReadArgsCommandSupport.CopyCString(
                ref platform, value, valueBuffer, valueCapacity,
                out valueLength))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Fail;
        }
        platform.FreeArgs(rdArgs);

        var promptValue = valueBuffer;
        if (reset != 0) promptValue = APTR.FromPointer(0);
        return platform.TrySetPrompt(
                invocation.Cli,
                promptValue,
                valueLength,
                reset)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
