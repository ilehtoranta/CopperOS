using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record RequestFileEntryCase(
    bool MultiSelect = false,
    string? Drawer = null,
    string? File = null,
    string? Pattern = null,
    string? Title = null,
    string? Positive = null,
    string? Negative = null,
    string? AcceptPattern = null,
    string? RejectPattern = null,
    bool SaveMode = false,
    bool DrawersOnly = false,
    bool NoIcons = false,
    string? PubScreen = null,
    bool InitialVolumes = false,
    bool Cancel = false,
    bool AslOpenFailure = false,
    bool AllocationFailure = false,
    bool Workbench = false,
    bool MissingDos = false,
    int ParserError = 0);

internal sealed partial class ProbeFixture
{
    public const string RequestFileEntrySuite =
        "morphos320-requestfile-native-entry-vector-fixture";
    public const string Workbench31RequestFileEntrySuite =
        "wb31-requestfile-native-entry-vector-fixture";

    private const short AslAllocAslRequest = -48;
    private const short AslFreeAslRequest = -54;
    private const short AslRequest = -60;

    private const uint AslTb = 0x8008_0000u;
    private const uint TagIgnore = 1;
    private const uint AslFrTitleText = AslTb + 1;
    private const uint AslFrInitialFile = AslTb + 8;
    private const uint AslFrInitialDrawer = AslTb + 9;
    private const uint AslFrInitialPattern = AslTb + 10;
    private const uint AslFrPositiveText = AslTb + 18;
    private const uint AslFrNegativeText = AslTb + 19;
    private const uint AslFrDoSaveMode = AslTb + 44;
    private const uint AslFrDoMultiSelect = AslTb + 45;
    private const uint AslFrDoPatterns = AslTb + 46;
    private const uint AslFrDrawersOnly = AslTb + 47;
    private const uint AslFrPubScreenName = AslTb + 41;
    private const uint AslFrRejectIcons = AslTb + 60;
    private const uint AslFrRejectPattern = AslTb + 61;
    private const uint AslFrAcceptPattern = AslTb + 62;
    private const uint AslFrInitialShowVolumes = AslTb + 130;

    private void RegisterRequestFileEntryAsl()
    {
        Register(AslBase, AslAllocAslRequest, "AllocAslRequest",
            (state, invocation) =>
            {
                Require(state.D[0] == 0 && state.A[0] != 0,
                    "RequestFile AllocAslRequest ABI differs.");
                VerifyRequestFileTags(invocation, state.A[0]);
                invocation.RequestFileAllocCalls++;
                if (invocation.Definition.RequestFile!.AllocationFailure)
                    return 0;
                var requester = Bus.Allocate(invocation, 56, "FileRequester", true);
                var drawer = invocation.Arguments + 0x300;
                var file = invocation.Arguments + 0x320;
                WriteRequestCString(drawer, "SYS:");
                WriteRequestCString(file, invocation.Definition.RequestFile.MultiSelect
                    ? "one.txt" : "picked.txt");
                Bus.Long(requester + 4, file);
                Bus.Long(requester + 8, drawer);
                if (invocation.Definition.RequestFile.MultiSelect)
                {
                    var list = Bus.Allocate(invocation, 16, "WBArgList", true);
                    var first = invocation.Arguments + 0x340;
                    var second = invocation.Arguments + 0x350;
                    WriteRequestCString(first, "one.txt");
                    WriteRequestCString(second, "two.txt");
                    Bus.Long(list, 0);
                    Bus.Long(list + 4, first);
                    Bus.Long(list + 8, 0);
                    Bus.Long(list + 12, second);
                    Bus.Long(requester + 28, 2);
                    Bus.Long(requester + 32, list);
                }
                else
                {
                    Bus.Long(requester + 28, 0);
                    Bus.Long(requester + 32, 0);
                }
                invocation.RequestFileLayout = requester;
                return requester;
            });
        Register(AslBase, AslRequest, "AslRequest",
            (state, invocation) =>
            {
                Require(state.A[0] == invocation.RequestFileLayout &&
                    state.A[1] == 0, "RequestFile AslRequest ABI differs.");
                invocation.RequestFileRequests++;
                if (invocation.Definition.RequestFile!.Cancel)
                {
                    invocation.IoError = 0;
                    return 0;
                }
                return 1;
            });
        Register(AslBase, AslFreeAslRequest, "FreeAslRequest",
            (state, invocation) =>
            {
                Require(state.A[0] == invocation.RequestFileLayout,
                    "RequestFile FreeAslRequest ABI differs.");
                var argList = invocation.Definition.RequestFile!.MultiSelect
                    ? Bus.Long(state.A[0] + 32) : 0u;
                Bus.Release(invocation, state.A[0], "FileRequester", 56);
                if (argList != 0)
                    Bus.Release(invocation, argList, "WBArgList", 16);
                invocation.RequestFileFreeCalls++;
                return 0;
            });
    }

