using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned implementation of MorphOS <c>Echo</c>.
///
/// The command delegates template parsing and temporary-result ownership to
/// the DOS ReadArgs owner.  It consumes only the fixed-width result slots and
/// copies them into caller-owned output buffers before releasing RDArgs.
/// </summary>
public static class EchoCommand
{
    /// <summary>
    /// Maximum argument span accepted by this bounded semantic core.
    /// </summary>
    public const uint MaximumArgumentLength = 65_535;

    /// <summary>
    /// Preserves the original canonical form for callers that already have a
    /// decoded message buffer.  The default line feed is always emitted.
    /// </summary>
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation)
        where TPlatform : struct, IShellPlatform
    {
        EchoArguments arguments = new()
        {
            Message = invocation.ArgumentText,
            MessageLength = invocation.ArgumentLength,
        };
        return ExecuteParsed(ref platform, in invocation, in arguments);
    }

    /// <summary>
    /// Parses and executes Echo using explicit caller-owned guest buffers.
    /// </summary>
    public static int ParseAndExecute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR messageBuffer,
        uint messageCapacity,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR toBuffer,
        uint toCapacity)
        where TPlatform : struct, IShellPlatform
    {
        EchoArguments arguments = new();
        int parseResult = Parse(
            ref platform,
            invocation.ArgumentText,
            invocation.ArgumentLength,
            messageBuffer,
            messageCapacity,
            tokenBuffer,
            tokenCapacity,
            toBuffer,
            toCapacity,
            ref arguments);
        if (parseResult != (int)ShellCommandResult.Ok)
            return parseResult;

        return ExecuteParsed(ref platform, in invocation, in arguments);
    }

    /// <summary>
    /// Parses the MorphOS Echo template into guest-resident fixed-width state.
    /// </summary>
    public static int Parse<TPlatform>(
        ref TPlatform platform,
        APTR source,
        uint sourceLength,
        APTR messageBuffer,
        uint messageCapacity,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR toBuffer,
        uint toCapacity,
        ref EchoArguments arguments)
        where TPlatform : struct, IShellPlatform
    {
        if (sourceLength > MaximumArgumentLength || messageCapacity < 20 ||
            tokenCapacity < 38 || toCapacity == 0 || messageBuffer.IsNull ||
            tokenBuffer.IsNull || toBuffer.IsNull ||
            messageBuffer.Raw > uint.MaxValue - messageCapacity ||
            tokenBuffer.Raw > uint.MaxValue - tokenCapacity ||
            toBuffer.Raw > uint.MaxValue - toCapacity ||
            !platform.IsMapped(messageBuffer, messageCapacity) ||
            !platform.IsMapped(tokenBuffer, tokenCapacity) ||
            !platform.IsMapped(toBuffer, toCapacity) ||
            (sourceLength != 0 && (source.IsNull ||
                source.Raw > uint.MaxValue - sourceLength ||
                !platform.IsMapped(source, sourceLength))))
            return (int)ShellCommandResult.Fail;

        WriteReadArgsTemplate(ref platform, tokenBuffer);
        if (!platform.TryReadArgs(source, sourceLength, tokenBuffer, 37,
            messageBuffer, 20, out var rdArgs))
            return (int)ShellCommandResult.Error;

        var messageList = APTR.FromPointer(platform.ReadUInt32(messageBuffer));
        var noLine = platform.ReadUInt32(messageBuffer, 4);
        var firstValue = APTR.FromPointer(platform.ReadUInt32(messageBuffer, 8));
        var lengthValue = APTR.FromPointer(platform.ReadUInt32(messageBuffer, 12));
        var toValue = APTR.FromPointer(platform.ReadUInt32(messageBuffer, 16));
        var messageLength = CopyReadArgsMultiple(ref platform, messageList,
            messageBuffer, messageCapacity);
        if (messageLength == uint.MaxValue)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        uint toLength = 0;
        if (toValue.IsNotNull)
        {
            if (!CStringCodec.TryReadLength(ref platform, toValue,
                    MaximumArgumentLength, out toLength) ||
                toLength >= toCapacity)
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
            platform.Copy(toValue, toBuffer, toLength);
            platform.WriteUInt8(toBuffer, (int)toLength, 0);
        }

        arguments = new EchoArguments
        {
            Message = messageBuffer,
            MessageLength = messageLength,
            NoLine = noLine,
            ToPath = toBuffer,
            ToPathLength = toLength,
        };
        if (firstValue.IsNotNull)
        {
            arguments.HasFirst = 1;
            arguments.First = platform.ReadUInt32(firstValue);
        }
        if (lengthValue.IsNotNull)
        {
            arguments.HasLength = 1;
            arguments.Length = platform.ReadUInt32(lengthValue);
        }
        platform.FreeArgs(rdArgs);
        return (int)ShellCommandResult.Ok;
    }

    private static void WriteReadArgsTemplate<TPlatform>(ref TPlatform platform,
        APTR template) where TPlatform : struct, IShellPlatform
    {
        platform.WriteUInt8(template, 0, (byte)'M');
        platform.WriteUInt8(template, 1, (byte)'E');
        platform.WriteUInt8(template, 2, (byte)'S');
        platform.WriteUInt8(template, 3, (byte)'S');
        platform.WriteUInt8(template, 4, (byte)'A');
        platform.WriteUInt8(template, 5, (byte)'G');
        platform.WriteUInt8(template, 6, (byte)'E');
        platform.WriteUInt8(template, 7, (byte)'/');
        platform.WriteUInt8(template, 8, (byte)'M');
        platform.WriteUInt8(template, 9, (byte)',');
        platform.WriteUInt8(template, 10, (byte)'N');
        platform.WriteUInt8(template, 11, (byte)'O');
        platform.WriteUInt8(template, 12, (byte)'L');
        platform.WriteUInt8(template, 13, (byte)'I');
        platform.WriteUInt8(template, 14, (byte)'N');
        platform.WriteUInt8(template, 15, (byte)'E');
        platform.WriteUInt8(template, 16, (byte)'/');
        platform.WriteUInt8(template, 17, (byte)'S');
        platform.WriteUInt8(template, 18, (byte)',');
        platform.WriteUInt8(template, 19, (byte)'F');
        platform.WriteUInt8(template, 20, (byte)'I');
        platform.WriteUInt8(template, 21, (byte)'R');
        platform.WriteUInt8(template, 22, (byte)'S');
        platform.WriteUInt8(template, 23, (byte)'T');
        platform.WriteUInt8(template, 24, (byte)'/');
        platform.WriteUInt8(template, 25, (byte)'N');
        platform.WriteUInt8(template, 26, (byte)',');
        platform.WriteUInt8(template, 27, (byte)'L');
        platform.WriteUInt8(template, 28, (byte)'E');
        platform.WriteUInt8(template, 29, (byte)'N');
        platform.WriteUInt8(template, 30, (byte)'/');
        platform.WriteUInt8(template, 31, (byte)'N');
        platform.WriteUInt8(template, 32, (byte)',');
        platform.WriteUInt8(template, 33, (byte)'T');
        platform.WriteUInt8(template, 34, (byte)'O');
        platform.WriteUInt8(template, 35, (byte)'/');
        platform.WriteUInt8(template, 36, (byte)'K');
        platform.WriteUInt8(template, 37, 0);
    }

    private static uint CopyReadArgsMultiple<TPlatform>(ref TPlatform platform,
        APTR list, APTR destination, uint capacity)
        where TPlatform : struct, IShellPlatform
    {
        if (list.IsNull)
        {
            if (capacity == 0 || !platform.IsMapped(destination, capacity))
                return uint.MaxValue;
            platform.WriteUInt8(destination, 0, 0);
            return 0;
        }
        uint output = 0;
        for (var index = 0; index < 256; index++)
        {
            if (list.Raw > uint.MaxValue - (uint)(index * 4) ||
                !platform.IsMapped(list, (uint)((index + 1) * 4)))
                return uint.MaxValue;
            var item = APTR.FromPointer(platform.ReadUInt32(list, index * 4));
            if (item.IsNull) break;
            if (!CStringCodec.TryReadLength(ref platform, item,
                    MaximumArgumentLength, out var length) ||
                output > capacity - 1 ||
                length > capacity - 1 - output)
                return uint.MaxValue;
            if (output != 0)
                platform.WriteUInt8(destination, (int)output++, (byte)' ');
            platform.Copy(item, APTR.FromPointer(destination.Raw + output), length);
            output += length;
        }
        if (output >= capacity) return uint.MaxValue;
        platform.WriteUInt8(destination, (int)output, 0);
        return output;
    }

    /// <summary>
    /// Executes already parsed Echo state.
    /// </summary>
    public static int ExecuteParsed<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        in EchoArguments arguments)
        where TPlatform : struct, IShellPlatform
    {
        if (arguments.MessageLength > MaximumArgumentLength)
            return (int)ShellCommandResult.Error;

        if ((arguments.MessageLength != 0 &&
             (arguments.Message.IsNull ||
              !platform.IsMapped(arguments.Message, arguments.MessageLength))) ||
            (arguments.ToPathLength != 0 &&
             (arguments.ToPath.IsNull ||
              !platform.IsMapped(arguments.ToPath, arguments.ToPathLength))))
            return (int)ShellCommandResult.Fail;

        BPTR output = invocation.Output;
        uint closeOutput = 0;
        if (arguments.ToPathLength != 0)
        {
            output = platform.OpenOutput(arguments.ToPath, arguments.ToPathLength);
            if (output.IsNull)
                return (int)ShellCommandResult.Fail;
            closeOutput = 1;
        }
        else if (output.IsNull)
        {
            return (int)ShellCommandResult.Fail;
        }

        uint start = 0;
        if (arguments.HasFirst != 0)
        {
            if (arguments.First == 0)
                start = 0;
            else if (arguments.First > arguments.MessageLength)
                start = arguments.MessageLength;
            else
                start = arguments.First - 1;
        }
        else if (arguments.HasLength != 0 &&
                 arguments.Length < arguments.MessageLength)
        {
            start = arguments.MessageLength - arguments.Length;
        }

        uint available = arguments.MessageLength - start;
        uint outputLength = arguments.HasLength != 0 &&
            arguments.Length < available ? arguments.Length : available;

        int result = (int)ShellCommandResult.Ok;
        if (outputLength != 0)
        {
            if (start > uint.MaxValue - outputLength ||
                arguments.Message.Raw > uint.MaxValue - (start + outputLength))
            {
                if (closeOutput != 0) platform.CloseOutput(output);
                return (int)ShellCommandResult.Fail;
            }
            int written = platform.Write(
                output,
                APTR.FromPointer(arguments.Message.Raw + start),
                outputLength);
            if (written < 0 || (uint)written != outputLength)
                result = (int)ShellCommandResult.Error;
        }

        if (result == (int)ShellCommandResult.Ok && arguments.NoLine == 0 &&
            platform.WriteByte(output, (byte)'\n') < 0)
            result = (int)ShellCommandResult.Error;

        if (closeOutput != 0 && !platform.CloseOutput(output))
            result = (int)ShellCommandResult.Error;
        return result;
    }

}
