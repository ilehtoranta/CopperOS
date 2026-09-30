using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 <c>Which</c> command body. The implementation uses
/// DOS-owned segment and CLI path records directly: it neither loads a segment
/// nor retains a lock, segment, or command-path node after one candidate has
/// been reported. Exact option interactions, diagnostics, cancellation, and
/// installed pure qualification remain reference-work items.
/// </summary>
public static class Workbench31WhichCommand
{
    private const string ClassicTemplate = "FILE/A,NORES/S,RES/S,ALL/S";
    private const string MorphOSTemplate =
        "FILE/A,NOALIAS/S,ALIAS/S,NORES/S,RES/S,ALL/S";
    private const uint BufferBytes = 1024;

    /// <summary>
/// Parses the classic Workbench 3.1 template and reports bounded resident,
/// file, and CLI-path matches. The caller owns DOS startup and final teardown
/// through <see cref="NativeCommandStartup"/>.
    /// </summary>
    public static int Run(out int ioError)
        => RunProfile(false, out ioError);

    /// <summary>
    /// Runs the MorphOS profile through the same DOS-owned lookup machinery.
    /// Alias lookup uses public DOS FindVar; its output formatting and exact
    /// search order remain unverified against the shipped command.
    /// </summary>
    public static int RunMorphOS(out int ioError)
        => RunProfile(true, out ioError);

    private static int RunProfile(bool morphos, out int ioError)
    {
        ioError = 0;
        var arguments = default(NativeCommandArguments);
        var parsed = false;
        if (morphos)
            parsed = NativeCommandArguments.TryRead(MorphOSTemplate, 6,
                out arguments);
        else
            parsed = NativeCommandArguments.TryRead(ClassicTemplate, 4,
                out arguments);
        if (!parsed)
        {
            ioError = arguments.IoError;
            if (!morphos && arguments.ReturnLevel == DOS.RETURN_ERROR)
            {
                // The original classic command reports the DOS ReadArgs fault
                // and returns WARN for argument syntax/required-field errors.
                DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
                return DOS.RETURN_WARN;
            }
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_WARN;
        var buffer = APTR.Null;
        var fileInfo = APTR.Null;
        var noAlias = morphos ? ReadSwitch(arguments, 1) : 0u;
        var alias = morphos ? ReadSwitch(arguments, 2) : 0u;
        var noResidentsIndex = morphos ? 3u : 1u;
        var residentsOnlyIndex = morphos ? 4u : 2u;
        var allIndex = morphos ? 5u : 3u;
        if (!arguments.TryGetResult(0, out var fileAddress) ||
            fileAddress == 0 ||
            !arguments.TryGetResult(noResidentsIndex, out var noResidents) ||
            !arguments.TryGetResult(residentsOnlyIndex, out var residentsOnly) ||
            !arguments.TryGetResult(allIndex, out var all))
        {
            result = DOS.RETURN_ERROR;
            ioError = (int)DOS.Error.BadTemplate;
            goto Cleanup;
        }

        buffer = Exec.AllocMem(BufferBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (buffer.IsNull)
        {
            result = DOS.RETURN_FAIL;
            ioError = (int)DOS.Error.NoFreeStore;
            goto Cleanup;
        }

        var file = CString.FromPointer(fileAddress);
        var filePart = STRPTR.ToUInt32(DOS.FilePart(file));
        var pathQualified = filePart != CString.ToUInt32(file);
        var assignRoot = pathQualified &&
            APTR.ReadUInt8(APTR.FromPointer(filePart), 0) == 0 &&
            APTR.ReadUInt8(APTR.FromPointer(filePart - 1), 0) == (byte)':';
        var continueAfterMatch = all != 0;
        var restrictToResidents = residentsOnly != 0;
        var reportResidents = noResidents == 0;
        var found = false;
        var foundNonInternal = false;

        // MorphOS extends Which with alias lookup. FindVar is the public DOS
        // API for an exact local alias name. Alias results are reported before
        // resident and filesystem results; exact original output/order remains
        // a differential gate because the packed shipped command is not yet
        // available in a MorphOS guest for capture.
        var aliasOnly = morphos && alias != 0;
        var aliasFound = morphos && noAlias == 0 &&
            (aliasOnly || !restrictToResidents) &&
            DOS.FindVar(file, (uint)LocalVariableType.Alias).IsNotNull;
        if (aliasOnly)
        {
            if (aliasFound)
            {
                if (!WriteAliasCategory(fileAddress, buffer))
                {
                    result = DOS.RETURN_ERROR;
                    ioError = (int)DOS.IoErr();
                    goto Cleanup;
                }
                result = DOS.RETURN_OK;
                ioError = 0;
            }
            else
            {
                result = DOS.RETURN_WARN;
                ioError = (int)DOS.Error.ObjectNotFound;
            }
            goto Cleanup;
        }

        if (aliasFound)
        {
            if (!WriteAliasCategory(fileAddress, buffer))
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.IoErr();
                goto Cleanup;
            }
            found = true;
            foundNonInternal = true;
            if (!continueAfterMatch)
            {
                result = DOS.RETURN_OK;
                ioError = 0;
                goto Cleanup;
            }
        }

        // System segments are the classic INTERNAL category. The direct
        // segment result is never dereferenced after Permit, so this does
        // not retain a resident-list reference while output may block.
        if (reportResidents && IsSegmentPresent(file, 1))
        {
            if (!WriteCategory(fileAddress, buffer, true))
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.IoErr();
                goto Cleanup;
            }
            found = true;
            if (!continueAfterMatch) { result = DOS.RETURN_OK; goto Cleanup; }
        }

        if (reportResidents && IsSegmentPresent(file, 0))
        {
            if (!WriteCategory(fileAddress, buffer, false))
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.IoErr();
                goto Cleanup;
            }
            found = true;
            foundNonInternal = true;
            if (!continueAfterMatch) { result = DOS.RETURN_OK; goto Cleanup; }
        }

