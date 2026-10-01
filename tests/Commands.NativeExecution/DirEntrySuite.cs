using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record DirEntryData(string Name, bool Directory,
    DirEntryData[]? Children = null, bool SoftLink = false,
    bool Dangling = false);

internal sealed record DirEntryCase(bool Dirs = false, bool Files = false,
    string? Option = null, bool All = false, bool Inter = false,
    bool Empty = false, bool AllocationFailure = false,
    bool LockFailure = false, bool CtrlC = false, int ParserError = 0,
    DirEntryData[]? Entries = null, string? Path = null,
    int ExAllError = 0, string? ExAllErrorPath = null,
    int CtrlCAtSignal = 1, int MatchNextError = 0,
    uint FailAllocVecBytes = 0, int FailAllocVecOccurrence = 1,
    int PatternSuccessIoError = 0, int MatchEndIoError = 0);

internal sealed class DirNativeLayout(uint control, uint path, uint option,
    uint firstName, uint secondName)
{
    public uint Control { get; } = control;
    public uint Path { get; } = path;
    public uint Option { get; } = option;
    public uint FirstName { get; } = firstName;
    public uint SecondName { get; } = secondName;
    public uint Workspace { get; set; }
    public uint ExAllBuffer { get; set; }
    public uint RdArgs { get; set; }
    public uint ExAllControl { get; set; }
    public uint Locked { get; set; }
    public int ExAllCalls { get; set; }
    public int AllocVecSuccesses { get; set; }
    public bool LockLive { get; set; }
    public uint CurrentDirectory { get; set; }
    public uint RootLock { get; set; }
    public uint NextLock { get; set; }
    public uint PathBuffer { get; set; }
    public uint CurrentChain { get; set; }
    public uint DeviceProc { get; set; }
    public DirEntryData? LastLockedEntry { get; set; }
    public Dictionary<uint, DirEntryData[]> EntriesByLock { get; } = [];
    public Dictionary<uint, DirEntryData?> LockedEntryByLock { get; } = [];
    public Dictionary<uint, string> PathByLock { get; } = [];
    public Dictionary<string, DirEntryData[]> EntriesByPath { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, DirEntryData> EntryByPath { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<uint, (string Kind, uint Size)> Vectors { get; } = [];
    public Dictionary<uint, int> AllocVecSizeOccurrences { get; } = [];
    public HashSet<uint> LiveLocks { get; } = [];
    public HashSet<uint> LiveControls { get; } = [];
    public uint PatternAnchor { get; set; }
    public uint PatternIndex { get; set; }
    public bool PatternLive { get; set; }
    public int DirParsePatternCalls { get; set; }
    public int DirMatchFirstCalls { get; set; }
    public int DirMatchNextCalls { get; set; }
    public int DirSetSignalCalls { get; set; }
    public int DirMatchEndCalls { get; set; }
    public int DirSoftLinkLocks { get; set; }
    public int DirSoftLinkLockFailures { get; set; }
    public int DirSoftLinkExaminations { get; set; }
    public int DirSoftLinkUnlocks { get; set; }
    public int DirDeviceLookups { get; set; }
    public int DirReadLinks { get; set; }
    public int DirDeviceReleases { get; set; }
    public int DirDanglingWarnings { get; set; }
    public int DirLastPrintFaultCode { get; set; }
    public uint DirLastPrintFaultText { get; set; }
    public bool DirMatchErrorDelivered { get; set; }
}

internal sealed partial class ProbeFixture
{
    private const uint DirCtrlCMask = 1u << 12;
    public const string DirEntrySuite =
        "morphos320-dir-native-entry-vector-fixture";
    public const string Workbench31DirEntrySuite =
        "workbench31-dir-native-entry-vector-fixture";

    public static bool IsDirEntrySuite(string value) =>
        value == DirEntrySuite || value == Workbench31DirEntrySuite;

    private void RegisterDirEntryExec()
    {
        Register(ExecBase, ExecLvo.SetSignal, "Dir SetSignal", (state,
            invocation) =>
        {
            Require(state.D[0] == 0 && state.D[1] == 0,
                "Dir SetSignal query differs.");
            var layout = invocation.DirLayout!;
            layout.DirSetSignalCalls++;
            return invocation.Definition.Dir!.CtrlC &&
                layout.DirSetSignalCalls ==
                    invocation.Definition.Dir.CtrlCAtSignal
                ? DirCtrlCMask : 0u;
        });
    }

    private static readonly DirEntryData[] DefaultDirEntries =
    [
        new("one.txt", false),
        new("subdir", true, [new("deep.txt", false)]),
    ];
    private static readonly DirEntryData[] LargeDirEntries =
        Enumerable.Range(0, 300)
            .Select(index => new DirEntryData($"file{index:D3}.txt", false))
            .ToArray();

    private List<object> RunDirEntryCases()
    {
        var largeOutput = DirOutput(new(Entries: LargeDirEntries));
        var largePatternOutput = DirOutput(new(Files: true,
            Path: "SRC:dir/#?", Entries: LargeDirEntries), true);
        ProbeCase[] cases =
        [
            DirCase("default", new(), "", DirOutput(new())),
            DirCase("dirs", new(Dirs: true), "DIRS", DirOutput(new(Dirs: true))),
            DirCase("files", new(Files: true), "FILES", DirOutput(new(Files: true))),
            DirCase("opt-d", new(Option: "D"), "OPT D", DirOutput(new(Dirs: true))),
            DirCase("unknown-option", new(Option: "X"), "OPT X",
                "X option ignored\n" + DirOutput(new())),
            DirCase("empty", new(Empty: true), "", ""),
            DirCase("large-exall-directory", new(Entries: LargeDirEntries),
                "", largeOutput),
            DirCase("large-wildcard-directory", new(Files: true,
                Path: "SRC:dir/#?", Entries: LargeDirEntries),
                "SRC:dir/#? FILES", largePatternOutput),
            DirCase("large-name-copy-allocation-failure", new(
                Entries: LargeDirEntries, FailAllocVecBytes: 12,
                FailAllocVecOccurrence: 11), "",
                "Could not get information for \n", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore),
            DirCase("large-row-growth-allocation-failure", new(
                Entries: LargeDirEntries, FailAllocVecBytes: 2048), "",
                "Could not get information for \n", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore),
            DirCase("ctrl-c-before-output", new(CtrlC: true), "", "",
                DOS.RETURN_WARN, (int)DOS.Error.Break),
            DirCase("ctrl-c-before-file-pair", new(CtrlC: true,
                CtrlCAtSignal: 2), "", "     subdir (dir)\n",
                DOS.RETURN_WARN, (int)DOS.Error.Break),
            DirCase("wildcard-break", new(Path: "SRC:dir/#?",
                Entries: [new("match.txt", false)],
                MatchNextError: (int)DOS.Error.Break),
                "SRC:dir/#?", "", DOS.RETURN_WARN,
                (int)DOS.Error.Break),
            DirCase("wildcard-saved-ioerr", new(Files: true,
                Path: "SRC:dir/#?", Entries: [new("match.txt", false)],
                PatternSuccessIoError: 47, MatchEndIoError: 901),
                "SRC:dir/#? FILES",
                DirOutput(new(Files: true, Path: "SRC:dir/#?",
                    Entries: [new("match.txt", false)]), true),
                error: 47),
            DirCase("wildcard-ctrl-c-during-pairs", new(CtrlC: true,
                CtrlCAtSignal: 2, Files: true, Path: "SRC:dir/#?",
                Entries: LargeDirEntries), "SRC:dir/#? FILES",
                largePatternOutput.Split('\n')[0] +
                    "\nCould not get information for SRC:dir/#?\n",
                DOS.RETURN_WARN),
            DirCase("exall-error", new(Path: "SRC:dir",
                ExAllError: (int)DOS.Error.ObjectNotFound),
                "SRC:dir", "Could not get information for SRC:dir\n",
                DOS.RETURN_ERROR, (int)DOS.Error.ObjectNotFound),
            DirCase("exall-wrong-type", new(Path: "SRC:dir",
                ExAllError: (int)DOS.Error.ObjectWrongType),
                "SRC:dir", "SRC:dir is not a directory\n",
                DOS.RETURN_ERROR, (int)DOS.Error.ObjectWrongType),
            DirCase("all-nested-exall-error", new(All: true,
                Path: "SRC:dir", ExAllError:
                    (int)DOS.Error.ObjectNotFound,
                ExAllErrorPath: "SRC:dir/subdir"), "SRC:dir ALL",
                "     subdir (dir)\nCould not get information for SRC:dir\n",
                DOS.RETURN_ERROR, (int)DOS.Error.ObjectNotFound),
            DirCase("all-recursive", new(All: true), "ALL",
                DirOutput(new(All: true))),
            DirCase("opt-a-recursive", new(Option: "A"), "OPT A",
                DirOutput(new(All: true))),
            DirCase("opt-i-joined-lines", new(Option: "I"), "OPT I",
                DirOutput(new()).Replace("\n", "")),
            DirCase("directory-order-file-pairs", new(Entries:
                [new("later-dir", true), new("first-dir", true),
                 new("zeta.txt", false), new("alpha.txt", false),
                 new("beta.txt", false)]), "", DirOutput(new(Entries:
                [new("later-dir", true), new("first-dir", true),
                 new("zeta.txt", false), new("alpha.txt", false),
                 new("beta.txt", false)]))),
            DirCase("all-multilevel", new(All: true, Entries:
                [new("later-dir", true,
                    [new("inner", true, [new("deep.txt", false)]),
                     new("outer.txt", false)]),
                 new("first-dir", true, [new("first.txt", false)])]),
                "ALL", DirOutput(new(All: true, Entries:
                [new("later-dir", true,
                    [new("inner", true, [new("deep.txt", false)]),
                     new("outer.txt", false)]),
                 new("first-dir", true, [new("first.txt", false)])]))),
            DirCase("wildcard-pattern", new(Path: "SRC:dir/#?"),
                "SRC:dir/#?", DirOutput(new(Path: "SRC:dir/#?"), true)),
            DirCase("wildcard-pattern-all", new(All: true,
                Path: "SRC:dir/#?"), "SRC:dir/#? ALL",
                DirOutput(new(All: true, Path: "SRC:dir/#?"), true),
                error: (int)DOS.Error.NoMoreEntries),
            DirCase("softlink-to-file", new(Entries:
                [new("LinkFile", false, SoftLink: true)]), "",
                DirOutput(new(Entries:
                    [new("LinkFile", false, SoftLink: true)]))),
            DirCase("softlink-to-directory-all", new(All: true, Entries:
                [new("LinkDir", true, [new("inside.txt", false)],
                    SoftLink: true)]), "ALL", DirOutput(new(All: true,
                Entries: [new("LinkDir", true, [new("inside.txt", false)],
                    SoftLink: true)]))),
            DirCase("dangling-softlink-all", new(All: true, Entries:
                [new("Dangling", true, SoftLink: true, Dangling: true)]),
                "ALL", DirOutput(new(All: true, Entries:
                    [new("Dangling", true, SoftLink: true, Dangling: true)])),
                error: (int)DOS.Error.ObjectNotFound),
            DirCase("wildcard-softlinks", new(All: true, Path: "SRC:dir/#?",
                Entries: [new("LinkDir", true, [new("inside.txt", false)],
                    SoftLink: true), new("LinkFile", false, SoftLink: true),
                    new("Dangling", false, SoftLink: true, Dangling: true)]),
                "SRC:dir/#? ALL", DirOutput(new(All: true,
                    Path: "SRC:dir/#?", Entries:
                    [new("LinkDir", true, [new("inside.txt", false)],
                        SoftLink: true),
                     new("LinkFile", false, SoftLink: true),
                     new("Dangling", false, SoftLink: true,
                        Dangling: true)]), true),
                error: (int)DOS.Error.ObjectNotFound),
            DirCase("explicit-dangling-softlink", new(
                Path: "SRC:dir/Dangling", Entries:
                    [new("Dangling", true, SoftLink: true,
                        Dangling: true)]), "SRC:dir/Dangling",
                "Warning: Skipping dangling softlink SRC:dir/Dangling -> TARGET:\n",
                error: (int)DOS.Error.ObjectNotFound),
            new("inter-unsupported", "INTER", DOS.RETURN_ERROR,
                (int)DOS.Error.NotImplemented, "") { Dir = new(Inter: true) },
            new("parser-failure", "", DOS.RETURN_ERROR, 116, "")
            { Dir = new(ParserError: 116) },
            new("allocation-failure", "", DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore, "")
            { Dir = new(AllocationFailure: true) },
            new("lock-failure", "", DOS.RETURN_FAIL,
                (int)DOS.Error.ObjectNotFound,
                "Could not get information for \n")
            { Dir = new(LockFailure: true) },
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Dir = new(), Workbench = true },
            new("negative-entry-length", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Dir = new(), EntryLength = -1 },
            new("null-entry-buffer", "", DOS.RETURN_ERROR,
                (int)DOS.Error.LineTooLong, "")
            { Dir = new(), EntryLength = 4, NullArgumentPointer = true },
        ];

        if (suite == Workbench31DirEntrySuite)
        {
            cases = [
                .. cases.Where(test => test.Name != "all-recursive" &&
                    test.Name != "opt-a-recursive" &&
                    test.Name != "opt-i-joined-lines" &&
                    test.Name != "all-multilevel" &&
                    test.Name != "all-nested-exall-error" &&
                    test.Name != "wildcard-pattern-all" &&
                    test.Name != "softlink-to-file" &&
                    test.Name != "softlink-to-directory-all" &&
                    test.Name != "dangling-softlink-all" &&
                    test.Name != "wildcard-softlinks" &&
                    test.Name != "wildcard-saved-ioerr" &&
                    test.Name != "explicit-dangling-softlink" &&
                    test.Name != "inter-unsupported"),
                new("all-unsupported", "ALL", DOS.RETURN_FAIL,
                    (int)DOS.Error.NotImplemented, "")
                { Dir = new(All: true) },
                new("wildcard-all-unsupported", "SRC:dir/#? ALL",
                    DOS.RETURN_FAIL, (int)DOS.Error.NotImplemented, "")
                { Dir = new(All: true, Path: "SRC:dir/#?") },
                new("inter-unsupported", "INTER", DOS.RETURN_FAIL,
                    (int)DOS.Error.NotImplemented, "")
                { Dir = new(Inter: true) },
                new ProbeCase("missing-dos", "", DOS.RETURN_FAIL,
                    Invocation.InitialIoError, "")
                { Dir = new(), MissingDos = true }
            ];
        }

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            DirCase("interleaved-left", new(Dirs: true), "DIRS",
                DirOutput(new(Dirs: true))),
            DirCase("interleaved-right", new(Files: true), "FILES",
                DirOutput(new(Files: true)))
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase DirCase(string name, DirEntryCase definition,
        string arguments, string output, int result = DOS.RETURN_OK,
        int error = 0) => new(name, arguments, result, error, output)
        { Dir = definition };

    private static string DirOutput(DirEntryCase definition,
        bool patternRoot = false)
    {
        var entries = definition.Entries ?? DefaultDirEntries;
        var dirs = definition.Dirs || !definition.Files && !definition.Dirs;
        var files = definition.Files || !definition.Files && !definition.Dirs;
        var output = new StringBuilder();
        AppendDirOutput(output, entries, dirs, files, definition.All, 0,
            definition.Option, patternRoot,
            definition.Path is null ? "" :
                definition.Path[..definition.Path.LastIndexOf('/')]);
        return output.ToString();
    }

    private static void AppendDirOutput(StringBuilder output,
        DirEntryData[] entries, bool dirs, bool files, bool all, int depth,
        string? option, bool patternRoot, string patternDirectory)
    {
        var directories = entries.Where(entry => entry.Directory).ToArray();
        var sortedFiles = entries.Where(entry => !entry.Directory)
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var joinedLines = string.Equals(option, "I",
            StringComparison.OrdinalIgnoreCase);
        if (dirs)
            foreach (var entry in directories)
            {
                output.Append(' ', (depth + 1) * 5)
                    .Append(patternRoot
                        ? patternDirectory + "/" + entry.Name : entry.Name)
                    .Append(" (dir)");
                if (!joinedLines) output.Append('\n');
                if (all && entry.Dangling)
                    output.Append("Warning: Skipping dangling softlink ")
                        .Append(patternRoot ? entry.Name :
                            patternDirectory.Length == 0
                                ? "SRC:dir/" + entry.Name
                                : patternDirectory + "/" + entry.Name)
                        .Append(" -> TARGET:\n");
                if (all && entry.Children is not null)
                    AppendDirOutput(output, entry.Children, dirs, files,
                        all, depth + 1, option, false, patternDirectory);
            }
        var names = files ? sortedFiles.Select(entry => entry.Name).ToArray() : [];
        if (patternRoot)
            foreach (var entry in entries.Where(entry =>
                         entry.SoftLink && entry.Dangling))
                output.Append("Warning: Skipping dangling softlink ")
                    .Append(entry.Name).Append(" -> TARGET:\n");
        for (var index = 0; index < names.Length; index += 2)
        {
            var first = names[index];
            var second = index + 1 < names.Length ? names[index + 1] : "";
            output.Append(' ', depth * 5).Append("  ")
                .Append(first.PadRight(32)).Append(' ')
                .Append(second);
            if (!joinedLines) output.Append('\n');
        }
    }

    private void PrepareDirEntry(Invocation invocation)
    {
        var control = invocation.Process + 0x200;
        var path = control + 32;
        var option = control + 544;
        var firstName = control + 560;
        var secondName = control + 576;
        invocation.DirLayout = new(control, path, option, firstName,
            secondName);
        invocation.DirLayout.NextLock = 0x300u +
            (uint)invocation.Slot * 0x100u;
        var definition = invocation.Definition.Dir!;
        PutCString(path, definition.Path ?? "SRC:dir");
        var rootPath = definition.Path is null ? "SRC:dir" :
            definition.Path.Contains('#') || definition.Path.Contains('?')
                ? definition.Path[..definition.Path.LastIndexOf('/')]
                : definition.Path.StartsWith("SRC:dir/",
                    StringComparison.OrdinalIgnoreCase)
                    ? "SRC:dir"
                : definition.Path;
        AddDirPath(invocation.DirLayout, rootPath,
            definition.Entries ?? DefaultDirEntries);
        invocation.DirLayout.CurrentChain = control + 640;
        invocation.DirLayout.DeviceProc = control + 704;
        Bus.Long(invocation.DirLayout.DeviceProc +
            (uint)DosLayout.DevProc.Port, 0x6780u);
        Bus.Long(invocation.DirLayout.DeviceProc +
            (uint)DosLayout.DevProc.Lock, 0x234u);
        Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Version, 51);
        Bus.Word(invocation.DosBase + (uint)ExecLayout.Library.Revision, 28);
        PutCString(option, invocation.Definition.Dir?.Option ?? "");
        Bus.Long(control, 0);
    }

    private static void AddDirPath(DirNativeLayout layout, string path,
        DirEntryData[] entries)
    {
        layout.EntriesByPath[path] = entries;
        foreach (var entry in entries)
        {
            var entryPath = path + "/" + entry.Name;
            layout.EntryByPath[entryPath] = entry;
            layout.EntriesByPath[entryPath] = entry.Children ?? [];
            if (entry.Directory)
                AddDirPath(layout, entryPath, entry.Children ?? []);
        }
    }

    private void RegisterDirEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state,
            invocation) =>
        {
            var definition = invocation.Definition.Dir!;
            var template = suite == Workbench31DirEntrySuite
                ? NativeWorkbench31DirCommand.Template
                : NativeMorphOSDirCommand.Template;
            var resultCount = suite == Workbench31DirEntrySuite
                ? NativeWorkbench31DirCommand.ResultCount
                : NativeMorphOSDirCommand.ResultCount;
            Require(Bus.CString(state.D[1]) == template &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size ==
                    resultCount * 4u,
                "Dir template or result storage differs.");
            invocation.DirReadArgsCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var result = state.D[2];
            var layout = invocation.DirLayout!;
            if (invocation.Definition.Arguments.Length != 0)
                Bus.Long(result, layout.Path);
            if (definition.Option is not null)
                Bus.Long(result + 4, layout.Option);
            Bus.Long(result + 8, definition.All ? uint.MaxValue : 0);
            Bus.Long(result + 12, definition.Dirs ? uint.MaxValue : 0);
            Bus.Long(result + 16, definition.Files ? uint.MaxValue : 0);
            Bus.Long(result + 20, definition.Inter ? uint.MaxValue : 0);
            layout.RdArgs = Bus.Allocate(invocation, 32, "DirRdArgs", true);
            return layout.RdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state,
            invocation) =>
        {
            Bus.Release(invocation, state.D[1], "DirRdArgs", 32);
            invocation.DirFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.ParsePattern, "ParsePattern", (state,
            invocation) =>
        {
            var source = Bus.CString(state.D[1]);
            Require(state.D[2] != 0 && state.D[3] == source.Length * 2u,
                "Dir ParsePattern buffer differs.");
            invocation.DirLayout!.DirParsePatternCalls++;
            return source.Contains('#') || source.Contains('?') ? 1u : 0u;
        });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state,
            invocation) =>
        {
            var layout = invocation.DirLayout!;
            var definition = invocation.Definition.Dir!;
            Require(!layout.PatternLive && state.D[1] == layout.Path,
                "Dir MatchFirst state differs.");
            invocation.DirLayout!.DirMatchFirstCalls++;
            var entries = definition.Entries ?? DefaultDirEntries;
            if (entries.Length == 0)
            {
                invocation.IoError = (int)DOS.Error.ObjectNotFound;
                return unchecked((uint)DOS.Error.ObjectNotFound);
            }
            layout.PatternAnchor = state.D[2];
            layout.PatternIndex = 0;
            layout.PatternLive = true;
            layout.RootLock = 0x131u + (uint)invocation.Slot;
            var patternPath = definition.Path ?? "SRC:dir/#?";
            var patternSeparator = patternPath.LastIndexOf('/');
            Require(patternSeparator >= 0,
                "Dir wildcard fixture path has no parent directory.");
            layout.PathByLock[layout.RootLock] =
                patternPath[..patternSeparator];
            Bus.Long(state.D[2] + (uint)DosLayout.AnchorPath.Current,
                layout.CurrentChain);
            Bus.Long(layout.CurrentChain + (uint)DosLayout.AChain.Lock,
                layout.RootLock);
            WriteDirPatternMatch(invocation, state.D[2], entries[0]);
            invocation.IoError = definition.PatternSuccessIoError;
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state,
            invocation) =>
        {
            var layout = invocation.DirLayout!;
            Require(layout.PatternLive &&
                state.D[1] == layout.PatternAnchor,
                "Dir MatchNext state differs.");
            layout.DirMatchNextCalls++;
            var definition = invocation.Definition.Dir!;
            if (definition.MatchNextError != 0 &&
                !layout.DirMatchErrorDelivered)
            {
                layout.DirMatchErrorDelivered = true;
                invocation.IoError = definition.MatchNextError;
                return unchecked((uint)definition.MatchNextError);
            }
            var entries = definition.Entries ?? DefaultDirEntries;
            layout.PatternIndex++;
            if (layout.PatternIndex >= entries.Length)
            {
                invocation.IoError = (int)DOS.Error.NoMoreEntries;
                return unchecked((uint)DOS.Error.NoMoreEntries);
            }
            WriteDirPatternMatch(invocation, state.D[1],
                entries[layout.PatternIndex]);
            return 0;
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state,
            invocation) =>
        {
            var layout = invocation.DirLayout!;
            Require(layout.PatternLive &&
                state.D[1] == layout.PatternAnchor,
                "Dir MatchEnd state differs.");
            layout.PatternLive = false;
            layout.DirMatchEndCalls++;
            if (invocation.Definition.Dir!.MatchEndIoError != 0)
                invocation.IoError =
                    invocation.Definition.Dir.MatchEndIoError;
            return 0;
        });
        Register(baseAddress, DosLvo.Lock, "LockRaw", (state, invocation) =>
        {
            Require(state.D[2] == unchecked((uint)DOS.LockMode.Shared),
                "Dir Lock mode differs.");
            invocation.DirLockCalls++;
            var definition = invocation.Definition.Dir!;
            if (definition.LockFailure)
            {
                invocation.IoError = (int)DOS.Error.ObjectNotFound;
                return 0;
            }
            var layout = invocation.DirLayout!;
            var name = Bus.CString(state.D[1]);
            var path = name;
            if (layout.CurrentDirectory != 0)
            {
                Require(layout.PathByLock.TryGetValue(layout.CurrentDirectory,
                        out var parentPath),
                    "Dir relative lock has no current directory.");
                path = parentPath + "/" + name;
            }
            else if (name.Length == 0) path = "SRC:dir";
            if (layout.EntryByPath.TryGetValue(path, out var entry) &&
                entry.SoftLink)
            {
                layout.DirSoftLinkLocks++;
                if (entry.Dangling)
                {
                    layout.DirSoftLinkLockFailures++;
                    invocation.IoError = (int)DOS.Error.ObjectNotFound;
                    return 0;
                }
            }
            Require(layout.EntriesByPath.TryGetValue(path, out var entries),
                "Dir lock path is not in the fixture tree: " + path);
            uint locked;
            if ((name == "SRC:dir" || name.Length == 0) &&
                layout.RootLock == 0)
            {
                locked = 0x131u + (uint)invocation.Slot;
                layout.RootLock = locked;
            }
            else locked = layout.NextLock++;
            layout.EntriesByLock[locked] = entries!;
            layout.LockedEntryByLock[locked] =
                layout.EntryByPath.TryGetValue(path, out var lockedEntry)
                    ? lockedEntry : null;
            layout.LastLockedEntry = layout.LockedEntryByLock[locked];
            layout.PathByLock[locked] = path;
            layout.Locked = locked;
            layout.LiveLocks.Add(locked);
            layout.LockLive = true;
            return locked;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            var layout = invocation.DirLayout!;
            Require(layout.LiveLocks.Remove(state.D[1]),
                "Dir unlock lifetime differs.");
            if (layout.LockedEntryByLock.Remove(state.D[1],
                    out var unlockedEntry) && unlockedEntry?.SoftLink == true)
                layout.DirSoftLinkUnlocks++;
            layout.LastLockedEntry = null;
            layout.LockLive = layout.LiveLocks.Count != 0;
            invocation.DirUnlockCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.CurrentDir, "CurrentDirRaw", (state,
            invocation) =>
        {
            var layout = invocation.DirLayout!;
            var previous = layout.CurrentDirectory;
            layout.CurrentDirectory = state.D[1];
            return previous;
        });
        Register(baseAddress, DosLvo.NameFromLock, "NameFromLock",
            (state, invocation) =>
        {
            var layout = invocation.DirLayout!;
            var foundPath = layout.PathByLock.TryGetValue(state.D[1],
                out var path);
            Require(layout.LiveLocks.Contains(state.D[1]) &&
                state.D[3] is 512 or 4096 && foundPath,
                "Dir NameFromLock arguments differ.");
            PutCString(state.D[2], path!);
            return 1;
        });
        Register(baseAddress, DosLvo.AddPart, "AddPart", (state,
            invocation) =>
        {
            Require(state.D[3] == 4096,
                "Dir recursive AddPart buffer size differs.");
            var directory = Bus.CString(state.D[1]);
            var name = Bus.CString(state.D[2]);
            var joined = directory.TrimEnd(':', '/') + "/" + name;
            if (directory.EndsWith(':')) joined = directory + name;
            if (joined.Length + 1 > state.D[3])
            {
                invocation.IoError = (int)DOS.Error.LineTooLong;
                return 0;
            }
            PutCString(state.D[1], joined);
            return 1;
        });
        Register(baseAddress, DosLvo.AllocDosObject, "AllocDosObject",
            (state, invocation) =>
        {
            Require(state.D[1] == (uint)DosObjectType.ExAllControl &&
                state.D[2] == 0, "Dir ExAllControl allocation differs.");
            invocation.DirAllocDosObjectCalls++;
            var control = Bus.Allocate(invocation, ExAllControl.Size,
                "DirControl", true);
            invocation.DirLayout!.ExAllControl = control;
            invocation.DirLayout.LiveControls.Add(control);
            return control;
        });
        Register(baseAddress, DosLvo.FreeDosObject, "FreeDosObject",
            (state, invocation) =>
        {
            Require(state.D[1] == (uint)DosObjectType.ExAllControl &&
                invocation.DirLayout!.LiveControls.Remove(state.D[2]),
                "Dir ExAllControl release differs.");
            Bus.Release(invocation, state.D[2], "DirControl", ExAllControl.Size);
            invocation.DirFreeDosObjectCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.ExAll, "ExAll", (state, invocation) =>
        {
            var layout = invocation.DirLayout!;
            Require(layout.LiveLocks.Contains(state.D[1]) &&
                state.D[3] == 8192 &&
                state.D[4] == (uint)DosExAllDataLevel.Comment &&
                layout.LiveControls.Contains(state.D[5]),
                $"Dir ExAll ABI differs ({state.D[1]:X8},{state.D[2]:X8},{state.D[3]},{state.D[4]},{state.D[5]:X8}; expected lock={layout.Locked:X8}, buffer={layout.ExAllBuffer:X8}, control={layout.ExAllControl:X8}).");
            invocation.DirExAllCalls++;
            var definition = invocation.Definition.Dir!;
            if (definition.LockFailure)
            {
                invocation.IoError = (int)DOS.Error.ObjectNotFound;
                Bus.Long(state.D[5] + DosLayout.ExAllControl.Entries, 0);
                return 0;
            }
            var lockedPath = layout.PathByLock[state.D[1]];
            if (definition.ExAllError != 0 &&
                (definition.ExAllErrorPath is null ||
                 string.Equals(lockedPath, definition.ExAllErrorPath,
                     StringComparison.OrdinalIgnoreCase)))
            {
                invocation.IoError = definition.ExAllError;
                Bus.Long(state.D[5] + DosLayout.ExAllControl.Entries, 0);
                return 0;
            }
            var entries = definition.Empty ? [] :
                layout.EntriesByLock[state.D[1]];
            var start = Bus.Long(state.D[5] +
                DosLayout.ExAllControl.LastKey);
            if (entries.Length == 0 || start >= entries.Length)
            {
                Bus.Long(state.D[5] + DosLayout.ExAllControl.Entries, 0);
                invocation.IoError = (int)DOS.Error.NoMoreEntries;
                return 0;
            }
            var record = state.D[2];
            const uint pageSize = 64;
            var count = Math.Min(pageSize, (uint)entries.Length - start);
            for (var index = 0u; index < count; index++)
            {
                var current = record + index * 40;
                var name = state.D[2] + 4096 + index * 64;
                var entryIndex = unchecked((int)(start + index));
                PutCString(name, entries[entryIndex].Name);
                Bus.Long(current + DosLayout.ExAllData.Next,
                    index + 1 < count ? current + 40 : 0);
                Bus.Long(current + DosLayout.ExAllData.Name, name);
                Bus.Long(current + DosLayout.ExAllData.Type,
                    entries[entryIndex].SoftLink ?
                        (uint)DosConstants.SoftLink :
                        entries[entryIndex].Directory ? 2u : unchecked((uint)-3));
            }
            // Real dos.library ExAll: eac_Entries is a record count and the
            // ExAllData chain starts at the caller's buffer (D2).
            Bus.Long(state.D[5] + DosLayout.ExAllControl.Entries,
                count);
            var end = start + count;
            Bus.Long(state.D[5] + DosLayout.ExAllControl.LastKey, end);
            var more = end < entries.Length;
            invocation.IoError = more ? 0 :
                (int)DOS.Error.NoMoreEntries;
            return more ? 1u : 0u;
        });
        Register(baseAddress, DosLvo.Examine, "Examine", (state,
            invocation) =>
        {
            var layout = invocation.DirLayout!;
            var target = layout.LastLockedEntry;
            Require(target?.SoftLink == true && !target.Dangling &&
                layout.LiveLocks.Contains(state.D[1]) && state.D[2] != 0,
                "Dir soft-link Examine target or FIB differs.");
            layout.DirSoftLinkExaminations++;
            Bus.Long(state.D[2] + (uint)FileInfoBlock.DirEntryTypeOffset,
                target?.Directory == true ? 2u : unchecked((uint)-3));
            return 1;
        });
        Register(baseAddress, DosLvo.Examine64, "Examine64", (state,
            invocation) =>
        {
            var layout = invocation.DirLayout!;
            var target = layout.LastLockedEntry;
            Require(target?.SoftLink == true && !target.Dangling &&
                layout.LiveLocks.Contains(state.D[1]) && state.D[2] != 0 &&
                state.D[3] == 0,
                "Dir wildcard soft-link Examine64 arguments differ.");
            layout.DirSoftLinkExaminations++;
            Bus.Long(state.D[2] + (uint)FileInfoBlock.DirEntryTypeOffset,
                target?.Directory == true ? 2u : unchecked((uint)-3));
            return 1;
        });
        Register(baseAddress, DosLvo.GetDeviceProc, "GetDeviceProc",
            (state, invocation) =>
        {
            var layout = invocation.DirLayout!;
            var name = Bus.CString(state.D[1]);
            var hasDanglingLink = layout.EntryByPath.Values.Any(entry =>
                entry.SoftLink && entry.Dangling);
            Require(state.D[2] == 0 &&
                (name.Length == 0 || name == "Dangling" ||
                    name == "SRC:dir/Dangling"),
                "Dir dangling-link device lookup differs.");
            layout.DirDeviceLookups++;
            invocation.IoError = 901;
            return hasDanglingLink ? layout.DeviceProc : 0;
        });
        Register(baseAddress, DosLvo.ReadLink, "ReadLink", (state,
            invocation) =>
        {
            var layout = invocation.DirLayout!;
            Require(state.D[1] == 0x6780u && state.D[2] == 0x234u &&
                (state.D[5] == 511 || state.D[5] == 4095),
                "Dir ReadLink device or buffer ABI differs.");
            layout.DirReadLinks++;
            PutCString(state.D[4], "TARGET:");
            invocation.IoError = 903;
            return 7;
        });
        Register(baseAddress, DosLvo.FreeDeviceProc, "FreeDeviceProc",
            (state, invocation) =>
            {
                Require(state.D[1] == invocation.DirLayout!.DeviceProc,
                    "Dir freed a different device process.");
                invocation.DirLayout.DirDeviceReleases++;
                invocation.IoError = 904;
                return 0;
            });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                Bus.CString(state.D[1])));
            return 0;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) =>
            invocation.OutputBptr);
        Register(baseAddress, DosLvo.FPuts, "FPuts", (state, invocation) =>
        {
            Require(Bus.CString(state.D[2]) == "\n",
                "Dir newline output differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes("\n"));
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            var format = Bus.CString(state.D[1]);
            var args = state.D[2];
            if (format == "%c option ignored\n")
            {
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    ((char)Bus.Long(args)).ToString() + " option ignored\n"));
            }
            else if (format == "%s (dir)")
            {
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    Bus.CString(Bus.Long(args)) + " (dir)"));
            }
            else if (format == "Could not get information for %s\n")
            {
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    "Could not get information for " +
                    Bus.CString(Bus.Long(args)) + "\n"));
            }
            else if (format == "%s is not a directory\n")
            {
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    Bus.CString(Bus.Long(args)) + " is not a directory\n"));
            }
            else if (format ==
                "Warning: Skipping dangling softlink %s -> %s\n")
            {
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    "Warning: Skipping dangling softlink " +
                    Bus.CString(Bus.Long(args)) + " -> " +
                    Bus.CString(Bus.Long(args + 4)) + "\n"));
                invocation.DirLayout!.DirDanglingWarnings++;
            }
            else
            {
                Require(format == "  %-32.s %s",
                    "Dir file format differs.");
                invocation.Output.Write(Encoding.Latin1.GetBytes(
                    "  " + Bus.CString(Bus.Long(args)).PadRight(32) + " " +
                    Bus.CString(Bus.Long(args + 4))));
            }
            invocation.DirVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state, invocation) =>
        {
            invocation.DirPrintFaultCalls++;
            invocation.DirLayout!.DirLastPrintFaultCode =
                unchecked((int)state.D[1]);
            invocation.DirLayout.DirLastPrintFaultText = state.D[2];
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

    private void VerifyDirEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Dir!;
        if (definition.CtrlC || definition.MatchNextError ==
                (int)DOS.Error.Break)
        {
            var outputBreakDuringPattern = definition.CtrlC &&
                definition.Path is not null && definition.CtrlCAtSignal == 2;
            var expectedFault = outputBreakDuringPattern
                ? 0 : (int)DOS.Error.Break;
            Require(invocation.DirPrintFaultCalls == 1 &&
                invocation.DirLayout!.DirLastPrintFaultCode == expectedFault &&
                invocation.DirLayout.DirLastPrintFaultText == 0,
                "Dir break path did not preserve its source IoErr value.");
        }
        if (definition.FailAllocVecBytes != 0)
        {
            Require(invocation.DirPrintFaultCalls == 1 &&
                invocation.DirLayout!.DirLastPrintFaultCode ==
                    (int)DOS.Error.NoFreeStore &&
                invocation.DirLayout.DirLastPrintFaultText == 0,
                "Dir allocation failure did not preserve ERROR_NO_FREE_STORE.");
        }
        if (definition.ExAllError != 0)
        {
            Require(invocation.DirPrintFaultCalls == 1 &&
                invocation.DirLayout!.DirLastPrintFaultCode ==
                    (definition.ExAllError ==
                        (int)DOS.Error.ObjectWrongType ? 204 :
                        definition.ExAllError) &&
                invocation.DirLayout.DirLastPrintFaultText == 0,
                "Dir ExAll failure diagnostic or PrintFault error differs.");
        }
        if (invocation.Definition.Workbench ||
            invocation.Definition.EntryLength is < 0 ||
            invocation.Definition.NullArgumentPointer ||
            invocation.Definition.MissingDos)
        {
            Require(invocation.DirLockCalls == 0,
                "Dir crossed an invalid startup boundary.");
            return;
        }
        if (definition.ParserError != 0 || definition.Inter ||
            definition.AllocationFailure ||
            definition.All && suite == Workbench31DirEntrySuite)
        {
            Require(invocation.DirLockCalls == 0,
                "Dir traversed after an early failure.");
            return;
        }
        if (definition.LockFailure)
        {
            var failedLayout = invocation.DirLayout!;
            Require(invocation.DirReadArgsCalls == 1 &&
                invocation.DirFreeArgsCalls == 1 &&
                invocation.DirLockCalls == 1 &&
                invocation.DirUnlockCalls == 0 &&
                invocation.DirExAllCalls == 0 &&
                invocation.DirAllocVecCalls == invocation.DirFreeVecCalls &&
                failedLayout.Vectors.Count == 0 &&
                failedLayout.LiveLocks.Count == 0 &&
                failedLayout.CurrentDirectory == 0 &&
                (suite == Workbench31DirEntrySuite ||
                    failedLayout.DirDeviceLookups == 1 &&
                    failedLayout.DirDeviceReleases == 0 &&
                    failedLayout.DirReadLinks == 0),
                "Dir failed Lock/dangling-link probe ownership differs.");
            return;
        }
        var layout = invocation.DirLayout!;
        Require(invocation.DirReadArgsCalls == 1 &&
            invocation.DirFreeArgsCalls == 1 &&
            invocation.DirAllocDosObjectCalls == invocation.DirFreeDosObjectCalls &&
            invocation.DirLockCalls == invocation.DirUnlockCalls +
                layout.DirSoftLinkLockFailures &&
            invocation.DirUnlockCalls == layout.EntriesByLock.Count &&
            invocation.DirExAllCalls >= invocation.DirAllocDosObjectCalls &&
            invocation.DirFreeVecCalls == layout.AllocVecSuccesses &&
            invocation.DirAllocVecCalls == layout.AllocVecSuccesses +
                (definition.FailAllocVecBytes != 0 ? 1 : 0) &&
            layout.Vectors.Count == 0 &&
            layout.LiveLocks.Count == 0 &&
            layout.LiveControls.Count == 0 &&
            !layout.PatternLive &&
            layout.DirMatchFirstCalls == layout.DirMatchEndCalls &&
            layout.CurrentDirectory == 0 &&
            layout.DirSoftLinkLocks == layout.DirSoftLinkUnlocks +
                layout.DirSoftLinkLockFailures &&
            layout.DirDeviceLookups == layout.DirDeviceReleases &&
            layout.DirReadLinks == layout.DirDanglingWarnings &&
            !layout.LockLive,
            $"Dir parser/resource lifetime differs: ReadArgs={invocation.DirReadArgsCalls}/{invocation.DirFreeArgsCalls}, DOSObjects={invocation.DirAllocDosObjectCalls}/{invocation.DirFreeDosObjectCalls}, locks={invocation.DirLockCalls}/{invocation.DirUnlockCalls}/{layout.EntriesByLock.Count}, ExAll={invocation.DirExAllCalls}, vectors={invocation.DirAllocVecCalls}/{invocation.DirFreeVecCalls}, live={layout.LiveLocks.Count}/{layout.LiveControls.Count}, pattern={layout.DirMatchFirstCalls}/{layout.DirMatchEndCalls}/{layout.PatternLive}, cwd={layout.CurrentDirectory}, links={layout.DirSoftLinkLocks}/{layout.DirSoftLinkUnlocks}/{layout.DirSoftLinkLockFailures}, device={layout.DirDeviceLookups}/{layout.DirDeviceReleases}, readlink={layout.DirReadLinks}/{layout.DirDanglingWarnings}.");
    }

    private void WriteDirPatternMatch(Invocation invocation, uint anchor,
        DirEntryData entry)
    {
        var definition = invocation.Definition.Dir!;
        var info = anchor + (uint)DosLayout.AnchorPath.Info;
        PutCString(info + FileInfoBlock.FileNameOffset, entry.Name);
        Bus.Long(info + FileInfoBlock.DirEntryTypeOffset,
            entry.SoftLink ? (uint)DosConstants.SoftLink :
                entry.Directory ? 2u : unchecked((uint)-3));
        var sourcePath = definition.Path ?? "SRC:dir/#?";
        var parent = sourcePath[..sourcePath.LastIndexOf('/')];
        PutCString(anchor + (uint)DosLayout.AnchorPath.PathBuffer,
            parent + "/" + entry.Name);
    }
}
