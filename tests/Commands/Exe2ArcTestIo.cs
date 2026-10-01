using System.Buffers.Binary;
using Amiga;

namespace CopperOS.Commands.Tests;

// Test-only supplied DOS vectors. These bytes are inert data, never executable
// wrappers. This is neither a DOS handler nor a captured original command.
internal sealed class Exe2ArcTestIoState
{
    internal const int BufferBytes = 102400;
    internal const int GuardBytes = 16;
    internal static readonly BPTR Input = new(0x1137);
    internal static readonly BPTR Output = new(0x2579);
    internal readonly APTR Scratch;
    internal readonly byte[] InputBytes;
    internal readonly byte[] OriginalInput;
    internal readonly byte[] Storage = Enumerable.Repeat((byte)0xcc,
        BufferBytes + GuardBytes * 2).ToArray();
    internal readonly List<byte> OutputBytes = [];
    internal readonly List<IoCall> Calls = [];
    internal readonly Dictionary<(string Operation, int Occurrence), (int Count, int Error)> Faults = [];
    private readonly Dictionary<string, int> _occurrences = [];
    internal int Cursor;
    internal int Initialized;
    internal int ByteReads;
    internal int MappingCalls;
    internal int IoErrCalls;
    internal int SetIoErrCalls;
    internal int SetIoErrValue;
    internal int AmbientIoErr = 0x13572468;
    internal bool RejectMapping;
    internal bool BreakPending;
    internal int BreakAfterCallCount = -1;
    private bool _captureAllowed;

    internal Exe2ArcTestIoState(byte[] input, uint scratch = 0x1001)
    {
        InputBytes = input.ToArray();
        OriginalInput = input.ToArray();
        Scratch = new APTR(scratch);
    }

    internal readonly record struct IoCall(string Operation, uint Handle,
        uint Buffer, int Argument, int Mode, int Result);

    internal static byte[] InputWithMarker(bool cabinet, int length, int offset,
        uint cabinetLength = 36, uint table = 20)
    {
        byte[] bytes = Enumerable.Repeat((byte)0x55, length).ToArray();
        if (offset >= 0)
            PutMarker(bytes, offset, cabinet, cabinetLength, table);
        return bytes;
    }

    internal static byte[] InputWithAceMarker(int length, int offset)
    {
        byte[] bytes = Enumerable.Repeat((byte)0x55, length).ToArray();
        if (offset >= 0)
            PutAceMarker(bytes, offset);
        return bytes;
    }

    internal static byte[] InputWithArjMarker(int length, int offset,
        ushort headerLength = 6, bool validCrc = true)
    {
        byte[] bytes = Enumerable.Repeat((byte)0x55, length).ToArray();
        if (offset >= 0)
            PutArjMarker(bytes, offset, headerLength, validCrc);
        return bytes;
    }

    internal static byte[] InputWithLzhMarker(int length, int offset,
        byte level = 0)
    {
        byte[] bytes = Enumerable.Repeat((byte)0x55, length).ToArray();
        if (offset >= 0)
            PutLzhMarker(bytes, offset);
        if (20 < bytes.Length)
            bytes[20] = level;
        return bytes;
    }

    internal static byte[] InputWithLhaSfx(int length, uint start)
    {
        byte[] bytes = Enumerable.Repeat((byte)0x55, length).ToArray();
        PutLhaSfx(bytes, start);
        return bytes;
    }

    internal static byte[] InputWithZipEocd(int length, int eocdOffset,
        uint declaredCentralOffset = 44, uint centralSize = 46,
        uint localOffset = 0)
    {
        byte[] bytes = Enumerable.Repeat((byte)0x55, length).ToArray();
        int centralOffset = checked(eocdOffset - (int)centralSize);
        PutZipCentral(bytes, centralOffset, localOffset);
        PutZipEocd(bytes, eocdOffset, declaredCentralOffset, centralSize);
        return bytes;
    }

