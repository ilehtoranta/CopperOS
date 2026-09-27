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
                tokenCapacity, ReadArgsCommandTemplate.Stack,
                StackReadArgsResultRecord.Size,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, StackReadArgsResultRecord.Size,
                out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        if (!StackReadArgsResultRecordCodec.TryRead(ref platform,
                resultArray, out var parsed))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        var hasRequestedStack = parsed.StackNumber.IsNotNull;
        var requestedStack = 0u;
        if (hasRequestedStack)
        {
            if (!ReadArgsLongValueRecordCodec.TryRead(ref platform,
                    parsed.StackNumber, out var stackSize))
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
            requestedStack = unchecked((uint)stackSize.Value);
        }
        platform.FreeArgs(rdArgs);

        if (!hasRequestedStack)
        {
            if (invocation.Output.IsNull)
                return (int)ShellCommandResult.Fail;
            if (!platform.TryReadCliDefaultStack(
                    invocation.Cli,
                    out var currentStack) || currentStack < 0)
                return (int)ShellCommandResult.Fail;
            return ShellUnsignedOutput.WriteLine(ref platform,
                invocation.Output, (uint)currentStack);
        }

        if (requestedStack == 0 || requestedStack > int.MaxValue)
            return (int)ShellCommandResult.Error;

        return platform.TryWriteCliDefaultStack(
                invocation.Cli,
                (int)requestedStack)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }

}
