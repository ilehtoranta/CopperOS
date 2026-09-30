using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>Source-observed MorphOS/AROS DiskChange command body.</summary>
public static class NativeMorphOSDiskChangeCommand
{
    public const string Template = "DEVICE/A";
    public const uint ResultCount = 1;

    private const int ActionInhibit = 31;

    /// <summary>
    /// Finds the filesystem message port, inhibits the device long enough to
    /// deliver the disk-change event, and then releases the inhibit. The
    /// caller owns startup and DOS teardown; this body owns parser state.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "DiskChange");
            return DOS.RETURN_ERROR;
        }

        var result = DOS.RETURN_FAIL;
        do
        {
            if (!arguments.TryGetResult(0, out var device) || device == 0)
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            var port = DOS.DeviceProc(CString.FromPointer(device));
            if (port.IsNull)
            {
                ioError = (int)DOS.IoErr();
                DOS.PrintFault((DOS.Error)ioError,
                    "error while searching the device");
                break;
            }

            var inhibited = DOS.DoPkt(port, ActionInhibit, -1, 0, 0, 0, 0);
            if (inhibited == 0)
            {
                ioError = (int)DOS.IoErr();
                DOS.PrintFault((DOS.Error)ioError,
                    "error while inhibiting the device");
                break;
            }

            _ = DOS.DoPkt(port, ActionInhibit, 0, 0, 0, 0, 0);
            result = DOS.RETURN_OK;
        }
        while (false);

        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }
}
