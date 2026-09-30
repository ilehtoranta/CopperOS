using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS/AROS source-observed Filenote body. The matcher, comment copy,
/// diagnostics and recursive AnchorPath state are invocation-owned; the
/// Workbench binary and exact packed diagnostics remain separate evidence.
/// </summary>
public static class NativeMorphOSFileNoteCommand
{
    public const string Template = "FILE/A,COMMENT,ALL/S,QUIET/S";
    public const uint ResultCount = 4;

    private const uint PathBytes = 512;
    private const uint CommentBytes = 80;
    private const uint WarningBytes = 8;
    private const uint AnchorBytes = DosLayout.AnchorPath.Size + PathBytes;
    private const uint CommentOffset = AnchorBytes;
    private const uint WarningOffset = CommentOffset + CommentBytes;
    private const uint WorkspaceBytes = WarningOffset + WarningBytes;
    private const byte DoDirectory = (byte)AnchorPathFlags.DoDirectory;
    private const byte DidDirectory = (byte)AnchorPathFlags.DidDirectory;
    private const byte DoWildFollowHardLinks =
        (byte)(AnchorPathFlags.DoWild | AnchorPathFlags.FollowHardLinks);

    /// <summary>
    /// Applies one source-observed Filenote invocation through public DOS
    /// matcher and SetComment calls. The caller owns startup and final DOS
    /// teardown; this body owns only its parser and workspace.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        var workspace = Exec.AllocMem(WorkspaceBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (workspace.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            return DOS.RETURN_FAIL;
        }

        var arguments = default(NativeCommandArguments);
        var result = DOS.RETURN_FAIL;
        var parsed = NativeCommandArguments.TryRead(Template, ResultCount,
            out arguments);
        if (parsed)
        {
            if (!arguments.TryGetResult(0, out var file) || file == 0 ||
                !arguments.TryGetResult(1, out var comment) ||
                !arguments.TryGetResult(2, out var all) ||
                !arguments.TryGetResult(3, out var quiet))
            {
                ioError = (int)DOS.Error.BadTemplate;
            }
            else
            {
                var commentBuffer = APTR.FromPointer(workspace.Raw +
                    CommentOffset);
                var warningArguments = APTR.FromPointer(workspace.Raw +
                    WarningOffset);
                var truncated = CopyComment(comment == 0
                    ? APTR.Null
                    : APTR.FromPointer(comment), commentBuffer);
                if (truncated)
                    DOS.PutStr("Note truncated to 79 characters\n");

                var anchor = workspace;
                InitializeAnchor(anchor);
                var match = DOS.MatchFirst(CString.FromPointer(file), anchor);
                if (match != 0)
                {
                    ioError = (int)DOS.IoErr();
                    DOS.PrintFault((DOS.Error)ioError, "Filenote");
                    result = DOS.RETURN_FAIL;
                }
                else
                {
                    result = ApplyMatches(anchor, commentBuffer,
                        all != 0, quiet != 0, warningArguments, out ioError);
                }
                // MatchEnd is required after every MatchFirst attempt,
                // including an immediate failure.
                DOS.MatchEnd(anchor);
            }
        }
        else
        {
            result = arguments.ReturnLevel;
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, "Filenote");
        }

        arguments.Release();
        Exec.FreeMem(workspace, WorkspaceBytes);
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static void InitializeAnchor(APTR anchor)
    {
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Base, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Current, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.FoundBreak, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
            DoWildFollowHardLinks);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Reserved, 0);
        APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength,
            unchecked((ushort)PathBytes));
    }

    private static int ApplyMatches(APTR anchor, APTR comment,
        bool all, bool quiet, APTR warningArguments, out int ioError)
    {
        var result = DOS.RETURN_OK;
        ioError = 0;
        var depth = 0;
        var match = 0;
        do
        {
            var directory = FileInfoBlock.GetDirEntryType(anchor.Raw +
                (uint)DosLayout.AnchorPath.Info) > 0;
            var flags = APTR.ReadUInt8(anchor, DosLayout.AnchorPath.Flags);
            var process = true;
            var leaveDirectory = false;
            if (all && directory)
            {
                if ((flags & DidDirectory) == 0)
                {
                    APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                        (byte)(flags | DoDirectory));
                    depth++;
                }
                else
                {
                    APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                        (byte)(flags & ~DidDirectory));
                    depth = depth > 0 ? depth - 1 : 0;
                    process = false;
                    leaveDirectory = true;
                }
            }

            if (process)
            {
                var name = APTR.FromPointer(anchor.Raw +
                    DosLayout.AnchorPath.PathBuffer);
                var commentResult = DOS.SetComment(
                    CString.FromPointer(name), CString.FromPointer(comment));
                var itemError = commentResult != 0 ? 0 : (int)DOS.IoErr();
                if (commentResult == 0)
                    result = DOS.RETURN_WARN;
                if (itemError != 0)
                    ioError = itemError;
                if (!quiet)
                    PrintFileName(anchor, depth, directory, commentResult == 0,
                        itemError, warningArguments);
            }
            else if (leaveDirectory)
            {
                // The source only changes indentation while leaving a
                // directory; it does not call SetComment a second time.
            }

            match = DOS.MatchNext(anchor);
        }
        while (match == 0);
        return result;
    }

    private static void PrintFileName(APTR anchor, int depth, bool directory,
        bool failed, int itemError, APTR warningArguments)
    {
        for (var index = 0; index < depth; index++)
            DOS.PutStr("     ");
        if (!directory)
            DOS.PutStr("   ");
        var name = APTR.FromPointer(anchor.Raw +
            DosLayout.AnchorPath.Info + FileInfoBlock.FileNameOffset);
        APTR.WriteUInt32(warningArguments, 0, name.Raw);
        DOS.VPrintf("%s", warningArguments);
        if (directory)
            DOS.PutStr(" (dir)");
        if (failed)
        {
            DOS.PrintFault((DOS.Error)itemError, "..error");
        }
        else
        {
            DOS.PutStr("..done\n");
        }
    }

    private static bool CopyComment(APTR source, APTR destination)
    {
        var truncated = false;
        var length = 0u;
        if (source.IsNotNull)
        {
            while (APTR.ReadUInt8(source, unchecked((int)length)) != 0)
            {
                if (length + 1 >= CommentBytes)
                {
                    truncated = true;
                    break;
                }
                APTR.WriteUInt8(destination, unchecked((int)length),
                    APTR.ReadUInt8(source, unchecked((int)length)));
                length++;
            }
        }
        APTR.WriteUInt8(destination, unchecked((int)length), 0);
        return truncated;
    }
}
