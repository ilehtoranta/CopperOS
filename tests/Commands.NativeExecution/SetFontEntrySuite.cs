using System.Text;
using Amiga;
using CopperOS.Commands.Native;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record SetFontEntryCase(
    bool ParserFailure = false,
    bool GraphicsAvailable = true,
    bool DiskfontAvailable = true,
    bool UtilityAvailable = true,
    bool FontOpenSuccess = true,
    bool ConsoleTaskAvailable = true,
    bool ConsoleWindowAvailable = true,
    bool DiskInfoResult = true,
    bool RastPortAvailable = true,
    bool OldFont = true,
    bool OutputFailure = false,
    string FontName = "topaz",
    int Size = 8,
    byte OpenedFontStyle = 0,
    byte OpenedFontFlags = 0,
    bool Scale = false,
    bool Prop = false,
    bool Italic = false,
    bool Bold = false,
    bool Underline = false,
    int AllocationFailureAt = -1);

internal sealed partial class ProbeFixture
{
    public const string Workbench31SetFontEntrySuite =
        "workbench31-setfont-native-entry-vector-fixture";

    private const uint SetFontGraphicsBase = 0x11000;
    private const uint SetFontDiskfontBase = 0x12000;
    private const uint SetFontUtilityBase = 0x13000;
    private const uint SetFontResultBytes = 7 * 4u;
    private const uint SetFontNameBytes = 80;
    private const uint SetFontTextAttrBytes = 12;
    private const uint SetFontInfoDataBytes = InfoData.Size;
    private const uint SetFontWindowRastPortOffset = 0x32;
    private const uint SetFontWindowFontOffset = 0x80;

