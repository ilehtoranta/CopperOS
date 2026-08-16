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
        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Fault, 4,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;
        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 4, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var listAddress = APTR.FromPointer(platform.ReadUInt32(resultArray));
        if (listAddress.IsNull)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        uint codeCount = 0;
        while (codeCount < codeCapacity)
        {
            var listOffset = codeCount * 4;
            if (listAddress.Raw > uint.MaxValue - listOffset - 4 ||
                !platform.IsMapped(listAddress, listOffset + 4))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Fail;
            }
            var numberAddress = APTR.FromPointer(
                platform.ReadUInt32(listAddress, (int)listOffset));
            if (numberAddress.IsNull)
                break;
            if (numberAddress.Raw > uint.MaxValue - 4 ||
                !platform.IsMapped(numberAddress, 4))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Fail;
            }
            platform.WriteUInt32(errorCodeBuffer, (int)listOffset,
                platform.ReadUInt32(numberAddress));
            codeCount++;
        }

        if (codeCount == codeCapacity &&
            (listAddress.Raw > uint.MaxValue - codeCount * 4 - 4 ||
             !platform.IsMapped(listAddress, codeCount * 4 + 4) ||
             platform.ReadUInt32(listAddress, (int)(codeCount * 4)) != 0))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        platform.FreeArgs(rdArgs);
        if (codeCount == 0)
            return (int)ShellCommandResult.Error;

        return platform.TryWriteFault(
                invocation.Output,
                errorCodeBuffer,
                codeCount)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
