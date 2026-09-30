using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// The MorphOS Copy ReadArgs and mode-validation boundary. It deliberately
/// stops before matcher, filesystem, metadata, requester, and output work.
/// </summary>
public static class NativeMorphOSCopyArgumentGate
{
    // c/copy/copy.c PARAM with USE_ALWAYSVERBOSE enabled: 24 IPTR slots.
    public const uint ResultCount = 24;
    public const uint From = 0, To = 1, Pattern = 2, Buffer = 3, All = 4,
        Direct = 5, Clone = 6, Dates = 7, NoProtection = 8, ProtectionX = 9,
        Comment = 10, Quiet = 11, NoRequesters = 12, ErrorWarning = 13,
        MakeDirectory = 14, Move = 15, Delete = 16, HardLink = 17,
        SoftLink = 18, ForceLink = 19, ForceDelete = 20, ForceOverwrite = 21,
        DontOverwrite = 22, CompatibilityForce = 23;

    public const string Template =
        "FROM/M,TO,PAT=PATTERN/K,BUF=BUFFER/K/N,ALL/S,DIRECT/S,CLONE/S," +
        "DATES/S,NOPRO/S,PROX/S,COM=COMMENT/S,QUIET/S,NOREQ/S,ERRWARN/S," +
        "MAKEDIR/S,MOVE/S,DELETE/S,HARD=HARDLINK/S,SOFT=SOFTLINK/S," +
        "FOLNK=FORCELINK/S,FODEL=FORCEDELETE/S,FOOVR=FORCEOVERWRITE/S," +
        "DONTOVR=DONTOVERWRITE/S,FORCE/S";

