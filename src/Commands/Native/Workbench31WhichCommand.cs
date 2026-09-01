using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded Workbench 3.1 <c>Which</c> command body. The implementation uses
/// DOS-owned segment and CLI path records directly: it neither loads a segment
/// nor retains a lock, segment, or command-path node after one candidate has
/// been reported. Exact option interactions, diagnostics, cancellation, and
/// installed pure qualification remain reference-work items.
/// </summary>
public static class Workbench31WhichCommand
{
    private const uint BufferBytes = 1024;
    private const uint MaximumPathLocks = 64;

    /// <summary>
    /// Parses the classic Workbench 3.1 template and reports bounded resident,
    /// current-directory, and CLI-path matches. The caller owns DOS startup and
    /// final teardown through <see cref="NativeCommandStartup"/>.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead("FILE/A,NORES/S,RES/S,ALL/S", 4,
                out var arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_WARN;
        var buffer = APTR.Null;
        try
        {
            if (!arguments.TryGetResult(0, out var fileAddress) ||
                fileAddress == 0 ||
                !arguments.TryGetResult(1, out var noResidents) ||
                !arguments.TryGetResult(2, out var residentsOnly) ||
                !arguments.TryGetResult(3, out var all))
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.Error.BadTemplate;
                return result;
            }

            buffer = Exec.AllocMem(BufferBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (buffer.IsNull)
            {
                result = DOS.RETURN_FAIL;
                ioError = (int)DOS.Error.NoFreeStore;
                return result;
            }

            var file = CString.FromPointer(fileAddress);
            var continueAfterMatch = all != 0;
            var restrictToResidents = residentsOnly != 0;
            var reportResidents = noResidents == 0;
            var found = false;
            var foundNonInternal = false;

            // System segments are the classic INTERNAL category. The direct
            // segment result is never dereferenced after Permit, so this does
            // not retain a resident-list reference while output may block.
            if (reportResidents && IsSegmentPresent(file, 1))
            {
                if (!WriteCategory(fileAddress, buffer, true))
                {
                    result = DOS.RETURN_ERROR;
                    ioError = (int)DOS.IoErr();
                    return result;
                }
                found = true;
                if (!continueAfterMatch) return DOS.RETURN_OK;
            }

            if (reportResidents && IsSegmentPresent(file, 0))
            {
                if (!WriteCategory(fileAddress, buffer, false))
                {
                    result = DOS.RETURN_ERROR;
                    ioError = (int)DOS.IoErr();
                    return result;
                }
                found = true;
                foundNonInternal = true;
                if (!continueAfterMatch) return DOS.RETURN_OK;
            }

            if (restrictToResidents)
            {
                if (found) return DOS.RETURN_OK;
                result = DOS.RETURN_WARN;
                // The observed NORES+RES conflict returns WARN without
                // publishing ObjectNotFound, whether or not ALL is present.
                ioError = noResidents != 0 ? 0 : (int)DOS.Error.ObjectNotFound;
                return result;
            }

            var direct = ReportLockedPath(file, buffer);
            if (direct < 0)
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.IoErr();
                return result;
            }
            if (direct != 0)
            {
                found = true;
                foundNonInternal = true;
                if (!continueAfterMatch) return DOS.RETURN_OK;
            }

            var cli = DOS.Cli();
            var path = cli.IsNull
                ? BPTR.Null
                : BPTR.FromRaw(APTR.ReadUInt32(cli,
                    DosLayout.CommandLineInterface.CommandDirectory));
            for (var index = 0u; path.IsNotNull && index < MaximumPathLocks;
                index++)
            {
                var node = path.Address;
                var directory = BPTR.FromRaw(APTR.ReadUInt32(node,
                    DosLayout.PathLock.Lock));
                var next = BPTR.FromRaw(APTR.ReadUInt32(node,
                    DosLayout.PathLock.Next));
                if (directory.IsNull ||
                    DOS.NameFromLock(directory, buffer, (int)BufferBytes) == 0 ||
                    DOS.AddPart(CString.FromPointer(buffer.Raw), file,
                        BufferBytes) == 0)
                {
                    result = DOS.RETURN_ERROR;
                    ioError = (int)DOS.IoErr();
                    return result;
                }

                var match = ReportLockedPath(CString.FromPointer(buffer.Raw),
                    buffer);
                if (match < 0)
                {
                    result = DOS.RETURN_ERROR;
                    ioError = (int)DOS.IoErr();
                    return result;
                }
                if (match != 0)
                {
                    found = true;
                    foundNonInternal = true;
                    if (!continueAfterMatch) return DOS.RETURN_OK;
                }
                path = next;
            }

