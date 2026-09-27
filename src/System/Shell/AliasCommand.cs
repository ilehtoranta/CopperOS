using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Alias</c> command.
///
/// Alias records are owned by the CLI/DOS implementation. Named invocations
/// pass bounded decoded guest text to that owner; the no-argument form only
/// requests its canonical listing.
/// </summary>
public static class AliasCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR nameBuffer,
        uint nameCapacity,
        APTR valueBuffer,
        uint valueCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || nameBuffer.IsNull || valueBuffer.IsNull ||
            nameCapacity == 0 || valueCapacity == 0 ||
            valueBuffer.Raw > uint.MaxValue - valueCapacity ||
            !platform.IsMapped(valueBuffer, valueCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, nameBuffer,
                nameCapacity, ReadArgsCommandTemplate.Alias,
                NameAndValueReadArgsResultRecord.Size,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, nameBuffer, templateLength,
                resultArray, NameAndValueReadArgsResultRecord.Size,
                out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        if (!NameAndValueReadArgsResultRecordCodec.TryRead(ref platform,
                resultArray, out var parsed))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        var name = parsed.Name;
        var value = parsed.Value;
        if (name.IsNull)
        {
            platform.FreeArgs(rdArgs);
            return invocation.Output.IsNull ||
                !platform.TryWriteAliases(invocation.Output, invocation.Cli)
                ? (int)ShellCommandResult.Fail
                : (int)ShellCommandResult.Ok;
        }

        uint nameLength;
        uint valueLength = 0;
        if (!ReadArgsCommandSupport.CopyCString(ref platform, name, nameBuffer,
                nameCapacity, out nameLength) ||
            (value.IsNotNull && !ReadArgsCommandSupport.CopyCString(
                ref platform, value, valueBuffer, valueCapacity,
                out valueLength)))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Fail;
        }
        valueLength = value.IsNull ? 0 : valueLength;
        platform.FreeArgs(rdArgs);

        return platform.TrySetAlias(
                invocation.Cli,
                nameBuffer,
                nameLength,
                valueBuffer,
                valueLength)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
