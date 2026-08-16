using Amiga;

namespace CopperOS.Shell;

/// <summary>Parsed command-stream redirection targets.</summary>
public struct ShellRedirectionSpec
{
    public ShellRedirectionSpec(
        APTR inputPath,
        uint inputLength,
        APTR outputPath,
        uint outputLength,
        uint outputAppend,
        APTR errorPath,
        uint errorLength,
        uint errorAppend)
    {
        InputPath = inputPath;
        InputLength = inputLength;
        OutputPath = outputPath;
        OutputLength = outputLength;
        OutputAppend = outputAppend;
        ErrorPath = errorPath;
        ErrorLength = errorLength;
        ErrorAppend = errorAppend;
    }

    public APTR InputPath { get; set; }
    public uint InputLength { get; set; }
    public APTR OutputPath { get; set; }
    public uint OutputLength { get; set; }
    public uint OutputAppend { get; set; }
    public APTR ErrorPath { get; set; }
    public uint ErrorLength { get; set; }
    public uint ErrorAppend { get; set; }

    public bool HasInput => InputPath.IsNotNull && InputLength != 0;
    public bool HasOutput => OutputPath.IsNotNull && OutputLength != 0;
    public bool HasError => ErrorPath.IsNotNull && ErrorLength != 0;
    public bool IsEmpty => !HasInput && !HasOutput && !HasError;
}

/// <summary>Caller-owned buffers used while parsing one command line.</summary>
public struct ShellRedirectionWorkspace
{
    public ShellRedirectionWorkspace(
        APTR command,
        uint commandCapacity,
        APTR inputPath,
        uint inputCapacity,
        APTR outputPath,
        uint outputCapacity,
        APTR errorPath,
        uint errorCapacity)
    {
        Command = command;
        CommandCapacity = commandCapacity;
        InputPath = inputPath;
        InputCapacity = inputCapacity;
        OutputPath = outputPath;
        OutputCapacity = outputCapacity;
        ErrorPath = errorPath;
        ErrorCapacity = errorCapacity;
    }

	public APTR Command { get; set; }
	public uint CommandCapacity { get; set; }
	public APTR InputPath { get; set; }
	public uint InputCapacity { get; set; }
	public APTR OutputPath { get; set; }
	public uint OutputCapacity { get; set; }
	public APTR ErrorPath { get; set; }
	public uint ErrorCapacity { get; set; }

    public bool IsEnabled => !Command.IsNull && CommandCapacity >= 2;
}

/// <summary>Temporary stream handles opened for one command.</summary>
public struct ShellRedirectionHandles
{
    public BPTR Input;
    public BPTR Output;
    public BPTR Error;
    public uint Owned;
}

