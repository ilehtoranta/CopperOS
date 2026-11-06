using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>FailAt</c> command.
///
/// The command changes the CLI failure threshold. It remains in effect after
/// a command source or nested script ends, until a later FailAt changes it.
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
                tokenCapacity, ReadArgsCommandTemplate.Failat,
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

        var failureAddress = parsed.Value;
        if (failureAddress.IsNull)
        {
            platform.FreeArgs(rdArgs);
            if (invocation.Output.IsNull ||
                !platform.TryReadCliFailureLimit(invocation.Cli,
                    out var currentFailureLimit))
                return (int)ShellCommandResult.Fail;
            // V40 displays a label before the numeric threshold.
            if (platform.WriteByte(invocation.Output, (byte)'F') < 0 ||
                platform.WriteByte(invocation.Output, (byte)'a') < 0 ||
                platform.WriteByte(invocation.Output, (byte)'i') < 0 ||
                platform.WriteByte(invocation.Output, (byte)'l') < 0 ||
                platform.WriteByte(invocation.Output, (byte)' ') < 0 ||
                platform.WriteByte(invocation.Output, (byte)'l') < 0 ||
                platform.WriteByte(invocation.Output, (byte)'i') < 0 ||
                platform.WriteByte(invocation.Output, (byte)'m') < 0 ||
                platform.WriteByte(invocation.Output, (byte)'i') < 0 ||
                platform.WriteByte(invocation.Output, (byte)'t') < 0 ||
                platform.WriteByte(invocation.Output, (byte)':') < 0 ||
                platform.WriteByte(invocation.Output, (byte)' ') < 0)
                return (int)ShellCommandResult.Error;
            return ShellUnsignedOutput.WriteLine(ref platform,
                invocation.Output, currentFailureLimit);
        }

        if (!ReadArgsLongValueRecordCodec.TryRead(ref platform,
                failureAddress, out var failureLimitValue))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        platform.FreeArgs(rdArgs);
        if (failureLimitValue.Value <= 0)
            return (int)ShellCommandResult.Error;

        return platform.TryWriteCliFailureLimit(
                invocation.Cli,
                (uint)failureLimitValue.Value)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
