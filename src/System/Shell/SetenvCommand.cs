using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Setenv NAME [SAVE] STRING/F</c> command slice.
///
/// The no-argument form asks the DOS owner for its canonical global-variable
/// listing; the Shell does not retain or format an environment map.
/// </summary>
public static class SetenvCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR nameBuffer,
        uint nameCapacity,
        APTR optionBuffer,
        uint optionCapacity,
        APTR valueBuffer,
        uint valueCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (nameBuffer.IsNull || optionBuffer.IsNull || valueBuffer.IsNull ||
            nameCapacity == 0 || optionCapacity == 0 || valueCapacity == 0 ||
            optionBuffer.Raw > uint.MaxValue - optionCapacity ||
            valueBuffer.Raw > uint.MaxValue - valueCapacity ||
            !platform.IsMapped(optionBuffer, optionCapacity) ||
            !platform.IsMapped(valueBuffer, valueCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, nameBuffer,
                nameCapacity, ReadArgsCommandTemplate.SetenvOptional, 12,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, nameBuffer, templateLength,
                resultArray, 12, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var name = APTR.FromPointer(platform.ReadUInt32(resultArray));
        var value = APTR.FromPointer(platform.ReadUInt32(resultArray, 8));
        var save = platform.ReadUInt32(resultArray, 4);
        if (name.IsNull)
        {
            if (value.IsNotNull || save != 0)
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

        return platform.TrySetGlobalVariable(
                nameBuffer,
                nameLength,
                valueBuffer,
                valueLength,
                save)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