    private List<object> RunSetFontEntryCases()
    {
        ProbeCase[] cases =
        [
            SetFontCase("default", new()),
            SetFontCase("all-switches", new(Scale: true, Prop: true,
                Italic: true, Bold: true, Underline: true),
                output: "\u001bc\u001b[3m\u001b[1m\u001b[4m"),
            SetFontCase("scale-clears-designed-font-flag",
                new(Scale: true)),
            SetFontCase("output-failures-do-not-change-success",
                new(OutputFailure: true, Italic: true),
                output: "\u001bc\u001b[3m"),
            SetFontCase("font-already-italic", new(Italic: true,
                OpenedFontStyle: 4), output: "\u001bc"),
            SetFontCase("font-already-bold", new(Bold: true,
                OpenedFontStyle: 2), output: "\u001bc"),
            SetFontCase("font-already-underlined", new(Underline: true,
                OpenedFontStyle: 1), output: "\u001bc"),
            SetFontCase("font-has-italic-and-underline", new(Italic: true,
                Bold: true, Underline: true, OpenedFontStyle: 5),
                output: "\u001bc\u001b[1m"),
            SetFontCase("proportional-font-rejected-by-default",
                new(OpenedFontFlags: 0x20), DOS.RETURN_FAIL,
                (int)DOS.Error.ObjectWrongType),
            SetFontCase("proportional-font-accepted-with-prop",
                new(Prop: true, OpenedFontFlags: 0x20)),
            SetFontCase("existing-font-suffix", new(FontName: "Topaz.FoNt")),
            SetFontCase("small-size", new(Size: 4), DOS.RETURN_FAIL,
                (int)DOS.Error.BadNumber),
            SetFontCase("size-low-word-zero", new(Size: 65536),
                DOS.RETURN_FAIL, (int)DOS.Error.BadNumber),
            SetFontCase("size-low-word-four", new(Size: 65540),
                DOS.RETURN_FAIL, (int)DOS.Error.BadNumber),
            SetFontCase("size-low-word-five", new(Size: 65541)),
            SetFontCase("negative-size-low-word-ffff", new(Size: -1)),
            SetFontCase("result-allocation-failure",
                new(AllocationFailureAt: 0), DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore),
            SetFontCase("name-allocation-failure",
                new(AllocationFailureAt: 1), DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore),
            SetFontCase("textattr-allocation-failure",
                new(AllocationFailureAt: 2), DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore),
            SetFontCase("infodata-allocation-failure",
                new(AllocationFailureAt: 3), DOS.RETURN_FAIL,
                (int)DOS.Error.NoFreeStore),
            SetFontCase("graphics-library-failure",
                new(GraphicsAvailable: false), DOS.RETURN_FAIL,
                (int)DOS.Error.InvalidResidentLibrary),
            SetFontCase("diskfont-library-failure",
                new(DiskfontAvailable: false), DOS.RETURN_FAIL,
                (int)DOS.Error.InvalidResidentLibrary),
            SetFontCase("utility-library-failure",
                new(UtilityAvailable: false), DOS.RETURN_FAIL,
                (int)DOS.Error.InvalidResidentLibrary),
            SetFontCase("font-open-failure",
                new(FontOpenSuccess: false), DOS.RETURN_FAIL,
                (int)DOS.Error.ObjectNotFound),
            SetFontCase("console-task-missing",
                new(ConsoleTaskAvailable: false), DOS.RETURN_FAIL,
                (int)DOS.Error.ObjectNotFound),
            SetFontCase("console-window-absent-retains-open-font",
                new(ConsoleWindowAvailable: false), DOS.RETURN_OK,
                output: ""),
            SetFontCase("window-present-packet-false",
                new(DiskInfoResult: false)),
            SetFontCase("rastport-missing",
                new(RastPortAvailable: false), DOS.RETURN_FAIL,
                (int)DOS.Error.ObjectWrongType),
            SetFontCase("parser-failure", new(ParserFailure: true),
                DOS.RETURN_FAIL, (int)DOS.Error.RequiredArgumentMissing),
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Workbench = true, SetFont = new() },
            new("missing-dos", "topaz 8\n", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { MissingDos = true, SetFont = new() }
        ];

        var reports = new List<object>();
        foreach (var test in cases)
            reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            SetFontCase("interleaved-default-a", new()),
            SetFontCase("interleaved-styled-b", new(Prop: true, Italic: true,
                OldFont: false), output: "\u001bc\u001b[3m")
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private static ProbeCase SetFontCase(string name, SetFontEntryCase definition,
        int result = DOS.RETURN_OK, int error = 0, string? output = null) =>
        new(name, $"{definition.FontName} {definition.Size}\n", result, error,
            output ?? (result == DOS.RETURN_OK ? "\u001bc" : ""))
        { SetFont = definition };

    private void PrepareSetFontEntry(Invocation invocation)
    {
        var definition = invocation.Definition.SetFont!;
        invocation.SetFontSourceName = invocation.Arguments + 0x300;
        invocation.SetFontSize = invocation.Arguments + 0x340;
        WriteCString(invocation.SetFontSourceName, definition.FontName);
        Bus.Long(invocation.SetFontSize, unchecked((uint)definition.Size));

        invocation.SetFontConsoleStorage = Bus.Allocate(invocation, 0x400,
            "SetFontConsole", true);
        invocation.SetFontConsoleTask = invocation.SetFontConsoleStorage;
        invocation.SetFontWindow = invocation.SetFontConsoleTask + 0x100;
        invocation.SetFontRastPort = invocation.SetFontConsoleTask + 0x200;
        invocation.SetFontOldFont = invocation.SetFontConsoleTask + 0x300;
        invocation.SetFontFont = invocation.SetFontConsoleTask + 0x380;
        Bus.Long(invocation.Process + (uint)DosLayout.Process.ConsoleTask,
            definition.ConsoleTaskAvailable ? invocation.SetFontConsoleTask : 0);
        Bus.Long(invocation.SetFontWindow + SetFontWindowRastPortOffset,
            definition.RastPortAvailable ? invocation.SetFontRastPort : 0);
        Bus.Long(invocation.SetFontWindow + SetFontWindowFontOffset,
            definition.OldFont ? invocation.SetFontOldFont : 0);
    }

    private void RegisterSetFontEntryExec()
    {
        Register(SetFontGraphicsBase, -66, "SetFont",
            (state, invocation) =>
            {
                Require(invocation.Forbidden &&
                    state.A[1] == invocation.SetFontRastPort &&
                    state.A[0] == invocation.SetFontFont,
                    "SetFont graphics ABI or critical-section ordering differs.");
                invocation.SetFontSetFontCalls++;
                return 1;
            });
        Register(SetFontGraphicsBase, -78, "CloseFont",
            (state, invocation) =>
            {
                Require(state.A[1] == invocation.SetFontOldFont ||
                    state.A[1] == invocation.SetFontFont,
                    "SetFont closed an unexpected font.");
                if (state.A[1] == invocation.SetFontOldFont)
                    Require(invocation.Forbidden,
                        "SetFont closed the console's old font outside Forbid.");
                invocation.SetFontCloseFontCalls++;
                return 0;
            });
        Register(SetFontDiskfontBase, -30, "OpenDiskFont",
            (state, invocation) =>
            {
                Require(state.A[0] == invocation.SetFontTextAttr,
                    "SetFont OpenDiskFont TextAttr pointer differs.");
                invocation.SetFontOpenDiskFontCalls++;
                var attr = invocation.SetFontTextAttr;
                Require(Bus.Long(attr) == invocation.SetFontName &&
                    Bus.Word(attr + 4) == (ushort)invocation.Definition.SetFont!.Size,
                    "SetFont TextAttr name/size differs.");
                var definition = invocation.Definition.SetFont!;
                var style = Bus.Memory[attr + 6];
                var flags = Bus.Memory[attr + 7];
                var expectedStyle = (definition.Italic ? 4 : 0) |
                    (definition.Bold ? 2 : 0) |
                    (definition.Underline ? 1 : 0);
                Require(style == expectedStyle,
                    "SetFont TextAttr style differs.");
                Require((flags & 0x20) != 0 == definition.Prop &&
                    (flags & 0x40) != 0 != definition.Scale,
                    "SetFont TextAttr flags differ.");
                Bus.Memory[invocation.SetFontFont + 22] =
                    definition.OpenedFontStyle;
                Bus.Memory[invocation.SetFontFont + 23] =
                    definition.OpenedFontFlags;
                return definition.FontOpenSuccess ? invocation.SetFontFont : 0;
            });
    }

    private void RegisterSetFontEntryDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.SetFont!;
            Require(Bus.CString(state.D[1]) == NativeWorkbench31SetFontCommand.Template &&
                state.D[3] == 0 && state.D[2] % 4 == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "SetFontResults").Size == SetFontResultBytes,
                "SetFont ReadArgs ABI differs.");
            invocation.SetFontReadArgsCalls++;
            if (definition.ParserFailure)
            {
                invocation.IoError = (int)DOS.Error.RequiredArgumentMissing;
                return 0;
            }
            Bus.Long(state.D[2], invocation.SetFontSourceName);
            Bus.Long(state.D[2] + 4, invocation.SetFontSize);
            Bus.Long(state.D[2] + 8, definition.Scale ? 1u : 0u);
            Bus.Long(state.D[2] + 12, definition.Prop ? 1u : 0u);
            Bus.Long(state.D[2] + 16, definition.Italic ? 1u : 0u);
            Bus.Long(state.D[2] + 20, definition.Bold ? 1u : 0u);
            Bus.Long(state.D[2] + 24, definition.Underline ? 1u : 0u);
            return Bus.Allocate(invocation, 40, "RDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.SetFontFreeArgsCalls++;
            invocation.IoError = 901;
            return 0;
        });
        Register(baseAddress, DosLvo.DoPkt, "DoPkt", (state, invocation) =>
        {
            Require(state.D[1] == invocation.SetFontConsoleTask && state.D[2] == 25 &&
                state.D[4] == 0 && state.D[5] == 0 && state.D[6] == 0 && state.D[7] == 0,
                "SetFont DoPkt ABI differs.");
            var infoData = unchecked((uint)state.D[3]) << 2;
            Require(infoData == invocation.SetFontInfoData,
                "SetFont ACTION_DISK_INFO InfoData BPTR differs.");
            invocation.SetFontDoPktCalls++;
            Bus.Long(infoData + (uint)DosLayout.InfoData.VolumeNode,
                invocation.Definition.SetFont!.ConsoleWindowAvailable
                    ? invocation.SetFontWindow : 0);
            return invocation.Definition.SetFont!.DiskInfoResult
                ? uint.MaxValue : 0u;
        });
        Register(baseAddress, DosLvo.Output, "Output", (_, invocation) => invocation.OutputBptr);
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            Require(state.D[1] != 0, "SetFont PutStr received a null string.");
            var text = Bus.CString(state.D[1]);
            invocation.Output.Write(Encoding.Latin1.GetBytes(text));
            invocation.SetFontPutStrCalls++;
            return invocation.Definition.SetFont!.OutputFailure ? 0u : 1u;
        });
        Register(baseAddress, DosLvo.Flush, "Flush", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "SetFont Flush stream differs.");
            invocation.SetFontFlushCalls++;
            return invocation.Definition.SetFont!.OutputFailure ? 0u : 1u;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (_, invocation) => { invocation.SetFontPrintFaultCalls++; return 0; });
        Register(baseAddress, DosLvo.IoErr, "IoErr", (_, invocation) =>
            unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void VerifySetFontEntry(Invocation invocation)
    {
        var definition = invocation.Definition.SetFont!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            var startupForbid = invocation.Definition.Workbench ? 1 : 0;
            Require(invocation.SetFontReadArgsCalls == 0 &&
                invocation.SetFontOpenDiskFontCalls == 0 &&
                invocation.SetFontForbidCalls == startupForbid &&
                invocation.SetFontPermitCalls == 0,
                "SetFont crossed a startup boundary.");
            return;
        }

        if (!definition.GraphicsAvailable || !definition.DiskfontAvailable ||
            !definition.UtilityAvailable)
        {
            Require(invocation.SetFontReadArgsCalls == 0,
                $"SetFont parsed after a required library failure (name={invocation.Definition.Name}, flags={definition.GraphicsAvailable}/{definition.DiskfontAvailable}/{definition.UtilityAvailable}, graphics={invocation.SetFontGraphicsOpens}, diskfont={invocation.SetFontDiskfontOpens}, utility={invocation.SetFontUtilityOpens}, reads={invocation.SetFontReadArgsCalls}).");
            Require(invocation.SetFontForbidCalls == 0 &&
                invocation.SetFontPermitCalls == 0,
                "SetFont entered the console critical section after a library failure.");
            return;
        }
        if (definition.AllocationFailureAt >= 0)
        {
            var failedAt = definition.AllocationFailureAt;
            var expectedAllocations = failedAt == 0 ? 1 : 4;
            var expectedFreeMem = failedAt == 0 ? 0 : 3;
            Require(invocation.Allocations == expectedAllocations &&
                invocation.FreeMem == expectedFreeMem &&
                invocation.SetFontReadArgsCalls == (failedAt == 0 ? 0 : 1) &&
                invocation.SetFontFreeArgsCalls == (failedAt == 0 ? 0 : 1) &&
                invocation.SetFontOpenDiskFontCalls == 0 &&
                invocation.SetFontDoPktCalls == 0 &&
                invocation.SetFontForbidCalls == 0 &&
                invocation.SetFontPermitCalls == 0 &&
                invocation.SetFontPrintFaultCalls == 1,
                $"SetFont allocation-failure cleanup differs at allocation {failedAt}: attempts={invocation.Allocations}, freed={invocation.FreeMem}, reads={invocation.SetFontReadArgsCalls}, FreeArgs={invocation.SetFontFreeArgsCalls}.");
            return;
        }
        Require(invocation.SetFontReadArgsCalls == 1,
            "SetFont ReadArgs count differs.");
        if (definition.ParserFailure)
        {
            Require(invocation.SetFontFreeArgsCalls == 0 &&
                invocation.SetFontOpenDiskFontCalls == 0 && invocation.FreeMem == 1,
                "SetFont parser failure ownership differs.");
            Require(invocation.SetFontForbidCalls == 0 &&
                invocation.SetFontPermitCalls == 0,
                "SetFont entered the console critical section after parser failure.");
            return;
        }
        Require(invocation.SetFontFreeArgsCalls == 1,
            "SetFont RDArgs was not released.");
        var sizeFailure = (unchecked((uint)definition.Size) & 0xFFFFu) <= 4u;
        if (sizeFailure)
        {
            Require(invocation.SetFontOpenDiskFontCalls == 0 &&
                invocation.FreeMem == 1,
                "SetFont rejected size after opening resources.");
            Require(invocation.SetFontForbidCalls == 0 &&
                invocation.SetFontPermitCalls == 0,
                "SetFont entered the console critical section after size rejection.");
            return;
        }
        Require(invocation.FreeMem == 4,
            "SetFont allocation/free count differs.");
        if (!definition.FontOpenSuccess)
        {
            Require(invocation.SetFontOpenDiskFontCalls == 1 &&
                invocation.SetFontCloseFontCalls == 0,
                "SetFont font-open failure cleanup differs.");
            Require(invocation.SetFontForbidCalls == 0 &&
                invocation.SetFontPermitCalls == 0,
                "SetFont entered the console critical section after font-open failure.");
            return;
        }
        if (!definition.Prop && (definition.OpenedFontFlags & 0x20) != 0)
        {
            Require(invocation.SetFontOpenDiskFontCalls == 1 &&
                invocation.SetFontCloseFontCalls == 1 &&
                invocation.SetFontDoPktCalls == 0 &&
                invocation.SetFontForbidCalls == 0 &&
                invocation.SetFontPermitCalls == 0,
                "SetFont proportional-font rejection differs.");
            return;
        }
        if (!definition.ConsoleTaskAvailable || !definition.RastPortAvailable)
        {
            Require(invocation.SetFontDoPktCalls == (definition.ConsoleTaskAvailable ? 1 : 0) &&
                invocation.SetFontCloseFontCalls == 1,
                "SetFont console failure cleanup differs.");
            Require(invocation.SetFontForbidCalls == 0 &&
                invocation.SetFontPermitCalls == 0,
                "SetFont entered the console critical section without a valid console target.");
            return;
        }
        if (!definition.ConsoleWindowAvailable)
        {
            Require(invocation.SetFontDoPktCalls == 1 &&
                invocation.SetFontOpenDiskFontCalls == 1 &&
                invocation.SetFontSetFontCalls == 0 &&
                invocation.SetFontForbidCalls == 0 &&
                invocation.SetFontPermitCalls == 0 &&
                invocation.SetFontCloseFontCalls == 0 &&
                invocation.SetFontPutStrCalls == 0 &&
                invocation.SetFontFlushCalls == 0,
                "SetFont absent-window behavior differs.");
            return;
        }
        var expectedStyleOutputCount =
            (definition.Italic && (definition.OpenedFontStyle & 4) == 0 ? 1 : 0) +
            (definition.Bold && (definition.OpenedFontStyle & 2) == 0 ? 1 : 0) +
            (definition.Underline && (definition.OpenedFontStyle & 1) == 0 ? 1 : 0);
        Require(invocation.SetFontDoPktCalls == 1 && invocation.SetFontSetFontCalls == 1 &&
            invocation.SetFontForbidCalls == 1 && invocation.SetFontPermitCalls == 1 &&
            invocation.SetFontCloseFontCalls == (definition.OldFont ? 1 : 0) &&
            invocation.SetFontPutStrCalls == 1 + expectedStyleOutputCount &&
            invocation.SetFontFlushCalls == 2,
            $"SetFont successful operation vector differs (DoPkt={invocation.SetFontDoPktCalls}, SetFont={invocation.SetFontSetFontCalls}, CloseFont={invocation.SetFontCloseFontCalls}, PutStr={invocation.SetFontPutStrCalls}, Flush={invocation.SetFontFlushCalls}, old={definition.OldFont}, styled={definition.Italic || definition.Bold || definition.Underline}).");
    }

    private void ReclaimSetFontConsoleStorage(Invocation invocation)
    {
        if (invocation.SetFontConsoleStorage != 0)
        {
            Bus.Release(invocation, invocation.SetFontConsoleStorage,
                "SetFontConsole", 0x400);
            invocation.SetFontConsoleStorage = 0;
        }
    }
}
