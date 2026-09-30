using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 <c>Date</c> command body.  The command keeps the classic
/// ReadArgs grammar and uses DOS date conversion plus timer.device for the
/// set path.  All temporary records and strings are guest allocations owned
/// by the invocation; no command state is retained between calls.
/// </summary>
public static class NativeWorkbench31DateCommand
{
    private const uint DateTimeBytes = DosDateTime.Size;
    private const uint StringBytes = DosDateTime.StringLength;
    private const uint FormatArgumentsBytes = 12;
    private const uint TimerRequestBytes = 40;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead("DAY,DATE,TIME,TO=VER/K", 4,
                out var arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        APTR dateTime = APTR.Null;
        APTR day = APTR.Null;
        APTR date = APTR.Null;
        APTR time = APTR.Null;
        APTR formatArguments = APTR.Null;
        APTR timerRequest = APTR.Null;
        APTR utility = APTR.Null;
        BPTR output = BPTR.Null;
        var timerOpened = false;
        var result = DOS.RETURN_OK;
        var setterArguments = false;

        dateTime = Exec.AllocMem(DateTimeBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        day = Exec.AllocMem(StringBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        date = Exec.AllocMem(StringBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        time = Exec.AllocMem(StringBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        formatArguments = Exec.AllocMem(FormatArgumentsBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (dateTime.IsNull || day.IsNull || date.IsNull || time.IsNull ||
            formatArguments.IsNull)
        {
            result = DOS.RETURN_FAIL;
            ioError = (int)DOS.Error.NoFreeStore;
            goto Cleanup;
        }

        if (!arguments.TryGetResult(0, out var dayArgument) ||
            !arguments.TryGetResult(1, out var dateArgument) ||
            !arguments.TryGetResult(2, out var timeArgument) ||
            !arguments.TryGetResult(3, out var outputArgument))
        {
            result = DOS.RETURN_FAIL;
            ioError = (int)DOS.Error.BadTemplate;
            goto Cleanup;
        }

        // DateStamp supplies the current value for fields that are omitted
        // by StrToDate.  This is also the display source when no setters are
        // supplied.
        if (DOS.DateStamp(dateTime) == 0)
        {
            result = DOS.RETURN_FAIL;
            ioError = (int)DOS.IoErr();
            goto Cleanup;
        }

        setterArguments = dayArgument != 0 || dateArgument != 0 ||
            timeArgument != 0;
        if (setterArguments)
        {
            // UMult32/UDivMod32 are utility.library V36 date/math vectors.
            utility = Exec.OpenLibraryRaw(Utility.Name, 36);
            if (utility.IsNull)
            {
                result = DOS.RETURN_FAIL;
                ioError = (int)DOS.Error.NotImplemented;
                goto Cleanup;
            }
            Utility.UtilityLibraryBase = utility;

            APTR.WriteUInt8(dateTime, DosLayout.DateTime.Format,
                (byte)DosDateFormat.Dos);
            APTR.WriteUInt8(dateTime, DosLayout.DateTime.Flags,
                (byte)DosDateTimeFlags.Future);

            // The original v40 body accepts each supplied token as either a
            // date or a time.  Keeping the two attempts in this order also
            // preserves the useful DATE-versus-TIME ambiguity resolution.
            for (var index = 0; index < 3; index++)
            {
                var argument = index == 0 ? dayArgument :
                    index == 1 ? dateArgument : timeArgument;
                if (argument == 0) continue;

                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date,
                    argument);
                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, 0);
                if (DOS.StrToDate(dateTime) != 0)
                {
                    if (!AcceptParserResult(ref ioError, ref result))
                        goto Cleanup;
                    continue;
                }

                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, 0);
                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time,
                    argument);
                if (DOS.StrToDate(dateTime) == 0)
                {
                    ioError = (int)DOS.IoErr();
                    if (ioError == 0) ioError = (int)DOS.Error.BadTemplate;
                    result = DOS.RETURN_FAIL;
                    goto Cleanup;
                }
                if (!AcceptParserResult(ref ioError, ref result))
                    goto Cleanup;
            }

            timerRequest = Exec.AllocMem(TimerRequestBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (timerRequest.IsNull)
            {
                result = DOS.RETURN_FAIL;
                ioError = (int)DOS.Error.NoFreeStore;
                goto Cleanup;
            }

            APTR.WriteUInt16(timerRequest, ExecLayout.IORequest.Command,
                11);
            var seconds = Utility.UMult32(
                APTR.ReadUInt32(dateTime, DosLayout.DateStamp.Days), 86400);
            seconds += Utility.UMult32(
                APTR.ReadUInt32(dateTime, DosLayout.DateStamp.Minutes), 60);
            seconds += Utility.UDivMod32(
                APTR.ReadUInt32(dateTime, DosLayout.DateStamp.Ticks), 50);
            APTR.WriteUInt32(timerRequest, TimerDeviceLayout.TimerRequest.Seconds,
                seconds);
            APTR.WriteUInt32(timerRequest,
                TimerDeviceLayout.TimerRequest.Microseconds, 0);

            if (Exec.OpenDevice(TimerDevice.Name, 0, timerRequest, 0) != 0)
            {
                result = DOS.RETURN_FAIL;
                ioError = (int)DOS.Error.NotImplemented;
                goto Cleanup;
            }
            timerOpened = true;
            Exec.DoIO(timerRequest);
            if (APTR.ReadUInt8(timerRequest, ExecLayout.IORequest.Error) != 0)
            {
                result = DOS.RETURN_FAIL;
                ioError = (int)DOS.Error.NotImplemented;
            }
            Exec.CloseDevice(timerRequest);
            timerOpened = false;
            goto FormatOutput;
        }

    FormatOutput:
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Format,
            (byte)DosDateFormat.Dos);
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Flags, 0);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Day, day.Raw);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, date.Raw);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, time.Raw);
        if (DOS.DateToStr(dateTime) == 0)
        {
            result = DOS.RETURN_FAIL;
            ioError = (int)DOS.IoErr();
            goto Cleanup;
        }

        APTR.WriteUInt32(formatArguments, 0,
            APTR.ReadUInt32(dateTime, DosLayout.DateTime.Day));
        APTR.WriteUInt32(formatArguments, 4,
            APTR.ReadUInt32(dateTime, DosLayout.DateTime.Date));
        APTR.WriteUInt32(formatArguments, 8,
            APTR.ReadUInt32(dateTime, DosLayout.DateTime.Time));

        if (outputArgument != 0)
        {
            output = DOS.OpenRaw(CString.FromPointer(outputArgument),
                DOS.FileMode.NewFile);
            if (output.IsNull)
            {
                ioError = (int)DOS.IoErr();
                if (ioError == 0) ioError = (int)DOS.Error.ObjectNotFound;
                DOS.PrintFault((DOS.Error)ioError,
                    CString.FromPointer(outputArgument));
                result = setterArguments ? DOS.RETURN_WARN : DOS.RETURN_FAIL;
                goto Cleanup;
            }

            _ = DOS.VFPrintf(output, "%s %s %s\n", formatArguments);
            _ = DOS.Close(output);
            output = BPTR.Null;
        }
        else if (result == DOS.RETURN_OK)
        {
            _ = DOS.VPrintf("%s %s %s\n", formatArguments);
        }

    Cleanup:
        if (output.IsNotNull) _ = DOS.Close(output);
        if (timerOpened && timerRequest.IsNotNull)
            Exec.CloseDevice(timerRequest);
        if (timerRequest.IsNotNull)
            Exec.FreeMem(timerRequest, TimerRequestBytes);
        if (formatArguments.IsNotNull)
            Exec.FreeMem(formatArguments, FormatArgumentsBytes);
        if (time.IsNotNull) Exec.FreeMem(time, StringBytes);
        if (date.IsNotNull) Exec.FreeMem(date, StringBytes);
        if (day.IsNotNull) Exec.FreeMem(day, StringBytes);
        if (dateTime.IsNotNull) Exec.FreeMem(dateTime, DateTimeBytes);
        arguments.Release();
        if (utility.IsNotNull) Exec.CloseLibrary(utility);
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool AcceptParserResult(ref int ioError, ref int result)
    {
        ioError = (int)DOS.IoErr();
        if (ioError == 0) return true;
        result = DOS.RETURN_FAIL;
        return false;
    }
}
