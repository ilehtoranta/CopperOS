using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shared implementation for MorphOS <c>NewCLI</c> and <c>NewShell</c>.
/// Both forms use the optional <c>WINDOW,FROM</c> ReadArgs template; only the
/// launch identity differs.
/// </summary>
public static class NewShellCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR windowBuffer,
        uint windowCapacity,
        APTR fromBuffer,
        uint fromCapacity)
        where TPlatform : struct, IShellPlatform =>
        Execute(ref platform, in invocation, tokenBuffer, tokenCapacity,
            windowBuffer, windowCapacity, fromBuffer, fromCapacity,
            ShellLaunchKind.NewShell);

    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR windowBuffer,
        uint windowCapacity,
        APTR fromBuffer,
        uint fromCapacity,
        ShellLaunchKind kind)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || tokenBuffer.IsNull ||
            windowBuffer.IsNull || fromBuffer.IsNull || tokenCapacity == 0 ||
            windowCapacity == 0 || fromCapacity == 0 ||
            RangesOverlap(tokenBuffer, tokenCapacity, windowBuffer,
                windowCapacity) ||
            RangesOverlap(tokenBuffer, tokenCapacity, fromBuffer,
                fromCapacity) ||
            RangesOverlap(windowBuffer, windowCapacity, fromBuffer,
                fromCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.NewShell, 8,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;
        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 8, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var window = APTR.FromPointer(platform.ReadUInt32(resultArray));
        var from = APTR.FromPointer(platform.ReadUInt32(resultArray, 4));
        if (!CopyOptional(ref platform, window, windowBuffer, windowCapacity,
                out var windowLength) ||
            !CopyOptional(ref platform, from, fromBuffer, fromCapacity,
                out var fromLength))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        platform.FreeArgs(rdArgs);

        if (invocation.Continuation.IsNotNull &&
            !ShellProcessContinuationTransitions.TryStart(
                ref platform, invocation.Continuation))
            return (int)ShellCommandResult.Error;

        var windowArgument = windowBuffer;
        if (window.IsNull) windowArgument = APTR.FromPointer(0);
        var fromArgument = fromBuffer;
        if (from.IsNull) fromArgument = APTR.FromPointer(0);
        var launched = platform.TryCreateShell(invocation.Cli, kind,
                invocation.Input, invocation.Output, invocation.Error,
                invocation.CurrentDirectory, invocation.Continuation,
                windowArgument,
                windowLength,
                fromArgument,
                fromLength);
        if (!launched && invocation.Continuation.IsNotNull)
            ShellProcessContinuationTransitions.TryFail(
                ref platform,
                invocation.Continuation,
                (int)ShellCommandResult.Fail);
        return launched
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }

    private static bool CopyOptional<TPlatform>(
        ref TPlatform platform,
        APTR source,
        APTR destination,
        uint capacity,
        out uint length)
        where TPlatform : struct, IShellPlatform
    {
        length = 0;
        return source.IsNull || ReadArgsCommandSupport.CopyCString(
            ref platform, source, destination, capacity, out length);
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
