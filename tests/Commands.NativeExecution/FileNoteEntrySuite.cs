using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record FileNoteEntryCase(string File, string Comment,
    bool All = false, bool Quiet = false, bool Match = true,
    bool Directory = false, bool SetCommentSucceeds = true,
    int Error = 0, int ParserError = 0);

internal sealed class FileNoteNativeLayout(uint control, uint file,
    uint comment)
{
    public uint Control { get; set; } = control;
    public uint File { get; } = file;
    public uint Comment { get; } = comment;
}

internal sealed partial class ProbeFixture
{
    public const string FileNoteEntrySuite =
        "filenote-native-entry-vector-fixture";
    public const string WorkbenchFileNoteEntrySuite =
        "wb31-filenote-native-entry-vector-fixture";

    public static bool IsFileNoteEntrySuite(string value) =>
        value == FileNoteEntrySuite || value == WorkbenchFileNoteEntrySuite;

    public static bool IsWorkbenchFileNoteEntrySuite(string value) =>
        value == WorkbenchFileNoteEntrySuite;

    private List<object> RunFileNoteEntryCases()
    {
        ProbeCase[] cases =
        [
            FileNoteCase("success", "RAM:test", "hello", DOS.RETURN_OK,
                "   test..done\n"),
            FileNoteCase("directory-all", "RAM:dir", "hello", DOS.RETURN_OK,
                "     dir (dir)..done\n", all: true, directory: true),
            FileNoteCase("quiet", "RAM:quiet", "hello", DOS.RETURN_OK, "",
                quiet: true),
            FileNoteCase("null-comment", "RAM:empty", "", DOS.RETURN_OK,
                "   empty..done\n"),
            FileNoteCase("setcomment-failure", "RAM:bad", "hello", DOS.RETURN_WARN,
                "   bad", setCommentSucceeds: false, error: 205),
            FileNoteCase("truncate", "RAM:long", new string('x', 80),
                DOS.RETURN_OK,
                "Note truncated to 79 characters\n   long..done\n"),
            FileNoteCase("no-match", "RAM:missing", "hello", DOS.RETURN_FAIL, "",
                match: false, error: 205),
            FileNoteCase("parser-failure", "RAM:test", "hello", DOS.RETURN_ERROR, "",
                parserError: 116),
        ];
        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "interleaved-left" },
            cases[2] with { Name = "interleaved-right" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase FileNoteCase(string name, string file, string comment,
        int result, string output, bool all = false, bool quiet = false,
        bool match = true, bool directory = false,
        bool setCommentSucceeds = true, int error = 0,
        int parserError = 0) =>
        new(name, "", result,
            parserError != 0 ? parserError : error, output)
        {
            FileNote = new(file, comment, all, quiet, match, directory,
                setCommentSucceeds, error, parserError)
        };

    private void RegisterFileNoteEntryExec()
    {
        // FileNote uses only the common Exec vectors. AllocMem is qualified by
        // the FileNote-specific branch in ProbeFixture.RegisterExec.
    }

    private void PrepareFileNoteEntry(Invocation invocation)
    {
        var definition = invocation.Definition.FileNote ??
            throw new InvalidOperationException("Missing FileNote definition.");
        var file = invocation.Arguments + 0x400;
        var comment = invocation.Arguments + 0x800;
        invocation.FileNoteLayout = new FileNoteNativeLayout(
            invocation.Arguments, file, comment);
        PutFileNote(file, definition.File);
        if (definition.Comment.Length != 0)
            PutFileNote(comment, definition.Comment);
    }

    private void VerifyFileNoteEntry(Invocation invocation)
    {
        var definition = invocation.Definition.FileNote ??
            throw new InvalidOperationException("Missing FileNote definition.");
        var parserFailure = definition.ParserError != 0;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "FileNote parser lifetime differs.");
        Require(invocation.Allocations == 2 && invocation.FreeMem == 2,
            "FileNote workspace/result allocation cleanup differs.");
        Require(invocation.AllocationRequests.SequenceEqual(new uint[] { 882, 16 }),
            "FileNote allocation order or sizes differ.");
        Require(invocation.Events.Count(x => x == "MatchFirst") ==
                (!parserFailure && definition.Match ? 1 : !parserFailure ? 1 : 0) &&
            invocation.Events.Count(x => x == "MatchEnd") ==
                (parserFailure ? 0 : 1),
            "FileNote matcher start/end count differs.");
        var expectedNext = !parserFailure && definition.Match
            ? definition.All && definition.Directory ? 2 : 1
            : 0;
        Require(invocation.Events.Count(x => x == "MatchNext") == expectedNext,
            "FileNote matcher advance count differs.");
        var expectedSetComment = !parserFailure && definition.Match ?
            (definition.All && definition.Directory ? 1 : 1) : 0;
        Require(invocation.Events.Count(x => x == "SetComment") ==
            expectedSetComment, "FileNote SetComment count differs.");
        Require(parserFailure || invocation.Events.IndexOf("ReadArgs") <
            invocation.Events.IndexOf("FreeArgs"),
            "FileNote released ReadArgs before its body completed.");
        invocation.FileNoteLayout = null;
    }

