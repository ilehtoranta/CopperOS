using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-bound MorphOS 3.20 Dir body. It uses DOS ReadArgs, Lock, ExAll,
/// CurrentDir, and resident-owned buffers for listing and recursive ALL.
/// </summary>
public static class NativeMorphOSDirCommand
{
    public const string Template = "DIR,OPT/K,ALL/S,DIRS/S,FILES/S,INTER/S";
    public const uint ResultCount = 6;

    private const uint WorkspaceBytes = 8192;
    private const uint ExAllBytes = 8192;
    private const uint RecordBytes = 8;
    private const uint RowGrowth = 128;
    private const uint RecursiveScratchOffset = 2048;
    private const uint PatternLabelStart = 2304;
    private const uint PatternPathBytes = 512;
    private const uint DanglingLinkBufferBytes = 4096;
    private const uint DanglingLinkScratchBytes = DanglingLinkBufferBytes + 8;
    private const uint CtrlCMask = 1u << 12;

    private struct NameRow
    {
        public uint Name;

        public static APTR AddressOf(ref NameRow row) =>
            throw new System.NotSupportedException(
                "Dir.NameRow.AddressOf is lowered by CopperSharp.");
    }

    private struct PairRow
    {
        public uint First;
        public uint Second;

        public static APTR AddressOf(ref PairRow row) =>
            throw new System.NotSupportedException(
                "Dir.PairRow.AddressOf is lowered by CopperSharp.");
    }

    public static int Run(out int ioError) =>
        Run(true, Template, ResultCount, out ioError);

    /// <summary>Runs the bounded public-DOS body with a profile template.</summary>
    public static int Run(CString template, uint resultCount,
        out int ioError) => Run(true, template, resultCount, out ioError);