/// <summary>
/// Allocation-free parser for the bounded Shell redirection subset:
/// <c>&lt;</c>, <c>&gt;</c>, <c>&gt;&gt;</c>, <c>2&gt;</c>, and <c>2&gt;&gt;</c>.
/// Operators inside quotes or after a semicolon comment are ordinary text.
/// </summary>
public static class ShellRedirectionParser
{
    public static bool Parse<TPlatform>(
        ref TPlatform platform,
        APTR source,
        uint sourceLength,
        in ShellRedirectionWorkspace workspace,
        out ShellRedirectionSpec spec,
        out uint commandLength)
        where TPlatform : struct, IShellPlatform
    {
        spec = default;
        commandLength = 0;
        if (!ValidSource(ref platform, source, sourceLength) ||
            !ValidWorkspace(ref platform, in workspace) ||
            (sourceLength != 0 &&
             (RangesOverlap(source, sourceLength, workspace.Command,
                     workspace.CommandCapacity) ||
              RangesOverlap(source, sourceLength, workspace.InputPath,
                     workspace.InputCapacity) ||
              RangesOverlap(source, sourceLength, workspace.OutputPath,
                     workspace.OutputCapacity) ||
              RangesOverlap(source, sourceLength, workspace.ErrorPath,
                     workspace.ErrorCapacity))))
            return false;

        var input = APTR.Null;
        var output = APTR.Null;
        var error = APTR.Null;
        uint inputLength = 0;
        uint outputLength = 0;
        uint errorLength = 0;
        uint outputAppend = 0;
        uint errorAppend = 0;
        var quote = false;
        var position = 0u;
        var written = 0u;
        while (position < sourceLength)
        {
            var value = platform.ReadUInt8(source, (int)position);
            if (value == 0)
                return false;
            if (value == ';' && !quote)
            {
                if (!CopyRaw(ref platform, source, sourceLength,
                        ref position, workspace.Command,
                        workspace.CommandCapacity, ref written))
                    return false;
                break;
            }
            if (value == '"')
            {
                quote = !quote;
                if (!CopyByte(ref platform, workspace.Command,
                        workspace.CommandCapacity, ref written, value))
                    return false;
                position++;
                continue;
            }
            if (value == '*')
            {
                if (!CopyByte(ref platform, workspace.Command,
                        workspace.CommandCapacity, ref written, value) ||
                    ++position >= sourceLength ||
                    !CopyByte(ref platform, workspace.Command,
                        workspace.CommandCapacity, ref written,
                        platform.ReadUInt8(source, (int)position)))
                    return false;
                position++;
                continue;
            }

            var kind = RedirectionKind.None;
            uint operatorLength = 0;
            uint append = 0;
            if (!quote && value == '<')
            {
                kind = RedirectionKind.Input;
                operatorLength = 1;
            }
            else if (!quote && value == '>')
            {
                kind = RedirectionKind.Output;
                operatorLength = 1;
                if (position + 1 < sourceLength &&
                    platform.ReadUInt8(source, (int)(position + 1)) == '>')
                {
                    append = 1;
                    operatorLength = 2;
                }
            }
            else if (!quote && value == '2' &&
                (position == 0 || IsWhitespace(platform.ReadUInt8(
                    source, (int)(position - 1)))) &&
                position + 1 < sourceLength &&
                platform.ReadUInt8(source, (int)(position + 1)) == '>')
            {
                kind = RedirectionKind.Error;
                operatorLength = 2;
                if (position + 2 < sourceLength &&
                    platform.ReadUInt8(source, (int)(position + 2)) == '>')
                {
                    append = 1;
                    operatorLength = 3;
                }
            }

            if (kind == RedirectionKind.None)
            {
                if (!CopyByte(ref platform, workspace.Command,
                        workspace.CommandCapacity, ref written, value))
                    return false;
                position++;
                continue;
            }

            if (!PutSeparator(ref platform, workspace.Command,
                    workspace.CommandCapacity, ref written))
                return false;
            position += operatorLength;
            while (position < sourceLength && IsWhitespace(
                    platform.ReadUInt8(source, (int)position)))
                position++;

            APTR path;
            uint pathCapacity;
            switch (kind)
            {
                case RedirectionKind.Input:
                    if (input.IsNotNull) return false;
                    path = workspace.InputPath;
                    pathCapacity = workspace.InputCapacity;
                    break;
                case RedirectionKind.Output:
                    if (output.IsNotNull) return false;
                    path = workspace.OutputPath;
                    pathCapacity = workspace.OutputCapacity;
                    break;
                default:
                    if (error.IsNotNull) return false;
                    path = workspace.ErrorPath;
                    pathCapacity = workspace.ErrorCapacity;
                    break;
            }

            if (!ReadPath(ref platform, source, sourceLength, ref position,
                    path, pathCapacity, out var pathLength))
                return false;
            switch (kind)
            {
                case RedirectionKind.Input:
                    input = path;
                    inputLength = pathLength;
                    break;
                case RedirectionKind.Output:
                    output = path;
                    outputLength = pathLength;
                    outputAppend = append;
                    break;
                default:
                    error = path;
                    errorLength = pathLength;
                    errorAppend = append;
                    break;
            }
        }

        if (quote || !CopyByte(ref platform, workspace.Command,
                workspace.CommandCapacity, ref written, 0))
            return false;
        commandLength = written - 1;
        spec = default;
        spec.InputPath = input;
        spec.InputLength = inputLength;
        spec.OutputPath = output;
        spec.OutputLength = outputLength;
        spec.OutputAppend = outputAppend;
        spec.ErrorPath = error;
        spec.ErrorLength = errorLength;
        spec.ErrorAppend = errorAppend;
        return true;
    }

