using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record SearchEntryCase(string Path = "SRC:one.txt",
    string Content = "alpha\nNeedle\nlast\n", string Search = "needle",
    bool NoNumber = false, bool Quiet = false, bool Quick = false,
    bool FileMode = false, bool PatternMode = false,
    bool CaseSensitive = false, uint LinesAfter = 0,
    bool MatchFailure = false, bool ReadFailure = false,
    bool AllocationFailure = false, bool BufferAllocationFailure = false,
    int BufferAllocationFailures = 0,
    bool CtrlC = false, bool CtrlD = false,
    bool AllTraversal = false,
    string TraversalShape = "single",
    bool LocaleCaseMap = false, bool LocaleLibraryOpenFailure = false,
    bool OpenLocaleFailure = false,
    bool LocaleHighByteControl = false,
    bool LocaleHighBytePrintable = false,
    int ParserError = 0, int ReadChunkBytes = 0,
    int CtrlDSignalCall = 0, bool Seek64FileSize = false,
    bool Seek64Failure = false, bool OpenFailure = false);

internal sealed record SearchTraversalStep(string Name, int EntryType,
    byte Flags, uint DirectoryLock, string DirectoryPath);

internal sealed class SearchEntryNativeLayout(uint control, uint from,
    uint search, uint path, uint lines)
{
    public uint Control { get; } = control;
    public uint From { get; } = from;
    public uint Search { get; } = search;
    public uint Path { get; } = path;
    public uint Lines { get; } = lines;
    public uint CurrentChain { get; } = control + 1200;
    public uint DeviceProc { get; } = control + 128;
    public uint Workspace { get; set; }
    public uint FileBuffer { get; set; }
    public uint FileBufferBytes { get; set; }
    public byte[] FileContent { get; set; } = [];
    public int FileBufferAllocationCount { get; set; }
    public uint RdArgs { get; set; }
    public bool MatchLive { get; set; }
    public int MatchNextCalls { get; set; }
    public Dictionary<uint, string> DirectoryPaths { get; } = [];
    public List<string> BuiltPaths { get; } = [];
    public uint CurrentDirectory { get; set; } = 0x1000u;
    public int SoftLinkLocks { get; set; }
    public int SoftLinkExaminations { get; set; }
    public int SoftLinkUnlocks { get; set; }
    public int SoftLinkDeviceLookups { get; set; }
    public int SoftLinkReads { get; set; }
    public int SoftLinkDeviceReleases { get; set; }
    public int SoftLinkWarnings { get; set; }
    public int SearchSeekCalls { get; set; }
    public int SearchSeek64Calls { get; set; }
    public List<(uint PositionHigh, uint PositionLow, int Mode)>
        SearchSeek64Requests { get; } = [];
}

internal sealed partial class ProbeFixture
{
    private const uint SearchLocaleBase = 0xd000;
    private const uint SearchLocaleObject = 0xcafe;
    public const string SearchEntrySuite =
        "morphos320-search-native-entry-vector-fixture";
    public const string WorkbenchSearchEntrySuite =
        "wb31-search-native-entry-vector-fixture";

    private static bool IsSearchEntrySuite(string suite) =>
        suite == SearchEntrySuite || suite == WorkbenchSearchEntrySuite;

