using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Fault</c> command.
///
/// Numeric error codes are collected in caller-owned guest memory and handed
/// to DOS for translation. Keeping the error catalogue and formatting in DOS
/// avoids a second, potentially divergent host-side table.
/// </summary>
public static class FaultCommand
{
    public const uint MaximumErrorCodes = 256;

    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR errorCodeBuffer,
        uint errorCodeCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Output.IsNull || tokenBuffer.IsNull ||
            errorCodeBuffer.IsNull || tokenCapacity == 0 ||
            errorCodeCapacity < 4 ||
            errorCodeCapacity > MaximumErrorCodes * 4 ||
            errorCodeBuffer.Raw > uint.MaxValue - errorCodeCapacity ||
            !platform.IsMapped(errorCodeBuffer, errorCodeCapacity))
            return (int)ShellCommandResult.Fail;

        uint codeCapacity = errorCodeCapacity / 4;
        if (!FaultErrorCodeBufferCodec.TryCreate(ref platform,
                errorCodeBuffer, errorCodeCapacity, out var codes))
            return (int)ShellCommandResult.Fail;
        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Fault,
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
        var listAddress = parsed.Value;
        if (listAddress.IsNull)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        FaultReadArgsCodeCursor cursor = new()
        {
            Base = listAddress,
            MaximumCodes = codeCapacity,
        };
        while (true)
        {
            if (!FaultReadArgsCodeCursorCodec.TryReadCurrent(ref platform,
                    ref cursor, out var numberAddress, out var hasValue))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Fail;
            }
            if (!hasValue)
                break;
            if (codes.Count >= codeCapacity)
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
            if (!ReadArgsLongValueRecordCodec.TryRead(ref platform,
                    numberAddress, out var errorCode))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Fail;
            }
            if (!FaultErrorCodeBufferCodec.TryAppend(ref platform,
                    ref codes, unchecked((uint)errorCode.Value)) ||
                !FaultReadArgsCodeCursorCodec.TryAdvance(ref cursor))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Fail;
            }
        }
        platform.FreeArgs(rdArgs);
        if (codes.Count == 0)
            return (int)ShellCommandResult.Error;

        return platform.TryWriteFault(
                invocation.Output,
                errorCodeBuffer,
                codes.Count)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
