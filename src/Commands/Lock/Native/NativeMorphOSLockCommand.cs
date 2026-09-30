using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Source-observed MorphOS/AROS Lock command body.</summary>
public static class NativeMorphOSLockCommand
{
    public const string Template = "DRIVE/A,ON/S,OFF/S,PASSKEY";
    public const uint ResultCount = 4;

    private const int ActionWriteProtect = 1023;
    private const uint DeviceProcDeviceNodeOffset = 12;
    private const uint DeviceNodeTypeOffset = 4;

    /// <summary>
    /// Uses the public DOS device-process and packet vectors. The caller owns
    /// startup and DOS teardown; this body owns only ReadArgs state.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "Lock");
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_FAIL;
        var deviceProc = APTR.Null;
        do
        {
            if (!arguments.TryGetResult(0, out var drive) || drive == 0 ||
                !arguments.TryGetResult(1, out var on) ||
                !arguments.TryGetResult(2, out var off) ||
                !arguments.TryGetResult(3, out var passkey))
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            deviceProc = DOS.GetDeviceProc(CString.FromPointer(drive),
                APTR.Null);
            if (deviceProc.IsNull)
            {
                ioError = (int)DOS.IoErr();
                break;
            }

            var deviceNode = APTR.FromPointer(APTR.ReadUInt32(deviceProc,
                unchecked((int)DeviceProcDeviceNodeOffset)));
            var type = APTR.ReadUInt32(deviceNode,
                unchecked((int)DeviceNodeTypeOffset));
            if (type > (uint)DosListType.Volume)
            {
                ioError = (int)DOS.Error.ObjectWrongType;
                break;
            }

            var key = 0u;
            if (passkey != 0)
            {
                var value = APTR.FromPointer(passkey);
                for (var index = 0u;; index++)
                {
                    var character = APTR.ReadUInt8(value,
                        unchecked((int)index));
                    if (character == 0) break;
                    key = key * 10u + character;
                }
            }

            if (on != 0)
            {
                var packetResult = DOS.DoPkt2(
                    APTR.FromPointer(APTR.ReadUInt32(deviceProc, 0)),
                    ActionWriteProtect, -1, unchecked((int)key));
                if (packetResult == 0)
                {
                    ioError = (int)DOS.IoErr();
                    DOS.VPrintf("Attempt to lock drive %s failed\n",
                        APTR.FromPointer(drive));
                    break;
                }
                DOS.VPrintf("%s locked\n", APTR.FromPointer(drive));
            }
            else if (off != 0)
            {
                var packetResult = DOS.DoPkt2(
                    APTR.FromPointer(APTR.ReadUInt32(deviceProc, 0)),
                    ActionWriteProtect, 0, unchecked((int)key));
                if (packetResult == 0)
                {
                    ioError = (int)DOS.IoErr();
                    DOS.VPrintf("Attempt to unlock drive %s failed\n",
                        APTR.FromPointer(drive));
                    break;
                }
                DOS.VPrintf("%s unlocked\n", APTR.FromPointer(drive));
            }

            result = DOS.RETURN_OK;
        }
        while (false);

        if (deviceProc.IsNotNull) DOS.FreeDeviceProc(deviceProc);
        if (ioError != 0)
        {
            DOS.PrintFault((DOS.Error)ioError, "Lock");
            result = DOS.RETURN_FAIL;
        }
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }
}
