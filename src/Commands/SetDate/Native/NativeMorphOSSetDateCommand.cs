using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 SetDate body based on the released source contract.  The
/// original keeps the AnchorPath, DateTime and argument vector on its stack;
/// this bounded entry uses invocation-owned Exec storage for those records so
/// the resident image has no mutable static state.
/// </summary>
public static class NativeMorphOSSetDateCommand
{
    private const uint AnchorBytes = DosLayout.AnchorPath.Size;
    private const uint DateTimeBytes = DosDateTime.Size;
    private const byte DoWild = (byte)AnchorPathFlags.DoWild;
    private const byte DidDirectory = (byte)AnchorPathFlags.DidDirectory;
    private const byte DoDirectory = (byte)AnchorPathFlags.DoDirectory;
    private const byte SoftLink = (byte)DosConstants.SoftLink;

    // MorphOS dos.library 50.67 added the extended AnchorPath overlay.  The
    // high bit belongs to ap_Strlen and ap_Extended aliases ap_Reserved.
    private const ushort ExtendedAnchorPath = 0x8000;
    private const byte DontFollowSoftLinks = 1 << 2;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(
                "FILE/A,WEEKDAY,DATE,TIME,ALL/S", 5,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            // The MorphOS command leaves its initial RETURN_FAIL unchanged on
            // a ReadArgs failure, unlike the generic helper's ERROR default.
            return DOS.RETURN_FAIL;
        }

        APTR dateTime = APTR.Null;
        APTR anchor = APTR.Null;
        var result = DOS.RETURN_FAIL;
        var error = 0;
        var matchStarted = false;
        do
        {
            if (!arguments.TryGetResult(0, out var file) || file == 0 ||
                !arguments.TryGetResult(1, out var weekday) ||
                !arguments.TryGetResult(2, out var date) ||
                !arguments.TryGetResult(3, out var time) ||
                !arguments.TryGetResult(4, out var all))
            {
                error = (int)DOS.Error.BadTemplate;
                break;
            }

            dateTime = Exec.AllocMem(DateTimeBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (dateTime.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }

            // Defaults to the current DOS date and time.
            DOS.DateStamp(dateTime);
            APTR.WriteUInt8(dateTime, DosLayout.DateTime.Flags,
                (byte)DosDateTimeFlags.Future);
            APTR.WriteUInt8(dateTime, DosLayout.DateTime.Format,
                (byte)DosDateFormat.Dos);

            for (var valueIndex = 0; valueIndex < 3; valueIndex++)
            {
                var value = valueIndex == 0 ? weekday :
                    valueIndex == 1 ? date : time;
                if (value == 0) continue;

                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, value);
                APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, 0);
                if (DOS.StrToDate(dateTime) == 0)
                {
                    APTR.WriteUInt32(dateTime, DosLayout.DateTime.Date, 0);
                    APTR.WriteUInt32(dateTime, DosLayout.DateTime.Time, value);
                    if (DOS.StrToDate(dateTime) == 0)
                    {
                        error = (int)DOS.IoErr();
                        DOS.PutStr("SetDate failed: Invalid WEEKDAY, DATE or TIME string!\n");
                        break;
                    }
                }
            }
            if (error != 0) break;

            anchor = Exec.AllocMem(AnchorBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (anchor.IsNull)
            {
                error = (int)DOS.Error.NoFreeStore;
                break;
            }
            APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, DoWild);
            APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits,
                0x1000);
            APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength, 0);
            if (SupportsExtendedAnchorPath())
            {
                APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength,
                    ExtendedAnchorPath);
                APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Reserved,
                    DontFollowSoftLinks);
            }

            var match = DOS.MatchFirst(CString.FromPointer(file), anchor);
            error = match;
            matchStarted = true;
            while (error == 0)
            {
                var entryType = unchecked((int)APTR.ReadUInt32(anchor,
                    DosLayout.AnchorPath.Info + FileInfoBlock.DirEntryTypeOffset));
                var flags = APTR.ReadUInt8(anchor, DosLayout.AnchorPath.Flags);
                if (entryType > 0 && entryType != (int)SoftLink && all != 0)
                {
                    if ((flags & DidDirectory) == 0)
                    {
                        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                            (byte)(flags | DoDirectory));
                        error = DOS.MatchNext(anchor);
                        continue;
                    }
                    APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                        (byte)(flags & ~DidDirectory));
                }

                var current = APTR.FromPointer(APTR.ReadUInt32(anchor,
                    DosLayout.AnchorPath.Current));
                var sourceLock = current.IsNull ? BPTR.Null : BPTR.FromRaw(
                    APTR.ReadUInt32(current, DosLayout.AChain.Lock));
                if (sourceLock.IsNull)
                {
                    error = (int)DOS.Error.ObjectNotFound;
                    break;
                }
                var duplicate = DOS.DupLock(sourceLock);
                var duplicateRaw = duplicate.GetValueOrDefault();
                var oldDirectory = DOS.CurrentDirRaw(duplicateRaw);
                var name = APTR.FromPointer(anchor.Raw +
                    (uint)DosLayout.AnchorPath.Info +
                    (uint)FileInfoBlock.FileNameOffset);
                var updated = DOS.SetFileDate(CString.FromPointer(name),
                    dateTime);
                if (updated == 0 && entryType == (int)SoftLink)
                    updated = 1;
                if (updated == 0)
                    error = (int)DOS.IoErr();
                DOS.CurrentDirRaw(oldDirectory);
                DOS.UnLock(duplicateRaw);
                if (updated == 0) break;

                error = DOS.MatchNext(anchor);
            }

            if (error == (int)DOS.Error.NoMoreEntries)
            {
                error = 0;
                result = DOS.RETURN_OK;
            }
            else
            {
                DOS.PrintFault((DOS.Error)error, "SetDate failed");
                result = error == (int)DOS.Error.Break
                    ? DOS.RETURN_WARN : DOS.RETURN_FAIL;
            }
        }
        while (false);

        if (matchStarted) DOS.MatchEnd(anchor);
        if (anchor.IsNotNull) Exec.FreeMem(anchor, AnchorBytes);
        arguments.Release();
        if (dateTime.IsNotNull) Exec.FreeMem(dateTime, DateTimeBytes);
        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static bool SupportsExtendedAnchorPath()
    {
        var version = APTR.ReadUInt16(DOS.DOSLibraryBase,
            ExecLayout.Library.Version);
        var revision = APTR.ReadUInt16(DOS.DOSLibraryBase,
            ExecLayout.Library.Revision);
        return version > 50 || version == 50 && revision >= 67;
    }
}
