using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Native;

/// <summary>
/// Shared Search body for the MorphOS source profile and Workbench binary
/// syntax candidate. This public-DOS stage traverses AnchorPath directory
/// results, reads files into invocation-owned storage, and emits line markers.
/// The MorphOS profile uses the invocation's default locale for case
/// conversion, line delimiters, and output character classes; it also applies
/// MorphOS soft-link traversal policy.
/// </summary>
public static class NativeMorphOSSearchCommand
{
    public const string Template =
        "FROM/M,SEARCH/A,ALL/S,NONUM/S,QUIET/S,QUICK/S,FILE/S,PATTERN/S,CASE/S,LINES/N";
    public const uint ResultCount = 10;

    private const uint PathBytes = 512;
    private const uint PatternBytes = 1024;
    private const uint UserPatternBytes = 1024;
    private const uint InputBytes = 8192;
    private const uint OutputBytes = 8192;
    private const uint MaximumContiguousFileBytes = 0x7fff0000u;
    private const uint MorphosInitialBufferBytes = 512u * 1024u + 1u;
    private const uint CtrlCMask = 1u << 12;
    private const uint CtrlDMask = 1u << 13;
    private const uint OutputOffset = (uint)DosLayout.AnchorPath.Size +
        PathBytes + PatternBytes + UserPatternBytes + InputBytes + 1;
    public const uint LinkInfoOffset = (OutputOffset + OutputBytes + 3u) & ~3u;
    public const uint LinkWarningArgumentsOffset = LinkInfoOffset +
        (uint)FileInfoBlock.SizeInBytes;
    public const uint LinkBufferOffset = LinkWarningArgumentsOffset + 8u;
    public const uint WorkspaceAllocationBytes = LinkBufferOffset + 512u;

    private struct LineFields
    {
        public uint Number;

        public static APTR AddressOf(ref LineFields fields) =>
            throw new System.NotSupportedException(
                "Search.LineFields.AddressOf is lowered by CopperSharp.");
    }

    private struct MorphosLineState
    {
        public uint LineNumber;
        public uint Following;
        public bool Found;
        public bool EndEarly;
    }

    public static int Run(out int ioError)
        => Run(true, out ioError);

    public static int Run(bool morphosProfile, out int ioError)
    {
        ioError = 0;
        APTR localeLibrary = APTR.Null;
        uint locale = 0;
        if (morphosProfile)
        {
            localeLibrary = Exec.OpenLibraryRaw(Locale.Name, 37);
            if (localeLibrary.IsNull)
                return DOS.RETURN_WARN;
            Locale.LocaleLibraryBase = localeLibrary;
            locale = Locale.OpenLocale(CString.FromPointer(0));
            if (locale == 0)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                Exec.CloseLibrary(localeLibrary);
                DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
                return DOS.RETURN_FAIL;
            }
        }

        var arguments = default(NativeCommandArguments);
        var argumentsParsed = false;
        if (morphosProfile)
            argumentsParsed = NativeCommandArguments.TryRead(Template, ResultCount,
                out arguments);
        else
            argumentsParsed = NativeCommandArguments.TryRead(
                NativeWorkbench31SearchCommand.Template,
                NativeWorkbench31SearchCommand.ResultCount, out arguments);
        if (!argumentsParsed)
        {
            ioError = arguments.IoError;
            var returnLevel = arguments.ReturnLevel;
            arguments.Release();
            if (locale != 0) Locale.CloseLocale(locale);
            if (localeLibrary.IsNotNull) Exec.CloseLibrary(localeLibrary);
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return returnLevel;
        }

