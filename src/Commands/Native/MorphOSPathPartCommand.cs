using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded MorphOS PathPart command body. It keeps all path interpretation in
/// the public DOS path helpers; the command owns only a per-invocation output
/// buffer and its ReadArgs result lease.
/// </summary>
public static class MorphOSPathPartCommand
{
    private const uint BufferBytes = 1024;
    private const uint MaximumAddParts = 64;

    /// <summary>
    /// Implements the documented candidate MorphOS outer template. The exact
    /// combined-mode presentation remains subject to reference capture, but
    /// every lexical operation delegates to DOS rather than host path rules.
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
            buffer = Exec.AllocMem(BufferBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (buffer.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
            }
            else if (directory != 0 && !WriteDirectory(APTR.FromPointer(directory),
                    buffer, out ioError))
                result = DOS.RETURN_ERROR;
            else if (file != 0 && !WriteString(CString.FromPointer(DOS.FilePart(
                    CString.FromPointer(file)).Raw), buffer, out ioError))
                result = DOS.RETURN_ERROR;
            else if (additions != 0 && !WriteAddedPath(APTR.FromPointer(additions),
                    buffer, out ioError))
                result = DOS.RETURN_ERROR;
        }
        if (buffer.IsNotNull)
            Exec.FreeMem(buffer, BufferBytes);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool WriteDirectory(APTR value, APTR buffer, out int ioError)
    {
        var start = CString.FromPointer(value.Raw);
        var part = DOS.PathPart(start);
        if (part.Raw < value.Raw || part.Raw - value.Raw >= BufferBytes)
        {
            ioError = (int)DOS.Error.LineTooLong;
            return false;
        }
        var length = part.Raw - value.Raw;
        for (var index = 0u; index < length; index++)
            APTR.WriteUInt8(buffer, (int)index, APTR.ReadUInt8(value, (int)index));
        return WriteBuffer(buffer, length, out ioError);
    }

    private static bool WriteString(CString value, APTR buffer, out int ioError)
    {
        var source = APTR.FromPointer(CString.ToUInt32(value));
        for (var index = 0u; index + 1 < BufferBytes; index++)
        {
            var current = APTR.ReadUInt8(source, (int)index);
            if (current == 0)
                return WriteBuffer(buffer, index, out ioError);
            APTR.WriteUInt8(buffer, (int)index, current);
        }
        ioError = (int)DOS.Error.LineTooLong;
        return false;
    }

    private static bool WriteAddedPath(APTR additions, APTR buffer, out int ioError)
    {
        APTR.WriteUInt8(buffer, 0, 0);
        for (var index = 0u; index < MaximumAddParts; index++)
        {
            var value = APTR.ReadUInt32(additions, (int)(index * sizeof(uint)));
            if (value == 0)
                return WriteString(CString.FromPointer(buffer.Raw), buffer,
                    out ioError);
            if (DOS.AddPart(CString.FromPointer(buffer.Raw),
                    CString.FromPointer(value), BufferBytes) == 0)
            {
                ioError = (int)DOS.IoErr();
                return false;
            }
        }
        ioError = (int)DOS.Error.LineTooLong;
        return false;
    }

    private static bool WriteBuffer(APTR buffer, uint length, out int ioError)
    {
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
