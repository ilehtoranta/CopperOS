using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record ListEntryData(string Name, bool Directory, uint Size,
    uint Key, uint Protection, int DateDays = 1, int DateMinutes = 1,
    int DateTicks = 0, string Comment = "", bool ExitDirectory = false);

internal sealed record ListEntryCase(bool Quick = false, bool Keys = false,
    bool Dates = false, bool NoDates = false, bool Block = false,
    bool NoHead = false, bool Files = false, bool Dirs = false,
    string? Pattern = null, string? To = null, string? Sub = null,
    string? Since = null, string? Upto = null,
    int SinceDay = 0, int UptoDay = 0, bool DateParseFailure = false,
    bool MatchFailure = false, bool AllocationFailure = false,
    bool CtrlC = false, bool DateFailure = false, int ParserError = 0,
    bool All = false,
    ListEntryData[]? Entries = null);

internal sealed class ListNativeLayout(uint control, uint names, uint pattern,
    uint firstName, uint secondName, uint outputName, uint subName,
    uint sinceName, uint uptoName)
{
    public uint Control { get; } = control;
    public uint Names { get; } = names;
    public uint Pattern { get; } = pattern;
    public uint FirstName { get; } = firstName;
    public uint SecondName { get; } = secondName;
    public uint OutputName { get; } = outputName;
    public uint SubName { get; } = subName;
    public uint SinceName { get; } = sinceName;
    public uint UptoName { get; } = uptoName;
    public uint Workspace { get; set; }
    public uint RdArgs { get; set; }
    public int EntryIndex { get; set; }
    public bool MatchLive { get; set; }
}

internal sealed partial class ProbeFixture
{
    public const string ListEntrySuite =
        "morphos320-list-native-entry-vector-fixture";
    public const string WorkbenchListEntrySuite =
        "workbench31-list-native-entry-vector-fixture";

    private static readonly ListEntryData[] DefaultListEntries =
    [
        new("one.txt", false, 1025, 17, 5),
        new("subONE", true, 0, 18, 7),
    ];