    public static int Run(bool morphosProfile, CString template,
        uint resultCount, out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(template, resultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_WARN;
        var error = 0;
        var ioErrorOnSuccess = 0;
        var ioErrorWasOverridden = false;
        var overriddenIoError = 0;
        var reportCommandFailure = false;
        var diagnosticPath = CString.FromPointer(0);
        var workspace = APTR.Null;
        var danglingLinkScratch = APTR.Null;
        var locked = BPTR.Null;
        var lockHeld = false;

        do
        {
            var directory = ReadPointer(arguments, 0);
            var option = ReadPointer(arguments, 1);
            var all = ReadSwitch(arguments, 2) != 0;
            var dirs = ReadSwitch(arguments, 3) != 0;
            var files = ReadSwitch(arguments, 4) != 0;
            var interactiveSwitch = ReadSwitch(arguments, 5) != 0;
            var inter = false;

            if (interactiveSwitch || all && !morphosProfile)
            {
                error = (int)DOS.Error.NotImplemented;
                break;
            }

            if (option.IsNotNull)
            {
                var optionOffset = 0;
                while (true)
                {
                    var value = APTR.ReadUInt8(option,
                        optionOffset++);
                    if (value == 0) break;
                    var upper = ToUpper(value);
                    if (upper == (byte)'D') dirs = true;
                    else if (upper == (byte)'F') files = true;
                    else if (upper == (byte)'A' && morphosProfile) all = true;
                    else if (upper == (byte)'A')
                    {
                        error = (int)DOS.Error.NotImplemented;
                        break;
                    }
                    else if (upper == (byte)'I' && morphosProfile)
                    {
                        inter = true;
                    }
                    else if (upper == (byte)'I')
                    {
                        error = (int)DOS.Error.NotImplemented;
                        break;
                    }
                    else
                    {
                        var ignored = default(NameRow);
                        ignored.Name = value;
                        DOS.VPrintf("%c option ignored\n",
                            NameRow.AddressOf(ref ignored));
                    }
                }
                if (error != 0) break;
            }

            if (!dirs && !files)
            {
                dirs = true;
                files = true;
            }

            CString target;
            if (directory.IsNull)
            {
                workspace = Exec.AllocVec(1,
                    (uint)Exec.MemoryFlags.Any);
                if (workspace.IsNull)
                {
                    error = (int)DOS.Error.NoFreeStore;
                    break;
                }
                APTR.WriteUInt8(workspace, 0, 0);
                target = CString.FromPointer(workspace.Raw);
            }
            else target = CString.FromPointer(directory.Raw);
            diagnosticPath = target;

            if (directory.IsNotNull)
            {
                var pathLength = CStringLength(directory);
                var tokenBytes = pathLength * 2;
                if (tokenBytes != 0)
                {
                    var patternTokens = Exec.AllocVec(tokenBytes,
                        (uint)Exec.MemoryFlags.Any);
                    if (patternTokens.IsNull)
                    {
                        error = (int)DOS.Error.NoFreeStore;
                        break;
                    }
                    var isWild = DOS.ParsePattern(target, patternTokens,
                        unchecked((int)tokenBytes));
                    Exec.FreeVec(patternTokens);
                    if (isWild == 1)
                    {
                        result = ListPattern(directory, morphosProfile, all,
                            dirs, files, inter, 0, out error,
                            out overriddenIoError);
                        ioErrorWasOverridden = true;
                        reportCommandFailure = result != DOS.RETURN_OK;
                        break;
                    }
                }
            }

            locked = DOS.LockRaw(target,
                DOS.LockMode.Shared);
            if (locked.IsNull)
            {
                error = (int)DOS.IoErr();
                if (morphosProfile && error ==
                        (int)DOS.Error.ObjectNotFound)
                {
                    danglingLinkScratch = Exec.AllocVec(
                        DanglingLinkScratchBytes,
                        (uint)Exec.MemoryFlags.Any);
                    if (danglingLinkScratch.IsNull)
                    {
                        error = (int)DOS.Error.NoFreeStore;
                        break;
                    }
                    if (TryReportDanglingSoftLink(target,
                            danglingLinkScratch,
                            APTR.FromPointer(danglingLinkScratch.Raw +
                                DanglingLinkBufferBytes)))
                    {
                        ioErrorOnSuccess = error;
                        result = DOS.RETURN_OK;
                        error = 0;
                        break;
                    }
                }
                if (error == 0) error = (int)DOS.Error.ObjectNotFound;
                reportCommandFailure = true;
                break;
            }
            lockHeld = true;
            result = ListDirectory(locked, morphosProfile, all, dirs, files,
                inter: inter, depth: 0, out _, out error);
            if (result != DOS.RETURN_OK)
            {
                reportCommandFailure = true;
                break;
            }
            var completedIoError = DOS.IoErr();
            if (completedIoError == DOS.Error.ObjectNotFound)
                ioErrorOnSuccess = (int)completedIoError;
            result = DOS.RETURN_OK;
        }
        while (false);

        if (lockHeld) DOS.UnLock(locked);
        if (danglingLinkScratch.IsNotNull)
            Exec.FreeVec(danglingLinkScratch);
        arguments.Release();
        ioError = ioErrorWasOverridden
            ? overriddenIoError
            : error != 0 ? error : ioErrorOnSuccess;
        DOS.SetIoErr((DOS.Error)ioError);
        if (result != DOS.RETURN_OK || error != 0)
        {
            if (reportCommandFailure)
            {
                var fault = (int)DOS.IoErr();
                if (fault == (int)DOS.Error.NoMoreEntries) fault = 0;
                else if (fault == (int)DOS.Error.ObjectWrongType)
                {
                    var line = new NameRow { Name = (uint)diagnosticPath };
                    DOS.VPrintf("%s is not a directory\n",
                        NameRow.AddressOf(ref line));
                    fault = 204;
                }
                else if (fault != (int)DOS.Error.Break)
                {
                    var line = new NameRow { Name = (uint)diagnosticPath };
                    DOS.VPrintf("Could not get information for %s\n",
                        NameRow.AddressOf(ref line));
                }
                DOS.PrintFault((DOS.Error)fault, CString.FromPointer(0));
            }
            else DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            if (error == (int)DOS.Error.Break)
            {
                if (workspace.IsNotNull) Exec.FreeVec(workspace);
                return DOS.RETURN_WARN;
            }
            if (morphosProfile && error == (int)DOS.Error.NotImplemented)
            {
                if (workspace.IsNotNull) Exec.FreeVec(workspace);
                return DOS.RETURN_ERROR;
            }
            var failureResult = error != 0 && result == DOS.RETURN_WARN
                ? DOS.RETURN_FAIL
                : result != DOS.RETURN_OK ? result : DOS.RETURN_FAIL;
            if (workspace.IsNotNull) Exec.FreeVec(workspace);
            return failureResult;
        }
        if (workspace.IsNotNull) Exec.FreeVec(workspace);
        return result;
    }

    private static int ListDirectory(BPTR locked, bool morphosProfile,
        bool all, bool dirs, bool files, bool inter, uint depth,
        out bool foundAny,
        out int ioError)
    {
        foundAny = false;
        ioError = 0;
        var workspace = Exec.AllocVec(WorkspaceBytes,
            (uint)Exec.MemoryFlags.Any);
        var rows = APTR.Null;
        var exAllBuffer = APTR.Null;
        var control = APTR.Null;
        var controlHeld = false;
        var error = 0;
        var result = DOS.RETURN_OK;
        var count = 0u;
        var rowCapacity = 0u;
        if (workspace.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            return DOS.RETURN_FAIL;
        }
        APTR.WriteUInt8(workspace, 0, 0);
        exAllBuffer = Exec.AllocVec(ExAllBytes,
            (uint)Exec.MemoryFlags.Any);
        if (exAllBuffer.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            goto cleanup;
        }
        control = DOS.AllocDosObject((uint)DosObjectType.ExAllControl,
            APTR.Null);
        if (control.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            goto cleanup;
        }
        controlHeld = true;
        APTR.WriteUInt32(control, DosLayout.ExAllControl.LastKey, 0);
        APTR.WriteUInt32(control, DosLayout.ExAllControl.Entries, 0);

        while (true)
        {
            var more = DOS.ExAll(locked, exAllBuffer,
                (int)ExAllBytes, (int)DosExAllDataLevel.Comment, control);
            if (more == 0 && DOS.IoErr() != DOS.Error.NoMoreEntries)
            {
                error = (int)DOS.IoErr();
                result = DOS.RETURN_ERROR;
                break;
            }
            // eac_Entries is the number of ExAllData records this call
            // placed in the buffer; the chain always starts at the buffer.
            var entryCount = APTR.ReadUInt32(control,
                DosLayout.ExAllControl.Entries);
            var entry = entryCount != 0 ? exAllBuffer.Raw : 0u;
            while (entry != 0)
            {
                var name = APTR.ReadUInt32(APTR.FromPointer(entry),
                    DosLayout.ExAllData.Name);
                var type = unchecked((int)APTR.ReadUInt32(
                    APTR.FromPointer(entry), DosLayout.ExAllData.Type));
                if (morphosProfile && name != 0 && type ==
                        (int)DosConstants.SoftLink)
                    type = ResolveExAllSoftLink(locked,
                        APTR.FromPointer(name), type);
                if (name != 0 && ((type > 0 && dirs) ||
                        (type <= 0 && files)))
                {
                    var source = APTR.FromPointer(name);
                    var length = CStringLength(source);
                    if (!EnsureRowCapacity(ref rows, ref rowCapacity,
                            count + 1, count, out error))
                    {
                        break;
                    }
                    var copy = Exec.AllocVec(length + 1,
                        (uint)Exec.MemoryFlags.Any);
                    if (copy.IsNull)
                    {
                        error = (int)DOS.Error.NoFreeStore;
                        break;
                    }
                    CopyCString(source, copy, length + 1);
                    var row = APTR.FromPointer(rows.Raw +
                        count * RecordBytes);
                    APTR.WriteUInt32(row, 0, copy.Raw);
                    APTR.WriteUInt32(row, 4, unchecked((uint)type));
                    count++;
                }
                entry = APTR.ReadUInt32(APTR.FromPointer(entry),
                    DosLayout.ExAllData.Next);
            }
            if (error != 0 || more == 0) break;
            if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
            {
                error = (int)DOS.Error.Break;
                break;
            }
        }

        if (error == 0)
        {
            if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                error = (int)DOS.Error.Break;
        }

        if (error == 0)
        {
            var directoryCount = SortRows(rows, count);
            foundAny = count != 0;
            for (var index = 0u; index < directoryCount; index++)
            {
                var row = APTR.FromPointer(rows.Raw +
                    index * RecordBytes);
                var name = APTR.FromPointer(APTR.ReadUInt32(row, 0));
                if (dirs)
                {
                    if (!WriteIndent(depth + 1, out error)) break;
                    var line = new NameRow { Name = name.Raw };
                    DOS.VPrintf("%s (dir)", NameRow.AddressOf(ref line));
                    if (!inter) DOS.FPuts(DOS.Output(), "\n");
                }
                if (all && dirs)
                {
                    var childResult = ListChildDirectory(locked, name,
                        exAllBuffer, APTR.FromPointer(workspace.Raw +
                            RecursiveScratchOffset), morphosProfile, all,
                        dirs, files, inter, depth + 1, ref foundAny,
                        out error);
                    if (childResult != DOS.RETURN_OK)
                    {
                        result = childResult;
                        break;
                    }
                }
            }

            if (error == 0)
                WriteFileRows(rows, directoryCount, count, depth,
                    inter, out error);
        }

    cleanup:
        if (controlHeld)
            DOS.FreeDosObject((uint)DosObjectType.ExAllControl, control);
        if (exAllBuffer.IsNotNull) Exec.FreeVec(exAllBuffer);
        FreeRows(rows, count);
        Exec.FreeVec(workspace);
        ioError = error;
        if (result == DOS.RETURN_OK && error != 0)
            result = error == (int)DOS.Error.Break
                ? DOS.RETURN_WARN : DOS.RETURN_FAIL;
        return result;
    }

    private static int ResolveExAllSoftLink(BPTR parentLock, APTR name,
        int fallbackType)
    {
        var previousDirectory = DOS.CurrentDirRaw(parentLock);
        var targetLock = DOS.LockRaw(CString.FromPointer(name.Raw),
            DOS.LockMode.Read);
        DOS.CurrentDirRaw(previousDirectory);
        if (targetLock.IsNull) return fallbackType;

        var fileInfo = default(FileInfoBlock);
        var fileInfoPointer = FileInfoBlock.AddressOf(ref fileInfo);
        var examined = DOS.Examine(targetLock, fileInfoPointer);
        var type = examined != 0
            ? FileInfoBlock.GetDirEntryType(fileInfoPointer.Raw)
            : fallbackType;
        DOS.UnLock(targetLock);
        return type;
    }

    private static int ListChildDirectory(BPTR parentLock, APTR name,
        APTR scratch, APTR warningArguments, bool morphosProfile, bool all,
        bool dirs, bool files, bool inter, uint depth, ref bool foundAny,
        out int ioError)
    {
        var previousDirectory = DOS.CurrentDirRaw(parentLock);
        var childLock = DOS.LockRaw(CString.FromPointer(name.Raw),
            DOS.LockMode.Shared);
        var lockError = childLock.IsNull ? (int)DOS.IoErr() : 0;
        DOS.CurrentDirRaw(previousDirectory);
        if (childLock.IsNull)
        {
            ioError = lockError != 0 ? lockError :
                (int)DOS.Error.ObjectNotFound;
            if (morphosProfile && ioError ==
                    (int)DOS.Error.ObjectNotFound)
            {
                var path = scratch;
                var linkBuffer = APTR.FromPointer(scratch.Raw +
                    DanglingLinkBufferBytes);
                if (DOS.NameFromLock(parentLock, path,
                        unchecked((int)DanglingLinkBufferBytes)) != 0)
                {
                    if (DOS.AddPart(CString.FromPointer(path.Raw),
                            CString.FromPointer(name.Raw),
                            DanglingLinkBufferBytes) == 0)
                    {
                        ioError = (int)DOS.Error.LineTooLong;
                        return DOS.RETURN_ERROR;
                    }
                    if (TryReportDanglingSoftLink(
                            CString.FromPointer(path.Raw), linkBuffer,
                            warningArguments))
                    {
                        ioError = 0;
                        return DOS.RETURN_OK;
                    }
                }
            }
            return ioError == (int)DOS.Error.LineTooLong
                ? DOS.RETURN_ERROR : DOS.RETURN_FAIL;
        }
        var result = ListDirectory(childLock, morphosProfile, all, dirs,
            files, inter, depth, out var childFound, out ioError);
        DOS.UnLock(childLock);
        if (childFound) foundAny = true;
        return result;
    }

    private static bool TryReportDanglingSoftLink(CString path,
        APTR linkBuffer, APTR warningArguments)
    {
        var savedError = DOS.IoErr();
        if (savedError != DOS.Error.ObjectNotFound) return false;
        var device = DOS.GetDeviceProc(path, APTR.Null);
        if (device.IsNull)
        {
            DOS.SetIoErr(savedError);
            return false;
        }

        var port = APTR.FromPointer(APTR.ReadUInt32(device,
            DosLayout.DevProc.Port));
        var deviceLock = BPTR.FromRaw(APTR.ReadUInt32(device,
            DosLayout.DevProc.Lock));
        var read = DOS.ReadLink(port, deviceLock, path, linkBuffer,
            unchecked((int)(DanglingLinkBufferBytes - 1)));
        if (read > 0)
        {
            APTR.WriteUInt8(linkBuffer,
                unchecked((int)(DanglingLinkBufferBytes - 1)), 0);
            APTR.WriteUInt32(warningArguments, 0, (uint)path);
            APTR.WriteUInt32(warningArguments, 4, linkBuffer.Raw);
            DOS.VPrintf("Warning: Skipping dangling softlink %s -> %s\n",
                warningArguments);
        }
        DOS.FreeDeviceProc(device);
        DOS.SetIoErr(savedError);
        return read > 0;
    }

    private static int ListPattern(APTR pattern, bool morphosProfile,
        bool all, bool dirs, bool files, bool inter, uint depth,
        out int ioError, out int finalIoError)
    {
        ioError = 0;
        finalIoError = 0;
        var result = DOS.RETURN_FAIL;
        var workspace = Exec.AllocVec(WorkspaceBytes,
            (uint)Exec.MemoryFlags.Any);
        var rows = APTR.Null;
        var rowCapacity = 0u;
        var anchor = APTR.Null;
        var anchorHeld = false;
        var matchStarted = false;
        var error = 0;
        var fileCount = 0u;
        if (workspace.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            return DOS.RETURN_FAIL;
        }
        APTR.WriteUInt8(workspace, 0, 0);
        anchor = Exec.AllocVec((uint)(DosLayout.AnchorPath.Size +
                PatternPathBytes),
            (uint)(Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
        if (anchor.IsNull)
        {
            error = (int)DOS.Error.NoFreeStore;
            goto cleanup;
        }
        anchorHeld = true;
        InitializeAnchor(anchor);
        var match = DOS.MatchFirst(CString.FromPointer(pattern.Raw), anchor);
        if (match != 0)
        {
            error = match;
            finalIoError = match;
            result = match == (int)DOS.Error.Break
                ? DOS.RETURN_WARN : DOS.RETURN_FAIL;
            goto cleanup;
        }
        matchStarted = true;
        result = DOS.RETURN_OK;

        while (true)
        {
            var info = APTR.FromPointer(anchor.Raw +
                (uint)DosLayout.AnchorPath.Info);
            var type = unchecked((int)APTR.ReadUInt32(info,
                FileInfoBlock.DirEntryTypeOffset));
            var isDirectory = type > 0;
            if (morphosProfile && type == (int)DosConstants.SoftLink)
            {
                var linkInfo = APTR.FromPointer(workspace.Raw +
                    PatternLabelStart);
                var linkBuffer = APTR.FromPointer(workspace.Raw + 2560);
                var warningArguments = APTR.FromPointer(workspace.Raw + 3072);
                isDirectory = NativeMorphOSSearchSoftLinks.ShouldDescend(
                    anchor, linkInfo, linkBuffer, warningArguments, false) != 0;
                type = unchecked((int)APTR.ReadUInt32(info,
                    FileInfoBlock.DirEntryTypeOffset));
            }
            if (isDirectory)
            {
                if (dirs)
                {
                    var directoryResult = WriteMatchedDirectory(anchor,
                        workspace, morphosProfile, all, dirs, files, inter,
                        depth, out error);
                    if (directoryResult != DOS.RETURN_OK)
                    {
                        result = directoryResult;
                    }
                }
                if (result == DOS.RETURN_OK && all && !dirs)
                {
                    var directoryResult = ListMatchedDirectoryContents(
                        anchor, morphosProfile, all, dirs, files, inter,
                        depth, out error);
                    if (directoryResult != DOS.RETURN_OK)
                    {
                        result = directoryResult;
                    }
                }
            }
            else if (files)
            {
                var name = APTR.FromPointer(info.Raw +
                    FileInfoBlock.FileNameOffset);
                var length = CStringLength(name);
                if (!EnsureRowCapacity(ref rows, ref rowCapacity,
                        fileCount + 1, fileCount, out error))
                {
                    finalIoError = error;
                    result = DOS.RETURN_FAIL;
                    break;
                }
                var copy = Exec.AllocVec(length + 1,
                    (uint)Exec.MemoryFlags.Any);
                if (copy.IsNull)
                {
                    error = (int)DOS.Error.NoFreeStore;
                    finalIoError = error;
                    result = DOS.RETURN_FAIL;
                    break;
                }
                CopyCString(name, copy, length + 1);
                var row = APTR.FromPointer(rows.Raw +
                    fileCount * RecordBytes);
                APTR.WriteUInt32(row, 0, copy.Raw);
                APTR.WriteUInt32(row, 4, unchecked((uint)type));
                fileCount++;
            }

            // Save IoErr after processing the current match, before
            // MatchNext can replace it with ERROR_NO_MORE_ENTRIES.
            finalIoError = (int)DOS.IoErr();
            if (result != DOS.RETURN_OK) break;

            APTR.WriteUInt16(anchor,
                DosLayout.AnchorPath.StringLength,
                unchecked((ushort)PatternPathBytes));
            match = DOS.MatchNext(anchor);
            if (match == 0) continue;
            if (match == (int)DOS.Error.NoMoreEntries) break;
            error = match;
            finalIoError = match;
            result = match == (int)DOS.Error.Break
                ? DOS.RETURN_WARN : DOS.RETURN_FAIL;
            break;
        }

        if (error == 0 && fileCount != 0)
        {
            SortRows(rows, fileCount);
            WriteFileRows(rows, 0, fileCount, depth, inter,
                out error);
            if (error != 0)
                result = error == (int)DOS.Error.Break
                    ? DOS.RETURN_WARN : DOS.RETURN_FAIL;
        }

    cleanup:
        if (matchStarted) DOS.MatchEnd(anchor);
        if (anchorHeld) Exec.FreeVec(anchor);
        FreeRows(rows, fileCount);
        Exec.FreeVec(workspace);
        DOS.SetIoErr((DOS.Error)finalIoError);
        ioError = error;
        return result;
    }

    private static int WriteMatchedDirectory(APTR anchor, APTR workspace,
        bool morphosProfile, bool all, bool dirs, bool files, bool inter,
        uint depth, out int ioError)
    {
        ioError = 0;
        var path = APTR.FromPointer(anchor.Raw +
            (uint)DosLayout.AnchorPath.PathBuffer);
        var label = APTR.FromPointer(workspace.Raw + PatternLabelStart);
        var locked = DOS.LockRaw(CString.FromPointer(path.Raw),
            DOS.LockMode.Shared);
        if (locked.IsNotNull)
        {
            if (DOS.NameFromLock(locked, label,
                    unchecked((int)PatternPathBytes)) != 0)
            {
                if (WriteIndent(depth + 1, out ioError))
                {
                    var line = new NameRow { Name = label.Raw };
                    DOS.VPrintf("%s (dir)", NameRow.AddressOf(ref line));
                    if (!inter) DOS.FPuts(DOS.Output(), "\n");
                }
            }
            DOS.UnLock(locked);
            if (ioError != 0) return ioError == (int)DOS.Error.Break
                ? DOS.RETURN_WARN : DOS.RETURN_FAIL;
        }
        if (all)
            return ListMatchedDirectoryContents(anchor, morphosProfile,
                all, dirs, files, inter, depth, out ioError);
        return DOS.RETURN_OK;
    }

    private static int ListMatchedDirectoryContents(APTR anchor,
        bool morphosProfile, bool all, bool dirs, bool files, bool inter,
        uint depth, out int ioError)
    {
        var path = APTR.FromPointer(anchor.Raw +
            (uint)DosLayout.AnchorPath.PathBuffer);
        var locked = DOS.LockRaw(CString.FromPointer(path.Raw),
            DOS.LockMode.Shared);
        if (locked.IsNull)
        {
            ioError = (int)DOS.IoErr();
            if (ioError == 0) ioError = (int)DOS.Error.ObjectNotFound;
            return ioError == (int)DOS.Error.Break
                ? DOS.RETURN_WARN : DOS.RETURN_FAIL;
        }
        var result = ListDirectory(locked, morphosProfile, all, dirs,
            files, inter, depth + 1, out _, out ioError);
        DOS.UnLock(locked);
        return result;
    }

    private static void WriteFileRows(APTR rows, uint first,
        uint count, uint depth, bool inter, out int ioError)
    {
        ioError = 0;
        for (var index = first; index < count; index += 2)
        {
            if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
            {
                ioError = (int)DOS.Error.Break;
                return;
            }
            var firstRow = APTR.FromPointer(rows.Raw +
                index * RecordBytes);
            var secondRow = index + 1 < count
                ? APTR.FromPointer(rows.Raw + (index + 1) * RecordBytes)
                : APTR.Null;
            if (!WriteIndent(depth, out ioError)) return;
            var line = new PairRow {
                First = APTR.ReadUInt32(firstRow, 0),
                Second = secondRow.IsNotNull
                    ? APTR.ReadUInt32(secondRow, 0) : rows.Raw
            };
            DOS.VPrintf("  %-32.s %s", PairRow.AddressOf(ref line));
            if (!inter) DOS.FPuts(DOS.Output(), "\n");
        }
    }

    private static void InitializeAnchor(APTR anchor)
    {
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Base, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Current, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, CtrlCMask);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.FoundBreak, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Reserved, 0);
        APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength,
            unchecked((ushort)PatternPathBytes));
    }

