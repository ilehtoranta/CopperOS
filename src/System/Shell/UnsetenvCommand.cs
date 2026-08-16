using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Unsetenv NAME [SAVE]</c> command.  With no name it
/// delegates the canonical global-variable listing to the DOS owner.
/// </summary>
public static class UnsetenvCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR nameBuffer,
        uint nameCapacity,
        APTR optionBuffer,
        uint optionCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (nameBuffer.IsNull || optionBuffer.IsNull ||
            nameCapacity == 0 || optionCapacity == 0 ||
            optionBuffer.Raw > uint.MaxValue - optionCapacity ||
            !platform.IsMapped(optionBuffer, optionCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, nameBuffer,
                nameCapacity, ReadArgsCommandTemplate.UnsetenvOptional, 8,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, nameBuffer, templateLength,
                resultArray, 8, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;
        var name = APTR.FromPointer(platform.ReadUInt32(resultArray));
        var save = platform.ReadUInt32(resultArray, 4);
        if (name.IsNull)
        {
            if (save != 0)
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }

            platform.FreeArgs(rdArgs);
            return invocation.Output.IsNull ||
                !platform.TryWriteGlobalVariables(invocation.Output)
                ? (int)ShellCommandResult.Fail
                : (int)ShellCommandResult.Ok;
        }

        var nameLength = ReadCStringLength(ref platform, name, 65536);
        if (nameLength < 0)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Fail;
        }

        var removed = platform.TryRemoveGlobalVariable(name,
            (uint)nameLength, save);
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