    private List<object> RunSearchEntryCases()
    {
        ProbeCase[] cases =
        [
            Search("literal", new(), "SRC:one.txt needle",
                "     2> Needle\n"),
            Search("nonum-lines", new(NoNumber: true, LinesAfter: 1),
                "SRC:one.txt needle NONUM LINES 1", "Needle\nlast\n"),
            Search("quiet", new(Quiet: true), "SRC:one.txt needle QUIET",
                "SRC:one.txt\n"),
            Search("case-sensitive-miss", new(CaseSensitive: true),
                "SRC:one.txt needle CASE", "", result: DOS.RETURN_WARN),
            Search("file-pattern", new(FileMode: true, Search: "one.txt"),
                "SRC:one.txt one.txt FILE", "SRC:one.txt\n"),
            Search("pattern-line", new(PatternMode: true, Search: "eed"),
                "SRC:one.txt eed PATTERN", "     2> Needle\n"),
            Search("empty-match", new(MatchFailure: true), "SRC:missing needle",
                "", result: DOS.RETURN_WARN),
            Search("allocation-failure", new(AllocationFailure: true),
                "SRC:one.txt needle", "", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore),
            Search("read-failure", new(ReadFailure: true),
                "SRC:one.txt needle", "", DOS.RETURN_WARN),
            Search("ctrl-c", new(CtrlC: true), "SRC:one.txt needle",
                "     2> Needle\n",
                DOS.RETURN_FAIL, (int)DOS.Error.Break),
            new("parser-failure", "SRC:one.txt needle", DOS.RETURN_ERROR,
                116, "") { Search = new(ParserError: 116) },
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Search = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Search = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Search = new(), EntryLength = 4, NullArgumentPointer = true },
        ];
        if (suite == WorkbenchSearchEntrySuite)
        {
            // Workbench Search's captured candidate has no LINES/N option;
            // retain an equivalent NONUM output vector for this profile.
            cases[1] = Search("nonum", new(NoNumber: true),
                "SRC:one.txt needle NONUM", "Needle\n");
            // CASE/S is a MorphOS-only extension in the source template.
            cases[3] = Search("quick", new(Quick: true),
                "SRC:one.txt needle QUICK", "     2> Needle\n");
            // The Workbench profile has no source-bound error contract; keep
            // the existing candidate assumption distinct from MorphOS 3.20.
            cases[8] = Search("candidate-read-failure",
                new(ReadFailure: true), "SRC:one.txt needle", "",
                DOS.RETURN_FAIL, 205);
            cases =
            [
                .. cases,
                Search("long-line", new(Search: "Needle",
                        Content: "prefix " + new string('x', 8179) + " Needle\n"),
                    "SRC:one.txt Needle",
                    "     1> prefix " + new string('x', 8179) + " Needle\n"),
                Search("all-directory", new(AllTraversal: true,
                        Path: "SRC:"), "SRC: needle ALL",
                    "     SRC (dir)\n        one.txt..\n     2> Needle\n"),
                new("missing-dos", "SRC:one.txt needle", DOS.RETURN_FAIL,
                    Invocation.InitialIoError, "")
                { Search = new(), MissingDos = true }
            ];
        }
        else
        {
            cases =
            [
                .. cases,
                Search("all-directory", new(AllTraversal: true,
                        Path: "SRC:"), "SRC: needle ALL",
                    "     SRC (dir)\n        one.txt..\n     2> Needle\n"),
                Search("all-nested-siblings", new(AllTraversal: true,
                        TraversalShape: "nested-siblings", Path: "SRC:"),
                    "SRC: needle ALL",
                    "     SRC (dir)\n" +
                    "        one.txt..\n     2> Needle\n" +
                    "          Sub (dir)\n" +
                    "               Deep (dir)\n" +
                    "                  leaf.txt..\n     2> Needle\n" +
                    "             sub.txt..\n     2> Needle\n" +
                    "          Other (dir)\n" +
                    "             other.txt..\n     2> Needle\n" +
                    "        last.txt..\n     2> Needle\n"),
                Search("quick-match", new(Quick: true),
                    "SRC:one.txt needle QUICK",
                    "SRC:one.txt\u009bK\r\n     2> Needle\n\u009bK"),
                Search("quick-no-match", new(Quick: true,
                        Search: "absent"), "SRC:one.txt absent QUICK",
                    "SRC:one.txt\u009bK\r\u009bK", DOS.RETURN_WARN),
                Search("quick-quiet", new(Quick: true, Quiet: true),
                    "SRC:one.txt needle QUICK QUIET",
                    "SRC:one.txt\u009bK\r\n\u009bK"),
                Search("ctrl-d-abandon", new(CtrlD: true),
                    "SRC:one.txt needle",
                    "     2> Needle\n** File abandoned\n"),
                Search("all-quick", new(AllTraversal: true, Quick: true,
                        Path: "SRC:"), "SRC: needle ALL QUICK",
                    "SRC:one.txt\u009bK\r\n     2> Needle\n\u009bK"),
                Search("all-softlink-file", new(AllTraversal: true,
                        TraversalShape: "softlink-file", Path: "SRC:"),
                    "SRC: needle ALL",
                    "     SRC (dir)\n        LinkFile..\n     2> Needle\n"),
                Search("all-softlink-directory", new(AllTraversal: true,
                        TraversalShape: "softlink-directory", Path: "SRC:"),
                    "SRC: needle ALL",
                    "     SRC (dir)\n          LinkDir (dir)\n" +
                    "             child.txt..\n     2> Needle\n"),
                Search("all-dangling-softlink", new(AllTraversal: true,
                        TraversalShape: "dangling-softlink", Path: "SRC:"),
                    "SRC: needle ALL",
                    "     SRC (dir)\n" +
                    "Warning: Skipping dangling softlink Dangling -> TARGET:\n" +
                    "        Dangling..\n", DOS.RETURN_WARN),
                Search("all-dangling-softlink-quiet", new(AllTraversal: true,
                        Quiet: true, TraversalShape: "dangling-softlink",
                        Path: "SRC:"), "SRC: needle ALL QUIET", "",
                    DOS.RETURN_WARN),
                Search("all-dangling-softlink-quick", new(AllTraversal: true,
                        Quick: true, TraversalShape: "dangling-softlink",
                        Path: "SRC:"), "SRC: needle ALL QUICK",
                    "SRC:Dangling\u009bK\r\u009bK",
                    DOS.RETURN_WARN),
                Search("all-dangling-softlink-file-mode",
                    new(AllTraversal: true, FileMode: true, Search: "Dangling",
                        TraversalShape: "dangling-softlink", Path: "SRC:"),
                    "SRC: Dangling FILE ALL", "SRC:Dangling\n"),
                Search("source-open-failure-is-per-file-miss",
                    new(OpenFailure: true), "SRC:one.txt needle", "",
                    DOS.RETURN_WARN),
                Search("locale-case-map", new(LocaleCaseMap: true,
                        Quiet: true, Search: "Ä", Content: "alpha\nä\n"),
                    "SRC:one.txt Ä QUIET", "SRC:one.txt\n"),
                Search("control-delimiters", new(LinesAfter: 1,
                        CaseSensitive: true, Search: "Needle",
                        Content: "ignored\u0001Needle\rTail\n"),
                    "SRC:one.txt Needle CASE LINES 1",
                    "     1> Needle\n     1: Tail\n"),
                Search("control-delimiters-pattern", new(LinesAfter: 1,
                        PatternMode: true, Search: "eed",
                        Content: "ignored\u0001Needle\rTail\n"),
                    "SRC:one.txt eed PATTERN LINES 1",
                    "     1> Needle\n     1: Tail\n"),
                Search("tab-is-not-a-control-delimiter", new(
                        Content: "foo\tNeedle\n"),
                    "SRC:one.txt needle", "     1> foo.Needle\n"),
                Search("locale-high-byte-control-delimiter", new(
                        LocaleHighByteControl: true, LinesAfter: 1,
                        CaseSensitive: true, Search: "Needle",
                        Content: "ignored\u0085Needle\rTail\n"),
                    "SRC:one.txt Needle CASE LINES 1",
                    "     1> Needle\n     1: Tail\n"),
                Search("locale-high-byte-printable", new(
                        LocaleHighBytePrintable: true, CaseSensitive: true,
                        Search: "Needle", Content: "caf\u00e9 Needle\n"),
                    "SRC:one.txt Needle CASE", "     1> café Needle\n"),
                Search("long-file-abandon", new(CtrlD: true,
                        Content: new string('x', 9000)),
                    "SRC:one.txt needle", "** File abandoned\n",
                    DOS.RETURN_WARN),
                Search("long-file-short-reads", new(Quiet: true,
                        Search: "needle",
                        Content: "Needle\n" + new string('x', 8993),
                        ReadChunkBytes: 4096),
                    "SRC:one.txt needle QUIET", "SRC:one.txt\n"),
                Search("source-buffer-growth-and-prepass", new(Quiet: true,
                        Content: "Needle\n" + new string('x', 524293),
                        ReadChunkBytes: 65536),
                    "SRC:one.txt needle QUIET", "SRC:one.txt\n"),
                Search("source-buffer-halving-recovery", new(Quiet: true,
                        BufferAllocationFailures: 1),
                    "SRC:one.txt needle QUIET", "SRC:one.txt\n"),
                Search("source-seek64-rewind", new(Quiet: true,
                        ReadChunkBytes: 8, Seek64FileSize: true),
                    "SRC:one.txt needle QUIET", "SRC:one.txt\n"),
                Search("source-seek64-start-failure", new(Quiet: true,
                        Seek64FileSize: true, Seek64Failure: true),
                    "SRC:one.txt needle QUIET", "", DOS.RETURN_WARN),
                Search("source-prepass-ctrl-d", new(CtrlD: true,
                        Content: "Needle\n" + new string('x', 524293),
                        ReadChunkBytes: 65536),
                    "SRC:one.txt needle", "** File abandoned\n",
                    DOS.RETURN_WARN),
                Search("source-buffer-allocation-exhaustion", new(
                        BufferAllocationFailure: true),
                    "SRC:one.txt needle", "", DOS.RETURN_WARN),
                Search("locale-library-open-failure",
                    new(LocaleLibraryOpenFailure: true),
                    "SRC:one.txt needle", "", DOS.RETURN_WARN),
                Search("open-locale-failure", new(OpenLocaleFailure: true),
                    "SRC:one.txt needle", "", DOS.RETURN_FAIL,
                    (int)DOS.Error.NoFreeStore)
            ];
        }

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            Search("interleaved-left", new(), "SRC:one.txt needle QUICK",
                "     2> Needle\n"),
            Search("interleaved-right", new(NoNumber: true),
                "SRC:one.txt needle NONUM", "Needle\n")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase Search(string name, SearchEntryCase definition,
        string arguments, string output, int result = DOS.RETURN_OK,
        int error = 0) => new(name, arguments, result, error, output)
    {
        EntryLength = 64,
        Search = definition
    };

