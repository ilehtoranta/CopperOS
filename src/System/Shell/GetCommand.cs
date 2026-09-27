using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Get</c> command for local CLI variables.
/// </summary>
public static class GetCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR valueBuffer,
        uint valueCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || invocation.Output.IsNull ||
            tokenBuffer.IsNull || valueBuffer.IsNull ||
            tokenCapacity == 0 || valueCapacity == 0 ||
            valueBuffer.Raw > uint.MaxValue - valueCapacity ||
            !platform.IsMapped(valueBuffer, valueCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Name,
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

        var nameAddress = parsed.Value;
        if (!CStringCodec.TryReadLength(ref platform, nameAddress, 65536,
                out var nameLength))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Fail;
        }

        uint valueLength;
        if (!platform.TryGetLocalVariable(invocation.Cli, nameAddress,
                nameLength, valueBuffer, valueCapacity,
                out valueLength))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        platform.FreeArgs(rdArgs);

        if (valueLength > valueCapacity ||
            (valueLength != 0 && !platform.IsMapped(valueBuffer, valueLength)))
            return (int)ShellCommandResult.Fail;

        if (valueLength != 0)
        {
            int written = platform.Write(invocation.Output, valueBuffer, valueLength);
            if (written < 0 || (uint)written != valueLength)
                return (int)ShellCommandResult.Error;
        }

        return platform.WriteByte(invocation.Output, (byte)'\n') < 0
            ? (int)ShellCommandResult.Error
            : (int)ShellCommandResult.Ok;
    }
}