        APTR workspace = APTR.Null;
        var output = DOS.Output();
        var result = DOS.RETURN_WARN;
        var error = 0;
        var foundAny = false;
        do
        {
            if (!arguments.TryGetResult(1, out var searchRaw) || searchRaw == 0)
            {
                error = (int)DOS.Error.RequiredArgumentMissing;
                break;
            }

            var all = ReadSwitch(ref arguments, 2) != 0;
            var noNumber = ReadSwitch(ref arguments, 3) != 0;
            var quiet = ReadSwitch(ref arguments, 4) != 0;
            var quick = ReadSwitch(ref arguments, 5) != 0;
            var fileMode = ReadSwitch(ref arguments, 6) != 0;
            var patternMode = ReadSwitch(ref arguments, 7) != 0;
            var caseSensitive = morphosProfile &&
                ReadSwitch(ref arguments, 8) != 0;
            var linesAfter = morphosProfile
                ? ReadNumber(ref arguments, 9) : 0;

            workspace = Exec.AllocMem(WorkspaceAllocationBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (workspace.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }

            var anchor = workspace;
            var path = APTR.FromPointer(anchor.Raw +
                (uint)DosLayout.AnchorPath.Size);
            var pattern = APTR.FromPointer(path.Raw + PathBytes);
            var userPattern = APTR.FromPointer(pattern.Raw + PatternBytes);
            var input = APTR.FromPointer(userPattern.Raw + UserPatternBytes);
            var outputBuffer = APTR.FromPointer(input.Raw + InputBytes + 1);
            var linkInfo = APTR.FromPointer(workspace.Raw + LinkInfoOffset);
            var linkWarningArguments = APTR.FromPointer(workspace.Raw +
                LinkWarningArgumentsOffset);
            var linkBuffer = APTR.FromPointer(workspace.Raw + LinkBufferOffset);
            InitializeAnchor(anchor);

            var search = APTR.FromPointer(searchRaw);
            var searchLength = CStringLength(search, UserPatternBytes - 1);
            if (searchLength == UserPatternBytes - 1)
            {
                error = (int)DOS.Error.LineTooLong;
                break;
            }

            // FILE and PATTERN use DOS's pattern compiler. The ordinary
            // literal path uses locale.library's source-defined case mapping.
            if (fileMode || patternMode)
            {
                var source = userPattern;
                if (patternMode)
                {
                    APTR.WriteUInt8(source, 0, (byte)'#');
                    APTR.WriteUInt8(source, 1, (byte)'?');
                    CopyBytes(search, source, searchLength, 2);
                    APTR.WriteUInt8(source, unchecked((int)(searchLength + 2)),
                        (byte)'#');
                    APTR.WriteUInt8(source, unchecked((int)(searchLength + 3)),
                        (byte)'?');
                    APTR.WriteUInt8(source, unchecked((int)(searchLength + 4)), 0);
                }
                else
                {
                    CopyBytes(search, source, searchLength, 0);
                    APTR.WriteUInt8(source, unchecked((int)searchLength), 0);
                }

                var parsed = caseSensitive
                    ? DOS.ParsePattern(CString.FromPointer(source.Raw), pattern,
                        unchecked((int)PatternBytes))
                    : DOS.ParsePatternNoCase(CString.FromPointer(source.Raw),
                        pattern, unchecked((int)PatternBytes));
                if (parsed < 0)
                {
                    error = (int)DOS.Error.BadTemplate;
                    break;
                }
            }
            else
            {
                CopyBytes(search, pattern, searchLength, 0);
                APTR.WriteUInt8(pattern, unchecked((int)searchLength), 0);
            }

            var from = ReadPointer(ref arguments, 0);
            var fromCount = from.IsNull ? 1u : CountPointers(from, 4096);
            if (fromCount == 0)
            {
                error = (int)DOS.Error.RequiredArgumentMissing;
                break;
            }

            for (var fromIndex = 0u; fromIndex < fromCount; fromIndex++)
            {
                var indentation = 0u;
                var sourceName = from.IsNull
                    ? path
                    : APTR.FromPointer(APTR.ReadUInt32(from,
                        unchecked((int)(fromIndex * 4))));
                if (sourceName.IsNull)
                {
                    error = (int)DOS.Error.RequiredArgumentMissing;
                    break;
                }
                if (from.IsNull) APTR.WriteUInt8(path, 0, 0);

                var match = DOS.MatchFirst(CString.FromPointer(sourceName.Raw),
                    anchor);
                var anchorFlags = APTR.ReadUInt8(anchor,
                    DosLayout.AnchorPath.Flags);
                var printNames = fromCount > 1 ||
                    (anchorFlags & (byte)AnchorPathFlags.IsWild) != 0;
                // The MorphOS source descends into an explicitly named
                // directory even without ALL. Workbench currently follows
                // this common candidate behavior pending a guest comparison.
                if (match == 0 &&
                    (anchorFlags & (byte)AnchorPathFlags.IsWild) == 0 &&
                    FileInfoBlock.GetDirEntryType(APTR.FromPointer(anchor.Raw +
                        (uint)DosLayout.AnchorPath.Info)) > 0)
                {
                    APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                        (byte)(anchorFlags |
                            (byte)AnchorPathFlags.DoDirectory));
                }
                while (match == 0)
                {
                    var fib = APTR.FromPointer(anchor.Raw +
                        (uint)DosLayout.AnchorPath.Info);
                    var entryType = FileInfoBlock.GetDirEntryType(fib);
                    var directory = morphosProfile && entryType ==
                        (int)DosConstants.SoftLink
                        ? NativeMorphOSSearchSoftLinks.ShouldDescend(anchor,
                            linkInfo, linkBuffer, linkWarningArguments,
                            fileMode || quiet || quick)
                        : entryType > 0 ? 1u : 0u;
                    if (directory != 0)
                    {
                        anchorFlags = APTR.ReadUInt8(anchor,
                            DosLayout.AnchorPath.Flags);
                        if ((anchorFlags &
                                (byte)AnchorPathFlags.DidDirectory) != 0)
                        {
                            if (indentation != 0) indentation--;
                            APTR.WriteUInt8(anchor,
                                DosLayout.AnchorPath.Flags,
                                (byte)(anchorFlags &
                                    ~(byte)AnchorPathFlags.DidDirectory));
                        }
                        else
                        {
                            if (!fileMode && !quiet && !quick &&
                                !WriteDirectoryHeading(fib, indentation,
                                    out error)) break;
                            if (all || (anchorFlags &
                                    (byte)AnchorPathFlags.DoDirectory) != 0)
                            {
                                APTR.WriteUInt8(anchor,
                                    DosLayout.AnchorPath.Flags,
                                    (byte)(anchorFlags |
                                        (byte)AnchorPathFlags.DoDirectory));
                                indentation++;
                                printNames = true;
                            }
                        }
                    }
                    else
                    {
                        var changed = (APTR.ReadUInt8(anchor,
                            DosLayout.AnchorPath.Flags) &
                            (byte)AnchorPathFlags.DirectoryChanged) != 0;
                        _ = changed; // MatchNext owns the source path buffer.
                        var fileName = CString.FromPointer(fib.Raw +
                            (uint)FileInfoBlock.FileNameOffset);
                        var selected = fileMode
                            ? (caseSensitive
                                ? DOS.MatchPattern(
                                    CString.FromPointer(pattern.Raw), fileName)
                                : DOS.MatchPatternNoCase(
                                    CString.FromPointer(pattern.Raw), fileName)) != 0
                            : true;
                        if (selected && !TryBuildMatchedPath(anchor, path,
                                fileName, out error)) break;
                        var fullPath = CString.FromPointer(path.Raw);
                        if (selected && fileMode)
                        {
                            if (!WriteCString(output, fullPath, outputBuffer,
                                    OutputBytes, out error)) break;
                            foundAny = true;
                        }
                        else if (selected)
                        {
                            if (!quiet && !quick && printNames &&
                                !WriteFileHeading(fileName, indentation,
                                    out error)) break;
                            var morphosQuick = morphosProfile && quick;
                            if (morphosQuick && !WriteQuickPathPrefix(output,
                                    outputBuffer, OutputBytes, fullPath,
                                    out error)) break;
                            var opened = DOS.OpenRaw(
                                fullPath,
                                DOS.FileMode.OldFile);
                            if (opened.IsNull)
                            {
                                if (!morphosProfile)
                                {
                                    error = (int)DOS.IoErr();
                                    break;
                                }
                                // MorphOS 3.20's FindString helper returns its
                                // found state after an Open failure. The outer
                                // MatchNext loop continues and clears IoErr on
                                // successful traversal.
                            }
                            else if (morphosProfile)
                            {
                                var completed = SearchMorphosFile(opened, fib, output,
                                        outputBuffer, pattern, locale,
                                        patternMode, caseSensitive, noNumber,
                                        quiet, morphosQuick, linesAfter,
                                        fullPath, out var fileFound,
                                        out _, out var fileIoError);
                                // FindString treats its internal Open/Read/Seek
                                // exits as a per-file miss. Only its observed
                                // Ctrl-C outcome reaches the command-level
                                // failure path; MatchNext/SetIoErr determines
                                // outer traversal status.
                                if (!completed && fileIoError ==
                                        (int)DOS.Error.Break)
                                {
                                    error = fileIoError;
                                    break;
                                }
                                if (fileFound) foundAny = true;
                            }
                            else
                            {
                                var bytes = ReadSearchFile(opened, fib, input,
                                    false, out var fileInput,
                                    out var fileInputBytes,
                                    out var abandonFile, out error);
                                if (error != 0) break;
                                if (!abandonFile)
                                {
                                    var matched = FormatLines(output,
                                        outputBuffer, OutputBytes, fileInput,
                                        unchecked((uint)bytes), pattern, false,
                                        locale, fileMode, patternMode,
                                        caseSensitive, noNumber, quiet,
                                        morphosQuick, linesAfter, fullPath,
                                        out var fileFound, out error);
                                    if (fileInputBytes != 0)
                                    {
                                        Exec.FreeMem(fileInput,
                                            fileInputBytes);
                                        fileInputBytes = 0;
                                    }
                                    if (!matched) break;
                                    if (fileFound) foundAny = true;
                                }
                                if (abandonFile && fileInputBytes != 0)
                                {
                                    Exec.FreeMem(fileInput, fileInputBytes);
                                    fileInputBytes = 0;
                                }
                            }
                        }
                    }

                    if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                    {
                        error = (int)DOS.Error.Break;
                        break;
                    }
                    match = DOS.MatchNext(anchor);
                }
                var matchError = (int)DOS.IoErr();
                DOS.MatchEnd(anchor);
                if (error != 0) break;
                if (match != 0 && match != (int)DOS.Error.NoMoreEntries)
                {
                    error = matchError != 0 ? matchError : match;
                    break;
                }
            }
            if (morphosProfile && quick && !fileMode &&
                !WriteQuickLineClear(output, outputBuffer, OutputBytes,
                    out var quickClearError) && error == 0)
                error = quickClearError;
            if (error == 0) result = foundAny ? DOS.RETURN_OK : DOS.RETURN_WARN;
        }
        while (false);

