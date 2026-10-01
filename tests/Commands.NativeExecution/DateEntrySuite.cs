using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed record DateEntryCase(bool Setter, bool To,
    bool TimerOpenFailure = false, bool ToOpenFailure = false,
    int ParserError = 0, bool LFormat = false,
    bool LocaleOpenFailure = false, bool TimerIoFailure = false,
    bool WriteFailure = false);

internal sealed partial class ProbeFixture
{
    public const string Workbench31DateEntrySuite =
        "workbench31-date-native-entry-vector-fixture";

    private List<object> RunWorkbench31DateEntryCases()
    {
        ProbeCase[] cases =
        [
            new("display", "", DOS.RETURN_OK, 0,
                "Mon 01-Jan-90 12:34:56\n")
            { Date = new(false, false) },
            new("display-to", "TO=RAM:date.out\n", DOS.RETURN_OK, 0,
                "Mon 01-Jan-90 12:34:56\n")
            { Date = new(false, true) },
            new("set-date", "DATE=02-Jan-91\n", DOS.RETURN_OK, 0,
                "Mon 01-Jan-90 12:34:56\n")
            { Date = new(true, false) },
            new("parser-failure", "DATE=bad\n", DOS.RETURN_ERROR, 116, "")
            { Date = new(false, false, ParserError: 116) },
            new("timer-open-failure", "DATE=02-Jan-91\n", DOS.RETURN_FAIL,
                (int)DOS.Error.NotImplemented, "")
            { Date = new(true, false, TimerOpenFailure: true) },
            new("workbench-startup", "", DOS.RETURN_ERROR,
                (int)DOS.Error.ObjectWrongType, "")
            { Workbench = true, Date = new(false, false) },
            new("missing-dos", "", DOS.RETURN_FAIL,
                Invocation.InitialIoError, "")
            { MissingDos = true, Date = new(false, false) },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "repeat-display" },
            cases[2] with { Name = "repeat-set-date" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void VerifyWorkbench31DateEntry(Invocation invocation)
    {
        var definition = invocation.Definition.Date!;
        if (invocation.Definition.Workbench || invocation.Definition.MissingDos)
        {
            Require(invocation.Reads == 0 && invocation.FreeArgs == 0 &&
                invocation.Allocations == 0 && invocation.FreeMem == 0 &&
                invocation.UtilityOpens == 0 && invocation.UtilityCloses == 0 &&
                invocation.DateStampCalls == 0 && invocation.DateToStrCalls == 0 &&
                invocation.StrToDateCalls == 0 && invocation.TimerOpenCalls == 0 &&
                invocation.TimerDoIoCalls == 0 && invocation.TimerCloseCalls == 0 &&
                invocation.DateFileOpens == 0 && invocation.DateFileCloses == 0 &&
                invocation.DateVFPrintfCalls == 0 && invocation.DateVPrintfCalls == 0 &&
                invocation.DatePrintFaultCalls == 0,
                "Date startup boundary reached the command body.");
            return;
        }
        var parserFailure = definition.ParserError != 0;
        var setter = definition.Setter && !parserFailure;
        var to = definition.To && !parserFailure && !definition.TimerOpenFailure;
        var expectedAllocations = parserFailure ? 1u : setter ? 7u : 6u;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "Date parser ownership differs.");
        Require(invocation.Allocations == expectedAllocations &&
            invocation.FreeMem == expectedAllocations,
            "Date allocation cleanup differs.");
        Require(invocation.UtilityOpens == (setter ? 1 : 0) &&
            invocation.UtilityCloses == (setter ? 1 : 0),
            "Date utility.library lifetime differs.");
        Require(invocation.DateStampCalls == (parserFailure ? 0 : 1) &&
            invocation.DateToStrCalls == (to || (!definition.TimerOpenFailure &&
                !parserFailure) ? 1 : 0),
            "Date conversion call count differs.");
        Require(invocation.StrToDateCalls == (setter ? 1 : 0),
            "Date parser conversion count differs.");
        Require(invocation.TimerOpenCalls == (setter ? 1 : 0) &&
            invocation.TimerDoIoCalls == (setter && !definition.TimerOpenFailure ? 1 : 0) &&
            invocation.TimerCloseCalls == (setter && !definition.TimerOpenFailure ? 1 : 0),
            "Date timer.device lifecycle differs.");
        Require(invocation.DateFileOpens == (to ? 1 : 0) &&
            invocation.DateFileCloses == (to ? 1 : 0) &&
            invocation.DateVFPrintfCalls == (to ? 1 : 0) &&
            invocation.DateVPrintfCalls == (!definition.To &&
                !parserFailure && !definition.TimerOpenFailure ? 1 : 0),
            "Date output path differs.");
        Require(invocation.DatePrintFaultCalls == 0,
            "Date unexpectedly printed an output fault.");
    }

    private void RegisterWorkbench31DateDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.Date!;
            Require(Bus.CString(state.D[1]) == "DAY,DATE,TIME,TO=VER/K" &&
                state.D[3] == 0 && Bus.OwnedAllocation(invocation, state.D[2],
                    "Exec").Size == 16, "Date template or result storage differs.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }

            var rdArgs = Bus.Allocate(invocation, 64, "DateRDArgs", true);
            if (definition.Setter)
            {
                WriteArgument(rdArgs + 4, "02-Jan-91");
                Bus.Long(state.D[2] + 4, rdArgs + 4);
            }
            if (definition.To)
            {
                WriteArgument(rdArgs + 24, "RAM:date.out");
                Bus.Long(state.D[2] + 12, rdArgs + 24);
            }
            invocation.IoError = 0;
            return rdArgs;
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "DateRDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.DateStamp, "DateStamp", (state, invocation) =>
        {
            Require(state.D[1] != 0, "DateStamp received a null DateTime.");
            Bus.Long(state.D[1] + 0, 1);
            Bus.Long(state.D[1] + 4, 2);
            Bus.Long(state.D[1] + 8, 3);
            invocation.DateStampCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.StrToDate, "StrToDate", (state, invocation) =>
        {
            var definition = invocation.Definition.Date!;
            invocation.StrToDateCalls++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            Bus.Long(state.D[1] + 0, 2);
            Bus.Long(state.D[1] + 4, 3);
            Bus.Long(state.D[1] + 8, 4);
            return 1;
        });
        Register(baseAddress, DosLvo.DateToStr, "DateToStr", (state, invocation) =>
        {
            Require(state.D[1] != 0, "DateToStr received a null DateTime.");
            var dateTime = state.D[1];
            var day = Bus.Long(dateTime + (uint)DosLayout.DateTime.Day);
            var date = Bus.Long(dateTime + (uint)DosLayout.DateTime.Date);
            var time = Bus.Long(dateTime + (uint)DosLayout.DateTime.Time);
            Require(day != 0 && date != 0 && time != 0,
                "DateToStr pointers were not initialized.");
            WriteArgument(day, "Mon");
            WriteArgument(date, "01-Jan-90");
            WriteArgument(time, "12:34:56");
            invocation.DateToStrCalls++;
            return 1;
        });
        Register(baseAddress, DosLvo.Open, "Open", (state, invocation) =>
        {
            var definition = invocation.Definition.Date!;
            Require(Bus.CString(state.D[1]) == "RAM:date.out" &&
                state.D[2] == (uint)DOS.FileMode.NewFile,
                "Date TO open ABI differs.");
            invocation.DateFileOpens++;
            if (definition.ToOpenFailure)
            {
                invocation.IoError = (int)DOS.Error.ObjectNotFound;
                return 0;
            }
            return invocation.OutputBptr;
        });
        Register(baseAddress, DosLvo.VFPrintf, "VFPrintf", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr &&
                Bus.CString(state.D[2]) == "%s %s %s\n",
                "Date VFPrintf ABI differs.");
            invocation.DateVFPrintfCalls++;
            WriteDateOutput(invocation, state.D[3]);
            return 1;
        });
        Register(baseAddress, DosLvo.VPrintf, "VPrintf", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == "%s %s %s\n",
                "Date VPrintf format differs.");
            invocation.DateVPrintfCalls++;
            WriteDateOutput(invocation, state.D[2]);
            return 1;
        });
        Register(baseAddress, DosLvo.Close, "Close", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "Date closed an unexpected file handle.");
            invocation.DateFileCloses++;
            return 1;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(state.D[1] != 0 && state.D[2] != 0,
                    "Date PrintFault arguments are incomplete.");
                invocation.DatePrintFaultCalls++;
                return 1;
            });
        Register(baseAddress, DosLvo.IoErr, "IoErr",
            (_, invocation) => unchecked((uint)invocation.IoError));
        Register(baseAddress, DosLvo.SetIoErr, "SetIoErr", (state, invocation) =>
        {
            invocation.IoError = unchecked((int)state.D[1]);
            return 0;
        });
    }

    private void WriteArgument(uint address, string value)
    {
        var bytes = Encoding.Latin1.GetBytes(value);
        bytes.CopyTo(Bus.Memory.AsSpan((int)address));
        Bus.Memory[(int)address + bytes.Length] = 0;
    }

    private void WriteDateOutput(Invocation invocation, uint arguments)
    {
        Require(Bus.CString(Bus.Long(arguments)) == "Mon" &&
            Bus.CString(Bus.Long(arguments + 4)) == "01-Jan-90" &&
            Bus.CString(Bus.Long(arguments + 8)) == "12:34:56",
            "Date output arguments differ.");
        invocation.Output.Write(Encoding.Latin1.GetBytes(
            "Mon 01-Jan-90 12:34:56\n"));
    }
}
