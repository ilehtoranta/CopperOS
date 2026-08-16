using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Lab</c> command.
/// </summary>
public static class LabCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR stableLabelBuffer,
        uint stableLabelCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || tokenBuffer.IsNull ||
            stableLabelBuffer.IsNull || tokenCapacity == 0 ||
            stableLabelCapacity == 0 ||
            stableLabelBuffer.Raw > uint.MaxValue - stableLabelCapacity ||
            !platform.IsMapped(stableLabelBuffer, stableLabelCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Lab, 4,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 4, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var label = APTR.FromPointer(platform.ReadUInt32(resultArray));
        if (!ReadArgsCommandSupport.CopyCString(ref platform, label,
                stableLabelBuffer, stableLabelCapacity, out var labelLength) ||
            labelLength == 0)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        platform.FreeArgs(rdArgs);

        return platform.TryDefineScriptLabel(
                invocation.Cli,
                stableLabelBuffer,
                labelLength)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }
}
