using Amiga;
using CopperSharp.Compiler;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 Date command body based on the released 50.7 source
/// contract.  The source keeps its DateTime, timer request, message port,
/// locale hook, and formatting buffers on the invocation stack.  This native
/// entry keeps that ownership local while using public Exec storage for the
/// records that must survive DOS/locale calls.
/// </summary>
public static class NativeMorphOSDateCommand
{
    private const uint WorkspaceBytes = 192;
    private const uint TimerRequestBytes = TimerRequest.Size;

    private const int DateTimeOffset = 0;
    private const int HookOffset = 32;
    private const int FullTimeOffset = 64;
    private const int DayStringOffset = 80;
    private const int DateStringOffset = 96;
    private const int TimeStringOffset = 112;
    private const int ResultStringOffset = 128;

    private const uint BadArgs = 1;
    private const uint OutOfRange = 2;
    private const uint TimerFailure = 3;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(
                "DAY,DATE,TIME,TO=VER/K,LFORMAT/K", 5,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "Date");
            return DOS.RETURN_FAIL;
        }

        APTR workspace = APTR.Null;
        APTR timerPort = APTR.Null;
        APTR timerRequest = APTR.Null;
        APTR localeLibrary = APTR.Null;
        uint locale = 0;
        BPTR output = BPTR.Null;
        var ownOutput = false;
        var timerOpened = false;
        var result = DOS.RETURN_OK;
        var error = 0;

        do
        {
            if (!arguments.TryGetResult(0, out var day) ||
                !arguments.TryGetResult(1, out var date) ||
                !arguments.TryGetResult(2, out var time) ||
                !arguments.TryGetResult(3, out var to) ||
                !arguments.TryGetResult(4, out var lformat))
            {
                error = (int)DOS.Error.BadTemplate;
                result = DOS.RETURN_FAIL;
                break;
            }

            workspace = Exec.AllocMem(WorkspaceBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (workspace.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }

            var dateTime = APTR.FromPointer(workspace.Raw + DateTimeOffset);
            var fullTime = APTR.FromPointer(workspace.Raw + FullTimeOffset);
            var setter = day != 0 || date != 0 || time != 0;
            if (setter)
            {
                result = SetDate(day, date, time, dateTime, fullTime,
                    out error, ref timerPort, ref timerRequest,
                    ref timerOpened);
                if (timerOpened && timerRequest.IsNotNull)
                {
                    Exec.CloseDevice(timerRequest);
                    timerOpened = false;
                }
                if (timerRequest.IsNotNull)
                {
                    Exec.DeleteIORequest(timerRequest);
                    timerRequest = APTR.Null;
                }
                if (timerPort.IsNotNull)
                {
                    Exec.DeleteMsgPort(timerPort);
                    timerPort = APTR.Null;
                }
                if (result != DOS.RETURN_OK || to == 0)
                    break;
            }

            result = PrintDate(to, lformat, dateTime, workspace,
                ref output, ref ownOutput, ref localeLibrary, ref locale,
                out error);
        }
        while (false);

        if (timerOpened && timerRequest.IsNotNull)
            Exec.CloseDevice(timerRequest);
        if (timerRequest.IsNotNull)
            Exec.DeleteIORequest(timerRequest);
        if (timerPort.IsNotNull)
            Exec.DeleteMsgPort(timerPort);
        if (localeLibrary.IsNotNull)
            Locale.CloseLocale(locale);
        if (localeLibrary.IsNotNull)
            Exec.CloseLibrary(localeLibrary);
        if (ownOutput && output.IsNotNull)
            DOS.Close(output);
        if (workspace.IsNotNull)
            Exec.FreeMem(workspace, WorkspaceBytes);
        arguments.Release();
        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static int SetDate(uint day, uint date, uint time, APTR dateTime,
        APTR fullTime, out int error, ref APTR timerPort,
        ref APTR timerRequest, ref bool timerOpened)
    {
        error = 0;
        APTR realDate = APTR.Null;
        APTR realTime = APTR.Null;
        for (var index = 0; index < 3; index++)
        {
            var value = index == 0 ? day : index == 1 ? date : time;
            if (value == 0) continue;
            var source = APTR.FromPointer(value);
            if (CountChar(source, (byte)'-') != 0)
            {
                realDate = source;
            }
            else
            {
                var colons = CountChar(source, (byte)':');
                if (colons != 0)
                {
                    if (colons == 1 && CStringLength(source) <= 5)
                    {
                        CopyCString(source, fullTime, 9);
                        var length = CStringLength(fullTime);
                        APTR.WriteUInt8(fullTime, unchecked((int)length), (byte)':');
                        APTR.WriteUInt8(fullTime, unchecked((int)(length + 1)), (byte)'0');
                        APTR.WriteUInt8(fullTime, unchecked((int)(length + 2)), (byte)'0');
                        APTR.WriteUInt8(fullTime, unchecked((int)(length + 3)), 0);
                        realTime = fullTime;
                    }
                    else
                    {
                        realTime = source;
                    }
                }
                else if (realDate.IsNull)
                {
                    realDate = source;
                }
            }
        }

        timerPort = Exec.CreateMsgPort();
        if (timerPort.IsNull)
        {
            DOS.PutStr("Date: Error creating MsgPort\n");
            error = (int)DOS.Error.NoFreeStore;
            return DOS.RETURN_FAIL;
        }

        timerRequest = Exec.CreateIORequest(timerPort, TimerRequestBytes);
        if (timerRequest.IsNull)
        {
            DOS.PutStr("Date: Error creating timerequest\n");
            error = (int)DOS.Error.NoFreeStore;
            return DOS.RETURN_FAIL;
        }

        if (Exec.OpenDevice(TimerDevice.Name, (uint)TimerUnit.VBlank,
                timerRequest, 0) != 0)
        {
            DOS.PutStr("Date: Error opening timer.device\n");
            error = (int)DOS.Error.NotImplemented;
            return DOS.RETURN_FAIL;
        }
        timerOpened = true;

        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Format,
            (byte)DosDateFormat.Dos);
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Flags,
            (byte)DosDateTimeFlags.Future);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Day, 0);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, realDate.Raw);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, realTime.Raw);
        DOS.DateStamp(dateTime);

        if ((realDate.IsNull && realTime.IsNull) || DOS.StrToDate(dateTime) == 0)
        {
            DOS.PutStr("***Bad args:\n- use DD-MMM-YY or <dayname> or yesterday etc. to set date\n      HH:MM:SS or HH:MM to set time\n");
            error = (int)DOS.Error.BadTemplate;
            return DOS.RETURN_FAIL;
        }

        var days = unchecked((int)APTR.ReadUInt32(dateTime,
            DosLayout.DateStamp.Days));
        var minutes = unchecked((int)APTR.ReadUInt32(dateTime,
            DosLayout.DateStamp.Minutes));
        var ticks = unchecked((int)APTR.ReadUInt32(dateTime,
            DosLayout.DateStamp.Ticks));
        if (days < 0 || minutes < 0 || ticks < 0 ||
            !MultiplyChecked((uint)days, 86400, out var seconds) ||
            !MultiplyChecked((uint)minutes, 60, out var minuteSeconds) ||
            !AddChecked(seconds, minuteSeconds, out seconds) ||
            !AddChecked(seconds, (uint)(ticks / 50), out seconds))
        {
            DOS.PutStr("Date: The desired system time is outside of valid range\n");
            error = (int)DOS.Error.BadTemplate;
            return DOS.RETURN_FAIL;
        }

        APTR.WriteUInt32(timerRequest, TimerDeviceLayout.TimerRequest.Seconds,
            seconds);
        APTR.WriteUInt32(timerRequest,
            TimerDeviceLayout.TimerRequest.Microseconds, 0);
        APTR.WriteUInt16(timerRequest, ExecLayout.IORequest.Command,
            (ushort)TimerCommand.SetSystemTime);
        var flags = APTR.ReadUInt8(timerRequest, ExecLayout.IORequest.Flags);
        APTR.WriteUInt8(timerRequest, ExecLayout.IORequest.Flags,
            (byte)(flags | (byte)IOFlags.Quick));
        Exec.DoIO(timerRequest);
        if (APTR.ReadUInt8(timerRequest, ExecLayout.IORequest.Error) != 0)
        {
            DOS.PutStr("Date: Unable to set system time\n");
            error = (int)DOS.Error.NotImplemented;
            return DOS.RETURN_FAIL;
        }
        return DOS.RETURN_OK;
    }

    private static int PrintDate(uint to, uint lformat, APTR dateTime,
        APTR workspace, ref BPTR output, ref bool ownOutput,
        ref APTR localeLibrary, ref uint locale, out int error)
    {
        error = 0;
        output = DOS.Output();
        if (to != 0)
        {
            output = DOS.OpenRaw(CString.FromPointer(to), DOS.FileMode.NewFile);
            ownOutput = true;
        }
        if (output.IsNull)
        {
            error = (int)DOS.IoErr();
            DOS.PrintFault((DOS.Error)error, "Date");
            return DOS.RETURN_FAIL;
        }

        DOS.DateStamp(dateTime);
        if (lformat != 0)
        {
            localeLibrary = Exec.OpenLibraryRaw(Locale.Name, 38);
            if (localeLibrary.IsNull)
            {
                DOS.PutStr("Date: Error opening locale.library\n");
                error = (int)DOS.Error.NotImplemented;
                return DOS.RETURN_FAIL;
            }
            Locale.LocaleLibraryBase = localeLibrary;
            locale = Locale.OpenLocale(CString.FromPointer(0));
            var hook = APTR.FromPointer(workspace.Raw + HookOffset);
            APTR.WriteUInt32(hook, UtilityLayout.Hook.Entry,
                APTR.ExportAddress("copperos.morphos.date.putch").Raw);
            APTR.WriteUInt32(hook, UtilityLayout.Hook.SubEntry, 0);
            APTR.WriteUInt32(hook, UtilityLayout.Hook.Data, 0);
            APTR.WriteUInt32(hook, 20, output.Raw);
            Locale.FormatDate(locale, CString.FromPointer(lformat),
                dateTime.Raw, hook.Raw);
            _ = DOS.FPutC(output, '\n');
            return DOS.RETURN_OK;
        }

        var day = APTR.FromPointer(workspace.Raw + DayStringOffset);
        var date = APTR.FromPointer(workspace.Raw + DateStringOffset);
        var time = APTR.FromPointer(workspace.Raw + TimeStringOffset);
        var result = APTR.FromPointer(workspace.Raw + ResultStringOffset);
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Format,
            (byte)DosDateFormat.Dos);
        APTR.WriteUInt8(dateTime, DosLayout.DateTime.Flags, 0);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Day, day.Raw);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, date.Raw);
        APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, time.Raw);
        if (DOS.DateToStr(dateTime) == 0)
        {
            error = (int)DOS.IoErr();
            return DOS.RETURN_FAIL;
        }
        var length = CopyCString(day, result, 64);
        APTR.WriteUInt8(result, unchecked((int)length++), (byte)' ');
        length += CopyCString(date, APTR.FromPointer(result.Raw + length), 64 - length);
        APTR.WriteUInt8(result, unchecked((int)length++), (byte)' ');
        length += CopyCString(time, APTR.FromPointer(result.Raw + length), 64 - length);
        APTR.WriteUInt8(result, unchecked((int)length++), (byte)'\n');
        if (unchecked((uint)DOS.Write(output, result, unchecked((int)length))) < length)
        {
            error = (int)DOS.IoErr();
            DOS.PrintFault((DOS.Error)error, "Date");
            return DOS.RETURN_FAIL;
        }
        return DOS.RETURN_OK;
    }

    [M68kExport("copperos.morphos.date.putch")]
    [return: M68kRegister(M68kRegister.D0)]
    public static uint PutChar(
        [M68kRegister(M68kRegister.A0)] APTR hook,
        [M68kRegister(M68kRegister.A1)] APTR locale,
        [M68kRegister(M68kRegister.D0)] uint character)
    {
        var file = BPTR.FromRaw(APTR.ReadUInt32(hook, 20));
        if ((character & 0xff) != 0)
            DOS.FPutC(file, unchecked((int)(character & 0xff)));
        return 0;
    }

    private static uint CountChar(APTR source, byte value)
    {
        var count = 0u;
        for (var index = 0u;; index++)
        {
            var current = APTR.ReadUInt8(source, unchecked((int)index));
            if (current == 0) return count;
            if (current == value) count++;
        }
    }

    private static bool MultiplyChecked(uint left, uint right, out uint value)
    {
        value = 0;
        var multiplicand = left;
        var multiplier = right;
        for (var index = 0; index < 32; index++)
        {
            if ((multiplier & 1) != 0)
            {
                if (uint.MaxValue - value < multiplicand) return false;
                value += multiplicand;
            }
            multiplier >>= 1;
            multiplicand <<= 1;
        }
        return true;
    }

    private static bool AddChecked(uint left, uint right, out uint value)
    {
        value = left + right;
        return value >= left;
    }

    private static uint CStringLength(APTR source)
    {
        for (var index = 0u;; index++)
            if (APTR.ReadUInt8(source, unchecked((int)index)) == 0)
                return index;
    }

    private static uint CopyCString(APTR source, APTR destination, uint capacity)
    {
        var length = CStringLength(source);
        if (length + 1 > capacity) length = capacity == 0 ? 0 : capacity - 1;
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(destination, unchecked((int)index),
                APTR.ReadUInt8(source, unchecked((int)index)));
        if (capacity != 0) APTR.WriteUInt8(destination, unchecked((int)length), 0);
        return length;
    }
}
