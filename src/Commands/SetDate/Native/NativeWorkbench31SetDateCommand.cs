using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 <c>SetDate</c> command body.  It retains the classic
/// ReadArgs grammar, parses the supplied date/time tokens through DOS, and
/// applies the resulting DOS DateTime to each MatchFirst/MatchNext result.
/// </summary>
public static class NativeWorkbench31SetDateCommand
{
    private const uint AnchorBytes = DosLayout.AnchorPath.Size;
    private const uint DateTimeBytes = DosDateTime.Size;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(
                "FILE/A,WEEKDAY,DATE,TIME,ALL/S", 5,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "SetDate");
            return arguments.ReturnLevel;
        }

        APTR anchor = APTR.Null;
        APTR dateTime = APTR.Null;
        var result = DOS.RETURN_OK;
        var error = 0;
        var searchStarted = false;

        anchor = Exec.AllocVec(AnchorBytes, 0x10001);
        dateTime = Exec.AllocMem(DateTimeBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (anchor.IsNull || dateTime.IsNull)
        {
            result = DOS.RETURN_FAIL;
            error = (int)DOS.Error.NoFreeStore;
            goto Cleanup;
        }

        if (!arguments.TryGetResult(0, out var file) || file == 0 ||
            !arguments.TryGetResult(1, out var weekday) ||
            !arguments.TryGetResult(2, out var date) ||
            !arguments.TryGetResult(3, out var time) ||
            !arguments.TryGetResult(4, out var all))
        {
            result = DOS.RETURN_FAIL;
            error = (int)DOS.Error.BadTemplate;
            goto Cleanup;
        }

        if (DOS.DateStamp(dateTime) == 0)
        {
            result = DOS.RETURN_FAIL;
            error = (int)DOS.IoErr();
            goto Cleanup;
        }
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Format,
            (byte)DosDateFormat.Dos);
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Flags,
            (byte)DosDateTimeFlags.Future);

        // The v37 command tries each supplied field as a date first and as a
        // time second. DOS retains omitted portions of the current stamp.
        for (var index = 0; index < 3; index++)
        {
            var value = index == 0 ? weekday : index == 1 ? date : time;
            if (value == 0) continue;
            APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, value);
            APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, 0);
            if (DOS.StrToDate(dateTime) == 0)
            {
                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, 0);
                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, value);
                if (DOS.StrToDate(dateTime) == 0)
                {
                    error = (int)DOS.IoErr();
                    if (error == 0) error = (int)DOS.Error.BadTemplate;
                    result = DOS.RETURN_FAIL;
                    goto Cleanup;
                }
            }
        }

        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, 1);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, 0x1000);
        searchStarted = true;
        DOS.MatchFirst(CString.FromPointer(file), anchor);
        error = (int)DOS.IoErr();
        while (error == 0)
        {
            var stringLength = APTR.ReadUInt16(anchor,
                DosLayout.AnchorPath.StringLength);
            if (stringLength > 0 && all != 0)
            {
                var flags = APTR.ReadUInt8(anchor, DosLayout.AnchorPath.Flags);
                if ((flags & (byte)AnchorPathFlags.DidDirectory) == 0)
                    flags |= (byte)AnchorPathFlags.DoDirectory;
                flags &= unchecked((byte)~(byte)AnchorPathFlags.DidDirectory);
                APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, flags);
            }

            var current = APTR.FromPointer(APTR.ReadUInt32(anchor,
                DosLayout.AnchorPath.Current));
            var lockValue = current.IsNull
                ? BPTR.Null
                : BPTR.FromRaw(APTR.ReadUInt32(current,
                    DosLayout.AChain.Lock));
            if (lockValue.IsNull)
            {
                error = (int)DOS.Error.ObjectNotFound;
                result = DOS.RETURN_FAIL;
                goto Cleanup;
            }

            var duplicate = DOS.DupLock(lockValue);
            var duplicateValue = duplicate.GetValueOrDefault();
            var previous = DOS.CurrentDirRaw(duplicateValue);
            var name = CString.FromPointer(anchor.Raw +
                (uint)DosLayout.AnchorPath.Info +
                (uint)FileInfoBlock.FileNameOffset);
            var applied = DOS.SetFileDate(name, dateTime);
            DOS.CurrentDirRaw(previous);
            DOS.UnLock(duplicateValue);
            if (applied == 0)
            {
                error = (int)DOS.IoErr();
                result = error == (int)DOS.Error.Break
                    ? DOS.RETURN_WARN : DOS.RETURN_FAIL;
                goto Cleanup;
            }
            DOS.MatchNext(anchor);
            error = (int)DOS.IoErr();
        }

        // MatchFirst/MatchNext report a nonzero termination error.  Preserve
        // the classic no-more success and break warning while treating other
        // failures as ERROR.
        if (error != 0)
        {
            if (error == (int)DOS.Error.NoMoreEntries)
            {
                error = 0;
                result = DOS.RETURN_OK;
            }
            else
            {
                result = error == (int)DOS.Error.Break
                ? DOS.RETURN_WARN : DOS.RETURN_FAIL;
            }
        }

    Cleanup:
        if (searchStarted) DOS.MatchEnd(anchor);
        if (anchor.IsNotNull) Exec.FreeVec(anchor);
        arguments.Release();
        if (dateTime.IsNotNull) Exec.FreeMem(dateTime, DateTimeBytes);
        ioError = error;
        if (ioError != 0) DOS.PrintFault((DOS.Error)ioError, "SetDate");
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }
}
