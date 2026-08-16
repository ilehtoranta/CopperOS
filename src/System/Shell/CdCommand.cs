using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>CD</c> command.
///
/// Directory locks, current-directory lifetime, and path resolution remain
/// DOS responsibilities. The command only decodes one bounded path token or
/// copies the DOS-owned current path to the inherited output.
/// </summary>
public static class CdCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR pathBuffer,
        uint pathCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || tokenBuffer.IsNull || pathBuffer.IsNull ||
            tokenCapacity == 0 || pathCapacity == 0 ||
            tokenBuffer.Raw > uint.MaxValue - tokenCapacity ||
            pathBuffer.Raw > uint.MaxValue - pathCapacity ||
            !platform.IsMapped(tokenBuffer, tokenCapacity) ||
            !platform.IsMapped(pathBuffer, pathCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Dir, 4,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 4, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var directory = APTR.FromPointer(platform.ReadUInt32(resultArray));
        if (directory.IsNotNull)
        {
            if (!ReadArgsCommandSupport.CopyCString(ref platform, directory,
                    pathBuffer, pathCapacity, out var pathLength) ||
                pathLength == 0)
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
            platform.FreeArgs(rdArgs);
            return platform.TryChangeCurrentDirectory(
                    invocation.Cli, pathBuffer, pathLength)
                ? (int)ShellCommandResult.Ok
                : (int)ShellCommandResult.Error;
        }

        platform.FreeArgs(rdArgs);
        if (invocation.Output.IsNull ||
            !platform.TryGetCurrentDirectory(
                invocation.Cli, pathBuffer, pathCapacity,
                out var currentLength) || currentLength >= pathCapacity)
            return (int)ShellCommandResult.Fail;

        if (currentLength != 0)
        {
            int written = platform.Write(
                invocation.Output, pathBuffer, currentLength);
            if (written < 0 || (uint)written != currentLength)
                return (int)ShellCommandResult.Error;
        }

        return platform.WriteByte(invocation.Output, (byte)'\n') < 0
            ? (int)ShellCommandResult.Error
            : (int)ShellCommandResult.Ok;
    }
}
