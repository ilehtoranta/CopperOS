using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Skip</c> command.
/// </summary>
public static class SkipCommand
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
            tokenBuffer.Raw > uint.MaxValue - tokenCapacity ||
            stableLabelBuffer.Raw > uint.MaxValue - stableLabelCapacity ||
            !platform.IsMapped(tokenBuffer, tokenCapacity) ||
            !platform.IsMapped(stableLabelBuffer, stableLabelCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Skip, 8,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 8, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var label = APTR.FromPointer(platform.ReadUInt32(resultArray));
        var back = platform.ReadUInt32(resultArray, 4);
        if (label.IsNull)
        {
            platform.FreeArgs(rdArgs);
            return RequestSkip(ref platform, invocation.Cli,
                APTR.Null, 0, back);
        }

        if (!ReadArgsCommandSupport.CopyCString(ref platform, label,
                stableLabelBuffer, stableLabelCapacity, out var labelLength) ||
            labelLength == 0)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        platform.FreeArgs(rdArgs);

        return RequestSkip(ref platform, invocation.Cli,
            stableLabelBuffer, labelLength, back);
    }

    private static int RequestSkip<TPlatform>(
        ref TPlatform platform,
        APTR cli,
        APTR label,
        uint labelLength,
        uint back)
        where TPlatform : struct, IShellPlatform =>
        platform.TrySkipToLabel(cli, label, labelLength, back)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
}
