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
                tokenCapacity, ReadArgsCommandTemplate.Resident,
                ResidentReadArgsResultRecord.Size,
                out var resultArray, out var templateLength))
            return (int)ShellCommandResult.Error;
        if (!platform.TryReadArgs(invocation.ArgumentText,
                invocation.ArgumentLength, tokenBuffer, templateLength,
                resultArray, ResidentReadArgsResultRecord.Size,
                out var rdArgs) || rdArgs.IsNull)
            return (int)ShellCommandResult.Error;

        if (!ResidentReadArgsResultRecordCodec.TryRead(ref platform,
                resultArray, out var parsed))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        if (!CopyOptional(ref platform, parsed.Name, nameBuffer, nameCapacity,
                out var nameLength) ||
            !CopyOptional(ref platform, parsed.File, fileBuffer, fileCapacity,
                out var fileLength) ||
            !CopyOptional(ref platform, parsed.Alias, aliasBuffer, aliasCapacity,
                out var aliasLength))
        {
            platform.FreeArgs(rdArgs);
            return (int)ShellCommandResult.Error;
        }

        platform.FreeArgs(rdArgs);

        if (!ValidSwitchResult(parsed.Remove) ||
            !ValidSwitchResult(parsed.Add) ||
            !ValidSwitchResult(parsed.Replace) ||
            !ValidSwitchResult(parsed.Force) ||
            !ValidSwitchResult(parsed.System) ||
            !ValidSwitchResult(parsed.Defer))
            return (int)ShellCommandResult.Error;

        var remove = NormalizeSwitchResult(parsed.Remove);
        var add = NormalizeSwitchResult(parsed.Add);
        var replace = NormalizeSwitchResult(parsed.Replace);
        var force = NormalizeSwitchResult(parsed.Force);
        var system = NormalizeSwitchResult(parsed.System);
        var defer = NormalizeSwitchResult(parsed.Defer);
        var operationCount = remove + add + replace;
        var aliasOperation = parsed.Alias.IsNotNull;
        // Classic startup files give only a full filename, e.g.
        // Resident C:Assign PURE. ReadArgs places that in NAME; preserve the
        // backing path and derive the public resident name from its FilePart.
        if (!aliasOperation && remove == 0 && parsed.File.IsNull &&
            FilePartOffset(ref platform, nameBuffer, nameLength) != 0)
        {
            if (!ReadArgsCommandSupport.CopyCString(ref platform, nameBuffer,
                    fileBuffer, fileCapacity, out fileLength))
                return (int)ShellCommandResult.Error;
            parsed.File = fileBuffer;
            parsed.Name = APTR.Null;
        }
        if (!aliasOperation && parsed.Name.IsNull && parsed.File.IsNotNull)
        {
            var part = FilePartOffset(ref platform, fileBuffer, fileLength);
            if (!ReadArgsCommandSupport.CopyCString(ref platform,
                    APTR.FromPointer(fileBuffer.Raw + part), nameBuffer,
                    nameCapacity, out nameLength))
                return (int)ShellCommandResult.Error;
            parsed.Name = nameBuffer;
        }
        if (operationCount > 1 ||
            (aliasOperation && (parsed.File.IsNotNull || remove != 0 ||
                force != 0 || defer != 0)) ||
            (!aliasOperation && parsed.File.IsNull &&
                (add != 0 || force != 0 || defer != 0)))
            return (int)ShellCommandResult.Error;

        var nameArgument = nameBuffer;
        if (parsed.Name.IsNull) nameArgument = APTR.FromPointer(0);
        var fileArgument = fileBuffer;
        if (parsed.File.IsNull) fileArgument = APTR.FromPointer(0);
        var aliasArgument = aliasBuffer;
        if (parsed.Alias.IsNull) aliasArgument = APTR.FromPointer(0);

        var internalCommand = nameLength == 0
            ? ShellInternalCommand.Unknown
            : ShellInternalCommandResolver.Resolve(ref platform, nameBuffer,
                nameLength);
        if (!aliasOperation && parsed.File.IsNull && internalCommand !=
                ShellInternalCommand.Unknown && (remove != 0 || replace != 0))
        {
            var enabled = remove != 0 ? 0u : 1u;
            return platform.TrySetInternalCommandEnabled(invocation.Cli,
                    (uint)internalCommand, enabled)
                ? (int)ShellCommandResult.Ok
                : (int)ShellCommandResult.Fail;
        }

        var listing = parsed.File.IsNull && !aliasOperation && remove == 0;
        if (!listing && remove == 0 && add == 0 && replace == 0)
        {
            if (aliasOperation) add = 1;
            else replace = 1;
        }
        var request = new ShellResidentManagementRequest
        {
            Output = invocation.Output,
            Name = nameArgument,
            NameLength = nameLength,
            File = fileArgument,
            FileLength = fileLength,
            Alias = aliasArgument,
            AliasLength = aliasLength,
            Remove = listing ? 0u : remove,
            Add = listing ? 0u : add,
            Replace = listing ? 0u : replace,
            Force = listing ? 0u : force,
            System = system,
            Defer = listing ? 0u : defer,
        };
        var success = platform.TryManageResident(invocation.Cli,
            in request);
        if (!success) return (int)ShellCommandResult.Fail;
        if (listing && !ShellInternalCommandResolver.WriteResidentNames(
            ref platform, invocation.Cli, invocation.Output, system))
            return (int)ShellCommandResult.Fail;
        return (int)ShellCommandResult.Ok;
    }

    private static bool ValidSwitchResult(uint value) =>
        value == 0 || value == 1 || value == uint.MaxValue;

    private static uint FilePartOffset<TPlatform>(ref TPlatform platform,
        APTR text, uint length) where TPlatform : struct, IShellPlatform
    {
        var part = 0u;
        for (var i = 0u; i < length; i++)
        {
            var value = platform.ReadUInt8(text, (int)i);
            if (value == (byte)':' || value == (byte)'/') part = i + 1;
        }
        return part;
    }

    private static uint NormalizeSwitchResult(uint value) => value == 0
        ? 0u : 1u;

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
