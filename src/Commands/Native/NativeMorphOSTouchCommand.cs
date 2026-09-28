using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 Touch body based on the released C source.  The matcher,
/// DateStamp workspace and argument lease are invocation owned.  This slice
/// uses the classic DateStamp/SetFileDate path, which is valid on DOS 3.1;
/// the DOS 51.66 UTC/POSIX branch remains a separately tracked ABI gap.
/// </summary>
public static class NativeMorphOSTouchCommand
{
    public const string Template = "NAME/A/M,VERBOSE/S,ALL/S";
    public const uint ResultCount = 3;

    private const uint PathBytes = 1024;
    private const uint AnchorBytes = DosLayout.AnchorPath.Size + PathBytes;
    private const uint DateBytes = DosLayout.DateStamp.Size;
    private const uint WorkspaceBytes = AnchorBytes + DateBytes;
    private const uint DateOffset = AnchorBytes;
    private const uint CtrlCMask = 1u << 12;
    private const byte DoDirectory = (byte)AnchorPathFlags.DoDirectory;
    private const byte DidDirectory = (byte)AnchorPathFlags.DidDirectory;

    public static int Run(out int ioError)
    {
        ioError = 0;
        var workspace = Exec.AllocMem(WorkspaceBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (workspace.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_FAIL;
        var error = 0;
        var matchStarted = false;
        APTR failureHeader = APTR.Null;
        var arguments = default(NativeCommandArguments);
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out arguments))
        {
            error = arguments.IoError;
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
        }
        else if (!arguments.TryGetResult(0, out var names) || names == 0 ||
            !arguments.TryGetResult(1, out var verbose) ||
            !arguments.TryGetResult(2, out var all))
        {
            error = (int)DOS.Error.BadTemplate;
        }
        else
        {
            result = DOS.RETURN_OK;
            var nameSlot = APTR.FromPointer(names);
            var date = APTR.FromPointer(workspace.Raw + DateOffset);
            DOS.DateStamp(date);

            while (APTR.ReadUInt32(nameSlot, 0) != 0)
            {
                var name = APTR.FromPointer(APTR.ReadUInt32(nameSlot, 0));
                failureHeader = name;
                InitializeAnchor(workspace);
                var match = DOS.MatchFirst(CString.FromPointer(name.Raw),
                    workspace);
                matchStarted = true;
                while (match == 0)
                {
                    var fib = APTR.FromPointer(workspace.Raw +
                        (uint)DosLayout.AnchorPath.Info);
                    var directory = FileInfoBlock.GetDirEntryType(fib.Raw) > 0;
                    var flags = APTR.ReadUInt8(workspace,
                        DosLayout.AnchorPath.Flags);
                    var handled = true;
                    if (directory)
                    {
                        if ((flags & DidDirectory) != 0)
                        {
                            APTR.WriteUInt8(workspace,
                                DosLayout.AnchorPath.Flags,
                                (byte)(flags & ~DidDirectory));
                            handled = TouchMatched(workspace, verbose != 0,
                                date, out error);
                        }
                        else if (all != 0)
                        {
                            APTR.WriteUInt8(workspace,
                                DosLayout.AnchorPath.Flags,
                                (byte)(flags | DoDirectory));
                            handled = true;
                        }
                        else
                        {
                            handled = TouchMatched(workspace, verbose != 0,
                                date, out error);
                        }
                    }
                    else
                    {
                        handled = TouchMatched(workspace, verbose != 0,
                            date, out error);
                    }

                    if (!handled)
                    {
                        result = DOS.RETURN_FAIL;
                        failureHeader = APTR.FromPointer(workspace.Raw +
                            (uint)DosLayout.AnchorPath.PathBuffer);
                        break;
                    }

                    if ((Exec.SetSignal(0u, CtrlCMask) & CtrlCMask) != 0)
                    {
                        error = (int)DOS.Error.Break;
                        result = DOS.RETURN_FAIL;
                        break;
                    }
                    match = DOS.MatchNext(workspace);
                }

                var matchError = match == 0 ? (int)DOS.IoErr() :
                    (error != 0 ? error : (int)DOS.IoErr());
                DOS.MatchEnd(workspace);
                matchStarted = false;

                if (result == DOS.RETURN_FAIL && error != 0)
                    break;

                if (matchError == (int)DOS.Error.ObjectNotFound)
                {
                    if (!TouchEntry(name, verbose != 0, date, out error))
                    {
                        result = DOS.RETURN_FAIL;
                        break;
                    }
                    error = (int)DOS.IoErr();
                }
                else if (matchError != (int)DOS.Error.NoMoreEntries &&
                    matchError != 0)
                {
                    error = matchError;
                    result = DOS.RETURN_FAIL;
                    break;
                }

                nameSlot = APTR.FromPointer(nameSlot.Raw + 4);
            }

            if (result != DOS.RETURN_OK && error != 0)
                DOS.PrintFault((DOS.Error)error,
                    CString.FromPointer(failureHeader.Raw));
        }

        if (matchStarted) DOS.MatchEnd(workspace);
        arguments.Release();
        Exec.FreeMem(workspace, WorkspaceBytes);
        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static void InitializeAnchor(APTR anchor)
    {
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Base, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Current, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, CtrlCMask);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.FoundBreak, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Reserved, 0);
        APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength,
            unchecked((ushort)PathBytes));
    }

    private static bool TouchMatched(APTR anchor, bool verbose, APTR date,
        out int error)
    {
        var current = APTR.FromPointer(APTR.ReadUInt32(anchor,
            DosLayout.AnchorPath.Current));
        var lockRaw = current.IsNull ? 0u : APTR.ReadUInt32(current,
            DosLayout.AChain.Lock);
        if (lockRaw == 0)
        {
            error = (int)DOS.Error.ObjectNotFound;
            return false;
        }

        var oldDirectory = DOS.CurrentDirRaw(BPTR.FromRaw(lockRaw));
        var fileName = APTR.FromPointer(anchor.Raw +
            (uint)DosLayout.AnchorPath.Info +
            (uint)FileInfoBlock.FileNameOffset);
        var result = TouchEntry(fileName, verbose, date, out error);
        DOS.CurrentDirRaw(oldDirectory);
        return result;
    }

    private static bool TouchEntry(APTR name, bool verbose, APTR date,
        out int error)
    {
        error = 0;
        if (verbose) DOS.PutStr(CString.FromPointer(name.Raw));
        var touched = DOS.SetFileDate(CString.FromPointer(name.Raw), date) != 0;
        if (touched)
        {
            if (verbose) DOS.PutStr("...touched\n");
            return true;
        }

        error = (int)DOS.IoErr();
        if (error == (int)DOS.Error.ObjectNotFound)
        {
            var file = DOS.OpenRaw(CString.FromPointer(name.Raw),
                DOS.FileMode.ReadWrite);
            if (file.IsNotNull)
            {
                DOS.Close(file);
                if (verbose) DOS.PutStr("...created\n");
                error = 0;
                return true;
            }
            error = (int)DOS.IoErr();
        }
        if (verbose) DOS.PutStr("...failed\n");
        return false;
    }
}
