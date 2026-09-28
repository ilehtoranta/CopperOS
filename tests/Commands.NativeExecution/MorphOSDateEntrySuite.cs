using System.Text;
using Amiga;
using static CopperOS.Commands.NativeExecution.CommandTestBus;

namespace CopperOS.Commands.NativeExecution;

internal sealed class MorphOSDateNativeLayout(uint day, uint date, uint time,
    uint to, uint lformat)
{
    public uint Day { get; } = day;
    public uint Date { get; } = date;
    public uint Time { get; } = time;
    public uint To { get; } = to;
    public uint LFormat { get; } = lformat;
}

internal sealed partial class ProbeFixture
{
    public const string MorphOSDateEntrySuite =
        "morphos320-date-native-entry-vector-fixture";

    private List<object> RunMorphOSDateEntryCases()
    {
        ProbeCase[] cases =
        [
            new("display", "", DOS.RETURN_OK, 0,
                "Mon 01-Jan-90 12:34:56\n")
            { MorphOSDate = new(false, false) },
            new("display-to", "TO=RAM:date.out\n", DOS.RETURN_OK, 0,
                "Mon 01-Jan-90 12:34:56\n")
            { MorphOSDate = new(false, true) },
            new("display-lformat", "LFORMAT=%a\n", DOS.RETURN_OK, 0,
                "Locale date\n")
            { MorphOSDate = new(false, false, LFormat: true) },
            new("set-date", "DATE=02-Jan-91\n", DOS.RETURN_OK, 0, "")
            { MorphOSDate = new(true, false) },
            new("set-date-ver", "DATE=02-Jan-91 TO=RAM:date.out\n",
                DOS.RETURN_OK, 0, "Mon 01-Jan-90 12:34:56\n")
            { MorphOSDate = new(true, true) },
            new("set-time-short", "TIME=12:34\n", DOS.RETURN_OK, 0, "")
            { MorphOSDate = new(true, false) },
            new("parser-failure", "DATE=bad\n", DOS.RETURN_FAIL, 116, "")
            { MorphOSDate = new(false, false, ParserError: 116) },
            new("timer-open-failure", "DATE=02-Jan-91\n",
                DOS.RETURN_FAIL, (int)DOS.Error.NotImplemented, "")
            { MorphOSDate = new(true, false, TimerOpenFailure: true) },
            new("timer-io-failure", "DATE=02-Jan-91\n",
                DOS.RETURN_FAIL, (int)DOS.Error.NotImplemented, "")
            { MorphOSDate = new(true, false, TimerIoFailure: true) },
            new("locale-open-failure", "LFORMAT=%a\n", DOS.RETURN_OK,
                0, "Locale date\n")
            { MorphOSDate = new(false, false, LFormat: true,
                LocaleOpenFailure: true) },
            new("write-failure", "TO=RAM:date.out\n", DOS.RETURN_FAIL,
                205, "")
            { MorphOSDate = new(false, true, WriteFailure: true) },
        ];

        var reports = new List<object>();
        foreach (var test in cases) reports.AddRange(Execute([test], false));
        reports.AddRange(Execute([
            cases[0] with { Name = "interleaved-display" },
            cases[2] with { Name = "interleaved-lformat" }
        ], true));
        Bus.AssertImageUnchanged();
        return reports;
    }

    private void PrepareMorphOSDateEntry(Invocation invocation)
    {
        var definition = invocation.Definition.MorphOSDate ??
            throw new InvalidOperationException("Missing MorphOS Date definition.");
        var layout = new MorphOSDateNativeLayout(
            invocation.Arguments + 0x400,
            invocation.Arguments + 0x500,
            invocation.Arguments + 0x600,
            invocation.Arguments + 0x700,
            invocation.Arguments + 0x800);
        invocation.MorphOSDateLayout = layout;
        if (definition.Setter)
            WriteArgument(invocation.Definition.Arguments.Contains("TIME=") ? layout.Time : layout.Date,
                invocation.Definition.Arguments.Contains("TIME=") ? "12:34" : "02-Jan-91");
        if (definition.To) WriteArgument(layout.To, "RAM:date.out");
        if (definition.LFormat) WriteArgument(layout.LFormat, "%a");
    }

