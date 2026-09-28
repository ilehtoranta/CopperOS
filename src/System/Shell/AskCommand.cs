using Amiga;

namespace CopperOS.Shell;

/// <summary>The bounded answer accepted by MorphOS <c>Ask</c>.</summary>
public enum ShellAskAnswer : uint
{
    No = 0,
    Yes = 1,
}

/// <summary>Named response returned by the DOS-owned interactive prompt.</summary>
public struct ShellAskResponse
{
    public ShellAskAnswer Answer;

    public static bool TryDecode<TPlatform>(ref TPlatform platform,
        APTR line, uint lineCapacity, out ShellAskResponse response)
        where TPlatform : struct, IShellPlatform
    {
        response = default;
        if (line.IsNull || lineCapacity == 0 ||
            line.Raw > uint.MaxValue - lineCapacity ||
            !platform.IsMapped(line, lineCapacity))
            return false;

        for (var offset = 0u; offset < lineCapacity; offset++)
        {
            var value = platform.ReadUInt8(line, unchecked((int)offset));
            if (value is (byte)'\r' or (byte)'\n') continue;
            if (value == 0 || value is (byte)'N' or (byte)'n')
            {
                response.Answer = ShellAskAnswer.No;
                return true;
            }
            if (value is (byte)'Y' or (byte)'y')
            {
                response.Answer = ShellAskAnswer.Yes;
                return true;
            }
            return false;
        }
        return false;
    }
}

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
                promptCapacity, ReadArgsCommandTemplate.Ask,
                ReadArgsPointerResultRecord.Size,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, promptBuffer, templateLength,
                resultArray, ReadArgsPointerResultRecord.Size,
                out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        if (!ReadArgsPointerResultRecordCodec.TryRead(ref platform,
                resultArray, out var parsed))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        var prompt = parsed.Value;
        if (!ReadArgsCommandSupport.CopyCString(ref platform, prompt,
                promptBuffer, promptCapacity, out var promptLength) ||
            promptLength == 0)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        platform.FreeArgs(rdArgs);

        if (!platform.TryAsk(
                invocation.Cli,
                invocation.Input,
                invocation.Output,
                promptBuffer,
                promptLength,
                out var response))
            return (int)ShellCommandResult.Fail;

        return response.Answer switch
        {
            ShellAskAnswer.Yes => (int)ShellCommandResult.Ok,
            ShellAskAnswer.No => (int)ShellCommandResult.Warn,
            _ => (int)ShellCommandResult.Error,
        };
    }
}
