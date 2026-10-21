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
        => ExecuteCore(ref platform, in invocation, tokenBuffer, tokenCapacity,
            errorCodeBuffer, errorCodeCapacity, false);

    internal static int ExecuteUsingReadArgs<TPlatform>(ref TPlatform platform,
        in CommandInvocation invocation, APTR tokenBuffer, uint tokenCapacity)
        where TPlatform : struct, IShellPlatform
        => ExecuteCore(ref platform, in invocation, tokenBuffer, tokenCapacity,
            APTR.Null, MaximumErrorCodes * 4, true);

    private static int ExecuteCore<TPlatform>(ref TPlatform platform,
        in CommandInvocation invocation, APTR tokenBuffer, uint tokenCapacity,
        APTR errorCodeBuffer, uint errorCodeCapacity, bool useReadArgsStorage)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Output.IsNull || tokenBuffer.IsNull ||
            tokenCapacity == 0 ||
            errorCodeCapacity < 4 ||
            errorCodeCapacity > MaximumErrorCodes * 4 ||
            (!useReadArgsStorage && (errorCodeBuffer.IsNull ||
             errorCodeBuffer.Raw > uint.MaxValue - errorCodeCapacity ||
             !platform.IsMapped(errorCodeBuffer, errorCodeCapacity))))
            return (int)ShellCommandResult.Fail;

        uint codeCapacity = errorCodeCapacity / 4;
        var codes = default(FaultErrorCodeBuffer);
        if (!useReadArgsStorage && !FaultErrorCodeBufferCodec.TryCreate(ref platform,
                errorCodeBuffer, errorCodeCapacity, out codes))
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

        // ReadArgs owns both the pointer list and its numeric values until FreeArgs.
        // Flatten only each list slot already validated by the cursor; the next
        // pointer and terminal word remain intact. FreeArgs uses its allocation
        // chain, independently of these caller-owned result values.
        if (useReadArgsStorage)
            codes = new FaultErrorCodeBuffer { Base = listAddress, CapacityBytes = errorCodeCapacity };

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
        if (codes.Count == 0)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        if (!useReadArgsStorage) platform.FreeArgs(rdArgs);
        var written = platform.TryWriteFault(invocation.Output, codes.Base, codes.Count);
        if (useReadArgsStorage) platform.FreeArgs(rdArgs);
        return written ? (int)ShellCommandResult.Ok : (int)ShellCommandResult.Fail;
    }
}