    private void PrepareSearchEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Search!;
        var control = invocation.Process + 0x200;
        var from = control + 64;
        var search = control + 80;
        var path = control + 600;
        var lines = control + 1120;
        invocation.SearchEntryLayout = new(control, from, search, path, lines);
        invocation.SearchEntryLayout.FileContent =
            Encoding.Latin1.GetBytes(definition.Content);
        Bus.Long(control + 128, 0x6780u);
        Bus.Long(control + 132, 0x234u);
        PutCString(search, definition.Search);
        PutCString(path, definition.Path);
        Bus.Long(from, path);
        Bus.Long(from + 4, 0);
        Bus.Long(lines, definition.LinesAfter);
    }

    private void RegisterSearchEntryExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "SetSignal", (state,
            invocation) =>
        {
            var definition = invocation.Definition.Search!;
            Require(state.D[0] == 0 &&
                (state.D[1] == 0 || state.D[1] == 1u << 13),
                "Search Ctrl-C/CTRL-D query ABI differs.");
            var call = invocation.SearchSetSignalCalls;
            invocation.SearchSetSignalCalls++;
            if (state.D[1] == 1u << 13 && definition.CtrlD &&
                call == definition.CtrlDSignalCall)
                return 1u << 13;
            if (state.D[1] == 0 && definition.CtrlC) return 1u << 12;
            if (state.D[1] == 1u << 13 && definition.CtrlC)
                return 1u << 12;
            return 0;
        });
        if (suite == SearchEntrySuite)
        {
            Register(SearchLocaleBase, -156, "OpenLocale", (state,
                invocation) =>
            {
                Require(state.A[0] == 0 && invocation.LocaleOpenLocaleCalls == 0,
                    "Search OpenLocale must select the default locale once.");
                invocation.LocaleOpenLocaleCalls++;
                return invocation.Definition.Search!.OpenLocaleFailure
                    ? 0u : SearchLocaleObject;
            });
            Register(SearchLocaleBase, -42, "CloseLocale", (state,
                invocation) =>
            {
                Require(state.A[0] == SearchLocaleObject &&
                    invocation.LocaleOpenLocaleCalls == 1 &&
                    invocation.LocaleCloseLocaleCalls == 0,
                    "Search closed an unowned locale object.");
                invocation.LocaleCloseLocaleCalls++;
                return 0;
            });
            Register(SearchLocaleBase, -54, "ConvToUpper", (state,
                invocation) =>
            {
                Require(state.A[0] == SearchLocaleObject,
                    "Search ConvToUpper locale object differs.");
                invocation.SearchConvToUpperCalls++;
                var value = unchecked((byte)state.D[0]);
                if (value >= (byte)'a' && value <= (byte)'z')
                    value = unchecked((byte)(value - ('a' - 'A')));
                else if (value == 0xe4)
                    value = 0xc4;
                return value;
            });
            Register(SearchLocaleBase, -96, "IsCntrl", (state,
                invocation) =>
            {
                Require(state.A[0] == SearchLocaleObject,
                    "Search IsCntrl locale object differs.");
                invocation.SearchIsCntrlCalls++;
                var value = unchecked((byte)state.D[0]);
                return value < 0x20 || value == 0x7f ||
                    invocation.Definition.Search!.LocaleHighByteControl &&
                    value == 0x85 ? 1u : 0u;
            });
            Register(SearchLocaleBase, -120, "IsPrint", (state,
                invocation) =>
            {
                Require(state.A[0] == SearchLocaleObject,
                    "Search IsPrint locale object differs.");
                invocation.SearchIsPrintCalls++;
                var value = unchecked((byte)state.D[0]);
                return value >= 0x20 && value <= 0x7e ||
                    invocation.Definition.Search!.LocaleHighBytePrintable &&
                    value == 0xe9 ? 1u : 0u;
            });
        }
    }

    private void RegisterSearchEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.Search!;
            var expectedTemplate = suite == WorkbenchSearchEntrySuite
                ? NativeWorkbench31SearchCommand.Template
                : NativeMorphOSSearchCommand.Template;
            var expectedResults = suite == WorkbenchSearchEntrySuite
                ? NativeWorkbench31SearchCommand.ResultCount
                : NativeMorphOSSearchCommand.ResultCount;
            Require(Bus.CString(state.D[1]) == expectedTemplate &&
                state.D[3] == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size ==
                    expectedResults * 4,
                "Search ReadArgs template/storage differs.");
            invocation.SearchReadArgsCalls++;
            var results = state.D[2];
            var layout = invocation.SearchEntryLayout!;
            Bus.Long(results, layout.From);
            Bus.Long(results + 4, layout.Search);
            Bus.Long(results + 8, definition.AllTraversal ? uint.MaxValue : 0);
            Bus.Long(results + 12, definition.NoNumber ? uint.MaxValue : 0);
            Bus.Long(results + 16, definition.Quiet ? uint.MaxValue : 0);
            Bus.Long(results + 20, definition.Quick ? uint.MaxValue : 0);
            Bus.Long(results + 24, definition.FileMode ? uint.MaxValue : 0);
            Bus.Long(results + 28, definition.PatternMode ? uint.MaxValue : 0);
            if (suite != WorkbenchSearchEntrySuite)
            {
                Bus.Long(results + 32,
                    definition.CaseSensitive ? uint.MaxValue : 0);
                if (definition.LinesAfter != 0)
                    Bus.Long(results + 36, layout.Lines);
            }
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            layout.RdArgs = Bus.Allocate(invocation, 128, "SearchRdArgs", true);
            return layout.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            var layout = invocation.SearchEntryLayout!;
            Bus.Release(invocation, state.D[1], "SearchRdArgs", 128);
            invocation.SearchFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.CurrentDir, "CurrentDir", (state,
            invocation) =>
        {
            var layout = invocation.SearchEntryLayout!;
            var previous = layout.CurrentDirectory;
            layout.CurrentDirectory = state.D[1];
            return previous;
        });
        Register(baseAddress, DosLvo.Lock, "Lock", (state, invocation) =>
        {
            var definition = invocation.Definition.Search!;
            Require(definition.TraversalShape.StartsWith("softlink-",
                    StringComparison.Ordinal) ||
                definition.TraversalShape == "dangling-softlink",
                "Search unexpectedly attempted a link lock.");
            var name = Bus.CString(state.D[1]);
            Require(state.D[2] == unchecked((uint)DOS.LockMode.Shared) &&
                name == (definition.TraversalShape == "softlink-file"
                    ? "LinkFile" : definition.TraversalShape == "softlink-directory"
                    ? "LinkDir" : "Dangling"),
                "Search soft-link Lock name or mode differs.");
            invocation.SearchEntryLayout!.SoftLinkLocks++;
            if (definition.TraversalShape == "dangling-softlink")
            {
                invocation.IoError = (int)DOS.Error.ObjectNotFound;
                return 0;
            }
            return 0x3400u;
        });
        Register(baseAddress, DosLvo.Examine64, "Examine64", (state,
            invocation) =>
        {
            var layout = invocation.SearchEntryLayout!;
            Require(state.D[1] == 0x3400u &&
                state.D[2] == layout.Workspace +
                    NativeMorphOSSearchCommand.LinkInfoOffset && state.D[3] == 0,
                "Search soft-link Examine64 lock, FIB, or tags differ.");
            layout.SoftLinkExaminations++;
            Bus.Memory.AsSpan((int)state.D[2], FileInfoBlock.SizeInBytes).Clear();
            var entryType = invocation.Definition.Search!.TraversalShape ==
                "softlink-directory" ? 2 : unchecked((int)-3);
            Bus.Long(state.D[2] + (uint)FileInfoBlock.DirEntryTypeOffset,
                unchecked((uint)entryType));
            var fileSize = (uint)Encoding.Latin1.GetByteCount(
                invocation.Definition.Search.Content);
            Bus.Long(state.D[2] + (uint)FileInfoBlock.SizeOffset, fileSize);
            Bus.Long(state.D[2] + (uint)FileInfoBlock.Size64Offset, 0);
            Bus.Long(state.D[2] + (uint)FileInfoBlock.Size64Offset + 4,
                fileSize);
            PutFileInfoName(state.D[2], "ResolvedTarget");
            return 1;
        });
        Register(baseAddress, DosLvo.Examine, "Examine", (_, _) =>
            throw new InvalidOperationException(
                "Search DOS 51.28 fixture must use Examine64."));
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            Require(state.D[1] == 0x3400u,
                "Search released an unexpected soft-link target lock.");
            invocation.SearchEntryLayout!.SoftLinkUnlocks++;
            return 0;
        });
        Register(baseAddress, DosLvo.GetDeviceProc, "GetDeviceProc",
            (state, invocation) =>
            {
                Require(Bus.CString(state.D[1]) == "" && state.D[2] == 0 &&
                    invocation.Definition.Search!.TraversalShape ==
                        "dangling-softlink",
                    "Search dangling-link device lookup differs.");
                invocation.SearchEntryLayout!.SoftLinkDeviceLookups++;
                invocation.IoError = 901;
                return invocation.SearchEntryLayout.DeviceProc;
            });
        Register(baseAddress, DosLvo.ReadLink, "ReadLink", (state,
            invocation) =>
        {
            var layout = invocation.SearchEntryLayout!;
            Require(state.D[1] == 0x6780u && state.D[2] == 0x234u &&
                Bus.CString(state.D[3]) == "Dangling" &&
                state.D[4] == layout.Workspace +
                    NativeMorphOSSearchCommand.LinkBufferOffset && state.D[5] == 511,
                "Search ReadLink device, path, or buffer ABI differs.");
            Encoding.Latin1.GetBytes("TARGET:").CopyTo(
                Bus.Memory.AsSpan((int)state.D[4]));
            Bus.Memory[state.D[4] + 7] = 0;
            layout.SoftLinkReads++;
            invocation.IoError = 903;
            return 7;
        });
        Register(baseAddress, DosLvo.FreeDeviceProc, "FreeDeviceProc",
            (state, invocation) =>
            {
                Require(state.D[1] == invocation.SearchEntryLayout!.DeviceProc,
                    "Search released a different device process.");
                invocation.SearchEntryLayout.SoftLinkDeviceReleases++;
                invocation.IoError = 904;
                return 0;
            });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state,
            invocation) =>
        {
            var layout = invocation.SearchEntryLayout!;
            Require(state.D[1] != 0 && state.D[2] != 0,
                "Search MatchFirst arguments differ.");
            invocation.SearchMatchFirstCalls++;
            layout.Workspace = state.D[2];
            layout.MatchLive = true;
            layout.MatchNextCalls = 0;
            Bus.Long(state.D[2] + (uint)DosLayout.AnchorPath.Current,
                layout.CurrentChain);
            Bus.Long(layout.CurrentChain + (uint)DosLayout.AChain.Lock,
                0x1200u);
            var sourcePath = invocation.Definition.Search!.Path;
            var sourceBoundary = Math.Max(sourcePath.LastIndexOf('/'),
                sourcePath.LastIndexOf(':'));
            layout.DirectoryPaths[0x1200u] = sourceBoundary < 0 ? "" :
                sourcePath[..(sourceBoundary + 1)];
            if (invocation.Definition.Search!.MatchFailure)
                return (uint)DOS.Error.NoMoreEntries;
            PutSearchFib(invocation, layout);
            if (invocation.Definition.Search.AllTraversal)
            {
                var anchorFlags = state.D[2] +
                    (uint)DosLayout.AnchorPath.Flags;
                Bus.Memory[anchorFlags] = 0;
                Bus.Long(state.D[2] + (uint)DosLayout.AnchorPath.Info +
                    (uint)FileInfoBlock.DirEntryTypeOffset, 2);
                PutSearchFileName(state.D[2], "SRC");
            }
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state,
            invocation) =>
        {
            var layout = invocation.SearchEntryLayout!;
            Require(layout.MatchLive && state.D[1] == layout.Workspace,
                "Search MatchNext lifetime differs.");
            layout.MatchNextCalls++;
            invocation.SearchMatchNextCalls++;
            if (invocation.Definition.Search!.AllTraversal)
            {
                var anchor = layout.Workspace;
                var shape = invocation.Definition.Search.TraversalShape;
                if (shape is "softlink-file" or "dangling-softlink")
                {
                    if (layout.MatchNextCalls == 1)
                    {
                        var name = shape == "softlink-file"
                            ? "LinkFile" : "Dangling";
                        Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] =
                            shape == "softlink-file"
                                ? (byte)AnchorPathFlags.DirectoryChanged : (byte)0;
                        Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
                            (uint)FileInfoBlock.DirEntryTypeOffset,
                            (uint)DosConstants.SoftLink);
                        Bus.Long(layout.CurrentChain +
                            (uint)DosLayout.AChain.Lock, 0x1200u);
                        layout.DirectoryPaths[0x1200u] = "SRC:";
                        PutSearchFileName(anchor, name);
                        PutSearchFileSize(anchor,
                            invocation.Definition.Search!.Content);
                        PutSearchPath(anchor, "SRC:" + name);
                        return 0;
                    }
                    Require(layout.MatchNextCalls == 2,
                        "Search traversed beyond its supplied soft-link entry.");
                    return (uint)DOS.Error.NoMoreEntries;
                }
                if (shape == "softlink-directory")
                {
                    var softLinkFlagsAddress = anchor +
                        (uint)DosLayout.AnchorPath.Flags;
                    if (layout.MatchNextCalls == 1)
                    {
                        Bus.Memory[softLinkFlagsAddress] = 0;
                        Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
                            (uint)FileInfoBlock.DirEntryTypeOffset,
                            (uint)DosConstants.SoftLink);
                        Bus.Long(layout.CurrentChain +
                            (uint)DosLayout.AChain.Lock, 0x1200u);
                        PutSearchFileName(anchor, "LinkDir");
                        PutSearchPath(anchor, "SRC:LinkDir");
                        return 0;
                    }
                    if (layout.MatchNextCalls == 2)
                    {
                        Require((Bus.Memory[softLinkFlagsAddress] &
                            (byte)AnchorPathFlags.DoDirectory) != 0,
                            "Search did not descend through the soft-linked directory.");
                        Bus.Memory[softLinkFlagsAddress] =
                            (byte)AnchorPathFlags.DirectoryChanged;
                        Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
                            (uint)FileInfoBlock.DirEntryTypeOffset,
                            unchecked((uint)-3));
                        Bus.Long(layout.CurrentChain +
                            (uint)DosLayout.AChain.Lock, 0x1600u);
                        layout.DirectoryPaths[0x1600u] = "SRC:LinkDir/";
                        PutSearchFileName(anchor, "child.txt");
                        PutSearchFileSize(anchor,
                            invocation.Definition.Search!.Content);
                        PutSearchPath(anchor, "SRC:LinkDir/child.txt");
                        return 0;
                    }
                    if (layout.MatchNextCalls == 3)
                    {
                        Bus.Memory[softLinkFlagsAddress] =
                            (byte)AnchorPathFlags.DidDirectory;
                        Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
                            (uint)FileInfoBlock.DirEntryTypeOffset, 2);
                        Bus.Long(layout.CurrentChain +
                            (uint)DosLayout.AChain.Lock, 0x1200u);
                        PutSearchFileName(anchor, "LinkDir");
                        PutSearchPath(anchor, "SRC:LinkDir");
                        return 0;
                    }
                    if (layout.MatchNextCalls == 4)
                    {
                        Bus.Memory[softLinkFlagsAddress] =
                            (byte)AnchorPathFlags.DidDirectory;
                        Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
                            (uint)FileInfoBlock.DirEntryTypeOffset, 2);
                        Bus.Long(layout.CurrentChain +
                            (uint)DosLayout.AChain.Lock, 0x1200u);
                        PutSearchFileName(anchor, "SRC");
                        PutSearchPath(anchor, "SRC:");
                        return 0;
                    }
                    Require(layout.MatchNextCalls == 5,
                        "Search traversed beyond the soft-linked directory exits.");
                    return (uint)DOS.Error.NoMoreEntries;
                }
                if (invocation.Definition.Search.TraversalShape ==
                    "nested-siblings")
                {
                    SearchTraversalStep[] steps =
                    [
                        new("one.txt", -3,
                            (byte)AnchorPathFlags.DirectoryChanged,
                            0x1200u, "SRC:"),
                        new("Sub", 2, 0, 0x1200u, "SRC:"),
                        new("Deep", 2,
                            (byte)AnchorPathFlags.DirectoryChanged,
                            0x1300u, "SRC:Sub/"),
                        new("leaf.txt", -3,
                            (byte)AnchorPathFlags.DirectoryChanged,
                            0x1400u, "SRC:Sub/Deep/"),
                        new("Deep", 2,
                            (byte)AnchorPathFlags.DidDirectory,
                            0x1300u, "SRC:Sub/"),
                        new("sub.txt", -3,
                            (byte)AnchorPathFlags.DirectoryChanged,
                            0x1300u, "SRC:Sub/"),
                        new("Sub", 2,
                            (byte)AnchorPathFlags.DidDirectory,
                            0x1200u, "SRC:"),
                        new("Other", 2, 0, 0x1200u, "SRC:"),
                        new("other.txt", -3,
                            (byte)AnchorPathFlags.DirectoryChanged,
                            0x1500u, "SRC:Other/"),
                        new("Other", 2,
                            (byte)AnchorPathFlags.DidDirectory,
                            0x1200u, "SRC:"),
                        new("last.txt", -3,
                            (byte)AnchorPathFlags.DirectoryChanged,
                            0x1200u, "SRC:"),
                        new("SRC", 2,
                            (byte)AnchorPathFlags.DidDirectory,
                            0x1200u, "SRC:")
                    ];
                    if (layout.MatchNextCalls <= steps.Length)
                    {
                        var step = steps[layout.MatchNextCalls - 1];
                        Bus.Memory[anchor +
                            (uint)DosLayout.AnchorPath.Flags] = step.Flags;
                        Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
                            (uint)FileInfoBlock.DirEntryTypeOffset,
                            unchecked((uint)step.EntryType));
                        Bus.Long(layout.CurrentChain +
                            (uint)DosLayout.AChain.Lock, step.DirectoryLock);
                        layout.DirectoryPaths[step.DirectoryLock] =
                            step.DirectoryPath;
                        PutSearchFileName(anchor, step.Name);
                        if (step.EntryType < 0)
                            PutSearchFileSize(anchor,
                                invocation.Definition.Search!.Content);
                        PutSearchPath(anchor, step.DirectoryPath + step.Name);
                        return 0;
                    }
                    Require(layout.MatchNextCalls == steps.Length + 1,
                        "Search ALL traversed beyond nested directory exits.");
                    return (uint)DOS.Error.NoMoreEntries;
                }
                var flagsAddress = anchor + (uint)DosLayout.AnchorPath.Flags;
                if (layout.MatchNextCalls == 1)
                {
                    Require((Bus.Memory[flagsAddress] &
                        (byte)AnchorPathFlags.DoDirectory) != 0,
                        "Search ALL did not request AnchorPath directory descent.");
                    Bus.Memory[flagsAddress] =
                        (byte)AnchorPathFlags.DirectoryChanged;
                    Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
                        (uint)FileInfoBlock.DirEntryTypeOffset,
                        unchecked((uint)-3));
                    PutSearchFileName(anchor, "one.txt");
                    PutSearchFileSize(anchor,
                        invocation.Definition.Search!.Content);
                    PutSearchPath(anchor, "SRC:one.txt");
                    return 0;
                }
                if (layout.MatchNextCalls == 2)
                {
                    Bus.Memory[flagsAddress] =
                        (byte)AnchorPathFlags.DidDirectory;
                    Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
                        (uint)FileInfoBlock.DirEntryTypeOffset, 2);
                    PutSearchFileName(anchor, "SRC");
                    PutSearchPath(anchor, "SRC:");
                    return 0;
                }
                Require(layout.MatchNextCalls == 3,
                    "Search ALL traversed beyond the supplied directory exit.");
                return (uint)DOS.Error.NoMoreEntries;
            }
            return (uint)DOS.Error.NoMoreEntries;
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state,
            invocation) =>
        {
            var layout = invocation.SearchEntryLayout!;
            Require(state.D[1] == layout.Workspace && layout.MatchLive,
                "Search MatchEnd workspace differs.");
            layout.MatchLive = false;
            invocation.SearchMatchEndCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.OpenRaw, "OpenRaw", (state,
            invocation) =>
        {
            Require(state.D[2] == (uint)DOS.FileMode.OldFile,
                "Search file mode differs.");
            invocation.SearchFileOpenCalls++;
            invocation.SearchReadOffset = 0;
            if (invocation.Definition.Search!.OpenFailure)
            {
                invocation.IoError = 205;
                return 0;
            }
            var traversal = invocation.Definition.Search!.TraversalShape;
            if (traversal == "softlink-file")
                Require(Bus.CString(state.D[1]) == "SRC:LinkFile",
                    "Search did not open the linked file through its name.");
            else if (traversal == "softlink-directory")
                Require(Bus.CString(state.D[1]) == "SRC:LinkDir/child.txt",
                    "Search did not open a file beneath the linked directory.");
            else if (traversal == "dangling-softlink")
            {
                var actualPath = Bus.CString(state.D[1]);
                Require(actualPath == "SRC:Dangling",
                    $"Search did not preserve the dangling link path: '{actualPath}'.");
                invocation.IoError = (int)DOS.Error.ObjectNotFound;
                return 0;
            }
            return 0x900u + (uint)invocation.Slot;
        });
        Register(baseAddress, DosLvo.Read, "Read", (state, invocation) =>
        {
            var definition = invocation.Definition.Search!;
            var layout = invocation.SearchEntryLayout!;
            var morphos = suite == SearchEntrySuite;
            var expectedInput = layout.FileBuffer != 0
                ? layout.FileBuffer
                : layout.Workspace + (uint)DosLayout.AnchorPath.Size +
                    512u + 1024u + 1024u;
            if (!morphos)
                expectedInput += unchecked((uint)invocation.SearchReadOffset);
            Require(state.D[1] == 0x900u + (uint)invocation.Slot &&
                state.D[2] == expectedInput,
                "Search Read buffer differs.");
            invocation.SearchReadCalls++;
            if (definition.ReadFailure)
            {
                invocation.IoError = 205;
                return unchecked((uint)-1);
            }
            var bytes = layout.FileContent;
            var remaining = bytes.Length - invocation.SearchReadOffset;
            var expectedCapacity = morphos
                ? layout.FileBufferBytes - 1u
                : remaining == 0 ? 8192u : unchecked((uint)remaining);
            Require(state.D[3] == expectedCapacity,
                $"Search Read capacity differs ({definition.Content.Length} bytes, offset {invocation.SearchReadOffset}, requested {state.D[3]}, expected {expectedCapacity}, FIB size {Bus.Long(invocation.SearchEntryLayout.Workspace + (uint)DosLayout.AnchorPath.Info + FileInfoBlock.SizeOffset)}, FIB64 low {Bus.Long(invocation.SearchEntryLayout.Workspace + (uint)DosLayout.AnchorPath.Info + FileInfoBlock.Size64Offset + 4)}).");
            var count = Math.Min(unchecked((int)state.D[3]), remaining);
            if (definition.ReadChunkBytes > 0)
                count = Math.Min(count, definition.ReadChunkBytes);
            if (count > 0)
            {
                bytes.AsSpan(invocation.SearchReadOffset, count).CopyTo(
                    Bus.Memory.AsSpan((int)state.D[2]));
                invocation.SearchReadOffset += count;
            }
            return unchecked((uint)count);
        });
        Register(baseAddress, DosLvo.Seek, "Seek", (state, invocation) =>
        {
            Require(state.D[1] == 0x900u + (uint)invocation.Slot,
                "Search Seek file handle differs.");
            var layout = invocation.SearchEntryLayout!;
            var previous = invocation.SearchReadOffset;
            layout.SearchSeekCalls++;
            var position = unchecked((int)state.D[2]);
            var mode = unchecked((int)state.D[3]);
            int next;
            if (mode == (int)DosConstants.OffsetBeginning)
                next = position;
            else if (mode == (int)DosConstants.OffsetCurrent)
                next = previous + position;
            else
                throw new InvalidOperationException(
                    "Search used an unsupported Seek origin.");
            Require(next >= 0 && next <= layout.FileContent.Length,
                $"Search Seek target {next} is outside the fixture file.");
            invocation.SearchReadOffset = next;
            return unchecked((uint)previous);
        });
        Register(baseAddress, DosLvo.Seek64, "Seek64", (state, invocation) =>
        {
            var layout = invocation.SearchEntryLayout!;
            var definition = invocation.Definition.Search!;
            Require(definition.Seek64FileSize &&
                state.D[1] == 0x900u + (uint)invocation.Slot,
                "Search used Seek64 outside the >2 GiB DOS64 fixture.");
            var positionHigh = state.D[2];
            var positionLow = state.D[3];
            var mode = unchecked((int)state.D[4]);
            layout.SearchSeek64Calls++;
            layout.SearchSeek64Requests.Add((positionHigh, positionLow,
                mode));
            if (definition.Seek64Failure)
            {
                invocation.IoError = 205;
                state.D[1] = uint.MaxValue;
                return uint.MaxValue;
            }

            var displacement = unchecked((long)(
                ((ulong)positionHigh << 32) | positionLow));
            var previous = invocation.SearchReadOffset;
            var next = mode == (int)DosConstants.OffsetBeginning
                ? checked((int)displacement)
                : mode == (int)DosConstants.OffsetCurrent
                    ? checked(previous + checked((int)displacement))
                    : throw new InvalidOperationException(
                        "Search Seek64 used an unsupported origin.");
            Require(next >= 0 && next <= layout.FileContent.Length,
                $"Search Seek64 target {next} is outside the fixture file.");
            invocation.SearchReadOffset = next;
            state.D[1] = unchecked((uint)previous);
            return 0;
        }, preserveD1: true);
        Register(baseAddress, DosLvo.Close, "Close", (state, invocation) =>
        {
            Require(state.D[1] == 0x900u + (uint)invocation.Slot,
                "Search closed an unexpected file handle.");
            invocation.SearchFileCloseCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.NameFromLock, "NameFromLock", (state,
            invocation) =>
        {
            var layout = invocation.SearchEntryLayout!;
            Require(state.D[1] != 0 &&
                state.D[2] == layout.Workspace +
                    (uint)DosLayout.AnchorPath.Size && state.D[3] == 512,
                "Search did not resolve the current directory through NameFromLock.");
            if (!layout.DirectoryPaths.TryGetValue(state.D[1], out var parent))
                throw new InvalidOperationException(
                    "Search used an unknown current-directory lock.");
            PutCString(state.D[2], parent);
            return 1;
        });
        Register(baseAddress, DosLvo.AddPart, "AddPart", (state,
            invocation) =>
        {
            Require(state.D[1] == invocation.SearchEntryLayout!.Workspace +
                (uint)DosLayout.AnchorPath.Size && state.D[3] == 512,
                "Search AddPart capacity differs.");
            var parent = Bus.CString(state.D[1]);
            var child = Bus.CString(state.D[2]);
            var separator = parent.Length != 0 &&
                parent[^1] != ':' && parent[^1] != '/' ? "/" : "";
            var combined = parent + separator + child;
            if (combined.Length >= state.D[3])
            {
                invocation.IoError = (int)DOS.Error.LineTooLong;
                return 0;
            }
            invocation.SearchEntryLayout!.BuiltPaths.Add(combined);
            PutCString(state.D[1], combined);
            return 1;
        });
        Register(baseAddress, DosLvo.ParsePattern, "ParsePattern", (state,
            invocation) =>
        {
            invocation.SearchParsePatternCalls++;
            Require(state.D[2] == invocation.SearchEntryLayout!.Workspace +
                (uint)DosLayout.AnchorPath.Size + 512u && state.D[3] == 1024,
                "Search ParsePattern storage differs.");
            var source = Bus.CString(state.D[1]);
            var bytes = Encoding.Latin1.GetBytes(source);
            bytes.CopyTo(Bus.Memory.AsSpan((int)state.D[2]));
            Bus.Memory[state.D[2] + (uint)bytes.Length] = 0;
            return (uint)bytes.Length;
        });
        Register(baseAddress, DosLvo.ParsePatternNoCase, "ParsePatternNoCase",
            (state, invocation) =>
        {
            invocation.SearchParsePatternCalls++;
            Require(state.D[2] == invocation.SearchEntryLayout!.Workspace +
                (uint)DosLayout.AnchorPath.Size + 512u && state.D[3] == 1024,
                "Search ParsePatternNoCase storage differs.");
            var source = Bus.CString(state.D[1]);
            var bytes = Encoding.Latin1.GetBytes(source);
            bytes.CopyTo(Bus.Memory.AsSpan((int)state.D[2]));
            Bus.Memory[state.D[2] + (uint)bytes.Length] = 0;
            return (uint)bytes.Length;
        });
        Register(baseAddress, DosLvo.MatchPattern, "MatchPattern", (state,
            invocation) =>
        {
            invocation.SearchMatchPatternCalls++;
            return Glob(Bus.CString(state.D[1]), Bus.CString(state.D[2])) ? 1u : 0u;
        });
        Register(baseAddress, DosLvo.MatchPatternNoCase,
            "MatchPatternNoCase", (state, invocation) =>
        {
            invocation.SearchMatchPatternCalls++;
            return Glob(Bus.CString(state.D[1]), Bus.CString(state.D[2])) ? 1u : 0u;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            var value = Bus.CString(state.D[1]);
            invocation.Output.Write(Encoding.Latin1.GetBytes(value));
            return (uint)value.Length;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            var format = Bus.CString(state.D[1]);
            if (format == "%6ld")
            {
                var number = Bus.Long(state.D[2]);
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    string.Format("{0,6}", number)));
                return 0;
            }
            Require(format ==
                "Warning: Skipping dangling softlink %s -> %s\n" &&
                Bus.CString(Bus.Long(state.D[2])) == "Dangling" &&
                Bus.CString(Bus.Long(state.D[2] + 4)) == "TARGET:",
                "Search dangling-link warning arguments differ.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                "Warning: Skipping dangling softlink Dangling -> TARGET:\n"));
            invocation.SearchEntryLayout!.SoftLinkWarnings++;
            return 0;
        });
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            var markerText = Bus.Memory[state.D[2]] == (byte)'>' ? "> " : ": ";
            Require(state.D[1] == invocation.OutputBptr &&
                (Bus.Memory[state.D[2]] == (byte)'>' || Bus.Memory[state.D[2]] == (byte)':') &&
                Bus.Memory[state.D[2] + 1] == (byte)' ',
                "Search line marker output differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(markerText));
            return 0;
        });
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr && state.D[2] != 0 &&
                state.D[3] > 0 && state.D[3] <= 8192,
                "Search Write arguments differ.");
            invocation.SearchWriteCalls++;
            invocation.Output.Write(Bus.Memory, (int)state.D[2],
                checked((int)state.D[3]));
            return state.D[3];
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (_, invocation) =>
        {
            invocation.SearchPrintFaultCalls++;
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

    private void PutSearchFib(Invocation invocation, SearchEntryNativeLayout layout)
    {
        var fib = layout.Workspace + (uint)DosLayout.AnchorPath.Info;
        Bus.Long(fib + (uint)FileInfoBlock.DirEntryTypeOffset,
            unchecked((uint)-3));
        Bus.Long(fib + (uint)FileInfoBlock.FileNameOffset,
            0);
        var fileSize = (uint)Encoding.Latin1.GetByteCount(
            invocation.Definition.Search!.Content);
        var seek64File = invocation.Definition.Search.Seek64FileSize;
        Bus.Long(fib + (uint)FileInfoBlock.SizeOffset, fileSize);
        Bus.Long(fib + (uint)FileInfoBlock.Size64Offset,
            seek64File ? 1u : 0u);
        Bus.Long(fib + (uint)FileInfoBlock.Size64Offset + 4, fileSize);
        var bytes = Encoding.Latin1.GetBytes(invocation.Definition.Search!.Path);
        bytes.CopyTo(Bus.Memory.AsSpan((int)(layout.Workspace +
            (uint)DosLayout.AnchorPath.PathBuffer)));
        Bus.Memory[layout.Workspace + (uint)DosLayout.AnchorPath.PathBuffer +
            (uint)bytes.Length] = 0;
        bytes = Encoding.Latin1.GetBytes(FilePart(invocation.Definition.Search.Path));
        bytes.CopyTo(Bus.Memory.AsSpan((int)fib + FileInfoBlock.FileNameOffset));
        Bus.Memory[(uint)fib + (uint)FileInfoBlock.FileNameOffset +
            (uint)bytes.Length] = 0;
    }

    private void PutSearchFileName(uint anchor, string value)
    {
        var address = anchor + (uint)DosLayout.AnchorPath.Info +
            (uint)FileInfoBlock.FileNameOffset;
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }

    private void PutSearchFileSize(uint anchor, string content)
    {
        var fileSize = (uint)Encoding.Latin1.GetByteCount(content);
        var fib = anchor + (uint)DosLayout.AnchorPath.Info;
        Bus.Long(fib + (uint)FileInfoBlock.SizeOffset, fileSize);
        Bus.Long(fib + (uint)FileInfoBlock.Size64Offset, 0);
        Bus.Long(fib + (uint)FileInfoBlock.Size64Offset + 4, fileSize);
    }

    private void PutFileInfoName(uint fib, string value)
    {
        var address = fib + (uint)FileInfoBlock.FileNameOffset;
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }

    private void PutSearchPath(uint anchor, string value)
    {
        var address = anchor + (uint)DosLayout.AnchorPath.PathBuffer;
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }

    private void VerifySearchEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Search!;
        var layout = invocation.SearchEntryLayout!;
        if (invocation.Definition.Workbench ||
            invocation.Definition.MissingDos ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer)
        {
            Require(invocation.SearchReadArgsCalls == 0 &&
                invocation.SearchMatchFirstCalls == 0,
                "Search crossed an invalid startup boundary.");
            return;
        }
        if (definition.LocaleLibraryOpenFailure || definition.OpenLocaleFailure)
        {
            Require(invocation.SearchReadArgsCalls == 0 &&
                invocation.SearchFreeArgsCalls == 0 &&
                invocation.SearchAllocMemCalls == 0 &&
                invocation.SearchFreeMemCalls == 0 &&
                invocation.SearchMatchFirstCalls == 0,
                "Search locale failure leaked or crossed into traversal.");
            Require(invocation.LocaleOpens == 1 &&
                invocation.LocaleCloses == (definition.LocaleLibraryOpenFailure ? 0 : 1) &&
                invocation.LocaleOpenLocaleCalls == (definition.OpenLocaleFailure ? 1 : 0) &&
                invocation.LocaleCloseLocaleCalls == 0,
                "Search locale failure ownership differs.");
            Require(invocation.SearchPrintFaultCalls ==
                (definition.OpenLocaleFailure ? 1 : 0),
                "Search locale failure published the wrong diagnostic.");
            return;
        }
        Require((suite == SearchEntrySuite && invocation.LocaleOpens == 1 &&
                 invocation.LocaleCloses == 1 &&
                 invocation.LocaleOpenLocaleCalls == 1 &&
                 invocation.LocaleCloseLocaleCalls == 1) ||
                (suite == WorkbenchSearchEntrySuite &&
                 invocation.LocaleOpens == 0 && invocation.LocaleCloses == 0 &&
                 invocation.LocaleOpenLocaleCalls == 0 &&
                 invocation.LocaleCloseLocaleCalls == 0),
            "Search profile locale-library ownership differs.");
        if (definition.ParserError != 0 || definition.AllocationFailure)
        {
            Require(invocation.SearchMatchFirstCalls == 0,
                "Search traversed after an early failure.");
            return;
        }
        if (definition.BufferAllocationFailure)
        {
            var requested = (uint)layout.FileContent.Length + 1u;
            if (requested >= 512u * 1024u + 1u)
                requested = 512u * 1024u + 1u;
            var failedAttempts = 0;
            while (requested != 0)
            {
                failedAttempts++;
                requested >>= 1;
            }
            Require(suite == SearchEntrySuite &&
                invocation.SearchReadArgsCalls == 1 &&
                invocation.SearchFreeArgsCalls == 1 &&
                invocation.SearchAllocMemCalls == 2 + failedAttempts &&
                invocation.SearchFreeMemCalls == 2 &&
                invocation.SearchMatchFirstCalls == 1 &&
                invocation.SearchMatchEndCalls == 1 &&
                invocation.SearchFileOpenCalls == 1 &&
                invocation.SearchFileCloseCalls == 1 &&
                invocation.SearchReadCalls == 0,
                "Search did not exhaust source-sized Any memory requests and skip the file cleanly.");
            return;
        }
        var longestLfLine = LongestLfLineLength(layout.FileContent);
        var morphosResize = !definition.CtrlD &&
            layout.FileContent.Length >= 512u * 1024u + 1u &&
            longestLfLine >= 512u * 1024u + 1u;
        var allocationsPerMorphosFile = morphosResize ? 2 : 1;
        var expectedAllocations = suite == SearchEntrySuite
            ? 2 + invocation.SearchFileCloseCalls *
                (allocationsPerMorphosFile + definition.BufferAllocationFailures)
            : 2 + (layout.FileContent.Length >= 8192
                ? invocation.SearchFileCloseCalls : 0);
        var expectedFrees = suite == SearchEntrySuite
            ? 2 + invocation.SearchFileCloseCalls * allocationsPerMorphosFile
            : expectedAllocations;
        Require(invocation.SearchReadArgsCalls == 1 &&
            invocation.SearchFreeArgsCalls == 1 &&
            invocation.SearchAllocMemCalls == expectedAllocations &&
            invocation.SearchFreeMemCalls == expectedFrees &&
            (suite != SearchEntrySuite ||
                layout.FileBufferAllocationCount ==
                    allocationsPerMorphosFile * invocation.SearchFileCloseCalls) &&
            invocation.SearchMatchFirstCalls == 1 &&
            invocation.SearchMatchEndCalls == 1,
            $"Search parser/workspace/matcher ownership differs for {invocation.Definition.Name} (alloc={invocation.SearchAllocMemCalls}/{expectedAllocations}, free={invocation.SearchFreeMemCalls}/{expectedAllocations}, opens={invocation.SearchFileOpenCalls}).");
        if (suite == SearchEntrySuite &&
            definition.BufferAllocationFailures != 0)
        {
            var firstRequest = (uint)layout.FileContent.Length + 1u;
            if (firstRequest >= 512u * 1024u + 1u)
                firstRequest = 512u * 1024u + 1u;
            var expectedRequests = new List<uint>();
            for (var file = 0; file < invocation.SearchFileCloseCalls; file++)
            {
                var request = firstRequest;
                for (var attempt = 0;
                    attempt <= definition.BufferAllocationFailures; attempt++)
                {
                    expectedRequests.Add(request);
                    if (attempt < definition.BufferAllocationFailures)
                        request >>= 1;
                }
            }
            Require(invocation.AllocationRequests.Skip(2)
                    .SequenceEqual(expectedRequests),
                "Search did not halve failed MEMF_ANY requests before recovery.");
        }
        if (definition.AllTraversal)
        {
            var expectedMatchNextCalls = definition.TraversalShape switch
            {
                "nested-siblings" => 13,
                "softlink-directory" => 5,
                "softlink-file" => 2,
                "dangling-softlink" when definition.FileMode => 2,
                "dangling-softlink" when definition.Quiet || definition.Quick => 2,
                "dangling-softlink" => 2,
                _ => 3
            };
            Require(invocation.SearchMatchNextCalls == expectedMatchNextCalls,
                "Search ALL did not consume every supplied entry, directory exit, and end.");
        }
        if (definition.TraversalShape is "softlink-file" or
            "softlink-directory")
        {
            Require(layout.SoftLinkLocks == 1 &&
                layout.SoftLinkExaminations == 1 &&
                layout.SoftLinkUnlocks == 1 &&
                layout.CurrentDirectory == 0x1000u,
                "Search did not classify and release its soft-link target lock or restore CurrentDir.");
        }
        if (definition.TraversalShape == "dangling-softlink")
        {
            var quietLink = definition.FileMode || definition.Quiet ||
                definition.Quick;
            Require(layout.SoftLinkLocks == 1 &&
                layout.SoftLinkExaminations == 0 &&
                layout.SoftLinkUnlocks == 0 &&
                layout.SoftLinkDeviceLookups == 1 &&
                layout.SoftLinkReads == 1 &&
                layout.SoftLinkDeviceReleases == 1 &&
                layout.SoftLinkWarnings == (quietLink ? 0 : 1) &&
                layout.CurrentDirectory == 0x1000u,
                "Search dangling-link warning, quiet policy, ownership, or CurrentDir restoration differs.");
        }
        if (definition.MatchFailure)
            Require(invocation.SearchFileOpenCalls == 0,
                "Search opened a file after an empty match.");
        else if (definition.OpenFailure)
            Require(suite == SearchEntrySuite &&
                invocation.SearchFileOpenCalls == 1 &&
                invocation.SearchFileCloseCalls == 0 &&
                invocation.SearchReadCalls == 0 &&
                invocation.SearchMatchNextCalls == 1,
                "MorphOS Search did not continue traversal after a per-file Open failure.");
        else if (definition.FileMode)
            Require(invocation.SearchFileOpenCalls == 0 &&
                invocation.SearchMatchPatternCalls > 0,
                "Search FILE did not use DOS pattern matching.");
        else if (definition.TraversalShape == "dangling-softlink" &&
            !definition.FileMode)
            Require(invocation.SearchFileOpenCalls == 1 &&
                invocation.SearchFileCloseCalls == 0 &&
                invocation.SearchReadCalls == 0,
                "Search did not stop after opening a dangling-link path failed.");
        else if (definition.TraversalShape == "nested-siblings")
            Require(invocation.SearchFileOpenCalls == 5 &&
                invocation.SearchFileCloseCalls == 5 &&
                invocation.SearchReadCalls ==
                    (suite == SearchEntrySuite ? 10 : 5),
                "Search did not process each nested and sibling file exactly once.");
        else if (definition.TraversalShape is "softlink-file" or
            "softlink-directory")
            Require(invocation.SearchFileOpenCalls == 1 &&
                invocation.SearchFileCloseCalls == 1 &&
                invocation.SearchReadCalls ==
                    (suite == SearchEntrySuite ? 2 : 1),
                "Search did not read the file reached through a soft link.");
        else
        {
            var expectedReads = definition.OpenFailure ? 0 :
                suite == SearchEntrySuite
                ? definition.CtrlC || definition.CtrlD ||
                    definition.ReadFailure || definition.Quiet ? 1 : 2
                : 1;
            if (suite == SearchEntrySuite && definition.Seek64FileSize)
            {
                var readChunk = definition.ReadChunkBytes == 0
                    ? 512 * 1024 : definition.ReadChunkBytes;
                var prepassReads = (layout.FileContent.Length + readChunk - 1) /
                    readChunk + 1;
                var matchReads = definition.Seek64Failure ? 0 :
                    definition.ReadChunkBytes == 0 ? 1 : 2;
                expectedReads = prepassReads + matchReads;
            }
            else if (suite == SearchEntrySuite &&
                definition.BufferAllocationFailures != 0)
            {
                var initialBufferBytes =
                    (uint)layout.FileContent.Length + 1u;
                for (var index = 0;
                     index < definition.BufferAllocationFailures; index++)
                    initialBufferBytes >>= 1;
                var prepassChunk = unchecked((int)(initialBufferBytes - 1u));
                var prepassReads = (layout.FileContent.Length +
                    prepassChunk - 1) / prepassChunk + 1;
                expectedReads = prepassReads + 2;
            }
            if (suite == SearchEntrySuite &&
                layout.FileContent.Length >= 512u * 1024u + 1u)
            {
                if (definition.CtrlD)
                {
                    expectedReads = 1;
                }
                else
                {
                    var prepassChunk = definition.ReadChunkBytes == 0
                        ? 512 * 1024 : Math.Min(512 * 1024,
                            definition.ReadChunkBytes);
                    var prepassReads = (layout.FileContent.Length +
                        prepassChunk - 1) / prepassChunk + 1;
                    var matchReads = definition.CtrlC || definition.CtrlD ||
                        definition.ReadFailure || definition.Quiet ? 1 : 2;
                    expectedReads = prepassReads + matchReads;
                }
            }
            else if (suite == WorkbenchSearchEntrySuite &&
                definition.ReadChunkBytes > 0)
            {
                expectedReads = layout.FileContent.Length == 0 ? 1 :
                    (layout.FileContent.Length + definition.ReadChunkBytes - 1) /
                        definition.ReadChunkBytes;
            }
            Require(invocation.SearchFileOpenCalls == 1 &&
                invocation.SearchFileCloseCalls == 1 &&
                invocation.SearchReadCalls == expectedReads,
                $"Search file ownership/read cadence differs for {invocation.Definition.Name} (opens={invocation.SearchFileOpenCalls}, closes={invocation.SearchFileCloseCalls}, reads={invocation.SearchReadCalls}, expected={expectedReads}).");
            if (suite == SearchEntrySuite)
            {
                if (definition.Seek64FileSize)
                {
                    var expectedSeek64Calls = definition.Seek64Failure ? 1 :
                        definition.ReadChunkBytes == 0 ? 2 : 3;
                    Require(layout.SearchSeek64Calls == expectedSeek64Calls &&
                        layout.SearchSeekCalls == 0 &&
                        layout.SearchSeek64Requests.Count ==
                            expectedSeek64Calls &&
                        layout.SearchSeek64Requests[0] ==
                            (0u, 0u, (int)DosConstants.OffsetBeginning),
                        "Search did not use the DOS64 absolute file rewind.");
                    if (!definition.Seek64Failure &&
                        definition.ReadChunkBytes != 0)
                    {
                        Require(layout.SearchSeek64Requests[1] ==
                                (uint.MaxValue, 0xffff_fffeu,
                                    (int)DosConstants.OffsetCurrent) &&
                            layout.SearchSeek64Requests[2] ==
                                (uint.MaxValue, uint.MaxValue,
                                    (int)DosConstants.OffsetCurrent),
                            "Search DOS64 relative rewind lost its sign extension.");
                    }
                    else if (!definition.Seek64Failure)
                    {
                        Require(layout.SearchSeek64Requests[1] ==
                                (0u, 0u, (int)DosConstants.OffsetCurrent),
                            "Search DOS64 matched-line completion rewind differs.");
                    }
                }
                else
                {
                    Require(layout.SearchSeek64Calls == 0,
                        "Search used DOS64 for a file within the classic range.");
                }
            }
        }
        if (definition.TraversalShape == "nested-siblings")
            Require(layout.BuiltPaths.SequenceEqual(new[]
                {
                    "SRC:one.txt", "SRC:Sub/Deep/leaf.txt",
                    "SRC:Sub/sub.txt", "SRC:Other/other.txt",
                    "SRC:last.txt"
                }), "Search ALL reconstructed nested file paths incorrectly.");
        if (definition.TraversalShape == "softlink-file")
            Require(layout.BuiltPaths.SequenceEqual(new[] { "SRC:LinkFile" }),
                "Search replaced a soft-link filename with its target name.");
        if (definition.TraversalShape == "softlink-directory")
            Require(layout.BuiltPaths.SequenceEqual(
                new[] { "SRC:LinkDir/child.txt" }),
                "Search lost the path beneath a soft-linked directory.");
        Require(definition.AllTraversal || definition.CtrlC ||
            definition.MatchFailure || definition.ReadFailure ||
            definition.Seek64Failure ||
            invocation.SearchMatchNextCalls == 1,
            $"Search MatchNext cadence differs ({invocation.Definition.Name}: observed={invocation.SearchMatchNextCalls}).");
        if (definition.FileMode || definition.PatternMode)
            Require(invocation.SearchParsePatternCalls == 1,
                "Search pattern mode did not compile through DOS.");
        if (definition.LocaleCaseMap)
            Require(invocation.SearchConvToUpperCalls > 0,
                "Search no-CASE literal matching did not use locale ConvToUpper.");
        if (definition.LocaleHighByteControl)
            Require(invocation.SearchIsCntrlCalls > 0,
                "Search high-byte delimiters did not use locale IsCntrl.");
        if (definition.LocaleHighBytePrintable)
            Require(invocation.SearchIsPrintCalls > 0,
                "Search high-byte output did not use locale IsPrint.");
        if (definition.CtrlC || definition.ReadFailure &&
            suite == WorkbenchSearchEntrySuite)
            Require(invocation.SearchPrintFaultCalls == 1,
                "Search failure did not publish a DOS fault.");
        _ = layout;
    }

    private static uint LongestLfLineLength(byte[] content)
    {
        var current = 0u;
        var longest = 0u;
        foreach (var value in content)
        {
            if (value == (byte)'\n')
            {
                if (current > longest) longest = current;
                current = 0;
            }
            else if (current < uint.MaxValue)
            {
                current++;
            }
        }
        return current > longest ? current : longest;
    }

    private static string FilePart(string value)
    {
        var slash = value.LastIndexOf('/');
        var colon = value.LastIndexOf(':');
        var index = Math.Max(slash, colon);
        return index < 0 ? value : value[(index + 1)..];
    }

    private static bool Glob(string pattern, string text)
    {
        var p = 0;
        var t = 0;
        var star = -1;
        var retry = 0;
        while (t < text.Length)
        {
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
}
