using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS Exe2Arc command owner. It composes the bounded parser, input/FIB/
/// scratch lease, scanner dispatch, output naming and extractor components.
/// Archive payloads are copied as data; the input is never loaded or executed.
/// Exact original diagnostics and final IoErr precedence remain qualification
/// work, so this body records only bounded DOS errors before cleanup.
/// </summary>
public static class NativeMorphOSExe2ArcCommand
{
    private const uint ScratchBytes = 102400;
    private const uint PathBytes = 512;
    private const uint WorkspaceBytes = ScratchBytes + PathBytes + PathBytes;
    private const uint GeneratedOffset = ScratchBytes;
    private const uint FallbackOffset = ScratchBytes + PathBytes;

    public static int Run(APTR dosBase, out int ioError)
    {
        ioError = 0;
        var arguments = default(NativeCommandArguments);
        if (NativeExe2ArcArgumentGate.Read(out arguments, out ioError) !=
            DOS.RETURN_OK)
        {
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var input = BPTR.Null;
        var output = BPTR.Null;
        var fib = APTR.Null;
        var workspace = APTR.Null;
        var selectedPath = APTR.Null;
        var selectedError = 0;
        var outputOpened = false;
        var diagnosticIssued = false;
        var result = DOS.RETURN_FAIL;
        do
        {
            if (!arguments.TryGetResult(NativeExe2ArcArgumentGate.From,
                    out var from) || from == 0 ||
                !arguments.TryGetResult(NativeExe2ArcArgumentGate.To,
                    out var to) ||
                !arguments.TryGetResult(NativeExe2ArcArgumentGate.Type,
                    out var type))
            {
                selectedError = (int)DOS.Error.BadTemplate;
                break;
            }

            workspace = Exec.AllocMem(WorkspaceBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (workspace.IsNull)
            {
                DOS.PutStr("Failed to open temporary buffer.\n");
                diagnosticIssued = true;
                break;
            }

            DOS.DOSLibraryBase = dosBase;

            var outputIo = new NativeExe2ArcOutputIo(workspace);
            if (!Exe2ArcTypeSelection.TrySelect(ref outputIo,
                    APTR.FromPointer(type), out var requested))
            {
                var unknown = Exe2ArcResultPolicy.UnknownType();
                selectedError = unknown.IoError;
                PrintUnknownType(APTR.FromPointer(type), dosBase);
                break;
            }

            input = DOS.OpenRaw(CString.FromPointer(from), DOS.FileMode.OldFile);
            if (input.IsNull)
            {
                selectedError = (int)DOS.IoErr();
                DOS.PutStr("Failed to open input file.\n");
                diagnosticIssued = true;
                break;
            }

            fib = DOS.AllocDosObject((uint)DosObjectType.FileInfoBlock,
                APTR.Null);
            if (fib.IsNull)
            {
                selectedError = (int)DOS.IoErr();
                DOS.PutStr("Failed to open file information object.\n");
                diagnosticIssued = true;
                break;
            }
            if (DOS.ExamineFH(input, fib) == 0)
            {
                selectedError = (int)DOS.IoErr();
                DOS.PutStr("Failed to examine file.\n");
                diagnosticIssued = true;
                break;
            }
            var fileLength = APTR.ReadUInt32(fib, FileInfoBlock.SizeOffset);
            if (fileLength > int.MaxValue)
            {
                selectedError = (int)DOS.Error.LineTooLong;
                break;
            }

            var scannerIo = new NativeExe2ArcIo(workspace);
            var scan = Exe2ArcScannerSelection.Scan(ref scannerIo, input,
                workspace, ScratchBytes, fileLength, requested,
                out var selected, out var archiveOffset,
                out var payloadLength, out var correction, out var observation);
            selectedError = observation.IoErrCaptured ? observation.IoError : 0;
            var scanDecision = Exe2ArcResultPolicy.ForScan(scan, selectedError);
            if (scanDecision.ReturnLevel != DOS.RETURN_OK)
            {
                diagnosticIssued = PrintScanFailure(requested,
                    APTR.FromPointer(type), scan, selectedError, observation);
                break;
            }

            DOS.DOSLibraryBase = dosBase;
            DOS.PutStr(" Found.\n");

            var generated = APTR.FromPointer(workspace.Raw + GeneratedOffset);
            var fallback = APTR.FromPointer(workspace.Raw + FallbackOffset);
            var extension = APTR.FromPointer(CString.ToUInt32(
                Extension(selected)));
            if (!Exe2ArcOutputName.TryBuild(ref outputIo,
                    APTR.FromPointer(from), generated, PathBytes, extension,
                    out _))
            {
                selectedError = (int)DOS.Error.LineTooLong;
                break;
            }

            var basename = APTR.FromPointer(STRPTR.ToUInt32(DOS.FilePart(
                CString.FromPointer(generated.Raw))));
            var openStatus = Exe2ArcOutputSelection.TryOpen(ref outputIo,
                APTR.FromPointer(to), generated, fallback, basename,
                PathBytes, out output, out selectedPath);
            if (openStatus is Exe2ArcOutputStatus.NotOpened or
                Exe2ArcOutputStatus.InvalidBuffer)
            {
                selectedError = openStatus == Exe2ArcOutputStatus.NotOpened
                    ? (int)DOS.IoErr() : (int)DOS.Error.LineTooLong;
                DOS.PutStr("\nFailed to open output file.\n");
                diagnosticIssued = true;
                break;
            }
            outputOpened = true;

            Exe2ArcIoStatus extraction;
            uint outputBytes;
            if (selected == Exe2ArcArchiveType.Zip)
            {
                extraction = Exe2ArcZipRecordExtractor.Extract(ref scannerIo,
                    input, output, workspace, ScratchBytes, fileLength,
                    archiveOffset, correction, out outputBytes,
                    out observation);
            }
            else
            {
                extraction = Exe2ArcPayloadCopy.Copy(ref scannerIo, input,
                    output, workspace, ScratchBytes, payloadLength,
                    out observation);
                outputBytes = observation.BytesCompleted;
            }
            selectedError = observation.IoErrCaptured ? observation.IoError : 0;
            if (observation.BreakObserved)
                selectedError = observation.BreakError;
            var extractionDecision = Exe2ArcResultPolicy.ForExtraction(
                extraction, outputBytes, selectedError,
                observation.BreakObserved);
            result = extractionDecision.ReturnLevel;
            selectedError = extractionDecision.IoError;
            if (selected == Exe2ArcArchiveType.Zip)
            {
                if (extraction == Exe2ArcIoStatus.InvalidRecord)
                {
                    DOS.PutStr("Unknown or illegal data found.\n");
                    diagnosticIssued = true;
                }
                else if (extraction == Exe2ArcIoStatus.IoStopped &&
                    observation.Stage == Exe2ArcIoStage.PayloadRead)
                {
                    DOS.PutStr("Unexpected end of data.\n");
                    diagnosticIssued = true;
                }
            }
            if (extractionDecision.ReportSaved && selectedPath.IsNotNull)
            {
                // Keep the printf argument vector invocation-owned. The
                // source reports the extractor's count, which for ZIP can be
                // larger than the actual emitted record bytes.
                APTR.WriteUInt32(workspace, 0, outputBytes);
                APTR.WriteUInt32(workspace, 4, selectedPath.Raw);
                DOS.VPrintf("Saved %lu byte to %s\n", workspace);
                DOS.PutStr("Be careful and test the file for correctness.\n");
            }
        }
        while (false);

        if (outputOpened)
        {
            DOS.Close(output);
            if (result != DOS.RETURN_OK && selectedPath.IsNotNull)
                DOS.DeleteFile(CString.FromPointer(selectedPath.Raw));
        }
        if (input.IsNotNull)
            DOS.Close(input);
        if (fib.IsNotNull)
            DOS.FreeDosObject((uint)DosObjectType.FileInfoBlock, fib);
        if (workspace.IsNotNull)
            Exec.FreeMem(workspace, WorkspaceBytes);
        arguments.Release();
        ioError = selectedError;
        if (result != DOS.RETURN_OK && selectedError != 0 &&
            !diagnosticIssued)
            DOS.PrintFault((DOS.Error)selectedError, CString.FromPointer(0));
        DOS.SetIoErr((DOS.Error)selectedError);
        return result;
    }

    private static CString Extension(Exe2ArcArchiveType type) => type switch
    {
        Exe2ArcArchiveType.Zip => CString.FromLiteral("zip"),
        Exe2ArcArchiveType.Ace => CString.FromLiteral("ace"),
        Exe2ArcArchiveType.Rar => CString.FromLiteral("rar"),
        Exe2ArcArchiveType.Cabinet => CString.FromLiteral("cab"),
        Exe2ArcArchiveType.Arj => CString.FromLiteral("arj"),
        Exe2ArcArchiveType.Lha => CString.FromLiteral("lha"),
        Exe2ArcArchiveType.Lzh => CString.FromLiteral("lzh"),
        _ => CString.FromLiteral("arc"),
    };

    private static bool PrintScanFailure(Exe2ArcArchiveType requested,
        APTR type, Exe2ArcIoStatus status, int ioError,
        Exe2ArcIoObservation observation)
    {
        if (status == Exe2ArcIoStatus.NoMatch)
        {
            if (requested == Exe2ArcArchiveType.Unspecified)
            {
                DOS.PutStr("\r\033[KDid not find archive data.\n");
                return true;
            }

            DOS.PutStr("\r\033[KDid not find archive of type '");
            if (type.IsNotNull)
                DOS.VPrintf("%s", type);
            DOS.PutStr("'.\nMaybe it is an archive of another type?\n");
            PrintValidTypes();
            return true;
        }

        if (status == Exe2ArcIoStatus.OffsetZero ||
            status == Exe2ArcIoStatus.MatchedNotPositioned)
        {
            DOS.PutStr("\r\033[KArchive position is unsafe.\n");
            return true;
        }

        if (observation.Stage == Exe2ArcIoStage.DispatchSeek)
        {
            DOS.PutStr("Failed to seek to file start.\n");
            return true;
        }

        if (ioError != 0)
        {
            DOS.PrintFault((DOS.Error)ioError,
                CString.FromPointer(0));
            return true;
        }

        return false;
    }

    private static void PrintUnknownType(APTR type, APTR dosBase)
    {
        DOS.DOSLibraryBase = dosBase;
        DOS.PutStr("\r\033[KDid not find archive of type '");
        if (type.IsNotNull)
            DOS.VPrintf("%s", type);
        DOS.PutStr("'.\n");
        PrintValidTypes();
    }

    private static void PrintValidTypes() => DOS.PutStr(
        "Valid types are: zip (Zip), ace (Ace), rar (Rar), " +
        "cab (Cabinet), arj (Arj), lha (LhA), lzh (Amiga-LhA)\n");
}
