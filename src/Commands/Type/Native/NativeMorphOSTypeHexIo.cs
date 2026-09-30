using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Streaming MorphOS Type HEX transfer. The caller owns streams and buffers;
/// the first sixteen bytes of the caller-owned input buffer retain a row across
/// DOS Read boundaries.
/// </summary>
public static class NativeMorphOSTypeHexIo
{
    private const uint CtrlCMask = 1u << 12;

    /// <summary>Copies an opened input stream as independently observed HEX rows.</summary>
    public static int Copy(BPTR input, BPTR output, APTR inputBuffer,
        uint inputCapacity, APTR outputBuffer, uint outputCapacity,
        out int ioError)
    {
        ioError = 0;
        if (input.IsNull || output.IsNull || inputBuffer.IsNull ||
            outputBuffer.IsNull || inputCapacity < 17 || outputCapacity < 64 ||
            inputCapacity > int.MaxValue || outputCapacity > int.MaxValue)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return DOS.RETURN_ERROR;
        }

        var line = inputBuffer;
        var readBuffer = APTR.FromPointer(inputBuffer.Raw + 16);
        var readCapacity = inputCapacity - 16;
        var lineUsed = 0u;
        var offset = 0u;
        var outputUsed = 0u;
        uint rows = 0;

        for (;;)
        {
            var read = DOS.Read(input, readBuffer, unchecked((int)readCapacity));
            if (read < 0)
            {
                ioError = (int)DOS.IoErr();
                return DOS.RETURN_ERROR;
            }
            if (read == 0) break;
            for (var index = 0; index < read; index++)
            {
                APTR.WriteUInt8(line, unchecked((int)lineUsed++),
                    APTR.ReadUInt8(readBuffer, index));
                if (lineUsed != 16) continue;
                if (!TryPoll(ref rows, out ioError)) return DOS.RETURN_ERROR;
                if (!TryWriteRow(output, line, 16, offset, outputBuffer,
                    outputCapacity, ref outputUsed, false, out ioError) ||
                    !TryFlush(output, outputBuffer, ref outputUsed, out ioError))
                    return DOS.RETURN_ERROR;
                offset += 16;
                lineUsed = 0;
            }
        }

        if (lineUsed != 0)
        {
            if (!TryPoll(ref rows, out ioError) || !TryWriteRow(output, line,
                lineUsed, offset, outputBuffer, outputCapacity, ref outputUsed,
                true, out ioError)) return DOS.RETURN_ERROR;
        }
        else if (!TryPoll(ref rows, out ioError)) return DOS.RETURN_ERROR;
        return TryFlush(output, outputBuffer, ref outputUsed, out ioError)
            ? DOS.RETURN_OK : DOS.RETURN_ERROR;
    }

    private static bool TryPoll(ref uint rows, out int ioError)
    {
        ioError = 0;
        if ((++rows & 15u) == 0 &&
            (Exec.SetSignal(0u, CtrlCMask) & CtrlCMask) != 0)
        {
            ioError = (int)DOS.Error.Break;
            return false;
        }
        return true;
    }

    private static bool TryWriteRow(BPTR output, APTR line, uint length,
        uint offset, APTR buffer, uint capacity, ref uint used, bool blankLine,
        out int ioError)
    {
        var digits = OffsetDigits(offset);
        for (var shift = (digits - 1) * 4; shift >= 0; shift -= 4)
            if (!TryAppend(output, buffer, capacity, ref used,
                Hex((byte)(offset >> shift)), out ioError)) return false;
        if (!TryAppend(output, buffer, capacity, ref used, (byte)':', out ioError) ||
            !TryAppend(output, buffer, capacity, ref used, (byte)' ', out ioError))
            return false;
        for (var index = 0u; index < 16; index++)
        {
            if (index < length)
            {
                var value = APTR.ReadUInt8(line, unchecked((int)index));
                if (!TryAppend(output, buffer, capacity, ref used, Hex((byte)(value >> 4)), out ioError) ||
                    !TryAppend(output, buffer, capacity, ref used, Hex(value), out ioError)) return false;
            }
            else if (!TryAppend(output, buffer, capacity, ref used, (byte)' ', out ioError) ||
                !TryAppend(output, buffer, capacity, ref used, (byte)' ', out ioError)) return false;
            if ((index & 3) == 3 && !TryAppend(output, buffer, capacity, ref used,
                (byte)' ', out ioError)) return false;
        }
        for (var index = 0u; index < length; index++)
        {
            var value = APTR.ReadUInt8(line, unchecked((int)index));
            if (!TryAppend(output, buffer, capacity, ref used,
                (value & 0x7f) >= 0x20 && value != 0x7f ? value : (byte)'.',
                out ioError)) return false;
        }
        if (!TryAppend(output, buffer, capacity, ref used, (byte)'\n', out ioError))
            return false;
        return !blankLine || TryAppend(output, buffer, capacity, ref used,
            (byte)'\n', out ioError);
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
            var written = DOS.Write(output, APTR.FromPointer(buffer.Raw + offset),
                unchecked((int)remaining));
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

    private static int OffsetDigits(uint offset) => offset < 0x10000 ? 4 :
        offset < 0x100000 ? 5 : offset < 0x1000000 ? 6 :
        offset < 0x10000000 ? 7 : 8;

    private static byte Hex(byte value)
    {
        var nibble = (byte)(value & 15);
        return nibble < 10 ? (byte)(nibble + '0') :
            (byte)(nibble - 10 + 'A');
    }
}
