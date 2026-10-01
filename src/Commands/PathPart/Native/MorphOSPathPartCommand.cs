using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded MorphOS PathPart command body. It keeps all path interpretation in
/// the public DOS path helpers; the command owns only a per-invocation output
/// buffer and its ReadArgs result lease.
/// </summary>
public static class MorphOSPathPartCommand
{
    private const uint MaximumWriteBytes = 0x7fffffff;
    private const uint MaximumOutputLength = MaximumWriteBytes - 1;

    /// <summary>
    /// Implements the documented candidate MorphOS outer template. The exact
    /// combined-mode presentation remains subject to reference capture, but
    /// every lexical operation delegates to DOS rather than host path rules.
    /// Output storage is sized from the ReadArgs-owned inputs; the DOS Write
    /// LONG limit is the only path-length bound imposed by this body.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead("DIR/K,FILE/K,ADD/K/M", 3,
                out var arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        var buffer = APTR.Null;
        var bufferBytes = 0u;
        var result = DOS.RETURN_OK;
        if (!arguments.TryGetResult(0, out var directory) ||
            !arguments.TryGetResult(1, out var file) ||
            !arguments.TryGetResult(2, out var additions))
        {
            ioError = (int)DOS.Error.BadTemplate;
            result = DOS.RETURN_ERROR;
        }
        else
        {
            var requiredBytes = 1u;
            var directoryStart = APTR.Null;
            var directoryLength = 0u;
            var fileStart = APTR.Null;
            var fileLength = 0u;
            var additionCount = 0u;
            var additionBytes = 1u;
            if (directory != 0 && !TryGetDirectory(APTR.FromPointer(directory),
                    out directoryStart, out directoryLength, out ioError))
                result = DOS.RETURN_ERROR;
            else if (file != 0 && !TryGetFile(APTR.FromPointer(file),
                    out fileStart, out fileLength, out ioError))
                result = DOS.RETURN_ERROR;
            else if (additions != 0 && !TryMeasureAdditions(
                    APTR.FromPointer(additions), out additionCount,
                    out additionBytes, out ioError))
                result = DOS.RETURN_ERROR;

            if (result == DOS.RETURN_OK)
            {
                if (directory != 0 && directoryLength + 1 > requiredBytes)
                    requiredBytes = directoryLength + 1;
                if (file != 0 && fileLength + 1 > requiredBytes)
                    requiredBytes = fileLength + 1;
                if (additions != 0 && additionBytes > requiredBytes)
                    requiredBytes = additionBytes;

                bufferBytes = requiredBytes;
                buffer = Exec.AllocMem(bufferBytes,
                    Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
                if (buffer.IsNull)
                {
                    ioError = (int)DOS.Error.NoFreeStore;
                    result = DOS.RETURN_FAIL;
                }
                else if (directory != 0 && !WriteDirectory(directoryStart,
                        directoryLength, buffer, bufferBytes, out ioError))
                    result = DOS.RETURN_ERROR;
                else if (file != 0 && !WriteString(fileStart, fileLength,
                        buffer, bufferBytes, out ioError))
                    result = DOS.RETURN_ERROR;
                else if (additions != 0 && !WriteAddedPath(
                        APTR.FromPointer(additions), additionCount, buffer,
                        bufferBytes, out ioError))
                    result = DOS.RETURN_ERROR;
            }
        }
        if (buffer.IsNotNull)
        {
            Exec.FreeMem(buffer, bufferBytes);
        }
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool TryGetDirectory(APTR value, out APTR start,
        out uint length, out int ioError)
    {
        var path = CString.FromPointer(value.Raw);
        var part = DOS.PathPart(path);
        if (part.Raw < value.Raw || part.Raw - value.Raw > MaximumOutputLength)
        {
            start = APTR.Null;
            length = 0;
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }
        start = value;
        length = part.Raw - value.Raw;
        ioError = 0;
        return true;
    }

    private static bool TryGetFile(APTR value, out APTR start,
        out uint length, out int ioError)
    {
        var part = DOS.FilePart(CString.FromPointer(value.Raw));
        start = APTR.FromPointer(STRPTR.ToUInt32(part));
        return TryMeasureString(start, MaximumOutputLength, out length,
            out ioError);
    }

    private static bool TryMeasureAdditions(APTR additions, out uint count,
        out uint requiredBytes, out int ioError)
    {
        count = 0;
        requiredBytes = 1;
        var cursor = additions;
        while (true)
        {
            if (cursor.Raw > uint.MaxValue - sizeof(uint))
            {
                ioError = (int)DOS.Error.LineTooLong;
                return false;
            }
            var value = APTR.ReadUInt32(cursor, 0);
            if (value == 0)
            {
                ioError = 0;
                return true;
            }
            if (!TryMeasureString(APTR.FromPointer(value), MaximumOutputLength,
                    out var length, out ioError))
                return false;
            var additionBytes = length + 1;
            if (additionBytes > MaximumWriteBytes - requiredBytes)
            {
                ioError = (int)DOS.Error.LineTooLong;
                return false;
            }
            requiredBytes += additionBytes;
            count++;
            cursor += sizeof(uint);
        }
    }

    private static bool TryMeasureString(APTR value, uint maximumLength,
        out uint length, out int ioError)
    {
        for (var index = 0u; index <= maximumLength; index++)
        {
            if (APTR.ReadUInt8(value, (int)index) == 0)
            {
                length = index;
                ioError = 0;
                return true;
            }
        }
        length = 0;
        ioError = (int)DOS.Error.LineTooLong;
        return false;
    }

    private static bool WriteDirectory(APTR start, uint length, APTR buffer,
        uint bufferBytes, out int ioError)
    {
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(buffer, (int)index,
                APTR.ReadUInt8(start, (int)index));
        return WriteBuffer(buffer, length, bufferBytes, out ioError);
    }

    private static bool WriteString(APTR value, uint length, APTR buffer,
        uint bufferBytes, out int ioError)
    {
        if (length >= bufferBytes)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(buffer, (int)index,
                APTR.ReadUInt8(value, (int)index));
        return WriteBuffer(buffer, length, bufferBytes, out ioError);
    }

    private static bool WriteAddedPath(APTR additions, uint count, APTR buffer,
        uint bufferBytes, out int ioError)
    {
        APTR.WriteUInt8(buffer, 0, 0);
        var cursor = additions;
        for (var index = 0u; index < count; index++)
        {
            var value = APTR.ReadUInt32(cursor, 0);
            if (DOS.AddPart(CString.FromPointer(buffer.Raw),
                    CString.FromPointer(value), bufferBytes) == 0)
            {
                ioError = (int)DOS.IoErr();
                return false;
            }
            cursor += sizeof(uint);
        }
        if (!TryMeasureString(buffer, bufferBytes - 1,
                out var length, out ioError))
            return false;
        return WriteBuffer(buffer, length, bufferBytes, out ioError);
    }

    private static bool WriteBuffer(APTR buffer, uint length, uint bufferBytes,
        out int ioError)
    {
        if (length >= bufferBytes)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }
        APTR.WriteUInt8(buffer, (int)length, (byte)'\n');
        if (DOS.Write(DOS.Output(), buffer, (int)(length + 1)) ==
            (int)(length + 1))
        {
            ioError = 0;
            return true;
        }
        ioError = (int)DOS.IoErr();
        return false;
    }
}
