using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Stack</c> command.
///
/// With no argument it reports the current CLI default stack.  With one
/// decimal argument it changes only that default for future child commands;
/// the platform boundary intentionally provides no operation for changing the
/// stack of the command that is already executing.
/// </summary>
public static class StackCommand
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
                tokenCapacity, ReadArgsCommandTemplate.Stack, 4,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 4, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var requestedAddress = APTR.FromPointer(platform.ReadUInt32(resultArray));
        var hasRequestedStack = requestedAddress.IsNotNull;
        var requestedStack = hasRequestedStack
            ? platform.ReadUInt32(requestedAddress)
            : 0;
        platform.FreeArgs(rdArgs);

        if (!hasRequestedStack)
        {
            if (invocation.Output.IsNull)
                return (int)ShellCommandResult.Fail;
            if (!platform.TryReadCliDefaultStack(
                    invocation.Cli,
                    out var currentStack) || currentStack < 0)
                return (int)ShellCommandResult.Fail;
            return WriteStackSize(ref platform, invocation.Output, currentStack);
        }

        if (requestedStack == 0 || requestedStack > int.MaxValue)
            return (int)ShellCommandResult.Error;

        return platform.TryWriteCliDefaultStack(
                invocation.Cli,
                (int)requestedStack)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }

    private static int WriteStackSize<TPlatform>(
        ref TPlatform platform,
        BPTR output,
        int stackBytes)
        where TPlatform : struct, IShellPlatform
    {
        uint value = (uint)stackBytes;
        uint divisor = 1;
        while (value / divisor >= 10 && divisor <= uint.MaxValue / 10)
            divisor *= 10;

        do
        {
            byte digit = (byte)('0' + value / divisor);
            if (platform.WriteByte(output, digit) < 0)
                return (int)ShellCommandResult.Error;
            value %= divisor;
            divisor /= 10;
        }
        while (divisor != 0);

        return platform.WriteByte(output, (byte)'\n') < 0
            ? (int)ShellCommandResult.Error
            : (int)ShellCommandResult.Ok;
    }
}
