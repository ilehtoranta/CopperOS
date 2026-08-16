using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Set NAME STRING/F</c> command slice.
///
/// The no-argument form asks the CLI owner for its canonical local-variable
/// listing; the Shell does not retain or format a variable map.
/// </summary>
public static class SetCommand
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
                nameCapacity, ReadArgsCommandTemplate.SetOptional, 8,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, nameBuffer, templateLength,
                resultArray, 8, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var name = APTR.FromPointer(platform.ReadUInt32(resultArray));
        var value = APTR.FromPointer(platform.ReadUInt32(resultArray, 4));
        if (name.IsNull)
        {
            if (value.IsNotNull)
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }

            platform.FreeArgs(rdArgs);
            return invocation.Output.IsNull ||
                !platform.TryWriteLocalVariables(invocation.Output,
                    invocation.Cli)
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

        return platform.TrySetLocalVariable(
                invocation.Cli,
                nameBuffer,
                nameLength,
                valueBuffer,
                valueLength)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