    private void RegisterRequestFileEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var workbench = suite == Workbench31RequestFileEntrySuite;
            var expectedTemplate = workbench
                ? NativeWorkbench31RequestFileCommand.Template
                : NativeMorphOSRequestFileCommand.Template;
            var expectedBytes = (workbench
                ? NativeWorkbench31RequestFileCommand.ResultCount
                : NativeMorphOSRequestFileCommand.ResultCount) * 4u;
            Require(Bus.CString(state.D[1]) == expectedTemplate &&
                state.D[3] == 0, "RequestFile ReadArgs template/source ABI differs.");
            Require((state.D[2] & 3) == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == expectedBytes,
                "RequestFile result slots differ.");
            invocation.RequestFileReadArgsCalls++;
            var definition = invocation.Definition.RequestFile!;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            var rdArgs = Bus.Allocate(invocation, 40, "RDArgs", true);
            invocation.RequestFileRdArgs = rdArgs;
            var drawer = invocation.Arguments + 0x100;
            WriteRequestCString(drawer, definition.Drawer ?? "SYS:");
            Bus.Long(state.D[2], drawer);
            var optional = new[]
            {
                definition.File, definition.Pattern, definition.Title,
                definition.Positive, definition.Negative, definition.AcceptPattern,
                definition.RejectPattern
            };
            for (var index = 0; index < optional.Length; index++)
            {
                if (optional[index] is null) continue;
                var address = invocation.Arguments + 0x120u +
                    unchecked((uint)index * 0x20u);
                WriteRequestCString(address, optional[index]!);
                Bus.Long(state.D[2] + unchecked((uint)(index + 1) * 4u), address);
            }
            var resultCount = workbench
                ? NativeWorkbench31RequestFileCommand.ResultCount
                : NativeMorphOSRequestFileCommand.ResultCount;
            for (var index = 1u; index < resultCount; index++)
                Bus.Long(state.D[2] + index * 4, 0);
            // Reapply optional string slots after clearing the absent slots.
            for (var index = 0; index < optional.Length; index++)
            {
                if (optional[index] is null) continue;
                var address = invocation.Arguments + 0x120u +
                    unchecked((uint)index * 0x20u);
                Bus.Long(state.D[2] + unchecked((uint)(index + 1) * 4u), address);
            }
            if (definition.SaveMode) Bus.Long(state.D[2] + 8 * 4, uint.MaxValue);
            if (definition.MultiSelect) Bus.Long(state.D[2] + 9 * 4, uint.MaxValue);
            if (definition.DrawersOnly) Bus.Long(state.D[2] + 10 * 4, uint.MaxValue);
            if (definition.NoIcons) Bus.Long(state.D[2] + 11 * 4, uint.MaxValue);
            if (definition.PubScreen is not null)
            {
                var address = invocation.Arguments + 0x200;
                WriteRequestCString(address, definition.PubScreen);
                Bus.Long(state.D[2] + 12 * 4, address);
            }
            if (!workbench && definition.InitialVolumes)
                Bus.Long(state.D[2] + 13 * 4, uint.MaxValue);
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Require(state.D[1] == invocation.RequestFileRdArgs,
                "RequestFile FreeArgs pointer ABI differs.");
            Bus.Release(invocation, state.D[1], "RDArgs", 40);
            invocation.RequestFileFreeArgsCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.AddPart, "AddPart", (state, invocation) =>
        {
            Require(state.D[3] == 512, "RequestFile AddPart capacity differs.");
            var directory = Bus.CString(state.D[1]);
            var file = Bus.CString(state.D[2]);
            Require(directory == "SYS:" && (file == "picked.txt" ||
                file == "one.txt" || file == "two.txt"),
                "RequestFile AddPart paths differ.");
            Encoding.Latin1.GetBytes(directory + file + "\0")
                .CopyTo(Bus.Memory.AsSpan((int)state.D[1]));
            return 1;
        });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            Require(state.D[2] != 0 &&
                (Bus.CString(state.D[1]) == "\"%s\"\n" ||
                 Bus.CString(state.D[1]) == "\"%s\" "),
                "RequestFile VPrintf format differs.");
            var path = Bus.CString(Bus.Long(state.D[2]));
            invocation.Output.Write(Encoding.Latin1.GetBytes('"' + path + '"' +
                (Bus.CString(state.D[1]).EndsWith(' ') ? " " : "\n")));
            invocation.RequestFileVPrintfCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == "\n",
                "RequestFile PutStr format differs.");
            invocation.Output.WriteByte((byte)'\n');
            invocation.RequestFilePutStrCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault", (_, invocation) =>
        {
            invocation.Output.Write(Encoding.Latin1.GetBytes("RequestFile\n"));
            return 0;
        });
    }

    private List<object> RunRequestFileEntryCases()
    {
        ProbeCase[] cases =
        [
            new("single", "SYS:", DOS.RETURN_OK, 0, "\"SYS:picked.txt\"\n")
            { RequestFile = new() },
            new("tag-options", "SYS: OPTIONS", DOS.RETURN_OK, 0,
                "\"SYS:picked.txt\"\n")
            {
                RequestFile = new(
                    Drawer: "WORK:", File: "initial.txt", Pattern: "#?.txt",
                    Title: "Choose", Positive: "Use", Negative: "Cancel",
                    AcceptPattern: "#?.txt", RejectPattern: "~(#?.bak)",
                    SaveMode: true, DrawersOnly: true, NoIcons: true,
                    PubScreen: "PUB", InitialVolumes: true)
            },
            new("multi", "SYS: MULTISELECT", DOS.RETURN_OK, 0,
                "\"SYS:one.txt\" \"SYS:two.txt\" \n")
            { RequestFile = new(MultiSelect: true) },
            new("cancel", "SYS:", DOS.RETURN_WARN, (int)DOS.Error.Break,
                "") { RequestFile = new(Cancel: true) },
            new("allocation-failure", "", DOS.RETURN_FAIL,
                31337, "RequestFile\n")
            { RequestFile = new(AllocationFailure: true) },
            new("parser-failure", "", DOS.RETURN_ERROR, 103, "RequestFile\n")
            { RequestFile = new(ParserError: 103) },
            new("asl-open-failure", "SYS:", DOS.RETURN_OK, 0, "")
            { RequestFile = new(AslOpenFailure: true) },
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Workbench = true, RequestFile = new(Workbench: true) },
            new("missing-dos", "", DOS.RETURN_FAIL,
                (int)DOS.Error.InvalidResidentLibrary, "")
            { MissingDos = true, WritesOwnProcessError = true,
                RequestFile = new(MissingDos: true) },
        ];
        var reports = new List<object>();
        if (suite == Workbench31RequestFileEntrySuite)
            cases[1] = cases[1] with
            {
                Name = "tag-options-workbench",
                RequestFile = cases[1].RequestFile! with { InitialVolumes = false }
            };
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([cases[0] with { Name = "single-repeat" }], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "single-interleaved-left" },
            cases[1] with { Name = "tag-options-interleaved-right" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyRequestFileEntry(Invocation invocation)
    {
        var definition = invocation.Definition.RequestFile!;
        if (definition.MissingDos)
        {
            Require(invocation.RequestFileReadArgsCalls == 0 &&
                invocation.RequestFileAllocVecCalls == 0 &&
                invocation.RequestFileAllocCalls == 0 &&
                invocation.AslOpens == 0,
                "RequestFile missing-DOS startup reached the command body.");
            return;
        }
        if (definition.Workbench)
        {
            Require(invocation.RequestFileReadArgsCalls == 0,
                "Workbench RequestFile startup reached command body.");
            return;
        }
        if (definition.AslOpenFailure)
        {
            Require(invocation.RequestFileReadArgsCalls == 0 &&
                invocation.AslOpens == 1, "RequestFile ASL failure was not isolated.");
            return;
        }
        if (definition.ParserError != 0)
        {
            Require(invocation.RequestFileAllocVecCalls == 0 &&
                invocation.RequestFileFreeVecCalls == 0 &&
                invocation.RequestFileReadArgsCalls == 1 &&
                invocation.RequestFileExecMemCalls == 1 &&
                invocation.RequestFileFreeMemCalls == 1 &&
                invocation.RequestFileFreeArgsCalls == 0 &&
                invocation.RequestFileAllocCalls == 0 &&
                invocation.RequestFileFreeCalls == 0,
                "RequestFile parser failure ownership differs.");
            return;
        }
        if (definition.AllocationFailure)
        {
            Require(invocation.RequestFileAllocVecCalls == 1 &&
                invocation.RequestFileFreeVecCalls == 0 &&
                invocation.RequestFileReadArgsCalls == 1 &&
                invocation.RequestFileExecMemCalls == 1 &&
                invocation.RequestFileFreeMemCalls == 1 &&
                invocation.RequestFileFreeArgsCalls == 1 &&
                invocation.RequestFileAllocCalls == 0 &&
                invocation.RequestFileFreeCalls == 0 &&
                invocation.RequestFileRequests == 0,
                "RequestFile allocation failure ownership differs.");
            return;
        }
        Require(invocation.RequestFileAllocVecCalls == 1 &&
            invocation.RequestFileFreeVecCalls == 1 &&
            invocation.RequestFileReadArgsCalls == 1 &&
            invocation.RequestFileExecMemCalls == 1 &&
            invocation.RequestFileFreeMemCalls == 1 &&
            invocation.RequestFileFreeArgsCalls == 1,
            "RequestFile parser/buffer ownership differs.");
        if (definition.ParserError == 0)
            Require(invocation.RequestFileAllocCalls == 1 &&
                invocation.RequestFileFreeCalls == 1 &&
                invocation.RequestFileRequests == 1,
                "RequestFile ASL requester lifecycle differs.");
    }

    private void VerifyRequestFileTags(Invocation invocation, uint tags)
    {
        var definition = invocation.Definition.RequestFile!;
        var expected = new List<(uint Tag, uint Data)>
        {
            (AslFrInitialDrawer, StringAddress(invocation,
                definition.Drawer ?? "SYS:", 0x100)),
            (AslFrInitialFile, StringAddress(invocation, definition.File, 0x120)),
            (AslFrInitialPattern, StringAddress(invocation, definition.Pattern, 0x140)),
            (AslFrTitleText, StringAddress(invocation, definition.Title, 0x160)),
            (AslFrPositiveText, StringAddress(invocation, definition.Positive, 0x180)),
            (AslFrNegativeText, StringAddress(invocation, definition.Negative, 0x1a0)),
            (AslFrAcceptPattern, StringAddress(invocation, definition.AcceptPattern, 0x1c0)),
            (AslFrRejectPattern, StringAddress(invocation, definition.RejectPattern, 0x1e0)),
            (AslFrDoSaveMode, definition.SaveMode ? 1u : 0u),
            (AslFrDoMultiSelect, definition.MultiSelect ? 1u : 0u),
            (AslFrDrawersOnly, definition.DrawersOnly ? 1u : 0u),
            (AslFrRejectIcons, definition.NoIcons ? 1u : 0u),
            (AslFrPubScreenName, StringAddress(invocation, definition.PubScreen, 0x200)),
            (AslFrDoPatterns, definition.Pattern is null ? 0u : 1u),
            (0, 0)
        };
        if (suite == RequestFileEntrySuite)
            expected.Insert(expected.Count - 1,
                (definition.InitialVolumes ? AslFrInitialShowVolumes : TagIgnore,
                    definition.InitialVolumes ? 1u : 0u));
        for (var index = 0; index < expected.Count; index++)
        {
            var actualTag = Bus.Long(tags + unchecked((uint)index * 8u));
            var actualData = Bus.Long(tags + unchecked((uint)index * 8u) + 4);
            Require(actualTag == expected[index].Tag && actualData == expected[index].Data,
                $"RequestFile ASL tag {index} differs (actual {actualTag:X8}/{actualData:X8}, expected {expected[index].Tag:X8}/{expected[index].Data:X8}).");
        }
    }

    private uint StringAddress(Invocation invocation, string? value, uint offset)
    {
        if (value is null) return 0;
        var address = invocation.Arguments + offset;
        WriteRequestCString(address, value);
        return address;
    }

}
