using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed partial class ProbeFixture
{
    public const string MorphOSSetDateEntrySuite =
        "morphos320-setdate-native-entry-vector-fixture";

    private List<object> RunMorphOSSetDateEntryCases()
    {
        var cases = new[]
        {
            MorphOSSetDateCase("success", "RAM:file", DOS.RETURN_OK, 0),
            MorphOSSetDateCase("date-fallback", "RAM:fallback", DOS.RETURN_OK,
                0, dateFailures: 1),
            MorphOSSetDateCase("directory-all", "RAM:dir", DOS.RETURN_OK, 0,
                all: true, directory: true),
            MorphOSSetDateCase("directory-without-all", "RAM:dir-no-all",
                DOS.RETURN_OK, 0, directory: true),
            MorphOSSetDateCase("softlink-update-failure", "RAM:link",
                DOS.RETURN_OK, 0, softLink: true,
                setFileDateSucceeds: false),
            MorphOSSetDateCase("no-match", "RAM:missing", DOS.RETURN_FAIL,
                205, match: false),
            MorphOSSetDateCase("setfiledate-failure", "RAM:bad",
                DOS.RETURN_FAIL, 205, setFileDateSucceeds: false),
            MorphOSSetDateCase("break", "RAM:break", DOS.RETURN_WARN,
                (int)DOS.Error.Break, nextError: (int)DOS.Error.Break),
            MorphOSSetDateCase("invalid-date", "RAM:file", DOS.RETURN_FAIL,
                116, output: "SetDate failed: Invalid WEEKDAY, DATE or TIME string!\n",
                dateFailures: 2),
            MorphOSSetDateCase("parser-failure", "RAM:file", DOS.RETURN_FAIL,
                116, parserError: 116)
        };

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

    private static ProbeCase MorphOSSetDateCase(string name, string file,
        int result, int error, bool all = false, bool match = true,
        bool directory = false, bool softLink = false,
        bool setFileDateSucceeds = true, int parserError = 0,
        int nextError = (int)DOS.Error.NoMoreEntries, int dateFailures = 0,
        string output = "") =>
        new(name, $"{file}\n", result, error, output)
        {
            SetDate = new(file,
                DateToken: parserError == 0 ? "02-Jan-91" : null,
                All: all, Match: match, Directory: directory,
                SoftLink: softLink, SetFileDateSucceeds: setFileDateSucceeds,
                ParserError: parserError, NextError: nextError,
                StrToDateFailures: dateFailures)
        };

    private void VerifyMorphOSSetDateEntry(Invocation invocation)
    {
        var definition = invocation.Definition.SetDate!;
        var parserFailure = definition.ParserError != 0;
        var invalidDate = !parserFailure && definition.StrToDateFailures >= 2;
        var matched = !parserFailure && !invalidDate && definition.Match;
        var directoryWalk = matched && definition.Directory && definition.All;
        var updated = matched && !directoryWalk;
        var updateFailure = updated && !definition.SetFileDateSucceeds &&
            !definition.SoftLink;
        var expectedDateCalls = parserFailure || invalidDate ? 0 : 1;
        if (!parserFailure && !invalidDate)
            expectedDateCalls = definition.DateToken is null ? 0 :
                1 + definition.StrToDateFailures;
        else if (invalidDate)
            expectedDateCalls = 2;

        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "MorphOS SetDate parser lifetime differs.");
        Require(invocation.Allocations == (parserFailure ? 1 : invalidDate ? 2 : 3) &&
            invocation.FreeMem == (parserFailure ? 1 : invalidDate ? 2 : 3),
            "MorphOS SetDate temporary storage lifetime differs.");
        Require(invocation.SetDateDateStampCalls == (parserFailure ? 0 : 1) &&
            invocation.SetDateStrToDateCalls == expectedDateCalls,
            "MorphOS SetDate date conversion differs.");
        Require(invocation.SetDateMatchFirstCalls == (matched || !parserFailure && !invalidDate ? 1 : 0) &&
            invocation.SetDateMatchEndCalls == (matched || !parserFailure && !invalidDate ? 1 : 0) &&
            invocation.SetDateMatchNextCalls == (matched && !updateFailure ? 1 : 0),
            $"{invocation.Definition.Name}: MorphOS SetDate matcher lifetime differs.");
        Require(invocation.SetDateFileDateCalls == (updated ? 1 : 0) &&
            invocation.SetDateDupLockCalls == (updated ? 1 : 0) &&
            invocation.SetDateCurrentDirCalls == (updated ? 2 : 0) &&
            invocation.SetDateUnLockCalls == (updated ? 1 : 0),
            "MorphOS SetDate update lifetime differs.");
        var expectedFault = parserFailure || !definition.Match || updateFailure ||
            definition.NextError != (int)DOS.Error.NoMoreEntries;
        if (definition.SoftLink && updateFailure) expectedFault = false;
        Require(invocation.SetDatePrintFaultCalls == (expectedFault ? 1 : 0),
            "MorphOS SetDate diagnostic path differs.");
        if (!parserFailure && !invalidDate)
            Require(invocation.Events.IndexOf("MatchEnd") <
                invocation.Events.IndexOf("FreeMem"),
                "MorphOS SetDate cleanup order differs.");
    }

    private void RegisterMorphOSSetDateEntryExec()
    {
        // MorphOS SetDate uses only the shared AllocMem/FreeMem gateways.
    }

    private void RegisterMorphOSSetDateDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.SetDate!;
            var layout = invocation.SetDateLayout!;
            Require(Bus.CString(state.D[1]) ==
                "FILE/A,WEEKDAY,DATE,TIME,ALL/S" && state.D[3] == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 20,
                "MorphOS SetDate ReadArgs ABI differs.");
            for (var offset = 0u; offset < 20; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "MorphOS SetDate result slots were not cleared.");
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
                "MorphOS SetDate DateStamp pointer differs.");
            Bus.Long(state.D[1] + (uint)DosLayout.DateTime.Stamp, 1);
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
                "MorphOS SetDate StrToDate pointer differs.");
            invocation.SetDateStrToDateCalls++;
            if (invocation.SetDateStrToDateCalls <= definition.StrToDateFailures)
            {
                invocation.IoError = 116;
                return 0;
            }
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
                Bus.Word(anchor + (uint)DosLayout.AnchorPath.StringLength) == 0x8000 &&
                Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Reserved] == 4,
                "MorphOS SetDate AnchorPath extension differs.");
            invocation.SetDateMatchFirstCalls++;
            if (!definition.Match)
            {
                invocation.IoError = definition.MatchError;
                return unchecked((uint)definition.MatchError);
            }
            Bus.Long(anchor + (uint)DosLayout.AnchorPath.Current,
                layout.Chain);
            Bus.Long(layout.Chain + (uint)DosLayout.AChain.Lock, 0x5200);
            Bus.Long(anchor + (uint)DosLayout.AnchorPath.StringLength, 0x8000);
            PutSetDateString(anchor + (uint)DosLayout.AnchorPath.Info +
                (uint)FileInfoBlock.FileNameOffset, Leaf(definition.File));
            var type = definition.SoftLink ? (int)DosConstants.SoftLink :
                definition.Directory ? 2 : -3;
            Bus.Long(anchor + (uint)DosLayout.AnchorPath.Info +
                (uint)FileInfoBlock.DirEntryTypeOffset, unchecked((uint)type));
            invocation.IoError = 0;
            return 0;
        });
        Register(baseAddress, DosLvo.MatchNext, "MatchNext", (state,
            invocation) =>
        {
            var definition = invocation.Definition.SetDate!;
            var anchor = invocation.SetDateLayout!.Anchor;
            Require(state.D[1] == anchor,
                "MorphOS SetDate MatchNext anchor differs.");
            if (definition.Directory && definition.All &&
                invocation.SetDateMatchNextCalls == 0)
                Require((Bus.Memory[anchor + (uint)DosLayout.AnchorPath.Flags] &
                    (byte)AnchorPathFlags.DoDirectory) != 0,
                    "MorphOS SetDate did not request directory descent.");
            invocation.SetDateMatchNextCalls++;
            invocation.IoError = definition.NextError;
            return unchecked((uint)definition.NextError);
        });
        Register(baseAddress, DosLvo.MatchEnd, "MatchEnd", (state,
            invocation) =>
        {
            Require(state.D[1] == invocation.SetDateLayout!.Anchor,
                "MorphOS SetDate MatchEnd anchor differs.");
            invocation.SetDateMatchEndCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.DupLock, "DupLock", (state, invocation) =>
        {
            Require(state.D[1] == 0x5200, "MorphOS SetDate DupLock source differs.");
            invocation.SetDateDupLockCalls++;
            return 0x5300;
        });
        Register(baseAddress, DosLvo.CurrentDir, "CurrentDir", (state,
            invocation) =>
        {
            Require(state.D[1] is 0x5300 or 0x5400,
                "MorphOS SetDate CurrentDir lock differs.");
            invocation.SetDateCurrentDirCalls++;
            return 0x5400;
        });
        Register(baseAddress, DosLvo.UnLock, "UnLock", (state, invocation) =>
        {
            Require(state.D[1] == 0x5300,
                "MorphOS SetDate UnLock lock differs.");
            invocation.SetDateUnLockCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.SetFileDate, "SetFileDate", (state,
            invocation) =>
        {
            var definition = invocation.Definition.SetDate!;
            Require(Bus.CString(state.D[1]) == Leaf(definition.File) &&
                state.D[2] == invocation.SetDateLayout!.DateTime,
                "MorphOS SetDate SetFileDate ABI differs.");
            invocation.SetDateFileDateCalls++;
            invocation.IoError = definition.UpdateError;
            return definition.SetFileDateSucceeds ? 1u : 0;
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            invocation.Output.Write(Encoding.Latin1.GetBytes(Bus.CString(state.D[1])));
            return 1;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                var definition = invocation.Definition.SetDate!;
                if (definition.ParserError != 0)
                    Require(state.D[2] == 0,
                        "MorphOS SetDate parser fault header differs.");
                else
                    Require(Bus.CString(state.D[2]) == "SetDate failed",
                        "MorphOS SetDate fault header differs.");
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
}