    internal static void PutZipEocd(byte[] bytes, int eocdOffset,
        uint declaredCentralOffset, uint centralSize)
    {
        "PK\x05\x06"u8.CopyTo(bytes.AsSpan(eocdOffset));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(eocdOffset + 12),
            centralSize);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(eocdOffset + 16),
            declaredCentralOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(eocdOffset + 20), 0);
    }

    internal static byte[] InputWithZipRecords()
    {
        byte[] bytes = Enumerable.Repeat((byte)0x55, 160).ToArray();
        const int local = 10;
        const int central = 43;
        const int eocd = 89;
        "PK\x03\x04"u8.CopyTo(bytes.AsSpan(local));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(local + 18), 3);
        bytes[local + 26] = 0;
        bytes[local + 27] = 0;
        bytes[local + 28] = 0;
        bytes[local + 29] = 0;
        bytes[local + 30] = 0xa1;
        bytes[local + 31] = 0xb2;
        bytes[local + 32] = 0xc3;

        "PK\x01\x02"u8.CopyTo(bytes.AsSpan(central));
        Array.Clear(bytes, central + 28, 6);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(central + 42), 0);

        "PK\x05\x06"u8.CopyTo(bytes.AsSpan(eocd));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(eocd + 12), 46);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(eocd + 16), 33);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(eocd + 20), 0);
        return bytes;
    }

    private static void PutZipCentral(byte[] bytes, int centralOffset,
        uint localOffset)
    {
        "PK\x01\x02"u8.CopyTo(bytes.AsSpan(centralOffset));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(centralOffset + 42),
            localOffset);
    }

    internal static void PutMarker(byte[] bytes, int offset, bool cabinet,
        uint cabinetLength = 36, uint table = 20)
    {
        if (cabinet)
        {
            "MSCF"u8.CopyTo(bytes.AsSpan(offset));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 8), cabinetLength);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 16), table);
        }
        else
            new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0 }.CopyTo(bytes, offset);
    }

    internal static void PutAceMarker(byte[] bytes, int offset)
    {
        new byte[] { (byte)'*', (byte)'*', (byte)'A', (byte)'C', (byte)'E',
            (byte)'*', (byte)'*' }.CopyTo(bytes, offset + 7);
    }

    internal static void PutArjMarker(byte[] bytes, int offset,
        ushort headerLength = 6, bool validCrc = true)
    {
        bytes[offset] = 0x60;
        bytes[offset + 1] = 0xea;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(offset + 2),
            headerLength);
        for (var index = 0; index < headerLength; index++)
            bytes[offset + 4 + index] = (byte)(index + 1);

        uint crc = uint.MaxValue;
        for (var index = 0; index < headerLength; index++)
        {
            crc ^= bytes[offset + 4 + index];
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 0xedb88320u
                    : crc >> 1;
        }
        if (!validCrc)
            crc ^= 1;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(
            offset + 4 + headerLength), ~crc);
    }

    internal static void PutLzhMarker(byte[] bytes, int offset)
    {
        bytes[offset + 2] = (byte)'-';
        bytes[offset + 3] = (byte)'l';
        bytes[offset + 4] = (byte)'h';
        bytes[offset + 6] = (byte)'-';
    }

    internal static void PutLhaSfx(byte[] bytes, uint start)
    {
        bytes[0] = 0;
        bytes[1] = 0;
        bytes[2] = 3;
        bytes[3] = 0xf3;
        bytes[44] = (byte)'S';
        bytes[45] = (byte)'F';
        bytes[46] = (byte)'X';
        bytes[47] = (byte)'!';
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(52), start);
    }

    internal int Seek(BPTR file, int position, int mode)
    {
        Assert.Equal(Input.Raw, file.Raw);
        Assert.True(mode is -1 or 0 or 2);
        int occurrence = Next("Seek");
        int result = Cursor;
        if (Faults.TryGetValue(("Seek", occurrence), out var fault))
            result = fault.Count;
        if (result >= 0)
        {
            long next = mode == -1 ? position
                : mode == 2 ? (long)InputBytes.Length + position
                : (long)Cursor + position;
            Assert.InRange(next, 0, InputBytes.Length);
            Cursor = (int)next;
        }
        CompleteCall("Seek", file, APTR.Null, position, mode, result,
            Faults.TryGetValue(("Seek", occurrence), out fault) ? fault.Error : 0);
        return result;
    }

    internal int Read(BPTR file, APTR buffer, int length)
    {
        Assert.Equal(Input.Raw, file.Raw);
        int bufferOffset = checked((int)(buffer.Raw - Scratch.Raw));
        Assert.InRange(bufferOffset, 0, BufferBytes - 1);
        Assert.InRange(length, 1, BufferBytes);
        Assert.True(bufferOffset + length <= BufferBytes);
        int occurrence = Next("Read");
        int count = Math.Min(length, InputBytes.Length - Cursor);
        if (Faults.TryGetValue(("Read", occurrence), out var fault))
            count = fault.Count;
        int actual = count > 0 ? Math.Min(count, length) : 0;
        Assert.True(actual <= InputBytes.Length - Cursor);
        InputBytes.AsSpan(Cursor, actual).CopyTo(Storage.AsSpan(GuardBytes + bufferOffset));
        Initialized = Math.Max(Initialized, bufferOffset + actual);
        Cursor += actual;
        CompleteCall("Read", file, buffer, length, 0, count,
            Faults.TryGetValue(("Read", occurrence), out fault) ? fault.Error : 0);
        return count;
    }

    internal int Write(BPTR file, APTR buffer, int length)
    {
        Assert.Equal(Output.Raw, file.Raw);
        int bufferOffset = checked((int)(buffer.Raw - Scratch.Raw));
        Assert.InRange(bufferOffset, 0, BufferBytes - 1);
        Assert.InRange(length, 1, Initialized - bufferOffset);
        int occurrence = Next("Write");
        int count = length;
        if (Faults.TryGetValue(("Write", occurrence), out var fault))
            count = fault.Count;
        if (count > 0 && count <= length)
            OutputBytes.AddRange(Storage.AsSpan(GuardBytes + bufferOffset, count).ToArray());
        CompleteCall("Write", file, buffer, length, 0, count,
            Faults.TryGetValue(("Write", occurrence), out fault) ? fault.Error : 0);
        return count;
    }

    internal int IoErr()
    {
        Assert.True(_captureAllowed, "IoErr must follow exactly one raw -1, not success/short I/O.");
        _captureAllowed = false;
        IoErrCalls++;
        Calls.Add(new IoCall("IoErr", 0, 0, 0, 0, AmbientIoErr));
        return AmbientIoErr;
    }

    internal void SetIoErr(int ioError)
    {
        SetIoErrCalls++;
        SetIoErrValue = ioError;
        AmbientIoErr = ioError;
    }

    internal bool IsBreakPending() => BreakPending ||
        (BreakAfterCallCount >= 0 && Calls.Count >= BreakAfterCallCount);

    private int Next(string operation)
    {
        Assert.False(_captureAllowed, "A -1 error observation was lost before another DOS call.");
        _occurrences.TryGetValue(operation, out int count);
        _occurrences[operation] = count + 1;
        return count + 1;
    }

    private void CompleteCall(string operation, BPTR file, APTR buffer,
        int argument, int mode, int result, int error)
    {
        _captureAllowed = result == -1;
        AmbientIoErr = _captureAllowed ? error : unchecked((int)0xace01234);
        Calls.Add(new IoCall(operation, file.Raw, buffer.Raw, argument, mode, result));
    }

    internal bool IsMapped(APTR address, uint size)
    {
        MappingCalls++;
        return !RejectMapping && size <= BufferBytes && address.Raw >= Scratch.Raw &&
            (ulong)address.Raw - Scratch.Raw + size <= BufferBytes;
    }

    internal byte ReadByte(APTR address, int offset)
    {
        long index = (long)address.Raw - Scratch.Raw + offset;
        Assert.InRange(index, 0, Initialized - 1);
        ByteReads++;
        return Storage[GuardBytes + (int)index];
    }

    internal void WriteByte(APTR address, int offset, byte value)
    {
        long index = (long)address.Raw - Scratch.Raw + offset;
        Assert.InRange(index, 0, BufferBytes - 1);
        Storage[GuardBytes + (int)index] = value;
    }

    internal void AssertUnownedResourcesAndGuards()
    {
        Assert.False(_captureAllowed, "Failure IoErr was never captured.");
        Assert.Equal(OriginalInput, InputBytes);
        Assert.All(Storage.Take(GuardBytes), b => Assert.Equal(0xcc, b));
        Assert.All(Storage.Skip(GuardBytes + BufferBytes), b => Assert.Equal(0xcc, b));
    }
}

