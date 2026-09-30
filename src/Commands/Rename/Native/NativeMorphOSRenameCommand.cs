using Amiga;
using CopperSharp.Compiler;
using System.Runtime.InteropServices;

namespace CopperOS.Commands.Native;

/// <summary>
/// Independently authored MorphOS Rename behavior through public DOS vectors.
/// The caller owns startup and the library lease. This is a development body;
/// native runtime, original-system and installed PURE qualification are separate.
/// </summary>
public static class NativeMorphOSRenameCommand
{
    private const int PathBytes = 2048;
    private const uint WorkspaceBytes = DosLayout.AnchorPath.Size + PathBytes + PathBytes;
    private const uint DestinationOffset = DosLayout.AnchorPath.Size + PathBytes;
    private const int EntryTypeOffset = FileInfoBlock.ProtectionOffset + sizeof(uint);
    private const uint BreakMask = 0x1000;

    [M68kStackAlignment(4)]
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct Cells
    {
        public uint Sources, Destination, Quiet, FormatSource, FormatDestination;

        public static APTR AddressOf(ref Cells cells) =>
            throw new System.NotSupportedException("AddressOf is lowered by CopperSharp.");
    }

    private struct State
    {
        public APTR Cells, Workspace, Path, Destination, Diagnostic;
        public BPTR DestinationLock;
        public bool SearchAttempted;
    }

    public static int Run(out int ioError)
    {
        Cells cells = default;
        var cellAddress = Cells.AddressOf(ref cells);
        var rdArgs = DOS.ReadArgs("FROM/A/M,TO=AS/A,QUIET/S", cellAddress, APTR.Null);
        if (rdArgs.IsNull)
        {
            DOS.PrintFault(DOS.IoErr(), "Rename");
            ioError = (int)DOS.IoErr();
            return DOS.RETURN_ERROR;
        }

        State state = default;
        state.Cells = cellAddress;
        var selectedError = 0;
        var result = DOS.RETURN_FAIL;
        state.Workspace = Exec.AllocVec(WorkspaceBytes, (uint)Exec.MemoryFlags.Clear);
        if (state.Workspace.IsNull)
        {
            selectedError = (int)DOS.IoErr();
        }
        else
        {
            state.Path = APTR.FromPointer(state.Workspace.Raw + DosLayout.AnchorPath.PathBuffer);
            state.Destination = APTR.FromPointer(state.Workspace.Raw + DestinationOffset);
            result = Execute(ref state, out selectedError);
        }

        // Failed initial matching also owns one MatchEnd. Preserve ambient
        // error around this added safety cleanup, independently of diagnostics.
        if (state.SearchAttempted)
        {
            var ambient = DOS.IoErr();
            EndSearch(ref state);
            DOS.SetIoErr(ambient);
        }
        DOS.UnLock(state.DestinationLock);
        if (selectedError != 0)
        {
            DOS.PrintFault((DOS.Error)selectedError, CString.FromPointer(0));
            DOS.SetIoErr((DOS.Error)selectedError);
        }
        if (state.Diagnostic.IsNotNull) Exec.FreeVec(state.Diagnostic);
        if (state.Workspace.IsNotNull) Exec.FreeVec(state.Workspace);
        DOS.FreeArgs(rdArgs);
        ioError = (int)DOS.IoErr();
        return result;
    }

