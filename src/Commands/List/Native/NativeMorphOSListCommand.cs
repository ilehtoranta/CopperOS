using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-bound List frontend.  The directory matcher and FileInfoBlock
/// remain DOS-owned; this body only snapshots the fields needed by the bounded
/// output modes before releasing the matcher context. Recursive traversal uses
/// the public AnchorPath APF_DODIR/APF_DIDDIR protocol. Extended formatting,
/// sorting, owners and exact profile parity remain provider follow-up work.
/// </summary>
public static class NativeMorphOSListCommand
{
    public const string Template =
        "DIR/M,P=PAT/K,KEYS/S,DATES/S,NODATES/S,TO/K,SUB/K,SINCE/K,UPTO/K,QUICK/S,BLOCK/S,NOHEAD/S,FILES/S,DIRS/S,LFORMAT/K,SORT/K,USERS/S,GROUPS/S,ALL/S";
    public const uint ResultCount = 19;

    private const uint PathBytes = 512;
    private const uint PatternBytes = 512;
    private const uint DateTextBytes = 16;
    private const uint ProtectionBytes = 9;
    private const uint CommentBytes = 82;
    private const uint DateTimeOffset = (uint)DosLayout.AnchorPath.Size +
        PathBytes + PatternBytes * 3u + (uint)FileInfoBlock.SizeInBytes;
    private const uint DayTextOffset = DateTimeOffset + DosDateTime.Size;
    private const uint DateTextOffset = DayTextOffset + DateTextBytes;
    private const uint TimeTextOffset = DateTextOffset + DateTextBytes;
    private const uint ProtectionTextOffset = TimeTextOffset + DateTextBytes;
    private const uint CommentTextOffset = ProtectionTextOffset + ProtectionBytes;
    public const uint WorkspaceAllocationBytes = (uint)DosLayout.AnchorPath.Size +
        PathBytes + PatternBytes * 3u + (uint)FileInfoBlock.SizeInBytes +
        DosDateTime.Size + DateTextBytes * 3u + ProtectionBytes + CommentBytes;
    private const uint CtrlCMask = 1u << 12;
    private const byte DoDirectory = (byte)AnchorPathFlags.DoDirectory;
    private const byte DidDirectory = (byte)AnchorPathFlags.DidDirectory;

    private struct QuickFields
    {
        public uint Name;

        public static APTR AddressOf(ref QuickFields fields) =>
            throw new System.NotSupportedException(
                "List.QuickFields.AddressOf is lowered by CopperSharp.");
    }

    private struct LongFields
    {
        public uint Name;
        public uint Size;
        public uint Key;
        public uint Protection;
        public uint Date;
        public uint Time;
        public uint Comment;

        public static APTR AddressOf(ref LongFields fields) =>
            throw new System.NotSupportedException(
                "List.LongFields.AddressOf is lowered by CopperSharp.");
    }

    private struct DefaultFields
    {
        public uint Name;
        public uint Size;
        public uint Protection;
        public uint Date;
        public uint Time;
        public uint Comment;

        public static APTR AddressOf(ref DefaultFields fields) =>
            throw new System.NotSupportedException(
                "List.DefaultFields.AddressOf is lowered by CopperSharp.");
    }

    private struct LongNoDateFields
    {
        public uint Name;
        public uint Size;
        public uint Key;
        public uint Protection;
        public uint Comment;

        public static APTR AddressOf(ref LongNoDateFields fields) =>
            throw new System.NotSupportedException(
                "List.LongNoDateFields.AddressOf is lowered by CopperSharp.");
    }

    private struct DefaultNoDateFields
    {
        public uint Name;
        public uint Size;
        public uint Protection;
        public uint Comment;

        public static APTR AddressOf(ref DefaultNoDateFields fields) =>
            throw new System.NotSupportedException(
                "List.DefaultNoDateFields.AddressOf is lowered by CopperSharp.");
    }

    /// <summary>
    /// Implements the public DOS matcher/FIB path for name, metadata and
    /// recursive modes. Unsupported provider modes fail closed before output
    /// mutation or host-side substitution is attempted.
    /// </summary>
    public static int Run(out int ioError)
        => Run(Template, ResultCount, out ioError);

