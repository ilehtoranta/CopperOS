using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Run</c> command.
///
/// ReadArgs owns option syntax and temporary allocations. The command copies
/// the final /F text into caller-owned storage before asking DOS/Shell to
/// create the background process.
/// </summary>
public static class RunCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR commandBuffer,
        uint commandCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || tokenBuffer.IsNull || commandBuffer.IsNull ||
            tokenCapacity == 0 || commandCapacity == 0 ||
            RangesOverlap(tokenBuffer, tokenCapacity,
                commandBuffer, commandCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Run, 20,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 20, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var stackAddress = APTR.FromPointer(platform.ReadUInt32(resultArray, 8));
        var priorityAddress = APTR.FromPointer(platform.ReadUInt32(resultArray, 12));
        var command = APTR.FromPointer(platform.ReadUInt32(resultArray, 16));
        if (!ReadNumberPointer(ref platform, stackAddress, out var stack) ||
            !ReadNumberPointer(ref platform, priorityAddress, out var priority) ||
            command.IsNull ||
            !ReadArgsCommandSupport.CopyCString(ref platform, command,
                commandBuffer, commandCapacity, out var commandLength) ||
            commandLength == 0)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        var detach = platform.ReadUInt32(resultArray, 0) != 0 ? 1u : 0u;
        var quiet = platform.ReadUInt32(resultArray, 4) != 0 ? 1u : 0u;
        var stackPresent = stackAddress.IsNotNull ? 1u : 0u;
        var priorityPresent = priorityAddress.IsNotNull ? 1u : 0u;
        platform.FreeArgs(rdArgs);

        if (invocation.Continuation.IsNotNull &&
            !ShellProcessContinuationTransitions.TryStart(
                ref platform, invocation.Continuation))
            return (int)ShellCommandResult.Error;

        var launched = platform.TryRunCommand(invocation.Cli,
                invocation.Input, invocation.Output, invocation.Error,
                invocation.CurrentDirectory, invocation.Continuation,
                commandBuffer,
                commandLength, detach, quiet, stack, stackPresent,
                unchecked((int)priority), priorityPresent);
        if (!launched && invocation.Continuation.IsNotNull)
            ShellProcessContinuationTransitions.TryFail(
                ref platform,
                invocation.Continuation,
                (int)ShellCommandResult.Fail);
        return launched
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }

    private static bool ReadNumberPointer<TPlatform>(
        ref TPlatform platform,
        APTR address,
        out uint value)
        where TPlatform : struct, IShellPlatform
    {
        value = 0;
        if (address.IsNull)
            return true;
        if (!platform.IsMapped(address, 4))
            return false;
        value = platform.ReadUInt32(address);
        return true;
    }

    private static bool RangesOverlap(
        APTR first,
        uint firstLength,
        APTR second,
        uint secondLength)
    {
        if (first.Raw > uint.MaxValue - firstLength ||
            second.Raw > uint.MaxValue - secondLength)
            return true;
        var firstEnd = first.Raw + firstLength;
        var secondEnd = second.Raw + secondLength;
        return first.Raw < secondEnd && second.Raw < firstEnd;
    }
}
