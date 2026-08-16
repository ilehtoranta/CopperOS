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
                tokenCapacity, ReadArgsCommandTemplate.Name, 4,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 4, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var nameAddress = APTR.FromPointer(platform.ReadUInt32(resultArray));
        var nameLength = ReadCStringLength(ref platform, nameAddress, 65536);
        if (nameLength < 0)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Fail;
        }

        uint valueLength;
        if (!platform.TryGetLocalVariable(invocation.Cli, nameAddress,
                (uint)nameLength, valueBuffer, valueCapacity,
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

    private static int ReadCStringLength<TPlatform>(
        ref TPlatform platform,
        APTR value,
        uint maximum)
        where TPlatform : struct, IShellPlatform
    {
        if (value.IsNull) return -1;
        for (var index = 0u; index < maximum; index++)
        {
            if (value.Raw > uint.MaxValue - index ||
                !platform.IsMapped(value, index + 1))
                return -1;
            if (platform.ReadUInt8(value, (int)index) == 0)
                return (int)index;
        }
        return -1;
    }
}