    private void VerifyMorphOSDateEntry(Invocation invocation)
    {
        var definition = invocation.Definition.MorphOSDate ??
            throw new InvalidOperationException("Missing MorphOS Date definition.");
        var parserFailure = definition.ParserError != 0;
        var setter = definition.Setter && !parserFailure;
        var prints = !parserFailure && (!setter || definition.To);
        var lformat = prints && definition.LFormat;
        var outputFile = prints && definition.To;
        Require(invocation.Reads == 1 &&
            invocation.FreeArgs == (parserFailure ? 0 : 1),
            "MorphOS Date parser ownership differs.");
        Require(invocation.Allocations == (parserFailure ? 1 : 2) &&
            invocation.FreeMem == invocation.Allocations,
            "MorphOS Date workspace cleanup differs.");
        var expectedDateStamp = parserFailure ? 0 : setter
            ? definition.TimerOpenFailure ? 0 : definition.To ? 2 : 1
            : 1;
        Require(invocation.DateStampCalls == expectedDateStamp,
            $"MorphOS Date DateStamp count differs (actual={invocation.DateStampCalls}, expected={expectedDateStamp}, reads={invocation.Reads}, alloc={invocation.Allocations}, freeArgs={invocation.FreeArgs}, puts={invocation.DatePutStrCalls}, faults={invocation.DatePrintFaultCalls}, events={string.Join(',', invocation.Events)}).");
        var expectedStrToDate = setter && !definition.TimerOpenFailure ? 1 : 0;
        Require(invocation.StrToDateCalls == expectedStrToDate,
            $"MorphOS Date StrToDate count differs (actual={invocation.StrToDateCalls}, expected={expectedStrToDate}).");
        Require(invocation.DateMsgPortCreates == (setter ? 1 : 0) &&
            invocation.DateMsgPortDeletes == (setter ? 1 : 0) &&
            invocation.DateIoRequestCreates == (setter ? 1 : 0) &&
            invocation.DateIoRequestDeletes == (setter ? 1 : 0),
            "MorphOS Date timer resource lifetime differs.");
        Require(invocation.TimerOpenCalls == (setter ? 1 : 0) &&
            invocation.TimerCloseCalls == (setter && !definition.TimerOpenFailure ? 1 : 0) &&
            invocation.TimerDoIoCalls == (setter && !definition.TimerOpenFailure ? 1 : 0),
            "MorphOS Date timer lifecycle differs.");
        Require(invocation.DateFileOpens == (outputFile ? 1 : 0) &&
            invocation.DateFileCloses == (outputFile ? 1 : 0),
            "MorphOS Date TO lifecycle differs.");
        var expectedDateToStr = prints && !lformat ? 1 : 0;
        var expectedDateWrite = prints && !lformat ? 1 : 0;
        var expectedLocaleOpen = lformat ? 1 : 0;
        var expectedLocaleClose = lformat ? 1 : 0;
        var expectedOpenLocale = lformat ? 1 : 0;
        var expectedFormatDate = lformat ? 1 : 0;
        Require(invocation.DateToStrCalls == expectedDateToStr &&
            invocation.DateWriteCalls == (prints && !lformat ? 1 : 0) &&
            invocation.LocaleOpens == expectedLocaleOpen &&
            invocation.LocaleCloses == expectedLocaleClose &&
            invocation.LocaleOpenLocaleCalls == expectedOpenLocale &&
            invocation.LocaleFormatDateCalls == expectedFormatDate,
            $"MorphOS Date output path differs for {invocation.Definition.Name} (dateToStr={invocation.DateToStrCalls}/{expectedDateToStr}, write={invocation.DateWriteCalls}/{expectedDateWrite}, localeOpen={invocation.LocaleOpens}/{expectedLocaleOpen}, localeClose={invocation.LocaleCloses}/{expectedLocaleClose}, openLocale={invocation.LocaleOpenLocaleCalls}/{expectedOpenLocale}, formatDate={invocation.LocaleFormatDateCalls}/{expectedFormatDate}, events={string.Join(',', invocation.Events)}).");
        var expectedFault = parserFailure || definition.TimerOpenFailure ||
            definition.TimerIoFailure || definition.WriteFailure;
        Require(invocation.DatePrintFaultCalls == (expectedFault &&
            (parserFailure || definition.WriteFailure) ? 1 : 0),
            "MorphOS Date PrintFault path differs.");
        Require(invocation.DatePutStrCalls >= (setter &&
            (definition.TimerOpenFailure || definition.TimerIoFailure) ? 1 : 0),
            "MorphOS Date diagnostic output differs.");
        invocation.MorphOSDateLayout = null;
    }

