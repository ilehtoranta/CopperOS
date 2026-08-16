using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned named form of MorphOS <c>Unset NAME</c>.
///
/// The no-argument form asks the CLI owner for its canonical local-variable
/// listing; the Shell does not retain or format a variable map.
/// </summary>
public static class UnsetCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR nameBuffer,
        uint nameCapacity,
        APTR checkBuffer,
        uint checkCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || nameBuffer.IsNull || checkBuffer.IsNull ||
            nameCapacity == 0 || checkCapacity == 0 ||
            checkBuffer.Raw > uint.MaxValue - checkCapacity ||
            !platform.IsMapped(checkBuffer, checkCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, nameBuffer,
                nameCapacity, ReadArgsCommandTemplate.UnsetOptional, 4,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, nameBuffer, templateLength,
                resultArray, 4, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;
        var name = APTR.FromPointer(platform.ReadUInt32(resultArray));
        if (name.IsNull)
        {
            platform.FreeArgs(rdArgs);
            return invocation.Output.IsNull ||
                !platform.TryWriteLocalVariables(invocation.Output,
                    invocation.Cli)
                ? (int)ShellCommandResult.Fail
                : (int)ShellCommandResult.Ok;
        }

        var nameLength = ReadCStringLength(ref platform, name, 65536);
        if (nameLength < 0)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Fail;
        }

        var removed = platform.TryRemoveLocalVariable(invocation.Cli, name,
                (uint)nameLength);
        platform.FreeArgs(rdArgs);
        return removed
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
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