        if (restrictToResidents)
        {
            if (found) { result = DOS.RETURN_OK; goto Cleanup; }
            result = DOS.RETURN_WARN;
            // The observed NORES+RES conflict returns WARN without
            // publishing ObjectNotFound, whether or not ALL is present.
            ioError = noResidents != 0 ? 0 : (int)DOS.Error.ObjectNotFound;
            goto Cleanup;
        }

        // Classic Which treats a bare directory name as a command lookup,
        // not as a path result. DOS Examine lets the candidate distinguish a
        // directory lock from an ordinary file while keeping all FIB state
        // invocation-local. MorphOS behavior remains on its independent path
        // until its reference semantics are captured.
        if (!morphos)
        {
            fileInfo = DOS.AllocDosObject(
                (uint)DosObjectType.FileInfoBlock, APTR.Null);
            if (fileInfo.IsNull)
            {
                result = DOS.RETURN_FAIL;
                ioError = (int)DOS.IoErr();
                if (ioError == 0) ioError = (int)DOS.Error.NoFreeStore;
                goto Cleanup;
            }
        }

        var direct = ReportLockedPath(file, buffer, fileInfo, assignRoot);
        if (direct < 0)
        {
            result = DOS.RETURN_ERROR;
            ioError = (int)DOS.IoErr();
            goto Cleanup;
        }
        if (direct != 0)
        {
            found = true;
            foundNonInternal = true;
            if (!continueAfterMatch) { result = DOS.RETURN_OK; goto Cleanup; }
        }

        var cli = DOS.Cli();
        var path = pathQualified || cli.IsNull
            ? BPTR.Null
            : BPTR.FromRaw(APTR.ReadUInt32(cli,
                DosLayout.CommandLineInterface.CommandDirectory));
        var slowPath = path;
        var fastPath = path;
        while (path.IsNotNull)
        {
            var node = path.Address;
            var directory = BPTR.FromRaw(APTR.ReadUInt32(node,
                DosLayout.PathLock.Lock));
            var next = BPTR.FromRaw(APTR.ReadUInt32(node,
                DosLayout.PathLock.Next));
            if (directory.IsNull ||
                DOS.NameFromLock(directory, buffer, (int)BufferBytes) == 0 ||
                DOS.AddPart(CString.FromPointer(buffer.Raw), file,
                    BufferBytes) == 0)
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.IoErr();
                goto Cleanup;
            }

