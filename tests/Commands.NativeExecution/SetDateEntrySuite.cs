using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record SetDateEntryCase(
    string File,
    string? WeekdayToken = null,
    string? DateToken = "02-Jan-91",
    string? TimeToken = null,
    bool All = false,
    bool Match = true,
    bool Directory = false,
    bool SoftLink = false,
    bool SetFileDateSucceeds = true,
    int ParserError = 0,
    int MatchError = 205,
    int UpdateError = 205,
    int NextError = (int)DOS.Error.NoMoreEntries,
    int StrToDateFailures = 0,
    bool AllocationFailure = false);

internal sealed class SetDateNativeLayout(uint control, uint file, uint weekday,
    uint date, uint time, uint chain)
{
    public uint Control { get; } = control;
    public uint File { get; } = file;
    public uint Weekday { get; } = weekday;
    public uint Date { get; } = date;
    public uint Time { get; } = time;
    public uint Chain { get; } = chain;
    public uint Anchor { get; set; }
    public uint DateTime { get; set; }
}

internal sealed partial class ProbeFixture
{
    public const string Workbench31SetDateEntrySuite =
        "workbench31-setdate-native-entry-vector-fixture";

    private List<object> RunWorkbench31SetDateEntryCases()
    {
        ProbeCase[] cases =
        [
            SetDateCase("success", "RAM:file", DOS.RETURN_OK, 0),
            SetDateCase("date-fallback", "RAM:fallback", DOS.RETURN_OK, 0,
                dateFailures: 1),
            SetDateCase("directory-all", "RAM:dir", DOS.RETURN_OK, 0,
                all: true, directory: true),
            SetDateCase("directory-without-all", "RAM:dir-no-all", DOS.RETURN_OK,
                0, directory: true),
            SetDateCase("no-match", "RAM:missing", DOS.RETURN_FAIL, 205,
                match: false),
            SetDateCase("setfiledate-failure", "RAM:bad", DOS.RETURN_FAIL, 205,
                setFileDateSucceeds: false),
            SetDateCase("break", "RAM:break", DOS.RETURN_WARN,
                (int)DOS.Error.Break, nextError: (int)DOS.Error.Break),
            SetDateCase("parser-failure", "RAM:file", DOS.RETURN_ERROR, 116,
                parserError: 116),
        ];

        cases = [
            .. cases,
            new ProbeCase("workbench-startup", "FILE/A", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            {
                Workbench = true,
                SetDate = new SetDateEntryCase("RAM:startup")
            },
            new ProbeCase("missing-dos", "FILE/A", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            {
                MissingDos = true,
                SetDate = new SetDateEntryCase("RAM:missing-dos")
            }
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

    private static ProbeCase SetDateCase(string name, string file, int result,
        int error, bool all = false, bool match = true,
        bool directory = false, bool setFileDateSucceeds = true,
        int parserError = 0, int nextError = (int)DOS.Error.NoMoreEntries,
        int dateFailures = 0, bool softLink = false) =>
        new(name, $"{file}\n", result, error, "")
        {
            SetDate = new(file, DateToken: parserError == 0 ? "02-Jan-91" : null,
                All: all, Match: match, Directory: directory,
                SoftLink: softLink,
                SetFileDateSucceeds: setFileDateSucceeds,
                ParserError: parserError, NextError: nextError,
                StrToDateFailures: dateFailures)
        };

    private void PrepareSetDateEntry(Invocation invocation)
    {
        var definition = invocation.Definition.SetDate ??
            throw new InvalidOperationException("Missing SetDate definition.");
        var layout = new SetDateNativeLayout(
            invocation.Arguments,
            invocation.Arguments + 0x400,
            invocation.Arguments + 0x500,
            invocation.Arguments + 0x600,
            invocation.Arguments + 0x700,
            invocation.Arguments + 0x900);
        invocation.SetDateLayout = layout;
        PutSetDateString(layout.File, definition.File);
        if (definition.WeekdayToken is not null)
            PutSetDateString(layout.Weekday, definition.WeekdayToken);
        if (definition.DateToken is not null)
            PutSetDateString(layout.Date, definition.DateToken);
        if (definition.TimeToken is not null)
            PutSetDateString(layout.Time, definition.TimeToken);
        Bus.Long(layout.Chain + (uint)DosLayout.AChain.Lock, 0x5200);
    }

    private void VerifyWorkbench31SetDateEntry(Invocation invocation)
    {
        var definition = invocation.Definition.SetDate ??
            throw new InvalidOperationException("Missing SetDate definition.");
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.Reads == 0 && invocation.FreeArgs == 0 &&
                invocation.Allocations == 0 && invocation.FreeMem == 0 &&
                invocation.SetDateAllocVecCalls == 0 &&
                invocation.SetDateFreeVecCalls == 0 &&
                invocation.SetDateDateStampCalls == 0 &&
                invocation.SetDateStrToDateCalls == 0 &&
                invocation.SetDateMatchFirstCalls == 0 &&
                invocation.SetDateMatchNextCalls == 0 &&
                invocation.SetDateMatchEndCalls == 0 &&
                invocation.SetDateDupLockCalls == 0 &&
                invocation.SetDateCurrentDirCalls == 0 &&
                invocation.SetDateUnLockCalls == 0 &&
                invocation.SetDateFileDateCalls == 0 &&
                invocation.SetDatePrintFaultCalls == 0,
                "SetDate startup boundary reached the command body.");
            invocation.SetDateLayout = null;
            return;
        }
        var parserFailure = definition.ParserError != 0;
        var matched = !parserFailure && definition.Match;
        var updated = matched && definition.SetFileDateSucceeds;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "SetDate parser lifetime differs.");
        Require(invocation.SetDateAllocVecCalls == (parserFailure ? 0 : 1) &&
            invocation.SetDateFreeVecCalls == (parserFailure ? 0 : 1),
            "SetDate AnchorPath allocation lifetime differs.");
        Require(invocation.Allocations == (parserFailure ? 1 : 3) &&
            invocation.FreeMem == (parserFailure ? 1 : 2),
            "SetDate allocation cleanup differs.");
        Require(invocation.SetDateDateStampCalls == (parserFailure ? 0 : 1),
            "SetDate DateStamp call count differs.");
        var expectedStrToDate = parserFailure ? 0 :
            (definition.WeekdayToken is null ? 0 : 1 + definition.StrToDateFailures) +
            (definition.DateToken is null ? 0 : 1 + definition.StrToDateFailures) +
            (definition.TimeToken is null ? 0 : 1 + definition.StrToDateFailures);
        Require(invocation.SetDateStrToDateCalls == expectedStrToDate,
            "SetDate date conversion call count differs.");
        Require(invocation.SetDateMatchFirstCalls == (parserFailure ? 0 : 1) &&
            invocation.SetDateMatchEndCalls == (parserFailure ? 0 : 1),
            "SetDate matcher lifetime differs.");
        Require(invocation.SetDateMatchNextCalls == (updated ? 1 : 0) &&
            invocation.SetDateFileDateCalls == (matched ? 1 : 0),
            "SetDate update/advance count differs.");
        Require(invocation.SetDateDupLockCalls == (matched ? 1 : 0) &&
            invocation.SetDateCurrentDirCalls == (matched ? 2 : 0) &&
            invocation.SetDateUnLockCalls == (matched ? 1 : 0),
            "SetDate lock/current-directory lifetime differs.");
        var expectedFault = parserFailure || !definition.Match ||
            !definition.SetFileDateSucceeds || definition.NextError !=
            (int)DOS.Error.NoMoreEntries;
        Require(invocation.SetDatePrintFaultCalls == (expectedFault ? 1 : 0),
            "SetDate diagnostic path differs.");
        if (!parserFailure)
            Require(invocation.Events.IndexOf("MatchEnd") <
                invocation.Events.IndexOf("FreeVec") &&
                invocation.Events.IndexOf("FreeVec") <
                invocation.Events.IndexOf("FreeArgs"),
                "SetDate cleanup order differs.");
        invocation.SetDateLayout = null;
    }

    private void RegisterWorkbench31SetDateEntryExec()
    {
        // SetDate uses the common Exec allocator and FreeMem fixtures plus
        // the SetDate-specific AllocVec/FreeVec branches in Program.cs.
    }

    private void RegisterWorkbench31SetDateDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.SetDate!;
            var layout = invocation.SetDateLayout!;
            Require(Bus.CString(state.D[1]) ==
                "FILE/A,WEEKDAY,DATE,TIME,ALL/S" && state.D[3] == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 20,
                "SetDate ReadArgs template/result ABI differs.");
            for (var offset = 0u; offset < 20; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "SetDate result slots were not cleared.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            Bus.Long(state.D[2], layout.File);
            if (definition.WeekdayToken is not null)
                Bus.Long(state.D[2] + 4, layout.Weekday);
            if (definition.DateToken is not null)
                Bus.Long(state.D[2] + 8, layout.Date);
            if (definition.TimeToken is not null)
                Bus.Long(state.D[2] + 12, layout.Time);
            if (definition.All)
                Bus.Long(state.D[2] + 16, uint.MaxValue);
            return Bus.Allocate(invocation, 40, "RDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "RDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.DateStamp, "DateStamp", (state,
            invocation) =>
        {
            Require(state.D[1] == invocation.SetDateLayout!.DateTime,
                "SetDate DateStamp pointer differs.");
            Bus.Long(state.D[1] + (uint)DosLayout.DateTime.Stamp + 0, 1);
            Bus.Long(state.D[1] + (uint)DosLayout.DateTime.Stamp + 4, 2);
            Bus.Long(state.D[1] + (uint)DosLayout.DateTime.Stamp + 8, 3);
            invocation.SetDateDateStampCalls++;
            invocation.IoError = 0;
            return 1;
        });
        Register(baseAddress, DosLvo.StrToDate, "StrToDate", (state,
            invocation) =>
        {
            var definition = invocation.Definition.SetDate!;
            Require(state.D[1] == invocation.SetDateLayout!.DateTime,
                "SetDate StrToDate pointer differs.");
            invocation.SetDateStrToDateCalls++;
            if (invocation.SetDateStrToDateCalls <= definition.StrToDateFailures)
            {
                invocation.IoError = 116;
                return 0;
            }
            Bus.Long(state.D[1] + (uint)DosLayout.DateTime.Stamp + 0, 4);
            Bus.Long(state.D[1] + (uint)DosLayout.DateTime.Stamp + 4, 5);
            Bus.Long(state.D[1] + (uint)DosLayout.DateTime.Stamp + 8, 6);
            invocation.IoError = 0;
            return 1;
        });
        Register(baseAddress, DosLvo.MatchFirst, "MatchFirst", (state,
            invocation) =>
        {
            var definition = invocation.Definition.SetDate!;
            var layout = invocation.SetDateLayout!;
            var anchor = state.D[2];
            Require(Bus.CString(state.D[1]) == definition.File &&
                Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] == 1 &&
                Bus.Long(anchor + (uint)DosLayout.AnchorPath.BreakBits) == 0x1000 &&
                Bus.Word(anchor + (uint)DosLayout.AnchorPath.StringLength) == 0,
                "SetDate MatchFirst AnchorPath policy differs.");
            invocation.SetDateMatchFirstCalls++;
            if (!definition.Match)
            {
                invocation.IoError = definition.MatchError;
                return unchecked((uint)definition.MatchError);
            }
            Bus.Long(anchor + (uint)DosLayout.AnchorPath.Current,
                layout.Chain);
            Bus.Long(layout.Chain + (uint)DosLayout.AChain.Lock, 0x5200);
            Bus.Long(anchor + (uint)DosLayout.AnchorPath.StringLength,
                definition.Directory ? 1u : 0u);
            PutSetDateString(anchor + (uint)DosLayout.AnchorPath.Info +
                (uint)FileInfoBlock.FileNameOffset, Leaf(definition.File));
            Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
                (uint)FileInfoBlock.DirEntryTypeOffset,
                definition.Directory ? 2u : unchecked((uint)-3));
            invocation.IoError = 0;
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state,
            invocation) =>
        {
            var definition = invocation.Definition.SetDate!;
            Require(state.D[1] == invocation.SetDateLayout!.Anchor &&
                invocation.SetDateMatchFirstCalls == 1,
                "SetDate MatchNext ownership differs.");
            invocation.SetDateMatchNextCalls++;
            invocation.IoError = definition.NextError;
            return unchecked((uint)definition.NextError);
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state,
            invocation) =>
        {
            Require(state.D[1] == invocation.SetDateLayout!.Anchor,
                "SetDate MatchEnd anchor differs.");
            invocation.SetDateMatchEndCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.DupLock, "DupLock", (state,
            invocation) =>
        {
            Require(state.D[1] == 0x5200, "SetDate DupLock source differs.");
            invocation.SetDateDupLockCalls++;
            return 0x5300;
        });
        Register(baseAddress, DosLvo.CurrentDir, "CurrentDir", (state,
            invocation) =>
        {
            Require(state.D[1] is 0x5300 or 0x5400,
                "SetDate CurrentDir lock differs.");
            invocation.SetDateCurrentDirCalls++;
            return 0x5400;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state,
            invocation) =>
        {
            Require(state.D[1] == 0x5300,
                "SetDate UnLock lock differs.");
            invocation.SetDateUnLockCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.SetFileDate, "SetFileDate", (state,
            invocation) =>
        {
            var definition = invocation.Definition.SetDate!;
            Require(Bus.CString(state.D[1]) == Leaf(definition.File) &&
                state.D[2] == invocation.SetDateLayout!.DateTime,
                "SetDate SetFileDate ABI differs.");
            invocation.SetDateFileDateCalls++;
            invocation.IoError = definition.UpdateError;
            return definition.SetFileDateSucceeds ? 1u : 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(state.D[1] != 0 && state.D[2] != 0,
                    "SetDate PrintFault arguments are incomplete.");
                invocation.SetDatePrintFaultCalls++;
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

    private void PutSetDateString(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[address + (uint)bytes.Length] = 0;
    }
}
