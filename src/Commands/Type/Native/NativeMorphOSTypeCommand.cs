using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded MorphOS Type frontend for separately observed text and HEX stream
/// layouts. It keeps argument parsing, wildcard iteration, handles, buffers,
/// and ownership in one DOS invocation; diagnostics remain a separate slice.
/// </summary>
public static class NativeMorphOSTypeCommand
{
    public const string MorphOSTemplate =
        "FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S,NOLINE/S";
    public const uint MorphOSResultCount = 6;
    public const string WorkbenchTemplate =
        "FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S";
    public const uint WorkbenchResultCount = 5;
    private const uint PathBytes = 512;
    private const uint InputBytes = 8192;
    private const uint OutputBytes = 8192;
    private const uint AnchorPrefixBytes = (uint)DosLayout.AnchorPath.PathBuffer;
    private const uint WorkspaceBytes = AnchorPrefixBytes + PathBytes +
        InputBytes + OutputBytes;
    private const uint CtrlCMask = 1u << 12;
    private const ushort ExtendedAnchorPathFlag = 0x8000;
    private const ushort ExtendedAnchorPathVersion = 50;
    private const ushort ExtendedAnchorPathRevision = 67;
    private const byte LiteralSoftLinks = 1 << 1;

    /// <summary>
    /// Parses the source-observed 50.6 outer grammar and processes every
    /// matched input in text mode. All allocations and handles are released by
    /// this invocation; the caller owns startup and final DOS teardown.
    /// </summary>
    public static int Run(out int ioError) => RunProfile(true, out ioError);

    /// <summary>
    /// Runs the classic Workbench 3.1 five-slot Type syntax candidate. The
    /// NOLINE extension is intentionally unavailable on this profile.
    /// </summary>
    public static int RunWorkbench31(out int ioError) =>
        RunProfile(false, out ioError);

    private static int RunProfile(bool morphos, out int ioError)
    {
        ioError = 0;
        var arguments = default(NativeCommandArguments);
        var parsed = false;
        if (morphos)
            parsed = NativeCommandArguments.TryRead(MorphOSTemplate,
                MorphOSResultCount, out arguments);
        else
            parsed = NativeCommandArguments.TryRead(WorkbenchTemplate,
                WorkbenchResultCount, out arguments);
        if (!parsed)
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }

        var workspace = APTR.Null;
        var output = BPTR.Null;
        var closeOutput = false;
        var result = DOS.RETURN_ERROR;
        do
        {
            var literalSoftLinks = morphos && SupportsLiteralSoftLinks();
            if (morphos && !literalSoftLinks)
            {
                ioError = (int)DOS.Error.NotImplemented;
                DOS.PrintFault((DOS.Error)ioError,
                    CString.FromPointer(0));
                break;
            }

            if (!arguments.TryGetResult(0, out var from) || from == 0 ||
                !arguments.TryGetResult(1, out var to) ||
                !arguments.TryGetResult(2, out var option) ||
                !arguments.TryGetResult(3, out var hex) ||
                !arguments.TryGetResult(4, out var number))
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            var noLine = 0u;
            var optionArgument = APTR.Null;
            if (morphos)
            {
                if (!arguments.TryGetResult(5, out noLine) ||
                    !arguments.TryGetResultSlot(5, out optionArgument))
                {
                    ioError = (int)DOS.Error.BadTemplate;
                    break;
                }
            }
            else if (!arguments.TryGetResultSlot(4, out optionArgument))
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            ApplyLegacyOptions(APTR.FromPointer(option), optionArgument, ref hex,
                ref number);
            if (hex != 0 && number != 0)
            {
                // The inspected 50.6 release source rejects this pair before
                // allocation or matching. Release parser-owned storage before
                // emitting its diagnostic, as the source-visible ownership does.
                arguments.Release();
                DOS.PutStr("Type can't do both HEX and NUMBER\n");
                result = DOS.RETURN_WARN;
                break;
            }
            workspace = Exec.AllocMem(WorkspaceBytes,
                Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
            if (workspace.IsNull)
            {
                ioError = (int)DOS.Error.NoFreeStore;
                result = DOS.RETURN_FAIL;
                break;
            }
            if (to != 0)
            {
                var opened = DOS.OpenRaw(CString.FromPointer(to), DOS.FileMode.NewFile);
                if (opened.IsNull)
                {
                    ioError = (int)DOS.IoErr();
                    DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
                    break;
                }
                output = opened;
                closeOutput = true;
            }
            else output = DOS.Output();
            if (output.IsNull)
            {
                ioError = (int)DOS.IoErr();
                DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
                break;
            }

            var anchor = workspace;
            var inputBuffer = APTR.FromPointer(workspace.Raw + AnchorPrefixBytes + PathBytes);
            var outputBuffer = APTR.FromPointer(inputBuffer.Raw + InputBytes);
            InitializeAnchor(anchor, literalSoftLinks);
            var names = APTR.FromPointer(from);
            var allCompleted = true;
            for (var index = 0u;; index++)
            {
                var nameSlot = APTR.FromPointer(names.Raw + index * 4);
                var name = APTR.ReadUInt32(nameSlot, 0);
                if (name == 0) break;
                var files = 0u;
                var matchError = DOS.MatchFirst(CString.FromPointer(name), anchor);
                while (matchError == 0)
                {
                    files++;
                    var current = APTR.FromPointer(anchor.Raw + AnchorPrefixBytes);
                    var opened = DOS.OpenRaw(CString.FromPointer(current.Raw),
                        DOS.FileMode.OldFile);
                    if (opened.IsNull)
                    {
                        matchError = (int)DOS.IoErr();
                        break;
                    }
                    var copyResult = hex != 0
                        ? NativeMorphOSTypeHexIo.Copy(opened, output,
                            inputBuffer, InputBytes, outputBuffer, OutputBytes,
                            out ioError)
                        : NativeMorphOSTypeTextIo.Copy(opened, output,
                            number != 0, noLine != 0, inputBuffer,
                            InputBytes, outputBuffer, OutputBytes, out ioError);
                    DOS.Close(opened);
                    if (copyResult != DOS.RETURN_OK)
                    {
                        matchError = ioError;
                        break;
                    }
                    matchError = DOS.MatchNext(anchor);
                }
                DOS.MatchEnd(anchor);
                if (files == 0 && matchError == (int)DOS.Error.NoMoreEntries)
                    matchError = -1;
                if (matchError != (int)DOS.Error.NoMoreEntries)
                {
                    if (matchError != (int)DOS.Error.Break)
                        DOS.VPrintf("TYPE: can't open %s\n", nameSlot);
                    if (matchError != -1)
                    {
                        DOS.PrintFault((DOS.Error)matchError,
                            CString.FromPointer(0));
                        DOS.SetIoErr((DOS.Error)matchError);
                    }
                    if (matchError != -1 && ioError == 0) ioError = matchError;
                    allCompleted = false;
                    break;
                }
            }
            result = allCompleted ? DOS.RETURN_OK : DOS.RETURN_ERROR;
        }
        while (false);

        if (closeOutput && output.IsNotNull)
            DOS.Close(output);
        if (workspace.IsNotNull)
            Exec.FreeMem(workspace, WorkspaceBytes);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    private static void InitializeAnchor(APTR anchor, bool literalSoftLinks)
    {
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Base, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Current, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, CtrlCMask);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.FoundBreak, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Reserved, 0);
        var stringLength = unchecked((ushort)PathBytes);
        if (literalSoftLinks)
        {
            stringLength |= ExtendedAnchorPathFlag;
            APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Reserved,
                LiteralSoftLinks);
        }
        APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength,
            stringLength);
    }

    private static bool SupportsLiteralSoftLinks()
    {
        var version = APTR.ReadUInt16(DOS.DOSLibraryBase,
            ExecLayout.Library.Version);
        var revision = APTR.ReadUInt16(DOS.DOSLibraryBase,
            ExecLayout.Library.Revision);
        return version > ExtendedAnchorPathVersion ||
            version == ExtendedAnchorPathVersion &&
            revision >= ExtendedAnchorPathRevision;
    }

    private static void ApplyLegacyOptions(APTR option, APTR optionArgument,
        ref uint hex, ref uint number)
    {
        if (option.IsNull) return;
        for (var index = 0u;; index++)
        {
            var value = APTR.ReadUInt8(option, unchecked((int)index));
            if (value == 0) return;
            if (value is (byte)'h' or (byte)'H') hex = uint.MaxValue;
            if (value is (byte)'n' or (byte)'N') number = uint.MaxValue;
            if (value is not (byte)'h' and not (byte)'H' and not (byte)'n' and not (byte)'N')
            {
                APTR.WriteUInt32(optionArgument, 0, value);
                DOS.VPrintf("Option '%lc' ignored\n", optionArgument);
            }
        }
    }
}
