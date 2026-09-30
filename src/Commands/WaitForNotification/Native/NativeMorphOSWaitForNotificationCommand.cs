using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 WaitForNotification body.  The command registers one DOS
/// notification request for every NAME result, waits on one invocation-owned
/// Exec signal, and removes every request before releasing the parser lease.
/// CONTINUE controls whether a failed StartNotify prevents the remaining names
/// from being registered.  The exact guest diagnostic text remains a
/// differential-validation concern; request ownership and signal behaviour do
/// not depend on managed state.
/// </summary>
public static class NativeMorphOSWaitForNotificationCommand
{
    public const string Template = "NAME/A/M,QUIET/S,CONTINUE=CNT/S";
    public const uint ResultCount = 3;

    private const uint ControlC = 1u << 12;
    private const uint MaxNames = 256;
    private const uint RequestBytes = (uint)DosLayout.NotifyRequest.Size;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "WaitForNotification");
            return arguments.ReturnLevel;
        }

        APTR requests = APTR.Null;
        var signal = (sbyte)-1;
        uint namesCount = 0;
        uint started = 0;
        var result = DOS.RETURN_FAIL;
        var error = 0;

        do
        {
            if (!arguments.TryGetResult(0, out var names) || names == 0 ||
                !arguments.TryGetResult(1, out var quiet) ||
                !arguments.TryGetResult(2, out var continuation))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            // ReadArgs NAME/A/M returns a null-terminated pointer vector.  A
            // bounded walk keeps malformed provider memory from turning into
            // an unbounded resident loop while retaining the normal grammar.
            namesCount = CountNames(APTR.FromPointer(names));
            if (namesCount == 0 || namesCount > MaxNames)
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            signal = Exec.AllocSignal(-1);
            if (signal < 0)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }

            if (!MultiplyBytes(namesCount, RequestBytes, out var bytes))
            {
                error = (int)DOS.Error.ObjectTooLarge;
                break;
            }
            requests = Exec.AllocMem(bytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (requests.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }

            var task = Exec.FindTask(CString.FromPointer(0));
            if (task.IsNull)
            {
                error = (int)DOS.Error.ObjectNotFound;
                break;
            }

            for (var index = 0u; index < namesCount; index++)
            {
                var nameSlot = APTR.FromPointer(names + index * 4);
                var name = APTR.FromPointer(APTR.ReadUInt32(nameSlot, 0));
                // Compact successful registrations so cleanup only needs the
                // count of live requests when CONTINUE skips a failed name.
                var request = APTR.FromPointer(requests.Raw + started * RequestBytes);
                APTR.WriteUInt32(request, DosLayout.NotifyRequest.Name,
                    name.Raw);
                APTR.WriteUInt32(request, DosLayout.NotifyRequest.FullName, 0);
                APTR.WriteUInt32(request, DosLayout.NotifyRequest.UserData,
                    name.Raw);
                APTR.WriteUInt32(request, DosLayout.NotifyRequest.Flags,
                    (uint)(DosNotifyFlags.SendSignal |
                        DosNotifyFlags.NotifyInitial));
                APTR.WriteUInt32(request, DosLayout.NotifyRequest.Target,
                    task.Raw);
                APTR.WriteUInt8(request,
                    DosLayout.NotifyRequest.Target +
                        DosLayout.NotifyRequestTarget.SignalNumber,
                    unchecked((byte)signal));

                if (DOS.StartNotify(request) != 0)
                {
                    started++;
                    continue;
                }

                var notifyError = (int)DOS.IoErr();
                if (quiet == 0)
                    DOS.PrintFault((DOS.Error)notifyError,
                        "WaitForNotification");
                if (continuation == 0)
                {
                    error = notifyError;
                    break;
                }
            }

            if (error != 0 && continuation == 0)
                break;
            if (started == 0)
            {
                if (error == 0) error = (int)DOS.Error.ObjectNotFound;
                break;
            }

            var signals = Exec.Wait(ControlC | (1u << signal));
            if ((signals & ControlC) != 0)
            {
                error = (int)DOS.Error.Break;
                result = DOS.RETURN_WARN;
            }
            else
            {
                result = DOS.RETURN_OK;
            }
        }
        while (false);

        for (var index = 0u; index < started; index++)
        {
            var request = APTR.FromPointer(requests.Raw + index * RequestBytes);
            DOS.EndNotify(request);
        }
        if (requests.IsNotNull && namesCount != 0 &&
            MultiplyBytes(namesCount, RequestBytes, out var requestBytes))
            Exec.FreeMem(requests, requestBytes);
        if (signal >= 0) Exec.FreeSignal(signal);
        arguments.Release();

        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static uint CountNames(APTR vector)
    {
        for (var index = 0u; index < MaxNames; index++)
        {
            if (APTR.ReadUInt32(vector, unchecked((int)(index * 4))) == 0)
                return index;
        }
        return MaxNames + 1;
    }

    private static bool MultiplyBytes(uint count, uint size, out uint bytes)
    {
        bytes = 0;
        if (count != 0 && size > uint.MaxValue / count) return false;
        bytes = count * size;
        return bytes != 0;
    }
}
