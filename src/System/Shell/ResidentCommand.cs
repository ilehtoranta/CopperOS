using Amiga;

namespace CopperOS.Shell;

/// <summary>
/// Shell-owned MorphOS <c>Resident</c> command.
///
/// This wrapper owns only ReadArgs result ownership and bounded copies. The
/// resident list, HUNK loading, purity qualification, and mutation policy
/// remain in the DOS/Shell platform owner.
/// </summary>
public static class ResidentCommand
{
    public static int Execute<TPlatform>(
        ref TPlatform platform,
        in CommandInvocation invocation,
        APTR tokenBuffer,
        uint tokenCapacity,
        APTR nameBuffer,
        uint nameCapacity,
        APTR fileBuffer,
        uint fileCapacity,
        APTR aliasBuffer,
        uint aliasCapacity)
        where TPlatform : struct, IShellPlatform
    {
        if (invocation.Cli.IsNull || invocation.Output.IsNull ||
            tokenBuffer.IsNull || nameBuffer.IsNull || fileBuffer.IsNull ||
            aliasBuffer.IsNull || tokenCapacity == 0 || nameCapacity == 0 ||
            fileCapacity == 0 || aliasCapacity == 0 ||
            RangesOverlap(tokenBuffer, tokenCapacity, nameBuffer, nameCapacity) ||
            RangesOverlap(tokenBuffer, tokenCapacity, fileBuffer, fileCapacity) ||
            RangesOverlap(tokenBuffer, tokenCapacity, aliasBuffer, aliasCapacity) ||
            RangesOverlap(nameBuffer, nameCapacity, fileBuffer, fileCapacity) ||
            RangesOverlap(nameBuffer, nameCapacity, aliasBuffer, aliasCapacity) ||
            RangesOverlap(fileBuffer, fileCapacity, aliasBuffer, aliasCapacity))
            return (int)ShellCommandResult.Fail;

        if (!ReadArgsCommandSupport.Prepare(ref platform, tokenBuffer,
                tokenCapacity, ReadArgsCommandTemplate.Resident, 36,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;
        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, 36, out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        var name = APTR.FromPointer(platform.ReadUInt32(resultArray));
        var file = APTR.FromPointer(platform.ReadUInt32(resultArray, 4));
        var alias = APTR.FromPointer(platform.ReadUInt32(resultArray, 8));
        if (!CopyOptional(ref platform, name, nameBuffer, nameCapacity,
                out var nameLength) ||
            !CopyOptional(ref platform, file, fileBuffer, fileCapacity,
                out var fileLength) ||
            !CopyOptional(ref platform, alias, aliasBuffer, aliasCapacity,
                out var aliasLength))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        var remove = platform.ReadUInt32(resultArray, 12);
        var add = platform.ReadUInt32(resultArray, 16);
        var replace = platform.ReadUInt32(resultArray, 20);
        var force = platform.ReadUInt32(resultArray, 24);
        var system = platform.ReadUInt32(resultArray, 28);
        var defer = platform.ReadUInt32(resultArray, 32);
        platform.FreeArgs(rdArgs);

        var nameArgument = nameBuffer;
        if (name.IsNull) nameArgument = APTR.FromPointer(0);
        var fileArgument = fileBuffer;
        if (file.IsNull) fileArgument = APTR.FromPointer(0);
        var aliasArgument = aliasBuffer;
        if (alias.IsNull) aliasArgument = APTR.FromPointer(0);
        return platform.TryManageResident(invocation.Cli,
                invocation.Output,
                nameArgument,
                nameLength,
                fileArgument,
                fileLength,
                aliasArgument,
                aliasLength,
                remove, add, replace, force, system, defer)
            ? (int)ShellCommandResult.Ok
            : (int)ShellCommandResult.Fail;
    }

    private static bool CopyOptional<TPlatform>(
        ref TPlatform platform,
        APTR source,
        APTR destination,
        uint capacity,
        out uint length)
        where TPlatform : struct, IShellPlatform
    {
        length = 0;
        return source.IsNull || ReadArgsCommandSupport.CopyCString(
            ref platform, source, destination, capacity, out length);
    }

    private static bool RangesOverlap(
        APTR first,
        uint firstLength,
        APTR second,
        uint secondLength)
    {
        if (first.Raw > uint.MaxValue - firstLength ||
            second.Raw > uint.MaxValue - secondLength)
            return true;
        var firstEnd = first.Raw + firstLength;
        var secondEnd = second.Raw + secondLength;
        return first.Raw < secondEnd && second.Raw < firstEnd;
    }
}