    private void RegisterMorphOSDateEntryExec()
    {
        Register(ExecBase, ExecLvo.CreateMsgPort, "CreateMsgPort",
            (state, invocation) =>
            {
                Require(invocation.Definition.MorphOSDate is not null,
                    "Date created a message port outside its suite.");
                invocation.DateMsgPortCreates++;
                return Bus.Allocate(invocation, MsgPort.Size, "DateMsgPort", true);
            });
        Register(ExecBase, ExecLvo.DeleteMsgPort, "DeleteMsgPort",
            (state, invocation) =>
            {
                Bus.Release(invocation, state.A[0], "DateMsgPort");
                invocation.DateMsgPortDeletes++;
                return 0;
            });
        Register(ExecBase, ExecLvo.CreateIORequest, "CreateIORequest",
            (state, invocation) =>
            {
                Require(state.A[0] != 0 && state.D[0] == TimerRequest.Size,
                    "Date CreateIORequest ABI differs.");
                invocation.DateIoRequestCreates++;
                return Bus.Allocate(invocation, TimerRequest.Size,
                    "DateTimerRequest", true);
            });
        Register(ExecBase, ExecLvo.DeleteIORequest, "DeleteIORequest",
            (state, invocation) =>
            {
                Bus.Release(invocation, state.A[0], "DateTimerRequest");
                invocation.DateIoRequestDeletes++;
                return 0;
            });
        Register(ExecBase, ExecLvo.OpenDevice, "OpenDevice",
            (state, invocation) =>
            {
                var definition = invocation.Definition.MorphOSDate!;
                Require(Bus.CString(state.A[0]) == TimerDevice.Name &&
                    state.D[0] == (uint)TimerUnit.VBlank && state.D[1] == 0 &&
                    state.A[1] != 0, "MorphOS Date OpenDevice ABI differs.");
                invocation.TimerOpenCalls++;
                return definition.TimerOpenFailure ? 1u : 0u;
            });
        Register(ExecBase, ExecLvo.DoIO, "DoIO", (state, invocation) =>
        {
            var definition = invocation.Definition.MorphOSDate!;
            Require(state.A[1] != 0 &&
                Bus.Word(state.A[1] + (uint)ExecLayout.IORequest.Command) ==
                (ushort)TimerCommand.SetSystemTime &&
                (Bus.Memory[state.A[1] + (uint)ExecLayout.IORequest.Flags] &
                    (byte)IOFlags.Quick) != 0,
                "MorphOS Date timer request differs.");
            invocation.TimerDoIoCalls++;
            Bus.Memory[state.A[1] + (uint)ExecLayout.IORequest.Error] =
                definition.TimerIoFailure ? (byte)1 : (byte)0;
            return 0;
        });
        Register(ExecBase, ExecLvo.CloseDevice, "CloseDevice",
            (state, invocation) =>
            {
                Require(state.A[1] != 0, "Date closed a null timer request.");
                invocation.TimerCloseCalls++;
                return 0;
            });
    }