    private static bool WriteIndent(uint depth, out int ioError)
    {
        ioError = 0;
        for (var level = 0u; level < depth; level++)
        {
            if (DOS.PutStr("     ") >= 0) continue;
            ioError = (int)DOS.IoErr();
            return false;
        }
        return true;
    }

    private static APTR ReadPointer(NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value)
        ? APTR.FromPointer(value) : APTR.Null;

    private static uint ReadSwitch(NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value) ? value : 0;

    private static byte ToUpper(byte value) => value >= (byte)'a' &&
        value <= (byte)'z' ? (byte)(value - ((byte)'a' - (byte)'A')) : value;

    private static uint CStringLength(APTR value)
    {
        var length = 0u;
        while (length < 511 && APTR.ReadUInt8(value,
                   unchecked((int)length)) != 0) length++;
        return length;
    }

    private static void CopyCString(APTR source, APTR destination,
        uint capacity)
    {
        for (var index = 0u; index < capacity; index++)
        {
            var value = APTR.ReadUInt8(source, unchecked((int)index));
            APTR.WriteUInt8(destination, unchecked((int)index), value);
            if (value == 0) break;
        }
    }

    private static bool EnsureRowCapacity(ref APTR rows,
        ref uint capacity, uint needed, uint used, out int ioError)
    {
        ioError = 0;
        if (needed <= capacity) return true;
        var nextCapacity = capacity;
        while (nextCapacity < needed)
        {
            if (nextCapacity > 0x1fffffff - RowGrowth)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                return false;
            }
            nextCapacity += RowGrowth;
        }
        var replacement = Exec.AllocVec(nextCapacity * RecordBytes,
            (uint)Exec.MemoryFlags.Any);
        if (replacement.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            return false;
        }
        for (var index = 0u; index < used; index++)
        {
            var source = APTR.FromPointer(rows.Raw + index * RecordBytes);
            var target = APTR.FromPointer(replacement.Raw +
                index * RecordBytes);
            APTR.WriteUInt32(target, 0, APTR.ReadUInt32(source, 0));
            APTR.WriteUInt32(target, 4, APTR.ReadUInt32(source, 4));
        }
        if (rows.IsNotNull) Exec.FreeVec(rows);
        rows = replacement;
        capacity = nextCapacity;
        return true;
    }

    private static void FreeRows(APTR rows, uint count)
    {
        if (rows.IsNull) return;
        for (var index = 0u; index < count; index++)
        {
            var row = APTR.FromPointer(rows.Raw + index * RecordBytes);
            var name = APTR.FromPointer(APTR.ReadUInt32(row, 0));
            if (name.IsNotNull) Exec.FreeVec(name);
        }
        Exec.FreeVec(rows);
    }

    private static uint SortRows(APTR workspace, uint count)
    {
        var directoryCount = 0u;
        for (var index = 1u; index < count; index++)
        {
            var current = APTR.FromPointer(workspace.Raw +
                index * RecordBytes);
            var currentName = APTR.ReadUInt32(current, 0);
            var currentType = APTR.ReadUInt32(current, 4);
            var position = index;
            while (position > 0)
            {
                var previous = APTR.FromPointer(workspace.Raw +
                    (position - 1) * RecordBytes);
                var previousName = APTR.ReadUInt32(previous, 0);
                var previousType = APTR.ReadUInt32(previous, 4);
                var previousDirectory = unchecked((int)previousType) > 0;
                var currentDirectory = unchecked((int)currentType) > 0;
                if (previousDirectory && !currentDirectory ||
                    previousDirectory && currentDirectory ||
                    !previousDirectory && !currentDirectory &&
                    Compare(APTR.FromPointer(previousName),
                        APTR.FromPointer(currentName)) <= 0) break;
                var destination = APTR.FromPointer(workspace.Raw +
                    position * RecordBytes);
                APTR.WriteUInt32(destination, 0, previousName);
                APTR.WriteUInt32(destination, 4, previousType);
                position--;
            }
            var target = APTR.FromPointer(workspace.Raw +
                position * RecordBytes);
            APTR.WriteUInt32(target, 0, currentName);
            APTR.WriteUInt32(target, 4, currentType);
        }
        while (directoryCount < count)
        {
            var row = APTR.FromPointer(workspace.Raw +
                directoryCount * RecordBytes);
            if (unchecked((int)APTR.ReadUInt32(row, 4)) <= 0) break;
            directoryCount++;
        }
        return directoryCount;
    }

    private static int Compare(APTR left, APTR right)
    {
        var offset = 0;
        while (true)
        {
            var a = ToUpper(APTR.ReadUInt8(left,
                offset));
            var b = ToUpper(APTR.ReadUInt8(right,
                offset));
            if (a != b) return a < b ? -1 : 1;
            if (a == 0) return 0;
            offset++;
        }
    }
}