    /// <summary>Runs the bounded public-DOS List path with a profile template.</summary>
    public static int Run(CString template, uint resultCount, out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(template, resultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return arguments.ReturnLevel;
        }

        var workspace = APTR.Null;
        var output = BPTR.Null;
        var previousOutput = BPTR.Null;
        var selectedOutput = false;
        var closeOutput = false;
        var result = DOS.RETURN_WARN;
        var error = 0;
        var matched = 0u;
        do
        {
            var names = ReadPointer(ref arguments, 0);
            if (names.IsNull || APTR.ReadUInt32(names, 0) == 0)
            {
                error = (int)DOS.Error.RequiredArgumentMissing;
                break;
            }

            var pat = ReadPointer(ref arguments, 1);
            var keys = ReadSwitch(ref arguments, 2) != 0;
            var noDates = ReadSwitch(ref arguments, 4) != 0;
            var to = ReadPointer(ref arguments, 5);
            var sub = ReadPointer(ref arguments, 6);
            var since = ReadPointer(ref arguments, 7);
            var upto = ReadPointer(ref arguments, 8);
            var quick = ReadSwitch(ref arguments, 9) != 0;
            var block = ReadSwitch(ref arguments, 10) != 0;
            var noHead = ReadSwitch(ref arguments, 11) != 0;
            var files = ReadSwitch(ref arguments, 12) != 0;
            var dirs = ReadSwitch(ref arguments, 13) != 0;
#if COPPEROS_WORKBENCH31_LIST
            // The shipping entry always uses the classic sixteen-slot template.
            const bool workbench31 = true;
#else
            var workbench31 = resultCount == NativeWorkbench31ListCommand.ResultCount;
#endif
            var lformat = ReadPointer(ref arguments, 14);
            var sort = workbench31 ? APTR.Null : ReadPointer(ref arguments, 15);
            var users = !workbench31 && ReadSwitch(ref arguments, 16) != 0;
            var groups = !workbench31 && ReadSwitch(ref arguments, 17) != 0;
            var all = ReadSwitch(ref arguments, workbench31 ? 15u : 18u) != 0;

            // QUICK explicitly selects name-only rows, including when DATES
            // is also supplied; the Workbench manual says DATES is the default
            // unless QUICK is used. Date filters use public DOS StrToDate.
            // ALL recurses through the public AnchorPath protocol below;
            // sorting, owners and custom formatting remain fail-closed.
            if (lformat.IsNotNull || sort.IsNotNull || users || groups)
            {
                error = (int)DOS.Error.NotImplemented;
                break;
            }

            workspace = Exec.AllocMem(WorkspaceAllocationBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (workspace.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }

            var dateTime = APTR.FromPointer(workspace.Raw + DateTimeOffset);
            var sinceDays = 0u;
            var uptoDays = 0u;
            if ((since.IsNotNull && !TryParseDate(since, dateTime,
                    out sinceDays)) ||
                (upto.IsNotNull && !TryParseDate(upto, dateTime,
                    out uptoDays)))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            if (to.IsNotNull)
            {
                output = DOS.OpenRaw(CString.FromPointer(to), DOS.FileMode.NewFile);
                if (output.IsNull)
                {
                    error = (int)DOS.IoErr();
                    break;
                }
                previousOutput = DOS.SelectOutput(output);
                selectedOutput = true;
                closeOutput = true;
            }

            var anchor = workspace;
            var patternBuffer = APTR.FromPointer(workspace.Raw +
                (uint)DosLayout.AnchorPath.Size + PathBytes);
            var subInputBuffer = APTR.FromPointer(patternBuffer.Raw +
                PatternBytes);
            var subPatternBuffer = APTR.FromPointer(subInputBuffer.Raw +
                PatternBytes);
            var fib = APTR.FromPointer(subPatternBuffer.Raw + PatternBytes);
            InitializeAnchor(anchor);

            if (pat.IsNotNull && DOS.ParsePatternNoCase(
                    CString.FromPointer(pat), patternBuffer,
                    unchecked((int)PatternBytes)) < 0)
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            if (sub.IsNotNull)
            {
                if (!BuildSubstringPattern(sub, subInputBuffer))
                {
                    error = (int)DOS.Error.BadTemplate;
                    break;
                }
                if (DOS.ParsePatternNoCase(
                        CString.FromPointer(subInputBuffer.Raw),
                        subPatternBuffer, unchecked((int)PatternBytes)) < 0)
                {
                    error = (int)DOS.Error.BadTemplate;
                    break;
                }
            }

            if (!noHead && !quick)
            {
                if (keys && !noDates)
                    DOS.FPuts(DOS.Output(),
                        "Name                      Size  Key Protection Date        Time     Comment\n");
                else if (!noDates)
                    DOS.FPuts(DOS.Output(),
                        "Name                      Size Protection Date        Time     Comment\n");
                else if (keys)
                    DOS.FPuts(DOS.Output(),
                        "Name                      Size  Key Protection Comment\n");
                else
                    DOS.FPuts(DOS.Output(),
                        "Name                      Size Protection Comment\n");
            }

            for (var index = 0u;; index++)
            {
                var slot = APTR.FromPointer(names.Raw + index * 4);
                var nameRaw = APTR.ReadUInt32(slot, 0);
                if (nameRaw == 0) break;
                var name = CString.FromPointer(nameRaw);
                var match = DOS.MatchFirst(name, anchor);
                while (match == 0)
                {
                    var currentFib = APTR.FromPointer(anchor.Raw +
                        (uint)DosLayout.AnchorPath.Info);
                    var directory = FileInfoBlock.GetDirEntryType(currentFib) >= 0;
                    var anchorFlags = APTR.ReadUInt8(anchor,
                        DosLayout.AnchorPath.Flags);
                    var directoryFinished = all && directory &&
                        (anchorFlags & DidDirectory) != 0;
                    if (directoryFinished)
                    {
                        anchorFlags = (byte)(anchorFlags & ~DidDirectory);
                        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                            anchorFlags);
                    }
                    if (!directoryFinished &&
                        ((!files && !dirs) || (directory && dirs) ||
                            (!directory && files)))
                    {
                        var fileName = FileInfoBlock.FileName(currentFib);
                        var allowed = pat.IsNull || DOS.MatchPatternNoCase(
                            CString.FromPointer(patternBuffer.Raw), fileName) != 0;
                        if (allowed && sub.IsNotNull)
                        {
                            // SUB is the source's literal file-name substring
                            // filter.  The surrounding #? operators provide
                            // substring matching; quoted metacharacters keep
                            // the user text literal while DOS supplies its
                            // case-insensitive comparison.
                            allowed = !directory &&
                                DOS.MatchPatternNoCase(
                                    CString.FromPointer(subPatternBuffer.Raw),
                                    fileName) != 0;
                        }
                        if (allowed && (since.IsNotNull || upto.IsNotNull))
                        {
                            var entryDays = unchecked((uint)
                                FileInfoBlock.GetDateDays(currentFib));
                            allowed = (since.IsNull || entryDays >= sinceDays) &&
                                (upto.IsNull || entryDays <= uptoDays);
                        }
                        if (allowed)
                        {
                            if (!quick && !noDates && !PrepareDateFields(currentFib,
                                    workspace))
                            {
                                error = (int)DOS.IoErr();
                                break;
                            }
                            var fields = default(LongFields);
                            var defaultFields = default(DefaultFields);
                            var fieldsNoDate = default(LongNoDateFields);
                            var defaultFieldsNoDate =
                                default(DefaultNoDateFields);
                            var namePointer = CString.ToUInt32(fileName);
                            if (directory)
                                fields.Size = CString.ToUInt32("Dir");
                            else
                                fields.Size = unchecked((uint)
                                    FileInfoBlock.GetSize(currentFib));
                            if (block && !directory)
                                fields.Size = (fields.Size + 511u) >> 9;
                            fields.Key = unchecked((uint)APTR.ReadUInt32(
                                currentFib, FileInfoBlock.DiskKeyOffset));
                            fields.Protection = FormatProtection(
                                currentFib, workspace);
                            fields.Date = workspace.Raw + DateTextOffset;
                            fields.Time = workspace.Raw + TimeTextOffset;
                            fields.Comment = FormatComment(currentFib, workspace);
                            defaultFields.Name = namePointer;
                            defaultFields.Size = fields.Size;
                            defaultFields.Protection = fields.Protection;
                            defaultFields.Date = fields.Date;
                            defaultFields.Time = fields.Time;
                            defaultFields.Comment = fields.Comment;
                            fieldsNoDate.Name = namePointer;
                            fieldsNoDate.Size = fields.Size;
                            fieldsNoDate.Key = fields.Key;
                            fieldsNoDate.Protection = fields.Protection;
                            fieldsNoDate.Comment = fields.Comment;
                            defaultFieldsNoDate.Name = namePointer;
                            defaultFieldsNoDate.Size = fields.Size;
                            defaultFieldsNoDate.Protection = fields.Protection;
                            defaultFieldsNoDate.Comment = fields.Comment;
                            fields.Name = namePointer;
                            if (quick)
                            {
                                var quickFields = default(QuickFields);
                                quickFields.Name = CString.ToUInt32(fileName);
                                DOS.VPrintf("%s\n",
                                    QuickFields.AddressOf(ref quickFields));
                            }
                            else if (directory && keys)
                            {
                                if (noDates)
                                    DOS.VPrintf(
                                        "%-24s %7s %4ld %8s%s\n",
                                        LongNoDateFields.AddressOf(
                                            ref fieldsNoDate));
                                else
                                    DOS.VPrintf(
                                        "%-24s %7s %4ld %8s %11s %8s%s\n",
                                        LongFields.AddressOf(ref fields));
                            }
                            else if (directory)
                            {
                                if (noDates)
                                    DOS.VPrintf(
                                        "%-24s %7s %8s%s\n",
                                        DefaultNoDateFields.AddressOf(
                                            ref defaultFieldsNoDate));
                                else
                                    DOS.VPrintf(
                                        "%-24s %7s %8s %11s %8s%s\n",
                                        DefaultFields.AddressOf(ref defaultFields));
                            }
                            else if (keys)
                            {
                                if (noDates)
                                    DOS.VPrintf(
                                        "%-24s %7ld %4ld %8s%s\n",
                                        LongNoDateFields.AddressOf(
                                            ref fieldsNoDate));
                                else
                                    DOS.VPrintf(
                                        "%-24s %7ld %4ld %8s %11s %8s%s\n",
                                        LongFields.AddressOf(ref fields));
                            }
                            else
                            {
                                if (noDates)
                                    DOS.VPrintf(
                                        "%-24s %7ld %8s%s\n",
                                        DefaultNoDateFields.AddressOf(
                                            ref defaultFieldsNoDate));
                                else
                                    DOS.VPrintf(
                                        "%-24s %7ld %8s %11s %8s%s\n",
                                        DefaultFields.AddressOf(ref defaultFields));
                            }
                            matched++;
                        }
                    }

                    if (all && directory && !directoryFinished)
                    {
                        anchorFlags = APTR.ReadUInt8(anchor,
                            DosLayout.AnchorPath.Flags);
                        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                            (byte)(anchorFlags | DoDirectory));
                    }

                    if ((Exec.SetSignal(0u, 0u) & CtrlCMask) != 0)
                    {
                        error = (int)DOS.Error.Break;
                        break;
                    }
                    match = DOS.MatchNext(anchor);
                }
                DOS.MatchEnd(anchor);
                if (error != 0) break;
                if (match != (int)DOS.Error.NoMoreEntries && match != 0)
                {
                    error = match;
                    break;
                }
            }
            if (error == 0 && !noHead && !quick)
                DOS.FPuts(DOS.Output(),
                    "\n");
            result = matched == 0 ? DOS.RETURN_WARN : DOS.RETURN_OK;
        }
        while (false);