    private void RegisterMorphOSDateDos(uint baseAddress)
    {
        Register(baseAddress, DosLvo.ReadArgs, "ReadArgs", (state, invocation) =>
        {
            var definition = invocation.Definition.MorphOSDate!;
            var layout = invocation.MorphOSDateLayout!;
            Require(Bus.CString(state.D[1]) ==
                "DAY,DATE,TIME,TO=VER/K,LFORMAT/K" && state.D[3] == 0 &&
                Bus.OwnedAllocation(invocation, state.D[2], "Exec").Size == 20,
                "MorphOS Date template/result ABI differs.");
            for (var offset = 0u; offset < 20; offset += 4)
                Require(Bus.Long(state.D[2] + offset) == 0,
                    "MorphOS Date result slots were not cleared.");
            invocation.Reads++;
            if (definition.ParserError != 0)
            {
                invocation.IoError = definition.ParserError;
                return 0;
            }
            if (definition.Setter)
                Bus.Long(state.D[2] + (invocation.Definition.Arguments.Contains("TIME=") ? 8u : 4u),
                    invocation.Definition.Arguments.Contains("TIME=") ? layout.Time : layout.Date);
            if (definition.To) Bus.Long(state.D[2] + 12, layout.To);
            if (definition.LFormat) Bus.Long(state.D[2] + 16, layout.LFormat);
            return Bus.Allocate(invocation, 32, "DateRDArgs", true);
        });
        Register(baseAddress, DosLvo.FreeArgs, "FreeArgs", (state, invocation) =>
        {
            Bus.Release(invocation, state.D[1], "DateRDArgs");
            invocation.FreeArgs++;
            return 0;
        });
        Register(baseAddress, DosLvo.DateStamp, "DateStamp",
            (state, invocation) =>
            {
                Require(state.D[1] != 0, "MorphOS Date DateStamp pointer is null.");
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Days, 1);
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Minutes, 2);
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Ticks, 3);
                invocation.DateStampCalls++;
                invocation.IoError = 0;
                return 1;
            });
        Register(baseAddress, DosLvo.StrToDate, "StrToDate",
            (state, invocation) =>
            {
                invocation.StrToDateCalls++;
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Days, 4);
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Minutes, 5);
                Bus.Long(state.D[1] + (uint)DosLayout.DateStamp.Ticks, 6);
                invocation.IoError = 0;
                return 1;
            });
        Register(baseAddress, DosLvo.DateToStr, "DateToStr",
            (state, invocation) =>
            {
                var dateTime = state.D[1];
                WriteArgument(Bus.Long(dateTime + (uint)DosLayout.DateTime.Day), "Mon");
                WriteArgument(Bus.Long(dateTime + (uint)DosLayout.DateTime.Date), "01-Jan-90");
                WriteArgument(Bus.Long(dateTime + (uint)DosLayout.DateTime.Time), "12:34:56");
                invocation.DateToStrCalls++;
                return 1;
            });
        Register(baseAddress, DosLvo.Output, "Output",
            (_, invocation) => invocation.OutputBptr);
        Register(baseAddress, DosLvo.Open, "Open", (state, invocation) =>
        {
            Require(Bus.CString(state.D[1]) == "RAM:date.out" &&
                state.D[2] == (uint)DOS.FileMode.NewFile,
                "MorphOS Date TO open ABI differs.");
            invocation.DateFileOpens++;
            return invocation.OutputBptr;
        });
        Register(baseAddress, DosLvo.Close, "Close", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "MorphOS Date closed an unexpected output.");
            invocation.DateFileCloses++;
            return 1;
        });
        Register(baseAddress, DosLvo.Write, "Write", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr && state.D[3] > 0,
                "MorphOS Date Write ABI differs.");
            invocation.DateWriteCalls++;
            if (invocation.Definition.MorphOSDate!.WriteFailure)
            {
                invocation.IoError = 205;
                return 2;
            }
            invocation.Output.Write(Bus.Memory,
                checked((int)state.D[2]), checked((int)state.D[3]));
            return state.D[3];
        });
        Register(baseAddress, DosLvo.FPutC, "FPutC", (state, invocation) =>
        {
            Require(state.D[1] == invocation.OutputBptr,
                "MorphOS Date FPutC output differs.");
            if (state.D[2] == '\n') invocation.Output.Write(Encoding.Latin1.GetBytes("\n"));
            return state.D[2];
        });
        Register(baseAddress, DosLvo.PutStr, "PutStr", (state, invocation) =>
        {
            invocation.DatePutStrCalls++;
            return 0;
        });
        Register(baseAddress, DosLvo.PrintFault, "PrintFault",
            (state, invocation) =>
            {
                Require(state.D[2] != 0, "MorphOS Date PrintFault header is null.");
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
        if (baseAddress != 0x8000) return;
        Register(DateLocaleBase, -156, "OpenLocale", (state, invocation) =>
        {
            invocation.LocaleOpenLocaleCalls++;
            return invocation.Definition.MorphOSDate!.LocaleOpenFailure
                ? 0u : 0xcafeu;
        });
        Register(DateLocaleBase, -42, "CloseLocale", (state, invocation) =>
        {
            invocation.LocaleCloseLocaleCalls++;
            return 0;
        });
        Register(DateLocaleBase, -60, "FormatDate", (state, invocation) =>
        {
            Require(state.A[1] != 0 && state.A[2] != 0 && state.A[3] != 0,
                "MorphOS Date FormatDate ABI differs.");
            invocation.LocaleFormatDateCalls++;
            invocation.Output.Write(Encoding.Latin1.GetBytes("Locale date"));
            return 0;
        });
    }
}
