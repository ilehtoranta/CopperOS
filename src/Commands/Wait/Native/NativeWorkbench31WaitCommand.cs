using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 Wait body. Short waits use DOS Delay; longer waits use the
/// public VBlank timer and scheduler signal path. The UNTIL branch follows the
/// v37 command's HH:MM and midnight-rollover behavior.
/// </summary>
public static class NativeWorkbench31WaitCommand
{
    public const string Template = "/N,SEC=SECS/S,MIN=MINS/S,UNTIL/K";
    public const uint ResultCount = 4;

    private const uint TicksPerSecond = 50;
    private const uint TicksPerDay = 24 * 60 * 60 * TicksPerSecond;
    private const uint ControlC = 1u << 12;
    private const uint WorkspaceBytes = 64;
    private const uint DateTimeOffset = 0;
    private const uint TimeStringOffset = 32;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "Wait");
            return arguments.ReturnLevel;
        }

        APTR workspace = APTR.Null;
        APTR timerPort = APTR.Null;
        APTR timerRequest = APTR.Null;
        var timerOpened = false;
        var result = DOS.RETURN_FAIL;
        var error = 0;

        do
        {
            if (!arguments.TryGetResult(0, out var numberSlot) ||
                !arguments.TryGetResult(1, out var secondsSwitch) ||
                !arguments.TryGetResult(2, out var minutesSwitch) ||
                !arguments.TryGetResult(3, out var until))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            _ = secondsSwitch;
            uint delay;
            if (until != 0)
            {
                workspace = Exec.AllocMem(WorkspaceBytes,
                    Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
                if (workspace.IsNull)
                {
                    error = (int)DOS.Error.NoFreeStore;
                    break;
                }
                var dateTime = APTR.FromPointer(workspace.Raw + DateTimeOffset);
                var timeString = APTR.FromPointer(workspace.Raw + TimeStringOffset);
                DOS.DateStamp(dateTime);
                var nowMinutes = APTR.ReadUInt32(dateTime,
                    DosLayout.DateStamp.Minutes);
                var nowTicks = APTR.ReadUInt32(dateTime,
                    DosLayout.DateStamp.Ticks);
                var nowSeconds = nowMinutes * 60 + nowTicks / TicksPerSecond;
                if (CStringLength(APTR.FromPointer(until)) > 5)
                {
                    DOS.PutStr("Time should be HH:MM");
                    error = (int)DOS.Error.BadTemplate;
                    break;
                }
                var length = CopyCString(APTR.FromPointer(until), timeString, 9);
                APTR.WriteUInt8(timeString, unchecked((int)length), (byte)':');
                APTR.WriteUInt8(timeString, unchecked((int)(length + 1)),
                    (byte)'0');
                APTR.WriteUInt8(timeString, unchecked((int)(length + 2)),
                    (byte)'0');
                APTR.WriteUInt8(timeString, unchecked((int)(length + 3)), 0);
                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time,
                    timeString.Raw);
                if (DOS.StrToDate(dateTime) == 0)
                {
                    DOS.PutStr("Time should be HH:MM");
                    error = (int)DOS.Error.BadTemplate;
                    break;
                }
                var thenMinutes = APTR.ReadUInt32(dateTime,
                    DosLayout.DateStamp.Minutes);
                var thenTicks = APTR.ReadUInt32(dateTime,
                    DosLayout.DateStamp.Ticks);
                var thenSeconds = thenMinutes * 60 + thenTicks / TicksPerSecond;
                var difference = thenSeconds >= nowSeconds
                    ? thenSeconds - nowSeconds
                    : TicksPerDay / TicksPerSecond - nowSeconds + thenSeconds;
                if (!MultiplyChecked(difference, TicksPerSecond, out delay))
                {
                    error = (int)DOS.Error.ObjectTooLarge;
                    break;
                }
            }
            else
            {
                var amount = numberSlot == 0 ? 1 : unchecked((int)
                    APTR.ReadUInt32(APTR.FromPointer(numberSlot), 0));
                if (amount < 0)
                {
                    error = (int)DOS.Error.BadNumber;
                    break;
                }
                var unitTicks = minutesSwitch != 0 ? 3000u : TicksPerSecond;
                if (!MultiplyChecked(unchecked((uint)amount), unitTicks,
                        out delay))
                {
                    error = (int)DOS.Error.ObjectTooLarge;
                    break;
                }
            }

            if (delay <= TicksPerSecond)
            {
                DOS.Delay(unchecked((int)delay));
                result = DOS.RETURN_OK;
                break;
            }

            timerPort = Exec.CreateMsgPort();
            if (timerPort.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }
            timerRequest = Exec.CreateIORequest(timerPort, TimerRequest.Size);
            if (timerRequest.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }
            if (Exec.OpenDevice(TimerDevice.Name, (uint)TimerUnit.VBlank,
                    timerRequest, 0) != 0)
            {
                DOS.PutStr("Wait: Could not open timer.device!");
                error = (int)DOS.Error.NotImplemented;
                break;
            }
            timerOpened = true;
            APTR.WriteUInt16(timerRequest, ExecLayout.IORequest.Command,
                (ushort)TimerCommand.AddRequest);
            APTR.WriteUInt32(timerRequest, TimerDeviceLayout.TimerRequest.Seconds,
                delay / TicksPerSecond);
            APTR.WriteUInt32(timerRequest,
                TimerDeviceLayout.TimerRequest.Microseconds,
                20000u * (delay % TicksPerSecond));
            Exec.SendIO(timerRequest);

            var signalBit = APTR.ReadUInt8(timerPort,
                ExecLayout.MsgPort.SignalBit);
            var timerMask = signalBit < 32 ? 1u << signalBit : 0u;
            var done = false;
            while (!done)
            {
                var signals = Exec.Wait(ControlC | timerMask);
                if ((signals & timerMask) != 0)
                {
                    result = DOS.RETURN_OK;
                    done = true;
                }
                if ((signals & ControlC) != 0)
                {
                    if (Exec.CheckIO(timerRequest).IsNull)
                        Exec.AbortIO(timerRequest);
                    _ = Exec.WaitIO(timerRequest);
                    result = DOS.RETURN_WARN;
                    done = true;
                }
            }
        }
        while (false);

        if (timerOpened && timerRequest.IsNotNull)
            Exec.CloseDevice(timerRequest);
        if (timerRequest.IsNotNull)
            Exec.DeleteIORequest(timerRequest);
        if (timerPort.IsNotNull)
            Exec.DeleteMsgPort(timerPort);
        if (workspace.IsNotNull)
            Exec.FreeMem(workspace, WorkspaceBytes);
        arguments.Release();

        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
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
        if (capacity != 0)
            APTR.WriteUInt8(destination, unchecked((int)length), 0);
        return length;
    }
}
