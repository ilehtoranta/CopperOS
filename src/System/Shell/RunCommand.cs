using Amiga;

namespace CopperOS.Shell;

/// <summary>Launches the rest of the command line in a background Shell.</summary>
public static class RunCommand
{
    public static int Execute<TPlatform>(ref TPlatform platform,
        in CommandInvocation invocation, APTR tokenBuffer, uint tokenCapacity,
        APTR commandBuffer, uint commandCapacity) where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || tokenBuffer.IsNull || commandBuffer.IsNull ||
            tokenCapacity == 0 || commandCapacity == 0 ||
            RangesOverlap(tokenBuffer, tokenCapacity, commandBuffer, commandCapacity))
            return (int)ShellCommandResult.Fail;

        var source = invocation.ArgumentText;
        var length = invocation.ArgumentLength;
        if (length != 0 && (source.IsNull || source.Raw > uint.MaxValue - length ||
            !platform.IsMapped(source, length)))
            return (int)ShellCommandResult.Error;
        var start = 0u;
        while (start < length && (platform.ReadUInt8(source, (int)start) is (byte)' ' or (byte)'\t'))
            start++;
        var commandLength = length - start;
        if (commandLength != 0 && platform.ReadUInt8(source, (int)start) == (byte)';')
            commandLength = 0;
        if (commandLength >= commandCapacity || !platform.IsMapped(commandBuffer, commandCapacity))
            return (int)ShellCommandResult.Error;

        // The V40 Shell passes the unparsed remainder to its child. In
        // particular, quotes, star escapes, redirections, and words such as
        // DETACH and STACK belong to the command, not to RUN itself.
        if (commandLength != 0)
            platform.Copy(APTR.FromPointer(source.Raw + start), commandBuffer, commandLength);
        platform.WriteUInt8(commandBuffer, (int)commandLength, 0);

        if (invocation.Continuation.IsNotNull &&
            !ShellProcessContinuationTransitions.TryStart(ref platform, invocation.Continuation))
            return (int)ShellCommandResult.Error;

        var launched = platform.TryRunCommand(invocation.Cli,
            invocation.Input, invocation.Output, invocation.Error,
            invocation.OwnedStreams, invocation.CurrentDirectory, invocation.Continuation,
            commandBuffer, commandLength, 0, 0, 0, 0, 0, 0);
        if (!launched && invocation.Continuation.IsNotNull)
            ShellProcessContinuationTransitions.TryFail(ref platform,
                invocation.Continuation, (int)ShellCommandResult.Fail);
        return launched ? (int)ShellCommandResult.Ok : (int)ShellCommandResult.Fail;
    }

    private static bool RangesOverlap(APTR first, uint firstLength,
        APTR second, uint secondLength)
    {
        if (first.Raw > uint.MaxValue - firstLength || second.Raw > uint.MaxValue - secondLength)
            return true;
        return first.Raw < second.Raw + secondLength && second.Raw < first.Raw + firstLength;
    }
}
