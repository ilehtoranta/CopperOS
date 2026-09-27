using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Unalias</c> command.
/// </summary>
public static class UnaliasCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR stableNameBuffer,
        uint stableNameCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || tokenBuffer.IsNull ||
            stableNameBuffer.IsNull || tokenCapacity == 0 ||
            stableNameCapacity == 0 ||
            tokenBuffer.Raw > uint.MaxValue - tokenCapacity ||
            stableNameBuffer.Raw > uint.MaxValue - stableNameCapacity ||
            !platform.IsMapped(tokenBuffer, tokenCapacity) ||
            !platform.IsMapped(stableNameBuffer, stableNameCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Unalias,
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

        var name = parsed.Value;
        if (name.IsNull)
        {
            platform.FreeArgs(rdArgs);
            return invocation.Output.IsNull ||
                !platform.TryWriteAliases(invocation.Output, invocation.Cli)
                ? (int)ShellCommandResult.Fail
                : (int)ShellCommandResult.Ok;
        }

        if (!ReadArgsCommandSupport.CopyCString(ref platform, name,
                stableNameBuffer, stableNameCapacity, out var nameLength) ||
            nameLength == 0)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        platform.FreeArgs(rdArgs);

        return platform.TryRemoveAlias(
                invocation.Cli, stableNameBuffer, nameLength)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
