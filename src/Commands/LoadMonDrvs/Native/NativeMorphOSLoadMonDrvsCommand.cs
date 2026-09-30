using Amiga;
using CopperSharp.Compiler;
using CopperSharp.Sdk.Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// MorphOS 3.20 LoadMonDrvs candidate.  The packed command does not expose a
/// plaintext template; the documented FROM/K,EXCEPT boundary is retained while
/// all directory, segment and resident state remains guest-owned.
/// </summary>
public static class NativeMorphOSLoadMonDrvsCommand
{
    public const string Template = "FROM/K,EXCEPT";
    public const uint ResultCount = 2;

    private const uint DosVersion = 37;
    private const uint PathBytes = 1024;
    private const uint PatternOffset =
        ((uint)DosLayout.AnchorPath.Size + 3u) & ~3u;
    public const uint WorkspaceBytes = PatternOffset + PathBytes * 2u;
    private const int AChainLockOffset = 8;
    private const ushort ResidentMatchWord = 0x4AFC;

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (!NativeCommandArguments.TryRead(Template, ResultCount,
                out var arguments))
        {
            ioError = arguments.IoError;
            DOS.PrintFault((DOS.Error)ioError, CString.FromPointer(0));
            return arguments.ReturnLevel;
        }

        var result = DOS.RETURN_OK;
        var error = 0;
        var workspace = Exec.AllocMem(WorkspaceBytes,
            Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (workspace.IsNull)
        {
            arguments.Release();
            ioError = (int)DOS.Error.NoFreeStore;
            DOS.SetIoErr(DOS.Error.NoFreeStore);
            DOS.PrintFault(DOS.Error.NoFreeStore, CString.FromPointer(0));
            return DOS.RETURN_FAIL;
        }

        var anchor = workspace;
        var path = APTR.FromPointer(workspace.Raw + PatternOffset);
        var pattern = APTR.FromPointer(workspace.Raw + PatternOffset +
            PathBytes);
        var from = APTR.Null;
        var except = APTR.Null;
        var matchOwned = false;

        if (arguments.TryGetResult(0, out var fromRaw) && fromRaw != 0)
            from = APTR.FromPointer(fromRaw);
        if (arguments.TryGetResult(1, out var exceptRaw) && exceptRaw != 0)
            except = APTR.FromPointer(exceptRaw);

        if (from.IsNull)
            CopyCString(path, CString.FromLiteral("DEVS:Monitors"));
        else if (!CopyCString(path, from, PathBytes))
            error = (int)DOS.Error.LineTooLong;

        if (error == 0 && DOS.AddPart(CString.FromPointer(path.Raw),
                CString.FromLiteral("#?"), PathBytes) == 0)
            error = (int)DOS.IoErr();

        if (error == 0)
        {
            CopyCString(pattern, path, PathBytes);
            InitializeAnchor(anchor);
            var match = DOS.MatchFirst(CString.FromPointer(pattern.Raw),
                anchor);
            matchOwned = true;
            while (match == 0)
            {
                if (!ProcessMatch(anchor, path, except, ref error))
                {
                    result = DOS.RETURN_WARN;
                    break;
                }

                match = DOS.MatchNext(anchor);
            }

            if (match != 0 && match != (int)DOS.Error.NoMoreEntries &&
                error == 0)
            {
                error = match;
                result = DOS.RETURN_WARN;
            }
        }

        if (matchOwned)
            DOS.MatchEnd(anchor);

        Exec.FreeMem(workspace, WorkspaceBytes);
        arguments.Release();
        ioError = error;
        DOS.SetIoErr((DOS.Error)ioError);
        if (error != 0)
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
        return error == 0 ? result : result == DOS.RETURN_OK
            ? DOS.RETURN_WARN : result;
    }

    private static bool ProcessMatch(APTR anchor, APTR path, APTR except,
        ref int error)
    {
        var info = APTR.FromPointer(anchor.Raw +
            (uint)DosLayout.AnchorPath.Info);
        if (FileInfoBlock.GetDirEntryType(info.Raw) > 0)
            return true;

        var fileName = APTR.FromPointer(info.Raw +
            (uint)FileInfoBlock.FileNameOffset);
        if (except.IsNotNull && EqualNoCase(fileName, except))
            return true;

        var current = APTR.FromPointer(APTR.ReadUInt32(anchor,
            DosLayout.AnchorPath.Current));
        if (current.IsNull)
            return true;

        var lock_ = BPTR.FromRaw(APTR.ReadUInt32(current,
            AChainLockOffset));
        if (DOS.NameFromLock(lock_, path, (int)PathBytes) == 0)
        {
            error = (int)DOS.IoErr();
            return false;
        }
        if (DOS.AddPart(CString.FromPointer(path.Raw),
                CString.FromPointer(fileName.Raw), PathBytes) == 0)
        {
            error = (int)DOS.IoErr();
            return false;
        }

        var segment = NativeMorphOSLoadMonDrvsDosRaw.LoadSegRaw(
            CString.FromPointer(path.Raw));
        if (segment.IsNull)
            return true;

        var resident = FindResident(segment.Raw);
        if (resident.IsNotNull && Exec.InitResident(resident, segment).IsNotNull)
            return true;

        NativeMorphOSLoadMonDrvsDosRaw.UnLoadSegRaw(segment);
        error = (int)DOS.IoErr();
        if (error == 0)
            error = (int)DOS.Error.ObjectNotFound;
        return false;
    }