        if (selectedOutput)
            DOS.SelectOutput(previousOutput);
        if (closeOutput && output.IsNotNull)
            DOS.Close(output);
        if (workspace.IsNotNull)
            Exec.FreeMem(workspace, WorkspaceAllocationBytes);
        arguments.Release();

        ioError = error;
        DOS.SetIoErr((DOS.Error)error);
        if (error != 0)
        {
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }
        return result;
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

    // Avoid copying the 24-byte parser lease for each option lookup.
    private static uint ReadSwitch(ref NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value) ? value : 0;

    private static bool BuildSubstringPattern(APTR substring,
        APTR destination)
    {
        var length = CStringLength(substring, PatternBytes - 1u);
        if (length >= PatternBytes - 1u) return false;

        APTR.WriteUInt8(destination, 0, (byte)'#');
        APTR.WriteUInt8(destination, 1, (byte)'?');
        var target = 2u;
        for (var index = 0u; index < length; index++)
        {
            var value = APTR.ReadUInt8(substring, unchecked((int)index));
            var escaped = IsPatternSpecial(value);
            var required = escaped ? 2u : 1u;
            if (target + required + 3u > PatternBytes) return false;
            if (escaped)
                APTR.WriteUInt8(destination, unchecked((int)target++),
                    (byte)'\'');
            APTR.WriteUInt8(destination, unchecked((int)target++), value);
        }

        APTR.WriteUInt8(destination, unchecked((int)target++), (byte)'#');
        APTR.WriteUInt8(destination, unchecked((int)target++), (byte)'?');
        APTR.WriteUInt8(destination, unchecked((int)target), 0);
        return true;
    }

