using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Source-informed MorphOS 50.4 MakeDir command body.  The command keeps the
/// NAME/M vector and all path edits inside the DOS ReadArgs lease, and uses
/// public DOS directory/lock calls for both the ordinary and ALL paths.
///
/// The packed 3.20 member has not been decoded or executed; this class is an
/// independently written public-API candidate, not a copied implementation.
/// </summary>
public static class NativeMorphOSMakeDirCommand
{
    public const string Template = "NAME/M,ALL/S";
    public const uint ResultCount = 2;

    private const uint FileInfoBytes = FileInfoBlock.SizeInBytes;

    /// <summary>
    /// Creates each requested directory and returns the last operation level,
    /// matching the source's unusual per-name result policy.  The optional
    /// ALL path creates missing parents by walking DOS path components.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            if (ioError != 0)
                DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            // The 50.4 body initializes its command result to RETURN_FAIL;
            // ReadArgs failure never changes that selected level.
            return DOS.RETURN_FAIL;
        }

        APTR fib = APTR.Null;
        var result = DOS.RETURN_FAIL;
        var savedError = 0;
        do
        {
            if (!arguments.TryGetResult(0, out var names) ||
                !arguments.TryGetResult(1, out var all))
            {
                savedError = (int)DOS.Error.BadTemplate;
                break;
            }

            if (names == 0)
            {
                DOS.FPuts(DOS.Output(), "No name given\n");
                savedError = 0;
                break;
            }

            if (all != 0)
            {
                fib = Exec.AllocMem(FileInfoBytes,
                    Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
                if (fib.IsNull)
                {
                    savedError = (int)DOS.Error.NoFreeStore;
                    result = DOS.RETURN_FAIL;
                    break;
                }
            }

            var slot = APTR.FromPointer(names);
            while (true)
            {
                var name = APTR.ReadUInt32(slot, 0);
                if (name == 0) break;

                BPTR created;
                if (all != 0)
                {
                    created = CreateDirAll(APTR.FromPointer(name), fib,
                        out savedError);
                }
                else
                {
                    created = DOS.CreateDirRaw(CString.FromPointer(name));
                    if (created.IsNull)
                        savedError = (int)DOS.IoErr();
                    else
                        savedError = 0;
                }

                if (created.IsNotNull)
                {
                    DOS.UnLock(created);
                    result = DOS.RETURN_OK;
                }
                else
                {
                    DOS.VPrintf("Cannot create directory %s\n", slot);
                    result = DOS.RETURN_ERROR;
                    // Keep the immediate DOS error for the final PrintFault;
                    // a later successful operation may still change it.
                    if (savedError == 0)
                        savedError = (int)DOS.IoErr();
                }

                slot = APTR.FromPointer(slot.Raw + sizeof(uint));
            }

            // A final successful CreateDir may leave the error from an earlier
            // failure in DOS.  Preserve the provider's current value exactly.
            savedError = (int)DOS.IoErr();
        }
        while (false);

        if (fib.IsNotNull)
            Exec.FreeMem(fib, FileInfoBytes);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)savedError);
        if (result != DOS.RETURN_OK && savedError != 0)
            DOS.PrintFault((DOS.Error)savedError, CString.FromPointer(0));
        ioError = savedError;
        return result;
    }

    private static BPTR CreateDirAll(APTR start, APTR fib, out int ioError)
    {
        ioError = 0;
        var name = start;
        var cursor = start;
        var first = true;
        var oldCurrent = DOS.CurrentDirRaw(BPTR.Null);
        var skip = 0;
        byte saved = 0;

        while (true)
        {
            var character = APTR.ReadUInt8(cursor, 0);
            cursor = APTR.FromPointer(cursor.Raw + 1);
            if (character != (byte)':' && character != (byte)'/' &&
                character != 0)
                continue;

            BPTR lock_;
            if (character == (byte)':')
            {
                skip = 0;
                if (!first)
                {
                    DOS.SetIoErr(DOS.Error.DeviceNotMounted);
                    ioError = (int)DOS.Error.DeviceNotMounted;
                    break;
                }
                first = false;
                saved = APTR.ReadUInt8(cursor, skip);
                APTR.WriteUInt8(cursor, skip, 0);
                lock_ = DOS.LockRaw(CString.FromPointer(name.Raw),
                    DOS.LockMode.Read);
            }
            else
            {
                skip = 0;
                if (character == (byte)'/')
                {
                    while (APTR.ReadUInt8(cursor, skip) == (byte)'/')
                        skip++;
                }

                saved = APTR.ReadUInt8(cursor, skip);
                APTR.WriteUInt8(cursor, skip, 0);
                lock_ = DOS.LockRaw(CString.FromPointer(name.Raw),
                    DOS.LockMode.Read);
                if (lock_.IsNull)
                {
                    APTR.WriteUInt8(cursor, skip, saved);
                    skip = APTR.ReadUInt8(name, 0) != (byte)'/' &&
                        character == (byte)'/' ? -1 : 0;
                    saved = ReadRelative(cursor, skip);
                    WriteRelative(cursor, skip, 0);
                    lock_ = APTR.ReadUInt8(name, 0) == (byte)'/' ||
                        APTR.ReadUInt8(name, 0) == 0
                        ? BPTR.Null
                        : DOS.CreateDirRaw(CString.FromPointer(name.Raw));
                    if (lock_.IsNotNull &&
                        DOS.ChangeMode(DosChangeModeTarget.Lock, lock_,
                            DOS.LockMode.Read) == 0)
                    {
                        DOS.UnLock(lock_);
                        lock_ = DOS.LockRaw(CString.FromPointer(name.Raw),
                            DOS.LockMode.Read);
                    }
                }
                else
                {
                    var examined = DOS.Examine(lock_, fib);
                    if (examined == 0 ||
                        FileInfoBlock.GetDirEntryType(fib.Raw) < 0)
                    {
                        DOS.UnLock(lock_);
                        if (examined != 0)
                        {
                            var error = character == 0
                                ? DOS.Error.ObjectExists
                                : DOS.Error.ObjectWrongType;
                            DOS.SetIoErr(error);
                            ioError = (int)error;
                        }
                        else
                        {
                            ioError = (int)DOS.IoErr();
                        }
                        break;
                    }
                    cursor = APTR.FromPointer(cursor.Raw + (uint)skip);
                    skip = 0;
                }
            }

            if (lock_.IsNull)
            {
                ioError = (int)DOS.IoErr();
                break;
            }

            var previous = DOS.CurrentDirRaw(lock_);
            if (previous.IsNotNull && previous.Raw != oldCurrent.Raw)
                DOS.UnLock(previous);

            WriteRelative(cursor, skip, saved);
            name = cursor;
            if (character == 0)
            {
                var restored = RestoreCurrent(oldCurrent);
                ioError = 0;
                return restored;
            }
        }

        WriteRelative(cursor, skip, saved);
        var failedRestore = RestoreCurrent(oldCurrent);
        if (failedRestore.IsNotNull && failedRestore.Raw != oldCurrent.Raw)
            DOS.UnLock(failedRestore);
        if (ioError == 0)
            ioError = (int)DOS.IoErr();
        return BPTR.Null;
    }

    private static byte ReadRelative(APTR address, int offset) =>
        APTR.ReadUInt8(offset < 0
            ? APTR.FromPointer(address.Raw - (uint)(-offset))
            : APTR.FromPointer(address.Raw + (uint)offset), 0);

    private static void WriteRelative(APTR address, int offset, byte value)
    {
        APTR.WriteUInt8(offset < 0
            ? APTR.FromPointer(address.Raw - (uint)(-offset))
            : APTR.FromPointer(address.Raw + (uint)offset), 0, value);
    }

    private static BPTR RestoreCurrent(BPTR oldCurrent)
    {
        var previous = DOS.CurrentDirRaw(oldCurrent);
        return previous.Raw == oldCurrent.Raw ? BPTR.Null : previous;
    }
}
