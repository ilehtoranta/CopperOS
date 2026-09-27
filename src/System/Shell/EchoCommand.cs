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
        if (sourceLength > MaximumArgumentLength ||
            messageCapacity < EchoReadArgsResultRecord.Size ||
            tokenCapacity < 42 || toCapacity == 0 || messageBuffer.IsNull ||
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

        if (!ReadArgsCommandSupport.TryWriteTemplate(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Echo,
                out var templateLength) ||
            !platform.TryReadArgs(source, sourceLength, tokenBuffer,
                templateLength, messageBuffer, EchoReadArgsResultRecord.Size,
                out var rdArgs))
            return (int)ShellCommandResult.Error;

        if (!EchoReadArgsResultRecordCodec.TryRead(ref platform, messageBuffer,
            out var readArgsResult))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }
        var messageLength = CopyReadArgsMultiple(ref platform,
            readArgsResult.MessageList, messageBuffer, messageCapacity);
        if (messageLength == uint.MaxValue)
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        uint toLength = 0;
        if (readArgsResult.To.IsNotNull)
        {
            if (!CStringCodec.TryReadLength(ref platform, readArgsResult.To,
                    MaximumArgumentLength, out toLength) ||
                toLength >= toCapacity)
            {
                platform.FreeArgs(rdArgs);
                return (int)ShellCommandResult.Error;
            }
            platform.Copy(readArgsResult.To, toBuffer, toLength);
            platform.WriteUInt8(toBuffer, (int)toLength, 0);
        }

        ReadArgsLongValueRecord first = default;
        ReadArgsLongValueRecord length = default;
        if ((readArgsResult.First.IsNotNull &&
             !ReadArgsLongValueRecordCodec.TryRead(ref platform,
                 readArgsResult.First, out first)) ||
            (readArgsResult.Length.IsNotNull &&
             !ReadArgsLongValueRecordCodec.TryRead(ref platform,
                 readArgsResult.Length, out length)))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        arguments = new EchoArguments
        {
            Message = messageBuffer,
            MessageLength = messageLength,
            NoLine = readArgsResult.NoLine,
            HasFirst = readArgsResult.First.IsNotNull ? 1u : 0u,
            // Echo's bounded substring core retains the existing raw 32-bit
            // arithmetic; signed edge-case behavior remains a MorphOS trace
            // question, not an assumption made by this memory-safety check.
            First = unchecked((uint)first.Value),
            HasLength = readArgsResult.Length.IsNotNull ? 1u : 0u,
            Length = unchecked((uint)length.Value),
            ToPath = toBuffer,
            ToPathLength = toLength,
        };
        platform.FreeArgs(rdArgs);
        return (int)ShellCommandResult.Ok;
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
        var cursor = default(EchoReadArgsStringVectorCursor);
        cursor.Base = list;
        for (var index = 0u; index <
            EchoReadArgsStringVectorCursor.MaximumEntries; index++)
        {
            if (!EchoReadArgsStringVectorCursorCodec.TryReadCurrent(
                ref platform, cursor, out var item, out var hasItem))
                return uint.MaxValue;
            if (!hasItem) break;
            if (!CStringCodec.TryReadLength(ref platform, item,
                    MaximumArgumentLength, out var length) ||
                output > capacity - 1 ||
                length > capacity - 1 - output)
                return uint.MaxValue;
            if (output != 0)
                platform.WriteUInt8(destination, (int)output++, (byte)' ');
            platform.Copy(item, APTR.FromPointer(destination.Raw + output), length);
            output += length;
            if (index + 1 == EchoReadArgsStringVectorCursor.MaximumEntries ||
                !EchoReadArgsStringVectorCursorCodec.TryAdvance(ref cursor))
                return uint.MaxValue;
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