    private void RegisterFileNoteEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.FileNote!;
            Require(Bus.CString(state.D[1]) ==
                "FILE/A,COMMENT,ALL/S,QUIET/S" && state.D[3] == 0,
                "FileNote ReadArgs template ABI differs.");
            Require(Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 16,
                "FileNote result storage differs.");
            for (var offset = 0u; offset < 16; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "FileNote result slots were not cleared.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var layout = invocation.FileNoteLayout!;
            Bus.Long(state.D[2], layout.File);
            Bus.Long(state.D[2] + 4,
                definition.Comment.Length == 0 ? 0u : layout.Comment);
            Bus.Long(state.D[2] + 8, definition.All ? uint.MaxValue : 0);
            Bus.Long(state.D[2] + 12, definition.Quiet ? uint.MaxValue : 0);
            var rdArgs = Bus.Allocate(invocation, 40, "RDArgs", true);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state, invocation) =>
        {
            var definition = invocation.Definition.FileNote!;
            var anchor = state.D[2];
            Require(Bus.CString(state.D[1]) == definition.File &&
                Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] ==
                    (byte)(AnchorPathFlags.DoWild | AnchorPathFlags.FollowHardLinks) &&
                Bus.Word(anchor + (uint)DosLayout.AnchorPath.StringLength) == 512,
                "FileNote MatchFirst ABI or AnchorPath policy differs.");
            if (!definition.Match)
            {
                invocation.IoError = definition.Error;
                return unchecked((uint)definition.Error);
            }
            PutFileNote(anchor + (uint)DosLayout.AnchorPath.PathBuffer,
                definition.File);
            var fib = anchor + (uint)DosLayout.AnchorPath.Info;
            Bus.Long(fib + (uint)FileInfoBlock.DirEntryTypeOffset,
                definition.Directory ? 2u : unchecked((uint)-3));
            PutFileNote(fib + (uint)FileInfoBlock.FileNameOffset,
                Leaf(definition.File));
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state, invocation) =>
        {
            var definition = invocation.Definition.FileNote!;
            var anchor = state.D[1];
            Require(definition.Match && invocation.Events.Count(x => x == "MatchFirst") == 1,
                "FileNote MatchNext entered without a match.");
            var flags = Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags];
            if (definition.All && definition.Directory &&
                invocation.Events.Count(x => x == "MatchNext") == 1)
            {
                Require((flags & (byte)AnchorPathFlags.DoDirectory) != 0,
                    "FileNote ALL did not request directory descent.");
                Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] =
                    (byte)AnchorPathFlags.DidDirectory;
                return 0;
            }
            if (definition.All && definition.Directory)
                Require((flags & (byte)AnchorPathFlags.DoDirectory) == 0,
                    "FileNote directory exit flag was not consumed.");
            invocation.IoError = (int)DOS.Error.NoMoreEntries;
            return (uint)DOS.Error.NoMoreEntries;
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state, invocation) =>
        {
            Require(state.D[1] == invocation.FileNoteLayout!.Control,
                "FileNote MatchEnd anchor differs from workspace.");
            return 0;
        });
        Register(baseAddress, DosLvo.SetComment, "SetComment", (state, invocation) =>
        {
            var definition = invocation.Definition.FileNote!;
            var expectedComment = definition.Comment.Length > 79
                ? definition.Comment[..79] : definition.Comment;
            Require(Bus.CString(state.D[1]) == definition.File &&
                Bus.CString(state.D[2]) == expectedComment,
                "FileNote SetComment ABI or truncation differs.");
            invocation.IoError = definition.Error;
            return definition.SetCommentSucceeds ? 1u : 0u;
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            invocation.Output.Write(Encoding.Latin1.GetBytes(Bus.CString(state.D[1])));
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == "%s",
                "FileNote VPrintf format differs.");
            invocation.Output.Write(Encoding.Latin1.GetBytes(
                Bus.CString(Bus.Long(state.D[2]))));
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (state, invocation) =>
        {
            var definition = invocation.Definition.FileNote!;
            var expected = definition.ParserError != 0 ? definition.ParserError :
                definition.Match ? definition.Error : definition.Error;
            Require(unchecked((int)state.D[1]) == expected,
                "FileNote PrintFault error differs.");
            return 0;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private static string Leaf(string path)
    {
        var separator = Math.Max(path.LastIndexOf(':'), path.LastIndexOf('/'));
        return path[(separator + 1)..];
    }

    private void PutFileNote(uint address, string value)
    {
        Encoding.Latin1.GetBytes(value).CopyTo(
            Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)value.Length] = 0;
    }
}