    private List<object> RunListEntryCases()
    {
        ProbeCase[] cases =
        [
            ListCase("default", new(), "SRC:dir"),
            ListCase("quick", new(Quick: true), "SRC:dir QUICK"),
            ListCase("dates-quick", new(Quick: true, Dates: true),
                "SRC:dir DATES QUICK"),
            ListCase("files", new(Quick: true, Files: true),
                "SRC:dir QUICK FILES"),
            ListCase("dirs", new(Quick: true, Dirs: true),
                "SRC:dir QUICK DIRS"),
            ListCase("pattern", new(Quick: true, Pattern: "#?.txt"),
                "SRC:dir P #?.txt QUICK"),
            ListCase("sub-string", new(Quick: true, Sub: "ONE"),
                "SRC:dir SUB ONE QUICK"),
            ListCase("sub-literal-pattern-character", new(Quick: true,
                    Sub: "?name", Entries:
                    [new("file?name.txt", false, 4, 21, 5)]),
                "SRC:dir SUB ?name QUICK"),
            ListCase("keys", new(Keys: true, NoHead: true),
                "SRC:dir KEYS NOHEAD"),
            ListCase("dates", new(Dates: true, NoHead: true),
                "SRC:dir DATES NOHEAD"),
            ListCase("no-dates", new(NoDates: true, NoHead: true),
                "SRC:dir NODATES NOHEAD"),
            ListCase("keys-no-dates", new(Keys: true, NoDates: true,
                    NoHead: true), "SRC:dir KEYS NODATES NOHEAD"),
            ListCase("since-inclusive", new(Quick: true,
                    Since: "02-Jan-78", SinceDay: 1, Entries:
                    [new("before", false, 1, 30, 5, DateDays: 0),
                     new("on-date", false, 2, 31, 5, DateDays: 1),
                     new("after", false, 3, 32, 5, DateDays: 2)]),
                "SRC:dir SINCE 02-Jan-78 QUICK"),
            ListCase("upto-inclusive", new(Quick: true,
                    Upto: "03-Jan-78", UptoDay: 2, Entries:
                    [new("before", false, 1, 30, 5, DateDays: 1),
                     new("on-date", false, 2, 31, 5, DateDays: 2),
                     new("after", false, 3, 32, 5, DateDays: 3)]),
                "SRC:dir UPTO 03-Jan-78 QUICK"),
            ListCase("since-upto-inclusive", new(Quick: true,
                    Since: "02-Jan-78", SinceDay: 1,
                    Upto: "03-Jan-78", UptoDay: 2, Entries:
                    [new("before", false, 1, 30, 5, DateDays: 0),
                     new("start", false, 2, 31, 5, DateDays: 1),
                     new("end", false, 3, 32, 5, DateDays: 2),
                     new("after", false, 4, 33, 5, DateDays: 3)]),
                "SRC:dir SINCE 02-Jan-78 UPTO 03-Jan-78 QUICK"),
            ListCase("all-recursive", new(All: true, Entries:
                [new("one.txt", false, 2, 30, 5),
                 new("subdir", true, 0, 31, 5),
                 new("inside.txt", false, 3, 32, 5),
                 new("subdir", true, 0, 31, 5, ExitDirectory: true),
                 new("two.txt", false, 4, 33, 5)]), "SRC:dir ALL"),
            new("invalid-since-date", "SRC:dir SINCE invalid",
                DOS.RETURN_FAIL, (int)DOS.Error.BadTemplate, "")
            { List = new(Since: "invalid", DateParseFailure: true) },
            ListCase("comment", new(NoHead: true, Entries:
                    [new("note.txt", false, 6, 22, 0, Comment: "memo")]),
                "SRC:dir NOHEAD"),
            new("date-conversion-failure", "SRC:dir", DOS.RETURN_FAIL,
                205, ListHeader(false))
            { List = new(DateFailure: true) },
            ListCase("pattern-and-sub", new(Quick: true,
                    Pattern: "#?.txt", Sub: "ONE"),
                "SRC:dir P #?.txt SUB ONE QUICK"),
            ListCase("block-nohead", new(Block: true, NoHead: true),
                "SRC:dir BLOCK NOHEAD"),
            ListCase("to", new(Quick: true, To: "RAM:out"),
                "SRC:dir QUICK TO RAM:out"),
            new("empty-match", "SRC:missing", DOS.RETURN_WARN, 0,
                ListOutput(new(MatchFailure: true)))
            { List = new(MatchFailure: true) },
            new("allocation-failure", "SRC:dir", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "")
            { List = new(AllocationFailure: true) },
            new("ctrl-c", "SRC:dir", DOS.RETURN_FAIL,
                (int)DOS.Error.Break,
                ListOutput(new(), ctrlC: true))
            { List = new(CtrlC: true) },
            new("parser-failure", "SRC:dir", DOS.RETURN_ERROR, 116, "")
            { List = new(ParserError: 116) },
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { List = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { List = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { List = new(), EntryLength = 4, NullArgumentPointer = true },
        ];
        if (suite == WorkbenchListEntrySuite)
        {
            cases =
            [
                .. cases,
                new("missing-dos", "SRC:dir", DOS.RETURN_FAIL,
                    Invocation.InitialIoError, "")
                { List = new(), MissingDos = true }
            ];
        }

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            ListCase("interleaved-left", new(Quick: true), "SRC:dir QUICK"),
            ListCase("interleaved-right", new(Block: true, NoHead: true),
                "SRC:dir BLOCK NOHEAD")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase ListCase(string name, ListEntryCase definition,
        string arguments)
    {
        var output = ListOutput(definition);
        return new(name, arguments, DOS.RETURN_OK, 0, output)
        { List = definition };
    }

    private static string ListOutput(ListEntryCase definition, bool ctrlC = false)
    {
        var entries = definition.Entries ?? DefaultListEntries;
        var output = new StringBuilder();
        if (!definition.NoHead && !definition.Quick)
            output.Append(ListHeader(definition.Keys, definition.NoDates));
        var emitted = 0;
        foreach (var entry in entries)
        {
            if (definition.MatchFailure) break;
            if (definition.All && entry.Directory && entry.ExitDirectory)
                continue;
            if (definition.Files && entry.Directory ||
                definition.Dirs && !entry.Directory)
                continue;
            if (definition.Sub is not null &&
                (entry.Directory || !ListContainsNoCase(entry.Name,
                    definition.Sub)))
                continue;
            if (definition.Pattern is not null &&
                !ListGlobMatch(definition.Pattern, entry.Name))
                continue;
            if ((definition.Since is not null &&
                    entry.DateDays < definition.SinceDay) ||
                (definition.Upto is not null &&
                    entry.DateDays > definition.UptoDay))
                continue;
            if (definition.Quick)
                output.Append(entry.Name).Append('\n');
            else
            {
                var size = definition.Block ? (entry.Size + 511) >> 9 :
                    entry.Size;
                var sizeText = entry.Directory ? "Dir" : size.ToString();
                var protection = ListProtectionText(entry.Protection);
                var comment = entry.Comment.Length == 0
                    ? string.Empty : ":" + entry.Comment;
                if (definition.NoDates)
                {
                    if (definition.Keys)
                        output.AppendFormat(
                            "{0,-24} {1,7} {2,4} {3,8}{4}\n",
                            entry.Name, sizeText, entry.Key, protection,
                            comment);
                    else
                        output.AppendFormat("{0,-24} {1,7} {2,8}{3}\n",
                            entry.Name, sizeText, protection, comment);
                }
                else
                {
                    var date = "01-Jan-78";
                    var time = "00:01:00";
                    if (definition.Keys)
                        output.AppendFormat(
                            "{0,-24} {1,7} {2,4} {3,8} {4,11} {5,8}{6}\n",
                            entry.Name, sizeText, entry.Key, protection, date,
                            time, comment);
                    else
                        output.AppendFormat(
                            "{0,-24} {1,7} {2,8} {3,11} {4,8}{5}\n",
                            entry.Name, sizeText, protection, date, time,
                            comment);
                }
            }
            emitted++;
            if (ctrlC && emitted == 1) break;
        }
        if (!ctrlC && !definition.NoHead && !definition.Quick)
            output.Append('\n');
        return output.ToString();
    }

    private void PrepareListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.List ?? new();
        var control = invocation.Process + 0x200;
        var names = control + 32;
        var pattern = control + 48;
        var firstName = control + 560;
        var secondName = control + 576;
        var outputName = control + 592;
        var subName = control + 608;
        var sinceName = control + 624;
        var uptoName = control + 640;
        invocation.ListLayout = new(control, names, pattern, firstName,
            secondName, outputName, subName, sinceName, uptoName);
        PutCString(firstName, "SRC:dir");
        PutCString(secondName, "SRC:missing");
        PutCString(outputName, definition.To ?? "RAM:out");
        PutCString(subName, definition.Sub ?? "");
        PutCString(sinceName, definition.Since ?? "");
        PutCString(uptoName, definition.Upto ?? "");
        PutCString(pattern, definition.Pattern ?? "");
        Bus.Long(names, definition.MatchFailure ? secondName : firstName);
        Bus.Long(names + 4, 0);
    }

    private void RegisterListEntryExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state,
            invocation) =>
        {
            Require(state.D[0] == 0 && state.D[1] == 0,
                "List Ctrl-C query ABI differs.");
            invocation.ListSetSignalCalls++;
            return invocation.Definition.List!.CtrlC ? 1u << 12 : 0;
        });
    }

    private void RegisterListEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.List!;
            var template = suite == WorkbenchListEntrySuite
                ? NativeWorkbench31ListCommand.Template
                : NativeMorphOSListCommand.Template;
            var resultBytes = suite == WorkbenchListEntrySuite
                ? NativeWorkbench31ListCommand.ResultCount * 4u
                : NativeMorphOSListCommand.ResultCount * 4u;
            Require(Bus.CString(state.D[1]) == template,
                "List template differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2],
                "ListResults").Size == resultBytes,
                "List result storage differs.");
            invocation.ListReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var result = state.D[2];
            var layout = invocation.ListLayout!;
            Bus.Long(result, layout.Names);
            if (definition.Pattern is not null) Bus.Long(result + 4, layout.Pattern);
            Bus.Long(result + 2 * 4,
                definition.Keys ? uint.MaxValue : 0);
            Bus.Long(result + 3 * 4,
                definition.Dates ? uint.MaxValue : 0);
            Bus.Long(result + 4 * 4,
                definition.NoDates ? uint.MaxValue : 0);
            if (definition.Since is not null)
                Bus.Long(result + 7 * 4, layout.SinceName);
            if (definition.Upto is not null)
                Bus.Long(result + 8 * 4, layout.UptoName);
            if (definition.Sub is not null)
                Bus.Long(result + 6 * 4, layout.SubName);
            Bus.Long(result + 9 * 4, definition.Quick ? uint.MaxValue : 0);
            Bus.Long(result + 10 * 4, definition.Block ? uint.MaxValue : 0);
            Bus.Long(result + 11 * 4, definition.NoHead ? uint.MaxValue : 0);
            Bus.Long(result + 12 * 4, definition.Files ? uint.MaxValue : 0);
            Bus.Long(result + 13 * 4, definition.Dirs ? uint.MaxValue : 0);
            if (definition.To is not null) Bus.Long(result + 5 * 4, layout.OutputName);
            Bus.Long(result + (suite == WorkbenchListEntrySuite ? 15u : 18u) * 4,
                definition.All ? uint.MaxValue : 0);
            layout.RdArgs = Bus.Allocate(invocation, 128, "ListRdArgs", true);
            return layout.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            var layout = invocation.ListLayout!;
            Bus.Release(invocation, state.D[1], "ListRdArgs", 128);
            invocation.ListFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.ParsePatternNoCase,
            "ParsePatternNoCase", (state, invocation) =>
        {
            invocation.ListParsePatternCalls++;
            var pattern = Bus.CString(state.D[1]);
            invocation.ListParsePatternInputs.Add(pattern);
            Require(state.D[1] != state.D[2] && state.D[2] != 0 &&
                state.D[3] == 512,
                "List pattern parser ABI differs.");
            Encoding.Latin1.GetBytes(pattern).CopyTo(
                Bus.Memory.AsSpan((int)state.D[2]));
            Bus.Memory[state.D[2] + (uint)pattern.Length] = 0;
            return unchecked((uint)(pattern.Length <= 510 ? pattern.Length : -1));
        });
        Register(baseAddress, DosLvo.MatchPatternNoCase,
            "MatchPatternNoCase", (state, invocation) =>
        {
            invocation.ListMatchPatternCalls++;
            return ListGlobMatch(Bus.CString(state.D[1]), Bus.CString(state.D[2]))
                ? 1u : 0u;
        });
        Register(baseAddress, DosLvo.StrToDate, "StrToDate",
            (state, invocation) =>
        {
            var layout = invocation.ListLayout!;
            var definition = invocation.Definition.List!;
            var dateTime = state.D[1];
            var dateTimeOffset = (uint)DosLayout.AnchorPath.Size + 512u +
                512u * 3u + (uint)FileInfoBlock.SizeInBytes;
            Require(dateTime == layout.Workspace + dateTimeOffset &&
                Bus.Memory[dateTime + (uint)DosLayout.DateTime.Format] ==
                    (byte)DosDateFormat.Dos &&
                Bus.Memory[dateTime + (uint)DosLayout.DateTime.Flags] == 0 &&
                Bus.Long(dateTime + (uint)DosLayout.DateTime.Day) == 0 &&
                Bus.Long(dateTime + (uint)DosLayout.DateTime.Time) == 0,
                "List StrToDate structure differs.");
            var input = Bus.Long(dateTime + (uint)DosLayout.DateTime.Date);
            var text = Bus.CString(input);
            var days = text == definition.Since
                ? definition.SinceDay : text == definition.Upto
                    ? definition.UptoDay : int.MinValue;
            Require(input == layout.SinceName || input == layout.UptoName,
                "List StrToDate did not receive a ReadArgs-owned date.");
            invocation.ListStrToDateCalls++;
            if (definition.DateParseFailure || days == int.MinValue)
                return 0;
            Bus.Long(dateTime + DosLayout.DateTime.Stamp +
                DosLayout.DateStamp.Days, unchecked((uint)days));
            return 1;
        });
        Register(baseAddress, DosLvo.DateToStr, "DateToStr",
            (state, invocation) =>
        {
            var layout = invocation.ListLayout!;
            var dateTime = state.D[1];
            var dateTimeOffset = (uint)DosLayout.AnchorPath.Size + 512u +
                512u * 3u + (uint)FileInfoBlock.SizeInBytes;
            Require(dateTime == layout.Workspace + dateTimeOffset &&
                Bus.Memory[dateTime + (uint)DosLayout.DateTime.Format] ==
                    (byte)DosDateFormat.Dos &&
                Bus.Memory[dateTime + (uint)DosLayout.DateTime.Flags] == 0,
                "List DateToStr structure differs.");
            var entry = (invocation.Definition.List!.Entries ??
                DefaultListEntries)[layout.EntryIndex];
            Require(Bus.Long(dateTime + DosLayout.DateTime.Stamp +
                    DosLayout.DateStamp.Days) == unchecked((uint)entry.DateDays) &&
                Bus.Long(dateTime + DosLayout.DateTime.Stamp +
                    DosLayout.DateStamp.Minutes) ==
                    unchecked((uint)entry.DateMinutes) &&
                Bus.Long(dateTime + DosLayout.DateTime.Stamp +
                    DosLayout.DateStamp.Ticks) == unchecked((uint)entry.DateTicks),
                "List DateToStr did not receive the current FileInfoBlock stamp.");
            var day = Bus.Long(dateTime + DosLayout.DateTime.Day);
            var date = Bus.Long(dateTime + DosLayout.DateTime.Date);
            var time = Bus.Long(dateTime + DosLayout.DateTime.Time);
            Require(day == layout.Workspace + dateTimeOffset + DosDateTime.Size &&
                date == day + DosDateTime.StringLength &&
                time == date + DosDateTime.StringLength,
                "List DateToStr output buffers are not invocation-owned.");
            PutCString(day, "Monday");
            PutCString(date, "01-Jan-78");
            PutCString(time, "00:01:00");
            invocation.ListDateToStrCalls++;
            if (invocation.Definition.List!.DateFailure)
            {
                invocation.IoError = 205;
                return 0;
            }
            return 1;
        });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state,
            invocation) =>
        {
            var layout = invocation.ListLayout!;
            Require(state.D[2] != 0 && state.D[1] != 0,
                "List MatchFirst arguments differ.");
            invocation.ListMatchFirstCalls++;
            layout.Workspace = state.D[2];
            layout.EntryIndex = 0;
            layout.MatchLive = true;
            if (invocation.Definition.List!.MatchFailure)
                return (uint)DOS.Error.NoMoreEntries;
            PutListFib(invocation, layout, 0);
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state,
            invocation) =>
        {
            var layout = invocation.ListLayout!;
            Require(layout.MatchLive && state.D[1] == layout.Workspace,
                "List MatchNext lifetime differs.");
            invocation.ListMatchNextCalls++;
            var entries = invocation.Definition.List!.Entries ?? DefaultListEntries;
            if (invocation.Definition.List.All)
            {
                var previous = entries[layout.EntryIndex];
                var flags = Bus.Memory[layout.Workspace +
                    (uint)DosLayout.AnchorPath.Flags];
                var shouldDescend = previous.Directory &&
                    !previous.ExitDirectory;
                Require(((flags & (byte)AnchorPathFlags.DoDirectory) != 0) ==
                        shouldDescend &&
                    (flags & (byte)AnchorPathFlags.DidDirectory) == 0,
                    "List ALL did not follow the AnchorPath recursion flags.");
            }
            layout.EntryIndex++;
            if (layout.EntryIndex >= entries.Length)
                return (uint)DOS.Error.NoMoreEntries;
            PutListFib(invocation, layout, layout.EntryIndex);
            return 0;
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state,
            invocation) =>
        {
            var layout = invocation.ListLayout!;
            Require(state.D[1] == layout.Workspace,
                "List MatchEnd workspace differs.");
            layout.MatchLive = false;
            invocation.ListMatchEndCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.OpenRaw, "OpenRaw", (state,
            invocation) =>
        {
            Require(state.D[2] == (uint)DOS.FileMode.NewFile,
                "List TO mode differs.");
            invocation.ListOpenCalls++;
            return 0x900u + (uint)invocation.Slot;
        });
        Register(baseAddress, DosLvo.SelectOutput, "SelectOutput",
            (state, invocation) =>
        {
            Require(state.D[1] != 0, "List output selection handle is null.");
            invocation.ListSelectOutputCalls++;
            return invocation.OutputBptr;
        });
        Register(baseAddress, DosLvo.Close, "Close", (state, invocation) =>
        {
            Require(state.D[1] != 0, "List closed a null TO handle.");
            invocation.ListCloseCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state,
            invocation) =>
        {
            var format = Bus.CString(state.D[1]);
            var args = state.D[2];
            if (format == "%s\n")
            {
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    Bus.CString(Bus.Long(args)) + "\n"));
            }
            else
            {
                var hasKeys = format.Contains("%4ld",
                    StringComparison.Ordinal);
                var hasDates = format.Contains("%11s",
                    StringComparison.Ordinal);
                var directory = format.Contains("%7s",
                    StringComparison.Ordinal);
                Require(format is "%-24s %7s %4ld %8s %11s %8s%s\n" or
                    "%-24s %7s %8s %11s %8s%s\n" or
                    "%-24s %7ld %4ld %8s %11s %8s%s\n" or
                    "%-24s %7ld %8s %11s %8s%s\n" or
                    "%-24s %7s %4ld %8s%s\n" or
                    "%-24s %7s %8s%s\n" or
                    "%-24s %7ld %4ld %8s%s\n" or
                    "%-24s %7ld %8s%s\n",
                    "List long format differs.");
                var name = Bus.CString(Bus.Long(args));
                var rawSize = Bus.Long(args + 4);
                var size = directory ? Bus.CString(rawSize) :
                    rawSize.ToString();
                var offset = 8u;
                var key = 0u;
                if (hasKeys)
                {
                    key = Bus.Long(args + offset);
                    offset += 4;
                }
                var protection = Bus.CString(Bus.Long(args + offset));
                var date = hasDates
                    ? Bus.CString(Bus.Long(args + offset + 4)) : string.Empty;
                var time = hasDates
                    ? Bus.CString(Bus.Long(args + offset + 8)) : string.Empty;
                var commentOffset = offset + (hasDates ? 12u : 4u);
                var comment = Bus.CString(Bus.Long(args + commentOffset));
                var line = (hasKeys, hasDates) switch
                {
                    (true, true) => string.Format(
                        "{0,-24} {1,7} {2,4} {3,8} {4,11} {5,8}{6}\n",
                        name, size, key, protection, date, time, comment),
                    (false, true) => string.Format(
                        "{0,-24} {1,7} {2,8} {3,11} {4,8}{5}\n",
                        name, size, protection, date, time, comment),
                    (true, false) => string.Format(
                        "{0,-24} {1,7} {2,4} {3,8}{4}\n",
                        name, size, key, protection, comment),
                    _ => string.Format("{0,-24} {1,7} {2,8}{3}\n",
                        name, size, protection, comment)
                };
                invocation.Output.Write(Encoding.Latin1.GetBytes(line));
            }
            invocation.ListVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            var text = Bus.CString(state.D[2]);
            Require(text == ListHeader(invocation.Definition.List!.Keys,
                    invocation.Definition.List.NoDates) ||
                text == "\n", "List header/trailer differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.ListFPutsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (_, invocation) =>
        {
            invocation.ListPrintFaultCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state,
            invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void PutListFib(Invocation invocation, ListNativeLayout layout,
        int index)
    {
        var entries = invocation.Definition.List!.Entries ?? DefaultListEntries;
        var entry = entries[index];
        var fib = layout.Workspace + (uint)DosLayout.AnchorPath.Info;
        Bus.Memory[layout.Workspace + (uint)DosLayout.AnchorPath.Flags] =
            entry.ExitDirectory ? (byte)AnchorPathFlags.DidDirectory : (byte)0;
        Bus.Long(fib + (uint)FileInfoBlock.DirEntryTypeOffset,
            entry.Directory ? 2u : unchecked((uint)-3));
        Bus.Long(fib + (uint)FileInfoBlock.DiskKeyOffset, entry.Key);
        Bus.Long(fib + (uint)FileInfoBlock.SizeOffset, entry.Size);
        Bus.Long(fib + (uint)FileInfoBlock.DateDaysOffset,
            unchecked((uint)entry.DateDays));
        Bus.Long(fib + (uint)FileInfoBlock.DateMinuteOffset,
            unchecked((uint)entry.DateMinutes));
        Bus.Long(fib + (uint)FileInfoBlock.DateTickOffset,
            unchecked((uint)entry.DateTicks));
        Bus.Long(fib + (uint)FileInfoBlock.ProtectionOffset, entry.Protection);
        var commentBytes = Encoding.Latin1.GetBytes(entry.Comment);
        commentBytes.CopyTo(Bus.Memory.AsSpan(
            (int)fib + FileInfoBlock.CommentOffset));
        Bus.Memory[fib + (uint)FileInfoBlock.CommentOffset +
            (uint)commentBytes.Length] = 0;
        var bytes = Encoding.Latin1.GetBytes(entry.Name);
        bytes.CopyTo(Bus.Memory.AsSpan((int)fib + FileInfoBlock.FileNameOffset));
        Bus.Memory[fib + (uint)FileInfoBlock.FileNameOffset +
            (uint)bytes.Length] = 0;
    }

    private static bool ListGlobMatch(string pattern, string text)
    {
        var p = 0;
        var t = 0;
        var star = -1;
        var retry = 0;
        while (t < text.Length)
        {
            if (p + 1 < pattern.Length && pattern[p] == '\'')
            {
                if (char.ToUpperInvariant(pattern[p + 1]) ==
                    char.ToUpperInvariant(text[t]))
                { p += 2; t++; continue; }
            }
            if (p < pattern.Length &&
                (pattern[p] == '?' ||
                 char.ToUpperInvariant(pattern[p]) ==
                    char.ToUpperInvariant(text[t])))
            { p++; t++; continue; }
            if (p + 1 < pattern.Length && pattern[p] == '#' &&
                pattern[p + 1] == '?')
            { star = p; p += 2; retry = t; continue; }
            if (p < pattern.Length && pattern[p] == '*')
            { star = p++; retry = t; continue; }
            if (star < 0) return false;
            p = star + (pattern[star] == '#' ? 2 : 1);
            t = ++retry;
        }
        while (p + 1 < pattern.Length && pattern[p] == '#' &&
            pattern[p + 1] == '?') p += 2;
        while (p < pattern.Length && pattern[p] == '*') p++;
        return p == pattern.Length;
    }

    private static bool ListContainsNoCase(string value, string substring) =>
        value.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0;

    private static int ListVisibleCount(ListEntryCase definition)
    {
        if (definition.MatchFailure) return 0;
        var count = 0;
        foreach (var entry in definition.Entries ?? DefaultListEntries)
        {
            if (definition.All && entry.Directory && entry.ExitDirectory)
                continue;
            if (definition.Files && entry.Directory ||
                definition.Dirs && !entry.Directory ||
                definition.Sub is not null && (entry.Directory ||
                    !ListContainsNoCase(entry.Name, definition.Sub)) ||
                definition.Pattern is not null &&
                    !ListGlobMatch(definition.Pattern, entry.Name) ||
                definition.Since is not null &&
                    entry.DateDays < definition.SinceDay ||
                definition.Upto is not null &&
                    entry.DateDays > definition.UptoDay)
                continue;
            count++;
        }
        return count;
    }

    private static string ListHeader(bool keys, bool noDates = false) =>
        (keys, noDates) switch
        {
            (true, false) =>
                "Name                      Size  Key Protection Date        Time     Comment\n",
            (false, false) =>
                "Name                      Size Protection Date        Time     Comment\n",
            (true, true) =>
                "Name                      Size  Key Protection Comment\n",
            _ => "Name                      Size Protection Comment\n"
        };

    private static string ListProtectionText(uint protection)
    {
        var flags = unchecked((int)protection);
        return new string([
            (flags & 0x80) != 0 ? 'h' : '-',
            (flags & (int)FileProtection.Script) != 0 ? 's' : '-',
            (flags & (int)FileProtection.Pure) != 0 ? 'p' : '-',
            (flags & (int)FileProtection.Archive) != 0 ? 'a' : '-',
            (flags & (int)FileProtection.Read) == 0 ? 'r' : '-',
            (flags & (int)FileProtection.Write) == 0 ? 'w' : '-',
            (flags & (int)FileProtection.Execute) == 0 ? 'e' : '-',
            (flags & (int)FileProtection.Delete) == 0 ? 'd' : '-']);
    }

    private static string ListSubstringPattern(string value)
    {
        var pattern = new StringBuilder("#?");
        foreach (var character in value)
        {
            if ("?#()|~%'[]".Contains(character)) pattern.Append('\'');
            pattern.Append(character);
        }
        return pattern.Append("#?").ToString();
    }

    private void VerifyListEntry(Invocation invocation)
    {
        var definition = invocation.Definition.List!;
        if (invocation.Definition.Workbench ||
            invocation.Definition.MissingDos ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.ListMatchFirstCalls == 0,
                "List crossed an invalid startup boundary.");
            return;
        }
        if (definition.ParserError != 0 || definition.AllocationFailure)
        {
            Require(invocation.ListMatchFirstCalls == 0,
                "List traversed after an early failure.");
            return;
        }
        if (definition.DateParseFailure)
        {
            Require(invocation.ListReadArgsCalls == 1 &&
                invocation.ListFreeArgsCalls == 1 &&
                invocation.ListAllocMemCalls == 2 &&
                invocation.ListFreeMemCalls == 2 &&
                invocation.ListStrToDateCalls == 1 &&
                invocation.ListMatchFirstCalls == 0 &&
                invocation.ListPrintFaultCalls == 1 &&
                invocation.IoError == (int)DOS.Error.BadTemplate,
                "List invalid date cleanup or error publication differs.");
            return;
        }
        Require(invocation.ListReadArgsCalls == 1 &&
            invocation.ListFreeArgsCalls == 1 &&
            invocation.ListAllocMemCalls == 2 &&
            invocation.ListFreeMemCalls == 2 &&
            invocation.ListMatchFirstCalls == 1 &&
            invocation.ListMatchEndCalls == 1,
            "List parser/workspace/matcher ownership differs.");
        var expectedDateCalls = definition.Quick || definition.NoDates
            ? 0 : ListVisibleCount(definition);
        if ((definition.CtrlC || definition.DateFailure) &&
            expectedDateCalls > 0)
            expectedDateCalls = 1;
        Require(invocation.ListDateToStrCalls == expectedDateCalls,
            "List DateToStr cadence differs.");
        var expectedParseCalls = (definition.Since is null ? 0 : 1) +
            (definition.Upto is null ? 0 : 1);
        Require(invocation.ListStrToDateCalls == expectedParseCalls,
            "List StrToDate cadence differs.");
        if (!definition.MatchFailure)
            Require(invocation.ListMatchNextCalls ==
                (definition.CtrlC || definition.DateFailure ? 0 :
                    (definition.Entries ?? DefaultListEntries).Length),
                $"List MatchNext cadence differs ({invocation.Definition.Name}: observed={invocation.ListMatchNextCalls}, expected={(definition.CtrlC || definition.DateFailure ? 0 : (definition.Entries ?? DefaultListEntries).Length)}).");
        if (definition.Pattern is not null)
            Require(invocation.ListParsePatternCalls ==
                    (definition.Sub is null ? 1 : 2) &&
                invocation.ListMatchPatternCalls >= 1,
                "List pattern matching did not use DOS APIs.");
        else if (definition.Sub is not null)
            Require(invocation.ListParsePatternCalls == 1 &&
                invocation.ListMatchPatternCalls >= 1,
                "List SUB filtering did not use DOS patterns.");
        if (definition.Pattern is not null && definition.Sub is not null)
            Require(invocation.ListParsePatternInputs.Count == 2 &&
                invocation.ListParsePatternInputs[0] == definition.Pattern &&
                invocation.ListParsePatternInputs[1] ==
                    ListSubstringPattern(definition.Sub),
                "List P and SUB pattern parsing order or escaping differs.");
        else if (definition.Sub is not null)
            Require(invocation.ListParsePatternInputs.Count == 1 &&
                invocation.ListParsePatternInputs[0] ==
                    ListSubstringPattern(definition.Sub),
                "List SUB literal pattern differs.");
        if (definition.To is not null)
            Require(invocation.ListOpenCalls == 1 &&
                invocation.ListSelectOutputCalls == 2 &&
                invocation.ListCloseCalls == 1,
                "List TO output lifetime differs.");
        if (definition.CtrlC)
            Require(invocation.ListPrintFaultCalls == 1,
                "List Ctrl-C fault publication differs.");
        if (definition.DateFailure)
            Require(invocation.ListPrintFaultCalls == 1 &&
                invocation.IoError == 205 &&
                invocation.ListMatchEndCalls == 1 &&
                invocation.ListFreeMemCalls == 2,
                "List DateToStr failure cleanup differs.");
    }
}
