using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-observed streaming core shared by MorphOS Copy's normal file path.
/// The caller owns valid DOS handles and invocation-local buffer storage.
/// </summary>
public static class NativeMorphOSCopyLoop
{
    /// <summary>
    /// Copies until a zero read, preserving Copy's one Write for every Read
    /// including the zero-length EOF read. A short write and a failed read
    /// fail the operation; Ctrl-C sets ERROR_BREAK before returning FAIL.
    /// </summary>
    public static int Run(BPTR from, BPTR to, APTR buffer, int bufferSize,
        out int ioError)
    {
        ioError = 0;
        if (from.IsNull || to.IsNull || buffer.IsNull || bufferSize <= 0)
        {
            ioError = (int)DOS.Error.BadTemplate;
            return DOS.RETURN_FAIL;
        }

        while (true)
        {
            if (NativeCommandIo.IsCtrlCPending())
            {
                ioError = (int)DOS.Error.Break;
                DOS.SetIoErr((DOS.Error)ioError);
                return DOS.RETURN_FAIL;
            }

            var count = DOS.Read(from, buffer, bufferSize);
            if (count == -1)
            {
                ioError = (int)DOS.IoErr();
                return DOS.RETURN_FAIL;
            }

            if (DOS.Write(to, buffer, count) != count)
            {
                ioError = (int)DOS.IoErr();
                return DOS.RETURN_FAIL;
            }

            if (count == 0)
                return DOS.RETURN_OK;
        }
    }
}