    private enum RedirectionKind : byte
    {
        None,
        Input,
        Output,
        Error,
    }

    private static bool ReadPath<TPlatform>(
        ref TPlatform platform,
        APTR source,
        uint sourceLength,
        ref uint position,
        APTR destination,
        uint destinationCapacity,
        out uint pathLength)
        where TPlatform : struct, IShellPlatform
    {
        pathLength = 0;
        if (destination.IsNull || destinationCapacity < 2 ||
            destination.Raw > uint.MaxValue - destinationCapacity ||
            !platform.IsMapped(destination, destinationCapacity) ||
            position >= sourceLength ||
            platform.ReadUInt8(source, (int)position) == ';')
            return false;
        var quote = false;
        while (position < sourceLength)
        {
            var value = platform.ReadUInt8(source, (int)position);
            if (!quote && (IsWhitespace(value) || value == ';'))
                break;
            position++;
            if (value == '"')
            {
                quote = !quote;
                continue;
            }
            if (value == '*')
            {
                if (position >= sourceLength)
                    return false;
                value = platform.ReadUInt8(source, (int)position++);
                if (value == 'e' || value == 'E') value = 0x1B;
                else if (value == 'n' || value == 'N') value = (byte)'\n';
                else if (value == 'r' || value == 'R') value = (byte)'\r';
                else if (value == 't' || value == 'T') value = (byte)'\t';
            }
            if (pathLength >= destinationCapacity - 1)
                return false;
            platform.WriteUInt8(destination, (int)pathLength++, value);
        }
        if (quote || pathLength == 0)
            return false;
        platform.WriteUInt8(destination, (int)pathLength, 0);
        return true;
    }

    private static bool CopyRaw<TPlatform>(
        ref TPlatform platform,
        APTR source,
        uint sourceLength,
        ref uint position,
        APTR destination,
        uint destinationCapacity,
        ref uint written)
        where TPlatform : struct, IShellPlatform
    {
        while (position < sourceLength)
        {
            if (!CopyByte(ref platform, destination, destinationCapacity,
                    ref written, platform.ReadUInt8(source, (int)position++)))
                return false;
        }
        return true;
    }

    private static bool PutSeparator<TPlatform>(
        ref TPlatform platform,
        APTR destination,
        uint capacity,
        ref uint written)
        where TPlatform : struct, IShellPlatform
    {
        if (written == 0 || IsWhitespace(platform.ReadUInt8(
                destination, (int)(written - 1))))
            return true;
        return CopyByte(ref platform, destination, capacity,
            ref written, (byte)' ');
    }

    private static bool CopyByte<TPlatform>(
        ref TPlatform platform,
        APTR destination,
        uint capacity,
        ref uint written,
        byte value)
        where TPlatform : struct, IShellPlatform
    {
        if (destination.IsNull || capacity == 0 || written >= capacity)
            return false;
        platform.WriteUInt8(destination, (int)written++, value);
        return true;
    }

    private static bool ValidSource<TPlatform>(
        ref TPlatform platform,
        APTR source,
        uint length)
        where TPlatform : struct, IShellPlatform =>
        length <= ShellTextParser.MaximumSourceLength &&
        (length == 0 || (!source.IsNull &&
            source.Raw <= uint.MaxValue - length &&
            platform.IsMapped(source, length)));