    /// <summary>
    /// Parses exactly the source template and applies its no-I/O selection
    /// gate. A successful result only admits a later Copy operation; it never
    /// claims a file, directory, link, or metadata operation was performed.
    /// </summary>
    public static int Run(out int mode, out uint flags, out int ioError)
    {
        var result = Read(out var arguments, out mode, out flags, out ioError);
        arguments.Release();
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    /// <summary>
    /// Retains the successful ReadArgs lease for command execution. The caller
    /// must release it, including on a mode-admission failure after parsing.
    /// FROM, TO and PATTERN pointers must not outlive this lease.
    /// </summary>
    public static int Read(out NativeCommandArguments arguments,
        out int mode, out uint flags, out int ioError)
    {
        mode = NativeMorphOSCopyModeSelection.Copy;
        flags = 0;
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount, out arguments))
        {
            ioError = arguments.IoError;
            return arguments.ReturnLevel;
        }
        return Validate(ref arguments, out mode, out flags, out ioError);
    }

    /// <summary>Validates a live parse without copying or releasing its lease.</summary>
    public static int Validate(ref NativeCommandArguments arguments,
        out int mode, out uint flags, out int ioError)
    {
        var result = Evaluate(ref arguments, out mode, out flags, out ioError);
        DOS.SetIoErr((DOS.Error)ioError);
        return result;
    }

    /// <summary>Computes admission without publishing an error before source setup.</summary>
    public static int Evaluate(ref NativeCommandArguments arguments,
        out int mode, out uint flags, out int ioError)
    {
        mode = NativeMorphOSCopyModeSelection.Copy;
        flags = 0;
        ioError = 0;
        var result = DOS.RETURN_OK;
        do
        {
            if (!TryGet(ref arguments, From, out var from) ||
                !TryGet(ref arguments, To, out var to) ||
                !TryGet(ref arguments, Pattern, out var pattern) ||
                !TryGet(ref arguments, Buffer, out _) ||
                !TryGet(ref arguments, All, out var all) ||
                !TryGet(ref arguments, Direct, out var direct) ||
                !TryGet(ref arguments, Clone, out var clone) ||
                !TryGet(ref arguments, Dates, out var dates) ||
                !TryGet(ref arguments, NoProtection, out var noProtection) ||
                !TryGet(ref arguments, ProtectionX, out var protectionX) ||
                !TryGet(ref arguments, Comment, out var comment) ||
                !TryGet(ref arguments, Quiet, out _) ||
                !TryGet(ref arguments, NoRequesters, out _) ||
                !TryGet(ref arguments, ErrorWarning, out _) ||
                !TryGet(ref arguments, MakeDirectory, out var makeDirectory) ||
                !TryGet(ref arguments, Move, out var move) ||
                !TryGet(ref arguments, Delete, out var delete) ||
                !TryGet(ref arguments, HardLink, out var hardLink) ||
                !TryGet(ref arguments, SoftLink, out var softLink) ||
                !TryGet(ref arguments, ForceLink, out var forceLink) ||
                !TryGet(ref arguments, ForceDelete, out var forceDelete) ||
                !TryGet(ref arguments, ForceOverwrite, out var forceOverwrite) ||
                !TryGet(ref arguments, DontOverwrite, out var dontOverwrite) ||
                !TryGet(ref arguments, CompatibilityForce, out var compatibilityForce))
            {
                ioError = (int)DOS.Error.BadTemplate;
                result = DOS.RETURN_FAIL;
                break;
            }

            var inputOmitted = from == 0;
            var inputCount = CountForModeGate(from, to, delete,
                makeDirectory, out var positionalTarget);
            var controls =
                (delete != 0 ? NativeMorphOSCopyModeSelection.DeleteMode : 0u) |
                (move != 0 ? NativeMorphOSCopyModeSelection.MoveMode : 0u) |
                (makeDirectory != 0 ? NativeMorphOSCopyModeSelection.MakeDirectory : 0u) |
                (hardLink != 0 ? NativeMorphOSCopyModeSelection.HardLink : 0u) |
                (softLink != 0 ? NativeMorphOSCopyModeSelection.SoftLinkMode : 0u) |
                (all != 0 ? NativeMorphOSCopyModeSelection.All : 0u) |
                (direct != 0 ? NativeMorphOSCopyModeSelection.Direct : 0u) |
                (inputOmitted ? NativeMorphOSCopyModeSelection.InputOmitted : 0u) |
                (to != 0 || positionalTarget ? NativeMorphOSCopyModeSelection.HasTarget : 0u) |
                (pattern != 0 ? NativeMorphOSCopyModeSelection.HasPattern : 0u) |
                (HasRestrictedDirectOption(all, clone, dates, noProtection,
                    protectionX, comment, forceLink, forceDelete,
                    forceOverwrite, dontOverwrite)
                    ? NativeMorphOSCopyModeSelection.RestrictedDirectOption : 0u) |
                (compatibilityForce != 0
                    ? NativeMorphOSCopyModeSelection.CompatibilityForce : 0u) |
                (forceOverwrite != 0
                    ? NativeMorphOSCopyModeSelection.ForceOverwrite : 0u) |
                (dontOverwrite != 0
                    ? NativeMorphOSCopyModeSelection.DontOverwrite : 0u);
            if (!NativeMorphOSCopyModeSelection.TrySelect(controls, inputCount,
                    out mode, out flags))
            {
                ioError = (int)DOS.Error.TooManyArguments;
                result = DOS.RETURN_FAIL;
            }
        }
        while (false);


        return result;
    }

    private static bool TryGet(ref NativeCommandArguments arguments, uint index,
        out uint value) => arguments.TryGetResult(index, out value);

    // The source only needs to distinguish no usable input, one input, and
    // multiple inputs at this stage. Looking through the third /M pointer is
    // enough after its own COPY/MOVE/LINK positional TO normalization.
    private static int CountForModeGate(uint from, uint to, uint delete,
        uint makeDirectory,
        out bool positionalTarget)
    {
        positionalTarget = false;
        if (from == 0) return 0;
        var first = APTR.ReadUInt32(APTR.FromPointer(from), 0);
        if (first == 0) return 0;
        var second = APTR.ReadUInt32(APTR.FromPointer(from), 4);
        if (second == 0) return 1;
        // Source moves the final /M argument to TO for COPY, MOVE and LINK.
        if (to == 0 && delete == 0 && makeDirectory == 0)
        {
            positionalTarget = true;
            return APTR.ReadUInt32(APTR.FromPointer(from), 8) == 0 ? 1 : 2;
        }
        return 2;
    }

    private static bool HasRestrictedDirectOption(uint all, uint clone,
        uint dates, uint noProtection, uint protectionX, uint comment,
        uint forceLink, uint forceDelete, uint forceOverwrite,
        uint dontOverwrite) => all != 0 || clone != 0 || dates != 0 ||
            noProtection != 0 || protectionX != 0 || comment != 0 ||
            forceLink != 0 || forceDelete != 0 || forceOverwrite != 0 ||
            dontOverwrite != 0;
}
