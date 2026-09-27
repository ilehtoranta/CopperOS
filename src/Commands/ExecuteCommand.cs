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
            stableFileCapacity < ExecuteReadArgsResultRecord.Size)
            return (int)ShellCommandResult.Fail;
        WriteFileTemplate(ref platform, fileTokenBuffer);

        // ReadItem establishes the exact raw boundary of FILE before ReadArgs
        // decodes it.  The suffix remains untouched for the script frame; it
        // is not a second Execute option list.
        if (!platform.TryReadScriptFilePrefix(invocation.ArgumentText,
                invocation.ArgumentLength, out var filePrefixLength) ||
            filePrefixLength == 0 ||
            filePrefixLength > invocation.ArgumentLength)
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
            filePrefixLength, fileTokenBuffer, templateLength,
            stableFileBuffer, ExecuteReadArgsResultRecord.Size,
            out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        if (!ExecuteReadArgsResultRecordCodec.TryRead(ref platform,
                stableFileBuffer, out var parsed))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        var file = parsed.File;
        if (!CStringCodec.TryReadLength(ref platform, file,
            EchoCommand.MaximumArgumentLength, out var fileLength))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Fail;
        }

        var scriptArgumentLength = invocation.ArgumentLength - filePrefixLength;
        var scriptArguments = scriptArgumentLength == 0
            ? APTR.Null
            : APTR.FromPointer(invocation.ArgumentText.Raw + filePrefixLength);
        var status = platform.TryExecuteScript(invocation.Cli, file, fileLength,
            scriptArguments, scriptArgumentLength, out var commandResult);
        platform.FreeArgs(rdArgs);
        return status switch
        {
            ShellScriptExecutionStatus.Completed => commandResult,
            ShellScriptExecutionStatus.Pending =>
                (int)ShellCommandResult.Pending,
            _ => (int)ShellCommandResult.Fail,
        };
    }

    // This template is a NUL-terminated guest byte string. Keep its only
    // positional state in a named sequential writer record.
    [System.Runtime.InteropServices.StructLayout(
        System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 2)]
    private struct ExecuteTemplateWriter
    {
        internal APTR Buffer;
        internal uint Position;
    }

    private static void WriteFileTemplate<TPlatform>(ref TPlatform platform,
        APTR template) where TPlatform : struct, IShellPlatform
    {
        var writer = new ExecuteTemplateWriter { Buffer = template };
        AppendTemplateByte(ref platform, ref writer, (byte)'F');
        AppendTemplateByte(ref platform, ref writer, (byte)'I');
        AppendTemplateByte(ref platform, ref writer, (byte)'L');
        AppendTemplateByte(ref platform, ref writer, (byte)'E');
        AppendTemplateByte(ref platform, ref writer, (byte)'/');
        AppendTemplateByte(ref platform, ref writer, (byte)'A');
        AppendTemplateByte(ref platform, ref writer, 0);
    }

    private static void AppendTemplateByte<TPlatform>(ref TPlatform platform,
        ref ExecuteTemplateWriter writer, byte value)
        where TPlatform : struct, IShellPlatform
    {
        // The caller has validated the complete template and backing span.
        platform.WriteUInt8(writer.Buffer, (int)writer.Position, value);
        writer.Position++;
    }

}
