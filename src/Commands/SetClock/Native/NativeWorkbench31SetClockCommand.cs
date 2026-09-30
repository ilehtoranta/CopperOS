using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench 3.1 SetClock candidate for the captured classic
/// LOAD/SAVE/RESET grammar.  It deliberately uses the pre-MorphOS
/// battclock.resource and timer.device vectors.
/// </summary>
public static class NativeWorkbench31SetClockCommand
{
    public const string Template = "LOAD/S,SAVE/S,RESET/S";
    public const uint ResultCount = 3;

    private const short ReadBattClockLvo = -12;
    private const short WriteBattClockLvo = -18;
    private const short ResetBattClockLvo = -6;
    private const short GetSysTimeLvo = -66;
    private const ushort SetSystemTimeCommand = (ushort)TimerCommand.SetSystemTime;

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
            if (load != 0)
            {
                APTR.WriteUInt32(timerRequest,
                    TimerDeviceLayout.TimerRequest.Seconds,
                    ReadBattClockCall(Entry(battClock, ReadBattClockLvo), battClock));
                APTR.WriteUInt32(timerRequest,
                    TimerDeviceLayout.TimerRequest.Microseconds, 0);
                APTR.WriteUInt16(timerRequest, ExecLayout.IORequest.Command,
                    SetSystemTimeCommand);
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
                var time = APTR.FromPointer(Exec.AllocMem(8,
                    Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear));
                if (time.IsNull)
                {
                    error = (int)DOS.Error.NoFreeStore;
                    break;
                }
                GetSysTimeCall(Entry(timerBase, GetSysTimeLvo), timerBase, time);
                WriteBattClockCall(Entry(battClock, WriteBattClockLvo),
                    battClock, APTR.ReadUInt32(time, 0));
                Exec.FreeMem(time, 8);
                result = DOS.RETURN_OK;
            }
            else if (reset != 0)
            {
                ResetBattClockCall(Entry(battClock, ResetBattClockLvo), battClock);
                result = DOS.RETURN_OK;
            }
            else
                error = (int)DOS.Error.RequiredArgumentMissing;
        }
        while (false);

        if (timerOpened && timerRequest.IsNotNull)
            Exec.CloseDevice(timerRequest);
        if (timerRequest.IsNotNull) Exec.DeleteIORequest(timerRequest);
        if (timerPort.IsNotNull) Exec.DeleteMsgPort(timerPort);
        arguments.Release();
        if (error != 0)
        {
            DOS.PrintFault((DOS.Error)error, "SetClock");
            result = DOS.RETURN_FAIL;
        }
        else
            DOS.SetIoErr(DOS.Error.None);
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
}