    private static bool IsPatternSpecial(byte value) =>
        value is (byte)'?' or (byte)'#' or (byte)'(' or (byte)')' or
            (byte)'|' or (byte)'~' or (byte)'%' or (byte)'\'' or
            (byte)'[' or (byte)']';

    private static uint CStringLength(APTR value, uint maximum)
    {
        var length = 0u;
        while (length < maximum && APTR.ReadUInt8(value,
                unchecked((int)length)) != 0)
            length++;
        return length;
    }

    private static bool PrepareDateFields(APTR fib, APTR workspace)
    {
        var dateTime = APTR.FromPointer(workspace.Raw + DateTimeOffset);
        var day = APTR.FromPointer(workspace.Raw + DayTextOffset);
        var date = APTR.FromPointer(workspace.Raw + DateTextOffset);
        var time = APTR.FromPointer(workspace.Raw + TimeTextOffset);
        APTR.WriteUInt32(dateTime,
            DosLayout.DateTime.Stamp + DosLayout.DateStamp.Days,
            unchecked((uint)FileInfoBlock.GetDateDays(fib)));
        APTR.WriteUInt32(dateTime,
            DosLayout.DateTime.Stamp + DosLayout.DateStamp.Minutes,
            unchecked((uint)FileInfoBlock.GetDateMinute(fib)));
        APTR.WriteUInt32(dateTime,
            DosLayout.DateTime.Stamp + DosLayout.DateStamp.Ticks,
            unchecked((uint)FileInfoBlock.GetDateTick(fib)));
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Format,
            (byte)DosDateFormat.Dos);
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Flags, 0);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Day, day.Raw);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, date.Raw);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, time.Raw);
        return DOS.DateToStr(dateTime.Raw) != 0;
    }

    private static bool TryParseDate(APTR date, APTR dateTime,
        out uint days)
    {
        APTR.WriteUInt32(dateTime,
            DosLayout.DateTime.Stamp + DosLayout.DateStamp.Days, 0);
        APTR.WriteUInt32(dateTime,
            DosLayout.DateTime.Stamp + DosLayout.DateStamp.Minutes, 0);
        APTR.WriteUInt32(dateTime,
            DosLayout.DateTime.Stamp + DosLayout.DateStamp.Ticks, 0);
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Format,
            (byte)DosDateFormat.Dos);
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Flags, 0);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Day, 0);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, date.Raw);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, 0);
        if (DOS.StrToDate(dateTime.Raw) == 0)
        {
            days = 0;
            return false;
        }
        days = unchecked((uint)APTR.ReadUInt32(dateTime,
            DosLayout.DateTime.Stamp + DosLayout.DateStamp.Days));
        return true;
    }

    private static uint FormatProtection(APTR fib, APTR workspace)
    {
        var result = APTR.FromPointer(workspace.Raw + ProtectionTextOffset);
        var protection = unchecked((uint)FileInfoBlock.GetProtection(fib));
        APTR.WriteUInt8(result, 0,
            (protection & 0x80u) != 0 ? (byte)'h' : (byte)'-');
        APTR.WriteUInt8(result, 1,
            (protection & (uint)FileProtection.Script) != 0
                ? (byte)'s' : (byte)'-');
        APTR.WriteUInt8(result, 2,
            (protection & (uint)FileProtection.Pure) != 0
                ? (byte)'p' : (byte)'-');
        APTR.WriteUInt8(result, 3,
            (protection & (uint)FileProtection.Archive) != 0
                ? (byte)'a' : (byte)'-');
        APTR.WriteUInt8(result, 4,
            (protection & (uint)FileProtection.Read) == 0
                ? (byte)'r' : (byte)'-');
        APTR.WriteUInt8(result, 5,
            (protection & (uint)FileProtection.Write) == 0
                ? (byte)'w' : (byte)'-');
        APTR.WriteUInt8(result, 6,
            (protection & (uint)FileProtection.Execute) == 0
                ? (byte)'e' : (byte)'-');
        APTR.WriteUInt8(result, 7,
            (protection & (uint)FileProtection.Delete) == 0
                ? (byte)'d' : (byte)'-');
        APTR.WriteUInt8(result, 8, 0);
        return result.Raw;
    }

    private static uint FormatComment(APTR fib, APTR workspace)
    {
        var result = APTR.FromPointer(workspace.Raw + CommentTextOffset);
        var source = APTR.FromPointer(fib.Raw + FileInfoBlock.CommentOffset);
        var length = CStringLength(source, 80);
        var target = 0u;
        if (length != 0)
            APTR.WriteUInt8(result, unchecked((int)target++), (byte)':');
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(result, unchecked((int)target++),
                APTR.ReadUInt8(source, unchecked((int)index)));
        APTR.WriteUInt8(result, unchecked((int)target), 0);
        return result.Raw;
    }

    private static APTR ReadPointer(ref NativeCommandArguments arguments,
        uint index) => arguments.TryGetResult(index, out var value)
        ? APTR.FromPointer(value) : APTR.Null;
}