            if (path.IsNotNull)
            {
                result = DOS.RETURN_ERROR;
                ioError = (int)DOS.Error.ObjectWrongType;
                return result;
            }
            // The original 3.1 command reports an INTERNAL match during ALL,
            // but still ends with WARN/ObjectNotFound when no resident or
            // filesystem route succeeds. Preserve that observable result
            // without treating the system-segment pointer as an owned object.
            if (found && !foundNonInternal)
            {
                result = DOS.RETURN_WARN;
                ioError = (int)DOS.Error.ObjectNotFound;
                return result;
            }
            if (!found)
            {
                result = DOS.RETURN_WARN;
                ioError = (int)DOS.Error.ObjectNotFound;
                return result;
            }
            return DOS.RETURN_OK;
        }
        finally
        {
            if (buffer.IsNotNull)
                Exec.FreeMem(buffer, BufferBytes);
            arguments.Release();
            DOS.SetIoErr((DOS.Error)ioError);
        }
    }

    private static bool IsSegmentPresent(CString name, int system)
    {
        Exec.Forbid();
        var segment = DOS.FindSegment(name, APTR.Null, system);
        Exec.Permit();
        return segment.IsNotNull;
    }

    private static int ReportLockedPath(CString candidate, APTR buffer)
    {
        var locked = DOS.Lock(candidate, DOS.LockMode.Shared);
        if (!locked.HasValue) return 0;
        var value = locked.Value;
        var reported = DOS.NameFromLock(value, buffer, (int)BufferBytes) != 0 &&
            WriteLine(buffer, buffer);
        var error = reported ? 0 : (int)DOS.IoErr();
        DOS.UnLock(value);
        if (!reported)
            DOS.SetIoErr((DOS.Error)error);
        return reported ? 1 : -1;
    }

    private static bool WriteCategory(uint fileAddress, APTR buffer,
        bool internalSegment)
    {
        var prefixLength = internalSegment ? 9u : 4u;
        if (internalSegment)
        {
            APTR.WriteUInt8(buffer, 0, (byte)'I');
            APTR.WriteUInt8(buffer, 1, (byte)'N');
            APTR.WriteUInt8(buffer, 2, (byte)'T');
            APTR.WriteUInt8(buffer, 3, (byte)'E');
            APTR.WriteUInt8(buffer, 4, (byte)'R');
            APTR.WriteUInt8(buffer, 5, (byte)'N');
            APTR.WriteUInt8(buffer, 6, (byte)'A');
            APTR.WriteUInt8(buffer, 7, (byte)'L');
            APTR.WriteUInt8(buffer, 8, (byte)' ');
        }
        else
        {
            APTR.WriteUInt8(buffer, 0, (byte)'R');
            APTR.WriteUInt8(buffer, 1, (byte)'E');
            APTR.WriteUInt8(buffer, 2, (byte)'S');
            APTR.WriteUInt8(buffer, 3, (byte)' ');
        }

        var source = APTR.FromPointer(fileAddress);
        for (var index = 0u; index + prefixLength + 1 < BufferBytes; index++)
        {
            var value = APTR.ReadUInt8(source, (int)index);
            if (value == 0)
            {
                APTR.WriteUInt8(buffer, (int)(prefixLength + index),
                    (byte)'\n');
                return DOS.Write(DOS.Output(), buffer,
                    (int)(prefixLength + index + 1)) ==
                    (int)(prefixLength + index + 1);
            }
            APTR.WriteUInt8(buffer, (int)(prefixLength + index), value);
        }
        DOS.SetIoErr(DOS.Error.LineTooLong);
        return false;
    }

    private static bool WriteLine(APTR source, APTR buffer)
    {
        for (var index = 0u; index + 1 < BufferBytes; index++)
        {
            if (APTR.ReadUInt8(source, (int)index) != 0) continue;
            APTR.WriteUInt8(buffer, (int)index, (byte)'\n');
            return DOS.Write(DOS.Output(), buffer, (int)(index + 1)) ==
                (int)(index + 1);
        }
        DOS.SetIoErr(DOS.Error.LineTooLong);
        return false;
    }
}
