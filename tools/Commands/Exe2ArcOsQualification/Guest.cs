using Amiga;

namespace CopperOS.Exe2ArcQualification;

// A normal CLI program. Every command and file operation goes through native
// public OS vectors; it neither implements DOS services nor injects CPU state.
public static class Guest
{
    private const uint Capacity = 65536;
    private const uint RowBytes = 128;
    private const uint PayloadCapacity = 131072;
    private const uint RequestBytes = 1552;
    private const uint BreakMask = 1u << 12;

    public static int Main()
    {
        var library = Exec.OpenLibraryRaw("dos.library", 37);
        if (library.IsNull) return 101;
        DOS.DOSLibraryBase = library;
        var rows = Exec.AllocMem(Capacity, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        var buffer = Exec.AllocMem(Capacity, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        var payloadBuffer = Exec.AllocMem(PayloadCapacity, Exec.MemoryFlags.Public | Exec.MemoryFlags.Clear);
        if (rows.IsNull || buffer.IsNull || payloadBuffer.IsNull) return 102;
        var fixture = DOS.OpenRaw("CASES.FIX", DOS.FileMode.OldFile);
        if (fixture.IsNull) return 103;
        var fixtureLength = DOS.Read(fixture, rows.Raw, (int)Capacity);
        DOS.Close(fixture);
        if (fixtureLength < 16 || APTR.ReadUInt32(rows, 0) != 0x45414631) return 104;
        var count = APTR.ReadUInt32(rows, 4);
        if (count > 42 || fixtureLength != unchecked((int)(16 + count * RequestBytes))) return 105;
        var baseline = DOS.LoadSeg("EXE2ARCB");
        var candidate = DOS.LoadSeg("EXE2ARCC");
        if (!baseline.HasValue || !candidate.HasValue) return 106;
        var nil = DOS.OpenRaw("NIL:", DOS.FileMode.OldFile);
        var capture = DOS.OpenRaw("RAM:Exe2ArcQOutput", DOS.FileMode.NewFile);
        var record = DOS.OpenRaw("PAYLOAD.OK", DOS.FileMode.NewFile);
        if (nil.IsNull || capture.IsNull || record.IsNull) return 107;
        var directory = DOS.CreateDirRaw("RAM:Exe2ArcQ");
        if (directory.IsNull) return 108;
        DOS.UnLock(directory);
        directory = DOS.CreateDirRaw("RAM:Exe2ArcOut");
        if (directory.IsNull) return 109;
        DOS.UnLock(directory);
        if (!PrepareInputs(buffer, APTR.ReadUInt32(rows, 8))) return 110;
        var previousInput = DOS.SelectInput(nil);
        var previousOutput = DOS.SelectOutput(capture);
        var process = Exec.FindTask(CString.FromPointer(0));
        var result = 0;
        APTR.WriteUInt32(buffer, 0, 0x45415231);
        APTR.WriteUInt32(buffer, 4, count);
        if (DOS.Write(record, buffer.Raw, 8) != 8) result = 111;
        for (var index = 0u; index < count && result == 0; index++)
        {
            var row = APTR.FromPointer(rows.Raw + 16 + index * RequestBytes);
            var id = APTR.ReadUInt32(row, 0);
            var image = APTR.ReadUInt32(row, 4);
            var stack = APTR.ReadUInt32(row, 8);
            var flags = APTR.ReadUInt32(row, 12);
            var length = APTR.ReadUInt32(row, 16);
            var payloadPath = CString.FromPointer(row.Raw + 1040);
            DOS.DeleteFile(payloadPath);
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

            Snapshot(buffer, 72, process, library);
            Exec.SetSignal(oldSignals, BreakMask);
            if (DOS.Flush(capture) == 0)
            { result = 113; break; }
            var outputLength = DOS.Seek(capture, 0, 0);
            if (outputLength < 0 || outputLength > (int)(Capacity - RowBytes))
            { result = 114; break; }
            if (DOS.Seek(capture, 0, -1) < 0) { result = 113; break; }
            // Preserve the command's caller snapshot before inspecting files.
            var stdoutLength = outputLength;
            if (outputLength != 0 && DOS.Read(capture, buffer.Raw + RowBytes, outputLength) != outputLength)
            { result = 116; break; }
            var payload = DOS.OpenRaw(payloadPath, DOS.FileMode.OldFile);
            var exists = payload.IsNull ? 0u : 1u;
            var payloadLength = 0;
            var closeResult = 0;
            var deleteResult = 0;
            if (payload.IsNotNull)
            {
                if (DOS.Seek(payload, 0, 1) < 0) { result = 121; break; }
                payloadLength = DOS.Seek(payload, 0, 0);
                if (payloadLength < 0 || payloadLength > (int)PayloadCapacity || DOS.Seek(payload, 0, -1) < 0)
                { result = 122; break; }
                if (payloadLength != 0 && DOS.Read(payload, payloadBuffer.Raw, payloadLength) != payloadLength)
                { result = 123; break; }
                closeResult = DOS.Close(payload);
                deleteResult = DOS.DeleteFile(payloadPath) != 0 ? 1 : 0;
            }
            // Successful extraction allocates RAM file nodes/blocks. Compare
            // free memory after closing and deleting those intentional outputs.
            var availableAfter = Exec.AvailMem(Exec.MemoryFlags.Any);
            APTR.WriteUInt32(buffer, 0, id);
            APTR.WriteUInt32(buffer, 4, image);
            APTR.WriteUInt32(buffer, 8, stack);
            APTR.WriteUInt32(buffer, 12, unchecked((uint)answer));
            APTR.WriteUInt32(buffer, 16, error);
            APTR.WriteUInt32(buffer, 20, unchecked((uint)stdoutLength));
            APTR.WriteUInt32(buffer, 24, availableBefore);
            APTR.WriteUInt32(buffer, 28, availableAfter);
            APTR.WriteUInt32(buffer, 112, exists);
            APTR.WriteUInt32(buffer, 116, unchecked((uint)payloadLength));
            APTR.WriteUInt32(buffer, 120, unchecked((uint)closeResult));
            APTR.WriteUInt32(buffer, 124, unchecked((uint)deleteResult));
            if (DOS.Write(record, buffer.Raw, (int)RowBytes + stdoutLength) != (int)RowBytes + stdoutLength)
            { result = 115; break; }
            if (payloadLength != 0 && DOS.Write(record, payloadBuffer.Raw, payloadLength) != payloadLength)
            { result = 124; break; }
        }
        DOS.SelectInput(previousInput);
        DOS.SelectOutput(previousOutput);
        DOS.Close(nil);
        DOS.Close(capture);
        if (DOS.Flush(record) == 0 || DOS.Close(record) == 0) result = 117;
        DOS.UnLoadSeg(candidate.Value);
        DOS.UnLoadSeg(baseline.Value);
        Exec.FreeMem(payloadBuffer, PayloadCapacity);
        Exec.FreeMem(buffer, Capacity);
        Exec.FreeMem(rows, Capacity);
        Exec.CloseLibrary(library);
        DOS.DOSLibraryBase = APTR.Null;
        return result;
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

    private static bool PrepareInputs(APTR buffer, uint mask) =>
        ((mask & 1u) == 0 || CopyFile("ZIP.IN", "RAM:Exe2ArcQ/zip.exe", buffer)) &&
        ((mask & 2u) == 0 || CopyFile("ACE.IN", "RAM:Exe2ArcQ/ace.exe", buffer)) &&
        ((mask & 4u) == 0 || CopyFile("RAR.IN", "RAM:Exe2ArcQ/rar.exe", buffer)) &&
        ((mask & 8u) == 0 || CopyFile("CAB.IN", "RAM:Exe2ArcQ/cab.exe", buffer)) &&
        ((mask & 16u) == 0 || CopyFile("ARJ.IN", "RAM:Exe2ArcQ/arj.exe", buffer)) &&
        ((mask & 32u) == 0 || CopyFile("LHA.IN", "RAM:Exe2ArcQ/lha.exe", buffer)) &&
        ((mask & 64u) == 0 || CopyFile("LZH.IN", "RAM:Exe2ArcQ/lzh.exe", buffer)) &&
        ((mask & 128u) == 0 || CopyFile("RANDOM.IN", "RAM:Exe2ArcQ/random.exe", buffer)) &&
        ((mask & 256u) == 0 || CopyFile("EMPTY.IN", "RAM:Exe2ArcQ/empty.exe", buffer)) &&
        ((mask & 512u) == 0 || CopyFile("ZERO.IN", "RAM:Exe2ArcQ/zero.exe", buffer)) &&
        ((mask & 1024u) == 0 || CopyFile("BADZIP.IN", "RAM:Exe2ArcQ/badzip.exe", buffer)) &&
        ((mask & 2048u) == 0 || CopyFile("SHORTZIP.IN", "RAM:Exe2ArcQ/shortzip.exe", buffer)) &&
        ((mask & 4096u) == 0 || CopyFile("CROSS.IN", "RAM:Exe2ArcQ/cross.exe", buffer)) &&
        ((mask & 8192u) == 0 || CopyFile("BIG.IN", "RAM:Exe2ArcQ/big.exe", buffer)) &&
        ((mask & 16384u) == 0 || CopyFile("RAR.IN", "RAM:Exe2ArcQ/quoted name.exe", buffer)) &&
        ((mask & 32768u) == 0 || CopyFile("RAR.IN", "RAM:Exe2ArcQ/nosuffix", buffer));

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
