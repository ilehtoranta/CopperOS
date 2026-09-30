using Amiga;

namespace CopperOS.Commands.Native;

/// <summary>
/// Workbench Rename profile through public DOS. The caller owns library/startup
/// lifetime. Search and path safety normalizations are documented in Rename.md.
/// Native execution and complete profile qualification remain open.
/// </summary>
public static class Workbench31RenameCommand
{
    private struct State
    {
        public APTR Arguments, Anchor, Source, Destination, RdArgs;
        public BPTR DestinationLock;
        public bool SearchAttempted;
    }

    public static int Run(out int ioError)
    {
        ioError = 0;
        if (DOS.CheckSignal(0x1000) != 0)
        {
            DOS.PrintFault((DOS.Error)304, CString.FromPointer(0));
            ioError = (int)DOS.IoErr();
            return DOS.RETURN_FAIL;
        }
        State state = default;
        var error = 0;
        var result = DOS.RETURN_FAIL;
        do
        {
            // Reuse the original otherwise-unused 80-byte block for private
            // result/format cells. Keep all strings and matcher buffers separate.
            state.Arguments = Exec.AllocVec(80, 0);
            if (state.Arguments.IsNull) { error = 103; break; }
            state.Anchor = Exec.AllocVec(538, (uint)Exec.MemoryFlags.Clear);
            if (state.Anchor.IsNull) { error = 103; break; }
            state.Source = Exec.AllocVec(256, 0);
            if (state.Source.IsNull) { error = 103; break; }
            state.Destination = Exec.AllocVec(256, (uint)Exec.MemoryFlags.Clear);
            if (state.Destination.IsNull) { error = 103; break; }
            APTR.WriteUInt32(state.Arguments, 0, 0);
            APTR.WriteUInt32(state.Arguments, 4, 0);
            APTR.WriteUInt32(state.Arguments, 8, 0);
            state.RdArgs = DOS.ReadArgs("FROM/A/M,TO=AS/A,QUIET/S", state.Arguments, APTR.Null);
            if (state.RdArgs.IsNull) { error = (int)DOS.IoErr(); break; }
            result = Execute(ref state, out error);
        }
        while (false);

        // This also ends the preflight on the original nondirectory early exit.
        // Preserve its ambient error; selected diagnostic errors are separate.
        var ambient = DOS.IoErr();
        EndSearch(ref state);
        DOS.SetIoErr(ambient);
        if (error != 0)
        {
            DOS.PrintFault((DOS.Error)error, CString.FromPointer(0));
            result = DOS.RETURN_FAIL;
        }
        if (state.RdArgs.IsNotNull) DOS.FreeArgs(state.RdArgs);
        DOS.UnLock(state.DestinationLock);
        Exec.FreeVec(state.Anchor);
        Exec.FreeVec(state.Arguments);
        Exec.FreeVec(state.Destination);
        Exec.FreeVec(state.Source);
        // Unlike the MakeDir policy, Rename observes ambient error after cleanup.
        ioError = (int)DOS.IoErr();
        return result;
    }

