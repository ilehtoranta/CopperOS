using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Path</c> command.
///
/// Parsed path entries are packed as NUL-terminated guest strings and handed
/// to DOS. The command path itself, assign/requester policy, and path-entry
/// lifetime remain outside the Shell semantic core.
/// </summary>
public static class PathCommand
{
    public const uint MaximumPathEntries = 64;
    public const uint MaximumPathBytes = 65_535;

    public const uint OperationReset = 0;
    public const uint OperationAdd = 1;
    public const uint OperationRemove = 2;

    private const uint OptionShow = 1;
    private const uint OptionAdd = 2;
    private const uint OptionReset = 4;
    private const uint OptionRemove = 8;
    private const uint OptionQuiet = 16;

    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR pathBuffer,
        uint pathCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || tokenBuffer.IsNull || pathBuffer.IsNull ||
            tokenCapacity == 0 || pathCapacity == 0 ||
            pathCapacity > MaximumPathBytes ||
            tokenBuffer.Raw > uint.MaxValue - tokenCapacity ||
            pathBuffer.Raw > uint.MaxValue - pathCapacity ||
            !platform.IsMapped(tokenBuffer, tokenCapacity) ||
            !platform.IsMapped(pathBuffer, pathCapacity) ||
            RangesOverlap(tokenBuffer, tokenCapacity, pathBuffer, pathCapacity))
            return (int)ShellCommandResult.Fail;

        uint optionMask = 0;
        uint pathBytes = 0;
        uint pathCount = 0;
        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Path, 24,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;

        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 24, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var pathList = APTR.FromPointer(platform.ReadUInt32(resultArray));
        if (pathList.IsNotNull)
        {
            for (var index = 0u; index < MaximumPathEntries; index++)
            {
                if (pathList.Raw > uint.MaxValue - (index + 1) * 4u ||
                    !platform.IsMapped(pathList, (index + 1) * 4u))
                {
                    platform.FreeArgs(rdArgs);
                    return (int)ShellCommandResult.Error;
                }

                var item = APTR.FromPointer(platform.ReadUInt32(
                    pathList, (int)(index * 4u)));
                if (item.IsNull)
                    break;
                if (pathCount >= MaximumPathEntries ||
                    pathBytes >= pathCapacity)
                {
                    platform.FreeArgs(rdArgs);
                    return (int)ShellCommandResult.Error;
                }

                var destination = new APTR(pathBuffer.Raw + pathBytes);
                if (!ReadArgsCommandSupport.CopyCString(ref platform, item,
                        destination, pathCapacity - pathBytes,
                        out var itemLength) ||
                    itemLength >= pathCapacity - pathBytes)
                {
                    platform.FreeArgs(rdArgs);
                    return (int)ShellCommandResult.Error;
                }
                pathBytes += itemLength + 1;
                pathCount++;
            }
        }

        if (platform.ReadUInt32(resultArray, 4) != 0)
            optionMask |= OptionAdd;
        if (platform.ReadUInt32(resultArray, 8) != 0)
            optionMask |= OptionShow;
        if (platform.ReadUInt32(resultArray, 12) != 0)
            optionMask |= OptionReset;
        if (platform.ReadUInt32(resultArray, 16) != 0)
            optionMask |= OptionRemove;
        if (platform.ReadUInt32(resultArray, 20) != 0)
            optionMask |= OptionQuiet;
        platform.FreeArgs(rdArgs);

        uint operationBits = optionMask &
            (OptionAdd | OptionReset | OptionRemove);
        if (operationBits != 0 &&
            operationBits != OptionAdd &&
            operationBits != OptionReset &&
            operationBits != OptionRemove)
            return (int)ShellCommandResult.Error;

        if ((optionMask & OptionShow) != 0)
        {
            if (pathCount != 0 || operationBits != 0 ||
                invocation.Output.IsNull)
                return (int)ShellCommandResult.Error;
            return platform.TryWriteCommandPath(
                    invocation.Output,
                    invocation.Cli,
                    (optionMask & OptionQuiet) != 0 ? 1u : 0u)
                ? (int)ShellCommandResult.Ok
                : (int)ShellCommandResult.Fail;
        }

        if (operationBits == 0)
        {
            if (pathCount == 0)
            {
                if (invocation.Output.IsNull)
                    return (int)ShellCommandResult.Fail;
                return platform.TryWriteCommandPath(
                        invocation.Output,
                        invocation.Cli,
                        (optionMask & OptionQuiet) != 0 ? 1u : 0u)
                    ? (int)ShellCommandResult.Ok
                    : (int)ShellCommandResult.Fail;
            }
            operationBits = OptionReset;
        }

        if ((operationBits == OptionAdd || operationBits == OptionRemove) &&
            pathCount == 0)
            return (int)ShellCommandResult.Error;

        uint operation = operationBits == OptionAdd
            ? OperationAdd
            : operationBits == OptionRemove
                ? OperationRemove
                : OperationReset;
        return platform.TryUpdateCommandPath(
                invocation.Cli,
                pathBuffer,
                pathBytes,
                pathCount,
                operation,
                (optionMask & OptionQuiet) != 0 ? 1u : 0u)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }

    private static bool RangesOverlap(
        APTR first,
        uint firstLength,
        APTR second,
        uint secondLength)
    {
        uint firstEnd = first.Raw + firstLength;
        uint secondEnd = second.Raw + secondLength;
        return first.Raw < secondEnd && second.Raw < firstEnd;
    }
}