    private static int Execute(ref State state, out int selectedError)
    {
        selectedError = 0;
        var sources = APTR.ReadUInt32(state.Cells, 0);
        var to = APTR.ReadUInt32(state.Cells, 4);
        var quiet = APTR.ReadUInt32(state.Cells, 8) != 0;
        if (sources == 0 || to == 0)
        { selectedError = (int)DOS.Error.BadTemplate; return DOS.RETURN_FAIL; }
        var first = APTR.ReadUInt32(APTR.FromPointer(sources), 0);
        if (first == 0)
        { selectedError = (int)DOS.Error.BadTemplate; return DOS.RETURN_FAIL; }
        var multiple = APTR.ReadUInt32(APTR.FromPointer(sources), 4) != 0;
        APTR.WriteUInt32(state.Workspace, DosLayout.AnchorPath.BreakBits, BreakMask);
        APTR.WriteUInt8(state.Workspace, DosLayout.AnchorPath.Flags, 1);
        APTR.WriteUInt16(state.Workspace, DosLayout.AnchorPath.StringLength, PathBytes);

        state.SearchAttempted = true;
        if (DOS.MatchFirst(CString.FromPointer(first), state.Workspace) != 0)
        {
            selectedError = (int)DOS.IoErr();
            if (selectedError == 205) FailurePrefix(ref state, first, to);
            return DOS.RETURN_FAIL;
        }
        var wild = (APTR.ReadUInt8(state.Workspace, DosLayout.AnchorPath.Flags) & 2) != 0;
        EndSearch(ref state);

        var directory = false;
        state.DestinationLock = DOS.LockRaw(CString.FromPointer(to), DOS.LockMode.Shared);
        if (!state.DestinationLock.IsNull)
        {
            var fib = DOS.AllocDosObject((uint)DosObjectType.FileInfoBlock, APTR.Null);
            if (fib.IsNull)
            {
                DOS.PrintFault(DOS.IoErr(), "Rename");
                return DOS.RETURN_FAIL;
            }
            if (DOS.Examine(state.DestinationLock, fib) == 0)
            {
                DOS.PrintFault(DOS.IoErr(), "Rename");
                DOS.FreeDosObject((uint)DosObjectType.FileInfoBlock, fib);
                return DOS.RETURN_FAIL;
            }
            directory = unchecked((int)APTR.ReadUInt32(fib, EntryTypeOffset)) >= 0;
            DOS.FreeDosObject((uint)DosObjectType.FileInfoBlock, fib);
        }
        if (!directory && (wild || multiple))
        {
            FormatPair(ref state, to, 0);
            DOS.VPrintf("Destination \"%s\" is not a directory.\n", FormatAddress(ref state));
            return DOS.RETURN_FAIL;
        }
        if (directory && !multiple)
        {
            var sourceLock = DOS.LockRaw(CString.FromPointer(first), DOS.LockMode.Shared);
            if (!sourceLock.IsNull)
            {
                directory = DOS.SameLock(sourceLock, state.DestinationLock) != 0;
                DOS.UnLock(sourceLock);
            }
            quiet = true;
        }
        if (!directory)
        {
            if (DOS.ParsePattern(CString.FromPointer(first), state.Path, PathBytes) < 0)
            {
                selectedError = (int)DOS.IoErr();
                FailurePrefix(ref state, first, to);
                return DOS.RETURN_FAIL;
            }
            if (Length(state.Path) < 0)
            { selectedError = 120; return DOS.RETURN_FAIL; }
            if (DOS.Rename(CString.FromPointer(state.Path.Raw), CString.FromPointer(to)) == 0)
            {
                selectedError = (int)DOS.IoErr();
                FailurePrefix(ref state, first, to);
                return DOS.RETURN_FAIL;
            }
            return DOS.RETURN_OK;
        }

        if (DOS.NameFromLock(state.DestinationLock, state.Destination, PathBytes) == 0)
        {
            if ((int)DOS.IoErr() == 120)
            {
                DOS.PrintFault((DOS.Error)120, "Rename");
                return DOS.RETURN_FAIL;
            }
            APTR.WriteUInt8(state.Destination, 0, 0);
        }
        var prefixLength = Length(state.Destination);
        if (prefixLength == 0)
        {
            prefixLength = Length(APTR.FromPointer(to));
            if (prefixLength >= 0) Copy(APTR.FromPointer(to), state.Destination, prefixLength);
        }
        if (prefixLength < 0)
        { selectedError = 120; return DOS.RETURN_FAIL; }

        // Diagnostic storage survives MatchEnd and costs no large stack frame.
        // Its failure is checked before any directory mutation is attempted.
        state.Diagnostic = Exec.AllocVec(PathBytes, 0);
        if (state.Diagnostic.IsNull)
        { selectedError = (int)DOS.IoErr(); return DOS.RETURN_FAIL; }
        var sourceSlot = APTR.FromPointer(sources);
        while (APTR.ReadUInt32(sourceSlot, 0) != 0)
        {
            state.SearchAttempted = true;
            var matched = DOS.MatchFirst(CString.FromPointer(APTR.ReadUInt32(sourceSlot, 0)), state.Workspace);
            while (matched == 0)
            {
                var pathLength = Length(state.Path);
                if (pathLength < 0)
                { selectedError = 120; return DOS.RETURN_FAIL; }
                APTR.WriteUInt8(state.Destination, prefixLength, 0);
                var file = DOS.FilePart(CString.FromPointer(state.Path.Raw));
                var fileAddress = STRPTR.ToUInt32(file);
                if (fileAddress < state.Path.Raw || fileAddress > state.Path.Raw + (uint)pathLength)
                { selectedError = 120; return DOS.RETURN_FAIL; }
                if (DOS.AddPart(CString.FromPointer(state.Destination.Raw), CString.FromPointer(fileAddress), PathBytes) == 0)
                {
                    EndSearch(ref state);
                    DOS.PrintFault((DOS.Error)120, "Rename");
                    DOS.SetIoErr((DOS.Error)120);
                    return DOS.RETURN_FAIL;
                }
                if (Length(state.Destination) < 0)
                { selectedError = 120; return DOS.RETURN_FAIL; }
                Copy(state.Path, state.Diagnostic, pathLength);
                FormatPair(ref state, state.Path.Raw, state.Destination.Raw);
                if (!quiet) DOS.VPrintf("Renaming %s as %s\n", FormatAddress(ref state));
                if (DOS.Rename(CString.FromPointer(state.Path.Raw), CString.FromPointer(state.Destination.Raw)) == 0)
                {
                    selectedError = (int)DOS.IoErr();
                    EndSearch(ref state);
                    FailurePrefix(ref state, state.Diagnostic.Raw, state.Destination.Raw);
                    return DOS.RETURN_FAIL;
                }
                matched = DOS.MatchNext(state.Workspace);
            }
            EndSearch(ref state);
            sourceSlot = APTR.FromPointer(sourceSlot.Raw + sizeof(uint));
        }
        if ((APTR.ReadUInt32(state.Workspace, DosLayout.AnchorPath.FoundBreak) & BreakMask) != 0)
        {
            DOS.PrintFault((DOS.Error)304, CString.FromPointer(0));
            return DOS.RETURN_WARN;
        }
        return DOS.RETURN_OK;
    }

    private static void EndSearch(ref State state)
    {
        if (!state.SearchAttempted) return;
        state.SearchAttempted = false;
        DOS.MatchEnd(state.Workspace);
    }

    private static int Length(APTR text)
    {
        for (var index = 0; index < PathBytes; index++)
            if (APTR.ReadUInt8(text, index) == 0) return index;
        return -1;
    }

    private static void Copy(APTR source, APTR destination, int length)
    {
        for (var index = 0; index <= length; index++)
            APTR.WriteUInt8(destination, index, APTR.ReadUInt8(source, index));
    }

    private static APTR FormatAddress(ref State state) => APTR.FromPointer(state.Cells.Raw + 12);

    private static void FormatPair(ref State state, uint source, uint destination)
    {
        APTR.WriteUInt32(state.Cells, 12, source);
        APTR.WriteUInt32(state.Cells, 16, destination);
    }

    private static void FailurePrefix(ref State state, uint source, uint destination)
    {
        FormatPair(ref state, source, destination);
        DOS.VPrintf("Can't rename %s as %s because ", FormatAddress(ref state));
    }
}
