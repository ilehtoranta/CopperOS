using Amiga;

namespace CopperOS.DirQualification;

// A normal CLI program. Every command and file operation goes through native
// public OS vectors; it neither implements DOS services nor injects CPU state.
public static class Guest
{
    private const uint Capacity = 65536;
    private const uint RowBytes = 112;
    private const uint BreakMask = 1u << 12;

    public static int Main()
    {
        var library = Exec.OpenLibraryRaw("dos.library", 37);
        if (library.IsNull) return 101;
        DOS.DOSLibraryBase = library;
        var rows = Exec.AllocMem(Capacity, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        var buffer = Exec.AllocMem(Capacity, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (rows.IsNull || buffer.IsNull) return 102;
        var fixture = DOS.OpenRaw("CASES.FIX", DOS.FileMode.OldFile);
        if (fixture.IsNull) return 103;
        var fixtureLength = DOS.Read(fixture, rows.Raw, (int)Capacity);
        DOS.Close(fixture);
        if (fixtureLength < 16 || APTR.ReadUInt32(rows, 0) != 0x53514631) return 104;
        var count = APTR.ReadUInt32(rows, 4);
        if (count > 63 || fixtureLength != unchecked((int)(16 + count * 1040))) return 105;
        var baseline = DOS.LoadSeg("DIRB");
        var candidate = DOS.LoadSeg("DIRC");
        if (!baseline.HasValue || !candidate.HasValue) return 106;
        var nil = DOS.OpenRaw("NIL:", DOS.FileMode.OldFile);
        var capture = DOS.OpenRaw("RAM:DirQOutput", DOS.FileMode.NewFile);
        var record = DOS.OpenRaw("PAYLOAD.OK", DOS.FileMode.NewFile);
        if (nil.IsNull || capture.IsNull || record.IsNull) return 107;
        // Native RAM directories and files provide nested traversal independent
        // of the floppy's launcher/receipt files.
        var directory = DOS.CreateDirRaw("RAM:DirQ");
        if (directory.IsNull) return 108;
        DOS.UnLock(directory);
        directory = DOS.CreateDirRaw("RAM:DirQ/sub");
        if (directory.IsNull) return 109;
        DOS.UnLock(directory);
        if (!CopyFile("TEXT.IN", "RAM:DirQ/one.txt", buffer) ||
            !CopyFile("TEXT.IN", "RAM:DirQ/sub/two.txt", buffer) ||
            !CopyFile("LONG.IN", "RAM:DirQ/long.txt", buffer) ||
            !CopyFile("EMPTY.IN", "RAM:DirQ/empty.txt", buffer) ||
            !CopyFile("TEXT.IN", "RAM:DirQ/spaced name.txt", buffer)) return 110;
        directory = DOS.CreateDirRaw("RAM:DirEmptyQ");
        if (directory.IsNull) return 118;
        DOS.UnLock(directory);
        if (!PrepareFaults(buffer)) return 119;
        var working = DOS.LockRaw("RAM:DirQ", DOS.LockMode.Shared);
        if (working.IsNull) return 120;
        var previousDirectory = DOS.CurrentDirRaw(working);
        var previousInput = DOS.SelectInput(nil);
        var previousOutput = DOS.SelectOutput(capture);
        var process = Exec.FindTask(CString.FromPointer(0));
        var result = 0;
        APTR.WriteUInt32(buffer, 0, 0x53515231);
        APTR.WriteUInt32(buffer, 4, count);
        if (DOS.Write(record, buffer.Raw, 8) != 8) result = 111;
        for (var index = 0u; index < count && result == 0; index++)
        {
            var row = APTR.FromPointer(rows.Raw + 16 + index * 1040);
            var id = APTR.ReadUInt32(row, 0);
            var image = APTR.ReadUInt32(row, 4);
            var stack = APTR.ReadUInt32(row, 8);
            var flags = APTR.ReadUInt32(row, 12);
            var length = APTR.ReadUInt32(row, 16);
            if (length > 1019) { result = 112; break; }
            var segment = image == 0 ? baseline.Value : candidate.Value;
            DOS.Seek(capture, 0, -1);
            var oldSignals = Exec.SetSignal((flags & 1) != 0 ? BreakMask : 0, BreakMask);
            DOS.SetIoErr(DOS.Error.None);
            Snapshot(buffer, 32, process, library);
            var availableBefore = Exec.AvailMem(Exec.MemoryFlags.Any);
            var answer = DOS.RunCommand(segment, unchecked((int)stack),
                APTR.FromPointer(row.Raw + 20), unchecked((int)length));
            // Capture before any DOS operation can overwrite pr_Result2.
            var error = (uint)DOS.IoErr();
            DOS.DOSLibraryBase = library;
            var availableAfter = Exec.AvailMem(Exec.MemoryFlags.Any);
            Snapshot(buffer, 72, process, library);
            Exec.SetSignal(oldSignals, BreakMask);
            if (DOS.Flush(capture) == 0)
            { result = 113; break; }
            var outputLength = DOS.Seek(capture, 0, 0);
            if (outputLength < 0 || outputLength > (int)(Capacity - RowBytes))
            { result = 114; break; }
            if (DOS.Seek(capture, 0, -1) < 0) { result = 113; break; }
            // Capture up to this invocation's final position, excluding any
            // tail left by a longer earlier invocation on the reused handle.
            APTR.WriteUInt32(buffer, 0, id);
            APTR.WriteUInt32(buffer, 4, image);
            APTR.WriteUInt32(buffer, 8, stack);
            APTR.WriteUInt32(buffer, 12, unchecked((uint)answer));
            APTR.WriteUInt32(buffer, 16, error);
            APTR.WriteUInt32(buffer, 20, unchecked((uint)outputLength));
            APTR.WriteUInt32(buffer, 24, availableBefore);
            APTR.WriteUInt32(buffer, 28, availableAfter);
            if (DOS.Write(record, buffer.Raw, (int)RowBytes) != (int)RowBytes)
            { result = 115; break; }
            var remaining = outputLength;
            while (remaining > 0)
            {
                var read = DOS.Read(capture, buffer.Raw, remaining);
                if (read <= 0 || DOS.Write(record, buffer.Raw, read) != read)
                { result = 116; break; }
                remaining -= read;
            }
        }
        DOS.SelectInput(previousInput);
        DOS.SelectOutput(previousOutput);
        DOS.CurrentDirRaw(previousDirectory);
        DOS.UnLock(working);
        DOS.Close(nil);
        DOS.Close(capture);
        if (DOS.Flush(record) == 0 || DOS.Close(record) == 0) result = 117;
        DOS.UnLoadSeg(candidate.Value);
        DOS.UnLoadSeg(baseline.Value);
        Exec.FreeMem(buffer, Capacity);
        Exec.FreeMem(rows, Capacity);
        Exec.CloseLibrary(library);
        DOS.DOSLibraryBase = APTR.Null;
        return result;
    }

    private static bool PrepareFaults(APTR buffer)
    {
        var faults = DOS.OpenRaw("FAULTS.FIX", DOS.FileMode.NewFile);
        if (faults.IsNull) return false;
        var success = WriteFault(117, faults, buffer) && WriteFault(118, faults, buffer) &&
            WriteFault(120, faults, buffer) && WriteFault(204, faults, buffer) &&
            WriteFault(205, faults, buffer) && WriteFault(212, faults, buffer) &&
            WriteFault(236, faults, buffer) && WriteFault(304, faults, buffer);
        if (DOS.Close(faults) == 0) success = false;
        return success;
    }

    private static bool WriteFault(int error, BPTR output, APTR buffer)
    {
        APTR.WriteUInt32(buffer, 0, unchecked((uint)error));
        var text = APTR.FromPointer(buffer.Raw + 4);
        if (DOS.Fault((DOS.Error)error, CString.FromPointer(0), text, 124) == 0) return false;
        return DOS.Write(output, buffer.Raw, 128) == 128;
    }

    private static void Snapshot(APTR buffer, int offset, APTR process, APTR library)
    {
        APTR.WriteUInt32(buffer, offset, DOS.Input().Raw);
        APTR.WriteUInt32(buffer, offset + 4, DOS.Output().Raw);
        APTR.WriteUInt32(buffer, offset + 8, APTR.ReadUInt32(process, DosLayout.Process.CurrentDirectory));
        APTR.WriteUInt32(buffer, offset + 12, APTR.ReadUInt32(process, DosLayout.Process.CommandLineInterface));
        APTR.WriteUInt32(buffer, offset + 16, APTR.ReadUInt32(process, DosLayout.Process.StackSize));
        APTR.WriteUInt32(buffer, offset + 20, APTR.ReadUInt32(process, DosLayout.Process.StackBase));
        APTR.WriteUInt32(buffer, offset + 24, APTR.ReadUInt32(process, ExecLayout.Task.StackLower));
        APTR.WriteUInt32(buffer, offset + 28, APTR.ReadUInt32(process, ExecLayout.Task.StackUpper));
        APTR.WriteUInt32(buffer, offset + 32, APTR.ReadUInt32(process, ExecLayout.Task.SignalAllocated));
        APTR.WriteUInt32(buffer, offset + 36, APTR.ReadUInt16(library, 32)); // lib_OpenCnt
    }

    private static bool CopyFile(CString source, CString target, APTR buffer)
    {
        var input = DOS.OpenRaw(source, DOS.FileMode.OldFile);
        if (input.IsNull) return false;
        var output = DOS.OpenRaw(target, DOS.FileMode.NewFile);
        if (output.IsNull) { DOS.Close(input); return false; }
        var success = true;
        while (true)
        {
            var read = DOS.Read(input, buffer.Raw, (int)Capacity);
            if (read < 0) { success = false; break; }
            if (read == 0) break;
            if (DOS.Write(output, buffer.Raw, read) != read) { success = false; break; }
        }
        if (DOS.Close(input) == 0) success = false;
        if (DOS.Close(output) == 0) success = false;
        return success;
    }
}