    private static void InitializeAnchor(APTR anchor)
    {
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Base, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.Current, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.BreakBits, 0);
        APTR.WriteUInt32(anchor, DosLayout.AnchorPath.FoundBreak, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Flags, 0);
        APTR.WriteUInt8(anchor, DosLayout.AnchorPath.Reserved, 0);
        APTR.WriteUInt16(anchor, DosLayout.AnchorPath.StringLength,
            unchecked((ushort)PathBytes));
    }

    private static APTR FindResident(uint segmentRaw)
    {
        var currentSegment = segmentRaw;
        var fastSegment = segmentRaw;
        var cycleCheckEnabled = true;
        while (currentSegment != 0)
        {
            if (currentSegment > (uint.MaxValue >> 2))
                return APTR.Null;
            var memoryRaw = currentSegment << 2;
            if (memoryRaw < 4)
                return APTR.Null;
            var memory = APTR.FromPointer(memoryRaw);
            if (Exec.TypeOfMem(APTR.FromPointer(memoryRaw - 4)) == 0 ||
                Exec.TypeOfMem(memory) == 0)
                return APTR.Null;
            var longWords = APTR.ReadUInt32(memory, -4);
            if (longWords < 2 || longWords > uint.MaxValue / 4)
                return APTR.Null;
            var segmentBytes = longWords * 4;
            if (memoryRaw > uint.MaxValue - segmentBytes)
                return APTR.Null;
            var segmentEnd = memoryRaw + segmentBytes;
            if (segmentEnd <= memoryRaw ||
                Exec.TypeOfMem(APTR.FromPointer(segmentEnd - 1)) == 0)
                return APTR.Null;

            // A Resident needs its match word and match-tag pointer. Keep the
            // full six-byte read inside the declared HUNK.
            var offset = 4u;
            while (offset <= segmentBytes - 6u)
            {
                var resident = APTR.FromPointer(memory.Raw + offset);
                if (APTR.ReadUInt16(resident,
                        ExecLayout.Resident.MatchWord) == ResidentMatchWord &&
                    APTR.ReadUInt32(resident,
                        ExecLayout.Resident.MatchTag) == resident.Raw)
                    return resident;
                offset += 2;
            }

            currentSegment = APTR.ReadUInt32(memory, 0);

            // Segment lists have no public length. Preserve support for any
            // number of valid hunks while rejecting cyclic next-link chains.
            if (cycleCheckEnabled && fastSegment != 0)
            {
                if (!TryReadSegmentLink(fastSegment, out var fastNext))
                {
                    cycleCheckEnabled = false;
                }
                else
                {
                    fastSegment = fastNext;
                    if (fastSegment != 0)
                    {
                        if (!TryReadSegmentLink(fastSegment,
                                out fastNext))
                            cycleCheckEnabled = false;
                        else
                            fastSegment = fastNext;
                    }
                }
            }
            if (cycleCheckEnabled && currentSegment != 0 &&
                currentSegment == fastSegment)
                return APTR.Null;
        }
        return APTR.Null;
    }

    private static bool TryReadSegmentLink(uint segmentRaw,
        out uint nextSegment)
    {
        nextSegment = 0;
        if (segmentRaw > (uint.MaxValue >> 2))
            return false;
        var memoryRaw = segmentRaw << 2;
        if (memoryRaw == 0)
            return false;
        var memory = APTR.FromPointer(memoryRaw);
        if (Exec.TypeOfMem(memory) == 0)
            return false;
        nextSegment = APTR.ReadUInt32(memory, 0);
        return true;
    }

    private static bool EqualNoCase(APTR first, APTR second)
    {
        var index = 0;
        while (true)
        {
            var a = Fold(APTR.ReadUInt8(first, index));
            var b = Fold(APTR.ReadUInt8(second, index));
            if (a != b) return false;
            if (a == 0) return true;
            index++;
        }
    }

    private static byte Fold(byte value) => value >= (byte)'a' &&
        value <= (byte)'z' ? (byte)(value - ((byte)'a' - (byte)'A')) : value;

    private static bool CopyCString(APTR destination, APTR source,
        uint capacity)
    {
        var index = 0u;
        while (index + 1u < capacity)
        {
            var value = APTR.ReadUInt8(source, unchecked((int)index));
            APTR.WriteUInt8(destination, unchecked((int)index), value);
            index++;
            if (value == 0) return true;
        }
        APTR.WriteUInt8(destination, unchecked((int)(capacity - 1u)), 0);
        return APTR.ReadUInt8(source, unchecked((int)index)) == 0;
    }

    private static void CopyCString(APTR destination, CString source)
    {
        var pointer = APTR.FromPointer(CString.ToUInt32(source));
        CopyCString(destination, pointer, PathBytes);
    }
}

[AmigaLibrary(DOS.Name)]
internal static class NativeMorphOSLoadMonDrvsDosRaw
{
    [AmigaLvo(-150)]
    [return: M68kRegister(M68kRegister.D0)]
    public static extern BPTR LoadSegRaw(
        [M68kRegister(M68kRegister.D1)] CString name);

    [AmigaLvo(-156)]
    public static extern void UnLoadSegRaw(
        [M68kRegister(M68kRegister.D1)] BPTR segment);
}
