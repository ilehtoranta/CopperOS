using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Bounded MorphOS Type frontend for the independently observed text-mode
/// path. It keeps argument parsing, wildcard iteration, handles, buffers, and
/// ownership in one DOS invocation; HEX and diagnostic-byte parity remain
/// separate slices.
/// </summary>
public static class NativeMorphOSTypeCommand
{
    private const uint PathBytes = 512;
    private const uint InputBytes = 8192;
    private const uint OutputBytes = 8192;
    private const uint AnchorPrefixBytes = (uint)DosLayout.AnchorPath.PathBuffer;
    private const uint WorkspaceBytes = AnchorPrefixBytes + PathBytes +
        InputBytes + OutputBytes;
    private const uint CtrlCMask = 1u << 12;

    /// <summary>
    /// Parses the source-observed 50.6 outer grammar and processes every
    /// matched input in text mode. All allocations and handles are released by
    /// this invocation; the caller owns startup and final DOS teardown.
    /// </summary>
    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(
                "FROM/A/M,TO/K,OPT/K,HEX/S,NUMBER/S,NOLINE/S", 6,
                out var arguments))
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
            if (!arguments.TryGetResult(0, out var from) || from == 0 ||
                !arguments.TryGetResult(1, out var to) ||
                !arguments.TryGetResult(2, out var option) ||
                !arguments.TryGetResult(3, out var hex) ||
                !arguments.TryGetResult(4, out var number) ||
                !arguments.TryGetResult(5, out var noLine))
            {
                ioError = (int)DOS.Error.BadTemplate;
                break;
            }

            ApplyLegacyOptions(APTR.FromPointer(option), ref hex, ref number);
            if (hex != 0)
            {
                // The source-observed HEX layout needs its own byte-exact
                // renderer and vectors; do not silently produce text output.
                ioError = (int)DOS.Error.NotImplemented;
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
                var opened = DOS.Open(CString.FromPointer(to), DOS.FileMode.NewFile);
                if (!opened.HasValue)
                {
                    ioError = (int)DOS.IoErr();
                    break;
                }
                output = opened.Value;
                closeOutput = true;
            }
            else output = DOS.Output();
            if (output.IsNull)
            {
                ioError = (int)DOS.IoErr();
                break;
            }

            var anchor = workspace;
            var inputBuffer = APTR.FromPointer(workspace.Raw + AnchorPrefixBytes + PathBytes);
            var outputBuffer = APTR.FromPointer(inputBuffer.Raw + InputBytes);
            InitializeAnchor(anchor);
            var names = APTR.FromPointer(from);
            var allCompleted = true;
            for (var index = 0u;; index++)
            {
                var name = APTR.ReadUInt32(names, unchecked((int)(index * 4)));
                if (name == 0) break;
                var files = 0u;
                var matchError = DOS.MatchFirst(CString.FromPointer(name), anchor);
                while (matchError == 0)
                {
                    files++;
                    var current = APTR.FromPointer(anchor.Raw + AnchorPrefixBytes);
                    var opened = DOS.Open(CString.FromPointer(current.Raw),
                        DOS.FileMode.OldFile);
                    if (!opened.HasValue)
                    {
                        matchError = (int)DOS.IoErr();
                        break;
                    }
                    matchError = NativeMorphOSTypeTextIo.Copy(opened.Value,
                        output, number != 0, noLine != 0, inputBuffer,
                        InputBytes, outputBuffer, OutputBytes, out ioError);
                    DOS.Close(opened.Value);
                    if (matchError != DOS.RETURN_OK) break;
                    matchError = DOS.MatchNext(anchor);
                }
                DOS.MatchEnd(anchor);
                if (files == 0 && matchError == (int)DOS.Error.NoMoreEntries)
                    matchError = -1;
                if (matchError != (int)DOS.Error.NoMoreEntries)
                {
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

    private static void ApplyLegacyOptions(APTR option, ref uint hex,
        ref uint number)
    {
        if (option.IsNull) return;
        for (var index = 0u;; index++)
        {
            var value = APTR.ReadUInt8(option, unchecked((int)index));
            if (value == 0) return;
            if (value is (byte)'h' or (byte)'H') hex = uint.MaxValue;
            if (value is (byte)'n' or (byte)'N') number = uint.MaxValue;
        }
    }
}
