using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Streaming text-mode transfer used by the MorphOS Type command. The caller
/// owns both DOS handles and the guest buffers; this routine owns no state
/// beyond one invocation and never imposes a whole-file size limit.
/// </summary>
public static class NativeMorphOSTypeTextIo
{
    // NDK 3.1 dos/dos.h: SIGBREAKF_CTRL_C. Match the observed Type polling
    // semantics: clear and observe Ctrl-C only, preserving other signals.
    private const uint CtrlCMask = 1u << 12;

    /// <summary>
    /// Copies one opened stream with the independently observed NUMBER and
    /// NOLINE behavior. Positive short writes are completed, a zero or failed
    /// write reports the current IoErr, and Ctrl-C is polled without consuming
    /// unrelated task signals.
    /// </summary>
    public static int Copy(BPTR input, BPTR output, bool number, bool noLine,
        APTR inputBuffer, uint inputCapacity, APTR outputBuffer,
        uint outputCapacity, out int ioError)
    {
        ioError = 0;
        if (input.IsNull || output.IsNull || inputBuffer.IsNull ||
            outputBuffer.IsNull || inputCapacity == 0 || outputCapacity == 0 ||
            inputCapacity > int.MaxValue || outputCapacity > int.MaxValue ||
            number && outputCapacity < 6)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return DOS.RETURN_ERROR;
        }

        var used = 0u;
        var last = (byte)0;
        var sawByte = false;
        ushort line = 0;
        uint breakPoll = 0;

        if (number && !TryWriteLineNumber(output, outputBuffer, outputCapacity,
                ref used, unchecked(++line), out ioError))
            return DOS.RETURN_ERROR;

        for (;;)
        {
            var read = DOS.Read(input, inputBuffer, unchecked((int)inputCapacity));
            if (read < 0)
            {
                ioError = (int)DOS.IoErr();
                return DOS.RETURN_ERROR;
            }
            if (read == 0) break;

            for (var index = 0; index < read; index++)
            {
                if ((++breakPoll & 255u) == 0 &&
                    (Exec.SetSignal(0u, CtrlCMask) & CtrlCMask) != 0)
                {
                    ioError = (int)DOS.Error.Break;
                    return DOS.RETURN_ERROR;
                }

                var value = APTR.ReadUInt8(inputBuffer, index);
                if (number && sawByte && last == (byte)'\n' &&
                    !TryWriteLineNumber(output, outputBuffer, outputCapacity,
                        ref used, unchecked(++line), out ioError))
                    return DOS.RETURN_ERROR;
                if (!TryAppend(output, outputBuffer, outputCapacity, ref used,
                        value, out ioError))
                    return DOS.RETURN_ERROR;
                if (value == (byte)'\n' && !TryFlush(output, outputBuffer,
                        ref used, out ioError))
                    return DOS.RETURN_ERROR;
                last = value;
                sawByte = true;
            }
        }

        if ((!sawByte || last != (byte)'\n') && !noLine &&
            !TryAppend(output, outputBuffer, outputCapacity, ref used,
                (byte)'\n', out ioError))
            return DOS.RETURN_ERROR;
        return TryFlush(output, outputBuffer, ref used, out ioError)
            ? DOS.RETURN_OK : DOS.RETURN_ERROR;
    }

    private static bool TryWriteLineNumber(BPTR output, APTR buffer,
        uint capacity, ref uint used, ushort line, out int ioError)
    {
        var value = (uint)line;
        var divisor = 10000u;
        var emitted = false;
        while (divisor != 0)
        {
            var digit = value / divisor;
            if (!TryAppend(output, buffer, capacity, ref used,
                    digit != 0 || emitted ? (byte)(digit + '0') : (byte)' ',
                    out ioError))
                return false;
            if (digit != 0 || emitted)
            {
                value %= divisor;
                emitted = true;
            }
            divisor /= 10;
        }
        return TryAppend(output, buffer, capacity, ref used, (byte)' ',
            out ioError);
    }

    private static bool TryAppend(BPTR output, APTR buffer, uint capacity,
        ref uint used, byte value, out int ioError)
    {
        if (used == capacity && !TryFlush(output, buffer, ref used, out ioError))
            return false;
        APTR.WriteUInt8(buffer, unchecked((int)used++), value);
        ioError = 0;
        return true;
    }

    private static bool TryFlush(BPTR output, APTR buffer, ref uint used,
        out int ioError)
    {
        var offset = 0u;
        while (offset < used)
        {
            var remaining = used - offset;
            var written = DOS.Write(output,
                APTR.FromPointer(buffer.Raw + offset), unchecked((int)remaining));
            if (written <= 0 || (uint)written > remaining)
            {
                ioError = (int)DOS.IoErr();
                return false;
            }
            offset += (uint)written;
        }
        used = 0;
        ioError = 0;
        return true;
    }
}
