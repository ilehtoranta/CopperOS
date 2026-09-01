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
    internal int AmbientIoErr = 0x13572468;
    internal bool RejectMapping;
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

    internal int Seek(BPTR file, int position, int mode)
    {
        Assert.Equal(Input.Raw, file.Raw);
        Assert.True(mode is -1 or 0);
        int occurrence = Next("Seek");
        int result = Cursor;
        if (Faults.TryGetValue(("Seek", occurrence), out var fault))
            result = fault.Count;
        if (result >= 0)
        {
            long next = mode == -1 ? position : (long)Cursor + position;
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
        Assert.Equal(Scratch.Raw, buffer.Raw);
        Assert.InRange(length, 1, BufferBytes);
        int occurrence = Next("Read");
        int count = Math.Min(length, InputBytes.Length - Cursor);
        if (Faults.TryGetValue(("Read", occurrence), out var fault))
            count = fault.Count;
        Initialized = count > 0 ? Math.Min(count, length) : 0;
        Assert.True(Initialized <= InputBytes.Length - Cursor);
        InputBytes.AsSpan(Cursor, Initialized).CopyTo(Storage.AsSpan(GuardBytes));
        Cursor += Initialized;
        CompleteCall("Read", file, buffer, length, 0, count,
            Faults.TryGetValue(("Read", occurrence), out fault) ? fault.Error : 0);
        return count;
    }

    internal int Write(BPTR file, APTR buffer, int length)
    {
        Assert.Equal(Output.Raw, file.Raw);
        Assert.Equal(Scratch.Raw, buffer.Raw);
        Assert.InRange(length, 1, Initialized);
        int occurrence = Next("Write");
        int count = length;
        if (Faults.TryGetValue(("Write", occurrence), out var fault))
            count = fault.Count;
        if (count > 0 && count <= length)
            OutputBytes.AddRange(Storage.AsSpan(GuardBytes, count).ToArray());
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

    internal void AssertUnownedResourcesAndGuards()
    {
        Assert.False(_captureAllowed, "Failure IoErr was never captured.");
        Assert.Equal(OriginalInput, InputBytes);
        Assert.All(Storage.Take(GuardBytes), b => Assert.Equal(0xcc, b));
        Assert.All(Storage.Skip(GuardBytes + BufferBytes), b => Assert.Equal(0xcc, b));
    }
}

internal readonly struct Exe2ArcTestIo(Exe2ArcTestIoState state) : IExe2ArcIo
{
    public int Read(BPTR file, APTR buffer, int length) => state.Read(file, buffer, length);
    public int Write(BPTR file, APTR buffer, int length) => state.Write(file, buffer, length);
    public int Seek(BPTR file, int position, int mode) => state.Seek(file, position, mode);
    public int IoErr() => state.IoErr();
    public bool IsMapped(APTR address, uint byteSize) => state.IsMapped(address, byteSize);
    public byte ReadUInt8(APTR address, int offset = 0) => state.ReadByte(address, offset);
    public ushort ReadUInt16(APTR address, int offset = 0) => throw new NotSupportedException();
    public uint ReadUInt32(APTR address, int offset = 0) => throw new NotSupportedException();
    public void WriteUInt8(APTR address, int offset, byte value) => throw new NotSupportedException();
    public void WriteUInt16(APTR address, int offset, ushort value) => throw new NotSupportedException();
    public void WriteUInt32(APTR address, int offset, uint value) => throw new NotSupportedException();
    public void Clear(APTR address, uint byteCount) => throw new NotSupportedException();
    public void Copy(APTR source, APTR destination, uint byteCount) => throw new NotSupportedException();
}
