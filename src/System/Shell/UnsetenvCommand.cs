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
                nameCapacity, ReadArgsCommandTemplate.UnsetenvOptional,
                UnsetenvReadArgsResultRecord.Size,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, nameBuffer, templateLength,
                resultArray, UnsetenvReadArgsResultRecord.Size,
                out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        if (!UnsetenvReadArgsResultRecordCodec.TryRead(ref platform,
                resultArray, out var parsed))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        var name = parsed.Name;
        var save = parsed.Save;
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

        if (!CStringCodec.TryReadLength(ref platform, name, 65536,
                out var nameLength))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Fail;
        }

        var removed = platform.TryRemoveGlobalVariable(name,
            nameLength, save);
        platform.FreeArgs(rdArgs);
        return removed
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
