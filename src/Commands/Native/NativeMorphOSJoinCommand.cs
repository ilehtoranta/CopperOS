using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 Join command frontend.  The public DOS matcher and the
/// source-observed append loop are reused, while all parser, matcher snapshots,
/// streams and temporary buffers remain owned by this invocation.
///
/// Exact packed diagnostics, handler-specific no-match policy and the classic
/// Workbench body still require their own reference captures.
/// </summary>
public static class NativeMorphOSJoinCommand
{
    public const string Template = "FILE/M/A,AS=TO/K/A";
    public const uint ResultCount = 2;

    private const uint ClassifierOffset = 0;
    private const uint AnchorOffset = 284; // Align(AnchorPath.Size).
    private const uint PathOffset = AnchorOffset + DosLayout.AnchorPath.Size + 2048;
    private const uint FibOffset = PathOffset + 2048;
    private const uint WorkspaceBytes = FibOffset + FileInfoBlock.SizeInBytes;
    private const uint BufferBytes = 262144;
    private const uint CtrlCMask = 1u << 12;

    /// <summary>Resident fixture allocation size for the command workspace.</summary>
    public const uint WorkspaceAllocationBytes = WorkspaceBytes;

    /// <summary>Resident fixture allocation size for one append buffer.</summary>
    public const uint TransferBufferBytes = BufferBytes;

    /// <summary>
    /// Joins FILE sources into the new TO file through public DOS calls. The
    /// caller owns DOS startup and publishes the returned result/error.
    /// </summary>
    public static int Run()
    {
        if (DOS.DOSLibraryBase.IsNull)
        {
            DOS.SetIoErr(DOS.Error.ObjectWrongType);
            return DOS.RETURN_FAIL;
        }

        var workspace = Exec.AllocMem(WorkspaceBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (workspace.IsNull)
        {
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            return DOS.RETURN_FAIL;
        }

        var arguments = default(NativeCommandArguments);
        var result = DOS.RETURN_FAIL;
        var ioError = (int)DOS.Error.BadTemplate;
        BPTR destination = BPTR.Null;
        APTR files = APTR.Null;
        APTR to = APTR.Null;
        var destinationCreated = false;

        if (NativeCommandArguments.TryRead(Template, ResultCount,
                out arguments) &&
            arguments.TryGetResult(0, out var fileVector) && fileVector != 0 &&
            arguments.TryGetResult(1, out var toValue) && toValue != 0 &&
            APTR.ReadUInt32(APTR.FromPointer(fileVector), 0) != 0)
        {
            files = APTR.FromPointer(fileVector);
            to = APTR.FromPointer(toValue);
            destination = DOS.OpenRaw(CString.FromPointer(to.Raw),
                DOS.FileMode.NewFile);
            if (destination.IsNotNull)
            {
                destinationCreated = true;
                result = JoinSources(files, to, destination,
                    APTR.FromPointer(workspace.Raw + ClassifierOffset),
                    APTR.FromPointer(workspace.Raw + AnchorOffset),
                    APTR.FromPointer(workspace.Raw + PathOffset),
                    APTR.FromPointer(workspace.Raw + FibOffset),
                    out ioError);
                var saved = ioError;
                DOS.Close(destination);
                destination = BPTR.Null;
                if (result != DOS.RETURN_OK && destinationCreated)
                {
                    DOS.DeleteFile(CString.FromPointer(to.Raw));
                    DOS.SetIoErr((DOS.Error)saved);
                }
                ioError = saved;
            }
            else
            {
                ioError = (int)DOS.IoErr();
                result = DOS.RETURN_FAIL;
            }
        }
        else
        {
            result = arguments.ReturnLevel;
            ioError = arguments.IoError;
        }

        arguments.Release();
        var cleanupError = ioError;
        Exec.FreeMem(workspace, WorkspaceBytes);
        DOS.SetIoErr((DOS.Error)cleanupError);
        return result;
    }

    private static int JoinSources(APTR files, APTR destinationName,
        BPTR destination, APTR classifier, APTR anchor, APTR path, APTR fib,
        out int ioError)
    {
        ioError = 0;
        var offset = 0;
        var source = APTR.ReadUInt32(files, offset);
        while (source != 0)
        {
            var sourceName = APTR.FromPointer(source);
            var wildcard = NativeMorphOSCopyPatternClassifier.IsMatchPattern(
                CString.FromPointer(source), classifier);
            if (wildcard > 0)
            {
                if (!AppendMatches(sourceName, destination, anchor, path, fib,
                        destinationName, out ioError))
                    return DOS.RETURN_FAIL;
            }
            else if (!AppendOne(sourceName, destination, out ioError))
            {
                return DOS.RETURN_FAIL;
            }
            offset += 4;
            source = APTR.ReadUInt32(files, offset);
        }
        return DOS.RETURN_OK;
    }

    private static bool AppendMatches(APTR source, BPTR destination,
        APTR anchor, APTR path, APTR fib, APTR destinationName,
        out int ioError)
    {
        ioError = 0;
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, CtrlCMask);
        APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength, 2048);
        var match = DOS.MatchFirst(CString.FromPointer(source.Raw), anchor);
        var failed = false;
        while (match == 0)
        {
            Copy(anchor.Raw + (uint)DosLayout.AnchorPath.PathBuffer,
                path.Raw, 2048);
            Copy(anchor.Raw + (uint)DosLayout.AnchorPath.Info,
                fib.Raw, FileInfoBlock.SizeInBytes);
            if (FileInfoBlock.GetDirEntryType(fib.Raw) <= 0 &&
                !AppendOne(path, destination, out ioError))
            {
                failed = true;
                break;
            }
            match = DOS.MatchNext(anchor);
        }
        var matchError = (int)DOS.IoErr();
        DOS.MatchEnd(anchor);
        if (failed) return false;
        if (match != 0 && match != (int)DOS.Error.NoMoreEntries)
        {
            ioError = matchError;
            return false;
        }
        return true;
    }

    private static bool AppendOne(APTR source, BPTR destination,
        out int ioError)
    {
        ioError = 0;
        var input = DOS.OpenRaw(CString.FromPointer(source.Raw),
            DOS.FileMode.OldFile);
        if (input.IsNull)
        {
            ioError = (int)DOS.IoErr();
            return false;
        }

        var buffer = Exec.AllocMem(BufferBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (buffer.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            var saved = ioError;
            DOS.Close(input);
            DOS.SetIoErr((DOS.Error)saved);
            return false;
        }

        var result = NativeMorphOSJoinAppendLoop.Run(destination, input,
            buffer, unchecked((int)BufferBytes), out ioError);
        var savedError = ioError;
        DOS.Close(input);
        Exec.FreeMem(buffer, BufferBytes);
        DOS.SetIoErr((DOS.Error)savedError);
        return result == DOS.RETURN_OK;
    }

    private static void Copy(uint source, uint destination, uint bytes)
    {
        for (var offset = 0u; offset < bytes; offset++)
            APTR.WriteUInt8(APTR.FromPointer(destination), unchecked((int)offset),
                APTR.ReadUInt8(APTR.FromPointer(source), unchecked((int)offset)));
    }
}
