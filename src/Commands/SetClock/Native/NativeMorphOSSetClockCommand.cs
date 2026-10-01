using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 SetClock body based on the released AROS-derived source.
/// Argument parsing and all temporary Exec/DOS/timer resources are invocation
/// owned.  The classic ReadBattClock/WriteBattClock/ResetBattClock path and
/// MorphOS 52+ UTC extensions are available through the public battclock and
/// timer vectors.  The provider-version gate mirrors the released command.
/// </summary>
public static class NativeMorphOSSetClockCommand
{
    public const string Template = "LOAD/S,SAVE/S,RESET/S";
    public const uint ResultCount = 3;

    private const uint CtrlCMask = 1u << 12;
    private const short ReadBattClockLvo = -12;
    private const short WriteBattClockLvo = -18;
    private const short ResetBattClockLvo = -6;
    private const short ReadUtcBattClockLvo = -40;
    private const short WriteUtcBattClockLvo = -46;
    private const short GetSysTimeLvo = -66;
    private const short GetUtcSysTimeLvo = -88;
    private const ushort SetSystemTimeCommand = (ushort)TimerCommand.SetSystemTime;
    private const ushort SetUtcSystemTimeCommand =
        (ushort)((ushort)TimerCommand.SetSystemTime + 2);

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "SetClock");
            return DOS.RETURN_FAIL;
        }

        APTR battClock = APTR.Null;
        APTR timerPort = APTR.Null;
        APTR timerRequest = APTR.Null;
        var timerOpened = false;
        var result = DOS.RETURN_FAIL;
        var error = 0;

        do
        {
            if (!arguments.TryGetResult(0, out var load) ||
                !arguments.TryGetResult(1, out var save) ||
                !arguments.TryGetResult(2, out var reset))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            battClock = Exec.OpenResource("battclock.resource");
            if (battClock.IsNull)
            {
                error = (int)DOS.Error.InvalidResidentLibrary;
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
                error = (int)DOS.Error.InvalidResidentLibrary;
                break;
            }
            timerOpened = true;

            var timerBase = APTR.FromPointer(APTR.ReadUInt32(timerRequest,
                ExecLayout.IORequest.Device));
            var battVersion = APTR.ReadUInt16(battClock,
                ExecLayout.Library.Version);
            var timerVersion = timerBase.IsNull ? 0u :
                APTR.ReadUInt16(timerBase, ExecLayout.Library.Version);
            var utc = battVersion >= 52 && timerVersion >= 52;

            if (load != 0)
            {
                var time = utc ? ReadUtcBattClock(battClock) :
                    ReadBattClock(battClock);
                APTR.WriteUInt32(timerRequest,
                    TimerDeviceLayout.TimerRequest.Seconds, time);
                APTR.WriteUInt32(timerRequest,
                    TimerDeviceLayout.TimerRequest.Microseconds, 0);
                APTR.WriteUInt16(timerRequest, ExecLayout.IORequest.Command,
                    utc ? SetUtcSystemTimeCommand : SetSystemTimeCommand);
                var flags = APTR.ReadUInt8(timerRequest,
                    ExecLayout.IORequest.Flags);
                APTR.WriteUInt8(timerRequest, ExecLayout.IORequest.Flags,
                    (byte)(flags | (byte)IOFlags.Quick));
                Exec.DoIO(timerRequest);
                if (APTR.ReadUInt8(timerRequest,
                        ExecLayout.IORequest.Error) != 0)
                {
                    DOS.PutStr("Error: Could not set system time!\n");
                    break;
                }
                result = DOS.RETURN_OK;
            }
            else if (save != 0)
            {
                var time = APTR.FromPointer(
                    Exec.AllocMem(8, Exec.MemoryFlags.Public |
                        Exec.MemoryFlags.Clear));
                if (time.IsNull)
                {
                    error = (int)DOS.Error.NoFreeStore;
                    break;
                }
                if (utc)
                    GetUtcSysTime(timerBase, time);
                else
                    GetSysTime(timerBase, time);
                if (utc)
                    WriteUtcBattClock(battClock, APTR.ReadUInt32(time, 0));
                else
                    WriteBattClock(battClock, APTR.ReadUInt32(time, 0));
                Exec.FreeMem(time, 8);
                result = DOS.RETURN_OK;
            }
            else if (reset != 0)
            {
                ResetBattClock(battClock);
                result = DOS.RETURN_OK;
            }
            else
            {
                error = (int)DOS.Error.RequiredArgumentMissing;
            }
        }
        while (false);

        if (timerOpened && timerRequest.IsNotNull)
            Exec.CloseDevice(timerRequest);
        if (timerRequest.IsNotNull)
            Exec.DeleteIORequest(timerRequest);
        if (timerPort.IsNotNull)
            Exec.DeleteMsgPort(timerPort);

        arguments.Release();
        if (error != 0)
        {
            DOS.PrintFault((DOS.Error)error, "SetClock");
            result = DOS.RETURN_FAIL;
        }
        else
        {
            DOS.SetIoErr(DOS.Error.None);
        }
        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static APTR Entry(APTR library, short lvo) =>
        APTR.FromPointer(unchecked(library.Raw - (uint)-lvo));

    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern uint ReadBattClockCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR resource);

    [AmigaIndirectCall(M68kRegister.A3)]
    private static extern void WriteBattClockCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR resource,
        [M68kRegister(M68kRegister.D0)] uint time);

    [AmigaIndirectCall(M68kRegister.A3)]
    private static extern void ResetBattClockCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR resource);

    [AmigaIndirectCall(M68kRegister.A3)]
    private static extern void GetSysTimeCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR timer,
        [M68kRegister(M68kRegister.A0)] APTR timeval);

    private static uint ReadBattClock(APTR resource) =>
        ReadBattClockCall(Entry(resource, ReadBattClockLvo), resource);

    private static void WriteBattClock(APTR resource, uint time) =>
        WriteBattClockCall(Entry(resource, WriteBattClockLvo), resource, time);

    private static void ResetBattClock(APTR resource) =>
        ResetBattClockCall(Entry(resource, ResetBattClockLvo), resource);

    private static uint ReadUtcBattClock(APTR resource) =>
        ReadUtcBattClockCall(Entry(resource, ReadUtcBattClockLvo), resource);

    private static void WriteUtcBattClock(APTR resource, uint time) =>
        WriteUtcBattClockCall(Entry(resource, WriteUtcBattClockLvo), resource,
            time);

    private static void GetSysTime(APTR timer, APTR timeval) =>
        GetSysTimeCall(Entry(timer, GetSysTimeLvo), timer, timeval);

    private static void GetUtcSysTime(APTR timer, APTR timeval) =>
        GetUtcSysTimeCall(Entry(timer, GetUtcSysTimeLvo), timer, timeval);

    [AmigaIndirectCall(M68kRegister.A3)]
    [return: M68kRegister(M68kRegister.D0)]
    private static extern uint ReadUtcBattClockCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR resource);

    [AmigaIndirectCall(M68kRegister.A3)]
    private static extern void WriteUtcBattClockCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR resource,
        [M68kRegister(M68kRegister.D0)] uint time);

    [AmigaIndirectCall(M68kRegister.A3)]
    private static extern void GetUtcSysTimeCall(
        [M68kRegister(M68kRegister.A3)] APTR entry,
        [M68kRegister(M68kRegister.A6)] APTR timer,
        [M68kRegister(M68kRegister.A0)] APTR timeval);
}