    private static int Execute(ref State state, out int error)
    {
        error = 0;
        var sources = APTR.ReadUInt32(state.Arguments, 0);
        var to = APTR.ReadUInt32(state.Arguments, 4);
        var quiet = APTR.ReadUInt32(state.Arguments, 8) != 0;
        if (sources == 0 || to == 0) { error = (int)DOS.Error.BadTemplate; return 20; }
        var first = APTR.ReadUInt32(APTR.FromPointer(sources), 0);
        if (first == 0) { error = (int)DOS.Error.BadTemplate; return 20; }
        var multiple = APTR.ReadUInt32(APTR.FromPointer(sources), 4) != 0;
        APTR.WriteUInt32(state.Anchor, DosLayout.AnchorPath.BreakBits, 0x1000);
        APTR.WriteUInt8(state.Anchor, DosLayout.AnchorPath.Flags, 1);
        APTR.WriteUInt16(state.Anchor, DosLayout.AnchorPath.StringLength, 255);
        state.SearchAttempted = true;
        if (DOS.MatchFirst(CString.FromPointer(first), state.Anchor) != 0)
        {
            error = (int)DOS.IoErr();
            if (error == 205) FailurePrefix(ref state, first, to, error);
            return 0;
        }
        var wild = (APTR.ReadUInt8(state.Anchor, DosLayout.AnchorPath.Flags) & 2) != 0;
        state.DestinationLock = DOS.LockRaw(CString.FromPointer(to), DOS.LockMode.Shared);
        var directory = false;
        if (!state.DestinationLock.IsNull)
        {
            var fib = DOS.AllocDosObject((uint)DosObjectType.FileInfoBlock, APTR.Null);
            if (fib.IsNull) { error = (int)DOS.IoErr(); return 20; }
            directory = DOS.Examine(state.DestinationLock, fib) != 0 && FileInfoBlock.GetDirEntryType(fib.Raw) > 0;
            DOS.FreeDosObject((uint)DosObjectType.FileInfoBlock, fib);
        }
        if (!directory && (wild || multiple))
        {
            APTR.WriteUInt32(state.Arguments, 16, to);
            DOS.VPrintf("Destination \"%s\" is not a directory.\n", APTR.FromPointer(state.Arguments.Raw + 16));
            return 0;
        }
        if (directory && !multiple)
        {
            var sourceLock = DOS.LockRaw(CString.FromPointer(first), DOS.LockMode.Shared);
            if (!sourceLock.IsNull)
            {
                directory = DOS.SameLock(sourceLock, state.DestinationLock) != 0;
                DOS.UnLock(sourceLock);
            }
        }
        EndSearch(ref state);
        if (!directory)
        {
            if (DOS.ParsePattern(CString.FromPointer(first), state.Source, 256) < 0)
            { error = (int)DOS.IoErr(); return 20; }
            if (Length(state.Source, 256) < 0) { error = 120; return 20; }
            if (DOS.Rename(CString.FromPointer(state.Source.Raw), CString.FromPointer(to)) == 0)
            {
                error = (int)DOS.IoErr();
                FailurePrefix(ref state, first, to, error);
            }
            return 0;
        }
        if (DOS.NameFromLock(state.DestinationLock, state.Destination, 256) == 0)
        { error = (int)DOS.IoErr(); return 20; }
        var prefix = Length(state.Destination, 256);
        if (prefix < 0) { error = 120; return 20; }
        if (prefix == 0) { error = 210; return 20; }
        if (APTR.ReadUInt8(state.Destination, prefix - 1) != (byte)':')
        {
            if (prefix >= 255) { error = 120; return 20; }
            APTR.WriteUInt8(state.Destination, prefix++, (byte)'/');
            APTR.WriteUInt8(state.Destination, prefix, 0);
        }
        var sourceSlot = APTR.FromPointer(sources);
        while (APTR.ReadUInt32(sourceSlot, 0) != 0)
        {
            state.SearchAttempted = true;
            if (DOS.MatchFirst(CString.FromPointer(APTR.ReadUInt32(sourceSlot, 0)), state.Anchor) != 0)
            { error = (int)DOS.IoErr(); return 0; }
            while (true)
            {
                var name = APTR.FromPointer(state.Anchor.Raw + DosLayout.AnchorPath.Info + FileInfoBlock.FileNameOffset);
                var path = APTR.FromPointer(state.Anchor.Raw + DosLayout.AnchorPath.PathBuffer);
                var nameLength = Length(name, 108);
                var sourceLength = Length(path, 256);
                if (nameLength < 0 || sourceLength < 0 || prefix + nameLength >= 256)
                { error = 120; return 20; }
                Copy(path, state.Source, sourceLength);
                Copy(name, APTR.FromPointer(state.Destination.Raw + (uint)prefix), nameLength);
                var next = DOS.MatchNext(state.Anchor);
                FormatPair(ref state, state.Source.Raw, state.Destination.Raw);
                if (!quiet) DOS.VPrintf("Renaming %s as %s\n", APTR.FromPointer(state.Arguments.Raw + 16));
                if (DOS.Rename(CString.FromPointer(state.Source.Raw), CString.FromPointer(state.Destination.Raw)) == 0)
                {
                    error = (int)DOS.IoErr();
                    FailurePrefix(ref state, state.Source.Raw, state.Destination.Raw, error);
                    return 0;
                }
                if (next != 0) break;
            }
            EndSearch(ref state);
            sourceSlot = APTR.FromPointer(sourceSlot.Raw + 4);
        }
        return 0;
    }

    private static void EndSearch(ref State state)
    {
        if (!state.SearchAttempted) return;
        state.SearchAttempted = false;
        DOS.MatchEnd(state.Anchor);
    }

    private static int Length(APTR text, int capacity)
    {
        for (var length = 0; length < capacity; length++)
            if (APTR.ReadUInt8(text, length) == 0) return length;
        return -1;
    }

    private static void Copy(APTR source, APTR destination, int length)
    {
        for (var index = 0; index <= length; index++)
            APTR.WriteUInt8(destination, index, APTR.ReadUInt8(source, index));
    }

    private static void FormatPair(ref State state, uint source, uint destination)
    {
        APTR.WriteUInt32(state.Arguments, 16, source);
        APTR.WriteUInt32(state.Arguments, 20, destination);
    }

    private static void FailurePrefix(ref State state, uint source, uint destination, int error)
    {
        FormatPair(ref state, source, destination);
        DOS.VPrintf("Can't rename %s as %s because ", APTR.FromPointer(state.Arguments.Raw + 16));
        DOS.SetIoErr((DOS.Error)error);
    }
}
