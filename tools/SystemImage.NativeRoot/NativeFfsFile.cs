using Amiga;

namespace CopperOS.SystemImage;

// Original sequential read-only DOS1 bootstrap reader. Uses public Exec and
// trackdisk IO; keeps the current FFS pointer table apart from the track cache.
// It admits only the generated L/Shell-Seg file on standard DD DF0.
internal struct NativeFfsFile
{
    private APTR workspace, port, request;
    private uint cachedTrack, header, size, position, tableIndex;
    private const uint Bytes = 6144;
    private APTR Table => APTR.FromPointer(workspace.Raw + 5632);

    public bool Open()
    {
        workspace = APTR.FromPointer(Exec.AllocMem(Bytes, Exec.MemoryFlags.Chip | Exec.MemoryFlags.Clear));
        port = Exec.CreateMsgPort();
        request = port.IsNull ? APTR.Null : Exec.CreateIORequest(port, 56);
        cachedTrack = uint.MaxValue;
        if (workspace.IsNull || request.IsNull || Exec.OpenDevice("trackdisk.device", 0, request, 0) != 0) return false;
        if (!Block(0, Table) || APTR.ReadUInt32(Table, 0) != 0x444F5301) return false;
        var directory = Find(880, "L", 2);
        header = directory == 0 ? 0 : Find(directory, "Shell-Seg", -3);
        if (header == 0) return false;
        size = APTR.ReadUInt32(Table, 324);
        return size > 0 && size <= 901120;
    }

    public void Close()
    {
        if (request.IsNotNull)
        {
            if (APTR.ReadUInt32(request, 20) != 0) Exec.CloseDevice(request);
            Exec.DeleteIORequest(request);
        }
        if (port.IsNotNull) Exec.DeleteMsgPort(port);
        if (workspace.IsNotNull) Exec.FreeMem(workspace.Raw, Bytes);
    }

    private uint Find(uint parent, CString name, int kind)
    {
        if (!Block(parent, Table) || !Checksum()) return 0;
        var text = APTR.FromPointer(CString.ToUInt32(name));
        var length = 0u;
        while (APTR.ReadUInt8(text, (int)length) != 0) length++;
        var hash = length;
        for (var i = 0u; i < length; i++) hash = (hash * 13 + Upper(APTR.ReadUInt8(text, (int)i))) & 0x7FF;
        var key = APTR.ReadUInt32(Table, 24 + (int)(hash % 72) * 4);
        for (var guard = 0; key != 0 && guard < 1760; guard++)
        {
            if (!Block(key, Table) || !Checksum() || APTR.ReadUInt32(Table, 4) != key || APTR.ReadUInt32(Table, 500) != parent) return 0;
            var same = APTR.ReadUInt8(Table, 432) == length;
            for (var i = 0u; same && i < length; i++) same = Upper(APTR.ReadUInt8(Table, 433 + (int)i)) == Upper(APTR.ReadUInt8(text, (int)i));
            if (same) return unchecked((int)APTR.ReadUInt32(Table, 508)) == kind ? key : 0;
            key = APTR.ReadUInt32(Table, 496);
        }
        return 0;
    }

    private static uint Upper(byte value) => value >= 'a' && value <= 'z' ? (uint)(value - 32) : value;

    public int Read(APTR destination, uint length)
    {
        if (position > size) return -1;
        if (length > size - position) length = size - position;
        var copied = 0u;
        while (copied < length)
        {
            var index = position / 512;
            if (index / 72 != tableIndex)
            {
                var next = APTR.ReadUInt32(Table, 504);
                if (index / 72 != tableIndex + 1 || !Block(next, Table) || !Checksum() ||
                    APTR.ReadUInt32(Table, 0) != 16 || APTR.ReadUInt32(Table, 4) != next ||
                    APTR.ReadUInt32(Table, 500) != header || APTR.ReadUInt32(Table, 508) != 0xFFFFFFFD) return -1;
                tableIndex++;
            }
            if (APTR.ReadUInt32(Table, 8) > 72 || index % 72 >= APTR.ReadUInt32(Table, 8)) return -1;
            var block = APTR.ReadUInt32(Table, 308 - (int)(index % 72) * 4);
            if (block < 2 || block >= 1760 || !Track(block / 11)) return -1;
            var within = position % 512;
            var count = 512 - within;
            if (count > length - copied) count = length - copied;
            var source = (block % 11) * 512 + within;
            for (var i = 0u; i < count; i++) APTR.WriteUInt8(destination, (int)(copied + i), APTR.ReadUInt8(workspace, (int)(source + i)));
            copied += count; position += count;
        }
        return (int)copied;
    }

    private bool Block(uint block, APTR destination)
    {
        if (block >= 1760 || !Track(block / 11)) return false;
        for (var i = 0; i < 512; i += 4) APTR.WriteUInt32(destination, i, APTR.ReadUInt32(workspace, (int)(block % 11 * 512) + i));
        return true;
    }

    private bool Track(uint track)
    {
        if (track == cachedTrack) return true;
        cachedTrack = uint.MaxValue;
        APTR.WriteUInt16(request, 28, 2);
        APTR.WriteUInt32(request, 40, workspace.Raw);
        APTR.WriteUInt32(request, 36, 5632);
        APTR.WriteUInt32(request, 44, track * 5632);
        Exec.DoIO(request);
        if (APTR.ReadUInt8(request, 31) != 0 || APTR.ReadUInt32(request, 32) != 5632) return false;
        cachedTrack = track;
        return true;
    }

    private bool Checksum()
    {
        var sum = 0u;
        for (var i = 0; i < 512; i += 4) sum += APTR.ReadUInt32(Table, i);
        return sum == 0;
    }
}