    private static bool ValidWorkspace<TPlatform>(
        ref TPlatform platform,
        in ShellRedirectionWorkspace workspace)
        where TPlatform : struct, IShellPlatform =>
        ValidBuffer(ref platform, workspace.Command,
            workspace.CommandCapacity) &&
        OptionalBuffer(ref platform, workspace.InputPath,
            workspace.InputCapacity) &&
        OptionalBuffer(ref platform, workspace.OutputPath,
            workspace.OutputCapacity) &&
        OptionalBuffer(ref platform, workspace.ErrorPath,
            workspace.ErrorCapacity);

    private static bool OptionalBuffer<TPlatform>(
        ref TPlatform platform,
        APTR address,
        uint capacity)
        where TPlatform : struct, IShellPlatform =>
        (address.IsNull && capacity == 0) ||
        ValidBuffer(ref platform, address, capacity);

    private static bool ValidBuffer<TPlatform>(
        ref TPlatform platform,
        APTR address,
        uint capacity)
        where TPlatform : struct, IShellPlatform =>
        !address.IsNull && capacity >= 2 &&
        address.Raw <= uint.MaxValue - capacity &&
        platform.IsMapped(address, capacity);

    private static bool IsWhitespace(byte value) =>
        value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';

    private static bool RangesOverlap(
        APTR first,
        uint firstLength,
        APTR second,
        uint secondLength)
    {
        if (firstLength == 0 || secondLength == 0 ||
            first.IsNull || second.IsNull)
            return false;
        if (first.Raw > uint.MaxValue - firstLength ||
            second.Raw > uint.MaxValue - secondLength)
            return true;
        return first.Raw < second.Raw + secondLength &&
            second.Raw < first.Raw + firstLength;
    }
}

/// <summary>Applies and rolls back command-scoped redirection handles.</summary>
public static class ShellRedirectionTransaction
{
    public static bool TryOpen<TPlatform>(
        ref TPlatform platform,
        in ShellScriptFrameState frame,
        in ShellRedirectionSpec spec,
        out ShellRedirectionHandles handles)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        handles = new ShellRedirectionHandles
        {
            Input = frame.Input,
            Output = frame.Output,
            Error = frame.Error,
        };
        if (spec.HasInput && (!platform.TryOpenScriptInput(
                frame.Cli, spec.InputPath, spec.InputLength,
                out handles.Input) || handles.Input.IsNull))
            return false;
        if (spec.HasInput)
            handles.Owned |= 1;
        if (spec.HasOutput && (!platform.TryOpenScriptOutput(
                frame.Cli, spec.OutputPath, spec.OutputLength,
                spec.OutputAppend, out handles.Output) ||
                handles.Output.IsNull))
        {
            Close(ref platform, in frame, ref handles);
            return false;
        }
        if (spec.HasOutput)
            handles.Owned |= 2;
        if (spec.HasError && (!platform.TryOpenScriptOutput(
                frame.Cli, spec.ErrorPath, spec.ErrorLength,
                spec.ErrorAppend, out handles.Error) || handles.Error.IsNull))
        {
            Close(ref platform, in frame, ref handles);
            return false;
        }
        if (spec.HasError)
            handles.Owned |= 4;
        return true;
    }

    public static bool Close<TPlatform>(
        ref TPlatform platform,
        in ShellScriptFrameState frame,
        ref ShellRedirectionHandles handles)
        where TPlatform : struct, IShellPlatform, IShellScriptPlatform
    {
        var success = true;
        if ((handles.Owned & 4) != 0)
            success &= platform.TryCloseScriptRedirection(
                frame.Cli, handles.Error);
        if ((handles.Owned & 2) != 0)
            success &= platform.TryCloseScriptRedirection(
                frame.Cli, handles.Output);
        if ((handles.Owned & 1) != 0)
            success &= platform.TryCloseScriptRedirection(
                frame.Cli, handles.Input);
        handles.Owned = 0;
        return success;
    }
}