internal readonly struct Exe2ArcTestIo(Exe2ArcTestIoState state) : IExe2ArcIo,
    IExe2ArcBreakSource
{
    public int Read(BPTR file, APTR buffer, int length) => state.Read(file, buffer, length);
    public int Write(BPTR file, APTR buffer, int length) => state.Write(file, buffer, length);
    public int Seek(BPTR file, int position, int mode) => state.Seek(file, position, mode);
    public int IoErr() => state.IoErr();
    public bool IsBreakPending() => state.IsBreakPending();
    public void SetIoErr(int ioError) => state.SetIoErr(ioError);
    public bool IsMapped(APTR address, uint byteSize) => state.IsMapped(address, byteSize);
    public byte ReadUInt8(APTR address, int offset = 0) => state.ReadByte(address, offset);
    public ushort ReadUInt16(APTR address, int offset = 0) => throw new NotSupportedException();
    public uint ReadUInt32(APTR address, int offset = 0) => throw new NotSupportedException();
    public void WriteUInt8(APTR address, int offset, byte value) => state.WriteByte(address, offset, value);
    public void WriteUInt16(APTR address, int offset, ushort value) => throw new NotSupportedException();
    public void WriteUInt32(APTR address, int offset, uint value) => throw new NotSupportedException();
    public void Clear(APTR address, uint byteCount) => throw new NotSupportedException();
    public void Copy(APTR source, APTR destination, uint byteCount) => throw new NotSupportedException();
}
