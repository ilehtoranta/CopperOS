using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 Protect body based on the released C source.  The command
/// keeps its AnchorPath in AllocVec memory, uses DOS ReadArgs/MatchFirst/
/// MatchNext/MatchEnd/SetProtection, and carries no mutable resident state.
/// </summary>
public static class NativeMorphOSProtectCommand
{
    public const string Template = "FILE/A,FLAGS,ADD/S,SUB/S,ALL/S,QUIET/S";
    public const uint ResultCount = 6;

    private const uint PathBytes = 512;
    private const uint AnchorBytes = DosLayout.AnchorPath.Size + PathBytes;
    private const uint AllOff = (uint)(FileProtection.Read |
        FileProtection.Write | FileProtection.Delete | FileProtection.Execute);
    private const byte DoDirectory = (byte)AnchorPathFlags.DoDirectory;
    private const byte DidDirectory = (byte)AnchorPathFlags.DidDirectory;
    private const uint CtrlCMask = 1u << 12;

    public static int Run(out int ioError)
    {
        ioError = 0;
        var anchor = Exec.AllocVec(AnchorBytes,
            (uint)(Exec.MemoryFlags.Any | Exec.MemoryFlags.Clear));
        if (anchor.IsNull)
        {
            ioError = (int)DOS.Error.NoFreeStore;
            DOS.SetIoErr((DOS.Error)ioError);
            return DOS.RETURN_FAIL;
        }

        var result = DOS.RETURN_FAIL;
        var error = 0;
        var matchStarted = false;
        var protectionFailure = false;
        var failurePath = APTR.Null;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            error = arguments.IoError;
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
        }
        else
        {
            if (!arguments.TryGetResult(0, out var file) || file == 0 ||
                !arguments.TryGetResult(1, out var flags) || flags == 0 ||
                !arguments.TryGetResult(2, out var add) ||
                !arguments.TryGetResult(3, out var sub) ||
                !arguments.TryGetResult(4, out var all) ||
                !arguments.TryGetResult(5, out var quiet))
            {
                error = (int)DOS.Error.BadTemplate;
            }
            else
            {
                var addFlag = add != 0;
                var subFlag = sub != 0;
                var firstFlag = APTR.ReadUInt8(APTR.FromPointer(flags), 0);
                if (firstFlag == (byte)'+') addFlag = true;
                if (firstFlag == (byte)'-') subFlag = true;
                var allFlag = all != 0;
                var quietFlag = quiet != 0;
                var flagValues = ParseFlags(APTR.FromPointer(flags),
                    out var valid);
                if (!valid)
                {
                    DOS.PutStr("Invalid flag - must be one of HSPARWED\n");
                }
                else if (addFlag && subFlag)
                {
                    DOS.PutStr("Can't specify both ADD (+) and SUB (-)\n");
                }
                else
                {
                    InitializeAnchor(anchor);
                    var match = DOS.MatchFirst(CString.FromPointer(file),
                        anchor);
                    matchStarted = true;
                    var matchFirstFailed = match != 0;
                    var indent = 0;
                    while (match == 0)
                    {
                        var fib = APTR.FromPointer(anchor.Raw +
                            (uint)DosLayout.AnchorPath.Info);
                        var directory = FileInfoBlock.GetDirEntryType(fib.Raw) >= 0;
                        var anchorFlags = APTR.ReadUInt8(anchor,
                            DosLayout.AnchorPath.Flags);
                        if (directory && (anchorFlags & DidDirectory) != 0)
                        {
                            indent = indent > 0 ? indent - 1 : 0;
                            APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                                (byte)(anchorFlags & ~DidDirectory));
                            match = DOS.MatchNext(anchor);
                            continue;
                        }

                        if (directory && allFlag)
                        {
                            APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags,
                                (byte)(anchorFlags | DoDirectory));
                            indent++;
                        }

                        var oldFlags = unchecked((uint)FileInfoBlock.GetProtection(
                            fib.Raw));
                        var newFlags = ComputeProtection(oldFlags, flagValues,
                            addFlag, subFlag);
                        var name = APTR.FromPointer(anchor.Raw +
                            (uint)DosLayout.AnchorPath.PathBuffer);
                        var success = newFlags == oldFlags ||
                            DOS.SetProtection(CString.FromPointer(name),
                                unchecked((int)newFlags)) != 0;
                        if (!success)
                        {
                            protectionFailure = true;
                            failurePath = name;
                            error = (int)DOS.IoErr();
                            if (error == 0) error = (int)DOS.Error.WriteProtected;
                            match = error;
                            break;
                        }

                        if (allFlag && !quietFlag)
                            PrintProgress(anchor, indent, directory);
                        match = DOS.MatchNext(anchor);
                    }

                    if (match == (int)DOS.Error.NoMoreEntries)
                    {
                        result = DOS.RETURN_OK;
                        error = 0;
                    }
                    else if (match == (int)DOS.Error.Break)
                    {
                        result = DOS.RETURN_WARN;
                        error = match;
                    }
                    else
                    {
                        result = DOS.RETURN_FAIL;
                        if (error == 0) error = match;
                        if (protectionFailure && failurePath.IsNotNull)
                        {
                            DOS.PutStr("Can't set protection for ");
                            DOS.PutStr(CString.FromPointer(failurePath));
                            DOS.PutStr(" - ");
                        }
                        DOS.PrintFault((DOS.Error)error,
                            matchFirstFailed ? "Protect" : CString.FromPointer(0));
                    }
                }
            }
            arguments.Release();
        }

        if (matchStarted) DOS.MatchEnd(anchor);
        Exec.FreeVec(anchor);
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

    private static uint ParseFlags(APTR flags, out bool valid)
    {
        var value = AllOff;
        valid = true;
        var first = APTR.ReadUInt8(flags, 0);
        var offset = 0;
        if (first == (byte)'+' || first == (byte)'-') offset = 1;
        for (var index = offset;; index++)
        {
            var current = APTR.ReadUInt8(flags, index);
            if (current == 0) break;
            if (current >= (byte)'a' && current <= (byte)'z')
                current = (byte)(current - ('a' - 'A'));
            switch (current)
            {
                case (byte)'R': value &= ~(uint)FileProtection.Read; break;
                case (byte)'W': value &= ~(uint)FileProtection.Write; break;
                case (byte)'D': value &= ~(uint)FileProtection.Delete; break;
                case (byte)'E': value &= ~(uint)FileProtection.Execute; break;
                case (byte)'A': value |= (uint)FileProtection.Archive; break;
                case (byte)'S': value |= (uint)FileProtection.Script; break;
                case (byte)'P': value |= (uint)FileProtection.Pure; break;
                case (byte)'H': value |= 1u << 7; break;
                default: valid = false; break;
            }
        }
        return value;
    }

    private static uint ComputeProtection(uint oldFlags, uint flags,
        bool add, bool sub)
    {
        if (flags != AllOff)
        {
            if (add)
                return ((~(~oldFlags | ~flags) & AllOff) |
                    ((oldFlags | flags) & ~AllOff));
            if (sub)
                return (((oldFlags | ~flags) & AllOff) |
                    ((oldFlags & ~flags) & ~AllOff));
            return (oldFlags & ~0xffu) | flags;
        }
        return !add && !sub ? AllOff : oldFlags;
    }

    private static void PrintProgress(APTR anchor, int indent, bool directory)
    {
        for (var index = 0; index < indent; index++) DOS.PutStr("     ");
        if (!directory) DOS.PutStr("   ");
        var name = APTR.FromPointer(anchor.Raw +
            (uint)DosLayout.AnchorPath.Info +
            (uint)FileInfoBlock.FileNameOffset);
        DOS.PutStr(CString.FromPointer(name));
        if (directory) DOS.PutStr(" (dir)");
        DOS.PutStr("..done\n");
    }
}
