using Amiga;
using CopperOS.Shell;

namespace CopperOS.Commands;

/// <summary>
/// External MorphOS <c>Execute</c> command.
///
/// This wrapper delegates FILE/A parsing and allocation ownership to the
/// DOS-owned ReadArgs implementation. The active Shell engine owns script
/// protection, argument substitution, nested frames, failure limits, and line
/// execution.
/// </summary>
public static class ExecuteCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR fileTokenBuffer,
        uint fileTokenCapacity,
        APTR stableFileBuffer,
        uint stableFileCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || fileTokenBuffer.IsNull ||
            stableFileBuffer.IsNull || fileTokenCapacity == 0 ||
            stableFileCapacity == 0 ||
            fileTokenBuffer.Raw > uint.MaxValue - fileTokenCapacity ||
            stableFileBuffer.Raw > uint.MaxValue - stableFileCapacity ||
            !platform.IsMapped(fileTokenBuffer, fileTokenCapacity) ||
            !platform.IsMapped(stableFileBuffer, stableFileCapacity))
            return (int)ShellCommandResult.Fail;

        // Keep the MorphOS template in caller-owned guest storage so the
        // command never embeds a second parser or a host string dependency.
        const uint templateLength = 6;
        if (fileTokenCapacity <= templateLength ||
            !platform.IsMapped(fileTokenBuffer, templateLength + 1) ||
            stableFileCapacity < 4)
            return (int)ShellCommandResult.Fail;
        WriteFileTemplate(ref platform, fileTokenBuffer);

        if (!platform.TryReadArgs(invocation.ArgumentText,
            invocation.ArgumentLength, fileTokenBuffer, templateLength,
            stableFileBuffer, 4, out var rdArgs))
            return (int)ShellCommandResult.Error;

        var file = APTR.FromPointer(platform.ReadUInt32(stableFileBuffer));
        if (!CStringCodec.TryReadLength(ref platform, file,
            EchoCommand.MaximumArgumentLength, out var fileLength))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Fail;
        }

        var status = platform.TryExecuteScript(invocation.Cli, file, fileLength,
            out var commandResult);
        platform.FreeArgs(rdArgs);
        return status switch
        {
            ShellScriptExecutionStatus.Completed => commandResult,
            ShellScriptExecutionStatus.Pending =>
                (int)ShellCommandResult.Pending,
            _ => (int)ShellCommandResult.Fail,
        };
    }

    private static void WriteFileTemplate<TPlatform>(ref TPlatform platform,
        APTR template) where TPlatform : struct, IShellPlatform
    {
        platform.WriteUInt8(template, 0, (byte)'F');
        platform.WriteUInt8(template, 1, (byte)'I');
        platform.WriteUInt8(template, 2, (byte)'L');
        platform.WriteUInt8(template, 3, (byte)'E');
        platform.WriteUInt8(template, 4, (byte)'/');
        platform.WriteUInt8(template, 5, (byte)'A');
        platform.WriteUInt8(template, 6, 0);
    }

}