        if (workspace.IsNotNull) Exec.FreeMem(workspace, WorkspaceAllocationBytes);
        arguments.Release();
        if (locale != 0) Locale.CloseLocale(locale);
        if (localeLibrary.IsNotNull) Exec.CloseLibrary(localeLibrary);
        ioError = error;
        DOS.SetIoErr((DOS.Error)error);
        if (error != 0)
        {
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }
        return result;
    }

    private static bool FormatLines(BPTR output, APTR destination, uint capacity,
        APTR source, uint sourceLength, APTR pattern, bool useLocale,
        uint locale, bool fileMode, bool patternMode, bool caseSensitive,
        bool noNumber, bool quiet,
        bool quick, uint linesAfter, CString fullPath, out bool found,
        out int ioError)
    {
        found = false;
        ioError = 0;
        var offset = 0u;
        var lineNumber = 1u;
        var following = 0u;
        while (offset < sourceLength)
        {
            var start = offset;
            var localeControlDelimiter = false;
            while (offset < sourceLength)
            {
                var value = APTR.ReadUInt8(source, unchecked((int)offset++));
                localeControlDelimiter = useLocale &&
                    value != (byte)'\n' && value != 0 &&
                    value != (byte)'\t' &&
                    Locale.IsCntrl(locale, value) != 0;
                if (value == (byte)'\n' || value == 0 ||
                    localeControlDelimiter) break;
            }
            var length = offset - start;
            var delimiter = (byte)0;
            var hasDelimiter = offset > start && offset <= sourceLength &&
                APTR.ReadUInt8(source, unchecked((int)(offset - 1))) == (byte)'\n';
            if (hasDelimiter)
            {
                delimiter = (byte)'\n';
                length--;
            }
            else if (useLocale && offset > start && offset <= sourceLength)
            {
                delimiter = APTR.ReadUInt8(source,
                    unchecked((int)(offset - 1)));
                if (delimiter == 0 || localeControlDelimiter)
                    length--;
                else
                    delimiter = 0;
            }
            var outputLineNumber = lineNumber;
            if (!useLocale || (offset > start &&
                APTR.ReadUInt8(source, unchecked((int)(offset - 1))) ==
                    (byte)'\n'))
                lineNumber++;
            var line = APTR.FromPointer(source.Raw + start);
            if (patternMode)
                APTR.WriteUInt8(source, unchecked((int)(start + length)), 0);
            var matches = patternMode
                ? (caseSensitive
                    ? DOS.MatchPattern(CString.FromPointer(pattern.Raw),
                        CString.FromPointer(line.Raw))
                    : DOS.MatchPatternNoCase(CString.FromPointer(pattern.Raw),
                        CString.FromPointer(line.Raw))) != 0
                : LiteralContains(line, length, pattern, caseSensitive,
                    useLocale, locale);
            if (patternMode && (hasDelimiter || delimiter != 0))
                APTR.WriteUInt8(source, unchecked((int)(start + length)), delimiter);
            if (matches)
            {
                if (quick && !found && !WriteByte(output, destination,
                        capacity, (byte)'\n', out ioError)) return false;
                found = true;
                if (quiet)
                    return quick || WritePath(output, destination, capacity,
                        fullPath, out ioError);
                following = linesAfter;
                if (!WriteLine(destination, capacity, output, line, length,
                        outputLineNumber, noNumber, (byte)'>', useLocale,
                        locale, out ioError)) return false;
            }
            else if (following != 0)
            {
                following--;
                if (!WriteLine(destination, capacity, output, line, length,
                        outputLineNumber, noNumber, (byte)':', useLocale,
                        locale, out ioError)) return false;
            }
        }
        return true;
    }

    private static bool SearchMorphosFile(BPTR file, APTR fib, BPTR output,
        APTR destination, APTR pattern, uint locale, bool patternMode,
        bool caseSensitive, bool noNumber, bool quiet, bool quick,
        uint linesAfter, CString fullPath, out bool found,
        out bool abandonFile, out int ioError)
    {
        found = false;
        abandonFile = false;
        ioError = 0;
        var dosVersion = APTR.ReadUInt16(DOS.DOSLibraryBase,
            ExecLayout.Library.Version);
        var dosRevision = APTR.ReadUInt16(DOS.DOSLibraryBase,
            ExecLayout.Library.Revision);
        var useDos64 = dosVersion > 51 ||
            dosVersion == 51 && dosRevision >= 28;
        var fileSizeHigh = useDos64
            ? APTR.ReadUInt32(fib, FileInfoBlock.Size64Offset) : 0u;
        var fileSizeLow = useDos64
            ? APTR.ReadUInt32(fib, FileInfoBlock.Size64Offset + 4)
            : APTR.ReadUInt32(fib, FileInfoBlock.SizeOffset);
        var useSeek64 = useDos64 && (fileSizeHigh != 0 ||
            fileSizeLow > 0x7fff_ffffu);
        var bufferBytes = fileSizeHigh != 0 ||
            fileSizeLow >= MorphosInitialBufferBytes - 1u
                ? MorphosInitialBufferBytes : fileSizeLow + 1u;
        if (bufferBytes > MaximumContiguousFileBytes)
            bufferBytes = MaximumContiguousFileBytes;

        var buffer = APTR.Null;
        while (buffer.IsNull && bufferBytes != 0)
        {
            buffer = Exec.AllocMem(bufferBytes, Exec.MemoryFlags.Any);
            if (buffer.IsNull) bufferBytes >>= 1;
        }

        var fileLargerThanBuffer = fileSizeHigh != 0 ||
            bufferBytes <= fileSizeLow;
        var maxLineLength = 0u;
        if (!buffer.IsNull && fileLargerThanBuffer)
        {
            var currentLineLength = 0u;
            while (true)
            {
                var count = DOS.Read(file, buffer,
                    unchecked((int)(bufferBytes - 1u)));
                if (count < 0)
                {
                    ioError = (int)DOS.IoErr();
                    break;
                }
                if ((uint)count > bufferBytes - 1u)
                {
                    ioError = (int)DOS.Error.LineTooLong;
                    break;
                }
                for (var i = 0; i < count; i++)
                {
                    if (APTR.ReadUInt8(buffer, i) == (byte)'\n')
                    {
                        if (currentLineLength > maxLineLength)
                            maxLineLength = currentLineLength;
                        currentLineLength = 0;
                    }
                    else if (currentLineLength <= MaximumContiguousFileBytes)
                    {
                        currentLineLength++;
                    }
                }

                var signals = Exec.SetSignal(0u, CtrlDMask);
                if ((signals & (CtrlCMask | CtrlDMask)) != 0)
                {
                    if ((signals & CtrlDMask) != 0)
                    {
                        DOS.PutStr("** File abandoned\n");
                        abandonFile = true;
                    }
                    if ((signals & CtrlCMask) != 0)
                        ioError = (int)DOS.Error.Break;
                    break;
                }
                if (count == 0) break;
            }
            if (currentLineLength > maxLineLength)
                maxLineLength = currentLineLength;

            if (ioError == 0 && !abandonFile &&
                maxLineLength >= bufferBytes)
            {
                Exec.FreeMem(buffer, bufferBytes);
                buffer = APTR.Null;
                if (maxLineLength <= MaximumContiguousFileBytes)
                {
                    bufferBytes = maxLineLength + 1u;
                    buffer = Exec.AllocMem(bufferBytes,
                        Exec.MemoryFlags.Any);
                }
            }
        }

        if (ioError == 0 && !abandonFile && buffer.IsNotNull &&
            pattern.IsNotNull)
        {
            if (useSeek64)
            {
                var previousLow = M68kRuntime.SplitInt64(
                    DOS.Seek64(file, 0L,
                        (int)DosConstants.OffsetBeginning),
                    out var previousHigh);
                if (previousHigh == uint.MaxValue &&
                    previousLow == uint.MaxValue)
                    ioError = (int)DOS.IoErr();
            }
            else if (DOS.Seek(file, 0,
                         (int)DosConstants.OffsetBeginning) == -1)
            {
                ioError = (int)DOS.IoErr();
            }

            var lineState = new MorphosLineState { LineNumber = 1 };
            var filePositionHigh = 0u;
            var filePositionLow = 0u;
            while (ioError == 0 && !lineState.EndEarly)
            {
                var count = DOS.Read(file, buffer,
                    unchecked((int)(bufferBytes - 1u)));
                if (count < 0)
                {
                    ioError = (int)DOS.IoErr();
                    break;
                }
                if ((uint)count > bufferBytes - 1u)
                {
                    ioError = (int)DOS.Error.LineTooLong;
                    break;
                }
                if (count == 0) break;

                var previousPositionLow = filePositionLow;
                filePositionLow += unchecked((uint)count);
                if (filePositionLow < previousPositionLow)
                    filePositionHigh++;
                var atEnd = filePositionHigh == fileSizeHigh &&
                    filePositionLow == fileSizeLow;

                var sourceLength = unchecked((uint)count);
                if (atEnd)
                {
                    APTR.WriteUInt8(buffer, count, 0);
                    sourceLength++;
                }
                if (!FormatMorphosChunk(output, destination, OutputBytes,
                        buffer, sourceLength, pattern, locale, patternMode,
                        caseSensitive, noNumber, quiet, quick, linesAfter,
                        fullPath, ref lineState, out var trailingLineOffset,
                        out ioError))
                    break;

                var signals = Exec.SetSignal(0u, CtrlDMask);
                if ((signals & (CtrlCMask | CtrlDMask)) != 0)
                {
                    lineState.EndEarly = true;
                    if ((signals & CtrlDMask) != 0)
                    {
                        DOS.PutStr("** File abandoned\n");
                        abandonFile = true;
                    }
                    if ((signals & CtrlCMask) != 0)
                        ioError = (int)DOS.Error.Break;
                }

                if (!atEnd)
                {
                    var rewind = unchecked((int)trailingLineOffset - count);
                    var seekFailed = false;
                    if (useSeek64)
                    {
                        var previousLow = M68kRuntime.SplitInt64(DOS.Seek64(file,
                            unchecked((long)rewind),
                            (int)DosConstants.OffsetCurrent),
                            out var previousHigh);
                        seekFailed = previousHigh == uint.MaxValue &&
                            previousLow == uint.MaxValue;
                    }
                    else
                    {
                        seekFailed = DOS.Seek(file, rewind,
                            (int)DosConstants.OffsetCurrent) == -1;
                    }
                    if (seekFailed)
                        ioError = (int)DOS.IoErr();
                    else
                    {
                        var rewindBytes = unchecked((uint)(-rewind));
                        if (filePositionLow < rewindBytes)
                            filePositionHigh--;
                        filePositionLow -= rewindBytes;
                    }
                }
            }
            found = lineState.Found;
        }

        if (buffer.IsNotNull) Exec.FreeMem(buffer, bufferBytes);
        DOS.Close(file);
        return ioError == 0;
    }

    private static bool FormatMorphosChunk(BPTR output, APTR destination,
        uint capacity, APTR source, uint sourceLength, APTR pattern,
        uint locale, bool patternMode, bool caseSensitive, bool noNumber,
        bool quiet, bool quick, uint linesAfter, CString fullPath,
        ref MorphosLineState state, out uint trailingLineOffset,
        out int ioError)
    {
        trailingLineOffset = 0;
        ioError = 0;
        var offset = 0u;
        while (offset < sourceLength && !state.EndEarly)
        {
            var start = offset;
            var localeControlDelimiter = false;
            while (offset < sourceLength)
            {
                var value = APTR.ReadUInt8(source, unchecked((int)offset++));
                localeControlDelimiter = value != (byte)'\n' && value != 0 &&
                    value != (byte)'\t' && Locale.IsCntrl(locale, value) != 0;
                if (value == (byte)'\n' || value == 0 ||
                    localeControlDelimiter) break;
            }
            var hasDelimiter = offset > start &&
                (APTR.ReadUInt8(source, unchecked((int)(offset - 1))) ==
                    (byte)'\n' ||
                 APTR.ReadUInt8(source, unchecked((int)(offset - 1))) == 0 ||
                 localeControlDelimiter);
            if (!hasDelimiter)
            {
                trailingLineOffset = start;
                break;
            }
            var delimiter = hasDelimiter
                ? APTR.ReadUInt8(source, unchecked((int)(offset - 1)))
                : (byte)0;
            var length = offset - start - (hasDelimiter ? 1u : 0u);
            var lineNumber = state.LineNumber;
            var line = APTR.FromPointer(source.Raw + start);
            if (patternMode)
                APTR.WriteUInt8(source, unchecked((int)(start + length)), 0);
            var matches = patternMode
                ? (caseSensitive
                    ? DOS.MatchPattern(CString.FromPointer(pattern.Raw),
                        CString.FromPointer(line.Raw))
                    : DOS.MatchPatternNoCase(
                        CString.FromPointer(pattern.Raw),
                        CString.FromPointer(line.Raw))) != 0
                : LiteralContains(line, length, pattern, caseSensitive, true,
                    locale);
            if (matches)
            {
                if (quick && !state.Found && !WriteByte(output, destination,
                        capacity, (byte)'\n', out ioError)) return false;
                state.Found = true;
                if (quiet)
                {
                    state.EndEarly = true;
                    if (!quick && !WritePath(output, destination, capacity,
                            fullPath, out ioError)) return false;
                }
                else if (!WriteLine(destination, capacity, output, line,
                             length, lineNumber, noNumber, (byte)'>', true,
                             locale, out ioError))
                {
                    return false;
                }
                if (!quiet) state.Following = linesAfter;
            }
            else if (state.Following != 0)
            {
                state.Following--;
                if (!WriteLine(destination, capacity, output, line, length,
                        lineNumber, noNumber, (byte)':', true, locale,
                        out ioError)) return false;
            }

            if (patternMode && hasDelimiter)
                APTR.WriteUInt8(source, unchecked((int)(start + length)),
                    delimiter);
            trailingLineOffset = offset;
            if (delimiter == (byte)'\n') state.LineNumber++;
        }
        return true;
    }

    private static bool WriteLine(APTR destination, uint capacity, BPTR output,
        APTR line, uint length, uint lineNumber, bool noNumber, byte marker,
        bool useLocale, uint locale, out int ioError)
    {
        ioError = 0;
        var used = 0u;
        if (!noNumber)
        {
            var fields = default(LineFields);
            fields.Number = lineNumber;
            if (marker == (byte)'>')
            {
                DOS.VPrintf("%6ld", LineFields.AddressOf(ref fields));
                DOS.FPuts(DOS.Output(), "> ");
            }
            else
            {
                DOS.VPrintf("%6ld", LineFields.AddressOf(ref fields));
                DOS.FPuts(DOS.Output(), ": ");
            }
        }
        for (var i = 0u; i < length; i++)
        {
            if (used == capacity &&
                !WriteBuffer(output, destination, used, out ioError))
                return false;
            if (used == capacity) used = 0;
            var value = APTR.ReadUInt8(line, unchecked((int)i));
            var printable = useLocale
                ? Locale.IsPrint(locale, value) != 0
                : value >= 0x20 && value <= 0x7e;
            APTR.WriteUInt8(destination, unchecked((int)used++),
                printable ? value : (byte)'.');
        }
        if (used != 0 && !WriteBuffer(output, destination, used, out ioError))
            return false;
        APTR.WriteUInt8(destination, 0, (byte)'\n');
        return WriteBuffer(output, destination, 1, out ioError);
    }

    private static uint ReadFileSize(APTR fib, bool morphosProfile)
    {
        if (!morphosProfile)
            return unchecked((uint)FileInfoBlock.GetSize(fib.Raw));
        var extendedHigh = APTR.ReadUInt32(fib, FileInfoBlock.Size64Offset);
        var extendedLow = APTR.ReadUInt32(fib,
            FileInfoBlock.Size64Offset + 4);
        if (extendedHigh != 0) return uint.MaxValue;
        return extendedLow != 0 ? extendedLow :
            unchecked((uint)FileInfoBlock.GetSize(fib.Raw));
    }

    private static int ReadSearchFile(BPTR file, APTR fib, APTR input,
        bool morphosProfile, out APTR fileInput, out uint fileInputBytes,
        out bool abandonFile, out int ioError)
    {
        fileInput = input;
        fileInputBytes = 0;
        abandonFile = false;
        ioError = 0;
        var fileBytes = ReadFileSize(fib, morphosProfile);
        if (fileBytes > MaximumContiguousFileBytes)
        {
            DOS.Close(file);
            ioError = (int)DOS.Error.LineTooLong;
            return -1;
        }
        if (fileBytes >= InputBytes)
        {
            fileInputBytes = fileBytes + 1u;
            fileInput = Exec.AllocMem(fileInputBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (fileInput.IsNull)
            {
                DOS.Close(file);
                ioError = (int)DOS.Error.NoFreeStore;
                fileInputBytes = 0;
                return -1;
            }
        }
        var bytesRead = 0u;
        var probeEmptyFile = fileBytes == 0;
        while (probeEmptyFile || bytesRead < fileBytes)
        {
            var readCapacity = probeEmptyFile ? InputBytes :
                fileBytes - bytesRead;
            var destination = APTR.FromPointer(fileInput.Raw + bytesRead);
            var count = DOS.Read(file, destination,
                unchecked((int)readCapacity));
            if (count < 0)
            {
                ioError = (int)DOS.IoErr();
                break;
            }
            if ((uint)count > readCapacity)
            {
                ioError = (int)DOS.Error.LineTooLong;
                break;
            }
            if (count > 0) bytesRead += unchecked((uint)count);
            if (morphosProfile)
            {
                var fileSignals = Exec.SetSignal(0u, CtrlDMask);
                if ((fileSignals & CtrlDMask) != 0)
                {
                    DOS.PutStr("** File abandoned\n");
                    abandonFile = true;
                }
                if ((fileSignals & CtrlCMask) != 0)
                    ioError = (int)DOS.Error.Break;
            }
            if (ioError != 0 || abandonFile || count == 0 ||
                probeEmptyFile) break;
        }
        DOS.Close(file);
        if (ioError != 0)
        {
            if (fileInputBytes != 0)
            {
                Exec.FreeMem(fileInput, fileInputBytes);
                fileInputBytes = 0;
            }
            return -1;
        }
        if (!abandonFile && bytesRead == InputBytes && fileInputBytes == 0)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return -1;
        }
        if (!abandonFile) APTR.WriteUInt8(fileInput, unchecked((int)bytesRead), 0);
        return unchecked((int)bytesRead);
    }

    private static bool WritePath(BPTR output, APTR destination, uint capacity,
        CString path, out int ioError)
    {
        var length = CStringLength(APTR.FromPointer(CString.ToUInt32(path)),
            capacity - 1);
        if (length >= capacity - 1)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }
        CopyBytes(APTR.FromPointer(CString.ToUInt32(path)), destination, length, 0);
        APTR.WriteUInt8(destination, unchecked((int)length), (byte)'\n');
        return WriteBuffer(output, destination, length + 1, out ioError);
    }

    private static bool WriteCString(BPTR output, CString path, APTR destination,
        uint capacity, out int ioError) => WritePath(output, destination, capacity,
            path, out ioError);

    private static bool WriteQuickPathPrefix(BPTR output, APTR destination,
        uint capacity, CString path, out int ioError)
    {
        ioError = 0;
        if (capacity < 3)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }
        var source = APTR.FromPointer(CString.ToUInt32(path));
        var length = CStringLength(source, PathBytes);
        if (length >= PathBytes || length > capacity - 3)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }
        CopyBytes(source, destination, length, 0);
        APTR.WriteUInt8(destination, unchecked((int)length), 0x9b);
        APTR.WriteUInt8(destination, unchecked((int)(length + 1)), (byte)'K');
        APTR.WriteUInt8(destination, unchecked((int)(length + 2)), (byte)'\r');
        return WriteBuffer(output, destination, length + 3, out ioError);
    }

    private static bool WriteQuickLineClear(BPTR output, APTR destination,
        uint capacity, out int ioError)
    {
        if (capacity < 2)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }
        APTR.WriteUInt8(destination, 0, 0x9b);
        APTR.WriteUInt8(destination, 1, (byte)'K');
        return WriteBuffer(output, destination, 2, out ioError);
    }

    private static bool WriteDirectoryHeading(APTR fib, uint indentation,
        out int ioError)
    {
        ioError = 0;
        for (var index = 0u; index < 5u + 5u * indentation; index++)
            if (DOS.PutStr(" ") < 0) { ioError = (int)DOS.IoErr(); return false; }
        if (DOS.PutStr(CString.FromPointer(fib.Raw +
                (uint)FileInfoBlock.FileNameOffset)) < 0 ||
            DOS.PutStr(" (dir)\n") < 0)
        {
            ioError = (int)DOS.IoErr();
            return false;
        }
        return true;
    }

    private static bool TryBuildMatchedPath(APTR anchor, APTR path,
        CString fileName, out int ioError)
    {
        ioError = 0;
        var current = APTR.FromPointer(APTR.ReadUInt32(anchor,
            DosLayout.AnchorPath.Current));
        if (current.IsNull)
        {
            ioError = (int)DOS.Error.ObjectNotFound;
            return false;
        }
        var directoryLock = BPTR.FromRaw(APTR.ReadUInt32(current,
            DosLayout.AChain.Lock));
        if (directoryLock.Raw == 0)
        {
            ioError = (int)DOS.Error.ObjectNotFound;
            return false;
        }
        if (DOS.NameFromLock(directoryLock, path,
                unchecked((int)PathBytes)) == 0 ||
            DOS.AddPart(CString.FromPointer(path.Raw), fileName, PathBytes) == 0)
        {
            ioError = (int)DOS.IoErr();
            return false;
        }
        return true;
    }

    private static bool WriteFileHeading(CString name, uint indentation,
        out int ioError)
    {
        ioError = 0;
        for (var index = 0u; index < 3u + 5u * indentation; index++)
            if (DOS.PutStr(" ") < 0) { ioError = (int)DOS.IoErr(); return false; }
        if (DOS.PutStr(name) < 0 || DOS.PutStr("..\n") < 0)
        {
            ioError = (int)DOS.IoErr();
            return false;
        }
        return true;
    }

    private static bool WriteByte(BPTR output, APTR destination, uint capacity,
        byte value, out int ioError)
    {
        if (capacity == 0) { ioError = (int)DOS.Error.LineTooLong; return false; }
        APTR.WriteUInt8(destination, 0, value);
        return WriteBuffer(output, destination, 1, out ioError);
    }

    private static bool WriteBuffer(BPTR output, APTR buffer, uint length,
        out int ioError)
    {
        var offset = 0u;
        while (offset < length)
        {
            var written = DOS.Write(output,
                APTR.FromPointer(buffer.Raw + offset),
                unchecked((int)(length - offset)));
            if (written <= 0 || (uint)written > length - offset)
            {
                ioError = (int)DOS.IoErr();
                return false;
            }
            offset += (uint)written;
        }
        ioError = 0;
        return true;
    }

    private static bool LiteralContains(APTR text, uint length, APTR pattern,
        bool caseSensitive, bool useLocale, uint locale)
    {
        var patternLength = CStringLength(pattern, PatternBytes - 1);
        if (patternLength == 0 || patternLength > length) return false;
        for (var start = 0u; start <= length - patternLength; start++)
        {
            var matched = true;
            for (var index = 0u; index < patternLength; index++)
            {
                var left = APTR.ReadUInt8(text, unchecked((int)(start + index)));
                var right = APTR.ReadUInt8(pattern, unchecked((int)index));
                if (!caseSensitive)
                {
                    left = useLocale ? LocaleUpper(locale, left) : Fold(left);
                    right = useLocale ? LocaleUpper(locale, right) : Fold(right);
                }
                if (left == right) continue;
                matched = false;
                break;
            }
            if (matched) return true;
        }
        return false;
    }

    private static byte Fold(byte value) => value >= (byte)'a' &&
        value <= (byte)'z' ? (byte)(value - ('a' - 'A')) : value;

    private static byte LocaleUpper(uint locale, byte value) =>
        unchecked((byte)Locale.ConvToUpper(locale, value));

    private static uint CStringLength(APTR value, uint limit)
    {
        for (var index = 0u; index < limit; index++)
            if (APTR.ReadUInt8(value, unchecked((int)index)) == 0) return index;
        return limit;
    }

    private static void CopyBytes(APTR source, APTR destination, uint length,
        uint destinationOffset)
    {
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(destination, unchecked((int)(destinationOffset + index)),
                APTR.ReadUInt8(source, unchecked((int)index)));
    }

    private static uint CountPointers(APTR values, uint limit)
    {
        var count = 0u;
        while (count < limit && APTR.ReadUInt32(values,
                   unchecked((int)(count * 4))) != 0) count++;
        return count;
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
            unchecked((ushort)PathBytes));
    }

    // The lease is passed by ref: it is a 24-byte struct, and a by-value
    // parameter makes the compiler copy it onto the stack for every call.
    private static uint ReadSwitch(ref NativeCommandArguments arguments, uint index) =>
        arguments.TryGetResult(index, out var value) ? value : 0;

    private static uint ReadNumber(ref NativeCommandArguments arguments, uint index)
    {
        if (!arguments.TryGetResult(index, out var value) || value == 0) return 0;
        return APTR.ReadUInt32(APTR.FromPointer(value), 0);
    }

    private static APTR ReadPointer(ref NativeCommandArguments arguments, uint index) =>
        arguments.TryGetResult(index, out var value) ? APTR.FromPointer(value) : APTR.Null;
}
