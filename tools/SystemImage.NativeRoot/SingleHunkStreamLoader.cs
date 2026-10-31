using Amiga;

namespace CopperOS.SystemImage;

// Bounded original bootstrap loader: one standard CODE hunk, self RELOC32,
// optional SYMBOL table, END. Resolve through native dos.library and stream
// into a 4 KiB scratch block; never require the entire file in contiguous RAM.
// This is not a replacement for general dos.library LoadSeg.
internal struct SingleHunkStreamLoader
{
    private APTR buffer;
    private APTR dos;
    private BPTR file;
    private NativeFfsFile floppyFile;
    private bool floppy;
    private uint cursor, count;
    private bool failed;

    public static BPTR Load(APTR exec, APTR dos, APTR name)
    {
        var reader = new SingleHunkStreamLoader();
        reader.dos = dos;
        reader.file = Vectors.Open(dos, name, 1005);
        if (reader.file.IsNull) { SystemBootstrap.Print(dos, "Shell file open failed\n"); return BPTR.Null; }
        // Retain the qualified DD reader's track/table cache for the original
        // generated DF0 image. DOS identifies the actual opened file's handler;
        // an inserted floppy must never override a Shell opened on DH0.
        reader.floppy = IsDf0File(dos, reader.file);
        if (reader.floppy && !reader.floppyFile.Open())
        { reader.Close(); SystemBootstrap.Print(dos, "FFS open failed\n"); return BPTR.Null; }
        reader.buffer = ExecVectors.AllocMem(exec, 4096, 0x10001);
        if (reader.buffer.IsNull) { reader.Close(); SystemBootstrap.Print(dos, "Scratch allocation failed\n"); return BPTR.Null; }
        var allocation = APTR.Null;
        var bytes = 0u;
        var ok = reader.Word() == 0x3F3 && reader.Word() == 0 &&
            reader.Word() == 1 && reader.Word() == 0 && reader.Word() == 0;
        if (ok)
        {
            var words = reader.Word();
            // Explicit bounded format, not a general Amiga argument guard.
            ok = words is > 0 and < 131070 && reader.Word() == 0x3E9 && reader.Word() == words;
            bytes = words * 4;
        }
        if (!ok || reader.failed) SystemBootstrap.Print(dos, "HUNK header read failed\n");
        if (ok && !reader.failed)
        {
            allocation = ExecVectors.AllocMem(exec, bytes + 8, 0x10001);
            ok = allocation.IsNotNull;
            if (!ok) SystemBootstrap.Print(dos, "Code allocation failed\n");
            if (ok)
            {
                APTR.WriteUInt32(allocation, 0, bytes + 8);
                APTR.WriteUInt32(allocation, 4, 0);
                for (var at = 0u; at < bytes && !reader.failed; at += 4)
                    APTR.WriteUInt32(allocation, (int)(at + 8), reader.Word());
                var type = reader.Word();
                while (!reader.failed && type == 0x3EC)
                {
                    var n = reader.Word();
                    while (!reader.failed && n != 0)
                    {
                        if (reader.Word() != 0 || n > bytes / 4) { reader.failed = true; break; }
                        for (var i = 0u; i < n && !reader.failed; i++)
                        {
                            var offset = reader.Word();
                            if ((offset & 1) != 0 || offset > bytes - 4) { reader.failed = true; break; }
                            var at = APTR.FromPointer(allocation.Raw + 8 + offset);
                            APTR.WriteUInt32(at, 0, APTR.ReadUInt32(at, 0) + allocation.Raw + 8);
                        }
                        n = reader.Word();
                    }
                    type = reader.Word();
                }
                if (!reader.failed && type == 0x3F0)
                {
                    var symbols = 0u;
                    var nameWords = reader.Word();
                    while (!reader.failed && nameWords != 0)
                    {
                        if (nameWords > 256 || symbols++ >= 16384) { reader.failed = true; break; }
                        for (var i = 0u; i < nameWords; i++) reader.Word();
                        // Symbol values are metadata, never applied as relocations.
                        reader.Word();
                        nameWords = reader.Word();
                    }
                    type = reader.Word();
                }
                ok = !reader.failed && type == 0x3F2 && reader.AtEnd();
            }
        }
        ExecVectors.FreeMem(exec, reader.buffer, 4096);
        reader.Close();
        if (!ok || reader.failed)
        {
            SystemBootstrap.Print(dos, reader.failed ? "HUNK stream read failed\n" : "HUNK format rejected\n");
            if (allocation.IsNotNull) ExecVectors.FreeMem(exec, allocation, bytes + 8);
            return BPTR.Null;
        }
        return BPTR.FromAddress(APTR.FromPointer(allocation.Raw + 4));
    }

    private uint Word()
    {
        if (failed) return 0;
        if (cursor == count)
        {
            var read = Read(4096);
            if (read <= 0 || (read & 3) != 0) { failed = true; return 0; }
            cursor = 0; count = (uint)read;
        }
        var value = APTR.ReadUInt32(buffer, (int)cursor);
        cursor += 4;
        return value;
    }
    private bool AtEnd() => cursor == count && Read(4) == 0;

    private int Read(int bytes) => floppy ? floppyFile.Read(buffer, (uint)bytes) : Vectors.Read(dos, file, buffer, bytes);
    private void Close()
    {
        if (floppy) floppyFile.Close();
        Vectors.Close(dos, file);
    }
    private static bool IsDf0File(APTR dos, BPTR file)
    {
        const uint flags = 5; // LDF_DEVICES | LDF_READ
        var list = Vectors.LockDosList(dos, flags);
        if (list.IsNull) return false;
        var device = Vectors.FindDosEntry(dos, list, APTR.FromPointer(CString.ToUInt32("DF0")), flags);
        // Public FileHandle.fh_Type and DeviceNode.dn_Task are handler ports.
        var result = device.IsNotNull && APTR.ReadUInt32(device, 8) != 0 &&
            APTR.ReadUInt32(device, 8) == APTR.ReadUInt32(file.Address, 8);
        Vectors.UnLockDosList(dos, flags);
        return result;
    }

    public static void Release(APTR exec, BPTR segment)
    {
        var allocation = APTR.FromPointer(segment.Address.Raw - 4);
        ExecVectors.FreeMem(exec, allocation, APTR.ReadUInt32(allocation, 0));
    }
}