            var match = ReportLockedPath(CString.FromPointer(buffer.Raw),
                buffer, fileInfo, false);
            if (match < 0)
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.IoErr();
                goto Cleanup;
            }
            if (match != 0)
            {
                found = true;
                foundNonInternal = true;
                if (!continueAfterMatch) { result = DOS.RETURN_OK; goto Cleanup; }
            }
            path = next;

            // Walk the complete DOS-owned CLI path instead of imposing a
            // command-local node limit. Detect a corrupt cycle without
            // allocating or retaining a copy of the list.
            slowPath = NextPathLock(slowPath);
            fastPath = NextPathLock(fastPath);
            if (fastPath.IsNotNull)
                fastPath = NextPathLock(fastPath);
            if (slowPath.IsNotNull && slowPath.Raw == fastPath.Raw)
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.Error.ObjectWrongType;
                goto Cleanup;
            }
        }

        // A path-qualified FILE is a complete lookup candidate. The original
        // command does not feed it through every CLI path entry. Bare names
        // get the C: search after current-directory and CLI-path candidates.
        if (!pathQualified)
        {
            if (!WriteCPath(file, buffer))
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.IoErr();
                goto Cleanup;
            }
            var cPathMatch = ReportLockedPath(
                CString.FromPointer(buffer.Raw), buffer, fileInfo, false);
            if (cPathMatch < 0)
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.IoErr();
                goto Cleanup;
            }
            if (cPathMatch != 0)
            {
                found = true;
                foundNonInternal = true;
                if (!continueAfterMatch)
                {
                    result = DOS.RETURN_OK;
                    goto Cleanup;
                }
            }
        }

        // The original 3.1 command reports an INTERNAL match during ALL,
        // but still ends with WARN/ObjectNotFound when no resident or
        // filesystem route succeeds. Preserve that observable result
        // without treating the system-segment pointer as an owned object.
        if (found && !foundNonInternal)
        {
            result = DOS.RETURN_WARN;
            ioError = (int)DOS.Error.ObjectNotFound;
            goto Cleanup;
        }
        if (!found)
        {
            result = DOS.RETURN_WARN;
            ioError = (int)DOS.IoErr();
            goto Cleanup;
        }
        result = DOS.RETURN_OK;

    Cleanup:
        if (fileInfo.IsNotNull)
            DOS.FreeDosObject((uint)DosObjectType.FileInfoBlock, fileInfo);
        if (buffer.IsNotNull)
            Exec.FreeMem(buffer, BufferBytes);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static uint ReadSwitch(NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value) ? value : 0;

    private static BPTR NextPathLock(BPTR path)
    {
        if (path.IsNull) return BPTR.Null;
        return BPTR.FromRaw(APTR.ReadUInt32(path.Address,
            DosLayout.PathLock.Next));
    }

    private static bool WriteCPath(CString file, APTR buffer)
    {
        APTR.WriteUInt8(buffer, 0, (byte)'C');
        APTR.WriteUInt8(buffer, 1, (byte)':');
        APTR.WriteUInt8(buffer, 2, 0);
        return DOS.AddPart(CString.FromPointer(buffer.Raw), file,
            BufferBytes) != 0;
    }

    private static bool IsSegmentPresent(CString name, int system)
    {
        Exec.Forbid();
        var segment = DOS.FindSegment(name, APTR.Null, system);
        Exec.Permit();
        return segment.IsNotNull;
    }

    private static int ReportLockedPath(CString candidate, APTR buffer,
        APTR fileInfo, bool allowDirectory)
    {
        var locked = DOS.Lock(candidate, DOS.LockMode.Shared);
        if (!locked.HasValue) return 0;
        var value = locked.Value;
        if (fileInfo.IsNotNull)
        {
            if (DOS.Examine(value, fileInfo) == 0)
            {
                var examineError = (int)DOS.IoErr();
                DOS.UnLock(value);
                DOS.SetIoErr((DOS.Error)examineError);
                return -1;
            }
            if (!allowDirectory &&
                FileInfoBlock.GetDirEntryType(fileInfo.Raw) > 0)
            {
                var directoryError = (int)DOS.IoErr();
                DOS.UnLock(value);
                DOS.SetIoErr((DOS.Error)directoryError);
                return 0;
            }
        }
        var reported = DOS.NameFromLock(value, buffer, (int)BufferBytes) != 0 &&
            WriteLine(buffer, buffer);
        var error = reported ? 0 : (int)DOS.IoErr();
        DOS.UnLock(value);
        if (!reported)
            DOS.SetIoErr((DOS.Error)error);
        return reported ? 1 : -1;
    }

    private static bool WriteCategory(uint fileAddress, APTR buffer,
        bool internalSegment)
    {
        var prefixLength = internalSegment ? 9u : 4u;
        if (internalSegment)
        {
            APTR.WriteUInt8(buffer, 0, (byte)'I');
            APTR.WriteUInt8(buffer, 1, (byte)'N');
            APTR.WriteUInt8(buffer, 2, (byte)'T');
            APTR.WriteUInt8(buffer, 3, (byte)'E');
            APTR.WriteUInt8(buffer, 4, (byte)'R');
            APTR.WriteUInt8(buffer, 5, (byte)'N');
            APTR.WriteUInt8(buffer, 6, (byte)'A');
            APTR.WriteUInt8(buffer, 7, (byte)'L');
            APTR.WriteUInt8(buffer, 8, (byte)' ');
        }
        else
        {
            APTR.WriteUInt8(buffer, 0, (byte)'R');
            APTR.WriteUInt8(buffer, 1, (byte)'E');
            APTR.WriteUInt8(buffer, 2, (byte)'S');
            APTR.WriteUInt8(buffer, 3, (byte)' ');
        }

        var source = APTR.FromPointer(fileAddress);
        for (var index = 0u; index + prefixLength + 1 < BufferBytes; index++)
        {
            var value = APTR.ReadUInt8(source, (int)index);
            if (value == 0)
            {
                APTR.WriteUInt8(buffer, (int)(prefixLength + index),
                    (byte)'\n');
                return DOS.Write(DOS.Output(), buffer,
                    (int)(prefixLength + index + 1)) ==
                    (int)(prefixLength + index + 1);
            }
            APTR.WriteUInt8(buffer, (int)(prefixLength + index), value);
        }
        DOS.SetIoErr(DOS.Error.LineTooLong);
        return false;
    }

    private static bool WriteAliasCategory(uint fileAddress, APTR buffer)
    {
        APTR.WriteUInt8(buffer, 0, (byte)'A');
        APTR.WriteUInt8(buffer, 1, (byte)'L');
        APTR.WriteUInt8(buffer, 2, (byte)'I');
        APTR.WriteUInt8(buffer, 3, (byte)'A');
        APTR.WriteUInt8(buffer, 4, (byte)'S');
        APTR.WriteUInt8(buffer, 5, (byte)' ');
        const uint prefixLength = 6;
        var source = APTR.FromPointer(fileAddress);
        for (var index = 0u; index + prefixLength + 1 < BufferBytes; index++)
        {
            var value = APTR.ReadUInt8(source, (int)index);
            if (value == 0)
            {
                APTR.WriteUInt8(buffer, (int)(prefixLength + index),
                    (byte)'\n');
                return DOS.Write(DOS.Output(), buffer,
                    (int)(prefixLength + index + 1)) ==
                    (int)(prefixLength + index + 1);
            }
            APTR.WriteUInt8(buffer, (int)(prefixLength + index), value);
        }
        DOS.SetIoErr(DOS.Error.LineTooLong);
        return false;
    }

    private static bool WriteLine(APTR source, APTR buffer)
    {
        for (var index = 0u; index + 1 < BufferBytes; index++)
        {
            if (APTR.ReadUInt8(source, (int)index) != 0) continue;
            APTR.WriteUInt8(buffer, (int)index, (byte)'\n');
            return DOS.Write(DOS.Output(), buffer, (int)(index + 1)) ==
                (int)(index + 1);
        }
        DOS.SetIoErr(DOS.Error.LineTooLong);
        return false;
    }
}
